using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Aigamo.Otsuki.Transport;

namespace Aigamo.Otsuki.Bluetooth;

/// <summary>
/// Bluetooth RFCOMM channels on Windows, through Winsock's AF_BTH sockets. Starting listens on a channel that Windows
/// chooses and publishes the service in the computer's SDP server, so that remote devices find the channel; opening
/// a stream gives Windows the service's UUID, and Windows looks the channel up with SDP. The computer's controller and
/// its paired devices come from the Bluetooth APIs.
/// </summary>
/// <param name="service">The UUID of the service, which hosts publish and guests connect to.</param>
/// <param name="serviceName">The name that hosts publish the service under.</param>
[SupportedOSPlatform("windows")]
internal sealed partial class WindowsBluetoothConnector(Guid service, string serviceName)
	: IStreamConnector
{
	private const AddressFamily BluetoothAddressFamily = (AddressFamily)32; // AF_BTH
	private const ProtocolType RfcommProtocol = (ProtocolType)3; // BTHPROTO_RFCOMM
	private const uint AnyPort = uint.MaxValue; // BT_PORT_ANY
	private const int MaxNameLength = 248; // BLUETOOTH_MAX_NAME_SIZE
	private const int BluetoothNameSpace = 16; // NS_BTH
	private const int RegisterService = 0; // RNRSERVICE_REGISTER
	private const int DeleteService = 2; // RNRSERVICE_DELETE
	private const int Success = 0; // ERROR_SUCCESS

	private const string BluetoothLibrary = "bluetoothapis.dll";
	private const string WinsockLibrary = "ws2_32.dll";

	/// <summary>
	/// A SOCKADDR_BTH, which Winsock reads and writes for AF_BTH sockets.
	/// </summary>
	private sealed class SocketEndPoint(ulong address, Guid service, uint port) : EndPoint
	{
		// SOCKADDR_BTH is packed: the family, the device's address, the service's UUID and the RFCOMM channel.
		private const int AddressOffset = 2;
		private const int ServiceOffset = 10;
		private const int PortOffset = 26;
		private const int Size = 30;

		public ulong Address { get; } = address;

		public Guid Service { get; } = service;

		public uint Port { get; } = port;

		public override AddressFamily AddressFamily => BluetoothAddressFamily;

		public override SocketAddress Serialize()
		{
			var socketAddress = new SocketAddress(AddressFamily, Size);
			var buffer = socketAddress.Buffer.Span;
			BitConverter.TryWriteBytes(buffer[AddressOffset..], Address);
			Service.TryWriteBytes(buffer[ServiceOffset..]);
			BitConverter.TryWriteBytes(buffer[PortOffset..], Port);
			return socketAddress;
		}

		public override EndPoint Create(SocketAddress socketAddress)
		{
			var buffer = socketAddress.Buffer.Span;
			return new SocketEndPoint(
				BitConverter.ToUInt64(buffer[AddressOffset..]),
				new Guid(buffer.Slice(ServiceOffset, 16)),
				BitConverter.ToUInt32(buffer[PortOffset..])
			);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct FindRadioParams
	{
		public int Size;
	}

	[StructLayout(LayoutKind.Sequential)]
	private unsafe struct RadioInfo
	{
		public int Size;
		public ulong Address;
		public fixed char Name[MaxNameLength];
		public uint ClassOfDevice;
		public ushort LmpSubversion;
		public ushort Manufacturer;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct DeviceSearchParams
	{
		public int Size;
		public int ReturnAuthenticated;
		public int ReturnRemembered;
		public int ReturnUnknown;
		public int ReturnConnected;
		public int IssueInquiry;
		public byte TimeoutMultiplier;
		public nint Radio;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct SystemTime
	{
		public ushort Year;
		public ushort Month;
		public ushort DayOfWeek;
		public ushort Day;
		public ushort Hour;
		public ushort Minute;
		public ushort Second;
		public ushort Milliseconds;
	}

	[StructLayout(LayoutKind.Sequential)]
	private unsafe struct DeviceInfo
	{
		public int Size;
		public ulong Address;
		public uint ClassOfDevice;
		public int Connected;
		public int Remembered;
		public int Authenticated;
		public SystemTime LastSeen;
		public SystemTime LastUsed;
		public fixed char Name[MaxNameLength];
	}

	// SOCKET_ADDRESS: a socket address and its length.
	[StructLayout(LayoutKind.Sequential)]
	private unsafe struct SocketAddressPointer
	{
		public byte* Address;
		public int Length;
	}

	// CSADDR_INFO: the local address of a service, for SDP.
	[StructLayout(LayoutKind.Sequential)]
	private struct CsAddrInfo
	{
		public SocketAddressPointer LocalAddress;
		public SocketAddressPointer RemoteAddress;
		public int SocketType;
		public int Protocol;
	}

	// WSAQUERYSETW: a service to publish.
	[StructLayout(LayoutKind.Sequential)]
	private unsafe struct QuerySet
	{
		public int Size;
		public char* ServiceInstanceName;
		public Guid* ServiceClassId;
		public nint Version;
		public char* Comment;
		public int NameSpace;
		public Guid* NameSpaceProviderId;
		public char* Context;
		public int NumberOfProtocols;
		public nint Protocols;
		public char* QueryString;
		public int NumberOfCsAddrs;
		public CsAddrInfo* CsAddrs;
		public int OutputFlags;
		public nint Blob;
	}

	private readonly Guid _service = service;
	private readonly string _serviceName = serviceName;
	private readonly CancellationTokenSource _disposing = new();
	private readonly Lock _lock = new();
	private Socket? _listener;
	private SocketEndPoint? _published;
	private bool _disposed;

	[LibraryImport(BluetoothLibrary, EntryPoint = "BluetoothFindFirstRadio", SetLastError = true)]
	private static unsafe partial nint BluetoothFindFirstRadio(
		FindRadioParams* findParams,
		nint* radio
	);

	[LibraryImport(BluetoothLibrary, EntryPoint = "BluetoothFindRadioClose")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool BluetoothFindRadioClose(nint find);

	[LibraryImport(BluetoothLibrary, EntryPoint = "BluetoothGetRadioInfo")]
	private static unsafe partial int BluetoothGetRadioInfo(nint radio, RadioInfo* info);

	[LibraryImport(BluetoothLibrary, EntryPoint = "BluetoothFindFirstDevice")]
	private static unsafe partial nint BluetoothFindFirstDevice(
		DeviceSearchParams* searchParams,
		DeviceInfo* info
	);

	[LibraryImport(BluetoothLibrary, EntryPoint = "BluetoothFindNextDevice")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static unsafe partial bool BluetoothFindNextDevice(nint find, DeviceInfo* info);

	[LibraryImport(BluetoothLibrary, EntryPoint = "BluetoothFindDeviceClose")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool BluetoothFindDeviceClose(nint find);

	[LibraryImport("kernel32.dll", EntryPoint = "CloseHandle")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool CloseHandle(nint handle);

	[LibraryImport(WinsockLibrary, EntryPoint = "WSASetServiceW", SetLastError = true)]
	private static unsafe partial int WSASetService(QuerySet* querySet, int operation, int flags);

	internal static unsafe IReadOnlyList<BluetoothDevice> GetPairedDevices()
	{
		var devices = new List<BluetoothDevice>();
		var searchParams = new DeviceSearchParams
		{
			Size = sizeof(DeviceSearchParams),
			ReturnAuthenticated = 1,
		};
		var info = new DeviceInfo { Size = sizeof(DeviceInfo) };
		// None when there is no controller, or it is off.
		var find = BluetoothFindFirstDevice(&searchParams, &info);
		if (find == 0)
			return devices;

		try
		{
			do
			{
				if (info.Authenticated == 0)
					continue;

				var endPoint = new BluetoothEndPoint(info.Address);
				var name = new string(info.Name);
				devices.Add(
					new BluetoothDevice(endPoint, name.Length > 0 ? name : endPoint.ToString())
				);
			} while (BluetoothFindNextDevice(find, &info));
		}
		finally
		{
			BluetoothFindDeviceClose(find);
		}
		return devices;
	}

	internal static unsafe BluetoothEndPoint GetLocalEndPoint()
	{
		var findParams = new FindRadioParams { Size = sizeof(FindRadioParams) };
		nint radio;
		var find = BluetoothFindFirstRadio(&findParams, &radio);
		if (find == 0)
			throw new IOException(
				$"No Bluetooth controller is available (error {Marshal.GetLastPInvokeError()})."
			);

		try
		{
			var info = new RadioInfo { Size = sizeof(RadioInfo) };
			var status = BluetoothGetRadioInfo(radio, &info);
			return status == Success
				? new BluetoothEndPoint(info.Address)
				: throw new IOException(
					$"Could not read the Bluetooth controller's address (error {status})."
				);
		}
		finally
		{
			CloseHandle(radio);
			BluetoothFindRadioClose(find);
		}
	}

	public EndPoint LocalEndPoint { get; } = GetLocalEndPoint();

	private static Socket CreateSocket() =>
		new(BluetoothAddressFamily, SocketType.Stream, RfcommProtocol);

	// Publishes the service on the listener's channel, or with DeleteService, removes it. Returns a Winsock error.
	private unsafe int SetService(SocketEndPoint channel, int operation)
	{
		var socketAddress = channel.Serialize();
		var address = socketAddress.Buffer[..socketAddress.Size].ToArray();
		var service = _service;
		fixed (byte* addressBytes = address)
		fixed (char* name = _serviceName)
		{
			var csAddr = new CsAddrInfo
			{
				LocalAddress = new SocketAddressPointer
				{
					Address = addressBytes,
					Length = address.Length,
				},
				SocketType = (int)SocketType.Stream,
				Protocol = (int)RfcommProtocol,
			};
			var querySet = new QuerySet
			{
				Size = sizeof(QuerySet),
				ServiceInstanceName = name,
				ServiceClassId = &service,
				NameSpace = BluetoothNameSpace,
				NumberOfCsAddrs = 1,
				CsAddrs = &csAddr,
			};
			return WSASetService(&querySet, operation, 0) == 0
				? Success
				: Marshal.GetLastPInvokeError();
		}
	}

	private async Task AcceptAsync(
		Socket listener,
		Action<EndPoint, Stream> onAccept,
		CancellationToken cancellationToken
	)
	{
		while (true)
		{
			Socket socket;
			try
			{
				socket = await listener.AcceptAsync(cancellationToken).ConfigureAwait(false);
			}
			catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionReset)
			{
				// The device gave up before the channel was accepted.
				continue;
			}
			catch (Exception e)
				when (e is SocketException or ObjectDisposedException or OperationCanceledException)
			{
				return;
			}

			try
			{
				var remoteEndPoint = (SocketEndPoint)socket.RemoteEndPoint!;
				onAccept(
					new BluetoothEndPoint(remoteEndPoint.Address),
					new NetworkStream(socket, ownsSocket: true)
				);
			}
			catch (Exception)
			{
				socket.Dispose();
			}
		}
	}

	public void Start(Action<EndPoint, Stream> onAccept)
	{
		var listener = CreateSocket();
		SocketEndPoint channel;
		try
		{
			listener.Bind(new SocketEndPoint(0, Guid.Empty, AnyPort));
			listener.Listen();
			channel = (SocketEndPoint)listener.LocalEndPoint!;
		}
		catch (SocketException e)
		{
			listener.Dispose();
			throw new IOException(
				$"Could not listen for Bluetooth channels ({e.SocketErrorCode}).",
				e
			);
		}

		var status = SetService(channel, RegisterService);
		if (status != Success)
		{
			listener.Dispose();
			throw new IOException(
				$"Could not publish the Bluetooth service {_serviceName} (Winsock error {status})."
			);
		}

		lock (_lock)
		{
			if (_disposed)
			{
				SetService(channel, DeleteService);
				listener.Dispose();
				throw new ObjectDisposedException(nameof(WindowsBluetoothConnector));
			}

			_listener = listener;
			_published = channel;
		}
		_ = AcceptAsync(listener, onAccept, _disposing.Token);
	}

	public async Task<Stream> ConnectAsync(
		EndPoint remoteEndPoint,
		CancellationToken cancellationToken
	)
	{
		if (remoteEndPoint is not BluetoothEndPoint bluetooth)
			throw new ArgumentException(
				$"{remoteEndPoint} is not a Bluetooth endpoint.",
				nameof(remoteEndPoint)
			);

		using var connecting = CancellationTokenSource.CreateLinkedTokenSource(
			cancellationToken,
			_disposing.Token
		);
		var socket = CreateSocket();
		try
		{
			// ConnectAsync needs the socket bound, and .NET binds only IP sockets itself. Channel 0 takes none.
			socket.Bind(new SocketEndPoint(0, Guid.Empty, 0));
			// Without a channel, Windows looks up the service's channel on the device with SDP.
			await socket
				.ConnectAsync(new SocketEndPoint(bluetooth.Address, _service, 0), connecting.Token)
				.ConfigureAwait(false);
			return new NetworkStream(socket, ownsSocket: true);
		}
		catch (Exception e)
		{
			socket.Dispose();
			cancellationToken.ThrowIfCancellationRequested();
			ObjectDisposedException.ThrowIf(_disposing.IsCancellationRequested, this);
			if (e is SocketException socketException)
				throw new IOException(
					$"Could not open a Bluetooth channel to {bluetooth} ({socketException.SocketErrorCode}).",
					e
				);
			throw;
		}
	}

	public void Dispose()
	{
		Socket? listener;
		SocketEndPoint? published;
		lock (_lock)
		{
			if (_disposed)
				return;

			_disposed = true;
			listener = _listener;
			published = _published;
		}
		_disposing.Cancel();
		if (published is not null)
			SetService(published, DeleteService);
		listener?.Dispose();
	}
}
