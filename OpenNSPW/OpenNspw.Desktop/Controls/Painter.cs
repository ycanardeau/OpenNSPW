using System.Diagnostics;
using System.Drawing;
using Aigamo.Saruhashi;
using Brush = Aigamo.Saruhashi.Brush;
using Graphics = Aigamo.Saruhashi.Graphics;
using Pen = Aigamo.Saruhashi.Pen;
using SolidBrush = Aigamo.Saruhashi.SolidBrush;

namespace OpenNspw.Desktop.Controls;

// Drawing the menus' shapes and text on a control.
internal static class Painter
{
	private static readonly Stopwatch Clock = Stopwatch.StartNew();

	// Seconds since the app started, for what blinks or expires.
	public static float Now => (float)Clock.Elapsed.TotalSeconds;

	public static void Fill(Graphics graphics, Rectangle rectangle, Color color)
	{
		graphics.FillRectangle(new SolidBrush(color), rectangle);
	}

	public static void Frame(Graphics graphics, Rectangle rectangle, Color color)
	{
		graphics.DrawRectangle(new Pen(color), rectangle.X, rectangle.Y, rectangle.Width - 1, rectangle.Height - 1);
	}

	// The corners of a rectangle, as on a chart's frame.
	public static void Brackets(Graphics graphics, Rectangle rectangle, int length, Color color)
	{
		var brush = new SolidBrush(color);
		var (left, top, right, bottom) = (rectangle.Left, rectangle.Top, rectangle.Right - 2, rectangle.Bottom - 2);
		graphics.FillRectangle(brush, left, top, length, 2);
		graphics.FillRectangle(brush, left, top, 2, length);
		graphics.FillRectangle(brush, right - length + 2, top, length, 2);
		graphics.FillRectangle(brush, right, top, 2, length);
		graphics.FillRectangle(brush, left, bottom, length, 2);
		graphics.FillRectangle(brush, left, bottom - length + 2, 2, length);
		graphics.FillRectangle(brush, right - length + 2, bottom, length, 2);
		graphics.FillRectangle(brush, right, bottom - length + 2, 2, length);
	}

	public static SizeF Measure(Graphics graphics, string text, IFont font)
	{
		return text.Length == 0 ? SizeF.Empty : graphics.MeasureString(text, font);
	}

	public static void Text(Graphics graphics, string text, IFont font, Color color, float x, float y)
	{
		if (text.Length > 0)
			graphics.DrawString(text, font, new SolidBrush(color), new PointF(x, y));
	}

	// The text, cut with an ellipsis to fit `width`.
	public static string Fit(Graphics graphics, string text, IFont font, float width)
	{
		if (Measure(graphics, text, font).Width <= width)
			return text;

		var length = text.Length;
		while (length > 0 && Measure(graphics, text[..length] + "…", font).Width > width)
			length--;

		return text[..length] + "…";
	}

	// Text spaced out by `spacing` pixels a character, for small capitals.
	public static void SpacedText(Graphics graphics, string text, IFont font, Color color, float x, float y, float spacing)
	{
		foreach (var c in text)
		{
			var s = c.ToString();
			Text(graphics, s, font, color, x, y);
			x += Measure(graphics, s, font).Width + spacing;
		}
	}

	// Breaks text into lines no wider than `width`, between any two characters, as Japanese is broken; a line does not
	// start with a closing punctuation mark.
	public static List<string> Wrap(Graphics graphics, string text, IFont font, float width)
	{
		const string noLineStart = "、。，．）」』！？ー…";
		var lines = new List<string>();
		var start = 0;
		for (var end = 1; end <= text.Length; end++)
		{
			if (Measure(graphics, text[start..end], font).Width <= width)
				continue;

			var breakAt = end - 1;
			while (breakAt > start + 1 && noLineStart.Contains(text[breakAt]))
				breakAt--;

			lines.Add(text[start..breakAt]);
			start = breakAt;
		}

		if (start < text.Length)
			lines.Add(text[start..]);

		return lines;
	}
}
