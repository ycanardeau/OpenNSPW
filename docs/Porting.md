# Porting
A proposal for porting the NSPW NET source ([Original/NSPW_NET](../Original/NSPW_NET)) to C# again, as an exact port, with automated tests that show the port behaves identically to the original source.

This is a proposal, not a final design. [Open questions](#open-questions) lists what still needs to be decided.

## Background
An earlier C# port started as a line-by-line port that kept the original's globals, function names and MSVC `rand()`. After that, it was refactored step by step and at some point stopped behaving like the original. Its behavior was only ever compared by playing both versions by hand, with tools to run the original windowed.

Comparing by eye cannot find these bugs, because of how the original uses random numbers:

- The simulation draws from one shared `rand()` sequence. A port that calls `rnd()` once more or once less, anywhere, shifts every random number after it, so the two versions slowly drift apart and it is impossible to tell where it started.
- Drawing code calls `rnd()` too. [`be_dstryd`](../Original/NSPW_NET/draw_cmbt_area.cpp#L454) and the effect code in `draw_cmbt_area.cpp` call it on 26 lines, so a bug in a drawing path also changes the simulation.

## Summary
- **Port again from the C++ source**, as close to the original code as C# allows, without refactoring. The earlier port is used only as a reference.
- **Build the unmodified C++ source on Windows** with Visual Studio, as it builds today (see [CHANGES.md](../Original/docs/NSPW_NET/CHANGES.md)), plus a layer that records and replays everything that enters or leaves the game: input, time, network messages, drawing and sound. This build, the **reference**, is the test oracle.
- **The reference writes traces**: the input, plus the whole game state and the drawing calls after every frame. The port replays a trace's input and must produce the same state and calls. Traces are files, so the port's tests run on any platform, and only recording needs Windows.
- **Test single functions** the same way, with recorded inputs and outputs, so porting can be tested file by file before the whole game runs.
- **Refactor only after the port matches**, with the same traces guarding every refactoring step.

## Layout
```
OpenNSPW/
  OpenNspw/            The C# port.
  OpenNspw.Reference/  The Windows-only Visual C++ project that builds the reference.
  OpenNspw.Tests/      Tests that replay traces through the port.
    Traces/            Recorded traces.
```

`OpenNspw.Reference` compiles the files in `Original/NSPW_NET` in place, with the same settings as `NSPW_NET.vcxproj`. It contains no copy of the original source, and the original source is not modified. It is not part of `OpenNSPW.slnx`, because `dotnet build` cannot build Visual C++ projects on macOS and Linux.

A desktop app that opens a window and plays the port comes later, as a separate project (see [Implementation steps](#implementation-steps)).

## Porting rules
The goal is that each C# file can be read side by side with its C++ file.

- **One C# file per C++ file**, with the functions in the same order and the same names (`chara_cont`, `set_pos_of_dynmc`, `unit`, `cc_count`). Comments are kept, including the Japanese ones. Commented-out and `#if 0` code is kept as comments.
- **Same types.** `short` stays `short`, `BYTE` becomes `byte`, `BOOL` becomes `int`, `DWORD` becomes `uint`, `double` stays `double`. Casts are kept where the C++ has them, and added where C++ converts implicitly (for example `short` arithmetic, which C# promotes to `int`).
- **Structs stay structs, with the same layout.** `UNIT`, `FIRE`, `EFFECT` and the others become C# `struct`s with the same field order, packing and size as in the 32-bit MSVC build, with fixed-size arrays as `[InlineArray]` fields. Copying a struct, as in `unit[a] = unit[b]`, then copies all its arrays, as in C++. A `class` would copy only a reference. Pointers into arrays (`UNIT *p = &unit[m]`) become `ref` locals. The same layout also lets the tests compare state byte for byte, and lets the port read the original's save files.
- **Strings stay bytes.** The original keeps Shift_JIS text in `char` arrays and formats it with `wsprintf`. The port keeps byte arrays and a `wsprintf` that produces the same bytes, so text can be compared exactly.
- **Globals become instance fields** of one `partial class`, split over the files like the C++ globals. This keeps the code unchanged (`unit[m].x`), and still allows two games in one process, for example a host and a guest in a test.
- **Platform calls go through stand-ins** with the original API's names and signatures (`Blt`, `BltFast`, `TextOut`, `SoundPlayEffect`, `GetDeviceData`, `SendTo`, `MessageBox`, `CreateFile`), so that the call sites are unchanged. In tests, they replay a trace and record calls. In the desktop app, they draw, play sound and send over the network.
- **No C# naming conventions, no `CLAUDE.md` ordering rules** in the ported files. Analyzer rules that conflict with the original names are turned off for these files in `.editorconfig`.

### Differences between C++ and C#
These are the places where a literal translation can behave differently. Either both sides are made to behave the same, or each case is handled on purpose.

| Difference | Reference (MSVC, 32-bit) | Port |
| --- | --- | --- |
| `char` signedness | Signed | `sbyte` where the code relies on sign |
| Integer overflow | Wraps | Wraps (`unchecked`, the default) |
| Fused multiply-add | Never: `/arch:SSE2` (the default) and no `/fp:contract` | Never |
| Out-of-range `double` to `int` | `0x80000000` | Saturates since .NET 9. Each case found is ported on purpose, with a comment. |
| Uninitialized locals | Leftover stack contents | Zero. Found with warnings C4700 and C4701, and reviewed one by one. |
| Out-of-bounds array access | Reads or writes the next variable | `IndexOutOfRangeException`. Found with `/fsanitize=address`, and each case is ported on purpose, with a comment. |
| `rand()` | MSVC CRT | MSVC algorithm: `seed = seed * 214013 + 2531011; return (seed >> 16) & 0x7FFF` |
| `sin`, `cos`, `atan2` | Same implementation as the port (see below) | Same implementation as the reference |

### Math functions
The MSVC CRT's `sin`, `cos` and `atan2` can differ in the last bit from .NET's `Math.Sin` and others, which call the platform's math library on macOS and Linux. The source calls them about 290 times, and positions feed back into the next tick, so one bit can change a battle.

Both sides therefore use the same portable implementation, built only from basic `double` arithmetic, which gives the same result everywhere: written in C for the reference, and ported to C# for the port. The reference's forced-include header (see below) redirects `sin`, `cos` and `atan2` to it. This makes the reference differ slightly from the original build. See [Matching the original build](#matching-the-original-build).

## The reference build
### Interception
The original source is compiled unmodified. A **forced-include header** (`/FI`) and a few extra source files intercept everything that enters or leaves the game:

| Area | Intercepted at | What is recorded or replayed |
| --- | --- | --- |
| DirectDraw | `DirectDrawCreateEx`, which returns a wrapper around the real object and its surfaces | Each `Blt` and `BltFast` with its rectangles and surface |
| GDI | `TextOut`, `SetTextColor`, `SetBkMode`, `SelectObject`, by macro | Each call with its text bytes and color |
| DirectSound | `DirectSoundCreate8`, wrapped like DirectDraw | Each sound played, with its volume and pan |
| DirectInput | `DirectInput8Create`, wrapped like DirectDraw | The keyboard and mouse data read by [`get_input`](../Original/NSPW_NET/input.cpp#L1051) |
| Cursor | `GetCursorPos`, `ScreenToClient`, by macro | The cursor position |
| Time | `timeGetTime`, `time`, by macro | The values returned |
| DirectPlay 8 | `CoCreateInstance` for `CLSID_DirectPlay8Peer`, wrapped | Messages sent and received |
| Dialogs | `DialogBox`, `CreateDialog`, `MessageBox`, by macro | The player's answers |
| Math | `sin`, `cos`, `atan2`, by macro | Nothing; see [Math functions](#math-functions) |

Everything else, including the files the game reads and writes, works as in the normal build. The wrappers pass calls on to the real DirectX objects, so the reference still opens a window (with `fullscreen` set to 0 from the forced-include header) and can be watched and played.

A frame starts when the main loop calls `updateFrame`, which first calls `timeGetTime`, and ends with the `Blt` from the back buffer to the primary surface ([win_main.cpp:586](../Original/NSPW_NET/win_main.cpp#L586)). The DirectDraw wrapper writes the frame's state to the trace at that point.

### Modes
- **Record**: play the game by hand. The reference passes all input through and writes the trace. To record a battle, run two instances on the same machine, or on two machines, connected with real DirectPlay. Each instance writes its own trace, including the messages it received.
- **Replay**: run a trace's input, time and received messages instead of the real ones, and write a new trace. Replaying a recorded trace must reproduce it exactly, which tests the interception itself.
- **Script**: run an input script, for example one generated randomly. A **fake rival** in the DirectPlay wrapper stands in for the other player: it answers each `DP_FLAG_1` with the same checksums, sends each turn's order from the script or `DP_NO_ORDER`, and plays the other side of the setup messages. NSPW NET is multiplayer-only, so without it a battle does not advance (see [Synchronization.md](../Original/docs/NSPW_NET/Synchronization.md)).

### Traces
A trace contains:

- A header: the size and field offsets of each struct, and the address range of each global, as compiled.
- The input of each frame: DirectInput data, cursor position, time values, received network messages and dialog answers.
- The output of each frame: drawing, text and sound calls, and network messages sent.
- The state: all globals declared in [`all_extern.h`](../Original/NSPW_NET/all_extern.h), including `unit`, `fire`, `effect`, `rnd_count`, `my_rnd_pt` and `cc_count`. The first frame stores all of it, and each later frame only the bytes that changed. The trace is compressed.

## Tests
### Struct layout
The test checks every C# struct's size and field offsets against the trace header. With identical layouts, the port's state can be compared with the trace byte for byte.

### Function tests
A small driver in `OpenNspw.Reference` calls single functions with generated inputs, for example the direction and distance helpers in `etc1.cpp` and `etc3.cpp`, and records the globals before and after each call. The C# test sets the same globals, calls the ported function, and compares. This allows porting and testing one file at a time, before the main loop runs.

### Trace tests
A trace test starts the port, feeds it the trace's input frame by frame, and compares its state and calls after every frame. On a mismatch, it fails with the first frame, the first variable or call that differs, and both values.

Traces come from:

- Hand-written scripts for specific features: each screen, each kind of order, reinforcements, saving and resuming, the end of a battle.
- Recorded games, including two-player games.
- Generated scripts: random clicks and keys in a battle, run for thousands of frames. They find differences nobody thought to test.

Traces are recorded on Windows and committed. The tests run on any platform, in CI, without Windows.

## Matching the original build
The traces check the port against the reference, which uses its own math functions. The original `NSPW_NET.exe` used the MSVC CRT's, on the x87 FPU, so the port does not match it bit for bit.

This matters only for playing against the original 1.10 (see [Multiplayer.md](Multiplayer.md#playing-against-the-original)). It can be checked later by recording the same scripts with the math redirection turned off and comparing. If they differ, the port would need its own copies of the x87 CRT functions.

## Relation to Multiplayer.md
[Multiplayer.md](Multiplayer.md) assumes a simulation that is already separated from drawing and input, with a replay harness and a parity check against the original. The exact port comes first: the reference and the trace tests are that parity check, and its replay harness grows out of the trace tests. The redesign described there happens afterwards, as refactoring guarded by the traces.

## Implementation steps
### 1. Reference builds
The Visual C++ project, the forced-include header and the portable math functions. The reference builds and plays like the normal build.

### 2. Function tests and the first ported files
The function-test driver. Then port the leaves: `etc3.cpp` (`rnd`, `my_rnd`, the math helpers), then `etc1.cpp` and `etc2.cpp`, each function with a function test. This can run in parallel with step 3.

### 3. Recording and replaying
The wrappers, the trace format, and the record, replay and script modes with the fake rival. A recorded trace replays exactly in the reference.

### 4. Trace tests
Port the rest of the source and the platform stand-ins, then add trace tests: state first, then the calls.

### 5. Desktop app
A window, drawing, sound and input, so the port can be played.

### 6. Two games connected
The port and the reference playing against each other over DirectPlay 8, using [Otsuki](../Otsuki) on the port's side. This tests the protocol code, `dplay.cpp`.

## Open questions
- Which library the desktop app uses: MonoGame directly, or [Saruhashi](../Saruhashi).
- Whether to port the map editor (`map_edit`) now or later.
- How the Win32 dialogs (chat, file selection, connection) look in the desktop app.
- How large the committed traces may get, and whether long ones should be stored with Git LFS.
