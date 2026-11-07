using System.Runtime.InteropServices;
using Aigamo.Saruhashi;
using Aigamo.Saruhashi.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Graphics;
using DrawingColor = System.Drawing.Color;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;

namespace OpenNspw.App.Controls;

// The map of the whole battle (DrawMinimap): a pixel a tile of 80, drawn `Scale` times as large, with the player's units,
// the enemy's it has found, and what the battlefield shows (Shown). A click held on it moves the battlefield there, with
// the commands of the host's UI. A touch, which a finger makes far less precise, moves it there only when it is lifted
// without moving; moving, it drags what is shown, a part of the way the finger goes (DragRatio).
internal sealed class Minimap : Control
{
	// The minimap's top: 199 high, at the bottom of the screen.
	public const int Top = all_head.CMBT_HEIGHT - 199;

	public static readonly DrawingRectangle Area = new(
		all_head.CMBT_WIDTH,
		Top,
		all_head.SCRN_WIDTH - all_head.CMBT_WIDTH,
		all_head.SCRN_HEIGHT - Top
	);

	// The map, 240 by 180 tiles, 8 from the minimap's corner, as the original draws it (MakeMinimap).
	private const int MapLeft = 8;
	private const int MapTop = 8;
	private const int Columns = 240;
	private const int Rows = 180;

	// How far a touch moves, in game pixels, before it drags rather than taps.
	private const int DragDistance = 8;

	// How much of the way a finger drags the minimap the battlefield moves: a third, as a pixel of the minimap is 80 of
	// the battlefield.
	private const float DragRatio = 1 / 3f;

	private static readonly Color Sea = new(10, 22, 38);
	private static readonly Color Land = new(104, 150, 96);

	private readonly GameSession _session;
	private readonly Texture2D _map;
	private readonly bool _isTouchScreen;
	private readonly Action<Vector2> _pan;
	private readonly Action<Vector2> _showWindow;
	private readonly Color[] _pixels = new Color[Columns * Rows];
	private readonly ushort[] _tilesShown = new ushort[256 * 256];
	private bool _hasMap;
	private GameSnapshot _snapshot = new();
	private bool _isHeld;

	// Where a touch pressed, and where it dragged to last, in the minimap's pixels, until it is lifted.
	private Vector2? _pressedAt;
	private Vector2? _draggedTo;

	// What the battlefield shows, in its view's pixels from the game's camera: the view, or a part of it, magnified
	// (Battlefield.Shown).
	public RectangleF Shown { get; set; } = new(0, 0, all_head.CMBT_WIDTH, all_head.CMBT_HEIGHT);

	// How many times as large the minimap is drawn: the larger, the less a slip of a finger moves the battlefield.
	public float Scale { get; private set; } = 1;

	// The minimap's size at a scale.
	public static System.Drawing.Size SizeAt(float scale) =>
		new((int)(Area.Width * scale), (int)(Area.Height * scale));

	public void SetScale(float scale)
	{
		Scale = scale;
		Size = SizeAt(scale);
	}

