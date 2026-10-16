using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenNspw.Desktop;

// The pictures of the original's sprite sheet (t3.bmp) that the battle screen's parts draw. Loaded for a battle screen,
// and disposed with it.
internal sealed class BattleArt : IDisposable
{
	// The menu's background, each side's flag (ButtonBase, set_sprt_data): Japan's, then the United States'.
	private static readonly Rectangle[] FlagRegions = [new(361, 3250, 198, 120), new(361, 3250 + 120, 198, 120)];

	private readonly Texture2D[] _flags;

	// The radar's map, a pixel a tile, which the radar draws as the map's tiles change.
	public Texture2D RadarMap { get; }

	public BattleArt(GraphicsDevice graphicsDevice, string dataDirectory)
	{
		_flags = SpriteSheet.LoadRegions(graphicsDevice, GameFiles.Resolve(dataDirectory, "t3.bmp"), FlagRegions);
		RadarMap = new Texture2D(graphicsDevice, 240, 180);
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
	}
}
