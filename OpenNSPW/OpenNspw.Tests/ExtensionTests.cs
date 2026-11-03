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

		game.Commands.ClickUnit(ship);
		run.Frames(2);
		game.Commands.SetDestination(destination);
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
}
