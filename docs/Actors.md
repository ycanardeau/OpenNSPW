# Actors
A design: every thread of OpenNSPW and Otsuki is an actor. An actor owns its state, and the others reach it only by posting messages to its mailbox. No state is shared, so nothing is locked.

## The rule
- **Threads talk only by messages.** No thread reads or writes another's state. What one needs of another's, the other sends.
- **No locks:** no `lock`, `Monitor`, `System.Threading.Lock`, `ReaderWriterLockSlim`, no semaphore or event used to guard or hand over state, nor `System.Threading.Channels`, whose writers lock inside.
- **The mailbox is the only concurrent code.** It is built on `ConcurrentQueue<T>`, `Interlocked` and one signal for an actor that sleeps; nothing else uses them but its three siblings, `Latest<T>`, `Sequence` and `Tally` (below).
- **A message is immutable,** or handed over whole: a buffer sent is the receiver's until it is sent back.
- **Ownership may move, by message:** a thread that hands its state to another, as a benchmark's game hands its own to the benchmark once parked, posts it and touches it no more until it is posted back.

## The mailbox
One type, `Mailbox<T>`, in a project of Otsuki of its own, `Aigamo.Otsuki.Actors`, with `Actor<T>`: Otsuki and the game reference it, and the game does not reference the rest of Otsuki. The project has three more types, for what is not a queue of messages:

- **`Latest<T>`:** the newest of an immutable value that one actor posts and any thread reads, such as the session's snapshot of its players, a replay's progress, or a replay's view as the viewer sets it. A message that only the newest of matters, which no one changes.
- **`Sequence`:** numbers drawn from any thread, each once, such as the peer's send handles, which `SendTo` returns at once.
- **`Tally`:** a count that any thread adds to and reads, such as the bytes of native memory the stand-ins hold (`NativeAllocations`), which the game's threads allocate and the finalizers free.

- **Post** enqueues on a `ConcurrentQueue<T>`, from any thread. A message that is a struct allocates nothing.
- **Receive** takes the messages in order, on the owner's thread only.
- **Three ways to run:**
  - **Pooled** (`Actor<T>`): the actor runs on the thread pool while it has messages. `Post` schedules it if it is idle, a word swapped from 0 to 1. It handles messages until its queue is empty, sets the word back to 0, and looks at the queue once more, so a message posted between the two is not left. `Receive` is synchronous, so two messages never interleave. `Actor<T>` keeps what it has now: `Start`, `Stop` (messages already in the mailbox are still handled), `Post` returning false once stopped, `Completion` and `OnError`.
  - **Thread-bound**: an actor with a thread of its own, such as the game's or the UI's, drains its mailbox at points of its own choosing, and sleeps on the mailbox's signal when it has nothing to do. The signal is set after a message is enqueued, and the sleeper looks at the queue again after waking, so no message is missed.
  - **Awaited**: an async reader, such as the RFCOMM stream's `ReadAsync` or the host enumerator, awaits the next message (`WaitToReceiveAsync`, `ReceiveAsync`, `ReadAllAsync`, with a cancellation token), in place of a channel's.
- **Selective receive:** a thread-bound actor may take one kind of message at one point and another kind at another (`TryReceive<TKind>`, `TryPeek<TKind>`, or with a predicate), keeping each kind's order, as the game takes each input where the original takes it. The owner moves what the shared queue holds, as it passes over it, into a list of its own, which only it touches. Taking every message of a kind posted so far gives what a field that another thread writes gives now when it is read.
- **Waiting for some kinds:** a thread-bound actor sleeps until a message that it waits for is there (`WaitFor(predicate)`, with a timeout or none). A message of another kind may wake it, to be taken by a handler that changes nothing the game sees, such as a returned frame buffer, and it sleeps again.
- **Bounded, where it must be:** a mailbox may drop what is posted beyond a bound, as a link drops datagrams now (256 waiting).
- **Async reply:** a message may carry a `TaskCompletionSource` for its answer, as Otsuki's commands do now.

