using System.Buffers.Binary;
using Aigamo.Enzan;

namespace OpenNspw.Singleplayer;

// The emulator's memory as bytes, words, doublewords and quadwords, at the executable's addresses. The translated
// functions read and write it through these (emulator.Memory32[address]).

internal sealed class Memory8(byte[] memory, uint start)
{
	private readonly byte[] _memory = memory;
	private readonly uint _start = start;

	public Register8 this[Register32 address]
	{
		get => new(_memory[address.Value - _start]);
		set => _memory[address.Value - _start] = value.Value;
	}
}

internal sealed class Memory16(byte[] memory, uint start)
{
	private readonly byte[] _memory = memory;
	private readonly uint _start = start;

	public Register16 this[Register32 address]
	{
		get => new(BinaryPrimitives.ReadUInt16LittleEndian(_memory.AsSpan((int)(address.Value - _start))));
		set => BinaryPrimitives.WriteUInt16LittleEndian(_memory.AsSpan((int)(address.Value - _start)), value.Value);
	}
}

internal sealed class Memory32(byte[] memory, uint start)
{
	private readonly byte[] _memory = memory;
	private readonly uint _start = start;

	public Register32 this[Register32 address]
	{
		get => new(BinaryPrimitives.ReadUInt32LittleEndian(_memory.AsSpan((int)(address.Value - _start))));
		set => BinaryPrimitives.WriteUInt32LittleEndian(_memory.AsSpan((int)(address.Value - _start)), value.Value);
	}
}

internal sealed class Memory64(byte[] memory, uint start)
{
	private readonly byte[] _memory = memory;
	private readonly uint _start = start;

	public Register64 this[Register32 address]
	{
		get => new(BinaryPrimitives.ReadUInt64LittleEndian(_memory.AsSpan((int)(address.Value - _start))));
		set => BinaryPrimitives.WriteUInt64LittleEndian(_memory.AsSpan((int)(address.Value - _start)), value.Value);
	}
}
