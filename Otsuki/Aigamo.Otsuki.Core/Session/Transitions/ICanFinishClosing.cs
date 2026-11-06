namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// Finishes closing once every connection has ended, an established one by disconnecting and one still connecting by
/// failing to (closing while joining cancels the attempt), or the close time-out elapsed.
/// </summary>
internal interface ICanFinishClosing
	: ISessionTransition<SessionMessage.Close>,
		ISessionTransition<SessionMessage.ConnectionConnectFailed>,
		ISessionTransition<SessionMessage.ConnectionDisconnected>,
		ISessionTransition<SessionMessage.CloseTimedOut>,
		IHasCloseCompletions
{
	SessionState ISessionTransition<SessionMessage.Close>.Execute(
		SessionCore session,
		SessionMessage.Close command
	) => new SessionState.Closing(Completions.Add(command.Completion));

	SessionState ISessionTransition<SessionMessage.ConnectionConnectFailed>.Execute(
		SessionCore session,
		SessionMessage.ConnectionConnectFailed command
	) => session.Connections.Count == 0 ? session.CompleteClose(Completions) : Current;

	SessionState ISessionTransition<SessionMessage.ConnectionDisconnected>.Execute(
		SessionCore session,
		SessionMessage.ConnectionDisconnected command
	) => session.Connections.Count == 0 ? session.CompleteClose(Completions) : Current;

	SessionState ISessionTransition<SessionMessage.CloseTimedOut>.Execute(
		SessionCore session,
		SessionMessage.CloseTimedOut command
	) => session.CompleteClose(Completions);
}
