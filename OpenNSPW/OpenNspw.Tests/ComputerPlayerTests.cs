using System.Runtime.InteropServices;
using OpenNspw.Tests.Lockstep;
using Xunit;
using Xunit.Abstractions;

namespace OpenNspw.Tests;

// The rewrites of NSPW 1.22's computer player (ComputerPlayer.cs) against the emulated originals, on states of
// single-player games (docs/ComputerPlayer.md).
public class ComputerPlayerTests(ITestOutputHelper output)
{
	private readonly ITestOutputHelper _output = output;

	// The table of the computer's fleets, in the emulator's memory.
	private const int FleetTableSize = 11 * 0x498;

	// What decision changes: the units, the result, decision_point, the fleets, and rand()'s state, by its next number.
	private sealed record DecisionState(
		byte[] Units,
		GameResult Result,
		int[] DecisionPoints,
		int BattleTime,
		short MaxUnitId,
		byte[] Fleets,
		int NextRandom
	);

	public static TheoryData<int> Scenarios()
	{
		var data = new TheoryData<int>();
		foreach (var scenario in new[] { 3, 4, 6, 7, 8, 101, 108, 200, 209, 301 })
		{
			data.Add(scenario);
		}

		return data;
	}

	private static byte[] UnitBytes(Nspw game)
	{
		return MemoryMarshal.AsBytes((Span<Unit>)game.Units).ToArray();
	}

	private static void Restore(Nspw game, byte[] units)
	{
		units.CopyTo(MemoryMarshal.AsBytes((Span<Unit>)game.Units));
	}

	private static int[] DecisionPointsOf(Nspw game)
	{
		return
		[
			game.DecisionPoints[0],
			game.DecisionPoints[1],
			game.DecisionPoints[2],
			game.DecisionPoints[3],
		];
	}

	private static byte[] FleetBytes(Nspw game)
	{
		return MemoryMarshal.AsBytes((Span<ComputerFleet>)game.ComputerFleets).ToArray();
	}

	// The state after decision; rand() is advanced.
	private static DecisionState Capture(Nspw game)
	{
		return new DecisionState(
			UnitBytes(game),
			game.Result,
			DecisionPointsOf(game),
			game.BattleTime,
			game.MaxUnitId,
			FleetBytes(game),
			game.rand()
		);
	}

	private static void Restore(Nspw game, DecisionState state)
	{
		Restore(game, state.Units);
		game.Result = state.Result;
		for (var i = 0; i < 4; i++)
		{
			game.DecisionPoints[i] = state.DecisionPoints[i];
		}

		game.BattleTime = state.BattleTime;
		game.MaxUnitId = state.MaxUnitId;
		state.Fleets.CopyTo(MemoryMarshal.AsBytes((Span<ComputerFleet>)game.ComputerFleets));
	}

	// Where the two states first differ.
	private static string? Difference(DecisionState expected, DecisionState actual)
	{
		var size = System.Runtime.CompilerServices.Unsafe.SizeOf<Unit>();
		var unitByte = expected.Units.AsSpan().CommonPrefixLength(actual.Units);
		if (unitByte < expected.Units.Length)
		{
			return $"unit {unitByte / size}, byte {unitByte % size}";
		}

		var fleetByte = expected.Fleets.AsSpan().CommonPrefixLength(actual.Fleets);
		if (fleetByte < expected.Fleets.Length)
		{
			return $"fleet table, byte 0x{fleetByte:X}";
		}

		if (
			expected.Result == actual.Result
			&& expected.DecisionPoints.SequenceEqual(actual.DecisionPoints)
			&& expected.BattleTime == actual.BattleTime
			&& expected.MaxUnitId == actual.MaxUnitId
			&& expected.NextRandom == actual.NextRandom
		)
		{
			return null;
		}

		return $"result {expected.Result}/{actual.Result}, decision_point [{string.Join(",", expected.DecisionPoints)}]/[{string.Join(",", actual.DecisionPoints)}], rest_time {expected.BattleTime}/{actual.BattleTime}, max_unit {expected.MaxUnitId}/{actual.MaxUnitId}, rand {expected.NextRandom}/{actual.NextRandom}";
	}

	// Points on the edges of the rectangles that decision tests.
	private static readonly int[] EdgeXs = [-8400, -1040, -800, -80, 400, 4600, 6600, 7000, 9000];
	private static readonly int[] EdgeYs = [-8000, -6000, -3400, -1400, -240, -160, 3120, 3600];

