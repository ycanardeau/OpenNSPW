using System.Buffers.Binary;
using OpenNspw.Desktop;
using Xunit;

namespace OpenNspw.Tests;

// The sprite sheets that the desktop app puts back together from its sprites for the game's surface (SpriteSheet):
// as the game sees them, 16-bit with the color key, they are the original's wherever the game draws from
// (set_sprt_data, and the minimap's frames and dots beside it).
public class SpriteSheetTests
{
	private readonly record struct Area(int X, int Y, int Width, int Height);

	// The sheet's pixels as the game's surface has them (Gdi.LoadBitmap): RGB565, of a 24-bit bitmap, or of 8 bits and
	// a palette.
	private static (int Width, ushort[] Pixels) Decode(byte[] file)
	{
		var pixelsOffset = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(10));
		var paletteOffset = 14 + BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(14));
		var width = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(18));
		var height = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(22));
		var bitCount = BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(28));
		var stride = (width * (bitCount / 8) + 3) & ~3;
		var pixels = new ushort[width * height];
		for (var y = 0; y < height; y++)
		{
			var row = pixelsOffset + (height - 1 - y) * stride;
			for (var x = 0; x < width; x++)
			{
				var at = bitCount == 24 ? row + x * 3 : paletteOffset + file[row + x] * 4;
				pixels[y * width + x] = wingdi.ToPixel(
					wingdi.RGB(file[at + 2], file[at + 1], file[at])
				);
			}
		}

		return (width, pixels);
	}

	// Where the game draws from on the sheet, but for the facilities' row's columns 3 and 4, which no facility draws.
	private static IEnumerable<Area> DrawnAreas(bool isNspw122)
	{
		yield return new Area(0, 0, 720, 876); // JapanUnitInfo
		yield return new Area(0, 878, 720, 876); // UnitedStatesUnitInfo
		foreach (var unitsY in new[] { 1759, 4550 }) // JapanUnits, UnitedStatesUnits
		{
			yield return new Area(0, unitsY, 640, 11 * 80);
			yield return new Area(0, unitsY + 11 * 80, 240, 80);
			yield return new Area(400, unitsY + 11 * 80, 240, 80);
			yield return new Area(0, unitsY + 12 * 80, 640, 3 * 80);
		}

		yield return new Area(0, 3000, 480, 240); // MapTiles
		yield return new Area(0, 3250, 321, 221); // Buttons1, two columns
		yield return new Area(361, 3250, 198, 240); // ButtonBase, the two flags
		yield return new Area(0, 4340, 561, 199); // Minimap, its two frames and its dots
		yield return isNspw122 ? new Area(0, 3940, 599, 387) : new Area(0, 3940, 699, 384); // TitleBackground

		// SubUnits, 12 a row of 40 by 40, but for the three icons that only NSPW NET's sheet has, and only its rules
		// draw (docs/SpriteSheets.md).
		for (var frame = 0; frame < 120; frame++)
		{
			if (!isNspw122 || frame is not (16 or 26 or 27))
				yield return new Area(frame % 12 * 40, 3520 + frame / 12 * 40, 40, 40);
		}
	}

	[Theory]
	[InlineData("t3.bmp", false)]
	[InlineData("tst_cg1.bmp", true)]
	public void IsTheOriginalsWhereTheGameDraws(string name, bool isNspw122)
	{
		var original = Path.Combine(
			isNspw122 ? GameData.FindSingleplayer() : GameData.FindNspwNet(),
			name
		);
		var (width, expected) = Decode(File.ReadAllBytes(original));
		var (rebuiltWidth, actual) = Decode(SpriteSheet.Open(name)!);

		Assert.Equal(width, rebuiltWidth);
		Assert.Equal(expected.Length, actual.Length);
		var differences =
			from area in DrawnAreas(isNspw122)
			from y in Enumerable.Range(area.Y, area.Height)
			from x in Enumerable.Range(area.X, area.Width)
			where actual[y * width + x] != expected[y * width + x]
			select (x, y);
		Assert.Empty(differences.Take(10));
	}

	[Fact]
	public void IsNoneForAnotherFile()
	{
		Assert.Null(SpriteSheet.Open("Map/Japan_off.dat"));
	}
}
