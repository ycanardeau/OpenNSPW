using System.Collections.Immutable;
using Aigamo.Otsuki.Messages.Core;
using Aigamo.Results;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// The host's side of groups and info updates: it carries out its own changes and the ones peers request, and
/// broadcasts each to every peer ([MC-DPL8CS] sections 3.4.5.2 and 3.5). A request that is not valid is ignored, as
/// section 3.5 says; the requester's request times out.
/// </summary>
internal interface ICanHostNameTableChanges
	: ISessionTransition<SessionMessage.SetPeerInfo>,
		ISessionTransition<SessionMessage.CreateGroup>,
		ISessionTransition<SessionMessage.DestroyGroup>,
		ISessionTransition<SessionMessage.AddPlayerToGroup>,
		ISessionTransition<SessionMessage.RemovePlayerFromGroup>,
		ISessionTransition<Received<CoreMessage.RequestUpdateInfo>>,
		ISessionTransition<Received<CoreMessage.RequestCreateGroup>>,
		ISessionTransition<Received<CoreMessage.RequestDestroyGroup>>,
		ISessionTransition<Received<CoreMessage.RequestAddPlayerToGroup>>,
		ISessionTransition<Received<CoreMessage.RequestDeletePlayerFromGroup>>,
		IHasHostingData
{
	private static DirectPlayException Fail(ResultCode result, string message) =>
		new(result, message);

	/// <summary>
	/// Creates a group with the next name table index and tells the peers ([MC-DPL8CS] section 3.4.5.2, step 2).
	/// </summary>
	private SessionState CreateGroup(
		SessionActor session,
		string name,
		ImmutableArray<byte> data,
		GroupFlags flags,
		Dpnid owner,
		int context,
		out Dpnid id
	)
	{
		session.Version++;
		var group = new Group
		{
			Id = session.CreateDpnid(NextIndex, session.Version),
			Name = name,
			Data = data,
			Flags = flags,
			Owner = owner,
			Version = session.Version,
		};
		id = group.Id;
		session.AddGroup(group, announce: true);
		session.Announce(
			new CoreMessage.CreateGroup
			{
				DpnidRequesting = owner,
				PacketContext = context,
				Group = group.ToNameTableEntryInfo(),
			}
		);
		return ToHosting() with { NextIndex = NextIndex + 1 };
	}

	private static Result<Unit, ResultCode> ValidateMembership(
		SessionActor session,
		Dpnid groupId,
		Dpnid playerId,
		bool member
	)
	{
		if (!session.Groups.TryGetValue(groupId, out var group))
			return ResultCode.InvalidGroup;

		if (!session.Players.ContainsKey(playerId))
			return ResultCode.InvalidPlayer;

		if (group.Members.ContainsKey(playerId) != member)
			return member ? ResultCode.PlayerNotInGroup : ResultCode.PlayerAlreadyInGroup;

		return Unit.Default;
	}

	SessionState ISessionTransition<SessionMessage.SetPeerInfo>.Execute(
		SessionActor session,
		SessionMessage.SetPeerInfo command
	)
	{
		session.HostUpdateInfo(
			session.LocalPlayer,
			InfoFlags.Name | InfoFlags.Data,
			command.Name,
			command.Data,
			session.LocalPlayer,
			context: 0
		);
		command.Completion.TrySetResult();
		return Current;
	}

	SessionState ISessionTransition<SessionMessage.CreateGroup>.Execute(
		SessionActor session,
		SessionMessage.CreateGroup command
	)
	{
		var state = CreateGroup(
			session,
			command.Name,
			command.Data,
			command.Flags,
			session.LocalPlayer,
			context: 0,
			out var id
		);
		command.Completion.TrySetResult(id);
		return state;
	}

	SessionState ISessionTransition<SessionMessage.DestroyGroup>.Execute(
		SessionActor session,
		SessionMessage.DestroyGroup command
	)
	{
		if (!session.Groups.ContainsKey(command.Group))
		{
			command.Completion.TrySetException(
				Fail(ResultCode.InvalidGroup, $"There is no group {command.Group}.")
			);
			return Current;
		}

		session.HostDestroyGroup(
			command.Group,
			session.LocalPlayer,
			context: 0,
			DestroyGroupReason.Normal
		);
		command.Completion.TrySetResult();
		return Current;
	}

	SessionState ISessionTransition<SessionMessage.AddPlayerToGroup>.Execute(
		SessionActor session,
		SessionMessage.AddPlayerToGroup command
	)
	{
		ValidateMembership(session, command.Group, command.Player, member: false)
			.Fold(
				onOk: _ =>
				{
					session.HostAddMember(
						command.Group,
						command.Player,
						session.LocalPlayer,
						context: 0
					);
					command.Completion.TrySetResult();
					return Unit.Default;
				},
				onError: result =>
				{
					command.Completion.TrySetException(
						Fail(result, $"Cannot add {command.Player} to {command.Group}.")
					);
					return Unit.Default;
				}
			);
		return Current;
	}

	SessionState ISessionTransition<SessionMessage.RemovePlayerFromGroup>.Execute(
		SessionActor session,
		SessionMessage.RemovePlayerFromGroup command
	)
	{
		ValidateMembership(session, command.Group, command.Player, member: true)
			.Fold(
				onOk: _ =>
				{
					session.HostRemoveMember(
						command.Group,
						command.Player,
						session.LocalPlayer,
						context: 0
					);
					command.Completion.TrySetResult();
					return Unit.Default;
				},
				onError: result =>
				{
					command.Completion.TrySetException(
						Fail(result, $"Cannot remove {command.Player} from {command.Group}.")
					);
					return Unit.Default;
				}
			);
		return Current;
	}

	/// <summary>
	/// A peer may only update its own information.
	/// </summary>
	SessionState ISessionTransition<Received<CoreMessage.RequestUpdateInfo>>.Execute(
		SessionActor session,
		Received<CoreMessage.RequestUpdateInfo> command
	)
	{
		var message = command.Message;
		if (
			!session.PlayersByConnection.TryGetValue(command.Connection, out var requester)
			|| requester.Id != message.Dpnid
		)
			return Current;

		session.HostUpdateInfo(
			message.Dpnid,
			message.InfoFlags & (InfoFlags.Name | InfoFlags.Data),
			message.Name,
			[.. message.Data],
			requester.Id,
			message.PacketContext
		);
		return Current;
	}

	SessionState ISessionTransition<Received<CoreMessage.RequestCreateGroup>>.Execute(
		SessionActor session,
		Received<CoreMessage.RequestCreateGroup> command
	)
	{
		var message = command.Message;
		if (!session.PlayersByConnection.TryGetValue(command.Connection, out var requester))
			return Current;

		return CreateGroup(
			session,
			message.InfoFlags.HasFlag(InfoFlags.Name) ? message.Name : string.Empty,
			message.InfoFlags.HasFlag(InfoFlags.Data) ? [.. message.Data] : [],
			message.GroupFlags & GroupFlags.AutoDestruct,
			requester.Id,
			message.PacketContext,
			out _
		);
	}

	SessionState ISessionTransition<Received<CoreMessage.RequestDestroyGroup>>.Execute(
		SessionActor session,
		Received<CoreMessage.RequestDestroyGroup> command
	)
	{
		if (
			session.PlayersByConnection.TryGetValue(command.Connection, out var requester)
			&& session.Groups.ContainsKey(command.Message.DpnidGroup)
		)
			session.HostDestroyGroup(
				command.Message.DpnidGroup,
				requester.Id,
				command.Message.PacketContext,
				DestroyGroupReason.Normal
			);
		return Current;
	}

	SessionState ISessionTransition<Received<CoreMessage.RequestAddPlayerToGroup>>.Execute(
		SessionActor session,
		Received<CoreMessage.RequestAddPlayerToGroup> command
	)
	{
		var message = command.Message;
		if (
			session.PlayersByConnection.TryGetValue(command.Connection, out var requester)
			&& ValidateMembership(session, message.DpnidGroup, message.DpnidPlayer, member: false)
				.Fold(onOk: _ => true, onError: _ => false)
		)
			session.HostAddMember(
				message.DpnidGroup,
				message.DpnidPlayer,
				requester.Id,
				message.PacketContext
			);
		return Current;
	}

	SessionState ISessionTransition<Received<CoreMessage.RequestDeletePlayerFromGroup>>.Execute(
		SessionActor session,
		Received<CoreMessage.RequestDeletePlayerFromGroup> command
	)
	{
		var message = command.Message;
		if (
			session.PlayersByConnection.TryGetValue(command.Connection, out var requester)
			&& ValidateMembership(session, message.DpnidGroup, message.DpnidPlayer, member: true)
				.Fold(onOk: _ => true, onError: _ => false)
		)
			session.HostRemoveMember(
				message.DpnidGroup,
				message.DpnidPlayer,
				requester.Id,
				message.PacketContext
			);
		return Current;
	}
}
