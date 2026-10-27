using Aigamo.Otsuki.Messages.Reliable;

namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Establishes an inbound connection on the connector's final CONNECTED ([MC-DPL8R] section 3.1.5.1.2), or on its
/// first DFRAME or SACK, which prove that it saw our CONNECTED even if its own was lost. A signed connection needs the
/// connector's CONNECTED_SIGNED, which carries the secrets (section 3.1.5.1.3); until it arrives, our retried offer
/// makes the connector repeat it.
/// </summary>
internal interface ICanCompleteListening
	: IConnectionTransition<ReliableMessage.Connected>,
		IConnectionTransition<ReliableMessage.ConnectedSigned>,
		IConnectionTransition<ReliableMessage.DataFrame>,
		IConnectionTransition<ReliableMessage.Sack>,
		IHasHandshake
{
	private bool IsSigned => Handshake.Signing != PacketSigning.None;

	ConnectionState IConnectionTransition<ReliableMessage.Connected>.Execute(
		Connection connection,
		ReliableMessage.Connected message
	) =>
		IsSigned
		|| message.SessionId != Handshake.SessionId
		|| message.MajorVersion != 1
		|| message.Poll
			? Current
			: connection.Establish(Handshake);

	ConnectionState IConnectionTransition<ReliableMessage.ConnectedSigned>.Execute(
		Connection connection,
		ReliableMessage.ConnectedSigned message
	) =>
		!IsSigned
		|| message.SessionId != Handshake.SessionId
		|| message.MajorVersion != 1
		|| message.Poll
		|| message.ConnectSignature != Handshake.ConnectSignature
		|| message.SigningOptions != Handshake.Signing
		|| message.SenderSecret == 0
		|| message.ReceiverSecret == 0
			? Current
			: connection.Establish(
				Handshake with
				{
					SenderSecret = message.SenderSecret,
					ReceiverSecret = message.ReceiverSecret,
				}
			);

	ConnectionState IConnectionTransition<ReliableMessage.DataFrame>.Execute(
		Connection connection,
		ReliableMessage.DataFrame message
	) => IsSigned ? Current : connection.Apply(connection.Establish(Handshake), message);

	ConnectionState IConnectionTransition<ReliableMessage.Sack>.Execute(
		Connection connection,
		ReliableMessage.Sack message
	) => IsSigned ? Current : connection.Apply(connection.Establish(Handshake), message);
}
