using System.Net;

namespace Aigamo.Otsuki.Reliable;

/// <summary>
/// A connection as the session knows it: the partner, and the loop that runs it. Nothing in it changes, so the
/// session and the loop may both hold it: the connection's state is its loop's alone
/// (<see cref="ReliableConnection"/>), and the session reaches it only by a message to the loop. Two handles are the
/// same connection only if they are the same object.
/// </summary>
/// <param name="loop">The loop that runs the connection, which the core does not look into: the shell posts the
/// connection's messages to it.</param>
internal sealed class Connection(EndPoint remoteEndPoint, bool isOutbound, object loop)
{
	public EndPoint RemoteEndPoint { get; } = remoteEndPoint;

	public bool IsOutbound { get; } = isOutbound;

	public object Loop { get; } = loop;

	public override string ToString() =>
		$"{nameof(Connection)}({RemoteEndPoint}, {(IsOutbound ? "outbound" : "inbound")})";
}
