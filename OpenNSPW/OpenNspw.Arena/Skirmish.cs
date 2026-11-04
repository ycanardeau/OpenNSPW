using OpenNspw.AI;

namespace OpenNspw.Arena;

// A unit a skirmish places: the player's or the computer's, of a kind, where, heading which way, with the planes it
// carries (parked, armed as given), in a computer's fleet (0 for none), sailing or flying toward a point (null to stand),
// and, for a plane, flying from the start, at full speed, based on the unit placed `Deck` places before it.
internal sealed record Placement(
	bool IsPlayers,
	UnitKind Kind,
	WorldPosition Position,
	double Heading,
	IReadOnlyList<(UnitKind Kind, int Count, FireKind Weapon)> Planes,
	int Fleet = 0,
	WorldPosition? Toward = null,
	int Deck = 0
);

// A computer's fleet of a skirmish: its number, its mode (0 hold, 1 attack its targets), and the kinds of the player's
// units it targets, its first targets first.
internal sealed record SkirmishFleet(int Number, short Mode, IReadOnlyList<UnitKind> Targets);

// Where a skirmish's units start, from its seed: the units of both sides, the computer's fleets, and what the strong
// player knows of it, as a briefing would tell, in open sea, with no map (every tile sea).
internal sealed record SkirmishLayout(
	Side PlayerSide,
	IReadOnlyList<Placement> Units,
	IReadOnlyList<SkirmishFleet> Fleets,
	ScenarioKnowledge Knowledge
)
{
	private Side EnemySide => PlayerSide == Side.Japan ? Side.UnitedStates : Side.Japan;

	private Side Of(Placement placement) => placement.IsPlayers ? PlayerSide : EnemySide;

	// Places the units, as a scenario does (Nspw.CustomScenario): the sides, a time that does not run out before the
	// arena stops the battle, the units, their planes and their ways, then the computer's fleets and their targets.
	public void SetUp(Nspw game)
	{
		game.LocalSide = PlayerSide;
		game.CpuSide = EnemySide;
		game.BattleTime = 1_000_000;
		game.CameraPosition = Units[0].Position;
		var placed = new List<int>();
		foreach (var placement in Units)
		{
			var side = Of(placement);
			var isPlane =
				placement.Kind is UnitKind.Fighter or UnitKind.Attacker or UnitKind.Bomber;
			var m = isPlane
				? game.AddUnit2(
					side,
					placement.Kind,
					0,
					placement.Position.X,
					placement.Position.Y,
					placement.Heading
				)
				: game.AddUnit(
					side,
					placement.Kind,
					placement.Position.X,
					placement.Position.Y,
					placement.Heading
				);
			placed.Add(m);
			ref var unit = ref game.Units[m];
			if (isPlane)
			{
				unit.Carrier = placed[^(placement.Deck + 1)];
				unit.IsStopping = false;
				unit.Speed = unit.MaxSpeed;
			}

			if (placement.Toward is { } toward)
			{
				unit.SetPathPoint(0, toward);
				unit.EndPath(1);
			}

			if (!isPlane)
			{
				foreach (var (kind, count, weapon) in placement.Planes)
					game.AddPlane(side, kind, 0, m, count, weapon);
			}
		}

		foreach (var fleet in Fleets)
		{
			ref var entry = ref game.ComputerFleets[fleet.Number];
			var members = Units
				.Select((u, i) => (u, m: placed[i]))
				.Where(t => !t.u.IsPlayers && t.u.Fleet == fleet.Number)
				.Select(t => t.m)
				.ToList();
			entry.Flagship = (short)members[0];
			for (var i = 0; i < members.Count; i++)
				entry.Members[i] = (short)members[i];
			entry.RenewsFlagship = 1;
			entry.Mode = fleet.Mode;
			foreach (var kind in fleet.Targets)
				game.AddTargetsOfKind(fleet.Number, kind);
		}
	}
}

