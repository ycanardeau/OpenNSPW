# Verifying single-player against 1.22
Reading 1.22's machine code finds differences, but only 1.22 itself, run on Windows, shows what it does. A trace records a battle of 1.22 as it runs, point by point; the port replays it from the same random numbers and stops at the first point where it does something else.

## What a trace records
At each point, rand's state (`0x442250`), which every random number of the battle moves, so that a single draw more or less shows at once:

| Point | Where in 1.22 | Where in the port | Also recorded |
| --- | --- | --- | --- |
| Start | `game_init` (`0x429C50`) | `InitializeGame`, at its start, where the replay sets rand's state | The scenario (`sinario`) |
| Seeded | After `game_init`'s last `srand(time(0))` (`0x42A16E`) | `InitializeGame`, at its end | |
| Frame | The frame (`0x434570`), in a battle | `TryStartSingleplayerFrame` | The speed |
| Tick | `chara_cont` (`0x401000`) | `UpdateFrame`, before `UpdateBattle` | `cc_count`, whether the tick is drawn, the units 0 to 100 and the fires 0 to 255 in 1.22's layout |
| Decision | `decision` (`0x41C3C0`) | `CheckResult` | |
| Orders | The orders (`0x430390`), before `cpu_strtgy` | `IssueSingleplayerOrders` | |
| Drawing | `draw_cmbt_area` (`0x40DB50`), after the ticks | `UpdateFrame`, before `DrawBattleArea` | The view (`cmbt_x`, `cmbt_y`, `0x489AB0`) |
| PlayerOrders | The orders (`0x430390`), before the Orders point, in a frame where the player gave some | `HandleInput`, at its start | No rand's state, but the player's orders (below) |
| StackLocal | Where 1.22 reads a local it has not set: in `cpu_strtgy`, scenario 208's count at `0x4050C7`, `[esp+30h]`; in the battle, the heading of a torpedo plane's check for land at `0x413D23`, `[esp+38h]` | Given after the point before it (`GiveStackLocal`): written into the emulator's stack before `cpu_strtgy` runs, or kept until `PlaneDropTorpedo` reads it (`TakeStackLocal`) | No rand's state, but the instruction and the bytes Windows' stack held there, which the port's does not |

The replay (`Trace122Replay`) gives the port the two seeds, the speed of each frame, and runs it with the ticks the trace has, as 1.22's timer may run more than speed + 1. It gives the port 1.22's view before each drawing: the view moves by the player's input, which the trace does not have, and it decides which units are drawn, which sets their sprites (`SpriteColumn`). The port's cursor stays in the middle of the battle area, where it neither scrolls nor clicks. At each point it compares rand's state, and before each tick `cc_count` and every field of the units and the fires, by name (`unit 23: hp[0]: 1.22 12, port 11`). Doubles may differ by a millionth: 1.22 computes them with the x87, the port with SSE2 and its own `sin`, `cos` and `atan2`. The file format is in `Trace122.cs`.

### The player's orders
1.22's player orders the battle in two ways, which a trace records (`Trace122Orders`):

- **On the battle area.** The drawing (`0x40DB50`) clears last frame's orders, then turns the clicks into a move order (`new_pp[1]`, `0x463AE0`: the unit, the point, whether its path is cleared) and a select order (`new_slct[1]`, `0x489A60`: a target, a landing point, or a carrier to land on, for the selected unit), and changes the selection (`the_slct_unit`, `0x463AA8`; `slct_unit[1]`, `0x489CE0`, and its count, `0x463B28`). The orders (`0x430390`) apply them at the end of the frame, after `cpu_strtgy`. The recorder reads them all as the orders start.
- **On the combat menu.** The input (`0x4312A0`), after the ticks, applies a clicked item to the selected unit at once (`0x431C53` to `0x431D93`): repair and supply (5), a mode (1 to 4) for the planes of its group, or a weapon (15 to 17) for its attackers and bombers. The recorder reads the item (`cmbt_menu_slctd`, `0x489AC0`) and the unit where it is applied (`0x431C60`).

The port gives them where its own input takes the clicks (`GivePlayerOrders`, at the start of `HandleInput`): the selection, the item, through `ChooseCombatMenuItem`, then the move and select orders, which `HandleInput` and `IssueSingleplayerOrders` apply as they would apply the player's clicks. The port applies the menu's item with the orders, after the drawing and `decision`, where 1.22 applies it before them; the next tick compares the units either way. `TheReplayGivesThePlayersOrders` records the port's own orders and replays them without input.

## Recording on Windows
The recorder runs `NSPW_122.exe` under the Windows debugger API, with breakpoints at the points above, and writes every battle played in it to a file, `<scenario>-<date>-<time>.n122`.

1. Build it, on Windows or elsewhere, as a single 32-bit Windows executable:

    ```
    dotnet publish OpenNSPW/OpenNspw.Recorder122 -c Release -o recorder
    ```

