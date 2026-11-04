using System.Collections.Concurrent;
using System.Drawing;

namespace Aigamo.Saruhashi;

public abstract class Brush : IDisposable
{
	public virtual void Dispose()
	{
		GC.SuppressFinalize(this);
	}
}

public sealed class SolidBrush(Color color) : Brush
{
	// How many brushes Of keeps before it starts again, as a color that fades is a new color each frame.
	private const int MaxBrushes = 1024;

	private static readonly ConcurrentDictionary<Color, SolidBrush> s_brushes = new();

	public Color Color { get; } = color;

	/// <summary>
	/// The brush of a color, made once: a brush only holds its color, and painting every frame with new ones makes garbage.
	/// </summary>
	public static SolidBrush Of(Color color)
	{
		if (s_brushes.TryGetValue(color, out var brush))
			return brush;

		if (s_brushes.Count >= MaxBrushes)
			s_brushes.Clear();
		return s_brushes.GetOrAdd(color, static c => new SolidBrush(c));
	}
}
