using System.Runtime.InteropServices;

namespace OpenNspw;

// Stand-ins for the C runtime and Win32 memory functions that the game uses on pointers (malloc, memcpy, ZeroMemory,
// ...). Memory from malloc is native, so that its pointers stay valid.
public static unsafe partial class crt
{
	public static void* malloc(nuint size)
	{
		return NativeMemory.Alloc(size);
	}

	public static void free(void* memblock)
	{
		NativeMemory.Free(memblock);
	}

	public static void* memcpy(void* dest, void* src, nuint count)
	{
		Buffer.MemoryCopy(src, dest, count, count);
		return dest;
	}

	public static void* memset(void* dest, int c, nuint count)
	{
		NativeMemory.Fill(dest, count, (byte)c);
		return dest;
	}

	public static void ZeroMemory(void* destination, nuint length)
	{
		NativeMemory.Clear(destination, length);
	}

	public static int strcmp(ReadOnlySpan<byte> string1, ReadOnlySpan<byte> string2)
	{
		var a = UntilNull(string1);
		var b = UntilNull(string2);
		return a.SequenceCompareTo(b) switch
		{
			< 0 => -1,
			> 0 => 1,
			_ => 0,
		};
	}

	public static int strcmp(string string1, string string2)
	{
		return strcmp(tchar.ShiftJis.GetBytes(string1), tchar.ShiftJis.GetBytes(string2));
	}

	public static int strcmp(ReadOnlySpan<byte> string1, string string2)
	{
		return strcmp(string1, tchar.ShiftJis.GetBytes(string2));
	}

	// The game's printf calls write to a console that the Windows application does not have.
	public static int printf(string format, params object?[] args)
	{
		return 0;
	}
}

public partial class Nspw
{
	public int time(object? destTime)
	{
		return _platform.time();
	}
}
