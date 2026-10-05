namespace Aigamo.Otsuki.Messages.Reliable;

internal class ConnectMessageSerializer : IReliableMessageSerializer<ReliableMessage.Connect>
{
	public static ConnectMessageSerializer Default { get; } = new();

	public virtual ReliableMessage.Connect? Read(BinaryReader reader)
	{
		var command = (PacketCommand)reader.ReadByte();

		var opcode = (ExtendedOpcode)reader.ReadByte();
		if (opcode != ExtendedOpcode.Connect)
			return null;

		var messageId = reader.ReadByte();
		var responseId = reader.ReadByte();
		var protocolVersion = reader.ReadInt32();
		var sessionId = new SessionId(reader.ReadInt32());
		var timestamp = reader.ReadInt32();

		return new()
		{
			Command = command,
			MessageId = messageId,
			ResponseId = responseId,
			ProtocolVersion = protocolVersion,
			SessionId = sessionId,
			Timestamp = timestamp,
		};
	}

	public virtual void Write(BinaryWriter writer, ReliableMessage.Connect message)
	{
		writer.Write((byte)message.Command);
		writer.Write((byte)message.Opcode);
		writer.Write(message.MessageId);
		writer.Write(message.ResponseId);
		writer.Write(message.ProtocolVersion);
		writer.Write(message.SessionId.Value);
		writer.Write(message.Timestamp);
	}

	public virtual ReliableMessage.Connect? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(ReliableMessage.Connect message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