## The actors
| Actor | Runs | Owns | Receives |
| --- | --- | --- | --- |
| **Game** | Thread-bound: the game's thread (`WinMain`), its mailbox `Nspw.Mailbox` (`GameMessage`) | Everything of the port: the battle, the windows, the keys, the cursor, DirectInput's buffers, the settings, the sounds, the extensions' state (the host UI's commands, a replay's player and recorder, the view the game took of a replay, a trace of 1.22) | Each kind where the port takes that input (below) |
| **UI** | Thread-bound: MonoGame's thread; `GameSession.TakeMessages` drains it at the start of the game screens' `Update` | The screens, and its own copies of what the game sends: the windows' view, the newest frame and its snapshot, the last message box, whether the game has ended; a replay's view as the viewer sets it | `FrameReady(buffer)`, keeping the newest and sending the older back; `WindowsChanged(view)`; `MessageShown`; `DialogFilled`; `Ended` |
| **Session**, **ReliableEndpoint**, **Connection** (Otsuki) | Pooled, as now | As now | As now |
| **Links** (Otsuki, `StreamDatagramTransport`) | Pooled | The links by endpoint: creating, accepting and removing them | `Start`, `Send`, `Accepted`, `Ended`, `Dispose` |
| **Link's writer** (Otsuki) | Awaited, by its stream's writer; bounded | The datagrams waiting for its stream | Datagrams to write, from the links' actor |
| **MacBluetoothConnector**, **WindowsBluetoothConnector** (Otsuki) | Pooled | Their streams, requests and listener | The native callbacks' `Accepted`, `Opened`, `Received` and `Closed`; `Start`, `Connect`, `Cancel`, `Write`, `Close`, `Dispose` |
| **RFCOMM stream** (Otsuki) | Awaited, by its reader | What it has received | Data and its end, from the connector |
| **Host enumerator** (Otsuki) | Awaited, by the enumeration | The queries sent | The responses, from the transport |
| **Recorder122** | Thread-bound: the debugger's thread | The trace | Breakpoints, and `Close`, which Ctrl+C posts |
| **Lockstep scheduler** (tests) | Thread-bound: the driver's thread and each game's | Which game runs | `Yielded`, from a game to the driver; whether to stop, from the driver to a game |
| **In-memory networks** (Otsuki's tests) | Pooled | Their transports, partitions, random, delays and counters | `Bind`, `Start`, `Send`, `Deliver`, `Unbind`, settings, and questions (`Ask`) |
| Benchmarks, other test helpers | As the threads they have | Their queues, frames, events and counters | Mailboxes in place of their locked lists; questions asked on the game's thread |

### The game's points of receive
The port takes each input at a point of its frame (docs/UI.md), and the game actor receives each kind of message there, in order. Only the stand-ins that face the host change (`winuser.cs`, `fileapi.cs`, `dinput.cs`, the platform) and the extensions of the host (the host UI's commands, a replay, a trace of 1.22); the ported files do not.

| Kind | Taken in | Now |
| --- | --- | --- |
| Window messages, and the user's actions (`PostToGame`) | `PeekMessage` (with `PM_NOREMOVE`, a peek), `GetMessage`, `WaitMessage` | `_messages`, under its lock and `Monitor.PulseAll` |
| The UI's commands, by site | `UICommands.InputSite(site)` (OpenNspw.HostUI), which still drops the other screens' sites | `_queued`, under `UICommands`' lock |
| DirectInput's key and button events | `GetDeviceData`, in `ReadInput`; dropped unless the device is acquired, and those waiting as it is acquired or unacquired are dropped, as `Post` and `Unacquire` drop them now | A `ConcurrentQueue`, and `Acquired` and the sequence number read and written by the UI's thread |
| The keys' state | `GetKeyboardState`, in `DrawBattleArea`, and in the message loops | `_keyState`, under its lock |
| The cursor | `GetCursorPos`, in `ReadInput`, and in the message loops: the last one | `_cursor`, a struct written by the UI's thread with no lock |
| A replay's controls (speed, pause, step, seek, take over) | The player's `StartsFrame`, from the player's own mailbox | Fields of `ReplayPlayer` written by the UI's thread with no lock |
| A replay's view (follow, see all, move) | `ReplayViewExtension.StartsFrame`, from the view's `Latest`, which captures the frame again when it changed | `ReplayView`, under its lock, read as the game copies its snapshot |
| `BufferFree` | Before the game fills a frame (`Present`, `CaptureAgain`), and where it sleeps (below) | The frame's lock |
| DirectPlay's events | `DoWork`, from the peer's own mailbox | Otsuki's channel of events, read by `DoWork` |
| `Close` | As a user's action: the game cancels its dialog if one is up, or posts `WM_CLOSE` to its window | The UI reads `MainDialog` and `hwndApp` on its thread to choose |

The cursor and the keys' state are each the newest of their posts, so taking them earlier than where they are read gives the same state there: the message loops take them too, so that they do not wait in the mailbox while no frame reads them, and the UI posts the cursor only when it moves.

A message changes the battle only where the original's input would, so the battle is the same as it is now. Each stand-in today reads, at a call, state that another thread writes at any time; with messages it gives, at the same call, everything posted before it. The characterization traces post their input while the game is not running (the lockstep scheduler, the traces of 1.22), so for them the two are the same, and the traces must not change. On the desktop they differ only within the race there is now.

What the UI decides from the game's state it decides from its copy, never from the game: whether the input is held back (`Extensions.HoldsInput`, which the frame's snapshot carries, and which is held from the start for a session that plays a trace or a replay), whether a replay is watched or seeking (its progress), which dialog is up and the game's window (the windows' view). The host UI's commands are already dropped where they are taken while the input is held (`UICommands.InputSite`).

