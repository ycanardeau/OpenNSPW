namespace Aigamo.Otsuki.Messages.Core;

internal class IntegrityCheckResponseMessageSerializer
	: ICoreMessageSerializer<CoreMessage.IntegrityCheckResponse>
{
	public static IntegrityCheckResponseMessageSerializer Default { get; } = new();

	public virtual CoreMessage.IntegrityCheckResponse? Read(BinaryReader reader)
	{
		var packetType = (PacketType)reader.ReadInt32();
		if (packetType != PacketType.IntegrityCheckResponse)
			return null;

		var dpnidRequesting = new Dpnid(reader.ReadInt32());

		return new() { DpnidRequesting = dpnidRequesting };
	}

	public virtual void Write(BinaryWriter writer, CoreMessage.IntegrityCheckResponse message)
	{
		writer.Write((int)message.PacketType);
		writer.Write(message.DpnidRequesting.Value);
	}

	public virtual CoreMessage.IntegrityCheckResponse? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(CoreMessage.IntegrityCheckResponse message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
