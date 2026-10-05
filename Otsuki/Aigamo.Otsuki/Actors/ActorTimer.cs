namespace Aigamo.Otsuki.Actors;

/// <summary>
/// A one-shot timer owned by an actor. When it elapses it posts a message carrying a generation number
/// to the actor, which passes that number to <see cref="TryConsume"/> to discard firings that were
/// rescheduled or canceled in the meantime. Must only be used from the owning actor's message loop.
/// </summary>
internal sealed class ActorTimer<TMessage>(
	TimeProvider timeProvider,
	Func<long, TMessage> createMessage,
	Func<TMessage, bool> post
)
	where TMessage : notnull
{
	private ITimer? _timer;
	private long _generation;

	public bool IsRunning => _timer is not null;

	public void Cancel()
	{
		_timer?.Dispose();
		_timer = null;
		_generation++;
	}

	public void Start(TimeSpan dueTime)
	{
		Cancel();
		var generation = _generation;
		_timer = timeProvider.CreateTimer(
			_ => post(createMessage(generation)),
			null,
			dueTime,
			Timeout.InfiniteTimeSpan
		);
	}

	/// <summary>
	/// Returns <see langword="true"/> if the elapsed message belongs to the current schedule.
	/// The timer is then no longer running.
	/// </summary>
	public bool TryConsume(long generation)
	{
		if (_timer is null || generation != _generation)
			return false;

		Cancel();
		return true;
	}
}
