using System.Net;
using Aigamo.Otsuki.Messages.Reliable;
using Aigamo.Otsuki.Reliable;
using Aigamo.Results;

namespace Aigamo.Otsuki.Session;

/// <summary>
/// What a <see cref="SessionCore"/> does outside itself, in the order it decides to, on the thread that called it.
/// The caller carries each out before the call returns, and must not call the core back from one: what comes back,
/// such as a connection's report, is a later input.
/// </summary>
internal interface ISessionOutputs
{
	/// <summary>
	/// Opens the endpoint on <paramref name="localEndPoint"/>, and returns the address it is bound to.
	/// </summary>
	Result<EndPoint, Exception> OpenEndpoint(EndPoint localEndPoint);

	/// <summary>
	/// Shuts the endpoint down once its connections have ended, after the messages given them before.
	/// </summary>
	void ShutDownEndpoint();

	/// <summary>
	/// Makes the endpoint's new inbound connections offer <paramref name="signing"/>.
	/// </summary>
	void SetSigning(PacketSigning signing);

	/// <summary>
	/// Opens an outbound connection that signs as <paramref name="signing"/> says, on the loop that the partner's
	/// address chooses, and returns its handle at once; its loop reports how it goes.
	/// </summary>
	Connection Connect(EndPoint remoteEndPoint, PacketSigning signing);

	/// <summary>
	/// The IPv4 address of a host name that a peer's address gives, or <see langword="null"/> if it has none. Looks it
	/// up while the session waits: peers give addresses, not names, but for the rare one that is a name.
	/// </summary>
	IPAddress? ResolveHostname(string hostname);

	/// <summary>
	/// Gives a connection a message, through its loop.
	/// </summary>
	void Post(Connection connection, ConnectionMessage message);

	/// <summary>
	/// Gives the connections of one loop (<see cref="Connection.Loop"/>) one message to send.
	/// </summary>
	void SendToMany(object loop, ConnectionLoopMessage.SendToMany message);

	/// <summary>
	/// Sends a datagram outside any connection, such as a host enumeration response.
	/// </summary>
	void SendDatagram(EndPoint remoteEndPoint, byte[] datagram);

	/// <summary>
	/// Gives the session a message after those already waiting for it, as a command that had to wait is.
	/// </summary>
	void Defer(SessionMessage message);

	/// <summary>
	/// Publishes what the peer's queries read.
	/// </summary>
	void Publish(SessionSnapshot snapshot);

	/// <summary>
	/// Raises an event for the application.
	/// </summary>
	void Emit(PeerEvent peerEvent);

	/// <summary>
	/// Ends the events: the application has read the last once it has read those raised so far.
	/// </summary>
	void EndEvents();

	/// <summary>
	/// Completes a command, after the snapshot of the input is published.
	/// </summary>
	void Complete(Completion completion);

	void Complete<T>(Completion<T> completion, T value);

	void Fail(Completion completion, Exception exception);

	void Fail<T>(Completion<T> completion, Exception exception);

	/// <summary>
	/// The session is closed: it takes no more inputs.
	/// </summary>
	void Stop();
}
