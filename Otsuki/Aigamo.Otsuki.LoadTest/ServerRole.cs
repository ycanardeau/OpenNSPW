using System.Diagnostics;
using System.Net;

namespace Aigamo.Otsuki.LoadTest;

/// <summary>
/// The server of the load: hosts a client/server session, takes the clients' messages, and sends an update to every
/// client <paramref name="updatesPerSecond"/> times a second. Reports each second.
/// </summary>
internal sealed class ServerRole(
	int port,
	int loops,
	int updatesPerSecond,
	int size,
	TimeSpan duration,
	bool guaranteed
)
{
	public static readonly Guid Application = new("5b0e2c1d-6a7f-4c55-9d0e-0a1b2c3d4e5f");

	private readonly int _port = port;
	private readonly int _loops = loops;
	private readonly int _updatesPerSecond = updatesPerSecond;
	private readonly int _size = size;
	private readonly TimeSpan _duration = duration;
	private readonly SendFlags _flags =
		(guaranteed ? SendFlags.Guaranteed : SendFlags.None)
		| SendFlags.NoComplete
		| SendFlags.NoLoopback;
	private readonly DatagramCounts _counts = new();
	private readonly Histogram _latency = new();
	private readonly ProcessStats _stats = new();
	private int _clients;
	private long _messagesIn;
	private long _messagesOut;
	private long _lastDatagramsIn;
	private long _lastDatagramsOut;

	private void Handle(PeerEvent peerEvent)
	{
		switch (peerEvent)
		{
			case PeerEvent.DataReceived data:
				_messagesIn++;
				var age = Payload.Age(data.Data.AsSpan(), Stopwatch.GetTimestamp());
				if (age >= 0)
					_latency.Record(age);
				break;
			case PeerEvent.PlayerCreated created when !created.Player.IsLocal:
				_clients++;
				break;
			case PeerEvent.PlayerDestroyed:
				_clients--;
				break;
		}
	}

	private void Report()
	{
		var datagramsIn = _counts.Received.Value;
		var datagramsOut = _counts.Sent.Value;
		var report = _stats.Sample(
			new Report
			{
				Role = "server",
				Clients = _clients,
				MessagesIn = _messagesIn,
				MessagesOut = _messagesOut,
				DatagramsIn = datagramsIn - _lastDatagramsIn,
				DatagramsOut = datagramsOut - _lastDatagramsOut,
				P50 = _latency.Percentile(0.5),
				P99 = _latency.Percentile(0.99),
				P999 = _latency.Percentile(0.999),
				Max = _latency.Max,
			}
		);
		Console.Out.WriteLine(report.ToJson());
		Console.Out.Flush();
		_messagesIn = 0;
		_messagesOut = 0;
		_lastDatagramsIn = datagramsIn;
		_lastDatagramsOut = datagramsOut;
		_latency.Clear();
	}

	public async Task RunAsync()
	{
		var options = new PeerOptions
		{
			ConnectionLoops = _loops,
			TransportFactory = localEndPoint =>
				new CountingTransport(
					PeerOptions.Default.TransportFactory(localEndPoint),
					_counts
				),
		};
		await using var server = new Server(options);
		await server.HostAsync(
			new ApplicationDescription { GuidApplication = Application, SessionName = "load" },
			new IPEndPoint(IPAddress.Loopback, _port)
		);
		Console.Error.WriteLine($"server: hosting on {server.LocalEndPoint}");

		var start = Stopwatch.GetTimestamp();
		var updateInterval =
			_updatesPerSecond > 0 ? Stopwatch.Frequency / _updatesPerSecond : long.MaxValue;
		var nextUpdate = start + updateInterval;
		var nextReport = start + Stopwatch.Frequency;
		var end = start + (long)(_duration.TotalSeconds * Stopwatch.Frequency);
		while (true)
		{
			var handled = server.DoWork(Handle, 65536);
			var now = Stopwatch.GetTimestamp();
			if (now >= end)
				break;

			if (now >= nextUpdate)
			{
				nextUpdate += updateInterval;
				if (_clients > 0)
				{
					server.SendTo(Peer.AllPlayers, Payload.Create(_size), _flags);
					_messagesOut += _clients;
				}
			}

			if (now >= nextReport)
			{
				nextReport += Stopwatch.Frequency;
				Report();
			}

			if (handled == 0)
				Thread.Sleep(1);
		}
	}
}
