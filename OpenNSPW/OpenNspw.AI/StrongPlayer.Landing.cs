namespace OpenNspw.AI;

// The strong player's landings: its transports land their cargo where the briefing wants bases, as a player clicks the
// land with a transport selected (ClickLand), the battlefield moved there first with the minimap.
public sealed partial class StrongPlayer
{
	// The frames between a transport's landing orders.
	private const int LandingFrames = 600;

	private readonly Dictionary<int, int> _landingOrdered = [];
	private readonly Dictionary<int, WorldPosition> _landingSeas = [];
	private bool _convoyFormed;
	private int _convoyLeader;

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

	// Each transport of the player's with cargo is given the coast nearest where the briefing wants bases to land it at:
	// the battlefield is moved there, the transport selected, and the land clicked. TrySailToLand sails it there.
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
				_landingSeas[ship] = sea;
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

	// The transports with cargo, and the warships near them, sail together, the first transport leading, the others and
	// the warships around it, once: a transport alone is sunk on the way.
	private bool TryFormConvoy(Nspw game)
	{
		if (_knowledge.Landing is null || _convoyFormed)
			return false;

		_convoyFormed = true;
		var transports = new List<int>();
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var unit = ref game.Units[m];
			if (
				IsOwn(game, m)
				&& unit.Kind == UnitKind.Transport
				&& unit.Ammo > 0
				&& unit.Weapon >= FireKind.CargoNavalBase
				&& !UnitFacts.IsAnchored(ref unit)
			)
				transports.Add(m);
		}

		if (transports.Count == 0)
			return false;

		var leader = transports[0];
		var position = game.Units[leader].Position;
		var members = transports.Skip(1).ToList();
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var ship = ref game.Units[m];
			if (
				IsOwn(game, m)
				&& IsWarship(ship.Kind)
				&& !UnitFacts.IsAnchored(ref ship)
				&& ship.GroupLeader == 0
				&& !_formed.Contains(m)
				&& UnitFacts.Distance(ship.Position, position) <= 2500
			)
				members.Add(m);
		}

		_formed.UnionWith(members.Append(leader));
		_convoyLeader = leader;
		SelectAlone(leader, "forming the convoy");
		foreach (var member in members)
		{
			_clicks.Enqueue(g =>
			{
				if (g.SelectedUnit != leader)
					return GiveUp(g, $"forming the convoy of {leader}: selected {g.SelectedUnit}");
				if (IsOwn(g, member) && g.Selections[1][member] == 0)
					g.Commands.ClickUnit(member);
				return true;
			});
		}

		SendSelected(
			leader,
			new WorldPosition(position.X + 80, position.Y),
			$"leads a convoy of {members.Count} ships to"
		);
		return true;
	}

	// A transport given a coast to land at sails next to it, across the sea, the convoy it leads with it; and so does one
	// that is still far from it and stands still, the convoy's leader having landed its cargo, or the way having been
	// blocked. It lands its cargo once within 400 (SetTransportLandingDestination) and 360 (LandCargo).
	private bool TrySailToLand(Nspw game)
	{
		foreach (var (m, sea) in _landingSeas)
		{
			ref var transport = ref game.Units[m];
			if (!IsOwn(game, m) || transport.Ammo <= 0 || transport.Target != game.MaxUnitId + 1)
			{
				_landingSeas.Remove(m);
				return false;
			}

			var isFollowing =
				transport.GroupLeader != 0 && game.Units[transport.GroupLeader].IsUsed;
			var isLeading = m == _convoyLeader || !isFollowing;
			var isStill = transport.Speed == 0;
			if (
				UnitFacts.Distance(transport.Position, sea) <= 300
				|| (isFollowing && !isStill)
				|| (
					UnitFacts.Distance(UnitFacts.FinalDestination(ref transport), sea) < 150
					&& !isStill
				)
				|| game.Renderer.FrameCount - _landingOrdered.GetValueOrDefault(m, -LandingFrames)
					< (isLeading ? 150 : LandingFrames)
			)
				continue;

			_landingOrdered[m] = game.Renderer.FrameCount;
			SelectAlone(m, "sailing to land");
			return SailSelected(game, m, sea, "sails to land at") || true;
		}

		return false;
	}
}
