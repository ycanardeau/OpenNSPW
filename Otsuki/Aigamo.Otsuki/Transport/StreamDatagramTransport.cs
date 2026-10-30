using System.Buffers.Binary;
using System.Net;
using System.Threading.Channels;

namespace Aigamo.Otsuki.Transport;

/// <summary>
/// Carries datagrams over the reliable streams of an <see cref="IStreamConnector"/>, each prefixed with its length
/// as a big-endian 16-bit number. The first <see cref="Send"/> to an endpoint opens a stream to it, and the
/// datagrams wait for it to open; streams that remote endpoints open are accepted. A stream that fails or closes is
/// dropped, with the datagrams still waiting for it, as if they were lost, and the next <see cref="Send"/> opens a
/// new one. The transport owns the connector, and disposes it.
/// </summary>
public sealed class StreamDatagramTransport(IStreamConnector connector) : IDatagramTransport
{
	/// <summary>
	/// A stream to one remote endpoint, open or still opening, with the datagrams waiting to be written to it.
	/// </summary>
	private sealed class Link(EndPoint remoteEndPoint)
	{
		// Datagrams beyond this many waiting are lost, as a full socket buffer would lose them.
		private const int MaxWaitingDatagrams = 256;

		public EndPoint RemoteEndPoint { get; } = remoteEndPoint;

		public Channel<byte[]> Datagrams { get; } =
			Channel.CreateBounded<byte[]>(
				new BoundedChannelOptions(MaxWaitingDatagrams)
				{
					FullMode = BoundedChannelFullMode.DropWrite,
					SingleReader = true,
				}
			);
	}

	private const int HeaderSize = sizeof(ushort);

	private readonly IStreamConnector _connector = connector;
	private readonly CancellationTokenSource _cancellation = new();
	private readonly Lock _lock = new();

	// The link that sends to each endpoint. A link that a remote endpoint opened while we had one to it already
	// only receives.
	private readonly Dictionary<EndPoint, Link> _links = [];

	private bool _disposed;
	private Action<EndPoint, byte[]>? _onReceive;

	public EndPoint LocalEndPoint => _connector.LocalEndPoint;

	private async Task ReceiveAsync(
		EndPoint remoteEndPoint,
		Stream stream,
		CancellationToken cancellationToken
	)
	{
		var header = new byte[HeaderSize];
		while (true)
		{
			await stream.ReadExactlyAsync(header, cancellationToken).ConfigureAwait(false);
			var datagram = new byte[BinaryPrimitives.ReadUInt16BigEndian(header)];
			await stream.ReadExactlyAsync(datagram, cancellationToken).ConfigureAwait(false);
			Volatile.Read(ref _onReceive)?.Invoke(remoteEndPoint, datagram);
		}
	}

	private static async Task WriteAsync(
		ChannelReader<byte[]> datagrams,
		Stream stream,
		CancellationToken cancellationToken
	)
	{
		await foreach (
			var datagram in datagrams.ReadAllAsync(cancellationToken).ConfigureAwait(false)
		)
		{
			var frame = new byte[HeaderSize + datagram.Length];
			BinaryPrimitives.WriteUInt16BigEndian(frame, (ushort)datagram.Length);
			datagram.CopyTo(frame, HeaderSize);
			await stream.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
			if (!datagrams.TryPeek(out _))
				await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Receives from the link's stream and writes its datagrams to it until either fails or the stream closes, then
	/// drops the link.
	/// </summary>
	private async Task RunAsync(Link link, Func<CancellationToken, Task<Stream>> open)
	{
		Stream? stream = null;
		try
		{
			stream = await open(_cancellation.Token).ConfigureAwait(false);
			using var running = CancellationTokenSource.CreateLinkedTokenSource(
				_cancellation.Token
			);
			var receiving = ReceiveAsync(link.RemoteEndPoint, stream, running.Token);
			var writing = WriteAsync(link.Datagrams.Reader, stream, running.Token);
			await Task.WhenAny(receiving, writing).ConfigureAwait(false);
			await running.CancelAsync().ConfigureAwait(false);
			await Task.WhenAll(receiving, writing).ConfigureAwait(false);
		}
		catch (Exception)
		{
			// The stream did not open, failed, closed or was cancelled. Its datagrams are lost; the reliable protocol
			// resends them.
		}
		finally
		{
			lock (_lock)
			{
				if (_links.GetValueOrDefault(link.RemoteEndPoint) == link)
					_links.Remove(link.RemoteEndPoint);
			}
			link.Datagrams.Writer.TryComplete();
			if (stream is not null)
				await stream.DisposeAsync().ConfigureAwait(false);
		}
	}

	private void Accept(EndPoint remoteEndPoint, Stream stream)
	{
		var link = new Link(remoteEndPoint);
		lock (_lock)
		{
			if (_disposed)
			{
				stream.Dispose();
				return;
			}

			_links.TryAdd(remoteEndPoint, link);
		}
		_ = Task.Run(() => RunAsync(link, _ => Task.FromResult(stream)));
	}

	public void Start(Action<EndPoint, byte[]> onReceive)
	{
		Volatile.Write(ref _onReceive, onReceive);
		_connector.Start(Accept);
	}

	public void Send(EndPoint remoteEndPoint, byte[] datagram)
	{
		if (datagram.Length > ushort.MaxValue)
			return;

		Link? link;
		lock (_lock)
		{
			if (_disposed)
				return;

			if (!_links.TryGetValue(remoteEndPoint, out link))
			{
				link = new Link(remoteEndPoint);
				_links.Add(remoteEndPoint, link);
				var opening = link;
				_ = Task.Run(() =>
					RunAsync(
						opening,
						cancellationToken =>
							_connector.ConnectAsync(remoteEndPoint, cancellationToken)
					)
				);
			}
		}
		link.Datagrams.Writer.TryWrite(datagram);
	}

	public void Dispose()
	{
		lock (_lock)
			_disposed = true;
		_cancellation.Cancel();
		Volatile.Write(ref _onReceive, null);
		_connector.Dispose();
	}
}
