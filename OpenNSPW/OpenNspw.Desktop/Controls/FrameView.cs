using Aigamo.Saruhashi;
using Aigamo.Saruhashi.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;
using DrawingColor = System.Drawing.Color;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;

namespace OpenNspw.Desktop.Controls;

// A part of the battle screen that shows what the game draws there: the same rectangle of its frame, as sharp as the
// frame. The game reads the mouse itself, so clicks on the part still go to the game (docs/Ui.md).
internal abstract class FrameView : Control
{
	private readonly GameSession _session;

	// area: the rectangle of the game's screen that the part shows.
	protected FrameView(GameSession session, DrawingRectangle area)
	{
		_session = session;
		Area = area;
		BackColor = DrawingColor.Transparent;
	}

	public DrawingRectangle Area { get; }

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		if (_session.Frame is { } frame)
		{
			var region = new Texture2DRegion(frame, Area.X, Area.Y, Area.Width, Area.Height);
			e.Graphics.DrawImage(MonoGameImage.Create(region, Color.White, SamplerState.PointClamp), DrawingPoint.Empty);
		}
	}
}
