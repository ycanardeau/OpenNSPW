# The computer player
What NSPW 1.22's computer does in a single-player game, read from its machine code (`Original/Nspw122/NSPW_122.exe`), which OpenNSPW runs in an emulator ([Singleplayer.md](Singleplayer.md)). This is the specification for rewriting it as C#: each function will be rewritten from its section here, and tested against the emulated original.

The addresses are those of the executable. Unit fields and globals have the names of the NSPW NET source, at 1.22's offsets ([Singleplayer.md](Singleplayer.md#layout)). Distances are in map units (the map is 19200 × 14400, from -9600 to 9600 and from -7200 to 7200); angles are in degrees.

This is a first version. **Not read yet** marks what is still to be read; everything else was read from the disassembly.

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
| `+0x000` | `short` | Whether the fleet must form again (1) |
| `+0x002` | `short` | Its flagship (a unit number), or 0 for no fleet |
| `+0x004` | `short` | Set to 1 when the fleet forms again; meaning not known yet |
| `+0x006` | `short` | Its state; 50 is checked by the air strikes; not read yet |
| `+0x008`, `+0x010` | `double` | A point; not read yet |
| `+0x42C` | `short[18]` | Its targets: enemy unit numbers, from index 1 |
| `+0x450` | `short[18]` | Its members: unit numbers, the flagship first |
| `+0x474` | `short` | Cleared when the fleet forms again |
| `+0x478`, `+0x480` | `double` | Its destination; `x` 9601 (`MAP_RIGHT+1`) for none, as `game_init` sets it |
| `+0x488`, `+0x490` | `double` | A second point; not read yet |

The scenarios build the target lists with these functions, which `cpu_strtgy` also calls when a scenario's events change the plan:

| Function | Adds to the fleet's targets |
| --- | --- |
| `sub_41F230(fleet, kind)` (`0x41F230`) | Every enemy unit of the kind |
| `sub_41F300(fleet)` (`0x41F300`) | Every enemy naval base (kind 12) |
| `sub_41F3C0(fleet)` (`0x41F3C0`) | Every enemy carrier (5, 6) |
| `sub_41F490(fleet)` (`0x41F490`) | Every enemy battleship, cruiser and carrier (1, 2, 5, 6) |
| `sub_404D80(fleet)` (`0x404D80`) | Nothing: it shuffles the list, with `rand()%18+1` random swaps |

A unit is added once; the list holds 17 at most. `sub_403A70(unit, fleet)` (`0x403A70`) tells whether a plane's carrier (`info[1]`) is the fleet's flagship or a member.

## cpu_strtgy
`cpu_strtgy` (`0x404E30`) first clears the computer's order of the last frame (`cls_all_slct_unit_p2(0)`, `new_pp[0].used=0`). Then:

1. **The scenario's events** (`0x404E93` to `0x405554`): a `switch` on `sinario` runs the events of 7 scenarios, mostly reinforcements created with `set_new_unit` and `set_new_unit_plane` when `rest_time` reaches a threshold, and new targets for their fleets:

    | Scenario | Code |
    | --- | --- |
    | 8 | `0x4051C2` |
    | 10 | `0x40527F` |
    | 101, 102 | `0x405127` |
    | 200 | `0x405348` |
    | 203 | `0x40547E` |
    | 208 | `0x405071` |
    | 209 | `0x404E93` |

    **Not read yet**: what each one does.

2. **One fleet**: fleet `FrameCount%10+1`, so each fleet acts every 10 frames. The rest of this section is about that fleet.

3. **The target near its destination** (`0x405554`): if the fleet has a flagship and a destination, the last enemy unit in a square of ±2400 around the destination that is a battleship, cruiser, carrier, light carrier, transport or ground facility (1, 2, 5, 6, 10, 15 to 17; destroyers, 3, only if the flagship is a battleship), whose `arm[1]` is not 0. In scenario 204, only ground facilities. If the flagship is a carrier, then the last visible (`found`) such enemy within ±800 of the flagship instead, if there is one.

