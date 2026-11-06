using static OpenNspw.ComputerPlayers.StrategyBasics;

namespace OpenNspw.ComputerPlayers;

// cpu_strtgy's first step: the events of scenarios 8, 10, 101, 102, 200, 203, 208 and 209, each when the time left falls to
// a threshold (docs/ComputerPlayer.md, cpu_strtgy, step 1). They act on 1.22's fleets by their numbers.
public readonly struct ScenarioEvents(Nspw game)
{
	private readonly Nspw _game = game;

	// Step 1: the scenario's events.
	public void ApplyComputerEvents()
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
}
