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

	// The brushes made, each thread's own: threads share no state but through the actors' mailboxes (docs/Actors.md).
	[ThreadStatic]
	private static Dictionary<Color, SolidBrush>? s_brushes;

	public Color Color { get; } = color;

	/// <summary>
	/// The brush of a color, made once: a brush only holds its color, and painting every frame with new ones makes garbage.
	/// </summary>
	public static SolidBrush Of(Color color)
	{
		var brushes = s_brushes ??= [];
		if (brushes.TryGetValue(color, out var brush))
			return brush;

		if (brushes.Count >= MaxBrushes)
			brushes.Clear();
		brush = new SolidBrush(color);
		brushes[color] = brush;
		return brush;
	}
}
