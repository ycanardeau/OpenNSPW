# Sprite sheets
A proposal, for review: splitting the original's one sprite sheet into a file per unit and per part of the screen.

## Summary
- **NSPW NET's `t3.bmp` and 1.22's `tst_cg1.bmp` share their layout** (721 by 5756) and 93.6% of their pixels. They differ in three places only: the title, Japan's land-based fighter, and three effect icons that NSPW NET adds.
- **Split by what the code draws**: a file per unit kind and side, eight directions in a row; a file per unit-info picture; one file each for the tiles, the effects, the buttons, the flags and the minimap.
- **Name the ships by kind and the planes by model** (`bb`, `ca`, `a6m`, `j7w`), and the facilities by kind (`nb`, `ab`).
- **1.22's differences become files of their own**, not overrides: `title-1.22.png`, and `j7w.png` beside NSPW NET's land-based fighter. Everything else is shared.

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
Identified from the sprites alone; the manuals name only the kinds. The confidence is mine, and the names are for you to confirm.

| Side | Role | Model | Confidence | Why |
| --- | --- | --- | --- | --- |
| Japan | Carrier fighter | A6M Zero | high | light grey, single engine |
| Japan | Attacker | B5N (or B6N Tenzan) | medium | dark green, single engine, a long three-seat canopy |
| Japan | Strategic bomber | G8N Renzan | medium | four engines, a single fin, a glazed nose |
| Japan | Land-based fighter, NSPW NET | J1N Gekko (or Ki-45) | low | twin-engine, single fin, mottled green |
| Japan | Land-based fighter, 1.22 | J7W Shinden | high | canard, pusher, swept rear wing |
| United States | Carrier fighter | F6F Hellcat (or F4U Corsair) | low | dark blue, single engine; the gull wing does not show from above |
| United States | Attacker | TBF Avenger (or SBD Dauntless) | low | khaki, single engine, a long canopy |
| United States | Strategic bomber | B-24 Liberator | medium | four engines, twin fins |
| United States | Land-based fighter | P-38 Lightning | high | twin booms |

## Proposed files
PNG with transparency instead of the magenta key. A unit's file is a row of 8 frames of 80×80 (640×80), in the sheet's column order, so that `SpriteColumn` indexes it as before.

```
Data/Sprites/
  title.png                    NSPW NET's, 699×384
  title-1.22.png               1.22's, 599×387
  tiles.png                    6×3 frames of 80×80, as on the sheet
  effects.png                  12×10 frames of 40×40, NSPW NET's (a superset of 1.22's)
  buttons.png                  2 columns of 11 buttons of 140×20
  flag-jp.png, flag-us.png     198×120
  minimap.png                  the frame, and the three dots
  jp/
    bb.png ca.png dd.png ss.png ss-submerged.png cvl.png cv.png tr.png
    a6m.png a6m-stopped.png
    b5n.png b5n-stopped.png
    g8n.png
    j1n.png                    NSPW NET's land-based fighter
    j7w.png                    1.22's land-based fighter
    nb.png ab.png city.png ib.png pillboxes.png fortress.png     a frame each, 80×80
    info/
      bb.png ca.png dd.png ss.png cv.png cvl.png tr.png empty.png    120×438
      ab-runway.png ab-apron.png cv-hangar.png cvl-hangar.png
  us/
    (the same, with f6f, tbf, b24, p38)
```

The facilities' unused columns 3 and 4 are left out until something is found to draw them.

## What changes in the code
- `Sprite.SheetPosition` and `FramesPerRow` give way to a texture per unit kind, state and side; `Renderer` keeps choosing the frame (`SpriteRow`, `SpriteColumn`), and the desktop UI maps the row to its file.
- `Singleplayer.SpriteSheetName` gives way to the version's choices: the title and Japan's land-based fighter (`j7w` in a single-player game, `j1n` otherwise).
- `BattleArt`'s loading of the sheet in three overlapping pieces goes away, with the 8-bit decoding in `SpriteSheet`.
- The files come from a tool that cuts them out of `Original/NSPW_NET/t3.bmp` and `Original/Nspw122/tst_cg1.bmp`, so that they can be made again and checked against the originals.

## Open questions
- **Commit the PNGs, or cut them at build time?** Committed, they can be edited and replaced; cut at build, the originals stay the only source and nothing can drift. The answer depends on whether the split is meant for editing the art.
- **The plane names** of medium and low confidence above.
- **`effects.png`**: the code numbers effects by frame (`effect.SpriteNumber`, such as `96 + direction`), so it stays one grid. Its directional rows (landing craft, torpedo, bomb, anti-aircraft shell, gun shell) could become files of their own later, with a mapping from the frame numbers.
- **The sounds**, which differ the same way: 1.22 has its own `AA_SHL4.wav` and `GUN3.wav`, and lacks `AA_SHL5.wav`, `SNR.wav` and `click2.wav`. The port plays NSPW NET's in a single-player game too.
