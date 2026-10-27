using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Threading.Channels;
using Aigamo.Otsuki.Actors;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Messages.Enumeration;
using Aigamo.Otsuki.Messages.Reliable;
using Aigamo.Otsuki.Reliable;
using Aigamo.Otsuki.Session.Transitions;
using Aigamo.Results;

namespace Aigamo.Otsuki.Session;

/// <summary>
/// A session of the DirectPlay 8 core protocol ([MC-DPL8CS]), peer-to-peer or client/server. The protocol logic
/// lives in the transitions that each <see cref="SessionState"/> implements; this actor owns the state and the name
/// table, with its groups, and runs the transition for each message.
/// </summary>
internal sealed class SessionActor : Actor<SessionMessage>, IConnectionObserver
{
	private sealed class PendingSend(int remaining, SendFlags flags)
	{
		public int Remaining { get; set; } = remaining;
		public SendFlags Flags { get; } = flags;
		public ResultCode Result { get; set; } = ResultCode.Success;
	}

	/// <summary>
	/// A name table change that the local peer asked the host for, waiting for the host's broadcast.
	/// </summary>
	private sealed record PendingRequest(
		Action<Dpnid> OnSuccess,
		Action<Exception> OnFailure,
		ITimer Timer
	);

	private readonly ChannelWriter<PeerEvent> _events;
	private readonly Action<SessionSnapshot> _publish;
	private readonly Dictionary<long, PendingSend> _sends = [];
	private readonly Dictionary<long, long> _sendHandles = [];
	private SessionState _state = new SessionState.Idle();
	private ReliableEndpoint? _endpoint;
	private long _nextSendId;
	private readonly Dictionary<int, PendingRequest> _requests = [];
	private int _nextContext;

	/// <summary>
	/// Commands that wait for the state to allow them: see <see cref="MustWait"/>.
	/// </summary>
	private readonly List<Action> _deferred = [];

	public PeerOptions Options { get; }

	public ActorTimer<SessionMessage> JoinTimer { get; }

	public ActorTimer<SessionMessage> CloseTimer { get; }

	public ActorTimer<SessionMessage> MigrationTimer { get; }

	/// <summary>
	/// The name table ([MC-DPL8CS] section 2.2.6).
	/// </summary>
	public Dictionary<Dpnid, Player> Players { get; } = [];

	public Dictionary<Connection, Player> PlayersByConnection { get; } = [];

	/// <summary>
	/// The groups of the name table.
	/// </summary>
	public Dictionary<Dpnid, Group> Groups { get; } = [];

	/// <summary>
	/// Every connection that has not reported its end yet.
	/// </summary>
	public HashSet<Connection> Connections { get; } = [];

	public string LocalName { get; set; } = string.Empty;

	public ImmutableArray<byte> LocalData { get; set; } = [];

	public ApplicationDescription Application { get; set; } = new();

	public ImmutableArray<byte> ConnectData { get; set; } = [];

	/// <summary>
	/// The game-specific data in host enumeration responses.
	/// </summary>
	public ImmutableArray<byte> EnumResponseData { get; private set; } = [];

	public Dpnid LocalPlayer { get; set; }

	/// <summary>
	/// The name table version.
	/// </summary>
	public int Version { get; set; }

	/// <summary>
	/// The name table operations since <see cref="ResynchronizedVersion"/>, which a new host may need.
	/// </summary>
	public List<INameTableOperation> NameTableLog { get; } = [];

	/// <summary>
	/// A DN_HOST_MIGRATE that arrived while we still heard from the old host, kept until we lose it too.
	/// </summary>
	public Received<CoreMessage.HostMigrate>? HostClaim { get; set; }

	/// <summary>
	/// The newest name table version that every peer has, from DN_RESYNC_VERSION.
	/// </summary>
	public int ResynchronizedVersion { get; private set; }

	public SessionActor(
		PeerOptions options,
		ChannelWriter<PeerEvent> events,
		Action<SessionSnapshot> publish
	)
	{
		Options = options;
		_events = events;
		_publish = publish;
		JoinTimer = new(
			options.TimeProvider,
			generation => new SessionMessage.JoinTimedOut(generation),
			Post
		);
		CloseTimer = new(
			options.TimeProvider,
			generation => new SessionMessage.CloseTimedOut(generation),
			Post
		);
		MigrationTimer = new(
			options.TimeProvider,
			generation => new SessionMessage.MigrationTimedOut(generation),
			Post
		);
		Start();
	}

	void IConnectionObserver.OnConnected(Connection connection) =>
		Post(new SessionMessage.ConnectionConnected(connection));

	void IConnectionObserver.OnConnectFailed(Connection connection, ResultCode result) =>
		Post(new SessionMessage.ConnectionConnectFailed(connection, result));

	void IConnectionObserver.OnReceived(Connection connection, byte[] payload, bool user1) =>
		Post(new SessionMessage.ConnectionReceived(connection, payload, user1));

	void IConnectionObserver.OnSendCompleted(
		Connection connection,
		long sendId,
		ResultCode result
	) => Post(new SessionMessage.ConnectionSendCompleted(connection, sendId, result));

	void IConnectionObserver.OnDisconnected(Connection connection, ResultCode result) =>
		Post(new SessionMessage.ConnectionDisconnected(connection, result));

	void IConnectionObserver.OnEnumQuery(
		IPEndPoint remoteEndPoint,
		EnumerationMessage.Query query
	) => Post(new SessionMessage.EnumQueryReceived(remoteEndPoint, query));

	public IPEndPoint LocalEndPoint => _endpoint!.LocalEndPoint;

	/// <summary>
	/// Whether the session is client/server (DPNSESSION_CLIENT_SERVER): the server knows every client, but a client
	/// knows only the server and itself, and talks only to the server.
	/// </summary>
	public bool IsClientServer => Application.Flags.HasFlag(SessionFlags.ClientServer);

	/// <summary>
	/// Remote players that we have a connection to.
	/// </summary>
	public IEnumerable<Player> ConnectedPeers =>
		Players.Values.Where(p => !p.IsLocal && p.Connection is not null);

	public static void SendCore(Connection connection, CoreMessage message) =>
		connection.Post(
			new ConnectionMessage.SendPayload(
				SendId: 0,
				CoreMessageSerializer.Default.Serialize(message),
				Reliable: true,
				User1: true
			)
		);

	public void Emit(PeerEvent peerEvent) => _events.TryWrite(peerEvent);

