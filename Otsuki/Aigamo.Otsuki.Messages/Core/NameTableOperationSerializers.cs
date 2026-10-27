using System.Collections.Immutable;
using System.Text;

namespace Aigamo.Otsuki.Messages.Core;

/// <summary>
/// The common shape of the core messages that groups and info updates use: the packet type, fixed 32-bit fields, then
/// variable fields located by offsets from the end of <b>dwPacketType</b>.
/// </summary>
internal abstract class FixedLayoutMessageSerializer<TMessage> : ICoreMessageSerializer<TMessage>
	where TMessage : CoreMessage
{
	private const int PacketTypeSize = 4;

	protected abstract PacketType PacketType { get; }

	protected static byte[] ReadField(byte[] data, int offset, int size)
	{
		if (offset == 0 || size == 0)
			return [];

		if (offset < 0 || size < 0 || PacketTypeSize + offset + size > data.Length)
			throw new EndOfStreamException();

		var field = new byte[size];
		Array.Copy(data, PacketTypeSize + offset, field, 0, size);
		return field;
	}

	protected static string ReadName(byte[] data, int offset, int size) =>
		Encoding.Unicode.GetString(ReadField(data, offset, size)).TrimEnd('\0');

	protected static byte[] EncodeName(string name) =>
		name.Length == 0 ? [] : Encoding.Unicode.GetBytes(name + '\0');

	/// <summary>
	/// Reads the fields after <b>dwPacketType</b>. <paramref name="data"/> is the whole message, for variable fields.
	/// </summary>
	protected abstract TMessage Read(BinaryReader reader, byte[] data);

	/// <summary>
	/// Writes the fields after <b>dwPacketType</b>.
	/// </summary>
	protected abstract void Write(BinaryWriter writer, TMessage message);

	public virtual TMessage? Deserialize(byte[] data)
	{
		try
		{
			using var stream = new MemoryStream(data);
			using var reader = new BinaryReader(stream);
			return (PacketType)reader.ReadInt32() == PacketType ? Read(reader, data) : null;
		}
		catch (EndOfStreamException)
		{
			return null;
		}
	}

	public virtual byte[] Serialize(TMessage message)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		writer.Write((int)message.PacketType);
		Write(writer, message);
		return stream.ToArray();
	}
}

/// <summary>
/// Writes data and a name after a fixed part of <paramref name="fixedSize"/> bytes after <b>dwPacketType</b>, data
/// first, and gives their offsets, 0 for an empty field.
/// </summary>
internal readonly struct VariableFields(int fixedSize, byte[] data, byte[] name)
{
	public byte[] Data { get; } = data;

	public byte[] Name { get; } = name;

	public int DataOffset { get; } = data.Length == 0 ? 0 : fixedSize;

	public int NameOffset { get; } = name.Length == 0 ? 0 : fixedSize + data.Length;

	public void WriteTo(BinaryWriter writer)
	{
		writer.Write(Data);
		writer.Write(Name);
	}
}

internal sealed class RequestCreateGroupMessageSerializer
	: FixedLayoutMessageSerializer<CoreMessage.RequestCreateGroup>
{
	private const int FixedSize = 28;

	public static RequestCreateGroupMessageSerializer Default { get; } = new();

	protected override PacketType PacketType => PacketType.RequestCreateGroup;

	protected override CoreMessage.RequestCreateGroup Read(BinaryReader reader, byte[] data)
	{
		var context = reader.ReadInt32();
		var groupFlags = (GroupFlags)reader.ReadInt32();
		var infoFlags = (InfoFlags)reader.ReadInt32();
		var nameOffset = reader.ReadInt32();
		var nameSize = reader.ReadInt32();
		var dataOffset = reader.ReadInt32();
		var dataSize = reader.ReadInt32();
		return new()
		{
			PacketContext = context,
			GroupFlags = groupFlags,
			InfoFlags = infoFlags,
			Name = ReadName(data, nameOffset, nameSize),
			Data = ReadField(data, dataOffset, dataSize).ToImmutableArray(),
		};
	}

	protected override void Write(BinaryWriter writer, CoreMessage.RequestCreateGroup message)
	{
		var fields = new VariableFields(FixedSize, [.. message.Data], EncodeName(message.Name));
		writer.Write(message.PacketContext);
		writer.Write((int)message.GroupFlags);
		writer.Write((int)message.InfoFlags);
		writer.Write(fields.NameOffset);
		writer.Write(fields.Name.Length);
		writer.Write(fields.DataOffset);
		writer.Write(fields.Data.Length);
		fields.WriteTo(writer);
	}
}

