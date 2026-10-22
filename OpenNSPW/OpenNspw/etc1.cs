//
//                                    **
//                                   *  *
//                                    **  *
//                                    **   *
//                                    **  *
//                            *     ******     *
//                            ***   * **     ***
//                              ****  **  ****
//                             **  ********
//
//                 Ｎａｖａｌ Ｓｏｕｔｈ Ｐａｃｉｆｉｃ Ｗａｒ
//                             Ｏｎ　ｔｈｅ　Ｎｅｔ

// Port of etc1.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{

//============================================================================
// 当たりチェック
//----------------------------------------------------------------------------
[Original("hit_chk")]
public int		CheckHit(int m)
	{
	int size=default /* C4701 */, halfSize=default /* C4701 */;
	WorldRect	hitArea;

	var hit=0;

	var target=Fires[m].Target;						// ターゲットナンバー
	switch( Units[target].Kind )
		{
		case UnitKind.Battleship:	size=16;halfSize=size/2;	break;
		case UnitKind.Cruiser:	size=12;halfSize=size/2;	break;
		case UnitKind.Destroyer:	size=10;halfSize=size/2;		break;
		case UnitKind.Submarine:
			if( Units[target].IsSubmerged )// 潜航中、あたりがでかくなる
				{
				// ptin dbg
				hitArea=WorldRect.Around(Units[target].Position, 50, 50);

				if( hitArea.Contains(Fires[m].Position) )
					hit=1;
				return	(hit);
				}
			else
				{	size=Rules.SurfacedSubmarineHitSize;halfSize=size/2;		}

				break;
		case UnitKind.Carrier:	size=14;halfSize=size/2;	break;
		case UnitKind.LightCarrier:	size=12;halfSize=size/2;	break;
		case UnitKind.AirBase:
		case UnitKind.NavalBase:
		case UnitKind.Fortress:	case UnitKind.Pillboxes:	case	UnitKind.InfantryBase:
		case UnitKind.City:	case UnitKind.Mine:
				// ptin dbg
				hitArea=WorldRect.Around(Units[target].Position, 30, 30);

				if( hitArea.Contains(Fires[m].Position) )
					hit=1;

				return	(hit);
				break;
		case UnitKind.Transport:	size=12;halfSize=size/2;	break;
		}
	hit=0;
	var octant=ToEightDirections((int)(Units[target].Direction));
	switch( octant )
		{
		case 3: case 7:
			for( var i=0; i<=4 && hit==0 ; i++)
				{
				// ptin dbg
				hitArea=WorldRect.Around(Units[target].Position, halfSize, halfSize).Offset((+size-(i*halfSize)), (-size+(i*halfSize)));

				if( hitArea.Contains(Fires[m].Position) )
					hit=1;
				}
			break;
		case 1: case 5:
			for( var i=0; i<=4 && hit==0 ; i++)
				{
				// pt in dbg
				hitArea=WorldRect.Around(Units[target].Position, halfSize, halfSize).Offset((-size+(i*halfSize)), (-size+(i*halfSize)));

				if( hitArea.Contains(Fires[m].Position) )
					hit=1;
				}

			break;
		case 0: case 4:
			for( var i=0; i<=2  && hit==0 ; i++)
				{
				// ptin dbg
				hitArea=WorldRect.Around(Units[target].Position, halfSize, halfSize).Offset(0, (-size+(i*size)));

				if( hitArea.Contains(Fires[m].Position) )
					hit=1;
				}
			break;
		case 2: case 6:
			for( var i=0; i<=2  && hit==0 ; i++)
				{
				// ptin dbg
				hitArea=WorldRect.Around(Units[target].Position, halfSize, halfSize).Offset((-size+(i*size)), 0);

				if( hitArea.Contains(Fires[m].Position) )
					hit=1;
				}
			break;
		}

	return	(hit);
	}

//============================================================================
// 当たりチェック
//----------------------------------------------------------------------------
[Original("draw_hit_area")]
public void		DrawHitArea(int m)
	{
	int size=default /* C4701 */, halfSize=default /* C4701 */;

	var target=m;						// ターゲットナンバー
	switch( Units[target].Kind )
		{
		case UnitKind.Battleship:	size=16;halfSize=size/2;	break;
		case UnitKind.Cruiser:	size=12;halfSize=size/2;	break;
		case UnitKind.Destroyer:	size=6;halfSize=size/2;	break;
		case UnitKind.Submarine:
			if( Units[target].IsSubmerged )// 潜航中、あたりがでかくなる
				{
				// The box is upside down, its top below its bottom, as the original fills it: the same outline.
				var hitArea=WorldRect.Around(Units[target].Position, 50, -50);

				DrawWorldRect(hitArea, RGB(255,255,255));

				return;
				}
			else
				{	size=4;halfSize=size/2;		}

				break;
		case UnitKind.Carrier:	size=14;halfSize=size/2;	break;
		case UnitKind.LightCarrier:	size=12;halfSize=size/2;	break;
		}
	var octant=ToEightDirections((int)(Units[target].Direction));
	switch( octant )
		{
		case 3: case 7:
			for( var i=0; i<=4  ; i++)
				{
				var hitArea=WorldRect.Around(Units[target].Position, halfSize, -halfSize).Offset((+size-(i*halfSize)), (-size+(i*halfSize)));

				DrawWorldRect(hitArea, RGB(255,255,255));
				}
			break;
		case 1: case 5:
			for( var i=0; i<=4  ; i++)
				{
				var hitArea=WorldRect.Around(Units[target].Position, halfSize, -halfSize).Offset((-size+(i*halfSize)), (-size+(i*halfSize)));

				DrawWorldRect(hitArea, RGB(255,255,255));
				}

			break;
		case 0: case 4:
			for( var i=0; i<=2   ; i++)
				{
				var hitArea=WorldRect.Around(Units[target].Position, halfSize, -halfSize).Offset(0, (-size+(i*size)));

				DrawWorldRect(hitArea, RGB(255,255,255));

				}
			break;
		case 2: case 6:
			for( var i=0; i<=2   ; i++)
				{
				var hitArea=WorldRect.Around(Units[target].Position, halfSize, -halfSize).Offset((-size+(i*size)), 0);

				DrawWorldRect(hitArea, RGB(255,255,255));

				}
			break;
		}

	return	;
	}

private bool LandCargo(ref Unit unit, FireKind kind, int m)
	{
	var bearing=(unit.LandingPoint-unit.Position).Direction;
	var relativeBearing=bearing-unit.Direction;
	relativeBearing=relativeBearing.WrapNegative();

	if( (int)relativeBearing<=45||(int)relativeBearing>=315)
		{
		// 距離を求めます
		var distance=(unit.Position-unit.LandingPoint).Length;

		// NSPW NET lands from 160 (#if NSPW_THE_NET); 1.22, from 360 (0x413003).
		if( distance>=0 && distance<=Rules.CargoLandingRange )
			{
			// 発射！
			unit.Ammo=0;		// 残弾が０
			unit.Target=0;		// ターゲットをクリア

			//unit[m].arm[0]=NTG;		//  輸送船はこれやっと来ます、武装品種
			unit.MaxAmmo=0;		//  輸送船はこれやっと来ます、武装品種

			//unit[m].info[5]=RETURN;		// 航空機はメイン兵器ゼロで帰投

			// 部下、多分戦闘機に帰投命令

			var f=FindFreeFire();
			ref var fire = ref Fires[f];
			if( f!=0 )
				{
				PlaySoundEffect(SoundId.Splash, unit.Position);
				fire.Target=MaxUnitId+1;
				fire.Kind=kind;
				fire.Position=unit.Position;
				fire.Direction=bearing;
				fire.Speed=1.0;
				fire.Acceleration=+0.0;
				fire.FinalSpeed=0.0;
				fire.Ticks=0;
				fire.FlightTime=360;
				fire.TargetX=unit.LandingX;
				fire.TargetY=unit.LandingY;
				fire.ShooterSide=(int)unit.Side;

				// 1.22 keeps the transport, empty (0x413014 to 0x41312B); NSPW NET removes it.
				if( !Rules.TransportsLeaveAfterLanding )
					return false;

				unit.Side=0;
				unit.Hp=0;

				if( UnitInfoPanel[0]!=0 && UnitInfoPanel[3]==m )
					UnitInfoPanel[0]=0;

				if( SelectedUnit==m )
					{
					SelectedUnit=0; Selections[1][m]=0;	CombatMenuKind=0; CombatMenuSelection=CombatMenuItem.None;
					ClearSelection2(1);
					BufferedMoveOrders[1].ClearsPath=0;

					if( UnitInfoPanel[3]==m )
						UnitInfoPanel[0]=0;

//							unit_info[1]=0;				// 空母なら１で格納庫 ０ で飛行甲板
//							unit_info[3]=0;//m;				// そのユニットの番号
//							unit_info[4]=0;//unit[m].used;	// そのユニットの国籍

					}
				}
			}
		}
	return false;
	return true;
	}

private bool ShipFireRapidAntiAircraftShell(ref Unit unit, ref Angle bearing, ref double distance, FireKind kind)
	{
	var target=0;
	for(var n=1;n<=MaxUnitId;n++)
		{
		ref var other = ref Units[n];	//敵を探す。
		if( other.IsUsed && other.Category==UnitCategory.Plane && (((other.Kind==UnitKind.Attacker||other.Kind==UnitKind.Fighter) && other.Ammo!=0 )|| Random(10)==0 )  && other.PlaneState==UnitState.Flying && other.Side!=unit.Side && other.IsFound )
			{
			// 攻撃地点から攻撃目標地点への距離
			var flightTicks=(other.Position-unit.Position).Length;
			flightTicks/=10.0;

			// 攻撃地点から攻撃目標地点への絶対方位、方位角
			var futurePosition=other.Position;
			var turnRate=other.TurnRate;
			bearing=other.Direction;
			for(var f=0;f<=(int)flightTicks;f++)
				{
				bearing+=turnRate;
				bearing=bearing.WrapNegative().WrapFullTurn();
				futurePosition+=WorldVector.FromDirection(bearing) * other.Speed; // とりあえずターン後
				}

			bearing=(futurePosition-unit.Position).Direction;

			// 攻撃地点から攻撃目標地点への距離
			distance=(futurePosition-unit.Position).Length;

			if( distance>=40 && distance<=300 )
				{
				target=n;
				if( 1!=0  )
					break;
				else
					target=0;
				}
			}
		}
	if( target!=0 )
		{

		/*
		f=fc[2];	// 後面
		if( drctn3>=315.0 || drctn3<=45.0 )
			f=fc[0];	// 正面
		if( (drctn3>=45.0 && drctn3<=135.0) || (drctn3>=225.0 && drctn3<=315.0) )
			f=fc[1];	// 側面
		*/

		var f=FindFreeFire();
		ref var fire = ref Fires[f];
		if( f!=0 )
			{
			if(unit.Ammo!=0)
				unit.Ammo-=RAS_SZ;			// 弾薬消費

			// 1.22 plays AA_SHL3 alone, drawing nothing: it has no AA_SHL5 (0x41019F).
			if( !Rules.RapidFireSoundVaries || SharedRandom(2)!=0 )
				PlaySoundEffect(SoundId.AntiAircraftShell3, unit.Position);
			else
				PlaySoundEffect(SoundId.AntiAircraftShell5, unit.Position);

			fire.Target=target;
			fire.Kind=kind;
			fire.Position=unit.Position;

			var direction=bearing+(Random(18)-9);			// 絶対方位
			direction=direction.Wrapped();
			fire.Direction=direction;

			var spreadDistance=distance+(Random( ((int)(distance/5)) )-((int)(distance/10))   );

			fire.Speed=10.0;
			fire.Acceleration=-0.00;
			fire.FinalSpeed=0;
			fire.Ticks=(int)(spreadDistance/fire.Speed);
			fire.FlightTime=0;
			}
		}
	return false;
	return true;
	}

