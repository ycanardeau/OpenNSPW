using System.Globalization;
using System.Text;

namespace OpenNspw.Arena;

// The benchmark (docs/StrongAI.md, Measuring): what many games of each scenario say of a player, with the margin their
// number leaves, and whether one run of the arena does better than another on the same games. A game swings on a
// degree of one strike's approach, so a few games say little: a change is better only if it wins more of the same
// games than chance would.
internal static class Benchmark
{
	// The z of a 95% interval.
	private const double Z = 1.96;

	private static readonly string[] Columns =
	[
		"scenario",
		"date",
		"player",
		"side",
		"outcome",
		"frames",
		"time_left",
		"seconds",
		"drops",
		"turned_back",
		"out_of_fuel",
		"planes_lost",
		"enemy_damage",
		"enemy_carriers_sunk",
		"own_carriers_sunk",
		"carrier_damage",
		"carrier_drops",
		"carrier_hits",
		"planes_dry",
		"fighters_dry",
		"stuck_runs",
		"stuck_over_land",
		"enemy_strikers_downed",
		"enemy_strikers_turned_back",
		"own_ships_sunk",
		"enemy_ships_sunk",
		"own_planes_lost",
		"enemy_planes_lost",
		"own_bases_lost",
		"enemy_bases_lost",
	];

	// The 95% Wilson interval of a rate of `successes` in `count`, as fractions, which holds for few games and for rates
	// near 0 or 1, as the normal one does not.
	private static (double Low, double High) Wilson(int successes, int count)
	{
		if (count == 0)
			return (0, 1);

		var rate = (double)successes / count;
		var denominator = 1 + (Z * Z / count);
		var middle = (rate + (Z * Z / (2 * count))) / denominator;
		var half =
			Z
			* Math.Sqrt((rate * (1 - rate) / count) + (Z * Z / (4.0 * count * count)))
			/ denominator;
		return (Math.Max(0, middle - half), Math.Min(1, middle + half));
	}

	// The logarithm of n choose k.
	private static double LogChoose(int n, int k)
	{
		var sum = 0.0;
		for (var i = 1; i <= k; i++)
			sum += Math.Log(n - k + i) - Math.Log(i);
		return sum;
	}

	// The two-sided p of the exact sign test: how likely `better` and `worse` games, or a split further from even, are
	// if the change makes no difference, each game then going either way with a chance of one half.
	private static double SignTest(int better, int worse)
	{
		var count = better + worse;
		if (count == 0)
			return 1;

		var tail = 0.0;
		for (var k = 0; k <= Math.Min(better, worse); k++)
			tail += Math.Exp(LogChoose(count, k) - (count * Math.Log(2)));
		return Math.Min(1, 2 * tail);
	}

	// How good an outcome is for the player: a win 2, a draw or an unfinished game 1, a loss or a failed game 0.
	private static int Rank(Outcome outcome) =>
		outcome switch
		{
			Outcome.Won => 2,
			Outcome.Drawn or Outcome.Unfinished => 1,
			_ => 0,
		};

	// How a game went for the player, finer than its outcome: its rank, then, between games of the same rank, the enemy's
	// carriers sunk less its own, then the enemy's ships sunk less its own, then the enemy's planes lost less its own.
	// Most carrier battles are drawn at the time limit, and 105's and 106's are too long to play to their end, so the
	// battle's losses tell those games apart where the outcome cannot.
	private static (int Rank, int Carriers, int Ships, int Planes) Standing(ArenaResult result) =>
		(
			Rank(result.Outcome),
			result.Strikes.EnemyCarriersSunk - result.Strikes.OwnCarriersSunk,
			result.Strikes.EnemyShipsSunk - result.Strikes.OwnShipsSunk,
			result.Strikes.EnemyPlanesLost - result.Strikes.OwnPlanesLost
		);

	private static int CompareStanding(ArenaResult a, ArenaResult b) =>
		Standing(a).CompareTo(Standing(b));

	private static string Percent(double fraction) =>
		FormattableString.Invariant($"{fraction * 100:F0}%");

	private static string PerGame(IEnumerable<ArenaResult> games, Func<StrikeCounts, int> count) =>
		FormattableString.Invariant($"{games.Average(r => count(r.Strikes)):F1}");

	// The enemy's losses and the player's, a game: "theirs–ours".
	private static string Losses(
		IEnumerable<ArenaResult> games,
		Func<StrikeCounts, int> theirs,
		Func<StrikeCounts, int> ours
	) => $"{PerGame(games, theirs)}–{PerGame(games, ours)}";

