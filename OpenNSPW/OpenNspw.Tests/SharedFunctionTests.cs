using System.Runtime.InteropServices;
using OpenNspw.Tests.Lockstep;
using Xunit;

namespace OpenNspw.Tests;

// The port's functions that 1.22 also has (NSPW NET's versions of them) against 1.22's, emulated, on states of
// single-player games: where they do the same, the rewrites of 1.22's code call the port's.
public class SharedFunctionTests
{
	public static TheoryData<int> Scenarios()
	{
		var data = new TheoryData<int>();
		foreach (var scenario in new[] { 3, 8, 10, 204, 208, 301, 304 })
		{
			data.Add(scenario);
		}

		return data;
	}

	private static byte[] UnitBytes(Nspw game)
	{
		return MemoryMarshal.AsBytes((Span<Unit>)game.Units).ToArray();
	}

	// The units, max_unit and rand()'s next number after running the action from the state, the emulator's memory and
	// the rand() seed; the state is restored afterwards.
	private static (byte[] Units, short MaxUnitId, int NextRandom, int Result) RunFrom(
		Nspw game,
		uint seed,
		Func<int> action
	)
	{
		var units = UnitBytes(game);
		var maxUnitId = game.MaxUnitId;
		var memory = State122.Save(game);
		game.srand(seed);
		var result = action();
		var after = (UnitBytes(game), game.MaxUnitId, game.rand(), result);
		units.CopyTo(MemoryMarshal.AsBytes((Span<Unit>)game.Units));
		game.MaxUnitId = maxUnitId;
		State122.Restore(game, memory);
		return after;
	}

	// Fails at the first difference between 1.22's function and the port's from the same state.
	private static void Compare(
		Nspw game,
		string label,
		uint seed,
		Func<int> original,
		Func<int> port
	)
	{
		var expected = RunFrom(game, seed, original);
		var actual = RunFrom(game, seed, port);
		var size = System.Runtime.CompilerServices.Unsafe.SizeOf<Unit>();
		var unitByte = expected.Units.AsSpan().CommonPrefixLength(actual.Units);
		Assert.True(
			unitByte == expected.Units.Length,
			$"{label}: unit {unitByte / size}, byte 0x{unitByte % size:X}"
		);
		Assert.True(
			expected.MaxUnitId == actual.MaxUnitId,
			$"{label}: max_unit {expected.MaxUnitId}/{actual.MaxUnitId}"
		);
		Assert.True(
			expected.Result == actual.Result,
			$"{label}: result {expected.Result}/{actual.Result}"
		);
		Assert.True(
			expected.NextRandom == actual.NextRandom,
			$"{label}: rand {expected.NextRandom}/{actual.NextRandom}"
		);
	}

	private static IEnumerable<Nspw> States(int scenario)
	{
		using var run = new SingleplayerRun(scenario);
		run.Frames(2);
		yield return run.Game;
		run.Frames(800);
		yield return run.Game;
	}

	[Theory]
	[MemberData(nameof(Scenarios))]
	public void CountPlanesInIsPlaneInCV(int scenario)
	{
		foreach (var game in States(scenario))
		{
			for (var m = 0; m <= game.MaxUnitId; m++)
			{
				Compare(
					game,
					$"plane_in_cv({m})",
					1,
					() => game.RunCountPlanesIn(m),
					() => game.CountPlanesIn(m)
				);
			}
		}
	}

	[Theory]
	[MemberData(nameof(Scenarios))]
	public void SetParkingPositionIsSetPosOfParking(int scenario)
	{
		foreach (var game in States(scenario))
		{
			for (var m = 1; m <= game.MaxUnitId; m++)
			{
				if (game.Units[m].Category == UnitCategory.Plane && game.Units[m].Carrier != 0)
				{
					Compare(
						game,
						$"set_pos_of_parking({m})",
						1,
						() =>
						{
							game.RunSetParkingPosition(m);
							return 0;
						},
						() =>
						{
							game.SetParkingPosition(m);
							return 0;
						}
					);
				}
			}
		}
	}

	[Theory]
	[MemberData(nameof(Scenarios))]
	public void SetUnitDataIsSetUnitData(int scenario)
	{
		foreach (var game in States(scenario))
		{
			for (var m = 1; m <= game.MaxUnitId; m++)
			{
				if (game.Units[m].IsUsed)
				{
					Compare(
						game,
						$"set_unit_data({m}), kind {game.Units[m].Kind}, type {game.Units[m].Variant}",
						1,
						() =>
						{
							game.RunSetUnitData(m);
							return 0;
						},
						() =>
						{
							game.SetUnitData(m);
							return 0;
						}
					);
				}
			}
		}
	}

	[Theory]
	[MemberData(nameof(Scenarios))]
	public void AddUnitIsSetNewUnit(int scenario)
	{
		foreach (var game in States(scenario))
		{
			foreach (var side in new[] { Side.Japan, Side.UnitedStates })
			{
				for (var kind = UnitKind.Battleship; kind <= UnitKind.Fortress; kind++)
				{
					var k = kind;
					Compare(
						game,
						$"set_new_unit({side}, {kind})",
						1,
						() => game.RunAddUnit(side, k, 1234.5, -2345.25, 135),
						() => game.AddUnit(side, k, 1234.5, -2345.25, 135)
					);
				}
			}
		}
	}

	[Theory]
	[MemberData(nameof(Scenarios))]
	public void AddPlaneIsSetNewUnitPlane(int scenario)
	{
		foreach (var game in States(scenario))
		{
			for (var m = 1; m <= game.MaxUnitId; m++)
			{
				if (
					!game.Units[m].IsUsed
					|| game.Units[m].Kind
						is not (UnitKind.Carrier or UnitKind.LightCarrier or UnitKind.AirBase)
				)
				{
					continue;
				}

				foreach (var kind in new[] { UnitKind.Fighter, UnitKind.Attacker, UnitKind.Bomber })
				{
					foreach (
						var (planes, arm) in new[]
						{
							(1, FireKind.Unarmed),
							(3, FireKind.Bomb),
							(12, FireKind.Torpedo),
						}
					)
					{
						var side = game.Units[m].Side;
						var carrier = m;
						Compare(
							game,
							$"set_new_unit_plane({side}, {kind}, 0, {m}, {planes}, {arm})",
							1,
							() =>
							{
								game.RunAddPlane(side, kind, 0, carrier, planes, arm);
								return 0;
							},
							() => game.AddPlane(side, kind, 0, carrier, planes, arm) * 0
						);
					}
				}
			}
		}
	}
}
