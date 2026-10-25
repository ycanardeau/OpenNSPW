namespace OpenNspw.Tests;

// A directory that is deleted when disposed.
internal sealed class TemporaryDirectory(string path) : IDisposable
{
	public string Path { get; } = path;

	public void Dispose()
	{
		try
		{
			Directory.Delete(Path, recursive: true);
		}
		catch (IOException) { }
	}
}

// The original's data (Original/NSPW_NET), which whole games run on, and the single-player executable
// (Original/Nspw122), whose functions the single-player mode emulates.
internal static class GameData
{
	private static string? FindInOriginal(string folder, string file)
	{
		for (
			var directory = new DirectoryInfo(AppContext.BaseDirectory);
			directory is not null;
			directory = directory.Parent
		)
		{
			var candidate = System.IO.Path.Combine(directory.FullName, "Original", folder);
			if (File.Exists(System.IO.Path.Combine(candidate, file)))
			{
				return candidate;
			}
		}

		return null;
	}

	private static string? FindOriginal()
	{
		return FindInOriginal("NSPW_NET", "t3.bmp");
	}

	// Original/Nspw122, with NSPW_122.exe and its maps, sounds and sprite sheet.
	public static string FindSingleplayer()
	{
		return FindInOriginal("Nspw122", "NSPW_122.exe")
			?? throw new InvalidOperationException("Original/Nspw122 is not found.");
	}

	// Makes a new, empty directory case-sensitive on Windows, as directories are on Linux, so that the tests find the
	// game's file names that differ in case from its files (WAV\CLICK1.wav for wav/click1.wav) on every platform.
	// Directories created in it inherit it. Needs Windows 10 1803 or later with WSL; without it, nothing changes.
	private static void MakeCaseSensitive(string directory)
	{
		if (!OperatingSystem.IsWindows())
		{
			return;
		}

		try
		{
			using var fsutil = System.Diagnostics.Process.Start(
				new System.Diagnostics.ProcessStartInfo(
					"fsutil.exe",
					["file", "setCaseSensitiveInfo", directory, "enable"]
				)
				{
					RedirectStandardOutput = true,
					RedirectStandardError = true,
				}
			);
			fsutil?.WaitForExit();
		}
		catch (System.ComponentModel.Win32Exception) { }
	}

	private static void CopyDirectory(string source, string target)
	{
		Directory.CreateDirectory(target);
		foreach (var file in Directory.GetFiles(source))
		{
			File.Copy(file, System.IO.Path.Combine(target, System.IO.Path.GetFileName(file)));
		}

		foreach (
			var directory in Directory
				.GetDirectories(source)
				.Where(d =>
					System.IO.Path.GetFileName(d)
						is not ("Debug" or "Release" or "Backup" or "Saved")
				)
		)
		{
			CopyDirectory(
				directory,
				System.IO.Path.Combine(target, System.IO.Path.GetFileName(directory))
			);
		}
	}

	// A copy of the original's data in a new temporary directory, so that what a game writes (saved games, settings)
	// does not change the original or other tests. Its Saved directory is empty, whatever games have been saved in the
	// original's, so that the tests run the same everywhere.
	public static TemporaryDirectory CreateCopy()
	{
		var original =
			FindOriginal()
			?? throw new InvalidOperationException("Original/NSPW_NET is not found.");
		var directory = new TemporaryDirectory(
			System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"OpenNspw-{Guid.NewGuid():N}")
		);
		Directory.CreateDirectory(directory.Path);
		MakeCaseSensitive(directory.Path);
		CopyDirectory(original, directory.Path);
		Directory.CreateDirectory(System.IO.Path.Combine(directory.Path, "Saved"));
		return directory;
	}
}
