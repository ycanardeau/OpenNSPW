namespace OpenNspw.Desktop;

// Where a game's files come from (docs/BundledData.md): the original's that the app embeds (BundledFiles), with what the
// game writes in the user's folder; or NSPW NET's data directory (--data), read and written, instead of the embedded
// NSPW NET's.
internal sealed class GameData(string? dataDirectory)
{
	private readonly string? _dataDirectory = dataDirectory;

	// The user's folder of the game's files, which keeps what the game writes beside the embedded files: Files, beside
	// the settings (DesktopPlatform, UISettings).
	public static string UserDirectory { get; } =
		Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"OpenNSPW",
			"Files"
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
}
