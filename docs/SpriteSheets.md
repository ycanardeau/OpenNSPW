# Sprite sheets
How the desktop app draws the original's sprites: cut from its one sprite sheet into a file per unit and per part of the screen, in [OpenNspw.Desktop/Sprites](../OpenNSPW/OpenNspw.Desktop/Sprites).

## Summary
- **NSPW NET's `t3.bmp` and 1.22's `tst_cg1.bmp` share their layout** (721 by 5756) and 93.6% of their pixels. They differ in three places only: the title, Japan's land-based fighter, and three effect icons that NSPW NET adds.
- **Split by what the UI draws**: a file per unit kind and side, eight directions in a row; a file per facility and per unit-info picture; one file each for the title, the tiles, the effects and each flag.
- **The ships are named by kind and the planes by model** (`bb`, `ca`, `a6m`, `j7w`), and the facilities by kind (`nb`, `ab`).
- **1.22's land-based fighter is a file of its own**, `j7w.png` beside NSPW NET's `j1n.png`, which a single-player game draws instead. Everything else is NSPW NET's, and draws as 1.22's sheet would: its three extra icons are drawn only by NSPW NET's rules (`BattleRules.MarksSecondTypes`).
- **The files are made by [tools/CutSprites.cs](../tools/CutSprites.cs)** from the two sheets in `Original`, and embedded in the app.
- **The app ships no sheet**: the game still draws from a sheet on its own surface (`LoadBitmapSurface`), and `SpriteSheet` puts each sheet together again from the files for it. Every part that the game draws from is as on the sheet; the rest, the author's notes around the cells, is white.

## What differs between the two sheets
| Region | Rows of the sheet | NSPW NET | 1.22 |
| --- | --- | --- | --- |
| Title | 3940–4326 | 699 by 384 | 599 by 387, another picture |
| Japan's land-based fighter (row 14 of `JapanUnits`) | 2879–2958 | twin-engine | J7W Shinden |
| Effects, frames 16, 26, 27 | 3560–3639 | a falling bomb, headphones, a star | blank |

The rest is pixel-identical: both sides' ships, planes and facilities, the unit-info pictures, the tiles, the buttons, the flags and the minimap. NSPW NET's sheet is 24-bit and 1.22's is 8-bit with a palette; both decode to the same colors.

## The original layout
From `set_sprt_data` (`Renderer.InitializeSprites`).

| SpriteId | Sheet position | Frame | Frames a row | Contents |
| --- | --- | --- | --- | --- |
| `JapanUnitInfo` | 0, 0 | 120×438 | 6 | 12 pictures for the unit-info panel |
| `UnitedStatesUnitInfo` | 0, 878 | 120×438 | 6 | the same for the United States |
| `JapanUnits` | 0, 1759 | 80×80 | 8 | 15 rows, a row a unit kind and state, a column a direction |
| `MapTiles` | 0, 3000 | 80×80 | 6 | the sea, the coasts, the clouds |
| `Buttons1` | 0, 3250 | 140×20 | 1 | the combat menu's buttons, two columns |
| `ButtonBase` | 361, 3250 | 198×120 | 1 | the flags behind the menu: Japan's, then the United States' |
| `SubUnits` | 0, 3520 | 40×40 | 12 | 10 rows of effects and projectiles |
| `TitleBackground` | 0, 3940 | 699×384 or 599×387 | – | the title |
| `Minimap` | 0, 4340 | 255×199 | 1 | the minimap's frame, and the dots it draws units with (267, 4356 and 4397) |
| `UnitedStatesUnits` | 0, 4550 | 80×80 | 8 | the same 15 rows as Japan's |

### A unit's row
`unit.SpriteRow` (set in `cnct_game_cont.cs`); a submerged submarine draws the row after its own.

| Row | Unit | State |
| --- | --- | --- |
| 0 | Battleship | |
| 1 | Cruiser | |
| 2 | Destroyer | |
| 3 | Submarine | surfaced |
| 4 | Submarine | submerged |
| 5 | Light carrier | |
| 6 | Carrier | |
| 7 | Carrier fighter | flying |
| 8 | Carrier fighter | stopped (`IsStopping`: on a deck or runway) |
| 9 | Attacker | flying |
| 10 | Attacker | stopped |
| 11 | Facilities, by column (below) | |
| 12 | Strategic bomber | |
| 13 | Transport | |
| 14 | Land-based fighter | |

A column is a direction (`SpriteColumnOf`): 0 is north (90°), then clockwise, 45° apart, to 7 (135°).

The facilities set a fixed direction to choose their column of row 11:

| Column | Facility | Direction |
| --- | --- | --- |
| 0 | Naval base | 90° |
| 1 | Air base | 45° |
| 2 | City | 0° |
| 3 | unused? (a piece of land) | |
| 4 | unused? (a small blue plane) | |
| 5 | Infantry base | 225° |
| 6 | Pillboxes | 180° |
| 7 | Fortress | 135° |

### A unit-info picture
`UnitInfoPanel[2]` (`etc2.cs`), and `DrawUnitDetails` for the hangars.

| Frame | Picture |
| --- | --- |
| 0–3 | Battleship, cruiser, destroyer, submarine |
| 4, 5 | Carrier, light carrier: the flight deck |
| 6 | Transport |
| 7 | Empty, the side's background (any other kind) |
| 8 | Air base: the runway |
| 9 | Air base: the apron (`UnitInfoPanel[1]` set) |
| 10, 11 | Carrier, light carrier: the hangar |

