// Cuts the original's sprite sheet into the desktop app's sprites (docs/SpriteSheets.md): NSPW NET's t3.bmp, and 1.22's
// tst_cg1.bmp for what only 1.22 has, with sheets.txt, where they go on each sheet, from which the app puts the sheets
// back together for the game (SpriteSheet). Writes them to OpenNSPW/OpenNspw.Desktop/Sprites, or with --check, compares
// them with the files there and fails if any differs.
//
//     dotnet run tools/CutSprites.cs [--check]

using System.Buffers.Binary;
using System.IO.Compression;
using System.Runtime.CompilerServices;

var repository = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(SourcePath())!, ".."));
var output = Path.Combine(repository, "OpenNSPW", "OpenNspw.Desktop", "Sprites");
var isCheck = args is ["--check"];

var nspwNet = Sheet.Load(Path.Combine(repository, "Original", "NSPW_NET", "t3.bmp"));
var nspw122 = Sheet.Load(Path.Combine(repository, "Original", "Nspw122", "tst_cg1.bmp"));

var files = Layout
	.Sprites(nspwNet, nspw122)
	.Select(sprite =>
		(
			sprite.Name,
			Bytes: Png.Encode(
				sprite.Sheet.Cut(sprite.Region, sprite.IsKeyed),
				sprite.Region.Width,
				sprite.Region.Height
			)
		)
	)
	.Append(("sheets.txt", System.Text.Encoding.UTF8.GetBytes(Layout.Sheets(nspwNet))));

var differing = 0;
foreach (var (name, png) in files)
{
	var path = Path.Combine(output, name);
	if (isCheck)
	{
		if (!File.Exists(path) || !File.ReadAllBytes(path).AsSpan().SequenceEqual(png))
		{
			Console.WriteLine($"Differs: {name}");
			differing++;
		}
	}
	else
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllBytes(path, png);
	}
}

return differing == 0 ? 0 : 1;

static string SourcePath([CallerFilePath] string path = "") => path;

internal readonly record struct Region(int X, int Y, int Width, int Height);

// A bitmap of 24 bits, or of 8 bits and a palette, as RGB.
internal sealed class Sheet(int width, int height, byte[] rgb)
{
	private readonly int _width = width;
	private readonly int _height = height;
	private readonly byte[] _rgb = rgb;

	public int Width => _width;

	public int Height => _height;

	// Whether a pixel is the sheet's color key, the transparent color of the sprites: magenta, as the original's 16-bit
	// surface keys it (RestoreSurfaces: the red and the blue masks).
	private static bool IsColorKey(byte r, byte g, byte b) =>
		r >> 3 == 31 && g >> 2 == 0 && b >> 3 == 31;

	public static Sheet Load(string path)
	{
		var file = File.ReadAllBytes(path);
		var pixelsOffset = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(10));
		var paletteOffset = 14 + BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(14));
		var width = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(18));
		var height = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(22));
		var bitCount = BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(28));
		if (bitCount is not (24 or 8))
			throw new NotSupportedException($"{path} has {bitCount} bits per pixel, not 24 or 8.");

		// Rows are padded to 4 bytes, and stored bottom-up.
		var stride = (width * (bitCount / 8) + 3) & ~3;
		var rgb = new byte[width * height * 3];
		for (var y = 0; y < height; y++)
		{
			var row = pixelsOffset + (height - 1 - y) * stride;
			for (var x = 0; x < width; x++)
			{
				var offset = bitCount == 24 ? row + x * 3 : paletteOffset + file[row + x] * 4;
				var at = (y * width + x) * 3;
				(rgb[at], rgb[at + 1], rgb[at + 2]) = (
					file[offset + 2],
					file[offset + 1],
					file[offset]
				);
			}
		}

		return new Sheet(width, height, rgb);
	}

	// The region as RGBA: with the color key transparent, black, if `isKeyed`, and opaque otherwise.
	public byte[] Cut(Region region, bool isKeyed)
	{
		if (region.X + region.Width > _width || region.Y + region.Height > _height)
			throw new ArgumentOutOfRangeException(nameof(region), region, "Not in the sheet.");

		var rgba = new byte[region.Width * region.Height * 4];
		for (var y = 0; y < region.Height; y++)
		{
			for (var x = 0; x < region.Width; x++)
			{
				var from = ((region.Y + y) * _width + region.X + x) * 3;
				var (r, g, b) = (_rgb[from], _rgb[from + 1], _rgb[from + 2]);
				var to = (y * region.Width + x) * 4;
				if (isKeyed && IsColorKey(r, g, b))
					continue;

				(rgba[to], rgba[to + 1], rgba[to + 2], rgba[to + 3]) = (r, g, b, 255);
			}
		}

		return rgba;
	}
}

// A PNG of RGBA, 8 bits a channel.
internal static class Png
{
	private static uint[] CreateCrcTable()
	{
		var table = new uint[256];
		for (var n = 0u; n < 256; n++)
		{
			var c = n;
			for (var k = 0; k < 8; k++)
				c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;

			table[n] = c;
		}

		return table;
	}

	private static readonly uint[] CrcTable = CreateCrcTable();

	private static uint Crc(ReadOnlySpan<byte> bytes)
	{
		var c = 0xFFFFFFFFu;
		foreach (var b in bytes)
			c = CrcTable[(c ^ b) & 0xFF] ^ (c >> 8);

		return c ^ 0xFFFFFFFFu;
	}

