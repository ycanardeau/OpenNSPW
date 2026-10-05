namespace Aigamo.Otsuki.Messages.Core;

internal class HostMigrateCompleteMessageSerializer
	: ICoreMessageSerializer<CoreMessage.HostMigrateComplete>
{
	public static HostMigrateCompleteMessageSerializer Default { get; } = new();

	public virtual CoreMessage.HostMigrateComplete? Read(BinaryReader reader)
	{
		var packetType = (PacketType)reader.ReadInt32();
		if (packetType != PacketType.HostMigrateComplete)
			return null;

		return new();
	}

	public virtual void Write(BinaryWriter writer, CoreMessage.HostMigrateComplete message)
	{
		writer.Write((int)message.PacketType);
	}

	public virtual CoreMessage.HostMigrateComplete? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(CoreMessage.HostMigrateComplete message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
