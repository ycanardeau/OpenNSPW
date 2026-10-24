namespace OpenNspw.AI;

// What a player knows of the units by looking at them.
internal static class UnitFacts
{
	// The battle's time in frames of speed 1, two ticks each, from 1: the frame count at speed 1, where a frame runs two
	// ticks, and the game's own time at any other, where a frame runs the speed and one more (TryStartSingleplayerFrame):
	// the player thinks and waits by it, so that it does as much in a minute of the battle at any speed.
	public static int Now(Nspw game) => (game.Tick / 2) + 1;

	public static bool IsBase(UnitKind kind) =>
		kind is >= UnitKind.AirBase and <= UnitKind.Fortress;

	// Ground units: the bases that stand on land, but the naval base, which is in the water.
	public static bool IsGround(UnitKind kind) =>
		kind
			is UnitKind.AirBase
				or UnitKind.City
				or (>= UnitKind.InfantryBase and <= UnitKind.Fortress);

	// Carriers, light carriers and air bases, which planes take off from and land on.
	public static bool IsDeck(UnitKind kind) =>
		kind is UnitKind.Carrier or UnitKind.LightCarrier or UnitKind.AirBase;

	public static bool IsStrikePlane(UnitKind kind) => kind is UnitKind.Attacker or UnitKind.Bomber;

	// Whether a ship is anchored for the whole battle, as some scenarios set ships up (fuel -1): it never moves, and
	// cannot be refuelled.
	public static bool IsAnchored(ref Unit unit) =>
		unit.Category == UnitCategory.Ship && unit.Fuel < 0;

	// The last point of a unit's path, or its position if it has none.
	public static WorldPosition FinalDestination(ref Unit unit)
	{
		if (unit.PathX[0] == World.Right + 1)
			return unit.Position;

		var n = 0;
		while (n < 63 && unit.PathX[n + 1] != World.Right + 1)
		{
			n++;
		}

		return new WorldPosition(unit.PathX[n], unit.PathY[n]);
	}

	// Whether a ship can be at a point: on the map, and on a tile of sea, as the ships see the terrain (IsLandAt).
	public static bool IsSea(Nspw game, WorldPosition position)
	{
		if (
			position.Y > World.Top
			|| position.Y < World.Bottom
			|| position.X < World.Left
			|| position.X > World.Right
		)
			return false;

		ref var units = ref game.Renderer.Sprites[SpriteId.JapanUnits];
		ref var tiles = ref game.Renderer.Sprites[SpriteId.MapTiles];
		var column = (int)((position.X + (units.Size.Width / 2) - World.Left) / tiles.Size.Width);
		var row = (int)((World.Top - position.Y + (units.Size.Height / 2)) / tiles.Size.Height);
		return column is >= 0 and < 256 && row is >= 0 and < 256 && game.MapTiles[row][column] == 0;
	}

	public static double Distance(WorldPosition a, WorldPosition b)
	{
		var dx = a.X - b.X;
		var dy = a.Y - b.Y;
		return Math.Sqrt((dx * dx) + (dy * dy));
	}

	// Whether a parked plane can be clicked: on its deck and stopped, as the game takes a click on it (CanClickUnit).
	public static bool IsParked(ref Unit unit) =>
		unit.IsUsed
		&& unit.Category == UnitCategory.Plane
		&& unit.PlaneState == UnitState.Parked
		&& unit.IsStopping;

	// Whether a parked plane can take off now with its weapon: armed with ammunition and not being serviced.
	public static bool IsReady(ref Unit unit) =>
		IsParked(ref unit)
		&& unit.ReloadTime == 0
		&& unit.Ammo > 0
		&& (
			unit.Kind == UnitKind.Fighter
				? unit.Weapon == FireKind.Bullet
				: unit.Weapon is FireKind.Torpedo or FireKind.Bomb
		);

	// Whether a deck lets a plane take off now (CanSetDestination): not launching, not locked, not supplying, and above
	// a fifth of its HP (HandleInput).
	public static bool CanLaunch(ref Unit deck) =>
		deck.IsUsed
		&& deck.PlanesToLaunch == 0
		&& deck.LaunchLock == 0
		&& !deck.IsSupplying
		&& deck.HP > deck.MaxHP * 0.2;
}
