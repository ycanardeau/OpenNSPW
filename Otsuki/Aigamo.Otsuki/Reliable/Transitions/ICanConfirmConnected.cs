using Aigamo.Otsuki.Messages.Reliable;

namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Repeats our final CONNECTED when the listener is still retrying its own ([MC-DPL8R] section 3.1.5.1.2).
/// </summary>
internal interface ICanConfirmConnected
	: IConnectionTransition<ReliableMessage.Connected>,
		IHasHandshake
{
	ConnectionState IConnectionTransition<ReliableMessage.Connected>.Execute(
		Connection connection,
		ReliableMessage.Connected message
	)
	{
		if (
			connection.IsOutbound
			&& message.SessionId == Handshake.SessionId
			&& message.MajorVersion == 1
			&& message.Poll
		)
		{
			var handshake = Handshake with { RemoteMessageId = message.MessageId };
			connection.Transmit(handshake.CreateConnected(poll: false, connection.Timestamp));
		}
		return Current;
	}
}
