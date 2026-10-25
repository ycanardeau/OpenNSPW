using Aigamo.Saruhashi;

namespace OpenNspw.Desktop.Controls;

// A field to type text in: dark, outlined in brass while it has the focus, with a blinking caret, and a placeholder
// while it is empty.
internal sealed class TextField(UIFonts fonts) : Control
{
	private readonly UIFonts _fonts = fonts;

	public string Placeholder { get; set; } = "";

	public event EventHandler? Edited;

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (Enabled)
			WindowManager.SetFocus(this);
	}

	protected override void OnKeyPress(KeyPressEventArgs e)
	{
		base.OnKeyPress(e);
		if (!Enabled)
			return;

		if (e.KeyChar == '\b')
			Text = Text.Length > 0 ? Text[..^1] : Text;
		else if (!char.IsControl(e.KeyChar))
			Text += e.KeyChar;
		else
			return;

		Edited?.Invoke(this, EventArgs.Empty);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		var g = e.Graphics;
		var focused = Focused && Enabled;
		Painter.Fill(
			g,
			ClientRectangle,
			Enabled ? Theme.Alpha(Theme.Ink, 0.75f) : Theme.Alpha(Theme.Line, 0.12f)
		);
		Painter.Frame(
			g,
			ClientRectangle,
			focused ? Theme.Brass : Theme.Lerp(Theme.Line, Theme.Navy, Enabled ? 0 : 0.5f)
		);

		var font = _fonts.Regular(17);
		var lineHeight = Painter.Measure(g, "国", font).Height;
		var y = (Height - lineHeight) / 2;
		if (Text.Length == 0)
			Painter.Text(g, Placeholder, font, Theme.Lerp(Theme.Muted, Theme.Navy, 0.35f), 12, y);
		else
			Painter.Text(g, Text, font, Enabled ? Theme.Text : Theme.Muted, 12, y);

		if (focused && Painter.Now % 1 < 0.55f)
			Painter.Fill(
				g,
				new(
					13 + (int)Painter.Measure(g, Text, font).Width,
					(int)y + 2,
					2,
					(int)lineHeight - 4
				),
				Theme.Brass
			);
	}
}
