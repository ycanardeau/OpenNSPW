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
| `set_unit_data` | `0x41E150` | By the emulated code, and by `SetUnitData` in a single-player game, for the units the battle creates |
| `set_new_unit`, `set_new_unit_plane`, `plane_in_cv`, `set_pos_of_parking`, `cls_all_slct_unit_p2`, `pt_in_rect3` | | Only by the emulated code |

`set_cpu_route2` (`0x42ED30`) is translated, and `SetDynamicDestination` calls it (`SetCpuRoute2As122`) for a ship that follows a leader, as 1.22's formation does (`0x414D79`); the port's `SetCpuRoute2`, which NSPW NET calls for every unit, stays for network games.

The scenario numbers and their functions are those of 1.22's `game_init` (`0x429C50`), which dispatches to them through two jump tables.

Native, because they are part of the port's own state or platform:

| Function | Native |
| --- | --- |
| `rand` (`0x436056`) | The port's `rand()`, so that the whole game draws from one sequence, as in the original |
| `__ftol` (`0x4360C4`) | Truncation of `ST(0)` to `EDX:EAX` |
| `load_it2` (`0x432A30`) | The port's `LoadScenarioFile2`, by NSPW NET's name of the same file, which is read from 1.22's directory: 1.22 also spells some in lower case (`Map\south_pacific.dat`), and `LoadScenarioFile2` recognizes the map by its name |
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

`cpu_strtgy` runs once a frame, and `decision` once a tick. `EmulatorCopyBenchmarks` measures the copy.

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

