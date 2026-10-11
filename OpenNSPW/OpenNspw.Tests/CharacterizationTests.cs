using OpenNspw.Tests.Lockstep;
using Xunit;
using static OpenNspw.all_head;
using static OpenNspw.resource;
using static OpenNspw.windef;

namespace OpenNspw.Tests;

// Characterization traces (docs/Refactoring.md): scripts that play two whole games in lockstep, whose traces are
// committed. A refactoring must leave every trace unchanged.
public class CharacterizationTests
{
	// Connects the games through the connection dialogs, as the user does, starts them from the host and waits for the
	// title screen.
	private static void Connect(LockstepRun run)
	{
		run.Until(() => run.Host.Game.g_hDlg?.Id == IDD_ADDRESS_OVERRIDE && run.Guest.Game.g_hDlg?.Id == IDD_ADDRESS_OVERRIDE);
		run.Host.Post(g =>
		{
			g.TypeDlgItemText(g.g_hDlg!, IDC_ADDRESS_LINE2, "0");
			g.ClickDlgItem(g.g_hDlg!, IDOK);
		});
		run.Until(() => run.Host.Game.g_hDlg?.Id == IDD_MAIN_GAME);

		run.Guest.Post(g =>
		{
			g.ClickDlgItem(g.g_hDlg!, IDC_HOST_SESSION);
			g.TypeDlgItemText(g.g_hDlg!, IDC_ADDRESS_LINE1, "127.0.0.1");
			g.TypeDlgItemText(g.g_hDlg!, IDC_ADDRESS_LINE2, $"{ADDRESSOVERRIDE_PORT}");
			g.ClickDlgItem(g.g_hDlg!, IDOK);
		});
		run.Until(() => run.Host.Game.ActivePlayerCount == 2 && run.Guest.Game.ActivePlayerCount == 2 && run.Guest.Game.g_hDlg?.Id == IDD_MAIN_GAME);

		run.Host.Post(g => g.ClickDlgItem(g.g_hDlg!, IDC_START_GAME));
		run.Until(() => run.Host.Game.Mode == GameMode.Title && run.Guest.Game.Mode == GameMode.Title);
	}

	// Runs a script on two new games, and checks its trace.
	private static IReadOnlyList<string> Play(Action<LockstepRun> script)
	{
		using var run = new LockstepRun();
		script(run);
		Assert.Empty(run.Host.MessageBoxes);
		Assert.Empty(run.Guest.MessageBoxes);
		return run.FinishTrace();
	}

	private static void Title(LockstepRun run)
	{
		Connect(run);
		run.Frames(run.Host, 100);
	}

	// Clicks the left mouse button at a point of the screen, as the user does.
	private static void Click(LockstepRun run, LockstepGame game, int x, int y)
	{
		game.Game.SetCursorPos(x, y);
		game.Game.PostMouseInput(0, true);
		run.Frames(game, 2);
		game.Game.PostMouseInput(0, false);
		run.Frames(game, 2);
	}

	// From the title screen, the host chooses the scenario at `index` in the list of the setting screen (demo.cpp) and
	// starts the battle.
	private static void StartBattle(LockstepRun run, int index)
	{
		Connect(run);
		run.Frames(run.Host, 20);
		Click(run, run.Host, 512, 384);
		run.Until(() => run.Host.Game.Mode == GameMode.GameSetting && run.Host.Game.RivalMode == GameMode.GameSetting);
		Click(run, run.Host, 130, 150 + (index * 25) + 12);
		run.Until(() => run.Host.Game.Mode == GameMode.ConfigSetting && run.Host.Game.RivalMode == GameMode.ConfigSetting);
		Click(run, run.Host, 630 - 120 + 10, 700 + 12);
		run.Until(() => run.Host.Game.Mode == GameMode.Battle && run.Guest.Game.Mode == GameMode.Battle);
	}

