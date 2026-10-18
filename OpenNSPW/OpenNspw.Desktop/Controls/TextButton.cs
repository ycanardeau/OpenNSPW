using Aigamo.Saruhashi;
using DrawingColor = System.Drawing.Color;

namespace OpenNspw.Desktop.Controls;

// A button that is only its text, red while the mouse is over it.
internal sealed class TextButton : ButtonBase
{
	private ITextRenderer? _textRenderer;

	public TextButton()
	{
		SetStyle(ControlStyles.StandardClick, false);

		BackColor = DrawingColor.Transparent;
		ForeColor = DrawingColor.White;
	}

	public ITextRenderer TextRenderer
	{
		get => _textRenderer ??= new TextRenderer();
		set => _textRenderer = value;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);

		if (Font is not null)
		{
			Size = e.Graphics.MeasureString(Text, Font).ToSize();
		}

		TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor, TextFormatFlags.VerticalCenter);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left && Capture && WindowManager.WindowFromPoint(PointToScreen(e.Location)) == this)
		{
			OnClick(EventArgs.Empty);
			OnMouseClick(new MouseEventArgs(e.Button, e.Clicks, PointToClient(e.Location), e.Delta));
		}

		base.OnMouseUp(e);
	}

	protected override void OnMouseEnter(EventArgs e)
	{
		base.OnMouseEnter(e);

		ForeColor = DrawingColor.Red;
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);

		ForeColor = DrawingColor.White;
	}
}
