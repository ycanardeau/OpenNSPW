using System.Globalization;
using System.Text;
using OpenNspw;
using OpenNspw.AI;
using OpenNspw.Arena;

// The arena (docs/StrongAI.md): plays single-player scenarios against NSPW 1.22's computer, from several start dates,
// with each player on the player's side, and writes the table of their results.
//
//   dotnet run -c Release --project OpenNspw.Arena -- [--scenarios 4,5,6|carriers|all] [--dates 3] [--first-date 0] [--players idle,rush]
//       [--parallel <cores>] [--out results.csv] [--frames <limit>] [--verbose] [--sees-all] [--formation line|ring:400|screen:400|auto:400] [--speed 1]
//       [--params Name=value,Name=value]
//   dotnet run -c Release --project OpenNspw.Arena -- --tune <candidates> [--tune-rounds 3] [--tune-seed 1] [--scenarios ...]
//       [--first-date 100] [--dates 2] [--frames <limit>] [--formation ...] [--out tune.csv]
//       [--trace <units> [--trace-from <frame>] [--trace-to <frame>] [--trace-every <frames>]] [--strikes]
//       [--torpedoes <torpedoes.csv>]
//   dotnet run -c Release --project OpenNspw.Arena -- --describe [--scenarios 4,5,6|all]
//   dotnet run -c Release --project OpenNspw.Arena -- --compare <before.csv> <after.csv>

static IPlayer CreatePlayer(
	string name,
	bool verbose,
	bool seesAll,
	FormationShape? formation,
	StrongParameters parameters
) =>
	name switch
	{
		"idle" => new IdlePlayer(),
		"rush" => new RushPlayer(),
		"strong" when formation is not null => new StrongPlayer
		{
			Log = verbose ? Console.WriteLine : null,
			SeesEverything = seesAll,
			Formation = formation,
			Parameters = parameters,
		},
		"strong" => new StrongPlayer
		{
			Log = verbose ? Console.WriteLine : null,
			SeesEverything = seesAll,
			Parameters = parameters,
		},
		_ => throw new ArgumentException($"No player is named {name}."),
	};

static string Option(string[] args, string name, string defaultValue)
{
	var index = Array.IndexOf(args, name);
	return index >= 0 && index + 1 < args.Length ? args[index + 1] : defaultValue;
}

static string Letter(Outcome outcome) =>
	outcome switch
	{
		Outcome.Won => "W",
		Outcome.Drawn => "D",
		Outcome.Lost => "L",
		Outcome.Unfinished => "U",
		_ => "F",
	};

// A row per scenario and a column per player, each the outcomes of its start dates in order, and the totals.
static string Table(
	IReadOnlyList<ArenaResult> results,
	IReadOnlyList<int> scenarios,
	IReadOnlyList<string> players
)
{
	var text = new StringBuilder();
	text.AppendLine($"| Scenario | Side | {string.Join(" | ", players)} |");
	text.AppendLine($"| --- | --- |{string.Concat(players.Select(_ => " --- |"))}");
	foreach (var scenario in scenarios)
	{
		var side = results.FirstOrDefault(r => r.Scenario == scenario)?.Side;
		var cells = players.Select(p =>
			string.Concat(
				results
					.Where(r => r.Scenario == scenario && r.Player == p)
					.OrderBy(r => r.StartDate)
					.Select(r => Letter(r.Outcome))
			)
		);
		text.AppendLine(
			$"| {scenario} | {(side == Side.Japan ? "J" : "U")} | {string.Join(" | ", cells)} |"
		);
	}

	var totals = players.Select(p =>
	{
		var own = results.Where(r => r.Player == p).ToList();
		return $"{own.Count(r => r.Outcome == Outcome.Won)}/{own.Count}";
	});
	text.AppendLine($"| Won | | {string.Join(" | ", totals)} |");
	return text.ToString();
}

var compareIndex = Array.IndexOf(args, "--compare");
if (compareIndex >= 0 && compareIndex + 2 < args.Length)
{
	Console.WriteLine(
		Benchmark.Compare(
			Benchmark.Read(args[compareIndex + 1]),
			Benchmark.Read(args[compareIndex + 2])
		)
	);
	return;
}

