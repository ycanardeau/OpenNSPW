# UI
The app (`OpenNspw.App`, which the desktop host `OpenNspw.Desktop` starts) plays the port's games with a UI of its own, built with [Saruhashi](../Saruhashi) controls: a main menu, a browser of NSPW 1.22's scenarios, a form to host or join a game, the setting screens, and the battle screen. The game is the exact port and does not know about the UI: the UI reads its state and gives it the orders that the original's clicks give, so a game played with the UI sends the same network messages as one played with the original's screens.

## The original's frame
The original mixes drawing, input and the simulation. A battle frame (`UpdateFrame`, `win_main.cpp`) is:

1. `ReadInput`: the mouse and the keyboard, through DirectInput.
2. `UpdateBattle`, once or more: the simulation.
3. `UpdateUnitInfo`, `DrawBattleArea` and `DrawMinimap`: drawing the sidebar, the battlefield and the minimap, **and** taking the clicks on them. A click writes what the player wants into globals as it is drawn: `HandleUnitClick` sets `SelectOrders[1]`, `DrawPathAndSetDestination` sets `MoveOrders[1]`, `UpdateCombatMenu` sets the combat menu's choice.
4. `HandleInput`: turns those globals into the orders of the turn, which `SendOrders` sends to the rival.
5. The frame is shown.

The setting screens (`UpdateGameSetting`, `demo.cpp`) work the same way: each text that can be clicked is drawn and tested against the cursor in one place.

## How the UI works with the game
### Commands
What a click does in step 3 is extracted into functions, as refactorings that keep the [characterization traces](Refactoring.md) unchanged: `ClickUnit`, `SetDestination`, `CancelSelection`, `ChooseCombatMenuItem`, `RequestSupplyUnit`, `AskSystemMenu`, `ChooseScenario`, `StartBattle` and the others. The original's code calls them where it takes a click; each raises the player's action (`PlayerAction`, `SettingAction`), and the UI gives them through commands (`UICommands`, in `OpenNspw.HostUI`), an extension of the game ([Extensions.md](Extensions.md)) that the app adds to every game.

A command is queued on the UI's thread and run on the game's, at the site where the original takes the click it stands for (`ActionSite`, `IGameExtension.InputSite`), as the action the click does (`Nspw.Do`): the battlefield, the minimap, the switch of a carrier's view, the combat menu, the supply panel, the system menu, the scenario list, the buttons under it, and the rules. It checks what the click checks, through the game's queries (`Queries.cs`), and is dropped where the click would be dropped: while an order waits for its turn, or where the part it belongs to is not shown. A press of either button sounds where the game sounds a press (`Press`). Steps 4 and 5 are untouched, so the orders and the setting messages are the original's.

`UICommandTests` plays the transports' landing of ミッドウェイ攻略１, a right click and a click on the selected unit, once with the mouse and once with the commands, and requires the same traces from both players.

