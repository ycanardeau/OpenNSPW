# Computer players as extensions
A design: the strategy of 1.22's computer (`cpu_strtgy`, `ComputerStrategy.cs`) leaves the core for a project of its own, `OpenNspw.ComputerPlayers`, as one computer player among others, on the extension API ([Extensions.md](Extensions.md)). The core keeps what the battle and the scenarios need: the fleets, the routes, the decision.

## Why
- **Reuse.** The strategy's steps (the fleets' upkeep, the planes, the targets, the movement, the strikes, the formations) are private methods of `Nspw`. A new computer player cannot call them, only copy them. In a project of their own, split into parts, a new computer player composes them, replaces one, or leaves one out.
- **A smaller core.** With 1.22's strategy one computer player among others, the core is the battle's rules and 1.22's scenarios, and any scenario can be played by any computer player.
- **The fleets as the contract.** 1.22's scenarios already tell the computer what to do through the fleet table ([ComputerPlayer.md](ComputerPlayer.md#fleets)). Kept in the core, it is what a scenario gives every computer player: who leads, who follows, what to attack, where to go.

## What stays in the core
| What | Why |
| --- | --- |
| The fleet table (`ComputerFleets`, `ComputerFleet.cs`), cleared as `game_init` clears it (`SetUpSingleplayerScenario`) | The scenarios set it up (`Scenarios122.cs`), `decision` changes it (scenario 303's `TargetSurrenderedPlanes`), and the emulator copies it in and out |
| The target lists (`ComputerFleets.cs`: `AddTargetsOfKind`, `AddNavalBaseTargets`, `AddCarrierTargets`, `AddShipTargets`, `ShuffleTargets`, and `MemberAt`, `TargetAt`, which read on past a list's end as 1.22 does) | The scenarios build their fleets with them, over 300 calls. They become public, for the strategy's events |
| `Ftol` (`crt.cs`), 1.22's `__ftol` | The scenarios convert with it too |
| The routes (`PlanComputerRoute`, `SetCpuRoute2`, `ReplanBlockedRoute`, `CheckCourseAheadAs122`) | The movement calls them: when it gives the computer's order its path (`ApplyOrders`), for a ship that follows a leader, and for a blocked ship. They are how 1.22 moves the computer's ships, not what it decides |
| `decision` (`Decide`) and its events | `CheckResult` calls it, for every scenario, whoever plays |
| `cpu_strtgy`'s uninitialized local (`StackLocal(0x4050C7)`, `StackLocalsOnWindows` and its BUG(1.22) mark) | The core reads every uninitialized local, and asks the extensions for its value (`UninitializedLocal`); the strategy reads it through a public query |
| The emulator's glue (`RunCpuStrategy`, `RunSetFormation`, ...) | Only the tests use it, and it moves later if at all ([FeatureExtensions.md](FeatureExtensions.md#the-emulator-later)) |

## What moves
`ComputerStrategy.cs`, but for the target lists, into `OpenNspw.ComputerPlayers` as `ComputerPlayer122`: the scenarios' events (`ApplyComputerEvents`), the steps of a fleet, the formations (`SetFormation`), the placement of reinforcements (`PlaceReinforcement`), scenario 304's reinforcements (`ReinforceScenario304`) and the order (`OrderFleet`). The events go with the strategy: they are `cpu_strtgy`'s code, and they draw `rand()` in its order.

What it needs of the core is public already, but for four members, which become public:

| Member | Now |
| --- | --- |
| `FleetAt(f)` | Internal |
| The target lists | Internal |
| `StopWhereItIs(ref Unit)` (`ComputerPlayer.cs`), which `decision` calls too | Private |
| `StackLocal(instruction)` | Private |
| `MemberAt`, `TargetAt`, `Ftol` | Private |

The rest is public: `Units`, `MaxUnitId`, `ComputerFleets`, `MoveOrders`, `Selections`, `CpuSide`, `ScenarioNumber`, `BattleTime`, `Renderer.FrameCount`, `AddUnit`, `AddPlane`, `CountPlanesIn`, `rand()`.

## The seam
The core's API gets a computer player of its own kind:

- `IComputerPlayer`, with one method, `void Plan(Nspw game)`: the computer's order of the frame, in `MoveOrders[0]` and `Selections[0]`, and its writes to the units and the fleet table.
- `Nspw.ComputerPlayer`: the one that plays the computer's side, set by the host before the battle starts. One slot rather than a list of extensions: one computer player plays a side, and a replay names it.
- The game calls it where 1.22 runs `cpu_strtgy` (`IssueSingleplayerOrders`), when the computer plays a side and the battle is not held (`IsBattleHeld`).
- A single-player battle whose computer plays a side does not start without one: the host is told at once, rather than the computer standing still. In scenarios 1 and 2 the computer plays no side, and needs none.

## The rule for computer players
An extension changes the battle only through the player's actions and the points' interventions (Extensions.md, Rules). A computer player is the exception: 1.22's writes the units directly, in the middle of the frame, in the order it draws `rand()`, which no set of actions could give in the same order. So a computer player may write the battle, within limits that keep it as reproducible as the core:

- **Deterministic.** It reads only the battle and draws only the game's `rand()`: no clock, no thread, no file, no randomness of its own.
- **At its point only.** It writes only inside `Plan`.
- **No state of its own between frames.** What it keeps is in the game: the fleet table, the units. A replay that starts again, and a keyframe, a copy of the game ([Replays.md](Replays.md)), then carry it whole. A computer player that needs more gets a general place in the game for it, not a field of its own.
- **No allocation in a frame**, as the core's battle (`AllocationTests`).

A computer player in a network battle, one of the players rather than the computer's side, is another seam: it plays through the player's actions, as the host UI does, and is not this design's.

## Replays
- A replay names the computer player at its start, with the scenario: an id (`"1.22"`), from version 4 of the format.
- The replay's player plays it again with the same computer player, which the host gives it by id. A replay whose computer player the host does not have is refused, as a replay of another version is; one of an earlier version has none, and plays with 1.22's.
- The computer's orders are not recorded: the same computer player gives them again. What it decided can still be noted (`ReplayRecorder.Note`).

## The parts
`ComputerPlayer122`'s strategy (`Strategy122`) is split by the steps of `cpu_strtgy` ([ComputerPlayer.md](ComputerPlayer.md#cpu_strtgy)), each part a struct over the game, as the strategy is, so that a frame allocates nothing. The parts call none of each other; what they share is in `StrategyBasics`: the flow between them (`StrategyFlow`), the end of a path, the kinds the code names together, 1.22's distances, and the packing of the fleets' lists.

| Part | Steps | What it does |
| --- | --- | --- |
| `ScenarioEvents` | 1 | `ApplyComputerEvents` |
| `FleetUpkeep` | 3, 4 | `FindComputersTarget`, `KeepUpFleet` |
| `FleetPlanes` | 5, 6 | `ArmComputersPlanes`, `FlyComputersPlanes` |
| `FleetTargets` | 7, 8 | `ChooseTargetOf`: the ships' and the planes' in one loop, as 1.22 draws `rand()` for both in it |
| `ReturnAndSupply` | 9 | `DecideToReturn`, `SupplyAtBase`, `StopAtTheEdge` |
| `FleetMovement` | 9 | `MoveFleet`, and the moves of warships, carriers and submarines it chooses from |
| `FleetStrikes` | 10, 11 | `StrikeFromFlagship`, `BombFromAirBase`, `StrikeFromOtherCarriers`, `DefendAirBase` |
| `FleetFormation` | 9 | `SetFormation`, `OrderFleet` |
| `Scenario304Reinforcements` | 12 | `ReinforceScenario304`, and the placement of reinforcements (`PlaceReinforcement`) |

`Strategy122.Plan` composes them in 1.22's order, with the flow between them. The parts keep 1.22's assumptions as they are: unit 23 as the computer's home, Japan's longer ranges, the thresholds of the time left, ten fleets of at most 17 members and targets. A part that a new computer player needs otherwise gets a setting for it, with 1.22's value as its default, one at a time, each tested against the emulated original with 1.22's values.

## What the hosts change
- **The desktop app** sets `ComputerPlayer122` on every single-player battle, and gives the replays' player the computer players it knows.
- **The tests** set it on every single-player game they make: `SingleplayerTests`, the characterization traces, `Trace122Tests`, `Lockstep/SingleplayerRun`. A helper that makes the tests' games sets it, so that none forgets. `ComputerPlayerTests` compare `RunCpuStrategy` with `ComputerPlayer122`'s strategy (`Strategy122`).

## Steps
Each a commit that builds, and keeps the characterization traces, the trace tests and `ComputerPlayerTests` passing.

1. **The seam, in the core:** `IComputerPlayer`, `Nspw.ComputerPlayer`, and `ComputerPlayer122` in the core, whose `Plan` calls `PlanComputerStrategy` unchanged; the battle does not start without one; the hosts and the tests set it.
2. **Public:** `FleetAt`, the target lists, `StopWhereItIs`, `StackLocal`.
3. **Replays:** the computer player's id at the start, in the next version, and the replays' player given the computer players by id.
4. **`OpenNspw.ComputerPlayers`:** `ComputerStrategy.cs`, but for the target lists, moves into `ComputerPlayer122`'s strategy, `Strategy122`: a struct over the game it is given, which it reads rather than its own fields, so that a frame's strategy allocates nothing; the app, the tools and the tests reference the project. `docs/ComputerPlayer.md` names the new place.
5. **The parts:** `ComputerPlayer122` split as above, with nothing changed in what it does.
6. **Later:** settings of the parts, one at a time; new computer players; scenarios as extensions, a design of their own.

## Open questions
- **A slot or a point:** `Nspw.ComputerPlayer`, or a point of `IGameExtension` where the first extension that answers plays the frame, as `UninitializedLocal` answers? A slot says that one plays; a point composes as the other extensions do.
- **No default:** the core cannot fall back on 1.22's computer player once it is in a project of its own, so every host sets it. Should 1.22's stay in the core as the default instead, and only new computer players be extensions? That keeps every host as it is, but not the reuse of its parts.
- **The scenarios' events:** with the strategy, as 1.22 has them, or in the core, called just before `Plan`, so that any computer player of 1.22's scenarios gets them? They come first in `cpu_strtgy`, after it clears its order, so they would draw `rand()` in the same order. Scenario 304's reinforcements come at the end, within the strategy's flow, and stay with it either way.
- **Names:** `IComputerPlayer`, `ComputerPlayer122`, `OpenNspw.ComputerPlayers`, the id `"1.22"`.
