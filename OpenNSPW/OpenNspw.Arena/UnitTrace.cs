namespace OpenNspw.Arena;

// The arena's --trace: units written every few frames, from a frame to another, with the enemy units near each, to see
// what happens to them. It reads the whole state, hidden units too: it is for the developer, not the player.
internal sealed class UnitTrace(IReadOnlyList<int> units, int from, int to, int every)
{
	private const double NearDistance = 800;

	private readonly IReadOnlyList<int> _units = units;
	private readonly int _from = from;
	private readonly int _to = to;
	private readonly int _every = every;

	private static double Distance(WorldPosition a, WorldPosition b) =>
		Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

	private static string Describe(Nspw game, int m)
	{
		ref var unit = ref game.Units[m];
		if (!unit.IsUsed)
			return $"#{m} gone";

		var near = new List<string>();
		for (var n = 1; n <= game.MaxUnitId; n++)
		{
			ref var other = ref game.Units[n];
			var distance = Distance(other.Position, unit.Position);
			if (
				other.IsUsed
				&& other.Side != unit.Side
				&& distance <= NearDistance
				&& !(other.Category == UnitCategory.Plane && other.PlaneState == UnitState.Parked)
			)
				near.Add(
					FormattableString.Invariant($"{other.Kind.ToString()[..2]}{n}@{distance:F0}")
				);
		}

		return FormattableString.Invariant(
			$"#{m} {unit.Kind.ToString()[..2]} ({unit.Position.X:F0},{unit.Position.Y:F0}) dir {unit.Direction:F0} spd {unit.Speed:F2}/{unit.MaxSpeed:F2}{(unit.IsStopping ? " stop" : "")} hp {unit.HP} fuel {unit.Fuel:F0} {unit.Weapon} {unit.Ammo} T{unit.Target} L{unit.GroupLeader} {unit.Mode} path ({unit.PathX[0]:F0},{unit.PathY[0]:F0}) em {unit.EmergencyFlags[0]} near [{string.Join(" ", near)}]"
		);
	}

	public void Write(Nspw game)
	{
		if (
			game.Renderer.FrameCount < _from
			|| game.Renderer.FrameCount > _to
			|| game.Renderer.FrameCount % _every != 0
		)
			return;

		foreach (var m in _units)
		{
			Console.WriteLine($"TRACE {game.Renderer.FrameCount} {Describe(game, m)}");
		}
	}

	// From the arena's options: --trace 43,44 [--trace-from 1000] [--trace-to 2000] [--trace-every 10].
	public static UnitTrace? FromOptions(Func<string, string, string> option)
	{
		var units = option("--trace", "");
		if (units.Length == 0)
			return null;

		return new UnitTrace(
			[
				.. units
					.Split(
						',',
						StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
					)
					.Select(int.Parse),
			],
			int.Parse(option("--trace-from", "0")),
			int.Parse(option("--trace-to", $"{int.MaxValue}")),
			int.Parse(option("--trace-every", "10"))
		);
	}
}