// A heading reduced to whole degrees from 0 to 359, as NSPW NET does: a depth charge's direction from its ship, a
// fighter's to dodge a head-on pass, a ship's to evade a torpedo or a plane, a ship's wake behind it. 1.22 means to, but
// stores the int into the low half of the double and reads the double back (0x410452, 0x415324, 0x416417, 0x416838,
// 0x41C327): the heading keeps its high bits, so all of its fraction of a degree but the last digits, and the wake's is
// not brought below 360.
private Angle ReduceHeading(Angle heading)
	{
	var whole=(int)heading%360;
	if( !Rules.ReducedHeadingsKeepTheirFraction )
		return whole;

	var bits=BitConverter.DoubleToInt64Bits(heading.Degrees);
	return BitConverter.Int64BitsToDouble((bits & unchecked((long)0xFFFFFFFF00000000)) | (uint)whole);
	}

private bool ShipDropAntiSubmarineBomb(ref int target, ref Unit unit, FireKind kind)
	{
	for( var n=1; n<=MaxUnitId; n++)
		{
		target=n;
		if( Units[target].Side!=unit.Side && Units[target].Kind==UnitKind.Submarine && Units[target].IsSubmerged && Units[target].IsFound )
			{	// 爆雷

			// ptin dbg
			var contactArea=WorldRect.Around(Units[target].ContactPosition, Units[target].ContactRadius, Units[target].ContactRadius);
			if( contactArea.Contains(unit.Position) )
				{
				// 投雷
				var f=FindFreeFire();
				ref var fire = ref Fires[f];
					if( f!=0 )
					{
					if(unit.Ammo!=0)
						unit.Ammo-=ASB_SZ;			// 弾薬消費
					// 1.22 leaves an odd count at -1 (0x4103B6).
					if(unit.Ammo<0 && Rules.DepthChargeCountStopsAtZero)
						unit.Ammo=0;

					PlaySoundEffect(SoundId.Splash, unit.Position);
					fire.Target=target;
					fire.Kind=kind;
					fire.Position=unit.Position;

					var direction=unit.Direction;
					if( unit.Variant==0 )
						{
						// ただの駆逐艦
						direction+=180;
						direction=ReduceHeading(direction);
						fire.Position += WorldVector.FromDirection(direction) * 20;
						}
					else
						{
						// 対潜駆逐艦
						direction+=120+Random(3)*60;
						direction=direction.WholeDegrees();
						fire.Position += WorldVector.FromDirection(direction) * 35;
						}

					fire.Direction=0;
					fire.Speed=0;
					fire.Acceleration=0;
					fire.FinalSpeed=0;
					fire.Ticks=0;
					fire.FlightTime=100;
					}
				return false;
				}
			}
		}
	return false;
	return true;
	}

private bool ShipFireGunWithoutTarget(ref Unit unit, FireKind kind, ref int range, ref Array3<int> shotCounts, ref Angle bearing, ref Angle relativeBearing)
	{
	var target=0;
	var targetDistance=2000.0;
	for(var n=1;n<=MaxUnitId;n++)
		{
		ref var other = ref Units[n];	//敵を探す。
		if( other.IsUsed && other.Category==UnitCategory.Ship && !(other.Kind==UnitKind.Submarine && other.IsSubmerged ) && other.Side!=unit.Side && other.IsFound  && other.Kind!=UnitKind.City )
			{
			// 攻撃地点から攻撃目標地点への距離
			var flightTicks=(other.Position-unit.Position).Length;
			flightTicks/=10.0;

			// 敵の未来位置を求めます。
			var futurePosition=other.Position+WorldVector.FromDirection(other.Direction) * (other.Speed*flightTicks); // とりあえずターン後

			// 攻撃地点から攻撃目標地点への絶対方位、方位角
			var otherBearing=(futurePosition-unit.Position).Direction;							// 絶対方位

			var otherRelativeBearing=otherBearing-unit.Direction;
			otherRelativeBearing=otherRelativeBearing.WrapNegative();							// 方位角

			// 攻撃地点から攻撃目標地点への距離
			var distance=(futurePosition-unit.Position).Length;

			switch( unit.Kind )
				{
				case UnitKind.Battleship:
					if( kind==FireKind.NavalBaseGun )
						{
						range=1120; shotCounts[0]=3;shotCounts[1]=4;shotCounts[2]=2;
						}
					else
						{
						range=600; shotCounts[0]=3;shotCounts[1]=4;shotCounts[2]=2;
						}
					break;
				case UnitKind.Cruiser:	range=500; shotCounts[0]=1;shotCounts[1]=2;shotCounts[2]=1;	break;
				case UnitKind.Destroyer:	range=400; shotCounts[0]=1;shotCounts[1]=1;shotCounts[2]=0;	break;
				case UnitKind.Submarine:	range=200; shotCounts[0]=1;shotCounts[1]=1;shotCounts[2]=0;	break;

				case UnitKind.InfantryBase:	range=600; shotCounts[0]=1;shotCounts[1]=1;shotCounts[2]=1;	break;
				case UnitKind.Pillboxes:	range=800; shotCounts[0]=2;shotCounts[1]=2;shotCounts[2]=2;	break;
				case UnitKind.Fortress:	range=1000; shotCounts[0]=3;shotCounts[1]=3;shotCounts[2]=3;	break;
				}

			if( (distance>=(range*0.3) || (unit.Kind>=UnitKind.InfantryBase&&unit.Kind<=UnitKind.Fortress) ) && distance<=range && targetDistance>=distance )
				{
				target=n;
				targetDistance=distance;
				bearing=otherBearing;
				relativeBearing=otherRelativeBearing;
				}
			}
		}

	if( target!=0 )
		{
		var shots=shotCounts[2];	// 後面
		if( relativeBearing>=315.0 || relativeBearing<=45.0 )
			shots=shotCounts[0];	// 正面
		if( (relativeBearing>=45.0 && relativeBearing<=135.0) || (relativeBearing>=225.0 && relativeBearing<=315.0) )
			shots=shotCounts[1];	// 側面

		switch( unit.Kind )
			{
			case UnitKind.Battleship:
			case UnitKind.Fortress:
				if( kind==FireKind.NavalBaseGun )
					{
					PlaySoundEffect(SoundId.Gun3, unit.Position);
					break;
					}
				PlaySoundEffect(SoundId.Gun2+Random(2), unit.Position);
				break;
			case UnitKind.Cruiser:
			case UnitKind.Pillboxes:
				PlaySoundEffect(SoundId.Gun1+Random(2), unit.Position);
				break;

			case UnitKind.Destroyer:
			case UnitKind.Submarine:
			case UnitKind.InfantryBase:
				PlaySoundEffect(SoundId.Gun1, unit.Position);
				break;
			}

		for( var i=1; i<=shots ;i++ )
			{
			var f=FindFreeFire();
			ref var fire = ref Fires[f];
			if( f!=0 )
				{
				if(unit.Ammo!=0)
					unit.Ammo-=GUN_SZ;			// 弾薬消費

				fire.Target=target;
				fire.Kind=FireKind.Gun;
				fire.Position=unit.Position;

				Angle direction;
				if( kind==FireKind.NavalBaseGun )
					{
					if( Random(2)==0 )
						direction=bearing+(double)((double)(Random(50)-25)/10)    ;			// 絶対方位
					else
						direction=bearing+(double)((double)(Random(80)-40)/10)    ;			// 絶対方位
					}
				else if( Rules.GunsAimAlike )
					{
					// 1.22 aims as closely 1 time in 5 for every kind, and draws the hundredths, always 0, first (0x410A65).
					var narrow = Random(5)==0 || ( unit.Kind==UnitKind.Battleship && Random( 4 )==0 );
					var hundredths = Random(100)/100;
					direction=bearing+(narrow ? Random(7)-3 : Random(11)-5)+hundredths;
					}
				else
					{
					if( Random(5+(unit.Kind>=UnitKind.InfantryBase&&unit.Kind<=UnitKind.Fortress ? 1 : 0)*4  )==0 || ( unit.Kind==UnitKind.Battleship && Random( 4 )==0 ) )
						direction=bearing+(Random(7)-3)+(Random(100)/100);			// 絶対方位
					else
						direction=bearing+(Random(11)-5)+(Random(100)/100);				// 絶対方位
					}

				direction=direction.Wrapped();
				fire.Direction=direction;

				if( kind==FireKind.NavalBaseGun )
					targetDistance+=(Random( ((int)(targetDistance/12)) )-((int)(targetDistance/24)));
				else
					targetDistance+=(Random( ((int)(targetDistance/8)) )-((int)(targetDistance/16)));

				fire.Speed=10.0;
				fire.Acceleration=((fire.Speed)/(targetDistance/fire.Speed));
				fire.FinalSpeed=0;
				// 1.22's shell flies one tick less (0x410BCE).
				fire.Ticks=(int)(targetDistance/fire.Speed)+Rules.ExtraGunShellTicks;
				fire.FlightTime=fire.Ticks/2;

				}
			}
		}
	return false;
	return true;
	}

private bool ShipFireTorpedoWithoutTarget(ref Unit unit, ref Angle bearing, FireKind kind)
	{
	var target=0;
	for(var n=1;n<=MaxUnitId;n++)
		{
		ref var other = ref Units[n];	//敵を探す。
		if( other.IsUsed && other.Category==UnitCategory.Ship && !(other.Kind==UnitKind.AirBase||other.Kind==UnitKind.NavalBase||other.Kind==UnitKind.InfantryBase||other.Kind==UnitKind.Pillboxes||other.Kind==UnitKind.Fortress) && other.Side!=unit.Side && other.IsFound && !(other.Kind==UnitKind.Submarine||other.Kind==UnitKind.Destroyer) )
			{
			// 攻撃地点から攻撃目標地点への距離
			var runTicks=(other.Position-unit.Position).Length;
			runTicks/=TPD_SPD;							// 撃つ弾の速度で割る

			// 攻撃地点から攻撃目標地点への絶対方位、方位角
			var futurePosition=other.Position+WorldVector.FromDirection(other.Direction) * (other.Speed*runTicks);	// 標的の未来位置
/****
			drctn=unit[n].drctn_add;
			drctn2=unit[n].drctn;
			for(f=0;f<=(int)turn;f++)
				{
				drctn2+=drctn;
				if(drctn2<0)		drctn2=360+drctn2;
				if(drctn2>=360)		drctn2=drctn2-360;
				wrk_x+=CosDegrees(drctn2)*(unit[n].spd); // とりあえずターン後
				wrk_y+=SinDegrees(drctn2)*(unit[n].spd);
				}
***/
			bearing=(futurePosition-unit.Position).Direction;					// 未来位置への絶対角

			var relativeBearing=bearing-unit.Direction;
			relativeBearing=relativeBearing.WrapNegative();							// 方位角

			// 攻撃地点から攻撃目標地点への距離
			var distance=(futurePosition-unit.Position).Length;

			if( (distance>=100 && distance<=(500+(unit.Side==Side.Japan ? 1 : 0)*100)) && ((relativeBearing>=45&&relativeBearing<=135)||(relativeBearing>=225&&relativeBearing<=315)) )
				{
				// ばってん陸地があるけんしらべる
				target=n;

				var ticks=(int)(distance/TPD_SPD);
				for(var f=1;f<=ticks;f++)
					{
					if( IsLandAt(unit.Position+WorldVector.FromDirection(bearing) * (TPD_SPD*f)) ) // とりあえずターン後
						{
						target=0;
						break;
						}
					}

				if(target!=0)
					{
					if( Random(3)==0 )
						break;
					else
						target=0;
					}
				}
			}
		}
	if( target!=0 )
		{

		// 発射！
		if(unit.Ammo!=0)
			unit.Ammo-=TPD_SZ;
		switch( unit.Kind )
			{
			case UnitKind.Cruiser:
			case UnitKind.Destroyer:		unit.ReloadTime=RELOAD_TPD_DD;		break;	// 再装填時間
			case UnitKind.Submarine:		unit.ReloadTime=Rules.SubmarineTorpedoReload;
							//撃った瞬間に発見される。
							unit.ContactX=(int)(unit.Position.X+Random((50)*2)-50);
							unit.ContactY=(int)(unit.Position.Y+Random((50)*2)-50);

							unit.ContactRadius=100;
							unit.ContactTime=300;

							unit.IsFound=true;

							break;	// 再装填時間
			}

		int lastTorpedo;
		if( unit.Side==Side.Japan  )
			{
			if(unit.Kind==UnitKind.Destroyer)
				lastTorpedo=2;						// 日本海軍駆逐艦魚雷３発
			else
				lastTorpedo=1;						// 日本海軍巡洋艦魚雷２はつ
			}
		else
			{
			lastTorpedo=0;						// 合衆国海軍魚雷１発
			}

		if( unit.Kind!=UnitKind.Submarine )
			PlaySoundEffect(SoundId.TorpedoLaunch, unit.Position);

		for( var torpedo=0; torpedo<=lastTorpedo; torpedo++)
			{
			var f=FindFreeFire();
			ref var fire = ref Fires[f];
			if( f!=0 )
				{
				fire.Target=target;
				fire.Kind=kind;
				fire.Position=unit.Position;

				switch( torpedo )
					{
					case 0:	fire.Direction=bearing;	break;
					case 1:	fire.Direction=bearing+5;	break;
					case 2:	fire.Direction=bearing-5;	break;
					}
				fire.Direction=fire.Direction.WholeDegrees();

				fire.Speed=TPD_SPD;
				fire.Acceleration=+0.0;
				fire.FinalSpeed=0.0;
				if( unit.Kind==UnitKind.Submarine )
					fire.Ticks=1;
				else
					fire.Ticks=0;
				fire.FlightTime=275+(unit.Side==Side.Japan ? 1 : 0)*110;
				fire.ArmingTime=30;
				}
			}
		}
	return false;
	return true;
	}

