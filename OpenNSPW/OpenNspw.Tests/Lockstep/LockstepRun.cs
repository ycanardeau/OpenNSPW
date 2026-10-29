namespace OpenNspw.Tests.Lockstep;

// Two whole games, the host and the guest, on a copy of the original's data and a LoopbackNetwork, stepped in turn by
// the test's thread. A round steps each game once, then records the trace. Scripts act between rounds, while neither
// game runs, so a script does the same thing at the same point every time.
internal sealed class LockstepRun : IDisposable
{
	public const int DefaultRoundLimit = 20_000;

	private readonly TemporaryDirectory _data = GameData.CreateCopy();
	private readonly TemporaryDirectory _user = GameData.CreateEmpty();
	private readonly LockstepScheduler _scheduler = new();
	private readonly TraceRecorder _trace;

	public LockstepGame Host { get; }

	public LockstepGame Guest { get; }

	public IReadOnlyList<LockstepGame> Games { get; }

	public int Round { get; private set; }

	public string Status =>
		string.Join(
			"; ",
			Games.Select(g =>
				$"{g.Name}: dialog {g.Game.MainDialog?.Id}, players {g.Game.ActivePlayerCount}, mode {g.Game.Mode}, rival mode {g.Game.RivalMode}, frames {g.FrameCount}, ended {g.Ended}, boxes [{string.Join(", ", g.MessageBoxes)}]"
			)
		);

	// drawsFromSpriteFiles: whether the games draw from the desktop app's sprite files (docs/GameSprites.md), rather than
	// from the sprite sheet's file. playsBundledFiles: whether they play the desktop app's embedded files, with an empty
	// user's folder (docs/BundledData.md), rather than a copy of the original's data; they then draw from the sprite
	// files, as the app embeds no sheet.
	public LockstepRun(bool drawsFromSpriteFiles = false, bool playsBundledFiles = false)
	{
		var network = new LoopbackNetwork();
		Host = new LockstepGame(_scheduler, network, _data.Path, "Alice");
		Guest = new LockstepGame(_scheduler, network, _data.Path, "Bob");
		Games = [Host, Guest];
		_trace = new TraceRecorder(Games);
		foreach (var game in Games)
		{
			game.DrawsFromSpriteFiles = drawsFromSpriteFiles || playsBundledFiles;
			if (playsBundledFiles)
				game.Files = new GameFileLayers(
					[],
					_user.Path,
					[new Desktop.BundledFiles("NSPW_NET")]
				);
			game.Start();
		}
	}

	public void Step()
	{
		foreach (var game in Games)
		{
			game.Step();
		}

		Round++;
		_trace.Record(Round);
	}

	// Steps until the condition holds, checked before each round. `beforeRound`, if any, acts before each round, such as a
	// Monkey.
	public void Until(
		Func<bool> condition,
		int limit = DefaultRoundLimit,
		Action? beforeRound = null
	)
	{
		var end = Round + limit;
		while (!condition())
		{
			beforeRound?.Invoke();
			if (Round >= end)
			{
				throw new TimeoutException(
					$"The condition did not hold within {limit} rounds: {Status}"
				);
			}

			if (Games.All(g => g.Ended))
			{
				throw new InvalidOperationException($"The games ended: {Status}");
			}

			Step();
		}
	}

	public void Rounds(int count)
	{
		var end = Round + count;
		Until(() => Round >= end, count + 1);
	}

	// Steps until the game has shown `count` more frames.
	public void Frames(LockstepGame game, int count)
	{
		var end = game.FrameCount + count;
		Until(() => game.FrameCount >= end);
	}

	// The trace, ended with a checkpoint.
	public IReadOnlyList<string> FinishTrace()
	{
		_trace.Finish(Round);
		return _trace.Lines;
	}

	public void Dispose()
	{
		_scheduler.Stop();
		foreach (var game in Games)
		{
			game.Stop();
		}

		_data.Dispose();
		_user.Dispose();
	}
}
