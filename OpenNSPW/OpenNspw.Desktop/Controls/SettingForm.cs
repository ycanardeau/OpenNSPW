using Aigamo.Saruhashi;
using OpenNspw.Singleplayer;
using DrawingColor = System.Drawing.Color;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;

namespace OpenNspw.Desktop.Controls;

// The setting screens of a multiplayer game, and of the map editor (UpdateGameSetting): the scenarios, which the host
// chooses with the sides, and then the rules, which the host sets; the guest sees them as the host sends them. Drawn from
// the snapshot, in the game's words, and clicked with the commands of the host's UI. The arrow keys choose, and Enter goes
// on.
internal sealed class SettingForm : Form
{
	private const int RowHeight = 44;
	private const int RuleHeight = 54;

	// The cursor the game is given while the form is shown, as the original's screen takes the cursor's place too: on it,
	// a scenario of the list is chosen. Away from the original's texts and from the screen's edges, which scroll the map.
	public static readonly DrawingPoint GameCursor = new(1000, 740);

	private readonly GameSession _session;
	private readonly UiFonts _fonts;
	private readonly TabButton[] _tabs;
	private readonly ScenarioRow[] _rows;
	private readonly ScenarioDetails _details;
	private readonly ActionButton _switchSides;
	private readonly ActionButton _resume;
	private readonly ActionButton _autoSave;
	private readonly ActionButton _next;
	private readonly RuleRow[] _rules;
	private readonly ActionButton _back;
	private readonly ActionButton _start;
	private GameSnapshot _snapshot = new();
	private string[] _shownNames = [];
	private (short Number, int Side, string Description)? _shownScenario;

	private Nspw.UiCommands Commands => _session.Game.Commands;

	private bool IsChoosingScenario => _snapshot.Mode == GameMode.GameSetting;

	// Whether the host can act on the screen: the guest must be on it too, as the original's clicks need.
	private bool HasRival => _snapshot.RivalMode == _snapshot.Mode;

	// The row of the scenario chosen, on the page shown, or -1 (ShowScenarioList).
	private int ChosenRow => _snapshot.ScenarioNumber % 100 - 1;

	private int Page => _snapshot.ScenarioNumber <= 99 ? 0 : 1;

	// The player's side, from the host's: 0 for Japan.
	private int PlayerSide => _snapshot.IsHost ? _snapshot.HostSide : 1 - _snapshot.HostSide;

	private void Choose(int row)
	{
		if (_snapshot.IsHost && IsChoosingScenario && row >= 0 && row < _shownNames.Length)
			Commands.ChooseScenario(row);
	}

	private void TurnPage(int page)
	{
		if (_snapshot.IsHost && IsChoosingScenario && page != Page)
			Commands.TurnScenarioPage();
	}

	private void GoOn()
	{
		if (!_snapshot.IsHost)
			return;

		if (IsChoosingScenario && _next.Enabled)
			Commands.ConfirmScenario(ChosenRow);
		else if (!IsChoosingScenario && _start.Enabled)
			Commands.StartBattle();
	}

	private ActionButton AddButton(DrawingRectangle bounds, string text, Action onClick)
	{
		var button = new ActionButton(_fonts) { Bounds = bounds, Text = text };
		button.Click += (_, _) => onClick();
		Controls.Add(button);
		return button;
	}

	private ScenarioRow CreateRow(int row)
	{
		var scenario = new ScenarioEntry(0, "", ScenarioSide.Japan, "", []);
		var control = new ScenarioRow(_fonts, scenario) { Bounds = new DrawingRectangle(48, 200 + row * RowHeight, 432, RowHeight) };
		control.Click += (_, _) => Choose(row);
		Controls.Add(control);
		return control;
	}

	private TabButton CreateTab(int page)
	{
		var tab = new TabButton(_fonts) { Bounds = new DrawingRectangle(48 + page * 112, 112, 96, 32), Text = Texts.Page(page) };
		tab.Click += (_, _) => TurnPage(page);
		Controls.Add(tab);
		return tab;
	}

	private RuleRow CreateRule(int index, string name)
	{
		var rule = new RuleRow(_fonts, name) { Bounds = new DrawingRectangle(48, 200 + index * RuleHeight, 432, RuleHeight), BackColor = DrawingColor.Transparent };
		Controls.Add(rule);
		return rule;
	}

	// A rule set with a button each way, that goes down and up.
	private RuleRow CreateStepRule(int index, string name, Action<bool> change)
	{
		var rule = CreateRule(index, name);
		rule.AddButton("+", 36, () => change(true));
		rule.AddButton("−", 36, () => change(false));
		return rule;
	}

	// A rule set with one button, that goes to the next choice.
	private RuleRow CreateChoiceRule(int index, string name, Action change)
	{
		var rule = CreateRule(index, name);
		rule.AddButton(Texts.Change, 84, change);
		return rule;
	}

