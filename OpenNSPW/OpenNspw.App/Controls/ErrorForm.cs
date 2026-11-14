using Aigamo.Saruhashi;
using DrawingColor = System.Drawing.Color;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;

namespace OpenNspw.App.Controls;

// What stopped the game: a panel with its title, the exception in full, as far as it fits, Copy, which puts all of it on
// the system's clipboard (host), and Close, over the whole screen, dimmed, so that nothing under it takes the mouse.
// Enter closes it.
internal sealed class ErrorForm : Form
{
	private static readonly DrawingRectangle PanelBounds = new(
		(all_head.SCRN_WIDTH - 880) / 2,
		(all_head.SCRN_HEIGHT - 600) / 2,
		880,
		600
	);

	private const int LineHeight = 18;

	private readonly UIFonts _fonts;
	private readonly IAppHost _host;
	private readonly string _error;

	// The error's lines as they fit the panel, wrapped once: measuring them each frame would be slow.
	private List<string>? _lines;

	public event EventHandler? Closed;

	private ActionButton AddButton(int right, string text, bool isPrimary)
	{
		var button = new ActionButton(_fonts)
		{
			Bounds = new DrawingRectangle(right - 140, PanelBounds.Bottom - 28 - 40, 140, 40),
			Text = text,
			IsPrimary = isPrimary,
			FontSize = 15,
		};
		Controls.Add(button);
		return button;
	}

	public ErrorForm(UIFonts fonts, IAppHost host, string error)
	{
		_fonts = fonts;
		_host = host;
		_error = error;
		Size = new DrawingSize(all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);
		BackColor = DrawingColor.Transparent;
		KeyPreview = true;

		AddButton(PanelBounds.Right - 28, Texts.Close, true).Click += (_, _) =>
			Closed?.Invoke(this, EventArgs.Empty);
		var copy = AddButton(PanelBounds.Right - 28 - 140 - 12, Texts.Copy, false);
		copy.Click += (_, _) =>
		{
			_host.CopyText(_error);
			copy.Text = Texts.Copied;
		};
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (e.KeyCode == Keys.Enter)
			Closed?.Invoke(this, EventArgs.Empty);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		var g = e.Graphics;
		Painter.Fill(g, ClientRectangle, Theme.Alpha(Theme.Ink, 0.55f));
		Painter.Fill(g, PanelBounds, Theme.Panel);
		Painter.Frame(g, PanelBounds, Theme.Line);
		Painter.Brackets(g, PanelBounds, 14, Theme.Brass);
		Painter.Text(
			g,
			Texts.GameErrorTitle,
			_fonts.Bold(20),
			Theme.Text,
			PanelBounds.X + 28,
			PanelBounds.Y + 26
		);

		var font = _fonts.Regular(13);
		_lines ??=
		[
			.. _error
				.ReplaceLineEndings("\n")
				.Split('\n')
				.SelectMany(line => Painter.Wrap(g, line, font, PanelBounds.Width - 56)),
		];
		var top = PanelBounds.Y + 64;
		var bottom = PanelBounds.Bottom - 28 - 40 - 12;
		var shown = Math.Min(_lines.Count, (bottom - top) / LineHeight);
		for (var i = 0; i < shown; i++)
		{
			var line = i == shown - 1 && shown < _lines.Count ? "…" : _lines[i];
			Painter.Text(g, line, font, Theme.Muted, PanelBounds.X + 28, top + i * LineHeight);
		}
	}
}
