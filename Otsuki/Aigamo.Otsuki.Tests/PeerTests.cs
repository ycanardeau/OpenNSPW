using System.Net;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Reliable;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Tests;

public class PeerTests : IAsyncLifetime
{
	private static readonly Guid GuidApplication = new("6dd5f9d4-2b3c-4d39-9f1e-6a0d4a6c4c3e");

	private static readonly ReliableProfile FastProfile = new()
	{
		ConnectRetryInterval = TimeSpan.FromMilliseconds(20),
		MaxConnectRetryInterval = TimeSpan.FromMilliseconds(100),
		MaxConnectRetries = 5,
		InitialRoundTripTime = TimeSpan.FromMilliseconds(5),
		DelayedAcknowledgmentTimeout = TimeSpan.FromMilliseconds(10),
		ShortDelayedAcknowledgmentTimeout = TimeSpan.FromMilliseconds(2),
		DelayedSendMaskTimeout = TimeSpan.FromMilliseconds(5),
		MaxRetryInterval = TimeSpan.FromMilliseconds(200),
		MaxRetries = 10,
		KeepAliveInterval = TimeSpan.FromMilliseconds(500),
		HardDisconnectInterval = TimeSpan.FromMilliseconds(10),
		GracefulDisconnectTimeout = TimeSpan.FromMilliseconds(500),
	};

	private readonly InMemoryNetwork _network = new();
	private readonly List<Peer> _peers = [];
	private int _nextAddress;

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync()
	{
		foreach (var peer in _peers)
			await peer.CloseAsync().WaitAsync(TimeSpan.FromSeconds(10));
	}

	private static ApplicationDescription Application(int maxPlayers = 0) =>
		new() { GuidApplication = GuidApplication, MaxPlayers = maxPlayers };

	private (Peer Peer, EventLog Log, IPAddress Address) CreatePeer(string name)
	{
		var address = IPAddress.Parse($"10.0.0.{Interlocked.Increment(ref _nextAddress)}");
		var peer = new Peer(
			new PeerOptions
			{
				ReliableProfile = FastProfile,
				TransportFactory = _network.CreateFactory(address),
				JoinTimeout = TimeSpan.FromSeconds(5),
				IntegrityCheckTimeout = TimeSpan.FromSeconds(1),
				CloseTimeout = TimeSpan.FromSeconds(2),
			}
		);
		_peers.Add(peer);
		peer.SetPeerInfo(name);
		return (peer, new EventLog(peer), address);
	}

	private async Task<(Peer Peer, EventLog Log, IPAddress Address)> HostAsync(
		string name,
		ApplicationDescription? application = null
	)
	{
		var host = CreatePeer(name);
		await host.Peer.HostAsync(application ?? Application());
		await host.Log.WaitForAsync<PeerEvent.PlayerCreated>();
		return host;
	}

	private async Task<(Peer Peer, EventLog Log, IPAddress Address)> JoinAsync(
		string name,
		Peer host,
		ApplicationDescription? application = null
	)
	{
		var joiner = CreatePeer(name);
		await joiner.Peer.ConnectAsync(application ?? Application(), host.LocalEndPoint!);
		var completed = await joiner.Log.WaitForAsync<PeerEvent.ConnectCompleted>();
		completed.Result.Should().Be(ResultCode.Success);
		return joiner;
	}

	private static async Task<IReadOnlyList<PeerEvent.DataReceived>> ReceiveAsync(
		EventLog log,
		Dpnid sender,
		int count
	) => await log.WaitForAsync<PeerEvent.DataReceived>(count, e => e.Sender == sender);

	[Fact]
	public async Task Host_CreatesLocalPlayer()
	{
		var host = await HostAsync("Host");

		var created = host.Log.OfType<PeerEvent.PlayerCreated>().Single();
		created.Player.Name.Should().Be("Host");
		created.Player.IsLocal.Should().BeTrue();
		created.Player.IsHost.Should().BeTrue();
		host.Peer.LocalPlayer.Should().Be(created.Player.Id);
		host.Peer.IsHost.Should().BeTrue();
		host.Peer.ApplicationDescription!.GuidInstance.Should().NotBeEmpty();
	}

