namespace OpenNspw.AI;

// What a player knows of the units by looking at them.
internal static class UnitFacts
{
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

	// Whether a deck lets a plane take off now (CanSetDestination): not launching, not locked, not supplying, and above a
	// fifth of its HP (HandleInput).
	public static bool CanLaunch(ref Unit deck) =>
		deck.IsUsed
		&& deck.PlanesToLaunch == 0
		&& deck.LaunchLock == 0
		&& !deck.IsSupplying
		&& deck.HP > deck.MaxHP * 0.2;
}
