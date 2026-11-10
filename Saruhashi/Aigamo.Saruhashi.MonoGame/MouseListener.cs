using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input.InputListeners;
using MonoGame.Extended.ViewportAdapters;
using XnaButtonState = Microsoft.Xna.Framework.Input.ButtonState;

namespace Aigamo.Saruhashi.MonoGame;

// getState: where the pointer is and which buttons are held, read once each update; MonoGame's mouse when null, or what
// stands in for it, such as a touch screen's touches.
public class MouseListener(
	ViewportAdapter? viewportAdapter = null,
	Func<MouseState>? getState = null
) : InputListener
{
	public ViewportAdapter? ViewportAdapter { get; } = viewportAdapter;

	private readonly Func<MouseState> _getState = getState ?? Mouse.GetState;
	private MouseState _state;
	private MouseState _previousState;

	public event EventHandler<MouseEventArgs>? MouseDown;
	public event EventHandler<MouseEventArgs>? MouseMove;
	public event EventHandler<MouseEventArgs>? MouseUp;
	public event EventHandler<MouseEventArgs>? MouseWheel;

	private Point Position => ViewportAdapter?.PointToScreen(_state.Position) ?? _state.Position;
	private Point PreviousPosition =>
		ViewportAdapter?.PointToScreen(_previousState.Position) ?? _previousState.Position;

	// Windows' defaults: a second press of a button within 500 ms (GetDoubleClickTime) and in a 4-pixel square around the
	// first (SM_CXDOUBLECLK, SM_CYDOUBLECLK) is a double click.
	private static readonly TimeSpan DoubleClickTime = TimeSpan.FromMilliseconds(500);
	private const int DoubleClickSize = 4;

	private MouseButtons _lastPressed;
	private TimeSpan _lastPressedAt;
	private Point _lastPressedPosition;
	private int _lastClicks;

	// How many times `button`, pressed at `time`, has been clicked in a row: 2 for a double click, and 1 otherwise, also
	// for the press after a double click, as Windows counts.
	private int CountClicks(MouseButtons button, TimeSpan time)
	{
		var position = Position;
		var clicks =
			_lastClicks == 1
			&& button == _lastPressed
			&& time - _lastPressedAt <= DoubleClickTime
			&& Math.Abs(position.X - _lastPressedPosition.X) <= DoubleClickSize / 2
			&& Math.Abs(position.Y - _lastPressedPosition.Y) <= DoubleClickSize / 2
				? 2
				: 1;
		(_lastPressed, _lastPressedAt, _lastPressedPosition, _lastClicks) = (
			button,
			time,
			position,
			clicks
		);
		return clicks;
	}

	public override void Update(GameTime gameTime)
	{
		_state = _getState();

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

			MouseMove?.Invoke(
				this,
				new MouseEventArgs(buttons, clicks: 0, Position.ToDrawingPoint(), delta: 0)
			);
		}

		if (
			_state.LeftButton == XnaButtonState.Pressed
			&& _previousState.LeftButton == XnaButtonState.Released
		)
			MouseDown?.Invoke(
				this,
				new MouseEventArgs(
					MouseButtons.Left,
					clicks: CountClicks(MouseButtons.Left, gameTime.TotalGameTime),
					Position.ToDrawingPoint(),
					delta: 0
				)
			);

		if (
			_state.RightButton == XnaButtonState.Pressed
			&& _previousState.RightButton == XnaButtonState.Released
		)
			MouseDown?.Invoke(
				this,
				new MouseEventArgs(
					MouseButtons.Right,
					clicks: CountClicks(MouseButtons.Right, gameTime.TotalGameTime),
					Position.ToDrawingPoint(),
					delta: 0
				)
			);

		if (
			_state.MiddleButton == XnaButtonState.Pressed
			&& _previousState.MiddleButton == XnaButtonState.Released
		)
			MouseDown?.Invoke(
				this,
				new MouseEventArgs(
					MouseButtons.Middle,
					clicks: CountClicks(MouseButtons.Middle, gameTime.TotalGameTime),
					Position.ToDrawingPoint(),
					delta: 0
				)
			);

		if (
			_state.XButton1 == XnaButtonState.Pressed
			&& _previousState.XButton1 == XnaButtonState.Released
		)
			MouseDown?.Invoke(
				this,
				new MouseEventArgs(
					MouseButtons.XButton1,
					clicks: CountClicks(MouseButtons.XButton1, gameTime.TotalGameTime),
					Position.ToDrawingPoint(),
					delta: 0
				)
			);

		if (
			_state.XButton2 == XnaButtonState.Pressed
			&& _previousState.XButton2 == XnaButtonState.Released
		)
			MouseDown?.Invoke(
				this,
				new MouseEventArgs(
					MouseButtons.XButton2,
					clicks: CountClicks(MouseButtons.XButton2, gameTime.TotalGameTime),
					Position.ToDrawingPoint(),
					delta: 0
				)
			);

		if (
			_state.LeftButton == XnaButtonState.Released
			&& _previousState.LeftButton == XnaButtonState.Pressed
		)
			MouseUp?.Invoke(
				this,
				new MouseEventArgs(
					MouseButtons.Left,
					clicks: 1,
					Position.ToDrawingPoint(),
					delta: 0
				)
			);

		if (
			_state.RightButton == XnaButtonState.Released
			&& _previousState.RightButton == XnaButtonState.Pressed
		)
			MouseUp?.Invoke(
				this,
				new MouseEventArgs(
					MouseButtons.Right,
					clicks: 1,
					Position.ToDrawingPoint(),
					delta: 0
				)
			);

		if (
			_state.MiddleButton == XnaButtonState.Released
			&& _previousState.MiddleButton == XnaButtonState.Pressed
		)
			MouseUp?.Invoke(
				this,
				new MouseEventArgs(
					MouseButtons.Middle,
					clicks: 1,
					Position.ToDrawingPoint(),
					delta: 0
				)
			);

		if (
			_state.XButton1 == XnaButtonState.Released
			&& _previousState.XButton1 == XnaButtonState.Pressed
		)
			MouseUp?.Invoke(
				this,
				new MouseEventArgs(
					MouseButtons.XButton1,
					clicks: 1,
					Position.ToDrawingPoint(),
					delta: 0
				)
			);

		if (
			_state.XButton2 == XnaButtonState.Released
			&& _previousState.XButton2 == XnaButtonState.Pressed
		)
			MouseUp?.Invoke(
				this,
				new MouseEventArgs(
					MouseButtons.XButton2,
					clicks: 1,
					Position.ToDrawingPoint(),
					delta: 0
				)
			);

		// The delta is in the wheel's units, 120 a notch, as Windows gives it.
		if (_state.ScrollWheelValue != _previousState.ScrollWheelValue)
			MouseWheel?.Invoke(
				this,
				new MouseEventArgs(
					MouseButtons.None,
					clicks: 0,
					Position.ToDrawingPoint(),
					delta: _state.ScrollWheelValue - _previousState.ScrollWheelValue
				)
			);

		_previousState = _state;
	}
}