private bool ShipFireTorpedoAtTarget(int target, ref Unit unit, ref Angle bearing, FireKind kind)
	{
	var confirmedTarget=0;
	ref var other = ref Units[target];
	if( other.IsUsed && other.Category==UnitCategory.Ship && !(other.Kind==UnitKind.AirBase||other.Kind==UnitKind.NavalBase||other.Kind==UnitKind.InfantryBase||other.Kind==UnitKind.Pillboxes||other.Kind==UnitKind.Fortress) && other.Side!=unit.Side && other.IsFound )
		{

		// 攻撃地点から攻撃目標地点への距離
		var runTicks=(other.Position-unit.Position).Length;
		runTicks/=TPD_SPD;							// 撃つ弾の速度で割る

		// 攻撃地点から攻撃目標地点への絶対方位、方位角
		var futurePosition=other.Position+WorldVector.FromDirection(other.Direction) * (other.Speed*runTicks);	// 標的の未来位置

		bearing=(futurePosition-unit.Position).Direction;					// 未来位置への絶対角

		var relativeBearing=bearing-unit.Direction;
		relativeBearing=relativeBearing.WrapNegative();							// 方位角

		// 攻撃地点から攻撃目標地点への距離
		var distance=(futurePosition-unit.Position).Length;

		if( (distance>=100 && distance<=(550+(unit.Side==Side.Japan ? 1 : 0)*100)) && (relativeBearing<=5 || relativeBearing>=355) )
			{
			confirmedTarget=target;
			var ticks=(int)(distance/TPD_SPD);
			for(var f=1;f<=ticks;f++)
				{
				if( IsLandAt(unit.Position+WorldVector.FromDirection(bearing) * (TPD_SPD*f)) ) // とりあえずターン後
					{
					confirmedTarget=0;
					break;
					}
				}
			}
		}

	if( confirmedTarget!=0 )
		{
		// 発射！
		if(unit.Ammo!=0)
			unit.Ammo-=TPD_SZ;
		switch( unit.Kind )
			{
			case UnitKind.Destroyer:		unit.ReloadTime=RELOAD_TPD_DD;		break;	// 再装填時間
			case UnitKind.Submarine:		unit.ReloadTime=Rules.SubmarineTorpedoReload;
							//撃った瞬間に発見される。
							unit.ContactX=(int)(unit.Position.X+Random((50)*2)-50);
							unit.ContactY=(int)(unit.Position.Y+Random((50)*2)-50);

							unit.ContactRadius=100;
							unit.ContactTime=300;

							unit.IsFound=true;
							break;	// 再装填時間
			}

		if( unit.Kind!=UnitKind.Submarine )
			{
			PlaySoundEffect(SoundId.TorpedoLaunch, unit.Position);
			}
		else
			{
			if( !Rules.SubmarinesKeepTheirTargetAfterFiring )
				unit.Target=0;
			}

		for( var torpedo=0; torpedo<=2; torpedo++)
			{
			var f=FindFreeFire();
			ref var fire = ref Fires[f];
			if( f!=0 )
				{
				fire.Target=confirmedTarget;
				fire.Kind=kind;
				fire.Position=unit.Position;

				switch( torpedo )
					{
					case 0:	fire.Direction=bearing+5;	break;
					case 1:	fire.Direction=bearing;	break;
					case 2:	fire.Direction=bearing-5;	break;
					}
				fire.Direction=fire.Direction.WholeDegrees();

				fire.Speed=TPD_SPD;
				fire.Acceleration=+0.0;
				fire.FinalSpeed=0.0;
				if( unit.Kind==UnitKind.Submarine )
					fire.Ticks=1;
				else
					fire.Ticks=0;
				fire.FlightTime=290+(unit.Side==Side.Japan ? 1 : 0)*110;
				fire.ArmingTime=30;
				}
			}

		}
	return false;
	return true;
	}

private bool ShipFireAntiAircraftShellAtTarget(int target, ref Unit unit, ref int range, ref Array3<int> shotCounts)
	{
	if( !Units[target].IsFound )
		return false;

	var confirmedTarget=0;
	ref var other = ref Units[target];
	// 攻撃地点から攻撃目標地点への絶対方位、方位角

	var flightTicks=(other.Position-unit.Position).Length;
	flightTicks/=10.0;

	// ターゲットの未来位置を求めます。
	var futurePosition=other.Position+WorldVector.FromDirection(other.Direction) * (other.Speed*flightTicks); // ターン後

	// ターゲットの方位関係を
	var bearing=(futurePosition-unit.Position).Direction;

	var relativeBearing=bearing-unit.Direction;
	relativeBearing=relativeBearing.WrapNegative();							// 方位角

	// 攻撃地点から攻撃目標地点への距離
	var distance=(futurePosition-unit.Position).Length;

	switch( unit.Kind )
		{
		case UnitKind.Battleship:	range=700; shotCounts[0]=2;shotCounts[1]=3;shotCounts[2]=1;	break;
		case UnitKind.Cruiser:	range=600; shotCounts[0]=1;shotCounts[1]=2;shotCounts[2]=1;	break;
		case UnitKind.Destroyer:	range=300; shotCounts[0]=1;shotCounts[1]=1;shotCounts[2]=0;	break;

		case UnitKind.InfantryBase:	range=500; shotCounts[0]=1;shotCounts[1]=1;shotCounts[2]=1;	break;
		case UnitKind.Pillboxes:	range=600; shotCounts[0]=shotCounts[1]=shotCounts[2]=Random(2)+1;	break;
		case UnitKind.Fortress:	range=700; shotCounts[0]=shotCounts[1]=shotCounts[2]=Random(2)+2;	break;
		}

	if( distance>=(range*0.25) && distance<=range )
		{
		confirmedTarget=target;
		}

	if( confirmedTarget!=0 )
		{
		var shots=shotCounts[2];	// 後面
		if( relativeBearing>=315.0 || relativeBearing<=45.0 )
			shots=shotCounts[0];	// 正面
		if( (relativeBearing>=45.0 && relativeBearing<=135.0) || (relativeBearing>=225.0 && relativeBearing<=315.0) )
			shots=shotCounts[1];	// 側面

		PlaySoundEffect(SoundId.AntiAircraftShell2, unit.Position);
		for( var i=1; i<=shots ;i++ )
			{

			var f=FindFreeFire();
			ref var fire = ref Fires[f];
			if( f!=0 )
				{
				if(unit.Ammo!=0)
					unit.Ammo-=SHL_SZ;			// 弾薬消費
				fire.Target=confirmedTarget;

				fire.Kind=FireKind.AntiAircraftShell;

				fire.Position=unit.Position;

				var direction=bearing+(Random(18)-9);			// 絶対方位
				direction=direction.Wrapped();
				fire.Direction=direction;

				var spreadDistance=distance+(Random( ((int)(distance/10)) )-((int)(distance/20))   );

				fire.Speed=10.0;
				fire.Acceleration=-0.00;
				fire.FinalSpeed=0;
				fire.Ticks=(int)(spreadDistance/fire.Speed);
				fire.FlightTime=0;

				}
			}
		}
	return false;
	return true;
	}

private bool ShipFireAntiAircraftShellWithoutTarget(ref Unit unit, ref Angle bearing, ref Angle relativeBearing, ref double distance, ref int range, ref Array3<int> shotCounts, FireKind kind)
	{
	var target=0;
	for(var n=1;n<=MaxUnitId;n++)
		{
		ref var other = ref Units[n];	//敵を探す。
		if( other.IsUsed && other.Category==UnitCategory.Plane && (((other.Kind==UnitKind.Attacker||other.Kind==UnitKind.Bomber) && other.Ammo!=0 )|| Random(10)==0 )  && other.PlaneState==UnitState.Flying && other.Side!=unit.Side && other.IsFound )
			{
			// 攻撃地点から攻撃目標地点への距離
			var flightTicks=(other.Position-unit.Position).Length;
			flightTicks/=10.0;

			// 攻撃地点から攻撃目標地点への絶対方位、方位角
			var futurePosition=other.Position;
			var turnRate=other.TurnRate;
			bearing=other.Direction;
			for(var f=0;f<=(int)flightTicks;f++)
				{
				bearing+=turnRate;
				bearing=bearing.WrapNegative().WrapFullTurn();
				futurePosition+=WorldVector.FromDirection(bearing) * other.Speed; // とりあえずターン後
				}

			bearing=(futurePosition-unit.Position).Direction;

			relativeBearing=bearing-unit.Direction;
			relativeBearing=relativeBearing.WrapNegative();							// 方位角

			// 攻撃地点から攻撃目標地点への距離
			distance=(futurePosition-unit.Position).Length;

			switch( unit.Kind )
				{
				case UnitKind.Battleship:	range=700; shotCounts[0]=2;shotCounts[1]=3;shotCounts[2]=1;	break;
				case UnitKind.Cruiser:	range=600; shotCounts[0]=1;shotCounts[1]=2;shotCounts[2]=1;	break;
				case UnitKind.Destroyer:	range=300; shotCounts[0]=1;shotCounts[1]=1;shotCounts[2]=0;	break;

				case UnitKind.InfantryBase:	range=500; shotCounts[0]=1;shotCounts[1]=1;shotCounts[2]=1;	break;
				case UnitKind.Pillboxes:	range=600; shotCounts[0]=shotCounts[1]=shotCounts[2]=Random(2)+1;	break;
				case UnitKind.Fortress:	range=700; shotCounts[0]=shotCounts[1]=shotCounts[2]=Random(2)+2;	break;
				}

				if( distance>=(range*0.25) && distance<=range )
					{
					target=n;
					break;
					}
			}
		}
	if( target!=0 )
		{

		var shots=shotCounts[2];	// 後面
		if( relativeBearing>=315.0 || relativeBearing<=45.0 )
			shots=shotCounts[0];	// 正面
		if( (relativeBearing>=45.0 && relativeBearing<=135.0) || (relativeBearing>=225.0 && relativeBearing<=315.0) )
			shots=shotCounts[1];	// 側面

		for( var i=1; i<=shots ;i++ )
			{
			var f=FindFreeFire();
			ref var fire = ref Fires[f];
			if( f!=0 )
				{
				if(unit.Ammo!=0)
					unit.Ammo-=SHL_SZ;			// 弾薬消費
				PlaySoundEffect(SoundId.AntiAircraftShell2, unit.Position);
				fire.Target=target;
				fire.Kind=kind;
				fire.Position=unit.Position;

				var direction=bearing+(Random(18)-9);			// 絶対方位
				direction=direction.Wrapped();
				fire.Direction=direction;

				var spreadDistance=distance+(Random( ((int)(distance/10)) )-((int)(distance/20))   );

				fire.Speed=10.0;
				fire.Acceleration=-0.00;
				fire.FinalSpeed=0;
				fire.Ticks=(int)(spreadDistance/fire.Speed);
				fire.FlightTime=0;
				}
			}
		}
	return false;
	return true;
	}

