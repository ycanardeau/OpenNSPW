using System.Collections.Immutable;
using Aigamo.MatchGenerator;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Reliable;
using Aigamo.Otsuki.Session.Transitions;

namespace Aigamo.Otsuki.Session;

/// <summary>
/// A host's check whether <c>Target</c> is still in the session, on behalf of <see cref="Requester"/>
/// ([MC-DPL8CS] section 3.2.5.3).
/// </summary>
internal sealed record IntegrityCheck(Dpnid Requester, ITimer Timer);

internal interface IHasHostingData
{
	/// <summary>
	/// The name table index of the next player.
	/// </summary>
	int NextIndex { get; }

	/// <summary>
	/// The oldest name table version that every peer has reported.
	/// </summary>
	int ResynchronizedVersion { get; }

	/// <summary>
	/// Integrity checks in progress, by target.
	/// </summary>
	ImmutableDictionary<Dpnid, IntegrityCheck> IntegrityChecks { get; }

	SessionState.Hosting ToHosting() => (SessionState.Hosting)this;
}

internal interface IHasHostConnection
{
	Connection HostConnection { get; }
}

internal interface IHasJoinProgress : IHasHostConnection
{
	/// <summary>
	/// The reply data of DN_SEND_CONNECT_INFO, reported when the join completes.
	/// </summary>
	ImmutableArray<byte> Reply { get; }

	bool InstructConnectReceived { get; }

	/// <summary>
	/// Established peers that have not sent DN_SEND_PLAYER_DPNID yet.
	/// </summary>
	ImmutableHashSet<Dpnid> AwaitingPlayers { get; }

	SessionState.Joining ToJoining() => (SessionState.Joining)this;
}

internal interface IHasCloseCompletions
{
	/// <summary>
	/// Completed when closing finishes.
	/// </summary>
	ImmutableList<TaskCompletionSource> Completions { get; }
}

/// <summary>
/// The states of the local peer in a peer-to-peer session ([MC-DPL8CS] section 3). Each state implements the
/// transitions it allows; a message that the current state has no transition for is ignored.
/// </summary>
[GenerateMatch]
internal abstract record SessionState
{
	private SessionState() { }

	public sealed record Idle : SessionState, ICanHost, ICanConnect;

	public sealed record Hosting(
		int NextIndex,
		int ResynchronizedVersion,
		ImmutableDictionary<Dpnid, IntegrityCheck> IntegrityChecks
	)
		: SessionState,
			IHasHostingData,
			ICanAdmitPlayers,
			ICanCheckIntegrity,
			ICanResynchronizeVersion,
			ICanLosePlayers,
			ICanReceiveData,
			ICanSendTo,
			ICanLeave;

	/// <summary>
	/// Connecting to the host and waiting for DN_SEND_CONNECT_INFO.
	/// </summary>
	public sealed record Connecting(Connection HostConnection)
		: SessionState,
			IHasHostConnection,
			ICanIntroduceSelf,
			ICanReceiveConnectInfo,
			ICanFailConnecting;

	/// <summary>
	/// In the name table, waiting for the established peers to connect.
	/// </summary>
	public sealed record Joining(
		Connection HostConnection,
		ImmutableArray<byte> Reply,
		bool InstructConnectReceived,
		ImmutableHashSet<Dpnid> AwaitingPlayers
	)
		: SessionState,
			IHasJoinProgress,
			ICanFollowHost,
			ICanMeetPeers,
			ICanCompleteJoining,
			ICanFailJoining,
			ICanReceiveData;

	public sealed record Connected(Connection HostConnection)
		: SessionState,
			IHasHostConnection,
			ICanFollowHost,
			ICanMeetPeers,
			ICanFollowNewPlayers,
			ICanLoseHost,
			ICanReceiveData,
			ICanSendTo,
			ICanLeave;

	/// <summary>
	/// Left the session involuntarily; the application still has to close the peer.
	/// </summary>
	public sealed record Terminated : SessionState, ICanLeave;

	public sealed record Closing(ImmutableList<TaskCompletionSource> Completions)
		: SessionState,
			IHasCloseCompletions,
			ICanFinishClosing;

	public sealed record Closed : SessionState;
}

/// <summary>
/// A core message ([MC-DPL8CS] section 2.2) that arrived on <see cref="Connection"/>.
/// </summary>
internal sealed record Received<TMessage>(Connection Connection, TMessage Message)
	where TMessage : CoreMessage;
