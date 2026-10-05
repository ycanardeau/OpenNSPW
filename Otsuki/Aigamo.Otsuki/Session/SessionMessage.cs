using System.Collections.Immutable;
using System.Net;
using Aigamo.MatchGenerator;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Reliable;

namespace Aigamo.Otsuki.Session;

[GenerateMatch]
internal abstract record SessionMessage
{
	private SessionMessage() { }

	public sealed record SetPeerInfo(string Name, ImmutableArray<byte> Data) : SessionMessage;

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

	public sealed record JoinTimedOut(long Generation) : SessionMessage;

	public sealed record CloseTimedOut(long Generation) : SessionMessage;

	public sealed record PlayerJoinTimedOut(Dpnid Player) : SessionMessage;

	public sealed record IntegrityCheckTimedOut(Dpnid Target, Dpnid Requester) : SessionMessage;
}
