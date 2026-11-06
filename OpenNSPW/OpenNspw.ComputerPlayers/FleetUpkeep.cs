using static OpenNspw.ComputerPlayers.StrategyBasics;

namespace OpenNspw.ComputerPlayers;

// cpu_strtgy's steps 3 and 4 for a fleet: the enemy near its destination, and its upkeep, which removes its dead members,
// refuels the others, packs the list and forms it again (docs/ComputerPlayer.md, cpu_strtgy).
public readonly struct FleetUpkeep(Nspw game)
{
	private readonly Nspw _game = game;

	// Step 3: the target near the fleet's destination, or, for a carrier, near the carrier; 0 for none.
	public int FindComputersTarget(ref ComputerFleet fleet)
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
	public StrategyFlow KeepUpFleet(ref ComputerFleet fleet)
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
}
