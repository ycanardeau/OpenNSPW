using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.ViewportAdapters;

namespace OpenNspw.App;

// The game's screen, all_head.SCRN_WIDTH by SCRN_HEIGHT, which the game and the app's screens are laid out in, scaled to
// the back buffer. In a back buffer wider than the layout's shape, the screen is as high as it and wider than the layout,
// by ExtraWidth: the layout is centered in it, or at its left (IsLayoutLeft), for the battle screen to put more right of
// it (BattleForm). In one narrower, the screen is as wide as it, between bars above and below, as BoxingViewportAdapter
// does.
internal sealed class WideViewportAdapter : ViewportAdapter
{
	private const int LayoutWidth = all_head.SCRN_WIDTH;
	private const int LayoutHeight = all_head.SCRN_HEIGHT;

	private float _scale = 1;

	public WideViewportAdapter(GraphicsDevice graphicsDevice)
		: base(graphicsDevice)
	{
		Fit();
	}

	// The game pixels of the screen beside the layout, in a back buffer wider than its shape.
	public int ExtraWidth { get; private set; }

	// Whether the layout is at the screen's left, with all of ExtraWidth right of it, rather than centered.
	public bool IsLayoutLeft { get; set; }

	private float LayoutLeft => IsLayoutLeft ? 0 : ExtraWidth / 2f;

	public override int VirtualWidth => LayoutWidth + ExtraWidth;

	public override int VirtualHeight => LayoutHeight;

	public override int ViewportWidth => GraphicsDevice.Viewport.Width;

	public override int ViewportHeight => GraphicsDevice.Viewport.Height;

	// Fits the screen to the back buffer, whose size changes with the window's: at the start of each update.
	public void Fit()
	{
		var parameters = GraphicsDevice.PresentationParameters;
		var (width, height) = (parameters.BackBufferWidth, parameters.BackBufferHeight);
		if (width * LayoutHeight >= height * LayoutWidth)
		{
			_scale = height / (float)LayoutHeight;
			ExtraWidth = Math.Max(0, (int)(width / _scale) - LayoutWidth);
			GraphicsDevice.Viewport = new Viewport(0, 0, width, height);
		}
		else
		{
			_scale = width / (float)LayoutWidth;
			ExtraWidth = 0;
			var viewportHeight = (int)(LayoutHeight * _scale);
			GraphicsDevice.Viewport = new Viewport(
				0,
				(height - viewportHeight) / 2,
				width,
				viewportHeight
			);
		}
	}

	public override Matrix GetScaleMatrix()
	{
		return Matrix.CreateTranslation(LayoutLeft, 0, 0) * Matrix.CreateScale(_scale);
	}

	public override Point PointToScreen(int x, int y)
	{
		var viewport = GraphicsDevice.Viewport;
		return new Point(
			(int)MathF.Floor((x - viewport.X) / _scale - LayoutLeft),
			(int)MathF.Floor((y - viewport.Y) / _scale)
		);
	}

	public override void Reset()
	{
		Fit();
	}
}
