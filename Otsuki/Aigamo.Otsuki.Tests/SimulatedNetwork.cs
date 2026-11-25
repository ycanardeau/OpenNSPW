using System.Net;
using Aigamo.Otsuki.Actors;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Messages.Reliable;
using Aigamo.Otsuki.Transport;

namespace Aigamo.Otsuki.Tests;

/// <summary>
/// A network for the simulation: each datagram is delivered as a step of its own on the scheduler, so that the
/// scheduler's order mixes deliveries with the actors' messages, and reorders datagrams as UDP may. With a latency, a
/// datagram waits on the clock before that step, and with jitter, for a time of its own. It loses a share of them and
/// delivers another share twice, chosen by its seed, and none to or from an address cut off, or between two addresses
/// partitioned. Binding and everything else happen at once, on the one thread that drives the simulation.
/// </summary>
internal sealed class SimulatedNetwork(
	ManualScheduler scheduler,
	ManualTimeProvider clock,
	int seed,
	bool keepsTrace = true
)
{
	private readonly ManualScheduler _scheduler = scheduler;
	private readonly ManualTimeProvider _clock = clock;
	private readonly Random _random = new(seed);
	private readonly bool _keepsTrace = keepsTrace;
	private readonly Dictionary<IPEndPoint, SimulatedTransport> _transports = [];
	private readonly HashSet<IPAddress> _cutOff = [];
	private readonly HashSet<(IPAddress, IPAddress)> _partitions = [];
	private int _nextPort = 40000;

	/// <summary>
	/// The share of datagrams lost, from 0 to 1.
	/// </summary>
	public double LossRate { get; set; }

	/// <summary>
	/// The share of datagrams delivered twice, each copy a step of its own, from 0 to 1.
	/// </summary>
	public double DuplicateRate { get; set; }

	/// <summary>
	/// The least time a datagram takes.
	/// </summary>
	public TimeSpan Latency { get; set; }

	/// <summary>
	/// Up to how much longer than <see cref="Latency"/> each datagram takes.
	/// </summary>
	public TimeSpan Jitter { get; set; }

	/// <summary>
	/// What the network did, in order: each datagram delivered, lost or cut off, with the time, the addresses and the
	/// bytes. Two runs that take the same order have the same trace. Empty if the network keeps none.
	/// </summary>
	public List<string> Trace { get; } = [];

	/// <summary>
	/// Every transport bound so far, closed ones included.
	/// </summary>
	public List<SimulatedTransport> Transports { get; } = [];

	/// <summary>
	/// Every datagram to or from <paramref name="address"/> is lost from now on, as if its machine had stopped.
	/// </summary>
	public void CutOff(IPAddress address) => _cutOff.Add(address);

	/// <summary>
	/// Every datagram between <paramref name="first"/> and <paramref name="second"/> is lost until they are healed.
	/// </summary>
	public void Partition(IPAddress first, IPAddress second)
	{
		_partitions.Add((first, second));
		_partitions.Add((second, first));
	}

	public void Heal(IPAddress first, IPAddress second)
	{
		_partitions.Remove((first, second));
		_partitions.Remove((second, first));
	}

	public void HealAll() => _partitions.Clear();

	/// <summary>
	/// The addresses that have a transport open, in the order they were first bound.
	/// </summary>
	public List<IPAddress> Addresses() =>
		[.. Transports.Where(t => !t.IsDisposed).Select(t => t.LocalEndPoint.Address).Distinct()];

	public SimulatedTransport Bind(IPAddress address, int port)
	{
		var endPoint = new IPEndPoint(address, port == 0 ? _nextPort++ : port);
		var transport = new SimulatedTransport(this, endPoint);
		_transports.Add(endPoint, transport);
		Transports.Add(transport);
		return transport;
	}

	public Func<EndPoint, IDatagramTransport> CreateFactory(IPAddress address) =>
		localEndPoint => Bind(address, ((IPEndPoint)localEndPoint).Port);

	// What a datagram is, for whoever reads the trace: its reliable frame, and the core messages a data frame carries,
	// or the size of each payload that is not one.
	private static string Describe(byte[] datagram)
	{
		var message = ReliableMessageSerializer.Default.Deserialize(datagram);
		if (message is not ReliableMessage.DataFrame frame)
			return message?.GetType().Name ?? "?";

		List<byte[]> payloads = [[.. frame.Payload]];
		if (frame.Coalesce)
		{
			try
			{
				using var reader = new BinaryReader(new MemoryStream(payloads[0]));
				payloads =
				[
					.. CoalescedPayloadSerializer.Default.Read(reader)?.Select(p => p.Payload.ToArray())
						?? [],
				];
			}
			catch (Exception)
			{
				return $"DataFrame {frame.SequenceId}/{frame.NextReceive} coalesced";
			}
		}

		var carried = payloads.Select(p =>
			p.Length == 0 ? "keep-alive"
			: CoreMessageSerializer.Default.Deserialize(p) is { } core ? core.GetType().Name
			: $"{p.Length} bytes"
		);
		return $"DataFrame {frame.SequenceId}/{frame.NextReceive} {string.Join(" ", carried)}";
	}

	private void Record(string entry)
	{
		if (_keepsTrace)
			Trace.Add(entry);
	}

	private bool IsCut(IPAddress source, EndPoint destination) =>
		_cutOff.Contains(source)
		|| destination is IPEndPoint { Address: var address }
			&& (_cutOff.Contains(address) || _partitions.Contains((source, address)));

	// A step that delivers the datagram, at once or after the time it takes.
	private void Arrive(Action deliver)
	{
		if (Latency == TimeSpan.Zero && Jitter == TimeSpan.Zero)
		{
			_scheduler.Schedule(deliver);
			return;
		}

		var delay = Latency + TimeSpan.FromTicks(_random.NextInt64(Jitter.Ticks + 1));
		_clock.After(delay, () => _scheduler.Schedule(deliver));
	}

	public void Send(IPEndPoint source, EndPoint destination, byte[] datagram)
	{
		var copy = datagram.ToArray();
		var entry = _keepsTrace
			? $"{_clock.Now.Ticks} {source} > {destination} {Describe(copy)} {Convert.ToHexString(copy)}"
			: string.Empty;
		if (IsCut(source.Address, destination))
		{
			Record($"cut {entry}");
			return;
		}

		if (LossRate > 0 && _random.NextDouble() < LossRate)
		{
			Record($"lost {entry}");
			return;
		}

		void Deliver()
		{
			Record($"{_clock.Now.Ticks} delivered {entry}");
			if (
				destination is IPEndPoint ipEndPoint
				&& _transports.TryGetValue(ipEndPoint, out var transport)
			)
				transport.Deliver(source, copy);
		}

		Arrive(Deliver);
		if (DuplicateRate > 0 && _random.NextDouble() < DuplicateRate)
		{
			Record($"duplicated {entry}");
			Arrive(Deliver);
		}
	}

	public void Unbind(SimulatedTransport transport) => _transports.Remove(transport.LocalEndPoint);
}

