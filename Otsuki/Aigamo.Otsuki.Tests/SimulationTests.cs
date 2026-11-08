using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using Aigamo.Otsuki.Actors;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Reliable;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Tests;

/// <summary>
/// Runs whole sessions on one thread, every actor's messages and every datagram's delivery in an order that a seed
/// chooses, with a clock that moves only when nothing else can, and a network that loses what the seed says
/// (<see cref="ManualScheduler"/>, <see cref="ManualTimeProvider"/>, <see cref="SimulatedNetwork"/>). Many seeds try
/// many orders, and a seed takes the same order again, to the byte: a failing seed is a failing test that repeats.
/// </summary>
public class SimulationTests
{
	private const int Seeds = 100;

	private static readonly Guid GuidApplication = new("0c5a1e7d-2b9f-4e61-8d3a-7f4b6c2e9a15");

	private static readonly IPEndPoint ServerEndPoint = new(IPAddress.Parse("10.7.0.1"), 2302);

	private sealed class Simulation
	{
		private int _peers;

		public int Seed { get; }

		public ManualScheduler Scheduler { get; }

		public ManualTimeProvider Clock { get; } = new();

		public SimulatedNetwork Network { get; }

		/// <summary>
		/// The test's own choices, such as when a peer leaves, from the seed.
		/// </summary>
		public Random Random { get; }

		/// <summary>
		/// The peers whose events and snapshots are checked after every step.
		/// </summary>
		public List<SimulatedPeer> Peers { get; } = [];

		public Simulation(int seed)
		{
			Seed = seed;
			Scheduler = new ManualScheduler(seed);
			Network = new SimulatedNetwork(Scheduler, () => Clock.Now, seed);
			Random = new Random(seed ^ 0x5EED);
		}

		// Each peer draws its randomness from a seed of its own, which the simulation's seed and the peer's place
		// choose: two runs of one seed then make the same GUIDs, session IDs and secrets.
		public PeerOptions Options(IPAddress address, int loops = 1) =>
			new()
			{
				Scheduler = Scheduler,
				TimeProvider = Clock,
				TransportFactory = Network.CreateFactory(address),
				ConnectionLoops = loops,
				RandomSeed = Seed * 1000 + _peers++ * 10,
			};

		/// <summary>
		/// Runs messages, then timers when no message waits, until <paramref name="done"/>; false if the simulated time
		/// passes <paramref name="limit"/>, or nothing is left to run. Continuations of tasks run on the thread pool, so
		/// an idle simulation waits a little for them before it gives up.
		/// </summary>
		public bool RunUntil(Func<bool> done, TimeSpan limit)
		{
			var end = Clock.Now + limit;
			while (!done())
			{
				if (Scheduler.RunNext() || Clock.FireNext(end))
				{
					CheckPeers();
					continue;
				}

				var waited = Stopwatch.StartNew();
				while (Scheduler.Ready == 0 && !done() && waited.ElapsedMilliseconds < 20)
					Thread.Sleep(1);
				if (Scheduler.Ready == 0 && !done())
					return false;
			}

			return true;
		}

		// Every peer's events and snapshot hold together after each step, not only once a scenario is done.
		private void CheckPeers()
		{
			foreach (var peer in Peers)
				peer.Check();
		}

		/// <summary>
		/// Runs <paramref name="steps"/> steps at most, messages or timers: a point in the middle of whatever is
		/// going on, as the seed chooses it.
		/// </summary>
		public void Run(int steps)
		{
			var taken = 0;
			RunUntil(() => taken++ >= steps, TimeSpan.FromSeconds(60));
		}
	}

	/// <summary>
	/// A peer of a peer-to-peer session, with the events it raised so far, which it checks as they come: a player or
	/// group is created only when it is not, and destroyed, sends, joins a group or becomes host only while it is.
	/// </summary>
	private sealed class SimulatedPeer
	{
		private readonly int _seed;
		private readonly HashSet<Dpnid> _players = [];
		private readonly HashSet<Dpnid> _groups = [];

		public IPAddress Address { get; }

		public Peer Peer { get; }

		public List<PeerEvent> Events { get; } = [];

