# Making the strong player stronger
Why the strong player of [StrongAI.md](StrongAI.md) is weak, what has been tried and failed, and how to make it much stronger: the methods, the research they come from, and the order to take them in.

## Where it stands
- **All scenarios** (at `b4fca20`, three dates each): 13 games won of 111. Scenarios 1, 3, 101 and 103 are won most of the time, 2 and 202 since then; nearly every scenario with goals of its own (landings at defended places, bombers that must come back, captures) is lost.
- **The carrier battles**, the scenarios whose victory counts the enemy's carriers:

  | Scenario | Side | Won | Games |
  | --- | --- | --- | --- |
  | 4 | J | 28% (14; 28 drawn, 8 lost) | 50 |
  | 5 | U | 0% (23 drawn, 1 lost) | 24 |
  | 6 | U | about 1 in 3 | 3 |
  | 9 | J | 0% | 3 |
  | 105 | J | about 1 in 2 | 9 |
  | 106 | U | about 4 in 9 | 9 |
  | 201 | J | 0% | 3 |

  Only 4 and 5 have been played on 50 dates (`5353565`, stopped partway); the others are from 3 or 9 games, whose margin is about ±30%. On 50 dates each, capped at 60000 frames, the line formation won 72 of 300 games of 4, 5, 6, 105, 106 and 201 (24%, 20% to 29%): 4 14, 5 2, 6 21, 105 14, 106 21, 201 0.

## Why it is weak
- **It is a few hundred hand-picked numbers and rules, each checked on 3 to 9 games.** The approach radius (2000), the shadowing distance (900), the memory of a carrier lost from sight (3000 frames), the fuel margins, the size of a strike, the margin kept beyond the computer's strike range: every one was set by reasoning, then kept or dropped on a handful of games. Most are probably far from their best, and they interact.
- **A handful of games cannot tell a better player from a worse one.** A degree's difference in one strike's approach sends a game another way from there on, so the same start date is won by one build and lost by the next for no reason of their own. Nine games of a scenario leave a margin of about ±30% on its win rate: changes worth +10% or −10% looked the same, and several were kept or dropped on chance.
- **The game favors the computer**:
  - It sees every unit of the player's, while the player sees only what its units find.
  - Its carriers sail home at no more than 2 attackers or half their HP, get every lost plane back at once, and come out again (105, 106); the player's carriers never get a plane back, so a long battle is lost by attrition.
  - The American attackers reach 15400 on a full tank, the Japanese 18700, and the Japanese carriers strike from 4350, so the player's strikes in 5, 6 and 106 fly far and come home short of fuel.
- **Strikes fail on the way, not at the target.** A torpedo dropped on a carrier hits about six times in ten. But an attacker turns home at 70% of its HP, its torpedo dumped, and the escorts' guns, 600 from a cruiser and 700 from a battleship, hit every plane within 40 of a burst. In 9, ten escorts turn back three of four attackers. Strikes that arrive two or three planes at a time are turned back whole.
- **Its fleets are lines.** A formation keeps the shape its ships are in when they are grouped (`SetShipFormation`), and the player grouped them as they started, mostly in a line, so its carriers are covered from one side. A ring can now be formed first (`--formation`), but ships get stuck on each other on the way to their places: the places are given by kind and number, not to the nearest ship, so ships cross the formation and are blocked by the others, and after 900 frames the ring is grouped as it stands.
- **It makes mistakes a player watching sees at once**:
  - It keeps scouting after the enemy's carriers are found. A deck that hunts carriers keeps one or two of its attackers as scouts (two from eight), up to a quarter of its strike, and sends them out again whenever a carrier has not been seen for 300 frames. A carrier lost from sight is thought to sail on its last heading for 1500 ticks and then to stand; but the computer's carriers sail by known rules (ComputerPlayer.md): toward the player's ships to their strike range, away from them within it, and home at half their HP. A player who knows them can follow a carrier found without seeing it again.
  - Its strikes take roundabout routes, on purpose: a waypoint 2000 to one side of the way (`SideOffset`), to pass the enemy's strike; an arc round the carrier at 2000 (`ApproachRadius`), to come in from the side its escorts cover least, fanned out by 45° (`FanAngle`); and a hold 3300 to one side while enemy planes are over it (`HoldOff`). Each helped in a few games and none was measured on many; the escorts' places come from old sightings, so the weak side may be wrong, and the way round costs fuel and time.
  - It sends a scout home straight at its deck (`ReturnSelected`), over the enemy's fleet if that is the way, where its guns shoot it down; it should come home round them.
  - It lets its planes land in the game's order, not by need. A deck takes one plane at a time (`LandingLock`), and each frame the first plane in the unit list that is coming home and in its landing pattern lands (`MoveUnit`): the lowest-numbered, not the one with the least fuel or the most damage. A plane whose fuel reaches 0 slows to a stop and is lost. So, with a whole strike circling the deck, a plane with 3 fuel can wait behind one with 40 and run dry, and a damaged one can be caught by the enemy's fighters while it waits. The player turns its planes home in time (`FuelToHome` and its margins), but does nothing once they are over the deck.
  - It ignores that a carrier at 20% of its HP or less can neither launch nor take planes back (`UnitFacts.CanLaunch`; the landing check of `MoveUnit`). An enemy carrier there is out of the fight, its planes in the air lost, yet the strikes still go for the nearest carrier first; and the player's own planes, coming home to a deck at 20% or less, circle it until their fuel runs out instead of landing on another.
