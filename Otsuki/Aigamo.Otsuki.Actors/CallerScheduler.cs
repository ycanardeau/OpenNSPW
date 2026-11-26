namespace Aigamo.Otsuki.Actors;

/// <summary>
/// Runs the mailboxes scheduled on it on the thread that calls <see cref="RunReady"/>, when it calls, rather than on the
/// thread pool: a DoWork mode for actors. A mailbox that a message wakes, from any thread, such as a timer's, waits for
/// the next call. For many peers in one process, such as a server of many clients or a load test's clients: they then
/// cost no wake of a thread for each message, and run one at a time, on that thread, in turns.
/// </summary>
/// <remarks>
/// The thread runs every peer whose options name the scheduler, so it calls <see cref="RunReady"/> often, as a game
/// loop calls DoWork; a peer's tasks, such as <c>ConnectAsync</c> and <c>CloseAsync</c>, complete only while it does,
/// so it must not wait for one without calling it. Called from one thread at a time.
/// </remarks>
public sealed class CallerScheduler : MailboxScheduler
{
	// Posted from any thread that wakes a mailbox; taken by the thread that runs them.
	private readonly Mailbox<Action> _ready = new();

	public override void Schedule(Action run) => _ready.Post(run);

	/// <summary>
	/// Runs the mailboxes that wait, and those they wake in turn, until none waits. Returns how many runs it made.
	/// </summary>
	public int RunReady()
	{
		var runs = 0;
		while (_ready.TryReceive(out var run))
		{
			run();
			runs++;
		}

		return runs;
	}
}
