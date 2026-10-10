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
	int		n,h,j=default /* C4701 */,j2=default /* C4701 */,f,i;
	RECT	wrk_rect;

	h=0;

	n=Fires[m].Target;						// ターゲットナンバー
	switch( Units[n].Kind )
		{
		case UnitKind.Battleship:	j=16;j2=j/2;	break;
		case UnitKind.Cruiser:	j=12;j2=j/2;	break;
		case UnitKind.Destroyer:	j=10;j2=j/2;		break;
		case UnitKind.Submarine:
			if( Units[n].IsSubmerged )// 潜航中、あたりがでかくなる
				{
				// ptin dbg
				wrk_rect.top=(int)Units[n].Position.Y+50;
				wrk_rect.right=(int)Units[n].Position.X+50;
				wrk_rect.bottom=(int)Units[n].Position.Y-50;
				wrk_rect.left=(int)Units[n].Position.X-50;

				if( PointInRect3(ref wrk_rect,(int)Fires[m].Position.X,(int)Fires[m].Position.Y)!=0 )
					h=1;
				return	(h);
				}
			else
				{	j=8;j2=j/2;		}

				break;
		case UnitKind.Carrier:	j=14;j2=j/2;	break;
		case UnitKind.LightCarrier:	j=12;j2=j/2;	break;
		case UnitKind.AirBase:
		case UnitKind.NavalBase:
		case UnitKind.Fortress:	case UnitKind.Pillboxes:	case	UnitKind.InfantryBase:
		case UnitKind.City:	case UnitKind.Mine:
				// ptin dbg
				wrk_rect.top=(int)Units[n].Position.Y+30;
				wrk_rect.right=(int)Units[n].Position.X+30;
				wrk_rect.bottom=(int)Units[n].Position.Y-30;
				wrk_rect.left=(int)Units[n].Position.X-30;

				if( PointInRect3(ref wrk_rect,(int)Fires[m].Position.X,(int)Fires[m].Position.Y)!=0 )
					h=1;

				return	(h);
				break;
		case UnitKind.Transport:	j=12;j2=j/2;	break;
		}
	h=0;
	f=ToEightDirections((int)(Units[n].Direction));
	switch( f )
		{
		case 3: case 7:
			for( i=0; i<=4 && h==0 ; i++)
				{
				// ptin dbg
				wrk_rect.top=(int)Units[n].Position.Y+(-j+(i*j2))+j2;
				wrk_rect.right=(int)Units[n].Position.X+(+j-(i*j2))+j2;
				wrk_rect.bottom=(int)Units[n].Position.Y+(-j+(i*j2))-j2;
				wrk_rect.left=(int)Units[n].Position.X+(+j-(i*j2))-j2;

				if( PointInRect3(ref wrk_rect,(int)Fires[m].Position.X,(int)Fires[m].Position.Y)!=0 )
					h=1;
				}
			break;
		case 1: case 5:
			for( i=0; i<=4 && h==0 ; i++)
				{
				// pt in dbg
				wrk_rect.top=(int)Units[n].Position.Y+(-j+(i*j2))+j2;
				wrk_rect.right=(int)Units[n].Position.X+(-j+(i*j2))+j2;
				wrk_rect.bottom=(int)Units[n].Position.Y+(-j+(i*j2))-j2;
				wrk_rect.left=(int)Units[n].Position.X+(-j+(i*j2))-j2;

				if( PointInRect3(ref wrk_rect,(int)Fires[m].Position.X,(int)Fires[m].Position.Y)!=0 )
					h=1;
				}

			break;
		case 0: case 4:
			for( i=0; i<=2  && h==0 ; i++)
				{
				// ptin dbg
				wrk_rect.top=(int)Units[n].Position.Y+(-j+(i*j))+j2;
				wrk_rect.right=(int)Units[n].Position.X+j2;
				wrk_rect.bottom=(int)Units[n].Position.Y+(-j+(i*j))-j2;
				wrk_rect.left=(int)Units[n].Position.X-j2;

				if( PointInRect3(ref wrk_rect,(int)Fires[m].Position.X,(int)Fires[m].Position.Y)!=0 )
					h=1;
				}
			break;
		case 2: case 6:
			for( i=0; i<=2  && h==0 ; i++)
				{
				// ptin dbg
				wrk_rect.top=(int)Units[n].Position.Y+j2;
				wrk_rect.right=(int)Units[n].Position.X+(-j+(i*j))+j2;
				wrk_rect.bottom=(int)Units[n].Position.Y-j2;
				wrk_rect.left=(int)Units[n].Position.X+(-j+(i*j))-j2;

				if( PointInRect3(ref wrk_rect,(int)Fires[m].Position.X,(int)Fires[m].Position.Y)!=0 )
					h=1;
				}
			break;
		}

	return	(h);
	}

