using System.Net;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Reliable;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Tests;

/// <summary>
/// Client/server sessions ([MC-DPL8CS] sections 1.3.1 and 3.1).
/// </summary>
public class ClientServerTests : IAsyncLifetime
{
	private static readonly Guid GuidApplication = new("a7d0e5b2-61c4-4f3a-8e2d-9b0c1f4e6a37");

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
	private readonly List<Func<Task>> _closers = [];
	private int _nextAddress;

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync()
	{
		foreach (var close in _closers)
			await close().WaitAsync(TimeSpan.FromSeconds(10));
	}

	private static ApplicationDescription Application =>
		new() { GuidApplication = GuidApplication };

	private PeerOptions Options() =>
		new()
		{
			ReliableProfile = FastProfile,
			TransportFactory = _network.CreateFactory(
				IPAddress.Parse($"10.3.0.{Interlocked.Increment(ref _nextAddress)}")
			),
			JoinTimeout = TimeSpan.FromSeconds(5),
			RequestTimeout = TimeSpan.FromSeconds(5),
			CloseTimeout = TimeSpan.FromSeconds(2),
		};

	private async Task<(Server Server, EventLog Log)> StartServerAsync(
		ApplicationDescription? application = null
	)
	{
		var server = new Server(Options());
		_closers.Add(server.CloseAsync);
		await server.SetServerInfo("Server");
		var log = new EventLog(server.ReadEventsAsync());
		await server.HostAsync(application ?? Application);
		await log.WaitForAsync<PeerEvent.PlayerCreated>();
		return (server, log);
	}

	private async Task<(Client Client, EventLog Log)> ConnectAsync(string name, Server server)
	{
		var client = new Client(Options());
		_closers.Add(client.CloseAsync);
		await client.SetClientInfo(name);
		var log = new EventLog(client.ReadEventsAsync());
		await client.ConnectAsync(Application, server.LocalEndPoint!);
		(await log.WaitForAsync<PeerEvent.ConnectCompleted>())
			.Result.Should()
			.Be(ResultCode.Success);
		return (client, log);
	}

	[Fact]
	public async Task Clients_SeeOnlyTheServerAndThemselves()
	{
		var (server, serverLog) = await StartServerAsync();
		var (alice, aliceLog) = await ConnectAsync("Alice", server);
		var (bob, bobLog) = await ConnectAsync("Bob", server);

		await serverLog.WaitForAsync<PeerEvent.PlayerCreated>(3);
		server.Clients.Select(c => c.Name).Should().BeEquivalentTo(["Alice", "Bob"]);
		server.ApplicationDescription!.Flags.Should().HaveFlag(SessionFlags.ClientServer);

		foreach (
			var (client, log, name) in new[] { (alice, aliceLog, "Alice"), (bob, bobLog, "Bob") }
		)
		{
			var created = log.OfType<PeerEvent.PlayerCreated>();
			created.Should().HaveCount(2);
			created.Select(e => e.Player.Name).Should().BeEquivalentTo([name, "Server"]);
			client.GetServerInfo()!.Name.Should().Be("Server");
			client.GetServerInfo()!.Id.Should().Be(server.LocalPlayer);
		}

		// Bob joining told Alice nothing.
		aliceLog
			.OfType<PeerEvent.PlayerCreated>()
			.Should()
			.NotContain(e => e.Player.Name == "Bob");
	}

	[Fact]
	public async Task Data_FlowsBetweenServerAndClients()
	{
		var (server, serverLog) = await StartServerAsync();
		var (alice, aliceLog) = await ConnectAsync("Alice", server);
		var (_, bobLog) = await ConnectAsync("Bob", server);
		await serverLog.WaitForAsync<PeerEvent.PlayerCreated>(3);

		alice.Send([1]);
		var fromAlice = await serverLog.WaitForAsync<PeerEvent.DataReceived>();
		fromAlice.Sender.Should().Be(alice.LocalPlayer);
		fromAlice.Data.Should().Equal([1]);

		server.SendTo(alice.LocalPlayer, [2]);
		(await aliceLog.WaitForAsync<PeerEvent.DataReceived>(e => e.Data[0] == 2))
			.Sender.Should()
			.Be(server.LocalPlayer);

		server.SendTo(Peer.AllPlayers, [3], SendFlags.Guaranteed | SendFlags.NoLoopback);
		await aliceLog.WaitForAsync<PeerEvent.DataReceived>(e => e.Data[0] == 3);
		await bobLog.WaitForAsync<PeerEvent.DataReceived>(e => e.Data[0] == 3);
		bobLog.OfType<PeerEvent.DataReceived>().Should().NotContain(e => e.Data[0] == 1);
	}

	[Fact]
	public async Task InfoUpdates_ReachTheRightPlayers()
	{
		var (server, serverLog) = await StartServerAsync();
		var (alice, aliceLog) = await ConnectAsync("Alice", server);
		var (_, bobLog) = await ConnectAsync("Bob", server);
		await serverLog.WaitForAsync<PeerEvent.PlayerCreated>(3);

		// A client's change goes to the server only (DPN_MSGID_CLIENT_INFO).
		await alice.SetClientInfo("Alicia");
		(await serverLog.WaitForAsync<PeerEvent.PeerInfoUpdated>())
			.Player.Name.Should()
			.Be("Alicia");
		server.GetClientInfo(alice.LocalPlayer)!.Name.Should().Be("Alicia");

		// The server's change goes to every client (DPN_MSGID_SERVER_INFO).
		await server.SetServerInfo("Big server");
		(await aliceLog.WaitForAsync<PeerEvent.PeerInfoUpdated>(e => e.Player.Name == "Big server"))
			.Player.IsHost.Should()
			.BeTrue();
		await bobLog.WaitForAsync<PeerEvent.PeerInfoUpdated>(e => e.Player.Name == "Big server");
		bobLog
			.OfType<PeerEvent.PeerInfoUpdated>()
			.Should()
			.NotContain(e => e.Player.Name == "Alicia");
	}

