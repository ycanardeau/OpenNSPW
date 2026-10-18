using Aigamo.Saruhashi;
using OpenNspw.Singleplayer;
using DrawingRectangle = System.Drawing.Rectangle;

namespace OpenNspw.Desktop.Controls;

// What NSPW 1.22's setting screen says about a scenario, in a panel framed like a chart: its number, title and side, over
// its briefing and victory conditions, which scroll (BriefingView).
internal sealed class ScenarioDetails : Control
{
	private const int Padding = 28;

	private readonly UIFonts _fonts;

	// The title as wrapped to the panel's width, for the scenario it was wrapped for.
	private ScenarioEntry? _wrappedFor;
	private List<string> _title = [];

	private ScenarioEntry? _scenario;

	public BriefingView Briefing { get; }

	public ScenarioDetails(UIFonts fonts)
	{
		_fonts = fonts;
		Briefing = new BriefingView(fonts) { BackColor = System.Drawing.Color.Transparent };
		Controls.Add(Briefing);
	}

	public ScenarioEntry? Scenario
	{
		get => _scenario;
		set
		{
			_scenario = value;
			Briefing.Scenario = value;
		}
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
		{
			_title = Painter.Wrap(g, scenario.Title, _fonts.Bold(28), Width - Padding * 2);
			_wrappedFor = scenario;
		}

		Painter.SpacedText(g, Texts.ScenarioNumber(scenario.Number), _fonts.Bold(12), Theme.Brass, Padding, 26, 3);
		var y = 46f;
		foreach (var line in _title)
		{
			Painter.Text(g, line, _fonts.Bold(28), Theme.Text, Padding, y);
			y += 38;
		}

		y += 6;
		Painter.Text(g, Texts.PlayerSide, _fonts.Regular(14), Theme.Muted, Padding, y + 1);
		Painter.Fill(g, new(Padding + 72, (int)y + 6, 10, 10), scenario.Side == ScenarioSide.Japan ? Theme.Japan : Theme.UnitedStates);
		Painter.Text(g, GameTexts.Of(scenario.SideName), _fonts.Bold(16), Theme.Text, Padding + 90, y);
		y += 36;
		Painter.Fill(g, new(Padding, (int)y, Width - Padding * 2, 1), Theme.Alpha(Theme.Line, 0.9f));
		y += 12;

		// The briefing takes the rest, under the title, which is one line or two; its scroll bar is in the right margin.
		var briefing = new DrawingRectangle(Padding, (int)y, Width - Padding * 2 + 14, Height - (int)y - 20);
		if (Briefing.Bounds != briefing)
			Briefing.Bounds = briefing;
	}
}
