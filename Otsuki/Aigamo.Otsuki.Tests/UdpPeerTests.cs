using System.Net;
using Aigamo.Otsuki.Messages.Core;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Tests;

public class UdpPeerTests
{
	[Fact]
	public async Task Peers_TalkOverLoopbackUdp()
	{
		var application = new ApplicationDescription { GuidApplication = Guid.NewGuid() };
		await using var host = new Peer();
		var hostLog = new EventLog(host);
		host.SetPeerInfo("Host");
		await host.HostAsync(application, port: 0);
		await using var joiner = new Peer();
		var joinerLog = new EventLog(joiner);
		joiner.SetPeerInfo("Joiner");

		await joiner.ConnectAsync(
			application,
			new IPEndPoint(IPAddress.Loopback, host.LocalEndPoint!.Port)
		);

		(await joinerLog.WaitForAsync<PeerEvent.ConnectCompleted>())
			.Result.Should()
			.Be(ResultCode.Success);
		await hostLog.WaitForAsync<PeerEvent.PlayerCreated>(e => e.Player.Name == "Joiner");
		joiner.SendTo(host.LocalPlayer, "hello"u8);
		(await hostLog.WaitForAsync<PeerEvent.DataReceived>())
			.Data.Should()
			.Equal("hello"u8.ToArray());
	}
}