internal sealed class CreateGroupMessageSerializer
	: FixedLayoutMessageSerializer<CoreMessage.CreateGroup>
{
	/// <summary>
	/// <b>dpnidRequesting</b>, <b>dwPacketContext</b> and the 48 bytes of DN_NAMETABLE_ENTRY_INFO.
	/// </summary>
	private const int FixedSize = 56;

	public static CreateGroupMessageSerializer Default { get; } = new();

	protected override PacketType PacketType => PacketType.CreateGroup;

	protected override CoreMessage.CreateGroup Read(BinaryReader reader, byte[] data)
	{
		var requesting = new Dpnid(reader.ReadInt32());
		var context = reader.ReadInt32();
		var dpnid = new Dpnid(reader.ReadInt32());
		var owner = new Dpnid(reader.ReadInt32());
		var flags = (NameTableEntryFlags)reader.ReadInt32();
		var version = reader.ReadInt32();
		var versionNotUsed = reader.ReadInt32();
		var dnetVersion = (DnetVersion)reader.ReadInt32();
		var nameOffset = reader.ReadInt32();
		var nameSize = reader.ReadInt32();
		var dataOffset = reader.ReadInt32();
		var dataSize = reader.ReadInt32();
		reader.ReadInt32(); // dwURLOffset: groups have no address
		reader.ReadInt32(); // dwURLSize
		return new()
		{
			DpnidRequesting = requesting,
			PacketContext = context,
			Group = new()
			{
				Dpnid = dpnid,
				DpnidOwner = owner,
				Flags = flags,
				Version = version,
				VersionNotUsed = versionNotUsed,
				DnetVersion = dnetVersion,
				NameInternal = ReadField(data, nameOffset, nameSize),
				Data = ReadField(data, dataOffset, dataSize).ToImmutableArray(),
			},
		};
	}

	protected override void Write(BinaryWriter writer, CoreMessage.CreateGroup message)
	{
		var group = message.Group;
		var fields = new VariableFields(
			FixedSize,
			[.. group.Data],
			group.NameInternal.ToByteArray()
		);
		writer.Write(message.DpnidRequesting.Value);
		writer.Write(message.PacketContext);
		writer.Write(group.Dpnid.Value);
		writer.Write(group.DpnidOwner.Value);
		writer.Write((int)group.Flags);
		writer.Write(group.Version);
		writer.Write(group.VersionNotUsed);
		writer.Write((int)group.DnetVersion);
		writer.Write(fields.NameOffset);
		writer.Write(fields.Name.Length);
		writer.Write(fields.DataOffset);
		writer.Write(fields.Data.Length);
		writer.Write(0);
		writer.Write(0);
		fields.WriteTo(writer);
	}
}

internal sealed class RequestAddPlayerToGroupMessageSerializer
	: FixedLayoutMessageSerializer<CoreMessage.RequestAddPlayerToGroup>
{
	public static RequestAddPlayerToGroupMessageSerializer Default { get; } = new();

	protected override PacketType PacketType => PacketType.RequestAddPlayerToGroup;

	protected override CoreMessage.RequestAddPlayerToGroup Read(BinaryReader reader, byte[] data) =>
		new()
		{
			PacketContext = reader.ReadInt32(),
			DpnidGroup = new(reader.ReadInt32()),
			DpnidPlayer = new(reader.ReadInt32()),
		};

	protected override void Write(BinaryWriter writer, CoreMessage.RequestAddPlayerToGroup message)
	{
		writer.Write(message.PacketContext);
		writer.Write(message.DpnidGroup.Value);
		writer.Write(message.DpnidPlayer.Value);
	}
}

