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
		SessionActor session,
		SessionMessage.Close command
	) => new SessionState.Closing(Completions.Add(command.Completion));

	SessionState ISessionTransition<SessionMessage.ConnectionConnectFailed>.Execute(
		SessionActor session,
		SessionMessage.ConnectionConnectFailed command
	) => session.Connections.Count == 0 ? session.CompleteClose(Completions) : Current;

	SessionState ISessionTransition<SessionMessage.ConnectionDisconnected>.Execute(
		SessionActor session,
		SessionMessage.ConnectionDisconnected command
	) => session.Connections.Count == 0 ? session.CompleteClose(Completions) : Current;

	SessionState ISessionTransition<SessionMessage.CloseTimedOut>.Execute(
		SessionActor session,
		SessionMessage.CloseTimedOut command
	) => session.CompleteClose(Completions);
}
