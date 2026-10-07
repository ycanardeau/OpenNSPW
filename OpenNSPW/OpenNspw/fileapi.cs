using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OpenNspw;

// Stand-ins for the Win32 file functions that the game uses (fileapi.h). Paths are relative to the game's directory,
// with Windows separators, as in the original ("Saved\\auto_save.dat"). The platform opens and lists the files.

// An open file, or a search of FindFirstFile.
public sealed class HANDLE(Stream? stream, Queue<string>? found = null)
{
	public Stream? Stream { get; } = stream;

	public Queue<string>? Found { get; } = found;
}

[StructLayout(LayoutKind.Sequential)]
public struct WIN32_FIND_DATA
{
	public uint dwFileAttributes;
	public ulong ftCreationTime;
	public ulong ftLastAccessTime;
	public ulong ftLastWriteTime;
	public uint nFileSizeHigh;
	public uint nFileSizeLow;
	public uint dwReserved0;
	public uint dwReserved1;
	public Array260<byte> cFileName;
	public Array14<byte> cAlternateFileName;
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
	public const uint FILE_CURRENT = 1;
	public const uint FILE_END = 2;

	public static readonly HANDLE INVALID_HANDLE_VALUE = new(null);
}

public unsafe partial class Nspw
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

	public HANDLE CreateFile(ReadOnlySpan<byte> lpFileName, uint dwDesiredAccess, uint dwShareMode, object? lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, object? hTemplateFile)
	{
		return CreateFile(CString(lpFileName), dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
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

	// The buffer is any variable of the game: its bytes are read or written, as through the pointer in C++.
	public int ReadFile<T>(HANDLE hFile, ref T lpBuffer, uint nNumberOfBytesToRead, uint* lpNumberOfBytesRead, object? lpOverlapped) where T : unmanaged
	{
		var buffer = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref lpBuffer, 1));
		var count = (int)Math.Min(nNumberOfBytesToRead, (uint)buffer.Length);
		var read = hFile.Stream?.ReadAtLeast(buffer[..count], count, throwOnEndOfStream: false) ?? 0;
		if (lpNumberOfBytesRead is not null)
		{
			*lpNumberOfBytesRead = (uint)read;
		}

		return hFile.Stream is null ? FALSE : TRUE;
	}

	public int WriteFile<T>(HANDLE hFile, ref T lpBuffer, uint nNumberOfBytesToWrite, uint* lpNumberOfBytesWritten, object? lpOverlapped) where T : unmanaged
	{
		var buffer = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref lpBuffer, 1));
		var count = (int)Math.Min(nNumberOfBytesToWrite, (uint)buffer.Length);
		hFile.Stream?.Write(buffer[..count]);
		if (lpNumberOfBytesWritten is not null)
		{
			*lpNumberOfBytesWritten = hFile.Stream is null ? 0 : (uint)count;
		}

		return hFile.Stream is null ? FALSE : TRUE;
	}

	public uint SetFilePointer(HANDLE hFile, int lDistanceToMove, object? lpDistanceToMoveHigh, uint dwMoveMethod)
	{
		if (hFile.Stream is null)
		{
			return 0xFFFFFFFF;
		}

		return (uint)hFile.Stream.Seek(lDistanceToMove, dwMoveMethod switch
		{
			FILE_CURRENT => SeekOrigin.Current,
			FILE_END => SeekOrigin.End,
			_ => SeekOrigin.Begin,
		});
	}

	private static void SetFound(WIN32_FIND_DATA* lpFindFileData, string name)
	{
		*lpFindFileData = default;
		CopyString(name, lpFindFileData->cFileName, MAX_PATH);
	}

	// Lists the files that match a pattern such as "Scenario\\*.dat".
	public HANDLE FindFirstFile(string lpFileName, WIN32_FIND_DATA* lpFindFileData)
	{
		var path = lpFileName.Replace('\\', '/');
		var slash = path.LastIndexOf('/');
		var found = new Queue<string>(_platform.FindFiles(slash < 0 ? "" : path[..slash], path[(slash + 1)..]));
		if (!found.TryDequeue(out var name))
		{
			return INVALID_HANDLE_VALUE;
		}

		SetFound(lpFindFileData, name);
		return new HANDLE(null, found);
	}

	public int FindNextFile(HANDLE hFindFile, WIN32_FIND_DATA* lpFindFileData)
	{
		if (hFindFile.Found is null || !hFindFile.Found.TryDequeue(out var name))
		{
			return FALSE;
		}

		SetFound(lpFindFileData, name);
		return TRUE;
	}

	public int FindClose(HANDLE hFindFile)
	{
		return TRUE;
	}

	// Copies the selected item of a list box to a buffer.
	public int DlgDirSelectEx(HWND hwndDlg, Span<byte> lpString, int chCount, int idListBox)
	{
		lock (WindowsLock)
		{
			if (GetDlgItem(hwndDlg, idListBox) is not { CurSel: >= 0 } list || list.CurSel >= list.Items.Count)
			{
				return FALSE;
			}

			CopyString(list.Items[list.CurSel].Text, lpString, chCount);
			return TRUE;
		}
	}
}
