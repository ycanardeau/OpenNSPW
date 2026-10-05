using System.Collections.Immutable;
using Flags = Aigamo.Otsuki.Messages.Core.NameTableEntryFlags;

namespace Aigamo.Otsuki.Messages.Core;

internal class AddPlayerMessageSerializer : ICoreMessageSerializer<CoreMessage.AddPlayer>
{
	public static AddPlayerMessageSerializer Default { get; } = new();

	public virtual CoreMessage.AddPlayer? Read(BinaryReader reader)
	{
		var packetType = (PacketType)reader.ReadInt32();
		if (packetType != PacketType.AddPlayer)
			return null;

		var endOfPacketType = reader.BaseStream.Position;
		var dpnid = new Dpnid(reader.ReadInt32());
		var dpnidOwner = new Dpnid(reader.ReadInt32());
		var flags = (Flags)reader.ReadInt32();
		var version = reader.ReadInt32();
		var versionNotUsed = reader.ReadInt32();
		var dnetClientVersion = (DnetVersion)reader.ReadInt32();
		var nameOffset = reader.ReadInt32();
		var nameSize = reader.ReadInt32();
		var dataOffset = reader.ReadInt32();
		var dataSize = reader.ReadInt32();
		var urlOffset = reader.ReadInt32();
		var urlSize = reader.ReadInt32();

		var url = Array.Empty<byte>();
		if (urlOffset != 0)
		{
			reader.BaseStream.Seek(endOfPacketType + urlOffset, SeekOrigin.Begin);
			url = reader.ReadBytes(urlSize);
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
			Dpnid = dpnid,
			DpnidOwner = dpnidOwner,
			Flags = flags,
			Version = version,
			VersionNotUsed = versionNotUsed,
			DnetClientVersion = dnetClientVersion,
			UrlInternal = url,
			Data = data,
			NameInternal = name,
		};
	}

	public virtual void Write(BinaryWriter writer, CoreMessage.AddPlayer message)
	{
		writer.Write((int)message.PacketType);
		var endOfPacketType = writer.BaseStream.Position;
		var offset = 48 + message.UrlSize + message.DataSize + message.NameSize;

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

		var urlOffset = 0;
		if (message.UrlSize != 0)
		{
			urlOffset = offset -= message.UrlSize;
			writer.BaseStream.Seek(endOfPacketType + urlOffset, SeekOrigin.Begin);
			writer.Write(message.UrlInternal.ToByteArray());
		}

		writer.BaseStream.Seek(endOfPacketType, SeekOrigin.Begin);
		writer.Write(message.Dpnid.Value);
		writer.Write(message.DpnidOwner.Value);
		writer.Write((int)message.Flags);
		writer.Write(message.Version);
		writer.Write(message.VersionNotUsed);
		writer.Write((int)message.DnetClientVersion);
		writer.Write(nameOffset);
		writer.Write(message.NameSize);
		writer.Write(dataOffset);
		writer.Write(message.DataSize);
		writer.Write(urlOffset);
		writer.Write(message.UrlSize);
	}

	public virtual CoreMessage.AddPlayer? Deserialize(byte[] data)
	{
		using var stream = new MemoryStream(data);
		using var reader = new BinaryReader(stream);
		return Read(reader);
	}

	public virtual byte[] Serialize(CoreMessage.AddPlayer message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		Write(writer, message);
		return stream.ToArray();
	}
}
