using System.Drawing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.ViewportAdapters;
using DrawingColor = System.Drawing.Color;
using DrawingPoint = System.Drawing.Point;
using DrawingPointF = System.Drawing.PointF;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingRectangleF = System.Drawing.RectangleF;
using DrawingSizeF = System.Drawing.SizeF;
using XnaRectangle = Microsoft.Xna.Framework.Rectangle;
using XnaRectangleF = MonoGame.Extended.RectangleF;
using XnaSizeF = MonoGame.Extended.SizeF;

namespace Aigamo.Saruhashi.MonoGame;

public sealed class MonoGameGraphics : Graphics
{
	// A struct, as a graphics may serve a paint every frame.
	private readonly struct DisposableScissor : IDisposable
	{
		private readonly SpriteBatch _spriteBatch;
		private readonly XnaRectangle _scissorRectangle;

		public DisposableScissor(
			SpriteBatch spriteBatch,
			XnaRectangle scissorRectangle,
			ViewportAdapter? viewportAdapter = null
		)
		{
			_spriteBatch = spriteBatch;
			_scissorRectangle = spriteBatch.GraphicsDevice.ScissorRectangle;

			// The scissor is in the back buffer's pixels: the clip moved and scaled as the viewport's matrix moves and scales
			// the controls, and moved to where the viewport is, such as between the bars of a boxing viewport.
			if (viewportAdapter != null)
			{
				var viewport = spriteBatch.GraphicsDevice.Viewport;
				var matrix = viewportAdapter.GetScaleMatrix();
				var offset = new Vector2(viewport.X, viewport.Y);
				var topLeft =
					Vector2.Transform(scissorRectangle.Location.ToVector2(), matrix) + offset;
				var bottomRight =
					Vector2.Transform(
						(scissorRectangle.Location + scissorRectangle.Size).ToVector2(),
						matrix
					) + offset;
				scissorRectangle = new XnaRectangleF(
					topLeft.X,
					topLeft.Y,
					bottomRight.X - topLeft.X,
					bottomRight.Y - topLeft.Y
				).ToRectangle();
			}
			_spriteBatch.GraphicsDevice.ScissorRectangle = scissorRectangle;
		}

		public void Dispose()
		{
			_spriteBatch.GraphicsDevice.ScissorRectangle = _scissorRectangle;
		}
	}

	private static readonly RasterizerState s_rasterizerState = new() { ScissorTestEnable = true };

	public SpriteBatch SpriteBatch { get; }
	public ViewportAdapter? ViewportAdapter { get; }

	private DisposableScissor _scissor;
	private bool _isOpen;

	private DisposableScissor CreateDisposableScissor() =>
		new(SpriteBatch, DrawingRectangle.Round(ClipBounds).ToXnaRectangle(), ViewportAdapter);

	// Where the control's client area is on the screen, which its paint is drawn from, at the window manager's scale: the
	// control's scale (Control.ScreenScale) is drawn by the batch's matrix (Begin).
	private Vector2 Origin => Control.PointToScreen(DrawingPoint.Empty).ToXnaPoint().ToVector2();

	// A point of the control's client area, on the screen before the control's scale.
	private Vector2 ToScreen(DrawingPoint point) => Origin + point.ToXnaPoint().ToVector2();

	// A rectangle of the control's client area, on the screen before the control's scale.
	private XnaRectangle ToScreen(DrawingRectangle rectangle) =>
		new(
			(int)Origin.X + rectangle.X,
			(int)Origin.Y + rectangle.Y,
			rectangle.Width,
			rectangle.Height
		);

	// The control's scale, around where its client area is.
	private Matrix ControlMatrix()
	{
		var scale = Control.ScreenScale;
		if (scale == 1)
			return Matrix.Identity;

		var origin = Origin;
		return Matrix.CreateTranslation(-origin.X, -origin.Y, 0)
			* Matrix.CreateScale(scale)
			* Matrix.CreateTranslation(origin.X, origin.Y, 0);
	}

	// Begins the batch, drawing through `transform` first, if any, then the control's scale and the viewport's matrix.
	private void Begin(SamplerState? samplerState = null, Matrix? transform = null) =>
		SpriteBatch.Begin(
			blendState: BlendState.AlphaBlend,
			samplerState: samplerState,
			rasterizerState: s_rasterizerState,
			transformMatrix: (transform ?? Matrix.Identity)
				* ControlMatrix()
				* (ViewportAdapter?.GetScaleMatrix() ?? Matrix.Identity)
		);

	// Starts drawing on the control: its clip, and the batch.
	private void Open()
	{
		_scissor = CreateDisposableScissor();
		Begin();
		_isOpen = true;
	}

	// Ends the drawing: the batch, and the clip as it was.
	internal void Close()
	{
		if (!_isOpen)
			return;

		SpriteBatch.End();
		_scissor.Dispose();
		_isOpen = false;
	}

	public MonoGameGraphics(
		Control control,
		SpriteBatch spriteBatch,
		ViewportAdapter? viewportAdapter = null
	)
		: base(control)
	{
		SpriteBatch = spriteBatch;
		ViewportAdapter = viewportAdapter;

		Open();
	}

	// Starts drawing again, closed, on another control, for another paint (MonoGameGraphicsFactory.Rent).
	internal void Reopen(Control control)
	{
		Rebind(control);
		Open();
	}

