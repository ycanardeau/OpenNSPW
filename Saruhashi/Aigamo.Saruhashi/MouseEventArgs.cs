using System.Drawing;

namespace Aigamo.Saruhashi;

public sealed class MouseEventArgs : EventArgs
{
	public MouseButtons Button { get; }
	public int Clicks { get; }
	public Point Location { get; }
	public int Delta { get; }

	public MouseEventArgs(MouseButtons button, int clicks, Point location, int delta)
	{
		Button = button;
		Clicks = clicks;
		Location = location;
		Delta = delta;
	}
}
