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

## Carrier battles
What decides a carrier battle, read from the game's code and measured with the arena's `--strikes`:

- **A torpedo dropped on a carrier hits about six times in ten**, for 5 to 9 of its 36 or 41 HP. What fails is getting there: an attacker turns home at 70% of its HP, its torpedo dropped into the sea (`SetAttackerEmergencyDestination`), and is often shot down on the way.
- **The escorts' anti-aircraft fire** (`ShipFireAntiAircraftShellWithoutTarget`) reaches 600 from a cruiser and 700 from a battleship, about a volley every 60 frames, at the lowest-numbered armed attacker in range; each burst hits every plane within 40 of it. A destroyer has only machine guns (300). A ship with a gun target fires no anti-aircraft shells, and a supplying one fires nothing.
- **The computer's fighters fly only with its strikes**: it never sends them up over its carriers, as its interceptions need a plane for the fleet's target ([ComputerPlayer.md](ComputerPlayer.md#cpu_strtgy), Air strikes). What breaks a strike is meeting the computer's own: its attackers' rear guns and its escorting fighters, on the way or over its carriers as they launch or land.
- **A formation flies at about 60% of its planes' speed**: the leader slows down whenever a plane is out of its place (`SteerUnit`), while the fuel burns by the tick. A plane given a destination of its own leaves its formation (`ApplyOrders`).
- **Return turns a plane home only at the end of its path**, and the game turns a plane home only at 20 fuel, too late when it is far out.
- **Each bomb or shell that hits a carrier destroys each plane parked on it one time in 10** (`UpdateBomb`, `UpdateGunShell`): a carrier bombed unawares loses its strike on its deck.
- **An attacker sees 1000**, beyond the escorts' guns: a scout keeps a carrier in sight from 900.
- The computer's carriers come at the player's to within its strike range (3550, or 4350 as Japan), and launch as soon as one is within it: a carrier kept out of it is never struck.

So, while the victory conditions count the enemy's carriers (`HuntsCarriers`), the strong player:

- **finds them first**: its scouts sweep the areas where the briefing expects them early, then fly a picket across the way to them, 2500 beyond the computer's strike range, one going out every 900 frames so that one is always out; a scout that finds a carrier keeps it in sight, and another goes out to relieve it when its fuel runs low; a scout lost is replaced. Scenario 4's American carrier starts in one of three places, at random, which its knowledge now names.
- **keeps its carriers out of the computer's strike range**, as before, and keeps its fighters home to meet the computer's strikes;
- **strikes early** at an area where the briefing expects the carriers, if it is the only one, or so near that they would strike first (scenarios 4 and 201);
- **strikes only carriers**, the one most torpedoes were dropped on first, where one was last seen within 3000 frames, within 85% of its planes' reach; a strike that finds only escorts approaches the middle of their formation, where the carrier is;
- **launches when the enemy's strike will be home**, 2000 to one side of its way, and **holds off** 3300 to one side of the target while enemy planes are over it;
- **disperses** its strike once it is in the air, each plane flying on alone at full speed, and **approaches together**, around the target at 2000, to the side its escorts cover least, fanned out across it, as far as the fuel lets;
- **brings its planes home in time**, pointed at their deck, with the fuel for the way home while the deck sails away from them;
- **sends its warships after a crippled carrier**: once two torpedoes or more were dropped on one, the cruisers and destroyers screening its carriers leave them and go at it, if they are as many as the enemy's warships found around it, and no enemy strike has been seen for 3000 frames; the battleships stay with the carriers. The strikes often leave a carrier at a few HP, burning slowly, and the warships' guns and torpedoes finish it.

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

### The strong player
At commit `b4fca20` (air operations, positioning out of the computer's strike range, scenario knowledge, surface groups, landings and sea routes), capped at 100000 frames:

| Scenario | Side | strong |
| --- | --- | --- |
| 1 | U | WWL |
| 2 | J | LLL |
| 3 | U | WWW |
| 4 | J | DDL |
| 5 | U | LLL |
| 6 | U | LLL |
| 7 | U | LLL |
| 8 | J | LLL |
| 9 | J | LLL |
| 10 | J | LLL |
| 100 | J | LLL |
| 101 | J | WWW |
| 102 | U | WLL |
| 103 | U | WWW |
| 104 | J | LLL |
| 105 | J | LLL |
| 106 | U | LLL |
| 107 | U | LLL |
| 108 | J | LLL |
| 109 | U | LLL |
| 200 | J | LLL |
| 201 | J | LLL |
| 202 | U | DDD |
| 203 | U | LLL |
| 204 | U | LLL |
| 205 | U | LLL |
| 206 | U | WLL |
| 207 | U | LLL |
| 208 | U | LLL |
| 209 | J | LLL |
| 300 | U | LLL |
| 301 | J | LLL |
| 302 | J | LLL |
| 303 | U | LLL |
| 304 | J | LLL |
| 305 | J | LLL |
| 306 | J | LLL |
| Won | | 13/111 |

It wins 13 games of 111, where the rushing player wins 5: scenarios 1, 101 and 103 most of the time, 102 and 206 once. It still loses the carrier battles but 4, where it draws: its torpedo strikes reach the enemy carriers through their escorts' guns and fighters too seldom to sink them. It does nothing yet for most of the scenarios with goals of their own (landings at defended places, bombers that must come back, captures).

Since then, as measured scenario by scenario: 2 is won in all three games (every deck strikes Pearl Harbor at once, each at a different ship), 202 is won (the strategic bombers reach Tokyo, 62% of their range away), 4 is drawn in all three, and 6 drawn twice.

### The carrier battles
At commit `fa6b64e` ([Carrier battles](#carrier-battles)), with no cap on the frames:

| Scenario | Side | strong |
| --- | --- | --- |
| 4 | J | WWW |
| 5 | U | DDD |
| 6 | U | WLD |
| 9 | J | LLL |
| 105 | J | WWW |
| 106 | U | LLW |
| 201 | J | LLL |

Before, the strong player drew 4 in all three games, drew 5 and 6 at times, and lost 9, 105, 106 and 201. It now wins 4 and 105 in every game, and 6 and 106 one time in three: its torpedoes reach the carriers, and more than half of them hit.

- **4**: the strikes leave the American carrier at a few HP, and the cruisers and destroyers sent after it finish it; before they did, two games in three ended in a draw with the carrier at 2 to 6 HP. The carrier starts at one of three places, whichever the early strike and the scouts' sweeps find.
- **5**, **6** and **106**, where the player is American: the American attackers reach 15400 on a full tank, where the Japanese reach 18700, and the Japanese carriers strike from 4350, so the American strikes fly far and come home short of fuel; in 22500 frames (27500 in 6) the strikes sink one or two of the three Japanese carriers (two in 106), often leaving the last at a few HP. In 6 and 106 the Japanese strikes sink the player's carriers one or two times in three; in 106, whose battle lasts 450000 frames, the Japanese carriers also repair and get their planes back at Truk, which the player's do not. Sending all the warships after the Japanese carriers lost them to the Japanese fleets in 6; sending the strikes of several decks around as one approach made 6 worse too.
- **9**: the American fleet screens its three carriers with two battleships, six cruisers and two destroyers, whose fire turns home three of four attackers before they drop (47 in one game), and the player has no fighters; its strikes damage the carriers, but sink none in time.
- **201**: out of reach as it stands. Three of the four Japanese carriers are burning at 7 of 36 HP, which is not above the fifth a carrier needs to launch, and the ships have 3 to 7 fuel; the last carrier's five or six attackers reach the American carrier, 8000 away, at the edge of their range, and sinking its 41 HP needs about six hits in 4500 frames, one strike.

The other scenarios are as before: 1 WWW, 2 WWW, 101 WWW, 202 WWD, and 103 LLL, which the code before these changes (`10be27d`) also loses in every game now, as it does 202's third game: 103 was lost before them, after `b4fca20`.

## Open questions
- Whether the strong player should also be offered as the computer of a network game's empty side, or only as the single-player's player.
- Whether a scenario that cannot be won at speed 1 by any play (if one turns out so) counts against the goal.
