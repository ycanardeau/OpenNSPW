using System.Runtime.InteropServices;

namespace OpenNspw;

// Stand-ins for the DirectDraw 7 interfaces, structs and constants that the game uses (ddraw.h), implemented in
// software. Surfaces are 16-bit RGB565, the display mode the game sets. What the game flips or blits to the primary
// surface is passed to the platform to show.
//
// Structs that the game passes by pointer stay pointers (unsafe), so the call sites are unchanged.

[StructLayout(LayoutKind.Sequential)]
public struct DDCOLORKEY
{
	public uint dwColorSpaceLowValue;
	public uint dwColorSpaceHighValue;
}

[StructLayout(LayoutKind.Sequential)]
public struct DDPIXELFORMAT
{
	public uint dwSize;
	public uint dwFlags;
	public uint dwFourCC;
	public uint dwRGBBitCount;
	public uint dwRBitMask;
	public uint dwGBitMask;
	public uint dwBBitMask;
	public uint dwRGBAlphaBitMask;
}

[StructLayout(LayoutKind.Sequential)]
public struct DDSCAPS2
{
	public uint dwCaps;
	public uint dwCaps2;
	public uint dwCaps3;
	public uint dwCaps4;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct DDSURFACEDESC2
{
	public uint dwSize;
	public uint dwFlags;
	public uint dwHeight;
	public uint dwWidth;
	public int lPitch;
	public uint dwBackBufferCount;
	public uint dwMipMapCount;
	public uint dwAlphaBitDepth;
	public uint dwReserved;
	public void* lpSurface;
	public DDCOLORKEY ddckCKDestOverlay;
	public DDCOLORKEY ddckCKDestBlt;
	public DDCOLORKEY ddckCKSrcOverlay;
	public DDCOLORKEY ddckCKSrcBlt;
	public DDPIXELFORMAT ddpfPixelFormat;
	public DDSCAPS2 ddsCaps;
	public uint dwTextureStage;
}

[StructLayout(LayoutKind.Sequential)]
public struct DDBLTFX
{
	public uint dwSize;
	public uint dwDDFX;
	public uint dwROP;
	public uint dwDDROP;
	public uint dwRotationAngle;
	public uint dwZBufferOpCode;
	public uint dwZBufferLow;
	public uint dwZBufferHigh;
	public uint dwZBufferBaseDest;
	public uint dwZDestConstBitDepth;
	public uint dwZDestConst;
	public uint dwZSrcConstBitDepth;
	public uint dwZSrcConst;
	public uint dwAlphaEdgeBlendBitDepth;
	public uint dwAlphaEdgeBlend;
	public uint dwReserved;
	public uint dwAlphaDestConstBitDepth;
	public uint dwAlphaDestConst;
	public uint dwAlphaSrcConstBitDepth;
	public uint dwAlphaSrcConst;
	public uint dwFillColor;
	public DDCOLORKEY ddckDestColorkey;
	public DDCOLORKEY ddckSrcColorkey;
}

public interface IDirectDrawClipper : IUnknown
{
	int SetHWnd(uint dwFlags, HWND? hWnd);
}

public unsafe interface IDirectDrawSurface7 : IUnknown
{
	int Blt(RECT* lpDestRect, IDirectDrawSurface7? lpDDSrcSurface, RECT* lpSrcRect, uint dwFlags, DDBLTFX* lpDDBltFx);

	int BltFast(int dwX, int dwY, IDirectDrawSurface7 lpDDSrcSurface, RECT* lpSrcRect, uint dwTrans);

	int Flip(IDirectDrawSurface7? lpDDSurfaceTargetOverride, uint dwFlags);

	int GetAttachedSurface(DDSCAPS2* lpDDSCaps, out IDirectDrawSurface7? lplpDDAttachedSurface);

	int GetDC(HDC* lphDC);

	int ReleaseDC(HDC hDC);

	int GetPixelFormat(DDPIXELFORMAT* lpDDPixelFormat);

	int Lock(RECT* lpDestRect, DDSURFACEDESC2* lpDDSurfaceDesc, uint dwFlags, object? hEvent);

