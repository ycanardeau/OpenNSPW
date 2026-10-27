using System.Net;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Reliable;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Tests;

/// <summary>
/// Host migration in sessions with <see cref="SessionFlags.MigrateHost"/> ([MC-DPL8CS] section 3.2.5.4).
/// </summary>
public class HostMigrationTests : IAsyncLifetime
{
	private static readonly Guid GuidApplication = new("4b1d1e6c-3f0a-4f0e-9b8e-5c2b7d6a1f90");

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
	private readonly List<Peer> _peers = [];
	private int _nextAddress;

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync()
	{
		foreach (var peer in _peers)
			await peer.CloseAsync().WaitAsync(TimeSpan.FromSeconds(10));
	}

	private static ApplicationDescription Application(bool migrate = true) =>
		new()
		{
			GuidApplication = GuidApplication,
			Flags = migrate ? SessionFlags.MigrateHost : SessionFlags.NoDpnServer,
		};

	private sealed record TestPeer(Peer Peer, EventLog Log, IPAddress Address)
	{
		public Dpnid Id { get; set; }
	}

	private TestPeer CreatePeer(string name)
	{
		var address = IPAddress.Parse($"10.2.0.{Interlocked.Increment(ref _nextAddress)}");
		var peer = new Peer(
			new PeerOptions
			{
				ReliableProfile = FastProfile,
				TransportFactory = _network.CreateFactory(address),
				JoinTimeout = TimeSpan.FromSeconds(5),
				IntegrityCheckTimeout = TimeSpan.FromSeconds(1),
				RequestTimeout = TimeSpan.FromSeconds(5),
				CloseTimeout = TimeSpan.FromSeconds(2),
			}
		);
		_peers.Add(peer);
		peer.SetPeerInfo(name);
		return new TestPeer(peer, new EventLog(peer), address);
	}

