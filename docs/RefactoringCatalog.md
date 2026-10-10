# Refactoring catalog
The names and types that [Refactoring.md](Refactoring.md) proposes for the exact port. Each renamed member keeps its C++ name in `[Original]`.

The names come from the original's comments and the C++ code. Names marked **(check)** are not certain yet: they are decided when the code that uses them is refactored, and the neutral name (`Info3`) is kept until then.

This catalog will also be the input of the `rename` tool, as a file.

## Glossary
The original's abbreviations, as used in names of globals, fields, locals and functions.

| Abbreviation | Meaning | | Abbreviation | Meaning |
| --- | --- | --- | --- | --- |
| `a_` | Change of (`a_spd_add`), or "and" | | `kumo` | Cloud (雲) |
| `arm` | Armament, weapon | | `lf`, `ri`, `md` | Left, right, middle |
| `bf_` | Buffered: received, applied at the next turn | | `ltl_ldr` | Temporary leader of a group (一時的指揮機) |
| `cc` | `chara_cont`, the simulation tick | | `m`, `n`, `i`, `f` | Unit, unit, index, fire or effect number |
| `chk` | Check, checksum | | `my_rnd` | The random table shared by both peers |
| `cls` | Clear | | `no` | Number |
| `cmbt` | Combat, the battle screen | | `os` | Off-screen surface (`lpDDS_OS`) |
| `cnct` | Connected (network) game | | `pp` | Path point, a waypoint (移動目的地) |
| `cnfg` | Configuration | | `pt` | Point, points |
| `crsr` | Cursor | | `rein` | Reinforcements |
| `ctgry` | Category | | `rnd` | Random |
| `dmg` | Damage | | `rtn` | Return |
| `drctn` | Direction, in degrees | | `rvrs` | Reversal (of the supply rates) |
| `dsp` | Display | | `scrn` | Screen |
| `dstc` | Distance | | `sinario` | Scenario |
| `dstn` | Destination | | `slct` | Select, selection |
| `dynmc` | Dynamic | | `snd` | Sound |
| `em` | Emergency | | `spd` | Speed |
| `found`, `find_out` | Detected by the other side, detect | | `spry` | Supply and repair (補給と修理) |
| `frmtn` | Formation | | `sprt` | Sprite |
| `gas` | Fuel | | `sw` | Switch, flag |
| `gr_` | Ground (world) coordinates | | `trgt` | Target |
| `hp` | Hit points | | `ttl` | Title |
| `indx` | Index | | `wrk` | Work, a temporary |
| `info` | Slots whose meaning depends on the kind | | `you_` | The local player |

Unit kinds and weapons: `BB` battleship, `CA` cruiser, `DD` destroyer, `SS` submarine, `CV` carrier, `CVL` light carrier, `FT` fighter, `AT` attacker (carrier bomber), `BM` strategic bomber, `TR` transport, `AP` air base, `SP` naval base (軍港), `CT` city, `MN` mine, `GF` ground fortification; `BLT` bullet, `GUN` gun against ground targets, `SHL` anti-aircraft shell, `VTH` proximity-fuzed shell, `TPD` torpedo, `BOM` bomb, `ASB` anti-submarine bomb, `RAS` rapid anti-aircraft shell.

## Terms
Each word means one thing in the new names. The original uses some words for several things (マップ is the terrain, the minimap and the coordinate space), so names follow these terms, not the C++ names.

| Term | Means | Examples |
| --- | --- | --- |
| **World** | The simulation's coordinate space: `double` coordinates from `WorldLeft` to `WorldRight` (x) and from `WorldBottom` to `WorldTop` (y, growing upward). | `WorldPosition`, `WorldVector`, `ReturnIntoWorld` |
| **Map** | The terrain: the tile grid, which terrain is in use, the map files and the map editor. Never the coordinate space or the minimap. | `MapTiles`, `CurrentMap`, `LoadUserMap`, `IsEditingMap` |
| **Minimap** | The overview of the whole world in the side panel, drawn on the `MAP_BASE` sprite. | `DrawMinimap`, `MakeMinimap` |
| **Battle area** | The 768×768 part of the screen that shows the world. Its top-left corner is at `CameraPosition`, in world coordinates. | `BattleAreaWidth`, `DrawBattleArea`, `ScrollBattleArea` |
| **Screen** | The 1024×768 window, in integer pixels (`Point`, `Rect`). | `ScreenWidth`, `CursorPosition` |
| **Tick** | One step of the simulation: one call of `UpdateBattle` (`chara_cont`). | `Tick` (`cc_count`) |
| **Frame** | One call of `UpdateFrame`, which runs `GameSpeed` ticks and draws once. | `FrameCount` |
| **Turn** | The `TurnLength` ticks between two exchanges of orders (see [Synchronization.md](../Original/docs/NSPW_NET/Synchronization.md)). | `TurnLength`, `SyncTick1` |

