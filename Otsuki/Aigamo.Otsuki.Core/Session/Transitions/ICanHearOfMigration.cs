using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// A peer still joining hears of host migration ([MC-DPL8CS] section 3.2.5.4): the new host counts it among the peers
/// that report to it, but the join goes on with the old host, whose loss ends the join. The claim waits, with what the
/// new host sends after it, until the join completes and we lose the old host too (<see cref="ICanLoseHost"/>).
/// </summary>
internal interface ICanHearOfMigration : ISessionTransition<Received<CoreMessage.HostMigrate>>
{
	SessionState ISessionTransition<Received<CoreMessage.HostMigrate>>.Execute(
		SessionCore session,
		Received<CoreMessage.HostMigrate> command
	)
	{
		if (
			session.PlayersByConnection.TryGetValue(command.Connection, out var newHost)
			&& newHost.Id == command.Message.DpnidNewHost
		)
			session.KeepClaim(command);
		return Current;
	}
}
