using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using Aigamo.Otsuki.Transport;

namespace Aigamo.Otsuki.Tests;

/// <summary>
/// One end of an in-memory duplex stream: reads what the other end writes. Disposing it ends the other end's reads.
/// </summary>
internal sealed class DuplexStream(Stream input, Stream output) : Stream
{
	private readonly Stream _input = input;
	private readonly Stream _output = output;

	public override bool CanRead => true;

	public override bool CanSeek => false;

	public override bool CanWrite => true;

	public override long Length => throw new NotSupportedException();

	public override long Position
	{
		get => throw new NotSupportedException();
		set => throw new NotSupportedException();
	}

	public static (Stream, Stream) CreatePair()
	{
		var forward = new Pipe();
		var backward = new Pipe();
		return (
			new DuplexStream(backward.Reader.AsStream(), forward.Writer.AsStream()),
			new DuplexStream(forward.Reader.AsStream(), backward.Writer.AsStream())
		);
	}

	public override int Read(byte[] buffer, int offset, int count) =>
		_input.Read(buffer, offset, count);

	public override Task<int> ReadAsync(
		byte[] buffer,
		int offset,
		int count,
		CancellationToken cancellationToken
	) => _input.ReadAsync(buffer, offset, count, cancellationToken);

	public override ValueTask<int> ReadAsync(
		Memory<byte> buffer,
		CancellationToken cancellationToken = default
	) => _input.ReadAsync(buffer, cancellationToken);

	public override void Write(byte[] buffer, int offset, int count) =>
		_output.Write(buffer, offset, count);

	public override Task WriteAsync(
		byte[] buffer,
		int offset,
		int count,
		CancellationToken cancellationToken
	) => _output.WriteAsync(buffer, offset, count, cancellationToken);

	public override ValueTask WriteAsync(
		ReadOnlyMemory<byte> buffer,
		CancellationToken cancellationToken = default
	) => _output.WriteAsync(buffer, cancellationToken);

	public override void Flush() => _output.Flush();

	public override Task FlushAsync(CancellationToken cancellationToken) =>
		_output.FlushAsync(cancellationToken);

	public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

	public override void SetLength(long value) => throw new NotSupportedException();

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_input.Dispose();
			_output.Dispose();
		}
		base.Dispose(disposing);
	}
}

/// <summary>
/// A network of <see cref="InMemoryStreamConnector"/>s, which open <see cref="DuplexStream"/>s to each other.
/// </summary>
internal sealed class InMemoryStreamNetwork
{
	private readonly ConcurrentDictionary<EndPoint, InMemoryStreamConnector> _connectors = new();
	private int _connectCount;

	/// <summary>
	/// How many streams were opened.
	/// </summary>
	public int ConnectCount => Volatile.Read(ref _connectCount);

	public InMemoryStreamConnector Bind(EndPoint endPoint)
	{
		var connector = new InMemoryStreamConnector(this, endPoint);
		if (!_connectors.TryAdd(endPoint, connector))
			throw new SocketException((int)SocketError.AddressAlreadyInUse);
		return connector;
	}

	public StreamDatagramTransport CreateTransport(EndPoint endPoint) => new(Bind(endPoint));

	public Func<EndPoint, IDatagramTransport> CreateFactory() => CreateTransport;

	internal void Unbind(InMemoryStreamConnector connector) =>
		_connectors.TryRemove(connector.LocalEndPoint, out _);

	internal Stream Connect(EndPoint source, EndPoint destination)
	{
		if (
			!_connectors.TryGetValue(destination, out var connector)
			|| connector.Accept(source) is not { } stream
		)
			throw new SocketException((int)SocketError.ConnectionRefused);

		Interlocked.Increment(ref _connectCount);
		return stream;
	}
}

internal sealed class InMemoryStreamConnector(InMemoryStreamNetwork network, EndPoint localEndPoint)
	: IStreamConnector
{
	private readonly InMemoryStreamNetwork _network = network;
	private Action<EndPoint, Stream>? _onAccept;

	public EndPoint LocalEndPoint { get; } = localEndPoint;

	public void Start(Action<EndPoint, Stream> onAccept) => Volatile.Write(ref _onAccept, onAccept);

	public async Task<Stream> ConnectAsync(
		EndPoint remoteEndPoint,
		CancellationToken cancellationToken
	)
	{
		await Task.Yield();
		cancellationToken.ThrowIfCancellationRequested();
		return _network.Connect(LocalEndPoint, remoteEndPoint);
	}

	/// <summary>
	/// Hands the accepting end of a new stream to the owner, and returns the connecting end, or
	/// <see langword="null"/> if the connector is not accepting.
	/// </summary>
	internal Stream? Accept(EndPoint remoteEndPoint)
	{
		if (Volatile.Read(ref _onAccept) is not { } onAccept)
			return null;

		var (connecting, accepting) = DuplexStream.CreatePair();
		onAccept(remoteEndPoint, accepting);
		return connecting;
	}

	public void Dispose()
	{
		Volatile.Write(ref _onAccept, null);
		_network.Unbind(this);
	}
}
