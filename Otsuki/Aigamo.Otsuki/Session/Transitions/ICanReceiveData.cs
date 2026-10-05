namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// Passes application data to the application ([MC-DPL8CS] section 3.3.5.1).
/// </summary>
internal interface ICanReceiveData : ISessionTransition<SessionMessage.ConnectionReceived>
{
	SessionState ISessionTransition<SessionMessage.ConnectionReceived>.Execute(
		SessionActor session,
		SessionMessage.ConnectionReceived command
	)
	{
		session.ReceiveUserData(command.Connection, command.Payload);
		return Current;
	}
}
