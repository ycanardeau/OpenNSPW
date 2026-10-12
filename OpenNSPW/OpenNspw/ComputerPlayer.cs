namespace OpenNspw;

// NSPW 1.22's computer player (docs/ComputerPlayer.md), rewritten from its machine code in place of the emulated
// functions, one at a time. Each one is tested against the emulated original (ComputerPlayerTests).
public unsafe partial class Nspw
{
	// set_cpu_route's test for a ship in the way: a ship other than unit m, as in IsShipAt, but only air bases and naval
	// bases (and submerged submarines) are not in the way.
	private bool IsShipInComputersWay(int m, double x, double y)
	{
		RECT wrk_r;
		int f;

		for (f = 1; f <= MaxUnitId; f++)
		{
			ref var other = ref Units[f];
			if (other.IsUsed && m != f && other.Category == UnitCategory.Ship && !(other.Kind == UnitKind.Submarine && other.IsSubmerged) && other.Kind != UnitKind.NavalBase && other.Kind != UnitKind.AirBase)
			{
				wrk_r.top = (int)other.Position.Y + (Sprites[SpriteId.JapanUnits].Height / 2);
				wrk_r.right = (int)other.Position.X + (Sprites[SpriteId.JapanUnits].Width / 2);
				wrk_r.bottom = (int)other.Position.Y - (Sprites[SpriteId.JapanUnits].Height / 2);
				wrk_r.left = (int)other.Position.X - (Sprites[SpriteId.JapanUnits].Width / 2);
				if (PointInRect3(ref wrk_r, (int)x, (int)y) != 0)
					return true;
			}
		}

		return false;
	}

	// set_cpu_route's test of the destination: whether it is in a ship's square (any ship but a submerged submarine,
	// bases included), as wide as the units' sprite.
	private bool IsDestinationInAShip(int m, double x, double y)
	{
		RECT wrk_r;
		int n;

		for (n = 1; n <= MaxUnitId; n++)
		{
			ref var other = ref Units[n];
			if (other.IsUsed && m != n && other.Category == UnitCategory.Ship && !(other.Kind == UnitKind.Submarine && other.IsSubmerged))
			{
				wrk_r.top = (int)other.Position.Y + (Sprites[SpriteId.JapanUnits].Width / 2);
				wrk_r.right = (int)other.Position.X + (Sprites[SpriteId.JapanUnits].Width / 2);
				wrk_r.bottom = (int)other.Position.Y - (Sprites[SpriteId.JapanUnits].Width / 2);
				wrk_r.left = (int)other.Position.X - (Sprites[SpriteId.JapanUnits].Width / 2);
				if (PointInRect3(ref wrk_r, (int)x, (int)y) != 0)
					return true;
			}
		}

		return false;
	}

	// Stops unit m where it is: its path is its position alone.
	private void StopWhereItIs(ref Unit unit)
	{
		unit.PathX[0] = unit.Position.X;
		unit.PathY[0] = unit.Position.Y;
		unit.PathX[1] = MAP_RIGHT + 1;
	}