	int Unlock(RECT* lpRect);

	int Restore();

	int SetClipper(IDirectDrawClipper? lpDDClipper);

	int SetColorKey(uint dwFlags, DDCOLORKEY* lpDDColorKey);
}

public unsafe interface IDirectDraw7 : IUnknown
{
	int CreateClipper(uint dwFlags, out IDirectDrawClipper? lplpDDClipper, object? pUnkOuter);

	int CreateSurface(DDSURFACEDESC2* lpDDSurfaceDesc2, out IDirectDrawSurface7? lplpDDSurface, object? pUnkOuter);

	int FlipToGDISurface();

	int SetCooperativeLevel(HWND? hWnd, uint dwFlags);

	int SetDisplayMode(int dwWidth, int dwHeight, int dwBPP, int dwRefreshRate, uint dwFlags);
}

public static unsafe class ddraw
{
	public const int DD_OK = 0;
	public const int DDERR_GENERIC = winerror.E_FAIL;
	public const int DDERR_INVALIDRECT = unchecked((int)0x88760096);
	public const int DDERR_INVALIDPARAMS = unchecked((int)0x80070057);
	public const int DDERR_SURFACELOST = unchecked((int)0x887601C2);
	public const int DDERR_NOTFOUND = unchecked((int)0x887600FF);

	public static readonly Guid IID_IDirectDraw7 = new(0x15e65ec0, 0x3b9c, 0x11d2, 0xb9, 0x2f, 0x00, 0x60, 0x97, 0x97, 0xea, 0x5b);

	public const uint DDSCL_FULLSCREEN = 0x00000001;
	public const uint DDSCL_NORMAL = 0x00000008;
	public const uint DDSCL_EXCLUSIVE = 0x00000010;

	public const uint DDSD_CAPS = 0x00000001;
	public const uint DDSD_HEIGHT = 0x00000002;
	public const uint DDSD_WIDTH = 0x00000004;
	public const uint DDSD_BACKBUFFERCOUNT = 0x00000020;

	public const uint DDSCAPS_BACKBUFFER = 0x00000004;
	public const uint DDSCAPS_COMPLEX = 0x00000008;
	public const uint DDSCAPS_FLIP = 0x00000010;
	public const uint DDSCAPS_OFFSCREENPLAIN = 0x00000040;
	public const uint DDSCAPS_PRIMARYSURFACE = 0x00000200;
	public const uint DDSCAPS_SYSTEMMEMORY = 0x00000800;
	public const uint DDSCAPS_VIDEOMEMORY = 0x00004000;

	public const uint DDBLT_COLORFILL = 0x00000400;
	public const uint DDBLT_KEYSRC = 0x00008000;
	public const uint DDBLT_WAIT = 0x01000000;

	public const uint DDBLTFAST_NOCOLORKEY = 0x00000000;
	public const uint DDBLTFAST_SRCCOLORKEY = 0x00000001;
	public const uint DDBLTFAST_DESTCOLORKEY = 0x00000002;
	public const uint DDBLTFAST_WAIT = 0x00000010;

	public const uint DDFLIP_WAIT = 0x00000001;

	public const uint DDCKEY_SRCBLT = 0x00000008;

	public const uint DDLOCK_WAIT = 0x00000001;

	public const uint DDPF_RGB = 0x00000040;

	public static int IDirectDrawSurface_Blt(IDirectDrawSurface7 p, RECT* a, IDirectDrawSurface7? b, RECT* c, uint d, DDBLTFX* e)
	{
		return p.Blt(a, b, c, d, e);
	}

	public static int IDirectDrawSurface_BltFast(IDirectDrawSurface7 p, int a, int b, IDirectDrawSurface7 c, RECT* d, uint e)
	{
		return p.BltFast(a, b, c, d, e);
	}

	public static int IDirectDrawSurface_Flip(IDirectDrawSurface7 p, IDirectDrawSurface7? a, uint b)
	{
		return p.Flip(a, b);
	}

