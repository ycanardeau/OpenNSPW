using Aigamo.Otsuki.Messages.Reliable;

namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Establishes an inbound connection on the connector's final CONNECTED ([MC-DPL8R] section 3.1.5.1.2), or on its
/// first DFRAME or SACK, which prove that it saw our CONNECTED even if its own was lost.
/// </summary>
internal interface ICanCompleteListening
	: IConnectionTransition<ReliableMessage.Connected>,
		IConnectionTransition<ReliableMessage.DataFrame>,
		IConnectionTransition<ReliableMessage.Sack>,
		IHasHandshake
{
	ConnectionState IConnectionTransition<ReliableMessage.Connected>.Execute(
		Connection connection,
		ReliableMessage.Connected message
	) =>
		message.SessionId != Handshake.SessionId || message.MajorVersion != 1 || message.Poll
			? Current
			: connection.Establish(Handshake);

	ConnectionState IConnectionTransition<ReliableMessage.DataFrame>.Execute(
		Connection connection,
		ReliableMessage.DataFrame message
	) => connection.Apply(connection.Establish(Handshake), message);

	ConnectionState IConnectionTransition<ReliableMessage.Sack>.Execute(
		Connection connection,
		ReliableMessage.Sack message
	) => connection.Apply(connection.Establish(Handshake), message);
}
