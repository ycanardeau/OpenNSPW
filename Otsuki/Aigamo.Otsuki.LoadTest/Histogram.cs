namespace Aigamo.Otsuki.LoadTest;

/// <summary>
/// Latencies in microseconds, in buckets of 32 per power of two, so that a percentile is within about 3% of the true
/// value. Owned by one thread.
/// </summary>
internal sealed class Histogram
{
	private const int SubBuckets = 32;
	private const int Linear = 2 * SubBuckets;
	private const int Powers = 40;

	private readonly long[] _counts = new long[Linear + Powers * SubBuckets];

	public long Count { get; private set; }

	public long Max { get; private set; }

	private static int IndexOf(long value)
	{
		if (value < Linear)
			return (int)Math.Max(0, value);

		var exponent = 63 - (int)long.LeadingZeroCount(value) - 5;
		return Linear + (exponent - 1) * SubBuckets + (int)((value >> exponent) - SubBuckets);
	}

	private static long ValueOf(int index)
	{
		if (index < Linear)
			return index;

		var exponent = (index - Linear) / SubBuckets + 1;
		var mantissa = (index - Linear) % SubBuckets + SubBuckets;
		return ((long)mantissa << exponent) + (1L << exponent) - 1;
	}

	public void Record(long microseconds)
	{
		_counts[Math.Min(IndexOf(microseconds), _counts.Length - 1)]++;
		Count++;
		Max = Math.Max(Max, microseconds);
	}

	public void Add(Histogram other)
	{
		for (var i = 0; i < _counts.Length; i++)
			_counts[i] += other._counts[i];
		Count += other.Count;
		Max = Math.Max(Max, other.Max);
	}

	public void Clear()
	{
		Array.Clear(_counts);
		Count = 0;
		Max = 0;
	}

	/// <summary>
	/// The value below which <paramref name="fraction"/> of the recorded values are, in microseconds.
	/// </summary>
	public long Percentile(double fraction)
	{
		if (Count == 0)
			return 0;

		var rank = (long)Math.Ceiling(fraction * Count);
		long seen = 0;
		for (var i = 0; i < _counts.Length; i++)
		{
			seen += _counts[i];
			if (seen >= rank)
				return Math.Min(ValueOf(i), Max);
		}

		return Max;
	}
}
