using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using Aigamo.Otsuki.Actors;
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

internal abstract record StreamNetworkMessage
{
	private StreamNetworkMessage() { }

	public sealed record Bind(InMemoryStreamConnector Connector, TaskCompletionSource<bool> Bound)
		: StreamNetworkMessage;

	public sealed record Start(InMemoryStreamConnector Connector, Action<EndPoint, Stream> OnAccept)
		: StreamNetworkMessage;

	public sealed record Unbind(InMemoryStreamConnector Connector) : StreamNetworkMessage;

	public sealed record Connect(
		EndPoint Source,
		EndPoint Destination,
		TaskCompletionSource<Stream> Connected
	) : StreamNetworkMessage;

	public sealed record Ask(Action Question) : StreamNetworkMessage;
}

/// <summary>
/// A network of <see cref="InMemoryStreamConnector"/>s, which open <see cref="DuplexStream"/>s to each other.
/// It is an actor, which owns its connectors and what they accept with.
/// </summary>
internal sealed class InMemoryStreamNetwork : Actor<StreamNetworkMessage>
{
	private readonly Dictionary<
		EndPoint,
		(InMemoryStreamConnector Connector, Action<EndPoint, Stream>? OnAccept)
	> _connectors = [];
	private int _connectCount;

	public InMemoryStreamNetwork()
	{
		Start();
	}

	private T Ask<T>(Func<T> question)
	{
		var answer = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
		Post(new StreamNetworkMessage.Ask(() => answer.SetResult(question())));
		return answer.Task.GetAwaiter().GetResult();
	}

	/// <summary>
	/// How many streams were opened.
	/// </summary>
	public int ConnectCount => Ask(() => _connectCount);

	public InMemoryStreamConnector Bind(EndPoint endPoint)
	{
		var connector = new InMemoryStreamConnector(this, endPoint);
		var bound = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		Post(new StreamNetworkMessage.Bind(connector, bound));
		if (!bound.Task.GetAwaiter().GetResult())
			throw new SocketException((int)SocketError.AddressAlreadyInUse);
		return connector;
	}

	public StreamDatagramTransport CreateTransport(EndPoint endPoint) => new(Bind(endPoint));

	public Func<EndPoint, IDatagramTransport> CreateFactory() => CreateTransport;

	// Hands the accepting end of a new stream to the destination's owner, and the connecting end to the source; or
	// refuses, if the destination is not accepting.
	private void Connect(EndPoint source, EndPoint destination, TaskCompletionSource<Stream> connected)
	{
		if (!_connectors.TryGetValue(destination, out var entry) || entry.OnAccept is not { } onAccept)
		{
			connected.SetException(new SocketException((int)SocketError.ConnectionRefused));
			return;
		}

		var (connecting, accepting) = DuplexStream.CreatePair();
		onAccept(source, accepting);
		_connectCount++;
		connected.SetResult(connecting);
	}

	protected override void Receive(StreamNetworkMessage message)
	{
		switch (message)
		{
			case StreamNetworkMessage.Bind bind:
				bind.Bound.SetResult(_connectors.TryAdd(bind.Connector.LocalEndPoint, (bind.Connector, null)));
				break;
			case StreamNetworkMessage.Start start
				when _connectors.TryGetValue(start.Connector.LocalEndPoint, out var entry)
					&& entry.Connector == start.Connector:
				_connectors[start.Connector.LocalEndPoint] = (start.Connector, start.OnAccept);
				break;
			case StreamNetworkMessage.Unbind unbind
				when _connectors.TryGetValue(unbind.Connector.LocalEndPoint, out var entry)
					&& entry.Connector == unbind.Connector:
				_connectors.Remove(unbind.Connector.LocalEndPoint);
				break;
			case StreamNetworkMessage.Connect connect:
				Connect(connect.Source, connect.Destination, connect.Connected);
				break;
			case StreamNetworkMessage.Ask ask:
				ask.Question();
				break;
		}
	}
}

internal sealed class InMemoryStreamConnector(InMemoryStreamNetwork network, EndPoint localEndPoint)
	: IStreamConnector
{
	private readonly InMemoryStreamNetwork _network = network;

	public EndPoint LocalEndPoint { get; } = localEndPoint;

	public void Start(Action<EndPoint, Stream> onAccept) =>
		_network.Post(new StreamNetworkMessage.Start(this, onAccept));

	public async Task<Stream> ConnectAsync(
		EndPoint remoteEndPoint,
		CancellationToken cancellationToken
	)
	{
		await Task.Yield();
		cancellationToken.ThrowIfCancellationRequested();
		var connected = new TaskCompletionSource<Stream>(
			TaskCreationOptions.RunContinuationsAsynchronously
		);
		_network.Post(new StreamNetworkMessage.Connect(LocalEndPoint, remoteEndPoint, connected));
		return await connected.Task;
	}

	public void Dispose() => _network.Post(new StreamNetworkMessage.Unbind(this));
}
