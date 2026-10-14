namespace OpenNspw.Disassembler;

internal sealed class ParseContext
{
	public int Offset { get; set; }

	// The subroutine being translated: its first address, and the address after its last instruction.
	public int Start { get; set; }
	public int End { get; set; }

	// The register and the value of the last comparison of a 32-bit register with a constant: the largest index of a
	// switch statement's jump table, which the comparison guards.
	public (int Register, uint Value)? LastComparison { get; set; }
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