- **Tactics are missing.** It has no decoy wave against the escorts, as the walkthrough's players use. It makes no coordinated strikes of several decks from several sides, and no fighter cover over the strikes while it hunts carriers. Its submarines go unused, and its battleships are used only to finish cripples. 201, lost in every game, is the plainest case: Hiryu's strike lands about 20 to 25 of the American carrier's 41 HP, and the player's submarine, which starts about 3500 from it, could finish it in the time, as I-168 finished Yorktown; it was thought out of reach because the submarine was not counted.
- **It plays at speed 1, by clicks.** Every order is a series of clicks, a frame each, so it cannot steer many units at once.

## What has been tried and failed
Each was measured on 3 to 9 games of the carrier battles, unless said otherwise. Under the margin of those games, "no gain" can hide a small gain or a small loss.

| Change | Result |
| --- | --- |
| A fuel margin of +20 for the way home, instead of +10 | Nearly every carrier battle drawn: the strikes turned home before reaching the carriers |
| Strikes of several decks merged into one wave | 6 worse |
| All the warships detached after a crippled carrier | 6's fleet lost to the Japanese fleets; kept to cruisers and destroyers, only when as many as the enemy's warships and no enemy strike is seen |
| The approach arc's radius at least the distance to the target | Strikes flew huge arcs and searched again for carriers already found (the user saw it in 105); fixed at 2000 |
| A bingo-fuel rule for every plane | Attack runs cut short; planes attacking a target found are now exempt |
| A shadowing scout flying over the carrier as a strike comes in, to make it turn away (`EvadePlane`) and stop dodging torpedoes | No gain: the attackers already make it turn away from 600, and the scout adds nothing (4: 3 of 9 won against 2; 105: 4 of 9 against 5; 106 LLL against WLW) |
| Following a carrier lost on its way home (kept, `05f6176`) | 105 and 106: 9 of 18 against 8, within the noise |
| Marking a carrier found heading home beyond its strike range as going home, and closing on it with the carriers | 5 lost twice (LDL): closing on one carrier brought the player's within reach of another, not seen for a while; 105 and 106 no better |
| The same, closing only while every carrier found is going home | 105 and 106 won 8 of 18 against 9; strikes that reach it are turned back by its battleship and cruiser |
| Strikes at a carrier going home waiting for all their deck's planes | No change: the planes land and rearm at different times |
| The escorts in a ring around the carriers, or a screen across the enemy's way, before grouping (`--formation`; screened on 20 dates of 4, 5, 6, 9, 105 and 106, capped at 30000 frames) | Of 120 games: line 19 won, ring 250 28, ring 400 31, ring 600 23, screen 400 22, screen 600 19 |
| Ring 400 on 50 dates (capped at 60000 frames; 9 left out, which no formation won) | 4: 27 won against the line's 14 (p 0.03); 6: 12 against 21 (p 0.05), the escorts ordered to their places over and over while the ring formed, which kept the clicks busy, and the player from scouting and striking, for 1500 frames |
| Ring 400, each escort ordered once (`af427d4`), on the same 50 dates | 4: 26 against 14 (p 0.01); 5: 8 against 2 (p 0.06); 6: 17 against 21 (p 0.38); 105: 8 against 14 (p 0.71); 106: 19 against 21; 201: 0 and 0. In all, 78 won of 300 against 72 (p 0.53): better where the player has one carrier group (4, 5), no better where it has battleships and two carriers (6, 105, 106). The line stays the default |
| Strikes at the battleships and cruisers around a carrier first (`Targeting=escorts`), on 20 dates of 4, 5, 6, 9, 105 and 106, capped at 60000 frames | 5 won of 120 against 31 for the nearest carrier (12 games better, 33 worse, p under 0.005): 4 0 against 6, 6 0 against 9, 105 4 against 9, 106 1 against 7. The enemy's ships lose more HP (108 a game against about 92), but on ships the victory does not count, and the battle's time runs out before the carriers are struck: nearly every game is drawn. In 9 the attackers turned back fall only from 44.5 a game to 40.6. The cruisers only (`Targeting=cruisers`): 6 won of 120, the same games as the escorts in 4, 5 and 6, whose carriers have only cruisers around them |
| Strikes waiting for a share of their deck's planes (`StrikeShare`), on the same 20 dates | 0.6: 33 won against 31, most games the same (6 better, 7 worse): the share seldom holds a strike back. 1, all of them: 35 against 31 (12 better, 9 worse, p 0.66), 4 9 against 6, the attackers turned back 13.9 a game against 15.2. The best of this round, to check on fresh dates |
| Tuning the 17 numbers of `StrongParameters` (`--tune 18`, three rounds, on dates 100 and on of 4, 5, 6, 105 and 106, capped at 30000 frames) | In the last round, on 90 games each, the best two random settings and the defaults scored 0.650, 0.648 and 0.646, 20 wins each: a tie. Round 1's leader, on 10 games, fell to fifth on 30. No setting of these numbers does better than the defaults: what holds the player back is what it does, not its numbers |
| A ring only for a group with no battleship (`--formation auto:400`), checked on fresh dates 50 to 99 of 4 and 5 | 4: 18 against the line's 17 (p 0.86); 5: 4 against 6 (p 0.27). The gain of dates 0 to 49 does not come back: it was chosen for winning there, and much of the win was those dates' luck. Over the 100 dates of 4, 44 against 31. The line stays the default; a choice is kept only once it wins on dates it was not chosen on |
| The ring's places given to the warships that sail the least to them in all (`RingAssignment`, the assignment problem), ring 400 on 20 dates of 4, 5, 6, 105 and 106, capped at 30000 frames | 29 won of 100 against 29 by kind and number (21 games better, 18 worse, p 0.75); 5 played the same in every game. The ships stuck on the way to their places are not what holds the ring back |
| Ten choices screened on 20 dates of 4, 5, 6, 105, 106 and 201, capped at 30000 frames, against the defaults (19 won of 120) | `ReturnAround` 18 (1 game better, 3 worse); `UseSubmarines` 19, the same in every game but 201's, where the enemy's ships lose 9.4 HP a game against 8.4 and no carrier is sunk; `StraightWhenFar` 20 (1, 1); `SpareCrippled` 21 (3, 2); `BeamApproach` 21 (15, 12, p 0.70); `DivertFromCrippledDecks` 18 (3, 2); `ProbabilitySearch` 19 (10, 14); `FightersFirst` 18 (2, 2); `KeepScouting=false` 21 (12, 11); `CloseQuarters` 29 (15, 5, p 0.04), the only gain. On fresh dates 20 to 39, `CloseQuarters` won 24 against 21 (9, 8): over the 40 dates, 53 against 40 (24 better, 13 worse, p about 0.1), suggestive but not kept |
| A fuel margin of +2 for a plane attacking a target found (`AttackingBingoMargin=2`, not -8), on 10 dates of 5, 6 and 106 | 8 won of 30 against 7; 95 planes lost dry against 87: the planes do not run dry on the way home, but over their decks (below) |
| Launches held while a plane short of fuel comes home to its deck (`RecoverFirst`), on the same 30 games | Holding every launch: 3 won against 7, 66 lost dry against 87, the carriers losing 29.3 HP a game against 18.1, their patrols held too. Patrols and interceptions not held: 6 won, 77 dry, 24.9 HP. With `RecoverMargin=15`: 9 won (6 better, 5 worse), 74 dry, 21.1 HP |
| Carriers holding their course while their planes land (`HoldCourse`), on the same 30 games | 6 won against 7, 72 lost dry, 21.0 HP; with `RecoverFirst`, 4 won, 75 dry, 28.1 HP. Planes saved, carriers struck more: none is kept |
| Battleships and cruisers aiming at the attackers nearest the carriers (`TargetAttackers`), on the same 30 games | 8 won against 7 (2 better, 3 worse), the carriers losing 18.8 HP a game against 18.1: no effect seen on the carriers. Counted again: the enemy's attackers and bombers turned back near the player's ships, their weapon dumped, 7.57 a game against 6.93 (9% more), but its carriers lost no less (12 sunk against 8); hardly any is shot down with its weapon aboard (0.1 a game), as one hit to 70% turns home first |
| The scenarios' own missions and the defense of the places held (the goals commits, `806d667` to `326f351`), every scenario on 3 dates, capped at 100000 frames | 27 won of 111 against 28 without them: no gain; the player as it stands wins 28 of 111 |
| Strikes of more planes: `MinStrike` 6 or 9, or all the deck's (`StrikeShare=1`), on 10 dates of 4, 5, 6, 105 and 106, capped at 30000 frames | Against the defaults' 12 won of 50: 13 with 6 (4 better, 4 worse); 14 waiting for all (4, 3), 9 and all being the same rule for decks of 9 attackers at most. Fewer attackers turned back (8.5 a game against 9.2), more carriers sunk (1.16 against 1.08): the way the computer strikes. On fresh dates 20 to 39 of 4, 5, 6, 105, 106 and 201, waiting for all: 21 won of 120 against 21 (10 better, 10 worse), 6.9 attackers turned back a game against 7.6, as many carriers sunk: not kept |
| A third of each strike at the battleships and cruisers by the carrier, the rest at the carrier (`Targeting=Split`), on 20 dates of 4, 5, 6, 105 and 106, capped at 30000 frames | 13 won of 100 against 19 (12 better, 17 worse), 6 falling from 9 to 1. The enemy's ships lose more (111 HP a game against 101) but its carriers less (0.85 sunk a game against 1.05), and the strike's attackers are turned back no less (9.4 a game against 8.8): the escorts seldom sink in time to open the way. The carriers come first |

