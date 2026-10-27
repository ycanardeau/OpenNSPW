using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenNspw.Desktop;

// The pictures that the battle screen's parts draw, from the sprites cut from the original's sprite sheet
// (SpriteFiles): NSPW NET's, with 1.22's land-based fighter of Japan in a single-player game. Loaded for a battle
// screen, and disposed with it.
internal sealed class BattleArt : IDisposable
{
	// The row of the units' sprites (SpriteRow) that the facilities share, a column each.
	private const int FacilityRow = 11;

	// The row of the land-based fighters, which is 1.22's own for Japan.
	private const int LandBasedFighterRow = 14;

	// The files of a side's units' rows, by SpriteRow: 8 directions each, 80 by 80. The facilities' row has a file a
	// column.
	private static readonly string?[] JapanUnitRows =
	[
		"bb",
		"ca",
		"dd",
		"ss",
		"ss-submerged",
		"cvl",
		"cv",
		"a6m",
		"a6m-stopped",
		"b5n",
		"b5n-stopped",
		null,
		"g8n",
		"tr",
		"j1n",
	];

	private static readonly string?[] UnitedStatesUnitRows =
	[
		"bb",
		"ca",
		"dd",
		"ss",
		"ss-submerged",
		"cvl",
		"cv",
		"f6f",
		"f6f-stopped",
		"tbf",
		"tbf-stopped",
		null,
		"b24",
		"tr",
		"p38",
	];

	// The facilities' files, by the column of the facilities' row that their direction draws (set_unit_data).
	// Columns 3 and 4 are not drawn.
	private static readonly string?[] FacilityColumns =
	[
		"nb",
		"ab",
		"city",
		null,
		null,
		"ib",
		"pillboxes",
		"fortress",
	];

	// The unit-info pictures' files, by frame (UnitInfoPanel[2], DrawUnitDetails).
	private static readonly string[] Pictures =
	[
		"bb",
		"ca",
		"dd",
		"ss",
		"cv",
		"cvl",
		"tr",
		"empty",
		"ab-runway",
		"ab-apron",
		"cv-hangar",
		"cvl-hangar",
	];

	// A side's pictures, from its folder: its units' rows, its facilities, its unit-info pictures, and its flag, the
	// combat menu's background (ButtonBase).
	private sealed class SideArt : IDisposable
	{
		public Texture2D?[] UnitRows { get; }

		public Texture2D?[] Facilities { get; }

		public Texture2D[] Pictures { get; }

		public Texture2D Flag { get; }

		public SideArt(GraphicsDevice graphicsDevice, string folder, string?[] unitRows)
		{
			Texture2D? LoadOrNone(string? name) =>
				name is null ? null : SpriteFiles.Load(graphicsDevice, $"{folder}/{name}.png");

			UnitRows = unitRows.Select(LoadOrNone).ToArray();
			Facilities = FacilityColumns.Select(LoadOrNone).ToArray();
			Pictures = BattleArt
				.Pictures.Select(name =>
					SpriteFiles.Load(graphicsDevice, $"{folder}/info/{name}.png")
				)
				.ToArray();
			Flag = SpriteFiles.Load(graphicsDevice, $"flag-{folder}.png");
		}

		public void Dispose()
		{
			foreach (var texture in UnitRows.Concat(Facilities).Concat(Pictures))
				texture?.Dispose();

			Flag.Dispose();
		}
	}

	private readonly SideArt _japan;
	private readonly SideArt _unitedStates;
	private readonly Texture2D _tiles;
	private readonly Texture2D _effects;

	// The minimap's map, a pixel a tile, which the minimap draws as the map's tiles change.
	public Texture2D MinimapTexture { get; }

	// isSingleplayer: whether the game is a single-player game, which draws 1.22's land-based fighter of Japan.
	public BattleArt(GraphicsDevice graphicsDevice, bool isSingleplayer)
	{
		var japanUnitRows = (string?[])JapanUnitRows.Clone();
		if (isSingleplayer)
			japanUnitRows[LandBasedFighterRow] = "j7w";

		_japan = new SideArt(graphicsDevice, "jp", japanUnitRows);
		_unitedStates = new SideArt(graphicsDevice, "us", UnitedStatesUnitRows);
		_tiles = SpriteFiles.Load(graphicsDevice, "tiles.png");
		_effects = SpriteFiles.Load(graphicsDevice, "effects.png");
		MinimapTexture = new Texture2D(graphicsDevice, 240, 180);
	}

	// A frame of a sprite whose frames are a grid in one file, as the sheet has them: where it is in the file.
	private static Point GridCorner(in Sprite sprite, int frame)
	{
		return new Point(
			sprite.Size.Width * (frame % sprite.FramesPerRow),
			sprite.Size.Height * (frame / sprite.FramesPerRow)
		);
	}

	// A frame of a side's units: the file of its row, or of its column in the facilities' row, and where it is in the
	// file.
	private static (Texture2D? Texture, Point Corner) UnitFrame(
		SideArt side,
		in Sprite sprite,
		int frame
	)
	{
		var (row, column) = (frame / sprite.FramesPerRow, frame % sprite.FramesPerRow);
		return row == FacilityRow
			? (side.Facilities[column], Point.Zero)
			: (side.UnitRows[row], new Point(sprite.Size.Width * column, 0));
	}

	// A frame of a sprite at a position, cut `inset` in from the frame's corner and `width` by `height`, as the original
	// cuts each kind (DrawMapTiles, DrawUnit, UpdateLowerEffects, DrawUnitDetails). A frame with no file draws
	// nothing.
	public void Draw(
		SpriteBatch batch,
		SpriteId id,
		in Sprite sprite,
		int frame,
		int inset,
		int width,
		int height,
		Vector2 position
	)
	{
		var (texture, corner) = id switch
		{
			SpriteId.JapanUnits => UnitFrame(_japan, sprite, frame),
			SpriteId.UnitedStatesUnits => UnitFrame(_unitedStates, sprite, frame),
			SpriteId.JapanUnitInfo => (_japan.Pictures[frame], Point.Zero),
			SpriteId.UnitedStatesUnitInfo => (_unitedStates.Pictures[frame], Point.Zero),
			SpriteId.MapTiles => (_tiles, GridCorner(sprite, frame)),
			SpriteId.SubUnits => (_effects, GridCorner(sprite, frame)),
			_ => throw new ArgumentOutOfRangeException(
				nameof(id),
				id,
				"Not drawn by the battle screen."
			),
		};
		if (texture is null)
			return;

		batch.Draw(
			texture,
			position,
			new Rectangle(corner.X + inset, corner.Y + inset, width, height),
			Color.White
		);
	}

	// The flag of the side, as the combat menu's background is chosen (UpdateUnitInfo).
	public Texture2D Flag(Side side) => (side == Side.UnitedStates ? _unitedStates : _japan).Flag;

	public void Dispose()
	{
		_japan.Dispose();
		_unitedStates.Dispose();
		_tiles.Dispose();
		_effects.Dispose();
		MinimapTexture.Dispose();
	}
}
