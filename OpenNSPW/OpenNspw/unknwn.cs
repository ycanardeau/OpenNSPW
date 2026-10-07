namespace OpenNspw;

// IUnknown: what every COM interface stand-in has, for RELEASE (all_head.h) and SAFE_RELEASE (dxutil.h).
public interface IUnknown
{
	uint Release();
}

// Stand-ins for the DirectMusic interfaces that the game initializes (dmusici.h). It plays no music: the code that
// would is excluded with #if 0.
public interface IDirectMusicLoader8 : IUnknown;

public interface IDirectMusicPerformance8 : IUnknown
{
	int InitAudio(object? ppDirectMusic, object? ppDirectSound, HWND? hWnd, uint dwDefaultPathType, uint dwPChannelCount, uint dwFlags, object? pParams);

	int CloseDown();
}

public sealed class DirectMusicLoader8 : IDirectMusicLoader8
{
	public uint Release()
	{
		return 0;
	}
}

public sealed class DirectMusicPerformance8 : IDirectMusicPerformance8
{
	public int InitAudio(object? ppDirectMusic, object? ppDirectSound, HWND? hWnd, uint dwDefaultPathType, uint dwPChannelCount, uint dwFlags, object? pParams)
	{
		return winerror.S_OK;
	}

	public int CloseDown()
	{
		return winerror.S_OK;
	}

	public uint Release()
	{
		return 0;
	}
}

public static class dmusici
{
	public static readonly Guid CLSID_DirectMusicLoader = new(0xd2ac2892, 0xb39b, 0x11d1, 0x87, 0x04, 0x00, 0x60, 0x08, 0x93, 0xb1, 0xbd);
	public static readonly Guid CLSID_DirectMusicPerformance = new(0xd2ac2881, 0xb39b, 0x11d1, 0x87, 0x04, 0x00, 0x60, 0x08, 0x93, 0xb1, 0xbd);
	public static readonly Guid IID_IDirectMusicLoader8 = new(0x19e7c08c, 0x0a44, 0x4e6a, 0xa1, 0x16, 0x59, 0x5a, 0x7c, 0xd5, 0xde, 0x8c);
	public static readonly Guid IID_IDirectMusicPerformance8 = new(0x679c4137, 0xc62e, 0x4147, 0xb2, 0xb4, 0x9d, 0x56, 0x9a, 0xcb, 0x25, 0x4c);

	public const uint DMUS_APATH_SHARED_STEREOPLUSREVERB = 1;
	public const uint DMUS_AUDIOF_ALL = 0x3F;

	public const uint CLSCTX_INPROC = 0x3;
	public const uint COINIT_MULTITHREADED = 0x0;
}

public partial class Nspw
{
	// RELEASE (all_head.h): `if(x){x->Release();x=NULL;}`.
	public static void RELEASE<T>(ref T? x) where T : class, IUnknown
	{
		if (x is not null)
		{
			x.Release();
			x = null;
		}
	}

	public int DirectDrawCreateEx(object? lpGuid, out IDirectDraw7? lplpDD, Guid iid, object? pUnkOuter)
	{
		lplpDD = new DirectDraw7(gdi, _platform.Present);
		return ddraw.DD_OK;
	}
}
