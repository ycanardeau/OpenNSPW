using Microsoft.Xna.Framework.Graphics;

namespace OpenNspw.Desktop;

// The sprites cut from the original's sprite sheet (docs/SpriteSheets.md): PNG files in Sprites, embedded in the
// app, made again with tools/CutSprites.cs. The sheet's color key is transparent in them.
internal static class SpriteFiles
{
	// The file at the path under Sprites, such as "jp/bb.png".
	public static Stream Open(string path)
	{
		var name = $"{typeof(SpriteFiles).Namespace}.Sprites.{path.Replace('/', '.')}";
		return typeof(SpriteFiles).Assembly.GetManifestResourceStream(name)
			?? throw new FileNotFoundException($"The sprite {path} is not in the app.", name);
	}

	// The file at the path under Sprites as a texture.
	public static Texture2D Load(GraphicsDevice graphicsDevice, string path)
	{
		using var stream = Open(path);
		return Texture2D.FromStream(
			graphicsDevice,
			stream,
			DefaultColorProcessors.PremultiplyAlpha
		);
	}
}
