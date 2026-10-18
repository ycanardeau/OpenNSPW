using OpenNspw.Desktop;

// OpenNspw.Desktop [--data <directory>] [--exe <NSPW_122.exe>] [--singleplayer <scenario>]
//
// Plays the port, from its main menu. The game's data (t3.bmp, wav, Map, Scenario, Saved) is in the original's NSPW_NET
// directory: by default Original/NSPW_NET of this repository, found from the program's directory.
//
// The single-player mode plays a scenario of NSPW 1.22 against the computer (docs/Singleplayer.md), whose functions run
// from the single-player executable: by default Original/Nspw122/NSPW_122.exe of this repository. With
// --singleplayer, the game starts in that scenario, without the menus.

static string? Option(string[] args, string name)
{
	var index = Array.IndexOf(args, name);
	return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

// The file or directory at the path under Original, in the first directory up from the program's that has it.
static string? FindInOriginal(params string[] path)
{
	for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
	{
		var candidate = Path.Combine([directory.FullName, "Original", .. path]);
		if (File.Exists(candidate))
		{
			return candidate;
		}
	}

	return null;
}

static string? FindDataDirectory(string[] args)
{
	return Option(args, "--data") ?? Path.GetDirectoryName(FindInOriginal("NSPW_NET", "t3.bmp"));
}

var dataDirectory = FindDataDirectory(args);
if (dataDirectory is null)
{
	Console.Error.WriteLine("The game's data (t3.bmp) was not found. Pass its directory with --data <directory>.");
	return 1;
}

var executable = Option(args, "--exe") ?? FindInOriginal("Nspw122", "NSPW_122.exe");
if (executable is not null && !File.Exists(executable))
{
	Console.Error.WriteLine($"The single-player executable was not found: {executable}.");
	return 1;
}

int? scenario = null;
if (Option(args, "--singleplayer") is { } scenarioText)
{
	if (!int.TryParse(scenarioText, out var number) || !OpenNspw.Singleplayer.Functions.Scenarios.Contains(number))
	{
		Console.Error.WriteLine($"Not a scenario of NSPW 1.22: {scenarioText}. The scenarios are {string.Join(", ", OpenNspw.Singleplayer.Functions.Scenarios)}.");
		return 1;
	}

	if (executable is null)
	{
		Console.Error.WriteLine("The single-player executable (NSPW_122.exe) was not found. Pass it with --exe <file>.");
		return 1;
	}

	scenario = number;
}

using var game = new NspwGame(dataDirectory, executable, scenario);
game.Run();
return 0;
