using Aigamo.Enzan;

namespace OpenNspw.Disassembler;

internal sealed class Token
{
	private static readonly Dictionary<RegisterCode, string> Register32Names = new()
	{
		{ RegisterCode.Eax, nameof(RegisterCode.Eax) },
		{ RegisterCode.Ecx, nameof(RegisterCode.Ecx) },
		{ RegisterCode.Edx, nameof(RegisterCode.Edx) },
		{ RegisterCode.Ebx, nameof(RegisterCode.Ebx) },
		{ RegisterCode.Esp, nameof(RegisterCode.Esp) },
		{ RegisterCode.Ebp, nameof(RegisterCode.Ebp) },
		{ RegisterCode.Esi, nameof(RegisterCode.Esi) },
		{ RegisterCode.Edi, nameof(RegisterCode.Edi) },
	};
	private static readonly Dictionary<RegisterCode, string> Register16Names = new()
	{
		{ RegisterCode.Ax, nameof(RegisterCode.Ax) },
		{ RegisterCode.Cx, nameof(RegisterCode.Cx) },
		{ RegisterCode.Dx, nameof(RegisterCode.Dx) },
		{ RegisterCode.Bx, nameof(RegisterCode.Bx) },
		{ RegisterCode.Sp, nameof(RegisterCode.Sp) },
		{ RegisterCode.Bp, nameof(RegisterCode.Bp) },
		{ RegisterCode.Si, nameof(RegisterCode.Si) },
		{ RegisterCode.Di, nameof(RegisterCode.Di) },
	};
	private static readonly Dictionary<RegisterCode, string> Register8Names = new()
	{
		{ RegisterCode.Al, nameof(RegisterCode.Al) },
		{ RegisterCode.Cl, nameof(RegisterCode.Cl) },
		{ RegisterCode.Dl, nameof(RegisterCode.Dl) },
		{ RegisterCode.Bl, nameof(RegisterCode.Bl) },
		{ RegisterCode.Ah, nameof(RegisterCode.Ah) },
		{ RegisterCode.Ch, nameof(RegisterCode.Ch) },
		{ RegisterCode.Dh, nameof(RegisterCode.Dh) },
		{ RegisterCode.Bh, nameof(RegisterCode.Bh) },
	};

	public BinaryReader Reader { get; }
	public AddressingMode Mode { get; }
	public bool OperandSizeOverride { get; set; }
	public List<int> Labels { get; }

	private readonly ParseContext _parseContext;

	public Token(BinaryReader reader, AddressingMode mode, bool operandSizeOverride, List<int> labels, ParseContext parseContext)
	{
		Reader = reader;
		Mode = mode;
		OperandSizeOverride = operandSizeOverride;
		Labels = labels;
		_parseContext = parseContext;
	}

	public int Eip => _parseContext.Offset + (int)Reader.BaseStream.Position;

	public string GetR32String(RegisterCode registerCode) => string.Format(_parseContext.RegisterNameFormat, Register32Names[registerCode]);
	public string GetR16String(RegisterCode registerCode) => string.Format(_parseContext.RegisterNameFormat, Register16Names[registerCode]);
	public string GetR8String(RegisterCode registerCode) => string.Format(_parseContext.RegisterNameFormat, Register8Names[registerCode]);

	public string GetR32String(int registerCode) => GetR32String((RegisterCode)registerCode);
	public string GetR16String(int registerCode) => GetR16String((RegisterCode)registerCode);
	public string GetR8String(int registerCode) => GetR8String((RegisterCode)registerCode);

	public string GetR32String() => GetR32String(Mode.ModRM.Reg);
	public string GetR16String() => GetR16String(Mode.ModRM.Reg);
	public string GetR8String() => GetR8String(Mode.ModRM.Reg);

	public string GetM64String(string address) => string.Format(_parseContext.Memory64Format, address);
	public string GetM32String(string address) => string.Format(_parseContext.Memory32Format, address);
	public string GetM16String(string address) => string.Format(_parseContext.Memory16Format, address);
	public string GetM8String(string address) => string.Format(_parseContext.Memory8Format, address);

	public string GetM64String() => GetM64String(GetCalculatedModRMString());
	public string GetM32String() => GetM32String(GetCalculatedModRMString());
	public string GetM16String() => GetM16String(GetCalculatedModRMString());
	public string GetM8String() => GetM8String(GetCalculatedModRMString());

	public string GetRM32String() => Mode.ModRM.Mod == 3
		? GetR32String(Mode.ModRM.RM)
		: GetM32String();

	public string GetRM16String() => Mode.ModRM.Mod == 3
		? GetR16String(Mode.ModRM.RM)
		: GetM16String();

	public string GetRM8String() => Mode.ModRM.Mod == 3
		? GetR8String(Mode.ModRM.RM)
		: GetM8String();

	public string GetCalculatedModRMString() => Mode.ModRM.Mod switch
	{
		0 => Mode.ModRM.RM switch
		{
			4 => GetCalculatedSibString(),
			5 => FormatRegister32(Mode.Displacement.ToNumberLiteralString()),
			_ => GetR32String(Mode.ModRM.RM)
		},
		1 => Mode.ModRM.RM == 4
			? GetCalculatedSibString()
			: $"{GetR32String(Mode.ModRM.RM)} + {FormatRegister32(Mode.Displacement.ToNumberLiteralString())}",
		2 => Mode.ModRM.RM == 4
			? GetCalculatedSibString()
			: $"{GetR32String(Mode.ModRM.RM)} + {FormatRegister32(Mode.Displacement.ToNumberLiteralString())}",
		_ => throw new InvalidOperationException()
	};

