using Android.App;
using Microsoft.Xna.Framework;
using OpenNspw.App;

namespace OpenNspw.Android;

// The app on Android, in `activity`: played by touching the screen. MonoGame's Android gives no text typed
// (GameWindow.TextInput), and the app's folders are its own, in no file browser.
internal sealed class AndroidHost(Activity activity) : IAppHost
{
	private readonly Activity _activity = activity;

	public bool IsTouchScreen => true;

	public void ListenToText(GameWindow window, Action<char> typed) { }

	// MonoGame's Game.Exit on Android only moves the app's task to the back, and the game draws nothing from then on, also
	// once the app is shown again: the activity is finished and the process ends instead.
	public void Quit(Game game)
	{
		_activity.FinishAndRemoveTask();
		Java.Lang.JavaSystem.Exit(0);
	}

	public void OpenFolder(string path)
	{
		Directory.CreateDirectory(path);
		Console.Error.WriteLine($"The folder {path} cannot be shown on Android.");
	}
}
