namespace Aigamo.Otsuki.Messages.Reliable;

internal class SackMessageSerializer(bool signed = false)
	: IReliableMessageSerializer<ReliableMessage.Sack>
{
	public static SackMessageSerializer Default { get; } = new();

	/// <summary>
	/// Reads and writes the frames of a connection established with signing, which carry <b>ullSignature</b>.
	/// </summary>
	public static SackMessageSerializer Signed { get; } = new(signed: true);

	private readonly bool _signed = signed;

	public virtual ReliableMessage.Sack? Read(BinaryReader reader)
	{
		var command = (PacketCommand)reader.ReadByte();

		var opcode = (ExtendedOpcode)reader.ReadByte();
		if (opcode != ExtendedOpcode.Sack)
			return null;

		var flags = (SackFlags)reader.ReadByte();
		var retry = reader.ReadByte();
		var nextSend = new SequenceId(reader.ReadByte());
		var nextReceive = new SequenceId(reader.ReadByte());
		var padding = reader.ReadInt16();
		var timestamp = reader.ReadInt32();
		var sackMask1 = flags.HasFlag(SackFlags.Sack1) ? reader.ReadUInt32() : 0;
		var sackMask2 = flags.HasFlag(SackFlags.Sack2) ? reader.ReadUInt32() : 0;
		var sendMask1 = flags.HasFlag(SackFlags.Send1) ? reader.ReadUInt32() : 0;
		var sendMask2 = flags.HasFlag(SackFlags.Send2) ? reader.ReadUInt32() : 0;
		var signature = _signed ? reader.ReadInt64() : 0;

		// Flags goes last: the mask setters derive their flags from the value, which would otherwise clobber the flags read from the wire (e.g. SACK_MASK1 with a zero mask).
		return new()
		{
			Command = command,
			Retry = retry,
			NextSend = nextSend,
			NextReceive = nextReceive,
			Padding = padding,
			Timestamp = timestamp,
			SackMask1 = sackMask1,
			SackMask2 = sackMask2,
			SendMask1 = sendMask1,
			SendMask2 = sendMask2,
			Signature = signature,
			Flags = flags,
		};
	}

	public virtual void Write(BinaryWriter writer, ReliableMessage.Sack message)
	{
		writer.Write((byte)message.Command);
		writer.Write((byte)message.Opcode);
		writer.Write((byte)message.Flags);
		writer.Write(message.Retry);
		writer.Write(message.NextSend.Value);
		writer.Write(message.NextReceive.Value);
		writer.Write(message.Padding);
		writer.Write(message.Timestamp);

		if (message.Sack1)
			writer.Write(message.SackMask1);

		if (message.Sack2)
			writer.Write(message.SackMask2);

		if (message.Send1)
			writer.Write(message.SendMask1);

		if (message.Send2)
			writer.Write(message.SendMask2);

		if (_signed)
			writer.Write(message.Signature);
	}

	public virtual ReliableMessage.Sack? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(ReliableMessage.Sack message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
