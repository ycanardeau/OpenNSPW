using System.Drawing;

namespace Aigamo.Saruhashi;

public sealed class Pen(Color color, float width = 1) : IDisposable
{
	public Color Color { get; } = color;
	public float Width { get; } = width;

	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}
}
