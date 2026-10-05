using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// Waits until the host instructed the established peers to connect and all of them identified themselves
/// ([MC-DPL8CS] section 3.1, figure 3).
/// </summary>
internal interface ICanCompleteJoining
	: ISessionTransition<Received<CoreMessage.InstructConnect>>,
		ISessionTransition<Received<CoreMessage.SendPlayerDpnid>>,
		ISessionTransition<Received<CoreMessage.DestroyPlayer>>,
		IHasJoinProgress
{
	/// <summary>
	/// For the joining peer itself, the message only synchronizes the name table version.
	/// </summary>
	SessionState ISessionTransition<Received<CoreMessage.InstructConnect>>.Execute(
		SessionActor session,
		Received<CoreMessage.InstructConnect> command
	)
	{
		if (command.Connection != HostConnection)
			return Current;

		session.Version = command.Message.Version;
		session.ReportNameTableVersion(HostConnection);
		if (command.Message.Dpnid == session.LocalPlayer)
			return session.CompleteJoin(ToJoining() with { InstructConnectReceived = true });

		session.ConnectToNewPlayer(HostConnection, command.Message.Dpnid);
		return Current;
	}

	SessionState ISessionTransition<Received<CoreMessage.SendPlayerDpnid>>.Execute(
		SessionActor session,
		Received<CoreMessage.SendPlayerDpnid> command
	) =>
		session.IdentifyPeer(command.Connection, command.Message.Dpnid) is { } player
			? session.CompleteJoin(
				ToJoining() with
				{
					AwaitingPlayers = AwaitingPlayers.Remove(player.Id),
				}
			)
			: Current;

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
		return session.CompleteJoin(
			ToJoining() with
			{
				AwaitingPlayers = AwaitingPlayers.Remove(message.DpnidLeaving),
			}
		);
	}
}
