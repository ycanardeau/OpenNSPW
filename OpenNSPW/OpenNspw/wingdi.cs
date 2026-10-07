using System.Runtime.InteropServices;

namespace OpenNspw;

// Stand-ins for the GDI functions that the game uses (wingdi.h): device contexts on DirectDraw surfaces, fonts,
// bitmaps loaded from files, BitBlt and TextOut. Handles are numbers into a table per game, as in Win32. The platform
// rasterizes the text; the stand-ins draw it into the 16-bit surfaces.

// A handle to a device context. Zero is NULL.
[StructLayout(LayoutKind.Sequential)]
public readonly struct HDC(int value) : IEquatable<HDC>
{
	public int Value { get; } = value;

	public static bool operator !(HDC h) => h.Value == 0;
	public static bool operator true(HDC h) => h.Value != 0;
	public static bool operator false(HDC h) => h.Value == 0;
	public static bool operator ==(HDC a, HDC b) => a.Value == b.Value;
	public static bool operator !=(HDC a, HDC b) => a.Value != b.Value;

	public bool Equals(HDC other) => Value == other.Value;

	public override bool Equals(object? obj) => obj is HDC other && Equals(other);

	public override int GetHashCode() => Value;
}

// A handle to a GDI object: a font, a bitmap or a brush (HFONT, HBITMAP, HBRUSH). Zero is NULL.
[StructLayout(LayoutKind.Sequential)]
public readonly struct HGDIOBJ(int value) : IEquatable<HGDIOBJ>
{
	public int Value { get; } = value;

	public static bool operator !(HGDIOBJ h) => h.Value == 0;
	public static bool operator true(HGDIOBJ h) => h.Value != 0;
	public static bool operator false(HGDIOBJ h) => h.Value == 0;
	public static bool operator ==(HGDIOBJ a, HGDIOBJ b) => a.Value == b.Value;
	public static bool operator !=(HGDIOBJ a, HGDIOBJ b) => a.Value != b.Value;

	public bool Equals(HGDIOBJ other) => Value == other.Value;

	public override bool Equals(object? obj) => obj is HGDIOBJ other && Equals(other);

	public override int GetHashCode() => Value;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct BITMAP
{
	public int bmType;
	public int bmWidth;
	public int bmHeight;
	public int bmWidthBytes;
	public ushort bmPlanes;
	public ushort bmBitsPixel;
	public void* bmBits;
}

// Text rasterized by the platform: an alpha mask of Width x Height, the height being the font's cell height.
public sealed record TextBitmap(int Width, int Height, byte[] Alpha);

public static class wingdi
{
	public const int TRANSPARENT = 1;
	public const int OPAQUE = 2;

	public const int SHIFTJIS_CHARSET = 128;
	public const int OUT_DEFAULT_PRECIS = 0;
	public const int CLIP_DEFAULT_PRECIS = 0;
	public const int DEFAULT_QUALITY = 0;
	public const int DEFAULT_PITCH = 0;

	public const uint SRCCOPY = 0x00CC0020;

	public const int WHITE_BRUSH = 0;
	public const int BLACK_BRUSH = 4;

	public const uint IMAGE_BITMAP = 0;
	public const uint LR_DEFAULTSIZE = 0x00000040;
	public const uint LR_LOADFROMFILE = 0x00000010;

	public static uint RGB(int r, int g, int b)
	{
		return (uint)((byte)r | ((byte)g << 8) | ((byte)b << 16));
	}

	// A COLORREF as an RGB565 pixel.
	public static ushort ToPixel(uint color)
	{
		var r = (int)(color & 0xFF);
		var g = (int)((color >> 8) & 0xFF);
		var b = (int)((color >> 16) & 0xFF);
		return (ushort)(((r >> 3) << 11) | ((g >> 2) << 5) | (b >> 3));
	}
}

// The GDI objects of one game.
public sealed class Gdi(INspwPlatform platform)
{
	private sealed class Font(int height, string faceName)
	{
		public int Height { get; } = height;

		public string FaceName { get; } = faceName;
	}

	private sealed class Bitmap(int width, int height, ushort[] pixels)
	{
		public int Width { get; } = width;

		public int Height { get; } = height;

		public ushort[] Pixels { get; } = pixels;
	}

	private sealed class Brush;

	// A device context draws either to a DirectDraw surface or, as a memory DC, to the bitmap selected into it.
	private sealed class DeviceContext(DirectDrawSurface7? surface)
	{
		public DirectDrawSurface7? Surface { get; } = surface;

		public HGDIOBJ Font { get; set; }

		public HGDIOBJ Bitmap { get; set; }

		public uint TextColor { get; set; }

		public uint BkColor { get; set; } = 0xFFFFFF;

		public int BkMode { get; set; } = wingdi.OPAQUE;
	}

	private readonly INspwPlatform _platform = platform;
	private readonly Dictionary<int, object> _objects = [];
	private int _next = 1;

	private int Add(object value)
	{
		var handle = _next++;
		_objects[handle] = value;
		return handle;
	}

	private T? Get<T>(int handle) where T : class
	{
		return _objects.GetValueOrDefault(handle) as T;
	}

	public HDC CreateSurfaceDC(DirectDrawSurface7 surface)
	{
		return new HDC(Add(new DeviceContext(surface)));
	}

	public HDC CreateCompatibleDC(HDC hdc)
	{
		return new HDC(Add(new DeviceContext(null)));
	}

	public int DeleteDC(HDC hdc)
	{
		return _objects.Remove(hdc.Value) ? 1 : 0;
	}

	public HGDIOBJ CreateFont(int height, string faceName)
	{
		return new HGDIOBJ(Add(new Font(height, faceName)));
	}

	public HGDIOBJ CreateBrush()
	{
		return new HGDIOBJ(Add(new Brush()));
	}

	public int DeleteObject(HGDIOBJ obj)
	{
		return _objects.Remove(obj.Value) ? 1 : 0;
	}

	public HGDIOBJ SelectObject(HDC hdc, HGDIOBJ obj)
	{
		if (Get<DeviceContext>(hdc.Value) is not { } dc)
		{
			return default;
		}

		switch (_objects.GetValueOrDefault(obj.Value))
		{
			case Font:
				var oldFont = dc.Font;
				dc.Font = obj;
				return oldFont;
			case Bitmap:
				var oldBitmap = dc.Bitmap;
				dc.Bitmap = obj;
				return oldBitmap;
			default:
				return default;
		}
	}

	public uint SetTextColor(HDC hdc, uint color)
	{
		if (Get<DeviceContext>(hdc.Value) is not { } dc)
		{
			return 0xFFFFFFFF;
		}

		var old = dc.TextColor;
		dc.TextColor = color;
		return old;
	}

	public uint SetBkColor(HDC hdc, uint color)
	{
		if (Get<DeviceContext>(hdc.Value) is not { } dc)
		{
			return 0xFFFFFFFF;
		}

		var old = dc.BkColor;
		dc.BkColor = color;
		return old;
	}

	public int SetBkMode(HDC hdc, int mode)
	{
		if (Get<DeviceContext>(hdc.Value) is not { } dc)
		{
			return 0;
		}

		var old = dc.BkMode;
		dc.BkMode = mode;
		return old;
	}

	// Decodes a BMP file of 8, 16, 24 or 32 bits per pixel into RGB565 pixels, top-down.
	private static Bitmap? DecodeBmp(byte[] file)
	{
		if (file.Length < 54 || file[0] != 'B' || file[1] != 'M')
		{
			return null;
		}

		var dataOffset = BitConverter.ToInt32(file, 10);
		var headerSize = BitConverter.ToInt32(file, 14);
		var width = BitConverter.ToInt32(file, 18);
		var rawHeight = BitConverter.ToInt32(file, 22);
		var bitCount = BitConverter.ToUInt16(file, 28);
		var height = Math.Abs(rawHeight);
		var stride = (width * bitCount + 31) / 32 * 4;
		var palette = 14 + headerSize;
		var pixels = new ushort[width * height];
		for (var y = 0; y < height; y++)
		{
			var row = dataOffset + (rawHeight > 0 ? height - 1 - y : y) * stride;
			for (var x = 0; x < width; x++)
			{
				int r, g, b;
				switch (bitCount)
				{
					case 8:
						var index = file[row + x];
						b = file[palette + index * 4];
						g = file[palette + index * 4 + 1];
						r = file[palette + index * 4 + 2];
						break;
					case 16:
						var p = BitConverter.ToUInt16(file, row + x * 2);
						r = ((p >> 10) & 0x1F) << 3;
						g = ((p >> 5) & 0x1F) << 3;
						b = (p & 0x1F) << 3;
						break;
					default:
						var o = row + x * (bitCount / 8);
						b = file[o];
						g = file[o + 1];
						r = file[o + 2];
						break;
				}

				pixels[y * width + x] = wingdi.ToPixel(wingdi.RGB(r, g, b));
			}
		}

		return new Bitmap(width, height, pixels);
	}

	public HGDIOBJ LoadBitmap(string path)
	{
		using var stream = _platform.OpenFile(path.Replace('\\', '/'), FileMode.Open, FileAccess.Read, FileShare.Read);
		if (stream is null)
		{
			return default;
		}

		using var memory = new MemoryStream();
		stream.CopyTo(memory);
		return DecodeBmp(memory.ToArray()) is { } bitmap ? new HGDIOBJ(Add(bitmap)) : default;
	}

	public unsafe int GetObject(HGDIOBJ obj, int size, BITMAP* bitmap)
	{
		if (Get<Bitmap>(obj.Value) is not { } b)
		{
			return 0;
		}

		bitmap->bmWidth = b.Width;
		bitmap->bmHeight = b.Height;
		bitmap->bmBitsPixel = 16;
		bitmap->bmPlanes = 1;
		bitmap->bmWidthBytes = b.Width * 2;
		return sizeof(BITMAP);
	}

	public int BitBlt(HDC hdc, int x, int y, int cx, int cy, HDC hdcSrc, int x1, int y1, uint rop)
	{
		if (Get<DeviceContext>(hdc.Value)?.Surface is not { } surface
			|| Get<Bitmap>(Get<DeviceContext>(hdcSrc.Value)?.Bitmap.Value ?? 0) is not { } bitmap)
		{
			return 0;
		}

		for (var j = 0; j < cy; j++)
		{
			var dy = y + j;
			var sy = y1 + j;
			if (dy < 0 || dy >= surface.Height || sy < 0 || sy >= bitmap.Height)
			{
				continue;
			}

			var row = surface.Row(dy);
			for (var i = 0; i < cx; i++)
			{
				var dx = x + i;
				var sx = x1 + i;
				if (dx >= 0 && dx < surface.Width && sx >= 0 && sx < bitmap.Width)
				{
					row[dx] = bitmap.Pixels[sy * bitmap.Width + sx];
				}
			}
		}

		return 1;
	}

	public int TextOut(HDC hdc, int x, int y, string text)
	{
		if (Get<DeviceContext>(hdc.Value) is not { Surface: { } surface } dc)
		{
			return 0;
		}

		var font = Get<Font>(dc.Font.Value);
		if (_platform.RasterizeText(text, font?.Height ?? 16, font?.FaceName ?? "") is not { } glyphs)
		{
			return 1;
		}

		var color = wingdi.ToPixel(dc.TextColor);
		var background = wingdi.ToPixel(dc.BkColor);
		for (var j = 0; j < glyphs.Height; j++)
		{
			var dy = y + j;
			if (dy < 0 || dy >= surface.Height)
			{
				continue;
			}

			var row = surface.Row(dy);
			for (var i = 0; i < glyphs.Width; i++)
			{
				var dx = x + i;
				if (dx < 0 || dx >= surface.Width)
				{
					continue;
				}

				// No anti-aliasing, as on the original's 16-bit display.
				if (glyphs.Alpha[j * glyphs.Width + i] >= 128)
				{
					row[dx] = color;
				}
				else if (dc.BkMode == wingdi.OPAQUE)
				{
					row[dx] = background;
				}
			}
		}

		return 1;
	}
}

public unsafe partial class Nspw
{
	private Gdi? _gdi;

	// The GDI objects of this game.
	public Gdi gdi => _gdi ??= new Gdi(_platform);

	public uint SetTextColor(HDC hdc, uint color)
	{
		return gdi.SetTextColor(hdc, color);
	}

	public uint SetBkColor(HDC hdc, uint color)
	{
		return gdi.SetBkColor(hdc, color);
	}

	public int SetBkMode(HDC hdc, int mode)
	{
		return gdi.SetBkMode(hdc, mode);
	}

	public HGDIOBJ SelectObject(HDC hdc, HGDIOBJ h)
	{
		return gdi.SelectObject(hdc, h);
	}

	public int DeleteObject(HGDIOBJ ho)
	{
		return gdi.DeleteObject(ho);
	}

	public HDC CreateCompatibleDC(HDC hdc)
	{
		return gdi.CreateCompatibleDC(hdc);
	}

	public int DeleteDC(HDC hdc)
	{
		return gdi.DeleteDC(hdc);
	}

	public int BitBlt(HDC hdc, int x, int y, int cx, int cy, HDC hdcSrc, int x1, int y1, uint rop)
	{
		return gdi.BitBlt(hdc, x, y, cx, cy, hdcSrc, x1, y1, rop);
	}

	public int GetObject(HGDIOBJ h, int c, BITMAP* pv)
	{
		return gdi.GetObject(h, c, pv);
	}

	public HGDIOBJ GetStockObject(int i)
	{
		return gdi.CreateBrush();
	}

	public HGDIOBJ CreateFont(int cHeight, int cWidth, int cEscapement, int cOrientation, int cWeight, int bItalic, int bUnderline, int bStrikeOut, int iCharSet, int iOutPrecision, int iClipPrecision, int iQuality, int iPitchAndFamily, string? pszFaceName)
	{
		return gdi.CreateFont(cHeight, pszFaceName ?? "");
	}

	public HGDIOBJ LoadImage(object? hInst, string name, uint type, int cx, int cy, uint fuLoad)
	{
		return gdi.LoadBitmap(name);
	}

	// TextOut writes the first c chars (bytes) of the string.
	public int TextOut(HDC hdc, int x, int y, ReadOnlySpan<byte> lpString, int c)
	{
		return gdi.TextOut(hdc, x, y, ShiftJis.GetString(lpString[..Math.Min(Math.Max(c, 0), lpString.Length)]));
	}

	public int TextOut(HDC hdc, int x, int y, string lpString, int c)
	{
		return TextOut(hdc, x, y, ShiftJis.GetBytes(lpString), c);
	}
}
