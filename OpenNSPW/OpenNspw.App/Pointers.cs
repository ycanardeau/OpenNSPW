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

// A touch screen: the first touch on it holds the left button where it is, and once it is lifted, the pointer stays where
// it was lifted. A second finger turns the touch into the right button, which deselects, until all are lifted. The middle
// button and the wheel are never used.
internal sealed class TouchPointer(GameWindow window) : IPointer
{
	private readonly GameWindow _window = window;

	// Whether the touch has had a second finger since its first came down: it is then the right button, until all are
	// lifted.
	private bool _isTwoFingers;

	public MouseState State { get; private set; }

	// A touch in the window's pixels, as the mouse is, which the viewport turns into the game's (ViewportAdapter): MonoGame
	// gives it in the pixels of the window's client area, which it may move into the window, as MonoGame's Android does to
	// keep the back buffer's shape (ClientBounds).
	private Point ToWindow(TouchLocation touch) =>
		touch.Position.ToPoint() + _window.ClientBounds.Location;

	public void Update()
	{
		TouchPanel.DisplayWidth = _window.ClientBounds.Width;
		TouchPanel.DisplayHeight = _window.ClientBounds.Height;
		var position = State.Position;
		var fingers = 0;
		foreach (var touch in TouchPanel.GetState())
		{
			if (touch.State is TouchLocationState.Pressed or TouchLocationState.Moved)
			{
				if (fingers++ == 0)
					position = ToWindow(touch);
			}
			else if (touch.State == TouchLocationState.Released && fingers == 0)
			{
				position = ToWindow(touch);
			}
		}

		var isTouching = fingers > 0;
		_isTwoFingers = isTouching && (_isTwoFingers || fingers > 1);

		State = new MouseState(
			position.X,
			position.Y,
			scrollWheel: 0,
			leftButton: isTouching && !_isTwoFingers ? ButtonState.Pressed : ButtonState.Released,
			middleButton: ButtonState.Released,
			rightButton: _isTwoFingers ? ButtonState.Pressed : ButtonState.Released,
			xButton1: ButtonState.Released,
			xButton2: ButtonState.Released
		);
	}
}
