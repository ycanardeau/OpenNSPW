using System.Drawing;

namespace Aigamo.Saruhashi;

public abstract class Graphics : IDisposable
{
	private bool _disposed;

	protected Graphics(Control control)
	{
		Control = control;
		Clip = new Region(control.ClipRectangle);
	}

	~Graphics() => Dispose(false);

	public Region Clip { get; }
	public RectangleF ClipBounds => Clip.GetBounds(this);
	protected internal Control Control { get; }

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
			return;

		_disposed = true;
	}

	public abstract void DrawImage(IImage image, Point point);
	public abstract void DrawImage(IImage image, PointF point);

	public abstract void DrawLine(Pen pen, PointF point1, PointF point2);
	public abstract void DrawLine(Pen pen, int x1, int y1, int x2, int y2);
	public abstract void DrawLine(Pen pen, float x1, float y1, float x2, float y2);
	public abstract void DrawLine(Pen pen, Point point1, Point point2);

	public abstract void DrawRectangle(Pen pen, Rectangle rectangle);
	public abstract void DrawRectangle(Pen pen, int x, int y, int width, int height);
	public abstract void DrawRectangle(Pen pen, float x, float y, float width, float height);

	public abstract void DrawString(string? text, IFont font, Brush brush, PointF point);

	public abstract void FillRectangle(Brush brush, Rectangle rectangle);
	public abstract void FillRectangle(Brush brush, RectangleF rectangle);
	public abstract void FillRectangle(Brush brush, int x, int y, int width, int height);
	public abstract void FillRectangle(Brush brush, float x, float y, float width, float height);

	public abstract SizeF MeasureString(string? text, IFont font);
}
