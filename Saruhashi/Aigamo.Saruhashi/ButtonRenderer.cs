using System.Drawing;

namespace Aigamo.Saruhashi;

public class ButtonRenderer : IButtonRenderer
{
	private ITextRenderer? _textRenderer;

	// OPTIMIZE
	public ITextRenderer TextRenderer
	{
		get => _textRenderer ??= new TextRenderer();
		set => _textRenderer = value;
	}

	public void DrawButton(Graphics graphics, Rectangle bounds, PushButtonState state)
	{
		switch (state)
		{
			case PushButtonState.Normal:
				// OPTIMIZE
				using (var brush = new SolidBrush(Color.FromArgb(85, 85, 85)))
					graphics.FillRectangle(brush, bounds);

				// OPTIMIZE
				using (var brush = new SolidBrush(Color.FromArgb(63, 63, 70)))
					graphics.FillRectangle(brush, Rectangle.Inflate(bounds, -1, -1));
				break;

			case PushButtonState.Hot:
				// OPTIMIZE
				using (var brush = new SolidBrush(Color.FromArgb(0, 151, 251)))
					graphics.FillRectangle(brush, bounds);

				// OPTIMIZE
				using (var brush = new SolidBrush(Color.FromArgb(63, 63, 70)))
					graphics.FillRectangle(brush, Rectangle.Inflate(bounds, -1, -1));
				break;

			case PushButtonState.Pressed:
				// OPTIMIZE
				using (var brush = new SolidBrush(Color.FromArgb(0, 122, 204)))
					graphics.FillRectangle(brush, bounds);
				break;

			case PushButtonState.Disabled:
				// OPTIMIZE
				using (var brush = new SolidBrush(Color.FromArgb(85, 85, 85)))
					graphics.FillRectangle(brush, bounds);

				// OPTIMIZE
				using (var brush = new SolidBrush(Color.FromArgb(45, 45, 45)))
					graphics.FillRectangle(brush, Rectangle.Inflate(bounds, -1, -1));
				break;
		}
	}

	public void DrawButton(Graphics graphics, Rectangle bounds, string? buttonText, IFont? font, bool focused, PushButtonState state)
	{
		DrawButton(graphics, bounds, state);

		var foreColor = state == PushButtonState.Disabled ? Color.FromArgb(101, 101, 101) : Color.FromArgb(241, 241, 241);
		TextRenderer.DrawText(graphics, buttonText, font, bounds, foreColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
	}
}
