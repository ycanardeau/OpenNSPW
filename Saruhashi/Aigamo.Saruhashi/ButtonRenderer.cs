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
				graphics.FillRectangle(SolidBrush.Of(Color.FromArgb(85, 85, 85)), bounds);

				graphics.FillRectangle(
					SolidBrush.Of(Color.FromArgb(63, 63, 70)),
					Rectangle.Inflate(bounds, -1, -1)
				);
				break;

			case PushButtonState.Hot:
				graphics.FillRectangle(SolidBrush.Of(Color.FromArgb(0, 151, 251)), bounds);

				graphics.FillRectangle(
					SolidBrush.Of(Color.FromArgb(63, 63, 70)),
					Rectangle.Inflate(bounds, -1, -1)
				);
				break;

			case PushButtonState.Pressed:
				graphics.FillRectangle(SolidBrush.Of(Color.FromArgb(0, 122, 204)), bounds);
				break;

			case PushButtonState.Disabled:
				graphics.FillRectangle(SolidBrush.Of(Color.FromArgb(85, 85, 85)), bounds);

				graphics.FillRectangle(
					SolidBrush.Of(Color.FromArgb(45, 45, 45)),
					Rectangle.Inflate(bounds, -1, -1)
				);
				break;
		}
	}

	public void DrawButton(
		Graphics graphics,
		Rectangle bounds,
		string? buttonText,
		IFont? font,
		bool focused,
		PushButtonState state
	)
	{
		DrawButton(graphics, bounds, state);

		var foreColor =
			state == PushButtonState.Disabled
				? Color.FromArgb(101, 101, 101)
				: Color.FromArgb(241, 241, 241);
		TextRenderer.DrawText(
			graphics,
			buttonText,
			font,
			bounds,
			foreColor,
			TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
		);
	}
}
