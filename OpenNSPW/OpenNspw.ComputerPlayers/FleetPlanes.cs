using static OpenNspw.ComputerPlayers.StrategyBasics;

namespace OpenNspw.ComputerPlayers;

// cpu_strtgy's steps 5 and 6: the planes of a fleet's carriers and air bases armed, and the computer's planes in the air
// sent home or on to their next strike (docs/ComputerPlayer.md, cpu_strtgy).
public readonly struct FleetPlanes(Nspw game)
{
	private readonly Nspw _game = game;

	// Step 5: the computer's unarmed attackers and bombers parked on the fleet's carriers and air bases get their weapons.
	public void ArmComputersPlanes(ref ComputerFleet fleet, int near)
	{
		for (var k = 0; k <= 17; k++)
		{
			var s = fleet.Members[k];
			ref var member = ref _game.Units[s];
			if (
				!member.IsUsed
				|| member.Kind
					is not (UnitKind.Carrier or UnitKind.LightCarrier or UnitKind.AirBase)
			)
				continue;

			for (var p = 1; p <= _game.MaxUnitId; p++)
			{
				ref var plane = ref _game.Units[p];
				if (
					!plane.IsUsed
					|| plane.Side != _game.CpuSide
					|| !plane.IsStopping
					|| plane.Kind is not (UnitKind.Attacker or UnitKind.Bomber)
					|| plane.Weapon != FireKind.Unarmed
					|| plane.Carrier != s
				)
					continue;

				if (
					plane.Kind == UnitKind.Bomber
					|| _game.CpuSide == Side.UnitedStates
					|| IsGroundFacility(_game.Units[fleet.Targets[1]].Kind)
					|| _game.ScenarioNumber == 204
					|| ((short)near != 0 && IsGroundFacility(_game.Units[near].Kind))
				)
					plane.Weapon = FireKind.Bomb;
				else
					plane.Weapon = FireKind.Torpedo;

				plane.Ammo = plane.MaxAmmo;
				plane.ReloadTime = 700;
			}
		}
	}

	// A plane of the computer and the planes that follow it fly home.
	private void ReturnWithFollowers(int m)
	{
		ref var plane = ref _game.Units[m];
		plane.Mode = UnitMode.Return;
		plane.Ammo = 0;
		plane.Target = 0;
		_game.StopWhereItIs(ref plane);
		for (var j = 1; j <= _game.MaxUnitId; j++)
		{
			ref var follower = ref _game.Units[j];
			if (!follower.IsUsed || follower.GroupLeader != m)
				continue;

			follower.GroupLeader = 0;
			follower.Mode = UnitMode.Return;
			if (follower.Kind is UnitKind.Attacker or UnitKind.Bomber)
			{
				follower.Ammo = 0;
				follower.Target = 0;
			}

			_game.StopWhereItIs(ref follower);
		}
	}

	// An attacker or bomber of the computer that reached its destination: its next one, from the targets of the fleet
	// whose flagship is its carrier.
	private void ChooseNextStrike(ref Unit plane)
	{
		for (var g = 1; g <= 10; g++)
		{
			ref var fleet = ref _game.FleetAt(g);
			if (fleet.Flagship != plane.Carrier)
				continue;

			if (plane.Kind == UnitKind.Bomber && fleet.Targets[1] != 0)
			{
				ref var target = ref _game.Units[fleet.Targets[1]];
				plane.PathX[0] = target.Position.X;
				plane.PathY[0] = target.Position.Y;
				var a = _game.rand() % 360 * DegreesToRadians;
				plane.PathX[0] = cos(a) * 500 + plane.PathX[0];
				plane.PathY[0] = sin(a) * 500 + plane.PathY[0];
				continue;
			}

			var t = fleet.Targets[_game.rand() % 17 + 1];
			if (t != 0 && _game.Units[t].IsUsed)
			{
				ref var target = ref _game.Units[t];
				var distance = DistanceBetween(ref target, ref plane);
				if (distance < 2000)
				{
					var lead = distance * 0.5;
					plane.PathX[0] = target.Position.X;
					plane.PathY[0] = target.Position.Y;
					var a = target.Direction.Degrees * DegreesToRadians;
					plane.PathX[0] = cos(a) * (lead * target.Speed) + plane.PathX[0];
					plane.PathY[0] = sin(a) * (lead * target.Speed) + plane.PathY[0];
					plane.Target = t;
				}
			}
			else
			{
				plane.PathX[0] = plane.Position.X;
				plane.PathY[0] = plane.Position.Y;
				var a = _game.rand() % 360 * DegreesToRadians;
				plane.PathX[0] = cos(a) * 500 + plane.PathX[0];
				plane.PathY[0] = sin(a) * 500 + plane.PathY[0];
			}
		}
	}

	// Step 6: the computer's planes in the air.
	public void FlyComputersPlanes()
	{
		for (var i = 1; i <= _game.MaxUnitId; i++)
		{
			ref var plane = ref _game.Units[i];
			var leads = plane.GroupLeader == 0 || plane.IsGroupLeader != 0;
			if (
				plane.Side == _game.CpuSide
				&& plane.Kind is UnitKind.Attacker or UnitKind.Bomber
				&& plane.PlaneState == UnitState.Flying
				&& leads
				&& plane.Mode != UnitMode.Return
			)
			{
				if ((int)plane.Fuel <= 55 && plane.Target == 0)
				{
					ReturnWithFollowers(i);
				}
				else
				{
					RECT rect;
					rect.top = Ftol(plane.PathY[0]) - 40;
					rect.right = Ftol(plane.PathX[0]) + 40;
					rect.bottom = Ftol(plane.PathY[0]) + 40;
					rect.left = Ftol(plane.PathX[0]) - 40;
					if (_game.PointInRect(ref rect, Ftol(plane.Position.X), Ftol(plane.Position.Y)))
						ChooseNextStrike(ref plane);
				}
			}

			if (
				(int)plane.Fuel <= 60
				&& plane.Side == _game.CpuSide
				&& plane.Kind == UnitKind.Fighter
				&& plane.PlaneState == UnitState.Flying
				&& plane.Mode != UnitMode.Return
				&& _game.rand() % 10 == 1
			)
			{
				plane.Mode = UnitMode.Return;
				plane.GroupLeader = 0;
				_game.StopWhereItIs(ref plane);
			}

			leads = plane.GroupLeader == 0 || plane.IsGroupLeader != 0;
			if (
				plane.Side == _game.CpuSide
				&& plane.Kind == UnitKind.Fighter
				&& plane.PlaneState == UnitState.Flying
				&& leads
				&& plane.Mode != UnitMode.Return
				&& plane.Target == 0
				&& plane.AirSuperioritySortie == 0
			)
			{
				plane.Mode = UnitMode.Return;
				for (var j = 1; j <= _game.MaxUnitId; j++)
				{
					ref var follower = ref _game.Units[j];
					if (follower.IsUsed && follower.GroupLeader == i)
					{
						follower.GroupLeader = 0;
						follower.Mode = UnitMode.Return;
					}
				}
			}
		}
	}
}
