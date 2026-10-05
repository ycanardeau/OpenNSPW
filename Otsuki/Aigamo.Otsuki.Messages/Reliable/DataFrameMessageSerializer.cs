using System.Collections.Immutable;

namespace Aigamo.Otsuki.Messages.Reliable;

internal class DataFrameMessageSerializer : IReliableMessageSerializer<ReliableMessage.DataFrame>
{
	public static DataFrameMessageSerializer Default { get; } = new();

	public virtual ReliableMessage.DataFrame? Read(BinaryReader reader)
	{
		var enableSigning =
			false /* TODO */
		;

		var command = (PacketCommand)reader.ReadByte();
		var control = (PacketControl)reader.ReadByte();
		var sequenceId = new SequenceId(reader.ReadByte());
		var nextReceive = new SequenceId(reader.ReadByte());
		var sackMask1 = control.HasFlag(PacketControl.Sack1) ? reader.ReadUInt32() : 0;
		var sackMask2 = control.HasFlag(PacketControl.Sack2) ? reader.ReadUInt32() : 0;
		var sendMask1 = control.HasFlag(PacketControl.Send1) ? reader.ReadUInt32() : 0;
		var sendMask2 = control.HasFlag(PacketControl.Send2) ? reader.ReadUInt32() : 0;
		var signature = enableSigning ? reader.ReadInt64() : 0;
		var sessionId = control.HasFlag(PacketControl.KeepAliveOrCorrelate)
			? new SessionId(reader.ReadInt32())
			: SessionId.Empty;

		static byte[] ReadPayload(Stream source)
		{
			using var stream = new MemoryStream();
			source.CopyTo(stream);
			return stream.ToArray();
		}
		var payload = !control.HasFlag(PacketControl.KeepAliveOrCorrelate)
			? ReadPayload(reader.BaseStream).ToImmutableArray()
			: ImmutableArray<byte>.Empty;

		// Control goes last: the mask and session ID setters derive their flags from the value, which would otherwise clobber the flags read from the wire (e.g. SACK1 with a zero mask).
		return new()
		{
			Command = command,
			SequenceId = sequenceId,
			NextReceive = nextReceive,
			SackMask1 = sackMask1,
			SackMask2 = sackMask2,
			SendMask1 = sendMask1,
			SendMask2 = sendMask2,
			Signature = signature,
			SessionId = sessionId,
			Payload = payload,
			Control = control,
		};
	}

	public virtual void Write(BinaryWriter writer, ReliableMessage.DataFrame message)
	{
		var enableSigning =
			false /* TODO */
		;

		writer.Write((byte)message.Command);
		writer.Write((byte)message.Control);
		writer.Write(message.SequenceId.Value);
		writer.Write(message.NextReceive.Value);

		if (message.Sack1)
			writer.Write(message.SackMask1);

		if (message.Sack2)
			writer.Write(message.SackMask2);

		if (message.Send1)
			writer.Write(message.SendMask1);

		if (message.Send2)
			writer.Write(message.SendMask2);

		if (enableSigning)
			writer.Write(message.Signature);

		if (message.KeepAliveOrCorrelate)
			writer.Write(message.SessionId.Value);
		else
			writer.Write(message.Payload.ToArray());
	}

	public virtual ReliableMessage.DataFrame? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(ReliableMessage.DataFrame message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
