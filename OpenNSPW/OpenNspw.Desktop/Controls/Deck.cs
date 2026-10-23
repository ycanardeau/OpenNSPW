using Aigamo.Saruhashi;
using Aigamo.Saruhashi.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using static OpenNspw.all_head;
using static OpenNspw.Desktop.Controls.UnitMarks;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;

namespace OpenNspw.Desktop.Controls;

// The picture of the unit whose information the sidebar shows (DrawUnitDetails): a carrier's or an air base's flight
// deck or hangar, with the planes parked there (DrawUnit) and their effects, drawn from the snapshot with the original's
// sprites. A click on a parked plane is taken as the original takes it, and given with the commands of the host's UI.
internal sealed class Deck : Control
{
	// Where the deck is on the game's screen, as the original puts the unit's information (JapanUnitInfo, set_sprt_data).
	public static readonly DrawingRectangle Area = new(CMBT_WIDTH, 0, 120, 438);

	private readonly GameSession _session;
	private readonly BattleArt _art;
	private readonly UnitBrush _brush;
	private GameSnapshot _snapshot = new();
	private DrawingPoint? _mouse;

	public Deck(GameSession session, BattleArt art)
	{
		_session = session;
		_art = art;
		_brush = new UnitBrush(session);
		BackColor = System.Drawing.Color.Transparent;
	}

	// Shows the game as it was in the last frame: on the UI's thread, each frame.
	public void UpdateFrom(GameSnapshot snapshot)
	{
		_snapshot = snapshot;
	}

	// A point of the game's screen on the deck: the parked planes and their effects are where the original draws them,
	// on the screen and not in the world.
	private static Vector2 ToDeck(double x, double y)
	{
		return new((int)x - Area.X, (int)y - Area.Y);
	}

	// The parked plane a left click at a point takes, as the original's loop does (DrawUnitMarks): the first, by number,
	// whose sprite's middle has the point, and on which a click counts.
	private static int? HitPlane(GameSnapshot snapshot, DrawingPoint point)
	{
		for (var m = 1; m <= snapshot.MaxUnitId; m++)
		{
			ref var unit = ref snapshot.Units[m];
			if (snapshot.ShownOnDeck[m] && snapshot.Clickable[m] && IsInMiddle(ToDeck(unit.Position.X, unit.Position.Y), point))
				return m;
		}

		return null;
	}

	// The unit's picture: its kind's, or for a carrier or an air base, its hangar's when the view is switched to it
	// (DrawUnitDetails).
	private void DrawPicture(SpriteBatch batch, Vector2 origin, GameSnapshot snapshot)
	{
		var kind = (UnitKind)snapshot.UnitInfoPanel[0];
		if (kind == 0)
			return;

		var frame = snapshot.UnitInfoPanel[1] == 0 ? snapshot.UnitInfoPanel[2] : kind switch
		{
			UnitKind.AirBase => 9,
			UnitKind.Carrier => 10,
			UnitKind.LightCarrier => 11,
			_ => snapshot.UnitInfoPanel[2],
		};
		ref var sprite = ref snapshot.Sprites[snapshot.UnitInfoPanel[4] == (int)Side.Japan ? SpriteId.JapanUnitInfo : SpriteId.UnitedStatesUnitInfo];
		_art.Draw(batch, sprite, frame, 1, sprite.Width - 3, sprite.Height - 2, origin);
	}

	// The parked planes' effects of a layer that the player sees (UpdateLowerEffects, UpdateUpperEffects: 駐機場の飛行機用).
	private void DrawEffects(SpriteBatch batch, Vector2 origin, GameSnapshot snapshot, EffectLayer layer)
	{
		ref var sprite = ref snapshot.Sprites[SpriteId.SubUnits];
		for (var m = 1; m < EFFECT_MAX; m++)
		{
			ref var effect = ref snapshot.Effects[m];
			if (effect.Layer != layer || effect.Animation != 3 || !snapshot.SeenEffects[m])
				continue;

			var at = ToDeck(effect.Position.X, effect.Position.Y) - new Vector2(sprite.CenterX, sprite.CenterY);
			_art.Draw(batch, sprite, effect.SpriteNumber, 1, sprite.Width - 1, sprite.Height - 1, origin + at);
		}
	}

	// The planes parked on the deck shown, facing as they head (DrawUnit).
	private void DrawPlanes(SpriteBatch batch, Vector2 origin, GameSnapshot snapshot)
	{
		for (var m = 1; m <= snapshot.MaxUnitId; m++)
		{
			if (!snapshot.ShownOnDeck[m])
				continue;

			ref var unit = ref snapshot.Units[m];
			ref var sprite = ref snapshot.Sprites[snapshot.UnitSprites[m]];
			var at = ToDeck(unit.Position.X, unit.Position.Y) - new Vector2(sprite.CenterX, sprite.CenterY);
			_art.Draw(batch, sprite, unit.SpriteRow * 8 + Nspw.SpriteColumnOf(unit.Direction), 1, sprite.Width - 2, sprite.Height - 2, origin + at);
		}
	}

	// The selection's marks on the planes, and what a click at the mouse would take (DrawUnitMarks).
	private void DrawMarks(SpriteBatch batch, Vector2 origin, GameSnapshot snapshot)
	{
		for (var m = 1; m <= snapshot.MaxUnitId; m++)
		{
			ref var unit = ref snapshot.Units[m];
			if (snapshot.ShownOnDeck[m])
				DrawSelection(batch, origin + ToDeck(unit.Position.X, unit.Position.Y), snapshot, m);
		}

		if (_mouse is { } mouse && HitPlane(snapshot, mouse) is { } hit)
		{
			ref var unit = ref snapshot.Units[hit];
			DrawBox(batch, origin + ToDeck(unit.Position.X, unit.Position.Y), 25, MarkLight);
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		_mouse = e.Location;
		_brush.Over(HitPlane(_snapshot, e.Location));
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		_mouse = null;
	}

	// A left click takes a parked plane, and with the button held, brushes the planes parked by it into the selection or
	// out of it (UnitBrush); one elsewhere on the deck is not on the battlefield, and gives no destination
	// (DrawPathAndSetDestination). The right button deselects anywhere, as on the battlefield (DrawBattleArea).
	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (e.Button == MouseButtons.Left)
		{
			var m = HitPlane(_snapshot, e.Location);
			_brush.Press(_snapshot, m);
			if (m is { } plane)
				_session.Game.Commands.ClickUnit(plane);
		}
		else if (e.Button == MouseButtons.Right)
		{
			_session.Game.Commands.RightClick();
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (e.Button == MouseButtons.Left)
			_brush.Release();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		var snapshot = _snapshot;
		((MonoGameGraphics)e.Graphics).DrawWith(SamplerState.PointClamp, (batch, origin) =>
		{
			batch.FillRectangle(new RectangleF(origin.X, origin.Y, Width, Height), Theme.Ink.ToXnaColor());
			DrawPicture(batch, origin, snapshot);
			DrawEffects(batch, origin, snapshot, EffectLayer.Lower);
			DrawPlanes(batch, origin, snapshot);
			DrawEffects(batch, origin, snapshot, EffectLayer.Upper);
			DrawMarks(batch, origin, snapshot);
		});
	}
}
