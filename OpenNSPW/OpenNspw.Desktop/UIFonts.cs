using Aigamo.Saruhashi;
using FontStashSharp;

namespace OpenNspw.Desktop;

// The menus' fonts: the game's Japanese font, and a bold one for headings where the system has one.
internal sealed class UIFonts
{
	private static readonly string[] BoldFontPaths =
	[
		@"C:\Windows\Fonts\meiryob.ttc",
		@"C:\Windows\Fonts\YuGothB.ttc",
		"/usr/share/fonts/opentype/noto/NotoSansCJK-Bold.ttc",
		"/usr/share/fonts/noto-cjk/NotoSansCJK-Bold.ttc",
		"/System/Library/Fonts/ヒラギノ角ゴシック W6.ttc",
	];

	private readonly FontSystem _regular = new();
	private readonly FontSystem _bold = new();
	private readonly Dictionary<(bool Bold, int Size), IFont> _fonts = [];

	public UIFonts(byte[]? regularData)
	{
		if (regularData is not null)
			_regular.AddFont(regularData);

		var boldPath = BoldFontPaths.FirstOrDefault(File.Exists);
		var boldData = boldPath is null ? regularData : TextRasterizer.FirstFont(File.ReadAllBytes(boldPath));
		if (boldData is not null)
			_bold.AddFont(boldData);
	}

	private IFont Get(bool bold, int size)
	{
		if (!_fonts.TryGetValue((bold, size), out var font))
		{
			font = new DynamicSpriteFontWrapper((DynamicSpriteFont)(bold ? _bold : _regular).GetFont(size));
			_fonts[(bold, size)] = font;
		}

		return font;
	}

	public IFont Regular(int size) => Get(false, size);

	public IFont Bold(int size) => Get(true, size);
}
