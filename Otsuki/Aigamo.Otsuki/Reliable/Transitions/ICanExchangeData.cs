using Aigamo.Otsuki.Messages.Reliable;

namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Processes DFRAMEs and SACKs ([MC-DPL8R] sections 3.1.5.1.5 and 3.1.5.2).
/// </summary>
internal interface ICanExchangeData
	: IConnectionTransition<ReliableMessage.DataFrame>,
		IConnectionTransition<ReliableMessage.Sack>,
		IHasChannel
{
	ConnectionState IConnectionTransition<ReliableMessage.DataFrame>.Execute(
		Connection connection,
		ReliableMessage.DataFrame message
	) => connection.Settle(Current, Channel, Channel.Receive(message));

	ConnectionState IConnectionTransition<ReliableMessage.Sack>.Execute(
		Connection connection,
		ReliableMessage.Sack message
	) => connection.Settle(Current, Channel, Channel.Receive(message));
}
