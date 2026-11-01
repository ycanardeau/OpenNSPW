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

The arena runs in Release, one game per core, each on its own thread with a platform of its own (`ArenaPlatform`), whose clock runs as fast as the game can. The single-player computer is 1.22's rewritten as C# ([Singleplayer.md](Singleplayer.md)), not emulated: the same games, row for row, in about half the time (scenarios 4, 5 and 105, 10000 frames each, one at a time: 37 seconds against 68). A frame costs about 1.2 ms when the game runs alone and about 6 ms with every core busy, so a scenario of 60000 ticks, 30000 frames, takes about 40 seconds alone and about 3 minutes in a full arena. The tests' lockstep runs, in Debug, cost about 17 ms a frame.

```bash
dotnet run -c Release --project OpenNSPW/OpenNspw.Arena -- --scenarios 4,5,6 --dates 3 --players idle,rush
```

A game swings on small things: a degree of one strike's approach sends it another way from there on, so the same date can be won by one build and lost by the next for no reason of their own, and three or nine games say little of a change. The benchmark is the carrier battles (`--scenarios carriers`) on 50 start dates; the arena's summary gives each scenario's win rate with its 95% interval, and `--compare` tells, game by game, whether a change does better than chance would (the sign test's p under 0.05).

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

## Missions and holding on
Some scenarios' goals are not reached by the general tasks, and get their own plan (`StrongPlayer.Missions.cs`), played before the general tasks, which it may hold back: the general strikes, the scouts and the carriers' moves.

| Scenario | Mission |
| --- | --- |
| 300 | The carrier sails toward Tokyo until the bombers' fuel carries them over Tokyo and on to the corner of the map, or its own fuel, 9 at the start, is spent; the bombers strike the cities only; once two cities are destroyed, every bomber flies to the corner. |
| 205 | Nouméa's bomber flies to Bougainville's airfield by a point clear of the enemy's fighters, with Nouméa's fighters, all given Bougainville as their base; it comes down there within 1000; once serviced, it flies back the same way. Guadalcanal's fighters are based at Bougainville from the start, to defend it. |
| 109, 207 | Once the place is destroyed, every ship sails back to the waters of the start. |

### Holding on
Where the player wins by holding on (`ScenarioKnowledge.HoldsOn`), it defends (`StrongPlayer.Defense.cs`): the places it holds are where its bases, cities and anchored ships stand, as it sees them. Its fighters patrol over them, and every ready fighter meets a raid near them or near a deck; its strikes hit only enemies that come within 3500 of a deck or a place held, and it neither scouts nor sweeps; its warships fight only near the places held, keep station at the nearest one otherwise, and leave for repairs only below a quarter of their HP. The ready fighters and attackers of its other air bases fly to the air base nearest where the briefing expects the enemy, as many as it has room for, and are based there in the air: in scenario 7, every plane starts at Nouméa, beyond a fighter's reach of Guadalcanal's empty airfield, which the scenario is lost with.

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
- **A computer's carrier with no more than 2 attackers, or at half its HP or less, sails home** to its naval base ([ComputerPlayer.md](ComputerPlayer.md#cpu_strtgy), Returning to base). There it gets every plane it lost back at once, is repaired by 1 HP every 200 ticks, and comes out again; the player's carriers get no planes back, and the computer gets no other reinforcements in these battles. Only 105 and 106 have naval bases, each within the area where the briefing expects the computer's fleet. Their long games are lost this way: a carrier left at a few HP slips home, while the player's planes run out.

So, while the victory conditions count the enemy's carriers (`HuntsCarriers`), the strong player:

