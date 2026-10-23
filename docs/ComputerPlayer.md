# The computer player
What NSPW 1.22's computer does in a single-player game, read from its machine code (`Original/Nspw122/NSPW_122.exe`), which OpenNSPW runs in an emulator ([Singleplayer.md](Singleplayer.md)). This is the specification for rewriting it as C#: each function will be rewritten from its section here, and tested against the emulated original.

The addresses are those of the executable. Unit fields and globals have the names of the NSPW NET source, at 1.22's offsets ([Singleplayer.md](Singleplayer.md#layout)). Distances are in map units (the map is 19200 × 14400, from -9600 to 9600 and from -7200 to 7200); angles are in degrees.

**Not read yet** marks what is still to be read; everything else was read from the disassembly. The rewrite reads each function in full, so the details left out here (exact positions, the formation tables) are in the code at the addresses given.

## When the computer acts
| What | When | Function |
| --- | --- | --- |
| Its strategy | Once a frame, before the orders are applied, if `cpu_side` is set | `cpu_strtgy` (`0x404E30`) |
| A ship's route | When the computer orders a ship to move, and when a ship of its own is blocked | `set_cpu_route` (`0x42DA80`) |
| The scenario's events and result | Once a tick, and once a frame | `decision` (`0x41C3C0`) |

The computer gives orders the way the player does, through `new_pp[0]` and `slct_unit[0]`, at most one a frame. It also changes its units directly: their paths, targets, weapons and fuel.

## Fleets
The computer's units are organized in **fleets**: a table of 11 entries of `0x498` bytes at `0x4867A0`, of which entries 1 to 10 are used. The scenario fills it (`scenario_N`), and `cpu_strtgy` keeps it up to date.

| Offset | Type | Contents |
| --- | --- | --- |
| `+0x000` | `short` | Its state: 1, form again; 50, move; 10 is also tested; 0, nothing to do |
| `+0x002` | `short` | Its flagship (a unit number), or 0 for no fleet |
| `+0x004` | `short` | Whether the first member becomes the flagship when the members change (1) |
| `+0x006` | `short` | Its mode: 0, hold; 1, attack its targets; 50, go to its second point (`+0x488`) |
| `+0x008`, `+0x010` | `double` | The point its flagship aims at |
| `+0x018`, `+0x218` | `double[64]` | Its path: the next move of its flagship is element 0 |
| `+0x42C` | `short[18]` | Its targets: enemy unit numbers, from index 1 |
| `+0x450` | `short[18]` | Its members: unit numbers, the flagship first |
| `+0x474` | `short` | The naval base it is returning to, or 0 |
| `+0x478`, `+0x480` | `double` | Its destination; `x` 9601 (`MAP_RIGHT+1`) for none, as `game_init` sets it |
| `+0x488`, `+0x490` | `double` | Its second point, where mode 50 sends it |

The scenarios build the target lists with these functions, which `cpu_strtgy` also calls when a scenario's events change the plan:

| Function | Adds to the fleet's targets |
| --- | --- |
| `sub_41F230(fleet, kind)` (`0x41F230`) | Every enemy unit of the kind |
| `sub_41F300(fleet)` (`0x41F300`) | Every enemy naval base (kind 12) |
| `sub_41F3C0(fleet)` (`0x41F3C0`) | Every enemy carrier (5, 6) |
| `sub_41F490(fleet)` (`0x41F490`) | Every enemy battleship, cruiser and carrier (1, 2, 5, 6) |
| `sub_404D80(fleet)` (`0x404D80`) | Nothing: it shuffles the list, with `rand()%18+1` random swaps |

Each adds from the first empty element of 1 to 17 (18 when there is none) the units that are not in elements 1 to 17 already, with no limit: past element 17, they are written on into the members. The shuffle counts the targets up to the first empty element, and swaps two random ones (`rand()%n+1` each) `rand()%18+1` times. `sub_403A70(unit, fleet)` (`0x403A70`) tells whether a plane's carrier (`info[1]`) is the fleet's flagship or a member.

## cpu_strtgy
`cpu_strtgy` (`0x404E30`) first clears the computer's order of the last frame (`cls_all_slct_unit_p2(0)`, `new_pp[0].used=0`). Then:

