using OpenNspw.Pacing;
using OpenNspw.Replay;
using OpenNspw.Tests.Lockstep;
using Xunit;

namespace OpenNspw.Tests;

// Steady speed (docs/SteadySpeed.md): a single-player battle plays the same at every speed.
public sealed class SteadySpeedTests
{
	private const int Frames = 150;

	// The battle's hash as each frame ends, by its tick.
	private sealed class Hashes : IGameExtension
	{
		public Dictionary<int, ulong> ByTick { get; } = [];

		public void FrameEnded(Nspw game) => ByTick[game.Tick] = ReplayFormat.HashBattle(game);
	}

	// The player's ship, of scenario 4, that the tests order.
	private static int PlayersShip(Nspw game) =>
		Enumerable
			.Range(1, game.MaxUnitId)
			.First(m =>
				game.Units[m].IsUsed
				&& game.Units[m].Side == game.LocalSide
				&& game.Units[m].Category == UnitCategory.Ship
				&& game.Units[m].Kind <= UnitKind.Destroyer
			);

	// Plays scenario 4 for `frames` frames at `speed`, with Space held from the 100th frame to the 120th if `holdsSpace`,
	// and with steady speed if `isSteady`: a ship of the player's selected and sent away, as the battle starts.
	private static Hashes Play(short speed, int frames, bool isSteady, bool holdsSpace = false)
	{
		var hashes = new Hashes();
		using var run = new SingleplayerRun(
			4,
			game =>
			{
				if (isSteady)
					game.Extensions.Add(new SteadySpeed());
				game.Extensions.Add(hashes);
			}
		);
		var game = run.Game;
		run.Frames(2);
		game.GameSpeed = speed;
		var ship = PlayersShip(game);
		run.Player.Commands.ClickUnit(ship);
		run.Frames(2);
		run.Player.Commands.SetDestination(
			new WorldPosition(game.Units[ship].Position.X + 400, game.Units[ship].Position.Y)
		);
		for (var frame = 4; frame < frames; frame++)
		{
			if (holdsSpace && frame == 100)
				game.Buttons |= InputButtons.Space;
			if (holdsSpace && frame == 120)
				game.Buttons &= ~InputButtons.Space;
			run.Frames(1);
		}

		return hashes;
	}

	// Each frame runs 2 ticks, at every speed and with Space held: the battle is the same, tick by tick.
	[Theory]
	[InlineData(4, false)]
	[InlineData(10, false)]
	[InlineData(300, false)]
	[InlineData(1, true)]
	public void EverySpeedPlaysTheSameBattle(short speed, bool holdsSpace)
	{
		var atSpeed1 = Play(1, Frames, isSteady: true);

		var atSpeed = Play(speed, Frames, isSteady: true, holdsSpace);

		Assert.Equal(atSpeed1.ByTick, atSpeed.ByTick);
	}

	// Without it, as 1.22 plays, speed 4 plays another battle than speed 1: its frames run 5 ticks, and the computer plays
	// a frame at a time.
	[Fact]
	public void WithoutItAnotherSpeedPlaysAnotherBattle()
	{
		var atSpeed1 = Play(1, Frames, isSteady: false);

		var atSpeed4 = Play(4, Frames * 2 / 5, isSteady: false);

		var common = atSpeed1.ByTick.Keys.Intersect(atSpeed4.ByTick.Keys).ToList();
		Assert.NotEmpty(common);
		Assert.Contains(common, tick => atSpeed1.ByTick[tick] != atSpeed4.ByTick[tick]);
	}

	// The frames go at the speed's pace: speed + 1 ticks every 88 milliseconds of the game's clock.
	[Theory]
	[InlineData(1)]
	[InlineData(4)]
	[InlineData(10)]
	public void TheFramesGoAtTheSpeedsPace(short speed)
	{
		using var run = new SingleplayerRun(4, game => game.Extensions.Add(new SteadySpeed()));
		var game = run.Game;
		run.Frames(2);
		game.GameSpeed = speed;
		run.Frames(2);
		var (now, tick) = (game.Now, game.Tick);

		run.Frames(100);

		var ticksPerPeriod = (game.Tick - tick) * 88.0 / (game.Now - now);
		Assert.InRange(ticksPerPeriod, (speed + 1) * 0.9, (speed + 1) * 1.1);
	}
}
