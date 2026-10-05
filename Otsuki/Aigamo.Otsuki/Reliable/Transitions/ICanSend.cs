using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Sends upper-layer data ([MC-DPL8R] section 3.1.4.4).
/// </summary>
internal interface ICanSend : IConnectionTransition<ConnectionMessage.SendPayload>, IHasChannel
{
	ConnectionState IConnectionTransition<ConnectionMessage.SendPayload>.Execute(
		Connection connection,
		ConnectionMessage.SendPayload command
	)
	{
		if (!Channel.TrySend(command))
			connection.Observer.OnSendCompleted(
				connection,
				command.SendId,
				ResultCode.NoConnection
			);
		return Current;
	}
}
