namespace OpenNspw;

// A point in whole pixels, on the screen or on a surface, in which Y grows downward. Two ints, X then Y, like the pairs
// of fields it replaces (SPRT.x and SPRT.y, and the others), so that the layout of the structs that hold it is
// unchanged.
public readonly struct PixelPoint(int x, int y)
{
	public readonly int X = x;
	public readonly int Y = y;

	public static PixelPoint operator +(PixelPoint point, PixelPoint offset)
	{
		return new PixelPoint(point.X + offset.X, point.Y + offset.Y);
	}
}
