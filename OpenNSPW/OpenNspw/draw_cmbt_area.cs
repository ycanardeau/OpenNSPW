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

// Port of draw_cmbt_area.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{






//============================================================================
// 
//----------------------------------------------------------------------------
public void	cont_upper_effect(RECT* pfield_rect,RECT* pinfo_rect)
	{
	RECT src_rect,dstn_rect;
	int	m,no1,n,flg,size=default /* C4701 */;
	double	wrk_x,wrk_y,drctn,dstc;



	//=========		 Ｕｐｐｅｒのエフェクト描画		=========//
	no1=7;
	for( m=1; m<EFFECT_MAX; m++ )
		{
		if( effect[m].layer==EffectLayer.Upper )
			{
			flg=0;
			if(game_end!=GameResult.None)
				flg=1;
			for(n=1;n<=max_unit && flg==0 ;n++)
				{
				if( unit[n].used!=0 && unit[n].used==your_side && unit[n].PlaneState!=UnitState.Parked )
					{
					// マイユニットからこのエフェクトが見えるか
					// 現地点からユニット地点への距離
					wrk_x=unit[n].Position.X;
					wrk_y=unit[n].Position.Y;
					if( unit[n].kind==UnitKind.Fighter )
						{	// 航空機の場合はちょっと前へ
						wrk_x+=cos(unit[n].drctn*a_PI)*FT_EYE;
						wrk_y+=sin(unit[n].drctn*a_PI)*FT_EYE;
						}

					wrk_x=wrk_x-effect[m].Position.X;
					wrk_y=wrk_y-effect[m].Position.Y;

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

					dstc=((wrk_x)/(cos(drctn*a_PI)));

					switch( unit[n].kind )
						{
						case UnitKind.Battleship:		size=BB1_SIGHT;		break;
						case UnitKind.Cruiser:		size=CA1_SIGHT;		break;
						case UnitKind.Destroyer:		size=DD1_SIGHT;		break;
						case UnitKind.Submarine:		size=SS1_SIGHT;		break;
						case UnitKind.Carrier:		size=CV1_SIGHT;		break;
						case UnitKind.LightCarrier:		size=CVL1_SIGHT;	break;
						case UnitKind.Transport:		size=TR1_SIGHT;		break;
						case UnitKind.Fighter:		size=FT1_SIGHT;		break;
						case UnitKind.Attacker:		size=AT1_SIGHT;		break;
						case UnitKind.Bomber:		size=BM1_SIGHT;		break;
						case UnitKind.AirBase: case UnitKind.NavalBase:		size=AP_SIGHT;		break;		
						case UnitKind.City:		size=CT1_SIGHT;		break;
						case UnitKind.Mine:		size=MN1_SIGHT;		break;
						case UnitKind.InfantryBase:		size=GF1_SIGHT;		break;
						case UnitKind.Pillboxes:		size=GF2_SIGHT;		break;
						case UnitKind.Fortress:		size=GF3_SIGHT;		break;
						}


					if( unit[n].kind>=UnitKind.AirBase && unit[n].kind<=UnitKind.Fortress && unit[n].info[0]!=0 )
						{
						// 工事中は視界を制限
						size=FT1_SIGHT/2;
						}


					if( dstc<=size )
						{
						flg=1;
						}
					}
				}


//			effect[m].info[0]--;
//			if( !effect[m].info[0] )
//				effect[m].layer=0;

			if( flg!=0 )
				{


				// マイユニットから見えるので表示
				switch( effect[m].info[1] )
					{
					case 0:		// 
						sprt[SUB_UNIT].no=effect[m].no;
						sprt[SUB_UNIT].x=(int)(effect[m].Position.X-CameraPosition.X);
						sprt[SUB_UNIT].y=(int)(CameraPosition.Y-effect[m].Position.Y);
						n=1;
						break;
					case 1:		// 対空機関砲弾がヒット
						sprt[SUB_UNIT].no=effect[m].no+(sprt[SUB_UNIT].os_of_x*(effect[m].info[0]%2));
						sprt[SUB_UNIT].x=(int)(effect[m].Position.X-CameraPosition.X);
						sprt[SUB_UNIT].y=(int)(CameraPosition.Y-effect[m].Position.Y);
						n=1;
						break;
					case 2:		//	飛行機からの煙
						sprt[SUB_UNIT].no=effect[m].no+(sprt[SUB_UNIT].os_of_x*(my_rnd(2)));
						sprt[SUB_UNIT].x=(int)(effect[m].Position.X-CameraPosition.X)+my_rnd(10)-5;
						sprt[SUB_UNIT].y=(int)(CameraPosition.Y-effect[m].Position.Y)+my_rnd(10)-5;
						n=1;
						break;
					case 3:		//	駐機場の飛行機用
						sprt[SUB_UNIT].no=effect[m].no;
						sprt[SUB_UNIT].x=(int)(effect[m].Position.X);
						sprt[SUB_UNIT].y=(int)(effect[m].Position.Y);
						n=0;
						break;
					case 4:		// 雷跡
						sprt[SUB_UNIT].no=effect[m].no+(sprt[SUB_UNIT].os_of_x*(my_rnd(2)));
						sprt[SUB_UNIT].x=(int)(effect[m].Position.X-CameraPosition.X);
						sprt[SUB_UNIT].y=(int)(CameraPosition.Y-effect[m].Position.Y);
						n=1;
						break;
					case 10:	// 対空機関砲
						draw_line5((int)(effect[m].Position.X-CameraPosition.X),(int)(CameraPosition.Y-effect[m].Position.Y),(int)(effect[m].EndPosition.X-CameraPosition.X),(int)(CameraPosition.Y-effect[m].EndPosition.Y),CMBT_WIDTH-1,CMBT_HEIGHT-1,255);
						draw_line5((int)(effect[m].Position.X-CameraPosition.X)+1,(int)(CameraPosition.Y-effect[m].Position.Y),(int)(effect[m].EndPosition.X-CameraPosition.X)+1,(int)(CameraPosition.Y-effect[m].EndPosition.Y),CMBT_WIDTH-1,CMBT_HEIGHT-1,255);
						draw_line5((int)(effect[m].Position.X-CameraPosition.X),(int)(CameraPosition.Y-effect[m].Position.Y)-1,(int)(effect[m].EndPosition.X-CameraPosition.X),(int)(CameraPosition.Y-effect[m].EndPosition.Y)-1,CMBT_WIDTH-1,CMBT_HEIGHT-1,255);
						continue;
					case 11:	// 弾丸
						draw_line5((int)(effect[m].Position.X-CameraPosition.X),(int)(CameraPosition.Y-effect[m].Position.Y),(int)(effect[m].EndPosition.X-CameraPosition.X),(int)(CameraPosition.Y-effect[m].EndPosition.Y),CMBT_WIDTH-1,CMBT_HEIGHT-1,255);
						//draw_line5((int)(effect[m].x-cmbt_x)+1,(int)(cmbt_y-effect[m].y),(int)(effect[m].x2-cmbt_x)+1,(int)(cmbt_y-effect[m].y2),CMBT_WIDTH-1,CMBT_HEIGHT-1,255);
						//draw_line5((int)(effect[m].x-cmbt_x),(int)(cmbt_y-effect[m].y)-1,(int)(effect[m].x2-cmbt_x),(int)(cmbt_y-effect[m].y2)-1,CMBT_WIDTH-1,CMBT_HEIGHT-1,255);
						continue;
					}
				// src_rect は ソースサーフェスのレクタングルです。
				src_rect.left = sprt[SUB_UNIT].base_x+(sprt[SUB_UNIT].wd * (sprt[SUB_UNIT].no % sprt[SUB_UNIT].os_of_x)) +1;
				src_rect.top = sprt[SUB_UNIT].base_y+(sprt[SUB_UNIT].ht* (sprt[SUB_UNIT].no / sprt[SUB_UNIT].os_of_x)) +1;
				src_rect.right = (src_rect.left + sprt[SUB_UNIT].wd)-1;
				src_rect.bottom = (src_rect.top + sprt[SUB_UNIT].ht)-1;
				// dstn_rect は ディスティネーションレクタングルです。
				dstn_rect.left=sprt[SUB_UNIT].x-sprt[SUB_UNIT].cx;
				dstn_rect.top=sprt[SUB_UNIT].y-sprt[SUB_UNIT].cy;
				dstn_rect.right=dstn_rect.left+sprt[SUB_UNIT].wd-1;
				dstn_rect.bottom=dstn_rect.top+sprt[SUB_UNIT].ht-1;

				if( n!=0 )
					{
					if( same_rect(ref dstn_rect,ref src_rect,ref *pfield_rect)!=0 )
						{
						
	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,DDBLTFAST_SRCCOLORKEY) )
		{
		restoreAll();
		}

						}
					}
				else
					{
					if( same_rect(ref dstn_rect,ref src_rect,ref *pinfo_rect)!=0 )
						{
						
	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,DDBLTFAST_SRCCOLORKEY) )
		{
		restoreAll();
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
public void cont_lower_effect(RECT* pfield_rect,RECT* pinfo_rect)
	{
	RECT src_rect,dstn_rect;
	int	m,no1,n=default /* C4701 */,size=default /* C4701 */,flg;
	double	wrk_x,wrk_y,drctn,dstc;





	//=========		 Ｌｏｗｅｒのエフェクト描画		=========//
	no1=7;
	for(m=1;m<EFFECT_MAX/*255*/;m++)
		{
		if( effect[m].layer==EffectLayer.Lower )
			{
			if( effect[m].info[1]!=3 || game_end!=GameResult.None )
				{
				// ユニットインフォ画面以外のエフェクト表示は見える見えないのテストをします。
				flg=0;
				if(game_end!=GameResult.None)
					flg=1;
				for(n=1;n<=max_unit;n++)
					{
					if( unit[n].used!=0 && unit[n].used==your_side && unit[n].PlaneState!=UnitState.Parked )
						{
						// マイユニットからこのエフェクトが見えるか
						// 現地点からユニット地点への距離
						wrk_x=unit[n].Position.X;
						wrk_y=unit[n].Position.Y;
						if( unit[n].kind==UnitKind.Fighter )
							{	// 航空機の場合はちょっと前へ
							wrk_x+=cos(unit[n].drctn*a_PI)*FT_EYE;
							wrk_y+=sin(unit[n].drctn*a_PI)*FT_EYE;
							}

						wrk_x=wrk_x-effect[m].Position.X;
						wrk_y=wrk_y-effect[m].Position.Y;

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

						dstc=((wrk_x)/(cos(drctn*a_PI)));

						switch( unit[n].kind )
							{
							case UnitKind.Battleship:		size=BB1_SIGHT;		break;
							case UnitKind.Cruiser:		size=CA1_SIGHT;		break;
							case UnitKind.Destroyer:		size=DD1_SIGHT;		break;
							case UnitKind.Submarine:		size=SS1_SIGHT;		break;
							case UnitKind.Carrier:		size=CV1_SIGHT;		break;
							case UnitKind.LightCarrier:		size=CVL1_SIGHT;	break;
							case UnitKind.Transport:		size=TR1_SIGHT;		break;
							case UnitKind.Fighter:		size=FT1_SIGHT;		break;
							case UnitKind.Attacker:		size=AT1_SIGHT;		break;
							case UnitKind.Bomber:		size=BM1_SIGHT;		break;
							case UnitKind.AirBase: case UnitKind.NavalBase:		size=AP_SIGHT;		break;		
							case UnitKind.City:		size=CT1_SIGHT;		break;
							case UnitKind.Mine:		size=MN1_SIGHT;		break;
							case UnitKind.InfantryBase:		size=GF1_SIGHT;		break;
							case UnitKind.Pillboxes:		size=GF2_SIGHT;		break;
							case UnitKind.Fortress:		size=GF3_SIGHT;		break;
							}

						if( unit[n].kind>=UnitKind.AirBase && unit[n].kind<=UnitKind.Fortress && unit[n].info[0]!=0 )
							{
							// 工事中は視界を制限
							size=FT1_SIGHT/2;
							}

						if( dstc<=size)
							{
							flg=1;
							}
						}
					}
				}
			else
				{
				// ユニットインフォ内のエフェクトは必ず表示します。
				flg=1;
				}

//			effect[m].info[0]--;
//			if( !effect[m].info[0] )
//				effect[m].layer=0;


			if( flg!=0 )
				{
				// マイユニットからの見える
				switch( effect[m].info[1] )
					{
					case 0:		// 
						sprt[SUB_UNIT].no=effect[m].no;//+(os[no1].os_of_x);
						sprt[SUB_UNIT].x=(int)(effect[m].Position.X-CameraPosition.X);
						sprt[SUB_UNIT].y=(int)(CameraPosition.Y-effect[m].Position.Y);
						n=1;
						break;
					case 1:		// 対空機関砲弾がヒット
						sprt[SUB_UNIT].no=effect[m].no+(sprt[SUB_UNIT].os_of_x/*os[no1].os_of_x*/*(effect[m].info[0]%2));
						sprt[SUB_UNIT].x=(int)(effect[m].Position.X-CameraPosition.X);
						sprt[SUB_UNIT].y=(int)(CameraPosition.Y-effect[m].Position.Y);
						n=1;
						break;
					case 2:		//	飛行機からの煙
						sprt[SUB_UNIT].no=effect[m].no+(sprt[SUB_UNIT].os_of_x*(my_rnd(2)));
						sprt[SUB_UNIT].x=(int)(effect[m].Position.X-CameraPosition.X)+my_rnd(10)-5;
						sprt[SUB_UNIT].y=(int)(CameraPosition.Y-effect[m].Position.Y)+my_rnd(10)-5;
						n=1;
						break;
					case 3:		//	駐機場の飛行機用
						sprt[SUB_UNIT].no=effect[m].no;
						sprt[SUB_UNIT].x=(int)(effect[m].Position.X);
						sprt[SUB_UNIT].y=(int)(effect[m].Position.Y);
						n=0;
						break;
					case 4:		// 雷跡、航跡
						sprt[SUB_UNIT].no=effect[m].no+(sprt[SUB_UNIT].os_of_x*(my_rnd(2)));
						sprt[SUB_UNIT].x=(int)(effect[m].Position.X-CameraPosition.X);
						sprt[SUB_UNIT].y=(int)(CameraPosition.Y-effect[m].Position.Y);
						n=1;
						break;



					case 12:	// 矩形
						draw_line5((int)(effect[m].Position.X-effect[m].EndPosition.X-CameraPosition.X),(int)(CameraPosition.Y-effect[m].Position.Y-effect[m].EndPosition.Y),(int)(effect[m].Position.X+effect[m].EndPosition.X-CameraPosition.X),(int)(CameraPosition.Y-effect[m].Position.Y-effect[m].EndPosition.Y),CMBT_WIDTH-1,CMBT_HEIGHT-1,255);
						draw_line5((int)(effect[m].Position.X+effect[m].EndPosition.X-CameraPosition.X),(int)(CameraPosition.Y-effect[m].Position.Y-effect[m].EndPosition.Y),(int)(effect[m].Position.X+effect[m].EndPosition.X-CameraPosition.X),(int)(CameraPosition.Y-effect[m].Position.Y+effect[m].EndPosition.Y),CMBT_WIDTH-1,CMBT_HEIGHT-1,255);						//draw_line5((int)(effect[m].x-cmbt_x),(int)(cmbt_y-effect[m].y),(int)(effect[m].x2-cmbt_x),(int)(cmbt_y-effect[m].y2),CMBT_WIDTH-1,CMBT_HEIGHT-1,255);
						draw_line5((int)(effect[m].Position.X-effect[m].EndPosition.X-CameraPosition.X),(int)(CameraPosition.Y-effect[m].Position.Y+effect[m].EndPosition.Y),(int)(effect[m].Position.X+effect[m].EndPosition.X-CameraPosition.X),(int)(CameraPosition.Y-effect[m].Position.Y+effect[m].EndPosition.Y),CMBT_WIDTH-1,CMBT_HEIGHT-1,255);						//draw_line5((int)(effect[m].x-cmbt_x),(int)(cmbt_y-effect[m].y),(int)(effect[m].x2-cmbt_x),(int)(cmbt_y-effect[m].y2),CMBT_WIDTH-1,CMBT_HEIGHT-1,255);
						draw_line5((int)(effect[m].Position.X-effect[m].EndPosition.X-CameraPosition.X),(int)(CameraPosition.Y-effect[m].Position.Y+effect[m].EndPosition.Y),(int)(effect[m].Position.X-effect[m].EndPosition.X-CameraPosition.X),(int)(CameraPosition.Y-effect[m].Position.Y-effect[m].EndPosition.Y),CMBT_WIDTH-1,CMBT_HEIGHT-1,255);						//draw_line5((int)(effect[m].x-cmbt_x),(int)(cmbt_y-effect[m].y),(int)(effect[m].x2-cmbt_x),(int)(cmbt_y-effect[m].y2),CMBT_WIDTH-1,CMBT_HEIGHT-1,255);
						continue;
					}
				// src_rect は ソースサーフェスのレクタングルです。
				src_rect.left = sprt[SUB_UNIT].base_x+(sprt[SUB_UNIT].wd * (sprt[SUB_UNIT].no % sprt[SUB_UNIT].os_of_x)) +1;
				src_rect.top = sprt[SUB_UNIT].base_y+(sprt[SUB_UNIT].ht* (sprt[SUB_UNIT].no / sprt[SUB_UNIT].os_of_x)) +1;
				src_rect.right = (src_rect.left + sprt[SUB_UNIT].wd)-1;
				src_rect.bottom = (src_rect.top + sprt[SUB_UNIT].ht)-1;
				// dstn_rect は ディスティネーションレクタングルです。
				dstn_rect.left=sprt[SUB_UNIT].x-sprt[SUB_UNIT].cx;
				dstn_rect.top=sprt[SUB_UNIT].y-sprt[SUB_UNIT].cy;
				dstn_rect.right=dstn_rect.left+sprt[SUB_UNIT].wd-1;
				dstn_rect.bottom=dstn_rect.top+sprt[SUB_UNIT].ht-1;

				if( n!=0 )
					{
					if( same_rect(ref dstn_rect,ref src_rect,ref *pfield_rect)!=0 )
						{
						if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,DDBLTFAST_SRCCOLORKEY) )
							{
							restoreAll();
							}

						}
					}
				else
					{
					if( same_rect(ref dstn_rect,ref src_rect,ref *pinfo_rect)!=0 )
						{
						if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,DDBLTFAST_SRCCOLORKEY) )
							{
							restoreAll();
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
public void	draw_cloud(RECT* pfield_rect)
	{
	int	n,no1;
	RECT src_rect,dstn_rect;


	no1=4;	//OS Number
	for(n=0;n<KUMO_MAX;n++)
		{
		if( kumo[n].used!=0 )
			{
			sprt[MAP_TIP_NRML].x=(int)(kumo[n].Position.X-CameraPosition.X);
			sprt[MAP_TIP_NRML].y=(int)(CameraPosition.Y-kumo[n].Position.Y);
			sprt[MAP_TIP_NRML].no=2;
			// src_rect は ソースサーフェスのレクタングルです。
			src_rect.left = sprt[MAP_TIP_NRML].base_x+(sprt[MAP_TIP_NRML].wd * (sprt[MAP_TIP_NRML].no % sprt[MAP_TIP_NRML].os_of_x));
			src_rect.top = sprt[MAP_TIP_NRML].base_y+(sprt[MAP_TIP_NRML].ht* (sprt[MAP_TIP_NRML].no / sprt[MAP_TIP_NRML].os_of_x)) ;
			src_rect.right = (src_rect.left + sprt[MAP_TIP_NRML].wd);
			src_rect.bottom = (src_rect.top + sprt[MAP_TIP_NRML].ht);

			// dstn_rect は ディスティネーションレクタングルです。
			dstn_rect.left=sprt[MAP_TIP_NRML].x-(sprt[MAP_TIP_NRML].wd/2);
			dstn_rect.top=sprt[MAP_TIP_NRML].y-(sprt[MAP_TIP_NRML].ht/2);
			dstn_rect.right=dstn_rect.left+sprt[MAP_TIP_NRML].wd;
			dstn_rect.bottom=dstn_rect.top+sprt[MAP_TIP_NRML].ht;

			if( same_rect(ref dstn_rect,ref src_rect,ref *pfield_rect)!=0 )
				{
				
	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,DDBLTFAST_SRCCOLORKEY ) )
		{
		restoreAll();
		}

				}
			}
		}
	}








//============================================================================
//
//----------------------------------------------------------------------------
public void	be_dstryd(int m)
	{
	int	f,i;





	if( !(unit[m].kind>=UnitKind.AirBase && unit[m].kind<=UnitKind.Fortress ) )
		{
		// 沈没の水門
		f=seek_effect_no();
		effect[f].layer=EffectLayer.Lower;	
		//effect[f].kind=THERE;
		effect[f].info[0]=220;
		effect[f].info[1]=0;
		effect[f].Position=unit[m].Position;
		effect[f].no=7;			// ソースファイル上の番号	

		}


	if( unit[m].kind==UnitKind.Battleship || unit[m].kind==UnitKind.Carrier || unit[m].kind==UnitKind.LightCarrier || unit[m].kind==UnitKind.NavalBase || unit[m].kind==UnitKind.AirBase || unit[m].kind==UnitKind.Fortress )
		{
		// 大型艦船
		// 沈没の小水紋
		//if( unit[m].kind!=AP && unit[m].kind!=SP )
		if( !(unit[m].kind>=UnitKind.AirBase && unit[m].kind<=UnitKind.Fortress ) )
			{
			for(i=0;i<=7;i++)
				{
				f=seek_effect_no();			
				effect[f].layer=EffectLayer.Lower;	
				effect[f].info[0]=200+rnd(20);
				effect[f].info[1]=4;
				effect[f].Position = new WorldPosition(unit[m].Position.X+rnd(100)-50, unit[m].Position.Y+rnd(100)-50);
				effect[f].no=8;			// ソースファイル上の番号	
				}
			}
		// 沈没の煙
		for(i=0;i<=2;i++)
			{
			f=seek_effect_no();			
			effect[f].layer=EffectLayer.Upper;	
			effect[f].info[0]=150+rnd(20);
			effect[f].info[1]=4;
			effect[f].Position = new WorldPosition(unit[m].Position.X+rnd(30)-15, unit[m].Position.Y+rnd(30)-15);
			effect[f].no=5+rnd(2);			// ソースファイル上の番号	
			}
		// 沈没の爆炎
		for(i=0;i<=4;i++)
			{
			f=seek_effect_no();			
			effect[f].layer=EffectLayer.Upper;	
			effect[f].info[0]=30+rnd(20);
			effect[f].info[1]=4;
			effect[f].Position = new WorldPosition(unit[m].Position.X+rnd(40)-20, unit[m].Position.Y+rnd(40)-20);
			effect[f].no=9;			// ソースファイル上の番号	
			}
		// 沈没の小爆炎
		for(i=0;i<=4;i++)
			{
			f=seek_effect_no();			
			effect[f].layer=EffectLayer.Upper;	
			effect[f].info[0]=40+rnd(20);
			effect[f].info[1]=4;
			effect[f].Position = new WorldPosition(unit[m].Position.X+rnd(60)-30, unit[m].Position.Y+rnd(60)-30);
			effect[f].no=10;			// ソースファイル上の番号	
			}
		}
	else
		{
		// 中小型艦船
		// 沈没の小水紋
		if( !(unit[m].kind>=UnitKind.AirBase && unit[m].kind<=UnitKind.Fortress ) )
			{
			for(i=0;i<=3;i++)
				{
				f=seek_effect_no();			
				effect[f].layer=EffectLayer.Lower;	
				effect[f].info[0]=200+rnd(20);
				effect[f].info[1]=4;
				effect[f].Position = new WorldPosition(unit[m].Position.X+rnd(100)-50, unit[m].Position.Y+rnd(100)-50);
				effect[f].no=8;			// ソースファイル上の番号	
				}
			}
		// 沈没の煙
		for(i=0;i<=1;i++)
			{
			f=seek_effect_no();			
			effect[f].layer=EffectLayer.Upper;	
			effect[f].info[0]=150+rnd(20);
			effect[f].info[1]=4;
			effect[f].Position = new WorldPosition(unit[m].Position.X+rnd(30)-15, unit[m].Position.Y+rnd(30)-15);
			effect[f].no=5+rnd(2);			// ソースファイル上の番号	
			}
		// 沈没の爆炎
		for(i=0;i<=0;i++)
			{
			f=seek_effect_no();			
			effect[f].layer=EffectLayer.Upper;	
			effect[f].info[0]=30+rnd(20);
			effect[f].info[1]=4;
			effect[f].Position = new WorldPosition(unit[m].Position.X+rnd(40)-20, unit[m].Position.Y+rnd(40)-20);
			effect[f].no=9;			// ソースファイル上の番号	
			}
		// 沈没の小爆炎
		for(i=0;i<=1;i++)
			{
			f=seek_effect_no();			
			effect[f].layer=EffectLayer.Upper;	
			effect[f].info[0]=20+rnd(20);
			effect[f].info[1]=4;
			effect[f].Position = new WorldPosition(unit[m].Position.X+rnd(20)-10, unit[m].Position.Y+rnd(20)-10);
			effect[f].no=10;			// ソースファイル上の番号	
			}

		}
	}










//============================================================================
//コンバットエリア描画 同時にユーザー(通信対戦時はホスト)入力を受け付けます
//----------------------------------------------------------------------------
public void	draw_cmbt_area()
	{
	RECT	src_rect,field_rect,info_rect,dstn_rect,wrk_rect;
	int	i,m,n,f,h,no1,s,sign,cl,lc_ri_btn,lc_lf_btn,right,bottom,j,j2; Array2<int> pp_on = default;
	int	cm_scrn_x,cm_scrn_y; Array256<int> chk = default;
	double	wrk_x,wrk_y,wrk_x2,wrk_y2,drctn,drctn2,wrk_x3,wrk_y3,dstc;
	int		map_bld_x,map_bld_y,flg;
	Array256<byte> cBuf = default;
    Array5<Array128<byte>> ach = default;
    Array5<int> len = default;
	HDC					hdc;
	int	plane_fling_sound;




	plane_fling_sound=0;

	// field_rect は 戦域画面のレクタングルです。
	field_rect.left=0;
	field_rect.top=0;
	field_rect.right=CMBT_WIDTH;
	field_rect.bottom=CMBT_HEIGHT;

	// info_rect は インフォのレクタングルです

	info_rect.left=sprt[UNIT_INFO_JPN].x;
	info_rect.top=sprt[UNIT_INFO_JPN].y;
	info_rect.right=info_rect.left+sprt[UNIT_INFO_JPN].wd;
	info_rect.bottom=info_rect.top+sprt[UNIT_INFO_JPN].ht;





	// カーソルの示す、マップチップの場所
	map_bld_x=(int)(((crsr_pt.x+40+(int)CameraPosition.X)-MAP_LEFT)/sprt[MAP_TIP_NRML].wd);
	map_bld_y=(int)((MAP_TOP-((int)CameraPosition.Y-crsr_pt.y-40 ))/sprt[MAP_TIP_NRML].ht);


	// 標準キャラよう背景の表示
	cm_scrn_x=(int)((CameraPosition.X-MAP_LEFT)/sprt[MAP_TIP_NRML].wd);
	cm_scrn_y=(int)((MAP_TOP-CameraPosition.Y)/sprt[MAP_TIP_NRML].ht);




	for(m=0;m<=10;m++)
		{
		for(n=0;n<=10;n++)
			{
			if(  map_edit==0 && /*!reveal &&*/ game_end==GameResult.None /*&& FrameRate>=6*/ && cmbt_map[cm_scrn_y+n][cm_scrn_x+m]==0 )
				{
				flg=0;
				for( f=1; f<=max_unit; f++)
					{
					if( unit[f].used!=0 && unit[f].used==your_side && unit[f].PlaneState!=UnitState.Parked /*&& unit[f].spry==0*/ )
						{
						// 現地点からユニット地点への距離
						//wrk_x=unit[f].x-(cmbt_x-40+(m*sprt[MAP_TIP_NRML].wd));
						//wrk_y=unit[f].y-(cmbt_y+40-(n*sprt[MAP_TIP_NRML].ht));

						if(unit[f].kind==UnitKind.Fighter /*&& 0*/)
							{
							// 戦闘機場合、視点を
							wrk_x=unit[f].Position.X;
							wrk_y=unit[f].Position.Y;
							wrk_x+=cos(unit[f].drctn*a_PI)*FT_EYE;
							wrk_y+=sin(unit[f].drctn*a_PI)*FT_EYE;
							wrk_x=wrk_x-(((cm_scrn_x+m)*sprt[MAP_TIP_NRML].wd)-MAP_RIGHT);
							wrk_y=wrk_y-(MAP_TOP-((cm_scrn_y+n)*sprt[MAP_TIP_NRML].ht));
							}
						else
							{
							wrk_x=unit[f].Position.X-(((cm_scrn_x+m)*sprt[MAP_TIP_NRML].wd)-MAP_RIGHT);
							wrk_y=unit[f].Position.Y-(MAP_TOP-((cm_scrn_y+n)*sprt[MAP_TIP_NRML].ht));
							}

						//((cm_scrn_x+m)*sprt[MAP_TIP_NRML].wd)-MAP_RIGHT
						//MAP_TOP-((cm_scrn_x+m)*sprt[MAP_TIP_NRML].wd)


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
						if( dstc<=(double)find_out_size(f,0) )
							{
							flg=1;
							f=max_unit;
							break;
							}
						}
					}
				}
			else
				{
				flg=1;
				}



			sprt[MAP_TIP_NRML].x=(m*sprt[MAP_TIP_NRML].wd);
			sprt[MAP_TIP_NRML].y=(n*sprt[MAP_TIP_NRML].ht);

			sprt[MAP_TIP_NRML].x-=(int)(CameraPosition.X-MAP_LEFT)%sprt[MAP_TIP_NRML].wd;
			sprt[MAP_TIP_NRML].y-=(int)(MAP_TOP-CameraPosition.Y)%sprt[MAP_TIP_NRML].ht;


			if( cmbt_map[cm_scrn_y+n][cm_scrn_x+m]==0)
				{	
				if(flg!=0)
					{
					// 見える範囲内の海
					sprt[MAP_TIP_NRML].no=(FrameCount/30)%2;							// ただの海
					}
				else
					{
					//見えない範囲内の海
					sprt[MAP_TIP_NRML].no=3;							// ただの海
					continue;
					}
				}



			if( cmbt_map[cm_scrn_y+n][cm_scrn_x+m]>=1)
				{
				sprt[MAP_TIP_NRML].no=6+(cmbt_map[cm_scrn_y+n][cm_scrn_x+m]-1);	// 陸地
				}


			// マップエディット時のプログ
			if( map_edit!=0 && (cm_scrn_y+n)==(map_bld_y) && (cm_scrn_x+m)==(map_bld_x) )
				{
				if ( (FrameCount%2)!=0 )
					sprt[MAP_TIP_NRML].no=3;
				GetKeyboardState(cBuf);

				if( (cBuf[VK_NUMPAD1]&0x80)!=0 )
					{
					cmbt_map[cm_scrn_y+n][cm_scrn_x+m]=1;
					}
				if( (cBuf[VK_NUMPAD2]&0x80)!=0 )
					{
					cmbt_map[cm_scrn_y+n][cm_scrn_x+m]=2;
					}
				if( (cBuf[VK_NUMPAD3]&0x80)!=0 )
					{
					cmbt_map[cm_scrn_y+n][cm_scrn_x+m]=3;
					}
				if( (cBuf[VK_NUMPAD4]&0x80)!=0 )
					{
					cmbt_map[cm_scrn_y+n][cm_scrn_x+m]=4;
					}
				if( (cBuf[VK_NUMPAD5]&0x80)!=0 )
					{
					cmbt_map[cm_scrn_y+n][cm_scrn_x+m]=5;
					}
				if( (cBuf[VK_NUMPAD6]&0x80)!=0 )
					{
					cmbt_map[cm_scrn_y+n][cm_scrn_x+m]=6;
					}
				if( (cBuf[VK_NUMPAD7]&0x80)!=0 )
					{
					cmbt_map[cm_scrn_y+n][cm_scrn_x+m]=7;
					}
				if( (cBuf[VK_NUMPAD8]&0x80)!=0 )
					{
					cmbt_map[cm_scrn_y+n][cm_scrn_x+m]=8;
					}
				if( (cBuf[VK_NUMPAD9]&0x80)!=0 )
					{
					cmbt_map[cm_scrn_y+n][cm_scrn_x+m]=9;
					}
				if( (cBuf[VK_NUMPAD0]&0x80)!=0 )
					{
					cmbt_map[cm_scrn_y+n][cm_scrn_x+m]=0;
					}
				}


			// src_rect は ソースサーフェスのレクタングルです。
			src_rect.left = sprt[MAP_TIP_NRML].base_x+(sprt[MAP_TIP_NRML].wd * (sprt[MAP_TIP_NRML].no % sprt[MAP_TIP_NRML].os_of_x)) ;
			src_rect.top = sprt[MAP_TIP_NRML].base_y+(sprt[MAP_TIP_NRML].ht* (sprt[MAP_TIP_NRML].no / sprt[MAP_TIP_NRML].os_of_x)) ;
			src_rect.right = (src_rect.left + sprt[MAP_TIP_NRML].wd);
			src_rect.bottom = (src_rect.top + sprt[MAP_TIP_NRML].ht);

			// dstn_rect は ディスティネーションレクタングルです。
			dstn_rect.left=sprt[MAP_TIP_NRML].x-sprt[MAP_TIP_NRML].cx;
			dstn_rect.top=sprt[MAP_TIP_NRML].y-sprt[MAP_TIP_NRML].cy;
			dstn_rect.right=dstn_rect.left+sprt[MAP_TIP_NRML].wd;
			dstn_rect.bottom=dstn_rect.top+sprt[MAP_TIP_NRML].ht;

			if( same_rect(ref dstn_rect,ref src_rect,ref field_rect)!=0 )
				{
				if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,0) )
					{
					restoreAll();
					}
				}
			}
		}





	// ロウエフェクト
	cont_lower_effect( &field_rect,&info_rect );

	// 自サイドユニットからの距離により、可視不可視
	//find_out(m);



	new_pp[0].used=0;
	new_slct[0].sw=0;

	new_pp[1].used=0;
	new_slct[1].sw=0;


	// 標準キャラの表示
	lc_ri_btn=ri_btn;
	lc_lf_btn=lf_btn;

	if( /*(0 || cnct_game )  &&*/ you_ordered!=0 && game_end==GameResult.None)
		{
		lc_ri_btn=0;
		lc_lf_btn=0;
		}


	if( lc_ri_btn==1 || lc_lf_btn==1 )
		SoundPlayEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);







	for( m=0; m<=max_unit; m++)
		{
		if( m==JPN_PLANE_START/*41*/ )
			{	
			// 雲を描画します。
			draw_cloud( &field_rect );
			}



		if( unit[m].used==Side.Japan)
			no1=UNIT_JPN;		//Off Screen Number		日本海軍の表示
		else
			no1=UNIT_USA;		//Off Screen Number		合衆国海軍の表示





		// ユニットを描画します
		if( unit[m].used!=0 && (unit[m].used==your_side || unit[m].found!=0) && 
	( ( ( CameraPosition.X-CMBT_REST<=unit[m].Position.X && CameraPosition.X+CMBT_WIDTH+CMBT_REST>=unit[m].Position.X) && (CameraPosition.Y+CMBT_REST>=unit[m].Position.Y && CameraPosition.Y-CMBT_HEIGHT-CMBT_REST<=unit[m].Position.Y) )
	|| (unit[m].ctgry==UnitCategory.Plane && unit[m].PlaneState==UnitState.Parked )
	)
			)
			{
			switch((int)(unit[m].drctn/22.5))
				{
				case 0: case 15:
					unit[m].os_indx_x=2;
					break;
				case 1: case 2:
					unit[m].os_indx_x=1;
					break;
				case 3: case 4:
					unit[m].os_indx_x=0;
					break;
				case 5: case 6:
					unit[m].os_indx_x=7;
					break;
				case 7: case 8:
					unit[m].os_indx_x=6;
					break;
				case 9: case 10:
					unit[m].os_indx_x=5;
					break;
				case 11: case 12:
					unit[m].os_indx_x=4;
					break;
				case 13: case 14:
					unit[m].os_indx_x=3;
					break;
				default:
					unit[m].os_indx_x=2;
					break;
				}


			if( unit[m].ctgry==UnitCategory.Plane && unit[m].PlaneState==UnitState.Parked)
				{
				if( unit[m].info[1]==unit_info[3] && ((UnitKind)unit_info[0]==UnitKind.Carrier || (UnitKind)unit_info[0]==UnitKind.LightCarrier || (UnitKind)unit_info[0]==UnitKind.AirBase ) )
					{
					if( ( (unit[m].info[5]<=SLOW) && (unit_info[1]==1&&unit[m].info[3]<=2)||(unit_info[1]==0&&unit[m].info[3]>=3)) 
					 || ( unit[m].info[5]==RETURN && (unit_info[1]==0) ) )
						{
						// 駐機中のの飛行機
						sprt[no1].no=(unit[m].os_indx_y*8)+unit[m].os_indx_x;

						sprt[no1].x=(int)unit[m].Position.X;
						sprt[no1].y=(int)unit[m].Position.Y;

						// src_rect は ソースサーフェスのレクタングルです。
						src_rect.left = sprt[no1].base_x+(sprt[no1].wd * (sprt[no1].no % sprt[no1].os_of_x)) +1;
						src_rect.top = sprt[no1].base_y+(sprt[no1].ht* (sprt[no1].no / sprt[no1].os_of_x)) +1;
						src_rect.right = (src_rect.left + sprt[no1].wd)-2;
						src_rect.bottom = (src_rect.top + sprt[no1].ht)-2;

						// dstn_rect は ディスティネーションレクタングルです。

						dstn_rect.left=sprt[no1].x-sprt[no1].cx;
						dstn_rect.top=sprt[no1].y-sprt[no1].cy;
						dstn_rect.right=dstn_rect.left+sprt[no1].wd-2;
						dstn_rect.bottom=dstn_rect.top+sprt[no1].ht-2;


						if( same_rect(ref dstn_rect,ref src_rect,ref info_rect)!=0 )
							{
							if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,DDBLTFAST_SRCCOLORKEY) )
								{
								restoreAll();
								}
							}
						}
					}
				}
			else
				{
				sprt[no1].x=(int)(unit[m].Position.X-CameraPosition.X);
				sprt[no1].y=(int)(CameraPosition.Y-unit[m].Position.Y);


				if( unit[m].kind==UnitKind.Submarine && unit[m].info[6]!=0 && game_end==GameResult.None )
					{	// 潜航潜水艦
					sprt[no1].no=((unit[m].os_indx_y+1)*8)+unit[m].os_indx_x;	
					if( unit[m].used!=your_side )
						sprt[no1].x=-999;				// それが敵潜水艦なら表示を外す
					}
				else
					{
					sprt[no1].no=(unit[m].os_indx_y*8)+unit[m].os_indx_x;
					}


				// src_rect は ソースサーフェスのレクタングルです。
				src_rect.left = sprt[no1].base_x+(sprt[no1].wd * (sprt[no1].no % sprt[no1].os_of_x)) +1;
				src_rect.top = sprt[no1].base_y+(sprt[no1].ht* (sprt[no1].no / sprt[no1].os_of_x)) +1;
				src_rect.right = (src_rect.left + sprt[no1].wd)-2;
				src_rect.bottom = (src_rect.top + sprt[no1].ht)-2;


				// dstn_rect は ディスティネーションレクタングルです。
				dstn_rect.left=sprt[no1].x-sprt[no1].cx;
				dstn_rect.top=sprt[no1].y-sprt[no1].cy;
				dstn_rect.right=dstn_rect.left+sprt[no1].wd-2;
				dstn_rect.bottom=dstn_rect.top+sprt[no1].ht-2;
				if( same_rect(ref dstn_rect,ref src_rect,ref field_rect)!=0 )
					{
					if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,DDBLTFAST_SRCCOLORKEY) )
						{
						restoreAll();
						}


					if( map_edit==0 && plane_fling_sound==0 && game_end==GameResult.None && unit[m].ctgry==UnitCategory.Plane && unit[m].PlaneState==UnitState.Flying && (FrameCount%10)==0 )
						{
						SoundPlayEffect( 0, PLANE_FLYING ,unit[m].Position.X, unit[m].Position.Y);
						plane_fling_sound=1;
						}

					}
				}
			}



		var goto_dca1=false;	// goto dca1, into the block below, which C# does not allow
