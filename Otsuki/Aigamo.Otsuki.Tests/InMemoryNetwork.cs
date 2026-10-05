using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Aigamo.Otsuki.Transport;

namespace Aigamo.Otsuki.Tests;

/// <summary>
/// A simulated datagram network that can lose, delay and reorder datagrams, and partition hosts.
/// </summary>
internal sealed class InMemoryNetwork
{
	private readonly ConcurrentDictionary<IPEndPoint, InMemoryTransport> _transports = new();
	private readonly ConcurrentDictionary<IPAddress, bool> _partitioned = new();
	private readonly Random _random = new(12345);
	private readonly Lock _randomLock = new();
	private int _nextPort = 40000;
	private long _delivered;

	/// <summary>
	/// Probability that a datagram is lost.
	/// </summary>
	public double LossRate { get; set; }

	/// <summary>
	/// Datagrams are delayed by a random time up to this value, which reorders them.
	/// </summary>
	public TimeSpan MaxDelay { get; set; }

	/// <summary>
	/// Drops a datagram when it returns <see langword="true"/>. Arguments are source, destination and datagram.
	/// </summary>
	public Func<IPEndPoint, IPEndPoint, byte[], bool> Drop { get; set; } = (_, _, _) => false;

	/// <summary>
	/// Observes every datagram that is delivered.
	/// </summary>
	public Action<IPEndPoint, IPEndPoint, byte[]>? Observe { get; set; }

	public long DeliveredCount => Interlocked.Read(ref _delivered);

	public InMemoryTransport Bind(IPAddress address, int port = 0)
	{
		var endPoint = new IPEndPoint(
			address,
			port == 0 ? Interlocked.Increment(ref _nextPort) : port
		);
		var transport = new InMemoryTransport(this, endPoint);
		if (!_transports.TryAdd(endPoint, transport))
			throw new SocketException((int)SocketError.AddressAlreadyInUse);
		return transport;
	}

	public Func<IPEndPoint, IDatagramTransport> CreateFactory(IPAddress address) =>
		localEndPoint => Bind(address, localEndPoint.Port);

	public void Partition(IPAddress address) => _partitioned[address] = true;

	public void Heal(IPAddress address) => _partitioned.TryRemove(address, out _);

	internal void Unbind(InMemoryTransport transport) =>
		_transports.TryRemove(transport.LocalEndPoint, out _);

	private double NextRandom()
	{
		lock (_randomLock)
			return _random.NextDouble();
	}

	private void DeliverNow(IPEndPoint source, IPEndPoint destination, byte[] datagram)
	{
		if (_transports.TryGetValue(destination, out var transport))
		{
			Interlocked.Increment(ref _delivered);
			Observe?.Invoke(source, destination, datagram);
			transport.Receive(source, datagram);
		}
	}

	internal void Send(IPEndPoint source, IPEndPoint destination, byte[] datagram)
	{
		if (
			_partitioned.ContainsKey(source.Address)
			|| _partitioned.ContainsKey(destination.Address)
		)
			return;

		if (Drop(source, destination, datagram) || NextRandom() < LossRate)
			return;

		var copy = datagram.ToArray();
		if (MaxDelay <= TimeSpan.Zero)
		{
			DeliverNow(source, destination, copy);
			return;
		}

		var delay = MaxDelay * NextRandom();
		_ = Task.Run(async () =>
		{
			await Task.Delay(delay);
			DeliverNow(source, destination, copy);
		});
	}
}

internal sealed class InMemoryTransport(InMemoryNetwork network, IPEndPoint localEndPoint)
	: IDatagramTransport
{
	private Action<IPEndPoint, byte[]>? _onReceive;

	public IPEndPoint LocalEndPoint { get; } = localEndPoint;

	public void Start(Action<IPEndPoint, byte[]> onReceive) => _onReceive = onReceive;

	public void Send(IPEndPoint remoteEndPoint, byte[] datagram) =>
		network.Send(LocalEndPoint, remoteEndPoint, datagram);

	internal void Receive(IPEndPoint remoteEndPoint, byte[] datagram) =>
		_onReceive?.Invoke(remoteEndPoint, datagram);

	public void Dispose()
	{
		_onReceive = null;
		network.Unbind(this);
	}
}
