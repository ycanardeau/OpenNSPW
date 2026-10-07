# OpenNSPW
An exact port of the NSPW NET source ([Original/NSPW_NET](../Original/NSPW_NET)) to C#, tested against the original source. See [docs/Porting.md](../docs/Porting.md) for the design.

| Project | Description |
| --- | --- |
| `OpenNspw` | The C# port. One file per C++ file, plus stand-ins for the platform APIs it calls (Win32, DirectPlay 8, DirectSound), with the original APIs' names. |
| `OpenNspw.DirectPlay` | The DirectPlay 8 stand-ins implemented on [Otsuki](../Otsuki). |
| `OpenNspw.Desktop` | The game, on MonoGame: runs the port's `WinMain` and shows its frames and dialogs (with [Saruhashi](../Saruhashi)). |
| `OpenNspw.Porter` | Ports C++ files to the port's conventions, then fixes the compile errors that have a mechanical fix. |
| `OpenNspw.Reference` | The reference: the unmodified C++ source, compiled in place, plus the tools that record what the tests compare against. Windows only, and not part of `OpenNSPW.slnx`. |
| `OpenNspw.Tests` | Tests that replay the recordings through the port, and play whole games. They run on any platform. |

## Progress
Every C++ file is ported. The game runs from the connection dialogs through the title and setting screens into a battle.

What is tested:

- **Function tests**: `etc2.cpp` (except `new_unit_arrived`, `draw_line4` and `draw_line5`), and `make_my_rnd` through `cloud_cont` of `etc3.cpp`, against recordings of the reference.
- **`NetworkTests`**: `dplay.cpp`. Two games in one process connect over loopback UDP through the original's connection dialogs, and exchange the game's messages.
- **`TitleScreenTests`**: two whole games with the original's data connect, reach the title screen, and play a battle that the host starts by clicking through the setting screens. They check the modes, that both games keep in step, and that no message box shows. They save the frames they reach as PNG files next to the test assembly.

The rest of the game logic is not compared with the reference yet.

Run the commands below from this folder.

## Playing
```bash
dotnet run --project OpenNspw.Desktop
```

The game's data is `Original/NSPW_NET`, or the folder given with `--data <folder>`. Start two of them: one hosts (port `0` picks a free port, `2310` is the original's), and the other joins its address and port. Settings, such as the player name, are kept in `OpenNSPW/settings.json` under the user's application data folder.

## Porting a file
```bash
dotnet run --project OpenNspw.Porter -- port OpenNspw ../Original/NSPW_NET/<file>.cpp
```

This writes `OpenNspw/<file>.cs` and lists the errors left to fix by hand. `fix` instead of `port` runs only the second stage, on C# files of the project.

Run the commands below from this folder.

## Running the tests
```bash
dotnet test OpenNspw.Tests
```

## Recording
Recording needs Windows, Visual Studio 2022 with the **Desktop development with C++** workload, and the DirectX SDK (June 2006) (see [Original/DirectXSDK.md](../Original/DirectXSDK.md)).

```powershell
OpenNspw.Reference\record-functions.ps1
```

This builds the reference and writes to `OpenNspw.Tests`:

- `Layout.json`: the size and field offsets of each struct, the size of each global, and the initial values of the globals, as compiled.
- `Functions/<file>/<function>.jsonl.gz`: the recorded calls of each tested function (see `OpenNspw.Reference/function_tests.cpp`). The inputs are generated from a fixed seed, so recording again gives the same calls unless the reference changed.

To test a newly ported function, add its recording to `function_tests.cpp`, record, and add a test that calls the port's function to `OpenNspw.Tests`.

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
| `#if SND_SW`, `#if CONN_DBG==0`, `#if LNGG_VER==0` | `#if SND_SW`, `#if !CONN_DBG`, `#if !LNGG_VER`: the switches that are 1 (`SND_SW`, `DBG_MODE`) are defined in the project |
| Pointers to network messages (`(_DP_FLAG*) pReceiveMsg->pReceiveData`, `(BYTE*) &dp_flag`) | Unsafe pointers, as in C++ |
| `GUID*`, `HWND`, COM interface pointers | `Guid?`, `HWND?`, `IDirectPlay8Peer?` and so on; `p->f` becomes `p.f` |
| `TCHAR s[N]` passed to a function | A `Span<byte>`: the stand-ins take char arrays as spans |
| `rand`, `srand`, `abs`, `sprintf`, `wsprintf`, `_tcslen`, ... | `crt.cs` and `stdio.cs`, with the MSVC CRT's results |
| `sin`, `cos`, `atan2` | `nspw_math.cs`, the same portable implementation as the reference's |
