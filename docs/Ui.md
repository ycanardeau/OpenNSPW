# UI
A proposal for giving the game a UI built with [Saruhashi](../Saruhashi) controls: a main menu, single-player and multiplayer screens, and a battle screen made of a battlefield, a sidebar and a minimap.

This is a proposal, not a final design. [Open questions](#open-questions) lists what still needs to be decided.

## Background
A UI of screens and controls, each drawing what it shows and turning what the player does into orders, keeps drawing, input and the simulation apart.

This port keeps the original's UI, and the original mixes the three. A battle frame (`win_main.cpp`) is:

1. `ReadInput`: the mouse and the keyboard, through DirectInput.
2. `UpdateBattle`, once or more: the simulation.
3. `UpdateUnitInfo`, `DrawBattleArea` and `DrawMinimap`: drawing the sidebar, the battlefield and the minimap, **and** handling the clicks on them. A click writes what the player wants into globals as it is drawn: `HandleUnitClick` sets `SelectOrders[1]` or calls `set_the_slct_unit`, `DrawPathAndSetDestination` sets `MoveOrders[1]`, `UpdateCombatMenu` sets the combat menu's choice.
4. `HandleInput`: turns those globals into the orders of the turn (`BufferedMoveOrders`, `BufferedSelectOrders`, ...), which `SendOrders` sends to the rival.
5. `Flip`: the frame is shown.

The connection dialogs are Win32 dialogs, drawn by `OpenNspw.Desktop` with Saruhashi (`Dialogs.cs`). The title screen and the setting screens are drawn by the game.

## Summary
- **The UI writes what the original's clicks write.** Step 3's click handling becomes functions (`ClickUnit`, `SetDestination`, `ChooseMenuItem`, ...), extracted as a refactoring that keeps the traces. The original's drawing code calls them on a click, and the new UI calls them on its own events. `HandleInput` and `SendOrders` stay as they are, so the network messages are the same, whichever UI made them.
- **The UI reads a snapshot.** The game runs on its own thread. At the end of each frame, the game copies what the UI shows (the units, the fires, the effects, the camera, the selection and the mode) into a snapshot, as it already copies the frame's pixels. The UI draws from the last snapshot.
- **Commands run on the game's thread**, at step 3 of the next frame, where the original handles its clicks.
- **The original's UI stays**, behind a switch. The tests, the traces and the exact port use it.
- **The battle screen keeps the original's layout**: a 768×768 battlefield and a 256-pixel sidebar on a 1024×768 screen (`CMBT_WIDTH`, `CMBT_HEIGHT`).
- **Graphics come from the original's data**: the map, the units and the effects are drawn from `t3.bmp` with the source rectangles the original uses, so no content pipeline is needed.

## Screens
### Main menu
Single player, multiplayer and quit. The game's thread no longer starts with the app: it starts when a game is chosen, with a new `Nspw`, and returning to the menu ends it (`WM_CLOSE`).

### Single player
A scenario browser over NSPW 1.22's scenarios, a set at a time with their headings and titles, as its own scenario selection screen lists them: the strings are read from `NSPW_122.exe` (`ScenarioSelection`). Choosing one starts the game as `--singleplayer` does, which stays for scripts.

### Multiplayer
Hosting or joining: the player name, the address and the port, and the original's scenario editor. The form stands in for the original's connection dialog, which the game still creates: the form shows what the dialog has, and fills in and clicks the dialog (`ClickDlgItem`) as the user does, so the connection is the original's. Switching between hosting and joining clicks the dialog's own check box, which sets the address and the port again, as in the original. Once the dialog is gone, the game's waiting dialog follows, drawn by `Dialogs.cs`.

After connecting, the host chooses the scenario and the sides and the guest waits, then the host sets the rules: the original's setting screens (`UpdateGameSetting`), drawn by `SettingForm` since step 6. It works like the battle screen: functions extracted from the screens' click handling, called through commands, so the setting messages stay the same.

### Battle
`BattleForm`:

| Control | Bounds | What the original draws there |
| --- | --- | --- |
| `Battlefield` | (0, 0, 768, 768) | `DrawBattleArea`: the map, the units, the effects, the clouds, the paths, the selection and the targets |
| `Deck` | sidebar (0, 0, 117, 436) | `UpdateDeckView`: a carrier's flight deck or hangar |
| Unit details | sidebar, right of the deck | `DrawUnitDetails`: the name, the damage, the ammunition |
| Combat menu | sidebar, below the deck | `UpdateCombatMenu`: one button per `CombatMenuItem` |
| Supply panel | sidebar | `UpdateSupplyPanel`, `RequestSupply` |
| System menu | sidebar | `UpdateSystemMenu` |
| `Minimap` | sidebar (0, 572, 256, 196) | `DrawMinimap` |

Each control reads the snapshot and calls the commands. The map editor and the chat's input keep the original's UI at first.

## The snapshot
`Units`, `Fires` and `Effects` keep the original's layout, so the snapshot is a copy of their arrays, plus a few globals: `CameraPosition`, `SelectedUnit`, `Selections[1]`, `CombatMenuKind`, `CombatMenuSelection`, `LocalSide`, `FrameCount`, `Mode` and `Result`. The map is copied once per battle. The copy is taken in `Present`, under the lock that guards the frame.

The snapshot is read-only for the UI. Anything the UI derives (which unit the mouse is over, what a click would do) is computed from it with the same conditions the original's drawing code uses, shared rather than copied where possible.

## Commands
A command is one of the extracted functions with its arguments, posted with `PostToGame` and run at step 3. Until the original's UI is switched off, it must not also see the click: the host stops posting the mouse to the game while the new UI has it.

The functions are extracted under [Refactoring.md](Refactoring.md)'s rules for functions: one per commit, the characterization traces unchanged. For example, `HandleUnitClick`'s branch for a left click becomes:

```csharp
private void ClickUnit(int m, SpriteId sprite)
	{
	MoveOrders[1].Unit=0;
	if( SelectedUnit==m )
	// ... the original's branch, unchanged
	}
```

and `HandleUnitClick` calls it where the branch was.

## Switching the original's UI off
With the new UI, step 3 still runs at first: it draws into the back surface, which is not shown, and sees no clicks. This keeps the game the same as with the original's UI, because step 3 does more than handle clicks: `DrawUnit` plays the planes' sound, for example, and what else it writes has not been listed yet.

Skipping step 3 is a later step: each piece of state it writes moves to a function that runs either way, with the traces unchanged, before the drawing is skipped.

Step 7 switches off the drawing rather than step 3 (below): step 3 writes more than first thought, and some of it decides the battle.

## Tests
- The extractions keep the characterization traces, as any refactoring does.
- A command test plays the same orders through the original's clicks and through the commands, in the lockstep runner, and requires the same state and network messages after every frame.
- The controls draw from a snapshot, so they can be tested without a game: a saved snapshot, a click, and the commands posted.

## Implementation steps
### 1. Screens
The main menu, the single-player scenario browser, and the multiplayer host and join form, in `OpenNspw.Desktop`. The game's thread starts from a screen (`GameSession`), and the main menu comes back when the game ends. The setting screens and the battle stay the game's.

Done. The menus' text is English, as the original's dialogs are; what comes from the game, such as the scenarios' titles, is its own Japanese.

The menus have a look of their own (`Controls/Theme.cs`): the navy of a chart table with brass for what can be chosen, a bold font for headings, and a sea chart whose grid drifts under a sweeping radar (`Screens/Backdrop.cs`). The main menu shows the title's picture, slowly zooming, over the menu. The scenario browser shows, for the selected scenario, what NSPW 1.22's setting screen states: the side, the briefing and the victory conditions, read from the executable (`ScenarioSelection`). Each screen can be used with the keyboard as well as the mouse, and fades in from black. The game's own dialogs, such as the one that waits for the rival, are drawn in the same theme.

### 2. The battle screen's structure
`BattleForm` with the `Battlefield`, the sidebar and the `Minimap`, showing the game's own pixels of each area and passing the clicks through. The layout and the screens are in place before anything is redrawn.

Done. In a battle (not the map editor), `GameScreen` shows `BattleForm`, whose parts are `FrameView`s: each shows its rectangle of the game's frame, point-sampled as the frame is. Whether a frame is a battle's is recorded as the game shows it (`DesktopPlatform.IsInBattle`), so the layout goes with the pixels. The parts tile the screen as the original draws it:

| Part | Rectangle |
| --- | --- |
| `Battlefield` | (0, 0, 768, 768) |
| `Deck` | (768, 0, 120, 438) |
| `InfoPanel` | (888, 0, 136, 438) |
| `CombatMenu` | (768, 438, 256, 131) |
| `Minimap` | (768, 569, 256, 199) |

### 3. The snapshot and the commands
The snapshot in `Present`, and the extracted click functions, with the command test.

Done for the battlefield and the minimap; the sidebar's clicks are extracted in step 4, as its parts draw themselves.

- **The extracted functions**, one refactoring each: `CanClickUnit` and `ClickUnit` (a click on a unit), `CanSetDestination` and `SetDestination` (a click that gives the selected unit a destination), `AimLanding` (the land where a transport lands), `CancelSelection` and `RestoreSelection` (the right button), and `ShowMinimapCell` (a click held on the minimap).
- **The commands** (`UiCommands.cs`, `game.Commands`): `ClickUnit`, `ClickLand`, `SetDestination`, `RightClick` and `ShowMinimapCell`. The host queues them on any thread. `DrawBattleArea` runs the battlefield's where it takes the mouse's clicks, and drops them as it drops clicks while an order waits for its turn; `DrawMinimap` runs the minimap's where it takes a held click. Each checks what the click checks, plays its sound, and calls the same functions. A parked plane is clicked on the deck, in step 4.
- **The snapshot** (`BattleSnapshot`, now `GameSnapshot`): the units, the fires, the effects, the camera, the selection, the combat menu, the side, the mode and the result, copied in `Present` under the frame's lock, and taken by the UI with the frame's pixels. The map is to be copied once per battle, with step 5.
- **The command test** (`UiCommandTests`): the transports' landing of ミッドウェイ攻略１, then the right button and a click on the selected unit, played once with the mouse (`MouseInput`) and once with the commands (`CommandInput`). The guest's trace must be the same in every round, and so must the host's, but for its output and the mouse's globals (`crsr_pt`, `lf_btn`, `ri_btn`, `sprt`).

### 4. The sidebar
The deck, the unit details, the combat menu, the supply panel and the system menu as controls, drawn from the snapshot and calling the commands.

Done. The deck, which draws the units' sprites as the battlefield does, came with step 5.

- **`CombatMenu`**: the switch of a carrier's or an air base's view (飛行甲板, 格納庫) and the selected unit's orders, in the original buttons' words, over the side's flag. The game records the items it shows (`ShownCombatMenu`), because listing them also sets the choice shown (`ListCombatMenu`).
- **`InfoPanel`**: the unit's details, the speed and the time, the supply panel and the host's system menu. Its texts are the game's own (`UiState.cs`: `ReadUnitDetails`, `ReadSupplyTargetName`, `ReadSupplyRequestState`).
- **Extracted**: `ListCombatMenu`, `ChooseCombatMenuItem`, `ToggleDeckView`, `IsOnShownDeck`, `WriteUnitDetails`, `GetSupplyRequestState`, `RequestSupplyUnit`, `SelectPreviousSupplyTarget`, `SelectNextSupplyTarget` and `AskSystemMenu`.
- **Commands**: `ToggleDeckView`, `ChooseCombatMenuItem`, `SelectSupplyTarget`, `RequestSupply`, `ToScenarioSelection` and `Resume`, each run where its click is taken (`UpdateDeckView`, `UpdateCombatMenu`, `UpdateSupplyRequest`, `UpdateSystemMenu`), and dropped where the part is not shown.
- **The mouse**: a part that draws itself takes its own clicks (`BattleForm.Owns`), so the game is not given the presses there; a right click there deselects, as it does anywhere.

### 5. The battlefield, the minimap and the deck
Drawing the map, the units, the effects, the paths and the selection from the snapshot and `t3.bmp`, and selecting, moving and targeting through the commands.

Done. No part of the battle screen shows the game's frame any more (`FrameView` is gone), and the whole screen takes its own clicks.

- **`Battlefield`**: the original's sprites, cut from `t3.bmp` as the original cuts each kind, with magenta as the color key (`BattleArt.Draw`), in the original's order: the tiles the player sees, the lower effects, the ships, the clouds, the planes and the upper effects. The original's white lines become the theme's marks: the selection, the targets' directions, the selected unit's target or landing point, its path and the line to the mouse, and what a click would take. A left click takes the land where the selected transport lands, then the first unit by number whose sprite's middle has the cursor, then gives the selected unit a destination, as `DrawBattleArea` does; the right button deselects.
- **`Minimap`**: the map, a pixel a tile, redrawn when the tiles change; the player's units and the enemy's it has found; the battlefield's box. A click held on it moves the battlefield (`ShowMinimapCell`).
- **`Deck`**: the unit's picture, or a carrier's or an air base's flight deck or hangar, with the planes parked there and their effects, at the screen positions the game gives them. A click on a parked plane selects it, or makes it the selected unit's target, with `ClickUnit`, which now takes a parked plane on the deck shown (`IsOnShownDeck`).
- **What the player sees and can click is read on the game's thread** (`UiState.cs`): the tiles and the effects in sight (`ReadSight`, with the conditions of `DrawMapTiles` and the effects' functions), whether a click counts on each unit (`ReadClickable`, `CanClickUnit`), whether one gives a destination (`ReadCanSetDestination`), and the planes on the deck shown (`ReadShownOnDeck`). The snapshot copies them with the sprites, the clouds and the map, every frame: the map is 128 KB, and the map editor changes it.
- **Not drawn as the original draws them**: the smoke's and the wakes' jitter takes the UI's own random numbers, since the game's would change the simulation; the marks are drawn after all the units rather than with each.

