using System.Drawing;

namespace Aigamo.Saruhashi;

public sealed class Region
{
	private readonly Rectangle _rectangle;

	public Region(Rectangle rectangle)
	{
		_rectangle = rectangle;
	}

	public RectangleF GetBounds(Graphics graphics) => graphics.Control.GetClipRectangle(_rectangle);
}
