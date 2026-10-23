namespace OpenNspw.Recorder122;

// Gives orders to the player's side of 1.22 in place of the player (--orders): every `interval` frames, one time in
// three an item of the combat menu, else an order on the battle area.
//
// On the battle area: to one of its ships or planes chosen at random, but its bases and the units supplying, an attack on
// an enemy unit it sees, one time in two when it sees one, or else a move to a random point within 2000 of it (for a
// parked plane, a launch). They are written where 1.22's drawing leaves the orders of the clicks, new_pp[1] and
// new_slct[1], as its orders start (0x430390): 1.22 applies them as it applies the player's.
//
// On the combat menu, in the next frame: as its input starts (0x4312A0), one of its ships or planes is selected by 1.22's
// own selection of a clicked unit (0x417CF0), called there; then, where the input tests each entry of the menu it has
// made for the unit for a click (0x431C18), one entry chosen at random, but the item the unit has, is clicked: the thread
// goes where a click on it goes (0x431C53), with its item. 1.22 applies the item as it applies the player's click.
//
// The recorder records them all as the player's. The choices draw from the recorder's own random numbers, never from
// 1.22's rand.
internal sealed class Commander(Random random, int interval)
{
	private const uint Units = 0x4916A0;
	private const uint LocalSide = 0x489FB0; // your_side
	private const uint MoveOrder = 0x463AE0; // new_pp[1]: used, x at 8, y at 16, cls at 24
	private const uint SelectOrder = 0x489A60; // new_slct[1]: sw, the_slct_unit at 4, m at 8, gr_x at 16, gr_y at 24
	private const uint MenuItem = 0x489AC0; // cmbt_menu_slctd
	private const int OrderSize = 32;
	private const int Range = 2000;
	private const int PlaneParked = 2;

	// The input (0x4312A0), the selection it is made to call (0x417CF0), and in it, the test of a menu entry for a click,
	// with the entry's number in edi and, at esp, the count of entries at 0x18 and their items at 0x58, and where the
	// click goes, with the item in esi.
	public const uint Input = 0x4312A0;
	private const uint SelectUnit = 0x417CF0;
	public const uint MenuEntryTest = 0x431C18;
	private const uint MenuEntryClicked = 0x431C53;
	private const uint MenuEntryCount = 0x18;
	private const uint MenuEntryItems = 0x58;

	private readonly Random _random = random;
	private readonly int _interval = interval;

	// Whether the next frame gives a menu item; whether the selection called is returning to the input; the unit selected
	// for it, until the item is clicked; and the entry chosen, or -1.
	private bool _isMenuNext;
	private bool _isSelecting;
	private int _menuUnit;
	private int _menuEntry = -1;

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

	// The ships and planes of the player's side, with those supplying or without.
	private static int[] OwnUnits(Debugger debugger, bool withSupplying)
	{
		var units = debugger.Read(Units, Trace122.UnitCount * Trace122.UnitSize);
		var side = debugger.ReadInt16(LocalSide);
		return Enumerable.Range(1, Trace122.UnitCount - 1)
			.Where(m => BitConverter.ToInt16(units, m * Trace122.UnitSize + SideOffset) == side
				&& IsOrderable(BitConverter.ToInt32(units, m * Trace122.UnitSize + KindOffset))
				&& (withSupplying || BitConverter.ToInt32(units, m * Trace122.UnitSize + SupplyOffset) == 0))
			.ToArray();
	}

	// As the input starts: in a frame that gives a menu item, has it call the selection of a unit chosen at random, which
	// returns to the input's start; there, drops the unit it was given.
	public string? StartInput(Debugger debugger)
	{
		var esp = debugger.ReadEsp();
		if (_isSelecting)
		{
			_isSelecting = false;
			debugger.Resume(esp: esp + 4);
			return null;
		}
		if (!_isMenuNext)
			return null;

		_isMenuNext = false;
		var own = OwnUnits(debugger, withSupplying: true);
		if (own.Length == 0)
			return null;

		_menuUnit = own[_random.Next(own.Length)];
		_menuEntry = -1;
		debugger.WriteUInt32(esp - 8, Input);
		debugger.WriteUInt32(esp - 4, (uint)_menuUnit);
		debugger.Resume(eip: SelectUnit, esp: esp - 8);
		_isSelecting = true;
		return $"unit {_menuUnit} is selected";
	}

	// Where the input tests a menu entry for a click: chooses an entry at its first, and clicks it at the chosen one.
	public string? TestMenuEntry(Debugger debugger)
	{
		if (_menuUnit == 0)
			return null;

		var unit = _menuUnit;
		var esp = debugger.ReadEsp();
		var entry = (int)debugger.ReadEdi();
		uint ItemOf(int e) => debugger.ReadUInt32(esp + MenuEntryItems + 4 * (uint)e);
		if (_menuEntry < 0)
		{
			var current = (uint)debugger.ReadInt16(MenuItem);
			var entries = Enumerable.Range(0, debugger.ReadInt32(esp + MenuEntryCount)).Where(e => ItemOf(e) != current).ToArray();
			if (entries.Length == 0)
			{
				_menuUnit = 0;
				return $"unit {unit} has no other item on the combat menu";
			}
			_menuEntry = entries[_random.Next(entries.Length)];
		}
		if (entry != _menuEntry)
			return null;

		var item = ItemOf(entry);
		_menuUnit = 0;
		debugger.Resume(eip: MenuEntryClicked, esi: item);
		return $"unit {unit} takes the combat menu's item {(CombatMenuItem)item}";
	}

	// The order of the frame, if it is one to give one: written into 1.22, and described; or, one time in three, a menu
	// item next frame.
	public string? Order(Debugger debugger, int frame)
	{
		_menuUnit = 0;
		if (_interval <= 0 || frame % _interval != 0)
			return null;

		if (_random.Next(3) == 0)
		{
			_isMenuNext = true;
			return "a combat menu item next frame";
		}

		var units = debugger.Read(Units, Trace122.UnitCount * Trace122.UnitSize);
		var side = debugger.ReadInt16(LocalSide);
		short SideOf(int m) => BitConverter.ToInt16(units, m * Trace122.UnitSize + SideOffset);
		int IntOf(int m, int offset) => BitConverter.ToInt32(units, m * Trace122.UnitSize + offset);
		double DoubleOf(int m, int offset) => BitConverter.ToDouble(units, m * Trace122.UnitSize + offset);

		var own = OwnUnits(debugger, withSupplying: false);
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
