using Aigamo.Saruhashi;
using DrawingRectangle = System.Drawing.Rectangle;

namespace OpenNspw.Desktop.Controls;

// What the game writes over its screens, in the game's words, for the screens that the UI draws in its place: the
// notices that the players are out of step or the rival is gone, the players' chat, and the result of the battle.
internal static class GameMessages
{
	private const int LineHeight = 22;

	// The notices and the chat, a line each, down from a point: the notices first, as the game puts them in the top
	// left corner, then the player's chat and the rival's.
	public static void PaintLines(
		Graphics g,
		UIFonts fonts,
		GameSnapshot snapshot,
		float x,
		float y
	)
	{
		var font = fonts.Bold(14);
		var lines = snapshot
			.Notices.Select(n => (Text: GameTexts.Of(n), Color: Theme.Alert))
			.Append((Text: snapshot.MyChat, Color: Theme.BrassLight))
			.Append((Text: snapshot.RivalChat, Color: Theme.Text))
			.Where(l => l.Text.Length != 0)
			.ToList();
		foreach (var (text, color) in lines)
		{
			var width = (int)Painter.Measure(g, text, font).Width;
			Painter.Fill(
				g,
				new((int)x - 8, (int)y - 2, width + 16, LineHeight),
				Theme.Alpha(Theme.Ink, 0.75f)
			);
			Painter.Text(g, text, font, color, x, y);
			y += LineHeight + 4;
		}
	}

	// The result, once the battle is over, in a panel in the middle of an area (DrawResult).
	public static void PaintResult(
		Graphics g,
		UIFonts fonts,
		GameSnapshot snapshot,
		DrawingRectangle area
	)
	{
		if (snapshot.ResultText.Length == 0)
			return;

		var panel = new DrawingRectangle(
			area.X + (area.Width - 480) / 2,
			area.Y + (area.Height - 160) / 2,
			480,
			160
		);
		Painter.Fill(g, panel, Theme.Alpha(Theme.Panel, 0.94f));
		Painter.Frame(g, panel, Theme.Line);
		Painter.Brackets(g, panel, 14, Theme.Brass);

		var lineFonts = new[] { fonts.Bold(30), fonts.Regular(16), fonts.Bold(20) };
		var colors = new[] { Theme.Text, Theme.Muted, Theme.BrassLight };
		var y = panel.Y + 26f;
		for (var i = 0; i < snapshot.ResultText.Length; i++)
		{
			var text = GameTexts.Of(snapshot.ResultText[i]);
			var width = Painter.Measure(g, text, lineFonts[i]).Width;
			Painter.Text(g, text, lineFonts[i], colors[i], panel.X + (panel.Width - width) / 2, y);
			y += i == 0 ? 50 : 34;
		}
	}
}
