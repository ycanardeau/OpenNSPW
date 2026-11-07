using Aigamo.Saruhashi;
using Aigamo.Saruhashi.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using OpenNspw.HostUI;
using static OpenNspw.all_head;
using static OpenNspw.App.Controls.UnitMarks;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;

namespace OpenNspw.App.Controls;

// The sea, the land and the units (DrawBattleArea), drawn from the snapshot with the original's sprites, in its order:
// the tiles, the lower effects, the ships, the clouds, the planes and the upper effects; then the marks of the selection,
// the paths and the targets. A click is taken as the original takes it, and given with the commands of the host's UI.
internal sealed class Battlefield : Control
{
	public static readonly DrawingRectangle Area = new(0, 0, CMBT_WIDTH, CMBT_HEIGHT);

	// The sea out of the player's sight, which the original leaves undrawn.
	private static readonly Color Fog = new(8, 18, 34);

	private readonly GameSession _session;
	private readonly UIFonts _fonts;
	private readonly BattleArt _art;
	private readonly ScrollAhead _scrollAhead;

	// Whether the player touches the screen (IAppHost.IsTouchScreen): a touch is a click once it is lifted without moving;
	// moving, it brushes over units from one of the player's (UnitBrush), and pans the battlefield from anywhere else.
	private readonly bool _isTouchScreen;

	// How far, in game pixels, a touch moves before it pans rather than clicks.
	private const int PanDistance = 12;

	// The jitter of smoke and wakes, which the original draws with the game's random numbers: here only for the look.
	private readonly Random _jitter = new();

	private GameSnapshot _snapshot = new();
	private DrawingPoint? _mouse;

	// Where a drag with the middle button, or a touch's, which scrolls the battlefield, last was.
	private DrawingPoint? _scrolledTo;

	// Where a touch that is a click unless it moves, or a second finger comes, pressed, until it is lifted or moves.
	private DrawingPoint? _pressedAt;

	public const float MinZoom = 1;
	public const float MaxZoom = 2.5f;

	// How much the battlefield is magnified, and where its window into the game's view of the battlefield is, in the view's
	// pixels: the view is drawn from there, `_zoom` times as large.
	private float _zoom = MinZoom;
	private Vector2 _window;

	// What a pan moved the game's camera by but did not give it yet, under a pixel, at a zoom.
	private Vector2 _unscrolled;

	private readonly UnitBrush _brush;

	public Battlefield(
		GameSession session,
		UIFonts fonts,
		BattleArt art,
		ScrollAhead scrollAhead,
		bool isTouchScreen
	)
	{
		_session = session;
		_fonts = fonts;
		_art = art;
		_scrollAhead = scrollAhead;
		_isTouchScreen = isTouchScreen;
		_brush = new UnitBrush(session);
		BackColor = System.Drawing.Color.Transparent;
	}

	// Shows the game as it was in the last frame: on the UI's thread, each frame.
	public void UpdateFrom(GameSnapshot snapshot)
	{
		_snapshot = snapshot;
	}

	// A point of the world on the battlefield.
	private static Vector2 ToScreen(GameSnapshot snapshot, double x, double y)
	{
		return new((int)(x - snapshot.CameraPosition.X), (int)(snapshot.CameraPosition.Y - y));
	}

	private static bool IsParkedPlane(ref Unit unit) =>
		unit.Category == UnitCategory.Plane && unit.PlaneState == UnitState.Parked;

	// Whether a unit is on the battlefield as the original draws it there: the player's, or found, or any for a replay's
	// view that sees both sides, near the view, and not a parked plane, which is on the deck (DrawBattleArea).
	private static bool IsDrawn(GameSnapshot snapshot, ref Unit unit)
	{
		var camera = snapshot.CameraPosition;
		return unit.IsUsed
			&& (unit.Side == snapshot.LocalSide || unit.IsFound || snapshot.SeesAll)
			&& !IsParkedPlane(ref unit)
			&& camera.X - CMBT_REST <= unit.Position.X
			&& camera.X + CMBT_WIDTH + CMBT_REST >= unit.Position.X
			&& camera.Y + CMBT_REST >= unit.Position.Y
			&& camera.Y - CMBT_HEIGHT - CMBT_REST <= unit.Position.Y;
	}

