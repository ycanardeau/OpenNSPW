using System.Diagnostics;
using OpenNspw.AI;

namespace OpenNspw.Arena;

// How a game of the arena ended, for the player's side.
internal enum Outcome
{
	Won,
	Drawn,
	Lost,

	// The time ran out, and decision gave no result.
	Unfinished,

	// The game failed, or showed a message box.
	Failed,
}

internal sealed record ArenaResult(
	int Scenario,
	int StartDate,
	string Player,
	Outcome Outcome,
	Side Side,
	int Frames,
	int TimeLeft,
	TimeSpan Elapsed,
	string? Error,
	StrikeCounts Strikes,
	IReadOnlyList<string>? Torpedoes = null
);

// One single-player game of a scenario, from a start date, which seeds rand (docs/Singleplayer.md), with a player on the
// player's side, played on the calling thread until decision gives its result.
internal sealed class ArenaGame
{
	// The frames a game goes on once its time has run out with no result, before it counts as unfinished.
	private const int FramesAfterTimeOut = 200;

	private readonly IPlayer _player;
	private readonly int _maxFrames;
	private readonly short _speed;
	private readonly UnitTrace? _trace;
	private readonly StrikeLog _strikes;
	private readonly TorpedoLog _torpedoes = new();
	private readonly int? _forkCheckAt;
	private GameFork? _fork;
	private bool _forkChecked;
	private TimeSpan _forkStepTime;
	private readonly ArenaPlatform _platform;
	private readonly Nspw _game;
	private int _frames;
	private int _framesAfterTimeOut;

	// The outcome of a result of decision for a side.
	private static Outcome OutcomeFor(GameResult result, Side side) =>
		(result, side) switch
		{
			(GameResult.None, _) => Outcome.Unfinished,
			(GameResult.Draw, _) => Outcome.Drawn,
			(GameResult.JapanWon or GameResult.UnitedStatesLost, Side.Japan) => Outcome.Won,
			(GameResult.UnitedStatesWon or GameResult.JapanLost, Side.UnitedStates) => Outcome.Won,
			_ => Outcome.Lost,
		};

	// From frame _forkCheckAt, a fork of the game (GameFork) steps once a frame beside it, and the first frame where the
	// two differ is written, with the fields that differ, to check that a fork plays as the game does.
	private void CheckFork()
	{
		if (_forkCheckAt is not { } at || _game.Renderer.FrameCount < at || _forkChecked)
			return;

		if (_fork is null)
		{
			_fork = new GameFork(_game);
			Console.WriteLine($"FORK at frame {_game.Renderer.FrameCount}");
			return;
		}

		var stopwatch = Stopwatch.StartNew();
		_fork.Step();
		_forkStepTime += stopwatch.Elapsed;
		var differences = GameFork.Differences(_game, _fork.Game);
		if (differences.Count > 0 || _game.Renderer.FrameCount - at >= 2000)
		{
			Console.WriteLine(
				differences.Count == 0
					? $"FORK the same for {_game.Renderer.FrameCount - at} frames, {_forkStepTime.TotalMilliseconds / (_game.Renderer.FrameCount - at):F2} ms a frame"
					: $"FORK differs at frame {_game.Renderer.FrameCount}: {string.Join(", ", differences)}"
			);
			_forkChecked = true;
			throw new ArenaStoppedException();
		}
	}

	private void OnFrame()
	{
		if (_game.Mode != GameMode.Battle)
			return;

		// The mouse rests in the middle of the battlefield, as a player's does: at the screen's edges, the game scrolls the
		// battlefield every frame (ScrollBattleArea), where the cursor starts.
		if (_frames++ == 0)
		{
			_game.SetCursorPos(all_head.CMBT_WIDTH / 2, all_head.CMBT_HEIGHT / 2);
			_game.GameSpeed = _speed;
		}

		if (
			_game.Result != GameResult.None
			|| _platform.MessageBoxes.Count > 0
			|| _frames > _maxFrames
		)
			throw new ArenaStoppedException();

		if (_game.BattleTime <= 0 && ++_framesAfterTimeOut > FramesAfterTimeOut)
			throw new ArenaStoppedException();

		_trace?.Write(_game);
		_strikes.Write(_game);
		_torpedoes.Write(_game);
		CheckFork();
		_player.Act(_game);
	}

	// maxFrames: the frames of the battle after which the game stops, unfinished. trace: units to write as the game goes.
	// strikes: whether to write what becomes of the player's strikes, which is counted anyway. speed: the game's speed,
	// as the player's keys set it, a frame running the speed and one more ticks.
	public ArenaGame(
		GameData data,
		int scenario,
		int startDate,
		IPlayer player,
		int maxFrames = int.MaxValue,
		UnitTrace? trace = null,
		bool strikes = false,
		short speed = 1,
		int? forkCheckAt = null
	)
	{
		_player = player;
		_maxFrames = maxFrames;
		_speed = speed;
		_forkCheckAt = forkCheckAt;
		_trace = trace;
		_strikes = new StrikeLog(strikes);
		_platform = new ArenaPlatform(
			data.Directory,
			data.SingleplayerDirectory,
			startDate,
			OnFrame
		);
		_game = new Nspw(_platform) { IsSingleplayer = true, ScenarioNumber = (short)scenario };
	}

	// Whether the fork check ended, the same or not, before the game did.
	public bool HasForkChecked => _forkChecked;

	// The game, to read its state once it has been played.
	public Nspw Game => _game;

	public ArenaResult Play()
	{
		var stopwatch = Stopwatch.StartNew();
		string? error = null;
		try
		{
			_game.WinMain(null, null, "", winuser.SW_SHOWNORMAL);
			error = "The game ended before its result.";
		}
		catch (ArenaStoppedException) { }
		catch (Exception e)
		{
			error = e.ToString();
		}

		if (_platform.MessageBoxes.Count > 0)
			error = $"Message box: {string.Join(", ", _platform.MessageBoxes)}";

		var outcome = error is null ? OutcomeFor(_game.Result, _game.LocalSide) : Outcome.Failed;
		return new ArenaResult(
			_game.ScenarioNumber,
			_platform.time(),
			_player.Name,
			outcome,
			_game.LocalSide,
			_frames,
			_game.BattleTime,
			stopwatch.Elapsed,
			error,
			_strikes.Counts(_game),
			_torpedoes.Rows
		);
	}
}
