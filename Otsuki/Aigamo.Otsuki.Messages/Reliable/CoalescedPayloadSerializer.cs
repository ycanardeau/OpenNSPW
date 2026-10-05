// Comments from: https://docs.microsoft.com/en-us/openspecs/windows_protocols/mc-dpl8r/0ce3a800-f861-4556-9078-2004c2662ab3

using System.Collections.Immutable;

namespace Aigamo.Otsuki.Messages.Reliable;

/// <summary>
/// Reads and writes the payload of a <see cref="ReliableMessage.DataFrame"/> that has the <b>PACKET_CONTROL_COALESCE</b> flag set.
/// </summary>
public class CoalescedPayloadSerializer
{
	/// <summary>
	/// The buffer MUST NOT contain more than 32 coalesce headers.
	/// </summary>
	private const int MaxPayloadCount = 32;

	/// <summary>
	/// The size of each payload is an 11-bit value.
	/// </summary>
	private const int MaxPayloadSize = 0x7FF;

	private const CoalesceCommand SizeMask =
		CoalesceCommand.CoalesceBig1 | CoalesceCommand.CoalesceBig2 | CoalesceCommand.CoalesceBig3;

	private const CoalesceCommand FramingMask = SizeMask | CoalesceCommand.EndCoalesce;

	public static CoalescedPayloadSerializer Default { get; } = new();

	private static int PaddingSize(int payloadSize) => -payloadSize & 3;

	public virtual IImmutableList<CoalescedPayload>? Read(BinaryReader reader)
	{
		var headers = new List<(int Size, CoalesceCommand Command)>();
		while (true)
		{
			if (headers.Count == MaxPayloadCount)
				return null;

			var size = reader.ReadByte();
			var command = (CoalesceCommand)reader.ReadByte();
			headers.Add((size | ((int)(command & SizeMask) << 5), command & ~FramingMask));

			if (command.HasFlag(CoalesceCommand.EndCoalesce))
				break;
		}

		// Two bytes of padding align the payloads on a 32-bit boundary.
		if (headers.Count % 2 != 0)
			reader.ReadUInt16();

		var payloads = ImmutableArray.CreateBuilder<CoalescedPayload>(headers.Count);
		for (var i = 0; i < headers.Count; i++)
		{
			var (size, command) = headers[i];

			var payload = reader.ReadBytes(size);
			if (payload.Length != size)
				return null;

			if (i != headers.Count - 1)
				reader.ReadBytes(PaddingSize(size));

			payloads.Add(new() { Command = command, Payload = payload.ToImmutableArray() });
		}

		return payloads.MoveToImmutable();
	}

	public virtual void Write(BinaryWriter writer, IReadOnlyList<CoalescedPayload> payloads)
	{
		if (payloads.Count is 0 or > MaxPayloadCount)
			throw new ArgumentException(
				$"A coalesced payload holds between 1 and {MaxPayloadCount} payloads.",
				nameof(payloads)
			);

		if (payloads.Any(p => p.Payload.Count > MaxPayloadSize))
			throw new ArgumentException(
				$"Each coalesced payload holds at most {MaxPayloadSize} bytes.",
				nameof(payloads)
			);

		for (var i = 0; i < payloads.Count; i++)
		{
			var size = payloads[i].Payload.Count;
			var command =
				(payloads[i].Command & ~FramingMask)
				| ((CoalesceCommand)(size >> 5) & SizeMask)
				| (i == payloads.Count - 1 ? CoalesceCommand.EndCoalesce : 0);

			writer.Write((byte)size);
			writer.Write((byte)command);
		}

		if (payloads.Count % 2 != 0)
			writer.Write((ushort)0);

		for (var i = 0; i < payloads.Count; i++)
		{
			writer.Write(payloads[i].Payload.ToArray());

			if (i != payloads.Count - 1)
				writer.Write(new byte[PaddingSize(payloads[i].Payload.Count)]);
		}
	}

	public virtual IImmutableList<CoalescedPayload>? Deserialize(byte[] data)
	{
		try
		{
			using var stream = new MemoryStream(data);
			using var reader = new BinaryReader(stream);
			return Read(reader);
		}
		catch (EndOfStreamException)
		{
			return null;
		}
	}

	public virtual byte[] Serialize(IReadOnlyList<CoalescedPayload> payloads)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, payloads);
		return stream.ToArray();
	}
}
