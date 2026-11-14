namespace System;

// What `a..b` compiles to, which netstandard2.0 lacks.
internal readonly struct Range(Index start, Index end)
{
	public Index Start { get; } = start;
	public Index End { get; } = end;

	public static Range All => new(Index.Start, Index.End);

	public static Range StartAt(Index start) => new(start, Index.End);

	public static Range EndAt(Index end) => new(Index.Start, end);

	public (int Offset, int Length) GetOffsetAndLength(int length)
	{
		var start = Start.GetOffset(length);
		var end = End.GetOffset(length);
		if ((uint)end > (uint)length || (uint)start > (uint)end)
			throw new ArgumentOutOfRangeException(nameof(length));

		return (start, end - start);
	}
}
