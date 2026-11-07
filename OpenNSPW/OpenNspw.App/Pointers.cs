using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace OpenNspw.App;

// Where the player points, in the window's pixels, and which buttons are held, as MonoGame's mouse gives them: read once
// each update (Update), before the UI (Saruhashi's MouseListener) and the game (GameSession) read it (State).
internal interface IPointer
{
	MouseState State { get; }

	void Update();
}

// The mouse.
internal sealed class MousePointer : IPointer
{
	public MouseState State { get; private set; }

	public void Update()
	{
		State = Mouse.GetState();
	}
}

// A touch screen. The first finger holds the left button where it is, and once it is lifted, the pointer stays where it
// was lifted. A second finger, from when it comes down until all are lifted, lets the left button go and holds XButton1,
// which tells a tap from what follows; the pointer is then halfway between the two. Spread or pinched, the two fingers
// turn the wheel, a notch (120) for each quarter of their distance, to zoom (Battlefield); lifted without that, they click
// the right button, which deselects. The middle button is never used.
internal sealed class TouchPointer(GameWindow window) : IPointer
{
	// How far the distance between two fingers changes, as a ratio, before they zoom rather than tap.
	private const float PinchStart = 0.08f;

	private readonly GameWindow _window = window;

	// Whether the touch has had a second finger since its first came down, until all are lifted.
	private bool _isTwoFingers;

	// Whether the two fingers have zoomed, from how far apart they came down, and the wheel then.
	private bool _isPinching;
	private float _pinchDistance;
	private int _pinchWheel;

	private int _wheel;

	// Whether the right button is clicked in this update: two fingers lifted without zooming.
	private bool _isRightClick;

	public MouseState State { get; private set; }

	// A touch in the window's pixels, as the mouse is, which the viewport turns into the game's (ViewportAdapter): MonoGame
	// gives it in the pixels of the window's client area, which it may move into the window, as MonoGame's Android does to
	// keep the back buffer's shape (ClientBounds).
	private Vector2 ToWindow(TouchLocation touch) =>
		touch.Position + _window.ClientBounds.Location.ToVector2();

	public void Update()
	{
		TouchPanel.DisplayWidth = _window.ClientBounds.Width;
		TouchPanel.DisplayHeight = _window.ClientBounds.Height;
		var fingers = new List<Vector2>(2);
		Vector2? released = null;
		foreach (var touch in TouchPanel.GetState())
		{
			if (touch.State is TouchLocationState.Pressed or TouchLocationState.Moved)
				fingers.Add(ToWindow(touch));
			else if (touch.State == TouchLocationState.Released)
				released ??= ToWindow(touch);
		}

		var position = State.Position.ToVector2();
		_isRightClick = false;
		if (fingers.Count >= 2)
		{
			var distance = Vector2.Distance(fingers[0], fingers[1]);
			if (!_isTwoFingers)
			{
				(_isTwoFingers, _isPinching) = (true, false);
				(_pinchDistance, _pinchWheel) = (Math.Max(distance, 1), _wheel);
			}

			var ratio = distance / _pinchDistance;
			_isPinching |= Math.Abs(ratio - 1) > PinchStart;
			if (_isPinching && ratio > 0)
				_wheel = _pinchWheel + (int)(120 * MathF.Log(ratio) / MathF.Log(1.25f));
			position = (fingers[0] + fingers[1]) / 2;
		}
		else if (fingers.Count == 1 && !_isTwoFingers)
		{
			position = fingers[0];
		}
		else if (fingers.Count == 0)
		{
			if (_isTwoFingers)
				_isRightClick = !_isPinching;
			else if (released is { } at)
				position = at;
			_isTwoFingers = false;
		}

		State = new MouseState(
			(int)position.X,
			(int)position.Y,
			scrollWheel: _wheel,
			leftButton: fingers.Count > 0 && !_isTwoFingers
				? ButtonState.Pressed
				: ButtonState.Released,
			middleButton: ButtonState.Released,
			rightButton: _isRightClick ? ButtonState.Pressed : ButtonState.Released,
			xButton1: _isTwoFingers ? ButtonState.Pressed : ButtonState.Released,
			xButton2: ButtonState.Released
		);
	}
}