	// Changes a quarter of the units at random, in what decision tests: their side, kind, type, fuel, HP, position, and
	// whether they are parked being serviced, and at which base.
	private static void Mutate(Nspw game, Random random)
	{
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			if (random.Next(4) != 0)
			{
				continue;
			}

			ref var unit = ref game.Units[m];
			switch (random.Next(8))
			{
				case 0:
					unit.Side = (Side)random.Next(3);
					break;
				case 1:
					unit.Kind = (UnitKind)random.Next(1, 18);
					break;
				case 2:
					unit.Variant = (short)random.Next(2);
					break;
				case 3:
					unit.Fuel = random.Next(2) == 0 ? -1 : 100;
					break;
				case 4:
					unit.HP = random.Next(unit.MaxHP + 1);
					break;
				case 5:
					var other = game.Units[random.Next(1, game.MaxUnitId + 1)].Position;
					unit.Position = new WorldPosition(
						other.X + random.Next(-100, 100),
						other.Y + random.Next(-100, 100)
					);
					break;
				case 6:
					unit.PlaneState = UnitState.Parked;
					unit.Weapon = FireKind.Maintenance;
					unit.IsStopping = true;
					break;
				case 7:
					unit.Carrier = random.Next(1, game.MaxUnitId + 1);
					break;
			}
		}
	}

	// Moves the units of a random side and kind to a random point on, or next to, the edge of a rectangle.
	private static void GatherOnAnEdge(Nspw game, Random random)
	{
		var side = (Side)random.Next(1, 3);
		var kind = (UnitKind)random.Next(1, 18);
		var position = new WorldPosition(
			EdgeXs[random.Next(EdgeXs.Length)] + random.Next(-1, 2),
			EdgeYs[random.Next(EdgeYs.Length)] + random.Next(-1, 2)
		);
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var unit = ref game.Units[m];
			if (unit.Side == side && (unit.Kind == kind || random.Next(8) == 0))
			{
				unit.Position = position;
			}
		}
	}

	// Runs decision and its rewrite from the same state and rand() seed, and fails where they differ. Returns the state
	// after decision.
	private static DecisionState CompareDecisions(Nspw game, string label, uint seed)
	{
		var before = Capture(game);

		Restore(game, before);
		game.srand(seed);
		game.RunDecision();
		var emulated = Capture(game);

		Restore(game, before);
		game.srand(seed);
		game.Decide();
		var rewritten = Capture(game);

		Restore(game, before);
		if (Difference(emulated, rewritten) is { } difference)
		{
			Assert.True(false, $"{label}: {difference}");
		}

		return emulated;
	}

	private static bool Changes(DecisionState before, DecisionState after)
	{
		return !after.Units.AsSpan().SequenceEqual(before.Units)
			|| !after.Fleets.AsSpan().SequenceEqual(before.Fleets)
			|| !after.DecisionPoints.SequenceEqual(before.DecisionPoints);
	}

	// For every scenario's victory conditions and events, on states of a few games, as they are and with their units
	// changed at random, at a few times: what decision changes.
	[Theory]
	[InlineData(3)]
	[InlineData(205)]
	[InlineData(301)]
	[InlineData(303)]
	public void DecideIsDecision(int scenario)
	{
		using var run = new SingleplayerRun(scenario);
		var game = run.Game;
		var random = new Random(scenario);
		var results = new Dictionary<GameResult, int>();
		var cases = 0;
		var changed = 0;
		for (var point = 0; point < 2; point++)
		{
			run.Frames(point == 0 ? 2 : 600);
			var original = Capture(game);
			var number = game.ScenarioNumber;
			foreach (var decided in Singleplayer.Functions.Scenarios)
			{
				foreach (var time in new[] { original.BattleTime, 0, 45000, 12300 })
				{
					for (var variant = 0; variant < 8; variant++)
					{
						Restore(game, original);
						game.ScenarioNumber = (short)decided;
						game.BattleTime = time;
						if (decided != scenario)
						{
							for (var i = 0; i < 4; i++)
							{
								game.DecisionPoints[i] = random.Next(game.MaxUnitId + 1);
							}
						}
						else if (decided == 205 && variant % 2 == 1)
						{
							game.DecisionPoints[1] = random.Next(2);
						}

						if (variant is >= 1 and <= 4)
						{
							Mutate(game, random);
						}
						else if (variant >= 5)
						{
							GatherOnAnEdge(game, random);
						}

						if (variant is 4 or 7)
						{
							game.Result = GameResult.JapanWon;
						}

						var before = Capture(game);
						var emulated = CompareDecisions(
							game,
							$"Scenario {decided} on {scenario}'s game, point {point}, time {time}, variant {variant}",
							(uint)random.Next()
						);
						results[emulated.Result] = results.GetValueOrDefault(emulated.Result) + 1;
						if (Changes(before, emulated))
						{
							changed++;
						}

						cases++;
					}
				}
			}

			Restore(game, original);
			game.ScenarioNumber = number;
		}

		_output.WriteLine(
			$"{cases} cases, {changed} changing the units, the fleets or decision_point; results: {string.Join(", ", results.OrderBy(r => r.Key).Select(r => $"{r.Key} {r.Value}"))}"
		);
		Assert.True(changed > 0);
		Assert.True(results.Count >= 5);
	}

	// The planes that decision gives Japan's unit 1 in scenarios 204, 301 and 304, from many rand() seeds, at times when
	// it gives them, with the units of Japan's planes based elsewhere free for them.
	[Theory]
	[InlineData(204)]
	[InlineData(301)]
	[InlineData(304)]
	public void DecideGivesPlanesAsDecision(int scenario)
	{
		using var run = new SingleplayerRun(scenario);
		var game = run.Game;
		run.Frames(2);
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var unit = ref game.Units[m];
			if (unit.Side == Side.Japan && unit.Category == UnitCategory.Plane && unit.Carrier != 1)
			{
				unit.Side = Side.None;
			}
		}

		var added = 0;
		foreach (var time in new[] { 45000, 12300 })
		{
			game.BattleTime = time;
			for (var seed = 0u; seed < 400; seed++)
			{
				var before = Capture(game);
				var emulated = CompareDecisions(
					game,
					$"Scenario {scenario}, time {time}, seed {seed}",
					seed
				);
				if (Changes(before, emulated))
				{
					added++;
				}
			}
		}

		_output.WriteLine($"{added} cases adding a plane");
		Assert.True(added > 0);
	}

	// The rectangles of the scenarios' victory conditions, by scenario: (left, top, right, bottom); scenario 300's
	// bombers must be beyond a corner, and scenario 301's is around decision_point[1] and [2].
	private static (int Left, int Top, int Right, int Bottom) RectangleOf(Nspw game, int scenario)
	{
		return scenario switch
		{
			109 => (7000, -8000, 9000, -6000),
			204 or 306 => (-1040, -160, -800, -240),
			207 => (4600, -3400, 6600, -1400),
			209 => (-80, 3120, 400, 3600),
			300 => (-8400, -6000, -8400, -6000),
			301 => (
				game.DecisionPoints[1] - 80,
				game.DecisionPoints[2] - 80,
				game.DecisionPoints[1] + 80,
				game.DecisionPoints[2] + 80
			),
			_ => throw new ArgumentOutOfRangeException(nameof(scenario)),
		};
	}

	// Each scenario's rectangle, with all the units of a side (or all but its naval bases) on a point on or next to an
	// edge, or in the middle, and the other side's units where they are or gone, as the time runs or once it is up.
	[Fact]
	public void DecideIsDecisionOnTheEdges()
	{
		using var run = new SingleplayerRun(301);
		var game = run.Game;
		run.Frames(2);
		var original = Capture(game);
		var number = game.ScenarioNumber;
		var results = new HashSet<GameResult>();
		foreach (var scenario in new[] { 109, 204, 207, 209, 300, 301, 306 })
		{
			var (left, top, right, bottom) = RectangleOf(game, scenario);
			var xs = new[]
			{
				left - 1,
				left,
				left + 1,
				(left + right) / 2,
				right - 1,
				right,
				right + 1,
			};
			var ys = new[]
			{
				top - 1,
				top,
				top + 1,
				(top + bottom) / 2,
				bottom - 1,
				bottom,
				bottom + 1,
			};
			foreach (var x in xs)
			{
				foreach (var y in ys)
				{
					foreach (var side in new[] { Side.Japan, Side.UnitedStates })
					{
						for (var variant = 0; variant < 8; variant++)
						{
							Restore(game, original);
							game.ScenarioNumber = (short)scenario;
							if (variant % 4 >= 2)
							{
								game.BattleTime = 0;
							}

							for (var m = 1; m <= game.MaxUnitId; m++)
							{
								ref var unit = ref game.Units[m];
								if (
									unit.Side == side
									&& !(variant >= 4 && unit.Kind == UnitKind.NavalBase)
								)
								{
									unit.Position = new WorldPosition(x, y);
								}
								else if (variant % 2 == 1 && unit.IsUsed)
								{
									unit.Side = Side.None;
								}
							}

							// Scenario 207's ships must reach the rectangle once its unit decision_point[0] is gone.
							if (scenario == 207)
							{
								game.Units[game.DecisionPoints[0]].Side = Side.None;
							}

							var emulated = CompareDecisions(
								game,
								$"Scenario {scenario}, ({x},{y}), {side}, variant {variant}",
								1
							);
							results.Add(emulated.Result);
						}
					}
				}
			}
		}

		Restore(game, original);
		game.ScenarioNumber = number;
		_output.WriteLine($"Results: {string.Join(", ", results.Order())}");
		Assert.True(results.Count >= 5);
	}

	// Scenario 205's bomber landing at decision_point[0], with its HP, and then at decision_point[2]: the American
	// bombers parked being serviced, or not, at either, at random, as the time runs or once it is up.
	[Fact]
	public void DecideLandsTheBomberAsDecision()
	{
		using var run = new SingleplayerRun(205);
		var game = run.Game;
		run.Frames(2);
		var random = new Random(205);
		var original = Capture(game);
		var results = new HashSet<GameResult>();
		for (var i = 0; i < 1000; i++)
		{
			Restore(game, original);
			game.DecisionPoints[1] = random.Next(3);
			game.BattleTime = random.Next(2) == 0 ? 0 : original.BattleTime;
			ref var first = ref game.Units[game.DecisionPoints[0]];
			first.HP = random.Next(first.MaxHP + 1);
			for (var m = 1; m <= game.MaxUnitId; m++)
			{
				ref var unit = ref game.Units[m];
				if (
					unit.Side == Side.UnitedStates
					&& unit.Kind == UnitKind.Bomber
					&& random.Next(2) == 0
				)
				{
					unit.PlaneState = random.Next(4) == 0 ? UnitState.Flying : UnitState.Parked;
					unit.Weapon = random.Next(4) == 0 ? FireKind.Bomb : FireKind.Maintenance;
					unit.Carrier = game.DecisionPoints[random.Next(2) * 2];
				}
			}

			results.Add(CompareDecisions(game, $"Case {i}", 1).Result);
		}

		Restore(game, original);
		_output.WriteLine($"Results: {string.Join(", ", results.Order())}");
		Assert.True(results.Count >= 3);
	}

	// Scenario 303's air bases falling to the American infantry, and falling back to Japan when it has gone, from many
	// rand() seeds: American ships become infantry bases next to Japan's air bases, whose planes are in random steps of
	// taking off and landing, and an air base is taken already.
	[Fact]
	public void DecideTakesAirBasesAsDecision()
	{
		using var run = new SingleplayerRun(303);
		var game = run.Game;
		var random = new Random(303);
		var taken = 0;
		var givenBack = 0;
		for (var point = 0; point < 2; point++)
		{
			run.Frames(point == 0 ? 2 : 600);
			var airBases = new List<int>();
			var ships = new List<int>();
			for (var m = 1; m <= game.MaxUnitId; m++)
			{
				ref var unit = ref game.Units[m];
				if (unit.Side == Side.Japan && unit.Kind == UnitKind.AirBase)
				{
					airBases.Add(m);
				}
				else if (
					unit.Side == Side.UnitedStates
					&& unit.Category == UnitCategory.Ship
					&& unit.Kind < UnitKind.AirBase
				)
				{
					ships.Add(m);
				}
			}

			Assert.NotEmpty(airBases);
			Assert.True(ships.Count >= airBases.Count);
			var original = Capture(game);
			for (var seed = 0u; seed < 300; seed++)
			{
				Restore(game, original);
				for (var i = 0; i < airBases.Count; i++)
				{
					var position = game.Units[airBases[i]].Position;
					ref var infantry = ref game.Units[ships[i]];
					infantry.Kind = UnitKind.InfantryBase;
					infantry.Position = new WorldPosition(
						position.X + random.Next(-81, 82),
						position.Y + random.Next(-81, 82)
					);
				}

				for (var m = 1; m <= game.MaxUnitId; m++)
				{
					ref var unit = ref game.Units[m];
					if (airBases.Contains(m))
					{
						unit.PlanesToLaunch = random.Next(3);
						unit.LandingLock = random.Next(3);
						unit.LaunchLock = random.Next(2);
					}
					else if (unit.Category == UnitCategory.Plane && airBases.Contains(unit.Carrier))
					{
						unit.Variant = (short)random.Next(2);
						unit.DeckPhase = random.Next(5);
						unit.Mode = (UnitMode)random.Next(5);
					}
				}

				if (seed % 2 == 1)
				{
					var airBase = airBases[random.Next(airBases.Count)];
					game.Units[airBase].Side = Side.UnitedStates;
					game.DecisionPoints[random.Next(2)] = airBase;
				}

				var before = Capture(game);
				var emulated = CompareDecisions(game, $"Point {point}, seed {seed}", seed);
				for (var i = 0; i < airBases.Count; i++)
				{
					var side = MemoryMarshal
						.Read<Unit>(
							emulated.Units.AsSpan(
								airBases[i] * System.Runtime.CompilerServices.Unsafe.SizeOf<Unit>()
							)
						)
						.Side;
					if (side != game.Units[airBases[i]].Side)
					{
						if (side == Side.UnitedStates)
						{
							taken++;
						}
						else
						{
							givenBack++;
						}
					}
				}
			}

			Restore(game, original);
		}

		_output.WriteLine($"{taken} air bases taken, {givenBack} given back");
		Assert.True(taken > 0);
		Assert.True(givenBack > 0);
	}

	// The globals of 1.22 that only the emulator has: the computer's fleets and the scenarios' state.
	private const uint ComputerGlobalsAddress = 0x4867A0;
	private const int ComputerGlobalsSize = 0x489A28 - 0x4867A0;

	// What cpu_strtgy changes: the units, its order (new_pp[0] and slct_unit[0]), 1.22's own globals, and rand()'s state,
	// by its next number.
	private sealed record StrategyState(
		byte[] Units,
		short MaxUnitId,
		short OrderedUnit,
		WorldPosition Destination,
		short[] Selections,
		byte[] Globals,
		int NextRandom
	);

	private static StrategyState CaptureStrategy(Nspw game)
	{
		var selections = new short[256];
		for (var i = 0; i < 256; i++)
		{
			selections[i] = game.Selections[0][i];
		}

		// The fleets, which the game keeps, then the rest, which only the emulator's memory has.
		byte[] globals =
		[
			.. MemoryMarshal.AsBytes((Span<ComputerFleet>)game.ComputerFleets),
			.. game.Emulator!.At(
				ComputerGlobalsAddress + FleetTableSize,
				ComputerGlobalsSize - FleetTableSize
			),
		];
		return new StrategyState(
			UnitBytes(game),
			game.MaxUnitId,
			game.MoveOrders[0].Unit,
			game.MoveOrders[0].Destination,
			selections,
			globals,
			game.rand()
		);
	}

	// Where the two states first differ.
	private static string? StrategyDifference(StrategyState expected, StrategyState actual)
	{
		var size = System.Runtime.CompilerServices.Unsafe.SizeOf<Unit>();
		var unitByte = expected.Units.AsSpan().CommonPrefixLength(actual.Units);
		if (unitByte < expected.Units.Length)
		{
			return $"unit {unitByte / size}, byte 0x{unitByte % size:X}";
		}

		var globalByte = expected.Globals.AsSpan().CommonPrefixLength(actual.Globals);
		if (globalByte < expected.Globals.Length)
		{
			return $"global 0x{ComputerGlobalsAddress + globalByte:X} (fleet {globalByte / 0x498}, byte 0x{globalByte % 0x498:X})";
		}

		var selection = expected.Selections.AsSpan().CommonPrefixLength(actual.Selections);
		if (selection < expected.Selections.Length)
		{
			return $"slct_unit[0][{selection}] {expected.Selections[selection]}/{actual.Selections[selection]}";
		}

		if (
			expected.OrderedUnit != actual.OrderedUnit
			|| !expected.Destination.Equals(actual.Destination)
		)
		{
			return $"new_pp[0] {expected.OrderedUnit} ({expected.Destination.X},{expected.Destination.Y})/{actual.OrderedUnit} ({actual.Destination.X},{actual.Destination.Y})";
		}

		if (expected.MaxUnitId != actual.MaxUnitId || expected.NextRandom != actual.NextRandom)
		{
			return $"max_unit {expected.MaxUnitId}/{actual.MaxUnitId}, rand {expected.NextRandom}/{actual.NextRandom}";
		}

		return null;
	}

	// Runs cpu_strtgy and its rewrite from the same state, with the emulator's memory and rand() seed, and fails where
	// they differ. Returns the state after cpu_strtgy.
	private static StrategyState CompareStrategies(Nspw game, string label, uint seed)
	{
		var units = UnitBytes(game);
		var maxUnitId = game.MaxUnitId;
		var order = game.MoveOrders[0];
		var selections = game.Selections[0];
		var memory = State122.Save(game);

		void Reset()
		{
			Restore(game, units);
			game.MaxUnitId = maxUnitId;
			game.MoveOrders[0] = order;
			game.Selections[0] = selections;
			State122.Restore(game, memory);
			game.srand(seed);
		}

		Reset();
		game.RunCpuStrategy();
		var emulated = CaptureStrategy(game);

		Reset();
		game.PlanComputerStrategy();
		var rewritten = CaptureStrategy(game);

		Reset();
		if (StrategyDifference(emulated, rewritten) is { } difference)
		{
			Assert.True(false, $"{label}: {difference}");
		}

		return emulated;
	}

	public static TheoryData<int> StrategyScenarios()
	{
		var data = new TheoryData<int>();
		foreach (
			var scenario in new[]
			{
				3,
				4,
				5,
				7,
				8,
				9,
				10,
				100,
				101,
				105,
				200,
				203,
				204,
				205,
				208,
				209,
				301,
				303,
				306,
			}
		)
		{
			data.Add(scenario);
		}

		return data;
	}

	private static ref ComputerFleet FleetOf(Nspw game, int f)
	{
		return ref game.ComputerFleets[f];
	}

	// A random enemy (of the computer) unit, or 0.
	private static short RandomEnemy(Nspw game, Random random)
	{
		var m = random.Next(1, game.MaxUnitId + 1);
		return game.Units[m].IsUsed && game.Units[m].Side != game.CpuSide ? (short)m : (short)0;
	}

	// Changes the state at random in what cpu_strtgy tests: fleet f's state, mode, base, destination and targets; its
	// flagship stopped where it is or moved next to an enemy; the units' HP, ammunition, fuel and targets; the planes'
	// states; and the time left.
	private static void MutateForStrategy(Nspw game, Random random, int f)
	{
		ref var fleet = ref FleetOf(game, f);
		if (random.Next(2) == 0)
		{
			fleet.State = (short)new[] { 0, 1, 10, 50 }[random.Next(4)];
		}

		if (random.Next(2) == 0)
		{
			fleet.Mode = (short)new[] { 0, 1, 50 }[random.Next(3)];
		}

		if (random.Next(4) == 0)
		{
			fleet.Base = (short)random.Next(0, game.MaxUnitId + 1);
		}

		if (random.Next(3) == 0 && RandomEnemy(game, random) is var enemy and not 0)
		{
			fleet.DestinationX = game.Units[enemy].Position.X + random.Next(-2400, 2400);
			fleet.DestinationY = game.Units[enemy].Position.Y + random.Next(-2400, 2400);
		}

		if (random.Next(3) == 0)
		{
			for (var k = 1; k <= 17; k++)
			{
				fleet.Targets[k] = random.Next(3) == 0 ? (short)0 : RandomEnemy(game, random);
			}
		}

		if (fleet.Flagship != 0)
		{
			ref var flagship = ref game.Units[fleet.Flagship];
			if (random.Next(2) == 0)
			{
				flagship.IsStopping = true;
				flagship.PathX[0] = flagship.Position.X;
				flagship.PathY[0] = flagship.Position.Y;
				flagship.PathX[1] = World.Beyond;
				flagship.SupplyTime = random.Next(4) == 0 ? 1 : 0;
			}

			if (random.Next(4) == 0 && RandomEnemy(game, random) is var near and not 0)
			{
				var position = game.Units[near].Position;
				var distance = new[] { 300, 900, 1600, 3200, 4000, 5000, 8000 }[random.Next(7)];
				var angle = random.NextDouble() * 2 * Math.PI;
				flagship.Position = new WorldPosition(
					position.X + distance * Math.Cos(angle),
					position.Y + distance * Math.Sin(angle)
				);
			}
		}

		if (random.Next(3) == 0)
		{
			game.BattleTime = random.Next(0, 140000);
		}

		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			if (random.Next(6) != 0)
			{
				continue;
			}

			ref var unit = ref game.Units[m];
			switch (random.Next(9))
			{
				case 0:
					unit.HP = random.Next(unit.MaxHP + 1);
					break;
				case 1:
					unit.Ammo = random.Next(unit.MaxAmmo + 1);
					break;
				case 2:
					unit.Fuel = random.Next(0, 101);
					break;
				case 3:
					unit.Target = random.Next(2) == 0 ? 0 : RandomEnemy(game, random);
					break;
				case 4:
					unit.PlaneState = random.Next(2) == 0 ? UnitState.Flying : UnitState.Parked;
					unit.IsStopping = unit.PlaneState == UnitState.Parked;
					break;
				case 5:
					unit.Mode = (UnitMode)random.Next(0, 5);
					break;
				case 6:
					unit.Weapon = new[]
					{
						FireKind.Unarmed,
						FireKind.Maintenance,
						FireKind.Bomb,
						FireKind.Torpedo,
					}[random.Next(4)];
					unit.ReloadTime = random.Next(2) == 0 ? 0 : 100;
					break;
				case 7:
					unit.GroupLeader = 0;
					unit.IsFound = random.Next(2) == 0;
					break;
				case 8:
					unit.PathX[0] = unit.Position.X + random.Next(-60, 60);
					unit.PathY[0] = unit.Position.Y + random.Next(-60, 60);
					break;
			}
		}
	}

	private static void PlaceFrom(
		ref Unit unit,
		WorldPosition center,
		double distance,
		Random random
	)
	{
		var angle = random.NextDouble() * 2 * Math.PI;
		unit.Position = new WorldPosition(
			center.X + distance * Math.Cos(angle),
			center.Y + distance * Math.Sin(angle)
		);
	}

	// Sets up fleet f's battle at random, around the thresholds of cpu_strtgy: enemies, visible, at distances from its
	// flagship next to those it compares, as its targets and at the edge of its destination's square; enemy planes in
	// the air and a ground facility near; its members at about 65% of their HP; its planes ready, or arrived at their
	// destination.
	private static void MutateForBattle(Nspw game, Random random, int f)
	{
		ref var fleet = ref FleetOf(game, f);
		if (fleet.Flagship == 0)
		{
			return;
		}

		var center = game.Units[fleet.Flagship].Position;
		var enemies = new List<int>();
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			if (game.Units[m].IsUsed && game.Units[m].Side != game.CpuSide)
			{
				enemies.Add(m);
			}
		}

		if (enemies.Count == 0)
		{
			return;
		}

		var distances = new[]
		{
			500,
			880,
			899.5,
			900.5,
			920,
			999.5,
			1000.5,
			1150,
			1199.5,
			1200.5,
			1699.5,
			1700.5,
			1999.5,
			2000.5,
			2160,
			3500,
			3600,
			4300,
			5200,
			5400,
			6100,
			7400,
			19900,
			20100,
		};
		for (var k = 1; k <= 3; k++)
		{
			var e = enemies[random.Next(enemies.Count)];
			ref var enemy = ref game.Units[e];
			enemy.IsFound = true;
			PlaceFrom(ref enemy, center, distances[random.Next(distances.Length)], random);
			fleet.Targets[k] = (short)e;
		}

		if (random.Next(2) == 0)
		{
			ref var enemy = ref game.Units[fleet.Targets[1]];
			var edge = 2400 + random.Next(-1, 2) + random.NextDouble();
			var along = random.Next(-2400, 2401);
			var sign = random.Next(2) == 0 ? 1 : -1;
			fleet.DestinationX = enemy.Position.X + (random.Next(2) == 0 ? sign * edge : along);
			fleet.DestinationY = enemy.Position.Y + (random.Next(2) == 0 ? sign * edge : along);
		}

		var enemyPlanes = enemies.Where(e => game.Units[e].Category == UnitCategory.Plane).ToList();
		if (random.Next(2) == 0 && enemyPlanes.Count != 0)
		{
			var e = enemyPlanes[random.Next(enemyPlanes.Count)];
			ref var enemy = ref game.Units[e];
			enemy.PlaneState = UnitState.Flying;
			enemy.IsFound = true;
			PlaceFrom(
				ref enemy,
				center,
				new[] { 1000, 3000, 3540, 3560, 4340, 4360, 5300, 6000, 6600, 7000 }[
					random.Next(10)
				],
				random
			);
			fleet.Targets[1] = (short)e;

			// Nothing else near, so that the plane is the target of the flagship's planes.
			fleet.DestinationX = World.Beyond;
			for (var k = 2; k <= 3; k++)
			{
				PlaceFrom(ref game.Units[fleet.Targets[k]], center, 9000, random);
			}
		}

		for (var n = 0; n < 4; n++)
		{
			var e = enemies[random.Next(enemies.Count)];
			ref var enemy = ref game.Units[e];
			if (enemy.Category == UnitCategory.Plane)
			{
				enemy.PlaneState = UnitState.Flying;
				enemy.IsFound = true;
				enemy.Ammo = Math.Max(enemy.Ammo, 1);
				PlaceFrom(ref enemy, center, distances[random.Next(distances.Length)], random);
			}
			else if (enemy.Kind >= UnitKind.AirBase && enemy.Kind <= UnitKind.Fortress)
			{
				PlaceFrom(
					ref enemy,
					center,
					new[] { 1500, 2100, 2300, 2700 }[random.Next(4)],
					random
				);
			}
		}

		var ratio = new[] { 0.6, 0.65, 0.66, 0.7, 1.0 }[random.Next(5)];
		for (var k = 0; k <= 17; k++)
		{
			var m = fleet.Members[k];
			if (m == 0)
			{
				continue;
			}

			ref var member = ref game.Units[m];
			member.HP = (int)(member.MaxHP * ratio);
			if (random.Next(2) == 0)
			{
				member.Target = 0;
				member.Ammo = Math.Max(member.Ammo, 1);
			}
		}

		var readiness = random.Next(3);
		for (var p = 1; p <= game.MaxUnitId; p++)
		{
			ref var plane = ref game.Units[p];
			if (
				plane.Side != game.CpuSide
				|| plane.Category != UnitCategory.Plane
				|| plane.Carrier == 0
			)
			{
				continue;
			}

			switch (readiness == 2 ? random.Next(3) : readiness)
			{
				case 0:
					plane.PlaneState = UnitState.Parked;
					plane.IsStopping = true;
					plane.Ammo = Math.Max(plane.MaxAmmo, 1);
					plane.ReloadTime = 0;
					plane.Weapon = plane.Kind == UnitKind.Fighter ? FireKind.Bullet : FireKind.Bomb;
					break;
				case 1:
					plane.PlaneState = UnitState.Flying;
					plane.IsStopping = false;
					plane.Mode = UnitMode.Move;
					plane.GroupLeader = 0;
					plane.Fuel = 100;
					plane.Target = 0;
					plane.Ammo = Math.Max(plane.Ammo, 1);
					plane.PathX[0] = plane.Position.X;
					plane.PathY[0] = plane.Position.Y;
					break;
			}
		}
	}

	// For each fleet, at a few points of the game, with the state changed at random: what cpu_strtgy changes.
	[Theory]
	[InlineData(3)]
	[InlineData(7)]
	[InlineData(8)]
	[InlineData(10)]
	[InlineData(101)]
	[InlineData(200)]
	[InlineData(204)]
	[InlineData(208)]
	[InlineData(209)]
	[InlineData(303)]
	[InlineData(304)]
	public void PlanComputerStrategyIsCpuStrtgyOnChangedStates(int scenario)
	{
		using var run = new SingleplayerRun(scenario);
		var game = run.Game;
		var random = new Random(scenario);
		var orders = 0;
		var cases = 0;
		for (var point = 0; point < 3; point++)
		{
			run.Frames(point == 0 ? 2 : 600);
			var frame = game.Renderer.FrameCount;
			var time = game.BattleTime;
			var tick = game.Tick;
			var units = UnitBytes(game);
			var memory = State122.Save(game);
			for (var f = 1; f <= 10; f++)
			{
				for (var variant = 0; variant < 40; variant++)
				{
					Restore(game, units);
					State122.Restore(game, memory);
					game.BattleTime = time;
					game.Tick = tick + random.Next(150);
					game.Renderer.FrameCount = frame / 10 * 10 + f - 1;
					MutateForStrategy(game, random, f);
					if (variant % 2 == 1)
					{
						MutateForBattle(game, random, f);
					}

					var emulated = CompareStrategies(
						game,
						$"Scenario {scenario}, point {point}, fleet {f}, variant {variant}",
						(uint)random.Next()
					);
					if (emulated.OrderedUnit != 0)
					{
						orders++;
					}

					cases++;
				}
			}

			Restore(game, units);
			State122.Restore(game, memory);
			game.BattleTime = time;
			game.Tick = tick;
			game.Renderer.FrameCount = frame;
		}

		_output.WriteLine($"{cases} cases, {orders} with an order");
		Assert.True(orders > 0);
	}

	// The air strikes of cpu_strtgy (steps 10 and 11): for each fleet led by a carrier or an air base, its planes all
	// ready, the other enemies far away, and its first target an enemy plane in the air or a ship, moving, at distances
	// next to those it compares, as the time runs or near its end.
	[Theory]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(10)]
	[InlineData(208)]
	[InlineData(209)]
	[InlineData(304)]
	public void PlanComputerStrategyStrikesAsCpuStrtgy(int scenario)
	{
		using var run = new SingleplayerRun(scenario);
		var game = run.Game;
		// Every fleet's turn (FrameCount % 10 + 1) once, so that each has formed up around its flagship: the battle starts
		// at frame 1, as 1.22's does.
		run.Frames(12);
		var random = new Random(scenario);
		var units = UnitBytes(game);
		var memory = State122.Save(game);
		var time = game.BattleTime;
		var frame = game.Renderer.FrameCount;
		var planeOrders = 0;
		var cases = 0;
		for (var f = 1; f <= 10; f++)
		{
			if (
				FleetOf(game, f).Flagship is var flag and not 0
				&& game.Units[flag].Kind
					is UnitKind.Carrier
						or UnitKind.LightCarrier
						or UnitKind.AirBase
			)
			{
				foreach (
					var distance in new[]
					{
						1000,
						3549.5,
						3550.5,
						4349.5,
						4350.5,
						5324.5,
						5325.5,
						6034.5,
						6035.5,
						6524.5,
						6525.5,
						7394.5,
						7395.5,
						19999.5,
						20000.5,
					}
				)
				{
					foreach (
						var (targetsPlane, timeLeft) in new[]
						{
							(true, time),
							(false, time),
							(true, 5000),
							(false, 5000),
						}
					)
					{
						Restore(game, units);
						State122.Restore(game, memory);
						game.Renderer.FrameCount = frame / 10 * 10 + f - 1;
						game.BattleTime = timeLeft;
						ref var fleet = ref FleetOf(game, f);
						fleet.State = 0;
						fleet.Base = 0;
						fleet.Mode = (short)(random.Next(2) == 0 ? 1 : 50);
						fleet.DestinationX = World.Beyond;
						ref var flagship = ref game.Units[flag];
						flagship.IsStopping = false;
						flagship.SupplyTime = 0;
						flagship.PlanesToLaunch = 0;
						var center = flagship.Position;
						var target = 0;
						for (var m = 1; m <= game.MaxUnitId; m++)
						{
							ref var unit = ref game.Units[m];
							if (!unit.IsUsed)
							{
								continue;
							}

							if (unit.Side == game.CpuSide)
							{
								if (unit.Category == UnitCategory.Plane)
								{
									unit.PlaneState = UnitState.Parked;
									unit.IsStopping = true;
									unit.Ammo = Math.Max(unit.MaxAmmo, 1);
									unit.ReloadTime = 0;
									unit.Weapon =
										unit.Kind == UnitKind.Fighter
											? FireKind.Bullet
											: FireKind.Bomb;
								}
							}
							else if (
								target == 0
								&& (unit.Category == UnitCategory.Plane) == targetsPlane
								&& !(
									unit.Kind >= UnitKind.AirBase && unit.Kind <= UnitKind.Fortress
								)
							)
							{
								target = m;
								unit.IsFound = true;
								unit.Speed = new[] { 0.5, 1.0, 1.05, 2.5 }[random.Next(4)];
								unit.Direction = random.Next(360);
								if (targetsPlane)
								{
									unit.PlaneState = UnitState.Flying;
								}

								PlaceFrom(ref unit, center, distance, random);
							}
							else
							{
								PlaceFrom(ref unit, center, 30000, random);
							}
						}

						fleet.Targets[1] = (short)target;
						var emulated = CompareStrategies(
							game,
							$"Fleet {f}, distance {distance}, plane {targetsPlane}, time {timeLeft}",
							(uint)random.Next()
						);
						if (
							emulated.OrderedUnit != 0
							&& game.Units[emulated.OrderedUnit].Category == UnitCategory.Plane
						)
						{
							planeOrders++;
						}

						cases++;
					}
				}
			}
		}

		Restore(game, units);
		State122.Restore(game, memory);
		game.BattleTime = time;
		game.Renderer.FrameCount = frame;
		_output.WriteLine($"{cases} cases, {planeOrders} ordering planes");
		Assert.True(planeOrders > 0);
	}

	// For each fleet, at a few points of the game, from a few rand() seeds: what cpu_strtgy changes.
	[Theory]
	[MemberData(nameof(StrategyScenarios))]
	public void PlanComputerStrategyIsCpuStrtgy(int scenario)
	{
		using var run = new SingleplayerRun(scenario);
		var game = run.Game;
		var orders = 0;
		var cases = 0;
		for (var point = 0; point < 4; point++)
		{
			run.Frames(point == 0 ? 2 : 500);
			var frame = game.Renderer.FrameCount;
			for (var f = 1; f <= 10; f++)
			{
				for (var seed = 0u; seed < 3; seed++)
				{
					game.Renderer.FrameCount = frame / 10 * 10 + f - 1;
					var emulated = CompareStrategies(
						game,
						$"Scenario {scenario}, point {point}, fleet {f}, seed {seed}",
						seed
					);
					if (emulated.OrderedUnit != 0)
					{
						orders++;
					}

					cases++;
				}
			}

			game.Renderer.FrameCount = frame;
		}

		_output.WriteLine($"{cases} cases, {orders} with an order");
		Assert.True(orders > 0);
	}

	// cpu_strtgy's lists of targets (sub_41F230, sub_41F300, sub_41F490, sub_404D80): for each fleet, its list partly
	// filled at random, as they leave the fleet table and rand().
	[Theory]
	[MemberData(nameof(Scenarios))]
	public void TargetListsAreAsTheOriginals(int scenario)
	{
		using var run = new SingleplayerRun(scenario);
		var game = run.Game;
		var random = new Random(scenario);
		var operations = new List<(string Name, Action Original, Action Rewritten)>();
		for (var f = 1; f <= 10; f++)
		{
			var fleet = f;
			operations.Add(
				(
					$"sub_41F300({f})",
					() => game.RunAddNavalBaseTargets(fleet),
					() => game.AddNavalBaseTargets(fleet)
				)
			);
			operations.Add(
				(
					$"sub_41F490({f})",
					() => game.RunAddShipTargets(fleet),
					() => game.AddShipTargets(fleet)
				)
			);
			operations.Add(
				(
					$"sub_404D80({f})",
					() => game.RunShuffleTargets(fleet),
					() => game.ShuffleTargets(fleet)
				)
			);
			for (var kind = UnitKind.Battleship; kind <= UnitKind.Fortress; kind++)
			{
				var k = kind;
				operations.Add(
					(
						$"sub_41F230({f}, {kind})",
						() => game.RunAddTargetsOfKind(fleet, k),
						() => game.AddTargetsOfKind(fleet, k)
					)
				);
			}
		}

		for (var point = 0; point < 2; point++)
		{
			run.Frames(point == 0 ? 2 : 800);
			var memory = State122.Save(game);
			var units = UnitBytes(game);
			foreach (var (name, original, rewritten) in operations)
			{
				for (var variant = 0; variant < 3; variant++)
				{
					State122.Restore(game, memory);
					for (var g = 1; g <= 10; g++)
					{
						ref var fleet = ref FleetOf(game, g);
						var filled = random.Next(0, 19);
						for (var k = 1; k <= 17; k++)
						{
							fleet.Targets[k] =
								k <= filled ? (short)random.Next(0, game.MaxUnitId + 1) : (short)0;
						}
					}

					var before = State122.Save(game);
					var seed = (uint)random.Next();
					game.srand(seed);
					original();
					var expected = (FleetBytes(game), game.rand());
					State122.Restore(game, before);
					game.srand(seed);
					rewritten();
					var actual = (FleetBytes(game), game.rand());
					var differs = expected.Item1.AsSpan().CommonPrefixLength(actual.Item1);
					Assert.True(
						differs == expected.Item1.Length,
						$"{name}, point {point}: fleet {differs / 0x498}, byte 0x{differs % 0x498:X}"
					);
					Assert.True(
						expected.Item2 == actual.Item2,
						$"{name}, point {point}: rand {expected.Item2}/{actual.Item2}"
					);
					Assert.True(
						units.AsSpan().SequenceEqual(UnitBytes(game)),
						$"{name}: the units changed"
					);
				}
			}

			State122.Restore(game, memory);
		}
	}

	// sub_403AE0's formations: for each fleet, its members (and units added as members) in a random order, as they
	// leave the units, slct_unit[0] and the fleet table.
	[Theory]
	[MemberData(nameof(Scenarios))]
	public void SetFormationIsTheOriginal(int scenario)
	{
		using var run = new SingleplayerRun(scenario);
		var game = run.Game;
		var random = new Random(scenario);
		var cases = 0;
		for (var point = 0; point < 2; point++)
		{
			run.Frames(point == 0 ? 2 : 800);
			var memory = State122.Save(game);
			var units = UnitBytes(game);
			var selections = game.Selections[0];
			for (var f = 1; f <= 10; f++)
			{
				for (var variant = 0; variant < 20; variant++)
				{
					State122.Restore(game, memory);
					Restore(game, units);
					ref var fleet = ref FleetOf(game, f);
					var count = random.Next(0, 19);
					for (var k = 0; k < 18; k++)
					{
						fleet.Members[k] =
							k < count ? (short)random.Next(0, game.MaxUnitId + 1) : (short)0;
					}

					var before = State122.Save(game);
					var unitsBefore = UnitBytes(game);
					game.Selections[0] = selections;
					game.RunSetFormation(f);
					var expected = (
						UnitBytes(game),
						FleetBytes(game),
						game.Selections[0].ToBytes()
					);
					State122.Restore(game, before);
					Restore(game, unitsBefore);
					game.Selections[0] = selections;
					game.SetFormation(f);
					var actual = (UnitBytes(game), FleetBytes(game), game.Selections[0].ToBytes());
					var size = System.Runtime.CompilerServices.Unsafe.SizeOf<Unit>();
					var unitByte = expected.Item1.AsSpan().CommonPrefixLength(actual.Item1);
					Assert.True(
						unitByte == expected.Item1.Length,
						$"Fleet {f}, point {point}, variant {variant}: unit {unitByte / size}, byte 0x{unitByte % size:X}"
					);
					var fleetByte = expected.Item2.AsSpan().CommonPrefixLength(actual.Item2);
					Assert.True(
						fleetByte == expected.Item2.Length,
						$"Fleet {f}, point {point}, variant {variant}: fleet {fleetByte / 0x498}, byte 0x{fleetByte % 0x498:X}"
					);
					Assert.True(
						expected.Item3.AsSpan().SequenceEqual(actual.Item3),
						$"Fleet {f}, point {point}, variant {variant}: slct_unit[0]"
					);
					cases++;
				}
			}

			State122.Restore(game, memory);
			Restore(game, units);
			game.Selections[0] = selections;
		}

		Assert.True(cases > 0);
	}

	// sub_414DD0's places of reinforcements: from points on and next to the ships, where it leaves them.
	[Theory]
	[MemberData(nameof(Scenarios))]
	public void PlaceReinforcementIsTheOriginal(int scenario)
	{
		using var run = new SingleplayerRun(scenario);
		var game = run.Game;
		var random = new Random(scenario);
		run.Frames(2);
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			if (!game.Units[m].IsUsed || game.Units[m].Category != UnitCategory.Ship)
			{
				continue;
			}

			for (var variant = 0; variant < 5; variant++)
			{
				var x = game.Units[m].Position.X + random.Next(-50, 51) + random.NextDouble();
				var y = game.Units[m].Position.Y + random.Next(-50, 51) + random.NextDouble();
				var (ex, ey) = (x, y);
				game.RunPlaceReinforcement(ref ex, ref ey);
				var (ax, ay) = (x, y);
				game.PlaceReinforcement(ref ax, ref ay);
				Assert.True(ex == ax && ey == ay, $"From ({x},{y}): ({ex},{ey})/({ax},{ay})");
			}
		}
	}

	// For every ship but the ground facilities, at three points of a game, to its own destination and three random ones,
	// and to random ones with a ground unit other than a base (kinds 13 to 17) moved to the destination or halfway: the units
	// after the emulated route function, and after its rewrite.
	private static void CompareRoutes(
		int scenario,
		Action<Nspw, int> emulatedRoute,
		Action<Nspw, int> rewrittenRoute
	)
	{
		using var run = new SingleplayerRun(scenario);
		var game = run.Game;
		var random = new Random(scenario);
		var cases = 0;
		for (var point = 0; point < 3; point++)
		{
			run.Frames(point == 0 ? 2 : 700);
			for (var m = 1; m <= game.MaxUnitId; m++)
			{
				if (
					!game.Units[m].IsUsed
					|| game.Units[m].Category != UnitCategory.Ship
					|| game.Units[m].Kind >= UnitKind.AirBase
				)
				{
					continue;
				}

				var ground = Enumerable
					.Range(1, game.MaxUnitId)
					.FirstOrDefault(g =>
						game.Units[g].IsUsed && game.Units[g].Kind >= UnitKind.City && g != m
					);
				for (var i = 0; i < (ground != 0 ? 6 : 4); i++)
				{
					var before = UnitBytes(game);
					var x =
						i == 0
							? game.Units[m].PathX[0]
							: game.Units[m].Position.X + random.Next(-4000, 4000);
					var y =
						i == 0
							? game.Units[m].PathY[0]
							: game.Units[m].Position.Y + random.Next(-4000, 4000);

					var groundAt =
						i == 4 ? new WorldPosition(x, y)
						: i == 5
							? new WorldPosition(
								(x + game.Units[m].Position.X) / 2,
								(y + game.Units[m].Position.Y) / 2
							)
						: (WorldPosition?)null;
					if (groundAt is { } at)
						game.Units[ground].Position = at;
					game.Units[m].PathX[0] = x;
					game.Units[m].PathY[0] = y;
					game.Units[m].PathX[1] = World.Beyond;
					emulatedRoute(game, m);
					var emulated = UnitBytes(game);

					Restore(game, before);
					if (groundAt is { } atAgain)
						game.Units[ground].Position = atAgain;
					game.Units[m].PathX[0] = x;
					game.Units[m].PathY[0] = y;
					game.Units[m].PathX[1] = World.Beyond;
					rewrittenRoute(game, m);
					var rewritten = UnitBytes(game);

					Restore(game, before);
					if (!emulated.AsSpan().SequenceEqual(rewritten))
					{
						var path = (byte[] bytes) =>
						{
							var unit = MemoryMarshal.Read<Unit>(
								bytes.AsSpan(
									m * System.Runtime.CompilerServices.Unsafe.SizeOf<Unit>()
								)
							);
							var text = "";
							for (var p = 0; p < 10 && unit.PathX[p] != World.Beyond; p++)
							{
								text += $"({unit.PathX[p]:F1},{unit.PathY[p]:F1}) ";
							}

							return text;
						};
						Assert.True(
							false,
							$"Unit {m} at ({game.Units[m].Position.X:F1},{game.Units[m].Position.Y:F1}) to ({x},{y}): emulated {path(emulated)}, rewritten {path(rewritten)}"
						);
					}

					cases++;
				}
			}
		}

		Assert.True(cases > 0);
	}

	// For each ship and random destinations, at a few points of the game: the units after set_cpu_route.
	[Theory]
	[MemberData(nameof(Scenarios))]
	public void PlanComputerRouteIsSetCpuRoute(int scenario)
	{
		CompareRoutes(
			scenario,
			(game, m) => game.SetCpuRoute(m, 1.0),
			(game, m) => game.PlanComputerRoute(m)
		);
	}

	// set_cpu_route2, which 1.22 runs for a ship that follows a leader, on the same states.
	[Theory]
	[MemberData(nameof(Scenarios))]
	public void PlanFollowerRouteIsSetCpuRoute2(int scenario)
	{
		CompareRoutes(
			scenario,
			(game, m) => game.SetCpuRoute2As122(m),
			(game, m) => game.SetCpuRoute2(m)
		);
	}

	// The port's SetCpuRoute2 with NSPW NET's obstacles, which let ground units through, is not set_cpu_route2: a ground
	// unit on a ship's way shows it.
	[Fact]
	public void NspwNetsObstaclesAreNotSetCpuRoute2s()
	{
		Assert.ThrowsAny<Exception>(() =>
			CompareRoutes(
				203,
				(game, m) => game.SetCpuRoute2As122(m),
				(game, m) =>
				{
					var rules = game.Rules;
					game.Rules = rules with { LastKindRoutesIgnore = UnitKind.Fortress };
					game.SetCpuRoute2(m);
					game.Rules = rules;
				}
			)
		);
	}
}
