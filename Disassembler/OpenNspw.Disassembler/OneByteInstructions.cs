using Aigamo.Enzan;

namespace OpenNspw.Disassembler;

internal sealed class OneByteInstructions : Dictionary<int, Instruction>
{
	public OneByteInstructions()
	{
		this[0x03] = new Instruction(add_r1632_rm1632, true);
		this[0x05] = new Instruction(add_eAX_imm1632);
		this[0x1b] = new Instruction(sbb_r1632_rm1632, true);
		this[0x24] = new Instruction(and_AL_imm8);
		this[0x25] = new Instruction(and_eAX_imm1632);
		this[0x2b] = new Instruction(sub_r1632_rm1632, true);
		this[0x2d] = new Instruction(sub_eAX_imm1632);
		this[0x33] = new Instruction(xor_r1632_rm1632, true);
		this[0x39] = new Instruction(cmp_rm1632_r1632, true);
		this[0x3b] = new Instruction(cmp_r1632_rm1632, true);
		this[0x3d] = new Instruction(cmp_eAX_imm1632);

		for (var i = 0; i < 8; i++)
			this[0x40 + i] = new Instruction(inc_r1632);

		for (var i = 0; i < 8; i++)
			this[0x48 + i] = new Instruction(dec_r1632);

		for (var i = 0; i < 8; i++)
			this[0x50 + i] = new Instruction(push_r1632);

		for (var i = 0; i < 8; i++)
			this[0x58 + i] = new Instruction(pop_r1632);

		this[0x68] = new Instruction(push_imm1632);
		this[0x69] = new Instruction(imul_r1632_rm1632_imm1632, true);
		this[0x6a] = new Instruction(push_imm8);
		this[0x72] = new Instruction(jb_rel8);
		this[0x74] = new Instruction(je_rel8);
		this[0x75] = new Instruction(jne_rel8);
		this[0x77] = new Instruction(ja_rel8);
		this[0x79] = new Instruction(jns_rel8);
		this[0x7c] = new Instruction(jl_rel8);
		this[0x7d] = new Instruction(jge_rel8);
		this[0x7e] = new Instruction(jle_rel8);
		this[0x7f] = new Instruction(jg_rel8);
		this[0x81] = new Instruction(add_rm1632_imm1632, null, null, null, and_rm1632_imm1632, sub_rm1632_imm1632, null, cmp_rm1632_imm1632);
		this[0x83] = new Instruction(add_rm1632_imm8, or_rm1632_imm8, null, null, and_rm1632_imm8, sub_rm1632_imm8, null, cmp_rm1632_imm8);
		this[0x85] = new Instruction(test_rm1632_r1632, true);
		this[0x89] = new Instruction(mov_rm1632_r1632, true);
		this[0x8a] = new Instruction(mov_r8_rm8, true);
		this[0x8b] = new Instruction(mov_r1632_rm1632, true);
		this[0x8d] = new Instruction(lea_r1632_m, true);
		this[0x99] = new Instruction(cdq_EDX_EAX);
		this[0xa1] = new Instruction(mov_eAX_moffs1632);
		this[0xa3] = new Instruction(mov_moffs1632_eAX);

		for (var i = 0; i < 8; i++)
			this[0xb8 + i] = new Instruction(mov_r1632_imm1632);

		this[0xc1] = new Instruction(null, null, null, null, shl_rm1632_imm8, shr_rm1632_imm8, null, sar_rm1632_imm8);
		this[0xc3] = new Instruction(ret);
		this[0xc7] = new Instruction(mov_rm1632_imm1632, null, null, null, null, null, null, null);
		this[0xd1] = new Instruction(null, null, null, null, shl_rm1632_1, null, null, sar_rm1632_1);
		this[0xd8] = new Instruction(fadd_ST_STi, CodeD8_1, null, CodeD8_3, CodeD8_4, null, null, fdivr_ST_STi);
		this[0xd9] = new Instruction(fld_ST_STi, CodeD9_1, null, null, CodeD9_4, null, CodeD9_6, CodeD9_7);
		this[0xda] = new Instruction(null, CodeDA_1, null, null, null, null, fidiv_ST_m32int, null);
		this[0xdb] = new Instruction(CodeDB_0, null, null, null, null, null, null, null);
		this[0xdc] = new Instruction(CodeDC_0, CodeDC_1, CodeDC_2, CodeDC_3, CodeDC_4, CodeDC_5, null, null);
		this[0xdd] = new Instruction(CodeDD_0, null, CodeDD_2, CodeDD_3, null, null, null, null);
		this[0xde] = new Instruction(CodeDE_0, CodeDE_1, null, CodeDE_3, null, null, null, CodeDE_7);
		this[0xdf] = new Instruction(null, null, null, null, CodeDF_4, null, null, null);
		this[0xe8] = new Instruction(call_rel1632);
		this[0xe9] = new Instruction(jmp_rel1632);
		this[0xeb] = new Instruction(jmp_rel8);
		this[0xf6] = new Instruction(test_rm8_imm8, null, null, null, null, null, null, null);
		this[0xf7] = new Instruction(null, null, null, neg_rm1632, null, imul_eDX_eAX_rm1632, null, idiv_eDX_eAX_rm1632);
		this[0xff] = new Instruction(inc_rm1632, dec_rm1632, call_rm1632, null, jmp_rm1632, null, null, null);

		this[0xf3] = new Instruction(_ => Enumerable.Repeat("// TODO: f3", 1));
		this[0xab] = new Instruction(_ => Enumerable.Repeat("// TODO: ab", 1));
	}

