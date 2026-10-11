using System.Runtime.InteropServices;
using OpenNspw.Tests.Lockstep;
using Xunit;

namespace OpenNspw.Tests;

// The rewrites of NSPW 1.22's computer player (ComputerPlayer.cs) against the emulated originals, on states of
// single-player games (docs/ComputerPlayer.md).
public class ComputerPlayerTests
{
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
