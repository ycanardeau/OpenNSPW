using System.Net;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Reliable;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Tests;

/// <summary>
/// Changes to the name table after joining: player information and groups ([MC-DPL8CS] sections 3.4.5.2 and 3.5).
/// </summary>
public class NameTableChangeTests : IAsyncLifetime
{
	private static readonly Guid GuidApplication = new("0f0c3a52-90d1-4b40-a5c9-8f8a3b0d2e71");

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

	private static ApplicationDescription Application =>
		new() { GuidApplication = GuidApplication };

	private (Peer Peer, EventLog Log) CreatePeer(string name, TimeSpan? requestTimeout = null)
	{
		var address = IPAddress.Parse($"10.1.0.{Interlocked.Increment(ref _nextAddress)}");
		var peer = new Peer(
			new PeerOptions
			{
				ReliableProfile = FastProfile,
				TransportFactory = _network.CreateFactory(address),
				JoinTimeout = TimeSpan.FromSeconds(5),
				RequestTimeout = requestTimeout ?? TimeSpan.FromSeconds(5),
				CloseTimeout = TimeSpan.FromSeconds(2),
			}
		);
		_peers.Add(peer);
		peer.SetPeerInfo(name);
		return (peer, new EventLog(peer));
	}

	private async Task<(Peer Peer, EventLog Log)> HostAsync(string name)
	{
		var host = CreatePeer(name);
		await host.Peer.HostAsync(Application);
		await host.Log.WaitForAsync<PeerEvent.PlayerCreated>();
		return host;
	}

	private async Task<(Peer Peer, EventLog Log)> JoinAsync(string name, Peer host)
	{
		var joiner = CreatePeer(name);
		await joiner.Peer.ConnectAsync(Application, host.LocalEndPoint!);
		(await joiner.Log.WaitForAsync<PeerEvent.ConnectCompleted>())
			.Result.Should()
			.Be(ResultCode.Success);
		return joiner;
	}

	/// <summary>
	/// A host and two peers, each of which has seen all three players.
	/// </summary>
	private async Task<(Peer Peer, EventLog Log)[]> MeshAsync()
	{
		var host = await HostAsync("Host");
		var first = await JoinAsync("First", host.Peer);
		var second = await JoinAsync("Second", host.Peer);
		foreach (var (_, log) in new[] { host, first, second })
			await log.WaitForAsync<PeerEvent.PlayerCreated>(3);
		return [host, first, second];
	}

	[Fact]
	public async Task SetPeerInfo_AfterJoiningUpdatesEveryPeer()
	{
		var peers = await MeshAsync();
		var (_, first, _) = (peers[0], peers[1], peers[2]);

		await first.Peer.SetPeerInfo("Renamed", [7]);

		foreach (var (peer, log) in peers)
		{
			var updated = await log.WaitForAsync<PeerEvent.PeerInfoUpdated>();
			updated.Player.Id.Should().Be(first.Peer.LocalPlayer);
			updated.Player.Name.Should().Be("Renamed");
			updated.Player.Data.Should().Equal([7]);
			peer.GetPeerInfo(first.Peer.LocalPlayer)!.Name.Should().Be("Renamed");
		}
	}

	[Fact]
	public async Task SetPeerInfo_ByTheHostUpdatesEveryPeer()
	{
		var peers = await MeshAsync();
		var host = peers[0];

		await host.Peer.SetPeerInfo("New host name");

		foreach (var (peer, log) in peers)
		{
			(await log.WaitForAsync<PeerEvent.PeerInfoUpdated>())
				.Player.Name.Should()
				.Be("New host name");
			peer.GetPeerInfo(host.Peer.LocalPlayer)!.IsHost.Should().BeTrue();
		}
	}

	[Fact]
	public async Task SetPeerInfo_IsKnownToPeersThatJoinLater()
	{
		var host = await HostAsync("Host");
		var first = await JoinAsync("First", host.Peer);
		await first.Peer.SetPeerInfo("Renamed");

		var late = await JoinAsync("Late", host.Peer);

		late.Peer.GetPeerInfo(first.Peer.LocalPlayer)!.Name.Should().Be("Renamed");
	}

