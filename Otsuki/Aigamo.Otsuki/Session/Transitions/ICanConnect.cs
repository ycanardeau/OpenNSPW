using Aigamo.Results;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// Starts joining a session by connecting to its host ([MC-DPL8CS] section 3.1.5.2, step 1).
/// </summary>
internal interface ICanConnect : ISessionTransition<SessionMessage.Connect>
{
	private static SessionState Connect(SessionActor session, SessionMessage.Connect command)
	{
		session.Application = command.Application;
		session.ConnectData = command.ConnectData;
		var hostConnection = session.Connect(command.HostEndPoint);
		session.JoinTimer.Start(session.Options.JoinTimeout);
		session.Publish();
		command.Completion.TrySetResult();
		return new SessionState.Connecting(hostConnection);
	}

	SessionState ISessionTransition<SessionMessage.Connect>.Execute(
		SessionActor session,
		SessionMessage.Connect command
	) =>
		session
			.StartEndpoint(command.LocalEndPoint)
			.Fold(
				onOk: _ => Connect(session, command),
				onError: exception =>
				{
					command.Completion.TrySetException(exception);
					return Current;
				}
			);
}
