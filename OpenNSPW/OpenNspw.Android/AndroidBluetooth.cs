using System.Net;
using Aigamo.Otsuki.Actors;
using Aigamo.Otsuki.Bluetooth;
using Aigamo.Otsuki.Transport;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using OpenNspw.App;
using AndroidDevice = Android.Bluetooth.BluetoothDevice;
using BluetoothDevice = Aigamo.Otsuki.Bluetooth.BluetoothDevice;

namespace OpenNspw.Android;

// An RFCOMM socket as a stream: reads from its input, writes to its output, and closing the stream closes the socket.
internal sealed class BluetoothSocketStream(BluetoothSocket socket) : Stream
{
	private readonly BluetoothSocket _socket = socket;
	private readonly Stream _input = socket.InputStream!;
	private readonly Stream _output = socket.OutputStream!;

	public override bool CanRead => true;

	public override bool CanSeek => false;

	public override bool CanWrite => true;

	public override long Length => throw new NotSupportedException();

	public override long Position
	{
		get => throw new NotSupportedException();
		set => throw new NotSupportedException();
	}

	public override int Read(byte[] buffer, int offset, int count) =>
		_input.Read(buffer, offset, count);

	public override void Write(byte[] buffer, int offset, int count) =>
		_output.Write(buffer, offset, count);

	public override void Flush() => _output.Flush();

	public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

	public override void SetLength(long value) => throw new NotSupportedException();

	protected override void Dispose(bool disposing)
	{
		if (disposing)
			_socket.Close();
		base.Dispose(disposing);
	}
}

// Bluetooth RFCOMM channels on Android. Starting listens for the service under its UUID, which Android publishes in the
// device's SDP server; opening a stream gives Android the service's UUID, and Android looks the channel up with SDP, as
// Windows does (docs/Bluetooth.md). Android's sockets block, so accepting and connecting run on the thread pool. The
// connector is an actor that owns the listening socket, which starting and disposing hand it.
internal sealed class AndroidBluetoothConnector
	: Actor<AndroidBluetoothConnector.Message>,
		IStreamConnector
{
	internal abstract record Message
	{
		private Message() { }

		public sealed record Listening(BluetoothServerSocket Listener) : Message;

		public sealed record Dispose : Message;
	}

	private readonly BluetoothAdapter _adapter;
	private readonly Java.Util.UUID _service;
	private readonly string _serviceName;
	private readonly CancellationTokenSource _disposing = new();
	private BluetoothServerSocket? _listener;
	private bool _disposed;

	// Android hides the device's own address, so the connector has none of its own.
	public EndPoint LocalEndPoint => BluetoothEndPoint.Any;

	// service: the UUID of the service, which hosts publish and guests connect to. serviceName: the name that hosts
	// publish the service under.
	public AndroidBluetoothConnector(BluetoothAdapter adapter, Guid service, string serviceName)
	{
		_adapter = adapter;
		_service = Java.Util.UUID.FromString(service.ToString())!;
		_serviceName = serviceName;
		Start();
	}

	private static void Accept(
		BluetoothServerSocket listener,
		Action<EndPoint, Stream> onAccept,
		CancellationToken cancellationToken
	)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			BluetoothSocket socket;
			try
			{
				socket = listener.Accept()!;
			}
			catch (Java.IO.IOException)
			{
				// The listener was closed.
				return;
			}

			try
			{
				onAccept(
					BluetoothEndPoint.Parse(socket.RemoteDevice!.Address!),
					new BluetoothSocketStream(socket)
				);
			}
			catch (Exception)
			{
				socket.Close();
			}
		}
	}

	public void Start(Action<EndPoint, Stream> onAccept)
	{
		BluetoothServerSocket listener;
		try
		{
			listener = _adapter.ListenUsingRfcommWithServiceRecord(_serviceName, _service)!;
		}
		catch (Exception e) when (e is Java.IO.IOException or Java.Lang.SecurityException)
		{
			throw new IOException($"Could not listen for Bluetooth channels: {e.Message}", e);
		}

		if (!Post(new Message.Listening(listener)))
		{
			listener.Close();
			throw new ObjectDisposedException(nameof(AndroidBluetoothConnector));
		}

		var disposing = _disposing.Token;
		_ = Task.Run(() => Accept(listener, onAccept, disposing));
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
		BluetoothSocket socket;
		try
		{
			socket = _adapter
				.GetRemoteDevice(bluetooth.ToString())!
				.CreateRfcommSocketToServiceRecord(_service)!;
		}
		catch (Exception e) when (e is Java.IO.IOException or Java.Lang.SecurityException)
		{
			throw new IOException(
				$"Could not open a Bluetooth channel to {bluetooth}: {e.Message}",
				e
			);
		}

		// Closing the socket ends a connection that is still being opened.
		using var closing = connecting.Token.Register(socket.Close);
		try
		{
			await Task.Run(socket.Connect, CancellationToken.None).ConfigureAwait(false);
			return new BluetoothSocketStream(socket);
		}
		catch (Exception e)
		{
			socket.Close();
			cancellationToken.ThrowIfCancellationRequested();
			ObjectDisposedException.ThrowIf(_disposing.IsCancellationRequested, this);
			if (e is Java.IO.IOException or Java.Lang.SecurityException)
				throw new IOException(
					$"Could not open a Bluetooth channel to {bluetooth}: {e.Message}",
					e
				);
			throw;
		}
	}

	protected override void Receive(Message message)
	{
		switch (message)
		{
			case Message.Listening listening:
				_listener = listening.Listener;
				if (_disposed)
					_listener.Close();
				break;
			case Message.Dispose when !_disposed:
				_disposed = true;
				_listener?.Close();
				Stop();
				break;
		}
	}

	public void Dispose()
	{
		_disposing.Cancel();
		Post(new Message.Dispose());
	}
}

