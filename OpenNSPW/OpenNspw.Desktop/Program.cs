using OpenNspw.Desktop;

// OpenNspw.Desktop [--data <directory>] [--singleplayer <scenario> [--exe <NSPW_122.exe>]]
//
// Plays the port. The game's data (t3.bmp, wav, Map, Scenario, Saved) is in the original's NSPW_NET directory: by
// default Original/NSPW_NET of this repository, found from the program's directory.
//
// It starts with a menu: a scenario of NSPW 1.22 against the computer (docs/Singleplayer.md), or a network game. The
// single-player mode runs functions of the single-player executable: by default Original/Nspw122/NSPW_122.exe of this
// repository. --singleplayer starts that scenario at once.

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

// NSPW_122.exe, for the single-player mode. Without it, only network games can be played.
var executable = Option(args, "--exe") ?? FindInOriginal("Nspw122", "NSPW_122.exe");
var singleplayerExecutable = executable is not null && File.Exists(executable) ? File.ReadAllBytes(executable) : null;

int? scenario = null;
if (Option(args, "--singleplayer") is { } scenarioText)
{
	if (!int.TryParse(scenarioText, out var number) || !OpenNspw.Singleplayer.Functions.Scenarios.Contains(number))
	{
		Console.Error.WriteLine($"Not a scenario of NSPW 1.22: {scenarioText}. The scenarios are {string.Join(", ", OpenNspw.Singleplayer.Functions.Scenarios)}.");
		return 1;
	}

	if (singleplayerExecutable is null)
	{
		Console.Error.WriteLine("The single-player executable (NSPW_122.exe) was not found. Pass it with --exe <file>.");
		return 1;
	}

	scenario = number;
}

using var game = new NspwGame(dataDirectory, singleplayerExecutable, scenario);
game.Run();
return 0;
