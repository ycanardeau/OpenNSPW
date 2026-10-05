using Aigamo.Otsuki.Messages.Reliable;

namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Sends CONNECT ([MC-DPL8R] section 3.1.4.2).
/// </summary>
internal interface ICanStartConnecting : IConnectionTransition<ConnectionMessage.StartConnecting>
{
	ConnectionState IConnectionTransition<ConnectionMessage.StartConnecting>.Execute(
		Connection connection,
		ConnectionMessage.StartConnecting command
	)
	{
		var handshake = new Handshake(
			SessionId.NewSessionId(),
			connection.Profile.ConnectRetryInterval
		);
		connection.Transmit(handshake.CreateConnect(connection.Timestamp));
		connection.ConnectRetryTimer.Start(handshake.RetryInterval);
		return new ConnectionState.ConnectSent(handshake);
	}
}
