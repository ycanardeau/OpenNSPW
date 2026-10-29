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

	// Waits, like Sleep.
	void Sleep(uint milliseconds)
	{
		Thread.Sleep((int)milliseconds);
	}

	// Waits until a message may have been posted to the game's message queue, like Monitor.Wait(queue). Called with the
	// queue's lock held, when the queue is empty.
	void WaitForMessage(object queue)
	{
		Monitor.Wait(queue);
	}

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

	// A sprite file of the app, by its path under the app's sprites with '/' separators, such as "jp/bb.png"
	// (SpriteSheetLayout, docs/GameSprites.md), or null if the platform has none: the game then draws from its sprite
	// sheet's own file (SpriteSheetName).
	SpriteBitmap? LoadSprite(string path) => null;

	// The names of the files in a directory of the game ('/' separators, "" for the game's directory) that match a
	// pattern such as "*.dat".
	IReadOnlyList<string> FindFiles(string directory, string pattern);

	// Creates the object of a COM class, like CoCreateInstance. Returns S_OK, or an error such as REGDB_E_CLASSNOTREG.
	int CoCreateInstance(Guid rclsid, Guid riid, out object? ppv);

	// Shows a message box and returns the button the user chose, like MessageBox.
	int MessageBox(HWND? hWnd, string lpText, string lpCaption, uint uType);
}

// The game's files in a directory, with their names matched without case, as on Windows: the game opens WAV\CLICK1.wav,
// and the file is wav/click1.wav.
public static class GameFiles
{
	// The path of a file or directory of the game: each part of the path is the entry of that name, in any case, or the
	// name as it is where none exists, for a file or directory to create.
	public static string Resolve(string dataDirectory, string path)
	{
		var full = dataDirectory;
		foreach (var part in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
		{
			var exact = Path.Combine(full, part);
			full =
				File.Exists(exact) || Directory.Exists(exact) || !Directory.Exists(full)
					? exact
					: Directory
						.EnumerateFileSystemEntries(full)
						.FirstOrDefault(e =>
							string.Equals(
								Path.GetFileName(e),
								part,
								StringComparison.OrdinalIgnoreCase
							)
						)
						?? exact;
		}

		return full;
	}

	public static IReadOnlyList<string> Find(string dataDirectory, string directory, string pattern)
	{
		var full = Resolve(dataDirectory, directory);
		return Directory.Exists(full)
			?
			[
				.. Directory
					.GetFiles(
						full,
						pattern,
						new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }
					)
					.Select(Path.GetFileName)
					.OfType<string>()
					.Order(StringComparer.OrdinalIgnoreCase),
			]
			: [];
	}
}

// Files of the game that it only reads, in a layer of its files (GameFileLayers): a directory, or the original's files
// that an app embeds (docs/BundledData.md). By the game's path with '/' separators, its names matched without case.
public interface IGameFileSource
{
	// The file at the path, opened to read, or null where there is none.
	Stream? Open(string path, FileShare share);

	// The names of the files in a directory ("" for the top) that match a pattern such as "*.dat".
	IReadOnlyList<string> Find(string directory, string pattern);
}

// The files of a directory, as a layer that is only read (GameFiles).
public sealed class DirectoryFileSource(string directory) : IGameFileSource
{
	private readonly string _directory = directory;

	public Stream? Open(string path, FileShare share)
	{
		var full = GameFiles.Resolve(_directory, path);
		return File.Exists(full)
			? new FileStream(full, FileMode.Open, FileAccess.Read, share)
			: null;
	}

	public IReadOnlyList<string> Find(string directory, string pattern)
	{
		return GameFiles.Find(_directory, directory, pattern);
	}
}

// A file of a layer that is only read, opened to write (GameFileLayers): read from its bytes until the game first writes
// to it or sets its length, then copied to its path in the directory written, a file that it goes on in from the same
// position. A file only read is never copied.
public sealed class CopyOnWriteStream(byte[] bytes, string path, FileAccess access, FileShare share)
	: Stream
{
	private readonly byte[] _bytes = bytes;
	private readonly string _path = path;
	private readonly FileAccess _access = access;
	private readonly FileShare _share = share;
	private Stream _stream = new MemoryStream(bytes, writable: false);

	// Whether the file has been copied, and is now the file at the path.
	public bool IsCopied => _stream is FileStream;

	private Stream Copied()
	{
		if (_stream is FileStream file)
			return file;

		var position = _stream.Position;
		Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
		File.WriteAllBytes(_path, _bytes);
		var copy = new FileStream(_path, FileMode.Open, _access, _share) { Position = position };
		_stream.Dispose();
		_stream = copy;
		return copy;
	}

	public override bool CanRead => (_access & FileAccess.Read) != 0;

	public override bool CanSeek => true;

	public override bool CanWrite => true;

	public override long Length => _stream.Length;

	public override long Position
	{
		get => _stream.Position;
		set => _stream.Position = value;
	}

	public override void Flush()
	{
		_stream.Flush();
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		return _stream.Read(buffer, offset, count);
	}

	public override int Read(Span<byte> buffer)
	{
		return _stream.Read(buffer);
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		return _stream.Seek(offset, origin);
	}

	public override void SetLength(long value)
	{
		Copied().SetLength(value);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		Copied().Write(buffer, offset, count);
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		Copied().Write(buffer);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
			_stream.Dispose();
		base.Dispose(disposing);
	}
}

