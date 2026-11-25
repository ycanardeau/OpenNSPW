using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Aigamo.Otsuki.Actors;
using Aigamo.Otsuki.Tests;

// Runs the simulation's scenarios (Aigamo.Otsuki.Tests/SimulationScenarios.cs) on many seeds, each in conditions that
// its seed draws (SimulationConditions.Draw), on every core; prints each failure with the command that replays it.
//
// Usage:
//   swarm [--seeds N] [--first S] [--scenario NAME[,NAME...]] [--threads K] [--loops L]: with L, each peer has up to L
//       connection loops, as its seed draws
//   replay NAME SEED [--loops L] [--trace FILE]: the network's trace and each peer's events go to FILE if it fails
//   explore [NAME[,NAME...]] [--delays D] [--runs N]: every order of small cases from their marked point, within D delays
//   explore NAME --choices STEP:CHOICE,...: one order again, as a failed exploration prints it
//   list

static Dictionary<string, string> Parse(string[] args, int from)
{
	var options = new Dictionary<string, string>();
	for (var i = from; i < args.Length; i++)
	{
		if (!args[i].StartsWith("--", StringComparison.Ordinal))
			continue;

		var hasValue =
			i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal);
		options[args[i][2..]] = hasValue ? args[++i] : "true";
	}

	return options;
}

static int Int(Dictionary<string, string> options, string name, int fallback) =>
	options.TryGetValue(name, out var value)
		? int.Parse(value, CultureInfo.InvariantCulture)
		: fallback;

static string ReplayCommand(SimulationScenario scenario, int seed, int mostLoops) =>
	$"dotnet run --project Otsuki/Aigamo.Otsuki.Swarm -c Release -- replay {scenario.Name} {seed}{(mostLoops > 1 ? $" --loops {mostLoops}" : "")}";

// The first line of a failure, with its numbers taken out: failures that differ only in their seeds, times and
// identifiers share it.
static string Signature(Exception exception)
{
	var message = exception.Message.Split('\n')[0];
	message = Regex.Replace(message, @"\(seed [^)]*\)", "");
	message = Regex.Replace(message, @"[0-9A-Fa-f]{6,}|\d+(\.\d+)?", "#");
	return message.Length > 160 ? message[..160] : message;
}

static string Format(TimeSpan time) => time.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);

// Fails at once, as an exception in the run, instead of stopping the process.
Trace.Listeners.Clear();
Trace.Listeners.Add(new ThrowingTraceListener());

if (args.Length == 0)
{
	Console.Error.WriteLine("Usage: swarm | replay NAME SEED | list (see Program.cs).");
	return 2;
}

