using Microsoft.Xna.Framework.Graphics;
using StbImageSharp;

namespace OpenNspw.Desktop;

// The sprites cut from the original's sprite sheet (docs/SpriteSheets.md): PNG files in Sprites, embedded in the
// app, made again with tools/CutSprites.cs. The sheet's color key is transparent in them. The UI draws them as
// textures, and the game its own frames from them (docs/GameSprites.md).
internal static class SpriteFiles
{
	// The file at the path under Sprites, such as "jp/bb.png", or null if the app has none.
	private static Stream? Open(string path)
	{
		return typeof(SpriteFiles).Assembly.GetManifestResourceStream(
			$"{typeof(SpriteFiles).Namespace}.Sprites.{path.Replace('/', '.')}"
		);
	}

	// The file at the path, as the game's sprites are (INspwPlatform.LoadSprite), or null if the app has none.
	public static SpriteBitmap? LoadBitmap(string path)
	{
		using var stream = Open(path);
		if (stream is null)
		{
			return null;
		}

		var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
		return new SpriteBitmap(image.Width, image.Height, image.Data);
	}

	// The file at the path, as a texture.
	public static Texture2D Load(GraphicsDevice graphicsDevice, string path)
	{
		using var stream =
			Open(path)
			?? throw new FileNotFoundException($"The sprite {path} is not in the app.", path);
		return Texture2D.FromStream(
			graphicsDevice,
			stream,
			DefaultColorProcessors.PremultiplyAlpha
		);
	}
}