	[Fact]
	public async Task ServerGroups_ReachTheirMembers()
	{
		var (server, serverLog) = await StartServerAsync();
		var (alice, aliceLog) = await ConnectAsync("Alice", server);
		var (_, bobLog) = await ConnectAsync("Bob", server);
		await serverLog.WaitForAsync<PeerEvent.PlayerCreated>(3);

		var group = await server.CreateGroupAsync("Red team");
		await server.AddPlayerToGroupAsync(group, alice.LocalPlayer);
		server.SendTo(group, [9]);

		(await aliceLog.WaitForAsync<PeerEvent.DataReceived>()).Data.Should().Equal([9]);
		server.GetGroupInfo(group)!.Members.Should().Equal([alice.LocalPlayer]);
		await Task.Delay(100);
		bobLog.OfType<PeerEvent.DataReceived>().Should().BeEmpty();
		aliceLog.OfType<PeerEvent.GroupCreated>().Should().BeEmpty();
	}

	[Fact]
	public async Task DestroyClient_DisconnectsIt()
	{
		var (server, serverLog) = await StartServerAsync();
		var (alice, aliceLog) = await ConnectAsync("Alice", server);
		await serverLog.WaitForAsync<PeerEvent.PlayerCreated>(2);
		var id = alice.LocalPlayer;

		await server.DestroyClientAsync(id);

		(await serverLog.WaitForAsync<PeerEvent.PlayerDestroyed>())
			.Should()
			.Be(new PeerEvent.PlayerDestroyed(id, DestroyPlayerFlags.HostDestroyedPlayer));
		(await aliceLog.WaitForAsync<PeerEvent.SessionTerminated>())
			.Result.Should()
			.Be(ResultCode.HostTerminatedSession);
		server.Clients.Should().BeEmpty();
	}

	[Fact]
	public async Task ClientLeaving_IsDestroyedOnTheServer()
	{
		var (server, serverLog) = await StartServerAsync();
		var (alice, _) = await ConnectAsync("Alice", server);
		await serverLog.WaitForAsync<PeerEvent.PlayerCreated>(2);
		var id = alice.LocalPlayer;

		await alice.CloseAsync();

		(await serverLog.WaitForAsync<PeerEvent.PlayerDestroyed>())
			.Should()
			.Be(new PeerEvent.PlayerDestroyed(id, DestroyPlayerFlags.Normal));
	}

	[Fact]
	public async Task ServerLeaving_EndsTheSessionEvenWithMigrateHost()
	{
		var (server, _) = await StartServerAsync(
			Application with
			{
				Flags = SessionFlags.MigrateHost,
			}
		);
		var (_, aliceLog) = await ConnectAsync("Alice", server);
		var (_, bobLog) = await ConnectAsync("Bob", server);

		await server.CloseAsync();

		foreach (var log in new[] { aliceLog, bobLog })
		{
			(await log.WaitForAsync<PeerEvent.SessionTerminated>())
				.Result.Should()
				.Be(ResultCode.HostTerminatedSession);
			log.OfType<PeerEvent.HostMigrated>().Should().BeEmpty();
		}
	}

	[Fact]
	public async Task PeersAndClients_CannotJoinTheOtherKindOfSession()
	{
		var (server, _) = await StartServerAsync();
		var peer = new Peer(Options());
		_closers.Add(peer.CloseAsync);
		var peerLog = new EventLog(peer);
		await peer.ConnectAsync(Application, server.LocalEndPoint!);
		(await peerLog.WaitForAsync<PeerEvent.ConnectCompleted>())
			.Result.Should()
			.Be(ResultCode.InvalidInterface);

		var host = new Peer(Options());
		_closers.Add(host.CloseAsync);
		await host.HostAsync(Application);
		var client = new Client(Options());
		_closers.Add(client.CloseAsync);
		var clientLog = new EventLog(client.ReadEventsAsync());
		await client.ConnectAsync(Application, host.LocalEndPoint!);
		(await clientLog.WaitForAsync<PeerEvent.ConnectCompleted>())
			.Result.Should()
			.Be(ResultCode.InvalidInterface);
	}

	[Fact]
	public async Task EnumHosts_FindsTheServer()
	{
		var (server, _) = await StartServerAsync(Application with { SessionName = "Lobby" });
		var client = new Client(Options());
		_closers.Add(client.CloseAsync);

		var found = new List<EnumeratedHost>();
		await foreach (
			var host in client.EnumHostsAsync(
				server.LocalEndPoint!,
				options: new EnumHostsOptions
				{
					Count = 1,
					Timeout = TimeSpan.FromMilliseconds(200),
				}
			)
		)
			found.Add(host);

		found.Should().ContainSingle().Which.Application.SessionName.Should().Be("Lobby");
		found[0].Application.Flags.Should().HaveFlag(SessionFlags.ClientServer);
	}
}
