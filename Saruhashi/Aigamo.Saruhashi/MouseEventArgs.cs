using System.Drawing;

namespace Aigamo.Saruhashi;

public sealed class MouseEventArgs(MouseButtons button, int clicks, Point location, int delta) : EventArgs
{
	public MouseButtons Button { get; } = button;
	public int Clicks { get; } = clicks;
	public Point Location { get; } = location;
	public int Delta { get; } = delta;
}
