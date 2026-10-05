# Original
The original Naval South Pacific War (NSPW) distributions, kept unmodified as reference material for OpenNSPW.

Naval South Pacific War is a real-time strategy game for Windows by Ken-ichi Tokumitsu (徳光 健一) / Nara-Shika Seisakusho (奈良鹿製作所), first released in 1999.

## Contents

| Folder | Description |
| --- | --- |
| `Nspw122` | NSPW 1.22 (`NSPW_122.exe`). Single-player version, 3rd edition and the final scenario-expansion release (built December 2002). Requires DirectX 7.0a or later. The included note (`皆様へ、.txt`) is left over from 1.20c. |
| `NSPW_200` | NSPW 2.00 (`NSPW.exe`). Single-player version, rebuilt to run on Windows 7 and later. |
| `NSPW_NET_110` | NSPW NET 1.10 (`NSPW_NET.exe`). Multiplayer-only version (Naval South Pacific War on the Net), first released in 2002. |
| `NSPW_NET` | Source code of NSPW NET. Visual C++ 2008 project (`NSPW_NET.vcproj`) targeting Win32 and DirectX, converted to Visual Studio 2022 (`NSPW_NET.vcxproj`). See [docs/NSPW_NET/CHANGES.md](docs/NSPW_NET/CHANGES.md) for the changes made to build and run it, and [docs/NSPW_NET/Synchronization.md](docs/NSPW_NET/Synchronization.md) for how its multiplayer stays in sync. |

Each game distribution includes the author's manual in `説明書/` (Shift_JIS HTML).

To run NSPW 1.22 on Windows 8 and later (including Windows 11), the DirectPlay Windows feature must be installed. See [DirectPlay.md](DirectPlay.md).

To build the NSPW NET source, the DirectX SDK (June 2006) must be installed. See [DirectXSDK.md](DirectXSDK.md).

## Links
- [奈良鹿製作所 (Nara-Shika Seisakusho)](http://narashikabranch.web.fc2.com/): the author's website.
  - [Download page (2.00 and NET 1.10)](http://narashikabranch.web.fc2.com/dl_site_2nd/index.html)
  - [Older software page](http://narashikabranch.web.fc2.com/dl_site_1st.htm): describes 1.22, but no longer offers it for download.
- Vector: [Naval South Pacific War](https://www.vector.co.jp/soft/winnt/game/se136841.html), [Naval South Pacific War on the Net](https://www.vector.co.jp/soft/win95/game/se242938.html)
- [Tokumizman (@tokumizman) on X](https://x.com/tokumizman): the author's account.

The author's original homepages linked from the manuals (`www02.u-page.so-net.ne.jp/ta2/kenken/` and `www008.upp.so-net.ne.jp/nara-shika/`) no longer exist.

## License
The games are freeware. According to the manuals, all copyrights belong to the author, and redistribution is generally permitted, though the author asks to be notified when the software is reposted. The software is provided as is, and the author accepts no responsibility for any damage caused by its use. See `Nspw122/説明書/ＮＳＰＷ説明書.htm`, `NSPW_200/説明書/ＮＳＰＷ説明書.htm` and `NSPW_NET_110/説明書/説明書.htm` for the original terms.

These files are not covered by the licenses of the other projects in this repository.
