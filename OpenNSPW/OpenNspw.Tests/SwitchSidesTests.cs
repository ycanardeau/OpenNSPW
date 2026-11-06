using OpenNspw.Singleplayer;
using OpenNspw.Tests.Lockstep;
using Xunit;

namespace OpenNspw.Tests;

// A single-player battle with the sides switched (BattleOptions.SwitchSides, docs/Options.md): the player commands the
// computer's side of 1.22's scenario, and the computer the player's, in fleets the game forms.
public sealed class SwitchSidesTests
{
	private static readonly BattleOptions Switched = new(SwitchSides: true);

	// Whether a computer's plane flew and a computer's ship moved, as each frame ends.
	private sealed class ComputersMoves : IGameExtension
	{
		private readonly Dictionary<int, WorldPosition> _starts = [];

		public bool PlaneFlew { get; private set; }

		public double FarthestShipMove { get; private set; }

		public void FrameEnded(Nspw game)
		{
			for (var m = 1; m <= game.MaxUnitId; m++)
			{
				ref var unit = ref game.Units[m];
				if (unit.Side != game.CpuSide)
					continue;

				if (unit.Category == UnitCategory.Plane && unit.PlaneState == UnitState.Flying)
					PlaneFlew = true;

				if (unit.Category != UnitCategory.Ship)
					continue;

				if (!_starts.TryGetValue(m, out var start))
					_starts[m] = start = unit.Position;

				FarthestShipMove = Math.Max(FarthestShipMove, (unit.Position - start).Length);
			}
		}
	}

	public static TheoryData<int> Scenarios()
	{
		var data = new TheoryData<int>();
		foreach (var scenario in ScenarioSelection.Sets.SelectMany(set => set.Scenarios))
			data.Add(scenario.Number);

		return data;
	}

	private static Side SideOf(int scenario) =>
		ScenarioSelection
			.Sets.SelectMany(set => set.Scenarios)
			.Single(s => s.Number == scenario)
			.Side == ScenarioSide.Japan
			? Side.Japan
			: Side.UnitedStates;

	// The player takes the other side; the computer, if it plays one, the player's, in fleets of its own units only, each
	// unit in one fleet at most, whose targets are the player's and stay in elements 1 to 17.
	[Theory]
	[MemberData(nameof(Scenarios))]
	public void TheComputerPlaysTheSideThePlayerLeaves(int scenario)
	{
		using var run = new SingleplayerRun(scenario, game => game.Options = Switched);
		run.Frames(1);
		var game = run.Game;

		var side = SideOf(scenario);
		Assert.True(game.AreSidesSwitched);
		Assert.NotEqual(side, game.LocalSide);
		if (game.CpuSide == Side.None)
			return;

		Assert.Equal(side, game.CpuSide);
		var members = new HashSet<short>();
		var fleets = 0;
		for (var f = 1; f <= 10; f++)
		{
			ref var fleet = ref game.ComputerFleets[f];
			if (fleet.Flagship == 0)
				continue;

			fleets++;
			Assert.Equal(game.CpuSide, game.Units[fleet.Flagship].Side);
			for (var k = 0; k < 18; k++)
			{
				var m = fleet.Members[k];
				if (m == 0)
					continue;

				Assert.True(members.Add(m), $"Unit {m} is in two fleets.");
				Assert.Equal(game.CpuSide, game.Units[m].Side);
			}

			Assert.Equal(0, fleet.Targets[0]);
			for (var k = 1; k <= 17; k++)
			{
				var t = fleet.Targets[k];
				if (t != 0)
					Assert.Equal(game.LocalSide, game.Units[t].Side);
			}
		}

		Assert.True(fleets > 0);
		Assert.Equal(0, game.ComputerFleets[0].Flagship);
	}

	// Plays scenario `scenario` with the sides switched for `frames` frames, and tells what the computer moved.
	private static (Nspw Game, ComputersMoves Moves) Play(int scenario, int frames)
	{
		var moves = new ComputersMoves();
		var run = new SingleplayerRun(
			scenario,
			game =>
			{
				game.Options = Switched;
				game.Extensions.Add(moves);
			}
		);
		using (run)
			run.Frames(frames);

		return (run.Game, moves);
	}

	// In scenario 5, the computer, now the United States, sails its carriers' fleets at Japan's.
	[Fact]
	public void TheComputerSailsItsFleets()
	{
		var (game, moves) = Play(5, 1500);

		Assert.Equal(Side.Japan, game.LocalSide);
		Assert.True(moves.FarthestShipMove > 500, $"{moves.FarthestShipMove}");
	}

	// In scenario 3, the computer, now the United States at Wake, flies the planes of its air base.
	[Fact]
	public void TheComputerFliesItsPlanes()
	{
		var (game, moves) = Play(3, 500);

		Assert.Equal(Side.Japan, game.LocalSide);
		Assert.True(moves.PlaneFlew);
	}
}
