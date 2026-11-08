# Steady speed
A single-player battle that plays the same at every speed: the same orders at the same ticks give the same battle, whether it runs at speed 1, at speed 10, or with Space held. An extension of the game ([Extensions.md](Extensions.md)), in `OpenNspw.Pacing` (`SteadySpeed`). The port does not know it, and plays as 1.22 does without it.

## Why a speed plays another battle
1.22 runs a frame on its timer, every 88 milliseconds, of speed + 1 ticks, 20 while Space is held (`TryStartSingleplayerFrame`). Much of the battle goes by the frame, not by the tick:

| What | When |
| --- | --- |
| The computer (`cpu_strtgy`) | Once a frame, for its fleet `FrameCount % 10 + 1`: at speed 1 each fleet plays every 20 ticks, at speed 10 every 110 |
| The player's orders | Once a frame, in `HandleInput` |
| Returning planes' approach (`ApproachesOnDrawnTick`) | On the tick that is drawn only, the last of the frame |
| The effects (`EffectsOnDrawnTickOnly`), which draw from rand as the battle does (`EffectsDrawFromRand`) | On the tick that is drawn only: how many random numbers a tick draws depends on the speed, and so does every one drawn after |
| Two fighters of the same skill (`FighterTiesBreakByFrame`), a reload (`FrameCount % 3`, `% 2`), a base built | By the frame count |
| A reinforcement requested (`SupplyCount`) | Comes after 150 frames of a ship's, 30 of a plane's |

So the ticks of a battle at speed 4 are not the ticks of the same battle at speed 1, and it ends otherwise. Network battles go by the tick: their turns come at ticks (`Tick % TurnLength`), and NSPW NET's rules make none of the differences above.

## What it does
Every frame runs 2 ticks, as at speed 1, and a faster speed runs more frames instead: speed + 1 ticks every 88 milliseconds, 20 while Space is held. Everything that goes by the frame then goes as at speed 1, at every speed, and speed 1 plays as 1.22.

- `StartsFrame`: owes the ticks of the time since the last frame, at the speed, and starts a frame once it owes 2. It owes 250 milliseconds' at most, so that a game that runs slower than the pace runs as fast as it can, and does not hurry once it can keep up.
- `FrameStarting`: 2 ticks. The speed shown stays the player's (`GameSpeed`), which the keys change as before.
- `ShowsFrame`: one frame every 88 milliseconds, as 1.22 shows its frames, every frame at speed 1. The others are not shown: they run as any other, their drawing too, which draws random numbers and takes the clicks, but draw no pixel, write no text and are not presented. Drawing the pixels is most of a frame's time: in a Release build a frame shown takes about 700 microseconds, one not shown about 70, of which its drawing code is about 37, so speed 300, 301 ticks every 88 milliseconds or about 1,700 frames a second, takes about an eighth of a core. The text a frame writes after its drawing (the debug lines, the chat) and the text it decodes are left out of a frame not shown, and so are the unit panel's speed and time, as they would make garbage in each of the many. The device contexts that the game takes several times a frame (`GetDC`) are used again once released. Such a frame allocates about 120 bytes in a Release build, the rest of the unit panel's texts formatted (`AllocationTests.AFrameNotShownMakesLittleGarbage`), and speed 300 about 670 KB a second with the app's own, with no full collection in 20 seconds. With the option `SharedEffectRandom` ([Options.md](Options.md)), the effects draw no random number of the battle's, and a frame not shown skips them (`Renderer.SkipsEffects`).
- At speed 0 it leaves the frames to the game's timer, and holds the battle in them (`HoldsBattle`): they run no tick, the computer does not play, the scenario's events and a reinforcement requested wait, the effects keep still, and the frames are not counted. The player looks around and orders, and the battle goes on from where it was, as if it had not been paused. In 1.22, a frame of speed 0 runs no tick, but the computer plays in it, a fleet a frame, so that a pause changes the battle.
- It leaves alone a battle whose input an extension gives (`HoldsInput`): a replay that plays, a trace of 1.22.

Above what the game can run, the battle is the same, but slower than the speed shown. The battlefield scrolls once a frame shown, as fast as in 1.22 at every speed.

## In the app
Steady speed is one of the options ([Options.md](Options.md)), for debugging: a Debug build of the app has a switch for it in its settings' Debug tab, off unless turned on, which it remembers (`ui.json`); a Release build plays every single-player battle at the original's pace. A battle that follows a trace of 1.22 never plays at a steady speed; a single-player replay does once it is taken over, if it was recorded at one.

Replays record the speed shown in each frame ([Replays.md](Replays.md)), which the ticks no longer tell.
