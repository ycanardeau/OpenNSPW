using DrawingColor = System.Drawing.Color;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSizeF = System.Drawing.SizeF;
using XnaColor = Microsoft.Xna.Framework.Color;
using XnaPoint = Microsoft.Xna.Framework.Point;
using XnaRectangle = Microsoft.Xna.Framework.Rectangle;
using XnaSizeF = MonoGame.Extended.SizeF;

namespace Aigamo.Saruhashi.MonoGame;

public static class Extensions
{
	public static DrawingColor ToDrawingColor(this XnaColor color) =>
		DrawingColor.FromArgb(color.A, color.R, color.G, color.B);

	public static DrawingPoint ToDrawingPoint(this XnaPoint point) => new(point.X, point.Y);

	public static DrawingRectangle ToDrawingRectangle(this XnaRectangle rectangle) =>
		new(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

	public static DrawingSizeF ToDrawingSize(this XnaSizeF size) => new(size.Width, size.Height);

	public static XnaColor ToXnaColor(this DrawingColor color) =>
		new(color.R, color.G, color.B, color.A);

	public static XnaPoint ToXnaPoint(this DrawingPoint point) => new(point.X, point.Y);

	public static XnaRectangle ToXnaRectangle(this DrawingRectangle rectangle) =>
		new(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

	public static XnaSizeF ToXnaSize(this DrawingSizeF size) => new(size.Width, size.Height);
}
