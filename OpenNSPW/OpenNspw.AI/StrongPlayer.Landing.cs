namespace OpenNspw.AI;

// The strong player's landings: its transports land their cargo where the briefing wants bases, as a player clicks the
// land with a transport selected (ClickLand), the battlefield moved there first with the minimap.
public sealed partial class StrongPlayer
{
	// The frames between a transport's landing orders.
	private const int LandingFrames = 600;

	private readonly Dictionary<int, int> _landingOrdered = [];

	private static bool IsLandable(ushort tile) => tile is 2 or 5 or 7 or 8 or 9;

	// The point of the world that stands for a tile of the terrain, as a landing point clicked there is (AimLanding).
	private static WorldPosition TilePosition(int column, int row) =>
		new(World.Left + (column * 80), World.Top - (row * 80));

	// The landable tile on the coast nearest a point, at least 160 from the points other transports land at, with a point
	// of the sea next to it, within 240, for the transport to sail to; or null.
	private static (WorldPosition Land, WorldPosition Sea)? LandingSite(
		Nspw game,
		WorldPosition near,
		List<WorldPosition> taken
	)
	{
		var center = ((int)((near.X - World.Left) / 80), (int)((World.Top - near.Y) / 80));
		(WorldPosition Land, WorldPosition Sea)? best = null;
		var bestDistance = double.MaxValue;
		for (var row = center.Item2 - 12; row <= center.Item2 + 12; row++)
		{
			for (var column = center.Item1 - 12; column <= center.Item1 + 12; column++)
			{
				if (
					row is < 0 or >= 256
					|| column is < 0 or >= 256
					|| !IsLandable(game.MapTiles[row][column])
				)
					continue;

				var land = TilePosition(column, row);
				var distance = UnitFacts.Distance(land, near);
				if (distance >= bestDistance || taken.Any(t => UnitFacts.Distance(t, land) < 160))
					continue;

				foreach (
					var (dx, dy) in new[]
					{
						(1, 0),
						(-1, 0),
						(0, 1),
						(0, -1),
						(1, 1),
						(1, -1),
						(-1, 1),
						(-1, -1),
						(2, 0),
						(-2, 0),
						(0, 2),
						(0, -2),
					}
				)
				{
					var sea = TilePosition(column + dx, row + dy);
					if (UnitFacts.IsSea(game, sea) && UnitFacts.Distance(sea, land) <= 240)
					{
						best = (land, sea);
						bestDistance = distance;
						break;
					}
				}
			}
		}

		return best;
	}

	// The minimap's cell that shows a point near the middle of the battlefield (unit_info_cont.cpp).
	private static (int Column, int Row) MinimapCellShowing(WorldPosition position)
	{
		var left = Math.Clamp(
			position.X - (all_head.CMBT_WIDTH / 2),
			World.Left,
			World.Right - all_head.CMBT_WIDTH
		);
		var top = Math.Clamp(
			position.Y + (all_head.CMBT_HEIGHT / 2),
			World.Bottom + all_head.CMBT_HEIGHT,
			World.Top
		);
		return ((int)((left - World.Left) / 80), (int)((World.Top - top) / 80));
	}

	// Each transport of the player's with cargo lands it on the coast nearest where the briefing wants bases: the
	// battlefield is moved there, the transport selected, the land clicked, and the transport sails next to it, where it
	// lands its cargo once within 400 (SetTransportLandingDestination) and 360 (LandCargo).
	private bool TryLand(Nspw game)
	{
		if (_knowledge.Landing is not { } place)
			return false;

		var taken = new List<WorldPosition>();
		for (var n = 1; n <= game.MaxUnitId; n++)
		{
			ref var other = ref game.Units[n];
			if (
				IsOwn(game, n)
				&& other.Kind == UnitKind.Transport
				&& other.Target == game.MaxUnitId + 1
			)
				taken.Add(new WorldPosition(other.LandingX, other.LandingY));
		}

		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var transport = ref game.Units[m];
			if (
				!IsOwn(game, m)
				|| transport.Kind != UnitKind.Transport
				|| transport.Ammo <= 0
				|| transport.Weapon < FireKind.CargoNavalBase
				|| transport.Target == game.MaxUnitId + 1
				|| transport.IsSupplying
				|| game.Renderer.FrameCount - _landingOrdered.GetValueOrDefault(m, -LandingFrames)
					< LandingFrames
			)
				continue;

			_landingOrdered[m] = game.Renderer.FrameCount;
			if (LandingSite(game, place.Center, taken) is not var (land, sea))
				continue;

			var ship = m;
			var (column, row) = MinimapCellShowing(land);
			Deselect();
			_clicks.Enqueue(g => g.Commands.ShowMinimapCell(column, row, isPressed: true));
			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit != 0 || !IsOwn(g, ship))
					return GiveUp(g, $"landing {ship}: selected {g.SelectedUnit}");
				g.Commands.ClickUnit(ship);
				return true;
			});
			_clicks.Enqueue(g =>
			{
				var x = (int)(land.X - g.CameraPosition.X);
				var y = (int)(g.CameraPosition.Y - land.Y);
				if (
					g.SelectedUnit != ship
					|| x is < 0 or >= all_head.CMBT_WIDTH - 1
					|| y is < 0 or >= all_head.CMBT_HEIGHT
				)
					return GiveUp(
						g,
						$"landing {ship}: selected {g.SelectedUnit}, at ({x}, {y}) of the battlefield"
					);
				g.Commands.ClickLand(x, y);
				return true;
			});
			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit != ship || g.Units[ship].Target != g.MaxUnitId + 1)
					return GiveUp(
						g,
						$"landing {ship}: selected {g.SelectedUnit}, target {g.Units[ship].Target}"
					);
				g.Commands.SetDestination(sea);
				Log?.Invoke(
					$"{g.Renderer.FrameCount}: Transport {ship} lands {g.Units[ship].Weapon} at ({land.X:F0}, {land.Y:F0})"
				);
				return true;
			});
			Deselect();
			return true;
		}

		return false;
	}
}
