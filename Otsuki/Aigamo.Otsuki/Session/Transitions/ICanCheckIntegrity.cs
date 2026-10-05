using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// The host's side of the integrity check ([MC-DPL8CS] section 3.2.5.3).
/// </summary>
internal interface ICanCheckIntegrity
	: ISessionTransition<Received<CoreMessage.RequestIntegrityCheck>>,
		ISessionTransition<Received<CoreMessage.IntegrityCheckResponse>>,
		ISessionTransition<SessionMessage.IntegrityCheckTimedOut>,
		IHasHostingData
{
	SessionState ISessionTransition<Received<CoreMessage.RequestIntegrityCheck>>.Execute(
		SessionActor session,
		Received<CoreMessage.RequestIntegrityCheck> command
	)
	{
		if (
			!session.PlayersByConnection.TryGetValue(command.Connection, out var requester)
			|| !session.Players.TryGetValue(command.Message.DpnidTarget, out var target)
			|| target.IsLocal
			|| IntegrityChecks.ContainsKey(target.Id)
		)
			return Current;

		if (target.Connection is null)
			return session.DestroyPlayer(this, target, DestroyPlayerFlags.ConnectionLost);

		SessionActor.SendCore(
			target.Connection,
			new CoreMessage.IntegrityCheck { DpnidRequesting = requester.Id }
		);
		var timer = session.Schedule(
			session.Options.IntegrityCheckTimeout,
			new SessionMessage.IntegrityCheckTimedOut(target.Id, requester.Id)
		);
		return ToHosting() with
		{
			IntegrityChecks = IntegrityChecks.Add(target.Id, new(requester.Id, timer)),
		};
	}

	/// <summary>
	/// The checked peer is alive, so the peer that questioned it is the one to remove.
	/// </summary>
	SessionState ISessionTransition<Received<CoreMessage.IntegrityCheckResponse>>.Execute(
		SessionActor session,
		Received<CoreMessage.IntegrityCheckResponse> command
	)
	{
		if (
			!session.PlayersByConnection.TryGetValue(command.Connection, out var target)
			|| !IntegrityChecks.TryGetValue(target.Id, out var check)
		)
			return Current;

		check.Timer.Dispose();
		var hosting = ToHosting() with { IntegrityChecks = IntegrityChecks.Remove(target.Id) };
		if (!session.Players.TryGetValue(check.Requester, out var requester))
			return hosting;

		if (requester.Connection is not null)
			SessionActor.SendCore(requester.Connection, new CoreMessage.TerminateSession());
		return session.DestroyPlayer(hosting, requester, DestroyPlayerFlags.HostDestroyedPlayer);
	}

	/// <summary>
	/// The checked peer did not answer, so it is gone.
	/// </summary>
	SessionState ISessionTransition<SessionMessage.IntegrityCheckTimedOut>.Execute(
		SessionActor session,
		SessionMessage.IntegrityCheckTimedOut command
	)
	{
		if (
			!IntegrityChecks.TryGetValue(command.Target, out var check)
			|| check.Requester != command.Requester
		)
			return Current;

		var hosting = ToHosting() with { IntegrityChecks = IntegrityChecks.Remove(command.Target) };
		return session.Players.TryGetValue(command.Target, out var target)
			? session.DestroyPlayer(hosting, target, DestroyPlayerFlags.ConnectionLost)
			: hosting;
	}
}
