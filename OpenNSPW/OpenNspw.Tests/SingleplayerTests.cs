using OpenNspw.Tests.Lockstep;
using Xunit;
using static OpenNspw.all_head;

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

	// A single-player game reads 1.22's own files: its japan_off.dat, without the island that NSPW NET's has at tiles 56
	// to 64 and 164 to 180, and its sprite sheet, whose title is 599 by 387.
	[Fact]
	public void TheGameReads122sFiles()
	{
		using var run = new SingleplayerRun(202);
		run.Frames(2);
		var game = run.Game;

		Assert.Equal(2, game.CurrentMap);
		Assert.Equal(0, game.MapTiles[164][62]);
		Assert.Equal(599, game.Sprites[SpriteId.TitleBackground].Width);
		Assert.Equal("tst_cg1.bmp", game.SpriteSheetName);
	}

	// Orders go to every unit selected, as in 1.22, also to the units of the player's side that have the other side's
	// numbers: in scenario 303, the fighters taken with a Japanese air base (41 to 43) are sent to another base together.
	[Fact]
	public void OrdersGoToCapturedPlanes()
	{
		using var run = new SingleplayerRun(303);
		var game = run.Game;

		// Away from the edges, which scroll the battle area.
		game.SetCursorPos(CMBT_WIDTH / 2, CMBT_HEIGHT / 2);
		run.Frames(2);
		Assert.Equal(Side.UnitedStates, game.LocalSide);
		var airBase = game.Units[21].Position;
		for (var m = 41; m <= 43; m++)
		{
			ref var plane = ref game.Units[m];
			Assert.Equal(UnitKind.Fighter, plane.Kind);
			plane.Side = Side.UnitedStates;
			plane.PlaneState = UnitState.Flying;
			plane.Position = new WorldPosition(airBase.X - 300, airBase.Y + (m - 42) * 60);
			game.Selections[1][m] = (short)(m - 40);
		}

		game.SelectionCount = 3;
		game.SelectedUnit = 41;
		// The base in the middle of the battle area, under the cursor.
		game.CameraPosition = new WorldPosition(airBase.X - (CMBT_WIDTH / 2), airBase.Y + (CMBT_HEIGHT / 2));
		run.Frames(1);
		Assert.Equal(airBase.X - (CMBT_WIDTH / 2), game.CameraPosition.X);
		game.PostMouseInput(0, true);
		run.Frames(2);
		game.PostMouseInput(0, false);
		run.Frames(2);

		Assert.All([41, 42, 43], m => Assert.Equal(21, game.Units[m].Carrier));
	}

	// In scenario 303 the Americans' fighters of type 1, the ones taken from Japan's air bases, are drawn from Japan's
	// sheet, as 1.22 draws them, in the game's drawing and in the UI's (ReadUnitSprites).
	[Fact]
	public void CapturedFightersAreDrawnAsJapans()
	{
		using var run = new SingleplayerRun(303);
		run.Frames(2);
		var game = run.Game;
		ref var fighter = ref game.Units[41];
		Assert.Equal(UnitKind.Fighter, fighter.Kind);
		fighter.Side = Side.UnitedStates;
		fighter.Variant = 1;
		var sprites = new SpriteId[256];

		game.ReadUnitSprites(sprites);

		Assert.Equal(SpriteId.JapanUnits, sprites[41]);
		fighter.Variant = 0;
		game.ReadUnitSprites(sprites);
		Assert.Equal(SpriteId.UnitedStatesUnits, sprites[41]);
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

	// Outside the map, 1.22 brings back the player's ships only: the computer's sail on, to their naval bases outside it,
	// such as scenario 304's, where they supply and get new carriers.
	[Fact]
	public void OnlyThePlayersShipsComeBackIntoTheMap()
	{
		using var run = new SingleplayerRun(4);
		run.Frames(2);
		var game = run.Game;
		int ShipOf(Side side)
		{
			for (var i = 1; i <= game.MaxUnitId; i++)
			{
				ref var unit = ref game.Units[i];
				if (unit.Side == side && unit.Kind is UnitKind.Cruiser or UnitKind.Destroyer)
				{
					return i;
				}
			}

			return 0;
		}

		var computers = ShipOf(game.CpuSide);
		var players = ShipOf(game.LocalSide);
		Assert.NotEqual(0, computers);
		Assert.NotEqual(0, players);
		var y = 0.0;
		foreach (var m in new[] { computers, players })
		{
			ref var ship = ref game.Units[m];
			ship.Position = new WorldPosition(all_head.MAP_RIGHT + 100, y);
			ship.PathX[0] = all_head.MAP_RIGHT + 800;
			ship.PathY[0] = y;
			ship.PathX[1] = all_head.MAP_RIGHT + 1;
			ship.EmergencyFlags[0] = 0;
			ship.IsStopping = false;
			ship.GroupLeader = 0;
			y += 400;
		}

		run.Frames(20);
		Assert.True(game.Units[computers].Position.X > all_head.MAP_RIGHT, $"The computer's ship is at x {game.Units[computers].Position.X}.");
		Assert.NotEqual(all_head.MAP_RIGHT - 40, game.Units[computers].EmergencyDestination.X);
		Assert.Equal(all_head.MAP_RIGHT - 40, game.Units[players].EmergencyDestination.X);
	}

	// The units that the battle creates in a single-player game, such as the bases that transports land, have NSPW
	// 1.22's data, from its set_unit_data, as the scenario's have; a full carrier still gets the planes added to it.
	[Fact]
	public void UnitsCreatedInTheBattleHave122sData()
	{
		using var run = new SingleplayerRun(3);
		run.Frames(2);
		var game = run.Game;

		var battleship = game.AddUnit(Side.Japan, UnitKind.Battleship, 0, 0, 0);
		Assert.NotEqual(0, battleship);
		Assert.Equal(41, game.Units[battleship].MaxHp);
		Assert.Equal(9000, game.Units[battleship].FuelInterval);

		var carrier = game.AddUnit(Side.UnitedStates, UnitKind.Carrier, 0, 400, 0);
		Assert.NotEqual(0, carrier);
		Assert.Equal(41, game.Units[carrier].MaxHp);
		Assert.Equal(12, game.Units[carrier].Capacity);

		var planes = game.Units[carrier].PlaneCount;
		Assert.Equal(1, game.AddPlane(Side.UnitedStates, UnitKind.Attacker, 0, carrier, game.Units[carrier].Capacity + 2, FireKind.Bomb));
		Assert.Equal(planes + game.Units[carrier].Capacity + 2, game.Units[carrier].PlaneCount);
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
		player.Platform.SingleplayerDirectory = GameData.FindSingleplayer();

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