### The windows' view
The windows and their controls (`HWND`) are the game's. The window functions mark that one has changed; the game gives the platform an immutable view of the windows (`WindowView`, `INspwPlatform.ShowWindows`), if one has changed, as it next goes to its window messages (`PeekMessage`, `GetMessage`, `WaitMessage`), shows a frame or a message box. A window's view keeps its kind, id, text, enabled, visible, initialized, checked, items, selection, template and children. The desktop app posts it to the UI (`WindowsChanged`).

The UI draws the dialogs from it (`Dialogs.Update`), and the connection form reads its fields from it (`MultiplayerScreen`), in place of `WindowsLock`. It names a window by its `HWND`, only as a key: an action it posts (`PostToGame`) finds the window again on the game's thread. `_needsFill` becomes the UI's own, set by `DialogFilled`, which the action posts once it has run, after it has given the platform the windows as it left them (`ShowChangedWindows`).

`WindowsLock` goes, with its uses in `winuser.cs`, `fileapi.cs` (`DlgDirSelectEx`), `Dialogs.cs` and `MultiplayerScreen.cs`. `ShowWindow`, which sets `Visible` with no lock now, marks its window like the others.

### Frames, without copying
Three frame buffers, each the pixels and the snapshot of one frame (`GameSnapshot`), go back and forth between the game and the UI:

1. The game copies the primary surface's pixels, and its snapshot, into a buffer it holds, and posts `FrameReady(buffer)` to the UI, if it still holds another buffer to fill next. If it does not, it keeps the frame as the one to post. The desktop app's platform does this, given the game's mailbox, which `Nspw` takes as it is made.
2. The UI, as it drains its mailbox, keeps the newest buffer it got, and posts each older one back to the game (`BufferFree`).
3. The game takes `BufferFree` before it fills the next frame, and posts the frame it kept, if any. It never waits for the UI: with no buffer back, it fills the kept one again.
4. While the game sleeps (`WaitMessage`, `GetMessage`), a `BufferFree` wakes it to post the frame it kept, and it sleeps again (`AppPlatform.WaitForMessage`): the UI is not left with a stale frame while a dialog is up. This handler changes nothing the battle sees.

A buffer is the game's or the UI's, never both: no pixels are copied across the threads, and the UI draws the newest whole frame. Now the pixels are copied twice, into the platform's frame and out of it, under `_frameLock`; then once, from the primary surface into the buffer. `CaptureAgain`, as a replay's view changes while it is paused, fills a buffer from the primary surface, which still holds the last frame.

