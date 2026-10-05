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
	private sealed class MonoGameTexture2DImage : IMonoGameImage
	{
		public Texture2D Texture { get; }
		public Color Color { get; }

		public MonoGameTexture2DImage(Texture2D texture, Color color)
		{
			Texture = texture;
			Color = color;
		}

		public void Draw(SpriteBatch spriteBatch, Vector2 position)
		{
			spriteBatch.Draw(Texture, position, Color);
		}
	}

	private sealed class MonoGameTextureRegion2DImage : IMonoGameImage
	{
		public Texture2DRegion TextureRegion { get; }
		public Color Color { get; }

		public MonoGameTextureRegion2DImage(Texture2DRegion textureRegion, Color color)
		{
			TextureRegion = textureRegion;
			Color = color;
		}

		public void Draw(SpriteBatch spriteBatch, Vector2 position)
		{
			spriteBatch.Draw(TextureRegion, position, Color);
		}
	}

	private sealed class MonoGameSpriteImage : IMonoGameImage
	{
		public Sprite Sprite { get; }

		public MonoGameSpriteImage(Sprite sprite)
		{
			Sprite = sprite;
		}

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
