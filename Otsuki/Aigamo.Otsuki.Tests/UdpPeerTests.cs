using System.Net;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Transport;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Tests;

public class UdpPeerTests
{
	private static async Task TalkAsync(
		PeerOptions? options = null,
		PeerOptions? joinerOptions = null
	)
	{
		var application = new ApplicationDescription { GuidApplication = Guid.NewGuid() };
		await using var host = new Peer(options);
		var hostLog = new EventLog(host);
		host.SetPeerInfo("Host");
		await host.HostAsync(application, port: 0);
		await using var joiner = new Peer(joinerOptions ?? options);
		var joinerLog = new EventLog(joiner);
		joiner.SetPeerInfo("Joiner");

		await joiner.ConnectAsync(
			application,
			new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)host.LocalEndPoint!).Port)
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

	private static async Task PollAsync(UdpPoller poller, CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			poller.Poll();
			await Task.Delay(1, CancellationToken.None);
		}
	}

	[Fact]
	public async Task Peers_TalkOverLoopbackUdp() => await TalkAsync();

	[Fact]
	public async Task Peers_TalkOverLoopbackUdp_ReceivedByOnePoller()
	{
		var poller = new UdpPoller();
		using var cancellation = new CancellationTokenSource();
		var polling = PollAsync(poller, cancellation.Token);

		// The peers close, which takes the poller's acknowledgments, before it stops; their endpoints dispose the
		// transports after that, once their loops are done.
		await TalkAsync(new PeerOptions { TransportFactory = poller.CreateTransport });
		await cancellation.CancelAsync();
		await polling;
		var deadline = DateTime.UtcNow + EventLog.DefaultTimeout;
		poller.Poll();
		while (poller.Sockets > 0 && DateTime.UtcNow < deadline)
		{
			await Task.Delay(10);
			poller.Poll();
		}

		poller.Sockets.Should().Be(0, "the closed peers' sockets are no longer read");
	}

	// The host spread over two loops, each with a socket on its port, and the joiner on one loop.
	[Fact]
	public async Task Peers_TalkOverLoopbackUdp_OnPollingLoops()
	{
		await using var loops = new PollingLoops(2);

		await TalkAsync(loops.Spread(), loops.OnOneLoop());
	}
}
