using System.Net;
using System.Net.Sockets;
using Aigamo.Otsuki.Actors;
using Aigamo.Otsuki.Transport;

namespace Aigamo.Otsuki.Tests;

internal abstract record NetworkMessage
{
	private NetworkMessage() { }

	public sealed record Bind(InMemoryTransport Transport) : NetworkMessage;

	public sealed record Start(InMemoryTransport Transport, Action<EndPoint, byte[]> OnReceive)
		: NetworkMessage;

	public sealed record Unbind(InMemoryTransport Transport) : NetworkMessage;

	public sealed record Send(IPEndPoint Source, IPEndPoint Destination, byte[] Datagram)
		: NetworkMessage;

	public sealed record Deliver(IPEndPoint Source, IPEndPoint Destination, byte[] Datagram)
		: NetworkMessage;

	public sealed record Configure(Action Change) : NetworkMessage;

	public sealed record Ask(Action Question) : NetworkMessage;
}

/// <summary>
/// A simulated datagram network that can lose, delay and reorder datagrams, and partition hosts. It is an actor: its
/// settings, and the callbacks it runs (Drop, Observe), are its own, set by posting, and a test reads what they count
/// with <see cref="AskAsync"/>.
/// </summary>
internal sealed class InMemoryNetwork : Actor<NetworkMessage>
{
	private readonly Dictionary<
		IPEndPoint,
		(InMemoryTransport Transport, Action<EndPoint, byte[]>? OnReceive)
	> _transports = [];
	private readonly HashSet<IPAddress> _partitioned = [];
	private readonly Random _random = new(12345);
	private readonly Sequence _ports = new();
	private double _lossRate;
	private TimeSpan _maxDelay;
	private Func<IPEndPoint, IPEndPoint, byte[], bool> _drop = (_, _, _) => false;
	private Action<IPEndPoint, IPEndPoint, byte[]>? _observe;
	private long _delivered;

	public InMemoryNetwork()
	{
		Start();
	}

	private void Configure(Action change)
	{
		Post(new NetworkMessage.Configure(change));
	}

	/// <summary>
	/// Probability that a datagram is lost.
	/// </summary>
	public double LossRate
	{
		set => Configure(() => _lossRate = value);
	}

	/// <summary>
	/// Datagrams are delayed by a random time up to this value, which reorders them.
	/// </summary>
	public TimeSpan MaxDelay
	{
		set => Configure(() => _maxDelay = value);
	}

	/// <summary>
	/// Drops a datagram when it returns <see langword="true"/>. Arguments are source, destination and datagram. Runs on
	/// the network.
	/// </summary>
	public Func<IPEndPoint, IPEndPoint, byte[], bool> Drop
	{
		set => Configure(() => _drop = value);
	}

	/// <summary>
	/// Observes every datagram that is delivered. Runs on the network.
	/// </summary>
	public Action<IPEndPoint, IPEndPoint, byte[]>? Observe
	{
		set => Configure(() => _observe = value);
	}

	/// <summary>
	/// Answers a question on the network, such as what its callbacks counted, once what was posted before it is handled.
	/// </summary>
	public Task<T> AskAsync<T>(Func<T> question)
	{
		var answer = new TaskCompletionSource<T>(
			TaskCreationOptions.RunContinuationsAsynchronously
		);
		Post(new NetworkMessage.Ask(() => answer.SetResult(question())));
		return answer.Task;
	}

	public Task<long> DeliveredCountAsync() => AskAsync(() => _delivered);

	public InMemoryTransport Bind(IPAddress address, int port = 0)
	{
		var endPoint = new IPEndPoint(address, port == 0 ? 40000 + (int)_ports.Next() : port);
		var transport = new InMemoryTransport(this, endPoint);
		// Bound at once: an address already bound is reported by the network (OnError), and this one receives nothing.
		Post(new NetworkMessage.Bind(transport));
		return transport;
	}

	public Func<EndPoint, IDatagramTransport> CreateFactory(IPAddress address) =>
		localEndPoint => Bind(address, ((IPEndPoint)localEndPoint).Port);

	public void Partition(IPAddress address) => Configure(() => _partitioned.Add(address));

	public void Heal(IPAddress address) => Configure(() => _partitioned.Remove(address));

	private void DeliverNow(IPEndPoint source, IPEndPoint destination, byte[] datagram)
	{
		if (_transports.TryGetValue(destination, out var transport))
		{
			_delivered++;
			_observe?.Invoke(source, destination, datagram);
			transport.OnReceive?.Invoke(source, datagram);
		}
	}

	// A transport that is disposed sends nothing, as a closed socket does: what it sends after is lost.
	private void Send(IPEndPoint source, IPEndPoint destination, byte[] datagram)
	{
		if (!_transports.ContainsKey(source))
			return;

		if (_partitioned.Contains(source.Address) || _partitioned.Contains(destination.Address))
			return;

		if (_drop(source, destination, datagram) || _random.NextDouble() < _lossRate)
			return;

		if (_maxDelay <= TimeSpan.Zero)
		{
			DeliverNow(source, destination, datagram);
			return;
		}

		var delay = _maxDelay * _random.NextDouble();
		_ = Task.Run(async () =>
		{
			await Task.Delay(delay);
			Post(new NetworkMessage.Deliver(source, destination, datagram));
		});
	}

	protected override void Receive(NetworkMessage message)
	{
		switch (message)
		{
			case NetworkMessage.Bind bind:
				if (!_transports.TryAdd(bind.Transport.LocalEndPoint, (bind.Transport, null)))
					throw new SocketException((int)SocketError.AddressAlreadyInUse);
				break;
			case NetworkMessage.Start start
				when _transports.TryGetValue(start.Transport.LocalEndPoint, out var bound)
					&& bound.Transport == start.Transport:
				_transports[start.Transport.LocalEndPoint] = (start.Transport, start.OnReceive);
				break;
			case NetworkMessage.Unbind unbind
				when _transports.TryGetValue(unbind.Transport.LocalEndPoint, out var bound)
					&& bound.Transport == unbind.Transport:
				_transports.Remove(unbind.Transport.LocalEndPoint);
				break;
			case NetworkMessage.Send send:
				Send(send.Source, send.Destination, send.Datagram);
				break;
			case NetworkMessage.Deliver deliver:
				DeliverNow(deliver.Source, deliver.Destination, deliver.Datagram);
				break;
			case NetworkMessage.Configure configure:
				configure.Change();
				break;
			case NetworkMessage.Ask ask:
				ask.Question();
				break;
		}
	}
}

internal sealed class InMemoryTransport(InMemoryNetwork network, IPEndPoint localEndPoint)
	: IDatagramTransport
{
	private readonly InMemoryNetwork _network = network;

	public IPEndPoint LocalEndPoint { get; } = localEndPoint;

	EndPoint IDatagramTransport.LocalEndPoint => LocalEndPoint;

	public void Start(Action<EndPoint, byte[]> onReceive) =>
		_network.Post(new NetworkMessage.Start(this, onReceive));

	// The network carries only IPv4 endpoints, so a datagram to any other kind is lost.
	public void Send(EndPoint remoteEndPoint, byte[] datagram)
	{
		if (remoteEndPoint is IPEndPoint ipEndPoint)
			_network.Post(new NetworkMessage.Send(LocalEndPoint, ipEndPoint, [.. datagram]));
	}

	public void Dispose() => _network.Post(new NetworkMessage.Unbind(this));
}
