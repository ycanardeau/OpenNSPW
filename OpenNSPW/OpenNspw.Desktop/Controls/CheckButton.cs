using Aigamo.Saruhashi;

namespace OpenNspw.Desktop.Controls;

// A switch: a box, filled in brass while it is on, its label, and what it does under it. A click turns it over.
internal sealed class CheckButton(UIFonts fonts) : FlatButton
{
	private const int Box = 16;

	private readonly UIFonts _fonts = fonts;

	public bool IsChecked { get; set; }

	public string Description { get; set; } = "";

	public event EventHandler? CheckedChanged;

	protected override void OnClick(EventArgs e)
	{
		IsChecked = !IsChecked;
		base.OnClick(e);
		CheckedChanged?.Invoke(this, EventArgs.Empty);
	}

	protected override void PaintButton(PaintEventArgs e)
	{
		var g = e.Graphics;
		var box = new System.Drawing.Rectangle(0, 3, Box, Box);
		Painter.Fill(g, box, Theme.Alpha(Theme.Brass, 0.06f + 0.12f * Hover));
		Painter.Frame(g, box, Theme.Lerp(Theme.Line, Theme.Brass, 0.4f + 0.6f * Hover));
		if (IsChecked)
			Painter.Fill(g, new(4, 7, Box - 8, Box - 8), Theme.Brass);
		Painter.Text(
			g,
			Text,
			_fonts.Bold(15),
			Theme.Lerp(Theme.Text, Theme.BrassLight, Hover),
			Box + 10,
			1
		);
		Painter.Text(g, Description, _fonts.Regular(12), Theme.Muted, Box + 10, 23);
	}
}
