using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// Leaves the session, destroying every player for the application.
/// </summary>
internal interface ICanLeave : ISessionTransition<SessionMessage.Close>
{
	SessionState ISessionTransition<SessionMessage.Close>.Execute(
		SessionActor session,
		SessionMessage.Close command
	)
	{
		foreach (var player in session.Players.Values.Where(p => p.Created).OrderBy(p => p.IsLocal))
			session.Emit(new PeerEvent.PlayerDestroyed(player.Id, DestroyPlayerFlags.Normal));
		return session.BeginClosing(command.Completion);
	}
}
