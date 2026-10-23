namespace OpenNspw.AI;

// Routes across the sea for the player's ships, which the game does not plan around the land (only the computer's get
// set_cpu_route): a player clicks waypoints around the islands instead. The routes are planned on the tiles of the
// terrain, as the ships see them (IsLandAt), keeping off the coasts where the sea allows, and cut to the fewest
// waypoints a ship can sail between in straight lines.
public sealed class SeaRoutes(Nspw game)
{
	private const int Size = 256;

	// The extra cost of a tile next to the land, so that the routes keep off the coasts.
	private const double CoastCost = 2;

	// The waypoints of a route, at most, as a player clicks them.
	private const int MaxWaypoints = 16;

	private readonly Nspw _game = game;
	private bool[,]? _isSea;
	private HashSet<(int, int)> _blocked = [];

	private static readonly (int Dx, int Dy)[] Neighbors =
	[
		(1, 0),
		(-1, 0),
		(0, 1),
		(0, -1),
		(1, 1),
		(1, -1),
		(-1, 1),
		(-1, -1),
	];

	private int TileWidth => _game.Renderer.Sprites[SpriteId.MapTiles].Size.Width;

	private int TileHeight => _game.Renderer.Sprites[SpriteId.MapTiles].Size.Height;

	// The tile a ship at a point is on, as IsLandAt finds it: the point plus half a unit's sprite.
	private (int Column, int Row) TileOf(WorldPosition position)
	{
		ref var units = ref _game.Renderer.Sprites[SpriteId.JapanUnits];
		return (
			(int)((position.X + (units.Size.Width / 2) - World.Left) / TileWidth),
			(int)((World.Top - position.Y + (units.Size.Height / 2)) / TileHeight)
		);
	}

	// The point a ship is at the middle of a tile.
	private WorldPosition MiddleOf(int column, int row)
	{
		ref var units = ref _game.Renderer.Sprites[SpriteId.JapanUnits];
		return new WorldPosition(
			World.Left + (column * TileWidth) + (TileWidth / 2) - (units.Size.Width / 2),
			World.Top - (row * TileHeight) - (TileHeight / 2) + (units.Size.Height / 2)
		);
	}

	private bool IsSea(int column, int row)
	{
		_isSea ??= ReadSea();
		return column is >= 0 and < Size
			&& row is >= 0 and < Size
			&& _isSea[column, row]
			&& !_blocked.Contains((column, row));
	}

	// The tiles that a ship at a point keeps other ships out of: those its rectangle, the size of a unit's sprite, and
	// a half more, touches (IsShipAt).
	private IEnumerable<(int, int)> TilesUnder(WorldPosition position)
	{
		ref var units = ref _game.Renderer.Sprites[SpriteId.JapanUnits];
		var halfWidth = units.Size.Width * 0.75;
		var halfHeight = units.Size.Height * 0.75;
		var (left, top) = TileOf(
			new WorldPosition(position.X - halfWidth, position.Y + halfHeight)
		);
		var (right, bottom) = TileOf(
			new WorldPosition(position.X + halfWidth, position.Y - halfHeight)
		);
		for (var row = top; row <= bottom; row++)
		{
			for (var column = left; column <= right; column++)
			{
				yield return (column, row);
			}
		}
	}

	private bool[,] ReadSea()
	{
		var sea = new bool[Size, Size];
		for (var row = 0; row < Size; row++)
		{
			for (var column = 0; column < Size; column++)
			{
				sea[column, row] = _game.MapTiles[row][column] == 0;
			}
		}

		return sea;
	}

	private bool IsCoast(int column, int row) =>
		Neighbors.Any(n => !IsSea(column + n.Dx, row + n.Dy));

	// Whether a ship sails clear from one point to another in a straight line: every tile on the way is sea, and so are
	// those 30 to either side, so that the way does not graze the coast.
	private bool IsClear(WorldPosition from, WorldPosition to)
	{
		var length = UnitFacts.Distance(from, to);
		var steps = (int)(length / 10) + 1;
		var (sideX, sideY) =
			length == 0
				? (0.0, 0.0)
				: (-(to.Y - from.Y) / length * 30, (to.X - from.X) / length * 30);
		for (var i = 0; i <= steps; i++)
		{
			var x = from.X + ((to.X - from.X) * i / steps);
			var y = from.Y + ((to.Y - from.Y) * i / steps);
			foreach (var side in new[] { 0, 1, -1 })
			{
				var (column, row) = TileOf(
					new WorldPosition(x + (side * sideX), y + (side * sideY))
				);
				if (!IsSea(column, row))
					return false;
			}
		}

		return true;
	}

