# Converting NSPW 1.22
The aim is a complete conversion of NSPW 1.22 (`Original/Nspw122/NSPW_122.exe`) to C#:

- **Exact**: a single-player game behaves as 1.22 does.
- **Maintainable**: the conversion is C# that can be read and changed, not machine code. Each part is tested against 1.22's own code, emulated.
- **Documented**: what 1.22 does ([ComputerPlayer.md](ComputerPlayer.md)), and where the port differs ([Differences.md](Differences.md)).

The behavior lives in three forms: 1.22's machine code, the reference; its translation, run on the emulator (the [Disassembler](../Disassembler) turns each function into `Sub<address>.Call`, run on [Enzan](../Enzan)); and the C# conversion. The translation behaves as the machine code does only as far as the Disassembler and Enzan are right.

## Discrepancies between the emulator and the machine code
### Found and fixed
| Discrepancy | How it showed | Fix |
| --- | --- | --- |
| 8-bit displacements were not sign-extended (`[eax-0x1C]` read as `[eax+0xE4]`) | Wrong fields in 221 places of the translation | Enzan sign-extends them |
| `fsin`, `fcos` and `fpatan` gave the host's results | Results that could differ between platforms | The emulator uses the port's `sin`, `cos` and `atan2` |

### Known
- **Jump tables guarded by a register.** The Disassembler sizes a jump table from the last `cmp reg, imm`, even with a `cmp reg, reg` after it: a table of 17 cases was translated as 3. A register compare should clear it, and the table should be read while its entries are in the function.
- **A function's length past its end.** When a function's length runs into its jump table, the table is translated as code. The lengths must end before the tables.
- **Missing instruction forms.** The Disassembler stops at the first form it does not know, with its address (for example 16-bit `inc`/`dec`, `imm8` forms of `imul`, `test rm8, r8`, `jns rel32`, some x87 forms). Each is added to the Disassembler and Enzan, with a test of Enzan.
- **x87 precision.** Enzan keeps the FPU stack as `double`s, where x87 registers are 80-bit. 1.22 sets the precision to 53 bits at startup: `_controlfp(_PC_53, _MCW_PC)`, called from `0x436083` (the call is at `0x4363DE`). So addition, subtraction, multiplication, division and square roots round as `double`s do. Only the wider exponent range of x87 registers differs, for intermediate overflow and underflow, which the game's values do not reach.
- **`fsin`, `fcos` and `fpatan`.** Real x87 results differ from the port's functions in the last bits, and differ between processors. So there is no single machine result to match: the port's functions are the reference.
- **Natives.**
  - `rand` (`0x436056`) is the port's `rand()`, so that the whole game draws from one sequence. It is the same generator as 1.22's (`* 214013 + 2531011`, seed at `0x442250`).
  - `__ftol` (`0x4360C4`) truncates, as the CRT does.
- **Imports.** GDI, DirectDraw, DirectSound and the timer are stand-ins, and they define what 1.22 sees. A wrong stand-in is a bug of the port, not of the emulator.
- **The copied state.** A global that the emulated code writes but that is not copied back, or reads but that is not copied in, gives wrong results without failing.
  - Every global that the translated functions use must be in `SharedGlobals`, or stay in the emulator's memory only.
  - Check this whenever functions are added.
  - `gas` is `int` in 1.22 and `double` in NSPW NET, so it is converted.

### Finding more
- **Enzan's unit tests**, per instruction and flag.
- **Differential tests against an emulator that runs the machine code itself**, such as Unicorn:
  - Run a function from the same image, registers and state, once in Unicorn and once translated on Enzan.
  - Compare the memory afterwards.
  - This finds translation and flag errors. It does not check `fsin` and the like, which Unicorn approximates too.
- **Recording 1.22 on Windows**, the only ground truth.
  - Run `NSPW_122.exe` under a debugger script. Every frame, dump the units (`0x4916A0`, 256 × `0x598` bytes), the globals and the `rand` seed, with the frame's timer ticks (`0x483B50`) and input.
  - Replay the input and ticks on the emulated game, and compare.

## Known bugs
| Bug | Cause |
| --- | --- |
| The fighters captured in scenario 303 take off one at a time and cannot change base | NSPW NET's packing of the selection keeps only units with the player's side's numbers ([Differences.md](Differences.md#the-frame-the-input-and-the-drawing)) |
| Those fighters are drawn as American | 1.22 draws them from Japan's sheet (`0x40E070`: `sinario==303`, `used==2`, `kind==7`, `type==1`) |
| The computer's fleets cannot renew in scenario 304, and its ships stop at land until their next order | NSPW NET's battle ([Differences.md](Differences.md#the-battle)) |
| シナリオ選択へ and リジューム do nothing in single-player | They wait for the rival (`RivalMode`), and the system orders go back to NSPW NET's setting screen, which single-player does not have |