	public SettingForm(GameSession session, UiFonts fonts)
	{
		_session = session;
		_fonts = fonts;
		Size = new DrawingSize(all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);
		BackColor = DrawingColor.Transparent;
		KeyPreview = true;

		_tabs = [.. Enumerable.Range(0, 2).Select(CreateTab)];
		_rows = [.. Enumerable.Range(0, 9).Select(CreateRow)];
		_details = new ScenarioDetails(fonts) { Bounds = new DrawingRectangle(504, 200, 472, 460) };
		Controls.Add(_details);
		_switchSides = AddButton(new DrawingRectangle(380, 622, 100, 32), Texts.Switch, () => Commands.SwitchSides());
		_switchSides.FontSize = 14;
		_resume = AddButton(new DrawingRectangle(48, 688, 150, 44), Texts.Resume, () => Commands.StartFromResume());
		_autoSave = AddButton(new DrawingRectangle(210, 688, 150, 44), Texts.AutoSave, () => Commands.StartFromAutoSave());
		_next = AddButton(new DrawingRectangle(826, 688, 150, 44), Texts.Next, GoOn);
		_next.IsPrimary = true;

		// The rules, in the original's order (UpdateConfigSetting).
		_rules =
		[
			CreateChoiceRule(0, Texts.Decision, () => Commands.ToggleDecision()),
			CreateStepRule(1, Texts.HostSupplyRate, increase => Commands.ChangeSupplyRate(0, increase)),
			CreateStepRule(2, Texts.GuestSupplyRate, increase => Commands.ChangeSupplyRate(1, increase)),
			CreateStepRule(3, Texts.SwapTime, Commands.ChangeSwapTime),
			CreateChoiceRule(4, Texts.SwapRule, () => Commands.ToggleSwapRule()),
			CreateStepRule(5, Texts.HostInitialSupplyPoints, increase => Commands.ChangeInitialSupplyPoints(0, increase)),
			CreateStepRule(6, Texts.GuestInitialSupplyPoints, increase => Commands.ChangeInitialSupplyPoints(1, increase)),
			CreateChoiceRule(7, Texts.ArrivalControl, () => Commands.CycleArrivalControl()),
		];
		_back = AddButton(new DrawingRectangle(48, 688, 150, 44), Texts.Back, () => Commands.BackToScenarios());
		_start = AddButton(new DrawingRectangle(826, 688, 150, 44), Texts.Start, GoOn);
		_start.IsPrimary = true;
	}

	// The list's scenarios, when the page shown changes.
	private void UpdateRows(GameSnapshot snapshot)
	{
		if (!snapshot.ScenarioNames.SequenceEqual(_shownNames))
		{
			_shownNames = snapshot.ScenarioNames;
			var first = Page * 100 + 1;
			for (var i = 0; i < _rows.Length; i++)
			{
				if (i < _shownNames.Length)
					_rows[i].Scenario = new ScenarioEntry(first + i, GameTexts.Of(_shownNames[i]), ScenarioSide.Japan, "", []);
			}
		}

		for (var i = 0; i < _rows.Length; i++)
		{
			_rows[i].Visible = IsChoosingScenario && i < _shownNames.Length;
			_rows[i].IsSelected = i == ChosenRow;
			_rows[i].Enabled = snapshot.IsHost;
		}

		for (var i = 0; i < _tabs.Length; i++)
		{
			_tabs[i].Visible = IsChoosingScenario;
			_tabs[i].IsSelected = i == Page;
			_tabs[i].Enabled = snapshot.IsHost;
		}
	}

	// What the screen says about the scenario chosen, when it changes; nothing until one is chosen.
	private void UpdateDetails(GameSnapshot snapshot)
	{
		var description = string.Concat(snapshot.ScenarioDescription);
		var shown = (snapshot.ScenarioNumber, PlayerSide, description);
		if (_shownScenario == shown)
			return;

		_shownScenario = shown;
		var row = ChosenRow;
		_details.Scenario = row >= 0 && row < _shownNames.Length
			? new ScenarioEntry(
				snapshot.ScenarioNumber,
				GameTexts.Of(_shownNames[row]),
				PlayerSide == 0 ? ScenarioSide.Japan : ScenarioSide.UnitedStates,
				Texts.SideName(PlayerSide),
				GameTexts.DescriptionOf(snapshot.ScenarioNumber, snapshot.ScenarioDescription))
			: null;
	}

