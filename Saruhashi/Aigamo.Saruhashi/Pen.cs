using System.Collections.Concurrent;
using System.Drawing;

namespace Aigamo.Saruhashi;

public sealed class Pen(Color color, float width = 1) : IDisposable
{
	// How many pens Of keeps before it starts again, as a color that fades is a new color each frame.
	private const int MaxPens = 1024;

	private static readonly ConcurrentDictionary<(Color, float), Pen> s_pens = new();

	public Color Color { get; } = color;
	public float Width { get; } = width;

	/// <summary>
	/// The pen of a color and a width, made once: a pen only holds them, and painting every frame with new ones makes
	/// garbage.
	/// </summary>
	public static Pen Of(Color color, float width = 1)
	{
		if (s_pens.TryGetValue((color, width), out var pen))
			return pen;

		if (s_pens.Count >= MaxPens)
			s_pens.Clear();
		return s_pens.GetOrAdd((color, width), static key => new Pen(key.Item1, key.Item2));
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}
}