var options = Parse(args, 1);
var all = SimulationScenarios.All;
switch (args[0])
{
	case "list":
		foreach (var scenario in all)
			Console.WriteLine($"{scenario.Name}{(scenario.MayBeRough ? " (rough)" : "")}");
		return 0;

	case "explore":
	{
		var explorations =
			args.Length > 1 && !args[1].StartsWith("--", StringComparison.Ordinal)
				?
				[
					.. args[1]
						.Split(',')
						.Select(name => SimulationExplorations.All.Single(e => e.Name == name)),
				]
				: SimulationExplorations.All;
		if (options.TryGetValue("choices", out var choices))
		{
			SimulationExplorations.Replay(explorations[0], choices);
			Console.WriteLine("Passed.");
			return 0;
		}

		var maxDelays = Int(options, "delays", int.MaxValue);
		var maxRuns = Int(options, "runs", 1_000_000);
		var failed = false;
		foreach (var exploration in explorations)
		{
			var clock = Stopwatch.StartNew();
			try
			{
				var (runs, isExhausted, fromStep) = SimulationExplorations.Explore(
					exploration,
					maxRuns,
					maxDelays
				);
				Console.WriteLine(
					$"{exploration.Name}: {runs:N0} orders from step {fromStep}{(maxDelays == int.MaxValue ? "" : $" within {maxDelays} delays")}, {(isExhausted ? "every one" : "not all")}, in {Format(clock.Elapsed)}"
				);
			}
			catch (Exception exception)
			{
				failed = true;
				Console.WriteLine($"FAILED {exception.Message.Split('\n')[0]}");
			}
		}

		return failed ? 1 : 0;
	}

	case "replay":
	{
		var scenario = all.Single(s => s.Name == args[1]);
		var seed = int.Parse(args[2], CultureInfo.InvariantCulture);
		var trace = options.GetValueOrDefault("trace");
		Simulation? simulation = null;
		var conditions = SimulationConditions.Draw(
			seed,
			scenario.MayBeRough,
			Int(options, "loops", 1)
		) with
		{
			KeepsTrace = trace is not null,
			OnStart = started => simulation = started,
		};
		Console.WriteLine($"{scenario.Name} seed {seed}: {conditions}");
		try
		{
			scenario.Run(seed, conditions);
			Console.WriteLine("Passed.");
			return 0;
		}
		catch (Exception exception)
		{
			Console.WriteLine(exception);
			if (trace is not null && simulation is not null)
			{
				// The network's trace, then each peer's events, in full.
				File.WriteAllLines(
					trace,
					[
						.. simulation.Network.Trace,
						.. simulation.Peers.SelectMany(p =>
							p.Events.Select(e => $"{p.Address} {e}")
								.Prepend($"{p.Address} sees {p.View()}")
						),
					]
				);
				Console.WriteLine($"Trace and events in {trace}.");
			}

			return 1;
		}
	}

	case "swarm":
	{
		var seeds = Int(options, "seeds", 10_000);
		var first = Int(options, "first", 0);
		var threads = Int(options, "threads", Environment.ProcessorCount);
		var mostLoops = Int(options, "loops", 1);
		var scenarios = options.TryGetValue("scenario", out var names)
			? [.. names.Split(',').Select(name => all.Single(s => s.Name == name))]
			: all;
		var total = (long)seeds * scenarios.Count;
		Console.WriteLine(
			$"{seeds:N0} seeds from {first:N0} of {string.Join(", ", scenarios.Select(s => s.Name))}: {total:N0} runs on {threads} threads"
		);

		// Each worker takes every threads-th run and posts what it starts and how each ends; this thread alone keeps
		// the count, the failures and the report.
		var results = new Mailbox<SwarmResult>();
		for (var worker = 0; worker < threads; worker++)
		{
			var start = worker;
			new Thread(() =>
			{
				for (var index = (long)start; index < total; index += threads)
				{
					var scenario = scenarios[(int)(index % scenarios.Count)];
					var seed = first + (int)(index / scenarios.Count);
					var conditions = SimulationConditions.Draw(
						seed,
						scenario.MayBeRough,
						mostLoops
					);
					results.Post(new SwarmResult.Started(start, scenario.Name, seed));
					Exception? failure = null;
					try
					{
						scenario.Run(seed, conditions);
					}
					catch (Exception exception)
					{
						failure = exception;
					}

					results.Post(new SwarmResult.Ended(start, scenario, seed, conditions, failure));
				}
			})
			{
				IsBackground = true,
				Name = $"Swarm {worker}",
			}.Start();
		}

		var clock = Stopwatch.StartNew();
		var running = new Dictionary<int, (string Name, int Seed, TimeSpan Started)>();
		var failures = new List<(SimulationScenario Scenario, int Seed, string Signature)>();
		long done = 0;
		var nextReport = TimeSpan.FromSeconds(30);
		while (done < total)
		{
			if (results.WaitFor(_ => true, TimeSpan.FromSeconds(1)))
			{
				while (results.TryReceive(out var result))
				{
					switch (result)
					{
						case SwarmResult.Started started:
							running[started.Worker] = (started.Name, started.Seed, clock.Elapsed);
							break;
						case SwarmResult.Ended ended:
							running.Remove(ended.Worker);
							done++;
							if (ended.Failure is { } exception)
							{
								failures.Add((ended.Scenario, ended.Seed, Signature(exception)));
								var message = exception.Message.Split('\n')[0];
								Console.WriteLine(
									$"FAILED {ended.Scenario.Name} {ended.Seed} ({ended.Conditions}): {(message.Length > 600 ? message[..600] + "..." : message)}\n  {ReplayCommand(ended.Scenario, ended.Seed, mostLoops)}"
								);
							}
							break;
					}
				}
			}

			var elapsed = clock.Elapsed;
			if (elapsed < nextReport)
				continue;

			nextReport = elapsed + TimeSpan.FromSeconds(30);
			var left = done == 0 ? TimeSpan.Zero : elapsed * ((double)(total - done) / done);
			var slow = running
				.Values.Where(r => elapsed - r.Started > TimeSpan.FromSeconds(30))
				.Select(r => $"{r.Name} {r.Seed}")
				.ToList();
			Console.WriteLine(
				$"[{Format(elapsed)}] {done:N0}/{total:N0} ({100.0 * done / total:0.0}%), {done / elapsed.TotalSeconds:N0} runs/s, {failures.Count} failed, ETA {Format(left)}{(slow.Count > 0 ? $"; over 30 s: {string.Join(", ", slow)}" : "")}"
			);
		}

		Console.WriteLine(
			$"Done in {Format(clock.Elapsed)}: {total:N0} runs, {failures.Count} failed."
		);
		foreach (var byScenario in failures.GroupBy(f => f.Scenario.Name).OrderBy(g => g.Key))
		{
			Console.WriteLine(
				$"{byScenario.Key}: {byScenario.Count()} of {seeds:N0} seeds ({100.0 * byScenario.Count() / seeds:0.###}%)"
			);
			foreach (
				var bySignature in byScenario
					.GroupBy(f => f.Signature)
					.OrderByDescending(g => g.Count())
			)
				Console.WriteLine(
					$"  {bySignature.Count()} x {bySignature.Key}\n    seeds {string.Join(", ", bySignature.Select(f => f.Seed).Order().Take(10))}"
				);
		}

		return failures.Count == 0 ? 0 : 1;
	}

	default:
		Console.Error.WriteLine($"Unknown command {args[0]}.");
		return 2;
}

/// <summary>
/// What a worker of the swarm reports: a run it starts, and how it ended.
/// </summary>
internal abstract record SwarmResult(int Worker)
{
	public sealed record Started(int Worker, string Name, int Seed) : SwarmResult(Worker);

	public sealed record Ended(
		int Worker,
		SimulationScenario Scenario,
		int Seed,
		SimulationConditions Conditions,
		Exception? Failure
	) : SwarmResult(Worker);
}

/// <summary>
/// Turns a failed assertion of a debug build into an exception of the run that made it.
/// </summary>
internal sealed class ThrowingTraceListener : TraceListener
{
	public override void Fail(string? message, string? detailMessage) =>
		throw new InvalidOperationException($"Assertion failed: {message} {detailMessage}");

	public override void Write(string? message) { }

	public override void WriteLine(string? message) { }
}