	public static int IDirectDrawSurface_GetDC(IDirectDrawSurface7 p, HDC* a)
	{
		return p.GetDC(a);
	}

	public static int IDirectDrawSurface_ReleaseDC(IDirectDrawSurface7 p, HDC a)
	{
		return p.ReleaseDC(a);
	}

	public static int IDirectDrawSurface_Lock(IDirectDrawSurface7 p, RECT* a, DDSURFACEDESC2* b, uint c, object? d)
	{
		return p.Lock(a, b, c, d);
	}

	public static int IDirectDrawSurface_Unlock(IDirectDrawSurface7 p, RECT* a)
	{
		return p.Unlock(a);
	}

	public static int IDirectDrawSurface_Restore(IDirectDrawSurface7 p)
	{
		return p.Restore();
	}

	public static int IDirectDrawSurface_SetClipper(IDirectDrawSurface7 p, IDirectDrawClipper? a)
	{
		return p.SetClipper(a);
	}
}

public sealed class DirectDrawClipper : IDirectDrawClipper
{
	public HWND? Window { get; private set; }

	public int SetHWnd(uint dwFlags, HWND? hWnd)
	{
		Window = hWnd;
		return ddraw.DD_OK;
	}

	public uint Release()
	{
		return 0;
	}
}

// A surface in system memory, as 16-bit RGB565 pixels. The memory is native, so that Lock can return a pointer to it.
public sealed unsafe class DirectDrawSurface7 : IDirectDrawSurface7
{
	private readonly DirectDraw7 _directDraw;
	private ushort* _pixels;
	private DDCOLORKEY? _sourceColorKey;

	internal DirectDrawSurface7(DirectDraw7 directDraw, int width, int height, bool primary)
	{
		_directDraw = directDraw;
		Width = width;
		Height = height;
		IsPrimary = primary;
		_pixels = (ushort*)NativeMemory.AllocZeroed((nuint)(width * height), sizeof(ushort));
	}

	public int Width { get; }

	public int Height { get; }

	public bool IsPrimary { get; }

	public IDirectDrawSurface7? BackBuffer { get; internal set; }

	public IDirectDrawClipper? Clipper { get; private set; }

	public ReadOnlySpan<ushort> Pixels => new(_pixels, Width * Height);

	internal Span<ushort> Row(int y)
	{
		return new Span<ushort>(_pixels + y * Width, Width);
	}

	private static bool Contains(int width, int height, RECT r)
	{
		return r.left >= 0 && r.top >= 0 && r.right <= width && r.bottom <= height && r.left <= r.right && r.top <= r.bottom;
	}

	private bool IsKey(ushort pixel)
	{
		return _sourceColorKey is { } key && pixel >= key.dwColorSpaceLowValue && pixel <= key.dwColorSpaceHighValue;
	}

	// Copies src's rectangle to this surface's rectangle, stretching if their sizes differ, and skipping src's color
	// key pixels if useKey.
	private void Copy(DirectDrawSurface7 src, RECT s, RECT d, bool useKey)
	{
		var sw = s.right - s.left;
		var sh = s.bottom - s.top;
		var dw = d.right - d.left;
		var dh = d.bottom - d.top;
		if (sw <= 0 || sh <= 0 || dw <= 0 || dh <= 0)
		{
			return;
		}

		// A copy within one surface must not read what it already wrote.
		var source = src == this ? src.Pixels.ToArray() : null;
		for (var y = 0; y < dh; y++)
		{
			var sy = s.top + y * sh / dh;
			var row = Row(d.top + y);
			for (var x = 0; x < dw; x++)
			{
				var sx = s.left + x * sw / dw;
				var pixel = source is null ? src._pixels[sy * src.Width + sx] : source[sy * src.Width + sx];
				if (!useKey || !src.IsKey(pixel))
				{
					row[d.left + x] = pixel;
				}
			}
		}
	}

	private void Changed()
	{
		if (IsPrimary)
		{
			_directDraw.Present(this);
		}
	}

