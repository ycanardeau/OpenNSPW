namespace OpenNspw.Arena;

// The original's data (Original/NSPW_NET) and NSPW 1.22's directory (Original/Nspw122), found above the arena's folder.
internal sealed record GameData(string Directory, string SingleplayerDirectory)
{
	private static string FindInOriginal(string folder, string file)
	{
		for (
			var directory = new DirectoryInfo(AppContext.BaseDirectory);
			directory is not null;
			directory = directory.Parent
		)
		{
			var candidate = Path.Combine(directory.FullName, "Original", folder);
			if (File.Exists(Path.Combine(candidate, file)))
			{
				return candidate;
			}
		}

		throw new InvalidOperationException($"Original/{folder} is not found.");
	}

	public static GameData Find()
	{
		return new GameData(
			FindInOriginal("NSPW_NET", "t3.bmp"),
			FindInOriginal("Nspw122", "tst_cg1.bmp")
		);
	}
}
