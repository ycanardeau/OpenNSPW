using Aigamo.Otsuki.Messages.Reliable;

namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Answers a new CONNECT with a polled CONNECTED ([MC-DPL8R] section 3.1.5.1.1).
/// </summary>
internal interface ICanAcceptConnect : IConnectionTransition<ReliableMessage.Connect>
{
	ConnectionState IConnectionTransition<ReliableMessage.Connect>.Execute(
		Connection connection,
		ReliableMessage.Connect message
	)
	{
		if (message.MajorVersion != 1)
			return connection.Terminate();

		var handshake = new Handshake(message.SessionId, connection.Profile.ConnectRetryInterval)
		{
			RemoteProtocolVersion = message.ProtocolVersion,
			RemoteMessageId = message.MessageId,
		};
		connection.Transmit(handshake.CreateConnected(poll: true, connection.Timestamp));
		connection.ConnectRetryTimer.Start(handshake.RetryInterval);
		return new ConnectionState.ConnectReceived(handshake);
	}
}
