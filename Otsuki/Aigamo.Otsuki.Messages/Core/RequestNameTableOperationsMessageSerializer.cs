namespace Aigamo.Otsuki.Messages.Core;

internal class RequestNameTableOperationsMessageSerializer
	: ICoreMessageSerializer<CoreMessage.RequestNameTableOperations>
{
	public static RequestNameTableOperationsMessageSerializer Default { get; } = new();

	public virtual CoreMessage.RequestNameTableOperations? Read(BinaryReader reader)
	{
		var packetType = (PacketType)reader.ReadInt32();
		if (packetType != PacketType.RequestNameTableOperations)
			return null;

		var version = reader.ReadInt32();
		var versionNotUsed = reader.ReadInt32();

		return new() { Version = version, VersionNotUsed = versionNotUsed };
	}

	public virtual void Write(BinaryWriter writer, CoreMessage.RequestNameTableOperations message)
	{
		writer.Write((int)message.PacketType);
		writer.Write(message.Version);
		writer.Write(message.VersionNotUsed);
	}

	public virtual CoreMessage.RequestNameTableOperations? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(CoreMessage.RequestNameTableOperations message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
