using Microsoft.Xna.Framework;

namespace OpenNspw.App;

// What the app asks of the system it runs on beyond MonoGame, which each host gives in its own way (OpenNspw.Desktop,
// OpenNspw.Android).
internal interface IAppHost
{
	// Whether the player plays by touching the screen (TouchPointer), rather than with a mouse (MousePointer).
	bool IsTouchScreen { get; }

	// Passes each character typed in the window to `typed`, as the system composes them: MonoGame's GameWindow.TextInput
	// on a desktop, which MonoGame's Android does not have.
	void ListenToText(GameWindow window, Action<char> typed);

	// Closes the app, from its main menu, where no game is being played.
	void Quit(Game game);

	// Shows a folder of the app's, such as the replays', in the system's file browser, creating it if there is none.
	void OpenFolder(string path);
}
