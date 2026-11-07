using System.Diagnostics;
using Microsoft.Xna.Framework;
using OpenNspw.App;

namespace OpenNspw.Desktop;

// The app on a desktop: played with the mouse, and folders open in the system's file browser.
internal sealed class DesktopHost : IAppHost
{
	public bool IsTouchScreen => false;

	public float BattleZoom => 1;

	public void ListenToText(GameWindow window, Action<char> typed)
	{
		window.TextInput += (_, e) => typed(e.Character);
	}

	public bool EditText(string title, string text, Action<string> written) => false;

	public void Quit(Game game)
	{
		game.Exit();
	}

	public void OpenFolder(string path)
	{
		try
		{
			Directory.CreateDirectory(path);
			Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
		}
		catch (Exception e) when (e is IOException or System.ComponentModel.Win32Exception)
		{
			Console.Error.WriteLine($"The folder {path} cannot be opened: {e.Message}");
		}
	}
}
