namespace OpenNspw;

// A rectangle in whole pixels, by its edges, as the original fills a RECT of screen coordinates. Not stored in the
// state.
public readonly struct PixelRect(int left, int top, int right, int bottom)
{
	public readonly int Left = left;
	public readonly int Top = top;
	public readonly int Right = right;
	public readonly int Bottom = bottom;

	// Whether a point is inside, edges included, with Top the greater, as PointInRect2 and PointInRect3 test it.
	public bool ContainsWithTopAbove(PixelPoint point)
	{
		return Top >= point.Y && Bottom <= point.Y && Right >= point.X && Left <= point.X;
	}
}
