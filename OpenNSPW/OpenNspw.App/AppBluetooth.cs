using Aigamo.Otsuki.Bluetooth;
using Aigamo.Otsuki.Transport;
using OpenNspw.DirectPlay;

namespace OpenNspw.App;

// The computer's Bluetooth, which the host gives (IAppHost.Bluetooth) where the app can join over it and may use it: its
// own address, the devices paired with it, and channels to a service.
internal interface IBluetoothRadio
{
	// The address of the computer's Bluetooth controller. Fails with IOException if there is none, or it is off.
	BluetoothEndPoint GetLocalEndPoint();

	IReadOnlyList<BluetoothDevice> GetPairedDevices();

	// Opens and accepts channels to `service`, published under `serviceName`.
	IStreamConnector CreateConnector(Guid service, string serviceName);
}

// Bluetooth for the games (docs/Bluetooth.md), on the host's radio. The multiplayer form chooses the device to join
// before the game connects, so ChooseHost answers with it rather than asking.
internal sealed class AppBluetooth(IBluetoothRadio radio) : IBluetoothProvider
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

	private readonly IBluetoothRadio _radio = radio;

	// The device to join, which the multiplayer screen sets on the game's thread before the game connects, and
	// ChooseHost reads there.
	public BluetoothEndPoint? Host { get; set; }

	// The computer's own address, which a guest joins, or null if Bluetooth is off.
	public BluetoothEndPoint? FindLocalEndPoint()
	{
		try
		{
			return _radio.GetLocalEndPoint();
		}
		catch (IOException)
		{
			return null;
		}
	}

	public IReadOnlyList<BluetoothDevice> GetPairedDevices()
	{
		return _radio.GetPairedDevices();
	}

	public IDatagramTransport CreateTransport(BluetoothEndPoint localEndPoint)
	{
		return new StreamDatagramTransport(_radio.CreateConnector(Service, ServiceName));
	}

	public BluetoothEndPoint? ChooseHost()
	{
		return Host;
	}
}