	// set_cpu_route (0x42DA80): the route of the computer's ship m to the destination of its path, around land and ships,
	// as set_cpu_root2 (SetCpuRoute2) plans it, with 1.22's differences (docs/ComputerPlayer.md, set_cpu_route): a
	// destination on land or in a ship stops the ship, the first leg is walked by 1 for its first 320, the detours are
	// tried 10° apart, and waypoints are added until the last leg is clear.
	public void PlanComputerRoute(int m)
	{
		ref var unit = ref Units[m];
		double add;
		double pp_drctn, wrk_x, wrk_y, pp_dstc, chk_dstc, wrk, drctn1, drctn2, wrk_x2, wrk_y2, start_x, start_y, min_dstc, s_pp_x, s_pp_y;
		int n, hit, left, right, pp_indx, error;

		const double div = 2.0;
		const double re_add = 160;
		const double drctn_ok1 = 90.0;
		const double drctn_ok2 = 270.0;

		error = 0;

		if (unit.Category == UnitCategory.Ship)
		{
			if (IsDestinationInAShip(m, unit.PathX[0], unit.PathY[0]))
			{
				StopWhereItIs(ref unit);
				return;
			}

			if (IsLandAt(unit.PathX[0], unit.PathY[0]))
			{
				StopWhereItIs(ref unit);
				return;
			}
		}

		min_dstc = 0;
		s_pp_x = unit.PathX[0];
		s_pp_y = unit.PathY[0];
		pp_indx = 0;

		while (pp_indx >= 0)
		{
			if (pp_indx == 0)
			{
				start_x = unit.Position.X;
				start_y = unit.Position.Y;
			}
			else
			{
				start_x = unit.PathX[pp_indx - 1];
				start_y = unit.PathY[pp_indx - 1];
			}

			if (min_dstc != 0)
			{
				unit.PathX[pp_indx] = s_pp_x;
				unit.PathY[pp_indx] = s_pp_y;
				unit.PathX[pp_indx + 1] = MAP_RIGHT + 1;

				wrk_x = unit.PathX[pp_indx] - unit.Position.X;
				wrk_y = unit.PathY[pp_indx] - unit.Position.Y;
				if (wrk_x == 0) wrk_x = 1;
				if (wrk_y == 0) wrk_y = 1;

				pp_drctn = atan2(wrk_y, wrk_x) * RAD_to;

				unit.PathX[pp_indx] += CosDegrees(pp_drctn) * (min_dstc);
				unit.PathY[pp_indx] += SinDegrees(pp_drctn) * (min_dstc);
			}

			wrk_x = unit.PathX[pp_indx] - start_x;
			wrk_y = unit.PathY[pp_indx] - start_y;
			if (wrk_x == 0) wrk_x = 1;
			if (wrk_y == 0) wrk_y = 1;

			pp_drctn = Direction(wrk_x, wrk_y);
			if (pp_drctn >= 360)
				pp_drctn = pp_drctn - 360;

			if (wrk_x < 0)
				wrk_x = 0 - wrk_x;
			if (wrk_y < 0)
				wrk_y = 0 - wrk_y;
			wrk = pp_drctn;
			if (wrk >= 180)
				wrk = wrk - 180;
			if (wrk >= 90)
				wrk = 90 - (wrk - 90);
			pp_dstc = (wrk_x) / (CosDegrees(wrk));

			if (unit.Category == UnitCategory.Ship)
			{
				hit = 0;
				chk_dstc = 40;
				while (hit == 0 && chk_dstc <= pp_dstc)
				{
					if (pp_indx == 0 && chk_dstc <= 320)
						add = 1.0;
					else
						add = 40.0;

					chk_dstc += add;

					wrk_x = start_x;
					wrk_y = start_y;
					wrk_x += CosDegrees(pp_drctn) * (chk_dstc);
					wrk_y += SinDegrees(pp_drctn) * (chk_dstc);

					if (IsLandAt(wrk_x, wrk_y))
						hit = 1;

					if (hit == 0 && IsShipInComputersWay(m, wrk_x, wrk_y))
						hit = 1;
				}

				if (hit == 1)
				{
					drctn1 = pp_drctn;
					drctn2 = pp_drctn;
					for (n = 0; n <= 17; n++)
					{
						drctn1 += 10;
						if (drctn1 >= 360) drctn1 = drctn1 - 360;
						drctn2 -= 10;
						if (drctn2 < 0) drctn2 = 360 + drctn2;
						chk_dstc = 40;
						left = 0;
						right = 0;
						while (chk_dstc <= (pp_dstc) && (left == 0 || right == 0))
						{
							if (pp_indx == 0 && chk_dstc <= 320)
								add = 1.0;
							else
								add = 40.0;

							chk_dstc += add;

							if (left == 0)
							{
								wrk_x = start_x;
								wrk_y = start_y;
								wrk_x += CosDegrees(drctn1) * (chk_dstc);
								wrk_y += SinDegrees(drctn1) * (chk_dstc);
								if (IsLandAt(wrk_x, wrk_y))
									left = 1;

								if (IsShipInComputersWay(m, wrk_x, wrk_y))
									left = 1;
							}
							if (right == 0)
							{
								wrk_x2 = start_x;
								wrk_y2 = start_y;
								wrk_x2 += CosDegrees(drctn2) * (chk_dstc);
								wrk_y2 += SinDegrees(drctn2) * (chk_dstc);
								if (IsLandAt(wrk_x2, wrk_y2))
									right = 1;

								if (IsShipInComputersWay(m, wrk_x2, wrk_y2))
									right = 1;
							}
						}

						if (right == 0 && chk_dstc >= pp_dstc)
						{
							TakeDetour(ref unit, ref pp_indx, start_x, start_y, chk_dstc, div, drctn2, drctn_ok1, drctn_ok2, ref min_dstc, s_pp_x, s_pp_y, re_add, ref error);
							break;
						}

						if (left == 0 && chk_dstc >= pp_dstc)
						{
							TakeDetour(ref unit, ref pp_indx, start_x, start_y, chk_dstc, div, drctn1, drctn_ok1, drctn_ok2, ref min_dstc, s_pp_x, s_pp_y, re_add, ref error);
							break;
						}
					}
					pp_indx++;
				}
				else
				{
					if (min_dstc == 0)
					{
						pp_indx = -1;
					}
					else
					{
						min_dstc = 0;
						unit.PathX[pp_indx + 1] = s_pp_x;
						unit.PathY[pp_indx + 1] = s_pp_y;
						unit.PathX[pp_indx + 2] = MAP_RIGHT + 1;
						pp_indx++;
					}
				}
			}
			else
			{
				pp_indx = -1;
			}
		}
	}