- **finds them first**: its scouts sweep the areas where the briefing expects them early, then fly a picket across the way to them, 2500 beyond the computer's strike range, one going out every 900 frames so that one is always out; a scout that finds a carrier keeps it in sight, and another goes out to relieve it when its fuel runs low; a scout lost is replaced. Scenario 4's American carrier starts in one of three places, at random, which its knowledge now names.
- **keeps its carriers out of the computer's strike range**, as before, and keeps its fighters home to meet the computer's strikes;
- **strikes early** at an area where the briefing expects the carriers, if it is the only one, or so near that they would strike first (scenarios 4 and 201);
- **strikes only carriers**, the nearest first, where one was last seen within 3000 frames, within 85% of its planes' reach; a strike that does not find its carrier where it would be by now looks where it was last seen, and one that finds only escorts approaches the middle of their formation, where the carrier is;
- **launches when the enemy's strike will be home**, 2000 to one side of its way, and **holds off** 3300 to one side of the target while enemy planes are over it;
- **disperses** its strike once it is in the air, each plane flying on alone at full speed, and **approaches together**, around the target at 2000, 90° round at most, to the side its escorts cover least, fanned out across it, as far as the fuel lets;
- **brings its planes home in time**, pointed at their deck, with the fuel for the way home while the deck sails away from them;
- **follows a carrier home**: a carrier lost for 3000 frames that was last seen heading within 45° of the area where the briefing expects its fleet is thought to be on its way there, at 0.55 a tick at least, and remembered until it would have been there for 4000 frames. The scouts look for it on its way, 1500 each side, the strikes go at it there, the carriers follow it to keep it within their planes' reach, and the warships sent after it follow it home. Sooner than 3000 frames, a carrier sailing away is as often keeping out of the player's strike range.
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

