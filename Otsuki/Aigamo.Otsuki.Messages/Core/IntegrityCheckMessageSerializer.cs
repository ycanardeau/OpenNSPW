namespace Aigamo.Otsuki.Messages.Core;

internal class IntegrityCheckMessageSerializer : ICoreMessageSerializer<CoreMessage.IntegrityCheck>
{
	public static IntegrityCheckMessageSerializer Default { get; } = new();

	public virtual CoreMessage.IntegrityCheck? Read(BinaryReader reader)
	{
		var packetType = (PacketType)reader.ReadInt32();
		if (packetType != PacketType.IntegrityCheck)
			return null;

		var dpnidRequesting = new Dpnid(reader.ReadInt32());

		return new() { DpnidRequesting = dpnidRequesting };
	}

	public virtual void Write(BinaryWriter writer, CoreMessage.IntegrityCheck message)
	{
		writer.Write((int)message.PacketType);
		writer.Write(message.DpnidRequesting.Value);
	}

	public virtual CoreMessage.IntegrityCheck? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(CoreMessage.IntegrityCheck message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
