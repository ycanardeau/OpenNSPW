namespace OpenNspw;

// Where in a frame a command of the host's UI runs: where the battle takes the mouse's clicks on the battlefield
// (DrawBattleArea), on the minimap (DrawMinimap), on the switch of a carrier's view (UpdateDeckView), on the combat menu
// (UpdateCombatMenu), on the supply panel (UpdateSupplyRequest), or on the host's system menu (UpdateSystemMenu); or
// where the setting screens take them on the list of scenarios (UpdateScenarioList), on the buttons under it
// (UpdateScenarioPageButton, UpdateResumeButton, UpdateAutoSaveButton, UpdateSideSetting), or on the rules
// (UpdateConfigSetting).
public enum UICommandSite
{
	Battlefield,
	Minimap,
	DeckView,
	CombatMenu,
	SupplyPanel,
	SystemMenu,
	ScenarioList,
	ScenarioMenu,
	Rules,
}

// The commands that the host's own UI gives the battle in place of the mouse's clicks (docs/UI.md). Not part of the
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
	private readonly List<(UICommandSite Site, Action Run)> _uiCommands = [];

	private void QueueUICommand(UICommandSite site, Action run)
	{
		lock (_uiCommandsLock)
		{
			_uiCommands.Add((site, run));
		}
	}

	// Whether a site is the setting screens', not the battle's.
	private static bool IsSettingSite(UICommandSite site)
	{
		return site is UICommandSite.ScenarioList or UICommandSite.ScenarioMenu or UICommandSite.Rules;
	}

	// Runs the commands queued for a site, in order, on the game's thread; or drops them, as clicks are dropped, when they
	// do not count. The commands of the other screens' sites are dropped too: they were given for a screen that is gone.
	private void RunUICommands(UICommandSite site, bool count)
	{
		List<Action> runs;
		lock (_uiCommandsLock)
		{
			runs = [.. _uiCommands.Where(c => c.Site == site).Select(c => c.Run)];
			_uiCommands.RemoveAll(c => c.Site == site || IsSettingSite(c.Site) != IsSettingSite(site));
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
		if( m!=0 && unit.IsUsed && (unit.Side==LocalSide || unit.IsFound) && (!(unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked) || IsOnShownDeck(ref unit)) && CanClickUnit(ref unit) )
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

	// A click on the combat menu that waits for a frame in which it counts, and the frame it was given in.
	private (CombatMenuItem Item, int Since)? _waitingCombatMenu;

	// The combat menu's click counts while the menu shows the item, it is not the item chosen already, and the player can
	// order (UpdateCombatMenu). The player can order on and off with the turns, so a click waits for a frame in which it
	// counts, as a click the player gave a moment later would; for 40 frames at most, two seconds, and while the menu
	// still shows the item.
	private void RunWaitingCombatMenu()
	{
		if( _waitingCombatMenu is not var (item, since) )
			return;

		var isShown = false;
		for (var i = 0; i < ShownCombatMenuCount; i++)
		{
			isShown |= ShownCombatMenu[i] == item;
		}

		if( !isShown || item==CombatMenuSelection || FrameCount-since>40 )
			{
			_waitingCombatMenu=null;
			}
		else if( CanOrder )
			{
			_waitingCombatMenu=null;
			ChooseCombatMenuItem(item);
			}
	}

	private void RunChooseCombatMenuItem(CombatMenuItem item)
	{
		PlayClickSound();
		_waitingCombatMenu=(item, FrameCount);
	}

	private void RunSelectSupplyTarget(bool next)
	{
		PlayClickSound();
		if( next )
			SelectNextSupplyTarget();
		else
			SelectPreviousSupplyTarget();
	}

	private void RunRequestSupply()
	{
		PlayClickSound();
		if( GetSupplyRequestState()==SupplyRequestState.Available && Result==GameResult.None )
			{
			RequestSupplyUnit();
			}
	}

	// A click on the system menu that waits for a frame in which it counts, and the frame it was given in.
	private (MessageType Answer, int Since)? _waitingSystemMenu;

	// The system menu's click counts while the rival is in the battle and the player can order (UpdateSystemMenu). The
	// player can order on and off with the turns, so a click waits for a frame in which it counts, as a click the
	// player gave a moment later would; for 40 frames at most, two seconds.
	private void RunWaitingSystemMenu()
	{
		if( _waitingSystemMenu is not var (answer, since) )
			return;

		if( RivalMode!=GameMode.Battle || IsEditingMap || FrameCount-since>40 )
			{
			_waitingSystemMenu=null;
			}
		else if( CanOrder.Value==1 )
			{
			_waitingSystemMenu=null;
			AskSystemMenu(answer);
			}
	}

	private void RunAskSystemMenu(MessageType answer)
	{
		PlayClickSound();
		_waitingSystemMenu=(answer, FrameCount);
	}

	// Whether the host chooses on the scenarios, the guest being on them too (UpdateScenarioList and the buttons under it).
	private bool CanChooseScenario()
	{
		return IsHost && Mode==GameMode.GameSetting && RivalMode==GameMode.GameSetting;
	}

	// Whether the n-th scenario is in the list shown (ListScenarioNames).
	private bool IsListedScenario(int n)
	{
		Array12<int> len = default;
		Array12<Array128<byte>> ach = default;
		var m = -1;
		ListScenarioNames(ref m, ref len, ref ach);
		return n >= 0 && n <= m;
	}

	private void RunChooseScenario(int n)
	{
		if( CanChooseScenario() && IsListedScenario(n) )
			{
			ChooseScenario(n);
			}
	}

	private void RunConfirmScenario(int n)
	{
		if( CanChooseScenario() && IsListedScenario(n) )
			{
			_DP_DATA_1 dp_data_1 = default;
			ChooseScenario(n);
			LeaveGameSetting(ref dp_data_1);
			}
	}

	private void RunTurnScenarioPage()
	{
		if( CanChooseScenario() )
			{
			TurnScenarioPage();
			}
	}

	private void RunStartFromResume()
	{
		if( CanChooseScenario() && WasHost )
			{
			_DP_DATA_1 dp_data_1 = default;
			StartFromResume(ref dp_data_1);
			}
	}

	private void RunStartFromAutoSave()
	{
		if( CanChooseScenario() && HasAutoSave!=0 )
			{
			_DP_DATA_1 dp_data_1 = default;
			StartFromAutoSave(ref dp_data_1);
			}
	}

	private void RunSwitchHostSide()
	{
		if( CanChooseScenario() && !IsEditingMap )
			{
			SwitchHostSide();
			}
	}

	// Whether the host sets the rules (UpdateConfigSetting); the start and the way back need the guest on them too.
	private bool CanSetRules()
	{
		return IsHost && Mode==GameMode.ConfigSetting;
	}

	private void RunToggleDecision()
	{
		if( CanSetRules() )
			{
			ToggleDecision();
			}
	}

	private void RunChangeSupplyRate(int player, bool increase)
	{
		if( CanSetRules() && increase )
			{
			IncreaseSupplyRate(player);
			}
		else if( CanSetRules() && SupplyRates[player]!=0 )
			{
			DecreaseSupplyRate(player);
			}
	}

	private void RunChangeSwapTime(bool increase)
	{
		if( CanSetRules() && increase )
			{
			IncreaseSwapTime();
			}
		else if( CanSetRules() && SwapTime!=0 )
			{
			DecreaseSwapTime();
			}
	}

	private void RunToggleSwapRule()
	{
		if( CanSetRules() && SwapTime!=0 )
			{
			ToggleSwapRule();
			}
	}

	private void RunChangeInitialSupplyPoints(int player, bool increase)
	{
		if( CanSetRules() && increase )
			{
			IncreaseInitialSupplyPoints(player);
			}
		else if( CanSetRules() && InitialSupplyPoints[player]!=0 )
			{
			DecreaseInitialSupplyPoints(player);
			}
	}

	private void RunCycleArrivalControl()
	{
		if( CanSetRules() )
			{
			CycleArrivalControl();
			}
	}

	private void RunBackToScenarioSetting()
	{
		if( CanSetRules() && RivalMode==GameMode.ConfigSetting )
			{
			_DP_FLAG dp_flag = default;
			BackToScenarioSetting(ref dp_flag);
			}
	}

	private void RunStartBattle()
	{
		if( CanSetRules() && RivalMode==GameMode.ConfigSetting )
			{
			_DP_DATA_1 dp_data_1 = default;
			StartBattle(ref dp_data_1);
			}
	}

	// The commands of the host's UI, for the battle and the setting screens, in order: given on any thread, run on the
	// game's.
	public sealed class UICommands(Nspw game)
	{
		private readonly Nspw _game = game;

		// A left click on unit m on the battlefield, or on a parked plane on the deck shown in the sidebar, if the player
		// sees it and the click counts (CanClickUnit).
		public void ClickUnit(int m)
		{
			_game.QueueUICommand(UICommandSite.Battlefield, () => _game.RunClickUnit(m));
		}

		// A left click on the land at point (x, y) of the battlefield, where the selected transport lands, if it can.
		public void ClickLand(int x, int y)
		{
			_game.QueueUICommand(UICommandSite.Battlefield, () => _game.RunClickLand(x, y));
		}

		// A left click on the battlefield, that gives the selected unit a destination, if it can have one
		// (CanSetDestination).
		public void SetDestination(WorldPosition destination)
		{
			_game.QueueUICommand(UICommandSite.Battlefield, () => _game.RunSetDestination(destination));
		}

		// A right click on the battlefield: the selected unit is not selected any more, or, if none is, the one selected
		// before is selected again.
		public void RightClick()
		{
			_game.QueueUICommand(UICommandSite.Battlefield, _game.RunRightClick);
		}

		// A click held on the minimap, at cell (column, row) of the map.
		public void ShowMinimapCell(int column, int row)
		{
			_game.QueueUICommand(UICommandSite.Minimap, () => _game.ShowMinimapCell(column, row));
		}

		// A click on the switch of a carrier's or an air base's view, while its information is shown.
		public void ToggleDeckView()
		{
			_game.QueueUICommand(UICommandSite.DeckView, _game.RunToggleDeckView);
		}

		// A click on an item of the combat menu, if the menu shows it and it is not the item chosen already; it waits for a
		// frame in which the player can order (RunWaitingCombatMenu).
		public void ChooseCombatMenuItem(CombatMenuItem item)
		{
			_game.QueueUICommand(UICommandSite.CombatMenu, () => _game.RunChooseCombatMenuItem(item));
		}

		// A click on the previous or the next kind of reinforcement, while none is coming.
		public void SelectSupplyTarget(bool next)
		{
			_game.QueueUICommand(UICommandSite.SupplyPanel, () => _game.RunSelectSupplyTarget(next));
		}

		// A click that requests the chosen reinforcement, if it can be requested (GetSupplyRequestState).
		public void RequestSupply()
		{
			_game.QueueUICommand(UICommandSite.SupplyPanel, _game.RunRequestSupply);
		}

		// The host's system menu: back to the scenario selection (シナリオ選択へ), which the game asks first, while the
		// rival is in the battle; it waits for a frame in which the player can order (RunWaitingSystemMenu).
		public void ToScenarioSelection()
		{
			_game.QueueUICommand(UICommandSite.SystemMenu, () => _game.RunAskSystemMenu(MessageType.GoToGameSetting));
		}

		// The host's system menu: saving the battle to resume it, and back to the scenario selection (リジューム).
		public void Resume()
		{
			_game.QueueUICommand(UICommandSite.SystemMenu, () => _game.RunAskSystemMenu(MessageType.ResumeAndGoToGameSetting));
		}

		// The host's cursor on the n-th scenario of the list shown, which chooses it and shows it to the guest.
		public void ChooseScenario(int n)
		{
			_game.QueueUICommand(UICommandSite.ScenarioList, () => _game.RunChooseScenario(n));
		}

		// The host's click on the n-th scenario of the list shown: on to the rules with it.
		public void ConfirmScenario(int n)
		{
			_game.QueueUICommand(UICommandSite.ScenarioList, () => _game.RunConfirmScenario(n));
		}

		// The other page of the list (シナリオ切り替え).
		public void TurnScenarioPage()
		{
			_game.QueueUICommand(UICommandSite.ScenarioMenu, _game.RunTurnScenarioPage);
		}

		// The start from the resume-saved battle (リジュームスタート), if the player saved it as the host.
		public void StartFromResume()
		{
			_game.QueueUICommand(UICommandSite.ScenarioMenu, _game.RunStartFromResume);
		}

		// The start from the auto-saved battle (オートセーブスタート), if there is one.
		public void StartFromAutoSave()
		{
			_game.QueueUICommand(UICommandSite.ScenarioMenu, _game.RunStartFromAutoSave);
		}

		// The host and the guest swap their sides (操作対象の切り替え), but in the map editor.
		public void SwitchSides()
		{
			_game.QueueUICommand(UICommandSite.ScenarioMenu, _game.RunSwitchHostSide);
		}

		// The victory conditions on or off (勝敗判定の切り替え).
		public void ToggleDecision()
		{
			_game.QueueUICommand(UICommandSite.Rules, _game.RunToggleDecision);
		}

		// A player's increase of supply points (補給割当増加率) up or down: 0 for the host's, 1 for the guest's; not below 0.
		public void ChangeSupplyRate(int player, bool increase)
		{
			_game.QueueUICommand(UICommandSite.Rules, () => _game.RunChangeSupplyRate(player, increase));
		}

		// The time at which the supply points' rates swap (補給値反転地点) up or down, by 100; not below 0, never.
		public void ChangeSwapTime(bool increase)
		{
			_game.QueueUICommand(UICommandSite.Rules, () => _game.RunChangeSwapTime(increase));
		}

		// The rule of the swap (補給値反転ルール), once or every time, while the rates swap.
		public void ToggleSwapRule()
		{
			_game.QueueUICommand(UICommandSite.Rules, _game.RunToggleSwapRule);
		}

		// A player's supply points at the start (初期補給割当) up or down by 50: 0 for the host's, 1 for the guest's; not
		// below 0.
		public void ChangeInitialSupplyPoints(int player, bool increase)
		{
			_game.QueueUICommand(UICommandSite.Rules, () => _game.RunChangeInitialSupplyPoints(player, increase));
		}

		// The next rule of the reinforcements (増援ユニット制御).
		public void CycleArrivalControl()
		{
			_game.QueueUICommand(UICommandSite.Rules, _game.RunCycleArrivalControl);
		}

		// Back to the scenarios (シナリオセッティングへ戻る), while the guest is on the rules too.
		public void BackToScenarios()
		{
			_game.QueueUICommand(UICommandSite.Rules, _game.RunBackToScenarioSetting);
		}

		// The battle (ゲームスタート), while the guest is on the rules too.
		public void StartBattle()
		{
			_game.QueueUICommand(UICommandSite.Rules, _game.RunStartBattle);
		}
	}

	private UICommands? _commands;

	public UICommands Commands => _commands ??= new UICommands(this);
}
