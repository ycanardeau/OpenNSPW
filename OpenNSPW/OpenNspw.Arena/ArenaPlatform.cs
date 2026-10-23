using OpenNspw.DirectPlay;

namespace OpenNspw.Arena;

// Thrown on a game's thread, from Present, to end the game once the arena has what it needs.
internal sealed class ArenaStoppedException : Exception;

// The platform of a game in the arena: the original's files, which it reads but never changes, NSPW 1.22's directory
// for a single-player game's, a clock that advances by a millisecond each time it is read and by the time the game
// sleeps (as the tests' LockstepGame's), so that a game runs as fast as it can and the same way every time, and nothing
// shown or played. Each frame shown is handed to `frame`, on the game's thread.
internal sealed class ArenaPlatform(
	string dataDirectory,
	string singleplayerDirectory,
	int startDate,
	Action frame
) : INspwPlatform
{
	private readonly string _dataDirectory = dataDirectory;
	private readonly string _singleplayerDirectory = singleplayerDirectory;
	private readonly int _startDate = startDate;
	private readonly Action _frame = frame;
	private readonly DirectPlay8Factory _directPlay = new();
	private uint _now = 0x00010000;

	public List<string> MessageBoxes { get; } = [];

	public uint timeGetTime() => _now++;

	public int time() => _startDate;

	public void Sleep(uint milliseconds)
	{
		_now += milliseconds;
	}

	// Nothing posts to the game in the arena, so a game that waits for a message would wait for ever.
	public void WaitForMessage(object queue)
	{
		throw new InvalidOperationException(
			"The game waits for a message, which nothing in the arena posts."
		);
	}

	public void Present(DirectDrawSurface7 primary)
	{
		_frame();
	}

	public TextBitmap? RasterizeText(string text, int height, string faceName) => null;

	public ISound? CreateSound(WAVEFORMATEX format, ReadOnlySpan<byte> data) => null;

	public string? ReadSetting(string key) => null;

	public void WriteSetting(string key, string value) { }

	// A file opened for writing is a copy in memory, so that the game's saves leave the original's files as they are.
	public Stream? OpenFile(string path, FileMode mode, FileAccess access, FileShare share)
	{
		try
		{
			if ((access & FileAccess.Write) == 0)
			{
				return new DirectoryFileSource(_singleplayerDirectory).Open(path, share)
					?? new FileStream(GameFiles.Resolve(_dataDirectory, path), mode, access, share);
			}

			var full = GameFiles.Resolve(_dataDirectory, path);
			var exists = File.Exists(full);
			if (!exists && mode is FileMode.Open or FileMode.Truncate)
			{
				return null;
			}

			var copy = new MemoryStream();
			if (exists && mode is FileMode.Open or FileMode.OpenOrCreate or FileMode.Append)
			{
				using var file = File.OpenRead(full);
				file.CopyTo(copy);
				copy.Position = mode == FileMode.Append ? copy.Length : 0;
			}

			return copy;
		}
		catch (IOException)
		{
			return null;
		}
	}

	public IReadOnlyList<string> FindFiles(string directory, string pattern) =>
		GameFiles.Find(_dataDirectory, directory, pattern);

	public int CoCreateInstance(Guid rclsid, Guid riid, out object? ppv) =>
		_directPlay.CoCreateInstance(rclsid, riid, out ppv);

	public int MessageBox(HWND? hWnd, string lpText, string lpCaption, uint uType)
	{
		MessageBoxes.Add(lpText);
		return windef.IDOK;
	}
}
