namespace Aigamo.Otsuki.Actors;

/// <summary>
/// Runs pooled mailboxes: the thread pool (<see cref="ThreadPool"/>), or a test that chooses which runs next
/// (<see cref="ManualScheduler"/>).
/// </summary>
public abstract class MailboxScheduler
{
	private sealed class ThreadPoolScheduler : MailboxScheduler
	{
		public override void Schedule(Action run) =>
			System.Threading.ThreadPool.UnsafeQueueUserWorkItem(
				static run => run(),
				run,
				preferLocal: false
			);
	}

	/// <summary>
	/// Runs each mailbox on the thread pool while it has messages.
	/// </summary>
	public static MailboxScheduler ThreadPool { get; } = new ThreadPoolScheduler();

	/// <summary>
	/// How many messages a mailbox handles each time it runs before it is scheduled again: all it has, unless a
	/// scheduler wants to choose between mailboxes at each message.
	/// </summary>
	public virtual int MessagesPerRun => int.MaxValue;

	/// <summary>
	/// Runs <paramref name="run"/> once, later.
	/// </summary>
	public abstract void Schedule(Action run);
}

/// <summary>
/// Runs the mailboxes scheduled on it one message at a time, choosing which next by a seeded random order, only when a
/// test asks (<see cref="RunNext"/>), on the test's thread: a test of several actors then takes one order of their
/// messages out of all, and the same seed takes the same order again. Used from one thread at a time.
/// </summary>
public sealed class ManualScheduler(int seed) : MailboxScheduler
{
	private readonly List<Action> _ready = [];
	private readonly Random _random = new(seed);

	public override int MessagesPerRun => 1;

	/// <summary>
	/// How many mailboxes wait to run.
	/// </summary>
	public int Ready => _ready.Count;

	public override void Schedule(Action run) => _ready.Add(run);

	/// <summary>
	/// Runs one message of one mailbox, chosen at random. Returns <see langword="false"/> if none waits.
	/// </summary>
	public bool RunNext()
	{
		if (_ready.Count == 0)
			return false;

		var index = _random.Next(_ready.Count);
		var run = _ready[index];
		_ready.RemoveAt(index);
		run();
		return true;
	}
}
