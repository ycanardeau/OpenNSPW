using Aigamo.Saruhashi;

namespace OpenNspw.Desktop.Controls;

// A button of a form: a primary one filled with brass, or a secondary one outlined.
internal sealed class ActionButton(UIFonts fonts) : FlatButton
{
	private readonly UIFonts _fonts = fonts;

	public bool IsPrimary { get; set; }

	public int FontSize { get; set; } = 17;

	protected override void PaintButton(PaintEventArgs e)
	{
		var g = e.Graphics;
		var bounds = ClientRectangle;
		System.Drawing.Color text;
		if (!Enabled && IsPrimary)
		{
			Painter.Fill(g, bounds, Theme.Lerp(Theme.Brass, Theme.Panel, 0.55f));
			text = Theme.Lerp(Theme.Ink, Theme.Brass, 0.3f);
		}
		else if (!Enabled)
		{
			Painter.Fill(g, bounds, Theme.Alpha(Theme.Line, 0.25f));
			Painter.Frame(g, bounds, Theme.Line);
			text = Theme.Muted;
		}
		else if (IsPrimary)
		{
			var fill = Theme.Lerp(Theme.Brass, Theme.BrassLight, Hover);
			Painter.Fill(g, bounds, IsPressed ? Theme.Lerp(fill, Theme.Ink, 0.2f) : fill);
			text = Theme.Ink;
		}
		else
		{
			Painter.Fill(g, bounds, Theme.Alpha(Theme.Brass, 0.06f + 0.12f * Hover));
			Painter.Frame(g, bounds, Theme.Lerp(Theme.Line, Theme.Brass, 0.4f + 0.6f * Hover));
			text = Theme.Lerp(Theme.Text, Theme.BrassLight, Hover);
		}

		// Text too wide for the button, such as a long device name, is cut short with an ellipsis.
		var font = _fonts.Bold(FontSize);
		var shown = Painter.Fit(g, Text, font, Width - 16);
		var size = Painter.Measure(g, shown, font);
		Painter.Text(g, shown, font, text, (Width - size.Width) / 2, (Height - size.Height) / 2);
	}
}