	// isTouchScreen: whether the player touches the screen (IAppHost.IsTouchScreen). pan: moves what the battlefield shows
	// by so many of its view's pixels (Battlefield.Pan). showWindow: shows the view from a point of it (Battlefield.ShowWindow).
	public Minimap(
		GameSession session,
		BattleArt art,
		bool isTouchScreen,
		Action<Vector2> pan,
		Action<Vector2> showWindow
	)
	{
		_session = session;
		_map = art.MinimapTexture;
		_isTouchScreen = isTouchScreen;
		_pan = pan;
		_showWindow = showWindow;
		BackColor = DrawingColor.Transparent;
		Size = Area.Size;
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

	// A point of the control in the minimap's pixels, through its scale.
	private Vector2 ToMap(DrawingPoint point) => new(point.X / Scale, point.Y / Scale);

	// The battlefield is moved to where the click is held, if it is on the map: the cursor is the camera's box's
	// center, 5 from its corner (DrawMinimap). The part of the view the battlefield shows, magnified, is moved to the
	// cursor too, within the view, wherever the game puts its camera, which it keeps on the map (ShowMinimapCell).
	private void ShowPoint(Vector2 at, bool isPressed)
	{
		var (x, y) = ((int)at.X, (int)at.Y);
		if (x < MapLeft || x > MapLeft + Columns || y < MapTop || y > MapTop + Rows)
			return;

		var (column, row) = (x - 5 - MapLeft, y - 5 - MapTop);
		var camera = new Vector2(
			Math.Clamp(World.Left + column * 80, World.Left, World.Right - all_head.CMBT_WIDTH),
			Math.Clamp(World.Top - row * 80, World.Bottom + all_head.CMBT_HEIGHT, World.Top)
		);
		var cursor = new Vector2(
			World.Left + (x - MapLeft) * 80 - camera.X,
			camera.Y - (World.Top - (y - MapTop) * 80)
		);
		var shown = Shown;
		_showWindow(cursor - new Vector2(shown.Width, shown.Height) / 2);

		// A replay's view moves, not the battle's (ReplayView).
		if (_session.IsWatching && _session.View is { } view)
			view.ShowMinimapCell(column, row);
		else
			_session.Commands.ShowMinimapCell(column, row, isPressed);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (e.Button == MouseButtons.Left && _isTouchScreen)
		{
			_pressedAt = ToMap(e.Location);
		}
		else if (e.Button == MouseButtons.Left)
		{
			_isHeld = true;
			ShowPoint(ToMap(e.Location), isPressed: true);
		}
		else if (e.Button == MouseButtons.XButton1)
		{
			// A second finger: the touch is no tap.
			_pressedAt = null;
		}
		else if (e.Button == MouseButtons.Right)
		{
			// The right button deselects anywhere, as on the battlefield (DrawBattleArea).
			if (!_session.IsWatching)
				_session.Commands.RightClick();
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		var at = ToMap(e.Location);
		if (_isHeld)
			ShowPoint(at, isPressed: false);
		if (_pressedAt is { } pressed && Vector2.Distance(at, pressed) * Scale > DragDistance)
		{
			_pressedAt = null;
			_draggedTo = pressed;
		}

		if (_draggedTo is { } from)
		{
			_pan((at - from) * 80 * DragRatio);
			_draggedTo = at;
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (e.Button != MouseButtons.Left)
			return;

		if (_pressedAt is { } pressed)
			ShowPoint(pressed, isPressed: true);
		_isHeld = false;
		_pressedAt = null;
		_draggedTo = null;
	}

	// A unit's tile on the map, from a point of the world, as the original puts its dot (DrawMinimap).
	private static DrawingPoint Cell(double x, double y)
	{
		return new(
			MapLeft + (Math.Abs(World.Left) + (int)(x - 40)) / 80,
			MapTop + (Math.Abs(World.Top) - (int)(y + 40)) / 80
		);
	}

	private static bool IsOnMap(ref Unit unit)
	{
		return !(
			unit.Position.Y > World.Top
			|| unit.Position.Y < World.Bottom
			|| unit.Position.X < World.Left
			|| unit.Position.X > World.Right
		);
	}

	// The minimap at its scale, drawn with the sprite batch, its pixels kept sharp.
	private void Draw(SpriteBatch batch, Vector2 origin, GameSnapshot snapshot)
	{
		void Fill(float x, float y, float width, float height, DrawingColor color) =>
			batch.FillRectangle(
				new RectangleF(origin.X + x, origin.Y + y, width, height),
				color.ToXnaColor()
			);

		void Frame(float x, float y, float width, float height, DrawingColor color) =>
			batch.DrawRectangle(
				new RectangleF(origin.X + x, origin.Y + y, width, height),
				color.ToXnaColor()
			);

		Fill(0, 0, Area.Width, Area.Height, Theme.Navy);
		Fill(0, 0, Area.Width, 1, Theme.Alpha(Theme.Line, 0.9f));
		batch.Draw(
			_map,
			origin + new Vector2(MapLeft, MapTop),
			new Rectangle(0, 0, Columns, Rows),
			Color.White
		);

		for (var x = 20; x < Columns; x += 20)
			Fill(MapLeft + x, MapTop, 1, Rows, Theme.Alpha(Theme.Line, 0.25f));

		for (var y = 20; y < Rows; y += 20)
			Fill(MapLeft, MapTop + y, Columns, 1, Theme.Alpha(Theme.Line, 0.25f));

		Frame(MapLeft - 1, MapTop - 1, Columns + 2, Rows + 2, Theme.Alpha(Theme.Line, 0.9f));

		// The player's units, but parked planes; the enemy's it has found, a submerged submarine where it was found.
		// Planes blink (DrawMinimap).
		for (var m = 1; m <= snapshot.MaxUnitId; m++)
		{
			ref var unit = ref snapshot.Units[m];
			if (
				!unit.IsUsed
				|| !IsOnMap(ref unit)
				|| unit.PlaneState == UnitState.Parked
				|| (unit.Category == UnitCategory.Plane && snapshot.FrameCount % 4 == 0)
			)
				continue;

			DrawingPoint cell;
			if (unit.Side == snapshot.LocalSide || snapshot.SeesAll)
				cell = Cell(unit.Position.X, unit.Position.Y);
			else if (!unit.IsFound)
				continue;
			else if (unit.Kind == UnitKind.Submarine && unit.IsSubmerged)
				cell = Cell(unit.ContactX, unit.ContactY);
			else
				cell = Cell(unit.Position.X, unit.Position.Y);

			Fill(cell.X, cell.Y, 3, 3, unit.Side == Side.Japan ? Theme.Japan : Theme.UnitedStates);
		}

		// What the battlefield shows: the view's top left corner's tile, 10 tiles across, or the part of it shown.
		var camera = snapshot.CameraPosition;
		var shown = Shown;
		var left = MapLeft + (Math.Abs(World.Left) + (int)camera.X) / 80;
		var top = MapTop + (Math.Abs(World.Top) - (int)camera.Y) / 80;
		Frame(
			left + shown.X / 80,
			top + shown.Y / 80,
			shown.Width / 80 + 1.4f,
			shown.Height / 80 + 1.4f,
			Theme.BrassLight
		);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		var snapshot = _snapshot;
		var at = PointToScreen(DrawingPoint.Empty);
		var scale =
			Matrix.CreateTranslation(-at.X, -at.Y, 0)
			* Matrix.CreateScale(Scale)
			* Matrix.CreateTranslation(at.X, at.Y, 0);
		((MonoGameGraphics)e.Graphics).DrawWith(
			SamplerState.PointClamp,
			scale,
			(batch, origin) => Draw(batch, origin, snapshot)
		);
	}
}