// The device's Bluetooth on Android, in `activity`. Android hides the device's own address, so a host shows its name,
// which guests see among their paired devices. From Android 12 on, the app asks the user once, the first time it uses
// Bluetooth, for leave to connect to devices (BLUETOOTH_CONNECT); until the user allows it, there is no name and no
// paired device.
internal sealed class AndroidBluetooth(Activity activity, BluetoothAdapter adapter)
	: IBluetoothRadio
{
	private const int PermissionRequest = 1;

	private readonly Activity _activity = activity;
	private readonly BluetoothAdapter _adapter = adapter;

	// Whether the app has asked the user for leave to use Bluetooth, which it does once.
	private bool _hasAsked;

	// The device's Bluetooth, or null if it has none.
	public static AndroidBluetooth? Create(Activity activity)
	{
		return
			activity.GetSystemService(Context.BluetoothService)
				is BluetoothManager { Adapter: { } adapter }
			? new AndroidBluetooth(activity, adapter)
			: null;
	}

	// Whether the app may connect to devices: always before Android 12, where the manifest's permissions suffice; from
	// then on, once the user allows it, which the first call asks.
	private bool MayConnect()
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(31))
			return true;
		if (
			_activity.CheckSelfPermission(global::Android.Manifest.Permission.BluetoothConnect)
			== Permission.Granted
		)
			return true;

		if (!_hasAsked)
		{
			_hasAsked = true;
			_activity.RequestPermissions(
				[global::Android.Manifest.Permission.BluetoothConnect],
				PermissionRequest
			);
		}

		return false;
	}

	public BluetoothEndPoint GetLocalEndPoint()
	{
		throw new IOException("Android hides the device's Bluetooth address.");
	}

	public string? GetLocalName()
	{
		return MayConnect() && _adapter.IsEnabled ? _adapter.Name : null;
	}

	public IReadOnlyList<BluetoothDevice> GetPairedDevices()
	{
		if (!MayConnect() || !_adapter.IsEnabled)
			return [];

		return
		[
			.. (_adapter.BondedDevices ?? [])
				.Select(d =>
					(
						Device: d,
						EndPoint: BluetoothEndPoint.TryParse(d.Address ?? "", out var e) ? e : null
					)
				)
				.Where(d => d.EndPoint is not null)
				.Select(d => new BluetoothDevice(
					d.EndPoint!,
					string.IsNullOrEmpty(d.Device.Name) ? d.EndPoint!.ToString() : d.Device.Name
				)),
		];
	}

	public IStreamConnector CreateConnector(Guid service, string serviceName)
	{
		return new AndroidBluetoothConnector(_adapter, service, serviceName);
	}
}
