using static OpenNspw.ComputerPlayers.StrategyBasics;
using static OpenNspw.Nspw;

namespace OpenNspw.ComputerPlayers;

// cpu_strtgy's step 12: the American reinforcements of scenario 304, placed by sub_414DD0 (docs/ComputerPlayer.md,
// cpu_strtgy).
public readonly struct Scenario304Reinforcements(Nspw game)
{
	private readonly Nspw _game = game;

	// sub_414DD0: moves the point 80 down until no ship's square (the size of the units' sprite) covers it.
	public void PlaceReinforcement(ref double x, ref double y)
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

	// Step 12, scenario 304: a new American unit of the kind near the east edge of the map, from the point.
	private short AddReinforcement(UnitKind kind, double x, double y)
	{
		PlaceReinforcement(ref x, ref y);
		return (short)_game.AddUnit(Side.UnitedStates, kind, x, y, 135);
	}

	// Step 12, scenario 304: fleets 1 and 2 get a new carrier when they have none, and escorts; fleets 3 to 5 new
	// battleships and cruisers.
	public void ReinforceScenario304(int f, ref ComputerFleet fleet)
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
}
