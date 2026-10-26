using System.Collections.Immutable;
using System.Text;
using Aigamo.Otsuki.Messages.Core;

namespace Aigamo.Otsuki.Messages.Enumeration;

/// <summary>
/// Reads and writes <see cref="EnumerationMessage"/>s ([MC-DPLHP] section 2.2).
/// </summary>
public class EnumerationMessageSerializer
{
	private const byte LeadByte = 0x00;

	private const byte QueryCommand = 0x02;

	private const byte ResponseCommand = 0x03;

	private const byte QueryWithGuid = 0x01;

	private const byte QueryWithoutGuid = 0x02;

	/// <summary>
	/// The size of the application description, from <b>ApplicationDescSize</b> through <b>ApplicationGUID</b>.
	/// </summary>
	private const int ApplicationDescSize = 0x50;

	/// <summary>
	/// Offsets in a response count from the end of <b>EnumPayload</b>, the start of <b>ReplyOffset</b>.
	/// </summary>
	private const int OffsetBase = 4;

	/// <summary>
	/// The fixed part of a response, from <b>ReplyOffset</b> through <b>ApplicationGUID</b>.
	/// </summary>
	private const int FixedResponseSize = 8 + ApplicationDescSize;

	public static EnumerationMessageSerializer Default { get; } = new();

	/// <summary>
	/// Whether a datagram is an enumeration message rather than a frame of [MC-DPL8R], whose first byte is never 0.
	/// </summary>
	public static bool IsEnumerationMessage(byte[] datagram) =>
		datagram.Length >= 2 && datagram[0] == LeadByte;

	private static byte[] EncodeString(string value) =>
		value.Length == 0 ? [] : Encoding.Unicode.GetBytes(value + '\0');

	private static string DecodeString(byte[] value) =>
		Encoding.Unicode.GetString(value).TrimEnd('\0');

	private static byte[] ReadAt(byte[] data, int offset, int size) =>
		offset == 0 || size == 0 ? [] : data.Skip(OffsetBase + offset).Take(size).ToArray();

	private static EnumerationMessage.Query? ReadQuery(BinaryReader reader, byte[] data)
	{
		var enumPayload = reader.ReadUInt16();
		var queryType = reader.ReadByte();
		Guid? guidApplication = queryType switch
		{
			QueryWithGuid => new Guid(reader.ReadBytes(16)),
			QueryWithoutGuid => null,
			_ => throw new InvalidDataException($"Unknown query type {queryType}."),
		};
		var position = (int)reader.BaseStream.Position;
		return new()
		{
			EnumPayload = enumPayload,
			GuidApplication = guidApplication,
			ApplicationPayload = data.Skip(position).ToImmutableArray(),
		};
	}

	private static EnumerationMessage.Response? ReadResponse(BinaryReader reader, byte[] data)
	{
		var enumPayload = reader.ReadUInt16();
		var replyOffset = reader.ReadInt32();
		var responseSize = reader.ReadInt32();
		if (reader.ReadInt32() != ApplicationDescSize)
			return null;

		var flags = (SessionFlags)reader.ReadInt32();
		var maxPlayers = reader.ReadInt32();
		var currentPlayers = reader.ReadInt32();
		var sessionNameOffset = reader.ReadInt32();
		var sessionNameSize = reader.ReadInt32();
		reader.ReadInt32(); // PasswordOffset, always 0
		reader.ReadInt32(); // PasswordSize, always 0
		reader.ReadInt32(); // ReservedDataOffset, always 0
		reader.ReadInt32(); // ReservedDataSize, always 0
		var applicationReservedDataOffset = reader.ReadInt32();
		var applicationReservedDataSize = reader.ReadInt32();
		var guidInstance = new Guid(reader.ReadBytes(16));
		var guidApplication = new Guid(reader.ReadBytes(16));

		return new()
		{
			EnumPayload = enumPayload,
			Flags = flags,
			MaxPlayers = maxPlayers,
			CurrentPlayers = currentPlayers,
			GuidInstance = guidInstance,
			GuidApplication = guidApplication,
			SessionName = DecodeString(ReadAt(data, sessionNameOffset, sessionNameSize)),
			ApplicationReservedData = ReadAt(
					data,
					applicationReservedDataOffset,
					applicationReservedDataSize
				)
				.ToImmutableArray(),
			ApplicationData = ReadAt(data, replyOffset, responseSize).ToImmutableArray(),
		};
	}

	public virtual EnumerationMessage? Deserialize(byte[] data)
	{
		try
		{
			if (!IsEnumerationMessage(data))
				return null;

			using var stream = new MemoryStream(data);
			using var reader = new BinaryReader(stream);
			reader.ReadByte();
			return reader.ReadByte() switch
			{
				QueryCommand => ReadQuery(reader, data),
				ResponseCommand => ReadResponse(reader, data),
				_ => null,
			};
		}
		catch (Exception e) when (e is EndOfStreamException or InvalidDataException)
		{
			return null;
		}
	}

	private static byte[] WriteQuery(EnumerationMessage.Query message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		writer.Write(LeadByte);
		writer.Write(QueryCommand);
		writer.Write(message.EnumPayload);
		if (message.GuidApplication is Guid guidApplication)
		{
			writer.Write(QueryWithGuid);
			writer.Write(guidApplication.ToByteArray());
		}
		else
		{
			writer.Write(QueryWithoutGuid);
		}
		writer.Write(message.ApplicationPayload.ToArray());
		return stream.ToArray();
	}

	/// <summary>
	/// Writes the variable fields after the fixed part, in the order SessionName, ApplicationReservedData,
	/// ApplicationData.
	/// </summary>
	private static byte[] WriteResponse(EnumerationMessage.Response message)
	{
		var sessionName = EncodeString(message.SessionName);
		var applicationReservedData = message.ApplicationReservedData.ToArray();
		var applicationData = message.ApplicationData.ToArray();

		var sessionNameOffset = sessionName.Length == 0 ? 0 : FixedResponseSize;
		var applicationReservedDataOffset =
			applicationReservedData.Length == 0 ? 0 : FixedResponseSize + sessionName.Length;
		var replyOffset =
			applicationData.Length == 0
				? 0
				: FixedResponseSize + sessionName.Length + applicationReservedData.Length;

		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		writer.Write(LeadByte);
		writer.Write(ResponseCommand);
		writer.Write(message.EnumPayload);
		writer.Write(replyOffset);
		writer.Write(applicationData.Length);
		writer.Write(ApplicationDescSize);
		writer.Write((int)(message.Flags & ~SessionFlags.NoEnumerations));
		writer.Write(message.MaxPlayers);
		writer.Write(message.CurrentPlayers);
		writer.Write(sessionNameOffset);
		writer.Write(sessionName.Length);
		writer.Write(0); // PasswordOffset: passwords are never sent
		writer.Write(0); // PasswordSize
		writer.Write(0); // ReservedDataOffset: unused
		writer.Write(0); // ReservedDataSize
		writer.Write(applicationReservedDataOffset);
		writer.Write(applicationReservedData.Length);
		writer.Write(message.GuidInstance.ToByteArray());
		writer.Write(message.GuidApplication.ToByteArray());
		writer.Write(sessionName);
		writer.Write(applicationReservedData);
		writer.Write(applicationData);
		return stream.ToArray();
	}

	public virtual byte[] Serialize(EnumerationMessage message) =>
		message.Match(Query: WriteQuery, Response: WriteResponse);
}
