using Aigamo.Saruhashi.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.ViewportAdapters;
using OpenNspw.Desktop.Controls;

namespace OpenNspw.Desktop.Screens;

// The menus' background: a dark sea chart's grid.
internal sealed class Backdrop : IDisposable
{
	private const int GridSize = 48;

	private readonly Texture2D _gradient;

	public Backdrop(GraphicsDevice graphicsDevice)
	{
		var top = Theme.Navy.ToXnaColor();
		var bottom = Theme.Ink.ToXnaColor();
		var colors = new Color[256];
		for (var i = 0; i < colors.Length; i++)
			colors[i] = Color.Lerp(top, bottom, i / 255f);

		_gradient = new Texture2D(graphicsDevice, 1, colors.Length);
		_gradient.SetData(colors);
	}

	private static Color Faded(System.Drawing.Color color, float alpha) => color.ToXnaColor() * alpha;

	private static void DrawGrid(SpriteBatch spriteBatch)
	{
		for (var i = 0; i <= all_head.SCRN_WIDTH / GridSize; i++)
			spriteBatch.DrawLine(i * GridSize, 0, i * GridSize, all_head.SCRN_HEIGHT, Faded(Theme.Line, i % 4 == 0 ? 0.35f : 0.16f));

		for (var j = 0; j <= all_head.SCRN_HEIGHT / GridSize; j++)
			spriteBatch.DrawLine(0, j * GridSize, all_head.SCRN_WIDTH, j * GridSize, Faded(Theme.Line, j % 4 == 0 ? 0.35f : 0.16f));
	}

	public void Draw(SpriteBatch spriteBatch, ViewportAdapter viewportAdapter)
	{
		spriteBatch.Begin(transformMatrix: viewportAdapter.GetScaleMatrix(), blendState: BlendState.AlphaBlend);
		spriteBatch.Draw(_gradient, new Rectangle(0, 0, all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT), Color.White);
		DrawGrid(spriteBatch);
		spriteBatch.End();
	}

	public void Dispose()
	{
		_gradient.Dispose();
	}
}