	// decision's result once the time is up, and none before.
	private GameResult AtTimeUp(GameResult result)
	{
		return BattleTime == 0 ? result : GameResult.None;
	}

	private static bool IsOneOf(UnitKind kind, ReadOnlySpan<UnitKind> kinds)
	{
		foreach (var k in kinds)
		{
			if (kind == k)
				return true;
		}

		return false;
	}

	// The units of the side of one of the kinds.
	private int CountUnits(Side side, params ReadOnlySpan<UnitKind> kinds)
	{
		var count = 0;
		for (var i = 1; i <= MaxUnitId; i++)
		{
			if (Units[i].Side == side && IsOneOf(Units[i].Kind, kinds))
				count++;
		}

		return count;
	}

	// The units of the side whose fuel is -1 or less: those that the scenario placed at anchor.
	private int CountAnchoredUnits(Side side)
	{
		var count = 0;
		for (var i = 1; i <= MaxUnitId; i++)
		{
			if (Units[i].Side == side && (int)Units[i].Fuel <= -1)
				count++;
		}

		return count;
	}

	// The units of the side of one of the kinds in the rectangle, y downward (pt_in_rect3, 0x417BE0) or upward (0x417C10).
	private int CountUnitsIn(ref RECT rect, bool isUpward, Side side, params ReadOnlySpan<UnitKind> kinds)
	{
		var count = 0;
		for (var i = 1; i <= MaxUnitId; i++)
		{
			ref var unit = ref Units[i];
			if (unit.Side == side && IsOneOf(unit.Kind, kinds))
			{
				var isIn = isUpward ? PointInRect2(ref rect, (int)unit.Position.X, (int)unit.Position.Y) : PointInRect(ref rect, (int)unit.Position.X, (int)unit.Position.Y);
				if (isIn != 0)
					count++;
			}
		}

		return count;
	}

	// Whether all the ships of the side (but naval bases, when isBaseExcluded) are in the rectangle, y downward, and
	// there is one.
	private bool AreAllShipsIn(ref RECT rect, Side side, bool isBaseExcluded)
	{
		var count = 0;
		var inside = 0;
		for (var i = 1; i <= MaxUnitId; i++)
		{
			ref var unit = ref Units[i];
			if (unit.Side == side && unit.Category == UnitCategory.Ship && !(isBaseExcluded && unit.Kind == UnitKind.NavalBase))
			{
				count++;
				if (PointInRect(ref rect, (int)unit.Position.X, (int)unit.Position.Y) != 0)
					inside++;
			}
		}

		return count != 0 && count == inside;
	}

