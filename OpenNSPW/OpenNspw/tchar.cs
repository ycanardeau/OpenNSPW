using System.Runtime.InteropServices;
using System.Text;

namespace OpenNspw;

// Text, kept as Shift_JIS bytes like in the original (MultiByte character set, compiled with /execution-charset:.932).
public static class tchar
{
	private static Encoding CreateShiftJis()
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		return Encoding.GetEncoding(932);
	}

	public static readonly Encoding ShiftJis = CreateShiftJis();

	// A string literal passed to a function, as in `SetWindowText( hDlg, TEXT("...") )`.
	public static string TEXT(string s)
	{
		return s;
	}

	// A char array initialized with a string literal, as in `TCHAR s[N] = TEXT("...")`.
	public static T TEXT<T>(string s) where T : unmanaged
	{
		T array = default;
		var bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref array, 1));
		var length = ShiftJis.GetBytes(s, bytes);
		if (length >= bytes.Length)
		{
			throw new ArgumentException("The string does not fit with its terminating null.", nameof(s));
		}

		return array;
	}
}