	private bool _disposed;

	protected override void Dispose(bool disposing)
	{
		if (_disposed)
			return;

		if (disposing)
			Close();

		_disposed = true;

		base.Dispose(disposing);
	}

	private void DrawImage(IMonoGameImage? image, DrawingPoint point)
	{
		if (image is null)
			return;

		var position = ToScreen(point);
		if (image.SamplerState is not SamplerState samplerState)
		{
			image.Draw(SpriteBatch, position);
			return;
		}

		// An image with a sampler state of its own is drawn in a batch of its own, with the same blending, clip and
		// scale.
		SpriteBatch.End();
		Begin(samplerState);
		image.Draw(SpriteBatch, position);
		SpriteBatch.End();
		Begin();
	}

	public override void DrawImage(IImage image, DrawingPoint point) =>
		DrawImage(image as IMonoGameImage, point);

	/// <summary>
	/// Draws with the sprite batch directly, with a sampler state of its own, in a batch of its own with the same blending,
	/// clip and scale: for a control that draws many sprites, such as pixel art with <see cref="SamplerState.PointClamp"/>.
	/// The drawing gets the batch and where the control's client area is on the screen.
	/// </summary>
	public void DrawWith(SamplerState samplerState, Action<SpriteBatch, Vector2> draw)
	{
		DrawWith(samplerState, null, draw);
	}

	// Draws with `draw` as DrawWith does, through `transform` first, such as a zoom, then the viewport's matrix; still
	// clipped to the control.
	public void DrawWith(
		SamplerState samplerState,
		Matrix? transform,
		Action<SpriteBatch, Vector2> draw
	)
	{
		SpriteBatch.End();
		Begin(samplerState, transform);
		draw(SpriteBatch, Origin);
		SpriteBatch.End();
		Begin();
	}

	public override void DrawImage(IImage image, DrawingPointF point) =>
		DrawImage(image, DrawingPoint.Round(point));

	public override void DrawLine(Pen pen, DrawingPoint point1, DrawingPoint point2)
	{
		if (pen.Color == DrawingColor.Transparent)
			return;

		SpriteBatch.DrawLine(ToScreen(point1), ToScreen(point2), pen.Color.ToXnaColor(), pen.Width);
	}

	public override void DrawLine(Pen pen, DrawingPointF point1, DrawingPointF point2) =>
		DrawLine(pen, DrawingPoint.Round(point1), DrawingPoint.Round(point2));

	public override void DrawLine(Pen pen, int x1, int y1, int x2, int y2) =>
		DrawLine(pen, new DrawingPoint(x1, y1), new DrawingPoint(x2, y2));

	public override void DrawLine(Pen pen, float x1, float y1, float x2, float y2) =>
		DrawLine(pen, new DrawingPointF(x1, y1), new DrawingPointF(x2, y2));

	public override void DrawRectangle(Pen pen, DrawingRectangle rectangle)
	{
		if (pen.Color == DrawingColor.Transparent)
			return;

		SpriteBatch.DrawRectangle(ToScreen(rectangle), pen.Color.ToXnaColor(), pen.Width);
	}

	public override void DrawRectangle(Pen pen, int x, int y, int width, int height) =>
		DrawRectangle(pen, new DrawingRectangle(x, y, width, height));

	public override void DrawRectangle(Pen pen, float x, float y, float width, float height) =>
		DrawRectangle(pen, DrawingRectangle.Round(new DrawingRectangleF(x, y, width, height)));

	private void DrawString(string? text, IMonoGameFont? font, Brush brush, DrawingPointF point)
	{
		if (string.IsNullOrEmpty(text))
			return;

		if (font is null)
			return;

		switch (brush)
		{
			case SolidBrush b:
				if (b.Color == DrawingColor.Transparent)
					return;

				font.Draw(
					SpriteBatch,
					text,
					ToScreen(DrawingPoint.Round(point)),
					b.Color.ToXnaColor()
				);
				break;

			default:
				throw new NotImplementedException();
		}
	}

	public override void DrawString(string? text, IFont font, Brush brush, DrawingPointF point) =>
		DrawString(text, font as IMonoGameFont, brush, point);

	public override void FillRectangle(Brush brush, DrawingRectangle rectangle)
	{
		switch (brush)
		{
			case SolidBrush b:
				if (b.Color == DrawingColor.Transparent)
					return;

				SpriteBatch.FillRectangle(ToScreen(rectangle), b.Color.ToXnaColor());
				break;

			default:
				throw new NotImplementedException();
		}
	}

	public override void FillRectangle(Brush brush, DrawingRectangleF rectangle) =>
		FillRectangle(brush, DrawingRectangle.Round(rectangle));

	public override void FillRectangle(Brush brush, int x, int y, int width, int height) =>
		FillRectangle(brush, new DrawingRectangle(x, y, width, height));

	public override void FillRectangle(Brush brush, float x, float y, float width, float height) =>
		FillRectangle(brush, new DrawingRectangleF(x, y, width, height));

	private DrawingSizeF MeasureString(string? text, IMonoGameFont? font)
	{
		if (font is null)
			return DrawingSizeF.Empty;

		return ((XnaSizeF)font.MeasureString(text)).ToDrawingSize();
	}

	public override DrawingSizeF MeasureString(string? text, IFont font) =>
		MeasureString(text, font as IMonoGameFont);
}
