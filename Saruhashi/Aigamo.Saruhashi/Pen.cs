using System.Drawing;

namespace Aigamo.Saruhashi;

public sealed class Pen : IDisposable
{
	public Color Color { get; }
	public float Width { get; }

	public Pen(Color color, float width = 1)
	{
		Color = color;
		Width = width;
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}
}
