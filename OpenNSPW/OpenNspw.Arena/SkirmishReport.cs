using System.Globalization;
using System.Text;

namespace OpenNspw.Arena;

// A skirmish played, as the arena writes it: the losses of each side, what they were worth, and the player's strikes.
internal sealed record SkirmishRow(
	string Skirmish,
	int Seed,
	string Player,
	int Frames,
	double OwnWorth,
	double EnemyWorth,
	int OwnHP,
	int EnemyHP,
	int OwnSunk,
	int EnemySunk,
	int OwnPlanes,
	int EnemyPlanes,
	string OwnKinds,
	string EnemyKinds,
	int Drops,
	int TurnedBack,
	int CarrierDrops,
	int CarrierHits
)
{
	// What the enemy lost more than the player, in worth.
	public double Net => EnemyWorth - OwnWorth;

	// Won when the enemy lost more worth than the player, lost when less, drawn when as much (both nothing, often).
	public Outcome Outcome =>
		Net > 0.01 ? Outcome.Won
		: Net < -0.01 ? Outcome.Lost
		: Outcome.Drawn;
}

// The skirmishes' results (docs/StrongAI.md, Skirmishes): a line a game, and, for each skirmish and player, the games
// won, drawn and lost by worth, with the win rate's 95% Wilson interval, the losses of each side a game, the ratio of
// the worth the enemy lost to the player's, and, against the first player, the games better and worse by net worth on
// the same seeds, and the sign test's p.
internal static class SkirmishReport
{
	private static readonly string[] Columns =
	[
		"skirmish",
		"seed",
		"player",
		"frames",
		"own_worth",
		"enemy_worth",
		"own_hp",
		"enemy_hp",
		"own_sunk",
		"enemy_sunk",
		"own_planes",
		"enemy_planes",
		"own_kinds",
		"enemy_kinds",
		"drops",
		"turned_back",
		"carrier_drops",
		"carrier_hits",
	];

	public static SkirmishRow Row(ArenaResult result)
	{
		var losses = result.Losses!;
		return new SkirmishRow(
			result.Skirmish!,
			result.StartDate,
			result.Player,
			result.Frames,
			losses.Own.Worth,
			losses.Enemy.Worth,
			losses.Own.ShipHP,
			losses.Enemy.ShipHP,
			losses.Own.ShipsSunk,
			losses.Enemy.ShipsSunk,
			losses.Own.Planes,
			losses.Enemy.Planes,
			$"{SideLosses.Kinds(losses.Own.Sunk)} {SideLosses.Kinds(losses.Own.PlanesLost)}".Trim(),
			$"{SideLosses.Kinds(losses.Enemy.Sunk)} {SideLosses.Kinds(losses.Enemy.PlanesLost)}".Trim(),
			result.Strikes.Drops,
			result.Strikes.TurnedBack,
			result.Strikes.CarrierDrops,
			result.Strikes.CarrierHits
		);
	}

	public static IEnumerable<string> Lines(IEnumerable<SkirmishRow> rows) =>
		[
			string.Join(",", Columns),
			.. rows.Select(r =>
				FormattableString.Invariant(
					$"{r.Skirmish},{r.Seed},{r.Player},{r.Frames},{r.OwnWorth:F2},{r.EnemyWorth:F2},{r.OwnHP},{r.EnemyHP},{r.OwnSunk},{r.EnemySunk},{r.OwnPlanes},{r.EnemyPlanes},{r.OwnKinds},{r.EnemyKinds},{r.Drops},{r.TurnedBack},{r.CarrierDrops},{r.CarrierHits}"
				)
			),
		];

