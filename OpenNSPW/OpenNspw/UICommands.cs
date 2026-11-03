using static OpenNspw.all_head;

namespace OpenNspw;

// The commands that the host's own UI gives the battle in place of the mouse's clicks (docs/UI.md), as an extension of
// the game (docs/Extensions.md). Not part of the original. A command is given on any thread, and runs on the game's
// where the battle takes the click it stands for (IGameExtension.InputSite); it counts only where that click would count,
// and does the player's action that the click does (Nspw.Do), so that a battle ordered with commands sends the same
// orders as one ordered with the mouse.
public sealed class UICommands : IGameExtension
{
	private readonly Lock _lock = new();
	private readonly List<(ActionSite Site, Action<Nspw> Run)> _queued = [];

	// The units brushed with the left button held (BrushUnit), to put in the selected unit's formation or to take out of
	// it: a click on each, one a frame, as the game takes one click on a unit a frame (SelectOrders[1]).
	private readonly Queue<(int Unit, bool IsAdded)> _brushed = new();

	// A click on the combat menu that waits for a frame in which it counts, and the frame it was given in.
	private (CombatMenuItem Item, int Since)? _waitingCombatMenu;

	// A click on the system menu that waits for a frame in which it counts, and the frame it was given in.
	private (MessageType Answer, int Since)? _waitingSystemMenu;

	// The items of the combat menu in the last frame (ListCombatMenuItems), for the host's UI to show: none if the menu
	// was not shown. On the game's thread.
	public CombatMenuItem[] ShownCombatMenu { get; private set; } = [];

	// The last move of the battlefield taken (ScrollBattlefield), for the host's UI, which shows the moves it gave before
	// the game takes them. On the game's thread.
	public int LastCameraMove { get; private set; }

	private void Queue(ActionSite site, Action<Nspw> run)
	{
		lock (_lock)
		{
			_queued.Add((site, run));
		}
	}

	// Whether a site is the setting screens', not the battle's.
	private static bool IsSettingSite(ActionSite site)
	{
		return site is ActionSite.ScenarioList or ActionSite.ScenarioMenu or ActionSite.Rules;
	}

	// The sound of a press of either button, anywhere on the screen, which the game plays where it takes the battlefield's
	// clicks, unless an order waits for its turn (DrawBattleArea).
	private static void PlayClickSound(Nspw game)
	{
		game.Sound.PlayInterfaceSound(SoundId.Click1);
	}

	private static void RunClickUnit(Nspw game, int m)
	{
		ref var unit = ref game.Units[m];
		if (
			m != 0
			&& unit.IsUsed
			&& (unit.Side == game.LocalSide || unit.IsFound)
			&& (
				!(unit.Category == UnitCategory.Plane && unit.PlaneState == UnitState.Parked)
				|| game.IsOnShownDeck(m)
			)
			&& game.CanClickUnit(m)
		)
		{
			game.Do(new PlayerAction.ClickUnit(m));
		}
	}

	// The next unit brushed that a click still puts in the selection, or takes out of it, as it was brushed for.
	private void RunBrushedUnits(Nspw game, bool counts)
	{
		if (!counts)
			return;

		while (_brushed.TryDequeue(out var brushed))
		{
			var (m, isAdded) = brushed;
			if (
				game.SelectedUnit != 0
				&& game.IsFormationMate(game.SelectedUnit, m)
				&& (game.Selections[1][m] == 0) == isAdded
				&& game.CanClickUnit(m)
			)
			{
				PlayClickSound(game);
				game.Do(new PlayerAction.ClickUnit(m));
				return;
			}
		}
	}

	private static void RunClickLand(Nspw game, int x, int y)
	{
		if (
			game.SelectedUnit != 0
			&& game.Units[game.SelectedUnit].Kind == UnitKind.Transport
			&& game.LandingMarkAt(x, y) is { } mark
			&& game.CanClickUnit(0)
			&& !(game.Units[0].PlaneState != UnitState.Parked && x >= CMBT_WIDTH - 1)
		)
		{
			game.Do(
				new PlayerAction.ClickUnit(
					0,
					new WorldPosition(
						game.CameraPosition.X + mark.X,
						game.CameraPosition.Y - mark.Y
					)
				)
			);
		}
	}