		/// <summary>
		/// Closed or cut off by the test: no longer counted among the peers in the session.
		/// </summary>
		public bool Gone { get; set; }

		public SimulatedPeer(Simulation simulation, IPAddress address)
		{
			_seed = simulation.Seed;
			Address = address;
			Peer = new Peer(simulation.Options(address));
			simulation.Peers.Add(this);
		}

		public Dpnid Id => Peer.LocalPlayer;

		private void Require(bool holds, PeerEvent peerEvent, string what)
		{
			if (!holds)
				throw new InvalidOperationException(
					$"{Address} raised {peerEvent} {what} (seed {_seed}); before: {string.Join(", ", Events.TakeLast(8))}"
				);
		}

		private void Follow(PeerEvent peerEvent)
		{
			switch (peerEvent)
			{
				case PeerEvent.PlayerCreated m:
					Require(_players.Add(m.Player.Id), m, "for a player already created");
					break;
				case PeerEvent.PlayerDestroyed m:
					Require(_players.Remove(m.Player), m, "for a player not created");
					break;
				case PeerEvent.DataReceived m:
					Require(_players.Contains(m.Sender), m, "from a player not created");
					break;
				case PeerEvent.HostMigrated m:
					Require(_players.Contains(m.NewHost), m, "for a player not created");
					break;
				case PeerEvent.GroupCreated m:
					Require(_groups.Add(m.Group.Id), m, "for a group already created");
					break;
				case PeerEvent.GroupDestroyed m:
					Require(_groups.Remove(m.Group), m, "for a group not created");
					break;
				case PeerEvent.PlayerAddedToGroup m:
					Require(
						_groups.Contains(m.Group) && _players.Contains(m.Player),
						m,
						"for a group or player not created"
					);
					break;
				case PeerEvent.PlayerRemovedFromGroup m:
					Require(
						_groups.Contains(m.Group) && _players.Contains(m.Player),
						m,
						"for a group or player not created"
					);
					break;
				case PeerEvent.ConnectCompleted m:
					Require(!Events.OfType<PeerEvent.ConnectCompleted>().Any(), m, "a second time");
					break;
			}
			Events.Add(peerEvent);
		}

		public SimulatedPeer Drain()
		{
			while (Peer.TryGetEvent(out var peerEvent))
				Follow(peerEvent);
			return this;
		}

		public IEnumerable<T> Raised<T>()
			where T : PeerEvent => Drain().Events.OfType<T>();

		/// <summary>
		/// Follows the events raised since, and checks the snapshot: one host at most, and groups of its players.
		/// </summary>
		public void Check()
		{
			Drain();
			var players = Peer.Players.Select(p => p.Id).ToHashSet();
			if (
				Peer.Players.Count(p => p.IsHost) > 1
				|| Peer.Groups.Any(g => !g.Members.All(players.Contains))
			)
				throw new InvalidOperationException(
					$"{Address} shows {Peer.Players.Count(p => p.IsHost)} hosts or a group with members not in it (seed {_seed})"
				);
		}

		/// <summary>
		/// In the session: its own player created, and neither gone nor terminated since.
		/// </summary>
		public bool InSession => !Gone && Id != Dpnid.Empty && Drain()._players.Contains(Id);

		/// <summary>
		/// Done with joining, in or out.
		/// </summary>
		public bool Settled =>
			Gone
			|| InSession
			|| Raised<PeerEvent.SessionTerminated>().Any()
			|| Raised<PeerEvent.ConnectCompleted>().Any(c => c.Result != ResultCode.Success);
	}

	private static ApplicationDescription Application(
		int maxPlayers = 0,
		SessionFlags flags = default
	) =>
		new()
		{
			GuidApplication = GuidApplication,
			MaxPlayers = maxPlayers,
			Flags = flags,
		};

	private static void Drain(Client client, List<PeerEvent> events)
	{
		while (client.TryGetEvent(out var peerEvent))
			events.Add(peerEvent);
	}

	private static void Drain(Server server, List<PeerEvent> events)
	{
		while (server.TryGetEvent(out var peerEvent))
			events.Add(peerEvent);
	}

