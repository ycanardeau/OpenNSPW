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

// Port of etc3.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{









//============================================================================
//	マイ乱数を作ります
//----------------------------------------------------------------------------
[Original("make_my_rnd")]
public void	MakeSharedRandomTable()
	{
	short	i;

	for(i=0;i<4096;i++)
		{
		SharedRandomTable[i]=Random(65536);
		}

	SharedRandomIndex=0;
	}


//============================================================================
//	
//----------------------------------------------------------------------------
[Original("my_rnd")]
public int		SharedRandom(int r)
	{
	SharedRandomIndex++;
	SharedRandomIndex%=4096;

	return(SharedRandomTable[SharedRandomIndex]%r);
	}



//============================================================================
//	
//----------------------------------------------------------------------------

[Original("rnd")]
public int		Random(int x)
	{


	RandomCount++;

	return(rand()%(x));
	}



//============================================================================
// スプライト基礎データ
//----------------------------------------------------------------------------
[Original("set_sprt_data")]
public void		InitializeSprites()
	{
	
	// タイトル


	Sprites[TTL_BACK].wd=699;//599;
	Sprites[TTL_BACK].ht=384;//387;
	Sprites[TTL_BACK].base_x=0;
	Sprites[TTL_BACK].base_y=3940;
/*
#define	TTL_BACK			0
#define	UNIT_JPN			1
#define	UNIT_USA			2
#define UNIT_INFO_JPN		3
#define UNIT_INFO_USA		4
#define MAP_TIP_NRML		5
#define SUB_UNIT			6
#define BTN_1				7
#define BTN_2				8
#define BTN_BASE			9

*/
	// ユニット
	Sprites[UNIT_JPN].wd=80;
	Sprites[UNIT_JPN].ht=80;
	Sprites[UNIT_JPN].base_x=0;
	Sprites[UNIT_JPN].base_y=1759;
	Sprites[UNIT_JPN].os_of_x=8;
	Sprites[UNIT_JPN].cx=40;
	Sprites[UNIT_JPN].cy=40;

	Sprites[UNIT_USA].wd=80;
	Sprites[UNIT_USA].ht=80;
	Sprites[UNIT_USA].base_x=0;
	Sprites[UNIT_USA].base_y=4550;
	Sprites[UNIT_USA].os_of_x=8;
	Sprites[UNIT_USA].cx=40;
	Sprites[UNIT_USA].cy=40;

	// サブユニット
	Sprites[SUB_UNIT].wd=40;
	Sprites[SUB_UNIT].ht=40;
	Sprites[SUB_UNIT].base_x=0;
	Sprites[SUB_UNIT].base_y=3520;
	Sprites[SUB_UNIT].os_of_x=12;
	Sprites[SUB_UNIT].cx=20;
	Sprites[SUB_UNIT].cy=20;

	// マップチップ 雲
	Sprites[MAP_TIP_NRML].wd=80;
	Sprites[MAP_TIP_NRML].ht=80;
	Sprites[MAP_TIP_NRML].base_x=0;
	Sprites[MAP_TIP_NRML].base_y=3000;
	Sprites[MAP_TIP_NRML].os_of_x=6;
	Sprites[MAP_TIP_NRML].cx=40;
	Sprites[MAP_TIP_NRML].cy=40;

	// ユニットインフォ
	Sprites[UNIT_INFO_JPN].x=CMBT_WIDTH;
	Sprites[UNIT_INFO_JPN].y=0;
	Sprites[UNIT_INFO_JPN].wd=120;
	Sprites[UNIT_INFO_JPN].ht=438;
	Sprites[UNIT_INFO_JPN].base_x=0;
	Sprites[UNIT_INFO_JPN].base_y=0;
	Sprites[UNIT_INFO_JPN].os_of_x=6;
	Sprites[UNIT_INFO_JPN].cx=0;
	Sprites[UNIT_INFO_JPN].cy=0;

	Sprites[UNIT_INFO_USA].wd=120;
	Sprites[UNIT_INFO_USA].ht=438;
	Sprites[UNIT_INFO_USA].base_x=0;
	Sprites[UNIT_INFO_USA].base_y=878;
	Sprites[UNIT_INFO_USA].os_of_x=6;
	Sprites[UNIT_INFO_USA].cx=0;
	Sprites[UNIT_INFO_USA].cy=0;

	// 操作ボタンベース
	Sprites[BTN_BASE].wd=198;
	Sprites[BTN_BASE].ht=120;
	Sprites[BTN_BASE].base_x=361;
	Sprites[BTN_BASE].base_y=3250;
	Sprites[BTN_BASE].os_of_x=1;
	Sprites[BTN_BASE].cx=0;
	Sprites[BTN_BASE].cy=0;

	// 操作ボタンベース
	Sprites[BTN_1].wd=140;
	Sprites[BTN_1].ht=20;
	Sprites[BTN_1].base_x=0;
	Sprites[BTN_1].base_y=3250;
	Sprites[BTN_1].os_of_x=1;
	Sprites[BTN_1].cx=0;
	Sprites[BTN_1].cy=0;

	// マップベース
	Sprites[MAP_BASE].wd=256-1;
	Sprites[MAP_BASE].ht=200-1;
	Sprites[MAP_BASE].base_x=0;
	Sprites[MAP_BASE].base_y=4340;
	Sprites[MAP_BASE].os_of_x=1;
	Sprites[MAP_BASE].cx=0;
	Sprites[MAP_BASE].cy=0;


	}



//============================================================================
// 
//----------------------------------------------------------------------------
[Original("cloud_in_start")]
public void	InitializeClouds()
	{
	int	m,n; Array16<int> ok = default;	
	double	base_x,base_y,sub_x;



	while(true)
		{
		// 空いてるくもスプライトを探します。
		n=0;
		for(m=0; m<KUMO_MAX; m++)
			{
			if( Clouds[m].Used==0 )
				{
				ok[n]=m;
				n++;
				}
			if(n>=16)
				break;
			}

		if( n<=14 )
			return;



		base_x=(double)(Random(abs(MAP_RIGHT)+abs(MAP_LEFT))-abs(MAP_LEFT));
		base_y=(double)(Random(abs(MAP_TOP)+abs(MAP_BOTTOM))-abs(MAP_BOTTOM));


		//base_y=(double)(MAP_BOTTOM+300);
		sub_x=(double)( Random(Sprites[MAP_TIP_NRML].wd*4)-Sprites[MAP_TIP_NRML].wd*2 );

		n=0;	
		for( m=0;m<3;m++)
			{
			Clouds[ok[n]].Used=1;
			Clouds[ok[n]].Position = new WorldPosition(/*MAP_RIGHT*/base_x+m*80+sub_x, base_y);
			Clouds[ok[n]].Kind=1;
			n++;
			}
		sub_x=(double)( Random(Sprites[MAP_TIP_NRML].wd*2)-Sprites[MAP_TIP_NRML].wd*2 );
		for( m=0;m<5;m++)
			{
			Clouds[ok[n]].Used=1;
			Clouds[ok[n]].Position = new WorldPosition(/*MAP_RIGHT*/base_x+m*80-80+sub_x, base_y+80);
			Clouds[ok[n]].Kind=1;
			n++;
			}
		sub_x=(double)( Random(Sprites[MAP_TIP_NRML].wd*2)-Sprites[MAP_TIP_NRML].wd*2 );
		for( m=0;m<3;m++)
			{
			Clouds[ok[n]].Used=1;
			Clouds[ok[n]].Position = new WorldPosition(/*MAP_RIGHT*/base_x+m*80+sub_x, base_y+80*2);
			Clouds[ok[n]].Kind=1;
			n++;
			}

		}


	}




//============================================================================
// 
//----------------------------------------------------------------------------
[Original("cloud_cont")]
public void	UpdateClouds()
	{
	int	m,n; Array16<int> ok = default;	
	double	base_y,sub_x;



	
	for(n=0;n<KUMO_MAX;n++)
		{
		if( Clouds[n].Used!=0 )
			{
			if( Result==GameResult.None )
				{
				Clouds[n].Position = new WorldPosition(Clouds[n].Position.X - 2.0/*2.0*/, Clouds[n].Position.Y);
				if( Clouds[n].Position.X < MAP_LEFT )
					Clouds[n].Used=0;
				}
			}
		}





/*
	if( !((cc_count%200)==99) )
		return;
*/




	// 空いてるくもスプライトを探します。
	n=0;
	for(m=0; m<KUMO_MAX; m++)
		{
		if( Clouds[m].Used==0 )
			{
			ok[n]=m;
			n++;
			}
		if(n>=16)
			break;
		}

	if( n<=14 )
		return;




	base_y=(double)(Random(abs(MAP_TOP)+abs(MAP_BOTTOM))-abs(MAP_BOTTOM));
	//base_y=(double)(MAP_BOTTOM+300);
	sub_x=(double)( Random(Sprites[MAP_TIP_NRML].wd*4)-Sprites[MAP_TIP_NRML].wd*2 );
	n=0;	
	for( m=0;m<3;m++)
		{
		Clouds[ok[n]].Used=1;
		Clouds[ok[n]].Position = new WorldPosition(MAP_RIGHT+m*80+sub_x, base_y);
		Clouds[ok[n]].Kind=1;
		n++;
		}
	sub_x=(double)( Random(Sprites[MAP_TIP_NRML].wd*2)-Sprites[MAP_TIP_NRML].wd*2 );
	for( m=0;m<5;m++)
		{
		Clouds[ok[n]].Used=1;
		Clouds[ok[n]].Position = new WorldPosition(MAP_RIGHT+m*80-80+sub_x, base_y+80);
		Clouds[ok[n]].Kind=1;
		n++;
		}
	sub_x=(double)( Random(Sprites[MAP_TIP_NRML].wd*2)-Sprites[MAP_TIP_NRML].wd*2 );
	for( m=0;m<3;m++)
		{
		Clouds[ok[n]].Used=1;
		Clouds[ok[n]].Position = new WorldPosition(MAP_RIGHT+m*80+sub_x, base_y+80*2);
		Clouds[ok[n]].Kind=1;
		n++;
		}


	}











