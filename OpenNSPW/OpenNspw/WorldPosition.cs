namespace OpenNspw;

// A position in the world, the simulation's coordinate space (docs/RefactoringCatalog.md, Terms), in which Y grows
// upward. Two doubles, X then Y, like the pairs of fields it replaces (UNIT.x and UNIT.y, and the others), so that the
// layout of the structs and globals that hold it is unchanged.
public readonly struct WorldPosition(double x, double y)
{
	public readonly double X = x;
	public readonly double Y = y;

	// Each component plus the vector's, as `position.X += vector.X; position.Y += vector.Y;` computes them.
	public static WorldPosition operator +(WorldPosition position, WorldVector vector)
	{
		return new WorldPosition(position.X + vector.X, position.Y + vector.Y);
	}
}
