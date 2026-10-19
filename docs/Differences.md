# Differences from NSPW 1.22
A single-player game plays a scenario of NSPW 1.22 (`Original/Nspw122/NSPW_122.exe`) against its computer. It runs 1.22's own code, in the emulator ([Singleplayer.md](Singleplayer.md)), for the scenarios, the computer (`cpu_strtgy`, `set_cpu_route`, [ComputerPlayer.md](ComputerPlayer.md)) and the victory conditions (`decision`). Everything else is NSPW NET's: the battle (`chara_cont` and the functions it calls), the frame, the input, the drawing and the data.

This document lists every difference found between the two, read from 1.22's machine code, with the address where it shows. The single-player game follows 1.22 in the first table only; the others are left as NSPW NET has them, so that the port keeps a single battle, the one network games play.

## Followed in single-player
| Difference | 1.22 | NSPW NET | Where in the port |
| --- | --- | --- | --- |
| Unit numbers | 20 ships and 30 planes a side | 40 and 50 | `SHIP_LIMIT`, `PLANE_LIMIT` |
| Orders | Given and applied in the frame they are made | Exchanged by turn over the network | `IssueSingleplayerOrders` |
| Move orders of the computer | Clear the unit's path (`if( new_pp[e].cls || unit[m].used==cpu_side )`), and a ship's route is planned by `set_cpu_route` | The same condition is a comment; routes are planned by `set_cpu_root2` | `ApplyOrders` |
| Time | `rest_time` counts down every tick | Counted by the host | `UpdateBattle` |
| Victory | `decision` | NSPW NET's conditions | `CheckResult` |
| The data: maps, sprite sheet and sounds | 1.22's own: its `japan_off.dat` has no island at tiles 56 to 64 across and 164 to 180 down (scenarios 9, 10, 109, 202, 206, 300, 304 and 305), on which `set_cpu_route` also plans; `tst_cg1.bmp`, of 256 colours, with another title, 599 by 387 (`set_sprt_data`, `0x432E43`), and a single-engine land-based fighter for Japan (row 14); 28 sounds, where `GUN3` and `AA_SHL4` are other sounds and there are no `AA_SHL5`, `SNR` and `CLICK2` | NSPW NET's (`Original/NSPW_NET`): a small island there; `t3.bmp`, with NSPW NET's title, 699 by 384, a twin-engine fighter and three more icons; NSPW NET's sounds | Read from 1.22's directory (`GameFiles.OpenSingleplayer`, `SpriteSheetName`, `InitializeSprites`, and the UI's `GameSession.SpriteSheetPath`); the three sounds it does not have are NSPW NET's |
| Captured fighters in scenario 303 | The Americans' fighters of type 1, which `decision` takes with the Japanese air bases, are drawn from Japan's sheet (`0x40E070`: `sinario==303`, `used==2`, `kind==7`, `type==1`) | Every unit is drawn from its side's sheet | `GetUnitSprites`, for `DrawBattleArea` and the UI's battlefield (`ReadUnitSprites`) |
| Orders to the selected units | Go to every unit selected | Go to the selected units whose numbers are the player's side's, the ones the network sends; the units of the side with the other side's numbers, such as the fighters taken in scenario 303 (41 to 43), are left out, so they took off one at a time and could not be sent to another base | `BufferSelections`, `IssueSingleplayerOrders` |
| Supply away from a naval base | A unit supplies (`spry`) wherever it is (`0x4010BB`): nothing checks for a naval base | Supply stops at once unless one of the side's naval bases is within ±280 | `UpdateSupply` |
| Ships outside the map | Only the player's ships are brought back (`0x416B35`, the end of `set_pos_of_emrgncy_SHIP` at `0x415F40`, which skips the units of `cpu_side`); submarines never (`0x401966`: kind 4 does nothing) | Every ship, and every submarine, gets an emergency move back to 40 inside the edge | `SetShipEmergencyDestination`, `SteerUnit` (submarines) |
| Units' data | `set_unit_data` (`0x41E150`) gives other HP, speeds, weapons and fuel: a battleship has 41 HP and burns fuel every 9000 ticks, a carrier 36 or 41 HP | `SetBattleshipData` and the others: a battleship 40 HP and every 1000 ticks, a carrier 32 or 37 | `SetUnitData` calls 1.22's `set_unit_data` in the emulator, for the units the battle creates, such as the bases that transports land; the scenarios' units get it from 1.22's own setup |
| Planes added to a full carrier | `set_new_unit_plane` (`0x41F060`) adds them: it does not check the carrier's capacity | Returns, the planes not added | `AddPlane` |
| Transports landing | Land their cargo within 360 of the landing point (`0x413003`) | Within 160 (`#if NSPW_THE_NET`) | `LandCargo` |
| Transports with cargo | Not slowed (`0x402955`) | Slowed to 0.8 to 1.0 of the top speed | `MoveUnit` |
| Bases that transports land | Complete at once, with their full HP (`0x418E23`) | Built over 8000 to 20000 ticks, from a quarter of their HP | `UpdateCargo` |
| Attackers' bombs | Every attacker releases from 170 to 180 of its aim and speeds up (`a_spd_add` × 700); the bomb starts 130 ahead; only the Americans' drop a second one; the planes that followed it keep their path (`0x414186` to `0x4144E0`) | The Japanese attackers dive: they release from 35 to 65, pull up to 300 ahead, and their bombs start 13 ahead and fall 10 ticks late; the followers fly to where the leader released | `AttackerDropBomb` |
| Fighters and transports | A fighter shoots flying planes only (`0x4133B2`), and cannot be ordered to attack a ship (`0x430618`, `0x430679`); a ship flees attackers and bombers only (`0x41650D`) | A fighter also shoots transports, and can be ordered to attack one; a transport flees fighters too | `FighterFireBullet`, `ApplyOrders`, `SetShipEmergencyDestination` |

