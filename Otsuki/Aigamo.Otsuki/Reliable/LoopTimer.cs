namespace Aigamo.Otsuki.Reliable;

/// <summary>
/// The timers of a <see cref="ConnectionLoop"/>'s connections: their deadlines in one queue, and one runtime timer
/// for the nearest, which posts to the loop. A timer is in the queue once at most, at the deadline it had when it was
/// queued: started again later, it moves nothing, and is queued again at its new deadline when the old one comes;
/// started again sooner, it is queued anew, and the old entry is dropped when it comes. So a keep-alive restarted by
/// every frame costs no runtime timer and no lock. Only used on the loop's thread.
/// </summary>
internal sealed class LoopTimers(TimeProvider timeProvider, Action onDue)
{
	private readonly TimeProvider _timeProvider = timeProvider;
	private readonly Action _onDue = onDue;
	private readonly PriorityQueue<LoopTimer, long> _deadlines = new();
	private ITimer? _alarm;
	private long _alarmAt = long.MaxValue;

	public long Now => _timeProvider.GetTimestamp();

	public long ToTicks(TimeSpan duration) =>
		(long)(duration.TotalSeconds * _timeProvider.TimestampFrequency);

	private void Arm(long deadline)
	{
		_alarmAt = deadline;
		var due = TimeSpan.FromSeconds(
			Math.Max(0, deadline - Now) / (double)_timeProvider.TimestampFrequency
		);
		_alarm ??= _timeProvider.CreateTimer(
			_ => _onDue(),
			null,
			Timeout.InfiniteTimeSpan,
			Timeout.InfiniteTimeSpan
		);
		_alarm.Change(due, Timeout.InfiniteTimeSpan);
	}

	public void Enqueue(LoopTimer timer, long deadline)
	{
		_deadlines.Enqueue(timer, deadline);
		if (deadline < _alarmAt)
			Arm(deadline);
	}

	/// <summary>
	/// Fires the timers whose deadlines have come, and arms the runtime timer for the next.
	/// </summary>
	public void FireDue()
	{
		_alarmAt = long.MaxValue;
		var now = Now;
		while (_deadlines.TryPeek(out var timer, out var at) && at <= now)
		{
			_deadlines.Dequeue();
			timer.Come(at, now);
		}

		if (_deadlines.TryPeek(out _, out var next))
			Arm(next);
	}

	public void Dispose() => _alarm?.Dispose();
}

/// <summary>
/// A one-shot timer of a connection, run by its loop's <see cref="LoopTimers"/>. When it elapses it hands the
/// connection a message carrying a generation number, which the connection passes to <see cref="TryConsume"/>, as it
/// did with a timer of its own. Must only be used on the loop's thread.
/// </summary>
internal sealed class LoopTimer(
	LoopTimers timers,
	Func<long, ConnectionMessage> createMessage,
	Action<ConnectionMessage> handle
)
{
	private readonly LoopTimers _timers = timers;
	private readonly Func<long, ConnectionMessage> _createMessage = createMessage;
	private readonly Action<ConnectionMessage> _handle = handle;
	private long? _deadline;

	// The deadline at which the timer waits in the queue, if it does.
	private long? _queuedAt;
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
		var deadline = _timers.Now + _timers.ToTicks(dueTime);
		_deadline = deadline;
		if (_queuedAt is long queuedAt && queuedAt <= deadline)
			return;

		_queuedAt = deadline;
		_timers.Enqueue(this, deadline);
	}

	/// <summary>
	/// The queue's entry at <paramref name="at"/> has come: fires if the deadline has, or waits again for a later one.
	/// </summary>
	public void Come(long at, long now)
	{
		if (at != _queuedAt)
			return;

		_queuedAt = null;
		if (_deadline is not long deadline)
			return;

		if (deadline > now)
		{
			_queuedAt = deadline;
			_timers.Enqueue(this, deadline);
			return;
		}

		_handle(_createMessage(_generation));
	}

	/// <summary>
	/// Returns <see langword="true"/> if the elapsed message belongs to the current schedule. The timer is then no
	/// longer running.
	/// </summary>
	public bool TryConsume(long generation)
	{
		if (_deadline is null || generation != _generation)
			return false;

		Cancel();
		return true;
	}
}
