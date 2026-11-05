namespace Aigamo.Otsuki.Tests;

/// <summary>
/// A clock that moves only when a test moves it (<see cref="FireNext"/>): its timers fire on the test's thread, in
/// the order of their deadlines, and no real time passes. Used from one thread at a time.
/// </summary>
internal sealed class ManualTimeProvider : TimeProvider
{
	private sealed class ManualTimer(ManualTimeProvider clock, TimerCallback callback, object? state)
		: ITimer
	{
		private readonly ManualTimeProvider _clock = clock;
		private readonly TimerCallback _callback = callback;
		private readonly object? _state = state;

		public long Due { get; private set; } = long.MaxValue;

		public bool IsDisposed { get; private set; }

		public bool Change(TimeSpan dueTime, TimeSpan period)
		{
			if (IsDisposed)
				return false;

			Due =
				dueTime == Timeout.InfiniteTimeSpan
					? long.MaxValue
					: _clock._now + Math.Max(0, dueTime.Ticks);
			return true;
		}

		public void Fire()
		{
			Due = long.MaxValue;
			_callback(_state);
		}

		public void Dispose()
		{
			IsDisposed = true;
			Due = long.MaxValue;
		}

		public ValueTask DisposeAsync()
		{
			Dispose();
			return ValueTask.CompletedTask;
		}
	}

	private static readonly DateTimeOffset Epoch = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	private readonly List<ManualTimer> _timers = [];
	private long _now = TimeSpan.FromSeconds(1000).Ticks;

	public override long TimestampFrequency => TimeSpan.TicksPerSecond;

	public override long GetTimestamp() => _now;

	public override DateTimeOffset GetUtcNow() => Epoch + TimeSpan.FromTicks(_now);

	public TimeSpan Now => TimeSpan.FromTicks(_now);

	public override ITimer CreateTimer(
		TimerCallback callback,
		object? state,
		TimeSpan dueTime,
		TimeSpan period
	)
	{
		var timer = new ManualTimer(this, callback, state);
		_timers.Add(timer);
		timer.Change(dueTime, period);
		return timer;
	}

	/// <summary>
	/// Moves the clock to the nearest deadline, if it is before <paramref name="limit"/>, and fires that timer.
	/// Returns <see langword="false"/> if no timer is due by then.
	/// </summary>
	public bool FireNext(TimeSpan limit)
	{
		_timers.RemoveAll(t => t.IsDisposed);
		var next = _timers.Where(t => t.Due != long.MaxValue).MinBy(t => t.Due);
		if (next is null || next.Due > limit.Ticks)
			return false;

		_now = Math.Max(_now, next.Due);
		next.Fire();
		return true;
	}
}
