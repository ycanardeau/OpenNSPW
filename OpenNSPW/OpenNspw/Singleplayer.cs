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
	// with a path whose end, 9601 (MAP_RIGHT+1), it sets.
	private const uint FleetAddress = 0x4867A0;
	private const int FleetSize = 0x498;
	private const int FleetCount = 11;
	private const int FleetFlagshipOffset = 0x002;
	private const int FleetTargetsOffset = 0x42C;
	private const int FleetBaseOffset = 0x474;
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

	private static Span<byte> Bytes<T>(ref T value) where T : struct
	{
		return MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1));
	}

	private static readonly SharedGlobal[] SharedGlobals =
	[
		new(0x443A48, game => Bytes(ref game.BattleTime), true), // rest_time
		new(0x443A90, game => Bytes(ref game.MaxUnitId), true), // max_unit
		new(MapAddress, game => Bytes(ref game.MapTiles), false), // cmbt_map
		new(0x463AC0, game => Bytes(ref game.MoveOrders).Slice(0, 2 * sizeof(MoveOrder)), true), // new_pp[2]
		new(0x483B44, game => Bytes(ref game.Result), true), // game_end
		new(0x483B4C, game => Bytes(ref game.CpuSide), true), // cpu_side
		new(0x489AB0, game => Bytes(ref game.CameraPosition), true), // cmbt_x, cmbt_y
		new(0x489AD0, game => Bytes(ref game.FrameCount), true), // FrameCount
		new(0x489AE0, game => Bytes(ref game.Selections), true), // slct_unit
		new(0x489EE4, game => Bytes(ref game.Tick), true), // cc_count
		new(0x489FB0, game => Bytes(ref game.LocalSide), true), // your_side
		new(0x489FC0, game => Bytes(ref game.DecisionPoints), true), // decision_point
		new(0x48A1E8, game => Bytes(ref game.ScenarioNumber), true), // sinario
		new(0x48A200, game => Bytes(ref game.Sprites), false), // sprt
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

	// The sprite sheet: 1.22's own (tst_cg1.bmp) in a single-player game, which has another title and other land-based
	// fighters of Japan, and NSPW NET's otherwise.
	public string SpriteSheetName => IsSingleplayer ? "tst_cg1.bmp" : "t3.bmp";

	// cpu_side of 1.22: the side the computer plays, or None.
	public Side CpuSide;

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

	// The emulator of the single-player game being played: a new one for each game, so that 1.22's own globals start as
	// the executable has them.
	public Emulator? Emulator;

	// The gas of each unit as it was copied into the emulator, to write back only what the emulated code changed.
	private readonly int[] _emulatedGas = new int[UnitCount * 8];

	private void CopyUnitsIn(Emulator emulator)
	{
		var units = MemoryMarshal.AsBytes((Span<Unit>)Units);
		for (var m = 0; m < UnitCount; m++)
		{
			var unit = units.Slice(m * sizeof(Unit), sizeof(Unit));
			var target = emulator.At(UnitAddress + (uint)(m * UnitSize), UnitSize);
			unit[..UnitOffsets.SubWeapons].CopyTo(target);
			for (var i = 0; i < 8; i++)
			{
				var gas = (int)Units[m].gas[i];
				_emulatedGas[m * 8 + i] = gas;
				MemoryMarshal.Write(target[(UnitGasOffset + 4 * i)..], gas);
			}
			unit.Slice(UnitOffsets.Found, 2 * sizeof(int)).CopyTo(target[UnitFoundOffset..]);
			unit[UnitOffsets.Random..].CopyTo(target[UnitRandomOffset..]);
		}
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

	// The functions that 1.22 calls but the port provides: the C runtime's rand and the map, on the game's own state, and
	// the Win32 and DirectDraw functions that draw decision's messages, on the game's back buffer.
	private void SetNatives(Emulator emulator)
	{
		emulator.SetNative(Functions.Rand, () => emulator.Cpu.Eax = new Register32((uint)rand()));
		emulator.SetNative(Functions.LoadMap, () => LoadMapNatively(emulator));

		emulator.SetImport("wsprintfA", 0, () => FormatNatively(emulator));
		emulator.SetImport("SetWindowTextA", 8, () => emulator.Cpu.Eax = new Register32(1));
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
				var hr = IDirectDrawSurface_GetDC(lpDDSBack, &hdc);
				MemoryMarshal.Write(emulator.At(emulator.Argument(1), 4), hdc.Value);
				emulator.Cpu.Eax = new Register32((uint)hr);
			}
		);
		var releaseDC = emulator.AddStub(
			"IDirectDrawSurface7::ReleaseDC",
			8,
			() => emulator.Cpu.Eax = new Register32((uint)IDirectDrawSurface_ReleaseDC(lpDDSBack, new HDC((int)emulator.Argument(1))))
		);
		MemoryMarshal.Write(emulator.At(methods + GetDCOffset, 4), getDC);
		MemoryMarshal.Write(emulator.At(methods + ReleaseDCOffset, 4), releaseDC);

		MemoryMarshal.Write(emulator.At(FontAddress, 4), gameFont_1.Value);
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
			MemoryMarshal.Write(emulator.At(FleetAddress + (uint)(i * FleetSize + FleetPathEndOffset), sizeof(double)), (double)(MAP_RIGHT + 1));
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

	// cpu_strtgy: the computer's order of this frame, in new_pp[0] and slct_unit[0].
	public void RunCpuStrategy()
	{
		RunEmulated(Functions.CpuStrategy);
	}

	// The emulated decision, which ComputerPlayerTests compares Decide, its rewrite, with: the scenario's events and
	// victory conditions, and the result's messages once it has one.
	public void RunDecision()
	{
		RunEmulated(Functions.Decision);
	}

	// set_unit_data(m): the data of unit m by its kind and type, NSPW 1.22's.
	internal void RunSetUnitData(int m)
	{
		RunEmulated(Functions.SetUnitData, (uint)m);
	}

	// plane_in_cv(m): the planes of carrier or air base m.
	private int RunCountPlanesIn(int m)
	{
		return (int)RunEmulated(Functions.CountPlanesIn, (uint)m);
	}

	// set_new_unit_plane: a new plane of the side and kind, of the type, at carrier or air base no, as the port's
	// AddPlane adds one.
	private void RunAddPlane(Side side, UnitKind kind, int type, int no, int planes, FireKind arm)
	{
		RunEmulated(Functions.AddPlane, (uint)side, (uint)kind, (uint)type, (uint)no, (uint)planes, (uint)arm);
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

	// The orders of this frame, as 1.22 gives them after the input, without turns: the computer's, from cpu_strtgy, and
	// the player's, from the buffers that the input filled, as ReceiveAndIssueOrders gives a turn's. The system orders,
	// which go back to the network game's setting screen, are not given: a single-player game has no setting screen yet.
	private void IssueSingleplayerOrders()
	{
		if (Result != GameResult.None)
			return;

		if (CpuSide != Side.None)
			RunCpuStrategy();

		if (BufferedArrivedUnits[1] != 0)
			OnUnitArrived(1, BufferedArrivedUnits[1]);

		var bf_the_slct_unit = SelectedUnit;
		Array256<short> bf_slct_unit = Selections[1];
		for (var s = 0; s <= MaxUnitId; s++)
			Selections[1][s] = 0;

		MoveOrders[1] = BufferedMoveOrders[1];
		SelectOrders[1] = BufferedSelectOrders[1];
		SelectOrders[0].IsSet = false;
		UnpackSelections(1, LocalSide == Side.Japan);

		ApplyOrders();
		ApplyUnitInfoInput();

		Selections[1] = bf_slct_unit;
		SelectedUnit = bf_the_slct_unit;

		ClearBufferedOrders();
	}

	// The emulated set_cpu_route(m, a), which ComputerPlayerTests compares PlanComputerRoute, its rewrite, with. 1.22
	// passes 4.0 for a new order and 1.0 for a ship that is blocked, but does not use the value.
	public void SetCpuRoute(int m, double a)
	{
		var bits = BitConverter.DoubleToUInt64Bits(a);
		RunEmulated(Functions.SetCpuRoute, (uint)m, (uint)bits, (uint)(bits >> 32));
	}

	// Whether 1.22 plans the route of the ship itself: a computer's ship that follows no leader.
	private bool IsComputerShipOnItsOwn(ref Unit unit)
	{
		return CpuSide != Side.None && unit.Side == CpuSide && unit.GroupLeader == 0;
	}

	// 1.22's chara_cont, for a computer's ship whose course ahead is blocked: its path becomes its destination alone, and
	// set_cpu_route (PlanComputerRoute) plans it again. When a ship blocks it (shipAhead, that ship's rectangle) and its destination is in that
	// ship's rectangle, the destination becomes where it is.
	private void RerouteComputerShip(ref Unit unit, int m, RECT? shipAhead)
	{
		int n;
		for (n = 1; n <= 63 && unit.PathX[n] != MAP_RIGHT + 1; n++)
		{
		}

		if (n >= 2)
		{
			unit.PathX[0] = unit.PathX[n - 1];
			unit.PathY[0] = unit.PathY[n - 1];
			unit.PathX[1] = MAP_RIGHT + 1;
		}
		else if (shipAhead is { } rect && PointInRect3(ref rect, (int)unit.PathX[0], (int)unit.PathY[0]) != 0)
		{
			unit.PathX[0] = unit.Position.X;
			unit.PathY[0] = unit.Position.Y;
			unit.PathX[1] = MAP_RIGHT + 1;
		}

		PlanComputerRoute(m);
	}
}
