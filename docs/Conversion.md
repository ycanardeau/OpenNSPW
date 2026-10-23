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
| `push imm8` zero-extended its byte | Found reading Enzan: `6A ib` sign-extends. No translated function pushes a byte of 0x80 or more yet | Enzan sign-extends it |
| `sbb` lost the borrow when the subtrahend plus the carry did not fit in 32 bits | Found reading Enzan. The three `sbb reg, reg` of the translations only use the value, which was right | Enzan subtracts the carry separately |
| An unordered `fcom` (with a NaN) left C3, C2 and C0 as they were | Found reading Enzan: x87 sets all three | Enzan sets them |
| `__ftol` saturated a NaN or a value out of range, as a conversion in C# does | Found reading the native: the CRT's `fistp` stores the integer indefinite, `0x8000000000000000` | The native returns it |
| The single-player game gave the emulated code NSPW NET's map | `set_cpu_route` planned on NSPW NET's `japan_off.dat`, which has an island that 1.22's does not ([Differences.md](Differences.md)) | The single-player game reads 1.22's maps |

### Checked
A static audit of the 57 translated functions against the machine code, on 2026-10-19. It reads the machine code; it does not run 1.22.

- **Decoding.** Every translated instruction was decoded again with Iced, an independent decoder, and compared with the Disassembler's: the instruction boundaries of each function, the mnemonic, the registers, the immediates and displacements, the size of each memory operand, the direction of each operation (register or memory destination), the operand order of the x87 forms (`fdivr`, `fdivp`, `fpatan` and the others), and each constant of `.rdata` that the translation inlines, against the bytes at its address. All 29,610 agree. No branch leaves its function, every branch target is translated, and every function ends with `ret` or `jmp` before the data that follows it.
- **Jump tables.** All 11 have as many cases as their guard allows, including `set_unit_data`'s 17, guarded by `cmp ecx, edi`, and their targets are the table's bytes.
- **Calls.** Every direct call goes to a translated function or a native (`rand`, `__ftol`, `load_it2`); the indirect ones are imports and the surface's `GetDC` and `ReleaseDC`, which have stand-ins. An import without one throws.
- **The copied state.** Every global that the translations read or write is in `SharedGlobals`, in the units or the fleets, except `0x463AA4` (the window) and `0x463B20` (whether 1.22 is full screen), which only decide whether `set_cpu_route` calls `SetWindowTextA`. `0x463B20` stays 0, a window, so it does each time round its loop, and the stand-in, which shows nothing, ends a route that fills its path there ([KnownIssues.md](KnownIssues.md#single-player-games-crashed-on-a-route-that-never-ends)). The unit layout of the port has 1.22's offsets in the copied blocks, and the sprites that the emulated code reads (the units', the unit panel's and the map tiles' sizes and the panel's position) have 1.22's values (`set_sprt_data`, `0x432E09` to `0x432F21`).
- **Enzan.** The semantics of the 70 mnemonics that the translations use were read against x86: the flags of each arithmetic, logical and shift instruction, the conditions of each `jcc` and `setcc`, `cdq`, `idiv`, `movsx`, `rep stosd`, and the condition bits of `fnstsw`. The four discrepancies above were found and fixed.

What it does not cover: the translation could still differ from the machine code where Enzan's semantics and the reading agree but both are wrong, and in the natives' and stand-ins' behavior beyond what is listed. A differential test against an emulator of the machine code, or a recording of 1.22 on Windows (below), would.

### Known
- **Jump tables guarded by a register.** The Disassembler sizes a jump table from the last `cmp reg, imm`, even with a `cmp reg, reg` after it: a table of 17 cases was translated as 3. The translations today have the right sizes (Checked), but a new function must be checked again.
- **A function's length past its end.** When a function's length runs into its jump table, the table is translated as code. The lengths must end before the tables; today they do (Checked).
- **Missing instruction forms.** The Disassembler stops at the first form it does not know, with its address (for example 16-bit `inc`/`dec`, `imm8` forms of `imul`, `test rm8, r8`, `jns rel32`, some x87 forms). Each is added to the Disassembler and Enzan, with a test of Enzan.
- **x87 precision.** Enzan keeps the FPU stack as `double`s, where x87 registers are 80-bit. 1.22 sets the precision to 53 bits at startup: `_controlfp(_PC_53, _MCW_PC)`, called from `0x436083` (the call is at `0x4363DE`). So addition, subtraction, multiplication, division and square roots round as `double`s do. Only the wider exponent range of x87 registers differs, for intermediate overflow and underflow, which the game's values do not reach.
- **`fsin`, `fcos` and `fpatan`.** Real x87 results differ from the port's functions in the last bits, and differ between processors. So there is no single machine result to match: the port's functions are the reference. They are not affected by the precision control either: 1.22 keeps their results with a 64-bit mantissa in a register until an instruction that uses them rounds its own result, where the port rounds them to `double`s first. That changes a last bit now and then, which once changed a battle against a trace of 1.22 ([X87Emulation.md](X87Emulation.md)).
- **Natives.**
  - `rand` (`0x436056`) is the port's `rand()`, so that the whole game draws from one sequence. It is the same generator as 1.22's (`* 214013 + 2531011`, seed at `0x442250`).
  - `__ftol` (`0x4360C4`) truncates, as the CRT does, and gives `0x8000000000000000` for a NaN or a value out of range.
- **Imports.** GDI, DirectDraw, DirectSound and the timer are stand-ins, and they define what 1.22 sees. A wrong stand-in is a bug of the port, not of the emulator.
- **Memory past the image.** In 1.22, the image ends with its resources (`.rsrc`, `0x4F1000` to `0x4F2000`), which are read-only, and nothing is mapped after them, so a write past `.data` (which ends at `0x4F0A34`) is an access violation, and 1.22 crashes. The emulator's memory is writable there, and holds the port's own objects after the image (`Emulator.Allocate`), so such a write goes through, corrupts them, and throws only at the end of the memory. The one translated function known to write there, `set_cpu_route`, is stopped before it does ([KnownIssues.md](KnownIssues.md#single-player-games-crashed-on-a-route-that-never-ends)).
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
| シナリオ選択へ and リジューム do nothing in single-player | They wait for the rival (`RivalMode`), and the system orders go back to NSPW NET's setting screen, which single-player does not have |
