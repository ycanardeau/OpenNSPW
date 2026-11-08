using Microsoft.Xna.Framework.Input;
using OpenNspw.App;

namespace OpenNspw.Desktop;

// The mouse, with its buttons as SDL gives them to the window (SDL_GetMouseState): held only by a press on the window
// itself. MonoGame's Mouse.GetState gives them as they are on the whole screen (SDL_GetGlobalMouseState), so that a click
// on another app, such as on its window over this one, would be held here until the window learns that it is no longer
// active (ActivePointer): a click that the UI and the game would take where it lands on this window. The position and
// the wheel are MonoGame's; so are the buttons if SDL cannot be called where MonoGame loaded it (Sdl).
internal sealed class WindowMousePointer : IPointer
{
	// Whether the buttons are the window's, which the first update tells, once MonoGame has loaded SDL.
	private bool? _takesWindowButtons;

	public MouseState State { get; private set; }

	private static ButtonState Held(uint buttons, uint button) =>
		(buttons & button) != 0 ? ButtonState.Pressed : ButtonState.Released;

	private static bool CanCallSdl()
	{
		try
		{
			if ((Sdl.SDL_WasInit(Sdl.InitVideo) & Sdl.InitVideo) != 0)
				return true;
			Console.Error.WriteLine("SDL is not MonoGame's: the mouse's buttons are those of the whole screen.");
			return false;
		}
		catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
		{
			Console.Error.WriteLine($"SDL cannot be called: {e.Message}");
			return false;
		}
	}

	public void Update()
	{
		var mouse = Mouse.GetState();
		_takesWindowButtons ??= CanCallSdl();
		if (_takesWindowButtons != true)
		{
			State = mouse;
			return;
		}

		var buttons = Sdl.SDL_GetMouseState(0, 0);
		State = new MouseState(
			mouse.X,
			mouse.Y,
			mouse.ScrollWheelValue,
			Held(buttons, Sdl.ButtonLeft),
			Held(buttons, Sdl.ButtonMiddle),
			Held(buttons, Sdl.ButtonRight),
			Held(buttons, Sdl.ButtonX1),
			Held(buttons, Sdl.ButtonX2),
			mouse.HorizontalScrollWheelValue
		);
	}
}
