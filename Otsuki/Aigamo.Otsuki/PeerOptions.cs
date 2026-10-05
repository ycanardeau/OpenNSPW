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
	/// Creates the transport bound to the given local endpoint. Defaults to a UDP socket.
	/// </summary>
	public Func<IPEndPoint, IDatagramTransport> TransportFactory { get; init; } =
		localEndPoint => new UdpDatagramTransport(localEndPoint);

	/// <summary>
	/// How long a joining peer may take from connecting to the host until it is in the session.
	/// </summary>
	public TimeSpan JoinTimeout { get; init; } = TimeSpan.FromSeconds(60);

	/// <summary>
	/// How long the host waits for an answer to an integrity check before dropping the player.
	/// </summary>
	public TimeSpan IntegrityCheckTimeout { get; init; } = TimeSpan.FromSeconds(10);

	/// <summary>
	/// How long closing waits for connections to disconnect gracefully.
	/// </summary>
	public TimeSpan CloseTimeout { get; init; } = TimeSpan.FromSeconds(5);
}
