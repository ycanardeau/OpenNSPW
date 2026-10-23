namespace OpenNspw.Arena;

// The arena's --strikes: what becomes of the player's attackers and bombers, each time one drops its weapon, turns home
// damaged or is lost, and each hit on an enemy ship, to see how the strikes fare. It reads the whole state: it is for
// the developer, not the player.
internal sealed class StrikeLog
{
	private readonly Dictionary<
		int,
		(int Ammo, int HP, double Fuel, int Target, WorldPosition Position)
	> _planes = [];
	private readonly Dictionary<int, int> _ships = [];

	private static double Distance(WorldPosition a, WorldPosition b) =>
		Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

	// The enemy units of a category nearest a point, within 800, each with its distance.
	private static string Near(Nspw game, WorldPosition position, Side side, UnitCategory category)
	{
		var near = new List<(int Unit, double Distance)>();
		for (var n = 1; n <= game.MaxUnitId; n++)
		{
			ref var other = ref game.Units[n];
			if (
				other.IsUsed
				&& other.Side != side
				&& other.Category == category
				&& !(category == UnitCategory.Plane && other.PlaneState != UnitState.Flying)
				&& Distance(other.Position, position) <= 800
			)
				near.Add((n, Distance(other.Position, position)));
		}

		return string.Join(
			" ",
			near.OrderBy(t => t.Distance)
				.Take(4)
				.Select(t =>
					FormattableString.Invariant(
						$"{game.Units[t.Unit].Kind.ToString()[..2]}{t.Unit}@{t.Distance:F0}"
					)
				)
		);
	}

	public void Write(Nspw game)
	{
		for (var n = 1; n <= game.MaxUnitId; n++)
		{
			ref var unit = ref game.Units[n];
			if (!unit.IsUsed && _planes.Remove(n, out var gone))
			{
				Console.WriteLine(
					FormattableString.Invariant(
						$"STRIKE {game.Renderer.FrameCount} #{n} lost, ammo {gone.Ammo}, hp {gone.HP}, fuel {gone.Fuel:F0}, near [{Near(game, gone.Position, game.LocalSide, UnitCategory.Ship)}] [{Near(game, gone.Position, game.LocalSide, UnitCategory.Plane)}]"
					)
				);
				continue;
			}

			if (
				unit.IsUsed
				&& unit.Side == game.LocalSide
				&& (unit.Kind is UnitKind.Attacker or UnitKind.Bomber)
			)
			{
				var was = _planes.GetValueOrDefault(n);
				if (
					_planes.ContainsKey(n)
					&& was.Ammo > 0
					&& unit.Ammo < was.Ammo
					&& unit.PlaneState == UnitState.Flying
				)
				{
					var why =
						unit.HP <= unit.MaxHP * 0.7 ? $"turned home damaged, hp {unit.HP}"
						: unit.Fuel <= 20 ? "turned home for fuel"
						: "dropped";
					var target =
						was.Target != 0 && game.Units[was.Target].IsUsed
							? FormattableString.Invariant(
								$" at {game.Units[was.Target].Kind} {was.Target} {Distance(game.Units[was.Target].Position, unit.Position):F0} away"
							)
							: "";
					Console.WriteLine(
						FormattableString.Invariant(
							$"STRIKE {game.Renderer.FrameCount} #{n} {unit.Weapon} {why}{target}, near [{Near(game, unit.Position, game.LocalSide, UnitCategory.Ship)}] [{Near(game, unit.Position, game.LocalSide, UnitCategory.Plane)}]"
						)
					);
				}

				_planes[n] = (unit.Ammo, unit.HP, unit.Fuel, unit.Target, unit.Position);
			}
			else if (
				unit.IsUsed
				&& unit.Side != game.LocalSide
				&& unit.Category == UnitCategory.Ship
			)
			{
				if (_ships.TryGetValue(n, out var hp) && unit.HP < hp)
					Console.WriteLine(
						$"STRIKE {game.Renderer.FrameCount} {unit.Kind} {n} hit, {hp} -> {unit.HP}/{unit.MaxHP}"
					);
				_ships[n] = unit.HP;
			}
		}
	}
}
