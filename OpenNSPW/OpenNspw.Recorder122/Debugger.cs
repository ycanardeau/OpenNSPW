using System.ComponentModel;
using System.Runtime.InteropServices;
using static OpenNspw.Recorder122.Win32;

namespace OpenNspw.Recorder122;

// Runs a 32-bit program under the debugger API, and calls back at its breakpoints, before the instruction there runs.
// A breakpoint is an int3 written over its first byte: at a hit, the byte is put back and the thread steps over it once,
// then the int3 is written again. The callback may read and write the program's memory, and end it (Terminate).
internal sealed class Debugger(string executable, IReadOnlyCollection<uint> breakpoints, Action<Debugger, uint> hit) : IDisposable
{
	private const byte Int3 = 0xCC;

	private readonly string _executable = executable;
	private readonly uint[] _breakpoints = breakpoints.ToArray();
	private readonly Action<Debugger, uint> _hit = hit;
	private readonly Dictionary<uint, byte> _originalBytes = [];
	private readonly Dictionary<int, nint> _threads = [];
	private readonly Dictionary<int, uint> _steppingOver = [];
	private readonly nint _event = Marshal.AllocHGlobal(DebugEventSize);
	private readonly nint _context = Marshal.AllocHGlobal(ContextSize);
	private nint _process;
	private bool _hasSeenSystemBreakpoint;
	private bool _isTerminating;

	private static void Check(bool succeeded, string what)
	{
		if (!succeeded)
			throw new Win32Exception(Marshal.GetLastWin32Error(), what);
	}

	public byte[] Read(uint address, int size)
	{
		var bytes = new byte[size];
		Check(ReadProcessMemory(_process, (nint)address, bytes, size, out _), $"ReadProcessMemory at {address:X8}");
		return bytes;
	}

	public uint ReadUInt32(uint address) => BitConverter.ToUInt32(Read(address, 4));

	public int ReadInt32(uint address) => BitConverter.ToInt32(Read(address, 4));

	public short ReadInt16(uint address) => BitConverter.ToInt16(Read(address, 2));

	private void Write(uint address, byte value)
	{
		Check(WriteProcessMemory(_process, (nint)address, [value], 1, out _), $"WriteProcessMemory at {address:X8}");
		FlushInstructionCache(_process, (nint)address, 1);
	}

	public void WriteInt16(uint address, short value)
	{
		var bytes = BitConverter.GetBytes(value);
		Check(WriteProcessMemory(_process, (nint)address, bytes, bytes.Length, out _), $"WriteProcessMemory at {address:X8}");
	}

	// Ends the program: its threads step over no more breakpoints, and Run returns once it has exited.
	public void Terminate()
	{
		_isTerminating = true;
		Check(TerminateProcess(_process, 0), "TerminateProcess");
	}

	private void InstallBreakpoints()
	{
		foreach (var address in _breakpoints)
		{
			_originalBytes[address] = Read(address, 1)[0];
			Write(address, Int3);
		}
	}

	// Puts the instruction back, moves the thread back onto it, and has it step once.
	private void StepOver(int threadId, uint address)
	{
		var thread = _threads[threadId];
		Write(address, _originalBytes[address]);
		Marshal.WriteInt32(_context, 0, unchecked((int)CONTEXT_CONTROL));
		Check(GetThreadContext(thread, _context), "GetThreadContext");
		Marshal.WriteInt32(_context, ContextEip, (int)address);
		Marshal.WriteInt32(_context, ContextEFlags, Marshal.ReadInt32(_context, ContextEFlags) | (int)TrapFlag);
		Check(SetThreadContext(thread, _context), "SetThreadContext");
		_steppingOver[threadId] = address;
	}

	private uint HandleException(int threadId)
	{
		var code = (uint)Marshal.ReadInt32(_event, DebugEventUnion);
		var address = (uint)Marshal.ReadInt32(_event, DebugEventUnion + 12);
		if (_isTerminating)
			return DBG_CONTINUE;
		if (code == EXCEPTION_BREAKPOINT)
		{
			if (_originalBytes.ContainsKey(address))
			{
				_hit(this, address);
				if (!_isTerminating)
					StepOver(threadId, address);
				return DBG_CONTINUE;
			}
			if (!_hasSeenSystemBreakpoint)
			{
				_hasSeenSystemBreakpoint = true;
				return DBG_CONTINUE;
			}
		}
		if (code == EXCEPTION_SINGLE_STEP && _steppingOver.Remove(threadId, out var stepped))
		{
			Write(stepped, Int3);
			return DBG_CONTINUE;
		}
		return DBG_EXCEPTION_NOT_HANDLED;
	}

	// Runs the program until it exits.
	public void Run()
	{
		var startup = new STARTUPINFO { cb = Marshal.SizeOf<STARTUPINFO>() };
		Check(CreateProcess(_executable, null, 0, 0, false, DEBUG_ONLY_THIS_PROCESS, 0, Path.GetDirectoryName(_executable), ref startup, out var information), "CreateProcess");
		CloseHandle(information.hThread);

		while (true)
		{
			Check(WaitForDebugEvent(_event, INFINITE), "WaitForDebugEvent");
			var code = Marshal.ReadInt32(_event, 0);
			var processId = Marshal.ReadInt32(_event, 4);
			var threadId = Marshal.ReadInt32(_event, 8);
			var status = DBG_CONTINUE;
			switch (code)
			{
				case CREATE_PROCESS_DEBUG_EVENT:
					CloseHandle(Marshal.ReadIntPtr(_event, DebugEventUnion));
					_process = Marshal.ReadIntPtr(_event, DebugEventUnion + 4);
					_threads[threadId] = Marshal.ReadIntPtr(_event, DebugEventUnion + 8);
					InstallBreakpoints();
					break;
				case CREATE_THREAD_DEBUG_EVENT:
					_threads[threadId] = Marshal.ReadIntPtr(_event, DebugEventUnion);
					break;
				case EXIT_THREAD_DEBUG_EVENT:
					_threads.Remove(threadId);
					break;
				case LOAD_DLL_DEBUG_EVENT:
					var file = Marshal.ReadIntPtr(_event, DebugEventUnion);
					if (file != 0)
						CloseHandle(file);
					break;
				case EXCEPTION_DEBUG_EVENT:
					status = HandleException(threadId);
					break;
				case EXIT_PROCESS_DEBUG_EVENT:
					ContinueDebugEvent(processId, threadId, status);
					CloseHandle(information.hProcess);
					return;
			}
			Check(ContinueDebugEvent(processId, threadId, status), "ContinueDebugEvent");
		}
	}

	public void Dispose()
	{
		Marshal.FreeHGlobal(_event);
		Marshal.FreeHGlobal(_context);
	}
}
