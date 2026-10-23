using System.Runtime.InteropServices;

namespace OpenNspw.Recorder122;

// The Win32 functions and constants of the debugger API, for a 32-bit debugger of a 32-bit program.
internal static class Win32
{
	public const uint DEBUG_ONLY_THIS_PROCESS = 0x2;
	public const uint INFINITE = 0xFFFFFFFF;

	public const uint DBG_CONTINUE = 0x00010002;
	public const uint DBG_EXCEPTION_NOT_HANDLED = 0x80010001;

	public const int EXCEPTION_DEBUG_EVENT = 1;
	public const int CREATE_THREAD_DEBUG_EVENT = 2;
	public const int CREATE_PROCESS_DEBUG_EVENT = 3;
	public const int EXIT_THREAD_DEBUG_EVENT = 4;
	public const int EXIT_PROCESS_DEBUG_EVENT = 5;
	public const int LOAD_DLL_DEBUG_EVENT = 6;

	public const uint EXCEPTION_BREAKPOINT = 0x80000003;
	public const uint EXCEPTION_SINGLE_STEP = 0x80000004;

	// DEBUG_EVENT of a 32-bit process: the code, the process and the thread, then the union, at 12. In it,
	// EXCEPTION_DEBUG_INFO's ExceptionCode at 0 and ExceptionAddress at 12; CREATE_PROCESS_DEBUG_INFO's hFile at 0,
	// hProcess at 4 and hThread at 8; CREATE_THREAD_DEBUG_INFO's hThread and LOAD_DLL_DEBUG_INFO's hFile at 0.
	public const int DebugEventSize = 96;
	public const int DebugEventUnion = 12;

	// CONTEXT of x86: ContextFlags at 0, Edi at 0x9C, Esi at 0xA0, Eip at 0xB8, EFlags at 0xC0, Esp at 0xC4.
	public const int ContextSize = 0x2CC;
	public const int ContextEdi = 0x9C;
	public const int ContextEsi = 0xA0;
	public const int ContextEip = 0xB8;
	public const int ContextEFlags = 0xC0;
	public const int ContextEsp = 0xC4;
	public const uint CONTEXT_CONTROL = 0x00010001;
	public const uint CONTEXT_INTEGER = 0x00010002;
	public const uint TrapFlag = 0x100;

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct STARTUPINFO
	{
		public int cb;
		public string? lpReserved;
		public string? lpDesktop;
		public string? lpTitle;
		public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
		public short wShowWindow, cbReserved2;
		public nint lpReserved2, hStdInput, hStdOutput, hStdError;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct PROCESS_INFORMATION
	{
		public nint hProcess, hThread;
		public int dwProcessId, dwThreadId;
	}

	[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	public static extern bool CreateProcess(string? lpApplicationName, string? lpCommandLine, nint lpProcessAttributes, nint lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, nint lpEnvironment, string? lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool WaitForDebugEvent(nint lpDebugEvent, uint dwMilliseconds);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool ContinueDebugEvent(int dwProcessId, int dwThreadId, uint dwContinueStatus);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool ReadProcessMemory(nint hProcess, nint lpBaseAddress, [Out] byte[] lpBuffer, int nSize, out nint lpNumberOfBytesRead);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool WriteProcessMemory(nint hProcess, nint lpBaseAddress, byte[] lpBuffer, int nSize, out nint lpNumberOfBytesWritten);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool FlushInstructionCache(nint hProcess, nint lpBaseAddress, nint dwSize);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool GetThreadContext(nint hThread, nint lpContext);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool SetThreadContext(nint hThread, nint lpContext);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool CloseHandle(nint hObject);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool TerminateProcess(nint hProcess, uint uExitCode);
}
