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
/// Runs the mailboxes scheduled on it one message at a time, only when a test asks (<see cref="RunNext"/>), on the
/// test's thread, choosing which next in one of three ways: at random from a seed (<see cref="ManualScheduler(int)"/>);
/// by priorities that a seed draws and changes at a few steps, as probabilistic concurrency testing (PCT) does
/// (<see cref="WithPriorities"/>); or as a list of choices says (<see cref="Following"/>), with which
/// <see cref="ExploreAll"/> takes every order of a small test, one after another. A test of several actors then takes
/// one order of their messages out of all, and the same seed or choices take the same order again. Used from one
/// thread at a time.
/// </summary>
public sealed class ManualScheduler : MailboxScheduler
{
	// Chooses which of the mailboxes that wait, in the order they were scheduled, runs next.
	private abstract class Order
	{
		public abstract int Choose(IReadOnlyList<Action> ready);
	}

	private sealed class RandomOrder(int seed) : Order
	{
		private readonly Random _random = new(seed);

		public override int Choose(IReadOnlyList<Action> ready) => _random.Next(ready.Count);
	}

	// PCT (Burckhardt et al., "A Randomized Scheduler with Probabilistic Guarantees of Finding Bugs", ASPLOS 2010): each
	// mailbox, and each one-off run such as a datagram's delivery, has a priority, drawn as it is first scheduled, and the
	// one of highest priority that waits runs; at depth - 1 steps drawn from the first `steps`, the one about to run
	// falls below all the others. A bug that takes `depth` messages in one order to show, among n mailboxes and k steps,
	// is then found by each seed with a probability of at least 1 / (n * k^(depth - 1)).
	private sealed class PriorityOrder : Order
	{
		private readonly Random _random;
		private readonly int _depth;
		private readonly Dictionary<Action, double> _priorities =
		[
			with(ReferenceEqualityComparer.Instance),
		];
		private readonly int[] _changePoints;
		private int _step;

		public PriorityOrder(int seed, int depth, int steps)
		{
			ArgumentOutOfRangeException.ThrowIfLessThan(depth, 1);
			ArgumentOutOfRangeException.ThrowIfLessThan(steps, 1);
			_random = new Random(seed);
			_depth = depth;
			_changePoints =
			[
				.. Enumerable.Range(0, depth - 1).Select(_ => _random.Next(1, steps + 1)),
			];
		}

		// Above every priority lowered at a change point, which are 1 to depth - 1.
		private double PriorityOf(Action run)
		{
			if (!_priorities.TryGetValue(run, out var priority))
			{
				priority = _depth + _random.NextDouble();
				_priorities.Add(run, priority);
			}

			return priority;
		}

		private int Highest(IReadOnlyList<Action> ready)
		{
			var highest = 0;
			for (var i = 1; i < ready.Count; i++)
			{
				if (PriorityOf(ready[i]) > PriorityOf(ready[highest]))
					highest = i;
			}

			return highest;
		}

		public override int Choose(IReadOnlyList<Action> ready)
		{
			_step++;
			var chosen = Highest(ready);
			for (var i = 0; i < _changePoints.Length; i++)
			{
				if (_changePoints[i] == _step)
				{
					_priorities[ready[chosen]] = i + 1;
					chosen = Highest(ready);
				}
			}

			return chosen;
		}
	}

	// The choices given, in turn, then the first that waits at each step.
	private sealed class GivenOrder(IReadOnlyList<int> choices) : Order
	{
		private readonly IReadOnlyList<int> _choices = choices;
		private int _step;

		public override int Choose(IReadOnlyList<Action> ready)
		{
			var choice = _step < _choices.Count ? _choices[_step] : 0;
			_step++;
			return Math.Min(choice, ready.Count - 1);
		}
	}

	private readonly List<Action> _ready = [];
	private readonly Order _order;
	private readonly List<(int Choice, int Of)> _taken = [];

	private ManualScheduler(Order order)
	{
		_order = order;
	}

	/// <summary>
	/// Chooses at random, from <paramref name="seed"/>.
	/// </summary>
	public ManualScheduler(int seed)
		: this(new RandomOrder(seed)) { }

	/// <summary>
	/// Chooses by priorities that <paramref name="seed"/> draws, changed at <paramref name="depth"/> - 1 of the first
	/// <paramref name="steps"/> steps, as PCT does: a bug that takes <paramref name="depth"/> messages in one order to show
	/// is found with a probability that has a lower bound, which a random order does not have.
	/// </summary>
	public static ManualScheduler WithPriorities(int seed, int depth, int steps) =>
		new(new PriorityOrder(seed, depth, steps));

	/// <summary>
	/// Chooses as <paramref name="choices"/> say, each the index of a mailbox among those that wait, in the order they
	/// were scheduled, then the first that waits once they run out.
	/// </summary>
	public static ManualScheduler Following(IReadOnlyList<int> choices) =>
		new(new GivenOrder(choices));

	public override int MessagesPerRun => 1;

	/// <summary>
	/// How many mailboxes wait to run.
	/// </summary>
	public int Ready => _ready.Count;

	/// <summary>
	/// The choice made at each step so far, and of how many that waited.
	/// </summary>
	public IReadOnlyList<(int Choice, int Of)> Taken => _taken;

	public override void Schedule(Action run) => _ready.Add(run);

	/// <summary>
	/// Runs one message of one mailbox, chosen as the scheduler chooses. Returns <see langword="false"/> if none waits.
	/// </summary>
	public bool RunNext()
	{
		if (_ready.Count == 0)
			return false;

		var index = _order.Choose(_ready);
		_taken.Add((index, _ready.Count));
		var run = _ready[index];
		_ready.RemoveAt(index);
		run();
		return true;
	}

	/// <summary>
	/// Runs <paramref name="test"/> once in each order of its messages, depth first, each time from its start with a
	/// scheduler of its own, which <paramref name="test"/> runs to its end: every order, if there are at most
	/// <paramref name="maxRuns"/>. Returns how many orders were run, and whether they are all of them.
	/// </summary>
	public static (int Runs, bool IsExhausted) ExploreAll(Action<ManualScheduler> test, int maxRuns)
	{
		List<int> choices = [];
		for (var runs = 1; ; runs++)
		{
			var scheduler = Following(choices);
			test(scheduler);

			// The last step that had a choice not taken yet, taken next, after the same steps before it.
			var taken = scheduler.Taken;
			var last = taken.Count - 1;
			while (last >= 0 && taken[last].Choice + 1 >= taken[last].Of)
				last--;
			if (last < 0)
				return (runs, true);
			if (runs >= maxRuns)
				return (runs, false);

			choices = [.. taken.Take(last).Select(step => step.Choice), taken[last].Choice + 1];
		}
	}
}
