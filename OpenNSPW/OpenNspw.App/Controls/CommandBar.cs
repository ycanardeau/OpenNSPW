using Aigamo.Saruhashi;
using DrawingRectangle = System.Drawing.Rectangle;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace OpenNspw.App.Controls;

// Buttons for what the battle takes only from the keyboard and the right button, for touch screens and the mouse alike:
// the game's speed, slower (G), back to 1 (R) and faster (F); fast-forward while held (Space); the chat (Enter); and
// deselecting the unit (the right button). A key's button holds the key down while it is pressed, so the game takes it
// as it takes the keyboard's.
internal sealed class CommandBar : Control
{
	private const int ButtonWidth = 92;
	private const int ButtonHeight = 40;
	private const int Gap = 6;
	private const int Padding = 6;
	private const int Buttons = 6;

	private readonly GameSession _session;
	private readonly UIFonts _fonts;

	// A button of the bar; a `sign` is drawn larger, as a symbol rather than a word.
	private ActionButton AddButton(int index, string text, bool sign = false)
	{
		var button = new ActionButton(_fonts)
		{
			Bounds = new DrawingRectangle(
				Padding,
				Padding + index * (ButtonHeight + Gap),
				ButtonWidth,
				ButtonHeight
			),
			Text = text,
			FontSize = sign ? 24 : 15,
		};
		Controls.Add(button);
		return button;
	}

	// A button that holds `key` down while the left button presses it.
	private void AddKeyButton(int index, string text, XnaKeys key, bool sign = false)
	{
		var button = AddButton(index, text, sign);
		button.MouseDown += (_, e) =>
		{
			if (e.Button == MouseButtons.Left)
				_session.PostKey(key, true);
		};
		button.MouseUp += (_, e) =>
		{
			if (e.Button == MouseButtons.Left)
				_session.PostKey(key, false);
		};
	}

	public CommandBar(GameSession session, UIFonts fonts)
	{
		_session = session;
		_fonts = fonts;

		// Dark under the buttons, which are only outlined otherwise, over the sea.
		BackColor = Theme.Alpha(Theme.Navy, 0.85f);
		AddKeyButton(0, Texts.Slower, XnaKeys.G, sign: true);
		AddKeyButton(1, Texts.NormalSpeed, XnaKeys.R);
		AddKeyButton(2, Texts.Faster, XnaKeys.F, sign: true);
		AddKeyButton(3, Texts.FastForward, XnaKeys.Space, sign: true);
		AddKeyButton(4, Texts.Chat, XnaKeys.Enter);
		AddButton(5, Texts.Deselect).Click += (_, _) => session.Commands.RightClick();
		Size = new(ButtonWidth + Padding * 2, Buttons * (ButtonHeight + Gap) - Gap + Padding * 2);
	}
}
