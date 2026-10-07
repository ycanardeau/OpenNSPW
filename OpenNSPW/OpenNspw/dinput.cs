using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace OpenNspw;

// Stand-ins for the DirectInput 8 interfaces, structs and constants that the game uses (dinput.h). Devices are
// buffered: the platform posts key and mouse button events (PostKeyboardInput, PostMouseInput), and GetDeviceData reads
// them in order, as DirectInput's buffered data.

[StructLayout(LayoutKind.Sequential)]
public struct DIDEVICEOBJECTDATA
{
	public uint dwOfs;
	public uint dwData;
	public uint dwTimeStamp;
	public uint dwSequence;
	public nuint uAppData;
}

[StructLayout(LayoutKind.Sequential)]
public struct DIPROPHEADER
{
	public uint dwSize;
	public uint dwHeaderSize;
	public uint dwObj;
	public uint dwHow;
}

[StructLayout(LayoutKind.Sequential)]
public struct DIPROPDWORD
{
	public DIPROPHEADER diph;
	public uint dwData;
}

[StructLayout(LayoutKind.Sequential)]
public struct DIMOUSESTATE2
{
	public int lX;
	public int lY;
	public int lZ;
	public Array8<byte> rgbButtons;
}

// A data format (c_dfDIKeyboard, c_dfDIMouse2): which device it describes.
public sealed record DIDATAFORMAT(string Name);

public unsafe interface IDirectInputDevice8 : IUnknown
{
	int SetDataFormat(DIDATAFORMAT lpdf);

	int SetCooperativeLevel(HWND? hwnd, uint dwFlags);

	int SetProperty(nint rguidProp, DIPROPHEADER* pdiph);

	int Acquire();

	int Unacquire();

	int GetDeviceData(uint cbObjectData, DIDEVICEOBJECTDATA* rgdod, uint* pdwInOut, uint dwFlags);

	int GetDeviceState(uint cbData, void* lpvData);
}

public interface IDirectInput8 : IUnknown
{
	int CreateDevice(Guid rguid, out IDirectInputDevice8? lplpDirectInputDevice, object? pUnkOuter);
}

public static class dinput
{
	public const uint DIRECTINPUT_VERSION = 0x0800;

	public const int DI_OK = 0;
	public const int DIERR_INPUTLOST = unchecked((int)0x8007001E);
	public const int DIERR_NOTACQUIRED = unchecked((int)0x8007000C);

	public static readonly Guid IID_IDirectInput8 = new(0xbf798031, 0x483a, 0x4da2, 0xaa, 0x99, 0x5d, 0x64, 0xed, 0x36, 0x97, 0x00);
	public static readonly Guid GUID_SysMouse = new(0x6f1d2b60, 0xd5a0, 0x11cf, 0xbf, 0xc7, 0x44, 0x45, 0x53, 0x54, 0x00, 0x00);
	public static readonly Guid GUID_SysKeyboard = new(0x6f1d2b61, 0xd5a0, 0x11cf, 0xbf, 0xc7, 0x44, 0x45, 0x53, 0x54, 0x00, 0x00);

	public static readonly DIDATAFORMAT c_dfDIKeyboard = new("Keyboard");
	public static readonly DIDATAFORMAT c_dfDIMouse2 = new("Mouse2");

	public const uint DISCL_EXCLUSIVE = 0x00000001;
	public const uint DISCL_NONEXCLUSIVE = 0x00000002;
	public const uint DISCL_FOREGROUND = 0x00000004;
	public const uint DISCL_BACKGROUND = 0x00000008;

	public const nint DIPROP_BUFFERSIZE = 1;
	public const nint DIPROP_AXISMODE = 2;
	public const uint DIPH_DEVICE = 0;
	public const uint DIPROPAXISMODE_ABS = 0;
	public const uint DIPROPAXISMODE_REL = 1;

	// The offsets of DIMOUSESTATE2's buttons.
	public const uint DIMOFS_X = 0;
	public const uint DIMOFS_Y = 4;
	public const uint DIMOFS_Z = 8;
	public const uint DIMOFS_BUTTON0 = 12;
	public const uint DIMOFS_BUTTON1 = 13;
	public const uint DIMOFS_BUTTON2 = 14;
	public const uint DIMOFS_BUTTON3 = 15;
	public const uint DIMOFS_BUTTON4 = 16;