	// The square of 160 around the unit's position, y downward.
	private static RECT SquareAround(ref Unit unit)
	{
		var x = (int)unit.Position.X;
		var y = (int)unit.Position.Y;
		return new RECT { left = x - 80, top = y - 80, right = x + 80, bottom = y + 80 };
	}

	// Scenario 205: whether an American bomber is parked at unit m, being serviced.
	private bool IsBomberServicedAt(int m)
	{
		for (var i = 1; i <= MaxUnitId; i++)
		{
			ref var unit = ref Units[i];
			if (unit.Side == Side.UnitedStates && unit.Kind == UnitKind.Bomber && unit.PlaneState == UnitState.Parked && unit.Weapon == FireKind.Maintenance && unit.Carrier == m)
				return true;
		}

		return false;
	}

	// Scenario 205: the bomber must land at decision_point[0] while it has more than a fifth of its HP, and then at
	// decision_point[2]; decision_point[1] is set once it has landed at the first.
	private GameResult DecideScenario205()
	{
		if (CountUnits(Side.UnitedStates, UnitKind.Bomber) == 0)
			return GameResult.UnitedStatesLost;

		var landed = DecisionPoints[1];
		if (landed == 0)
		{
			if (IsBomberServicedAt(DecisionPoints[0]))
			{
				landed = 1;
				DecisionPoints[1] = 1;
			}

			ref var first = ref Units[DecisionPoints[0]];
			if (!(first.MaxHp * 0.2 < first.Hp))
				return GameResult.UnitedStatesLost;
		}

		var result = GameResult.None;
		if (landed == 1 && IsBomberServicedAt(DecisionPoints[2]))
			result = GameResult.UnitedStatesWon;

		// Once the time is up, Japan wins, even over the United States' victory.
		return BattleTime == 0 ? GameResult.JapanWon : result;
	}

	// Scenario 303: Japan's parked planes at air base m are lost to the United States as it falls to them: those of
	// type 0 are destroyed, the others change sides. Returns the planes that changed sides.
	private int SurrenderPlanesAt(int m)
	{
		ref var airBase = ref Units[m];
		var surrendered = 0;
		for (var k = 1; k <= MaxUnitId; k++)
		{
			ref var plane = ref Units[k];
			if (plane.Side != Side.Japan || plane.Category != UnitCategory.Plane || plane.PlaneState != UnitState.Parked || plane.Carrier != m)
				continue;

			if (plane.Variant == 0)
			{
				plane.Side = Side.None;
				airBase.PlaneCount--;
				if (plane.DeckPhase >= 1 && airBase.PlanesToLaunch >= 1 && (int)plane.Mode <= 2)
					airBase.PlanesToLaunch--;

				if (plane.DeckPhase >= 3 && airBase.LandingLock >= 1 && (int)plane.Mode <= 2)
					airBase.LandingLock--;

				if (plane.Mode == UnitMode.Return)
				{
					airBase.LandingLock = 0;
					airBase.LaunchLock = 0;
				}
			}
			else
			{
				plane.Side = Side.UnitedStates;
				surrendered++;
			}
		}

		return surrendered;
	}

	// Scenario 303: each fleet of the computer targets units 41 to 43 and stops.
	private void TargetSurrenderedPlanes()
	{
		for (var f = 1; f <= 8; f++)
		{
			ref var fleet = ref FleetAt(f);
			if (fleet.Flagship == 0)
				continue;

			fleet.Targets[1] = 41;
			fleet.Targets[2] = 42;
			fleet.Targets[3] = 43;
			fleet.Targets[4] = 0;
			fleet.Targets[5] = 0;
			fleet.Targets[6] = 0;
			fleet.Base = 0;
			StopWhereItIs(ref Units[fleet.Flagship]);
		}
	}

