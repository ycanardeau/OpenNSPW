namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// Finishes closing once every connection has ended or the close time-out elapsed.
/// </summary>
internal interface ICanFinishClosing
	: ISessionTransition<SessionMessage.Close>,
		ISessionTransition<SessionMessage.ConnectionDisconnected>,
		ISessionTransition<SessionMessage.CloseTimedOut>,
		IHasCloseCompletions
{
	SessionState ISessionTransition<SessionMessage.Close>.Execute(
		SessionActor session,
		SessionMessage.Close command
	) => new SessionState.Closing(Completions.Add(command.Completion));

	SessionState ISessionTransition<SessionMessage.ConnectionDisconnected>.Execute(
		SessionActor session,
		SessionMessage.ConnectionDisconnected command
	) => session.Connections.Count == 0 ? session.CompleteClose(Completions) : Current;

	SessionState ISessionTransition<SessionMessage.CloseTimedOut>.Execute(
		SessionActor session,
		SessionMessage.CloseTimedOut command
	) => session.CompleteClose(Completions);
}
