# Smaller builds
How the desktop app's downloads (`release.yml`) are made smaller, without changing what they do. Trimming is done; the fonts' subsets are still a proposal.

## Summary
- **The app is trimmed** (`PublishTrimmed`): the zipped downloads from about 45 MB to about 20 MB on each platform, the original's files that the app now embeds included ([BundledData.md](BundledData.md)). Its 24 warnings were in three places of our own code, none in the packages, and are gone.
- **Proposed: subset the fonts** to the characters that can be drawn: after trimming, the three fonts are the largest part of the download, 9.1 of its 20.5 MB on a Mac.
- **Do not compress the single file**: the downloads are zipped or, for Linux, compressed by the AppImage, which saves as much.
- **Keep the rest**: the embedded symbols, which give the exceptions the game shows their lines, and SDL2 and OpenAL, which MonoGame needs.

## Where the size is
Measured for each runtime with the publish of the release (`release.yml`, `tools/package-macos-app.sh`, `tools/package-linux-appimage.sh`), self-contained in one file, before the app is put in its bundle or AppImage, and zipped with its libraries and fonts.

| Runtime | Executable, as released before | Trimmed | With the original's files | Zipped, as released before | Trimmed | With the original's files |
| --- | --- | --- | --- | --- | --- | --- |
| `osx-arm64` | 86.6 MB | 19.6 MB | 24.8 MB | 44.2 MB | 19.6 MB | 20.5 MB |
| `win-x64` | 80.2 MB | 19.0 MB | 24.2 MB | 44.9 MB | 18.8 MB | 19.7 MB |
| `linux-x64` | 80.1 MB | 20.9 MB | 26.1 MB | 45.1 MB | 19.3 MB | 20.3 MB |

The releases before had none of the original's files; the app now embeds about 5 MB of them, 0.9 MB zipped.

Compressing the single file (`EnableCompressionInSingleFile`), measured on `osx-arm64` before the sprite change: 38.6 MB untrimmed, 14.0 MB trimmed, but 19.4 MB zipped, as the zip compresses as well.

The trimmed `osx-arm64` download, zipped part by part:

| Part | Unzipped | Zipped |
| --- | --- | --- |
| `OpenNspw.Desktop`, the executable | 19.4 MB | 7.9 MB |
| `Fonts`: BIZ UDGothic, BIZ UDPGothic and its bold, and their license | 14.0 MB | 9.1 MB |
| `libSDL2-2.0.0.dylib`, `libopenal.dylib` | 6.3 MB | 2.5 MB |

Untrimmed, the executable carries the whole .NET runtime, most of which the app never calls: `System.Private.Xml` (9.0 MB), `System.Linq.Expressions` (4.7 MB), `System.Data.Common` (3.2 MB), `System.Security.Cryptography`, `System.Net.Http`, `Microsoft.VisualBasic.Core` and so on. The app's own assemblies are 3.1 MB, a third of it `OpenNspw.Singleplayer`, the emulator's translated code. The sprites are 456 KB.

## Trimming
- **Set in `OpenNspw.Desktop.csproj`**: `<PublishTrimmed>true</PublishTrimmed>`, which applies only to a publish, so `dotnet build` and the tests are as before. Not on the command line: `/p:PublishTrimmed=true` reaches every referenced project too, and fails with `NETSDK1124`. `/p:PublishTrimmed=false` publishes the app untrimmed, to compare.
- **A trim warning fails the publish** (`<ILLinkTreatWarningsAsErrors>true</ILLinkTreatWarningsAsErrors>`), so that code the trimmer cannot follow, added later, fails the release rather than the player's game.

The warnings, and how each went away:

