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
	// paused for `pause` more frames at the 60th, and with steady speed if `isSteady`: a ship of the player's selected and
	// sent away, as the battle starts.
	private static Hashes Play(
		short speed,
		int frames,
		bool isSteady,
		bool holdsSpace = false,
		int pause = 0
	)
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
			if (pause > 0 && frame == 60)
			{
				game.GameSpeed = 0;
				run.Frames(pause);
				game.GameSpeed = speed;
			}
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

	// Paused, the frames hold the battle: it goes on from where it was, as if it had not been paused.
	[Fact]
	public void APauseLeavesTheBattleAsItWas()
	{
		var atSpeed1 = Play(1, Frames, isSteady: true);

		var paused = Play(1, Frames, isSteady: true, pause: 30);

		Assert.Equal(atSpeed1.ByTick, paused.ByTick);
	}

	// Without it, as 1.22 plays, a pause changes the battle: its frames run no tick, but the computer plays in them.
	[Fact]
	public void WithoutItAPauseChangesTheBattle()
	{
		var atSpeed1 = Play(1, Frames, isSteady: false);

		var paused = Play(1, Frames, isSteady: false, pause: 30);

		Assert.NotEqual(atSpeed1.ByTick, paused.ByTick);
	}

	// While paused, the player orders: a ship sent away stays where it is, and sails once the battle goes on. A replay of
	// it plays it again, its frames held as they were.
	[Fact]
	public void ThePlayerOrdersWhilePaused()
	{
		var stream = new MemoryStream();
		var recorder = new ReplayRecorder(_ => stream) { HashInterval = 1 };
		WorldPosition start,
			held,
			sailed;
		using (
			var run = new SingleplayerRun(
				4,
				game =>
				{
					game.Extensions.Add(new SteadySpeed());
					game.Extensions.Add(recorder);
				}
			)
		)
		{
			var game = run.Game;
			run.Frames(10);
			game.GameSpeed = 0;
			run.Frames(2);
			var ship = PlayersShip(game);
			start = game.Units[ship].Position;
			var destination = new WorldPosition(start.X + 400, start.Y);
			run.Player.Commands.ClickUnit(ship);
			run.Frames(2);
			run.Player.Commands.SetDestination(destination);
			run.Frames(20);
			held = game.Units[ship].Position;
			game.GameSpeed = 1;
			run.Frames(40);
			sailed = game.Units[ship].Position;
		}
		recorder.Dispose();

		Assert.Equal(start, held);
		Assert.True(sailed.X > start.X);

		var player = new ReplayPlayer(new MemoryStream(stream.ToArray()))
		{
			Speed = double.PositiveInfinity,
		};
		using var replayed = new SingleplayerRun(4, game => game.Extensions.Add(player));
		replayed.Frames(80);
		Assert.True(player.HasEnded);
		Assert.Null(player.Divergence);
		player.Dispose();
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

	// A replay records the speed that the player chose, which the frames' ticks no longer tell, and shows it again.
	[Fact]
	public void TheReplayShowsTheSteadySpeed()
	{
		var stream = new MemoryStream();
		var recorder = new ReplayRecorder(_ => stream);
		using (
			var run = new SingleplayerRun(
				3,
				game =>
				{
					game.Extensions.Add(new SteadySpeed());
					game.Extensions.Add(recorder);
				}
			)
		)
		{
			run.Frames(5);
			run.Game.GameSpeed = 4;
			run.Frames(10);
		}
		recorder.Dispose();

		var player = new ReplayPlayer(new MemoryStream(stream.ToArray()))
		{
			Speed = double.PositiveInfinity,
		};
		using var replayed = new SingleplayerRun(
			3,
			game =>
			{
				game.Extensions.Add(player);
				game.Extensions.Add(new SteadySpeed());
			}
		);
		replayed.Frames(3);
		var before = replayed.Game.GameSpeed;
		replayed.Frames(10);

		Assert.Equal(1, before);
		Assert.Equal(4, replayed.Game.GameSpeed);
		Assert.Null(player.Divergence);
		player.Dispose();
	}
}
