using Aigamo.Otsuki.Messages.Reliable;

namespace Aigamo.Otsuki.Reliable;

/// <summary>
/// Signs the frames of a connection established with signing and validates the partner's ([MC-DPL8R] section
/// 3.1.5.2.7). Owned by a <see cref="Connection"/> and only used from its message loop.
/// <para>
/// With fast signing, every frame carries the sender's secret. With full signing, it carries a digest of the frame
/// and the sender's secret, and both partners advance the sender's secret once per sequence space: the sender when it
/// sends sequence ID 192, the receiver when it reaches it. The new secret signs sequence IDs 0 to 191 of the next
/// round; IDs 192 to 255 keep the secret they started with, so that late frames of a round still validate.
/// </para>
/// </summary>
/// <param name="localSecret">The secret that signs our frames: CONNECTED_SIGNED's <b>ullSenderSecret</b> on the
/// connector, its <b>ullReceiverSecret</b> on the listener.</param>
/// <param name="remoteSecret">The secret that signs the partner's frames.</param>
internal sealed class PacketSigner(PacketSigning mode, ulong localSecret, ulong remoteSecret)
{
	/// <summary>
	/// Reaching this sequence ID advances the secret, and the secrets from here to the end of the sequence space are
	/// the ones of the previous round.
	/// </summary>
	private static readonly SequenceId AdvanceSequence = new(192);

	/// <summary>
	/// One direction of a full-signed connection: the secret, the one before it, and the modifier of the next one.
	/// </summary>
	private sealed class SecretChain(ulong secret)
	{
		public ulong Current { get; private set; } = secret;

		public ulong Previous { get; private set; } = secret;

		/// <summary>
		/// Starts as the secret of the connection, then comes from the first reliable payload of each round.
		/// </summary>
		public ulong Modifier { get; private set; } = secret;

		public bool ModifierTaken { get; private set; }

		/// <summary>
		/// Whether the secret already advanced in this round, so that IDs from 192 on use <see cref="Previous"/>.
		/// </summary>
		public bool AdvancedThisRound { get; private set; }

		public ulong SecretFor(SequenceId sequence) =>
			sequence.Value >= AdvanceSequence.Value && AdvancedThisRound ? Previous : Current;

		/// <summary>
		/// Accounts for the frame with <paramref name="sequence"/>, in sequence order. <paramref name="payload"/> is
		/// the reliable payload that it carries, if any; KeepAlives and unreliable frames carry none.
		/// </summary>
		public void Observe(SequenceId sequence, byte[]? payload)
		{
			if (sequence.Value == 0)
				AdvancedThisRound = false;

			if (sequence == AdvanceSequence)
			{
				Previous = Current;
				Current = PacketSignature.ComputeNextSecret(Previous, Modifier);
				ModifierTaken = false;
				AdvancedThisRound = true;
			}
			else if (
				sequence.Value < AdvanceSequence.Value
				&& !ModifierTaken
				&& payload is { Length: > 0 }
			)
			{
				Modifier = PacketSignature.ComputeModifier(payload);
				ModifierTaken = true;
			}
		}
	}

	private readonly PacketSigning _mode = mode;
	private readonly SecretChain _local = new(localSecret);
	private readonly SecretChain _remote = new(remoteSecret);

	public bool IsFullSigned => _mode == PacketSigning.Full;

	private SequenceId? _lastSent;

	/// <summary>
	/// The secret for SACKs and HARD_DISCONNECTs: that of the last data frame sent, which the partner already holds.
	/// A newer one would not validate at a partner that has not reached sequence ID 192 yet.
	/// </summary>
	public ulong ControlSecret =>
		_lastSent is { } sequence ? _local.SecretFor(sequence) : _local.Current;

	/// <summary>
	/// The secret that signs our frame with <paramref name="sequence"/>, which must be read when the frame first gets
	/// its sequence ID, after <see cref="ObserveSent"/>, and kept for its retries.
	/// </summary>
	public ulong LocalSecretFor(SequenceId sequence) => _local.SecretFor(sequence);

	public void ObserveSent(SequenceId sequence, byte[]? reliablePayload)
	{
		_lastSent = sequence;
		if (IsFullSigned)
			_local.Observe(sequence, reliablePayload);
	}

	public void ObserveReceived(SequenceId sequence, byte[]? reliablePayload)
	{
		if (IsFullSigned)
			_remote.Observe(sequence, reliablePayload);
	}

	private static ulong Sign(byte[] datagram, int offset, PacketSigning mode, ulong secret) =>
		mode == PacketSigning.Full
			? PacketSignature.ComputeFullSignature(datagram, offset, secret)
			: secret;

	/// <summary>
	/// Writes the signature into a serialized frame. Frames without a signature field are left as they are.
	/// </summary>
	public byte[] Sign(byte[] datagram, ulong secret)
	{
		if (PacketSignature.GetSignatureOffset(datagram) is int offset)
			PacketSignature.WriteSignature(datagram, offset, Sign(datagram, offset, _mode, secret));
		return datagram;
	}

	/// <summary>
	/// DFRAMEs, SACKs and HARD_DISCONNECTs must be signed; one too short to hold the field is invalid.
	/// </summary>
	private static bool IsSignedFrame(byte[] datagram) =>
		datagram.Length >= 2
		&& (
			((PacketCommand)datagram[0]).HasFlag(PacketCommand.Data)
			|| (ExtendedOpcode)datagram[1] is ExtendedOpcode.Sack or ExtendedOpcode.HardDisconnect
		);

	/// <summary>
	/// Whether a frame of the partner carries a valid signature. Frames without a signature field, the handshake
	/// frames, need none.
	/// </summary>
	/// <remarks>
	/// [MC-DPL8R] picks one remote secret from the next expected sequence ID and the frame's own. Near the end of the
	/// sequence space, frames that arrive early can be signed with the other one, so this also accepts it; both are
	/// secrets of this connection.
	/// </remarks>
	public bool Validate(byte[] datagram, SequenceId nextReceive, SequenceId sequence)
	{
		if (PacketSignature.GetSignatureOffset(datagram) is not int offset)
			return !IsSignedFrame(datagram);

		var signature = PacketSignature.ReadSignature(datagram, offset);
		if (!IsFullSigned)
			return signature == _remote.Current;

		var usePrevious =
			nextReceive.Value >= AdvanceSequence.Value
			|| (nextReceive.Value < 64 && sequence.Value >= AdvanceSequence.Value);
		var (first, second) = usePrevious
			? (_remote.Previous, _remote.Current)
			: (_remote.Current, _remote.Previous);
		return signature == Sign(datagram, offset, _mode, first)
			|| signature == Sign(datagram, offset, _mode, second);
	}
}
