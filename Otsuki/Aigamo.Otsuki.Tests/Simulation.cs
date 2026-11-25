using System.Buffers.Binary;
using System.Net;
using Aigamo.Otsuki.Actors;
using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Tests;

/// <summary>
/// Whole sessions on one thread: every actor's messages and every datagram's delivery run in an order that a seed
/// chooses, with a clock that moves only when nothing else can, and a network that does to the datagrams what the
/// conditions say (<see cref="ManualScheduler"/>, <see cref="ManualTimeProvider"/>, <see cref="SimulatedNetwork"/>).
/// A seed and its conditions take the same run again, to the byte.
/// </summary>
internal sealed class Simulation
{
	// The most steps a run takes before it counts as running away, such as messages that make more messages at the
	// same instant forever.
	private const long MostSteps = 5_000_000;

	private readonly SimulationConditions _conditions;

	// The faults' own choices, apart from the test's, so that a scenario's choices stay the same whatever the faults.
	private readonly Random _faults;
	private bool _calm;
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

	/// <summary>
	/// The steps run so far, messages and timers.
	/// </summary>
	public long Steps { get; private set; }

	/// <summary>
	/// The scheduler's step at which the scenario marked the point it is about (<see cref="Mark"/>), or -1.
	/// </summary>
	public int MarkedStep { get; private set; } = -1;

	/// <summary>
	/// Marks the point that the scenario is about, such as a peer leaving, from which an exploration takes every order.
	/// </summary>
	public void Mark() => MarkedStep = Scheduler.Taken.Count;

	// Cuts the network between one machine and another, or around one, for a while.
	private void Split(TimeSpan length)
	{
		var addresses = Network.Addresses();
		if (_calm || addresses.Count < 2)
			return;

		var first = addresses[_faults.Next(addresses.Count)];
		List<IPAddress> others = [.. addresses.Where(a => !a.Equals(first))];
		if (_faults.Next(2) == 0)
			others = [others[_faults.Next(others.Count)]];
		foreach (var other in others)
			Network.Partition(first, other);
		Clock.After(
			length,
			() =>
			{
				foreach (var other in others)
					Network.Heal(first, other);
			}
		);
	}

	/// <summary>
	/// Starts the faults of the conditions: datagrams lost and doubled, and partitions from now on.
	/// </summary>
	public void Disturb()
	{
		Network.LossRate = _conditions.LossRate;
		Network.DuplicateRate = _conditions.DuplicateRate;
		foreach (var (start, length) in _conditions.DrawPartitions(_faults))
			Clock.After(start, () => Split(length));
	}

	/// <summary>
	/// Ends the faults: nothing lost, doubled or partitioned any more, but datagrams still take their time.
	/// </summary>
	public void Calm()
	{
		_calm = true;
		Network.LossRate = 0;
		Network.DuplicateRate = 0;
		Network.HealAll();
	}

	/// <summary>
	/// A simulation in <paramref name="conditions"/>, with its faults from the start if <paramref name="disturbed"/>,
	/// or from <see cref="Disturb"/>.
	/// </summary>
	public Simulation(int seed, SimulationConditions conditions, bool disturbed = true)
	{
		_conditions = conditions;
		_faults = new Random(seed ^ 0xFA17);
		Seed = seed;
		Scheduler = conditions.CreateScheduler(seed);
		Network = new SimulatedNetwork(Scheduler, Clock, seed, conditions.KeepsTrace)
		{
			Latency = conditions.Latency,
			Jitter = conditions.Jitter,
		};
		Random = new Random(seed ^ 0x5EED);
		if (disturbed)
			Disturb();
		conditions.OnStart?.Invoke(this);
	}

	// Each peer draws its randomness from a seed of its own, which the simulation's seed and the peer's place
	// choose: two runs of one seed then make the same GUIDs, session IDs and secrets.
	public PeerOptions Options(IPAddress address, int? loops = null) =>
		new()
		{
			Scheduler = Scheduler,
			TimeProvider = Clock,
			TransportFactory = Network.CreateFactory(address),
			ConnectionLoops = loops ?? _conditions.Loops,
			RandomSeed = Seed * 1000 + _peers++ * 10,
		};

	// Every peer's events and snapshot hold together after each step, not only once a scenario is done.
	private void CheckPeers()
	{
		foreach (var peer in Peers)
			peer.Check();
	}

