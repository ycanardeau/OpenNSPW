namespace OpenNspw;

// A sprite file (docs/SpriteSheets.md) by its path under the app's sprites, and the rectangle of the sprite sheet it was
// cut from.
public readonly record struct SpritePiece(string Path, int X, int Y, int Width, int Height);

// Where the sprite files are on the original's sprite sheet (set_sprt_data), for the game to draw its own frames from
// them (SpriteSheetSurface, docs/GameSprites.md): every part of the sheet that the game draws from. NSPW NET's t3.bmp, or
// 1.22's tst_cg1.bmp, which has its own land-based fighter of Japan. tools/CutSprites.cs cuts the files from the sheets
// by the same layout.
public static class SpriteSheetLayout
{
	// The facilities' row, and the land-based fighters', which is 1.22's own for Japan, by SpriteRow.
	private const int FacilityRow = 11;
	private const int LandBasedFighterRow = 14;

	// A unit's row of 8 directions, 80 by 80, by SpriteRow (cnct_game_cont.cpp), for Japan and the United States. Row 11
	// is the facilities' (Facilities), and a submerged submarine draws the row after its own.
	private static readonly (int Row, string Japan, string UnitedStates)[] UnitRows =
	[
		(0, "bb", "bb"),
		(1, "ca", "ca"),
		(2, "dd", "dd"),
		(3, "ss", "ss"),
		(4, "ss-submerged", "ss-submerged"),
		(5, "cvl", "cvl"),
		(6, "cv", "cv"),
		(7, "a6m", "f6f"),
		(8, "a6m-stopped", "f6f-stopped"),
		(9, "b5n", "tbf"),
		(10, "b5n-stopped", "tbf-stopped"),
		(12, "g8n", "b24"),
		(13, "tr", "tr"),
		(LandBasedFighterRow, "j1n", "p38"),
	];

	// The facilities of row 11, by the column their direction draws (set_unit_data). Columns 3 and 4 are not drawn.
	private static readonly (int Column, string Name)[] Facilities =
	[
		(0, "nb"),
		(1, "ab"),
		(2, "city"),
		(5, "ib"),
		(6, "pillboxes"),
		(7, "fortress"),
	];

	// The unit-info pictures, 120 by 438, 6 a row, by frame (UnitInfoPanel[2], DrawUnitDetails).
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

	// A side's sprites, in its folder: its units' rows, its facilities and its unit-info pictures.
	private static IEnumerable<SpritePiece> Side(
		string folder,
		bool isJapan,
		int unitsY,
		int picturesY,
		bool isNspw122
	)
	{
		foreach (var (row, japan, unitedStates) in UnitRows)
		{
			var name =
				!isJapan ? unitedStates
				: isNspw122 && row == LandBasedFighterRow ? "j7w"
				: japan;
			yield return new SpritePiece($"{folder}/{name}.png", 0, unitsY + row * 80, 640, 80);
		}

		foreach (var (column, name) in Facilities)
			yield return new SpritePiece(
				$"{folder}/{name}.png",
				column * 80,
				unitsY + FacilityRow * 80,
				80,
				80
			);

		for (var frame = 0; frame < Pictures.Length; frame++)
			yield return new SpritePiece(
				$"{folder}/info/{Pictures[frame]}.png",
				frame % 6 * 120,
				picturesY + frame / 6 * 438,
				120,
				438
			);
	}

	// The pieces of NSPW NET's sheet, or of 1.22's, which differ in Japan's land-based fighter only: 1.22's title is not
	// drawn, as a single-player game starts in its battle, and NSPW NET's three extra effects are drawn only by NSPW NET's
	// rules (BattleRules.MarksSecondTypes).
	public static IEnumerable<SpritePiece> Pieces(bool isNspw122)
	{
		yield return new SpritePiece("title.png", 0, 3940, 699, 384);
		yield return new SpritePiece("tiles.png", 0, 3000, 480, 240);
		yield return new SpritePiece("effects.png", 0, 3520, 480, 400);
		yield return new SpritePiece("flag-jp.png", 361, 3250, 198, 120);
		yield return new SpritePiece("flag-us.png", 361, 3370, 198, 120);
		yield return new SpritePiece("buttons.png", 0, 3250, 361, 270);
		yield return new SpritePiece("minimap.png", 0, 4340, 561, 200);

		foreach (var piece in Side("jp", true, 1759, 0, isNspw122))
			yield return piece;

		foreach (var piece in Side("us", false, 4550, 878, isNspw122))
			yield return piece;
	}
}
