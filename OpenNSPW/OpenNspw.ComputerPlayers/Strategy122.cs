using System.Runtime.CompilerServices;
using static OpenNspw.Nspw;

namespace OpenNspw.ComputerPlayers;

// NSPW 1.22's cpu_strtgy (0x404E30), rewritten from its machine code (docs/ComputerPlayer.md, cpu_strtgy): the
// computer's order of the frame, for one of its fleets, in the game it is given. It is tested against the emulated
// original (ComputerPlayerTests). A struct, so that a frame's strategy allocates nothing.
internal readonly struct Strategy122(Nspw game)
{
	private readonly Nspw _game = game;

	// Where cpu_strtgy goes after a step: on to the next, to the reinforcements of scenario 304 and the order (when the
	// fleet has given its order, or has none to give), or out, without an order.
	private enum StrategyFlow
	{
		Next,
		Order,
		Return,
		NoFleet,
	}

	// The end of a path, x 9601 (World.Beyond).
	private const double PathEnd = World.Beyond;

	// The planes' and ships' kinds that the computer's code names together.
	private static bool IsGroundFacility(UnitKind kind) =>
		kind >= UnitKind.AirBase && kind <= UnitKind.Fortress;

	private static bool IsLandForce(UnitKind kind) =>
		kind >= UnitKind.InfantryBase && kind <= UnitKind.Fortress;

	// The distance of (dx, dy) as 1.22 computes it everywhere: the x component over the cosine of the direction folded
	// into 0 to 90°, with a component of 0 taken as 1.
	private static double DistanceOf(double dx, double dy)
	{
		if (dx == 0)
			dx = 1;
		if (dy == 0)
			dy = 1;

		var drctn = atan2(dy, dx) * RadiansToDegrees;
		if (drctn < 0)
			drctn = drctn + 360;
		if (dx < 0)
			dx = -dx;
		if (drctn >= 180)
			drctn = drctn - 180;
		if (drctn >= 90)
			drctn = 90 - (drctn - 90);
		return dx / cos(drctn * DegreesToRadians);
	}

	private static double DistanceBetween(ref Unit to, ref Unit from)
	{
		return DistanceOf(to.Position.X - from.Position.X, to.Position.Y - from.Position.Y);
	}

	// A double whose low 32 bits are replaced: 1.22 stores an int into the low half of a double, and reads the double
	// back (0x406A6D), whose high half, with its sign and exponent, is unchanged.
	private static double WithLowBits(double value, int low)
	{
		return BitConverter.Int64BitsToDouble(
			(BitConverter.DoubleToInt64Bits(value) & unchecked((long)0xFFFFFFFF00000000))
				| (uint)low
		);
	}

	// A plane parked at its base and ready to take off, as the computer selects its planes.
	private static bool IsReadyToTakeOff(ref Unit plane)
	{
		return plane.IsStopping
			&& plane.PlaneState == UnitState.Parked
			&& plane.Ammo != 0
			&& plane.ReloadTime == 0
			&& plane.Weapon != FireKind.Unarmed
			&& plane.Weapon != FireKind.Maintenance;
	}

	// Packs the list from start to last, moving each unit to the first empty element before it; after a move, the scan
	// starts again after start.
	private static void Pack(Span<short> list, int start, int last)
	{
		var empty = -1;
		for (var i = start; i <= last; i++)
		{
			var unit = list[i];
			if (unit == 0 && empty == -1)
				empty = i;

			if (empty != -1 && unit != 0)
			{
				list[empty] = unit;
				list[i] = 0;
				empty = -1;
				i = start;
			}
		}
	}

	// sub_414DD0: moves the point 80 down until no ship's square (the size of the units' sprite) covers it.
	internal void PlaceReinforcement(ref double x, ref double y)
	{
		for (var i = 1; i <= _game.MaxUnitId; i++)
		{
			ref var unit = ref _game.Units[i];
			if (!unit.IsUsed || unit.Category != UnitCategory.Ship)
				continue;

			var unitY = Ftol(unit.Position.Y);
			var unitX = Ftol(unit.Position.X);
			var halfHeight = _game.Renderer.Sprites[SpriteId.JapanUnits].Size.Height / 2;
			var halfWidth = _game.Renderer.Sprites[SpriteId.JapanUnits].Size.Width / 2;
			var square = new RECT
			{
				left = unitX - halfWidth,
				top = unitY - halfHeight,
				right = unitX + halfWidth,
				bottom = unitY + halfHeight,
			};
			if (_game.PointInRect(ref square, Ftol(x), Ftol(y)))
			{
				y = y - 80;
				i = 0;
			}
		}
	}

	// sub_403AE0's places of the escorts around a fleet's transports or carriers, by their number, 1 to 6: (bearing,
	// distance) to the flagship.
	private static readonly (double Bearing, double Distance)[][] EscortPlaces =
	[
		[(0, 180)],
		[(0, 240), (180, 180)],
		[(0, 300), (135, 260), (225, 260)],
		[(0, 300), (90, 280), (180, 280), (270, 280)],
		[(0, 390), (65, 360), (295, 360), (135, 360), (200, 360)],
		[(23, 390), (337, 390), (90, 360), (270, 360), (140, 360), (220, 360)],
	];

	// The first six of more than six escorts.
	private static readonly (double Bearing, double Distance)[] ManyEscortPlaces =
	[
		(23, 340),
		(337, 340),
		(90, 340),
		(270, 340),
		(140, 340),
		(220, 340),
	];

	// The places of the second to fourth transports, by their number, 2 to 4.
	private static readonly (double Bearing, double Distance)[][] TransportPlaces =
	[
		[(90, 100)],
		[(90, 100), (270, 100)],
		[(90, 100), (90, 200), (270, 100)],
	];

	// The places of the second to fourth carriers, by their number, 2 to 4.
	private static readonly (double Bearing, double Distance)[][] CarrierPlaces =
	[
		[(202.5, 140)],
		[(135, 100), (225, 100)],
		[(135, 100), (225, 100), (180, 220)],
	];

	private void PlaceInFormation(int m, double bearing, double distance)
	{
		_game.Units[m].DirectionToLeader = bearing;
		_game.Units[m].DistanceToLeader = distance;
	}

	// sub_403AE0's formation of a fleet led by its transports or carriers (the first members, isLead): the others
	// escort them; past the sixth escort, they follow at 100 apart, at a bearing by their place's parity.
	private void FormAroundLeads(
		ref ComputerFleet fleet,
		Func<UnitKind, bool> isLead,
		bool areTransports
	)
	{
		if (fleet.Members[0] == 0)
			return;

		var leads = 0;
		var escorts = 0;
		for (var k = 0; MemberAt(ref fleet, k) != 0; k++)
		{
			if (isLead(_game.Units[MemberAt(ref fleet, k)].Kind))
				leads++;
			else
				escorts++;
		}

		if (leads >= 2 && leads <= 4)
		{
			var places = (areTransports ? TransportPlaces : CarrierPlaces)[leads - 2];
			for (var j = 0; j < places.Length; j++)
				PlaceInFormation(MemberAt(ref fleet, j + 1), places[j].Bearing, places[j].Distance);
		}

		if (escorts == 0)
			return;

		var escortPlaces = escorts <= 6 ? EscortPlaces[escorts - 1] : ManyEscortPlaces;
		for (var j = 0; j < escortPlaces.Length; j++)
			PlaceInFormation(
				MemberAt(ref fleet, leads + j),
				escortPlaces[j].Bearing,
				escortPlaces[j].Distance
			);

		if (escorts <= 6)
			return;

		for (var k = leads + 6; MemberAt(ref fleet, k) != 0; k++)
		{
			var bearing = areTransports ? (k % 2 + 4) * 40 : k % 2 * 40 + 340;
			PlaceInFormation(MemberAt(ref fleet, k), bearing, 100 * k - 260);
		}
	}

	// sub_403AE0: the formation of fleet f, and its live members selected (slct_unit[0]), following the flagship. A live
	// carrier among the members becomes the first and the flagship; a carrier or transport fleet forms around them; any
	// other, in line behind the flagship, 180 apart.
	internal void SetFormation(int f)
	{
		ref var fleet = ref _game.FleetAt(f);
		var carrierAt = -1;
		if (fleet.Members[0] != 0)
		{
			for (var k = 0; MemberAt(ref fleet, k) != 0; k++)
			{
				ref var member = ref _game.Units[MemberAt(ref fleet, k)];
				if (
					carrierAt == -1
					&& member.Kind is UnitKind.Carrier or UnitKind.LightCarrier
					&& member.IsUsed
				)
					carrierAt = k;
			}
		}

		var n = 1;
		if (carrierAt == -1 && _game.Units[fleet.Members[0]].Kind != UnitKind.Transport)
		{
			var distance = 180;
			for (var k = 1; MemberAt(ref fleet, k) != 0; k++)
			{
				int m = MemberAt(ref fleet, k);
				if (!_game.Units[m].IsUsed)
					continue;

				_game.Selections[0][m] = (short)n++;
				_game.Units[m].GroupLeader = fleet.Flagship;
				PlaceInFormation(m, 180, distance);
				distance += 180;
			}

			return;
		}

		if (carrierAt != -1)
		{
			if (carrierAt != 0)
				(MemberAt(ref fleet, 0), MemberAt(ref fleet, carrierAt)) = (
					MemberAt(ref fleet, carrierAt),
					MemberAt(ref fleet, 0)
				);
			fleet.Flagship = fleet.Members[0];
			FormAroundLeads(
				ref fleet,
				kind => kind is UnitKind.Carrier or UnitKind.LightCarrier,
				false
			);
		}
		else
		{
			FormAroundLeads(ref fleet, kind => kind == UnitKind.Transport, true);
		}

		for (var k = 1; MemberAt(ref fleet, k) != 0; k++)
		{
			int m = MemberAt(ref fleet, k);
			if (!_game.Units[m].IsUsed)
				continue;

			_game.Selections[0][m] = (short)n++;
			_game.Units[m].GroupLeader = fleet.Flagship;
		}
	}

	// sub_403A70: whether plane m belongs to fleet f, its carrier or base being the fleet's flagship.
	private bool IsInFleet(int m, int f)
	{
		return _game.Units[m].Carrier == _game.FleetAt(f).Flagship;
	}

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

	// The fleet's next move: the point at the direction (in degrees) and distance from its flagship.
	private static void MoveFrom(
		ref ComputerFleet fleet,
		ref Unit flagship,
		double direction,
		double distance
	)
	{
		var a = direction * DegreesToRadians;
		fleet.PathY[0] = sin(a) * distance + flagship.Position.Y;
		fleet.PathX[0] = cos(a) * distance + flagship.Position.X;
	}

	// Step 1: the scenario's events.
	private void ApplyComputerEvents()
	{
		switch (_game.ScenarioNumber)
		{
			case 8:
			{
				ref var fleet5 = ref _game.FleetAt(5);
				if (
					fleet5.Flagship != 0
					&& fleet5.Mode == 0
					&& _game.Units[fleet5.Flagship].IsUsed
					&& _game.BattleTime <= 40000
				)
				{
					fleet5.Mode = 1;
					_game.AddTargetsOfKind(5, UnitKind.Transport);
					_game.ShuffleTargets(5);
				}

				ref var fleet7 = ref _game.FleetAt(7);
				if (
					fleet7.Flagship != 0
					&& fleet7.Mode == 50
					&& _game.Units[fleet7.Flagship].IsUsed
					&& _game.BattleTime <= 50000
				)
				{
					fleet7.Mode = 1;
					_game.AddTargetsOfKind(7, UnitKind.Transport);
					_game.ShuffleTargets(7);
				}
				break;
			}

			case 10:
				foreach (var (f, time) in new[] { (2, 60000), (4, 20000) })
				{
					ref var fleet = ref _game.FleetAt(f);
					if (
						fleet.Flagship != 0
						&& fleet.Mode == 0
						&& _game.Units[fleet.Flagship].IsUsed
						&& _game.BattleTime <= time
					)
					{
						_game.AddPlane(
							Side.UnitedStates,
							UnitKind.Bomber,
							0,
							fleet.Flagship,
							16,
							FireKind.Bomb
						);
						fleet.Mode = 1;
						_game.AddTargetsOfKind(f, UnitKind.City);
					}
				}
				break;

			case 101:
			case 102:
			{
				var navalBases = 0;
				for (var i = 1; i <= _game.MaxUnitId; i++)
				{
					if (
						_game.Units[i].Side == _game.CpuSide
						&& _game.Units[i].Kind == UnitKind.NavalBase
					)
						navalBases++;
				}

				for (var f = 1; f <= 4; f++)
				{
					ref var fleet = ref _game.FleetAt(f);
					if (
						navalBases == 0
						&& fleet.Flagship != 0
						&& fleet.Mode == 50
						&& _game.Units[fleet.Flagship].IsUsed
					)
					{
						fleet.RenewsFlagship = 1;
						fleet.Mode = 1;
						_game.AddNavalBaseTargets(f);
						_game.AddShipTargets(f);
						_game.ShuffleTargets(f);
					}
				}
				break;
			}

			case 200:
			{
				for (var f = 1; f <= 2; f++)
				{
					ref var fleet = ref _game.FleetAt(f);
					if (
						fleet.Flagship != 0
						&& fleet.Mode != 1
						&& _game.Units[fleet.Flagship].IsUsed
						&& _game.BattleTime <= 50000
					)
					{
						fleet.RenewsFlagship = 1;
						fleet.Mode = 1;
						_game.AddShipTargets(f);
						_game.ShuffleTargets(f);
					}
				}

				ref var fleet4 = ref _game.FleetAt(4);
				if (fleet4.Flagship != 0 && _game.Units[fleet4.Flagship].Kind == UnitKind.Cruiser)
					break;

				ref var fleet3 = ref _game.FleetAt(3);
				if (fleet3.Flagship != 0)
				{
					fleet3.RenewsFlagship = 1;
					fleet3.Mode = 50;
					fleet3.Targets[1] = 0;
					fleet3.Targets[2] = 0;
					fleet3.SecondX = -880;
					fleet3.SecondY = 40;
				}
				break;
			}

			case 203:
				foreach (var (f, time) in new[] { (6, 42000), (7, 40000) })
				{
					ref var fleet = ref _game.FleetAt(f);
					if (
						fleet.Flagship != 0
						&& fleet.Mode != 1
						&& _game.Units[fleet.Flagship].IsUsed
						&& _game.BattleTime <= time
					)
					{
						fleet.Mode = 1;
						fleet.AimX = -40;
						fleet.AimY = 3320;
					}
				}
				break;

			case 208:
			{
				var f2 = _game.FleetAt(2).Flagship;
				int planes;
				if (_game.Units[f2].IsUsed && _game.BattleTime <= 20000)
				{
					planes = 0;
					for (var i = 1; i <= _game.MaxUnitId; i++)
					{
						if (
							_game.Units[i].IsUsed
							&& _game.Units[i].Category == UnitCategory.Plane
							&& _game.Units[i].Carrier == f2
						)
							planes++;
					}
				}
				else
				{
					// BUG(1.22): the count is read before it is set (0x4050C7): what Windows' stack holds there, or what a
					// trace gave. Kept as is for compatibility with 1.22.
					planes = (int)_game.StackLocal(0x4050C7);
				}

				if (planes < 16 && planes != 0)
					_game.AddPlane(
						Side.Japan,
						_game.rand() % 2 == 0 ? UnitKind.Attacker : UnitKind.Bomber,
						0,
						f2,
						1,
						FireKind.Bomb
					);
				break;
			}

			case 209:
			{
				ref var fleet8 = ref _game.FleetAt(8);
				if (fleet8.Flagship != 0 || _game.BattleTime > 40000)
					break;

				const double Y = 6500;
				const double Direction = 135;
				var x = (double)(_game.rand() % 1000 + 8500);
				var carrier = (short)
					_game.AddUnit(Side.UnitedStates, UnitKind.Carrier, x, Y, Direction);
				fleet8.Flagship = carrier;
				fleet8.Members[0] = carrier;
				_game.AddPlane(Side.UnitedStates, UnitKind.Attacker, 0, carrier, 12, FireKind.Bomb);
				var escorts = new[]
				{
					UnitKind.Cruiser,
					UnitKind.Destroyer,
					UnitKind.Cruiser,
					UnitKind.Destroyer,
				};
				for (var i = 0; i < escorts.Length; i++)
				{
					x = x + 150;
					fleet8.Members[i + 1] = (short)
						_game.AddUnit(Side.UnitedStates, escorts[i], x, Y, Direction);
				}

				fleet8.RenewsFlagship = 1;
				fleet8.Mode = 1;
				_game.AddTargetsOfKind(8, UnitKind.InfantryBase);
				_game.ShuffleTargets(8);
				_game.AddShipTargets(8);
				fleet8.DestinationX = 80;
				fleet8.DestinationY = 3440;
				break;
			}
		}
	}

	// Step 3: the target near the fleet's destination, or, for a carrier, near the carrier; 0 for none.
	private int FindComputersTarget(ref ComputerFleet fleet)
	{
		var found = 0;
		if (fleet.Flagship == 0)
			return found;

		ref var flagship = ref _game.Units[fleet.Flagship];
		RECT rect;
		if (fleet.DestinationX != PathEnd)
		{
			rect.top = Ftol(fleet.DestinationY - 2400);
			rect.right = Ftol(fleet.DestinationX + 2400);
			rect.bottom = Ftol(fleet.DestinationY + 2400);
			rect.left = Ftol(fleet.DestinationX - 2400);
			for (var i = 1; i <= _game.MaxUnitId; i++)
			{
				ref var unit = ref _game.Units[i];
				if (!unit.IsUsed || unit.Side == _game.CpuSide)
					continue;

				var kind = unit.Kind;
				var isTarget =
					kind is UnitKind.Battleship or UnitKind.Cruiser
					|| (kind == UnitKind.Destroyer && flagship.Kind != UnitKind.Battleship)
					|| kind is UnitKind.Carrier or UnitKind.LightCarrier or UnitKind.Transport
					|| IsLandForce(kind);
				if (
					isTarget
					&& unit.Ammo != 0
					&& _game.PointInRect(ref rect, Ftol(unit.Position.X), Ftol(unit.Position.Y))
					&& (_game.ScenarioNumber != 204 || IsLandForce(kind))
				)
					found = i;
			}
		}

		if (flagship.Kind is UnitKind.Carrier or UnitKind.LightCarrier)
		{
			rect.top = Ftol(flagship.Position.Y - 800);
			rect.right = Ftol(flagship.Position.X + 800);
			rect.bottom = Ftol(flagship.Position.Y + 800);
			rect.left = Ftol(flagship.Position.X - 800);
			for (var i = 1; i <= _game.MaxUnitId; i++)
			{
				ref var unit = ref _game.Units[i];
				if (!unit.IsUsed || unit.Side == _game.CpuSide || !unit.IsFound)
					continue;

				var isTarget =
					unit.Kind
					is UnitKind.Battleship
						or UnitKind.Cruiser
						or UnitKind.Destroyer
						or UnitKind.Carrier
						or UnitKind.LightCarrier
						or UnitKind.Transport;
				if (
					isTarget
					&& unit.Ammo != 0
					&& _game.PointInRect(ref rect, Ftol(unit.Position.X), Ftol(unit.Position.Y))
				)
					found = i;
			}
		}

		return found;
	}

	// Step 4: the fleet's members and flagship.
	private StrategyFlow KeepUpFleet(ref ComputerFleet fleet)
	{
		if (fleet.Flagship != 0 && _game.Units[fleet.Flagship].Kind != UnitKind.AirBase)
		{
			var live = 0;
			var listed = 0;
			for (var k = 0; k <= 17; k++)
			{
				var m = fleet.Members[k];
				if (m == 0)
					continue;

				listed++;
				ref var member = ref _game.Units[m];
				if (member.IsUsed)
				{
					live++;
					if ((int)member.Fuel <= 0)
						member.Fuel = 8;
				}
				else
				{
					fleet.Members[k] = 0;
				}
			}

			if (live == 0)
			{
				fleet.Flagship = 0;
			}
			else if (listed != live)
			{
				Pack(fleet.Members, 0, 17);
				if (fleet.RenewsFlagship == 1)
				{
					fleet.State = 1;
					fleet.Flagship = fleet.Members[0];
					fleet.RenewsFlagship = 1;
					fleet.Base = 0;
					return StrategyFlow.Order;
				}
			}
		}

		if (fleet.Flagship == 0)
			return StrategyFlow.NoFleet;

		fleet.State = 0;
		ref var flagship = ref _game.Units[fleet.Flagship];
		if (
			flagship.Kind != UnitKind.AirBase
			&& fleet.Members[1] != 0
			&& flagship.IsGroupLeader == 0
		)
		{
			fleet.State = 1;
			return StrategyFlow.Order;
		}

		return StrategyFlow.Next;
	}

	// Step 5: the computer's unarmed attackers and bombers parked on the fleet's carriers and air bases get their weapons.
	private void ArmComputersPlanes(ref ComputerFleet fleet, int near)
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
	private void FlyComputersPlanes()
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
	private void ChooseTargetOf(int i, int f, ref ComputerFleet fleet, int near)
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

	// Step 9: the fleet returns to the naval base; its flagship stops.
	private void ReturnToBase(ref ComputerFleet fleet, int naval)
	{
		fleet.Base = (short)naval;
		_game.StopWhereItIs(ref _game.Units[fleet.Flagship]);
	}

	// Step 9: the fleet's dead targets dropped, and whether it returns to its nearest naval base.
	private void DecideToReturn(ref ComputerFleet fleet)
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
	private void SupplyAtBase(ref ComputerFleet fleet)
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
	private void StopAtTheEdge(ref ComputerFleet fleet)
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
	private StrategyFlow MoveFleet(ref ComputerFleet fleet, int near)
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
	private StrategyFlow StrikeFromFlagship(ref ComputerFleet fleet, int near)
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
	private StrategyFlow BombFromAirBase(ref ComputerFleet fleet, int near)
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
	private StrategyFlow StrikeFromOtherCarriers(ref ComputerFleet fleet, int near)
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
	private void DefendAirBase(ref ComputerFleet fleet)
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

	// Step 12, scenario 304: a new American unit of the kind near the east edge of the map, from the point.
	private short AddReinforcement(UnitKind kind, double x, double y)
	{
		PlaceReinforcement(ref x, ref y);
		return (short)_game.AddUnit(Side.UnitedStates, kind, x, y, 135);
	}

	// Step 12, scenario 304: fleets 1 and 2 get a new carrier when they have none, and escorts; fleets 3 to 5 new
	// battleships and cruisers.
	private void ReinforceScenario304(int f, ref ComputerFleet fleet)
	{
		if (_game.ScenarioNumber != 304)
			return;

		if (f is 1 or 2)
		{
			if (_game.BattleTime > (f == 1 ? 130000 : 100000))
				return;

			short members = 0;
			var carriers = 0;
			var flag = fleet.Flagship;
			if (flag != 0)
			{
				for (var k = 0; k <= 17; k++)
				{
					ref var member = ref _game.Units[fleet.Members[k]];
					if (member.IsUsed)
					{
						if (member.Kind == UnitKind.Carrier)
							carriers++;
						members++;
					}
				}
			}

			if (carriers == 0)
			{
				if (fleet.Base == 0)
					_game.Units[flag].Ammo = 0;

				ref var flagship = ref _game.Units[flag];
				var nearFlagship = flagship.SupplyTime != 0 && members != 0;
				if (nearFlagship || members == 0)
				{
					double x;
					double y;
					if (nearFlagship)
					{
						x = flagship.Position.X;
						y = flagship.Position.Y;
					}
					else
					{
						y = _game.rand() % 7200;
						x = 10100;
					}

					var carrier = AddReinforcement(UnitKind.Carrier, x, y);
					fleet.Flagship = carrier;
					MemberAt(ref fleet, members) = carrier;
					_game.AddPlane(
						Side.UnitedStates,
						UnitKind.Attacker,
						0,
						carrier,
						10,
						FireKind.Bomb
					);
					_game.AddPlane(
						Side.UnitedStates,
						UnitKind.Fighter,
						0,
						carrier,
						2,
						FireKind.Unarmed
					);
					fleet.RenewsFlagship = 1;
					members++;
					fleet.Mode = 1;
					_game.AddShipTargets(f);
					_game.AddTargetsOfKind(f, UnitKind.InfantryBase);
					_game.ShuffleTargets(f);
					if (f == 1)
					{
						ref var fleet1 = ref _game.FleetAt(1);
						fleet1.DestinationX = 6640;
						fleet1.DestinationY = -3760;
					}
				}
			}

			var last = f - (carriers == 0 ? 1 : 0) + 1;
			for (; members <= last; members++)
			{
				var y = (double)(_game.rand() % 7200);
				var x = 10100.0;
				PlaceReinforcement(ref x, ref y);
				var r = _game.rand() % 100;
				var kind =
					r < 60 ? UnitKind.Cruiser
					: r < 90 ? UnitKind.Destroyer
					: UnitKind.Battleship;
				MemberAt(ref fleet, members) = (short)
					_game.AddUnit(Side.UnitedStates, kind, x, y, 135);
			}

			return;
		}

		if (!(f is 3 or 4 || (f == 5 && _game.BattleTime <= 100000)))
			return;

		short count = 0;
		if (fleet.Flagship != 0)
		{
			for (var k = 0; k <= 17; k++)
			{
				if (_game.Units[fleet.Members[k]].IsUsed)
					count++;
			}

			if (count > 2)
				return;
		}

		for (; count <= 2; count++)
		{
			var y = (double)(_game.rand() % 7200);
			var x = 10100.0;
			PlaceReinforcement(ref x, ref y);
			var kind = _game.rand() % 100 < 60 ? UnitKind.Cruiser : UnitKind.Battleship;
			var m = (short)_game.AddUnit(Side.UnitedStates, kind, x, y, 135);
			if (count == 0)
			{
				fleet.Flagship = m;
				fleet.Members[0] = m;
				fleet.RenewsFlagship = 1;
				fleet.Mode = 1;
				_game.AddTargetsOfKind(f, UnitKind.InfantryBase);
				_game.AddTargetsOfKind(f, UnitKind.Pillboxes);
				_game.AddTargetsOfKind(f, UnitKind.Fortress);
				_game.ShuffleTargets(f);
			}

			fleet.Members[count] = m;
			if (f == 3)
			{
				ref var fleet3 = ref _game.FleetAt(3);
				fleet3.DestinationX = 6640;
				fleet3.DestinationY = -3760;
			}
		}
	}

	// The order of the fleet: in state 1, its members form up on the flagship; in state 50, the flagship and its members
	// move to its next move, on the grid of 80.
	private void OrderFleet(int f, ref ComputerFleet fleet)
	{
		int flag = fleet.Flagship;
		if (flag == 0 || _game.Units[flag].Kind == UnitKind.AirBase)
			return;

		if (fleet.State == 1)
		{
			SetFormation(f);
			int leader = fleet.Flagship;
			_game.MoveOrders[0].Unit = (short)leader;
			_game.MoveOrders[0].Destination = _game.Units[leader].Position;
			return;
		}

		if (fleet.State != 50)
			return;

		var n = 1;
		for (var k = 1; MemberAt(ref fleet, k) != 0; k++)
		{
			int m = MemberAt(ref fleet, k);
			if (_game.Units[m].IsUsed)
				_game.Selections[0][m] = (short)n++;
		}

		_game.MoveOrders[0].Unit = (short)flag;
		var y = fleet.PathY[0];
		_game.MoveOrders[0].Destination = new WorldPosition(
			Ftol(fleet.PathX[0] * 0.0125) * 80,
			Ftol(y * 0.0125) * 80
		);
	}

	// cpu_strtgy: the computer's order of this frame, in new_pp[0] and slct_unit[0], for its fleet Renderer.FrameCount%10+1
	// (docs/ComputerPlayer.md, cpu_strtgy).
	public void Plan()
	{
		var f = _game.Renderer.FrameCount % 10 + 1;
		_game.Selections[0] = default;
		_game.MoveOrders[0].Unit = 0;
		ApplyComputerEvents();

		ref var fleet = ref _game.FleetAt(f);
		var near = FindComputersTarget(ref fleet);
		var flow = KeepUpFleet(ref fleet);
		if (flow == StrategyFlow.NoFleet)
		{
			if (_game.ScenarioNumber != 304)
				return;
			flow = StrategyFlow.Order;
		}

		if (flow == StrategyFlow.Next)
		{
			ArmComputersPlanes(ref fleet, near);
			FlyComputersPlanes();
			if (fleet.Flagship != 0)
			{
				for (var i = 1; i <= _game.MaxUnitId; i++)
					ChooseTargetOf(i, f, ref fleet, near);
				DecideToReturn(ref fleet);
			}

			SupplyAtBase(ref fleet);
			StopAtTheEdge(ref fleet);
			flow = MoveFleet(ref fleet, near);
			if (flow == StrategyFlow.Next)
				flow = StrikeFromFlagship(ref fleet, near);
			if (flow == StrategyFlow.Next)
				flow = BombFromAirBase(ref fleet, near);
			if (flow == StrategyFlow.Next)
				flow = StrikeFromOtherCarriers(ref fleet, near);
			if (flow == StrategyFlow.Next)
				DefendAirBase(ref fleet);
			if (flow == StrategyFlow.Return)
				return;
		}

		ReinforceScenario304(f, ref fleet);
		OrderFleet(f, ref fleet);
	}
}
