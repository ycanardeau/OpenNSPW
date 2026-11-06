using Aigamo.Otsuki.Actors;

namespace OpenNspw.Benchmarks;

// What the game's thread posts to the benchmark's (SoloGame).
internal abstract record SoloMessage
{
	private SoloMessage() { }

	// A message box the game showed.
	public sealed record MessageShown(string Text) : SoloMessage;

	// The game's thread has stopped after a frame (ParkAfterFrame), and hands the game to the benchmark's.
	public sealed record Parked : SoloMessage;

	// What a question about the game, asked on its thread, answered, and the game's state then.
	public sealed record Answer(bool Value, string State) : SoloMessage;

	// What WinMain threw.
	public sealed record Failed(Exception Error) : SoloMessage;
}

// The platform of a game that plays alone, against a ScriptedRival, for benchmarks: the game's files in a folder, which
// it reads but never changes, a clock that advances by one frame at each frame, and nothing shown or played. It runs on
// the game's thread, and posts to the benchmark's mailbox (benchmark).
//
// Once ParkAfterFrame returns true at the end of a frame, the game's thread stops there until Dispose, and hands the game
// to the benchmark's thread (Parked), which runs the game's functions on its state.
internal sealed class SoloPlatform(string dataDirectory, Mailbox<SoloMessage> benchmark)
	: INspwPlatform,
		IDisposable
{
	// The time of a frame of the original (UpdateFrame), at the normal speed.
	private const int FrameMilliseconds = 50 + 5 + 1;

	private readonly string _dataDirectory = dataDirectory;
	private readonly Mailbox<SoloMessage> _benchmark = benchmark;

	// Where the parked game's thread waits for the game back.
	private readonly Mailbox<bool> _resumed = new();
	private bool _hasParked;
	private long _now = 1000;

	public ScriptedRival? Rival { get; private set; }

	public int FrameCount { get; private set; }

	// Whether the game's thread stops after this frame: on the game's thread, set by an action posted to it.
	public Func<bool> ParkAfterFrame { get; set; } = () => false;

	// A file opened for writing is a copy in memory, so that the game's saves leave the folder as it is.
	public Stream? OpenFile(string path, FileMode mode, FileAccess access, FileShare share)
	{
		var full = GameFiles.Resolve(_dataDirectory, path);
		try
		{
			if ((access & FileAccess.Write) == 0)
			{
				return new FileStream(full, mode, access, share);
			}

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

	public int CoCreateInstance(Guid rclsid, Guid riid, out object? ppv)
	{
		if (rclsid == dplay8.CLSID_DirectPlay8Peer)
		{
			ppv = Rival = new ScriptedRival();
		}
		else if (rclsid == dplay8.CLSID_DirectPlay8ThreadPool)
		{
			ppv = new ScriptedRivalThreadPool(() => Rival);
		}
		else if (rclsid == dplay8.CLSID_DirectPlay8Address)
		{
			ppv = new NullAddress();
		}
		else
		{
			ppv = null;
			return winerror.REGDB_E_CLASSNOTREG;
		}

		return winerror.S_OK;
	}

	public int MessageBox(HWND? hWnd, string lpText, string lpCaption, uint uType)
	{
		_benchmark.Post(new SoloMessage.MessageShown(lpText));
		return windef.IDOK;
	}

	public uint timeGetTime() => (uint)_now;

	public int time() => 0;

	public void Present(DirectDrawSurface7 primary)
	{
		_now += FrameMilliseconds;
		FrameCount++;
		if (!_hasParked && ParkAfterFrame())
		{
			_hasParked = true;
			_benchmark.Post(new SoloMessage.Parked());
			_resumed.Wait();
		}
	}

	public TextBitmap? RasterizeText(string text, int height, string faceName) => null;

	public ISound? CreateSound(WAVEFORMATEX format, ReadOnlySpan<byte> data) => null;

	public string? ReadSetting(string key) => null;

	public void WriteSetting(string key, string value) { }

	// Lets a parked game's thread go on, parking no more.
	public void Dispose()
	{
		_resumed.Post(true);
	}
}
