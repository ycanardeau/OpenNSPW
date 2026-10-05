namespace Aigamo.Otsuki.Messages.Core;

internal class RequestIntegrityCheckMessageSerializer
	: ICoreMessageSerializer<CoreMessage.RequestIntegrityCheck>
{
	public static RequestIntegrityCheckMessageSerializer Default { get; } = new();

	public virtual CoreMessage.RequestIntegrityCheck? Read(BinaryReader reader)
	{
		var packetType = (PacketType)reader.ReadInt32();
		if (packetType != PacketType.RequestIntegrityCheck)
			return null;

		var requestContext = reader.ReadInt32();
		var dpnidTarget = new Dpnid(reader.ReadInt32());

		return new() { RequestContext = requestContext, DpnidTarget = dpnidTarget };
	}

	public virtual void Write(BinaryWriter writer, CoreMessage.RequestIntegrityCheck message)
	{
		writer.Write((int)message.PacketType);
		writer.Write(message.RequestContext);
		writer.Write(message.DpnidTarget.Value);
	}

	public virtual CoreMessage.RequestIntegrityCheck? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(CoreMessage.RequestIntegrityCheck message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