#if true
		if (  m==0 && the_slct_unit!=0 && unit[the_slct_unit].kind==UnitKind.Transport && m!=the_slct_unit)
			{
			// カーソルのある場所が
			// カーソルの示す、マップチップの場所
			map_bld_x=(int)(((crsr_pt.x+40+(int)CameraPosition.X)-MAP_LEFT)/sprt[MAP_TIP_NRML].wd);
			map_bld_y=(int)((MAP_TOP-((int)CameraPosition.Y-crsr_pt.y-40 ))/sprt[MAP_TIP_NRML].ht);
			if(
				cmbt_map[map_bld_y][map_bld_x]==9 || 
				cmbt_map[map_bld_y][map_bld_x]==8 || 
				cmbt_map[map_bld_y][map_bld_x]==7 || 
				cmbt_map[map_bld_y][map_bld_x]==5 || 
				cmbt_map[map_bld_y][map_bld_x]==2  
				)
				{
				sprt[no1].x=(((crsr_pt.x+40+(int)(CameraPosition.X-MAP_LEFT)%sprt[MAP_TIP_NRML].wd)/80)*80)-(int)(CameraPosition.X-MAP_LEFT)%sprt[MAP_TIP_NRML].wd;
				sprt[no1].y=(((crsr_pt.y+40+(int)(MAP_TOP-CameraPosition.Y)%sprt[MAP_TIP_NRML].ht)/80)*80)-(int)(MAP_TOP-CameraPosition.Y)%sprt[MAP_TIP_NRML].ht;

				goto_dca1=true;		// goto		dca1;
				}
			}
