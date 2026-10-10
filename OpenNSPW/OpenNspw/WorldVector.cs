namespace OpenNspw;

// A difference between two positions in the world, such as how far something moves in a tick. It is not stored in the
// state.
public readonly struct WorldVector(double x, double y)
{
	public readonly double X = x;
	public readonly double Y = y;
}
