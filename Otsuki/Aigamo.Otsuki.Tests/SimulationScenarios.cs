using System.Net;
using Aigamo.Otsuki.Messages.Core;
using FluentAssertions;

namespace Aigamo.Otsuki.Tests;

/// <summary>
/// A scenario of the simulation, by name: one run of it from a seed, in the conditions given; it throws if something
/// does not hold. <see cref="MayBeRough"/> says whether it checks only what holds whatever the network does, and so
/// can run in faults beyond what the protocol is meant to ride out.
/// </summary>
internal sealed record SimulationScenario(
	string Name,
	Action<int, SimulationConditions> Run,
	bool MayBeRough = false
);

/// <summary>
/// The scenarios that <see cref="SimulationTests"/> runs on a hundred seeds each, and the swarm on many more, each in
/// conditions of its seed.
/// </summary>
internal static class SimulationScenarios
{
	private static readonly Guid GuidApplication = new("0c5a1e7d-2b9f-4e61-8d3a-7f4b6c2e9a15");

	private static readonly IPEndPoint ServerEndPoint = new(IPAddress.Parse("10.7.0.1"), 2302);

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

	public static Simulation ClientServerSession(int seed, SimulationConditions conditions)
	{
		const int ClientCount = 3;
		const int MessageCount = 5;
		var simulation = new Simulation(seed, conditions);
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
				clients[i].Send(SimulatedPeer.Message(i, number));
		}

		List<(int Sender, int Number)> Received() =>
			[
				.. serverEvents
					.OfType<PeerEvent.DataReceived>()
					.Select(SimulatedPeer.ReadMessage),
			];

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

