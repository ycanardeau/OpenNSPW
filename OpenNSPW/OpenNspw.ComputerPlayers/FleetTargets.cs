using static OpenNspw.ComputerPlayers.StrategyBasics;

namespace OpenNspw.ComputerPlayers;

// cpu_strtgy's steps 7 and 8: the target of each of the computer's ships and planes, in one loop over the units, as 1.22
// draws rand() for both in it (docs/ComputerPlayer.md, cpu_strtgy).
public readonly struct FleetTargets(Nspw game)
{
	private readonly Nspw _game = game;

	// sub_403A70: whether plane m belongs to fleet f, its carrier or base being the fleet's flagship.
	private bool IsInFleet(int m, int f)
	{
		return _game.Units[m].Carrier == _game.FleetAt(f).Flagship;
	}

	// Step 7, for a ship of a carrier or transport fleet: twice, a random enemy unit, visible and not a plane, within
	// 900.
	private void ChooseEscortsTarget(ref Unit ship)
	{
		for (var n = 0; n <= 1 && ship.Target == 0; n++)
		{
			var r = _game.rand() % _game.MaxUnitId + 1;
			ref var enemy = ref _game.Units[r];
			if (
				enemy.IsUsed
				&& enemy.Side != _game.CpuSide
				&& enemy.IsFound
				&& !(enemy.Kind == UnitKind.Submarine && enemy.IsSubmerged)
				&& enemy.Category != UnitCategory.Plane
				&& DistanceBetween(ref enemy, ref ship) < 900
			)
				ship.Target = r;
		}
	}

	// Step 7, for a ship of a battle fleet: one of the fleet's targets, one time in 2, within 1200; or, if that one is
	// not one to shoot, four times, a random enemy unit within 1200.
	private void ChooseWarshipsTarget(ref ComputerFleet fleet, ref Unit ship)
	{
		var t = fleet.Targets[_game.rand() % 17 + 1];
		if (
			t != 0
			&& _game.Units[t].IsFound
			&& !(_game.Units[t].Kind == UnitKind.Submarine && _game.Units[t].IsSubmerged)
			&& !(ship.Kind == UnitKind.Submarine && IsGroundFacility(_game.Units[t].Kind))
		)
		{
			if (_game.rand() % 2 == 1 && DistanceBetween(ref _game.Units[t], ref ship) < 1200)
				ship.Target = t;
			return;
		}

		for (var n = 0; n <= 3 && ship.Target == 0; n++)
		{
			var r = _game.rand() % _game.MaxUnitId + 1;
			ref var enemy = ref _game.Units[r];
			if (
				enemy.IsUsed
				&& enemy.Side != _game.CpuSide
				&& enemy.IsFound
				&& !(enemy.Kind == UnitKind.Submarine && enemy.IsSubmerged)
				&& enemy.Kind != UnitKind.Transport
				&& !(ship.Kind == UnitKind.Submarine && IsGroundFacility(enemy.Kind))
				&& DistanceBetween(ref enemy, ref ship) < 1200
			)
				ship.Target = r;
		}
	}

	// Step 8: the plane attacks unit t, out of its group; it flies first to its emergency point, 600 to one side by its
	// place in the formation, then to the target.
	private void AttackWith(ref Unit plane, int t)
	{
		ref var target = ref _game.Units[t];
		plane.GroupLeader = 0;
		plane.Target = t;
		var drctn = plane.Direction.Degrees;
		switch (plane.FormationNumber % 6)
		{
			case 1:
				drctn = drctn + 337.5;
				break;
			case 2:
				drctn = drctn + 22.5;
				break;
			case 3:
				drctn = drctn + 315.0;
				break;
			case 4:
				drctn = drctn + 45.0;
				break;
		}

		var a = WithLowBits(drctn, Ftol(drctn) % 360) * DegreesToRadians;
		plane.EmergencyDestination = new WorldPosition(
			cos(a) * 600 + plane.Position.X,
			sin(a) * 600 + plane.Position.Y
		);
		plane.EmergencyFlags[0] = plane.FormationNumber % 6 * 100 + 120;
		plane.PathX[0] = target.Position.X;
		plane.PathY[0] = target.Position.Y;
		plane.PathX[1] = PathEnd;
		plane.IsStopping = false;
	}

	// Step 8: an attacker or bomber of the fleet takes its first target (two times in 3) or a random one of its targets,
	// visible and within 1000.
	private void AttackFleetsTarget(ref ComputerFleet fleet, ref Unit plane)
	{
		if (plane.Target != 0)
			return;

		var t1 = fleet.Targets[1];
		var index =
			t1 != 0 && _game.Units[t1].IsFound && _game.rand() % 3 != 0 ? 1 : _game.rand() % 17 + 1;
		var t = fleet.Targets[index];
		if (
			t != 0
			&& _game.Units[t].IsFound
			&& !(plane.Weapon == FireKind.Torpedo && IsGroundFacility(_game.Units[t].Kind))
			&& DistanceBetween(ref _game.Units[t], ref plane) < 1000
		)
			AttackWith(ref plane, t);
	}

	// Step 8: an attacker or bomber takes, of eleven tries, the target near the destination (one time in 10) or a random
	// enemy ship, visible and within 1000.
	private void AttackAnyShip(ref Unit plane, int near)
	{
		for (var n = 0; n <= 10 && plane.Target == 0; n++)
		{
			var t =
				(short)near != 0 && _game.rand() % 10 == 0
					? (short)near
					: _game.rand() % _game.MaxUnitId + 1;
			ref var enemy = ref _game.Units[t];
			if (
				enemy.IsUsed
				&& enemy.Side != _game.CpuSide
				&& enemy.IsFound
				&& enemy.Kind != UnitKind.Submarine
				&& enemy.Category == UnitCategory.Ship
				&& !(plane.Weapon == FireKind.Torpedo && IsGroundFacility(enemy.Kind))
				&& DistanceBetween(ref enemy, ref plane) < 1000
			)
				AttackWith(ref plane, t);
		}
	}

	// Steps 7 and 8, for unit i: its target.
	public void ChooseTargetOf(int i, int f, ref ComputerFleet fleet, int near)
	{
		ref var unit = ref _game.Units[i];
		if (unit.Side == _game.CpuSide && unit.Target != 0 && unit.Kind != UnitKind.Transport)
		{
			ref var target = ref _game.Units[unit.Target];
			var keeps = target.IsFound && target.IsUsed && target.PlaneState != UnitState.Parked;
			if (!keeps && !(unit.Kind == UnitKind.Fighter && unit.AirSuperioritySortie == 1))
				unit.Target = 0;
		}

		for (var k = 0; k <= 17; k++)
		{
			var isMember =
				fleet.Members[k] == i
				&& unit.Side == _game.CpuSide
				&& unit.Category == UnitCategory.Ship
				&& unit.Ammo >= 1
				&& unit.Kind
					is not (UnitKind.Carrier or UnitKind.LightCarrier or UnitKind.Transport);
			if (
				_game.Units[fleet.Flagship].Kind
					is UnitKind.Carrier
						or UnitKind.LightCarrier
						or UnitKind.Transport
				&& isMember
				&& unit.Target == 0
			)
				ChooseEscortsTarget(ref unit);
			else if (isMember)
				ChooseWarshipsTarget(ref fleet, ref unit);
		}

		if (unit.Target == 0 && fleet.Targets[1] == 0 && (short)near == 0 && _game.rand() % 20 == 1)
		{
			if (
				IsInFleet(i, f)
				&& unit.Side == _game.CpuSide
				&& unit.Category == UnitCategory.Plane
				&& unit.PlaneState == UnitState.Flying
				&& unit.Mode != UnitMode.Return
			)
			{
				unit.Mode = UnitMode.Return;
				unit.GroupLeader = 0;
				if (unit.Kind is UnitKind.Attacker or UnitKind.Bomber)
				{
					unit.Ammo = 0;
					unit.Target = 0;
				}

				_game.StopWhereItIs(ref unit);
				return;
			}
		}

		if (!IsInFleet(i, f) && _game.Units[unit.Carrier].IsUsed)
			return;

		if (
			unit.Side != _game.CpuSide
			|| unit.Kind is not (UnitKind.Attacker or UnitKind.Bomber)
			|| unit.PlaneState != UnitState.Flying
			|| unit.Mode == UnitMode.Return
			|| unit.Target != 0
			|| unit.Ammo == 0
		)
			return;

		if (
			(_game.rand() % 10 == 0 && !(unit.Kind == UnitKind.Bomber && fleet.Targets[1] != 0))
			|| !_game.Units[unit.Carrier].IsUsed
		)
		{
			AttackAnyShip(ref unit, near);
		}
		else if ((short)near != 0)
		{
			if (DistanceBetween(ref _game.Units[near], ref unit) < 1000)
				AttackWith(ref unit, (short)near);
		}
		else
		{
			AttackFleetsTarget(ref fleet, ref unit);
		}
	}
}
