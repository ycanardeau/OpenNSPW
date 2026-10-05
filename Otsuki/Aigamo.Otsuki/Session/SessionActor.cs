using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Threading.Channels;
using Aigamo.Otsuki.Actors;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Reliable;
using Aigamo.Otsuki.Session.Transitions;
using Aigamo.Results;

namespace Aigamo.Otsuki.Session;

/// <summary>
/// The peer-to-peer session of the DirectPlay 8 core protocol ([MC-DPL8CS]). The protocol logic lives in the
/// transitions that each <see cref="SessionState"/> implements; this actor owns the state and the name table,
/// and runs the transition for each message. Host migration and groups are not implemented; when the host leaves,
/// the session terminates.
/// </summary>
internal sealed class SessionActor : Actor<SessionMessage>, IConnectionObserver
{
	private sealed class PendingSend(int remaining, SendFlags flags)
	{
		public int Remaining { get; set; } = remaining;
		public SendFlags Flags { get; } = flags;
		public ResultCode Result { get; set; } = ResultCode.Success;
	}

	private readonly ChannelWriter<PeerEvent> _events;
	private readonly Action<SessionSnapshot> _publish;
	private readonly Dictionary<long, PendingSend> _sends = [];
	private readonly Dictionary<long, long> _sendHandles = [];
	private SessionState _state = new SessionState.Idle();
	private ReliableEndpoint? _endpoint;
	private long _nextSendId;

	public PeerOptions Options { get; }

	public ActorTimer<SessionMessage> JoinTimer { get; }

	public ActorTimer<SessionMessage> CloseTimer { get; }

	/// <summary>
	/// The name table ([MC-DPL8CS] section 2.2.6).
	/// </summary>
	public Dictionary<Dpnid, Player> Players { get; } = [];

	public Dictionary<Connection, Player> PlayersByConnection { get; } = [];

	/// <summary>
	/// Every connection that has not reported its end yet.
	/// </summary>
	public HashSet<Connection> Connections { get; } = [];

	public string LocalName { get; private set; } = string.Empty;

	public ImmutableArray<byte> LocalData { get; private set; } = [];

	public ApplicationDescription Application { get; set; } = new();

	public ImmutableArray<byte> ConnectData { get; set; } = [];

	public Dpnid LocalPlayer { get; set; }

	/// <summary>
	/// The name table version.
	/// </summary>
	public int Version { get; set; }

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

	public IPEndPoint LocalEndPoint => _endpoint!.LocalEndPoint;

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
				Players.TryGetValue(LocalPlayer, out var local) && local.IsHost
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

	public void RemovePlayer(Player player)
	{
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
		var peers = ConnectedPeers.Where(p => p.Created).ToList();
		if (peers.Count == 0)
			return hosting.ToHosting();

		var oldest = peers.Min(p => p.ReportedVersion);
		if (oldest <= hosting.ResynchronizedVersion)
			return hosting.ToHosting();

		foreach (var peer in peers)
			SendCore(peer.Connection!, new CoreMessage.ResyncVersion { Version = oldest });
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
		foreach (var peer in ConnectedPeers)
			SendCore(peer.Connection!, message);

		if (player.Created)
		{
			Publish();
			Emit(new PeerEvent.PlayerDestroyed(player.Id, reason));
		}

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
		JoinTimer.Cancel();
		LocalPlayer = Dpnid.Empty;
		Version = 0;
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
		DisconnectAll();
		ClearSession();
		Publish();
		Emit(new PeerEvent.SessionTerminated(result));
		foreach (var player in created)
			Emit(new PeerEvent.PlayerDestroyed(player.Id, DestroyPlayerFlags.SessionTerminated));
		return new SessionState.Terminated();
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

	private Unit Execute<TCommand>(TCommand command)
	{
		_state = _state is ISessionTransition<TCommand> transition
			? transition.Execute(this, command)
			: _state;
		return Unit.Default;
	}

	/// <summary>
	/// Runs the transition for <paramref name="command"/>, or <paramref name="otherwise"/> if the current state has none.
	/// </summary>
	private Unit Execute<TCommand>(TCommand command, Action otherwise)
	{
		if (_state is ISessionTransition<TCommand>)
			return Execute(command);

		otherwise();
		return Unit.Default;
	}

	private Unit SetPeerInfo(SessionMessage.SetPeerInfo command)
	{
		LocalName = command.Name;
		LocalData = command.Data;
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

	/// <summary>
	/// Host migration and its name table operations are not implemented, so those messages have no transitions.
	/// </summary>
	private Unit Receive(Connection connection, CoreMessage message) =>
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
				Execute(new Received<CoreMessage.HostMigrateComplete>(connection, m))
		);

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
			JoinTimedOut: m => Elapse(JoinTimer, m.Generation, m),
			CloseTimedOut: m => Elapse(CloseTimer, m.Generation, m),
			PlayerJoinTimedOut: Execute,
			IntegrityCheckTimedOut: Execute
		);
		Publish();
	}
}
