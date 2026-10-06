using System.Drawing;

namespace Aigamo.Saruhashi;

public sealed class PaintEventArgs(Graphics graphics, Rectangle clipRectangle) : EventArgs
{
	public Graphics Graphics { get; } = graphics;
	public Rectangle ClipRectangle { get; } = clipRectangle;
}