	/// <summary>
	/// A host and <paramref name="count"/> peers that joined in order, each of which has seen everyone.
	/// </summary>
	private async Task<List<TestPeer>> MeshAsync(int count, bool migrate = true)
	{
		var host = CreatePeer("Host");
		await host.Peer.HostAsync(Application(migrate));
		await host.Log.WaitForAsync<PeerEvent.PlayerCreated>();
		host.Id = host.Peer.LocalPlayer;
		var peers = new List<TestPeer> { host };
		for (var i = 1; i <= count; i++)
		{
			var joiner = CreatePeer($"Peer{i}");
			await joiner.Peer.ConnectAsync(Application(migrate), host.Peer.LocalEndPoint!);
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

	[Fact]
	public async Task HostLeaving_MakesTheOldestPeerTheHost()
	{
		var peers = await MeshAsync(3);
		var (oldHost, first) = (peers[0], peers[1]);

		await oldHost.Peer.CloseAsync();

		foreach (var peer in peers.Skip(1))
		{
			var destroyed = await peer.Log.WaitForAsync<PeerEvent.PlayerDestroyed>(e =>
				e.Player == oldHost.Id
			);
			destroyed.Reason.Should().Be(DestroyPlayerFlags.Normal);
			(await peer.Log.WaitForAsync<PeerEvent.HostMigrated>()).NewHost.Should().Be(first.Id);
			peer.Peer.GetPeerInfo(first.Id)!.IsHost.Should().BeTrue();
			peer.Log.OfType<PeerEvent.SessionTerminated>().Should().BeEmpty();
		}
		first.Peer.IsHost.Should().BeTrue();
	}

	[Fact]
	public async Task HostDroppingOut_MigratesAndTheSessionGoesOn()
	{
		var peers = await MeshAsync(3);
		var (oldHost, first, second, third) = (peers[0], peers[1], peers[2], peers[3]);

		_network.Partition(oldHost.Address);

		foreach (var peer in peers.Skip(1))
		{
			(await peer.Log.WaitForAsync<PeerEvent.HostMigrated>(timeout: TimeSpan.FromSeconds(20)))
				.NewHost.Should()
				.Be(first.Id);
			(await peer.Log.WaitForAsync<PeerEvent.PlayerDestroyed>(e => e.Player == oldHost.Id))
				.Reason.Should()
				.Be(DestroyPlayerFlags.ConnectionLost);
		}

		// The remaining peers still talk to each other, and the new host runs the name table.
		second.Peer.SendTo(Peer.AllPlayers, [1], SendFlags.Guaranteed | SendFlags.NoLoopback);
		(await first.Log.WaitForAsync<PeerEvent.DataReceived>()).Data.Should().Equal([1]);
		(await third.Log.WaitForAsync<PeerEvent.DataReceived>()).Data.Should().Equal([1]);

		await third.Peer.SetPeerInfo("Renamed");
		foreach (var peer in peers.Skip(1))
			(await peer.Log.WaitForAsync<PeerEvent.PeerInfoUpdated>())
				.Player.Name.Should()
				.Be("Renamed");

		var group = await second.Peer.CreateGroupAsync("After");
		await third.Log.WaitForAsync<PeerEvent.GroupCreated>(e => e.Group.Id == group);
	}

	[Fact]
	public async Task NewHost_AdmitsNewPlayers()
	{
		var peers = await MeshAsync(2);
		var (oldHost, first, second) = (peers[0], peers[1], peers[2]);
		await oldHost.Peer.CloseAsync();
		await second.Log.WaitForAsync<PeerEvent.HostMigrated>();
		await first.Log.WaitForAsync<PeerEvent.HostMigrated>();

		var late = CreatePeer("Late");
		await late.Peer.ConnectAsync(Application(), first.Peer.LocalEndPoint!);

		(await late.Log.WaitForAsync<PeerEvent.ConnectCompleted>())
			.Result.Should()
			.Be(ResultCode.Success);
		late.Id = late.Peer.LocalPlayer;
		await late.Log.WaitForAsync<PeerEvent.PlayerCreated>(3);
		await second.Log.WaitForAsync<PeerEvent.PlayerCreated>(e => e.Player.Id == late.Id);
		late.Peer.GetPeerInfo(first.Id)!.IsHost.Should().BeTrue();
		late.Peer.GetPeerInfo(oldHost.Id).Should().BeNull();

		// DPNIDs stay unique after the migration.
		new[] { oldHost.Id, first.Id, second.Id }
			.Should()
			.NotContain(late.Id);
	}

	[Fact]
	public async Task NewHost_CollectsOperationsItMissed()
	{
		var peers = await MeshAsync(2);
		var (oldHost, first, second) = (peers[0], peers[1], peers[2]);

		// The old host's last change reaches the second peer but not the one that becomes host.
		_network.Drop = (from, to, _) =>
			from.Address.Equals(oldHost.Address) && to.Address.Equals(first.Address);
		var group = await oldHost.Peer.CreateGroupAsync("Late news");
		await second.Log.WaitForAsync<PeerEvent.GroupCreated>();
		_network.Partition(oldHost.Address);
		_network.Drop = (_, _, _) => false;

		await first.Log.WaitForAsync<PeerEvent.HostMigrated>(timeout: TimeSpan.FromSeconds(20));

		(await first.Log.WaitForAsync<PeerEvent.GroupCreated>()).Group.Id.Should().Be(group);
		first.Peer.GetGroupInfo(group).Should().NotBeNull();
	}

	[Fact]
	public async Task WithoutMigrateHost_HostLeavingEndsTheSession()
	{
		var peers = await MeshAsync(2, migrate: false);

		await peers[0].Peer.CloseAsync();

		foreach (var peer in peers.Skip(1))
		{
			(await peer.Log.WaitForAsync<PeerEvent.SessionTerminated>())
				.Result.Should()
				.Be(ResultCode.HostTerminatedSession);
			peer.Log.OfType<PeerEvent.HostMigrated>().Should().BeEmpty();
		}
	}

	[Fact]
	public async Task LastPeer_BecomesHostOfItsOwnSession()
	{
		var peers = await MeshAsync(1);

		await peers[0].Peer.CloseAsync();

		(await peers[1].Log.WaitForAsync<PeerEvent.HostMigrated>())
			.NewHost.Should()
			.Be(peers[1].Id);
		peers[1].Peer.IsHost.Should().BeTrue();
	}

	[Fact]
	public async Task SecondMigration_Works()
	{
		var peers = await MeshAsync(3);
		var (oldHost, first, second, third) = (peers[0], peers[1], peers[2], peers[3]);
		await oldHost.Peer.CloseAsync();
		await third.Log.WaitForAsync<PeerEvent.HostMigrated>();
		await second.Log.WaitForAsync<PeerEvent.HostMigrated>();

		await first.Peer.CloseAsync();

		foreach (var peer in new[] { second, third })
		{
			var migrations = await peer.Log.WaitForAsync<PeerEvent.HostMigrated>(2);
			migrations[1].NewHost.Should().Be(second.Id);
		}
		third.Peer.GetPeerInfo(second.Id)!.IsHost.Should().BeTrue();
	}
}