And what has worked, from LLL in every carrier battle to the results above:
- strikes at carriers only, the nearest first;
- launching when the enemy's strike will be home, to one side of its way;
- dispersing the formation once airborne, as a formation flies at 60% of its speed;
- approaching round to the side the escorts cover least, fanned out;
- bringing planes home in time;
- scout pickets that keep a carrier found in sight;
- warships sent to finish a cripple.

## A player's tips, and the rules behind them
Most of what made the player stronger, or is waiting to be measured, came from a player watching it, not from the work's own experiments. Each tip rests on a rule of the game that was in the code all along:

| Tip | The rule in the code | Where it stands |
| --- | --- | --- |
| Attackers that found the carriers should not search again; strikes should go for the nearest | `ChooseTarget`, `TryApproach` | Done (`298836a`) |
| Use the walkthrough's tactics: decoys, the Marianas waves | `EvadePlane`: a ship turns away from any attacker heading at it within 600, armed or not | The decoy scout gained nothing |
| Damaged enemy carriers go home and come back with all their planes | `cpu_strtgy`, Returning to base | Followed home (`05f6176`); catching them failed |
| Stop scouting once the enemy is found; predict where it goes | `Contacts`, `WhereNow` | `ProbabilitySearch` and `KeepScouting=false` gain nothing |
| Strikes take roundabout routes | `SideOffset`, `ApproachRadius`, `HoldOff`, `FanAngle` | Tuned: no better setting; `StraightWhenFar` gains nothing |
| A carrier at 20% of its HP or less can neither launch nor land | `UnitFacts.CanLaunch`, the landing check of `MoveUnit` | `SpareCrippled`, `DivertFromCrippledDecks`: no gain |
| Land the planes with the least fuel and the most damage first; a plane runs dry while others take off | `LandingLock`; no plane lands while a launch from its deck is under way, nor but along the deck's heading (`TryLandOnCarrier`) | 29% of the planes lost in 5, 6 and 106 run dry over their decks; `RecoverFirst` and `HoldCourse` save some, but lose games |
| Strike the escorts first, as a trade-off to try | `ShipFireAntiAircraftShellWithoutTarget` | Much worse: 5 won of 120 against 31 |
| Form a ring, not a line; ships get stuck forming it | `SetShipFormation` keeps the shape at grouping | Ring not confirmed on fresh dates; places by the assignment problem (`RingAssignment`) gain nothing |
| Speed the player up with the game's speed | A frame runs speed + 1 ticks (`TryStartSingleplayerFrame`) | Done (`3ed23f5`) |
| Launch all the attackers at once, to spread the escorts' fire | `MinStrike` | `StrikeShare=1`: 35 against 31 of 120, to confirm |
| Hiryu and the submarine can win 201 | A submarine dives and fires by itself (`ShipFireTorpedoWithoutTarget`) | `UseSubmarines`: the submarine reaches the carrier's way too late in 201's 4500 frames |
| Scouts should come home round the enemy, not over it | `ReturnSelected` flies straight | `ReturnAround`: planes seldom turn home across warships found; no gain |
| A Japanese destroyer beats a US cruiser by torpedoing, then sticking to it | Guns fire at nothing nearer than 30% of their range, or 150 at a target | `CloseQuarters`: 29 won of 120 against 19 (p 0.04), but 24 against 21 on fresh dates: not kept yet |
| The torpedoes hit too seldom; they hit more across a ship than along it | An aerial torpedo runs straight on the plane's heading; a ship is a strip of boxes along its heading (`CheckHit`) and turns away from attackers within 600 | Measured (below): 70% across, 40% head-on; `BeamApproach` does not move the drops, as the ship turns first |
| Fighters should go for fighters, mostly | `ChooseTargetInFlight` | `FightersFirst` gains nothing |
| Attacker targeting is dumb | Nearest carrier; any ship once low on fuel; no account of damage, turning, or other strikes on it | Written down |
| Ships attacked by planes should aim at the attackers | A battleship with a plane as its target fires half as many shells again; a cruiser with a ship as its target fires none (`ControlBattleshipFiring`, `ControlCruiserFiring`) | `TargetAttackers`: 9% more attackers turned back, the carriers no safer |
| Torpedoes need the sea: attackers should come in from it | A torpedo is dropped only over sea, with sea all round and all the way (`PlaneDropTorpedo`) | 33 attack runs stuck in one game of each scenario, 29 over land, 26 in 2; `SeaApproach`, not measured |
| Attackers too close to their ship should take another | The aim must be 100 to 220 ahead, within 45° of the heading | `RetargetWhenClose`, not measured |
| Attack the escorts as well as the carriers: their guns turn the strikes back | `ShipFireAntiAircraftShellWithoutTarget` hits every plane near a burst | All escorts first, or a third of each strike (`Targeting=Split`): both lose |
| Strike with many planes together, not a few | Each burst and fighter takes few planes; a small strike meets the whole defense | Waiting for all a deck's planes: 14 won of 50 against 12, then 21 against 21 on fresh dates: fewer turned back, no more won |