//============================================================================
// 
//----------------------------------------------------------------------------
[Original("cont_fire")]
public void	UpdateFires()
	{
	RECT	wrk_rect,wrk_r;
	int		m,f,h,cl,n=default /* C4701 */,cm_scrn_x,cm_scrn_y,i;
	double	wrk_x,wrk_y;


	//=========		 ファイアの制御		=========//
	// 弾丸、爆弾等の機動、炸裂を制御します。
	for(m=1;m<FIRE_MAX/*255*/;m++)
		{
		if( Fires[m].Target!=0 )
			{
			//	揚陸艇
			if( Fires[m].Kind==FireKind.CargoAirBase || Fires[m].Kind==FireKind.CargoNavalBase || Fires[m].Kind==FireKind.CargoInfantryBase || Fires[m].Kind==FireKind.CargoPillboxes || Fires[m].Kind==FireKind.CargoFortress )
				{
				if( Fires[m].info[0]<=Fires[m].info[1] )
					{
					wrk_x=Fires[m].Position.X;
					wrk_y=Fires[m].Position.Y;
					Fires[m].Position += new WorldVector(cos(Fires[m].Direction*a_PI)*Fires[m].Speed, sin(Fires[m].Direction*a_PI)*Fires[m].Speed);
					Fires[m].Speed+=Fires[m].Acceleration;
			

					h=0;

					// ptin dbg
					wrk_r.top=(int)Fires[m].info[7]+20;//(int)fire[m].info[7]-20;
					wrk_r.right=(int)Fires[m].info[6]+20;
					wrk_r.bottom=(int)Fires[m].info[7]-20;//(int)fire[m].info[7]+20;
					wrk_r.left=(int)Fires[m].info[6]-20;
					if( PointInRect3(ref wrk_r,(int)Fires[m].Position.X,(int)Fires[m].Position.Y)!=0 )
						h=1;

					//n=fire[m].used;

					if( h!=0 )
						{
						h=1;
						// 上陸地点到達
						for(n=1;n<=MaxUnitId && h!=0 ;n++)
							{
							if( Units[n].Side!=0 && Units[n].Kind>=UnitKind.AirBase && Units[n].Kind<=UnitKind.Fortress )
								{
								// ptin dbg
								wrk_r.top=(int)Units[n].Position.Y+20;//(int)unit[n].y-20;
								wrk_r.right=(int)Units[n].Position.X+20;
								wrk_r.bottom=(int)Units[n].Position.Y-20;//(int)unit[n].y+20;
								wrk_r.left=(int)Units[n].Position.X-20;
								if( PointInRect3(ref wrk_r,(int)Fires[m].Position.X,(int)Fires[m].Position.Y)!=0 )
									{
									// とにかくほかの地上施設の上
									h=0;		
									}
								}
							}

						if(h!=0)
							{
							//set_new_unit(fire[m].info[8], fire[m].kind-17 /*GF1*/,(double)fire[m].info[6],(double)fire[m].info[7],0);
							switch(Fires[m].Kind)
								{
								case FireKind.CargoInfantryBase:
									f=AddUnit((Side)Fires[m].info[8], UnitKind.InfantryBase,(double)Fires[m].info[6],(double)Fires[m].info[7],0);
									Units[f].info[0]=0;												// 建設期間
									break;
								case FireKind.CargoPillboxes:
									f=AddUnit((Side)Fires[m].info[8], UnitKind.Pillboxes,(double)Fires[m].info[6],(double)Fires[m].info[7],0);
									Units[f].info[0]=(int)(8000*( Fires[m].info[8]==(int)Side.UnitedStates ? 1.0 : 0.8 ));		// 建設期間
									Units[f].Hp/=4;
									Units[f].MaxHp/=4;
									break;
								case FireKind.CargoFortress:
									f=AddUnit((Side)Fires[m].info[8], UnitKind.Fortress,(double)Fires[m].info[6],(double)Fires[m].info[7],0);
									Units[f].info[0]=(int)(15000*( Fires[m].info[8]==(int)Side.UnitedStates ? 0.8 : 1.0 ));		// 建設期間
									Units[f].Hp/=4;
									Units[f].MaxHp/=4;
									break;
								case FireKind.CargoAirBase:
									f=AddUnit((Side)Fires[m].info[8], UnitKind.AirBase,(double)Fires[m].info[6],(double)Fires[m].info[7],0);
									Units[f].info[0]=(int)(10000*( Fires[m].info[8]==(int)Side.UnitedStates ? 0.8 : 1.0 ));		// 建設期間
									Units[f].Hp/=4;
									Units[f].MaxHp/=4;
									break;
								case FireKind.CargoNavalBase:
									f=AddUnit((Side)Fires[m].info[8], UnitKind.NavalBase,(double)Fires[m].info[6],(double)Fires[m].info[7],0);
									Units[f].info[0]=(int)(20000*( Fires[m].info[8]==(int)Side.UnitedStates ? 0.8 : 1.0 ));		// 建設期間
									Units[f].Hp/=4;
									Units[f].MaxHp/=4;
									break;
								}
							}
						Fires[m].Target=0;


						}
					else
						{
						// 当たってないので
						Fires[m].info[0]++;
						if( Fires[m].info[0]==1 )
							{
							f=FindFreeEffect();
							Effects[f].Layer=EffectLayer.Lower;	
							Effects[f].info[0]=30;
							Effects[f].info[1]=0;

							Effects[f].Position=Fires[m].Position;

							Effects[f].SpriteNumber=8;			// ソースファイル上の番号
							}


						if( Fires[m].info[0]>=Fires[m].info[2] /*&& paint_effect_on*/ )
							{
							if(!( Fires[m].Position.Y>MAP_TOP || Fires[m].Position.Y<MAP_BOTTOM || Fires[m].Position.X<MAP_LEFT || Fires[m].Position.X>MAP_RIGHT ))
								{
								cm_scrn_x=(int)((Fires[m].Position.X+(Sprites[UNIT_JPN].wd/2)-MAP_LEFT)/Sprites[MAP_TIP_NRML].wd);
								cm_scrn_y=(int)((MAP_TOP-Fires[m].Position.Y+(Sprites[UNIT_JPN].ht/2))/Sprites[MAP_TIP_NRML].ht);

								if( MapTiles[cm_scrn_y][cm_scrn_x]==0)
									{
									// 海の上
									if( (Tick%5)==0)
										{
										f=FindFreeEffect();
										Effects[f].Layer=EffectLayer.Lower;	
										Effects[f].info[0]=30+SharedRandom(25);
										Effects[f].info[1]=4;

										Effects[f].Position = new WorldPosition(wrk_x, wrk_y);

										Effects[f].SpriteNumber=8;			// ソースファイル上の番号
										}
									}
								}

							f=FindFreeEffect();
							Effects[f].Layer=EffectLayer.Lower;	
							Effects[f].info[0]=1;
							Effects[f].info[1]=0;

							Effects[f].Position=Fires[m].Position;

							Effects[f].SpriteNumber=36+ToEightDirections((int)(Fires[m].Direction));			// ソースファイル上の番号

							}
						}
					//effect[f].no=72;			// ソースファイル上の番号
					}
				else
					{
					Fires[m].Target=0;
					}
				}






			// 弾丸
			if( Fires[m].Kind==FireKind.Bullet )
				{
				if( Fires[m].Speed>=Fires[m].FinalSpeed )
					{
					wrk_x=Fires[m].Position.X;
					wrk_y=Fires[m].Position.Y;
					Fires[m].Position += new WorldVector(cos(Fires[m].Direction*a_PI)*Fires[m].Speed, sin(Fires[m].Direction*a_PI)*Fires[m].Speed);
					Fires[m].Speed+=Fires[m].Acceleration;

					n=Fires[m].Target;						// ターゲットナンバー

					// ptin dbg
					wrk_rect.top=(int)Units[n].Position.Y+10;//(int)unit[n].y-10;
					wrk_rect.right=(int)Units[n].Position.X+10;
					wrk_rect.bottom=(int)Units[n].Position.Y-10;//(int)unit[n].y+10;
					wrk_rect.left=(int)Units[n].Position.X-10;

#if true
					if( PointInRect3(ref wrk_rect,(int)Fires[m].Position.X,(int)Fires[m].Position.Y)!=0 )
						{
						// 命中
						Fires[m].Target=0;
						if(ShowsAntiAir==0)
							{
							if( Units[n].Kind!=UnitKind.Transport || Random(4)==0 ) 
								Units[n].Hp-=GetDamagePoints(m);
							}

						f=FindFreeEffect();
						Effects[f].Layer=EffectLayer.Upper;	

						Effects[f].info[0]=20;
						Effects[f].info[1]=1;	// アニメーションパターン
						//effect[f].kind=THERE;

						Effects[f].Position=Fires[m].Position;
						Effects[f].SpriteNumber=1;			// 弾丸着弾	のソースファイル上の番号
						}
					else
						{
						if( 1!=0/*paint_effect_on*/ )
							{
							// 弾丸描画
							f=FindFreeEffect();
							Effects[f].Layer=EffectLayer.Upper;	
							Effects[f].info[0]=1;	Effects[f].info[1]=11;
							Effects[f].Position=Fires[m].Position;
							Effects[f].EndPosition = new WorldPosition(wrk_x, wrk_y);
							}
						}
#endif

					}
				else
					{
					Fires[m].Target=0;
					}
				}




			// 対空機関砲
			if( Fires[m].Kind==FireKind.RapidAntiAircraftShell )
				{
				Fires[m].info[0]--;
				if( Fires[m].info[0]!=0/*fire[m].info[1] <= fire[m].info[0]*/ )
					{	//
					wrk_x=Fires[m].Position.X;	wrk_y=Fires[m].Position.Y;
					Fires[m].Position += new WorldVector(cos(Fires[m].Direction*a_PI)*Fires[m].Speed, sin(Fires[m].Direction*a_PI)*Fires[m].Speed);

					Fires[m].Speed+=Fires[m].Acceleration;		// 弾が減速

					if( 1!=0/*paint_effect_on*/ )
						{
						f=FindFreeEffect();
						Effects[f].Layer=EffectLayer.Upper;	
						Effects[f].info[0]=1;	Effects[f].info[1]=10;
						Effects[f].Position=Fires[m].Position;
						Effects[f].EndPosition = new WorldPosition(wrk_x, wrk_y);

						Effects[f].SpriteNumber=0;			// ソースファイル上の番号
						}

					//draw_line5((int)(fire[m].x-cmbt_x),(int)(cmbt_y-fire[m].y),(int)(wrk_x-cmbt_x),(int)(cmbt_y-wrk_y),CMBT_WIDTH-1,CMBT_HEIGHT-1,255);
					//draw_line5((int)(fire[m].x-cmbt_x)+1,(int)(cmbt_y-fire[m].y),(int)(wrk_x-cmbt_x)+1,(int)(cmbt_y-wrk_y),CMBT_WIDTH-1,CMBT_HEIGHT-1,255);
					//draw_line5((int)(fire[m].x-cmbt_x),(int)(cmbt_y-fire[m].y)-1,(int)(wrk_x-cmbt_x),(int)(cmbt_y-wrk_y)-1,CMBT_WIDTH-1,CMBT_HEIGHT-1,255);

					}
				else
					{	// 炸裂！
					Fires[m].Position += new WorldVector(cos(Fires[m].Direction*a_PI)*Fires[m].Speed, sin(Fires[m].Direction*a_PI)*Fires[m].Speed);


					// 砲弾炸裂
					f=FindFreeEffect();
					Effects[f].Layer=EffectLayer.Lower;	

					Effects[f].info[0]=10;
					Effects[f].info[1]=4;	// アニメーションパターン

					Effects[f].Position=Fires[m].Position;
					Effects[f].SpriteNumber=0;			// 弾丸着弾	のソースファイル上の番号



					// ptin dbg
					wrk_rect.top=(int)Fires[m].Position.Y+40;//(int)fire[m].y-40;
					wrk_rect.right=(int)Fires[m].Position.X+40;
					wrk_rect.bottom=(int)Fires[m].Position.Y-40;//(int)fire[m].y+40;
					wrk_rect.left=(int)Fires[m].Position.X-40;


					for(n=1;n<=MaxUnitId;n++)
						{
						if( Units[n].Side!=0 && Units[n].Side==Units[Fires[m].Target].Side && Units[n].Category==UnitCategory.Plane && Units[n].PlaneState==UnitState.Flying )
							{
							if( PointInRect3(ref wrk_rect,(int)Units[n].Position.X,(int)Units[n].Position.Y)!=0 /*&& unit[n].hp[0]>=unit[m].hp[1]*0.2+1*/ )
								{
								// 命中
								Fires[m].Target=0;
								if(ShowsAntiAir==0 /*&& unit[n].kind==AT1*/ )
									{
									Units[n].Hp-=GetDamagePoints(m);
									if(Units[n].Speed<=Units[n].MaxSpeed )
										Units[n].Direction=Random(360);
									}

								f=FindFreeEffect();
								Effects[f].Layer=EffectLayer.Upper;	

								Effects[f].info[0]=20;
								Effects[f].info[1]=2;	// アニメーションパターン
								//effect[f].kind=THERE;

								Effects[f].Position=Units[n].Position;
								Effects[f].SpriteNumber=1;			// 弾丸着弾	のソースファイル上の番号
								}
							}
						}
					Fires[m].Target=0;
					}
				}










			// 対潜水艦爆弾
			if( Fires[m].Kind==FireKind.AntiSubmarineBomb )
				{
				Fires[m].info[0]++;
				if( Fires[m].info[0]==5 )
					{
					f=FindFreeEffect();
					Effects[f].Layer=EffectLayer.Lower;	
					Effects[f].info[0]=30;
					Effects[f].info[1]=4;

					Effects[f].Position=Fires[m].Position;

					Effects[f].SpriteNumber=7;			// ソースファイル上の番号
					}
				if( Fires[m].info[0]==Fires[m].info[1] )
					{	// バクハツ！
					//unit[n].hp[0]--;

					f=FindFreeEffect();

					PlaySoundEffect( 0, TPD_HIT1 ,Fires[m].Position.X, Fires[m].Position.Y);

					Effects[f].Layer=EffectLayer.Lower;	

					Effects[f].info[0]=40;
					Effects[f].info[1]=4;	// アニメーションパターン

					Effects[f].Position=Fires[m].Position;
					Effects[f].SpriteNumber=11;			//ソースファイル上の番号

					h=0;
					for(n=1;n<=MaxUnitId;n++)
						{
						if( Units[n].Side!=0 && Units[n].Kind==UnitKind.Submarine && Units[n].info[6]!=0 )
							{
							Fires[m].Target=n;
							h=CheckHit(m);
							if( h!=0 )
								break;
							}
						}

					if( h!=0 )
						{
						// 命中
						Units[n].Hp-=GetDamagePoints(m);
						}
					Fires[m].Target=0;
					}
				}


			// 対空砲
			if( Fires[m].Kind==FireKind.AntiAircraftShell )
				{
				Fires[m].info[0]--;
				if( Fires[m].info[0]!=0/*fire[m].info[1] <= fire[m].info[0]*/ )
					{	//
					wrk_x=Fires[m].Position.X;	wrk_y=Fires[m].Position.Y;
					Fires[m].Position += new WorldVector(cos(Fires[m].Direction*a_PI)*Fires[m].Speed, sin(Fires[m].Direction*a_PI)*Fires[m].Speed);

					Fires[m].Speed+=Fires[m].Acceleration;		// 弾が減速
					if( 1!=0/*paint_effect_on*/ )
						{
						// 弾自体の絵
						f=FindFreeEffect();
						Effects[f].Layer=EffectLayer.Upper;	
						Effects[f].info[0]=1;	Effects[f].info[1]=0;
						Effects[f].Position=Fires[m].Position;
						Effects[f].SpriteNumber=96+ToEightDirections((int)(Fires[m].Direction));			// ソースファイル上の番号
						// 弾の煙
						f=FindFreeEffect();
						Effects[f].Layer=EffectLayer.Upper;	
						Effects[f].info[0]=3+SharedRandom(3);
						Effects[f].info[1]=4;

						Effects[f].Position = new WorldPosition(wrk_x+SharedRandom(10)-5, wrk_y+SharedRandom(10)-5);

						Effects[f].SpriteNumber=2;			// ソースファイル上の番号
						}
					}
				else
					{	// 炸裂！
					Fires[m].Position += new WorldVector(cos(Fires[m].Direction*a_PI)*Fires[m].Speed, sin(Fires[m].Direction*a_PI)*Fires[m].Speed);





					// 砲弾炸裂
					// 煙
					f=FindFreeEffect();
					Effects[f].Layer=EffectLayer.Lower;	
					//effect[f].kind=THERE;
					Effects[f].info[0]=80+Random(80);
					Effects[f].info[1]=2;
					Effects[f].Position=Fires[m].Position;
					Effects[f].SpriteNumber=6;			// ソースファイル上の番号

					// 漠炎
					f=FindFreeEffect();
					Effects[f].Layer=EffectLayer.Lower;	
					Effects[f].info[0]=10;
					Effects[f].info[1]=4;	// アニメーションパターン
					Effects[f].Position=Fires[m].Position;
					Effects[f].SpriteNumber=0;			// 弾丸着弾	のソースファイル上の番号






					//fire[m].used=0;
					f=40;
					// ptin dbg
					wrk_rect.top=(int)Fires[m].Position.Y+f;//(int)fire[m].y-f;
					wrk_rect.right=(int)Fires[m].Position.X+f;
					wrk_rect.bottom=(int)Fires[m].Position.Y-f;//(int)fire[m].y+f;
					wrk_rect.left=(int)Fires[m].Position.X-f;


					for(n=1;n<=MaxUnitId;n++)
						{
						if( Units[n].Side!=0 && Units[n].Side==Units[Fires[m].Target].Side && Units[n].Category==UnitCategory.Plane && Units[n].PlaneState==UnitState.Flying )
							{
							if( PointInRect3(ref wrk_rect,(int)Units[n].Position.X,(int)Units[n].Position.Y)!=0 && !( Units[n].Kind==UnitKind.Bomber && Units[n].Side==Side.UnitedStates && Random(3)!=0 ) )
								{
								// 命中
								/*fire[m].used=0;*/
								if(ShowsAntiAir==0  )
									{
									Units[n].Hp-=GetDamagePoints(m);
	
									if(Units[n].Speed<=Units[n].MaxSpeed )
										{
										Units[n].Direction=Random(360);
										//unit[n].spd=unit[n].spd/3;
										}

									}

								f=FindFreeEffect();
								Effects[f].Layer=EffectLayer.Upper;	

								Effects[f].info[0]=20;
								Effects[f].info[1]=2;	// アニメーションパターン
								//effect[f].kind=THERE;

								Effects[f].Position=Units[n].Position;
								Effects[f].SpriteNumber=1;			// 弾丸着弾	のソースファイル上の番号
								}
							}
						}
					Fires[m].Target=0;
					}
				}






			// 艦砲
			if( Fires[m].Kind==FireKind.Gun )
				{
				Fires[m].info[0]--;
				if( Fires[m].info[0]!=0 )
					{	//
					wrk_x=Fires[m].Position.X;	wrk_y=Fires[m].Position.Y;
					Fires[m].Position += new WorldVector(cos(Fires[m].Direction*a_PI)*Fires[m].Speed, sin(Fires[m].Direction*a_PI)*Fires[m].Speed);
					if( Fires[m].info[0] > Fires[m].info[1] ) 
						Fires[m].Speed-=Fires[m].Acceleration;		// 弾が上昇中
					else
						Fires[m].Speed+=(Fires[m].Acceleration*2.83);		// 弾が降下中
					if( 1!=0/*paint_effect_on*/ )
						{
						// 弾自体の絵
						f=FindFreeEffect();
						Effects[f].Layer=EffectLayer.Upper;	
						Effects[f].info[0]=1;	Effects[f].info[1]=0;
						Effects[f].Position=Fires[m].Position;
						Effects[f].SpriteNumber=108+ToEightDirections((int)(Fires[m].Direction));			// ソースファイル上の番号
						// 弾の煙
						f=FindFreeEffect();
						Effects[f].Layer=EffectLayer.Upper;	
						Effects[f].info[0]=3+SharedRandom(3);
						Effects[f].info[1]=4;

						Effects[f].Position = new WorldPosition(wrk_x+SharedRandom(10)-5, wrk_y+SharedRandom(10)-5);

						Effects[f].SpriteNumber=2;			// ソースファイル上の番号
						}
					}
				else
					{	// 着弾！
					Fires[m].Position += new WorldVector(cos(Fires[m].Direction*a_PI)*Fires[m].Speed, sin(Fires[m].Direction*a_PI)*Fires[m].Speed);
					n=Fires[m].Target;
					h=CheckHit(m);
					Fires[m].Target=0;
					if( Units[n].Side!=0 && h!=0)
						{
						// 命中
						Units[n].Hp-=GetDamagePoints(m);

						PlaySoundEffect( 0, TPD_HIT1 ,Fires[m].Position.X, Fires[m].Position.Y);

						f=FindFreeEffect();
						Effects[f].Layer=EffectLayer.Upper;	

						Effects[f].info[0]=40;
						Effects[f].info[1]=4;	// アニメーションパターン

						Effects[f].Position=Fires[m].Position;

						//effect[f].x=unit[n].x;
						//effect[f].y=unit[n].y;
						Effects[f].SpriteNumber=1;			// 弾丸着弾	のソースファイル上の番号


						// 当った的に収納機があれば破壊される場合もある
						if( Units[n].Kind==UnitKind.AirBase || Units[n].Kind==UnitKind.Carrier || Units[n].Kind==UnitKind.LightCarrier )
							{
							for(i=0;i<=MaxUnitId;i++)
								{
								if( Units[i].Side!=0 && Units[i].Category==UnitCategory.Plane && Units[i].PlaneState==UnitState.Parked && Units[i].info[1]==n && Random(10)==0 )
									{
									Units[i].Side=0;
									Units[Units[i].info[1]].info[1]--;	// 現在格納数
							
									if( Units[i].info[3]>=1 && Units[Units[i].info[1]].info[4]>=1 && Units[i].Mode<=UnitMode.Slow )
										Units[Units[i].info[1]].info[4]--;		// 発艦予定の機数を	
									if( Units[i].info[3]>=3 && Units[Units[i].info[1]].info[7]>=1  && Units[i].Mode<=UnitMode.Slow )
										Units[Units[i].info[1]].info[7]--;		// 


									if( Units[i].Mode==UnitMode.Return )
										{
										if(Units[Units[i].info[1]].info[7]!=0)
											Units[Units[i].info[1]].info[7]=0;	// 着艦、0許可、1不許可
										if(Units[Units[i].info[1]].info[8]!=0)
											Units[Units[i].info[1]].info[8]=0;	// その空母の次機発進許可	0許可、1不許可
										}

									if( SelectedUnit==i )
										{ SelectedUnit=0; CombatMenuKind=0; CombatMenuSelection=CombatMenuItem.None; ClearSelection2(1); }

									break;
									}
								}

							}

						}
					else
						{	
						// ハズレ
						PlaySoundEffect( 0, SPL1 ,Fires[m].Position.X, Fires[m].Position.Y);

						f=FindFreeEffect();
						Effects[f].Layer=EffectLayer.Lower;	

						Effects[f].info[0]=40;
						Effects[f].info[1]=4;	// アニメーションパターン

						Effects[f].Position=Fires[m].Position;

						wrk_x=Fires[m].Position.X;
						wrk_y=Fires[m].Position.Y;

						Effects[f].SpriteNumber=8;			// 弾丸着弾	のソースファイル上の番号
						if(!( wrk_y>MAP_TOP || wrk_y<MAP_BOTTOM || wrk_x<MAP_LEFT || wrk_x>MAP_RIGHT ))
							{
							cm_scrn_x=(int)((wrk_x+(Sprites[UNIT_JPN].wd/2)-MAP_LEFT)/Sprites[MAP_TIP_NRML].wd);
							cm_scrn_y=(int)((MAP_TOP-wrk_y+(Sprites[UNIT_JPN].ht/2))/Sprites[MAP_TIP_NRML].ht);
							if( MapTiles[cm_scrn_y][cm_scrn_x]>=1)
								Effects[f].SpriteNumber=10;			// 弾丸着弾	のソースファイル上の番号
							else
								Effects[f].SpriteNumber=8;			// 弾丸着弾	のソースファイル上の番号
							}

						}
					}
				}


			//	魚雷
			if( Fires[m].Kind==FireKind.Torpedo )
				{
				if( Fires[m].info[0]<=Fires[m].info[1] )
					{
					wrk_x=Fires[m].Position.X;
					wrk_y=Fires[m].Position.Y;
					wrk_x+=cos(Fires[m].Direction*a_PI)*-40;
					wrk_y+=sin(Fires[m].Direction*a_PI)*-40;
					if(!( wrk_y>MAP_TOP || wrk_y<MAP_BOTTOM || wrk_x<MAP_LEFT || wrk_x>MAP_RIGHT ))
						{
						cm_scrn_x=(int)((wrk_x+(Sprites[UNIT_JPN].wd/2)-MAP_LEFT)/Sprites[MAP_TIP_NRML].wd);
						cm_scrn_y=(int)((MAP_TOP-wrk_y+(Sprites[UNIT_JPN].ht/2))/Sprites[MAP_TIP_NRML].ht);
						if( MapTiles[cm_scrn_y][cm_scrn_x]>=1)
							{
							Fires[m].info[0]=Fires[m].info[1];
							}
						}



					wrk_x=Fires[m].Position.X;
					wrk_y=Fires[m].Position.Y;
					Fires[m].Position += new WorldVector(cos(Fires[m].Direction*a_PI)*Fires[m].Speed, sin(Fires[m].Direction*a_PI)*Fires[m].Speed);
					Fires[m].Speed+=Fires[m].Acceleration;
			


					h=0;
					if( Fires[m].info[0]>=Fires[m].info[2] )
						{
						for(n=1;n<=MaxUnitId;n++)
							{
							if( Units[n].Side!=0 && Units[n].Category==UnitCategory.Ship && Units[n].Kind>=UnitKind.Battleship && Units[n].Kind<=UnitKind.Transport && !(Units[n].Kind==UnitKind.Submarine && Units[n].info[6]!=0))
								{
								Fires[m].Target=n;
								h=CheckHit(m);
								if( h!=0 )
									break;
								}
							}
						}
			
					if( h!=0 )
						{
						// 命中
						Fires[m].Target=0;
						Units[n].Hp-=GetDamagePoints(m);


						PlaySoundEffect( 0, TPD_HIT1+Random(2) ,Fires[m].Position.X, Fires[m].Position.Y);

						f=FindFreeEffect();
						Effects[f].Layer=EffectLayer.Lower;	

						Effects[f].info[0]=40;
						Effects[f].info[1]=4;	// アニメーションパターン

						Effects[f].Position=Fires[m].Position;
						Effects[f].SpriteNumber=11;			// 弾丸着弾	のソースファイル上の番号
						}
					else
						{
						// 当たってないので
						Fires[m].info[0]++;
						if( Fires[m].info[0]==1 )
							{
							f=FindFreeEffect();
							Effects[f].Layer=EffectLayer.Lower;	
							Effects[f].info[0]=30;
							Effects[f].info[1]=0;

							Effects[f].Position=Fires[m].Position;

							Effects[f].SpriteNumber=8;			// ソースファイル上の番号
							}

						if( Fires[m].info[0]>=Fires[m].info[2] /*&& paint_effect_on*/ )
							{
							f=FindFreeEffect();
							Effects[f].Layer=EffectLayer.Lower;	
							Effects[f].info[0]=1;
							Effects[f].info[1]=0;

							Effects[f].Position=Fires[m].Position;

							Effects[f].SpriteNumber=72+ToEightDirections((int)(Fires[m].Direction));			// ソースファイル上の番号
							if( (Tick%3)==0)
								{
								f=FindFreeEffect();
								Effects[f].Layer=EffectLayer.Lower;	
								Effects[f].info[0]=30+SharedRandom(25);
								Effects[f].info[1]=4;

								Effects[f].Position = new WorldPosition(wrk_x+SharedRandom(10)-5, wrk_y+SharedRandom(10)-5);

								Effects[f].SpriteNumber=8;			// ソースファイル上の番号
								}
							}
						}
					//effect[f].no=72;			// ソースファイル上の番号
					}
				else
					{
					Fires[m].Target=0;
					}
				}



			// 爆撃			
			if( Fires[m].Kind==FireKind.Bomb )
				{
				Fires[m].info[0]++;

				if( Fires[m].Target==(int)UnitKind.Attacker && Fires[m].info[0]==12 )
					PlaySoundEffect( 0, BOMB_OFF ,Fires[m].Position.X, Fires[m].Position.Y);


				if( Fires[m].info[0]<=Fires[m].info[1]  )
					{	// 爆弾降下中
					if( Fires[m].info[0]>=50)
						{
						if( Fires[m].info[0]==50)
						PlaySoundEffect( 0, FALL1 ,Fires[m].Position.X, Fires[m].Position.Y);

						Fires[m].Position += new WorldVector(cos(Fires[m].Direction*a_PI)*Fires[m].Speed, sin(Fires[m].Direction*a_PI)*Fires[m].Speed);
						Fires[m].Speed+=Fires[m].Acceleration;
						if( 1!=0/*paint_effect_on*/ )
							{
							f=FindFreeEffect();
							Effects[f].Layer=EffectLayer.Upper;	
							Effects[f].info[0]=1;	Effects[f].info[1]=0;
							Effects[f].Position=Fires[m].Position;
							Effects[f].SpriteNumber=84+ToEightDirections((int)(Fires[m].Direction));			// ソースファイル上の番号
							}
						}
					}
				else
					{	// 着弾！
					Fires[m].Position += new WorldVector(cos(Fires[m].Direction*a_PI)*Fires[m].Speed, sin(Fires[m].Direction*a_PI)*Fires[m].Speed);
					h=0;
					//n=fire[m].used;
					for(n=1;n<=MaxUnitId;n++)
						{
						if( Units[n].Side!=0 && Units[n].Category==UnitCategory.Ship && !(Units[n].Kind==UnitKind.Submarine && Units[n].info[6]!=0))
							{
							Fires[m].Target=n;
							h=CheckHit(m);
							if( h!=0 )
								break;
							}
						}
					Fires[m].Target=0;
					if( h!=0 )
						{
						// 命中
						Units[n].Hp-=GetDamagePoints(m);

						PlaySoundEffect( 0, BOM_HIT1+Random(2) ,Fires[m].Position.X, Fires[m].Position.Y);

						f=FindFreeEffect();
						Effects[f].Layer=EffectLayer.Upper;	

						Effects[f].info[0]=40;
						Effects[f].info[1]=4;	// アニメーションパターン

						Effects[f].Position=Fires[m].Position;
						//effect[f].x=unit[n].x;
						//effect[f].y=unit[n].y;
						Effects[f].SpriteNumber=1;			// 弾丸着弾	のソースファイル上の番号


						// 当った的に収納機があれば破壊される場合もある
						if( Units[n].Kind==UnitKind.AirBase || Units[n].Kind==UnitKind.Carrier || Units[n].Kind==UnitKind.LightCarrier )
							{
							for(i=0;i<=MaxUnitId;i++)
								{
								if( Units[i].Side!=0 && Units[i].Category==UnitCategory.Plane && Units[i].PlaneState==UnitState.Parked && Units[i].info[1]==n && Random(10)==0 )
									{
									Units[i].Side=0;
									Units[Units[i].info[1]].info[1]--;	// 現在格納数
							
									if( Units[i].info[3]>=1 && Units[Units[i].info[1]].info[4]>=1 && Units[i].Mode<=UnitMode.Slow )
										Units[Units[i].info[1]].info[4]--;		// 発艦予定の機数を	
									if( Units[i].info[3]>=3 && Units[Units[i].info[1]].info[7]>=1  && Units[i].Mode<=UnitMode.Slow )
										Units[Units[i].info[1]].info[7]--;		// 


									if( /*unit[i].info[3]==1 &&*/ Units[i].Mode==UnitMode.Return )
										{
										if(Units[Units[i].info[1]].info[7]!=0)
											Units[Units[i].info[1]].info[7]=0;	// 着艦、0許可、1不許可
										if(Units[Units[i].info[1]].info[8]!=0)
											Units[Units[i].info[1]].info[8]=0;	// その空母の次機発進許可	0許可、1不許可
										}


									if( SelectedUnit==i )
										{ SelectedUnit=0; CombatMenuKind=0; CombatMenuSelection=CombatMenuItem.None; ClearSelection2(1); }
									break;
									}
								}

							}
						}
					else
						{	
						// ハズレ

						f=FindFreeEffect();
						Effects[f].Layer=EffectLayer.Lower;	

						Effects[f].info[0]=40;
						Effects[f].info[1]=4;	// アニメーションパターン

						Effects[f].Position=Fires[m].Position;
						wrk_x=Fires[m].Position.X;
						wrk_y=Fires[m].Position.Y;
						if(!( wrk_y>MAP_TOP || wrk_y<MAP_BOTTOM || wrk_x<MAP_LEFT || wrk_x>MAP_RIGHT ))
							{
							cm_scrn_x=(int)((wrk_x+(Sprites[UNIT_JPN].wd/2)-MAP_LEFT)/Sprites[MAP_TIP_NRML].wd);
							cm_scrn_y=(int)((MAP_TOP-wrk_y+(Sprites[UNIT_JPN].ht/2))/Sprites[MAP_TIP_NRML].ht);
							}
						else
							{
							cm_scrn_x=0;
							cm_scrn_y=0;
							}
	
						if( MapTiles[cm_scrn_y][cm_scrn_x]>=1)
							{
							Effects[f].SpriteNumber=10;			// 着弾	のソースファイル上の番号
							PlaySoundEffect( 0, BOM_HIT1 ,Fires[m].Position.X, Fires[m].Position.Y);
							}
						else
							{
							Effects[f].SpriteNumber=8;			// 着弾	のソースファイル上の番号
							PlaySoundEffect( 0, SPL1 ,Fires[m].Position.X, Fires[m].Position.Y);
							}
						}
					}
				}
			}
		}
	}







