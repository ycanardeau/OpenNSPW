using Aigamo.Otsuki.Bluetooth;
using Aigamo.Otsuki.Transport;
using OpenNspw.App;

namespace OpenNspw.Desktop;

// The computer's Bluetooth, on Windows and macOS (BluetoothTransport, docs/Bluetooth.md).
internal sealed class DesktopBluetooth : IBluetoothRadio
{
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

	public BluetoothEndPoint GetLocalEndPoint()
	{
		return BluetoothTransport.GetLocalEndPoint();
	}

	public IReadOnlyList<BluetoothDevice> GetPairedDevices()
	{
		return BluetoothTransport.GetPairedDevices();
	}

	public IStreamConnector CreateConnector(Guid service, string serviceName)
	{
		return BluetoothTransport.CreateConnector(service, serviceName);
	}
}