	// Scenario 303: a transport sinks slowly, the Japanese air bases fall to the American infantry nearby, and fall back
	// to Japan when it has gone; the United States win when their fighters of type 1 are all parked at unit 21.
	private GameResult DecideScenario303()
	{
		ref var transport = ref Units[DecisionPoints[3]];
		if (transport.IsUsed && transport.Hp != 0 && transport.Kind == UnitKind.Transport && BattleTime % 10 == 0)
			transport.Hp--;

		var surrendered = 0;
		for (var i = 1; i <= MaxUnitId; i++)
		{
			if (Units[i].Side != Side.UnitedStates || Units[i].Kind != UnitKind.InfantryBase)
				continue;

			var square = SquareAround(ref Units[i]);
			for (var j = 1; j <= MaxUnitId; j++)
			{
				ref var airBase = ref Units[j];
				if (rand() % 200 == 0 && airBase.Side == Side.Japan && airBase.Kind == UnitKind.AirBase && PointInRect(ref square, (int)airBase.Position.X, (int)airBase.Position.Y) != 0)
				{
					surrendered += SurrenderPlanesAt(j);
					airBase.Side = Side.UnitedStates;
					if (DecisionPoints[0] == 0)
						DecisionPoints[0] = j;
					else
						DecisionPoints[1] = j;
				}
			}
		}

		if (surrendered != 0)
			TargetSurrenderedPlanes();

		for (var p = 0; p <= 1; p++)
		{
			for (var i = 1; i <= MaxUnitId; i++)
			{
				if (Units[i].Side != Side.UnitedStates || i != DecisionPoints[p])
					continue;

				var square = SquareAround(ref Units[i]);
				if (CountUnitsIn(ref square, false, Side.UnitedStates, UnitKind.InfantryBase) == 0)
				{
					DecisionPoints[p] = 0;
					Units[i].Side = Side.Japan;
				}
			}
		}

		var fighters = 0;
		var americanFighters = 0;
		var parkedFighters = 0;
		for (var i = 1; i <= MaxUnitId; i++)
		{
			ref var unit = ref Units[i];
			if (unit.Kind != UnitKind.Fighter || unit.Variant != 1)
				continue;

			if (unit.IsUsed)
				fighters++;

			if (unit.Side == Side.UnitedStates)
			{
				americanFighters++;
				if (unit.PlaneState == UnitState.Parked && unit.IsStopping.Value == 1 && unit.Carrier == 21)
					parkedFighters++;
			}
		}

		if (fighters == 0)
			return GameResult.UnitedStatesLost;

		if (americanFighters >= 1 && americanFighters == parkedFighters)
			return GameResult.UnitedStatesWon;

		return AtTimeUp(GameResult.UnitedStatesLost);
	}

