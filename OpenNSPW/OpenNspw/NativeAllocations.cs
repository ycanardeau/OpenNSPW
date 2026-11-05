using System.Runtime.InteropServices;

namespace OpenNspw;

// The native memory of the stand-ins (docs/Performance.md, Native memory): a surface's pixels, a sound's data and an mmio
// file, whose pointers the game holds, and which the collector sees none of. Each block is allocated and freed here,
// which counts the bytes held, for the tests (NativeMemoryTests), and tells the collector of them, so that it collects
// the objects that hold them soon enough.
internal static unsafe class NativeAllocations
{
	private static long _bytes;

	// The bytes held now, by the whole process.
	public static long Bytes => Interlocked.Read(ref _bytes);

	// The collector is told of a byte at least, as it takes no less.
	private static long Pressure(long bytes) => Math.Max(bytes, 1);

	// A block of `bytes` zeros.
	public static void* Alloc(long bytes)
	{
		var block = NativeMemory.AllocZeroed((nuint)bytes);
		Interlocked.Add(ref _bytes, bytes);
		GC.AddMemoryPressure(Pressure(bytes));
		return block;
	}

	// Frees a block that Alloc gave, of the same `bytes`.
	public static void Free(void* block, long bytes)
	{
		NativeMemory.Free(block);
		Interlocked.Add(ref _bytes, -bytes);
		GC.RemoveMemoryPressure(Pressure(bytes));
	}
}