## Where the games go
Of the defaults' 100 games on 20 dates of 4, 5, 6, 105 and 106, capped at 30000 frames: 19 won, 40 drawn, 6 lost, 35 unfinished. In 4, 5 and 6 the player seldom loses (5 of 60): 40 games are drawn, the battle's time running out with enemy carriers afloat. 105 and 106 have far more time than the cap (840000 ticks left at 30000 frames), so the benchmark counts them won only when every carrier is sunk early: 35 of 40 are cut off unfinished. None of the techniques measured changes this, as none makes the carriers sink sooner: what the player lacks is tempo, finding the carriers and striking again and again until each is sunk within the time, and the benchmark should play 105 and 106 to their end, or count the carriers sunk.

## Torpedoes, by the angle they are dropped at
Every aerial torpedo of the 120 games of the screen's defaults (`--torpedoes`): 1318 of the player's, of which 773 hit the ship they were aimed at (59%). By the angle between the torpedo's way and the target's heading:

| Angle | 0–15° (from astern) | 15–45° | 45–90° | 90–135° | 135–150° | 150–180° (head-on) |
| --- | --- | --- | --- | --- | --- | --- |
| Hit | 61% | 43–53% | 65–73% | 61–77% | 60% | 36–42% |

The aim is close: two drops in three would pass within 5 of the ship if it held on. The misses come from its turn after the drop (64% hit when it does not turn, 53% when it does). Drops from 180 to 240 hit most, about 70%. The computer's torpedoes show the same, flatter. `BeamApproach` comes in on the beam, but the drops fall at the same angles (39% within 60–135°, against 40%), and hit a little more often (61.4% against 58.6%): the ship has turned away before the drop.

