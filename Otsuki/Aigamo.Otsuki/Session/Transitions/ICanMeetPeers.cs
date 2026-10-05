using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// Connects to the peers that the host instructed us to connect to ([MC-DPL8CS] section 3.1.5.2, steps 8 and 9).
/// </summary>
internal interface ICanMeetPeers
	: ISessionTransition<SessionMessage.ConnectionConnected>,
		ISessionTransition<SessionMessage.ConnectionConnectFailed>,
		IHasHostConnection
{
	/// <summary>
	/// Identifies ourselves on a connection we opened. Inbound connections identify themselves instead.
	/// </summary>
	SessionState ISessionTransition<SessionMessage.ConnectionConnected>.Execute(
		SessionActor session,
		SessionMessage.ConnectionConnected command
	)
	{
		if (
			command.Connection.IsOutbound
			&& session.PlayersByConnection.TryGetValue(command.Connection, out var player)
		)
		{
			SessionActor.SendCore(
				command.Connection,
				new CoreMessage.SendPlayerDpnid { Dpnid = session.LocalPlayer }
			);
			session.MarkCreated(player);
		}
		return Current;
	}

	SessionState ISessionTransition<SessionMessage.ConnectionConnectFailed>.Execute(
		SessionActor session,
		SessionMessage.ConnectionConnectFailed command
	)
	{
		if (session.Unbind(command.Connection) is { } player)
			SessionActor.SendCore(
				HostConnection,
				new CoreMessage.InstructedConnectFailed { Dpnid = player.Id }
			);
		return Current;
	}
}
