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
	)
	{
		if (
			session.Unbind(command.Connection) is not { } player
			|| !session.Players.ContainsKey(player.Id)
		)
			return Current;

		var hosting = session.DestroyPlayer(
			this,
			player,
			command.Result == ResultCode.Success
				? DestroyPlayerFlags.Normal
				: DestroyPlayerFlags.ConnectionLost
		);
		if (hosting.Migration is not { } migration)
			return hosting;

		// A peer that leaves during migration is no longer waited for, nor asked for operations.
		return session.ContinueMigration(
			hosting with
			{
				Migration = migration with
				{
					Awaiting = migration.Awaiting.Remove(player.Id),
					Reported = migration.Reported.Remove(player.Id),
					RequestedFrom =
						migration.RequestedFrom == player.Id ? null : migration.RequestedFrom,
				},
			}
		);
	}
}