1. **The scenario's events** (`0x404E93` to `0x405554`): a `switch` on `sinario` runs the events of 7 scenarios, each when `rest_time` falls to a threshold, and each once (a flag in the fleet table):

    | Scenario | Code | Events |
    | --- | --- | --- |
    | 8 | `0x4051C2` | At 40000, fleet 5 attacks (mode 1) and targets the transports (10); at 50000, fleet 7 too |
    | 10 | `0x40527F` | At 60000, the American air base of fleet 2 gets 16 strategic bombers with bombs, and fleet 2 attacks the cities (13); at 20000, fleet 4 the same |
    | 101, 102 | `0x405127` | Once the computer has no naval base (12) left, each of fleets 1 to 4 in mode 50 attacks (mode 1) the naval bases and the battleships, cruisers and carriers |
    | 200 | `0x405348` | At 50000, fleets 1 and 2 attack the battleships, cruisers and carriers; while fleet 4's flagship is not a cruiser, fleet 3 goes to its second point, (-880, 40) (mode 50) |
    | 203 | `0x40547E` | At 42000, fleet 6 attacks; at 40000, fleet 7 |
    | 208 | `0x405071` | From 20000, while the Japanese base of fleet 2 has 1 to 15 planes, it gets an attacker or a bomber (one time in 2 each), with bombs, each time. Before 20000, or once the base is gone, 1.22 compares an uninitialized local, `[esp+30h]` (`0x4050C7`), instead of the count, so a rewrite must read the same slot of the stack. Windows' stack holds 0 there in every frame recorded, so 1.22 does nothing then; the port gives the emulator's stack 0 there before each `cpu_strtgy` (`StackLocalsOnWindows`), or what a trace recorded (`GiveStackLocal`) |
    | 209 | `0x404E93` | From 40000, while fleet 8 has no flagship: an American carrier with 12 attackers (bombs), then a cruiser, a destroyer, a cruiser and a destroyer, 150 apart, from (8500 + `rand()%1000`, 6500), heading 135°, become fleet 8, which targets the infantry bases (15), then the battleships, cruisers and carriers, toward (80, 3440) |

    The events are not flags: an event runs again whenever its condition holds again.

2. **One fleet**: fleet `FrameCount%10+1`, so each fleet acts every 10 frames. The battle's first frame is 1, not 0 ([Differences.md](Differences.md)), so fleet 2 acts first. The rest of this section is about that fleet.

3. **The target near its destination** (`0x405554`): if the fleet has a flagship and a destination, the last enemy unit in a square of ±2400 around the destination that is a battleship, cruiser, carrier, light carrier, transport or land force (1, 2, 5, 6, 10, 15 to 17; destroyers, 3, unless the flagship is a battleship), whose `arm[1]` is not 0. In scenario 204, only ground facilities. If the flagship is a carrier, then the last visible (`found`) such enemy within ±800 of the flagship instead, if there is one.

4. **Upkeep** (`0x4057D1`): each member that is dead is removed; each member with no fuel (`gas[0]<=0`) gets 8. A fleet with no member left loses its flagship, and the computer stops using it. When the members' list had gaps, it is packed (each member moved to the first gap before it, the scan starting again at the second element after each move), and if `+0x004` is 1, the first member becomes the flagship and the fleet forms again, which goes straight to the order. A fleet whose flagship is not an air base, has a second member, and is not leading (`is_ltl_ldr==0`) forms again (`+0x000=1`), and does nothing else this frame.

5. **Arming the planes** (`0x405992`): for each member that is a carrier, light carrier or air base (5, 6, 11), each of the computer's attackers and bombers (8, 9) based there (`info[1]`), parked (`stop`), and unarmed (`arm[0]==17`), gets its weapon and full ammunition (`arm[1]=arm[4]`), and `arm[3]=700`:
    - a bomber: bombs (16);
    - an attacker: bombs if the computer plays the United States, if the fleet's first target or the target found at step 3 is a ground facility (11 to 17), or in scenario 204; torpedoes (15) otherwise.