	/// <summary>
	/// Publishes a snapshot for other threads. Called before raising events that the snapshot must already reflect.
	/// </summary>
	public void Publish() =>
		_publish(
			new SessionSnapshot(
				Players
					.Values.Where(p => p.Created)
					.ToImmutableDictionary(p => p.Id, p => p.ToPlayerInfo()),
				_endpoint is null ? null : Application,
				_endpoint?.LocalEndPoint,
				LocalPlayer,
				Players.TryGetValue(LocalPlayer, out var local) && local.IsHost,
				Groups
					.Values.Where(g => g.Created)
					.ToImmutableDictionary(g => g.Id, g => g.ToGroupInfo())
			)
		);

	public Dpnid CreateDpnid(int index, int version) =>
		new(index, version, Application.GuidInstance);

	public ITimer Schedule(TimeSpan dueTime, SessionMessage message) =>
		Options.TimeProvider.CreateTimer(
			_ => Post(message),
			null,
			dueTime,
			Timeout.InfiniteTimeSpan
		);

	public Result<Unit, Exception> StartEndpoint(IPEndPoint localEndPoint)
	{
		try
		{
			_endpoint = new ReliableEndpoint(
				Options.TransportFactory(localEndPoint),
				Options.TimeProvider,
				Options.ReliableProfile,
				this
			);
			return Unit.Default;
		}
		catch (Exception e)
		{
			return e;
		}
	}

	/// <summary>
	/// Makes new inbound connections sign as the session's flags say ([MC-DPL8R] section 3.1.5.1.3). Outbound
	/// connections sign as their listener offers.
	/// </summary>
	public void SetSigning(SessionFlags flags) =>
		_endpoint?.Post(
			new ReliableEndpointMessage.SetSigning(
				flags.HasFlag(SessionFlags.FullSigned) ? PacketSigning.Full
				: flags.HasFlag(SessionFlags.FastSigned) ? PacketSigning.Fast
				: PacketSigning.None
			)
		);

	public Connection Connect(IPEndPoint remoteEndPoint)
	{
		var connection = _endpoint!.Connect(remoteEndPoint);
		Connections.Add(connection);
		return connection;
	}

	public void Bind(Player player, Connection connection)
	{
		player.Connection = connection;
		PlayersByConnection[connection] = player;
	}

	/// <summary>
	/// Forgets the player of a connection that ended.
	/// </summary>
	public Player? Unbind(Connection connection)
	{
		if (!PlayersByConnection.Remove(connection, out var player))
			return null;

		player.Connection = null;
		return player;
	}

	/// <summary>
	/// Raises <see cref="PeerEvent.PlayerCreated"/>, followed by any data the player sent before that.
	/// </summary>
	public void MarkCreated(Player player)
	{
		if (player.Created)
			return;

		player.Created = true;
		Publish();
		Emit(new PeerEvent.PlayerCreated(player.ToPlayerInfo()));
		foreach (var data in player.PendingData)
			Emit(new PeerEvent.DataReceived(player.Id, [.. data]));
		player.PendingData.Clear();
	}

	public void ReceiveUserData(Connection connection, byte[] payload)
	{
		if (!PlayersByConnection.TryGetValue(connection, out var player))
			return;

		if (player.Created)
			Emit(new PeerEvent.DataReceived(player.Id, [.. payload]));
		else
			player.PendingData.Add(payload);
	}

	/// <summary>
	/// Non-host peers report every fourth name table version to the host ([MC-DPL8CS] section 2.2.6).
	/// </summary>
	public void ReportNameTableVersion(Connection hostConnection)
	{
		if (Version % 4 == 0)
			SendCore(hostConnection, new CoreMessage.NameTableVersion { Version = Version });
	}

	/// <summary>
	/// Keeps a name table operation until every peer has it, so that a new host can collect the ones it missed
	/// after a migration ([MC-DPL8CS] section 3.2.5.4).
	/// </summary>
	public void Record(INameTableOperation operation)
	{
		// Client/server sessions never migrate, so there is no one to keep them for.
		if (IsClientServer)
			return;

		if (operation.Version > ResynchronizedVersion)
			NameTableLog.Add(operation);
	}

	/// <summary>
	/// Forgets the operations that every peer has, once the name table version is resynchronized.
	/// </summary>
	public void TrimNameTableLog(int version)
	{
		ResynchronizedVersion = Math.Max(ResynchronizedVersion, version);
		NameTableLog.RemoveAll(op => op.Version <= ResynchronizedVersion);
	}

	/// <summary>
	/// The operations newer than <paramref name="version"/>, in order.
	/// </summary>
	public IEnumerable<CoreMessage> GetOperationsAfter(int version) =>
		NameTableLog
			.Where(op => op.Version > version)
			.OrderBy(op => op.Version)
			.Cast<CoreMessage>();

	/// <summary>
	/// Records a name table operation and sends it to every peer we have a connection to, joining ones included.
	/// </summary>
	public void Announce<TOperation>(TOperation operation)
		where TOperation : CoreMessage, INameTableOperation
	{
		Record(operation);
		if (IsClientServer)
			return;

		foreach (var peer in ConnectedPeers)
			SendCore(peer.Connection!, operation);
	}

	/// <summary>
	/// Waits for the host to carry out a name table change that we requested. Returns the context that the request
	/// and the host's broadcast carry.
	/// </summary>
	public int BeginRequest(Action<Dpnid> onSuccess, Action<Exception> onFailure)
	{
		var context = ++_nextContext;
		if (context == 0)
			context = ++_nextContext;

		var timer = Schedule(Options.RequestTimeout, new SessionMessage.RequestTimedOut(context));
		_requests[context] = new PendingRequest(onSuccess, onFailure, timer);
		return context;
	}

	public int BeginRequest(TaskCompletionSource completion) =>
		BeginRequest(_ => completion.TrySetResult(), e => completion.TrySetException(e));

	/// <summary>
	/// Completes our request once the host's broadcast for it arrives.
	/// </summary>
	public void CompleteRequest(Dpnid requesting, int context, Dpnid result = default)
	{
		if (requesting != LocalPlayer || !_requests.Remove(context, out var request))
			return;

		request.Timer.Dispose();
		request.OnSuccess(result);
	}

	private void FailRequests(Exception exception)
	{
		var requests = _requests.Values.ToList();
		_requests.Clear();
		foreach (var request in requests)
		{
			request.Timer.Dispose();
			request.OnFailure(exception);
		}
	}

	/// <summary>
	/// The host that our requests went to is gone, and the new one never saw them.
	/// </summary>
	public void FailRequestsToLostHost() =>
		FailRequests(
			new DirectPlayException(
				ResultCode.ConnectionLost,
				"The host left before carrying out the request."
			)
		);

