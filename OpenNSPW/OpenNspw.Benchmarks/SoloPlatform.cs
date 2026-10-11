namespace OpenNspw.Benchmarks;

// The platform of a game that plays alone, against a ScriptedRival, for benchmarks: the game's files in a folder, which
// it reads but never changes, a clock that advances by one frame at each frame, and nothing shown or played.
//
// Once ParkAfterFrame returns true at the end of a frame, the game's thread stops there until Dispose, so that another
// thread can run the game's functions on its state.
internal sealed class SoloPlatform(string dataDirectory) : INspwPlatform, IDisposable
{
	// The time of a frame of the original (UpdateFrame), at the normal speed.
	private const int FrameMilliseconds = 50 + 5 + 1;

	private readonly string _dataDirectory = dataDirectory;
	private readonly ManualResetEventSlim _parked = new();
	private readonly ManualResetEventSlim _resumed = new();
	private long _now = 1000;

	public ScriptedRival? Rival { get; private set; }

	public List<string> MessageBoxes { get; } = [];

	public int FrameCount { get; private set; }

	public Func<bool> ParkAfterFrame { get; set; } = () => false;

	public bool IsParked => _parked.IsSet;

	public void WaitUntilParked(TimeSpan timeout)
	{
		if (!_parked.Wait(timeout))
		{
			throw new TimeoutException("The game did not reach the frame to stop at.");
		}
	}

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

	public IReadOnlyList<string> FindFiles(string directory, string pattern) => GameFiles.Find(_dataDirectory, directory, pattern);

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
		lock (MessageBoxes)
		{
			MessageBoxes.Add(lpText);
		}

		return windef.IDOK;
	}

	public uint timeGetTime() => (uint)Interlocked.Read(ref _now);

	public int time() => 0;

	public void Present(DirectDrawSurface7 primary)
	{
		Interlocked.Add(ref _now, FrameMilliseconds);
		FrameCount++;
		if (!_parked.IsSet && ParkAfterFrame())
		{
			_parked.Set();
			_resumed.Wait();
		}
	}

	public TextBitmap? RasterizeText(string text, int height, string faceName) => null;

	public ISound? CreateSound(WAVEFORMATEX format, ReadOnlySpan<byte> data) => null;

	public string? ReadSetting(string key) => null;

	public void WriteSetting(string key, string value)
	{
	}

	// Lets a parked game's thread go on.
	public void Dispose()
	{
		ParkAfterFrame = () => false;
		_resumed.Set();
	}
}
