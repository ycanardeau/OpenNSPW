using Aigamo.Saruhashi.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.ViewportAdapters;
using OpenNspw.Desktop.Controls;

namespace OpenNspw.Desktop.Screens;

// The menus' background: a dark sea chart whose grid drifts, with a radar sweeping over a few contacts.
internal sealed class Backdrop : IDisposable
{
	private const int GridSize = 48;
	private static readonly Vector2 RadarCenter = new(850, 610);
	private const float RadarRadius = 300;

	// Contacts on the radar, by angle (radians) and distance from its center.
	private static readonly (float Angle, float Distance)[] Contacts =
		[(3.6f, 120), (3.9f, 210), (4.4f, 160), (4.9f, 260), (3.3f, 250), (5.3f, 90)];

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

	private static void DrawGrid(SpriteBatch spriteBatch, float time)
	{
		var offset = time * 6 % GridSize;
		for (var i = -1; i <= all_head.SCRN_WIDTH / GridSize + 1; i++)
		{
			var x = i * GridSize + offset;
			var major = (i + (int)(time * 6 / GridSize)) % 4 == 0;
			spriteBatch.DrawLine(x, 0, x, all_head.SCRN_HEIGHT, Faded(Theme.Line, major ? 0.35f : 0.16f));
		}

		for (var j = -1; j <= all_head.SCRN_HEIGHT / GridSize + 1; j++)
		{
			var y = j * GridSize + offset;
			var major = (j + (int)(time * 6 / GridSize)) % 4 == 0;
			spriteBatch.DrawLine(0, y, all_head.SCRN_WIDTH, y, Faded(Theme.Line, major ? 0.35f : 0.16f));
		}
	}

	private static void DrawRadar(SpriteBatch spriteBatch, float time)
	{
		for (var ring = 1; ring <= 4; ring++)
			spriteBatch.DrawCircle(RadarCenter, RadarRadius * ring / 4, 96, Faded(Theme.Line, 0.45f), 1);

		// The sweep's trail: lines close enough at its edge to fill a wedge, fading behind it.
		var sweep = time * 0.7f % MathHelper.TwoPi;
		const int trail = 140;
		for (var i = 0; i < trail; i++)
		{
			var angle = sweep - i * 0.004f;
			var end = RadarCenter + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * RadarRadius;
			spriteBatch.DrawLine(RadarCenter, end, Faded(Theme.Brass, i == 0 ? 0.6f : 0.12f * (1 - i / (float)trail)), 2);
		}

		foreach (var (angle, distance) in Contacts)
		{
			// A contact glows as the sweep passes it, and fades until the next pass.
			var since = (sweep - angle) % MathHelper.TwoPi;
			if (since < 0)
				since += MathHelper.TwoPi;

			var glow = MathF.Max(0, 1 - since / 2.5f);
			var position = RadarCenter + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
			spriteBatch.DrawCircle(position, 3, 12, Faded(Theme.BrassLight, 0.15f + 0.85f * glow), 3);
		}
	}

	public void Draw(SpriteBatch spriteBatch, ViewportAdapter viewportAdapter, float time)
	{
		spriteBatch.Begin(transformMatrix: viewportAdapter.GetScaleMatrix(), blendState: BlendState.AlphaBlend);
		spriteBatch.Draw(_gradient, new Rectangle(0, 0, all_head.SCRN_WIDTH, all_head.SCRN_HEIGHT), Color.White);
		DrawGrid(spriteBatch, time);
		DrawRadar(spriteBatch, time);
		spriteBatch.End();
	}

	public void Dispose()
	{
		_gradient.Dispose();
	}
}
