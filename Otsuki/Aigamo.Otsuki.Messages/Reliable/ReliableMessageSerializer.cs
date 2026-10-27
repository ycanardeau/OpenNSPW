namespace Aigamo.Otsuki.Messages.Reliable;

/// <remarks>
/// The frames of a connection established with signing carry a <b>ullSignature</b> field, so they are read and written
/// by <see cref="Signed"/>; everything else uses <see cref="Default"/>. The handshake frames (CONNECT, CONNECTED and
/// CONNECTED_SIGNED) never carry a signature and read the same either way.
/// </remarks>
/// <param name="signed">Whether DFRAMEs, SACKs and HARD_DISCONNECTs carry <b>ullSignature</b>.</param>
public class ReliableMessageSerializer(bool signed = false)
	: IReliableMessageSerializer<ReliableMessage>
{
	public static ReliableMessageSerializer Default { get; } = new();

	public static ReliableMessageSerializer Signed { get; } = new(signed: true);

	private readonly bool _signed = signed;

	private DataFrameMessageSerializer DataFrameSerializer =>
		_signed ? DataFrameMessageSerializer.Signed : DataFrameMessageSerializer.Default;

	private SackMessageSerializer SackSerializer =>
		_signed ? SackMessageSerializer.Signed : SackMessageSerializer.Default;

	private HardDisconnectMessageSerializer HardDisconnectSerializer =>
		_signed ? HardDisconnectMessageSerializer.Signed : HardDisconnectMessageSerializer.Default;

	public virtual ReliableMessage? Deserialize(byte[] data)
	{
		// OPTIMIZE
		try
		{
			if (data.Length < 4)
				return null;

			var command = (PacketCommand)data[0];

			if (command.HasFlag(PacketCommand.Data))
				return DataFrameSerializer.Deserialize(data);

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
					ExtendedOpcode.ConnectedSigned =>
						ConnectedSignedMessageSerializer.Default.Deserialize(data),
					ExtendedOpcode.HardDisconnect => HardDisconnectSerializer.Deserialize(data),
					ExtendedOpcode.Sack => SackSerializer.Deserialize(data),
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
			DataFrame: m => DataFrameSerializer.Serialize(m),
			Connect: static m => ConnectMessageSerializer.Default.Serialize(m),
			Connected: static m => ConnectedMessageSerializer.Default.Serialize(m),
			ConnectedSigned: static m => ConnectedSignedMessageSerializer.Default.Serialize(m),
			HardDisconnect: m => HardDisconnectSerializer.Serialize(m),
			Sack: m => SackSerializer.Serialize(m)
		);
}
