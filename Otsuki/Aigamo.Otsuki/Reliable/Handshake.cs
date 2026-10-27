using Aigamo.Otsuki.Messages.Reliable;

namespace Aigamo.Otsuki.Reliable;

/// <summary>
/// What the CONNECT and CONNECTED exchange established ([MC-DPL8R] sections 3.1.5.1.1 and 3.1.5.1.2).
/// </summary>
internal sealed record Handshake(SessionId SessionId, TimeSpan RetryInterval)
{
	public int RemoteProtocolVersion { get; init; }

	/// <summary>
	/// bMsgID of our next CONNECT or CONNECTED.
	/// </summary>
	public byte MessageId { get; init; }

	/// <summary>
	/// bMsgID of the partner's last CONNECT or CONNECTED, echoed in bRspId.
	/// </summary>
	public byte RemoteMessageId { get; init; }

	public int Retries { get; init; }

	/// <summary>
	/// The signing mode: the one the listener offers, then the one in force once established.
	/// </summary>
	public PacketSigning Signing { get; init; }

	/// <summary>
	/// The listener's cookie in CONNECTED_SIGNED, which the connector echoes ([MC-DPL8R] section 3.1.5.1.3).
	/// </summary>
	public ulong ConnectSignature { get; init; }

	/// <summary>
	/// The secret that signs the connector's frames, chosen by the connector.
	/// </summary>
	public ulong SenderSecret { get; init; }

	/// <summary>
	/// The secret that signs the listener's frames, chosen by the connector.
	/// </summary>
	public ulong ReceiverSecret { get; init; }

	/// <summary>
	/// The <b>tTimestamp</b> of the partner's CONNECTED_SIGNED, echoed in ours.
	/// </summary>
	public int RemoteTimestamp { get; init; }

	/// <summary>
	/// The retry period doubles up to <paramref name="maxRetryInterval"/> ([MC-DPL8R] section 3.1.6.1).
	/// </summary>
	public Handshake Retry(TimeSpan maxRetryInterval) =>
		this with
		{
			Retries = Retries + 1,
			MessageId = (byte)(MessageId + 1),
			RetryInterval =
				RetryInterval * 2 < maxRetryInterval ? RetryInterval * 2 : maxRetryInterval,
		};

	public ReliableMessage.Connect CreateConnect(int timestamp) =>
		new()
		{
			Poll = true,
			MessageId = MessageId,
			ProtocolVersion = Connection.ProtocolVersion,
			SessionId = SessionId,
			Timestamp = timestamp,
		};

	public ReliableMessage.Connected CreateConnected(bool poll, int timestamp) =>
		new()
		{
			Poll = poll,
			MessageId = MessageId,
			ResponseId = RemoteMessageId,
			ProtocolVersion = Connection.ProtocolVersion,
			SessionId = SessionId,
			Timestamp = timestamp,
		};

	/// <summary>
	/// The listener's polled offer, with its cookie and no secrets, or the connector's confirmation, with the secrets.
	/// </summary>
	public ReliableMessage.ConnectedSigned CreateConnectedSigned(bool poll, int timestamp) =>
		new()
		{
			Poll = poll,
			MessageId = MessageId,
			ResponseId = RemoteMessageId,
			ProtocolVersion = Connection.ProtocolVersion,
			SessionId = SessionId,
			Timestamp = timestamp,
			ConnectSignature = ConnectSignature,
			SenderSecret = poll ? 0 : SenderSecret,
			ReceiverSecret = poll ? 0 : ReceiverSecret,
			SigningOptions = Signing,
			EchoTimestamp = RemoteTimestamp,
		};

	/// <summary>
	/// The listener's answer to CONNECT: CONNECTED_SIGNED when it offers signing, CONNECTED otherwise.
	/// </summary>
	public ReliableMessage CreateListenerResponse(int timestamp) =>
		Signing == PacketSigning.None
			? CreateConnected(poll: true, timestamp)
			: CreateConnectedSigned(poll: true, timestamp);

	public PacketSigner? CreateSigner(bool isOutbound) =>
		Signing == PacketSigning.None ? null
		: isOutbound
			? new PacketSigner(Signing, localSecret: SenderSecret, remoteSecret: ReceiverSecret)
		: new PacketSigner(Signing, localSecret: ReceiverSecret, remoteSecret: SenderSecret);

	public ReliableMessage.HardDisconnect CreateHardDisconnect(int timestamp) =>
		new()
		{
			MessageId = MessageId,
			ProtocolVersion = Connection.ProtocolVersion,
			SessionId = SessionId,
			Timestamp = timestamp,
		};
}
