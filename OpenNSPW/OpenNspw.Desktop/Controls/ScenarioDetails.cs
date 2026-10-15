using Aigamo.Saruhashi;
using OpenNspw.Singleplayer;
using Graphics = Aigamo.Saruhashi.Graphics;

namespace OpenNspw.Desktop.Controls;

// What NSPW 1.22's setting screen says about a scenario: its side, its briefing, and its victory conditions (勝敗条件),
// in a panel framed like a chart.
internal sealed class ScenarioDetails(UiFonts fonts) : Control
{
	private const int Padding = 28;
	private const string ConditionsLabel = "勝敗条件";

	private readonly UiFonts _fonts = fonts;

	// The lines as wrapped to the panel's width, for the scenario they were wrapped for.
	private ScenarioEntry? _wrappedFor;
	private List<string> _title = [];
	private List<string> _briefing = [];
	private List<string> _conditions = [];

	public ScenarioEntry? Scenario { get; set; }

	// The briefing, and the victory conditions that end it, each as one text: the screen's lines are broken for its own
	// width.
	private static (string Briefing, string Conditions) Split(ScenarioEntry scenario)
	{
		var start = scenario.Briefing.ToList().FindIndex(l => l.StartsWith(ConditionsLabel + "：", StringComparison.Ordinal));
		if (start < 0)
			return (string.Concat(scenario.Briefing), "");

		var conditions = string.Concat(scenario.Briefing.Skip(start));
		return (string.Concat(scenario.Briefing.Take(start)), conditions[(ConditionsLabel.Length + 1)..]);
	}

	private void Wrap(Graphics graphics, ScenarioEntry scenario)
	{
		var width = Width - Padding * 2;
		var (briefing, conditions) = Split(scenario);
		_title = Painter.Wrap(graphics, scenario.Title, _fonts.Bold(28), width);
		_briefing = Painter.Wrap(graphics, briefing, _fonts.Regular(16), width);
		_conditions = Painter.Wrap(graphics, conditions, _fonts.Regular(16), width - 20);
		_wrappedFor = scenario;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		var g = e.Graphics;
		Painter.Fill(g, ClientRectangle, Theme.Alpha(Theme.Panel, 0.9f));
		Painter.Frame(g, ClientRectangle, Theme.Alpha(Theme.Line, 0.9f));
		Painter.Brackets(g, ClientRectangle, 16, Theme.Brass);
		if (Scenario is not { } scenario)
			return;

		if (_wrappedFor != scenario)
			Wrap(g, scenario);

		Painter.SpacedText(g, $"SCENARIO {scenario.Number}", _fonts.Bold(12), Theme.Brass, Padding, 26, 3);
		var y = 46f;
		foreach (var line in _title)
		{
			Painter.Text(g, line, _fonts.Bold(28), Theme.Text, Padding, y);
			y += 38;
		}

		y += 6;
		Painter.Text(g, "操作対象", _fonts.Regular(14), Theme.Muted, Padding, y + 1);
		Painter.Fill(g, new(Padding + 72, (int)y + 6, 10, 10), scenario.Side == ScenarioSide.Japan ? Theme.Japan : Theme.UnitedStates);
		Painter.Text(g, scenario.SideName, _fonts.Bold(16), Theme.Text, Padding + 90, y);
		y += 36;
		Painter.Fill(g, new(Padding, (int)y, Width - Padding * 2, 1), Theme.Alpha(Theme.Line, 0.9f));
		y += 16;

		foreach (var line in _briefing)
		{
			Painter.Text(g, line, _fonts.Regular(16), Theme.Lerp(Theme.Text, Theme.Muted, 0.15f), Padding, y);
			y += 26;
		}

		if (_conditions.Count == 0)
			return;

		y += 12;
		var box = new System.Drawing.Rectangle(Padding, (int)y, Width - Padding * 2, 40 + _conditions.Count * 26);
		Painter.Fill(g, box, Theme.Alpha(Theme.Brass, 0.09f));
		Painter.Fill(g, new(box.X, box.Y, 3, box.Height), Theme.Brass);
		Painter.Text(g, ConditionsLabel, _fonts.Bold(13), Theme.Brass, box.X + 16, box.Y + 10);
		y += 34;
		foreach (var line in _conditions)
		{
			Painter.Text(g, line, _fonts.Regular(16), Theme.Text, box.X + 16, y);
			y += 26;
		}
	}
}
