namespace Aigamo.Otsuki;

/// <summary>
/// Options for <see cref="Peer.SendTo"/>, using the values of the DPNSEND_* flags.
/// </summary>
[Flags]
public enum SendFlags
{
	None = 0,

	/// <summary>
	/// Do not report <see cref="PeerEvent.SendCompleted"/> for this send.
	/// </summary>
	NoComplete = 0x0002,

	/// <summary>
	/// Retry the message until it is delivered.
	/// </summary>
	Guaranteed = 0x0008,

	/// <summary>
	/// When sending to <see cref="Peer.AllPlayers"/>, do not deliver the message to the local player.
	/// </summary>
	NoLoopback = 0x0020,
}