private bool ShipFireGunAtTarget(int target, ref Unit unit, FireKind kind, ref int range, ref Array3<int> shotCounts)
	{
	if( !Units[target].IsFound )
		return false;

	var confirmedTarget=0;
	ref var other = ref Units[target];
	// 攻撃地点から攻撃目標地点への絶対方位、方位角

	// 攻撃地点から攻撃目標地点への距離
	var flightTicks=(other.Position-unit.Position).Length;
	flightTicks/=10.0;

	// ターゲットの未来位置を求めます。
	var futurePosition=other.Position+WorldVector.FromDirection(other.Direction) * (other.Speed*flightTicks); // ターン後

	// ターゲットの方位関係を
	var bearing=(futurePosition-unit.Position).Direction;

	var relativeBearing=bearing-unit.Direction;
	relativeBearing=relativeBearing.WrapNegative();							// 方位角

	// 攻撃地点から攻撃目標地点への距離
	var distance=(futurePosition-unit.Position).Length;

	// 水上艦への射程距離
	switch( unit.Kind )
		{
		case UnitKind.Battleship:
			if( kind==FireKind.NavalBaseGun )
				{
				range=1120; shotCounts[0]=3;shotCounts[1]=5;shotCounts[2]=2;
				}
			else
				{
				range=800; shotCounts[0]=3;shotCounts[1]=5;shotCounts[2]=2;
				}
			break;
		case UnitKind.Cruiser:	range=700; shotCounts[0]=2;shotCounts[1]=3;shotCounts[2]=1;	break;
		case UnitKind.Destroyer:	range=400; shotCounts[0]=1;shotCounts[1]=1;shotCounts[2]=0;	break;
		case UnitKind.Submarine:	range=300; shotCounts[0]=1;shotCounts[1]=1;shotCounts[2]=0;	break;

		case UnitKind.InfantryBase:	range=600; shotCounts[0]=1;shotCounts[1]=1;shotCounts[2]=1;	break;
		case UnitKind.Pillboxes:	range=800; shotCounts[0]=2;shotCounts[1]=2;shotCounts[2]=2;	break;
		case UnitKind.Fortress:	range=1000; shotCounts[0]=3;shotCounts[1]=3;shotCounts[2]=3;	break;
		}

	if( (distance>=150  || (unit.Kind>=UnitKind.InfantryBase&&unit.Kind<=UnitKind.Fortress) ) && distance<=range )
		{
		confirmedTarget=target;
		}

	if( confirmedTarget!=0 )
		{
		var shots=shotCounts[2];	// 後面
		if( relativeBearing>=315.0 || relativeBearing<=45.0 )
			shots=shotCounts[0];	// 正面
		if( (relativeBearing>=45.0 && relativeBearing<=135.0) || (relativeBearing>=225.0 && relativeBearing<=315.0) )
			shots=shotCounts[1];	// 側面

		switch( unit.Kind )
			{
			case UnitKind.Battleship:
			case UnitKind.Fortress:
				PlaySoundEffect(SoundId.Gun3, unit.Position);
				break;
			case UnitKind.Cruiser:
			case UnitKind.Pillboxes:
				PlaySoundEffect(SoundId.Gun2, unit.Position);
				break;

			case UnitKind.Destroyer:
			case UnitKind.Submarine:
			case UnitKind.InfantryBase:
				PlaySoundEffect(SoundId.Gun1+Random(2), unit.Position);
				break;
			}

		for( var i=1; i<=shots ;i++ )
			{

			var f=FindFreeFire();
			ref var fire = ref Fires[f];
			if( f!=0 )
				{
				unit.Ammo-=GUN_SZ;			// 弾薬消費
				fire.Target=confirmedTarget;

				Angle direction;
				double spreadDistance;
				if( Units[confirmedTarget].Category==UnitCategory.Ship )
					{
					fire.Kind=FireKind.Gun;

					fire.Position=unit.Position;

					if( kind==FireKind.NavalBaseGun && 0!=0 )
						{
						if( Random(3)!=0 )
							direction=bearing+(double)((double)(Random(20)-10)/10)    ;			// 絶対方位
						else
							direction=bearing+(double)((double)(Random(60)-30)/10)    ;			// 絶対方位
						}
					else if( Rules.GunsAimAlike )
						{
						// 1.22 draws the hundredths, always 0, first (0x4129AB).
						var narrow = Random(5)==0 || ( unit.Kind==UnitKind.Battleship && Random(4)==0 );
						var hundredths = Random(100)/100;
						direction=bearing+(narrow ? Random(3)-1 : Random(9)-4)+hundredths;
						}
					else
						{
						if( Random(5)==0 || ( unit.Kind==UnitKind.Battleship && Random(4)==0 ) )
							direction=bearing+(Random(3)-1)+(Random(100)/100);			// 絶対方位
						else
							direction=bearing+(Random(9)-4)+(Random(100)/100);				// 絶対方位
						}

					direction=direction.Wrapped();
					fire.Direction=direction;

					if( kind==FireKind.NavalBaseGun )
						spreadDistance=distance+(Random( ((int)(distance/12)) )-((int)(distance/24)));
					else
						spreadDistance=distance+(Random( ((int)(distance/8)) )-((int)(distance/16))   );

					fire.Speed=10.0;
					fire.Acceleration=((fire.Speed)/(spreadDistance/fire.Speed));
					fire.FinalSpeed=0;
					fire.Ticks=(int)(spreadDistance/fire.Speed);
					fire.FlightTime=fire.Ticks/2;
					}
				else
					{
					fire.Kind=FireKind.AntiAircraftShell;

					fire.Position=unit.Position;

					direction=bearing+(Random(20)-10);			// 絶対方位
					direction=direction.Wrapped();
					fire.Direction=direction;

					spreadDistance=distance+(Random( ((int)(distance/10)) )-((int)(distance/20))   );

					fire.Speed=10.0;
					fire.Acceleration=-0.00;
					fire.FinalSpeed=0;
					fire.Ticks=(int)(spreadDistance/fire.Speed);
					fire.FlightTime=0;
					}

				}
			}
		}
	return false;
	return true;
	}

private bool ShipFireBullet(ref int target, ref Unit unit, ref Angle bearing, FireKind kind)
	{
	target=0;
	for(var n=1;n<=MaxUnitId;n++)
		{
		ref var other = ref Units[n];	//敵を探す。
		if( other.IsUsed && ( (other.Category==UnitCategory.Plane && other.PlaneState==UnitState.Flying )  || (other.Kind>=UnitKind.InfantryBase && other.Kind<=UnitKind.Fortress ) ) && other.Kind!=UnitKind.Bomber
		&& other.Side!=unit.Side && other.IsFound )
			{
			// 全方位射撃可能
			var offset=other.Position-unit.Position;

			bearing=offset.Direction+(25-Random(50));
			bearing=abs((int)bearing)%360;

			// 攻撃地点から攻撃目標地点への距離
			var distance=offset.Length;
			if( distance<=100 )
				{
				target=n;
				if(Random(2)==1 )
					break;
				else
					target=0;
				}
			else
				{
				if( distance<=300 )
					{
					target=n;
					if(Random(8)==1 )
						break;
					else
						target=0;
					}
				}
			}
		}

	if( target!=0 )
		{
		var f=FindFreeFire();
		ref var fire = ref Fires[f];
		if( f!=0 )
			{
			//unit[m].arm[1]--;			// 弾薬消費
			PlaySoundEffect(SoundId.AntiAircraftBullet3, unit.Position);
			fire.Target=target;
			fire.Kind=kind;
			fire.Position=unit.Position;
			fire.Direction=bearing;
			fire.Speed=17.0;
			fire.Acceleration=-0.1;
			fire.FinalSpeed=15.0;
			}
		}
	return false;
	return true;
	}

private bool BomberFireBullet(ref int target, ref Unit unit, ref Angle bearing, FireKind kind)
	{
	target=0;
	for(var n=1;n<=MaxUnitId;n++)
		{
		ref var other = ref Units[n];	//敵を探す。
		if( other.IsUsed && other.Category==UnitCategory.Plane && other.PlaneState==UnitState.Flying && other.Side!=unit.Side && other.IsFound )
			{
			// 全方位射撃可能
			var offset=other.Position-unit.Position;

			bearing=offset.Direction+(Random(20)-10);
			bearing=abs((int)bearing)%360;

			// 攻撃地点から攻撃目標地点への距離
			var distance=offset.Length;
			if( distance<=100 )
				{
				target=n;
				if(Random(2)==1 )
					break;
				else
					target=0;
				}
			else
				{
				if( distance<=300 )
					{
					target=n;
					if(Random(8)==1 )
						break;
					else
						target=0;
					}
				}
			}
		}
	if( target!=0 )
		{
		var f=FindFreeFire();
		ref var fire = ref Fires[f];
		if( f!=0 )
			{
			//unit[m].arm[1]--;			// 弾薬消費
			PlaySoundEffect(SoundId.AntiAircraftBullet4, unit.Position);
			fire.Target=target;
			fire.Kind=kind;
			fire.Position=unit.Position;
			fire.Direction=bearing;
			fire.Speed=17.0;
			fire.Acceleration=-0.1;
			fire.FinalSpeed=15.0;
			}
		}
	return false;
	return true;
	}

private bool FighterFireBullet(ref int target, ref Unit unit, FireKind kind)
	{
	target=0;		var targetDistance=5000.0;
	for(var n=1;n<=MaxUnitId;n++)
		{
		ref var other = ref Units[n];	//前方の敵を探す。
		// NSPW NET's fighters also shoot transports; 1.22's, flying planes only (0x4133B2).
		if( other.IsUsed && (( other.Category==UnitCategory.Plane && other.PlaneState==UnitState.Flying ) || ( other.Kind==UnitKind.Transport && Rules.FightersAttackTransports ) ) && other.Side!=unit.Side )
			{
			// 距離を調べます
			var offset=other.Position-unit.Position;
			var distance=offset.Length;
			if( distance<=300 && distance<=targetDistance)
				{
				// 攻撃地点から攻撃目標地点への方位角
				var relativeBearing=offset.Direction;
				relativeBearing-=unit.Direction;
				relativeBearing=relativeBearing.WrapNegative();
				if( ((int)relativeBearing<=10||(int)relativeBearing>=350) && unit.Target!=0 && unit.Target==n )
					{
					targetDistance=distance;
					target=n;
					}
				}

			if( unit.Target==0 )
				{
				// A returning fighter takes a target only by 1.22's rules: if it is the computer's, within 300, with ammunition
				// and fuel for 60 (50 for Japan) (0x413542).
				if(unit.Mode==UnitMode.Return)
					{
					if( Rules.ReturningFightersRetarget && unit.Side==CpuSide && unit.Fuel>=(6-(unit.Side==Side.Japan ? 1 : 0))*10 && unit.Ammo!=0 && distance<=300 && Random(10)==0 && other.IsFound )
						unit.Target=n;
					}
				else
					{
					if( distance<=(other.Kind==UnitKind.Attacker||other.Kind==UnitKind.Bomber ? Rules.FighterTargetRangeAgainstBombers : 400) && Random(10)==0 )
						{
						if( other.IsFound )
							{
							unit.Target=n;
							}
						}
					}
				}
			}
		}

	if( target!=0 )
		{
		var f=FindFreeFire();
		ref var fire = ref Fires[f];
		if( f!=0 )
			{
			unit.Ammo--;			// 弾薬消費
			if(unit.Variant==0)
				{
				// 艦上戦闘機
				if(unit.Side==Side.Japan)
					PlaySoundEffect(SoundId.AntiAircraftBullet1, unit.Position);
				else
					PlaySoundEffect(SoundId.AntiAircraftBullet2, unit.Position);
				}
			else
				{
				// 陸上戦闘機
				PlaySoundEffect(SoundId.AntiAircraftShell4, unit.Position);
				}

			fire.Target=target;
			fire.Kind=kind;
			fire.Position=unit.Position;
			fire.Direction=unit.Direction;
			fire.Speed=16.0;
			fire.Acceleration=-0.1;
			fire.FinalSpeed=14.0;
			}
		}
	return false;
	return true;
	}