	public static void RejectedJoin(int seed, SimulationConditions conditions)
	{
		var simulation = new Simulation(seed, conditions);
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

	public static void PeerToPeerMesh(int seed, SimulationConditions conditions)
	{
		const int MessageCount = 3;
		var simulation = new Simulation(seed, conditions);
		var peers = Mesh(simulation, joiners: 3, flags: default);

		for (var number = 0; number < MessageCount; number++)
		{
			for (var i = 0; i < peers.Count; i++)
				peers[i]
					.Peer.SendTo(
						Peer.AllPlayers,
						SimulatedPeer.Message(i, number),
						SendFlags.Guaranteed | SendFlags.NoLoopback
					);
		}

		List<(int Sender, int Number)> Received(SimulatedPeer peer) =>
			[.. peer.Raised<PeerEvent.DataReceived>().Select(SimulatedPeer.ReadMessage)];

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

	public static void HostMigration(int seed, SimulationConditions conditions)
	{
		var simulation = new Simulation(seed, conditions);
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
		foreach (var peer in survivors)
			peer.Raised<PeerEvent.GroupCreated>()
				.Single()
				.Group.Id.Should()
				.Be(creating.Result, $"seed {seed}");

		CloseAll(simulation, peers);
	}

	private static string Describe(IEnumerable<SimulatedPeer> peers) =>
		string.Join(
			"; ",
			peers.Select(p =>
				$"{p.Address}{(p.Gone ? " gone" : "")} in={p.InSession} sees {p.View()} events {string.Join(",", p.Events.Select(e => e.GetType().Name))}"
			)
		);

	/// <summary>
	/// Whether the peers in the session hold together: the peers that show one name table
	/// (<see cref="SimulatedPeer.View"/>) are the players it lists, and one of them hosts it; and each peer's events add
	/// up to its snapshot. Unless the session <paramref name="mayDivide"/>, every peer shows the same, but for
	/// <paramref name="outcasts"/>, peers the host was asked to remove, which may stand apart: a host that leaves as it
	/// removes one may tell the others and not it. A session that split may give two players one DPNID, one in each
	/// part, so the parts go by what they show, not by DPNIDs.
	/// </summary>
	private static bool HoldTogether(
		List<SimulatedPeer> live,
		bool mayDivide,
		IReadOnlySet<SimulatedPeer> outcasts
	)
	{
		var parts = live.GroupBy(p => p.View()).ToList();
		if (!mayDivide && parts.Count(part => !part.All(outcasts.Contains)) > 1)
			return false;

		foreach (var part in parts)
		{
			var first = part.First();
			var hosts = first.Hosts();
			if (
				hosts.Count != 1
				|| !first.Seen().SetEquals(part.Select(p => p.Id))
				|| part.Count() != first.Seen().Count
			)
				return false;
		}

		return live.All(p => p.EventsMatchSnapshot());
	}

	/// <summary>
	/// Waits until every peer is done joining, in or out, and the peers in the session hold together
	/// (<see cref="HoldTogether"/>); then checks that each reaches every other it sees, with a message to all.
	/// Returns the peers in the session, in it or, if it <paramref name="mayDivide"/>, in any of the sessions it split
	/// into; none, if it <paramref name="mayEnd"/> and did. <paramref name="outcasts"/> may stand apart
	/// (<see cref="HoldTogether"/>).
	/// </summary>
	private static List<SimulatedPeer> Settle(
		Simulation simulation,
		List<SimulatedPeer> peers,
		string what,
		bool mayDivide = false,
		bool mayEnd = false,
		IReadOnlySet<SimulatedPeer>? outcasts = null
	)
	{
		var seed = simulation.Seed;
		List<SimulatedPeer> InSession() => [.. peers.Where(p => p.InSession)];

		bool Agree()
		{
			if (!peers.All(p => p.Settled))
				return false;

			var live = InSession();
			return live.Count > 0 ? HoldTogether(live, mayDivide, outcasts ?? new HashSet<SimulatedPeer>()) : mayEnd;
		}

		simulation
			.RunUntil(Agree, TimeSpan.FromSeconds(600))
			.Should()
			.BeTrue(
				$"the peers in the session agree on its players and host (seed {seed}, {what}): {Describe(peers)}"
			);

		var round = simulation.Random.Next(SimulatedPeer.MostTag + 1, int.MaxValue);
		foreach (var peer in InSession())
			peer.Peer.SendTo(
				Peer.AllPlayers,
				SimulatedPeer.Message(round, 0),
				SendFlags.Guaranteed | SendFlags.NoLoopback
			);

		HashSet<Dpnid> Heard(SimulatedPeer peer) =>
			[
				.. peer.Raised<PeerEvent.DataReceived>()
					.Where(d => SimulatedPeer.ReadMessage(d).Tag == round)
					.Select(d => d.Sender),
			];

		// From every other it sees, and perhaps from one that has left since.
		bool Reached(SimulatedPeer peer)
		{
			var others = peer.Seen();
			others.Remove(peer.Id);
			return Heard(peer).IsSupersetOf(others);
		}

		simulation
			.RunUntil(() => InSession().All(Reached), TimeSpan.FromSeconds(120))
			.Should()
			.BeTrue(
				$"every peer in the session reaches every other (seed {seed}, {what}): {string.Join("; ", InSession().Select(p => $"{p.Address} heard [{string.Join(",", Heard(p))}]"))}; {Describe(peers)}"
			);

		// What was still under way when they first agreed, such as an integrity check that a fault started, may have
		// changed the session since: they agree again.
		simulation
			.RunUntil(Agree, TimeSpan.FromSeconds(600))
			.Should()
			.BeTrue(
				$"the peers in the session agree again after reaching each other (seed {seed}, {what}): {Describe(peers)}"
			);
		return InSession();
	}

	/// <summary>
	/// A new peer joins through the host that the first of <paramref name="live"/> sees, but for
	/// <paramref name="outcasts"/>, and gets in: what the session went through left no trace in its name table that a
	/// newcomer would wait for, such as a player whose join never completed.
	/// </summary>
	private static void JoinAfterwards(
		Simulation simulation,
		List<SimulatedPeer> peers,
		List<SimulatedPeer> live,
		string what,
		bool mayDivide = false,
		IReadOnlySet<SimulatedPeer>? outcasts = null
	)
	{
		var first = live.FirstOrDefault(p => outcasts?.Contains(p) != true) ?? live[0];
		var hostId = first.Peer.Players.Single(p => p.IsHost).Id;
		var host = live.Single(p => p.Id == hostId && p.View() == first.View());
		var newcomer = new SimulatedPeer(simulation, IPAddress.Parse("10.8.0.20"));
		_ = newcomer.Peer.ConnectAsync(
			Application(flags: host.Peer.ApplicationDescription!.Flags),
			host.Peer.LocalEndPoint!
		);
		peers.Add(newcomer);
		Settle(simulation, peers, $"{what}, then a newcomer", mayDivide, outcasts: outcasts)
			.Should()
			.Contain(newcomer, $"a newcomer gets in afterwards (seed {simulation.Seed}, {what})");
	}

	public static void HostLeavesWhileAPeerJoins(int seed, SimulationConditions conditions)
	{
		var simulation = new Simulation(seed, conditions);
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

	public static void PeerLeavesDuringMigration(int seed, SimulationConditions conditions)
	{
		var simulation = new Simulation(seed, conditions);
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

	public static void PeerLeavesWhileAnotherJoins(int seed, SimulationConditions conditions)
	{
		var simulation = new Simulation(seed, conditions);
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

	/// <summary>
	/// For exploring orders: a session of <paramref name="peers"/>, migrating its host, from which at one moment the
	/// host leaves if <paramref name="hostLeaves"/>, and <paramref name="othersLeaving"/> of the others, by closing or,
	/// if <paramref name="crashes"/>, by their machines stopping, while a newcomer joins if <paramref name="joins"/>.
	/// The peers left must hold together and reach each other. The moment is marked (<see cref="Simulation.Mark"/>):
	/// the session forms in the order of scheduling, and only what follows is explored.
	/// </summary>
	public static void Departures(
		SimulationConditions conditions,
		int peers,
		bool hostLeaves,
		int othersLeaving,
		bool crashes,
		bool joins
	)
	{
		var simulation = new Simulation(0, conditions);
		var session = Mesh(simulation, joiners: peers - 1, SessionFlags.MigrateHost);
		simulation.Mark();
		var leaving = session.Skip(1).TakeLast(othersLeaving).ToList();
		if (hostLeaves)
			leaving.Insert(0, session[0]);
		if (joins)
		{
			var joiner = new SimulatedPeer(simulation, IPAddress.Parse("10.8.0.9"));
			_ = joiner.Peer.ConnectAsync(
				Application(flags: SessionFlags.MigrateHost),
				new IPEndPoint(session[0].Address, ServerEndPoint.Port)
			);
			session.Add(joiner);
		}
		foreach (var peer in leaving)
			Leave(simulation, peer, crashes);

		var what =
			$"{string.Join(", ", leaving.Select(p => p.Address))} {(crashes ? "crashed" : "closed")}{(joins ? " as a peer joined" : "")}";
		var live = Settle(simulation, session, what);
		if (!joins)
			live.Should().HaveCount(peers - leaving.Count, $"the others stay ({what})");
	}

	// One change to the session that Churn makes, by a peer in it; what it did, or null if it could not. A peer leaves,
	// or the host removes one, only while another would stay, counting those it was asked to remove as gone.
	private static string? Change(
		Simulation simulation,
		List<SimulatedPeer> peers,
		SimulatedPeer peer,
		Dictionary<SimulatedPeer, int> sent,
		HashSet<SimulatedPeer> removed
	)
	{
		var random = simulation.Random;
		var live = peers.Where(p => p.InSession).ToList();
		var staying = live.Where(p => !removed.Contains(p)).ToList();
		var groups = peer.Peer.Groups.ToList();
		switch (random.Next(9))
		{
			case 0 when staying.Count > 1 && staying.Contains(peer):
				var crashes = random.Next(2) == 0;
				Leave(simulation, peer, crashes);
				return $"{peer.Address} {(crashes ? "crashed" : "closed")}";
			case 1 when peers.Count < 9:
				var hostId = peer.Hosts().SingleOrDefault();
				var host = live.SingleOrDefault(p => p.Id == hostId);
				if (host is null)
					return null;

				var joiner = new SimulatedPeer(
					simulation,
					IPAddress.Parse($"10.8.0.{30 + peers.Count}")
				);
				_ = joiner.Peer.ConnectAsync(
					Application(flags: host.Peer.ApplicationDescription!.Flags),
					host.Peer.LocalEndPoint!
				);
				peers.Add(joiner);
				return $"{joiner.Address} joined through {host.Address}";
			case 2:
				var name = $"{peer.Address} {random.Next(100)}";
				_ = peer.Peer.SetPeerInfo(name);
				return $"{peer.Address} renamed itself '{name}'";
			case 3:
				_ = peer.Peer.CreateGroupAsync($"Group {random.Next(100)}");
				return $"{peer.Address} made a group";
			case 4 when groups.Count > 0:
				var group = groups[random.Next(groups.Count)];
				_ = peer.Peer.AddPlayerToGroupAsync(group.Id, peer.Id);
				return $"{peer.Address} joined {group.Id}";
			case 5 when groups.Count > 0:
				var left = groups[random.Next(groups.Count)];
				_ = peer.Peer.RemovePlayerFromGroupAsync(left.Id, peer.Id);
				return $"{peer.Address} left {left.Id}";
			case 6 when groups.Count > 0:
				var destroyed = groups[random.Next(groups.Count)];
				_ = peer.Peer.DestroyGroupAsync(destroyed.Id);
				return $"{peer.Address} destroyed {destroyed.Id}";
			case 7 when peer.Peer.IsHost && staying.Count > 1 && staying.Contains(peer):
				var others = staying.Where(p => p != peer).ToList();
				var target = others[random.Next(others.Count)];
				removed.Add(target);
				_ = peer.Peer.DestroyPeerAsync(target.Id);
				return $"{peer.Address} removed {target.Address}";
			default:
				foreach (var sender in live)
				{
					var number = sent.GetValueOrDefault(sender);
					sent[sender] = number + 1;
					sender.Peer.SendTo(
						Peer.AllPlayers,
						SimulatedPeer.Message(peers.IndexOf(sender), number),
						SendFlags.Guaranteed | SendFlags.NoLoopback
					);
				}
				return "everyone sent";
		}
	}

	/// <summary>
	/// A session of two to four peers, migrating its host or not, goes through two to six changes at points the seed
	/// chooses, in the conditions' faults: peers leave, crash or join, rename themselves, make, join, leave and
	/// destroy groups, the host removes a player, and everyone sends. Then the faults stop, and the peers left must
	/// hold together, reach each other and let a newcomer in; in rough conditions the session may have split, and each
	/// part must hold together on its own. A session whose host cannot migrate may end when its host leaves, and a
	/// rough one whenever. Every numbered message comes in order and once, whatever the faults.
	/// </summary>
	public static void Churn(int seed, SimulationConditions conditions)
	{
		var simulation = new Simulation(seed, conditions, disturbed: false);
		var random = simulation.Random;
		var flags = random.Next(4) == 0 ? default : SessionFlags.MigrateHost;
		var peers = Mesh(simulation, joiners: random.Next(1, 4), flags);
		simulation.Disturb();

		var sent = new Dictionary<SimulatedPeer, int>();
		var removed = new HashSet<SimulatedPeer>();
		var changes = new List<string>();
		var count = random.Next(2, 7);
		for (var i = 0; i < count; i++)
		{
			var steps = random.Next(0, 300);
			simulation.Run(steps);
			var live = peers.Where(p => p.InSession).ToList();
			if (live.Count == 0)
				break;

			var change = Change(simulation, peers, live[random.Next(live.Count)], sent, removed);
			if (change is not null)
				changes.Add($"{change} after {steps} steps");
		}

		simulation.Calm();
		var what = string.Join(", ", changes);
		var settled = Settle(
			simulation,
			peers,
			what,
			mayDivide: conditions.IsRough,
			mayEnd: conditions.IsRough || !flags.HasFlag(SessionFlags.MigrateHost),
			outcasts: removed
		);
		if (settled.Count > 0)
			JoinAfterwards(simulation, peers, settled, what, conditions.IsRough, removed);
		CloseAll(simulation, peers);
	}

	public static IReadOnlyList<SimulationScenario> All { get; } =
	[
		new("ClientServer", (seed, conditions) => ClientServerSession(seed, conditions)),
		new(nameof(RejectedJoin), RejectedJoin),
		new(nameof(PeerToPeerMesh), PeerToPeerMesh),
		new(nameof(HostMigration), HostMigration),
		new(nameof(HostLeavesWhileAPeerJoins), HostLeavesWhileAPeerJoins),
		new(nameof(PeerLeavesDuringMigration), PeerLeavesDuringMigration),
		new(nameof(PeerLeavesWhileAnotherJoins), PeerLeavesWhileAnotherJoins),
		new(nameof(Churn), Churn, MayBeRough: true),
	];
}
