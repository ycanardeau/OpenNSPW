namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Sends a dedicated SACK when no data frame carried our acknowledgment or send mask in time
/// ([MC-DPL8R] sections 3.1.6.2 and 3.1.6.3).
/// </summary>
internal interface ICanAcknowledge
	: IConnectionTransition<ConnectionMessage.DelayedAcknowledgmentTimerElapsed>,
		IConnectionTransition<ConnectionMessage.DelayedSendMaskTimerElapsed>,
		IHasChannel
{
	ConnectionState IConnectionTransition<ConnectionMessage.DelayedAcknowledgmentTimerElapsed>.Execute(
		Connection connection,
		ConnectionMessage.DelayedAcknowledgmentTimerElapsed command
	)
	{
		Channel.SendSelectiveAcknowledgment();
		return Current;
	}

	ConnectionState IConnectionTransition<ConnectionMessage.DelayedSendMaskTimerElapsed>.Execute(
		Connection connection,
		ConnectionMessage.DelayedSendMaskTimerElapsed command
	)
	{
		Channel.SendSelectiveAcknowledgment();
		return Current;
	}
}
