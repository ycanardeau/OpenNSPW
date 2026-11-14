using System.Runtime.InteropServices;

namespace Aigamo.Saruhashi.MonoGame;

/// <summary>
/// The system's clipboard through SDL 2, which MonoGame's DesktopGL runs on, for <see cref="WindowManager.Clipboard"/>:
/// text copied in the game is pasted in other apps, and the reverse. Where SDL's video is not running, as with another
/// MonoGame, such as Android's, the clipboard is the game's own, which only the game pastes from.
/// </summary>
/// <remarks>
/// Use it on the game's thread, once the game has started, as SDL's clipboard is used on the thread that runs its video.
/// </remarks>
public sealed unsafe class SdlClipboard : IClipboard
{
	// SDL_INIT_VIDEO.
	private const uint InitVideo = 0x00000020;

	// SDL as MonoGame's DesktopGL ships it, by the name it is loaded by: by that name, the system gives the library already
	// loaded, MonoGame's, whose video is running, rather than another copy.
	private static readonly string[] s_libraryNames =
		OperatingSystem.IsWindows() ? ["SDL2.dll"]
		: OperatingSystem.IsMacOS() ? ["libSDL2-2.0.0.dylib", "libSDL2.dylib"]
		: ["libSDL2-2.0.so.0", "libSDL2.so"];

	private sealed class Sdl(nint library)
	{
		public delegate* unmanaged[Cdecl]<uint, uint> WasInit { get; } =
			(delegate* unmanaged[Cdecl]<uint, uint>)NativeLibrary.GetExport(library, "SDL_WasInit");

		public delegate* unmanaged[Cdecl]<byte*> GetClipboardText { get; } =
			(delegate* unmanaged[Cdecl]<byte*>)
				NativeLibrary.GetExport(library, "SDL_GetClipboardText");

		public delegate* unmanaged[Cdecl]<byte*, int> SetClipboardText { get; } =
			(delegate* unmanaged[Cdecl]<byte*, int>)
				NativeLibrary.GetExport(library, "SDL_SetClipboardText");

		public delegate* unmanaged[Cdecl]<void*, void> Free { get; } =
			(delegate* unmanaged[Cdecl]<void*, void>)NativeLibrary.GetExport(library, "SDL_free");
	}

	// The text copied, for where SDL's clipboard is not reached.
	private string _text = string.Empty;

	private Sdl? _sdl;
	private bool _loaded;

	// SDL, loaded the first time the clipboard is used, by when MonoGame has loaded it; null if it is not there.
	private static Sdl? Load()
	{
		foreach (var name in s_libraryNames)
		{
			if (NativeLibrary.TryLoad(name, out var library))
			{
				try
				{
					return new Sdl(library);
				}
				catch (EntryPointNotFoundException)
				{
					return null;
				}
			}
		}

		return null;
	}

	// SDL, while its video is running, which its clipboard needs.
	private Sdl? Running
	{
		get
		{
			if (!_loaded)
			{
				_sdl = Load();
				_loaded = true;
			}

			return _sdl is { } sdl && (sdl.WasInit(InitVideo) & InitVideo) != 0 ? sdl : null;
		}
	}

	public string GetText()
	{
		if (Running is not { } sdl)
			return _text;

		var text = sdl.GetClipboardText();
		if (text is null)
			return string.Empty;

		try
		{
			return Marshal.PtrToStringUTF8((nint)text) ?? string.Empty;
		}
		finally
		{
			sdl.Free(text);
		}
	}

	public void SetText(string text)
	{
		_text = text;
		if (Running is not { } sdl)
			return;

		var utf8 = Marshal.StringToCoTaskMemUTF8(text);
		try
		{
			sdl.SetClipboardText((byte*)utf8);
		}
		finally
		{
			Marshal.FreeCoTaskMem(utf8);
		}
	}
}
