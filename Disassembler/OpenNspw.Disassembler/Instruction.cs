namespace OpenNspw.Disassembler;

internal sealed class Instruction
{
	private readonly List<Func<Token, IEnumerable<string>>?> _factories = new();
	public bool HasModRM { get; }

	public Instruction(Func<Token, IEnumerable<string>> factory, bool hasModRM = false)
	{
		_factories.Add(factory);
		HasModRM = hasModRM;
	}

	public Instruction(params Func<Token, IEnumerable<string>>?[] factories)
	{
		_factories.AddRange(factories);
		HasModRM = true;
	}

	public IReadOnlyList<Func<Token, IEnumerable<string>>?> Factories => _factories;
}
