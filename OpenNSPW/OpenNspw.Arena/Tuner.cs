using System.Globalization;
using OpenNspw.AI;

namespace OpenNspw.Arena;

// The tuner (StrongAIResearch.md, step 3): a random search over the strong player's numbers (StrongParameters), with
// successive halving. The defaults and `candidates` random settings play each scenario on a few start dates; the best
// third goes on, to three times as many dates, the games already played counted again; and so on for `rounds` rounds.
// Every setting of a round plays the same dates, so that the setups' luck falls alike on all of them (common random
// numbers). A game scores 1 won, 0.5 drawn or unfinished, 0 lost, and, to break ties among few games, the HP the enemy's
// ships lost less the HP the player's carriers lost, a thousandth each.
internal static class Tuner
{
	// A parameter's range: the tuner draws it uniformly from Low to High, a whole number if it is one.
	private sealed record Range(string Name, double Low, double High, bool IsWhole);

	private static readonly Range[] Ranges =
	[
		new("KeepAwayMargin", 300, 2500, false),
		new("ApproachBand", 300, 2500, false),
		new("CarrierMemoryFrames", 1000, 6000, true),
		new("ScoutFrames", 300, 2000, true),
		new("ShadowDistance", 700, 990, false),
		new("MinStrike", 2, 6, true),
		new("FightersKeptBack", 0, 1, false),
		new("RetargetFrames", 100, 1000, true),
		new("SideOffset", 500, 3500, false),
		new("ApproachRadius", 1000, 3500, false),
		new("HoldOff", 1500, 5000, false),
		new("FanAngle", 10, 90, false),
		new("BingoMargin", 0, 15, false),
		new("AttackingBingoMargin", -15, 5, false),
		new("HomeFuelMargin", 5, 25, false),
		new("TargetReach", 0.6, 1.0, false),
		new("ScoutFraction", 0.25, 0.45, false),
	];

	private static double Score(ArenaResult result) =>
		(
			result.Outcome switch
			{
				Outcome.Won => 1,
				Outcome.Drawn or Outcome.Unfinished => 0.5,
				_ => 0,
			}
		) + ((result.Strikes.EnemyDamage - result.Strikes.CarrierDamage) / 1000.0);

	// A random setting, as --params takes it.
	private static string Draw(Random random) =>
		string.Join(
			",",
			Ranges.Select(r =>
			{
				var value = r.Low + (random.NextDouble() * (r.High - r.Low));
				return FormattableString.Invariant(
					$"{r.Name}={(r.IsWhole ? Math.Round(value) : Math.Round(value, 2))}"
				);
			})
		);

	// Plays the rounds, writes each round's settings by score, and every game to `outPath`, each setting's games as
	// player "strong#n", n its number in the first round (0, the defaults); returns the best setting.
	public static string Tune(
		GameData data,
		IReadOnlyList<int> scenarios,
		int firstDate,
		int dates,
		int candidates,
		int rounds,
		int seed,
		int maxFrames,
		FormationShape? formation,
		int parallel,
		string outPath
	)
	{
		var random = new Random(seed);
		var settings = new List<string> { "" };
		settings.AddRange(Enumerable.Range(0, candidates).Select(_ => Draw(random)));
		var alive = Enumerable.Range(0, settings.Count).ToList();
		var played = new Dictionary<(int Setting, int Scenario, int Date), ArenaResult>();
		for (var round = 0; round < rounds; round++)
		{
			var dateCount = dates * (int)Math.Pow(3, round);
			var games = (
				from n in alive
				from scenario in scenarios
				from date in Enumerable.Range(firstDate, dateCount)
				where !played.ContainsKey((n, scenario, date))
				select (n, scenario, date)
			).ToList();
			Console.WriteLine(
				$"Round {round + 1}: {alive.Count} settings, {dateCount} dates of {scenarios.Count} scenarios."
			);
			var results = GamePool.Play(
				[
					.. games.Select(g =>
						(Func<ArenaResult>)(
							() =>
							{
								var parameters = StrongParameters.Parse(settings[g.n]);
								var player = formation is not null
									? new StrongPlayer
									{
										Formation = formation,
										Parameters = parameters,
									}
									: new StrongPlayer { Parameters = parameters };
								return new ArenaGame(
									data,
									g.scenario,
									1_000_000_000 + (g.date * 7919),
									player,
									maxFrames
								).Play() with
								{
									Player = $"strong#{g.n}",
								};
							}
						)
					),
				],
				parallel
			);
			foreach (var result in results)
			{
				var n = int.Parse(result.Player["strong#".Length..], CultureInfo.InvariantCulture);
				played[(n, result.Scenario, (result.StartDate - 1_000_000_000) / 7919)] = result;
			}

			var scores = alive
				.Select(n =>
					(
						Setting: n,
						Score: played.Where(p => p.Key.Setting == n).Average(p => Score(p.Value)),
						Won: played.Count(p =>
							p.Key.Setting == n && p.Value.Outcome == Outcome.Won
						),
						Games: played.Count(p => p.Key.Setting == n)
					)
				)
				.OrderByDescending(s => s.Score)
				.ToList();
			Console.WriteLine();
			Console.WriteLine(
				$"| Round {round + 1} | Setting | Score | Won | Games | Parameters |"
			);
			Console.WriteLine("| --- | --- | --- | --- | --- | --- |");
			foreach (var s in scores)
				Console.WriteLine(
					FormattableString.Invariant(
						$"| | {s.Setting} | {s.Score:F3} | {s.Won} | {s.Games} | {(settings[s.Setting].Length == 0 ? "defaults" : settings[s.Setting])} |"
					)
				);
			Console.WriteLine();

			File.WriteAllLines(
				outPath,
				Benchmark.Lines(
					played
						.Values.OrderBy(r => r.Player)
						.ThenBy(r => r.Scenario)
						.ThenBy(r => r.StartDate)
				)
			);
			alive =
			[
				.. scores
					.Take(Math.Max(1, (int)Math.Ceiling(scores.Count / 3.0)))
					.Select(s => s.Setting),
			];
		}

		return settings[alive[0]];
	}
}
