namespace OpenNspw.AI;

// A player that does not think: it sends its ships one after another at the nearest enemy found, as the tests' Monkey
// sends its ships, and gives no other order. The baseline of what charging gives.
public sealed class RushPlayer : IPlayer
{
	private readonly ClickQueue _clicks = new();
	private int _lastShip;

	public string Name => "rush";

	// Whether unit m is a ship of the player that can be ordered to move: not a base, not supplying.
	private static bool IsMovableShip(Nspw game, int m)
	{
		ref var unit = ref game.Units[m];
		return unit.IsUsed
			&& unit.Side == game.LocalSide
			&& unit.Category == UnitCategory.Ship
			&& !(unit.Kind >= UnitKind.AirBase && unit.Kind <= UnitKind.Fortress)
			&& !unit.IsSupplying
			&& unit.HP > 0;
	}

	// The next of the player's ships that can be ordered, after the last one ordered, or 0.
	private int NextShip(Nspw game)
	{
		for (var i = 1; i <= game.USA_SHIP_END; i++)
		{
			var m = ((_lastShip + i - 1) % game.USA_SHIP_END) + 1;
			if (IsMovableShip(game, m))
			{
				_lastShip = m;
				return m;
			}
		}

		return 0;
	}

	// The position of the enemy found nearest to unit m, or null if none is found.
	private static WorldPosition? NearestEnemyFound(Nspw game, int m)
	{
		var position = game.Units[m].Position;
		WorldPosition? best = null;
		var bestDistance = double.MaxValue;
		for (var n = 1; n <= game.MaxUnitId; n++)
		{
			ref var other = ref game.Units[n];
			if (other.IsUsed && other.Side != game.LocalSide && other.IsFound)
			{
				var distance =
					Math.Abs(other.Position.X - position.X)
					+ Math.Abs(other.Position.Y - position.Y);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = other.Position;
				}
			}
		}

		return best;
	}

	// Deselects, selects ship m, and gives it the nearest enemy found as its destination.
	private void Order(int m)
	{
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit != 0)
				g.Commands.RightClick();
		});
		_clicks.Enqueue(g => g.Commands.ClickUnit(m));
		_clicks.Enqueue(g =>
		{
			if (g.SelectedUnit == m && NearestEnemyFound(g, m) is { } target)
				g.Commands.SetDestination(target);
		});
	}

	public void Act(Nspw game)
	{
		if (_clicks.IsEmpty(game))
		{
			var m = NextShip(game);
			if (m != 0 && NearestEnemyFound(game, m) is not null)
				Order(m);

			_clicks.Wait(20);
		}

		_clicks.Act(game);
	}
}
