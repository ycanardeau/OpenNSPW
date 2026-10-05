using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// Destroys a player whose connection to the host ended ([MC-DPL8CS] section 3.2.1).
/// </summary>
internal interface ICanLosePlayers
	: ISessionTransition<SessionMessage.ConnectionDisconnected>,
		IHasHostingData
{
	SessionState ISessionTransition<SessionMessage.ConnectionDisconnected>.Execute(
		SessionActor session,
		SessionMessage.ConnectionDisconnected command
	) =>
		session.Unbind(command.Connection) is { } player && session.Players.ContainsKey(player.Id)
			? session.DestroyPlayer(
				this,
				player,
				command.Result == ResultCode.Success
					? DestroyPlayerFlags.Normal
					: DestroyPlayerFlags.ConnectionLost
			)
			: Current;
}
