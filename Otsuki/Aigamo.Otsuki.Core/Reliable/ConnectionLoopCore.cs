using System.Collections.Immutable;
using System.Net;
using Aigamo.MatchGenerator;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Messages.Enumeration;
using Aigamo.Otsuki.Messages.Reliable;
using Aigamo.Otsuki.Session;
using Aigamo.Results;

namespace Aigamo.Otsuki.Reliable;

[GenerateMatch]
internal abstract record ConnectionLoopMessage
{
	private ConnectionLoopMessage() { }

	public sealed record DatagramReceived(EndPoint RemoteEndPoint, byte[] Datagram)
		: ConnectionLoopMessage;

	/// <summary>
	/// Opens an outbound connection, which signs as <paramref name="Signing"/> says: the session's mode, which the
	/// session gives, as the loop's own is for inbound connections.
	/// </summary>
	public sealed record RegisterConnection(Connection Connection, PacketSigning Signing)
		: ConnectionLoopMessage;

	/// <summary>
	/// Sets the signing mode that new inbound connections offer, from the session's flags.
	/// </summary>
	public sealed record SetSigning(PacketSigning Signing) : ConnectionLoopMessage;

	public sealed record ShutDown : ConnectionLoopMessage;

	/// <summary>
	/// The nearest deadline of the loop's timers has come.
	/// </summary>
	public sealed record TimersDue : ConnectionLoopMessage;

	/// <summary>
	/// Sends one message to several connections of the loop, as a broadcast does: one post per loop, not one per
	/// connection. <paramref name="SendIds"/> are the connections' send ids, each 0 for no completion report.
	/// </summary>
	public sealed record SendToMany(
		ImmutableArray<Connection> Connections,
		ImmutableArray<long> SendIds,
		byte[] Payload,
		bool Reliable
	) : ConnectionLoopMessage;
}

/// <summary>
/// What a loop's mailbox holds: a message to one of its connections, or one to the loop itself.
/// </summary>
internal readonly record struct ConnectionLoopItem(
	Connection? Connection,
	ConnectionMessage? ToConnection,
	ConnectionLoopMessage? ToLoop
);

/// <summary>
/// What a <see cref="ConnectionLoopCore"/> does outside itself, in the order it decides to, on the thread that called
/// it. The caller carries each out before the call returns, and must not call the core back from one.
/// </summary>
internal interface IConnectionLoopOutputs
{
	/// <summary>
	/// Sends a datagram on the transport.
	/// </summary>
	void Send(EndPoint remoteEndPoint, byte[] datagram);

	/// <summary>
	/// Tells the session what a connection did.
	/// </summary>
	void Report(SessionMessage message);

	/// <summary>
	/// Gives a connection a message after those already waiting for the loop, as a flush is.
	/// </summary>
	void Defer(Connection connection, ConnectionMessage message);

	/// <summary>
	/// The loop is shut down and its last connection has terminated: it takes no more inputs.
	/// </summary>
	void Done();
}

