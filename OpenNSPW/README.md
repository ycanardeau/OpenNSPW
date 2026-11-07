# OpenNSPW
An exact port of the NSPW NET source ([Original/NSPW_NET](../Original/NSPW_NET)) to C#, tested against the original source. See [docs/Porting.md](../docs/Porting.md) for the design.

| Project | Description |
| --- | --- |
| `OpenNspw` | The C# port. One file per C++ file, plus stand-ins for the platform APIs it calls (Win32, DirectPlay 8, DirectSound), with the original APIs' names. |
| `OpenNspw.Benchmarks` | Benchmarks of the game's own work in a battle (simulation and drawing), and of copying the state for the emulator. |
| `OpenNspw.DirectPlay` | The DirectPlay 8 stand-ins implemented on [Otsuki](../Otsuki). |
| `OpenNspw.App` | The game, on MonoGame of no platform in particular: its menus (with [Saruhashi](../Saruhashi), see [docs/UI.md](../docs/UI.md)), which run the port's `WinMain` and show its frames and dialogs, and the original's files, sprites and fonts that it embeds. |
| `OpenNspw.Desktop` | The app on Windows, macOS and Linux: starts `OpenNspw.App` on MonoGame's DesktopGL, with the command line's options. |
| `OpenNspw.Android` | The app on Android: starts `OpenNspw.App` on MonoGame's Android, played by touch. Not part of `OpenNSPW.slnx`, as it needs the `android` workload, which the CI's build does not have. |
| `OpenNspw.Replay` | Replays of battles, recorded and played as an extension of the game. See [docs/Replays.md](../docs/Replays.md). |
| `OpenNspw.HostUI` | The commands that the app's own UI gives the battle in place of the mouse's clicks, and what the UI reads of the game, as an extension of the game. See [docs/UI.md](../docs/UI.md). |
| `OpenNspw.Pacing` | Steady speed: a single-player battle that plays the same at every speed, as an extension of the game. See [docs/SteadySpeed.md](../docs/SteadySpeed.md). |
| `OpenNspw.Verification` | Traces of NSPW 1.22: their format, the replay that the port follows and checks itself against, and the recorder of the port, as extensions of the game. See [docs/Verification.md](../docs/Verification.md). |
| `OpenNspw.Porter` | Ports C++ files to the port's conventions, then fixes the compile errors that have a mechanical fix. |
| `OpenNspw.Singleplayer` | The emulator of the single-player executable (NSPW 1.22) on [Enzan](../Enzan), and its functions translated by the [Disassembler](../Disassembler). See [docs/Singleplayer.md](../docs/Singleplayer.md). |
| `OpenNspw.Recorder122` | Records traces of NSPW 1.22: runs `NSPW_122.exe` under the debugger, and records each battle played in it. Windows only. See [docs/Verification.md](../docs/Verification.md). |
| `OpenNspw.Reference` | The reference: the unmodified C++ source, compiled in place, plus the tools that record what the tests compare against. Windows only, and not part of `OpenNSPW.slnx`. |
| `OpenNspw.Tests` | Tests that replay the recordings through the port, and play whole games. They run on any platform. |

## Progress
Every C++ file is ported. The game runs from the connection dialogs through the title and setting screens into a battle.

What is tested:

