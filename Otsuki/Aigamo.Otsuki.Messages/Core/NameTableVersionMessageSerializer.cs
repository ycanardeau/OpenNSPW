namespace Aigamo.Otsuki.Messages.Core;

internal class NameTableVersionMessageSerializer
	: ICoreMessageSerializer<CoreMessage.NameTableVersion>
{
	public static NameTableVersionMessageSerializer Default { get; } = new();

	public virtual CoreMessage.NameTableVersion? Read(BinaryReader reader)
	{
		var packetType = (PacketType)reader.ReadInt32();
		if (packetType != PacketType.NameTableVersion)
			return null;

		var version = reader.ReadInt32();
		var versionNotUsed = reader.ReadInt32();

		return new() { Version = version, VersionNotUsed = versionNotUsed };
	}

	public virtual void Write(BinaryWriter writer, CoreMessage.NameTableVersion message)
	{
		writer.Write((int)message.PacketType);
		writer.Write(message.Version);
		writer.Write(message.VersionNotUsed);
	}

	public virtual CoreMessage.NameTableVersion? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(CoreMessage.NameTableVersion message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
