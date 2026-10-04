namespace OpenNspw.Disassembler;

internal sealed class TwoByteInstructions : Dictionary<int, Instruction>
{
	public TwoByteInstructions()
	{
		this[0x82] = new Instruction(jb_rel32);
		this[0x83] = new Instruction(jae_rel1632);
		this[0x84] = new Instruction(je_rel1632);
		this[0x85] = new Instruction(jne_rel1632);
		this[0x87] = new Instruction(ja_rel1632);
		this[0x8c] = new Instruction(jl_rel1632);
		this[0x8d] = new Instruction(jge_rel32);
		this[0x8e] = new Instruction(jle_rel1632);
		this[0x8f] = new Instruction(jg_rel1632);
		this[0x94] = new Instruction(sete_rm8, null, null, null, null, null, null, null);
		this[0x9f] = new Instruction(setg_rm8, null, null, null, null, null, null, null);
		this[0xaf] = new Instruction(imul_r1632_rm1632, true);
		this[0xbf] = new Instruction(movsx_r1632_rm16, true);
	}

	// 82
	private IEnumerable<string> jb_rel32(Token t)
	{
		var rel32 = t.Reader.ReadInt32();
		var address = t.Eip + rel32;
		t.Labels.Add(address);
		yield return t.ToJumpLine(Mnemonic.Jb, address);
	}

	// 83
	private IEnumerable<string> jae_rel1632(Token t)
	{
		var rel32 = t.Reader.ReadInt32();
		var address = t.Eip + rel32;
		t.Labels.Add(address);
		yield return t.ToJumpLine(Mnemonic.Jae, address);
	}

	// 84
	private IEnumerable<string> je_rel1632(Token t)
	{
		var rel32 = t.Reader.ReadInt32();
		var address = t.Eip + rel32;
		t.Labels.Add(address);
		yield return t.ToJumpLine(Mnemonic.Je, address);
	}

	// 85
	private IEnumerable<string> jne_rel1632(Token t)
	{
		var rel32 = t.Reader.ReadInt32();
		var address = t.Eip + rel32;
		t.Labels.Add(address);
		yield return t.ToJumpLine(Mnemonic.Jne, address);
	}

	// 87
	private IEnumerable<string> ja_rel1632(Token t)
	{
		var rel32 = t.Reader.ReadInt32();
		var address = t.Eip + rel32;
		t.Labels.Add(address);
		yield return t.ToJumpLine(Mnemonic.Ja, address);
	}

	// 8c
	private IEnumerable<string> jl_rel1632(Token t)
	{
		var rel32 = t.Reader.ReadInt32();
		var address = t.Eip + rel32;
		t.Labels.Add(address);
		yield return t.ToJumpLine(Mnemonic.Jl, address);
	}

	// 8d
	private IEnumerable<string> jge_rel32(Token t)
	{
		var rel32 = t.Reader.ReadInt32();
		var address = t.Eip + rel32;
		t.Labels.Add(address);
		yield return t.ToJumpLine(Mnemonic.Jge, address);
	}

	// 8e
	private IEnumerable<string> jle_rel1632(Token t)
	{
		var rel32 = t.Reader.ReadInt32();
		var address = t.Eip + rel32;
		t.Labels.Add(address);
		yield return t.ToJumpLine(Mnemonic.Jle, address);
	}

	// 8f
	private IEnumerable<string> jg_rel1632(Token t)
	{
		var rel32 = t.Reader.ReadInt32();
		var address = t.Eip + rel32;
		t.Labels.Add(address);
		yield return t.ToJumpLine(Mnemonic.Jg, address);
	}

	// 94
	private IEnumerable<string> sete_rm8(Token t)
	{
		yield return t.ToAssignmentLine(t.GetRM8String(), Mnemonic.Sete);
	}

	// 9f
	private IEnumerable<string> setg_rm8(Token t)
	{
		yield return t.ToAssignmentLine(t.GetRM8String(), Mnemonic.Setg);
	}

	// af
	private IEnumerable<string> imul_r1632_rm1632(Token t)
	{
		yield return t.ToAssignmentLine(Mnemonic.Imul, t.GetR32String(), t.GetRM32String());
	}

	// bf
	private IEnumerable<string> movsx_r1632_rm16(Token t)
	{
		yield return t.ToAssignmentLine(t.GetR32String(), Mnemonic.Movsx, t.GetRM16String());
	}
}
