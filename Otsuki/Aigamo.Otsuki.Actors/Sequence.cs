namespace Aigamo.Otsuki.Actors;

/// <summary>
/// Numbers drawn from any thread, each once, from 1 up, such as the handles of sends.
/// </summary>
public sealed class Sequence
{
	private long _last;

	public long Next() => Interlocked.Increment(ref _last);
}
