using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using Aigamo.Otsuki.Actors;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Messages.Reliable;
using Aigamo.Otsuki.Reliable;
using Aigamo.Results;

namespace Aigamo.Otsuki.Session;

/// <summary>
/// The actor that runs a <see cref="SessionCore"/>: gives it each message of its mailbox with the time, and carries out
/// what it does, in order, on its own thread: opens and shuts the endpoint, posts to the connection loops, publishes
/// the snapshot, raises the events, completes the commands, and wakes it at its timers' deadlines.
/// </summary>
internal sealed class SessionActor : Actor<SessionMessage>, ISessionOutputs
{
	private readonly PeerOptions _options;
	private readonly Mailbox<PeerEvent> _events;
	private readonly Action<SessionSnapshot> _publish;
	private readonly LoopAlarm _alarm;
	private readonly RandomSource _random;

	// The open endpoint, which the peer's queries also read (LoopQueueLengths).
	private readonly Latest<ReliableEndpoint?> _endpoint = new(null);

	public SessionCore Core { get; }

	/// <summary>
	/// The messages waiting in each connection loop's mailbox, from any thread.
	/// </summary>
	public ImmutableArray<int> LoopQueueLengths => _endpoint.Value?.LoopQueueLengths ?? [];

	public SessionActor(
		PeerOptions options,
		Mailbox<PeerEvent> events,
		Action<SessionSnapshot> publish
	)
		: base(scheduler: options.Scheduler)
	{
		_options = options;
		_events = events;
		_publish = publish;
		_alarm = new LoopAlarm(options.TimeProvider, () => Post(new SessionMessage.TimersDue()));
		_random = new RandomSource(options.RandomSeed);
		Core = new SessionCore(options.ToSessionOptions(), this);
		Start();
	}

	Result<EndPoint, Exception> ISessionOutputs.OpenEndpoint(EndPoint localEndPoint)
	{
		try
		{
			var endpoint = new ReliableEndpoint(
				_options.TransportFactory(localEndPoint),
				_options.TimeProvider,
				_options.ReliableProfile,
				Post,
				_options.ConnectionLoops,
				_options.Scheduler,
				_options.RandomSeed
			);
			_endpoint.Post(endpoint);
			return endpoint.LocalEndPoint;
		}
		catch (Exception e)
		{
			return e;
		}
	}

	void ISessionOutputs.ShutDownEndpoint()
	{
		_endpoint.Value?.ShutDown();
		_endpoint.Post(null);
	}

	void ISessionOutputs.SetSigning(PacketSigning signing) => _endpoint.Value?.SetSigning(signing);

	Connection ISessionOutputs.Connect(EndPoint remoteEndPoint, PacketSigning signing) =>
		_endpoint.Value!.Connect(remoteEndPoint, signing);

	IPAddress? ISessionOutputs.ResolveHostname(string hostname)
	{
		try
		{
			return Dns.GetHostAddresses(hostname)
				.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
		}
		catch (SocketException)
		{
			// Reported like a name without IPv4 addresses.
			return null;
		}
	}

	void ISessionOutputs.Post(Connection connection, ConnectionMessage message) =>
		((ConnectionLoop)connection.Loop).Post(connection, message);

	void ISessionOutputs.SendToMany(object loop, ConnectionLoopMessage.SendToMany message) =>
		((ConnectionLoop)loop).Post(message);

	void ISessionOutputs.SendDatagram(EndPoint remoteEndPoint, byte[] datagram) =>
		_endpoint.Value?.Post(new ReliableEndpointMessage.SendDatagram(remoteEndPoint, datagram));

	void ISessionOutputs.Defer(SessionMessage message) => Post(message);

	void ISessionOutputs.Publish(SessionSnapshot snapshot) => _publish(snapshot);

	void ISessionOutputs.Emit(PeerEvent peerEvent) => _events.Post(peerEvent);

	void ISessionOutputs.EndEvents() => _events.Complete();

	void ISessionOutputs.Complete(Completion completion) =>
		((TaskCompletionSource)completion.Waiter).TrySetResult();

	void ISessionOutputs.Complete<T>(Completion<T> completion, T value) =>
		((TaskCompletionSource<T>)completion.Waiter).TrySetResult(value);

	void ISessionOutputs.Fail(Completion completion, Exception exception) =>
		((TaskCompletionSource)completion.Waiter).TrySetException(exception);

	void ISessionOutputs.Fail<T>(Completion<T> completion, Exception exception) =>
		((TaskCompletionSource<T>)completion.Waiter).TrySetException(exception);

	void ISessionOutputs.FillRandom(Span<byte> bytes) => _random.Fill(bytes);

	void ISessionOutputs.Stop()
	{
		_alarm.Dispose();
		Stop();
	}

	protected override void Receive(SessionMessage message)
	{
		if (message is SessionMessage.TimersDue)
			_alarm.Rang();
		Core.Handle(message, _alarm.Now);
		_alarm.Arm(Core.Timers.NextDeadline);
	}
}
