using Aigamo.Saruhashi.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using DrawingPoint = System.Drawing.Point;

namespace OpenNspw.Desktop.Controls;

// The marks drawn on units, on the battlefield and on the deck, in the theme's colors where the original draws white
// lines (DrawUnitMarks); and where a click on a unit counts.
internal static class UnitMarks
{
	public static readonly Color Mark = Theme.Brass.ToXnaColor();
	public static readonly Color MarkLight = Theme.BrassLight.ToXnaColor();
	public static readonly Color Enemy = Theme.Alert.ToXnaColor();

	// Whether a point is in the middle of a unit's sprite at `at`, 20 in from the sides of its 78 by 78 (DrawUnitMarks).
	public static bool IsInMiddle(Vector2 at, DrawingPoint point)
	{
		return point.X >= at.X - 20
			&& point.X <= at.X + 18
			&& point.Y >= at.Y - 20
			&& point.Y <= at.Y + 18;
	}

	// A square around a point, `half` from it to each side.
	public static void DrawBox(
		SpriteBatch batch,
		Vector2 center,
		int half,
		Color color,
		float thickness = 1
	)
	{
		batch.DrawRectangle(
			new RectangleF(center.X - half, center.Y - half, half * 2, half * 2),
			color,
			thickness
		);
	}

	// The corners of a square around a point, as on a chart.
	public static void DrawBrackets(SpriteBatch batch, Vector2 center, int half, Color color)
	{
		const int Length = 8;
		foreach (var (sx, sy) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
		{
			var corner = center + new Vector2(sx * half, sy * half);
			batch.DrawLine(corner, corner - new Vector2(sx * Length, 0), color, 2);
			batch.DrawLine(corner, corner - new Vector2(0, sy * Length), color, 2);
		}
	}

	// The selected unit's mark, or a mark for a unit in the selection (DrawSelectedUnit, DrawUnitMarks).
	public static void DrawSelection(
		SpriteBatch batch,
		Vector2 center,
		GameSnapshot snapshot,
		int m
	)
	{
		if (m == snapshot.SelectedUnit)
			DrawBrackets(batch, center, 30, MarkLight);
		else if (snapshot.Selections[m] != 0)
			DrawBrackets(batch, center, 20, Mark);
	}
}
