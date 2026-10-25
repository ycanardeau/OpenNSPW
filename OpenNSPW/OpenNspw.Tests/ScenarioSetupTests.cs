using System.Runtime.InteropServices;
using OpenNspw.Singleplayer;
using OpenNspw.Tests.Lockstep;
using Xunit;

namespace OpenNspw.Tests;

// The scenario setups of 1.22 lifted to C# (Scenarios122.cs) against the emulated originals: from the same state and
// the same random numbers, each places the same units, planes and fleets, sets the same globals and map, and draws as
// many random numbers.
public class ScenarioSetupTests
{
	public static TheoryData<int> Scenarios()
	{
		var data = new TheoryData<int>();
		foreach (var scenario in Functions.Scenarios)
		{
			data.Add(scenario);
		}

		return data;
	}

	private sealed record SetupState(
		byte[] Units,
		byte[] Fleets,
		byte[] Map,
		int[] Globals,
		WorldPosition Camera,
		int NextRandom
	);

	private static SetupState Capture(Nspw game)
	{
		return new SetupState(
			MemoryMarshal.AsBytes((Span<Unit>)game.Units).ToArray(),
			MemoryMarshal.AsBytes((Span<ComputerFleet>)game.ComputerFleets).ToArray(),
			MemoryMarshal.AsBytes((Span<Array256<ushort>>)game.MapTiles).ToArray(),
			[
				game.MaxUnitId,
				(int)game.LocalSide,
				(int)game.CpuSide,
				game.BattleTime,
				game.DecisionPoints[0],
				game.DecisionPoints[1],
				game.DecisionPoints[2],
				game.DecisionPoints[3],
				game.CurrentMap,
			],
			game.CameraPosition,
			game.rand()
		);
	}

	// Where the two states first differ.
	private static string? Difference(SetupState expected, SetupState actual)
	{
		var size = System.Runtime.CompilerServices.Unsafe.SizeOf<Unit>();
		for (var i = 0; i < expected.Units.Length; i++)
		{
			if (expected.Units[i] != actual.Units[i])
			{
				var location = TypeLayout.Locate(typeof(Unit), i % size);
				return $"unit {i / size}, {location}: emulated {expected.Units[i]}, rewritten {actual.Units[i]} (byte {i % size})";
			}
		}

		for (var i = 0; i < expected.Fleets.Length; i++)
		{
			if (expected.Fleets[i] != actual.Fleets[i])
			{
				return $"fleet {i / 0x498}, byte 0x{i % 0x498:X}";
			}
		}

		if (!expected.Map.AsSpan().SequenceEqual(actual.Map))
		{
			return "map";
		}

		for (var i = 0; i < expected.Globals.Length; i++)
		{
			if (expected.Globals[i] != actual.Globals[i])
			{
				return $"global {i}: emulated {expected.Globals[i]}, rewritten {actual.Globals[i]}";
			}
		}

		if (expected.Camera.X != actual.Camera.X || expected.Camera.Y != actual.Camera.Y)
		{
			return "camera";
		}

		return expected.NextRandom != actual.NextRandom ? "rand()" : null;
	}

	// The globals that a setup sets, as the game had them before.
	private sealed record Globals(
		short MaxUnitId,
		Side LocalSide,
		Side CpuSide,
		int BattleTime,
		WorldPosition Camera,
		short CurrentMap
	);

	// The state game_init leaves before it calls the scenario: no units, a map of sea, and the fleets cleared.
	private static void Clear(Nspw game, byte[] start, Globals globals)
	{
		State122.Restore(game, start);
		game.MaxUnitId = globals.MaxUnitId;
		game.LocalSide = globals.LocalSide;
		game.CpuSide = globals.CpuSide;
		game.BattleTime = globals.BattleTime;
		game.CameraPosition = globals.Camera;
		game.CurrentMap = globals.CurrentMap;
		game.Units = default;
		game.MapTiles = default;
		game.ComputerFleets = default;
		for (var f = 0; f < 11; f++)
		{
			game.ComputerFleets[f].DestinationX = World.Beyond;
		}

		game.DecisionPoints = default;
	}

	[Theory]
	[MemberData(nameof(Scenarios))]
	public void TheSetupIsTheOriginals(int scenario)
	{
		using var run = new SingleplayerRun(scenario);
		var game = run.Game;
		run.Frames(1);
		var start = State122.Save(game);
		var globals = new Globals(
			game.MaxUnitId,
			game.LocalSide,
			game.CpuSide,
			game.BattleTime,
			game.CameraPosition,
			game.CurrentMap
		);
		for (var seed = 0u; seed < 16; seed++)
		{
			Clear(game, start, globals);
			game.srand(seed);
			game.RunEmulated(Functions.SetUpScenario(scenario));
			var emulated = Capture(game);

			Clear(game, start, globals);
			game.srand(seed);
			game.SetUpScenarioAs122(scenario);
			var rewritten = Capture(game);

			Assert.True(
				Difference(emulated, rewritten) is not { } difference,
				$"Seed {seed}: {Difference(emulated, rewritten)}"
			);
		}
	}
}
