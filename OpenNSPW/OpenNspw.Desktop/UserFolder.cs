namespace OpenNspw.Desktop;

// The user's folder of the app, where it keeps what it writes: the settings, and the game's own files, such as its
// saves (DesktopPlatform). %APPDATA%\OpenNSPW on Windows, ~/Library/Application Support/OpenNSPW on macOS,
// ~/.config/OpenNSPW on Linux.
internal static class UserFolder
{
	public static string Path { get; } =
		System.IO.Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"OpenNSPW"
		);
}
