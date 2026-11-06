namespace Aigamo.Otsuki.Actors;

/// <summary>
/// A count that any thread adds to and reads, such as the bytes of native memory held, which the threads that allocate
/// it and the finalizers that free it change.
/// </summary>
public sealed class Tally
{
	private long _value;

	/// <summary>
	/// The count as of the last addition.
	/// </summary>
	public long Value => Interlocked.Read(ref _value);

	/// <summary>
	/// Adds <paramref name="amount"/>, which may be negative, to the count.
	/// </summary>
	public void Add(long amount) => Interlocked.Add(ref _value, amount);
}