### Otsuki's links
`IDatagramTransport` stays as it is: `Send(endPoint, datagram)`, from any thread.

- **Over UDP** the socket sends on the sender's thread, as now: the socket is the system's, not state of the process.
- **Over streams** (`StreamDatagramTransport`) an actor owns the links by endpoint (`Links`): a `Send` is a message to it, which it gives to the endpoint's link, opening one as the first datagram comes, as now. A link's datagrams wait in a bounded mailbox (256), which its stream's writer awaits; a link whose stream fails or closes posts that it has ended, and the next datagram opens a new one. A stream accepted from an endpoint that has a link only receives, as now.
- **The received datagrams** go to the handler given in `Start`, which the actor gives each link as it opens; `Dispose` is a message, so `_onReceive` is no longer read and cleared across threads with `Volatile`.

Giving each `Connection` its link once, as it starts, would save the lookup per datagram, but change `IDatagramTransport` and each of its transports, for no lock less: the lookup is the links' actor's own.

### The Bluetooth connectors
- **Mac:** the native callbacks post `Accepted`, `Opened`, `Received` (its data copied) and `Closed`, and return: a callback never waits, so `NativeDestroy`, which waits for the native thread, can run in the connector's `Dispose` with no lock to keep out of. `Write` copies the data and posts it without waiting, as the stream writes now; `Write` and `Close` of a channel are handled in their order. `Start` listens on the caller's thread, as now, as IOBluetooth may want, and throws if it cannot. `Connect` is not sent to the native library if its token is cancelled by the time it is handled; `Cancel` removes a request still waiting.
- **Windows:** the actor owns the listener, the published service and whether it is disposed, which `Start` and `Dispose` set under a lock now. The sockets' own async calls stay as they are.

### The peer
The peer's events wait in a mailbox of its own, which `DoWork`, `TryGetEvent` and `ReadEventsAsync` receive from; the session's snapshot of its players and groups is a `Latest`, which `Peer`'s queries (`IsHost`, `Players`, `GetPeerInfo`, `GetGroupInfo`) read from any thread, as they read the `Volatile` snapshot now; its send handles come from a `Sequence`.

Carrying the snapshot with each event, which would make `Peer` its owner's only, or making each query ask the session and wait, would not fit Otsuki's interface, which its tests and `Client` and `Server` query from any thread: the snapshot is an immutable message of which only the newest matters, which is what `Latest` is.

### Replays and traces
- **The player's controls** (`ChangeSpeed`, `Pause`, `SeekTo`, `Step`, `TakeOver`) are posted to the player's own mailbox, which it receives from as a frame starts (`StartsFrame`); the speed and the pause it starts with are set as it is made (`InitialSpeed`, `IsInitiallyPaused`).
- **Its progress** (`ReplayProgress`: the frame, the time, whether it has ended or is taken over or seeks, the difference, the notes) is a `Latest` it posts as a frame starts and ends, which the replay's bar reads.
- **A trace of 1.22's progress** (`Trace122Progress`) is a `Latest` too, which the window's title shows.
- **The viewer's view** (`ReplayView`) is the UI's; each change posts an immutable state (`ReplayViewState`) to a `Latest`, which the game takes as a frame starts (`ReplayViewExtension`) and copies its snapshot with.
- **Taking a replay over** posts the take-over to the player, and a user's action that scrolls the battlefield from the game's camera, which it reads on its thread, to the viewer's.