Supply and ships outside the map together let the computer's fleets renew. In scenario 304 the American naval base (unit 21) is at x 10600, outside the map. A fleet that has lost its carrier, its ammunition or its fuel returns there ([ComputerPlayer.md](ComputerPlayer.md), `cpu_strtgy` step 9); there it supplies, which gives back its HP, ammunition and fuel, and step 12 puts a new carrier at its supplying flagship. NSPW NET's battle would turn the fleet back at the edge, and stop its supply away from a base within 280.

How the off-map rule was found: the port's `em_of_out_of_map` sends a ship to 9560 or 7160 (`MAP_RIGHT-40`, `MAP_TOP-40`). In 1.22 those values are immediate operands (`mov dword ptr [esi+0x491b6c], 0x40c2ac00`, the high half of 9560.0) at `0x416B5C` to `0x416C8B`, after `cmp word ptr [esi+0x4916a0], cpu_side` / `je` at `0x416B3C`, so a search of the data for the doubles does not find them.

## The battle
What 1.22's `chara_cont` (`0x401000`) and the functions it calls do differently from the port's battle (`UpdateBattle` and the functions it calls), read instruction by instruction.

| Difference | 1.22 | NSPW NET |
| --- | --- | --- |
| The order of a tick | The tick is counted first, then the clouds (`0x418A20`) and the detection (`0x418230`), then the end of the game is checked (`0x401000` to `0x401039`) | The detection, the tick, the end of the game, the clouds |
| `set_pos_of_emrgncy_SHIP` (`0x415F40`) | Its evasions are laid out differently; it sets `em_flg[1]` before the off-map rule (`0x416B2F`) | `SetShipEmergencyDestination`; not compared yet |
| `set_new_unit_plane` with no parking place left (`0x41F111`) | The place number of the previous plane added, or, for the first, whatever its stack holds | `AddPlane`: 0, or the previous plane's |
| A plane landed on deck | Clears its base's launch lock (`info[8]`, `0x4018F1`) | Sets it to 2 plus the number of the player's other planes landing |
| Attackers' approach (`set_pos_of_attack_AT`, `0x415A10`) | No change of formation | At 500 to 510 from the target, the leader sends its attackers off to 600 ahead, fanned by their place in the formation, before they turn to the target |
| Returning planes | Adjust their approach to their carrier on the drawn tick only (`0x417530`), whatever its kind | On every tick; a base that is not a carrier or an air base is forgotten |
| Reloading and rearming (`0x403043` to `0x4030B6`) | `arm[3]` counts down, a submarine's only while it is stopped; when it reaches 0, a unit being serviced (weapon 17 or 18) gets 100 fuel and, a fighter, bullets (11) and 50 rounds; any other is left unarmed (17), with no ammunition, to be armed | Not compared yet |
| Computer's ships blocked by land ahead | Replanned (`set_cpu_route(m, 1.0)`) when the point 80 ahead toward the path is land (`0x40229D`) | Steering stops the ship until the computer gives it another order (`CheckCourseAhead`) |
| Computer's ships slowed by a ship ahead | Replanned every 100 ticks below a fifth of the top speed (`0x4020DB`) | Steering stops the ship (`CheckCourseAhead`) |
| Computer's units about to step on land | Every 500 ticks (`cc_count%500`), a computer's unit with no leader whose next step (its speed along its heading) is land is replanned (`0x402D2B`), planes included | Nothing (`MoveUnit`) |
| Ships ahead | A ship looks 40 ahead (`0x401FAC`); only air bases and naval bases are not in its way (`0x40202D`) | 40 plus ten times its top speed; no base, infantry base or fortress is in the way (kinds 11 to 17) (`CheckCourseAhead`) |
| Fuel | An integer (`gas[0]`): 1 every `gas[1]` ticks while moving (`0x402F72`) | A `double`: `Speed/MaxSpeed` every interval |
| Targets | A submerged submarine stays a target (`0x402FD2`) | Is dropped |
| Detection | The player's supplying units look out too; a submarine dives when it finds any unit, a surfaced submarine too, even while supplying; when the computer finds the player's submerged submarine, its destroyers chasing it (`em_flg[1]`) stop (`0x418724`) | Supplying units do not look out; a submarine dives when it finds a unit other than a submarine, not while supplying |
| Detection ranges (`0x418080`) | A battleship 900, a cruiser 800, a destroyer 700, a carrier 600, a light carrier 700; a base being built sees as far as built; a submerged submarine is seen within 80 by any unit but a destroyer, unless that unit is supplying, a plane, a transport, a carrier, a light carrier or a battleship, and within 280 by a destroyer, 140 above a third of its top speed, whatever its type; a surfaced submarine within half the range, at most 300, also while it supplies | 800, 700, 550, 500 and 600; half a fighter's while built; other ranges for submarines, by type |
| Firing (`0x4030D8` to `0x40396E`) | No firing for Japan in scenario 1; an infantry base's gun 1 in 200 a tick, pillboxes' 150, a fortress's 100; every battleship alike; no anti-aircraft cruisers or second type of destroyer; transports land infantry bases, pillboxes and fortresses only; bombers drop no torpedoes | 250, 200 and 150; a Yamato class with other rates; anti-aircraft cruisers and a second type of destroyer; transports also land naval bases and air bases; bombers drop torpedoes |
| Fires and effects | 255 fires and 255 effects in use at most | 511 and 1023 |
| The sonar | No sound | Played near the player's submerged submarines, drawing random numbers |
| Effects | As the effects function (`0x41AEB0`) does them | `UpdateSurfaceShipEffects` and the others, which draw random numbers every tick |