private bool PlaneFireBullet(ref int target, ref Unit unit, ref Angle bearing, FireKind kind)
	{
	target=0;
	for(var n=1;n<=MaxUnitId;n++)
		{
		ref var other = ref Units[n];	//後方の敵を探す。
		if( other.IsUsed && other.Category==UnitCategory.Plane && other.PlaneState==UnitState.Flying && other.Side!=unit.Side && other.IsFound )
			{
			// 攻撃地点から攻撃目標地点への方位角
			var offset=other.Position-unit.Position;
			var direction=offset.Direction;

			bearing=direction+(Random(10)-5);
			bearing=bearing.Wrapped();

			var relativeBearing=direction-unit.Direction;
			relativeBearing=relativeBearing.WrapNegative();
			if( (int)relativeBearing>=150&&(int)relativeBearing<=210 )
				{
				var distance=offset.Length;
				if( distance<=300 )
					{	target=n;	break;	}
				}
			}
		}
	if( target!=0 )
		{
		var f=FindFreeFire();
		ref var fire = ref Fires[f];
		if( f!=0 )
			{
			//unit[m].arm[1]--;			// 弾薬消費
			PlaySoundEffect(SoundId.AntiAircraftBullet3, unit.Position);
			fire.Target=target;
			fire.Kind=kind;
			fire.Position=unit.Position;
			fire.Direction=bearing;
			fire.Speed=16.0;
			fire.Acceleration=-0.1;
			fire.FinalSpeed=14.0;
			}
		}
	return false;
	return true;
	}

private bool PlaneDropTorpedo(ref Unit unit, ref int target, ref Angle bearing, int m, FireKind kind)
	{
	if( World.Contains(unit.Position) )
		{
		var (column, row)=TileAt(unit.Position);

		// BUG(1.22, NSPW NET): at the map's edges, the tiles around read beyond cmbt_map, or wrap to the next row. Kept as
		// is for compatibility with 1.22 and NSPW NET: in a single-player game, what 1.22's memory holds there
		// (MapTileOrBeyond); in a network game, sea, where NSPW NET's memory is not known.
		if( MapTileOrBeyond(row,column)>=1
			|| MapTileOrBeyond(row-1,column-1)>=1
			|| MapTileOrBeyond(row-1,column)>=1
			|| MapTileOrBeyond(row-1,column+1)>=1

			|| MapTileOrBeyond(row,column-1)>=1
			|| MapTileOrBeyond(row,column+1)>=1

			|| MapTileOrBeyond(row+1,column-1)>=1
			|| MapTileOrBeyond(row+1,column)>=1
			|| MapTileOrBeyond(row+1,column+1)>=1
			)
			{
			return false;
			}
		}

		// 攻撃地点から攻撃目標地点への方位角
		if( !Units[target].IsFound )
	return false;

		var futurePosition=Units[target].Position+WorldVector.FromDirection(Units[target].Direction) * ((AIR_TPD_LOS_DSTC/AIR_TPD_SPD)*Units[target].Speed); // とりあえずターン後

		var relativeBearing=(futurePosition-unit.Position).Direction;

		relativeBearing-=unit.Direction;
		relativeBearing=relativeBearing.WrapNegative();

		if( (int)relativeBearing<=45||(int)relativeBearing>=315)
	{
	// 距離を求めます
	var offset=(unit.Position-futurePosition).NonZero();
	// BUG(NSPW NET): the bearing from the aim to the plane, so the land is checked behind the plane. Kept as is for
	// compatibility with NSPW NET.
	if( !Rules.TorpedoPlanesCheckLandAlongUnsetHeading )
		bearing=offset.Direction;					// 未来位置への絶対角
	var distance=offset.Length;

	if( distance>=100 && distance<=AIR_TPD_LOS_DSTC )
		{
		var ticks=(int)(distance/2);
		// 1.22 reads the heading it has not set as the loop starts (0x413D23).
		if( ticks>=1 && Rules.TorpedoPlanesCheckLandAlongUnsetHeading )
			bearing=TakeStackLocal(0x413D23, bearing.Degrees);
		for(var f=1;f<=ticks;f++)
			{
			if( IsLandAt(unit.Position+WorldVector.FromDirection(bearing) * (2*f)) ) // とりあえずターン後
				{
				return false;
				}
			}

		// 発射！
		unit.Ammo=0;		// 魚雷が０
		unit.Target=0;		// ターゲットをクリア

		unit.Mode=UnitMode.Return;		// 航空機はメイン兵器ゼロで帰投

		// 雷撃時に適当に移動さす
		var direction=unit.Direction;

		if(Random(2)==0)
			direction+=(70-Random(40));
		else
			direction-=(70-Random(40));

		direction=direction.WholeDegrees();

		unit.EmergencyDestination = unit.Position+WorldVector.FromDirection(direction) * 300;
		unit.EmergencyFlags[0]=20+Random(300);

		// 部下、多分戦闘機に帰投命令
		if(unit.IsGroupLeader!=0)
			{
			for(var f=1;f<=MaxUnitId;f++)
				{
				ref var other = ref Units[f];
				if( other.IsUsed && other.GroupLeader==m && other.PlaneState==UnitState.Flying )
					{
					other.GroupLeader=0;
					other.Mode=UnitMode.Return;

					// 1.22 leaves their path as it is (0x413F51 to 0x413F7C).
					if( Rules.FollowersFlyToRelease )
						{
other.SetPathPoint(0, unit.Position);
other.EndPath(1);
						}

					}
				}

			unit.IsGroupLeader=0;
			}

		if( unit.IsGroupLeader==0 )
			{
			unit.SetPathPoint(0, unit.Position);
			unit.EndPath(1);
			}

		var n=FindFreeFire();
		ref var fire = ref Fires[n];
		if( n!=0 )
			{
			PlaySoundEffect(SoundId.Splash, unit.Position);
			fire.Target=target;
			fire.Kind=kind;
			fire.Position=unit.Position;
			fire.Direction=(int)unit.Direction;
			fire.Speed=AIR_TPD_SPD;
			fire.Acceleration=+0.0;
			fire.FinalSpeed=0.0;
			fire.Ticks=0;
			fire.FlightTime=240;
			fire.ArmingTime=15;
			}
		}
	}
		return false;
	return true;
	}

private bool AttackerDropBomb(ref int target, ref Unit unit, ref Angle bearing, int m, FireKind kind)
	{
	// NSPW NET's Japanese attackers dive: they release close, pull up and their bombs fall late. 1.22's attackers all
	// release from 170 and speed up, as NSPW NET's American ones do, and only the Americans' drop a second bomb (0x414186
	// to 0x4144E0).
	var dives = unit.Side==Side.Japan && Rules.JapaneseAttackersDive;
	if( !Units[target].IsFound )
		return false;
	var futurePosition=Units[target].Position+WorldVector.FromDirection(Units[target].Direction) * (Units[target].Speed*70.0);

	bearing=(futurePosition-unit.Position).Direction;
	var relativeBearing=bearing-unit.Direction;
	relativeBearing=relativeBearing.WrapNegative();

	if( (int)relativeBearing<=30||(int)relativeBearing>=330 )
		{
		// 距離を求めます
		var distance=(unit.Position-futurePosition).Length;

		if( ( distance>=170 && distance<=180 && !dives ) || ( distance>=35 && distance<=65 && dives ))
			{
			// 発射！

			if( !dives )
				unit.Speed+=unit.AccelerationChange*700;
			else
				{
				unit.EmergencyDestination = unit.Position+WorldVector.FromDirection(unit.Direction) * 300;
				unit.EmergencyFlags[0]=150+Random(50);
				}

			unit.Ammo=0;		// 消費
			unit.Target=0;

			unit.Mode=UnitMode.Return;		// 航空機はメイン兵器ゼロで帰投

			// 部下、多分戦闘機に帰投命令
			if(unit.IsGroupLeader!=0)
				{
				for(var f=1;f<=MaxUnitId;f++)
					{
					ref var other = ref Units[f];
					if( other.IsUsed && other.GroupLeader==m && other.PlaneState==UnitState.Flying)
						{
						other.GroupLeader=0;
						other.Mode=UnitMode.Return;

						// 1.22 leaves their path as it is.
						if( Rules.FollowersFlyToRelease )
							{
other.SetPathPoint(0, unit.Position);
other.EndPath(1);
							}

						}
					}
				unit.IsGroupLeader=0;
				}

			var n=FindFreeFire();
			ref var fire = ref Fires[n];
			if( n!=0 )
				{
				fire.Target=(int)UnitKind.Attacker;
				fire.Kind=kind;
				fire.Position = unit.Position+new WorldVector(3-Random(6), 3-Random(6));
				fire.Direction=bearing;

				if(dives)
					{
					fire.Position += WorldVector.FromDirection(fire.Direction) * 13;
					}
				else
					{
					fire.Position += WorldVector.FromDirection(fire.Direction) * 130;
					}

				fire.Speed=0.3;
				fire.Acceleration=+0.2;
				fire.FinalSpeed=0.0;
				if(dives)
					{
					fire.Ticks=10;
					fire.FlightTime=68+Random(5);
					}
				else
					{
					fire.Ticks=0;
					fire.FlightTime=70;
					}

				}

				// もう一発
				if( !Rules.JapaneseAttackersDive && unit.Side!=Side.UnitedStates )
					return false;
				n=FindFreeFire();
				fire = ref Fires[n];
				if( n!=0 )
					{
					fire.Target=target;
					fire.Kind=kind;
					fire.Position = unit.Position+new WorldVector(20-Random(40), 20-Random(40));
					fire.Direction=bearing;

					if(dives)
						{
						fire.Position += WorldVector.FromDirection(fire.Direction) * 13;
						}
					else
						{
						fire.Position += WorldVector.FromDirection(fire.Direction) * 130;
						}

					fire.Speed=0.3;
					fire.Acceleration=+0.2;
					fire.FinalSpeed=0.0;

					if(dives)
						{
						fire.Ticks=10;
						fire.FlightTime=75+(5-Random(10));
						}
					else
						{
						fire.Ticks=0;
						fire.FlightTime=70+(5-Random(10));
						}
					}
			}
		}
	return false;
	return true;
	}

private bool BomberDropBomb(ref int target, ref Unit unit, ref Angle bearing, FireKind kind)
	{
	var selectedTarget=0;
	if(target!=0)
		selectedTarget=target;
	for(var n=1;n<=MaxUnitId;n++)
		{	//前方の敵を探す。
		if(selectedTarget!=0)
			{ n=selectedTarget; target=0; }
		if( Units[n].IsUsed && Units[n].Category==UnitCategory.Ship && Units[n].Side!=unit.Side && Units[n].IsFound )
			{
			// 攻撃地点から攻撃目標地点への方位角
			var offset=Units[n].Position-unit.Position;
			var direction=offset.Direction;

			bearing=direction+(Random(10)-5);
			bearing=bearing.Wrapped();

			var relativeBearing=direction-unit.Direction;
			relativeBearing=relativeBearing.WrapNegative();
			if( (int)relativeBearing<=30 || (int)relativeBearing>=330 )
				{
				var distance=offset.Length;
				if( distance>=30 && distance<=60 )
					{	target=n;	break;	}
				}
			}
		if( selectedTarget!=0 )
			break;
		}
	if( target!=0 )
		{
		var f=FindFreeFire();
		ref var fire = ref Fires[f];
		if( f!=0 )
			{
			PlaySoundEffect(SoundId.BomberBombRelease, unit.Position);
			if(unit.Ammo!=0)
				unit.Ammo--;		// 消費

			if( unit.Ammo<=0)
				{
				unit.Ammo=0;		// 消費
				unit.Target=0;
				unit.Mode=UnitMode.Return;		// 航空機はメイン兵器ゼロで帰投
				}

			unit.ReloadTime=5;		// 再装填時間

			if(unit.Ammo<=0)
				unit.Mode=UnitMode.Return;		// 航空機はメイン兵器ゼロで帰投

			fire.Target=(int)UnitKind.Bomber;
			fire.Kind=kind;
			fire.Position = unit.Position+new WorldVector(-6+Random(13), -6+Random(13));
			fire.Direction=bearing;
			fire.Speed=0.3;
			fire.Acceleration=+0.2;
			fire.FinalSpeed=0.0;
			fire.Ticks=0;
			fire.FlightTime=70;
			}
		}

	return false;
	return true;
	}

