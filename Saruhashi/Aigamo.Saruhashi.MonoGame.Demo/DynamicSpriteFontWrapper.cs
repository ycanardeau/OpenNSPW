using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Aigamo.Saruhashi.MonoGame.Demo;

internal sealed class DynamicSpriteFontWrapper(DynamicSpriteFont dynamicSpriteFont) : IMonoGameFont
{
	public DynamicSpriteFont DynamicSpriteFont { get; } = dynamicSpriteFont;

	public void Draw(SpriteBatch spriteBatch, string? text, Vector2 position, Color color) =>
		DynamicSpriteFont.DrawText(spriteBatch, text, position, color);

	public Vector2 MeasureString(string? text) => DynamicSpriteFont.MeasureString(text);
}
