using System.Collections.Immutable;
using System.Net;
using Aigamo.MatchGenerator;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Messages.Enumeration;
using Aigamo.Otsuki.Reliable;

namespace Aigamo.Otsuki.Session;

[GenerateMatch]
internal abstract record SessionMessage
{
	private SessionMessage() { }

	/// <summary>
	/// Sets the local player's name and data, and updates them in the session if there is one.
	/// </summary>
	public sealed record SetPeerInfo(
		string Name,
		ImmutableArray<byte> Data,
		TaskCompletionSource Completion
	) : SessionMessage;

	public sealed record CreateGroup(
		string Name,
		ImmutableArray<byte> Data,
		GroupFlags Flags,
		TaskCompletionSource<Dpnid> Completion
	) : SessionMessage;

	public sealed record DestroyGroup(Dpnid Group, TaskCompletionSource Completion)
		: SessionMessage;

	public sealed record AddPlayerToGroup(
		Dpnid Group,
		Dpnid Player,
		TaskCompletionSource Completion
	) : SessionMessage;

	public sealed record RemovePlayerFromGroup(
		Dpnid Group,
		Dpnid Player,
		TaskCompletionSource Completion
	) : SessionMessage;

	/// <summary>
	/// The host removes a player from the session, like IDirectPlay8Peer::DestroyPeer and
	/// IDirectPlay8Server::DestroyClient.
	/// </summary>
	public sealed record DestroyPlayer(
		Dpnid Player,
		ImmutableArray<byte> Data,
		TaskCompletionSource Completion
	) : SessionMessage;

	/// <summary>
	/// The host did not answer a request for a name table change in time.
	/// </summary>
	public sealed record RequestTimedOut(int Context) : SessionMessage;

	public sealed record Host(
		ApplicationDescription Application,
		IPEndPoint LocalEndPoint,
		TaskCompletionSource Completion
	) : SessionMessage;

	public sealed record Connect(
		ApplicationDescription Application,
		IPEndPoint HostEndPoint,
		IPEndPoint LocalEndPoint,
		ImmutableArray<byte> ConnectData,
		TaskCompletionSource Completion
	) : SessionMessage;

	public sealed record SendTo(Dpnid Target, byte[] Data, SendFlags Flags, long Handle)
		: SessionMessage;

	public sealed record Close(TaskCompletionSource Completion) : SessionMessage;

	public sealed record ConnectionConnected(Connection Connection) : SessionMessage;

	public sealed record ConnectionConnectFailed(Connection Connection, ResultCode Result)
		: SessionMessage;

	public sealed record ConnectionReceived(Connection Connection, byte[] Payload, bool User1)
		: SessionMessage;

	public sealed record ConnectionSendCompleted(
		Connection Connection,
		long SendId,
		ResultCode Result
	) : SessionMessage;

	public sealed record ConnectionDisconnected(Connection Connection, ResultCode Result)
		: SessionMessage;

	public sealed record EnumQueryReceived(
		IPEndPoint RemoteEndPoint,
		EnumerationMessage.Query Query
	) : SessionMessage;

	/// <summary>
	/// Sets the data that the host returns in host enumeration responses.
	/// </summary>
	public sealed record SetEnumResponseData(ImmutableArray<byte> Data) : SessionMessage;

	public sealed record JoinTimedOut(long Generation) : SessionMessage;

	public sealed record CloseTimedOut(long Generation) : SessionMessage;

	/// <summary>
	/// A new host stops waiting for peers during migration.
	/// </summary>
	public sealed record MigrationTimedOut(long Generation) : SessionMessage;

	public sealed record PlayerJoinTimedOut(Dpnid Player) : SessionMessage;

	public sealed record IntegrityCheckTimedOut(Dpnid Target, Dpnid Requester) : SessionMessage;
}
