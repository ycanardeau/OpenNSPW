using System.Drawing;
using Aigamo.Saruhashi;
using Graphics = Aigamo.Saruhashi.Graphics;

namespace OpenNspw.App.Controls;

// A field to type text in, Saruhashi's text box: dark, outlined in brass while it has the focus, with a brass caret and
// selection, and a placeholder while it is empty. Where the system has its own text box (host, IAppHost.EditText),
// touching the field opens it, headed with the field's title.
internal sealed class TextField : TextBox
{
	private readonly IAppHost _host;

	public TextField(UIFonts fonts, IAppHost host)
	{
		_host = host;
		Font = fonts.Regular(17);
		CaretColor = Theme.Brass;
		CaretWidth = 2;
		PlaceholderForeColor = Theme.Lerp(Theme.Muted, Theme.Navy, 0.35f);
		SelectionBackColor = Theme.Brass;
		SelectionForeColor = Theme.Ink;
	}

	public string Title { get; set; } = "";

	protected override Rectangle TextRectangle => new(12, 0, Width - 24, Height);

	// As high as the menus' Japanese.
	protected override float MeasureLineHeight(Graphics graphics, IFont font) =>
		Painter.Measure(graphics, "国", font).Height;

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		_host.EditText(
			Title,
			Text,
			text =>
			{
				Text = text;
				OnEdited(EventArgs.Empty);
			}
		);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		var g = e.Graphics;
		Painter.Fill(
			g,
			ClientRectangle,
			Enabled ? Theme.Alpha(Theme.Ink, 0.75f) : Theme.Alpha(Theme.Line, 0.12f)
		);
		Painter.Frame(
			g,
			ClientRectangle,
			Focused && Enabled
				? Theme.Brass
				: Theme.Lerp(Theme.Line, Theme.Navy, Enabled ? 0 : 0.5f)
		);

		ForeColor = Enabled ? Theme.Text : Theme.Muted;
		base.OnPaint(e);
	}
}
