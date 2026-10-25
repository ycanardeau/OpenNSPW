using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenNspw.Desktop;

// The pictures of the game's sprite sheet (GameSession.SpriteSheetPath) that the battle screen's parts draw. Loaded for
// a battle screen, and disposed with it.
internal sealed class BattleArt : IDisposable
{
	// The menu's background, each side's flag (ButtonBase, set_sprt_data): Japan's, then the United States'.
	private static readonly Rectangle[] FlagRegions =
	[
		new(361, 3250, 198, 120),
		new(361, 3250 + 120, 198, 120),
	];

	// The sheet is 5756 high, taller than a texture may be: it is loaded in pieces that overlap by more than a sprite is
	// high, so that each sprite is whole in one of them.
	private static readonly Rectangle[] SheetPieces =
	[
		new(0, 0, 721, 2200),
		new(0, 2000, 721, 2200),
		new(0, 4000, 721, 1756),
	];

	private readonly Texture2D[] _flags;
	private readonly Texture2D[] _sheet;

	// The minimap's map, a pixel a tile, which the minimap draws as the map's tiles change.
	public Texture2D MinimapTexture { get; }

	public BattleArt(GraphicsDevice graphicsDevice, string sheetPath)
	{
		_flags = SpriteSheet.LoadRegions(graphicsDevice, sheetPath, FlagRegions);
		MinimapTexture = new Texture2D(graphicsDevice, 240, 180);
		_sheet = SpriteSheet.LoadRegions(graphicsDevice, sheetPath, SheetPieces, isKeyed: true);
	}

	// A sprite of the sheet, with its color key transparent: the piece that has it, and where it is in the piece.
	public (Texture2D Texture, Rectangle Source) Sheet(Rectangle source)
	{
		for (var i = 0; i < SheetPieces.Length; i++)
		{
			var piece = SheetPieces[i];
			if (source.Top >= piece.Top && source.Bottom <= piece.Bottom)
				return (
					_sheet[i],
					new Rectangle(source.X, source.Y - piece.Y, source.Width, source.Height)
				);
		}

		throw new ArgumentOutOfRangeException(
			nameof(source),
			source,
			"Not a region of the sprite sheet."
		);
	}

	// A frame of a sprite at a position, cut `inset` in from the frame's corner and `width` by `height`, as the original
	// cuts each kind (DrawMapTiles, DrawUnit, UpdateLowerEffects, DrawUnitDetails).
	public void Draw(
		SpriteBatch batch,
		in Sprite sprite,
		int frame,
		int inset,
		int width,
		int height,
		Vector2 position
	)
	{
		var source = new Rectangle(
			sprite.SheetPosition.X + sprite.Size.Width * (frame % sprite.FramesPerRow) + inset,
			sprite.SheetPosition.Y + sprite.Size.Height * (frame / sprite.FramesPerRow) + inset,
			width,
			height
		);
		var (texture, piece) = Sheet(source);
		batch.Draw(texture, position, piece, Color.White);
	}

	// The flag of the side, as the combat menu's background is chosen (UpdateUnitInfo).
	public Texture2D Flag(Side side) => _flags[side == Side.UnitedStates ? 1 : 0];

	public void Dispose()
	{
		foreach (var flag in _flags)
		{
			flag.Dispose();
		}

		MinimapTexture.Dispose();
		foreach (var piece in _sheet)
		{
			piece.Dispose();
		}
	}
}