## What the work needs
Why the player did not find these by itself, and what would let it:

- **To see what a player sees, as numbers.** A mistake on the screen is a condition the arena can test every frame and count, but only once someone writes the test: the counts so far (weapons dropped, turned back, planes lost, HP lost, hits on carriers) were chosen for strikes, and are blind to the rest. Detectors to add, each counted per game:
  - escorts with a ship as their gun target while enemy attackers or bombers are within their guns' reach;
  - planes circling a deck that cannot take them, and planes lost with no fuel;
  - scouts and strikes within the enemy's guns' reach on their way home;
  - attackers that drop on ships the victory does not count, and torpedoes dropped from ahead or astern, not across;
  - a carrier found and lost again, and how long each stays lost;
  - ships that stand still, or are stuck on one another, in a formation forming;
  - strikes that arrive two or three planes at a time;
  - our ships' guns idle while an enemy is within reach.
- **Pictures of the moments that matter.** The arena plays without a screen; a picture of each strike's run, each carrier hit and each plane lost, saved to a file, would let the work look at the game as a player does, not only count it.
- **Design before experiments.** Each experiment costs hours of games; testing ideas one by one, as they come, is the slowest way. First a pass over the rules (every condition that stops a unit acting, and what it means for both sides: the start of the table above) and over the detectors; then a list of choices, each with the rule behind it; then the experiments, several at a time.
- **Examples of good play.** Win rates say how often the player wins, not what a good player would have done instead. Recordings of strong human games of the carrier battles would show it, and could later be learned from (step 6 below). The single-player game cannot record a replay yet; when it can, a few dozen games would do.
- **Faster games.** 1.22's computer now runs as C#, not emulated, which halved the time of a game: 30000 frames take about 3 minutes on a busy core, not 4 to 5, and the games are the same, row for row. A benchmark of 50 dates still takes hours; profiling the game itself would make every step faster.
- **Shorter answers.** A sequential test (Wald) stops a comparison once it is clear; screening on the detectors, which move in a few games, before the win rate, which needs a hundred.
- **A memory of what was learnt.** This document: every tip, every rule behind one, and every result, kept or not, so that the next round starts from them.

