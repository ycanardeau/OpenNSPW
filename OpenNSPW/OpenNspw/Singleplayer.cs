using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Aigamo.Enzan;
using OpenNspw.Singleplayer;

namespace OpenNspw;

// The single-player mode (docs/Singleplayer.md): the game against the computer of NSPW 1.22, whose own functions (the
// computer's strategy, the scenarios and their victory conditions) run in an emulator on this state.
public unsafe partial class Nspw
{
	// The bytes of a global of the game.
	private delegate Span<byte> GlobalBytes(Nspw game);

	// A global that the game and 1.22 both have: its address in 1.22, and whether the emulated code writes it, so that it
	// is copied back after a call.
	private sealed record SharedGlobal(uint Address, GlobalBytes Bytes, bool IsWritten);

	// unit of 1.22, whose UNIT differs from NSPW NET's after arm (see the docs).
	private const uint UnitAddress = 0x4916A0;
	private const int UnitCount = 256;
	private const int UnitSize = 0x598;
	private const int UnitGasOffset = 0x538;
	private const int UnitFoundOffset = 0x558;
	private const int UnitRandomOffset = 0x560;

	// The table of the computer's fleets (0x4867A0, 11 entries of 0x498 bytes), which game_init clears; each entry ends
	// with a path whose end, 9601 (World.Beyond), it sets.
	private const uint FleetAddress = 0x4867A0;
	private const int FleetSize = 0x498;
	private const int FleetCount = 11;
	private const int FleetPathEndOffset = 0x478;

	// cmbt_map of 1.22, which load_it2 writes.
	private const uint MapAddress = 0x443AA0;

	// The globals of 1.22 that the emulated code uses, but that the port sets once: lpDDSBack, and the font of decision's
	// messages.
	private const uint BackBufferAddress = 0x463AAC;
	private const uint FontAddress = 0x491600;

	// IDirectDrawSurface7::GetDC and ReleaseDC, by their offsets in its table of methods.
	private const int GetDCOffset = 0x44;
	private const int ReleaseDCOffset = 0x68;
	private const int SurfaceMethodCount = 0x6C / 4;

	// "Panzer Vor!", which set_cpu_route gives SetWindowTextA each time round its loop of waypoints (0x42ECF3).
	private const uint WaypointMessageAddress = 0x441620;

	// set_cpu_route's pp_indx, [esp+2Ch] in its frame, from the stack of the SetWindowTextA it calls: past the return
	// address and the two arguments.
	private const uint RouteIndexOffset = 4 + 2 * 4 + 0x2C;

	// The points of a unit's path (pp_x, pp_y).
	private const int RoutePathLength = 64;

