using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Session.Transitions;

/// <summary>
/// Sends DN_INTERNAL_MESSAGE_PLAYER_CONNECT_INFO once connected to the host ([MC-DPL8CS] section 3.1.5.2, step 2).
/// </summary>
internal interface ICanIntroduceSelf
	: ISessionTransition<SessionMessage.ConnectionConnected>,
		IHasHostConnection
{
	SessionState ISessionTransition<SessionMessage.ConnectionConnected>.Execute(
		SessionActor session,
		SessionMessage.ConnectionConnected command
	)
	{
		if (command.Connection != HostConnection)
			return Current;

		SessionActor.SendCore(
			HostConnection,
			new CoreMessage.PlayerConnectInfo
			{
				Flags = ObjectType.Peer,
				DnetVersion = DnetVersion.DirectX90,
				GuidInstance = session.Application.GuidInstance,
				GuidApplication = session.Application.GuidApplication,
				Url = DirectPlayUrl.FromEndPoint(session.LocalEndPoint).Value,
				ConnectData = session.ConnectData,
				Password = session.Application.Password,
				Data = session.LocalData,
				Name = session.LocalName,
			}
		);
		return Current;
	}
}
