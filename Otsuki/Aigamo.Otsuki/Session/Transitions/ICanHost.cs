using System.Collections.Immutable;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Results;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// Creates a session with the local player as host ([MC-DPL8CS] section 1.3.3.2).
/// </summary>
internal interface ICanHost : ISessionTransition<SessionMessage.Host>
{
	private static SessionState Host(SessionActor session, SessionMessage.Host command)
	{
		session.Application = command.Application with
		{
			GuidInstance =
				command.Application.GuidInstance == Guid.Empty
					? Guid.NewGuid()
					: command.Application.GuidInstance,
		};

		// Name table version 1 and index 1 belong to the all players group, which is never sent.
		session.Version = 2;
		var host = new Player
		{
			Id = session.CreateDpnid(index: 2, session.Version),
			Name = session.LocalName,
			Data = session.LocalData,
			IsLocal = true,
			IsHost = true,
			Version = session.Version,
		};
		session.Players.Add(host.Id, host);
		session.LocalPlayer = host.Id;
		session.MarkCreated(host);
		command.Completion.TrySetResult();
		return new SessionState.Hosting(
			NextIndex: 3,
			ResynchronizedVersion: 0,
			IntegrityChecks: ImmutableDictionary<Dpnid, IntegrityCheck>.Empty
		);
	}

	SessionState ISessionTransition<SessionMessage.Host>.Execute(
		SessionActor session,
		SessionMessage.Host command
	) =>
		session
			.StartEndpoint(command.LocalEndPoint)
			.Fold(
				onOk: _ => Host(session, command),
				onError: exception =>
				{
					command.Completion.TrySetException(exception);
					return Current;
				}
			);
}
