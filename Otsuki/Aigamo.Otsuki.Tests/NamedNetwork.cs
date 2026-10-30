using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Aigamo.Otsuki.Transport;

namespace Aigamo.Otsuki.Tests;

/// <summary>
/// An endpoint that is not an IP address, such as one of a Bluetooth transport, named by a string.
/// </summary>
internal sealed class NamedEndPoint(string name) : EndPoint
{
	public string Name { get; } = name;

	public override AddressFamily AddressFamily => AddressFamily.Unspecified;

	public override bool Equals(object? obj) => obj is NamedEndPoint other && other.Name == Name;

	public override int GetHashCode() => Name.GetHashCode();

	public override string ToString() => Name;
}

/// <summary>
/// A lossless datagram network between <see cref="NamedEndPoint"/>s, which delivers every datagram at once.
/// </summary>
internal sealed class NamedNetwork
{
	private readonly ConcurrentDictionary<NamedEndPoint, NamedTransport> _transports = new();

	public NamedTransport Bind(NamedEndPoint endPoint)
	{
		var transport = new NamedTransport(this, endPoint);
		if (!_transports.TryAdd(endPoint, transport))
			throw new SocketException((int)SocketError.AddressAlreadyInUse);
		return transport;
	}

	public Func<EndPoint, IDatagramTransport> CreateFactory() =>
		localEndPoint => Bind((NamedEndPoint)localEndPoint);

	internal void Unbind(NamedTransport transport) =>
		_transports.TryRemove(transport.LocalEndPoint, out _);

	internal void Send(NamedEndPoint source, EndPoint destination, byte[] datagram)
	{
		if (destination is NamedEndPoint named && _transports.TryGetValue(named, out var transport))
			transport.Receive(source, datagram.ToArray());
	}
}

internal sealed class NamedTransport(NamedNetwork network, NamedEndPoint localEndPoint)
	: IDatagramTransport
{
	private readonly NamedNetwork _network = network;
	private Action<EndPoint, byte[]>? _onReceive;

	public NamedEndPoint LocalEndPoint { get; } = localEndPoint;

	EndPoint IDatagramTransport.LocalEndPoint => LocalEndPoint;

	public void Start(Action<EndPoint, byte[]> onReceive) => _onReceive = onReceive;

	public void Send(EndPoint remoteEndPoint, byte[] datagram) =>
		_network.Send(LocalEndPoint, remoteEndPoint, datagram);

	internal void Receive(EndPoint remoteEndPoint, byte[] datagram) =>
		_onReceive?.Invoke(remoteEndPoint, datagram);

	public void Dispose()
	{
		_onReceive = null;
		_network.Unbind(this);
	}
}
