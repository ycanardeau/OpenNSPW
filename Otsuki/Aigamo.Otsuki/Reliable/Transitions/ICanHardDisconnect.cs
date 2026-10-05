using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Disconnects immediately ([MC-DPL8R] section 3.1.4.5).
/// </summary>
internal interface ICanHardDisconnect
	: IConnectionTransition<ConnectionMessage.HardDisconnect>,
		IHasHandshake,
		IHasChannel
{
	ConnectionState IConnectionTransition<ConnectionMessage.HardDisconnect>.Execute(
		Connection connection,
		ConnectionMessage.HardDisconnect command
	)
	{
		Channel.FailAllSends(ResultCode.ConnectionLost);
		connection.CancelChannelTimers();
		connection.Transmit(Handshake.CreateHardDisconnect(connection.Timestamp));
		connection.HardDisconnectTimer.Start(connection.Profile.HardDisconnectInterval);
		return new ConnectionState.HardDisconnecting(Handshake, Sent: 1);
	}
}
