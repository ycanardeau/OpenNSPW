# Known issues
Bugs found in the port, and in the original, with what is known about them. A bug in the port is anything that makes it behave differently from the original source: the port must stay compatible with the original C++ build, including over the network.

## Fixed
### Transports landed their cargo from too far away
**Symptom**: a C# host and a C++ guest desynced (the out-of-sync indicator) between the moment the host ordered a transport to land and the moment its cargo came ashore.

**Cause**: [`all_head.h`](../Original/NSPW_NET/all_head.h) sets `#define NSPW_THE_NET 1`, so the original's `fire_now` ([etc1.cpp](../Original/NSPW_NET/etc1.cpp)) launches a transport's landing craft when the landing point is at most 160 away:

```cpp
#if NSPW_THE_NET
				if( dstc>=0 && dstc<=160 )
#else
				if( dstc>=0 && dstc<=360 )
#endif
```

The port kept the `#if`, and turned the `#define` into a `public const` in `all_head.cs`. A C# `#if` only sees the symbols defined for the compilation, not constants, and `NSPW_THE_NET` was not defined in `OpenNspw.csproj`, so the port compiled the `#else` branch and launched the cargo from up to 360 away. Two games of the port agree with each other, so only a game against the C++ build showed it.

The bug dates from the port of `etc1.cpp`, not from the refactoring: a landing replayed on the first commit of the refactoring and on its last gives the same trace, bit for bit.

**Fix**: `NSPW_THE_NET` is defined in `OpenNspw.csproj`, with the other switches of `all_head.h` that are 1 (`SND_SW`, `DBG_MODE`).

**Guards**:

- `CompileSwitchTests` checks that every switch of `all_head.h` that the port tests with `#if` is defined for the port exactly when the original sets it to a value other than 0.
- The characterization trace `transports-landing` lands transports, so a change of the distance changes the trace.
- Function tests of `fire_now`, `set_pos_of_attack_TR1` and the landing craft of `cont_fire` compare the landing with the reference (they are skipped until they are recorded on Windows; see [OpenNSPW/README.md](../OpenNSPW/README.md#recording)).

**How it was found**: two games of the port, with every landing that scripts and random input could produce, never desynced, and the refactoring commits that touch the landing all kept the behavior. The desync only appeared against the C++ build, so the cause had to be a difference between the port and the original, and the landing distance was the one place in the landing whose code depends on a compile-time switch.

### Single-player games crashed on a route that never ends
**Symptom**: single-player games crashed, whether the player gave orders or none: `ArgumentOutOfRangeException` in the emulator's `Memory32`, from `set_cpu_route`, when the computer replanned a blocked ship (scenario 209 started at time 1000007919, the player's ships sent at the enemy, at frame 5631; scenario 302 at 1000007919, no orders, at frame 19983) or ordered one (scenario 101); and `IndexOutOfRangeException` in `UpdateBattle` (scenario 301 at 1000000000, no orders, at frame 17084).

**Cause**: a bug of 1.22. `set_cpu_route`'s loop plans waypoints until a leg is clear (`while(pp_indx>=0)`, [ComputerPlayer.md](ComputerPlayer.md#set_cpu_route)), with nothing to keep the route in its path's 64 points. A waypoint that would turn back moves the destination 160 further, up to 2000; but once the leg to the moved destination is clear, that distance goes back to 0 (`0x42EC6F`), and when the leg from there back to the destination is blocked again, the loop adds the same points forever: two at a time where one step of 160 clears the leg (scenarios 209 and 301), five where it takes 640 (302). Past the path, it writes on through the units numbered after the ship and the globals after them. In 209 and 302 it went on past the image, and the emulator's memory ran out; in 301 one of its writes freed the leg, so the loop ended, and the port copied the units it had overwritten back into the battle, where a target number out of range failed.

1.22's machine code runs the same loop on the same state. Its image ends with its resources (`.rsrc`, `0x4F1000`), which are read-only, so its first write past `.data` would be an access violation, and it would crash, having overwritten every unit after the ship; where the loop ends first, as in 301, it would play on with those units. This is read from the machine code, not seen on Windows: these states came from the port's battle, which is NSPW NET's, and whether 1.22's own battle reaches them is not known.

**Fix**: the port ends the route at point 62 of the path, the last whose leg cannot write past it (point i + 2). `set_cpu_route` calls `SetWindowTextA` with "Panzer Vor!" each time round its loop, as 1.22 runs in a window there, and the port's stand-in sets the loop's index to -1 from point 62 (`SetWindowTextNatively`). A route that 1.22 plans within its path is unchanged: from point 62, 1.22 also ends it, unless it writes past the path or finds no heading clear.

**Guards**: `SingleplayerTests.RoutesThatNeverEndStopAtTheEndOfThePath` plans such a route between two ships on the open sea, and checks that the other units are left as they were and the route stays in its path. Without the fix, the loop clears the used of the ship in the way, the unit after, which ends it, as in 301.

**How it was found**: playing every scenario headless, from several start times. The crashes came in games where the player gave no order at all, so they were the game's. Logging `pp_indx` in the translated loop showed it past 64, with the same points written again and again, in each of these games, before its failure.

## Open
### A selected transport crashes the port when the cursor is off the map
With a transport selected, `DrawBattleArea` (`draw_cmbt_area.cpp`) looks up the tile under the cursor in `cmbt_map`, to show where the transport would land. If the cursor maps to a tile outside the map, such as left of the battle area while the battle area is at the left edge of the world, the index is out of range. The original reads out of bounds without failing; the port throws `IndexOutOfRangeException`, which ends the game. The desktop app passes the cursor position through even when it is outside the window.

### Single-player differs from 1.22 in the last bit of a sine, cosine or arc tangent
1.22 keeps the result of `fsin`, `fcos` and `fpatan` in an 80-bit x87 register until the next instruction uses it, and that instruction rounds only its own result to 53 bits; the port rounds the sine, cosine or arc tangent to a `double` first. The last bit of the result then differs now and then. It changes the battle only where a unit's decision rests on that bit: in 1 trace of 1.22 of 316 (s6/303), a ship that sat on its path point turned the other way. [X87Emulation.md](X87Emulation.md) has the details and plans the fix; it is not planned to be done for now.

## In the original
These are bugs of the original that the port keeps, because changing them would make the port incompatible with the C++ build.

### The launch lock of a carrier depends on the local side
When a plane has taken off, `chara_cont.cpp` counts the other planes of the same carrier that are still on its deck, to decide whether the carrier may launch the next one (`unit[].info[8]`). It counts only the planes of `your_side`, the local player's side, so for a carrier of the rival's side the two games can count differently and set different values. It is not yet confirmed by a test that this desyncs a game.

## Limits of the tests
- **The characterization traces** compare the port with itself. They show that a refactoring keeps the port's behavior, not that the behavior is the original's: the landing distance above passed them all.
- **Against the reference**, only the function tests compare the port with the original source: `etc2.cpp`, the start of `etc3.cpp`, and the landing above. The rest of the game logic is compared with nothing yet. Trace tests against the reference ([Porting.md](Porting.md#trace-tests)) would compare all of it, and need the reference's recording mode, which is not written yet.
- **Against the original build**, even a port that matches the reference can differ in the last bit of `sin`, `cos` and `atan2`, because the reference uses portable math functions and the original the MSVC CRT's on the x87 FPU ([Porting.md](Porting.md#matching-the-original-build)). A game against an unmodified C++ build can therefore desync for that reason alone.
