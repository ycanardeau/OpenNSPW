using System.Collections.Immutable;

namespace Aigamo.Otsuki.Messages.Core;

internal class PlayerConnectInfoMessageSerializer
	: ICoreMessageSerializer<CoreMessage.PlayerConnectInfo>
{
	public static PlayerConnectInfoMessageSerializer Default { get; } = new();

	// DN_INTERNAL_MESSAGE_PLAYER_CONNECT_INFO_EX (dwDNETVersion >= 7) appends the alternate address fields to the header of DN_INTERNAL_MESSAGE_PLAYER_CONNECT_INFO.
	private static bool IsExtended(DnetVersion dnetVersion) => dnetVersion >= DnetVersion.DirectX90;

	private static int HeaderSize(DnetVersion dnetVersion) => IsExtended(dnetVersion) ? 88 : 80;

	public virtual CoreMessage.PlayerConnectInfo? Read(BinaryReader reader)
	{
		var packetType = (PacketType)reader.ReadInt32();
		if (packetType != PacketType.PlayerConnectInfo)
			return null;

		var endOfPacketType = reader.BaseStream.Position;
		var flags = (ObjectType)reader.ReadInt32();
		var dnetVersion = (DnetVersion)reader.ReadInt32();
		var nameOffset = reader.ReadInt32();
		var nameSize = reader.ReadInt32();
		var dataOffset = reader.ReadInt32();
		var dataSize = reader.ReadInt32();
		var passwordOffset = reader.ReadInt32();
		var passwordSize = reader.ReadInt32();
		var connectDataOffset = reader.ReadInt32();
		var connectDataSize = reader.ReadInt32();
		var urlOffset = reader.ReadInt32();
		var urlSize = reader.ReadInt32();
		var guidInstance = new Guid(reader.ReadBytes(Guid.Empty.ToByteArray().Length));
		var guidApplication = new Guid(reader.ReadBytes(Guid.Empty.ToByteArray().Length));
		var alternateAddressDataOffset = IsExtended(dnetVersion) ? reader.ReadInt32() : 0;
		var alternateAddressDataSize = IsExtended(dnetVersion) ? reader.ReadInt32() : 0;

		var alternateAddresses = ImmutableArray<AlternateAddress>.Empty;
		if (alternateAddressDataOffset != 0)
		{
			IEnumerable<AlternateAddress> ReadAlternateAddresses()
			{
				while (
					reader.BaseStream.Position
					< (endOfPacketType + alternateAddressDataOffset + alternateAddressDataSize)
				)
					yield return AlternateAddress.FromBinaryReader(reader);
			}

			reader.BaseStream.Seek(endOfPacketType + alternateAddressDataOffset, SeekOrigin.Begin);
			alternateAddresses = ReadAlternateAddresses().ToImmutableArray();
		}

		var url = Array.Empty<byte>();
		if (urlOffset != 0)
		{
			reader.BaseStream.Seek(endOfPacketType + urlOffset, SeekOrigin.Begin);
			url = reader.ReadBytes(urlSize);
		}

		var connectData = ImmutableArray<byte>.Empty;
		if (connectDataOffset != 0)
		{
			reader.BaseStream.Seek(endOfPacketType + connectDataOffset, SeekOrigin.Begin);
			connectData = reader.ReadBytes(connectDataSize).ToImmutableArray();
		}

		var password = Array.Empty<byte>();
		if (passwordOffset != 0)
		{
			reader.BaseStream.Seek(endOfPacketType + passwordOffset, SeekOrigin.Begin);
			password = reader.ReadBytes(passwordSize);
		}

		var data = ImmutableArray<byte>.Empty;
		if (dataOffset != 0)
		{
			reader.BaseStream.Seek(endOfPacketType + dataOffset, SeekOrigin.Begin);
			data = reader.ReadBytes(dataSize).ToImmutableArray();
		}

		var name = Array.Empty<byte>();
		if (nameOffset != 0)
		{
			reader.BaseStream.Seek(endOfPacketType + nameOffset, SeekOrigin.Begin);
			name = reader.ReadBytes(nameSize);
		}

		return new()
		{
			Flags = flags,
			DnetVersion = dnetVersion,
			GuidInstance = guidInstance,
			GuidApplication = guidApplication,
			AlternateAddresses = alternateAddresses,
			UrlInternal = url,
			ConnectData = connectData,
			PasswordInternal = password,
			Data = data,
			NameInternal = name,
		};
	}

	public virtual void Write(BinaryWriter writer, CoreMessage.PlayerConnectInfo message)
	{
		if (!IsExtended(message.DnetVersion) && message.AlternateAddresses.Count != 0)
			throw new ArgumentException(
				$"Alternate addresses require {nameof(DnetVersion)} {DnetVersion.DirectX90} or later.",
				nameof(message)
			);

		writer.Write((int)message.PacketType);
		var endOfPacketType = writer.BaseStream.Position;
		var offset =
			HeaderSize(message.DnetVersion)
			+ message.AlternateAddressDataSize
			+ message.UrlSize
			+ message.ConnectDataSize
			+ message.PasswordSize
			+ message.DataSize
			+ message.NameSize;

		var nameOffset = 0;
		if (message.NameSize != 0)
		{
			nameOffset = offset -= message.NameSize;
			writer.BaseStream.Seek(endOfPacketType + nameOffset, SeekOrigin.Begin);
			writer.Write(message.NameInternal.ToByteArray());
		}

		var dataOffset = 0;
		if (message.DataSize != 0)
		{
			dataOffset = offset -= message.DataSize;
			writer.BaseStream.Seek(endOfPacketType + dataOffset, SeekOrigin.Begin);
			writer.Write(message.Data.ToArray());
		}

		var passwordOffset = 0;
		if (message.PasswordSize != 0)
		{
			passwordOffset = offset -= message.PasswordSize;
			writer.BaseStream.Seek(endOfPacketType + passwordOffset, SeekOrigin.Begin);
			writer.Write(message.PasswordInternal.ToByteArray());
		}

		var connectDataOffset = 0;
		if (message.ConnectDataSize != 0)
		{
			connectDataOffset = offset -= message.ConnectDataSize;
			writer.BaseStream.Seek(endOfPacketType + connectDataOffset, SeekOrigin.Begin);
			writer.Write(message.ConnectData.ToArray());
		}

		var urlOffset = 0;
		if (message.UrlSize != 0)
		{
			urlOffset = offset -= message.UrlSize;
			writer.BaseStream.Seek(endOfPacketType + urlOffset, SeekOrigin.Begin);
			writer.Write(message.UrlInternal.ToByteArray());
		}

		var alternateAddressDataOffset = 0;
		if (message.AlternateAddressDataSize != 0)
		{
			alternateAddressDataOffset = offset -= message.AlternateAddressDataSize;
			writer.BaseStream.Seek(endOfPacketType + alternateAddressDataOffset, SeekOrigin.Begin);
			writer.Write(message.AlternateAddresses.SelectMany(a => a.ToByteArray()).ToArray());
		}

		writer.BaseStream.Seek(endOfPacketType, SeekOrigin.Begin);
		writer.Write((int)message.Flags);
		writer.Write((int)message.DnetVersion);
		writer.Write(nameOffset);
		writer.Write(message.NameSize);
		writer.Write(dataOffset);
		writer.Write(message.DataSize);
		writer.Write(passwordOffset);
		writer.Write(message.PasswordSize);
		writer.Write(connectDataOffset);
		writer.Write(message.ConnectDataSize);
		writer.Write(urlOffset);
		writer.Write(message.UrlSize);
		writer.Write(message.GuidInstance.ToByteArray());
		writer.Write(message.GuidApplication.ToByteArray());

		if (IsExtended(message.DnetVersion))
		{
			writer.Write(alternateAddressDataOffset);
			writer.Write(message.AlternateAddressDataSize);
		}
	}

	public virtual CoreMessage.PlayerConnectInfo? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(CoreMessage.PlayerConnectInfo message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
