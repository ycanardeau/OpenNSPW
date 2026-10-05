using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// Tracks the name table versions that peers report ([MC-DPL8CS] section 2.2.6).
/// </summary>
internal interface ICanResynchronizeVersion
	: ISessionTransition<Received<CoreMessage.NameTableVersion>>,
		IHasHostingData
{
	SessionState ISessionTransition<Received<CoreMessage.NameTableVersion>>.Execute(
		SessionActor session,
		Received<CoreMessage.NameTableVersion> command
	)
	{
		if (!session.PlayersByConnection.TryGetValue(command.Connection, out var player))
			return Current;

		player.ReportedVersion = command.Message.Version;
		return session.ResynchronizeVersion(this);
	}
}
