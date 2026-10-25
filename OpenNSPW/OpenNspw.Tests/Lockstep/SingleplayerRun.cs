namespace OpenNspw.Tests.Lockstep;

// One whole game playing a single-player scenario against the computer (docs/Singleplayer.md), on a copy of the
// original's data, stepped by the test's thread as LockstepRun steps two.
internal sealed class SingleplayerRun : IDisposable
{
	private readonly TemporaryDirectory _data = GameData.CreateCopy();
	private readonly LockstepScheduler _scheduler = new();

	public LockstepGame Player { get; }

	public Nspw Game => Player.Game;

	// setUp runs before the game starts, to set a trace. startDate: the time it starts at (time(NULL)), which seeds rand.
	public SingleplayerRun(int scenario, Action<Nspw>? setUp = null, int? startDate = null)
	{
		Player = new LockstepGame(_scheduler, new LoopbackNetwork(), _data.Path, "Alice");
		if (startDate is { } date)
			Player.StartDate = date;
		Game.IsSingleplayer = true;
		Game.ScenarioNumber = (short)scenario;
		Game.SingleplayerExecutable = File.ReadAllBytes(
			Path.Combine(GameData.FindSingleplayer(), "NSPW_122.exe")
		);
		Player.SingleplayerDirectory = GameData.FindSingleplayer();
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
	}
}
