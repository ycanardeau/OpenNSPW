using System.Diagnostics;
using OpenNspw.App;

namespace OpenNspw.Desktop;

// The app on a desktop: folders open in the system's file browser.
internal sealed class DesktopHost : IAppHost
{
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
