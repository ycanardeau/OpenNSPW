using System.Net;
using System.Net.Sockets;
using Aigamo.Otsuki.Actors;
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

internal abstract record NamedNetworkMessage
{
	private NamedNetworkMessage() { }

	public sealed record Bind(NamedTransport Transport, TaskCompletionSource<bool> Bound)
		: NamedNetworkMessage;

	public sealed record Start(NamedTransport Transport, Action<EndPoint, byte[]> OnReceive)
		: NamedNetworkMessage;

	public sealed record Unbind(NamedTransport Transport) : NamedNetworkMessage;

	public sealed record Send(NamedEndPoint Source, EndPoint Destination, byte[] Datagram)
		: NamedNetworkMessage;
}

/// <summary>
/// A lossless datagram network between <see cref="NamedEndPoint"/>s, which delivers every datagram as it is sent. It
/// is an actor, which owns its transports and what they receive with.
/// </summary>
internal sealed class NamedNetwork : Actor<NamedNetworkMessage>
{
	private readonly Dictionary<
		NamedEndPoint,
		(NamedTransport Transport, Action<EndPoint, byte[]>? OnReceive)
	> _transports = [];

	public NamedNetwork()
	{
		Start();
	}

	public NamedTransport Bind(NamedEndPoint endPoint)
	{
		var transport = new NamedTransport(this, endPoint);
		var bound = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		Post(new NamedNetworkMessage.Bind(transport, bound));
		if (!bound.Task.GetAwaiter().GetResult())
			throw new SocketException((int)SocketError.AddressAlreadyInUse);
		return transport;
	}

	public Func<EndPoint, IDatagramTransport> CreateFactory() =>
		localEndPoint => Bind((NamedEndPoint)localEndPoint);

	protected override void Receive(NamedNetworkMessage message)
	{
		switch (message)
		{
			case NamedNetworkMessage.Bind bind:
				bind.Bound.SetResult(_transports.TryAdd(bind.Transport.LocalEndPoint, (bind.Transport, null)));
				break;
			case NamedNetworkMessage.Start start
				when _transports.TryGetValue(start.Transport.LocalEndPoint, out var entry)
					&& entry.Transport == start.Transport:
				_transports[start.Transport.LocalEndPoint] = (start.Transport, start.OnReceive);
				break;
			case NamedNetworkMessage.Unbind unbind
				when _transports.TryGetValue(unbind.Transport.LocalEndPoint, out var entry)
					&& entry.Transport == unbind.Transport:
				_transports.Remove(unbind.Transport.LocalEndPoint);
				break;
			case NamedNetworkMessage.Send send
				when send.Destination is NamedEndPoint named
					&& _transports.TryGetValue(named, out var entry):
				entry.OnReceive?.Invoke(send.Source, send.Datagram);
				break;
		}
	}
}

internal sealed class NamedTransport(NamedNetwork network, NamedEndPoint localEndPoint)
	: IDatagramTransport
{
	private readonly NamedNetwork _network = network;

	public NamedEndPoint LocalEndPoint { get; } = localEndPoint;

	EndPoint IDatagramTransport.LocalEndPoint => LocalEndPoint;

	public void Start(Action<EndPoint, byte[]> onReceive) =>
		_network.Post(new NamedNetworkMessage.Start(this, onReceive));

	public void Send(EndPoint remoteEndPoint, byte[] datagram) =>
		_network.Post(new NamedNetworkMessage.Send(LocalEndPoint, remoteEndPoint, datagram.ToArray()));

	public void Dispose() => _network.Post(new NamedNetworkMessage.Unbind(this));
}