	// Where the original puts a unit's marks: an enemy's submerged submarine where it was last made out (DrawUnitMarks).
	private static Vector2 MarkPosition(GameSnapshot snapshot, ref Unit unit)
	{
		return
			unit.Side != snapshot.LocalSide && unit.Kind == UnitKind.Submarine && unit.IsSubmerged
			? ToScreen(snapshot, unit.ContactX, unit.ContactY)
			: ToScreen(snapshot, unit.Position.X, unit.Position.Y);
	}

	// The unit a left click at a point takes, as the original's loop does (DrawUnitMarks): the first, by number, whose
	// sprite's middle has the point, and on which a click counts.
	private static int? HitUnit(GameSnapshot snapshot, DrawingPoint point)
	{
		if (point.X >= CMBT_WIDTH - 1)
			return null;

		for (var m = 1; m <= snapshot.MaxUnitId; m++)
		{
			ref var unit = ref snapshot.Units[m];
			if (
				unit.IsUsed
				&& (unit.Side == snapshot.LocalSide || unit.IsFound)
				&& !IsParkedPlane(ref unit)
				&& snapshot.Clickable[m]
				&& IsInMiddle(MarkPosition(snapshot, ref unit), point)
			)
				return m;
		}

		return null;
	}

	// The tile under a point of the battlefield, where the original puts the landing mark (AimLanding).
	private static Vector2 SnapToTile(GameSnapshot snapshot, DrawingPoint point)
	{
		var offsetX = (int)(snapshot.CameraPosition.X - World.Left) % 80;
		var offsetY = (int)(World.Top - snapshot.CameraPosition.Y) % 80;
		return new(
			(point.X + 40 + offsetX) / 80 * 80 - offsetX,
			(point.Y + 40 + offsetY) / 80 * 80 - offsetY
		);
	}

	// Whether a left click at a point is on land where the selected transport can land, in the middle of the tile, as the
	// original takes it before any unit (AimLanding, DrawUnitMarks).
	private static bool IsLanding(GameSnapshot snapshot, DrawingPoint point)
	{
		if (
			snapshot.SelectedUnit == 0
			|| snapshot.Units[snapshot.SelectedUnit].Kind != UnitKind.Transport
			|| !snapshot.Clickable[0]
			|| point.X >= CMBT_WIDTH - 1
		)
			return false;

		var camera = snapshot.CameraPosition;
		var column = (point.X + 40 + (int)camera.X - World.Left) / 80;
		var row = (World.Top - ((int)camera.Y - point.Y - 40)) / 80;
		if (
			column is < 0 or > 255
			|| row is < 0 or > 255
			|| snapshot.MapTiles[row][column] is not (9 or 8 or 7 or 5 or 2)
		)
			return false;

		return IsInMiddle(SnapToTile(snapshot, point), point);
	}

	// The tiles the player sees, each where it is on the map: read around where the game has the battlefield, they are
	// drawn where the UI shows it, ahead while it is dragged (ScrollAhead).
	private void DrawTiles(SpriteBatch batch, Vector2 origin, GameSnapshot snapshot)
	{
		ref var tiles = ref snapshot.Sprites[SpriteId.MapTiles];
		for (var n = 0; n < UIState.SightSize; n++)
		{
			for (var m = 0; m < UIState.SightSize; m++)
			{
				if (!snapshot.SeenTiles[n * UIState.SightSize + m])
					continue;

				var (column, row) = (snapshot.FirstColumn + m, snapshot.FirstRow + n);
				var tile = snapshot.MapTiles[row][column];
				var frame = tile == 0 ? snapshot.FrameCount / 30 % 2 : 6 + (tile - 1);
				var position =
					ToScreen(
						snapshot,
						World.Left + column * tiles.Size.Width,
						World.Top - row * tiles.Size.Height
					) - new Vector2(tiles.Center.X, tiles.Center.Y);
				_art.Draw(
					batch,
					SpriteId.MapTiles,
					tiles,
					frame,
					0,
					tiles.Size.Width,
					tiles.Size.Height,
					origin + position
				);
			}
		}
	}

