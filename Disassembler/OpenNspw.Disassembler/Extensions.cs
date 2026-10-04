namespace OpenNspw.Disassembler;

internal static class Extensions
{
	public static string ToNumberLiteralString(this uint value) => $"0x{value:X8}/*{value}*/";

	public static string ToNumberLiteralString(this ushort value) => $"0x{value:X4}/*{value}*/";

	public static string ToNumberLiteralString(this byte value) => $"0x{value:X2}/*{value}*/";

	public static string ToNumberLiteralString(this sbyte value) => $"0x{value:X2}/*{value}*/";
}
