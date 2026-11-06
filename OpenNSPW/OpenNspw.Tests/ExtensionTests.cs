using OpenNspw.Tests.Lockstep;
using Xunit;

namespace OpenNspw.Tests;

// The extension API (docs/Extensions.md): what an extension sees of a game, and what it may do to it.
public sealed class ExtensionTests
{
	// Sees the player's actions, and the frames of the battle.
	private sealed class Observer : IGameExtension
	{
		public List<PlayerAction> Actions { get; } = [];

		public int Frames { get; private set; }

		public void PlayerActed(Nspw game, PlayerAction action) => Actions.Add(action);

		public void FrameEnded(Nspw game) => Frames++;
	}

	// Runs every frame with `ticks` ticks.
	private sealed class Ticks(short ticks) : IGameExtension
	{
		private readonly short _ticks = ticks;

		public short FrameStarting(Nspw game, short ticks) => _ticks;
	}

	// Runs every frame with 2 ticks, and logs the points of the frames: the sites only where the frame's orders are taken.
	private sealed class Points : IGameExtension
	{
		public List<string> Log { get; } = [];

		public short FrameStarting(Nspw game, short ticks)
		{
			Log.Add(nameof(FrameStarting));
			return 2;
		}

		public void TickStarting(Nspw game) => Log.Add(nameof(TickStarting));

		public void ResultChecking(Nspw game) => Log.Add(nameof(ResultChecking));

		public void Drawing(Nspw game) => Log.Add(nameof(Drawing));

		public void InputSite(Nspw game, ActionSite site, bool counts)
		{
			if (site == ActionSite.Orders)
				Log.Add($"{nameof(InputSite)}({site}, {counts})");
		}

		public void OrdersStarting(Nspw game) => Log.Add(nameof(OrdersStarting));

		public void FrameEnded(Nspw game) => Log.Add(nameof(FrameEnded));
	}

	// Answers what Windows' stack held where 1.22 reads a local it has not set, at one instruction.
	private sealed class Local(uint instruction, byte[] value) : IGameExtension
	{
		private readonly uint _instruction = instruction;
		private readonly byte[] _value = value;

		public byte[]? UninitializedLocal(Nspw game, uint instruction) =>
			instruction == _instruction ? _value : null;
	}

	// Plays the computer's side as 1.22's computer player, and counts the frames it plays.
	private sealed class CountingComputer : IComputerPlayer
	{
		private readonly ComputerPlayer122 _computer = new();

		public int Plans { get; private set; }

		public void Plan(Nspw game)
		{
			Plans++;
			_computer.Plan(game);
		}
	}

	private static int PlayersShip(Nspw game) =>
		Enumerable
			.Range(1, game.MaxUnitId)
			.First(m =>
				game.Units[m].IsUsed
				&& game.Units[m].Side == game.LocalSide
				&& game.Units[m].Category == UnitCategory.Ship
				&& game.Units[m].Kind <= UnitKind.Destroyer
			);

	// The commands of the host's UI reach the game as the player's actions, one for each click that counts: a ship
	// selected, then given a destination.
	[Fact]
	public void TheCommandsAreThePlayersActions()
	{
		var observer = new Observer();
		using var run = new SingleplayerRun(4, game => game.Extensions.Add(observer));
		var game = run.Game;
		run.Frames(2);
		var ship = PlayersShip(game);
		var destination = new WorldPosition(
			game.Units[ship].Position.X + 400,
			game.Units[ship].Position.Y
		);

		run.Player.Commands.ClickUnit(ship);
		run.Frames(2);
		run.Player.Commands.SetDestination(destination);
		run.Frames(2);

		Assert.Equal(
			[new PlayerAction.ClickUnit(ship), new PlayerAction.SetDestination(destination)],
			observer.Actions
		);
		Assert.True(observer.Frames >= 6);
	}

	// An extension sets the ticks of each frame of the battle.
	[Fact]
	public void AnExtensionSetsTheTicks()
	{
		using var run = new SingleplayerRun(4, game => game.Extensions.Add(new Ticks(3)));
		run.Frames(2);
		var tick = run.Game.Tick;

		run.Frames(5);

		Assert.Equal(tick + 15, run.Game.Tick);
	}

	// The points of a single-player frame of two ticks, in order: the result is checked after the tick that is not drawn,
	// and after the drawing, as 1.22 checks it; the frame's orders are taken, then applied.
	[Fact]
	public void TheFrameReachesThePointsInOrder()
	{
		var points = new Points();
		using var run = new SingleplayerRun(4, game => game.Extensions.Add(points));
		run.Frames(2);
		points.Log.Clear();

		run.Frames(1);

		Assert.Equal(
			[
				"FrameStarting",
				"TickStarting",
				"ResultChecking",
				"TickStarting",
				"Drawing",
				"ResultChecking",
				"InputSite(Orders, True)",
				"OrdersStarting",
				"FrameEnded",
			],
			points.Log
		);
	}

	// A local of 1.22 that the battle reads uninitialized is what an extension answers, or else the port's own.
	[Fact]
	public void AnExtensionGivesAnUninitializedLocal()
	{
		using var run = new SingleplayerRun(4);
		var game = run.Game;

		Assert.Equal(1.5, game.TakeStackLocal(0x413D23, 1.5));

		game.Extensions.Add(new Local(0x413D23, BitConverter.GetBytes(270.0)));

		Assert.Equal(270.0, game.TakeStackLocal(0x413D23, 1.5));
		Assert.Equal(1.5, game.TakeStackLocal(0x4173BF, 1.5));
	}

	// The game's computer player plays the computer's side, once a frame.
	[Fact]
	public void TheComputerPlayerPlaysEachFrame()
	{
		var computer = new CountingComputer();
		using var run = new SingleplayerRun(4, game => game.ComputerPlayer = computer);
		run.Frames(2);
		var plans = computer.Plans;

		run.Frames(5);

		Assert.Equal(plans + 5, computer.Plans);
	}

	// A single-player battle whose computer plays a side does not start without a computer player.
	[Fact]
	public void TheBattleDoesNotStartWithoutAComputerPlayer()
	{
		using var run = new SingleplayerRun(4, game => game.ComputerPlayer = null);

		var failure = Assert.Throws<InvalidOperationException>(() => run.Frames(1));

		Assert.Contains("ComputerPlayer", failure.InnerException?.Message);
	}
}
