using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenNspw.Desktop;

// The pictures of the original's sprite sheet (t3.bmp) that the battle screen's parts draw. Loaded for a battle screen,
// and disposed with it.
internal sealed class BattleArt : IDisposable
{
	// The menu's background, each side's flag (ButtonBase, set_sprt_data): Japan's, then the United States'.
	private static readonly Rectangle[] FlagRegions = [new(361, 3250, 198, 120), new(361, 3250 + 120, 198, 120)];

	// The sheet is 5756 high, taller than a texture may be: it is loaded in pieces that overlap by more than a sprite is
	// high, so that each sprite is whole in one of them.
	private static readonly Rectangle[] SheetPieces = [new(0, 0, 721, 2200), new(0, 2000, 721, 2200), new(0, 4000, 721, 1756)];

	private readonly Texture2D[] _flags;
	private readonly Texture2D[] _sheet;

	// The radar's map, a pixel a tile, which the radar draws as the map's tiles change.
	public Texture2D RadarMap { get; }

	public BattleArt(GraphicsDevice graphicsDevice, string dataDirectory)
	{
		_flags = SpriteSheet.LoadRegions(graphicsDevice, GameFiles.Resolve(dataDirectory, "t3.bmp"), FlagRegions);
		RadarMap = new Texture2D(graphicsDevice, 240, 180);
		_sheet = SpriteSheet.LoadRegions(graphicsDevice, GameFiles.Resolve(dataDirectory, "t3.bmp"), SheetPieces, isKeyed: true);
	}

	// A sprite of the sheet, with its color key transparent: the piece that has it, and where it is in the piece.
	public (Texture2D Texture, Rectangle Source) Sheet(Rectangle source)
	{
		for (var i = 0; i < SheetPieces.Length; i++)
		{
			var piece = SheetPieces[i];
			if (source.Top >= piece.Top && source.Bottom <= piece.Bottom)
				return (_sheet[i], new Rectangle(source.X, source.Y - piece.Y, source.Width, source.Height));
		}

		throw new ArgumentOutOfRangeException(nameof(source), source, "Not a region of the sprite sheet.");
	}

	// The flag of the side, as the combat menu's background is chosen (UpdateUnitInfo).
	public Texture2D Flag(Side side) => _flags[side == Side.UnitedStates ? 1 : 0];

	public void Dispose()
	{
		foreach (var flag in _flags)
		{
			flag.Dispose();
		}

		RadarMap.Dispose();
		foreach (var piece in _sheet)
		{
			piece.Dispose();
		}
	}
}
