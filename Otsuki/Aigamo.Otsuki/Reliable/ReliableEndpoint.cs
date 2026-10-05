using System.Net;
using Aigamo.MatchGenerator;
using Aigamo.Otsuki.Actors;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Messages.Reliable;
using Aigamo.Otsuki.Transport;
using Aigamo.Results;

namespace Aigamo.Otsuki.Reliable;

[GenerateMatch]
internal abstract record ReliableEndpointMessage
{
	private ReliableEndpointMessage() { }

	public sealed record DatagramReceived(IPEndPoint RemoteEndPoint, byte[] Datagram)
		: ReliableEndpointMessage;

	public sealed record RegisterConnection(Connection Connection) : ReliableEndpointMessage;

	public sealed record ConnectionTerminated(Connection Connection) : ReliableEndpointMessage;

	public sealed record ShutDown : ReliableEndpointMessage;
}

/// <summary>
/// Owns a datagram transport and the connection table: routes each datagram to the <see cref="Connection"/>
/// for its source address, and accepts new inbound connections ([MC-DPL8R] section 3.1.4.1).
/// </summary>
internal sealed class ReliableEndpoint : Actor<ReliableEndpointMessage>
{
	private readonly IDatagramTransport _transport;
	private readonly TimeProvider _timeProvider;
	private readonly ReliableProfile _profile;
	private readonly IConnectionObserver _observer;
	private readonly Dictionary<IPEndPoint, Connection> _connections = [];
	private bool _shutDown;

	public IPEndPoint LocalEndPoint => _transport.LocalEndPoint;

	public ReliableEndpoint(
		IDatagramTransport transport,
		TimeProvider timeProvider,
		ReliableProfile profile,
		IConnectionObserver observer
	)
	{
		_transport = transport;
		_timeProvider = timeProvider;
		_profile = profile;
		_observer = observer;
		Start();
		_transport.Start(
			(remoteEndPoint, datagram) =>
				Post(new ReliableEndpointMessage.DatagramReceived(remoteEndPoint, datagram))
		);
	}

	private Connection CreateConnection(IPEndPoint remoteEndPoint, bool isOutbound) =>
		new(
			remoteEndPoint,
			isOutbound,
			_transport,
			_timeProvider,
			_profile,
			_observer,
			connection => Post(new ReliableEndpointMessage.ConnectionTerminated(connection))
		);

	/// <summary>
	/// Creates an outbound connection. It starts connecting once the endpoint has registered it.
	/// Thread-safe: only reads immutable state.
	/// </summary>
	public Connection Connect(IPEndPoint remoteEndPoint)
	{
		var connection = CreateConnection(remoteEndPoint, isOutbound: true);
		if (!Post(new ReliableEndpointMessage.RegisterConnection(connection)))
			connection.Post(new ConnectionMessage.Abort(ResultCode.NoConnection));
		return connection;
	}

	private Unit HandleDatagram(ReliableEndpointMessage.DatagramReceived message)
	{
		if (
			_shutDown
			|| ReliableMessageSerializer.Default.Deserialize(message.Datagram)
				is not { } reliableMessage
		)
			return Unit.Default;

		if (!_connections.TryGetValue(message.RemoteEndPoint, out var connection))
		{
			// Only CONNECT can open a connection; everything else from unknown sources is discarded.
			if (reliableMessage is not ReliableMessage.Connect)
				return Unit.Default;

			connection = CreateConnection(message.RemoteEndPoint, isOutbound: false);
			_connections.Add(message.RemoteEndPoint, connection);
		}

		connection.Post(new ConnectionMessage.DatagramArrived(reliableMessage));
		return Unit.Default;
	}

	private Unit HandleRegister(Connection connection)
	{
		if (_shutDown || !_connections.TryAdd(connection.RemoteEndPoint, connection))
			connection.Post(
				new ConnectionMessage.Abort(
					_shutDown ? ResultCode.NoConnection : ResultCode.AlreadyConnected
				)
			);
		else
			connection.Post(new ConnectionMessage.StartConnecting());
		return Unit.Default;
	}

	private Unit HandleTerminated(Connection connection)
	{
		if (
			_connections.TryGetValue(connection.RemoteEndPoint, out var current)
			&& current == connection
		)
			_connections.Remove(connection.RemoteEndPoint);
		return Unit.Default;
	}

	private Unit HandleShutDown()
	{
		if (_shutDown)
			return Unit.Default;

		_shutDown = true;
		foreach (var connection in _connections.Values)
			connection.Post(new ConnectionMessage.Abort(ResultCode.NoConnection));
		_connections.Clear();
		_transport.Dispose();
		Stop();
		return Unit.Default;
	}

	protected override void Receive(ReliableEndpointMessage message) =>
		message.Match(
			DatagramReceived: HandleDatagram,
			RegisterConnection: m => HandleRegister(m.Connection),
			ConnectionTerminated: m => HandleTerminated(m.Connection),
			ShutDown: _ => HandleShutDown()
		);
}
