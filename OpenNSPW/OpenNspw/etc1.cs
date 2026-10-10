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


//#include "all_head.h"
//#include "all_extern.h"
//#include	"all_forward.h"

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
			if( Units[n].info[6]!=0 )// 潜航中、あたりがでかくなる
				{
				// ptin dbg
				wrk_rect.top=(int)Units[n].Position.Y+50;//(int)unit[n].y-50;
				wrk_rect.right=(int)Units[n].Position.X+50;
				wrk_rect.bottom=(int)Units[n].Position.Y-50;//(int)unit[n].y+50;
				wrk_rect.left=(int)Units[n].Position.X-50;

				if( PointInRect3(ref wrk_rect,(int)Fires[m].Position.X,(int)Fires[m].Position.Y)!=0 )
					h=1;
				return	(h);			
				}	
			else
				{	j=8/*4*/;j2=j/2;		}

				break;
		case UnitKind.Carrier:	j=14;j2=j/2;	break;
		case UnitKind.LightCarrier:	j=12;j2=j/2;	break;
		case UnitKind.AirBase:
		case UnitKind.NavalBase:
		case UnitKind.Fortress:	case UnitKind.Pillboxes:	case	UnitKind.InfantryBase:
		case UnitKind.City:	case UnitKind.Mine:
				// ptin dbg
				wrk_rect.top=(int)Units[n].Position.Y+30;//(int)unit[n].y-30;
				wrk_rect.right=(int)Units[n].Position.X+30;
				wrk_rect.bottom=(int)Units[n].Position.Y-30;//(int)unit[n].y+30;
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
				wrk_rect.top=(int)Units[n].Position.Y+(-j+(i*j2))+j2;//(int)unit[n].y+(-j+(i*j2))-j2;
				wrk_rect.right=(int)Units[n].Position.X+(+j-(i*j2))+j2;
				wrk_rect.bottom=(int)Units[n].Position.Y+(-j+(i*j2))-j2;//(int)unit[n].y+(-j+(i*j2))+j2;
				wrk_rect.left=(int)Units[n].Position.X+(+j-(i*j2))-j2;


				if( PointInRect3(ref wrk_rect,(int)Fires[m].Position.X,(int)Fires[m].Position.Y)!=0 )
					h=1;
				}
			break;
		case 1: case 5:
			for( i=0; i<=4 && h==0 ; i++)
				{
				// pt in dbg
				wrk_rect.top=(int)Units[n].Position.Y+(-j+(i*j2))+j2;//(int)unit[n].y+(-j+(i*j2))-j2;
				wrk_rect.right=(int)Units[n].Position.X+(-j+(i*j2))+j2;
				wrk_rect.bottom=(int)Units[n].Position.Y+(-j+(i*j2))-j2;//(int)unit[n].y+(-j+(i*j2))+j2;
				wrk_rect.left=(int)Units[n].Position.X+(-j+(i*j2))-j2;


				if( PointInRect3(ref wrk_rect,(int)Fires[m].Position.X,(int)Fires[m].Position.Y)!=0 )
					h=1;
				}

			break;
		case 0: case 4:
			for( i=0; i<=2  && h==0 ; i++)
				{
				// ptin dbg
				wrk_rect.top=(int)Units[n].Position.Y+(-j+(i*j))+j2;//(int)unit[n].y+(-j+(i*j))-j2;
				wrk_rect.right=(int)Units[n].Position.X+j2;
				wrk_rect.bottom=(int)Units[n].Position.Y+(-j+(i*j))-j2;//(int)unit[n].y+(-j+(i*j))+j2;
				wrk_rect.left=(int)Units[n].Position.X-j2;

				if( PointInRect3(ref wrk_rect,(int)Fires[m].Position.X,(int)Fires[m].Position.Y)!=0 )
					h=1;
				}
			break;
		case 2: case 6:
			for( i=0; i<=2  && h==0 ; i++)
				{
				// ptin dbg
				wrk_rect.top=(int)Units[n].Position.Y+j2;//(int)unit[n].y-j2;
				wrk_rect.right=(int)Units[n].Position.X+(-j+(i*j))+j2;
				wrk_rect.bottom=(int)Units[n].Position.Y-j2;//(int)unit[n].y+j2;
				wrk_rect.left=(int)Units[n].Position.X+(-j+(i*j))-j2;

				if( PointInRect3(ref wrk_rect,(int)Fires[m].Position.X,(int)Fires[m].Position.Y)!=0 )
					h=1;
				}
			break;
		}


	//h=0;
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

