using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// Reacts to connections ending as a non-host peer in the session. Losing the host migrates it when the session
/// has <see cref="SessionFlags.MigrateHost"/> ([MC-DPL8CS] section 3.2.5.4), and ends the session otherwise.
/// </summary>
internal interface ICanLoseHost
	: ISessionTransition<Received<CoreMessage.TerminateSession>>,
		ISessionTransition<SessionMessage.ConnectionDisconnected>,
		IHasHostConnection
{
	/// <summary>
	/// The host removed us from the session ([MC-DPL8CS] section 3.2.5.2, step 1).
	/// </summary>
	SessionState ISessionTransition<Received<CoreMessage.TerminateSession>>.Execute(
		SessionActor session,
		Received<CoreMessage.TerminateSession> command
	) =>
		command.Connection == HostConnection
			? session.Terminate(ResultCode.HostTerminatedSession)
			: Current;

	SessionState ISessionTransition<SessionMessage.ConnectionDisconnected>.Execute(
		SessionActor session,
		SessionMessage.ConnectionDisconnected command
	)
	{
		if (session.Unbind(command.Connection) is not { } player)
			return Current;

		// The host we follow may be one that has not announced itself yet, after an earlier migration.
		if (player.IsHost || command.Connection == HostConnection)
			return
				session.Application.Flags.HasFlag(SessionFlags.MigrateHost)
				&& !session.IsClientServer
				? session.MigrateFrom(player, command.Result)
				: session.Terminate(
					command.Result == ResultCode.Success
						? ResultCode.HostTerminatedSession
						: ResultCode.ConnectionLost
				);

		// Ask the host whether the peer is really gone ([MC-DPL8CS] section 3.2.5.3, step 1).
		if (command.Result != ResultCode.Success)
			SessionActor.SendCore(
				HostConnection,
				new CoreMessage.RequestIntegrityCheck { DpnidTarget = player.Id }
			);
		return Current;
	}
}
