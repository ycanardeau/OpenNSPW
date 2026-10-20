namespace OpenNspw.Recorder122;

// Gives orders to the player's side of 1.22 in place of the player (--orders): every `interval` frames, to one of its
// ships or planes chosen at random, but its bases and the units supplying, an attack on an enemy unit it sees, one time
// in two when it sees one, or else a move to a random point within 2000 of it (for a parked plane, a launch). They are
// written where 1.22's drawing leaves the orders of the clicks, new_pp[1] and new_slct[1], as its orders start
// (0x430390): 1.22 applies them as it applies the player's, and the recorder records them as the player's. The choices
// draw from the recorder's own random numbers, never from 1.22's rand.
internal sealed class Commander(Random random, int interval)
{
	private const uint Units = 0x4916A0;
	private const uint LocalSide = 0x489FB0; // your_side
	private const uint MoveOrder = 0x463AE0; // new_pp[1]: used, x at 8, y at 16, cls at 24
	private const uint SelectOrder = 0x489A60; // new_slct[1]: sw, the_slct_unit at 4, m at 8, gr_x at 16, gr_y at 24
	private const int OrderSize = 32;
	private const int Range = 2000;
	private const int PlaneParked = 2;

	private readonly Random _random = random;
	private readonly int _interval = interval;

	private static int OffsetOf(string name)
	{
		return Trace122Layout.Unit.Single(field => field.Name == name).Offset;
	}

	private static readonly int SideOffset = OffsetOf("Side");
	private static readonly int XOffset = OffsetOf("Position.X");
	private static readonly int YOffset = OffsetOf("Position.Y");
	private static readonly int KindOffset = OffsetOf("Kind");
	private static readonly int PlaneStateOffset = OffsetOf("info[0]");
	private static readonly int SupplyOffset = OffsetOf("SupplyTime");
	private static readonly int FoundOffset = OffsetOf("IsFound.Value");

	// Whether a unit of the kind can be ordered: not a base, a city or a ground force (AirBase to Fortress).
	private static bool IsOrderable(int kind)
	{
		return kind is >= (int)UnitKind.Battleship and < (int)UnitKind.AirBase;
	}

	private static byte[] MoveOrderBytes(int m, int x, int y)
	{
		var bytes = new byte[OrderSize];
		BitConverter.TryWriteBytes(bytes.AsSpan(0), m);
		BitConverter.TryWriteBytes(bytes.AsSpan(8), (double)x);
		BitConverter.TryWriteBytes(bytes.AsSpan(16), (double)y);
		BitConverter.TryWriteBytes(bytes.AsSpan(24), 1);
		return bytes;
	}

	private static byte[] SelectOrderBytes(int m, int target)
	{
		var bytes = new byte[OrderSize];
		BitConverter.TryWriteBytes(bytes.AsSpan(0), 1);
		BitConverter.TryWriteBytes(bytes.AsSpan(4), m);
		BitConverter.TryWriteBytes(bytes.AsSpan(8), target);
		return bytes;
	}

	// The order of the frame, if it is one to give one: written into 1.22, and described.
	public string? Order(Debugger debugger, int frame)
	{
		if (_interval <= 0 || frame % _interval != 0)
			return null;

		var units = debugger.Read(Units, Trace122.UnitCount * Trace122.UnitSize);
		var side = debugger.ReadInt16(LocalSide);
		short SideOf(int m) => BitConverter.ToInt16(units, m * Trace122.UnitSize + SideOffset);
		int IntOf(int m, int offset) => BitConverter.ToInt32(units, m * Trace122.UnitSize + offset);
		double DoubleOf(int m, int offset) => BitConverter.ToDouble(units, m * Trace122.UnitSize + offset);

		var own = Enumerable.Range(1, Trace122.UnitCount - 1)
			.Where(m => SideOf(m) == side && IsOrderable(IntOf(m, KindOffset)) && IntOf(m, SupplyOffset) == 0)
			.ToArray();
		if (own.Length == 0)
			return null;

		var seen = Enumerable.Range(1, Trace122.UnitCount - 1)
			.Where(m => SideOf(m) != 0 && SideOf(m) != side && IntOf(m, FoundOffset) != 0)
			.ToArray();
		var unit = own[_random.Next(own.Length)];
		var isParked = IntOf(unit, PlaneStateOffset) == PlaneParked;
		if (seen.Length > 0 && !isParked && _random.Next(2) == 0)
		{
			var target = seen[_random.Next(seen.Length)];
			debugger.Write(SelectOrder, SelectOrderBytes(unit, target));
			return $"unit {unit} attacks unit {target}";
		}

		var x = Math.Clamp((int)DoubleOf(unit, XOffset) + _random.Next(-Range, Range + 1), all_head.MAP_LEFT, all_head.MAP_RIGHT);
		var y = Math.Clamp((int)DoubleOf(unit, YOffset) + _random.Next(-Range, Range + 1), all_head.MAP_BOTTOM, all_head.MAP_TOP);
		debugger.Write(MoveOrder, MoveOrderBytes(unit, x, y));
		return $"unit {unit} {(isParked ? "launches" : "moves")} to ({x}, {y})";
	}
}