	[Fact]
	public async Task Connect_CreatesPlayersOnBothSides()
	{
		var host = await HostAsync("Host");
		var joiner = await JoinAsync("Joiner", host.Peer);

		var joinerCreated = await host.Log.WaitForAsync<PeerEvent.PlayerCreated>(e =>
			!e.Player.IsLocal
		);
		joinerCreated.Player.Name.Should().Be("Joiner");
		joinerCreated.Player.IsHost.Should().BeFalse();

		var players = joiner.Log.OfType<PeerEvent.PlayerCreated>();
		players.Should().HaveCount(2);
		players[0]
			.Player.Should()
			.Match<PlayerInfo>(p =>
				p.IsLocal && p.Name == "Joiner" && p.Id == joinerCreated.Player.Id
			);
		players[1]
			.Player.Should()
			.Match<PlayerInfo>(p => p.IsHost && p.Name == "Host" && p.Id == host.Peer.LocalPlayer);

		// DN_DPNID: index and name table version XOR the first 32 bits of the instance GUID.
		var guidInstance = joiner.Peer.ApplicationDescription!.GuidInstance;
		guidInstance.Should().Be(host.Peer.ApplicationDescription!.GuidInstance);
		host.Peer.LocalPlayer.Should().Be(new Dpnid(2, 2, guidInstance));
		joiner.Peer.LocalPlayer.Should().Be(new Dpnid(3, 3, guidInstance));

		joiner.Peer.GetPeerInfo(host.Peer.LocalPlayer)!.Name.Should().Be("Host");
		host.Peer.GetPeerInfo(joiner.Peer.LocalPlayer)!.Name.Should().Be("Joiner");
	}

	[Fact]
	public async Task SendTo_DeliversInOrderInBothDirections()
	{
		var host = await HostAsync("Host");
		var joiner = await JoinAsync("Joiner", host.Peer);
		await host.Log.WaitForAsync<PeerEvent.PlayerCreated>(e => !e.Player.IsLocal);

		for (var i = 0; i < 50; i++)
		{
			host.Peer.SendTo(joiner.Peer.LocalPlayer, [(byte)i]);
			joiner.Peer.SendTo(host.Peer.LocalPlayer, [(byte)(100 + i)]);
		}

		var atJoiner = await ReceiveAsync(joiner.Log, host.Peer.LocalPlayer, 50);
		var atHost = await ReceiveAsync(host.Log, joiner.Peer.LocalPlayer, 50);
		atJoiner.Select(e => (int)e.Data[0]).Should().Equal(Enumerable.Range(0, 50));
		atHost.Select(e => (int)e.Data[0]).Should().Equal(Enumerable.Range(100, 50));

		var completed = await host.Log.WaitForAsync<PeerEvent.SendCompleted>(50);
		completed.Should().OnlyContain(e => e.Result == ResultCode.Success);
	}

	[Fact]
	public async Task SendTo_ReassemblesMessagesLargerThanAFrame()
	{
		var host = await HostAsync("Host");
		var joiner = await JoinAsync("Joiner", host.Peer);
		await host.Log.WaitForAsync<PeerEvent.PlayerCreated>(e => !e.Player.IsLocal);
		var data = Enumerable.Range(0, 20_000).Select(i => (byte)(i * 7)).ToArray();

		host.Peer.SendTo(joiner.Peer.LocalPlayer, data);

		var received = await joiner.Log.WaitForAsync<PeerEvent.DataReceived>();
		received.Data.Should().Equal(data);
	}

	[Fact]
	public async Task SendTo_SurvivesLossAndReordering()
	{
		var host = await HostAsync("Host");
		var joiner = await JoinAsync("Joiner", host.Peer);
		await host.Log.WaitForAsync<PeerEvent.PlayerCreated>(e => !e.Player.IsLocal);
		_network.LossRate = 0.2;
		_network.MaxDelay = TimeSpan.FromMilliseconds(5);

		for (var i = 0; i < 200; i++)
		{
			host.Peer.SendTo(joiner.Peer.LocalPlayer, BitConverter.GetBytes(i));
			joiner.Peer.SendTo(host.Peer.LocalPlayer, BitConverter.GetBytes(-i));
		}

		var timeout = TimeSpan.FromSeconds(30);
		var atJoiner = await joiner.Log.WaitForAsync<PeerEvent.DataReceived>(200, timeout: timeout);
		var atHost = await host.Log.WaitForAsync<PeerEvent.DataReceived>(200, timeout: timeout);
		atJoiner
			.Select(e => BitConverter.ToInt32([.. e.Data]))
			.Should()
			.Equal(Enumerable.Range(0, 200));
		atHost
			.Select(e => BitConverter.ToInt32([.. e.Data]))
			.Should()
			.Equal(Enumerable.Range(0, 200).Select(i => -i));
	}