private bool FirePlaneWeapons(FireKind kind, ref Unit unit, ref int target, ref Angle bearing, int m)
	{


	if( kind==FireKind.Bullet && unit.Kind==UnitKind.Bomber )
		{
		// 航空機の全方向対空機銃
		if( !BomberFireBullet(ref target, ref unit, ref bearing, kind) )
			return false;
		}

	if( kind==FireKind.Bullet && unit.Kind==UnitKind.Fighter )
		{
		// 戦闘機
		// 前方固定銃
		if( !FighterFireBullet(ref target, ref unit, kind) )
			return false;
		}

	if( kind==FireKind.Bullet )
		{
		if( !PlaneFireBullet(ref target, ref unit, ref bearing, kind) )
			return false;
		}

		if( kind==FireKind.Torpedo && !(Units[target].Kind>=UnitKind.AirBase && Units[target].Kind<=UnitKind.Fortress) )
			{
			// 攻撃機
			// トゥピード

			//地上の上なら投雷しない。

			if( !PlaneDropTorpedo(ref unit, ref target, ref bearing, m, kind) )
				return false;
		}

	if( kind==FireKind.Bomb && unit.Kind==UnitKind.Attacker )
		{
		// 攻撃機
		// 爆撃
		// 攻撃地点から攻撃目標地点への方位角
		if( !AttackerDropBomb(ref target, ref unit, ref bearing, m, kind) )
			return false;
		}

	if( kind==FireKind.Bomb && unit.Kind==UnitKind.Bomber )
		{
		// 爆撃機
		// 爆撃
		if( !BomberDropBomb(ref target, ref unit, ref bearing, kind) )
			return false;
		}
	return true;
	}

private bool FireShipWeapons(FireKind kind, ref Unit unit, int m, ref Angle bearing, ref double distance, ref int target, ref int range, ref Array3<int> shotCounts, ref Angle relativeBearing)
	{
	if( kind==FireKind.CargoNavalBase || kind==FireKind.CargoAirBase || kind==FireKind.CargoInfantryBase || kind==FireKind.CargoPillboxes || kind==FireKind.CargoFortress )
		{
		// トランスポート
		// 攻撃地点から攻撃目標地点への方位角
		if( !LandCargo(ref unit, kind, m) )
			return false;
		}

	if( kind==FireKind.RapidAntiAircraftShell )
		{
		// 自動の対空機関砲 Rapid Anti Air Shell
		if( !ShipFireRapidAntiAircraftShell(ref unit, ref bearing, ref distance, kind) )
			return false;
		}

	if( kind==FireKind.AntiSubmarineBomb && unit.Speed>=unit.MaxSpeed )
		{
		if( !ShipDropAntiSubmarineBomb(ref target, ref unit, kind) )
			return false;
		}

	if( ( kind==FireKind.Gun || kind==FireKind.NavalBaseGun ) && target==0)	// ターゲットが選択されていない砲撃、
		{
		// 艦砲、自動射撃
		if( !ShipFireGunWithoutTarget(ref unit, kind, ref range, ref shotCounts, ref bearing, ref relativeBearing) )
			return false;
		}

	// 選択でない自動の魚雷、主に駆逐艦
	if( kind==FireKind.Torpedo && target==0)
		{
		//
		if( !ShipFireTorpedoWithoutTarget(ref unit, ref bearing, kind) )
			return false;
		}

	// 選択された敵への魚雷、主に、潜水艦
	if( kind==FireKind.Torpedo && target!=0)
		{
		//

		if( !ShipFireTorpedoAtTarget(target, ref unit, ref bearing, kind) )
			return false;
		}

	if( kind==FireKind.AntiAircraftShell && target!=0 && Units[target].Category==UnitCategory.Plane )	// ターゲットが選択された対空砲
		{
		//	指定射撃
		if( !ShipFireAntiAircraftShellAtTarget(target, ref unit, ref range, ref shotCounts) )
			return false;
		}

	if( kind==FireKind.AntiAircraftShell && target==0 )
		{
		// 自動の対空砲 Anti Air Shell
		if( !ShipFireAntiAircraftShellWithoutTarget(ref unit, ref bearing, ref relativeBearing, ref distance, ref range, ref shotCounts, kind) )
			return false;
		}

	if( ( kind==FireKind.Gun || kind==FireKind.NavalBaseGun ) && target!=0 && Units[target].Category==UnitCategory.Ship )	// ターゲットが選択された砲撃
		{
		// 艦砲		指定射撃
		if( !ShipFireGunAtTarget(target, ref unit, kind, ref range, ref shotCounts) )
			return false;
		}

	if( kind==FireKind.Bullet )
		{
		// 艦船の対空機銃
		if( !ShipFireBullet(ref target, ref unit, ref bearing, kind) )
			return false;
		}
	return true;
	}

//============================================================================
// 射撃します。
//----------------------------------------------------------------------------
[Original("fire_now")]
public void	FireWeapons(int m,int target,FireKind kind)
	{
	ref var unit = ref Units[m];
	int range=default /* C4701 */; Array3<int> shotCounts = default;
	Angle bearing=default /* C4701 */, relativeBearing=default /* C4701 */;
	double distance=default /* C4701 */;

	if( (unit.Kind==UnitKind.NavalBase || unit.Kind==UnitKind.AirBase || unit.Kind==UnitKind.InfantryBase || unit.Kind==UnitKind.Pillboxes || unit.Kind==UnitKind.Fortress) && unit.BuildTime!=0	)
		{
		//工事中
		return;
		}

	if( unit.Category==UnitCategory.Ship )
		{
		//=========		 艦船の射撃制御		=========//
		if( !FireShipWeapons(kind, ref unit, m, ref bearing, ref distance, ref target, ref range, ref shotCounts, ref relativeBearing) )
			return;
		}

	if( unit.Category==UnitCategory.Plane )
		{
				//=========		 航空機の射撃制御		=========//
		if( !FirePlaneWeapons(kind, ref unit, ref target, ref bearing, m) )
			return;

		}
	}

//============================================================================
// 編隊のポジションをＰｐ＿ｘｙ「０」にセットします。
//----------------------------------------------------------------------------
[Original("set_pos_of_dynmc")]
public void	SetDynamicDestination(int n)
	{
	Angle			angle=default /* C4701 */;
	double			distance=default /* C4701 */;

	if( Units[n].Category==UnitCategory.Plane )
		{

		// 航空機編隊の制御

		int leader=Units[n].GroupLeader;

		// 1.22 keeps a flying plane with its parked leader (0x4149F0).
		if(Units[leader].PlaneState==UnitState.Parked && Units[leader].Mode==UnitMode.Return && Units[n].PlaneState==UnitState.Flying && Rules.FlyingFollowersLeaveParkedLeaders )
			{
			Units[n].GroupLeader=0;
			return;
			}


		if(Units[leader].Category==UnitCategory.Ship)	// こっちは飛行機だが指揮が艦船の場合
			{
			angle=(double)Random(359);
			distance=(double)Random(500+150);
			Units[n].Mode=UnitMode.Move;

			// 目的地を決定
			Units[n].SetPathPoint(0, Units[leader].Position+WorldVector.FromDirection(angle) * distance);
			Units[n].EndPath(1);
			}
		else
			{
			// 指揮が通常移動
			var planesPerFlight=5;			// １小隊何機か

			// 何番編隊か
			var flight=Units[n].FormationNumber/planesPerFlight;
			// 何番機か
			var place=Units[n].FormationNumber%planesPerFlight;

			switch( place )
				{
				case 0:		// １番機
					angle=0;	distance=0;
					break;
				case 1:		// 2番機
					angle=Units[leader].Direction-90.0-45.0;	distance=60*0.80;
					break;
				case 2:		//
					angle=Units[leader].Direction-90.0-45.0-90.0;	distance=60*0.80;
					break;
				case 3:		//
					angle=Units[leader].Direction-90.0-45.0;	distance=120*0.80;
					break;
				case 4:		//
					angle=Units[leader].Direction-90.0-45.0-90.0;	distance=120*0.80;
					break;
				case 5:		//
					angle=Units[leader].Direction-90.0-45.0-45.0;	distance=100*0.80;
					break;
				}

			// 目的地を決定
			Units[n].SetPathPoint(0, Units[leader].Position+WorldVector.FromDirection(angle) * distance);

			if( flight>=0 )
				{
				// 各編隊随伴指揮機位置
				switch( flight )
					{
					case 0:		// 1番編隊
						angle=Units[leader].Direction;	distance=35;
						break;
					case 1:		// 2番編隊
						angle=Units[leader].Direction-90.0-45.0;	distance=180*0.80;
						break;
					case 2:		//
						angle=Units[leader].Direction-90.0-45.0-90.0;	distance=180*0.80;
						break;
					case 3:		//
						angle=Units[leader].Direction-90.0-45.0;	distance=360*0.80;
						break;
					case 4:		//
						angle=Units[leader].Direction-90.0-45.0-45.0;	distance=300*0.80;
						break;
					case 5:		//
						angle=Units[leader].Direction-90.0-45.0-90.0;	distance=360*0.80;
						break;

					case 6:		//
						angle=Units[leader].Direction-90.0-45.0;	distance=540*0.80;
						break;
					case 7:		//
						angle=Units[leader].Direction-90.0-45.0-22.5;	distance=480*0.80;
						break;
					case 8:		//
						angle=Units[leader].Direction-90.0-45.0-45.0-22.5;	distance=480*0.80;
						break;
					case 9:		//
						angle=Units[leader].Direction-90.0-45.0-90.0;		distance=540*0.80;
						break;
					case 10:		//
						angle=Units[leader].Direction-90.0-45.0-11.2;		distance=640*0.80;
						break;
					case 11:		//
						angle=Units[leader].Direction-90.0-45.0-90.0+11.2;		distance=640*0.80;
						break;
					}

				// 目的地を決定
				Units[n].SetPathPoint(0, Units[n].GetPathPoint(0)+WorldVector.FromDirection(angle) * distance);
				Units[n].EndPath(1);
				}
			}
		}
	else
		{
		// 艦隊制御
		int leader=Units[n].GroupLeader;
		angle=Units[n].DirectionToLeader+Units[leader].Direction;
		angle=angle.WrapFullTurn();
		distance=Units[n].DistanceToLeader;

		Units[n].SetPathPoint(0, Units[leader].Position+WorldVector.FromDirection(angle) * distance);
		Units[n].EndPath(1);

		if( 1!=0  )
			{
			//コンピュータの進路計算
			if( Rules.FollowersRouteBy122sOwnCode )
				SetCpuRoute2As122( n );
			else
				SetCpuRoute2( n );
			}
		}

	Units[n].IsStopping=false;

	}

//============================================================================
// 戦闘機の緊急起動をセット
//----------------------------------------------------------------------------
[Original("chk_another_unit")]
public void	CheckOtherUnits(double* x,double* y)
	{

	for( var n=1; n<=MaxUnitId; n++)
		{
		if( Units[n].IsUsed && Units[n].Category==UnitCategory.Ship )
			{
			// ptin dbg
			var shipArea=WorldRect.Around(Units[n].Position, (Sprites[SpriteId.JapanUnits].Size.Width/2), (Sprites[SpriteId.JapanUnits].Size.Height/2));

			if( shipArea.Contains(new WorldPosition(*x, *y)))
				{
				n=0;
				*y-=80;
				}
			}
		}
	}

