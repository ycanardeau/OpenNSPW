# Refactoring
A proposal for refactoring the exact port ([OpenNSPW/OpenNspw](../OpenNSPW/OpenNspw)) into readable C#, step by step, without changing its behavior.

The concrete names and types are in [RefactoringCatalog.md](RefactoringCatalog.md). Improvements that go beyond an exact refactoring, such as removing the original's invalid states, are in [Improvements.md](Improvements.md).

This is a proposal, not a final design. [Open questions](#open-questions) lists what still needs to be decided.

## Background
The exact port reads side by side with the C++ source ([Porting.md](Porting.md#porting-rules)), so it is hard to read on its own:

```csharp
if( unit[m].ctgry==PLANE && unit[m].info[5]==RETURN && unit[m].info[3]==1 && unit[unit[m].info[1]].used!=0
	&& unit[unit[m].info[1]].info[7]==0 && !(unit[m].ctgry==PLANE && unit[m].info[0]==FLYING && unit[unit[m].info[1]].hp[0]<=unit[unit[m].info[1]].hp[1]*0.2) )
```

A refactoring of such a port easily stops behaving like the original, in three ways:

- **Nothing checks each step**, when behavior is only compared by playing by hand.
- **Helpers change the arithmetic.** A helper that converts degrees with `Math.PI / 180` instead of the original's `a_PI` (`0.01745329251994`), or calls `Math.Sin` instead of the original's `sin`, differs only in the last bits, but positions feed back into the next tick (see [Porting.md](Porting.md#math-functions)).
- **The state loses its layout.** Once `UNIT` becomes a class split into parts, and the tables become collections, the state can no longer be compared with the original's byte for byte, the original's files need a converter, and code that sees the state as memory, such as an emulator running the original's machine code, has to decode every field by hand.

## Summary
- **Behavior is preserved exactly.** Every refactoring step leaves the game state, the `rand()` sequence and everything the game outputs unchanged, for all inputs. Bugs found in the port are fixed in separate commits, never in a refactoring commit.
- **The state keeps the original's memory layout, permanently.** Every struct and global that the reference records (`Layout.json`) keeps its size and field offsets. New types must have the same layout as the primitives they replace. This keeps the [byte-for-byte tests](Porting.md#tests) working, keeps the original's save and scenario files readable without a converter, and lets the single-player mode map the emulator's memory onto the C# state with block copies (see [The emulator](#the-emulator)).
- **A safety net comes first.** Before the first refactoring, deterministic whole-game traces of the current port are recorded and committed. Every step must reproduce them bit for bit.
- **Readability comes from types and names, not from a new architecture**: enums instead of `int` constants, value types instead of pairs of `double`s, named accessors instead of `info[n]`, `bool`-like types instead of `BOOL`, C# names instead of abbreviations, `ref` locals instead of repeated `unit[m].`, and smaller functions.
- **One kind of change per commit**, done by a tool where possible, so each commit can be reviewed for the [hazards](#hazards) of that kind only.

## What "behavior" means
Two versions behave the same when, for the same input, they produce the same:

- **State**: the bytes of every global in `Layout.json`, at the end of every frame.
- **Random numbers**: the number and order of calls to `rand()` (`rnd`, `my_rnd`, `srand`).
- **Output**: drawing calls, text, sound, network messages (their bytes), files written, message boxes.

Everything else may change: local variables, function boundaries, names, the order of independent statements without side effects, and intermediate values that are not stored.

"For the same input" means all inputs, not only the recorded ones. The traces are evidence, not proof, so each kind of change also has rules that make it safe by construction (see [Kinds of change](#kinds-of-change)).

## Safety net
### What exists
- **Function tests** compare single functions of `etc2.cpp` and `etc3.cpp` with recordings of the reference, byte for byte.
- **Layout tests** check every struct and global against the reference's layout.
- **`TitleScreenTests`** and **`NetworkTests`** play whole games, but in real time on two threads, so they are not deterministic. They only check that the games reach a mode.

Most of the game logic (`chara_cont`, `etc1.cpp`, the rest of `etc3.cpp`, `cnct_game_cont.cpp`, `input.cpp`, the drawing) is not compared with anything yet.

### Characterization traces
Before refactoring, record what the current port does, and require every step to do the same. These traces come from the port, not from the reference: they freeze its behavior, including any porting bugs.

A **deterministic runner** plays two games, the host and the guest, in one test:

- **Time**: `timeGetTime` and `time` return values from a script, not the clock.
- **Network**: an in-memory DirectPlay 8 stand-in connects the two games. A message sent in a frame is delivered in the receiver's next `DoWork`, in the order sent.
- **Frames**: each game runs its `WinMain` on its own thread, as now, but `Present` (the end of a frame) blocks until the runner releases it. The runner advances the host and the guest one frame each, in turn, so the two games interleave the same way in every run.
- **Input**: keyboard and mouse input, and dialog actions, come from a script, keyed by frame.

At the end of each frame, the runner records a hash of the state (all globals of `Layout.json`, with the existing `Global` binding), a hash of the output calls, and the `rand()` state. Every 100 frames, and at the end, it records the full state, so that a mismatch can be shown as the first differing variable, with `GameState.Differences`.

Scripts:

- The title screen, the setting screens and the start of each scenario, as host and as guest, on each side.
- Each kind of order: moving, selecting, each combat menu entry, supply, reinforcements, landing, take-off and landing on carriers.
- Saving and resuming, the auto-save, and the end of a battle, with each result.
- The map editor.
- Random clicks and keys in a battle, for thousands of frames, from fixed seeds.

**Coverage** of the ported files under these scripts is measured, and reported per function. A function that no trace reaches is either given a script, or refactored only with the rules of its kind and a second reviewer.

### Rules
- Every refactoring commit passes all characterization traces, function tests and layout tests, unchanged.
- A trace is re-recorded only in a commit that changes behavior on purpose (a bug fix), which says what changed and why.
- The traces and the function tests are kept when the reference's trace tests ([Porting.md, step 4](Porting.md#4-trace-tests)) arrive: they test different things (the port against itself, and the port against the reference).

## The layout invariant
Every struct in `Layout.json` (`UNIT`, `FIRE`, `EFFECT`, `KUMO`, `SPRT`, `NEW_PP`, `NEW_SLCT`, `NEW_MENU` and the network messages) keeps its size and the offset and size of each field. Every global keeps its size. Only names and types change, and only to types with the same layout.

This rules out, for the state:

| Not allowed | Instead |
| --- | --- |
| `class Unit`, or a unit split into components | `struct Unit`, with named accessors for the parts (see [Union slots](#union-slots)) |
| `List<T>`, `Dictionary<int, T>`, `T[]` for the tables | The inline arrays (`Array256<Unit>`), used as `Span<T>` |
| `bool` (1 byte) for a `BOOL` (4 bytes) | `Bool32` |
| `string` for a `TCHAR[N]` | The byte array, with accessors that decode and encode it |
| Merging `pp_x[64]` and `pp_y[64]` into `WorldPosition[64]` | A view that reads `PathX[i]` and `PathY[i]` as a `WorldPosition` |
| Widening `short` to `int` | Keeping `short`, behind a typed accessor where it helps |

It also keeps the original's file formats: `save_on_resume`, `load_it2`, `load_it3` and `save_user_map` read and write the `unit`, `fire` and `effect` arrays as raw bytes (`ReadFile( hFile, ref unit, ...)`), so the original's scenario, map and saved files stay readable as they are.

### The emulator
The single-player mode runs code from the single-player executable on [Enzan](../Enzan): the CPU strategy reads and writes the game's state at the addresses of the single-player executable. Enzan's `Cpu` takes one flat `Memory<byte>` with a base address.

The single-player executable's structs are nearly the same as NSPW NET's. Its offsets, as known so far:

| | NSPW NET (`Layout.json`) | Single-player |
| --- | --- | --- |
| `UNIT` size | `0x5C0` | `0x598` |
| `used` to `em_flg`, `em_x`, ... `arm2` | `0x000` to `0x540` | The same offsets |
| `gas` | `double[8]` at `0x540` | `double[6]` at `0x540` (inferred) |
| `found`, `tech` | `0x580`, `0x584` | `0x570`, `0x574` (inferred) |
| `rnd_250` to `rnd_125` | `0x588` to `0x5A0` | None |
| `rnd_100` to `rnd_10` | `0x5A0` to `0x5C0` | `0x578` to `0x598` |
| `cmbt_map` | `ushort[256][256]` | The same, at `0x443AA0` |
| `SPRT` | 40 bytes | The same, at `0x48A200` |

The "inferred" rows follow from the sizes and must be confirmed against the disassembly.

With the layout kept, the C# state and the emulator's memory differ only in where each region is, and, for `UNIT`, in three ranges of each unit. Mapping is then mechanical:

- **Copy in and out**: before an emulated call, copy each region into the emulator's memory, and after it, copy it back: `cmbt_map` and `sprt` as one block each, and each unit as three blocks (`0x000`–`0x570`, then `found` and `tech`, then `rnd_100` to `rnd_10`). The regions are a table of address, size, global and field ranges, generated from `Layout.json` and the single-player layout. It is about 400 KB of `memcpy` per call, with no per-field code.
- **Zero copy**: if copying is measured to be too slow, Enzan's memory becomes an interface with a page table: each page of the emulated address space points at a C# region, through `MemoryMarshal.AsBytes` on the global. Everything except `unit` maps directly. `unit` needs an offset translation per access, from the same table.

Neither needs to know what `info[5]` means, so the C# side can rename and retype freely, as long as the layout holds. Without the layout, the mapping would need a case for each field, including one per meaning of each `info[]` slot, and would break whenever a field moved.

The single-player mode itself (its scenarios, its setting screen, which executable and functions) is designed separately. This document only guarantees that the state stays mappable.

## Kinds of change
Each kind is done across the whole project in its own commits, in the [order below](#implementation-steps). The examples use the names from the catalog.

### Name binding
The tests, the trace tools and the emulator's region table find globals and fields by their C++ names (`unit`, `pp_x`). Before anything is renamed, the binding moves to an attribute:

```csharp
[Original("unit")]
public Array256<Unit> Units;

[Original("cmbt_x", "cmbt_y")]	// One field that holds two globals, in order.
public WorldPosition CameraPosition;
```

- `Global.Create`, `TypeLayout`, `LayoutTests` and `GameState.Differences` resolve names through `[Original]`, and fall back to the member's name. A field with several names is split into slices, by the offsets of its type's fields.
- Struct fields carry it too (`[Original("x", "y")] public WorldPosition Position;`), so that layout tests keep comparing names, and mismatches are reported with both names.
- Methods carry it too (`[Original("chara_cont")]`), so that each C# function can be found from its C++ function, and back, after the exact port is gone. The function tests keep their C++ names (`FunctionTest.Run("etc2", "seek_parking_no", ...)`).

### Renaming
- **Globals, fields and functions** get C# names: PascalCase, whole English words, plurals for tables (`unit` → `Units`, `fire` → `Fires`, `effect` → `Effects`, `kumo` → `Clouds`, `sprt` → `Sprites`). The catalog lists them, with a glossary of the original's abbreviations.
- **One meaning per word.** The catalog's [terms](RefactoringCatalog.md#terms) fix what words such as world, map, minimap, battle area, tick, frame and turn mean, and every name uses them that way, even where the C++ uses one word for several things (`MAP_LEFT` is a bound of the world, `draw_map` draws the minimap, `cmbt_map` is the terrain).
- **State fields stay fields.** They are public fields in PascalCase, like the original's struct members. Auto-properties would add backing fields with generated names to the layout.
- **Locals** are renamed per function, when the function is refactored (`wrk_x` → `dx`, `dstc` → `distance`), not across the project.
- **Mechanics**: a Roslyn tool renames each symbol across the solution and adds `[Original]` with the old name. `OpenNspw.Porter` already uses Roslyn, so the tool is a new command of it (`rename`), driven by a table: the catalog, as a file.
- A rename commit contains renames only.

### Enums
`#define` constants that name the values of a field become enums, with the field's type as the underlying type, so that the layout does not change:

```csharp
public enum Side : short { None = 0, Japan = 1, UnitedStates = 2 }

public Side Side;	// UNIT.used
```

- The underlying type is that of the storage, not the constant. A value stored in fields of different widths (`used` is `short`, `fire.info[8]` is `int`, the network messages use `byte`) gets explicit conversions where it moves between them, exactly where the C++ converts.
- `0` gets a member where the code uses it (`Side.None` for "unused", `UnitKind.None`), so that `used!=0` becomes `IsUsed`, not a cast.
- Range comparisons (`kind>=AP && kind<=GF3`) stay comparisons, or become a property with the same comparison (`IsFacility`). Arithmetic on values (`kind-1` as an index into a table) becomes an explicit cast.
- Before converting, every write to the field is checked: a field that holds values outside the enum (a count, a unit number) is not an enum.
- Flags (`FRONT_BTN`, ...) become `[Flags]` enums.

### Value types
New value types replace groups of primitives that are adjacent in the struct, with the same order and alignment, so that the layout does not change:

| Type | Layout | Replaces |
| --- | --- | --- |
| `WorldPosition` | `double X, Y` | `x,y`, `em_x,em_y`, `x2,y2`, `last_x,last_y`, `gr_x,gr_y` |
| `Angle` | `double Degrees` | `drctn`, `to_ldr_drctn`, and angles in locals |
| `Point` | `int X, Y` | `POINT` |
| `Rect` | `int Left, Top, Right, Bottom` | `RECT` |
| `Bool32` | `int Value` | `BOOL` fields that are used as booleans |
| `UnitId` | `short Value` or `int Value` | Unit numbers (see below) |

Operations on them must compute exactly what the C++ computes, operation by operation:

- `Angle.Sin()` is `nspw_math.sin(Degrees * a_PI)`, because that is how the original writes it (`sin(unit[m].drctn*a_PI)` and `cos(drctn*a_PI)`, more than 100 times). An expression written differently, such as `sin((drctn+90)*a_PI)`, stays as it is, or gets its own helper. `a*b` may become `b*a` (IEEE multiplication is commutative), but nothing else is rearranged: no `(a+b)*c` for `a*c+b*c`, no `x*0.5` for `x/2`, no `Math.PI/180` for `a_PI`, no `Math.Sin` for `sin`.
- `WorldPosition - WorldPosition` is `new WorldVector(a.X - b.X, a.Y - b.Y)`, the same two subtractions. The original never uses `sqrt` for distances: about 40 places take `atan2(wrk_y,wrk_x)*RAD_to`, fold the angle into 0 to 90 degrees, and divide `wrk_x` by `cos(drctn*a_PI)`. `WorldVector.Length` is exactly that sequence, and replaces only the places that compute exactly that.
- Each value type has unit tests that check its results bit for bit against the inline expressions it replaces.

`Bool32` keeps the integer: `implicit operator bool` is `Value != 0`, and comparisons with `1` or `2` stay possible. A `BOOL` field is converted only when every read of it is a truth test, or the comparisons are kept. For example, `unit[m].info[6]` (submerged) is compared with `1` in one place and with `0` in others.

Unit numbers are stored as `short` (`the_slct_unit`, `ltl_ldr`), as `int` (`info[1]`, `arm[2]`, `fire.used`) and as `byte` (network messages). `UnitId` is exposed by accessors over that storage, never by changing its width:

```csharp
public UnitId Carrier { readonly get => new(info[1]); set => info[1] = value.Value; }
```

### Union slots
`info[16]`, `hp[8]`, `arm[8]` and `gas[8]` in `UNIT`, and `info[]` in `FIRE` and `EFFECT`, hold different things for different kinds. `info[0]` is the build time of a base and the state (flying or parked) of a plane. The storage stays, and gets named accessors, one per meaning:

```csharp
public Array16<int> info;

public int BuildTime { readonly get => info[0]; set => info[0] = value; }	// Bases.
public UnitState PlaneState { readonly get => (UnitState)info[0]; set => info[0] = (int)value; }	// Planes.
```

- The catalog gives each slot's meanings, from the original's comments and code. A slot whose meaning is not clear yet keeps a neutral name (`Info3`) until it is.
- Code that uses a slot for one meaning uses that accessor. Code that treats the array as a whole (clearing it, copying the struct) keeps doing so.
- A value written through one meaning and read through another stays possible, as in C++, and is commented where it happens (for example, a unit number reused for another kind with old values left in the slots).
- `[UnscopedRef] public ref int Ammo => ref arm[1];` is used where the slot is passed by `ref` or changed with `++`, `+=` through several levels.

### Tables and loops
The tables (`Units`, `Fires`, `Effects`, `Clouds`) stay inline arrays, indexed by number. The numbers are part of the state (planes refer to carriers, fires to targets), of the network messages (selections are sent as unit numbers) and of the files.

- **`ref` locals instead of repeated indexing**: `ref var unit = ref Units[m];`, then `unit.Position.X`. Never `var unit = Units[m];`, which copies the struct and loses writes. The `ref` is declared where `m` gets its value, and no longer used after `m` changes.
- **Loops keep their bounds and order.** `for( m=1; m<=max_unit; m++ )`, `m<=USA_PLANE_END` and `m<256` are different loops. A loop becomes `foreach (ref var unit in Units.AsSpan()[1..(MaxUnitId + 1)])` only if the bound is read once in the original too, which it is not when the body can add units: then the `for` stays.
- **Iteration helpers** (`EnumerateUsedUnits()`, `ShipsOf(side)`) are `ref`-returning span enumerators, ascending by number, never LINQ. LINQ is not used over game state at all: `Any`, `First` and `Where` stop early or run lazily, which changes how often a condition with `rnd()` runs, and they copy structs.
- **Unit 0** is the "no unit" sentinel, as in the original (`arm[2]==0` is "no target"). `UnitId.None` names it. Code that reads `Units[0]` on purpose keeps doing so.

### Strings
Text in the state, the files and the network messages stays Shift_JIS bytes in fixed arrays (`g_strLocalPlayerName`, `my_chat`, `_DP_DATA_1.my_name`). Accessors decode and encode them (`string LocalPlayerName { get; set; }`), with the same truncation as the C++ (by bytes, `MAX_PLAYER_NAME`, with the terminator). Text that is only drawn (`wsprintf` into a local, then `TextOut`) may become `string` interpolation, when it gives the same bytes; `wsprintf` formatting (`%d` of a `short`, `%3d`) is checked case by case.

### Functions
- **Return types**: functions that return `TRUE`/`FALSE` and are only tested return `bool`; functions that return a unit or fire number return `UnitId` or `FireId`, with the same "0 is none" meaning.
- **Extracting**: long functions are split into smaller ones, starting with `chara_cont` (about 2000 lines), `cnct_game_cont.cpp` and `etc1.cpp`. A block becomes a method only if it is entered and left in one place. Locals that carry values across the block become parameters, `ref` parameters or return values; locals that are read before being written (`/* C4701 */`) keep that behavior and comment. No statement moves across a call that can call `rnd()` or change state.
- **Splitting the partial class**: after the steps above, `Nspw` is split into classes by responsibility (the simulation, the screens and input, the network, drawing, sound), each holding its own globals. This is the point where the files stop being one file per C++ file. The globals stay fields with their layout; only their owner changes, and `[Original]` keeps them bound. The split follows the needs of [Multiplayer.md](Multiplayer.md#1-deterministic-simulation) (a simulation separated from drawing and input), and also gives the emulator one object to map.
- **Platform stand-ins** (`winuser.cs`, `ddraw.cs`, `dplay8.cs`, ...) keep the original APIs' names. The game code moves off them, to small interfaces of its own, only when the partial class is split.

### Clean-up
- **Commented-out code and `#if false` blocks** are removed, per file, in commits that contain nothing else. They stay readable in the exact port (see [The exact port](#the-exact-port)).
- **Integer truth tests** (`x!=0`, `(a==b ? 1 : 0)`) disappear as the types become `Bool32`, enums and `bool`; where an `int` really is a number, they stay.
- **`goto_<label>` flags** become structured code when the function around them is extracted.
- **Formatting and order**: once the partial class is split, the new files follow C# conventions and `CLAUDE.md`, including the dependency order of members. Before that, members are not reordered, so that diffs stay small.

## Hazards
Each item is a way a change that looks harmless changes behavior. Reviews of each kind of change check the items that apply to it.

1. **`rand()` calls.** A condition, argument or loop with `rnd()` or `my_rnd()` is never reordered, merged, split, short-circuited differently or made lazy. `a && rnd(3)==0` and `rnd(3)==0 && a` are different programs.
2. **Evaluation order.** C# evaluates left to right; the port fixed one order for every expression. Extracting a subexpression into a local before another call with side effects changes it.
3. **Floating-point arithmetic.** No rearranging, no other constants, no `Math.*` for `nspw_math`, no `float`, no `Math.FusedMultiplyAdd` (see [Value types](#value-types)).
4. **Integer widths.** `short` and `byte` arithmetic wraps, and casts truncate. Fields, locals and enums keep their widths, and casts stay where the port has them.
5. **`double` to `int`.** C# saturates where MSVC gives `0x80000000` ([Porting.md](Porting.md#differences-between-c-and-c)). The cases ported on purpose keep their comments.
6. **Struct copies.** `var u = Units[m]` and `foreach (var u in ...)` copy; writes to `u` are lost. Only `ref var`.
7. **Aliasing.** A `ref` local held while its index changes, or while the whole struct is overwritten (`unit[a]=unit[b]`), refers to a different unit than a fresh `unit[m]` would.
8. **Loop bounds.** Bounds evaluated once or each time, `<` or `<=`, starting at 0 or 1, and units added while iterating.
9. **Enums and `Bool32`.** Values outside the members, values other than 0 and 1, and arithmetic on values.
10. **Union slots.** Old values left in a slot by another kind are read in some paths. Clearing or splitting slots changes those paths.
11. **Uninitialized and out-of-bounds reads.** The `/* C4701 */` locals and the out-of-bounds accesses ported on purpose keep their behavior and comments.
12. **Bytes on the wire and on disk.** Network messages and files keep their bytes exactly. Checksums (`unit_chk`, `cc_chk`, `rnd_chk`) keep their computation, over the same values in the same order.
13. **Text bytes.** Shift_JIS encoding, truncation by bytes, and `wsprintf` formatting.
14. **Shared state.** Two games run in one process (the tests do it), so nothing becomes `static` and mutable: no static caches, no shared random generator.

## The exact port
The last commit of the exact port is tagged (`exact-port`). It stays the version that reads side by side with the C++ source, for finding where a refactored function came from (with `[Original]`) and for porting bugs found later.

Bugs found in the port after refactoring has started, for example by the reference's trace tests, are fixed in the refactored code. The reference's traces stay applicable, because the state keeps its layout and `[Original]` keeps the names bound.

## Implementation steps
### 1. Deterministic runner and characterization traces
The in-memory DirectPlay stand-in, the frame barrier, scripted time and input, the trace format and the scripts, with coverage per function. Tag `exact-port`.

### 2. Name binding
`[Original]` on globals, fields and methods, and the binding in the tests through it. No names change yet.

### 3. Refactoring tool
The `rename` command of `OpenNspw.Porter`, driven by the catalog as a file, adding `[Original]`.

### 4. Enums
`Side`, `UnitCategory`, `UnitKind`, `UnitState`, `UnitMode`, `CombatMenuItem`, `FireKind`, `EffectLayer`, `GameMode`, `GameResult`, `MessageType`, `SoundId`, the button flags. One enum per commit.

### 5. Value types
`WorldPosition`, `WorldVector`, `Angle`, `Point`, `Rect`, with their bit-for-bit tests. Then `Bool32` and `UnitId`, field by field.

### 6. Union slot accessors
`hp`, `arm` and `gas` first, whose meanings are clear, then `info[]` of `FIRE`, `EFFECT` and `UNIT`, slot by slot.

### 7. Renames
Globals and struct fields, then functions, with the catalog. The ported files' names follow their main function.

### 8. Functions
Per function, starting with the most covered: `ref` locals, local names, iteration helpers, return types, then extracting smaller functions. Commented-out code is removed per file before its functions are refactored.

### 9. Splitting the partial class
Into classes by responsibility, with C# conventions and `CLAUDE.md`. The emulator's region table is generated at this point at the latest.

Steps 4 to 6 make the largest difference to readability for the least risk, because the compiler checks most of them. Steps 8 and 9 carry the most risk, and depend most on the characterization traces.

## Relation to Porting.md and Multiplayer.md
[Porting.md](Porting.md#summary) says to refactor only after the port matches the reference. Most of the game logic has not been compared with the reference yet. Refactoring starts anyway, guarded by the port's own traces, because the layout invariant and `[Original]` keep the reference's trace tests possible on the refactored code. If they find differences later, the fix goes into the refactored code.

[Multiplayer.md](Multiplayer.md) needs a simulation separated from drawing and input. Step 9 provides it. Its redesign of the protocol is a change of behavior, and comes after this document.

## Open questions
- Whether the original's Japanese comments are kept as they are, translated, or kept with an English translation next to them.
- Whether the C++ abbreviations that are well known in the game's community (`BB1`, `CVL1`, `TPD`) are kept in enum members, or spelled out (`Battleship`, `LightCarrier`, `Torpedo`). The catalog proposes spelling them out, with the original in `[Original]`.
- How much the deterministic runner may change the stand-ins (`Present` blocking, scripted `timeGetTime`), and whether the desktop app should use it for replays.
- Whether to copy or to map the emulator's memory, which depends on how often the single-player executable's code is called per frame.
- Which single-player executable the emulator runs (1.22 or 2.00), and the full layout of its structs and globals.