## What stays a wait
- **DirectPlay's synchronous calls** (`Host`, `Connect`, `Close`, `Release`): the game posts the command to the session and waits for its answer, as the API requires. The session answers through the command's `TaskCompletionSource`, and posts the events that go with it meanwhile, which the game takes in `DoWork` after.
- **The game asleep:** while a dialog is up, or the app is inactive, the game sleeps on its mailbox's signal, for a window message (`WaitMessage`). The lockstep scheduler's hook (`INspwPlatform.WaitForMessage(mailbox)`) takes no lock: the default sleeps on the signal; the scheduler's yields to the driver, without the `Monitor.Exit` and `Monitor.Enter` around it now.
- **Questions to a running game:** a test or a benchmark that waits for the game to reach a state posts the question to the game's thread, which answers as its message loop dispatches it (`RunningGame.Ask`, `SoloGame.WaitUntil`), in place of reading the game's fields from its own thread; the in-memory networks of Otsuki's tests answer theirs (`InMemoryNetwork.Ask`). Binding a transport to such a network waits for its answer, as a socket's bind does.
- **The app's end:** the UI waits for the game's thread to end (`WaitForEnd`, a join with a timeout), so that the game says goodbye to its rival.

## What needs no message
- **The settings and the sounds** of the desktop app are used on the game's thread only (`ReadSetting`, `WriteSetting`, `CreateSound`, `StopSounds`): their locks go, with nothing in their place.
- **The extensions' list** is filled before the game runs, before its thread starts or, in the lockstep tests, before the driver first steps it: a plain list in place of `ImmutableInterlocked`.
- **What a test's callbacks count** on a network: the network's own, read with a question (`InMemoryNetwork.Ask`); the tests' next addresses come from a `Sequence`.
- **Caches of the tests** (`TypeLayout`), shared by the tests that run in parallel: one per thread, in place of a `ConcurrentDictionary`.

## What goes
- **The game:** `_messages` and its `Monitor.PulseAll`, `WindowsLock` and `_keyState`'s lock (`winuser.cs`, `fileapi.cs`); DirectInput's `ConcurrentQueue` (`dinput.cs`).
- **The host UI's commands:** `UICommands`' lock (OpenNspw.HostUI).
- **The desktop app:** `_frameLock`, `_sounds`' lock and `Messages`' lock (`AppPlatform.cs`, `GameSession.cs`); `WindowsLock` in `Dialogs.cs` and `MultiplayerScreen.cs`; `_hasEnded` and `_needsFill`, which are `volatile`; `ReplayView`'s lock.
- **A replay's player:** the lock on its notes, which come with its progress (`ReplayPlayer.cs`).
- **Otsuki:** the transport's lock and its `Volatile` handler (`StreamDatagramTransport.cs`); the Bluetooth connectors' locks and `Volatile` handler (`MacBluetoothConnector.cs`, `WindowsBluetoothConnector.cs`); the peer's `Volatile` snapshot and `Interlocked` send handles (`Peer.cs`).
- **Recorder122:** its gate (`Program.cs`). The debugger waits for its events with a timeout, in place of `INFINITE`, and takes `Close` between them; Ctrl+C cancels the process's end, posts `Close`, and the debugger's thread closes the trace and ends 1.22.
- **The benchmarks:** `SoloPlatform`'s events, which park and resume the game (a message each way: `Parked`, and the resume), its `Interlocked` clock (the game's, then the benchmark's once it is parked) and its message boxes' lock (`MessageShown`); `ScriptedRival`'s lock (mailboxes for its events and orders).
- **The tests:** the lockstep scheduler's semaphores and its hook's `Monitor` (`LockstepGame.cs`); `TitleScreenTests`' frame and message boxes' locks; Otsuki's `EventLog`, the in-memory networks (an actor each: the network owns its transports, partitions, random and counters, and the tests ask it for the counts) and the counters of `PeerTests`.
- **The fields read across threads with no lock:** the cursor; the game's camera, which the UI reads to take a replay over (`TakeOverReplay`: the take-over is a message, and the game scrolls from its own camera to the viewer's); a replay's controls and progress (`Frame`, `Milliseconds`, `HasEnded`, `Divergence`, `IsTakenOver`); a trace of 1.22's progress (`Frames`, `HasEnded`, `Divergence`), which the window's title shows; DirectInput's `Acquired`; the windows' fields that the UI reads. Each becomes a message.
- **`System.Threading.Channels`**, in `Actor<T>`, the peer's events, the links, the RFCOMM stream, the host enumerator and Otsuki's tests.
- **`GuardedByAttribute`**, which nothing uses.

