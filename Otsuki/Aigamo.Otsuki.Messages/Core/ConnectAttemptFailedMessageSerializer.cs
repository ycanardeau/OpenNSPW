namespace Aigamo.Otsuki.Messages.Core;

internal class ConnectAttemptFailedMessageSerializer
	: ICoreMessageSerializer<CoreMessage.ConnectAttemptFailed>
{
	public static ConnectAttemptFailedMessageSerializer Default { get; } = new();

	public virtual CoreMessage.ConnectAttemptFailed? Read(BinaryReader reader)
	{
		var packetType = (PacketType)reader.ReadInt32();
		if (packetType != PacketType.ConnectAttemptFailed)
			return null;

		var dpnid = new Dpnid(reader.ReadInt32());

		return new() { Dpnid = dpnid };
	}

	public virtual void Write(BinaryWriter writer, CoreMessage.ConnectAttemptFailed message)
	{
		writer.Write((int)message.PacketType);
		writer.Write(message.Dpnid.Value);
	}

	public virtual CoreMessage.ConnectAttemptFailed? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(CoreMessage.ConnectAttemptFailed message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