//	h=0;

	n=m;						// ターゲットナンバー
	switch( Units[n].Kind )
		{
		case UnitKind.Battleship:	j=16;j2=j/2;	break;
		case UnitKind.Cruiser:	j=12;j2=j/2;	break;
		case UnitKind.Destroyer:	j=6;j2=j/2;	break;
		case UnitKind.Submarine:	
			if( Units[n].info[6]!=0 )// 潜航中、あたりがでかくなる
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
//	h=0;
	f=ToEightDirections((int)(Units[n].Direction));
	switch( f )
		{
		case 3: case 7:
			for( i=0; i<=4 /*&& !h*/ ; i++)
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
			for( i=0; i<=4 /*&& !h*/ ; i++)
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
			for( i=0; i<=2  /*&& !h*/ ; i++)
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
			for( i=0; i<=2  /*&& !h*/ ; i++)
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


	return	/*(h)*/;
	}









//============================================================================
// 射撃します。
//----------------------------------------------------------------------------
[Original("fire_now")]
public void	FireWeapons(int m,int trgt,FireKind kind)
	{
	int	n,f,i,trgt2,rng=default /* C4701 */,s; Array3<int> fc = default;
	double		turn,wrk_x,wrk_y,drctn,drctn2=default /* C4701 */,drctn3=default /* C4701 */,drctn4,drctn5,dstc=default /* C4701 */,dstc2,dstc3,wrk_x2,wrk_y2,trgt_x,trgt_y;
	RECT		wrk_r;
	int			cm_scrn_x,cm_scrn_y;




	if( (Units[m].Kind==UnitKind.NavalBase || Units[m].Kind==UnitKind.AirBase || Units[m].Kind==UnitKind.InfantryBase || Units[m].Kind==UnitKind.Pillboxes || Units[m].Kind==UnitKind.Fortress) && Units[m].info[0]!=0	)
		{
		//工事中
		return;
		}


	if( Units[m].Category==UnitCategory.Ship )
		{
		//=========		 艦船の射撃制御		=========//
		if( kind==FireKind.CargoNavalBase || kind==FireKind.CargoAirBase || kind==FireKind.CargoInfantryBase || kind==FireKind.CargoPillboxes || kind==FireKind.CargoFortress )
			{
			// トランスポート
			// 攻撃地点から攻撃目標地点への方位角
			wrk_x=(double)Units[m].info[6]-Units[m].Position.X;
			wrk_y=(double)Units[m].info[7]-Units[m].Position.Y;
			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;	
			drctn2=drctn;
			drctn=drctn-Units[m].Direction;
			if(drctn<0)
				drctn=360+drctn;	

			if( (int)drctn<=45||(int)drctn>=315)
				{
				// 距離を求めます
				wrk_x=Units[m].Position.X-(double)Units[m].info[6];
				wrk_y=Units[m].Position.Y-(double)Units[m].info[7];
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
					Units[m].Ammo=0;		// 残弾が０
					Units[m].Target=0;		// ターゲットをクリア


					//unit[m].arm[0]=NTG;		//  輸送船はこれやっと来ます、武装品種
					Units[m].MaxAmmo=0;		//  輸送船はこれやっと来ます、武装品種



					//unit[m].used=0;


					//unit[m].info[5]=RETURN;		// 航空機はメイン兵器ゼロで帰投

					// 部下、多分戦闘機に帰投命令					
/*
					if(unit[m].is_ltl_ldr)
						{
						for(f=1;f<=max_unit;f++)
							{
							if( unit[f].used && unit[f].ltl_ldr==m )
								{
								unit[f].ltl_ldr=0;
								unit[f].info[5]=RETURN;
								}
							}

						unit[m].is_ltl_ldr=0;
						}
*/
/*
					if( !unit[m].is_ltl_ldr )
						{
						unit[m].pp_x[0]=unit[m].x;
						unit[m].pp_y[0]=unit[m].y;
						unit[m].pp_x[1]=MAP_RIGHT+1;
						}
*/

					n=FindFreeFire();
					if( n!=0 )
						{
						PlaySoundEffect( 0, SPL1 ,Units[m].Position.X, Units[m].Position.Y);
						Fires[n].Target=MaxUnitId+1;
						Fires[n].Kind=kind;
						Fires[n].Position=Units[m].Position;
						Fires[n].Direction=drctn2;
						Fires[n].Speed=1.0;
						Fires[n].Acceleration=+0.0;
						Fires[n].FinalSpeed=0.0;
						Fires[n].info[0]=0;
						Fires[n].info[1]=360;
						Fires[n].info[6]=Units[m].info[6];
						Fires[n].info[7]=Units[m].info[7];
						Fires[n].info[8]=(int)Units[m].Side;

//if( cnct_game )
//{
						Units[m].Side=0;
						Units[m].Hp=0;

						if( UnitInfoPanel[0]!=0 && UnitInfoPanel[3]==m )
							UnitInfoPanel[0]=0;


						if( SelectedUnit==m )
							{
							SelectedUnit=0; Selections[1][m]=0;	CombatMenuKind=0; CombatMenuSelection=CombatMenuItem.None; 
							ClearSelection2(1);
							BufferedMoveOrders[1].ClearsPath=0;

							if( UnitInfoPanel[3]==m )
								UnitInfoPanel[0]=0;

//							unit_info[0]=0;//unit[m].kind;
//							unit_info[1]=0;				// 空母なら１で格納庫 ０ で飛行甲板
//							unit_info[3]=0;//m;				// そのユニットの番号
//							unit_info[4]=0;//unit[m].used;	// そのユニットの国籍

							}
//}
						}
					}
				}
			return;
			}






		if( kind==FireKind.RapidAntiAircraftShell )
			{
			// 自動の対空機関砲 Rapid Anti Air Shell
			trgt2=0;
			for(n=1;n<=MaxUnitId;n++)
				{	//敵を探す。
				if( Units[n].Side!=0 && Units[n].Category==UnitCategory.Plane && (((Units[n].Kind==UnitKind.Attacker||Units[n].Kind==UnitKind.Fighter) && Units[n].Ammo!=0 )|| Random(10)==0 )  && Units[n].PlaneState==UnitState.Flying && Units[n].Side!=Units[m].Side && Units[n].Found!=0 )
				//if( unit[n].used && unit[n].ctgry==PLANE && unit[n].info[0]==FLYING && unit[n].used!=unit[m].used /*&& unit[n].hp[0]>=unit[n].hp[2]+1*/ && unit[n].found )
					{
					// 攻撃地点から攻撃目標地点への距離
					wrk_x=Units[n].Position.X-Units[m].Position.X;
					wrk_y=Units[n].Position.Y-Units[m].Position.Y;
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
					wrk_x=Units[n].Position.X;
					wrk_y=Units[n].Position.Y;
					drctn=Units[n].TurnRate;
					drctn2=Units[n].Direction;
					for(f=0;f<=(int)turn;f++)
						{
						drctn2+=drctn;
						if(drctn2<0)		drctn2=360+drctn2;
						if(drctn2>=360)		drctn2=drctn2-360;
						wrk_x+=cos(drctn2*a_PI)*(Units[n].Speed); // とりあえずターン後
						wrk_y+=sin(drctn2*a_PI)*(Units[n].Speed);
						}


					wrk_x2=wrk_x;
					wrk_y2=wrk_y;
					wrk_x=wrk_x2-Units[m].Position.X;
					wrk_y=wrk_y2-Units[m].Position.Y;





					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					drctn2=drctn;							


					drctn=drctn-Units[m].Direction;
					if(drctn<0)
						drctn=360+drctn;				
					drctn3=drctn;							// 方位角


					
					// 攻撃地点から攻撃目標地点への距離
					wrk_x=wrk_x2-Units[m].Position.X;
					wrk_y=wrk_y2-Units[m].Position.Y;
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
	
					/****				
					switch( unit[m].kind )
						{
						case BB1:	rng=900; fc[0]=1;fc[1]=2;fc[2]=1;	break;
						case CA1:	rng=600; fc[0]=1;fc[1]=1;fc[2]=1;	break;
						case DD1:	rng=300; fc[0]=1;fc[1]=1;fc[2]=0;	break;
						}
					****/
					if( dstc>=40 && dstc<=300/*480*/ )
						{
						trgt2=n;	
						if( 1!=0 /*|| rnd(2)==0*/ )
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


				//for( i=1; i<=f ;i++ )
					//{

				n=FindFreeFire();
				if( n!=0 )
					{
					if(Units[m].Ammo!=0)
						Units[m].Ammo-=RAS_SZ;			// 弾薬消費

					if(SharedRandom(2)!=0 )
						PlaySoundEffect( 0, AA_SHL3 ,Units[m].Position.X, Units[m].Position.Y);
					else
						PlaySoundEffect( 0, AA_SHL5 ,Units[m].Position.X, Units[m].Position.Y);

					Fires[n].Target=trgt2;
					Fires[n].Kind=kind;
					Fires[n].Position=Units[m].Position;

					drctn3=drctn2+(Random(18)-9);			// 絶対方位
					if(drctn3>=360)	drctn3=drctn3-360;				
					if(drctn3<0)	drctn3=360+drctn3;				
					Fires[n].Direction=drctn3;

					dstc2=dstc+(Random( ((int)(dstc/5)) )-((int)(dstc/10))   );

					Fires[n].Speed=10.0;
					Fires[n].Acceleration=-0.00;
					Fires[n].FinalSpeed=0;
					Fires[n].info[0]=(int)(dstc2/Fires[n].Speed);
					Fires[n].info[1]=0;
					}
					//}
				}
			return;
			}




		if( kind==FireKind.AntiSubmarineBomb && Units[m].Speed>=Units[m].MaxSpeed )
			{
			for( n=1; n<=MaxUnitId; n++)
				{
				trgt=n;
				if( Units[trgt].Side!=Units[m].Side && Units[trgt].Kind==UnitKind.Submarine && Units[trgt].info[6]!=0 && Units[trgt].Found!=0 )
					{	// 爆雷

					// ptin dbg
					wrk_r.top=(int)Units[trgt].info[8]+Units[trgt].info[9];//(int)unit[trgt].info[8]-unit[trgt].info[9];
					wrk_r.right=(int)Units[trgt].info[7]+Units[trgt].info[9];
					wrk_r.bottom=(int)Units[trgt].info[8]-Units[trgt].info[9];//(int)unit[trgt].info[8]+unit[trgt].info[9];
					wrk_r.left=(int)Units[trgt].info[7]-Units[trgt].info[9];
					if( PointInRect3(ref wrk_r,(int)Units[m].Position.X,(int)Units[m].Position.Y)!=0 )
						{
						// 投雷
						n=FindFreeFire();
							if(n!=0)
							{
							if(Units[m].Ammo!=0)
								Units[m].Ammo-=ASB_SZ;			// 弾薬消費
							if(Units[m].Ammo<0)
								Units[m].Ammo=0;

							PlaySoundEffect( 0, SPL1 ,Units[m].Position.X, Units[m].Position.Y);
							Fires[n].Target=trgt;
							Fires[n].Kind=kind;
							Fires[n].Position=Units[m].Position;

							drctn=Units[m].Direction;
							if( Units[m].Variant==0 )
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
							Fires[n].info[0]=0;
							Fires[n].info[1]=100;
							}
						return;
						}
					}
				}
			return;
			}




		if( ( kind==FireKind.Gun || kind==FireKind.NavalBaseGun ) && trgt==0)	// ターゲットが選択されていない砲撃、
			{
			// 艦砲、自動射撃
			trgt2=0;
			dstc2=2000;
			for(n=1;n<=MaxUnitId;n++)
				{	//敵を探す。
				if( Units[n].Side!=0 && Units[n].Category==UnitCategory.Ship && !(Units[n].Kind==UnitKind.Submarine && Units[n].info[6]!=0 ) && Units[n].Side!=Units[m].Side && Units[n].Found!=0  && Units[n].Kind!=UnitKind.City )
					{
					// 攻撃地点から攻撃目標地点への距離
					wrk_x=Units[n].Position.X-Units[m].Position.X;
					wrk_y=Units[n].Position.Y-Units[m].Position.Y;
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
					wrk_x=Units[n].Position.X;
					wrk_y=Units[n].Position.Y;
					wrk_x+=cos(Units[n].Direction*a_PI)*(Units[n].Speed*turn); // とりあえずターン後
					wrk_y+=sin(Units[n].Direction*a_PI)*(Units[n].Speed*turn);
					wrk_x2=wrk_x;										// ターゲットの未来位置
					wrk_y2=wrk_y;


					// 攻撃地点から攻撃目標地点への絶対方位、方位角
					wrk_x=wrk_x-Units[m].Position.X;
					wrk_y=wrk_y-Units[m].Position.Y;
					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					drctn4=drctn;							// 絶対方位

					drctn=drctn-Units[m].Direction;
					if(drctn<0)
						drctn=360+drctn;				
					drctn5=drctn;							// 方位角


					
					// 攻撃地点から攻撃目標地点への距離
					wrk_x=wrk_x2-Units[m].Position.X;
					wrk_y=wrk_y2-Units[m].Position.Y;
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
					
					switch( Units[m].Kind )
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

					if( (dstc>=(rng*0.3) || (Units[m].Kind>=UnitKind.InfantryBase&&Units[m].Kind<=UnitKind.Fortress) ) && dstc<=rng && dstc2>=dstc )
						{	
						trgt2=n;	
						dstc2=dstc;
						drctn2=drctn4;
						drctn3=drctn5;							
						//break;	
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
			

				switch( Units[m].Kind )
					{
					case UnitKind.Battleship:
					case UnitKind.Fortress:
						if( kind==FireKind.NavalBaseGun )
							{
							PlaySoundEffect( 0, GUN3 ,Units[m].Position.X, Units[m].Position.Y);	
							break;
							}
						PlaySoundEffect( 0, GUN2+Random(2) ,Units[m].Position.X, Units[m].Position.Y);	
						break;
					case UnitKind.Cruiser:	
					case UnitKind.Pillboxes:	
						PlaySoundEffect( 0, GUN1+Random(2) ,Units[m].Position.X, Units[m].Position.Y);
						break;

					case UnitKind.Destroyer:	
					case UnitKind.Submarine:	
					case UnitKind.InfantryBase:	
						PlaySoundEffect( 0, GUN1 ,Units[m].Position.X, Units[m].Position.Y);
						break;
					}


				for( i=1; i<=f ;i++ )
					{
					n=FindFreeFire();
					if( n!=0 )
						{
						if(Units[m].Ammo!=0)
							Units[m].Ammo-=GUN_SZ;			// 弾薬消費

						Fires[n].Target=trgt2;
						Fires[n].Kind=FireKind.Gun/*kind*/;
						Fires[n].Position=Units[m].Position;

						if( kind==FireKind.NavalBaseGun )
							{
							if( Random(2)==0 )
								drctn3=drctn2+(double)((double)(Random(50)-25)/10)    /*+(rnd(100)/100)*/;			// 絶対方位
							else
								drctn3=drctn2+(double)((double)(Random(80)-40)/10)    /*+(rnd(100)/100)*/;			// 絶対方位
							}
						else
							{
							if( Random(5+(Units[m].Kind>=UnitKind.InfantryBase&&Units[m].Kind<=UnitKind.Fortress ? 1 : 0)*4  )==0 || ( Units[m].Kind==UnitKind.Battleship && Random( 4 )==0 ) )
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
						Fires[n].info[0]=(int)(dstc2/Fires[n].Speed)+1;
						Fires[n].info[1]=Fires[n].info[0]/2;



						}
					}
				}
			return;
			}





		// 選択でない自動の魚雷、主に駆逐艦
		if( kind==FireKind.Torpedo && trgt==0)
			{
			// 
			trgt2=0;
			for(n=1;n<=MaxUnitId;n++)
				{	//敵を探す。
				if( Units[n].Side!=0 && Units[n].Category==UnitCategory.Ship && !(Units[n].Kind==UnitKind.AirBase||Units[n].Kind==UnitKind.NavalBase||Units[n].Kind==UnitKind.InfantryBase||Units[n].Kind==UnitKind.Pillboxes||Units[n].Kind==UnitKind.Fortress) && Units[n].Side!=Units[m].Side && Units[n].Found!=0 && !(Units[n].Kind==UnitKind.Submarine||Units[n].Kind==UnitKind.Destroyer) )
					{
					// 攻撃地点から攻撃目標地点への距離
					wrk_x=Units[n].Position.X-Units[m].Position.X;
					wrk_y=Units[n].Position.Y-Units[m].Position.Y;
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
					wrk_x=wrk_x2-Units[m].Position.X;
					wrk_y=wrk_y2-Units[m].Position.Y;

					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					drctn2=drctn;					// 未来位置への絶対角


					drctn=drctn-Units[m].Direction;
					if(drctn<0)
						drctn=360+drctn;				
					drctn3=drctn;							// 方位角


					
					// 攻撃地点から攻撃目標地点への距離
					wrk_x=wrk_x2-Units[m].Position.X;
					wrk_y=wrk_y2-Units[m].Position.Y;
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

					

					if( (dstc>=100 && dstc<=(500+(Units[m].Side==Side.Japan ? 1 : 0)*100)) && ((drctn3>=45&&drctn3<=135)||(drctn3>=225&&drctn3<=315)) )
						{
						// ばってん陸地があるけんしらべる
						trgt2=n;

						i=(int)(dstc/TPD_SPD);
						for(f=1;f<=i;f++)
							{
							wrk_x=Units[m].Position.X;
							wrk_y=Units[m].Position.Y;
							wrk_x+=cos(drctn2*a_PI)*(TPD_SPD*f); // とりあえずターン後
							wrk_y+=sin(drctn2*a_PI)*(TPD_SPD*f);

							if(!( wrk_y>MAP_TOP || wrk_y<MAP_BOTTOM || wrk_x<MAP_LEFT || wrk_x>MAP_RIGHT ))
								{
								cm_scrn_x=(int)((wrk_x+(Sprites[UNIT_JPN].wd/2)-MAP_LEFT)/Sprites[MAP_TIP_NRML].wd);
								cm_scrn_y=(int)((MAP_TOP-wrk_y+(Sprites[UNIT_JPN].ht/2))/Sprites[MAP_TIP_NRML].ht);
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
				if(Units[m].Ammo!=0)
					Units[m].Ammo-=TPD_SZ;
				switch( Units[m].Kind )
					{
					case UnitKind.Cruiser:
					case UnitKind.Destroyer:		Units[m].ReloadTime=RELOAD_TPD_DD;		break;	// 再装填時間
					case UnitKind.Submarine:		Units[m].ReloadTime=RELOAD_TPD_SS;		
									//撃った瞬間に発見される。
									Units[m].info[7]=(int)(Units[m].Position.X+Random((50)*2)-50);
									Units[m].info[8]=(int)(Units[m].Position.Y+Random((50)*2)-50);

									Units[m].info[9]=100;
									Units[m].info[10]=300;

									Units[m].Found=1;





									break;	// 再装填時間
					}


				if( Units[m].Side==Side.Japan  )
					{
					if(Units[m].Kind==UnitKind.Destroyer)
						i=2;						// 日本海軍駆逐艦魚雷３発
					else
						i=1;						// 日本海軍巡洋艦魚雷２はつ
					}
				else
					{
					i=0;						// 合衆国海軍魚雷１発
					}

				if( Units[m].Kind!=UnitKind.Submarine )
					PlaySoundEffect( 0, TPD_LOS ,Units[m].Position.X, Units[m].Position.Y);

				for( f=0; f<=i; f++)
					{
					n=FindFreeFire();
					if( n!=0 )
						{
//						if( unit[m].kind!=SS1 )
//							SoundPlayEffect( NULL, SPL1 ,unit[m].x, unit[m].y);
						Fires[n].Target=trgt2;
						Fires[n].Kind=kind;
						Fires[n].Position=Units[m].Position;


						switch( f )
							{
							case 0:	Fires[n].Direction=drctn2;	break;
							case 1:	Fires[n].Direction=drctn2+5;	break;
							case 2:	Fires[n].Direction=drctn2-5;	break;
							}
						Fires[n].Direction=(int)(Fires[n].Direction)%360;


/*****

						if( unit[m].kind==SS1 )
							{
							fire[n].drctn=unit[m].drctn+355.0+(f*5);
							while(fire[n].drctn>=360)
								{fire[n].drctn=fire[n].drctn-360;}
							}
						else
							{
							if( ((int)drctn3>=45&&(int)drctn3<=135) )
								fire[n].drctn=unit[m].drctn+85.0+(f*5);
							else
								fire[n].drctn=unit[m].drctn+265.0+(f*5);
							if(fire[n].drctn>=360)
								fire[n].drctn=fire[n].drctn-360;
							}
***/
						Fires[n].Speed=TPD_SPD;
						Fires[n].Acceleration=+0.0;
						Fires[n].FinalSpeed=0.0;
						if( Units[m].Kind==UnitKind.Submarine )
							Fires[n].info[0]=1;
						else
							Fires[n].info[0]=0;
						Fires[n].info[1]=275+(Units[m].Side==Side.Japan ? 1 : 0)*110;
						Fires[n].info[2]=30;
						}
					}
				}
			return;
			}





		// 選択された敵への魚雷、主に、潜水艦
		if( kind==FireKind.Torpedo && trgt!=0)
			{
			// 

			trgt2=0;
			n=trgt;
			if( Units[n].Side!=0 && Units[n].Category==UnitCategory.Ship && !(Units[n].Kind==UnitKind.AirBase||Units[n].Kind==UnitKind.NavalBase||Units[n].Kind==UnitKind.InfantryBase||Units[n].Kind==UnitKind.Pillboxes||Units[n].Kind==UnitKind.Fortress) && Units[n].Side!=Units[m].Side && Units[n].Found!=0 )
				{

	
				// 攻撃地点から攻撃目標地点への距離
				wrk_x=Units[n].Position.X-Units[m].Position.X;
				wrk_y=Units[n].Position.Y-Units[m].Position.Y;
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
				wrk_x=wrk_x2-Units[m].Position.X;
				wrk_y=wrk_y2-Units[m].Position.Y;

				drctn=atan2(wrk_y,wrk_x)*RAD_to;
				if(drctn<0)
					drctn=360+drctn;
				drctn2=drctn;					// 未来位置への絶対角


				drctn=drctn-Units[m].Direction;
				if(drctn<0)
					drctn=360+drctn;				
				drctn3=drctn;							// 方位角


				
				// 攻撃地点から攻撃目標地点への距離
				wrk_x=wrk_x2-Units[m].Position.X;
				wrk_y=wrk_y2-Units[m].Position.Y;
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

				
				if( (dstc>=100 && dstc<=(550+(Units[m].Side==Side.Japan ? 1 : 0)*100)) && (drctn3<=5 || drctn3>=355) )
					{
					trgt2=n;	
					i=(int)(dstc/TPD_SPD);
					for(f=1;f<=i;f++)
						{
						wrk_x=Units[m].Position.X;
						wrk_y=Units[m].Position.Y;
						wrk_x+=cos(drctn2*a_PI)*(TPD_SPD*f); // とりあえずターン後
						wrk_y+=sin(drctn2*a_PI)*(TPD_SPD*f);

						if(!( wrk_y>MAP_TOP || wrk_y<MAP_BOTTOM || wrk_x<MAP_LEFT || wrk_x>MAP_RIGHT ))
							{
							cm_scrn_x=(int)((wrk_x+(Sprites[UNIT_JPN].wd/2)-MAP_LEFT)/Sprites[MAP_TIP_NRML].wd);
							cm_scrn_y=(int)((MAP_TOP-wrk_y+(Sprites[UNIT_JPN].ht/2))/Sprites[MAP_TIP_NRML].ht);
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
				if(Units[m].Ammo!=0)
					Units[m].Ammo-=TPD_SZ;
				switch( Units[m].Kind )
					{
					case UnitKind.Destroyer:		Units[m].ReloadTime=RELOAD_TPD_DD;		break;	// 再装填時間
					case UnitKind.Submarine:		Units[m].ReloadTime=RELOAD_TPD_SS;		
									//撃った瞬間に発見される。
									Units[m].info[7]=(int)(Units[m].Position.X+Random((50)*2)-50);
									Units[m].info[8]=(int)(Units[m].Position.Y+Random((50)*2)-50);

									Units[m].info[9]=100;
									Units[m].info[10]=300;

									Units[m].Found=1;
									break;	// 再装填時間
					}


				if( Units[m].Kind!=UnitKind.Submarine )
					{
					PlaySoundEffect( 0, TPD_LOS ,Units[m].Position.X, Units[m].Position.Y);
					}
				else
					{
					Units[m].Target=0;
					}
					
/*					SoundPlayEffect( NULL, SPL1 ,unit[m].x, unit[m].y);
*/
				for( f=0; f<=2; f++)
					{
					n=FindFreeFire();
					if( n!=0 )
						{
//						if( unit[m].kind!=SS1 )
//							SoundPlayEffect( NULL, SPL1 ,unit[m].x, unit[m].y);
						Fires[n].Target=trgt2;
						Fires[n].Kind=kind;
						Fires[n].Position=Units[m].Position;

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
						if( Units[m].Kind==UnitKind.Submarine )
							Fires[n].info[0]=1;
						else
							Fires[n].info[0]=0;
						Fires[n].info[1]=290+(Units[m].Side==Side.Japan ? 1 : 0)*110;
						Fires[n].info[2]=30;
						}
					}



				}
			return;
			}







		if( kind==FireKind.AntiAircraftShell && trgt!=0 && Units[trgt].Category==UnitCategory.Plane )	// ターゲットが選択された対空砲
			{
			//	指定射撃
			if( Units[trgt].Found==0 )
				return;

			trgt2=0;
			n=trgt;
			// 攻撃地点から攻撃目標地点への絶対方位、方位角

			wrk_x=Units[n].Position.X-Units[m].Position.X;
			wrk_y=Units[n].Position.Y-Units[m].Position.Y;
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
			wrk_x=wrk_x-Units[m].Position.X;
			wrk_y=wrk_y-Units[m].Position.Y;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			drctn2=drctn;							


			drctn=drctn-Units[m].Direction;
			if(drctn<0)
				drctn=360+drctn;				
			drctn3=drctn;							// 方位角


					
			// 攻撃地点から攻撃目標地点への距離
			wrk_x=wrk_x2-Units[m].Position.X;
			wrk_y=wrk_y2-Units[m].Position.Y;
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




					
			switch( Units[m].Kind )
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
			


				PlaySoundEffect( 0, AA_SHL2 ,Units[m].Position.X, Units[m].Position.Y);
				for( i=1; i<=f ;i++ )
					{

					n=FindFreeFire();
					if( n!=0 )
						{
						if(Units[m].Ammo!=0)
							Units[m].Ammo-=SHL_SZ;			// 弾薬消費
						//SoundPlayEffect( NULL, GUN1+rnd(3) ,unit[m].x, unit[m].y);
						Fires[n].Target=trgt2;


						Fires[n].Kind=FireKind.AntiAircraftShell;

						Fires[n].Position=Units[m].Position;


						drctn3=drctn2+(Random(18)-9);			// 絶対方位
						if(drctn3>=360)	drctn3=drctn3-360;				
						if(drctn3<0)	drctn3=360+drctn3;				
						Fires[n].Direction=drctn3;

						dstc2=dstc+(Random( ((int)(dstc/10)) )-((int)(dstc/20))   );

						Fires[n].Speed=10.0;
						Fires[n].Acceleration=-0.00;
						Fires[n].FinalSpeed=0;
						Fires[n].info[0]=(int)(dstc2/Fires[n].Speed);
						Fires[n].info[1]=0;


						}
					}
				}
			return;
			}





		if( kind==FireKind.AntiAircraftShell && trgt==0 )
			{
			// 自動の対空砲 Anti Air Shell
			trgt2=0;
			for(n=1;n<=MaxUnitId;n++)
				{	//敵を探す。
				if( Units[n].Side!=0 && Units[n].Category==UnitCategory.Plane && (((Units[n].Kind==UnitKind.Attacker||Units[n].Kind==UnitKind.Bomber) && Units[n].Ammo!=0 )|| Random(10)==0 )  && Units[n].PlaneState==UnitState.Flying && Units[n].Side!=Units[m].Side && Units[n].Found!=0 )
					{
					// 攻撃地点から攻撃目標地点への距離
					wrk_x=Units[n].Position.X-Units[m].Position.X;
					wrk_y=Units[n].Position.Y-Units[m].Position.Y;
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
					wrk_x=Units[n].Position.X;
					wrk_y=Units[n].Position.Y;
					drctn=Units[n].TurnRate;
					drctn2=Units[n].Direction;
					for(f=0;f<=(int)turn;f++)
						{
						drctn2+=drctn;
						if(drctn2<0)		drctn2=360+drctn2;
						if(drctn2>=360)		drctn2=drctn2-360;
						wrk_x+=cos(drctn2*a_PI)*(Units[n].Speed); // とりあえずターン後
						wrk_y+=sin(drctn2*a_PI)*(Units[n].Speed);
						}


					wrk_x2=wrk_x;
					wrk_y2=wrk_y;
					wrk_x=wrk_x2-Units[m].Position.X;
					wrk_y=wrk_y2-Units[m].Position.Y;





					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					drctn2=drctn;							


					drctn=drctn-Units[m].Direction;
					if(drctn<0)
						drctn=360+drctn;				
					drctn3=drctn;							// 方位角


					
					// 攻撃地点から攻撃目標地点への距離
					wrk_x=wrk_x2-Units[m].Position.X;
					wrk_y=wrk_y2-Units[m].Position.Y;
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


					
					switch( Units[m].Kind )
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
/**
							if( 1 || rnd(2)==0 )
								break;
							else
								trgt2=0;
**/
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
			


				//SoundPlayEffect( NULL, AA_SHL2 ,unit[m].x, unit[m].y);
				for( i=1; i<=f ;i++ )
					{
					n=FindFreeFire();
					if( n!=0 )
						{
						if(Units[m].Ammo!=0)
							Units[m].Ammo-=SHL_SZ;			// 弾薬消費
						PlaySoundEffect( 0, AA_SHL2 ,Units[m].Position.X, Units[m].Position.Y);
						Fires[n].Target=trgt2;
						Fires[n].Kind=kind;
						Fires[n].Position=Units[m].Position;

						drctn3=drctn2+(Random(18)-9);			// 絶対方位
						if(drctn3>=360)	drctn3=drctn3-360;				
						if(drctn3<0)	drctn3=360+drctn3;				
						Fires[n].Direction=drctn3;

						dstc2=dstc+(Random( ((int)(dstc/10)) )-((int)(dstc/20))   );

						Fires[n].Speed=10.0;
						Fires[n].Acceleration=-0.00;
						Fires[n].FinalSpeed=0;
						Fires[n].info[0]=(int)(dstc2/Fires[n].Speed);
						Fires[n].info[1]=0;
						}
					}
				}
			return;
			}






		if( ( kind==FireKind.Gun || kind==FireKind.NavalBaseGun ) && trgt!=0 && Units[trgt].Category==UnitCategory.Ship )	// ターゲットが選択された砲撃
			{
			// 艦砲		指定射撃
			if( Units[trgt].Found==0 )
				return;

			trgt2=0;
			n=trgt;
			// 攻撃地点から攻撃目標地点への絶対方位、方位角

			// 攻撃地点から攻撃目標地点への距離
			wrk_x=Units[n].Position.X-Units[m].Position.X;
			wrk_y=Units[n].Position.Y-Units[m].Position.Y;
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
			wrk_x=wrk_x-Units[m].Position.X;
			wrk_y=wrk_y-Units[m].Position.Y;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			drctn2=drctn;							


			drctn=drctn-Units[m].Direction;
			if(drctn<0)
				drctn=360+drctn;				
			drctn3=drctn;							// 方位角


			
			// 攻撃地点から攻撃目標地点への距離
			wrk_x=wrk_x2-Units[m].Position.X;
			wrk_y=wrk_y2-Units[m].Position.Y;
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
			switch( Units[m].Kind )
				{
				case UnitKind.Battleship:	
					if( kind==FireKind.NavalBaseGun )
						{
//						rng=750; fc[0]=3;fc[1]=4;fc[2]=2;	
						rng=1120; fc[0]=3;fc[1]=5;fc[2]=2;	
						}
					else
						{
						rng=800; fc[0]=3;fc[1]=5;fc[2]=2;	
						}
//					rng=800; fc[0]=3;fc[1]=5;fc[2]=2;	
					break;
				case UnitKind.Cruiser:	rng=700; fc[0]=2;fc[1]=3;fc[2]=1;	break;
				case UnitKind.Destroyer:	rng=400; fc[0]=1;fc[1]=1;fc[2]=0;	break;
				case UnitKind.Submarine:	rng=300; fc[0]=1;fc[1]=1;fc[2]=0;	break;

				case UnitKind.InfantryBase:	rng=600; fc[0]=1;fc[1]=1;fc[2]=1;	break;
				case UnitKind.Pillboxes:	rng=800; fc[0]=2;fc[1]=2;fc[2]=2;	break;
				case UnitKind.Fortress:	rng=1000; fc[0]=3;fc[1]=3;fc[2]=3;	break;
				}


			if( (dstc>=150  || (Units[m].Kind>=UnitKind.InfantryBase&&Units[m].Kind<=UnitKind.Fortress) ) && dstc<=rng )
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
			


				switch( Units[m].Kind )
					{
					case UnitKind.Battleship:	
					case UnitKind.Fortress:
						PlaySoundEffect( 0, GUN3 ,Units[m].Position.X, Units[m].Position.Y);	
						break;
					case UnitKind.Cruiser:	
					case UnitKind.Pillboxes:	
						PlaySoundEffect( 0, GUN2 ,Units[m].Position.X, Units[m].Position.Y);
						break;

					case UnitKind.Destroyer:	
					case UnitKind.Submarine:	
					case UnitKind.InfantryBase:	
						PlaySoundEffect( 0, GUN1+Random(2) ,Units[m].Position.X, Units[m].Position.Y);
						break;
					}

				for( i=1; i<=f ;i++ )
					{

					n=FindFreeFire();
					if( n!=0 )
						{
						Units[m].Ammo-=GUN_SZ;			// 弾薬消費
						Fires[n].Target=trgt2;


						if( Units[trgt2].Category==UnitCategory.Ship )
							{
							Fires[n].Kind=FireKind.Gun/*kind*/;
	
							Fires[n].Position=Units[m].Position;

							if( kind==FireKind.NavalBaseGun && 0!=0 )
								{
								if( Random(3)!=0 )
									drctn3=drctn2+(double)((double)(Random(20)-10)/10)    /*+(rnd(100)/100)*/;			// 絶対方位
								else
									drctn3=drctn2+(double)((double)(Random(60)-30)/10)    /*+(rnd(100)/100)*/;			// 絶対方位
								}
							else
								{
								if( Random(5)==0 || ( Units[m].Kind==UnitKind.Battleship && Random(4)==0 ) )
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
							Fires[n].info[0]=(int)(dstc2/Fires[n].Speed);
							Fires[n].info[1]=Fires[n].info[0]/2;
							}
						else
							{
							Fires[n].Kind=FireKind.AntiAircraftShell;

							Fires[n].Position=Units[m].Position;

							drctn3=drctn2+(Random(20)-10);			// 絶対方位
							if(drctn3>=360)	drctn3=drctn3-360;				
							if(drctn3<0)	drctn3=360+drctn3;				
							Fires[n].Direction=drctn3;

							dstc2=dstc+(Random( ((int)(dstc/10)) )-((int)(dstc/20))   );

							Fires[n].Speed=10.0;
							Fires[n].Acceleration=-0.00;
							Fires[n].FinalSpeed=0;
							Fires[n].info[0]=(int)(dstc2/Fires[n].Speed);
							Fires[n].info[1]=0;
							}


						}
					}
				}
			return;
			}



		if( kind==FireKind.Bullet )
			{
			// 艦船の対空機銃 
			trgt=0;
			for(n=1;n<=MaxUnitId;n++)
				{	//敵を探す。
				if( Units[n].Side!=0 && ( (Units[n].Category==UnitCategory.Plane && Units[n].PlaneState==UnitState.Flying )  || (Units[n].Kind>=UnitKind.InfantryBase && Units[n].Kind<=UnitKind.Fortress ) ) && Units[n].Kind!=UnitKind.Bomber
				&& Units[n].Side!=Units[m].Side && Units[n].Found!=0 )
					{
					// 全方位射撃可能
					wrk_x=Units[n].Position.X-Units[m].Position.X;
					wrk_y=Units[n].Position.Y-Units[m].Position.Y;

					drctn=atan2(wrk_y,wrk_x)*RAD_to;
				
					if(drctn<0)
						drctn=360+drctn;
					drctn2=drctn+(25-Random(50));
//drctn2=drctn+(25);
					drctn2=abs((int)drctn2)%360;


					// 攻撃地点から攻撃目標地点への距離
					wrk_x=Units[n].Position.X-Units[m].Position.X;
					wrk_y=Units[n].Position.Y-Units[m].Position.Y;
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
//if(1 )
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
//if( 1 )
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
					PlaySoundEffect( 0, AA_BLT3 ,Units[m].Position.X, Units[m].Position.Y);
					Fires[n].Target=trgt;
					Fires[n].Kind=kind;
					Fires[n].Position=Units[m].Position;
					Fires[n].Direction=drctn2;
					Fires[n].Speed=17.0;
					Fires[n].Acceleration=-0.1;
					Fires[n].FinalSpeed=15.0;
					}
				}
			return;
			}
		}




	if( Units[m].Category==UnitCategory.Plane )
		{
				//=========		 航空機の射撃制御		=========//
		if( kind==FireKind.Bullet && Units[m].Kind==UnitKind.Bomber )
			{
			// 航空機の全方向対空機銃 
			trgt=0;
			for(n=1;n<=MaxUnitId;n++)
				{	//敵を探す。
				if( Units[n].Side!=0 && Units[n].Category==UnitCategory.Plane && Units[n].PlaneState==UnitState.Flying && Units[n].Side!=Units[m].Side && Units[n].Found!=0 )
					{
					// 全方位射撃可能
					wrk_x=Units[n].Position.X-Units[m].Position.X;
					wrk_y=Units[n].Position.Y-Units[m].Position.Y;

					drctn=atan2(wrk_y,wrk_x)*RAD_to;
				
					if(drctn<0)
						drctn=360+drctn;
					drctn2=drctn+(Random(20)-10);
					drctn2=abs((int)drctn2)%360;


					// 攻撃地点から攻撃目標地点への距離
					wrk_x=Units[n].Position.X-Units[m].Position.X;
					wrk_y=Units[n].Position.Y-Units[m].Position.Y;
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
					PlaySoundEffect( 0, AA_BLT4 ,Units[m].Position.X, Units[m].Position.Y);
					Fires[n].Target=trgt;
					Fires[n].Kind=kind;
					Fires[n].Position=Units[m].Position;
					Fires[n].Direction=drctn2;
					Fires[n].Speed=17.0;
					Fires[n].Acceleration=-0.1;
					Fires[n].FinalSpeed=15.0;
					}
				}
			return;
			}





		if( kind==FireKind.Bullet && Units[m].Kind==UnitKind.Fighter )
			{
			// 戦闘機
			// 前方固定銃
			trgt=0;		dstc2=5000;
			for(n=1;n<=MaxUnitId;n++)
				{	//前方の敵を探す。
				if( Units[n].Side!=0 && (( Units[n].Category==UnitCategory.Plane && Units[n].PlaneState==UnitState.Flying ) || Units[n].Kind==UnitKind.Transport ) && Units[n].Side!=Units[m].Side )
					{
					// 距離を調べます
					wrk_x=Units[n].Position.X-Units[m].Position.X;
					wrk_y=Units[n].Position.Y-Units[m].Position.Y;
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
						wrk_x=Units[n].Position.X-Units[m].Position.X;
						wrk_y=Units[n].Position.Y-Units[m].Position.Y;
						drctn=atan2(wrk_y,wrk_x)*RAD_to;
						if(drctn<0)
							drctn=360+drctn;
						drctn=drctn-Units[m].Direction;
						if(drctn<0)
							drctn=360+drctn;
						if( ((int)drctn<=10||(int)drctn>=350) && Units[m].Target!=0 && Units[m].Target==n )
							{
							dstc2=dstc;
							trgt=n;
							}
						}


					if( Units[m].Target==0 )
						{
						if(Units[m].Mode==UnitMode.Return)
							{
#if false
							if( /*unit[m].used==cpu_side &&*/ unit[m].Fuel>=(60-(unit[m].used==Side.Japan)*10) && unit[m].Ammo )
								{
								if( dstc<=300 && rnd(10)==0 )
									{
									if( unit[n].found )
										{
										unit[m].Target=n;
										}
									}

								}
#endif
							}
						else
							{
							if( dstc<=400+(Units[n].Kind==UnitKind.Attacker||Units[n].Kind==UnitKind.Bomber ? 1 : 0)*250 && Random(10)==0 )
								{
								if( Units[n].Found!=0 )
									{
									Units[m].Target=n;
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
					Units[m].Ammo--;			// 弾薬消費
					if(Units[m].Variant==0)
						{
						// 艦上戦闘機
						if(Units[m].Side==Side.Japan)
							PlaySoundEffect( 0, AA_BLT1 ,Units[m].Position.X, Units[m].Position.Y);
						else
							PlaySoundEffect( 0, AA_BLT2 ,Units[m].Position.X, Units[m].Position.Y);
						}
					else
						{
						// 陸上戦闘機
						PlaySoundEffect( 0, AA_SHL4 ,Units[m].Position.X, Units[m].Position.Y);
						}

					Fires[n].Target=trgt;
					Fires[n].Kind=kind;
					Fires[n].Position=Units[m].Position;
					Fires[n].Direction=Units[m].Direction;
					Fires[n].Speed=16.0;
					Fires[n].Acceleration=-0.1;
					Fires[n].FinalSpeed=14.0;
					}
				}
			return;
			}





		if( kind==FireKind.Bullet )
			{
			trgt=0;
			for(n=1;n<=MaxUnitId;n++)
				{	//後方の敵を探す。
				if( Units[n].Side!=0 && Units[n].Category==UnitCategory.Plane && Units[n].PlaneState==UnitState.Flying && Units[n].Side!=Units[m].Side && Units[n].Found!=0 )
					{
					// 攻撃地点から攻撃目標地点への方位角
					wrk_x=Units[n].Position.X-Units[m].Position.X;
					wrk_y=Units[n].Position.Y-Units[m].Position.Y;
					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;	

					drctn2=drctn+(Random(10)-5);
					if(drctn2>=360)	drctn2=drctn2-360;				
					if(drctn2<0)	drctn2=360+drctn2;				

					drctn=drctn-Units[m].Direction;
					if(drctn<0)
						drctn=360+drctn;
					if( (int)drctn>=150&&(int)drctn<=210 )
						{
						wrk_x=Units[n].Position.X-Units[m].Position.X;
						wrk_y=Units[n].Position.Y-Units[m].Position.Y;
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
					PlaySoundEffect( 0, AA_BLT3 ,Units[m].Position.X, Units[m].Position.Y);
					Fires[n].Target=trgt;
					Fires[n].Kind=kind;
					Fires[n].Position=Units[m].Position;
					Fires[n].Direction=drctn2;
					Fires[n].Speed=16.0;
					Fires[n].Acceleration=-0.1;
					Fires[n].FinalSpeed=14.0;
					}
				}
			return;
			}




			if( kind==FireKind.Torpedo && !(Units[trgt].Kind>=UnitKind.AirBase && Units[trgt].Kind<=UnitKind.Fortress) )
				{
				// 攻撃機
				// トゥピード

				//地上の上なら投雷しない。

				if(!( Units[m].Position.Y>MAP_TOP || Units[m].Position.Y<MAP_BOTTOM || Units[m].Position.X<MAP_LEFT || Units[m].Position.X>MAP_RIGHT ))
					{
					cm_scrn_x=(int)((Units[m].Position.X+(Sprites[UNIT_JPN].wd/2)-MAP_LEFT)/Sprites[MAP_TIP_NRML].wd);
					cm_scrn_y=(int)((MAP_TOP-Units[m].Position.Y+(Sprites[UNIT_JPN].ht/2))/Sprites[MAP_TIP_NRML].ht);

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
						return;
						}
					}



			// 攻撃地点から攻撃目標地点への方位角
			if( Units[trgt].Found==0 )
				return;

			trgt_x=Units[trgt].Position.X;
			trgt_y=Units[trgt].Position.Y;

			trgt_x+=cos(Units[trgt].Direction*a_PI)*((AIR_TPD_LOS_DSTC/AIR_TPD_SPD)*Units[trgt].Speed); // とりあえずターン後
			trgt_y+=sin(Units[trgt].Direction*a_PI)*((AIR_TPD_LOS_DSTC/AIR_TPD_SPD)*Units[trgt].Speed);


			wrk_x=trgt_x-Units[m].Position.X;
			wrk_y=trgt_y-Units[m].Position.Y;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;	

			drctn=drctn-Units[m].Direction;
			if(drctn<0)
				drctn=360+drctn;	


			if( (int)drctn<=45||(int)drctn>=315)
				{
				// 距離を求めます
				wrk_x=Units[m].Position.X-trgt_x;
				wrk_y=Units[m].Position.Y-trgt_y;
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
						wrk_x=Units[m].Position.X;
						wrk_y=Units[m].Position.Y;
						wrk_x+=cos(drctn2*a_PI)*(2*f); // とりあえずターン後
						wrk_y+=sin(drctn2*a_PI)*(2*f);

						if(!( wrk_y>MAP_TOP || wrk_y<MAP_BOTTOM || wrk_x<MAP_LEFT || wrk_x>MAP_RIGHT ))
							{
							cm_scrn_x=(int)((wrk_x+(Sprites[UNIT_JPN].wd/2)-MAP_LEFT)/Sprites[MAP_TIP_NRML].wd);
							cm_scrn_y=(int)((MAP_TOP-wrk_y+(Sprites[UNIT_JPN].ht/2))/Sprites[MAP_TIP_NRML].ht);
							if( MapTiles[cm_scrn_y][cm_scrn_x]>=1 )
								{
								return;
								}
							}
						}


					// 発射！
					Units[m].Ammo=0;		// 魚雷が０
					Units[m].Target=0;		// ターゲットをクリア

					Units[m].Mode=UnitMode.Return;		// 航空機はメイン兵器ゼロで帰投


					// 雷撃時に適当に移動さす
					wrk_x=Units[m].Position.X;
					wrk_y=Units[m].Position.Y;
					drctn=Units[m].Direction;

					if(Random(2)==0)
						drctn+=(70-Random(40));
					else
						drctn-=(70-Random(40));

					drctn=(int)(drctn)%360;


					wrk_x+=cos(drctn*a_PI)*300; 
					wrk_y+=sin(drctn*a_PI)*300;

					Units[m].EmergencyDestination = new WorldPosition(wrk_x, wrk_y);
					Units[m].EmergencyFlags[0]=20+Random(300);


					




					// 部下、多分戦闘機に帰投命令					
					if(Units[m].IsGroupLeader!=0)
						{
						for(f=1;f<=MaxUnitId;f++)
							{
							if( Units[f].Side!=0 && Units[f].GroupLeader==m && Units[f].PlaneState==UnitState.Flying )
								{
								Units[f].GroupLeader=0;
								Units[f].Mode=UnitMode.Return;

Units[f].PathX[0]=Units[m].Position.X;
Units[f].PathY[0]=Units[m].Position.Y;
Units[f].PathX[1]=MAP_RIGHT+1;

								}
							}

						Units[m].IsGroupLeader=0;
						}


					if( Units[m].IsGroupLeader==0 )
						{
						Units[m].PathX[0]=Units[m].Position.X;
						Units[m].PathY[0]=Units[m].Position.Y;
						Units[m].PathX[1]=MAP_RIGHT+1;
						}

					n=FindFreeFire();
					if( n!=0 )
						{
						PlaySoundEffect( 0, SPL1 ,Units[m].Position.X, Units[m].Position.Y);
						Fires[n].Target=trgt;
						Fires[n].Kind=kind;
						Fires[n].Position=Units[m].Position;
						//fire[n].drctn=(int)(unit[m].drctn+(2-rnd(4)))%360;
						Fires[n].Direction=(int)Units[m].Direction;
						Fires[n].Speed=AIR_TPD_SPD;
						Fires[n].Acceleration=+0.0;
						Fires[n].FinalSpeed=0.0;
						Fires[n].info[0]=0;
						Fires[n].info[1]=240;
						Fires[n].info[2]=15;
						}
					}
				}
			return;
			}




		if( kind==FireKind.Bomb && Units[m].Kind==UnitKind.Attacker )
			{
			// 攻撃機
			// 爆撃
			// 攻撃地点から攻撃目標地点への方位角
			if( Units[trgt].Found==0 )
				return;
			wrk_x=Units[trgt].Position.X;
			wrk_y=Units[trgt].Position.Y;
			wrk_x+=cos(Units[trgt].Direction*a_PI)*(Units[trgt].Speed*70.0);
			wrk_y+=sin(Units[trgt].Direction*a_PI)*(Units[trgt].Speed*70.0);

			wrk_x2=wrk_x;	wrk_y2=wrk_y;



			wrk_x=wrk_x-Units[m].Position.X;
			wrk_y=wrk_y-Units[m].Position.Y;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;	
			drctn2=drctn;
			drctn=drctn-Units[m].Direction;
			if(drctn<0)
				drctn=360+drctn;	

			if( (int)drctn<=30/*45*/||(int)drctn>=330/*315*/ )
				{
				// 距離を求めます
				wrk_x=Units[m].Position.X-wrk_x2;
				wrk_y=Units[m].Position.Y-wrk_y2;
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




//				if( dstc>=170/*40*/ && dstc<=180/*50*/  )
				if( ( dstc>=170 && dstc<=180 && Units[m].Side==Side.UnitedStates ) || ( dstc>=35 && dstc<=65 && Units[m].Side==Side.Japan ))
					{
					// 発射！
//					SoundPlayEffect( NULL, BOMB_OFF ,unit[m].x, unit[m].y);

					if( Units[m].Side==Side.UnitedStates )
						Units[m].Speed+=Units[m].AccelerationChange*700;
					else
						{
						wrk_x=Units[m].Position.X;
						wrk_y=Units[m].Position.Y;
						drctn=Units[m].Direction;
						wrk_x+=cos(drctn*a_PI)*300; 
						wrk_y+=sin(drctn*a_PI)*300;
						Units[m].EmergencyDestination = new WorldPosition(wrk_x, wrk_y);
						Units[m].EmergencyFlags[0]=150+Random(50);
						}

					Units[m].Ammo=0;		// 消費
					Units[m].Target=0;		

					Units[m].Mode=UnitMode.Return;		// 航空機はメイン兵器ゼロで帰投


					// 部下、多分戦闘機に帰投命令					
					if(Units[m].IsGroupLeader!=0)
						{
						for(f=1;f<=MaxUnitId;f++)
							{
							if( Units[f].Side!=0 && Units[f].GroupLeader==m && Units[f].PlaneState==UnitState.Flying)
								{
								Units[f].GroupLeader=0;
								Units[f].Mode=UnitMode.Return;

Units[f].PathX[0]=Units[m].Position.X;
Units[f].PathY[0]=Units[m].Position.Y;
Units[f].PathX[1]=MAP_RIGHT+1;

								}
							}
						Units[m].IsGroupLeader=0;
						}



					n=FindFreeFire();
					if( n!=0 )
						{
//						fire[n].used=trgt;
						Fires[n].Target=(int)UnitKind.Attacker;
						Fires[n].Kind=kind;
						Fires[n].Position = new WorldPosition(Units[m].Position.X+(3-Random(6)), Units[m].Position.Y+(3-Random(6)));
						Fires[n].Direction=drctn2;
	
						if(Units[m].Side==Side.Japan)
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
						if(Units[m].Side==Side.Japan)
							{
							Fires[n].info[0]=10;
							Fires[n].info[1]=68+Random(5);
							}
						else
							{
							Fires[n].info[0]=0;
							Fires[n].info[1]=70;
							}
							
						}

//					if( unit[m].used==USA )
//						{
						// もう一発
						n=FindFreeFire();
						if( n!=0 )
							{
							Fires[n].Target=trgt;
							Fires[n].Kind=kind;
							Fires[n].Position = new WorldPosition(Units[m].Position.X+(20-Random(40)), Units[m].Position.Y+(20-Random(40)));
							Fires[n].Direction=drctn2;
	
							if(Units[m].Side==Side.Japan)
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

							if(Units[m].Side==Side.Japan)
								{
								Fires[n].info[0]=10;
								Fires[n].info[1]=75+(5-Random(10));
								}
							else
								{
								Fires[n].info[0]=0;
								Fires[n].info[1]=70+(5-Random(10));
								}
							}
//						}
					}
				}
			return;
			}

		if( kind==FireKind.Bomb && Units[m].Kind==UnitKind.Bomber )
			{	
			// 爆撃機
			// 爆撃
			trgt2=0;
			if(trgt!=0)
				trgt2=trgt;
			for(n=1;n<=MaxUnitId;n++)
				{	//前方の敵を探す。
				if(trgt2!=0)
					{ n=trgt2; trgt=0; }
				if( Units[n].Side!=0 && Units[n].Category==UnitCategory.Ship && Units[n].Side!=Units[m].Side && Units[n].Found!=0 )
					{
					// 攻撃地点から攻撃目標地点への方位角
					wrk_x=Units[n].Position.X-Units[m].Position.X;
					wrk_y=Units[n].Position.Y-Units[m].Position.Y;
					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;	
					drctn2=drctn;

					drctn2=drctn+(Random(10)-5);
					if(drctn2>=360)	drctn2=drctn2-360;				
					if(drctn2<0)	drctn2=360+drctn2;				

					drctn=drctn-Units[m].Direction;
					if(drctn<0)
						drctn=360+drctn;
					if( (int)drctn<=30 || (int)drctn>=330 )
						{
						wrk_x=Units[n].Position.X-Units[m].Position.X;
						wrk_y=Units[n].Position.Y-Units[m].Position.Y;
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
					PlaySoundEffect( 0, BB_BOMB ,Units[m].Position.X, Units[m].Position.Y);
					if(Units[m].Ammo!=0)
						Units[m].Ammo--;		// 消費

					if( Units[m].Ammo<=0)
						{
						Units[m].Ammo=0;		// 消費
						Units[m].Target=0;		
						Units[m].Mode=UnitMode.Return;		// 航空機はメイン兵器ゼロで帰投
						}

					Units[m].ReloadTime=5;		// 再装填時間

					if(Units[m].Ammo<=0)
						Units[m].Mode=UnitMode.Return;		// 航空機はメイン兵器ゼロで帰投


					Fires[n].Target=(int)UnitKind.Bomber;
					Fires[n].Kind=kind;
					Fires[n].Position = new WorldPosition(Units[m].Position.X+((double)(-6+Random(13))), Units[m].Position.Y+((double)(-6+Random(13))));
					Fires[n].Direction=drctn2;
					Fires[n].Speed=0.3;
					Fires[n].Acceleration=+0.2;
					Fires[n].FinalSpeed=0.0;
					Fires[n].info[0]=0;
					Fires[n].info[1]=70;
					}
				}




			return;
			}

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
			nums=5/*6*/;			// １小隊何機か

			// 何番編隊か
			a=Units[n].FormationNumber/nums;
			// 何番機か
			b=Units[n].FormationNumber%nums;

			//if(a==1&&b==1)
			//	dstc=250+320;
				

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



		if( 1!=0 /*|| unit[n].used==cpu_side*/ )
			{
			//コンピュータの進路計算
			SetCpuRoute2( n );
			}
		}





//	unit[n].pp_now=0;
	Units[n].Stop=0;

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
		if( Units[n].Side!=0 && Units[n].Category==UnitCategory.Ship )
			{
			// ptin dbg
			wrk_r.top=(int)Units[n].Position.Y+(Sprites[UNIT_JPN].ht/2);//(int)unit[n].y-(sprt[UNIT_JPN].ht/2);
			wrk_r.right=(int)Units[n].Position.X+(Sprites[UNIT_JPN].wd/2);
			wrk_r.bottom=(int)Units[n].Position.Y-(Sprites[UNIT_JPN].ht/2);//(int)unit[n].y+(sprt[UNIT_JPN].ht/2);
			wrk_r.left=(int)Units[n].Position.X-(Sprites[UNIT_JPN].wd/2);

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

#if false
	// 損傷がひどくなったら逃げよう
	if( unit[m].kind==UnitKind.Fighter && unit[m].Mode!=UnitMode.Return && unit[m].Hp<=unit[m].MaxHp/2 )
		{
		unit[m].Target=0;		// ターゲットをクリア
		unit[m].Mode=UnitMode.Return;		// 航空機はメイン兵器ゼロで帰投

		unit[m].ltl_ldr=0;

		if( !unit[m].is_ltl_ldr )
			{
			unit[m].pp_x[0]=unit[m].x;
			unit[m].pp_y[0]=unit[m].y;
			unit[m].pp_x[1]=MAP_RIGHT+1;
			}
		}
#endif


	// 目標も、爆弾も無く、損傷がひどいかガソリンが切れそうな場合はきとうしよう
	if( Units[m].Kind==UnitKind.Fighter && Units[m].Mode!=UnitMode.Return && (Units[m].Fuel<=30 || Units[m].Hp<=Units[m].MaxHp*0.70 || Units[m].Ammo<=0 ) )
		{
		// 発射！
		Units[m].Target=0;		// ターゲットをクリア
		Units[m].Mode=UnitMode.Return;		// 航空機はメイン兵器ゼロで帰投
		Units[m].GroupLeader=0;

		if( Units[m].IsGroupLeader==0 )
			{
			Units[m].PathX[0]=Units[m].Position.X;
			Units[m].PathY[0]=Units[m].Position.Y;
			Units[m].PathX[1]=MAP_RIGHT+1;
			}
		}
	}








//============================================================================
// 戦闘機の攻撃機動をセット
//----------------------------------------------------------------------------
[Original("set_pos_of_attack_FT")]
public void	SetFighterAttackDestination(int m)
	{
	double			angl,dstc,wrk_x,wrk_y,drctn,drctn2,em_drctn;
	int				trgt,pos_of_no,a,b,c;
	int				lvl_my,lvl_en,my_tec=default /* C4701 */,en_tec,n;
	RECT			wrk_r;


	// ptin dbg
	wrk_r.top=(int)Units[m].EmergencyDestination.Y+35;//(int)unit[m].em_y-35;
	wrk_r.right=(int)Units[m].EmergencyDestination.X+35;
	wrk_r.bottom=(int)Units[m].EmergencyDestination.Y-35;//(int)unit[m].em_y+35;
	wrk_r.left=(int)Units[m].EmergencyDestination.X-35;

	if( PointInRect3(ref wrk_r,(int)Units[m].Position.X,(int)Units[m].Position.Y)!=0 )
		{
		Units[m].EmergencyDestination = new WorldPosition(Units[m].Position.X+cos(Units[m].Direction*a_PI)*(100+Random(50)), Units[m].Position.Y+sin(Units[m].Direction*a_PI)*(100+Random(50)));
		Units[m].EmergencyFlags[0]=100;
		Units[m].Stop=0;
		return;
		}





	trgt=Units[m].Target;
	lvl_my=Units[m].Skill;
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
	wrk_x=Units[m].Position.X-Units[n].Position.X;
	wrk_y=Units[m].Position.Y-Units[n].Position.Y;
	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;					// drctnが絶対角

	drctn=drctn-Units[n].Direction;				// 敵機ｎからの方位角をしらべます。
	if(drctn<0)
		drctn=360+drctn;
	if( (int)drctn<=5 || (int)drctn>=355 )
		{
		// 敵機が正面に自機を捕らえています。
		wrk_x=Units[n].Position.X-Units[m].Position.X;
		wrk_y=Units[n].Position.Y-Units[m].Position.Y;
		drctn=atan2(wrk_y,wrk_x)*RAD_to;
		if(drctn<0)
			drctn=360+drctn;					// drctnが絶対角

		drctn=drctn-Units[m].Direction;				// 自機ｍからの方位角をしらべます。
		if(drctn<0)
			drctn=360+drctn;

		if( (int)drctn<=5 || (int)drctn>=355 )
			{
			// 自機も敵機を正面に捕らえています
			wrk_x=Units[n].Position.X-Units[m].Position.X;
			wrk_y=Units[n].Position.Y-Units[m].Position.Y;
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
				wrk_x=Units[m].Position.X;
				wrk_y=Units[m].Position.Y;
				em_drctn=Units[m].Direction;
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

				Units[m].EmergencyDestination = new WorldPosition(wrk_x, wrk_y);
				Units[m].EmergencyFlags[0]=70+Random(40);

				return;
				}
			}
		}







	// 敵機の直前にＥｍ＿Ｘｙを設定します。
	wrk_x=Units[trgt].Position.X;
	wrk_y=Units[trgt].Position.Y;
	wrk_x+=cos(Units[trgt].Direction*a_PI)*(Units[trgt].Speed*20.0); 
	wrk_y+=sin(Units[trgt].Direction*a_PI)*(Units[trgt].Speed*20.0);

	Units[m].EmergencyDestination = new WorldPosition(wrk_x, wrk_y);
	Units[m].EmergencyFlags[0]=70+Random(40);
	Units[m].Stop=0;
	if(Random(10)==0)
		PlaySoundEffect( 0, PLANE1+Random(2) ,Units[m].Position.X, Units[m].Position.Y);

	}




//============================================================================
// 攻撃機の緊急起動をセット
//----------------------------------------------------------------------------
[Original("set_pos_of_emrgncy_AT")]
public void	SetAttackerEmergencyDestination(int m)
	{
	int		n,g,new_ldr,f;
	double	em_drctn,wrk_x,wrk_y,drctn,dstc,drctn2;

	

	// 戦闘機から逃げよう
	for(n=1;n<=MaxUnitId;n++)
		{	//後方の敵を探す。
		if( Units[n].Side!=0 && Units[n].Category==UnitCategory.Plane && Units[n].PlaneState==UnitState.Flying 
		&& Units[n].Side!=Units[m].Side && Units[n].Found!=0 )
			{

			// 攻撃地点から攻撃目標地点への方位角
			wrk_x=Units[n].Position.X-Units[m].Position.X;
			wrk_y=Units[n].Position.Y-Units[m].Position.Y;
			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;	

			drctn=drctn-Units[m].Direction;
			if(drctn<0)
				drctn=360+drctn;


			// 攻撃目標地点から攻撃地点への方位角
			wrk_x=Units[m].Position.X-Units[n].Position.X;
			wrk_y=Units[m].Position.Y-Units[n].Position.Y;
			drctn2=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn2<0)
				drctn2=360+drctn2;	

			drctn2=drctn2-Units[n].Direction;
			if(drctn2<0)
				drctn2=360+drctn2;




			if( ((int)drctn>=150 && (int)drctn<=210 && Units[n].Kind==UnitKind.Fighter) 
				|| 
				( ((int)drctn<=45 || (int)drctn>=315) && ( (int)drctn2>=135 && (int)drctn2<=225)  && Units[n].Kind==UnitKind.Attacker && Units[m].Target==0 )
				/*||
				(  ( ((int)drctn>=150&&(int)drctn<=210) || ((int)drctn<=45||(int)drctn>=315) )  && unit[n].kind==FT1 && unit[m].kind==BM1) 
				*/
			  )
				{


				wrk_x=Units[n].Position.X-Units[m].Position.X;
				wrk_y=Units[n].Position.Y-Units[m].Position.Y;
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
				if( dstc<=250+((Units[n].Kind==UnitKind.Attacker ? 1 : 0)*70) )
					{	

					wrk_x=Units[m].Position.X;
					wrk_y=Units[m].Position.Y;
					em_drctn=Units[m].Direction;
					switch(Random(2))
						{
						case 0:
							em_drctn+=45+Random(90);
							break;
						case 1:
							em_drctn-=45+Random(90);
							break;
						}
					/*(int)*/em_drctn=(int)em_drctn%360;


					wrk_x+=cos(em_drctn*a_PI)*300; 
					wrk_y+=sin(em_drctn*a_PI)*300;

					Units[m].EmergencyDestination = new WorldPosition(wrk_x, wrk_y);
					Units[m].EmergencyFlags[0]=100;


					return;
					}
				}
			}
		}








	// 損傷がひどいかガソリンが切れそうな場合はきとうしよう
	if( Units[m].Mode!=UnitMode.Return && (Units[m].Fuel<=20 || Units[m].Hp<=Units[m].MaxHp*0.70 ) 
		/*&& unit[unit[m].info[1]].used*/
		)
		{
		Units[m].Ammo=0;		// 魚雷が０
		Units[m].Target=0;		// ターゲットをクリア


		if(Units[Units[m].info[1]].Side!=0)		
			{
		Units[m].Mode=UnitMode.Return;		// 航空機はメイン兵器ゼロで帰投

		Units[m].GroupLeader=0;

		if( Units[m].IsGroupLeader==0 )
			{
			Units[m].PathX[0]=Units[m].Position.X;
			Units[m].PathY[0]=Units[m].Position.Y;
			Units[m].PathX[1]=MAP_RIGHT+1;
			}
		else
			{

			n=MaxUnitId+1;
			g=1;
			new_ldr=0;

			for(f=1;f<=MaxUnitId;f++)
				{
				if( Units[f].Side!=0 && Units[f].GroupLeader==m )
					{
					g++;
					if( Units[f].FormationNumber < n )
						{
						n=Units[f].FormationNumber;
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
				if( Units[new_ldr].Kind==UnitKind.Fighter && (Units[m].Kind==UnitKind.Attacker || Units[m].Kind==UnitKind.Bomber) /*&& unit[m].info[0]==FLYING*/ && Units[new_ldr].PlaneState==UnitState.Flying )
					{
					Units[new_ldr].Mode=UnitMode.Return;		// それまでの隊長がボスだったらきかんしよっと
					}


				for(f=1;f<=MaxUnitId;f++)
					{
					if( Units[f].Side!=0 && Units[f].GroupLeader==m )
						{
						Units[f].GroupLeader=(short)new_ldr;

						// 昔の小隊長が攻爆撃機だったら、帰投にしておく
						if( Units[new_ldr].Kind==UnitKind.Fighter && Units[f].Kind==UnitKind.Fighter && (Units[m].Kind==UnitKind.Attacker || Units[m].Kind==UnitKind.Bomber) /*&& unit[m].info[0]==FLYING*/&& Units[f].PlaneState==UnitState.Flying )
							Units[f].Mode=UnitMode.Return;		// それまでの隊長がボスだったらきかんしよっと
						}


					}

				for(f=0;f<64;f++)
					{
					Units[new_ldr].PathX[f]=Units[m].PathX[f];
					Units[new_ldr].PathY[f]=Units[m].PathY[f];
					}

				// 昔の小隊長
				Units[m].PathX[0]=Units[m].Position.X;
				Units[m].PathY[0]=Units[m].Position.Y;
				Units[m].PathX[1]=MAP_RIGHT+1;


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
	double			angl,dstc,wrk_x,wrk_y,drctn,drctn2,drctn3,turn;
	int				trgt,pos_of_no,a,b,c,i;
	int				nums,lvl_jp,lvl_us,jp_tec,us_tec,n,f;
	RECT			wrk_r;



	n=Units[m].Target;				// 攻撃目標


	// 現位置から攻撃目標地点への距離
	wrk_x=Units[n].Position.X-Units[m].Position.X;
	wrk_y=Units[n].Position.Y-Units[m].Position.Y;
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
	wrk_x=wrk_x-Units[m].Position.X;
	wrk_y=wrk_y-Units[m].Position.Y;
	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;	
	drctn=drctn-Units[m].Direction;
	if(drctn<0)
		drctn=360+drctn;	


	if( dstc<=510 && dstc >= 500  && (drctn<=22.5||drctn>=337.5) /*&& unit[m].no /*&& unit[m].is_ltl_ldr==0*/ /*&& (int)(unit[m].ltl_ldr)*/ )
		{

		// リーダー機か単独機のみここに来ます。

		f=0;
		for(i=1;i<=MaxUnitId;i++)
			{
			if( i!=m && Units[i].Side!=0 && Units[i].GroupLeader==m && Units[i].Kind==UnitKind.Attacker )
				{
				f++;

				if( Units[i].Kind!=UnitKind.Fighter )
					Units[i].GroupLeader=0; 

				wrk_x=Units[i].Position.X;
				wrk_y=Units[i].Position.Y;
				drctn=Units[i].Direction;

				switch( Units[i].FormationNumber%5 )
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

				Units[i].EmergencyDestination = new WorldPosition(wrk_x, wrk_y);
				Units[i].EmergencyFlags[0]=10+((1+(Units[i].FormationNumber%5))*20);

				Units[i].PathX[0]=Units[n].Position.X;
				Units[i].PathY[0]=Units[n].Position.Y;
				Units[i].PathX[1]=MAP_RIGHT+1;

				}
			}


		}
	else if( dstc >= 400 )
		{
		if( Units[m].PathX[1]==MAP_RIGHT+1 )
			{
			// ptin dbg
			wrk_r.top=(int)Units[m].PathY[0]+35;//(int)unit[m].pp_y[0]-35;
			wrk_r.right=(int)Units[m].PathX[0]+35;
			wrk_r.bottom=(int)Units[m].PathY[0]-35;//(int)unit[m].pp_y[0]+35;
			wrk_r.left=(int)Units[m].PathX[0]-35;
			if( PointInRect3(ref wrk_r,(int)Units[m].Position.X,(int)Units[m].Position.Y)==0 || Units[n].Found==0)
				{
				return;
				}
			wrk_x=Units[n].Position.X;
			wrk_y=Units[n].Position.Y;
			wrk_x+=cos(Units[n].Direction*a_PI); // とりあえずターン後
			wrk_y+=sin(Units[n].Direction*a_PI);
			Units[m].PathX[0]=wrk_x;
			Units[m].PathY[0]=wrk_y;			
			Units[m].PathX[1]=MAP_RIGHT+1;
			Units[m].Stop=0;
			}
		}
	else if ( dstc >= 40 && (drctn<=45||drctn>=315))
		{
		if( Units[m].PathX[1]==MAP_RIGHT+1 )
			{
			wrk_r.top=(int)Units[m].PathY[0]-35;
			wrk_r.right=(int)Units[m].PathX[0]+35;
			wrk_r.bottom=(int)Units[m].PathY[0]+35;
			wrk_r.left=(int)Units[m].PathX[0]-35;
			if(Units[m].Weapon==FireKind.Torpedo)
				turn=(/*dstc*/ (AIR_TPD_LOS_DSTC) /AIR_TPD_SPD);		// 投雷距離　÷ 魚雷速度　でターンを求めます
			else
				turn=70.0;
			wrk_x=Units[n].Position.X;
			wrk_y=Units[n].Position.Y;
			wrk_x+=cos(Units[n].Direction*a_PI)*(Units[n].Speed*turn); // とりあえずターン後
			wrk_y+=sin(Units[n].Direction*a_PI)*(Units[n].Speed*turn);
			Units[m].PathX[0]=wrk_x;
			Units[m].PathY[0]=wrk_y;
			Units[m].PathX[1]=MAP_RIGHT+1;
			Units[m].Stop=0;
			if( Units[m].IsGroupLeader!=0 /*&& unit[m].arm[0]==BOM*/ )
				{
				a=0;
				for(f=1;f<=MaxUnitId;f++)
					{
 					if( Units[f].Side!=0 && Units[f].Kind==UnitKind.Fighter && Units[f].GroupLeader==m)
						{	a++;	}
					else
						{
						if( Units[f].Side!=0 && Units[f].GroupLeader==m )
							Units[f].GroupLeader=0;
						}
					}
				Units[m].IsGroupLeader=(short)a;
				}
			}
		}
	else 
		{	// 近すぎる場合は離脱
		// ptin dbg
		wrk_r.top=(int)Units[m].PathY[0]+35;//(int)unit[m].pp_y[0]-35;
		wrk_r.right=(int)Units[m].PathX[0]+35;
		wrk_r.bottom=(int)Units[m].PathY[0]-35;//(int)unit[m].pp_y[0]+35;
		wrk_r.left=(int)Units[m].PathX[0]-35;
		if( PointInRect3(ref wrk_r,(int)Units[m].Position.X,(int)Units[m].Position.Y)!=0 )
			{
			Units[m].PathX[0]=Units[m].Position.X+cos(Units[m].Direction*a_PI)*(300);
			Units[m].PathY[0]=Units[m].Position.Y+sin(Units[m].Direction*a_PI)*(300);
			Units[m].PathX[1]=MAP_RIGHT+1;
			Units[m].Stop=0;
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
	wrk_x=Units[m].info[6]-Units[m].Position.X;
	wrk_y=Units[m].info[7]-Units[m].Position.Y;
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
		Units[m].EmergencyDestination = new WorldPosition(Units[m].info[6], Units[m].info[7]);
		Units[m].EmergencyFlags[0]=200;
		}
	}







//============================================================================
// 
//----------------------------------------------------------------------------
[Original("em_of_out_of_map")]
public void ReturnIntoWorld(int m)
	{
	// 展開海域より外れたなら戻る
	if( 1!=0 /*unit[m].used!=cpu_side*/ )
		{
		if( Units[m].Position.X>MAP_RIGHT )
			{
			Units[m].EmergencyDestination = new WorldPosition(MAP_RIGHT-40, Units[m].Position.Y-100+Random(200));
			Units[m].EmergencyFlags[0]=50+Random(200);
			return;
			}

		if( Units[m].Position.X<MAP_LEFT )
			{
			Units[m].EmergencyDestination = new WorldPosition(MAP_LEFT+40, Units[m].Position.Y-100+Random(200));
			Units[m].EmergencyFlags[0]=50+Random(200);
			return;
			}

		if( Units[m].Position.Y>MAP_TOP )
			{
			Units[m].EmergencyDestination = new WorldPosition(Units[m].Position.X-100+Random(200), MAP_TOP-40);
			Units[m].EmergencyFlags[0]=50+Random(200);
			return;
			}

		if( Units[m].Position.Y<MAP_BOTTOM )
			{
			Units[m].EmergencyDestination = new WorldPosition(Units[m].Position.X-100+Random(200), MAP_BOTTOM+40);
			Units[m].EmergencyFlags[0]=50+Random(200);
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
	int		n;
	double	em_drctn,wrk_x,wrk_y,drctn,dstc;
	int		size;



//return;


	for(n=1;n<FIRE_MAX;n++)
		{
		// 艦船によってくる魚雷から逃げる
		if( Fires[n].Target!=0 && Fires[n].Kind==FireKind.Torpedo && Fires[n].info[0]>=Fires[n].info[2] /*&& unit[n].found*/ )
			{
			// 自点と対象点の距離
			wrk_x=Fires[n].Position.X-Units[m].Position.X;
			wrk_y=Fires[n].Position.Y-Units[m].Position.Y;
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
				wrk_x=Units[m].Position.X;
				wrk_y=Units[m].Position.Y;
				wrk_x=wrk_x-Fires[n].Position.X;
				wrk_y=wrk_y-Fires[n].Position.Y;
				if(wrk_x==0)	wrk_x=1;
				if(wrk_y==0)	wrk_y=1;
				drctn=atan2(wrk_y,wrk_x)*RAD_to;
				if(drctn<0)
					drctn=360+drctn;	
				drctn=drctn-Fires[n].Direction;
				if(drctn<0)
					drctn=360+drctn;
				if( drctn<=20 || drctn>=340 )
					{

					wrk_x=Units[m].Position.X;
					wrk_y=Units[m].Position.Y;
					em_drctn=Units[m].Direction;

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

					Units[m].EmergencyDestination = new WorldPosition(wrk_x, wrk_y);
					Units[m].EmergencyFlags[0]=100+Random(150);
					//unit[m].stop=1;

					return;
					}
				}
			}
		}



	for(n=1;n<=MaxUnitId;n++)
		{
		// 艦船によってくる艦船からにげる
		if( (Units[m].Stop!=0 || (Units[m].Kind==UnitKind.Carrier||Units[m].Kind==UnitKind.LightCarrier)) && Units[n].Side!=0 && (Units[n].Category==UnitCategory.Ship /*&& unit[n].kind!=AP && unit[n].kind!=SP*/ && !(Units[n].Kind>=UnitKind.AirBase && Units[n].Kind<=UnitKind.Fortress) ) && 
		Units[n].Kind!=UnitKind.Submarine && Units[n].Side!=Units[m].Side && Units[n].Found!=0 && Units[n].Supply<=0 && Units[m].Target==0)
			{
			// 自点と対象点の距離
			wrk_x=Units[n].Position.X-Units[m].Position.X;
			wrk_y=Units[n].Position.Y-Units[m].Position.Y;
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
				wrk_x=Units[n].Position.X-Units[m].Position.X;
				wrk_y=Units[n].Position.Y-Units[m].Position.Y;
				if(wrk_x==0)	wrk_x=1;
				if(wrk_y==0)	wrk_y=1;
				drctn=atan2(wrk_y,wrk_x)*RAD_to;
				if(drctn<0)
					drctn=360+drctn;	

				if( (Units[m].Kind==UnitKind.Carrier||Units[m].Kind==UnitKind.LightCarrier) && Units[m].GroupLeader==0 && Units[n].Stop!=0 && Random(3)!=0 )
					{
					if(Units[m].Random10[0]<=4)
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

				wrk_x=Units[m].Position.X;
				wrk_y=Units[m].Position.Y;
				wrk_x+=cos(em_drctn*a_PI)*300;
				wrk_y+=sin(em_drctn*a_PI)*300;

				Units[m].EmergencyDestination = new WorldPosition(wrk_x, wrk_y);
				Units[m].EmergencyFlags[0]=100;
//unit[m].em_flg[0]=0;
				}
			}


		// 艦船によってくる攻撃機から逃げる
		if( Units[n].Side!=0 && (Units[n].Kind==UnitKind.Attacker || Units[n].Kind==UnitKind.Bomber || ( Units[n].Kind==UnitKind.Fighter && Units[m].Kind==UnitKind.Transport ) ) && Units[n].PlaneState==UnitState.Flying && Units[n].Side!=Units[m].Side && Units[n].Found!=0 
			)
			{
			// 自点と対象点の距離
			wrk_x=Units[n].Position.X-Units[m].Position.X;
			wrk_y=Units[n].Position.Y-Units[m].Position.Y;
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

			if( dstc<=600-((Units[n].Kind==UnitKind.Bomber ? 1 : 0)*300) && dstc>=40 )
				{
				// 対象ユニットからの自点への方位角
				wrk_x=Units[m].Position.X;
				wrk_y=Units[m].Position.Y;
				wrk_x=wrk_x-Units[n].Position.X;
				wrk_y=wrk_y-Units[n].Position.Y;
				if(wrk_x==0)	wrk_x=1;
				if(wrk_y==0)	wrk_y=1;
				drctn=atan2(wrk_y,wrk_x)*RAD_to;
				if(drctn<0)
					drctn=360+drctn;	
				drctn=drctn-Units[n].Direction;
				if(drctn<0)
					drctn=360+drctn;
				if( drctn<=45 || drctn>=315 )
					{

					wrk_x=Units[m].Position.X;
					wrk_y=Units[m].Position.Y;
					em_drctn=Units[m].Direction;
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

					Units[m].EmergencyDestination = new WorldPosition(wrk_x, wrk_y);
					Units[m].EmergencyFlags[0]=200+Random(250);
					//unit[m].stop=1;

					return;
					}
				}
			}
		}



	if( Units[m].Kind==UnitKind.Destroyer /*&&  unit[m].spd<=unit[m].max_spd*0.9*/ && Units[m].Stop==1  /*&& unit[m].used==cpu_side*/ )
		{
		// 駆逐艦の対潜水艦行動、発見された後！
		if(Units[m].EmergencyFlags[0]==0)
			{
			for(n=1; n<=MaxUnitId; n++)
				{
				if( Units[n].Side!=0 && Units[n].Kind==UnitKind.Submarine && Units[n].Found!=0 && Units[n].Side!=Units[m].Side )
					{
					// 自点と対象点の距離
					if( Units[n].info[6]!=0 )
						{
						// 潜水中
						wrk_x=Units[n].info[7]-Units[m].Position.X;
						wrk_y=Units[n].info[8]-Units[m].Position.Y;
						}
					else
						{
						// 浮上してます
						wrk_x=Units[n].Position.X-Units[m].Position.X;
						wrk_y=Units[n].Position.Y-Units[m].Position.Y;
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
					
					if( dstc<=( Units[m].Variant==1 ? 500 : 400 ) )
						{
						// 近くに潜水艦推定位置

						wrk_x=Units[n].info[7]-Units[m].Position.X;
						wrk_y=Units[n].info[8]-Units[m].Position.Y;
						if(wrk_x==0)	wrk_x=1;
						if(wrk_y==0)	wrk_y=1;
						drctn=atan2(wrk_y,wrk_x)*RAD_to;

						drctn+=20-Random(40);
/*
						if(drctn<0)
							drctn=360+drctn;	
*/

						em_drctn=(int)drctn%360;

						wrk_x=Units[m].Position.X;
						wrk_y=Units[m].Position.Y;
						wrk_x+=cos(em_drctn*a_PI)*((dstc)+200);
						wrk_y+=sin(em_drctn*a_PI)*((dstc)+200);


						Units[m].EmergencyDestination = new WorldPosition(wrk_x, wrk_y);
						Units[m].EmergencyFlags[0]=100+Random(250);
	
//						unit[m].em_flg[1]=n;
						return;
						}
					}
				}
			}

#if false
		else
			{
			if( unit[unit[m].em_flg[1]].found==0  )
				{
				if( rnd(10)==0 )
					{
					unit[m].em_flg[1]=0;
					}
				else
					{
					unit[m].em_x=unit[unit[m].em_flg[1]].info[7]+(400-rnd(800));
					unit[m].em_y=unit[unit[m].em_flg[1]].info[8]+(400-rnd(800));
					unit[m].em_flg[0]=200+rnd(100);
					}
				return;
				}
			else
				{
				unit[m].em_flg[1]=0;	
				}
			}
#endif


		}


	ReturnIntoWorld(m);

	}
}
