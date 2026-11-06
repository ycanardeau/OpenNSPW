namespace OpenNspw;

// A single-player battle with the sides switched (BattleOptions.SwitchSides, docs/Options.md): the player commands the
// side the computer commands in 1.22's scenario, and the computer the player's. 1.22's computer plays only the fleets its
// scenario sets up, of its own side, so the game forms fleets of the side the player leaves, for cpu_strtgy to play as it
// plays 1.22's (docs/ComputerPlayer.md, Switched sides).
public unsafe partial class Nspw
{
	// The most members of a fleet of 1.22's scenarios (13, scenario 304), and the most targets it holds (1 to 17).
	private const int SwitchedFleetMembers = 13;
	private const int SwitchedFleetTargets = 17;

	// Whether the sides of the battle are switched, as its options were when it was set up: cpu_strtgy then plays the fleets
	// the game formed, without the scenario's events, which act on 1.22's.
	public bool AreSidesSwitched;

	private static bool IsWarship(UnitKind kind) =>
		kind
			is UnitKind.Battleship
				or UnitKind.Cruiser
				or UnitKind.Destroyer
				or UnitKind.Carrier
				or UnitKind.LightCarrier;

	// Where a transport lands its cargo, and a fleet of transports aims: a base, a city or a ground unit.
	private static bool IsLandingTarget(UnitKind kind) =>
		kind
			is UnitKind.AirBase
				or UnitKind.NavalBase
				or UnitKind.City
				or UnitKind.InfantryBase
				or UnitKind.Pillboxes
				or UnitKind.Fortress;

	// The kinds a fleet attacks: the ships that 1.22's scenarios give their fleets as targets, and the landing targets.
	private static bool IsSwitchedTarget(UnitKind kind, bool shipsOnly) =>
		kind
			is UnitKind.Battleship
				or UnitKind.Cruiser
				or UnitKind.Carrier
				or UnitKind.LightCarrier
				or UnitKind.Transport
		|| (!shipsOnly && IsLandingTarget(kind));

	// The order in which the units of the computer lead fleets, while there are fleets to fill: carriers, then air bases,
	// then the other warships by size, transports and submarines; int.MaxValue for a unit that leads none.
	private static int LeadRank(UnitKind kind) =>
		kind switch
		{
			UnitKind.Carrier => 0,
			UnitKind.LightCarrier => 1,
			UnitKind.AirBase => 2,
			UnitKind.Battleship => 3,
			UnitKind.Cruiser => 4,
			UnitKind.Destroyer => 5,
			UnitKind.Transport => 6,
			UnitKind.Submarine => 7,
			_ => int.MaxValue,
		};

	private double DistanceBetweenUnits(int a, int b) =>
		(Units[a].Position - Units[b].Position).Length;

	// Whether unit m may join the fleet that `leader` leads: a warship a warship's, a transport a transport's; an air base
	// and a submarine lead fleets of their own.
	private bool JoinsFleetOf(int m, int leader)
	{
		var kind = Units[m].Kind;
		var leads = Units[leader].Kind;
		return (IsWarship(kind) && IsWarship(leads))
			|| (kind == UnitKind.Transport && leads == UnitKind.Transport);
	}

	private static int CountMembers(ref ComputerFleet fleet)
	{
		var count = 0;
		while (count < SwitchedFleetMembers && fleet.Members[count] != 0)
			count++;

		return count;
	}

	// The enemy's unit nearest to unit m among those `isTarget` takes, or 0.
	private int FindNearestEnemy(int m, Func<UnitKind, bool> isTarget)
	{
		var nearest = 0;
		for (var i = 1; i <= MaxUnitId; i++)
		{
			ref var unit = ref Units[i];
			if (!unit.IsUsed || unit.Side == CpuSide || !isTarget(unit.Kind))
				continue;

			if (nearest == 0 || DistanceBetweenUnits(i, m) < DistanceBetweenUnits(nearest, m))
				nearest = i;
		}

		return nearest;
	}

	// Fleet f's targets, from element 1: the enemy's carriers first, then the other units it attacks, the nearest to its
	// flagship first, up to 17. 1.22's AddTargets has no limit, and would write on into the members.
	private void SetSwitchedTargets(ref ComputerFleet fleet, bool shipsOnly)
	{
		int flagship = fleet.Flagship;
		var targets = new List<int>();
		for (var i = 1; i <= MaxUnitId; i++)
		{
			ref var unit = ref Units[i];
			if (unit.IsUsed && unit.Side != CpuSide && IsSwitchedTarget(unit.Kind, shipsOnly))
				targets.Add(i);
		}

		var ordered = targets
			.OrderBy(i => Units[i].Kind is UnitKind.Carrier or UnitKind.LightCarrier ? 0 : 1)
			.ThenBy(i => DistanceBetweenUnits(i, flagship))
			.Take(SwitchedFleetTargets)
			.ToArray();
		for (var k = 0; k < ordered.Length; k++)
			fleet.Targets[k + 1] = (short)ordered[k];
	}