	public static List<SkirmishRow> Read(string path)
	{
		var lines = File.ReadAllLines(path);
		var header = lines[0].Split(',');
		return
		[
			.. lines
				.Skip(1)
				.Where(l => l.Length > 0)
				.Select(line =>
				{
					var cells = line.Split(',');
					string Cell(string name) => cells[Array.IndexOf(header, name)];
					int Number(string name) => int.Parse(Cell(name), CultureInfo.InvariantCulture);
					double Real(string name) =>
						double.Parse(Cell(name), CultureInfo.InvariantCulture);
					return new SkirmishRow(
						Cell("skirmish"),
						Number("seed"),
						Cell("player"),
						Number("frames"),
						Real("own_worth"),
						Real("enemy_worth"),
						Number("own_hp"),
						Number("enemy_hp"),
						Number("own_sunk"),
						Number("enemy_sunk"),
						Number("own_planes"),
						Number("enemy_planes"),
						Cell("own_kinds"),
						Cell("enemy_kinds"),
						Number("drops"),
						Number("turned_back"),
						Number("carrier_drops"),
						Number("carrier_hits")
					);
				}),
		];
	}

	private static string Average(IEnumerable<SkirmishRow> rows, Func<SkirmishRow, double> value) =>
		FormattableString.Invariant($"{rows.Average(value):F2}");

	// The table, the players in the order they first appear, each against the first, `baseline` if named.
	public static string Summary(IReadOnlyList<SkirmishRow> rows, string? baseline = null)
	{
		var text = new StringBuilder();
		text.AppendLine(
			"| Skirmish | Player | Games | W | D | L | Won (95%) | Worth lost, ours | theirs | Ratio | HP lost, ours | theirs | Sunk, ours | theirs | Planes lost, ours | theirs | Hits/drops at carriers | Better | Worse | p |"
		);
		text.AppendLine(
			"| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |"
		);
		foreach (var skirmish in rows.Select(r => r.Skirmish).Distinct())
		{
			var own = rows.Where(r => r.Skirmish == skirmish).ToList();
			var players = own.Select(r => r.Player).Distinct().ToList();
			var first = baseline is not null && players.Contains(baseline) ? baseline : players[0];
			var bySeed = own.Where(r => r.Player == first).ToDictionary(r => r.Seed);
			foreach (var player in players)
			{
				var games = own.Where(r => r.Player == player).ToList();
				var won = games.Count(r => r.Outcome == Outcome.Won);
				var (low, high) = Benchmark.Wilson(won, games.Count);
				var paired = games.Where(r => bySeed.ContainsKey(r.Seed)).ToList();
				var better = paired.Count(r => r.Net > bySeed[r.Seed].Net + 0.01);
				var worse = paired.Count(r => r.Net < bySeed[r.Seed].Net - 0.01);
				var ownWorth = games.Sum(r => r.OwnWorth);
				var drops = games.Sum(r => r.CarrierDrops);
				text.AppendLine(
					FormattableString.Invariant(
						$"| {skirmish} | {player} | {games.Count} | {won} | {games.Count(r => r.Outcome == Outcome.Drawn)} | {games.Count(r => r.Outcome == Outcome.Lost)} | {(double)won / games.Count * 100:F0}% ({low * 100:F0}–{high * 100:F0}%) | {Average(games, r => r.OwnWorth)} | {Average(games, r => r.EnemyWorth)} | {(ownWorth == 0 ? "∞" : $"{games.Sum(r => r.EnemyWorth) / ownWorth:F2}")} | {Average(games, r => r.OwnHP)} | {Average(games, r => r.EnemyHP)} | {Average(games, r => r.OwnSunk)} | {Average(games, r => r.EnemySunk)} | {Average(games, r => r.OwnPlanes)} | {Average(games, r => r.EnemyPlanes)} | {(drops == 0 ? "–" : $"{games.Sum(r => r.CarrierHits)}/{drops}")} | {(player == first ? "" : $"{better}")} | {(player == first ? "" : $"{worse}")} | {(player == first ? "" : $"{Benchmark.SignTest(better, worse):F3}")} |"
					)
				);
			}
		}

		return text.ToString();
	}
}
