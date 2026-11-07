using Aigamo.Saruhashi;
using DrawingRectangle = System.Drawing.Rectangle;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace OpenNspw.App.Controls;

// How the command bar's buttons are laid out: in so many columns, each button so large, with its text in a font so large,
// a sign's larger.
internal sealed record CommandBarLayout(
	int ButtonWidth,
	int ButtonHeight,
	int Columns,
	int TextFontSize,
	int SignFontSize
)
{
	// One column, as small as the original's sidebar's buttons.
	public static readonly CommandBarLayout Small = new(92, 40, 1, 15, 24);

	// Two columns of buttons large enough for a finger, and their text to be read, on a small screen.
	public static readonly CommandBarLayout Large = new(150, 64, 2, 22, 32);

	public const int Gap = 6;
	public const int Padding = 6;

	// The button's place in the bar, from its place in the order of the keys: in two columns, the faster button is beside
	// the slower, and the normal speed under them.
	public int Slot(int index) => Columns == 2 && index is 1 or 2 ? 3 - index : index;
}

// Buttons for what the battle takes only from the keyboard and the right button, for touch screens and the mouse alike:
// the game's speed, slower (G), back to 1 (R) and faster (F); fast-forward while held (Space); the chat (Enter); and
// deselecting the unit (the right button). A key's button holds the key down while it is pressed, so the game takes it
// as it takes the keyboard's.
internal sealed class CommandBar : Control
{
	private const int Buttons = 6;

	private readonly GameSession _session;
	private readonly UIFonts _fonts;

	public CommandBarLayout Layout { get; }

	// A button of the bar; a `sign` is drawn larger, as a symbol rather than a word.
	private ActionButton AddButton(int index, string text, bool sign = false)
	{
		var slot = Layout.Slot(index);
		var button = new ActionButton(_fonts)
		{
			Bounds = new DrawingRectangle(
				CommandBarLayout.Padding
					+ slot % Layout.Columns * (Layout.ButtonWidth + CommandBarLayout.Gap),
				CommandBarLayout.Padding
					+ slot / Layout.Columns * (Layout.ButtonHeight + CommandBarLayout.Gap),
				Layout.ButtonWidth,
				Layout.ButtonHeight
			),
			Text = text,
			FontSize = sign ? Layout.SignFontSize : Layout.TextFontSize,
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

	public CommandBar(GameSession session, UIFonts fonts, CommandBarLayout layout)
	{
		_session = session;
		_fonts = fonts;
		Layout = layout;

		// Dark under the buttons, which are only outlined otherwise, over the sea.
		BackColor = Theme.Alpha(Theme.Navy, 0.85f);
		AddKeyButton(0, Texts.Slower, XnaKeys.G, sign: true);
		AddKeyButton(1, Texts.NormalSpeed, XnaKeys.R);
		AddKeyButton(2, Texts.Faster, XnaKeys.F, sign: true);
		AddKeyButton(3, Texts.FastForward, XnaKeys.Space, sign: true);
		AddKeyButton(4, Texts.Chat, XnaKeys.Enter);
		AddButton(5, Texts.Deselect).Click += (_, _) => session.Commands.RightClick();
		var rows = (Buttons + layout.Columns - 1) / layout.Columns;
		Size = new(
			layout.Columns * (layout.ButtonWidth + CommandBarLayout.Gap)
				- CommandBarLayout.Gap
				+ CommandBarLayout.Padding * 2,
			rows * (layout.ButtonHeight + CommandBarLayout.Gap)
				- CommandBarLayout.Gap
				+ CommandBarLayout.Padding * 2
		);
	}
}
