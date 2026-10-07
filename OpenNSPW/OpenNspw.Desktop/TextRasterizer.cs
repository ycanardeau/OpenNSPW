using System.Buffers.Binary;
using StbTrueTypeSharp;

namespace OpenNspw.Desktop;

// Rasterizes the game's text on the CPU with a Japanese TrueType font: MS Gothic, the font the original's
// SHIFTJIS_CHARSET fonts resolve to on Windows, or another Japanese font where it does not exist.
public sealed unsafe class TextRasterizer
{
	private static readonly string[] FontPaths =
	[
		@"C:\Windows\Fonts\msgothic.ttc",
		@"C:\Windows\Fonts\meiryo.ttc",
		"/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc",
		"/usr/share/fonts/noto-cjk/NotoSansCJK-Regular.ttc",
		"/usr/share/fonts/opentype/ipafont-gothic/ipag.ttf",
		"/usr/share/fonts/truetype/fonts-japanese-gothic.ttf",
		"/System/Library/Fonts/ヒラギノ角ゴシック W3.ttc",
	];

	private readonly byte[]? _data;
	private readonly StbTrueType.stbtt_fontinfo? _font;

	public TextRasterizer()
	{
		var path = FontPaths.FirstOrDefault(File.Exists);
		if (path is null)
		{
			return;
		}

		_data = File.ReadAllBytes(path);
		_font = new StbTrueType.stbtt_fontinfo();
		fixed (byte* data = _data)
		{
			var offset = StbTrueType.stbtt_GetFontOffsetForIndex(data, 0);
			if (StbTrueType.stbtt_InitFont(_font, data, offset) == 0)
			{
				_font = null;
			}
		}
	}

	// The first font of a TrueType collection (.ttc) as a TrueType font file, for loaders that do not read collections.
	// The collection's table offsets are from the start of the file, so the font's table directory is copied over the
	// collection's header, which the directories come after; the tables stay where they are.
	private static byte[] FirstFont(byte[] data)
	{
		if (data.Length < 16 || BinaryPrimitives.ReadUInt32BigEndian(data) != 0x74746366) // 'ttcf'
		{
			return data;
		}

		var offset = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(12));
		var numTables = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset + 4));
		var length = 12 + 16 * numTables;
		var font = (byte[])data.Clone();
		data.AsSpan(offset, length).CopyTo(font);
		return font;
	}

	// The font file, for the dialogs' font.
	public byte[]? FontData => _data is null ? null : FirstFont(_data);

	public TextBitmap? Rasterize(string text, int height)
	{
		if (_font is null || _data is null || text.Length == 0)
		{
			return null;
		}

		fixed (byte* data = _data)
		{
			_font.data = data;
			var scale = StbTrueType.stbtt_ScaleForPixelHeight(_font, height);
			int ascent, descent, lineGap;
			StbTrueType.stbtt_GetFontVMetrics(_font, &ascent, &descent, &lineGap);
			var baseline = (int)(ascent * scale);

			var width = 0;
			foreach (var c in text)
			{
				int advance, bearing;
				StbTrueType.stbtt_GetCodepointHMetrics(_font, c, &advance, &bearing);
				width += (int)Math.Ceiling(advance * scale);
			}

			var alpha = new byte[Math.Max(width, 1) * height];
			var x = 0;
			foreach (var c in text)
			{
				int advance, bearing, x0, y0, x1, y1;
				StbTrueType.stbtt_GetCodepointHMetrics(_font, c, &advance, &bearing);
				StbTrueType.stbtt_GetCodepointBitmapBox(_font, c, scale, scale, &x0, &y0, &x1, &y1);
				var gw = x1 - x0;
				var gh = y1 - y0;
				if (gw > 0 && gh > 0)
				{
					var glyph = new byte[gw * gh];
					fixed (byte* g = glyph)
					{
						StbTrueType.stbtt_MakeCodepointBitmap(_font, g, gw, gh, gw, scale, scale, c);
					}

					for (var j = 0; j < gh; j++)
					{
						var ty = baseline + y0 + j;
						for (var i = 0; i < gw; i++)
						{
							var tx = x + (int)(bearing * scale) + i;
							if (ty >= 0 && ty < height && tx >= 0 && tx < width)
							{
								alpha[ty * width + tx] = Math.Max(alpha[ty * width + tx], glyph[j * gw + i]);
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