	private static Span<byte> Bytes<T>(ref T value) where T : struct
	{
		return MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1));
	}

	private static readonly SharedGlobal[] SharedGlobals =
	[
		new(0x443A48, game => Bytes(ref game.BattleTime), true), // rest_time
		new(0x443A90, game => Bytes(ref game.MaxUnitId), true), // max_unit
		new(MapAddress, game => Bytes(ref game.MapTiles), false), // cmbt_map
		new(0x463AC0, game => Bytes(ref game.MoveOrders)[..(2 * sizeof(MoveOrder))], true), // new_pp[2]
		new(0x483B44, game => Bytes(ref game.Result), true), // game_end
		new(0x483B4C, game => Bytes(ref game.CpuSide), true), // cpu_side
		new(0x489AB0, game => Bytes(ref game.CameraPosition), true), // cmbt_x, cmbt_y
		new(0x489AD0, game => Bytes(ref game.Renderer.FrameCount), true), // FrameCount
		new(0x489AE0, game => Bytes(ref game.Selections), true), // slct_unit
		new(0x489EE4, game => Bytes(ref game.Tick), true), // cc_count
		new(0x489FB0, game => Bytes(ref game.LocalSide), true), // your_side
		new(0x489FC0, game => Bytes(ref game.DecisionPoints), true), // decision_point
		new(0x48A1E8, game => Bytes(ref game.ScenarioNumber), true), // sinario
		new(0x48A200, game => Bytes(ref game.Renderer.Sprites), false), // sprt
	];

	private static int OffsetOf<T>(ref Unit unit, ref T field)
	{
		return (int)Unsafe.ByteOffset(ref Unsafe.As<Unit, byte>(ref unit), ref Unsafe.As<T, byte>(ref field));
	}

	private static (int SubWeapons, int Found, int Random) GetUnitOffsets()
	{
		var unit = new Unit();
		return (OffsetOf(ref unit, ref unit.SubWeapons), OffsetOf(ref unit, ref unit.IsFound), OffsetOf(ref unit, ref unit.Random250));
	}

	// The offsets in Unit of the fields where its layout and 1.22's part.
	private static readonly (int SubWeapons, int Found, int Random) UnitOffsets = GetUnitOffsets();

	// Whether this is a single-player game, which numbers its units as 1.22 does.
	public bool IsSingleplayer;

	// The rules the battle follows where 1.22 and NSPW NET differ: 1.22's in a single-player game, NSPW NET's otherwise.
	// Chosen when a game starts (InitializeGame).
	public BattleRules Rules = BattleRules.NspwNet;

	// The sprite sheet: 1.22's own (tst_cg1.bmp) in a single-player game, which has another title and other land-based
	// fighters of Japan, and NSPW NET's otherwise.
	public string SpriteSheetName => IsSingleplayer ? "tst_cg1.bmp" : "t3.bmp";

	// cpu_side of 1.22: the side the computer plays, or None.
	public Side CpuSide;

	// The trace this single-player game follows and checks itself against, or records itself in, or null
	// (docs/Verification.md).
	public ISingleplayerTrace? Trace;

	// The period of 1.22's multimedia timer, which drives its frames (0x432960), in milliseconds.
	private const uint SingleplayerTimerPeriod = 88;

	// Whether 1.22's timer has ticked since the last frame, which starts a frame, and the ticks of that frame: none at
	// speed 0, or speed + the timer's ticks counted (0x4345DF). The count is 1: 1.22 clears it after a frame (0x433C99), so
	// the timer's ticks while a frame runs are lost. The speed is set by NSPW NET's keys, as in a network game, and Space
	// runs 20 ticks a frame while it is held.
	private bool TryStartSingleplayerFrame(out short ticks)
	{
		if (Elapsed < SingleplayerTimerPeriod)
		{
			ticks = 0;
			return false;
		}

		LastTime2 += Elapsed / SingleplayerTimerPeriod * SingleplayerTimerPeriod;
		if (Buttons.HasFlag(InputButtons.Space))
			ticks = 20;
		else
			ticks = GameSpeed == 0 ? (short)0 : (short)(GameSpeed + 1);
		Trace?.Reach(this, TracePoint.Frame);
		if (Trace?.TicksOfNextFrame() is { } traced)
			ticks = (short)traced;
		return true;
	}

	// The unit ranges of all_head.h. 1.22 has the same, with 20 ships and 30 planes a side instead of 40 and 50.
	public int SHIP_LIMIT => IsSingleplayer ? 20 : 40;
	public int PLANE_LIMIT => IsSingleplayer ? 30 : 50;

	public int JPN_SHIP_START => 1;
	public int JPN_SHIP_END => JPN_SHIP_START - 1 + SHIP_LIMIT;

	public int USA_SHIP_START => JPN_SHIP_END + 1;
	public int USA_SHIP_END => USA_SHIP_START - 1 + SHIP_LIMIT;

	public int JPN_PLANE_START => USA_SHIP_END + 1;
	public int JPN_PLANE_END => JPN_PLANE_START - 1 + PLANE_LIMIT;

	public int USA_PLANE_START => JPN_PLANE_END + 1;
	public int USA_PLANE_END => USA_PLANE_START - 1 + PLANE_LIMIT;

	// NSPW_122.exe, which the host gives the game to play single-player.
	public byte[]? SingleplayerExecutable;

	// The player's selection of every unit when the order in the buffers was given (BufferSelections). The network game
	// sends only the units of the player's side (BufferedSelections); 1.22 gives an order to every unit selected, such as
	// the fighters that scenario 303 takes from Japan, whose numbers are Japan's.
	private Array256<short> BufferedSelectionsOfAllUnits;

	// The emulator of the single-player game being played: a new one for each game, so that 1.22's own globals start as
	// the executable has them.
	public Emulator? Emulator;

	// The gas of each unit as it was copied into the emulator, to write back only what the emulated code changed.
	private readonly int[] _emulatedGas = new int[UnitCount * 8];

	// Writes unit m in 1.22's layout, its gas truncated to int.
	private void WriteUnitAs122(int m, Span<byte> target)
	{
		var unit = MemoryMarshal.AsBytes((Span<Unit>)Units).Slice(m * sizeof(Unit), sizeof(Unit));
		unit[..UnitOffsets.SubWeapons].CopyTo(target);
		for (var i = 0; i < 8; i++)
			MemoryMarshal.Write(target[(UnitGasOffset + 4 * i)..], (int)Units[m].gas[i]);
		unit.Slice(UnitOffsets.Found, 2 * sizeof(int)).CopyTo(target[UnitFoundOffset..]);
		unit[UnitOffsets.Random..].CopyTo(target[UnitRandomOffset..]);
	}

	private void CopyUnitsIn(Emulator emulator)
	{
		for (var m = 0; m < UnitCount; m++)
		{
			var target = emulator.At(UnitAddress + (uint)(m * UnitSize), UnitSize);
			WriteUnitAs122(m, target);
			for (var i = 0; i < 8; i++)
				_emulatedGas[m * 8 + i] = (int)Units[m].gas[i];
		}
	}

	// Writes the units and the fires that a trace of 1.22 records (Trace122.Units, Trace122.Fires), in 1.22's layout.
	public void WriteStateAs122(Span<byte> state)
	{
		for (var m = 0; m < Trace122.UnitCount; m++)
			WriteUnitAs122(m, state.Slice(m * Trace122.UnitSize, Trace122.UnitSize));
		MemoryMarshal.AsBytes((Span<Fire>)Fires)[..(Trace122.FireCount * Trace122.FireSize)].CopyTo(state[(Trace122.UnitCount * Trace122.UnitSize)..]);
	}

	private void CopyUnitsOut(Emulator emulator)
	{
		var units = MemoryMarshal.AsBytes((Span<Unit>)Units);
		for (var m = 0; m < UnitCount; m++)
		{
			var unit = units.Slice(m * sizeof(Unit), sizeof(Unit));
			var source = emulator.At(UnitAddress + (uint)(m * UnitSize), UnitSize);
			source[..UnitOffsets.SubWeapons].CopyTo(unit);
			for (var i = 0; i < 8; i++)
			{
				var gas = MemoryMarshal.Read<int>(source[(UnitGasOffset + 4 * i)..]);
				if (gas != _emulatedGas[m * 8 + i])
					Units[m].gas[i] = gas;
			}
			source.Slice(UnitFoundOffset, 2 * sizeof(int)).CopyTo(unit[UnitOffsets.Found..]);
			source[UnitRandomOffset..].CopyTo(unit[UnitOffsets.Random..]);
		}
	}

	private void CopyIn(Emulator emulator)
	{
		foreach (var global in SharedGlobals)
		{
			var bytes = global.Bytes(this);
			bytes.CopyTo(emulator.At(global.Address, bytes.Length));
		}

		CopyUnitsIn(emulator);
	}

	private void CopyOut(Emulator emulator)
	{
		foreach (var global in SharedGlobals)
		{
			if (global.IsWritten)
			{
				var bytes = global.Bytes(this);
				emulator.At(global.Address, bytes.Length).CopyTo(bytes);
			}
		}

		CopyUnitsOut(emulator);
	}

	// Runs a function of 1.22 on the game's state.
	private uint RunEmulated(uint address, params ReadOnlySpan<uint> arguments)
	{
		var emulator = Emulator ?? throw new InvalidOperationException("No single-player game is being played.");
		CopyIn(emulator);
		var result = emulator.Run(address, arguments);
		CopyOut(emulator);
		return result;
	}

	private static string CString(Emulator emulator, uint address)
	{
		var length = 0;
		while (emulator.At(address + (uint)length, 1)[0] != 0)
			length++;
		return ShiftJis.GetString(emulator.At(address, length));
	}

	// load_it2(file_name): the map, by the name NSPW NET gives the same file, so that it is found and recognized
	// (CurrentMap) as in a network game. 1.22 names some of them differently in case.
	private void LoadMapNatively(Emulator emulator)
	{
		var name = CString(emulator, emulator.Argument(0));
		var known = new[] { "Map\\South_pacific.dat", "Map\\Middle_pacific.dat", "Map\\Japan_off.dat" };
		LoadScenarioFile2(known.FirstOrDefault(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase)) ?? name);
		Bytes(ref MapTiles).CopyTo(emulator.At(MapAddress, sizeof(Array256<Array256<ushort>>)));
	}

	// wsprintfA(buffer, format, ...): the arguments, a doubleword each, are read as the format's conversions ask.
	private void FormatNatively(Emulator emulator)
	{
		var buffer = emulator.Argument(0);
		var format = emulator.Argument(1);
		var formatBytes = ShiftJis.GetBytes(CString(emulator, format));
		var arguments = new List<object?>();
		for (var i = 0; i < formatBytes.Length; i++)
		{
			if (formatBytes[i] != '%')
				continue;

			i++;
			while (i < formatBytes.Length && "-0+ #.123456789hlL".Contains((char)formatBytes[i]))
				i++;
			if (i >= formatBytes.Length || formatBytes[i] == '%')
				continue;

			var value = emulator.Argument(2 + arguments.Count);
			arguments.Add(formatBytes[i] == 's' ? ShiftJis.GetBytes(CString(emulator, value)) : (int)value);
		}

		var text = new byte[1025];
		var length = wsprintf(text, formatBytes, [.. arguments]);
		text.AsSpan(0, length + 1).CopyTo(emulator.At(buffer, length + 1));
		emulator.Cpu.Eax = new Register32((uint)length);
	}

	// SetWindowTextA(hWnd, text): nothing is shown. set_cpu_route calls it each time round its loop of waypoints, with
	// "Panzer Vor!", as 1.22 runs in a window (0x463B20 is 0, as the emulator leaves it), and the port ends the loop there
	// once the route fills its path.
	// BUG(1.22): set_cpu_route's loop (0x42DDA6 to 0x42ECFF) goes on until a leg is clear, with nothing to keep the route
	// in its path's 64 points. A waypoint that turns back moves the destination 160 further (0x42EB92), up to 2000, but the
	// leg to the moved destination then being clear sets that distance back to 0 (0x42EC6F): when the leg back from there
	// is blocked again, the loop adds the same points again and again. 1.22 writes on through the units after the ship
	// and the globals after them, until its first write to the resources, read-only, at 0x4F1000, crashes it. The port
	// ends the route at point 62, the last whose leg cannot write past the path (point i + 2): from there, 1.22 also ends
	// it, unless it writes past the path or finds no heading clear.
	private void SetWindowTextNatively(Emulator emulator)
	{
		if (emulator.Argument(1) == WaypointMessageAddress)
		{
			var index = emulator.At(emulator.Cpu.Esp.Value + RouteIndexOffset, sizeof(int));
			if (MemoryMarshal.Read<int>(index) + 2 >= RoutePathLength)
				MemoryMarshal.Write(index, -1);
		}

		emulator.Cpu.Eax = new Register32(1);
	}

	// The functions that 1.22 calls but the port provides: the C runtime's rand and the map, on the game's own state, and
	// the Win32 and DirectDraw functions that draw decision's messages, on the game's back buffer.
	private void SetNatives(Emulator emulator)
	{
		emulator.SetNative(Functions.Rand, () => emulator.Cpu.Eax = new Register32((uint)rand()));
		emulator.SetNative(Functions.LoadMap, () => LoadMapNatively(emulator));

		emulator.SetImport("wsprintfA", 0, () => FormatNatively(emulator));
		emulator.SetImport("SetWindowTextA", 8, () => SetWindowTextNatively(emulator));
		emulator.SetImport("SetTextColor", 8, () => emulator.Cpu.Eax = new Register32(SetTextColor(new HDC((int)emulator.Argument(0)), emulator.Argument(1))));
		emulator.SetImport("SetBkMode", 8, () => emulator.Cpu.Eax = new Register32((uint)SetBkMode(new HDC((int)emulator.Argument(0)), (int)emulator.Argument(1))));
		emulator.SetImport("SelectObject", 8, () => emulator.Cpu.Eax = new Register32((uint)SelectObject(new HDC((int)emulator.Argument(0)), new HGDIOBJ((int)emulator.Argument(1))).Value));
		emulator.SetImport(
			"TextOutA",
			20,
			() =>
			{
				var length = (int)emulator.Argument(4);
				var text = emulator.At(emulator.Argument(3), length);
				emulator.Cpu.Eax = new Register32((uint)TextOut(new HDC((int)emulator.Argument(0)), (int)emulator.Argument(1), (int)emulator.Argument(2), text, length));
			}
		);

		// lpDDSBack: an object whose table of methods has GetDC and ReleaseDC.
		var methods = emulator.Allocate(4 * SurfaceMethodCount);
		var surface = emulator.Allocate(4);
		MemoryMarshal.Write(emulator.At(surface, 4), methods);
		MemoryMarshal.Write(emulator.At(BackBufferAddress, 4), surface);
		var getDC = emulator.AddStub(
			"IDirectDrawSurface7::GetDC",
			8,
			() =>
			{
				HDC hdc = default;
				var hr = IDirectDrawSurface_GetDC(Renderer.lpDDSBack, &hdc);
				MemoryMarshal.Write(emulator.At(emulator.Argument(1), 4), hdc.Value);
				emulator.Cpu.Eax = new Register32((uint)hr);
			}
		);
		var releaseDC = emulator.AddStub(
			"IDirectDrawSurface7::ReleaseDC",
			8,
			() => emulator.Cpu.Eax = new Register32((uint)IDirectDrawSurface_ReleaseDC(Renderer.lpDDSBack, new HDC((int)emulator.Argument(1))))
		);
		MemoryMarshal.Write(emulator.At(methods + GetDCOffset, 4), getDC);
		MemoryMarshal.Write(emulator.At(methods + ReleaseDCOffset, 4), releaseDC);

		MemoryMarshal.Write(emulator.At(FontAddress, 4), Renderer.GameFont1.Value);
	}

	// A new emulator for a single-player game, with 1.22's own globals as its game_init leaves them before it calls the
	// scenario.
	private Emulator CreateEmulator()
	{
		var emulator = new Emulator(SingleplayerExecutable ?? throw new InvalidOperationException("The single-player executable (NSPW_122.exe) is not set."));

		// fsin, fcos and fpatan as the port computes sin, cos and atan2, the same on every platform.
		emulator.Cpu.Fpu.Sin = sin;
		emulator.Cpu.Fpu.Cos = cos;
		emulator.Cpu.Fpu.Atan2 = atan2;
		SetNatives(emulator);
		for (var i = 0; i < FleetCount; i++)
			MemoryMarshal.Write(emulator.At(FleetAddress + (uint)(i * FleetSize + FleetPathEndOffset), sizeof(double)), (double)World.Beyond);
		return emulator;
	}

	// In place of get_sinario_data: 1.22's scenario, which places the units, loads the map and sets the sides, the time
	// limit and the camera.
	public void SetUpSingleplayerScenario()
	{
		Emulator = CreateEmulator();
		RunEmulated(Functions.SetUpScenario(ScenarioNumber));
	}

	// Starts the battle of ScenarioNumber, without the title and setting screens.
	public void StartSingleplayerGame()
	{
		Mode = GameMode.Battle;
		InitializeGame();
	}

	// Where an uninitialized local that cpu_strtgy reads is on the emulator's stack, by the instruction that reads it. A
	// call from the port starts with esp at the image base (Emulator.Run), and cpu_strtgy's prologue (its return address,
	// ebp, the alignment to 8, 0x90 of locals and four registers) leaves esp 0xA8 below it. 0x4050C7: [esp+30h], which
	// scenario 208's event reads as the count of planes at fleet 2's base when more than 20000 is left or the base is
	// gone (docs/ComputerPlayer.md).
	private static uint? StackLocalAddress(Emulator emulator, uint instruction) =>
		instruction switch
		{
			0x4050C7 => emulator.ImageBase - 0xA8 + 0x30,
			_ => null,
		};

	// The uninitialized locals that the battle's own code reads, by the instruction of 1.22 that reads them: the heading
	// of a torpedo plane's land check (0x413D23, PlaneDropTorpedo), the angle of a plane's landing pattern and approach to
	// a carrier heading 360 (0x4173BF, SetLandingDestination; 0x41764E, UpdateLanding). A trace gives them as 1.22 read
	// them, in order.
	private static readonly uint[] BattleStackLocals = [0x413D23, 0x4173BF, 0x41764E];
	private readonly Queue<(uint Instruction, byte[] Value)> _battleStackLocals = new();

	// The instruction of an uninitialized local of the battle that a trace gave and the battle has not read, if any.
	internal uint? UntakenStackLocal => _battleStackLocals.TryPeek(out var local) ? local.Instruction : null;

	// What the battle reads, at the instruction of 1.22 given, from a local 1.22 has not set there: what a trace gave, or
	// else what the port's own local holds.
	internal double TakeStackLocal(uint instruction, double own)
	{
		if (!_battleStackLocals.TryPeek(out var local) || local.Instruction != instruction)
			return own;
		_battleStackLocals.Dequeue();
		return BitConverter.ToDouble(local.Value);
	}

	// The uninitialized locals that cpu_strtgy reads, with what Windows' stack holds there: 0, in every frame of the
	// traces recorded so far, where the emulator's would hold what the emulated code left.
	// BUG(1.22): scenario 208's count at 0x4050C7 is read before it is set. Kept as is for compatibility with 1.22.
	private static readonly (uint Instruction, uint Value)[] StackLocalsOnWindows = [(0x4050C7, 0)];

	// Whether a trace has given the uninitialized locals of the coming cpu_strtgy (GiveStackLocal).
	private bool _hasStackLocals;

	// Gives cpu_strtgy, before it runs, what Windows' stack held where it reads a local it has not set, as a trace of
	// 1.22 records it; false if the port does not know where that local is.
	internal bool GiveStackLocal(uint instruction, uint value)
	{
		if (Emulator is not { } emulator || StackLocalAddress(emulator, instruction) is not { } address)
			return false;

		MemoryMarshal.Write(emulator.At(address, sizeof(uint)), value);
		_hasStackLocals = true;
		return true;
	}

	// What Windows' stack held where 1.22 read a local it had not set: for cpu_strtgy, written into the emulator's; for
	// the battle's own code, kept until it reads it.
	internal bool GiveStackLocal(uint instruction, byte[] value)
	{
		if (!BattleStackLocals.Contains(instruction))
			return value.Length == sizeof(uint) && GiveStackLocal(instruction, BitConverter.ToUInt32(value));
		_battleStackLocals.Enqueue((instruction, value));
		return true;
	}

	// cpu_strtgy: the computer's order of this frame, in new_pp[0] and slct_unit[0]. The locals it reads uninitialized
	// hold what Windows' stack holds there, or what a trace has given.
	public void RunCpuStrategy()
	{
		if (!_hasStackLocals)
		{
			foreach (var (instruction, value) in StackLocalsOnWindows)
				GiveStackLocal(instruction, value);
		}

		RunEmulated(Functions.CpuStrategy);
		_hasStackLocals = false;
	}

	// decision: the scenario's events and victory conditions, and the result's messages once it has one.
	public void RunDecision()
	{
		RunEmulated(Functions.Decision);
	}

	// set_unit_data(m): the data of unit m by its kind and type, NSPW 1.22's.
	internal void RunSetUnitData(int m)
	{
		RunEmulated(Functions.SetUnitData, (uint)m);
	}

	// The buffers of the orders, empty, and the player free to give the next: what ReceiveAndIssueOrders leaves after a
	// turn.
	private void ClearBufferedOrders()
	{
		for (var e = 0; e <= 1; e++)
		{
			BufferedMoveOrders[e].Unit = 0;
			BufferedSelectOrders[e].IsSet = false;
			BufferedMenuOrders[e].Menu = CombatMenuItem.None;
			BufferedSystemOrders[e] = 0;
			SystemOrders[e] = 0;
			BufferedArrivedUnits[e] = 0;
			for (var s = 0; s <= (USA_PLANE_END / 2) - 1; s++)
				BufferedSelections[e][s] = 0;
		}

		BufferedSelectionsOfAllUnits = default;

		CanOrder = true;
		HasOrdered = false;
	}

	// What cnct_game_init sets for the orders and the supply of a network game, for a single-player game.
	private void InitializeSingleplayerOrders()
	{
		ClearBufferedOrders();
		SupplyPoints = 0;
		SupplyCount = 0;
		SupplyTarget = 0;
	}

	// The player's orders of this frame, before HandleInput takes them, as a trace of 1.22 records them: the orders the
	// clicks on the battle area made, the combat menu's item chosen, and the selection; null if the player gave none, or
	// if the battle has a result, which gives no more orders, as 1.22 does.
	internal Trace122Orders? ReadPlayerOrders()
	{
		if (Result != GameResult.None || (MoveOrders[1].Unit == 0 && !SelectOrders[1].IsSet && BufferedMenuOrders[1].Menu == CombatMenuItem.None))
			return null;

		var selections = new short[Trace122Orders.SelectionLength];
		for (var s = 0; s < selections.Length; s++)
			selections[s] = Selections[1][s];
		return new Trace122Orders(MoveOrders[1], SelectOrders[1], BufferedMenuOrders[1], SelectedUnit, SelectionCount, selections);
	}

	// Gives the player's orders of a trace, before HandleInput takes them, as the player's clicks would: the selection,
	// the combat menu's item for its unit, then the orders of the battle area for the selected unit.
	internal void GivePlayerOrders(Trace122Orders orders)
	{
		SelectionCount = orders.SelectionCount;
		for (var s = 0; s < Trace122Orders.SelectionLength; s++)
			Selections[1][s] = orders.Selections[s];
		if (orders.Menu.Menu != CombatMenuItem.None)
		{
			SelectedUnit = orders.Menu.SelectedUnit;
			ChooseCombatMenuItem(orders.Menu.Menu);
		}

		SelectedUnit = orders.SelectedUnit;
		MoveOrders[1] = orders.Move;
		SelectOrders[1] = orders.Select;
	}

	// The orders of this frame, as 1.22 gives them after the input, without turns: the computer's, from cpu_strtgy, and
	// the player's, from the buffers that the input filled, as ReceiveAndIssueOrders gives a turn's. The system orders,
	// which go back to the network game's setting screen, are not given: a single-player game has no setting screen yet.
	private void IssueSingleplayerOrders()
	{
		if (Result != GameResult.None)
			return;

		Trace?.Reach(this, TracePoint.Orders);

		var savedSelectedUnit = SelectedUnit;
		Array256<short> savedSelections = Selections[1];
		Selections[1] = BufferedSelectionsOfAllUnits;

		// The combat menu's item first: 1.22 applies it in its input (0x431C53), before cpu_strtgy, which may see it.
		ApplyUnitInfoInput();

		Selections[1] = savedSelections;
		SelectedUnit = savedSelectedUnit;

		if (CpuSide != Side.None)
			RunCpuStrategy();

		if (BufferedArrivedUnits[1] != 0)
			OnUnitArrived(1, BufferedArrivedUnits[1]);

		savedSelectedUnit = SelectedUnit;
		savedSelections = Selections[1];
		Selections[1] = BufferedSelectionsOfAllUnits;

		MoveOrders[1] = BufferedMoveOrders[1];
		SelectOrders[1] = BufferedSelectOrders[1];
		SelectOrders[0].IsSet = false;

		ApplyOrders();

		Selections[1] = savedSelections;
		SelectedUnit = savedSelectedUnit;

		ClearBufferedOrders();
	}

	// The tile of cmbt_map at row, column, flat as 1.22 reads it, row by row: beyond the map, what 1.22's memory holds around
	// it, in a single-player game, or else 0, sea.
	internal ushort MapTileOrBeyond(int row, int column)
	{
		var index = row * 256 + column;
		if ((uint)index < 256 * 256)
			return MapTiles[index / 256][index % 256];
		if (Emulator is not { } emulator)
			return 0;
		CopyIn(emulator);
		return BitConverter.ToUInt16(emulator.At((uint)(MapAddress + index * 2), sizeof(ushort)));
	}

	// set_cpu_route2(m): the route of ship m to the destination just set as its path, as a ship that follows a leader
	// takes it (0x414D79).
	public void SetCpuRoute2As122(int m)
	{
		RunEmulated(Functions.SetCpuRoute2, (uint)m);
	}

	// set_cpu_route(m, radius): the route of the computer's ship m to the destination just set as its path; the orders
	// pass 4.0, the battle 1.0 when the ship is blocked.
	public void SetCpuRoute(int m, double radius)
	{
		var bits = BitConverter.DoubleToUInt64Bits(radius);
		RunEmulated(Functions.SetCpuRoute, (uint)m, (uint)bits, (uint)(bits >> 32));
	}
}
