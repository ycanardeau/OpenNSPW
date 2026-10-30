using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Aigamo.Otsuki.Transport;

namespace Aigamo.Otsuki.Bluetooth;

/// <summary>
/// Bluetooth RFCOMM channels on macOS, through IOBluetooth and the native library built from
/// Native/OtsukiBluetooth.m. Starting publishes the service in the computer's SDP server, so that remote devices find
/// its channel. The native library calls back on a thread of its own, so the callbacks only hand data on.
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed unsafe partial class MacBluetoothConnector : IStreamConnector
{
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
	private readonly Lock _lock = new();
	private readonly Dictionary<long, RfcommChannelStream> _streams = [];
	private readonly Dictionary<
		long,
		(BluetoothEndPoint, TaskCompletionSource<Stream>)
	> _requests = [];
	private nint _host;
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
		lock (_lock)
			_streams.Add(channel, stream);
		return stream;
	}

	internal void Write(long channel, ReadOnlySpan<byte> data)
	{
		lock (_lock)
		{
			if (_host == 0 || !_streams.ContainsKey(channel))
				return;

			fixed (byte* bytes = data)
				NativeWrite(_host, channel, bytes, data.Length);
		}
	}

	internal void Close(long channel)
	{
		lock (_lock)
		{
			if (_host != 0 && _streams.Remove(channel))
				NativeClose(_host, channel);
		}
	}

	private void OnAccepted(long channel, BluetoothEndPoint remoteEndPoint)
	{
		var stream = AddStream(channel);
		if (Volatile.Read(ref _onAccept) is { } onAccept)
			onAccept(remoteEndPoint, stream);
		else
			stream.Dispose();
	}

	private void OnOpened(long request, long channel, int status)
	{
		TaskCompletionSource<Stream>? completion = null;
		BluetoothEndPoint? remoteEndPoint = null;
		lock (_lock)
		{
			if (_requests.Remove(request, out var entry))
				(remoteEndPoint, completion) = entry;
		}

		if (channel == 0)
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

	private void OnReceived(long channel, ReadOnlySpan<byte> data)
	{
		RfcommChannelStream? stream;
		lock (_lock)
			stream = _streams.GetValueOrDefault(channel);
		stream?.Receive(data);
	}

	private void OnClosed(long channel)
	{
		RfcommChannelStream? stream;
		lock (_lock)
			_streams.Remove(channel, out stream);
		stream?.End();
	}

	// The callbacks run on the native library's thread, where an exception would end the process, so they keep
	// none.

	[UnmanagedCallersOnly]
	private static void Accepted(nint context, long channel, byte* address)
	{
		try
		{
			FromContext(context)
				?.OnAccepted(
					channel,
					BluetoothEndPoint.FromBytes(new ReadOnlySpan<byte>(address, 6))
				);
		}
		catch (Exception) { }
	}

	[UnmanagedCallersOnly]
	private static void Opened(nint context, long request, long channel, int status)
	{
		try
		{
			FromContext(context)?.OnOpened(request, channel, status);
		}
		catch (Exception) { }
	}

	[UnmanagedCallersOnly]
	private static void Received(nint context, long channel, byte* data, int length)
	{
		try
		{
			FromContext(context)?.OnReceived(channel, new ReadOnlySpan<byte>(data, length));
		}
		catch (Exception) { }
	}

	[UnmanagedCallersOnly]
	private static void Closed(nint context, long channel)
	{
		try
		{
			FromContext(context)?.OnClosed(channel);
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
	}

	public void Start(Action<EndPoint, Stream> onAccept)
	{
		Volatile.Write(ref _onAccept, onAccept);
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
		var address = bluetooth.ToBytes();
		lock (_lock)
		{
			if (_host == 0)
				throw new ObjectDisposedException(nameof(MacBluetoothConnector));

			var request = ++_nextRequest;
			_requests.Add(request, (bluetooth, completion));
			var registration = cancellationToken.Register(() =>
			{
				lock (_lock)
					_requests.Remove(request);
				completion.TrySetCanceled(cancellationToken);
			});
			completion.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default);
			fixed (byte* addressBytes = address)
			fixed (byte* service = _service)
				NativeConnect(_host, request, addressBytes, service);
		}
		return completion.Task;
	}

	public void Dispose()
	{
		nint host;
		lock (_lock)
		{
			host = _host;
			_host = 0;
		}
		if (host == 0)
			return;

		// Outside the lock: the native thread may be waiting for it in a callback, and destroying waits for that
		// thread. No callback comes after.
		NativeDestroy(host);
		_handle.Free();

		RfcommChannelStream[] streams;
		TaskCompletionSource<Stream>[] completions;
		lock (_lock)
		{
			streams = [.. _streams.Values];
			completions = [.. _requests.Values.Select(entry => entry.Item2)];
			_streams.Clear();
			_requests.Clear();
		}
		foreach (var stream in streams)
			stream.End();
		foreach (var completion in completions)
			completion.TrySetException(new ObjectDisposedException(nameof(MacBluetoothConnector)));
	}
}
