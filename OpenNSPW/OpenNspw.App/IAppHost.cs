using Microsoft.Xna.Framework;

namespace OpenNspw.App;

// What the app asks of the system it runs on beyond MonoGame, which each host gives in its own way (OpenNspw.Desktop,
// OpenNspw.Android).
internal interface IAppHost
{
	// Whether the player plays by touching the screen (TouchPointer), rather than with a mouse.
	bool IsTouchScreen { get; }

	// What the player points with in `window`: the screen's touches (TouchPointer), or the mouse.
	IPointer CreatePointer(GameWindow window);

	// How much the battlefield is magnified at first, until the player zooms it (Battlefield.Zoom): more on a small screen,
	// where the original's layout, as high as the screen, makes the units too small to touch.
	float BattleZoom { get; }

	// The window's pixels in a density-independent pixel (Android's dp), which a touch's sizes are measured in: how far a
	// finger moves before it drags (TouchPointer), and how far from a unit it takes it (Battlefield.TouchRadius).
	float Density { get; }

	// Passes each character typed in the window to `typed`, as the system composes them: MonoGame's GameWindow.TextInput
	// on a desktop, which MonoGame's Android does not have.
	void ListenToText(GameWindow window, Action<char> typed);

	// Lets the player write a field's `text` in the system's own text box, headed `title`, and passes what they wrote to
	// `written` once they confirm it: true where the system has one for a touch screen, as Android does; false where the
	// player types into the field itself, key by key (ListenToText).
	bool EditText(string title, string text, Action<string> written);

	// Closes the app, from its main menu, where no game is being played.
	void Quit(Game game);

	// Shows a folder of the app's, such as the replays', in the system's file browser, creating it if there is none.
	void OpenFolder(string path);
}
