using System.Net;
using System.Reflection;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Messages.Reliable;
using Aigamo.Otsuki.Reliable;
using Aigamo.Otsuki.Session;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Aigamo.Otsuki.Tests;

/// <summary>
/// Cases that an independent review of groups, signing, host migration and client/server sessions found broken.
/// </summary>
public class RegressionTests(ITestOutputHelper output) : IAsyncLifetime
{
	private static readonly Guid GuidApplication = new("4b1d1e6c-3f0a-4f0e-9b8e-5c2b7d6a1f91");

	private static readonly ReliableProfile FastProfile = new()
	{
		ConnectRetryInterval = TimeSpan.FromMilliseconds(20),
		MaxConnectRetryInterval = TimeSpan.FromMilliseconds(100),
		MaxConnectRetries = 5,
		InitialRoundTripTime = TimeSpan.FromMilliseconds(5),
		DelayedAcknowledgmentTimeout = TimeSpan.FromMilliseconds(10),
		ShortDelayedAcknowledgmentTimeout = TimeSpan.FromMilliseconds(2),
		DelayedSendMaskTimeout = TimeSpan.FromMilliseconds(5),
		MaxRetryInterval = TimeSpan.FromMilliseconds(100),
		MaxRetries = 5,
		KeepAliveInterval = TimeSpan.FromMilliseconds(200),
		HardDisconnectInterval = TimeSpan.FromMilliseconds(10),
		GracefulDisconnectTimeout = TimeSpan.FromMilliseconds(500),
	};

	private readonly InMemoryNetwork _network = new();
	private readonly List<Func<Task>> _closers = [];
	private int _nextAddress;

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync()
	{
		foreach (var close in _closers)
			await close().WaitAsync(TimeSpan.FromSeconds(10));
	}

	private static ApplicationDescription Application(
		SessionFlags flags = SessionFlags.MigrateHost
	) => new() { GuidApplication = GuidApplication, Flags = flags };

	private sealed record TestPeer(Peer Peer, EventLog Log, IPAddress Address)
	{
		public Dpnid Id { get; set; }
	}

	private PeerOptions Options(IPAddress address) =>
		new()
		{
			ReliableProfile = FastProfile,
			TransportFactory = _network.CreateFactory(address),
			JoinTimeout = TimeSpan.FromSeconds(5),
			IntegrityCheckTimeout = TimeSpan.FromSeconds(1),
			RequestTimeout = TimeSpan.FromSeconds(5),
			CloseTimeout = TimeSpan.FromSeconds(2),
		};

	private TestPeer CreatePeer(string name)
	{
		var address = IPAddress.Parse($"10.9.0.{Interlocked.Increment(ref _nextAddress)}");
		var peer = new Peer(Options(address));
		_closers.Add(peer.CloseAsync);
		peer.SetPeerInfo(name);
		return new TestPeer(peer, new EventLog(peer), address);
	}

	private async Task<List<TestPeer>> MeshAsync(
		int count,
		SessionFlags flags = SessionFlags.MigrateHost
	)
	{
		var host = CreatePeer("Host");
		await host.Peer.HostAsync(Application(flags));
		await host.Log.WaitForAsync<PeerEvent.PlayerCreated>();
		host.Id = host.Peer.LocalPlayer;
		var peers = new List<TestPeer> { host };
		for (var i = 1; i <= count; i++)
		{
			var joiner = CreatePeer($"Peer{i}");
			await joiner.Peer.ConnectAsync(Application(flags), host.Peer.LocalEndPoint!);
			(await joiner.Log.WaitForAsync<PeerEvent.ConnectCompleted>())
				.Result.Should()
				.Be(ResultCode.Success);
			joiner.Id = joiner.Peer.LocalPlayer;
			peers.Add(joiner);
		}
		foreach (var peer in peers)
			await peer.Log.WaitForAsync<PeerEvent.PlayerCreated>(peers.Count);
		return peers;
	}