internal sealed class AddPlayerToGroupMessageSerializer
	: FixedLayoutMessageSerializer<CoreMessage.AddPlayerToGroup>
{
	public static AddPlayerToGroupMessageSerializer Default { get; } = new();

	protected override PacketType PacketType => PacketType.AddPlayerToGroup;

	protected override CoreMessage.AddPlayerToGroup Read(BinaryReader reader, byte[] data) =>
		new()
		{
			DpnidGroup = new(reader.ReadInt32()),
			DpnidPlayer = new(reader.ReadInt32()),
			Version = reader.ReadInt32(),
			VersionNotUsed = reader.ReadInt32(),
			DpnidRequesting = new(reader.ReadInt32()),
			PacketContext = reader.ReadInt32(),
		};

	protected override void Write(BinaryWriter writer, CoreMessage.AddPlayerToGroup message)
	{
		writer.Write(message.DpnidGroup.Value);
		writer.Write(message.DpnidPlayer.Value);
		writer.Write(message.Version);
		writer.Write(message.VersionNotUsed);
		writer.Write(message.DpnidRequesting.Value);
		writer.Write(message.PacketContext);
	}
}

internal sealed class RequestDeletePlayerFromGroupMessageSerializer
	: FixedLayoutMessageSerializer<CoreMessage.RequestDeletePlayerFromGroup>
{
	public static RequestDeletePlayerFromGroupMessageSerializer Default { get; } = new();

	protected override PacketType PacketType => PacketType.RequestDeletePlayerFromGroup;

	protected override CoreMessage.RequestDeletePlayerFromGroup Read(
		BinaryReader reader,
		byte[] data
	) =>
		new()
		{
			PacketContext = reader.ReadInt32(),
			DpnidGroup = new(reader.ReadInt32()),
			DpnidPlayer = new(reader.ReadInt32()),
		};

	protected override void Write(
		BinaryWriter writer,
		CoreMessage.RequestDeletePlayerFromGroup message
	)
	{
		writer.Write(message.PacketContext);
		writer.Write(message.DpnidGroup.Value);
		writer.Write(message.DpnidPlayer.Value);
	}
}

internal sealed class DeletePlayerFromGroupMessageSerializer
	: FixedLayoutMessageSerializer<CoreMessage.DeletePlayerFromGroup>
{
	public static DeletePlayerFromGroupMessageSerializer Default { get; } = new();

	protected override PacketType PacketType => PacketType.DeletePlayerFromGroup;

	protected override CoreMessage.DeletePlayerFromGroup Read(BinaryReader reader, byte[] data) =>
		new()
		{
			DpnidGroup = new(reader.ReadInt32()),
			DpnidPlayer = new(reader.ReadInt32()),
			Version = reader.ReadInt32(),
			VersionNotUsed = reader.ReadInt32(),
			DpnidRequesting = new(reader.ReadInt32()),
			PacketContext = reader.ReadInt32(),
		};

	protected override void Write(BinaryWriter writer, CoreMessage.DeletePlayerFromGroup message)
	{
		writer.Write(message.DpnidGroup.Value);
		writer.Write(message.DpnidPlayer.Value);
		writer.Write(message.Version);
		writer.Write(message.VersionNotUsed);
		writer.Write(message.DpnidRequesting.Value);
		writer.Write(message.PacketContext);
	}
}

internal sealed class RequestDestroyGroupMessageSerializer
	: FixedLayoutMessageSerializer<CoreMessage.RequestDestroyGroup>
{
	public static RequestDestroyGroupMessageSerializer Default { get; } = new();

	protected override PacketType PacketType => PacketType.RequestDestroyGroup;

	protected override CoreMessage.RequestDestroyGroup Read(BinaryReader reader, byte[] data) =>
		new()
		{
			PacketContext = reader.ReadInt32(),
			DpnidGroup = new(reader.ReadInt32()),
			DpnidPlayer = new(reader.ReadInt32()),
		};

	protected override void Write(BinaryWriter writer, CoreMessage.RequestDestroyGroup message)
	{
		writer.Write(message.PacketContext);
		writer.Write(message.DpnidGroup.Value);
		writer.Write(message.DpnidPlayer.Value);
	}
}