//============================================================================
// 
//----------------------------------------------------------------------------
[Original("set_new_ltl_ldr")]
public void	AssignGroupLeader(int m)
	{
	int		n,min_no,f,i;




	// 部下がまだ生きてるか
	min_no=MaxUnitId+1;
	f=0;
	for( n=1; n<=MaxUnitId; n++)
		{
		if( Units[n].Side!=0 && Units[n].GroupLeader==m )
			{
			if( Units[min_no].FormationNumber>Units[n].FormationNumber || min_no==MaxUnitId+1 )
				min_no=n;

//			unit[n].no--;

			f++;
			}
		}



	if( f>=2 )
		{


		// 部下が生存
		Units[min_no].FormationNumber=0;
		Units[min_no].IsGroupLeader=(short)f;
		Units[min_no].GroupLeader=0;

		if( Units[min_no].Kind==UnitKind.Fighter && (Units[m].Kind==UnitKind.Attacker || Units[m].Kind==UnitKind.Bomber) && Units[min_no].PlaneState==UnitState.Flying )
			Units[min_no].Mode=UnitMode.Return;		// それまでの隊長がボスだったらきかんしよっと



		for( n=1; n<=MaxUnitId; n++)
			{
			if( Units[n].Side!=0 && Units[n].GroupLeader==m )
				{
				Units[n].GroupLeader=(short)min_no;			// ｍｉｎ＿ｎｏが新しい隊長機

				if( Units[n].Kind==UnitKind.Fighter && (Units[m].Kind==UnitKind.Attacker || Units[m].Kind==UnitKind.Bomber) && Units[n].PlaneState==UnitState.Flying )
					Units[n].Mode=UnitMode.Return;		// それまでの隊長がボスだったらきかんしよっと

				}
			}
		// ｐｐ＿ｘ、ｙをコピーします。
		for(i=0;i<=63;i++)
			{
			Units[min_no].PathX[i]=Units[m].PathX[i];
			Units[min_no].PathY[i]=Units[m].PathY[i];
			}
		}



	else if( f==1 )
		{
		// 部下がひとつ
		Units[min_no].FormationNumber=0;
		Units[min_no].IsGroupLeader=0;
		Units[min_no].GroupLeader=0;
		}
	}








