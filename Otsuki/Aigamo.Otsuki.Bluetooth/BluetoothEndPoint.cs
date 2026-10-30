using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Aigamo.Otsuki.Bluetooth;

/// <summary>
/// A Bluetooth device, by its 48-bit address (BD_ADDR), written like <c>00:1A:7D:DA:71:13</c>. A device runs an
/// application's service once, so the address is enough to reach it: the RFCOMM channel of the service is looked up
/// whenever a stream to it opens.
/// </summary>
public sealed class BluetoothEndPoint(ulong address) : EndPoint
{
	private const int AddressSize = 6;

	private const ulong MaxAddress = (1UL << (AddressSize * 8)) - 1;

	/// <summary>
	/// The computer's own Bluetooth controller, as the local endpoint to host or connect from.
	/// </summary>
	public static BluetoothEndPoint Any { get; } = new(0);

	public ulong Address { get; } =
		address <= MaxAddress ? address : throw new ArgumentOutOfRangeException(nameof(address));

	public override AddressFamily AddressFamily => AddressFamily.Unspecified;

	internal static BluetoothEndPoint FromBytes(ReadOnlySpan<byte> bytes)
	{
		var address = 0UL;
		foreach (var b in bytes[..AddressSize])
			address = (address << 8) | b;
		return new(address);
	}

	internal byte[] ToBytes()
	{
		var bytes = new byte[AddressSize];
		for (var i = 0; i < AddressSize; i++)
			bytes[i] = (byte)(Address >> ((AddressSize - 1 - i) * 8));
		return bytes;
	}

	/// <summary>
	/// Reads an address of six hexadecimal bytes, separated by colons or hyphens.
	/// </summary>
	public static bool TryParse(string text, out BluetoothEndPoint? endPoint)
	{
		endPoint = null;
		var parts = text.Split(':', '-');
		if (parts.Length != AddressSize)
			return false;

		var bytes = new byte[AddressSize];
		for (var i = 0; i < AddressSize; i++)
		{
			if (
				parts[i].Length != 2
				|| !byte.TryParse(
					parts[i],
					NumberStyles.AllowHexSpecifier,
					CultureInfo.InvariantCulture,
					out bytes[i]
				)
			)
				return false;
		}

		endPoint = FromBytes(bytes);
		return true;
	}

	public static BluetoothEndPoint Parse(string text) =>
		TryParse(text, out var endPoint)
			? endPoint!
			: throw new FormatException($"'{text}' is not a Bluetooth address.");

	public override bool Equals(object? obj) =>
		obj is BluetoothEndPoint other && other.Address == Address;

	public override int GetHashCode() => Address.GetHashCode();

	public override string ToString() =>
		string.Join(':', ToBytes().Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));
}