	private static void RunSetDestination(Nspw game, WorldPosition destination)
	{
		if (game.IsSelectedShown() && game.CanSetDestination(game.SelectedUnit))
		{
			game.Do(new PlayerAction.SetDestination(destination));
		}
	}

	private static void RunRightClick(Nspw game)
	{
		if (game.IsSelectedShown())
		{
			game.Do(new PlayerAction.CancelSelection(game.SelectedUnit));
		}
		else
		{
			game.Do(new PlayerAction.RestoreSelection());
		}
	}

	// The combat menu's click counts while the menu shows the item, it is not the item in effect already
	// (ReadActiveCombatMenuItem), and the player can order (UpdateCombatMenu). The player can order on and off with the
	// turns, so a click waits for a frame in which it counts, as a click the player gave a moment later would; for 40
	// frames at most, two seconds, and while the menu still shows the item.
	private void RunWaitingCombatMenu(Nspw game)
	{
		if (_waitingCombatMenu is not var (item, since))
			return;

		if (
			!ShownCombatMenu.Contains(item)
			|| item == game.ReadActiveCombatMenuItem()
			|| game.Renderer.FrameCount - since > 40
		)
		{
			_waitingCombatMenu = null;
		}
		else if (game.CanOrder)
		{
			_waitingCombatMenu = null;
			game.Do(new PlayerAction.ChooseCombatMenuItem(item));
		}
	}

	private static void RunRequestSupply(Nspw game)
	{
		if (
			game.ReadSupplyRequestState() == SupplyRequestState.Available
			&& game.Result == GameResult.None
		)
		{
			game.Do(new PlayerAction.RequestSupply());
		}
	}

	// Moves the battlefield by (dx, dy) pixels of the screen, kept on the map as the minimap keeps it (ShowMinimapCell).
	// Not a click of the original: the host's UI scrolls by dragging with the middle button. The camera is only what the
	// player sees, as the screen's edges and the minimap move it.
	private void MoveCamera(Nspw game, int dx, int dy, int id)
	{
		LastCameraMove = id;
		game.CameraPosition = new WorldPosition(
			Math.Clamp(game.CameraPosition.X + dx, World.Left, World.Right - CMBT_WIDTH),
			Math.Clamp(game.CameraPosition.Y - dy, World.Bottom + CMBT_HEIGHT, World.Top)
		);
	}

	// The system menu's click counts while the rival is in the battle and the player can order (UpdateSystemMenu). The
	// player can order on and off with the turns, so a click waits for a frame in which it counts, as a click the
	// player gave a moment later would; for 40 frames at most, two seconds.
	private void RunWaitingSystemMenu(Nspw game)
	{
		if (_waitingSystemMenu is not var (answer, since))
			return;

		if (
			game.RivalMode != GameMode.Battle
			|| game.IsEditingMap
			|| game.Renderer.FrameCount - since > 40
		)
		{
			_waitingSystemMenu = null;
		}
		else if (game.CanOrder.Value == 1)
		{
			_waitingSystemMenu = null;
			game.AskSystemMenu(answer);
		}
	}

	// Whether the host chooses on the scenarios, the guest being on them too (UpdateScenarioList and the buttons under it).
	private static bool CanChooseScenario(Nspw game)
	{
		return game.IsHost
			&& game.Mode == GameMode.GameSetting
			&& game.RivalMode == GameMode.GameSetting;
	}

	// Whether the n-th scenario is in the list shown (ReadScenarioNames).
	private static bool IsListedScenario(Nspw game, int n)
	{
		return n >= 0 && n < game.ReadScenarioNames().Length;
	}

