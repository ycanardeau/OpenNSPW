using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// The new host's side of host migration ([MC-DPL8CS] section 3.2.5.4): installs the operations it asked a peer for
/// with DN_REQ_NAMETABLE_OP, and stops waiting for peers that do not report in time. The version reports themselves
/// arrive through <see cref="ICanResynchronizeVersion"/>.
/// </summary>
internal interface ICanCompleteMigration
	: ISessionTransition<Received<CoreMessage.AckNameTableOperations>>,
		ISessionTransition<SessionMessage.MigrationTimedOut>,
		IHasHostingData
{
	SessionState ISessionTransition<Received<CoreMessage.AckNameTableOperations>>.Execute(
		SessionActor session,
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

	SessionState ISessionTransition<SessionMessage.MigrationTimedOut>.Execute(
		SessionActor session,
		SessionMessage.MigrationTimedOut command
	) => Migration is null ? Current : session.FinishMigration(ToHosting());
}
