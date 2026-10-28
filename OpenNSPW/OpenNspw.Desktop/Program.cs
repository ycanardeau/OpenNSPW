using OpenNspw.Desktop;

// OpenNspw.Desktop [--data <directory>] [--exe <NSPW_122.exe>] [--singleplayer <scenario> | --replay <trace.n122>]
//
// Plays the port, from its main menu. The game's data (t3.bmp, wav, Map, Scenario) is NSPW NET's, from Original/NSPW_NET
// of this repository, embedded in the app, with the sprite sheet t3.bmp put together from the sprites (SpriteSheet); or
// the directory given with --data, read from disk. What the game writes, such as its saves (Saved), is in the user's
// folder (UserFolder).
//
// The single-player mode plays a scenario of NSPW 1.22 against the computer (docs/Singleplayer.md), whose functions run
// from the single-player executable, with 1.22's maps, sounds and sprite sheet from its directory: Original/Nspw122 of
// this repository, embedded in the app, with the sheet tst_cg1.bmp put together from the sprites; or the NSPW_122.exe
// given with --exe, and its directory, read from disk. With
// --singleplayer, the game starts in that scenario, without the menus. With --replay, it starts in the scenario of a
// trace recorded from 1.22, and follows it, to watch it (docs/Verification.md).

static string? Option(string[] args, string name)
{
	var index = Array.IndexOf(args, name);
	return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

IOriginalFiles data = new WithSpriteSheet(
	new EmbeddedFiles("NSPW_NET"),
	"t3.bmp",
	() => SpriteSheet.NspwNet
);
if (Option(args, "--data") is { } dataDirectory)
{
	if (!File.Exists(OpenNspw.GameFiles.Resolve(dataDirectory, "t3.bmp")))
	{
		Console.Error.WriteLine($"The game's data (t3.bmp) was not found in {dataDirectory}.");
		return 1;
	}

	data = new DirectoryFiles(dataDirectory);
}

IOriginalFiles singleplayerFiles = new WithSpriteSheet(
	new EmbeddedFiles("Nspw122"),
	"tst_cg1.bmp",
	() => SpriteSheet.Nspw122
);
var singleplayerExecutableName = "NSPW_122.exe";
if (Option(args, "--exe") is { } executable)
{
	if (!File.Exists(executable))
	{
		Console.Error.WriteLine($"The single-player executable was not found: {executable}.");
		return 1;
	}

	singleplayerFiles = new DirectoryFiles(Path.GetDirectoryName(Path.GetFullPath(executable))!);
	singleplayerExecutableName = Path.GetFileName(executable);
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

using var game = new NspwGame(
	data,
	singleplayerFiles,
	singleplayerExecutableName,
	scenario,
	replay
);
game.Run();
return 0;
