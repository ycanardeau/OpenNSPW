namespace Aigamo.Saruhashi;

public class Button : ButtonBase
{
	public Button() : base()
	{
		SetStyle(ControlStyles.StandardClick, false);
	}

	private PushButtonState DetermineState(bool up)
	{
		if (!up)
			return PushButtonState.Pressed;

		if (MouseIsOver)
			return PushButtonState.Hot;

		if (!IsEnabled())
			return PushButtonState.Disabled;

		return PushButtonState.Normal;
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			if (Capture && WindowManager.WindowFromPoint(PointToScreen(e.Location)) == this)
			{
				OnClick(EventArgs.Empty);
				OnMouseClick(new MouseEventArgs(e.Button, e.Clicks, PointToClient(e.Location), e.Delta));
			}
		}

		base.OnMouseUp(e);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		ButtonRenderer.DrawButton(e.Graphics, ClientRectangle, GetText(), Font, focused: false, DetermineState(!MouseIsDown));

		base.OnPaint(e);
	}
}
