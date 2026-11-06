using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Sends upper-layer data ([MC-DPL8R] section 3.1.4.4).
/// </summary>
internal interface ICanSend
	: IConnectionTransition<ConnectionMessage.SendPayload>,
		IConnectionTransition<ConnectionMessage.Flush>,
		IHasChannel
{
	ConnectionState IConnectionTransition<ConnectionMessage.SendPayload>.Execute(
		ReliableConnection connection,
		ConnectionMessage.SendPayload command
	)
	{
		if (!Channel.TrySend(command) && command.SendId != 0)
			connection.Observer.OnSendCompleted(
				connection.Handle,
				command.SendId,
				ResultCode.NoConnection
			);
		return Current;
	}

	ConnectionState IConnectionTransition<ConnectionMessage.Flush>.Execute(
		ReliableConnection connection,
		ConnectionMessage.Flush command
	)
	{
		Channel.Flush();
		return Current;
	}
}
