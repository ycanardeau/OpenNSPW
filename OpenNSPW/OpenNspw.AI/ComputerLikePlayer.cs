namespace OpenNspw.AI;

// A player that plays as 1.22's computer does: its units are put in fleets as a scenario puts the computer's, and
// cpu_strtgy (Nspw.PlanComputerStrategy) plays them, for the player's side, with fleets of their own, once a frame
// (docs/StrongAI.md, Measuring). It knows what the computer knows: its fleets' targets are every enemy unit of their
// kinds, seen or not. The baseline of what the computer's own way of playing gives on the player's side.
public sealed class ComputerLikePlayer : IPlayer
{
	// How near a ship is to another of its fleet, at the start.
	private const double FleetSpread = 1000;

	// The members a fleet gets at most, of the 17 that its table holds.
	private const int MaxMembers = 12;

	// The targets a fleet's list holds, from index 1. The scenarios' lists are filled by Nspw.AddTargetsOfKind and the
	// others, which write on into the members past them (docs/ComputerPlayer.md, Fleets).
	private const int MaxTargets = 17;

	private Array11<ComputerFleet> _fleets;
	private bool _isSetUp;

	public string Name => "computer";

	// The order a fleet's members are in: its carriers first, which its formation puts at its centre, then its
	// transports, then its warships, heaviest first.
	private static int Rank(UnitKind kind) =>
		kind switch
		{
			UnitKind.Carrier => 0,
			UnitKind.LightCarrier => 1,
			UnitKind.Transport => 2,
			_ => 3 + (int)kind,
		};

	// Whether unit m is one of the player's that a fleet can lead or hold: a ship, a transport, or an air base; not a
	// ship anchored, which a scenario such as 8 counts while it stays at anchor.
	private static bool IsFleetUnit(Nspw game, int m)
	{
		ref var unit = ref game.Units[m];
		return unit.IsUsed
			&& unit.HP > 0
			&& unit.Side == game.LocalSide
			&& !UnitFacts.IsAnchored(ref unit)
			&& (
				unit.Kind == UnitKind.AirBase
				|| (unit.Category == UnitCategory.Ship && !UnitFacts.IsBase(unit.Kind))
			);
	}

	// The player's ships in groups, each of the ships within FleetSpread of another of its group, and each air base and
	// submarine alone, in the order of their numbers.
	private static List<List<int>> Groups(Nspw game)
	{
		var groups = new List<List<int>>();
		var grouped = new HashSet<int>();
		for (var m = 1; m <= game.MaxUnitId; m++)
		{
			if (!IsFleetUnit(game, m) || !grouped.Add(m))
				continue;

			var group = new List<int> { m };
			groups.Add(group);
			if (game.Units[m].Kind is UnitKind.AirBase or UnitKind.Submarine)
				continue;

			for (var i = 0; i < group.Count; i++)
			{
				for (var n = 1; n <= game.MaxUnitId; n++)
				{
					if (
						IsFleetUnit(game, n)
						&& game.Units[n].Kind is not (UnitKind.AirBase or UnitKind.Submarine)
						&& !grouped.Contains(n)
						&& UnitFacts.Distance(game.Units[group[i]].Position, game.Units[n].Position)
							<= FleetSpread
					)
					{
						grouped.Add(n);
						group.Add(n);
					}
				}
			}
		}

		return groups;
	}

	// The order fleets get their slots in: carriers, transports, battle fleets, air bases, submarines.
	private static int FleetRank(Nspw game, List<int> group) =>
		game.Units[group[0]].Kind switch
		{
			UnitKind.AirBase => 20,
			UnitKind.Submarine => 21,
			var kind => Rank(kind),
		};

