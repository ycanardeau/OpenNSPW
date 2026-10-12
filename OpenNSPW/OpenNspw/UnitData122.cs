namespace OpenNspw;

// NSPW 1.22's set_unit_data (0x41E150), rewritten from its machine code: the data of a unit by its kind, type and side.
// Its values (HP, speeds, weapons, fuel) are not NSPW NET's (docs/Differences.md), so the port's SetUnitData uses it in
// a single-player game. It is tested against the emulated original (SharedFunctionTests).
public unsafe partial class Nspw
{
	private static void SetMovement122(
		ref Unit unit,
		int row,
		double turn,
		double acceleration,
		double minSpeed,
		double maxSpeed
	)
	{
		unit.SpriteRow = row;
		unit.TurnRateChange = turn;
		unit.AccelerationChange = acceleration;
		unit.MinSpeed = minSpeed;
		unit.MaxSpeed = maxSpeed;
	}

	private static void SetArms122(ref Unit unit, FireKind weapon, int ammo, int maxAmmo)
	{
		unit.Weapon = weapon;
		unit.Ammo = ammo;
		unit.MaxAmmo = maxAmmo;
	}

	// gas[0], full, and gas[1], the ticks a unit of fuel lasts.
	private static void SetFuel122(ref Unit unit, int interval)
	{
		unit.Fuel = 100;
		unit.FuelInterval = interval;
	}

	// A carrier's or an air base's planes: none launching, and the mode of its menu.
	private void SetBase122(ref Unit unit, int m, int capacity)
	{
		unit.info[0] = 0;
		unit.PlaneCount = CountPlanesIn(m);
		unit.Capacity = capacity;
		unit.info[3] = 0;
		unit.PlanesToLaunch = 0;
		unit.Mode = UnitMode.Move;
	}

