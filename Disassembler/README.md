# Disassembler
You need the original `NSPW_122.exe` file to run this program.

Translates the functions of the single-player executable that OpenNSPW emulates to C#, for `OpenNSPW/OpenNspw.Singleplayer` (see [docs/Singleplayer.md](../docs/Singleplayer.md)):

```bash
dotnet run --project Disassembler/OpenNspw.Disassembler -- Original/Nspw122/NSPW_122.exe OpenNSPW/OpenNspw.Singleplayer/Subroutines
```
