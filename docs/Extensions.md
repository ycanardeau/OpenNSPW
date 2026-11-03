# Extensions
A design: the port stays the core, and what is not the original game is built as extensions around it, in projects of their own. Replays are the first.

## Why
New features have been going into `OpenNspw`, next to the ported code: the traces of 1.22 (`Trace122.cs`), the host UI's commands (`UICommands.cs`), what the UI reads (`UIState.cs`). Each adds calls in the ported files, and each is changed together with the port. The port should change only to be more exact, and features should change without touching it.

## The model
As in VS Code, which talks to its extensions through an extension API, and to language servers through a protocol:

| VS Code | OpenNSPW |
| --- | --- |
| The editor's core | `OpenNspw`: the port, the game as the original plays it |
| The extension API | The game's extension API, in `OpenNspw`: the points of a frame, the player's actions, a few entry points |
| Built-in and third-party extensions | Projects beside the port: `OpenNspw.Replay`, the verification against 1.22 (`OpenNspw.Verification`), the host UI (`OpenNspw.HostUI`), later computer players |
| Language servers, out of process | Later, a bridge extension that speaks the API over a protocol, for programs outside the app |
| The editor's window | The hosts: the desktop app, the tests, command-line tools, which choose the extensions and compose them |

The core does not know any extension. Extensions know the core only through the API. Hosts know both.

## The extension API
The API is all that the core adds for extensions. It is written once and kept small. A new feature uses it as it is; when one needs more, the API grows by a general point, never by a feature's own call in the ported files.

### Points of a frame
An extension implements `IGameExtension`, whose methods all do nothing by default, and is added to the game (`Nspw.Extensions`). The game calls it on its own thread, at fixed points, synchronously: the game waits, so what an extension does is in step with the battle.

| Point | Where | What an extension may do there |
| --- | --- | --- |
| `StartsBattle` | `WinMain`, before it connects to a rival | Say that it starts the battle itself, so that the game does not connect first |
| `GameStarted` | `WinMain`, before the message loop | Start a battle (below) |
| `BattleStarting`, `BattleStarted` | `InitializeGame`, at its start and at its end | Read the start; set rand's state (`srand`) |
| `FrameStarting` | `UpdateFrame`, as a frame of the battle would start | Hold the frame, start it now, or leave it to the game's timer; set its ticks and the speed shown |
| `TickStarting` | Before each tick of the battle (`UpdateBattle`) | Read the battle |
| `ResultChecking` | At the start of `CheckResult`: after each tick that is not drawn in a single-player game, and after the drawing | Read the battle |
| `Drawing` | Before `DrawBattleArea` | Read or set the camera |
| `InputSite(site, counts)` | Where the game takes the clicks of a part of the screen (`DrawBattleArea`, `UpdateUnitInfo`, `UpdateGameSetting`), with whether a click counts there in this frame; and at the start of `HandleInput` (`Orders`), where it takes the frame's orders | Do player actions there |
| `OrdersStarting` | Where a turn's orders start to be applied (`IssueSingleplayerOrders`, `ReceiveAndIssueOrders`) | Read the battle |
| `PlayerAction(action)` | In the functions that a click calls, as the game does it | Read it |
| `UninitializedLocal(instruction)` | Where the port reads a local that 1.22 reads without setting it ([Verification.md](Verification.md)) | Answer what Windows' stack held there; the first that answers wins |
| `FrameEnded` | After `HandleInput` | Read the battle |
| `MessageReceived`, `RivalLeft` | `DirectPlayMessageHandler` | Read them |
| `Presented` | After the frame is shown | Read the battle; queue commands for later frames |

### The player's actions
`PlayerAction` is what the player did, as the game does it: a closed set of cases, one record each with what its function takes (`ClickUnit`, `SetDestination`, `CancelSelection`, `RestoreSelection`, `ChooseCombatMenuItem`, `ToggleDeckView`, `SelectSupplyTarget`, `RequestSupply`, `ConfirmSystemMenu`, the debug keys `SwapLocalSide` and `SinkSelectedUnit`, and `GiveOrders`, a frame's orders given whole as the input leaves them, `PlayerOrders`, as a trace of 1.22 gives its player's). The game raises it from the functions that the mouse, the host UI's commands and a computer player all reach, so one record covers every source. `Nspw.Do(action)` does one, as the click did. Cases are handled with the `Match` that Aigamo.MatchGenerator generates (`[GenerateMatch]`), which takes every case: a case added fails to compile wherever it is not handled, such as where a replay writes actions.

### The setting screens' actions
`SettingAction` is what the host did on the setting screens, as `PlayerAction` is in a battle: choosing and confirming a scenario, turning the page, the starts from the resume-saved and the auto-saved battles, switching the sides, the rules' changes, going back to the scenarios and starting the battle. The game raises it from the functions that the clicks reach (`IGameExtension.SettingActed`), and `Nspw.Do(action)` does one. A set of its own, as a battle has none of them: a replay, which starts with the battle, never meets them.

### Entry points
- `StartNetworkBattle(settings, peer, threadPool)`: starts a network battle from its settings, over a DirectPlay peer and thread pool of the extension's, without the setting screens, as `StartSingleplayerGame` does for 1.22's scenarios. The settings are what the setting screens and the rival's messages set: the seed, the sides, the rules, the map editor's scenario, the names, and what the rival's messages before the battle left of the turns, which `InitializeGame` keeps: whether a turn may go on at each sync point (`CanAdvance1`, `CanAdvance2`), and the rival's checksums.
- `ReadNetworkBattleSettings()`: those settings, as a battle starts.
- The queries (`Queries.cs`): the game's own checks and texts, which read and write nothing, such as whether a click on a unit counts (`CanClickUnit`), the combat menu's items (`ListCombatMenuItems`), where the landing mark goes (`LandingMarkAt`), what the player's units see (`IsSeaTileSeen`, `IsEffectSeen`), and the texts the game writes (`ReadUnitDetails`, `ReadResult`, ...).

