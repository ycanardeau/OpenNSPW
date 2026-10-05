using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// Applies the host's name table operations and answers its integrity checks, as a non-host peer.
/// </summary>
internal interface ICanFollowHost
	: ISessionTransition<Received<CoreMessage.AddPlayer>>,
		ISessionTransition<Received<CoreMessage.IntegrityCheck>>,
		IHasHostConnection
{
	/// <summary>
	/// Adds a joining peer, which will connect or be connected to later ([MC-DPL8CS] section 3.1.5.2, step 5).
	/// </summary>
	SessionState ISessionTransition<Received<CoreMessage.AddPlayer>>.Execute(
		SessionActor session,
		Received<CoreMessage.AddPlayer> command
	)
	{
		if (command.Connection != HostConnection)
			return Current;

		var message = command.Message;
		session.Version = message.Version;
		session.Players.TryAdd(
			message.Dpnid,
			new Player
			{
				Id = message.Dpnid,
				Name = message.Name,
				Data = [.. message.Data],
				Url = new DirectPlayUrl(message.Url),
				IsHost = message.Host,
				Version = message.Version,
				DnetVersion = message.DnetClientVersion,
			}
		);
		session.ReportNameTableVersion(HostConnection);
		return Current;
	}

	/// <summary>
	/// Confirms that we are still in the session ([MC-DPL8CS] section 3.2.5.3, step 3).
	/// </summary>
	SessionState ISessionTransition<Received<CoreMessage.IntegrityCheck>>.Execute(
		SessionActor session,
		Received<CoreMessage.IntegrityCheck> command
	)
	{
		if (command.Connection == HostConnection)
			SessionActor.SendCore(
				HostConnection,
				new CoreMessage.IntegrityCheckResponse
				{
					DpnidRequesting = command.Message.DpnidRequesting,
				}
			);
		return Current;
	}
}
