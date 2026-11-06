namespace Aigamo.Otsuki.Reliable;

/// <summary>
/// The timers of a core: their deadlines in one queue, on a clock that only moves when the caller says what time it
/// is (<see cref="Advance"/>), as an input. The core's caller arms one runtime timer of its own for
/// <see cref="NextDeadline"/>, and calls the core again when it comes. A timer is in the queue once at most, at the
/// deadline it had when it was queued: started again later, it moves nothing, and is queued again at its new deadline
/// when the old one comes; started again sooner, it is queued anew, and the old entry is dropped when it comes. So a
/// keep-alive restarted by every frame costs no runtime timer.
/// </summary>
internal sealed class LoopTimers
{
	private readonly PriorityQueue<LoopTimer, TimeSpan> _deadlines = new();

	/// <summary>
	/// The time of the input being handled, on the caller's clock.
	/// </summary>
	public TimeSpan Now { get; private set; }

	/// <summary>
	/// The nearest deadline in the queue, which may belong to a timer started again since: the caller wakes the core
	/// then, and the core finds what has really come.
	/// </summary>
	public TimeSpan? NextDeadline => _deadlines.TryPeek(out _, out var at) ? at : null;

	/// <summary>
	/// Sets the time of the input that comes; a clock that went back is not followed.
	/// </summary>
	public void Advance(TimeSpan now)
	{
		if (now > Now)
			Now = now;
	}

	public void Enqueue(LoopTimer timer, TimeSpan deadline) => _deadlines.Enqueue(timer, deadline);

	/// <summary>
	/// Fires the timers whose deadlines have come by <see cref="Now"/>.
	/// </summary>
	public void FireDue()
	{
		while (_deadlines.TryPeek(out var timer, out var at) && at <= Now)
		{
			_deadlines.Dequeue();
			timer.Come(at, Now);
		}
	}
}

/// <summary>
/// A one-shot timer of a core, run by its <see cref="LoopTimers"/>. When it elapses it calls back with a generation
/// number, which the owner passes to <see cref="TryConsume"/>, so that a firing that a later start or a cancel
/// replaced does nothing.
/// </summary>
internal sealed class LoopTimer(LoopTimers timers, Action<long> elapse)
{
	private readonly LoopTimers _timers = timers;
	private readonly Action<long> _elapse = elapse;
	private TimeSpan? _deadline;

	// The deadline at which the timer waits in the queue, if it does.
	private TimeSpan? _queuedAt;
	private long _generation;

	public bool IsRunning => _deadline is not null;

	public void Cancel()
	{
		_deadline = null;
		_generation++;
	}

	public void Start(TimeSpan dueTime)
	{
		_generation++;
		var deadline = _timers.Now + dueTime;
		_deadline = deadline;
		if (_queuedAt is TimeSpan queuedAt && queuedAt <= deadline)
			return;

		_queuedAt = deadline;
		_timers.Enqueue(this, deadline);
	}

	/// <summary>
	/// The queue's entry at <paramref name="at"/> has come: fires if the deadline has, or waits again for a later one.
	/// </summary>
	public void Come(TimeSpan at, TimeSpan now)
	{
		if (at != _queuedAt)
			return;

		_queuedAt = null;
		if (_deadline is not TimeSpan deadline)
			return;

		if (deadline > now)
		{
			_queuedAt = deadline;
			_timers.Enqueue(this, deadline);
			return;
		}

		_elapse(_generation);
	}

	/// <summary>
	/// Returns <see langword="true"/> if the firing of <paramref name="generation"/> belongs to the current schedule.
	/// The timer is then no longer running.
	/// </summary>
	public bool TryConsume(long generation)
	{
		if (_deadline is null || generation != _generation)
			return false;

		Cancel();
		return true;
	}

	/// <summary>
	/// A timer that calls <paramref name="elapsed"/> once, <paramref name="dueTime"/> from now, unless canceled first.
	/// </summary>
	public static LoopTimer Once(LoopTimers timers, TimeSpan dueTime, Action elapsed)
	{
		LoopTimer? timer = null;
		timer = new LoopTimer(
			timers,
			generation =>
			{
				if (timer!.TryConsume(generation))
					elapsed();
			}
		);
		timer.Start(dueTime);
		return timer;
	}
}