	private static void RunChooseScenario(Nspw game, int n)
	{
		if (CanChooseScenario(game) && IsListedScenario(game, n))
		{
			game.Do(new SettingAction.ChooseScenario(n));
		}
	}

	private static void RunConfirmScenario(Nspw game, int n)
	{
		if (CanChooseScenario(game) && IsListedScenario(game, n))
		{
			game.Do(new SettingAction.ChooseScenario(n));
			game.Do(new SettingAction.ConfirmScenario());
		}
	}

	private static void RunTurnScenarioPage(Nspw game)
	{
		if (CanChooseScenario(game))
		{
			game.Do(new SettingAction.TurnScenarioPage());
		}
	}

	private static void RunStartFromResume(Nspw game)
	{
		if (CanChooseScenario(game) && game.WasHost)
		{
			game.Do(new SettingAction.StartFromResume());
		}
	}

	private static void RunStartFromAutoSave(Nspw game)
	{
		if (CanChooseScenario(game) && game.HasAutoSave != 0)
		{
			game.Do(new SettingAction.StartFromAutoSave());
		}
	}

	private static void RunSwitchHostSide(Nspw game)
	{
		if (CanChooseScenario(game) && !game.IsEditingMap)
		{
			game.Do(new SettingAction.SwitchHostSide());
		}
	}

	// Whether the host sets the rules (UpdateConfigSetting); the start and the way back need the guest on them too.
	private static bool CanSetRules(Nspw game)
	{
		return game.IsHost && game.Mode == GameMode.ConfigSetting;
	}

	private static void RunToggleDecision(Nspw game)
	{
		if (CanSetRules(game))
		{
			game.Do(new SettingAction.ToggleDecision());
		}
	}

	private static void RunChangeSupplyRate(Nspw game, int player, bool increase)
	{
		if (CanSetRules(game) && (increase || game.SupplyRates[player] != 0))
		{
			game.Do(new SettingAction.ChangeSupplyRate(player, increase));
		}
	}

	private static void RunChangeSwapTime(Nspw game, bool increase)
	{
		if (CanSetRules(game) && (increase || game.SwapTime != 0))
		{
			game.Do(new SettingAction.ChangeSwapTime(increase));
		}
	}

	private static void RunToggleSwapRule(Nspw game)
	{
		if (CanSetRules(game) && game.SwapTime != 0)
		{
			game.Do(new SettingAction.ToggleSwapRule());
		}
	}

	private static void RunChangeInitialSupplyPoints(Nspw game, int player, bool increase)
	{
		if (CanSetRules(game) && (increase || game.InitialSupplyPoints[player] != 0))
		{
			game.Do(new SettingAction.ChangeInitialSupplyPoints(player, increase));
		}
	}

	private static void RunCycleArrivalControl(Nspw game)
	{
		if (CanSetRules(game))
		{
			game.Do(new SettingAction.CycleArrivalControl());
		}
	}

	private static void RunBackToScenarios(Nspw game)
	{
		if (CanSetRules(game) && game.RivalMode == GameMode.ConfigSetting)
		{
			game.Do(new SettingAction.BackToScenarios());
		}
	}

	private static void RunStartBattle(Nspw game)
	{
		if (CanSetRules(game) && game.RivalMode == GameMode.ConfigSetting)
		{
			game.Do(new SettingAction.StartBattle());
		}
	}

