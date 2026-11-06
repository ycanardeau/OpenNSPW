using System.Collections.Immutable;
using System.Net;
using Aigamo.MatchGenerator;
using Aigamo.Otsuki.Actors;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Messages.Reliable;
using Aigamo.Otsuki.Session;
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
	private readonly Func<SessionMessage, bool> _report;
	private readonly ImmutableArray<ConnectionLoop> _loops;
	private int _loopsDone;
	private bool _shutDown;

	public EndPoint LocalEndPoint => _transport.LocalEndPoint;

	/// <summary>
	/// The messages waiting in each loop's mailbox, from any thread.
	/// </summary>
	public ImmutableArray<int> LoopQueueLengths => [.. _loops.Select(l => l.QueueLength)];

	// A hash of the partner's address that every process computes alike: an endpoint's own hash differs from process
	// to process, and the same partner would take another loop in each run (FNV-1a of the address, then the port).
	private static int StableHash(EndPoint remoteEndPoint)
	{
		var hash = 2166136261u;
		void Add(byte value) => hash = (hash ^ value) * 16777619u;

		if (remoteEndPoint is IPEndPoint ip)
		{
			foreach (var value in ip.Address.GetAddressBytes())
				Add(value);
			Add((byte)ip.Port);
			Add((byte)(ip.Port >> 8));
		}
		else
		{
			foreach (var value in remoteEndPoint.ToString() ?? string.Empty)
			{
				Add((byte)value);
				Add((byte)(value >> 8));
			}
		}

		return (int)(hash & int.MaxValue);
	}

	/// <summary>
	/// The loop, of <paramref name="loops"/>, that runs the connection to <paramref name="remoteEndPoint"/>: the same in
	/// every process.
	/// </summary>
	public static int LoopIndex(EndPoint remoteEndPoint, int loops) =>
		StableHash(remoteEndPoint) % loops;

	private ConnectionLoop LoopFor(EndPoint remoteEndPoint) =>
		_loops[LoopIndex(remoteEndPoint, _loops.Length)];

	/// <param name="randomSeed">Seeds the loops' randomness, each its own, for a test that must repeat;
	/// <see langword="null"/>, the system's secure source.</param>
	public ReliableEndpoint(
		IDatagramTransport transport,
		TimeProvider timeProvider,
		ReliableProfile profile,
		Func<SessionMessage, bool> report,
		int loops = 1,
		MailboxScheduler? scheduler = null,
		int? randomSeed = null
	)
		: base(scheduler: scheduler)
	{
		_transport = transport;
		_report = report;
		_loops =
		[
			.. Enumerable
				.Range(0, Math.Max(1, loops))
				.Select(index => new ConnectionLoop(
					transport,
					timeProvider,
					profile,
					report,
					() => Post(new ReliableEndpointMessage.LoopDone()),
					scheduler,
					randomSeed + 1 + index
				)),
		];
		Start();
		_transport.Start(
			(remoteEndPoint, datagram) =>
				LoopFor(remoteEndPoint)
					.Post(new ConnectionLoopMessage.DatagramReceived(remoteEndPoint, datagram))
		);
	}

	/// <summary>
	/// Opens an outbound connection, which signs as <paramref name="signing"/> says, the session's mode, which the
	/// caller gives: the loops' own are for inbound connections. Returns its handle at once; it starts connecting once
	/// its loop has registered it, and if the loop has stopped, the session hears that it failed.
	/// </summary>
	public Connection Connect(EndPoint remoteEndPoint, PacketSigning signing)
	{
		var loop = LoopFor(remoteEndPoint);
		var connection = new Connection(remoteEndPoint, isOutbound: true, loop);
		if (!loop.Post(new ConnectionLoopMessage.RegisterConnection(connection, signing)))
			_report(new SessionMessage.ConnectionConnectFailed(connection, ResultCode.NoConnection));
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