	[Fact]
	public async Task SendTo_UnreliableLossDoesNotBlockLaterMessages()
	{
		var host = await HostAsync("Host");
		var joiner = await JoinAsync("Joiner", host.Peer);
		await host.Log.WaitForAsync<PeerEvent.PlayerCreated>(e => !e.Player.IsLocal);

		// Lose every unreliable data frame, so the receiver can only move on through the send mask.
		_network.Drop = (_, _, datagram) => (datagram[0] & 0x03) == 0x01;
		host.Peer.SendTo(joiner.Peer.LocalPlayer, [1], SendFlags.None);
		host.Peer.SendTo(joiner.Peer.LocalPlayer, [2], SendFlags.None);
		host.Peer.SendTo(joiner.Peer.LocalPlayer, [3], SendFlags.Guaranteed);

		var received = await joiner.Log.WaitForAsync<PeerEvent.DataReceived>();
		received.Data.Should().Equal([3]);
		joiner.Log.OfType<PeerEvent.DataReceived>().Should().ContainSingle();
	}

	[Fact]
	public async Task SendTo_AllPlayersLoopsBackUnlessNoLoopback()
	{
		var host = await HostAsync("Host");
		var joiner = await JoinAsync("Joiner", host.Peer);
		await host.Log.WaitForAsync<PeerEvent.PlayerCreated>(e => !e.Player.IsLocal);

		host.Peer.SendTo(Peer.AllPlayers, [1]);
		host.Peer.SendTo(Peer.AllPlayers, [2], SendFlags.Guaranteed | SendFlags.NoLoopback);

		await ReceiveAsync(joiner.Log, host.Peer.LocalPlayer, 2);
		var atHost = await host.Log.WaitForAsync<PeerEvent.DataReceived>();
		atHost.Sender.Should().Be(host.Peer.LocalPlayer);
		atHost.Data.Should().Equal([1]);
		await host.Log.WaitForAsync<PeerEvent.SendCompleted>(2);
		host.Log.OfType<PeerEvent.DataReceived>().Should().ContainSingle();
	}

	[Fact]
	public async Task SendTo_UnknownPlayerFails()
	{
		var host = await HostAsync("Host");

		var handle = host.Peer.SendTo(new Dpnid(12345), [1]);

		var completed = await host.Log.WaitForAsync<PeerEvent.SendCompleted>();
		completed.Should().Be(new PeerEvent.SendCompleted(handle, ResultCode.InvalidPlayer));
	}

	[Fact]
	public async Task ThreePeers_FormAFullMesh()
	{
		var host = await HostAsync("Host");
		var first = await JoinAsync("First", host.Peer);
		var second = await JoinAsync("Second", host.Peer);

		await first.Log.WaitForAsync<PeerEvent.PlayerCreated>(3);
		await host.Log.WaitForAsync<PeerEvent.PlayerCreated>(3);
		second
			.Log.OfType<PeerEvent.PlayerCreated>()
			.Select(e => e.Player.Name)
			.Should()
			.Equal("Second", "Host", "First");
		first.Peer.GetPeerInfo(second.Peer.LocalPlayer)!.Name.Should().Be("Second");

		var direct = 0;
		_network.Observe = (source, destination, _) =>
		{
			if (source.Address.Equals(first.Address) && destination.Address.Equals(second.Address))
				Interlocked.Increment(ref direct);
		};
		first.Peer.SendTo(second.Peer.LocalPlayer, [42]);

		var received = await second.Log.WaitForAsync<PeerEvent.DataReceived>();
		received.Should().Be(new PeerEvent.DataReceived(first.Peer.LocalPlayer, received.Data));
		received.Data.Should().Equal([42]);
		direct.Should().BePositive("peers talk to each other directly, not through the host");
	}