	private static SessionActor SessionOf(Peer peer) =>
		(SessionActor)
			typeof(Peer)
				.GetField("_session", BindingFlags.NonPublic | BindingFlags.Instance)!
				.GetValue(peer)!;

	private static Peer PeerOf(object facade) =>
		(Peer)
			facade
				.GetType()
				.GetField("_peer", BindingFlags.NonPublic | BindingFlags.Instance)!
				.GetValue(facade)!;

	/// <summary>AUTODESTRUCT groups of the old host go when it does.</summary>
	[Fact]
	public async Task AutoDestructGroupOfOldHost_GoesWithIt()
	{
		var peers = await MeshAsync(2);
		var (oldHost, first, second) = (peers[0], peers[1], peers[2]);
		var group = await oldHost.Peer.CreateGroupAsync("Owned", flags: GroupFlags.AutoDestruct);
		await first.Log.WaitForAsync<PeerEvent.GroupCreated>();
		await second.Log.WaitForAsync<PeerEvent.GroupCreated>();

		await oldHost.Peer.CloseAsync();
		await first.Log.WaitForAsync<PeerEvent.HostMigrated>();
		await second.Log.WaitForAsync<PeerEvent.HostMigrated>();
		await Task.Delay(1500);

		output.WriteLine(
			$"first group: {first.Peer.GetGroupInfo(group)}, second group: {second.Peer.GetGroupInfo(group)}"
		);
		output.WriteLine(
			$"first GroupDestroyed events: {first.Log.OfType<PeerEvent.GroupDestroyed>().Count}"
		);
		first.Peer.GetGroupInfo(group).Should().BeNull("AUTODESTRUCT groups go with their creator");
	}

	/// <summary>A change the new host makes while migrating waits, so that it neither hides an operation it missed nor reuses its DPNID.</summary>
	[Fact]
	public async Task NewHostChangeDuringMigration_WaitsForMissedOperations()
	{
		var peers = await MeshAsync(2);
		var (oldHost, first, second) = (peers[0], peers[1], peers[2]);

		var blockReport = false;
		_network.Drop = (from, to, datagram) =>
		{
			if (from.Address.Equals(oldHost.Address) && to.Address.Equals(first.Address))
				return true;
			if (
				blockReport
				&& from.Address.Equals(second.Address)
				&& to.Address.Equals(first.Address)
			)
				return ReliableMessageSerializer.Default.Deserialize(datagram)
					is ReliableMessage.DataFrame { Payload.Count: > 0 };
			return false;
		};
		var missed = await oldHost.Peer.CreateGroupAsync("Late news");
		await second.Log.WaitForAsync<PeerEvent.GroupCreated>();
		blockReport = true;
		_network.Partition(oldHost.Address);

		await first.Log.WaitForAsync<PeerEvent.HostMigrated>(timeout: TimeSpan.FromSeconds(20));
		var mine = await first.Peer.CreateGroupAsync("Mine");
		blockReport = false;
		await Task.Delay(1500);

		output.WriteLine($"missed={missed} mine={mine}");
		output.WriteLine(
			$"first: {string.Join(", ", first.Peer.Groups.Select(g => $"{g.Id}:{g.Name}"))}"
		);
		output.WriteLine(
			$"second: {string.Join(", ", second.Peer.Groups.Select(g => $"{g.Id}:{g.Name}"))}"
		);
		first.Peer.GetGroupInfo(missed)?.Name.Should().Be("Late news");
		second.Peer.GetGroupInfo(mine)?.Name.Should().Be("Mine");
	}

