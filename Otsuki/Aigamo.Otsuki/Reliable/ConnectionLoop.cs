using System.Net;
using Aigamo.Otsuki.Actors;
using Aigamo.Otsuki.Session;
using Aigamo.Otsuki.Transport;

namespace Aigamo.Otsuki.Reliable;

/// <summary>
/// The actor that runs a <see cref="ConnectionLoopCore"/>: gives it each message of its mailbox with the time, and
/// carries out what it does, in order, on its own thread: sends on the transport, reports to the session, and wakes it
/// at its timers' deadlines.
/// </summary>
internal sealed class ConnectionLoop : Actor<ConnectionLoopItem>, IConnectionLoopOutputs
{
	private readonly IDatagramTransport _transport;
	private readonly Func<SessionMessage, bool> _report;
	private readonly Action _onDone;
	private readonly LoopAlarm _alarm;
	private readonly RandomSource _random;

	public ConnectionLoopCore Core { get; }

	/// <param name="report">Posts a connection's report to the session.</param>
	/// <param name="onDone">Called once the loop is shut down and its last connection has terminated.</param>
	/// <param name="randomSeed">Seeds the loop's randomness, for a test that must repeat; <see langword="null"/>, the
	/// system's secure source.</param>
	public ConnectionLoop(
		IDatagramTransport transport,
		TimeProvider timeProvider,
		ReliableProfile profile,
		Func<SessionMessage, bool> report,
		Action onDone,
		MailboxScheduler? scheduler = null,
		int? randomSeed = null
	)
		: base(scheduler: scheduler)
	{
		_transport = transport;
		_report = report;
		_onDone = onDone;
		_random = new RandomSource(randomSeed);
		_alarm = new LoopAlarm(timeProvider, () => Post(new ConnectionLoopMessage.TimersDue()));
		Core = new ConnectionLoopCore(this, profile, this);
		Start();
	}

	public bool Post(Connection connection, ConnectionMessage message) =>
		Post(new ConnectionLoopItem(connection, message, null));

	public bool Post(ConnectionLoopMessage message) =>
		Post(new ConnectionLoopItem(null, null, message));

	void IConnectionLoopOutputs.Send(EndPoint remoteEndPoint, byte[] datagram) =>
		_transport.Send(remoteEndPoint, datagram);

	void IConnectionLoopOutputs.Report(SessionMessage message) => _report(message);

	void IConnectionLoopOutputs.Defer(Connection connection, ConnectionMessage message) =>
		Post(connection, message);

	void IConnectionLoopOutputs.Done()
	{
		_alarm.Dispose();
		Stop();
		_onDone();
	}

	void IConnectionLoopOutputs.FillRandom(Span<byte> bytes) => _random.Fill(bytes);

	protected override void Receive(ConnectionLoopItem item)
	{
		if (item.ToLoop is ConnectionLoopMessage.TimersDue)
			_alarm.Rang();
		Core.Handle(item, _alarm.Now);
		_alarm.Arm(Core.Timers.NextDeadline);
	}
}
