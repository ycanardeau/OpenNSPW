using OpenNspw.Desktop;

// OpenNspw.Desktop [--data <directory>] [--exe <NSPW_122.exe>] [--singleplayer <scenario> | --replay <trace.n122>]
//
// Plays the port, from its main menu. The game's data (wav, Map, Scenario, Saved) is in the original's NSPW_NET
// directory: by default Original/NSPW_NET of this repository, found from the program's directory. Its sprite sheet,
// t3.bmp, isn't needed: the app has the sprites cut from it (SpriteSheet).
//
// The single-player mode plays a scenario of NSPW 1.22 against the computer (docs/Singleplayer.md), whose functions run
// from the single-player executable: by default Original/Nspw122/NSPW_122.exe of this repository. With
// --singleplayer, the game starts in that scenario, without the menus. With --replay, it starts in the scenario of a
// trace recorded from 1.22, and follows it, to watch it (docs/Verification.md).

static string? Option(string[] args, string name)
{
	var index = Array.IndexOf(args, name);
	return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

// The file or directory at the path under Original, in the first directory up from the program's that has it.
static string? FindInOriginal(params string[] path)
{
	for (
		var directory = new DirectoryInfo(AppContext.BaseDirectory);
		directory is not null;
		directory = directory.Parent
	)
	{
		var candidate = Path.Combine([directory.FullName, "Original", .. path]);
		if (Path.Exists(candidate))
		{
			return candidate;
		}
	}

	return null;
}

static string? FindDataDirectory(string[] args)
{
	return Option(args, "--data") ?? FindInOriginal("NSPW_NET");
}

var dataDirectory = FindDataDirectory(args);
if (dataDirectory is null)
{
	Console.Error.WriteLine(
		"The game's data (Original/NSPW_NET) was not found. Pass its directory with --data <directory>."
	);
	return 1;
}

var executable = Option(args, "--exe") ?? FindInOriginal("Nspw122", "NSPW_122.exe");
if (executable is not null && !File.Exists(executable))
{
	Console.Error.WriteLine($"The single-player executable was not found: {executable}.");
	return 1;
}

OpenNspw.Trace122Replay? replay = null;
if (Option(args, "--replay") is { } tracePath)
{
	try
	{
		replay = new OpenNspw.Trace122Replay(File.OpenRead(tracePath));
	}
	catch (Exception e)
		when (e is IOException or UnauthorizedAccessException or InvalidDataException)
	{
		Console.Error.WriteLine($"The trace cannot be read: {e.Message}");
		return 1;
	}
}

var scenarioText = replay is not null ? replay.Scenario.ToString() : Option(args, "--singleplayer");
int? scenario = null;
if (scenarioText is not null)
{
	if (
		!int.TryParse(scenarioText, out var number)
		|| !OpenNspw.Singleplayer.Functions.Scenarios.Contains(number)
	)
	{
		Console.Error.WriteLine(
			$"Not a scenario of NSPW 1.22: {scenarioText}. The scenarios are {string.Join(", ", OpenNspw.Singleplayer.Functions.Scenarios)}."
		);
		return 1;
	}

	if (executable is null)
	{
		Console.Error.WriteLine(
			"The single-player executable (NSPW_122.exe) was not found. Pass it with --exe <file>."
		);
		return 1;
	}

	scenario = number;
}

using var game = new NspwGame(dataDirectory, executable, scenario, replay);
game.Run();
return 0;
