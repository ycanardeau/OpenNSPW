using System.Collections.Immutable;
using Aigamo.MatchGenerator;
using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki;

/// <summary>
/// A notification from a <see cref="Peer"/>, like the DPN_MSGID_* messages of DirectPlay.
/// Handle every case with the generated <c>Match</c>.
/// </summary>
[GenerateMatch]
public abstract record PeerEvent
{
	private PeerEvent() { }

	/// <summary>
	/// A player joined the session (DPN_MSGID_CREATE_PLAYER). Also raised for the local player.
	/// </summary>
	public sealed record PlayerCreated(PlayerInfo Player) : PeerEvent;

	/// <summary>
	/// A player left the session (DPN_MSGID_DESTROY_PLAYER).
	/// </summary>
	public sealed record PlayerDestroyed(Dpnid Player, DestroyPlayerFlags Reason) : PeerEvent;

	/// <summary>
	/// A message arrived from another player (DPN_MSGID_RECEIVE).
	/// </summary>
	public sealed record DataReceived(Dpnid Sender, ImmutableArray<byte> Data) : PeerEvent;

	/// <summary>
	/// A <see cref="Peer.ConnectAsync(ApplicationDescription, System.Net.IPEndPoint, int, byte[])"/> finished (DPN_MSGID_CONNECT_COMPLETE).
	/// </summary>
	public sealed record ConnectCompleted(ResultCode Result, ImmutableArray<byte> ReplyData)
		: PeerEvent;

	/// <summary>
	/// A message sent by <see cref="Peer.SendTo"/> was delivered or failed (DPN_MSGID_SEND_COMPLETE).
	/// </summary>
	public sealed record SendCompleted(long Handle, ResultCode Result) : PeerEvent;

	/// <summary>
	/// The local peer is no longer in the session (DPN_MSGID_TERMINATE_SESSION).
	/// </summary>
	public sealed record SessionTerminated(ResultCode Result) : PeerEvent;
}