	// The hits on enemy carriers for each weapon dropped at one, of all the games.
	private static string HitRate(IEnumerable<ArenaResult> games)
	{
		var drops = games.Sum(r => r.Strikes.CarrierDrops);
		return drops == 0
			? "-"
			: FormattableString.Invariant(
				$"{games.Sum(r => r.Strikes.CarrierHits)}/{drops} ({(double)games.Sum(r => r.Strikes.CarrierHits) / drops * 100:F0}%)"
			);
	}

	// A row per scenario and player, and one for all: the games, how many were won, drawn and lost, the win rate with
	// its 95% interval, and, per game, what became of the strikes (StrikeCounts).
	public static string Summary(IReadOnlyList<ArenaResult> results)
	{
		var text = new StringBuilder();
		text.AppendLine(
			"| Scenario | Player | Games | W | D | L | Won (95%) | Drops | Turned back | Out of fuel | Planes lost | Lost dry | Enemy damage | Carriers sunk | Carriers lost | Carrier damage | Hits on carriers | Ships sunk–lost | Planes downed–lost | Bases taken–lost |"
		);
		text.AppendLine(
			"| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |"
		);
		var groups = results
			.GroupBy(r => (r.Scenario, r.Player))
			.OrderBy(g => g.Key.Scenario)
			.ThenBy(g => g.Key.Player)
			.Select(g => ($"{g.Key.Scenario}", g.Key.Player, g.ToList()))
			.Concat(
				results
					.GroupBy(r => r.Player)
					.OrderBy(g => g.Key)
					.Select(g => ("All", g.Key, g.ToList()))
			);
		foreach (var (scenario, player, games) in groups)
		{
			var won = games.Count(r => r.Outcome == Outcome.Won);
			var (low, high) = Wilson(won, games.Count);
			text.AppendLine(
				string.Join(
					" | ",
					$"| {scenario}",
					player,
					games.Count,
					won,
					games.Count(r => Rank(r.Outcome) == 1),
					games.Count(r => Rank(r.Outcome) == 0),
					$"{Percent((double)won / games.Count)} ({Percent(low)}–{Percent(high)})",
					PerGame(games, s => s.Drops),
					PerGame(games, s => s.TurnedBack),
					PerGame(games, s => s.OutOfFuel),
					PerGame(games, s => s.PlanesLost),
					PerGame(games, s => s.PlanesDry),
					PerGame(games, s => s.EnemyDamage),
					PerGame(games, s => s.EnemyCarriersSunk),
					PerGame(games, s => s.OwnCarriersSunk),
					PerGame(games, s => s.CarrierDamage),
					HitRate(games),
					Losses(games, s => s.EnemyShipsSunk, s => s.OwnShipsSunk),
					Losses(games, s => s.EnemyPlanesLost, s => s.OwnPlanesLost),
					$"{Losses(games, s => s.EnemyBasesLost, s => s.OwnBasesLost)} |"
				)
			);
		}

		return text.ToString();
	}

	// The arena's table of results, one line a game.
	public static IEnumerable<string> Lines(IEnumerable<ArenaResult> results) =>
		[
			string.Join(",", Columns),
			.. results.Select(r =>
				FormattableString.Invariant(
					$"{r.Scenario},{r.StartDate},{r.Player},{r.Side},{r.Outcome},{r.Frames},{r.TimeLeft},{r.Elapsed.TotalSeconds:F1},{r.Strikes.Drops},{r.Strikes.TurnedBack},{r.Strikes.OutOfFuel},{r.Strikes.PlanesLost},{r.Strikes.EnemyDamage},{r.Strikes.EnemyCarriersSunk},{r.Strikes.OwnCarriersSunk},{r.Strikes.CarrierDamage},{r.Strikes.CarrierDrops},{r.Strikes.CarrierHits},{r.Strikes.PlanesDry},{r.Strikes.FightersDry},{r.Strikes.StuckRuns},{r.Strikes.StuckOverLand},{r.Strikes.EnemyStrikersDowned},{r.Strikes.EnemyStrikersTurnedBack},{r.Strikes.OwnShipsSunk},{r.Strikes.EnemyShipsSunk},{r.Strikes.OwnPlanesLost},{r.Strikes.EnemyPlanesLost},{r.Strikes.OwnBasesLost},{r.Strikes.EnemyBasesLost}"
				)
			),
		];

