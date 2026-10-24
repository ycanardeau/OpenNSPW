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

  Only 4 and 5 have been played on 50 dates (`5353565`, stopped partway); the others are from 3 or 9 games, whose margin is about ±30%.

## Why it is weak
- **It is a few hundred hand-picked numbers and rules, each checked on 3 to 9 games.** The approach radius (2000), the shadowing distance (900), the memory of a carrier lost from sight (3000 frames), the fuel margins, the size of a strike, the margin kept beyond the computer's strike range: every one was set by reasoning, then kept or dropped on a handful of games. Most are probably far from their best, and they interact.
- **A handful of games cannot tell a better player from a worse one.** A degree's difference in one strike's approach sends a game another way from there on, so the same start date is won by one build and lost by the next for no reason of their own. Nine games of a scenario leave a margin of about ±30% on its win rate: changes worth +10% or −10% looked the same, and several were kept or dropped on chance.
- **The game favors the computer**:
  - It sees every unit of the player's, while the player sees only what its units find.
  - Its carriers sail home at no more than 2 attackers or half their HP, get every lost plane back at once, and come out again (105, 106); the player's carriers never get a plane back, so a long battle is lost by attrition.
  - The American attackers reach 15400 on a full tank, the Japanese 18700, and the Japanese carriers strike from 4350, so the player's strikes in 5, 6 and 106 fly far and come home short of fuel.
- **Strikes fail on the way, not at the target.** A torpedo dropped on a carrier hits about six times in ten. But an attacker turns home at 70% of its HP, its torpedo dumped, and the escorts' guns, 600 from a cruiser and 700 from a battleship, hit every plane within 40 of a burst. In 9, ten escorts turn back three of four attackers. Strikes that arrive two or three planes at a time are turned back whole.
- **Its fleets are lines.** A formation keeps the shape its ships are in when they are grouped (`SetShipFormation`), and the player grouped them as they started, mostly in a line, so its carriers are covered from one side.
- **Tactics are missing.** It has no decoy wave against the escorts, as the walkthrough's players use. It makes no coordinated strikes of several decks from several sides, and no fighter cover over the strikes while it hunts carriers. Its submarines go unused, and its battleships are used only to finish cripples.
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

And what has worked, from LLL in every carrier battle to the results above:
- strikes at carriers only, the nearest first;
- launching when the enemy's strike will be home, to one side of its way;
- dispersing the formation once airborne, as a formation flies at 60% of its speed;
- approaching round to the side the escorts cover least, fanned out;
- bringing planes home in time;
- scout pickets that keep a carrier found in sight;
- warships sent to finish a cripple.

## How to make it much stronger
In order: each step needs the ones before it.

1. **A benchmark that can tell** (done, `5353565`). The carrier battles on 50 start dates (`--scenarios carriers --dates 50`), each scenario's win rate with its 95% Wilson interval, and, per game, what became of the strikes: weapons dropped, planes turned back, out of fuel and lost, the HP the enemy's ships lost, the carriers sunk and lost. `--compare` pairs two runs game by game, on the same dates, and gives the exact sign test's p of the games done better and worse. Playing both builds on the same dates (common random numbers) cancels much of the noise of the setups, so fewer games tell. 50 dates of all seven battles take about two hours on 8 cores.
2. **Experiments, not guesses.** One change at a time: first screened on a measure that moves faster than the win rate, such as the HP the player's carriers lose, the planes lost, or the torpedoes dropped, on 20 dates with the frames capped. Then confirmed on 50 dates with `--compare`, and recorded here whether it helps or not. The formations come first: line, rings and screens of several radii, as an experiment's choice (`--formation`).
3. **Tuning the numbers automatically.** The constants go into one record of parameters that the arena can set. A black-box optimiser searches them against the benchmark: random search with successive halving (Hyperband), which drops poor settings after a few games and plays more of the good ones; or CMA-ES, for a few dozen continuous parameters. A few hundred to a few thousand games, hours to days on one machine. It stays within the rules: the player still knows only what a player sees while it plays; only its numbers were chosen from the games before.
4. **A learned policy over the existing tactics**, if tuning levels off. The code keeps flying, scouting and clicking; a small policy chooses at a few decision points, from what the player sees:
   - which carrier to strike;
   - whether to launch now or wait;
   - whether to close in or keep away;
   - whether to send the warships.

   Such macro-actions are how OpenAI Five and AlphaStar kept their problems learnable. In microRTS research, at budgets like this one, scripted policies with search or tuning over them beat learning from raw actions. With a few decisions per game, a policy can be trained from thousands of games: evolution strategies, or policy gradients (REINFORCE) on the outcome.
5. **Deep reinforcement learning from raw actions: not here.** AlphaStar and OpenAI Five learned from millions to billions of games. A game here costs 1.2 ms a frame: a scenario of 30000 frames takes 36 seconds, and 8 cores play about 800 an hour. A million games would take more than a year of this machine. It would need the game made many times faster, or a cluster, and it would be a project of its own.
6. **An upper bound for the scouting**: one benchmark with `--sees-all`, which breaks the rules, only to measure. If seeing everything wins much more, finding the enemy is what is missing; if not, the tactics are.

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
