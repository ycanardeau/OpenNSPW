using System.Buffers.Binary;
using StbImageSharp;

namespace OpenNspw.Desktop;

// The original's sprite sheets, put together again from the sprites cut from them (SpriteFiles, docs/SpriteSheets.md),
// for the game's own surface (LoadBitmapSurface), so that the app ships no sheet: NSPW NET's t3.bmp, and 1.22's
// tst_cg1.bmp. Every part that the game draws from is as on the sheet; the rest, the author's notes around the cells,
// is white. A 24-bit BMP, whose sprites' transparent pixels are the sheet's color key, magenta.
internal static class SpriteSheet
{
	private const int Width = 721;
	private const int Height = 5756;

	// A unit's row of 8 directions by SpriteRow, and the facilities' row 11 by column, for Japan and the United States.
	private static readonly (int Row, string Japan, string UnitedStates)[] UnitRows =
	[
		(0, "bb", "bb"),
		(1, "ca", "ca"),
		(2, "dd", "dd"),
		(3, "ss", "ss"),
		(4, "ss-submerged", "ss-submerged"),
		(5, "cvl", "cvl"),
		(6, "cv", "cv"),
		(7, "a6m", "f6f"),
		(8, "a6m-stopped", "f6f-stopped"),
		(9, "b5n", "tbf"),
		(10, "b5n-stopped", "tbf-stopped"),
		(12, "g8n", "b24"),
		(13, "tr", "tr"),
		(14, "j1n", "p38"),
	];

	private static readonly string[] Facilities =
	[
		"nb",
		"ab",
		"city",
		"unused-3",
		"unused-4",
		"ib",
		"pillboxes",
		"fortress",
	];

	// The unit-info pictures, 120 by 438, 6 a row, by frame.
	private static readonly string[] Pictures =
	[
		"bb",
		"ca",
		"dd",
		"ss",
		"cv",
		"cvl",
		"tr",
		"empty",
		"ab-runway",
		"ab-apron",
		"cv-hangar",
		"cvl-hangar",
	];

	// A side's sprites and where they go: its units' rows, its facilities and its unit-info pictures. 1.22's Japan has
	// its own land-based fighter.
	private static IEnumerable<(string Path, int X, int Y)> Side(
		string folder,
		bool isJapan,
		int unitsY,
		int picturesY,
		bool isNspw122
	)
	{
		foreach (var (row, japan, unitedStates) in UnitRows)
		{
			var name =
				!isJapan ? unitedStates
				: isNspw122 && row == 14 ? "j7w"
				: japan;
			yield return ($"{folder}/{name}.png", 0, unitsY + row * 80);
		}

		for (var column = 0; column < Facilities.Length; column++)
			yield return ($"{folder}/{Facilities[column]}.png", column * 80, unitsY + 11 * 80);

		for (var frame = 0; frame < Pictures.Length; frame++)
			yield return (
				$"{folder}/info/{Pictures[frame]}.png",
				frame % 6 * 120,
				picturesY + frame / 6 * 438
			);
	}

	// The sprites of a sheet and where they go on it, as tools/CutSprites.cs cuts them. The effects are NSPW NET's in
	// both: 1.22's three blank frames are drawn only by NSPW NET's rules (BattleRules.MarksSecondTypes).
	private static IEnumerable<(string Path, int X, int Y)> Sprites(bool isNspw122)
	{
		yield return (isNspw122 ? "title-122.png" : "title.png", 0, 3940);
		yield return ("tiles.png", 0, 3000);
		yield return ("buttons.png", 0, 3250);
		yield return ("flag-jp.png", 361, 3250);
		yield return ("flag-us.png", 361, 3370);
		yield return ("effects.png", 0, 3520);
		yield return ("minimap.png", 0, 4340);

		foreach (var sprite in Side("jp", true, 1759, 0, isNspw122))
			yield return sprite;

		foreach (var sprite in Side("us", false, 4550, 878, isNspw122))
			yield return sprite;
	}

	private static byte[] Build(bool isNspw122)
	{
		// Rows are padded to 4 bytes, and stored bottom-up.
		const int Stride = (Width * 3 + 3) & ~3;
		const int PixelsOffset = 14 + 40;
		var bmp = new byte[PixelsOffset + Stride * Height];
		bmp[0] = (byte)'B';
		bmp[1] = (byte)'M';
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(2), bmp.Length);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(10), PixelsOffset);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(14), 40);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(18), Width);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(22), Height);
		BinaryPrimitives.WriteUInt16LittleEndian(bmp.AsSpan(26), 1);
		BinaryPrimitives.WriteUInt16LittleEndian(bmp.AsSpan(28), 24);
		bmp.AsSpan(PixelsOffset).Fill(0xFF);

		foreach (var (path, x, y) in Sprites(isNspw122))
		{
			using var stream = SpriteFiles.Open(path);
			var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
			for (var row = 0; row < image.Height; row++)
			{
				for (var column = 0; column < image.Width; column++)
				{
					var from = (row * image.Width + column) * 4;
					var to = PixelsOffset + (Height - 1 - (y + row)) * Stride + (x + column) * 3;
					var isTransparent = image.Data[from + 3] == 0;
					bmp[to] = isTransparent ? (byte)0xFF : image.Data[from + 2];
					bmp[to + 1] = isTransparent ? (byte)0 : image.Data[from + 1];
					bmp[to + 2] = isTransparent ? (byte)0xFF : image.Data[from];
				}
			}
		}

		return bmp;
	}

	private static readonly Lazy<byte[]> NspwNetSheet = new(() => Build(false));

	private static readonly Lazy<byte[]> Nspw122Sheet = new(() => Build(true));

	// NSPW NET's t3.bmp.
	public static byte[] NspwNet => NspwNetSheet.Value;

	// 1.22's tst_cg1.bmp.
	public static byte[] Nspw122 => Nspw122Sheet.Value;
}