1. `chara_cont` (the port's `UpdateBattle`) speed + 1 times, each but the last followed by `decision`.
2. The input (`0x4312A0`), which applies a combat menu item the player clicked to the selected unit at once (`0x431C53`).
3. Drawing, which clears the orders of the frame before the mouse gives the player's, then `decision` for the last tick.
4. If the game has no result: `cpu_strtgy`, which writes the computer's order into `new_pp[0]` and `slct_unit[0]`, then the orders of both sides, applied at once.

So in a single-player game (`IsSingleplayer`):

- `WinMain` connects to no one, and starts the battle of `ScenarioNumber` (`StartSingleplayerGame`).
- `UpdateFrame` runs a frame on 1.22's timer, every 88 ms (`TryStartSingleplayerFrame`), with speed + 1 ticks, none at speed 0, and `decision` after each tick but the last, which is drawn first (`0x4345DF` to `0x43463A`). The speed keys are NSPW NET's, as in a network game: F, G and R, and Space for 20 ticks a frame; 1.22's are F7, F8 and F9 (`0x433AF0` to `0x433B53`).
- `WinMain` seeds rand from the time as the game starts, as 1.22 does (`0x434456`), and `InitializeGame` goes on from there, unless a trace gives rand's state: the scenario's random setup, such as where scenario 4's American fleets start, differs from game to game. It makes no table of shared random numbers, which 1.22 does not have, and seeds rand last from the time, as 1.22's `game_init` does (`0x42A163`).
- `InitializeGame` sets up 1.22's scenario instead of NSPW NET's, which sets the sides, the camera and the time limit, and sends nothing.
- `InitializeGame` starts the battle at frame 1 (`FrameCount`), as 1.22 does: its `game_init` runs within the frame of the setting screen that starts the battle (`0x40C61E`), which then counts itself (`0x434E9F`).
- `UpdateBattle` skips the two network phases, and counts `BattleTime` down every tick, as 1.22 counts `rest_time`.
- `HandleInput` buffers the player's orders as in a network game, then `IssueSingleplayerOrders` gives the orders of this frame: the combat menu's item, which 1.22 applies in its input, then `cpu_strtgy`, which may see it, then what `ReceiveAndIssueOrders` does for a turn, for the player only, and `ApplyOrders` for both. Orders take effect in the frame they are given, not at the next turn.
- `ApplyOrders` clears the path of a computer's unit for every move order (`if( new_pp[e].cls || unit[m].used==cpu_side )`, as 1.22 does and NSPW NET has as a comment), and calls `set_cpu_route(m, 4.0)` for a computer's ship.
- The maps, the sprite sheet (`tst_cg1.bmp`) and the sounds are read from 1.22's directory (`GameFiles.OpenSingleplayer`), so that the map on which `set_cpu_route` plans is 1.22's; the three sounds 1.22 does not have are NSPW NET's.
- `UpdateSupply` keeps a ship supplying wherever it is, as 1.22's `chara_cont` does (`0x4010BB`), without NSPW NET's check for a naval base within 280: the computer starts supplying once its flagship has stopped within 580 of the base, and that check stopped it at once.
- `SetShipEmergencyDestination` brings only the player's ships back into the map, and `SteerUnit` no submarine, as 1.22 does (`0x416B35`, `0x401966`): the computer's ships sail on, to its naval bases outside the map, such as scenario 304's.
- `SetUnitData` runs 1.22's `set_unit_data`, so that the units the battle creates, such as the bases that transports land, have 1.22's data, as the scenario's do; `AddPlane` adds planes to a full carrier, as 1.22's `set_new_unit_plane` does (`0x41F060`).
- `LandCargo` lands a transport's cargo within 360 of its landing point, as 1.22 does (`0x413003`), where NSPW NET lands it within 160 (`#if NSPW_THE_NET`), and keeps the transport, empty, as 1.22 does (`0x413014` to `0x41312B`), where NSPW NET removes it.
- `MoveUnit` does not slow a transport with cargo, as 1.22 does not (`0x402955`).
- `UpdateCargo` completes the base a transport lands at once, with its full HP, as 1.22 does (`0x418E23`), where NSPW NET builds it over 8000 to 20000 ticks from a quarter of its HP.
- `AttackerDropBomb` releases every attacker's bombs from 170 to 180 of its aim, as 1.22 does (`0x414186`): NSPW NET's Japanese attackers dive to 35 to 65 instead.
- `SetAttackerEmergencyDestination` sends a returning attacker's or bomber's fighters back when it hands them over if it is itself flying, as 1.22 does (`0x415916`), where NSPW NET tests each fighter.
- `PlaneDropTorpedo` leaves the path of the planes that follow a torpedo plane once it drops its torpedo, as 1.22 does (`0x413F51`), where NSPW NET sends them to where it dropped it; and checks for land along the heading that a trace gives as 1.22's stack held it, or else 0, due east, as 1.22 does, which never sets it (`0x413D23`); at the map's edges, it tests the tiles around the plane beyond `cmbt_map` as 1.22 reads them, its memory around the map, in the emulator (`MapTileOrBeyond`), where the port failed.
- `FighterFireBullet` shoots no transport, `ApplyOrders` sends no fighter to attack one, and `SetShipEmergencyDestination` does not turn a transport away from fighters, as 1.22 does (`0x4133B2`, `0x430618`, `0x41650D`): its fighters fight planes only.
- `ApplyOrders` sends a torpedo plane against any ground unit but an air base or a naval base, as 1.22 does (`0x4305F9`), where NSPW NET refuses them all; neither drops a torpedo on one.
- `SetDynamicDestination` keeps a flying plane in the group of a parked, returning leader, as 1.22 does (`0x4149F0`), where NSPW NET has it leave.
- `ApplyUnitInfoInput` gives a mode chosen from the combat menu to every plane it is for, flying or parked, and Move clears its deck phase, as 1.22 does (`0x431D19`), where NSPW NET gives Move and Return to flying planes only.
- `ApplyUnitInfoInput` arms no bomber with a torpedo from the combat menu, as 1.22 does (`0x431D56`), where NSPW NET arms Japan's.
- `ControlFiring` fires as 1.22 does (`0x4030D8` to `0x40396E`): nothing for Japan in scenario 1, the guns of infantry bases, pillboxes and fortresses 1 in 200, 150 and 100 a tick, every battleship, cruiser and destroyer as NSPW NET does its first type, transports landing ground units only, and no torpedoes from bombers.
- `UpdateBattle` keeps a submerged submarine as a target, and a parked plane's target, as 1.22 does (`0x402FD2`).
- `Detect`, `GetDetectionSize` and `DetectSubmarines` find units as 1.22 does (`0x418230`, `0x418080`, `0x417EA0`): with its ranges, the player's supplying units looking out, submarines diving at any unit found, and the computer's destroyers that chase a submarine stopping when it is found. The ranges at which effects show and sounds play are still NSPW NET's.
- `CheckCourseAhead` looks 40 ahead with only air bases and naval bases out of the way, and `ReplanBlockedRoute` replans the computer's units that a ship or land blocks, as 1.22 does with `set_cpu_route(m, 1.0)` (`0x401F95` to `0x4023EB`, `0x402D2B`), where NSPW NET's steering stops them.
- `UpdateParkedPlane` clears the launch lock of the base a plane has landed on, whatever other planes land, as 1.22 does (`0x4018F1`).
- `UpdateBattle` plays no sonar, which 1.22 does not have.
- `FindFreeFire` and `FindFreeEffect` keep 255 fires and 255 effects in use at most, as 1.22 does.
- `SetShipEmergencyDestination` and `EvadeShip` make a stopped ship flee enemy ground units too, within 900, as 1.22 does (`0x41616C`, `0x416277`): in scenario 305 the American transports at Okinawa move away at the start.
- `ShipDropAntiSubmarineBomb`, `SetFighterAttackDestination`, `EvadeTorpedo`, `EvadePlane` and `LeaveWake` keep the fraction of a degree of a depth charge's heading from its ship, a fighter's to dodge a head-on pass, a ship's to evade and a ship's wake's, but its last digits, as 1.22 does by storing the whole degrees into the low half of the double (`0x410452`, `0x415324`, `0x416417`, `0x416838`, `0x41C327`, `ReduceHeading`).
- `HuntSubmarineAs122` makes only the computer's destroyers hunt submarines, and chase one (`em_flg[1]`), as 1.22 does (`0x416706` to `0x416B35`).
- `UpdateBattle` counts the tick, moves the clouds and finds the units in 1.22's order, and runs its unit loop for cities too (`0x401009` to `0x4010A3`).
- `UpdateLanding` adjusts a returning plane's approach on the tick that is drawn only (`IsDrawnTick`, 1.22's `0x48A1EC`), and to any base still in use, as 1.22 does (`0x401AB4`, `0x417530`).
- `MoveUnit` burns a moving unit's fuel 1 at a time, as 1.22 does with its integer `gas[0]` (`0x402F72`).
- `MoveUnit` turns a heading back to 0 only past 360, as 1.22 does (`0x4024B6`), where NSPW NET does at 360; `SetLandingDestination` and `UpdateLanding` then take the landing pattern and the approach of a carrier heading 360 along the angle that a trace gives as 1.22's stack held it, or else 0, and `SetShipFormation` the place in the formation of a leader heading 360 from the ship's x offset taken as an angle, as 1.22's switches have no case for 360 (`0x4173BF`, `0x41764E`, `0x417179`).
- `MoveUnit` brakes a ship out of fuel from its top speed times 0.1, as 1.22 does (`0x40285F`), where NSPW NET's top speed over 10 can be less in the last bit, so it brakes sooner.
- `SetAttackerAttackDestination` keeps the attackers in formation, as 1.22 does (`0x415A10`), where NSPW NET's leader sends them off at 500 to 510 from the target.
- `ShipFireRapidAntiAircraftShell`, `ShipDropAntiSubmarineBomb`, `ShipFireGunWithoutTarget` and `ShipFireGunAtTarget` shoot as 1.22's `fire_now` does (`0x40FE7C` to `0x412BE1`): one sound for rapid fire, an odd count of depth charges left at −1, the same aim for every kind of gun, one tick less of flight, and the draws in its order.
- `ShipFireTorpedoWithoutTarget` and `ShipFireTorpedoAtTarget` reload a submarine's torpedoes in 2000 ticks, as 1.22 does (`0x4110DB`); `ShipFireTorpedoAtTarget` leaves a submarine its target once it fires at it, as 1.22 does (`0x4117F7`).
- `FighterFireBullet` chooses a fighter's target as 1.22 does (`0x413542`): within 400 for every kind, and the computer's returning fighters too, within 300.
- `SetFighterAttackDestination` tells a fighter and its target of the same skill apart by the frame, as 1.22 does (`0x4150A9`), where NSPW NET does by the tick.
- `EffectRandom` draws the random numbers of the fires' effects, and of the place of a plane landed on deck, from `rand`, as 1.22 does, where NSPW NET draws them from `SharedRandom`; `UpdateBullet` damages a transport with every bullet (`0x418F92`).
- `DrawBattleArea` tests whether the player sees an effect by 1.22's sight ranges, and draws its animation from `rand`, as 1.22's drawing does (`0x40C7A0`, `0x40CDD0`): what the player sees moves the battle's random numbers. `UpdateLowerEffects` still tests the lower effects once the battle has a result, as 1.22 does (`0x40CDEC`), where NSPW NET draws them all.
- `UpdateUnitEffects` makes the marks, the leader's flag, the damaged ships' smoke and fire and the damaged submarines' bubbles on the drawn tick only (`IsEffectTick`), tests a damaged plane's tick and a ship's damage before it draws the random number of its smoke, and makes no marks for the second types, as 1.22 does (`0x41AEB0`); `UpdateFires` makes the fires' sprites, trails and wakes on the drawn tick only (`0x419099` to `0x41A774`).
- `GetDamagePoints` gives a depth charge 1.22's damage, 3 to 5 (`0x416D1F`).
- `UpdateAntiSubmarineBomb` keeps an exploded depth charge's fire in use, with the submerged submarine it hit or tested last as its target, as 1.22's bug does (`0x4196DC` to `0x4197EC`).
- `CheckHit` hits a surfaced submarine within 1.22's smaller size, 4 by 2 of each part (`0x40F9D6`), where NSPW NET's is 8 by 4.
- `CheckResult` calls `decision`, which also shows the result, instead of NSPW NET's conditions.

