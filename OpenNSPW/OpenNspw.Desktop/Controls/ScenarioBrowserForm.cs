using Aigamo.Saruhashi;
using OpenNspw.Singleplayer;
using DrawingColor = System.Drawing.Color;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;

namespace OpenNspw.Desktop.Controls;

// The scenarios of NSPW 1.22, a set at a time, as its scenario selection screen shows them, with what its setting
// screen says about the one selected. Chosen with the mouse, or with the arrow keys, Enter and Escape.
internal sealed class ScenarioBrowserForm : Form
{
	private const int RowHeight = 44;

	private readonly UiFonts _fonts;
	private readonly IReadOnlyList<ScenarioSet> _sets;
	private readonly TabButton[] _tabs;
	private readonly ScenarioRow[][] _rows;
	private readonly ScenarioDetails _details;
	private int _page;
	private int _selected;

	public event EventHandler<int>? ScenarioSelected;

	public event EventHandler? Canceled;

	// The selected scenario's number.
	public int SelectedScenario => _rows[_page][_selected].Scenario.Number;

	// A set's heading, シナリオ選択画面（第一集、本ゲームの標準シナリオです。）, as its name and its description.
	private static (string Name, string Description) SplitHeading(string heading, int index)
	{
		var open = heading.IndexOf('（');
		var comma = heading.IndexOf('、', Math.Max(open, 0));
		var close = heading.LastIndexOf('）');
		return open >= 0 && comma > open && close > comma
			? (heading[(open + 1)..comma], heading[(comma + 1)..close])
			: ($"{index + 1}", heading);
	}

	private void Select(int page, int index)
	{
		_page = Math.Clamp(page, 0, _rows.Length - 1);
		_selected = Math.Clamp(index, 0, _rows[_page].Length - 1);
		for (var i = 0; i < _rows.Length; i++)
		{
			_tabs[i].IsSelected = i == _page;
			for (var j = 0; j < _rows[i].Length; j++)
			{
				_rows[i][j].Visible = i == _page;
				_rows[i][j].IsSelected = i == _page && j == _selected;
			}
		}

		_details.Scenario = _rows[_page][_selected].Scenario;
	}

	private void Start()
	{
		ScenarioSelected?.Invoke(this, SelectedScenario);
	}

	private ScenarioRow CreateRow(ScenarioEntry scenario, int page, int index)
	{
		var row = new ScenarioRow(_fonts, scenario) { Bounds = new DrawingRectangle(48, 200 + index * RowHeight, 432, RowHeight) };
		row.Click += (_, _) => Select(page, index);
		Controls.Add(row);
		return row;
	}

	private TabButton CreateTab(ScenarioSet set, int page)
	{
		var tab = new TabButton(_fonts) { Bounds = new DrawingRectangle(48 + page * 112, 112, 96, 32), Text = GameTexts.Of(SplitHeading(set.Heading, page).Name) };
		tab.Click += (_, _) => Select(page, 0);
		Controls.Add(tab);
		return tab;
	}

	// initialScenario: the scenario to select first, such as the one played last.
	public ScenarioBrowserForm(UiFonts fonts, IReadOnlyList<ScenarioSet> sets, int? initialScenario)
	{
		_fonts = fonts;
		_sets = sets;
		Size = new DrawingSize(all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);
		BackColor = DrawingColor.Transparent;
		KeyPreview = true;

		_tabs = [.. sets.Select(CreateTab)];
		_rows = [.. sets.Select((set, page) => set.Scenarios.Select((scenario, index) => CreateRow(scenario, page, index)).ToArray())];
		_details = new ScenarioDetails(fonts) { Bounds = new DrawingRectangle(504, 200, 472, 460) };
		Controls.Add(_details);

		var back = new ActionButton(fonts) { Bounds = new DrawingRectangle(48, 688, 150, 44), Text = Texts.Back };
		back.Click += (_, _) => Canceled?.Invoke(this, EventArgs.Empty);
		Controls.Add(back);

		var start = new ActionButton(fonts) { Bounds = new DrawingRectangle(826, 688, 150, 44), Text = Texts.Start, IsPrimary = true };
		start.Click += (_, _) => Start();
		Controls.Add(start);

		var initial = sets.SelectMany((set, page) => set.Scenarios.Select((scenario, index) => (scenario.Number, Page: page, Index: index)))
			.FirstOrDefault(s => s.Number == initialScenario);
		Select(initial.Page, initial.Index);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		switch (e.KeyCode)
		{
			case Keys.Up:
				Select(_page, (_selected - 1 + _rows[_page].Length) % _rows[_page].Length);
				break;
			case Keys.Down:
				Select(_page, (_selected + 1) % _rows[_page].Length);
				break;
			case Keys.Left:
				Select((_page - 1 + _rows.Length) % _rows.Length, 0);
				break;
			case Keys.Right:
				Select((_page + 1) % _rows.Length, 0);
				break;
			case Keys.PageUp:
				_details.Briefing.ScrollPage(-1);
				break;
			case Keys.PageDown:
				_details.Briefing.ScrollPage(1);
				break;
			case Keys.Home:
				_details.Briefing.ScrollToStart();
				break;
			case Keys.End:
				_details.Briefing.ScrollToEnd();
				break;
			case Keys.Enter:
				Start();
				break;
			case Keys.Escape:
				Canceled?.Invoke(this, EventArgs.Empty);
				break;
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		var g = e.Graphics;
		Painter.SpacedText(g, Texts.SinglePlayerKicker, _fonts.Bold(12), Theme.Brass, 48, 34, 3);
		Painter.Text(g, Texts.Scenarios, _fonts.Bold(34), Theme.Text, 48, 52);
		Painter.Text(g, SplitHeading(_sets[_page].Heading, _page).Description, _fonts.Regular(14), Theme.Muted, 48, 156);
		Painter.Fill(g, new(48, 186, 928, 1), Theme.Alpha(Theme.Line, 0.9f));

		var hint = Texts.BrowserHint;
		var hintFont = _fonts.Regular(13);
		Painter.Text(g, hint, hintFont, Theme.Muted, (Width - Painter.Measure(g, hint, hintFont).Width) / 2, 701);
	}
}
