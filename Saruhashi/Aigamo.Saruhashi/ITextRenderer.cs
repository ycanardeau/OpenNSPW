using System.Drawing;

namespace Aigamo.Saruhashi;

public interface ITextRenderer
{
	void DrawText(Graphics graphics, string? text, IFont? font, Rectangle bounds, Color foreColor, TextFormatFlags flags);
}
