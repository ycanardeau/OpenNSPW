using System.Runtime.InteropServices;

namespace OpenNspw.Desktop;

// The functions of SDL that MonoGame's DesktopGL does not give, called in the library that MonoGame has loaded: the one
// beside the app, as a release publishes it, or in its runtimes folder, as a build leaves it. The same file, which the
// process has loaded once, so that its state is that of MonoGame's window; another copy would have no window.
internal static class Sdl
{
	private const string Library = "SDL2";

	// SDL_GetMouseState's buttons.
	public const uint ButtonLeft = 1 << 0;
	public const uint ButtonMiddle = 1 << 1;
	public const uint ButtonRight = 1 << 2;
	public const uint ButtonX1 = 1 << 3;
	public const uint ButtonX2 = 1 << 4;

	// SDL_WasInit's video.
	public const uint InitVideo = 0x20;

	// SDL's file on this system, as MonoGame names it.
	private static string FileName =>
		OperatingSystem.IsWindows() ? "SDL2.dll"
		: OperatingSystem.IsMacOS() ? "libSDL2-2.0.0.dylib"
		: "libSDL2-2.0.so.0";

	private static nint Resolve(
		string libraryName,
		System.Reflection.Assembly assembly,
		DllImportSearchPath? searchPath
	)
	{
		if (libraryName != Library)
			return 0;

		var beside = Path.Combine(AppContext.BaseDirectory, FileName);
		if (NativeLibrary.TryLoad(beside, out var handle))
			return handle;

		// The runtimes folder, which the app's deps.json lists.
		return NativeLibrary.TryLoad(FileName, assembly, searchPath, out handle) ? handle : 0;
	}

	static Sdl()
	{
		NativeLibrary.SetDllImportResolver(typeof(Sdl).Assembly, Resolve);
	}

	// The buttons held, as the window got them from the system, and the pointer's position in the window, which `x` and
	// `y` point to, unless they are 0.
	[DllImport(Library)]
	public static extern uint SDL_GetMouseState(nint x, nint y);

	// The subsystems that are initialized among `flags`, or all of them for 0.
	[DllImport(Library)]
	public static extern uint SDL_WasInit(uint flags);
}