//============================================================================
// 戦闘機の緊急起動をセット
//----------------------------------------------------------------------------
[Original("set_pos_of_emrgncy_FT")]
public void	SetFighterEmergencyDestination(int m)
	{
	ref var unit = ref Units[m];

	// 目標も、爆弾も無く、損傷がひどいかガソリンが切れそうな場合はきとうしよう
	if( unit.Kind==UnitKind.Fighter && unit.Mode!=UnitMode.Return && (unit.Fuel<=30 || unit.Hp<=unit.MaxHp*0.70 || unit.Ammo<=0 ) )
		{
		// 発射！
		unit.Target=0;		// ターゲットをクリア
		unit.Mode=UnitMode.Return;		// 航空機はメイン兵器ゼロで帰投
		unit.GroupLeader=0;

		if( unit.IsGroupLeader==0 )
			{
			unit.SetPathPoint(0, unit.Position);
			unit.EndPath(1);
			}
		}
	}

//============================================================================
// 戦闘機の攻撃機動をセット
//----------------------------------------------------------------------------
[Original("set_pos_of_attack_FT")]
public void	SetFighterAttackDestination(int m)
	{
	ref var unit = ref Units[m];
	int ownTechnique=default /* C4701 */;

	// ptin dbg
	var destinationArea=WorldRect.Around(unit.EmergencyDestination, 35, 35);

	if( destinationArea.Contains(unit.Position) )
		{
		unit.EmergencyDestination = unit.Position+new WorldVector(unit.Direction.Cos()*(100+Random(50)), unit.Direction.Sin()*(100+Random(50)));
		unit.EmergencyFlags[0]=100;
		unit.IsStopping=false;
		return;
		}

	var target=unit.Target;
	var ownSkill=unit.Skill;
	var enemySkill=Units[target].Skill;

	// 1.22 tells two pilots of the same skill apart by the frame (0x4150A9), NSPW NET by the tick.
	if( ownSkill == enemySkill )
		{
		if( ((Rules.FighterTiesBreakByFrame ? FrameCount : Tick)%600)<300 )
			{ownSkill++;}
		else
			{enemySkill++;}
		}
	if( ownSkill > enemySkill )
		{	ownTechnique=10;	}
	if( ownSkill < enemySkill )
		{	ownTechnique=40+(enemySkill-ownSkill);	}

	if( Random(ownTechnique)!=0 )
		return;

	// 正面打ち合いをさけるようにします。
	ref var enemy = ref Units[target];
	// 自機ｍと敵機ｎの絶対角を調べます。
	var bearing=(unit.Position-enemy.Position).Direction;					// drctnが絶対角

	bearing-=enemy.Direction;				// 敵機ｎからの方位角をしらべます。
	bearing=bearing.WrapNegative();
	if( (int)bearing<=5 || (int)bearing>=355 )
		{
		// 敵機が正面に自機を捕らえています。
		bearing=(enemy.Position-unit.Position).Direction;					// drctnが絶対角

		bearing-=unit.Direction;				// 自機ｍからの方位角をしらべます。
		bearing=bearing.WrapNegative();

		if( (int)bearing<=5 || (int)bearing>=355 )
			{
			// 自機も敵機を正面に捕らえています
			var distance=(enemy.Position-unit.Position).Length;
			if( distance>=160 && distance<=320 )
				{
				// さらに、距離が近い よけよう
				var evasion=unit.Direction;
				switch( Random(2) )
					{
					case 0:
						evasion+=45+Random(45);
						break;
					case 1:
						evasion-=45+Random(45);
						break;
					}
				evasion=ReduceHeading(evasion);

				unit.EmergencyDestination = unit.Position+WorldVector.FromDirection(evasion) * 300;
				unit.EmergencyFlags[0]=70+Random(40);

				return;
				}
			}
		}

	// 敵機の直前にＥｍ＿Ｘｙを設定します。
	unit.EmergencyDestination = Units[target].Position+WorldVector.FromDirection(Units[target].Direction) * (Units[target].Speed*20.0);
	unit.EmergencyFlags[0]=70+Random(40);
	unit.IsStopping=false;
	if(Random(10)==0)
		PlaySoundEffect(SoundId.Plane1+Random(2), unit.Position);

	}

//============================================================================
// 攻撃機の緊急起動をセット
//----------------------------------------------------------------------------
[Original("set_pos_of_emrgncy_AT")]
public void	SetAttackerEmergencyDestination(int m)
	{
	ref var unit = ref Units[m];

	// 戦闘機から逃げよう
	for(var n=1;n<=MaxUnitId;n++)
		{
		ref var other = ref Units[n];	//後方の敵を探す。
		if( other.IsUsed && other.Category==UnitCategory.Plane && other.PlaneState==UnitState.Flying
		&& other.Side!=unit.Side && other.IsFound )
			{

			// 攻撃地点から攻撃目標地点への方位角
			var relativeBearing=(other.Position-unit.Position).Direction;

			relativeBearing-=unit.Direction;
			relativeBearing=relativeBearing.WrapNegative();

			// 攻撃目標地点から攻撃地点への方位角
			var otherRelativeBearing=(unit.Position-other.Position).Direction;

			otherRelativeBearing-=other.Direction;
			otherRelativeBearing=otherRelativeBearing.WrapNegative();

			if( ((int)relativeBearing>=150 && (int)relativeBearing<=210 && other.Kind==UnitKind.Fighter)
				||
				( ((int)relativeBearing<=45 || (int)relativeBearing>=315) && ( (int)otherRelativeBearing>=135 && (int)otherRelativeBearing<=225)  && other.Kind==UnitKind.Attacker && unit.Target==0 )
			  )
				{

				var distance=(other.Position-unit.Position).Length;
				if( distance<=250+((other.Kind==UnitKind.Attacker ? 1 : 0)*70) )
					{

					var evasion=unit.Direction;
					switch(Random(2))
						{
						case 0:
							evasion+=45+Random(90);
							break;
						case 1:
							evasion-=45+Random(90);
							break;
						}
					evasion=evasion.WholeDegrees();

					unit.EmergencyDestination = unit.Position+WorldVector.FromDirection(evasion) * 300;
					unit.EmergencyFlags[0]=100;

					return;
					}
				}
			}
		}

	// 損傷がひどいかガソリンが切れそうな場合はきとうしよう
	if( unit.Mode!=UnitMode.Return && (unit.Fuel<=20 || unit.Hp<=unit.MaxHp*0.70 )
		)
		{
		unit.Ammo=0;		// 魚雷が０
		unit.Target=0;		// ターゲットをクリア

		if(Units[unit.Carrier].IsUsed)
			{
		unit.Mode=UnitMode.Return;		// 航空機はメイン兵器ゼロで帰投

		unit.GroupLeader=0;

		if( unit.IsGroupLeader==0 )
			{
			unit.SetPathPoint(0, unit.Position);
			unit.EndPath(1);
			}
		else
			{

			var lowestFormationNumber=MaxUnitId+1;
			var groupSize=1;
			var newLeader=0;

			for(var f=1;f<=MaxUnitId;f++)
				{
				ref var other = ref Units[f];
				if( other.IsUsed && other.GroupLeader==m )
					{
					groupSize++;
					if( other.FormationNumber < lowestFormationNumber )
						{
						lowestFormationNumber=other.FormationNumber;
						newLeader=f;					// これが新しい隊長番号
						}
					}
				}

			if(newLeader!=0 && groupSize>=2 )
				{
				Units[newLeader].IsGroupLeader=(short)groupSize;
				Units[newLeader].GroupLeader=0;
				Units[newLeader].FormationNumber=0;

				// 昔の小隊長が攻爆撃機だったら、帰投にしておく
				// 1.22 tests whether the old leader is flying, for the new one and the followers alike (0x415916, 0x415971).
				if( Units[newLeader].Kind==UnitKind.Fighter && (unit.Kind==UnitKind.Attacker || unit.Kind==UnitKind.Bomber)  && (Rules.HandoverTestsTheOldLeaderFlying ? unit : Units[newLeader]).PlaneState==UnitState.Flying )
					{
					Units[newLeader].Mode=UnitMode.Return;		// それまでの隊長がボスだったらきかんしよっと
					}

				for(var f=1;f<=MaxUnitId;f++)
					{
					ref var other = ref Units[f];
					if( other.IsUsed && other.GroupLeader==m )
						{
						other.GroupLeader=(short)newLeader;

						// 昔の小隊長が攻爆撃機だったら、帰投にしておく
						if( Units[newLeader].Kind==UnitKind.Fighter && other.Kind==UnitKind.Fighter && (unit.Kind==UnitKind.Attacker || unit.Kind==UnitKind.Bomber) && (Rules.HandoverTestsTheOldLeaderFlying ? unit : other).PlaneState==UnitState.Flying )
							other.Mode=UnitMode.Return;		// それまでの隊長がボスだったらきかんしよっと
						}

					}

				for(var f=0;f<64;f++)
					{
					Units[newLeader].SetPathPoint(f, unit.GetPathPoint(f));
					}

				// 昔の小隊長
				unit.SetPathPoint(0, unit.Position);
				unit.EndPath(1);

				}
			}
			}
		}

	}

//============================================================================
// 攻撃機の攻撃機動をＰｐ＿ｘｙにセット
//----------------------------------------------------------------------------
[Original("set_pos_of_attack_AT")]
public void	SetAttackerAttackDestination(int m)
	{
	ref var unit = ref Units[m];

	var target=unit.Target;				// 攻撃目標

	// 現位置から攻撃目標地点への距離
	var distance=(Units[target].Position-unit.Position).Length;

	// 目標ユニットへの方位角を求めます
	var relativeBearing=(Units[target].Position-unit.Position).Direction;
	relativeBearing-=unit.Direction;
	relativeBearing=relativeBearing.WrapNegative();

	// 1.22's leader does not send its attackers off at 500 to 510 from the target: it has no such case (0x415A10).
	if( Rules.AttackersBreakFormation && distance<=510 && distance >= 500  && (relativeBearing<=22.5||relativeBearing>=337.5)   )
		{

		// リーダー機か単独機のみここに来ます。

		var followers=0;
		for(var i=1;i<=MaxUnitId;i++)
			{
			ref var other = ref Units[i];
			if( i!=m && other.IsUsed && other.GroupLeader==m && other.Kind==UnitKind.Attacker )
				{
				followers++;

				if( other.Kind!=UnitKind.Fighter )
					other.GroupLeader=0;

				var heading=other.Direction;

				switch( other.FormationNumber%5 )
					{
					case 1:
						heading+=315+22.5;
						break;
					case 2:
						heading+=22.5;
						break;
					case 3:
						heading+=315;
						break;
					case 4:
						heading+=45;
						break;
					}
				heading=heading.WholeDegrees();

				other.EmergencyDestination = other.Position+WorldVector.FromDirection(heading) * 600;
				other.EmergencyFlags[0]=10+((1+(other.FormationNumber%5))*20);

				other.SetPathPoint(0, Units[target].Position);
				other.EndPath(1);

				}
			}

		}
	else if( distance >= 400 )
		{
		if( unit.IsPathEnd(1) )
			{
			// ptin dbg
			var pathPointArea=WorldRect.Around(unit.GetPathPoint(0), 35, 35);
			if( !pathPointArea.Contains(unit.Position) || !Units[target].IsFound)
				{
				return;
				}
			var aim=Units[target].Position+WorldVector.FromDirection(Units[target].Direction); // とりあえずターン後
			unit.SetPathPoint(0, aim);
			unit.EndPath(1);
			unit.IsStopping=false;
			}
		}
	else if ( distance >= 40 && (relativeBearing<=45||relativeBearing>=315))
		{
		if( unit.IsPathEnd(1) )
			{
			double ticks;
			if(unit.Weapon==FireKind.Torpedo)
				ticks=( (AIR_TPD_LOS_DSTC) /AIR_TPD_SPD);		// 投雷距離　÷ 魚雷速度　でターンを求めます
			else
				ticks=70.0;
			var aim=Units[target].Position+WorldVector.FromDirection(Units[target].Direction) * (Units[target].Speed*ticks); // とりあえずターン後
			unit.SetPathPoint(0, aim);
			unit.EndPath(1);
			unit.IsStopping=false;
			if( unit.IsGroupLeader!=0  )
				{
				var escorts=0;
				for(var f=1;f<=MaxUnitId;f++)
					{
 					ref var other = ref Units[f];
 					if( other.IsUsed && other.Kind==UnitKind.Fighter && other.GroupLeader==m)
						{	escorts++;	}
					else
						{
						if( other.IsUsed && other.GroupLeader==m )
							other.GroupLeader=0;
						}
					}
				unit.IsGroupLeader=(short)escorts;
				}
			}
		}
	else
		{	// 近すぎる場合は離脱
		// ptin dbg
		var pathPointArea=WorldRect.Around(unit.GetPathPoint(0), 35, 35);
		if( pathPointArea.Contains(unit.Position) )
			{
			unit.SetPathPoint(0, unit.Position+WorldVector.FromDirection(unit.Direction) * 300);
			unit.EndPath(1);
			unit.IsStopping=false;
			}
		}
	}

