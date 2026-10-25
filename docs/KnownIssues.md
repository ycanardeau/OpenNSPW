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
