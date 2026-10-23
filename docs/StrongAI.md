# A strong player
A proposal for a computer player that plays the player's side of a single-player game, and beats NSPW 1.22's computer ([ComputerPlayer.md](ComputerPlayer.md)) in as many of its 37 scenarios as it can.

This is a proposal, not a final design. [Open questions](#open-questions) lists what still needs to be decided.

## Rules
- **It plays as a player does.** It sees what the player's screen shows: its own units, the enemy units that are found (`IsFound`), the map, the time left. It gives orders only with the commands of the host's UI (`UICommands`, [UI.md](UI.md#commands), which it adds to the game as an extension if the game has none), which are the player's clicks: one click on a unit a frame, dropped where a click would be dropped. It never writes the game's state, and uses no debug key.
- **It knows what a player knows from the briefing and the map** ([Scenario knowledge](#scenario-knowledge)): where the places the briefing names are, such as Pearl Harbor, Guadalcanal's airfield or Tokyo, which the map shows; what the victory conditions count; and roughly where the enemy is expected, as the briefing says or as a player learns from playing the scenario. It never reads where a hidden unit is.
- **It plays at the game's own speed**, speed 1 (2 ticks a frame), as a player starts. Holding Space or raising the speed gives the computer fewer frames, so fewer orders, for the same battle: a win at a higher speed does not count.
- **The game does not change.** The player is a separate project that the game knows nothing of; a network game and the characterization traces are untouched.

## Measuring
"Beats the computer" is measured, not judged by eye: an arena plays each scenario from several start dates, which seed rand and so the scenario's random setup and the battle, and counts the results that `decision` gives (`GameResult`): won, drawn, lost.

Each scenario is also played by two players that do not think, as baselines:

- **Idle**: gives no order. Several scenarios are won by holding on until the end (3, 7, 108, 204, 208, 304), and some may be won by doing nothing at all; a win that the idle player also gets says nothing of the strong one.
- **Rush**: sends its ships one after another at the nearest enemy found, as the tests' `Monkey` does, through the commands, and gives no other order. The `Monkey` itself does not fit: it clicks at random, holds Space and presses the speed keys, which the rules forbid, and it sends its ships at enemies that are not found.

The arena runs in Release, one game per core, each on its own thread with a platform of its own (`ArenaPlatform`), whose clock runs as fast as the game can: a frame costs about 1.2 ms, so a scenario of 60000 ticks, 30000 frames, takes about 40 seconds. The tests' lockstep runs, in Debug, cost about 17 ms a frame.

```bash
dotnet run -c Release --project OpenNSPW/OpenNspw.Arena -- --scenarios 4,5,6 --dates 3 --players idle,rush
```

## Scenario knowledge
A player reads the briefing and looks at the map before the battle. The strong player gets the same, per scenario (`ScenarioKnowledge`), written from the briefings ([SingleplayerTexts](../OpenNSPW/OpenNspw.Desktop/SingleplayerTexts.cs)), the victory conditions ([ComputerPlayer.md](ComputerPlayer.md#decision)) and the maps:

- **Places**: the harbors, airfields, islands and cities the briefing names, at the place of the map where they are. They do not move, and are the same in every scenario on the same map: Pearl Harbor on Oahu, Midway, Guadalcanal's airfield, Rabaul, Truk, Tokyo.
- **What counts**: the kinds of units the victory conditions count, which the player's targets favor, and what it must keep.
- **Where the enemy is expected**: an area, not a position, as wide as a player's guess: where the briefing puts it ("straight to the right of the Japanese fleet on the map", scenario 9), or the part of the map it comes from. A scenario whose enemy fleets start at random places gets an area that covers them all.

The scouts search the expected areas first; once the enemy is found, only what is found counts, as before.

## Shape
| Part | What it does |
| --- | --- |
| `OpenNspw.AI` | The player, a library on `OpenNspw`. |
| `OpenNspw.Arena` | The arena: plays scenarios × start dates × players, headless, stepped as `SingleplayerRun` steps a game, and writes the table of results. |
| `OpenNspw.Desktop` | `--ai`: the player plays the battle on screen, to watch it. |
| `OpenNspw.Tests` | Short tests: the player gives orders that count, in a few hundred frames of a few scenarios. |

The player acts between frames, while the game waits (in the arena, between steps; on the desktop, where the UI reads its snapshot), in three layers:

1. **Perception**: the picture of the battle that the player is allowed: its units by kind and fleet, the enemy units found, and where each enemy was last found and heading, kept after it is lost from sight.
2. **Plans**: what each group of units should do now, from the scenario's goal and the picture:
    - **Air operations**, NSPW's decisive weapon: arming the parked planes (torpedoes against ships, bombs against ground units), strikes with fighter escort against the targets found within the planes' reach, fighters kept back to intercept the computer's strikes, and scouting to find the enemy first.
    - **Surface operations**: battle fleets close on the enemies found, and keep between the enemy and the carriers; carriers keep out of the enemy's gun range.
    - **Logistics**: damaged, empty or low-fuel ships return to a naval base and supply.
    - **Landings**: transports land their cargo where the scenario needs it.
    - **The scenario's goal**: what `decision` counts for each scenario ([ComputerPlayer.md](ComputerPlayer.md#decision)), such as sinking the carriers, holding a base, or bringing bombers back to a corner of the map, sets the targets and what to protect.
3. **Clicks**: the plans' orders become a queue of commands (select a unit, add its formation, arm, choose a destination or a target), at most one click on a unit a frame, each checked to have counted (the selection, the path, the target) before the next.

The computer's own weaknesses, read from its code, are part of the plans: it acts for one fleet every 10 frames and gives one order a frame; it strikes only within 3550 (4350 as Japan), and intercepts only a plane found within that range; its carriers back away under 3550 of their aim; its ships take only targets that are found. A player that finds the computer first and strikes from farther than it does should win the carrier battles.

## Steps
1. **The arena and the baselines**: `OpenNspw.Arena` with the idle and rush players, and the table of every scenario for both. Then the scenarios are sorted by what they need (carriers, surface ships, transports, bases, bombers), and the strong player starts where it matters most.
2. **The player's skeleton**: perception, the click queue, `--ai` on the desktop, and the short tests.
3. **Air operations**, measured on the carrier battles (4, 5, 6, 9, 105, 106).
4. **Surface operations and logistics**, measured on the surface battles (1, 101, 102, 206, ...).
5. **Landings and the other goals**, scenario by scenario, until each is won or known to be out of reach, with why.

Each step ends with the arena's table, kept in this document.

## Results
Each cell is the outcomes of the start dates 1000000000, 1000007919 and 1000015838, in order: W won, D drawn, L lost, U unfinished, F failed.

### Baselines
| Scenario | Side | idle | rush |
| --- | --- | --- | --- |
| 1 | U | LLL | LLL |
| 2 | J | LLL | LLL |
| 3 | U | WWW | WWW |
| 4 | J | LLL | LLL |
| 5 | U | LLL | LLL |
| 6 | U | DDL | DDD |
| 7 | U | LLL | LLL |
| 8 | J | LLL | LLL |
| 9 | J | LLL | LLL |
| 10 | J | LLL | LLL |
| 100 | J | LLL | LLL |
| 101 | J | LLL | WWL |
| 102 | U | LLL | LLL |
| 103 | U | LLL | LLL |
| 104 | J | LLL | LLL |
| 105 | J | LLL | LLL |
| 106 | U | LLL | LLL |
| 107 | U | LLL | LLL |
| 108 | J | LLL | LLL |
| 109 | U | LLL | LLL |
| 200 | J | LLL | LLL |
| 201 | J | LLL | LLL |
| 202 | U | DDD | DDD |
| 203 | U | LLL | LLL |
| 204 | U | LLL | LLL |
| 205 | U | LLL | LLL |
| 206 | U | LLL | LLL |
| 207 | U | LLL | LLL |
| 208 | U | LLL | LLL |
| 209 | J | LLL | LFL |
| 300 | U | LLL | LLL |
| 301 | J | FLL | LLL |
| 302 | J | LFL | LLL |
| 303 | U | LLL | LLL |
| 304 | J | LLL | LLL |
| 305 | J | LLL | LLL |
| 306 | J | LLL | LLL |
| Won | | 3/111 | 5/111 |

The idle player wins scenario 3 by holding on, and draws 6 and 202, where nothing it does not do decides the result. The rushing player also wins 101 twice, its battle line closing on the American one.

Three games failed, in the single-player game itself, not the players: the computer's `set_cpu_route` (`0x42DA80`), planning a route by `ReplanBlockedRoute` or a move order, adds waypoints until it writes beyond the emulator's memory (209 and 302), and `UpdateBattle` indexed out of an array (301).

## Open questions
- Whether the strong player should also be offered as the computer of a network game's empty side, or only as the single-player's player.
- Whether a scenario that cannot be won at speed 1 by any play (if one turns out so) counts against the goal.
