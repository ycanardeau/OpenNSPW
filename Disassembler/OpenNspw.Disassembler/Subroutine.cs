namespace OpenNspw.Disassembler;

internal sealed class Subroutine(string name, int start, int length)
{
	public string Name { get; } = name;
	public int Start { get; } = start;
	public int Length { get; } = length;
}
