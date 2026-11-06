using Aigamo.Saruhashi;
using DrawingRectangle = System.Drawing.Rectangle;

namespace OpenNspw.App.Controls;

// What the game writes over its screens, in the game's words, for the screens that the UI draws in its place: the
// notices that the players are out of step or the rival is gone, the players' chat, and the result of the battle.
internal static class GameMessages
{
	private const int LineHeight = 22;

	// A line on a dark band, and the next line's place below it; none for an empty text.
	private static void PaintLine(
		Graphics g,
		string text,
		IFont font,
		System.Drawing.Color color,
		float x,
		ref float y
	)
	{
		if (text.Length == 0)
			return;

		var width = (int)Painter.Measure(g, text, font).Width;
		Painter.Fill(
			g,
			new((int)x - 8, (int)y - 2, width + 16, LineHeight),
			Theme.Alpha(Theme.Ink, 0.75f)
		);
		Painter.Text(g, text, font, color, x, y);
		y += LineHeight + 4;
	}

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
		foreach (var notice in snapshot.Notices)
			PaintLine(g, GameTexts.Of(notice), font, Theme.Alert, x, ref y);
		PaintLine(g, snapshot.MyChat, font, Theme.BrassLight, x, ref y);
		PaintLine(g, snapshot.RivalChat, font, Theme.Text, x, ref y);
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