// The game's files in layers (docs/BundledData.md): the layers before a directory, the directory, and the layers after
// it. A file is read from the first layer that has it, and written in the directory: a file of another layer that the
// game opens to write is copied there when it first writes to it (CopyOnWriteStream), and a file that no layer has is
// created there. Listing a directory lists the names of every layer, each once.
public sealed class GameFileLayers(
	IEnumerable<IGameFileSource> before,
	string directory,
	IEnumerable<IGameFileSource> after
)
{
	private readonly IGameFileSource[] _before = [.. before];
	private readonly string _directory = directory;
	private readonly IGameFileSource[] _after = [.. after];

	// The files of a directory, the data directory, read and written, and in a single-player game NSPW 1.22's directory
	// before it, which has the game's maps and sounds as 1.22 has them (docs/Differences.md).
	public static GameFileLayers OfDirectories(string dataDirectory, string? singleplayerDirectory)
	{
		return new(
			singleplayerDirectory is null ? [] : [new DirectoryFileSource(singleplayerDirectory)],
			dataDirectory,
			[]
		);
	}

	// The file at the path in a layer that is only read, opened to read, or to write as a copy on write.
	private static Stream? OpenIn(
		IGameFileSource layer,
		string path,
		string written,
		FileAccess access,
		FileShare share
	)
	{
		var stream = layer.Open(path, share);
		if (stream is null || access == FileAccess.Read)
			return stream;

		using (stream)
		{
			using var bytes = new MemoryStream();
			stream.CopyTo(bytes);
			return new CopyOnWriteStream(bytes.ToArray(), written, access, share);
		}
	}

	private static bool Exists(IGameFileSource[] layers, string path)
	{
		foreach (var layer in layers)
		{
			using var stream = layer.Open(path, FileShare.ReadWrite);
			if (stream is not null)
				return true;
		}

		return false;
	}

	private Stream? OpenOrCreate(string path, FileMode mode, FileAccess access, FileShare share)
	{
		var written = GameFiles.Resolve(_directory, path);
		if (mode is FileMode.Open or FileMode.OpenOrCreate)
		{
			foreach (var layer in _before)
			{
				if (OpenIn(layer, path, written, access, share) is { } before)
					return before;
			}

			if (File.Exists(written))
				return new FileStream(written, mode, access, share);

			foreach (var layer in _after)
			{
				if (OpenIn(layer, path, written, access, share) is { } after)
					return after;
			}

			if (mode == FileMode.Open)
				return null;
		}
		else if (mode == FileMode.CreateNew && (Exists(_before, path) || Exists(_after, path)))
		{
			return null;
		}

		Directory.CreateDirectory(Path.GetDirectoryName(written)!);
		return new FileStream(written, mode, access, share);
	}

	// Opens a file of the game, as INspwPlatform.OpenFile does, or returns null if it cannot be opened.
	public Stream? Open(string path, FileMode mode, FileAccess access, FileShare share)
	{
		try
		{
			return OpenOrCreate(path, mode, access, share);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			return null;
		}
	}

	// The names of the files in a directory of the game that match a pattern, in every layer, each once (in any case and
	// either Unicode form, the first layer's), in the order of GameFiles.Find.
	public IReadOnlyList<string> Find(string directory, string pattern)
	{
		IEnumerable<string> names =
		[
			.. _before.SelectMany(layer => layer.Find(directory, pattern)),
			.. GameFiles.Find(_directory, directory, pattern),
			.. _after.SelectMany(layer => layer.Find(directory, pattern)),
		];
		return
		[
			.. names
				.DistinctBy(name =>
					name.Normalize(System.Text.NormalizationForm.FormC).ToUpperInvariant()
				)
				.Order(StringComparer.OrdinalIgnoreCase),
		];
	}
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

	public void Present(DirectDrawSurface7 primary) { }

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

	public void WriteSetting(string key, string value) { }
}

public static class winerror
{
	public const int S_OK = 0;
	public const int S_FALSE = 1;
	public const int E_FAIL = unchecked((int)0x80004005);
	public const int E_OUTOFMEMORY = unchecked((int)0x8007000E);
	public const int E_NOINTERFACE = unchecked((int)0x80004002);
	public const int E_NOTIMPL = unchecked((int)0x80004001);
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

public partial class Nspw
{
	private readonly INspwPlatform _platform;

	// The parts of the game that hold their own globals (docs/Refactoring.md, Splitting the partial class).
	public Sound Sound { get; }

	public Renderer Renderer { get; }

	public Nspw(INspwPlatform? platform = null)
	{
		_platform = platform ?? NullPlatform.Instance;
		Sound = new Sound(this);
		Renderer = new Renderer(this);
	}

	// DirectMusic's classes are the port's own (unknwn.cs); the platform creates the others.
	public int CoCreateInstance<T>(Guid rclsid, Guid riid, out T? ppv)
		where T : class
	{
		object? obj;
		var hr =
			rclsid == dmusici.CLSID_DirectMusicLoader
				? (obj = new DirectMusicLoader8()) is not null
					? winerror.S_OK
					: 0
				: rclsid == dmusici.CLSID_DirectMusicPerformance
					? (obj = new DirectMusicPerformance8()) is not null
						? winerror.S_OK
						: 0
					: _platform.CoCreateInstance(rclsid, riid, out obj);
		ppv = obj as T;
		return winerror.SUCCEEDED(hr) && ppv is null ? winerror.E_NOINTERFACE : hr;
	}
}
