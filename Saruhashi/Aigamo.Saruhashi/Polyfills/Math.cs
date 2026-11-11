namespace System;

// Math.Clamp, which netstandard2.0 lacks: for the types Saruhashi clamps.
internal static class MathPolyfill
{
	extension(Math)
	{
		public static int Clamp(int value, int min, int max) =>
			min > max ? throw new ArgumentException($"'{min}' cannot be greater than {max}.")
			: value < min ? min
			: value > max ? max
			: value;

		public static long Clamp(long value, long min, long max) =>
			min > max ? throw new ArgumentException($"'{min}' cannot be greater than {max}.")
			: value < min ? min
			: value > max ? max
			: value;

		public static float Clamp(float value, float min, float max) =>
			min > max ? throw new ArgumentException($"'{min}' cannot be greater than {max}.")
			: value < min ? min
			: value > max ? max
			: value;
	}
}
