using System.Drawing;

namespace Aigamo.Saruhashi;

public abstract class ButtonBase : Control
{
	[Flags]
	private enum States
	{
		MouseOver = 0x0001,
		MouseDown = 0x0002,
		MousePressed = 0x0004,
		InButtonUp = 0x0008,
		CurrentlyAnimating = 0x0010,
		AutoEllipsis = 0x0020,
		IsDefault = 0x0040,
		UseMnemonic = 0x0080,
		ShowToolTip = 0x0100,
	}

	private IButtonRenderer? _buttonRenderer;
	private States _state;

	// OPTIMIZE
	public IButtonRenderer ButtonRenderer
	{
		get => _buttonRenderer ??= new ButtonRenderer();
		set => _buttonRenderer = value;
	}

	protected override Size DefaultSize => new Size(75, 23);
	internal bool MouseIsPressed => _state.HasFlag(States.MousePressed);
	internal bool MouseIsDown => _state.HasFlag(States.MouseDown);
	internal bool MouseIsOver => _state.HasFlag(States.MouseOver);

	protected override void OnLostFocus(EventArgs e)
	{
		base.OnLostFocus(e);

		_state &= ~States.MouseDown;
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);

		if (e.Button == MouseButtons.Left)
		{
			_state |= States.MouseDown;
			_state |= States.MousePressed;
		}
	}

	protected override void OnMouseEnter(EventArgs e)
	{
		base.OnMouseEnter(e);

		_state |= States.MouseOver;
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);

		_state &= ~States.MouseOver;
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);

		if (MouseIsPressed)
		{
			if (Capture && ClientRectangle.Contains(e.Location))
				_state |= States.MouseDown;
			else
				_state &= ~States.MouseDown;
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);

		_state &= ~States.MousePressed;
		_state &= ~States.MouseDown;
	}
}
