using Aigamo.Enzan;

namespace OpenNspw.Disassembler;

internal sealed class Disassembler
{
	private static readonly OneByteInstructions _oneByteInstructions = new();
	private static readonly TwoByteInstructions _twoByteInstructions = new();

	private readonly BinaryReader _reader;

	private bool _operandSizeOverride;
	private int _currentAddress;
	private readonly int _endPosition;
	private readonly ParseContext _parseContext;

	private readonly List<int> _labels = new();
	private readonly Dictionary<int, IEnumerable<string>> _lines = new();

	public Disassembler(BinaryReader reader, int start, int length, ParseContext parseContext)
	{
		_reader = reader;
		var position = start - parseContext.Offset;
		_reader.BaseStream.Position = position;
		_endPosition = position + length;
		_parseContext = parseContext;
	}

	public IReadOnlyList<int> Labels => _labels;

	public IReadOnlyDictionary<int, IEnumerable<string>> Lines => _lines;

	public bool Disassemble()
	{
		Parse();
		return _reader.BaseStream.Position < _endPosition;
	}

	private void Parse()
	{
		_currentAddress = _parseContext.Offset + (int)_reader.BaseStream.Position;
		ParsePrefixes();
		ParseOpcodes();
	}

	private void ParsePrefixes()
	{
		var isPrefix = true;
		do
		{
			var prefix = _reader.ReadByte();
			switch (prefix)
			{
				case 0x66:
					_operandSizeOverride = true;
					break;

				default:
					isPrefix = false;
					break;
			}
		}
		while (isPrefix);

		_reader.BaseStream.Position--;
	}

	private void ParseOpcodes()
	{
		var opcode = _reader.ReadByte();

		Instruction? instruction;
		if (opcode == 0x0f)
		{
			var opcode2 = _reader.ReadByte();
			if (!_twoByteInstructions.TryGetValue(opcode2, out instruction))
				throw new NotImplementedException($"{opcode:X2} {opcode2:X2}");
		}
		else
		{
			if (!_oneByteInstructions.TryGetValue(opcode, out instruction))
				throw new NotImplementedException($"{opcode:X2}");
		}

		var token = new Token(
			_reader,
			mode: instruction.HasModRM ? new AddressingMode(_reader) : default,
			_operandSizeOverride,
			_labels,
			_parseContext
		);
		var factory = instruction.Factories.Count switch
		{
			1 => instruction.Factories[0],
			_ => instruction.Factories[token.Mode.ModRM.Opcode]
		};
		if (factory is null)
			throw new NotImplementedException($"{opcode:X2} /{token.Mode.ModRM.Opcode}");
		_lines.Add(_currentAddress, factory(token).ToArray());

		_operandSizeOverride = token.OperandSizeOverride;
		if (_operandSizeOverride)
			throw new NotImplementedException();
	}
}
