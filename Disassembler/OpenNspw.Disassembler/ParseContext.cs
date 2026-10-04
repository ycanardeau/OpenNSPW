namespace OpenNspw.Disassembler;

internal sealed class ParseContext
{
	public int Offset { get; set; }
	public string RegisterNameFormat { get; set; } = string.Empty;
	public string MnemonicFormat { get; set; } = string.Empty;
	public string Memory8Format { get; set; } = string.Empty;
	public string Memory16Format { get; set; } = string.Empty;
	public string Memory32Format { get; set; } = string.Empty;
	public string Memory64Format { get; set; } = string.Empty;
	public string FpuStackFormat { get; set; } = string.Empty;
	public string FpuStackPopFormat { get; set; } = string.Empty;
	public string Register8Format { get; set; } = string.Empty;
	public string Register16Format { get; set; } = string.Empty;
	public string Register32Format { get; set; } = string.Empty;
	public string Register64Format { get; set; } = string.Empty;
}