	// decision's victory conditions of the scenario (docs/ComputerPlayer.md, decision): its result, or None.
	private GameResult DecideScenario()
	{
		const Side J = Side.Japan;
		const Side U = Side.UnitedStates;
		const UnitKind BB = UnitKind.Battleship;
		const UnitKind CA = UnitKind.Cruiser;
		const UnitKind DD = UnitKind.Destroyer;
		const UnitKind CV = UnitKind.Carrier;
		const UnitKind CVL = UnitKind.LightCarrier;
		const UnitKind Fighter = UnitKind.Fighter;
		const UnitKind Attacker = UnitKind.Attacker;
		const UnitKind Bomber = UnitKind.Bomber;
		const UnitKind Infantry = UnitKind.InfantryBase;
		const UnitKind Pillboxes = UnitKind.Pillboxes;
		const UnitKind Fortress = UnitKind.Fortress;

		// The island in the west of the map, y upward.
		var island = new RECT { left = -1040, top = -160, right = -800, bottom = -240 };

		switch (ScenarioNumber)
		{
			case 1:
				return CountUnits(J, BB, CA, DD) > 0 ? AtTimeUp(GameResult.UnitedStatesLost) : GameResult.UnitedStatesWon;

			case 2:
			{
				var anchored = CountAnchoredUnits(U);
				if (anchored == 0)
					return GameResult.JapanWon;

				return AtTimeUp(anchored > 3 ? GameResult.JapanLost : GameResult.JapanWon);
			}

			case 3:
				if (CountUnits(J, CV, CVL) == 0)
					return GameResult.UnitedStatesWon;

				if (CountUnits(U, Infantry) == 0)
					return GameResult.JapanWon;

				return CountUnits(U, Attacker, Fighter) > 0 ? AtTimeUp(GameResult.UnitedStatesWon) : GameResult.JapanWon;

			case 4:
			case 5:
			case 6:
			case 105:
			case 106:
				if (CountUnits(J, CV, CVL) == 0)
					return GameResult.UnitedStatesWon;

				return CountUnits(U, CV, CVL) > 0 ? AtTimeUp(GameResult.Draw) : GameResult.JapanWon;

			case 7:
				if (!Units[DecisionPoints[0]].IsUsed)
					return GameResult.JapanWon;

				return CountUnits(J, BB, CA, DD, Attacker, Bomber) > 0 ? AtTimeUp(GameResult.UnitedStatesWon) : GameResult.UnitedStatesWon;

			case 8:
				return CountAnchoredUnits(J) > 0 ? AtTimeUp(GameResult.JapanWon) : GameResult.UnitedStatesWon;

			case 9:
				if (CountUnits(J, CV, CVL) == 0)
					return GameResult.UnitedStatesWon;

				return CountUnits(U, CV, CVL) > 0 ? AtTimeUp(GameResult.UnitedStatesWon) : GameResult.JapanWon;

			case 10:
				if (CountUnits(J, UnitKind.City) == 0)
					return GameResult.UnitedStatesWon;

				return CountUnits(U, Attacker, Bomber, Fighter) > 0 ? AtTimeUp(GameResult.Draw) : GameResult.JapanWon;

			case 100:
				if (CountUnits(J, UnitKind.Submarine) == 0 || CountUnits(J, Infantry, Pillboxes, Fortress) == 0)
					return GameResult.UnitedStatesWon;

				return CountUnits(U, CV, CVL) > 0 ? AtTimeUp(GameResult.Draw) : GameResult.JapanWon;

			case 101:
			case 102:
				if (CountUnits(J, BB, CA) == 0)
					return GameResult.UnitedStatesWon;

				return CountUnits(U, BB, CA) > 0 ? AtTimeUp(GameResult.Draw) : GameResult.JapanWon;

			case 103:
				if (CountUnits(U, BB, CA) == 0)
					return GameResult.JapanWon;

				return CountUnits(J, Attacker) > 0 ? AtTimeUp(GameResult.UnitedStatesWon) : GameResult.UnitedStatesWon;

			case 104:
			{
				if (CountUnits(U, Infantry, Pillboxes, Fortress) == 0)
					return GameResult.JapanWon;

				var fueled = 0;
				for (var i = 1; i <= MaxUnitId; i++)
				{
					ref var unit = ref Units[i];
					if (unit.Side == J && unit.Kind >= BB && unit.Kind <= UnitKind.Transport && (int)unit.Fuel != -1)
						fueled++;
				}

				return fueled > 0 ? AtTimeUp(GameResult.UnitedStatesWon) : GameResult.UnitedStatesWon;
			}

			case 107:
			{
				if (CountUnits(J, Bomber) == 0)
					return GameResult.UnitedStatesWon;

				for (var i = 1; i <= MaxUnitId; i++)
				{
					ref var unit = ref Units[i];
					if (unit.Side == J && unit.Kind == Bomber && unit.PlaneState == UnitState.Parked && unit.Weapon == FireKind.Maintenance)
						return GameResult.JapanWon;
				}

				return AtTimeUp(GameResult.JapanWon);
			}

			case 108:
				return CountUnits(J, Infantry, Pillboxes) > 0 ? AtTimeUp(GameResult.JapanWon) : GameResult.UnitedStatesWon;

			case 109:
			{
				var east = new RECT { left = 7000, top = -8000, right = 9000, bottom = -6000 };
				if (CountAnchoredUnits(J) == 0 && AreAllShipsIn(ref east, U, false))
					return GameResult.UnitedStatesWon;

				var american = 0;
				for (var i = 1; i <= MaxUnitId; i++)
				{
					if (Units[i].Side == U)
						american++;
				}

				return american == 0 ? GameResult.JapanWon : AtTimeUp(GameResult.UnitedStatesLost);
			}

			case 200:
				if (CountUnits(U, UnitKind.AirBase, Infantry, Pillboxes, Fortress) == 0)
					return GameResult.JapanWon;

				return CountUnits(J, CV) > 0 ? AtTimeUp(GameResult.UnitedStatesWon) : GameResult.UnitedStatesWon;

			case 201:
				return CountUnits(U, CV, CVL) > 0 ? AtTimeUp(GameResult.UnitedStatesWon) : GameResult.JapanWon;

			case 202:
				if (CountUnits(U, Bomber) == 0)
					return GameResult.JapanWon;

				return CountUnits(J, UnitKind.City) > 0 ? AtTimeUp(GameResult.Draw) : GameResult.UnitedStatesWon;

			case 203:
				if (CountUnits(U, Pillboxes) == 0)
					return GameResult.JapanWon;

				return CountUnits(J, CV, CVL, BB, CA, UnitKind.Transport) > 4 ? AtTimeUp(GameResult.UnitedStatesWon) : GameResult.UnitedStatesWon;

			case 204:
				return CountUnitsIn(ref island, true, U, Infantry, Pillboxes, Fortress) == 0 ? GameResult.JapanWon : AtTimeUp(GameResult.UnitedStatesWon);

			case 205:
				return DecideScenario205();

			case 206:
				if (CountUnits(J, BB) == 0)
					return GameResult.UnitedStatesWon;

				return CountUnits(U, Infantry, Pillboxes, Fortress) > 0 ? AtTimeUp(GameResult.Draw) : GameResult.JapanWon;

			case 207:
			{
				var harbor = new RECT { left = 4600, top = -3400, right = 6600, bottom = -1400 };
				if (!Units[DecisionPoints[0]].IsUsed && AreAllShipsIn(ref harbor, U, true))
					return GameResult.UnitedStatesWon;

				return CountUnits(U, BB, CA) != 0 ? AtTimeUp(GameResult.UnitedStatesLost) : GameResult.JapanWon;
			}

			case 208:
				if (CountUnits(J, BB, CA, DD, Attacker, Bomber) == 0)
					return GameResult.UnitedStatesWon;

				return CountUnits(U, Infantry, Pillboxes) > 0 ? AtTimeUp(GameResult.UnitedStatesWon) : GameResult.JapanWon;

			case 209:
			{
				if (BattleTime != 0)
					return GameResult.None;

				if (CountUnits(U, Pillboxes, Infantry) != 0)
					return GameResult.JapanLost;

				var midway = new RECT { left = -80, top = 3120, right = 400, bottom = 3600 };
				var infantry = CountUnits(J, Infantry);
				return infantry != 0 && infantry == CountUnitsIn(ref midway, false, J, Infantry) ? GameResult.JapanWon : GameResult.JapanLost;
			}

			case 300:
			{
				if (CountUnits(U, Bomber) == 0)
					return GameResult.UnitedStatesLost;

				if (CountUnits(J, UnitKind.City) > 3)
					return AtTimeUp(GameResult.UnitedStatesLost);

				var bombers = 0;
				var away = 0;
				for (var i = 1; i <= MaxUnitId; i++)
				{
					ref var unit = ref Units[i];
					if (unit.Side == U && unit.Kind == Bomber)
					{
						bombers++;
						if (unit.Position.X < -8400 && unit.Position.Y < -6000)
							away++;
					}
				}

				return bombers == away ? GameResult.UnitedStatesWon : AtTimeUp(GameResult.UnitedStatesLost);
			}

			case 301:
			{
				for (var i = 1; i <= MaxUnitId; i++)
				{
					ref var unit = ref Units[i];
					if (unit.Side == U && IsOneOf(unit.Kind, [Infantry, Pillboxes, Fortress, CV, CVL]) && i != DecisionPoints[0])
						return AtTimeUp(GameResult.JapanLost);
				}

				var target = new RECT { left = DecisionPoints[1] - 80, top = DecisionPoints[2] - 80, right = DecisionPoints[1] + 80, bottom = DecisionPoints[2] + 80 };
				return CountUnitsIn(ref target, false, J, Infantry, Pillboxes, Fortress) > 0 ? GameResult.JapanWon : AtTimeUp(GameResult.JapanLost);
			}

			case 302:
				if (!Units[DecisionPoints[0]].IsUsed && !Units[DecisionPoints[1]].IsUsed)
					return GameResult.JapanWon;

				return CountUnits(J, CV, CVL) > 2 ? AtTimeUp(GameResult.JapanLost) : GameResult.JapanLost;

			case 303:
				return DecideScenario303();

			case 304:
				return CountUnits(J, Infantry, Pillboxes, Fortress) > 0 ? AtTimeUp(GameResult.UnitedStatesLost) : GameResult.UnitedStatesWon;

			case 305:
				if (CountUnits(U, Infantry, Pillboxes, Fortress) == 0)
					return GameResult.JapanWon;

				return CountUnits(J, BB, CA, DD) == 0 ? GameResult.JapanLost : AtTimeUp(GameResult.JapanLost);

			case 306:
			{
				var american = CountUnitsIn(ref island, true, U, Infantry, Pillboxes, Fortress);
				if (american == 0 && CountUnitsIn(ref island, true, J, Infantry, Pillboxes, Fortress) >= 2)
					return GameResult.JapanWon;

				return AtTimeUp(american != 0 ? GameResult.UnitedStatesWon : GameResult.JapanLost);
			}

			default:
				return GameResult.None;
		}
	}

