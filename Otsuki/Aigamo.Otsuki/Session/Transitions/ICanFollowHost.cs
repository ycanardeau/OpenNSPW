using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// Applies the host's name table operations and answers its integrity checks, as a non-host peer.
/// </summary>
internal interface ICanFollowHost
	: ISessionTransition<Received<CoreMessage.AddPlayer>>,
		ISessionTransition<Received<CoreMessage.IntegrityCheck>>,
		ISessionTransition<Received<CoreMessage.UpdateInfo>>,
		ISessionTransition<Received<CoreMessage.CreateGroup>>,
		ISessionTransition<Received<CoreMessage.DestroyGroup>>,
		ISessionTransition<Received<CoreMessage.AddPlayerToGroup>>,
		ISessionTransition<Received<CoreMessage.DeletePlayerFromGroup>>,
		IHasHostConnection
{
	/// <summary>
	/// While joining, groups are announced only once the join completes, with the players.
	/// </summary>
	private bool IsInSession => Current is SessionState.Connected;

	/// <summary>
	/// Takes the name table version of a change from the host, reports it, and completes our request for it.
	/// </summary>
	private SessionState Follow(
		SessionActor session,
		int version,
		Dpnid requesting,
		int context,
		Dpnid result = default
	)
	{
		session.Version = version;
		session.ReportNameTableVersion(HostConnection);
		session.CompleteRequest(requesting, context, result);
		return Current;
	}

	/// <summary>
	/// A player's name or data changed ([MC-DPL8CS] section 3.5).
	/// </summary>
	SessionState ISessionTransition<Received<CoreMessage.UpdateInfo>>.Execute(
		SessionActor session,
		Received<CoreMessage.UpdateInfo> command
	)
	{
		if (command.Connection != HostConnection)
			return Current;

		var message = command.Message;
		session.UpdatePlayerInfo(message.Dpnid, message.InfoFlags, message.Name, [.. message.Data]);
		return Follow(session, message.Version, message.DpnidRequesting, message.PacketContext);
	}

	/// <summary>
	/// The host created a group ([MC-DPL8CS] section 3.4.5.2, step 2).
	/// </summary>
	SessionState ISessionTransition<Received<CoreMessage.CreateGroup>>.Execute(
		SessionActor session,
		Received<CoreMessage.CreateGroup> command
	)
	{
		if (command.Connection != HostConnection || !command.Message.Group.Group)
			return Current;

		var group = Group.FromNameTableEntry(command.Message.Group);
		session.AddGroup(group, announce: IsInSession);
		return Follow(
			session,
			group.Version,
			command.Message.DpnidRequesting,
			command.Message.PacketContext,
			group.Id
		);
	}

	SessionState ISessionTransition<Received<CoreMessage.DestroyGroup>>.Execute(
		SessionActor session,
		Received<CoreMessage.DestroyGroup> command
	)
	{
		if (command.Connection != HostConnection)
			return Current;

		var message = command.Message;
		session.RemoveGroup(message.DpnidGroup, session.GetDestroyReason(message.DpnidGroup));
		return Follow(session, message.Version, message.DpnidRequesting, message.PacketContext);
	}

	SessionState ISessionTransition<Received<CoreMessage.AddPlayerToGroup>>.Execute(
		SessionActor session,
		Received<CoreMessage.AddPlayerToGroup> command
	)
	{
		if (command.Connection != HostConnection)
			return Current;

		var message = command.Message;
		session.AddMember(message.DpnidGroup, message.DpnidPlayer, message.Version);
		return Follow(session, message.Version, message.DpnidRequesting, message.PacketContext);
	}

	SessionState ISessionTransition<Received<CoreMessage.DeletePlayerFromGroup>>.Execute(
		SessionActor session,
		Received<CoreMessage.DeletePlayerFromGroup> command
	)
	{
		if (command.Connection != HostConnection)
			return Current;

		var message = command.Message;
		session.RemoveMember(message.DpnidGroup, message.DpnidPlayer);
		return Follow(session, message.Version, message.DpnidRequesting, message.PacketContext);
	}

	/// <summary>
	/// Adds a joining peer, which will connect or be connected to later ([MC-DPL8CS] section 3.1.5.2, step 5).
	/// </summary>
	SessionState ISessionTransition<Received<CoreMessage.AddPlayer>>.Execute(
		SessionActor session,
		Received<CoreMessage.AddPlayer> command
	)
	{
		if (command.Connection != HostConnection)
			return Current;

		var message = command.Message;
		session.Version = message.Version;
		session.Players.TryAdd(
			message.Dpnid,
			new Player
			{
				Id = message.Dpnid,
				Name = message.Name,
				Data = [.. message.Data],
				Url = new DirectPlayUrl(message.Url),
				IsHost = message.Host,
				Version = message.Version,
				DnetVersion = message.DnetClientVersion,
			}
		);
		session.ReportNameTableVersion(HostConnection);
		return Current;
	}

	/// <summary>
	/// Confirms that we are still in the session ([MC-DPL8CS] section 3.2.5.3, step 3).
	/// </summary>
	SessionState ISessionTransition<Received<CoreMessage.IntegrityCheck>>.Execute(
		SessionActor session,
		Received<CoreMessage.IntegrityCheck> command
	)
	{
		if (command.Connection == HostConnection)
			SessionActor.SendCore(
				HostConnection,
				new CoreMessage.IntegrityCheckResponse
				{
					DpnidRequesting = command.Message.DpnidRequesting,
				}
			);
		return Current;
	}
}
