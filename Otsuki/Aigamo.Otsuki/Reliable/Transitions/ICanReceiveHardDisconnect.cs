using Aigamo.Otsuki.Messages.Core;
using Aigamo.Otsuki.Messages.Reliable;

namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Acknowledges the partner's HARD_DISCONNECT and ends the connection ([MC-DPL8R] section 3.1.5.1.4).
/// </summary>
internal interface ICanReceiveHardDisconnect
	: IConnectionTransition<ReliableMessage.HardDisconnect>,
		IHasHandshake,
		IHasChannel
{
	ConnectionState IConnectionTransition<ReliableMessage.HardDisconnect>.Execute(
		Connection connection,
		ReliableMessage.HardDisconnect message
	)
	{
		if (message.SessionId != Handshake.SessionId)
			return Current;

		for (var i = 0; i < connection.Profile.HardDisconnectCount; i++)
			connection.Transmit(Handshake.CreateHardDisconnect(connection.Timestamp));
		return connection.Close(Channel, ResultCode.Success);
	}
}
