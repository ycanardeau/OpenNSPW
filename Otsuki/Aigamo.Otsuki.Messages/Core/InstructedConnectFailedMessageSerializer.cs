namespace Aigamo.Otsuki.Messages.Core;

internal class InstructedConnectFailedMessageSerializer
	: ICoreMessageSerializer<CoreMessage.InstructedConnectFailed>
{
	public static InstructedConnectFailedMessageSerializer Default { get; } = new();

	public virtual CoreMessage.InstructedConnectFailed? Read(BinaryReader reader)
	{
		var packetType = (PacketType)reader.ReadInt32();
		if (packetType != PacketType.InstructedConnectFailed)
			return null;

		var dpnid = new Dpnid(reader.ReadInt32());

		return new() { Dpnid = dpnid };
	}

	public virtual void Write(BinaryWriter writer, CoreMessage.InstructedConnectFailed message)
	{
		writer.Write((int)message.PacketType);
		writer.Write(message.Dpnid.Value);
	}

	public virtual CoreMessage.InstructedConnectFailed? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(CoreMessage.InstructedConnectFailed message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
