using Aigamo.Otsuki.Messages.Reliable;

namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Answers a new CONNECT with a polled CONNECTED, or with a polled CONNECTED_SIGNED when the endpoint signs
/// ([MC-DPL8R] sections 3.1.5.1.1 and 3.1.5.1.3).
/// </summary>
internal interface ICanAcceptConnect : IConnectionTransition<ReliableMessage.Connect>
{
	/// <summary>
	/// Signing needs protocol version 1.6 on both sides.
	/// </summary>
	private const int SigningProtocolVersion = 0x00010006;

	ConnectionState IConnectionTransition<ReliableMessage.Connect>.Execute(
		ReliableConnection connection,
		ReliableMessage.Connect message
	)
	{
		var signing = connection.OfferedSigning;
		if (
			message.MajorVersion != 1
			|| (signing != PacketSigning.None && message.ProtocolVersion < SigningProtocolVersion)
		)
			return connection.Terminate();

		var handshake = new Handshake(message.SessionId, connection.Profile.ConnectRetryInterval)
		{
			RemoteProtocolVersion = message.ProtocolVersion,
			RemoteMessageId = message.MessageId,
			Signing = signing,
			ConnectSignature = signing == PacketSigning.None ? 0 : connection.CreateSecret(),
		};
		connection.Transmit(handshake.CreateListenerResponse(connection.Timestamp));
		connection.ConnectRetryTimer.Start(handshake.RetryInterval);
		return new ConnectionState.ConnectReceived(handshake);
	}
}