4. **Upkeep** (`0x4057D1`): each member that is dead is removed; each member with no fuel (`gas[0]<=0`) gets 8. A fleet with no member left loses its flagship, and the computer stops using it. When the members' list had gaps, it is packed, and if `+0x004` is 1, the first member becomes the flagship and the fleet forms again. A fleet whose flagship is not an air base, has a second member, and is not leading (`is_ltl_ldr==0`) forms again (`+0x000=1`), and does nothing else this frame.

5. **Arming the planes** (`0x405992`): for each member that is a carrier, light carrier or air base (5, 6, 11), each of the computer's attackers and bombers (8, 9) based there (`info[1]`), parked (`stop`), and unarmed (`arm[0]==17`), gets its weapon and full ammunition (`arm[1]=arm[4]`), and `arm[3]=700`:
    - a bomber: bombs (16);
    - an attacker: bombs if the computer plays the United States, if the fleet's first target or the target found at step 3 is a ground facility (11 to 17), or in scenario 204; torpedoes (15) otherwise.

6. **The planes in the air** (`0x405B38`): for each of the computer's attackers and bombers (8, 9) that is flying (`info[0]==1`), leads its group or flies alone, and is not returning (`info[5]!=4`):
    - with no more than 55 fuel and no target, it returns (`info[5]=4`, its ammunition and target cleared, its path ended where it is), and so do the planes that follow it;
    - when it reaches its destination (within 40), for the fleet whose flagship is its carrier: a bomber flies to a random point 500 from the fleet's first target; an attacker takes a random target of the fleet's list and, if it is within 2000, flies to where the target will be (its position plus its heading times its speed times half the distance) and attacks it (`arm[2]`); with no target, it flies to a random point 500 away.

    And for each of the computer's fighters (7) flying and not returning: with no more than 60 fuel, it returns, one time in 10; with no target and not escorting (`info[9]==0`), it returns, with the planes that follow it.

7. **The ships' targets** (`0x40610A`): for each of the computer's units:
    - its target (`arm[2]`) is dropped when the target is dead, a plane, or no longer visible (except for a fighter on escort, `info[9]==1`);
    - an armed ship of the fleet (`arm[1]>=1`, not a carrier or a transport) with no target picks one: in a carrier or transport fleet, twice, a random enemy unit that is visible, not a plane and not a submerged submarine, within 900; in a battle fleet, a random target of the fleet's list, one time in 2, within 1200 (a submarine never targets a ground facility), or else a random enemy unit as above.

8. **The planes of the fleet** (`0x4066ED` to `0x40708A`): the fleet's attackers and bombers (`sub_403A70`) that are flying are sent back to their carrier, one time in 20 when the fleet has no target; **not read yet** in detail.

9. **The fleet's movement** (`0x40708A` to `0x4092F0`):
    - Dead units are removed from the target list, and the computer's naval base (12) nearest the flagship is found.
    - **Returning to base** (`+0x474` = that base, and the flagship stops): a carrier with no planes, with no more than 2 attackers (unless its first target is a plane), or at half its HP or less; a transport fleet with no cargo left (`arm[1]`); a battleship, cruiser or destroyer fleet below 65% of its total HP while at least 20000 time is left; any fleet below 20% of its total ammunition, or whose flagship is; a flagship whose fuel (`gas[0]*gas[1]*max_spd*0.9`) would not carry it to the point at `0x499750`, while more than 10000 time is left.
    - **At the base**: once the returning flagship has stopped within ±580 of the base, with no visible enemy ship, surfaced submarine or flying attacker or bomber within 900, the fleet supplies (`spry=1`) and stops returning. A carrier gets its destroyed planes back, rebuilt parked on it (`set_pos_of_parking`, `set_unit_data`); in scenario 204, new attackers with bombs (`set_new_unit_plane`) up to 12.
    - A flagship near the edge of the map (|x| ≥ 9500, |y| ≥ 7100), or whose path leaves the map, stops.
    - **Choosing a move**: a stopped flagship that is not supplying puts the fleet in state 50 (`+0x000`). In state 50, with a one-point path, it aims (`+0x008`, `+0x010`) at its gun target if visible, else at the target found at step 3, else at the first of the fleet's targets (a plane's carrier for a plane in the air). Transport fleets go to `0x408F8A` (**not read yet**).
    - **Approaching**, by the kind of the flagship, from the distance and bearing to the aim: a carrier at `0x4086F3`; a submarine that is reloading (`arm[3]`) moves away when within 1500 (bearing + 160 + `rand()%40`, 100 to 300 away), otherwise it closes in from 800 (bearing ± 20, 100 to 600 away), and within 200 it steps 120 along the bearing; the other kinds compare with 7000, 3000, 700, 200 and 100 and move at bearing ± 45, 70, 90 or 115. **Not read yet** in detail.

