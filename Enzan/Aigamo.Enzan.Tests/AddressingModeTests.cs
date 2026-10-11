namespace Aigamo.Enzan.Tests;

public class AddressingModeTests
{
	private static AddressingMode Read(params byte[] bytes) =>
		new(new BinaryReader(new MemoryStream(bytes)));

	[Fact]
	public void Displacement8IsSignExtended()
	{
		// [eax-0x1C]: mod 1, rm 0, disp8 0xE4.
		Read(0x40 | 0x00, 0xE4).Displacement.Should().Be(0xFFFFFFE4);
	}

	[Fact]
	public void Displacement32IsAsIs()
	{
		// [eax+0x491C54]: mod 2, rm 0, disp32.
		Read(0x80 | 0x00, 0x54, 0x1C, 0x49, 0x00).Displacement.Should().Be(0x491C54);
	}
}