	private static byte[] Message(int sender, int number)
	{
		var data = new byte[8];
		BinaryPrimitives.WriteInt32LittleEndian(data, sender);
		BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(4), number);
		return data;
	}

	private static (int Sender, int Number) ReadMessage(PeerEvent.DataReceived data) =>
		(
			BinaryPrimitives.ReadInt32LittleEndian(data.Data.AsSpan()),
			BinaryPrimitives.ReadInt32LittleEndian(data.Data.AsSpan()[4..])
		);

	private static Simulation ClientServerSession(
		int seed,
		double lossRate = 0,
		double duplicateRate = 0
	)
	{
		const int ClientCount = 3;
		const int MessageCount = 5;
		var simulation = new Simulation(seed);
		simulation.Network.LossRate = lossRate;
		simulation.Network.DuplicateRate = duplicateRate;
		var server = new Server(simulation.Options(ServerEndPoint.Address, loops: 2));
		var serverEvents = new List<PeerEvent>();
		var hosting = server.HostAsync(Application(), ServerEndPoint);
		simulation
			.RunUntil(() => hosting.IsCompleted, TimeSpan.FromSeconds(5))
			.Should()
			.BeTrue($"the server hosts (seed {seed})");

		var clients = Enumerable
			.Range(0, ClientCount)
			.Select(i => new Client(simulation.Options(IPAddress.Parse($"10.7.1.{i + 1}"))))
			.ToList();
		var clientEvents = clients.Select(_ => new List<PeerEvent>()).ToList();
		foreach (var client in clients)
			_ = client.ConnectAsync(Application(), ServerEndPoint);

		bool AllJoined()
		{
			for (var i = 0; i < ClientCount; i++)
				Drain(clients[i], clientEvents[i]);
			Drain(server, serverEvents);
			return clientEvents.All(e => e.OfType<PeerEvent.ConnectCompleted>().Any())
				&& serverEvents.OfType<PeerEvent.PlayerCreated>().Count(p => !p.Player.IsLocal)
					== ClientCount;
		}

		simulation
			.RunUntil(AllJoined, TimeSpan.FromSeconds(60))
			.Should()
			.BeTrue($"every client joins (seed {seed})");
		clientEvents
			.Select(e => e.OfType<PeerEvent.ConnectCompleted>().First().Result)
			.Should()
			.AllBeEquivalentTo(ResultCode.Success, $"seed {seed}");

		for (var number = 0; number < MessageCount; number++)
		{
			for (var i = 0; i < ClientCount; i++)
				clients[i].Send(Message(i, number));
		}

		List<(int Sender, int Number)> Received() =>
			[.. serverEvents.OfType<PeerEvent.DataReceived>().Select(ReadMessage)];

		simulation
			.RunUntil(
				() =>
				{
					Drain(server, serverEvents);
					return Received().Count >= ClientCount * MessageCount;
				},
				TimeSpan.FromSeconds(60)
			)
			.Should()
			.BeTrue($"the server receives every message (seed {seed})");
		foreach (var group in Received().GroupBy(r => r.Sender))
			group
				.Select(r => r.Number)
				.Should()
				.Equal(
					Enumerable.Range(0, MessageCount),
					$"one client's messages come once each, in order (seed {seed})"
				);

		var closing = clients.Select(c => c.CloseAsync()).Append(server.CloseAsync()).ToList();
		simulation
			.RunUntil(() => closing.All(c => c.IsCompleted), TimeSpan.FromSeconds(60))
			.Should()
			.BeTrue($"everyone closes (seed {seed})");
		return simulation;
	}

	[Fact]
	public void ClientServer_JoinsSendsAndCloses_InEveryOrderTried()
	{
		for (var seed = 0; seed < Seeds; seed++)
			ClientServerSession(seed);
	}

	// A fifth of the datagrams lost, the rest reordered: the reliable protocol still gets every guaranteed message
	// through, once, in order.
	[Fact]
	public void ClientServer_LosingAFifthOfTheDatagrams_StillDeliversEachMessageOnceInOrder()
	{
		for (var seed = 0; seed < Seeds; seed++)
			ClientServerSession(seed, lossRate: 0.2);
	}

	// A tenth of the datagrams delivered twice, on top of the losses: a copy is acknowledged and dropped, never
	// delivered to the application again.
	[Fact]
	public void ClientServer_DuplicatingDatagrams_StillDeliversEachMessageOnceInOrder()
	{
		for (var seed = 0; seed < Seeds; seed++)
			ClientServerSession(seed, lossRate: 0.1, duplicateRate: 0.1);
	}

	// The same seed takes the same order again, to every byte on the wire and every tick of the clock, losses
	// included; another seed takes another.
	[Fact]
	public void SameSeed_TakesTheSameOrderAgain()
	{
		for (var seed = 0; seed < 5; seed++)
		{
			var first = ClientServerSession(seed, lossRate: 0.2).Network.Trace;
			var second = ClientServerSession(seed, lossRate: 0.2).Network.Trace;
			var differs = Enumerable
				.Range(0, Math.Min(first.Count, second.Count))
				.FirstOrDefault(i => first[i] != second[i], -1);
			if (differs >= 0)
				throw new Exception(
					$"seed {seed} differs at {differs} of {first.Count}/{second.Count}:\n{string.Join("\n", first.Skip(Math.Max(0, differs - 3)).Take(5))}\n---\n{string.Join("\n", second.Skip(Math.Max(0, differs - 3)).Take(5))}"
				);
			second.Should().Equal(first, $"seed {seed} repeats");
		}

		ClientServerSession(1, lossRate: 0.2)
			.Network.Trace.Should()
			.NotEqual(ClientServerSession(2, lossRate: 0.2).Network.Trace);
	}

	// Which loop runs a partner's connections depends on its address alone, not on a hash that each process makes
	// differently, so that a seed repeats from one run of the tests to the next.
	[Fact]
	public void LoopOfAnAddress_IsTheSameInEveryProcess()
	{
		var loops = Enumerable
			.Range(1, 8)
			.Select(i =>
				ReliableEndpoint.LoopIndex(new IPEndPoint(IPAddress.Parse($"10.7.1.{i}"), 40000), 4)
			)
			.ToList();

		loops.Should().Equal(0, 1, 2, 3, 0, 1, 2, 3);
	}

	private static void RejectedJoin(int seed)
	{
		var simulation = new Simulation(seed);
		var server = new Server(simulation.Options(ServerEndPoint.Address, loops: 2));
		var hosting = server.HostAsync(Application(maxPlayers: 1), ServerEndPoint);
		simulation.RunUntil(() => hosting.IsCompleted, TimeSpan.FromSeconds(5)).Should().BeTrue();

		var clientAddress = IPAddress.Parse("10.7.2.1");
		var client = new Client(simulation.Options(clientAddress));
		var events = new List<PeerEvent>();
		_ = client.ConnectAsync(Application(), ServerEndPoint);
		simulation
			.RunUntil(
				() =>
				{
					Drain(client, events);
					return events.OfType<PeerEvent.ConnectCompleted>().Any();
				},
				TimeSpan.FromSeconds(30)
			)
			.Should()
			.BeTrue($"the join ends (seed {seed})");
		events
			.OfType<PeerEvent.ConnectCompleted>()
			.Single()
			.Result.Should()
			.NotBe(ResultCode.Success, $"the server is full (seed {seed})");

		var closing = client.CloseAsync();
		simulation
			.RunUntil(() => closing.IsCompleted, TimeSpan.FromSeconds(30))
			.Should()
			.BeTrue($"the client closes (seed {seed})");
		var serverClosing = server.CloseAsync();
		simulation
			.RunUntil(() => serverClosing.IsCompleted, TimeSpan.FromSeconds(30))
			.Should()
			.BeTrue();
		simulation.RunUntil(() => false, TimeSpan.FromSeconds(30));

		// The failed join closed the client's transport only once its connection had ended, and so had said goodbye
		// (HARD_DISCONNECT) if it still had to: nothing was sent after the transport was disposed, whatever the order
		// of the session's, the endpoint's and the loop's messages.
		var transport = simulation.Network.Transports.Single(t =>
			t.LocalEndPoint.Address.Equals(clientAddress)
		);
		transport.IsDisposed.Should().BeTrue($"seed {seed}");
		transport
			.Log.Should()
			.NotContain(e => e.StartsWith("Lost", StringComparison.Ordinal), $"seed {seed}");
	}

	[Fact]
	public void RejectedJoin_SaysGoodbyeBeforeClosing_InEveryOrderTried()
	{
		for (var seed = 0; seed < Seeds; seed++)
			RejectedJoin(seed);
	}

	/// <summary>
	/// Hosts a peer-to-peer session and has <paramref name="joiners"/> peers join it all at once, then waits until
	/// every peer sees every player; checks that they all see the same players, and the same host.
	/// </summary>
	private static List<SimulatedPeer> Mesh(Simulation simulation, int joiners, SessionFlags flags)
	{
		var seed = simulation.Seed;
		var host = new SimulatedPeer(simulation, IPAddress.Parse("10.8.0.1"));
		var hosting = host.Peer.HostAsync(Application(flags: flags), ServerEndPoint.Port);
		simulation
			.RunUntil(() => hosting.IsCompleted, TimeSpan.FromSeconds(5))
			.Should()
			.BeTrue($"the host hosts (seed {seed})");

		var peers = new List<SimulatedPeer> { host };
		var hostEndPoint = new IPEndPoint(host.Address, ServerEndPoint.Port);
		for (var i = 1; i <= joiners; i++)
		{
			var joiner = new SimulatedPeer(simulation, IPAddress.Parse($"10.8.0.{i + 1}"));
			_ = joiner.Peer.ConnectAsync(Application(flags: flags), hostEndPoint);
			peers.Add(joiner);
		}

		simulation
			.RunUntil(
				() =>
					peers.Skip(1).All(p => p.Raised<PeerEvent.ConnectCompleted>().Any())
					&& peers.All(p => p.Drain().Peer.Players.Count == peers.Count),
				TimeSpan.FromSeconds(60)
			)
			.Should()
			.BeTrue(
				$"every peer joins and sees every player (seed {seed}): "
					+ string.Join(
						"; ",
						peers.Select(p =>
							$"{p.Address} {string.Join(",", p.Drain().Events.Select(e => e.ToString()))} sees {p.Peer.Players.Count}"
						)
					)
			);
		peers
			.Skip(1)
			.Select(p => p.Raised<PeerEvent.ConnectCompleted>().Single().Result)
			.Should()
			.AllBeEquivalentTo(ResultCode.Success, $"seed {seed}");

		var ids = peers.Select(p => p.Id).OrderBy(id => id.Value).ToList();
		foreach (var peer in peers)
		{
			peer.Peer.Players.Select(p => p.Id)
				.OrderBy(id => id.Value)
				.Should()
				.Equal(ids, $"every peer sees the same players (seed {seed})");
			peer.Peer.Players.Single(p => p.IsHost).Id.Should().Be(host.Id, $"seed {seed}");
			peer.Raised<PeerEvent.PlayerCreated>()
				.Select(p => p.Player.Id)
				.Should()
				.OnlyHaveUniqueItems($"a player is created once (seed {seed})");
		}

		return peers;
	}

	private static void CloseAll(Simulation simulation, IEnumerable<SimulatedPeer> peers)
	{
		var closing = peers.Select(p => p.Peer.CloseAsync()).ToList();
		simulation
			.RunUntil(() => closing.All(c => c.IsCompleted), TimeSpan.FromSeconds(60))
			.Should()
			.BeTrue($"everyone closes (seed {simulation.Seed})");
	}

	private static void PeerToPeerMesh(int seed, double lossRate = 0, double duplicateRate = 0)
	{
		const int MessageCount = 3;
		var simulation = new Simulation(seed);
		simulation.Network.LossRate = lossRate;
		simulation.Network.DuplicateRate = duplicateRate;
		var peers = Mesh(simulation, joiners: 3, flags: default);

		for (var number = 0; number < MessageCount; number++)
		{
			for (var i = 0; i < peers.Count; i++)
				peers[i]
					.Peer.SendTo(
						Peer.AllPlayers,
						Message(i, number),
						SendFlags.Guaranteed | SendFlags.NoLoopback
					);
		}

		List<(int Sender, int Number)> Received(SimulatedPeer peer) =>
			[.. peer.Raised<PeerEvent.DataReceived>().Select(ReadMessage)];

		simulation
			.RunUntil(
				() => peers.All(p => Received(p).Count >= (peers.Count - 1) * MessageCount),
				TimeSpan.FromSeconds(60)
			)
			.Should()
			.BeTrue($"every peer receives every other's messages (seed {seed})");
		for (var i = 0; i < peers.Count; i++)
		{
			var received = Received(peers[i]);
			received.Should().NotContain(r => r.Sender == i, $"no loopback (seed {seed})");
			foreach (var group in received.GroupBy(r => r.Sender))
				group
					.Select(r => r.Number)
					.Should()
					.Equal(
						Enumerable.Range(0, MessageCount),
						$"one peer's messages come once each, in order (seed {seed})"
					);
		}

		CloseAll(simulation, peers);
	}

	// Three peers join a host at once: each connects to the host, then to the others as the host instructs, whatever
	// the order of all their handshakes.
	[Fact]
	public void PeerToPeer_JoinAtOnceAndBroadcast_InEveryOrderTried()
	{
		for (var seed = 0; seed < Seeds; seed++)
			PeerToPeerMesh(seed);
	}

	private static async Task HostMigrationAsync(int seed, double lossRate = 0)
	{
		var simulation = new Simulation(seed);
		simulation.Network.LossRate = lossRate;
		var peers = Mesh(simulation, joiners: 3, SessionFlags.MigrateHost);
		var (oldHost, survivors) = (peers[0], peers.Skip(1).ToList());
		var oldHostId = oldHost.Id;

		// The host leaves, by closing in even seeds, and by its machine stopping in odd ones, when the others hear
		// nothing more from it and give up on it themselves.
		var crashed = seed % 2 == 1;
		Leave(simulation, oldHost, crashed);

		simulation
			.RunUntil(
				() =>
					survivors.All(p =>
						p.Drain().Peer.Players.Count == survivors.Count
						&& p.Peer.Players.Count(i => i.IsHost) == 1
						&& p.Peer.Players.Single(i => i.IsHost).Id != oldHostId
					),
				TimeSpan.FromSeconds(300)
			)
			.Should()
			.BeTrue(
				$"the others agree on a new host (seed {seed}, {(crashed ? "crashed" : "closed")})"
			);

		var newHost = survivors[0].Peer.Players.Single(p => p.IsHost).Id;
		foreach (var peer in survivors)
		{
			peer.Peer.Players.Single(p => p.IsHost)
				.Id.Should()
				.Be(newHost, $"one new host (seed {seed})");
			peer.Raised<PeerEvent.HostMigrated>()
				.Select(m => m.NewHost)
				.Should()
				.Equal([newHost], $"each peer hears of the new host once (seed {seed})");
			peer.Raised<PeerEvent.PlayerDestroyed>()
				.Select(d => d.Player)
				.Should()
				.Equal([oldHostId], $"the old host goes, once (seed {seed})");
			peer.Raised<PeerEvent.SessionTerminated>().Should().BeEmpty($"seed {seed}");
		}

		// The new host runs the name table: a group it makes reaches everyone.
		var host = survivors.Single(p => p.Id == newHost);
		var creating = host.Peer.CreateGroupAsync("After migration");
		simulation
			.RunUntil(
				() =>
					creating.IsCompleted
					&& survivors.All(p => p.Raised<PeerEvent.GroupCreated>().Any()),
				TimeSpan.FromSeconds(60)
			)
			.Should()
			.BeTrue($"the new host's group reaches every peer (seed {seed})");
		var created = await creating;
		foreach (var peer in survivors)
			peer.Raised<PeerEvent.GroupCreated>()
				.Single()
				.Group.Id.Should()
				.Be(created, $"seed {seed}");

		CloseAll(simulation, peers);
	}

	// The host of four peers leaves, by closing or by stopping: the three others elect the same new host, each hears
	// of it once, and the new host's name table changes reach them all.
	[Fact]
	public async Task HostMigration_TheOthersAgreeOnANewHost_InEveryOrderTried()
	{
		for (var seed = 0; seed < Seeds; seed++)
			await HostMigrationAsync(seed);
	}

	[Fact]
	public void PeerToPeer_LosingAndDuplicatingDatagrams_InEveryOrderTried()
	{
		for (var seed = 0; seed < Seeds; seed++)
			PeerToPeerMesh(seed, lossRate: 0.1, duplicateRate: 0.1);
	}

	[Fact]
	public async Task HostMigration_LosingDatagrams_InEveryOrderTried()
	{
		for (var seed = 0; seed < Seeds; seed++)
			await HostMigrationAsync(seed, lossRate: 0.1);
	}

	/// <summary>
	/// A peer leaves: it closes, or its machine stops, when the others hear nothing more from it.
	/// </summary>
	private static void Leave(Simulation simulation, SimulatedPeer peer, bool crashes)
	{
		peer.Gone = true;
		if (crashes)
			simulation.Network.CutOff(peer.Address);
		else
			_ = peer.Peer.CloseAsync();
	}

	/// <summary>
	/// Waits until every peer is done joining, in or out, and the peers in the session agree on who is in it and who
	/// hosts it; then checks that each reaches every other, with a message to all. Returns the peers in the session.
	/// </summary>
	private static List<SimulatedPeer> Settle(
		Simulation simulation,
		List<SimulatedPeer> peers,
		string what
	)
	{
		var seed = simulation.Seed;
		List<SimulatedPeer> InSession() => [.. peers.Where(p => p.InSession)];

		bool Agree()
		{
			if (!peers.All(p => p.Settled))
				return false;

			var live = InSession();
			var ids = live.Select(p => p.Id).ToHashSet();
			var hosts = live.Select(p =>
					p.Peer.Players.Where(i => i.IsHost).Select(i => i.Id).ToList()
				)
				.ToList();
			return live.Count > 0
				&& live.All(p => p.Peer.Players.Select(i => i.Id).ToHashSet().SetEquals(ids))
				&& hosts.All(h => h.Count == 1 && h[0] == hosts[0][0])
				&& ids.Contains(hosts[0][0]);
		}

		simulation
			.RunUntil(Agree, TimeSpan.FromSeconds(600))
			.Should()
			.BeTrue(
				$"the peers in the session agree on its players and host (seed {seed}, {what}): "
					+ string.Join(
						"; ",
						peers.Select(p =>
							$"{p.Address}{(p.Gone ? " gone" : "")} in={p.InSession} sees [{string.Join(",", p.Peer.Players.Select(i => $"{i.Id}{(i.IsHost ? "*" : "")}"))}] events {string.Join(",", p.Events.Select(e => e.GetType().Name))}"
						)
					)
			);

		var live = InSession();
		var round = simulation.Random.Next(1000, int.MaxValue);
		foreach (var peer in live)
			peer.Peer.SendTo(
				Peer.AllPlayers,
				Message(round, 0),
				SendFlags.Guaranteed | SendFlags.NoLoopback
			);

		HashSet<Dpnid> Heard(SimulatedPeer peer) =>
			[
				.. peer.Raised<PeerEvent.DataReceived>()
					.Where(d => ReadMessage(d).Sender == round)
					.Select(d => d.Sender),
			];

		simulation
			.RunUntil(
				() => live.All(p => Heard(p).SetEquals(live.Where(o => o != p).Select(o => o.Id))),
				TimeSpan.FromSeconds(120)
			)
			.Should()
			.BeTrue($"every peer in the session reaches every other (seed {seed}, {what})");
		return live;
	}

	/// <summary>
	/// A new peer joins through the host the session has now, and gets in: what the session went through left no
	/// trace in its name table that a newcomer would wait for, such as a player whose join never completed.
	/// </summary>
	private static void JoinAfterwards(
		Simulation simulation,
		List<SimulatedPeer> peers,
		List<SimulatedPeer> live,
		string what
	)
	{
		var hostId = live[0].Peer.Players.Single(p => p.IsHost).Id;
		var host = live.Single(p => p.Id == hostId);
		var newcomer = new SimulatedPeer(simulation, IPAddress.Parse("10.8.0.20"));
		_ = newcomer.Peer.ConnectAsync(
			Application(flags: SessionFlags.MigrateHost),
			host.Peer.LocalEndPoint!
		);
		peers.Add(newcomer);
		Settle(simulation, peers, $"{what}, then a newcomer")
			.Should()
			.Contain(newcomer, $"a newcomer gets in afterwards (seed {simulation.Seed}, {what})");
	}

	private static void HostLeavesWhileAPeerJoins(int seed)
	{
		var simulation = new Simulation(seed);
		var peers = Mesh(simulation, joiners: 2, SessionFlags.MigrateHost);
		var host = peers[0];
		var joiner = new SimulatedPeer(simulation, IPAddress.Parse("10.8.0.9"));
		_ = joiner.Peer.ConnectAsync(
			Application(flags: SessionFlags.MigrateHost),
			new IPEndPoint(host.Address, ServerEndPoint.Port)
		);
		peers.Add(joiner);

		var steps = simulation.Random.Next(0, 120);
		simulation.Run(steps);
		var crashes = seed % 2 == 1;
		Leave(simulation, host, crashes);

		var what = $"host {(crashes ? "crashed" : "closed")} after {steps} steps of a join";
		var live = Settle(simulation, peers, what);
		live.Should().NotContain(host);
		JoinAfterwards(simulation, peers, live, what);
		CloseAll(simulation, peers);
	}

	// A peer joins a session of three, and the host leaves at a point of the join that the seed chooses: whether the
	// joiner gets in or not, the peers left agree on who is in and who hosts, and reach each other.
	[Fact]
	public void HostLeavesWhileAPeerJoins_InEveryOrderTried()
	{
		for (var seed = 0; seed < Seeds; seed++)
			HostLeavesWhileAPeerJoins(seed);
	}

	private static void PeerLeavesDuringMigration(int seed)
	{
		var simulation = new Simulation(seed);
		var peers = Mesh(simulation, joiners: 3, SessionFlags.MigrateHost);
		Leave(simulation, peers[0], crashes: seed % 2 == 1);

		var steps = simulation.Random.Next(0, 300);
		simulation.Run(steps);
		var leaving = peers[1 + simulation.Random.Next(0, 3)];
		var crashes = seed / 2 % 2 == 1;
		Leave(simulation, leaving, crashes);

		var what =
			$"{leaving.Address} {(crashes ? "crashed" : "closed")} {steps} steps into the migration";
		var live = Settle(simulation, peers, what);
		live.Should().HaveCount(2, $"seed {seed}");
		JoinAfterwards(simulation, peers, live, what);
		CloseAll(simulation, peers);
	}

	// The host of four leaves, and then, at a point of the migration that the seed chooses, one of the others too: the
	// two left agree on who is in and who hosts.
	[Fact]
	public void PeerLeavesDuringMigration_InEveryOrderTried()
	{
		for (var seed = 0; seed < Seeds; seed++)
			PeerLeavesDuringMigration(seed);
	}

	private static void PeerLeavesWhileAnotherJoins(int seed)
	{
		var simulation = new Simulation(seed);
		var peers = Mesh(simulation, joiners: 2, SessionFlags.MigrateHost);
		var joiner = new SimulatedPeer(simulation, IPAddress.Parse("10.8.0.9"));
		_ = joiner.Peer.ConnectAsync(
			Application(flags: SessionFlags.MigrateHost),
			new IPEndPoint(peers[0].Address, ServerEndPoint.Port)
		);
		peers.Add(joiner);

		var steps = simulation.Random.Next(0, 120);
		simulation.Run(steps);
		var leaving = peers[1 + simulation.Random.Next(0, 2)];
		var crashes = seed % 2 == 1;
		Leave(simulation, leaving, crashes);

		var what =
			$"{leaving.Address} {(crashes ? "crashed" : "closed")} after {steps} steps of a join";
		var live = Settle(simulation, peers, what);
		live.Should().Contain(peers[0], $"the host stays (seed {seed})");
		JoinAfterwards(simulation, peers, live, what);
		CloseAll(simulation, peers);
	}

	// A peer joins a session of three, and one of the peers already in leaves at a point of the join that the seed
	// chooses: the joiner gets in or not, and the peers left agree and reach each other.
	[Fact]
	public void PeerLeavesWhileAnotherJoins_InEveryOrderTried()
	{
		for (var seed = 0; seed < Seeds; seed++)
			PeerLeavesWhileAnotherJoins(seed);
	}
}
