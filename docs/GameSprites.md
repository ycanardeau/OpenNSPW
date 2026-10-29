# The game's own sprites
How the game draws its own frames from the sprite files ([SpriteSheets.md](SpriteSheets.md)) instead of a sprite sheet, so that a single-player game reads no `tst_cg1.bmp`, and a network game no `t3.bmp`.

## Summary
- **The game still draws every frame** ([UI.md](UI.md#the-games-own-drawing)), from one surface, `OffscreenSurface`, in sheet coordinates. Its 37 blits stay as they are.
- **That surface becomes a set of pieces** (`SpriteSheetSurface`): each sprite file is a surface of its own, at the place on the sheet it was cut from. A blit from or to the sheet finds the piece that holds its rectangle, and blits from or to that piece. No sheet is put together.
- **The layout is the game's, the pixels the platform's**: the core lists which file goes where (`SpriteSheetLayout`, from `set_sprt_data`), for NSPW NET and for 1.22; the platform decodes the files (`INspwPlatform.LoadSprite`). The desktop app decodes its embedded PNGs. A platform without them, such as the tests', reads the sheet file, as now.
- **1.22's own pieces in a single-player game**: `j7w.png`, 1.22's land-based fighter, in row 14 of Japan's units. Everything else is NSPW NET's, as the UI already draws it.
- **Two files are for the game only**, the parts that only it draws: `buttons.png` and `minimap.png`.
- **Tested against the sheets**: each piece holds what the sheet holds there, every blit of a played game falls in one piece, and the frames are the same.

## Why the sheet is not put together
Putting together a 721 by 5756 picture from the files, to give `LoadBitmapSurface` a BMP, decodes the PNGs, writes a 12 MB BMP, decodes it again, and fills a surface of 4.15 million pixels, of which about an eighth is the author's notes around the cells, never drawn. The pieces hold only what is drawn: 3.4 million pixels of the files the UI has, and 0.2 million for the game's two. The sheet's coordinates stay the game's, as the original's code has them, and nothing is written twice.

## What the game reads from the sheet
From `set_sprt_data` (`Renderer.InitializeSprites`) and the blits from `OffscreenSurface`.

| Region | Sheet position | Piece | Note |
| --- | --- | --- | --- |
| `JapanUnitInfo` | 0, 0 | `jp/info/*.png`, one a frame | 120×438, 6 a row |
| `UnitedStatesUnitInfo` | 0, 878 | `us/info/*.png` | |
| `JapanUnits` | 0, 1759 | `jp/<row>.png`, one a row | 640×80; 1.22's row 14 is `j7w.png` |
| Row 11, the facilities | 0, 2639 | `jp/nb.png` ... `jp/fortress.png`, one a column | columns 3 and 4 are not drawn: no piece |
| `MapTiles` | 0, 3000 | `tiles.png` | 6×3 of 80×80 |
| `Buttons1` | 0, 3250 | `buttons.png` | both columns, 361×270 |
| `ButtonBase` | 361, 3250 | `flag-jp.png`, `flag-us.png` | 198×120 each |
| `SubUnits` | 0, 3520 | `effects.png` | 12×10 of 40×40 |
| `TitleBackground` | 0, 3940 | `title.png` | NSPW NET's only (below) |
| `Minimap` | 0, 4340 | `minimap.png` | the frame, its dots and the band it copies from, 561×200; also written (below) |
| `UnitedStatesUnits` | 0, 4550 | `us/<row>.png` | |

**The minimap's piece is written to**: `make_map_cg` (`MakeTerrainSurface`) copies its band (x + 306) over the frame, draws the map's dots on it from the dots at x + 270, and the minimap is then blitted from there to the screen. It is the only part of the sheet the game writes, and both rectangles of its copies are in the one piece.

**The title**: a single-player game starts in its battle (`StartSingleplayerGame`) and never draws the title, so 1.22's title, `title-122.png`, is not needed. Its region is NSPW NET's `title.png` in both layouts.

**Effects 16, 26 and 27**, blank on 1.22's sheet, are NSPW NET's in `effects.png`. Only NSPW NET's rules draw them (`BattleRules.MarksSecondTypes`), so a single-player game draws the same as from 1.22's sheet.

## The surface
`SpriteSheetSurface`, in `ddraw.cs`, an `IDirectDrawSurface7` the size of the sheet, made of pieces: a `DirectDrawSurface7` each, with its place on the sheet.

- **As a source**: `DirectDrawSurface7.Blt` and `BltFast` find the piece that holds the source rectangle (`TryFind`), move the rectangle into the piece's coordinates, and copy from it as from any surface.
- **As a destination**: its own `Blt` and `BltFast` find the piece that holds the destination rectangle, and forward the blit to it. A source that is the sheet itself is found the same way: the minimap's copies are within its piece.
- **`SetColorKey`, `Restore` and `Release`** go to every piece, and `GetPixelFormat` is theirs. **`Lock`, `Unlock`, `GetDC`, `ReleaseDC`, `Flip` and `SetClipper`** return `DDERR_UNSUPPORTED`: the game calls none of them on the sheet.
- **A rectangle in no piece, or across two**, returns `DDERR_INVALIDRECT`, as a rectangle beyond a surface does, draws nothing, and is kept in `Misses`. The tests below check that a played game has none.
- **The pieces are decoded when the surface is made** (`Renderer.LoadSpriteSheet`), where the game loaded the sheet, at its start and again in `RestoreSurfaces`. They are about 70 small PNGs, 456 KB in all. Decoding each on its first blit would only skip the title in a single-player game, and would make the battle's first frames wait for files.

## Where the files come from
- **`INspwPlatform.LoadSprite(string path)`**: the file of `Sprites` at the path, such as `jp/bb.png`, as its width, height and RGBA pixels, or null. The default returns null.
- **The core converts the pixels** to the surface's 16 bits (`wingdi.ToPixel`), a transparent pixel to the color key that the game sets (`dwRBitMask | dwBBitMask`, magenta), as the sheet's magenta is.
- **All or nothing**: `LoadSpriteSheet` makes the pieces if the platform has every file of the layout, each of its size, and otherwise loads the sheet file with the original's `LoadBitmapSurface`, so that a surface never mixes the two. `SpriteSheetName` stays, as the name of that file.
- **The desktop app** decodes its embedded PNGs (`SpriteFiles.LoadBitmap`) with StbImageSharp, which MonoGame already brings, on the game's thread: no graphics device is needed.
- **The files stay in the desktop app**, not in the core: the release packs every packable project to NuGet, and the original's art should not go there. The app is not packed.
- **`GameFiles.OpenSingleplayer`** no longer serves the sheet in the app; it still serves 1.22's maps and sounds. The tests' platforms draw from the sheets, unless a test asks for the files (`DrawsFromSpriteFiles`).

## Tests
`GameSpritesTests`, which need `Original`, as the other tests of whole games do (`GameData`):

- **Each piece is the sheet's**: every piece of both layouts holds the pixels of the sheet (`t3.bmp`, `tst_cg1.bmp`) as the game's surface holds them, color key included; for 1.22's, but the title and the three effects above.
- **The same frames**: two single-player scenarios (202 and 303) show the same frames, texts and sounds from the files as from 1.22's sheet, 500 frames each; and a network game's committed traces, which hash every frame, are unchanged from the files: the title screen (`title`) and a battle of the setting screen's first scenario (`scenario-1`).
- **Every blit falls in one piece**: the games of those tests have no `Misses`.

The four runs of whole games take about 80 seconds together.

## Open questions
- **`title-122.png`**: left out, as nothing draws it. It would be needed if a single-player game had a title screen.
