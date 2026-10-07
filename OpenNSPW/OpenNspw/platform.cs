namespace OpenNspw;

// What the game needs from the platform that the ported code cannot do by itself: creating COM objects (DirectPlay 8),
// showing message boxes and opening files. The desktop app and the tests provide it.
public interface INspwPlatform
{
	// Opens a file of the game, by its path relative to the game's directory with '/' separators. Returns null if the
	// file cannot be opened.
	Stream? OpenFile(string path, FileMode mode, FileAccess access, FileShare share);

	// Creates the object of a COM class, like CoCreateInstance. Returns S_OK, or an error such as REGDB_E_CLASSNOTREG.
	int CoCreateInstance(Guid rclsid, Guid riid, out object? ppv);

	// Shows a message box and returns the button the user chose, like MessageBox.
	int MessageBox(HWND? hWnd, string lpText, string lpCaption, uint uType);
}

// A platform with no COM classes and no files, whose message boxes answer OK. The default, for function tests.
public sealed class NullPlatform : INspwPlatform
{
	public static NullPlatform Instance { get; } = new();

	public int CoCreateInstance(Guid rclsid, Guid riid, out object? ppv)
	{
		ppv = null;
		return winerror.REGDB_E_CLASSNOTREG;
	}

	public int MessageBox(HWND? hWnd, string lpText, string lpCaption, uint uType)
	{
		return windef.IDOK;
	}

	public Stream? OpenFile(string path, FileMode mode, FileAccess access, FileShare share)
	{
		return null;
	}
}

public static class winerror
{
	public const int S_OK = 0;
	public const int S_FALSE = 1;
	public const int E_FAIL = unchecked((int)0x80004005);
	public const int E_OUTOFMEMORY = unchecked((int)0x8007000E);
	public const int E_NOINTERFACE = unchecked((int)0x80004002);
	public const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);

	public static bool FAILED(int hr)
	{
		return hr < 0;
	}

	public static bool SUCCEEDED(int hr)
	{
		return hr >= 0;
	}
}

public partial class Nspw(INspwPlatform? platform = null)
{
	private readonly INspwPlatform _platform = platform ?? NullPlatform.Instance;

	public const uint CLSCTX_INPROC_SERVER = 0x1;

	public int CoCreateInstance<T>(Guid rclsid, object? pUnkOuter, uint dwClsContext, Guid riid, out T? ppv) where T : class
	{
		var hr = _platform.CoCreateInstance(rclsid, riid, out var obj);
		ppv = obj as T;
		return winerror.SUCCEEDED(hr) && ppv is null ? winerror.E_NOINTERFACE : hr;
	}
}
