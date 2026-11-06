using OpenNspw.Replay;

namespace OpenNspw.App;

// Where a game's files come from (docs/BundledData.md): the original's that the app embeds (BundledFiles), with what the
// game writes in the user's folder; or NSPW NET's data directory (--data), read and written, instead of the embedded
// NSPW NET's.
internal sealed class GameData(string? dataDirectory)
{
	private readonly string? _dataDirectory = dataDirectory;

	// The user's folder of the game's files, which keeps what the game writes beside the embedded files: Files, beside
	// the settings (AppPlatform, UISettings).
	public static string UserDirectory { get; } =
		Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"OpenNSPW",
			"Files"
		);

	// Where the app keeps the replays of the battles played (docs/Replays.md), beside the game's files.
	public static string ReplayDirectory { get; } =
		Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"OpenNSPW",
			"Replays"
		);

	// The files of a game, a single-player one or not. A single-player game reads 1.22's three embedded files that differ
	// from NSPW NET's first.
	public GameFileLayers Files(bool isSingleplayer)
	{
		IGameFileSource[] nspw122 = isSingleplayer ? [new BundledFiles("Nspw122")] : [];
		return _dataDirectory is { } directory
			? new GameFileLayers(nspw122, directory, [])
			: new GameFileLayers([], UserDirectory, [.. nspw122, new BundledFiles("NSPW_NET")]);
	}

	// The files of a replay's game: those its battle started from first (StartFileSource), then the game's, only read.
	// What the game writes as it plays the replay, such as the auto-save, goes to `scratch`, not to the user's folder.
	public GameFileLayers ReplayFiles(ReplayStart start, string scratch)
	{
		IGameFileSource[] nspw122 =
			start.Mode == ReplayMode.Singleplayer ? [new BundledFiles("Nspw122")] : [];
		return new GameFileLayers(
			[new StartFileSource(start.Files), .. nspw122],
			scratch,
			_dataDirectory is { } directory
				? [new DirectoryFileSource(directory)]
				: [new BundledFiles("NSPW_NET")]
		);
	}
}
