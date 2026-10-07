using System.Runtime.InteropServices;

namespace OpenNspw;

// Stand-ins for the Win32 types and constants that the game uses as data, with the names and layouts of the Windows
// SDK (windef.h, minwindef.h).

[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
	public int x;
	public int y;
}

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
	public int left;
	public int top;
	public int right;
	public int bottom;
}

public static class windef
{
	public const int FALSE = 0;

	public const int TRUE = 1;

	public const int MAX_PATH = 260;
}
