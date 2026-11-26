using System.Diagnostics;
using System.Net;
using Aigamo.Otsuki.Actors;
using Aigamo.Otsuki.Transport;

namespace Aigamo.Otsuki.LoadTest;

/// <summary>
/// The server of the load: hosts a client/server session, takes the clients' messages, and sends an update to every
/// client <paramref name="updatesPerSecond"/> times a second. Reports each second. <paramref name="onOneThread"/>, its
/// mailboxes run on the thread that takes its events, and it receives on one socket that the thread polls, as the
/// clients do; <paramref name="threads"/> above 0, on as many <see cref="PollingLoops"/>, spread over them; otherwise on
/// the thread pool, with a receive waiting on each of its sockets.
/// </summary>
internal sealed class ServerRole(
	int port,
	int loops,
	int updatesPerSecond,
	int size,
	TimeSpan duration,
	bool guaranteed,
	bool onOneThread,
	int threads
)
{
	public static readonly Guid Application = new("5b0e2c1d-6a7f-4c55-9d0e-0a1b2c3d4e5f");

	private readonly int _port = port;
	private readonly int _loops = loops;
	private readonly int _updatesPerSecond = updatesPerSecond;
	private readonly int _size = size;
	private readonly TimeSpan _duration = duration;
	private readonly bool _onOneThread = onOneThread;
	private readonly int _threads = threads;
	private readonly UdpPoller _poller = new();
	private readonly CallerScheduler _scheduler = new();
	private readonly SendFlags _flags =
		(guaranteed ? SendFlags.Guaranteed : SendFlags.None)
		| SendFlags.NoComplete
		| SendFlags.NoLoopback;
	private readonly DatagramCounts _counts = new();
	private readonly Histogram _latency = new();
	private readonly ProcessStats _stats = new();
	private readonly Wakes _wakes = new();
	private (long Session, long Loop, long Endpoint, long Timer) _lastWakes;
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

	private void Report(PeerQueues queues)
	{
		var datagramsIn = _counts.Received.Value;
		var datagramsOut = _counts.Sent.Value;
		(long Session, long Loop, long Endpoint, long Timer) wakes = (
			_wakes.SessionRuns.Value,
			_wakes.LoopRuns.Value,
			_wakes.EndpointRuns.Value,
			_wakes.TimerCallbacks.Value
		);
		var report = _stats.Sample(
			new Report
			{
				Role = "server",
				Clients = _clients,
				SessionQueue = queues.Session,
				LoopQueue = queues.ConnectionLoops.DefaultIfEmpty().Max(),
				EventQueue = queues.Events,
				MessagesIn = _messagesIn,
				MessagesOut = _messagesOut,
				DatagramsIn = datagramsIn - _lastDatagramsIn,
				DatagramsOut = datagramsOut - _lastDatagramsOut,
				SendErrors = _counts.SendErrors.Value,
				P50 = _latency.Percentile(0.5),
				P99 = _latency.Percentile(0.99),
				P999 = _latency.Percentile(0.999),
				Max = _latency.Max,
				SessionRuns = wakes.Session - _lastWakes.Session,
				LoopRuns = wakes.Loop - _lastWakes.Loop,
				EndpointRuns = wakes.Endpoint - _lastWakes.Endpoint,
				TimerCallbacks = wakes.Timer - _lastWakes.Timer,
			}
		);
		Console.Out.WriteLine(report.ToJson());
		Console.Out.Flush();
		_messagesIn = 0;
		_messagesOut = 0;
		_lastDatagramsIn = datagramsIn;
		_lastDatagramsOut = datagramsOut;
		_lastWakes = wakes;
		_latency.Clear();
	}

	// Receives the datagrams and runs the mailboxes that wait, on one thread; nothing to do on the thread pool.
	private int Pump()
	{
		if (!_onOneThread)
			return 0;

		var received = _poller.Poll();
		_scheduler.RunReady();
		return received;
	}

	// Until task completes, which on one thread needs the mailboxes run here.
	private void PumpUntil(Task task)
	{
		while (!task.IsCompleted)
		{
			if (Pump() == 0)
				Thread.Sleep(1);
		}
	}

	public async Task RunAsync()
	{
		await using var loops = _threads > 0 ? new PollingLoops(_threads) : null;
		var options = new PeerOptions
		{
			ConnectionLoops = _loops,
			Scheduler = _onOneThread ? _scheduler : new CountingScheduler(_wakes),
			TimeProvider = new CountingTimeProvider(_wakes),
			TransportFactory = localEndPoint =>
				new CountingTransport(
					_onOneThread
						? _poller.CreateTransport(localEndPoint)
						: new UdpDatagramTransport((IPEndPoint)localEndPoint, sockets: _loops),
					_counts
				),
		};
		if (loops is not null)
		{
			var spread = loops.Spread(options);
			options = spread with
			{
				TransportFactory = localEndPoint =>
					new CountingTransport(spread.TransportFactory(localEndPoint), _counts),
			};
		}

		var server = new Server(options);
		var hosting = server.HostAsync(
			new ApplicationDescription { GuidApplication = Application, SessionName = "load" },
			new IPEndPoint(IPAddress.Loopback, _port)
		);
		PumpUntil(hosting);
		await hosting;
		Console.Error.WriteLine($"server: hosting on {server.LocalEndPoint}");

		var start = Stopwatch.GetTimestamp();
		var updateInterval =
			_updatesPerSecond > 0 ? Stopwatch.Frequency / _updatesPerSecond : long.MaxValue;
		var nextUpdate = start + updateInterval;
		var nextReport = start + Stopwatch.Frequency;
		var end = start + (long)(_duration.TotalSeconds * Stopwatch.Frequency);
		while (true)
		{
			var handled = Pump();
			handled += server.DoWork(Handle, 65536);
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
					Pump();
				}
			}

			if (now >= nextReport)
			{
				nextReport += Stopwatch.Frequency;
				Report(server.Queues);
			}

			if (handled == 0)
				Thread.Sleep(1);
		}

		var closing = server.DisposeAsync().AsTask();
		PumpUntil(closing);
		await closing;
	}
}
