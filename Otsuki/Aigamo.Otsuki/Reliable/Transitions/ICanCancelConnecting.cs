using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Abandons a connection that is not established yet.
/// </summary>
internal interface ICanCancelConnecting
	: IConnectionTransition<ConnectionMessage.Disconnect>,
		IConnectionTransition<ConnectionMessage.HardDisconnect>
{
	private static ConnectionState Cancel(Connection connection)
	{
		if (connection.IsOutbound)
			connection.Observer.OnConnectFailed(connection, ResultCode.UserCancel);
		return connection.Terminate();
	}

	ConnectionState IConnectionTransition<ConnectionMessage.Disconnect>.Execute(
		Connection connection,
		ConnectionMessage.Disconnect command
	) => Cancel(connection);

	ConnectionState IConnectionTransition<ConnectionMessage.HardDisconnect>.Execute(
		Connection connection,
		ConnectionMessage.HardDisconnect command
	) => Cancel(connection);
}
