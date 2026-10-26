using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Reliable;
using Aigamo.Results;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// The host's side of a peer joining the session ([MC-DPL8CS] section 3.1.5.2).
/// </summary>
internal interface ICanAdmitPlayers
	: ISessionTransition<SessionMessage.ConnectionConnected>,
		ISessionTransition<Received<CoreMessage.PlayerConnectInfo>>,
		ISessionTransition<Received<CoreMessage.AckConnectInfo>>,
		ISessionTransition<Received<CoreMessage.InstructedConnectFailed>>,
		ISessionTransition<SessionMessage.PlayerJoinTimedOut>,
		IHasHostingData
{
	/// <summary>
	/// Checks whether a peer may join (step 2). The error is sent back in DN_CONNECT_FAILED.
	/// </summary>
	private static Result<Unit, ResultCode> ValidateJoin(
		SessionActor session,
		CoreMessage.PlayerConnectInfo message
	)
	{
		var application = session.Application;
		if (message.Flags != (session.IsClientServer ? ObjectType.Client : ObjectType.Peer))
			return ResultCode.InvalidInterface;

		if (message.GuidApplication != application.GuidApplication)
			return ResultCode.InvalidApplication;

		if (message.GuidInstance != Guid.Empty && message.GuidInstance != application.GuidInstance)
			return ResultCode.InvalidInstance;

		if (
			application.Flags.HasFlag(SessionFlags.RequirePassword)
			&& message.Password != application.Password
		)
			return ResultCode.InvalidPassword;

		if (application.MaxPlayers > 0 && session.Players.Count >= application.MaxPlayers)
			return ResultCode.SessionFull;

		return Unit.Default;
	}

	private SessionState RejectJoin(Connection connection, ResultCode result)
	{
		SessionActor.SendCore(connection, new CoreMessage.ConnectFailed { ResultCode = result });
		connection.Post(new ConnectionMessage.Disconnect());
		return Current;
	}

	/// <summary>
	/// The name table a new player gets: everyone in a peer-to-peer session, but only the server and itself for a
	/// client.
	/// </summary>
	private static IEnumerable<NameTableEntryInfo> GetNameTableEntries(
		SessionActor session,
		Player player
	) =>
		session.IsClientServer
			?
			[
				session.Players[session.LocalPlayer].ToNameTableEntryInfo(),
				player.ToNameTableEntryInfo(),
			]
			: session
				.Players.Values.Select(p => p.ToNameTableEntryInfo())
				.Concat(session.Groups.Values.Select(g => g.ToNameTableEntryInfo()))
				.OrderBy(e => e.Version);

	private static IEnumerable<NameTableMembershipInfo> GetNameTableMemberships(
		SessionActor session
	) =>
		session.IsClientServer
			? []
			: session
				.Groups.Values.SelectMany(g =>
					g.Members.Select(m => new NameTableMembershipInfo
					{
						DpnidPlayer = m.Key,
						DpnidGroup = g.Id,
						Version = m.Value,
					})
				)
				.OrderBy(m => m.Version);

	/// <summary>
	/// Adds a validated peer to the name table and tells everyone about it (steps 4 and 5).
	/// </summary>
	private SessionState AdmitPlayer(
		SessionActor session,
		Connection connection,
		CoreMessage.PlayerConnectInfo message
	)
	{
		var application = session.Application;
		session.Version++;
		var player = new Player
		{
			Id = session.CreateDpnid(NextIndex, session.Version),
			Name = message.Name,
			Data = [.. message.Data],
			// The new peer listens on the address it connected from, as observed by the host.
			Url = DirectPlayUrl.FromEndPoint(connection.RemoteEndPoint),
			Version = session.Version,
			DnetVersion = message.DnetVersion,
			Role = session.IsClientServer ? NameTableEntryFlags.Client : NameTableEntryFlags.Peer,
		};
		session.Players.Add(player.Id, player);
		session.Bind(player, connection);

		SessionActor.SendCore(
			connection,
			new CoreMessage.SendConnectInfo
			{
				Flags = application.Flags,
				MaxPlayers = application.MaxPlayers,
				CurrentPlayers = session.Players.Count,
				GuidInstance = application.GuidInstance,
				GuidApplication = application.GuidApplication,
				Dpnid = player.Id,
				Version = session.Version,
				NameTableEntries = [.. GetNameTableEntries(session, player)],
				NameTableMemberships = [.. GetNameTableMemberships(session)],
				ApplicationReservedData = application.ApplicationReservedData,
				Password = application.Flags.HasFlag(SessionFlags.RequirePassword)
					? application.Password
					: string.Empty,
				SessionName = application.SessionName,
			}
		);

		// Peers that are still joining need the new entry as well, so that they connect to each other. Clients never
		// hear of each other.
		if (!session.IsClientServer)
		{
			var addPlayer = player.ToAddPlayerMessage();
			session.Record(addPlayer);
			foreach (var peer in session.ConnectedPeers.Where(p => p != player))
				SessionActor.SendCore(peer.Connection!, addPlayer);
		}

		player.JoinTimer = session.Schedule(
			session.Options.JoinTimeout,
			new SessionMessage.PlayerJoinTimedOut(player.Id)
		);
		return ToHosting() with { NextIndex = NextIndex + 1 };
	}

	/// <summary>
	/// An inbound connection identifies itself with DN_INTERNAL_MESSAGE_PLAYER_CONNECT_INFO.
	/// </summary>
	SessionState ISessionTransition<SessionMessage.ConnectionConnected>.Execute(
		SessionActor session,
		SessionMessage.ConnectionConnected command
	) => Current;

	SessionState ISessionTransition<Received<CoreMessage.PlayerConnectInfo>>.Execute(
		SessionActor session,
		Received<CoreMessage.PlayerConnectInfo> command
	) =>
		session.PlayersByConnection.ContainsKey(command.Connection)
			? Current
			: ValidateJoin(session, command.Message)
				.Fold(
					onOk: _ => AdmitPlayer(session, command.Connection, command.Message),
					onError: result => RejectJoin(command.Connection, result)
				);

	/// <summary>
	/// Instructs every peer to connect to the new one (step 7).
	/// </summary>
	SessionState ISessionTransition<Received<CoreMessage.AckConnectInfo>>.Execute(
		SessionActor session,
		Received<CoreMessage.AckConnectInfo> command
	)
	{
		if (
			!session.PlayersByConnection.TryGetValue(command.Connection, out var player)
			|| player.Created
		)
			return Current;

		player.JoinTimer?.Dispose();
		player.JoinTimer = null;

		// A client is in once it acknowledged; there are no other peers to connect to it ([MC-DPL8CS] section 3.1).
		if (session.IsClientServer)
		{
			session.MarkCreated(player);
			return Current;
		}

		session.Version++;
		var instructConnect = new CoreMessage.InstructConnect
		{
			Dpnid = player.Id,
			Version = session.Version,
		};
		session.Announce(instructConnect);
		session.MarkCreated(player);
		return Current;
	}

	/// <summary>
	/// An established peer could not connect to the new one, so the join fails (steps 10 and 11).
	/// </summary>
	SessionState ISessionTransition<Received<CoreMessage.InstructedConnectFailed>>.Execute(
		SessionActor session,
		Received<CoreMessage.InstructedConnectFailed> command
	)
	{
		if (!session.Players.TryGetValue(command.Message.Dpnid, out var player))
			return Current;

		if (player.Connection is not null)
			SessionActor.SendCore(
				player.Connection,
				new CoreMessage.ConnectAttemptFailed { Dpnid = command.Message.Dpnid }
			);
		return session.DestroyPlayer(this, player, DestroyPlayerFlags.ConnectionLost);
	}

	SessionState ISessionTransition<SessionMessage.PlayerJoinTimedOut>.Execute(
		SessionActor session,
		SessionMessage.PlayerJoinTimedOut command
	) =>
		session.Players.TryGetValue(command.Player, out var player) && !player.Created
			? session.DestroyPlayer(this, player, DestroyPlayerFlags.ConnectionLost)
			: Current;
}