//============================================================================
// 輸送船の上陸機動をＥｍ＿ｘｙにセット
//----------------------------------------------------------------------------
[Original("set_pos_of_attack_TR1")]
public void	SetTransportLandingDestination(int m)
	{

	if( Random(200)!=0 )
		return;

	// 現位置から攻撃目標地点への距離
	var distance=(Units[m].LandingPoint-Units[m].Position).Length;

	if(distance<=400)
		{
		Units[m].EmergencyDestination = Units[m].LandingPoint;
		Units[m].EmergencyFlags[0]=200;
		}
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("em_of_out_of_map")]
public void ReturnIntoWorld(int m)
	{
	ref var unit = ref Units[m];
	// 展開海域より外れたなら戻る
	if( 1!=0  )
		{
		if( unit.Position.X>World.Right )
			{
			unit.EmergencyDestination = new WorldPosition(World.Right-40, unit.Position.Y-100+Random(200));
			unit.EmergencyFlags[0]=50+Random(200);
			return;
			}

		if( unit.Position.X<World.Left )
			{
			unit.EmergencyDestination = new WorldPosition(World.Left+40, unit.Position.Y-100+Random(200));
			unit.EmergencyFlags[0]=50+Random(200);
			return;
			}

		if( unit.Position.Y>World.Top )
			{
			unit.EmergencyDestination = new WorldPosition(unit.Position.X-100+Random(200), World.Top-40);
			unit.EmergencyFlags[0]=50+Random(200);
			return;
			}

		if( unit.Position.Y<World.Bottom )
			{
			unit.EmergencyDestination = new WorldPosition(unit.Position.X-100+Random(200), World.Bottom+40);
			unit.EmergencyFlags[0]=50+Random(200);
			return;
			}
		}
	}

// 1.22's hunt of the computer's destroyers, stopped or not (0x416706 to 0x416B35). One that chases no submarine
// (em_flg[1]) takes the first enemy one found within 600 of its contact, or of itself when surfaced: it runs toward the
// contact, give or take 20°, 300 past it, for 600 ticks, and chases it. One that chases a submarine stops chasing it once
// it is found again (0x416A70), as finding it stops its chasers (FoundSubmarinesStopChasers); while it is not, the
// destroyer stops 1 time in 10, or else goes to within 400 of its last contact for 200 to 299 ticks.
private bool HuntSubmarineAs122(ref Unit unit)
	{
	if( unit.EmergencyFlags[1]!=0 )
		{
		ref var chased = ref Units[unit.EmergencyFlags[1]];
		if( chased.IsFound )
			{
			unit.EmergencyFlags[1]=0;
			return true;
			}

		if( Random(10)==0 )
			{
			unit.EmergencyFlags[1]=0;
			return false;
			}

		unit.EmergencyDestination = new WorldPosition(chased.ContactX-Random(800)+400, chased.ContactY-Random(800)+400);
		unit.EmergencyFlags[0]=200+Random(100);
		return false;
		}

	for(var n=1; n<=MaxUnitId; n++)
		{
		ref var other = ref Units[n];
		if( other.IsUsed && other.Kind==UnitKind.Submarine && other.IsFound && other.Side!=unit.Side )
			{
			var distance=((other.IsSubmerged ? other.ContactPosition : other.Position)-unit.Position).Length;

			if( distance<=600 )
				{
				var toContact=(other.ContactPosition-unit.Position).NonZero();
				var heading=new Angle(atan2(toContact.Y,toContact.X)*RadiansToDegrees+(20-Random(40))).WholeDegrees();

				unit.EmergencyDestination = unit.Position + WorldVector.FromDirection(heading) * (distance+300);
				unit.EmergencyFlags[0]=600;
				unit.EmergencyFlags[1]=n;
				return false;
				}
			}
		}

	return true;
	}

private bool HuntSubmarine(ref Unit unit)
	{
	if(unit.EmergencyFlags[0]==0)
		{
		for(var n=1; n<=MaxUnitId; n++)
			{
			ref var other = ref Units[n];
			if( other.IsUsed && other.Kind==UnitKind.Submarine && other.IsFound && other.Side!=unit.Side )
				{
				// 自点と対象点の距離: 潜水中なら推定位置、浮上してますなら位置
				var distance=((other.IsSubmerged ? other.ContactPosition : other.Position)-unit.Position).Length;		// 距離

				if( distance<=( unit.Variant==1 ? 500 : 400 ) )
					{
					// 近くに潜水艦推定位置

					var toContact=(other.ContactPosition-unit.Position).NonZero();
					Angle direction=atan2(toContact.Y,toContact.X)*RadiansToDegrees;

					direction+=20-Random(40);

					var heading=direction.WholeDegrees();

					unit.EmergencyDestination = unit.Position+WorldVector.FromDirection(heading) * (distance+200);
					unit.EmergencyFlags[0]=100+Random(250);

					return false;
					}
				}
			}
		}
	return true;
	}

private bool EvadePlane(ref Unit other, ref Unit unit)
	{
	// 自点と対象点の距離
	var distance=(other.Position-unit.Position).Length;		// 距離

	if( distance<=600-((other.Kind==UnitKind.Bomber ? 1 : 0)*300) && distance>=40 )
		{
		// 対象ユニットからの自点への方位角
		var relativeBearing=(unit.Position-other.Position).NonZero().Direction;
		relativeBearing-=other.Direction;
		relativeBearing=relativeBearing.WrapNegative();
		if( relativeBearing<=45 || relativeBearing>=315 )
			{

			var evasion=unit.Direction;
			switch(Random(2))
				{
				case 0:
					evasion+=45+Random(45+20);
					break;
				case 1:
					evasion-=45+Random(45+20);
					break;
				}
			evasion=ReduceHeading(evasion);

			unit.EmergencyDestination = unit.Position+WorldVector.FromDirection(evasion) * 300;
			unit.EmergencyFlags[0]=200+Random(250);

			return false;
			}
		}
	return true;
	}

private void EvadeShip(ref Unit other, ref Unit unit)
	{
	// 自点と対象点の距離
	var distance=(other.Position-unit.Position).Length;		// 距離

	// 見る側

	// 1.22 flees within 900, its battleship's sight, where NSPW NET's is 800 (0x416277).
	if( distance<=(double)Rules.ShipEvasionRange )
		{
		var direction=(other.Position-unit.Position).NonZero().Direction;

		if( (unit.Kind==UnitKind.Carrier||unit.Kind==UnitKind.LightCarrier) && unit.GroupLeader==0 && other.IsStopping && Random(3)!=0 )
			{
			if(unit.Random10[0]<=4)
				{
				direction+=(90+Random(40));
				}
			else
				{
				direction-=(90+Random(40));
				}
			}
		else
			{
			direction+=160+Random(40);
			}

		var heading=direction.WholeDegrees();

		unit.EmergencyDestination = unit.Position+WorldVector.FromDirection(heading) * 300;
		unit.EmergencyFlags[0]=100;
		}
	}

private bool EvadeTorpedo(ref Fire fire, ref Unit unit)
	{
	// 自点と対象点の距離
	var distance=(fire.Position-unit.Position).Length;		// 距離

	if( distance<=600 && distance>=40 )
		{
		// 対象ユニットからの自点への方位角
		var relativeBearing=(unit.Position-fire.Position).NonZero().Direction;
		relativeBearing-=fire.Direction;
		relativeBearing=relativeBearing.WrapNegative();
		if( relativeBearing<=20 || relativeBearing>=340 )
			{

			var evasion=unit.Direction;

			switch(Random(2))
				{
				case 0:
					evasion+=60+Random(30);
					break;
				case 1:
					evasion-=60+Random(30);
					break;
				}

			evasion=ReduceHeading(evasion);

			unit.EmergencyDestination = unit.Position+WorldVector.FromDirection(evasion) * 300;
			unit.EmergencyFlags[0]=100+Random(150);

			return false;
			}
		}
	return true;
	}

//============================================================================
// 艦船の緊急機動をＥｍ＿ｘｙにセット
//----------------------------------------------------------------------------
[Original("set_pos_of_emrgncy_SHIP")]
public void	SetShipEmergencyDestination(int m)
	{
	ref var unit = ref Units[m];

	for(var n=1;n<FIRE_MAX;n++)
		{
		ref var fire = ref Fires[n];
		// 艦船によってくる魚雷から逃げる
		if( fire.Target!=0 && fire.Kind==FireKind.Torpedo && fire.Ticks>=fire.ArmingTime  )
			{
			if( !EvadeTorpedo(ref fire, ref unit) )
				return;
			}
		}

	for(var n=1;n<=MaxUnitId;n++)
		{
		ref var other = ref Units[n];
		// 艦船によってくる艦船からにげる
		// NSPW NET's ships flee no base, city, mine or ground unit (kinds 11 to 17); 1.22's flee all of them but air bases
		// and naval bases (0x41616C).
		if( (unit.IsStopping || (unit.Kind==UnitKind.Carrier||unit.Kind==UnitKind.LightCarrier)) && other.IsUsed && (other.Category==UnitCategory.Ship  && (Rules.ShipsFleeGroundUnits ? other.Kind!=UnitKind.AirBase && other.Kind!=UnitKind.NavalBase : !(other.Kind>=UnitKind.AirBase && other.Kind<=UnitKind.Fortress)) ) &&
		other.Kind!=UnitKind.Submarine && other.Side!=unit.Side && other.IsFound && other.SupplyTime<=0 && unit.Target==0)
			{
			EvadeShip(ref other, ref unit);
			}

		// 艦船によってくる攻撃機から逃げる
		// NSPW NET's transports also flee fighters; 1.22's ships, attackers and bombers only (0x41650D).
		if( other.IsUsed && (other.Kind==UnitKind.Attacker || other.Kind==UnitKind.Bomber || ( other.Kind==UnitKind.Fighter && unit.Kind==UnitKind.Transport && Rules.FightersAttackTransports ) ) && other.PlaneState==UnitState.Flying && other.Side!=unit.Side && other.IsFound
			)
			{
			if( !EvadePlane(ref other, ref unit) )
				return;
			}
		}

	if( Rules.OnlyComputersDestroyersHunt )
		{
		if( unit.Kind==UnitKind.Destroyer && unit.Side==CpuSide && !HuntSubmarineAs122(ref unit) )
			return;
		}
	else if( unit.Kind==UnitKind.Destroyer  && unit.IsStopping.Value==1   )
		{
		// 駆逐艦の対潜水艦行動、発見された後！
		if( !HuntSubmarine(ref unit) )
			return;

		}

	// 1.22 brings only the player's ships back into the map (0x416B35): the computer's leave it, to its naval bases
	// outside, such as scenario 304's.
	if( Rules.ComputerShipsReturnIntoMap || unit.Side!=CpuSide )
		ReturnIntoWorld(m);

	}
}
