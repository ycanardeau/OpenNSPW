using System.Collections.Immutable;

namespace Aigamo.Otsuki;

/// <summary>
/// The messages waiting in a peer's mailboxes at one moment (<see cref="Peer.Queues"/>).
/// </summary>
/// <param name="Session">The session's mailbox: commands, and what the connections report.</param>
/// <param name="ConnectionLoops">Each connection loop's mailbox: datagrams, timers and sends.</param>
/// <param name="Events">The events not yet handled by the application.</param>
public sealed record PeerQueues(int Session, ImmutableArray<int> ConnectionLoops, int Events);
