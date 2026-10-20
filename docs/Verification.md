# Verifying single-player against 1.22
Reading 1.22's machine code finds differences, but only 1.22 itself, run on Windows, shows what it does. A trace records a battle of 1.22 as it runs, point by point; the port replays it from the same random numbers and stops at the first point where it does something else.

## What a trace records
At each point, rand's state (`0x442250`), which every random number of the battle moves, so that a single draw more or less shows at once:

| Point | Where in 1.22 | Where in the port | Also recorded |
| --- | --- | --- | --- |
| Start | `game_init` (`0x429C50`) | `InitializeGame`, after its `srand` | The scenario (`sinario`) |
| Seeded | After `game_init`'s last `srand(time(0))` (`0x42A16E`) | `InitializeGame`, at its end | |
| Frame | The frame (`0x434570`), in a battle | `TryStartSingleplayerFrame` | The speed |
| Tick | `chara_cont` (`0x401000`) | `UpdateFrame`, before `UpdateBattle` | `cc_count`, whether the tick is drawn, the units 0 to 100 and the fires 0 to 255 in 1.22's layout |
| Decision | `decision` (`0x41C3C0`) | `CheckResult` | |
| Orders | The orders (`0x430390`), before `cpu_strtgy` | `IssueSingleplayerOrders` | |
| Drawing | `draw_cmbt_area` (`0x40DB50`), after the ticks | `UpdateFrame`, before `DrawBattleArea` | The view (`cmbt_x`, `cmbt_y`, `0x489AB0`) |

The replay (`Trace122Replay`) gives the port the two seeds, the speed of each frame, and runs it with the ticks the trace has, as 1.22's timer may run more than speed + 1. It gives the port 1.22's view before each drawing: the view moves by the player's input, which the trace does not have, and it decides which units are drawn, which sets their sprites (`SpriteColumn`). The port's cursor stays in the middle of the battle area, where it neither scrolls nor clicks. At each point it compares rand's state, and before each tick `cc_count` and every field of the units and the fires, by name (`unit 23: hp[0]: 1.22 12, port 11`). Doubles may differ by a millionth: 1.22 computes them with the x87, the port with SSE2 and its own `sin`, `cos` and `atan2`. The file format is in `Trace122.cs`.

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

3. Choose a scenario in 1.22. From the moment the battle starts, neither click nor press a key but the speed keys (F7 slower, F8 faster, F9 back to 1): the replay gives the port no input, only the speed and the view, so a click or another key would be the first difference. The view may be scrolled with the cursor at an edge of the battle area. Let the battle run, as long as wanted.
4. Close the game (Alt+F4, or Escape). The recorder writes the end of the file and exits. Closing the recorder's console instead keeps what was recorded until then.

The recorder checks that the executable is 1.22 by its `rand`, at `0x436056`. It slows 1.22 a little, which only makes some of its frames run more ticks; the trace has them.

## Replaying
Copy the traces to `OpenNSPW/OpenNspw.Tests/Traces122`, then:

```
dotnet test OpenNSPW/OpenNspw.Tests --filter ThePortFollowsTheTracesOf122
```

The test replays every trace there to its end, and fails with the first difference of each: the frame, the point, `cc_count`, and what differs. `TheTraceOfThePortReplays`, `TheReplayFindsAChange` and `TheReplayFollowsTheView` check the recording, the comparison and the view themselves on traces of the port. A trace of an older format is refused: record it again.

## Watching a trace
The desktop app plays a trace in its battle screen:

```
dotnet run --project OpenNSPW/OpenNspw.Desktop -- --replay OpenNSPW/OpenNspw.Tests/Traces122/<trace>.n122
```

The game starts in the trace's scenario and follows it as the test does: its seeds, the speed and the ticks of each frame, and 1.22's view. Up to the first difference its battle is 1.22's, so what it shows is the recorded battle. While it follows the trace, it takes none of the player's keys, clicks or commands, only Escape and F12 to quit; the cursor moves, which changes only what is drawn and the view, which the trace gives back (`TheCursorDoesNotChangeTheReplay`). The window's title shows the frame. At the first difference, or at the end of the trace, the title says so, the console shows the difference in full, and the game goes on as an ordinary single-player game, which the player can take over.

## Not covered yet
- Input: a trace has none but the speed and the view, so the player's orders, the unit panel and its menus are not checked.
- The effects themselves, the clouds and the computer's own globals are not recorded; their random numbers are, through rand's state.