	private static void WriteChunk(Stream stream, string type, ReadOnlySpan<byte> data)
	{
		Span<byte> word = stackalloc byte[4];
		BinaryPrimitives.WriteInt32BigEndian(word, data.Length);
		stream.Write(word);
		var typed = new byte[4 + data.Length];
		System.Text.Encoding.ASCII.GetBytes(type, typed);
		data.CopyTo(typed.AsSpan(4));
		stream.Write(typed);
		BinaryPrimitives.WriteUInt32BigEndian(word, Crc(typed));
		stream.Write(word);
	}

	public static byte[] Encode(byte[] rgba, int width, int height)
	{
		var header = new byte[13];
		BinaryPrimitives.WriteInt32BigEndian(header, width);
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
		header[8] = 8; // Bits a channel.
		header[9] = 6; // RGBA.

		// Each row is preceded by its filter, none.
		using var pixels = new MemoryStream();
		using (var zlib = new ZLibStream(pixels, CompressionLevel.SmallestSize))
		{
			for (var y = 0; y < height; y++)
			{
				zlib.WriteByte(0);
				zlib.Write(rgba, y * width * 4, width * 4);
			}
		}

		using var png = new MemoryStream();
		png.Write([0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]);
		WriteChunk(png, "IHDR", header);
		WriteChunk(png, "IDAT", pixels.ToArray());
		WriteChunk(png, "IEND", []);
		return png.ToArray();
	}
}

// Where the sprites are in the sheet (set_sprt_data), and the files they go to.
internal static class Layout
{
	// A unit's row of 8 directions, 80 by 80, by SpriteRow (cnct_game_cont.cpp), for Japan and the United States.
	// Row 11 is the facilities' (Facilities), and a submerged submarine draws the row after its own.
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

	// The facilities of row 11, by the column their direction draws (set_unit_data). Columns 3 and 4 are not drawn.
	private static readonly (int Column, string Name)[] Facilities =
	[
		(0, "nb"),
		(1, "ab"),
		(2, "city"),
		(5, "ib"),
		(6, "pillboxes"),
		(7, "fortress"),
	];

	// The unit-info pictures, 120 by 438, 6 a row, by frame (UnitInfoPanel[2], DrawUnitDetails).
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

	// A side's sprites, in its folder: its units' rows, its facilities and its unit-info pictures.
	private static IEnumerable<(string Name, Region Region)> Side(
		string folder,
		bool isJapan,
		int unitsY,
		int picturesY
	)
	{
		foreach (var (row, japan, unitedStates) in UnitRows)
		{
			yield return (
				$"{folder}/{(isJapan ? japan : unitedStates)}.png",
				new Region(0, unitsY + row * 80, 640, 80)
			);
		}

		foreach (var (column, name) in Facilities)
			yield return (
				$"{folder}/{name}.png",
				new Region(column * 80, unitsY + 11 * 80, 80, 80)
			);

		for (var frame = 0; frame < Pictures.Length; frame++)
		{
			yield return (
				$"{folder}/info/{Pictures[frame]}.png",
				new Region(frame % 6 * 120, picturesY + frame / 6 * 438, 120, 438)
			);
		}
	}

	// The files, where they are on the sheets, and which sheets have them: 1.22's only (IsNspw122 true), cut from it,
	// NSPW NET's only (false), or both (null), cut from NSPW NET's.
	private static IEnumerable<(string Name, Region Region, bool IsKeyed, bool? IsNspw122)> Placed()
	{
		yield return ("title.png", new Region(0, 3940, 699, 384), false, false);
		// 1.22's title, which its sheet has instead (set_sprt_data, 0x432E43).
		yield return ("title-122.png", new Region(0, 3940, 599, 387), false, true);
		yield return ("tiles.png", new Region(0, 3000, 480, 240), true, null);
		// The combat menu's buttons (Buttons1), two columns, and the minimap's frames and the dots it draws units with
		// (Minimap): the UI draws its own, but the game draws them on its surface.
		yield return ("buttons.png", new Region(0, 3250, 321, 221), false, null);
		yield return ("minimap.png", new Region(0, 4340, 561, 199), false, null);
		yield return ("effects.png", new Region(0, 3520, 480, 400), true, null);
		yield return ("flag-jp.png", new Region(361, 3250, 198, 120), false, null);
		yield return ("flag-us.png", new Region(361, 3370, 198, 120), false, null);

		foreach (var (name, region) in Side("jp", true, 1759, 0))
			yield return (name, region, true, name == "jp/j1n.png" ? false : null);

		// 1.22's land-based fighter of Japan.
		yield return ("jp/j7w.png", new Region(0, 1759 + 14 * 80, 640, 80), true, true);

		foreach (var (name, region) in Side("us", false, 4550, 878))
			yield return (name, region, true, null);
	}

	public static IEnumerable<(string Name, Sheet Sheet, Region Region, bool IsKeyed)> Sprites(
		Sheet nspwNet,
		Sheet nspw122
	)
	{
		foreach (var (name, region, isKeyed, isNspw122) in Placed())
			yield return (name, isNspw122 is true ? nspw122 : nspwNet, region, isKeyed);
	}

	// sheets.txt: each sheet's name and size, then its files and where they go on it. 1.22's has NSPW NET's effects,
	// with the three icons that only NSPW NET's rules draw.
	public static string Sheets(Sheet nspwNet)
	{
		var text = new System.Text.StringBuilder();
		foreach (var (sheet, isNspw122) in new[] { ("t3.bmp", false), ("tst_cg1.bmp", true) })
		{
			text.Append($"{sheet} {nspwNet.Width} {nspwNet.Height}\n");
			foreach (var (name, region, _, onNspw122) in Placed())
			{
				if (onNspw122 is null || onNspw122 == isNspw122)
					text.Append($"\t{name} {region.X} {region.Y}\n");
			}
		}

		return text.ToString();
	}
}
