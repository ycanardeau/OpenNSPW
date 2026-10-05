using Aigamo.Otsuki.Messages.Reliable;

namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Answers a retried CONNECT, whose CONNECTED was apparently lost ([MC-DPL8R] section 3.1.5.1.1).
/// </summary>
internal interface ICanRepeatConnected
	: IConnectionTransition<ReliableMessage.Connect>,
		IHasHandshake
{
	ConnectionState IConnectionTransition<ReliableMessage.Connect>.Execute(
		Connection connection,
		ReliableMessage.Connect message
	)
	{
		if (message.SessionId != Handshake.SessionId)
			return Current;

		var handshake = Handshake with { RemoteMessageId = message.MessageId };
		connection.Transmit(handshake.CreateConnected(poll: true, connection.Timestamp));
		return new ConnectionState.ConnectReceived(handshake);
	}
}
