using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace OpenNspw.Benchmarks;

// The copy of the game's state into and out of the emulator's memory that the single-player mode makes around a call
// into the single-player executable (docs/Singleplayer.md, Copying): MapTiles and Sprites as one block each, and each
// unit as three blocks and its gas, at the single-player executable's addresses and offsets. Compare with
// BattleBenchmarks.Tick to see what copying costs per call, against what a tick costs.
[MemoryDiagnoser]
public class EmulatorCopyBenchmarks
{
	private const int ImageBase = 0x400000;
	private const int MapAddress = 0x443AA0;
	private const int SpriteAddress = 0x48A200;

	private const int UnitAddress = 0x4916A0;

	private const int UnitCount = 256;
	private const int UnitSize = 0x598;
	private const int GasOffset = 0x538;
	private const int FoundOffset = 0x558;
	private const int RandomOffset = 0x560;

	// found and tech.
	private const int FoundSize = 2 * sizeof(int);

	private static int OffsetOf<T>(ref Unit unit, ref T field)
	{
		return (int)Unsafe.ByteOffset(ref Unsafe.As<Unit, byte>(ref unit), ref Unsafe.As<T, byte>(ref field));
	}

	// The offsets of SubWeapons, IsFound and Random250 in Unit.
	private static (int SubWeapons, int Found, int Random) UnitOffsets()
	{
		var unit = new Unit();
		return (OffsetOf(ref unit, ref unit.SubWeapons), OffsetOf(ref unit, ref unit.IsFound), OffsetOf(ref unit, ref unit.Random250));
	}

	private static readonly (int SubWeapons, int Found, int Random) Offsets = UnitOffsets();

	// From Side to arm, at the same offsets.
	private static readonly int FirstBlockSize = Offsets.SubWeapons;

	// Random250 to Random10.
	private static readonly int RandomSize = Unsafe.SizeOf<Unit>() - Offsets.Random;

	private readonly Nspw _game = new();
	private readonly byte[] _memory = new byte[UnitAddress - ImageBase + UnitCount * UnitSize];

	private Span<byte> At(int address, int size)
	{
		return _memory.AsSpan(address - ImageBase, size);
	}

	[Benchmark]
	public void CopyIn()
	{
		MemoryMarshal.AsBytes((Span<Array256<ushort>>)_game.MapTiles).CopyTo(At(MapAddress, Unsafe.SizeOf<Array256<Array256<ushort>>>()));
		MemoryMarshal.AsBytes((Span<Sprite>)_game.Sprites).CopyTo(At(SpriteAddress, Unsafe.SizeOf<SpriteArray>()));
		var units = MemoryMarshal.AsBytes((Span<Unit>)_game.Units);
		for (var m = 0; m < UnitCount; m++)
		{
			var unit = units.Slice(m * Unsafe.SizeOf<Unit>(), Unsafe.SizeOf<Unit>());
			var target = At(UnitAddress + m * UnitSize, UnitSize);
			unit[..FirstBlockSize].CopyTo(target);
			for (var i = 0; i < 8; i++)
				MemoryMarshal.Write(target[(GasOffset + 4 * i)..], (int)_game.Units[m].gas[i]);
			unit.Slice(Offsets.Found, FoundSize).CopyTo(target[FoundOffset..]);
			unit.Slice(Offsets.Random, RandomSize).CopyTo(target[RandomOffset..]);
		}
	}

	[Benchmark]
	public void CopyOut()
	{
		At(MapAddress, Unsafe.SizeOf<Array256<Array256<ushort>>>()).CopyTo(MemoryMarshal.AsBytes((Span<Array256<ushort>>)_game.MapTiles));
		At(SpriteAddress, Unsafe.SizeOf<SpriteArray>()).CopyTo(MemoryMarshal.AsBytes((Span<Sprite>)_game.Sprites));
		var units = MemoryMarshal.AsBytes((Span<Unit>)_game.Units);
		for (var m = 0; m < UnitCount; m++)
		{
			var unit = units.Slice(m * Unsafe.SizeOf<Unit>(), Unsafe.SizeOf<Unit>());
			var source = At(UnitAddress + m * UnitSize, UnitSize);
			source[..FirstBlockSize].CopyTo(unit);
			for (var i = 0; i < 8; i++)
				_game.Units[m].gas[i] = MemoryMarshal.Read<int>(source[(GasOffset + 4 * i)..]);
			source.Slice(FoundOffset, FoundSize).CopyTo(unit[Offsets.Found..]);
			source.Slice(RandomOffset, RandomSize).CopyTo(unit[Offsets.Random..]);
		}
	}
}
