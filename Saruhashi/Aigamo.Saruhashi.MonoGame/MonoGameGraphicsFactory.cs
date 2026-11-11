using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.ViewportAdapters;

namespace Aigamo.Saruhashi.MonoGame;

public sealed class MonoGameGraphicsFactory(
	SpriteBatch spriteBatch,
	ViewportAdapter? viewportAdapter = null
) : GraphicsFactory
{
	// The graphics that paints gave back, closed, for the next paints: every control paints every frame. On the UI's
	// thread.
	private readonly Stack<MonoGameGraphics> _free = new();

	public SpriteBatch SpriteBatch { get; } = spriteBatch;
	public ViewportAdapter? ViewportAdapter { get; } = viewportAdapter;

	public override Graphics Create(Control control) =>
		new MonoGameGraphics(control, SpriteBatch, ViewportAdapter);

	public override Graphics Rent(Control control)
	{
		if (!_free.TryPop(out var graphics))
			return Create(control);

		graphics.Reopen(control);
		return graphics;
	}

	public override void Return(Graphics graphics)
	{
		if (graphics is MonoGameGraphics own && own.SpriteBatch == SpriteBatch)
		{
			own.Close();
			_free.Push(own);
		}
		else
		{
			graphics.Dispose();
		}
	}
}
