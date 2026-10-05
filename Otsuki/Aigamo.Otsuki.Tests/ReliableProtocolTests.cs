using System.Net;
using System.Threading.Channels;
using Aigamo.Otsuki.Messages.Reliable;
using FluentAssertions;
using Xunit;

namespace Aigamo.Otsuki.Tests;

/// <summary>
/// Drives a hosting peer with hand-written datagrams, following the samples of [MC-DPL8R] section 4.
/// </summary>
public class ReliableProtocolTests : IAsyncLifetime
{
	private static readonly SessionId SampleSessionId = new(0x79C9AEC6);

	private readonly InMemoryNetwork _network = new();
	private readonly Channel<ReliableMessage> _received =
		Channel.CreateUnbounded<ReliableMessage>();
	private readonly Peer _host;
	private InMemoryTransport _remote = null!;

	public ReliableProtocolTests() =>
		_host = new Peer(
			new PeerOptions
			{
				TransportFactory = _network.CreateFactory(IPAddress.Parse("10.0.0.1")),
			}
		);

	public async Task InitializeAsync()
	{
		await _host.HostAsync(new ApplicationDescription { GuidApplication = Guid.NewGuid() });
		_remote = _network.Bind(IPAddress.Parse("10.0.0.2"), 5000);
		_remote.Start(
			(_, datagram) =>
				_received.Writer.TryWrite(ReliableMessageSerializer.Default.Deserialize(datagram)!)
		);
	}

	public Task DisposeAsync() => _host.CloseAsync();

	private void Send(string hex) =>
		_remote.Send(_host.LocalEndPoint!, Convert.FromHexString(hex.Replace(" ", "")));

	private async Task<T> ReceiveAsync<T>(Func<T, bool>? predicate = null)
		where T : ReliableMessage
	{
		using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		while (true)
		{
			var message = await _received.Reader.ReadAsync(cancellation.Token);
			if (message is T match && (predicate?.Invoke(match) ?? true))
				return match;
		}
	}

	private async Task ConnectAsync()
	{
		// Connector to listener: CONNECT, then CONNECTED and a KeepAlive.
		Send("88 01 00 00 06 00 01 00 C6 AE C9 79 9D 36 67 23");
		await ReceiveAsync<ReliableMessage.Connected>();
		Send("80 02 01 00 06 00 01 00 C6 AE C9 79 9D 36 67 23");
		Send("3F 02 00 00 C6 AE C9 79");
	}

	[Fact]
	public async Task Connect_IsAnsweredWithPolledConnected()
	{
		Send("88 01 00 00 06 00 01 00 C6 AE C9 79 9D 36 67 23");

		var connected = await ReceiveAsync<ReliableMessage.Connected>();
		connected.Poll.Should().BeTrue();
		connected.MessageId.Should().Be(0);
		connected.ResponseId.Should().Be(0);
		connected.ProtocolVersion.Should().Be(0x00010006);
		connected.SessionId.Should().Be(SampleSessionId);
	}

	[Fact]
	public async Task RetriedConnect_IsAnsweredAgain()
	{
		Send("88 01 00 00 06 00 01 00 C6 AE C9 79 9D 36 67 23");
		await ReceiveAsync<ReliableMessage.Connected>();

		Send("88 01 01 00 06 00 01 00 C6 AE C9 79 9D 36 67 23");

		var connected = await ReceiveAsync<ReliableMessage.Connected>(m => m.ResponseId == 1);
		connected.SessionId.Should().Be(SampleSessionId);
	}

	[Fact]
	public async Task Established_ExchangesKeepAlives()
	{
		await ConnectAsync();

		var keepAlive = await ReceiveAsync<ReliableMessage.DataFrame>();
		keepAlive.KeepAliveOrCorrelate.Should().BeTrue();
		keepAlive.SessionId.Should().Be(SampleSessionId);
		keepAlive.Reliable.Should().BeTrue();
		keepAlive.SequenceId.Should().Be(new SequenceId(0));

		// The connector's KeepAlive polls, so it is acknowledged immediately.
		var acknowledgment = await ReceiveAsync<ReliableMessage.Sack>();
		acknowledgment.NextReceive.Should().Be(new SequenceId(1));
		acknowledgment.NextSend.Should().Be(new SequenceId(1));
	}

	[Fact]
	public async Task DataFrame_IsAcknowledged()
	{
		await ConnectAsync();
		await ReceiveAsync<ReliableMessage.Sack>(m => m.NextReceive == new SequenceId(1));

		// Unreliable, sequential, polled, complete message "ABCDE" with sequence ID 1, as in section 4.2.
		Send("3D 00 01 01 41 42 43 44 45");

		var acknowledgment = await ReceiveAsync<ReliableMessage.Sack>(m =>
			m.NextReceive == new SequenceId(2)
		);
		acknowledgment.Response.Should().BeTrue();
	}

	[Fact]
	public async Task OutOfOrderFrame_IsSelectivelyAcknowledged()
	{
		await ConnectAsync();
		await ReceiveAsync<ReliableMessage.Sack>(m => m.NextReceive == new SequenceId(1));

		// Sequence ID 2 arrives while 1 is missing.
		Send("3F 00 02 01 41");

		var acknowledgment = await ReceiveAsync<ReliableMessage.Sack>(m => m.SackMask != 0);
		acknowledgment.NextReceive.Should().Be(new SequenceId(1));
		acknowledgment.SackMask.Should().Be(1);
	}

	[Fact]
	public async Task HardDisconnect_IsAcknowledged()
	{
		await ConnectAsync();

		Send("80 04 02 00 06 00 01 00 C6 AE C9 79 9D 36 67 23");

		var hardDisconnect = await ReceiveAsync<ReliableMessage.HardDisconnect>();
		hardDisconnect.SessionId.Should().Be(SampleSessionId);
	}
}
