# Bundled data
How the desktop app carries the original's files that the game reads, so that `OpenNSPW.app` and the Windows and Linux builds play without an `Original` folder.

## Summary
- **The app embeds what the game reads, as the files are**: NSPW NET's sounds, maps and scenarios, and for single-player, `NSPW_122.exe` and the three files of 1.22's that differ from NSPW NET's. About 5 MB in the app, 0.9 MB in a zipped download.
- **No sprite sheet**: the game draws from the cut sprites already ([GameSprites.md](GameSprites.md)).
- **What the game writes goes to the user's folder**, beside the settings: its saves, and a scenario that the map editor saves over. The user's folder is read first, so a scenario put there is listed with the bundled ones.
- **A bundled file is copied to the user's folder when the game first writes to it**, not when it opens it: the original opens the files it only reads, such as the maps, to read and write.
- **`--data` and `--exe` still use other files**, such as a player's own NSPW NET folder. Without them, the app plays its own, also when it runs from the repository.
- **The author's terms**: the games are freeware whose author permits redistribution and asks to be told of it (Original/README.md). The app ships a notice of his copyright and terms (`OriginalNotice.txt`).

## What the game reads
| Folder of the game | Files | Size | Read by |
| --- | --- | --- | --- |
| `wav` | NSPW NET's 32 sounds | 1.3 MB | `InitDSound` (`WAV\NAME.wav`) |
| `Map` | `South_pacific.dat`, `Middle_pacific.dat`, `Japan_off.dat` | 384 KB | `load_it2` (`LoadScenarioFile2`) |
| `Scenario` | `中部太平洋.dat`, `南太平洋.dat`, `日本近海.dat`, `tst2.dat`, `tst3.dat`, `てすと.dat` | 2.9 MB | the setting screen's list (`FindFirstFile("Scenario\\*.dat")`) and `load_it3`, `load_user_map` |
| 1.22's executable | `NSPW_122.exe` | 272 KB | the emulator (its image), the scenarios' titles and briefings (`ScenarioSelection`) |
| 1.22's `Map` | `japan_off.dat`, without NSPW NET's island | 128 KB | a single-player game's `load_it2` |
| 1.22's `wav` | `GUN3.wav`, `AA_SHL4.wav`, other sounds than NSPW NET's | 31 KB | a single-player game's sounds |

**Not bundled**:
- **1.22's other files**: its `South_pacific.dat`, `Middle_pacific.dat` and 26 other sounds are byte for byte NSPW NET's, which a single-player game reads where 1.22's files have none (`GameFileLayers`). Its `Map_1.dat` and `no_land_map.dat` are named nowhere in its code.
- **`Saved`**: the game's own saves (`auto_save.dat`, `resume_1.dat`, `cnct_error.dat`), which belong to each player. The repository's are the author's.
- **`t3.bmp`, `tst_cg1.bmp`**: replaced by the sprites.
- **`Mapuser_map.dat`**, at the top of NSPW NET's folder, is named nowhere in the code.
- **The manuals** (`説明書`): not read by the game. Their terms go into the notice.

## What the game writes
The game writes only through `CreateFile` with `GENERIC_WRITE` (`fileapi.cs`):

| File | Written by | Mode |
| --- | --- | --- |
| `Saved\resume_1.dat`, `Saved\cnct_error.dat` | the saves of a network game (`save_on_resume`) | `OPEN_ALWAYS` |
| `Saved\auto_save.dat` | opened by the setting screen (`go_cnct_game_setting`) | `OPEN_EXISTING` |
| `UserScenarioFileName`, a `Scenario\*.dat` | `save_user_map`, the map editor's save | `OPEN_ALWAYS` |

But the game also opens every file it only reads to read and write, `OPEN_ALWAYS`: `load_it2`, `load_it3`, `load_user_map`. `OPEN_ALWAYS` creates a file that does not exist, as on Windows: a map that is missing is created empty, and the original's folder gets it.

## The files of a game
`DesktopPlatform` reads and writes the game's files through layers (`GameFileLayers`, in the core, which the tests' platforms use too), by the game's path with `/` separators, its names in any case. `GameData` puts them in order:

1. **The user's folder**, `OpenNSPW/Files` beside `settings.json` (`%APPDATA%`, `~/Library/Application Support`, `~/.config`; `GameData.UserDirectory`): written, and read first.
2. **In a single-player game, 1.22's files**: the bundled ones (`BundledFiles("Nspw122")`), or the directory of `--exe` (`DirectoryFileSource`).
3. **NSPW NET's bundled files** (`BundledFiles("NSPW_NET")`).

With `--data`, the data directory takes the place of the user's folder and of NSPW NET's files, read and written, as before; in a single-player game, 1.22's files come before it, as 1.22's directory did (`GameFileLayers.OfDirectories`).

