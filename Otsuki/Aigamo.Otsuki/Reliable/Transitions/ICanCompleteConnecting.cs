using Aigamo.Otsuki.Messages.Reliable;

namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Confirms the listener's polled CONNECTED or CONNECTED_SIGNED and establishes the connection ([MC-DPL8R] sections
/// 3.1.5.1.2 and 3.1.5.1.3).
/// </summary>
/// <remarks>
/// The connector signs whichever way the listener offers, as DirectPlay's connecting players use the host's choice.
/// </remarks>
internal interface ICanCompleteConnecting
	: IConnectionTransition<ReliableMessage.Connected>,
		IConnectionTransition<ReliableMessage.ConnectedSigned>,
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

	ConnectionState IConnectionTransition<ReliableMessage.ConnectedSigned>.Execute(
		Connection connection,
		ReliableMessage.ConnectedSigned message
	)
	{
		if (
			message.SessionId != Handshake.SessionId
			|| message.MajorVersion != 1
			|| message.MinorVersion < 6
			|| !message.Poll
			|| message.SigningOptions is not (PacketSigning.Fast or PacketSigning.Full)
		)
			return Current;

		var handshake = Handshake with
		{
			RemoteProtocolVersion = message.ProtocolVersion,
			MessageId = (byte)(Handshake.MessageId + 1),
			RemoteMessageId = message.MessageId,
			RemoteTimestamp = message.Timestamp,
			Signing = message.SigningOptions,
			ConnectSignature = message.ConnectSignature,
			SenderSecret = ICanAcceptConnect.CreateSecret(),
			ReceiverSecret = ICanAcceptConnect.CreateSecret(),
		};
		connection.Transmit(handshake.CreateConnectedSigned(poll: false, connection.Timestamp));
		return connection.Establish(handshake);
	}
}
