using static OpenNspw.ComputerPlayers.StrategyBasics;
using static OpenNspw.Nspw;

namespace OpenNspw.ComputerPlayers;

// cpu_strtgy's order, at its end: a fleet's formation (sub_403AE0) when it forms again, or its flagship's next move
// (docs/ComputerPlayer.md, cpu_strtgy, step 9).
public readonly struct FleetFormation(Nspw game)
{
	private readonly Nspw _game = game;

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
	public void SetFormation(int f)
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

	// The order of the fleet: in state 1, its members form up on the flagship; in state 50, the flagship and its members
	// move to its next move, on the grid of 80.
	public void OrderFleet(int f, ref ComputerFleet fleet)
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
}