6. **The planes in the air** (`0x405B38`): for each of the computer's attackers and bombers (8, 9) that is flying (`info[0]==1`), leads its group or flies alone, and is not returning (`info[5]!=4`):
    - with no more than 55 fuel and no target, it returns (`info[5]=4`, its ammunition and target cleared, its path ended where it is), and so do the planes that follow it;
    - when it reaches its destination (within 40), for the fleet whose flagship is its carrier: a bomber flies to a random point 500 from the fleet's first target; an attacker takes a random target of the fleet's list and, if it is within 2000, flies to where the target will be (its position plus its heading times its speed times half the distance) and attacks it (`arm[2]`); with no target, it flies to a random point 500 away.

    And for each of the computer's fighters (7) flying and not returning: with no more than 60 fuel, it returns, one time in 10; with no target and not escorting (`info[9]==0`), it returns, with the planes that follow it.

7. **The ships' targets** (`0x40610A`): for each of the computer's units:
    - its target (`arm[2]`) is dropped when the target is dead, parked (`info[0]==2`), or no longer visible (except for a fighter on escort, `info[9]==1`);
    - an armed ship of the fleet (`arm[1]>=1`, not a carrier or a transport): in a carrier or transport fleet, with no target, twice, a random enemy unit that is visible, not a plane and not a submerged submarine, within 900; in a battle fleet, target or not, a random element of the fleet's list: if it is a visible target (not a submerged submarine, nor a ground facility for a submarine), it is taken one time in 2 within 1200; if not, four times, a random enemy unit as above, but no transport, within 1200.

8. **The planes' targets** (`0x406707` to `0x40708A`), in the same loop over the units:
    - With no target anywhere (the unit's, the fleet's first, nor the one found at step 3), one time in 20, a flying plane of the fleet (`sub_403A70`) flies home.
    - An attacker or bomber of the fleet, or one whose carrier is gone, flying, not returning, armed and with no target, attacks: one time in 10 (unless it is a bomber and the fleet has a first target), or when its carrier is gone, a random enemy ship (or, one time in 10, the target found at step 3), visible, not a submarine, within 1000, of eleven tries; else the target found at step 3, within 1000; else the fleet's first target, two times in 3 when it is visible, or a random target of the list, visible, within 1000. A plane with torpedoes never attacks a ground facility.
    - An attacking plane leaves its group, and flies first to its emergency point (`em_x`, `em_y`, `em_flg[0]` = 100 times `no%6`, plus 120): 600 away at its heading, turned by 337.5°, 22.5°, 315° or 45° by `no%6` (1 to 4). 1.22 means to reduce the heading to whole degrees, but stores the int into the low half of the double and reads the double back: the heading keeps its high bits.