	/// <summary>
	/// Raises <see cref="PeerEvent.GroupCreated"/>, followed by <see cref="PeerEvent.PlayerAddedToGroup"/> for the
	/// members it already has, which a peer that joins later learns all at once.
	/// </summary>
	public void MarkGroupCreated(Group group)
	{
		if (group.Created)
			return;

		group.Created = true;
		Publish();
		Emit(new PeerEvent.GroupCreated(group.ToGroupInfo()));
		foreach (var member in group.Members.OrderBy(m => m.Value).Select(m => m.Key))
			Emit(new PeerEvent.PlayerAddedToGroup(group.Id, member));
	}

	/// <summary>
	/// Adds a group to the name table. <paramref name="announce"/> is false while joining, until the join completes.
	/// </summary>
	public void AddGroup(Group group, bool announce)
	{
		if (!Groups.TryAdd(group.Id, group))
			return;

		if (announce)
			MarkGroupCreated(group);
	}

	public void RemoveGroup(Dpnid id, DestroyGroupReason reason)
	{
		if (!Groups.Remove(id, out var group) || !group.Created)
			return;

		Publish();
		Emit(new PeerEvent.GroupDestroyed(id, reason));
	}

	public bool AddMember(Dpnid groupId, Dpnid playerId, int version)
	{
		if (
			!Groups.TryGetValue(groupId, out var group)
			|| !Players.ContainsKey(playerId)
			|| !group.Members.TryAdd(playerId, version)
		)
			return false;

		if (group.Created)
		{
			Publish();
			Emit(new PeerEvent.PlayerAddedToGroup(groupId, playerId));
		}
		return true;
	}

	public bool RemoveMember(Dpnid groupId, Dpnid playerId)
	{
		if (!Groups.TryGetValue(groupId, out var group) || !group.Members.Remove(playerId))
			return false;

		if (group.Created)
		{
			Publish();
			Emit(new PeerEvent.PlayerRemovedFromGroup(groupId, playerId));
		}
		return true;
	}

	/// <summary>
	/// Applies DPNINFO_NAME and DPNINFO_DATA to a player and raises <see cref="PeerEvent.PeerInfoUpdated"/>.
	/// </summary>
	public void UpdatePlayerInfo(Dpnid id, InfoFlags flags, string name, ImmutableArray<byte> data)
	{
		if (!Players.TryGetValue(id, out var player))
			return;

		if (flags.HasFlag(InfoFlags.Name))
			player.Name = name;
		if (flags.HasFlag(InfoFlags.Data))
			player.Data = data;
		if (player.IsLocal)
		{
			LocalName = player.Name;
			LocalData = player.Data;
		}

		if (player.Created)
		{
			Publish();
			Emit(new PeerEvent.PeerInfoUpdated(player.ToPlayerInfo()));
		}
	}

	/// <summary>
	/// The reason a peer gives for a group that the host destroyed: an <see cref="GroupFlags.AutoDestruct"/> group
	/// goes when its creator leaves, which the peer has already seen.
	/// </summary>
	public DestroyGroupReason GetDestroyReason(Dpnid groupId) =>
		Groups.TryGetValue(groupId, out var group)
		&& group.Flags.HasFlag(GroupFlags.AutoDestruct)
		&& !Players.ContainsKey(group.Owner)
			? DestroyGroupReason.AutoDestructed
			: DestroyGroupReason.Normal;

	/// <summary>
	/// Destroys a group as the host and tells the peers ([MC-DPL8CS] section 3.4.5.2, step 8).
	/// </summary>
	public void HostDestroyGroup(
		Dpnid groupId,
		Dpnid requesting,
		int context,
		DestroyGroupReason reason
	)
	{
		Version++;
		Announce(
			new CoreMessage.DestroyGroup
			{
				DpnidGroup = groupId,
				Version = Version,
				DpnidRequesting = requesting,
				PacketContext = context,
			}
		);
		RemoveGroup(groupId, reason);
	}

	/// <summary>
	/// Adds a player to a group as the host and tells the peers (step 4).
	/// </summary>
	public void HostAddMember(Dpnid groupId, Dpnid playerId, Dpnid requesting, int context)
	{
		Version++;
		AddMember(groupId, playerId, Version);
		Announce(
			new CoreMessage.AddPlayerToGroup
			{
				DpnidGroup = groupId,
				DpnidPlayer = playerId,
				Version = Version,
				DpnidRequesting = requesting,
				PacketContext = context,
			}
		);
	}

	/// <summary>
	/// Removes a player from a group as the host and tells the peers (step 6).
	/// </summary>
	public void HostRemoveMember(Dpnid groupId, Dpnid playerId, Dpnid requesting, int context)
	{
		Version++;
		RemoveMember(groupId, playerId);
		Announce(
			new CoreMessage.DeletePlayerFromGroup
			{
				DpnidGroup = groupId,
				DpnidPlayer = playerId,
				Version = Version,
				DpnidRequesting = requesting,
				PacketContext = context,
			}
		);
	}

	/// <summary>
	/// Updates a player's name and data as the host and tells the peers ([MC-DPL8CS] section 3.5).
	/// </summary>
	public void HostUpdateInfo(
		Dpnid playerId,
		InfoFlags flags,
		string name,
		ImmutableArray<byte> data,
		Dpnid requesting,
		int context
	)
	{
		Version++;
		UpdatePlayerInfo(playerId, flags, name, data);
		var message = new CoreMessage.UpdateInfo
		{
			PacketContext = context,
			Dpnid = playerId,
			Version = Version,
			InfoFlags = flags,
			Name = flags.HasFlag(InfoFlags.Name) ? name : string.Empty,
			Data = flags.HasFlag(InfoFlags.Data) ? data : [],
			DpnidRequesting = requesting,
		};
		if (!IsClientServer)
		{
			Announce(message);
			return;
		}

		// A client knows only the server: every client hears of the server's change (DPN_MSGID_SERVER_INFO), and a
		// client's change goes back to that client alone.
		Record(message);
		if (playerId == LocalPlayer)
		{
			foreach (var client in ConnectedPeers)
				SendCore(client.Connection!, message);
		}
		else if (Players.TryGetValue(playerId, out var player) && player.Connection is not null)
		{
			SendCore(player.Connection, message);
		}
	}

	public void RemovePlayer(Player player)
	{
		foreach (var group in Groups.Values.Where(g => g.Members.ContainsKey(player.Id)).ToList())
			RemoveMember(group.Id, player.Id);
		Players.Remove(player.Id);
		player.JoinTimer?.Dispose();
		if (player.Connection is not null)
		{
			PlayersByConnection.Remove(player.Connection);
			player.Connection.Post(new ConnectionMessage.Disconnect());
			player.Connection = null;
		}
	}

