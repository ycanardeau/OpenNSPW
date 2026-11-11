using UnityEngine;
using DrawingPoint = System.Drawing.Point;

namespace Aigamo.Saruhashi.Unity;

/// <summary>
/// Sends a window manager IMGUI's mouse and keyboard and draws it, each time a <see cref="MonoBehaviour"/>'s
/// <c>OnGUI</c> calls <see cref="OnGUI"/>.
/// </summary>
public sealed class UnityGui(WindowManager windowManager)
{
	// The buttons held, as the mouse's events left them.
	private MouseButtons _buttons;

	// Where the mouse was last sent, if it was.
	private DrawingPoint? _mouseLocation;

	/// <summary>
	/// Where the window manager's pixels are on the screen, in IMGUI's points, such as a scale to fit a layout to the
	/// screen: none by default.
	/// </summary>
	public Matrix4x4 Matrix { get; set; } = Matrix4x4.identity;

	public WindowManager WindowManager { get; } = windowManager;

	private static MouseButtons ToMouseButtons(int button) =>
		button switch
		{
			0 => MouseButtons.Left,
			1 => MouseButtons.Right,
			2 => MouseButtons.Middle,
			3 => MouseButtons.XButton1,
			4 => MouseButtons.XButton2,
			_ => MouseButtons.None,
		};

	// A point of IMGUI's, on the screen, in the window manager's pixels.
	private DrawingPoint ToWindowManager(Vector2 point)
	{
		var p = Matrix.inverse.MultiplyPoint3x4(point);
		return new DrawingPoint(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
	}

	// Sends the mouse where it is, if it moved: IMGUI sends a player no event when the mouse moves with no button held.
	private void MoveMouse(DrawingPoint location)
	{
		if (_mouseLocation == location)
			return;

		_mouseLocation = location;
		WindowManager.OnMouseMove(new MouseEventArgs(_buttons, clicks: 0, location, delta: 0));
	}

	private void Draw()
	{
		var matrix = GUI.matrix;
		GUI.matrix = Matrix;
		try
		{
			WindowManager.Draw();
		}
		finally
		{
			GUI.matrix = matrix;
		}
	}

	public void OnGUI()
	{
		var e = Event.current;
		switch (e.type)
		{
			case EventType.MouseDown:
			{
				var location = ToWindowManager(e.mousePosition);
				MoveMouse(location);

				var button = ToMouseButtons(e.button);
				_buttons |= button;
				WindowManager.OnMouseDown(new MouseEventArgs(button, e.clickCount, location, delta: 0));
				break;
			}

			case EventType.MouseUp:
			{
				var location = ToWindowManager(e.mousePosition);
				MoveMouse(location);

				var button = ToMouseButtons(e.button);
				_buttons &= ~button;
				WindowManager.OnMouseUp(new MouseEventArgs(button, e.clickCount, location, delta: 0));
				break;
			}

			// The delta is in the wheel's units, 120 a notch and up away from the user, as Windows gives it: IMGUI's is in
			// lines, 3 a notch and down away from the user.
			case EventType.ScrollWheel:
			{
				var location = ToWindowManager(e.mousePosition);
				MoveMouse(location);
				WindowManager.OnMouseWheel(
					new MouseEventArgs(
						MouseButtons.None,
						clicks: 0,
						location,
						delta: Mathf.RoundToInt(-e.delta.y * 40)
					)
				);
				break;
			}

			// IMGUI sends a key and the character it types as two events: the key with no character, then the character
			// with no key. Enter types a line feed, which Windows types as a carriage return.
			case EventType.KeyDown:
				if (e.keyCode != KeyCode.None)
					WindowManager.OnKeyDown(e.ToKeyEventArgs());

				if (e.character != '\0')
					WindowManager.OnKeyPress(new KeyPressEventArgs(e.character == '\n' ? '\r' : e.character));

				break;

			case EventType.KeyUp:
				if (e.keyCode != KeyCode.None)
					WindowManager.OnKeyUp(e.ToKeyEventArgs());

				break;

			case EventType.Repaint:
				MoveMouse(ToWindowManager(e.mousePosition));
				Draw();
				break;

			default:
				MoveMouse(ToWindowManager(e.mousePosition));
				break;
		}
	}
}
