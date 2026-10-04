using System.Drawing;

namespace Aigamo.Saruhashi;

public sealed class WindowManager
{
	public WindowManager(Rectangle bounds, IGraphicsFactory graphicsFactory, IFont? defaultFont = null)
	{
		Root = new Control(this)
		{
			Bounds = bounds,
			BackColor = Color.Transparent,
		};
		GraphicsFactory = graphicsFactory;
		DefaultFont = defaultFont;
	}

	public Control? Capture { get; set; }
	private Control CaptureOrRoot => Capture ?? Root;
	public IFont? DefaultFont { get; }
	private Control? Focus { get; set; }
	private Control FocusOrRoot => Focus ?? Root;
	public IGraphicsFactory GraphicsFactory { get; }
	internal Control? MouseOver { get; set; }
	internal Control? PreviousMouseOver { get; set; }
	public Control Root { get; }

	public Control? GetFocus() => Focus;

	public void OnKeyDown(KeyEventArgs e) => FocusOrRoot.HandleKeyDown(e);
	public void OnKeyPress(KeyPressEventArgs e) => FocusOrRoot.HandleKeyPress(e);
	public void OnKeyUp(KeyEventArgs e) => FocusOrRoot.HandleKeyUp(e);
	public void OnMouseDown(MouseEventArgs e) => CaptureOrRoot.HandleMouseDown(e);
	public void OnMouseMove(MouseEventArgs e) => CaptureOrRoot.HandleMouseMove(e);

	public void OnMouseUp(MouseEventArgs e)
	{
		var captureOrRoot = CaptureOrRoot;
		if (captureOrRoot.HandleMouseUp(e) == captureOrRoot)
			OnMouseMove(new MouseEventArgs(MouseButtons.None, e.Clicks, e.Location, e.Delta));
	}

	public void Draw()
	{
		Root.Draw();
	}

	public bool SetFocus(Control? control)
	{
		Focus = control;
		return true;
	}

	public Control? WindowFromPoint(Point point) => Root.WindowFromPoint(point);
}
