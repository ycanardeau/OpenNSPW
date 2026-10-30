using System.Net;
using Aigamo.Otsuki.Reliable;
using Aigamo.Otsuki.Transport;

namespace Aigamo.Otsuki;

public sealed record PeerOptions
{
	public static PeerOptions Default { get; } = new();

	public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

	public ReliableProfile ReliableProfile { get; init; } = ReliableProfile.Default;

	/// <summary>
	/// Creates the transport bound to the given local endpoint. Defaults to a UDP socket, which binds only to an
	/// <see cref="IPEndPoint"/>; other kinds of endpoint need a transport of their own.
	/// </summary>
	public Func<EndPoint, IDatagramTransport> TransportFactory { get; init; } =
		localEndPoint =>
			localEndPoint is IPEndPoint ipEndPoint
				? new UdpDatagramTransport(ipEndPoint)
				: throw new NotSupportedException(
					$"UDP cannot bind to {localEndPoint}. Set {nameof(TransportFactory)} to a transport for it."
				);

	/// <summary>
	/// How long a joining peer may take from connecting to the host until it is in the session.
	/// </summary>
	public TimeSpan JoinTimeout { get; init; } = TimeSpan.FromSeconds(60);

	/// <summary>
	/// How long the host waits for an answer to an integrity check before dropping the player.
	/// </summary>
	public TimeSpan IntegrityCheckTimeout { get; init; } = TimeSpan.FromSeconds(10);

	/// <summary>
	/// How long a peer waits for the host to carry out a group change or an info update it requested
	/// ([MC-DPL8CS] section 3.5).
	/// </summary>
	public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);

	/// <summary>
	/// How long closing waits for connections to disconnect gracefully.
	/// </summary>
	public TimeSpan CloseTimeout { get; init; } = TimeSpan.FromSeconds(5);
}
