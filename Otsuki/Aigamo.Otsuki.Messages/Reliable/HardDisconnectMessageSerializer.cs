namespace Aigamo.Otsuki.Messages.Reliable;

internal class HardDisconnectMessageSerializer
	: IReliableMessageSerializer<ReliableMessage.HardDisconnect>
{
	public static HardDisconnectMessageSerializer Default { get; } = new();

	public virtual ReliableMessage.HardDisconnect? Read(BinaryReader reader)
	{
		var enableSigning =
			false /* TODO */
		;

		var command = (PacketCommand)reader.ReadByte();

		var opcode = (ExtendedOpcode)reader.ReadByte();
		if (opcode != ExtendedOpcode.HardDisconnect)
			return null;

		var messageId = reader.ReadByte();
		var responseId = reader.ReadByte();
		var protocolVersion = reader.ReadInt32();
		var sessionId = new SessionId(reader.ReadInt32());
		var timestamp = reader.ReadInt32();
		var signature = enableSigning ? reader.ReadInt64() : 0;

		return new()
		{
			Command = command,
			MessageId = messageId,
			ResponseId = responseId,
			ProtocolVersion = protocolVersion,
			SessionId = sessionId,
			Timestamp = timestamp,
			Signature = signature,
		};
	}

	public virtual void Write(BinaryWriter writer, ReliableMessage.HardDisconnect message)
	{
		var enableSigning =
			false /* TODO */
		;

		writer.Write((byte)message.Command);
		writer.Write((byte)message.Opcode);
		writer.Write(message.MessageId);
		writer.Write(message.ResponseId);
		writer.Write(message.ProtocolVersion);
		writer.Write(message.SessionId.Value);
		writer.Write(message.Timestamp);

		if (enableSigning)
			writer.Write(message.Signature);
	}

	public virtual ReliableMessage.HardDisconnect? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(ReliableMessage.HardDisconnect message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
