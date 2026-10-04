using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.ViewportAdapters;

namespace Aigamo.Saruhashi.MonoGame;

public sealed class MonoGameGraphicsFactory : IGraphicsFactory
{
	public SpriteBatch SpriteBatch { get; }
	public ViewportAdapter? ViewportAdapter { get; }

	public MonoGameGraphicsFactory(SpriteBatch spriteBatch, ViewportAdapter? viewportAdapter = null)
	{
		SpriteBatch = spriteBatch;
		ViewportAdapter = viewportAdapter;
	}

	public Graphics Create(Control control) => new MonoGameGraphics(control, SpriteBatch, ViewportAdapter);
}