	/// <summary>
	/// Runs messages, then timers when no message waits, until <paramref name="done"/>; false if the simulated time
	/// passes <paramref name="limit"/>, or nothing is left to run.
	/// </summary>
	public bool RunUntil(Func<bool> done, TimeSpan limit)
	{
		var end = Clock.Now + limit;
		while (!done())
		{
			if (!Scheduler.RunNext() && !Clock.FireNext(end))
				return false;

			if (++Steps > MostSteps)
				throw new InvalidOperationException(
					$"The simulation runs away: {MostSteps} steps by {Clock.Now} (seed {Seed})"
				);
			CheckPeers();
		}

		return true;
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
/// group is created only when it is not, and destroyed, sends, joins a group or becomes host only while it is; a
/// player joins a group it is not in and leaves one it is in, and is in none when it is destroyed; and the numbered
/// messages of one sender (<see cref="Message"/>) come in order, each once.
/// </summary>
internal sealed class SimulatedPeer
{
	/// <summary>
	/// The highest tag of a numbered message: those above are messages of a test's own.
	/// </summary>
	public const int MostTag = 999;

	private readonly int _seed;
	private readonly HashSet<Dpnid> _players = [];
	private readonly HashSet<Dpnid> _groups = [];
	private readonly HashSet<(Dpnid Group, Dpnid Player)> _members = [];
	private readonly Dictionary<(Dpnid Sender, int Tag), int> _lastNumbers = [];

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

	/// <summary>
	/// A message numbered <paramref name="number"/> under <paramref name="tag"/>, such as its sender's place.
	/// </summary>
	public static byte[] Message(int tag, int number)
	{
		var data = new byte[8];
		BinaryPrimitives.WriteInt32LittleEndian(data, tag);
		BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(4), number);
		return data;
	}

	public static (int Tag, int Number) ReadMessage(PeerEvent.DataReceived data) =>
		(
			BinaryPrimitives.ReadInt32LittleEndian(data.Data.AsSpan()),
			BinaryPrimitives.ReadInt32LittleEndian(data.Data.AsSpan()[4..])
		);

	private void Require(bool holds, PeerEvent peerEvent, string what)
	{
		if (!holds)
			throw new InvalidOperationException(
				$"{Address} raised {peerEvent} {what} (seed {_seed}); before: {string.Join(", ", Events.TakeLast(8))}"
			);
	}

	private void FollowData(PeerEvent.DataReceived m)
	{
		Require(_players.Contains(m.Sender), m, "from a player not created");
		if (m.Data.Length != 8)
			return;

		var (tag, number) = ReadMessage(m);
		if (tag is < 0 or > MostTag)
			return;

		var key = (m.Sender, tag);
		Require(
			!_lastNumbers.TryGetValue(key, out var last) || number > last,
			m,
			$"numbered {number} after {last}: out of order or twice"
		);
		_lastNumbers[key] = number;
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
				Require(
					!_members.Any(member => member.Player == m.Player),
					m,
					"for a player still in a group"
				);
				break;
			case PeerEvent.DataReceived m:
				FollowData(m);
				break;
			case PeerEvent.HostMigrated m:
				Require(_players.Contains(m.NewHost), m, "for a player not created");
				break;
			case PeerEvent.PeerInfoUpdated m:
				Require(_players.Contains(m.Player.Id), m, "for a player not created");
				break;
			case PeerEvent.GroupCreated m:
				Require(_groups.Add(m.Group.Id), m, "for a group already created");
				break;
			case PeerEvent.GroupDestroyed m:
				Require(_groups.Remove(m.Group), m, "for a group not created");
				_members.RemoveWhere(member => member.Group == m.Group);
				break;
			case PeerEvent.PlayerAddedToGroup m:
				Require(
					_groups.Contains(m.Group) && _players.Contains(m.Player),
					m,
					"for a group or player not created"
				);
				Require(_members.Add((m.Group, m.Player)), m, "for a player already in it");
				break;
			case PeerEvent.PlayerRemovedFromGroup m:
				Require(
					_groups.Contains(m.Group) && _players.Contains(m.Player),
					m,
					"for a group or player not created"
				);
				Require(_members.Remove((m.Group, m.Player)), m, "for a player not in it");
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

	/// <summary>
	/// The players the snapshot shows.
	/// </summary>
	public HashSet<Dpnid> Seen() => [.. Peer.Players.Select(p => p.Id)];

	/// <summary>
	/// The hosts the snapshot shows: one, in a session that holds together.
	/// </summary>
	public List<Dpnid> Hosts() => [.. Peer.Players.Where(p => p.IsHost).Select(p => p.Id)];

	/// <summary>
	/// What the snapshot shows of the name table: its players, with their names and the host, and its groups, with
	/// their members. Peers in one session that holds together show the same.
	/// </summary>
	public string View() =>
		string.Join(
			" ",
			Peer.Players.OrderBy(p => p.Id.Value)
				.Select(p => $"{p.Id}{(p.IsHost ? "*" : "")}'{p.Name}'")
		)
		+ " | "
		+ string.Join(
			" ",
			Peer.Groups.OrderBy(g => g.Id.Value)
				.Select(g =>
					$"{g.Id}[{string.Join(",", g.Members.OrderBy(m => m.Value).Select(m => m.ToString()))}]"
				)
		);

	/// <summary>
	/// Whether the events raised so far add up to the snapshot: the same players, groups and members.
	/// </summary>
	public bool EventsMatchSnapshot()
	{
		Drain();
		return _players.SetEquals(Seen())
			&& _groups.SetEquals(Peer.Groups.Select(g => g.Id))
			&& _members.SetEquals(Peer.Groups.SelectMany(g => g.Members.Select(m => (g.Id, m))));
	}
}
