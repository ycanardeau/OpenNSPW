using UnityEngine;
using DrawingColor = System.Drawing.Color;
using UnityColor = UnityEngine.Color;

namespace Aigamo.Saruhashi.Unity;

public static class Extensions
{
	// Command stands for Control on a Mac, as Command with C copies there.
	private static readonly bool s_commandIsControl =
		Application.platform is RuntimePlatform.OSXEditor or RuntimePlatform.OSXPlayer;

	public static DrawingColor ToDrawingColor(this UnityColor color)
	{
		Color32 color32 = color;
		return DrawingColor.FromArgb(color32.a, color32.r, color32.g, color32.b);
	}

	public static UnityColor ToUnityColor(this DrawingColor color) =>
		new Color32(color.R, color.G, color.B, color.A);

	/// <summary>
	/// Saruhashi's key for a Unity key: Windows' virtual-key code, or <see cref="Keys.None"/> for a key it has none for.
	/// </summary>
	public static Keys ToKeys(this KeyCode keyCode) =>
		keyCode switch
		{
			>= KeyCode.A and <= KeyCode.Z => Keys.A + (keyCode - KeyCode.A),
			>= KeyCode.Alpha0 and <= KeyCode.Alpha9 => Keys.D0 + (keyCode - KeyCode.Alpha0),
			>= KeyCode.Keypad0 and <= KeyCode.Keypad9 => Keys.NumPad0 + (keyCode - KeyCode.Keypad0),
			>= KeyCode.F1 and <= KeyCode.F15 => Keys.F1 + (keyCode - KeyCode.F1),
			KeyCode.Backspace => Keys.Back,
			KeyCode.Tab => Keys.Tab,
			KeyCode.Clear => Keys.Clear,
			KeyCode.Return or KeyCode.KeypadEnter => Keys.Return,
			KeyCode.Pause => Keys.Pause,
			KeyCode.Escape => Keys.Escape,
			KeyCode.Space => Keys.Space,
			KeyCode.Delete => Keys.Delete,
			KeyCode.KeypadPeriod => Keys.Decimal,
			KeyCode.KeypadDivide => Keys.Divide,
			KeyCode.KeypadMultiply => Keys.Multiply,
			KeyCode.KeypadMinus => Keys.Subtract,
			KeyCode.KeypadPlus => Keys.Add,
			KeyCode.UpArrow => Keys.Up,
			KeyCode.DownArrow => Keys.Down,
			KeyCode.RightArrow => Keys.Right,
			KeyCode.LeftArrow => Keys.Left,
			KeyCode.Insert => Keys.Insert,
			KeyCode.Home => Keys.Home,
			KeyCode.End => Keys.End,
			KeyCode.PageUp => Keys.PageUp,
			KeyCode.PageDown => Keys.PageDown,
			KeyCode.Numlock => Keys.NumLock,
			KeyCode.CapsLock => Keys.CapsLock,
			KeyCode.ScrollLock => Keys.Scroll,
			KeyCode.LeftShift => Keys.LShiftKey,
			KeyCode.RightShift => Keys.RShiftKey,
			KeyCode.LeftControl => Keys.LControlKey,
			KeyCode.RightControl => Keys.RControlKey,
			KeyCode.LeftAlt => Keys.LMenu,
			KeyCode.RightAlt => Keys.RMenu,
			KeyCode.LeftWindows => Keys.LWin,
			KeyCode.RightWindows => Keys.RWin,
			KeyCode.Menu => Keys.Apps,
			KeyCode.Print => Keys.PrintScreen,
			KeyCode.Help => Keys.Help,
			KeyCode.Semicolon => Keys.OemSemicolon,
			KeyCode.Equals => Keys.Oemplus,
			KeyCode.Comma => Keys.Oemcomma,
			KeyCode.Minus => Keys.OemMinus,
			KeyCode.Period => Keys.OemPeriod,
			KeyCode.Slash => Keys.OemQuestion,
			KeyCode.BackQuote => Keys.Oemtilde,
			KeyCode.LeftBracket => Keys.OemOpenBrackets,
			KeyCode.Backslash => Keys.OemPipe,
			KeyCode.RightBracket => Keys.OemCloseBrackets,
			KeyCode.Quote => Keys.OemQuotes,
			_ => Keys.None,
		};

	/// <summary>
	/// Saruhashi's arguments for a key that an IMGUI event reports pressed or released: the key, with the modifiers held
	/// then.
	/// </summary>
	public static KeyEventArgs ToKeyEventArgs(this Event e)
	{
		var keyData = e.keyCode.ToKeys();
		if (e.shift)
			keyData |= Keys.Shift;

		if (e.control || (e.command && s_commandIsControl))
			keyData |= Keys.Control;

		if (e.alt)
			keyData |= Keys.Alt;

		return new KeyEventArgs(keyData);
	}
}
