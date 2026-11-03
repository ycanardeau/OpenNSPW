using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Aigamo.Otsuki.Actors;
using Aigamo.Otsuki.Transport;

namespace Aigamo.Otsuki.Bluetooth;

/// <summary>
/// Bluetooth RFCOMM channels on macOS, through IOBluetooth and the native library built from
/// Native/OtsukiBluetooth.m. Starting publishes the service in the computer's SDP server, so that remote devices find
/// its channel. The native library calls back on a thread of its own, so the callbacks only post what happened to the
/// connector, an actor that owns the streams and the requests.
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed unsafe partial class MacBluetoothConnector
	: Actor<MacBluetoothConnector.Message>,
		IStreamConnector
{
	internal abstract record Message
	{
		private Message() { }

		public sealed record Started(Action<EndPoint, Stream> OnAccept) : Message;

		public sealed record Connect(
			BluetoothEndPoint RemoteEndPoint,
			TaskCompletionSource<Stream> Completion,
			CancellationToken CancellationToken
		) : Message;

		public sealed record Cancel(long Request) : Message;

		public sealed record Write(long Channel, byte[] Data) : Message;

		public sealed record Close(long Channel) : Message;

		public sealed record Accepted(long Channel, BluetoothEndPoint RemoteEndPoint) : Message;

		public sealed record Opened(long Request, long Channel, int Status) : Message;

		public sealed record Received(long Channel, byte[] Data) : Message;

		public sealed record Closed(long Channel) : Message;

		public sealed record Dispose : Message;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct Callbacks
	{
		public delegate* unmanaged<nint, long, byte*, void> Accepted;
		public delegate* unmanaged<nint, long, long, int, void> Opened;
		public delegate* unmanaged<nint, long, byte*, int, void> Received;
		public delegate* unmanaged<nint, long, void> Closed;
	}

	private const string Library = "OtsukiBluetooth";

	// IOReturn's kIOReturnSuccess.
	private const int Success = 0;

	private readonly byte[] _service;
	private readonly string _serviceName;
	private readonly GCHandle _handle;
	private readonly nint _host;
	private readonly Dictionary<long, RfcommChannelStream> _streams = [];
	private readonly Dictionary<
		long,
		(BluetoothEndPoint, TaskCompletionSource<Stream>)
	> _requests = [];
	private bool _isDestroyed;
	private long _nextRequest;
	private Action<EndPoint, Stream>? _onAccept;

	[LibraryImport(Library, EntryPoint = "otsuki_bluetooth_create")]
	private static partial nint NativeCreate(Callbacks callbacks, nint context);

	[LibraryImport(Library, EntryPoint = "otsuki_bluetooth_destroy")]
	private static partial void NativeDestroy(nint host);

	[LibraryImport(Library, EntryPoint = "otsuki_bluetooth_usage_described")]
	private static partial int NativeUsageDescribed();

	[LibraryImport(Library, EntryPoint = "otsuki_bluetooth_paired_devices")]
	private static partial void NativePairedDevices(
		delegate* unmanaged<nint, byte*, byte*, void> device,
		nint context
	);

	[LibraryImport(Library, EntryPoint = "otsuki_bluetooth_local_address")]
	private static partial int NativeLocalAddress(byte* address);

	[LibraryImport(
		Library,
		EntryPoint = "otsuki_bluetooth_listen",
		StringMarshalling = StringMarshalling.Utf8
	)]
	private static partial int NativeListen(nint host, byte* service, string name);

	[LibraryImport(Library, EntryPoint = "otsuki_bluetooth_connect")]
	private static partial void NativeConnect(
		nint host,
		long request,
		byte* address,
		byte* service
	);

	[LibraryImport(Library, EntryPoint = "otsuki_bluetooth_write")]
	private static partial void NativeWrite(nint host, long channel, byte* data, int length);

	[LibraryImport(Library, EntryPoint = "otsuki_bluetooth_close")]
	private static partial void NativeClose(nint host, long channel);

	[LibraryImport(Library, EntryPoint = "otsuki_bluetooth_run_main_loop")]
	private static partial int NativeRunMainLoop(double seconds);

	public EndPoint LocalEndPoint { get; }

	// Whether the app declares why it uses Bluetooth, without which macOS kills it at its first Bluetooth call. False
	// when the native library is missing, as in a build made on another system.
	internal static bool IsUsageDescribed()
	{
		try
		{
			return NativeUsageDescribed() != 0;
		}
		catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
		{
			return false;
		}
	}

	// Runs the main thread's run loop until something happens on it, or for at most the given time, so that IOBluetooth
	// can deliver callbacks there, as on macOS 26.
	internal static void RunMainLoop(TimeSpan timeout)
	{
		if (NativeRunMainLoop(timeout.TotalSeconds) != Success)
			throw new InvalidOperationException("The main run loop runs only on the main thread.");
	}

	[UnmanagedCallersOnly]
	private static void AddPairedDevice(nint context, byte* address, byte* name)
	{
		try
		{
			if (GCHandle.FromIntPtr(context).Target is List<BluetoothDevice> devices)
				devices.Add(
					new BluetoothDevice(
						BluetoothEndPoint.FromBytes(new ReadOnlySpan<byte>(address, 6)),
						Marshal.PtrToStringUTF8((nint)name) ?? ""
					)
				);
		}
		catch (Exception) { }
	}

	internal static IReadOnlyList<BluetoothDevice> GetPairedDevices()
	{
		var devices = new List<BluetoothDevice>();
		var handle = GCHandle.Alloc(devices);
		try
		{
			NativePairedDevices(&AddPairedDevice, GCHandle.ToIntPtr(handle));
		}
		finally
		{
			handle.Free();
		}
		return devices;
	}

	internal static BluetoothEndPoint GetLocalEndPoint()
	{
		var address = stackalloc byte[6];
		var status = NativeLocalAddress(address);
		return status == Success
			? BluetoothEndPoint.FromBytes(new ReadOnlySpan<byte>(address, 6))
			: throw new IOException(
				$"No Bluetooth controller is available (IOReturn 0x{status:X8})."
			);
	}

	private static MacBluetoothConnector? FromContext(nint context) =>
		GCHandle.FromIntPtr(context).Target as MacBluetoothConnector;

	private RfcommChannelStream AddStream(long channel)
	{
		var stream = new RfcommChannelStream(this, channel);
		_streams.Add(channel, stream);
		return stream;
	}

	internal void Write(long channel, ReadOnlySpan<byte> data) =>
		Post(new Message.Write(channel, data.ToArray()));

	internal void Close(long channel) => Post(new Message.Close(channel));

	private void OnWrite(long channel, byte[] data)
	{
		if (_isDestroyed || !_streams.ContainsKey(channel))
			return;

		fixed (byte* bytes = data)
			NativeWrite(_host, channel, bytes, data.Length);
	}

	private void OnClose(long channel)
	{
		if (!_isDestroyed && _streams.Remove(channel))
			NativeClose(_host, channel);
	}

	private void OnConnect(
		BluetoothEndPoint remoteEndPoint,
		TaskCompletionSource<Stream> completion,
		CancellationToken cancellationToken
	)
	{
		if (_isDestroyed)
		{
			completion.TrySetException(new ObjectDisposedException(nameof(MacBluetoothConnector)));
			return;
		}

		// A connect cancelled before its turn is not sent.
		if (cancellationToken.IsCancellationRequested)
		{
			completion.TrySetCanceled(cancellationToken);
			return;
		}

		var request = ++_nextRequest;
		_requests.Add(request, (remoteEndPoint, completion));
		var registration = cancellationToken.Register(() =>
		{
			Post(new Message.Cancel(request));
			completion.TrySetCanceled(cancellationToken);
		});
		completion.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default);
		var address = remoteEndPoint.ToBytes();
		fixed (byte* addressBytes = address)
		fixed (byte* service = _service)
			NativeConnect(_host, request, addressBytes, service);
	}

	private void OnAccepted(long channel, BluetoothEndPoint remoteEndPoint)
	{
		if (_isDestroyed)
			return;

		var stream = AddStream(channel);
		if (_onAccept is { } onAccept)
			onAccept(remoteEndPoint, stream);
		else
			stream.Dispose();
	}

	private void OnOpened(long request, long channel, int status)
	{
		TaskCompletionSource<Stream>? completion = null;
		BluetoothEndPoint? remoteEndPoint = null;
		if (_requests.Remove(request, out var entry))
			(remoteEndPoint, completion) = entry;

		if (channel == 0 || _isDestroyed)
		{
			completion?.TrySetException(
				new IOException(
					$"Could not open a Bluetooth channel to {remoteEndPoint} (IOReturn 0x{status:X8})."
				)
			);
			return;
		}

		var stream = AddStream(channel);
		if (completion is null || !completion.TrySetResult(stream))
			stream.Dispose();
	}

	private void OnReceived(long channel, byte[] data)
	{
		_streams.GetValueOrDefault(channel)?.Receive(data);
	}

	private void OnClosed(long channel)
	{
		if (_streams.Remove(channel, out var stream))
			stream.End();
	}

	// Destroying waits for the native thread, which only posts to the connector: no callback comes after.
	private void OnDispose()
	{
		if (_isDestroyed)
			return;

		_isDestroyed = true;
		NativeDestroy(_host);
		_handle.Free();
		foreach (var stream in _streams.Values)
			stream.End();
		foreach (var (_, completion) in _requests.Values)
			completion.TrySetException(new ObjectDisposedException(nameof(MacBluetoothConnector)));
		_streams.Clear();
		_requests.Clear();
		Stop();
	}

	protected override void Receive(Message message)
	{
		switch (message)
		{
			case Message.Started started:
				_onAccept = started.OnAccept;
				break;
			case Message.Connect connect:
				OnConnect(connect.RemoteEndPoint, connect.Completion, connect.CancellationToken);
				break;
			case Message.Cancel cancel:
				_requests.Remove(cancel.Request);
				break;
			case Message.Write write:
				OnWrite(write.Channel, write.Data);
				break;
			case Message.Close close:
				OnClose(close.Channel);
				break;
			case Message.Accepted accepted:
				OnAccepted(accepted.Channel, accepted.RemoteEndPoint);
				break;
			case Message.Opened opened:
				OnOpened(opened.Request, opened.Channel, opened.Status);
				break;
			case Message.Received received:
				OnReceived(received.Channel, received.Data);
				break;
			case Message.Closed closed:
				OnClosed(closed.Channel);
				break;
			case Message.Dispose:
				OnDispose();
				break;
		}
	}

	// The callbacks run on the native library's thread, where an exception would end the process, so they keep
	// none.

	[UnmanagedCallersOnly]
	private static void Accepted(nint context, long channel, byte* address)
	{
		try
		{
			FromContext(context)
				?.Post(
					new Message.Accepted(
						channel,
						BluetoothEndPoint.FromBytes(new ReadOnlySpan<byte>(address, 6))
					)
				);
		}
		catch (Exception) { }
	}

	[UnmanagedCallersOnly]
	private static void Opened(nint context, long request, long channel, int status)
	{
		try
		{
			FromContext(context)?.Post(new Message.Opened(request, channel, status));
		}
		catch (Exception) { }
	}

	[UnmanagedCallersOnly]
	private static void Received(nint context, long channel, byte* data, int length)
	{
		try
		{
			FromContext(context)
				?.Post(new Message.Received(channel, new ReadOnlySpan<byte>(data, length).ToArray()));
		}
		catch (Exception) { }
	}

	[UnmanagedCallersOnly]
	private static void Closed(nint context, long channel)
	{
		try
		{
			FromContext(context)?.Post(new Message.Closed(channel));
		}
		catch (Exception) { }
	}

	public MacBluetoothConnector(Guid service, string serviceName)
	{
		_service = service.ToByteArray(bigEndian: true);
		_serviceName = serviceName;
		LocalEndPoint = GetLocalEndPoint();
		_handle = GCHandle.Alloc(this, GCHandleType.Weak);
		_host = NativeCreate(
			new Callbacks
			{
				Accepted = &Accepted,
				Opened = &Opened,
				Received = &Received,
				Closed = &Closed,
			},
			GCHandle.ToIntPtr(_handle)
		);
		Start();
	}

	// Listens on the caller's thread, as IOBluetooth may want, and throws if it cannot.
	public void Start(Action<EndPoint, Stream> onAccept)
	{
		Post(new Message.Started(onAccept));
		int status;
		fixed (byte* service = _service)
			status = NativeListen(_host, service, _serviceName);
		if (status != Success)
			throw new IOException(
				$"Could not publish the Bluetooth service {_serviceName} (IOReturn 0x{status:X8})."
			);
	}

	public Task<Stream> ConnectAsync(EndPoint remoteEndPoint, CancellationToken cancellationToken)
	{
		if (remoteEndPoint is not BluetoothEndPoint bluetooth)
			throw new ArgumentException(
				$"{remoteEndPoint} is not a Bluetooth endpoint.",
				nameof(remoteEndPoint)
			);

		// Continuations must not run on the native library's thread, which completes the task.
		var completion = new TaskCompletionSource<Stream>(
			TaskCreationOptions.RunContinuationsAsynchronously
		);
		if (!Post(new Message.Connect(bluetooth, completion, cancellationToken)))
			throw new ObjectDisposedException(nameof(MacBluetoothConnector));

		return completion.Task;
	}

	public void Dispose() => Post(new Message.Dispose());
}
