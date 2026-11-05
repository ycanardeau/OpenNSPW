using System.Collections.Immutable;
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

	/// <summary>
	/// Sends a datagram outside any connection, such as a host enumeration response.
	/// </summary>
	public sealed record SendDatagram(EndPoint RemoteEndPoint, byte[] Datagram)
		: ReliableEndpointMessage;

	public sealed record ShutDown : ReliableEndpointMessage;

	/// <summary>
	/// A loop is shut down and its last connection has terminated.
	/// </summary>
	public sealed record LoopDone : ReliableEndpointMessage;
}

/// <summary>
/// Owns a datagram transport and the loops that run the connections ([MC-DPL8R] section 3.1.4.1): gives each datagram
/// to the loop of its source address, which a hash of the address chooses, so that routing reads no shared state.
/// Shut down, it keeps its transport until every loop's last connection has terminated, so that a connection saying
/// goodbye (HARD_DISCONNECT) is not cut off by the shutdown that follows it.
/// </summary>
internal sealed class ReliableEndpoint : Actor<ReliableEndpointMessage>
{
	private readonly IDatagramTransport _transport;
	private readonly ImmutableArray<ConnectionLoop> _loops;
	private int _loopsDone;
	private bool _shutDown;

	public EndPoint LocalEndPoint => _transport.LocalEndPoint;

	public ReliableEndpoint(
		IDatagramTransport transport,
		TimeProvider timeProvider,
		ReliableProfile profile,
		IConnectionObserver observer,
		int loops = 1
	)
	{
		_transport = transport;
		_loops =
		[
			.. Enumerable
				.Range(0, Math.Max(1, loops))
				.Select(_ => new ConnectionLoop(
					transport,
					timeProvider,
					profile,
					observer,
					() => Post(new ReliableEndpointMessage.LoopDone())
				)),
		];
		Start();
		_transport.Start(
			(remoteEndPoint, datagram) =>
				LoopFor(remoteEndPoint)
					.Post(new ConnectionLoopMessage.DatagramReceived(remoteEndPoint, datagram))
		);
	}

	private ConnectionLoop LoopFor(EndPoint remoteEndPoint) =>
		_loops[(remoteEndPoint.GetHashCode() & int.MaxValue) % _loops.Length];

	/// <summary>
	/// Creates an outbound connection, which signs as <paramref name="signing"/> says, the session's mode, which the
	/// caller gives: the loops' own are their threads'. It starts connecting once its loop has registered it.
	/// Thread-safe: only reads immutable state.
	/// </summary>
	public Connection Connect(EndPoint remoteEndPoint, PacketSigning signing)
	{
		var loop = LoopFor(remoteEndPoint);
		var connection = loop.CreateConnection(remoteEndPoint, isOutbound: true, signing);
		if (!loop.Post(new ConnectionLoopMessage.RegisterConnection(connection)))
			connection.Observer.OnConnectFailed(connection, ResultCode.NoConnection);
		return connection;
	}

	/// <summary>
	/// Makes new inbound connections offer <paramref name="signing"/>. Posts to every loop, from any thread.
	/// </summary>
	public void SetSigning(PacketSigning signing)
	{
		foreach (var loop in _loops)
			loop.Post(new ConnectionLoopMessage.SetSigning(signing));
	}

	/// <summary>
	/// Shuts the connections down, then the transport. Posts to every loop, from any thread, after the messages the
	/// caller posted to their connections before.
	/// </summary>
	public void ShutDown()
	{
		Post(new ReliableEndpointMessage.ShutDown());
		foreach (var loop in _loops)
			loop.Post(new ConnectionLoopMessage.ShutDown());
	}

	private Unit HandleLoopDone()
	{
		_loopsDone++;
		if (_loopsDone < _loops.Length)
			return Unit.Default;

		_transport.Dispose();
		Stop();
		return Unit.Default;
	}

	protected override void Receive(ReliableEndpointMessage message) =>
		message.Match(
			SendDatagram: m =>
			{
				if (!_shutDown)
					_transport.Send(m.RemoteEndPoint, m.Datagram);
				return Unit.Default;
			},
			ShutDown: _ =>
			{
				_shutDown = true;
				return Unit.Default;
			},
			LoopDone: _ => HandleLoopDone()
		);
}
