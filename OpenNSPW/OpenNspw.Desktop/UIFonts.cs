using Aigamo.Saruhashi;
using FontStashSharp;

namespace OpenNspw.Desktop;

// The menus' fonts: the app's font for them (BundledFonts.Menu), and its bold for headings.
internal sealed class UIFonts
{
	private readonly FontSystem _regular = new();
	private readonly FontSystem _bold = new();
	private readonly Dictionary<(bool Bold, int Size), IFont> _fonts = [];

	public UIFonts()
	{
		_regular.AddFont(BundledFonts.Menu());
		_bold.AddFont(BundledFonts.MenuBold());
	}

	private IFont Get(bool bold, int size)
	{
		if (!_fonts.TryGetValue((bold, size), out var font))
		{
			font = new DynamicSpriteFontWrapper(
				(DynamicSpriteFont)(bold ? _bold : _regular).GetFont(size)
			);
			_fonts[(bold, size)] = font;
		}

		return font;
	}

	public IFont Regular(int size) => Get(false, size);

	public IFont Bold(int size) => Get(true, size);
}