	/// <summary>
	/// Releases name table operations that every peer has seen ([MC-DPL8CS] section 2.2.6).
	/// </summary>
	public SessionState.Hosting ResynchronizeVersion(IHasHostingData hosting)
	{
		// Clients keep no name table of the session, so there is nothing to resynchronize.
		if (IsClientServer)
			return hosting.ToHosting();

		var peers = ConnectedPeers.Where(p => p.Created).ToList();
		if (peers.Count == 0)
			return hosting.ToHosting();

		var oldest = peers.Min(p => p.ReportedVersion);
		if (oldest <= hosting.ResynchronizedVersion)
			return hosting.ToHosting();

		foreach (var peer in peers)
			SendCore(peer.Connection!, new CoreMessage.ResyncVersion { Version = oldest });
		TrimNameTableLog(oldest);
		return hosting.ToHosting() with { ResynchronizedVersion = oldest };
	}

	/// <summary>
	/// Removes a player from the session as the host and tells the remaining peers ([MC-DPL8CS] section 3.2.5.2).
	/// </summary>
	public SessionState.Hosting DestroyPlayer(
		IHasHostingData hosting,
		Player player,
		DestroyPlayerFlags reason
	)
	{
		RemovePlayer(player);
		var involved = hosting
			.IntegrityChecks.Where(c => c.Key == player.Id || c.Value.Requester == player.Id)
			.ToList();
		foreach (var (_, check) in involved)
			check.Timer.Dispose();

		Version++;
		var message = new CoreMessage.DestroyPlayer
		{
			DpnidLeaving = player.Id,
			Version = Version,
			Reason = reason,
		};
		Announce(message);

		if (player.Created)
		{
			Publish();
			Emit(new PeerEvent.PlayerDestroyed(player.Id, reason));
		}

		// DPNGROUP_AUTODESTRUCT groups go with the player that created them.
		foreach (
			var group in Groups
				.Values.Where(g => g.Flags.HasFlag(GroupFlags.AutoDestruct) && g.Owner == player.Id)
				.ToList()
		)
			HostDestroyGroup(group.Id, LocalPlayer, context: 0, DestroyGroupReason.AutoDestructed);

		return ResynchronizeVersion(
			hosting.ToHosting() with
			{
				IntegrityChecks = hosting.IntegrityChecks.RemoveRange(involved.Select(c => c.Key)),
			}
		);
	}

	/// <summary>
	/// Removes a player that the host destroyed, as a non-host peer.
	/// </summary>
	public void ForgetPlayer(Dpnid id, DestroyPlayerFlags reason)
	{
		if (!Players.TryGetValue(id, out var player))
			return;

		RemovePlayer(player);
		if (player.Created)
		{
			Publish();
			Emit(new PeerEvent.PlayerDestroyed(player.Id, reason));
		}
	}

	/// <summary>
	/// Binds the connection that an established peer opened to us ([MC-DPL8CS] section 3.1.5.2, step 8).
	/// Returns <see langword="null"/> and drops the connection if the peer is not one we expect.
	/// </summary>
	public Player? IdentifyPeer(Connection connection, Dpnid id)
	{
		if (PlayersByConnection.ContainsKey(connection))
			return null;

		if (
			!Players.TryGetValue(id, out var player)
			|| player.IsLocal
			|| player.IsHost
			|| player.Connection is not null
		)
		{
			connection.Post(new ConnectionMessage.Disconnect());
			return null;
		}

		Bind(player, connection);
		return player;
	}

	private void ReportInstructedConnectFailed(
		Connection hostConnection,
		Dpnid player,
		string reason
	)
	{
		Trace.TraceWarning($"Cannot connect to player {player}: {reason}.");
		SendCore(hostConnection, new CoreMessage.InstructedConnectFailed { Dpnid = player });
	}

	/// <summary>
	/// Connects to a new peer as instructed by the host ([MC-DPL8CS] section 3.1.5.2, step 8).
	/// </summary>
	public void ConnectToNewPlayer(Connection hostConnection, Dpnid id)
	{
		if (!Players.TryGetValue(id, out var player))
		{
			ReportInstructedConnectFailed(hostConnection, id, "it is not in the name table");
			return;
		}

		player
			.Url.ToEndPoint()
			.Fold(
				onOk: remoteEndPoint =>
				{
					if (player.Connection is null)
						Bind(player, Connect(remoteEndPoint));
					return Unit.Default;
				},
				onError: error =>
				{
					ReportInstructedConnectFailed(
						hostConnection,
						player.Id,
						$"its address {player.Url} is unusable ({error})"
					);
					return Unit.Default;
				}
			);
	}

	public void DisconnectAll()
	{
		foreach (var connection in Connections)
			connection.Post(new ConnectionMessage.Disconnect());
	}

	private void ShutDownEndpoint()
	{
		_endpoint?.Post(new ReliableEndpointMessage.ShutDown());
		_endpoint = null;
		Connections.Clear();
		PlayersByConnection.Clear();
	}

	private void ClearSession()
	{
		foreach (var player in Players.Values)
			player.JoinTimer?.Dispose();
		Players.Clear();
		PlayersByConnection.Clear();
		Groups.Clear();
		FailRequests(
			new DirectPlayException(
				ResultCode.NoConnection,
				"The peer is no longer in the session."
			)
		);
		JoinTimer.Cancel();
		MigrationTimer.Cancel();
		LocalPlayer = Dpnid.Empty;
		Version = 0;
		NameTableLog.Clear();
		ResynchronizedVersion = 0;
		HostClaim = null;
	}

	/// <summary>
	/// Ends a connection attempt and returns to idle, so that the application can try again.
	/// </summary>
	public SessionState FailConnect(ResultCode result, ImmutableArray<byte> reply)
	{
		foreach (var connection in Connections)
			connection.Post(new ConnectionMessage.HardDisconnect());
		ShutDownEndpoint();
		ClearSession();
		Publish();
		Emit(new PeerEvent.ConnectCompleted(result, reply));
		return new SessionState.Idle();
	}

	/// <summary>
	/// The local peer left the session involuntarily. The application still has to close the peer.
	/// </summary>
	public SessionState Terminate(ResultCode result)
	{
		var created = Players.Values.Where(p => p.Created).OrderBy(p => p.IsLocal).ToList();
		var groups = Groups.Values.Where(g => g.Created).ToList();
		DisconnectAll();
		ClearSession();
		Publish();
		Emit(new PeerEvent.SessionTerminated(result));
		foreach (var group in groups)
			Emit(new PeerEvent.GroupDestroyed(group.Id, DestroyGroupReason.SessionTerminated));
		foreach (var player in created)
			Emit(new PeerEvent.PlayerDestroyed(player.Id, DestroyPlayerFlags.SessionTerminated));
		return new SessionState.Terminated();
	}

