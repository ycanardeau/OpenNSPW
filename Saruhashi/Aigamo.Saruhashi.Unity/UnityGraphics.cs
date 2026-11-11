using UnityEngine;
using DrawingColor = System.Drawing.Color;
using DrawingPoint = System.Drawing.Point;
using DrawingPointF = System.Drawing.PointF;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingRectangleF = System.Drawing.RectangleF;
using DrawingSizeF = System.Drawing.SizeF;
using UnityColor = UnityEngine.Color;

namespace Aigamo.Saruhashi.Unity;

/// <summary>
/// Draws a control's paint with IMGUI, during an IMGUI repaint (<see cref="UnityGui"/>): clipped to the control, and
/// <see cref="Control.ScreenScale"/> times as large around where its client area is.
/// </summary>
public sealed class UnityGraphics : Graphics
{
	// IMGUI's matrix as it was before the control's scale, given back when the drawing ends.
	private Matrix4x4 _matrix;

	// Where the control's client area is, in the clip.
	private Vector2 _origin;

	private bool _isOpen;

	// Starts drawing on the control: with its scale, then in a clip of its own, from the clip's top left. The scale is set
	// before the clip, and the clip made as much smaller, for IMGUI to clip where the control is on the screen.
	private void Open()
	{
		var origin = Control.PointToScreen(DrawingPoint.Empty);
		var o = new Vector2(origin.X, origin.Y);
		var clip = DrawingRectangle.Round(ClipBounds);
		var clipRect = new Rect(clip.X, clip.Y, clip.Width, clip.Height);

		_matrix = GUI.matrix;
		var scale = Control.ScreenScale;
		if (scale != 1)
		{
			GUI.matrix =
				_matrix
				* Matrix4x4.Translate(o)
				* Matrix4x4.Scale(new Vector3(scale, scale, 1))
				* Matrix4x4.Translate(-o);
			clipRect = new Rect(o + (clipRect.position - o) / scale, clipRect.size / scale);
		}

		GUI.BeginClip(clipRect);
		_origin = o - clipRect.position;
		_isOpen = true;
	}

	// Ends the drawing: the clip, and the scale as it was.
	internal void Close()
	{
		if (!_isOpen)
			return;

		GUI.EndClip();
		GUI.matrix = _matrix;
		_isOpen = false;
	}

	public UnityGraphics(Control control)
		: base(control)
	{
		Open();
	}

	// Starts drawing again, closed, on another control, for another paint (UnityGraphicsFactory.Rent).
	internal void Reopen(Control control)
	{
		Rebind(control);
		Open();
	}

	private bool _disposed;

	protected override void Dispose(bool disposing)
	{
		if (_disposed)
			return;

		if (disposing)
			Close();

		_disposed = true;

		base.Dispose(disposing);
	}

	// A point of the control's client area, in the clip before the control's scale.
	private Vector2 ToGui(DrawingPoint point) => _origin + new Vector2(point.X, point.Y);

	// A rectangle of the control's client area, in the clip before the control's scale.
	private Rect ToGui(DrawingRectangle rectangle) =>
		new(_origin.x + rectangle.X, _origin.y + rectangle.Y, rectangle.Width, rectangle.Height);

	private static void Fill(Rect rectangle, UnityColor color) =>
		GUI.DrawTexture(
			rectangle,
			Texture2D.whiteTexture,
			ScaleMode.StretchToFill,
			alphaBlend: true,
			imageAspect: 0,
			color,
			borderWidth: 0,
			borderRadius: 0
		);

	public override void DrawImage(IImage image, DrawingPoint point) =>
		(image as IUnityImage)?.Draw(ToGui(point));

	public override void DrawImage(IImage image, DrawingPointF point) =>
		DrawImage(image, DrawingPoint.Round(point));