### 6. The setting screens
The scenario browser and the side and rules form for multiplayer, through functions extracted from `cnct_game_cont.cpp`.

Done; the setting screens are in `demo.cpp`. The title screen, where the host clicks to go on, is still the game's.

- **`SettingForm`**: the scenarios, a page at a time (第一集, 第二集), with what the screen says about the one chosen (`ScenarioDetails`), the sides, and the starts from the resume-saved and the auto-saved battles; then the rules, each with its value in the game's words and the host's buttons (`RuleRow`). The guest sees the host's choices as the host sends them, and the host's buttons wait for the guest to be on the same screen, as the original's clicks do. The map editor goes through the same form.
- **Extracted**: `ChooseScenario` (the original chooses the scenario under the cursor), `LeaveGameSetting`, `TurnScenarioPage`, `SwitchHostSide`, `StartFromResume`, `StartFromAutoSave`, `ToggleDecision`, `IncreaseSupplyRate` and `DecreaseSupplyRate`, `IncreaseSwapTime` and `DecreaseSwapTime`, `ToggleSwapRule`, `IncreaseInitialSupplyPoints` and `DecreaseInitialSupplyPoints`, `CycleArrivalControl`, `BackToScenarioSetting`, `StartBattle`, and `WriteArrivalControl` for its text.
- **Commands**: run where the screens take their clicks, at three sites: the list (`ScenarioList`), the buttons under it (`ScenarioMenu`) and the rules (`Rules`), each before the periodic message that sends the host's choices, as the clicks are. A site drops the commands of the other screen's sites, so that a click given as a screen goes cannot count on the next one.
- **The cursor**: the original chooses a scenario by the cursor's place alone, so while the form is shown, the game is given a cursor of its own (`SettingForm.GameCursor`), away from its texts and from the screen's edges, which scroll the map in any mode.
- **The snapshot** is now `GameSnapshot`, with the setting screens' state and texts (`ReadScenarioNames`, `ReadScenarioDescription`, `ReadArrivalControl`).
- **The game's dialogs over the screen's forms**: Saruhashi's z-order is WinForms', the first child on top, so the dialogs, added as they open, were under the battle screen, and the system menu's question was hidden while the game waited for it. They are brought to the front (`Control.BringToFront`, new in Saruhashi).
- **Not the original's**: a held 増やす or 減らす steps the swap time and the initial points every frame in the original; a button steps them once a click.