	[Fact]
	public async Task ManyPeers_AllSeeEachOther()
	{
		var host = await HostAsync("Host");
		var joiners = new List<(Peer Peer, EventLog Log, IPAddress Address)>();
		for (var i = 0; i < 5; i++)
			joiners.Add(await JoinAsync($"Peer{i}", host.Peer));

		foreach (var (peer, log, _) in joiners.Append(host))
		{
			await log.WaitForAsync<PeerEvent.PlayerCreated>(6);
			peer.Players.Should().HaveCount(6);
		}

		foreach (var sender in joiners)
			sender.Peer.SendTo(Peer.AllPlayers, [1], SendFlags.Guaranteed | SendFlags.NoLoopback);

		foreach (var (_, log, _) in joiners.Append(host))
			await log.WaitForAsync<PeerEvent.DataReceived>(
				joiners.Count - 1 + (log == host.Log ? 1 : 0)
			);
	}

	[Fact]
	public async Task Close_NonHostIsDestroyedOnOtherPeers()
	{
		var host = await HostAsync("Host");
		var first = await JoinAsync("First", host.Peer);
		var second = await JoinAsync("Second", host.Peer);
		await first.Log.WaitForAsync<PeerEvent.PlayerCreated>(3);
		var leaving = second.Peer.LocalPlayer;

		await second.Peer.CloseAsync();

		(await host.Log.WaitForAsync<PeerEvent.PlayerDestroyed>())
			.Should()
			.Be(new PeerEvent.PlayerDestroyed(leaving, DestroyPlayerFlags.Normal));
		(await first.Log.WaitForAsync<PeerEvent.PlayerDestroyed>())
			.Should()
			.Be(new PeerEvent.PlayerDestroyed(leaving, DestroyPlayerFlags.Normal));
		second.Log.OfType<PeerEvent.PlayerDestroyed>().Should().HaveCount(3);
		first.Peer.Players.Should().HaveCount(2);
		first.Log.OfType<PeerEvent.SessionTerminated>().Should().BeEmpty();
	}

	[Fact]
	public async Task Close_HostTerminatesTheSession()
	{
		var host = await HostAsync("Host");
		var joiner = await JoinAsync("Joiner", host.Peer);

		await host.Peer.CloseAsync();

		var terminated = await joiner.Log.WaitForAsync<PeerEvent.SessionTerminated>();
		terminated.Result.Should().Be(ResultCode.HostTerminatedSession);
		await joiner.Log.WaitForAsync<PeerEvent.PlayerDestroyed>(
			2,
			e => e.Reason == DestroyPlayerFlags.SessionTerminated
		);
		joiner.Peer.Players.Should().BeEmpty();
	}

	[Fact]
	public async Task LostPeer_IsDestroyed()
	{
		var host = await HostAsync("Host");
		var first = await JoinAsync("First", host.Peer);
		var second = await JoinAsync("Second", host.Peer);
		await first.Log.WaitForAsync<PeerEvent.PlayerCreated>(3);
		var lost = second.Peer.LocalPlayer;

		_network.Partition(second.Address);

		(await host.Log.WaitForAsync<PeerEvent.PlayerDestroyed>())
			.Should()
			.Be(new PeerEvent.PlayerDestroyed(lost, DestroyPlayerFlags.ConnectionLost));
		(await first.Log.WaitForAsync<PeerEvent.PlayerDestroyed>())
			.Should()
			.Be(new PeerEvent.PlayerDestroyed(lost, DestroyPlayerFlags.ConnectionLost));
		(await second.Log.WaitForAsync<PeerEvent.SessionTerminated>())
			.Result.Should()
			.Be(ResultCode.ConnectionLost);
	}

	[Fact]
	public async Task Connect_WithWrongApplicationFails()
	{
		var host = await HostAsync("Host");
		var joiner = CreatePeer("Joiner");

		await joiner.Peer.ConnectAsync(
			new ApplicationDescription { GuidApplication = Guid.NewGuid() },
			host.Peer.LocalEndPoint!
		);

		(await joiner.Log.WaitForAsync<PeerEvent.ConnectCompleted>())
			.Result.Should()
			.Be(ResultCode.InvalidApplication);
		joiner.Log.OfType<PeerEvent.PlayerCreated>().Should().BeEmpty();
		host.Peer.Players.Should().ContainSingle();
	}

	[Fact]
	public async Task Connect_ToFullSessionFails()
	{
		var host = await HostAsync("Host", Application(maxPlayers: 2));
		await JoinAsync("First", host.Peer, Application(maxPlayers: 2));
		var second = CreatePeer("Second");

		await second.Peer.ConnectAsync(Application(), host.Peer.LocalEndPoint!);

		(await second.Log.WaitForAsync<PeerEvent.ConnectCompleted>())
			.Result.Should()
			.Be(ResultCode.SessionFull);
	}

