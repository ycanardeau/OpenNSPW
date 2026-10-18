using Aigamo.Saruhashi;
using DrawingRectangle = System.Drawing.Rectangle;

namespace OpenNspw.Desktop.Controls;

// A rule of a multiplayer game, as the setting screen shows it (UpdateConfigSetting): its name and its value, in the
// game's words, with the buttons that change it on the right.
internal sealed class RuleRow(UIFonts fonts, string name) : Control
{
	private const int ButtonHeight = 28;

	private readonly UIFonts _fonts = fonts;
	private readonly string _name = name;
	private readonly List<ActionButton> _buttons = [];

	public string Value { get; set; } = "";

	// Adds a button left of those added before.
	public ActionButton AddButton(string text, int width, Action onClick)
	{
		var right = _buttons.Count == 0 ? Width : _buttons[^1].Bounds.Left - 8;
		var button = new ActionButton(_fonts)
		{
			Bounds = new DrawingRectangle(right - width, (Height - ButtonHeight) / 2, width, ButtonHeight),
			Text = text,
			FontSize = 14,
		};
		button.Click += (_, _) => onClick();
		_buttons.Add(button);
		Controls.Add(button);
		return button;
	}

	// Shows the buttons, for the host; the guest only sees the rules.
	public void ShowButtons(bool show)
	{
		foreach (var button in _buttons)
			button.Visible = show;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		var g = e.Graphics;
		Painter.Text(g, _name, _fonts.Regular(13), Theme.Muted, 0, 5);
		Painter.Text(g, Value, _fonts.Bold(18), Theme.Text, 0, 24);
		Painter.Fill(g, new(0, Height - 1, Width, 1), Theme.Alpha(Theme.Line, 0.6f));
	}
}
