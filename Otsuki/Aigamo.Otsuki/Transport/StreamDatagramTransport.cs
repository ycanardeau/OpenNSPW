using System.Buffers.Binary;
using System.Net;
using Aigamo.Otsuki.Actors;

namespace Aigamo.Otsuki.Transport;

/// <summary>
/// Carries datagrams over the reliable streams of an <see cref="IStreamConnector"/>, each prefixed with its length
/// as a big-endian 16-bit number. The first <see cref="Send"/> to an endpoint opens a stream to it, and the
/// datagrams wait for it to open; streams that remote endpoints open are accepted. A stream that fails or closes is
/// dropped, with the datagrams still waiting for it, as if they were lost, and the next <see cref="Send"/> opens a
/// new one. The transport owns the connector, and disposes it.
/// </summary>
public sealed class StreamDatagramTransport : IDatagramTransport
{
	/// <summary>
	/// A stream to one remote endpoint, open or still opening, with the datagrams waiting to be written to it, which
	/// its writer receives.
	/// </summary>
	private sealed class Link(EndPoint remoteEndPoint)
	{
		// Datagrams beyond this many waiting are lost, as a full socket buffer would lose them.
		private const int MaxWaitingDatagrams = 256;

		public EndPoint RemoteEndPoint { get; } = remoteEndPoint;

		public Mailbox<byte[]> Datagrams { get; } = new(MaxWaitingDatagrams);
	}

	private abstract record LinksMessage
	{
		private LinksMessage() { }

		public sealed record Start(Action<EndPoint, byte[]> OnReceive) : LinksMessage;

		public sealed record Send(EndPoint RemoteEndPoint, byte[] Datagram) : LinksMessage;

		public sealed record Accepted(EndPoint RemoteEndPoint, Stream Stream) : LinksMessage;

		public sealed record Ended(Link Link) : LinksMessage;

		public sealed record Dispose : LinksMessage;
	}

	/// <summary>
	/// Owns the links: opens one as the first datagram to an endpoint comes, accepts the streams that remote
	/// endpoints open, and drops a link once its stream has failed or closed.
	/// </summary>
	private sealed class Links : Actor<LinksMessage>
	{
		private const int HeaderSize = sizeof(ushort);

		private readonly IStreamConnector _connector;
		private readonly CancellationToken _cancellation;

		// The link that sends to each endpoint. A link that a remote endpoint opened while we had one to it already
		// only receives.
		private readonly Dictionary<EndPoint, Link> _links = [];

		private Action<EndPoint, byte[]>? _onReceive;

		public Links(IStreamConnector connector, CancellationToken cancellation)
		{
			_connector = connector;
			_cancellation = cancellation;
			Start();
		}

		private static async Task ReceiveAsync(
			EndPoint remoteEndPoint,
			Stream stream,
			Action<EndPoint, byte[]>? onReceive,
			CancellationToken cancellationToken
		)
		{
			var header = new byte[HeaderSize];
			while (true)
			{
				await stream.ReadExactlyAsync(header, cancellationToken).ConfigureAwait(false);
				var datagram = new byte[BinaryPrimitives.ReadUInt16BigEndian(header)];
				await stream.ReadExactlyAsync(datagram, cancellationToken).ConfigureAwait(false);
				onReceive?.Invoke(remoteEndPoint, datagram);
			}
		}

