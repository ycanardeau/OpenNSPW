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

So the ticks of a battle at speed 4 are not the ticks of the same battle at speed 1, and it ends otherwise. Network battles go by the tick: their turns come at ticks (`Tick % TurnLength`), and NSPW NET's rules make none of the differences above.

## What it does
Every frame runs 2 ticks, as at speed 1, and a faster speed runs more frames instead: speed + 1 ticks every 88 milliseconds, 20 while Space is held. Everything that goes by the frame then goes as at speed 1, at every speed, and speed 1 plays as 1.22.

- `StartsFrame`: owes the ticks of the time since the last frame, at the speed, and starts a frame once it owes 2. It owes 250 milliseconds' at most, so that a game that draws slower than the pace runs as fast as it draws, and does not hurry once it can keep up.
- `FrameStarting`: 2 ticks. The speed shown stays the player's (`GameSpeed`), which the keys change as before.
- At speed 0 it leaves the frames to the game's timer: frames of no ticks, as 1.22 runs them. The computer still plays in them, a fleet a frame, as it does in 1.22.
- It leaves alone a battle whose input an extension gives (`HoldsInput`): a replay that plays, a trace of 1.22.

The fastest speed is as fast as the game draws frames: above it, the battle is the same, but slower than the speed shown.

Replays record the speed shown in each frame ([Replays.md](Replays.md)), which the ticks no longer tell.
