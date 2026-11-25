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
		SessionCore session,
		SessionMessage.ConnectionDisconnected command
	)
	{
		if (
			session.Unbind(command.Connection) is not { } player
			|| !session.Players.ContainsKey(player.Id)
		)
			return Current;

		// A peer that leaves during migration is no longer waited for, nor asked for operations, having no connection
		// (SessionCore.ContinueMigration). It is destroyed when migration finishes, after the operations that came
		// before it.
		return Migration is null
			? session.DestroyPlayer(
				this,
				player,
				command.Result == ResultCode.Success
					? DestroyPlayerFlags.Normal
					: DestroyPlayerFlags.ConnectionLost
			)
			: Current;
	}
}