	public int Blt(RECT* lpDestRect, IDirectDrawSurface7? lpDDSrcSurface, RECT* lpSrcRect, uint dwFlags, DDBLTFX* lpDDBltFx)
	{
		var d = lpDestRect is null ? new RECT { right = Width, bottom = Height } : *lpDestRect;
		if (Clipper is null && !Contains(Width, Height, d))
		{
			return ddraw.DDERR_INVALIDRECT;
		}

		d.left = Math.Max(d.left, 0);
		d.top = Math.Max(d.top, 0);
		d.right = Math.Min(d.right, Width);
		d.bottom = Math.Min(d.bottom, Height);
		if ((dwFlags & ddraw.DDBLT_COLORFILL) != 0)
		{
			if (lpDDBltFx is null)
			{
				return ddraw.DDERR_INVALIDPARAMS;
			}

			for (var y = d.top; y < d.bottom; y++)
			{
				Row(y)[d.left..d.right].Fill((ushort)lpDDBltFx->dwFillColor);
			}
		}
		else
		{
			if (lpDDSrcSurface is not DirectDrawSurface7 src)
			{
				return ddraw.DDERR_INVALIDPARAMS;
			}

			var s = lpSrcRect is null ? new RECT { right = src.Width, bottom = src.Height } : *lpSrcRect;
			if (!Contains(src.Width, src.Height, s))
			{
				return ddraw.DDERR_INVALIDRECT;
			}

			Copy(src, s, d, (dwFlags & ddraw.DDBLT_KEYSRC) != 0);
		}

		Changed();
		return ddraw.DD_OK;
	}

	public int BltFast(int dwX, int dwY, IDirectDrawSurface7 lpDDSrcSurface, RECT* lpSrcRect, uint dwTrans)
	{
		if (lpDDSrcSurface is not DirectDrawSurface7 src)
		{
			return ddraw.DDERR_INVALIDPARAMS;
		}

		var s = lpSrcRect is null ? new RECT { right = src.Width, bottom = src.Height } : *lpSrcRect;
		var d = new RECT { left = dwX, top = dwY, right = dwX + (s.right - s.left), bottom = dwY + (s.bottom - s.top) };
		if (!Contains(src.Width, src.Height, s) || !Contains(Width, Height, d))
		{
			return ddraw.DDERR_INVALIDRECT;
		}

		Copy(src, s, d, (dwTrans & ddraw.DDBLTFAST_SRCCOLORKEY) != 0);
		Changed();
		return ddraw.DD_OK;
	}

	public int Flip(IDirectDrawSurface7? lpDDSurfaceTargetOverride, uint dwFlags)
	{
		if (BackBuffer is not DirectDrawSurface7 back)
		{
			return ddraw.DDERR_NOTFOUND;
		}

		var front = _pixels;
		_pixels = back._pixels;
		back._pixels = front;
		Changed();
		return ddraw.DD_OK;
	}

	public int GetAttachedSurface(DDSCAPS2* lpDDSCaps, out IDirectDrawSurface7? lplpDDAttachedSurface)
	{
		lplpDDAttachedSurface = BackBuffer;
		return BackBuffer is null ? ddraw.DDERR_NOTFOUND : ddraw.DD_OK;
	}

	public int GetDC(HDC* lphDC)
	{
		*lphDC = _directDraw.Gdi.CreateSurfaceDC(this);
		return ddraw.DD_OK;
	}

	public int ReleaseDC(HDC hDC)
	{
		_directDraw.Gdi.DeleteDC(hDC);
		Changed();
		return ddraw.DD_OK;
	}

	public int GetPixelFormat(DDPIXELFORMAT* lpDDPixelFormat)
	{
		lpDDPixelFormat->dwFlags = ddraw.DDPF_RGB;
		lpDDPixelFormat->dwRGBBitCount = 16;
		lpDDPixelFormat->dwRBitMask = 0xF800;
		lpDDPixelFormat->dwGBitMask = 0x07E0;
		lpDDPixelFormat->dwBBitMask = 0x001F;
		lpDDPixelFormat->dwRGBAlphaBitMask = 0;
		return ddraw.DD_OK;
	}

