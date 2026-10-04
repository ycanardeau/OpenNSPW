using System.Drawing;

namespace Aigamo.Saruhashi;

public abstract class Brush : IDisposable
{
	public virtual void Dispose()
	{
		GC.SuppressFinalize(this);
	}
}

public sealed class SolidBrush : Brush
{
	public Color Color { get; }

	public SolidBrush(Color color)
	{
		Color = color;
	}
}