9. **The fleet's movement** (`0x40708A` to `0x4092F0`):
    - Dead units are removed from the target list, and the computer's naval base (12) nearest the flagship is found.
    - Dead targets are removed from the list, which is packed like the members, from its first element.
    - **Returning to base** (`+0x474` = that base, and the flagship stops): a carrier with no planes, with no more than 2 attackers (unless its first target is a plane), or at half its HP or less; a transport fleet with no cargo left (`arm[1]`); a battleship, cruiser or destroyer fleet at 65% of its total HP or less while at least 20000 time is left; any fleet below 20% of its total ammunition, or whose first member is below 20% of its own; a flagship whose fuel (`gas[0]*gas[1]*max_spd*0.9`) would not carry it to unit 23 (its position, at `0x499750`), while more than 10000 time is left.
    - **At the base**: once the returning flagship has stopped within ±580 of the base, with no visible enemy battleship, cruiser, destroyer, surfaced submarine or flying attacker or bomber within 900, the flagship and the units that follow it supply (`spry=1`), and the fleet stops returning. A carrier gets its destroyed planes back, rebuilt parked on it (`set_pos_of_parking`, `set_unit_data`); in scenario 204, new attackers with bombs (`set_new_unit_plane`) up to 12, counting the Japanese planes based there.
    - A flagship at the edge of the map (|x| ≥ 9500, |y| ≥ 7100) whose path also leaves the map (|x| ≥ 9600, |y| ≥ 7200) stops, unless a member is supplying.
    - **Choosing a move**: a stopped flagship puts the fleet in state 50 (`+0x000`), unless a member is supplying or the fleet's mode is 0. In state 50, with a one-point path, a returning fleet goes to its base (below); any other aims (`+0x008`, `+0x010`) at its gun target if visible, else at the target found at step 3, else at the first of the fleet's targets (the carrier of a parked plane). Transport fleets, fleets in mode 50 with no gun target and nothing found at step 3, and fleets with nothing to aim at, act by their mode (below). A move goes straight to the order: steps 10 and 11 wait for the next time.
    - **Approaching**: the next move (the fleet's path, `+0x018`) is a point at a random bearing and distance from the flagship, by its kind and its distance d to the aim, whose bearing is b:

        | Flagship | d | Bearing | Distance |
        | --- | --- | --- | --- |
        | Battleship, cruiser, destroyer | 7000 or more | b ± 70 | 1500 to 4500 |
        | | 3000 to 7000 | b ± 70 | 800 to 1800 |
        | | 1500 to 3000 | b ± 45 | 400 to 700 |
        | | 700 to 1500 | b ± 20, or one time in 7 each, 35 to 100 to one side | 200 to 500 |
        | | under 700 | b ± 20, or 35 to 100 to one side, one time in 3 each | 300 to 500 |
        | Carrier, light carrier | 7000 or more (7800 for Japan) | b ± 20 | 1500 to 2000 |
        | | 4500 (5300) to 7000 (7800) | b ± 70 | 1300 to 1800 |
        | | 3550 (4350) to 4500 (5300) | b ± 20 | 800 to 1100 |
        | | under 3550 (4350) | b + 115 to 245, away | 1300 to 1800 |
        | Submarine, reloading (`arm[3]`) | 1500 or less | b + 160 to 200, away | 100 to 300 |
        | Submarine | 800 or more | b ± 20 | 100 to 600 |
        | | 200 to 800 | b | 120 |

        A reloading submarine more than 1500 from the aim, a submarine within 200 of it, and a carrier with a plane flying home more than 3700 from it, give no order at all: `cpu_strtgy` returns. A carrier with a plane flying home within 3700 moves 500 away from the aim. A carrier's move is then kept 2150 from every enemy ground facility (11 to 17): the point is turned around the carrier, 500 away, by 45° (or 45° plus a random 0 to 129°, one time in 10) and more, up to 180°, on one side then the other, until it is clear, or else the carrier moves 300 to 1100 away from the facility, at 80° to 100° to one side.
    - **With nothing to aim at**, by its mode: in mode 1, a transport fleet goes to its aim point plus a random 100 to 250, and any other fleet wanders 500 around its flagship; in mode 50, the fleet goes to its second point plus a random 300. A returning fleet goes to its base, 100 to 500 to its east and south.
    - **The order** (`0x40A949`, at the end of `cpu_strtgy`): in state 1, `sub_403AE0(fleet)` sets the members' formation, and the flagship is ordered to where it is; in state 50, the live members are selected and the flagship is ordered to the fleet's next move, rounded to a multiple of 80 toward 0. `ApplyOrders` then gives the flagship that path, and `set_cpu_route(m, 4.0)` plans it.
    - **Formations** (`sub_403AE0`, `0x403AE0`): the first live carrier among the members is swapped to the front and becomes the flagship. A fleet with a carrier, or led by a transport, forms around its carriers or transports, which it assumes are its first members: the second to fourth by their number (transports 90°/100, 270°/100, 90°/200; carriers 202.5°/140 for two, else 135°/100, 225°/100, 180°/220), then the escorts after them, by their number, 1 to 6, from one table for both (0°/180; 0°/240 and 180°/180; up to 23°, 337°, 90°, 270°, 140°, 220° at 390 or 360), and beyond 6, those six at 340 and the rest 100 further each, at 160° or 200° (transports) or 340° or 380° (carriers) by the parity of their place. Any other fleet forms in line behind its flagship, 180 apart. The live members get the flagship as their leader (`ltl_ldr`), and are selected (`slct_unit[0]`), numbered from 1.

10. **Air strikes** (`0x4091DE` to `0x409C24`), from the fleet's flagship, a carrier or an air base, against a target whose distance is compared with R, 3550, or 4350 when the computer plays Japan. The target is the one found at step 3, or else the fleet's first target (for an air base, a random one of its targets); the flagship must not be launching (`info[4]<=0`), supplying, nor returning to base, and the fleet must have a first target or a target found at step 3. The target is near within R, or up to 1.5 R when the flagship is an air base, the fleet's mode is 50, or `rest_time` is under 8000.
    - **Interception**: when the target is an enemy plane in the air and near, all the ready fighters of the base are given it as their target (`arm[2]`, `info[9]=1`) and selected, and the first is ordered to where the target will be: its position plus its heading times its speed times a third of the distance.
    - **Strike**: against a near target that is not a plane (or, from an air base, a ground facility up to 1.7 R), when all its attackers are ready, they are selected, with its ready fighters as escort, and the first is ordered a quarter of the distance times the target's speed ahead of the target, plus a random ±400 each way when the target's speed is 1 or less.
    - **Strategic bombing** (an air base): when the fleet's first target (or, in scenario 204, the one found at step 3) is within 20000 and is not a plane, and all its strategic bombers (9) are ready, they are selected, with its ready fighters and attackers as escort, and the first is ordered to the target plus 1500 times its speed along its heading.
    - A plane is ready when it is parked (`info[0]==2`, `stop`), armed (`arm[0]` not 17 or 18), with ammunition, and not reloading (`arm[3]==0`); the interception counts the fighters stopped, armed and not reloading, parked or not. Against an air base or a naval base (11, 12), the planes are ordered 300 from it at a random angle instead.

    The order goes in `new_pp[0]`, with the planes numbered in `slct_unit[0]`, as the player's would be.

11. **The other carriers** (`0x409C24` to `0x409F20`): strikes like step 10's (not interceptions), against the fleet's first target, from each other carrier (5, 6) of the fleet, until one gives the order; all its planes but bombers must be ready, and an attacker leads.

    **An air base's fighters** (`0x409F20` to `0x40A2B5`): when no order was given, an air base flagship, not launching (`info[4]`, `info[8]`), during 51 ticks of every 150 (`cc_count%150`), sends its first ready fighter and two more against the enemy attackers and bombers in the air, visible and armed, within 1700: each is given a random one of them as its target, and the first is ordered 200 from the base at a random angle.

12. **Scenario 304** (`0x40A44D` to `0x40A949`, 日本近海の戦い), for the fleet of the frame, also after a move or a new formation:
    - fleet 1 (from 130000) or 2 (from 100000) with no carrier left: if it has no member, or its flagship is supplying, a new American carrier with 10 attackers (bombs) and 2 fighters, at the flagship or at (10100, `rand()%7200`), placed by `sub_414DD0` (`0x414DD0`) near the east edge of the map, becomes its flagship; the fleet attacks the battleships, cruisers, carriers and infantry bases (fleet 1 toward (6640, -3760)). Then it gets cruisers, destroyers or battleships (60, 30 and 10 in 100) until it has its number plus 2 members (plus 1 when it had no carrier).
    - fleets 3 and 4, and 5 from 100000, with no more than 2 live members: cruisers and battleships (60 and 40 in 100) until they have 3; a new fleet attacks the land forces (fleet 3 toward (6640, -3760)).

## set_cpu_route
`set_cpu_route(m, a)` (`0x42DA80`) plans the route of the computer's ship `m` to `pp_x[0]`, `pp_y[0]`. It is NSPW NET's `set_cpu_root2` (`input.cpp`), with these differences:

| | NSPW NET's `set_cpu_root2` | 1.22's `set_cpu_route` |
| --- | --- | --- |
| A destination on land, or in another ship | Return, the path unchanged | The ship stops: its path is its own position |
| Steps along a leg | 1 for the first 80, then 40 | 1 for the first 320, then 40 |
| Headings tried around an obstacle | 8 each side, 22.5° apart | 18 each side, 10° apart |
| Waypoints | At most one (`while(pp_indx==0)`) | Until the last leg is clear (`while(pp_indx>=0)`) |
| Debug output | Commented out | `SetWindowText` "in Set_cpu_root", "Panzer Vor!" for each waypoint, "out Set_cpu_root", when not full screen |

The same as NSPW NET:

- A leg is walked from its start toward its end. It is blocked where it reaches a tile of `cmbt_map` that is not sea (`>=1`), or a ship's rectangle (`sprt[UNIT_JPN]`'s size around it; submerged submarines and ground facilities, kinds 11 to 17, do not count).
- When a leg is blocked, the headings on each side are tried, nearest first, and the first side whose leg is clear for the whole distance wins. The waypoint is on that heading, at half the clear distance (at least 40).
- A waypoint that would make the route turn back (between 90° and 270° from the previous leg) is moved back: the destination is moved 160 further, up to 2000, and the waypoint is planned again.

