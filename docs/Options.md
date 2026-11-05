# Options
OpenNSPW's own options for a single-player battle, which neither original has: `BattleOptions` (`OpenNSPW/OpenNspw/BattleOptions.cs`). They are kept apart from `BattleRules`, which records only what 1.22 and NSPW NET do where they differ, each rule read from the original's code ([Differences.md](Differences.md)). Each option is off unless the player turns it on, so that a battle with none plays as 1.22's.

The game holds the options it plays by in `Nspw.Options`, which the host sets before the battle starts, or an extension in `BattleStarting`, as a replay sets those it was recorded with. Network games have none: they play by NSPW NET's rules, which both sides share, and none of the options is for them.

## Two kinds
| Kind | How it applies | Options |
| --- | --- | --- |
| Changes the battle's rules | `InitializeGame` plays by 1.22's rules changed by the options (`BattleOptions.Apply`), as a `with` on `BattleRules.Nspw122`: the battle's code reads the rules, and does not know the options | `SharedEffectRandom` |
| Changes when the frames run, not the battle | The host adds an extension for it ([Extensions.md](Extensions.md)); the record says only whether it does | `SteadySpeed` |

## SharedEffectRandom
1.22's effects, the smoke, the fires, the wakes and their animations, draw their random numbers from `rand`, as the battle does (`EffectsDrawFromRand`), and its drawing draws some for the effects in sight: what the player sees, and whether a frame is drawn at all, moves the battle's random numbers. A frame that is not shown ([SteadySpeed.md](SteadySpeed.md)) must still run the drawing code, or the battle would be another.

With the option, the effects draw from `SharedRandom`, as NSPW NET's do (`EffectsDrawFromRand` off). Its table, which 1.22 does not have, is made as the battle starts from `rand` as it is, which then goes on from where it was (`MakeSharedRandomTableAside`). The place of a plane landed on deck, which NSPW NET also draws from the table, is the one number of the battle that does, and the option leaves it to `rand` (`DeckPlacesDrawFromRand`): the battle reads nothing that the effects draw, and plays the same whether they are drawn or not, so that a frame that is not shown could skip the effects (`BattleOptionsTests`). The battle is not 1.22's: its `rand` no longer gives the effects their numbers, so every number after the first effect differs.

## SteadySpeed
The battle plays the same at every speed ([SteadySpeed.md](SteadySpeed.md)), with the extension `OpenNspw.Pacing.SteadySpeed`, which the host adds.

## In the app
The single-player scenario browser has a switch for each option, by the scenarios' title: Steady speed, on unless the player turns it off, and Shared effect table, off unless the player turns it on. The app remembers them (`ui.json`). A battle that follows a trace of 1.22 has none; a replay has those it was recorded with, a replay of version 2 none, and plays at a steady speed once it is taken over if it was recorded at one.
