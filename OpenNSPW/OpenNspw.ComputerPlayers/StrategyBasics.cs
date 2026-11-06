namespace OpenNspw.ComputerPlayers;

// Where cpu_strtgy goes after a step: on to the next, to the reinforcements of scenario 304 and the order (when the
// fleet has given its order, or has none to give), or out, without an order.
public enum StrategyFlow
{
	Next,
	Order,
	Return,
	NoFleet,
}

// What the parts of 1.22's strategy share (docs/ComputerPlayer.md, cpu_strtgy): the end of a path, the kinds its code names
// together, distances as 1.22 computes them, and the packing of the fleets' lists.
public static class StrategyBasics
{
	// The end of a path, x 9601 (World.Beyond).
	public const double PathEnd = World.Beyond;

	// The planes' and ships' kinds that the computer's code names together.
	public static bool IsGroundFacility(UnitKind kind) =>
		kind >= UnitKind.AirBase && kind <= UnitKind.Fortress;

	public static bool IsLandForce(UnitKind kind) =>
		kind >= UnitKind.InfantryBase && kind <= UnitKind.Fortress;

	// The distance of (dx, dy) as 1.22 computes it everywhere: the x component over the cosine of the direction folded
	// into 0 to 90°, with a component of 0 taken as 1.
	public static double DistanceOf(double dx, double dy)
	{
		if (dx == 0)
			dx = 1;
		if (dy == 0)
			dy = 1;

		var drctn = atan2(dy, dx) * RadiansToDegrees;
		if (drctn < 0)
			drctn = drctn + 360;
		if (dx < 0)
			dx = -dx;
		if (drctn >= 180)
			drctn = drctn - 180;
		if (drctn >= 90)
			drctn = 90 - (drctn - 90);
		return dx / cos(drctn * DegreesToRadians);
	}

	public static double DistanceBetween(ref Unit to, ref Unit from)
	{
		return DistanceOf(to.Position.X - from.Position.X, to.Position.Y - from.Position.Y);
	}

	// A double whose low 32 bits are replaced: 1.22 stores an int into the low half of a double, and reads the double
	// back (0x406A6D), whose high half, with its sign and exponent, is unchanged.
	public static double WithLowBits(double value, int low)
	{
		return BitConverter.Int64BitsToDouble(
			(BitConverter.DoubleToInt64Bits(value) & unchecked((long)0xFFFFFFFF00000000))
				| (uint)low
		);
	}

	// A plane parked at its base and ready to take off, as the computer selects its planes.
	public static bool IsReadyToTakeOff(ref Unit plane)
	{
		return plane.IsStopping
			&& plane.PlaneState == UnitState.Parked
			&& plane.Ammo != 0
			&& plane.ReloadTime == 0
			&& plane.Weapon != FireKind.Unarmed
			&& plane.Weapon != FireKind.Maintenance;
	}

	// Packs the list from start to last, moving each unit to the first empty element before it; after a move, the scan
	// starts again after start.
	public static void Pack(Span<short> list, int start, int last)
	{
		var empty = -1;
		for (var i = start; i <= last; i++)
		{
			var unit = list[i];
			if (unit == 0 && empty == -1)
				empty = i;

			if (empty != -1 && unit != 0)
			{
				list[empty] = unit;
				list[i] = 0;
				empty = -1;
				i = start;
			}
		}
	}

	// The fleet's next move: the point at the direction (in degrees) and distance from its flagship.
	public static void MoveFrom(
		ref ComputerFleet fleet,
		ref Unit flagship,
		double direction,
		double distance
	)
	{
		var a = direction * DegreesToRadians;
		fleet.PathY[0] = sin(a) * distance + flagship.Position.Y;
		fleet.PathX[0] = cos(a) * distance + flagship.Position.X;
	}
}
