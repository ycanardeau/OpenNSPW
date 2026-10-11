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
	private const uint FleetAddress = 0x4867A0;
	private const int FleetTableSize = 11 * 0x498;

	// What decision changes: the units, the result, decision_point, the fleets, and rand()'s state, by its next number.
	private sealed record DecisionState(byte[] Units, GameResult Result, int[] DecisionPoints, int BattleTime, short MaxUnitId, byte[] Fleets, int NextRandom);

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
		return [game.DecisionPoints[0], game.DecisionPoints[1], game.DecisionPoints[2], game.DecisionPoints[3]];
	}

	private static byte[] FleetBytes(Nspw game)
	{
		return game.Emulator!.At(FleetAddress, FleetTableSize).ToArray();
	}

	// The state after decision; rand() is advanced.
	private static DecisionState Capture(Nspw game)
	{
		return new DecisionState(UnitBytes(game), game.Result, DecisionPointsOf(game), game.BattleTime, game.MaxUnitId, FleetBytes(game), game.rand());
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
		state.Fleets.CopyTo(game.Emulator!.At(FleetAddress, FleetTableSize));
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

		if (expected.Result == actual.Result && expected.DecisionPoints.SequenceEqual(actual.DecisionPoints) && expected.BattleTime == actual.BattleTime && expected.MaxUnitId == actual.MaxUnitId && expected.NextRandom == actual.NextRandom)
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
					unit.Hp = random.Next(unit.MaxHp + 1);
					break;
				case 5:
					var other = game.Units[random.Next(1, game.MaxUnitId + 1)].Position;
					unit.Position = new WorldPosition(other.X + random.Next(-100, 100), other.Y + random.Next(-100, 100));
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
		var position = new WorldPosition(EdgeXs[random.Next(EdgeXs.Length)] + random.Next(-1, 2), EdgeYs[random.Next(EdgeYs.Length)] + random.Next(-1, 2));
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
		return !after.Units.AsSpan().SequenceEqual(before.Units) || !after.Fleets.AsSpan().SequenceEqual(before.Fleets) || !after.DecisionPoints.SequenceEqual(before.DecisionPoints);
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
						var emulated = CompareDecisions(game, $"Scenario {decided} on {scenario}'s game, point {point}, time {time}, variant {variant}", (uint)random.Next());
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

		_output.WriteLine($"{cases} cases, {changed} changing the units, the fleets or decision_point; results: {string.Join(", ", results.OrderBy(r => r.Key).Select(r => $"{r.Key} {r.Value}"))}");
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
				var emulated = CompareDecisions(game, $"Scenario {scenario}, time {time}, seed {seed}", seed);
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
			301 => (game.DecisionPoints[1] - 80, game.DecisionPoints[2] - 80, game.DecisionPoints[1] + 80, game.DecisionPoints[2] + 80),
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
			var xs = new[] { left - 1, left, left + 1, (left + right) / 2, right - 1, right, right + 1 };
			var ys = new[] { top - 1, top, top + 1, (top + bottom) / 2, bottom - 1, bottom, bottom + 1 };
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
								if (unit.Side == side && !(variant >= 4 && unit.Kind == UnitKind.NavalBase))
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

							var emulated = CompareDecisions(game, $"Scenario {scenario}, ({x},{y}), {side}, variant {variant}", 1);
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
			first.Hp = random.Next(first.MaxHp + 1);
			for (var m = 1; m <= game.MaxUnitId; m++)
			{
				ref var unit = ref game.Units[m];
				if (unit.Side == Side.UnitedStates && unit.Kind == UnitKind.Bomber && random.Next(2) == 0)
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
				else if (unit.Side == Side.UnitedStates && unit.Category == UnitCategory.Ship && unit.Kind < UnitKind.AirBase)
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
					infantry.Position = new WorldPosition(position.X + random.Next(-81, 82), position.Y + random.Next(-81, 82));
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
					var side = MemoryMarshal.Read<Unit>(emulated.Units.AsSpan(airBases[i] * System.Runtime.CompilerServices.Unsafe.SizeOf<Unit>())).Side;
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

	// For each ship and random destinations, at a few points of the game: the units after set_cpu_route.
	[Theory]
	[MemberData(nameof(Scenarios))]
	public void PlanComputerRouteIsSetCpuRoute(int scenario)
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
				if (!game.Units[m].IsUsed || game.Units[m].Category != UnitCategory.Ship || game.Units[m].Kind >= UnitKind.AirBase)
				{
					continue;
				}

				for (var i = 0; i < 4; i++)
				{
					var before = UnitBytes(game);
					var x = i == 0 ? game.Units[m].PathX[0] : game.Units[m].Position.X + random.Next(-4000, 4000);
					var y = i == 0 ? game.Units[m].PathY[0] : game.Units[m].Position.Y + random.Next(-4000, 4000);

					game.Units[m].PathX[0] = x;
					game.Units[m].PathY[0] = y;
					game.Units[m].PathX[1] = all_head.MAP_RIGHT + 1;
					game.SetCpuRoute(m, 1.0);
					var emulated = UnitBytes(game);

					Restore(game, before);
					game.Units[m].PathX[0] = x;
					game.Units[m].PathY[0] = y;
					game.Units[m].PathX[1] = all_head.MAP_RIGHT + 1;
					game.PlanComputerRoute(m);
					var rewritten = UnitBytes(game);

					Restore(game, before);
					if (!emulated.AsSpan().SequenceEqual(rewritten))
					{
						var path = (byte[] bytes) =>
						{
							var unit = MemoryMarshal.Read<Unit>(bytes.AsSpan(m * System.Runtime.CompilerServices.Unsafe.SizeOf<Unit>()));
							var text = "";
							for (var p = 0; p < 10 && unit.PathX[p] != all_head.MAP_RIGHT + 1; p++)
							{
								text += $"({unit.PathX[p]:F1},{unit.PathY[p]:F1}) ";
							}

							return text;
						};
						Assert.True(false, $"Unit {m} at ({game.Units[m].Position.X:F1},{game.Units[m].Position.Y:F1}) to ({x},{y}): emulated {path(emulated)}, rewritten {path(rewritten)}");
					}

					cases++;
				}
			}
		}

		Assert.True(cases > 0);
	}
}
