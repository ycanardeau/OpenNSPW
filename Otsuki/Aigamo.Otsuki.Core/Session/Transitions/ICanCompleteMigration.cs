using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// The new host's side of host migration ([MC-DPL8CS] section 3.2.5.4): installs the operations it asked a peer for
/// with DN_REQ_NAMETABLE_OP. The version reports themselves arrive through <see cref="ICanResynchronizeVersion"/>. It
/// waits for every peer it is connected to, as the specification has it, with no timer: a peer stops counting only when
/// its connection ends (<see cref="ICanLosePlayers"/>). A new host that stopped waiting on a timer finished with the
/// name table it had, while a peer still connected could hold operations of the old host that it lacked, and the two
/// name tables went apart for good.
/// </summary>
internal interface ICanCompleteMigration
	: ISessionTransition<Received<CoreMessage.AckNameTableOperations>>,
		IHasHostingData
{
	SessionState ISessionTransition<Received<CoreMessage.AckNameTableOperations>>.Execute(
		SessionCore session,
		Received<CoreMessage.AckNameTableOperations> command
	) =>
		Migration is { RequestedFrom: Dpnid requested }
		&& session.PlayersByConnection.TryGetValue(command.Connection, out var peer)
		&& peer.Id == requested
			? session.ApplyMissedOperations(
				ToHosting(),
				command.Message.Entries.OfType<INameTableOperation>()
			)
			: Current;
}
