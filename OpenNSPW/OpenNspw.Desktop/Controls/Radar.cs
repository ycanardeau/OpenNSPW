using System.Runtime.InteropServices;
using Aigamo.Saruhashi;
using Aigamo.Saruhashi.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;
using DrawingColor = System.Drawing.Color;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;

namespace OpenNspw.Desktop.Controls;

// The map of the whole battle (DrawMinimap): a pixel a tile of 80, with the player's units, the enemy's it has found,
// and where the battlefield is. A click held on it moves the battlefield there, with the commands of the host's UI.
internal sealed class Radar : Control
{
	// The minimap's top: 199 high, at the bottom of the screen.
	public const int Top = all_head.CMBT_HEIGHT - 199;

	public static readonly DrawingRectangle Area = new(all_head.CMBT_WIDTH, Top, all_head.SCRN_WIDTH - all_head.CMBT_WIDTH, all_head.SCRN_HEIGHT - Top);

	// The map, 240 by 180 tiles, 8 from the minimap's corner, as the original draws it (MakeMinimap).
	private const int MapLeft = 8;
	private const int MapTop = 8;
	private const int Columns = 240;
	private const int Rows = 180;

	private static readonly Color Sea = new(10, 22, 38);
	private static readonly Color Land = new(58, 104, 72);

	private readonly GameSession _session;
	private readonly Texture2D _map;
	private readonly Color[] _pixels = new Color[Columns * Rows];
	private readonly ushort[] _tilesShown = new ushort[256 * 256];
	private bool _hasMap;
	private GameSnapshot _snapshot = new();
	private bool _isHeld;

	public Radar(GameSession session, BattleArt art)
	{
		_session = session;
		_map = art.RadarMap;
		BackColor = DrawingColor.Transparent;
	}

	private static ReadOnlySpan<ushort> Tiles(GameSnapshot snapshot)
	{
		return MemoryMarshal.CreateReadOnlySpan(ref snapshot.MapTiles[0][0], 256 * 256);
	}

	// Draws the map again when its tiles change: a new battle's, or the map editor's.
	private void UpdateMap(GameSnapshot snapshot)
	{
		var tiles = Tiles(snapshot);
		if (_hasMap && tiles.SequenceEqual(_tilesShown))
			return;

		tiles.CopyTo(_tilesShown);
		for (var row = 0; row < Rows; row++)
		{
			for (var column = 0; column < Columns; column++)
				_pixels[row * Columns + column] = snapshot.MapTiles[row][column] != 0 ? Land : Sea;
		}

		_map.SetData(_pixels);
		_hasMap = true;
	}

	// Shows the game as it was in the last frame: on the UI's thread, each frame.
	public void UpdateFrom(GameSnapshot snapshot)
	{
		_snapshot = snapshot;
		UpdateMap(snapshot);
	}

	// The battlefield is moved to where the click is held, if it is on the map: the cursor is the camera's box's
	// center, 5 from its corner (DrawMinimap).
	private void ShowPoint(DrawingPoint point)
	{
		if (point.X >= MapLeft && point.X <= MapLeft + Columns && point.Y >= MapTop && point.Y <= MapTop + Rows)
			_session.Game.Commands.ShowMinimapCell(point.X - 5 - MapLeft, point.Y - 5 - MapTop);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (e.Button == MouseButtons.Left)
		{
			_isHeld = true;
			ShowPoint(e.Location);
		}
		else if (e.Button == MouseButtons.Right)
		{
			// The right button deselects anywhere, as on the battlefield (DrawBattleArea).
			_session.Game.Commands.RightClick();
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (_isHeld)
			ShowPoint(e.Location);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (e.Button == MouseButtons.Left)
			_isHeld = false;
	}

	// A unit's tile on the map, from a point of the world, as the original puts its dot (DrawMinimap).
	private static DrawingPoint Cell(double x, double y)
	{
		return new(MapLeft + (Math.Abs(all_head.MAP_LEFT) + (int)(x - 40)) / 80, MapTop + (Math.Abs(all_head.MAP_TOP) - (int)(y + 40)) / 80);
	}

	private static bool IsOnMap(ref Unit unit)
	{
		return !(unit.Position.Y > all_head.MAP_TOP || unit.Position.Y < all_head.MAP_BOTTOM || unit.Position.X < all_head.MAP_LEFT || unit.Position.X > all_head.MAP_RIGHT);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		var g = e.Graphics;
		var snapshot = _snapshot;
		Painter.Fill(g, ClientRectangle, Theme.Navy);
		Painter.Fill(g, new(0, 0, Width, 1), Theme.Alpha(Theme.Line, 0.9f));
		g.DrawImage(MonoGameImage.Create(new Texture2DRegion(_map, 0, 0, Columns, Rows), Color.White, SamplerState.PointClamp), new DrawingPoint(MapLeft, MapTop));

		for (var x = 20; x < Columns; x += 20)
			Painter.Fill(g, new(MapLeft + x, MapTop, 1, Rows), Theme.Alpha(Theme.Line, 0.25f));

		for (var y = 20; y < Rows; y += 20)
			Painter.Fill(g, new(MapLeft, MapTop + y, Columns, 1), Theme.Alpha(Theme.Line, 0.25f));

		Painter.Frame(g, new(MapLeft - 1, MapTop - 1, Columns + 2, Rows + 2), Theme.Alpha(Theme.Line, 0.9f));

		// The player's units, but parked planes; the enemy's it has found, a submerged submarine where it was found.
		// Planes blink (DrawMinimap).
		for (var m = 1; m <= snapshot.MaxUnitId; m++)
		{
			ref var unit = ref snapshot.Units[m];
			if (!unit.IsUsed || !IsOnMap(ref unit) || unit.PlaneState == UnitState.Parked || (unit.Category == UnitCategory.Plane && snapshot.FrameCount % 4 == 0))
				continue;

			DrawingPoint cell;
			if (unit.Side == snapshot.LocalSide)
				cell = Cell(unit.Position.X, unit.Position.Y);
			else if (!unit.IsFound)
				continue;
			else if (unit.Kind == UnitKind.Submarine && unit.IsSubmerged)
				cell = Cell(unit.ContactX, unit.ContactY);
			else
				cell = Cell(unit.Position.X, unit.Position.Y);

			Painter.Fill(g, new(cell.X, cell.Y, 3, 3), unit.Side == Side.Japan ? Theme.Japan : Theme.UnitedStates);
		}

		// Where the battlefield is: its top left corner's tile, 10 tiles across.
		var camera = snapshot.CameraPosition;
		var left = MapLeft + (Math.Abs(all_head.MAP_LEFT) + (int)camera.X) / 80;
		var top = MapTop + (Math.Abs(all_head.MAP_TOP) - (int)camera.Y) / 80;
		Painter.Frame(g, new(left, top, 11, 11), Theme.BrassLight);
	}
}