	// decision's reinforcements: a plane, by the chances of its kind in percent, for Japan's unit 1, while it has room.
	private void ReinforceUnit1(int planes, int attackerChance, int secondChance, UnitKind second, UnitKind third)
	{
		if (!Units[1].IsUsed || planes >= Units[1].Capacity)
			return;

		var r = rand() % 100;
		var kind = r <= attackerChance ? UnitKind.Attacker : r <= secondChance ? second : third;
		AddPlane(Side.Japan, kind, 0, 1, 1, FireKind.Unarmed);
	}

	// decision's events of scenarios 204, 301 and 304, which run whether or not the scenario has a result.
	private void ApplyScenarioEvents()
	{
		switch (ScenarioNumber)
		{
			case 204:
				if (BattleTime % 1250 == 0 || (BattleTime < 13000 && BattleTime % 300 == 0))
				{
					var planes = 0;
					for (var i = 1; i <= MaxUnitId; i++)
					{
						ref var unit = ref Units[i];
						if (unit.Side == Side.Japan && unit.Category == UnitCategory.Plane && unit.Carrier == 1)
							planes++;
					}

					ReinforceUnit1(planes, 50, 95, UnitKind.Bomber, UnitKind.Fighter);
				}
				break;

			case 301:
				if (BattleTime % 2500 == 0 && Units[1].IsUsed)
					ReinforceUnit1(CountPlanesIn(1), 60, 98, UnitKind.Fighter, UnitKind.Bomber);
				break;

			case 304:
				if (BattleTime % 4500 == 0 && Units[1].IsUsed)
					ReinforceUnit1(CountPlanesIn(1), 75, 98, UnitKind.Fighter, UnitKind.Bomber);
				break;
		}
	}

	// decision (0x41C3C0): the scenario's result, shown once it has one, and its events.
	public void Decide()
	{
		if (Result == GameResult.None)
			Result = DecideScenario();

		if (Result != GameResult.None)
		{
			Array5<Array128<byte>> ach = default;
			Array5<int> len = default;
			DrawResult(ref len, ref ach);
		}

		ApplyScenarioEvents();
	}
}
