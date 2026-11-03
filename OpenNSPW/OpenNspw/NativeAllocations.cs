using System.Runtime.InteropServices;
using Aigamo.Otsuki.Actors;

namespace OpenNspw;

// The native memory of the stand-ins (docs/Performance.md, Native memory): a surface's pixels, a sound's data and an mmio
// file, whose pointers the game holds, and which the collector sees none of. Each block is allocated and freed here,
// which counts the bytes held, for the tests (NativeMemoryTests), and tells the collector of them, so that it collects
// the objects that hold them soon enough.
internal static unsafe class NativeAllocations
{
	// The bytes held, a Tally, as the game's threads allocate them and the finalizers free them (docs/Actors.md).
	private static readonly Tally Held = new();

	// The bytes held now, by the whole process.
	public static long Bytes => Held.Value;

	// The collector is told of a byte at least, as it takes no less.
	private static long Pressure(long bytes) => Math.Max(bytes, 1);

	// A block of `bytes` zeros.
	public static void* Alloc(long bytes)
	{
		var block = NativeMemory.AllocZeroed((nuint)bytes);
		Held.Add(bytes);
		GC.AddMemoryPressure(Pressure(bytes));
		return block;
	}

	// Frees a block that Alloc gave, of the same `bytes`.
	public static void Free(void* block, long bytes)
	{
		NativeMemory.Free(block);
		Held.Add(-bytes);
		GC.RemoveMemoryPressure(Pressure(bytes));
	}
}