## How to make it much stronger
In order: each step needs the ones before it.

1. **A benchmark that can tell** (done, `5353565`). The carrier battles on 50 start dates (`--scenarios carriers --dates 50`), each scenario's win rate with its 95% Wilson interval, and, per game, what became of the strikes: weapons dropped, planes turned back, out of fuel and lost, the HP the enemy's ships lost, the carriers sunk and lost. `--compare` pairs two runs game by game, on the same dates, and gives the exact sign test's p of the games done better and worse. Playing both builds on the same dates (common random numbers) cancels much of the noise of the setups, so fewer games tell. 50 dates of all seven battles take about two hours on 8 cores.
2. **Experiments, not guesses** (doing). One change at a time: first screened on a measure that moves faster than the win rate, such as the HP the player's carriers lose, the planes lost, or the torpedoes dropped, on 20 dates with the frames capped. Then confirmed on 50 dates with `--compare`, and recorded here whether it helps or not. The formations come first: line, rings and screens of several radii, as an experiment's choice (`--formation`).
3. **Tuning the numbers automatically** (the record `StrongParameters`, `--params`, and the arena's `--tune`, `0ce0d46`). The constants go into one record of parameters that the arena can set. A black-box optimiser searches them against the benchmark: random search with successive halving (Hyperband), which drops poor settings after a few games and plays more of the good ones; or CMA-ES, for a few dozen continuous parameters. A few hundred to a few thousand games, hours to days on one machine. It stays within the rules: the player still knows only what a player sees while it plays; only its numbers were chosen from the games before.
4. **Fixing the mistakes a player sees**, each an experiment of step 2:
   - fewer scouts once the carriers are found, and their way predicted from the computer's known rules (a Kalman or particle filter with its motion model, below), the attackers freed to strike;
   - strikes that leave a carrier that can no longer launch for one that can, and finish it after; planes that come home to another deck when theirs is at 20% or less;
   - scouts, and strikes, coming home round the enemy's ships found, their guns' reach (600, 700) and a margin, by the shortest way round the circles (below);
   - landings by need: the planes with fuel to spare held off the deck, a little way out (a destination, not Return), so that only the ones with the least fuel and the most damage wait in the pattern, and called in as the deck frees; and every plane turned home with the fuel for the way and for its wait to land, which grows with the planes ahead of it;
   - the ring's places given to the ships by the assignment problem (the least sailing, so that none crosses the formation), and the ships sent round the carrier, not through it;
   - straight strikes, if the tuning finds the ways round cost more than they save;
   - the submarines sent at the carriers the scouts find, submerged, along the way the carrier is predicted to sail, to fire their torpedoes at it, out of the reach of the destroyers that hunt them; first in 201, whose games are short (4500 frames) and cheap to play many of.
5. **A learned policy over the existing tactics**, if tuning levels off. The code keeps flying, scouting and clicking; a small policy chooses at a few decision points, from what the player sees:
   - which carrier to strike;
   - whether to launch now or wait;
   - whether to close in or keep away;
   - whether to send the warships.

   Such macro-actions are how OpenAI Five and AlphaStar kept their problems learnable. In microRTS research, at budgets like this one, scripted policies with search or tuning over them beat learning from raw actions. With a few decisions per game, a policy can be trained from thousands of games: evolution strategies, or policy gradients (REINFORCE) on the outcome.
6. **Learning from human games** (imitation learning). A model learns, from recordings of good players, what they decide in each situation, from what they see. AlphaStar began so, from human replays, and then improved by playing itself. Here the decisions of step 5 (which carrier, when to launch, how to form, when to close in) could be learned from 50 to 200 recorded games of the carrier battles, the existing code still flying and clicking; then improved by the arena's games, as step 5 does. It needs the recordings: the game records traces (Trace122, docs/Verification.md), but the few in this repository were made to check the port, not to show good play, and the walkthrough gives tactics in words, not data.
7. **Deep reinforcement learning from raw actions: not here.** AlphaStar and OpenAI Five learned from millions to billions of games. A frame costs 1.2 ms on an idle machine, about 6 ms on each core with all of them busy: the arena plays about 160 games of 30000 frames an hour on 8 cores. A million games would take years of this machine. It would need the game made many times faster, or a cluster, and it would be a project of its own.
8. **An upper bound for the scouting**: one benchmark with `--sees-all`, which breaks the rules, only to measure. If seeing everything wins much more, finding the enemy is what is missing; if not, the tactics are.

### Finding tactics without being told
Most of the tips players give, the strong player could have found by itself: they come from the game's rules, which its code has, and from what goes wrong in its games, which the arena records. Neither needs a player to point it out, only a loop that looks for it, and choices wide enough to hold the answer:

1. **Read the rules for what the computer cannot do.** Every rule of the game and of the computer is in the code (`cpu_strtgy`, ComputerPlayer.md): a carrier at 20% or less can neither launch nor land; a deck lands one plane at a time, the lowest-numbered first; a fleet sailing home launches no strike; the computer launches only within 3550 or 4350, and never intercepts. Each is a weakness to use or a trap to avoid. The search is mechanical: list every condition under which a unit does or does not act, and ask of each whether the player can make it hold for the enemy, or keep it from holding for itself.
2. **Mine the games for where they are lost.** The arena already counts what becomes of every strike (dropped, turned back, out of fuel, lost) and every HP lost. Grouped by cause, across hundreds of games, they say what to fix next: if most attackers are turned back by escorts' guns, the strikes' approach or size; if planes are lost waiting to land, the landing order; if carriers are lost to strikes not seen coming, the scouting. A finer log (where each plane was lost, to what, with what fuel; when each enemy was found and lost) makes the causes sharper.
3. **Turn each finding into a choice, not a fix.** Each idea becomes a parameter or an option with the old behaviour as one of its values, as the formation (`--formation`), the targeting (`Targeting`) and the strike's size (`StrikeShare`) are now: the benchmark then tells whether it helps, and the tuner can combine them.
4. **Let search pick among the choices.** With the choices in `StrongParameters`, the tuner tries combinations no one suggested, and keeps what wins more of the same games: a tip is then found when some value of some choice wins, whoever thought of the choice.
5. **Widen the choices into decisions learned from the games** (step 5 of the plan): when the right choice depends on the situation (escorts first while there are many, the carrier when few), a small policy learns it from the outcomes.
6. **Record every result** here, kept or not, so that the next idea starts from what is known.

What this cannot find by itself is a tactic outside every choice it has: no search over targeting finds a decoy wave, if nothing in the player can send one. Widening the choices (one more kind of order, one more kind of mission) is where a player's experience, or reading the walkthroughs, still helps most; past that, only learning from raw actions (step 7) discovers tactics with no vocabulary given, and it is far beyond this machine. The loop of steps 1 to 6 can be run by an assistant with the code and the arena, given the hours of games each step takes: each round is a reading of the rules or the logs, a choice added, a benchmark, and a line in the table above.

### Mathematics that fits
Each of these answers a failure seen in the games, with a known algorithm, and can be tried as one experiment of step 2:

| Concept | Where it fails now | What it gives |
| --- | --- | --- |
| **Interception geometry**: the meeting point of a plane and a ship sailing away, a quadratic in the time | The strike sent at the crippled carrier in 105 chased it from behind and turned home for fuel 3000 short (StrongAI.md) | Whether a strike can reach a moving carrier and come home, before it is launched, and where to aim: from the deck's position, the target's speed and heading, and the fuel |
| **Rendezvous**: where a deck sailing on meets its planes flying home | Planes are brought home with a margin of fuel, the deck sailing away from them (`FuelToHome`) | A deck that sails toward its returning strike lets it fly farther: the strike's reach grows by the deck's way |
| **Bayesian search theory** (Koopman; the searches for the USS Scorpion and Air France 447): a map of where the enemy probably is, moved by what is known of how it sails, and lowered where a scout looked and found nothing | Scouts fly fixed legs: where it was last seen, a picket, the expected areas in turn; a carrier going home was searched for where it was | Each scout flies where finding is likeliest. A particle filter carries the map: the computer's carriers come at the player's to their strike range and sail home at half their HP, which is a motion model |
| **A Kalman filter**: a contact's position and velocity, with their uncertainty, from its sightings | A carrier lost from sight is thought to sail on its last heading for 1500 ticks (`WhereNow`), then to stand | Where to aim and how wide to search, from how long it has been lost |
| **Lanchester's square law**: a force's strength is the square of its numbers when all fire at once | Strikes of two or three planes are turned back whole by the escorts, whose fire falls on the first attackers in range | Why strikes must arrive together, and how many: massing them, not merely launching more often |
| **Shortest paths around circles** (tangent lines, a visibility graph over the escorts' gun ranges) | The approach comes round at 2000 to the side the escorts cover least, by a rule of thumb | The way in and out that spends the least time within the escorts' 600 and 700 |
| **The assignment problem** (the Hungarian algorithm) | The escorts are given their places in a ring by kind and number, and one may be on the far side of its place, crossing the carrier's way | Which escort takes which place, for the least sailing; which strike or warship takes which target |
| **A known opponent, a partly observed MDP**: the computer's rules are known (ComputerPlayer.md) and do not adapt, so the game is a planning problem under fog, not a game against a learner | The player keeps away by rules of thumb | Planning against its model: it launches only within 3550 or 4350, intercepts no strike, strikes no more on its way home. Rollouts of a fast model of the battle (determinized, as Information Set Monte Carlo tree search does) can compare plans; the game's own engine, at 1.2 ms a frame, is too slow for that |
| **Sequential testing** (Wald's SPRT) | The benchmark plays all its games | Stops a comparison as soon as it is clearly better or worse, which saves most of the games of a bad change |

### Research
- Common random numbers, and paired tests on them: the standard way to compare two simulated policies with fewer runs.
- Successive halving and Hyperband (Jamieson and Talwalkar, 2016; Li et al., 2017); CMA-ES (Hansen, 2001): black-box tuning of noisy evaluations.
- Portfolio greedy search, and scripted policies with search over them, in microRTS and StarCraft combat research: how small budgets are spent.
- OpenAI Five (2019) and AlphaStar (Vinyals et al., 2019): macro-actions, self-play, and the compute that learning from raw actions takes.
- Koopman's search theory (1946 to 1957) and its Bayesian use at sea (Stone, 1975); particle filters (Gordon et al., 1993); the Kalman filter (1960).
- Lanchester (1916); Information Set Monte Carlo tree search (Cowling et al., 2012); Wald's sequential probability ratio test (1945).
