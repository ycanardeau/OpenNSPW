using OpenNspw.AI;
using OpenNspw.Tests.Lockstep;
using Xunit;
using static OpenNspw.all_head;

namespace OpenNspw.Tests;

// The strong player (docs/StrongAI.md) gives orders that take, through the commands of the host's UI, in the first
// frames of single-player games. Whether it wins is the arena's to measure.
public class StrongPlayerTests
{
	// Plays `frames` frames of a scenario with the strong player on the player's side, which acts between frames, as the
	// arena and the desktop let it, the mouse resting in the middle of the battlefield.
	private static SingleplayerRun Play(int scenario, int frames)
	{
		var run = new SingleplayerRun(scenario);
		run.Frames(1);
		run.Game.SetCursorPos(CMBT_WIDTH / 2, CMBT_HEIGHT / 2);
		var player = new StrongPlayer();
		for (var frame = 0; frame < frames; frame++)
		{
			player.Act(run.Game);
			run.Frames(1);
		}

		Assert.Empty(run.Player.MessageBoxes);
		return run;
	}

	private static IEnumerable<int> OwnUnits(Nspw game, UnitKind kind)
	{
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			if (
				game.Units[m].IsUsed
				&& game.Units[m].Side == game.LocalSide
				&& game.Units[m].Kind == kind
			)
				yield return m;
		}
	}

	// In scenario 4, the carrier takes its escorts into its formation, arms its attackers with torpedoes, and sends out a
	// scout, which flies.
	[Fact]
	public void TheCarrierFormsArmsAndScouts()
	{
		using var run = Play(4, 300);
		var game = run.Game;
		var carrier = OwnUnits(game, UnitKind.Carrier).Single();
		Assert.All(
			OwnUnits(game, UnitKind.Cruiser).Concat(OwnUnits(game, UnitKind.Destroyer)),
			m => Assert.Equal(carrier, game.Units[m].GroupLeader)
		);
		var attackers = OwnUnits(game, UnitKind.Attacker).ToList();
		Assert.True(
			attackers.Count(m => game.Units[m].Weapon == FireKind.Torpedo) >= 5,
			"The attackers were not armed with torpedoes."
		);
		Assert.Contains(attackers, m => game.Units[m].PlaneState == UnitState.Flying);
	}

	// In scenario 104, each transport is given a landing point on Guadalcanal's coast, clicked on the battlefield moved
	// there with the minimap.
	[Fact]
	public void TheTransportsAreGivenLandingPoints()
	{
		using var run = Play(104, 200);
		var game = run.Game;
		var transports = OwnUnits(game, UnitKind.Transport).ToList();
		Assert.NotEmpty(transports);
		Assert.All(
			transports,
			m =>
			{
				ref var transport = ref game.Units[m];
				Assert.Equal(game.MaxUnitId + 1, transport.Target);
				Assert.InRange(transport.LandingX, -1500, -200);
				Assert.InRange(transport.LandingY, -600, 600);
			}
		);
	}

	// A route from Truk's harbor to Guadalcanal, on scenario 104's map, goes around the land: every leg is on the sea.
	[Fact]
	public void RoutesStayOnTheSea()
	{
		using var run = new SingleplayerRun(104);
		run.Frames(1);
		var game = run.Game;
		var from = new WorldPosition(-6800, 6550);
		var route = new SeaRoutes(game).Route(from, new WorldPosition(-880, -80), []);
		Assert.NotNull(route);
		Assert.True(route.Count > 1, "The route is a straight line across the land.");
		foreach (var to in route)
		{
			for (var i = 0; i <= 100; i++)
			{
				var point = new WorldPosition(
					from.X + ((to.X - from.X) * i / 100),
					from.Y + ((to.Y - from.Y) * i / 100)
				);
				Assert.True(
					UnitFacts.IsSea(game, point),
					$"({point.X:F0}, {point.Y:F0}) on the way to ({to.X:F0}, {to.Y:F0}) is land."
				);
			}

			from = to;
		}
	}
}
