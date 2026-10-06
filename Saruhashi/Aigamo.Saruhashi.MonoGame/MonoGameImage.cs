using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;

namespace Aigamo.Saruhashi.MonoGame;

public interface IMonoGameImage : IImage
{
	void Draw(SpriteBatch spriteBatch, Vector2 position);
}

public static class MonoGameImage
{
	private sealed class MonoGameTexture2DImage(Texture2D texture, Color color) : IMonoGameImage
	{
		public Texture2D Texture { get; } = texture;
		public Color Color { get; } = color;

		public void Draw(SpriteBatch spriteBatch, Vector2 position)
		{
			spriteBatch.Draw(Texture, position, Color);
		}
	}

	private sealed class MonoGameTextureRegion2DImage(Texture2DRegion textureRegion, Color color) : IMonoGameImage
	{
		public Texture2DRegion TextureRegion { get; } = textureRegion;
		public Color Color { get; } = color;

		public void Draw(SpriteBatch spriteBatch, Vector2 position)
		{
			spriteBatch.Draw(TextureRegion, position, Color);
		}
	}

	private sealed class MonoGameSpriteImage(Sprite sprite) : IMonoGameImage
	{
		public Sprite Sprite { get; } = sprite;

		public void Draw(SpriteBatch spriteBatch, Vector2 position)
		{
			spriteBatch.Draw(Sprite, position);
		}
	}

	public static IMonoGameImage Create(Texture2D texture, Color color) =>
		new MonoGameTexture2DImage(texture, color);

	public static IMonoGameImage Create(Texture2DRegion textureRegion, Color color) =>
		new MonoGameTextureRegion2DImage(textureRegion, color);

	public static IMonoGameImage Create(Sprite sprite) => new MonoGameSpriteImage(sprite);
}
