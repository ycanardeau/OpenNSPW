namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Sends a KeepAlive after a period without traffic from the partner ([MC-DPL8R] section 3.1.6.6).
/// </summary>
internal interface ICanKeepAlive
	: IConnectionTransition<ConnectionMessage.KeepAliveTimerElapsed>,
		IHasChannel
{
	ConnectionState IConnectionTransition<ConnectionMessage.KeepAliveTimerElapsed>.Execute(
		Connection connection,
		ConnectionMessage.KeepAliveTimerElapsed command
	)
	{
		Channel.SendKeepAlive();
		connection.KeepAliveTimer.Start(connection.Profile.KeepAliveInterval);
		return Current;
	}
}
