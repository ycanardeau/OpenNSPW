# Plan: the x87's 80-bit intermediates in single-player

A single-player game follows 1.22 to the last bit in every trace recorded so far but one (s6/303).
There, a ship's distance to its formation leader differs in its last bit
(1.22 6439.518615550071, the port 6439.518615550072), which moves its path point by 2e-12; the ship sits on the point, so
the last bit decides which way it turns, and the battles part.

The cause is the x87 FPU. 1.22 computes `dx / cos(angle)` with `fcos` and `fdivp`: `fcos` leaves its result in an
80-bit register, with a 64-bit mantissa, and the division takes it from there, rounding only its own result to 53 bits.
The port computes in doubles: `cos` is rounded to 53 bits before the division. Rounding twice instead of once changes the
last bit now and then. A correctly rounded `cos` alone does not help (tried: s6/303 is unchanged).

## What 1.22 does

- **Precision control.** 1.22's C runtime sets it to 53 bits at startup (`_controlfp(_PC_53, _MCW_PC)`, called from
  `0x436083`, [Conversion.md](Conversion.md)), and the game's own code never loads a control word (no `fldcw` in
  `0x401000` to `0x436000`). So `fadd`, `fsub`, `fmul` and `fdiv` round their results to 53 bits, as doubles do; only
  their exponent range is wider, which the battle's values never reach. What is left to check on Windows is that nothing
  changes it during the battle, such as DirectDraw (step 1).
- **Transcendentals.** `fsin`, `fcos` and `fpatan` are not affected by precision control: they leave a 64-bit mantissa
  (Intel documents an error of at most 1 unit in the last of those bits). That value is then rounded to 53 bits only
  when it is stored to memory (`fst`/`fstp qword`), or when an arithmetic instruction takes it and rounds its own result.
- 1.22 has no `fsqrt`, `fptan` or `fsincos`.

So the port differs only where 1.22 uses a sine, a cosine or an arc tangent before storing it.

## The sites

1.22 has 366 transcendental instructions (162 `fcos`, 105 `fsin`, 99 `fpatan`). 123 are in the functions the emulator
runs (57 translated subroutines: `cpu_strtgy`, `set_cpu_route`, `set_cpu_route2`, the scenarios), 243 in the functions
the port runs natively. Of these 243, 232 use the result at 80 bits: 188 multiply by it (the `cos(a)*d` of every move),
39 divide by it (the distance, `dx/cos(a)`), the others copy, exchange or compare it; 11 store it at once, and need
nothing.

