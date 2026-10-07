using System.Runtime.InteropServices;

namespace OpenNspw;

// Stand-ins for the registry functions that the game uses (winreg.h, and dxutil's helpers in dxutil.cs). Values are
// kept by the platform, under "<key path>\<value name>".

// A handle to an open registry key. Zero is NULL.
[StructLayout(LayoutKind.Sequential)]
public readonly struct HKEY(int value)
{
	public int Value { get; } = value;
}

public static class winreg
{
	public static readonly HKEY HKEY_CURRENT_USER = new(unchecked((int)0x80000001));

	public const uint REG_OPTION_NON_VOLATILE = 0;
	public const uint KEY_READ = 0x20019;
	public const uint KEY_WRITE = 0x20006;
	public const uint REG_SZ = 1;
	public const int ERROR_SUCCESS = 0;
}

public partial class Nspw
{
	private readonly Dictionary<int, string> _registryKeys = [];
	private int _nextRegistryKey = 1;

	// The path of an open key, for the platform's settings.
	internal string? RegistryPath(HKEY hKey)
	{
		return _registryKeys.GetValueOrDefault(hKey.Value);
	}

	public int RegCreateKeyEx(HKEY hKey, string lpSubKey, uint Reserved, string? lpClass, uint dwOptions, uint samDesired, object? lpSecurityAttributes, ref HKEY phkResult, object? lpdwDisposition)
	{
		var handle = _nextRegistryKey++;
		_registryKeys[handle] = lpSubKey;
		phkResult = new HKEY(handle);
		return winreg.ERROR_SUCCESS;
	}

	public int RegCloseKey(HKEY hKey)
	{
		_registryKeys.Remove(hKey.Value);
		return winreg.ERROR_SUCCESS;
	}

	public int DXUtil_ReadStringRegKeyCch(HKEY hKey, string strRegName, Span<byte> strDest, uint cchDest, string strDefault)
	{
		var value = RegistryPath(hKey) is { } path ? _platform.ReadSetting($"{path}\\{strRegName}") : null;
		CopyString(value ?? strDefault, strDest, (int)cchDest);
		return S_OK;
	}

	public int DXUtil_WriteStringRegKey(HKEY hKey, string strRegName, ReadOnlySpan<byte> strValue)
	{
		if (RegistryPath(hKey) is { } path)
		{
			_platform.WriteSetting($"{path}\\{strRegName}", CString(strValue));
		}

		return S_OK;
	}
}