At commit `f329921` (strikes that reach as far as the planes' fuel, every deck at once against known places, fanned attacks, carriers that close in only on what is known), capped at 100000 frames:

| Scenario | Side | strong |
| --- | --- | --- |
| 1 | U | WWW |
| 2 | J | WWW |
| 3 | U | WLW |
| 4 | J | DDD |
| 5 | U | DDL |
| 6 | U | DDD |
| 7 | U | LLL |
| 8 | J | LLL |
| 9 | J | LLL |
| 10 | J | LLL |
| 100 | J | LLL |
| 101 | J | WWW |
| 102 | U | WWL |
| 103 | U | LLL |
| 104 | J | LLL |
| 105 | J | LLL |
| 106 | U | LLL |
| 107 | U | LLL |
| 108 | J | LLL |
| 109 | U | LLL |
| 200 | J | FLF |
| 201 | J | LLL |
| 202 | U | WWD |
| 203 | U | LLL |
| 204 | U | LLL |
| 205 | U | LLL |
| 206 | U | WWW |
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
| Won | | 18/111 |

It wins 18 games of 111. Scenario 103 is lost where it was won: the fighters swept to the enemy's carriers, which the victory conditions count, and left Pearl Harbor to the raids; 3 is lost once, the air units spent on strikes. Both are mended since (see [Holding on](#holding-on)).

### The carrier battles
With carriers followed home, and with no cap on the frames; 105 and 106 also from the six start dates after these (1000023757 to 1000063352), as three games of each swing on small changes. Before, with strikes that go for the nearest carrier (`c3e4e43`), on the same dates:

| Scenario | Side | strong | six more | before | six more |
| --- | --- | --- | --- | --- | --- |
| 4 | J | DWD | | DWD | WDDLDD |
| 5 | U | DDD | | DDD | |
| 6 | U | WDD | | WDD | |
| 9 | J | LLL | | LLL | |
| 105 | J | LWW | WLWWLL | LLW | LLWWWW |
| 106 | U | WWW | LWLLLL | WLW | LLLLWL |
| 201 | J | LLL | | LLL | |

Following carriers home wins 9 of the 18 games of 105 and 106, where it won 8 before: as much as the games swing, no gain to speak of. A change of a degree in one strike's approach sends a game another way from there on, so the same date can be won by one build and lost by the next for no reason of their own. In the games it loses, a carrier left at a few HP still gets home: in 105's 1000055433, the American carrier sat at 9 of 41 HP for 8000 frames on its way home, but the one strike sent at it found nothing where it would be, and came home with half its fuel.

That strike was not lost: it chased the carrier from behind, from 7900 away, where the player's carrier kept out of the computer's strike range, and turned home for fuel 3000 short of it. A fleet whose flagship sails home launches no strike ([ComputerPlayer.md](ComputerPlayer.md#cpu_strtgy), Air strikes), so three ways to catch it were tried, none better than this:

- marking a carrier found heading home beyond its strike range of the player's ships as going home, predicting it straight home, and letting the player's carriers close to 2500 from it: the strikes reach it, but its battleship and cruiser turn back ten of twelve attackers before they drop; and in 5, closing on one Japanese carrier going home brought the player's carrier within reach of another, not seen for a while, which sank it (5 LDL);
- the same, closing only while every carrier found is going home: 5 draws again, but 105 and 106 win 8 of 18;
- and, on top of it, strikes at a carrier going home waiting for all of their deck's planes: no change, as the planes come home and rearm at different times.

Before, the strong player drew 4 in all three games, drew 5 and 6 at times, and lost 9, 105, 106 and 201. It now wins 105 and 106 about one time in two, and 4 and 6 at times: its torpedoes reach the carriers, and more than half of them hit. At `fa6b64e`, whose strikes went for the carriers by their worth, 4 was WWW and 105 WWW on the three dates, but 4 WLDLDD and 105 WLLWLW on the six more: about as often.

- **4**: the strikes leave the American carrier at a few HP, and the cruisers and destroyers sent after it often finish it, when they find it; most games still end in a draw with the carrier at 2 to 10 HP. The carrier starts at one of three places, whichever the early strike and the scouts' sweeps find.
- **5**, **6** and **106**, where the player is American: the American attackers reach 15400 on a full tank, where the Japanese reach 18700, and the Japanese carriers strike from 4350, so the American strikes fly far and come home short of fuel; in 22500 frames (27500 in 6) the strikes sink one or two of the three Japanese carriers (two in 106), often leaving the last at a few HP. The Japanese strikes sometimes sink the player's carriers (6 and 106 lost a game each in some runs); in 106, whose battle lasts 450000 frames, the Japanese carriers also repair and get their planes back at Truk, which the player's do not. Sending all the warships after the Japanese carriers lost them to the Japanese fleets in 6; sending the strikes of several decks around as one approach made 6 worse too.
- **9**: the American fleet screens its three carriers with two battleships, six cruisers and two destroyers, whose fire turns home three of four attackers before they drop (47 in one game), and the player has no fighters; its strikes damage the carriers, but sink none in time.
- **201**: lost, as the player uses only half of what it has. Three of the four Japanese carriers are burning at 7 of 36 HP, which is not above the fifth a carrier needs to launch, and the ships have 3 to 7 fuel; Hiryu's six attackers reach the American carrier, 8000 away, at the edge of their range, and one strike lands about 20 to 25 of its 41 HP. But the player also has a submarine, at (-2000, 3500), about 3500 from the American carrier, within reach in the battle's 9000 ticks, whose torpedoes could finish it, as the submarine I-168 finished Yorktown; the strong player never uses its submarines.

The other scenarios are as before: 1 WWW, 2 WWW, 101 WWW, 202 WWD, and 103 LLL, which the code before these changes (`10be27d`) also loses in every game now, as it does 202's third game: 103 was lost before them, after `b4fca20`.

## Making it stronger
Why the strong player is still weak, what has been tried and failed, and the methods to make it much stronger: [StrongAIResearch.md](StrongAIResearch.md).

## Open questions
- Whether the strong player should also be offered as the computer of a network game's empty side, or only as the single-player's player.
- Whether a scenario that cannot be won at speed 1 by any play (if one turns out so) counts against the goal.
