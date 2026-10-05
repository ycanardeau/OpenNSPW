using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Messages.Reliable;

namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Treats the partner's HARD_DISCONNECT as the acknowledgment of ours ([MC-DPL8R] section 3.1.5.1.4).
/// </summary>
internal interface ICanCompleteHardDisconnect
	: IConnectionTransition<ReliableMessage.HardDisconnect>,
		IHasHandshake
{
	ConnectionState IConnectionTransition<ReliableMessage.HardDisconnect>.Execute(
		Connection connection,
		ReliableMessage.HardDisconnect message
	)
	{
		if (message.SessionId != Handshake.SessionId)
			return Current;

		connection.Observer.OnDisconnected(connection, ResultCode.Success);
		return connection.Terminate();
	}
}
