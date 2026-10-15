using Aigamo.Saruhashi;

namespace OpenNspw.Desktop.Controls;

// An item of the main menu: a large caption over a line that says what it does, with a brass bar that grows on hover.
internal sealed class MenuItemButton(UiFonts fonts) : FlatButton
{
	private readonly UiFonts _fonts = fonts;

	public string Description { get; set; } = "";

	protected override void PaintButton(PaintEventArgs e)
	{
		var g = e.Graphics;
		var barHeight = (int)(20 + 32 * Hover);
		Painter.Fill(g, new(0, (Height - barHeight) / 2, 3, barHeight), Theme.Alpha(Theme.Brass, 0.35f + 0.65f * Hover));

		var x = 22 + 10 * Hover;
		var caption = Enabled ? Theme.Lerp(Theme.Text, Theme.BrassLight, Hover) : Theme.Lerp(Theme.Muted, Theme.Navy, 0.4f);
		Painter.Text(g, Text, _fonts.Bold(28), caption, x, 4);
		Painter.Text(g, Description, _fonts.Regular(15), Enabled ? Theme.Muted : Theme.Lerp(Theme.Muted, Theme.Navy, 0.4f), x, 42);
	}
}