### 7. Switching the original's UI off
Skipping step 3 of the frame when the new UI is on, after moving the state it writes out of it.

Done the other way round: step 3 runs as it does, and only its pixels are skipped. What it writes besides the pixels is more than the planes' sound: the effects' drawing takes the shared random numbers for smoke and wakes (`SharedRandom`), as many times as the player sees such effects, and the players' random numbers must stay in step; `DrawBattleArea` clears the turn's orders before taking the clicks; `ListCombatMenu` sets the choice shown; `DrawUnit` sets each unit's sprite column; the chat lines count down as they are drawn. Moving all of it out, unchecked by the traces, would risk the lockstep for little: with the drawing off, what step 3 costs is its logic, which is small.

- **The switch**: a host whose UI draws the battle and the setting screens says so (`Nspw.HostDrawsScreens`, set by `GameSession`). Each frame, as it starts, the game marks its back surface as drawn or not (`DirectDrawSurface7.IsDrawn`), by whether the screen is one the UI draws (`IsHostScreen`): a battle but the map editor's, or a setting screen. A surface that is not drawn takes `Blt`, `BltFast`, `TextOut` and `BitBlt` as before, checking and returning the same, but keeps its pixels. The title screen and the map editor are drawn by the game as before, and the tests, which do not set the switch, run as they did.
- **The frame**: the UI no longer converts the game's frames on the screens it draws itself (`GameSession.UpdateFrame`).
- **What the game wrote over its screens**, which the battlefield had stopped showing since step 5: the battle's result (`WriteResult`, extracted from `DrawResult`), the notices that the players are out of step or the rival is gone, and the players' chat, read for the UI (`ReadResult`, `ReadNotices`, `ReadChat`) and shown over the battlefield and the setting form (`GameMessages`).

