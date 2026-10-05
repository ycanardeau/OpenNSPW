using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Spaces out the HARD_DISCONNECT packets and gives up after the last one ([MC-DPL8R] section 3.1.6.4).
/// </summary>
internal interface ICanRepeatHardDisconnect
	: IConnectionTransition<ConnectionMessage.HardDisconnectTimerElapsed>,
		IHasHandshake
{
	int Sent { get; }

	ConnectionState IConnectionTransition<ConnectionMessage.HardDisconnectTimerElapsed>.Execute(
		Connection connection,
		ConnectionMessage.HardDisconnectTimerElapsed command
	)
	{
		if (Sent >= connection.Profile.HardDisconnectCount)
		{
			connection.Observer.OnDisconnected(connection, ResultCode.Success);
			return connection.Terminate();
		}

		connection.Transmit(Handshake.CreateHardDisconnect(connection.Timestamp));
		connection.HardDisconnectTimer.Start(connection.Profile.HardDisconnectInterval);
		return new ConnectionState.HardDisconnecting(Handshake, Sent + 1);
	}
}