## Keeping it so
An analyzer in `Aigamo.Otsuki.Analyzers`, beside its `ImmutableAnalyzer`, reports as an error (`OTSUKI0003`), in every assembly but `Aigamo.Otsuki.Actors`:
- a `lock` statement, and a `volatile` field;
- a use of `Monitor`, `System.Threading.Lock`, `SpinLock`, `Mutex`, `Semaphore`, `SemaphoreSlim`, `ReaderWriterLock`, `ReaderWriterLockSlim`, `ManualResetEvent`, `ManualResetEventSlim`, `AutoResetEvent`, `CountdownEvent`, `Barrier`, `Interlocked`, `ImmutableInterlocked` and `Volatile`;
- a use of `System.Threading.Channels` and `System.Collections.Concurrent`, and a using directive of them.

It reports the names of these types, which any use of them or of their members names, as their instances come only from them.

What the libraries do inside (`Task`, `TaskCompletionSource`, `CancellationTokenSource`, timers, sockets) is theirs. Every project gets the analyzer from `Directory.Build.props`, but the analyzer and its tests, and the mailbox's tests, which race threads against it.

## Where it meets the extensions
The host UI's commands and the traces of 1.22 are extensions of the game (docs/Extensions.md), in OpenNspw.HostUI and OpenNspw.Verification. The commands' queue (`UICommands`) becomes a mailbox of that extension: the UI posts to it, and the extension takes each site's commands where the port calls it at that site (`IGameExtension.InputSite`), on the game's thread, as it takes them now under its lock. What the extension shows the UI (`ShownCombatMenu`, `LastCameraMove`) goes with the frame's snapshot, as now. A trace's progress, and a replay's, go to the UI as messages.

## Open questions
- **DirectPlay's events while the game sleeps.** The original sleeps in `WaitMessage` while a dialog is up or the app is inactive, and runs `DoWork` only once a window message has woken it: DirectPlay's events wait for the next window message. Two ways:
  - **The events wake the game:** the peer posts its events to the game's mailbox (`PeerOptions` would take the mailbox to post them to), and the game waits for window messages and DirectPlay's events: it sees a rival leave, or a message, while a dialog is up, at once. The port no longer waits as the original does, and it is a difference to write in docs/Differences.md.
  - **Only window messages wake it:** the events wait in the peer's own mailbox for the next window message, as in the original and in the port now. Nothing changes.

  The port does the second, which keeps the original, until this is decided.

## Steps
Each a commit, built and tested on its own:

1. **`Mailbox<T>`**, pooled, thread-bound and awaited, selective, bounded; with its own tests, which race posters against the owner, and a wake that comes between the owner's last look and its sleep.
2. **Otsuki's actors on it:** `Actor<T>` and the session, endpoint and connections; then the links and the transport, the peer's events, the RFCOMM stream and the host enumerator; then the Bluetooth connectors. Otsuki's tests, and the network tests of OpenNSPW.
3. **The game actor:** window messages, DirectInput's events, the keys' state and the cursor, `Close`, each received at its point; the lockstep scheduler's hook. The characterization traces show the battle is unchanged.
4. **The UI actor:** frames by buffer, the windows' view in the dialogs and the connection form, the message boxes, the game's end, the input held back; the host UI's commands; and a replay's and a trace's view, controls, take-over and progress, which share the session and the game's screen with it.
5. **The rest:** Recorder122, the benchmarks, the test helpers.
6. **The analyzer**, once nothing is left for it to report.

## Costs
- More message types, one for each thing that crosses a thread.
- The UI sees the game's state as of its last message, as it now sees the last snapshot; the windows' view as of the game's last look at its messages.
- The game posts the windows' view when one has changed, which the UI reads now in place.
- A selective receive passes over what waits of the other kinds, which the owner keeps in a list: a kind that no point takes for long would make each receive longer. The cursor and the keys' state, which only frames read, are taken by the message loops too.
- Every thread depends on `Mailbox<T>`: it must be small and tested hard.
