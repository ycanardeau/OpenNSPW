namespace Aigamo.Otsuki.Actors;

/// <summary>
/// The newest of an immutable value that one actor posts and any thread reads, such as a session's snapshot of its
/// players: a message that each reader takes as of the last post, and that no one changes.
/// </summary>
public sealed class Latest<T>(T initial)
	where T : class?
{
	// Read and written only with Interlocked (OTSUKI0006).
	private T _value = initial;

	/// <summary>
	/// The value posted last, or the initial one.
	/// </summary>
	public T Value => Interlocked.CompareExchange(ref _value, null!, null!);

	/// <summary>
	/// Replaces the value, for the reads that follow.
	/// </summary>
	public void Post(T value) => Interlocked.Exchange(ref _value, value);
}