	/// <summary>
	/// The player that takes over when the host leaves: of the players still in the session that we are connected
	/// to, and ourselves, the one with the lowest name table version, that is the one that joined first
	/// ([MC-DPL8CS] section 3.2.5.4, step 1). Every peer reaches the same choice from the same name table.
	/// </summary>
	public Player? ElectHost() =>
		Players
			.Values.Where(p => !p.IsHost && (p.IsLocal || (p.Created && p.Connection is not null)))
			.OrderBy(p => p.Version)
			.ThenBy(p => p.Id.Value)
			.FirstOrDefault();

	/// <summary>
	/// The name table index after the highest in use, from which a new host hands out DPNIDs.
	/// </summary>
	private int GetNextNameTableIndex()
	{
		var mask = BitConverter.ToInt32(Application.GuidInstance.ToByteArray(), 0);
		var logged = NameTableLog.SelectMany(op =>
			op switch
			{
				CoreMessage.AddPlayer m => [m.Dpnid],
				CoreMessage.CreateGroup m => new[] { m.Group.Dpnid },
				_ => [],
			}
		);
		var highest = Players
			.Keys.Concat(Groups.Keys)
			.Concat(logged)
			.Select(id => (id.Value ^ mask) & 0xFFFFF)
			.DefaultIfEmpty(2)
			.Max();
		return highest + 1;
	}

	/// <summary>
	/// Applies an operation that the new host collected with DN_ACK_NAMETABLE_OP, as a peer would have.
	/// </summary>
	private void ApplyOperation(INameTableOperation operation)
	{
		switch (operation)
		{
			case CoreMessage.AddPlayer m:
				Players.TryAdd(
					m.Dpnid,
					new Player
					{
						Id = m.Dpnid,
						Name = m.Name,
						Data = [.. m.Data],
						Url = new DirectPlayUrl(m.Url),
						Version = m.Version,
						DnetVersion = m.DnetClientVersion,
					}
				);
				break;
			case CoreMessage.DestroyPlayer m:
				ForgetPlayer(m.DpnidLeaving, m.Reason);
				break;
			case CoreMessage.UpdateInfo m:
				UpdatePlayerInfo(m.Dpnid, m.InfoFlags, m.Name, [.. m.Data]);
				break;
			case CoreMessage.CreateGroup m:
				AddGroup(Group.FromNameTableEntry(m.Group), announce: true);
				break;
			case CoreMessage.DestroyGroup m:
				RemoveGroup(m.DpnidGroup, GetDestroyReason(m.DpnidGroup));
				break;
			case CoreMessage.AddPlayerToGroup m:
				AddMember(m.DpnidGroup, m.DpnidPlayer, m.Version);
				break;
			case CoreMessage.DeletePlayerFromGroup m:
				RemoveMember(m.DpnidGroup, m.DpnidPlayer);
				break;
		}

		if (operation.Version > Version)
			Version = operation.Version;
		Record(operation);
	}

	/// <summary>
	/// Ends migration (steps 5 and 6): sends every peer the operations it missed, removes the old host and the
	/// players whose join it never completed, and announces DN_HOST_MIGRATE_COMPLETE.
	/// </summary>
	public SessionState.Hosting FinishMigration(SessionState.Hosting hosting)
	{
		MigrationTimer.Cancel();
		var migration = hosting.Migration!;
		foreach (var (id, reported) in migration.Reported)
		{
			if (Players.TryGetValue(id, out var peer) && peer.Connection is not null)
			{
				foreach (var operation in GetOperationsAfter(reported))
					SendCore(peer.Connection, operation);
			}
		}

		// The operations collected may name players and groups that are gone already, whose indexes are taken.
		hosting = hosting with
		{
			Migration = null,
			NextIndex = Math.Max(hosting.NextIndex, GetNextNameTableIndex()),
		};
		if (Players.TryGetValue(migration.OldHost, out var oldHost))
			hosting = DestroyPlayer(hosting, oldHost, DestroyPlayerFlags.ConnectionLost);
		else
		{
			// Everyone removed the old host already; the operation keeps the name table versions in step.
			Version++;
			Announce(
				new CoreMessage.DestroyPlayer
				{
					DpnidLeaving = migration.OldHost,
					Version = Version,
					Reason = DestroyPlayerFlags.ConnectionLost,
				}
			);
			foreach (
				var group in Groups
					.Values.Where(g =>
						g.Flags.HasFlag(GroupFlags.AutoDestruct) && g.Owner == migration.OldHost
					)
					.ToList()
			)
				HostDestroyGroup(
					group.Id,
					LocalPlayer,
					context: 0,
					DestroyGroupReason.AutoDestructed
				);
		}

		// Joins the old host never completed, and peers that left while we were migrating, go now, once every peer
		// has the operations before them.
		foreach (
			var gone in Players
				.Values.Where(p => !p.IsLocal && (!p.Created || p.Connection is null))
				.ToList()
		)
			hosting = DestroyPlayer(hosting, gone, DestroyPlayerFlags.ConnectionLost);

		foreach (var peer in ConnectedPeers)
			SendCore(peer.Connection!, new CoreMessage.HostMigrateComplete());
		return ResynchronizeVersion(hosting);
	}

	/// <summary>
	/// Moves migration on once every peer has reported its name table version (steps 2 to 4): if a peer is ahead of
	/// us, asks it for what we missed; otherwise finishes.
	/// </summary>
	public SessionState.Hosting ContinueMigration(SessionState.Hosting hosting)
	{
		if (
			hosting.Migration is not { RequestedFrom: null } migration
			|| !migration.Awaiting.IsEmpty
		)
			return hosting;

		var newest = migration
			.Reported.Where(r => r.Value > Version && Players.ContainsKey(r.Key))
			.OrderByDescending(r => r.Value)
			.Select(r => Players[r.Key])
			.FirstOrDefault(p => p.Connection is not null);
		if (newest is null)
			return FinishMigration(hosting);

		SendCore(
			newest.Connection!,
			new CoreMessage.RequestNameTableOperations { Version = Version }
		);
		return hosting with { Migration = migration with { RequestedFrom = newest.Id } };
	}

