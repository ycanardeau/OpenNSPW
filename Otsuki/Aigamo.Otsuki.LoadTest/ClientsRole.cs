using System.Diagnostics;
using System.Net;

namespace Aigamo.Otsuki.LoadTest;

/// <summary>
/// Clients of the load, in one process: <paramref name="count"/> of them join the server, <paramref name="joinsPerSecond"/>
/// a second, and each sends <paramref name="messagesPerSecond"/> messages a second, spread over the second. One thread
/// drives them all and takes their events. Reports each second.
/// </summary>
internal sealed class ClientsRole(
	int id,
	IPEndPoint server,
	int count,
	int joinsPerSecond,
	int messagesPerSecond,
	int size,
	TimeSpan duration,
	bool guaranteed
)
{
	private sealed class Load(Client client, long phase)
	{
		public Client Client { get; } = client;

		public long Phase { get; } = phase;

		public Task? Connecting { get; set; }

		public bool IsConnected { get; set; }

		public long NextSend { get; set; }
	}

	private readonly int _id = id;
	private readonly IPEndPoint _server = server;
	private readonly int _count = count;
	private readonly int _joinsPerSecond = joinsPerSecond;
	private readonly int _messagesPerSecond = messagesPerSecond;
	private readonly int _size = size;
	private readonly TimeSpan _duration = duration;
	private readonly SendFlags _flags =
		(guaranteed ? SendFlags.Guaranteed : SendFlags.None) | SendFlags.NoComplete;
	private readonly DatagramCounts _counts = new();
	private readonly Histogram _latency = new();
	private readonly ProcessStats _stats = new();
	private readonly List<Load> _loads = [];
	private int _connected;
	private int _failed;
	private int _terminated;
	private long _messagesIn;
	private long _messagesOut;
	private long _lastDatagramsIn;
	private long _lastDatagramsOut;

	private void Handle(PeerEvent peerEvent)
	{
		if (peerEvent is PeerEvent.DataReceived data)
		{
			_messagesIn++;
			var age = Payload.Age(data.Data.AsSpan(), Stopwatch.GetTimestamp());
			if (age >= 0)
				_latency.Record(age);
		}
	}

	private void Report()
	{
		var datagramsIn = _counts.Received.Value;
		var datagramsOut = _counts.Sent.Value;
		var report = _stats.Sample(
			new Report
			{
				Role = "clients",
				Id = _id,
				Clients = _connected,
				Failed = _failed,
				Terminated = _terminated,
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

	private void Join(Load load)
	{
		load.Connecting = load.Client.ConnectAsync(
			new ApplicationDescription { GuidApplication = ServerRole.Application },
			_server,
			localPort: 0
		);
	}

	private void Check(Load load)
	{
		if (load.Connecting is not { IsCompleted: true } connecting)
			return;

		load.Connecting = null;
		if (connecting.IsCompletedSuccessfully)
			return;

		_failed++;
	}

	private void Drive(Load load, long now, long sendInterval)
	{
		while (load.Client.TryGetEvent(out var peerEvent))
		{
			switch (peerEvent)
			{
				case PeerEvent.ConnectCompleted { Result: Messages.Core.ResultCode.Success }:
					load.IsConnected = true;
					load.NextSend = now + load.Phase;
					_connected++;
					break;
				case PeerEvent.ConnectCompleted:
					_failed++;
					break;
				case PeerEvent.SessionTerminated:
					_terminated++;
					if (load.IsConnected)
						_connected--;
					load.IsConnected = false;
					break;
				default:
					Handle(peerEvent);
					break;
			}
		}

		Check(load);
		if (!load.IsConnected || sendInterval == long.MaxValue)
			return;

		while (now >= load.NextSend)
		{
			load.Client.Send(Payload.Create(_size), _flags);
			_messagesOut++;
			load.NextSend += sendInterval;
		}
	}

	public async Task RunAsync()
	{
		var options = new PeerOptions
		{
			TransportFactory = localEndPoint =>
				new CountingTransport(
					PeerOptions.Default.TransportFactory(localEndPoint),
					_counts
				),
		};
		var sendInterval =
			_messagesPerSecond > 0 ? Stopwatch.Frequency / _messagesPerSecond : long.MaxValue;
		var start = Stopwatch.GetTimestamp();
		var joinInterval = Stopwatch.Frequency / Math.Max(1, _joinsPerSecond);
		var nextJoin = start;
		var nextReport = start + Stopwatch.Frequency;
		var end = start + (long)(_duration.TotalSeconds * Stopwatch.Frequency);
		var random = new Random(_id);
		while (true)
		{
			var now = Stopwatch.GetTimestamp();
			if (now >= end)
				break;

			while (_loads.Count < _count && now >= nextJoin)
			{
				var phase =
					sendInterval == long.MaxValue ? 0 : (long)(random.NextDouble() * sendInterval);
				var load = new Load(new Client(options), phase);
				_loads.Add(load);
				Join(load);
				nextJoin += joinInterval;
			}

			foreach (var load in _loads)
				Drive(load, now, sendInterval);

			if (now >= nextReport)
			{
				nextReport += Stopwatch.Frequency;
				Report();
			}

			Thread.Sleep(2);
		}

		await Task.WhenAll(_loads.Select(l => l.Client.DisposeAsync().AsTask()));
	}
}
