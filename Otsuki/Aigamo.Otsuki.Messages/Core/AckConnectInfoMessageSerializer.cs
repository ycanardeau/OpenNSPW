namespace Aigamo.Otsuki.Messages.Core;

internal class AckConnectInfoMessageSerializer : ICoreMessageSerializer<CoreMessage.AckConnectInfo>
{
	public static AckConnectInfoMessageSerializer Default { get; } = new();

	public virtual CoreMessage.AckConnectInfo? Read(BinaryReader reader)
	{
		var packetType = (PacketType)reader.ReadInt32();
		if (packetType != PacketType.AckConnectInfo)
			return null;

		return new();
	}

	public virtual void Write(BinaryWriter writer, CoreMessage.AckConnectInfo message)
	{
		writer.Write((int)message.PacketType);
	}

	public virtual CoreMessage.AckConnectInfo? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(CoreMessage.AckConnectInfo message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
