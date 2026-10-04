using System.Drawing;

namespace Aigamo.Saruhashi;

public interface IButtonRenderer
{
	void DrawButton(Graphics graphics, Rectangle bounds, PushButtonState state);
	void DrawButton(Graphics graphics, Rectangle bounds, string? buttonText, IFont? font, bool focused, PushButtonState state);
}
