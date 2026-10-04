using System.Drawing;

namespace Aigamo.Saruhashi;

public class TextRenderer : ITextRenderer
{
	public void DrawText(Graphics graphics, string? text, IFont? font, Rectangle bounds, Color foreColor, TextFormatFlags flags)
	{
		if (string.IsNullOrEmpty(text) || foreColor == Color.Transparent)
			return;

		if (font == null)
			return;

		var textSize = graphics.MeasureString(text, font);

		float x;
		if (flags.HasFlag(TextFormatFlags.HorizontalCenter))
			x = (bounds.Width - textSize.Width) / 2;
		else if (flags.HasFlag(TextFormatFlags.Right))
			x = bounds.Width - textSize.Width;
		else
			x = bounds.X;

		float y;
		if (flags.HasFlag(TextFormatFlags.VerticalCenter))
			y = (bounds.Height - textSize.Height) / 2;
		else if (flags.HasFlag(TextFormatFlags.Bottom))
			y = bounds.Height - textSize.Height;
		else
			y = bounds.Y;

		var point = new PointF(x, y);

		// OPTIMIZE
		using (var brush = new SolidBrush(foreColor))
			graphics.DrawString(text, font, brush, point);
	}
}
