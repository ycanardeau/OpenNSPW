# Replays
A replay records a battle from one player's seat, with only what the game takes from outside: the seeds, the ticks and the view of each frame, what the player did, and, in a network battle, what the rival sent. Played again, the game computes the same battle from them, frame by frame. Replays are an extension of the game ([Extensions.md](Extensions.md)), in `OpenNspw.Replay`: the port does not know them.

They serve two purposes: watching a battle again, and studying one, such as what a computer player did and when, or the first frame where a battle, replayed after the code changed, is not the one recorded.

## What a replay records
| Record | When | What |
| --- | --- | --- |
| Start | `BattleStarting`, `BattleStarted` | The mode (single-player, network host or guest), the scenario, the date, the app's version; in a single-player battle rand's state, the last seed of `InitializeGame` and its options ([Options.md](Options.md)), which it plays again with, whatever the player's are now; in a network battle its settings (`NetworkBattleSettings`); and the files it started from: the map editor's scenario, or the saved battle it resumes |
| Frame | `FrameStarting` | The milliseconds since the last frame, which pace the replay, the frame's ticks, which cover the speed and Space, the speed shown, which the ticks do not tell at a steady speed ([SteadySpeed.md](SteadySpeed.md)), and whether it holds the battle |
| Camera | `Drawing` | The view, when it has changed |
| Action | `PlayerActed` | What the player did (`PlayerAction`), wherever it came from: the mouse, the UI's commands, a computer player, a key; a byte for its case (`ReplayActionCase`), then what the case has |
| Received, RivalLeft | `MessageReceived`, `RivalLeft` | A network battle's message of the rival, as its bytes, or the rival leaving |
| Hash | `FrameEnded` | A hash of the battle every 10 frames (`ReplayRecorder.HashInterval`, 1 to find the first frame that differs) |
| Note | Any time | A text for the frame, such as what a computer player decided (`ReplayRecorder.Note`) |
| Result | `FrameEnded` | The battle's result, as it has one |

The file (`.onspwr`) is compressed with gzip, and written as the battle goes, flushed every 100 frames: a game that stops without ending it leaves a replay that plays up to where it stopped. The format is in `ReplayFormat.cs`. Its magic has its version (`ONSPWRP3`): a replay of another version is told to be one, and refused, rather than read wrong, but for version 2, whose frames have no speed: it shows the speed that their ticks tell.

**The hash** covers the units, the fires, rand's state and the tick, but not a unit's sprite column (`SpriteColumn`): the drawing sets it for the units it draws, so it follows the camera and the cursor, and the battle never reads it.

## Playing one
`ReplayPlayer` holds the player's input (`HoldsInput`): the host's keys, clicks and commands are held back. It sets the seeds and a single-player battle's options, and starts a network battle itself (`StartNetworkBattle`) without connecting, over a DirectPlay of its own that sends nothing and gives the game the rival's messages after their frame, as DirectPlay gave them. The game's own out-of-sync check stays live: it compares the recorded rival's checksums with its own.

It decides when each frame starts: at the pace it was played (`Speed`, `IsPaused`, `Step`), or as fast as the game can up to a frame (`SeekTo`). While it seeks, plays as fast as it can, or its frames come later than their pace, as a battle played at a steady speed of 300 does at 1×, it shows a frame every 100 milliseconds and the one a seek ends with, and draws nothing of the others (`ShowsFrame`), which play the same ([Extensions.md](Extensions.md)). At each frame it gives the ticks, the camera before the drawing, and each action where the game takes the clicks of its part of the screen (`InputSite`); the system menu's answer and the debug keys, which the game takes between frames, after the frame. It compares the hashes and the result, and keeps the first difference (`Divergence`), but plays on.

At its end, or once taken over (`TakeOver`, single-player only), the game is the player's. A recorder added after it (`ReplayRecorder.Replay`) records the battle as it plays, with the frames' times as they were played, but keeps it in memory until the player takes it over, and only then opens its file: the new replay is the battle from its start, as recorded, then what the player did. A replay never taken over leaves none.

A network battle starts with what the rival's messages left of the turns before it: whether the first turn may go on (`CanAdvance1`, `CanAdvance2`) and the rival's checksums, which `InitializeGame` keeps. They are part of its settings.

## In the desktop app
- **Recording**: every battle but the map editor's is recorded in the user's folder, `Replays` beside `Files`, as `<date>-<time>-<mode>-<scenario>.onspwr`; a restart or another battle of the same network game starts another file. None is deleted but by the player.
- **The list** (main menu, Replays): the replays, newest first, with when, against whom, the scenario, how long and how the battle ended; Watch, Delete (after asking) and Open Folder.
- **Watching**: the battle screen, with a bar along the battlefield's bottom: play or pause, a step of one frame (the period), the speed (½× to 8×), whose sounds play at any speed, the view scrolled with W, A, S and D as the game scrolls the battlefield, and the game's speed that the player had, which the sidebar shows as the game did, as the frames' ticks tell it, the timeline, which marks the point the mouse is over with its time and goes where it is clicked or a drag of its knob is let go, the player's view or a free view, both sides or the player's, and Take over in a single-player replay, which records a new replay: the battle up to there, then the player's. Its game writes in a folder of its own, deleted as it ends, with the files the battle started from in front of the app's.
- **Seeking**: ahead, the game plays as fast as it can, without sound, drawing a frame every 100 milliseconds, which the bar follows. Back, the replay starts again and seeks ahead. A Release build seeks about 2,800 frames a second, so going back to the start of a battle of an hour, 41,000 frames, takes about 15 seconds.
- `--replay <file>` plays a replay at once, as it follows a trace of 1.22 (`.n122`, [Verification.md](Verification.md)).

### The free view
The battle keeps the recorded camera: the camera decides what the game draws, which draws random numbers. The viewer's camera is the UI's: dragged with the middle button or moved with the minimap, it reads what is seen around it (`ReadSight(camera, ...)`), and the battlefield is drawn from there. Both sides shows every unit, tile and effect. Neither changes the battle. While paused, a move of the view takes the game's state again at once (`ReplayViewExtension`).

## Not covered yet
- Seeking back without playing from the start: keyframes, copies of the whole game kept as it plays.
- A network replay has not been watched in the desktop app between two of its own games; the tests play both peers' replays alone (`ANetworkBattlesReplaysPlayAlone`).