The rest of the battle (shooting and hits in `fire_now`, `0x40FE10`, the fires in `0x418C30`, the emergency moves of each kind) has not been read. Running a tick of the port's battle and one of 1.22's, emulated, from the same state, and comparing them, would find the remaining differences.

## The frame, the input and the drawing
| Difference | 1.22 | NSPW NET |
| --- | --- | --- |
| The frame | A multimedia timer of 88 ms ([below](#122s-frame)); speed + the timer's ticks a frame, none at speed 0 | A frame every 50 ms at least, `GameSpeed` ticks (20 while Space is held) |
| `decision` | Once a tick, after the drawing for the tick that is drawn, so that its messages show over the battle area | After every tick, and again after the drawing |
| The speed keys | F7 slower (to 0, which stops the battle), F8 faster (to 99), F9 back to 1 (`0x433AF0` to `0x433B53`) | F, G and R, by steps of 1, 2, 4, 6 and so on to 300, and Space |

### 1.22's frame
1.22 is driven by a multimedia timer, `timeSetEvent(88 × wPeriodMin, wPeriodMin, 0x4329B0, 0, TIME_PERIODIC)` (`0x432960`); with the usual minimum period of 1 ms, every 88 ms. The callback counts the timer's ticks (`0x483B50`) and posts `WM_USER` once; the window procedure handles it (`0x433C61`) by drawing a frame (`0x434570`) and clearing the count.

A frame of the battle:

1. If the speed (`0x489FA8`) is not 0, `chara_cont` and `decision` run (speed + ticks counted − 1) times, then `chara_cont` once more with `0x48A1EC` set, which marks the tick that is drawn: on that tick only, the view scrolls (`0x430150`) and returning planes adjust their approach to their moving carrier (`0x417530`). At speed 0 no `chara_cont` runs, but the rest of the frame does.
2. `0x4312A0`, the drawing of the battle area (`draw_cmbt_area`, `0x40DB50`), `0x430B40`.
3. `decision` (`0x41C3C0`).
4. Unless the game has ended, the orders (`0x430390`): `cpu_strtgy` (`0x404E30`) when the computer plays, then the player's.

The speed starts at 1 (`0x42A148`), so two ticks run in a frame of 88 ms.

## Not compared yet
- In the battle, as above: shooting and hits, the fires, the emergency moves.
- The input (`0x4312A0` and the window procedure from `0x433000`), the unit information and its menus (`0x430B40`), and the drawing (`0x40DB50`), beyond what is listed above.
