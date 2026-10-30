using Aigamo.Otsuki.Bluetooth;
using Aigamo.Otsuki.Transport;
using OpenNspw.DirectPlay;

namespace OpenNspw.Desktop;

// Bluetooth for the games, on Windows, and on macOS before 26 where the app may use it (docs/Bluetooth.md). The
// multiplayer form chooses the device to join before the game connects, so ChooseHost answers with it rather than
// asking.
internal sealed class DesktopBluetooth : IBluetoothProvider
{
	// The service that hosts publish: the game's application GUID (g_guidApp, Nspw.AppGuid).
	private static readonly Guid Service = new(
		0x11bc0eb,
		0xbdb3,
		0x11d6,
		0xba,
		0x95,
		0x9c,
		0xce,
		0x36,
		0x89,
		0x70,
		0x55
	);

	private const string ServiceName = "OpenNSPW";

	private BluetoothEndPoint? _host;

	private DesktopBluetooth() { }

	// Bluetooth, if the app can join over it and may use it: always on Windows; on macOS before 26, OpenNSPW.app
	// declares why (tools/package-macos-app.sh), and without that macOS kills the app at its first Bluetooth call. On
	// macOS 26, a Mac can only host, which the form doesn't offer alone. OPENNSPW_BLUETOOTH=1 turns it on anyway: for a
	// build run from a terminal that may use Bluetooth, or to try a later macOS.
	public static DesktopBluetooth? Create()
	{
		return
			BluetoothTransport.IsSupported
			&& (
				BluetoothTransport.CanConnect && BluetoothTransport.IsAllowed
				|| Environment.GetEnvironmentVariable("OPENNSPW_BLUETOOTH") == "1"
			)
			? new DesktopBluetooth()
			: null;
	}

	// The device to join, which the multiplayer form sets before the game connects.
	public BluetoothEndPoint? Host
	{
		get => Volatile.Read(ref _host);
		set => Volatile.Write(ref _host, value);
	}

	// The computer's own address, which a guest joins, or null if Bluetooth is off.
	public BluetoothEndPoint? FindLocalEndPoint()
	{
		try
		{
			return BluetoothTransport.GetLocalEndPoint();
		}
		catch (IOException)
		{
			return null;
		}
	}

	public IReadOnlyList<BluetoothDevice> GetPairedDevices()
	{
		return BluetoothTransport.GetPairedDevices();
	}

	public IDatagramTransport CreateTransport(BluetoothEndPoint localEndPoint)
	{
		return new StreamDatagramTransport(
			BluetoothTransport.CreateConnector(Service, ServiceName)
		);
	}

	public BluetoothEndPoint? ChooseHost()
	{
		return Host;
	}
}