	[Fact]
	public async Task SetPeerInfo_TimesOutWhenTheHostDoesNotAnswer()
	{
		var host = await HostAsync("Host");
		var joiner = CreatePeer("Joiner", requestTimeout: TimeSpan.FromMilliseconds(300));
		await joiner.Peer.ConnectAsync(Application, host.Peer.LocalEndPoint!);
		await joiner.Log.WaitForAsync<PeerEvent.ConnectCompleted>();

		// The host never sees the request.
		_network.Drop = (_, _, datagram) => datagram.Length > 8 && datagram.Contains((byte)0xD6);

		var update = () => joiner.Peer.SetPeerInfo("Renamed");

		(await update.Should().ThrowAsync<DirectPlayException>())
			.Which.Result.Should()
			.Be(ResultCode.TimedOut);
	}

	[Fact]
	public async Task Groups_AreCreatedFilledAndDestroyedEverywhere()
	{
		var peers = await MeshAsync();
		var (host, first, second) = (peers[0], peers[1], peers[2]);

		// A non-host peer asks the host to create a group, then to add players to it.
		var group = await first.Peer.CreateGroupAsync("Team", [1, 2]);
		await first.Peer.AddPlayerToGroupAsync(group, first.Peer.LocalPlayer);
		await host.Peer.AddPlayerToGroupAsync(group, second.Peer.LocalPlayer);

		foreach (var (peer, log) in peers)
		{
			var created = await log.WaitForAsync<PeerEvent.GroupCreated>();
			created.Group.Id.Should().Be(group);
			created.Group.Name.Should().Be("Team");
			created.Group.Data.Should().Equal([1, 2]);
			created.Group.Owner.Should().Be(first.Peer.LocalPlayer);
			await log.WaitForAsync<PeerEvent.PlayerAddedToGroup>(2);
			peer.GetGroupInfo(group)!
				.Members.Should()
				.BeEquivalentTo([first.Peer.LocalPlayer, second.Peer.LocalPlayer]);
		}

		await second.Peer.RemovePlayerFromGroupAsync(group, second.Peer.LocalPlayer);
		foreach (var (peer, log) in peers)
		{
			var removed = await log.WaitForAsync<PeerEvent.PlayerRemovedFromGroup>();
			removed
				.Should()
				.Be(new PeerEvent.PlayerRemovedFromGroup(group, second.Peer.LocalPlayer));
		}

		await second.Peer.DestroyGroupAsync(group);
		foreach (var (peer, log) in peers)
		{
			var destroyed = await log.WaitForAsync<PeerEvent.GroupDestroyed>();
			destroyed.Should().Be(new PeerEvent.GroupDestroyed(group, DestroyGroupReason.Normal));
			peer.Groups.Should().BeEmpty();
		}
	}

	[Fact]
	public async Task SendTo_AGroupReachesItsMembersOnly()
	{
		var peers = await MeshAsync();
		var (host, first, second) = (peers[0], peers[1], peers[2]);
		var group = await host.Peer.CreateGroupAsync("Pair");
		await host.Peer.AddPlayerToGroupAsync(group, host.Peer.LocalPlayer);
		await host.Peer.AddPlayerToGroupAsync(group, second.Peer.LocalPlayer);
		await first.Log.WaitForAsync<PeerEvent.PlayerAddedToGroup>(2);

		// First is not a member, but can still send to the group.
		first.Peer.SendTo(group, [5]);

		(await host.Log.WaitForAsync<PeerEvent.DataReceived>()).Data.Should().Equal([5]);
		(await second.Log.WaitForAsync<PeerEvent.DataReceived>()).Data.Should().Equal([5]);
		await first.Log.WaitForAsync<PeerEvent.SendCompleted>();
		first.Log.OfType<PeerEvent.DataReceived>().Should().BeEmpty();

		// A member gets its own message back unless it asks not to.
		host.Peer.SendTo(group, [6], SendFlags.Guaranteed | SendFlags.NoLoopback);
		second.Peer.SendTo(group, [7]);
		await second.Log.WaitForAsync<PeerEvent.DataReceived>(3);
		second
			.Log.OfType<PeerEvent.DataReceived>()
			.Select(e => (int)e.Data[0])
			.Should()
			.BeEquivalentTo([5, 6, 7]);
		await host.Log.WaitForAsync<PeerEvent.DataReceived>(e => e.Data[0] == 7);
		host.Log.OfType<PeerEvent.DataReceived>().Should().NotContain(e => e.Data[0] == 6);
	}