	// The results of a table the arena wrote (Lines); one written before the strikes were counted reads them as 0.
	public static List<ArenaResult> Read(string path)
	{
		var lines = File.ReadAllLines(path);
		var header = lines[0].Split(',');
		var results = new List<ArenaResult>();
		foreach (var line in lines.Skip(1).Where(l => l.Length > 0))
		{
			var cells = line.Split(',');
			string Cell(string name) =>
				Array.IndexOf(header, name) is var i and >= 0 && i < cells.Length ? cells[i] : "0";
			int Number(string name) => int.Parse(Cell(name), CultureInfo.InvariantCulture);
			results.Add(
				new ArenaResult(
					Number("scenario"),
					Number("date"),
					Cell("player"),
					Enum.Parse<Outcome>(Cell("outcome")),
					Enum.Parse<Side>(Cell("side")),
					Number("frames"),
					Number("time_left"),
					TimeSpan.FromSeconds(
						double.Parse(Cell("seconds"), CultureInfo.InvariantCulture)
					),
					null,
					new StrikeCounts(
						Number("drops"),
						Number("turned_back"),
						Number("out_of_fuel"),
						Number("planes_lost"),
						Number("enemy_damage"),
						Number("enemy_carriers_sunk"),
						Number("own_carriers_sunk"),
						Number("carrier_damage"),
						Number("carrier_drops"),
						Number("carrier_hits"),
						Number("planes_dry"),
						Number("fighters_dry"),
						Number("stuck_runs"),
						Number("stuck_over_land"),
						Number("enemy_strikers_downed"),
						Number("enemy_strikers_turned_back"),
						Number("own_ships_sunk"),
						Number("enemy_ships_sunk"),
						Number("own_planes_lost"),
						Number("enemy_planes_lost"),
						Number("own_bases_lost"),
						Number("enemy_bases_lost")
					)
				)
			);
		}

		return results;
	}

	// A row per scenario and player that both runs played, and one for all, over the games of the same start dates: the
	// wins of each run, the games the second did better and worse in (a win over a draw or a loss, a draw over a loss),
	// and the sign test's p that so many more better or worse games come by chance: under 0.05, the change counts. Then the
	// same by standing (Standing), which also counts a draw or an unfinished game that went better in the battle's
	// losses as better; and the carriers, ships and planes the enemy lost less the player's, a game.
	public static string Compare(
		IReadOnlyList<ArenaResult> before,
		IReadOnlyList<ArenaResult> after
	)
	{
		var pairs = (
			from a in after
			join b in before
				on (a.Scenario, a.StartDate, a.Player) equals (b.Scenario, b.StartDate, b.Player)
			select (Before: b, After: a)
		).ToList();
		var text = new StringBuilder();
		text.AppendLine(
			"| Scenario | Player | Games | Won before | Won after | Better | Worse | p | Carriers before | Carriers after | Ships before | Ships after | Planes before | Planes after | Better by losses | Worse by losses | p |"
		);
		text.AppendLine(
			"| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |"
		);
		var groups = pairs
			.GroupBy(p => (p.After.Scenario, p.After.Player))
			.OrderBy(g => g.Key.Scenario)
			.ThenBy(g => g.Key.Player)
			.Select(g => ($"{g.Key.Scenario}", g.Key.Player, g.ToList()))
			.Concat(
				pairs
					.GroupBy(p => p.After.Player)
					.OrderBy(g => g.Key)
					.Select(g => ("All", g.Key, g.ToList()))
			);
		foreach (var (scenario, player, games) in groups)
		{
			var better = games.Count(p => Rank(p.After.Outcome) > Rank(p.Before.Outcome));
			var worse = games.Count(p => Rank(p.After.Outcome) < Rank(p.Before.Outcome));
			var ahead = games.Count(p => CompareStanding(p.After, p.Before) > 0);
			var behind = games.Count(p => CompareStanding(p.After, p.Before) < 0);
			text.AppendLine(
				FormattableString.Invariant(
					$"| {scenario} | {player} | {games.Count} | {games.Count(p => p.Before.Outcome == Outcome.Won)} | {games.Count(p => p.After.Outcome == Outcome.Won)} | {better} | {worse} | {SignTest(better, worse):F2} | {games.Average(p => Standing(p.Before).Carriers):F2} | {games.Average(p => Standing(p.After).Carriers):F2} | {games.Average(p => Standing(p.Before).Ships):F2} | {games.Average(p => Standing(p.After).Ships):F2} | {games.Average(p => Standing(p.Before).Planes):F1} | {games.Average(p => Standing(p.After).Planes):F1} | {ahead} | {behind} | {SignTest(ahead, behind):F2} |"
				)
			);
		}

		return text.ToString();
	}
}
