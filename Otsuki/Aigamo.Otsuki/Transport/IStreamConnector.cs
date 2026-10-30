using System.Net;

namespace Aigamo.Otsuki.Transport;

/// <summary>
/// Opens reliable, ordered byte streams to remote endpoints and accepts the ones they open, such as Bluetooth
/// RFCOMM channels. <see cref="StreamDatagramTransport"/> carries datagrams over them.
/// </summary>
public interface IStreamConnector : IDisposable
{
	EndPoint LocalEndPoint { get; }

	/// <summary>
	/// Starts accepting streams. <paramref name="onAccept"/> is called with the remote endpoint of each, and must not
	/// block. It owns the stream from then on.
	/// </summary>
	void Start(Action<EndPoint, Stream> onAccept);

	/// <summary>
	/// Opens a stream to <paramref name="remoteEndPoint"/>, which the caller owns. Fails if the remote endpoint does
	/// not accept it.
	/// </summary>
	Task<Stream> ConnectAsync(EndPoint remoteEndPoint, CancellationToken cancellationToken);
}
