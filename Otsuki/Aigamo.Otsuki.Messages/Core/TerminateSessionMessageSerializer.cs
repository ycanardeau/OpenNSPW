using System.Collections.Immutable;

namespace Aigamo.Otsuki.Messages.Core;

internal class TerminateSessionMessageSerializer
	: ICoreMessageSerializer<CoreMessage.TerminateSession>
{
	public static TerminateSessionMessageSerializer Default { get; } = new();

	public virtual CoreMessage.TerminateSession? Read(BinaryReader reader)
	{
		var packetType = (PacketType)reader.ReadInt32();
		if (packetType != PacketType.TerminateSession)
			return null;

		var endOfPacketType = reader.BaseStream.Position;
		var terminateDataOffset = reader.ReadInt32();
		var terminateDataSize = reader.ReadInt32();

		var terminateData = ImmutableArray<byte>.Empty;
		if (terminateDataOffset != 0)
		{
			reader.BaseStream.Seek(endOfPacketType + terminateDataOffset, SeekOrigin.Begin);
			terminateData = reader.ReadBytes(terminateDataSize).ToImmutableArray();
		}

		return new() { TerminateData = terminateData };
	}

	public virtual void Write(BinaryWriter writer, CoreMessage.TerminateSession message)
	{
		writer.Write((int)message.PacketType);
		var endOfPacketType = writer.BaseStream.Position;
		var offset = 8 + message.TerminateDataSize;

		var terminateDataOffset = 0;
		if (message.TerminateDataSize != 0)
		{
			terminateDataOffset = offset -= message.TerminateDataSize;
			writer.BaseStream.Seek(endOfPacketType + terminateDataOffset, SeekOrigin.Begin);
			writer.Write(message.TerminateData.ToArray());
		}

		writer.BaseStream.Seek(endOfPacketType, SeekOrigin.Begin);
		writer.Write(terminateDataOffset);
		writer.Write(message.TerminateDataSize);
		writer.Write(message.TerminateData.ToArray());
	}

	public virtual CoreMessage.TerminateSession? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(CoreMessage.TerminateSession message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
