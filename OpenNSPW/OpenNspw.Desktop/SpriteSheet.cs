using System.Buffers.Binary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenNspw.Desktop;

// The original's sprite sheet, a bitmap taller than a texture may be, read a region at a time: NSPW NET's t3.bmp, of 24
// bits, or 1.22's tst_cg1.bmp, of 8 bits and a palette, which single-player games use.
internal static class SpriteSheet
{
	// Whether a pixel is the sheet's color key, the transparent color of the sprites: magenta, as the original's 16-bit
	// surface keys it (RestoreSurfaces: the red and the blue masks).
	private static bool IsColorKey(byte r, byte g, byte b) =>
		r >> 3 == 31 && g >> 2 == 0 && b >> 3 == 31;

	// Regions of the sheet, as textures, reading it once: with the color key transparent, if `isKeyed`.
	public static Texture2D[] LoadRegions(
		GraphicsDevice graphicsDevice,
		string path,
		Rectangle[] regions,
		bool isKeyed = false
	)
	{
		var file = File.ReadAllBytes(path);
		var pixelsOffset = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(10));
		var width = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(18));
		var height = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(22));
		var bitCount = BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(28));
		if (bitCount is not (24 or 8))
		{
			throw new NotSupportedException($"{path} has {bitCount} bits per pixel, not 24 or 8.");
		}

		// An 8-bit bitmap's palette follows its header: blue, green, red and a reserved byte for each color.
		var paletteOffset = 14 + BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(14));

		// Rows are padded to 4 bytes, and stored bottom-up unless the height is negative.
		var stride = (width * (bitCount / 8) + 3) & ~3;
		var textures = new Texture2D[regions.Length];
		for (var i = 0; i < regions.Length; i++)
		{
			var region = regions[i];
			var colors = new Color[region.Width * region.Height];
			for (var y = 0; y < region.Height; y++)
			{
				var row = height > 0 ? height - 1 - (region.Y + y) : region.Y + y;
				for (var x = 0; x < region.Width; x++)
				{
					var offset =
						bitCount == 24
							? pixelsOffset + row * stride + (region.X + x) * 3
							: paletteOffset + file[pixelsOffset + row * stride + region.X + x] * 4;
					var (r, g, b) = (file[offset + 2], file[offset + 1], file[offset]);
					colors[y * region.Width + x] =
						isKeyed && IsColorKey(r, g, b) ? Color.Transparent : new Color(r, g, b);
				}
			}

			textures[i] = new Texture2D(graphicsDevice, region.Width, region.Height);
			textures[i].SetData(colors);
		}

		return textures;
	}

	// The region of the sheet, as a texture.
	public static Texture2D LoadRegion(GraphicsDevice graphicsDevice, string path, Rectangle region)
	{
		return LoadRegions(graphicsDevice, path, [region])[0];
	}
}
