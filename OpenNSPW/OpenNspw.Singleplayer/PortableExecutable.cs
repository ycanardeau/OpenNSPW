using System.Buffers.Binary;
using System.Text;

namespace OpenNspw.Singleplayer;

// What the emulator needs of a 32-bit Windows executable: its image base, its sections and its imports.
internal sealed class PortableExecutable(
	uint imageBase,
	uint imageSize,
	IReadOnlyList<PortableExecutable.Section> sections,
	IReadOnlyList<PortableExecutable.Import> imports
)
{
	// A section: where it is loaded (an address), how much of it is in memory, and where its bytes are in the file.
	public sealed record Section(string Name, uint Address, uint VirtualSize, uint RawOffset, uint RawSize);

	// An imported function, by the address of its slot in the import table.
	public sealed record Import(string Library, string Name, uint Slot);

	public uint ImageBase { get; } = imageBase;
	public uint ImageSize { get; } = imageSize;
	public IReadOnlyList<Section> Sections { get; } = sections;
	public IReadOnlyList<Import> Imports { get; } = imports;

	private static uint UInt32(ReadOnlySpan<byte> file, uint offset) =>
		BinaryPrimitives.ReadUInt32LittleEndian(file[(int)offset..]);

	private static ushort UInt16(ReadOnlySpan<byte> file, uint offset) =>
		BinaryPrimitives.ReadUInt16LittleEndian(file[(int)offset..]);

	private static string CString(ReadOnlySpan<byte> file, uint offset)
	{
		var bytes = file[(int)offset..];
		return Encoding.ASCII.GetString(bytes[..bytes.IndexOf((byte)0)]);
	}

	// The offset in the file of a relative virtual address.
	private static uint FileOffset(IReadOnlyList<Section> sections, uint imageBase, uint rva)
	{
		var address = imageBase + rva;
		var section = sections.First(s => address >= s.Address && address < s.Address + Math.Max(s.VirtualSize, s.RawSize));
		return address - section.Address + section.RawOffset;
	}

	private static List<Import> ReadImports(ReadOnlySpan<byte> file, IReadOnlyList<Section> sections, uint imageBase, uint directory)
	{
		var imports = new List<Import>();
		for (var descriptor = FileOffset(sections, imageBase, directory); ; descriptor += 20)
		{
			var lookupTable = UInt32(file, descriptor);
			var name = UInt32(file, descriptor + 12);
			var addressTable = UInt32(file, descriptor + 16);
			if (name == 0)
				return imports;

			var library = CString(file, FileOffset(sections, imageBase, name));
			var thunks = FileOffset(sections, imageBase, lookupTable != 0 ? lookupTable : addressTable);
			for (var i = 0u; ; i++)
			{
				var thunk = UInt32(file, thunks + 4 * i);
				if (thunk == 0)
					break;

				var function = (thunk & 0x80000000) != 0
					? $"#{thunk & 0xFFFF}"
					: CString(file, FileOffset(sections, imageBase, thunk) + 2);
				imports.Add(new(library, function, imageBase + addressTable + 4 * i));
			}
		}
	}

	public static PortableExecutable Read(ReadOnlySpan<byte> file)
	{
		var header = UInt32(file, 0x3C);
		var sectionCount = UInt16(file, header + 6);
		var optionalHeader = header + 24;
		var optionalHeaderSize = UInt16(file, header + 20);
		var imageBase = UInt32(file, optionalHeader + 28);
		var imageSize = UInt32(file, optionalHeader + 56);
		var importDirectory = UInt32(file, optionalHeader + 104);

		var sections = new List<Section>();
		for (var i = 0u; i < sectionCount; i++)
		{
			var entry = optionalHeader + optionalHeaderSize + 40 * i;
			sections.Add(
				new(
					Encoding.ASCII.GetString(file.Slice((int)entry, 8)).TrimEnd('\0'),
					imageBase + UInt32(file, entry + 12),
					UInt32(file, entry + 8),
					UInt32(file, entry + 20),
					UInt32(file, entry + 16)
				)
			);
		}

		return new(imageBase, imageSize, sections, ReadImports(file, sections, imageBase, importDirectory));
	}
}
