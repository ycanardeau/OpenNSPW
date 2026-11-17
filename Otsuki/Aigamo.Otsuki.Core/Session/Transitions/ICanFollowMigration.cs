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
		SessionCore session,
		Received<CoreMessage.HostMigrate> command
	)
	{
		var message = command.Message;
		if (
			!session.PlayersByConnection.TryGetValue(command.Connection, out var newHost)
			|| newHost.Id != message.DpnidNewHost
		)
			return Current;

		// While we still hear from the old host, or from the host we follow, the claim waits: the peer may merely have
		// lost its own link to it. The old host that the peer names may be one it elected and lost since, while the host
		// we follow has not left us yet: following the claim then would leave us two hosts.
		if (
			session.Players.TryGetValue(message.DpnidOldHost, out var oldHost)
				&& oldHost.Connection is not null
			|| command.Connection != HostConnection && session.Connections.Contains(HostConnection)
		)
		{
			session.KeepClaim(command);
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
		SessionCore session,
		Received<CoreMessage.RequestNameTableOperations> command
	)
	{
		if (command.Connection == HostConnection)
			session.SendCore(
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
		SessionCore session,
		Received<CoreMessage.HostMigrateComplete> command
	) => Current;
}
