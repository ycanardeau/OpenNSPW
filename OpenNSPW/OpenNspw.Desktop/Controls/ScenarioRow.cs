using Aigamo.Saruhashi;
using OpenNspw.Singleplayer;

namespace OpenNspw.Desktop.Controls;

// A scenario in the browser's list: its number, its title, and the side it gives the player.
internal sealed class ScenarioRow(UIFonts fonts, ScenarioEntry scenario) : FlatButton
{
	private readonly UIFonts _fonts = fonts;

	public ScenarioEntry Scenario { get; } = scenario;

	public bool IsSelected { get; set; }

	private string? _fitTitle;

	protected override void PaintButton(PaintEventArgs e)
	{
		var g = e.Graphics;
		if (IsSelected)
		{
			Painter.Fill(g, ClientRectangle, Theme.Alpha(Theme.Brass, 0.16f));
			Painter.Fill(g, new(0, 0, 3, Height), Theme.Brass);
		}
		else if (Hover > 0)
		{
			Painter.Fill(g, ClientRectangle, Theme.Alpha(Theme.Text, 0.06f * Hover));
			Painter.Fill(g, new(0, 0, 3, Height), Theme.Alpha(Theme.Brass, 0.5f * Hover));
		}

		Painter.Fill(g, new(0, Height - 1, Width, 1), Theme.Alpha(Theme.Line, 0.6f));

		var number = Scenario.Number.ToString();
		var numberFont = _fonts.Bold(15);
		Painter.Text(g, number, numberFont, Theme.Brass, 54 - Painter.Measure(g, number, numberFont).Width, 13);

		var sideFont = _fonts.Regular(13);
		var sideWidth = Painter.Measure(g, Scenario.SideName, sideFont).Width;
		var sideX = Width - 14 - sideWidth;

		// The title, cut to end before the side's mark; the full title is in the details.
		var titleFont = _fonts.Regular(18);
		var title = IsSelected ? Theme.BrassLight : Theme.Lerp(Theme.Text, Theme.BrassLight, 0.5f * Hover);
		_fitTitle ??= Painter.Fit(g, Scenario.Title, titleFont, sideX - 14 - 8 - 72);
		Painter.Text(g, _fitTitle, titleFont, title, 72, 10);

		Painter.Fill(g, new((int)sideX - 14, 18, 8, 8), Scenario.Side == ScenarioSide.Japan ? Theme.Japan : Theme.UnitedStates);
		Painter.Text(g, Scenario.SideName, sideFont, Theme.Muted, sideX, 13);
	}
}
