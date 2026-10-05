using System.Net;

namespace Aigamo.Otsuki.Transport;

/// <summary>
/// An unreliable datagram transport, such as a UDP socket. Implementations must allow
/// <see cref="Send"/> to be called concurrently from multiple threads.
/// </summary>
public interface IDatagramTransport : IDisposable
{
	IPEndPoint LocalEndPoint { get; }

	/// <summary>
	/// Starts receiving. <paramref name="onReceive"/> is called for each datagram and must not block.
	/// </summary>
	void Start(Action<IPEndPoint, byte[]> onReceive);

	/// <summary>
	/// Sends a datagram on a best-effort basis. Failures are silently ignored, like packet loss.
	/// </summary>
	void Send(IPEndPoint remoteEndPoint, byte[] datagram);
}
