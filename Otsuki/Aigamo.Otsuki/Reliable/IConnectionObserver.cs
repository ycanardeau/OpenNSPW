using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Reliable;

/// <summary>
/// The upper layer of a <see cref="Connection"/>. Every method is called from the connection's message loop,
/// so implementations must not block; an actor implements these by posting to its own mailbox.
/// For any single connection, calls are made in order.
/// </summary>
internal interface IConnectionObserver
{
	void OnConnected(Connection connection);

	/// <summary>
	/// An outbound connection attempt failed before it was established.
	/// </summary>
	void OnConnectFailed(Connection connection, ResultCode result);

	void OnReceived(Connection connection, byte[] payload, bool user1);

	void OnSendCompleted(Connection connection, long sendId, ResultCode result);

	/// <summary>
	/// An established connection ended. <paramref name="result"/> is <see cref="ResultCode.Success"/>
	/// when the partner disconnected deliberately and <see cref="ResultCode.ConnectionLost"/> when it timed out.
	/// </summary>
	void OnDisconnected(Connection connection, ResultCode result);
}
