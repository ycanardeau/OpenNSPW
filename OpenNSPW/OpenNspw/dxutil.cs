namespace OpenNspw;

// Stand-ins for the DirectX sample utilities (dxutil.h, dxerr9.h) that the game uses. dxutil.cpp is library code from
// the DirectX SDK samples, so only what the game calls is provided, with the same results.
public static class dxutil
{
	// The Release build's definitions: the HRESULT, without a trace or a message box.
	public static int DXTRACE_ERR(string str, int hr)
	{
		return hr;
	}

	public static int DXTRACE_ERR_MSGBOX(string str, int hr)
	{
		return hr;
	}

	public static void SAFE_RELEASE(ref IDirectPlay8Address? p)
	{
		if (p is not null)
		{
			p.Release();
			p = null;
		}
	}

	public static void SAFE_DELETE_ARRAY<T>(ref T? p) where T : class
	{
		p = null;
	}

	// Converts a Shift_JIS string (the game's TCHAR, with the MultiByte character set) to a wide string.
	public static int DXUtil_ConvertGenericStringToWideCch(out string wstrDestination, ReadOnlySpan<byte> tstrSource, int cchDestChar)
	{
		var length = tstrSource.IndexOf((byte)0);
		var text = tchar.ShiftJis.GetString(length < 0 ? tstrSource : tstrSource[..length]);
		wstrDestination = text.Length < cchDestChar ? text : text[..(cchDestChar - 1)];
		return winerror.S_OK;
	}

	// Converts a wide string to Shift_JIS, truncated to cchDestChar chars including the null.
	public static int DXUtil_ConvertWideStringToGenericCch(Span<byte> tstrDestination, string? wstrSource, int cchDestChar)
	{
		if (wstrSource is null || cchDestChar < 1)
		{
			return winerror.E_FAIL;
		}

		var bytes = tchar.ShiftJis.GetBytes(wstrSource);
		var length = Math.Min(bytes.Length, cchDestChar - 1);
		bytes.AsSpan(0, length).CopyTo(tstrDestination);
		tstrDestination[length] = 0;
		return winerror.S_OK;
	}
}
