using System.Drawing;

namespace Aigamo.Saruhashi;

public sealed class Pen(Color color, float width = 1) : IDisposable
{
	// How many pens Of keeps before it starts again, as a color that fades is a new color each frame.
	private const int MaxPens = 1024;

	// The pens made, each thread's own: threads share no state but through the actors' mailboxes (docs/Actors.md).
	[ThreadStatic]
	private static Dictionary<(Color, float), Pen>? s_pens;

	public Color Color { get; } = color;
	public float Width { get; } = width;

	/// <summary>
	/// The pen of a color and a width, made once: a pen only holds them, and painting every frame with new ones makes
	/// garbage.
	/// </summary>
	public static Pen Of(Color color, float width = 1)
	{
		var pens = s_pens ??= [];
		if (pens.TryGetValue((color, width), out var pen))
			return pen;

		if (pens.Count >= MaxPens)
			pens.Clear();
		pen = new Pen(color, width);
		pens[(color, width)] = pen;
		return pen;
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}
}