- **Function tests**: `etc2.cpp` (except `new_unit_arrived`, `draw_line4` and `draw_line5`), and `make_my_rnd` through `cloud_cont` of `etc3.cpp`, against recordings of the reference. The tests of the transports' landing (`set_pos_of_attack_TR1`, the transports' branch of `fire_now`, and the landing craft of `cont_fire`) are written, and skipped until their calls are recorded.
- **`NetworkTests`**: `dplay.cpp`. Two games in one process connect over loopback UDP through the original's connection dialogs, and exchange the game's messages.
- **`TitleScreenTests`**: two whole games with the original's data connect, reach the title screen, and play a battle that the host starts by clicking through the setting screens. They check the modes, that both games keep in step, and that no message box shows. They save the frames they reach as PNG files next to the test assembly.
- **`CharacterizationTests`**: the characterization traces that guard the refactoring (see [docs/Refactoring.md](../docs/Refactoring.md#characterization-traces)). Scripts play two whole games in lockstep: the title screen, the first three scenarios, and the original's user scenarios `tst3.dat` and `てすと.dat`, whose fleets start close enough to fight, with seeded random input on both sides, and the host's transports landing their cargo in ミッドウェイ攻略１. Each script's trace, the state, output and `rand()` state of both games after every round, must match the one in `OpenNspw.Tests/Traces`, which was recorded from the port itself, and neither game may detect a desync.
- **`ReplayTests`** and **`ExtensionTests`**: replays of single-player battles and of both peers of a network battle, each played again alone with the same hash every frame, and what the extension API gives an extension ([docs/Replays.md](../docs/Replays.md), [docs/Extensions.md](../docs/Extensions.md)).
- **`SteadySpeedTests`** and **`BattleOptionsTests`**: a single-player battle plays the same at every speed, paused or not, shown or not, and OpenNSPW's own options for it ([docs/SteadySpeed.md](../docs/SteadySpeed.md), [docs/Options.md](../docs/Options.md)).
- **`AllocationTests`** and **`PrintfTests`**: the garbage that a battle's frame, DirectPlay's `DoWork`, the app's text and printf make, within budgets, so that a change that makes much more fails before it is merged ([docs/Performance.md](../docs/Performance.md)).
- **`NativeMemoryTests`**: a game let go of gives back the native memory of its surfaces and sounds once it is collected, so that a process that plays many games does not grow with each ([docs/Performance.md](../docs/Performance.md#native-memory)).
- **`EmulatorTests`** and **`SingleplayerTests`**: the single-player mode. Every scenario of NSPW 1.22 sets up and plays against the computer, which gives orders to its ships, without the emulator. **`ScenarioSetupTests`**, **`ComputerPlayerTests`** and **`SharedFunctionTests`** compare 1.22's functions rewritten as C# with the originals, run in the emulator.

The rest of the game logic is not compared with the reference yet.

The known bugs, of the port and of the original, and the limits of these tests are in [docs/KnownIssues.md](../docs/KnownIssues.md).

Run the commands below from this folder.

## Playing
```bash
dotnet run --project OpenNspw.Desktop
```

The game plays the original's files that the app embeds, and keeps what it writes, such as its saves, in `OpenNSPW/Files` under the user's application data folder (see [docs/BundledData.md](../docs/BundledData.md)); with `--data <folder>`, it plays that folder instead, such as `../Original/NSPW_NET`. The game starts at its main menu, and returns to it when a game ends.

- **Multiplayer**: start two of them. One hosts (port `0` picks a free port, `2310` is the original's), and the other joins its address and port. **Scenario Editor** opens the original's map editor. Settings, such as the player name, are kept in `OpenNSPW/settings.json` under the user's application data folder.
- **Single Player**: a scenario of NSPW 1.22 against the computer (see [docs/Singleplayer.md](../docs/Singleplayer.md)), chosen from its scenarios. Its scenarios, computer and victory conditions are rewritten as C#, so it needs no `NSPW_122.exe`; it plays with 1.22's maps and sounds that the app embeds.

To start a single-player scenario without the menus:

```bash
dotnet run --project OpenNspw.Desktop -- --singleplayer 3
```

### On Android
`OpenNspw.Android` needs the `android` workload of the .NET SDK that builds it (`dotnet workload install android`, from an administrator's terminal on Windows), JDK 17 and the Android SDK, with platform 36. With an emulator or a device connected:

```bash
dotnet build OpenNspw.Android -t:Run -p:AndroidSdkDirectory=<Android SDK> -p:JavaSdkDirectory=<JDK 17>
```

It fills the screen in landscape, as a desktop's wide window does: the battle's command bar and minimap in a column at the right, and the rest of the sidebar in a panel that **Panel** shows. A tap is a click, a drag pans the battlefield, two fingers zoom it, and two fingers lifted without zooming deselect; the command bar gives what only the keyboard does, such as the speed (see [docs/UI.md](../docs/UI.md#differences-from-the-originals-screens)). Once a touch is lifted, the game's cursor rests away from the screen's edges, which would scroll the map. Back is Escape. Touching a field to write in, such as a host's address, opens Android's text box, which fills the field once it is confirmed. What the game writes, such as the settings, the saves and the replays, is in the app's own folder (`files/.config/OpenNSPW`).

The text is drawn in fonts that come with the game, so that it looks the same on every platform: BIZ UDGothic for the game's own text and BIZ UDPGothic for the menus, under the SIL Open Font License 1.1 ([OpenNspw.App/Fonts/OFL.txt](OpenNspw.App/Fonts/OFL.txt)).

The battle's pictures and the title are the original's sprites, cut from its sprite sheet into a file each in `OpenNspw.App/Sprites` (see [docs/SpriteSheets.md](../docs/SpriteSheets.md)).

## Porting a file
```bash
dotnet run --project OpenNspw.Porter -- port OpenNspw ../Original/NSPW_NET/<file>.cpp
```

This writes `OpenNspw/<file>.cs` and lists the errors left to fix by hand. `fix` instead of `port` runs only the second stage, on C# files of the project.

Run the commands below from this folder.

## Running the tests
```bash
dotnet test OpenNspw.Tests -c Release
```

In Release, as CI runs them: the tests run whole games and the emulated 1.22 for many frames, about 3.5 times as fast as in Debug.

### Characterization traces
The characterization tests run two games in lockstep (`OpenNspw.Tests/Lockstep`): each game runs only while the test steps it, until it ends a frame, sleeps or waits for a message; its clock advances only as it reads it; the two games connect through an in-memory DirectPlay 8; and input arrives only between steps. A run therefore does the same thing every time, on every machine.

A change that keeps behavior leaves every trace unchanged. A change that changes behavior on purpose records the traces again, in the same commit:

```bash
OPENNSPW_RECORD_TRACES=1 dotnet test OpenNspw.Tests --filter CharacterizationTests
```

When a run differs from its trace, the test reports the first line that differs and the globals that differ at the next checkpoint.

## Benchmarks
```bash
dotnet run -c Release --project OpenNspw.Benchmarks -- --filter '*'
```

`BattleBenchmarks` measures the game alone, without a network: one game plays against a rival played in the same process (`ScriptedRival`), which answers each turn as a peer with the same simulation. The game starts through its dialogs, title screen and setting screens, with the original's data, into the first scenario. Both sides then send their ships and planes at each other. The benchmarks start from tick 2000, where the air battle begins:

| Benchmark | What one operation is |
| --- | --- |
| `Tick` | A tick of the simulation (`UpdateBattle` and `CheckResult`), over 1500 ticks. |
| `Frame` | A frame at the normal speed (`UpdateFrame`): input, a tick, drawing the battle and showing it, over 300 frames. |
| `Draw` | Drawing the battle (`UpdateUnitInfo`, `DrawBattleArea` and `DrawMinimap`), without the tick. |

Each iteration restores the game's state to tick 2000 first, so each one does the same work. The state an iteration ends with is printed (`// State after an iteration`). It is the same in every iteration and every run, and a version of the game that behaves the same ends with the same state.

`EmulatorCopyBenchmarks` measures copying the state into and out of the emulator's memory around a call into the single-player executable, as in [Refactoring.md](../docs/Refactoring.md#the-emulator). Compare it with `Tick` to see what the copy costs per call.

To compare two versions, such as before and after a refactoring, run the benchmarks in a worktree of each and compare the tables. The state hashes must match; if they do not, the two versions did not do the same work.

```bash
git worktree add ../OpenNSPW-before <commit>
```

## Recording
Recording needs Windows, Visual Studio 2022 with the **Desktop development with C++** workload, and the DirectX SDK (June 2006) (see [Original/DirectXSDK.md](../Original/DirectXSDK.md)).

```powershell
OpenNspw.Reference\record-functions.ps1
```

This builds the reference and writes to `OpenNspw.Tests`:

- `Layout.json`: the size and field offsets of each struct, the size of each global, and the initial values of the globals, as compiled.
- `Functions/<file>/<function>.jsonl.gz`: the recorded calls of each tested function (see `OpenNspw.Reference/function_tests.cpp`). The inputs are generated from a fixed seed, so recording again gives the same calls unless the reference changed.

To test a newly ported function, add its recording to `function_tests.cpp`, record, and add a test that calls the port's function to `OpenNspw.Tests`. A test whose calls are not recorded yet is skipped with `FunctionTest.NotRecorded`; remove the `Skip` once they are.

The reference also runs the game, from `Original/NSPW_NET` as the working directory (set as the debugger's working directory in the project). For now it runs fullscreen, like the normal build.

## Platform stand-ins
The ported code calls the platform through stand-ins with the original APIs' names and signatures, so its call sites stay as they are:

- **Windows and dialogs** (`winuser.cs`, `NSPW_NET_RC.cs`): `CreateDialog`, `SendDlgItemMessage`, `PostMessage`, `PeekMessage` and the rest keep the state of each dialog and a message queue per game. The stand-ins draw nothing: the desktop app draws the dialogs, and the user's actions on them, like the tests', go through `ClickDlgItem`, `TypeDlgItemText` and `SelectDlgItem` on the game's thread (`PostToGame`).
- **DirectPlay 8** (`dplay8.cs`): the interfaces, structs and constants of `dplay8.h`. `OpenNspw.DirectPlay` implements them on Otsuki. Messages are delivered by `IDirectPlay8ThreadPool::DoWork`, on the game's thread, as in the original's DoWork mode.
- **DirectDraw and GDI** (`ddraw.cs`, `wingdi.cs`): surfaces are RGB565 pixels in memory, and `Blt`, `BltFast`, `Lock` and `GetDC` work on them. Text is rasterized by the platform. A flip or a blit to the primary surface hands the frame to the platform, which shows it in a window, so the game never runs fullscreen.
- **DirectInput** (`dinput.cs`): buffered keyboard and mouse devices, fed by `PostKeyboardInput` and `PostMouseInput`.
- **DirectSound and DirectMusic** (`dsound.cs`, `unknwn.cs`): sound buffers in memory, played by the platform. DirectMusic is initialized but plays nothing, as in the original, whose music code is excluded with `#if 0`.
- **Files, the registry and the clock** (`fileapi.cs`, `mmsystem.cs`, `winreg.cs`): files of the game's folder, settings and `timeGetTime`, through the platform.
- **Everything else the game needs from its environment** (`platform.cs`): `INspwPlatform` creates the COM objects, shows message boxes and frames, rasterizes text, plays sounds, keeps settings and opens files. Each game has its own, so that two games can run in one process.

## Porting conventions
The ported files read side by side with the C++ files: same order, names, comments, blank lines and formatting. They differ only where C# requires it:

| C++ | C# |
| --- | --- |
| `void f(void)` | `public void f()`. Every function is a public method of the partial class `Nspw`. |
| `if( x )`, `!x`, `a && x` with an integer `x` | `x!=0`, `x==0`, `a && x!=0` |
| `while(1)` | `while(true)` |
| `#if 0`, `#if 1` | `#if false`, `#if true` |
| `T a[N]` (global, field or local) | `ArrayN<T> a` (locals: `= default`) |
| `T *p` parameter, `p->x` | `ref T p`, `p.x` in the files ported by hand; `T* p`, `p->x` in the files from the porter, which pass `ref *p` where a function takes `ref T` |
| Implicit narrowing (`short s = i;`, `int i = d;`) | Explicit cast (`(short)i`, `(int)(d)`) |
| Narrowing compound assignment (`i *= 1.35;`, `s -= i;`) | `i=(int)(i * 1.35);`, `s=(short)(s - i);` |
| A `#define` in a `.cpp` file | A `public const` |
| `goto` into a block | A flag, `goto_<label>`, that enters the block and skips to the label |
| Direct3D declarations, which the game does not use | Commented out |
| `(a==b)` used as an integer | `(a==b ? 1 : 0)` |
| A local that can be read uninitialized (MSVC warnings C4700, C4701) | Initialized to zero, marked `/* C4701 */`. The tests avoid the inputs that read it. |
| A local that shadows a local of an enclosing scope | The outer one is renamed, with a comment |
| `#if NSPW_THE_NET`, `#if SND_SW`, `#if CONN_DBG==0`, `#if LNGG_VER==0` | `#if NSPW_THE_NET`, `#if SND_SW`, `#if !CONN_DBG`, `#if !LNGG_VER`: the switches that are 1 (`NSPW_THE_NET`, `SND_SW`, `DBG_MODE`) are defined in the project |
| Pointers to network messages (`(_DP_FLAG*) pReceiveMsg->pReceiveData`, `(BYTE*) &dp_flag`) | Unsafe pointers, as in C++ |
| `GUID*`, `HWND`, COM interface pointers | `Guid?`, `HWND?`, `IDirectPlay8Peer?` and so on; `p->f` becomes `p.f` |
| `TCHAR s[N]` passed to a function | A `Span<byte>`: the stand-ins take char arrays as spans |
| `rand`, `srand`, `abs`, `sprintf`, `wsprintf`, `_tcslen`, ... | `crt.cs` and `stdio.cs`, with the MSVC CRT's results |
| `sin`, `cos`, `atan2` | `nspw_math.cs`, the same portable implementation as the reference's |