	// 03
	private IEnumerable<string> add_r1632_rm1632(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Add, t.GetR32String(), t.GetRM32String());
	}

	// 05
	private IEnumerable<string> add_eAX_imm1632(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Add, t.GetR32String(RegisterCode.Eax), t.ReadInt32AsString());
	}

	// 1b
	private IEnumerable<string> sbb_r1632_rm1632(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Sbb, t.GetR32String(), t.GetRM32String());
	}

	// 24
	private IEnumerable<string> and_AL_imm8(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.And, t.GetR8String(RegisterCode.Al), t.ReadByteAsString());
	}

	// 25
	private IEnumerable<string> and_eAX_imm1632(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.And, t.GetR32String(RegisterCode.Eax), t.ReadInt32AsString());
	}

	// 2b
	private IEnumerable<string> sub_r1632_rm1632(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Sub, t.GetR32String(), t.GetRM32String());
	}

	// 2d
	private IEnumerable<string> sub_eAX_imm1632(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Sub, t.GetR32String(RegisterCode.Eax), t.ReadInt32AsString());
	}

	// 33
	private IEnumerable<string> xor_r1632_rm1632(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Xor, t.GetR32String(), t.GetRM32String());
	}

	// 39
	private IEnumerable<string> cmp_rm1632_r1632(Token t)
	{
		if (t.OperandSizeOverride)
		{
			t.OperandSizeOverride = false;
			yield return t.ToStatementLine(Mnemonic.Cmp, t.GetRM16String(), t.GetR16String());
		}
		else
			yield return t.ToStatementLine(Mnemonic.Cmp, t.GetRM32String(), t.GetR32String());
	}

	// 3b
	private IEnumerable<string> cmp_r1632_rm1632(Token t)
	{
		if (t.OperandSizeOverride)
		{
			t.OperandSizeOverride = false;
			yield return t.ToStatementLine(Mnemonic.Cmp, t.GetR16String(), t.GetRM16String());
		}
		else
			yield return t.ToStatementLine(Mnemonic.Cmp, t.GetR32String(), t.GetRM32String());
	}

	// 3d
	private IEnumerable<string> cmp_eAX_imm1632(Token t)
	{
		if (t.OperandSizeOverride)
		{
			t.OperandSizeOverride = false;
			yield return t.ToStatementLine(Mnemonic.Cmp, t.GetR16String(RegisterCode.Ax), t.ReadInt16AsString());
		}
		else
			yield return t.ToStatementLine(Mnemonic.Cmp, t.GetR32String(RegisterCode.Eax), t.ReadInt32AsString());
	}

	// 40
	private IEnumerable<string> inc_r1632(Token t)
	{
		t.Reader.BaseStream.Position--;
		var registerCode = t.Reader.ReadByte() - 0x40;
		yield return t.ToAssignmentLine(Mnemonic.Inc, t.GetR32String((RegisterCode)registerCode));
	}

	// 48
	private IEnumerable<string> dec_r1632(Token t)
	{
		t.Reader.BaseStream.Position--;
		var registerCode = t.Reader.ReadByte() - 0x48;
		yield return t.ToAssignmentLine(Mnemonic.Dec, t.GetR32String((RegisterCode)registerCode));
	}

	// 50
	private IEnumerable<string> push_r1632(Token t)
	{
		t.Reader.BaseStream.Position--;
		var registerCode = t.Reader.ReadByte() - 0x50;
		yield return t.ToStatementLine(Mnemonic.Push, t.GetR32String((RegisterCode)registerCode));
	}

	//// 58
	private IEnumerable<string> pop_r1632(Token t)
	{
		t.Reader.BaseStream.Position--;
		var registerCode = t.Reader.ReadByte() - 0x58;
		yield return t.ToAssignmentLine(t.GetR32String((RegisterCode)registerCode), Mnemonic.Pop32);
	}

	// 68
	private IEnumerable<string> push_imm1632(Token t)
	{
		yield return t.ToStatementLine(Mnemonic.Push, t.ReadInt32AsString());
	}

	// 69
	private IEnumerable<string> imul_r1632_rm1632_imm1632(Token t)
	{
		yield return t.ToAssignmentLine(t.GetR32String(), Mnemonic.Imul, t.GetRM32String(), t.ReadInt32AsString());
	}

	// 6a
	private IEnumerable<string> push_imm8(Token t)
	{
		yield return t.ToStatementLine(Mnemonic.Push, t.ReadByteAsString());
	}

	// 72
	private IEnumerable<string> jb_rel8(Token t)
	{
		var rel8 = t.Reader.ReadSByte();
		var address = t.Eip + rel8;
		t.Labels.Add(address);
		yield return t.ToJumpLine(Mnemonic.Jb, address);
	}

	// 74
	private IEnumerable<string> je_rel8(Token t)
	{
		var rel8 = t.Reader.ReadSByte();
		var address = t.Eip + rel8;
		t.Labels.Add(address);
		yield return t.ToJumpLine(Mnemonic.Je, address);
	}

	// 75
	private IEnumerable<string> jne_rel8(Token t)
	{
		var rel8 = t.Reader.ReadSByte();
		var address = t.Eip + rel8;
		t.Labels.Add(address);
		yield return t.ToJumpLine(Mnemonic.Jne, address);
	}

	// 77
	private IEnumerable<string> ja_rel8(Token t)
	{
		var rel8 = t.Reader.ReadSByte();
		var address = t.Eip + rel8;
		t.Labels.Add(address);
		yield return t.ToJumpLine(Mnemonic.Ja, address);
	}

	// 79
	private IEnumerable<string> jns_rel8(Token t)
	{
		var rel8 = t.Reader.ReadSByte();
		var address = t.Eip + rel8;
		t.Labels.Add(address);
		yield return t.ToJumpLine(Mnemonic.Jns, address);
	}

	// 7c
	private IEnumerable<string> jl_rel8(Token t)
	{
		var rel8 = t.Reader.ReadSByte();
		var address = t.Eip + rel8;
		t.Labels.Add(address);
		yield return t.ToJumpLine(Mnemonic.Jl, address);
	}

	// 7d
	private IEnumerable<string> jge_rel8(Token t)
	{
		var rel8 = t.Reader.ReadSByte();
		var address = t.Eip + rel8;
		t.Labels.Add(address);
		yield return t.ToJumpLine(Mnemonic.Jge, address);
	}

	// 7e
	private IEnumerable<string> jle_rel8(Token t)
	{
		var rel8 = t.Reader.ReadSByte();
		var address = t.Eip + rel8;
		t.Labels.Add(address);
		yield return t.ToJumpLine(Mnemonic.Jle, address);
	}

	// 7f
	private IEnumerable<string> jg_rel8(Token t)
	{
		var rel8 = t.Reader.ReadSByte();
		var address = t.Eip + rel8;
		t.Labels.Add(address);
		yield return t.ToJumpLine(Mnemonic.Jg, address);
	}

	// 81 /0
	private IEnumerable<string> add_rm1632_imm1632(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Add, t.GetRM32String(), t.ReadInt32AsString());
	}

	// 81 /4
	private IEnumerable<string> and_rm1632_imm1632(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.And, t.GetRM32String(), t.ReadInt32AsString());
	}

	// 81 /5
	private IEnumerable<string> sub_rm1632_imm1632(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Sub, t.GetRM32String(), t.ReadInt32AsString());
	}

	// 81 /7
	private IEnumerable<string> cmp_rm1632_imm1632(Token t)
	{
		if (t.OperandSizeOverride)
		{
			t.OperandSizeOverride = false;
			yield return t.ToStatementLine(Mnemonic.Cmp, t.GetRM16String(), t.ReadInt16AsString());
		}
		else
			yield return t.ToStatementLine(Mnemonic.Cmp, t.GetRM32String(), t.ReadInt32AsString());
	}

	// 83 /0
	private IEnumerable<string> add_rm1632_imm8(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Add, t.GetRM32String(), t.ReadSByteAsString());
	}

	// 83 /1
	private IEnumerable<string> or_rm1632_imm8(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Or, t.GetRM32String(), t.ReadSByteAsString());
	}

	// 83 /4
	private IEnumerable<string> and_rm1632_imm8(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.And, t.GetRM32String(), t.ReadSByteAsString());
	}

	// 83 /5
	private IEnumerable<string> sub_rm1632_imm8(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Sub, t.GetRM32String(), t.ReadSByteAsString());
	}

	// 83 /7
	private IEnumerable<string> cmp_rm1632_imm8(Token t)
	{
		if (t.OperandSizeOverride)
		{
			t.OperandSizeOverride = false;
			yield return t.ToStatementLine(Mnemonic.Cmp, t.GetRM16String(), t.ReadSByteAsString());
		}
		else
			yield return t.ToStatementLine(Mnemonic.Cmp, t.GetRM32String(), t.ReadSByteAsString());
	}

	// 85
	private IEnumerable<string> test_rm1632_r1632(Token t)
	{
		if (t.OperandSizeOverride)
		{
			t.OperandSizeOverride = false;
			yield return t.ToStatementLine(Mnemonic.Test, t.GetRM16String(), t.GetR16String());
		}
		else
			yield return t.ToStatementLine(Mnemonic.Test, t.GetRM32String(), t.GetR32String());
	}

	// 89
	private IEnumerable<string> mov_rm1632_r1632(Token t)
	{
		if (t.OperandSizeOverride)
		{
			t.OperandSizeOverride = false;
			yield return t.ToAssignmentLine(t.GetRM16String(), Mnemonic.Mov, t.GetR16String());
		}
		else
			yield return t.ToAssignmentLine(t.GetRM32String(), Mnemonic.Mov, t.GetR32String());
	}

	// 8a
	private IEnumerable<string> mov_r8_rm8(Token t)
	{
		yield return t.ToAssignmentLine(t.GetR8String(), Mnemonic.Mov, t.GetRM8String());
	}

	// 8b
	private IEnumerable<string> mov_r1632_rm1632(Token t)
	{
		if (t.OperandSizeOverride)
		{
			t.OperandSizeOverride = false;
			yield return t.ToAssignmentLine(t.GetR16String(), Mnemonic.Mov, t.GetRM16String());
		}
		else
			yield return t.ToAssignmentLine(t.GetR32String(), Mnemonic.Mov, t.GetRM32String());
	}

	// 8d
	private IEnumerable<string> lea_r1632_m(Token t)
	{
		yield return t.ToAssignmentLine(t.GetR32String(), Mnemonic.Lea, t.GetCalculatedModRMString());
	}

	// 99
	private IEnumerable<string> cdq_EDX_EAX(Token t)
	{
		yield return t.ToStatementLine(Mnemonic.Cdq);
	}

	// a1
	private IEnumerable<string> mov_eAX_moffs1632(Token t)
	{
		if (t.OperandSizeOverride)
		{
			t.OperandSizeOverride = false;
			yield return t.ToAssignmentLine(t.GetR16String(RegisterCode.Ax), Mnemonic.Mov, t.GetM16String(t.ReadInt32AsString()));
		}
		else
			yield return t.ToAssignmentLine(t.GetR32String(RegisterCode.Eax), Mnemonic.Mov, t.GetM32String(t.ReadInt32AsString()));
	}

	// a3
	private IEnumerable<string> mov_moffs1632_eAX(Token t)
	{
		if (t.OperandSizeOverride)
		{
			t.OperandSizeOverride = false;
			yield return t.ToAssignmentLine(t.GetM16String(t.ReadInt32AsString()), Mnemonic.Mov, t.GetR16String(RegisterCode.Ax));
		}
		else
			yield return t.ToAssignmentLine(t.GetM32String(t.ReadInt32AsString()), Mnemonic.Mov, t.GetR32String(RegisterCode.Eax));
	}

	// b8
	private IEnumerable<string> mov_r1632_imm1632(Token t)
	{
		t.Reader.BaseStream.Position--;
		var registerCode = t.Reader.ReadByte() - 0xb8;
		yield return t.ToAssignmentLine(t.GetR32String((RegisterCode)registerCode), Mnemonic.Mov, t.ReadInt32AsString());
	}

	// c1 /4
	private IEnumerable<string> shl_rm1632_imm8(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Shl, t.GetRM32String(), t.ReadByteAsString());
	}

	// c1 /5
	private IEnumerable<string> shr_rm1632_imm8(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Shr, t.GetRM32String(), t.ReadByteAsString());
	}

	// c1 /7
	private IEnumerable<string> sar_rm1632_imm8(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Sar, t.GetRM32String(), t.ReadByteAsString());
	}

	// c3
	private IEnumerable<string> ret(Token t)
	{
		yield return "return;";
	}

	// c7 /0
	private IEnumerable<string> mov_rm1632_imm1632(Token t)
	{
		if (t.OperandSizeOverride)
		{
			t.OperandSizeOverride = false;
			yield return t.ToAssignmentLine(t.GetRM16String(), Mnemonic.Mov, t.ReadInt16AsString());
		}
		else
			yield return t.ToAssignmentLine(t.GetRM32String(), Mnemonic.Mov, t.ReadInt32AsString());
	}

	// c9
	private IEnumerable<string> leave_ebp(Token t) => throw new NotImplementedException();

	// d1 /4
	private IEnumerable<string> shl_rm1632_1(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Shl, t.GetRM32String(), t.FormatRegister8(1.ToString()));
	}

	// d1 /7
	private IEnumerable<string> sar_rm1632_1(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Sar, t.GetRM32String(), t.FormatRegister8(1.ToString()));
	}

	// d8 /0
	private IEnumerable<string> fadd_ST_STi(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Fadd, t.GetFpuStackString(0), t.GetFpuStackString(t.Mode.ModRM.RM));
	}

	// d8 /1
	private IEnumerable<string> CodeD8_1(Token t) => t.Mode.ModRM.Mod == 3
		? fmul_ST_STi(t)
		: fmul_ST_m32real(t);

	// d8 /1
	private IEnumerable<string> fmul_ST_STi(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Fmul, t.GetFpuStackString(0), t.GetFpuStackString(t.Mode.ModRM.RM));
	}

	// d8 /1
	private IEnumerable<string> fmul_ST_m32real(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Fmul, t.GetFpuStackString(0), t.GetM32String());
	}

	// d8 /3
	private IEnumerable<string> CodeD8_3(Token t)
	{
		t.Reader.BaseStream.Position--;
		var opcode = t.Reader.ReadByte();
		return opcode switch
		{
			0xd9 => fcomp_ST_ST1(t),
			_ => throw new NotImplementedException($"d8 /3 {opcode:X2}")
		};
	}

	// d8 /3 d9
	private IEnumerable<string> fcomp_ST_ST1(Token t)
	{
		yield return t.ToStatementLine(Mnemonic.Fcom, t.GetFpuStackString(1));
		yield return t.ToFpuStackPopLine();
	}

	// d8 /4
	private IEnumerable<string> CodeD8_4(Token t) => t.Mode.ModRM.Mod == 3
		? fsub_ST_STi(t)
		: throw new NotImplementedException();

	// d8 /4
	private IEnumerable<string> fsub_ST_STi(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Fsub, t.GetFpuStackString(0), t.GetFpuStackString(t.Mode.ModRM.RM));
	}

	// d8 /7
	private IEnumerable<string> fdivr_ST_STi(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Fdivr, t.GetFpuStackString(0), t.GetFpuStackString(t.Mode.ModRM.RM));
	}

	// d9 /0
	private IEnumerable<string> fld_ST_STi(Token t)
	{
		yield return t.ToStatementLine(Mnemonic.Fld, t.GetFpuStackString(t.Mode.ModRM.RM));
	}

	// d9 /1
	private IEnumerable<string> CodeD9_1(Token t)
	{
		t.Reader.BaseStream.Position--;
		var opcode = t.Reader.ReadByte();
		return opcode switch
		{
			0xc9 => fxch_ST_ST1(t),
			_ => throw new NotImplementedException($"d9 /1 {opcode:X2}")
		};
	}

	// d9 /1 c9
	private IEnumerable<string> fxch_ST_ST1(Token t)
	{
		yield return t.ToAssignmentLine($"({t.GetFpuStackString(0)}, {t.GetFpuStackString(1)})", Mnemonic.Fxch, t.GetFpuStackString(0), t.GetFpuStackString(1));
	}

	// d9 /4
	private IEnumerable<string> CodeD9_4(Token t)
	{
		t.Reader.BaseStream.Position--;
		var opcode = t.Reader.ReadByte();
		return opcode switch
		{
			0xe0 => fchs_ST(t),
			_ => throw new NotImplementedException($"d9 /4 {opcode:X2}")
		};
	}

	// d9 /4 e0
	private IEnumerable<string> fchs_ST(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Fchs, t.GetFpuStackString(0));
	}

	// d9 /6
	private IEnumerable<string> CodeD9_6(Token t)
	{
		t.Reader.BaseStream.Position--;
		var opcode = t.Reader.ReadByte();
		return opcode switch
		{
			0xf3 => fpatan_ST1_ST(t),
			_ => throw new NotImplementedException($"d9 /6 {opcode:X2}")
		};
	}

	// d9 /6 f3
	private IEnumerable<string> fpatan_ST1_ST(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Fpatan, t.GetFpuStackString(1), t.GetFpuStackString(0));
		yield return t.ToFpuStackPopLine();
	}

	// d9 /7
	private IEnumerable<string> CodeD9_7(Token t)
	{
		t.Reader.BaseStream.Position--;
		var opcode = t.Reader.ReadByte();
		return opcode switch
		{
			0xfe => fsin_ST(t),
			0xff => fcos_ST(t),
			_ => throw new NotImplementedException($"d9 /7 {opcode:X2}")
		};
	}

	// d9 /7 fe
	private IEnumerable<string> fsin_ST(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Fsin, t.GetFpuStackString(0));
	}

	// d9 /7 ff
	private IEnumerable<string> fcos_ST(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Fcos, t.GetFpuStackString(0));
	}

	// da /1
	private IEnumerable<string> CodeDA_1(Token t) => t.Mode.ModRM.Mod == 3
		? throw new NotImplementedException()
		: fimul_ST_m32int(t);

	// da /1
	private IEnumerable<string> fimul_ST_m32int(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Fimul, t.GetFpuStackString(0), t.GetM32String());
	}

	// da /6
	private IEnumerable<string> fidiv_ST_m32int(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Fidiv, t.GetFpuStackString(0), t.GetM32String());
	}

	// db /0
	private IEnumerable<string> CodeDB_0(Token t) => t.Mode.ModRM.Mod == 3
		? throw new NotImplementedException()
		: fild_ST_m32int(t);

	// db /0
	private IEnumerable<string> fild_ST_m32int(Token t)
	{
		yield return t.ToStatementLine(Mnemonic.Fild, t.GetM32String());
	}

	// dc /0
	private IEnumerable<string> CodeDC_0(Token t) => t.Mode.ModRM.Mod == 3
		? throw new NotImplementedException()
		: fadd_ST_m64real(t);

	// dc /0
	private IEnumerable<string> fadd_ST_m64real(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Fadd, t.GetFpuStackString(0), t.GetDoubleString());
	}

	// dc /1
	private IEnumerable<string> CodeDC_1(Token t) => t.Mode.ModRM.Mod == 3
		? throw new NotImplementedException()
		: fmul_ST_m64real(t);

	// dc /1
	private IEnumerable<string> fmul_ST_m64real(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Fmul, t.GetFpuStackString(0), t.GetDoubleString());
	}

	// dc /2
	private IEnumerable<string> CodeDC_2(Token t) => t.Mode.ModRM.Mod == 3
		? throw new NotImplementedException()
		: fcom_ST_m64real(t);

	// dc /2
	private IEnumerable<string> fcom_ST_m64real(Token t)
	{
		yield return t.ToStatementLine(Mnemonic.Fcom, t.GetDoubleString());
	}

	// dc /3
	private IEnumerable<string> CodeDC_3(Token t) => t.Mode.ModRM.Mod == 3
		? throw new NotImplementedException()
		: fcomp_ST_m64real(t);

	// dc /3
	private IEnumerable<string> fcomp_ST_m64real(Token t)
	{
		yield return t.ToStatementLine(Mnemonic.Fcom, t.GetDoubleString());
		yield return t.ToFpuStackPopLine();
	}

	// dc /4
	private IEnumerable<string> CodeDC_4(Token t) => t.Mode.ModRM.Mod == 3
		? throw new NotImplementedException()
		: fsub_ST_m64real(t);

	// dc /4
	private IEnumerable<string> fsub_ST_m64real(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Fsub, t.GetFpuStackString(0), t.GetDoubleString());
	}

	// dc /5
	private IEnumerable<string> CodeDC_5(Token t) => t.Mode.ModRM.Mod == 3
		? throw new NotImplementedException()
		: fsubr_ST_m64real(t);

	// dc /5
	private IEnumerable<string> fsubr_ST_m64real(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Fsubr, t.GetFpuStackString(0), t.GetDoubleString());
	}

	// dd /0
	private IEnumerable<string> CodeDD_0(Token t) => t.Mode.ModRM.Mod == 3
		? throw new NotImplementedException()
		: fld_ST_m64real(t);

	// dd /0
	private IEnumerable<string> fld_ST_m64real(Token t)
	{
		yield return t.ToStatementLine(Mnemonic.Fld, t.GetDoubleString());
	}

	// dd /2
	private IEnumerable<string> CodeDD_2(Token t) => t.Mode.ModRM.Mod == 3
		? fst_ST_STi(t)
		: fst_m64real_ST(t);

	// dd /2
	private IEnumerable<string> fst_ST_STi(Token t)
	{
		yield return t.ToAssignmentLine(t.GetFpuStackString(0), Mnemonic.Fst, t.GetFpuStackString(t.Mode.ModRM.RM));
	}

	// dd /2
	private IEnumerable<string> fst_m64real_ST(Token t)
	{
		yield return t.ToAssignmentLine(t.GetM64String(), Mnemonic.Fst, t.GetFpuStackString(0));    // TODO
	}

	// dd /3
	private IEnumerable<string> CodeDD_3(Token t) => t.Mode.ModRM.Mod == 3
		? fstp_ST_STi(t)
		: fstp_m64real_ST(t);

	// dd /3
	private IEnumerable<string> fstp_ST_STi(Token t)
	{
		yield return t.ToAssignmentLine(t.GetFpuStackString(0), Mnemonic.Fst, t.GetFpuStackString(t.Mode.ModRM.RM));
		yield return t.ToFpuStackPopLine();
	}

	// dd /3
	private IEnumerable<string> fstp_m64real_ST(Token t)
	{
		yield return t.ToAssignmentLine(t.GetM64String(), Mnemonic.Fst, t.GetFpuStackString(0));   // TODO
		yield return t.ToFpuStackPopLine();
	}

	// de /0
	private IEnumerable<string> CodeDE_0(Token t)
	{
		t.Reader.BaseStream.Position--;
		var opcode = t.Reader.ReadByte();
		return opcode switch
		{
			0xc1 => faddp_ST1_ST(t),
			_ => throw new NotImplementedException($"de /0 {opcode:X2}")
		};
	}

	// de /0 c1
	private IEnumerable<string> faddp_ST1_ST(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Fadd, t.GetFpuStackString(1), t.GetFpuStackString(0));
		yield return t.ToFpuStackPopLine();
	}

	// de /1
	private IEnumerable<string> CodeDE_1(Token t)
	{
		t.Reader.BaseStream.Position--;
		var opcode = t.Reader.ReadByte();
		return opcode switch
		{
			0xc9 => fmulp_ST1_ST(t),
			_ => throw new NotImplementedException($"de /1 {opcode:X2}")
		};
	}

	// de /1 c9
	private IEnumerable<string> fmulp_ST1_ST(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Fmul, t.GetFpuStackString(1), t.GetFpuStackString(0));
		yield return t.ToFpuStackPopLine();
	}

	// de /3
	private IEnumerable<string> CodeDE_3(Token t)
	{
		t.Reader.BaseStream.Position--;
		var opcode = t.Reader.ReadByte();
		return opcode switch
		{
			0xd9 => fcompp_ST_ST1(t),
			_ => throw new NotImplementedException($"de /3 {opcode:X2}")
		};
	}

	// de /3 d9
	private IEnumerable<string> fcompp_ST_ST1(Token t)
	{
		yield return t.ToStatementLine(Mnemonic.Fcom, t.GetFpuStackString(1));
		yield return t.ToFpuStackPopLine();
		yield return t.ToFpuStackPopLine();
	}

	// de /7
	private IEnumerable<string> CodeDE_7(Token t)
	{
		t.Reader.BaseStream.Position--;
		var opcode = t.Reader.ReadByte();
		return opcode switch
		{
			0xf9 => fdivp_ST1_ST(t),
			_ => throw new NotImplementedException($"de /7 {opcode:X2}")
		};
	}

	private IEnumerable<string> fdivp_ST1_ST(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Fdiv, t.GetFpuStackString(1), t.GetFpuStackString(0));
		yield return t.ToFpuStackPopLine();
	}

	// df /4
	private IEnumerable<string> CodeDF_4(Token t)
	{
		t.Reader.BaseStream.Position--;
		var opcode = t.Reader.ReadByte();
		return opcode switch
		{
			0xe0 => fnstsw_AX(t),
			_ => throw new NotImplementedException($"df /4 {opcode:X2}")
		};
	}

	// df /4 e0
	private IEnumerable<string> fnstsw_AX(Token t)
	{
		yield return t.ToAssignmentLine(t.GetR16String(RegisterCode.Ax), Mnemonic.Fnstsw);
	}

	// e8
	private IEnumerable<string> call_rel1632(Token t)
	{
		var rel32 = t.Reader.ReadInt32();
		yield return t.ToStatementLine(Mnemonic.Call, t.FormatRegister32(((uint)(t.Eip + rel32)).ToNumberLiteralString()));
	}

	// e9
	private IEnumerable<string> jmp_rel1632(Token t)
	{
		var rel32 = t.Reader.ReadInt32();
		var address = t.Eip + rel32;
		t.Labels.Add(address);
		t.Labels.Add(t.Eip);   // HACK
		yield return $"goto loc_{address:X};";
	}

	// eb
	private IEnumerable<string> jmp_rel8(Token t)
	{
		var rel8 = t.Reader.ReadSByte();
		var address = t.Eip + rel8;
		t.Labels.Add(address);
		t.Labels.Add(t.Eip);   // HACK
		yield return $"goto loc_{address:X};";
	}

	// f6 /0
	private IEnumerable<string> test_rm8_imm8(Token t)
	{
		yield return t.ToStatementLine(Mnemonic.Test, t.GetRM8String(), t.ReadByteAsString());
	}

	// f7 /3
	private IEnumerable<string> neg_rm1632(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Neg, t.GetRM32String());
	}

	// f7 /5
	private IEnumerable<string> imul_eDX_eAX_rm1632(Token t)
	{
		yield return t.ToStatementLine(Mnemonic.Imul, t.GetRM32String());
	}

	// f7 /7
	private IEnumerable<string> idiv_eDX_eAX_rm1632(Token t)
	{
		yield return t.ToStatementLine(Mnemonic.Idiv, t.GetRM32String());
	}

	// ff /0
	private IEnumerable<string> inc_rm1632(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Inc, t.GetRM32String());
	}

	// ff /1
	private IEnumerable<string> dec_rm1632(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Dec, t.GetRM32String());
	}

	// ff /2
	private IEnumerable<string> call_rm1632(Token t)
	{
		yield return t.ToStatementLine(Mnemonic.Call, t.GetRM32String());
	}

	// ff /4
	private IEnumerable<string> jmp_rm1632(Token t)
	{
		t.Labels.Add(t.Eip);   // HACK
		yield return $"// TODO: jmp {t.GetRM32String()}"; // TODO
	}
}
