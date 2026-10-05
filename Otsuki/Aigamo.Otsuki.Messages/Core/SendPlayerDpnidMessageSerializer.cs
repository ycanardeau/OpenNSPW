namespace Aigamo.Otsuki.Messages.Core;

internal class SendPlayerDpnidMessageSerializer
	: ICoreMessageSerializer<CoreMessage.SendPlayerDpnid>
{
	public static SendPlayerDpnidMessageSerializer Default { get; } = new();

	public virtual CoreMessage.SendPlayerDpnid? Read(BinaryReader reader)
	{
		var packetType = (PacketType)reader.ReadInt32();
		if (packetType != PacketType.SendPlayerDpnid)
			return null;

		var dpnid = new Dpnid(reader.ReadInt32());

		return new() { Dpnid = dpnid };
	}

	public virtual void Write(BinaryWriter writer, CoreMessage.SendPlayerDpnid message)
	{
		writer.Write((int)message.PacketType);
		writer.Write(message.Dpnid.Value);
	}

	public virtual CoreMessage.SendPlayerDpnid? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(CoreMessage.SendPlayerDpnid message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
