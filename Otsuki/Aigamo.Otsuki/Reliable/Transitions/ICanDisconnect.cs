namespace Aigamo.Otsuki.Reliable.Transitions;

/// <summary>
/// Disconnects gracefully ([MC-DPL8R] section 3.1.4.3).
/// </summary>
internal interface ICanDisconnect : IConnectionTransition<ConnectionMessage.Disconnect>, IHasChannel
{
	ConnectionState IConnectionTransition<ConnectionMessage.Disconnect>.Execute(
		Connection connection,
		ConnectionMessage.Disconnect command
	)
	{
		Channel.Disconnect();
		return Current;
	}
}