	/// <summary>
	/// Installs the operations from the peer we asked, then finishes migration.
	/// </summary>
	public SessionState.Hosting ApplyMissedOperations(
		SessionState.Hosting hosting,
		IEnumerable<INameTableOperation> operations
	)
	{
		foreach (
			var operation in operations.Where(op => op.Version > Version).OrderBy(op => op.Version)
		)
			ApplyOperation(operation);
		return FinishMigration(hosting);
	}

	/// <summary>
	/// Takes over as host after <paramref name="oldHost"/> left (step 1): tells every peer with DN_HOST_MIGRATE and
	/// waits for their name table versions.
	/// </summary>
	public SessionState BecomeHost(Player local, Dpnid oldHost)
	{
		local.IsHost = true;
		Publish();
		Emit(new PeerEvent.HostMigrated(local.Id));

		var peers = ConnectedPeers.Where(p => p.Created).ToList();
		foreach (var peer in peers)
			SendCore(
				peer.Connection!,
				new CoreMessage.HostMigrate { DpnidOldHost = oldHost, DpnidNewHost = local.Id }
			);

		var hosting = new SessionState.Hosting(
			GetNextNameTableIndex(),
			ResynchronizedVersion,
			ImmutableDictionary<Dpnid, IntegrityCheck>.Empty,
			new MigrationProgress(
				oldHost,
				[.. peers.Select(p => p.Id)],
				ImmutableDictionary<Dpnid, int>.Empty,
				RequestedFrom: null
			)
		);
		MigrationTimer.Start(Options.IntegrityCheckTimeout);
		return ContinueMigration(hosting);
	}

	/// <summary>
	/// Follows <paramref name="newHost"/>, which announced itself with DN_HOST_MIGRATE (step 2): the old host goes,
	/// requests sent to another host fail, and the new host gets our name table version.
	/// </summary>
	public SessionState AcceptHost(
		Connection connection,
		Player newHost,
		Dpnid oldHostId,
		Connection previousHost
	)
	{
		HostClaim = null;
		if (connection != previousHost)
			FailRequestsToLostHost();

		if (Players.TryGetValue(oldHostId, out var oldHost) && oldHost != newHost)
			ForgetPlayer(oldHost.Id, DestroyPlayerFlags.ConnectionLost);

		if (!newHost.IsHost)
		{
			newHost.IsHost = true;
			Publish();
			Emit(new PeerEvent.HostMigrated(newHost.Id));
		}

		SendCore(connection, new CoreMessage.NameTableVersion { Version = Version });
		return new SessionState.Connected(connection);
	}

	/// <summary>
	/// The host is gone and the session allows migration: the local peer takes over or follows whoever does,
	/// at once if that peer already announced itself.
	/// </summary>
	public SessionState MigrateFrom(Player oldHost, ResultCode result)
	{
		FailRequestsToLostHost();
		ForgetPlayer(
			oldHost.Id,
			result == ResultCode.Success
				? DestroyPlayerFlags.Normal
				: DestroyPlayerFlags.ConnectionLost
		);
		var next = ElectHost();
		if (next is null)
			return Terminate(ResultCode.HostTerminatedSession);

		if (next.IsLocal)
			return BecomeHost(next, oldHost.Id);

		return HostClaim is { } claim && claim.Connection == next.Connection
			? AcceptHost(next.Connection, next, claim.Message.DpnidOldHost, next.Connection)
			: new SessionState.Connected(next.Connection!);
	}

	/// <summary>
	/// Completes the join once the host has instructed the established peers to connect and all of them have
	/// identified themselves ([MC-DPL8CS] section 3.1, figure 3).
	/// </summary>
	public SessionState CompleteJoin(SessionState.Joining joining)
	{
		if (!joining.InstructConnectReceived || joining.AwaitingPlayers.Count > 0)
			return joining;

		JoinTimer.Cancel();
		var players = Players
			.Values.Where(p => p.IsLocal || p.Connection is not null)
			.OrderByDescending(p => p.IsLocal)
			.ThenByDescending(p => p.IsHost)
			.ThenBy(p => p.Version)
			.ToList();
		foreach (var player in players)
			MarkCreated(player);
		foreach (var group in Groups.Values.OrderBy(g => g.Version).ToList())
			MarkGroupCreated(group);
		Emit(new PeerEvent.ConnectCompleted(ResultCode.Success, joining.Reply));
		return new SessionState.Connected(joining.HostConnection);
	}

	public SessionState CompleteClose(IEnumerable<TaskCompletionSource> completions)
	{
		CloseTimer.Cancel();
		ShutDownEndpoint();
		ClearSession();
		_events.TryComplete();
		foreach (var completion in completions)
			completion.TrySetResult();
		Stop();
		return new SessionState.Closed();
	}

	/// <summary>
	/// Disconnects gracefully from every peer, waiting at most <see cref="PeerOptions.CloseTimeout"/>.
	/// </summary>
	public SessionState BeginClosing(TaskCompletionSource completion)
	{
		DisconnectAll();
		CloseTimer.Start(Options.CloseTimeout);
		return Connections.Count == 0
			? CompleteClose([completion])
			: new SessionState.Closing([completion]);
	}

	public void SendTo(SessionMessage.SendTo command)
	{
		var targets = new List<Connection>();
		var loopback = false;
		var result = ResultCode.Success;
		if (command.Target == Peer.AllPlayers)
		{
			targets.AddRange(ConnectedPeers.Where(p => p.Created).Select(p => p.Connection!));
			loopback = !command.Flags.HasFlag(SendFlags.NoLoopback);
		}
		else if (command.Target == LocalPlayer)
		{
			loopback = true;
		}
		else if (
			Players.TryGetValue(command.Target, out var player)
			&& player.Created
			&& player.Connection is not null
		)
		{
			targets.Add(player.Connection);
		}
		else if (Groups.TryGetValue(command.Target, out var group) && group.Created)
		{
			// A group send goes to each member we are connected to, and to us if we are a member.
			targets.AddRange(
				group
					.Members.Keys.Select(id => Players.GetValueOrDefault(id))
					.Where(p => p is { IsLocal: false, Created: true, Connection: not null })
					.Select(p => p!.Connection!)
			);
			loopback =
				group.Members.ContainsKey(LocalPlayer)
				&& !command.Flags.HasFlag(SendFlags.NoLoopback);
		}
		else
		{
			result = ResultCode.InvalidPlayer;
		}

		if (loopback)
			Emit(new PeerEvent.DataReceived(LocalPlayer, [.. command.Data]));

		if (result != ResultCode.Success || targets.Count == 0)
		{
			if (!command.Flags.HasFlag(SendFlags.NoComplete))
				Emit(new PeerEvent.SendCompleted(command.Handle, result));
			return;
		}

		_sends.Add(command.Handle, new PendingSend(targets.Count, command.Flags));
		foreach (var target in targets)
		{
			var sendId = ++_nextSendId;
			_sendHandles.Add(sendId, command.Handle);
			target.Post(
				new ConnectionMessage.SendPayload(
					sendId,
					command.Data,
					Reliable: command.Flags.HasFlag(SendFlags.Guaranteed),
					User1: false
				)
			);
		}
	}

