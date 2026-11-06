using Aigamo.Saruhashi;
using OpenNspw.Replay;
using OpenNspw.Singleplayer;

namespace OpenNspw.App.Controls;

// A replay in the list of replays: when it was played, single-player or against whom, its scenario, how long it lasted
// and how the battle ended; or that it cannot be read.
internal sealed class ReplayRow(UIFonts fonts, string path, ReplaySummary? summary) : FlatButton
{
	// The scenario's title, in the language chosen: one of 1.22's, as the single-player browser names it, or a network
	// battle's.
	private static string ScenarioTitle(ReplayStart start)
	{
		if (start.Mode == ReplayMode.Singleplayer)
			return SingleplayerTexts
					.Of(ScenarioSelection.Sets)
					.SelectMany(set => set.Scenarios)
					.FirstOrDefault(scenario => scenario.Number == start.Scenario)
					?.Title
				?? Texts.NetworkScenario(start.Scenario);

		return start.Network?.UserScenarioFileName is { Length: > 0 } file
			? System.IO.Path.GetFileNameWithoutExtension(file.Replace('\\', '/'))
			: Texts.NetworkScenario(start.Scenario);
	}

	private readonly UIFonts _fonts = fonts;
	private readonly LastText<ReplayStart> _scenarioTitle = new(ScenarioTitle);

	public string Path { get; } = path;

	public ReplaySummary? Summary { get; } = summary;

	public bool IsSelected { get; set; }

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

		var small = _fonts.Regular(13);
		var title = IsSelected
			? Theme.BrassLight
			: Theme.Lerp(Theme.Text, Theme.BrassLight, 0.5f * Hover);
		if (Summary is not { } summary)
		{
			Painter.Text(g, System.IO.Path.GetFileName(Path), _fonts.Regular(16), title, 16, 10);
			Painter.Text(g, Texts.UnreadableReplay, small, Theme.Alert, 560, 13);
			return;
		}

		var start = summary.Start;
		Painter.Text(
			g,
			start.Date.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
			small,
			Theme.Brass,
			16,
			13
		);
		var titleFont = _fonts.Regular(16);
		Painter.Text(
			g,
			Painter.Fit(g, _scenarioTitle.Of(start), titleFont, 300),
			titleFont,
			title,
			160,
			10
		);
		var mode = Texts.ReplayMode(start.Mode, start.Network?.RivalPlayerName ?? "");
		Painter.Text(g, Painter.Fit(g, mode, small, 190), small, Theme.Muted, 476, 13);
		Painter.Text(g, Texts.Duration(summary.Milliseconds), small, Theme.Text, 680, 13);
		Painter.Text(g, Texts.ResultName(summary.Result), small, Theme.Muted, 760, 13);
	}
}
