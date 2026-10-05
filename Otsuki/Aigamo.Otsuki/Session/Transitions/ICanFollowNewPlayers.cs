using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// Connects to the peers that join after us and removes the ones that leave, as instructed by the host.
/// </summary>
internal interface ICanFollowNewPlayers
	: ISessionTransition<Received<CoreMessage.InstructConnect>>,
		ISessionTransition<Received<CoreMessage.SendPlayerDpnid>>,
		ISessionTransition<Received<CoreMessage.DestroyPlayer>>,
		IHasHostConnection
{
	SessionState ISessionTransition<Received<CoreMessage.InstructConnect>>.Execute(
		SessionActor session,
		Received<CoreMessage.InstructConnect> command
	)
	{
		if (command.Connection != HostConnection)
			return Current;

		session.Version = command.Message.Version;
		session.ReportNameTableVersion(HostConnection);
		if (command.Message.Dpnid != session.LocalPlayer)
			session.ConnectToNewPlayer(HostConnection, command.Message.Dpnid);
		return Current;
	}

	SessionState ISessionTransition<Received<CoreMessage.SendPlayerDpnid>>.Execute(
		SessionActor session,
		Received<CoreMessage.SendPlayerDpnid> command
	)
	{
		if (session.IdentifyPeer(command.Connection, command.Message.Dpnid) is { } player)
			session.MarkCreated(player);
		return Current;
	}

	/// <summary>
	/// Removes the player that the host destroyed ([MC-DPL8CS] section 3.2.5.2, step 2).
	/// </summary>
	SessionState ISessionTransition<Received<CoreMessage.DestroyPlayer>>.Execute(
		SessionActor session,
		Received<CoreMessage.DestroyPlayer> command
	)
	{
		if (command.Connection != HostConnection)
			return Current;

		var message = command.Message;
		session.Version = message.Version;
		if (message.DpnidLeaving == session.LocalPlayer)
			return session.Terminate(ResultCode.HostTerminatedSession);

		session.ForgetPlayer(message.DpnidLeaving, message.Reason);
		session.ReportNameTableVersion(HostConnection);
		return Current;
	}
}