- **Opening to read** opens the first layer that has the file.
- **Opening to write a file that the user's folder has** opens it there, as a file.
- **Opening to write a file that only a layer that is only read has** opens a stream (`CopyOnWriteStream`) that reads the bundled bytes until the game first writes or sets its length; then it copies them to the user's folder, opens that file at the same position, and goes on there. A file only read is never copied, so a later version of the app reads its own.
- **Opening to write a file that no layer has** creates it in the user's folder, as `OPEN_ALWAYS` does.
- **Listing a folder** (`FindFiles`) lists the names of every layer, each once, in the order of `GameFiles.Find`.

**The names**: an embedded file is a resource named by its path under the game's folder (`NSPW_NET/Scenario/てすと.dat`, `LogicalName` in the project). Japanese names are compared in Unicode's composed form (NFC), as macOS may hand the build decomposed names, and in any case.

## Exits and updates
**When the game exits**, nothing is left to save: a file copied to the user's folder is an ordinary file from its first write, and every later write goes to it as the game makes it, as the original writes to its own folder. Closing the window gives the game's thread 3 seconds to end (`NspwGame.OnExiting`), after which the process ends with it, as it is a background thread: what the game had written by then is in the file, and what it had not is lost, as when the original is killed. A copy kept in memory and written when the file is closed would lose the whole save then.

**When the app is updated**, its bundled files are replaced with it, and the user's folder is not touched:
- **A file the game only read** was never copied, so the game reads the new version.
- **A file the game wrote** stays the user's, and hides the new bundled one of the same name: a scenario saved over with the map editor stays as it was saved. That is the point of the user's folder, but it also means that a fix to a bundled file does not reach a player who saved over it. Deleting the user's copy brings the bundled one back. As the bundled files are the original's, which do not change, a fix to one is not expected.
- **The saves** (`Saved`) are the user's only, and stay. They hold the game's state in the original's layout (`Units`, `Fires`), which the port keeps, so an update reads them as before.
- **A file the game created** with `OPEN_ALWAYS` where none existed stays too: a network game's guest that lacks the host's user scenario opens it by name, and the original creates it empty in its folder, which then lists it. The port does the same in the user's folder.
- **Uninstalling** leaves the user's folder, as it leaves the settings.

## The single-player executable
- **Embedded as it is.** The emulator starts from its image, and `ScenarioSelection` reads the titles and briefings from it, as now. Taking out the parts the emulator reads, or keeping the texts in C#, would add thousands of generated lines to save 272 KB, and two places to keep in step with it.
- `GameData.ReadExecutable` reads the bundled one unless `--exe` is given, and the main menu's single-player item is always on.

## The command line
| | Without the option | With it |
| --- | --- | --- |
| `--data <directory>` | NSPW NET's bundled files, and the user's folder | that directory, read and written, as before |
| `--exe <NSPW_122.exe>` | the bundled executable and 1.22's three files | that executable and its directory, as before |

The app no longer looks for `Original` up from its own directory: a build run from the repository plays the bundled files, as a release does, and leaves `Original` unchanged, which its README asks. `tools/package-linux-appimage.sh` no longer passes the `Original` beside the AppImage, and the release notes no longer ask for one: they say where the user's folder is.

## The notice
`OpenNspw.Desktop/OriginalNotice.txt`, in English and Japanese: what is bundled, that it is the work of Ken-ichi Tokumitsu (徳光 健一) / 奈良鹿製作所, all of whose copyrights are his, that it is freeware he permits to redistribute, that it is provided as is, and where his site and the original distributions are. It ships beside the app's executable, as the fonts and their license do, and in `Contents/Resources` in `OpenNSPW.app` (`tools/package-macos-app.sh`); the release notes name it.

## Tests
`BundledDataTests`, of which those that compare with the original need `Original`, as the other tests of whole games do (`GameData`):

- **The layers**: a file is read from the first layer that has it; one opened to write is copied when it is first written, at the position written, and not when it is opened; a file that no layer has is created in the directory with `OPEN_ALWAYS`, and not with `OPEN_EXISTING`; a directory lists the names of every layer once, in any case and either Unicode form.
- **The bundled files**: found in any case and either Unicode form; each byte for byte the file of `Original` it names, `NSPW_122.exe` too; and every file of 1.22's that a single-player game reads and that is not bundled byte for byte NSPW NET's.
- **A game plays from the bundled files alone**, with an empty user's folder and the sprite files (`playsBundledFiles` of `LockstepRun` and `SingleplayerRun`): the committed `title` trace is unchanged, and scenario 202 shows the same frames, texts and sounds as from `Original`, 200 frames.

## Open questions
- **Telling the author**: the README says he asks to be told when the software is reposted: before the first release that bundles the files.
