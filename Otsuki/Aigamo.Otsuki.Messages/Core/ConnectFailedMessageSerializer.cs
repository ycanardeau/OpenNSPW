using System.Collections.Immutable;

namespace Aigamo.Otsuki.Messages.Core;

internal class ConnectFailedMessageSerializer : ICoreMessageSerializer<CoreMessage.ConnectFailed>
{
	public static ConnectFailedMessageSerializer Default { get; } = new();

	public virtual CoreMessage.ConnectFailed? Read(BinaryReader reader)
	{
		var packetType = (PacketType)reader.ReadInt32();
		if (packetType != PacketType.ConnectFailed)
			return null;

		var endOfPacketType = reader.BaseStream.Position;
		var resultCode = (ResultCode)reader.ReadInt32();
		var replyOffset = reader.ReadInt32();
		var replySize = reader.ReadInt32();

		var reply = ImmutableArray<byte>.Empty;
		if (replyOffset != 0)
		{
			reader.BaseStream.Seek(endOfPacketType + replyOffset, SeekOrigin.Begin);
			reply = reader.ReadBytes(replySize).ToImmutableArray();
		}

		return new() { ResultCode = resultCode, Reply = reply };
	}

	public virtual void Write(BinaryWriter writer, CoreMessage.ConnectFailed message)
	{
		writer.Write((int)message.PacketType);
		var endOfPacketType = writer.BaseStream.Position;
		var offset = 12 + message.ReplySize;

		var replyOffset = 0;
		if (message.ReplySize != 0)
		{
			replyOffset = offset -= message.ReplySize;
			writer.BaseStream.Seek(endOfPacketType + replyOffset, SeekOrigin.Begin);
			writer.Write(message.Reply.ToArray());
		}

		writer.BaseStream.Seek(endOfPacketType, SeekOrigin.Begin);
		writer.Write((int)message.ResultCode);
		writer.Write(replyOffset);
		writer.Write(message.ReplySize);
	}

	public virtual CoreMessage.ConnectFailed? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(CoreMessage.ConnectFailed message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
