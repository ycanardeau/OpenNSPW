using System.Net;
using System.Threading.Channels;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Transport;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Tests;

public class StreamDatagramTransportTests : IDisposable
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

	private static readonly ApplicationDescription Application = new()
	{
		GuidApplication = new("8b3c2f4e-6a1d-4e7b-a5c9-0f2d8e6b4a17"),
	};

	private readonly InMemoryStreamNetwork _network = new();
	private readonly List<IDisposable> _disposables = [];

	public void Dispose()
	{
		foreach (var disposable in _disposables)
			disposable.Dispose();
	}

	private (StreamDatagramTransport Transport, ChannelReader<(EndPoint, byte[])> Received) Start(
		string name
	)
	{
		var transport = _network.CreateTransport(new NamedEndPoint(name));
		_disposables.Add(transport);
		var received = Channel.CreateUnbounded<(EndPoint, byte[])>();
		transport.Start(
			(remoteEndPoint, datagram) => received.Writer.TryWrite((remoteEndPoint, datagram))
		);
		return (transport, received.Reader);
	}

	private static async Task<(EndPoint, byte[])> ReceiveAsync(
		ChannelReader<(EndPoint, byte[])> received
	)
	{
		using var timeout = new CancellationTokenSource(Timeout);
		return await received.ReadAsync(timeout.Token);
	}

	/// <summary>
	/// Sends <paramref name="datagram"/> again and again until it arrives, for links that need a retry to open.
	/// </summary>
	private static async Task<(EndPoint, byte[])> SendUntilReceivedAsync(
		StreamDatagramTransport from,
		EndPoint to,
		byte[] datagram,
		ChannelReader<(EndPoint, byte[])> received
	)
	{
		using var timeout = new CancellationTokenSource(Timeout);
		while (true)
		{
			from.Send(to, datagram);
			using var retry = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
			retry.CancelAfter(TimeSpan.FromMilliseconds(20));
			try
			{
				return await received.ReadAsync(retry.Token);
			}
			catch (OperationCanceledException) when (!timeout.IsCancellationRequested) { }
		}
	}

	[Fact]
	public async Task Send_OpensOneStreamAndKeepsTheOrder()
	{
		var (a, _) = Start("a");
		var (b, receivedByB) = Start("b");

		a.Send(b.LocalEndPoint, [1]);
		a.Send(b.LocalEndPoint, [2, 2]);
		a.Send(b.LocalEndPoint, [3, 3, 3]);

		(await ReceiveAsync(receivedByB))
			.Should()
			.BeEquivalentTo((a.LocalEndPoint, new byte[] { 1 }));
		(await ReceiveAsync(receivedByB)).Item2.Should().Equal(2, 2);
		(await ReceiveAsync(receivedByB)).Item2.Should().Equal(3, 3, 3);
		_network.ConnectCount.Should().Be(1);
	}

	[Fact]
	public async Task Send_RepliesOverTheAcceptedStream()
	{
		var (a, receivedByA) = Start("a");
		var (b, receivedByB) = Start("b");
		a.Send(b.LocalEndPoint, [1]);
		var (sender, _) = await ReceiveAsync(receivedByB);

		b.Send(sender, [2]);

		(await ReceiveAsync(receivedByA))
			.Should()
			.BeEquivalentTo((b.LocalEndPoint, new byte[] { 2 }));
		_network.ConnectCount.Should().Be(1);
	}

	[Fact]
	public async Task Send_KeepsDatagramBoundariesUpToTheLargestLength()
	{
		var (a, _) = Start("a");
		var (b, receivedByB) = Start("b");
		var largest = Enumerable.Range(0, ushort.MaxValue).Select(i => (byte)i).ToArray();

		a.Send(b.LocalEndPoint, []);
		a.Send(b.LocalEndPoint, largest);
		a.Send(b.LocalEndPoint, new byte[ushort.MaxValue + 1]);
		a.Send(b.LocalEndPoint, [7]);

		(await ReceiveAsync(receivedByB)).Item2.Should().BeEmpty();
		(await ReceiveAsync(receivedByB)).Item2.Should().Equal(largest);
		(await ReceiveAsync(receivedByB)).Item2.Should().Equal(7);
	}

	[Fact]
	public async Task Send_ToAnEndPointThatRefuses_IsLostUntilItAccepts()
	{
		var (a, _) = Start("a");
		a.Send(new NamedEndPoint("b"), [1]);
		await Task.Delay(50);
		var (b, receivedByB) = Start("b");

		(await SendUntilReceivedAsync(a, b.LocalEndPoint, [2], receivedByB))
			.Item2.Should()
			.Equal(2);
		receivedByB.TryRead(out _).Should().BeFalse();
	}

	[Fact]
	public async Task Send_OpensANewStreamToAPeerThatCameBack()
	{
		var (a, _) = Start("a");
		var (b, receivedByB) = Start("b");
		a.Send(b.LocalEndPoint, [1]);
		await ReceiveAsync(receivedByB);

		b.Dispose();
		var (newB, receivedByNewB) = Start("b");

		(await SendUntilReceivedAsync(a, newB.LocalEndPoint, [2], receivedByNewB))
			.Item2.Should()
			.Equal(2);
		_network.ConnectCount.Should().Be(2);
	}

	[Fact]
	public async Task Peers_TalkOverStreams()
	{
		var options = new PeerOptions { TransportFactory = _network.CreateFactory() };
		await using var host = new Peer(options);
		var hostLog = new EventLog(host);
		await host.SetPeerInfo("Host");
		await host.HostAsync(Application, new NamedEndPoint("host"));
		await using var joiner = new Peer(options);
		var joinerLog = new EventLog(joiner);
		await joiner.SetPeerInfo("Joiner");

		await joiner.ConnectAsync(Application, host.LocalEndPoint!, new NamedEndPoint("joiner"));

		(await joinerLog.WaitForAsync<PeerEvent.ConnectCompleted>())
			.Result.Should()
			.Be(ResultCode.Success);
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
		_network.ConnectCount.Should().Be(1);
	}
}
