using Aigamo.Otsuki.Messages.Reliable;

namespace Aigamo.Otsuki.Reliable;

/// <summary>
/// What the CONNECT and CONNECTED exchange established ([MC-DPL8R] sections 3.1.5.1.1 and 3.1.5.1.2).
/// </summary>
internal sealed record Handshake(SessionId SessionId, TimeSpan RetryInterval)
{
	public int RemoteProtocolVersion { get; init; }

	/// <summary>
	/// bMsgID of our next CONNECT or CONNECTED.
	/// </summary>
	public byte MessageId { get; init; }

	/// <summary>
	/// bMsgID of the partner's last CONNECT or CONNECTED, echoed in bRspId.
	/// </summary>
	public byte RemoteMessageId { get; init; }

	public int Retries { get; init; }

	/// <summary>
	/// The retry period doubles up to <paramref name="maxRetryInterval"/> ([MC-DPL8R] section 3.1.6.1).
	/// </summary>
	public Handshake Retry(TimeSpan maxRetryInterval) =>
		this with
		{
			Retries = Retries + 1,
			MessageId = (byte)(MessageId + 1),
			RetryInterval =
				RetryInterval * 2 < maxRetryInterval ? RetryInterval * 2 : maxRetryInterval,
		};

	public ReliableMessage.Connect CreateConnect(int timestamp) =>
		new()
		{
			Poll = true,
			MessageId = MessageId,
			ProtocolVersion = Connection.ProtocolVersion,
			SessionId = SessionId,
			Timestamp = timestamp,
		};

	public ReliableMessage.Connected CreateConnected(bool poll, int timestamp) =>
		new()
		{
			Poll = poll,
			MessageId = MessageId,
			ResponseId = RemoteMessageId,
			ProtocolVersion = Connection.ProtocolVersion,
			SessionId = SessionId,
			Timestamp = timestamp,
		};

	public ReliableMessage.HardDisconnect CreateHardDisconnect(int timestamp) =>
		new()
		{
			MessageId = MessageId,
			ProtocolVersion = Connection.ProtocolVersion,
			SessionId = SessionId,
			Timestamp = timestamp,
		};
}