	// The sea tile nearest a tile, within 6, or null.
	private (int Column, int Row)? NearestSea(int column, int row)
	{
		for (var radius = 0; radius <= 6; radius++)
		{
			for (var dy = -radius; dy <= radius; dy++)
			{
				for (var dx = -radius; dx <= radius; dx++)
				{
					if (
						Math.Max(Math.Abs(dx), Math.Abs(dy)) == radius
						&& IsSea(column + dx, row + dy)
					)
						return (column + dx, row + dy);
				}
			}
		}

		return null;
	}

	// The tiles of the cheapest way across the sea from one tile to another (A*, eight ways, no corner cut across the
	// land), or null if the sea does not join them.
	private List<(int Column, int Row)>? FindTiles(
		(int Column, int Row) start,
		(int Column, int Row) goal
	)
	{
		var cost = new Dictionary<(int, int), double> { [start] = 0 };
		var from = new Dictionary<(int, int), (int, int)>();
		var open = new PriorityQueue<(int Column, int Row), double>();
		open.Enqueue(start, 0);
		while (open.TryDequeue(out var tile, out _))
		{
			if (tile == goal)
			{
				var tiles = new List<(int, int)> { tile };
				while (from.TryGetValue(tile, out var previous))
				{
					tile = previous;
					tiles.Add(tile);
				}

				tiles.Reverse();
				return tiles;
			}

			foreach (var (dx, dy) in Neighbors)
			{
				var next = (tile.Column + dx, tile.Row + dy);
				if (
					!IsSea(next.Item1, next.Item2)
					|| (
						dx != 0
						&& dy != 0
						&& (
							!IsSea(tile.Column + dx, tile.Row) || !IsSea(tile.Column, tile.Row + dy)
						)
					)
				)
					continue;

				var step =
					(dx != 0 && dy != 0 ? 1.414 : 1)
					+ (IsCoast(next.Item1, next.Item2) ? CoastCost : 0);
				var nextCost = cost[tile] + step;
				if (cost.TryGetValue(next, out var known) && known <= nextCost)
					continue;

				cost[next] = nextCost;
				from[next] = tile;
				var dxGoal = Math.Abs(goal.Column - next.Item1);
				var dyGoal = Math.Abs(goal.Row - next.Item2);
				open.Enqueue(
					next,
					nextCost + Math.Max(dxGoal, dyGoal) + (0.414 * Math.Min(dxGoal, dyGoal))
				);
			}
		}

		return null;
	}

	// The waypoints of a route for a ship from a point to another across the sea, around the ships that do not move out
	// of the way (`obstacles`), the last one the destination, or the sea nearest it if it is land; each leg clear in a
	// straight line. Null if the sea does not join them.
	public List<WorldPosition>? Route(
		WorldPosition from,
		WorldPosition to,
		IEnumerable<WorldPosition> obstacles
	)
	{
		var startTiles = TilesUnder(from).ToHashSet();
		_blocked = [.. obstacles.SelectMany(TilesUnder).Where(t => !startTiles.Contains(t))];
		if (IsClear(from, to))
			return [to];

		var startTile = TileOf(from);
		var start = IsSea(startTile.Column, startTile.Row)
			? startTile
			: NearestSea(startTile.Column, startTile.Row);
		var goalTile = TileOf(to);
		var isGoalSea = IsSea(goalTile.Column, goalTile.Row);
		var goal = isGoalSea ? goalTile : NearestSea(goalTile.Column, goalTile.Row);
		if (start is not { } s || goal is not { } g || FindTiles(s, g) is not { } tiles)
			return null;

		var points = tiles.Select(t => MiddleOf(t.Column, t.Row)).ToList();
		points[^1] = isGoalSea ? to : points[^1];
		var waypoints = new List<WorldPosition>();
		var current = from;
		var index = 0;
		while (index < points.Count - 1 && waypoints.Count < MaxWaypoints - 1)
		{
			var next = index + 1;
			for (var candidate = points.Count - 1; candidate > index + 1; candidate--)
			{
				if (IsClear(current, points[candidate]))
				{
					next = candidate;
					break;
				}
			}

			current = points[next];
			waypoints.Add(current);
			index = next;
		}

		if (waypoints.Count == 0 || !waypoints[^1].Equals(points[^1]))
			waypoints.Add(points[^1]);

		return waypoints;
	}
}