	// The transports of the fleet land their cargo at the enemy's landing target nearest to its flagship, which the fleet
	// aims at, as 1.22's scenarios set up the computer's transports (scenario 103); with none, the fleet holds.
	private void SetSwitchedLanding(ref ComputerFleet fleet)
	{
		int flagship = fleet.Flagship;
		var landing = FindNearestEnemy(flagship, IsLandingTarget);
		if (landing == 0)
		{
			fleet.Mode = 0;
			return;
		}

		ref var target = ref Units[landing];
		fleet.AimX = target.Position.X;
		fleet.AimY = target.Position.Y;
		fleet.Targets[1] = (short)landing;
		for (var k = 0; k < SwitchedFleetMembers && fleet.Members[k] != 0; k++)
		{
			ref var transport = ref Units[fleet.Members[k]];
			if (
				transport.Weapon is < FireKind.CargoNavalBase or > FireKind.CargoFortress
				|| transport.Ammo == 0
			)
				continue;

			transport.Target = MaxUnitId + 1;
			transport.LandingX = (int)target.Position.X;
			transport.LandingY = (int)target.Position.Y;
		}
	}

	// A fleet led by unit `leader`, with the computer's units not in a fleet yet within 1200 of it that may join it.
	private void FormSwitchedFleet(ref ComputerFleet fleet, int leader, bool[] isInFleet)
	{
		fleet.Flagship = (short)leader;
		fleet.Members[0] = (short)leader;
		fleet.RenewsFlagship = 1;
		fleet.Mode = 1;
		isInFleet[leader] = true;

		var count = 1;
		for (var m = 1; m <= MaxUnitId && count < SwitchedFleetMembers; m++)
		{
			ref var unit = ref Units[m];
			if (
				isInFleet[m]
				|| unit.Side != CpuSide
				|| !JoinsFleetOf(m, leader)
				|| DistanceBetweenUnits(m, leader) > 1200
			)
				continue;

			fleet.Members[count++] = (short)m;
			isInFleet[m] = true;
		}

		var kind = Units[leader].Kind;
		if (kind == UnitKind.Transport)
			SetSwitchedLanding(ref fleet);
		else
			SetSwitchedTargets(ref fleet, kind == UnitKind.Submarine);
	}

	// The fleets of the computer, the side the player left: 10, as cpu_strtgy plays fleets 1 to 10, led by the units in the
	// order of LeadRank, and then the units left join the nearest fleet they may join with room, or stay in none, where they
	// still fire and defend themselves. An air base leads a fleet only with planes, which are what it plays.
	private void FormSwitchedFleets()
	{
		var isInFleet = new bool[MaxUnitId + 1];
		var leaders = Enumerable
			.Range(1, MaxUnitId)
			.Where(m =>
				Units[m].Side == CpuSide
				&& LeadRank(Units[m].Kind) != int.MaxValue
				&& (Units[m].Kind != UnitKind.AirBase || CountPlanesIn(m) > 0)
			)
			.OrderBy(m => LeadRank(Units[m].Kind))
			.ThenBy(m => m)
			.ToArray();

		var f = 1;
		foreach (var leader in leaders)
		{
			if (f > 10)
				break;

			if (!isInFleet[leader])
				FormSwitchedFleet(ref FleetAt(f++), leader, isInFleet);
		}

		foreach (var m in leaders)
		{
			if (isInFleet[m])
				continue;

			var nearest = 0;
			for (var g = 1; g < f; g++)
			{
				ref var fleet = ref FleetAt(g);
				if (
					!JoinsFleetOf(m, fleet.Flagship)
					|| CountMembers(ref fleet) == SwitchedFleetMembers
				)
					continue;

				if (
					nearest == 0
					|| DistanceBetweenUnits(m, fleet.Flagship)
						< DistanceBetweenUnits(m, FleetAt(nearest).Flagship)
				)
					nearest = g;
			}

			if (nearest != 0)
			{
				ref var fleet = ref FleetAt(nearest);
				fleet.Members[CountMembers(ref fleet)] = (short)m;
				isInFleet[m] = true;
			}
		}
	}

	// The camera on the player's first unit in the world that is not a plane, where scenarios 3 and 4 put it from theirs,
	// kept within the map, as the minimap keeps it (ShowMinimapCell).
	private void PlaceCameraOnLocalSide()
	{
		for (var m = 1; m <= MaxUnitId; m++)
		{
			ref var unit = ref Units[m];
			if (
				unit.Side != LocalSide
				|| unit.Category == UnitCategory.Plane
				|| !World.Contains(unit.Position)
			)
				continue;

			CameraPosition = new WorldPosition(
				Math.Clamp(unit.Position.X - 400, World.Left, World.Right - CMBT_WIDTH),
				Math.Clamp(unit.Position.Y + 400, World.Bottom + CMBT_HEIGHT, World.Top)
			);
			return;
		}
	}

	// Switches the sides of the scenario 1.22 has set up: the player takes the computer's, and the computer, in fleets
	// formed anew, the player's. In scenarios 1 and 2, where the computer plays no side, the player only takes the other.
	private void SwitchSides()
	{
		if (CpuSide == Side.None)
		{
			LocalSide = LocalSide == Side.Japan ? Side.UnitedStates : Side.Japan;
		}
		else
		{
			(LocalSide, CpuSide) = (CpuSide, LocalSide);
			ClearComputerFleets();
			FormSwitchedFleets();
		}

		PlaceCameraOnLocalSide();
	}
}
