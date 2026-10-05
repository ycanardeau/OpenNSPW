namespace Aigamo.Otsuki.Messages.Core;

internal class ResyncVersionMessageSerializer : ICoreMessageSerializer<CoreMessage.ResyncVersion>
{
	public static ResyncVersionMessageSerializer Default { get; } = new();

	public virtual CoreMessage.ResyncVersion? Read(BinaryReader reader)
	{
		var packetType = (PacketType)reader.ReadInt32();
		if (packetType != PacketType.ResyncVersion)
			return null;

		var version = reader.ReadInt32();
		var versionNotUsed = reader.ReadInt32();

		return new() { Version = version, VersionNotUsed = versionNotUsed };
	}

	public virtual void Write(BinaryWriter writer, CoreMessage.ResyncVersion message)
	{
		writer.Write((int)message.PacketType);
		writer.Write(message.Version);
		writer.Write(message.VersionNotUsed);
	}

	public virtual CoreMessage.ResyncVersion? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(CoreMessage.ResyncVersion message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
