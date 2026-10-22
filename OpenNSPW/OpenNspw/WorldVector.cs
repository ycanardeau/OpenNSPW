namespace OpenNspw;

// A difference between two positions in the world, such as how far something moves in a tick. It is not stored in the
// state.
public readonly struct WorldVector(double x, double y)
{
	public readonly double X = x;
	public readonly double Y = y;

	// The direction of the vector in degrees, from 0 to 360 (nspw_math.Direction).
	public double Direction => nspw_math.Direction(X, Y);

	// The length of the vector, as the original computes it (nspw_math.Distance), which counts a component of 0 as 1.
	public double Length => nspw_math.Distance(X, Y);

	// The vector of length 1 in a direction in degrees: `CosDegrees(direction)` and `SinDegrees(direction)`, as the
	// original moves things.
	public static WorldVector FromDirection(double degrees)
	{
		return new WorldVector(nspw_math.CosDegrees(degrees), nspw_math.SinDegrees(degrees));
	}

	// Each component times the factor, as `x*factor` and `y*factor` compute them.
	public static WorldVector operator *(WorldVector vector, double factor)
	{
		return new WorldVector(vector.X * factor, vector.Y * factor);
	}

	// The vector with each component of 0 replaced by 1, as the original does before it takes the direction of a
	// vector: `if(wrk_x==0) wrk_x=1; if(wrk_y==0) wrk_y=1;`.
	public WorldVector NonZero()
	{
		return new WorldVector(X == 0 ? 1 : X, Y == 0 ? 1 : Y);
	}
}
