using System.Net;
using Aigamo.Otsuki.Transport;

namespace Aigamo.Otsuki.Bluetooth;

/// <summary>
/// Sessions over Bluetooth RFCOMM channels. Each application publishes a service of its own, named by a UUID, and
/// peers find each other's channel by it. macOS and Windows are supported.
/// </summary>
public static class BluetoothTransport
{
	private const string NotSupported = "Bluetooth is supported on macOS and Windows only.";

	public static bool IsSupported => OperatingSystem.IsMacOS() || OperatingSystem.IsWindows();

	/// <summary>
	/// Whether channels to other devices can be opened on this version of the OS: not on macOS 26, where IOBluetooth
	/// runs over CoreBluetooth, never completes SDP queries, and opens a channel only to a device that it counts as
	/// connected, which a computer never is (docs/Bluetooth.md). Hosting still works there, with
	/// <see cref="RunMainLoopUntil"/>.
	/// </summary>
	public static bool CanConnect => !OperatingSystem.IsMacOSVersionAtLeast(26);

	/// <summary>
	/// Runs the main thread's run loop until <paramref name="task"/> completes: on macOS 26, IOBluetooth delivers its
	/// callbacks there, so an app whose main thread runs no run loop of its own, such as a console app, calls this on
	/// its main thread. Elsewhere, it only waits.
	/// </summary>
	public static void RunMainLoopUntil(Task task)
	{
		if (!OperatingSystem.IsMacOS())
		{
			((IAsyncResult)task).AsyncWaitHandle.WaitOne();
			return;
		}

		while (!task.IsCompleted)
			MacBluetoothConnector.RunMainLoop(TimeSpan.FromMilliseconds(100));
	}

	/// <summary>
	/// Whether the app may use Bluetooth: on macOS, it must declare why in its Info.plist
	/// (NSBluetoothAlwaysUsageDescription), or macOS kills it at its first Bluetooth call, before anything can catch it.
	/// Checking touches no Bluetooth. A process started from a terminal may use Bluetooth if the terminal may, which
	/// this cannot tell.
	/// </summary>
	public static bool IsUsageDescribed =>
		OperatingSystem.IsMacOS() && MacBluetoothConnector.IsUsageDescribed();

	/// <summary>
	/// Whether the app may use Bluetooth: on Windows, always; on macOS, if <see cref="IsUsageDescribed"/>.
	/// </summary>
	public static bool IsAllowed => OperatingSystem.IsWindows() || IsUsageDescribed;

	/// <summary>
	/// The address of the computer's Bluetooth controller. Fails with <see cref="IOException"/> if there is none, or it
	/// is off.
	/// </summary>
	public static BluetoothEndPoint GetLocalEndPoint() =>
		OperatingSystem.IsMacOS() ? MacBluetoothConnector.GetLocalEndPoint()
		: OperatingSystem.IsWindows() ? WindowsBluetoothConnector.GetLocalEndPoint()
		: throw new PlatformNotSupportedException(NotSupported);

	/// <summary>
	/// The devices paired with the computer.
	/// </summary>
	public static IReadOnlyList<BluetoothDevice> GetPairedDevices() =>
		OperatingSystem.IsMacOS() ? MacBluetoothConnector.GetPairedDevices()
		: OperatingSystem.IsWindows() ? WindowsBluetoothConnector.GetPairedDevices()
		: throw new PlatformNotSupportedException(NotSupported);

	/// <summary>
	/// Opens and accepts channels to <paramref name="service"/>, published under <paramref name="serviceName"/>.
	/// </summary>
	public static IStreamConnector CreateConnector(Guid service, string serviceName) =>
		OperatingSystem.IsMacOS() ? new MacBluetoothConnector(service, serviceName)
		: OperatingSystem.IsWindows() ? new WindowsBluetoothConnector(service, serviceName)
		: throw new PlatformNotSupportedException(NotSupported);

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
