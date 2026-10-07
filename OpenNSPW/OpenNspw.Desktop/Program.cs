using OpenNspw.Desktop;

// OpenNspw.Desktop [--data <directory>]
//
// Plays the port. The game's data (t3.bmp, wav, Map, Scenario, Saved) is in the original's NSPW_NET directory: by
// default Original/NSPW_NET of this repository, found from the program's directory.

static string? FindDataDirectory(string[] args)
{
	var index = Array.IndexOf(args, "--data");
	if (index >= 0 && index + 1 < args.Length)
	{
		return args[index + 1];
	}

	for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
	{
		var candidate = Path.Combine(directory.FullName, "Original", "NSPW_NET");
		if (File.Exists(Path.Combine(candidate, "t3.bmp")))
		{
			return candidate;
		}
	}

	return null;
}

var dataDirectory = FindDataDirectory(args);
if (dataDirectory is null)
{
	Console.Error.WriteLine("The game's data (t3.bmp) was not found. Pass its directory with --data <directory>.");
	return 1;
}

using var game = new NspwGame(dataDirectory);
game.Run();
return 0;