	// From the title screen, the host chooses the user scenario at `index` in the list of the load dialog (win_proc.cpp,
	// the files of Scenario/) and starts the battle.
	private static void StartUserBattle(LockstepRun run, int index)
	{
		Connect(run);
		run.Frames(run.Host, 20);
		Click(run, run.Host, 512, 384);
		run.Until(() => run.Host.Game.Mode == GameMode.GameSetting && run.Host.Game.RivalMode == GameMode.GameSetting);
		Click(run, run.Host, 130, 150 + (8 * 25) + 12);
		run.Until(() => FileDialog(run.Host) is not null);
		var dialog = FileDialog(run.Host)!;
		run.Host.Post(g =>
		{
			g.SelectDlgItem(dialog, IDC_LIST, index);
			g.ClickDlgItem(dialog, IDOK);
		});
		run.Until(() => run.Host.Game.Mode == GameMode.ConfigSetting && run.Host.Game.RivalMode == GameMode.ConfigSetting);
		Click(run, run.Host, 630 - 120 + 10, 700 + 12);
		run.Until(() => run.Host.Game.Mode == GameMode.Battle && run.Guest.Game.Mode == GameMode.Battle);
	}

	private static HWND? FileDialog(LockstepGame game)
	{
		return game.Game.windows.FirstOrDefault(w => w is { Destroyed: false, Id: IDD_FILE_CONT });
	}

	// Plays a battle with a monkey on each side, for `frames` frames of the host, or until the battle ends.
	private static void Battle(LockstepRun run, uint seed, int frames)
	{
		Monkey[] monkeys = [new(run.Host, seed), new(run.Guest, seed + 1000)];
		var end = run.Host.FrameCount + frames;
		run.Until(() => run.Host.FrameCount >= end || run.Host.Game.Mode != GameMode.Battle, frames * 2, () =>
		{
			foreach (var monkey in monkeys)
			{
				monkey.Act();
			}
		});
	}

	// The point of the world that stands for a tile of the terrain: TileAt maps it back to the tile, and a landing point
	// clicked there is exactly that point (draw_cmbt_area.cpp).
	private static WorldPosition TilePosition(Nspw nspw, int column, int row)
	{
		ref Sprite tiles = ref nspw.Sprites[SpriteId.MapTiles];
		return new WorldPosition(MAP_LEFT + (column * tiles.Width), MAP_TOP - (row * tiles.Height));
	}

	// The tiles a transport's landing point can be on (draw_cmbt_area.cpp).
	private static bool IsLandable(ushort tile)
	{
		return tile is 2 or 5 or 7 or 8 or 9;
	}

	private static bool IsSea(Nspw nspw, int column, int row)
	{
		return column is >= 0 and < 256 && row is >= 0 and < 256 && nspw.MapTiles[row][column] == 0;
	}

	// The tile of the terrain under a point of the world, as the ships see it (chara_cont.cpp).
	private static (int Column, int Row) TileAt(Nspw nspw, WorldPosition position)
	{
		ref Sprite units = ref nspw.Sprites[SpriteId.JapanUnits];
		ref Sprite tiles = ref nspw.Sprites[SpriteId.MapTiles];
		return ((int)((position.X + (units.Width / 2) - MAP_LEFT) / tiles.Width), (int)((MAP_TOP - position.Y + (units.Height / 2)) / tiles.Height));
	}

	// Whether a ship nearer than 80 to a point would stop ship m there (CheckCourseAhead in chara_cont.cpp).
	private static bool IsShipNear(Nspw nspw, int m, WorldPosition position)
	{
		for (var n = 1; n <= nspw.MaxUnitId; n++)
		{
			ref Unit unit = ref nspw.Units[n];
			if (n != m && unit.IsUsed && unit.Category == UnitCategory.Ship && !(unit.Kind >= UnitKind.AirBase && unit.Kind <= UnitKind.Fortress) && Math.Abs(unit.Position.X - position.X) < 80 && Math.Abs(unit.Position.Y - position.Y) < 80)
			{
				return true;
			}
		}

		return false;
	}

	// Whether ship m can sail straight from where it is to a point: there is sea all the way and no other ship in the
	// way, except near the start, where a ship in a harbor lies on land.
	private static bool IsClearCourse(Nspw nspw, int m, WorldPosition to)
	{
		var from = nspw.Units[m].Position;
		var length = Math.Abs(to.X - from.X) + Math.Abs(to.Y - from.Y);
		var steps = (int)(length / 20) + 1;
		for (var i = (int)(80 / length * steps) + 1; i <= steps; i++)
		{
			var position = new WorldPosition(from.X + ((to.X - from.X) * i / steps), from.Y + ((to.Y - from.Y) * i / steps));
			var (column, row) = TileAt(nspw, position);
			if (!IsSea(nspw, column, row) || IsShipNear(nspw, m, position))
			{
				return false;
			}
		}

		return true;
	}