/// <summary>
/// Runs the connections whose remote address its loop is given ([MC-DPL8R] section 3.1.4.1): owns their table, routes
/// each datagram to the connection for its source address, accepts new inbound ones, and runs each connection's
/// messages one at a time. Shut down, it accepts nothing new, and is done once its last connection has terminated, so
/// that a connection saying goodbye (HARD_DISCONNECT) is not cut off.
/// <para>
/// It does nothing by itself: each input comes with the time (<see cref="Handle"/>), and what it does outside goes to
/// <see cref="IConnectionLoopOutputs"/>, in order.
/// </para>
/// </summary>
/// <param name="loop">The loop that runs the core, which its inbound connections name (<see cref="Connection.Loop"/>).</param>
internal sealed class ConnectionLoopCore(
	object loop,
	ReliableProfile profile,
	IConnectionLoopOutputs outputs
) : IConnectionObserver
{
	private readonly object _loop = loop;
	private readonly IConnectionLoopOutputs _outputs = outputs;
	private readonly Dictionary<EndPoint, ReliableConnection> _connections = [];
	private readonly Dictionary<Connection, ReliableConnection> _byHandle = [];
	private bool _shutDown;
	private bool _isDone;
	private PacketSigning _signing;

	public ReliableProfile Profile { get; } = profile;

	/// <summary>
	/// The timers of the loop's connections.
	/// </summary>
	public LoopTimers Timers { get; } = new();

	public int ConnectionCount => _connections.Count;

	/// <summary>
	/// The connection of a handle, while it runs: for tests.
	/// </summary>
	public ReliableConnection? Find(Connection connection) =>
		_byHandle.GetValueOrDefault(connection);

	void IConnectionObserver.OnConnected(Connection connection) =>
		_outputs.Report(new SessionMessage.ConnectionConnected(connection));

	void IConnectionObserver.OnConnectFailed(Connection connection, ResultCode result) =>
		_outputs.Report(new SessionMessage.ConnectionConnectFailed(connection, result));

	void IConnectionObserver.OnReceived(Connection connection, byte[] payload, bool user1) =>
		_outputs.Report(new SessionMessage.ConnectionReceived(connection, payload, user1));

	void IConnectionObserver.OnSendCompleted(
		Connection connection,
		long sendId,
		ResultCode result
	) => _outputs.Report(new SessionMessage.ConnectionSendCompleted(connection, sendId, result));

	void IConnectionObserver.OnDisconnected(Connection connection, ResultCode result) =>
		_outputs.Report(new SessionMessage.ConnectionDisconnected(connection, result));

	void IConnectionObserver.OnEnumQuery(EndPoint remoteEndPoint, EnumerationMessage.Query query) =>
		_outputs.Report(new SessionMessage.EnumQueryReceived(remoteEndPoint, query));

	public void Send(EndPoint remoteEndPoint, byte[] datagram) =>
		_outputs.Send(remoteEndPoint, datagram);

	public void Defer(Connection connection, ConnectionMessage message) =>
		_outputs.Defer(connection, message);

	// Done once the loop is shut down and its last connection has terminated.
	private void ReportWhenDone()
	{
		if (!_shutDown || _isDone || _connections.Count > 0)
			return;

		_isDone = true;
		_outputs.Done();
	}

	public void Terminated(ReliableConnection connection)
	{
		if (
			_connections.TryGetValue(connection.RemoteEndPoint, out var current)
			&& current == connection
		)
		{
			_connections.Remove(connection.RemoteEndPoint);
			_byHandle.Remove(connection.Handle);
		}
		ReportWhenDone();
	}

	private ReliableConnection Create(Connection handle, PacketSigning signing) =>
		new(handle, signing, this);

	private void Add(ReliableConnection connection)
	{
		_connections.Add(connection.RemoteEndPoint, connection);
		_byHandle.Add(connection.Handle, connection);
	}

	private Unit HandleDatagram(ConnectionLoopMessage.DatagramReceived message)
	{
		if (_connections.TryGetValue(message.RemoteEndPoint, out var connection))
		{
			connection.ReceiveDatagram(message.Datagram);
			return Unit.Default;
		}

		if (_shutDown)
			return Unit.Default;

		// Host enumeration travels outside connections, with a first byte of 0 ([MC-DPLHP] section 2.2).
		if (EnumerationMessageSerializer.IsEnumerationMessage(message.Datagram))
		{
			if (
				EnumerationMessageSerializer.Default.Deserialize(message.Datagram)
				is EnumerationMessage.Query query
			)
				((IConnectionObserver)this).OnEnumQuery(message.RemoteEndPoint, query);
			return Unit.Default;
		}

		// Only CONNECT can open a connection; everything else from unknown sources is discarded.
		if (
			ReliableMessageSerializer.Default.Deserialize(message.Datagram)
			is not ReliableMessage.Connect connect
		)
			return Unit.Default;

		connection = Create(
			new Connection(message.RemoteEndPoint, isOutbound: false, _loop),
			_signing
		);
		Add(connection);
		connection.Run(new ConnectionMessage.DatagramArrived(connect));
		return Unit.Default;
	}

	private Unit HandleRegister(ConnectionLoopMessage.RegisterConnection message)
	{
		var connection = Create(message.Connection, message.Signing);
		if (_shutDown || _connections.ContainsKey(connection.RemoteEndPoint))
		{
			connection.Run(
				new ConnectionMessage.Abort(
					_shutDown ? ResultCode.NoConnection : ResultCode.AlreadyConnected
				)
			);
			return Unit.Default;
		}

		Add(connection);
		connection.Run(new ConnectionMessage.StartConnecting());
		return Unit.Default;
	}

	// The connections end: at once, but one that says goodbye, which the session told to before it shut the endpoint
	// down, and so takes that first.
	private Unit HandleShutDown()
	{
		if (_shutDown)
			return Unit.Default;

		_shutDown = true;
		foreach (var connection in _connections.Values.ToList())
			connection.Run(new ConnectionMessage.EndpointShutDown(ResultCode.NoConnection));
		ReportWhenDone();
		return Unit.Default;
	}

	private Unit SendToMany(ConnectionLoopMessage.SendToMany message)
	{
		for (var i = 0; i < message.Connections.Length; i++)
			Find(message.Connections[i])
				?.Run(
					new ConnectionMessage.SendPayload(
						message.SendIds[i],
						message.Payload,
						message.Reliable,
						User1: false
					)
				);
		return Unit.Default;
	}

	private void Receive(ConnectionLoopItem item)
	{
		if (item.Connection is { } connection)
		{
			Find(connection)?.Run(item.ToConnection!);
			return;
		}

		// Datagrams, broadcasts and timers take a switch, which allocates nothing; the rest take Match, which keeps every
		// kind handled but makes a delegate for each kind at each call.
		switch (item.ToLoop)
		{
			case ConnectionLoopMessage.DatagramReceived m:
				HandleDatagram(m);
				return;
			case ConnectionLoopMessage.SendToMany m:
				SendToMany(m);
				return;
			case ConnectionLoopMessage.TimersDue:
				Timers.FireDue();
				return;
		}

		item.ToLoop!.Match(
			DatagramReceived: HandleDatagram,
			RegisterConnection: HandleRegister,
			SetSigning: m =>
			{
				_signing = m.Signing;
				return Unit.Default;
			},
			ShutDown: _ => HandleShutDown(),
			TimersDue: _ =>
			{
				Timers.FireDue();
				return Unit.Default;
			},
			SendToMany: SendToMany
		);
	}

	/// <summary>
	/// Handles one input at <paramref name="now"/>, on the caller's clock, and everything it causes. Once
	/// <see cref="IConnectionLoopOutputs.Done"/>, inputs are ignored.
	/// </summary>
	public void Handle(ConnectionLoopItem item, TimeSpan now)
	{
		if (_isDone)
			return;

		Timers.Advance(now);
		Receive(item);
	}
}
