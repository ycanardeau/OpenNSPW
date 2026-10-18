using System.Buffers.Binary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenNspw.Desktop;

// The original's sprite sheet (t3.bmp), a 24-bit bitmap taller than a texture may be, read a region at a time.
internal static class SpriteSheet
{
	// The region of the sheet, as a texture.
	public static Texture2D LoadRegion(GraphicsDevice graphicsDevice, string path, Rectangle region)
	{
		var file = File.ReadAllBytes(path);
		var pixelsOffset = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(10));
		var width = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(18));
		var height = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(22));
		var bitCount = BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(28));
		if (bitCount != 24)
		{
			throw new NotSupportedException($"{path} has {bitCount} bits per pixel, not 24.");
		}

		// Rows are padded to 4 bytes, and stored bottom-up unless the height is negative.
		var stride = (width * 3 + 3) & ~3;
		var colors = new Color[region.Width * region.Height];
		for (var y = 0; y < region.Height; y++)
		{
			var row = height > 0 ? height - 1 - (region.Y + y) : region.Y + y;
			for (var x = 0; x < region.Width; x++)
			{
				var offset = pixelsOffset + row * stride + (region.X + x) * 3;
				colors[y * region.Width + x] = new Color(file[offset + 2], file[offset + 1], file[offset]);
			}
		}

		var texture = new Texture2D(graphicsDevice, region.Width, region.Height);
		texture.SetData(colors);
		return texture;
	}
}