	// The effects of a layer that the player sees (UpdateLowerEffects, UpdateUpperEffects), but the parked planes', which
	// are on the deck.
	private void DrawEffects(
		SpriteBatch batch,
		Vector2 origin,
		GameSnapshot snapshot,
		EffectLayer layer
	)
	{
		ref var sprite = ref snapshot.Sprites[SpriteId.SubUnits];
		for (var m = 1; m < EFFECT_MAX; m++)
		{
			ref var effect = ref snapshot.Effects[m];
			if (effect.Layer != layer || !snapshot.SeenEffects[m])
				continue;

			var at = ToScreen(snapshot, effect.Position.X, effect.Position.Y);
			int frame;
			switch (effect.Animation)
			{
				case 0:
					frame = effect.SpriteNumber;
					break;
				case 1: // 対空機関砲弾がヒット
					frame = effect.SpriteNumber + sprite.FramesPerRow * (effect.TimeLeft % 2);
					break;
				case 2: // 飛行機からの煙
					frame = effect.SpriteNumber + sprite.FramesPerRow * _jitter.Next(2);
					at += new Vector2(_jitter.Next(10) - 5, _jitter.Next(10) - 5);
					break;
				case 4: // 雷跡、航跡
					frame = effect.SpriteNumber + sprite.FramesPerRow * _jitter.Next(2);
					break;
				case 10: // 対空機関砲
					batch.DrawLine(
						origin + at,
						origin + ToScreen(snapshot, effect.EndPosition.X, effect.EndPosition.Y),
						MarkLight,
						2
					);
					continue;
				case 11: // 弾丸
					batch.DrawLine(
						origin + at,
						origin + ToScreen(snapshot, effect.EndPosition.X, effect.EndPosition.Y),
						MarkLight,
						1
					);
					continue;
				case 12: // 矩形
					var corner = ToScreen(
						snapshot,
						effect.Position.X - effect.EndPosition.X,
						effect.Position.Y + effect.EndPosition.Y
					);
					batch.DrawRectangle(
						new RectangleF(
							origin + corner,
							new SizeF(
								(float)(effect.EndPosition.X * 2),
								(float)(effect.EndPosition.Y * 2)
							)
						),
						Color.White
					);
					continue;
				default: // 3: 駐機場の飛行機用
					continue;
			}

			_art.Draw(
				batch,
				SpriteId.SubUnits,
				sprite,
				frame,
				1,
				sprite.Size.Width - 1,
				sprite.Size.Height - 1,
				origin + at - new Vector2(sprite.Center.X, sprite.Center.Y)
			);
		}
	}

	// The units from `first` to `last` on the battlefield, facing as they head; a submerged submarine in its own frame,
	// and none of the enemy's (DrawUnit).
	private void DrawUnits(
		SpriteBatch batch,
		Vector2 origin,
		GameSnapshot snapshot,
		int first,
		int last
	)
	{
		for (var m = first; m <= last; m++)
		{
			ref var unit = ref snapshot.Units[m];
			if (!IsDrawn(snapshot, ref unit))
				continue;

			var id = snapshot.UnitSprites[m];
			var row = unit.SpriteRow;
			if (
				unit.Kind == UnitKind.Submarine
				&& unit.IsSubmerged
				&& snapshot.Result == GameResult.None
			)
			{
				if (unit.Side != snapshot.LocalSide && !snapshot.SeesAll)
					continue;

				row++;
			}

			ref var sprite = ref snapshot.Sprites[id];
			var at =
				ToScreen(snapshot, unit.Position.X, unit.Position.Y)
				- new Vector2(sprite.Center.X, sprite.Center.Y);
			_art.Draw(
				batch,
				id,
				sprite,
				row * 8 + Nspw.SpriteColumnOf(unit.Direction),
				1,
				sprite.Size.Width - 2,
				sprite.Size.Height - 2,
				origin + at
			);
		}
	}

	private void DrawClouds(SpriteBatch batch, Vector2 origin, GameSnapshot snapshot)
	{
		ref var tiles = ref snapshot.Sprites[SpriteId.MapTiles];
		for (var n = 0; n < KUMO_MAX; n++)
		{
			ref var cloud = ref snapshot.Clouds[n];
			if (cloud.Used == 0)
				continue;

			var at =
				ToScreen(snapshot, cloud.Position.X, cloud.Position.Y)
				- new Vector2(tiles.Size.Width / 2, tiles.Size.Height / 2);
			_art.Draw(
				batch,
				SpriteId.MapTiles,
				tiles,
				2,
				0,
				tiles.Size.Width,
				tiles.Size.Height,
				origin + at
			);
		}
	}

	// The ships under the clouds and the planes over them: the original draws the clouds as its loop reaches the first
	// plane (DrawBattleArea).
	private void DrawUnitsAndClouds(SpriteBatch batch, Vector2 origin, GameSnapshot snapshot)
	{
		var firstPlane = snapshot.FirstPlane;
		DrawUnits(batch, origin, snapshot, 1, Math.Min(firstPlane - 1, snapshot.MaxUnitId));
		if (firstPlane > snapshot.MaxUnitId)
			return;

		DrawClouds(batch, origin, snapshot);
		DrawUnits(batch, origin, snapshot, firstPlane, snapshot.MaxUnitId);
	}