The second argument, a `double` (4.0 for a new order, 1.0 for a blocked ship), is not used.

`set_cpu_route2` (`0x42ED30`) is NSPW NET's `set_cpu_root2` as 1.22 compiled it. 1.22's formation (`set_pos_of_dynmc`, `0x414D79`) calls it for a ship that follows a leader, every 10 ticks; the port runs it in the emulator there (`SetCpuRoute2As122`), as NSPW NET's, run natively, took the other side of an obstacle where both were clear, 22.5° either way, in a trace of scenario 203.

### Blocked ships
1.22's movement (`chara_cont`) also replans the computer's ships that follow no leader (`ltl_ldr==0`), which NSPW NET does not:

- **Land ahead** (`0x40229D`): when the point 80 ahead of the ship toward its path is land (a tile of 1 to 9), and it has no emergency move (`em_flg[0]==0`).
- **A ship ahead** (`0x4020DB`): every 100 ticks (`(cc_count+m)%100==0`), when the ship is at no more than a fifth of its top speed. If its destination is in that ship's rectangle, the destination becomes its own position.

Its path becomes its final destination alone, and `set_cpu_route(m, 1.0)` plans it again. The port does not: its single-player game moves the ships as NSPW NET does, which stops them until the computer gives them another order.

## decision
`decision` (`0x41C3C0`) runs in three steps:

1. While `game_end` is 0, the scenario's conditions, a `switch` on `sinario` (scenarios 1 to 10 at `0x41DFCC`, the others through a byte table), may set it.
2. If `game_end` is set, the result's three lines, from the messages of `0x4414B0` to `0x4415A4`, in white at (300, 300), 20 apart: the same as the port's `DrawResult`.
3. The events of scenarios 204, 301 and 304, with or without a result.

Each scenario counts units by side and kind, and compares the time left (`rest_time`) and `decision_point`. For example, scenario 3: Japan has no carrier (5, 6) left → the United States win; the United States have no infantry base (15) left → Japan wins; then their planes.

The setting screen (`0x40B406`) states each scenario's side and victory conditions, from the strings at `0x43E0C0` to `0x441030`. `decision` implements them; its code is to be read against them, scenario by scenario:

| Scenario | Player | Stated conditions |
| --- | --- | --- |
| 1 | United States | Win when every target ship is sunk; lose when time runs out |
| 2 | Japan | Do heavy damage to the American ships in Pearl Harbor; lose when time runs out |
| 3 | United States | Win by holding Wake's infantry base until the end, or by sinking the enemy carriers; lose with that base or the air units |
| 4 | Japan | Be the first to sink an enemy carrier; lose when time runs out |
| 5, 6 | United States | Sink every enemy carrier; lose when time runs out |
| 7 | United States | Hold Guadalcanal's air base until the end, or destroy the Japanese navy |
| 8 | Japan | Win when anchored ships remain at the end; lose when they are all sunk |
| 9 | Japan | Win by destroying every enemy carrier; lose when time runs out or every carrier is lost |
| 10 | Japan | Japan loses with all its cities, the United States with all their air units; a draw when time runs out |
| 100 | Japan | Win by sinking an enemy carrier; lose with the base on Guadalcanal or the submarines |
| 101, 102 | Japan, United States | Win by sinking every enemy battleship and cruiser |
| 103 | United States | Win by destroying the enemy air units; lose with every ship in Pearl Harbor |
| 104 | Japan | Destroy the enemy ground forces on Guadalcanal in time; anything else loses |
| 105, 106 | Japan, United States | Win by destroying the enemy carriers; lose with one's own |
| 107 | United States | Shoot down the plane carrying Yamamoto |
| 108 | Japan | Keep at least one base on the islands east of Rabaul until the end |
| 109 | United States | Sink the carriers, battleships, cruisers and transports at Yokosuka, and return near the start; lose when time runs out |
| 200 | Japan | Destroy every enemy facility on Guadalcanal; lose when time runs out or with a carrier |
| 201 | Japan | Sink the American carrier; anything else loses |
| 202 | United States | Destroy the cities of Tokyo in time |
| 203 | United States | Sink many enemy ships; lose with Midway's pillboxes |
| 204 | United States | Hold Guadalcanal's ground facilities until the end |
| 205 | United States | Land Noumea's strategic bombers at Bougainville's air base, and bring them back to Noumea |
| 206 | United States | Sink the battleship Yamato; lose when time runs out |
| 207 | United States | Destroy Rabaul's air base and return near the start; lose with every battleship and cruiser |
| 208 | United States | Hold Guadalcanal's infantry base until the end, or destroy the enemy |
| 209 | Japan | Keep a base on Midway at the end, and destroy the enemy ground bases |
| 300 | United States | Destroy two cities of Tokyo in time, and gather every bomber left at the bottom left of the map |
| 301 | Japan | Destroy two enemy carriers and Port Moresby's infantry base, pillboxes and fortress in time, keeping the city, with a base built next to it |
| 302 | Japan | Destroy Midway's ground facilities in time, without losing a carrier |
| 303 | United States | Not stated |
| 304 | Japan | Hold the ground facilities until the end |
| 305 | Japan | Destroy the American bases on Okinawa in time |
| 306 | Japan | Destroy the enemy on Guadalcanal in time, and build at least two ground facilities |

