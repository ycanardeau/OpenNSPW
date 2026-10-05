using System.Collections.Immutable;

namespace Aigamo.Otsuki.Messages.Core;

internal class SendConnectInfoMessageSerializer
	: ICoreMessageSerializer<CoreMessage.SendConnectInfo>
{
	public static SendConnectInfoMessageSerializer Default { get; } = new();

	public virtual NameTableEntryInfo ReadNameTableEntryInfo(BinaryReader reader)
	{
		var dpnid = new Dpnid(reader.ReadInt32());
		var dpnidOwner = new Dpnid(reader.ReadInt32());
		var flags = (NameTableEntryFlags)reader.ReadInt32();
		var version = reader.ReadInt32();
		var versionNotUsed = reader.ReadInt32();
		var dnetVersion = (DnetVersion)reader.ReadInt32();

		return new()
		{
			Dpnid = dpnid,
			DpnidOwner = dpnidOwner,
			Flags = flags,
			Version = version,
			VersionNotUsed = versionNotUsed,
			DnetVersion = dnetVersion,
		};
	}

	public virtual NameTableMembershipInfo ReadNameTableMembershipInfo(BinaryReader reader)
	{
		var dpnidPlayer = new Dpnid(reader.ReadInt32());
		var dpnidGroup = new Dpnid(reader.ReadInt32());
		var version = reader.ReadInt32();
		var versionNotUsed = reader.ReadInt32();

		return new()
		{
			DpnidPlayer = dpnidPlayer,
			DpnidGroup = dpnidGroup,
			Version = version,
			VersionNotUsed = versionNotUsed,
		};
	}

	public virtual CoreMessage.SendConnectInfo? Read(BinaryReader reader)
	{
		var packetType = (PacketType)reader.ReadInt32();
		if (packetType != PacketType.SendConnectInfo)
			return null;

		var endOfPacketType = reader.BaseStream.Position;
		var replyOffset = reader.ReadInt32();
		var replySize = reader.ReadInt32();

		var size = reader.ReadInt32();
		if (size != 80)
			return null;

		var flags = (SessionFlags)reader.ReadInt32();
		var maxPlayers = reader.ReadInt32();
		var currentPlayers = reader.ReadInt32();
		var sessionNameOffset = reader.ReadInt32();
		var sessionNameSize = reader.ReadInt32();
		var passwordOffset = reader.ReadInt32();
		var passwordSize = reader.ReadInt32();
		var reservedDataOffset = reader.ReadInt32();
		var reservedDataSize = reader.ReadInt32();
		var applicationReservedDataOffset = reader.ReadInt32();
		var applicationReservedDataSize = reader.ReadInt32();
		var guidInstance = new Guid(reader.ReadBytes(Guid.Empty.ToByteArray().Length));
		var guidApplication = new Guid(reader.ReadBytes(Guid.Empty.ToByteArray().Length));
		var dpnid = new Dpnid(reader.ReadInt32());
		var version = reader.ReadInt32();
		var versionNotUsed = reader.ReadInt32();
		var entryCount = reader.ReadInt32();
		var membershipCount = reader.ReadInt32();

		var nameTableEntries = new NameTableEntryInfo[entryCount];
		for (var i = 0; i < nameTableEntries.Length; i++)
		{
			reader.BaseStream.Seek(endOfPacketType + 108 + 48 * i + 24, SeekOrigin.Begin);
			var nameOffset = reader.ReadInt32();
			var nameSize = reader.ReadInt32();
			var dataOffset = reader.ReadInt32();
			var dataSize = reader.ReadInt32();
			var urlOffset = reader.ReadInt32();
			var urlSize = reader.ReadInt32();

			byte[] url;
			if (urlOffset != 0)
			{
				reader.BaseStream.Seek(endOfPacketType + urlOffset, SeekOrigin.Begin);
				url = reader.ReadBytes(urlSize);
			}
			else
				url = Array.Empty<byte>();

			byte[] data;
			if (dataOffset != 0)
			{
				reader.BaseStream.Seek(endOfPacketType + dataOffset, SeekOrigin.Begin);
				data = reader.ReadBytes(dataSize);
			}
			else
				data = Array.Empty<byte>();

			byte[] name;
			if (nameOffset != 0)
			{
				reader.BaseStream.Seek(endOfPacketType + nameOffset, SeekOrigin.Begin);
				name = reader.ReadBytes(nameSize);
			}
			else
				name = Array.Empty<byte>();

			reader.BaseStream.Seek(endOfPacketType + 108 + 48 * i, SeekOrigin.Begin);
			nameTableEntries[i] = ReadNameTableEntryInfo(reader) with
			{
				UrlInternal = url,
				Data = data.ToImmutableArray(),
				NameInternal = name,
			};
		}

		var nameTableMemberships = new NameTableMembershipInfo[membershipCount];
		for (var i = 0; i < nameTableMemberships.Length; i++)
		{
			reader.BaseStream.Seek(
				endOfPacketType + 108 + 48 * entryCount + 16 * i,
				SeekOrigin.Begin
			);
			nameTableMemberships[i] = ReadNameTableMembershipInfo(reader);
		}

		var applicationReservedData = ImmutableArray<byte>.Empty;
		if (applicationReservedDataOffset != 0)
		{
			reader.BaseStream.Seek(
				endOfPacketType + applicationReservedDataOffset,
				SeekOrigin.Begin
			);
			applicationReservedData = reader
				.ReadBytes(applicationReservedDataSize)
				.ToImmutableArray();
		}

		var reservedData = ImmutableArray<byte>.Empty;
		if (reservedDataOffset != 0)
		{
			reader.BaseStream.Seek(endOfPacketType + reservedDataOffset, SeekOrigin.Begin);
			reservedData = reader.ReadBytes(reservedDataSize).ToImmutableArray();
		}

		var password = Array.Empty<byte>();
		if (passwordOffset != 0)
		{
			reader.BaseStream.Seek(endOfPacketType + passwordOffset, SeekOrigin.Begin);
			password = reader.ReadBytes(passwordSize);
		}

		var sessionName = Array.Empty<byte>();
		if (sessionNameOffset != 0)
		{
			reader.BaseStream.Seek(endOfPacketType + sessionNameOffset, SeekOrigin.Begin);
			sessionName = reader.ReadBytes(sessionNameSize);
		}

		var reply = ImmutableArray<byte>.Empty;
		if (replyOffset != 0)
		{
			reader.BaseStream.Seek(endOfPacketType + replyOffset, SeekOrigin.Begin);
			reply = reader.ReadBytes(replySize).ToImmutableArray();
		}

		return new()
		{
			Flags = flags,
			MaxPlayers = maxPlayers,
			CurrentPlayers = currentPlayers,
			GuidInstance = guidInstance,
			GuidApplication = guidApplication,
			Dpnid = dpnid,
			Version = version,
			VersionNotUsed = versionNotUsed,
			NameTableEntries = nameTableEntries.ToImmutableArray(),
			NameTableMemberships = nameTableMemberships.ToImmutableArray(),
			ApplicationReservedData = applicationReservedData,
			ReservedData = reservedData,
			PasswordInternal = password,
			SessionNameInternal = sessionName,
			Reply = reply,
		};
	}

	public virtual void WriteNameTableEntryInfo(BinaryWriter writer, NameTableEntryInfo value)
	{
		writer.Write(value.Dpnid.Value);
		writer.Write(value.DpnidOwner.Value);
		writer.Write((int)value.Flags);
		writer.Write(value.Version);
		writer.Write(value.VersionNotUsed);
		writer.Write((int)value.DnetVersion);
	}

	public virtual void WriteNameTableMembershipInfo(
		BinaryWriter writer,
		NameTableMembershipInfo value
	)
	{
		writer.Write(value.DpnidPlayer.Value);
		writer.Write(value.DpnidGroup.Value);
		writer.Write(value.Version);
		writer.Write(value.VersionNotUsed);
	}

	public virtual void Write(BinaryWriter writer, CoreMessage.SendConnectInfo message)
	{
		writer.Write((int)message.PacketType);
		var endOfPacketType = writer.BaseStream.Position;
		var offset =
			108
			+ 48 * message.EntryCount
			+ 16 * message.MembershipCount
			+ message.NameTableEntries.Sum(e => e.UrlSize + e.DataSize + e.NameSize)
			+ message.ApplicationReservedDataSize
			+ message.ReservedDataSize
			+ message.PasswordSize
			+ message.SessionNameSize
			+ message.ReplySize;

		var replyOffset = 0;
		if (message.ReplySize != 0)
		{
			replyOffset = offset -= message.ReplySize;
			writer.BaseStream.Seek(endOfPacketType + replyOffset, SeekOrigin.Begin);
			writer.Write(message.Reply.ToArray());
		}

		var sessionNameOffset = 0;
		if (message.SessionNameSize != 0)
		{
			sessionNameOffset = offset -= message.SessionNameSize;
			writer.BaseStream.Seek(endOfPacketType + sessionNameOffset, SeekOrigin.Begin);
			writer.Write(message.SessionNameInternal.ToByteArray());
		}

		var passwordOffset = 0;
		if (message.PasswordSize != 0)
		{
			passwordOffset = offset -= message.PasswordSize;
			writer.BaseStream.Seek(endOfPacketType + passwordOffset, SeekOrigin.Begin);
			writer.Write(message.PasswordInternal.ToByteArray());
		}

		var reservedDataOffset = 0;
		if (message.ReservedDataSize != 0)
		{
			reservedDataOffset = offset -= message.ReservedDataSize;
			writer.BaseStream.Seek(endOfPacketType + reservedDataOffset, SeekOrigin.Begin);
			writer.Write(message.ReservedData.ToArray());
		}

		var applicationReservedDataOffset = 0;
		if (message.ApplicationReservedDataSize != 0)
		{
			applicationReservedDataOffset = offset -= message.ApplicationReservedDataSize;
			writer.BaseStream.Seek(
				endOfPacketType + applicationReservedDataOffset,
				SeekOrigin.Begin
			);
			writer.Write(message.ApplicationReservedData.ToArray());
		}

		var nameOffsets = new int[message.EntryCount];
		var dataOffsets = new int[message.EntryCount];
		var urlOffsets = new int[message.EntryCount];
		for (var i = 0; i < message.EntryCount; i++)
		{
			var e = message.NameTableEntries[i];

			nameOffsets[i] = 0;
			if (e.NameSize != 0)
			{
				nameOffsets[i] = offset -= e.NameSize;
				writer.BaseStream.Seek(endOfPacketType + nameOffsets[i], SeekOrigin.Begin);
				writer.Write(e.NameInternal.ToByteArray());
			}

			dataOffsets[i] = 0;
			if (e.DataSize != 0)
			{
				dataOffsets[i] = offset -= e.DataSize;
				writer.BaseStream.Seek(endOfPacketType + dataOffsets[i], SeekOrigin.Begin);
				writer.Write(e.Data.ToArray());
			}

			urlOffsets[i] = 0;
			if (e.UrlSize != 0)
			{
				urlOffsets[i] = offset -= e.UrlSize;
				writer.BaseStream.Seek(endOfPacketType + urlOffsets[i], SeekOrigin.Begin);
				writer.Write(e.UrlInternal.ToByteArray());
			}
		}

		writer.BaseStream.Seek(endOfPacketType, SeekOrigin.Begin);
		writer.Write(replyOffset);
		writer.Write(message.ReplySize);
		writer.Write(message.Size);
		writer.Write((int)message.Flags);
		writer.Write(message.MaxPlayers);
		writer.Write(message.CurrentPlayers);
		writer.Write(sessionNameOffset);
		writer.Write(message.SessionNameSize);
		writer.Write(passwordOffset);
		writer.Write(message.PasswordSize);
		writer.Write(reservedDataOffset);
		writer.Write(message.ReservedDataSize);
		writer.Write(applicationReservedDataOffset);
		writer.Write(message.ApplicationReservedDataSize);
		writer.Write(message.GuidInstance.ToByteArray());
		writer.Write(message.GuidApplication.ToByteArray());
		writer.Write(message.Dpnid.Value);
		writer.Write(message.Version);
		writer.Write(message.VersionNotUsed);
		writer.Write(message.EntryCount);
		writer.Write(message.MembershipCount);

		for (var i = 0; i < message.EntryCount; i++)
		{
			var e = message.NameTableEntries[i];
			WriteNameTableEntryInfo(writer, e);
			writer.Write(nameOffsets[i]);
			writer.Write(e.NameSize);
			writer.Write(dataOffsets[i]);
			writer.Write(e.DataSize);
			writer.Write(urlOffsets[i]);
			writer.Write(e.UrlSize);
		}

		foreach (var m in message.NameTableMemberships)
			WriteNameTableMembershipInfo(writer, m);
	}

	public virtual CoreMessage.SendConnectInfo? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(CoreMessage.SendConnectInfo message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
