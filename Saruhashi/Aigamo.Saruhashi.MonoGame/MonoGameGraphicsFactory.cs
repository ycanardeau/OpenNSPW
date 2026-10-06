using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.ViewportAdapters;

namespace Aigamo.Saruhashi.MonoGame;

public sealed class MonoGameGraphicsFactory(SpriteBatch spriteBatch, ViewportAdapter? viewportAdapter = null) : IGraphicsFactory
{
	public SpriteBatch SpriteBatch { get; } = spriteBatch;
	public ViewportAdapter? ViewportAdapter { get; } = viewportAdapter;

	public Graphics Create(Control control) =>
		new MonoGameGraphics(control, SpriteBatch, ViewportAdapter);
}