2. On Windows, run it with 1.22's executable and a directory for the traces:

    ```
    recorder\OpenNspw.Recorder122.exe Original\Nspw122\NSPW_122.exe traces
    ```

3. Choose a scenario in 1.22, and play it with the mouse: orders on the battle area and the combat menu, the speed keys (F7 slower, F8 faster, F9 back to 1), and the view, with the cursor at an edge or on the minimap. Press no other key: the replay gives the port only the speed, the view and the orders. Let the battle run, as long as wanted.
4. Close the game (Alt+F4, or Escape). The recorder writes the end of the file and exits. Closing the recorder's console instead keeps what was recorded until then.

### Without playing
With `--scenario <n>`, or `--all` for every scenario one after another, the recorder plays 1.22 itself:

```
recorder\OpenNspw.Recorder122.exe Original\Nspw122\NSPW_122.exe traces --scenario 10 --speed 100 --frames 2000
```

It goes past the title screen as a click does (the title's button, `0x463B30`, set to 3 where the title tests it, `0x40AD39`), and starts the scenario where the scenario screen tests for a battle to start (`0x40C613`), after its rows and its click have set `sinario` and the mode: there it writes the scenario and the mode 10, and `game_init` starts the battle as after a click. `--speed` sets the speed at the start of each frame, as the speed keys do (1.22 goes up to 100). It closes 1.22 at the start of a frame once `--frames` frames are recorded, or once the battle has a result (`game_end`, `0x483B44`), so that the trace ends with a whole frame, and says why.

It also orders the player's side (`Commander`): every `--orders` frames (20 by default, 0 for none), one time in three an item of the combat menu, else an order on the battle area, and records them as the player's:

- **On the battle area.** One of its ships or planes, chosen at random but its bases and the units supplying, attacks an enemy unit it sees, one time in two when it sees one, or else moves to a random point within 2000 of it, which launches a parked plane. The recorder writes the order where 1.22's drawing leaves those of the clicks (`new_pp[1]`, `new_slct[1]`), as its orders start, so that 1.22 applies it as a click's.
- **On the combat menu**, in the next frame. As 1.22's input starts (`0x4312A0`), the recorder has it call its own selection of a clicked unit (`0x417CF0`) for one of its ships or planes, chosen at random; then, where the input tests each entry of the menu it has made for the unit for a click (`0x431C18`), it sends the input where a click on an entry goes (`0x431C53`), with the entry's item, one chosen at random but the item the unit has. 1.22 applies it as a click's.

Its choices draw from its own random numbers, of `--seed`, which it prints, never from 1.22's `rand`; 1.22's battle still differs from run to run, by the seed of its `srand(time(0))`.

Click nothing in 1.22 while it plays; moving the cursor only scrolls the view, which the trace records.

The recorder checks that the executable is 1.22 by its `rand`, at `0x436056`. It slows 1.22 a little, which only makes some of its frames run more ticks; the trace has them.

## Replaying
Copy the traces to `OpenNSPW/OpenNspw.Tests/Traces122`, then:

```
dotnet test OpenNSPW/OpenNspw.Tests --filter ThePortFollowsTheTracesOf122
```

The test replays every trace there to its end, and fails with the first difference of each: the frame, the point, `cc_count`, and what differs. `TheTraceOfThePortReplays`, `TheReplayFindsAChange`, `TheReplayFollowsTheView` and `TheReplayGivesThePlayersOrders` check the recording, the comparison, the view and the orders themselves on traces of the port. A trace of the formats before is read as it is: one with uninitialized locals of four bytes (`N122TRC4`), one without them (`N122TRC3`), whose port reads what its own locals hold, and one without the orders (`N122TRC2`), whose player gave none; an older one is refused: record it again. A trace without the torpedo planes' headings may differ where 1.22's stack held one but 0, which it does now and then.

## Watching a trace
The desktop app plays a trace in its battle screen:

```
dotnet run --project OpenNSPW/OpenNspw.Desktop -- --replay OpenNSPW/OpenNspw.Tests/Traces122/<trace>.n122
```

The game starts in the trace's scenario and follows it as the test does: its seeds, the speed and the ticks of each frame, and 1.22's view. Up to the first difference its battle is 1.22's, so what it shows is the recorded battle. While it follows the trace, it takes none of the player's keys, clicks or commands, only Escape and F12 to quit; the cursor moves, which changes only what is drawn and the view, which the trace gives back (`TheCursorDoesNotChangeTheReplay`). The window's title shows the frame. At the first difference, or at the end of the trace, the title says so, the console shows the difference in full, and the game goes on as an ordinary single-player game, which the player can take over.

## Not covered yet
- Input beyond the orders: the keys but the speed keys, and what the player's clicks change outside the orders and the combat menu, if 1.22 has any; a trace that has some shows them as its first difference. No trace of 1.22 with the player's orders has been replayed yet.
- The effects themselves, the clouds and the computer's own globals are not recorded; their random numbers are, through rand's state.
