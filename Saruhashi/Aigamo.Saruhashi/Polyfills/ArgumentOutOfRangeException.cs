using System.Runtime.CompilerServices;

namespace System;

// .NET's throw helpers, which netstandard2.0 lacks: for an int only, as Saruhashi throws them.
internal static class ArgumentOutOfRangeExceptionPolyfill
{
	extension(ArgumentOutOfRangeException)
	{
		public static void ThrowIfNegative(
			int value,
			[CallerArgumentExpression(nameof(value))] string? paramName = null
		)
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(
					paramName,
					value,
					$"{paramName} ('{value}') must be a non-negative value."
				);
		}

		public static void ThrowIfGreaterThan(
			int value,
			int other,
			[CallerArgumentExpression(nameof(value))] string? paramName = null
		)
		{
			if (value > other)
				throw new ArgumentOutOfRangeException(
					paramName,
					value,
					$"{paramName} ('{value}') must be less than or equal to '{other}'."
				);
		}
	}
}
