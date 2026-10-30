using Aigamo.Otsuki.Bluetooth;
using Aigamo.Otsuki.Transport;

namespace OpenNspw.DirectPlay;

// Bluetooth for the DirectPlay 8 Bluetooth service provider, from the platform. A platform gives one only where the app
// may use Bluetooth (BluetoothTransport.IsAllowed): on macOS, an app without NSBluetoothAlwaysUsageDescription is killed by its first Bluetooth call,
// before anything can catch it.
public interface IBluetoothProvider
{
	// Creates the transport of a session, as PeerOptions.TransportFactory does.
	IDatagramTransport CreateTransport(BluetoothEndPoint localEndPoint);

	// Asks the player for the device that hosts the game, as DirectPlay's own dialog does when Connect may query for
	// addressing (DPNCONNECT_OKTOQUERYFORADDRESSING). Returns null if the player cancels.
	BluetoothEndPoint? ChooseHost();
}