var scenarioOption = Option(args, "--scenarios", "all");
IReadOnlyList<int> scenarios = scenarioOption switch
{
	"all" => OpenNspw.Singleplayer.Functions.Scenarios,
	// The carrier battles: the scenarios whose victory counts the enemy's carriers.
	"carriers" => [4, 5, 6, 9, 105, 106, 201],
	_ => [.. scenarioOption.Split(',').Select(s => int.Parse(s, CultureInfo.InvariantCulture))],
};
var dateCount = int.Parse(Option(args, "--dates", "3"), CultureInfo.InvariantCulture);
var firstDate = int.Parse(Option(args, "--first-date", "0"), CultureInfo.InvariantCulture);
IReadOnlyList<string> players = Option(args, "--players", "idle,rush").Split(',');
var parallel = int.Parse(
	Option(args, "--parallel", $"{Environment.ProcessorCount}"),
	CultureInfo.InvariantCulture
);
var outPath = Option(args, "--out", "arena.csv");
var verbose = args.Contains("--verbose");
var seesAll = args.Contains("--sees-all");
var maxFrames = int.Parse(
	Option(args, "--frames", $"{int.MaxValue}"),
	CultureInfo.InvariantCulture
);
var strikes = args.Contains("--strikes");
var speed = short.Parse(Option(args, "--speed", "1"), CultureInfo.InvariantCulture);
var parameters = StrongParameters.Parse(Option(args, "--params", ""));
var formationOption = Option(args, "--formation", "");
FormationShape? formation =
	formationOption.Length == 0 ? null : FormationShape.Parse(formationOption);
var trace = UnitTrace.FromOptions((name, defaultValue) => Option(args, name, defaultValue));

var data = GameData.Find();
if (Option(args, "--fork-check", "") is { Length: > 0 } forkText)
{
	foreach (var scenario in scenarios)
	{
		var checkedGame = new ArenaGame(
			data,
			scenario,
			1_000_000_000 + (firstDate * 7919),
			CreatePlayer(players[0], verbose, seesAll, formation, parameters),
			forkCheckAt: int.Parse(forkText, CultureInfo.InvariantCulture)
		);
		var result = checkedGame.Play();
		if (!checkedGame.HasForkChecked && result.Error is null)
			Console.WriteLine($"FORK the same until the game ended, at frame {result.Frames}");
		if (result.Error is { } error)
			Console.WriteLine($"FORK check failed: {error}");
	}

	return;
}

if (args.Contains("--describe"))
{
	foreach (var scenario in scenarios)
	{
		var game = new ArenaGame(data, scenario, 1_000_000_000, new IdlePlayer(), maxFrames: 1);
		game.Play();
		Console.WriteLine(ScenarioDescription.Describe(game.Game));
	}

	return;
}

var tuneIndex = Array.IndexOf(args, "--tune");
if (tuneIndex >= 0 && tuneIndex + 1 < args.Length)
{
	var best = Tuner.Tune(
		data,
		scenarios,
		firstDate,
		dateCount,
		int.Parse(args[tuneIndex + 1], CultureInfo.InvariantCulture),
		int.Parse(Option(args, "--tune-rounds", "3"), CultureInfo.InvariantCulture),
		int.Parse(Option(args, "--tune-seed", "1"), CultureInfo.InvariantCulture),
		maxFrames,
		formation,
		parallel,
		outPath
	);
	Console.WriteLine($"Best: {(best.Length == 0 ? "the defaults" : $"--params {best}")}");
	return;
}

var jobs =
	from scenario in scenarios
	from date in Enumerable.Range(firstDate, dateCount)
	from player in players
	select (Scenario: scenario, StartDate: 1_000_000_000 + (date * 7919), Player: player);
var results = GamePool.Play(
	[
		.. jobs.Select(job =>
			(Func<ArenaResult>)(
				() =>
					new ArenaGame(
						data,
						job.Scenario,
						job.StartDate,
						CreatePlayer(job.Player, verbose, seesAll, formation, parameters),
						maxFrames,
						trace,
						strikes,
						speed
					).Play()
			)
		),
	],
	parallel
);

var all = results.OrderBy(r => r.Scenario).ThenBy(r => r.Player).ThenBy(r => r.StartDate).ToList();
File.WriteAllLines(outPath, Benchmark.Lines(all));
if (Option(args, "--torpedoes", "") is { Length: > 0 } torpedoesPath)
{
	File.WriteAllLines(
		torpedoesPath,
		[
			$"scenario,date,{TorpedoLog.Header}",
			.. all.SelectMany(r =>
				(r.Torpedoes ?? []).Select(row => $"{r.Scenario},{r.StartDate},{row}")
			),
		]
	);
}
Console.WriteLine();
Console.WriteLine(Table(all, scenarios, players));
Console.WriteLine(Benchmark.Summary(all));
