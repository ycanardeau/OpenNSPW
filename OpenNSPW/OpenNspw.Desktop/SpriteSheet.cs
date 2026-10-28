using System.Buffers.Binary;
using StbImageSharp;

namespace OpenNspw.Desktop;

// The original's sprite sheets, NSPW NET's t3.bmp and 1.22's tst_cg1.bmp, which the game reads for its own surface
// (LoadBitmapSurface), put back together from the sprite files (SpriteFiles) where Sprites/sheets.txt
// (tools/CutSprites.cs) puts them: the app needs neither sheet. Where a file is transparent, the sheet has its color
// key, magenta; what no file covers, the game never draws, and is white.
internal static class SpriteSheet
{
	private readonly record struct Placement(string File, int X, int Y);

	private sealed record Layout(int Width, int Height, Placement[] Placements);

	// sheets.txt: a line a sheet, its name and size, then a line a file, indented, with where it goes.
	private static Dictionary<string, Layout> ReadLayouts()
	{
		using var reader = new StreamReader(SpriteFiles.Open("sheets.txt"));
		var layouts = new Dictionary<string, Layout>(StringComparer.OrdinalIgnoreCase);
		string? sheet = null;
		var (width, height) = (0, 0);
		var placements = new List<Placement>();
		void Add()
		{
			if (sheet is not null)
				layouts[sheet] = new Layout(width, height, [.. placements]);
		}

		while (reader.ReadLine() is { } line)
		{
			var fields = line.Trim().Split(' ');
			if (line.StartsWith('\t'))
			{
				placements.Add(
					new Placement(fields[0], int.Parse(fields[1]), int.Parse(fields[2]))
				);
			}
			else if (fields is [var name, var w, var h])
			{
				Add();
				(sheet, width, height) = (name, int.Parse(w), int.Parse(h));
				placements.Clear();
			}
		}

		Add();
		return layouts;
	}

	private static readonly Lazy<Dictionary<string, Layout>> Layouts = new(ReadLayouts);

	// The layout's sheet as a 24-bit BMP, as the original's t3.bmp is.
	private static byte[] Build(Layout layout)
	{
		// Rows are padded to 4 bytes, and stored bottom-up.
		var stride = (layout.Width * 3 + 3) & ~3;
		var pixelsOffset = 14 + 40;
		var bmp = new byte[pixelsOffset + stride * layout.Height];
		bmp.AsSpan(pixelsOffset).Fill(0xFF);
		"BM"u8.CopyTo(bmp);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(2), bmp.Length);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(10), pixelsOffset);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(14), 40);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(18), layout.Width);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(22), layout.Height);
		BinaryPrimitives.WriteUInt16LittleEndian(bmp.AsSpan(26), 1);
		BinaryPrimitives.WriteUInt16LittleEndian(bmp.AsSpan(28), 24);

		foreach (var (file, left, top) in layout.Placements)
		{
			using var stream = SpriteFiles.Open(file);
			var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
			for (var y = 0; y < image.Height; y++)
			{
				var row = pixelsOffset + (layout.Height - 1 - (top + y)) * stride + left * 3;
				for (var x = 0; x < image.Width; x++)
				{
					var from = (y * image.Width + x) * 4;
					var to = row + x * 3;
					var isKey = image.Data[from + 3] == 0;
					bmp[to] = isKey ? (byte)0xFF : image.Data[from + 2];
					bmp[to + 1] = isKey ? (byte)0x00 : image.Data[from + 1];
					bmp[to + 2] = isKey ? (byte)0xFF : image.Data[from];
				}
			}
		}

		return bmp;
	}

	// The sheet of the name that the game opens (SpriteSheetName), such as "t3.bmp", or null for any other file.
	public static byte[]? Open(string name) =>
		Layouts.Value.TryGetValue(name, out var layout) ? Build(layout) : null;
}
