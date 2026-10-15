using Aigamo.Saruhashi;

namespace OpenNspw.Desktop.Controls;

// A tab: its label, underlined in brass while it is the selected one.
internal sealed class TabButton(UiFonts fonts) : FlatButton
{
	private readonly UiFonts _fonts = fonts;

	public bool IsSelected { get; set; }

	protected override void PaintButton(PaintEventArgs e)
	{
		var g = e.Graphics;
		var color = IsSelected ? Theme.BrassLight : Theme.Lerp(Theme.Muted, Theme.Text, Hover);
		Painter.Text(g, Text, _fonts.Bold(19), color, 0, 2);
		if (IsSelected)
			Painter.Fill(g, new(0, Height - 3, Width, 3), Theme.Brass);
		else if (Hover > 0)
			Painter.Fill(g, new(0, Height - 2, Width, 2), Theme.Alpha(Theme.Brass, 0.5f * Hover));
	}
}
