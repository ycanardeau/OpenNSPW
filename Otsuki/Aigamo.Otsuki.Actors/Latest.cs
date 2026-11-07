namespace Aigamo.Otsuki.Actors;

/// <summary>
/// The newest of an immutable value that one actor posts and any thread reads, such as a session's snapshot of its
/// players: a message that each reader takes as of the last post, and that no one changes.
/// </summary>
public sealed class Latest<T>(T initial)
	where T : class?
{
	private T _value = initial;

	/// <summary>
	/// The value posted last, or the initial one.
	/// </summary>
	public T Value => Volatile.Read(ref _value);

	/// <summary>
	/// Replaces the value, for the reads that follow.
	/// </summary>
	public void Post(T value) => Volatile.Write(ref _value, value);
}
