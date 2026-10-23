using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using OpenNspw;
using OpenNspw.AI;
using OpenNspw.Arena;

// The arena (docs/StrongAI.md): plays single-player scenarios against NSPW 1.22's computer, from several start dates,
// with each player on the player's side, and writes the table of their results.
//
//   dotnet run -c Release --project OpenNspw.Arena -- [--scenarios 4,5,6|all] [--dates 3] [--first-date 0] [--players idle,rush]
//       [--parallel <cores>] [--out results.csv] [--frames <limit>] [--verbose] [--sees-all]
//       [--trace <units> [--trace-from <frame>] [--trace-to <frame>] [--trace-every <frames>]] [--strikes]
//   dotnet run -c Release --project OpenNspw.Arena -- --describe [--scenarios 4,5,6|all]

static IPlayer CreatePlayer(string name, bool verbose, bool seesAll) =>
	name switch
	{
		"idle" => new IdlePlayer(),
		"rush" => new RushPlayer(),
		"strong" => new StrongPlayer
		{
			Log = verbose ? Console.WriteLine : null,
			SeesEverything = seesAll,
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

var scenarioOption = Option(args, "--scenarios", "all");
IReadOnlyList<int> scenarios =
	scenarioOption == "all"
		? OpenNspw.Singleplayer.Functions.Scenarios
		: [.. scenarioOption.Split(',').Select(s => int.Parse(s, CultureInfo.InvariantCulture))];
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
var trace = UnitTrace.FromOptions((name, defaultValue) => Option(args, name, defaultValue));

var data = GameData.Find();
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

var jobs = new ConcurrentQueue<(int Scenario, int StartDate, string Player)>(
	from scenario in scenarios
	from date in Enumerable.Range(firstDate, dateCount)
	from player in players
	select (scenario, 1_000_000_000 + (date * 7919), player)
);
var total = jobs.Count;
var results = new ConcurrentBag<ArenaResult>();
var started = DateTime.Now;

Console.WriteLine($"{total} games, {parallel} at a time.");
var workers = Enumerable
	.Range(0, Math.Min(parallel, total))
	.Select(i => new Thread(() =>
	{
		while (jobs.TryDequeue(out var job))
		{
			var result = new ArenaGame(
				data,
				job.Scenario,
				job.StartDate,
				CreatePlayer(job.Player, verbose, seesAll),
				maxFrames,
				trace,
				strikes
			).Play();
			results.Add(result);
			lock (results)
			{
				Console.WriteLine(
					$"[{results.Count}/{total} {DateTime.Now - started:hh\\:mm\\:ss}] scenario {result.Scenario} date {result.StartDate} {result.Player}: {result.Outcome} after {result.Frames} frames, {result.TimeLeft} left ({result.Elapsed.TotalSeconds:F0} s)"
				);
				if (result.Error is { } error)
					Console.WriteLine(error);
			}
		}
	})
	{
		Name = $"Arena {i}",
		IsBackground = true,
	})
	.ToList();

foreach (var worker in workers)
{
	worker.Start();
}

foreach (var worker in workers)
{
	worker.Join();
}

var all = results.OrderBy(r => r.Scenario).ThenBy(r => r.Player).ThenBy(r => r.StartDate).ToList();
File.WriteAllLines(
	outPath,
	[
		"scenario,date,player,side,outcome,frames,time_left,seconds",
		.. all.Select(r =>
			FormattableString.Invariant(
				$"{r.Scenario},{r.StartDate},{r.Player},{r.Side},{r.Outcome},{r.Frames},{r.TimeLeft},{r.Elapsed.TotalSeconds:F1}"
			)
		),
	]
);
Console.WriteLine();
Console.WriteLine(Table(all, scenarios, players));
