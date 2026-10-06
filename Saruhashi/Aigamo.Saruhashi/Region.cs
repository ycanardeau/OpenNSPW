using System.Drawing;

namespace Aigamo.Saruhashi;

public sealed class Region(Rectangle rectangle)
{
	private readonly Rectangle _rectangle = rectangle;

	public RectangleF GetBounds(Graphics graphics) => graphics.Control.GetClipRectangle(_rectangle);
}
