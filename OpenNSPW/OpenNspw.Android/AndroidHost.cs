using Android.App;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using OpenNspw.App;

namespace OpenNspw.Android;

// The app on Android, in `activity`: played by touching the screen, and text written in Android's text box, as MonoGame's
// Android gives no text typed (GameWindow.TextInput). The app's folders are its own, in no file browser.
internal sealed class AndroidHost(Activity activity) : IAppHost
{
	private readonly Activity _activity = activity;

	public bool IsTouchScreen => true;

	// The screen's short side, which the layout is as high as, in inches: about 2.8 on a phone, 5 or more on a tablet.
	private float ShortSideInches
	{
		get
		{
			var metrics = _activity.Resources!.DisplayMetrics!;
			return Math.Min(
				metrics.WidthPixels / metrics.Xdpi,
				metrics.HeightPixels / metrics.Ydpi
			);
		}
	}

	// About 1.8 on a phone, and none on a tablet.
	public float BattleZoom => Math.Clamp(5 / ShortSideInches, 1, 2.5f);

	public void ListenToText(GameWindow window, Action<char> typed) { }

	// MonoGame's text box (KeyboardInput), one at a time: a field touched while one is shown is not edited. What is written
	// comes on the UI's thread, which runs the game's updates.
	public bool EditText(string title, string text, Action<string> written)
	{
		async Task ShowAsync()
		{
			if (await KeyboardInput.Show(title, "", text) is { } result)
				written(result);
		}

		if (!KeyboardInput.IsVisible)
			_ = ShowAsync();
		return true;
	}

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
