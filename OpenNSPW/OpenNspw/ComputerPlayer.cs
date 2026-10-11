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
}
