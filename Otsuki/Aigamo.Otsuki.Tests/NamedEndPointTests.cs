using Aigamo.Otsuki.Messages.Core;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Tests;

/// <summary>
/// Sessions over a transport whose endpoints are not IP addresses.
/// </summary>
public class NamedEndPointTests
{
	private static readonly ApplicationDescription Application = new()
	{
		GuidApplication = new("3f0e7c1a-5b8d-4c2e-9a61-2d7b4e8f0c35"),
	};

	private readonly NamedNetwork _network = new();

	private PeerOptions Options => new() { TransportFactory = _network.CreateFactory() };

	[Fact]
	public async Task Peers_TalkOverNamedEndPoints()
	{
		await using var host = new Peer(Options);
		var hostLog = new EventLog(host);
		host.SetPeerInfo("Host");
		await host.HostAsync(Application, new NamedEndPoint("host"));
		await using var joiner = new Peer(Options);
		var joinerLog = new EventLog(joiner);
		joiner.SetPeerInfo("Joiner");

		await joiner.ConnectAsync(Application, host.LocalEndPoint!, new NamedEndPoint("joiner"));

		(await joinerLog.WaitForAsync<PeerEvent.ConnectCompleted>())
			.Result.Should()
			.Be(ResultCode.Success);
		host.LocalEndPoint.Should().Be(new NamedEndPoint("host"));
		joiner.LocalEndPoint.Should().Be(new NamedEndPoint("joiner"));
		var joinerPlayer = (
			await hostLog.WaitForAsync<PeerEvent.PlayerCreated>(e => e.Player.Name == "Joiner")
		).Player;
		joiner.SendTo(host.LocalPlayer, "hello"u8);
		host.SendTo(joinerPlayer.Id, "welcome"u8);
		(await hostLog.WaitForAsync<PeerEvent.DataReceived>())
			.Data.Should()
			.Equal("hello"u8.ToArray());
		(await joinerLog.WaitForAsync<PeerEvent.DataReceived>())
			.Data.Should()
			.Equal("welcome"u8.ToArray());
	}

	[Fact]
	public async Task Connect_NeedsTheLocalEndPointOfANamedHost()
	{
		await using var joiner = new Peer(Options);

		await joiner
			.Awaiting(p => p.ConnectAsync(Application, new NamedEndPoint("host")))
			.Should()
			.ThrowAsync<ArgumentException>();
	}

	[Fact]
	public async Task Host_OverUdp_RejectsANamedEndPoint()
	{
		await using var host = new Peer();

		await host.Awaiting(p => p.HostAsync(Application, new NamedEndPoint("host")))
			.Should()
			.ThrowAsync<NotSupportedException>();
	}
}
