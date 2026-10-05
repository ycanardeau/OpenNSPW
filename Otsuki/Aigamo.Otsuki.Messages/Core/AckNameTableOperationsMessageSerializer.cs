using System.Collections.Immutable;

namespace Aigamo.Otsuki.Messages.Core;

internal class AckNameTableOperationsMessageSerializer
	: ICoreMessageSerializer<CoreMessage.AckNameTableOperations>
{
	public static AckNameTableOperationsMessageSerializer Default { get; } = new();

	public virtual CoreMessage.AckNameTableOperations? Read(BinaryReader reader)
	{
		var packetType = (PacketType)reader.ReadInt32();
		if (packetType != PacketType.AckNameTableOperations)
			return null;

		var endOfPacketType = reader.BaseStream.Position;
		var numEntries = reader.ReadInt32();

		var entries = new CoreMessage.AckNameTableOperations.Entry[numEntries];
		for (var i = 0; i < entries.Length; i++)
		{
			reader.BaseStream.Seek(endOfPacketType + 4 + 12 * i + 4, SeekOrigin.Begin);
			var operationOffset = reader.ReadInt32();
			var operationSize = reader.ReadInt32();

			byte[] operation;
			if (operationOffset != 0)
			{
				reader.BaseStream.Seek(endOfPacketType + operationOffset, SeekOrigin.Begin);
				operation = reader.ReadBytes(operationSize);
			}
			else
				operation = Array.Empty<byte>();

			reader.BaseStream.Seek(endOfPacketType + 4 + 12 * i, SeekOrigin.Begin);
			entries[i] = new CoreMessage.AckNameTableOperations.Entry(reader)
			{
				Operation = operation.ToImmutableArray(),
			};
		}

		return new() { EntriesInternal = entries.ToImmutableArray() };
	}

	public virtual void Write(BinaryWriter writer, CoreMessage.AckNameTableOperations message)
	{
		writer.Write((int)message.PacketType);
		var endOfPacketType = writer.BaseStream.Position;
		var offset =
			4 + 12 * message.NumEntries + message.EntriesInternal.Sum(e => e.OperationSize);

		var operationOffsets = new int[message.NumEntries];
		for (var i = 0; i < message.NumEntries; i++)
		{
			var e = message.EntriesInternal[i];

			operationOffsets[i] = 0;
			if (e.OperationSize != 0)
			{
				operationOffsets[i] = offset -= e.OperationSize;
				writer.BaseStream.Seek(endOfPacketType + operationOffsets[i], SeekOrigin.Begin);
				writer.Write(e.Operation.ToArray());
			}
		}

		writer.BaseStream.Seek(endOfPacketType, SeekOrigin.Begin);
		writer.Write(message.NumEntries);

		for (var i = 0; i < message.NumEntries; i++)
		{
			var e = message.EntriesInternal[i];
			writer.Write(e.ToByteArray());
			writer.Write(operationOffsets[i]);
			writer.Write(e.OperationSize);
		}
	}

	public virtual CoreMessage.AckNameTableOperations? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(CoreMessage.AckNameTableOperations message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
