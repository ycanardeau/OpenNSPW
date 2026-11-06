using System.Net;
using Aigamo.Otsuki.Actors;
using Aigamo.Otsuki.Messages.Reliable;
using Aigamo.Otsuki.Transport;

namespace Aigamo.Otsuki.Tests;

/// <summary>
/// A network for the simulation: each datagram is delivered as a step of its own on the scheduler, so that the
/// scheduler's order mixes deliveries with the actors' messages, and reorders datagrams as UDP may. It loses a share of
/// them, chosen by its seed, and none to or from an address cut off. Binding and everything else happen at once, on
/// the one thread that drives the simulation.
/// </summary>
internal sealed class SimulatedNetwork(ManualScheduler scheduler, Func<TimeSpan> now, int seed)
{
	private readonly ManualScheduler _scheduler = scheduler;
	private readonly Func<TimeSpan> _now = now;
	private readonly Random _random = new(seed);
	private readonly Dictionary<IPEndPoint, SimulatedTransport> _transports = [];
	private readonly HashSet<IPAddress> _cutOff = [];
	private int _nextPort = 40000;

	/// <summary>
	/// The share of datagrams lost, from 0 to 1.
	/// </summary>
	public double LossRate { get; set; }

	/// <summary>
	/// What the network did, in order: each datagram delivered, lost or cut off, with the time, the addresses and the
	/// bytes. Two runs that take the same order have the same trace.
	/// </summary>
	public List<string> Trace { get; } = [];

	/// <summary>
	/// Every datagram to or from <paramref name="address"/> is lost from now on, as if its machine had stopped.
	/// </summary>
	public void CutOff(IPAddress address) => _cutOff.Add(address);

	/// <summary>
	/// Every transport bound so far, closed ones included.
	/// </summary>
	public List<SimulatedTransport> Transports { get; } = [];

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

	public void Send(IPEndPoint source, EndPoint destination, byte[] datagram)
	{
		var copy = datagram.ToArray();
		var entry = $"{_now().Ticks} {source} > {destination} {Convert.ToHexString(copy)}";
		if (
			_cutOff.Contains(source.Address)
			|| destination is IPEndPoint { Address: var address } && _cutOff.Contains(address)
		)
		{
			Trace.Add($"cut {entry}");
			return;
		}

		if (LossRate > 0 && _random.NextDouble() < LossRate)
		{
			Trace.Add($"lost {entry}");
			return;
		}

		_scheduler.Schedule(() =>
		{
			Trace.Add($"{_now().Ticks} delivered {entry}");
			if (
				destination is IPEndPoint ipEndPoint
				&& _transports.TryGetValue(ipEndPoint, out var transport)
			)
				transport.Deliver(source, copy);
		});
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
		var kind = ReliableMessageSerializer.Default.Deserialize(datagram)?.GetType().Name ?? "Other";
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
