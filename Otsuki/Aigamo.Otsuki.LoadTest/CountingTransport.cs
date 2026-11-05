using System.Net;
using Aigamo.Otsuki.Actors;
using Aigamo.Otsuki.Transport;

namespace Aigamo.Otsuki.LoadTest;

/// <summary>
/// The datagrams that every transport of a process sends and receives, counted from whichever threads send and
/// receive.
/// </summary>
internal sealed class DatagramCounts
{
	public Tally Sent { get; } = new();

	public Tally Received { get; } = new();

	public Tally BytesSent { get; } = new();

	public Tally BytesReceived { get; } = new();
}

/// <summary>
/// A transport that counts what passes through <paramref name="inner"/>.
/// </summary>
internal sealed class CountingTransport(IDatagramTransport inner, DatagramCounts counts)
	: IDatagramTransport
{
	private readonly IDatagramTransport _inner = inner;
	private readonly DatagramCounts _counts = counts;

	public EndPoint LocalEndPoint => _inner.LocalEndPoint;

	public void Start(Action<EndPoint, byte[]> onReceive) =>
		_inner.Start(
			(remoteEndPoint, datagram) =>
			{
				_counts.Received.Add(1);
				_counts.BytesReceived.Add(datagram.Length);
				onReceive(remoteEndPoint, datagram);
			}
		);

	public void Send(EndPoint remoteEndPoint, byte[] datagram)
	{
		_counts.Sent.Add(1);
		_counts.BytesSent.Add(datagram.Length);
		_inner.Send(remoteEndPoint, datagram);
	}

	public void Dispose() => _inner.Dispose();
}
