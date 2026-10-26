using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// A peer's side of host migration ([MC-DPL8CS] section 3.2.5.4): follows the peer that announces itself with
/// DN_HOST_MIGRATE, reports its name table version to it, and hands it the operations it missed.
/// </summary>
internal interface ICanFollowMigration
	: ISessionTransition<Received<CoreMessage.HostMigrate>>,
		ISessionTransition<Received<CoreMessage.RequestNameTableOperations>>,
		ISessionTransition<Received<CoreMessage.HostMigrateComplete>>,
		IHasHostConnection
{
	/// <summary>
	/// The new host may announce itself before we notice that the old one left; we follow it once we notice too
	/// (steps 1 and 2).
	/// </summary>
	SessionState ISessionTransition<Received<CoreMessage.HostMigrate>>.Execute(
		SessionActor session,
		Received<CoreMessage.HostMigrate> command
	)
	{
		var message = command.Message;
		if (
			!session.PlayersByConnection.TryGetValue(command.Connection, out var newHost)
			|| newHost.Id != message.DpnidNewHost
		)
			return Current;

		// While we still hear from the old host, the claim waits: the peer may merely have lost its own link to it.
		if (
			session.Players.TryGetValue(message.DpnidOldHost, out var oldHost)
			&& oldHost.Connection is not null
		)
		{
			session.HostClaim = command;
			return Current;
		}

		// Only the peer that we would elect ourselves may take over.
		return session.ElectHost() == newHost
			? session.AcceptHost(command.Connection, newHost, message.DpnidOldHost, HostConnection)
			: Current;
	}

	/// <summary>
	/// Returns the operations that the new host is missing (step 4).
	/// </summary>
	SessionState ISessionTransition<Received<CoreMessage.RequestNameTableOperations>>.Execute(
		SessionActor session,
		Received<CoreMessage.RequestNameTableOperations> command
	)
	{
		if (command.Connection == HostConnection)
			SessionActor.SendCore(
				HostConnection,
				new CoreMessage.AckNameTableOperations
				{
					Entries = [.. session.GetOperationsAfter(command.Message.Version)],
				}
			);
		return Current;
	}

	/// <summary>
	/// The new host is fully in charge (step 6); its missed operations have already arrived.
	/// </summary>
	SessionState ISessionTransition<Received<CoreMessage.HostMigrateComplete>>.Execute(
		SessionActor session,
		Received<CoreMessage.HostMigrateComplete> command
	) => Current;
}
