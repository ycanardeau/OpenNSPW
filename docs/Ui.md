# UI
A proposal for giving the game a UI built with [Saruhashi](../Saruhashi) controls, as [OpenNSPW/OpenNSPW](https://github.com/OpenNSPW/OpenNSPW) has: a main menu, single-player and multiplayer screens, and a battle screen made of a battlefield, a sidebar and a radar.

This is a proposal, not a final design. [Open questions](#open-questions) lists what still needs to be decided.

## Background
OpenNSPW/OpenNSPW is a rewrite. Its UI is a set of screens (`MainMenuScreen`, `SingleplayerScreen`, `MultiplayerScreen`, `GameScreen`) and controls (`MainForm`, `Battlefield`, `Deck`, `Radar`, `ScenarioBrowserForm`). Each control draws the world it is given and dispatches typed orders (`FlightModeOrder`, `AirplaneWeaponOrder`, ...) to it. Drawing, input and the simulation are separate.

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
- **Screens and controls follow OpenNSPW/OpenNSPW's**, with the same names and layout: a 768×768 battlefield and a 256-pixel sidebar on a 1024×768 screen, which is also the original's layout (`CMBT_WIDTH`, `CMBT_HEIGHT`).
- **Graphics come from the original's data**: the map, the units and the effects are drawn from `t3.bmp` with the source rectangles the original uses, so no content pipeline is needed.

## Screens
### Main menu
Single player, multiplayer and quit. The game's thread no longer starts with the app: it starts when a game is chosen, with a new `Nspw`, and returning to the menu ends it (`WM_CLOSE`).

### Single player
A scenario browser over NSPW 1.22's scenarios, a set at a time with their headings and titles, as its own scenario selection screen lists them: the strings are read from `NSPW_122.exe` (`ScenarioSelection`). Choosing one starts the game as `--singleplayer` does, which stays for scripts.

### Multiplayer
Hosting or joining: the player name, the address and the port, and the original's scenario editor. The form stands in for the original's connection dialog, which the game still creates: the form shows what the dialog has, and fills in and clicks the dialog (`ClickDlgItem`) as the user does, so the connection is the original's. Switching between hosting and joining clicks the dialog's own check box, which sets the address and the port again, as in the original. Once the dialog is gone, the game's waiting dialog follows, drawn by `Dialogs.cs`.

After connecting, the host chooses the scenario and the guest waits, then both choose sides and rules: the original's setting screens (`UpdateGameSetting`). They first stay drawn by the game. Their forms come later, and work like the battle's: functions extracted from the screens' click handling, called by the forms, so the setting messages stay the same.

### Battle
`MainForm`, as in OpenNSPW/OpenNSPW:

| Control | Bounds | What the original draws there |
| --- | --- | --- |
| `Battlefield` | (0, 0, 768, 768) | `DrawBattleArea`: the map, the units, the effects, the clouds, the paths, the selection and the targets |
| `Deck` | sidebar (0, 0, 117, 436) | `UpdateDeckView`: a carrier's flight deck or hangar |
| Unit details | sidebar, right of the deck | `DrawUnitDetails`: the name, the damage, the ammunition |
| Combat menu | sidebar, below the deck | `UpdateCombatMenu`: one button per `CombatMenuItem` |
| Supply panel | sidebar | `UpdateSupplyPanel`, `RequestSupply` |
| System menu | sidebar | `UpdateSystemMenu` |
| `Radar` | sidebar (0, 572, 256, 196) | `DrawMinimap` |

Each control reads the snapshot and calls the commands. The map editor and the chat keep the original's UI at first.

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

## Tests
- The extractions keep the characterization traces, as any refactoring does.
- A command test plays the same orders through the original's clicks and through the commands, in the lockstep runner, and requires the same state and network messages after every frame.
- The controls draw from a snapshot, so they can be tested without a game: a saved snapshot, a click, and the commands posted.

## Implementation steps
### 1. Screens
The main menu, the single-player scenario browser, and the multiplayer host and join form, in `OpenNspw.Desktop`. The game's thread starts from a screen (`GameSession`), and the main menu comes back when the game ends. The setting screens and the battle stay the game's.

Done. The menus' text is English, as OpenNSPW/OpenNSPW's and the original's dialogs are; what comes from the game, such as the scenarios' titles, is its own Japanese.

The menus have a look of their own (`Controls/Theme.cs`): the navy of a chart table with brass for what can be chosen, a bold font for headings, and a sea chart whose grid drifts under a sweeping radar (`Screens/Backdrop.cs`). The main menu shows the title's picture, slowly zooming, over the menu. The scenario browser shows, for the selected scenario, what NSPW 1.22's setting screen states: the side, the briefing and the victory conditions, read from the executable (`ScenarioSelection`). Each screen can be used with the keyboard as well as the mouse, and fades in from black. The game's own dialogs, such as the one that waits for the rival, are drawn in the same theme.

### 2. The battle screen's structure
`MainForm` with the `Battlefield`, the sidebar and the `Radar`, showing the game's own pixels of each area and passing the clicks through. The layout and the screens are in place before anything is redrawn.

Done. In a battle (not the map editor), `GameScreen` shows `MainForm`, whose parts are `FrameView`s: each shows its rectangle of the game's frame, point-sampled as the frame is. Whether a frame is a battle's is recorded as the game shows it (`DesktopPlatform.IsInBattle`), so the layout goes with the pixels. The parts tile the screen as the original draws it:

| Part | Rectangle |
| --- | --- |
| `Battlefield` | (0, 0, 768, 768) |
| `Deck` | (768, 0, 120, 438) |
| `InfoPanel` | (888, 0, 136, 438) |
| `CombatMenu` | (768, 438, 256, 131) |
| `Radar` | (768, 569, 256, 199) |

### 3. The snapshot and the commands
The snapshot in `Present`, and the extracted click functions, with the command test.

### 4. The sidebar
The deck, the unit details, the combat menu, the supply panel and the system menu as controls, drawn from the snapshot and calling the commands.

### 5. The battlefield and the radar
Drawing the map, the units, the effects, the paths and the selection from the snapshot and `t3.bmp`, and selecting, moving and targeting through the commands.

### 6. The setting screens
The scenario browser and the side and rules form for multiplayer, through functions extracted from `cnct_game_cont.cpp`.

### 7. Switching the original's UI off
Skipping step 3 of the frame when the new UI is on, after moving the state it writes out of it.

## Open questions
- Should the new UI be the default, with the original's behind `--original-ui`, or the other way round?
- The original's text is Japanese, and OpenNSPW/OpenNSPW's is English. Which should the new UI show?
- Should the controls live in `OpenNspw.Desktop`, as in OpenNSPW/OpenNSPW, or in a project of their own, so that the tests can use them without MonoGame?
- Should the new UI add what OpenNSPW/OpenNSPW has and the original does not, such as the game speed keys and scrolling by dragging with the middle button? Scrolling changes only what the player sees. The game speed changes the simulation, so it could only apply to the single-player mode.
