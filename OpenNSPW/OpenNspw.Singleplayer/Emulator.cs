using Aigamo.Enzan;
using OpenNspw.Singleplayer.Subroutines;

namespace OpenNspw.Singleplayer;

// Runs functions of the single-player executable (docs/Singleplayer.md): its translated functions on Enzan, in a memory
// that holds the executable's image at its own addresses, with a stack below it.
//
// A call goes to a native function if one is set at its address, and otherwise to the translated function there. The
// slots of the import table hold stubs, addresses outside the memory, that call the native function set for the import;
// an import that has none throws when it is called.
public sealed class Emulator
{
	private const uint StackSize = 0x10000;

	// After the image: memory for what the native side places there, such as the objects behind COM pointers.
	private const uint HostSize = 0x1000;

	// The stubs: addresses that code calls, but that are not in the memory.
	private const uint FirstStub = 0xFFFF0000;

	// The return address that a call pushes. The translated functions return to their caller without popping it.
	private static readonly Register32 ReturnAddress = new(0xFFFFFFFF);

	// A native function: what the stub or address stands for, the bytes of arguments it pops (a __stdcall function), and
	// what it does, if it is implemented.
	private sealed record Native(string Name, int ArgumentBytes, Action? Handler);

	private readonly byte[] _memory;
	private readonly Dictionary<uint, Native> _natives = [];
	private readonly Dictionary<string, uint> _importStubs = [];
	private uint _nextStub = FirstStub;
	private uint _nextHost;
	private int _depth;

	public uint ImageBase { get; }

	// The lowest address of the memory: the bottom of the stack.
	public uint Start { get; }

	public Cpu Cpu { get; }
	internal Memory8 Memory8 { get; }
	internal Memory16 Memory16 { get; }
	internal Memory32 Memory32 { get; }
	internal Memory64 Memory64 { get; }

	public Span<byte> At(uint address, int length) => _memory.AsSpan((int)(address - Start), length);

	// All the memory, from Start: for tests that run code twice from the same state.
	internal Span<byte> Memory => _memory;

	// Memory after the image that the code of the executable does not use, for the native side.
	public uint Allocate(int size)
	{
		var address = _nextHost;
		_nextHost += (uint)((size + 7) & ~7);
		if (_nextHost > Start + _memory.Length)
			throw new InvalidOperationException("The emulator's host memory is full.");
		return address;
	}

	// Makes code calling the address run the handler instead of the translated function there.
	public void SetNative(uint address, Action handler) => _natives[address] = new Native($"0x{address:X}", 0, handler);

	// Implements an import. A __stdcall function pops its arguments: argumentBytes says how many bytes.
	public void SetImport(string name, int argumentBytes, Action handler) =>
		_natives[_importStubs[name]] = new Native(name, argumentBytes, handler);

	// A new address that runs the handler when code calls it, such as a method in the table of a COM object.
	public uint AddStub(string name, int argumentBytes, Action handler)
	{
		var stub = _nextStub++;
		_natives[stub] = new Native(name, argumentBytes, handler);
		return stub;
	}

	// In a native function: its argument at the index, a doubleword. The return address is on top of the stack.
	public uint Argument(int index) => Memory32[Cpu.Esp + new Register32((uint)(4 + 4 * index))].Value;

	private void Call(uint address)
	{
		Cpu.Push(ReturnAddress);
		if (_natives.TryGetValue(address, out var native))
		{
			if (native.Handler is null)
				throw new NotImplementedException($"The emulator has no native function for {native.Name}.");

			native.Handler();
			Cpu.Pop32();
			Cpu.Esp += new Register32((uint)native.ArgumentBytes);
		}
		else if (SubroutineTable.Find(address) is { } subroutine)
		{
			subroutine(this);
			Cpu.Pop32();
		}
		else
			throw new NotImplementedException($"The emulator has no function at 0x{address:X}.");
	}

	public Emulator(ReadOnlySpan<byte> executable)
	{
		var image = PortableExecutable.Read(executable);
		ImageBase = image.ImageBase;
		Start = ImageBase - StackSize;
		_memory = new byte[StackSize + image.ImageSize + HostSize];
		_nextHost = ImageBase + image.ImageSize;

		foreach (var section in image.Sections)
			executable
				.Slice((int)section.RawOffset, (int)Math.Min(section.RawSize, section.VirtualSize))
				.CopyTo(At(section.Address, (int)section.VirtualSize));

		Cpu = new Cpu(_memory, new Register32(Start), address => Call(address.Value));
		Memory8 = new Memory8(_memory, Start);
		Memory16 = new Memory16(_memory, Start);
		Memory32 = new Memory32(_memory, Start);
		Memory64 = new Memory64(_memory, Start);

		foreach (var import in image.Imports)
		{
			var stub = _nextStub++;
			_natives[stub] = new Native(import.Name, 0, null);
			_importStubs[import.Name] = stub;
			Memory32[new Register32(import.Slot)] = new Register32(stub);
		}

		// __ftol: ST(0), popped and truncated, in EDX:EAX.
		SetNative(
			Functions.Ftol,
			() =>
			{
				var value = (ulong)(long)Cpu.Fpu.Stack.Pop().ToDouble();
				Cpu.Eax = new Register32((uint)value);
				Cpu.Edx = new Register32((uint)(value >> 32));
			}
		);
	}

	// Calls the function at the address with the arguments, pushed as __cdecl does, and returns EAX. Called from outside
	// the emulated code, it starts with an empty stack; from a native function, it continues on the caller's stack.
	public uint Run(uint address, params ReadOnlySpan<uint> arguments)
	{
		if (_depth == 0)
			Cpu.Esp = new Register32(ImageBase);

		_depth++;
		try
		{
			for (var i = arguments.Length - 1; i >= 0; i--)
				Cpu.Push(new Register32(arguments[i]));
			Call(address);
			Cpu.Esp += new Register32((uint)(4 * arguments.Length));
			return Cpu.Eax.Value;
		}
		finally
		{
			_depth--;
		}
	}
}