## Playing
```bash
dotnet run --project OpenNSPW/OpenNspw.Desktop -- --singleplayer 3
```

The scenario is one of 1, 2, ..., 10, 100, ..., 109, 200, ..., 209, 300, ..., 306. The executable is `Original/Nspw122/NSPW_122.exe`, which the app embeds with the files of its directory, or the file given with `--exe`. A single-player game reads the files of the executable's directory, which are 1.22's own: the maps, the sounds and the sprite sheet (`tst_cg1.bmp`, which the app puts together from its sprites). The files 1.22 does not have, such as the sounds `AA_SHL5`, `SNR` and `CLICK2`, are NSPW NET's (`DesktopPlatform.OpenFile`, and `GameFiles.OpenSingleplayer` in the tests).

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
7. **Characterization trace** of a single-player game, to guard the refactoring of the code it runs. Traces of 1.22 itself, recorded on Windows, replayed by the port ([Verification.md](Verification.md)): the tools are done; the traces are not recorded yet.

## Open questions
- Which other differences between 1.22's battle and NSPW NET's ([Differences.md](Differences.md#the-battle)) matter for single-player play.
- Whether to show 1.22's own setting screen, or NSPW NET's with 1.22's scenarios.
- Saving and resuming a single-player game, which would also need the single-player globals of the emulator's memory.