| Function (1.22) | Port | Sites at 80 bits |
| --- | --- | --- |
| `fire_now` (`0x40FE10`), every weapon's aim | `FireWeapons` and the functions it calls (etc1.cs) | 85 (+6 stored) |
| `cont_fire` (`0x418C30`), the fires' flight | `UpdateFires` and its functions (etc3.cs) | 24 |
| `set_pos_of_emrgncy_SHIP` (`0x415F40`), evasions | `SetShipEmergencyDestination`, `EvadeShip`, `EvadeTorpedo`, `EvadePlane`, `HuntSubmarineAs122` | 20 |
| `chara_cont` (`0x401000`), the moves | `UpdateBattle`, `MoveUnit`, `SteerUnit`, `UpdateParkedPlane` | 17 |
| `cont_pos_of_take_down` (`0x417530`), approaches | `UpdateLanding` | 10 (+3) |
| `draw_cmbt_area` (`0x40DB50`) and the effects' drawing (`0x40C7A0`, `0x40CDD0`), what the player sees | `DrawBattleArea`, `UpdateUpperEffects`, `UpdateLowerEffects` | 19 |
| `set_pos_of_attack_FT` (`0x414F80`) | `SetFighterAttackDestination` | 10 |
| `0x415A10` (attackers' attack) | `SetAttackerAttackDestination` | 9 |
| `set_pos_of_dynmc` (`0x4149F0`), formations | `SetDynamicDestination` | 8 |
| `0x418230` (detection) | `Detect` and its helpers | 8 |
| `0x415430` (attackers' emergency moves) | `SetAttackerEmergencyDestination` | 6 |
| `set_frmtn_of_ships` (`0x417080`) | `SetShipFormation` | 3 |
| `set_pos_of_take_down` (`0x4172C0`) | `SetLandingDestination` | 3 (+2) |
| `0x415E20`, `0x417EA0`, `cont_unit_effect` (`0x41AEB0`) | | 2 each |
| the sound's position (`0x4320C0`) | `PlaySoundEffect` | 4, not part of the battle |

On the port's side, these are its calls of `CosDegrees` and `SinDegrees` (182 in the battle files), `Direction` (34),
`Distance` (35) and `atan2` (3). Most follow a few patterns: `pos + CosDegrees(a)*d`, `Distance(dx, dy)` (an `fpatan`,
then an `fcos` and an `fdiv`), and `Direction(dx, dy)` (an `fpatan`, then a multiplication by 57.2957795131).

The full list, one site per line, is produced by a scan of the disassembly (`fsin`, `fcos` and `fpatan`, and the first
instruction that uses each result).

## The work

1. **Confirm the control word on Windows** (2 hours). The recorder reads the FPU's control word (`CONTEXT.FloatSave`)
   at a breakpoint in the battle, and checks that it stays `0x27F` (53 bits, round to nearest) for the whole battle,
   after DirectDraw has started.
2. **An 80-bit number** (1 to 2 days). A value with a 64-bit mantissa (`Extended`), with the x87's rounding: to 64 bits
   for the transcendentals' results, to 53 for `fadd`, `fsub`, `fmul` and `fdiv` under precision control 53, and to 53
   on a store. Unit tests against the real x87, run natively through a small thunk of machine code on Windows x86.
3. **The x87's `fsin`, `fcos` and `fpatan`** (2 to 3 days, the risky part). A model of what the hardware returns to 64
   bits: the argument reduced with the x87's 66-bit π, a kernel in higher precision, rounded to 64 bits. Tested
   against the real instructions on millions of arguments of the ranges the battle uses; where the model and the
   hardware disagree (the hardware is not always correctly rounded), find the rule or keep a table.
4. **The emulator's FPU** (1 to 2 days). Enzan's FPU keeps doubles (`Register64`); it keeps `Extended` instead, with the
   rounding of step 2, for the 123 sites in the emulated code, without changing them one by one. Its tests, and every
   trace, again.
5. **The ported battle code** (4 to 6 days). For each of the 232 sites, the port's expression computes what stays in a
   register as `Extended`, from 1.22's code: which values are kept in registers and which are stored. The patterns
   above make most of it mechanical (`Distance`, `Direction`, `pos + cos*d`); `fire_now` alone has 85 sites. A battle
   rule (`BattleRules`, single-player only): network games keep `nspw_math`, NSPW NET's C++ build, as now.
6. **Verify** (2 to 3 days, mostly waiting). Every trace replayed (about 300 traces now, 8 to 10 hours a pass), s6/303 to
   follow 1.22 to its end, nothing that followed to differ; then fresh batches until two in a row are clean.

About 2 to 3 weeks in all, most of it steps 3 and 5. Step 3 decides whether the rest is worth it: if the model of the
x87's transcendentals cannot match the hardware on the battle's arguments, the port can only come closer, not match.

## The processor
`fsin`, `fcos` and `fpatan` are not correctly rounded, and Intel's and AMD's processors are known to differ in their last
bits. So 1.22 itself can play differently on another processor, and the traces are 1.22's on the processor that recorded
them. Step 3 can match one processor's results, the recorder's, either by a model in software, which runs anywhere, or by
calling the x87's own instructions through a small native thunk, which matches the processor it runs on without a model
but runs only on x86 and x64.

## Cheaper ways
- **Only `Distance` and `Direction`** (a few days): the patterns of the port's own helpers, where s6/303's difference is,
  and through which most divisions by a cosine go. The port then comes closer to 1.22, without matching it everywhere.

## Without it

The difference shows only where a unit's decision rests on a last bit: in 1 trace of 316 so far. The battle then
goes on as plausibly as 1.22's, but not as 1.22's.