	// A fleet's targets, as a scenario gives the computer's with Nspw.AddTargetsOfKind and the others: every enemy unit
	// of the kinds the victory counts, then every enemy carrier, then every battleship, cruiser and carrier, each once, by
	// their numbers, as many as the list holds.
	private static List<short> Targets(Nspw game, ScenarioKnowledge knowledge)
	{
		var targets = new List<short>();

		void Add(Func<UnitKind, bool> isTarget)
		{
			for (var m = 1; m <= game.MaxUnitId; m++)
			{
				ref var unit = ref game.Units[m];
				if (
					unit.IsUsed
					&& unit.Side != game.LocalSide
					&& isTarget(unit.Kind)
					&& !targets.Contains((short)m)
				)
					targets.Add((short)m);
			}
		}

		foreach (var kind in knowledge.Counted)
			Add(k => k == kind);
		Add(k => k is UnitKind.Carrier or UnitKind.LightCarrier);
		Add(k =>
			k
				is UnitKind.Battleship
					or UnitKind.Cruiser
					or UnitKind.Carrier
					or UnitKind.LightCarrier
		);
		return targets.Take(MaxTargets).ToList();
	}

	// The player's fleets, set up in a table of their own as a scenario sets up the computer's: each group's members,
	// its first the flagship, attacking its targets toward where the briefing expects the enemy.
	private void SetUpFleets(Nspw game)
	{
		var knowledge = ScenarioKnowledge.For(game.ScenarioNumber);
		var targets = Targets(game, knowledge);
		var area = knowledge.Landing ?? knowledge.Enemy.FirstOrDefault();
		var groups = Groups(game)
			.Select(group => group.OrderBy(m => Rank(game.Units[m].Kind)).ThenBy(m => m).ToList())
			.OrderBy(group => FleetRank(game, group))
			.Take(10)
			.ToList();
		for (var f = 1; f < 11; f++)
		{
			ref var fleet = ref _fleets[f];
			fleet.DestinationX = World.Beyond;
			if (f > groups.Count)
				continue;

			var group = groups[f - 1];
			fleet.Flagship = (short)group[0];
			for (var k = 0; k < Math.Min(group.Count, MaxMembers); k++)
				fleet.Members[k] = (short)group[k];
			for (var k = 0; k < targets.Count; k++)
				fleet.Targets[k + 1] = targets[k];
			fleet.RenewsFlagship = 1;
			fleet.Mode = 1;
			if (area is not null)
			{
				fleet.DestinationX = area.Center.X;
				fleet.DestinationY = area.Center.Y;
			}
		}
	}

	// cpu_strtgy for the player's fleets, as the computer's runs in a frame's orders (Nspw.IssueSingleplayerOrders), and
	// its order applied as the computer's is, the ships' routes planned by set_cpu_route. The computer's side, fleets,
	// orders and the scenario number, which runs the computer's scenario events, are put back after; the scenario is 0
	// while it plays, so that none of them runs for the player.
	public void Act(Nspw game)
	{
		if (game.Result != GameResult.None)
			return;

		if (!_isSetUp)
		{
			SetUpFleets(game);
			_isSetUp = true;
		}

		var savedFleets = game.ComputerFleets;
		var savedSide = game.CpuSide;
		var savedScenario = game.ScenarioNumber;
		var savedMoveOrders = game.MoveOrders;
		var savedSelectOrders = game.SelectOrders;
		var savedSelections = game.Selections[0];
		var savedSelectedUnit = game.SelectedUnit;
		game.ComputerFleets = _fleets;
		game.CpuSide = game.LocalSide;
		game.ScenarioNumber = 0;
		try
		{
			game.PlanComputerStrategy();
			game.MoveOrders[1].Unit = 0;
			game.SelectOrders[0].IsSet = false;
			game.SelectOrders[1].IsSet = false;
			game.ApplyOrders();
		}
		finally
		{
			_fleets = game.ComputerFleets;
			game.ComputerFleets = savedFleets;
			game.CpuSide = savedSide;
			game.ScenarioNumber = savedScenario;
			game.MoveOrders = savedMoveOrders;
			game.SelectOrders = savedSelectOrders;
			game.Selections[0] = savedSelections;
			game.SelectedUnit = savedSelectedUnit;
		}
	}
}
