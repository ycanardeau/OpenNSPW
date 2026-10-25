namespace OpenNspw;

// A width and a height in whole pixels. Two ints, like the pairs of fields it replaces (SPRT.wd and SPRT.ht), so that
// the layout of the structs that hold it is unchanged.
public readonly struct PixelSize(int width, int height)
{
	public readonly int Width = width;
	public readonly int Height = height;
}
