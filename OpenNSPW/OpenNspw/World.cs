namespace OpenNspw;

// The bounds of the world, the simulation's coordinate space (docs/RefactoringCatalog.md, Terms): MAP_TOP,
// MAP_BOTTOM, MAP_RIGHT and MAP_LEFT.
public static class World
{
	[Original("MAP_TOP")]
	public const int Top = 7200;

	[Original("MAP_BOTTOM")]
	public const int Bottom = -7200;

	[Original("MAP_RIGHT")]
	public const int Right = 9600;

	[Original("MAP_LEFT")]
	public const int Left = -9600;

	// abs(MAP_RIGHT)+abs(MAP_LEFT) and abs(MAP_TOP)+abs(MAP_BOTTOM).
	public const int Width = Right - Left;
	public const int Height = Top - Bottom;

	// An X just east of the world, MAP_RIGHT+1: it ends a unit's path (PathX[n]), and a sound there is heard
	// everywhere.
	public const int Beyond = Right + 1;

	// Whether a position is in the world, its edges included.
	public static bool Contains(WorldPosition position)
	{
		return !(
			position.Y > Top || position.Y < Bottom || position.X < Left || position.X > Right
		);
	}
}
