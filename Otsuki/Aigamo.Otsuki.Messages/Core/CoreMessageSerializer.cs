namespace Aigamo.Otsuki.Messages.Core;

public class CoreMessageSerializer : ICoreMessageSerializer<CoreMessage>
{
	public static CoreMessageSerializer Default { get; } = new();

	public virtual CoreMessage? Deserialize(byte[] data)
	{
		try
		{
			var packetType = (PacketType)BitConverter.ToInt32(data, 0);
			return packetType switch
			{
				PacketType.PlayerConnectInfo =>
					PlayerConnectInfoMessageSerializer.Default.Deserialize(data),
				PacketType.ConnectFailed => ConnectFailedMessageSerializer.Default.Deserialize(
					data
				),
				PacketType.SendConnectInfo => SendConnectInfoMessageSerializer.Default.Deserialize(
					data
				),
				PacketType.AddPlayer => AddPlayerMessageSerializer.Default.Deserialize(data),
				PacketType.AckConnectInfo => AckConnectInfoMessageSerializer.Default.Deserialize(
					data
				),
				PacketType.InstructConnect => InstructConnectMessageSerializer.Default.Deserialize(
					data
				),
				PacketType.SendPlayerDpnid => SendPlayerDpnidMessageSerializer.Default.Deserialize(
					data
				),
				PacketType.InstructedConnectFailed =>
					InstructedConnectFailedMessageSerializer.Default.Deserialize(data),
				PacketType.ConnectAttemptFailed =>
					ConnectAttemptFailedMessageSerializer.Default.Deserialize(data),
				PacketType.TerminateSession =>
					TerminateSessionMessageSerializer.Default.Deserialize(data),
				PacketType.DestroyPlayer => DestroyPlayerMessageSerializer.Default.Deserialize(
					data
				),
				PacketType.HostMigrate => HostMigrateMessageSerializer.Default.Deserialize(data),
				PacketType.NameTableVersion =>
					NameTableVersionMessageSerializer.Default.Deserialize(data),
				PacketType.ResyncVersion => ResyncVersionMessageSerializer.Default.Deserialize(
					data
				),
				PacketType.RequestIntegrityCheck =>
					RequestIntegrityCheckMessageSerializer.Default.Deserialize(data),
				PacketType.IntegrityCheck => IntegrityCheckMessageSerializer.Default.Deserialize(
					data
				),
				PacketType.IntegrityCheckResponse =>
					IntegrityCheckResponseMessageSerializer.Default.Deserialize(data),
				PacketType.RequestNameTableOperations =>
					RequestNameTableOperationsMessageSerializer.Default.Deserialize(data),
				PacketType.AckNameTableOperations =>
					AckNameTableOperationsMessageSerializer.Default.Deserialize(data),
				PacketType.HostMigrateComplete =>
					HostMigrateCompleteMessageSerializer.Default.Deserialize(data),
				PacketType.RequestCreateGroup =>
					RequestCreateGroupMessageSerializer.Default.Deserialize(data),
				PacketType.CreateGroup => CreateGroupMessageSerializer.Default.Deserialize(data),
				PacketType.RequestAddPlayerToGroup =>
					RequestAddPlayerToGroupMessageSerializer.Default.Deserialize(data),
				PacketType.AddPlayerToGroup =>
					AddPlayerToGroupMessageSerializer.Default.Deserialize(data),
				PacketType.RequestDeletePlayerFromGroup =>
					RequestDeletePlayerFromGroupMessageSerializer.Default.Deserialize(data),
				PacketType.DeletePlayerFromGroup =>
					DeletePlayerFromGroupMessageSerializer.Default.Deserialize(data),
				PacketType.RequestDestroyGroup =>
					RequestDestroyGroupMessageSerializer.Default.Deserialize(data),
				PacketType.DestroyGroup => DestroyGroupMessageSerializer.Default.Deserialize(data),
				PacketType.RequestUpdateInfo =>
					RequestUpdateInfoMessageSerializer.Default.Deserialize(data),
				PacketType.UpdateInfo => UpdateInfoMessageSerializer.Default.Deserialize(data),
				_ => null,
			};
		}
		catch
		{
			// TODO: trace exception
			return null;
		}
	}

	public virtual byte[] Serialize(CoreMessage message) =>
		message.Match(
			PlayerConnectInfo: static m => PlayerConnectInfoMessageSerializer.Default.Serialize(m),
			ConnectFailed: static m => ConnectFailedMessageSerializer.Default.Serialize(m),
			SendConnectInfo: static m => SendConnectInfoMessageSerializer.Default.Serialize(m),
			AddPlayer: static m => AddPlayerMessageSerializer.Default.Serialize(m),
			AckConnectInfo: static m => AckConnectInfoMessageSerializer.Default.Serialize(m),
			InstructConnect: static m => InstructConnectMessageSerializer.Default.Serialize(m),
			SendPlayerDpnid: static m => SendPlayerDpnidMessageSerializer.Default.Serialize(m),
			InstructedConnectFailed: static m =>
				InstructedConnectFailedMessageSerializer.Default.Serialize(m),
			ConnectAttemptFailed: static m =>
				ConnectAttemptFailedMessageSerializer.Default.Serialize(m),
			TerminateSession: static m => TerminateSessionMessageSerializer.Default.Serialize(m),
			DestroyPlayer: static m => DestroyPlayerMessageSerializer.Default.Serialize(m),
			HostMigrate: static m => HostMigrateMessageSerializer.Default.Serialize(m),
			NameTableVersion: static m => NameTableVersionMessageSerializer.Default.Serialize(m),
			ResyncVersion: static m => ResyncVersionMessageSerializer.Default.Serialize(m),
			RequestIntegrityCheck: static m =>
				RequestIntegrityCheckMessageSerializer.Default.Serialize(m),
			IntegrityCheck: static m => IntegrityCheckMessageSerializer.Default.Serialize(m),
			IntegrityCheckResponse: static m =>
				IntegrityCheckResponseMessageSerializer.Default.Serialize(m),
			RequestNameTableOperations: static m =>
				RequestNameTableOperationsMessageSerializer.Default.Serialize(m),
			AckNameTableOperations: static m =>
				AckNameTableOperationsMessageSerializer.Default.Serialize(m),
			HostMigrateComplete: static m =>
				HostMigrateCompleteMessageSerializer.Default.Serialize(m),
			RequestCreateGroup: static m =>
				RequestCreateGroupMessageSerializer.Default.Serialize(m),
			CreateGroup: static m => CreateGroupMessageSerializer.Default.Serialize(m),
			RequestAddPlayerToGroup: static m =>
				RequestAddPlayerToGroupMessageSerializer.Default.Serialize(m),
			AddPlayerToGroup: static m => AddPlayerToGroupMessageSerializer.Default.Serialize(m),
			RequestDeletePlayerFromGroup: static m =>
				RequestDeletePlayerFromGroupMessageSerializer.Default.Serialize(m),
			DeletePlayerFromGroup: static m =>
				DeletePlayerFromGroupMessageSerializer.Default.Serialize(m),
			RequestDestroyGroup: static m =>
				RequestDestroyGroupMessageSerializer.Default.Serialize(m),
			DestroyGroup: static m => DestroyGroupMessageSerializer.Default.Serialize(m),
			RequestUpdateInfo: static m => RequestUpdateInfoMessageSerializer.Default.Serialize(m),
			UpdateInfo: static m => UpdateInfoMessageSerializer.Default.Serialize(m)
		);
}
