namespace Aigamo.Otsuki.Messages.Reliable;

public class ReliableMessageSerializer : IReliableMessageSerializer<ReliableMessage>
{
	public static ReliableMessageSerializer Default { get; } = new();

	public virtual ReliableMessage? Deserialize(byte[] data)
	{
		// OPTIMIZE
		try
		{
			if (data.Length < 4)
				return null;

			var command = (PacketCommand)data[0];

			if (command.HasFlag(PacketCommand.Data))
				return DataFrameMessageSerializer.Default.Deserialize(data);

			if (command.HasFlag(PacketCommand.CommandFrame))
			{
				if (data.Length < 12)
					return null;

				if (
					(command != PacketCommand.CommandFrame)
					&& (command != (PacketCommand.CommandFrame | PacketCommand.Poll))
				)
					return null;

				var opcode = (ExtendedOpcode)data[1];
				return opcode switch
				{
					ExtendedOpcode.Connect => ConnectMessageSerializer.Default.Deserialize(data),
					ExtendedOpcode.Connected => ConnectedMessageSerializer.Default.Deserialize(
						data
					),
					ExtendedOpcode.HardDisconnect =>
						HardDisconnectMessageSerializer.Default.Deserialize(data),
					ExtendedOpcode.Sack => SackMessageSerializer.Default.Deserialize(data),
					_ => null,
				};
			}

			return null;
		}
		catch
		{
			// TODO: trace exception
			return null;
		}
	}

	public virtual byte[] Serialize(ReliableMessage message) =>
		message.Match(
			DataFrame: static m => DataFrameMessageSerializer.Default.Serialize(m),
			Connect: static m => ConnectMessageSerializer.Default.Serialize(m),
			Connected: static m => ConnectedMessageSerializer.Default.Serialize(m),
			HardDisconnect: static m => HardDisconnectMessageSerializer.Default.Serialize(m),
			Sack: static m => SackMessageSerializer.Default.Serialize(m)
		);
}
