using OpenNspw.Desktop;

// OpenNspw.Desktop [--data <directory>] [--singleplayer <scenario> | --replay <trace.n122 | replay.onspwr>]
//     [--ai [--formation line|ring:400|screen:400|auto:400] [--params Name=value,Name=value]] [--start-date <seconds>]
//
// Plays the port, from its main menu, with the original's files that the app embeds, and what the game writes in the
// user's folder (GameData, docs/BundledData.md). With --data, the game's data (wav, Map, Scenario, Saved) is that
// directory instead, such as the original's NSPW_NET, read and written.
//
// The single-player mode plays a scenario of NSPW 1.22 against the computer (docs/Singleplayer.md), whose functions are
// rewritten as C#, with 1.22's maps and sounds that the app embeds. With --singleplayer, the game starts in that
// scenario, without the menus. With --replay, it starts in the scenario of a trace recorded from 1.22, and follows it,
// to watch it (docs/Verification.md); or it plays a replay of a battle of the app, to watch it (docs/Replays.md). Each
// battle played is recorded in the user's replays, which the main menu lists. With --ai, the strong player plays the
// player's side of the single-player games, to watch it (docs/StrongAI.md), with its carriers formed as --formation
// says and the numbers of its tactics that --params sets (StrongParameters), as the arena's.

static string? Option(string[] args, string name)
{
	var index = Array.IndexOf(args, name);
	return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

var dataDirectory = Option(args, "--data");
if (dataDirectory is not null && !Directory.Exists(dataDirectory))
{
	Console.Error.WriteLine($"The game's data was not found: {dataDirectory}.");
	return 1;
}

OpenNspw.Verification.Trace122Replay? replay = null;
OpenNspw.Desktop.ReplayWatch? watch = null;
if (Option(args, "--replay") is { } tracePath)
{
	try
	{
		using (var file = File.OpenRead(tracePath))
		{
			if (OpenNspw.Replay.ReplayFormat.IsReplay(file))
			{
				watch = OpenNspw.Desktop.ReplayWatch.Open(tracePath);
				if (watch is null)
					return 1;
			}
		}

		if (watch is null)
			replay = new OpenNspw.Verification.Trace122Replay(File.OpenRead(tracePath));
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

var formation = Option(args, "--formation") is { } formationText
	? OpenNspw.AI.FormationShape.Parse(formationText)
	: null;
var parameters = OpenNspw.AI.StrongParameters.Parse(Option(args, "--params") ?? "");
if (Option(args, "--start-date") is { } startDate)
	DesktopPlatform.FixedTime = int.Parse(
		startDate,
		System.Globalization.CultureInfo.InvariantCulture
	);
Func<OpenNspw.AI.IPlayer>? computerPlayer =
	!args.Contains("--ai") ? null
	: formation is not null
		? () => new OpenNspw.AI.StrongPlayer { Formation = formation, Parameters = parameters }
	: () => new OpenNspw.AI.StrongPlayer { Parameters = parameters };
using var game = new NspwGame(new GameData(dataDirectory), scenario, replay, watch, computerPlayer);
game.Run();
return 0;