	[Fact]
	public async Task Groups_AreKnownToPeersThatJoinLater()
	{
		var host = await HostAsync("Host");
		var first = await JoinAsync("First", host.Peer);
		var group = await host.Peer.CreateGroupAsync("Early", flags: GroupFlags.AutoDestruct);
		await host.Peer.AddPlayerToGroupAsync(group, first.Peer.LocalPlayer);

		var late = await JoinAsync("Late", host.Peer);

		var created = await late.Log.WaitForAsync<PeerEvent.GroupCreated>();
		created.Group.Id.Should().Be(group);
		created.Group.Flags.Should().Be(GroupFlags.AutoDestruct);
		created.Group.Members.Should().Equal([first.Peer.LocalPlayer]);
		(await late.Log.WaitForAsync<PeerEvent.PlayerAddedToGroup>())
			.Should()
			.Be(new PeerEvent.PlayerAddedToGroup(group, first.Peer.LocalPlayer));
	}

	[Fact]
	public async Task LeavingPlayer_LeavesItsGroupsAndTakesAutoDestructGroupsAlong()
	{
		var peers = await MeshAsync();
		var (host, first, second) = (peers[0], peers[1], peers[2]);
		var lasting = await first.Peer.CreateGroupAsync("Lasting");
		var owned = await first.Peer.CreateGroupAsync("Owned", flags: GroupFlags.AutoDestruct);
		await first.Peer.AddPlayerToGroupAsync(lasting, first.Peer.LocalPlayer);
		await second.Log.WaitForAsync<PeerEvent.PlayerAddedToGroup>();
		var leaving = first.Peer.LocalPlayer;

		await first.Peer.CloseAsync();

		foreach (var (peer, log) in new[] { host, second })
		{
			(await log.WaitForAsync<PeerEvent.PlayerRemovedFromGroup>())
				.Should()
				.Be(new PeerEvent.PlayerRemovedFromGroup(lasting, leaving));
			(await log.WaitForAsync<PeerEvent.GroupDestroyed>())
				.Should()
				.Be(new PeerEvent.GroupDestroyed(owned, DestroyGroupReason.AutoDestructed));
			peer.GetGroupInfo(lasting)!.Members.Should().BeEmpty();
		}
	}

	[Fact]
	public async Task Groups_RejectInvalidChanges()
	{
		var peers = await MeshAsync();
		var (host, first, _) = (peers[0], peers[1], peers[2]);
		var group = await host.Peer.CreateGroupAsync("Team");
		await first.Log.WaitForAsync<PeerEvent.GroupCreated>();
		await first.Peer.AddPlayerToGroupAsync(group, first.Peer.LocalPlayer);

		async Task ShouldFailAsync(Func<Task> action, ResultCode result) =>
			(await action.Should().ThrowAsync<DirectPlayException>())
				.Which.Result.Should()
				.Be(result);

		await ShouldFailAsync(
			() => first.Peer.AddPlayerToGroupAsync(group, first.Peer.LocalPlayer),
			ResultCode.PlayerAlreadyInGroup
		);
		await ShouldFailAsync(
			() => host.Peer.RemovePlayerFromGroupAsync(group, host.Peer.LocalPlayer),
			ResultCode.PlayerNotInGroup
		);
		await ShouldFailAsync(
			() => first.Peer.DestroyGroupAsync(new Dpnid(42)),
			ResultCode.InvalidGroup
		);
		await ShouldFailAsync(
			() => host.Peer.AddPlayerToGroupAsync(group, new Dpnid(42)),
			ResultCode.InvalidPlayer
		);
		await ShouldFailAsync(
			() => CreatePeer("Alone").Peer.CreateGroupAsync("Nowhere"),
			ResultCode.NoConnection
		);
	}
}
