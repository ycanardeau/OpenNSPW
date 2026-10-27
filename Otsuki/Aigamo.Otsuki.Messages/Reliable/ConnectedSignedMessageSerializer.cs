namespace Aigamo.Otsuki.Messages.Reliable;

internal class ConnectedSignedMessageSerializer
	: IReliableMessageSerializer<ReliableMessage.ConnectedSigned>
{
	public static ConnectedSignedMessageSerializer Default { get; } = new();

	public virtual ReliableMessage.ConnectedSigned? Read(BinaryReader reader)
	{
		var command = (PacketCommand)reader.ReadByte();

		var opcode = (ExtendedOpcode)reader.ReadByte();
		if (opcode != ExtendedOpcode.ConnectedSigned)
			return null;

		var messageId = reader.ReadByte();
		var responseId = reader.ReadByte();
		var protocolVersion = reader.ReadInt32();
		var sessionId = new SessionId(reader.ReadInt32());
		var timestamp = reader.ReadInt32();
		var connectSignature = reader.ReadUInt64();
		var senderSecret = reader.ReadUInt64();
		var receiverSecret = reader.ReadUInt64();
		var signingOptions = (PacketSigning)reader.ReadInt32();
		var echoTimestamp = reader.ReadInt32();

		return new()
		{
			Command = command,
			MessageId = messageId,
			ResponseId = responseId,
			ProtocolVersion = protocolVersion,
			SessionId = sessionId,
			Timestamp = timestamp,
			ConnectSignature = connectSignature,
			SenderSecret = senderSecret,
			ReceiverSecret = receiverSecret,
			SigningOptions = signingOptions,
			EchoTimestamp = echoTimestamp,
		};
	}

	public virtual void Write(BinaryWriter writer, ReliableMessage.ConnectedSigned message)
	{
		writer.Write((byte)message.Command);
		writer.Write((byte)message.Opcode);
		writer.Write(message.MessageId);
		writer.Write(message.ResponseId);
		writer.Write(message.ProtocolVersion);
		writer.Write(message.SessionId.Value);
		writer.Write(message.Timestamp);
		writer.Write(message.ConnectSignature);
		writer.Write(message.SenderSecret);
		writer.Write(message.ReceiverSecret);
		writer.Write((int)message.SigningOptions);
		writer.Write(message.EchoTimestamp);
	}

	public virtual ReliableMessage.ConnectedSigned? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(ReliableMessage.ConnectedSigned message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
