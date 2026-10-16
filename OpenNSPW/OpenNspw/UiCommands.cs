namespace OpenNspw;

// Where in a frame a command of the host's UI runs: where the battle takes the mouse's clicks on the battlefield
// (DrawBattleArea), on the minimap (DrawMinimap), on the switch of a carrier's view (UpdateDeckView), or on the combat
// menu (UpdateCombatMenu).
public enum UiCommandSite
{
	Battlefield,
	Minimap,
	DeckView,
	CombatMenu,
}

// The commands that the host's own UI gives the battle in place of the mouse's clicks (docs/Ui.md). Not part of the
// original. Each command runs where the battle takes the click it stands for, counts only where that click would count,
// and does what it does with the same functions, so that a battle ordered with commands sends the same orders as one
// ordered with the mouse.
public unsafe partial class Nspw
{
	// The items of the combat menu in the last frame (ListCombatMenu), for the host's UI to show: none if the menu was
	// not shown.
	public Array6<CombatMenuItem> ShownCombatMenu;
	public int ShownCombatMenuCount;

	private readonly Lock _uiCommandsLock = new();
	private readonly List<(UiCommandSite Site, Action Run)> _uiCommands = [];

	private void QueueUiCommand(UiCommandSite site, Action run)
	{
		lock (_uiCommandsLock)
		{
			_uiCommands.Add((site, run));
		}
	}

	// Runs the commands queued for a site, in order, on the game's thread; or drops them, as clicks are dropped, when they
	// do not count.
	private void RunUiCommands(UiCommandSite site, bool count)
	{
		List<Action> runs;
		lock (_uiCommandsLock)
		{
			runs = [.. _uiCommands.Where(c => c.Site == site).Select(c => c.Run)];
			_uiCommands.RemoveAll(c => c.Site == site);
		}

		if (count)
		{
			foreach (var run in runs)
			{
				run();
			}
		}
	}

	// The sound of a click on the battlefield (DrawBattleArea).
	private void PlayClickSound()
	{
		PlaySoundEffect( 0, SoundId.Click1 ,(double)(MAP_RIGHT+1), 0);
	}

	// Whether the selected unit is shown, so that its path is drawn and a click gives it a destination or a right click
	// deselects it (DrawUnitMarks): a parked plane only on the deck shown in the sidebar (IsOnShownDeck).
	private bool IsSelectedShown()
	{
		ref var unit = ref Units[SelectedUnit];
		return SelectedUnit!=0 && unit.IsUsed && (!(unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked) || IsOnShownDeck(ref unit)) && !IsEditingMap;
	}

	private void RunClickUnit(int m)
	{
		PlayClickSound();
		ref var unit = ref Units[m];
		if( m!=0 && unit.IsUsed && (unit.Side==LocalSide || unit.IsFound) && !(unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked) && CanClickUnit(ref unit) )
			{
			ClickUnit(m, default);
			}
	}

	private void RunClickLand(int x, int y)
	{
		PlayClickSound();
		var sprite = Units[0].Side==Side.Japan ? SpriteId.JapanUnits : SpriteId.UnitedStatesUnits;
		if( SelectedUnit!=0 && Units[SelectedUnit].Kind==UnitKind.Transport && AimLanding(sprite, x, y)
			&& CanClickUnit(ref Units[0]) && !(Units[0].PlaneState!=UnitState.Parked && x>=CMBT_WIDTH-1) )
			{
			ClickUnit(0, new WorldPosition(CameraPosition.X+Sprites[sprite].X, CameraPosition.Y-Sprites[sprite].Y));
			}
	}

	private void RunSetDestination(WorldPosition destination)
	{
		PlayClickSound();
		if( IsSelectedShown() && CanSetDestination(ref Units[SelectedUnit]) )
			{
			SetDestination(destination);
			}
	}

	private void RunRightClick()
	{
		PlayClickSound();
		if( IsSelectedShown() )
			{
			CancelSelection(SelectedUnit);
			}
		else
			{
			RestoreSelection();
			}
	}

	private void RunToggleDeckView()
	{
		PlayClickSound();
		ToggleDeckView();
	}

	private void RunChooseCombatMenuItem(CombatMenuItem item)
	{
		PlayClickSound();
		var isShown = false;
		for (var i = 0; i < ShownCombatMenuCount; i++)
		{
			isShown |= ShownCombatMenu[i] == item;
		}

		if( isShown && item!=CombatMenuSelection && CanOrder )
			{
			ChooseCombatMenuItem(item);
			}
	}

	// The commands of the host's UI, for the battle, in order: given on any thread, run on the game's.
	public sealed class UiCommands(Nspw game)
	{
		private readonly Nspw _game = game;

		// A left click on unit m on the battlefield, if the player sees it and the click counts (CanClickUnit). A parked
		// plane is on the deck, in the sidebar, not on the battlefield.
		public void ClickUnit(int m)
		{
			_game.QueueUiCommand(UiCommandSite.Battlefield, () => _game.RunClickUnit(m));
		}

		// A left click on the land at point (x, y) of the battlefield, where the selected transport lands, if it can.
		public void ClickLand(int x, int y)
		{
			_game.QueueUiCommand(UiCommandSite.Battlefield, () => _game.RunClickLand(x, y));
		}

		// A left click on the battlefield, that gives the selected unit a destination, if it can have one
		// (CanSetDestination).
		public void SetDestination(WorldPosition destination)
		{
			_game.QueueUiCommand(UiCommandSite.Battlefield, () => _game.RunSetDestination(destination));
		}

		// A right click on the battlefield: the selected unit is not selected any more, or, if none is, the one selected
		// before is selected again.
		public void RightClick()
		{
			_game.QueueUiCommand(UiCommandSite.Battlefield, _game.RunRightClick);
		}

		// A click held on the minimap, at cell (column, row) of the map.
		public void ShowMinimapCell(int column, int row)
		{
			_game.QueueUiCommand(UiCommandSite.Minimap, () => _game.ShowMinimapCell(column, row));
		}

		// A click on the switch of a carrier's or an air base's view, while its information is shown.
		public void ToggleDeckView()
		{
			_game.QueueUiCommand(UiCommandSite.DeckView, _game.RunToggleDeckView);
		}

		// A click on an item of the combat menu, if the menu shows it, it is not the item chosen already, and the player
		// can order.
		public void ChooseCombatMenuItem(CombatMenuItem item)
		{
			_game.QueueUiCommand(UiCommandSite.CombatMenu, () => _game.RunChooseCombatMenuItem(item));
		}
	}

	private UiCommands? _commands;

	public UiCommands Commands => _commands ??= new UiCommands(this);
}