	/// <summary>
	/// Whether the state makes some commands wait: a new host takes no name table changes, joins or integrity
	/// checks until it has collected the name table, and a joining peer sends info updates once it is in.
	/// </summary>
	private bool IsWaiting =>
		_state
			is SessionState.Hosting { Migration: not null }
				or SessionState.Connecting
				or SessionState.Joining;

	private bool MustWait<TCommand>(TCommand command) =>
		_state switch
		{
			SessionState.Hosting { Migration: not null } => command
				is SessionMessage.SetPeerInfo
					or SessionMessage.CreateGroup
					or SessionMessage.DestroyGroup
					or SessionMessage.AddPlayerToGroup
					or SessionMessage.RemovePlayerFromGroup
					or SessionMessage.DestroyPlayer
					or Received<CoreMessage.PlayerConnectInfo>
					or Received<CoreMessage.RequestIntegrityCheck>
					or Received<CoreMessage.RequestUpdateInfo>
					or Received<CoreMessage.RequestCreateGroup>
					or Received<CoreMessage.RequestDestroyGroup>
					or Received<CoreMessage.RequestAddPlayerToGroup>
					or Received<CoreMessage.RequestDeletePlayerFromGroup>,
			SessionState.Connecting or SessionState.Joining => command
				is SessionMessage.SetPeerInfo,
			_ => false,
		};

	private Unit Execute<TCommand>(TCommand command)
	{
		if (MustWait(command))
		{
			// A command goes back to the mailbox, so that it runs as it would have, fallback included.
			_deferred.Add(
				command is SessionMessage message ? () => Post(message) : () => Execute(command)
			);
			return Unit.Default;
		}

		_state = _state is ISessionTransition<TCommand> transition
			? transition.Execute(this, command)
			: _state;
		return Unit.Default;
	}

	/// <summary>
	/// Runs the commands that waited, once the state no longer makes them wait.
	/// </summary>
	private void ReplayDeferred()
	{
		if (_deferred.Count == 0 || IsWaiting)
			return;

		var deferred = _deferred.ToList();
		_deferred.Clear();
		foreach (var replay in deferred)
			replay();
	}

	/// <summary>
	/// Runs the transition for <paramref name="command"/>, or <paramref name="otherwise"/> if the current state has none.
	/// </summary>
	private Unit Execute<TCommand>(TCommand command, Action otherwise)
	{
		if (_state is ISessionTransition<TCommand> || MustWait(command))
			return Execute(command);

		otherwise();
		return Unit.Default;
	}

	/// <summary>
	/// Outside a session, the name and data take effect for the next session; in one, the transition updates them
	/// everywhere.
	/// </summary>
	private Unit SetPeerInfo(SessionMessage.SetPeerInfo command) =>
		Execute(
			command,
			otherwise: () =>
			{
				LocalName = command.Name;
				LocalData = command.Data;
				command.Completion.TrySetResult();
			}
		);

	private static DirectPlayException NotInSession() =>
		new(ResultCode.NoConnection, "The peer is not in a session.");

	private Unit RequestTimedOut(int context)
	{
		if (_requests.Remove(context, out var request))
		{
			request.Timer.Dispose();
			request.OnFailure(
				new DirectPlayException(ResultCode.TimedOut, "The host did not answer the request.")
			);
		}
		return Unit.Default;
	}

	/// <summary>
	/// Answers a host enumeration query while hosting, unless the session forbids enumeration or the query is for
	/// another application ([MC-DPLHP] section 2.2.2). The response goes from the session's endpoint, the address a
	/// joining peer connects to.
	/// </summary>
	private Unit AnswerEnumQuery(SessionMessage.EnumQueryReceived message)
	{
		var query = message.Query;
		if (
			_state is not SessionState.Hosting
			|| Application.Flags.HasFlag(SessionFlags.NoEnumerations)
			|| (query.GuidApplication is Guid guid && guid != Application.GuidApplication)
		)
			return Unit.Default;

		var response = new EnumerationMessage.Response
		{
			EnumPayload = query.EnumPayload,
			Flags = Application.Flags,
			MaxPlayers = Application.MaxPlayers,
			CurrentPlayers = Players.Count,
			GuidInstance = Application.GuidInstance,
			GuidApplication = Application.GuidApplication,
			SessionName = Application.SessionName,
			ApplicationReservedData = Application.ApplicationReservedData,
			ApplicationData = EnumResponseData,
		};
		_endpoint?.Post(
			new ReliableEndpointMessage.SendDatagram(
				message.RemoteEndPoint,
				EnumerationMessageSerializer.Default.Serialize(response)
			)
		);
		return Unit.Default;
	}

	private Unit SendCompleted(long sendId, ResultCode result)
	{
		if (
			!_sendHandles.Remove(sendId, out var handle)
			|| !_sends.TryGetValue(handle, out var send)
		)
			return Unit.Default;

		send.Remaining--;
		if (result != ResultCode.Success && send.Result == ResultCode.Success)
			send.Result = result;

		if (send.Remaining > 0)
			return Unit.Default;

		_sends.Remove(handle);
		if (!send.Flags.HasFlag(SendFlags.NoComplete))
			Emit(new PeerEvent.SendCompleted(handle, send.Result));
		return Unit.Default;
	}

