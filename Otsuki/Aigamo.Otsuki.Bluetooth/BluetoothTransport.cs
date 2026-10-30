using System.Net;
using Aigamo.Otsuki.Transport;

namespace Aigamo.Otsuki.Bluetooth;

/// <summary>
/// Sessions over Bluetooth RFCOMM channels. Each application publishes a service of its own, named by a UUID, and
/// peers find each other's channel by it. Only macOS is supported so far.
/// </summary>
public static class BluetoothTransport
{
	public static bool IsSupported => OperatingSystem.IsMacOS();

	/// <summary>
	/// Whether the app may use Bluetooth: on macOS, it must declare why in its Info.plist
	/// (NSBluetoothAlwaysUsageDescription), or macOS kills it at its first Bluetooth call, before anything can catch it.
	/// Checking touches no Bluetooth. A process started from a terminal may use Bluetooth if the terminal may, which
	/// this cannot tell.
	/// </summary>
	public static bool IsUsageDescribed =>
		OperatingSystem.IsMacOS() && MacBluetoothConnector.IsUsageDescribed();

	/// <summary>
	/// The address of the computer's Bluetooth controller. Fails with <see cref="IOException"/> if there is none, or it
	/// is off.
	/// </summary>
	public static BluetoothEndPoint GetLocalEndPoint() =>
		OperatingSystem.IsMacOS()
			? MacBluetoothConnector.GetLocalEndPoint()
			: throw new PlatformNotSupportedException("Bluetooth is supported on macOS only.");

	/// <summary>
	/// The devices paired with the computer.
	/// </summary>
	public static IReadOnlyList<BluetoothDevice> GetPairedDevices() =>
		OperatingSystem.IsMacOS()
			? MacBluetoothConnector.GetPairedDevices()
			: throw new PlatformNotSupportedException("Bluetooth is supported on macOS only.");

	/// <summary>
	/// Opens and accepts channels to <paramref name="service"/>, published under <paramref name="serviceName"/>.
	/// </summary>
	public static IStreamConnector CreateConnector(Guid service, string serviceName) =>
		OperatingSystem.IsMacOS()
			? new MacBluetoothConnector(service, serviceName)
			: throw new PlatformNotSupportedException("Bluetooth is supported on macOS only.");

	/// <summary>
	/// A <see cref="PeerOptions.TransportFactory"/> for Bluetooth. The local endpoint is
	/// <see cref="BluetoothEndPoint.Any"/>, or the computer's own address: the computer's Bluetooth controller is
	/// used either way.
	/// </summary>
	public static Func<EndPoint, IDatagramTransport> CreateFactory(
		Guid service,
		string serviceName
	) =>
		localEndPoint =>
			localEndPoint is BluetoothEndPoint
				? new StreamDatagramTransport(CreateConnector(service, serviceName))
				: throw new NotSupportedException(
					$"Bluetooth cannot bind to {localEndPoint}, which is not a Bluetooth endpoint."
				);
}