### Rules
- **Input authority.** At most one extension gives the player's input, such as a replay that plays or a trace of 1.22 that the game follows. While one does, the host's keys, clicks and commands are held back. It holds the input as long as it says so (`HoldsInput`): a replay until it ends or is taken over, a trace until it ends or the game differs from it.
- **No hidden writes.** An extension changes the battle only through actions and the interventions above: seeds, ticks, camera, held frames, and 1.22's uninitialized locals. Then a recorder sees all of it, and a replay of a game with any extension plays the same.
- **Order.** Extensions are called in the order they were added. Where several intervene, the first that answers wins.
- **Platform seams first.** What the platform already abstracts is replaced, not hooked in the core: files (`OpenFile`, `GameFileLayers`), the clock, DirectPlay.

## Projects
| Project | What it is |
| --- | --- |
| `OpenNspw` | The port, and the extension API (`Extensions.cs`, and the calls at the points above) |
| `OpenNspw.Verification` | The traces of 1.22 ([Verification.md](Verification.md)): the format, the replay that the game follows and checks itself against, and the recorder of the port |
| `OpenNspw.HostUI` | The host UI's commands (`UICommands`), given as the player's actions where the game takes the clicks they stand for, and what the UI reads of the game (`UIState`), such as what the player sees around any camera (`ReadSight(camera, ...)`) ([UI.md](UI.md)) |
| `OpenNspw.Replay` | Replays: the format, the recorder, the player, the DirectPlay stand-in that gives the rival's messages, and a file layer with the files a battle started from |
| `OpenNspw.Desktop` | The app: adds the recorder to every battle, plays replays, and shows them with a camera of their own |
| `OpenNspw.Tests` | Tests of the port, and of the extensions in their games |

Later, each its own change: computer players as an extension, and the out-of-process bridge. How the traces of 1.22 and the host UI left the core is in [FeatureExtensions.md](FeatureExtensions.md).

## Replays, as an extension
- **Recorder.** At `BattleStarting` it takes the start: the mode, the scenario, the seeds, the network settings (`ReadNetworkBattleSettings`), and the files the battle starts from, read through the game's platform. At each point it writes what it sees: ticks, camera, actions, the rival's messages, hashes. It intervenes in nothing.
- **Hashes** cover the units and the fires, rand's state and the tick, but not a unit's sprite column (`SpriteColumn`): the drawing sets it for the units it draws, so it follows the camera and the cursor, and the battle never reads it.
- **Player.** It holds the input authority. It sets the seeds, starts a network battle (`StartNetworkBattle`), paces the frames and gives their ticks (`FrameStarting`), the camera (`Drawing`), and the actions (`InputSite`).
- **The rival, in a network replay**, is a DirectPlay peer and thread pool that the player gives `StartNetworkBattle`: they send nothing, and deliver the recorded messages in `DoWork` after their frame, where DirectPlay delivered them.
- **Files.** The files a battle started from (the map editor's scenario, a saved battle) come from a file layer over the host's platform. The game's writes during a replay (the auto-save, `cnct_error.dat`) go to a scratch folder, not the user's.

## A free camera
A replay follows the recorded camera: the camera decides what is drawn, and drawing draws random numbers (1.22's effects take `rand`; a network battle's take the cosmetic table, which a landing plane reads). So the battle keeps the recorded camera, and the viewer gets one of its own:

- The game runs with the recorded camera, exactly.
- The desktop app keeps a viewer's camera, moved by dragging with the middle button and with the minimap. Its snapshot reads the sight around the viewer's camera (`ReadSight(camera, ...)`, `OpenNspw.HostUI`), and the battlefield is drawn from there. A button puts the view back on the player's camera.
- The viewer may show both sides, or only the recording player's. This is a choice of what to draw, not a change to the battle.
- Taking over a single-player replay moves the game's camera to the viewer's.

What was checked: the game shows the effects by what the player's units see, not by the camera, so they animate off the recorded camera too; the UI draws a unit's sprite by its heading (`SpriteColumnOf`), not by the column the game last drew; the sounds keep the recorded camera's volume.

## Out of process, later
A bridge extension serves the API over a protocol (JSON-RPC over stdio or a socket): each point is a request that the game waits on, and each action a call. Bots in other languages, external debuggers and training tools then need no build of the app. It costs a round trip a point, so it gives only the points a client asks for.

## Steps
1. **Core:** the extension API (`Extensions.cs`), and its calls at the points above. Existing tests show nothing else changed.
2. **`OpenNspw.Replay`:** the format, the recorder and the player, on the API, for single-player and network battles: the rival's DirectPlay stand-in and the file layer.
3. **Desktop:** every battle recorded; the Replays screen; the watch screen's bar; the free view, through `ReadSight(camera, ...)`.
4. **The traces of 1.22** into `OpenNspw.Verification`, and **the host UI** into `OpenNspw.HostUI` ([FeatureExtensions.md](FeatureExtensions.md)).
5. **Later:** keyframes, to seek back without playing from the start; computer players as an extension; the out-of-process bridge.

## Open questions
- Names: "extension" (VS Code's) or "plugin" or "mod"?
- Loading: only compiled in for now. Assemblies loaded at run time, and the bridge, come later. Should third-party extensions be a goal at all?
