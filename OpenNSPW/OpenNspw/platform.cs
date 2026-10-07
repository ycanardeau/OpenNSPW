namespace OpenNspw;

// What the game needs from the platform that the ported code cannot do by itself: creating COM objects (DirectPlay 8),
// showing message boxes, opening files, the clock, showing frames, rasterizing text, playing sounds and keeping
// settings (the registry). The desktop app and the tests provide it.
public interface INspwPlatform
{
	// Milliseconds since some fixed time, like timeGetTime.
	uint timeGetTime();

	// Seconds since 1970, like time(NULL).
	int time();

	// Shows a frame: what the game flipped or blitted to the primary surface.
	void Present(DirectDrawSurface7 primary);

	// Rasterizes text in a font of the given height and face (the game uses "", the default Shift_JIS font), or returns
	// null to draw nothing.
	TextBitmap? RasterizeText(string text, int height, string faceName);

	// Creates a sound to play from PCM data.
	ISound? CreateSound(WAVEFORMATEX format, ReadOnlySpan<byte> data);

	// Reads and writes a setting, like the registry values the game keeps under HKEY_CURRENT_USER.
	string? ReadSetting(string key);

	void WriteSetting(string key, string value);

	// Opens a file of the game, by its path relative to the game's directory with '/' separators. Returns null if the
	// file cannot be opened.
	Stream? OpenFile(string path, FileMode mode, FileAccess access, FileShare share);

	// The names of the files in a directory of the game ('/' separators, "" for the game's directory) that match a
	// pattern such as "*.dat".
	IReadOnlyList<string> FindFiles(string directory, string pattern);

	// Creates the object of a COM class, like CoCreateInstance. Returns S_OK, or an error such as REGDB_E_CLASSNOTREG.
	int CoCreateInstance(Guid rclsid, Guid riid, out object? ppv);

	// Shows a message box and returns the button the user chose, like MessageBox.
	int MessageBox(HWND? hWnd, string lpText, string lpCaption, uint uType);
}

// A platform with no COM classes, no files, no settings, nothing shown or played, and message boxes that answer OK.
// The default, for function tests.
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

	public IReadOnlyList<string> FindFiles(string directory, string pattern)
	{
		return [];
	}

	public uint timeGetTime()
	{
		return (uint)Environment.TickCount64;
	}

	public int time()
	{
		return (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
	}

	public void Present(DirectDrawSurface7 primary)
	{
	}

	public TextBitmap? RasterizeText(string text, int height, string faceName)
	{
		return null;
	}

	public ISound? CreateSound(WAVEFORMATEX format, ReadOnlySpan<byte> data)
	{
		return null;
	}

	public string? ReadSetting(string key)
	{
		return null;
	}

	public void WriteSetting(string key, string value)
	{
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

	// DirectMusic's classes are the port's own (unknwn.cs); the platform creates the others.
	public int CoCreateInstance<T>(Guid rclsid, object? pUnkOuter, uint dwClsContext, Guid riid, out T? ppv) where T : class
	{
		object? obj;
		var hr = rclsid == dmusici.CLSID_DirectMusicLoader ? (obj = new DirectMusicLoader8()) is not null ? winerror.S_OK : 0
			: rclsid == dmusici.CLSID_DirectMusicPerformance ? (obj = new DirectMusicPerformance8()) is not null ? winerror.S_OK : 0
			: _platform.CoCreateInstance(rclsid, riid, out obj);
		ppv = obj as T;
		return winerror.SUCCEEDED(hr) && ppv is null ? winerror.E_NOINTERFACE : hr;
	}
}