## Value types
Each has the layout of the primitives it replaces (see [Refactoring.md](Refactoring.md#value-types)).

| Type | Layout | Operations |
| --- | --- | --- |
| `WorldPosition` | `double X, Y` | `WorldPosition ± WorldVector`, `WorldPosition - WorldPosition` → `WorldVector`, `==`. `ToPoint()` truncates with `(int)`, as the C++ casts. |
| `WorldVector` | `double X, Y` | `* double`, `Length` (the original's `atan2` and `cos` distance, exactly). Not stored in the state. |
| `Angle` | `double Degrees` | `Sin()`, `Cos()` as `sin(Degrees*a_PI)`, `cos(Degrees*a_PI)`. `FromVector(dx, dy)` as `atan2(dy,dx)*RAD_to`. No normalization that the C++ does not do. |
| `Point` | `int X, Y` | Replaces `POINT`. |
| `Rect` | `int Left, Top, Right, Bottom` | Replaces `RECT`. `Contains` only where a `pt_in_rect` variant matches it exactly. |
| `Bool32` | `int Value` | `implicit operator bool` (`Value != 0`), and from `bool` (1 or 0). Comparisons with 1 stay as `Value==1`. |
| `Bool8` | `byte Value` | The same, for a `BYTE` used as a boolean (`you_are_host`, `map_edit`, `decision_sw`, ...). |
| `UnitId` | `int Value` | `None` (0). Exposed by accessors over `short`, `int` and `byte` storage. `Units[id]` indexer. |
| `FireId`, `EffectId` | `int Value` | `None` (0), as returned by `seek_fire_no` and `seek_effect_no`. |

## Enums
The underlying type is that of the field that stores the value. Members are spelled out, with the original constant in `[Original]`.

### `Side : short`
`UNIT.used`, `EFFECT.used`, `your_side`, `host_side` (`int`, converted), `FIRE.info[8]` (`int`, converted).

| Original | Member | Value |
| --- | --- | --- |
| | `None` | 0 (an unused slot) |
| `JPN` | `Japan` | 1 |
| `USA` | `UnitedStates` | 2 |

### `UnitCategory : int`
`UNIT.ctgry`: `SHIP` → `Ship` (1), `PLANE` → `Plane` (2), `BASE` → `Base` (3), and `None` (0).

### `UnitKind : int`
`UNIT.kind`, `put_kind` (`byte`, converted).

| Original | Member | | Original | Member |
| --- | --- | --- | --- | --- |
| `BB1` (1) | `Battleship` | | `TR1` (10) | `Transport` |
| `CA1` (2) | `Cruiser` | | `AP` (11) | `AirBase` |
| `DD1` (3) | `Destroyer` | | `SP` (12) | `NavalBase` |
| `SS1` (4) | `Submarine` | | `CT1` (13) | `City` |
| `CV1` (5) | `Carrier` | | `MN1` (14) | `Mine` |
| `CVL1` (6) | `LightCarrier` | | `GF1` (15) | `InfantryBase` (歩兵基地) |
| `FT1` (7) | `Fighter` | | `GF2` (16) | `Pillboxes` (トーチカ群) |
| `AT1` (8) | `Attacker` | | `GF3` (17) | `Fortress` (要塞) |
| `BM1` (9) | `Bomber` | | | `None` (0) |

The code compares ranges (`kind>=AP && kind<=GF3`, `kind>=GF1`), so the order is part of the type. The per-kind tables (`*_HP`, `*_SIGHT`) stay constants until the functions that read them are refactored.

### `UnitState : int`
`info[0]` of planes: `FLYING` → `Flying` (1), `PARKING` → `Parked` (2).

### `UnitMode : int`
`info[5]`. The code writes only `MOVE` and `RETURN` to it (or a menu entry that is one of them), and compares it with `<=SLOW`.

| Original | Member | Value |
| --- | --- | --- |
| | `None` | 0 |
| `MOVE` | `Move` | 1 |
| `SLOW` | `Slow` | 2 |
| `RETURN` | `Return` | 4 |

### `CombatMenuItem : short`
`cmbt_menu_slctd`, `NEW_MENU.menu`, `_DP_NEW_MENU.menu` (`byte`, converted). The original keeps these in the same `#define` group as the modes, but they are three different things: modes, an action, and the weapon an attacker or bomber is armed with.

| Original | Member | Value | Means |
| --- | --- | --- | --- |
| | `None` | 0 | |
| `MOVE` | `Move` | 1 | Sets `UnitMode.Move` |
| `SLOW` | `Slow` | 2 | Sets `UnitMode.Slow` |
| `RETURN` | `Return` | 4 | Sets `UnitMode.Return` |
| `SPRY` | `Supply` | 5 | An action |
| `RDY_TPD` | `ArmWithTorpedo` | 15 | Sets `arm[0]` to `FireKind.Torpedo` (15) |
| `RDY_BOM` | `ArmWithBomb` | 16 | Sets `arm[0]` to `FireKind.Bomb` (16) |
| `NOTHING` | `Disarm` | 17 | Sets `arm[0]` to `FireKind.Unarmed` (17) |

The arming entries have the values of the `FireKind`s they set, and the original relies on it: `input.cpp` compares `arm[0]` with `RDY_TPD`, `RDY_BOM` and `NOTHING`. The conversions (`ToMode()`, `ToWeapon()`) are casts, and keep those values.

### `FireKind : int`
`FIRE.kind`, `UNIT.arm[0]`, `UNIT.arm2[]`.

| Original | Member | | Original | Member |
| --- | --- | --- | --- | --- |
| `BLT` (11) | `Bullet` | | `ASB` (19) | `AntiSubmarineBomb` |
| `GUN` (12) | `Gun` | | `RAS` (20) | `RapidAntiAircraftShell` |
| `SHL` (13) | `AntiAircraftShell` | | `SP_GUN` (21) | `NavalBaseGun` |
| `VTH` (14) | `ProximityFuzedShell` | | `TR_SP` (30) | `CargoNavalBase` |
| `TPD` (15) | `Torpedo` | | `TR_AP` (31) | `CargoAirBase` |
| `BOM` (16) | `Bomb` | | `TR_GF1` (32) | `CargoInfantryBase` |
| `NTG` (17) | `Unarmed` (no weapon loaded) | | `TR_GF2` (33) | `CargoPillboxes` |
| `TUN` (18) | `Maintenance` (発進最低整備) | | `TR_GF3` (34) | `CargoFortress` |

### `EffectLayer : short`
`EFFECT.layer`: `None` (0, unused), `UPPER` → `Upper` (1), `LOWER` → `Lower` (2).

### `GameMode : short`
`mode`, `rival_mode`, `_DP_FLAG.rival_mode`.

| Original | Member | Value |
| --- | --- | --- |
| `DEMO` | `Title` | 1 |
| `CNCT_GAME_SETUP` | `Setup` | 3 |
| `CNCT_GAME_SETTING` | `GameSetting` | 4 |
| `CNCT_CNFG_SETTING` | `ConfigSetting` | 5 |
| `CMBT` | `Battle` | 10 |

### `GameResult : int`
`game_end`: `None` (0), `JPN_WIN` → `JapanWon` (1), `USA_WIN` → `UnitedStatesWon` (2), `JPN_LOST` → `JapanLost` (3), `USA_LOST` → `UnitedStatesLost` (4), `DRAW` → `Draw` (10).

### `MessageType : uint`
`dwType` of the network messages: `MY_NAME_IS` (1) to `RIVAL_VER` (501), spelled out (`DP_NEW_PP` → `MoveOrder`, `DP_NEW_SLCT_SHIP` → `SelectShipsOrder`, `DP_FLAG_1` → `SyncFlag`, `DP_CHAT_1` → `Chat`, ...). Each message struct writes its type as `uint`, unchanged.

`MSG_TST`, `MSG_EXIT_WAITING` and `MSG_END` are a different set, with overlapping values, and get their own enum (`AppMessage`).

### Other enums
| Enum | From | Notes |
| --- | --- | --- |
| `InputButtons : int`, `[Flags]` | `FRONT_BTN` ... `TOP_VIEW_BTN2` | `key_cndtn` |
| `SoundId : int` | `AA_BLT1` ... `CLICK2` | Used as an index into `lpDSB_` and `snd_`, with a cast. The sounds load from `WAV\<original name>.wav`, in this order. |
| `SpriteId : int` | `TTL_BACK` ... `MAP_BASE` | The entries of `Sprites`, regions of the one offscreen surface (`t3.bmp`). `Sprites` is a `SpriteArray`, an inline array of 25 that can also be indexed by `SpriteId`. |
| `KeyDirection : int` | `KEY_UP` ... `KEY_LFUP` | Numeric keypad layout (8 is up). |

## Constants
The `#define`s of `all_head.h` that are not enum members. Each keeps its value and type exactly, including the ones written as expressions (`CV1_HP` is `35-3`), and `a_PI` keeps its own value, which is not `Math.PI / 180`.

Per-kind tables become static classes, so that a value reads as table and kind (`HitPoints.Battleship`). They become lookups by `UnitKind` only where the original code does the lookup with a `switch`, in the same order.

### Screen and world
| Original | New |
| --- | --- |
| `SCRN_WIDTH`, `SCRN_HEIGHT` | `ScreenWidth`, `ScreenHeight` |
| `WIDTH`, `HEIGHT` | `WindowWidth`, `WindowHeight` |
| `CMBT_WIDTH`, `CMBT_HEIGHT` | `BattleAreaWidth`, `BattleAreaHeight` |
| `CMBT_REST` | `BattleAreaMargin` (check) |
| `MAP_TOP`, `MAP_BOTTOM`, `MAP_RIGHT`, `MAP_LEFT` | `WorldTop`, `WorldBottom`, `WorldRight`, `WorldLeft` |
| `SCRN_MAX_SPD`, `scrn_moving_add` | `MaxScrollSpeed`, `ScrollAcceleration` |
| `CAPTION`, `CLASS_NAME` | `WindowCaption`, `WindowClassName` |
| `PALT_RED`, `PALT_BUL` | `PaletteRed`, `PaletteBlue` |
| `MAX_SPRT`, `OS_MAX` | `SpriteCount`, `OffscreenSurfaceCount` (check) |

### Math
| Original | New |
| --- | --- |
| `PI` | `Pi` |
| `a_PI` | `DegreesToRadians` |
| `RAD_to` | `RadiansToDegrees` |

### Tables and unit numbers
| Original | New |
| --- | --- |
| `KUMO_MAX`, `FIRE_MAX`, `EFFECT_MAX` | `MaxClouds`, `MaxFires`, `MaxEffects` |
| `MAX_TF` | `MaxTaskForces` |
| `JPN_SHIP_START`, `JPN_SHIP_END` | `FirstJapanShip`, `LastJapanShip` |
| `USA_SHIP_START`, `USA_SHIP_END` | `FirstUnitedStatesShip`, `LastUnitedStatesShip` |
| `JPN_PLANE_START`, `JPN_PLANE_END` | `FirstJapanPlane`, `LastJapanPlane` |
| `USA_PLANE_START`, `USA_PLANE_END` | `FirstUnitedStatesPlane`, `LastUnitedStatesPlane` (also the highest unit number) |

### Units and weapons
| Original | New |
| --- | --- |
| `BB1_HP` ... `GF3_HP` | `HitPoints.Battleship` ... `HitPoints.Fortress` |
| `BB1_SIGHT` ... `GF3_SIGHT` | `Sight.Battleship` ... `Sight.Fortress` |
| `GUN_SZ`, `SHL_SZ`, `TPD_SZ`, `ASB_SZ`, `RAS_SZ` | `AmmoCost.Gun` ... `AmmoCost.RapidAntiAircraftShell` (弾薬の消費サイズ) |
| `BLT_DMG` ... `RAS_DMG` | `Damage.Bullet` ... `Damage.RapidAntiAircraftShell` |
| `TPD_SPD`, `AIR_TPD_SPD` | `TorpedoSpeed`, `AerialTorpedoSpeed` |
| `AIR_TPD_LOS_DSTC` | `AerialTorpedoRange` (check) |
| `RELOAD_TPD_DD`, `RELOAD_TPD_SS` | `DestroyerTorpedoReloadTime`, `SubmarineTorpedoReloadTime` |
| `TUNE_SPAN`, `RDY_SPAN` | `MaintenanceTime`, `ReadyTime` |
| `CMBT_SPD` | `BattleSpeed` |
| `FT_EYE` | `FighterEyesight` (check) |
| `ON_PP` | `WaypointReachedDistance` (check) |

### Network and input
| Original | New |
| --- | --- |
| `MAXPLAYERS`, `MAX_PLAYER_NAME` | `MaxPlayers`, `MaxPlayerNameLength` |
| `ADDRESSOVERRIDE_PORT`, `DOWORK_TIMESLICE` | `DefaultPort`, `DoWorkTimeSlice` |
| `CHAT_DSP_TIME` | `ChatDisplayTime` |
| `DIDEVICE_BUFFERSIZE` | `InputBufferSize` |
| `VER` | `Version` |

The compile-time switches (`SND_SW`, `DBG_MODE`, `CONN_DBG`, `LNGG_VER`, `NSPW_THE_NET`) keep their names, because they are also conditional compilation symbols. Win32 and DirectPlay constants (`WM_APP_UPDATE_STATS`, `TYPE_UNIT_MSG`) keep theirs with the stand-ins.

## Structs
The struct types are renamed (`UNIT` → `Unit`, `FIRE` → `Fire`, `EFFECT` → `Effect`, `KUMO` → `Cloud`, `SPRT` → `Sprite`), and keep `[Original]` with the C++ name for the layout tests.

### `Unit` (`UNIT`)
| Original | New | Type | Notes |
| --- | --- | --- | --- |
| `used` | `Side` | `Side` | `IsUsed` is `Side != Side.None`. |
| `x`, `y` | `Position` | `WorldPosition` | |
| `ctgry` | `Category` | `UnitCategory` | `IsShip`, `IsPlane`, `IsBase`. |
| `kind` | `Kind` | `UnitKind` | `IsCarrier`, `IsFacility`, ... with the original's comparisons. |
| `type` | `Variant` | `short` | 形式: Yamato class, anti-aircraft cruiser, land-based fighter, ... |
| `info` | `info` | `Array16<int>` | Accessors below. |
| `os_indx_y` | `SpriteRow` | `int` | Before `os_indx_x`, so not a `Point`. |
| `os_indx_x` | `SpriteColumn` | `int` | The facing. |
| `drctn` | `Direction` | `Angle` | |
| `drctn_add` | `TurnRate` | `double` | 変進角度 |
| `a_drctn_add` | `TurnRateChange` | `double` | |
| `spd` | `Speed` | `double` | |
| `spd_add` | `Acceleration` | `double` | |
| `a_spd_add` | `AccelerationChange` | `double` | |
| `min_spd`, `max_spd` | `MinSpeed`, `MaxSpeed` | `double` | |
| `stop` | `IsStopping` | `Bool32` | Keeps its direction and only slows down. |
| `spry` | `SupplyTime` | `int` | Supply and repair (補給と修理): not a flag but a timer, 0 when not supplied, counted up from 1 and back to 1 after each repair or refill. `IsSupplying` tests it against 0. |
| `mark` | `IsMarked` | `Bool32` | Selected (check). |
| `pp_x`, `pp_y` | `PathX`, `PathY` | `Array64<double>` | `Path[i]` is a view that reads and writes both as a `WorldPosition`. |
| `em_flg` | `EmergencyFlags` | `Array2<int>` | |
| `em_x`, `em_y` | `EmergencyDestination` | `WorldPosition` | |
| `to_ldr_drctn` | `DirectionToLeader` | `Angle` | Formation offset. |
| `to_ldr_dstc` | `DistanceToLeader` | `double` | |
| `is_ltl_ldr` | `IsGroupLeader` | `short` | |
| `ltl_ldr` | `GroupLeader` | `short`, accessor `UnitId` | |
| `no` | `FormationNumber` | `short` | 何番機 |
| `for_ltl_ldr` | `ForGroupLeader` | `short` | (check) |
| `for_form_spd` | `FormationSpeed` | `double` | Speed of a unit catching up with its formation. |
| `hp` | `hp` | `Array8<int>` | `Hp` (`[0]`), `MaxHp` (`[1]`). |
| `arm` | `arm` | `Array8<int>` | `Weapon` (`[0]`, `FireKind`), `Ammo` (`[1]`), `Target` (`[2]`, `UnitId`), `ReloadTime` (`[3]`, 再装填時間, also the maintenance time of planes), `MaxAmmo` (`[4]`, 全容量). |
| `arm2` | `SubWeapons` | `Array2<int>` | サブ武装 |
| `gas` | `gas` | `Array8<double>` | `Fuel` (`[0]`), `[1]` (check: maximum fuel). |
| `found` | `IsFound` | `Bool32` | Visible to the other side. |
| `tech` | `Skill` | `int` | 技量 |
| `rnd_250` ... `rnd_10` | `Random250` ... `Random10` | `Array2<short>` | Random numbers drawn when the unit is created, below 250 ... 10. |

`info[]` slots:

| Slot | Ships | Planes | Bases |
| --- | --- | --- | --- |
| 0 | | `PlaneState` (`UnitState`) | `BuildTime` (建設期間) |
| 1 | Carriers: `PlaneCount` (現在収容数, with the flight deck) | `Carrier` (`UnitId`, 所属の空母、及び、基地) | Air bases: `PlaneCount` |
| 2 | Carriers: `Capacity` (最大収容数) | `ParkingNumber` (格納庫の位置、番機番号) | Air bases: `Capacity` |
| 3 | | `Info3` (check: hangar movement, landing preparation, straight flight after take-off) | |
| 4 | Carriers: `PlanesToLaunch` (発進予定機数, 0 allows landing) | `Info4` (check) | |
| 5 | `Mode` (`UnitMode`) | `Mode` | `Mode` |
| 6 | Submarines: `IsSubmerged` (`Bool32`; the one comparison with 1 stays as `IsSubmerged.Value==1`). Transports: `LandingPoint.X` (揚陸座標) | | |
| 7 | Carriers: `LandingLock` (0 allowed, 1 not; also the deck side, check). Transports: `LandingPoint.Y` | | |
| 8 | Carriers: `LaunchLock` (0 allowed, 1 not) | | |
| 9 | | Fighters: `Info9` (制空出撃フラグ, and other uses, check) | |
| 10, 11 | `Info10`, `Info11` (check) | | |

`LandingPoint` is a `Point` view over `info[6]` and `info[7]`.

### `Fire` (`FIRE`)
| Original | New | Type | Notes |
| --- | --- | --- | --- |
| `used` | `Target` | `int`, accessor `UnitId` | 0 is a free slot; otherwise the target unit. `IsUsed`. |
| `kind` | `Kind` | `FireKind` | |
| `info[0]` | `Timer` | `int` | Mostly the ticks to the target (`dstc/spd`), check per kind. |
| `info[1]`, `info[2]` | `Info1`, `Info2` | `int` | Per kind (check). |
| `info[6]`, `info[7]` | `LandingPoint` | `Point` view | Copied from the transport's `info[6]` and `info[7]`. |
| `info[8]` | `Side` | `int`, accessor `Side` | The firing unit's side. |
| `no` | `SpriteNumber` | `int` | |
| `x`, `y` | `Position` | `WorldPosition` | |
| `drctn` | `Direction` | `Angle` | |
| `spd`, `spd_add`, `last_spd` | `Speed`, `Acceleration`, `FinalSpeed` | `double` | |
| `last_x`, `last_y` | `Destination` | `WorldPosition` | 最終目的地 |

### `Effect` (`EFFECT`)
| Original | New | Type | Notes |
| --- | --- | --- | --- |
| `used` | `Side` | `Side` | 自サイド |
| `layer` | `Layer` | `EffectLayer` | 0 is a free slot. |
| `kind` | `Kind` | `int` | An enum once its values are listed (check). |
| `no` | `SpriteNumber` | `int` | |
| `info[0]` | `Timer` | `int` | |
| `info[1]` | `AnimationFrame` | `int` | アニメーションパターン |
| `x`, `y` | `Position` | `WorldPosition` | |
| `x2`, `y2` | `EndPosition` | `WorldPosition` | Bullets and rapid anti-aircraft shells. |
| `found` | `IsVisible` | `Bool32` | |

### Other structs
| Original | New | Fields |
| --- | --- | --- |
| `KUMO` | `Cloud` | `used` → `Used` (`short`), `x`, `y` → `Position`, `kind` → `Kind` |
| `SPRT` | `Sprite` | `no` → `Frame` (the frame drawn), `x`, `y` → `X`, `Y` (where it is drawn on the screen), `cx`, `cy` → `CenterX`, `CenterY`, `wd`, `ht` → `Width`, `Height` (of a frame), `base_x`, `base_y` → `SheetX`, `SheetY` (where its frames start on the surface), `os_of_x` → `FramesPerRow` |
| `NEW_PP` | `MoveOrder` | `used` → `Unit` (`short`; check what it holds), `x`, `y` → `Destination`, `cls` → `ClearsPath` (`Bool32`) |
| `NEW_SLCT` | `SelectOrder` | `sw` → `IsSet`, `the_slct_unit` → `SelectedUnit`, `m` → `Unit`, `gr_x`, `gr_y` → `GroundPosition` |
| `NEW_MENU` | `MenuOrder` | `menu` → `Menu`, `the_slct_unit` → `SelectedUnit` |

The network messages keep their packed layout and get message names: `_DP_NEW_PP` → `MoveOrderMessage`, `_DP_NEW_PP_SHIP` → `MoveShipsOrderMessage`, `_DP_NEW_SLCT_LAND` → `SelectBaseOrderMessage`, `_DP_NEW_MENU` → `MenuOrderMessage`, `_DP_FLAG` → `SyncFlagMessage`, `_DP_DATA_1` → `NameMessage`, `_DP_DATA_20` → `ChatMessage`, `GENERICMSG` → `MessageHeader`. Their fields keep their widths (`byte` unit numbers, `short` coordinates).

## Globals
All 119 globals of `Layout.json`, grouped. The flags among them are `Bool32` (`int`) or `Bool8` (`byte`): `IsAppActive`, `IsFullscreen`, `IsEditingMap`, `IsHost`, `WasHost`, `CanOrder`, `HasOrdered`, `CanAdvance1`, `CanAdvance2`, `IsDecisionEnabled`, `IsTickOutOfSync`, `IsRandomOutOfSync`, `AreUnitsOutOfSync`, `HasSavedDesync`. Arrays indexed by player seem to use `[1]` for the local player and `[0]` for the rival (check per array; `[2]` is unused).

### Units, fires, effects, map
| Original | New | Notes |
| --- | --- | --- |
| `unit` | `Units` | |
| `max_unit` | `MaxUnitId` | |
| `the_slct_unit`, `old_the_slct_unit` | `SelectedUnit`, `PreviousSelectedUnit` | |
| `slct_unit`, `slct_unit_no` | `Selections`, `SelectionCount` | `Selections[player][unit]` is the unit's place in the selection, 0 if not selected. |
| `unit_info` | `UnitInfoPanel` | (check) |
| `fire`, `max_fire` | `Fires`, `MaxFireId` | |
| `effect` | `Effects` | |
| `kumo` | `Clouds` | |
| `sprt` | `Sprites` | |
| `cmbt_map` | `MapTiles` | |
| `cls_flg` | `ClearFlag` | (check) |
| `wrk_pp_x`, `wrk_pp_y` | `WorkPathX`, `WorkPathY` | |

### Battle
| Original | New | Notes |
| --- | --- | --- |
| `mode` | `Mode` | `GameMode` |
| `cc_count` | `Tick` | |
| `FrameCount` | `FrameCount` | |
| `rest_time` | `BattleTime` | Counts up once per turn (check). |
| `game_end` | `Result` | `GameResult` |
| `decision_point` | `DecisionPoints` | |
| `decision_sw` | `IsDecisionEnabled` | |
| `your_side` | `LocalSide` | `Side` |
| `game_speed` | `GameSpeed` | |
| `demo_time` | `TitleTime` | |
| `sinario` | `ScenarioNumber` | |
| `rein` | `Reinforcements` | |
| `arrival_cont` | `ArrivalControl` | (check) |
| `map_now` | `CurrentMap` | |
| `spry_pt`, `first_spry_pt` | `SupplyPoints`, `InitialSupplyPoints` | |
| `spry_rate` | `SupplyRates` | |
| `spry_no_cont`, `spry_trgt` | `SupplyCount`, `SupplyTarget` | (check) |
| `rvrs_time`, `rvrs_rule` | `SwapTime`, `SwapRule` | When the two sides' supply rates are swapped. |
| `auto_save_time`, `exist_auto_save` | `AutoSaveTime`, `HasAutoSave` | |

### Orders and synchronization
See [Synchronization.md](../Original/docs/NSPW_NET/Synchronization.md) for what these do.

| Original | New |
| --- | --- |
| `new_pp`, `new_slct`, `new_menu`, `game_system_menu` | `MoveOrders`, `SelectOrders`, `MenuOrders`, `SystemOrders` |
| `bf_new_pp`, `bf_new_slct`, `bf_new_menu`, `bf_game_system_menu` | `BufferedMoveOrders`, `BufferedSelectOrders`, `BufferedMenuOrders`, `BufferedSystemOrders` |
| `bf_slct_unit`, `bf_arrived_unit` | `BufferedSelections`, `BufferedArrivedUnits` |
| `you_can_order`, `you_ordered` | `CanOrder`, `HasOrdered` |
| `go_next_1`, `go_next_2` | `CanAdvance1`, `CanAdvance2` |
| `cnct_loop`, `cnct_loop_ct` | `TurnLength`, `TurnCounter` |
| `cnct_loop_pt1`, `cnct_loop_pt2` | `SyncTick1`, `SyncTick2` |
| `my_rnd_sheet`, `my_rnd_pt`, `cnct_game_rnd_sheed` | `SharedRandomTable`, `SharedRandomIndex`, `SharedRandomSeed` |
| `rnd_count` | `RandomCount` |
| `bf_cc_count`, `bf_rnd_count`, `bf_unit_chk` | `TickChecksums`, `RandomChecksums`, `UnitChecksums` |
| `ccc_out`, `rnd_out`, `unit_out` | `IsTickOutOfSync`, `IsRandomOutOfSync`, `AreUnitsOutOfSync` |
| `ccc_wait` | `TickWaits` |
| `first_r_error` | `HasSavedDesync` |

### Session
| Original | New |
| --- | --- |
| `cnct_game` | `IsNetworkGame` |
| `you_are_host`, `you_were_host`, `host_side` | `IsHost`, `WasHost`, `HostSide` |
| `join_game_start` | `JoinGameStart` |
| `rival_mode`, `rival_ver` | `RivalMode`, `RivalVersion` |
| `g_dwNumberOfActivePlayers`, `g_bHostPlayer` | `ActivePlayerCount`, `IsHostPlayer` |
| `g_strAppName`, `g_strLocalPlayerName`, `g_strRivalPlayerName` | `AppName`, `LocalPlayerName`, `RivalPlayerName` (byte arrays, with `string` accessors) |
| `g_strPreferredProvider`, `g_strRemoteHostname` | `PreferredProvider`, `RemoteHostName` |
| `my_chat`, `friend_chat` | `MyChat`, `RivalChat` |
| `my_chat_dsp_time`, `friend_chat_dsp_time` | `MyChatDisplayTime`, `RivalChatDisplayTime` |
| `input_chat_now` | `IsTypingChat` |

### Screen, input, editor
| Original | New |
| --- | --- |
| `cmbt_x`, `cmbt_y` | `CameraPosition` (`WorldPosition` over the two globals) |
| `scrn_moving_spd` | `ScrollSpeed` |
| `crsr_pt` | `CursorPosition` (`Point`) |
| `key_cndtn` | `Buttons` (`InputButtons`) |
| `lf_btn`, `ri_btn` | `LeftButton`, `RightButton` |
| `cmbt_menu_kind`, `cmbt_menu_slctd` | `CombatMenuKind`, `CombatMenuSelection` |
| `map_edit`, `put_trgt`, `put_kind`, `put_kind_sub` | `IsEditingMap`, `EditorTarget`, `EditorKind`, `EditorVariant` |
| `user_sinario_fn` | `UserScenarioFileName` |
| `snd_` | `NextSoundBuffers` |
| `appActive`, `fullscreen`, `scrn_mode`, `video_memory`, `g_bDeviceLost` | `IsAppActive`, `IsFullscreen`, `ScreenMode`, `VideoMemory`, `IsDeviceLost` |
| `dlg_answer` | `DialogAnswer` |
| `missed_pending`, `a_paint_speed` | `MissedPending`, `PaintSpeed` |
| `last_tick`, `last_tick2`, `tick_now`, `tick_diff` | `LastTime`, `LastTime2`, `Now`, `Elapsed` |
| `anti_air`, `reveal` | `ShowsAntiAir`, `RevealsAll` (debug, check) |
| `dbg_menu`, `dbg` | `DebugMenu`, `DebugValues` |

## Functions
Proposed names. Each is finalized when the function is refactored, after reading it whole. The Win32 callbacks (`MainWndProc`, the dialog procedures), `WinMain` and the DirectPlay and wave-file helpers (`DirectPlayMessageHandler`, `YWave*`, `LoadWave`) keep their names until the partial class is split.

| File | Original | New |
| --- | --- | --- |
| `chara_cont.cpp` | `chara_cont` | `UpdateBattle` |
| `cnct_game_cont.cpp` | `save_on_resume`, `load_on_resume` | `SaveResume`, `LoadResume` |
| | `cnct_decision` | `CheckResult` |
| | `set_unit_data` | `SetUnitData` |
| | `set_new_unit`, `set_new_unit_2`, `set_new_unit_plane` | `AddUnit`, `AddUnit2` (check), `AddPlane` |
| | `cnct_sinario_1` ... `cnct_sinario_999` | `SetUpScenario1` ... `SetUpScenario999` |
| | `get_sinario_data` | `LoadScenarioData` |
| | `make_map_cg` | `MakeTerrainSurface` (check) |
| | `cnct_game_init` | `InitializeGame` |
| `demo.cpp` | `demo_func` | `UpdateTitle` |
| | `go_cnct_game_setting` | `GoToGameSetting` |
| | `cnct_game_setting`, `cnct_game_setup` | `UpdateGameSetting`, `UpdateSetup` |
| `draw.cpp` | `draw` | `Draw` |
| `draw_cmbt_area.cpp` | `draw_cmbt_area` | `DrawBattleArea` |
| | `cont_upper_effect`, `cont_lower_effect` | `UpdateUpperEffects`, `UpdateLowerEffects` |
| | `draw_cloud` | `DrawClouds` |
| | `be_dstryd` | `DrawDestruction` |
| `etc1.cpp` | `hit_chk`, `draw_hit_area` | `CheckHit`, `DrawHitArea` |
| | `fire_now` | `FireWeapons` |
| | `set_pos_of_dynmc` | `SetDynamicDestination` |
| | `chk_another_unit` | `CheckOtherUnits` |
| | `set_pos_of_emrgncy_FT`, `set_pos_of_emrgncy_AT`, `set_pos_of_emrgncy_SHIP` | `SetFighterEmergencyDestination`, `SetAttackerEmergencyDestination`, `SetShipEmergencyDestination` |
| | `set_pos_of_attack_FT`, `set_pos_of_attack_AT`, `set_pos_of_attack_TR1` | `SetFighterAttackDestination`, `SetAttackerAttackDestination`, `SetTransportLandingDestination` |
| | `em_of_out_of_map` | `ReturnIntoWorld` |
| `etc2.cpp` | `new_unit_arrived` | `OnUnitArrived` |
| | `find_out`, `find_out_ss`, `find_out_size` | `Detect`, `DetectSubmarines`, `GetDetectionSize` |
| | `draw_line4`, `draw_line5` | `DrawLine4`, `DrawLine5` |
| | `pt_in_rect`, `pt_in_rect2`, `pt_in_rect3` | `PointInRect`, `PointInRect2`, `PointInRect3`, until their differences are named |
| | `same_rect` | `ClipRects` |
| | `seek_parking_no`, `plane_in_cv`, `set_pos_of_parking` | `FindParkingNumber`, `CountPlanesIn`, `SetParkingPosition` |
| | `cls_all_slct_unit`, `cls_all_slct_unit_p2` | `ClearSelection`, `ClearSelection2` (check) |
| | `seek_effect_no`, `seek_fire_no` | `FindFreeEffect`, `FindFreeFire` |
| | `rtn_damage_pt` | `GetDamagePoints` |
| | `drctn_for_8` | `ToEightDirections` |
| | `set_frmtn_of_ships` | `SetShipFormation` |
| | `set_pos_of_take_down`, `cont_pos_of_take_down` | `SetLandingDestination`, `UpdateLanding` |
| `etc3.cpp` | `make_my_rnd`, `my_rnd`, `rnd` | `MakeSharedRandomTable`, `SharedRandom`, `Random` |
| | `set_sprt_data` | `InitializeSprites` |
| | `cloud_in_start`, `cloud_cont` | `InitializeClouds`, `UpdateClouds` |
| | `cont_fire` | `UpdateFires` |
| | `set_new_ltl_ldr` | `AssignGroupLeader` |
| | `cont_unit_effect` | `UpdateUnitEffects` |
| | `edit_now` | `UpdateMapEditor` |
| `input.cpp` | `get_input`, `InitDInput` | `ReadInput`, `InitializeDirectInput` |
| | `cnct_game_input_cont`, `cnct_game_input_now` | `HandleInput`, `ApplyOrders` (check) |
| | `set_cpu_root2` | `SetCpuRoute2` |
| | `tac_map_scrl` | `ScrollBattleArea` |
| `unit_info_cont.cpp` | `unit_info_cont`, `cnct_unit_info_cont_now` | `UpdateUnitInfo`, `ApplyUnitInfoInput` (check) |
| | `spry_pt_per_unit` | `GetSupplyPointsPerUnit` |
| | `draw_map`, `make_map` | `DrawMinimap`, `MakeMinimap` |
| `win_main.cpp` | `updateFrame` | `UpdateFrame` |
| `win_proc.cpp` | `restoreAll` | `RestoreSurfaces` |
| | `load_user_map`, `save_user_map`, `load_it2`, `load_it3` | `LoadUserMap`, `SaveUserMap`, `LoadScenarioFile2`, `LoadScenarioFile3` (check) |
| | `init_apl_reg`, `EndApp`, `my_dlg_wait` | `InitializeRegistry`, `EndApp`, `WaitForDialog` |
| `Audio.cpp` | `InitDSound`, `InitDMusic`, `play_snd`, `SoundPlayEffect` | `InitializeDirectSound`, `InitializeDirectMusic`, `PlaySound`, `PlaySoundEffect` |
