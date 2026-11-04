# Features as extensions
A design: the three features that still live in the core, next to the ported code, move into extensions of their own, on the extension API ([Extensions.md](Extensions.md)): the traces of 1.22 (`Trace122.cs`), the host UI's commands (`UICommands.cs`) and what the UI reads (`UIState.cs`). The API grows by a few general points for them, never by a feature's own call in the ported files.

## What stays in the core
The core is the port: the game as NSPW NET plays it, and NSPW 1.22's single-player game as 1.22 plays it.

- The battle, the setting screens, the network game, the drawing and the sounds.
- 1.22's single-player game: its scenarios (`Scenarios122.cs`), its computer player (`ComputerPlayer.cs`, `ComputerStrategy.cs`, `ComputerFleet.cs`), its decision, and the rules where it differs (`BattleRules`). The computer player of 1.22 is part of the port, as `cpu_strtgy` is part of 1.22; new computer players are extensions.
- What 1.22 reads but never sets (its uninitialized locals): the port reads them where 1.22 does, and keeps what Windows' stack holds there by default. Where a value comes from is the API's (below).
- 1.22's layout of the units and the fires (`WriteStateAs122`), which the port also writes into the emulator.
- The extension API, and the queries below.

## The traces of 1.22: `OpenNspw.Verification`
`Trace122.cs` moves whole into a new project, `OpenNspw.Verification`: the format (`Trace122Writer`, `Trace122Reader`, `Trace122Layout`), the replay that follows a trace of 1.22 (`Trace122Replay`) and the recorder of the port (`Trace122Recorder`). Both become extensions (`IGameExtension`); `ISingleplayerTrace`, `TracePoint`'s calls in the ported files and the game's `Trace` field go.

### Its points
Each call of a trace in the core is an existing point, or a new general one:

| Trace's point | Where it is called now | On the API |
| --- | --- | --- |
| `Start` | `InitializeGame`: `srand(Trace.Seeds.Start)`, then `Reach(Start)` | `BattleStarting`, which comes just before: the replay sets rand's state there (`srand`) and checks it; the recorder reads it |
| `Seeded` | `InitializeGame`: `srand(Trace.Seeds.Battle ?? time())`, then `Reach(Seeded)` | `BattleStarted`, which comes just after: the replay sets the seed of game_init's last `srand` over the time's, as a replay of the game already does |
| `Frame` | `TryStartSingleplayerFrame`: `Reach(Frame)`, then `TicksOfNextFrame` | `FrameStarting`, the next thing the frame does: the replay sets the speed and returns the trace's ticks; the recorder writes the speed |
| `Tick` | `UpdateFrame`, before each `UpdateBattle` | New: `TickStarting`, before each tick of the battle, in every mode |
| `Decision` | `CheckResult` (`cnct_decision`), before `Decide` | New: `ResultChecking`, at the start of `CheckResult`, in every mode |
| `Drawing` | `UpdateFrame`, before `DrawBattleArea` | `Drawing`, which comes just after it: the replay sets the camera, as a replay of the game does |
| `PlayerOrders` | `HandleInput`, at its start (`ReachPlayerOrders`) | `InputSite` at a new site, `Orders`: the start of `HandleInput`, where the frame's orders are taken (below) |
| `Orders` | `IssueSingleplayerOrders`, before the combat menu's item and `cpu_strtgy` | New: `OrdersStarting`, where a turn's orders start to be applied: `IssueSingleplayerOrders`, and `ReceiveAndIssueOrders` in a network battle |
| `StackLocal` | `GiveStackLocal`, `TakeStackLocal`, `UntakenStackLocal`, `StackLocal(0x4050C7)` | New: `UninitializedLocal`, an answer (below) |

The three new points read only. In the ported files, `Trace?.Reach(...)` at the tick, the decision and the orders becomes `Extensions.TickStarting()` and the others; the trace's calls in `InitializeGame`, `TryStartSingleplayerFrame` and `UpdateFrame`'s drawing go, as the existing points cover them.

The frame's speed (`GameSpeed`) is set at `FrameStarting`, as a replay of the game sets it: once the ticks are given, only the sidebar shows it. `FrameStarting`'s rule says so: the point may set the frame's ticks and the speed.

