namespace OpenNspw.App;

// How the app was built.
internal static class AppBuild
{
	// Whether it is a Debug build, which shows what is there for debugging, such as the battles' options
	// (SettingsSection.Debug).
#if DEBUG
	public const bool IsDebug = true;
#else
	public const bool IsDebug = false;
#endif
}
