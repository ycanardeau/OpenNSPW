namespace Aigamo.Otsuki.Messages.Core;

internal class DestroyPlayerMessageSerializer : ICoreMessageSerializer<CoreMessage.DestroyPlayer>
{
	public static DestroyPlayerMessageSerializer Default { get; } = new();

	public virtual CoreMessage.DestroyPlayer? Read(BinaryReader reader)
	{
		var packetType = (PacketType)reader.ReadInt32();
		if (packetType != PacketType.DestroyPlayer)
			return null;

		var dpnidLeaving = new Dpnid(reader.ReadInt32());
		var version = reader.ReadInt32();
		var versionNotUsed = reader.ReadInt32();
		var reason = (DestroyPlayerFlags)reader.ReadInt32();

		return new()
		{
			DpnidLeaving = dpnidLeaving,
			Version = version,
			VersionNotUsed = versionNotUsed,
			Reason = reason,
		};
	}

	public virtual void Write(BinaryWriter writer, CoreMessage.DestroyPlayer message)
	{
		writer.Write((int)message.PacketType);
		writer.Write(message.DpnidLeaving.Value);
		writer.Write(message.Version);
		writer.Write(message.VersionNotUsed);
		writer.Write((int)message.Reason);
	}

	public virtual CoreMessage.DestroyPlayer? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(CoreMessage.DestroyPlayer message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