The code decides as follows, in order; the first condition that holds gives the result. J and U are Japan's and the United States' units; "at the end, R" means R once `rest_time` is 0, and no result before. The results are those of `game_end`: J wins (1), U wins (2), J loses (3), U loses (4), and a draw (10).

| Scenario | Conditions |
| --- | --- |
| 1 | J has a battleship, cruiser or destroyer: at the end, U loses; else U wins |
| 2 | No U unit is anchored (fuel ≤ -1): J wins; at the end, J loses with more than 3 anchored, and wins with 3 or fewer |
| 3 | J has no carrier: U wins; U has no infantry base: J wins; U has attackers or fighters: at the end, U wins; else J wins |
| 4, 5, 6, 105, 106 | J has no carrier: U wins; U has a carrier: at the end, a draw; else J wins |
| 7 | Unit `decision_point[0]` is gone: J wins; J has a battleship, cruiser, destroyer, attacker or bomber: at the end, U wins; else U wins |
| 8 | J has an anchored unit: at the end, J wins; else U wins |
| 9 | J has no carrier: U wins; U has a carrier: at the end, U wins; else J wins |
| 10 | J has no city: U wins; U has attackers, bombers or fighters: at the end, a draw; else J wins |
| 100 | J has no submarine, or no ground facility (15 to 17): U wins; U has a carrier: at the end, a draw; else J wins |
| 101, 102 | J has no battleship or cruiser: U wins; U has one: at the end, a draw; else J wins |
| 103 | U has no battleship or cruiser: J wins; J has an attacker: at the end, U wins; else U wins |
| 104 | U has no ground facility: J wins; J has a unit of kinds 1 to 10 whose fuel is not -1: at the end, U wins; else U wins |
| 107 | J has no bomber: U wins; J has a bomber parked and serviced (`arm[0]` 18): J wins; else at the end, J wins |
| 108 | J has an infantry base or pillboxes: at the end, J wins; else U wins |
| 109 | No J unit is anchored, and U's ships are all in (7000, -8000)–(9000, -6000): U wins; U has no unit left: J wins; else at the end, U loses |
| 200 | U has no air base or ground facility: J wins; J has a carrier (5): at the end, U wins; else U wins |
| 201 | U has a carrier: at the end, U wins; else J wins |
| 202 | U has no bomber: J wins; J has a city: at the end, a draw; else U wins |
| 203 | U has no pillboxes: J wins; J has more than 4 carriers, battleships, cruisers and transports: at the end, U wins; else U wins |
| 204 | U has no ground facility in (-1040, -240)–(-800, -160): J wins; else at the end, U wins |
| 205 | U has no bomber: U loses. Until `decision_point[1]` is set, it is set to 1 when a U bomber is parked and serviced at unit `decision_point[0]`, and U loses when that unit has a fifth of its HP or less. Then a U bomber parked and serviced at `decision_point[2]` wins for U; but at the end, J wins, even then |
| 206 | J has no battleship: U wins; U has a ground facility: at the end, a draw; else J wins |
| 207 | Unit `decision_point[0]` is gone, and U's ships (but naval bases) are all in (4600, -3400)–(6600, -1400): U wins; U has a battleship or cruiser: at the end, U loses; else J wins |
| 208 | J has no battleship, cruiser, destroyer, attacker or bomber: U wins; U has an infantry base or pillboxes: at the end, U wins; else J wins |
| 209 | At the end only: U has an infantry base or pillboxes: J loses; J's infantry bases, if any, are all in (-80, 3120)–(400, 3600): J wins; else J loses |
| 300 | U has no bomber: U loses; J has more than 3 cities: at the end, U loses; U's bombers are all beyond x < -8400 and y < -6000: U wins; else at the end, U loses |
| 301 | U has a ground facility or carrier, other than unit `decision_point[0]`: at the end, J loses; J has a ground facility within 80 of (`decision_point[1]`, `decision_point[2]`): J wins; else at the end, J loses |
| 302 | Units `decision_point[0]` and `[1]` are gone: J wins; else J loses, at the end if J has more than 2 carriers, and at once if not |
| 303 | The events below; then, no J or U fighter of type 1 is left: U loses; U's fighters of type 1 are all parked and stopped at unit 21: U wins; else at the end, U loses |
| 304 | J has a ground facility: at the end, U loses; else U wins |
| 305 | U has no ground facility: J wins; J has no battleship, cruiser or destroyer: J loses; else at the end, J loses |
| 306 | U has no ground facility in (-1040, -240)–(-800, -160), and J has at least two: J wins; else at the end, U wins if U still has one there, and J loses if not |