	// Runs the commands given for a site, in order; or drops them, as clicks are dropped, when they do not count there,
	// or while an extension gives the player's input, such as a replay or a trace of 1.22 that the game follows
	// (IGameExtension.HoldsInput). The commands of the other screens' sites are dropped too: they were given for a screen
	// that is gone. Then what waits for the site: the units brushed on the battlefield, and the clicks on the combat menu
	// and the system menu that wait for a frame in which they count.
	public void InputSite(Nspw game, ActionSite site, bool counts)
	{
		List<Action<Nspw>> runs;
		lock (_lock)
		{
			runs = [.. _queued.Where(c => c.Site == site).Select(c => c.Run)];
			_queued.RemoveAll(c => c.Site == site || IsSettingSite(c.Site) != IsSettingSite(site));
		}

		if (site == ActionSite.CombatMenu)
			ShownCombatMenu = counts ? game.ListCombatMenuItems() : [];

		if (counts && !game.Extensions.HoldsInput)
		{
			foreach (var run in runs)
			{
				run(game);
			}
		}

		switch (site)
		{
			case ActionSite.Battlefield:
				RunBrushedUnits(game, counts);
				break;
			case ActionSite.CombatMenu:
				RunWaitingCombatMenu(game);
				break;
			case ActionSite.SystemMenu when counts:
				RunWaitingSystemMenu(game);
				break;
		}
	}

	// The press that the command stands for: its sound, at the battlefield's site, as the game plays it for any press,
	// and drops it as it drops the battlefield's clicks.
	private void Press()
	{
		Queue(ActionSite.Battlefield, PlayClickSound);
	}

	// A left click on unit m on the battlefield, or on a parked plane on the deck shown in the sidebar, if the player sees
	// it and the click counts (CanClickUnit).
	public void ClickUnit(int m)
	{
		Press();
		Queue(ActionSite.Battlefield, game => RunClickUnit(game, m));
	}

	// Unit m brushed with the left button held, after a press on a unit: put in the selected unit's formation, or taken
	// out of it, by a click on it, if a click does that.
	public void BrushUnit(int m, bool isAdded)
	{
		Queue(ActionSite.Battlefield, _ => _brushed.Enqueue((m, isAdded)));
	}

	// A left click on the land at point (x, y) of the battlefield, where the selected transport lands, if it can.
	public void ClickLand(int x, int y)
	{
		Press();
		Queue(ActionSite.Battlefield, game => RunClickLand(game, x, y));
	}

	// A left click on the battlefield, that gives the selected unit a destination, if it can have one
	// (CanSetDestination).
	public void SetDestination(WorldPosition destination)
	{
		Press();
		Queue(ActionSite.Battlefield, game => RunSetDestination(game, destination));
	}

	// A right click on the battlefield: the selected unit is not selected any more, or, if none is, the one selected
	// before is selected again.
	public void RightClick()
	{
		Press();
		Queue(ActionSite.Battlefield, RunRightClick);
	}

	// A click held on the minimap, at cell (column, row) of the map: pressed there, or dragged there.
	public void ShowMinimapCell(int column, int row, bool isPressed)
	{
		if (isPressed)
			Press();

		Queue(ActionSite.Minimap, game => game.ShowMinimapCell(column, row));
	}

	// The battlefield moved by (dx, dy) pixels, by a drag with the middle button, the move numbered `id`
	// (LastCameraMove); run with the minimap's, which move it too.
	public void ScrollBattlefield(int dx, int dy, int id)
	{
		Queue(ActionSite.Minimap, game => MoveCamera(game, dx, dy, id));
	}

	// A click on the switch of a carrier's or an air base's view, while its information is shown.
	public void ToggleDeckView()
	{
		Press();
		Queue(ActionSite.DeckView, game => game.Do(new PlayerAction.ToggleDeckView()));
	}

	// A click on an item of the combat menu, if the menu shows it and it is not the item in effect already; it waits for
	// a frame in which the player can order (RunWaitingCombatMenu).
	public void ChooseCombatMenuItem(CombatMenuItem item)
	{
		Press();
		Queue(ActionSite.CombatMenu, game => _waitingCombatMenu = (item, game.Renderer.FrameCount));
	}

	// A click on the previous or the next kind of reinforcement, while none is coming.
	public void SelectSupplyTarget(bool next)
	{
		Press();
		Queue(ActionSite.SupplyPanel, game => game.Do(new PlayerAction.SelectSupplyTarget(next)));
	}

