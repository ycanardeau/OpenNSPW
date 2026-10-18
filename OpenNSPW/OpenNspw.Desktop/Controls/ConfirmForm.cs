using Aigamo.Saruhashi;
using DrawingColor = System.Drawing.Color;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;

namespace OpenNspw.Desktop.Controls;

// A question asked before what cannot be undone: a panel with its title, its message and two buttons, the one that does
// it and Cancel, over the whole screen, dimmed, so that nothing under it takes the mouse. Enter does it.
internal sealed class ConfirmForm : Form
{
	private static readonly DrawingRectangle PanelBounds = new((all_head.SCRN_WIDTH - 480) / 2, (all_head.SCRN_HEIGHT - 200) / 2, 480, 200);

	private readonly UIFonts _fonts;
	private readonly string _title;
	private readonly string _message;

	public event EventHandler? Confirmed;

	public event EventHandler? Canceled;

	private void AddButton(int right, string text, bool isPrimary, Action onClick)
	{
		var button = new ActionButton(_fonts)
		{
			Bounds = new DrawingRectangle(right - 140, PanelBounds.Bottom - 28 - 40, 140, 40),
			Text = text,
			IsPrimary = isPrimary,
			FontSize = 15,
		};
		button.Click += (_, _) => onClick();
		Controls.Add(button);
	}

	public ConfirmForm(UIFonts fonts, string title, string message, string confirm)
	{
		_fonts = fonts;
		_title = title;
		_message = message;
		Size = new DrawingSize(all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT);
		BackColor = DrawingColor.Transparent;
		KeyPreview = true;

		AddButton(PanelBounds.Right - 28, confirm, true, () => Confirmed?.Invoke(this, EventArgs.Empty));
		AddButton(PanelBounds.Right - 28 - 140 - 12, Texts.Cancel, false, () => Canceled?.Invoke(this, EventArgs.Empty));
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (e.KeyCode == Keys.Enter)
			Confirmed?.Invoke(this, EventArgs.Empty);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		var g = e.Graphics;
		Painter.Fill(g, ClientRectangle, Theme.Alpha(Theme.Ink, 0.55f));
		Painter.Fill(g, PanelBounds, Theme.Panel);
		Painter.Frame(g, PanelBounds, Theme.Line);
		Painter.Brackets(g, PanelBounds, 14, Theme.Brass);
		Painter.Text(g, _title, _fonts.Bold(20), Theme.Text, PanelBounds.X + 28, PanelBounds.Y + 26);

		var font = _fonts.Regular(15);
		var y = PanelBounds.Y + 64f;
		foreach (var line in Painter.Wrap(g, _message, font, PanelBounds.Width - 56))
		{
			Painter.Text(g, line, font, Theme.Muted, PanelBounds.X + 28, y);
			y += 22;
		}
	}
}
