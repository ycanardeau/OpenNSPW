using System.Drawing;

namespace Aigamo.Saruhashi.Tests;

internal sealed class FakeFont : IFont { }

// A backend that draws nothing and measures each character 10 pixels wide and 16 high, and records the strings drawn.
internal sealed class FakeGraphics(
	Control control,
	List<(string Text, Color Color, PointF Point)> strings
) : Graphics(control)
{
	public const int CharWidth = 10;
	public const int LineHeight = 16;

	private readonly List<(string Text, Color Color, PointF Point)> _strings = strings;

	public override void DrawImage(IImage image, Point point) { }

	public override void DrawImage(IImage image, PointF point) { }

	public override void DrawLine(Pen pen, PointF point1, PointF point2) { }

	public override void DrawLine(Pen pen, int x1, int y1, int x2, int y2) { }

	public override void DrawLine(Pen pen, float x1, float y1, float x2, float y2) { }

	public override void DrawLine(Pen pen, Point point1, Point point2) { }

	public override void DrawRectangle(Pen pen, Rectangle rectangle) { }

	public override void DrawRectangle(Pen pen, int x, int y, int width, int height) { }

	public override void DrawRectangle(Pen pen, float x, float y, float width, float height) { }

	public override void DrawString(string? text, IFont font, Brush brush, PointF point)
	{
		if (Control is TextBox && text is not null && brush is SolidBrush solid)
			_strings.Add((text, solid.Color, point));
	}

	public override void FillRectangle(Brush brush, Rectangle rectangle) { }

	public override void FillRectangle(Brush brush, RectangleF rectangle) { }

	public override void FillRectangle(Brush brush, int x, int y, int width, int height) { }

	public override void FillRectangle(Brush brush, float x, float y, float width, float height) { }

	public override SizeF MeasureString(string? text, IFont font) =>
		new((text?.Length ?? 0) * CharWidth, LineHeight);
}

internal sealed class FakeGraphicsFactory : IGraphicsFactory
{
	public List<(string Text, Color Color, PointF Point)> Strings { get; } = [];

	public Graphics Create(Control control) => new FakeGraphics(control, Strings);
}

// A clipboard of the test's own, as the system's would be.
internal sealed class FakeClipboard : IClipboard
{
	public string Text { get; set; } = string.Empty;

	public string GetText() => Text;

	public void SetText(string text) => Text = text;
}