	private string GetCalculatedSibString() => Mode.ModRM.Mod switch
	{
		0 => (Mode.Sib.Index == 4) switch
		{
			true => Mode.Sib.Base == 5
				? FormatRegister32(Mode.Displacement.ToNumberLiteralString())
				: GetR32String(Mode.Sib.Index),
			false => Mode.Sib.Base == 5
				? $"{GetR32String(Mode.Sib.Index)} * {FormatRegister32(((int)Math.Pow(2, Mode.Sib.Scale)).ToString())} + {FormatRegister32(Mode.Displacement.ToNumberLiteralString())}"
				: $"{GetR32String(Mode.Sib.Base)} + {GetR32String(Mode.Sib.Index)} * {FormatRegister32(((int)Math.Pow(2, Mode.Sib.Scale)).ToString())}"
		},
		1 => Mode.Sib.Index == 4
			? $"{GetR32String(Mode.Sib.Base)} + {FormatRegister32(Mode.Displacement.ToNumberLiteralString())}"
			: $"{GetR32String(Mode.Sib.Base)} + {GetR32String(Mode.Sib.Index)} * {FormatRegister32(((int)Math.Pow(2, Mode.Sib.Scale)).ToString())} + {FormatRegister32(Mode.Displacement.ToNumberLiteralString())}",
		2 => Mode.Sib.Index == 4
			? $"{GetR32String(Mode.Sib.Base)} + {FormatRegister32(Mode.Displacement.ToNumberLiteralString())}"
			: $"{GetR32String(Mode.Sib.Base)} + {GetR32String(Mode.Sib.Index)} * {FormatRegister32(((int)Math.Pow(2, Mode.Sib.Scale)).ToString())} + {FormatRegister32(Mode.Displacement.ToNumberLiteralString())}",
		_ => throw new InvalidOperationException()
	};

	public string GetDoubleString() => IsConstant(Mode)
		? $"{FormatRegister64($"0x{GetConstant64():X16}")}/*{BitConverter.Int64BitsToDouble(GetConstant64())}*/"
		: $"{GetM64String()}";

	// TODO
	private bool IsConstant(AddressingMode mode) => mode.Displacement < _parseContext.Offset + Reader.BaseStream.Length && mode.IsConstant;

	private long GetConstant64()
	{
		var position = Reader.BaseStream.Position;
		Reader.BaseStream.Position = Mode.Displacement - _parseContext.Offset;
		var value = Reader.ReadInt64();
		Reader.BaseStream.Position = position;
		return value;
	}

	public string GetFpuStackString(int index) => string.Format(_parseContext.FpuStackFormat, index);

	public string ToFpuStackPopLine() => _parseContext.FpuStackPopFormat;

	private string FormatMnemonic(Mnemonic value) => string.Format(_parseContext.MnemonicFormat, value);

	public string ToJumpLine(Mnemonic mnemonic, int address) => $"if ({FormatMnemonic(mnemonic)}) goto loc_{address:X};";

	// mnemonic()
	public string ToStatementLine(Mnemonic mnemonic) => $"{FormatMnemonic(mnemonic)}();";
	// mnemonic(operand)
	public string ToStatementLine(Mnemonic mnemonic, string operand) => $"{FormatMnemonic(mnemonic)}({operand});";
	// mnemonic(operand1, operand2)
	public string ToStatementLine(Mnemonic mnemonic, string operand1, string operand2) => $"{FormatMnemonic(mnemonic)}({operand1}, {operand2});";

	// dest <- mnemonic()
	public string ToAssignmentLine(string dest, Mnemonic mnemonic) => $"{dest} = {FormatMnemonic(mnemonic)}();";
	// dest <- mnemonic(src)
	public string ToAssignmentLine(string dest, Mnemonic mnemonic, string src) => $"{dest} = {FormatMnemonic(mnemonic)}({src});";
	// dest <- mnemonic(src1, src2)
	public string ToAssignmentLine(string dest, Mnemonic mnemonic, string src1, string src2) => $"{dest} = {FormatMnemonic(mnemonic)}({src1}, {src2});";

	// dest <- mnemonic(dest)
	public string ToAssignmentLine(Mnemonic mnemonic, string dest) => $"{dest} = {FormatMnemonic(mnemonic)}({dest});";
	// dest <- mnemonic(dest, src)
	public string ToAssignmentLine(Mnemonic mnemonic, string dest, string src) => $"{dest} = {FormatMnemonic(mnemonic)}({dest}, {src});";

	public string FormatRegister8(string value) => string.Format(_parseContext.Register8Format, value);
	public string FormatRegister16(string value) => string.Format(_parseContext.Register16Format, value);
	public string FormatRegister32(string value) => string.Format(_parseContext.Register32Format, value);
	public string FormatRegister64(string value) => string.Format(_parseContext.Register64Format, value);

	public string ReadSByteAsString() => FormatRegister8(Reader.ReadSByte().ToNumberLiteralString());
	public string ReadByteAsString() => FormatRegister8(Reader.ReadByte().ToNumberLiteralString());
	public string ReadInt16AsString() => FormatRegister16(Reader.ReadUInt16().ToNumberLiteralString());
	public string ReadInt32AsString() => FormatRegister32(Reader.ReadUInt32().ToNumberLiteralString());
}