internal sealed class DestroyGroupMessageSerializer
	: FixedLayoutMessageSerializer<CoreMessage.DestroyGroup>
{
	public static DestroyGroupMessageSerializer Default { get; } = new();

	protected override PacketType PacketType => PacketType.DestroyGroup;

	protected override CoreMessage.DestroyGroup Read(BinaryReader reader, byte[] data) =>
		new()
		{
			DpnidGroup = new(reader.ReadInt32()),
			Version = reader.ReadInt32(),
			VersionNotUsed = reader.ReadInt32(),
			DpnidRequesting = new(reader.ReadInt32()),
			PacketContext = reader.ReadInt32(),
		};

	protected override void Write(BinaryWriter writer, CoreMessage.DestroyGroup message)
	{
		writer.Write(message.DpnidGroup.Value);
		writer.Write(message.Version);
		writer.Write(message.VersionNotUsed);
		writer.Write(message.DpnidRequesting.Value);
		writer.Write(message.PacketContext);
	}
}

internal sealed class RequestUpdateInfoMessageSerializer
	: FixedLayoutMessageSerializer<CoreMessage.RequestUpdateInfo>
{
	private const int FixedSize = 28;

	public static RequestUpdateInfoMessageSerializer Default { get; } = new();

	protected override PacketType PacketType => PacketType.RequestUpdateInfo;

	protected override CoreMessage.RequestUpdateInfo Read(BinaryReader reader, byte[] data)
	{
		var context = reader.ReadInt32();
		var dpnid = new Dpnid(reader.ReadInt32());
		var infoFlags = (InfoFlags)reader.ReadInt32();
		var nameOffset = reader.ReadInt32();
		var nameSize = reader.ReadInt32();
		var dataOffset = reader.ReadInt32();
		var dataSize = reader.ReadInt32();
		return new()
		{
			PacketContext = context,
			Dpnid = dpnid,
			InfoFlags = infoFlags,
			Name = ReadName(data, nameOffset, nameSize),
			Data = ReadField(data, dataOffset, dataSize).ToImmutableArray(),
		};
	}

	protected override void Write(BinaryWriter writer, CoreMessage.RequestUpdateInfo message)
	{
		var fields = new VariableFields(FixedSize, [.. message.Data], EncodeName(message.Name));
		writer.Write(message.PacketContext);
		writer.Write(message.Dpnid.Value);
		writer.Write((int)message.InfoFlags);
		writer.Write(fields.NameOffset);
		writer.Write(fields.Name.Length);
		writer.Write(fields.DataOffset);
		writer.Write(fields.Data.Length);
		fields.WriteTo(writer);
	}
}

internal sealed class UpdateInfoMessageSerializer
	: FixedLayoutMessageSerializer<CoreMessage.UpdateInfo>
{
	private const int FixedSize = 40;

	public static UpdateInfoMessageSerializer Default { get; } = new();

	protected override PacketType PacketType => PacketType.UpdateInfo;

	protected override CoreMessage.UpdateInfo Read(BinaryReader reader, byte[] data)
	{
		var context = reader.ReadInt32();
		var dpnid = new Dpnid(reader.ReadInt32());
		var version = reader.ReadInt32();
		var versionNotUsed = reader.ReadInt32();
		var infoFlags = (InfoFlags)reader.ReadInt32();
		var nameOffset = reader.ReadInt32();
		var nameSize = reader.ReadInt32();
		var dataOffset = reader.ReadInt32();
		var dataSize = reader.ReadInt32();
		var requesting = new Dpnid(reader.ReadInt32());
		return new()
		{
			PacketContext = context,
			Dpnid = dpnid,
			Version = version,
			VersionNotUsed = versionNotUsed,
			InfoFlags = infoFlags,
			Name = ReadName(data, nameOffset, nameSize),
			Data = ReadField(data, dataOffset, dataSize).ToImmutableArray(),
			DpnidRequesting = requesting,
		};
	}

	protected override void Write(BinaryWriter writer, CoreMessage.UpdateInfo message)
	{
		var fields = new VariableFields(FixedSize, [.. message.Data], EncodeName(message.Name));
		writer.Write(message.PacketContext);
		writer.Write(message.Dpnid.Value);
		writer.Write(message.Version);
		writer.Write(message.VersionNotUsed);
		writer.Write((int)message.InfoFlags);
		writer.Write(fields.NameOffset);
		writer.Write(fields.Name.Length);
		writer.Write(fields.DataOffset);
		writer.Write(fields.Data.Length);
		writer.Write(message.DpnidRequesting.Value);
		fields.WriteTo(writer);
	}
}