//============================================================================
// 
//----------------------------------------------------------------------------
[Original("cont_unit_effect")]
public void	UpdateUnitEffects(int m)
	{
	RECT	wrk_rect;
	int		f,i,n;
	double	wrk_x2,wrk_y2,drctn,dstc;
	int		cm_scrn_x,cm_scrn_y;





	if( Units[m].IsGroupLeader!=0 && Units[m].Side==LocalSide && Units[m].PlaneState!=UnitState.Parked /*&& paint_effect_on*/ )
		{
		f=FindFreeEffect();
		Effects[f].Layer=EffectLayer.Upper;	
		Effects[f].info[0]=1;
		Effects[f].info[1]=0;
		Effects[f].Position = new WorldPosition(Units[m].Position.X, Effects[f].Position.Y);
		if( Units[m].Kind==UnitKind.Attacker || Units[m].Kind==UnitKind.Fighter || Units[m].Kind==UnitKind.Destroyer || Units[m].Kind==UnitKind.Submarine)
			Effects[f].Position = new WorldPosition(Effects[f].Position.X, Units[m].Position.Y+30.0);
		else
			Effects[f].Position = new WorldPosition(Effects[f].Position.X, Units[m].Position.Y+35.0);

		// 編隊長の旗
		Effects[f].SpriteNumber=24;			// ソースファイル上の番号
		}


	// 航空機のユニットエフェクト
	if ( Units[m].Category==UnitCategory.Plane )
		{
		// 武装の表示
		if( /*paint_effect_on &&*/ Units[m].Ammo!=0 && !(Units[m].ReloadTime!=0 && (FrameCount%3)==0)&& (Units[m].Weapon==FireKind.Bomb || Units[m].Weapon==FireKind.Torpedo || Units[m].Weapon==FireKind.Maintenance || Units[m].Weapon==FireKind.Unarmed)  && Units[m].Side==LocalSide && !(Units[m].PlaneState==UnitState.Parked && UnitInfoPanel[1]==0) && !( Units[m].PlaneState==UnitState.Parked && Units[m].info[3]>=3 ) && !( Units[m].PlaneState==UnitState.Parked && Units[m].info[1]!=UnitInfoPanel[3]))
			{
			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Lower;	
			Effects[f].info[0]=1;
			if( Units[m].PlaneState==UnitState.Flying )
				{
				Effects[f].info[1]=0;
				Effects[f].Position = new WorldPosition(Units[m].Position.X, Units[m].Position.Y-25);
				}
			else
				{
				Effects[f].info[1]=3;
				Effects[f].Position = new WorldPosition(Units[m].Position.X, Units[m].Position.Y+25);
				}
			switch( Units[m].Weapon )
				{
				case FireKind.Torpedo:
					Effects[f].SpriteNumber=3;			// ソースファイル上の番号
					break;
				case FireKind.Bomb:
					Effects[f].SpriteNumber=4;			// ソースファイル上の番号
					break;
				case FireKind.Maintenance:	case FireKind.Unarmed:
					Effects[f].SpriteNumber=15;			// ソースファイル上の番号
					break;
				}
			}


		if( Units[m].PlaneState==UnitState.Parked )
			{	// 収容後のエフェクト

			}
		else
			{	// 飛行中のエフェクト
			if( Units[m].Hp<=0 )
				{	// 墜落
				Units[m].Side=0;


				if( UnitInfoPanel[3]==m )
					UnitInfoPanel[0]=0;				// ユニットインフォをクリア


				f=FindFreeEffect();
				Effects[f].Layer=EffectLayer.Lower;	
				//effect[f].kind=THERE;
				Effects[f].info[0]=80;

				Effects[f].Position=Units[m].Position;

				wrk_x2=Units[m].Position.X;
				wrk_y2=Units[m].Position.Y;
				if(!( wrk_y2>MAP_TOP || wrk_y2<MAP_BOTTOM || wrk_x2<MAP_LEFT || wrk_x2>MAP_RIGHT ))
					{
					cm_scrn_x=(int)((wrk_x2+(Sprites[UNIT_JPN].wd/2)-MAP_LEFT)/Sprites[MAP_TIP_NRML].wd);
					cm_scrn_y=(int)((MAP_TOP-wrk_y2+(Sprites[UNIT_JPN].ht/2))/Sprites[MAP_TIP_NRML].ht);
					}
				else
					{
					cm_scrn_x=0;
					cm_scrn_y=0;
					}
	
				if( MapTiles[cm_scrn_y][cm_scrn_x]>=1)
					{
					Effects[f].SpriteNumber=9;			// ソースファイル上の番号
					Effects[f].info[1]=4;
					}
				else
					{
					Effects[f].SpriteNumber=7;			// ソースファイル上の番号
					Effects[f].info[1]=0;
					}


				if( SelectedUnit==m )
					{
					SelectedUnit=0;
					CombatMenuKind=0; CombatMenuSelection=CombatMenuItem.None; ClearSelection2(1);
					}

				if( Units[m].IsGroupLeader!=0 )
					AssignGroupLeader(m);					// 爆砕されたのがＬＤＲなら、新しいのを決めます。


				}
			else if( Units[m].Hp<=Units[m].MaxHp*0.2 )
				{
				// ＨＰはあるが、事実上の墜落、
				if( Units[m].Hp==Units[m].MaxHp*0.2 )
					{
					// 飛行機が火を吹く
					if(Units[m].Kind==UnitKind.Bomber)
						{
						for(i=0;i<3;i++)
							{
							f=FindFreeEffect();
							if( f!=0 )
								{
								Effects[f].Layer=EffectLayer.Upper;	
								Effects[f].info[0]=20+SharedRandom(20);
								Effects[f].info[1]=4;
								Effects[f].Position = new WorldPosition(Units[m].Position.X+20-SharedRandom(40), Units[m].Position.Y+20-SharedRandom(40));
								Effects[f].SpriteNumber=9;				// ソースファイル上の番号	
								}
							}
						}
					else
						{
						f=FindFreeEffect();			
						Effects[f].Layer=EffectLayer.Upper;	
						Effects[f].info[0]=20+SharedRandom(20);
						Effects[f].info[1]=4;
						Effects[f].Position=Units[m].Position;
						Effects[f].SpriteNumber=9;				// ソースファイル上の番号	
						}
					if(IsEditingMap==0)
						Units[m].Hp--;
					}
				else
					{
					if( Random(80)==0 )
						{
						if(IsEditingMap==0)
							Units[m].Hp--;
						}

					if( Random(5)!=0 && (Tick%(10))==0 )
						{
						f=FindFreeEffect();
						Effects[f].Layer=EffectLayer.Upper;	
						//effect[f].kind=THERE;
						Effects[f].info[0]=40+SharedRandom(15);
						Effects[f].info[1]=2;
						Effects[f].Position=Units[m].Position;
						Effects[f].SpriteNumber=6;			// ソースファイル上の番号
						}


					if( Random(3)==0 && Units[m].Hp<=Units[m].MaxHp*0.1 )
						{
						// 小爆炎
						f=FindFreeEffect();			
						Effects[f].Layer=EffectLayer.Upper;	
						Effects[f].info[0]=8+SharedRandom(6);
						Effects[f].info[1]=4;
						Effects[f].Position = new WorldPosition(Units[m].Position.X+SharedRandom(6)-3, Units[m].Position.Y+SharedRandom(6)-3);
						Effects[f].SpriteNumber=10;			// ソースファイル上の番号	
						}
					}
				}
			else if( Units[m].Hp<=Units[m].MaxHp*0.3  )
				{
				if( Random(500)==0 )
					{
					if(IsEditingMap==0)
						Units[m].Hp--;
					}

				if( Random(3)!=0 && (Tick%(10) )==0 )
					{
					f=FindFreeEffect();
					Effects[f].Layer=EffectLayer.Upper;	
					//effect[f].kind=THERE;
					Effects[f].info[0]=40+SharedRandom(15);
					Effects[f].info[1]=2;
					Effects[f].Position=Units[m].Position;
					Effects[f].SpriteNumber=6;			// ソースファイル上の番号


					if( Random(5)==0 )
						{
						// 小爆炎
						f=FindFreeEffect();			
						Effects[f].Layer=EffectLayer.Upper;	
						Effects[f].info[0]=8+SharedRandom(6);
						Effects[f].info[1]=4;
						Effects[f].Position = new WorldPosition(Units[m].Position.X+SharedRandom(6)-3, Units[m].Position.Y+SharedRandom(6)-3);
						Effects[f].SpriteNumber=10;			// ソースファイル上の番号	
						}
					}


				}
			else if( Units[m].Hp<=Units[m].MaxHp*0.5  )
				{
				if( Random(500)==0 )
					{
					if(IsEditingMap==0)
						Units[m].Hp--;
					}
				if( Random(2)==1 && (Tick%10)==0 )
					{
					f=FindFreeEffect();
					Effects[f].Layer=EffectLayer.Upper;	
					//effect[f].kind=THERE;
					Effects[f].info[0]=40+SharedRandom(15);
					Effects[f].info[1]=2;

					Effects[f].Position=Units[m].Position;
					Effects[f].SpriteNumber=6;			// ソースファイル上の番号

					if( Random(7)==0 )
						{
						// 小爆炎
						f=FindFreeEffect();			
						Effects[f].Layer=EffectLayer.Upper;	
						Effects[f].info[0]=8+SharedRandom(6);
						Effects[f].info[1]=4;
						Effects[f].Position = new WorldPosition(Units[m].Position.X+SharedRandom(6)-3, Units[m].Position.Y+SharedRandom(6)-3);
						Effects[f].SpriteNumber=10;			// ソースファイル上の番号	
						}
					}

				}
			else if( Units[m].Hp<=Units[m].MaxHp*0.7  )
				{

				if( Random(8)==0 && (Tick%10)==0 )
					{
					f=FindFreeEffect();
					Effects[f].Layer=EffectLayer.Upper;	
					//effect[f].kind=THERE;
					Effects[f].info[0]=40+SharedRandom(15);
					Effects[f].info[1]=2;

					Effects[f].Position=Units[m].Position;
					Effects[f].SpriteNumber=6;			// ソースファイル上の番号


					}
				}
			}
		}


	// 艦船のエフェクト
	if( Units[m].Category==UnitCategory.Ship  )
		{
		// 修理と補給中の表示
		if( /*paint_effect_on &&*/ Units[m].Supply!=0 && (FrameCount%2)!=0 && Units[m].Side==LocalSide )
			{
			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Lower;	
			Effects[f].info[0]=1;

			Effects[f].info[1]=0;
			Effects[f].Position = new WorldPosition(Units[m].Position.X, Units[m].Position.Y-25.0);

			Effects[f].SpriteNumber=15;			// ソースファイル上の番号
			}

		// 武装の表示
// 弾薬の消費サイズ
//		if( ( unit[m].used==JPN && ( unit[m].kind==SS1 || unit[m].kind==DD1 || unit[m].kind==CA1 ) || unit[m].used==USA && ( unit[m].kind==SS1 || unit[m].kind==DD1 ) ) && unit[m].spry==0 && unit[m].arm[1] && !(unit[m].arm[3] && (FrameCount%2)) && unit[m].arm[1]>=1 && unit[m].used==your_side )




		if( ( ( Units[m].Side==Side.Japan && Units[m].Kind==UnitKind.Battleship ) || ( Units[m].Side==Side.UnitedStates && Units[m].Kind==UnitKind.Carrier ) ) && Units[m].Variant==1 && Units[m].Side==LocalSide )
			{
			// 大和級とエセックス
			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Lower;	
			Effects[f].info[0]=1;

			Effects[f].info[1]=0;
			Effects[f].Position = new WorldPosition(Units[m].Position.X+18, Units[m].Position.Y+25.0);

			Effects[f].SpriteNumber=27;			// ソースファイル上の番号
			}
		else if( Units[m].Kind==UnitKind.Destroyer && Units[m].Variant==1 /*&& unit[m].spry==0 /*&& unit[m].arm[1]>=1*/ && Units[m].Side==LocalSide )
			{
			// 対潜駆逐艦
			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Lower;	
			Effects[f].info[0]=1;

			Effects[f].info[1]=0;
			Effects[f].Position = new WorldPosition(Units[m].Position.X+18, Units[m].Position.Y+25.0);

			Effects[f].SpriteNumber=26;			// ソースファイル上の番号
			}
		else if( Units[m].Kind==UnitKind.Cruiser && Units[m].Variant==1 /*&& unit[m].spry==0 /*&& unit[m].arm[1]>=1*/ && Units[m].Side==LocalSide )
			{
			// 防空巡洋艦
			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Lower;	
			Effects[f].info[0]=1;

			Effects[f].info[1]=0;
			Effects[f].Position = new WorldPosition(Units[m].Position.X+18, Units[m].Position.Y+25.0);

			Effects[f].SpriteNumber=16;			// ソースファイル上の番号
			}
		else if( ( Units[m].Side==Side.Japan && ( Units[m].Kind==UnitKind.Submarine || Units[m].Kind==UnitKind.Destroyer || Units[m].Kind==UnitKind.Cruiser ) || Units[m].Side==Side.UnitedStates && ( Units[m].Kind==UnitKind.Submarine || Units[m].Kind==UnitKind.Destroyer ) ) && Units[m].Supply==0 && Units[m].Ammo!=0 && !(Units[m].ReloadTime!=0 && (FrameCount%2)!=0) && Units[m].Ammo>=1 && Units[m].Side==LocalSide )
			{
			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Lower;	
			Effects[f].info[0]=1;

			Effects[f].info[1]=0;
			Effects[f].Position = new WorldPosition(Units[m].Position.X, Units[m].Position.Y-25.0);

			Effects[f].SpriteNumber=3;			// ソースファイル上の番号
			}


		// トランスポーターの荷物の表示 
		if( /*paint_effect_on &&*/ Units[m].Kind==UnitKind.Transport && Units[m].Supply==0 && Units[m].Ammo!=0 && !(Units[m].ReloadTime!=0 && (FrameCount%2)!=0) && Units[m].Side==LocalSide )
			{
			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Lower;	
			Effects[f].info[0]=1;

			Effects[f].info[1]=0;
			Effects[f].Position = new WorldPosition(Units[m].Position.X, Units[m].Position.Y-25.0);

			//switch( unit[m].arm[0] )
			//	{
			//	case TPD:
					Effects[f].SpriteNumber=44;			// ソースファイル上の番号
			//		break;
			//	}
			}


//TR_GF1

		// およその敵潜航潜水艦
		if( /*paint_effect_on &&*/ Units[m].Side!=LocalSide && Units[m].Kind==UnitKind.Submarine && Units[m].info[6]!=0 && Units[m].info[10]!=0 )
			{
			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Upper;
			Effects[f].info[0]=1;
			Effects[f].info[1]=0;
			Effects[f].Position = new WorldPosition(Units[m].info[7], Units[m].info[8]);

			Effects[f].SpriteNumber=60;			// ソースファイル上の番号



			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Lower;
			Effects[f].info[0]=1;
			Effects[f].info[1]=12;
			Effects[f].Position = new WorldPosition(Units[m].info[7], Units[m].info[8]);
			Effects[f].EndPosition = new WorldPosition(Units[m].info[9], Units[m].info[9]);
/*
			wrk_rect.top=(int)effect[f].y-unit[m].info[9];
			wrk_rect.right=(int)effect[f].x+unit[m].info[9];
			wrk_rect.bottom=(int)effect[f].y+unit[m].info[9];
			wrk_rect.left=(int)effect[f].x-unit[m].info[9];
			draw_line4((int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.top),(int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
			draw_line4((int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.top),(int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
			draw_line4((int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),(int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
			draw_line4((int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),(int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
*/
			}


		if( Units[m].Kind==UnitKind.Submarine && Units[m].info[6]!=0 )
			{	// 潜航中潜水艦
			if( Units[m].Hp<=0 )
				{	// 沈没
				Units[m].Side=0;
				Units[m].Found=0;


				if( UnitInfoPanel[3]==m )
					UnitInfoPanel[0]=0;				// ユニットインフォをクリア


				if( SelectedUnit==m )
					{
					SelectedUnit=0;
					CombatMenuKind=0; CombatMenuSelection=CombatMenuItem.None; ClearSelection2(1);
					}
				}
			else
				{
				if( Units[m].Hp<=/*unit[m].hp[2]*/Units[m].MaxHp*0.2 )
					{	// 空気漏れ
					if( Random(100)==0 )
						{
						if(IsEditingMap==0)
							Units[m].Hp--;
						}
					if( Random(300)==1 /*&& paint_effect_on*/ )
						{
						for(n=0;n<=3;n++)
							{
							f=FindFreeEffect();			
							if( f!=0 )
								{
								Effects[f].Layer=EffectLayer.Lower;	
								Effects[f].info[0]=100+SharedRandom(20);
								Effects[f].info[1]=4;
								Effects[f].Position = new WorldPosition(Units[m].Position.X+SharedRandom(40)-20, Units[m].Position.Y+SharedRandom(40)-20);
								Effects[f].SpriteNumber=8;			// ソースファイル上の番号	
								}
							}
						// ついでに発見される
						Units[m].info[7]=(int)Units[m].Position.X;
						Units[m].info[8]=(int)Units[m].Position.Y;

						Units[m].info[9]=100;
						Units[m].info[10]=400;

						Units[m].Found=1;
						}
					}
				else
					{
					if( Units[m].Hp<=Units[m].MaxHp*0.5 )
						{	// 空気漏れ
						if( Random(1000)==0 )
							{
							if(IsEditingMap==0)
								Units[m].Hp--;
							}
						if( Random(600)==1 /*&& paint_effect_on*/ )
							{
							for(n=0;n<=2;n++)
								{
								f=FindFreeEffect();			
								if( f!=0)
									{
									Effects[f].Layer=EffectLayer.Lower;	
									Effects[f].info[0]=100+SharedRandom(20);
									Effects[f].info[1]=4;
									Effects[f].Position = new WorldPosition(Units[m].Position.X+SharedRandom(40)-20, Units[m].Position.Y+SharedRandom(40)-20);
									Effects[f].SpriteNumber=8;			// ソースファイル上の番号	
									}
								}

							// ついでに発見される
							Units[m].info[7]=(int)Units[m].Position.X;
							Units[m].info[8]=(int)Units[m].Position.Y;

							Units[m].info[9]=100;
							Units[m].info[10]=400;

							Units[m].Found=1;
							}
						}
					}
				}
			}
		else
			{	// 水上艦船
			if( Units[m].Hp<=0 )
				{	// 沈没

				PlaySoundEffect( 0, SHIP_SINK1 ,Units[m].Position.X, Units[m].Position.Y);

				Units[m].Side=0;

		
				if( UnitInfoPanel[3]==m )
					UnitInfoPanel[0]=0;

				// 空母なら艦載機とユニットインフォを
				if( Units[m].Kind==UnitKind.Carrier || Units[m].Kind==UnitKind.LightCarrier || Units[m].Kind==UnitKind.AirBase )
					{
					for( i=1;i<=MaxUnitId;i++)
						{
						if( Units[i].Side!=0 && Units[i].Category==UnitCategory.Plane && Units[i].PlaneState==UnitState.Parked && Units[i].info[1]==m)
							{
							Units[i].Side=0;
							if( SelectedUnit==i)
								{
								SelectedUnit=0;
								CombatMenuKind=0; CombatMenuSelection=CombatMenuItem.None; ClearSelection2(1);
								}
							}
						}
					}



				DrawDestruction(m);

				if( SelectedUnit==m )
					{
					SelectedUnit=0;
					CombatMenuKind=0; CombatMenuSelection=CombatMenuItem.None; ClearSelection2(1);
					}


				}
			else 
				{
				if( Units[m].Hp<=Units[m].MaxHp*0.2  )
					{
					if( Random(4000)==0 && Units[m].Supply==0 )
						{
						if(IsEditingMap==0)
							Units[m].Hp--;
						}
					if( Random(2)!=0 /*&& paint_effect_on*/ )
						{
						f=FindFreeEffect();
						Effects[f].Layer=EffectLayer.Upper;	
						Effects[f].info[0]=1;
						Effects[f].info[1]=4;

						Effects[f].Position=Units[m].Position;
						Effects[f].SpriteNumber=9;			// ソースファイル上の番号
						}
					}
				else
					{
					if( Units[m].Hp<=Units[m].MaxHp*0.5  )
						{
						n=Random(4);
						if( /*paint_effect_on &&*/ n==0 )
							{
							f=FindFreeEffect();
							Effects[f].Layer=EffectLayer.Upper;	
							Effects[f].info[0]=1;	Effects[f].info[1]=4;
							Effects[f].Position=Units[m].Position;
							Effects[f].SpriteNumber=9;			// ソースファイル上の番号
							}
						if( /*paint_effect_on &&*/ n==1 )
							{
							f=FindFreeEffect();
							Effects[f].Layer=EffectLayer.Upper;	
							Effects[f].info[0]=1;	Effects[f].info[1]=4;
							Effects[f].Position=Units[m].Position;
							Effects[f].SpriteNumber=10;			// ソースファイル上の番号
							}
						}
					else
						{
						if( Random(10)==1 /*&& paint_effect_on*/ && Units[m].Hp<=Units[m].MaxHp*0.7 )
							{
							f=FindFreeEffect();
							Effects[f].Layer=EffectLayer.Upper;	
							Effects[f].info[0]=1;	Effects[f].info[1]=4;
							Effects[f].Position=Units[m].Position;
							Effects[f].SpriteNumber=10;			// ソースファイル上の番号
							}
						}
					}
				}
			}


		// 航跡のエフェクト
		if( !(Units[m].Kind==UnitKind.Submarine||Units[m].Kind==UnitKind.NavalBase||Units[m].Kind==UnitKind.AirBase||Units[m].Kind==UnitKind.City||Units[m].Kind==UnitKind.Mine||Units[m].Kind==UnitKind.InfantryBase||Units[m].Kind==UnitKind.Pillboxes||Units[m].Kind==UnitKind.Fortress) && (Tick%15)==0 && Units[m].Speed>=Units[m].MaxSpeed/3 )
			{
			// 航跡のエフェクトを残す
			f=FindFreeEffect();			
			Effects[f].Layer=EffectLayer.Lower;	
			Effects[f].info[0]=60+SharedRandom(60);
			Effects[f].info[1]=4;
			Effects[f].Position=Units[m].Position;

			drctn=Units[m].Direction;
			drctn+=180;
			drctn=(int)drctn%360;

			switch(Units[m].Kind)
				{
				case UnitKind.Battleship:	case UnitKind.Carrier:
					dstc=26;
					break;

				case UnitKind.Cruiser:	case UnitKind.LightCarrier:
					dstc=22;
					break;
		
				default:
					dstc=16;
					break;
				}


			Effects[f].Position += new WorldVector(cos(drctn*a_PI)*dstc, sin(drctn*a_PI)*dstc);

			Effects[f].SpriteNumber=8;			// ソースファイル上の番号	
			}
		}


	// 艦船のエフェクト
	if( Units[m].Kind==UnitKind.InfantryBase || Units[m].Kind==UnitKind.Pillboxes || Units[m].Kind==UnitKind.Fortress || Units[m].Kind==UnitKind.AirBase || Units[m].Kind==UnitKind.InfantryBase || Units[m].Kind==UnitKind.NavalBase )
		{
		// 建設工事中
		if( /*paint_effect_on &&*/ Units[m].info[0]!=0 && (FrameCount%2)!=0 && Units[m].Side==LocalSide )
			{
			f=FindFreeEffect();
//			effect[f].layer=LOWER;	
			Effects[f].Layer=EffectLayer.Upper;	
			Effects[f].info[0]=1;

			Effects[f].info[1]=0;
			Effects[f].Position = new WorldPosition(Units[m].Position.X, Units[m].Position.Y-25.0);

			Effects[f].SpriteNumber=15;			// ソースファイル上の番号
			}
		}


	}






