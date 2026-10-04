namespace OpenNspw.Disassembler;

internal sealed class Subroutine
{
	public string Name { get; }
	public int Start { get; }
	public int Length { get; }

	public Subroutine(string name, int start, int length)
	{
		Name = name;
		Start = start;
		Length = length;
	}
}
