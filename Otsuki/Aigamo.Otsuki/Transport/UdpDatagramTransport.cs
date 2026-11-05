using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;

namespace Aigamo.Otsuki.Transport;

/// <summary>
/// An IPv4 UDP socket, which is what the DirectPlay 8 TCP/IP service provider uses; or several, bound to the same
/// address and port, so that the threads that send do not wait for each other on one socket in the kernel.
/// </summary>
/// <remarks>
/// With several sockets, a send goes out on the socket that a hash of the remote endpoint chooses, the hash by which
/// <see cref="PeerOptions.ConnectionLoops"/> chooses a connection's loop: with as many sockets as loops, each loop
/// sends on a socket of its own. Every socket receives: Linux spreads the remote endpoints over them, macOS gives all
/// to one. Windows has no such sharing of a port, and takes one socket.
/// </remarks>
public sealed class UdpDatagramTransport : IDatagramTransport
{
	// Stops Windows from reporting ICMP port unreachable as a failure of the next receive.
	private const int SioUdpConnectionReset = -1744830452;

	private const int MaxDatagramSize = 65535;

	private readonly ImmutableArray<Socket> _sockets;
	private readonly CancellationTokenSource _cancellation = new();

	// SOL_SOCKET and SO_REUSEPORT, which .NET does not name, where the system lets sockets share a port.
	private static (int Level, int Name)? ReusePort =>
		OperatingSystem.IsLinux() ? (1, 15)
		: OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsFreeBSD()
			? (0xffff, 0x0200)
		: null;

	private static Socket Bind(IPEndPoint localEndPoint, bool sharesPort)
	{
		var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		try
		{
			if (OperatingSystem.IsWindows())
				socket.IOControl(SioUdpConnectionReset, [0, 0, 0, 0], null);

			if (sharesPort && ReusePort is var (level, name))
				socket.SetRawSocketOption(level, name, BitConverter.GetBytes(1));

			// Host enumeration can query a broadcast address.
			socket.EnableBroadcast = true;
			socket.Bind(localEndPoint);
			return socket;
		}
		catch
		{
			socket.Dispose();
			throw;
		}
	}

	public UdpDatagramTransport(IPEndPoint localEndPoint, int sockets = 1)
	{
		var count = ReusePort is null ? 1 : Math.Max(1, sockets);
		var first = Bind(localEndPoint, sharesPort: count > 1);
		var builder = ImmutableArray.CreateBuilder<Socket>(count);
		builder.Add(first);
		try
		{
			// The others take the first's port, which the system chose if the local endpoint gave none.
			for (var i = 1; i < count; i++)
				builder.Add(Bind((IPEndPoint)first.LocalEndPoint!, sharesPort: true));
		}
		catch
		{
			foreach (var socket in builder)
				socket.Dispose();
			throw;
		}

		_sockets = builder.MoveToImmutable();
	}

	public EndPoint LocalEndPoint => _sockets[0].LocalEndPoint!;

	private static async Task ReceiveLoopAsync(
		Socket socket,
		Action<EndPoint, byte[]> onReceive,
		CancellationToken cancellationToken
	)
	{
		var buffer = new byte[MaxDatagramSize];
		var any = new IPEndPoint(IPAddress.Any, 0);
		while (!cancellationToken.IsCancellationRequested)
		{
			SocketReceiveFromResult result;
			try
			{
				result = await socket
					.ReceiveFromAsync(buffer, SocketFlags.None, any, cancellationToken)
					.ConfigureAwait(false);
			}
			catch (SocketException e)
				when (e.SocketErrorCode is SocketError.ConnectionReset or SocketError.MessageSize)
			{
				continue;
			}
			catch (Exception e)
				when (e is OperationCanceledException or ObjectDisposedException or SocketException)
			{
				return;
			}

			onReceive(result.RemoteEndPoint, buffer.AsSpan(0, result.ReceivedBytes).ToArray());
		}
	}

	public void Start(Action<EndPoint, byte[]> onReceive)
	{
		foreach (var socket in _sockets)
			_ = ReceiveLoopAsync(socket, onReceive, _cancellation.Token);
	}

	public void Send(EndPoint remoteEndPoint, byte[] datagram)
	{
		var socket =
			_sockets.Length == 1
				? _sockets[0]
				: _sockets[(remoteEndPoint.GetHashCode() & int.MaxValue) % _sockets.Length];
		try
		{
			socket.SendTo(datagram, remoteEndPoint);
		}
		catch (Exception e) when (e is SocketException or ObjectDisposedException)
		{
			// Datagrams are best effort; the reliable protocol retries.
		}
	}

	public void Dispose()
	{
		_cancellation.Cancel();
		foreach (var socket in _sockets)
			socket.Dispose();
	}
}
