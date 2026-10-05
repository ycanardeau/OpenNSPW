namespace Aigamo.Otsuki.Messages.Core;

internal class InstructConnectMessageSerializer
	: ICoreMessageSerializer<CoreMessage.InstructConnect>
{
	public static InstructConnectMessageSerializer Default { get; } = new();

	public virtual CoreMessage.InstructConnect? Read(BinaryReader reader)
	{
		var packetType = (PacketType)reader.ReadInt32();
		if (packetType != PacketType.InstructConnect)
			return null;

		var dpnid = new Dpnid(reader.ReadInt32());
		var version = reader.ReadInt32();
		var versionNotUsed = reader.ReadInt32();

		return new()
		{
			Dpnid = dpnid,
			Version = version,
			VersionNotUsed = versionNotUsed,
		};
	}

	public virtual void Write(BinaryWriter writer, CoreMessage.InstructConnect message)
	{
		writer.Write((int)message.PacketType);
		writer.Write(message.Dpnid.Value);
		writer.Write(message.Version);
		writer.Write(message.VersionNotUsed);
	}

	public virtual CoreMessage.InstructConnect? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(CoreMessage.InstructConnect message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
