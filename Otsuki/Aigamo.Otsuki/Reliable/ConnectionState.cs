using Aigamo.MatchGenerator;
using Aigamo.Otsuki.Reliable.Transitions;

namespace Aigamo.Otsuki.Reliable;

internal interface IHasHandshake
{
	Handshake Handshake { get; }
}

internal interface IHasChannel
{
	ReliableChannel Channel { get; }
}

/// <summary>
/// The states of a connection ([MC-DPL8R] section 3.1, figure 1). Each state implements the transitions it allows;
/// a message that the current state has no transition for is ignored.
/// </summary>
[GenerateMatch]
internal abstract record ConnectionState
{
	private ConnectionState() { }

	/// <summary>
	/// An outbound connection that has not sent CONNECT yet.
	/// </summary>
	public sealed record Idle : ConnectionState, ICanStartConnecting, ICanCancelConnecting;

	/// <summary>
	/// An inbound connection waiting for the CONNECT that created it.
	/// </summary>
	public sealed record Listening : ConnectionState, ICanAcceptConnect, ICanCancelConnecting;

	public sealed record ConnectSent(Handshake Handshake)
		: ConnectionState,
			IHasHandshake,
			ICanCompleteConnecting,
			ICanRetryConnect,
			ICanCancelConnecting;

	public sealed record ConnectReceived(Handshake Handshake)
		: ConnectionState,
			IHasHandshake,
			ICanRepeatConnected,
			ICanCompleteListening,
			ICanRetryConnected,
			ICanCancelConnecting;

	public sealed record Established(Handshake Handshake, ReliableChannel Channel)
		: ConnectionState,
			IHasHandshake,
			IHasChannel,
			ICanConfirmConnected,
			ICanExchangeData,
			ICanSend,
			ICanRetransmit,
			ICanAcknowledge,
			ICanKeepAlive,
			ICanDisconnect,
			ICanTimeOutGracefulDisconnect,
			ICanHardDisconnect,
			ICanReceiveHardDisconnect;

	public sealed record HardDisconnecting(Handshake Handshake, int Sent)
		: ConnectionState,
			IHasHandshake,
			ICanRepeatHardDisconnect,
			ICanCompleteHardDisconnect;

	public sealed record Closed : ConnectionState;
}
