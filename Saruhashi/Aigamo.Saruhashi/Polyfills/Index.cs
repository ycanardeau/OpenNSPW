namespace System;

// What `^1` compiles to, which netstandard2.0 lacks.
internal readonly struct Index(int value, bool fromEnd = false)
{
	public int Value { get; } = value;
	public bool IsFromEnd { get; } = fromEnd;

	public static Index Start => new(0);
	public static Index End => new(0, fromEnd: true);

	public int GetOffset(int length) => IsFromEnd ? length - Value : Value;

	public static implicit operator Index(int value) => new(value);
}