	// set_unit_data: the data of unit m, its random numbers, and its path, where it is.
	private void SetUnitData122(int m)
	{
		ref var unit = ref Units[m];
		var japanese = unit.Side == Side.Japan;
		int? hp = null;
		switch (unit.Kind)
		{
			case UnitKind.Battleship:
				SetMovement122(ref unit, 0, 0.3, 0.01, 0.0, 0.7);
				SetArms122(ref unit, FireKind.Gun, 1000, 1000);
				SetFuel122(ref unit, 9000);
				hp = 41;
				break;

			case UnitKind.Cruiser:
				SetMovement122(ref unit, 1, 0.5, 0.01, 0.0, 0.8);
				SetArms122(ref unit, FireKind.Gun, 600, 600);
				SetFuel122(ref unit, 650);
				hp = 32;
				break;

			case UnitKind.Destroyer:
				SetMovement122(ref unit, 2, 1.3, 0.05, 0.0, 1.0);
				SetArms122(ref unit, FireKind.Gun, 120, 120);
				SetFuel122(ref unit, 550);
				hp = 18;
				break;

			case UnitKind.Submarine:
				SetMovement122(ref unit, 3, 0.5, 0.02, 0.0, 0.5);
				SetArms122(ref unit, FireKind.Gun, 25, 25);
				SetFuel122(ref unit, 1000);
				hp = 7;
				break;

			case UnitKind.Carrier:
				SetMovement122(ref unit, 6, 0.3, 0.01, 0.0, 0.7);
				SetBase122(ref unit, m, 12);
				SetArms122(ref unit, FireKind.None, 100, 100);
				SetFuel122(ref unit, 750);
				hp = unit.Side == Side.UnitedStates ? 41 : 36;
				break;

			case UnitKind.LightCarrier:
				SetMovement122(ref unit, 5, 0.5, 0.01, 0.0, 0.9);
				SetBase122(ref unit, m, 8);
				SetArms122(ref unit, FireKind.None, 80, 80);
				SetFuel122(ref unit, 700);
				hp = unit.Side == Side.UnitedStates ? 36 : 31;
				break;

			case UnitKind.Fighter when unit.Variant == 1:
				if (japanese)
				{
					SetMovement122(ref unit, 14, 3.0, 0.03, 0.5, 3.0);
					SetArms122(ref unit, FireKind.Bullet, 60, 60);
					SetFuel122(ref unit, 55);
					hp = 32;
				}
				else
				{
					SetMovement122(ref unit, 14, 2.0, 0.02, 0.5, 2.8);
					SetArms122(ref unit, FireKind.Bullet, 70, 70);
					SetFuel122(ref unit, 100);
					hp = 48;
				}
				break;

			case UnitKind.Fighter when unit.Variant == 0:
				if (japanese)
				{
					SetMovement122(ref unit, unit.IsStopping ? 8 : 7, 4.0, 0.01, 0.5, 2.35);
					SetArms122(ref unit, FireKind.Bullet, 35, 35);
					SetFuel122(ref unit, 80);
					hp = 16;
				}
				else
				{
					SetMovement122(ref unit, unit.IsStopping ? 8 : 7, 2.2, 0.01, 0.5, 2.5);
					SetArms122(ref unit, FireKind.Bullet, 40, 40);
					SetFuel122(ref unit, 60);
					hp = 20;
				}
				break;

			case UnitKind.Attacker:
				SetMovement122(ref unit, unit.IsStopping ? 10 : 9, 3.0, 0.01, 0.5, 2.2);
				SetArms122(ref unit, FireKind.Unarmed, 0, 1);
				SetFuel122(ref unit, japanese ? 85 : 70);
				hp = japanese ? 16 : 20;
				break;

			case UnitKind.Bomber:
				if (japanese)
				{
					SetMovement122(ref unit, 12, 2.4, 0.005, 0.5, 1.8);
					SetArms122(ref unit, FireKind.Unarmed, 0, 9);
					SetFuel122(ref unit, 120);
					hp = 32;
				}
				else
				{
					SetMovement122(ref unit, 12, 2.0, 0.005, 0.5, 1.9);
					SetArms122(ref unit, FireKind.Unarmed, 0, 20);
					SetFuel122(ref unit, 220);
					hp = 50;
				}
				break;

			case UnitKind.Transport:
				SetMovement122(ref unit, 13, 0.5, 0.01, 0.0, 0.65);
				SetArms122(ref unit, FireKind.Unarmed, 0, 0);
				SetFuel122(ref unit, 1000);
				hp = 12;
				break;

			case UnitKind.AirBase:
				unit.SpriteRow = 11;
				SetBase122(ref unit, m, 16);
				unit.Direction = 45.0;
				hp = 260;
				break;

			case UnitKind.NavalBase:
				unit.SpriteRow = 11;
				unit.Direction = 90.0;
				hp = 320;
				break;

			case UnitKind.City:
				unit.SpriteRow = 11;
				SetArms122(ref unit, FireKind.None, 0, 0);
				unit.Direction = 0.0;
				hp = 150;
				break;

			case UnitKind.InfantryBase:
				unit.SpriteRow = 11;
				SetArms122(ref unit, FireKind.Gun, 700, 700);
				unit.Direction = 225.0;
				hp = 180;
				break;

			case UnitKind.Pillboxes:
				unit.SpriteRow = 11;
				SetArms122(ref unit, FireKind.Gun, 1500, 1500);
				unit.Direction = 180.0;
				hp = 320;
				break;

			case UnitKind.Fortress:
				unit.SpriteRow = 11;
				SetArms122(ref unit, FireKind.Gun, 2000, 2000);
				unit.Direction = 135.0;
				hp = 400;
				break;
		}

		if (hp is { } value)
		{
			unit.Skill = 5;
			unit.HP = unit.MaxHP = value;
		}

		for (var i = 0; i <= 1; i++)
		{
			unit.Random250[i] = (short)(rand() % 250);
			unit.Random225[i] = (short)(rand() % 225);
			unit.Random200[i] = (short)(rand() % 200);
			unit.Random175[i] = (short)(rand() % 175);
			unit.Random150[i] = (short)(rand() % 150);
			unit.Random125[i] = (short)(rand() % 125);
			unit.Random100[i] = (short)(rand() % 100);
			unit.Random80[i] = (short)(rand() % 80);
			unit.Random65[i] = (short)(rand() % 65);
			unit.Random50[i] = (short)(rand() % 50);
			unit.Random40[i] = (short)(rand() % 40);
			unit.Random30[i] = (short)(rand() % 30);
			unit.Random20[i] = (short)(rand() % 20);
			unit.Random10[i] = (short)(rand() % 10);
		}

		StopWhereItIs(ref unit);
	}
}
