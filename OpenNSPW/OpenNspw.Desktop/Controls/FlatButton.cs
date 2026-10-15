using Aigamo.Saruhashi;
using DrawingColor = System.Drawing.Color;

namespace OpenNspw.Desktop.Controls;

// A button that draws itself, with `Hover` going from 0 to 1 while the mouse is over it, for its animation. It clicks
// when the left button is released over it, as a button does.
internal abstract class FlatButton : ButtonBase
{
	private float _lastPaint = Painter.Now;
	private bool _isMouseOver;

	protected FlatButton()
	{
		SetStyle(ControlStyles.StandardClick, false);
		BackColor = DrawingColor.Transparent;
	}

	protected float Hover { get; private set; }

	protected bool IsPressed { get; private set; }

	// Shown as hovered without the mouse, such as the item the keyboard has chosen.
	public bool IsHighlighted { get; set; }

	public event EventHandler? Hovered;

	protected override void OnMouseEnter(EventArgs e)
	{
		base.OnMouseEnter(e);
		_isMouseOver = true;
		Hovered?.Invoke(this, EventArgs.Empty);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		_isMouseOver = false;
		IsPressed = false;
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (e.Button == MouseButtons.Left)
			IsPressed = true;
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left && IsPressed && Enabled && WindowManager.WindowFromPoint(PointToScreen(e.Location)) == this)
		{
			IsPressed = false;
			OnClick(EventArgs.Empty);
		}

		IsPressed = false;
		base.OnMouseUp(e);
	}

	// Clicks the button from the keyboard.
	public void Press()
	{
		if (Enabled)
			OnClick(EventArgs.Empty);
	}

	protected abstract void PaintButton(PaintEventArgs e);

	protected sealed override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		var now = Painter.Now;
		var target = Enabled && (_isMouseOver || IsHighlighted) ? 1 : 0;
		Hover = Painter.Approach(Hover, target, now - _lastPaint, 7);
		_lastPaint = now;
		PaintButton(e);
	}
}
