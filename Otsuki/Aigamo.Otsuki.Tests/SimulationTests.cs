using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using Aigamo.Otsuki.Actors;
using Aigamo.Otsuki.Messages.Core;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Tests;

/// <summary>
/// Runs whole sessions on one thread, every actor's messages and every datagram's delivery in an order that a seed
/// chooses, with a clock that moves only when nothing else can (<see cref="ManualScheduler"/>,
/// <see cref="ManualTimeProvider"/>, <see cref="SimulatedNetwork"/>). Many seeds try many orders. A seed takes nearly
/// the same order again: the session's GUID and the order of a set of connections by their hash still differ from run
/// to run.
/// </summary>
public class SimulationTests
{
	private const int Seeds = 100;

	private static readonly Guid GuidApplication = new("0c5a1e7d-2b9f-4e61-8d3a-7f4b6c2e9a15");

	private static readonly IPEndPoint ServerEndPoint = new(IPAddress.Parse("10.7.0.1"), 2302);

	private sealed class Simulation
	{
		public ManualScheduler Scheduler { get; }

		public ManualTimeProvider Clock { get; } = new();

		public SimulatedNetwork Network { get; }

		public Simulation(int seed)
		{
			Scheduler = new ManualScheduler(seed);
			Network = new SimulatedNetwork(Scheduler);
		}

		public PeerOptions Options(IPAddress address, int loops = 1) =>
			new()
			{
				Scheduler = Scheduler,
				TimeProvider = Clock,
				TransportFactory = Network.CreateFactory(address),
				ConnectionLoops = loops,
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
				if (Scheduler.RunNext())
					continue;

				if (Clock.FireNext(end))
					continue;

				var waited = Stopwatch.StartNew();
				while (Scheduler.Ready == 0 && !done() && waited.ElapsedMilliseconds < 20)
					Thread.Sleep(1);
				if (Scheduler.Ready == 0 && !done())
					return false;
			}

			return true;
		}
	}

	private static ApplicationDescription Application(int maxPlayers = 0) =>
		new() { GuidApplication = GuidApplication, MaxPlayers = maxPlayers };

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

	private static byte[] Message(int client, int number)
	{
		var data = new byte[8];
		BinaryPrimitives.WriteInt32LittleEndian(data, client);
		BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(4), number);
		return data;
	}

	private static void ClientServerSession(int seed)
	{
		const int ClientCount = 3;
		const int MessageCount = 5;
		var simulation = new Simulation(seed);
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
			.RunUntil(AllJoined, TimeSpan.FromSeconds(30))
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

		List<(int Client, int Number)> Received() =>
			[
				.. serverEvents
					.OfType<PeerEvent.DataReceived>()
					.Select(d =>
						(
							BinaryPrimitives.ReadInt32LittleEndian(d.Data.AsSpan()),
							BinaryPrimitives.ReadInt32LittleEndian(d.Data.AsSpan()[4..])
						)
					),
			];

		simulation
			.RunUntil(
				() =>
				{
					Drain(server, serverEvents);
					return Received().Count == ClientCount * MessageCount;
				},
				TimeSpan.FromSeconds(30)
			)
			.Should()
			.BeTrue($"the server receives every message (seed {seed})");
		foreach (var group in Received().GroupBy(r => r.Client))
			group
				.Select(r => r.Number)
				.Should()
				.Equal(Enumerable.Range(0, MessageCount), $"one client's messages keep their order (seed {seed})");

		var closing = clients.Select(c => c.CloseAsync()).Append(server.CloseAsync()).ToList();
		simulation
			.RunUntil(() => closing.All(c => c.IsCompleted), TimeSpan.FromSeconds(30))
			.Should()
			.BeTrue($"everyone closes (seed {seed})");
	}

	[Fact]
	public void ClientServer_JoinsSendsAndCloses_InEveryOrderTried()
	{
		for (var seed = 0; seed < Seeds; seed++)
			ClientServerSession(seed);
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
		simulation.RunUntil(() => serverClosing.IsCompleted, TimeSpan.FromSeconds(30)).Should().BeTrue();
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
}