	// Whether a landing at a point would build nothing: a facility stands there (cont_fire in etc3.cpp), or another
	// transport is already landing there.
	private static bool IsTaken(Nspw nspw, WorldPosition position)
	{
		for (var m = 1; m <= nspw.MaxUnitId; m++)
		{
			ref Unit unit = ref nspw.Units[m];
			if (unit.IsUsed && unit.Kind >= UnitKind.AirBase && unit.Kind <= UnitKind.Fortress && Math.Abs(unit.Position.X - position.X) <= 20 && Math.Abs(unit.Position.Y - position.Y) <= 20)
			{
				return true;
			}

			if (unit.IsUsed && unit.Kind == UnitKind.Transport && unit.Target == nspw.MaxUnitId + 1 && unit.LandingX == (int)position.X && unit.LandingY == (int)position.Y)
			{
				return true;
			}
		}

		return false;
	}

	// The tile nearest to a point (by the sum of the distances along the axes) that satisfies a condition, if any.
	private static (int Column, int Row)? NearestTile(Nspw nspw, WorldPosition from, Func<int, int, bool> condition)
	{
		(int Column, int Row)? best = null;
		var bestDistance = double.MaxValue;
		for (var row = 0; row < 256; row++)
		{
			for (var column = 0; column < 256; column++)
			{
				var position = TilePosition(nspw, column, row);
				var distance = Math.Abs(position.X - from.X) + Math.Abs(position.Y - from.Y);
				if (distance < bestDistance && condition(column, row))
				{
					best = (column, row);
					bestDistance = distance;
				}
			}
		}

		return best;
	}

	// Moves the battle area so that its center is near a point, by clicking the minimap (unit_info_cont.cpp).
	private static void CenterOn(LockstepRun run, LockstepGame game, WorldPosition position)
	{
		var nspw = game.Game;
		ref Sprite minimap = ref nspw.Sprites[SpriteId.Minimap];
		var left = Math.Clamp(position.X - (CMBT_WIDTH / 2), MAP_LEFT, MAP_RIGHT - CMBT_WIDTH);
		var top = Math.Clamp(position.Y + (CMBT_HEIGHT / 2), MAP_BOTTOM + CMBT_HEIGHT, MAP_TOP);
		Click(run, game, CMBT_WIDTH + 8 + 5 + (int)((left - MAP_LEFT) / 80), CMBT_HEIGHT - minimap.Height + 8 + 5 + (int)((MAP_TOP - top) / 80));
	}

	// Clicks a point of the world on the battle area.
	private static void ClickAt(LockstepRun run, LockstepGame game, WorldPosition position)
	{
		var nspw = game.Game;
		Click(run, game, (int)(position.X - nspw.CameraPosition.X), (int)(nspw.CameraPosition.Y - position.Y));
	}

	// Selects a unit of the game's side on the battle area. A click on a unit while another is selected orders the
	// selected one, so another selected unit is deselected first, with the right button.
	private static void Select(LockstepRun run, LockstepGame game, int m)
	{
		var nspw = game.Game;
		if (nspw.SelectedUnit != 0 && nspw.SelectedUnit != m)
		{
			nspw.SetCursorPos(CMBT_WIDTH / 2, CMBT_HEIGHT / 2);
			nspw.PostMouseInput(1, true);
			run.Frames(game, 2);
			nspw.PostMouseInput(1, false);
			run.Frames(game, 2);
		}

		if (nspw.SelectedUnit != m)
		{
			ClickAt(run, game, nspw.Units[m].Position);
		}
	}