### Uninitialized locals
1.22 reads four locals it never sets (`docs/Verification.md`, StackLocal). The port reads them at the same places, through two calls that stay: `TakeStackLocal(instruction, own)` in the battle (`etc1.cs`, `etc2.cs`), and `StackLocal(0x4050C7)` in the computer's strategy (`ComputerStrategy.cs`, and the emulated `RunCpuStrategy`). What they return comes from the extensions:

- `byte[]? UninitializedLocal(Nspw game, uint instruction)`: what Windows' stack held where 1.22 reads the local at that instruction, or null. The first extension that answers wins, as for `StartsFrame`; with no answer, the port keeps its own: the battle's own local, and 0 for `cpu_strtgy`'s, as Windows' stack holds there (`StackLocalsOnWindows`, the BUG(1.22) mark stays on it).

The trace's queue of the locals it gives, and its check that the port read each one before the next point (`UntakenStackLocal`), move into `Trace122Replay`: it knows which it gave and which the port asked for. The core loses `_battleStackLocals`, `_stackLocals`, `GiveStackLocal` and `UntakenStackLocal`; `cpu_strtgy`'s locals, which a trace gives for the coming strategy only, are dropped by the replay at its next point rather than by `IssueSingleplayerOrders`.

### The player's orders
A trace of 1.22 records a frame's orders as 1.22's globals leave them (`new_pp[1]`, `new_slct[1]`, the menu's item, the selection), not as clicks. The port takes them the same way, at the start of `HandleInput`:

- `Trace122Orders` moves into the core as `PlayerOrders`, the API's type for a frame's orders, and `ReadPlayerOrders()` becomes public: the recorder reads them at the `Orders` site.
- Giving them is an action, `PlayerAction.GiveOrders(PlayerOrders Orders)`, whose site is `Orders`. `GivePlayerOrders` raises it, once for the whole: the menu's item it chooses inside does not raise its own `ChooseCombatMenuItem` again. Then a recorder of the game sees it, and the rule that an extension changes the battle only through actions holds for the trace too.
- The replay's format writes it as a case of its own, as `Match` requires.

### The input
`ISingleplayerTrace.TakesInput` is `IGameExtension.HoldsInput` negated:

| Trace | `TakesInput` now | `HoldsInput` |
| --- | --- | --- |
| `Trace122Replay` | `HasEnded \|\| Divergence is not null` | `!HasEnded && Divergence is null`: it holds the input while it follows 1.22, and lets the player take the game over at the first difference or at the end |
| `Trace122Recorder` | `true` | `false`, the default |
| The tests' captures (`SetupCapture`) | `true` | `false` |

`Trace is { TakesInput: false } || Extensions.HoldsInput`, in `QueueUICommand` and in the desktop app's `GameSession`, becomes `Extensions.HoldsInput`.

### What it needs public
- `WriteStateAs122`, public already. The constants of the layout it writes (`UnitCount`, `UnitSize`, `FireCount`, `FireSize`) move from `Trace122` into the core beside it, which then no longer reads the trace's class.
- `ReadPlayerOrders`, and `PlayerOrders` with it.
- `Tick`, `Renderer.IsDrawnTick`, `RandomState`, `CameraPosition`, `GameSpeed`, `ScenarioNumber`: public already.
- `Trace122Layout` reads the fields of `Unit` and `Fire` by reflection; they are public.

`OpenNspw.Verification` gets no `InternalsVisibleTo`: what it needs is the API. `OpenNspw.Recorder122` references it instead of the core, for the format.

## The host UI's commands: `OpenNspw.HostUI`
`UICommands.cs` moves into a new project, `OpenNspw.HostUI`, as an extension: the commands are queued on the UI's thread, as now, and run at `InputSite` on the game's, as the player's actions (`Nspw.Do`) after the checks the click makes, through public queries.

### Its points
The calls in the ported files (`unit_info_cont.cs`, `draw_cmbt_area.cs`, `demo.cs`) are `RunUICommands(site, count)` at each site where the game takes a part's clicks, and the UI's own behaviour around them. `RunUICommands` already calls `Extensions.InputSite(site)`; it shrinks to that call, which also passes whether a click counts there in this frame:

- `void InputSite(Nspw game, ActionSite site, bool counts)`. `counts` is the condition the ported code passes today: whether the part is shown, whether an order waits for its turn, whether the host is on the screen. A site not shown is still reached, with `counts` false, so that the commands given for it are dropped, as clicks are.

The site's enum, `UICommandSite`, stays in the core, since the API takes it, as `ActionSite`: where in a frame the game takes an action. It gains `Orders` (above). `PlayerAction.Site` returns it.

What the core does now only for the UI goes with the commands, and its calls in the ported files go:

| In the core now | In `OpenNspw.HostUI` |
| --- | --- |
| `RunBrushedUnits`, after the battlefield's site | At `InputSite(Battlefield)`, after the queued commands: the next unit brushed, if it still is the selected unit's formation mate and a click on it counts |
| `RunWaitingCombatMenu`, after the combat menu's site, shown or not | At `InputSite(CombatMenu)`: the waiting click, done once the player can order (`CanOrder`), dropped after 40 frames or once the menu no longer shows its item |
| `RunWaitingSystemMenu`, after the system menu's site | At `InputSite(SystemMenu)`, the same |
| `ShownCombatMenu`, `ShownCombatMenuCount`, written in `UpdateCombatMenu` and where the menu is not shown | Read at `InputSite(CombatMenu)`: the items the menu shows (`ListCombatMenuItems`, below) when `counts`, none otherwise |
| `MoveCamera`, `LastCameraMove` | At `InputSite(Minimap)`: the camera, set as the minimap's click sets it, and the last move taken, which the snapshot reads |
| `ActiveCombatMenuItem`, `IsFormationMate`, `IsSelectedShown` | Its own: they read public fields of the units |
| Dropping the other screens' commands, and every command while an extension holds the input | Its own: the input is checked on the game's thread, at the site, rather than as the command is queued |

### The setting screens' actions
The battle's clicks are `PlayerAction`s already. The setting screens' are not: they call the game's functions directly. They become a closed set of their own, `SettingAction`, with a generated `Match`, raised from the functions that the clicks and the commands reach, and done with `Nspw.Do(SettingAction)`:

| Case | Function | Site |
| --- | --- | --- |
| `ChooseScenario(int N)` | `ChooseScenario` | `ScenarioList` |
| `ConfirmScenario` | `LeaveGameSetting` | `ScenarioList` |
| `TurnScenarioPage` | `TurnScenarioPage` | `ScenarioMenu` |
| `StartFromResume`, `StartFromAutoSave` | `StartFromResume`, `StartFromAutoSave` | `ScenarioMenu` |
| `SwitchHostSide` | `SwitchHostSide` | `ScenarioMenu` |
| `ToggleDecision`, `ToggleSwapRule`, `CycleArrivalControl` | The same | `Rules` |
| `ChangeSupplyRate(int Player, bool Increase)` | `IncreaseSupplyRate`, `DecreaseSupplyRate` | `Rules` |
| `ChangeSwapTime(bool Increase)` | `IncreaseSwapTime`, `DecreaseSwapTime` | `Rules` |
| `ChangeInitialSupplyPoints(int Player, bool Increase)` | `IncreaseInitialSupplyPoints`, `DecreaseInitialSupplyPoints` | `Rules` |
| `BackToScenarios` | `BackToScenarioSetting` | `Rules` |
| `StartBattle` | `StartBattle` | `Rules` |

A set of its own, rather than more cases of `PlayerAction`, because a battle never has them: a replay, which starts at `BattleStarting`, would otherwise have to write cases it never meets. A click on a scenario is two actions, as it is two functions: `ChooseScenario`, then `ConfirmScenario`.

Two clicks change no state of the battle and are entry points rather than actions:

- `AskSystemMenu(answer)`: the system menu's click, which only opens the game's question. Its answer is the action (`ConfirmSystemMenu`).
- `ShowMinimapCell(column, row)`: the minimap's click, which only moves the camera. A recorder reads the camera at `Drawing`, wherever it was moved.

The press's sound is the game's `Sound`, public already.

### The queries
A query is one of the game's own checks, made public, that reads and writes nothing. Where one now also writes, the reading part is extracted, as a refactoring that keeps the characterization traces unchanged, and the ported code calls it:

| Query | From | Now |
| --- | --- | --- |
| `CanClickUnit(m)` | `draw_cmbt_area.cs` | Internal, takes `ref Unit` |
| `IsOnShownDeck(m)` | `Renderer` | Internal |
| `CanSetDestination(m)` | `draw_cmbt_area.cs` | Private |
| `GetSupplyRequestState()` | `unit_info_cont.cs` | Private |
| `ListCombatMenuItems(items)` | `ListCombatMenu`, without setting the choice shown (`CombatMenuSelection`) | Private, and writes |
| `LandingPointAt(x, y)` | `AimLanding`, without putting the landing mark | Internal, and writes the mark's sprite |
| `IsSeaTileSeen(column, row)`, `IsEffectSeen(position)` | `UIState.cs`, as `DrawMapTiles` and the effects test it | Private; where the drawing tests the same, it calls them, so that the two cannot drift |
| `EyeOf(unit)` | `UIState.cs`, but the ported code calls it (`Audio.cs`, `draw_cmbt_area.cs`, `etc2.cs`) | Internal; it moves into the ported code's own files |
| `GetUnitSprites(unit)`, `GetDetectionSize(m, n)` | The drawing, the battle | Public already |

They live in the core, in a file of the API's (`Queries.cs`). An extension calls them on the game's thread, at a point.

## What the UI reads: into `OpenNspw.HostUI`
`UIState.cs` splits in two:

- **The game's texts stay in the core**, among the queries: `ReadUnitDetails`, `ReadSupplyTargetName`, `ReadSupplyTargetFrame`, `ReadScenarioNames`, `ReadScenarioDescription`, `ReadArrivalControl`, `ReadResult`. Each is one call of the writer the game draws its text with (`WriteUnitDetails`, `ListScenarioNames`, `WriteResult`, ...), decoded from Shift JIS. Moving them would make those writers public, with the original's text buffers in their signatures.
- **What is put together from the queries moves**: `ReadSight`, `SightMargin`, `SightSize`, `ReadClickable`, `ReadShownOnDeck`, `ReadUnitSprites`, `ReadActiveCombatMenuItem`, `ReadCanSetDestination`, `ReadNotices`, `ReadChat`, `SpriteColumnOf`, `EffectSightOf`. They become extension methods on `Nspw` in `OpenNspw.HostUI`, so that the app's calls read as they do now.
- **`IsSupplyTargetShown` stays**: the ported drawing writes it and tests it (`UpdateUnitInfo`), by the frame's count, which has moved on by the time the snapshot reads it. It is the frame's own state, as the other globals the snapshot reads.

`ReadSight(camera, ...)` leaves the API ([Extensions.md](Extensions.md), entry points): a replay's free view reads it from `OpenNspw.HostUI`, and the API keeps `IsSeaTileSeen` and `IsEffectSeen`, which are what it needs.

## What the hosts change
### The desktop app
- References `OpenNspw.Verification` and `OpenNspw.HostUI`.
- `GameSession` adds the host UI to every game, and keeps it (`Commands`): the screens give their commands through it rather than `Game.Commands`. It adds a trace of 1.22 (`--replay <trace>.n122`) as an extension, as it adds a replay's player; `IsInputHeldBack` is `Extensions.HoldsInput`. A game that follows a trace is still not recorded.
- The snapshot (`GameSnapshot.CopyFrom`) reads the combat menu's items and the last camera move from the host UI, under the frame's lock as now.
- `ReplayView` reads the sight from `OpenNspw.HostUI`.

### The tests
- `Trace122Tests`, `SingleplayerTests` and `Lockstep/SingleplayerRun` add a trace as an extension (`game.Extensions.Add`), not `game.Trace`. `SetupCapture` captures at `BattleStarted`.
- `UICommandTests` and `Lockstep/BattleInput` add the host UI and give their commands through it. `UICommandTests` still requires the same traces from the mouse and from the commands.
- `ExtensionTests` cover the new points and the answer: the order of the points in a frame, and a local given and taken.
- `ReplayTests` cover `GiveOrders` in the format.
- `ComputerPlayerTests`, which run `RunCpuStrategy`, do not change: with no answer, its local holds Windows' 0, as now.

## The emulator, later
The tests compare the port's rewrites with 1.22's machine code, through glue in the core (`Singleplayer.cs`): `SingleplayerExecutable`, `Emulator`, copying the units and the shared globals in and out (`CopyIn`, `CopyOut`, `SharedGlobals`), and `RunEmulated` with each function of 1.22 it calls (`RunCpuStrategy`, `RunDecision`, `RunSetFormation`, ...). Only the tests use it. It could move into a project of its own, and the core would then no longer reference the emulator (`OpenNspw.Singleplayer`, Aigamo.Enzan). It needs more of the core than the three features: 1.22's layout read back as well as written, the globals' bytes, and the gas of each unit as it went in. So it is a later change, and only if the core's surface for it stays small.

## Where this meets the actors
A later design moves each thread to an actor. This one leaves room for it, and designs none of it:

- The host UI's queue, given on the UI's thread and taken at the sites on the game's, is a mailbox already. It would become the game's actor's mailbox, and its lock would go.
- The input is checked on the game's thread, at the site, not as a command is queued: no extension's state is read from the UI's thread.
- The points stay synchronous calls within the game's actor: an extension is in step with the battle, so it is part of the game's actor, not one of its own.
- The snapshot, taken at `Presented`, is the message the game's actor sends the UI's.

## Steps
Each a commit that builds, and keeps the characterization traces and the trace tests passing.

1. **API:** the points `TickStarting`, `ResultChecking` and `OrdersStarting`; the answer `UninitializedLocal`; `InputSite`'s `counts`; `UICommandSite` as `ActionSite`, with `Orders`; `PlayerOrders`, public `ReadPlayerOrders`, and `PlayerAction.GiveOrders`, with its case in the replay's format. Nothing uses them yet but the tests. `docs/Extensions.md`.
2. **The traces as extensions, in the core:** `Trace122Replay` and `Trace122Recorder` on the points; `ISingleplayerTrace`, the `Trace` field, the trace's calls in the ported files and the stack locals' queues go; the tests and the app add traces as extensions.
3. **`OpenNspw.Verification`:** `Trace122.cs` moves; the layout's constants stay in the core; `OpenNspw.Recorder122`, the tests and the app reference it. `docs/Verification.md`.
4. **The queries:** the checks made public, the reading parts of `ListCombatMenu` and `AimLanding` extracted, `EyeOf` into the ported code's files, the texts in `Queries.cs`.
5. **`SettingAction`:** the setting screens' actions, raised by their functions, and `Do`.
6. **The host UI as an extension, in the core:** `UICommands` on `InputSite`, `Do` and the queries; the brushing, the waiting clicks, the camera's moves and the combat menu's items move into it, and their calls in the ported files go.
7. **`OpenNspw.HostUI`:** `UICommands.cs` and the part of `UIState.cs` that composes move; the app and the tests reference it; `ReadSight` leaves the API. `docs/UI.md`, `docs/Extensions.md`.
8. **Later:** the emulator's glue, if it is worth it.

Steps 1 to 3 and 4 to 7 are independent of each other but for step 1's `ActionSite` and `counts`.

## Open questions
- **The setting screens' actions:** a set of their own (`SettingAction`, as above), or more cases of `PlayerAction`, which a replay writes as cases it never meets? A set of their own keeps the replay's format as it is; one set would let a later recorder of the setting screens, or the out-of-process bridge, see every action through one point.
- **The trace's orders as an action:** `GiveOrders` keeps the rule that the battle changes only through actions, at the cost of a case that only a trace gives, of the globals rather than of a click. The other way is a write the API allows the input's holder alone, which a recorder does not see.
- **The uninitialized locals' answers** change the battle, but no replay records them. That holds while the app does not record a game that follows a trace. Should a replay record them, as it records the ticks, so that such a game could be recorded and played again?
- **The texts:** in the core, as above, or in `OpenNspw.HostUI`, with the game's text writers public?
- **`AskSystemMenu` and `ShowMinimapCell`** as entry points rather than actions: a replay would otherwise open the system menu's question as it plays.
- **`ResultChecking` and `OrdersStarting` in a network battle**: nothing reads them there yet. Raised in every mode, the points mean the same everywhere; raised only in a single-player game, the network game's code is not touched.
- **Names:** `ActionSite`, `ResultChecking`, `UninitializedLocal`, `OpenNspw.HostUI`.
