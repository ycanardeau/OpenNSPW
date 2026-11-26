using System.Net;
using System.Net.Sockets;
using Aigamo.Otsuki.Actors;

namespace Aigamo.Otsuki.Transport;

/// <summary>
/// Receives for the UDP transports it makes (<see cref="CreateTransport"/>, a <see cref="PeerOptions.TransportFactory"/>),
/// on the thread that calls <see cref="Poll"/>, for many peers in one process, such as a load test's clients. The
/// default transport keeps a receive waiting on each socket, which costs the system and the thread pool a wake for
/// every datagram; a poll asks the system once which sockets have datagrams, and reads them all in one pass.
/// </summary>
/// <remarks>
/// A datagram waits in its socket's buffer until the next poll, so the application polls as often as the latency it
/// wants, like <see cref="Peer.DoWork"/>. While it does not poll, the peers on these transports hear nothing: no
/// acknowledgment, no keep-alive, no host that answers an enumeration.
/// </remarks>
public sealed class UdpPoller
{
	// A socket to read, with what to hand its datagrams to; or, with nothing, a socket to read no more.
	private sealed record Change(Socket Socket, Action<EndPoint, byte[]>? OnReceive);

	// Posted from the threads that start and dispose the transports, taken by the one that polls.
	private readonly Mailbox<Change> _changes = new();

	// Only the thread that polls touches the rest.
	private readonly Dictionary<Socket, Action<EndPoint, byte[]>> _receivers = [];
	private readonly List<Socket> _ready = [];
	private readonly byte[] _buffer = new byte[UdpDatagramTransport.MaxDatagramSize];
	private readonly SocketAddress _address = new(AddressFamily.InterNetwork);
	private readonly Dictionary<SocketAddress, UdpDatagramTransport.RemoteEndPoint> _endPoints = [];

	/// <summary>
	/// How many sockets the last poll read, for a test.
	/// </summary>
	internal int Sockets => _receivers.Count;

	/// <summary>
	/// A UDP transport bound to <paramref name="localEndPoint"/>, whose datagrams this poller receives.
	/// </summary>
	public IDatagramTransport CreateTransport(EndPoint localEndPoint) =>
		localEndPoint is IPEndPoint ipEndPoint
			? new UdpDatagramTransport(ipEndPoint, [this])
			: throw new NotSupportedException($"UDP cannot bind to {localEndPoint}.");

	internal void Add(Socket socket, Action<EndPoint, byte[]> onReceive) =>
		_changes.Post(new Change(socket, onReceive));

	internal void Remove(Socket socket) => _changes.Post(new Change(socket, null));

	private void TakeChanges()
	{
		while (_changes.TryReceive(out var change))
		{
			if (change.OnReceive is { } onReceive)
				_receivers[change.Socket] = onReceive;
			else
				_receivers.Remove(change.Socket);
		}
	}

	// Reads the socket's datagrams until it has none left. Returns how many.
	private int Receive(Socket socket, Action<EndPoint, byte[]> onReceive)
	{
		var count = 0;
		var isFirst = true;
		while (true)
		{
			int received;
			try
			{
				// The first is there, as the system said; the others only while it has more, so that the last read
				// does not fail for want of one.
				if (!isFirst && socket.Available == 0)
					return count;

				isFirst = false;
				received = socket.ReceiveFrom(_buffer, SocketFlags.None, _address);
			}
			catch (SocketException e)
				when (e.SocketErrorCode is SocketError.ConnectionReset or SocketError.MessageSize)
			{
				continue;
			}
			catch (Exception e) when (e is ObjectDisposedException or SocketException)
			{
				return count;
			}

			onReceive(
				UdpDatagramTransport.FindRemote(_endPoints, _address),
				_buffer.AsSpan(0, received).ToArray()
			);
			count++;
		}
	}

	/// <summary>
	/// Hands every datagram that waits in the transports' sockets to its transport's peer, on the calling thread, and
	/// returns how many. Waits for nothing. Called from one thread at a time.
	/// </summary>
	public int Poll()
	{
		TakeChanges();
		if (_receivers.Count == 0)
			return 0;

		_ready.Clear();
		_ready.AddRange(_receivers.Keys);
		try
		{
			Socket.Select(_ready, null, null, 0);
		}
		catch (Exception e) when (e is ObjectDisposedException or SocketException)
		{
			// A transport disposed its socket after the changes were taken: it posted its removal before, which the
			// next poll takes.
			return 0;
		}

		var count = 0;
		foreach (var socket in _ready)
			count += Receive(socket, _receivers[socket]);
		return count;
	}
}
