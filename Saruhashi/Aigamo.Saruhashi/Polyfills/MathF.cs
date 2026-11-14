namespace System;

// netstandard2.0 has a MathF of its own that it does not make public.
internal static class MathF
{
	public static float Floor(float x) => (float)Math.Floor(x);

	public static float Round(float x) => (float)Math.Round(x);
}
