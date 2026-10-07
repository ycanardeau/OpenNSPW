using System.Text.Json;
using Xunit;
using Xunit.Sdk;

namespace OpenNspw.Tests;

// Replays the recorded calls of a function of the reference through the port (see docs/Porting.md, Function tests).
//
// Each call starts from a new game whose globals are set to the reference's state before the call, and whose rand()
// seed is set with srand(), as in the reference. Then the port must return the same value, leave every global with
// the same bytes, and leave rand() at the same point of its sequence.
internal static class FunctionTest
{
	private static void CompareReturn(JsonElement? expected, object? actual, List<string> problems)
	{
		switch (expected, actual)
		{
			case (null, null):
				break;
			case ({ ValueKind: JsonValueKind.Number } e, int a) when e.GetInt64() == a:
				break;
			case ({ ValueKind: JsonValueKind.String } e, double a)
				when BitConverter.DoubleToInt64Bits(FunctionCase.ParseDouble(e)) == BitConverter.DoubleToInt64Bits(a):
				break;
			case ({ ValueKind: JsonValueKind.String } e, double a):
				problems.Add($"return value: expected {TypeLayout.Format(typeof(double), BitConverter.GetBytes(FunctionCase.ParseDouble(e)))}, " +
					$"actual {TypeLayout.Format(typeof(double), BitConverter.GetBytes(a))}");
				break;
			default:
				problems.Add($"return value: expected {expected?.ToString() ?? "none"}, actual {actual ?? "none"}");
				break;
		}
	}

	private static IReadOnlyList<string> RunCase(FunctionCase c, Func<Nspw, FunctionCase, object?> call)
	{
		var game = new Nspw();
		var before = GameState.Initial.With(c.Before);
		before.Restore(game);
		game.srand(c.Seed);

		object? result;
		try
		{
			result = call(game, c);
		}
		catch (Exception e)
		{
			return [$"threw {e.GetType().Name}: {e.Message}"];
		}

		var problems = new List<string>();
		CompareReturn(c.Return, result, problems);
		problems.AddRange(before.With(c.After).Differences(game));
		int[] rand = [game.rand(), game.rand()];
		if (!rand.SequenceEqual(c.Rand))
		{
			problems.Add($"rand() after the call: expected {string.Join(", ", c.Rand)}, actual {string.Join(", ", rand)}");
		}

		return problems;
	}

	// Runs every recorded call of `function` in `file`. `call` calls the port's function with the case's arguments,
	// and returns its return value, or null if it returns void.
	public static void Run(string file, string function, Func<Nspw, FunctionCase, object?> call)
	{
		const int MaxFailures = 3;

		var count = 0;
		var failures = new List<string>();
		foreach (var c in FunctionCase.Load(file, function))
		{
			count++;
			var problems = RunCase(c, call);
			if (problems.Count > 0)
			{
				failures.Add($"Case {c.Index} (args [{string.Join(", ", c.Args)}], seed {c.Seed}):{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", problems)}");
				if (failures.Count >= MaxFailures)
				{
					break;
				}
			}
		}

		Assert.True(count > 0, $"No recorded calls of {file}/{function}.");
		if (failures.Count > 0)
		{
			throw new XunitException($"{file}/{function} differs from the reference:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
		}
	}
}
