using System.Globalization;
using System.Text;

namespace OpenNspw;

// Stand-ins for the C runtime and Win32 string functions that the game uses on char arrays (sprintf, wsprintf,
// _tcslen, ...). Strings are Shift_JIS bytes ending with a null, as in the original. A C# string passed as a format or an
// argument is converted to Shift_JIS.
public static partial class crt
{
	private static byte[] ToCString(object? value)
	{
		return value switch
		{
			string s => tchar.ShiftJis.GetBytes(s),
			byte[] bytes => bytes,
			IInlineArray array => array.ToBytes(),
			null => Encoding.ASCII.GetBytes("(null)"),
			_ => throw new ArgumentException($"Not a string: {value.GetType()}.", nameof(value)),
		};
	}

	private static ReadOnlySpan<byte> UntilNull(ReadOnlySpan<byte> s)
	{
		var length = s.IndexOf((byte)0);
		return length < 0 ? s : s[..length];
	}

	private static long ToInteger(object? value)
	{
		return value switch
		{
			int i => i,
			short s => s,
			sbyte b => b,
			byte b => b,
			ushort s => s,
			uint u => u,
			long l => l,
			ulong u => (long)u,
			char c => c,
			bool b => b ? 1 : 0,
			// An enum is passed as its value, as in C.
			Enum e => Convert.ToInt64(e, CultureInfo.InvariantCulture),
			// So are the integers used as booleans.
			Bool32 b => b.Value,
			Bool8 b => b.Value,
			_ => throw new ArgumentException($"Not an integer: {value?.GetType()}.", nameof(value)),
		};
	}

	private static void Pad(List<byte> output, byte[] text, int width, bool left, byte fill)
	{
		if (left)
		{
			output.AddRange(text);
		}

		for (var i = text.Length; i < width; i++)
		{
			output.Add(left ? (byte)' ' : fill);
		}

		if (!left)
		{
			output.AddRange(text);
		}
	}

	// Formats like the MSVC printf family for the conversions the game uses: d, i, u, x, X, c, s and %, with the flags
	// '-' and '0', a width and a precision.
	private static byte[] Format(ReadOnlySpan<byte> format, object?[] args)
	{
		var output = new List<byte>();
		var next = 0;
		var i = 0;
		while (i < format.Length && format[i] != 0)
		{
			if (format[i] != '%')
			{
				output.Add(format[i++]);
				continue;
			}

			i++;
			var left = false;
			var zero = false;
			while (i < format.Length && (format[i] is (byte)'-' or (byte)'0' or (byte)'+' or (byte)' ' or (byte)'#'))
			{
				left |= format[i] == '-';
				zero |= format[i] == '0';
				i++;
			}

			var width = 0;
			while (i < format.Length && format[i] is >= (byte)'0' and <= (byte)'9')
			{
				width = width * 10 + (format[i++] - '0');
			}

			var precision = -1;
			if (i < format.Length && format[i] == '.')
			{
				i++;
				precision = 0;
				while (i < format.Length && format[i] is >= (byte)'0' and <= (byte)'9')
				{
					precision = precision * 10 + (format[i++] - '0');
				}
			}

			while (i < format.Length && (format[i] is (byte)'h' or (byte)'l' or (byte)'L'))
			{
				i++;
			}

			if (i >= format.Length)
			{
				break;
			}

			var conversion = (char)format[i++];
			var fill = zero && !left ? (byte)'0' : (byte)' ';
			switch (conversion)
			{
				case 'd' or 'i':
					var number = (int)ToInteger(args[next++]);
					var digits = Math.Abs((long)number).ToString(CultureInfo.InvariantCulture);
					if (precision >= 0)
					{
						digits = digits.PadLeft(precision, '0');
					}

					var sign = number < 0 ? "-" : "";
					if (fill == '0')
					{
						digits = digits.PadLeft(width - sign.Length, '0');
					}

					Pad(output, Encoding.ASCII.GetBytes(sign + digits), width, left, (byte)' ');
					break;
				case 'u':
					Pad(output, Encoding.ASCII.GetBytes(((uint)ToInteger(args[next++])).ToString(CultureInfo.InvariantCulture)), width, left, fill);
					break;
				case 'x' or 'X':
					var hex = ((uint)ToInteger(args[next++])).ToString(conversion == 'x' ? "x" : "X", CultureInfo.InvariantCulture);
					Pad(output, Encoding.ASCII.GetBytes(hex), width, left, fill);
					break;
				case 'c':
					Pad(output, [(byte)ToInteger(args[next++])], width, left, (byte)' ');
					break;
				case 's':
					var text = UntilNull(ToCString(args[next++]));
					if (precision >= 0 && text.Length > precision)
					{
						text = text[..precision];
					}

					Pad(output, text.ToArray(), width, left, (byte)' ');
					break;
				default:
					output.Add((byte)conversion);
					break;
			}
		}

		return [.. output];
	}

	private static int Print(Span<byte> buffer, ReadOnlySpan<byte> format, object?[] args, int limit)
	{
		var text = Format(format, args);
		var length = Math.Min(text.Length, Math.Min(limit, buffer.Length - 1));
		text.AsSpan(0, length).CopyTo(buffer);
		buffer[length] = 0;
		return length;
	}

	public static int sprintf(Span<byte> buffer, string format, params object?[] args)
	{
		return Print(buffer, tchar.ShiftJis.GetBytes(format), args, int.MaxValue);
	}

	public static int sprintf(Span<byte> buffer, ReadOnlySpan<byte> format, params object?[] args)
	{
		return Print(buffer, format, args, int.MaxValue);
	}

	// wsprintf writes at most 1024 chars.
	public static int wsprintf(Span<byte> buffer, string format, params object?[] args)
	{
		return Print(buffer, tchar.ShiftJis.GetBytes(format), args, 1024);
	}

	public static int wsprintf(Span<byte> buffer, ReadOnlySpan<byte> format, params object?[] args)
	{
		return Print(buffer, format, args, 1024);
	}

	public static int wcslen(string s)
	{
		return s.Length;
	}

	public static int _tcslen(ReadOnlySpan<byte> s)
	{
		return UntilNull(s).Length;
	}

	public static void _tcsncpy(Span<byte> strDest, ReadOnlySpan<byte> strSource, int count)
	{
		var source = UntilNull(strSource);
		for (var i = 0; i < count && i < strDest.Length; i++)
		{
			strDest[i] = i < source.Length ? source[i] : (byte)0;
		}
	}

	// Like atoi: leading white space, an optional sign and digits, and 0 if there are none.
	public static int _ttoi(ReadOnlySpan<byte> str)
	{
		var s = UntilNull(str);
		var i = 0;
		while (i < s.Length && s[i] is (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r')
		{
			i++;
		}

		var negative = i < s.Length && s[i] == '-';
		if (i < s.Length && s[i] is (byte)'-' or (byte)'+')
		{
			i++;
		}

		var value = 0;
		while (i < s.Length && s[i] is >= (byte)'0' and <= (byte)'9')
		{
			value = unchecked(value * 10 + (s[i++] - '0'));
		}

		return negative ? -value : value;
	}

	public static void _itot(int value, Span<byte> buffer, int radix)
	{
		var text = radix == 10 ? value.ToString(CultureInfo.InvariantCulture) : Convert.ToString(value, radix);
		var bytes = Encoding.ASCII.GetBytes(text);
		bytes.CopyTo(buffer);
		buffer[bytes.Length] = 0;
	}
}
