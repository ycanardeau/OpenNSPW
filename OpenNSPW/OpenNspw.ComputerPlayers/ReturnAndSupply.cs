using static OpenNspw.ComputerPlayers.StrategyBasics;
using static OpenNspw.Nspw;

namespace OpenNspw.ComputerPlayers;

// cpu_strtgy's step 9, its first part: whether a fleet returns to its naval base, its supply there, and a flagship stopped
// at the edge of the map (docs/ComputerPlayer.md, cpu_strtgy).
public readonly struct ReturnAndSupply(Nspw game)
{
	private readonly Nspw _game = game;

	// Step 9: the fleet returns to the naval base; its flagship stops.
	private void ReturnToBase(ref ComputerFleet fleet, int naval)
	{
		fleet.Base = (short)naval;
		_game.StopWhereItIs(ref _game.Units[fleet.Flagship]);
	}

	// Step 9: the fleet's dead targets dropped, and whether it returns to its nearest naval base.
	public void DecideToReturn(ref ComputerFleet fleet)
	{
		if (fleet.Flagship == 0)
			return;

		var removed = 0;
		for (var k = 1; k <= 17; k++)
		{
			var t = fleet.Targets[k];
			if (t != 0 && !_game.Units[t].IsUsed)
			{
				fleet.Targets[k] = 0;
				removed++;
			}
		}

		if (removed != 0)
			Pack(fleet.Targets, 1, 17);

		ref var flagship = ref _game.Units[fleet.Flagship];
		var naval = 0;
		var nearest = -1.0;
		for (var i = 1; i <= _game.MaxUnitId; i++)
		{
			ref var unit = ref _game.Units[i];
			if (!unit.IsUsed || unit.Kind != UnitKind.NavalBase || unit.Side != _game.CpuSide)
				continue;

			var distance = DistanceBetween(ref unit, ref flagship);
			if (nearest == -1.0 || distance < nearest)
			{
				nearest = distance;
				naval = i;
			}
		}

		if (fleet.Base != 0 || flagship.Kind == UnitKind.AirBase || naval == 0)
			return;

		if (flagship.Kind is UnitKind.Carrier or UnitKind.LightCarrier && flagship.IsUsed)
		{
			var attackers = 0;
			var planes = 0;
			for (var i = 1; i <= _game.MaxUnitId; i++)
			{
				ref var unit = ref _game.Units[i];
				if (!unit.IsUsed || unit.Carrier != fleet.Flagship)
					continue;

				if (unit.Kind == UnitKind.Attacker)
					attackers++;
				planes++;
			}

			if (_game.ScenarioNumber == 303 && flagship.Kind == UnitKind.LightCarrier)
				attackers = 3;

			if (
				planes == 0
				|| (attackers <= 2 && _game.Units[fleet.Targets[1]].Category != UnitCategory.Plane)
				|| flagship.HP <= flagship.MaxHP / 2
			)
				ReturnToBase(ref fleet, naval);
		}

		if (fleet.Base == 0 && flagship.Kind == UnitKind.Transport)
		{
			var hasCargo = false;
			for (var k = 0; k <= 17 && !hasCargo; k++)
			{
				var m = fleet.Members[k];
				hasCargo =
					m != 0
					&& _game.Units[m].IsUsed
					&& _game.Units[m].Kind == UnitKind.Transport
					&& _game.Units[m].Ammo != 0;
			}

			if (!hasCargo)
				ReturnToBase(ref fleet, naval);
		}

		if (
			fleet.Base == 0
			&& flagship.Kind >= UnitKind.Battleship
			&& flagship.Kind <= UnitKind.Destroyer
			&& _game.BattleTime >= 20000
		)
		{
			var hp = 0;
			var maxHP = 0;
			for (var k = 0; k <= 17; k++)
			{
				var m = fleet.Members[k];
				if (m != 0 && _game.Units[m].IsUsed)
				{
					hp += _game.Units[m].HP;
					maxHP += _game.Units[m].MaxHP;
				}
			}

			if (!(maxHP * 0.65 < hp))
				ReturnToBase(ref fleet, naval);
		}

		if (fleet.Base == 0)
		{
			var ammo = 0;
			var maxAmmo = 0;
			for (var k = 0; k <= 17; k++)
			{
				var m = fleet.Members[k];
				if (m != 0 && _game.Units[m].IsUsed)
				{
					maxAmmo += _game.Units[m].MaxAmmo;
					if (_game.Units[m].Ammo >= 1)
						ammo += _game.Units[m].Ammo;
				}
			}

			var percent = Ftol((double)ammo / maxAmmo * 100.0f);
			ref var first = ref _game.Units[fleet.Members[0]];
			if (percent < 20 || !(first.MaxAmmo * 0.2 <= first.Ammo))
				ReturnToBase(ref fleet, naval);
		}

		if (fleet.Base == 0)
		{
			// Unit 23, wherever it is, stands for the computer's home.
			var distance = DistanceBetween(ref _game.Units[23], ref flagship);
			var range =
				(double)(int)flagship.gas[1] * (int)flagship.gas[0] * flagship.MaxSpeed * 0.9;
			if (!(range > distance) && _game.BattleTime > 10000)
				ReturnToBase(ref fleet, naval);
		}
	}

	// Step 9: a returning fleet whose flagship has stopped at its base, with no enemy near, supplies; a carrier gets its
	// lost planes back.
	public void SupplyAtBase(ref ComputerFleet fleet)
	{
		if (fleet.Flagship == 0)
			return;

		ref var flagship = ref _game.Units[fleet.Flagship];
		if (
			flagship.Kind == UnitKind.AirBase
			|| fleet.Base == 0
			|| !flagship.IsStopping
			|| flagship.SupplyTime != 0
		)
			return;

		ref var naval = ref _game.Units[fleet.Base];
		RECT rect;
		var y = Ftol(naval.Position.Y);
		var x = Ftol(naval.Position.X);
		rect.top = y - 580;
		rect.right = x + 580;
		rect.bottom = y + 580;
		rect.left = x - 580;
		if (!_game.PointInRect(ref rect, Ftol(flagship.Position.X), Ftol(flagship.Position.Y)))
			return;

		for (var i = 1; i <= _game.MaxUnitId; i++)
		{
			ref var enemy = ref _game.Units[i];
			if (!enemy.IsUsed || enemy.Side == _game.CpuSide || !enemy.IsFound)
				continue;

			var threatens =
				enemy.Kind is UnitKind.Battleship or UnitKind.Cruiser or UnitKind.Destroyer
				|| (
					enemy.Kind is UnitKind.Attacker or UnitKind.Bomber
					&& enemy.PlaneState == UnitState.Flying
				)
				|| (enemy.Kind == UnitKind.Submarine && !enemy.IsSubmerged);
			if (threatens && !(DistanceBetween(ref enemy, ref flagship) > 900))
				return;
		}

		flagship.SupplyTime = 1;
		for (var i = 1; i <= _game.MaxUnitId; i++)
		{
			if (_game.Units[i].IsUsed && _game.Units[i].GroupLeader == fleet.Flagship)
				_game.Units[i].SupplyTime = 1;
		}

		fleet.Base = 0;
		if (flagship.Kind is not (UnitKind.Carrier or UnitKind.LightCarrier))
			return;

		int carrier = fleet.Flagship;
		var planes = 0;
		if (_game.ScenarioNumber == 204)
		{
			for (var i = 1; i <= _game.MaxUnitId; i++)
			{
				// 1.22 compares with the register that holds 1 by then, not with cpu_side.
				if (
					_game.Units[i].Category == UnitCategory.Plane
					&& _game.Units[i].Carrier == carrier
					&& _game.Units[i].Side == Side.Japan
				)
					planes++;
			}

			_game.AddPlane(Side.Japan, UnitKind.Attacker, 0, carrier, 12 - planes, FireKind.Bomb);
			return;
		}

		for (var i = 1; i <= _game.MaxUnitId; i++)
		{
			if (
				_game.Units[i].Category == UnitCategory.Plane
				&& _game.Units[i].Carrier == carrier
				&& _game.Units[i].IsUsed
			)
				planes++;
		}

		for (var i = 1; i <= _game.MaxUnitId; i++)
		{
			ref var plane = ref _game.Units[i];
			if (
				plane.Category != UnitCategory.Plane
				|| plane.Carrier != fleet.Flagship
				|| plane.IsUsed
			)
				continue;

			plane.Side = _game.CpuSide;
			plane.Position = new WorldPosition(730, 150);
			plane.Category = UnitCategory.Plane;
			plane.PlaneState = UnitState.Parked;
			plane.ParkingNumber = planes;
			plane.DeckPhase = 0;
			plane.TakeOffRun = 0;
			plane.Mode = UnitMode.Move;
			_game.SetParkingPosition(i);
			plane.IsStopping = true;
			_game.Units[plane.Carrier].PlaneCount++;
			_game.SetUnitData(i);
			planes++;
		}
	}

	// Step 9: a flagship at the edge of the map whose path leaves it stops; a stopped flagship puts the fleet in state 50.
	public void StopAtTheEdge(ref ComputerFleet fleet)
	{
		if (fleet.Flagship == 0)
			return;

		ref var flagship = ref _game.Units[fleet.Flagship];
		if (flagship.Kind == UnitKind.AirBase)
			return;

		var supplying = false;
		for (var k = 0; MemberAt(ref fleet, k) != 0; k++)
		{
			if (_game.Units[MemberAt(ref fleet, k)].SupplyTime != 0)
			{
				supplying = true;
				break;
			}
		}

		if (!flagship.IsUsed)
			return;

		if (!supplying && !flagship.IsStopping && fleet.Base == 0)
		{
			var x = flagship.Position.X;
			var y = flagship.Position.Y;
			var atEdge = x <= -9500 || !(x < 9500) || !(y < 7100) || y <= -7100;
			var px = flagship.PathX[0];
			var py = flagship.PathY[0];
			var leaves = px <= -9600 || !(px < 9600) || !(py < 7200) || py <= -7200;
			if (atEdge && leaves)
				_game.StopWhereItIs(ref flagship);
		}

		if (!supplying && flagship.IsStopping && fleet.Mode != 0)
			fleet.State = 50;
	}
}
