namespace Aigamo.Otsuki.Actors;

/// <summary>
/// The clock and the runtime timer of an actor that runs a core: gives the core the time of each input, and wakes it
/// at its nearest deadline by posting to the actor. Armed for one deadline at a time, the nearest asked for; a later
/// one waits until it has rung. Only used on the actor's thread.
/// </summary>
internal sealed class LoopAlarm(TimeProvider timeProvider, Action onDue)
{
	private readonly TimeProvider _timeProvider = timeProvider;
	private readonly Action _onDue = onDue;
	private readonly long _origin = timeProvider.GetTimestamp();
	private ITimer? _timer;
	private TimeSpan _armedAt = TimeSpan.MaxValue;
	private bool _disposed;

	/// <summary>
	/// The time since the alarm was made, which the core takes as its clock.
	/// </summary>
	public TimeSpan Now => _timeProvider.GetElapsedTime(_origin);

	/// <summary>
	/// Rings at <paramref name="deadline"/>, unless it is armed sooner already.
	/// </summary>
	public void Arm(TimeSpan? deadline)
	{
		if (_disposed || deadline is not TimeSpan at || at >= _armedAt)
			return;

		_armedAt = at;
		var due = at - Now;
		_timer ??= _timeProvider.CreateTimer(
			_ => _onDue(),
			null,
			Timeout.InfiniteTimeSpan,
			Timeout.InfiniteTimeSpan
		);
		_timer.Change(due > TimeSpan.Zero ? due : TimeSpan.Zero, Timeout.InfiniteTimeSpan);
	}

	/// <summary>
	/// The alarm has rung: the next <see cref="Arm"/> sets it again.
	/// </summary>
	public void Rang() => _armedAt = TimeSpan.MaxValue;

	public void Dispose()
	{
		_disposed = true;
		_timer?.Dispose();
	}
}