	// The rules' values, in the setting screen's words (UpdateConfigSetting).
	private void UpdateRules(GameSnapshot snapshot)
	{
		_rules[0].Value = Texts.DecisionValue(snapshot.IsDecisionEnabled);
		_rules[1].Value = Texts.Points(snapshot.SupplyRates[0]);
		_rules[2].Value = Texts.Points(snapshot.SupplyRates[1]);
		_rules[3].Value = Texts.SwapTimeValue(snapshot.SwapTime * 100);
		_rules[4].Value = snapshot.SwapTime == 0 ? "―" : Texts.SwapRuleValue(snapshot.SwapRule);
		_rules[5].Value = Texts.Points(snapshot.InitialSupplyPoints[0]);
		_rules[6].Value = Texts.Points(snapshot.InitialSupplyPoints[1]);
		_rules[7].Value = GameTexts.Of(snapshot.ArrivalControl);

		foreach (var rule in _rules)
		{
			rule.Visible = !IsChoosingScenario;
			rule.ShowButtons(snapshot.IsHost);
		}

		// The rule of the swap is set only while the rates swap.
		_rules[4].ShowButtons(snapshot.IsHost && snapshot.SwapTime != 0);
	}

	// Shows the game as it was in the last frame: on the UI's thread, each frame.
	public void UpdateFrom(GameSnapshot snapshot)
	{
		_snapshot = snapshot;
		UpdateRows(snapshot);
		UpdateDetails(snapshot);
		UpdateRules(snapshot);

		var isHost = snapshot.IsHost;
		_switchSides.Visible = IsChoosingScenario && isHost && !snapshot.IsEditingMap;
		_switchSides.Enabled = HasRival;
		_resume.Visible = _autoSave.Visible = _next.Visible = IsChoosingScenario && isHost;
		_resume.Enabled = HasRival && snapshot.WasHost;
		_autoSave.Enabled = HasRival && snapshot.HasAutoSave;
		_next.Enabled = HasRival && ChosenRow >= 0 && ChosenRow < _shownNames.Length;
		_back.Visible = _start.Visible = !IsChoosingScenario && isHost;
		_back.Enabled = _start.Enabled = HasRival;
	}

	// Whether a point of the screen is on the form, which takes the clicks there: the mouse's buttons are not the game's.
	public bool Owns(DrawingPoint point)
	{
		return Visible && ClientRectangle.Contains(point);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		var count = _shownNames.Length;
		switch (e.KeyCode)
		{
			case Keys.Up when count != 0:
				Choose(ChosenRow < 0 ? count - 1 : (ChosenRow - 1 + count) % count);
				break;
			case Keys.Down when count != 0:
				Choose(ChosenRow < 0 ? 0 : (ChosenRow + 1) % count);
				break;
			case Keys.Left:
			case Keys.Right:
				TurnPage(1 - Page);
				break;
			case Keys.Enter:
				GoOn();
				break;
		}
	}

	// What the screen waits for, or asks.
	private string Status()
	{
		if (!_snapshot.IsHost)
			return IsChoosingScenario ? Texts.HostChoosesScenario : Texts.HostSetsRules;

		if (!HasRival)
			return Texts.WaitingForGuest;

		if (_snapshot.IsEditingMap)
			return IsChoosingScenario ? Texts.ChooseScenarioToEdit : Texts.SetRules;

		return IsChoosingScenario ? Texts.ChooseScenario : Texts.SetRules;
	}

	// The sides, under the list: the host's and the guest's (UpdateSideSetting).
	private void PaintSides(Aigamo.Saruhashi.Graphics g)
	{
		var hostSide = _snapshot.HostSide;
		Painter.Text(g, Texts.PlayerSide, _fonts.Regular(13), Theme.Muted, 48, 618);
		foreach (var (side, player, y) in new[] { (hostSide, Texts.HostPlayer, 614), (1 - hostSide, Texts.GuestPlayer, 640) })
		{
			Painter.Fill(g, new(120, y + 8, 8, 8), side == 0 ? Theme.Japan : Theme.UnitedStates);
			Painter.Text(g, Texts.SideOf(side, player), _fonts.Bold(14), Theme.Text, 134, y);
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		var g = e.Graphics;
		var kicker = _snapshot.IsEditingMap ? Texts.MapEditorKicker : _snapshot.IsHost ? Texts.HostKicker : Texts.GuestKicker;
		Painter.SpacedText(g, kicker, _fonts.Bold(12), Theme.Brass, 48, 34, 3);
		Painter.Text(g, IsChoosingScenario ? Texts.Scenarios : Texts.Rules, _fonts.Bold(34), Theme.Text, 48, 52);
		Painter.Text(g, Status(), _fonts.Regular(14), Theme.Muted, IsChoosingScenario ? 300 : 48, 156);
		Painter.Fill(g, new(48, 186, 928, 1), Theme.Alpha(Theme.Line, 0.9f));
		if (IsChoosingScenario)
			PaintSides(g);

		GameMessages.PaintLines(g, _fonts, _snapshot, 504, 40);
	}
}
