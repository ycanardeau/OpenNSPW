namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Resends CONNECT until the listener answers ([MC-DPL8R] section 3.1.6.1).
/// </summary>
internal interface ICanRetryConnect
	: IConnectionTransition<ConnectionMessage.ConnectRetryTimerElapsed>,
		IHasHandshake
{
	ConnectionState IConnectionTransition<ConnectionMessage.ConnectRetryTimerElapsed>.Execute(
		Connection connection,
		ConnectionMessage.ConnectRetryTimerElapsed command
	)
	{
		if (Handshake.Retries >= connection.Profile.MaxConnectRetries)
			return connection.GiveUpConnecting();

		var handshake = Handshake.Retry(connection.Profile.MaxConnectRetryInterval);
		connection.Transmit(handshake.CreateConnect(connection.Timestamp));
		connection.ConnectRetryTimer.Start(handshake.RetryInterval);
		return new ConnectionState.ConnectSent(handshake);
	}
}
