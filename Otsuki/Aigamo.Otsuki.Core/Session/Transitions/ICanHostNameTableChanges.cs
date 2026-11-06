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
		ISessionTransition<SessionMessage.DestroyPlayer>,
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
		SessionCore session,
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
		SessionCore session,
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
		SessionCore session,
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
		session.Reply(command.Completion);
		return Current;
	}

	SessionState ISessionTransition<SessionMessage.CreateGroup>.Execute(
		SessionCore session,
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
		session.Reply(command.Completion, id);
		return state;
	}

	SessionState ISessionTransition<SessionMessage.DestroyGroup>.Execute(
		SessionCore session,
		SessionMessage.DestroyGroup command
	)
	{
		if (!session.Groups.ContainsKey(command.Group))
		{
			session.Fail(command.Completion, 
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
		session.Reply(command.Completion);
		return Current;
	}

	SessionState ISessionTransition<SessionMessage.AddPlayerToGroup>.Execute(
		SessionCore session,
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
					session.Reply(command.Completion);
					return Unit.Default;
				},
				onError: result =>
				{
					session.Fail(command.Completion, 
						Fail(result, $"Cannot add {command.Player} to {command.Group}.")
					);
					return Unit.Default;
				}
			);
		return Current;
	}

	SessionState ISessionTransition<SessionMessage.RemovePlayerFromGroup>.Execute(
		SessionCore session,
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
					session.Reply(command.Completion);
					return Unit.Default;
				},
				onError: result =>
				{
					session.Fail(command.Completion, 
						Fail(result, $"Cannot remove {command.Player} from {command.Group}.")
					);
					return Unit.Default;
				}
			);
		return Current;
	}

	/// <summary>
	/// Removes a player: DN_TERMINATE_SESSION tells it, and DN_DESTROY_PLAYER the others ([MC-DPL8CS] section
	/// 3.2.5.2).
	/// </summary>
	SessionState ISessionTransition<SessionMessage.DestroyPlayer>.Execute(
		SessionCore session,
		SessionMessage.DestroyPlayer command
	)
	{
		if (!session.Players.TryGetValue(command.Player, out var player) || player.IsLocal)
		{
			session.Fail(command.Completion, 
				Fail(ResultCode.InvalidPlayer, $"There is no other player {command.Player}.")
			);
			return Current;
		}

		if (player.Connection is not null)
			session.SendCore(
				player.Connection,
				new CoreMessage.TerminateSession { TerminateData = command.Data }
			);
		var state = session.DestroyPlayer(this, player, DestroyPlayerFlags.HostDestroyedPlayer);
		session.Reply(command.Completion);
		return state;
	}

	/// <summary>
	/// A peer may only update its own information.
	/// </summary>
	SessionState ISessionTransition<Received<CoreMessage.RequestUpdateInfo>>.Execute(
		SessionCore session,
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
		SessionCore session,
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
		SessionCore session,
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
		SessionCore session,
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
		SessionCore session,
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
