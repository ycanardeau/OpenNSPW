using System.Collections.Immutable;
using System.Net;
using Aigamo.MatchGenerator;
using Aigamo.Otsuki.Actors;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Messages.Enumeration;
using Aigamo.Otsuki.Messages.Reliable;
using Aigamo.Otsuki.Transport;
using Aigamo.Results;

namespace Aigamo.Otsuki.Reliable;

[GenerateMatch]
internal abstract record ConnectionLoopMessage
{
	private ConnectionLoopMessage() { }

	public sealed record DatagramReceived(EndPoint RemoteEndPoint, byte[] Datagram)
		: ConnectionLoopMessage;

	public sealed record RegisterConnection(Connection Connection) : ConnectionLoopMessage;

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
/// Runs the connections whose remote address the endpoint gives it ([MC-DPL8R] section 3.1.4.1): owns their table,
/// routes each datagram to the connection for its source address, accepts new inbound ones, and runs each
/// connection's messages one at a time. Shut down, it accepts nothing new, and reports that it is done once its last
/// connection has terminated, so that a connection saying goodbye (HARD_DISCONNECT) is not cut off.
/// </summary>
internal sealed class ConnectionLoop : Actor<ConnectionLoopItem>
{
	private readonly IDatagramTransport _transport;
	private readonly TimeProvider _timeProvider;
	private readonly ReliableProfile _profile;
	private readonly IConnectionObserver _observer;
	private readonly Action _onDone;
	private readonly Dictionary<EndPoint, Connection> _connections = [];
	private bool _shutDown;
	private bool _isDone;
	private PacketSigning _signing;

	/// <summary>
	/// The timers of the loop's connections.
	/// </summary>
	public LoopTimers Timers { get; }

	public ConnectionLoop(
		IDatagramTransport transport,
		TimeProvider timeProvider,
		ReliableProfile profile,
		IConnectionObserver observer,
		Action onDone,
		MailboxScheduler? scheduler = null
	)
		: base(scheduler: scheduler)
	{
		_transport = transport;
		_timeProvider = timeProvider;
		_profile = profile;
		_observer = observer;
		_onDone = onDone;
		Timers = new LoopTimers(timeProvider, () => Post(new ConnectionLoopMessage.TimersDue()));
		Start();
	}

	public bool Post(Connection connection, ConnectionMessage message) =>
		Post(new ConnectionLoopItem(connection, message, null));

	public bool Post(ConnectionLoopMessage message) =>
		Post(new ConnectionLoopItem(null, null, message));

	// Reports once that the loop is shut down and its last connection has terminated.
	private void ReportWhenDone()
	{
		if (!_shutDown || _isDone || _connections.Count > 0)
			return;

		_isDone = true;
		Timers.Dispose();
		Stop();
		_onDone();
	}

	private void Terminated(Connection connection)
	{
		if (
			_connections.TryGetValue(connection.RemoteEndPoint, out var current)
			&& current == connection
		)
			_connections.Remove(connection.RemoteEndPoint);
		ReportWhenDone();
	}

	/// <summary>
	/// A connection of this loop, which signs as <paramref name="signing"/> says. Thread-safe: only reads immutable
	/// state.
	/// </summary>
	public Connection CreateConnection(
		EndPoint remoteEndPoint,
		bool isOutbound,
		PacketSigning signing
	) =>
		new(
			this,
			remoteEndPoint,
			isOutbound,
			signing,
			_transport,
			_timeProvider,
			_profile,
			_observer,
			Terminated
		);

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
				_observer.OnEnumQuery(message.RemoteEndPoint, query);
			return Unit.Default;
		}

		// Only CONNECT can open a connection; everything else from unknown sources is discarded.
		if (
			ReliableMessageSerializer.Default.Deserialize(message.Datagram)
			is not ReliableMessage.Connect connect
		)
			return Unit.Default;

		connection = CreateConnection(message.RemoteEndPoint, isOutbound: false, _signing);
		_connections.Add(message.RemoteEndPoint, connection);
		connection.Handle(new ConnectionMessage.DatagramArrived(connect));
		return Unit.Default;
	}

	private Unit HandleRegister(Connection connection)
	{
		if (_shutDown || !_connections.TryAdd(connection.RemoteEndPoint, connection))
			connection.Handle(
				new ConnectionMessage.Abort(
					_shutDown ? ResultCode.NoConnection : ResultCode.AlreadyConnected
				)
			);
		else
			connection.Handle(new ConnectionMessage.StartConnecting());
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
			connection.Handle(new ConnectionMessage.EndpointShutDown(ResultCode.NoConnection));
		ReportWhenDone();
		return Unit.Default;
	}

	private Unit SendToMany(ConnectionLoopMessage.SendToMany message)
	{
		for (var i = 0; i < message.Connections.Length; i++)
			message
				.Connections[i]
				.Handle(
					new ConnectionMessage.SendPayload(
						message.SendIds[i],
						message.Payload,
						message.Reliable,
						User1: false
					)
				);
		return Unit.Default;
	}

	protected override void Receive(ConnectionLoopItem item)
	{
		if (item.Connection is { } connection)
		{
			connection.Handle(item.ToConnection!);
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
			RegisterConnection: m => HandleRegister(m.Connection),
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
}
