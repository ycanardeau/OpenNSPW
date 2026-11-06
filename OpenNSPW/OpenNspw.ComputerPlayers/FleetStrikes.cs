using static OpenNspw.ComputerPlayers.StrategyBasics;
using static OpenNspw.Nspw;

namespace OpenNspw.ComputerPlayers;

// cpu_strtgy's steps 10 and 11: the air strikes, interceptions and strategic bombing from a fleet's flagship, its other
// carriers' strikes, and an air base's fighters (docs/ComputerPlayer.md, cpu_strtgy).
public readonly struct FleetStrikes(Nspw game)
{
	private readonly Nspw _game = game;

	// The order's destination, moved 300 in a random direction: a strike on a base.
	private void MoveOrderDestinationAtRandom()
	{
		var a = _game.rand() % 360 * DegreesToRadians;
		_game.MoveOrders[0].Destination = new WorldPosition(
			cos(a) * 300 + _game.MoveOrders[0].Destination.X,
			sin(a) * 300 + _game.MoveOrders[0].Destination.Y
		);
	}

	// The order's destination, where the target will be: its position plus its heading times the distance.
	private void LeadMoveOrderDestination(ref Unit target, double distance)
	{
		var a = target.Direction.Degrees * DegreesToRadians;
		_game.MoveOrders[0].Destination = new WorldPosition(
			cos(a) * distance + _game.MoveOrders[0].Destination.X,
			sin(a) * distance + _game.MoveOrders[0].Destination.Y
		);
	}

	// The order: the leader flies to the target, with the selected planes.
	private void OrderStrike(int leader, ref Unit target)
	{
		_game.MoveOrders[0].Unit = (short)leader;
		_game.MoveOrders[0].Destination = target.Position;
	}

	// Step 10: the carrier's fighters intercept an enemy plane in the air.
	private bool Intercept(int carrier, int t, double distance)
	{
		var all = 0;
		var ready = 0;
		var leader = 0;
		for (var c = 1; c <= _game.MaxUnitId; c++)
		{
			ref var plane = ref _game.Units[c];
			if (!plane.IsUsed || plane.Kind != UnitKind.Fighter || plane.Carrier != carrier)
				continue;

			all++;
			if (
				plane.IsStopping
				&& plane.Ammo != 0
				&& plane.ReloadTime == 0
				&& plane.Weapon != FireKind.Unarmed
				&& plane.Weapon != FireKind.Maintenance
			)
			{
				ready++;
				if (leader == 0)
					leader = c;
			}
		}

		if (ready != all || leader == 0)
			return false;

		var n = 1;
		for (var c = 1; c <= _game.MaxUnitId; c++)
		{
			ref var plane = ref _game.Units[c];
			if (
				!plane.IsUsed
				|| plane.Kind != UnitKind.Fighter
				|| plane.Carrier != carrier
				|| !IsReadyToTakeOff(ref plane)
			)
				continue;

			plane.Target = t;
			plane.AirSuperioritySortie = 1;
			if (c != leader)
				_game.Selections[0][c] = (short)n++;
		}

		ref var target = ref _game.Units[t];
		OrderStrike(leader, ref target);
		if (target.Kind is not (UnitKind.AirBase or UnitKind.NavalBase))
			LeadMoveOrderDestination(ref target, distance * (1.0 / 3) * target.Speed);
		else
			MoveOrderDestinationAtRandom();
		return true;
	}

	// Steps 10 and 11: the planes of the carrier or air base that strike, when they are all ready: the first ready one
	// of the leader's kind leads; the others of the kinds follow, selected.
	private int SelectStrike(
		int carrier,
		UnitKind leaders,
		Func<UnitKind, bool> counts,
		Func<UnitKind, bool> follows
	)
	{
		var all = 0;
		var ready = 0;
		var leader = 0;
		for (var c = 1; c <= _game.MaxUnitId; c++)
		{
			ref var plane = ref _game.Units[c];
			if (
				!plane.IsUsed
				|| plane.Category != UnitCategory.Plane
				|| !counts(plane.Kind)
				|| plane.Carrier != carrier
			)
				continue;

			all++;
			if (IsReadyToTakeOff(ref plane))
			{
				ready++;
				if (plane.Kind == leaders && leader == 0)
					leader = c;
			}
		}

		if (ready != all || leader == 0)
			return 0;

		var n = 1;
		for (var c = 1; c <= _game.MaxUnitId; c++)
		{
			ref var plane = ref _game.Units[c];
			if (
				plane.IsUsed
				&& plane.Category == UnitCategory.Plane
				&& follows(plane.Kind)
				&& plane.Carrier == carrier
				&& IsReadyToTakeOff(ref plane)
				&& c != leader
			)
				_game.Selections[0][c] = (short)n++;
		}

		return leader;
	}

	// Step 10: the strikes of the fleet's flagship, a carrier or an air base.
	public StrategyFlow StrikeFromFlagship(ref ComputerFleet fleet, int near)
	{
		var carrier = fleet.Flagship;
		int t;
		if ((short)near != 0)
			t = (short)near;
		else
			t = fleet.Targets[
				_game.Units[carrier].Kind == UnitKind.AirBase ? _game.rand() % 17 + 1 : 1
			];

		if (t == 0 || !_game.Units[t].IsUsed || fleet.Flagship == 0)
			return StrategyFlow.Next;

		ref var flagship = ref _game.Units[carrier];
		var kind = flagship.Kind;
		if (
			flagship.PlanesToLaunch > 0
			|| kind is not (UnitKind.Carrier or UnitKind.LightCarrier or UnitKind.AirBase)
			|| fleet.Base != 0
		)
			return StrategyFlow.Next;

		if ((fleet.Targets[1] == 0 && (short)near == 0) || flagship.SupplyTime != 0)
			return StrategyFlow.Next;

		ref var target = ref _game.Units[t];
		var distance = DistanceBetween(ref target, ref flagship);
		var range = (double)(_game.CpuSide == Side.Japan ? 4350 : 3550);
		var isNear =
			distance <= range
			|| (
				!(range * 1.5 < distance)
				&& (kind == UnitKind.AirBase || fleet.Mode == 50 || _game.BattleTime < 8000)
			);
		if (
			isNear
			&& target.Category == UnitCategory.Plane
			&& target.PlaneState == UnitState.Flying
			&& Intercept(carrier, t, distance)
		)
			return StrategyFlow.Order;

		var strikes =
			isNear
			|| (
				!(range * 1.7 < distance)
				&& kind == UnitKind.AirBase
				&& IsGroundFacility(target.Kind)
			);
		if (!strikes || target.Category == UnitCategory.Plane)
			return StrategyFlow.Next;

		var leader = SelectStrike(
			carrier,
			UnitKind.Attacker,
			k => k == UnitKind.Attacker,
			k => k is UnitKind.Attacker or UnitKind.Fighter
		);
		if (leader == 0)
			return StrategyFlow.Next;

		OrderStrike(leader, ref target);
		if (target.Kind is UnitKind.AirBase or UnitKind.NavalBase)
		{
			MoveOrderDestinationAtRandom();
		}
		else
		{
			LeadMoveOrderDestination(ref target, distance * 0.25 * target.Speed);
			if (!(target.Speed > 1.0))
			{
				_game.MoveOrders[0].Destination = new WorldPosition(
					_game.rand() % 800 - 400 + _game.MoveOrders[0].Destination.X,
					_game.MoveOrders[0].Destination.Y
				);
				_game.MoveOrders[0].Destination = new WorldPosition(
					_game.MoveOrders[0].Destination.X,
					_game.rand() % 800 - 400 + _game.MoveOrders[0].Destination.Y
				);
			}
		}

		return StrategyFlow.Order;
	}

	// Step 10: an air base's strategic bombers strike its first target (or, in scenario 204, the target near the
	// destination) within 20000.
	public StrategyFlow BombFromAirBase(ref ComputerFleet fleet, int near)
	{
		int t = fleet.Targets[1];
		if (_game.ScenarioNumber == 204 && (short)near != 0)
			t = (short)near;

		if (t == 0 || !_game.Units[t].IsUsed || fleet.Flagship == 0)
			return StrategyFlow.Next;

		int airBase = fleet.Flagship;
		ref var flagship = ref _game.Units[airBase];
		if (
			flagship.PlanesToLaunch > 0
			|| flagship.Kind != UnitKind.AirBase
			|| fleet.Base != 0
			|| flagship.SupplyTime != 0
		)
			return StrategyFlow.Next;

		ref var target = ref _game.Units[t];
		if (
			!(DistanceBetween(ref target, ref flagship) <= 20000)
			|| !flagship.IsUsed
			|| target.Category == UnitCategory.Plane
		)
			return StrategyFlow.Next;

		var leader = SelectStrike(
			airBase,
			UnitKind.Bomber,
			k => k == UnitKind.Bomber,
			k => k is UnitKind.Bomber or UnitKind.Fighter or UnitKind.Attacker
		);
		if (leader == 0)
			return StrategyFlow.Next;

		OrderStrike(leader, ref target);
		if (target.Kind is UnitKind.AirBase or UnitKind.NavalBase)
			MoveOrderDestinationAtRandom();
		else
			LeadMoveOrderDestination(ref target, target.Speed * 1500);
		return StrategyFlow.Order;
	}

	// Step 11: the strikes of the fleet's other carriers, on its first target.
	public StrategyFlow StrikeFromOtherCarriers(ref ComputerFleet fleet, int near)
	{
		int t = fleet.Targets[1];
		for (var k = 1; MemberAt(ref fleet, k) != 0; k++)
		{
			int m = MemberAt(ref fleet, k);
			ref var carrier = ref _game.Units[m];
			if (
				carrier.Kind is not (UnitKind.Carrier or UnitKind.LightCarrier)
				|| t == 0
				|| carrier.PlanesToLaunch > 0
				|| fleet.Base != 0
			)
				continue;

			if (
				(fleet.Targets[1] == 0 && (short)near == 0)
				|| _game.Units[fleet.Flagship].SupplyTime != 0
			)
				continue;

			ref var target = ref _game.Units[t];
			var distance = DistanceBetween(ref target, ref carrier);
			var range = (double)(_game.CpuSide == Side.Japan ? 4350 : 3550);
			var isNear =
				distance <= range
				|| (!(range * 1.5 < distance) && (fleet.Mode == 50 || _game.BattleTime < 8000));
			if (!isNear || target.Category == UnitCategory.Plane)
				continue;

			var leader = SelectStrike(
				m,
				UnitKind.Attacker,
				kind => kind != UnitKind.Bomber,
				kind => kind != UnitKind.Bomber
			);
			if (leader == 0)
				continue;

			OrderStrike(leader, ref target);
			if (target.Kind is UnitKind.AirBase or UnitKind.NavalBase)
				MoveOrderDestinationAtRandom();
			else
				LeadMoveOrderDestination(ref target, distance * 0.25 * target.Speed);
			return StrategyFlow.Order;
		}

		return StrategyFlow.Next;
	}

	// After step 11: an air base's fighters, two at most with their leader, intercept the enemy attackers and bombers
	// within 1700, each a random one of them.
	public void DefendAirBase(ref ComputerFleet fleet)
	{
		if (fleet.Flagship == 0)
			return;

		int airBase = fleet.Flagship;
		ref var flagship = ref _game.Units[airBase];
		if (
			flagship.Kind != UnitKind.AirBase
			|| flagship.PlanesToLaunch != 0
			|| flagship.LaunchLock != 0
			|| _game.Tick % 150 > 50
		)
			return;

		var threats = new List<short>();
		for (var i = 1; i <= _game.MaxUnitId; i++)
		{
			ref var enemy = ref _game.Units[i];
			if (
				enemy.IsUsed
				&& enemy.Side != _game.CpuSide
				&& enemy.Kind is UnitKind.Attacker or UnitKind.Bomber
				&& enemy.PlaneState == UnitState.Flying
				&& enemy.Ammo != 0
				&& enemy.IsFound
				&& DistanceBetween(ref enemy, ref flagship) < 1700
			)
				threats.Add((short)i);
		}

		if (threats.Count == 0)
			return;

		static bool IsReadyFighter(ref Unit plane, int airBase) =>
			plane.IsUsed
			&& plane.Category == UnitCategory.Plane
			&& plane.Kind == UnitKind.Fighter
			&& plane.Carrier == airBase
			&& plane.IsStopping
			&& plane.PlaneState == UnitState.Parked
			&& plane.Ammo != 0
			&& plane.ReloadTime == 0
			&& plane.Weapon != FireKind.Maintenance;

		var leader = 0;
		for (var c = 1; c <= _game.MaxUnitId && leader == 0; c++)
		{
			if (IsReadyFighter(ref _game.Units[c], airBase))
				leader = c;
		}

		if (leader == 0)
			return;

		var n = 1;
		for (var c = 1; c <= _game.MaxUnitId && n < 3; c++)
		{
			ref var plane = ref _game.Units[c];
			if (!IsReadyFighter(ref plane, fleet.Flagship))
				continue;

			plane.Target = threats[_game.rand() % threats.Count];
			plane.AirSuperioritySortie = 1;
			if (c != leader)
				_game.Selections[0][c] = (short)n++;
		}

		_game.MoveOrders[0].Unit = (short)leader;
		_game.MoveOrders[0].Destination = flagship.Position;
		var a = _game.rand() % 360 * DegreesToRadians;
		_game.MoveOrders[0].Destination = new WorldPosition(
			cos(a) * 200 + _game.MoveOrders[0].Destination.X,
			sin(a) * 200 + _game.MoveOrders[0].Destination.Y
		);
	}
}