/// <summary>
/// A transport of the <see cref="SimulatedNetwork"/>, which records what it sends and when it is disposed.
/// </summary>
internal sealed class SimulatedTransport(SimulatedNetwork network, IPEndPoint localEndPoint)
	: IDatagramTransport
{
	private readonly SimulatedNetwork _network = network;
	private Action<EndPoint, byte[]>? _onReceive;

	public IPEndPoint LocalEndPoint { get; } = localEndPoint;

	EndPoint IDatagramTransport.LocalEndPoint => LocalEndPoint;

	/// <summary>
	/// What happened to the transport, in order: each datagram sent, by the kind of reliable message it is, then
	/// "Disposed", and "Lost" before the kind of each datagram sent after.
	/// </summary>
	public List<string> Log { get; } = [];

	public bool IsDisposed { get; private set; }

	public void Start(Action<EndPoint, byte[]> onReceive) => _onReceive = onReceive;

	public void Deliver(IPEndPoint source, byte[] datagram)
	{
		if (!IsDisposed)
			_onReceive?.Invoke(source, datagram);
	}

	public void Send(EndPoint remoteEndPoint, byte[] datagram)
	{
		var kind =
			ReliableMessageSerializer.Default.Deserialize(datagram)?.GetType().Name ?? "Other";
		if (IsDisposed)
		{
			// A send after the transport is closed is lost, as a goodbye was before the endpoint waited for it.
			Log.Add($"Lost {kind}");
			return;
		}

		Log.Add(kind);
		_network.Send(LocalEndPoint, remoteEndPoint, datagram);
	}

	public void Dispose()
	{
		IsDisposed = true;
		Log.Add("Disposed");
		_network.Unbind(this);
	}
}
