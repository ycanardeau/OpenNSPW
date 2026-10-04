using System.Drawing;

namespace Aigamo.Saruhashi;

public sealed class PaintEventArgs : EventArgs
{
	public Graphics Graphics { get; }
	public Rectangle ClipRectangle { get; }

	public PaintEventArgs(Graphics graphics, Rectangle clipRectangle)
	{
		Graphics = graphics;
		ClipRectangle = clipRectangle;
	}
}
