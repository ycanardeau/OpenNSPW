namespace OpenNspw.Disassembler;

/// <summary>
/// Comments from: Intel® 64 and IA-32 Architectures Software Developer Manual: Vol 2
/// </summary>
internal enum Mnemonic
{
	/// <summary>
	/// Add.
	/// </summary>
	Add,

	/// <summary>
	/// Logical AND.
	/// </summary>
	And,

	/// <summary>
	/// Call procedure.
	/// </summary>
	Call,

	/// <summary>
	/// Convert doubleword to quadword.
	/// </summary>
	Cdq,

	/// <summary>
	/// Compare two operands.
	/// </summary>
	Cmp,

	/// <summary>
	/// Decrement by 1.
	/// </summary>
	Dec,

	/// <summary>
	/// Add.
	/// </summary>
	Fadd,

	/// <summary>
	/// Change sign.
	/// </summary>
	Fchs,

	/// <summary>
	/// Compare floating point values.
	/// </summary>
	Fcom,

	/// <summary>
	/// Cosine.
	/// </summary>
	Fcos,

	/// <summary>
	/// Divide.
	/// </summary>
	Fdiv,
	
	/// <summary>
	/// Reverse divide.
	/// </summary>
	Fdivr,

	/// <summary>
	/// Divide.
	/// </summary>
	Fidiv,

	/// <summary>
	/// Load integer.
	/// </summary>
	Fild,

	/// <summary>
	/// Multiply.
	/// </summary>
	Fimul,

	/// <summary>
	/// Load floating point value.
	/// </summary>
	Fld,

	/// <summary>
	/// Multiply.
	/// </summary>
	Fmul,

	/// <summary>
	/// Store x87 FPU status word.
	/// </summary>
	Fnstsw,

	/// <summary>
	/// Partial arctangent.
	/// </summary>
	Fpatan,

	/// <summary>
	/// Sine.
	/// </summary>
	Fsin,

	/// <summary>
	/// Store floating point value.
	/// </summary>
	Fst,

	/// <summary>
	/// Subtract.
	/// </summary>
	Fsub,

	/// <summary>
	/// Reverse subtract.
	/// </summary>
	Fsubr,

	/// <summary>
	/// Exchange register contents.
	/// </summary>
	Fxch,

	/// <summary>
	/// Signed divide.
	/// </summary>
	Idiv,

	/// <summary>
	/// Signed multiply.
	/// </summary>
	Imul,

	/// <summary>
	/// Increment by 1.
	/// </summary>
	Inc,

	/// <summary>
	/// Jump if above.
	/// </summary>
	Ja,

	/// <summary>
	/// Jump if above or equal.
	/// </summary>
	Jae,

	/// <summary>
	/// Jump if below.
	/// </summary>
	Jb,

	/// <summary>
	/// Jump if equal.
	/// </summary>
	Je,

	/// <summary>
	/// Jump if greater.
	/// </summary>
	Jg,

	/// <summary>
	/// Jump if greater or equal.
	/// </summary>
	Jge,

	/// <summary>
	/// Jump if less.
	/// </summary>
	Jl,

	/// <summary>
	/// Jump if less or equal.
	/// </summary>
	Jle,

	/// <summary>
	/// Jump if not equal.
	/// </summary>
	Jne,

	/// <summary>
	/// Jump if not sign.
	/// </summary>
	Jns,

	/// <summary>
	/// Load effective address.
	/// </summary>
	Lea,

	/// <summary>
	/// Move.
	/// </summary>
	Mov,

	/// <summary>
	/// Move with sign-extension.
	/// </summary>
	Movsx,

	/// <summary>
	/// Two's complement negation.
	/// </summary>
	Neg,

	/// <summary>
	/// Logical inclusive OR.
	/// </summary>
	Or,

	/// <summary>
	/// Pop a value from the stack.
	/// </summary>
	Pop8,

	/// <summary>
	/// Pop a value from the stack.
	/// </summary>
	Pop16,

	/// <summary>
	/// Pop a value from the stack.
	/// </summary>
	Pop32,

	/// <summary>
	/// Push word, doubleword or quadword onto the stack.
	/// </summary>
	Push,

	/// <summary>
	/// Arithmetic right shift.
	/// </summary>
	Sar,

	/// <summary>
	/// Integer subtraction with borrow.
	/// </summary>
	Sbb,

	/// <summary>
	/// Set byte if equal.
	/// </summary>
	Sete,

	/// <summary>
	/// Set byte if greater.
	/// </summary>
	Setg,

	/// <summary>
	/// Left shift.
	/// </summary>
	Shl,

	/// <summary>
	/// Logical right shift.
	/// </summary>
	Shr,

	/// <summary>
	/// Subtract.
	/// </summary>
	Sub,

	/// <summary>
	/// Logical compare.
	/// </summary>
	Test,

	/// <summary>
	/// Logical exclusive OR.
	/// </summary>
	Xor,
}
