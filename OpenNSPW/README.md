# OpenNSPW
An exact port of the NSPW NET source ([Original/NSPW_NET](../Original/NSPW_NET)) to C#, tested against the original source. See [docs/Porting.md](../docs/Porting.md) for the design.

| Project | Description |
| --- | --- |
| `OpenNspw` | The C# port. One file per C++ file, plus stand-ins for the platform APIs it calls (Win32, DirectPlay 8, DirectSound), with the original APIs' names. |
| `OpenNspw.DirectPlay` | The DirectPlay 8 stand-ins implemented on [Otsuki](../Otsuki). |
| `OpenNspw.Reference` | The reference: the unmodified C++ source, compiled in place, plus the tools that record what the tests compare against. Windows only, and not part of `OpenNSPW.slnx`. |
| `OpenNspw.Tests` | Tests that replay the recordings through the port. They run on any platform. |

## Progress
| C++ file | Ported |
| --- | --- |
| `all_head.h`, `all_typedef.h` | All |
| `resource.h`, the dialogs of `NSPW_NET.RC` | All |
| `dplay.cpp` | All |
| `win_main.cpp`, `draw.cpp` | Globals, except the platform objects whose stand-ins do not exist yet |
| `demo.cpp` | Globals, `go_cnct_game_setting` |
| `etc2.cpp` | All except `new_unit_arrived`, `draw_line4` and `draw_line5` |
| `etc3.cpp` | `make_my_rnd`, `my_rnd`, `rnd`, `set_sprt_data`, `cloud_in_start`, `cloud_cont` |

Every ported function of the game logic has a function test. `dplay.cpp` is tested by `NetworkTests`: two games in one process connect over loopback UDP through the original's connection dialogs, and exchange the game's messages.

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

- **Windows and dialogs** (`winuser.cs`, `NSPW_NET_RC.cs`): `CreateDialog`, `SendDlgItemMessage`, `PostMessage`, `PeekMessage` and the rest keep the state of each dialog and a message queue per game. Nothing is drawn: the desktop app will draw the dialogs, and tests drive them with `ClickDlgItem`, `TypeDlgItemText` and `SelectDlgItem`.
- **DirectPlay 8** (`dplay8.cs`): the interfaces, structs and constants of `dplay8.h`. `OpenNspw.DirectPlay` implements them on Otsuki. Messages are delivered by `IDirectPlay8ThreadPool::DoWork`, on the game's thread, as in the original's DoWork mode.
- **Everything else the game needs from its environment** (`platform.cs`): `INspwPlatform` creates the COM objects, shows message boxes and opens files. Each game has its own, so that two games can run in one process.

## Porting conventions
The ported files read side by side with the C++ files: same order, names, comments, blank lines and formatting. They differ only where C# requires it:

| C++ | C# |
| --- | --- |
| `void f(void)` | `public void f()`. Every function is a public method of the partial class `Nspw`. |
| `if( x )`, `!x`, `a && x` with an integer `x` | `x!=0`, `x==0`, `a && x!=0` |
| `while(1)` | `while(true)` |
| `#if 0`, `#if 1` | `#if false`, `#if true` |
| `T a[N]` (global, field or local) | `ArrayN<T> a` (locals: `= default`) |
| `T *p` parameter, `p->x` | `ref T p`, `p.x` |
| Implicit narrowing (`short s = i;`, `int i = d;`) | Explicit cast (`(short)i`, `(int)(d)`) |
| `(a==b)` used as an integer | `(a==b ? 1 : 0)` |
| A local that can be read uninitialized (MSVC warnings C4700, C4701) | Initialized to zero, marked `/* C4701 */`. The tests avoid the inputs that read it. |
| A local that shadows a local of an enclosing scope | The outer one is renamed, with a comment |
| `#if SND_SW`, `#if CONN_DBG==0`, `#if LNGG_VER==0` | `#if SND_SW`, `#if !CONN_DBG`, `#if !LNGG_VER`: the switches that are 1 (`SND_SW`, `DBG_MODE`) are defined in the project |
| Pointers to network messages (`(_DP_FLAG*) pReceiveMsg->pReceiveData`, `(BYTE*) &dp_flag`) | Unsafe pointers, as in C++ |
| `GUID*`, `HWND`, COM interface pointers | `Guid?`, `HWND?`, `IDirectPlay8Peer?` and so on; `p->f` becomes `p.f` |
| `TCHAR s[N]` passed to a function | A `Span<byte>`: the stand-ins take char arrays as spans |
| `rand`, `srand`, `abs`, `sprintf`, `wsprintf`, `_tcslen`, ... | `crt.cs` and `stdio.cs`, with the MSVC CRT's results |
| `sin`, `cos`, `atan2` | `nspw_math.cs`, the same portable implementation as the reference's |