### The snapshot
The game runs on its own thread. As it shows a frame (`Present`, under the frame's lock), it copies what the UI shows into a snapshot (`GameSnapshot`): the units, the fires, the effects, the clouds, the map's tiles and the sprites, which keep the original's layout, and the globals the screens read, such as the camera, the selection, the side, the mode, the result and the setting screens' state. What the original works out as it draws is read on the game's thread with the same conditions, put together from the game's queries (`UIState`, in `OpenNspw.HostUI`): the tiles and effects the player sees (`ReadSight`), whether a click counts on each unit (`ReadClickable`), the planes on the deck shown (`ReadShownOnDeck`), what the game writes over its screens (`ReadNotices`, `ReadChat`); and the texts the game writes, in its own words, are its queries (`ReadUnitDetails`, `ReadScenarioNames`, `ReadResult`, ...). The UI draws from the last snapshot.

### The game's own drawing
On the screens the UI draws (a battle but the map editor's, and the setting screens), the game still draws each frame as the original does, and the UI does not show it. Step 3 cannot be skipped, because it writes more than pixels: the effects take the players' shared random numbers (`SharedRandom`), which must stay in step, or, in a single-player game, `rand`, as 1.22's do; `DrawBattleArea` clears the turn's orders; `ListCombatMenu` sets the choice shown; the chat lines count down as they are drawn. Nor can its drawing: at a speed other than 1, or with Space held, the original does not wait between frames, so a frame that took less time to draw would run the battle faster and play its sounds more often, such as a flying plane's, every tenth frame. The title screen and the map editor are drawn by the game.

### Threads and input
The game's thread starts when a game is chosen (`GameSession`), and the main menu comes back when it ends. Going back never waits for the game: it is asked to close, as closing its window or cancelling its dialog does, and ends on its own thread, stopping its sounds; only quitting the app waits for it, three seconds at most, so that it can leave the session.

The game's dialogs are not in the snapshot. The UI reads them where the game keeps them, its windows (`HWND`), under `WindowsLock`, and changes them only through the game's message queue (`PostToGame`, `PostMessage`). A dialog is created in two steps on the game's thread: it joins the windows with its template's fields, then its procedure sets them as it handles `WM_INITDIALOG`. As in Win32, which shows it only after, the UI takes a dialog only once `HWND.Initialized` says it has handled `WM_INITDIALOG`. Otherwise the multiplayer form could fill itself from the empty template, once and for good: an empty name, and a host listening on DirectPlay's default port rather than the one saved.

The keyboard goes to the game as it would to the original, but for Escape and F12, which close the game at once in the original: the UI asks first (`ConfirmForm`). In a single-player game in a Release build, the debug keys that swap the player's side, start the scenario again or sink a unit (F5, F6, F9) are held back. A mouse press on a form of the UI is the form's; the game only sees the presses elsewhere, such as on the title screen. While the setting form is shown, the game is given a cursor of its own away from its texts (`SettingForm.GameCursor`), as the original chooses a scenario by the cursor's place alone.

## Screens
### Main menu
Single player, multiplayer, replays, settings and quit, over the title's picture. The settings (`SettingsForm`) have a tab for each of their sections: General, the language, which shows the menus in it again; on a touch screen, Touch, how a touch screen is played (`TouchSettings`), which the battle's Settings button also shows, over the battle; and in a Debug build, Debug, the options of single-player battles ([Options.md](Options.md)), which a player's battles do not play by.

### Single player
NSPW 1.22's scenarios, a set at a time, as its scenario selection screen lists them, with what its setting screen says about the one selected: its side, its briefing and its victory conditions. The strings are `NSPW_122.exe`'s, kept in C# (`ScenarioSelection.Sets`), so that the app needs no executable. Choosing one starts the game as `--singleplayer` does.

### Multiplayer
The player's name, hosting or joining, the address and the port, and the scenario editor. The form stands in for the original's connection dialog, which the game still creates: it shows what the dialog has, and types into and clicks the dialog as the user does (`ClickDlgItem`), so that the game connects as the original does.

### Replays
The battles played, which the app records ([Replays.md](Replays.md)), newest first, a page at a time: when, single-player or against whom, the scenario, how long and how it ended. One is watched in the battle screen, with a bar along the battlefield's bottom (`ReplayBar`): play or pause, a step, the speed, the timeline, the player's view or a free view, both sides, and taking a single-player battle over. While it plays, the battle takes no clicks; the middle button, the minimap, and W, A, S and D move the free view.

### Setting screens
`SettingForm`: the scenarios a page at a time, with what the setting screen says about the one chosen, the sides, and the starts from the resume-saved and the auto-saved battles; then the rules, each with its value in the game's words (`RuleRow`). The host chooses and the guest sees the host's choices as the host sends them; the host's buttons wait for the guest to be on the same screen, as the original's clicks do. The map editor goes through the same form.

### Battle
`BattleForm`, laid out as the original's screen:

| Part | Rectangle | What the original draws there |
| --- | --- | --- |
| `Battlefield` | (0, 0, 768, 768) | `DrawBattleArea`: the tiles in sight, the lower effects, the ships, the clouds, the planes and the upper effects, then the marks |
| `Deck` | (768, 0, 120, 438) | `DrawUnitDetails`'s picture: the unit, or a carrier's or an air base's flight deck or hangar with the planes parked there |
| `InfoPanel` | (888, 0, 136, 438) | The unit's details, the speed and the time, the supply panel with the chosen reinforcement's picture, blinking while one is coming, and the host's system menu |
| `CombatMenu` | (768, 438, 256, 131) | `UpdateDeckView`'s switch and `UpdateCombatMenu`'s orders, or the side's flag while there are none |
| `Minimap` | (768, 569, 256, 199) | `DrawMinimap`: the map, the units, and the battlefield's box |

On a screen wider than the layout's 4:3 (`WideViewportAdapter`), the screen is as high as the window and the rest of its width is beside the layout: the other screens are centered in it, and the battle's layout is at its left, with a column of its own at the right (`BattleForm.Fit`). The column has the **Panel** and **Controls** buttons at its top, the command bar under them, and the minimap at its bottom. The rest of the sidebar, the deck, `InfoPanel` and `CombatMenu`, is a panel left of the column, which **Panel** shows and hides, over the battlefield, at the screen's left edge on a touch screen, for the left thumb; it is hidden at first on a touch screen, and opens as one of the player's carriers or air bases is selected, for the planes on its deck (`BattleForm.FollowSelection`). The battlefield takes the room up to the column, as far as its zoom lets it: at most the game's view's width times the zoom, as it shows no more than the game's view. On a screen too narrow for the column, the layout is the original's.

The sprites are cut from `t3.bmp` as the original cuts each kind, with magenta as the color key (`BattleArt`). A left click on the battlefield takes the land where the selected transport lands, then the first unit by number whose sprite's middle has the cursor, then gives the selected unit a destination, as `DrawBattleArea` does; the right button deselects anywhere on the screen.

### Over the screens
What the game writes over its screens is drawn by the UI over its own (`GameMessages`): the result of the battle, the notices that the players are out of step or that the rival is gone, and the players' chat. The game's own dialogs, such as the one that waits for the rival or the system menu's question, are drawn as forms (`Dialogs.cs`) in front of the screens.

## Look
- **Colors and shapes** (`Controls/Theme.cs`): the navy of a chart table, brass for what can be chosen, and a chart's corner brackets on panels, over a sea chart's grid (`Screens/Backdrop.cs`). Nothing moves: the menus have no animation.
- **Fonts** (`BundledFonts`): the app comes with its fonts, embedded in it, so that its text is the same on every platform: BIZ UDGothic for the game's own text, fixed-width as the original's, and BIZ UDPGothic for the menus (`UIFonts`), under the SIL Open Font License.
- **Languages** (`Texts`, `GameTexts`, `SingleplayerTexts`): English and Japanese. The UI starts in the system's language and remembers the one chosen in the settings (`UISettings`, `ui.json` beside the game's settings). What the game writes stays Japanese in the snapshot and is put into English as the UI draws it, in the words of the game's English edition (its `LNGG_VER` code); the single-player scenarios' titles and briefings, which `NSPW_122.exe` has only in Japanese, are translated.
- **Keyboard**: the menus, the multiplayer form and the setting screens can be used with the keyboard as well as the mouse; in a battle the keys are the game's, but while a dialog of the game is shown, such as the chat, which takes the keys pressed, as the original's keyboard reads keys only while its window is the active one (`DISCL_FOREGROUND`).

## Differences from the original's screens
What the UI adds, each a way of giving the original's own clicks or of showing what the player already sees:

- **Scrolling by dragging with the middle button** on the battlefield (`ScrollBattlefield`, `MoveCamera`). The game takes each move in its next frame, so the UI shows the moves not taken yet on top of the game's camera (`ScrollAhead`), with the tiles read two around the battlefield (`SightMargin`) so that none is missing at its edges. The camera is only what the player sees, as the screen's edges and the minimap move it.
- **Selecting units by brushing over them** with the left button held, on the battlefield and on the deck (`UnitBrush`): each unit the mouse passes over is put in the selected unit's formation, or taken out, as the first one pressed was, by a click on it, one a frame, as the game takes one click on a unit a frame (`BrushUnit`, `RunBrushedUnits`). Only units that can be in the formation are brushed (`IsFormationMate`).
- **Zooming the battlefield** (`Battlefield.Zoom`): two fingers spread or pinched, or the mouse's wheel, magnify it from 1 to 2.5 times, around the fingers or the pointer. The battlefield shows a part of the game's view, its window: a drag moves the window within the view first and then the camera (`Pan`), and a click on the minimap moves both, so that the map's edges come into view (`ShowWindow`). A battle starts at the zoom the player chose last, or at one for the screen's size (`IAppHost.BattleZoom`): 1 on a desktop, and on Android 5 divided by the screen's short side in inches, from 1 to 2.5, about 1.8 on a phone and 1 on a tablet (`UISettings`, `BattleZoom`).
- **The command bar** (`CommandBar`): buttons for what the battle takes only from the keyboard and the right button, the speed, slower (G), back to 1 (R) and faster (F), fast-forward while held (Space), the chat (Enter), and deselecting (the right button). A key's button holds the key down while it is pressed, so the game takes it as the keyboard's, and a replay records it the same. **Controls** shows and hides it, remembered (`IsCommandBarShown`), shown at first on a touch screen only. Without the column, it is over the battlefield's top right corner, on a touch screen only: a desktop at 4:3 shows the original's screen whole. On a screen small enough to magnify the battlefield at first, its buttons are larger, in two columns (`CommandBarLayout.Large`), and the panel is drawn 1.35 times as large, as high as the screen (Saruhashi's `Control.Scale`).
- **A touch screen** (`IAppHost.IsTouchScreen`, `TouchPointer`): a touch is the left button, and a tap is a click once it is lifted without moving. Dragged, it pans the battlefield, or brushes over units from one of the player's (`UnitBrush`). Two fingers zoom, or deselect when lifted without zooming. On the minimap, which a finger makes far less precise and hides what it presses, a touch aims: a box shows what the battlefield would show from where it presses, moves a third of the way the finger moves (`Minimap.AimRatio`), and the battlefield moves there once it is lifted; the minimap is drawn up to twice as large in the column (`Minimap.Scale`).
- **Escape and F12 ask before quitting** (`ConfirmForm`).
- **The buttons do not flicker with the turns**: the system menu's stay on while the rival is in the battle, and the combat menu's while it shows them; a click waits, two seconds at most, for a frame in which the player can order (`RunWaitingSystemMenu`, `RunWaitingCombatMenu`), as a click given a moment later would.
- **The combat menu shows what is in effect** for the selected unit (`ReadActiveCombatMenuItem`): a ship's supply while it is supplying, a flying plane's mode, a parked plane's arms. The game's own choice (`CombatMenuSelection`) is set as an order is given but not as it ends, so its button would stay chosen after a ship's supply is done.
- **English**, as above.

What is drawn or taken differently:

- The original's white lines are the theme's marks: brackets around the selection, the targets' directions, the selected unit's target, landing point and path, and what a click would take. They are drawn after all the units rather than with each.
- The smoke's and the wakes' jitter takes the UI's own random numbers, since the game's would change the simulation.
- The minimap has no radar sweep, and its box is what the battlefield shows, magnified, rather than the game's whole view.
- On a wide screen, the game's cursor stays left of the layout's right edge while the pointer is right of it (`GameSession.UpdateMouse`): the edge scrolls the map, and the pointer there is in the column, not at the screen's edge.
- A held 増やす or 減らす steps the swap time and the initial supply points every frame in the original; a button steps them once a click.
- In a single-player game, the result is written in NSPW NET's words (`ReadResult`); 1.22's decision writes its own, which the UI does not read.

## Not done
- **The dialogs are read live** (see [Threads and input](#threads-and-input)). The lock keeps the windows consistent, but a dialog changes in several steps, and the UI can read it between them; `MultiplayerScreen.Update` even reads whether hosting is checked and OK is enabled without the lock. The way out is the battle's: the game's thread would publish an immutable copy of each dialog (its fields, checks, enabled states and list items) after `WM_INITDIALOG` and after each message it dispatches, and the UI would read only those copies and keep writing through `PostToGame`. Only the Win32 layer (`winuser.cs`) and the app would change, not the ported game, and the app would no longer take `WindowsLock`.
- **The title screen and the map editor** are still drawn by the game, and the chat is still typed into the game's dialog.
- **The controls have no tests of their own**: they live in `OpenNspw.App` and draw with MonoGame. Moving what they compute (which unit a click takes, what a part shows) into a project the tests can use would let a saved snapshot be tested without a window.
- **The debug lines** that the original's `DBG_MODE` writes at the top of the screen, and the keys that page them (F3, F4), are shown in every build, as the original's release shows them.
