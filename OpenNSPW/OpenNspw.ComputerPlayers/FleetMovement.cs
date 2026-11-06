using static OpenNspw.ComputerPlayers.StrategyBasics;
using static OpenNspw.Nspw;

namespace OpenNspw.ComputerPlayers;

// cpu_strtgy's step 9, its moves: the fleet's next move, by its flagship's kind and its distance to what it aims at
// (docs/ComputerPlayer.md, cpu_strtgy).
public readonly struct FleetMovement(Nspw game)
{
	private readonly Nspw _game = game;

	// Step 9: the move of a fleet with nothing to aim at, by its mode.
	private void MoveByMode(ref ComputerFleet fleet, ref Unit flagship, short mode)
	{
		if (mode == 1)
		{
			if (flagship.Kind == UnitKind.Transport)
			{
				fleet.PathX[0] = fleet.AimX;
				fleet.PathY[0] = fleet.AimY;
				var a = _game.rand() % 360 * DegreesToRadians;
				fleet.PathX[0] = (_game.rand() % 150 + 100) * cos(a) + fleet.PathX[0];
				fleet.PathY[0] = (_game.rand() % 150 + 100) * sin(a) + fleet.PathY[0];
			}
			else
			{
				fleet.AimX = flagship.Position.X;
				fleet.PathX[0] = flagship.Position.X;
				fleet.AimY = flagship.Position.Y;
				fleet.PathY[0] = flagship.Position.Y;
				var a = _game.rand() % 360 * DegreesToRadians;
				fleet.PathX[0] = cos(a) * 500 + fleet.PathX[0];
				fleet.PathY[0] = sin(a) * 500 + fleet.PathY[0];
			}
		}
		else if (mode == 50)
		{
			fleet.PathX[0] = fleet.SecondX;
			fleet.PathY[0] = fleet.SecondY;
			var a = _game.rand() % 360 * DegreesToRadians;
			fleet.PathX[0] = cos(a) * 300 + fleet.PathX[0];
			fleet.PathY[0] = sin(a) * 300 + fleet.PathY[0];
		}
	}

	// Step 9: a submarine's next move, by its distance to the aim and its bearing b.
	private StrategyFlow MoveSubmarine(
		ref ComputerFleet fleet,
		ref Unit flagship,
		double distance,
		double b
	)
	{
		double length;
		if (flagship.ReloadTime != 0)
		{
			if (!(distance <= 1500))
				return StrategyFlow.Return;

			var r = _game.rand() % 40;
			b = Ftol((double)r + b + 160.0) % 360;
			length = _game.rand() % 200 + 100;
		}
		else if (!(distance < 800))
		{
			var turned = b + 20;
			b = Ftol(turned - _game.rand() % 40) % 360;
			length = _game.rand() % 500 + 100;
		}
		else
		{
			if (distance < 200)
				return StrategyFlow.Return;

			MoveFrom(ref fleet, ref flagship, b, 120);
			return StrategyFlow.Order;
		}

		MoveFrom(ref fleet, ref flagship, b, length);
		return StrategyFlow.Order;
	}

	// A bearing turned by a random amount: (b - from) plus rand()%range, in whole degrees.
	private int TurnedFrom(double b, double from, int range)
	{
		var r = _game.rand() % range;
		return Ftol(r + (b - from)) % 360;
	}

	// A bearing turned back by a random amount: (b + by) minus rand()%range, in whole degrees.
	private int TurnedBack(double b, double by, int range)
	{
		var turned = b + by;
		return Ftol(turned - _game.rand() % range) % 360;
	}

	// Step 9: a battleship's, cruiser's or destroyer's next move, by its distance to the aim and its bearing b.
	private void MoveWarship(ref ComputerFleet fleet, ref Unit flagship, double distance, double b)
	{
		if (!(distance < 7000))
		{
			var far = (double)(_game.rand() % 3000 + 1500);
			MoveFrom(ref fleet, ref flagship, TurnedFrom(b, 70, 140), far);
			return;
		}

		int direction;
		int length;
		if (!(distance < 3000))
		{
			direction = TurnedFrom(b, 70, 140);
			length = _game.rand() % 1000 + 800;
		}
		else if (!(distance < 1500))
		{
			direction = TurnedFrom(b, 45, 90);
			length = _game.rand() % 300 + 400;
		}
		else if (!(distance < 700))
		{
			direction = (_game.rand() % 7) switch
			{
				0 => TurnedFrom(b, 100, 65),
				1 => TurnedBack(b, 100, 65),
				_ => TurnedBack(b, 20, 40),
			};
			length = _game.rand() % 300 + 200;
		}
		else
		{
			direction = (_game.rand() % 3) switch
			{
				0 => TurnedFrom(b, 100, 65),
				1 => TurnedBack(b, 100, 65),
				_ => TurnedBack(b, 20, 40),
			};
			length = _game.rand() % 200 + 300;
		}

		MoveFrom(ref fleet, ref flagship, direction, length);
	}

	// Step 9: the carrier's move 500 from it at the direction, and whether that move keeps more than 2150 from the
	// facility.
	private static bool TryCarriersMove(
		ref ComputerFleet fleet,
		ref Unit flagship,
		ref Unit facility,
		double direction
	)
	{
		var a = direction * DegreesToRadians;
		fleet.PathX[0] = flagship.Position.X;
		fleet.PathY[0] = flagship.Position.Y;
		fleet.PathX[0] = cos(a) * 500 + fleet.PathX[0];
		fleet.PathY[0] = sin(a) * 500 + fleet.PathY[0];
		return DistanceOf(
				facility.Position.X - fleet.PathX[0],
				facility.Position.Y - fleet.PathY[0]
			) > 2150;
	}

	// Step 9: a carrier's next move, by its distance to the aim and its bearing b, kept away from enemy ground
	// facilities.
	private StrategyFlow MoveCarrier(
		ref ComputerFleet fleet,
		ref Unit flagship,
		double distance,
		double b
	)
	{
		for (var i = 1; i <= _game.MaxUnitId; i++)
		{
			ref var plane = ref _game.Units[i];
			if (
				plane.Side == _game.CpuSide
				&& plane.Category == UnitCategory.Plane
				&& plane.PlaneState == UnitState.Flying
				&& plane.Carrier == fleet.Flagship
				&& plane.Mode == UnitMode.Return
			)
			{
				if (!(distance <= 3700))
					return StrategyFlow.Return;

				MoveFrom(ref fleet, ref flagship, Ftol(b + 180) % 360, 500);
				return StrategyFlow.Order;
			}
		}

		var japan = _game.CpuSide == Side.Japan ? 800 : 0;
		int direction;
		int length;
		if (japan + 7000 <= distance)
		{
			direction = TurnedBack(b, 20, 40);
			length = _game.rand() % 500 + 1500;
		}
		else if (japan + 4500 <= distance)
		{
			direction = TurnedFrom(b, 70, 140);
			length = _game.rand() % 500 + 1300;
		}
		else if (japan + 3550 <= distance)
		{
			direction = TurnedFrom(b, 20, 40);
			length = _game.rand() % 300 + 800;
		}
		else
		{
			var r = _game.rand() % 130;
			direction = Ftol((double)r + b + 115) % 360;
			length = _game.rand() % 500 + 1300;
		}

		MoveFrom(ref fleet, ref flagship, direction, length);
		for (var j = 1; j <= _game.MaxUnitId; j++)
		{
			ref var facility = ref _game.Units[j];
			if (
				!facility.IsUsed
				|| facility.Side == _game.CpuSide
				|| !IsGroundFacility(facility.Kind)
			)
				continue;

			if (
				!(
					DistanceOf(
						facility.Position.X - fleet.PathX[0],
						facility.Position.Y - fleet.PathY[0]
					) <= 2150
				)
			)
				continue;

			var away =
				atan2(fleet.PathY[0] - flagship.Position.Y, fleet.PathX[0] - flagship.Position.X)
					* RadiansToDegrees
				- 360;
			var once = _game.rand() % 10 == 0 ? 1 : 0;
			var k = once * (_game.rand() % 130) + 45;
			var cleared = false;
			for (; k <= 180 && !cleared; k++)
				cleared =
					TryCarriersMove(ref fleet, ref flagship, ref facility, away - k)
					|| TryCarriersMove(ref fleet, ref flagship, ref facility, k + away);

			if (cleared)
				continue;

			var bearing =
				atan2(
					facility.Position.Y - flagship.Position.Y,
					facility.Position.X - flagship.Position.X
				) * RadiansToDegrees;
			if (bearing < 0)
				bearing = bearing + 360;

			var aside =
				_game.rand() % 2 == 1 ? TurnedFrom(bearing, 100, 20) : TurnedBack(bearing, 100, 20);
			MoveFrom(ref fleet, ref flagship, aside, _game.rand() % 800 + 300);
		}

		return StrategyFlow.Order;
	}

	// Step 9: the fleet's next move, when its flagship has stopped in state 50.
	public StrategyFlow MoveFleet(ref ComputerFleet fleet, int near)
	{
		if (fleet.Flagship == 0)
			return StrategyFlow.Order;

		ref var flagship = ref _game.Units[fleet.Flagship];
		var kind = flagship.Kind;
		if (
			kind == UnitKind.AirBase
			|| fleet.State != 50
			|| flagship.PathX[1] != PathEnd
			|| !flagship.IsStopping
			|| flagship.SupplyTime != 0
		)
			return StrategyFlow.Next;

		if (fleet.Base != 0)
		{
			ref var naval = ref _game.Units[fleet.Base];
			var x = (double)(_game.rand() % 400) + naval.Position.X + 100;
			fleet.AimX = x;
			fleet.PathX[0] = x;
			var north = naval.Position.Y - 100;
			var y = north - _game.rand() % 400;
			fleet.AimY = y;
			fleet.PathY[0] = y;
			return StrategyFlow.Order;
		}

		var first = 1;
		while (first <= 17 && fleet.Targets[first] == 0)
			first++;

		var mode = fleet.Mode;
		if (mode == 50)
			first = 18;

		var hasNoTarget = first == 18 && (flagship.Target == 0 || mode == 50) && (short)near == 0;
		if (hasNoTarget || kind == UnitKind.Transport)
		{
			MoveByMode(ref fleet, ref flagship, mode);
			return StrategyFlow.Order;
		}

		if (flagship.Target != 0 && _game.Units[flagship.Target].IsFound)
		{
			fleet.AimX = _game.Units[flagship.Target].Position.X;
			fleet.AimY = _game.Units[flagship.Target].Position.Y;
		}
		else if ((short)near != 0)
		{
			fleet.AimX = _game.Units[(short)near].Position.X;
			fleet.AimY = _game.Units[(short)near].Position.Y;
		}
		else
		{
			ref var target = ref _game.Units[TargetAt(ref fleet, first)];
			ref var aimed = ref target.Category == UnitCategory.Plane
			&& target.PlaneState == UnitState.Parked
				? ref _game.Units[target.Carrier]
				: ref target;
			fleet.AimX = aimed.Position.X;
			fleet.AimY = aimed.Position.Y;
		}

		var dx = fleet.AimX - flagship.Position.X;
		var dy = fleet.AimY - flagship.Position.Y;
		var distance = DistanceOf(dx, dy);
		var b = atan2(dy, dx) * RadiansToDegrees;
		if (b < 0)
			b = b + 360;

		switch (kind)
		{
			case UnitKind.Carrier:
			case UnitKind.LightCarrier:
				return MoveCarrier(ref fleet, ref flagship, distance, b);
			case UnitKind.Submarine:
				return MoveSubmarine(ref fleet, ref flagship, distance, b);
			default:
				MoveWarship(ref fleet, ref flagship, distance, b);
				return StrategyFlow.Order;
		}
	}
}
