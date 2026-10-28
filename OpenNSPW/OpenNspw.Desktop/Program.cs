using OpenNspw.Desktop;

// OpenNspw.Desktop [--data <directory>] [--exe <NSPW_122.exe>] [--singleplayer <scenario> | --replay <trace.n122>]
//
// Plays the port, from its main menu. The game's data (t3.bmp, wav, Map, Scenario) is NSPW NET's, bundled in the app
// (BundledOriginal), or the original's NSPW_NET directory given with --data, such as Original/NSPW_NET of this
// repository. What the game writes, its saves (Saved) and the scenarios saved in it, goes to the player's folder beside
// the settings (DesktopPlatform).
//
// The single-player mode plays a scenario of NSPW 1.22 against the computer (docs/Singleplayer.md), whose functions run
// from the single-player executable: NSPW_122.exe, bundled in the app with the files of its directory, or the file given
// with --exe, such as Original/Nspw122/NSPW_122.exe of this repository. With --singleplayer, the game starts in that
// scenario, without the menus. With --replay, it starts in the scenario of a trace recorded from 1.22, and follows it,
// to watch it (docs/Verification.md).

static string? Option(string[] args, string name)
{
	var index = Array.IndexOf(args, name);
	return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

IOriginalFiles files = BundledOriginal.NspwNet;
if (Option(args, "--data") is { } dataDirectory)
{
	files = new OriginalDirectory(dataDirectory);
	if (!files.Contains("t3.bmp"))
	{
		Console.Error.WriteLine($"The game's data (t3.bmp) was not found in {dataDirectory}.");
		return 1;
	}
}

IOriginalFiles singleplayerFiles = BundledOriginal.Nspw122;
var executableName = "NSPW_122.exe";
if (Option(args, "--exe") is { } executable)
{
	if (!File.Exists(executable))
	{
		Console.Error.WriteLine($"The single-player executable was not found: {executable}.");
		return 1;
	}

	singleplayerFiles = new OriginalDirectory(Path.GetDirectoryName(Path.GetFullPath(executable))!);
	executableName = Path.GetFileName(executable);
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

	scenario = number;
}

using var game = new NspwGame(files, singleplayerFiles, executableName, scenario, replay);
game.Run();
return 0;
