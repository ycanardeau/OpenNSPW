# Single-player
How OpenNSPW plays the single-player game of NSPW 1.22 (`Original/Nspw122/NSPW_122.exe`) against the computer.

The NSPW NET source has no single-player code: the computer's strategy, the single-player scenarios and their victory conditions exist only as machine code in the single-player executable. OpenNSPW runs that machine code on [Enzan](../Enzan), translated to C# by the [Disassembler](../Disassembler), on the port's own state. Everything else (the battle, drawing, input) is the port of NSPW NET.

## Summary
- **Emulate only what NSPW NET lacks**: the scenario setups, the computer's strategy, its route planning, the victory conditions, and the 1.22 versions of the functions they call to create units. The battle itself stays the port's.
- **The emulator's memory is the executable's own image**, loaded from `NSPW_122.exe`. Code, jump tables, constants and the single-player globals are where the code expects them.
- **The state is copied in and out** around every emulated call, region by region, as planned in [Refactoring.md](Refactoring.md#the-emulator). The single-player globals that the port does not have stay in the emulator's memory for the whole game.
- **The port runs with 1.22's unit numbering** in a single-player game. The unit ranges are values of the game instead of constants, with NSPW NET's values in a network game.
- **The single-player paths come back where NSPW NET left them as comments**, such as `/*|| unit[m].used==cpu_side*/`, behind `IsSingleplayer` or `CpuSide`. A network game runs exactly as before, and its traces do not change.

## The executable
| Section | Address | Contents |
| --- | --- | --- |
| `.text` | `0x401000` | The code, and the jump tables of its `switch` statements. |
| `.rdata` | `0x43C000` | The import table, then constants (`double`s used by the FPU code). |
| `.data` | `0x43E000` | The globals, initialized and then zeroed (`unit` at `0x4916A0`). |

The emulator's memory (`OpenNspw.Singleplayer/Emulator.cs`) is the image from `0x400000` to `0x4F1000`, a stack of 64 KB below it, and 4 KB after it for what the port places there, such as the object behind a COM pointer. Each slot of the import table holds a stub, an address from `0xFFFF0000` that is not in the memory; calling it calls the native function set for that import. An import that has none throws, with its name.

What the emulated functions do is in [ComputerPlayer.md](ComputerPlayer.md); where the single-player game differs from 1.22, in [Differences.md](Differences.md); and what is known of the emulator's discrepancies with the machine code, in [Conversion.md](Conversion.md).

## What is emulated
The Disassembler translates each function to a static method `Sub<address>.Call(Emulator)`, in `OpenNspw.Singleplayer/Subroutines`, and writes the table that finds them by address. `Emulator.Call` dispatches a call to a native function if one is set at its address, and otherwise to the translated function there.

```bash
dotnet run --project Disassembler/OpenNspw.Disassembler -- Original/Nspw122/NSPW_122.exe OpenNSPW/OpenNspw.Singleplayer/Subroutines
```

| Function | Address | Called by the port |
| --- | --- | --- |
| `scenario_1` to `scenario_10`, `scenario_100` to `scenario_109`, `scenario_200` to `scenario_209`, `scenario_300` to `scenario_306` | `Functions.SetUpScenario` | `InitializeGame`, in place of `get_sinario_data` |
| `cpu_strtgy`, and the functions only it calls (`sub_403A70`, `sub_403AE0`, `sub_404D80`, `sub_414DD0`, `sub_41F230`, `sub_41F300`, `sub_41F3C0`, `sub_41F490`, `sub_417C10`) | `0x404E30` | `HandleInput`, before the orders are applied |
| `set_cpu_route` | `0x42DA80` | `ApplyOrders`, for a move order of a computer's ship |
| `decision` | `0x41C3C0` | `CheckResult`, in place of NSPW NET's conditions and result |
| `set_new_unit`, `set_new_unit_plane`, `set_unit_data`, `plane_in_cv`, `set_pos_of_parking`, `cls_all_slct_unit_p2`, `pt_in_rect3` | | Only by the emulated code |

`set_cpu_route2` (`0x42ED30`) is translated, but nothing calls it: the port's `SetCpuRoute2`, which NSPW NET calls for every unit, stays.

The scenario numbers and their functions are those of 1.22's `game_init` (`0x429C50`), which dispatches to them through two jump tables.

Native, because they are part of the port's own state or platform:

| Function | Native |
| --- | --- |
| `rand` (`0x436056`) | The port's `rand()`, so that the whole game draws from one sequence, as in the original |
| `__ftol` (`0x4360C4`) | Truncation of `ST(0)` to `EDX:EAX` |
| `load_it2` (`0x432A30`) | The port's `LoadScenarioFile2`, by NSPW NET's name of the same file: 1.22 also spells some in lower case (`Map\south_pacific.dat`), and `LoadScenarioFile2` recognizes the map by its name |
| `IDirectDrawSurface7::GetDC` and `ReleaseDC` of `lpDDSBack` (`0x463AAC`), `SetTextColor`, `SetBkMode`, `SelectObject`, `TextOutA`, `wsprintfA` | The port's stand-ins, on its back buffer, so that the messages of `decision` show. `0x491600`, the font that `decision` selects, is the port's `gameFont_1` |
| `SetWindowTextA` | `set_cpu_route`'s debug output: dropped |

## The state
### Layout
1.22's structs are NSPW NET's, except `UNIT`. Its offsets were read from `set_unit_data` (`gas[0]=100`, `gas[1]=9000`, `tech=5`) and `game_init`, which clears 256 units of `0x598` bytes:

| | NSPW NET | 1.22 |
| --- | --- | --- |
| `UNIT` size | `0x5C0` | `0x598` |
| `used` to `arm[8]` | `0x000` to `0x538` | The same offsets |
| `arm2` | `int[2]` at `0x538` | None |
| `gas` | `double[8]` at `0x540` | `int[8]` at `0x538` |
| `found`, `tech` | `0x580`, `0x584` | `0x558`, `0x55C` |
| `rnd_250` to `rnd_10` | `0x588` to `0x5C0` | `0x560` to `0x598` |

The globals that both have, at 1.22's addresses (`SharedGlobals` in `OpenNspw/Singleplayer.cs`):

| Global | Address | Copied back | Global | Address | Copied back |
| --- | --- | --- | --- | --- | --- |
| `rest_time` | `0x443A48` | Yes | `cmbt_x`, `cmbt_y` | `0x489AB0` | Yes |
| `max_unit` | `0x443A90` | Yes | `FrameCount` | `0x489AD0` | Yes |
| `cmbt_map` | `0x443AA0` | No | `slct_unit` | `0x489AE0` | Yes |
| `new_pp[2]` | `0x463AC0` | Yes | `cc_count` | `0x489EE4` | Yes |
| `game_end` | `0x483B44` | Yes | `your_side` | `0x489FB0` | Yes |
| `cpu_side` | `0x483B4C` | Yes | `decision_point` | `0x489FC0` | Yes |
| `unit` | `0x4916A0` | Yes | `sinario` | `0x48A1E8` | Yes |
| `sprt` | `0x48A200` | No | | | |

`cpu_side` is new to the port (`CpuSide`): the side the computer plays, or none, as in the first two scenarios.

### Copying
Before an emulated call, each region is copied from the port into the emulator's memory; after it, the ones that the emulated code writes are copied back. A unit is three blocks (`0x000` to `0x538`, `found` and `tech`, `rnd_250` to `rnd_10`), plus `gas`, which is converted: in, truncated to `int`; out, only the values the emulated code changed are written back, so that a call that does not touch the port's fractional fuel does not truncate it. `load_it2`, which is native, writes the map to both the port and the emulator's memory.

`cpu_strtgy` runs once a frame, and `decision` once a tick and once a frame. `EmulatorCopyBenchmarks` measures the copy.

### The single-player globals
1.22 has globals that NSPW NET does not: the computer's plans and the scenarios' state, from `0x4867A0` to `0x489A28`. They live only in the emulator's memory. A new game creates a new emulator, so they start as the executable has them, then sets what 1.22's `game_init` sets before it calls the scenario: the end of the path of each of the 11 entries of the computer's fleet table (`0x4867A0`, `0x498` bytes each) to 9601, `MAP_RIGHT+1`.

## Unit numbering
1.22 numbers the units as NSPW NET does, with smaller ranges, and the emulated code depends on them: it loops over them, and keeps unit numbers in its own globals.

| | NSPW NET | 1.22 |
| --- | --- | --- |
| Japanese ships | 1 to 40 | 1 to 20 |
| American ships | 41 to 80 | 21 to 40 |
| Japanese planes | 81 to 130 | 41 to 70 |
| American planes | 131 to 180 | 71 to 100 |

Translating unit numbers at the boundary would need to know every field that holds one, including some meanings of `info[]`, and every single-player global that does. Instead, the port uses 1.22's ranges in a single-player game: `JPN_SHIP_START` to `USA_PLANE_END` are properties of the game, computed from `SHIP_LIMIT` and `PLANE_LIMIT`, which are 40 and 50 in a network game and 20 and 30 in a single-player game. The arrays keep NSPW NET's sizes, the larger.

## The game
1.22's battle frame is the port's `UpdateFrame` without the network:

1. `chara_cont` (the port's `UpdateBattle`), then `decision`, as many times as the speed asks.
2. Drawing, which clears the orders of the frame before the mouse gives the player's, then `decision` again.
3. If the game has no result: `cpu_strtgy`, which writes the computer's order into `new_pp[0]` and `slct_unit[0]`, then the orders of both sides, applied at once.

So in a single-player game (`IsSingleplayer`):

- `WinMain` connects to no one, and starts the battle of `ScenarioNumber` (`StartSingleplayerGame`).
- `InitializeGame` sets up 1.22's scenario instead of NSPW NET's, which sets the sides, the camera and the time limit, and sends nothing.
- `UpdateBattle` skips the two network phases, and counts `BattleTime` down every tick, as 1.22 counts `rest_time`.
- `HandleInput` buffers the player's orders as in a network game, then `IssueSingleplayerOrders` gives the orders of this frame: `cpu_strtgy`, then what `ReceiveAndIssueOrders` does for a turn, for the player only, and `ApplyOrders` for both. Orders take effect in the frame they are given, not at the next turn.
- `ApplyOrders` clears the path of a computer's unit for every move order (`if( new_pp[e].cls || unit[m].used==cpu_side )`, as 1.22 does and NSPW NET has as a comment), and calls `set_cpu_route(m, 4.0)` for a computer's ship.
- `CheckResult` calls `decision`, which also shows the result, instead of NSPW NET's conditions.

## Playing
```bash
dotnet run --project OpenNSPW/OpenNspw.Desktop -- --singleplayer 3
```

The scenario is one of 1, 2, ..., 10, 100, ..., 109, 200, ..., 209, 300, ..., 306. The executable is `Original/Nspw122/NSPW_122.exe`, or the file given with `--exe`. The graphics, sounds and maps are NSPW NET's.

NSPW NET's debug keys (`DBG_MODE`) work as in a network game, but three are held back from a single-player game in a Release build (`GameSession`): F5, which swaps the side the player plays and would hand the player the computer's fleet; F6, which starts the scenario again; and F9, which sinks the selected unit.

## Tests
- `EmulatorTests`: every scenario setup runs on the executable alone and places units of both sides.
- `SingleplayerTests`: whole games, stepped as the characterization tests step them. In scenarios 3 and 4, the computer gives move orders to its own ships, which move, and the time limit counts down. Every scenario plays 3000 frames without failing. A battle is saved as `singleplayer_battle.png` next to the test assembly.

## Implementation steps
1. **Disassembler**: jump tables read from the executable, `rep stosd`, the generated files and their table. Done. It also found that Enzan did not sign-extend 8-bit displacements (`[eax-0x1C]`), which the earlier translation had wrong in 221 places.
2. **Emulator**: the image, the import stubs, the dispatch, native functions. Done.
3. **Mapping**: the shared globals and the units, copied in and out. Done.
4. **Unit ranges**: properties of the game. Done; the characterization traces did not change.
5. **The single-player game**: the paths above, started from the desktop app with `--singleplayer`. Done.
6. **Setting screen**: choosing single-player and the scenario in the game, as 1.22's setting screen does. Not done.
7. **Characterization trace** of a single-player game, to guard the refactoring of the code it runs. Not done.

## Open questions
- Whether the port's own `SetUnitData` (used when a unit is created during the battle, outside the emulated code, such as a reinforcement) should call 1.22's in a single-player game.
- Which other differences between 1.22's battle and NSPW NET's matter for single-player play. The landing distance of transports, 360 in 1.22 and 160 in NSPW NET (`#if NSPW_THE_NET` in `etc1.cpp`), is one; the other remains of `cpu_side` in NSPW NET's comments are others.
- Whether to load the maps from 1.22's own `Map` folder: its `japan_off.dat` differs from NSPW NET's.
- Whether to show 1.22's own setting screen, or NSPW NET's with 1.22's scenarios.
- Saving and resuming a single-player game, which would also need the single-player globals of the emulator's memory.
