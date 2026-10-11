# Differences from NSPW 1.22
The port's battle is NSPW NET's: its movement, combat, supply and steering (`chara_cont` and the functions it calls) come from NSPW NET's source. A single-player game should behave exactly as NSPW 1.22 (`Original/Nspw122/NSPW_122.exe`). This document lists every difference found between the two, with the address in 1.22 where it shows, and whether the single-player game now follows 1.22 (`IsSingleplayer`).

The computer player itself (`cpu_strtgy`, `set_cpu_route`, `decision`) runs 1.22's own code in the emulator ([ComputerPlayer.md](ComputerPlayer.md)), so it has no difference of its own. The differences are in the battle that the computer plays in.

## Followed in single-player
| Difference | 1.22 | NSPW NET | Where in the port |
| --- | --- | --- | --- |
| Supply away from a naval base | A unit supplies (`spry`) wherever it is (`0x4010BB`): nothing checks for a naval base | Supply stops at once unless one of the side's naval bases is within ±280 | `UpdateSupply` |
| Ships outside the map | Only the player's ships are brought back (`0x416B35`, the end of `set_pos_of_emrgncy_SHIP` at `0x415F40`, which skips the units of `cpu_side`); submarines never (`0x401966`: kind 4 does nothing) | Every ship, and every submarine, gets an emergency move back to 40 inside the edge (`em_of_out_of_map`) | `SetShipEmergencyDestination`, `UpdateUnits` (submarines) |
| Computer's ships blocked by land ahead | Replanned (`set_cpu_route(m, 1.0)`) when the point 80 ahead toward its path is land (`0x40229D`) | Steering stops the ship | `CheckCourseAhead` |
| Computer's ships slowed by a ship ahead | Replanned every 100 ticks below a fifth of its top speed (`0x4020DB`) | Steering stops the ship | `CheckCourseAhead` |
| Computer's units about to step on land | Every 500 ticks (`cc_count%500`), a computer's unit with no leader whose next step (its speed along its heading) is land is replanned (`0x402D2B`), planes included | Nothing | `MoveUnit` |
| Move orders of the computer | Clear the unit's path (`if( new_pp[e].cls || unit[m].used==cpu_side )`), and a ship's route is planned by `set_cpu_route` | The same condition is a comment; routes are planned by `set_cpu_root2` | `ApplyOrders` |
| Orders | Given and applied in the frame they are made | Exchanged by turn over the network | `IssueSingleplayerOrders` |
| Time | `rest_time` counts down every tick | Counted by the host | `UpdateBattle` |
| Victory | `decision` | NSPW NET's conditions | `CheckResult` |
| Unit numbers | 20 ships and 30 planes a side | 40 and 50 | `SHIP_LIMIT`, `PLANE_LIMIT` |

The second and fifth rows are why the computer's fleets could not renew: in scenario 304 the American naval base (unit 21) is at x 10600, outside the map. A fleet that has lost its carrier, its ammunition or its fuel returns there (`cpu_strtgy` step 9); there it supplies, which gives back its HP, ammunition and fuel, and step 12 puts a new carrier at its supplying flagship. In the port, the fleet was turned back at the edge, and could not supply away from a base within 280.

How the off-map rule was found: the port's `em_of_out_of_map` sends a ship to 9560 or 7160 (`MAP_RIGHT-40`, `MAP_TOP-40`). In 1.22 those values are immediate operands (`mov dword ptr [esi+0x491b6c], 0x40c2ac00`, the high half of 9560.0) at `0x416B5C` to `0x416C8B`, after `cmp word ptr [esi+0x4916a0], cpu_side` / `je` at `0x416B3C`, so a search of the data for the doubles does not find them.

## Found, not followed yet
| Difference | 1.22 | Port |
| --- | --- | --- |
| `set_pos_of_emrgncy_SHIP` (`0x415F40`) | Its evasions are laid out differently; it sets `em_flg[1]` before the off-map rule (`0x416B2F`) | `SetShipEmergencyDestination`, NSPW NET's, to compare |
| Reloading and rearming (`0x403043` to `0x4030B6`) | `arm[3]` counts down, a submarine's only while it is stopped; when it reaches 0, a unit being serviced (weapon 17 or 18) gets 100 fuel and, a fighter, bullets (11) and 50 rounds; any other is left unarmed (17), with no ammunition, to be armed | To compare |

## Not compared yet
Everything else in 1.22's `chara_cont` (`0x401000`) and the 38 functions it reaches: shooting, hits, detection, planes taking off and landing, and the emergency moves of each kind. They are to be compared with the port function by function; each difference found goes in the tables above.
