using System.Net;
using System.Net.Sockets;

namespace Aigamo.Otsuki.Transport;

/// <summary>
/// An IPv4 UDP socket, which is what the DirectPlay 8 TCP/IP service provider uses.
/// </summary>
public sealed class UdpDatagramTransport : IDatagramTransport
{
	// Stops Windows from reporting ICMP port unreachable as a failure of the next receive.
	private const int SioUdpConnectionReset = -1744830452;

	private const int MaxDatagramSize = 65535;

	private readonly Socket _socket;
	private readonly CancellationTokenSource _cancellation = new();

	public UdpDatagramTransport(IPEndPoint localEndPoint)
	{
		_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		try
		{
			if (OperatingSystem.IsWindows())
				_socket.IOControl(SioUdpConnectionReset, [0, 0, 0, 0], null);

			// Host enumeration can query a broadcast address.
			_socket.EnableBroadcast = true;
			_socket.Bind(localEndPoint);
		}
		catch
		{
			_socket.Dispose();
			throw;
		}
	}

	public EndPoint LocalEndPoint => _socket.LocalEndPoint!;

	private async Task ReceiveLoopAsync(
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
				result = await _socket
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

	public void Start(Action<EndPoint, byte[]> onReceive) =>
		_ = ReceiveLoopAsync(onReceive, _cancellation.Token);

	public void Send(EndPoint remoteEndPoint, byte[] datagram)
	{
		try
		{
			_socket.SendTo(datagram, remoteEndPoint);
		}
		catch (Exception e) when (e is SocketException or ObjectDisposedException)
		{
			// Datagrams are best effort; the reliable protocol retries.
		}
	}

	public void Dispose()
	{
		_cancellation.Cancel();
		_socket.Dispose();
	}
}
