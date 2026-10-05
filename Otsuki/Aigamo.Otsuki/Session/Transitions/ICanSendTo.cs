namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// Sends application data to players in the session.
/// </summary>
internal interface ICanSendTo : ISessionTransition<SessionMessage.SendTo>
{
	SessionState ISessionTransition<SessionMessage.SendTo>.Execute(
		SessionActor session,
		SessionMessage.SendTo command
	)
	{
		session.SendTo(command);
		return Current;
	}
}
