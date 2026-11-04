namespace OpenNspw.Pacing;

// Steady speed (docs/SteadySpeed.md): a single-player battle that plays the same at every speed. 1.22 runs a frame on its
// timer, every 88 milliseconds, of speed + 1 ticks, and the computer, the player's orders and some of 1.22's rules go by
// the frame, not by the tick, so that another speed plays another battle. This runs every frame as speed 1 runs it, of 2
// ticks, and more of them instead: speed + 1 ticks every 88 milliseconds, 20 while Space is held, as fast as the game
// draws them at most. Paused, the game's timer runs its frames, which hold the battle (Nspw.IsBattleHeld): the player
// looks around and orders, and the battle goes on from where it was. It leaves alone a battle whose input an extension
// gives, such as a replay that plays.
public sealed class SteadySpeed : IGameExtension
{
	// The period of 1.22's timer, which drives its frames (Nspw.SingleplayerTimerPeriod), in milliseconds.
	private const double TimerPeriod = 88;

	// The ticks of every frame: speed 1's.
	public const short FrameTicks = 2;

	// The most milliseconds of play that the frames may fall behind while the game draws them slower than their pace: the
	// battle then runs as fast as the game draws, and does not hurry once it can keep up again.
	private const int MaxLag = 250;

	private uint? _lastNow;

	// The ticks that the time since the last frame owes, at the speed.
	private double _owed;

	// The ticks that 1.22 runs every 88 milliseconds: 20 while Space is held, none at speed 0, speed + 1 otherwise
	// (TryStartSingleplayerFrame).
	private static int TicksPerPeriod(Nspw game) =>
		game.Buttons.HasFlag(InputButtons.Space) ? 20
		: game.GameSpeed == 0 ? 0
		: game.GameSpeed + 1;

	private static bool Paces(Nspw game) => game.IsSingleplayer && !game.Extensions.HoldsInput;

	public bool? StartsFrame(Nspw game)
	{
		if (!Paces(game))
		{
			_lastNow = null;
			_owed = 0;
			return null;
		}

		var elapsed = _lastNow is { } last ? game.Now - last : 0;
		_lastNow = game.Now;
		var rate = TicksPerPeriod(game);
		if (rate == 0)
		{
			_owed = 0;
			return null;
		}

		_owed = Math.Min(
			_owed + elapsed * rate / TimerPeriod,
			Math.Max(FrameTicks, MaxLag * rate / TimerPeriod)
		);
		if (_owed < FrameTicks)
			return false;

		_owed -= FrameTicks;
		return true;
	}

	public bool? HoldsBattle(Nspw game) => Paces(game) && TicksPerPeriod(game) == 0 ? true : null;

	public short FrameStarting(Nspw game, short ticks) =>
		Paces(game) && ticks != 0 ? FrameTicks : ticks;
}