	// A click that requests the chosen reinforcement, if it can be requested (ReadSupplyRequestState).
	public void RequestSupply()
	{
		Press();
		Queue(ActionSite.SupplyPanel, RunRequestSupply);
	}

	// The host's system menu: back to the scenario selection (シナリオ選択へ), which the game asks first, while the
	// rival is in the battle; it waits for a frame in which the player can order (RunWaitingSystemMenu).
	public void ToScenarioSelection()
	{
		Press();
		Queue(
			ActionSite.SystemMenu,
			game => _waitingSystemMenu = (MessageType.GoToGameSetting, game.Renderer.FrameCount)
		);
	}

	// The host's system menu: saving the battle to resume it, and back to the scenario selection (リジューム).
	public void Resume()
	{
		Press();
		Queue(
			ActionSite.SystemMenu,
			game =>
				_waitingSystemMenu = (
					MessageType.ResumeAndGoToGameSetting,
					game.Renderer.FrameCount
				)
		);
	}

	// The host's cursor on the n-th scenario of the list shown, which chooses it and shows it to the guest.
	public void ChooseScenario(int n)
	{
		Queue(ActionSite.ScenarioList, game => RunChooseScenario(game, n));
	}

	// The host's click on the n-th scenario of the list shown: on to the rules with it.
	public void ConfirmScenario(int n)
	{
		Queue(ActionSite.ScenarioList, game => RunConfirmScenario(game, n));
	}

	// The other page of the list (シナリオ切り替え).
	public void TurnScenarioPage()
	{
		Queue(ActionSite.ScenarioMenu, RunTurnScenarioPage);
	}

	// The start from the resume-saved battle (リジュームスタート), if the player saved it as the host.
	public void StartFromResume()
	{
		Queue(ActionSite.ScenarioMenu, RunStartFromResume);
	}

	// The start from the auto-saved battle (オートセーブスタート), if there is one.
	public void StartFromAutoSave()
	{
		Queue(ActionSite.ScenarioMenu, RunStartFromAutoSave);
	}

	// The host and the guest swap their sides (操作対象の切り替え), but in the map editor.
	public void SwitchSides()
	{
		Queue(ActionSite.ScenarioMenu, RunSwitchHostSide);
	}

	// The victory conditions on or off (勝敗判定の切り替え).
	public void ToggleDecision()
	{
		Queue(ActionSite.Rules, RunToggleDecision);
	}

	// A player's increase of supply points (補給割当増加率) up or down: 0 for the host's, 1 for the guest's; not below 0.
	public void ChangeSupplyRate(int player, bool increase)
	{
		Queue(ActionSite.Rules, game => RunChangeSupplyRate(game, player, increase));
	}

	// The time at which the supply points' rates swap (補給値反転地点) up or down, by 100; not below 0, never.
	public void ChangeSwapTime(bool increase)
	{
		Queue(ActionSite.Rules, game => RunChangeSwapTime(game, increase));
	}

	// The rule of the swap (補給値反転ルール), once or every time, while the rates swap.
	public void ToggleSwapRule()
	{
		Queue(ActionSite.Rules, RunToggleSwapRule);
	}

	// A player's supply points at the start (初期補給割当) up or down by 50: 0 for the host's, 1 for the guest's; not
	// below 0.
	public void ChangeInitialSupplyPoints(int player, bool increase)
	{
		Queue(ActionSite.Rules, game => RunChangeInitialSupplyPoints(game, player, increase));
	}

	// The next rule of the reinforcements (増援ユニット制御).
	public void CycleArrivalControl()
	{
		Queue(ActionSite.Rules, RunCycleArrivalControl);
	}

	// Back to the scenarios (シナリオセッティングへ戻る), while the guest is on the rules too.
	public void BackToScenarios()
	{
		Queue(ActionSite.Rules, RunBackToScenarios);
	}

	// The battle (ゲームスタート), while the guest is on the rules too.
	public void StartBattle()
	{
		Queue(ActionSite.Rules, RunStartBattle);
	}
}