## Beyond the original
What the new UI adds, each a way of giving the original's own clicks or of showing what the player already sees:

- **Scrolling by dragging with the middle button** on the battlefield (`UiCommands.ScrollBattlefield`, `MoveCamera`): the battlefield follows the mouse at once. The game takes each move in its next frame, so the UI shows the moves it has not taken yet on top of the game's camera (`ScrollAhead`, `LastCameraMove`), within the bounds the minimap keeps it in. The camera is only what the player sees, as the screen's edges and the minimap move it.
- **Selecting units by brushing over them** with the left button held, on the battlefield and on the deck (`UnitBrush`): a press on one of the player's units selects it, or puts it in the selected unit's formation, or takes it out; then each unit the mouse passes over is put in, or taken out, as that press did. Each is a click on the unit (`BrushUnit`, `RunBrushedUnits`), one a frame, as the game takes one click on a unit a frame: the original's own way of making a formation (`SelectFriendlyTarget`), which the next order takes with it. Only units that can be in the selected unit's formation are brushed (`IsFormationMate`): of its category, and no bases; a plane never brushes a carrier, which a click would make it land on; a parked plane brushes the planes parked on the deck shown, a flying one those flying.
- **The system menu's buttons stay on** while the rival is in the battle: the player can order on and off with the turns, so a click waits, two seconds at most, for a frame in which it counts (`RunWaitingSystemMenu`), as a click given a moment later would.
- **English and Japanese** (`Texts`, `GameTexts`): the UI starts in the system's language and remembers the one chosen on the main menu (`UiSettings`, `ui.json` beside the game's settings). The UI's own words are in both. What the game writes stays Japanese in the snapshot and is put into English as the UI draws it, in the words of the game's English edition (its `LNGG_VER` code), their spelling and grammar mended; the combat menu's words, which the original draws as pictures of Japanese, are new. The single-player scenarios' titles, briefings and victory conditions, which NSPW_122.exe has only in Japanese, are translated for the UI (`SingleplayerTexts`).
- **The minimap** has no radar sweep: it shows only the map, the units and the battlefield's box.

## Open questions
- Should the new UI be the default, with the original's behind `--original-ui`, or the other way round?
- Should the controls live in `OpenNspw.Desktop`, or in a project of their own, so that the tests can use them without MonoGame?
- Should the new UI add game speed keys? The game speed changes the simulation, so they could only apply to the single-player mode.
