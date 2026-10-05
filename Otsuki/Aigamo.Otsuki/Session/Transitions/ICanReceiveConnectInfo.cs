using System.Collections.Immutable;
using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// Builds the name table from DN_SEND_CONNECT_INFO and acknowledges it ([MC-DPL8CS] section 3.1.5.2, step 6).
/// </summary>
internal interface ICanReceiveConnectInfo
	: ISessionTransition<Received<CoreMessage.SendConnectInfo>>,
		IHasHostConnection
{
	SessionState ISessionTransition<Received<CoreMessage.SendConnectInfo>>.Execute(
		SessionActor session,
		Received<CoreMessage.SendConnectInfo> command
	)
	{
		if (command.Connection != HostConnection)
			return Current;

		var message = command.Message;
		session.Application = session.Application with
		{
			GuidInstance = message.GuidInstance,
			Flags = message.Flags,
			MaxPlayers = message.MaxPlayers,
			SessionName = message.SessionName,
			ApplicationReservedData = [.. message.ApplicationReservedData],
		};
		session.LocalPlayer = message.Dpnid;
		session.Version = message.Version;

		var awaitingPlayers = ImmutableHashSet.CreateBuilder<Dpnid>();
		foreach (var entry in message.NameTableEntries)
		{
			var isLocal = entry.Dpnid == message.Dpnid;
			var player = new Player
			{
				Id = entry.Dpnid,
				Name = isLocal ? session.LocalName : entry.Name,
				Data = isLocal ? session.LocalData : [.. entry.Data],
				Url = new DirectPlayUrl(entry.Url),
				IsLocal = isLocal,
				IsHost = entry.Host,
				Version = entry.Version,
				DnetVersion = entry.DnetVersion,
			};
			session.Players[player.Id] = player;

			if (player.IsHost)
				session.Bind(player, HostConnection);
			else if (!isLocal)
				awaitingPlayers.Add(player.Id);
		}

		if (
			!session.Players.ContainsKey(session.LocalPlayer)
			|| !session.PlayersByConnection.ContainsKey(HostConnection)
		)
			return session.FailConnect(ResultCode.InvalidHostAddress, []);

		SessionActor.SendCore(HostConnection, new CoreMessage.AckConnectInfo());
		return session.CompleteJoin(
			new SessionState.Joining(
				HostConnection,
				Reply: [.. message.Reply],
				InstructConnectReceived: false,
				AwaitingPlayers: awaitingPlayers.ToImmutable()
			)
		);
	}
}
