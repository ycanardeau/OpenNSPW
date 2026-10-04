using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Aigamo.Saruhashi.MonoGame;

public interface IMonoGameFont : IFont
{
	void Draw(SpriteBatch spriteBatch, string? text, Vector2 position, Color color);
	Vector2 MeasureString(string? text);
}