#endif




		if( goto_dca1 || unit[m].used!=0 && (unit[m].used==your_side || unit[m].found!=0)  )
			{
			if( !goto_dca1 )
			{

//			if( unit[m].used==JPN)
//				no1=UNIT_JPN;		//Off Screen Number		日本海軍の表示
//			else
//				no1=UNIT_USA;		//Off Screen Number		合衆国海軍の表示


			//The Slct された機体への移動予定の線引き、
			if( unit[m].ctgry==UnitCategory.Plane && unit[m].PlaneState==UnitState.Parked )
				{
/*
				continue;
*/

				if( unit[m].info[1]==unit_info[3] &&  ((UnitKind)unit_info[0]==UnitKind.Carrier || (UnitKind)unit_info[0]==UnitKind.LightCarrier || (UnitKind)unit_info[0]==UnitKind.AirBase ) )
					{
					if( ( (unit[m].info[5]<=SLOW)  && (unit_info[1]==1&&unit[m].info[3]<=2)||(unit_info[1]==0&&unit[m].info[3]>=3)) 
					 || (unit[m].info[5]==RETURN && (unit_info[1]==0) ) )
						{	
						// 空母で飛行甲板か格納庫かで航空機を表示するかしない。
						sprt[no1].x=(int)unit[m].Position.X;
						sprt[no1].y=(int)unit[m].Position.Y;
/*
						if(unit_info[1]==0 && unit[m].info[3]>=3 && (int)unit[m].y==sprt[UNIT_INFO_JPN].y+370-120)
							{
							SoundPlayEffect( NULL, TAKE_OFF,(double)(MAP_RIGHT+1), 0);
							
							}
*/
						}
					else
						continue;
					}
				else
					continue;

				}
			else
				{		
				if( unit[m].used!=your_side && unit[m].kind==UnitKind.Submarine && unit[m].info[6]!=0 )
					{	// およその敵潜航潜水艦
					sprt[no1].x=(int)(unit[m].info[7]-CameraPosition.X);
					sprt[no1].y=(int)(CameraPosition.Y-unit[m].info[8]);
					}
				else
					{	// マップ上のユニット

					sprt[no1].x=(int)(unit[m].Position.X-CameraPosition.X);
					sprt[no1].y=(int)(CameraPosition.Y-unit[m].Position.Y);

					}
				}
			}



// dca1:
			dstn_rect.left=sprt[no1].x-sprt[no1].cx;
			dstn_rect.top=sprt[no1].y-sprt[no1].cy;
			dstn_rect.right=dstn_rect.left+sprt[no1].wd-2;
			dstn_rect.bottom=dstn_rect.top+sprt[no1].ht-2;

			wrk_rect.left=dstn_rect.left+20;
			wrk_rect.top=dstn_rect.top+20;
			wrk_rect.right=dstn_rect.right-20;
			wrk_rect.bottom=dstn_rect.bottom-20;



			
			
			// クリック選択・非選択
			if( pt_in_rect(ref wrk_rect,crsr_pt.x,crsr_pt.y)!=0 && !( unit[the_slct_unit].ctgry==UnitCategory.Plane && unit[m].ctgry==UnitCategory.Ship && !(unit[m].kind==UnitKind.Carrier || unit[m].kind==UnitKind.LightCarrier || unit[m].kind==UnitKind.AirBase || unit[the_slct_unit].used!=unit[m].used) ) && !(unit[the_slct_unit].ctgry==UnitCategory.Plane && unit[the_slct_unit].PlaneState==UnitState.Parked && unit[m].ctgry==UnitCategory.Plane && unit[m].PlaneState==UnitState.Flying )  && !( unit[m].ctgry==UnitCategory.Plane && unit[m].PlaneState==UnitState.Parked && unit[m].stop==0 ) && !(unit[the_slct_unit].ctgry==UnitCategory.Plane && unit[the_slct_unit].PlaneState==UnitState.Parked && unit[the_slct_unit].stop==0 ) && !( unit[the_slct_unit].ctgry==UnitCategory.Plane && unit[the_slct_unit].PlaneState==UnitState.Parked && unit[m].ctgry==UnitCategory.Ship ) 
				&& !(unit[the_slct_unit].ctgry==UnitCategory.Ship && unit[m].ctgry==UnitCategory.Plane && unit[m].PlaneState==UnitState.Parked)  && !(the_slct_unit!=0 && unit[the_slct_unit].used!=your_side) && !(the_slct_unit!=0 && unit[the_slct_unit].ctgry==UnitCategory.Plane&&unit[m].Weapon==TUN)
				 && !(unit[m].PlaneState!=UnitState.Parked && crsr_pt.x>=CMBT_WIDTH-1) 
				 && !(unit[m].used!=your_side && unit[m].kind==UnitKind.Submarine && unit[m].info[6]!=0)
				)
				{
				if( (FrameCount%2)!=0 )
					{
					if( unit[m].ctgry==UnitCategory.Plane && unit[m].PlaneState==UnitState.Parked )
						{
						right=SCRN_WIDTH-1; bottom=SCRN_HEIGHT-1;
						}
					else
						{right=CMBT_WIDTH-1; bottom=CMBT_HEIGHT-1;}
					cl=0xffff;

					n=15;
					draw_line4(dstn_rect.left+n,dstn_rect.top+n,dstn_rect.right-n,dstn_rect.top+n,right,bottom,RGB(255,255,255));
					draw_line4(dstn_rect.right-n,dstn_rect.top+n,dstn_rect.right-n,dstn_rect.bottom-n,right,bottom,RGB(255,255,255));
					draw_line4(dstn_rect.right-n,dstn_rect.bottom-n,dstn_rect.left+n,dstn_rect.bottom-n,right,bottom,RGB(255,255,255));
					draw_line4(dstn_rect.left+n,dstn_rect.bottom-n,dstn_rect.left+n,dstn_rect.top+n,right,bottom,RGB(255,255,255));
					}

				if( lc_lf_btn==1 )
					{
					lc_lf_btn=0;
					new_pp[1].used=0;

					if( the_slct_unit==m )
						{ 
						the_slct_unit=0; cmbt_menu_kind=0; cmbt_menu_slctd=0; cls_all_slct_unit_p2(1); 
						}
					else
						{
						if( the_slct_unit==0 ) 
							{
							if( !(unit[m].ctgry==UnitCategory.Plane && unit[m].PlaneState==UnitState.Parked) )
								old_the_slct_unit=(short)m;

							set_the_slct_unit( m );
							}
						else if(map_edit==0)
							{
							if( m!=0 /*unit[the_slct_unit].kind!=TR1*/ )
								{
								// 陸地以外、普通の場合
								new_slct[1].sw=1;
								new_slct[1].the_slct_unit=the_slct_unit;
								new_slct[1].m=(short)m;
								}
							else
								{
								// 陸地指定
								new_slct[1].sw=1;
								new_slct[1].the_slct_unit=the_slct_unit;
								new_slct[1].m=0;
								new_slct[1].GroundPosition = new WorldPosition(CameraPosition.X+sprt[no1].x, CameraPosition.Y-sprt[no1].y);
								}
							}
						}
					}
				}




			if( m==0 )
				continue;


			// 選択されてればマークの絵というか枠 
			if( unit[m].ctgry==UnitCategory.Plane && unit[m].PlaneState==UnitState.Parked )
				{right=SCRN_WIDTH-1; bottom=SCRN_HEIGHT-1;}
			else
				{right=CMBT_WIDTH-1; bottom=CMBT_HEIGHT-1;}
			cl=0xffff;
			if( the_slct_unit==m && m!=0)
				{
				n=10;
				draw_line4(dstn_rect.left+n,dstn_rect.top+n,dstn_rect.right-n,dstn_rect.top+n,right,bottom,RGB(255,255,255));
				draw_line4(dstn_rect.right-n,dstn_rect.top+n,dstn_rect.right-n,dstn_rect.bottom-n,right,bottom,RGB(255,255,255));
				draw_line4(dstn_rect.right-n,dstn_rect.bottom-n,dstn_rect.left+n,dstn_rect.bottom-n,right,bottom,RGB(255,255,255));
				draw_line4(dstn_rect.left+n,dstn_rect.bottom-n,dstn_rect.left+n,dstn_rect.top+n,right,bottom,RGB(255,255,255));
				}
			else
				{
				if( slct_unit[1][m]!=0 )
					{
					n=20;
					draw_line4(dstn_rect.left+n,dstn_rect.top+n,dstn_rect.right-n,dstn_rect.top+n,right,bottom,RGB(255,255,255));
					draw_line4(dstn_rect.right-n,dstn_rect.top+n,dstn_rect.right-n,dstn_rect.bottom-n,right,bottom,RGB(255,255,255));
					draw_line4(dstn_rect.right-n,dstn_rect.bottom-n,dstn_rect.left+n,dstn_rect.bottom-n,right,bottom,RGB(255,255,255));
					draw_line4(dstn_rect.left+n,dstn_rect.bottom-n,dstn_rect.left+n,dstn_rect.top+n,right,bottom,RGB(255,255,255));
					}
				}

			if( the_slct_unit!=0 && unit[the_slct_unit].kind==UnitKind.Transport && unit[the_slct_unit].Target==max_unit+1 && m==the_slct_unit )
				{
				// 輸送船の揚陸先のマーク
				cl=0x1f;
				n=10+(FrameCount%8)*2;
//				draw_line4(0,0,100,100,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));

//					sprt[no1].x=unit[m].x-cmbt_x;
//					sprt[no1].y=cmbt_y-unit[m].y;


				dstn_rect.left=(int)(unit[the_slct_unit].info[6]-CameraPosition.X-40);
				dstn_rect.top=(int)(CameraPosition.Y-unit[the_slct_unit].info[7]-40);
				dstn_rect.right=dstn_rect.left+sprt[no1].wd-2;
				dstn_rect.bottom=dstn_rect.top+sprt[no1].ht-2;

				draw_line4(dstn_rect.left+n,dstn_rect.top+n,dstn_rect.right-n,dstn_rect.top+n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4(dstn_rect.right-n,dstn_rect.top+n,dstn_rect.right-n,dstn_rect.bottom-n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4(dstn_rect.right-n,dstn_rect.bottom-n,dstn_rect.left+n,dstn_rect.bottom-n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4(dstn_rect.left+n,dstn_rect.bottom-n,dstn_rect.left+n,dstn_rect.top+n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));


				}
			else  if( the_slct_unit!=0 && unit[the_slct_unit].Target==m )
				{
				// 攻撃先 としてのマーク
				// B=0xF800 R=0x7E0 G=0x1F
				cl=0x1f;
				n=10+(FrameCount%8)*2;
				draw_line4(dstn_rect.left+n,dstn_rect.top+n,dstn_rect.right-n,dstn_rect.top+n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4(dstn_rect.right-n,dstn_rect.top+n,dstn_rect.right-n,dstn_rect.bottom-n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4(dstn_rect.right-n,dstn_rect.bottom-n,dstn_rect.left+n,dstn_rect.bottom-n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4(dstn_rect.left+n,dstn_rect.bottom-n,dstn_rect.left+n,dstn_rect.top+n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				}
			else  if( the_slct_unit!=0 && unit[the_slct_unit].info[1]==m && unit[the_slct_unit].ctgry==UnitCategory.Plane && unit[the_slct_unit].PlaneState==UnitState.Flying)
				{
				// 攻撃先 としてのマーク
				// B=0xF800 R=0x7E0 G=0x1F
				cl=0x1f;
				//n=10+(FrameCount%8)*2;
				n=15;
				//draw_line4(dstn_rect.left+n,dstn_rect.top+n,dstn_rect.right-n,dstn_rect.top+n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				//draw_line4(dstn_rect.right-n,dstn_rect.top+n,dstn_rect.right-n,dstn_rect.bottom-n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4(dstn_rect.right-n-4,dstn_rect.bottom-n,dstn_rect.left+n+4,dstn_rect.bottom-n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4(dstn_rect.right-n-4,dstn_rect.bottom-n+2,dstn_rect.left+n+4,dstn_rect.bottom-n+2,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				//draw_line4(dstn_rect.left+n,dstn_rect.bottom-n,dstn_rect.left+n,dstn_rect.top+n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				}



				// 攻撃先 着艦先 の方向
			if( unit[m].Target!=0 && unit[m].used==your_side )
				{
				if(  0!=0 && unit[unit[m].Target].kind==UnitKind.Submarine && unit[unit[m].Target].info[6]!=0) 
					{	// 対潜水艦
					wrk_x3=unit[unit[m].Target].info[7];
					wrk_y3=unit[unit[m].Target].info[8];
					}
				else
					{	// 対潜航潜水艦以外
					if( unit[m].kind!=UnitKind.Transport )
						{
						wrk_x3=unit[unit[m].Target].Position.X;
						wrk_y3=unit[unit[m].Target].Position.Y;
						}
					else
						{
						// 揚陸方向
						wrk_x3=(double)unit[m].info[6];
						wrk_y3=(double)unit[m].info[7];
						}
					}

				if( (unit[unit[m].Target].found!=0 || (unit[m].Target==max_unit+1&&unit[m].kind==UnitKind.Transport) )  && !(unit[unit[m].Target].kind==UnitKind.Submarine && unit[unit[m].Target].info[6]!=0) /*unit[unit[m].arm[2]].kind!=SS1*/  )
					{	// 視認
					wrk_x=wrk_x3-unit[m].Position.X;
					wrk_y=wrk_y3-unit[m].Position.Y;
					if( wrk_x==0 )	wrk_x=1;
					if( wrk_y==0 )	wrk_y=1;
					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;

					wrk_x=unit[m].Position.X;
					wrk_y=unit[m].Position.Y;
					wrk_x+=cos(drctn*a_PI)*40;
					wrk_y+=sin(drctn*a_PI)*40;

					wrk_x2=unit[m].Position.X;
					wrk_y2=unit[m].Position.Y;
					drctn2=drctn;
					drctn2-=10;
					if(drctn2<0)
						drctn2=360+drctn2;
					wrk_x2+=cos(drctn2*a_PI)*20;
					wrk_y2+=sin(drctn2*a_PI)*20;
					cl=0x1f;
					draw_line4((int)(wrk_x-CameraPosition.X),(int)(CameraPosition.Y-wrk_y),(int)(wrk_x2-CameraPosition.X),(int)(CameraPosition.Y-wrk_y2),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));

					wrk_x2=unit[m].Position.X;
					wrk_y2=unit[m].Position.Y;
					drctn2=drctn;
					drctn2-=350;
					if(drctn2<0)
						drctn2=360+drctn2;
					wrk_x2+=cos(drctn2*a_PI)*20;
					wrk_y2+=sin(drctn2*a_PI)*20;
					cl=0x1f;
					draw_line4((int)(wrk_x-CameraPosition.X),(int)(CameraPosition.Y-wrk_y),(int)(wrk_x2-CameraPosition.X),(int)(CameraPosition.Y-wrk_y2),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
					}
				else
					{	// 視認不可
					wrk_x=unit[m].Position.X+10+20;
					wrk_y=unit[m].Position.Y+40;
					wrk_x2=unit[m].Position.X-10+20;
					wrk_y2=unit[m].Position.Y+20;
					cl=0x1f;
					draw_line4((int)(wrk_x-CameraPosition.X),(int)(CameraPosition.Y-wrk_y),(int)(wrk_x2-CameraPosition.X),(int)(CameraPosition.Y-wrk_y2),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
					wrk_x=unit[m].Position.X-10+20;
					wrk_y=unit[m].Position.Y+40;
					wrk_x2=unit[m].Position.X+10+20;
					wrk_y2=unit[m].Position.Y+20;
					cl=0x1f;
					draw_line4((int)(wrk_x-CameraPosition.X),(int)(CameraPosition.Y-wrk_y),(int)(wrk_x2-CameraPosition.X),(int)(CameraPosition.Y-wrk_y2),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
					}
				}


			// 決定された進路線ひき
			if( the_slct_unit==m && map_edit==0 )
				{

				// 緊急移動先までの線
				if( unit[m].em_flg[0]!=0 )
					{
					draw_line5((int)(unit[m].Position.X-CameraPosition.X),(int)(CameraPosition.Y-unit[m].Position.Y),(int)(unit[m].EmergencyDestination.X-CameraPosition.X),(int)(CameraPosition.Y-unit[m].EmergencyDestination.Y),CMBT_WIDTH-1,CMBT_HEIGHT-1,PALT_RED);
					}

				for(n=0; unit[m].pp_x[n]!=MAP_RIGHT+1; n++)
					{
					cl=0xffff;
					//cl=0xffff;
					//cl=(31<<7)|(0); // Ｇ 各値最大３１
					draw_line4((int)(unit[m].pp_x[n]-CameraPosition.X)-10,(int)(CameraPosition.Y-unit[m].pp_y[n])-10,(int)(unit[m].pp_x[n]-CameraPosition.X)+10,(int)(CameraPosition.Y-unit[m].pp_y[n])-10,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
					draw_line4((int)(unit[m].pp_x[n]-CameraPosition.X)+10,(int)(CameraPosition.Y-unit[m].pp_y[n])-10,(int)(unit[m].pp_x[n]-CameraPosition.X)+10,(int)(CameraPosition.Y-unit[m].pp_y[n])+10,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
					draw_line4((int)(unit[m].pp_x[n]-CameraPosition.X)+10,(int)(CameraPosition.Y-unit[m].pp_y[n])+10,(int)(unit[m].pp_x[n]-CameraPosition.X)-10,(int)(CameraPosition.Y-unit[m].pp_y[n])+10,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
					draw_line4((int)(unit[m].pp_x[n]-CameraPosition.X)-10,(int)(CameraPosition.Y-unit[m].pp_y[n])+10,(int)(unit[m].pp_x[n]-CameraPosition.X)-10,(int)(CameraPosition.Y-unit[m].pp_y[n])-10,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));


					

					// B=0xF800 R=0x7E0 G=0x1F
					//cl=0x07e0;
					if(n==/*unit[m].pp_now*/0)
						{
						//cl=0x001f;
						right=CMBT_WIDTH-1; bottom=CMBT_HEIGHT-1;
						if( unit[m].ctgry==UnitCategory.Plane && unit[m].PlaneState==UnitState.Parked )
							{draw_line4((int)(unit[unit[m].info[1]].Position.X-CameraPosition.X),(int)(CameraPosition.Y-unit[unit[m].info[1]].Position.Y),(int)(unit[m].pp_x[n]-CameraPosition.X),(int)(CameraPosition.Y-unit[m].pp_y[n]),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));}
						else
							{draw_line4(sprt[no1].x,sprt[no1].y,(int)(unit[m].pp_x[n]-CameraPosition.X),(int)(CameraPosition.Y-unit[m].pp_y[n]),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));}
						//draw_line2(os[no1].x,os[no1].y,(int)(unit[m].pp_x[n]-cmbt_x),(int)(cmbt_y-unit[m].pp_y[n]),cl);
						}
					else
						{
						//cl=0xf800;
						draw_line4((int)(unit[m].pp_x[n-1]-CameraPosition.X),(int)(CameraPosition.Y-unit[m].pp_y[n-1]),(int)(unit[m].pp_x[n]-CameraPosition.X),(int)(CameraPosition.Y-unit[m].pp_y[n]),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
						}
					}

				// 次の定点なるか までの線引き
				//cl=0xffff;
				//cl=0x07e0;
				right=CMBT_WIDTH-1; bottom=CMBT_HEIGHT-1;
				if( crsr_pt.x<=CMBT_WIDTH )
					{
					if( new_pp[1].cls!=0 )
						{
						if( unit[m].ctgry==UnitCategory.Plane && unit[m].PlaneState==UnitState.Parked )
							draw_line4((int)(unit[unit[m].info[1]].Position.X-CameraPosition.X),(int)(CameraPosition.Y-unit[unit[m].info[1]].Position.Y),(int)(crsr_pt.x),(int)(crsr_pt.y),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
						else
							draw_line4((int)(unit[m].Position.X-CameraPosition.X),(int)(CameraPosition.Y-unit[m].Position.Y),(int)(crsr_pt.x),(int)(crsr_pt.y),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
						}
					else
						draw_line4((int)(unit[m].pp_x[n-1]-CameraPosition.X),(int)(CameraPosition.Y-unit[m].pp_y[n-1]),(int)(crsr_pt.x),(int)(crsr_pt.y),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
					}


				// 定点設定
				if( lc_lf_btn==1 && unit[m].used==your_side && crsr_pt.x < CMBT_WIDTH &&
					// 発進チェック
					!(unit[m].ctgry==UnitCategory.Plane && unit[m].PlaneState==UnitState.Parked
					&& ( unit[unit[m].info[1]].info[4]!=0 || unit[unit[m].info[1]].info[8]!=0 || unit[m].ReloadTime>0 || unit[unit[m].info[1]].spry!=0 ))
					&& map_edit==0 )
					{
					new_pp[1].used=the_slct_unit;
					new_pp[1].Destination = new WorldPosition(crsr_pt.x+CameraPosition.X, CameraPosition.Y-crsr_pt.y);
					}


				if( 1!=0 )
					{
					if( lc_ri_btn==1 )
						{
						lc_ri_btn=0;
						the_slct_unit=0; slct_unit[1][m]=0;	cmbt_menu_kind=0; cmbt_menu_slctd=0; 
						cls_all_slct_unit_p2(1);

						bf_new_pp[1].cls=0;
						}
					}
				}
			}
		}


#if true


	if( lc_ri_btn==1 )
		{
		if( the_slct_unit==0 && old_the_slct_unit!=0 && unit[old_the_slct_unit].used!=0)
			{ 
			set_the_slct_unit( old_the_slct_unit );
			}
		}

	// アッパーエフェクト
	cont_upper_effect(&field_rect,&info_rect);



#endif
	}
}
