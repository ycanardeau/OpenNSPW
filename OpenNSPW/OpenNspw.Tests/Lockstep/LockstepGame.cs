using System.Runtime.InteropServices;
using static OpenNspw.windef;
using static OpenNspw.winuser;

namespace OpenNspw.Tests.Lockstep;

// Thrown on a game's thread to end the game when its run is disposed.
internal sealed class LockstepStoppedException : Exception;

// Runs the games of a test one at a time, so that a run does the same thing every time. Each game runs WinMain on its
// own thread, but only while the driver (the test's thread) has stepped it, until the game yields: at the end of a
// frame, in Sleep, or when it waits for a message. Everything that enters a game (input, dialog actions, network
// messages) arrives while it is not running.
internal sealed class LockstepScheduler
{
	private static readonly TimeSpan StepTimeout = TimeSpan.FromSeconds(60);

	private readonly SemaphoreSlim _yielded = new(0);

	public bool Stopping { get; private set; }

	// On a game's thread: lets the driver run, and waits until the driver steps the game again.
	public void Yield(SemaphoreSlim resume)
	{
		_yielded.Release();
		resume.Wait();
		if (Stopping)
		{
			throw new LockstepStoppedException();
		}
	}

	// On a game's thread, when the game ends.
	public void Exit()
	{
		_yielded.Release();
	}

	// On the driver's thread: runs a game until it yields or ends.
	public void Step(SemaphoreSlim resume)
	{
		resume.Release();
		if (!_yielded.Wait(StepTimeout))
		{
			throw new TimeoutException("A game did not yield.");
		}
	}

	public void Stop()
	{
		Stopping = true;
	}
}

// A sound that plays nothing, and adds what the game does with it to the game's output.
internal sealed class LockstepSound(LockstepGame game, int id) : ISound
{
	private readonly LockstepGame _game = game;
	private readonly int _id = id;

	public void Play(bool loop)
	{
		_game.AddOutput($"play {_id} {loop}");
	}

	public void Stop()
	{
		_game.AddOutput($"stop {_id}");
	}

	public void SetVolume(int volume)
	{
		_game.AddOutput($"volume {_id} {volume}");
	}

	public void Rewind()
	{
		_game.AddOutput($"rewind {_id}");
	}
}

// A whole game, run by a LockstepScheduler, and the platform it runs on. Its clock advances by one millisecond each
// time the game reads it, and by the time it sleeps, so that time depends only on what the game does. What it outputs
// (frames, text, sounds, network messages, message boxes) is hashed, for the trace. Text is not rasterized, because
// fonts differ between machines.
internal sealed class LockstepGame : INspwPlatform
{
	private const uint StartTime = 0x00010000;

	// time(NULL): 2001-09-09, the same in every run.
	private const int StartDate = 1_000_000_000;

	private readonly LockstepScheduler _scheduler;
	private readonly string _dataDirectory;
	private readonly LoopbackDirectPlayFactory _directPlay;
	private readonly Dictionary<string, string> _settings = [];
	private readonly SemaphoreSlim _resume = new(0);
	private readonly Thread _thread;
	private Hash64 _output = new();
	private uint _now = StartTime;
	private int _soundCount;

	public string Name { get; }

	public Nspw Game { get; }

	public int FrameCount { get; private set; }

	public bool Ended { get; private set; }

	public Exception? Error { get; private set; }

	public List<string> MessageBoxes { get; } = [];

	private void Run()
	{
		_resume.Wait();
		try
		{
			if (!_scheduler.Stopping)
			{
				Game.WinMain(null, null, "", SW_SHOWNORMAL);
			}
		}
		catch (LockstepStoppedException)
		{
		}
		catch (Exception e)
		{
			Error = e;
		}
		finally
		{
			Ended = true;
			_scheduler.Exit();
		}
	}

	private void Yield()
	{
		_scheduler.Yield(_resume);
	}

	public void AddOutput(string text)
	{
		_output.Add(text);
	}

	public LockstepGame(LockstepScheduler scheduler, LoopbackNetwork network, string dataDirectory, string name)
	{
		_scheduler = scheduler;
		_dataDirectory = dataDirectory;
		_directPlay = new LoopbackDirectPlayFactory(network, (dpnid, data) =>
		{
			AddOutput($"send {dpnid:x8}");
			_output.Add(data);
		});
		Name = name;
		Game = new Nspw(this);
		_thread = new Thread(Run) { IsBackground = true, Name = name };
	}

	// The hash of what the game output since the last call.
	public ulong TakeOutput()
	{
		var value = _output.Value;
		_output = new Hash64();
		return value;
	}

	public void Start()
	{
		_thread.Start();
	}

	// On the driver's thread: runs the game until it yields or ends.
	public void Step()
	{
		if (!Ended)
		{
			_scheduler.Step(_resume);
		}

		if (Error is { } error)
		{
			throw new InvalidOperationException($"{Name}'s game failed.", error);
		}
	}

	// On the driver's thread, after LockstepScheduler.Stop: ends the game's thread.
	public void Stop()
	{
		if (!Ended)
		{
			_resume.Release();
			_thread.Join(TimeSpan.FromSeconds(10));
		}
	}

	// Runs a user's action on the game's thread, when its message loop dispatches it.
	public void Post(Action<Nspw> action)
	{
		Game.PostToGame(() => action(Game));
	}

	public uint timeGetTime()
	{
		return _now++;
	}

	public int time()
	{
		return StartDate;
	}

	public void Sleep(uint milliseconds)
	{
		_now += milliseconds;
		Yield();
	}

	public void WaitForMessage(object queue)
	{
		Monitor.Exit(queue);
		try
		{
			Yield();
		}
		finally
		{
			Monitor.Enter(queue);
		}
	}

	public void Present(DirectDrawSurface7 primary)
	{
		_output.Add(MemoryMarshal.AsBytes(primary.Pixels));
		FrameCount++;
		Yield();
	}

	public TextBitmap? RasterizeText(string text, int height, string faceName)
	{
		AddOutput($"text {height} {text}");
		return null;
	}

	public ISound? CreateSound(WAVEFORMATEX format, ReadOnlySpan<byte> data)
	{
		var id = _soundCount++;
		AddOutput($"sound {id}");
		_output.Add(data);
		return new LockstepSound(this, id);
	}

	public string? ReadSetting(string key)
	{
		return _settings.GetValueOrDefault(key) ?? (key.EndsWith("\\Player Name", StringComparison.Ordinal) ? Name : null);
	}

	public void WriteSetting(string key, string value)
	{
		_settings[key] = value;
	}

	public Stream? OpenFile(string path, FileMode mode, FileAccess access, FileShare share)
	{
		try
		{
			return new FileStream(GameFiles.Resolve(_dataDirectory, path), mode, access, share);
		}
		catch (IOException)
		{
			return null;
		}
	}

	public IReadOnlyList<string> FindFiles(string directory, string pattern)
	{
		return GameFiles.Find(_dataDirectory, directory, pattern);
	}

	public int CoCreateInstance(Guid rclsid, Guid riid, out object? ppv)
	{
		return _directPlay.CoCreateInstance(rclsid, riid, out ppv);
	}

	public int MessageBox(HWND? hWnd, string lpText, string lpCaption, uint uType)
	{
		AddOutput($"message box {lpText}");
		MessageBoxes.Add(lpText);
		return IDOK;
	}
}
