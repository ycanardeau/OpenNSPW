namespace Aigamo.Otsuki.Messages.Core;

internal class HostMigrateMessageSerializer : ICoreMessageSerializer<CoreMessage.HostMigrate>
{
	public static HostMigrateMessageSerializer Default { get; } = new();

	public virtual CoreMessage.HostMigrate? Read(BinaryReader reader)
	{
		var packetType = (PacketType)reader.ReadInt32();
		if (packetType != PacketType.HostMigrate)
			return null;

		var dpnidOldHost = new Dpnid(reader.ReadInt32());
		var dpnidNewHost = new Dpnid(reader.ReadInt32());

		return new() { DpnidOldHost = dpnidOldHost, DpnidNewHost = dpnidNewHost };
	}

	public virtual void Write(BinaryWriter writer, CoreMessage.HostMigrate message)
	{
		writer.Write((int)message.PacketType);
		writer.Write(message.DpnidOldHost.Value);
		writer.Write(message.DpnidNewHost.Value);
	}

	public virtual CoreMessage.HostMigrate? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(CoreMessage.HostMigrate message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