## The planes
Identified from the sprites: the game, its manuals, its source and the author's site name only the kinds. The B-24 is confirmed by the author's own NSPW manga, in which the bomber introduces itself as the B-24 that players take for a B-29 ([奈良鹿製作所, まんが](http://narashikabranch.web.fc2.com/manga.htm)). The other names of medium and low confidence are still to be confirmed.

The attacker is both the torpedo bomber and the dive bomber: the author merged them on purpose, to keep the game easy for beginners ([his post on X of 17 October 2017](https://x.com/tokumizman/status/920107084514856960), archived in the [Wayback Machine's capture of its thread](https://web.archive.org/web/20220209140925/https://twitter.com/tokumizman/status/909772884897292290)). It is named after the torpedo bomber that it looks like.

| Side | Role | Model | Confidence | Why |
| --- | --- | --- | --- | --- |
| Japan | Carrier fighter | A6M Zero | high | light grey, single engine |
| Japan | Attacker | B5N (or B6N Tenzan) | medium | dark green, single engine, a long three-seat canopy |
| Japan | Strategic bomber | G8N Renzan | medium | four engines, a single fin, a glazed nose |
| Japan | Land-based fighter, NSPW NET | J1N Gekko (or Ki-45) | low | twin-engine, single fin, mottled green |
| Japan | Land-based fighter, 1.22 | J7W Shinden | high | canard, pusher, swept rear wing |
| United States | Carrier fighter | F6F Hellcat (or F4U Corsair) | low | dark blue, single engine; the gull wing does not show from above |
| United States | Attacker | TBF Avenger (or SBD Dauntless) | low | khaki, single engine, a long canopy |
| United States | Strategic bomber | B-24 Liberator | confirmed | the author's manga; four engines, twin fins |
| United States | Land-based fighter | P-38 Lightning | high | twin booms |

## The files
PNG with transparency where the sheet has its color key. A unit's file is a row of 8 frames of 80×80 (640×80), in the sheet's column order, so that `SpriteColumn` indexes it as before. Each frame keeps the sheet's cell whole, its border too: the UI cuts it as the original does.

```
OpenNspw.Desktop/Sprites/
  title.png                    NSPW NET's, 699×384
  tiles.png                    6×3 frames of 80×80, as on the sheet
  effects.png                  12×10 frames of 40×40, NSPW NET's
  flag-jp.png, flag-us.png     198×120
  title-122.png                1.22's, 599×387, for the game's own sheet
  buttons.png                  the combat menu's buttons, both columns, 361×270, for the game's own sheet
  minimap.png                  the minimap's frame, its dots and the background it copies from, 561×200, for the game's own sheet
  jp/
    bb.png ca.png dd.png ss.png ss-submerged.png cvl.png cv.png tr.png
    a6m.png a6m-stopped.png
    b5n.png b5n-stopped.png
    g8n.png
    j1n.png                    NSPW NET's land-based fighter
    j7w.png                    1.22's land-based fighter
    nb.png ab.png city.png ib.png pillboxes.png fortress.png     a frame each, 80×80
    unused-3.png unused-4.png    columns 3 and 4 of the facilities' row, for the game's own sheet
    info/
      bb.png ca.png dd.png ss.png cv.png cvl.png tr.png empty.png    120×438
      ab-runway.png ab-apron.png cv-hangar.png cvl-hangar.png
  us/
    (the same, with f6f, tbf, b24 and p38)
```

For the game's own sheet only, as the UI does not draw them: 1.22's title, the combat menu's buttons (the UI draws its own), the minimap's band, and the facilities' row's columns 3 and 4.

To make the files again, and to check that they still match the sheets:

```bash
dotnet run tools/CutSprites.cs
dotnet run tools/CutSprites.cs --check
```

## The code
- `SpriteFiles` loads a file from the app's resources.
- `BattleArt` maps a frame of the original's sprites (`SpriteId` and frame number, with `Sprite.Size` and `FramesPerRow` from `set_sprt_data`) to its file and where it is in the file: a unit's row to the row's file, a facility's column to its file, a unit-info picture to its file, and a tile or an effect to its place in the grid. Its callers draw as before.
- `MainMenuScreen` draws `title.png`.
- `SpriteSheet` puts `t3.bmp` and `tst_cg1.bmp` together from the files, as 24-bit BMPs whose transparent pixels are the color key, for the game (`WithSpriteSheet`). `SpriteSheetTests` checks that they are the original's sheets, as the game's 16-bit surface holds them, wherever the game draws from: everywhere but 1.22's three blank effects, which the app's sheet has as NSPW NET's and which only NSPW NET's rules draw.

Every frame that `BattleArt` draws was compared with the same frame cut from the sheet as before: all of them match NSPW NET's sheet, and 1.22's but for the three icons that only NSPW NET's rules draw.

## Open questions
- **The plane names** of medium and low confidence above.
- **`effects.png`**: the code numbers effects by frame (`effect.SpriteNumber`, such as `96 + direction`), so it stays one grid. Its directional rows (landing craft, torpedo, bomb, anti-aircraft shell, gun shell) could become files of their own later, with a mapping from the frame numbers.
- **The sounds**, which differ the same way: 1.22 has its own `AA_SHL4.wav` and `GUN3.wav`, and lacks `AA_SHL5.wav`, `SNR.wav` and `click2.wav`. The port plays NSPW NET's in a single-player game too.
