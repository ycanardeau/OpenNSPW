using System.Reflection;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Window;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using OpenNspw.App;

namespace OpenNspw.Android;

// MonoGame's keyboard on Android, which takes keys only from a keyboard that has a vendor and a product
// (MonoGameAndroidGameView.IsKeyboard), and drops any other key event given to the game's view, such as one the app makes:
// its own Keyboard.KeyDown and KeyUp, internal, which the view calls for such a keyboard's keys, hold a key and let it go.
internal static class MonoGameKeyboard
{
	private const BindingFlags Internal = BindingFlags.NonPublic | BindingFlags.Static;

	private static readonly Func<Keycode, bool> s_keyDown = typeof(Keyboard)
		.GetMethod("KeyDown", Internal, [typeof(Keycode)])!
		.CreateDelegate<Func<Keycode, bool>>();

	private static readonly Func<Keycode, bool> s_keyUp = typeof(Keyboard)
		.GetMethod("KeyUp", Internal, [typeof(Keycode)])!
		.CreateDelegate<Func<Keycode, bool>>();

	public static void KeyDown(Keycode key) => s_keyDown(key);

	public static void KeyUp(Keycode key) => s_keyUp(key);
}

// Calls `onBack` when the user goes back: Back, or the gesture, from Android 13 on.
internal sealed class BackCallback(Action onBack) : Java.Lang.Object, IOnBackInvokedCallback
{
	private readonly Action _onBack = onBack;

	public void OnBackInvoked() => _onBack();
}

// The app's one activity: the game's view, the whole screen in landscape, as the desktop layout is wider than high
// (all_head.SCRN_WIDTH by SCRN_HEIGHT, scaled by the viewport). It keeps running through changes of the screen, which the
// viewport follows, rather than being made again.
[Activity(
	Label = NspwGame.Title,
	MainLauncher = true,
	AlwaysRetainTaskState = true,
	LaunchMode = LaunchMode.SingleInstance,
	ScreenOrientation = ScreenOrientation.SensorLandscape,
	ConfigurationChanges = ConfigChanges.Orientation
		| ConfigChanges.Keyboard
		| ConfigChanges.KeyboardHidden
		| ConfigChanges.ScreenSize
		| ConfigChanges.ScreenLayout
		| ConfigChanges.SmallestScreenSize
		| ConfigChanges.UiMode
)]
public sealed class MainActivity : AndroidGameActivity
{
	// How long Escape is held when Back is pressed: a press of Back is often a down and an up at once (the gesture, or
	// adb), which the UI and the game, reading the keyboard once each update, would not see.
	private const int EscapeMilliseconds = 100;

	private readonly Handler _handler = new(Looper.MainLooper!);
	private NspwGame _game = default!;

	// The status and navigation bars hidden, and shown over the game while the user swipes from an edge.
	private void HideSystemBars()
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			// From Android 15 on, the app draws under the bars anyway.
			if (!OperatingSystem.IsAndroidVersionAtLeast(35))
				Window!.SetDecorFitsSystemWindows(false);
			if (Window!.InsetsController is { } controller)
			{
				controller.Hide(WindowInsets.Type.SystemBars());
				controller.SystemBarsBehavior = (int)
					WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
			}
		}
		else
		{
#pragma warning disable CA1422 // The flags are what Android before 11 has.
			Window!.DecorView.SystemUiFlags =
				SystemUiFlags.Fullscreen
				| SystemUiFlags.HideNavigation
				| SystemUiFlags.ImmersiveSticky
				| SystemUiFlags.LayoutStable
				| SystemUiFlags.LayoutHideNavigation
				| SystemUiFlags.LayoutFullscreen;
#pragma warning restore CA1422
		}
	}

	// Back acts as Escape, as the UI already handles it: it goes back from the menus, and in a game, asks whether to close it
	// (GameScreen).
	private void PressEscape()
	{
		MonoGameKeyboard.KeyDown(Keycode.Escape);
		_handler.PostDelayed(() => MonoGameKeyboard.KeyUp(Keycode.Escape), EscapeMilliseconds);
	}

	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);
		_game = new NspwGame(new AndroidHost(this), new GameData(null));
		SetContentView((View)_game.Services.GetService(typeof(View))!);
		HideSystemBars();
		if (OperatingSystem.IsAndroidVersionAtLeast(33))
			OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(
				IOnBackInvokedDispatcher.PriorityDefault,
				new BackCallback(PressEscape)
			);
		_game.Run();
	}

	// Back before Android 13, as a key: pressed when it is let go.
	public override bool DispatchKeyEvent(KeyEvent? e)
	{
		if (e?.KeyCode != Keycode.Back)
			return base.DispatchKeyEvent(e);
		if (e.Action == KeyEventActions.Up)
			PressEscape();
		return true;
	}

	public override void OnWindowFocusChanged(bool hasFocus)
	{
		base.OnWindowFocusChanged(hasFocus);
		if (hasFocus)
			HideSystemBars();
	}
}
