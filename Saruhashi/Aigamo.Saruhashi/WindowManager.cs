using System.Drawing;

namespace Aigamo.Saruhashi;

public sealed class WindowManager
{
	public WindowManager(
		Rectangle bounds,
		GraphicsFactory graphicsFactory,
		IFont? defaultFont = null
	)
	{
		Root = new Control(this) { Bounds = bounds, BackColor = Color.Transparent };
		GraphicsFactory = graphicsFactory;
		DefaultFont = defaultFont;
	}

	public Control? Capture { get; set; }
	private Control CaptureOrRoot => Capture ?? Root;

	/// <summary>
	/// Where the controls cut, copy and paste text (<see cref="TextBox"/>): one of the window manager's own, which only this
	/// app pastes from, until the backend gives the system's.
	/// </summary>
	public IClipboard Clipboard { get; set; } = new LocalClipboard();

	public IFont? DefaultFont { get; }
	private Control? Focus { get; set; }
	private Control FocusOrRoot => Focus ?? Root;
	public GraphicsFactory GraphicsFactory { get; }

	/// <summary>
	/// The modifier keys held (<see cref="Keys.Shift"/>, <see cref="Keys.Control"/>, <see cref="Keys.Alt"/> and
	/// <see cref="Keys.Command"/>), as the last
	/// key pressed or released had them, for a click that a modifier changes, such as Shift extending a selection.
	/// </summary>
	public Keys ModifierKeys { get; private set; }

	internal Control? MouseOver { get; set; }
	internal Control? PreviousMouseOver { get; set; }
	public Control Root { get; }

	public Control? GetFocus() => Focus;

	public void OnKeyDown(KeyEventArgs e)
	{
		ModifierKeys = e.Modifiers;
		FocusOrRoot.HandleKeyDown(e);
	}

	public void OnKeyPress(KeyPressEventArgs e) => FocusOrRoot.HandleKeyPress(e);

	public void OnKeyUp(KeyEventArgs e)
	{
		ModifierKeys = e.Modifiers;
		FocusOrRoot.HandleKeyUp(e);
	}

	public void OnMouseDown(MouseEventArgs e) => CaptureOrRoot.HandleMouseDown(e);

	public void OnMouseMove(MouseEventArgs e) => CaptureOrRoot.HandleMouseMove(e);

	public void OnMouseUp(MouseEventArgs e)
	{
		var captureOrRoot = CaptureOrRoot;
		if (captureOrRoot.HandleMouseUp(e) == captureOrRoot)
			OnMouseMove(new MouseEventArgs(MouseButtons.None, e.Clicks, e.Location, e.Delta));
	}

	public void OnMouseWheel(MouseEventArgs e) => Root.HandleMouseWheel(e);

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