10. **Air strikes** (`0x4092F0` to `0x409C24`), from the fleet's flagship, against a target whose distance is compared with R, 3550, or 4350 when the computer plays Japan. Which target, and some of the conditions, are **not read yet**; what is read:
    - **Interception from a carrier**: when the target is an enemy plane in the air, within R (or up to 1.5 R when the flagship is an air base, the fleet's state is 50, or `rest_time` is under 8000), all the ready fighters of the carrier are selected, and the first is ordered to where the target will be: its position plus its heading times its speed times a third of the distance.
    - **Strike from a carrier**: against a target that is not a plane, further out (up to 1.7 R; ground facilities when the flagship is an air base), its ready attackers are selected, with its fighters as escort, and ordered a quarter of the distance ahead of the target, plus a random ±400 when the target is slower than 1.
    - **Strike from an air base** (a flagship of kind 11): when the target is within 20000, its ready strategic bombers (9) are selected, with its fighters and attackers as escort, and ordered to the target plus 1500 times its speed along its heading.
    - A plane is ready when it is parked (`info[0]==2`, `stop`), armed (`arm[0]` not 17 or 18), with ammunition, and not reloading (`arm[3]==0`). The planes of a base are selected only when all of them are ready. Against an air base or a naval base (11, 12), the planes are ordered at a random angle instead.

    The order goes in `new_pp[0]`, with the planes numbered in `slct_unit[0]`, as the player's would be.

11. **Not read yet**: `0x409C24` to `0x40A43E` (the same strikes from the fleet's other carriers, it seems), and `0x40A44D` to the end: `sub_414DD0` (`0x414DD0`, three times), reinforcements with `set_new_unit` and `set_new_unit_plane`, and `sub_403AE0` (`0x403AE0`).

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

`set_cpu_route2` (`0x42ED30`) is the same as NSPW NET's `set_cpu_root2`; nothing that the port emulates calls it.

### Blocked ships
1.22's movement (`chara_cont`) also replans the computer's ships that follow no leader (`ltl_ldr==0`), which NSPW NET does not:

- **Land ahead** (`0x40229D`): when the point 80 ahead of the ship toward its path is land (a tile of 1 to 9), and it has no emergency move (`em_flg[0]==0`).
- **A ship ahead** (`0x4020DB`): every 100 ticks (`(cc_count+m)%100==0`), when the ship is at no more than a fifth of its top speed. If its destination is in that ship's rectangle, the destination becomes its own position.

Its path becomes its final destination alone, and `set_cpu_route(m, 1.0)` plans it again. The port does not: its single-player game moves the ships as NSPW NET does, which stops them until the computer gives them another order.

## decision
`decision` (`0x41C3C0`) checks the scenario's result with a `switch` on `sinario` (scenarios 1 to 10 at `0x41DFCC`, the others through a byte table), and then, if `game_end` is set, shows the result with the messages of `0x4414B0` to `0x4415A4`.

Each scenario counts units by side and kind, and compares the time left (`rest_time`) and `decision_point`. For example, scenario 3 (ウェーク島防衛): Japan has no carrier (5, 6) left → the United States win; the United States have no infantry base (15) left → Japan wins; and then its air units; **not read yet** in full.

The executable also holds, for each scenario, its description and victory conditions as the setting screen shows them (`0x43E0C0` to `0x441030`, such as 勝敗条件：制限時間内にガダルカナル島の敵を殲滅し…), which tell what `decision` checks.

**Not read yet**: the conditions of each scenario.

## Next
1. Read the rest of `cpu_strtgy` (steps 8, 9 and 11, the scenario events).
2. Read `decision`, scenario by scenario, against the victory conditions in the executable.
3. Rewrite `set_cpu_route` as C#, with a test that compares it with the emulated original on recorded and random states.
