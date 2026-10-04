# Changes
Changes made to the original NSPW NET source so that it builds with Visual Studio 2022 and runs on Windows 11. The C++ sources are unmodified; all changes are to the project and resource files.

## Requirements
- Visual Studio 2022 with the **Desktop development with C++** workload.
- DirectX SDK (June 2006). See [../DirectXSDK.md](../DirectXSDK.md).
- The DirectPlay Windows feature to run the game. See [../DirectPlay.md](../DirectPlay.md).

## Project
- **Converted `NSPW_NET.vcproj` (Visual Studio 2008) to `NSPW_NET.vcxproj` and `NSPW_NET.vcxproj.filters`** with Visual Studio 2022 (platform toolset v143). The original `.vcproj` is kept for reference.
- **Replaced the hard-coded SDK paths with `$(DXSDK_DIR)`.** The project pointed to `C:\Program Files\Microsoft DirectX SDK (June 2006)`, but on 64-bit Windows the SDK installs to `C:\Program Files (x86)`. The installer sets `DXSDK_DIR` to the actual location.
- **Searched the DirectX SDK after the Windows SDK** (`IncludePath` and `LibraryPath` instead of `AdditionalIncludeDirectories` and `AdditionalLibraryDirectories`). The DirectX SDK ships outdated copies of Windows headers such as `rpcsal.h`, `d3d9.h`, `ddraw.h` and `dsound.h`, which broke the build with hundreds of syntax errors when they took precedence. The DirectX SDK now only provides what the Windows SDK lacks: D3DX9, `dxerr9`, DirectPlay 8 and parts of DirectMusic.
- **Removed the paths to the Microsoft Platform SDK**, which the Windows SDK replaces.
- **Linked `$(DXSDK_DIR)Lib\x86\dxguid.lib` explicitly.** The Windows SDK's `dxguid.lib`, which is found first, lacks the DirectMusic GUIDs used by `Audio.cpp` (`CLSID_DirectMusicLoader`, `CLSID_DirectMusicPerformance`, `IID_IDirectMusicLoader8`, `IID_IDirectMusicPerformance8`).
- **Linked `legacy_stdio_definitions.lib`.** `dxerr9.lib` was built against the old C runtime and references `__vsnwprintf`, which the current C runtime only provides through this library.

## Resources
- **Added `NSPW_NET.RC`.** The resource script, which defines the dialogs and the icon referenced by `resource.h`, was not included in the original source distribution.
- **Converted `NSPW_NET.RC` from Shift_JIS to UTF-8 with BOM** and changed its `#pragma code_page(932)` and `#pragma code_page(1252)` to `#pragma code_page(65001)`. Visual Studio displayed the Shift_JIS file garbled on systems whose code page is UTF-8. The English section's `code_page(1252)` was also wrong for its Shift_JIS font name `ＭＳ ゴシック`.

## Text encoding
The C++ sources are UTF-8 with BOM, but the game expects Shift_JIS strings at run time: it draws text with `TextOut` using fonts created with `SHIFTJIS_CHARSET`, and the original build (`NSPW_NET_110/NSPW_NET.exe`) stores its strings as Shift_JIS. Without the following changes, Japanese text in the game is garbled.

- **Compiled with `/execution-charset:.932`**, so narrow string literals are stored as Shift_JIS, as in the original build on a Japanese system.
- **Added `NSPW_NET.manifest`, which sets `<activeCodePage>ja-JP</activeCodePage>`**, so the process uses code page 932 regardless of the system locale. This affects message boxes, dialogs, window titles and file names, such as the Japanese names of the scenario files in `Scenario/`. It requires Windows 11; on earlier versions, the system code page is used.