//============================================================================
//	
//----------------------------------------------------------------------------
[Original("edit_now")]
public void	UpdateMapEditor()
	{
    Array128<byte> ach = default;
    int len;
	HDC					hdc;
	short	i,jp_plane,jp_ship,us_plane,us_ship;


	if ( IDirectDrawSurface_GetDC(lpDDSBack, &hdc) == DD_OK ) 
		{


		// draw stats, like frame number and frame rate
		SetBkMode(hdc, TRANSPARENT);
		SelectObject(hdc, gameFont_1);
		SetTextColor(hdc, RGB(255, 255, 0));


		jp_plane=0;
		jp_ship=0;
		us_plane=0;
		us_ship=0;

		for(i=1;i<=MaxUnitId;i++)
			{
			if( Units[i].Side!=0 )
				{
				if( Units[i].Side==Side.Japan)
					{
					if( Units[i].Category==UnitCategory.Plane )
						jp_plane++;
					else
						jp_ship++;
					}
				else
					{
					if( Units[i].Category==UnitCategory.Plane )
						us_plane++;
					else
						us_ship++;
					}
				}
			}



#if !LNGG_VER
		len = wsprintf(ach, "日本海軍　：　航空機　%d／%d　艦船又は地上施設　%d／%d　",jp_plane,JPN_PLANE_END-JPN_PLANE_START+1,jp_ship,JPN_SHIP_END-JPN_SHIP_START+1 );
		TextOut(hdc, 0, 530, ach, len);
		len = wsprintf(ach, "合衆国海軍：　航空機　%d／%d　艦船又は地上施設　%d／%d　",us_plane,USA_PLANE_END-USA_PLANE_START+1,us_ship,USA_SHIP_END-USA_SHIP_START+1 );
		TextOut(hdc, 0, 550, ach, len);

		// 使用説明
		len = wsprintf(ach, "F1:SAVE　F2=LOAD　F3:マップ再描画 F5:ユニット選択-> F6:ユニット選択<-");
		TextOut(hdc, 0, 580, ach, len);
		len = wsprintf(ach, "F7:ユニット消去 F8:ユニット配置 F9:サイドチェンジ F11:増援ポイント変更");
		TextOut(hdc, 0, 600, ach, len);
		len = wsprintf(ach, "1:ユニットの回転 2:航空機の武装／艦船の停泊 3:損傷回復 4:損傷 5:増燃料 6:減燃料 7:増残弾 8:減残弾");
		TextOut(hdc, 0, 620, ach, len);
#else

		len = wsprintf(ach, "Japan Navy : Airplane %d/%d  Ships or Ground Facility %d/%d ",jp_plane,JPN_PLANE_END-JPN_PLANE_START+1,jp_ship,JPN_SHIP_END-JPN_SHIP_START+1 );
		TextOut(hdc, 0, 530, ach, len);
		len = wsprintf(ach, "U.S. Navy  : Airplane %d/%d  Ships or Ground Facility %d/%d ",us_plane,USA_PLANE_END-USA_PLANE_START+1,us_ship,USA_SHIP_END-USA_SHIP_START+1 );
		TextOut(hdc, 0, 550, ach, len);

		// 使用説明
		len = wsprintf(ach, "F1:SAVE　F2=LOAD　F3:MAP REDRAW F5:KIND<- F6:KIND-> F7:UNIT DELETE");
		TextOut(hdc, 0, 580, ach, len);
		len = wsprintf(ach, "F8:PUT UNIT F9:SIDE CHANGE F11:CHANGE REINFORCE POINT");
		TextOut(hdc, 0, 600, ach, len);
		len = wsprintf(ach, "1:ROTATE 2:AIR PLANE'S ARM/ANCHORED 3:1 PT RECOVERY 4:1 PT DAMAGE 5:GAS UP 6:GAS DOWN 7:AMMO UP 8:AMMO DOWN");
		TextOut(hdc, 0, 620, ach, len);

#endif






#if !LNGG_VER


		// 配置ユニット
		switch(EditorTarget)
			{
			case 1:
				len= wsprintf(ach, "戦艦",10);
				EditorKind=(byte)UnitKind.Battleship;
				EditorVariant=0;
				break;
			case 2:
				len= wsprintf(ach, "巡洋艦",10);
				EditorKind=(byte)UnitKind.Cruiser;
				EditorVariant=0;
				break;
			case 3:
				len= wsprintf(ach, "駆逐艦",10);
				EditorKind=(byte)UnitKind.Destroyer;
				EditorVariant=0;
				break;
			case 4:
				len= wsprintf(ach, "潜水艦",10);
				EditorKind=(byte)UnitKind.Submarine;
				EditorVariant=0;
				break;
			case 5:
				len= wsprintf(ach, "正規空母",10);
				EditorKind=(byte)UnitKind.Carrier;
				EditorVariant=0;
				break;
			case 6:
				len= wsprintf(ach, "軽空母",10);
				EditorKind=(byte)UnitKind.LightCarrier;
				EditorVariant=0;
				break;
			case 7:
				len = wsprintf(ach, "輸送船(歩兵基地)",10);
				EditorKind=(byte)UnitKind.Transport;
				EditorVariant=(byte)FireKind.CargoInfantryBase;
				break;
			case 8:
				len = wsprintf(ach, "輸送船(トーチカ群)",10);
				EditorKind=(byte)UnitKind.Transport;
				EditorVariant=(byte)FireKind.CargoPillboxes;
				break;
			case 9:
				len = wsprintf(ach, "輸送船(要塞)",10);
				EditorKind=(byte)UnitKind.Transport;
				EditorVariant=(byte)FireKind.CargoFortress;
				break;
			case 10:
				len = wsprintf(ach, "輸送船(航空基地)",10);
				EditorKind=(byte)UnitKind.Transport;
				EditorVariant=(byte)FireKind.CargoAirBase;
				break;
			case 11:
				len = wsprintf(ach, "輸送船(軍港)",10);
				EditorKind=(byte)UnitKind.Transport;
				EditorVariant=(byte)FireKind.CargoNavalBase;
				break;


			case 12:
				len= wsprintf(ach, "軍港",10);
				EditorKind=(byte)UnitKind.NavalBase;
				EditorVariant=0;
				break;
			case 13:
				len= wsprintf(ach, "航空基地",10);
				EditorKind=(byte)UnitKind.AirBase;
				EditorVariant=0;
				break;
			case 14:
				len= wsprintf(ach, "都市",10);
				EditorKind=(byte)UnitKind.City;
				EditorVariant=0;
				break;
			case 15:
				len= wsprintf(ach, "歩兵基地",10);
				EditorKind=(byte)UnitKind.InfantryBase;
				EditorVariant=0;
				break;
			case 16:
				len= wsprintf(ach, "トーチカ群",10);
				EditorKind=(byte)UnitKind.Pillboxes;
				EditorVariant=0;
				break;
			case 17:
				len= wsprintf(ach, "要塞",10);
				EditorKind=(byte)UnitKind.Fortress;
				EditorVariant=0;
				break;



			case 18:
				len = wsprintf(ach, "戦闘機",10);
				EditorKind=(byte)UnitKind.Fighter;
				EditorVariant=0;
				break;
			case 19:
				len = wsprintf(ach, "陸上戦闘機",10);
				EditorKind=(byte)UnitKind.Fighter;
				EditorVariant=1;
				break;
			case 20:
				len= wsprintf(ach, "攻撃機",10);
				EditorKind=(byte)UnitKind.Attacker;
				EditorVariant=0;
				break;
			case 21:
				len= wsprintf(ach, "戦略爆撃機",10);
				EditorKind=(byte)UnitKind.Bomber;
				EditorVariant=0;
				break;


			case 22:
				len= wsprintf(ach, "防空巡洋艦",10);
				EditorKind=(byte)UnitKind.Cruiser;
				EditorVariant=1;
				break;
			case 23:
				len= wsprintf(ach, "対潜駆逐艦",10);
				EditorKind=(byte)UnitKind.Destroyer;
				EditorVariant=1;
				break;
			case 24:
				if( LocalSide==Side.Japan )
					{
					len= wsprintf(ach, "大和級戦艦",10);
					EditorKind=(byte)UnitKind.Battleship;
					}
				else
					{
					len= wsprintf(ach, "エセックス型空母",10);
					EditorKind=(byte)UnitKind.Carrier;
					}
				EditorVariant=1;
				break;




			default:
				len= wsprintf(ach, "-----",10);
				break;
			}
		TextOut(hdc, 120, 640, ach, len);

		len = wsprintf(ach, "配置ユニット:");
		TextOut(hdc, 0, 640, ach, len);




		// 増援場所
		len = wsprintf(ach, "増援場所:");
		TextOut(hdc, 0, 660, ach, len);

		switch( Reinforcements[(int)LocalSide] )
			{
			case 0:
				len = wsprintf(ach, "左上");
				break;
			case 1:
				len = wsprintf(ach, "右上");
				break;
			case 2:
				len = wsprintf(ach, "右下");
				break;
			case 3:
				len = wsprintf(ach, "左下");
				break;
			}

		TextOut(hdc, 80, 660, ach, len);

#else




		// 配置ユニット
		switch(put_trgt)
			{
			case 1:
				len= wsprintf(ach, "Battleship",10);
				put_kind=(byte)UnitKind.Battleship;
				put_kind_sub=0;
				break;
			case 2:
				len= wsprintf(ach, "Cruiser",10);
				put_kind=(byte)UnitKind.Cruiser;
				put_kind_sub=0;
				break;
			case 3:
				len= wsprintf(ach, "Destroyer",10);
				put_kind=(byte)UnitKind.Destroyer;
				put_kind_sub=0;
				break;
			case 4:
				len= wsprintf(ach, "Submarine",10);
				put_kind=(byte)UnitKind.Submarine;
				put_kind_sub=0;
				break;
			case 5:
				len= wsprintf(ach, "Carrier",10);
				put_kind=(byte)UnitKind.Carrier;
				put_kind_sub=0;
				break;
			case 6:
				len= wsprintf(ach, "Lt.Carrier",10);
				put_kind=(byte)UnitKind.LightCarrier;
				put_kind_sub=0;
				break;
			case 7:
				len = wsprintf(ach, "Transport(Trenchies)",10);
				put_kind=(byte)UnitKind.Transport;
				put_kind_sub=(byte)FireKind.CargoInfantryBase;
				break;
			case 8:
				len = wsprintf(ach, "Transport(Pillboxes)",10);
				put_kind=(byte)UnitKind.Transport;
				put_kind_sub=(byte)FireKind.CargoPillboxes;
				break;
			case 9:
				len = wsprintf(ach, "Transport(Fortress)",10);
				put_kind=(byte)UnitKind.Transport;
				put_kind_sub=(byte)FireKind.CargoFortress;
				break;
			case 10:
				len = wsprintf(ach, "Transport(Airfield)",10);
				put_kind=(byte)UnitKind.Transport;
				put_kind_sub=(byte)FireKind.CargoAirBase;
				break;
			case 11:
				len = wsprintf(ach, "Transport(port)",10);
				put_kind=(byte)UnitKind.Transport;
				put_kind_sub=(byte)FireKind.CargoNavalBase;
				break;


			case 12:
				len= wsprintf(ach, "Military Port",10);
				put_kind=(byte)UnitKind.NavalBase;
				put_kind_sub=0;
				break;
			case 13:
				len= wsprintf(ach, "Airfield",10);
				put_kind=(byte)UnitKind.AirBase;
				put_kind_sub=0;
				break;
			case 14:
				len= wsprintf(ach, "City",10);
				put_kind=(byte)UnitKind.City;
				put_kind_sub=0;
				break;
			case 15:
				len= wsprintf(ach, "Trenchies",10);
				put_kind=(byte)UnitKind.InfantryBase;
				put_kind_sub=0;
				break;
			case 16:
				len= wsprintf(ach, "Pillboxes",10);
				put_kind=(byte)UnitKind.Pillboxes;
				put_kind_sub=0;
				break;
			case 17:
				len= wsprintf(ach, "Fortress",10);
				put_kind=(byte)UnitKind.Fortress;
				put_kind_sub=0;
				break;



			case 18:
				len = wsprintf(ach, "Car.Fighter",10);
				put_kind=(byte)UnitKind.Fighter;
				put_kind_sub=0;
				break;
			case 19:
				len = wsprintf(ach, "Grn.Fighter",10);
				put_kind=(byte)UnitKind.Fighter;
				put_kind_sub=1;
				break;
			case 20:
				len= wsprintf(ach, "Car.Bomber",10);
				put_kind=(byte)UnitKind.Attacker;
				put_kind_sub=0;
				break;
			case 21:
				len= wsprintf(ach, "Bomber",10);
				put_kind=(byte)UnitKind.Bomber;
				put_kind_sub=0;
				break;


			case 22:
				len= wsprintf(ach, "AntiAir Cruiser",10);
				put_kind=(byte)UnitKind.Cruiser;
				put_kind_sub=1;
				break;
			case 23:
				len= wsprintf(ach, "AntiSub Destroyer",10);
				put_kind=(byte)UnitKind.Destroyer;
				put_kind_sub=1;
				break;
			case 24:
				if( your_side==Side.Japan )
					{
					len= wsprintf(ach, "Class Yamato",10);
					put_kind=(byte)UnitKind.Battleship;
					}
				else
					{
					len= wsprintf(ach, "Type Essex",10);
					put_kind=(byte)UnitKind.Carrier;
					}
				put_kind_sub=1;
				break;


			default:
				len= wsprintf(ach, "-----",10);
				break;
			}
		TextOut(hdc, 120, 640, ach, len);

		len = wsprintf(ach, "PUT UNIT:");
		TextOut(hdc, 0, 640, ach, len);




		// 増援場所
		len = wsprintf(ach, "Reinforce:");
		TextOut(hdc, 0, 660, ach, len);

		switch( rein[(int)your_side] )
			{
			case 0:
				len = wsprintf(ach, "Left-Top");
				break;
			case 1:
				len = wsprintf(ach, "Right-Top");
				break;
			case 2:
				len = wsprintf(ach, "Right-bottom");
				break;
			case 3:
				len = wsprintf(ach, "Left-Bottom");
				break;
			}

		TextOut(hdc, 80, 660, ach, len);



#endif


		IDirectDrawSurface_ReleaseDC(lpDDSBack, hdc);
		}
	}
}
