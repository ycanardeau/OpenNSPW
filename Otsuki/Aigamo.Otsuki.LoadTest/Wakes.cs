using Aigamo.Otsuki.Actors;

namespace Aigamo.Otsuki.LoadTest;

/// <summary>
/// The wakes of a peer, counted from whichever threads make them: the runs of its mailboxes, by kind, that a
/// <see cref="CountingScheduler"/> schedules, and the callbacks of the timers that a <see cref="CountingTimeProvider"/>
/// makes. The rest of a process's work items are its sockets' receives and the runtime's own.
/// </summary>
internal sealed class Wakes
{
	public Tally SessionRuns { get; } = new();

	public Tally LoopRuns { get; } = new();

	public Tally EndpointRuns { get; } = new();

	public Tally OtherRuns { get; } = new();

	public Tally TimerCallbacks { get; } = new();
}

/// <summary>
/// Runs each mailbox on the thread pool, as Otsuki does by default, counting each run by the kind of its mailbox.
/// </summary>
internal sealed class CountingScheduler : MailboxScheduler
{
	private readonly Wakes _wakes;
	private readonly Type? _session;
	private readonly Type? _loop;
	private readonly Type? _endpoint;

	// The type of a mailbox of Otsuki's internal message type, which the load test cannot name.
	private static Type? MailboxOf(string assembly, string message) =>
		System.Reflection.Assembly.Load(assembly).GetType(message) is { } type
			? typeof(Mailbox<>).MakeGenericType(type)
			: null;

	public CountingScheduler(Wakes wakes)
	{
		_wakes = wakes;
		_session = MailboxOf("Aigamo.Otsuki.Core", "Aigamo.Otsuki.Session.SessionMessage");
		_loop = MailboxOf("Aigamo.Otsuki.Core", "Aigamo.Otsuki.Reliable.ConnectionLoopItem");
		_endpoint = MailboxOf("Aigamo.Otsuki", "Aigamo.Otsuki.Reliable.ReliableEndpointMessage");
	}

	private Tally TallyOf(object? mailbox)
	{
		var type = mailbox?.GetType();
		return type == _loop ? _wakes.LoopRuns
			: type == _session ? _wakes.SessionRuns
			: type == _endpoint ? _wakes.EndpointRuns
			: _wakes.OtherRuns;
	}

	public override void Schedule(Action run)
	{
		TallyOf(run.Target).Add(1);
		ThreadPool.Schedule(run);
	}
}

/// <summary>
/// The system's clock and timers, counting each callback of a timer.
/// </summary>
internal sealed class CountingTimeProvider(Wakes wakes) : TimeProvider
{
	private readonly Wakes _wakes = wakes;

	public override ITimer CreateTimer(
		TimerCallback callback,
		object? state,
		TimeSpan dueTime,
		TimeSpan period
	) =>
		System.CreateTimer(
			s =>
			{
				_wakes.TimerCallbacks.Add(1);
				callback(s);
			},
			state,
			dueTime,
			period
		);
}
