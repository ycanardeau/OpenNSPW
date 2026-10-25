namespace OpenNspw.Singleplayer;

// The image of the single-player executable as Windows loads it, read in place from the file: what its memory holds
// before its code runs, as the emulator's memory starts, but the import table, which the emulator fills with its stubs.
public sealed class ExecutableImage(byte[] file)
{
	private readonly byte[] _file = file;
	private readonly PortableExecutable _executable = PortableExecutable.Read(file);

	public uint ImageBase => _executable.ImageBase;

	public uint ImageSize => _executable.ImageSize;

	// The byte at the address: the file's in the raw data of a section, and 0 in the rest of a section, which Windows
	// zeroes, and outside the sections.
	public byte ByteAt(uint address)
	{
		foreach (var section in _executable.Sections)
		{
			var offset = address - section.Address;
			if (address >= section.Address && offset < section.VirtualSize)
				return offset < section.RawSize ? _file[section.RawOffset + offset] : (byte)0;
		}

		return 0;
	}
}
