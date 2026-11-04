using Xunit;

namespace OpenNspw.Tests;

// The printf family's stand-ins (stdio.cs): the conversions the game uses, written as MSVC's printf writes them, into
// the game's char arrays.
public sealed class PrintfTests
{
	private static string Print(string format, params object?[] args)
	{
		var buffer = new byte[64];
		var length = crt.sprintf(buffer, format, args);
		Assert.Equal(0, buffer[length]);
		return tchar.ShiftJis.GetString(buffer.AsSpan(0, length));
	}

	[Theory]
	[InlineData("%d", 42, "42")]
	[InlineData("%d", -7, "-7")]
	[InlineData("%i", int.MinValue, "-2147483648")]
	[InlineData("%5d", 42, "   42")]
	[InlineData("%-5d|", 42, "42   |")]
	[InlineData("%05d", -42, "-0042")]
	[InlineData("%-05d|", 42, "42   |")]
	[InlineData("%.3d", 7, "007")]
	[InlineData("%8.3d", -7, "    -007")]
	[InlineData("%08.3d", 7, "00000007")]
	[InlineData("%ld", 5, "5")]
	[InlineData("%u", -1, "4294967295")]
	[InlineData("%6u", 12, "    12")]
	[InlineData("%06u", 12, "000012")]
	[InlineData("%x", 255, "ff")]
	[InlineData("%04X", 255, "00FF")]
	[InlineData("%-4x|", 10, "a   |")]
	[InlineData("%c", 65, "A")]
	[InlineData("%3c", 65, "  A")]
	[InlineData("%-3c|", 65, "A  |")]
	[InlineData("%s", "abc", "abc")]
	[InlineData("%5s", "abc", "  abc")]
	[InlineData("%-5s|", "abc", "abc  |")]
	[InlineData("%05s", "abc", "  abc")]
	[InlineData("%.2s", "abc", "ab")]
	[InlineData("%s", null, "(null)")]
	[InlineData("残り%d機", 3, "残り3機")]
	[InlineData("%s様", "日本", "日本様")]
	[InlineData("100%%", 0, "100%")]
	[InlineData("%q", 0, "q")]
	public void FormatsAsMsvcDoes(string format, object? argument, string expected)
	{
		Assert.Equal(expected, Print(format, argument));
	}

	[Fact]
	public void FormatsSeveralArguments()
	{
		Assert.Equal(
			"1:2 x=ff s=ok b=1 e=10",
			Print("%d:%d x=%x s=%s b=%d e=%d", 1, (short)2, (byte)255, "ok", true, GameResult.Draw)
		);
	}

	// A string argument ends at its first null.
	[Fact]
	public void AStringEndsAtItsNull()
	{
		Assert.Equal("ab|", Print("%s|", new byte[] { (byte)'a', (byte)'b', 0, (byte)'c' }));
	}

	// The text is cut to the buffer, with its null.
	[Fact]
	public void TheTextIsCutToTheBuffer()
	{
		var buffer = new byte[5];
		Assert.Equal(4, crt.sprintf(buffer, "%d", 123456));
		Assert.Equal("1234\0"u8.ToArray(), buffer);
	}

	// wsprintf writes 1024 chars at most.
	[Fact]
	public void WsprintfWrites1024CharsAtMost()
	{
		var buffer = new byte[2000];
		Assert.Equal(1024, crt.wsprintf(buffer, "%s", new string('a', 1500)));
		Assert.Equal(0, buffer[1024]);
	}

	// printf writes into its buffer, and makes no garbage but the boxes of its arguments: the game writes its texts every
	// frame.
	[Fact]
	public void PrintfMakesNoGarbage()
	{
		var buffer = new byte[64];
		var name = "大和"u8.ToArray();
		crt.wsprintf(buffer, "%s: %-8s|"u8, name, name);

		var start = GC.GetAllocatedBytesForCurrentThread();
		crt.wsprintf(buffer, "%s: %-8s|"u8, name, name);
		crt.sprintf(buffer, "%s", name);
		var bytes = GC.GetAllocatedBytesForCurrentThread() - start;

		Assert.Equal(0, bytes);
	}

	// A string argument may be the buffer itself, as the text written goes over it.
	[Fact]
	public void TheBufferMayBeAnArgument()
	{
		var buffer = new byte[16];
		"abc\0"u8.CopyTo(buffer);
		Assert.Equal(8, crt.sprintf(buffer, "<%s%s>", buffer, buffer));
		Assert.Equal("<abcabc>", tchar.ShiftJis.GetString(buffer.AsSpan(0, 8)));
	}
}
