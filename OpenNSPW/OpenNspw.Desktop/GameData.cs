namespace OpenNspw.Desktop;

// Where a game's files come from (docs/BundledData.md): the original's that the app embeds (BundledFiles), with what the
// game writes in the user's folder; or NSPW NET's data directory (--data), read and written, and NSPW 1.22's executable
// and its directory (--exe), instead of the embedded ones.
internal sealed class GameData(string? dataDirectory, string? executablePath)
{
	private readonly string? _dataDirectory = dataDirectory;
	private readonly string? _executablePath = executablePath;

	// The user's folder of the game's files, which keeps what the game writes beside the embedded files: Files, beside
	// the settings (DesktopPlatform, UISettings).
	public static string UserDirectory { get; } =
		Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"OpenNSPW",
			"Files"
		);

	// The files of 1.22 that a single-player game reads instead of NSPW NET's: its executable's directory, or the three
	// embedded ones that differ from NSPW NET's.
	private IGameFileSource Nspw122Files =>
		_executablePath is { } path
			? new DirectoryFileSource(Path.GetDirectoryName(Path.GetFullPath(path))!)
			: new BundledFiles("Nspw122");

	// The files of a game, a single-player one or not.
	public GameFileLayers Files(bool isSingleplayer)
	{
		IGameFileSource[] nspw122 = isSingleplayer ? [Nspw122Files] : [];
		return _dataDirectory is { } directory
			? new GameFileLayers(nspw122, directory, [])
			: new GameFileLayers([], UserDirectory, [.. nspw122, new BundledFiles("NSPW_NET")]);
	}

	// NSPW_122.exe: the one given, or the embedded one.
	public byte[] ReadExecutable()
	{
		return _executablePath is { } path
			? File.ReadAllBytes(path)
			: BundledFiles.Read("Nspw122/NSPW_122.exe");
	}
}
