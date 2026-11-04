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
- `ShowsFrame`: one frame every 88 milliseconds, as 1.22 shows its frames, every frame at speed 1. The others are not shown: they run as any other, their drawing too, which draws random numbers and takes the clicks, but draw no pixel, rasterize no text and are not presented. Drawing the pixels is most of a frame's time: a Release build runs about 210 frames a second shown, and enough not shown for speed 300, 301 ticks every 88 milliseconds.
- At speed 0 it leaves the frames to the game's timer, and holds the battle in them (`HoldsBattle`): they run no tick, the computer does not play, the scenario's events and a reinforcement requested wait, the effects keep still, and the frames are not counted. The player looks around and orders, and the battle goes on from where it was, as if it had not been paused. In 1.22, a frame of speed 0 runs no tick, but the computer plays in it, a fleet a frame, so that a pause changes the battle.
- It leaves alone a battle whose input an extension gives (`HoldsInput`): a replay that plays, a trace of 1.22.

Above what the game can run, the battle is the same, but slower than the speed shown. The battlefield scrolls once a frame shown, as fast as in 1.22 at every speed.

## In the app
The single-player scenario browser has a switch, Steady speed, on unless the player turns it off, which the app remembers (`ui.json`). A battle that follows a trace of 1.22 never plays at a steady speed; a single-player replay does once it is taken over.

Replays record the speed shown in each frame ([Replays.md](Replays.md)), which the ticks no longer tell.
