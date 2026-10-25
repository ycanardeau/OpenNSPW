using StbTrueTypeSharp;

namespace OpenNspw.Desktop;

// Rasterizes the game's text on the CPU with the app's font for it (BundledFonts.Game), where the original's resolves to
// MS Gothic on Windows.
public sealed unsafe class TextRasterizer
{
	private readonly byte[] _data = BundledFonts.Game();
	private readonly StbTrueType.stbtt_fontinfo? _font;

	public TextRasterizer()
	{
		_font = new StbTrueType.stbtt_fontinfo();
		fixed (byte* data = _data)
		{
			if (
				StbTrueType.stbtt_InitFont(
					_font,
					data,
					StbTrueType.stbtt_GetFontOffsetForIndex(data, 0)
				) == 0
			)
			{
				_font = null;
			}
		}
	}

	public TextBitmap? Rasterize(string text, int height)
	{
		if (_font is null || text.Length == 0)
		{
			return null;
		}

		fixed (byte* data = _data)
		{
			_font.data = data;
			var scale = StbTrueType.stbtt_ScaleForPixelHeight(_font, height);
			int ascent,
				descent,
				lineGap;
			StbTrueType.stbtt_GetFontVMetrics(_font, &ascent, &descent, &lineGap);
			var baseline = (int)(ascent * scale);

			var width = 0;
			foreach (var c in text)
			{
				int advance,
					bearing;
				StbTrueType.stbtt_GetCodepointHMetrics(_font, c, &advance, &bearing);
				width += (int)Math.Ceiling(advance * scale);
			}

			var alpha = new byte[Math.Max(width, 1) * height];
			var x = 0;
			foreach (var c in text)
			{
				int advance,
					bearing,
					x0,
					y0,
					x1,
					y1;
				StbTrueType.stbtt_GetCodepointHMetrics(_font, c, &advance, &bearing);
				StbTrueType.stbtt_GetCodepointBitmapBox(_font, c, scale, scale, &x0, &y0, &x1, &y1);
				var gw = x1 - x0;
				var gh = y1 - y0;
				if (gw > 0 && gh > 0)
				{
					var glyph = new byte[gw * gh];
					fixed (byte* g = glyph)
					{
						StbTrueType.stbtt_MakeCodepointBitmap(
							_font,
							g,
							gw,
							gh,
							gw,
							scale,
							scale,
							c
						);
					}

					for (var j = 0; j < gh; j++)
					{
						var ty = baseline + y0 + j;
						for (var i = 0; i < gw; i++)
						{
							var tx = x + (int)(bearing * scale) + i;
							if (ty >= 0 && ty < height && tx >= 0 && tx < width)
							{
								alpha[ty * width + tx] = Math.Max(
									alpha[ty * width + tx],
									glyph[j * gw + i]
								);
							}
						}
					}
				}

				x += (int)Math.Ceiling(advance * scale);
			}

			return new TextBitmap(Math.Max(width, 1), height, alpha);
		}
	}
}
