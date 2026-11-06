using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// Ends a join that is in the name table but not complete yet.
/// </summary>
internal interface ICanFailJoining
	: ISessionTransition<Received<CoreMessage.ConnectAttemptFailed>>,
		ISessionTransition<Received<CoreMessage.TerminateSession>>,
		ISessionTransition<SessionMessage.ConnectionDisconnected>,
		ISessionTransition<SessionMessage.JoinTimedOut>,
		ISessionTransition<SessionMessage.Close>,
		IHasHostConnection
{
	/// <summary>
	/// An established peer could not connect to us ([MC-DPL8CS] section 3.1.5.2, step 10).
	/// </summary>
	SessionState ISessionTransition<Received<CoreMessage.ConnectAttemptFailed>>.Execute(
		SessionCore session,
		Received<CoreMessage.ConnectAttemptFailed> command
	) =>
		command.Connection == HostConnection
			? session.FailConnect(ResultCode.PlayerNotReachable, [])
			: Current;

	SessionState ISessionTransition<Received<CoreMessage.TerminateSession>>.Execute(
		SessionCore session,
		Received<CoreMessage.TerminateSession> command
	) =>
		command.Connection == HostConnection
			? session.FailConnect(ResultCode.HostTerminatedSession, [])
			: Current;

	SessionState ISessionTransition<SessionMessage.ConnectionDisconnected>.Execute(
		SessionCore session,
		SessionMessage.ConnectionDisconnected command
	)
	{
		if (command.Connection == HostConnection)
			return session.FailConnect(
				command.Result == ResultCode.Success
					? ResultCode.HostTerminatedSession
					: ResultCode.ConnectionLost,
				[]
			);

		if (
			session.Unbind(command.Connection) is { } player
			&& command.Result != ResultCode.Success
		)
			session.SendCore(
				HostConnection,
				new CoreMessage.RequestIntegrityCheck { DpnidTarget = player.Id }
			);
		return Current;
	}

	SessionState ISessionTransition<SessionMessage.JoinTimedOut>.Execute(
		SessionCore session,
		SessionMessage.JoinTimedOut command
	) => session.FailConnect(ResultCode.TimedOut, []);

	SessionState ISessionTransition<SessionMessage.Close>.Execute(
		SessionCore session,
		SessionMessage.Close command
	)
	{
		session.Emit(new PeerEvent.ConnectCompleted(ResultCode.UserCancel, []));
		return session.BeginClosing(command.Completion);
	}
}
