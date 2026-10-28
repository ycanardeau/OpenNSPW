using OpenNspw.Desktop;
using Xunit;

namespace OpenNspw.Tests;

// The sprite sheets that the app puts together from the sprites (SpriteSheet) are the original's, as the game's 16-bit
// surface holds them, wherever the game draws from (set_sprt_data, and the minimap's and the buttons' own rectangles).
public sealed class SpriteSheetTests
{
	private readonly record struct Region(int X, int Y, int Width, int Height);

	// The parts of the sheet that the game draws from, by SpriteId.
	private static readonly Region[] Drawn =
	[
		new(0, 0, 720, 876), // JapanUnitInfo: 12 pictures, 6 a row
		new(0, 878, 720, 876), // UnitedStatesUnitInfo
		new(0, 1759, 640, 1200), // JapanUnits: 15 rows of 8
		new(0, 3000, 480, 240), // MapTiles
		new(0, 3250, 361, 270), // Buttons1, both columns
		new(361, 3250, 198, 240), // ButtonBase: both flags
		new(0, 3520, 480, 400), // SubUnits
		new(0, 4340, 561, 200), // Minimap: its frame, dots and background
		new(0, 4550, 640, 1200), // UnitedStatesUnits
	];

	// A BMP of 8 or 24 bits, as the game's RGB565 surface holds it (wingdi.DecodeBmp), with its width.
	private static (ushort[] Pixels, int Width) Decode(byte[] file)
	{
		var pixelsOffset = BitConverter.ToInt32(file, 10);
		var palette = 14 + BitConverter.ToInt32(file, 14);
		var width = BitConverter.ToInt32(file, 18);
		var height = BitConverter.ToInt32(file, 22);
		var bitCount = BitConverter.ToUInt16(file, 28);
		var stride = (width * bitCount + 31) / 32 * 4;
		var pixels = new ushort[width * height];
		for (var y = 0; y < height; y++)
		{
			var row = pixelsOffset + (height - 1 - y) * stride;
			for (var x = 0; x < width; x++)
			{
				var at = bitCount == 8 ? palette + file[row + x] * 4 : row + x * 3;
				pixels[y * width + x] = wingdi.ToPixel(
					wingdi.RGB(file[at + 2], file[at + 1], file[at])
				);
			}
		}

		return (pixels, width);
	}

	// The pixels of the regions where the sheets differ, as "x,y".
	private static List<string> Differences(
		byte[] expected,
		byte[] actual,
		IEnumerable<Region> regions,
		Func<int, int, bool> isSkipped
	)
	{
		var (expectedPixels, width) = Decode(expected);
		var (actualPixels, actualWidth) = Decode(actual);
		Assert.Equal(width, actualWidth);
		Assert.Equal(expectedPixels.Length, actualPixels.Length);

		var differences = new List<string>();
		foreach (var region in regions)
		{
			for (var y = region.Y; y < region.Y + region.Height; y++)
			for (var x = region.X; x < region.X + region.Width; x++)
				if (
					!isSkipped(x, y)
					&& expectedPixels[y * width + x] != actualPixels[y * width + x]
				)
					differences.Add($"{x},{y}");
		}

		return differences;
	}

	[Fact]
	public void NspwNetSheet()
	{
		var original = File.ReadAllBytes(Path.Combine(GameData.FindData(), "t3.bmp"));

		var differences = Differences(
			original,
			SpriteSheet.NspwNet,
			[.. Drawn, new(0, 3940, 699, 384)],
			(_, _) => false
		);

		Assert.Empty(differences);
	}

	// But for the three effects that 1.22's sheet leaves blank, which only NSPW NET's rules draw
	// (BattleRules.MarksSecondTypes): the app's are NSPW NET's.
	[Fact]
	public void Nspw122Sheet()
	{
		var original = File.ReadAllBytes(Path.Combine(GameData.FindSingleplayer(), "tst_cg1.bmp"));
		int[] blankEffects = [16, 26, 27];

		var differences = Differences(
			original,
			SpriteSheet.Nspw122,
			[.. Drawn, new(0, 3940, 599, 387)],
			(x, y) =>
				y is >= 3520 and < 3920
				&& x < 480
				&& blankEffects.Contains((y - 3520) / 40 * 12 + x / 40)
		);

		Assert.Empty(differences);
	}
}
