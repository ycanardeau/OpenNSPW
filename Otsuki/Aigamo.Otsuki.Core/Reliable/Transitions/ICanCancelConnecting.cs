using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Abandons a connection that is not established yet.
/// </summary>
internal interface ICanCancelConnecting
	: IConnectionTransition<ConnectionMessage.Disconnect>,
		IConnectionTransition<ConnectionMessage.HardDisconnect>
{
	private static ConnectionState Cancel(ReliableConnection connection)
	{
		if (connection.IsOutbound)
			connection.Observer.OnConnectFailed(connection.Handle, ResultCode.UserCancel);
		return connection.Terminate();
	}

	ConnectionState IConnectionTransition<ConnectionMessage.Disconnect>.Execute(
		ReliableConnection connection,
		ConnectionMessage.Disconnect command
	) => Cancel(connection);

	ConnectionState IConnectionTransition<ConnectionMessage.HardDisconnect>.Execute(
		ReliableConnection connection,
		ConnectionMessage.HardDisconnect command
	) => Cancel(connection);
}
