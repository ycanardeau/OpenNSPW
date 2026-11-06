# Enzan

**An x86 emulator written in C#, built to run machine code translated to C#.** Each IA-32 instruction is a method that computes the same result as the CPU and sets the same flags, so the translated code computes exactly what the original does, down to the carry bit.

[![NuGet](https://img.shields.io/nuget/dt/Aigamo.Enzan?label=NuGet&logo=nuget)](https://www.nuget.org/packages/Aigamo.Enzan)
[![.NET Standard 2.0](https://img.shields.io/badge/.NET_Standard-2.0-512BD4?logo=dotnet)](https://learn.microsoft.com/dotnet/standard/net-standard)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

[OpenNSPW](../OpenNSPW) runs the single player of NSPW 1.22 on Enzan: the computer player's strategy and the scenarios, functions of the original executable that the [Disassembler](../Disassembler) translates to C#, one line per instruction.

```csharp
cpu.Ecx = cpu.Mov(new Register32(10));      //       mov  ecx, 10
cpu.Eax = cpu.Xor(cpu.Eax, cpu.Eax);        //       xor  eax, eax
do                                          // loop:
{
    cpu.Eax = cpu.Add(cpu.Eax, cpu.Ecx);    //       add  eax, ecx
    cpu.Ecx = cpu.Dec(cpu.Ecx);             //       dec  ecx
} while (cpu.Jne);                          //       jne  loop
                                            // eax = 55
```

## Why Enzan

- **Same bits as the CPU.** Results wrap around as on the CPU, and the flags (carry, zero, sign, overflow) are set as the Intel manual says, so a `jl` after a `cmp` branches exactly when the original's would.
- **Fast without a decoder.** There is no fetch-decode-execute loop: the translated code is ordinary C#, which the JIT compiles. Registers are small structs, and the instructions are methods marked for inlining.
- **The whole register file.** `eax` through `edi`, with their 16-bit (`ax`) and 8-bit (`al`, `ah`) views that read and write the same storage; EFLAGS; and the x87 FPU's register stack and status word.
- **The memory is yours.** The CPU works on a `Memory<byte>` you hand it, mapped at an address you choose, with the stack in it. `call` goes to a callback, so you decide what each address runs, a translated function or a stand-in for an API.
- **Choose your transcendentals.** `fsin`, `fcos` and `fpatan` call functions you can replace, such as with an implementation that matches the x87 bit for bit.
- **Runs almost anywhere.** It targets .NET Standard 2.0: .NET Framework 4.6.1 and later, .NET Core, .NET 5 and later, Mono and Unity.

## Installation

```xml
<PackageReference Include="Aigamo.Enzan" Version="0.0.1-alpha.*" />
```

The packages built from this repository are versioned `0.0.1-alpha.*`. NuGet also lists older `1.x` versions, which it picks by default, so ask for `0.0.1-alpha.*`.

## Usage

```csharp
using Aigamo.Enzan;

// 64 KiB of memory at 0x400000, with the stack at its top.
var cpu = new Cpu(new byte[0x10000], new Register32(0x400000), callback: target => Dispatch(target));
cpu.Esp = new Register32(0x410000);

cpu.Push(cpu.Ebp);                              // push ebp
cpu.Ebp = cpu.Mov(cpu.Esp);                     // mov  ebp, esp
cpu.Cmp(cpu.Eax, new Register8(0x02));          // cmp  eax, 2
if (cpu.Jl) goto done;                          // jl   done
cpu.Eax = cpu.Imul(cpu.Eax, cpu.Ecx);           // imul eax, ecx
done:
cpu.Ebp = cpu.Pop32();                          // pop  ebp
```

Immediates are `Register8`, `Register16`, `Register32` and `Register64` values, and an 8-bit immediate is sign-extended where the instruction does so. Floating-point code works on the FPU's stack:

```csharp
var st = cpu.Fpu.Stack;
cpu.Fld(Register64.FromDouble(Math.PI / 2));    // fld  qword [pi_2]
st[0] = cpu.Fsin(st[0]);                        // fsin
cpu.Fcom(Register64.FromDouble(1.0));           // fcom qword [one]
cpu.Ax = cpu.Fnstsw();                          // fnstsw ax
cpu.Test(cpu.Ah, new Register8(0x41));          // test ah, 41h
```

`AddressingMode`, `ModRM` and `Sib` decode an instruction's operands from a `BinaryReader`, for a translator that reads machine code.

## Instructions

| Group | Instructions |
| --- | --- |
| Arithmetic | `add` `sub` `sbb` `inc` `dec` `neg` `imul` `idiv` `cdq` |
| Logic and shifts | `and` `or` `xor` `test` `shl` `shr` `sar` |
| Comparison | `cmp`, the conditions `ja` `jae` `jb` `je` `jne` `jg` `jge` `jl` `jle` `jns`, and `sete` `setg` |
| Data movement | `mov` `movsx` `lea` `push` `pop` `stosd` `rep stosd` |
| Control | `call` |
| x87 FPU | `fld` `fild` `fst` `fxch` `fadd` `fsub` `fsubr` `fmul` `fimul` `fdiv` `fdivr` `fidiv` `fchs` `fsin` `fcos` `fpatan` `fcom` `fnstsw` |

These are the instructions that the translated code of NSPW 1.22 uses. Adding another is a method on `Cpu`.

## Benchmarks

[Aigamo.Enzan.Benchmarks](Aigamo.Enzan.Benchmarks) times loops written the way translated code drives the CPU: integer arithmetic, compares and branches, push and pop, 8-bit registers and the FPU.

```bash
dotnet run --configuration Release --project Enzan/Aigamo.Enzan.Benchmarks
```

## References

- [Intel® 64 and IA-32 Architectures Software Developer's Manual, Volume 2: Instruction Set Reference](https://www.intel.com/content/www/us/en/architecture-and-technology/64-ia-32-architectures-software-developer-instruction-set-reference-manual-325383.html)
- [X86 Opcode and Instruction Reference, coder32 edition](http://ref.x86asm.net/coder32.html)

## License

[MIT](LICENSE)
