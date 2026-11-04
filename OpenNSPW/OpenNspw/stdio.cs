using System.Buffers;
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

	// Where Format writes: a buffer, as far as it goes. What goes past its end is dropped, as the text is cut to it.
	private ref struct Output(Span<byte> buffer)
	{
		private readonly Span<byte> _buffer = buffer;

		public int Length { get; private set; }

		public void Add(byte value)
		{
			if (Length < _buffer.Length)
			{
				_buffer[Length++] = value;
			}
		}

		public void Add(scoped ReadOnlySpan<byte> text)
		{
			foreach (var value in text)
			{
				Add(value);
			}
		}

		public void Add(byte value, int count)
		{
			for (var i = 0; i < count; i++)
			{
				Add(value);
			}
		}
	}

	private static void Pad(
		ref Output output,
		scoped ReadOnlySpan<byte> text,
		int width,
		bool left,
		byte fill
	)
	{
		if (left)
		{
			output.Add(text);
		}

		output.Add(left ? (byte)' ' : fill, width - text.Length);

		if (!left)
		{
			output.Add(text);
		}
	}

	// A number's digits, in `scratch`.
	private static ReadOnlySpan<byte> Digits<T>(
		T number,
		Span<byte> scratch,
		ReadOnlySpan<char> format = default
	)
		where T : IUtf8SpanFormattable
	{
		number.TryFormat(scratch, out var length, format, CultureInfo.InvariantCulture);
		return scratch[..length];
	}

	// Formats like the MSVC printf family for the conversions the game uses: d, i, u, x, X, c, s and %, with the flags
	// '-' and '0', a width and a precision. Writes nothing but into `output`, so that the game's texts, which it writes
	// every frame, make no garbage.
	private static void Format(
		ref Output output,
		ReadOnlySpan<byte> format,
		ReadOnlySpan<object?> args
	)
	{
		Span<byte> scratch = stackalloc byte[24];
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
			while (
				i < format.Length
				&& (format[i] is (byte)'-' or (byte)'0' or (byte)'+' or (byte)' ' or (byte)'#')
			)
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
					// The sign, then the digits with zeros before them up to the precision, and up to the width with the
					// sign when the flag '0' fills; padded with spaces.
					var number = (int)ToInteger(args[next++]);
					var digits = Digits(Math.Abs((long)number), scratch);
					var signLength = number < 0 ? 1 : 0;
					var zeros = Math.Max(precision - digits.Length, 0);
					if (fill == '0')
					{
						zeros = Math.Max(zeros, width - signLength - digits.Length);
					}

					var length = signLength + zeros + digits.Length;
					if (!left)
					{
						output.Add((byte)' ', width - length);
					}

					if (number < 0)
					{
						output.Add((byte)'-');
					}

					output.Add((byte)'0', zeros);
					output.Add(digits);
					if (left)
					{
						output.Add((byte)' ', width - length);
					}

					break;
				case 'u':
					Pad(
						ref output,
						Digits((uint)ToInteger(args[next++]), scratch),
						width,
						left,
						fill
					);
					break;
				case 'x' or 'X':
					Pad(
						ref output,
						Digits(
							(uint)ToInteger(args[next++]),
							scratch,
							conversion == 'x' ? "x" : "X"
						),
						width,
						left,
						fill
					);
					break;
				case 'c':
					Pad(ref output, [(byte)ToInteger(args[next++])], width, left, (byte)' ');
					break;
				case 's':
					var text = UntilNull(ToCString(args[next++]));
					if (precision >= 0 && text.Length > precision)
					{
						text = text[..precision];
					}

					Pad(ref output, text, width, left, (byte)' ');
					break;
				default:
					output.Add((byte)conversion);
					break;
			}
		}
	}

	// Writes the text into `buffer`, cut to `limit` chars and to the buffer, with a null after it. It is written first in
	// a buffer of its own, as an argument may be `buffer` itself.
	private static int Print(
		Span<byte> buffer,
		ReadOnlySpan<byte> format,
		ReadOnlySpan<object?> args,
		int limit
	)
	{
		var capacity = Math.Min(limit, buffer.Length - 1);
		var text = ArrayPool<byte>.Shared.Rent(Math.Max(capacity, 0));
		try
		{
			var output = new Output(text.AsSpan(0, capacity));
			Format(ref output, format, args);
			text.AsSpan(0, output.Length).CopyTo(buffer);
			buffer[output.Length] = 0;
			return output.Length;
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(text);
		}
	}

	// A format given as a C# string, converted to Shift_JIS in a buffer of its own.
	private static int Print(
		Span<byte> buffer,
		string format,
		ReadOnlySpan<object?> args,
		int limit
	)
	{
		var bytes = ArrayPool<byte>.Shared.Rent(tchar.ShiftJis.GetMaxByteCount(format.Length));
		try
		{
			var length = tchar.ShiftJis.GetBytes(format, bytes);
			return Print(buffer, bytes.AsSpan(0, length), args, limit);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(bytes);
		}
	}

	public static int sprintf(Span<byte> buffer, string format, params ReadOnlySpan<object?> args)
	{
		return Print(buffer, format, args, int.MaxValue);
	}

	public static int sprintf(
		Span<byte> buffer,
		ReadOnlySpan<byte> format,
		params ReadOnlySpan<object?> args
	)
	{
		return Print(buffer, format, args, int.MaxValue);
	}

	// wsprintf writes at most 1024 chars.
	public static int wsprintf(Span<byte> buffer, string format, params ReadOnlySpan<object?> args)
	{
		return Print(buffer, format, args, 1024);
	}

	public static int wsprintf(
		Span<byte> buffer,
		ReadOnlySpan<byte> format,
		params ReadOnlySpan<object?> args
	)
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
		var text =
			radix == 10
				? value.ToString(CultureInfo.InvariantCulture)
				: Convert.ToString(value, radix);
		var bytes = Encoding.ASCII.GetBytes(text);
		bytes.CopyTo(buffer);
		buffer[bytes.Length] = 0;
	}
}
