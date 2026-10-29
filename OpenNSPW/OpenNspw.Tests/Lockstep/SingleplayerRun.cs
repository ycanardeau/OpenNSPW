namespace OpenNspw.Tests.Lockstep;

// One whole game playing a single-player scenario against the computer (docs/Singleplayer.md), on a copy of the
// original's data, stepped by the test's thread as LockstepRun steps two.
internal sealed class SingleplayerRun : IDisposable
{
	private readonly TemporaryDirectory _data = GameData.CreateCopy();
	private readonly TemporaryDirectory _user = GameData.CreateEmpty();
	private readonly LockstepScheduler _scheduler = new();

	public LockstepGame Player { get; }

	public Nspw Game => Player.Game;

	// setUp runs before the game starts, to set a trace. startDate: the time it starts at (time(NULL)), which seeds rand.
	// drawsFromSpriteFiles: whether the game draws from the desktop app's sprite files (docs/GameSprites.md), rather than
	// from 1.22's sprite sheet. playsBundledFiles: whether it plays the desktop app's embedded files, with an empty user's
	// folder and no executable (docs/BundledData.md), rather than a copy of the original's data and Original/Nspw122, with
	// its executable for the tests' emulator; it then draws from the sprite files, as the app embeds no sheet.
	public SingleplayerRun(
		int scenario,
		Action<Nspw>? setUp = null,
		int? startDate = null,
		bool drawsFromSpriteFiles = false,
		bool playsBundledFiles = false
	)
	{
		Player = new LockstepGame(_scheduler, new LoopbackNetwork(), _data.Path, "Alice")
		{
			DrawsFromSpriteFiles = drawsFromSpriteFiles || playsBundledFiles,
		};
		if (startDate is { } date)
			Player.StartDate = date;
		Game.IsSingleplayer = true;
		Game.ScenarioNumber = (short)scenario;
		if (playsBundledFiles)
		{
			Player.Files = new GameFileLayers(
				[],
				_user.Path,
				[new Desktop.BundledFiles("Nspw122"), new Desktop.BundledFiles("NSPW_NET")]
			);
		}
		else
		{
			Game.SingleplayerExecutable = File.ReadAllBytes(
				Path.Combine(GameData.FindSingleplayer(), "NSPW_122.exe")
			);
			Player.SingleplayerDirectory = GameData.FindSingleplayer();
		}
		setUp?.Invoke(Game);
		Player.Start();
	}

	// Steps until the game has shown `count` more frames.
	public void Frames(int count)
	{
		var end = Player.FrameCount + count;
		while (Player.FrameCount < end)
		{
			if (Player.Ended)
			{
				throw new InvalidOperationException(
					$"The game ended after {Player.FrameCount} frames."
				);
			}

			Player.Step();
		}
	}

	public void Dispose()
	{
		_scheduler.Stop();
		Player.Stop();
		_data.Dispose();
		_user.Dispose();
	}
}
