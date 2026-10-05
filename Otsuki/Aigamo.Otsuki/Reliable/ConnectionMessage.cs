using Aigamo.MatchGenerator;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Messages.Reliable;

namespace Aigamo.Otsuki.Reliable;

[GenerateMatch]
internal abstract record ConnectionMessage
{
	private ConnectionMessage() { }

	/// <summary>
	/// Begins an outbound connection by sending CONNECT.
	/// </summary>
	public sealed record StartConnecting : ConnectionMessage;

	public sealed record DatagramArrived(ReliableMessage Message) : ConnectionMessage;

	/// <summary>
	/// Sends an upper-layer message. <see cref="IConnectionObserver.OnSendCompleted"/> reports
	/// <paramref name="SendId"/> when it is acknowledged (reliable) or transmitted (unreliable).
	/// </summary>
	public sealed record SendPayload(long SendId, byte[] Payload, bool Reliable, bool User1)
		: ConnectionMessage;

	/// <summary>
	/// Disconnects gracefully after all queued messages are delivered.
	/// </summary>
	public sealed record Disconnect : ConnectionMessage;

	/// <summary>
	/// Disconnects immediately, discarding queued messages ([MC-DPL8R] section 3.1.4.5).
	/// </summary>
	public sealed record HardDisconnect : ConnectionMessage;

	/// <summary>
	/// Terminates silently without telling the partner, for example when the endpoint shuts down.
	/// </summary>
	public sealed record Abort(ResultCode Result) : ConnectionMessage;

	public sealed record ConnectRetryTimerElapsed(long Generation) : ConnectionMessage;

	public sealed record RetryTimerElapsed(long Generation) : ConnectionMessage;

	public sealed record DelayedAcknowledgmentTimerElapsed(long Generation) : ConnectionMessage;

	public sealed record DelayedSendMaskTimerElapsed(long Generation) : ConnectionMessage;

	public sealed record KeepAliveTimerElapsed(long Generation) : ConnectionMessage;

	public sealed record HardDisconnectTimerElapsed(long Generation) : ConnectionMessage;

	public sealed record GracefulDisconnectTimerElapsed(long Generation) : ConnectionMessage;
}