	private Unit Dispatch(Connection connection, CoreMessage message) =>
		message.Match(
			PlayerConnectInfo: m =>
				Execute(new Received<CoreMessage.PlayerConnectInfo>(connection, m)),
			ConnectFailed: m => Execute(new Received<CoreMessage.ConnectFailed>(connection, m)),
			SendConnectInfo: m => Execute(new Received<CoreMessage.SendConnectInfo>(connection, m)),
			AddPlayer: m => Execute(new Received<CoreMessage.AddPlayer>(connection, m)),
			AckConnectInfo: m => Execute(new Received<CoreMessage.AckConnectInfo>(connection, m)),
			InstructConnect: m => Execute(new Received<CoreMessage.InstructConnect>(connection, m)),
			SendPlayerDpnid: m => Execute(new Received<CoreMessage.SendPlayerDpnid>(connection, m)),
			InstructedConnectFailed: m =>
				Execute(new Received<CoreMessage.InstructedConnectFailed>(connection, m)),
			ConnectAttemptFailed: m =>
				Execute(new Received<CoreMessage.ConnectAttemptFailed>(connection, m)),
			TerminateSession: m =>
				Execute(new Received<CoreMessage.TerminateSession>(connection, m)),
			DestroyPlayer: m => Execute(new Received<CoreMessage.DestroyPlayer>(connection, m)),
			HostMigrate: m => Execute(new Received<CoreMessage.HostMigrate>(connection, m)),
			NameTableVersion: m =>
				Execute(new Received<CoreMessage.NameTableVersion>(connection, m)),
			ResyncVersion: m => Execute(new Received<CoreMessage.ResyncVersion>(connection, m)),
			RequestIntegrityCheck: m =>
				Execute(new Received<CoreMessage.RequestIntegrityCheck>(connection, m)),
			IntegrityCheck: m => Execute(new Received<CoreMessage.IntegrityCheck>(connection, m)),
			IntegrityCheckResponse: m =>
				Execute(new Received<CoreMessage.IntegrityCheckResponse>(connection, m)),
			RequestNameTableOperations: m =>
				Execute(new Received<CoreMessage.RequestNameTableOperations>(connection, m)),
			AckNameTableOperations: m =>
				Execute(new Received<CoreMessage.AckNameTableOperations>(connection, m)),
			HostMigrateComplete: m =>
				Execute(new Received<CoreMessage.HostMigrateComplete>(connection, m)),
			RequestCreateGroup: m =>
				Execute(new Received<CoreMessage.RequestCreateGroup>(connection, m)),
			CreateGroup: m => Execute(new Received<CoreMessage.CreateGroup>(connection, m)),
			RequestAddPlayerToGroup: m =>
				Execute(new Received<CoreMessage.RequestAddPlayerToGroup>(connection, m)),
			AddPlayerToGroup: m =>
				Execute(new Received<CoreMessage.AddPlayerToGroup>(connection, m)),
			RequestDeletePlayerFromGroup: m =>
				Execute(new Received<CoreMessage.RequestDeletePlayerFromGroup>(connection, m)),
			DeletePlayerFromGroup: m =>
				Execute(new Received<CoreMessage.DeletePlayerFromGroup>(connection, m)),
			RequestDestroyGroup: m =>
				Execute(new Received<CoreMessage.RequestDestroyGroup>(connection, m)),
			DestroyGroup: m => Execute(new Received<CoreMessage.DestroyGroup>(connection, m)),
			RequestUpdateInfo: m =>
				Execute(new Received<CoreMessage.RequestUpdateInfo>(connection, m)),
			UpdateInfo: m => Execute(new Received<CoreMessage.UpdateInfo>(connection, m))
		);

	private Unit Receive(Connection connection, CoreMessage message)
	{
		// Peers keep the host's name table operations, in case they must hand them to a new host.
		if (
			_state is IHasHostConnection { HostConnection: var host }
			&& host == connection
			&& message is INameTableOperation operation
		)
			Record(operation);
		return Dispatch(connection, message);
	}

	private Unit Receive(SessionMessage.ConnectionReceived message)
	{
		if (!message.User1)
			return Execute(message);

		return CoreMessageSerializer.Default.Deserialize(message.Payload) is { } coreMessage
			? Receive(message.Connection, coreMessage)
			: Unit.Default;
	}

	private Unit Connected(SessionMessage.ConnectionConnected message)
	{
		if (_state is not ISessionTransition<SessionMessage.ConnectionConnected>)
		{
			message.Connection.Post(new ConnectionMessage.Disconnect());
			return Unit.Default;
		}

		Connections.Add(message.Connection);
		return Execute(message);
	}

	private Unit Elapse<TCommand>(
		ActorTimer<SessionMessage> timer,
		long generation,
		TCommand command
	) => timer.TryConsume(generation) ? Execute(command) : Unit.Default;

	protected override void Receive(SessionMessage message)
	{
		message.Match(
			SetPeerInfo: SetPeerInfo,
			CreateGroup: m =>
				Execute(m, otherwise: () => m.Completion.TrySetException(NotInSession())),
			DestroyGroup: m =>
				Execute(m, otherwise: () => m.Completion.TrySetException(NotInSession())),
			AddPlayerToGroup: m =>
				Execute(m, otherwise: () => m.Completion.TrySetException(NotInSession())),
			RemovePlayerFromGroup: m =>
				Execute(m, otherwise: () => m.Completion.TrySetException(NotInSession())),
			DestroyPlayer: m =>
				Execute(
					m,
					otherwise: () =>
						m.Completion.TrySetException(
							new DirectPlayException(
								ResultCode.NotHost,
								"Only the host removes players."
							)
						)
				),
			RequestTimedOut: m => RequestTimedOut(m.Context),
			Host: m =>
				Execute(
					m,
					otherwise: () =>
						m.Completion.TrySetException(
							new InvalidOperationException("The peer is already in a session.")
						)
				),
			Connect: m =>
				Execute(
					m,
					otherwise: () =>
						m.Completion.TrySetException(
							new InvalidOperationException("The peer is already in a session.")
						)
				),
			SendTo: m =>
				Execute(
					m,
					otherwise: () =>
					{
						if (!m.Flags.HasFlag(SendFlags.NoComplete))
							Emit(new PeerEvent.SendCompleted(m.Handle, ResultCode.NoConnection));
					}
				),
			Close: m => Execute(m, otherwise: () => m.Completion.TrySetResult()),
			ConnectionConnected: Connected,
			ConnectionConnectFailed: m =>
			{
				Connections.Remove(m.Connection);
				return Execute(m);
			},
			ConnectionReceived: Receive,
			ConnectionSendCompleted: m => SendCompleted(m.SendId, m.Result),
			ConnectionDisconnected: m =>
			{
				Connections.Remove(m.Connection);
				return Execute(m);
			},
			EnumQueryReceived: AnswerEnumQuery,
			SetEnumResponseData: m =>
			{
				EnumResponseData = m.Data;
				return Unit.Default;
			},
			JoinTimedOut: m => Elapse(JoinTimer, m.Generation, m),
			CloseTimedOut: m => Elapse(CloseTimer, m.Generation, m),
			MigrationTimedOut: m => Elapse(MigrationTimer, m.Generation, m),
			PlayerJoinTimedOut: Execute,
			IntegrityCheckTimedOut: Execute
		);
		ReplayDeferred();
		Publish();
	}
}
