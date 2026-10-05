namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Retries unacknowledged frames when the retry timer elapses ([MC-DPL8R] section 3.1.6.5).
/// </summary>
internal interface ICanRetransmit
	: IConnectionTransition<ConnectionMessage.RetryTimerElapsed>,
		IHasChannel
{
	ConnectionState IConnectionTransition<ConnectionMessage.RetryTimerElapsed>.Execute(
		Connection connection,
		ConnectionMessage.RetryTimerElapsed command
	) => connection.Settle(Current, Channel, Channel.Retransmit());
}
