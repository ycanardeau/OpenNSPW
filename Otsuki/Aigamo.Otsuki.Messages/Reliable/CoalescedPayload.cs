// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8r/0ce3a800-f861-4556-9078-2004c2662ab3

using System.Collections.Immutable;

namespace Aigamo.Otsuki.Messages.Reliable;

/// <summary>
/// One of the payloads carried by a <see cref="ReliableMessage.DataFrame"/> that has the <b>PACKET_CONTROL_COALESCE</b> flag set.
/// </summary>
[Immutable]
public sealed record CoalescedPayload
{
	/// <summary>
	/// Command field for the coalesced message. Only <see cref="CoalesceCommand.Reliable"/>, <see cref="CoalesceCommand.Sequential"/>, <see cref="CoalesceCommand.User1"/> and <see cref="CoalesceCommand.User2"/> are kept; the size bits and <see cref="CoalesceCommand.EndCoalesce"/> are framing, computed when serializing.
	/// </summary>
	public CoalesceCommand Command { get; internal init; }

	public bool Reliable
	{
		get => Command.HasFlag(CoalesceCommand.Reliable);
		init =>
			Command = value
				? (Command | CoalesceCommand.Reliable)
				: (Command & ~CoalesceCommand.Reliable);
	}

	public bool Sequential
	{
		get => Command.HasFlag(CoalesceCommand.Sequential);
		init =>
			Command = value
				? (Command | CoalesceCommand.Sequential)
				: (Command & ~CoalesceCommand.Sequential);
	}

	public bool User1
	{
		get => Command.HasFlag(CoalesceCommand.User1);
		init =>
			Command = value
				? (Command | CoalesceCommand.User1)
				: (Command & ~CoalesceCommand.User1);
	}

	public bool User2
	{
		get => Command.HasFlag(CoalesceCommand.User2);
		init =>
			Command = value
				? (Command | CoalesceCommand.User2)
				: (Command & ~CoalesceCommand.User2);
	}

	/// <summary>
	/// Consumer payload data, without alignment padding.
	/// </summary>
	public IImmutableList<byte> Payload { get; init; } = ImmutableArray<byte>.Empty;

	public override string ToString() =>
		$"{nameof(CoalescedPayload)} ["
		+ $"{nameof(Command)}={Command}, "
		+ $"{nameof(Payload)}={BitConverter.ToString(Payload.ToArray())}]";
}