	public int Lock(RECT* lpDestRect, DDSURFACEDESC2* lpDDSurfaceDesc, uint dwFlags, object? hEvent)
	{
		lpDDSurfaceDesc->dwWidth = (uint)Width;
		lpDDSurfaceDesc->dwHeight = (uint)Height;
		lpDDSurfaceDesc->lPitch = Width * sizeof(ushort);
		lpDDSurfaceDesc->lpSurface = _pixels;
		GetPixelFormat(&lpDDSurfaceDesc->ddpfPixelFormat);
		return ddraw.DD_OK;
	}

	public int Unlock(RECT* lpRect)
	{
		Changed();
		return ddraw.DD_OK;
	}

	public int Restore()
	{
		return ddraw.DD_OK;
	}

	public int SetClipper(IDirectDrawClipper? lpDDClipper)
	{
		Clipper = lpDDClipper;
		return ddraw.DD_OK;
	}

	public int SetColorKey(uint dwFlags, DDCOLORKEY* lpDDColorKey)
	{
		_sourceColorKey = lpDDColorKey is null ? null : *lpDDColorKey;
		return ddraw.DD_OK;
	}

	public uint Release()
	{
		NativeMemory.Free(_pixels);
		_pixels = (ushort*)NativeMemory.AllocZeroed((nuint)(Width * Height), sizeof(ushort));
		return 0;
	}
}

public sealed unsafe class DirectDraw7(Gdi gdi, Action<DirectDrawSurface7> present) : IDirectDraw7
{
	private readonly Action<DirectDrawSurface7> _present = present;
	private int _width = all_head.SCRN_WIDTH;
	private int _height = all_head.SCRN_HEIGHT;

	public Gdi Gdi { get; } = gdi;

	internal void Present(DirectDrawSurface7 primary)
	{
		_present(primary);
	}

	public int CreateClipper(uint dwFlags, out IDirectDrawClipper? lplpDDClipper, object? pUnkOuter)
	{
		lplpDDClipper = new DirectDrawClipper();
		return ddraw.DD_OK;
	}

	public int CreateSurface(DDSURFACEDESC2* lpDDSurfaceDesc2, out IDirectDrawSurface7? lplpDDSurface, object? pUnkOuter)
	{
		if ((lpDDSurfaceDesc2->ddsCaps.dwCaps & ddraw.DDSCAPS_PRIMARYSURFACE) != 0)
		{
			var primary = new DirectDrawSurface7(this, _width, _height, true);
			if ((lpDDSurfaceDesc2->dwFlags & ddraw.DDSD_BACKBUFFERCOUNT) != 0 && lpDDSurfaceDesc2->dwBackBufferCount > 0)
			{
				primary.BackBuffer = new DirectDrawSurface7(this, _width, _height, false);
			}

			lplpDDSurface = primary;
			return ddraw.DD_OK;
		}

		if ((lpDDSurfaceDesc2->dwFlags & (ddraw.DDSD_WIDTH | ddraw.DDSD_HEIGHT)) != (ddraw.DDSD_WIDTH | ddraw.DDSD_HEIGHT))
		{
			lplpDDSurface = null;
			return ddraw.DDERR_INVALIDPARAMS;
		}

		lplpDDSurface = new DirectDrawSurface7(this, (int)lpDDSurfaceDesc2->dwWidth, (int)lpDDSurfaceDesc2->dwHeight, false);
		return ddraw.DD_OK;
	}

	public int FlipToGDISurface()
	{
		return ddraw.DD_OK;
	}

	public int SetCooperativeLevel(HWND? hWnd, uint dwFlags)
	{
		return ddraw.DD_OK;
	}

	public int SetDisplayMode(int dwWidth, int dwHeight, int dwBPP, int dwRefreshRate, uint dwFlags)
	{
		_width = dwWidth;
		_height = dwHeight;
		return dwBPP == 16 ? ddraw.DD_OK : ddraw.DDERR_INVALIDPARAMS;
	}

	public uint Release()
	{
		return 0;
	}
}