| Where | What the trimmer cannot follow | Fix |
| --- | --- | --- |
| `UISettings.cs`, `DesktopPlatform.cs` (8) | `JsonSerializer` by reflection, for `ui.json` and `settings.json` | A generated `JsonSerializerContext` each: `UISettings.SavedJson`, with the language by its name as before, and `DesktopPlatform.SettingsJson` for `Dictionary<string, string>` |
| `Trace122.cs`, `Trace122Layout.FieldsOf` (1) | `Type.GetFields` on the traced structs and their fields' types, to name a difference of `--replay` | Suppressed (`UnconditionalSuppressMessage`): the types are structs, whose fields trimming always keeps. An annotation could not follow the fields' types |
| Saruhashi's `DataBindings` (16) | `TypeDescriptor`, for data binding, which the app does not use | `[RequiresUnreferencedCode]` in Saruhashi on the binding classes (`Binding`, the binding managers, `ListBindingHelper`, `Formatter`, `UITypeEditor`) and on where an app starts binding (`ControlBindingsCollection.Add`, `BindingContext`'s indexers and `UpdateBinding`), so that an app that binds is warned where it binds. Suppressed where only an existing binding gets: `Control.UpdateBindings` and `ListBindingConverter`'s designer code. Saruhashi has no trim warning left under the trim analyzer; `RadioButton` finds `Checked` by its type rather than its instance for it |

What the app uses by name and the trimmer could miss, checked on the trimmed `osx-arm64` build with a temporary hook:

- **Shift_JIS**, from `CodePagesEncodingProvider`, which the game registers in code (`tchar.ShiftJis`): encodes and decodes.
- **The settings**: `ui.json` and `settings.json` are written and read back.
- **The embedded resources**, the sprites and the icon, read by name: resources are never trimmed, and a sprite decodes.
- **MonoGame and FontStashSharp** gave no warnings.

## The fonts
- **What they draw**: the game's font (BIZ UDGothic, fixed width) draws what the game writes, which is Shift_JIS: the game keeps its text as Shift_JIS bytes (`TextOut`), the chat and the players' names too. The UI's fonts (BIZ UDPGothic and its bold) draw the UI's texts (`Texts`, `GameTexts`, `SingleplayerTexts`, in English and Japanese) and the game's.
- **The subset**: Shift_JIS's characters (code page 932: JIS X 0208 and its NEC and IBM extensions, about 7,900), and every character of the UI's texts beyond them, in all three fonts. Every other glyph goes.
- **Made by a tool**, `tools/SubsetFonts`, with fontTools' `pyftsubset`, from the fonts as the BIZ UD project publishes them, which stay in the repository outside the app; the subsets are what the app ships. A test checks that the subsets have a glyph for every character of the UI's texts and of code page 932, so that a text added later that needs another fails the test rather than drawing nothing.
- **The license allows it**: the SIL Open Font License 1.1, whose copyright line reserves no font name, so a subset may keep its name. `OFL.txt` ships with it, as now.
- **How much it saves** is not measured yet: fontTools is not installed here. The first step left measures it before anything is committed.
- **Not proposed**: drawing the UI's bold with the regular font, which would save a whole font but change how the UI looks.

## Not proposed
- **Native AOT**: smaller still, but MonoGame's DesktopGL and the emulator's 1 MB of translated code would have to compile ahead of time on each runtime, for a gain after trimming that is not known.
- **ReadyToRun**: faster to start, larger.
- **Framework-dependent builds**: the release notes promise builds that run without installing .NET.
- **Invariant globalization**: macOS and Linux use the system's ICU, which is not shipped, so there is little to save, and formatting by culture would change.

## Checked
- The trimmed `osx-arm64` app ran its main menu and the single-player scenarios 202 and 303, 20 seconds each, with nothing on its error output.
- Not run trimmed: a network game between two apps, a replay (no trace of 1.22 is in the repository), the map editor, and the Windows and Linux builds, which were only published.

## Steps left
1. The font subsets: the tool, the test, the measurement, then the fonts the app ships. fontTools (`pyftsubset`) has to be installed for it.

## Open questions
- **Where the original fonts are kept**, if the subsets replace them in `OpenNspw.Desktop/Fonts`: proposed, a folder of their own beside the tool.
- **The bundling of the original's files** in the app is not part of this: it makes the app larger, and is a choice of its own.