		private static async Task WriteAsync(
			Mailbox<byte[]> datagrams,
			Stream stream,
			CancellationToken cancellationToken
		)
		{
			while (await datagrams.WaitToReceiveAsync(cancellationToken).ConfigureAwait(false))
			{
				while (datagrams.TryReceive(out var datagram))
				{
					var frame = new byte[HeaderSize + datagram.Length];
					BinaryPrimitives.WriteUInt16BigEndian(frame, (ushort)datagram.Length);
					datagram.CopyTo(frame, HeaderSize);
					await stream.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
				}

				await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Receives from the link's stream and writes its datagrams to it until either fails or the stream closes,
		/// then drops the link.
		/// </summary>
		private async Task RunAsync(
			Link link,
			Action<EndPoint, byte[]>? onReceive,
			Func<CancellationToken, Task<Stream>> open
		)
		{
			Stream? stream = null;
			try
			{
				stream = await open(_cancellation).ConfigureAwait(false);
				using var running = CancellationTokenSource.CreateLinkedTokenSource(_cancellation);
				var receiving = ReceiveAsync(link.RemoteEndPoint, stream, onReceive, running.Token);
				var writing = WriteAsync(link.Datagrams, stream, running.Token);
				await Task.WhenAny(receiving, writing).ConfigureAwait(false);
				await running.CancelAsync().ConfigureAwait(false);
				await Task.WhenAll(receiving, writing).ConfigureAwait(false);
			}
			catch (Exception)
			{
				// The stream did not open, failed, closed or was cancelled. Its datagrams are lost; the reliable
				// protocol resends them.
			}
			finally
			{
				Post(new LinksMessage.Ended(link));
				link.Datagrams.Complete();
				if (stream is not null)
					await stream.DisposeAsync().ConfigureAwait(false);
			}
		}

		private void Run(Link link, Func<CancellationToken, Task<Stream>> open)
		{
			var onReceive = _onReceive;
			_ = Task.Run(() => RunAsync(link, onReceive, open));
		}

		private void Send(EndPoint remoteEndPoint, byte[] datagram)
		{
			if (!_links.TryGetValue(remoteEndPoint, out var link))
			{
				link = new Link(remoteEndPoint);
				_links.Add(remoteEndPoint, link);
				Run(link, cancellationToken => _connector.ConnectAsync(remoteEndPoint, cancellationToken));
			}

			link.Datagrams.Post(datagram);
		}

		private void Accept(EndPoint remoteEndPoint, Stream stream)
		{
			var link = new Link(remoteEndPoint);
			_links.TryAdd(remoteEndPoint, link);
			Run(link, _ => Task.FromResult(stream));
		}

		private void End(Link link)
		{
			if (_links.GetValueOrDefault(link.RemoteEndPoint) == link)
				_links.Remove(link.RemoteEndPoint);
		}

		protected override void Receive(LinksMessage message)
		{
			switch (message)
			{
				case LinksMessage.Start start:
					_onReceive = start.OnReceive;
					break;
				case LinksMessage.Send send:
					Send(send.RemoteEndPoint, send.Datagram);
					break;
				case LinksMessage.Accepted accepted:
					Accept(accepted.RemoteEndPoint, accepted.Stream);
					break;
				case LinksMessage.Ended ended:
					End(ended.Link);
					break;
				case LinksMessage.Dispose:
					_onReceive = null;
					_links.Clear();
					Stop();
					break;
			}
		}
	}

	private readonly IStreamConnector _connector;
	private readonly CancellationTokenSource _cancellation = new();
	private readonly Links _links;

	public StreamDatagramTransport(IStreamConnector connector)
	{
		_connector = connector;
		_links = new Links(connector, _cancellation.Token);
	}

	public EndPoint LocalEndPoint => _connector.LocalEndPoint;

	private void Accept(EndPoint remoteEndPoint, Stream stream)
	{
		if (!_links.Post(new LinksMessage.Accepted(remoteEndPoint, stream)))
			stream.Dispose();
	}

	public void Start(Action<EndPoint, byte[]> onReceive)
	{
		_links.Post(new LinksMessage.Start(onReceive));
		_connector.Start(Accept);
	}

	public void Send(EndPoint remoteEndPoint, byte[] datagram)
	{
		if (datagram.Length <= ushort.MaxValue)
			_links.Post(new LinksMessage.Send(remoteEndPoint, datagram));
	}

	public void Dispose()
	{
		_links.Post(new LinksMessage.Dispose());
		_cancellation.Cancel();
		_connector.Dispose();
	}
}
