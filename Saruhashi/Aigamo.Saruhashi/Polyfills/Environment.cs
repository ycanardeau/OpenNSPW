using System.Diagnostics;

namespace System;

// Environment.TickCount64, which netstandard2.0 lacks: the milliseconds of a clock that only goes forward, from the
// stopwatch's, which does not wrap after 24.9 days as Environment.TickCount does.
internal static class EnvironmentPolyfill
{
	extension(Environment)
	{
		public static long TickCount64 => Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;
	}
}