	// The direction of a unit's target, or of where a transport lands, or a cross for a target out of sight
	// (DrawTargetLine).
	private static void DrawTargetArrow(
		SpriteBatch batch,
		Vector2 origin,
		GameSnapshot snapshot,
		ref Unit unit
	)
	{
		var at = origin + ToScreen(snapshot, unit.Position.X, unit.Position.Y);
		ref var target = ref snapshot.Units[unit.Target];
		var isLanding = unit.Kind == UnitKind.Transport && unit.Target == snapshot.MaxUnitId + 1;
		if (
			(target.IsFound || isLanding)
			&& !(target.Kind == UnitKind.Submarine && target.IsSubmerged)
		)
		{
			var to =
				origin
				+ (
					unit.Kind == UnitKind.Transport
						? ToScreen(snapshot, unit.LandingX, unit.LandingY)
						: ToScreen(snapshot, target.Position.X, target.Position.Y)
				);
			var direction = to == at ? Vector2.UnitX : Vector2.Normalize(to - at);
			var side = new Vector2(-direction.Y, direction.X);
			var tip = at + direction * 40;
			batch.DrawLine(tip, at + direction * 26 + side * 7, Mark, 2);
			batch.DrawLine(tip, at + direction * 26 - side * 7, Mark, 2);
		}
		else
		{
			batch.DrawLine(at + new Vector2(30, -40), at + new Vector2(10, -20), Enemy, 2);
			batch.DrawLine(at + new Vector2(10, -40), at + new Vector2(30, -20), Enemy, 2);
		}
	}

	// The selected unit's target or landing point, pulsing, its carrier for a flying plane, its emergency course, its path,
	// and the line from where the next destination adds to it to the mouse (DrawUnitMarks, DrawPathAndSetDestination).
	private void DrawSelected(SpriteBatch batch, Vector2 origin, GameSnapshot snapshot)
	{
		ref var selected = ref snapshot.Units[snapshot.SelectedUnit];
		var pulse = 30 - snapshot.FrameCount % 8 * 2;
		if (selected.Kind == UnitKind.Transport && selected.Target == snapshot.MaxUnitId + 1)
		{
			DrawBox(
				batch,
				origin + ToScreen(snapshot, selected.LandingX, selected.LandingY),
				pulse,
				Mark,
				2
			);
		}
		else if (selected.Target != 0)
		{
			ref var target = ref snapshot.Units[selected.Target];
			if (
				target.IsUsed
				&& (target.Side == snapshot.LocalSide || target.IsFound)
				&& !IsParkedPlane(ref target)
			)
				DrawBox(batch, origin + MarkPosition(snapshot, ref target), pulse, Enemy, 2);
		}
		else if (
			selected.Category == UnitCategory.Plane
			&& selected.PlaneState == UnitState.Flying
			&& selected.Carrier != 0
		)
		{
			ref var carrier = ref snapshot.Units[selected.Carrier];
			var at = origin + ToScreen(snapshot, carrier.Position.X, carrier.Position.Y);
			batch.DrawLine(at + new Vector2(-20, 24), at + new Vector2(20, 24), Mark, 2);
		}

		if (selected.Side != snapshot.LocalSide)
			return;

		ref var carrierOrSelf = ref IsParkedPlane(ref selected)
			? ref snapshot.Units[selected.Carrier]
			: ref selected;
		var start = origin + ToScreen(snapshot, carrierOrSelf.Position.X, carrierOrSelf.Position.Y);
		if (selected.EmergencyFlags[0] != 0)
			batch.DrawLine(
				start,
				origin
					+ ToScreen(
						snapshot,
						selected.EmergencyDestination.X,
						selected.EmergencyDestination.Y
					),
				Enemy,
				1
			);

		var end = start;
		for (var n = 0; n < 64 && !selected.IsPathEnd(n); n++)
		{
			var point = origin + ToScreen(snapshot, selected.PathX[n], selected.PathY[n]);
			batch.DrawLine(end, point, Mark * 0.8f, 1);
			DrawBox(batch, point, 10, Mark);
			end = point;
		}

		if (_mouse is { } mouse && snapshot.CanSetDestination && HitUnit(snapshot, mouse) is null)
			batch.DrawLine(
				snapshot.ClearsPath ? start : end,
				origin + new Vector2(mouse.X, mouse.Y),
				MarkLight * 0.6f,
				1
			);
	}

