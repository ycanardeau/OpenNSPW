using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Ends the connection when the partner acknowledged our END_STREAM but never sent its own.
/// </summary>
internal interface ICanTimeOutGracefulDisconnect
	: IConnectionTransition<ConnectionMessage.GracefulDisconnectTimerElapsed>,
		IHasChannel
{
	ConnectionState IConnectionTransition<ConnectionMessage.GracefulDisconnectTimerElapsed>.Execute(
		Connection connection,
		ConnectionMessage.GracefulDisconnectTimerElapsed command
	) => connection.Close(Channel, ResultCode.Success);
}
