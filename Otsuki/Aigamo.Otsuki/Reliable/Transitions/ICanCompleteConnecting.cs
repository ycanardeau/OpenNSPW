using Aigamo.Otsuki.Messages.Reliable;

namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Confirms the listener's polled CONNECTED and establishes the connection ([MC-DPL8R] section 3.1.5.1.2).
/// </summary>
internal interface ICanCompleteConnecting
	: IConnectionTransition<ReliableMessage.Connected>,
		IHasHandshake
{
	ConnectionState IConnectionTransition<ReliableMessage.Connected>.Execute(
		Connection connection,
		ReliableMessage.Connected message
	)
	{
		if (message.SessionId != Handshake.SessionId || message.MajorVersion != 1 || !message.Poll)
			return Current;

		var handshake = Handshake with
		{
			RemoteProtocolVersion = message.ProtocolVersion,
			MessageId = (byte)(Handshake.MessageId + 1),
			RemoteMessageId = message.MessageId,
		};
		connection.Transmit(handshake.CreateConnected(poll: false, connection.Timestamp));
		return connection.Establish(handshake);
	}
}
