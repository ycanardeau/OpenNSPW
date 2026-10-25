namespace OpenNspw;

// A rectangle in the world, in whole units, as the original keeps the areas it tests positions against (hit areas,
// areas around a ship or a path point) in RECTs of world coordinates: Top is above Bottom, as Y grows upward.
public readonly struct WorldRect(int left, int top, int right, int bottom)
{
	public readonly int Left = left;
	public readonly int Top = top;
	public readonly int Right = right;
	public readonly int Bottom = bottom;

	// The rectangle that reaches halfWidth and halfHeight from a position, truncated as
	// `top=(int)y+halfHeight; right=(int)x+halfWidth; bottom=(int)y-halfHeight; left=(int)x-halfWidth;`.
	public static WorldRect Around(WorldPosition center, int halfWidth, int halfHeight)
	{
		return new WorldRect(
			(int)center.X - halfWidth,
			(int)center.Y + halfHeight,
			(int)center.X + halfWidth,
			(int)center.Y - halfHeight
		);
	}

	// The rectangle moved by dx and dy, as each edge's `+dx` or `+dy` does.
	public WorldRect Offset(int dx, int dy)
	{
		return new WorldRect(Left + dx, Top + dy, Right + dx, Bottom + dy);
	}

	// Whether a position is inside, edges included, by its truncated coordinates, as PointInRect2 and PointInRect3 test
	// `(int)x` and `(int)y`.
	public bool Contains(WorldPosition position)
	{
		var x = (int)position.X;
		var y = (int)position.Y;
		return Top >= y && Bottom <= y && Right >= x && Left <= x;
	}
}