	[Fact]
	public async Task Connect_RequiresThePassword()
	{
		var application = Application() with
		{
			Flags = SessionFlags.NoDpnServer | SessionFlags.RequirePassword,
			Password = "secret",
		};
		var host = await HostAsync("Host", application);
		var wrong = CreatePeer("Wrong");

		await wrong.Peer.ConnectAsync(
			application with
			{
				Password = "guess",
			},
			host.Peer.LocalEndPoint!
		);

		(await wrong.Log.WaitForAsync<PeerEvent.ConnectCompleted>())
			.Result.Should()
			.Be(ResultCode.InvalidPassword);
		await JoinAsync("Right", host.Peer, application);
	}

	[Fact]
	public async Task Connect_WithoutHostReportsNoResponse()
	{
		var joiner = CreatePeer("Joiner");

		await joiner.Peer.ConnectAsync(
			Application(),
			new IPEndPoint(IPAddress.Parse("10.9.9.9"), Peer.DefaultPort)
		);

		(await joiner.Log.WaitForAsync<PeerEvent.ConnectCompleted>())
			.Result.Should()
			.Be(ResultCode.NoResponse);
	}

	[Fact]
	public async Task Connect_CanBeRetriedAfterFailure()
	{
		var joiner = CreatePeer("Joiner");
		await joiner.Peer.ConnectAsync(
			Application(),
			new IPEndPoint(IPAddress.Parse("10.9.9.9"), Peer.DefaultPort)
		);
		await joiner.Log.WaitForAsync<PeerEvent.ConnectCompleted>();
		var host = await HostAsync("Host");

		await joiner.Peer.ConnectAsync(Application(), host.Peer.LocalEndPoint!);

		(await joiner.Log.WaitForAsync<PeerEvent.ConnectCompleted>(2))[1]
			.Result.Should()
			.Be(ResultCode.Success);
	}

	[Fact]
	public async Task Host_WhileInSessionThrows()
	{
		var host = await HostAsync("Host");

		var act = () => host.Peer.HostAsync(Application());

		await act.Should().ThrowAsync<InvalidOperationException>();
	}

	[Fact]
	public async Task Close_CompletesTheEventStream()
	{
		var host = await HostAsync("Host");

		await host.Peer.CloseAsync();

		var events = new List<PeerEvent>();
		await foreach (
			var peerEvent in host
				.Peer.ReadEventsAsync()
				.WithCancellation(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token)
		)
			events.Add(peerEvent);
		host.Peer.Invoking(p => p.SendTo(Peer.AllPlayers, [1]))
			.Should()
			.Throw<ObjectDisposedException>();
	}

	[Fact]
	public async Task DoWork_HandlesQueuedEventsOnTheCallingThread()
	{
		var network = new InMemoryNetwork();
		var options = new PeerOptions
		{
			ReliableProfile = FastProfile,
			TransportFactory = network.CreateFactory(IPAddress.Parse("10.1.0.1")),
		};
		await using var host = new Peer(options);
		host.SetPeerInfo("Host");
		await host.HostAsync(Application());
		await using var joiner = new Peer(
			options with
			{
				TransportFactory = network.CreateFactory(IPAddress.Parse("10.1.0.2")),
			}
		);
		joiner.SetPeerInfo("Joiner");
		await joiner.ConnectAsync(Application(), host.LocalEndPoint!);

		var events = new List<PeerEvent>();
		var deadline = DateTime.UtcNow + EventLog.DefaultTimeout;
		while (!events.OfType<PeerEvent.ConnectCompleted>().Any() && DateTime.UtcNow < deadline)
		{
			joiner.DoWork(events.Add);
			await Task.Delay(5);
		}

		events
			.Should()
			.SatisfyRespectively(
				e =>
					e.Should()
						.BeOfType<PeerEvent.PlayerCreated>()
						.Which.Player.IsLocal.Should()
						.BeTrue(),
				e =>
					e.Should()
						.BeOfType<PeerEvent.PlayerCreated>()
						.Which.Player.IsHost.Should()
						.BeTrue(),
				e =>
					e.Should()
						.BeOfType<PeerEvent.ConnectCompleted>()
						.Which.Result.Should()
						.Be(ResultCode.Success)
			);
		host.DoWork(_ => { }).Should().BeGreaterThan(0);
	}
}
