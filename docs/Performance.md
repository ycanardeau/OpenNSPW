# Performance
How the game keeps from stopping for the garbage collector, and how that is kept so. The garbage that a thread makes is what it allocates and lets go of; the collector stops the game to take it back. A game that makes little has short, rare pauses.

## Summary
- **The game's message loop never waits** while the game is active, as NSPW NET's does: about 14 million times a second, between frames. What it calls each time must allocate nothing.
- **A frame should allocate almost nothing**, on the game's thread and on the UI's, which paints about 60 to 75 times a second.
- **Measured** with a replay of a single-player battle at 1×, in a Release build: from about 1.3 GB a second to 129 KB a second, and from about 170 collections a second to about one a minute, each under 1 ms.
- **Guarded** by `AllocationTests`, which `dotnet test` runs on every pull request.
- **The native memory of a game**, which the collector sees none of, goes with the game: its surfaces and sounds free theirs once they are collected, which `NativeMemoryTests` checks ([Native memory](#native-memory)).

## Why the message loop matters
`WinMain` ends in the loop of NSPW NET ([win_main.cs](../OpenNSPW/OpenNspw/win_main.cs)). While the game is active and no dialog is up, nothing in it waits:

```csharp
if (PeekMessage(ref msg, PM_NOREMOVE) != 0)
	...                                    // a window message
else
{
	if ((IsAppActive || hwndChatDlg != null) && MainDialog == null)
		UpdateFrame();                     // returns at once until it is time for a frame
	else
		WaitMessage();

	DirectPlayThreadPool.DoWork(...);      // every time round
}
```

A frame of a single-player battle comes every 88 ms, on 1.22's timer; `UpdateFrame` returns at once until then. In between, the loop goes round as fast as the processor lets it. On Windows, each time round is a system call and a call into DirectPlay, about a microsecond. In the port, the stand-ins are a lock on an empty queue, the clock and an empty channel: about 70 ns, some 14 million times a second, in a single-player game too, which has a DirectPlay peer.

So 96 bytes a call there, a copy of the list of peers and a new delegate in `DoWork`, made 1.3 GB a second of garbage, and a collection about 170 times a second. The loop still keeps a core busy, as the original does; making it wait would be a difference from the original ([Differences.md](Differences.md)).

## The rules for code that runs every frame
- **No LINQ, closures or copies** (`Where`, `Select`, `Reverse`, `ToArray`, `ToList`, a lambda that captures): a loop by index, and the result kept in a list or array of the object's own.
- **A method given as a delegate is made into one once**, and kept in a field (`_dispatch ??= Dispatch`): naming a method where a delegate is taken makes a new delegate each time. A lambda that captures nothing is made once by the compiler; `static` makes sure it captures nothing.
- **A `foreach` over an interface** (`IEnumerable<T>`, `IReadOnlyList<T>`) boxes its enumerator; over a `List<T>` or an array it does not.
- **A text made from values is made again only when they change** (`LastText`), and a text that repeats is kept, in a cache that is emptied once it holds many, as the texts with numbers in them keep changing.
- **printf takes its numbers boxed** (`params ReadOnlySpan<object?>`), 24 bytes each; it writes nothing else but into its buffer.
- **Nothing is drawn that is not shown.**

## What keeps the garbage down
| Where | What | Instead of |
| --- | --- | --- |
| `DirectPlay8ThreadPool.DoWork`, `DirectPlay8Peer.DoWork` | The peers by index; the handler's delegate kept | A copy of the peers and a delegate at every call |
| `crt.Format` ([stdio.cs](../OpenNSPW/OpenNspw/stdio.cs)) | Writes into a buffer of the pool, then into the game's | A list, strings and an array for each conversion |
| `Gdi.Decode` ([wingdi.cs](../OpenNSPW/OpenNspw/wingdi.cs)) | The game's texts in Shift_JIS decoded once, looked up by their bytes without a copy | A string at each `TextOut` |
| `DesktopPlatform.RasterizeText` | Each text rasterized once; none on the screens that the UI draws in the game's place, where the game's frame is not shown (`HidesFrame`), as on the battle and the setting screens | A bitmap for every text of every frame, the original's debug lines (`DBG_MODE`) among them, which change every frame |
| `UICommands.InputSite` | The site's commands taken in place, into a list kept for it; the combat menu's items in a new array only when they change | LINQ and a list at each site, each frame |
| `ReplayPlayer.Take` | A frame's records taken in place; none, without a list, at a point that has none | LINQ and a list at each point |
| `SpriteSheetSurface.TryFind` | Its pieces in an array | An enumerator at each blit |
| `SolidBrush.Of`, `Pen.Of` (Saruhashi) | A brush and a pen a color | A new one for each fill and line |
| `Control.Draw` (Saruhashi) | The children by index; the graphics and its paint's arguments borrowed from the factory and given back (`IGraphicsFactory.Rent`, `Return`), which keeps them for the next paint | A copy of the children, a graphics, a scissor, a region and arguments for each control, each frame |
| `LastText`, `GameTexts.Of` (the app) | The UI's labels, and the English of the game's texts, made again only when their values change | Strings at each paint |

## How it is guarded
`AllocationTests` ([AllocationTests.cs](../OpenNSPW/OpenNspw.Tests/AllocationTests.cs)) counts what a thread allocates (`GC.GetAllocatedBytesForCurrentThread`):

- **A battle's frame**, on the game's thread, from its start to its presentation, in 1.22's first scenario, from the sheet and from the sprite files: at most 2,500 bytes. It is about 1,650, most of it the boxed numbers of the debug lines; it was about 14,000 before.
- **`DoWork`**, **a text that the app rasterizes again** and **printf** (`PrintfTests`) allocate nothing.

What is expected to allocate nothing is measured once the code is warm: the least of several rounds, as the runtime's tiered compilation may allocate on the calling thread as it compiles the code again. The game of these tests does not hash its output (`LockstepGame.RecordsOutput`), which makes garbage of its own.

The tests do not cover the UI's painting, which needs a window. Measure it in the app.

## Native memory
Some of the stand-ins hold memory that the game reaches by pointers, which must not move, so it is native, outside the collector's heap: the collector sees none of it, and frees none of it by itself.

| Memory | Held by | Freed |
| --- | --- | --- |
| A surface's pixels, which `Lock` points to | `DirectDrawSurface7` ([ddraw.cs](../OpenNSPW/OpenNspw/ddraw.cs)) | When the surface is collected. `Release` frees them and gives the surface new ones, which it holds until then. |
| A sound's data, which `Lock` points to | `SoundData` ([dsound.cs](../OpenNSPW/OpenNspw/dsound.cs)), shared by a buffer and its duplicates | When the last of them is collected |
| An mmio file, read whole | `mmioOpen` ([mmsystem.cs](../OpenNSPW/OpenNspw/mmsystem.cs)) | By `mmioClose`, which `LoadWave` calls once it has read the file |
| What the game takes with `malloc` | The game | By its `free`, as in the original |

The game releases none of its surfaces and sounds: the original's process ends with it, and the system takes back what it held. The port's games end in a process that goes on, as the tests' do, and a game stopped before its end, as a test stops one, releases nothing either. So a surface and a sound free their memory when the collector takes them, in their finalizers, once nothing of the game holds them. Before, each game left about 11 MB behind, the sprite sheet's pixels and the sounds' data, and a process that played thousands grew by tens of gigabytes.

Each block goes through `NativeAllocations` ([NativeAllocations.cs](../OpenNSPW/OpenNspw/NativeAllocations.cs)), which counts the bytes the process holds, and tells the collector of them (`GC.AddMemoryPressure`), so that it collects the objects that hold them soon enough, as their own size would not make it.

`NativeMemoryTests` ([NativeMemoryTests.cs](../OpenNSPW/OpenNspw.Tests/NativeMemoryTests.cs)) plays some frames of a battle, lets the game go, and collects it: the count must be back where it was. It runs alone, after the other tests, as the count is the process's. Memory that a new stand-in allocates natively should go through `NativeAllocations` too, so that the test counts it.

## Measuring the app
A listener of the runtime's events in the app's process (`EventListener`, the source `Microsoft-Windows-DotNETRuntime`, the keyword GC at the level Verbose) sees each collection and its pause (`GCSuspendEEBegin`, `GCRestartEEEnd`), and a sample about every 100 KB allocated, with its type and its thread (`GCAllocationTick`): enough to find what makes the most, by type, on each thread. `GC.GetTotalAllocatedBytes` gives the rate.

- Measure in a Release build, after the game has loaded, with a replay at 1× (`--replay`), which plays the same battle each time.
- **Keep the display on.** While it sleeps, macOS blocks the window's swap of its buffers, and the app stops drawing: what it then allocates is not what it allocates as it plays. Count the frames drawn to tell.
- Remove the listener after: it is not part of the app.

| Step | Garbage a second | Collections |
| --- | --- | --- |
| Before | About 1.3 GB | About 170 a second |
| `DoWork` | 2.1 MB | One every 3 to 4 s |
| The game's thread: printf, texts, sites, records | 942 KB | |
| Saruhashi: brushes, pens, children | 339 KB | |
| The app's labels and translations | 345 KB | |
| Saruhashi's graphics borrowed | 129 KB | About one a minute, under 1 ms |

What is left is about 40 KB a second of strings, and closures of the UI's input.