// A small battle in open sea (docs/StrongAI.md, Skirmishes): a handful of units a side, laid out from a seed, played to
// its end or to `Frames` frames, with both sides seeing everything.
internal sealed record Skirmish(
	string Name,
	string Description,
	int Frames,
	Func<Random, Side, SkirmishLayout> Lay
)
{
	private const double Spread = 60;

	private static readonly UnitKind[] Warships =
	[
		UnitKind.Battleship,
		UnitKind.Cruiser,
		UnitKind.Destroyer,
		UnitKind.Carrier,
		UnitKind.LightCarrier,
	];

	private static readonly UnitKind[] Carriers = [UnitKind.Carrier, UnitKind.LightCarrier];

	private static WorldPosition At(WorldPosition from, double bearing, double distance)
	{
		var radians = bearing * Math.PI / 180;
		return new WorldPosition(
			from.X + (Math.Cos(radians) * distance),
			from.Y + (Math.Sin(radians) * distance)
		);
	}

	private static double Jitter(Random random, double size) =>
		(random.NextDouble() * 2 - 1) * size;

	// A group of ships in a line along `heading`, 150 apart, about `center`, each a little off its place.
	private static IEnumerable<WorldPosition> Line(
		Random random,
		WorldPosition center,
		double heading,
		int count
	) =>
		Enumerable
			.Range(0, count)
			.Select(i => At(center, heading + 180, (i - ((count - 1) / 2.0)) * 150))
			.Select(p => new WorldPosition(
				p.X + Jitter(random, Spread / 2),
				p.Y + Jitter(random, Spread / 2)
			));

	private static Placement Ship(
		bool isPlayers,
		UnitKind kind,
		WorldPosition at,
		double heading,
		int fleet = 0,
		WorldPosition? toward = null
	) => new(isPlayers, kind, at, heading, [], fleet, toward);

	// Ships of both sides, each side in a line, `distance` apart (±20%), the whole turned at random and each side heading
	// its own random way; the computer's ships one fleet that attacks the player's warships.
	private static SkirmishLayout Surface(
		Random random,
		Side side,
		UnitKind[] own,
		UnitKind[] enemy,
		double distance
	)
	{
		var turn = random.NextDouble() * 360;
		var ownCenter = new WorldPosition(0, 0);
		var enemyCenter = At(ownCenter, turn, distance * (0.8 + (random.NextDouble() * 0.4)));
		var ownHeading = random.NextDouble() * 360;
		var enemyHeading = random.NextDouble() * 360;
		var units = new List<Placement>();
		units.AddRange(
			Line(random, ownCenter, ownHeading, own.Length)
				.Select((p, i) => Ship(true, own[i], p, ownHeading))
		);
		units.AddRange(
			Line(random, enemyCenter, enemyHeading, enemy.Length)
				.Select((p, i) => Ship(false, enemy[i], p, enemyHeading, 1))
		);
		return new SkirmishLayout(
			side,
			units,
			[new SkirmishFleet(1, 1, [.. Warships])],
			new ScenarioKnowledge([new Area("the enemy's ships", enemyCenter, 1000, false)], [])
		);
	}

	// The player's carrier with its planes, `distance` from an enemy carrier with escorts, in its fleet's own formation
	// around it (Ring) or in a line that sails on (not Ring), with no planes: the strike against the guns alone. The
	// computer's fleet comes at the player's carrier to its strike range, and away from it within.
	private static SkirmishLayout Strike(
		Random random,
		Side side,
		FireKind weapon,
		int attackers,
		bool ring,
		UnitKind[] escorts
	)
	{
		var turn = random.NextDouble() * 360;
		var ownCenter = new WorldPosition(0, 0);
		var enemyCenter = At(ownCenter, turn, 4500 + Jitter(random, 500));
		var enemyHeading = random.NextDouble() * 360;
		var units = new List<Placement>
		{
			new(
				true,
				UnitKind.Carrier,
				ownCenter,
				random.NextDouble() * 360,
				[(UnitKind.Attacker, attackers, weapon)]
			),
		};
		UnitKind[] enemy = [UnitKind.Carrier, .. escorts];
		units.AddRange(
			Line(random, enemyCenter, enemyHeading, enemy.Length)
				.Select(
					(p, i) =>
						ring
							? Ship(false, enemy[i], p, enemyHeading, 1)
							: Ship(
								false,
								enemy[i],
								p,
								enemyHeading,
								toward: At(p, enemyHeading, 20000)
							)
				)
		);
		return new SkirmishLayout(
			side,
			units,
			ring ? [new SkirmishFleet(1, 1, Carriers)] : [],
			new ScenarioKnowledge(
				[new Area("the enemy's carrier", enemyCenter, 1000, false)],
				Carriers
			)
		);
	}

	// The player's fighters in the air, flying at the enemy's carrier 3000 away, whose fighters it sends up against them
	// (ComputerPlayer.md, Air strikes: interception), its fleet's targets being the player's fighters; the player's
	// carrier 3000 behind them, for them to come home to.
	private static SkirmishLayout Fighters(Random random, Side side, int own, int enemy)
	{
		var turn = random.NextDouble() * 360;
		var front = new WorldPosition(0, 0);
		var enemyCarrier = At(front, turn, 3000 + Jitter(random, 300));
		var ownCarrier = At(front, turn + 180, 3000);
		var units = new List<Placement>
		{
			new(true, UnitKind.Carrier, ownCarrier, turn, []),
			new(
				false,
				UnitKind.Carrier,
				enemyCarrier,
				random.NextDouble() * 360,
				[(UnitKind.Fighter, enemy, FireKind.Unarmed)],
				1
			),
		};
		var toward = At(enemyCarrier, turn + 180, 1500);
		for (var i = 0; i < own; i++)
		{
			var at = At(front, turn + 90, (i - ((own - 1) / 2.0)) * 60);
			units.Add(new(true, UnitKind.Fighter, at, turn, [], Toward: toward, Deck: i + 2));
		}

		return new SkirmishLayout(
			side,
			units,
			[new SkirmishFleet(1, 1, [UnitKind.Fighter])],
			new ScenarioKnowledge(
				[new Area("the enemy's carrier", enemyCarrier, 1000, false)],
				[UnitKind.Fighter]
			)
		);
	}

	// The enemy's carrier with attackers and escorting fighters, 3000 from the player's air base and its fighters: the
	// computer strikes it as soon as its attackers are armed, and again as they come back. An air base, not a carrier,
	// as the strong player's carriers keep out of the computer's strike range (TryPosition), and are never struck.
	private static SkirmishLayout Raid(
		Random random,
		Side side,
		int ownFighters,
		int attackers,
		int escorts
	)
	{
		var turn = random.NextDouble() * 360;
		var ownBase = new WorldPosition(0, 0);
		var enemyCarrier = At(ownBase, turn, 3000 + Jitter(random, 300));
		return new SkirmishLayout(
			side,
			[
				new(
					true,
					UnitKind.AirBase,
					ownBase,
					0,
					[(UnitKind.Fighter, ownFighters, FireKind.Unarmed)]
				),
				new(
					false,
					UnitKind.Carrier,
					enemyCarrier,
					random.NextDouble() * 360,
					[
						(UnitKind.Attacker, attackers, FireKind.Unarmed),
						(UnitKind.Fighter, escorts, FireKind.Unarmed),
					],
					1
				),
			],
			[new SkirmishFleet(1, 1, [UnitKind.AirBase])],
			new ScenarioKnowledge([new Area("the enemy's carrier", enemyCarrier, 1000, false)], [])
		);
	}

	private const UnitKind BB = UnitKind.Battleship;
	private const UnitKind CA = UnitKind.Cruiser;
	private const UnitKind DD = UnitKind.Destroyer;

	public static readonly IReadOnlyList<Skirmish> All =
	[
		new(
			"fighters",
			"6 fighters in the air against a carrier's 6, sent up to meet them",
			2000,
			(r, s) => Fighters(r, s, 6, 6)
		),
		new(
			"raid",
			"an air base with 6 fighters against a carrier's strike of 6 attackers and 3 fighters",
			2500,
			(r, s) => Raid(r, s, 6, 6, 3)
		),
		new(
			"dd-ca",
			"a destroyer against a cruiser, 1500 apart",
			3000,
			(r, s) => Surface(r, s, [DD], [CA], 1500)
		),
		new(
			"ca-dd",
			"a cruiser against a destroyer, 1500 apart",
			3000,
			(r, s) => Surface(r, s, [CA], [DD], 1500)
		),
		new(
			"cruisers",
			"2 cruisers and 2 destroyers against the same, 2500 apart",
			4000,
			(r, s) => Surface(r, s, [CA, CA, DD, DD], [CA, CA, DD, DD], 2500)
		),
		new(
			"bb-ca",
			"a battleship against 2 cruisers, 2500 apart",
			4000,
			(r, s) => Surface(r, s, [BB], [CA, CA], 2500)
		),
		new(
			"ca-bb",
			"2 cruisers against a battleship, 2500 apart",
			4000,
			(r, s) => Surface(r, s, [CA, CA], [BB], 2500)
		),
		new(
			"torpedo-ring",
			"9 attackers with torpedoes against a carrier, 2 cruisers and a destroyer in their fleet's formation",
			2500,
			(r, s) => Strike(r, s, FireKind.Torpedo, 9, true, [CA, CA, DD])
		),
		new(
			"torpedo-line",
			"9 attackers with torpedoes against a carrier, 2 cruisers and a destroyer in a line that sails on",
			2500,
			(r, s) => Strike(r, s, FireKind.Torpedo, 9, false, [CA, CA, DD])
		),
		new(
			"bomb-ring",
			"9 attackers with bombs against a carrier, 2 cruisers and a destroyer in their fleet's formation",
			2500,
			(r, s) => Strike(r, s, FireKind.Bomb, 9, true, [CA, CA, DD])
		),
		new(
			"bomb-line",
			"9 attackers with bombs against a carrier, 2 cruisers and a destroyer in a line that sails on",
			2500,
			(r, s) => Strike(r, s, FireKind.Bomb, 9, false, [CA, CA, DD])
		),
	];

	public static IReadOnlyList<Skirmish> Parse(string names) =>
		names == "all"
			? All
			:
			[
				.. names
					.Split(',')
					.Select(n =>
						All.FirstOrDefault(s => s.Name == n)
						?? throw new ArgumentException($"No skirmish is named {n}.")
					),
			];
}
