using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input.InputListeners;
using MonoGame.Extended.ViewportAdapters;
using XnaButtonState = Microsoft.Xna.Framework.Input.ButtonState;

namespace Aigamo.Saruhashi.MonoGame;

public class MouseListener : InputListener
{
	public ViewportAdapter? ViewportAdapter { get; }

	private MouseState _state;
	private MouseState _previousState;

	public event EventHandler<MouseEventArgs>? MouseDown;
	public event EventHandler<MouseEventArgs>? MouseMove;
	public event EventHandler<MouseEventArgs>? MouseUp;

	public MouseListener(ViewportAdapter? viewportAdapter = null)
	{
		ViewportAdapter = viewportAdapter;
	}

	private Point Position => ViewportAdapter?.PointToScreen(_state.Position) ?? _state.Position;
	private Point PreviousPosition => ViewportAdapter?.PointToScreen(_previousState.Position) ?? _previousState.Position;

	public override void Update(GameTime gameTime)
	{
		_state = Mouse.GetState();

		if (Position != PreviousPosition)
		{
			var buttons = MouseButtons.None;

			if (_state.LeftButton == XnaButtonState.Pressed)
				buttons |= MouseButtons.Left;

			if (_state.RightButton == XnaButtonState.Pressed)
				buttons |= MouseButtons.Right;

			if (_state.MiddleButton == XnaButtonState.Pressed)
				buttons |= MouseButtons.Middle;

			if (_state.XButton1 == XnaButtonState.Pressed)
				buttons |= MouseButtons.XButton1;

			if (_state.XButton2 == XnaButtonState.Pressed)
				buttons |= MouseButtons.XButton2;

			MouseMove?.Invoke(this, new MouseEventArgs(buttons, clicks: 0, Position.ToDrawingPoint(), delta: 0));
		}

		if (_state.LeftButton == XnaButtonState.Pressed && _previousState.LeftButton == XnaButtonState.Released)
			MouseDown?.Invoke(this, new MouseEventArgs(MouseButtons.Left, clicks: 0, Position.ToDrawingPoint(), delta: 0));

		if (_state.RightButton == XnaButtonState.Pressed && _previousState.RightButton == XnaButtonState.Released)
			MouseDown?.Invoke(this, new MouseEventArgs(MouseButtons.Right, clicks: 0, Position.ToDrawingPoint(), delta: 0));

		if (_state.MiddleButton == XnaButtonState.Pressed && _previousState.MiddleButton == XnaButtonState.Released)
			MouseDown?.Invoke(this, new MouseEventArgs(MouseButtons.Middle, clicks: 0, Position.ToDrawingPoint(), delta: 0));

		if (_state.XButton1 == XnaButtonState.Pressed && _previousState.XButton1 == XnaButtonState.Released)
			MouseDown?.Invoke(this, new MouseEventArgs(MouseButtons.XButton1, clicks: 0, Position.ToDrawingPoint(), delta: 0));

		if (_state.XButton2 == XnaButtonState.Pressed && _previousState.XButton2 == XnaButtonState.Released)
			MouseDown?.Invoke(this, new MouseEventArgs(MouseButtons.XButton2, clicks: 0, Position.ToDrawingPoint(), delta: 0));

		if (_state.LeftButton == XnaButtonState.Released && _previousState.LeftButton == XnaButtonState.Pressed)
			MouseUp?.Invoke(this, new MouseEventArgs(MouseButtons.Left, clicks: 0, Position.ToDrawingPoint(), delta: 0));

		if (_state.RightButton == XnaButtonState.Released && _previousState.RightButton == XnaButtonState.Pressed)
			MouseUp?.Invoke(this, new MouseEventArgs(MouseButtons.Right, clicks: 0, Position.ToDrawingPoint(), delta: 0));

		if (_state.MiddleButton == XnaButtonState.Released && _previousState.MiddleButton == XnaButtonState.Pressed)
			MouseUp?.Invoke(this, new MouseEventArgs(MouseButtons.Middle, clicks: 0, Position.ToDrawingPoint(), delta: 0));

		if (_state.XButton1 == XnaButtonState.Released && _previousState.XButton1 == XnaButtonState.Pressed)
			MouseUp?.Invoke(this, new MouseEventArgs(MouseButtons.XButton1, clicks: 0, Position.ToDrawingPoint(), delta: 0));

		if (_state.XButton2 == XnaButtonState.Released && _previousState.XButton2 == XnaButtonState.Pressed)
			MouseUp?.Invoke(this, new MouseEventArgs(MouseButtons.XButton2, clicks: 0, Position.ToDrawingPoint(), delta: 0));

		_previousState = _state;
	}
}
