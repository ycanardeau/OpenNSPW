using OpenNspw.Tests.Lockstep;
using Xunit;

namespace OpenNspw.Tests;

// Whole single-player games: the port's battle with 1.22's scenarios, computer and victory conditions emulated
// (docs/Singleplayer.md).
public class SingleplayerTests
{
	public static TheoryData<int> Scenarios()
	{
		var data = new TheoryData<int>();
		foreach (var scenario in Singleplayer.Functions.Scenarios)
		{
			data.Add(scenario);
		}

		return data;
	}

	private static List<(int Unit, WorldPosition Position)> PositionsOf(Nspw game, Side side)
	{
		var positions = new List<(int, WorldPosition)>();
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			ref var unit = ref game.Units[m];
			if (unit.Side == side && unit.Category == UnitCategory.Ship)
			{
				positions.Add((m, unit.Position));
			}
		}

		return positions;
	}

	[Theory]
	[InlineData(3, Side.Japan)]
	[InlineData(4, Side.UnitedStates)]
	public void TheComputerOrdersAndMovesItsShips(int scenario, Side computer)
	{
		using var run = new SingleplayerRun(scenario);
		run.Frames(2);
		var game = run.Game;
		Assert.Equal(GameMode.Battle, game.Mode);
		Assert.Equal(computer, game.CpuSide);
		Assert.NotEqual(computer, game.LocalSide);
		var start = PositionsOf(game, computer);
		Assert.NotEmpty(start);
		var time = game.BattleTime;

		// cpu_strtgy's order stays in new_pp[0] until the next frame draws the battle.
		var orders = 0;
		for (var frame = 0; frame < 2000; frame++)
		{
			run.Frames(1);
			var unit = game.MoveOrders[0].Unit;
			if (unit != 0)
			{
				Assert.Equal(computer, game.Units[unit].Side);
				orders++;
			}
		}

		Assert.True(orders > 0, "The computer gave no move order.");
		Assert.Empty(run.Player.MessageBoxes);
		Assert.True(game.BattleTime < time, $"The time limit went from {time} to {game.BattleTime}.");
		var moved = start.Count(s => !game.Units[s.Unit].Position.Equals(s.Position));
		Assert.True(moved > 0, "None of the computer's ships moved.");
	}

	// decision shows the result itself, through the port's GDI stand-ins and wsprintf.
	[Fact]
	public void DecisionShowsTheResult()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		run.Game.Result = GameResult.JapanWon;
		run.Player.Texts.Clear();

		run.Frames(1);

		Assert.Contains("ゲーム終了", run.Player.Texts);
		Assert.Contains("日本海軍は勝利条件を達成した。", run.Player.Texts);
		Assert.Contains("日本海軍の勝利", run.Player.Texts);
	}

	// A computer's ship that land blocks plans its route again, as 1.22's chara_cont makes it, instead of stopping until the
	// computer gives it its next order. In scenario 7, destroyer 3 stopped for 880 frames without it.
	[Fact]
	public void TheComputersShipsGoAroundLand()
	{
		using var run = new SingleplayerRun(7);
		var game = run.Game;
		run.Frames(1);
		game.SetCursorPos(384, 384);
		var history = new Dictionary<int, Queue<WorldPosition>>();
		for (var round = 0; round < 20; round++)
		{
			run.Frames(100);
			for (var m = 1; m <= game.MaxUnitId; m++)
			{
				ref var unit = ref game.Units[m];
				if (unit.Side != game.CpuSide || unit.Category != UnitCategory.Ship || unit.Kind >= UnitKind.AirBase || unit.Hp <= 0)
				{
					continue;
				}

				var positions = history.TryGetValue(m, out var queue) ? queue : history[m] = new Queue<WorldPosition>();
				positions.Enqueue(unit.Position);
				if (positions.Count > 6)
				{
					var before = positions.Dequeue();
					var moved = Math.Abs(before.X - unit.Position.X) + Math.Abs(before.Y - unit.Position.Y);
					Assert.False(moved < 20 && unit.PathX[0] != all_head.MAP_RIGHT + 1, $"Ship {m} stopped at ({unit.Position.X:F0},{unit.Position.Y:F0}) with a path.");
				}
			}
		}
	}

	// Every scenario plays for a while with the computer, without failing.
	[Theory]
	[MemberData(nameof(Scenarios))]
	public void ScenarioPlays(int scenario)
	{
		using var run = new SingleplayerRun(scenario);
		run.Frames(1000);

		Assert.Equal(GameMode.Battle, run.Game.Mode);
		Assert.Empty(run.Player.MessageBoxes);
	}

	// A computer's ship supplies wherever it is, as in 1.22, whose computer starts supplying within 580 of its naval base:
	// NSPW NET's check for a naval base within 280 stopped it at once.
	[Fact]
	public void TheComputersShipsSupplyAwayFromTheBase()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;
		var m = 0;
		for (var i = 1; i <= game.MaxUnitId && m == 0; i++)
		{
			ref var unit = ref game.Units[i];
			if (unit.Side == game.CpuSide && unit.Category == UnitCategory.Ship && unit.Kind <= UnitKind.LightCarrier && unit.MaxHp >= 2)
			{
				m = i;
			}
		}

		Assert.NotEqual(0, m);
		for (var i = 1; i <= game.MaxUnitId; i++)
		{
			ref var unit = ref game.Units[i];
			if (unit.Side == game.CpuSide && unit.Kind == UnitKind.NavalBase)
			{
				Assert.True(Math.Abs(unit.Position.X - game.Units[m].Position.X) > 280 || Math.Abs(unit.Position.Y - game.Units[m].Position.Y) > 280);
			}
		}

		game.Units[m].Hp = game.Units[m].MaxHp - 1;
		game.Units[m].SupplyTime = 1;
		run.Frames(300);
		Assert.Equal(game.Units[m].MaxHp, game.Units[m].Hp);
	}

	// A battle as the desktop app shows it, in real time, saved as singleplayer_battle.png next to the test assembly.
	[Fact]
	public void TheBattleShows()
	{
		using var data = GameData.CreateCopy();
		using var player = new RunningGame(data.Path, "Alice");
		player.Game.IsSingleplayer = true;
		player.Game.ScenarioNumber = 3;
		player.Game.SingleplayerExecutable = File.ReadAllBytes(Path.Combine(GameData.FindSingleplayer(), "NSPW_122.exe"));

		// Away from the edges, which scroll the battle area.
		player.Game.SetCursorPos(384, 384);
		player.Start();

		var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(60);
		while (player.Platform.FrameCount < 200 && player.Error is null && DateTime.UtcNow < deadline)
		{
			Thread.Sleep(100);
		}

		Assert.Null(player.Error);
		Assert.True(player.Platform.FrameCount >= 200, $"The game showed {player.Platform.FrameCount} frames.");
		Assert.Equal(GameMode.Battle, player.Game.Mode);
		Assert.Empty(player.Platform.MessageBoxes);
		TitleScreenTests.WritePng(Path.Combine(AppContext.BaseDirectory, "singleplayer_battle.png"), player.Platform.LastFrame(), all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);
	}
}