	// The marks: the selection, the directions of the player's units' targets, the selected unit's, and what a click at
	// the mouse would take.
	private void DrawMarks(SpriteBatch batch, Vector2 origin, GameSnapshot snapshot)
	{
		for (var m = 1; m <= snapshot.MaxUnitId; m++)
		{
			ref var unit = ref snapshot.Units[m];
			if (
				!unit.IsUsed
				|| !(unit.Side == snapshot.LocalSide || unit.IsFound)
				|| IsParkedPlane(ref unit)
			)
				continue;

			DrawSelection(batch, origin + MarkPosition(snapshot, ref unit), snapshot, m);

			if (unit.Target != 0 && unit.Side == snapshot.LocalSide)
				DrawTargetArrow(batch, origin, snapshot, ref unit);
		}

		if (snapshot.SelectedUnit != 0 && !snapshot.IsEditingMap)
			DrawSelected(batch, origin, snapshot);

		if (_mouse is not { } mouse)
			return;

		if (IsLanding(snapshot, mouse))
			DrawBox(batch, origin + SnapToTile(snapshot, mouse), 25, MarkLight);
		else if (HitUnit(snapshot, mouse) is { } hit)
			DrawBox(batch, origin + MarkPosition(snapshot, ref snapshot.Units[hit]), 25, MarkLight);
	}

	// A left click takes the land where the selected transport lands, then a unit, then gives the selected unit a
	// destination (DrawBattleArea).
	private void TakeClick(DrawingPoint point)
	{
		var snapshot = _snapshot;
		var commands = _session.Commands;
		if (IsLanding(snapshot, point))
			commands.ClickLand(point.X, point.Y);
		else if (HitUnit(snapshot, point) is { } m)
			commands.ClickUnit(m);
		else
			commands.SetDestination(
				new WorldPosition(
					point.X + snapshot.CameraPosition.X,
					snapshot.CameraPosition.Y - point.Y
				)
			);
	}

	// The farthest the window goes into the view at the zoom, which the battlefield, as wide as the view times the zoom at
	// most (BattleForm.Fit), shows a part of.
	private Vector2 WindowRange =>
		Vector2.Max(
			Vector2.Zero,
			new Vector2(CMBT_WIDTH - Width / _zoom, CMBT_HEIGHT - Height / _zoom)
		);

	// A point of the control in the view's pixels, through the zoom.
	private DrawingPoint ToView(DrawingPoint point) =>
		new((int)(_window.X + point.X / _zoom), (int)(_window.Y + point.Y / _zoom));

	// Moves what is shown by `move`, in the view's pixels: the window first, within the view, and then the game's camera
	// with what is left, at once (ScrollAhead).
	public void Pan(Vector2 move)
	{
		var target = _window + move;
		_window = Vector2.Clamp(target, Vector2.Zero, WindowRange);
		_unscrolled += target - _window;
		var (dx, dy) = ((int)_unscrolled.X, (int)_unscrolled.Y);
		if (dx != 0 || dy != 0)
		{
			_scrollAhead.Scroll(dx, dy);
			_unscrolled -= new Vector2(dx, dy);
		}
	}

	// How much the battlefield is magnified, from MinZoom to MaxZoom.
	public float Zoom => _zoom;

	// Magnifies the battlefield by `zoom` around `focus`, a point of the control that stays where it is.
	public void SetZoom(float zoom, DrawingPoint focus)
	{
		var at = new Vector2(_window.X + focus.X / _zoom, _window.Y + focus.Y / _zoom);
		_zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
		_window = Vector2.Clamp(
			at - new Vector2(focus.X, focus.Y) / _zoom,
			Vector2.Zero,
			WindowRange
		);
	}

	// Shows the view from `at`, in its pixels, as far as the zoom lets it.
	public void ShowWindow(Vector2 at)
	{
		_window = Vector2.Clamp(at, Vector2.Zero, WindowRange);
	}

	// What is shown, in the view's pixels from the game's camera.
	public RectangleF Shown => new(_window.X, _window.Y, Width / _zoom, Height / _zoom);

	// Makes the battlefield `width` wide, at most the view's width times the zoom, keeping what is shown in the view.
	public void FitWidth(int width)
	{
		Width = width;
		_window = Vector2.Clamp(_window, Vector2.Zero, WindowRange);
	}