	/// <summary>Client/server sessions never migrate, so the server keeps no operations.</summary>
	[Fact]
	public async Task ClientServer_KeepsNoNameTableLog()
	{
		var server = new Server(Options(IPAddress.Parse("10.9.1.1")));
		_closers.Add(server.CloseAsync);
		var log = new EventLog(server.ReadEventsAsync());
		await server.HostAsync(Application(SessionFlags.NoDpnServer));
		await log.WaitForAsync<PeerEvent.PlayerCreated>();
		for (var i = 0; i < 20; i++)
		{
			var client = new Client(Options(IPAddress.Parse($"10.9.1.{10 + i}")));
			var clientLog = new EventLog(client.ReadEventsAsync());
			await client.ConnectAsync(Application(SessionFlags.NoDpnServer), server.LocalEndPoint!);
			(await clientLog.WaitForAsync<PeerEvent.ConnectCompleted>())
				.Result.Should()
				.Be(ResultCode.Success);
			await server.SetServerInfo($"Server {i}");
			await client.CloseAsync();
		}
		await log.WaitForAsync<PeerEvent.PlayerDestroyed>(20);
		var session = SessionOf(PeerOf(server));
		output.WriteLine($"server NameTableLog = {session.NameTableLog.Count}");
		session.NameTableLog.Count.Should().BeLessThan(10);
	}

	/// <summary>An info update while connecting is sent once the join completes.</summary>
	[Fact]
	public async Task SetPeerInfo_WhileConnecting_ReachesTheHost()
	{
		var host = CreatePeer("Host");
		await host.Peer.HostAsync(Application());
		var joiner = CreatePeer("Old");
		var block = true;
		_network.Drop = (from, to, datagram) =>
			block
			&& from.Address.Equals(host.Address)
			&& ReliableMessageSerializer.Default.Deserialize(datagram)
				is ReliableMessage.DataFrame { Payload.Count: > 0 };
		await joiner.Peer.ConnectAsync(Application(), host.Peer.LocalEndPoint!);
		await Task.Delay(100);
		// The update completes once it is in force, after the join.
		var update = joiner.Peer.SetPeerInfo("New");
		block = false;
		(await joiner.Log.WaitForAsync<PeerEvent.ConnectCompleted>())
			.Result.Should()
			.Be(ResultCode.Success);
		await update;
		await host.Log.WaitForAsync<PeerEvent.PlayerCreated>(2);
		var id = joiner.Peer.LocalPlayer;
		output.WriteLine(
			$"joiner sees itself as {joiner.Peer.GetPeerInfo(id)?.Name}, host sees {host.Peer.GetPeerInfo(id)?.Name}"
		);
		await host.Log.WaitForAsync<PeerEvent.PeerInfoUpdated>();
		host.Peer.GetPeerInfo(id)!.Name.Should().Be("New");
		joiner.Peer.GetPeerInfo(id)!.Name.Should().Be("New");
	}

	/// <summary>A peer that only lost its own link to the host cannot take the session from it.</summary>
	[Fact]
	public async Task PeerCutOffFromTheHostOnly_DoesNotTakeOver()
	{
		var peers = await MeshAsync(2);
		var (host, first, second) = (peers[0], peers[1], peers[2]);

		_network.Drop = (from, to, _) =>
			(from.Address.Equals(host.Address) && to.Address.Equals(first.Address))
			|| (from.Address.Equals(first.Address) && to.Address.Equals(host.Address));

		// The host removes the peer it no longer hears, and the other peer follows it.
		await second.Log.WaitForAsync<PeerEvent.PlayerDestroyed>(
			e => e.Player == first.Id,
			TimeSpan.FromSeconds(20)
		);
		second.Log.OfType<PeerEvent.HostMigrated>().Should().BeEmpty();
		second.Peer.GetPeerInfo(host.Id)!.IsHost.Should().BeTrue();
		host.Peer.IsHost.Should().BeTrue();
	}

