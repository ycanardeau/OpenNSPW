# Options
OpenNSPW's own options for a single-player battle, which neither original has: `BattleOptions` (`OpenNSPW/OpenNspw/BattleOptions.cs`). They are kept apart from `BattleRules`, which records only what 1.22 and NSPW NET do where they differ, each rule read from the original's code ([Differences.md](Differences.md)). Each option is off unless the player turns it on, so that a battle with none plays as 1.22's.

The game holds the options it plays by in `Nspw.Options`, which the host sets before the battle starts, or an extension in `BattleStarting`, as a replay sets those it was recorded with. Network games have none: they play by NSPW NET's rules, which both sides share, and none of the options is for them.

## Two kinds
| Kind | How it applies | Options |
| --- | --- | --- |
| Changes the battle's rules | `InitializeGame` plays by 1.22's rules changed by the options (`BattleOptions.Apply`), as a `with` on `BattleRules.Nspw122`: the battle's code reads the rules, and does not know the options | `SharedEffectRandom` |
| Changes when the frames run, not the battle | The host adds an extension for it ([Extensions.md](Extensions.md)); the record says only whether it does | `SteadySpeed` |

## Rule or option
| What | Where | Example |
| --- | --- | --- |
| What 1.22 and NSPW NET do where they differ, read from their code | A rule of `BattleRules`, with the address it was read at | `DeckPlacesDrawFromRand`: the place of a plane landed on deck, which 1.22 draws from `rand` (`0x402CA3`) and NSPW NET from its table. It is a rule of its own, apart from `EffectsDrawFromRand`, so that an option can draw the effects from the table and leave the place to `rand` |
| A choice that neither original has, which changes the battle | An option that changes 1.22's rules in `Apply`, by the rules there are: it adds none, as the rules hold only what the originals do | `SharedEffectRandom`, which plays by NSPW NET's `EffectsDrawFromRand` |
| A choice that changes when the frames run, not the battle | An option that the host plays by with an extension | `SteadySpeed` |
| A fix of a bug of 1.22 that 1.22 plays through, a rule marked `BUG(1.22)` | An option that plays by NSPW NET's value of the rule | None yet; `TorpedoPlanesCheckLandAlongUnsetHeading` would be one |
| A fix of a bug of 1.22 that 1.22 cannot play past: a crash, a write out of its memory | Neither: the port always fixes it, and marks the code `BUG(1.22)`. An option off would have nothing of 1.22's to play | `PlanComputerRoute` ends a route at point 62 of its path, where 1.22's `set_cpu_route` writes on through the units after the ship and crashes ([KnownIssues.md](KnownIssues.md#single-player-games-crashed-on-a-route-that-never-ends)) |

## SharedEffectRandom
1.22's effects, the smoke, the fires, the wakes and their animations, draw their random numbers from `rand`, as the battle does (`EffectsDrawFromRand`), and its drawing draws some for the effects in sight: what the player sees, and whether a frame is drawn at all, moves the battle's random numbers. A frame that is not shown ([SteadySpeed.md](SteadySpeed.md)) must still run the drawing code, or the battle would be another.

With the option, the effects draw from `SharedRandom`, as NSPW NET's do (`EffectsDrawFromRand` off). Its table, which 1.22 does not have, is made as the battle starts from `rand` as it is, which then goes on from where it was (`MakeSharedRandomTableAside`). The place of a plane landed on deck, which NSPW NET also draws from the table, is the one number of the battle that does, and the option leaves it to `rand` (`DeckPlacesDrawFromRand`): the battle reads nothing that the effects draw, and plays the same whether they are drawn or not. A frame that is not shown ([SteadySpeed.md](SteadySpeed.md)) then skips the effects' loops, `UpdateLowerEffects` and `UpdateUpperEffects` (`Renderer.SkipsEffects`), which only test what the player sees and draw: in the app at speed 300, in a Release build, its drawing takes about 89 microseconds rather than 116 in scenario 4, and 108 rather than 265 in scenario 7, which has many effects in sight, and where the game cannot keep up, it runs about a fifth more ticks (the game's thread, on a busy machine). The effects shown are the same, but their animations' numbers, drawn from the table, which the frames not shown no longer move. Network games never skip them: NSPW NET draws the place on deck from the table (`BattleOptionsTests`). The battle is not 1.22's: its `rand` no longer gives the effects their numbers, so every number after the first effect differs.

## SteadySpeed
The battle plays the same at every speed ([SteadySpeed.md](SteadySpeed.md)), with the extension `OpenNspw.Pacing.SteadySpeed`, which the host adds.

## In the app
The single-player scenario browser has a switch for each option, by the scenarios' title: Steady speed, on unless the player turns it off, and Shared effect table, off unless the player turns it on. The app remembers them (`ui.json`). A battle that follows a trace of 1.22 has none; a replay has those it was recorded with, a replay of version 2 none, and plays at a steady speed once it is taken over if it was recorded at one.
