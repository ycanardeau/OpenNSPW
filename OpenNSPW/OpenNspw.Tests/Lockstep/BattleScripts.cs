using Xunit;
using static OpenNspw.all_head;
using static OpenNspw.resource;
using static OpenNspw.windef;

namespace OpenNspw.Tests.Lockstep;

// What scripts of whole games do: connecting the games, starting a battle, and playing it.
internal static class BattleScripts
{
	// Connects the games through the connection dialogs, as the user does, starts them from the host and waits for the
	// title screen.
	internal static void Connect(LockstepRun run)
	{
		run.Until(() => run.Host.Game.MainDialog?.Id == IDD_ADDRESS_OVERRIDE && run.Guest.Game.MainDialog?.Id == IDD_ADDRESS_OVERRIDE);
		run.Host.Post(g =>
		{
			g.TypeDlgItemText(g.MainDialog!, IDC_ADDRESS_LINE2, "0");
			g.ClickDlgItem(g.MainDialog!, IDOK);
		});
		run.Until(() => run.Host.Game.MainDialog?.Id == IDD_MAIN_GAME);

		run.Guest.Post(g =>
		{
			g.ClickDlgItem(g.MainDialog!, IDC_HOST_SESSION);
			g.TypeDlgItemText(g.MainDialog!, IDC_ADDRESS_LINE1, "127.0.0.1");
			g.TypeDlgItemText(g.MainDialog!, IDC_ADDRESS_LINE2, $"{ADDRESSOVERRIDE_PORT}");
			g.ClickDlgItem(g.MainDialog!, IDOK);
		});
		run.Until(() => run.Host.Game.ActivePlayerCount == 2 && run.Guest.Game.ActivePlayerCount == 2 && run.Guest.Game.MainDialog?.Id == IDD_MAIN_GAME);

		run.Host.Post(g => g.ClickDlgItem(g.MainDialog!, IDC_START_GAME));
		run.Until(() => run.Host.Game.Mode == GameMode.Title && run.Guest.Game.Mode == GameMode.Title);
	}

	// Clicks the left mouse button at a point of the screen, as the user does.
	internal static void Click(LockstepRun run, LockstepGame game, int x, int y)
	{
		game.Game.SetCursorPos(x, y);
		game.Game.PostMouseInput(0, true);
		run.Frames(game, 2);
		game.Game.PostMouseInput(0, false);
		run.Frames(game, 2);
	}

	// From the title screen, the host chooses the scenario at `index` in the list of the setting screen (demo.cpp) and
	// starts the battle.
	internal static void StartBattle(LockstepRun run, int index)
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
	internal static void StartUserBattle(LockstepRun run, int index)
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

	internal static HWND? FileDialog(LockstepGame game)
	{
		return game.Game.windows.FirstOrDefault(w => w is { Destroyed: false, Id: IDD_FILE_CONT });
	}

	// Plays a battle with a monkey on each side, for `frames` frames of the host, or until the battle ends.
	internal static void Battle(LockstepRun run, uint seed, int frames)
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
	internal static WorldPosition TilePosition(Nspw nspw, int column, int row)
	{
		ref Sprite tiles = ref nspw.Sprites[SpriteId.MapTiles];
		return new WorldPosition(World.Left + (column * tiles.Size.Width), World.Top - (row * tiles.Size.Height));
	}

	// The tiles a transport's landing point can be on (draw_cmbt_area.cpp).
	internal static bool IsLandable(ushort tile)
	{
		return tile is 2 or 5 or 7 or 8 or 9;
	}

	internal static bool IsSea(Nspw nspw, int column, int row)
	{
		return column is >= 0 and < 256 && row is >= 0 and < 256 && nspw.MapTiles[row][column] == 0;
	}

	// The tile of the terrain under a point of the world, as the ships see it (chara_cont.cpp).
	internal static (int Column, int Row) TileAt(Nspw nspw, WorldPosition position)
	{
		ref Sprite units = ref nspw.Sprites[SpriteId.JapanUnits];
		ref Sprite tiles = ref nspw.Sprites[SpriteId.MapTiles];
		return ((int)((position.X + (units.Size.Width / 2) - World.Left) / tiles.Size.Width), (int)((World.Top - position.Y + (units.Size.Height / 2)) / tiles.Size.Height));
	}

	// Whether a ship nearer than 80 to a point would stop ship m there (CheckCourseAhead in chara_cont.cpp).
	internal static bool IsShipNear(Nspw nspw, int m, WorldPosition position)
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
	internal static bool IsClearCourse(Nspw nspw, int m, WorldPosition to)
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
	internal static bool IsTaken(Nspw nspw, WorldPosition position)
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
	internal static (int Column, int Row)? NearestTile(Nspw nspw, WorldPosition from, Func<int, int, bool> condition)
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

	// Selects a unit of the game's side on the battle area. A click on a unit while another is selected orders the
	// selected one, so another selected unit is deselected first, with the right button.
	internal static void Select(BattleInput input, Nspw nspw, int m)
	{
		if (nspw.SelectedUnit != 0 && nspw.SelectedUnit != m)
		{
			input.RightClick();
		}

		if (nspw.SelectedUnit != m)
		{
			input.ClickUnit(m);
		}
	}

	// Orders a transport of the game's side to land its cargo, as a player does: selects it, clicks a free tile that it
	// can land on, next to the nearest sea that it can sail straight to and that fits on the battle area with it, and
	// moves it to that sea. An order clicked while the game takes none (CanOrder) is lost, so each click is repeated
	// until it takes. False if there is no such sea, such as for a transport hemmed in by its fleet.
	internal static bool LandTransport(LockstepRun run, LockstepGame game, BattleInput input, int m)
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
		input.CenterOn(new WorldPosition((start.X + land.X) / 2, (start.Y + land.Y) / 2));
		for (var i = 0; i < 20 && nspw.Units[m].Target == 0; i++)
		{
			Select(input, nspw, m);
			input.ClickLand(land);
			run.Frames(game, 10);
		}

		for (var i = 0; i < 20 && !(Math.Abs(nspw.Units[m].PathX[0] - sea.X) < 120 && Math.Abs(nspw.Units[m].PathY[0] - sea.Y) < 120); i++)
		{
			Select(input, nspw, m);
			input.SetDestination(sea);
			run.Frames(game, 10);
		}

		Assert.Equal(nspw.MaxUnitId + 1, nspw.Units[m].Target);
		return true;
	}
}
