using Aigamo.Otsuki.Messages.Reliable;

namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Repeats our final CONNECTED or CONNECTED_SIGNED when the listener is still retrying its own ([MC-DPL8R] sections
/// 3.1.5.1.2 and 3.1.5.1.3).
/// </summary>
internal interface ICanConfirmConnected
	: IConnectionTransition<ReliableMessage.Connected>,
		IConnectionTransition<ReliableMessage.ConnectedSigned>,
		IHasHandshake
{
	ConnectionState IConnectionTransition<ReliableMessage.Connected>.Execute(
		Connection connection,
		ReliableMessage.Connected message
	)
	{
		if (
			connection.IsOutbound
			&& Handshake.Signing == PacketSigning.None
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

	ConnectionState IConnectionTransition<ReliableMessage.ConnectedSigned>.Execute(
		Connection connection,
		ReliableMessage.ConnectedSigned message
	)
	{
		if (
			connection.IsOutbound
			&& Handshake.Signing != PacketSigning.None
			&& message.SessionId == Handshake.SessionId
			&& message.MajorVersion == 1
			&& message.Poll
		)
		{
			var handshake = Handshake with
			{
				RemoteMessageId = message.MessageId,
				RemoteTimestamp = message.Timestamp,
			};
			connection.Transmit(handshake.CreateConnectedSigned(poll: false, connection.Timestamp));
		}
		return Current;
	}
}
