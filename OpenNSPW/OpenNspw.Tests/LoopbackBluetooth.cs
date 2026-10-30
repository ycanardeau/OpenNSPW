using Aigamo.Otsuki.Bluetooth;
using Aigamo.Otsuki.Tests;
using Aigamo.Otsuki.Transport;
using OpenNspw.DirectPlay;

namespace OpenNspw.Tests;

// Bluetooth between games in one process, over in-memory streams. Each game's provider has an address of its own, and
// answers ChooseHost with Host, as if the player chose that device; null, as if the player cancelled.
internal sealed class LoopbackBluetooth(InMemoryStreamNetwork network, BluetoothEndPoint address)
	: IBluetoothProvider
{
	private readonly InMemoryStreamNetwork _network = network;

	public BluetoothEndPoint Address { get; } = address;

	public BluetoothEndPoint? Host { get; init; }

	public int ChooseHostCount { get; private set; }

	public IDatagramTransport CreateTransport(BluetoothEndPoint localEndPoint) =>
		_network.CreateTransport(Address);

	public BluetoothEndPoint? ChooseHost()
	{
		ChooseHostCount++;
		return Host;
	}
}
