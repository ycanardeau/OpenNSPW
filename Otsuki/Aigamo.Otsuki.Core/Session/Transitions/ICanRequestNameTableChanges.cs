using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// A non-host peer asks the host for group changes and info updates, which only the host makes ([MC-DPL8CS]
/// sections 3.4.5.2 and 3.5). Each completes when the host's broadcast for it comes back, or fails after
/// <see cref="SessionOptions.RequestTimeout"/>. Requests that cannot succeed fail at once.
/// </summary>
internal interface ICanRequestNameTableChanges
	: ISessionTransition<SessionMessage.SetPeerInfo>,
		ISessionTransition<SessionMessage.CreateGroup>,
		ISessionTransition<SessionMessage.DestroyGroup>,
		ISessionTransition<SessionMessage.AddPlayerToGroup>,
		ISessionTransition<SessionMessage.RemovePlayerFromGroup>,
		IHasHostConnection
{
	/// <summary>
	/// Sends a request unless <paramref name="error"/> already says it would be ignored.
	/// </summary>
	private SessionState Request(
		SessionCore session,
		Completion completion,
		ResultCode? error,
		Func<int, CoreMessage> createRequest
	)
	{
		if (error is ResultCode result)
		{
			session.Fail(completion, 
				new DirectPlayException(result, $"The request would fail with {result}.")
			);
			return Current;
		}

		session.SendCore(HostConnection, createRequest(session.BeginRequest(completion)));
		return Current;
	}

	/// <summary>
	/// Only the server of a client/server session has groups; its clients do not even see them.
	/// </summary>
	private static ResultCode? CheckGroups(SessionCore session) =>
		session.IsClientServer ? ResultCode.NotHost : null;

	private static ResultCode? CheckMembership(
		SessionCore session,
		Dpnid groupId,
		Dpnid playerId,
		bool member
	) =>
		CheckGroups(session) is { } result ? result
		: !session.Groups.TryGetValue(groupId, out var group) ? ResultCode.InvalidGroup
		: !session.Players.ContainsKey(playerId) ? ResultCode.InvalidPlayer
		: group.Members.ContainsKey(playerId) != member
			? member ? ResultCode.PlayerNotInGroup
				: ResultCode.PlayerAlreadyInGroup
		: null;

	SessionState ISessionTransition<SessionMessage.SetPeerInfo>.Execute(
		SessionCore session,
		SessionMessage.SetPeerInfo command
	) =>
		Request(
			session,
			command.Completion,
			error: null,
			context => new CoreMessage.RequestUpdateInfo
			{
				PacketContext = context,
				Dpnid = session.LocalPlayer,
				InfoFlags = InfoFlags.Name | InfoFlags.Data,
				Name = command.Name,
				Data = command.Data,
			}
		);

	SessionState ISessionTransition<SessionMessage.CreateGroup>.Execute(
		SessionCore session,
		SessionMessage.CreateGroup command
	)
	{
		if (CheckGroups(session) is { } result)
		{
			session.Fail(command.Completion, 
				new DirectPlayException(result, "Clients cannot create groups.")
			);
			return Current;
		}

		var context = session.BeginRequest(
			id => session.Reply(command.Completion, id),
			e => session.Fail(command.Completion, e)
		);
		session.SendCore(
			HostConnection,
			new CoreMessage.RequestCreateGroup
			{
				PacketContext = context,
				GroupFlags = command.Flags,
				InfoFlags = InfoFlags.Name | InfoFlags.Data,
				Name = command.Name,
				Data = command.Data,
			}
		);
		return Current;
	}

	SessionState ISessionTransition<SessionMessage.DestroyGroup>.Execute(
		SessionCore session,
		SessionMessage.DestroyGroup command
	) =>
		Request(
			session,
			command.Completion,
			CheckGroups(session)
				?? (session.Groups.ContainsKey(command.Group) ? null : ResultCode.InvalidGroup),
			context => new CoreMessage.RequestDestroyGroup
			{
				PacketContext = context,
				DpnidGroup = command.Group,
			}
		);

	SessionState ISessionTransition<SessionMessage.AddPlayerToGroup>.Execute(
		SessionCore session,
		SessionMessage.AddPlayerToGroup command
	) =>
		Request(
			session,
			command.Completion,
			CheckMembership(session, command.Group, command.Player, member: false),
			context => new CoreMessage.RequestAddPlayerToGroup
			{
				PacketContext = context,
				DpnidGroup = command.Group,
				DpnidPlayer = command.Player,
			}
		);

	SessionState ISessionTransition<SessionMessage.RemovePlayerFromGroup>.Execute(
		SessionCore session,
		SessionMessage.RemovePlayerFromGroup command
	) =>
		Request(
			session,
			command.Completion,
			CheckMembership(session, command.Group, command.Player, member: true),
			context => new CoreMessage.RequestDeletePlayerFromGroup
			{
				PacketContext = context,
				DpnidGroup = command.Group,
				DpnidPlayer = command.Player,
			}
		);
}
