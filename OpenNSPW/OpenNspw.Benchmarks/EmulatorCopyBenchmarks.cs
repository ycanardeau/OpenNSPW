using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace OpenNspw.Benchmarks;

// The copy of the game's state into and out of the emulator's memory that the single-player mode makes around a call
// into the single-player executable (docs/Refactoring.md, The emulator): cmbt_map and sprt as one block each, and each
// unit as three blocks, at the single-player executable's addresses and offsets. Compare with BattleBenchmarks.Tick to
// see what copying costs per call, against what a tick costs.
[MemoryDiagnoser]
public class EmulatorCopyBenchmarks
{
	private const int ImageBase = 0x400000;
	private const int MapAddress = 0x443AA0;
	private const int SpriteAddress = 0x48A200;

	// Not known yet: after the sprites. The cost of the copy does not depend on it.
	private const int UnitAddress = 0x48A600;

	private const int UnitCount = 256;
	private const int UnitSize = 0x598;
	private const int FoundOffset = 0x570;
	private const int RandomOffset = 0x578;

	// found and tech.
	private const int FoundSize = 2 * sizeof(int);

	private static int OffsetOf<T>(ref UNIT unit, ref T field)
	{
		return (int)Unsafe.ByteOffset(ref Unsafe.As<UNIT, byte>(ref unit), ref Unsafe.As<T, byte>(ref field));
	}

	// The offsets of gas, found and rnd_100 in UNIT.
	private static (int Gas, int Found, int Random) UnitOffsets()
	{
		var unit = new UNIT();
		return (OffsetOf(ref unit, ref unit.gas), OffsetOf(ref unit, ref unit.found), OffsetOf(ref unit, ref unit.rnd_100));
	}

	private static readonly (int Gas, int Found, int Random) Offsets = UnitOffsets();

	// From used to the 6 values of gas that the single-player executable has, at the same offsets.
	private static readonly int FirstBlockSize = Offsets.Gas + 6 * sizeof(double);

	// rnd_100 to rnd_10.
	private static readonly int RandomSize = Unsafe.SizeOf<UNIT>() - Offsets.Random;

	private readonly Nspw _game = new();
	private readonly byte[] _memory = new byte[UnitAddress - ImageBase + UnitCount * UnitSize];

	private Span<byte> At(int address, int size)
	{
		return _memory.AsSpan(address - ImageBase, size);
	}

	[Benchmark]
	public void CopyIn()
	{
		MemoryMarshal.AsBytes((Span<Array256<ushort>>)_game.cmbt_map).CopyTo(At(MapAddress, Unsafe.SizeOf<Array256<Array256<ushort>>>()));
		MemoryMarshal.AsBytes((Span<SPRT>)_game.sprt).CopyTo(At(SpriteAddress, Unsafe.SizeOf<Array25<SPRT>>()));
		var units = MemoryMarshal.AsBytes((Span<UNIT>)_game.unit);
		for (var m = 0; m < UnitCount; m++)
		{
			var unit = units.Slice(m * Unsafe.SizeOf<UNIT>(), Unsafe.SizeOf<UNIT>());
			var target = At(UnitAddress + m * UnitSize, UnitSize);
			unit[..FirstBlockSize].CopyTo(target);
			unit.Slice(Offsets.Found, FoundSize).CopyTo(target[FoundOffset..]);
			unit.Slice(Offsets.Random, RandomSize).CopyTo(target[RandomOffset..]);
		}
	}

	[Benchmark]
	public void CopyOut()
	{
		At(MapAddress, Unsafe.SizeOf<Array256<Array256<ushort>>>()).CopyTo(MemoryMarshal.AsBytes((Span<Array256<ushort>>)_game.cmbt_map));
		At(SpriteAddress, Unsafe.SizeOf<Array25<SPRT>>()).CopyTo(MemoryMarshal.AsBytes((Span<SPRT>)_game.sprt));
		var units = MemoryMarshal.AsBytes((Span<UNIT>)_game.unit);
		for (var m = 0; m < UnitCount; m++)
		{
			var unit = units.Slice(m * Unsafe.SizeOf<UNIT>(), Unsafe.SizeOf<UNIT>());
			var source = At(UnitAddress + m * UnitSize, UnitSize);
			source[..FirstBlockSize].CopyTo(unit);
			source.Slice(FoundOffset, FoundSize).CopyTo(unit[Offsets.Found..]);
			source.Slice(RandomOffset, RandomSize).CopyTo(unit[Offsets.Random..]);
		}
	}
}