	// Orders a transport of the game's side to land its cargo, as a player does: selects it, clicks a free tile that it
	// can land on, next to the nearest sea that it can sail straight to and that fits on the battle area with it, and
	// moves it to that sea. An order clicked while the game takes none (CanOrder) is lost, so each click is repeated
	// until it takes. False if there is no such sea, such as for a transport hemmed in by its fleet.
	private static bool LandTransport(LockstepRun run, LockstepGame game, int m)
	{
		var nspw = game.Game;
		run.Until(() => !nspw.Units[m].IsSupplying);
		var start = nspw.Units[m].Position;
		bool IsFreeLandable(int column, int row) =>
			column is >= 0 and < 256 && row is >= 0 and < 256 && IsLandable(nspw.MapTiles[row][column]) && !IsTaken(nspw, TilePosition(nspw, column, row));
		bool IsOnScreen(int column, int row) =>
			Math.Abs(TilePosition(nspw, column, row).X - start.X) < CMBT_WIDTH - 200 && Math.Abs(TilePosition(nspw, column, row).Y - start.Y) < CMBT_HEIGHT - 200;
		if (NearestTile(nspw, start, (column, row) =>
			IsSea(nspw, column, row) && IsOnScreen(column, row) && IsClearCourse(nspw, m, TilePosition(nspw, column, row))
			&& (IsFreeLandable(column - 1, row) || IsFreeLandable(column + 1, row) || IsFreeLandable(column, row - 1) || IsFreeLandable(column, row + 1))) is not var (seaColumn, seaRow))
		{
			return false;
		}

		var (landColumn, landRow) = NearestTile(nspw, start, (column, row) => IsFreeLandable(column, row) && Math.Abs(column - seaColumn) + Math.Abs(row - seaRow) == 1)!.Value;
		var land = TilePosition(nspw, landColumn, landRow);
		var sea = TilePosition(nspw, seaColumn, seaRow);
		CenterOn(run, game, new WorldPosition((start.X + land.X) / 2, (start.Y + land.Y) / 2));
		for (var i = 0; i < 20 && nspw.Units[m].Target == 0; i++)
		{
			Select(run, game, m);
			ClickAt(run, game, land);
			run.Frames(game, 10);
		}

		for (var i = 0; i < 20 && !(Math.Abs(nspw.Units[m].PathX[0] - sea.X) < 120 && Math.Abs(nspw.Units[m].PathY[0] - sea.Y) < 120); i++)
		{
			Select(run, game, m);
			ClickAt(run, game, sea);
			run.Frames(game, 10);
		}

		Assert.Equal(nspw.MaxUnitId + 1, nspw.Units[m].Target);
		return true;
	}

	// Runs a script on two new games, and compares its trace with the committed one. Neither game may detect a desync
	// (first_r_error), which the games check every turn from the checksums they exchange.
	private static void Check(string name, Action<LockstepRun> script)
	{
		using var run = new LockstepRun();
		Exception? failure = null;
		try
		{
			script(run);
			Assert.Empty(run.Host.MessageBoxes);
			Assert.Empty(run.Guest.MessageBoxes);
			Assert.False(run.Host.Game.HasSavedDesync, "The host detected a desync.");
			Assert.False(run.Guest.Game.HasSavedDesync, "The guest detected a desync.");
		}
		catch (Exception e)
		{
			failure = e;
		}

		Traces.Check(name, run.FinishTrace(), failure);
	}

	[Fact]
	public void Title_screen()
	{
		Check("title", Title);
	}

	// The first scenarios of the setting screen. Their fleets start far apart, so these cover moving, the battle's
	// protocol and drawing more than fighting.
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public void Scenario_with_random_input(int index)
	{
		Check($"scenario-{index + 1}", run =>
		{
			StartBattle(run, index);
			Battle(run, (uint)(1 + index), 2000);
			Assert.Equal(GameMode.Battle, run.Host.Game.Mode);
		});
	}

	// The original's user scenarios tst3.dat and てすと.dat (Scenario/), whose fleets start close, so these cover fighting.
	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	public void User_scenario_with_random_input(int index)
	{
		Check($"user-scenario-{index}", run =>
		{
			StartUserBattle(run, index);
			Battle(run, (uint)(100 + index), 3000);
		});
	}

	// ミッドウェイ攻略１, whose host starts with transports: those with a clear course land their cargo, which builds bases.
	[Fact]
	public void Transports_landing()
	{
		Check("transports-landing", run =>
		{
			StartBattle(run, 5);
			var nspw = run.Host.Game;
			var transports = Enumerable.Range(1, nspw.MaxUnitId).Where(m => nspw.Units[m].IsUsed && nspw.Units[m].Kind == UnitKind.Transport && nspw.Units[m].Side == nspw.LocalSide).ToList();
			transports = [.. transports.Where(m => LandTransport(run, run.Host, m))];
			Assert.True(transports.Count >= 2, $"Only {transports.Count} transports could be ordered to land.");

			run.Until(() => transports.All(m => !nspw.Units[m].IsUsed || nspw.Units[m].Kind != UnitKind.Transport), 10000);
			run.Frames(run.Host, 1000);
		});
	}

	[Fact]
	public void Runs_are_deterministic()
	{
		var first = Play(Title);
		var second = Play(Title);
		Assert.Null(Traces.Difference(first, second));
	}
}