	// As MonoGame.Extended draws a line: `pen.Width` wide, centered on the line from `point1` to `point2`. A line neither
	// across nor down is drawn turned, which turns IMGUI's clip with it.
	public override void DrawLine(Pen pen, DrawingPoint point1, DrawingPoint point2)
	{
		if (pen.Color == DrawingColor.Transparent)
			return;

		var start = ToGui(point1);
		var end = ToGui(point2);
		var width = pen.Width;
		var color = pen.Color.ToUnityColor();
		if (start.y == end.y)
		{
			Fill(new Rect(Mathf.Min(start.x, end.x), start.y - width / 2, Mathf.Abs(end.x - start.x), width), color);
			return;
		}

		if (start.x == end.x)
		{
			Fill(new Rect(start.x - width / 2, Mathf.Min(start.y, end.y), width, Mathf.Abs(end.y - start.y)), color);
			return;
		}

		var matrix = GUI.matrix;
		GUIUtility.RotateAroundPivot(Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg, start);
		Fill(new Rect(start.x, start.y - width / 2, Vector2.Distance(start, end), width), color);
		GUI.matrix = matrix;
	}

	public override void DrawLine(Pen pen, DrawingPointF point1, DrawingPointF point2) =>
		DrawLine(pen, DrawingPoint.Round(point1), DrawingPoint.Round(point2));

	public override void DrawLine(Pen pen, int x1, int y1, int x2, int y2) =>
		DrawLine(pen, new DrawingPoint(x1, y1), new DrawingPoint(x2, y2));

	public override void DrawLine(Pen pen, float x1, float y1, float x2, float y2) =>
		DrawLine(pen, new DrawingPointF(x1, y1), new DrawingPointF(x2, y2));

	// As MonoGame.Extended draws a rectangle: its edges `pen.Width` wide, inside it.
	public override void DrawRectangle(Pen pen, DrawingRectangle rectangle)
	{
		if (pen.Color == DrawingColor.Transparent)
			return;

		var r = ToGui(rectangle);
		var width = pen.Width;
		var color = pen.Color.ToUnityColor();
		Fill(new Rect(r.x, r.y, r.width, width), color);
		Fill(new Rect(r.x, r.y, width, r.height), color);
		Fill(new Rect(r.xMax - width, r.y, width, r.height), color);
		Fill(new Rect(r.x, r.yMax - width, r.width, width), color);
	}

	public override void DrawRectangle(Pen pen, int x, int y, int width, int height) =>
		DrawRectangle(pen, new DrawingRectangle(x, y, width, height));

	public override void DrawRectangle(Pen pen, float x, float y, float width, float height) =>
		DrawRectangle(pen, DrawingRectangle.Round(new DrawingRectangleF(x, y, width, height)));

	private void DrawString(string? text, IUnityFont? font, Brush brush, DrawingPointF point)
	{
		if (string.IsNullOrEmpty(text))
			return;

		if (font is null)
			return;

		switch (brush)
		{
			case SolidBrush b:
				if (b.Color == DrawingColor.Transparent)
					return;

				font.Draw(text!, ToGui(DrawingPoint.Round(point)), b.Color.ToUnityColor());
				break;

			default:
				throw new NotImplementedException();
		}
	}

	public override void DrawString(string? text, IFont font, Brush brush, DrawingPointF point) =>
		DrawString(text, font as IUnityFont, brush, point);

	public override void FillRectangle(Brush brush, DrawingRectangle rectangle)
	{
		switch (brush)
		{
			case SolidBrush b:
				if (b.Color == DrawingColor.Transparent)
					return;

				Fill(ToGui(rectangle), b.Color.ToUnityColor());
				break;

			default:
				throw new NotImplementedException();
		}
	}

	public override void FillRectangle(Brush brush, DrawingRectangleF rectangle) =>
		FillRectangle(brush, DrawingRectangle.Round(rectangle));

	public override void FillRectangle(Brush brush, int x, int y, int width, int height) =>
		FillRectangle(brush, new DrawingRectangle(x, y, width, height));

	public override void FillRectangle(Brush brush, float x, float y, float width, float height) =>
		FillRectangle(brush, new DrawingRectangleF(x, y, width, height));

	private static DrawingSizeF MeasureString(string? text, IUnityFont? font)
	{
		if (font is null)
			return DrawingSizeF.Empty;

		var size = font.MeasureString(text);
		return new DrawingSizeF(size.x, size.y);
	}

	public override DrawingSizeF MeasureString(string? text, IFont font) =>
		MeasureString(text, font as IUnityFont);
}
