using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// Ends a connection attempt before the host sent DN_SEND_CONNECT_INFO.
/// </summary>
internal interface ICanFailConnecting
	: ISessionTransition<Received<CoreMessage.ConnectFailed>>,
		ISessionTransition<Received<CoreMessage.TerminateSession>>,
		ISessionTransition<SessionMessage.ConnectionConnectFailed>,
		ISessionTransition<SessionMessage.ConnectionDisconnected>,
		ISessionTransition<SessionMessage.JoinTimedOut>,
		ISessionTransition<SessionMessage.Close>,
		IHasHostConnection
{
	/// <summary>
	/// The host rejected us ([MC-DPL8CS] section 3.1.5.2, step 3).
	/// </summary>
	SessionState ISessionTransition<Received<CoreMessage.ConnectFailed>>.Execute(
		SessionCore session,
		Received<CoreMessage.ConnectFailed> command
	) =>
		command.Connection == HostConnection
			? session.FailConnect(command.Message.ResultCode, [.. command.Message.Reply])
			: Current;

	SessionState ISessionTransition<Received<CoreMessage.TerminateSession>>.Execute(
		SessionCore session,
		Received<CoreMessage.TerminateSession> command
	) =>
		command.Connection == HostConnection
			? session.FailConnect(ResultCode.HostTerminatedSession, [])
			: Current;

	SessionState ISessionTransition<SessionMessage.ConnectionConnectFailed>.Execute(
		SessionCore session,
		SessionMessage.ConnectionConnectFailed command
	) => command.Connection == HostConnection ? session.FailConnect(command.Result, []) : Current;

	SessionState ISessionTransition<SessionMessage.ConnectionDisconnected>.Execute(
		SessionCore session,
		SessionMessage.ConnectionDisconnected command
	) =>
		command.Connection == HostConnection
			? session.FailConnect(
				command.Result == ResultCode.Success
					? ResultCode.HostTerminatedSession
					: ResultCode.ConnectionLost,
				[]
			)
			: Current;

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