	// The battlefield follows a drag with the middle button, or a touch's, as a chart pushed across the table.
	private void Scroll(DrawingPoint point)
	{
		if (_scrolledTo is not { } from || point == from)
			return;

		Pan(new Vector2(from.X - point.X, from.Y - point.Y) / _zoom);
		_scrolledTo = point;
	}

	// Whether a point is on one of the player's units, where a touch that moves brushes over units (UnitBrush) rather than
	// panning.
	private bool IsOwnUnit(DrawingPoint point)
	{
		return !IsLanding(_snapshot, point)
			&& HitUnit(_snapshot, point) is { } m
			&& _snapshot.Units[m].Side == _snapshot.LocalSide;
	}

	// A press of the left button at a point of the view: a click, which starts a brush on one of the player's units.
	private void Click(DrawingPoint at)
	{
		_brush.Press(_snapshot, IsLanding(_snapshot, at) ? null : HitUnit(_snapshot, at));
		TakeClick(at);
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		_mouse = ToView(e.Location);
		if (
			_pressedAt is { } pressed
			&& Math.Abs(e.Location.X - pressed.X) + Math.Abs(e.Location.Y - pressed.Y) > PanDistance
		)
		{
			_pressedAt = null;
			if (IsOwnUnit(ToView(pressed)) && !_session.IsWatching)
				Click(ToView(pressed));
			else
				_scrolledTo = pressed;
		}

		Scroll(e.Location);
		_brush.Over(HitUnit(_snapshot, ToView(e.Location)));
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		_mouse = null;
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);

		// A touch waits to tell a click from a brush, a pan, or the second finger of a pinch.
		if (_isTouchScreen && e.Button == MouseButtons.Left)
		{
			_pressedAt = e.Location;
			return;
		}

		// A second finger (XButton1, TouchPointer) makes the touch no click, and pans or zooms instead.
		_pressedAt = null;
		if (e.Button == MouseButtons.XButton1)
			return;

		// A replay takes no clicks while it plays: only its view moves, with the middle button or a touch.
		if (_session.IsWatching && e.Button != MouseButtons.Middle)
			return;

		if (e.Button == MouseButtons.Left)
			Click(ToView(e.Location));
		else if (e.Button == MouseButtons.Right)
			_session.Commands.RightClick();
		else if (e.Button == MouseButtons.Middle)
			_scrolledTo = e.Location;
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (e.Button == MouseButtons.Middle)
		{
			Scroll(e.Location);
			_scrolledTo = null;
		}
		else if (e.Button == MouseButtons.Left)
		{
			// A touch lifted where it pressed is a click there; one that moved ends its pan.
			if (_pressedAt is { } pressed && !_session.IsWatching)
				Click(ToView(pressed));
			else if (_isTouchScreen)
				Scroll(e.Location);

			_pressedAt = null;
			if (_isTouchScreen)
				_scrolledTo = null;
			_brush.Release();
		}
	}

	// The wheel, or two fingers spread or pinched (TouchPointer), zooms around the pointer: a notch, 120, by a quarter.
	protected override void OnMouseWheel(MouseEventArgs e)
	{
		base.OnMouseWheel(e);
		SetZoom(_zoom * MathF.Pow(1.25f, e.Delta / 120f), e.Location);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		var snapshot = _snapshot;
		var at = PointToScreen(DrawingPoint.Empty);
		var zoom =
			Matrix.CreateTranslation(-at.X - _window.X, -at.Y - _window.Y, 0)
			* Matrix.CreateScale(_zoom)
			* Matrix.CreateTranslation(at.X, at.Y, 0);
		((MonoGameGraphics)e.Graphics).DrawWith(
			SamplerState.PointClamp,
			zoom,
			(batch, origin) =>
			{
				batch.FillRectangle(new RectangleF(origin.X, origin.Y, Width, Height), Fog);
				DrawTiles(batch, origin, snapshot);
				DrawEffects(batch, origin, snapshot, EffectLayer.Lower);
				DrawUnitsAndClouds(batch, origin, snapshot);
				DrawEffects(batch, origin, snapshot, EffectLayer.Upper);
				DrawMarks(batch, origin, snapshot);
			}
		);
		GameMessages.PaintLines(e.Graphics, _fonts, snapshot, 16, 16);
		GameMessages.PaintResult(e.Graphics, _fonts, snapshot, ClientRectangle);
	}
}
