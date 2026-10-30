using System.Runtime.Versioning;
using System.Threading.Channels;

namespace Aigamo.Otsuki.Bluetooth;

/// <summary>
/// An RFCOMM channel of a <see cref="MacBluetoothConnector"/> as a stream. What arrives waits in a queue until it is
/// read; writes are copied and sent without waiting.
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class RfcommChannelStream(MacBluetoothConnector connector, long channel) : Stream
{
	private readonly MacBluetoothConnector _connector = connector;
	private readonly Channel<byte[]> _received = Channel.CreateUnbounded<byte[]>(
		new UnboundedChannelOptions { SingleReader = true, SingleWriter = true }
	);
	private ReadOnlyMemory<byte> _pending;

	public long Id { get; } = channel;

	public override bool CanRead => true;

	public override bool CanSeek => false;

	public override bool CanWrite => true;

	public override long Length => throw new NotSupportedException();

	public override long Position
	{
		get => throw new NotSupportedException();
		set => throw new NotSupportedException();
	}

	public void Receive(ReadOnlySpan<byte> data) => _received.Writer.TryWrite(data.ToArray());

	/// <summary>
	/// Ends the reads once what arrived has been read.
	/// </summary>
	public void End() => _received.Writer.TryComplete();

	public override async ValueTask<int> ReadAsync(
		Memory<byte> buffer,
		CancellationToken cancellationToken = default
	)
	{
		while (_pending.IsEmpty)
		{
			if (_received.Reader.TryRead(out var chunk))
				_pending = chunk;
			else if (
				!await _received.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)
			)
				return 0;
		}

		var count = Math.Min(buffer.Length, _pending.Length);
		_pending[..count].CopyTo(buffer);
		_pending = _pending[count..];
		return count;
	}

	public override Task<int> ReadAsync(
		byte[] buffer,
		int offset,
		int count,
		CancellationToken cancellationToken
	) => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

	public override int Read(byte[] buffer, int offset, int count) =>
		ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

	public override void Write(ReadOnlySpan<byte> buffer) => _connector.Write(Id, buffer);

	public override void Write(byte[] buffer, int offset, int count) =>
		Write(buffer.AsSpan(offset, count));

	public override ValueTask WriteAsync(
		ReadOnlyMemory<byte> buffer,
		CancellationToken cancellationToken = default
	)
	{
		Write(buffer.Span);
		return ValueTask.CompletedTask;
	}

	public override Task WriteAsync(
		byte[] buffer,
		int offset,
		int count,
		CancellationToken cancellationToken
	) => WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

	public override void Flush() { }

	public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

	public override void SetLength(long value) => throw new NotSupportedException();

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_connector.Close(Id);
			End();
		}
		base.Dispose(disposing);
	}
}