	public const uint DIK_ESCAPE = 0x01;
	public const uint DIK_Q = 0x10;
	public const uint DIK_W = 0x11;
	public const uint DIK_E = 0x12;
	public const uint DIK_R = 0x13;
	public const uint DIK_RETURN = 0x1C;
	public const uint DIK_A = 0x1E;
	public const uint DIK_S = 0x1F;
	public const uint DIK_D = 0x20;
	public const uint DIK_F = 0x21;
	public const uint DIK_G = 0x22;
	public const uint DIK_Z = 0x2C;
	public const uint DIK_X = 0x2D;
	public const uint DIK_C = 0x2E;
	public const uint DIK_V = 0x2F;
	public const uint DIK_SPACE = 0x39;
}

public sealed unsafe class DirectInputDevice8 : IDirectInputDevice8
{
	private readonly ConcurrentQueue<DIDEVICEOBJECTDATA> _data = new();
	private uint _sequence;

	public bool Acquired { get; private set; }

	public DIDATAFORMAT? Format { get; private set; }

	// Adds an event to the device's buffer, if it is acquired.
	public void Post(uint offset, uint data, uint timeStamp)
	{
		if (Acquired)
		{
			_data.Enqueue(new DIDEVICEOBJECTDATA { dwOfs = offset, dwData = data, dwTimeStamp = timeStamp, dwSequence = ++_sequence });
		}
	}

	public int SetDataFormat(DIDATAFORMAT lpdf)
	{
		Format = lpdf;
		return dinput.DI_OK;
	}

	public int SetCooperativeLevel(HWND? hwnd, uint dwFlags)
	{
		return dinput.DI_OK;
	}

	public int SetProperty(nint rguidProp, DIPROPHEADER* pdiph)
	{
		return dinput.DI_OK;
	}

	public int Acquire()
	{
		Acquired = true;
		return dinput.DI_OK;
	}

	public int Unacquire()
	{
		Acquired = false;
		_data.Clear();
		return dinput.DI_OK;
	}

	public int GetDeviceData(uint cbObjectData, DIDEVICEOBJECTDATA* rgdod, uint* pdwInOut, uint dwFlags)
	{
		if (!Acquired)
		{
			*pdwInOut = 0;
			return dinput.DIERR_NOTACQUIRED;
		}

		uint count = 0;
		while (count < *pdwInOut && _data.TryDequeue(out var data))
		{
			rgdod[count++] = data;
		}

		*pdwInOut = count;
		return dinput.DI_OK;
	}

	public int GetDeviceState(uint cbData, void* lpvData)
	{
		NativeMemory.Clear(lpvData, cbData);
		return dinput.DI_OK;
	}

	public uint Release()
	{
		return 0;
	}
}

public sealed class DirectInput8 : IDirectInput8
{
	public DirectInputDevice8? Keyboard { get; private set; }

	public DirectInputDevice8? Mouse { get; private set; }

	public int CreateDevice(Guid rguid, out IDirectInputDevice8? lplpDirectInputDevice, object? pUnkOuter)
	{
		var device = new DirectInputDevice8();
		if (rguid == dinput.GUID_SysKeyboard)
		{
			Keyboard = device;
		}
		else if (rguid == dinput.GUID_SysMouse)
		{
			Mouse = device;
		}
		else
		{
			lplpDirectInputDevice = null;
			return winerror.E_FAIL;
		}

		lplpDirectInputDevice = device;
		return dinput.DI_OK;
	}

	public uint Release()
	{
		return 0;
	}
}

public partial class Nspw
{
	private DirectInput8? _directInput;

	public int DirectInput8Create(object? hinst, uint dwVersion, Guid riidltf, out IDirectInput8? ppvOut, object? punkOuter)
	{
		_directInput = new DirectInput8();
		ppvOut = _directInput;
		return dinput.DI_OK;
	}

	// A key went down or up, by its DIK_* scan code.
	public void PostKeyboardInput(uint dik, bool down)
	{
		_directInput?.Keyboard?.Post(dik, down ? 0x80u : 0, _platform.timeGetTime());
	}

	// A mouse button went down or up: 0 is the left button, 1 the right button.
	public void PostMouseInput(int button, bool down)
	{
		_directInput?.Mouse?.Post(dinput.DIMOFS_BUTTON0 + (uint)button, down ? 0x80u : 0, _platform.timeGetTime());
	}
}
