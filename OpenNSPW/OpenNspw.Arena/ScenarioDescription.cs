using System.Text;

namespace OpenNspw.Arena;

// What a scenario sets up, as the arena's --describe writes it: the sides, the time, and each side's units, the planes
// grouped by the carrier or base they belong to.
internal static class ScenarioDescription
{
	private static string Planes(Nspw game, int carrier)
	{
		var planes = new List<string>();
		for (var n = 1; n <= game.MaxUnitId; n++)
		{
			ref var plane = ref game.Units[n];
			if (plane.IsUsed && plane.Category == UnitCategory.Plane && plane.Carrier == carrier)
				planes.Add(
					$"{plane.Kind}({plane.Weapon}{(plane.PlaneState == UnitState.Flying ? ", flying" : "")})"
				);
		}

		return string.Join(", ", planes.GroupBy(p => p).Select(g => $"{g.Count()} {g.Key}"));
	}

	private static void DescribeSide(StringBuilder text, Nspw game, Side side)
	{
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var unit = ref game.Units[m];
			if (!unit.IsUsed || unit.Side != side || unit.Category == UnitCategory.Plane)
				continue;

			text.Append(
				FormattableString.Invariant(
					$"  #{m} {unit.Kind} at ({unit.Position.X:F0}, {unit.Position.Y:F0}), hp {unit.HP}/{unit.MaxHP}, {unit.Weapon} {unit.Ammo}"
				)
			);
			if (unit.Side != game.LocalSide)
				text.Append(unit.IsFound ? ", found" : ", not found");
			if (Planes(game, m) is { Length: > 0 } planes)
				text.Append($"; planes: {planes}");
			text.AppendLine();
		}

		var unbased = new List<string>();
		for (var n = 1; n <= game.MaxUnitId; n++)
		{
			ref var plane = ref game.Units[n];
			if (
				plane.IsUsed
				&& plane.Side == side
				&& plane.Category == UnitCategory.Plane
				&& !game.Units[plane.Carrier].IsUsed
			)
				unbased.Add(
					FormattableString.Invariant(
						$"#{n} {plane.Kind}({plane.Weapon}) at ({plane.Position.X:F0}, {plane.Position.Y:F0})"
					)
				);
		}

		if (unbased.Count > 0)
			text.AppendLine($"  planes with no base: {string.Join(", ", unbased)}");
	}

	public static string Describe(Nspw game)
	{
		var text = new StringBuilder();
		text.AppendLine(
			$"Scenario {game.ScenarioNumber}: the player plays {game.LocalSide}, the computer {game.CpuSide}; {game.BattleTime} ticks."
		);
		foreach (
			var side in new[]
			{
				game.LocalSide,
				game.LocalSide == Side.Japan ? Side.UnitedStates : Side.Japan,
			}
		)
		{
			text.AppendLine($" {side}{(side == game.LocalSide ? " (player)" : "")}:");
			DescribeSide(text, game, side);
		}

		return text.ToString();
	}
}