	/// <summary>A peer that hears the new host's claim before it notices that the host left follows it once it does.</summary>
	[Fact]
	public async Task ClaimBeforeLosingTheHost_IsFollowedOnceTheHostIsLost()
	{
		var peers = await MeshAsync(3);
		var (host, first) = (peers[0], peers[1]);

		// The first peer keeps sending to the host, so it notices first that the host is gone.
		_network.Partition(host.Address);
		for (var i = 0; i < 20; i++)
			first.Peer.SendTo(host.Id, [(byte)i]);

		foreach (var peer in peers.Skip(1))
		{
			(await peer.Log.WaitForAsync<PeerEvent.HostMigrated>(timeout: TimeSpan.FromSeconds(20)))
				.NewHost.Should()
				.Be(first.Id);
			peer.Log.OfType<PeerEvent.HostMigrated>().Should().ContainSingle();
		}
		await Task.Delay(500);
		foreach (var peer in peers.Skip(1))
			peer.Peer.GetPeerInfo(first.Id)!.IsHost.Should().BeTrue();
		peers[2].Peer.IsHost.Should().BeFalse();
		peers[3].Peer.IsHost.Should().BeFalse();
	}

	private static byte[] Payload(int i) => [(byte)i, (byte)(i >> 8), 1, 2, 3];

	/// <summary>A HARD_DISCONNECT sent right after the sender passed 192 validates at a receiver that has not.</summary>
	[Fact]
	public void FullSigning_HardDisconnectAfterAdvanceValidates()
	{
		var sender = new PacketSigner(
			PacketSigning.Full,
			localSecret: 0x1111,
			remoteSecret: 0x2222
		);
		var receiver = new PacketSigner(
			PacketSigning.Full,
			localSecret: 0x2222,
			remoteSecret: 0x1111
		);
		for (var s = 0; s <= 192; s++)
		{
			sender.ObserveSent(new SequenceId((byte)s), Payload(s));
			if (s < 192)
				receiver.ObserveReceived(new SequenceId((byte)s), Payload(s));
		}
		var datagram = ReliableMessageSerializer.Signed.Serialize(
			new ReliableMessage.HardDisconnect { SessionId = new SessionId(5) }
		);
		sender.Sign(datagram, sender.ControlSecret);
		var ok = receiver.Validate(
			datagram,
			nextReceive: new SequenceId(192),
			sequence: new SequenceId(192)
		);
		output.WriteLine($"valid = {ok}");
		ok.Should().BeTrue();
	}

	/// <summary>A SACK sent while frame 192 is unacknowledged validates.</summary>
	[Fact]
	public void FullSigning_SackAtWrapValidates()
	{
		var sender = new PacketSigner(
			PacketSigning.Full,
			localSecret: 0x1111,
			remoteSecret: 0x2222
		);
		var receiver = new PacketSigner(
			PacketSigning.Full,
			localSecret: 0x2222,
			remoteSecret: 0x1111
		);
		for (var s = 0; s <= 255; s++)
		{
			sender.ObserveSent(new SequenceId((byte)s), Payload(s));
			if (s < 192)
				receiver.ObserveReceived(new SequenceId((byte)s), Payload(s));
		}
		var datagram = ReliableMessageSerializer.Signed.Serialize(
			new ReliableMessage.Sack
			{
				NextSend = new SequenceId(0),
				NextReceive = new SequenceId(0),
			}
		);
		sender.Sign(datagram, sender.ControlSecret);
		var ok = receiver.Validate(
			datagram,
			nextReceive: new SequenceId(192),
			sequence: new SequenceId(0)
		);
		output.WriteLine($"valid = {ok}");
		ok.Should().BeTrue();
	}

	[Fact]
	public async Task Close_DestroysGroups()
	{
		var peers = await MeshAsync(1);
		var group = await peers[0].Peer.CreateGroupAsync("G");
		await peers[1].Log.WaitForAsync<PeerEvent.GroupCreated>();
		await peers[1].Peer.CloseAsync();
		output.WriteLine(
			$"PlayerDestroyed={peers[1].Log.OfType<PeerEvent.PlayerDestroyed>().Count} GroupDestroyed={peers[1].Log.OfType<PeerEvent.GroupDestroyed>().Count}"
		);
		peers[1].Log.OfType<PeerEvent.GroupDestroyed>().Should().NotBeEmpty();
	}
}
