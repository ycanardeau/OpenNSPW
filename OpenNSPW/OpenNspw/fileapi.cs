namespace OpenNspw;

// Stand-ins for the Win32 file functions that the game uses (fileapi.h). Paths are relative to the game's directory,
// with Windows separators, as in the original ("Saved\\auto_save.dat"). The platform opens the files.

public sealed class HANDLE(Stream? stream)
{
	public Stream? Stream { get; } = stream;
}

public static class fileapi
{
	public const uint GENERIC_READ = 0x80000000;
	public const uint GENERIC_WRITE = 0x40000000;
	public const uint FILE_SHARE_READ = 0x00000001;
	public const uint FILE_SHARE_WRITE = 0x00000002;
	public const uint CREATE_NEW = 1;
	public const uint CREATE_ALWAYS = 2;
	public const uint OPEN_EXISTING = 3;
	public const uint OPEN_ALWAYS = 4;
	public const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
	public const uint FILE_BEGIN = 0;

	public static readonly HANDLE INVALID_HANDLE_VALUE = new(null);
}

public partial class Nspw
{
	public HANDLE CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, object? lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, object? hTemplateFile)
	{
		var mode = dwCreationDisposition switch
		{
			CREATE_NEW => FileMode.CreateNew,
			CREATE_ALWAYS => FileMode.Create,
			OPEN_ALWAYS => FileMode.OpenOrCreate,
			_ => FileMode.Open,
		};
		var access = (dwDesiredAccess & GENERIC_WRITE) != 0
			? (dwDesiredAccess & GENERIC_READ) != 0 ? FileAccess.ReadWrite : FileAccess.Write
			: FileAccess.Read;
		var share = (dwShareMode & FILE_SHARE_WRITE) != 0 ? FileShare.ReadWrite : (dwShareMode & FILE_SHARE_READ) != 0 ? FileShare.Read : FileShare.None;
		var stream = _platform.OpenFile(lpFileName.Replace('\\', '/'), mode, access, share);
		return stream is null ? INVALID_HANDLE_VALUE : new HANDLE(stream);
	}

	public int CloseHandle(HANDLE hObject)
	{
		if (hObject.Stream is null)
		{
			return FALSE;
		}

		hObject.Stream.Dispose();
		return TRUE;
	}
}