//============================================================================
// 当たりチェック
//----------------------------------------------------------------------------
[Original("draw_hit_area")]
public void		DrawHitArea(int m)
	{
	int		n,h,j=default /* C4701 */,j2=default /* C4701 */,f,i;
	RECT	wrk_rect;

	n=m;						// ターゲットナンバー
	switch( Units[n].Kind )
		{
		case UnitKind.Battleship:	j=16;j2=j/2;	break;
		case UnitKind.Cruiser:	j=12;j2=j/2;	break;
		case UnitKind.Destroyer:	j=6;j2=j/2;	break;
		case UnitKind.Submarine:
			if( Units[n].IsSubmerged )// 潜航中、あたりがでかくなる
				{
				wrk_rect.top=(int)Units[n].Position.Y-50;
				wrk_rect.right=(int)Units[n].Position.X+50;
				wrk_rect.bottom=(int)Units[n].Position.Y+50;
				wrk_rect.left=(int)Units[n].Position.X-50;

				DrawLine4((int)(wrk_rect.left-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.top),(int)(wrk_rect.right-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				DrawLine4((int)(wrk_rect.right-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.top),(int)(wrk_rect.right-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				DrawLine4((int)(wrk_rect.right-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.bottom),(int)(wrk_rect.left-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				DrawLine4((int)(wrk_rect.left-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.bottom),(int)(wrk_rect.left-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));

				return;
				}
			else
				{	j=4;j2=j/2;		}

				break;
		case UnitKind.Carrier:	j=14;j2=j/2;	break;
		case UnitKind.LightCarrier:	j=12;j2=j/2;	break;
		}
	f=ToEightDirections((int)(Units[n].Direction));
	switch( f )
		{
		case 3: case 7:
			for( i=0; i<=4  ; i++)
				{
				wrk_rect.top=(int)Units[n].Position.Y+(-j+(i*j2))-j2;
				wrk_rect.right=(int)Units[n].Position.X+(+j-(i*j2))+j2;
				wrk_rect.bottom=(int)Units[n].Position.Y+(-j+(i*j2))+j2;
				wrk_rect.left=(int)Units[n].Position.X+(+j-(i*j2))-j2;

				DrawLine4((int)(wrk_rect.left-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.top),(int)(wrk_rect.right-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				DrawLine4((int)(wrk_rect.right-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.top),(int)(wrk_rect.right-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				DrawLine4((int)(wrk_rect.right-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.bottom),(int)(wrk_rect.left-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				DrawLine4((int)(wrk_rect.left-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.bottom),(int)(wrk_rect.left-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				}
			break;
		case 1: case 5:
			for( i=0; i<=4  ; i++)
				{
				wrk_rect.top=(int)Units[n].Position.Y+(-j+(i*j2))-j2;
				wrk_rect.right=(int)Units[n].Position.X+(-j+(i*j2))+j2;
				wrk_rect.bottom=(int)Units[n].Position.Y+(-j+(i*j2))+j2;
				wrk_rect.left=(int)Units[n].Position.X+(-j+(i*j2))-j2;

				DrawLine4((int)(wrk_rect.left-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.top),(int)(wrk_rect.right-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				DrawLine4((int)(wrk_rect.right-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.top),(int)(wrk_rect.right-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				DrawLine4((int)(wrk_rect.right-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.bottom),(int)(wrk_rect.left-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				DrawLine4((int)(wrk_rect.left-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.bottom),(int)(wrk_rect.left-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				}

			break;
		case 0: case 4:
			for( i=0; i<=2   ; i++)
				{
				wrk_rect.top=(int)Units[n].Position.Y+(-j+(i*j))-j2;
				wrk_rect.right=(int)Units[n].Position.X+j2;
				wrk_rect.bottom=(int)Units[n].Position.Y+(-j+(i*j))+j2;
				wrk_rect.left=(int)Units[n].Position.X-j2;

				DrawLine4((int)(wrk_rect.left-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.top),(int)(wrk_rect.right-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				DrawLine4((int)(wrk_rect.right-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.top),(int)(wrk_rect.right-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				DrawLine4((int)(wrk_rect.right-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.bottom),(int)(wrk_rect.left-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				DrawLine4((int)(wrk_rect.left-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.bottom),(int)(wrk_rect.left-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));

				}
			break;
		case 2: case 6:
			for( i=0; i<=2   ; i++)
				{
				wrk_rect.top=(int)Units[n].Position.Y-j2;
				wrk_rect.right=(int)Units[n].Position.X+(-j+(i*j))+j2;
				wrk_rect.bottom=(int)Units[n].Position.Y+j2;
				wrk_rect.left=(int)Units[n].Position.X+(-j+(i*j))-j2;

				DrawLine4((int)(wrk_rect.left-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.top),(int)(wrk_rect.right-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				DrawLine4((int)(wrk_rect.right-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.top),(int)(wrk_rect.right-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				DrawLine4((int)(wrk_rect.right-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.bottom),(int)(wrk_rect.left-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				DrawLine4((int)(wrk_rect.left-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.bottom),(int)(wrk_rect.left-CameraPosition.X),(int)(CameraPosition.Y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));

				}
			break;
		}

	return	;
	}

private bool LandCargo(ref Unit unit, FireKind kind, int m)
	{
	double wrk_x;
	double wrk_y;
	double drctn;
	double drctn2 =default;
	double dstc =default;
	int n;
	wrk_x=(double)unit.LandingX-unit.Position.X;
	wrk_y=(double)unit.LandingY-unit.Position.Y;
	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;
	drctn2=drctn;
	drctn=drctn-unit.Direction;
	if(drctn<0)
		drctn=360+drctn;

	if( (int)drctn<=45||(int)drctn>=315)
		{
		// 距離を求めます
		wrk_x=unit.Position.X-(double)unit.LandingX;
		wrk_y=unit.Position.Y-(double)unit.LandingY;
		if(wrk_x==0)	wrk_x=1;
		if(wrk_y==0)	wrk_y=1;

		drctn=atan2(wrk_y,wrk_x)*RAD_to;
		if(drctn<0)
			drctn=360+drctn;
		if(wrk_x<0)
			wrk_x=0-wrk_x;
		if(wrk_y<0)
			wrk_y=0-wrk_y;
		if(drctn>=180)
			drctn=drctn-180;
		if(drctn>=90)
			drctn=90-(drctn-90);
		dstc=(wrk_x)/(cos(drctn*a_PI));

#if NSPW_THE_NET
		if( dstc>=0 && dstc<=160 )
#else
		if( dstc>=0 && dstc<=360 )
#endif
			{
			// 発射！
			unit.Ammo=0;		// 残弾が０
			unit.Target=0;		// ターゲットをクリア

			//unit[m].arm[0]=NTG;		//  輸送船はこれやっと来ます、武装品種
			unit.MaxAmmo=0;		//  輸送船はこれやっと来ます、武装品種

			//unit[m].info[5]=RETURN;		// 航空機はメイン兵器ゼロで帰投

			// 部下、多分戦闘機に帰投命令

			n=FindFreeFire();
			if( n!=0 )
				{
				PlaySoundEffect( 0, SoundId.Splash ,unit.Position.X, unit.Position.Y);
				Fires[n].Target=MaxUnitId+1;
				Fires[n].Kind=kind;
				Fires[n].Position=unit.Position;
				Fires[n].Direction=drctn2;
				Fires[n].Speed=1.0;
				Fires[n].Acceleration=+0.0;
				Fires[n].FinalSpeed=0.0;
				Fires[n].Ticks=0;
				Fires[n].info[1]=360;
				Fires[n].TargetX=unit.LandingX;
				Fires[n].TargetY=unit.LandingY;
				Fires[n].ShooterSide=(int)unit.Side;

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

private bool ShipFireRapidAntiAircraftShell(ref Unit unit, ref double drctn2, ref double dstc, FireKind kind)
	{
	int trgt2;
	int n;
	double wrk_x;
	double wrk_y;
	double drctn;
	double turn;
	int f;
	double wrk_x2;
	double wrk_y2;
	double drctn3 =default;
	double dstc2;
	trgt2=0;
	for(n=1;n<=MaxUnitId;n++)
		{
		ref var other = ref Units[n];	//敵を探す。
		if( other.IsUsed && other.Category==UnitCategory.Plane && (((other.Kind==UnitKind.Attacker||other.Kind==UnitKind.Fighter) && other.Ammo!=0 )|| Random(10)==0 )  && other.PlaneState==UnitState.Flying && other.Side!=unit.Side && other.IsFound )
			{
			// 攻撃地点から攻撃目標地点への距離
			wrk_x=other.Position.X-unit.Position.X;
			wrk_y=other.Position.Y-unit.Position.Y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;
			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			turn=(wrk_x)/(cos(drctn*a_PI));
			turn=turn/10.0;

			// 攻撃地点から攻撃目標地点への絶対方位、方位角
			wrk_x=other.Position.X;
			wrk_y=other.Position.Y;
			drctn=other.TurnRate;
			drctn2=other.Direction;
			for(f=0;f<=(int)turn;f++)
				{
				drctn2+=drctn;
				if(drctn2<0)		drctn2=360+drctn2;
				if(drctn2>=360)		drctn2=drctn2-360;
				wrk_x+=cos(drctn2*a_PI)*(other.Speed); // とりあえずターン後
				wrk_y+=sin(drctn2*a_PI)*(other.Speed);
				}

			wrk_x2=wrk_x;
			wrk_y2=wrk_y;
			wrk_x=wrk_x2-unit.Position.X;
			wrk_y=wrk_y2-unit.Position.Y;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			drctn2=drctn;

			drctn=drctn-unit.Direction;
			if(drctn<0)
				drctn=360+drctn;
			drctn3=drctn;							// 方位角

			// 攻撃地点から攻撃目標地点への距離
			wrk_x=wrk_x2-unit.Position.X;
			wrk_y=wrk_y2-unit.Position.Y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;
			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			dstc=(wrk_x)/(cos(drctn*a_PI));

			if( dstc>=40 && dstc<=300 )
				{
				trgt2=n;
				if( 1!=0  )
					break;
				else
					trgt2=0;
				}
			}
		}
	if( trgt2!=0 )
		{

		/*
		f=fc[2];	// 後面
		if( drctn3>=315.0 || drctn3<=45.0 )
			f=fc[0];	// 正面
		if( (drctn3>=45.0 && drctn3<=135.0) || (drctn3>=225.0 && drctn3<=315.0) )
			f=fc[1];	// 側面
		*/

		n=FindFreeFire();
		if( n!=0 )
			{
			if(unit.Ammo!=0)
				unit.Ammo-=RAS_SZ;			// 弾薬消費

			if(SharedRandom(2)!=0 )
				PlaySoundEffect( 0, SoundId.AntiAircraftShell3 ,unit.Position.X, unit.Position.Y);
			else
				PlaySoundEffect( 0, SoundId.AntiAircraftShell5 ,unit.Position.X, unit.Position.Y);

			Fires[n].Target=trgt2;
			Fires[n].Kind=kind;
			Fires[n].Position=unit.Position;

			drctn3=drctn2+(Random(18)-9);			// 絶対方位
			if(drctn3>=360)	drctn3=drctn3-360;
			if(drctn3<0)	drctn3=360+drctn3;
			Fires[n].Direction=drctn3;

			dstc2=dstc+(Random( ((int)(dstc/5)) )-((int)(dstc/10))   );

			Fires[n].Speed=10.0;
			Fires[n].Acceleration=-0.00;
			Fires[n].FinalSpeed=0;
			Fires[n].Ticks=(int)(dstc2/Fires[n].Speed);
			Fires[n].info[1]=0;
			}
		}
	return false;
	return true;
	}

private bool ShipDropAntiSubmarineBomb(ref int trgt, ref Unit unit, FireKind kind)
	{
	int n;
	RECT wrk_r;
	double drctn;
	for( n=1; n<=MaxUnitId; n++)
		{
		trgt=n;
		if( Units[trgt].Side!=unit.Side && Units[trgt].Kind==UnitKind.Submarine && Units[trgt].IsSubmerged && Units[trgt].IsFound )
			{	// 爆雷

			// ptin dbg
			wrk_r.top=(int)Units[trgt].ContactY+Units[trgt].ContactRadius;
			wrk_r.right=(int)Units[trgt].ContactX+Units[trgt].ContactRadius;
			wrk_r.bottom=(int)Units[trgt].ContactY-Units[trgt].ContactRadius;
			wrk_r.left=(int)Units[trgt].ContactX-Units[trgt].ContactRadius;
			if( PointInRect3(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)!=0 )
				{
				// 投雷
				n=FindFreeFire();
					if(n!=0)
					{
					if(unit.Ammo!=0)
						unit.Ammo-=ASB_SZ;			// 弾薬消費
					if(unit.Ammo<0)
						unit.Ammo=0;

					PlaySoundEffect( 0, SoundId.Splash ,unit.Position.X, unit.Position.Y);
					Fires[n].Target=trgt;
					Fires[n].Kind=kind;
					Fires[n].Position=unit.Position;

					drctn=unit.Direction;
					if( unit.Variant==0 )
						{
						// ただの駆逐艦
						drctn+=180;
						drctn=(int)drctn%360;
						Fires[n].Position += new WorldVector(cos(drctn*a_PI)*20, sin(drctn*a_PI)*20);
						}
					else
						{
						// 対潜駆逐艦
						drctn+=120+Random(3)*60;
						drctn=(int)drctn%360;
						Fires[n].Position += new WorldVector(cos(drctn*a_PI)*35, sin(drctn*a_PI)*35);
						}

					Fires[n].Direction=0;
					Fires[n].Speed=0;
					Fires[n].Acceleration=0;
					Fires[n].FinalSpeed=0;
					Fires[n].Ticks=0;
					Fires[n].info[1]=100;
					}
				return false;
				}
			}
		}
	return false;
	return true;
	}

private bool ShipFireGunWithoutTarget(ref Unit unit, FireKind kind, ref int rng, ref Array3<int> fc, ref double drctn2, ref double drctn3)
	{
	int trgt2;
	double dstc2;
	int n;
	double wrk_x;
	double wrk_y;
	double drctn;
	double turn;
	double wrk_x2;
	double wrk_y2;
	double drctn4;
	double drctn5;
	double dstc =default;
	int f;
	int i;
	trgt2=0;
	dstc2=2000;
	for(n=1;n<=MaxUnitId;n++)
		{
		ref var other = ref Units[n];	//敵を探す。
		if( other.IsUsed && other.Category==UnitCategory.Ship && !(other.Kind==UnitKind.Submarine && other.IsSubmerged ) && other.Side!=unit.Side && other.IsFound  && other.Kind!=UnitKind.City )
			{
			// 攻撃地点から攻撃目標地点への距離
			wrk_x=other.Position.X-unit.Position.X;
			wrk_y=other.Position.Y-unit.Position.Y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;
			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			turn=(wrk_x)/(cos(drctn*a_PI));
			turn=turn/10.0;

			// 敵の未来位置を求めます。
			wrk_x=other.Position.X;
			wrk_y=other.Position.Y;
			wrk_x+=cos(other.Direction*a_PI)*(other.Speed*turn); // とりあえずターン後
			wrk_y+=sin(other.Direction*a_PI)*(other.Speed*turn);
			wrk_x2=wrk_x;										// ターゲットの未来位置
			wrk_y2=wrk_y;

			// 攻撃地点から攻撃目標地点への絶対方位、方位角
			wrk_x=wrk_x-unit.Position.X;
			wrk_y=wrk_y-unit.Position.Y;
			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			drctn4=drctn;							// 絶対方位

			drctn=drctn-unit.Direction;
			if(drctn<0)
				drctn=360+drctn;
			drctn5=drctn;							// 方位角

			// 攻撃地点から攻撃目標地点への距離
			wrk_x=wrk_x2-unit.Position.X;
			wrk_y=wrk_y2-unit.Position.Y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			dstc=(wrk_x)/(cos(drctn*a_PI));

			switch( unit.Kind )
				{
				case UnitKind.Battleship:
					if( kind==FireKind.NavalBaseGun )
						{
						rng=1120; fc[0]=3;fc[1]=4;fc[2]=2;
						}
					else
						{
						rng=600; fc[0]=3;fc[1]=4;fc[2]=2;
						}
					break;
				case UnitKind.Cruiser:	rng=500; fc[0]=1;fc[1]=2;fc[2]=1;	break;
				case UnitKind.Destroyer:	rng=400; fc[0]=1;fc[1]=1;fc[2]=0;	break;
				case UnitKind.Submarine:	rng=200; fc[0]=1;fc[1]=1;fc[2]=0;	break;

				case UnitKind.InfantryBase:	rng=600; fc[0]=1;fc[1]=1;fc[2]=1;	break;
				case UnitKind.Pillboxes:	rng=800; fc[0]=2;fc[1]=2;fc[2]=2;	break;
				case UnitKind.Fortress:	rng=1000; fc[0]=3;fc[1]=3;fc[2]=3;	break;
				}

			if( (dstc>=(rng*0.3) || (unit.Kind>=UnitKind.InfantryBase&&unit.Kind<=UnitKind.Fortress) ) && dstc<=rng && dstc2>=dstc )
				{
				trgt2=n;
				dstc2=dstc;
				drctn2=drctn4;
				drctn3=drctn5;
				}
			}
		}

	if( trgt2!=0 )
		{
		f=fc[2];	// 後面
		if( drctn3>=315.0 || drctn3<=45.0 )
			f=fc[0];	// 正面
		if( (drctn3>=45.0 && drctn3<=135.0) || (drctn3>=225.0 && drctn3<=315.0) )
			f=fc[1];	// 側面

		switch( unit.Kind )
			{
			case UnitKind.Battleship:
			case UnitKind.Fortress:
				if( kind==FireKind.NavalBaseGun )
					{
					PlaySoundEffect( 0, SoundId.Gun3 ,unit.Position.X, unit.Position.Y);
					break;
					}
				PlaySoundEffect( 0, SoundId.Gun2+Random(2) ,unit.Position.X, unit.Position.Y);
				break;
			case UnitKind.Cruiser:
			case UnitKind.Pillboxes:
				PlaySoundEffect( 0, SoundId.Gun1+Random(2) ,unit.Position.X, unit.Position.Y);
				break;

			case UnitKind.Destroyer:
			case UnitKind.Submarine:
			case UnitKind.InfantryBase:
				PlaySoundEffect( 0, SoundId.Gun1 ,unit.Position.X, unit.Position.Y);
				break;
			}

		for( i=1; i<=f ;i++ )
			{
			n=FindFreeFire();
			if( n!=0 )
				{
				if(unit.Ammo!=0)
					unit.Ammo-=GUN_SZ;			// 弾薬消費

				Fires[n].Target=trgt2;
				Fires[n].Kind=FireKind.Gun;
				Fires[n].Position=unit.Position;

				if( kind==FireKind.NavalBaseGun )
					{
					if( Random(2)==0 )
						drctn3=drctn2+(double)((double)(Random(50)-25)/10)    ;			// 絶対方位
					else
						drctn3=drctn2+(double)((double)(Random(80)-40)/10)    ;			// 絶対方位
					}
				else
					{
					if( Random(5+(unit.Kind>=UnitKind.InfantryBase&&unit.Kind<=UnitKind.Fortress ? 1 : 0)*4  )==0 || ( unit.Kind==UnitKind.Battleship && Random( 4 )==0 ) )
						drctn3=drctn2+(Random(7)-3)+(Random(100)/100);			// 絶対方位
					else
						drctn3=drctn2+(Random(11)-5)+(Random(100)/100);				// 絶対方位
					}

				if(drctn3>=360)	drctn3=drctn3-360;
				if(drctn3<0)	drctn3=360+drctn3;
				Fires[n].Direction=drctn3;

				if( kind==FireKind.NavalBaseGun )
					dstc2=dstc2+(Random( ((int)(dstc2/12)) )-((int)(dstc2/24)));
				else
					dstc2=dstc2+(Random( ((int)(dstc2/8)) )-((int)(dstc2/16)));

				Fires[n].Speed=10.0;
				Fires[n].Acceleration=((Fires[n].Speed)/(dstc2/Fires[n].Speed));
				Fires[n].FinalSpeed=0;
				Fires[n].Ticks=(int)(dstc2/Fires[n].Speed)+1;
				Fires[n].info[1]=Fires[n].Ticks/2;

				}
			}
		}
	return false;
	return true;
	}

private bool ShipFireTorpedoWithoutTarget(ref Unit unit, ref double drctn2, FireKind kind)
	{
	int trgt2;
	int n;
	double wrk_x;
	double wrk_y;
	double drctn;
	double turn;
	double wrk_x2;
	double wrk_y2;
	double drctn3 =default;
	double dstc =default;
	int i;
	int f;
	int cm_scrn_x;
	int cm_scrn_y;
	trgt2=0;
	for(n=1;n<=MaxUnitId;n++)
		{
		ref var other = ref Units[n];	//敵を探す。
		if( other.IsUsed && other.Category==UnitCategory.Ship && !(other.Kind==UnitKind.AirBase||other.Kind==UnitKind.NavalBase||other.Kind==UnitKind.InfantryBase||other.Kind==UnitKind.Pillboxes||other.Kind==UnitKind.Fortress) && other.Side!=unit.Side && other.IsFound && !(other.Kind==UnitKind.Submarine||other.Kind==UnitKind.Destroyer) )
			{
			// 攻撃地点から攻撃目標地点への距離
			wrk_x=other.Position.X-unit.Position.X;
			wrk_y=other.Position.Y-unit.Position.Y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;
			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			turn=(wrk_x)/(cos(drctn*a_PI));
			turn=turn/TPD_SPD;							// 撃つ弾の速度で割る

			// 攻撃地点から攻撃目標地点への絶対方位、方位角
			wrk_x=other.Position.X;
			wrk_y=other.Position.Y;
			wrk_x+=cos(other.Direction*a_PI)*(other.Speed*turn);
			wrk_y+=sin(other.Direction*a_PI)*(other.Speed*turn);
/****
			drctn=unit[n].drctn_add;
			drctn2=unit[n].drctn;
			for(f=0;f<=(int)turn;f++)
				{
				drctn2+=drctn;
				if(drctn2<0)		drctn2=360+drctn2;
				if(drctn2>=360)		drctn2=drctn2-360;
				wrk_x+=cos(drctn2*a_PI)*(unit[n].spd); // とりあえずターン後
				wrk_y+=sin(drctn2*a_PI)*(unit[n].spd);
				}
***/
			wrk_x2=wrk_x;					// 標的の未来位置
			wrk_y2=wrk_y;					//
			wrk_x=wrk_x2-unit.Position.X;
			wrk_y=wrk_y2-unit.Position.Y;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			drctn2=drctn;					// 未来位置への絶対角

			drctn=drctn-unit.Direction;
			if(drctn<0)
				drctn=360+drctn;
			drctn3=drctn;							// 方位角

			// 攻撃地点から攻撃目標地点への距離
			wrk_x=wrk_x2-unit.Position.X;
			wrk_y=wrk_y2-unit.Position.Y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;
			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			dstc=(wrk_x)/(cos(drctn*a_PI));

			if( (dstc>=100 && dstc<=(500+(unit.Side==Side.Japan ? 1 : 0)*100)) && ((drctn3>=45&&drctn3<=135)||(drctn3>=225&&drctn3<=315)) )
				{
				// ばってん陸地があるけんしらべる
				trgt2=n;

				i=(int)(dstc/TPD_SPD);
				for(f=1;f<=i;f++)
					{
					wrk_x=unit.Position.X;
					wrk_y=unit.Position.Y;
					wrk_x+=cos(drctn2*a_PI)*(TPD_SPD*f); // とりあえずターン後
					wrk_y+=sin(drctn2*a_PI)*(TPD_SPD*f);

					if(!( wrk_y>MAP_TOP || wrk_y<MAP_BOTTOM || wrk_x<MAP_LEFT || wrk_x>MAP_RIGHT ))
						{
						cm_scrn_x=(int)((wrk_x+(Sprites[SpriteId.JapanUnits].wd/2)-MAP_LEFT)/Sprites[SpriteId.MapTiles].wd);
						cm_scrn_y=(int)((MAP_TOP-wrk_y+(Sprites[SpriteId.JapanUnits].ht/2))/Sprites[SpriteId.MapTiles].ht);
						if( MapTiles[cm_scrn_y][cm_scrn_x]>=1 )
							{
							trgt2=0;
							break;
							}
						}
					}

				if(trgt2!=0)
					{
					if( Random(3)==0 )
						break;
					else
						trgt2=0;
					}
				}
			}
		}
	if( trgt2!=0 )
		{

		// 発射！
		if(unit.Ammo!=0)
			unit.Ammo-=TPD_SZ;
		switch( unit.Kind )
			{
			case UnitKind.Cruiser:
			case UnitKind.Destroyer:		unit.ReloadTime=RELOAD_TPD_DD;		break;	// 再装填時間
			case UnitKind.Submarine:		unit.ReloadTime=RELOAD_TPD_SS;
							//撃った瞬間に発見される。
							unit.ContactX=(int)(unit.Position.X+Random((50)*2)-50);
							unit.ContactY=(int)(unit.Position.Y+Random((50)*2)-50);

							unit.ContactRadius=100;
							unit.ContactTime=300;

							unit.IsFound=true;

							break;	// 再装填時間
			}

		if( unit.Side==Side.Japan  )
			{
			if(unit.Kind==UnitKind.Destroyer)
				i=2;						// 日本海軍駆逐艦魚雷３発
			else
				i=1;						// 日本海軍巡洋艦魚雷２はつ
			}
		else
			{
			i=0;						// 合衆国海軍魚雷１発
			}

		if( unit.Kind!=UnitKind.Submarine )
			PlaySoundEffect( 0, SoundId.TorpedoLaunch ,unit.Position.X, unit.Position.Y);

		for( f=0; f<=i; f++)
			{
			n=FindFreeFire();
			if( n!=0 )
				{
				Fires[n].Target=trgt2;
				Fires[n].Kind=kind;
				Fires[n].Position=unit.Position;

				switch( f )
					{
					case 0:	Fires[n].Direction=drctn2;	break;
					case 1:	Fires[n].Direction=drctn2+5;	break;
					case 2:	Fires[n].Direction=drctn2-5;	break;
					}
				Fires[n].Direction=(int)(Fires[n].Direction)%360;

				Fires[n].Speed=TPD_SPD;
				Fires[n].Acceleration=+0.0;
				Fires[n].FinalSpeed=0.0;
				if( unit.Kind==UnitKind.Submarine )
					Fires[n].Ticks=1;
				else
					Fires[n].Ticks=0;
				Fires[n].info[1]=275+(unit.Side==Side.Japan ? 1 : 0)*110;
				Fires[n].ArmingTime=30;
				}
			}
		}
	return false;
	return true;
	}

private bool ShipFireTorpedoAtTarget(int trgt, ref Unit unit, ref double drctn2, FireKind kind)
	{
	int trgt2;
	int n;
	double wrk_x;
	double wrk_y;
	double drctn;
	double turn;
	double wrk_x2;
	double wrk_y2;
	double drctn3 =default;
	double dstc =default;
	int i;
	int f;
	int cm_scrn_x;
	int cm_scrn_y;
	trgt2=0;
	n=trgt;
	if( Units[n].IsUsed && Units[n].Category==UnitCategory.Ship && !(Units[n].Kind==UnitKind.AirBase||Units[n].Kind==UnitKind.NavalBase||Units[n].Kind==UnitKind.InfantryBase||Units[n].Kind==UnitKind.Pillboxes||Units[n].Kind==UnitKind.Fortress) && Units[n].Side!=unit.Side && Units[n].IsFound )
		{

		// 攻撃地点から攻撃目標地点への距離
		wrk_x=Units[n].Position.X-unit.Position.X;
		wrk_y=Units[n].Position.Y-unit.Position.Y;
		if(wrk_x==0)	wrk_x=1;
		if(wrk_y==0)	wrk_y=1;
		drctn=atan2(wrk_y,wrk_x)*RAD_to;
		if(drctn<0)
			drctn=360+drctn;
		if(wrk_x<0)
			wrk_x=0-wrk_x;
		if(wrk_y<0)
			wrk_y=0-wrk_y;
		if(drctn>=180)
			drctn=drctn-180;
		if(drctn>=90)
			drctn=90-(drctn-90);
		turn=(wrk_x)/(cos(drctn*a_PI));
		turn=turn/TPD_SPD;							// 撃つ弾の速度で割る

		// 攻撃地点から攻撃目標地点への絶対方位、方位角
		wrk_x=Units[n].Position.X;
		wrk_y=Units[n].Position.Y;
		wrk_x+=cos(Units[n].Direction*a_PI)*(Units[n].Speed*turn);
		wrk_y+=sin(Units[n].Direction*a_PI)*(Units[n].Speed*turn);
		wrk_x2=wrk_x;					// 標的の未来位置
		wrk_y2=wrk_y;					//
		wrk_x=wrk_x2-unit.Position.X;
		wrk_y=wrk_y2-unit.Position.Y;

		drctn=atan2(wrk_y,wrk_x)*RAD_to;
		if(drctn<0)
			drctn=360+drctn;
		drctn2=drctn;					// 未来位置への絶対角

		drctn=drctn-unit.Direction;
		if(drctn<0)
			drctn=360+drctn;
		drctn3=drctn;							// 方位角

		// 攻撃地点から攻撃目標地点への距離
		wrk_x=wrk_x2-unit.Position.X;
		wrk_y=wrk_y2-unit.Position.Y;
		if(wrk_x==0)	wrk_x=1;
		if(wrk_y==0)	wrk_y=1;
		drctn=atan2(wrk_y,wrk_x)*RAD_to;
		if(drctn<0)
			drctn=360+drctn;
		if(wrk_x<0)
			wrk_x=0-wrk_x;
		if(wrk_y<0)
			wrk_y=0-wrk_y;
		if(drctn>=180)
			drctn=drctn-180;
		if(drctn>=90)
			drctn=90-(drctn-90);
		dstc=(wrk_x)/(cos(drctn*a_PI));

		if( (dstc>=100 && dstc<=(550+(unit.Side==Side.Japan ? 1 : 0)*100)) && (drctn3<=5 || drctn3>=355) )
			{
			trgt2=n;
			i=(int)(dstc/TPD_SPD);
			for(f=1;f<=i;f++)
				{
				wrk_x=unit.Position.X;
				wrk_y=unit.Position.Y;
				wrk_x+=cos(drctn2*a_PI)*(TPD_SPD*f); // とりあえずターン後
				wrk_y+=sin(drctn2*a_PI)*(TPD_SPD*f);

				if(!( wrk_y>MAP_TOP || wrk_y<MAP_BOTTOM || wrk_x<MAP_LEFT || wrk_x>MAP_RIGHT ))
					{
					cm_scrn_x=(int)((wrk_x+(Sprites[SpriteId.JapanUnits].wd/2)-MAP_LEFT)/Sprites[SpriteId.MapTiles].wd);
					cm_scrn_y=(int)((MAP_TOP-wrk_y+(Sprites[SpriteId.JapanUnits].ht/2))/Sprites[SpriteId.MapTiles].ht);
					if( MapTiles[cm_scrn_y][cm_scrn_x]>=1 )
						{
						trgt2=0;
						break;
						}
					}
				}
			}
		}

	if( trgt2!=0 )
		{
		// 発射！
		if(unit.Ammo!=0)
			unit.Ammo-=TPD_SZ;
		switch( unit.Kind )
			{
			case UnitKind.Destroyer:		unit.ReloadTime=RELOAD_TPD_DD;		break;	// 再装填時間
			case UnitKind.Submarine:		unit.ReloadTime=RELOAD_TPD_SS;
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
			PlaySoundEffect( 0, SoundId.TorpedoLaunch ,unit.Position.X, unit.Position.Y);
			}
		else
			{
			unit.Target=0;
			}

		for( f=0; f<=2; f++)
			{
			n=FindFreeFire();
			if( n!=0 )
				{
				Fires[n].Target=trgt2;
				Fires[n].Kind=kind;
				Fires[n].Position=unit.Position;

				switch( f )
					{
					case 0:	Fires[n].Direction=drctn2+5;	break;
					case 1:	Fires[n].Direction=drctn2;	break;
					case 2:	Fires[n].Direction=drctn2-5;	break;
					}
				Fires[n].Direction=(int)(Fires[n].Direction)%360;

				Fires[n].Speed=TPD_SPD;
				Fires[n].Acceleration=+0.0;
				Fires[n].FinalSpeed=0.0;
				if( unit.Kind==UnitKind.Submarine )
					Fires[n].Ticks=1;
				else
					Fires[n].Ticks=0;
				Fires[n].info[1]=290+(unit.Side==Side.Japan ? 1 : 0)*110;
				Fires[n].ArmingTime=30;
				}
			}

		}
	return false;
	return true;
	}

private bool ShipFireAntiAircraftShellAtTarget(int trgt, ref Unit unit, ref int rng, ref Array3<int> fc)
	{
	int trgt2;
	int n;
	double wrk_x;
	double wrk_y;
	double drctn;
	double turn;
	double wrk_x2;
	double wrk_y2;
	double drctn2 =default;
	double drctn3 =default;
	double dstc =default;
	int f;
	int i;
	double dstc2;
	if( !Units[trgt].IsFound )
		return false;

	trgt2=0;
	n=trgt;
	// 攻撃地点から攻撃目標地点への絶対方位、方位角

	wrk_x=Units[n].Position.X-unit.Position.X;
	wrk_y=Units[n].Position.Y-unit.Position.Y;
	if(wrk_x==0)	wrk_x=1;
	if(wrk_y==0)	wrk_y=1;
	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;
	if(wrk_x<0)
		wrk_x=0-wrk_x;
	if(wrk_y<0)
		wrk_y=0-wrk_y;
	if(drctn>=180)
		drctn=drctn-180;
	if(drctn>=90)
		drctn=90-(drctn-90);
	turn=(wrk_x)/(cos(drctn*a_PI));
	turn=turn/10.0;

	// ターゲットの未来位置を求めます。
	wrk_x=Units[n].Position.X;
	wrk_y=Units[n].Position.Y;
	wrk_x+=cos(Units[n].Direction*a_PI)*(Units[n].Speed*turn); // ターン後
	wrk_y+=sin(Units[n].Direction*a_PI)*(Units[n].Speed*turn);
	wrk_x2=wrk_x;										// 未来位置
	wrk_y2=wrk_y;

	// ターゲットの方位関係を
	wrk_x=wrk_x-unit.Position.X;
	wrk_y=wrk_y-unit.Position.Y;

	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;
	drctn2=drctn;

	drctn=drctn-unit.Direction;
	if(drctn<0)
		drctn=360+drctn;
	drctn3=drctn;							// 方位角

	// 攻撃地点から攻撃目標地点への距離
	wrk_x=wrk_x2-unit.Position.X;
	wrk_y=wrk_y2-unit.Position.Y;
	if(wrk_x==0)	wrk_x=1;
	if(wrk_y==0)	wrk_y=1;

	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;
	if(wrk_x<0)
		wrk_x=0-wrk_x;
	if(wrk_y<0)
		wrk_y=0-wrk_y;
	if(drctn>=180)
		drctn=drctn-180;
	if(drctn>=90)
		drctn=90-(drctn-90);
	dstc=(wrk_x)/(cos(drctn*a_PI));

	switch( unit.Kind )
		{
		case UnitKind.Battleship:	rng=700; fc[0]=2;fc[1]=3;fc[2]=1;	break;
		case UnitKind.Cruiser:	rng=600; fc[0]=1;fc[1]=2;fc[2]=1;	break;
		case UnitKind.Destroyer:	rng=300; fc[0]=1;fc[1]=1;fc[2]=0;	break;

		case UnitKind.InfantryBase:	rng=500; fc[0]=1;fc[1]=1;fc[2]=1;	break;
		case UnitKind.Pillboxes:	rng=600; fc[0]=fc[1]=fc[2]=Random(2)+1;	break;
		case UnitKind.Fortress:	rng=700; fc[0]=fc[1]=fc[2]=Random(2)+2;	break;
		}

	if( dstc>=(rng*0.25) && dstc<=rng )
		{
		trgt2=n;
		}

	if( trgt2!=0 )
		{
		f=fc[2];	// 後面
		if( drctn3>=315.0 || drctn3<=45.0 )
			f=fc[0];	// 正面
		if( (drctn3>=45.0 && drctn3<=135.0) || (drctn3>=225.0 && drctn3<=315.0) )
			f=fc[1];	// 側面

		PlaySoundEffect( 0, SoundId.AntiAircraftShell2 ,unit.Position.X, unit.Position.Y);
		for( i=1; i<=f ;i++ )
			{

			n=FindFreeFire();
			if( n!=0 )
				{
				if(unit.Ammo!=0)
					unit.Ammo-=SHL_SZ;			// 弾薬消費
				Fires[n].Target=trgt2;

				Fires[n].Kind=FireKind.AntiAircraftShell;

				Fires[n].Position=unit.Position;

				drctn3=drctn2+(Random(18)-9);			// 絶対方位
				if(drctn3>=360)	drctn3=drctn3-360;
				if(drctn3<0)	drctn3=360+drctn3;
				Fires[n].Direction=drctn3;

				dstc2=dstc+(Random( ((int)(dstc/10)) )-((int)(dstc/20))   );

				Fires[n].Speed=10.0;
				Fires[n].Acceleration=-0.00;
				Fires[n].FinalSpeed=0;
				Fires[n].Ticks=(int)(dstc2/Fires[n].Speed);
				Fires[n].info[1]=0;

				}
			}
		}
	return false;
	return true;
	}

private bool ShipFireAntiAircraftShellWithoutTarget(ref Unit unit, ref double drctn2, ref double drctn3, ref double dstc, ref int rng, ref Array3<int> fc, FireKind kind)
	{
	int trgt2;
	int n;
	double wrk_x;
	double wrk_y;
	double drctn;
	double turn;
	int f;
	double wrk_x2;
	double wrk_y2;
	int i;
	double dstc2;
	trgt2=0;
	for(n=1;n<=MaxUnitId;n++)
		{
		ref var other = ref Units[n];	//敵を探す。
		if( other.IsUsed && other.Category==UnitCategory.Plane && (((other.Kind==UnitKind.Attacker||other.Kind==UnitKind.Bomber) && other.Ammo!=0 )|| Random(10)==0 )  && other.PlaneState==UnitState.Flying && other.Side!=unit.Side && other.IsFound )
			{
			// 攻撃地点から攻撃目標地点への距離
			wrk_x=other.Position.X-unit.Position.X;
			wrk_y=other.Position.Y-unit.Position.Y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;
			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			turn=(wrk_x)/(cos(drctn*a_PI));
			turn=turn/10.0;

			// 攻撃地点から攻撃目標地点への絶対方位、方位角
			wrk_x=other.Position.X;
			wrk_y=other.Position.Y;
			drctn=other.TurnRate;
			drctn2=other.Direction;
			for(f=0;f<=(int)turn;f++)
				{
				drctn2+=drctn;
				if(drctn2<0)		drctn2=360+drctn2;
				if(drctn2>=360)		drctn2=drctn2-360;
				wrk_x+=cos(drctn2*a_PI)*(other.Speed); // とりあえずターン後
				wrk_y+=sin(drctn2*a_PI)*(other.Speed);
				}

			wrk_x2=wrk_x;
			wrk_y2=wrk_y;
			wrk_x=wrk_x2-unit.Position.X;
			wrk_y=wrk_y2-unit.Position.Y;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			drctn2=drctn;

			drctn=drctn-unit.Direction;
			if(drctn<0)
				drctn=360+drctn;
			drctn3=drctn;							// 方位角

			// 攻撃地点から攻撃目標地点への距離
			wrk_x=wrk_x2-unit.Position.X;
			wrk_y=wrk_y2-unit.Position.Y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;
			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			dstc=(wrk_x)/(cos(drctn*a_PI));

			switch( unit.Kind )
				{
				case UnitKind.Battleship:	rng=700; fc[0]=2;fc[1]=3;fc[2]=1;	break;
				case UnitKind.Cruiser:	rng=600; fc[0]=1;fc[1]=2;fc[2]=1;	break;
				case UnitKind.Destroyer:	rng=300; fc[0]=1;fc[1]=1;fc[2]=0;	break;

				case UnitKind.InfantryBase:	rng=500; fc[0]=1;fc[1]=1;fc[2]=1;	break;
				case UnitKind.Pillboxes:	rng=600; fc[0]=fc[1]=fc[2]=Random(2)+1;	break;
				case UnitKind.Fortress:	rng=700; fc[0]=fc[1]=fc[2]=Random(2)+2;	break;
				}

				if( dstc>=(rng*0.25) && dstc<=rng )
					{
					trgt2=n;
					break;
					}
			}
		}
	if( trgt2!=0 )
		{

		f=fc[2];	// 後面
		if( drctn3>=315.0 || drctn3<=45.0 )
			f=fc[0];	// 正面
		if( (drctn3>=45.0 && drctn3<=135.0) || (drctn3>=225.0 && drctn3<=315.0) )
			f=fc[1];	// 側面

		for( i=1; i<=f ;i++ )
			{
			n=FindFreeFire();
			if( n!=0 )
				{
				if(unit.Ammo!=0)
					unit.Ammo-=SHL_SZ;			// 弾薬消費
				PlaySoundEffect( 0, SoundId.AntiAircraftShell2 ,unit.Position.X, unit.Position.Y);
				Fires[n].Target=trgt2;
				Fires[n].Kind=kind;
				Fires[n].Position=unit.Position;

				drctn3=drctn2+(Random(18)-9);			// 絶対方位
				if(drctn3>=360)	drctn3=drctn3-360;
				if(drctn3<0)	drctn3=360+drctn3;
				Fires[n].Direction=drctn3;

				dstc2=dstc+(Random( ((int)(dstc/10)) )-((int)(dstc/20))   );

				Fires[n].Speed=10.0;
				Fires[n].Acceleration=-0.00;
				Fires[n].FinalSpeed=0;
				Fires[n].Ticks=(int)(dstc2/Fires[n].Speed);
				Fires[n].info[1]=0;
				}
			}
		}
	return false;
	return true;
	}

private bool ShipFireGunAtTarget(int trgt, ref Unit unit, FireKind kind, ref int rng, ref Array3<int> fc)
	{
	int trgt2;
	int n;
	double wrk_x;
	double wrk_y;
	double drctn;
	double turn;
	double wrk_x2;
	double wrk_y2;
	double drctn2 =default;
	double drctn3 =default;
	double dstc =default;
	int f;
	int i;
	double dstc2;
	if( !Units[trgt].IsFound )
		return false;

	trgt2=0;
	n=trgt;
	// 攻撃地点から攻撃目標地点への絶対方位、方位角

	// 攻撃地点から攻撃目標地点への距離
	wrk_x=Units[n].Position.X-unit.Position.X;
	wrk_y=Units[n].Position.Y-unit.Position.Y;
	if(wrk_x==0)	wrk_x=1;
	if(wrk_y==0)	wrk_y=1;
	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;
	if(wrk_x<0)
		wrk_x=0-wrk_x;
	if(wrk_y<0)
		wrk_y=0-wrk_y;
	if(drctn>=180)
		drctn=drctn-180;
	if(drctn>=90)
		drctn=90-(drctn-90);
	turn=(wrk_x)/(cos(drctn*a_PI));
	turn=turn/10.0;

	// ターゲットの未来位置を求めます。
	wrk_x=Units[n].Position.X;
	wrk_y=Units[n].Position.Y;
	wrk_x+=cos(Units[n].Direction*a_PI)*(Units[n].Speed*turn); // ターン後
	wrk_y+=sin(Units[n].Direction*a_PI)*(Units[n].Speed*turn);
	wrk_x2=wrk_x;										// 未来位置
	wrk_y2=wrk_y;

	// ターゲットの方位関係を
	wrk_x=wrk_x-unit.Position.X;
	wrk_y=wrk_y-unit.Position.Y;

	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;
	drctn2=drctn;

	drctn=drctn-unit.Direction;
	if(drctn<0)
		drctn=360+drctn;
	drctn3=drctn;							// 方位角

	// 攻撃地点から攻撃目標地点への距離
	wrk_x=wrk_x2-unit.Position.X;
	wrk_y=wrk_y2-unit.Position.Y;
	if(wrk_x==0)	wrk_x=1;
	if(wrk_y==0)	wrk_y=1;

	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;
	if(wrk_x<0)
		wrk_x=0-wrk_x;
	if(wrk_y<0)
		wrk_y=0-wrk_y;
	if(drctn>=180)
		drctn=drctn-180;
	if(drctn>=90)
		drctn=90-(drctn-90);
	dstc=(wrk_x)/(cos(drctn*a_PI));

	// 水上艦への射程距離
	switch( unit.Kind )
		{
		case UnitKind.Battleship:
			if( kind==FireKind.NavalBaseGun )
				{
				rng=1120; fc[0]=3;fc[1]=5;fc[2]=2;
				}
			else
				{
				rng=800; fc[0]=3;fc[1]=5;fc[2]=2;
				}
			break;
		case UnitKind.Cruiser:	rng=700; fc[0]=2;fc[1]=3;fc[2]=1;	break;
		case UnitKind.Destroyer:	rng=400; fc[0]=1;fc[1]=1;fc[2]=0;	break;
		case UnitKind.Submarine:	rng=300; fc[0]=1;fc[1]=1;fc[2]=0;	break;

		case UnitKind.InfantryBase:	rng=600; fc[0]=1;fc[1]=1;fc[2]=1;	break;
		case UnitKind.Pillboxes:	rng=800; fc[0]=2;fc[1]=2;fc[2]=2;	break;
		case UnitKind.Fortress:	rng=1000; fc[0]=3;fc[1]=3;fc[2]=3;	break;
		}

	if( (dstc>=150  || (unit.Kind>=UnitKind.InfantryBase&&unit.Kind<=UnitKind.Fortress) ) && dstc<=rng )
		{
		trgt2=n;
		}

	if( trgt2!=0 )
		{
		f=fc[2];	// 後面
		if( drctn3>=315.0 || drctn3<=45.0 )
			f=fc[0];	// 正面
		if( (drctn3>=45.0 && drctn3<=135.0) || (drctn3>=225.0 && drctn3<=315.0) )
			f=fc[1];	// 側面

		switch( unit.Kind )
			{
			case UnitKind.Battleship:
			case UnitKind.Fortress:
				PlaySoundEffect( 0, SoundId.Gun3 ,unit.Position.X, unit.Position.Y);
				break;
			case UnitKind.Cruiser:
			case UnitKind.Pillboxes:
				PlaySoundEffect( 0, SoundId.Gun2 ,unit.Position.X, unit.Position.Y);
				break;

			case UnitKind.Destroyer:
			case UnitKind.Submarine:
			case UnitKind.InfantryBase:
				PlaySoundEffect( 0, SoundId.Gun1+Random(2) ,unit.Position.X, unit.Position.Y);
				break;
			}

		for( i=1; i<=f ;i++ )
			{

			n=FindFreeFire();
			if( n!=0 )
				{
				unit.Ammo-=GUN_SZ;			// 弾薬消費
				Fires[n].Target=trgt2;

				if( Units[trgt2].Category==UnitCategory.Ship )
					{
					Fires[n].Kind=FireKind.Gun;

					Fires[n].Position=unit.Position;

					if( kind==FireKind.NavalBaseGun && 0!=0 )
						{
						if( Random(3)!=0 )
							drctn3=drctn2+(double)((double)(Random(20)-10)/10)    ;			// 絶対方位
						else
							drctn3=drctn2+(double)((double)(Random(60)-30)/10)    ;			// 絶対方位
						}
					else
						{
						if( Random(5)==0 || ( unit.Kind==UnitKind.Battleship && Random(4)==0 ) )
							drctn3=drctn2+(Random(3)-1)+(Random(100)/100);			// 絶対方位
						else
							drctn3=drctn2+(Random(9)-4)+(Random(100)/100);				// 絶対方位
						}

					if(drctn3>=360)	drctn3=drctn3-360;
					if(drctn3<0)	drctn3=360+drctn3;
					Fires[n].Direction=drctn3;

					if( kind==FireKind.NavalBaseGun )
						dstc2=dstc+(Random( ((int)(dstc/12)) )-((int)(dstc/24)));
					else
						dstc2=dstc+(Random( ((int)(dstc/8)) )-((int)(dstc/16))   );

					Fires[n].Speed=10.0;
					Fires[n].Acceleration=((Fires[n].Speed)/(dstc2/Fires[n].Speed));
					Fires[n].FinalSpeed=0;
					Fires[n].Ticks=(int)(dstc2/Fires[n].Speed);
					Fires[n].info[1]=Fires[n].Ticks/2;
					}
				else
					{
					Fires[n].Kind=FireKind.AntiAircraftShell;

					Fires[n].Position=unit.Position;

					drctn3=drctn2+(Random(20)-10);			// 絶対方位
					if(drctn3>=360)	drctn3=drctn3-360;
					if(drctn3<0)	drctn3=360+drctn3;
					Fires[n].Direction=drctn3;

					dstc2=dstc+(Random( ((int)(dstc/10)) )-((int)(dstc/20))   );

					Fires[n].Speed=10.0;
					Fires[n].Acceleration=-0.00;
					Fires[n].FinalSpeed=0;
					Fires[n].Ticks=(int)(dstc2/Fires[n].Speed);
					Fires[n].info[1]=0;
					}

				}
			}
		}
	return false;
	return true;
	}

private bool ShipFireBullet(ref int trgt, ref Unit unit, ref double drctn2, FireKind kind)
	{
	int n;
	double wrk_x;
	double wrk_y;
	double drctn;
	double dstc =default;
	trgt=0;
	for(n=1;n<=MaxUnitId;n++)
		{
		ref var other = ref Units[n];	//敵を探す。
		if( other.IsUsed && ( (other.Category==UnitCategory.Plane && other.PlaneState==UnitState.Flying )  || (other.Kind>=UnitKind.InfantryBase && other.Kind<=UnitKind.Fortress ) ) && other.Kind!=UnitKind.Bomber
		&& other.Side!=unit.Side && other.IsFound )
			{
			// 全方位射撃可能
			wrk_x=other.Position.X-unit.Position.X;
			wrk_y=other.Position.Y-unit.Position.Y;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;

			if(drctn<0)
				drctn=360+drctn;
			drctn2=drctn+(25-Random(50));
			drctn2=abs((int)drctn2)%360;

			// 攻撃地点から攻撃目標地点への距離
			wrk_x=other.Position.X-unit.Position.X;
			wrk_y=other.Position.Y-unit.Position.Y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;
			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			dstc=(wrk_x)/(cos(drctn*a_PI));
			if( dstc<=100 )
				{
				trgt=n;
				if(Random(2)==1 )
					break;
				else
					trgt=0;
				}
			else
				{
				if( dstc<=300 )
					{
					trgt=n;
					if(Random(8)==1 )
						break;
					else
						trgt=0;
					}
				}
			}
		}

	if( trgt!=0 )
		{
		n=FindFreeFire();
		if( n!=0 )
			{
			//unit[m].arm[1]--;			// 弾薬消費
			PlaySoundEffect( 0, SoundId.AntiAircraftBullet3 ,unit.Position.X, unit.Position.Y);
			Fires[n].Target=trgt;
			Fires[n].Kind=kind;
			Fires[n].Position=unit.Position;
			Fires[n].Direction=drctn2;
			Fires[n].Speed=17.0;
			Fires[n].Acceleration=-0.1;
			Fires[n].FinalSpeed=15.0;
			}
		}
	return false;
	return true;
	}

private bool BomberFireBullet(ref int trgt, ref Unit unit, ref double drctn2, FireKind kind)
	{
	int n;
	double wrk_x;
	double wrk_y;
	double drctn;
	double dstc =default;
	trgt=0;
	for(n=1;n<=MaxUnitId;n++)
		{
		ref var other = ref Units[n];	//敵を探す。
		if( other.IsUsed && other.Category==UnitCategory.Plane && other.PlaneState==UnitState.Flying && other.Side!=unit.Side && other.IsFound )
			{
			// 全方位射撃可能
			wrk_x=other.Position.X-unit.Position.X;
			wrk_y=other.Position.Y-unit.Position.Y;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;

			if(drctn<0)
				drctn=360+drctn;
			drctn2=drctn+(Random(20)-10);
			drctn2=abs((int)drctn2)%360;

			// 攻撃地点から攻撃目標地点への距離
			wrk_x=other.Position.X-unit.Position.X;
			wrk_y=other.Position.Y-unit.Position.Y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;
			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			dstc=(wrk_x)/(cos(drctn*a_PI));
			if( dstc<=100 )
				{
				trgt=n;
				if(Random(2)==1 )
					break;
				else
					trgt=0;
				}
			else
				{
				if( dstc<=300 )
					{
					trgt=n;
					if(Random(8)==1 )
						break;
					else
						trgt=0;
					}
				}
			}
		}
	if( trgt!=0 )
		{
		n=FindFreeFire();
		if( n!=0 )
			{
			//unit[m].arm[1]--;			// 弾薬消費
			PlaySoundEffect( 0, SoundId.AntiAircraftBullet4 ,unit.Position.X, unit.Position.Y);
			Fires[n].Target=trgt;
			Fires[n].Kind=kind;
			Fires[n].Position=unit.Position;
			Fires[n].Direction=drctn2;
			Fires[n].Speed=17.0;
			Fires[n].Acceleration=-0.1;
			Fires[n].FinalSpeed=15.0;
			}
		}
	return false;
	return true;
	}

private bool FighterFireBullet(ref int trgt, ref Unit unit, FireKind kind)
	{
	double dstc2;
	int n;
	double wrk_x;
	double wrk_y;
	double drctn;
	double dstc =default;
	trgt=0;		dstc2=5000;
	for(n=1;n<=MaxUnitId;n++)
		{
		ref var other = ref Units[n];	//前方の敵を探す。
		if( other.IsUsed && (( other.Category==UnitCategory.Plane && other.PlaneState==UnitState.Flying ) || other.Kind==UnitKind.Transport ) && other.Side!=unit.Side )
			{
			// 距離を調べます
			wrk_x=other.Position.X-unit.Position.X;
			wrk_y=other.Position.Y-unit.Position.Y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			dstc=(wrk_x)/(cos(drctn*a_PI));
			if( dstc<=300 && dstc<=dstc2)
				{
				// 攻撃地点から攻撃目標地点への方位角
				wrk_x=other.Position.X-unit.Position.X;
				wrk_y=other.Position.Y-unit.Position.Y;
				drctn=atan2(wrk_y,wrk_x)*RAD_to;
				if(drctn<0)
					drctn=360+drctn;
				drctn=drctn-unit.Direction;
				if(drctn<0)
					drctn=360+drctn;
				if( ((int)drctn<=10||(int)drctn>=350) && unit.Target!=0 && unit.Target==n )
					{
					dstc2=dstc;
					trgt=n;
					}
				}

			if( unit.Target==0 )
				{
				if(unit.Mode==UnitMode.Return)
					{
					}
				else
					{
					if( dstc<=400+(other.Kind==UnitKind.Attacker||other.Kind==UnitKind.Bomber ? 1 : 0)*250 && Random(10)==0 )
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

	if( trgt!=0 )
		{
		n=FindFreeFire();
		if( n!=0 )
			{
			unit.Ammo--;			// 弾薬消費
			if(unit.Variant==0)
				{
				// 艦上戦闘機
				if(unit.Side==Side.Japan)
					PlaySoundEffect( 0, SoundId.AntiAircraftBullet1 ,unit.Position.X, unit.Position.Y);
				else
					PlaySoundEffect( 0, SoundId.AntiAircraftBullet2 ,unit.Position.X, unit.Position.Y);
				}
			else
				{
				// 陸上戦闘機
				PlaySoundEffect( 0, SoundId.AntiAircraftShell4 ,unit.Position.X, unit.Position.Y);
				}

			Fires[n].Target=trgt;
			Fires[n].Kind=kind;
			Fires[n].Position=unit.Position;
			Fires[n].Direction=unit.Direction;
			Fires[n].Speed=16.0;
			Fires[n].Acceleration=-0.1;
			Fires[n].FinalSpeed=14.0;
			}
		}
	return false;
	return true;
	}

private bool PlaneFireBullet(ref int trgt, ref Unit unit, ref double drctn2, FireKind kind)
	{
	int n;
	double wrk_x;
	double wrk_y;
	double drctn;
	double dstc =default;
	trgt=0;
	for(n=1;n<=MaxUnitId;n++)
		{
		ref var other = ref Units[n];	//後方の敵を探す。
		if( other.IsUsed && other.Category==UnitCategory.Plane && other.PlaneState==UnitState.Flying && other.Side!=unit.Side && other.IsFound )
			{
			// 攻撃地点から攻撃目標地点への方位角
			wrk_x=other.Position.X-unit.Position.X;
			wrk_y=other.Position.Y-unit.Position.Y;
			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;

			drctn2=drctn+(Random(10)-5);
			if(drctn2>=360)	drctn2=drctn2-360;
			if(drctn2<0)	drctn2=360+drctn2;

			drctn=drctn-unit.Direction;
			if(drctn<0)
				drctn=360+drctn;
			if( (int)drctn>=150&&(int)drctn<=210 )
				{
				wrk_x=other.Position.X-unit.Position.X;
				wrk_y=other.Position.Y-unit.Position.Y;
				if(wrk_x==0)	wrk_x=1;
				if(wrk_y==0)	wrk_y=1;

				drctn=atan2(wrk_y,wrk_x)*RAD_to;
				if(drctn<0)
					drctn=360+drctn;
				if(wrk_x<0)
					wrk_x=0-wrk_x;
				if(wrk_y<0)
					wrk_y=0-wrk_y;
				if(drctn>=180)
					drctn=drctn-180;
				if(drctn>=90)
					drctn=90-(drctn-90);
				dstc=(wrk_x)/(cos(drctn*a_PI));
				if( dstc<=300 )
					{	trgt=n;	break;	}
				}
			}
		}
	if( trgt!=0 )
		{
		n=FindFreeFire();
		if( n!=0 )
			{
			//unit[m].arm[1]--;			// 弾薬消費
			PlaySoundEffect( 0, SoundId.AntiAircraftBullet3 ,unit.Position.X, unit.Position.Y);
			Fires[n].Target=trgt;
			Fires[n].Kind=kind;
			Fires[n].Position=unit.Position;
			Fires[n].Direction=drctn2;
			Fires[n].Speed=16.0;
			Fires[n].Acceleration=-0.1;
			Fires[n].FinalSpeed=14.0;
			}
		}
	return false;
	return true;
	}

private bool PlaneDropTorpedo(ref Unit unit, ref int trgt, ref double drctn2, int m, FireKind kind)
	{
	int cm_scrn_x;
	int cm_scrn_y;
	double trgt_x;
	double trgt_y;
	double wrk_x;
	double wrk_y;
	double drctn;
	double dstc =default;
	int i;
	int f;
	int n;
	if(!( unit.Position.Y>MAP_TOP || unit.Position.Y<MAP_BOTTOM || unit.Position.X<MAP_LEFT || unit.Position.X>MAP_RIGHT ))
		{
		cm_scrn_x=(int)((unit.Position.X+(Sprites[SpriteId.JapanUnits].wd/2)-MAP_LEFT)/Sprites[SpriteId.MapTiles].wd);
		cm_scrn_y=(int)((MAP_TOP-unit.Position.Y+(Sprites[SpriteId.JapanUnits].ht/2))/Sprites[SpriteId.MapTiles].ht);

		if( MapTiles[cm_scrn_y][cm_scrn_x]>=1
			|| MapTiles[cm_scrn_y-1][cm_scrn_x-1]>=1
			|| MapTiles[cm_scrn_y-1][cm_scrn_x]>=1
			|| MapTiles[cm_scrn_y-1][cm_scrn_x+1]>=1

			|| MapTiles[cm_scrn_y][cm_scrn_x-1]>=1
			|| MapTiles[cm_scrn_y][cm_scrn_x+1]>=1

			|| MapTiles[cm_scrn_y+1][cm_scrn_x-1]>=1
			|| MapTiles[cm_scrn_y+1][cm_scrn_x]>=1
			|| MapTiles[cm_scrn_y+1][cm_scrn_x+1]>=1
			)
			{
			return false;
			}
		}

		// 攻撃地点から攻撃目標地点への方位角
		if( !Units[trgt].IsFound )
	return false;

		trgt_x=Units[trgt].Position.X;
		trgt_y=Units[trgt].Position.Y;

		trgt_x+=cos(Units[trgt].Direction*a_PI)*((AIR_TPD_LOS_DSTC/AIR_TPD_SPD)*Units[trgt].Speed); // とりあえずターン後
		trgt_y+=sin(Units[trgt].Direction*a_PI)*((AIR_TPD_LOS_DSTC/AIR_TPD_SPD)*Units[trgt].Speed);

		wrk_x=trgt_x-unit.Position.X;
		wrk_y=trgt_y-unit.Position.Y;

		drctn=atan2(wrk_y,wrk_x)*RAD_to;
		if(drctn<0)
	drctn=360+drctn;

		drctn=drctn-unit.Direction;
		if(drctn<0)
	drctn=360+drctn;

		if( (int)drctn<=45||(int)drctn>=315)
	{
	// 距離を求めます
	wrk_x=unit.Position.X-trgt_x;
	wrk_y=unit.Position.Y-trgt_y;
	if(wrk_x==0)	wrk_x=1;
	if(wrk_y==0)	wrk_y=1;

	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;
	drctn2=drctn;					// 未来位置への絶対角
	if(wrk_x<0)
		wrk_x=0-wrk_x;
	if(wrk_y<0)
		wrk_y=0-wrk_y;
	if(drctn>=180)
		drctn=drctn-180;
	if(drctn>=90)
		drctn=90-(drctn-90);
	dstc=(wrk_x)/(cos(drctn*a_PI));

	if( dstc>=100 && dstc<=AIR_TPD_LOS_DSTC )
		{
		i=(int)(dstc/2);
		for(f=1;f<=i;f++)
			{
			wrk_x=unit.Position.X;
			wrk_y=unit.Position.Y;
			wrk_x+=cos(drctn2*a_PI)*(2*f); // とりあえずターン後
			wrk_y+=sin(drctn2*a_PI)*(2*f);

			if(!( wrk_y>MAP_TOP || wrk_y<MAP_BOTTOM || wrk_x<MAP_LEFT || wrk_x>MAP_RIGHT ))
				{
				cm_scrn_x=(int)((wrk_x+(Sprites[SpriteId.JapanUnits].wd/2)-MAP_LEFT)/Sprites[SpriteId.MapTiles].wd);
				cm_scrn_y=(int)((MAP_TOP-wrk_y+(Sprites[SpriteId.JapanUnits].ht/2))/Sprites[SpriteId.MapTiles].ht);
				if( MapTiles[cm_scrn_y][cm_scrn_x]>=1 )
					{
					return false;
					}
				}
			}

		// 発射！
		unit.Ammo=0;		// 魚雷が０
		unit.Target=0;		// ターゲットをクリア

		unit.Mode=UnitMode.Return;		// 航空機はメイン兵器ゼロで帰投

		// 雷撃時に適当に移動さす
		wrk_x=unit.Position.X;
		wrk_y=unit.Position.Y;
		drctn=unit.Direction;

		if(Random(2)==0)
			drctn+=(70-Random(40));
		else
			drctn-=(70-Random(40));

		drctn=(int)(drctn)%360;

		wrk_x+=cos(drctn*a_PI)*300;
		wrk_y+=sin(drctn*a_PI)*300;

		unit.EmergencyDestination = new WorldPosition(wrk_x, wrk_y);
		unit.EmergencyFlags[0]=20+Random(300);

		// 部下、多分戦闘機に帰投命令
		if(unit.IsGroupLeader!=0)
			{
			for(f=1;f<=MaxUnitId;f++)
				{
				ref var other = ref Units[f];
				if( other.IsUsed && other.GroupLeader==m && other.PlaneState==UnitState.Flying )
					{
					other.GroupLeader=0;
					other.Mode=UnitMode.Return;

other.PathX[0]=unit.Position.X;
other.PathY[0]=unit.Position.Y;
other.PathX[1]=MAP_RIGHT+1;

					}
				}

			unit.IsGroupLeader=0;
			}

		if( unit.IsGroupLeader==0 )
			{
			unit.PathX[0]=unit.Position.X;
			unit.PathY[0]=unit.Position.Y;
			unit.PathX[1]=MAP_RIGHT+1;
			}

		n=FindFreeFire();
		if( n!=0 )
			{
			PlaySoundEffect( 0, SoundId.Splash ,unit.Position.X, unit.Position.Y);
			Fires[n].Target=trgt;
			Fires[n].Kind=kind;
			Fires[n].Position=unit.Position;
			Fires[n].Direction=(int)unit.Direction;
			Fires[n].Speed=AIR_TPD_SPD;
			Fires[n].Acceleration=+0.0;
			Fires[n].FinalSpeed=0.0;
			Fires[n].Ticks=0;
			Fires[n].info[1]=240;
			Fires[n].ArmingTime=15;
			}
		}
	}
		return false;
	return true;
	}

private bool AttackerDropBomb(ref int trgt, ref Unit unit, ref double drctn2, int m, FireKind kind)
	{
	double wrk_x;
	double wrk_y;
	double wrk_x2;
	double wrk_y2;
	double drctn;
	double dstc =default;
	int f;
	int n;
	if( !Units[trgt].IsFound )
		return false;
	wrk_x=Units[trgt].Position.X;
	wrk_y=Units[trgt].Position.Y;
	wrk_x+=cos(Units[trgt].Direction*a_PI)*(Units[trgt].Speed*70.0);
	wrk_y+=sin(Units[trgt].Direction*a_PI)*(Units[trgt].Speed*70.0);

	wrk_x2=wrk_x;	wrk_y2=wrk_y;

	wrk_x=wrk_x-unit.Position.X;
	wrk_y=wrk_y-unit.Position.Y;

	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;
	drctn2=drctn;
	drctn=drctn-unit.Direction;
	if(drctn<0)
		drctn=360+drctn;

	if( (int)drctn<=30||(int)drctn>=330 )
		{
		// 距離を求めます
		wrk_x=unit.Position.X-wrk_x2;
		wrk_y=unit.Position.Y-wrk_y2;
		if(wrk_x==0)	wrk_x=1;
		if(wrk_y==0)	wrk_y=1;

		drctn=atan2(wrk_y,wrk_x)*RAD_to;
		if(drctn<0)
			drctn=360+drctn;
		if(wrk_x<0)
			wrk_x=0-wrk_x;
		if(wrk_y<0)
			wrk_y=0-wrk_y;
		if(drctn>=180)
			drctn=drctn-180;
		if(drctn>=90)
			drctn=90-(drctn-90);
		dstc=(wrk_x)/(cos(drctn*a_PI));

		if( ( dstc>=170 && dstc<=180 && unit.Side==Side.UnitedStates ) || ( dstc>=35 && dstc<=65 && unit.Side==Side.Japan ))
			{
			// 発射！

			if( unit.Side==Side.UnitedStates )
				unit.Speed+=unit.AccelerationChange*700;
			else
				{
				wrk_x=unit.Position.X;
				wrk_y=unit.Position.Y;
				drctn=unit.Direction;
				wrk_x+=cos(drctn*a_PI)*300;
				wrk_y+=sin(drctn*a_PI)*300;
				unit.EmergencyDestination = new WorldPosition(wrk_x, wrk_y);
				unit.EmergencyFlags[0]=150+Random(50);
				}

			unit.Ammo=0;		// 消費
			unit.Target=0;

			unit.Mode=UnitMode.Return;		// 航空機はメイン兵器ゼロで帰投

			// 部下、多分戦闘機に帰投命令
			if(unit.IsGroupLeader!=0)
				{
				for(f=1;f<=MaxUnitId;f++)
					{
					ref var other = ref Units[f];
					if( other.IsUsed && other.GroupLeader==m && other.PlaneState==UnitState.Flying)
						{
						other.GroupLeader=0;
						other.Mode=UnitMode.Return;

other.PathX[0]=unit.Position.X;
other.PathY[0]=unit.Position.Y;
other.PathX[1]=MAP_RIGHT+1;

						}
					}
				unit.IsGroupLeader=0;
				}

			n=FindFreeFire();
			if( n!=0 )
				{
				Fires[n].Target=(int)UnitKind.Attacker;
				Fires[n].Kind=kind;
				Fires[n].Position = new WorldPosition(unit.Position.X+(3-Random(6)), unit.Position.Y+(3-Random(6)));
				Fires[n].Direction=drctn2;

				if(unit.Side==Side.Japan)
					{
					Fires[n].Position += new WorldVector(cos(Fires[n].Direction*a_PI)*(13), sin(Fires[n].Direction*a_PI)*(13));
					}
				else
					{
					Fires[n].Position += new WorldVector(cos(Fires[n].Direction*a_PI)*(130), sin(Fires[n].Direction*a_PI)*(130));
					}

				Fires[n].Speed=0.3;
				Fires[n].Acceleration=+0.2;
				Fires[n].FinalSpeed=0.0;
				if(unit.Side==Side.Japan)
					{
					Fires[n].Ticks=10;
					Fires[n].info[1]=68+Random(5);
					}
				else
					{
					Fires[n].Ticks=0;
					Fires[n].info[1]=70;
					}

				}

				// もう一発
				n=FindFreeFire();
				if( n!=0 )
					{
					Fires[n].Target=trgt;
					Fires[n].Kind=kind;
					Fires[n].Position = new WorldPosition(unit.Position.X+(20-Random(40)), unit.Position.Y+(20-Random(40)));
					Fires[n].Direction=drctn2;

					if(unit.Side==Side.Japan)
						{
						Fires[n].Position += new WorldVector(cos(Fires[n].Direction*a_PI)*(13), sin(Fires[n].Direction*a_PI)*(13));
						}
					else
						{
						Fires[n].Position += new WorldVector(cos(Fires[n].Direction*a_PI)*(130), sin(Fires[n].Direction*a_PI)*(130));
						}

					Fires[n].Speed=0.3;
					Fires[n].Acceleration=+0.2;
					Fires[n].FinalSpeed=0.0;

					if(unit.Side==Side.Japan)
						{
						Fires[n].Ticks=10;
						Fires[n].info[1]=75+(5-Random(10));
						}
					else
						{
						Fires[n].Ticks=0;
						Fires[n].info[1]=70+(5-Random(10));
						}
					}
			}
		}
	return false;
	return true;
	}

private bool BomberDropBomb(ref int trgt, ref Unit unit, ref double drctn2, FireKind kind)
	{
	int trgt2;
	int n;
	double wrk_x;
	double wrk_y;
	double drctn;
	double dstc =default;
	trgt2=0;
	if(trgt!=0)
		trgt2=trgt;
	for(n=1;n<=MaxUnitId;n++)
		{	//前方の敵を探す。
		if(trgt2!=0)
			{ n=trgt2; trgt=0; }
		if( Units[n].IsUsed && Units[n].Category==UnitCategory.Ship && Units[n].Side!=unit.Side && Units[n].IsFound )
			{
			// 攻撃地点から攻撃目標地点への方位角
			wrk_x=Units[n].Position.X-unit.Position.X;
			wrk_y=Units[n].Position.Y-unit.Position.Y;
			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			drctn2=drctn;

			drctn2=drctn+(Random(10)-5);
			if(drctn2>=360)	drctn2=drctn2-360;
			if(drctn2<0)	drctn2=360+drctn2;

			drctn=drctn-unit.Direction;
			if(drctn<0)
				drctn=360+drctn;
			if( (int)drctn<=30 || (int)drctn>=330 )
				{
				wrk_x=Units[n].Position.X-unit.Position.X;
				wrk_y=Units[n].Position.Y-unit.Position.Y;
				if(wrk_x==0)	wrk_x=1;
				if(wrk_y==0)	wrk_y=1;

				drctn=atan2(wrk_y,wrk_x)*RAD_to;
				if(drctn<0)
					drctn=360+drctn;
				if(wrk_x<0)
					wrk_x=0-wrk_x;
				if(wrk_y<0)
					wrk_y=0-wrk_y;
				if(drctn>=180)
					drctn=drctn-180;
				if(drctn>=90)
					drctn=90-(drctn-90);
				dstc=(wrk_x)/(cos(drctn*a_PI));
				if( dstc>=30 && dstc<=60 )
					{	trgt=n;	break;	}
				}
			}
		if( trgt2!=0 )
			break;
		}
	if( trgt!=0 )
		{
		n=FindFreeFire();
		if( n!=0 )
			{
			PlaySoundEffect( 0, SoundId.BomberBombRelease ,unit.Position.X, unit.Position.Y);
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

			Fires[n].Target=(int)UnitKind.Bomber;
			Fires[n].Kind=kind;
			Fires[n].Position = new WorldPosition(unit.Position.X+((double)(-6+Random(13))), unit.Position.Y+((double)(-6+Random(13))));
			Fires[n].Direction=drctn2;
			Fires[n].Speed=0.3;
			Fires[n].Acceleration=+0.2;
			Fires[n].FinalSpeed=0.0;
			Fires[n].Ticks=0;
			Fires[n].info[1]=70;
			}
		}

	return false;
	return true;
	}

private bool FirePlaneWeapons(FireKind kind, ref Unit unit, ref int trgt, ref double drctn2, int m)
	{

	double dstc =default;

	if( kind==FireKind.Bullet && unit.Kind==UnitKind.Bomber )
		{
		// 航空機の全方向対空機銃
		if( !BomberFireBullet(ref trgt, ref unit, ref drctn2, kind) )
			return false;
		}

	if( kind==FireKind.Bullet && unit.Kind==UnitKind.Fighter )
		{
		// 戦闘機
		// 前方固定銃
		if( !FighterFireBullet(ref trgt, ref unit, kind) )
			return false;
		}

	if( kind==FireKind.Bullet )
		{
		if( !PlaneFireBullet(ref trgt, ref unit, ref drctn2, kind) )
			return false;
		}

		if( kind==FireKind.Torpedo && !(Units[trgt].Kind>=UnitKind.AirBase && Units[trgt].Kind<=UnitKind.Fortress) )
			{
			// 攻撃機
			// トゥピード

			//地上の上なら投雷しない。

			if( !PlaneDropTorpedo(ref unit, ref trgt, ref drctn2, m, kind) )
				return false;
		}

	if( kind==FireKind.Bomb && unit.Kind==UnitKind.Attacker )
		{
		// 攻撃機
		// 爆撃
		// 攻撃地点から攻撃目標地点への方位角
		if( !AttackerDropBomb(ref trgt, ref unit, ref drctn2, m, kind) )
			return false;
		}

	if( kind==FireKind.Bomb && unit.Kind==UnitKind.Bomber )
		{
		// 爆撃機
		// 爆撃
		if( !BomberDropBomb(ref trgt, ref unit, ref drctn2, kind) )
			return false;
		}
	return true;
	}

private bool FireShipWeapons(FireKind kind, ref Unit unit, int m, ref double drctn2, ref double dstc, ref int trgt, ref int rng, ref Array3<int> fc, ref double drctn3)
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
		if( !ShipFireRapidAntiAircraftShell(ref unit, ref drctn2, ref dstc, kind) )
			return false;
		}

	if( kind==FireKind.AntiSubmarineBomb && unit.Speed>=unit.MaxSpeed )
		{
		if( !ShipDropAntiSubmarineBomb(ref trgt, ref unit, kind) )
			return false;
		}

	if( ( kind==FireKind.Gun || kind==FireKind.NavalBaseGun ) && trgt==0)	// ターゲットが選択されていない砲撃、
		{
		// 艦砲、自動射撃
		if( !ShipFireGunWithoutTarget(ref unit, kind, ref rng, ref fc, ref drctn2, ref drctn3) )
			return false;
		}

	// 選択でない自動の魚雷、主に駆逐艦
	if( kind==FireKind.Torpedo && trgt==0)
		{
		//
		if( !ShipFireTorpedoWithoutTarget(ref unit, ref drctn2, kind) )
			return false;
		}

	// 選択された敵への魚雷、主に、潜水艦
	if( kind==FireKind.Torpedo && trgt!=0)
		{
		//

		if( !ShipFireTorpedoAtTarget(trgt, ref unit, ref drctn2, kind) )
			return false;
		}

	if( kind==FireKind.AntiAircraftShell && trgt!=0 && Units[trgt].Category==UnitCategory.Plane )	// ターゲットが選択された対空砲
		{
		//	指定射撃
		if( !ShipFireAntiAircraftShellAtTarget(trgt, ref unit, ref rng, ref fc) )
			return false;
		}

	if( kind==FireKind.AntiAircraftShell && trgt==0 )
		{
		// 自動の対空砲 Anti Air Shell
		if( !ShipFireAntiAircraftShellWithoutTarget(ref unit, ref drctn2, ref drctn3, ref dstc, ref rng, ref fc, kind) )
			return false;
		}

	if( ( kind==FireKind.Gun || kind==FireKind.NavalBaseGun ) && trgt!=0 && Units[trgt].Category==UnitCategory.Ship )	// ターゲットが選択された砲撃
		{
		// 艦砲		指定射撃
		if( !ShipFireGunAtTarget(trgt, ref unit, kind, ref rng, ref fc) )
			return false;
		}

	if( kind==FireKind.Bullet )
		{
		// 艦船の対空機銃
		if( !ShipFireBullet(ref trgt, ref unit, ref drctn2, kind) )
			return false;
		}
	return true;
	}

//============================================================================
// 射撃します。
//----------------------------------------------------------------------------
[Original("fire_now")]
public void	FireWeapons(int m,int trgt,FireKind kind)
	{
	ref var unit = ref Units[m];
	int	rng=default /* C4701 */,s; Array3<int> fc = default;
	double		drctn2=default /* C4701 */,drctn3=default /* C4701 */,dstc=default /* C4701 */,dstc3;

	if( (unit.Kind==UnitKind.NavalBase || unit.Kind==UnitKind.AirBase || unit.Kind==UnitKind.InfantryBase || unit.Kind==UnitKind.Pillboxes || unit.Kind==UnitKind.Fortress) && unit.BuildTime!=0	)
		{
		//工事中
		return;
		}

	if( unit.Category==UnitCategory.Ship )
		{
		//=========		 艦船の射撃制御		=========//
		if( !FireShipWeapons(kind, ref unit, m, ref drctn2, ref dstc, ref trgt, ref rng, ref fc, ref drctn3) )
			return;
		}

	if( unit.Category==UnitCategory.Plane )
		{
				//=========		 航空機の射撃制御		=========//
		if( !FirePlaneWeapons(kind, ref unit, ref trgt, ref drctn2, m) )
			return;

		}
	}

//============================================================================
// 編隊のポジションをＰｐ＿ｘｙ「０」にセットします。
//----------------------------------------------------------------------------
[Original("set_pos_of_dynmc")]
public void	SetDynamicDestination(int n)
	{
	double			angl=default /* C4701 */,dstc=default /* C4701 */;
	int				pt,pos_of_no,a,b,c;
	int				nums;

	if( Units[n].Category==UnitCategory.Plane )
		{

		// 航空機編隊の制御

		pt=Units[n].GroupLeader;

		if(Units[pt].PlaneState==UnitState.Parked && Units[pt].Mode==UnitMode.Return && Units[n].PlaneState==UnitState.Flying )
			{
			Units[n].GroupLeader=0;
			return;
			}

		pos_of_no=Units[n].FormationNumber;

		if(Units[pt].Category==UnitCategory.Ship)	// こっちは飛行機だが指揮が艦船の場合
			{
			angl=(double)Random(359);
			dstc=(double)Random(500+150);
			Units[n].Mode=UnitMode.Move;

			// 目的地を決定
			Units[n].PathX[0]=Units[pt].Position.X+cos(angl*a_PI)*dstc;
			Units[n].PathY[0]=Units[pt].Position.Y+sin(angl*a_PI)*dstc;
			Units[n].PathX[1]=MAP_RIGHT+1;
			}
		else
			{
			// 指揮が通常移動
			nums=5;			// １小隊何機か

			// 何番編隊か
			a=Units[n].FormationNumber/nums;
			// 何番機か
			b=Units[n].FormationNumber%nums;

			switch( b )
				{
				case 0:		// １番機
					angl=0;	dstc=0;
					break;
				case 1:		// 2番機
					angl=Units[pt].Direction-90.0-45.0;	dstc=60*0.80;
					break;
				case 2:		//
					angl=Units[pt].Direction-90.0-45.0-90.0;	dstc=60*0.80;
					break;
				case 3:		//
					angl=Units[pt].Direction-90.0-45.0;	dstc=120*0.80;
					break;
				case 4:		//
					angl=Units[pt].Direction-90.0-45.0-90.0;	dstc=120*0.80;
					break;
				case 5:		//
					angl=Units[pt].Direction-90.0-45.0-45.0;	dstc=100*0.80;
					break;
				}

			// 目的地を決定
			Units[n].PathX[0]=Units[pt].Position.X+cos(angl*a_PI)*dstc;
			Units[n].PathY[0]=Units[pt].Position.Y+sin(angl*a_PI)*dstc;

			if( a>=0 )
				{
				// 各編隊随伴指揮機位置
				switch( a )
					{
					case 0:		// 1番編隊
						angl=Units[pt].Direction;	dstc=35;
						break;
					case 1:		// 2番編隊
						angl=Units[pt].Direction-90.0-45.0;	dstc=180*0.80;
						break;
					case 2:		//
						angl=Units[pt].Direction-90.0-45.0-90.0;	dstc=180*0.80;
						break;
					case 3:		//
						angl=Units[pt].Direction-90.0-45.0;	dstc=360*0.80;
						break;
					case 4:		//
						angl=Units[pt].Direction-90.0-45.0-45.0;	dstc=300*0.80;
						break;
					case 5:		//
						angl=Units[pt].Direction-90.0-45.0-90.0;	dstc=360*0.80;
						break;

					case 6:		//
						angl=Units[pt].Direction-90.0-45.0;	dstc=540*0.80;
						break;
					case 7:		//
						angl=Units[pt].Direction-90.0-45.0-22.5;	dstc=480*0.80;
						break;
					case 8:		//
						angl=Units[pt].Direction-90.0-45.0-45.0-22.5;	dstc=480*0.80;
						break;
					case 9:		//
						angl=Units[pt].Direction-90.0-45.0-90.0;		dstc=540*0.80;
						break;
					case 10:		//
						angl=Units[pt].Direction-90.0-45.0-11.2;		dstc=640*0.80;
						break;
					case 11:		//
						angl=Units[pt].Direction-90.0-45.0-90.0+11.2;		dstc=640*0.80;
						break;
					}

				// 目的地を決定
				Units[n].PathX[0]=Units[n].PathX[0]+cos(angl*a_PI)*dstc;
				Units[n].PathY[0]=Units[n].PathY[0]+sin(angl*a_PI)*dstc;
				Units[n].PathX[1]=MAP_RIGHT+1;
				}
			}
		}
	else
		{
		// 艦隊制御
		pt=Units[n].GroupLeader;
		angl=Units[n].DirectionToLeader+Units[pt].Direction;
		if( angl>=360 )
			angl = angl-360;
		dstc=Units[n].DistanceToLeader;

		Units[n].PathX[0]=Units[pt].Position.X+cos(angl*a_PI)*dstc;
		Units[n].PathY[0]=Units[pt].Position.Y+sin(angl*a_PI)*dstc;
		Units[n].PathX[1]=MAP_RIGHT+1;

		if( 1!=0  )
			{
			//コンピュータの進路計算
			SetCpuRoute2( n );
			}
		}

	Units[n].IsStopping=false;

	}

//============================================================================
// 戦闘機の緊急起動をセット
//----------------------------------------------------------------------------
[Original("chk_another_unit")]
public void	CheckOtherUnits(double* rx,double* ry)
	{
	int		n;
	double	wrk_x,wrk_y;
	RECT	wrk_r;

	for( n=1; n<=MaxUnitId; n++)
		{
		if( Units[n].IsUsed && Units[n].Category==UnitCategory.Ship )
			{
			// ptin dbg
			wrk_r.top=(int)Units[n].Position.Y+(Sprites[SpriteId.JapanUnits].ht/2);
			wrk_r.right=(int)Units[n].Position.X+(Sprites[SpriteId.JapanUnits].wd/2);
			wrk_r.bottom=(int)Units[n].Position.Y-(Sprites[SpriteId.JapanUnits].ht/2);
			wrk_r.left=(int)Units[n].Position.X-(Sprites[SpriteId.JapanUnits].wd/2);

			if( PointInRect3(ref wrk_r,(int)*rx,(int)*ry)!=0)
				{
				n=0;
				*ry-=80;
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
			unit.PathX[0]=unit.Position.X;
			unit.PathY[0]=unit.Position.Y;
			unit.PathX[1]=MAP_RIGHT+1;
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
	double			angl,dstc,wrk_x,wrk_y,drctn,drctn2,em_drctn;
	int				trgt,pos_of_no,a,b,c;
	int				lvl_my,lvl_en,my_tec=default /* C4701 */,en_tec,n;
	RECT			wrk_r;

	// ptin dbg
	wrk_r.top=(int)unit.EmergencyDestination.Y+35;
	wrk_r.right=(int)unit.EmergencyDestination.X+35;
	wrk_r.bottom=(int)unit.EmergencyDestination.Y-35;
	wrk_r.left=(int)unit.EmergencyDestination.X-35;

	if( PointInRect3(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)!=0 )
		{
		unit.EmergencyDestination = new WorldPosition(unit.Position.X+cos(unit.Direction*a_PI)*(100+Random(50)), unit.Position.Y+sin(unit.Direction*a_PI)*(100+Random(50)));
		unit.EmergencyFlags[0]=100;
		unit.IsStopping=false;
		return;
		}

	trgt=unit.Target;
	lvl_my=unit.Skill;
	lvl_en=Units[trgt].Skill;

	if( lvl_my == lvl_en )
		{
		if( ((Tick)%600)<300 )
			{lvl_my++;}
		else
			{lvl_en++;}
		}
	if( lvl_my > lvl_en )
		{	my_tec=10;	en_tec=40+(lvl_my-lvl_en);	}
	if( lvl_my < lvl_en )
		{	my_tec=40+(lvl_en-lvl_my);	en_tec=10;	}

	if( Random(my_tec)!=0 )
		return;

	// 正面打ち合いをさけるようにします。
	n=trgt;
	// 自機ｍと敵機ｎの絶対角を調べます。
	wrk_x=unit.Position.X-Units[n].Position.X;
	wrk_y=unit.Position.Y-Units[n].Position.Y;
	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;					// drctnが絶対角

	drctn=drctn-Units[n].Direction;				// 敵機ｎからの方位角をしらべます。
	if(drctn<0)
		drctn=360+drctn;
	if( (int)drctn<=5 || (int)drctn>=355 )
		{
		// 敵機が正面に自機を捕らえています。
		wrk_x=Units[n].Position.X-unit.Position.X;
		wrk_y=Units[n].Position.Y-unit.Position.Y;
		drctn=atan2(wrk_y,wrk_x)*RAD_to;
		if(drctn<0)
			drctn=360+drctn;					// drctnが絶対角

		drctn=drctn-unit.Direction;				// 自機ｍからの方位角をしらべます。
		if(drctn<0)
			drctn=360+drctn;

		if( (int)drctn<=5 || (int)drctn>=355 )
			{
			// 自機も敵機を正面に捕らえています
			wrk_x=Units[n].Position.X-unit.Position.X;
			wrk_y=Units[n].Position.Y-unit.Position.Y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			dstc=(wrk_x)/(cos(drctn*a_PI));
			if( dstc>=160 && dstc<=320 )
				{
				// さらに、距離が近い よけよう
				wrk_x=unit.Position.X;
				wrk_y=unit.Position.Y;
				em_drctn=unit.Direction;
				switch( Random(2) )
					{
					case 0:
						em_drctn+=45+Random(45);
						break;
					case 1:
						em_drctn-=45+Random(45);
						break;
					}
				em_drctn=(int)em_drctn%360;

				wrk_x+=cos(em_drctn*a_PI)*300;
				wrk_y+=sin(em_drctn*a_PI)*300;

				unit.EmergencyDestination = new WorldPosition(wrk_x, wrk_y);
				unit.EmergencyFlags[0]=70+Random(40);

				return;
				}
			}
		}

	// 敵機の直前にＥｍ＿Ｘｙを設定します。
	wrk_x=Units[trgt].Position.X;
	wrk_y=Units[trgt].Position.Y;
	wrk_x+=cos(Units[trgt].Direction*a_PI)*(Units[trgt].Speed*20.0);
	wrk_y+=sin(Units[trgt].Direction*a_PI)*(Units[trgt].Speed*20.0);

	unit.EmergencyDestination = new WorldPosition(wrk_x, wrk_y);
	unit.EmergencyFlags[0]=70+Random(40);
	unit.IsStopping=false;
	if(Random(10)==0)
		PlaySoundEffect( 0, SoundId.Plane1+Random(2) ,unit.Position.X, unit.Position.Y);

	}

//============================================================================
// 攻撃機の緊急起動をセット
//----------------------------------------------------------------------------
[Original("set_pos_of_emrgncy_AT")]
public void	SetAttackerEmergencyDestination(int m)
	{
	ref var unit = ref Units[m];
	int		n,g,new_ldr,f;
	double	em_drctn,wrk_x,wrk_y,drctn,dstc,drctn2;

	// 戦闘機から逃げよう
	for(n=1;n<=MaxUnitId;n++)
		{
		ref var other = ref Units[n];	//後方の敵を探す。
		if( other.IsUsed && other.Category==UnitCategory.Plane && other.PlaneState==UnitState.Flying
		&& other.Side!=unit.Side && other.IsFound )
			{

			// 攻撃地点から攻撃目標地点への方位角
			wrk_x=other.Position.X-unit.Position.X;
			wrk_y=other.Position.Y-unit.Position.Y;
			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;

			drctn=drctn-unit.Direction;
			if(drctn<0)
				drctn=360+drctn;

			// 攻撃目標地点から攻撃地点への方位角
			wrk_x=unit.Position.X-other.Position.X;
			wrk_y=unit.Position.Y-other.Position.Y;
			drctn2=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn2<0)
				drctn2=360+drctn2;

			drctn2=drctn2-other.Direction;
			if(drctn2<0)
				drctn2=360+drctn2;

			if( ((int)drctn>=150 && (int)drctn<=210 && other.Kind==UnitKind.Fighter)
				||
				( ((int)drctn<=45 || (int)drctn>=315) && ( (int)drctn2>=135 && (int)drctn2<=225)  && other.Kind==UnitKind.Attacker && unit.Target==0 )
			  )
				{

				wrk_x=other.Position.X-unit.Position.X;
				wrk_y=other.Position.Y-unit.Position.Y;
				if(wrk_x==0)	wrk_x=1;
				if(wrk_y==0)	wrk_y=1;

				drctn=atan2(wrk_y,wrk_x)*RAD_to;
				if(drctn<0)
					drctn=360+drctn;
				if(wrk_x<0)
					wrk_x=0-wrk_x;
				if(wrk_y<0)
					wrk_y=0-wrk_y;
				if(drctn>=180)
					drctn=drctn-180;
				if(drctn>=90)
					drctn=90-(drctn-90);
				dstc=(wrk_x)/(cos(drctn*a_PI));
				if( dstc<=250+((other.Kind==UnitKind.Attacker ? 1 : 0)*70) )
					{

					wrk_x=unit.Position.X;
					wrk_y=unit.Position.Y;
					em_drctn=unit.Direction;
					switch(Random(2))
						{
						case 0:
							em_drctn+=45+Random(90);
							break;
						case 1:
							em_drctn-=45+Random(90);
							break;
						}
					em_drctn=(int)em_drctn%360;

					wrk_x+=cos(em_drctn*a_PI)*300;
					wrk_y+=sin(em_drctn*a_PI)*300;

					unit.EmergencyDestination = new WorldPosition(wrk_x, wrk_y);
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
			unit.PathX[0]=unit.Position.X;
			unit.PathY[0]=unit.Position.Y;
			unit.PathX[1]=MAP_RIGHT+1;
			}
		else
			{

			n=MaxUnitId+1;
			g=1;
			new_ldr=0;

			for(f=1;f<=MaxUnitId;f++)
				{
				ref var other = ref Units[f];
				if( other.IsUsed && other.GroupLeader==m )
					{
					g++;
					if( other.FormationNumber < n )
						{
						n=other.FormationNumber;
						new_ldr=f;					// これが新しい隊長番号
						}
					}
				}

			if(new_ldr!=0 && g>=2 )
				{
				Units[new_ldr].IsGroupLeader=(short)g;
				Units[new_ldr].GroupLeader=0;
				Units[new_ldr].FormationNumber=0;

				// 昔の小隊長が攻爆撃機だったら、帰投にしておく
				if( Units[new_ldr].Kind==UnitKind.Fighter && (unit.Kind==UnitKind.Attacker || unit.Kind==UnitKind.Bomber)  && Units[new_ldr].PlaneState==UnitState.Flying )
					{
					Units[new_ldr].Mode=UnitMode.Return;		// それまでの隊長がボスだったらきかんしよっと
					}

				for(f=1;f<=MaxUnitId;f++)
					{
					ref var other = ref Units[f];
					if( other.IsUsed && other.GroupLeader==m )
						{
						other.GroupLeader=(short)new_ldr;

						// 昔の小隊長が攻爆撃機だったら、帰投にしておく
						if( Units[new_ldr].Kind==UnitKind.Fighter && other.Kind==UnitKind.Fighter && (unit.Kind==UnitKind.Attacker || unit.Kind==UnitKind.Bomber) && other.PlaneState==UnitState.Flying )
							other.Mode=UnitMode.Return;		// それまでの隊長がボスだったらきかんしよっと
						}

					}

				for(f=0;f<64;f++)
					{
					Units[new_ldr].PathX[f]=unit.PathX[f];
					Units[new_ldr].PathY[f]=unit.PathY[f];
					}

				// 昔の小隊長
				unit.PathX[0]=unit.Position.X;
				unit.PathY[0]=unit.Position.Y;
				unit.PathX[1]=MAP_RIGHT+1;

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
	double			angl,dstc,wrk_x,wrk_y,drctn,drctn2,drctn3,turn;
	int				trgt,pos_of_no,a,b,c,i;
	int				nums,lvl_jp,lvl_us,jp_tec,us_tec,n,f;
	RECT			wrk_r;

	n=unit.Target;				// 攻撃目標

	// 現位置から攻撃目標地点への距離
	wrk_x=Units[n].Position.X-unit.Position.X;
	wrk_y=Units[n].Position.Y-unit.Position.Y;
	if(wrk_x==0)	wrk_x=1;
	if(wrk_y==0)	wrk_y=1;

	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;
	if(wrk_x<0)
		wrk_x=0-wrk_x;
	if(wrk_y<0)
		wrk_y=0-wrk_y;
	if(drctn>=180)
		drctn=drctn-180;
	if(drctn>=90)
		drctn=90-(drctn-90);
	dstc=(wrk_x)/(cos(drctn*a_PI));

	// 目標ユニットへの方位角を求めます
	wrk_x=Units[n].Position.X;
	wrk_y=Units[n].Position.Y;
	wrk_x=wrk_x-unit.Position.X;
	wrk_y=wrk_y-unit.Position.Y;
	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;
	drctn=drctn-unit.Direction;
	if(drctn<0)
		drctn=360+drctn;

	if( dstc<=510 && dstc >= 500  && (drctn<=22.5||drctn>=337.5)   )
		{

		// リーダー機か単独機のみここに来ます。

		f=0;
		for(i=1;i<=MaxUnitId;i++)
			{
			ref var other = ref Units[i];
			if( i!=m && other.IsUsed && other.GroupLeader==m && other.Kind==UnitKind.Attacker )
				{
				f++;

				if( other.Kind!=UnitKind.Fighter )
					other.GroupLeader=0;

				wrk_x=other.Position.X;
				wrk_y=other.Position.Y;
				drctn=other.Direction;

				switch( other.FormationNumber%5 )
					{
					case 1:
						drctn+=315+22.5;
						break;
					case 2:
						drctn+=22.5;
						break;
					case 3:
						drctn+=315;
						break;
					case 4:
						drctn+=45;
						break;
					}
				drctn=(int)drctn%360;

				wrk_x+=cos(drctn*a_PI)*600;
				wrk_y+=sin(drctn*a_PI)*600;

				other.EmergencyDestination = new WorldPosition(wrk_x, wrk_y);
				other.EmergencyFlags[0]=10+((1+(other.FormationNumber%5))*20);

				other.PathX[0]=Units[n].Position.X;
				other.PathY[0]=Units[n].Position.Y;
				other.PathX[1]=MAP_RIGHT+1;

				}
			}

		}
	else if( dstc >= 400 )
		{
		if( unit.PathX[1]==MAP_RIGHT+1 )
			{
			// ptin dbg
			wrk_r.top=(int)unit.PathY[0]+35;
			wrk_r.right=(int)unit.PathX[0]+35;
			wrk_r.bottom=(int)unit.PathY[0]-35;
			wrk_r.left=(int)unit.PathX[0]-35;
			if( PointInRect3(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)==0 || !Units[n].IsFound)
				{
				return;
				}
			wrk_x=Units[n].Position.X;
			wrk_y=Units[n].Position.Y;
			wrk_x+=cos(Units[n].Direction*a_PI); // とりあえずターン後
			wrk_y+=sin(Units[n].Direction*a_PI);
			unit.PathX[0]=wrk_x;
			unit.PathY[0]=wrk_y;
			unit.PathX[1]=MAP_RIGHT+1;
			unit.IsStopping=false;
			}
		}
	else if ( dstc >= 40 && (drctn<=45||drctn>=315))
		{
		if( unit.PathX[1]==MAP_RIGHT+1 )
			{
			wrk_r.top=(int)unit.PathY[0]-35;
			wrk_r.right=(int)unit.PathX[0]+35;
			wrk_r.bottom=(int)unit.PathY[0]+35;
			wrk_r.left=(int)unit.PathX[0]-35;
			if(unit.Weapon==FireKind.Torpedo)
				turn=( (AIR_TPD_LOS_DSTC) /AIR_TPD_SPD);		// 投雷距離　÷ 魚雷速度　でターンを求めます
			else
				turn=70.0;
			wrk_x=Units[n].Position.X;
			wrk_y=Units[n].Position.Y;
			wrk_x+=cos(Units[n].Direction*a_PI)*(Units[n].Speed*turn); // とりあえずターン後
			wrk_y+=sin(Units[n].Direction*a_PI)*(Units[n].Speed*turn);
			unit.PathX[0]=wrk_x;
			unit.PathY[0]=wrk_y;
			unit.PathX[1]=MAP_RIGHT+1;
			unit.IsStopping=false;
			if( unit.IsGroupLeader!=0  )
				{
				a=0;
				for(f=1;f<=MaxUnitId;f++)
					{
 					ref var other = ref Units[f];
 					if( other.IsUsed && other.Kind==UnitKind.Fighter && other.GroupLeader==m)
						{	a++;	}
					else
						{
						if( other.IsUsed && other.GroupLeader==m )
							other.GroupLeader=0;
						}
					}
				unit.IsGroupLeader=(short)a;
				}
			}
		}
	else
		{	// 近すぎる場合は離脱
		// ptin dbg
		wrk_r.top=(int)unit.PathY[0]+35;
		wrk_r.right=(int)unit.PathX[0]+35;
		wrk_r.bottom=(int)unit.PathY[0]-35;
		wrk_r.left=(int)unit.PathX[0]-35;
		if( PointInRect3(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)!=0 )
			{
			unit.PathX[0]=unit.Position.X+cos(unit.Direction*a_PI)*(300);
			unit.PathY[0]=unit.Position.Y+sin(unit.Direction*a_PI)*(300);
			unit.PathX[1]=MAP_RIGHT+1;
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
	double			angl,dstc,wrk_x,wrk_y,drctn,drctn2,drctn3,turn;
	int				trgt,pos_of_no,a,b,c;
	int				nums,lvl_jp,lvl_us,jp_tec,us_tec,n,f;
	RECT			wrk_r;

	if( Random(200)!=0 )
		return;

	// 現位置から攻撃目標地点への距離
	wrk_x=Units[m].LandingX-Units[m].Position.X;
	wrk_y=Units[m].LandingY-Units[m].Position.Y;
	if(wrk_x==0)	wrk_x=1;
	if(wrk_y==0)	wrk_y=1;

	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;
	if(wrk_x<0)
		wrk_x=0-wrk_x;
	if(wrk_y<0)
		wrk_y=0-wrk_y;
	if(drctn>=180)
		drctn=drctn-180;
	if(drctn>=90)
		drctn=90-(drctn-90);
	dstc=(wrk_x)/(cos(drctn*a_PI));

	if(dstc<=400)
		{
		Units[m].EmergencyDestination = new WorldPosition(Units[m].LandingX, Units[m].LandingY);
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
		if( unit.Position.X>MAP_RIGHT )
			{
			unit.EmergencyDestination = new WorldPosition(MAP_RIGHT-40, unit.Position.Y-100+Random(200));
			unit.EmergencyFlags[0]=50+Random(200);
			return;
			}

		if( unit.Position.X<MAP_LEFT )
			{
			unit.EmergencyDestination = new WorldPosition(MAP_LEFT+40, unit.Position.Y-100+Random(200));
			unit.EmergencyFlags[0]=50+Random(200);
			return;
			}

		if( unit.Position.Y>MAP_TOP )
			{
			unit.EmergencyDestination = new WorldPosition(unit.Position.X-100+Random(200), MAP_TOP-40);
			unit.EmergencyFlags[0]=50+Random(200);
			return;
			}

		if( unit.Position.Y<MAP_BOTTOM )
			{
			unit.EmergencyDestination = new WorldPosition(unit.Position.X-100+Random(200), MAP_BOTTOM+40);
			unit.EmergencyFlags[0]=50+Random(200);
			return;
			}
		}
	}

//============================================================================
// 艦船の緊急機動をＥｍ＿ｘｙにセット
//----------------------------------------------------------------------------
[Original("set_pos_of_emrgncy_SHIP")]
public void	SetShipEmergencyDestination(int m)
	{
	ref var unit = ref Units[m];
	int		n;
	double	em_drctn,wrk_x,wrk_y,drctn,dstc;
	int		size;

	for(n=1;n<FIRE_MAX;n++)
		{
		ref var fire = ref Fires[n];
		// 艦船によってくる魚雷から逃げる
		if( fire.Target!=0 && fire.Kind==FireKind.Torpedo && fire.Ticks>=fire.ArmingTime  )
			{
			// 自点と対象点の距離
			wrk_x=fire.Position.X-unit.Position.X;
			wrk_y=fire.Position.Y-unit.Position.Y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			dstc=(wrk_x)/(cos(drctn*a_PI));		// 距離

			if( dstc<=600 && dstc>=40 )
				{
				// 対象ユニットからの自点への方位角
				wrk_x=unit.Position.X;
				wrk_y=unit.Position.Y;
				wrk_x=wrk_x-fire.Position.X;
				wrk_y=wrk_y-fire.Position.Y;
				if(wrk_x==0)	wrk_x=1;
				if(wrk_y==0)	wrk_y=1;
				drctn=atan2(wrk_y,wrk_x)*RAD_to;
				if(drctn<0)
					drctn=360+drctn;
				drctn=drctn-fire.Direction;
				if(drctn<0)
					drctn=360+drctn;
				if( drctn<=20 || drctn>=340 )
					{

					wrk_x=unit.Position.X;
					wrk_y=unit.Position.Y;
					em_drctn=unit.Direction;

					switch(Random(2))
						{
						case 0:
							em_drctn+=60+Random(30);
							break;
						case 1:
							em_drctn-=60+Random(30);
							break;
						}

					em_drctn=(int)em_drctn%360;

					wrk_x+=cos(em_drctn*a_PI)*300;
					wrk_y+=sin(em_drctn*a_PI)*300;

					unit.EmergencyDestination = new WorldPosition(wrk_x, wrk_y);
					unit.EmergencyFlags[0]=100+Random(150);

					return;
					}
				}
			}
		}

	for(n=1;n<=MaxUnitId;n++)
		{
		ref var other = ref Units[n];
		// 艦船によってくる艦船からにげる
		if( (unit.IsStopping || (unit.Kind==UnitKind.Carrier||unit.Kind==UnitKind.LightCarrier)) && other.IsUsed && (other.Category==UnitCategory.Ship  && !(other.Kind>=UnitKind.AirBase && other.Kind<=UnitKind.Fortress) ) &&
		other.Kind!=UnitKind.Submarine && other.Side!=unit.Side && other.IsFound && other.SupplyTime<=0 && unit.Target==0)
			{
			// 自点と対象点の距離
			wrk_x=other.Position.X-unit.Position.X;
			wrk_y=other.Position.Y-unit.Position.Y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			dstc=(wrk_x)/(cos(drctn*a_PI));		// 距離

			// 見る側

			if( dstc<=(double)( BB1_SIGHT ) )
				{
				wrk_x=other.Position.X-unit.Position.X;
				wrk_y=other.Position.Y-unit.Position.Y;
				if(wrk_x==0)	wrk_x=1;
				if(wrk_y==0)	wrk_y=1;
				drctn=atan2(wrk_y,wrk_x)*RAD_to;
				if(drctn<0)
					drctn=360+drctn;

				if( (unit.Kind==UnitKind.Carrier||unit.Kind==UnitKind.LightCarrier) && unit.GroupLeader==0 && other.IsStopping && Random(3)!=0 )
					{
					if(unit.Random10[0]<=4)
						{
						drctn+=(90+Random(40));
						}
					else
						{
						drctn-=(90+Random(40));
						}
					}
				else
					{
					drctn+=160+Random(40);
					}

				em_drctn=(int)drctn%360;

				wrk_x=unit.Position.X;
				wrk_y=unit.Position.Y;
				wrk_x+=cos(em_drctn*a_PI)*300;
				wrk_y+=sin(em_drctn*a_PI)*300;

				unit.EmergencyDestination = new WorldPosition(wrk_x, wrk_y);
				unit.EmergencyFlags[0]=100;
				}
			}

		// 艦船によってくる攻撃機から逃げる
		if( other.IsUsed && (other.Kind==UnitKind.Attacker || other.Kind==UnitKind.Bomber || ( other.Kind==UnitKind.Fighter && unit.Kind==UnitKind.Transport ) ) && other.PlaneState==UnitState.Flying && other.Side!=unit.Side && other.IsFound
			)
			{
			// 自点と対象点の距離
			wrk_x=other.Position.X-unit.Position.X;
			wrk_y=other.Position.Y-unit.Position.Y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			dstc=(wrk_x)/(cos(drctn*a_PI));		// 距離

			if( dstc<=600-((other.Kind==UnitKind.Bomber ? 1 : 0)*300) && dstc>=40 )
				{
				// 対象ユニットからの自点への方位角
				wrk_x=unit.Position.X;
				wrk_y=unit.Position.Y;
				wrk_x=wrk_x-other.Position.X;
				wrk_y=wrk_y-other.Position.Y;
				if(wrk_x==0)	wrk_x=1;
				if(wrk_y==0)	wrk_y=1;
				drctn=atan2(wrk_y,wrk_x)*RAD_to;
				if(drctn<0)
					drctn=360+drctn;
				drctn=drctn-other.Direction;
				if(drctn<0)
					drctn=360+drctn;
				if( drctn<=45 || drctn>=315 )
					{

					wrk_x=unit.Position.X;
					wrk_y=unit.Position.Y;
					em_drctn=unit.Direction;
					switch(Random(2))
						{
						case 0:
							em_drctn+=45+Random(45+20);
							break;
						case 1:
							em_drctn-=45+Random(45+20);
							break;
						}
					em_drctn=(int)em_drctn%360;

					wrk_x+=cos(em_drctn*a_PI)*300;
					wrk_y+=sin(em_drctn*a_PI)*300;

					unit.EmergencyDestination = new WorldPosition(wrk_x, wrk_y);
					unit.EmergencyFlags[0]=200+Random(250);

					return;
					}
				}
			}
		}

	if( unit.Kind==UnitKind.Destroyer  && unit.IsStopping.Value==1   )
		{
		// 駆逐艦の対潜水艦行動、発見された後！
		if(unit.EmergencyFlags[0]==0)
			{
			for(n=1; n<=MaxUnitId; n++)
				{
				ref var other = ref Units[n];
				if( other.IsUsed && other.Kind==UnitKind.Submarine && other.IsFound && other.Side!=unit.Side )
					{
					// 自点と対象点の距離
					if( other.IsSubmerged )
						{
						// 潜水中
						wrk_x=other.ContactX-unit.Position.X;
						wrk_y=other.ContactY-unit.Position.Y;
						}
					else
						{
						// 浮上してます
						wrk_x=other.Position.X-unit.Position.X;
						wrk_y=other.Position.Y-unit.Position.Y;
						}

					if(wrk_x==0)	wrk_x=1;
					if(wrk_y==0)	wrk_y=1;

					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					if(wrk_x<0)
						wrk_x=0-wrk_x;
					if(wrk_y<0)
						wrk_y=0-wrk_y;
					if(drctn>=180)
						drctn=drctn-180;
					if(drctn>=90)
						drctn=90-(drctn-90);
					dstc=(wrk_x)/(cos(drctn*a_PI));		// 距離

					if( dstc<=( unit.Variant==1 ? 500 : 400 ) )
						{
						// 近くに潜水艦推定位置

						wrk_x=other.ContactX-unit.Position.X;
						wrk_y=other.ContactY-unit.Position.Y;
						if(wrk_x==0)	wrk_x=1;
						if(wrk_y==0)	wrk_y=1;
						drctn=atan2(wrk_y,wrk_x)*RAD_to;

						drctn+=20-Random(40);

						em_drctn=(int)drctn%360;

						wrk_x=unit.Position.X;
						wrk_y=unit.Position.Y;
						wrk_x+=cos(em_drctn*a_PI)*((dstc)+200);
						wrk_y+=sin(em_drctn*a_PI)*((dstc)+200);

						unit.EmergencyDestination = new WorldPosition(wrk_x, wrk_y);
						unit.EmergencyFlags[0]=100+Random(250);

						return;
						}
					}
				}
			}

		}

	ReturnIntoWorld(m);

	}
}