The rectangles, from their smallest x and y to their largest, include their edges.

Scenario 303's events, before its conditions:

- The transport `decision_point[3]` loses 1 HP every 10 ticks.
- For each U infantry base, and each unit, one time in 200 (`rand()%200`, drawn for every pair), a J air base within 80 of it falls: its parked J planes of type 0 are destroyed, and the others change sides, and the air base becomes U's, numbered in `decision_point[0]`, or `[1]` if `[0]` is set. If any plane changed sides, each of the computer's fleets 1 to 8 with a flagship targets units 41, 42 and 43, and its flagship stops.
- An air base in `decision_point[0]` or `[1]` with no U infantry base within 80 goes back to J, and its entry is cleared.

The events of the other scenarios give Japan's unit 1 a plane (`set_new_unit_plane(1, kind, 0, 1, 1, 17)`), unarmed, while it has fewer planes than its capacity (`info[2]`):

| Scenario | When | Planes counted | Kind, by `rand()%100` |
| --- | --- | --- | --- |
| 204 | Every 1250 of `rest_time`, and every 300 under 13000 | J's planes based there | Attacker to 50, bomber to 95, else fighter |
| 301 | Every 2500 | Parked there (`plane_in_cv(1)`) | Attacker to 60, fighter to 98, else bomber |
| 304 | Every 4500 | Parked there (`plane_in_cv(1)`) | Attacker to 75, fighter to 98, else bomber |

## Next
1. Rewrite the functions as C#, starting with `set_cpu_route`, each with a test that compares it with the emulated original on states recorded from games and random ones. The rewrite reads each function in full, and this document gets the details it finds.
2. Rewrite the scenarios' setups (`scenario_N`).
