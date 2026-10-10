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

// Port of unit_info_cont.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{



public const int		map_close	= 1;









//============================================================================
// 補給ポイントとユニットの値段
//----------------------------------------------------------------------------
public int	spry_pt_per_unit()
	{

	if( your_side==Side.UnitedStates )
		{
		switch( spry_trgt )
			{
			case 0:		return(1000);	//戦艦
			case 1:		return(700);	//巡洋艦
			case 2:		return(450);	//駆逐艦
			case 3:		return(250);	//潜水艦

			case 4:		return(600);	//軽空母
			case 5:		return(850);	//正規空母

			case 6:		return(100);	//戦闘機
			case 7:		return(200);	//攻撃機
			case 8:		return(350);	//戦略爆撃機
			case 9:		return(220);	//陸上戦闘機

			case 10:	return(120);	//輸送船(歩兵基地)
			case 11:	return(550);	//輸送船(トーチカ群)
			case 12:	return(1200);	//輸送船(要塞)
			case 13:	return(1500);	//輸送船(航空基地)
			case 14:	return(2500);	//輸送船(軍港)

			case 15:		return(700);	//防空巡洋艦
			case 16:		return(450+30);	//対潜潜水艦

			case 17:		return(1200);	//エセックス型空母
			}
		}
	else
		{
		switch( spry_trgt )
			{
			case 0:		return(1000);	//戦艦
			case 1:		return(700);	//巡洋艦
			case 2:		return(450);	//駆逐艦
			case 3:		return(250);	//潜水艦

			case 4:		return(600);	//軽空母
			case 5:		return(800);	//正規空母

			case 6:		return(100);	//戦闘機
			case 7:		return(200);	//攻撃機
			case 8:		return(250);	//戦略爆撃機
			case 9:		return(175);	//陸上戦闘機

			case 10:	return(100);	//輸送船(歩兵基地)
			case 11:	return(500);	//輸送船(トーチカ群)
			case 12:	return(1100);	//輸送船(要塞)
			case 13:	return(1500);	//輸送船(航空基地)
			case 14:	return(2500);	//輸送船(軍港)

			case 15:		return(700+20);	//防空巡洋艦
			case 16:		return(450-30);	//対潜潜水艦

			case 17:		return(1300);		//大和級戦艦
			}
		}

	return 0;
	}










//============================================================================
//ユニットインフォーメイション
//----------------------------------------------------------------------------
public void	unit_info_cont()
	{
	RECT	src_rect,field_rect,dstn_rect,wrk_rect;
	int	m=default /* C4701 */,n,g,no1,i,wrk,wrk2,wrk3,f,s,start,end,ry; Array6<int> menu = default; Array6<int> menu2 = default;
    Array8<Array128<byte>> ach = default;
    Array8<int> len = default;
	HDC					hdc;
	//D3DXVECTOR2 chrPos;		// Direct3D is not used.
//	RECT	srcRect;


#if CONN_DBG
you_can_order=1;
rival_mode=mode;
#endif

	if( map_edit!=0 )
		rival_mode=mode;



	// ユニットインフォーメィション
	if( unit_info[4]==(int)Side.Japan )
		no1=UNIT_INFO_JPN;	//	ユニットインフォのｏｓナンバー
	else
		no1=UNIT_INFO_USA;	//	ユニットインフォのｏｓナンバー


	if( unit_info[0]!=0 )
		{
		// ユニットインフォの絵（戦艦とか空母とか）を表示します。
		sprt[no1].no=unit_info[2];
		if( unit_info[1]!=0 )
			{
			if((UnitKind)unit_info[0]==UnitKind.AirBase)
				{
				sprt[no1].no=9;
				}
			if((UnitKind)unit_info[0]==UnitKind.Carrier)
				{
				sprt[no1].no=10;
				}
			if((UnitKind)unit_info[0]==UnitKind.LightCarrier)
				{
				sprt[no1].no=11;
				}
			}


		// src_rect は ソースサーフェスのレクタングルです。
		src_rect.left = sprt[no1].base_x+(sprt[no1].wd * (sprt[no1].no % sprt[no1].os_of_x))+1;
		src_rect.top = sprt[no1].base_y+(sprt[no1].ht* (sprt[no1].no / sprt[no1].os_of_x))+1;
		src_rect.right = (src_rect.left + sprt[no1].wd)-3;
		src_rect.bottom = (src_rect.top + sprt[no1].ht)-2;


		// dstn_rect は ディスティネーションレクタングルです。
		dstn_rect.left=CMBT_WIDTH;
		dstn_rect.top=0;

		dstn_rect.right=dstn_rect.left+sprt[no1].wd-1+1;
		dstn_rect.bottom=dstn_rect.top+sprt[no1].ht-1+1;


		IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,DDBLTFAST_WAIT );
	

		// そのユニットのテキストを表示します。
		if (IDirectDrawSurface_GetDC(lpDDSBack, &hdc) == DD_OK ) 
			{
			// draw stats, like frame number and frame rate
			SetBkMode(hdc, TRANSPARENT);
			SelectObject(hdc, gameFont_1);

#if !LNGG_VER
			// 艦種
			switch((UnitKind)unit_info[0])
				{
				case UnitKind.Battleship:
					if( unit[unit_info[3]].used==Side.Japan && unit[unit_info[3]].type==1 )
						len[0] = wsprintf(ach[0], "大和級戦艦");
					else
						len[0] = wsprintf(ach[0], "戦艦",10);
					break;
				case UnitKind.Cruiser:
					if( unit[unit_info[3]].type==0 )
						len[0] = wsprintf(ach[0], "巡洋艦",10);
					else
					len[0] = wsprintf(ach[0], "防空巡洋艦",10);
					break;

				case UnitKind.Destroyer:
					if( unit[unit_info[3]].type==0 )
						len[0] = wsprintf(ach[0], "駆逐艦",10);
					else
						len[0] = wsprintf(ach[0], "対潜駆逐艦",10);
					break;
				case UnitKind.Submarine:
					len[0] = wsprintf(ach[0], "潜水艦",10);
					break;
				case UnitKind.Carrier:
					if( unit[unit_info[3]].used==Side.UnitedStates && unit[unit_info[3]].type==1 )
						len[0] = wsprintf(ach[0], "エセックス型空母",10);
					else
						len[0] = wsprintf(ach[0], "正規空母",10);
					break;
				case UnitKind.LightCarrier:
					len[0] = wsprintf(ach[0], "軽空母",10);
					break;
				case UnitKind.Transport:
					if(unit[unit_info[3]].arm[1]!=0)
						{
						if(unit[unit_info[3]].arm[0]==TR_GF1)
							len[0] = wsprintf(ach[0], "輸送船(歩兵基地)",10);
						else if(unit[unit_info[3]].arm[0]==TR_GF2)
							len[0] = wsprintf(ach[0], "輸送船(トーチカ群)",10);
						else if(unit[unit_info[3]].arm[0]==TR_GF3)
							len[0] = wsprintf(ach[0], "輸送船(要塞)",10);
						else if(unit[unit_info[3]].arm[0]==TR_AP)
							len[0] = wsprintf(ach[0], "輸送船(航空基地)",10);
						else if(unit[unit_info[3]].arm[0]==TR_SP)
							len[0] = wsprintf(ach[0], "輸送船(軍港)",10);
						}
					else
						{
						len[0] = wsprintf(ach[0], "輸送船",10);
						}
					break;
				case UnitKind.Fighter:
					if( unit[unit_info[3]].type==0 )
						len[0] = wsprintf(ach[0], "戦闘機",10);
					else
						len[0] = wsprintf(ach[0], "陸上戦闘機",10);
					break;
				case UnitKind.Attacker:
					len[0] = wsprintf(ach[0], "攻撃機",10);
					break;
				case UnitKind.Bomber:
					len[0] = wsprintf(ach[0], "戦略爆撃機",10);
					break;
				case UnitKind.NavalBase:
					len[0] = wsprintf(ach[0], "軍港",10);
					break;
				case UnitKind.AirBase:
					len[0] = wsprintf(ach[0], "航空基地",10);
					break;
				case UnitKind.City:
					len[0] = wsprintf(ach[0], "都市",10);
					break;
				case UnitKind.Mine:
					len[0] = wsprintf(ach[0], "鉱山",10);
					break;
				case UnitKind.InfantryBase:
					len[0] = wsprintf(ach[0], "歩兵基地",10);
					break;
				case UnitKind.Pillboxes:
					len[0] = wsprintf(ach[0], "トーチカ群",10);
					break;
				case UnitKind.Fortress:
					len[0] = wsprintf(ach[0], "要塞",10);
					break;
				default:
					len[0] = wsprintf(ach[0], "だっちゅーの",10);
					break;
				}


			// 損傷
			if( unit[unit_info[3]].used!=0 && ( unit[unit_info[3]].kind==UnitKind.AirBase||unit[unit_info[3]].kind==UnitKind.NavalBase||unit[unit_info[3]].kind==UnitKind.InfantryBase||unit[unit_info[3]].kind==UnitKind.Pillboxes||unit[unit_info[3]].kind==UnitKind.Fortress ) && unit[unit_info[3]].info[0]!=0 && unit[unit_info[3]].hp[0]==unit[unit_info[3]].hp[1]
				)
				{
				len[1] = wsprintf(ach[1], "工事:%d", unit[unit_info[3]].info[0] );
				}
			else
				{
				if( unit[unit_info[3]].hp[0] <= unit[unit_info[3]].hp[1]*0.2 )
					{
					len[1] = wsprintf(ach[1], "大破");
					}
				else if( unit[unit_info[3]].hp[0] <= unit[unit_info[3]].hp[1]*0.5 )
					{
					len[1] = wsprintf(ach[1], "中破");
					}
				else if( unit[unit_info[3]].hp[0] <= unit[unit_info[3]].hp[1]*0.7 )
					{
					len[1] = wsprintf(ach[1], "小破");
					}
				else if( unit[unit_info[3]].hp[0] <= unit[unit_info[3]].hp[1]-1 )
					{
					len[1] = wsprintf(ach[1], "軽損傷");
					}
				else
					{
					len[1] = wsprintf(ach[1], "損傷無");
					}
				}

			// 残弾薬
			len[2] = wsprintf(ach[2], "弾薬：%d",unit[unit_info[3]].arm[1]);


			// 残燃料
			if(unit[unit_info[3]].gas[0]==-1)
				len[3] = wsprintf(ach[3], "停泊艦船");
			else
				len[3] = wsprintf(ach[3], "燃料：%d", (int)(unit[unit_info[3]].gas[0]) );


#else
			// 艦種
			switch((UnitKind)unit_info[0])
				{
				case UnitKind.Battleship:
					if( unit[unit_info[3]].used==Side.Japan && unit[unit_info[3]].type==1 )
						len[0] = wsprintf(ach[0], "Type Yamato",10);
					else
						len[0] = wsprintf(ach[0], "Battleship",10);
					break;
				case UnitKind.Cruiser:
if( unit[unit_info[3]].type==0 )
					len[0] = wsprintf(ach[0], "Cruiser",10);
else
					len[0] = wsprintf(ach[0], "AntiAir Cruiser",10);

					break;
				case UnitKind.Destroyer:
					len[0] = wsprintf(ach[0], "Destroyer",10);
					break;
				case UnitKind.Submarine:
					len[0] = wsprintf(ach[0], "Submarine",10);
					break;
				case UnitKind.Carrier:
					if( unit[unit_info[3]].used==Side.UnitedStates && unit[unit_info[3]].type==1 )
						len[0] = wsprintf(ach[0], "Type Essex",10);
					else
						len[0] = wsprintf(ach[0], "Carrier",10);
					break;
				case UnitKind.LightCarrier:
					len[0] = wsprintf(ach[0], "Lt. Carrier",10);
					break;

				case UnitKind.Transport:
					if(unit[unit_info[3]].arm[1])
						{
						if(unit[unit_info[3]].arm[0]==TR_GF1)
							len[0] = wsprintf(ach[0], "Transport(Trenchies)",10);
						else if(unit[unit_info[3]].arm[0]==TR_GF2)
							len[0] = wsprintf(ach[0], "Transport(Pillboxies)",10);
						else if(unit[unit_info[3]].arm[0]==TR_GF3)
							len[0] = wsprintf(ach[0], "Transport(Fortress)",10);
						else if(unit[unit_info[3]].arm[0]==TR_AP)
							len[0] = wsprintf(ach[0], "Transport(Airfield)",10);
						else if(unit[unit_info[3]].arm[0]==TR_SP)
							len[0] = wsprintf(ach[0], "Transport(Port)",10);
						}
					else
						{
						len[0] = wsprintf(ach[0], "Transport",10);
						}

						
					break;
				case UnitKind.Fighter:
					if( unit[unit_info[3]].type==0 )
						len[0] = wsprintf(ach[0], "Car. Fighter",10);
					else
						len[0] = wsprintf(ach[0], "Grd. Fighter",10);
					break;


				case UnitKind.Attacker:
					len[0] = wsprintf(ach[0], "Car. Bomber",10);
					break;
				case UnitKind.Bomber:
					len[0] = wsprintf(ach[0], "Bomber",10);
					break;
				case UnitKind.NavalBase:
					len[0] = wsprintf(ach[0], "Military port",10);
					break;
				case UnitKind.AirBase:
					len[0] = wsprintf(ach[0], "Airfield",10);
					break;
				case UnitKind.City:
					len[0] = wsprintf(ach[0], "City",10);
					break;
				case UnitKind.Mine:
					len[0] = wsprintf(ach[0], "Mine",10);
					break;
				case UnitKind.InfantryBase:
					len[0] = wsprintf(ach[0], "Trenchies",10);
					break;
				case UnitKind.Pillboxes:
					len[0] = wsprintf(ach[0], "Pillboxies",10);
					break;
				case UnitKind.Fortress:
					len[0] = wsprintf(ach[0], "Fortress",10);
					break;
				default:
					len[0] = wsprintf(ach[0], "だっちゅーの",10);
					break;
				}





			// 損傷
			if( unit[unit_info[3]].hp[0] <= unit[unit_info[3]].hp[1]*0.2 )
				{
				len[1] = wsprintf(ach[1], "Heavy damaged"/*"大破"*/);
				}
			else if( unit[unit_info[3]].hp[0] <= unit[unit_info[3]].hp[1]*0.5 )
				{
				len[1] = wsprintf(ach[1], "Moderate damaged"/*"中破"*/);
				}
			else if( unit[unit_info[3]].hp[0] <= unit[unit_info[3]].hp[1]*0.7 )
				{
				len[1] = wsprintf(ach[1], "light damaged"/*"小破"*/);
				}
			else if( unit[unit_info[3]].hp[0] <= unit[unit_info[3]].hp[1]-1 )
				{
				len[1] = wsprintf(ach[1], "A bit of damaged");
				}
			else
				{
				len[1] = wsprintf(ach[1], "No damaged");
				}

			// 残弾薬
			len[2] = wsprintf(ach[2], "Ammo:%d",unit[unit_info[3]].arm[1]);


			// 残燃料
			if(unit[unit_info[3]].gas[0]==-1)
				len[3] = wsprintf(ach[3], "Anchored");
			else
				len[3] = wsprintf(ach[3], "Fuel:%d",(int)(unit[unit_info[3]].gas[0]));

				

#endif

			for( n=0; n<=3; n++)
				{
				SetTextColor(hdc, RGB(255, 255, 0));
				TextOut(hdc, dstn_rect.left+20+110, 10+(n*20), ach[n], len[n]);
				}

			if( map_edit!=0 )
				{
				len[0] = wsprintf(ach[0], "(%d/%d)", unit[unit_info[3]].hp[0], unit[unit_info[3]].hp[1]);
				SetTextColor(hdc, RGB(255, 255, 0));
				TextOut(hdc, dstn_rect.left+20+110+55, 10+(1*20), ach[0], len[0]);
				}



			IDirectDrawSurface_ReleaseDC(lpDDSBack, hdc);
			}
		}


				


#if true
	//	スプライトグループ（メニュー下地）
	src_rect.left = sprt[BTN_BASE].base_x;
	src_rect.top = sprt[BTN_BASE].base_y+(sprt[BTN_BASE].ht*(Side.UnitedStates==your_side ? 1 : 0));
	src_rect.right = src_rect.left+sprt[BTN_BASE].wd;
	src_rect.bottom = src_rect.top+sprt[BTN_BASE].ht;

	dstn_rect.left=sprt[BTN_BASE].x=CMBT_WIDTH;
	dstn_rect.top=sprt[BTN_BASE].y=sprt[UNIT_INFO_JPN].y+sprt[UNIT_INFO_JPN].ht+20;

	
	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,0) )
		{
		restoreAll();
		}




	no1=BTN_1;	
	if( (UnitKind)unit_info[0]==UnitKind.Carrier || (UnitKind)unit_info[0]==UnitKind.LightCarrier  || (UnitKind)unit_info[0]==UnitKind.AirBase )
		{
		// 航空母艦の場合切り替えボタンを表示
		i=0;
		src_rect.left = sprt[no1].base_x+(sprt[no1].wd*0)+1;
		src_rect.top = sprt[no1].base_y+(sprt[no1].ht*(i+1))+1;
		src_rect.right = src_rect.left+(sprt[no1].wd)-1;
		src_rect.bottom = src_rect.top+(sprt[no1].ht)-1;

		dstn_rect.left=CMBT_WIDTH;
		dstn_rect.top=sprt[UNIT_INFO_JPN].y+sprt[UNIT_INFO_JPN].ht;
		dstn_rect.right=dstn_rect.left+(sprt[no1].wd)-1;
		dstn_rect.bottom=dstn_rect.top+(sprt[no1].ht)-1;


		if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 )
			{

			if( lf_btn==3 )
				{

				if( unit_info[1]!=0 )
					{
					unit_info[1]=0;				// 空母なら１で格納庫 ０ で飛行甲板
					}
				else
					{
					unit_info[1]=1;				// 空母なら１で格納庫 ０ で飛行甲板

					slct_unit[1][the_slct_unit]=0;	cmbt_menu_kind=0; cmbt_menu_slctd=0;
					the_slct_unit=0; 
					cls_all_slct_unit();
					}
				}
			}
		if( lf_btn==2 && pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 )
			{
			src_rect.right+=180;	src_rect.left+=180;
			}

		
	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,0) )
		{
		restoreAll();
		}

		}




	if(cmbt_menu_kind!=0 && map_edit==0 )
		{

		//	スプライトグループ（メニュー）
		no1=BTN_1;	


		n=0;
		if( unit[the_slct_unit].ctgry==UnitCategory.Ship && !(unit[the_slct_unit].kind==UnitKind.InfantryBase||unit[the_slct_unit].kind==UnitKind.Pillboxes||unit[the_slct_unit].kind==UnitKind.Fortress||unit[the_slct_unit].kind==UnitKind.AirBase) )
			{	// 艦船のメニュー
			if( unit[the_slct_unit].stop!=0 && unit[the_slct_unit].spd==0 )
				{
				for( i=1; i<=max_unit; i++)
					{
					if( unit[i].used==your_side && unit[i].kind==UnitKind.NavalBase && unit[i].info[0]==0 )
						{
						// ptin dbg
						wrk_rect.top=(int)unit[i].y+40+240;
						wrk_rect.right=(int)unit[i].x+40+240;
						wrk_rect.bottom=(int)unit[i].y-40-240;
						wrk_rect.left=(int)unit[i].x-40-240;

						if( pt_in_rect3( ref wrk_rect, (int)unit[the_slct_unit].x, (int)unit[the_slct_unit].y )!=0 )
							{
							n=1;
							menu[0]=10;			// ボタンＣＧのインデックス
							menu2[0]=SPRY;

							if( unit[the_slct_unit].spry!=0 )
								cmbt_menu_slctd=SPRY;

							break;
							}
						}
					}
				}
			}
		else
			{
			if( unit[the_slct_unit].info[0]==FLYING )
				{	// 飛行中のメニュー
				n=0;
				for(g=1;g<=max_unit;g++)
					{
					if( unit[g].used!=0 && ( unit[g].ltl_ldr==the_slct_unit || g==the_slct_unit || slct_unit[1][g]!=0) )
						if( unit[g].info[0]!=FLYING )
							{
							n=1; break;
							}
					}
				if( n==0 )
					{
					n=2;						// メニュー項目の数
					menu[0]=2;	menu2[0]=MOVE;
					menu[1]=6;	menu2[1]=RETURN;
					}
				else
					{
					n=1;						// メニュー項目の数
					menu[0]=2;	menu2[0]=MOVE;
					}
				}
			else
				{	// 収容中のメニュー
				if( (unit[the_slct_unit].kind==UnitKind.Attacker || unit[the_slct_unit].kind==UnitKind.Bomber ) && unit[the_slct_unit].arm[0]!=TUN )
					{
					if( unit[the_slct_unit].kind==UnitKind.Attacker || unit[the_slct_unit].used==Side.Japan )
						{
						n=3;			// メニューの数
						menu[0]=7;	menu2[0]=RDY_TPD;
						menu[1]=8;	menu2[1]=RDY_BOM;
						menu[2]=9;	menu2[2]=NOTHING;
						}
					else
						{
						n=2;			// メニューの数
						menu[0]=8;	menu2[0]=RDY_BOM;
						menu[1]=9;	menu2[1]=NOTHING;
						}
					cmbt_menu_slctd=(short)unit[the_slct_unit].arm[0];



					if ( unit[the_slct_unit].arm[1]==0 )
						{
						cmbt_menu_slctd=NTG;
						}
					}
				}
			}




		for( i=0;i<n;i++ )
			{
			m=menu[i];
			src_rect.left = sprt[no1].base_x+(sprt[no1].wd*0)+1;
			src_rect.top = sprt[no1].base_y+(sprt[no1].ht*(m))+1;
			src_rect.right = src_rect.left+sprt[no1].wd-1;
			src_rect.bottom = src_rect.top+sprt[no1].ht-1;

			dstn_rect.left=sprt[BTN_BASE].x;
			dstn_rect.top=sprt[BTN_BASE].y+(sprt[no1].ht*(i));
			dstn_rect.right=dstn_rect.left+sprt[no1].wd-1;
			dstn_rect.bottom=dstn_rect.top+sprt[no1].ht-1;



			if( lf_btn==3 && menu2[i]!=cmbt_menu_slctd && pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 )
				{
				if( you_can_order!=0 )
					{
					bf_new_menu[1].menu=(short)menu2[i];
					bf_new_menu[1].the_slct_unit=the_slct_unit;


					if(your_side==Side.Japan)
						{
						// 日本海軍サイド
						for(s=1;s<=JPN_SHIP_END;s++)
							{
							// 水上ユニット
							bf_slct_unit[1][s-1]=slct_unit[1][s];
							}
						for(s=JPN_PLANE_START;s<=JPN_PLANE_END;s++)
							{
							// 航空ユニット
							bf_slct_unit[1][s-JPN_SHIP_END/*20*/-1]=slct_unit[1][s];
							}
						}
					else
						{
						// 合衆国海軍サイド
						for(s=USA_SHIP_START;s<=USA_SHIP_END;s++)
							{
							// 水上ユニット
							bf_slct_unit[1][s-JPN_SHIP_END/*20*/-1]=slct_unit[1][s];
							}
						for(s=USA_PLANE_START;s<=USA_PLANE_END;s++)
							{
							// 航空ユニット
							bf_slct_unit[1][s-(USA_PLANE_END/2)/*50*/-1]=slct_unit[1][s];
							}
						}
					you_can_order=0;
					you_ordered=1;
					SoundPlayEffect( 0, CLICK2 ,(double)(MAP_RIGHT+1), 0);
					}
				}
			if( lf_btn==2 && pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 && FrameCount%2<1)
				{

				src_rect.left=sprt[no1].base_x+(sprt[no1].wd*0)+1+180;
				src_rect.right=src_rect.left+sprt[no1].wd-1;
				}
			if( menu2[i]!=cmbt_menu_slctd && lf_btn==0 && pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 && FrameCount%6<4 )
				{
				src_rect.left=sprt[no1].base_x+(sprt[no1].wd*0)+1+180;
				src_rect.right=src_rect.left+sprt[no1].wd-1;
				}
			if( cmbt_menu_slctd==menu2[i] )
				{
				src_rect.left=sprt[no1].base_x+(sprt[no1].wd*0)+1+180;
				src_rect.right=src_rect.left+sprt[no1].wd-1;
				}
			
	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,0) )
		{
		restoreAll();
		}

			}
		}







	// ゲームデータ
	if ( map_edit==0 && IDirectDrawSurface_GetDC(lpDDSBack, &hdc) == DD_OK ) 
		{
		SetBkMode(hdc, TRANSPARENT);
		SelectObject(hdc, gameFont_1);

#if !LNGG_VER
		len[0] = wsprintf(ach[0], "スピード : %d",game_speed);
		len[1] = wsprintf(ach[1], "経過時間");
		len[2] = wsprintf(ach[2], "%d",rest_time);


#else
		len[0] = wsprintf(ach[0], "SPEED : %d",game_speed);
		len[1] = wsprintf(ach[1], "TIME");
		len[2] = wsprintf(ach[2], "%d",rest_time);
#endif

		for( n=0; n<=2; n++)
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			TextOut(hdc, dstn_rect.left+20+110, 100+(n*20), ach[n], len[n]);
			}
		IDirectDrawSurface_ReleaseDC(lpDDSBack, hdc);
		}






	// 補給
	ry=190;
	if( IDirectDrawSurface_GetDC(lpDDSBack, &hdc) == DD_OK ) 
		{
		SetBkMode(hdc, TRANSPARENT);
		SelectObject(hdc, gameFont_1);

	
		if( map_edit==0 )
			{

#if !LNGG_VER
		len[0] = wsprintf(ach[0], "補給割当:%d",spry_pt);
#else
		len[0] = wsprintf(ach[0], "Supry pts:%d",spry_pt);
#endif
		SetTextColor(hdc, RGB(255, 255, 255));
		TextOut(hdc, dstn_rect.left+120, ry, ach[0], len[0]);

#if !LNGG_VER
		len[0] = wsprintf(ach[0], "要求対象:");
#else
		len[0] = wsprintf(ach[0], "Object:");
#endif
		SetTextColor(hdc, RGB(255, 255, 255));
		TextOut(hdc, dstn_rect.left+120, ry+20*1, ach[0], len[0]);
		
		switch( spry_trgt )
			{
#if !LNGG_VER
			case 0:		len[0] = wsprintf(ach[0], "戦艦");		break;
			case 1:		len[0] = wsprintf(ach[0], "巡洋艦");		break;
			case 2:		len[0] = wsprintf(ach[0], "駆逐艦");		break;
			case 3:		len[0] = wsprintf(ach[0], "潜水艦");		break;
			case 4:		len[0] = wsprintf(ach[0], "軽空母");		break;
			case 5:		len[0] = wsprintf(ach[0], "正規空母");		break;
			case 6:		len[0] = wsprintf(ach[0], "戦闘機");		break;
			case 7:		len[0] = wsprintf(ach[0], "攻撃機");		break;
			case 8:		len[0] = wsprintf(ach[0], "戦略爆撃機");		break;
			case 9:		len[0] = wsprintf(ach[0], "陸上戦闘機");		break;
			case 10:	len[0] = wsprintf(ach[0], "輸送船(歩兵基地)");		break;
			case 11:	len[0] = wsprintf(ach[0], "輸送船(トーチカ群)");		break;
			case 12:	len[0] = wsprintf(ach[0], "輸送船(要塞)");		break;
			case 13:	len[0] = wsprintf(ach[0], "輸送船(航空基地)");		break;
			case 14:	len[0] = wsprintf(ach[0], "輸送船(軍港)");		break;

			case 15:		len[0] = wsprintf(ach[0], "防空巡洋艦");		break;
			case 16:		len[0] = wsprintf(ach[0], "対潜駆逐艦");		break;

			case 17:
				if( your_side==Side.Japan )
					len[0] = wsprintf(ach[0], "大和級戦艦");
				else
					len[0] = wsprintf(ach[0], "エセックス型空母");

				break;


#else
			case 0:		len[0] = wsprintf(ach[0], "Battleship");		break;
			case 1:		len[0] = wsprintf(ach[0], "Cruiser");		break;
			case 2:		len[0] = wsprintf(ach[0], "Destroyer");		break;
			case 3:		len[0] = wsprintf(ach[0], "Submarine");		break;
			case 4:		len[0] = wsprintf(ach[0], "Light Carrier");		break;
			case 5:		len[0] = wsprintf(ach[0], "Carrier");		break;
			case 6:		len[0] = wsprintf(ach[0], "Car. Fighter");		break;
			case 7:		len[0] = wsprintf(ach[0], "Car. Bomber");		break;
			case 8:		len[0] = wsprintf(ach[0], "Bomber");		break;
			case 9:		len[0] = wsprintf(ach[0], "Grn. Fighter");		break;
			case 10:	len[0] = wsprintf(ach[0], "Transport(Trenchies)");		break;
			case 11:	len[0] = wsprintf(ach[0], "Transport(Pillboxies)");		break;
			case 12:	len[0] = wsprintf(ach[0], "Transport(Fortress)");		break;
			case 13:	len[0] = wsprintf(ach[0], "Transport(Airfield)");		break;
			case 14:	len[0] = wsprintf(ach[0], "Transport(Port)");		break;

			case 15:	len[0] = wsprintf(ach[0], "AntiAir Cruiser");		break;
			case 16:	len[0] = wsprintf(ach[0], "AntiSub Cruiser");		break;

			case 17:
				if( your_side==Side.Japan )
					len[0] = wsprintf(ach[0], "Class Yamato");
				else
					len[0] = wsprintf(ach[0], "Class Essex");

				break;
#endif
			}
		SetTextColor(hdc, RGB(255, 255, 255));
		TextOut(hdc, dstn_rect.left+120, ry+20*2, ach[0], len[0]);


		if( spry_no_cont==0 )
			{
			// 値段の表示
			len[0] = wsprintf(ach[0], "%d:" ,spry_pt_per_unit());
			TextOut(hdc, sprt[BTN_BASE].x+120, ry+20*3+5+10, ach[0], len[0]);


			//
#if !LNGG_VER
			len[0] = wsprintf(ach[0], "<<<-前の機種");
#else
			len[0] = wsprintf(ach[0], "<<<- Previous");
#endif

			dstn_rect.left=sprt[BTN_BASE].x+120;
			dstn_rect.top=ry+20*5+5;
			dstn_rect.right=dstn_rect.left+(len[0]*12);
			dstn_rect.bottom=dstn_rect.top+18;
			if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 && spry_no_cont==0 )
				{
				SetTextColor(hdc, RGB(255, 0, 0));
				if( lf_btn==3 )
					{
					if(spry_trgt==0)
						spry_trgt=17;//14;
					else
						spry_trgt--;
					}
				}
			else
				{
				SetTextColor(hdc, RGB(255, 255, 255));
				}

			TextOut(hdc, sprt[BTN_BASE].x+120, ry+20*5+5, ach[0], len[0]);


			//
#if !LNGG_VER
			len[0] = wsprintf(ach[0], "次の機種->>>");
#else
			len[0] = wsprintf(ach[0], "Next ->>>");
#endif

			dstn_rect.left=sprt[BTN_BASE].x+120;
			dstn_rect.top=ry+20*6+5;
			dstn_rect.right=dstn_rect.left+(len[0]*12);
			dstn_rect.bottom=dstn_rect.top+18;
			if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 )
				{
				SetTextColor(hdc, RGB(255, 0, 0));
				if( lf_btn==3 )
					{
					if(spry_trgt==17/*14*/)
						spry_trgt=0;
					else
						spry_trgt++;
					}
				}
			else
				{
				SetTextColor(hdc, RGB(255, 255, 255));
				}

			TextOut(hdc, sprt[BTN_BASE].x+120, ry+20*6+5, ach[0], len[0]);




			// 要求する

			if(your_side==Side.Japan)
				{
				// 日本サイドのユニット
				if( spry_trgt<=5 || spry_trgt>=10 /*ctgry==SHIP*/ )
					{
					start=JPN_SHIP_START;
					end=JPN_SHIP_END;
					g=1;						// 艦船
					}
				else
					{
					start=JPN_PLANE_START;
					end=JPN_PLANE_END;
					g=2;						// 航空機
					}
				}
			else
				{
				// 合衆国サイドのユニット
				if( spry_trgt<=5 || spry_trgt>=10 /*ctgry==SHIP*/ )
					{
					start=USA_SHIP_START;
					end=USA_SHIP_END;
					g=1;						// 艦船
					}
				else
					{
					start=USA_PLANE_START;
					end=USA_PLANE_END;
					g=2;						// 航空機
					}
				}

			n=0;
			for( m=start; m<=end; m++)
				{
				if(unit[m].used==0)
					{
					n++;
					}								
				}

			


/***
				case 0:
					len[0] = wsprintf(ach[0], "全種増援可(%d)",arrival_cont);
					break;
				case 1:
					len[0] = wsprintf(ach[0], "全種増援不可(%d)",arrival_cont);
					break;
				case 2:
					len[0] = wsprintf(ach[0], "輸送船以外可(%d)",arrival_cont);
					break;
				case 3:
					len[0] = wsprintf(ach[0], "輸送船のみ可(%d)",arrival_cont);
					break;
				case 4:
					len[0] = wsprintf(ach[0], "戦闘艦船のみ可(%d)",arrival_cont);
					break;
				case 5:
					len[0] = wsprintf(ach[0], "航空機のみ可(%d)",arrival_cont);
					break;
				case 6:
					len[0] = wsprintf(ach[0], "輸送船(軍港)以外可(%d)",arrival_cont);
					break;
***/

			if(	(arrival_cont!=1) && 
					( 
						(arrival_cont==0) || 
						(arrival_cont==2 && ( spry_trgt<=9 || spry_trgt>=15 ) ) || 
						(arrival_cont==3 && spry_trgt>=10 && spry_trgt<=14 ) || 
						(arrival_cont==4 && (spry_trgt<=3 || spry_trgt>=15 ) && !(spry_trgt==17 && your_side==Side.UnitedStates)  ) || 
						(arrival_cont==5 && spry_trgt>=6 && spry_trgt<=9 ) || 
						(arrival_cont==6 && spry_trgt!=14 ) 
					) 
					)
				{

				if( (g==1 && n!=0 ) || (g==2 && n>=3) )
					{
					if( spry_pt>=spry_pt_per_unit() )
						{
#if !LNGG_VER
						len[0] = wsprintf(ach[0], "　要求する");
#else
						len[0] = wsprintf(ach[0], "　Request");
#endif
						dstn_rect.left=sprt[BTN_BASE].x+120;
						dstn_rect.top=ry+20*7+5;
						dstn_rect.right=dstn_rect.left+(len[0]*12);
						dstn_rect.bottom=dstn_rect.top+18;
						if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 )
							{
							SetTextColor(hdc, RGB(255, 0, 0));
							if( lf_btn==3 && game_end==GameResult.None )
								{
								spry_pt=(short)(spry_pt - spry_pt_per_unit());
								if(spry_trgt<=5 || spry_trgt>=10)
									spry_no_cont=150;				// 艦船
								else
									spry_no_cont=30;				// 航空機
					
if(CONN_DBG!=0)
spry_no_cont=10;
								SoundPlayEffect( 0, CLICK2 ,(double)(MAP_RIGHT+1), 0);
								}
							}
						else
							{
							SetTextColor(hdc, RGB(255, 255, 255));
							}
						TextOut(hdc, sprt[BTN_BASE].x+120, ry+20*7+5, ach[0], len[0]);
						}
					else
						{
						SetTextColor(hdc, RGB(255, 255, 255));
#if !LNGG_VER
						len[0] = wsprintf(ach[0], "割当点数不足");
#else
						len[0] = wsprintf(ach[0], "Shortage of pts");
#endif
						TextOut(hdc, sprt[BTN_BASE].x+120, ry+20*7+5, ach[0], len[0]);
						}
					}
				else
					{
					SetTextColor(hdc, RGB(255, 255, 255));
#if !LNGG_VER
					len[0] = wsprintf(ach[0], "ユニットリミット");
#else
					len[0] = wsprintf(ach[0], "Units Max");
#endif
					TextOut(hdc, sprt[BTN_BASE].x+120, ry+20*7+5, ach[0], len[0]);
					}


				}
			else
				{
				SetTextColor(hdc, RGB(255, 255, 255));
#if !LNGG_VER
				len[0] = wsprintf(ach[0], "　要求制限");
#else
				len[0] = wsprintf(ach[0], "Restricted Unit");
#endif
				TextOut(hdc, sprt[BTN_BASE].x+120, ry+20*7+5, ach[0], len[0]);
				}
			}
		else
			{
			if((FrameCount%2)!=0)
				{
#if !LNGG_VER
				len[0] = wsprintf(ach[0], "要求中");
				TextOut(hdc, sprt[BTN_BASE].x+120+30, ry+20*6+5, ach[0], len[0]);
#else
				len[0] = wsprintf(ach[0], "Wait for coming");
				TextOut(hdc, sprt[BTN_BASE].x+120, ry+20*6+5, ach[0], len[0]);
#endif
				}

		
			if( game_end==GameResult.None )
				{
				spry_no_cont--;

				if( spry_no_cont==0 )
					{
					if(you_can_order==1)
						{
						bf_arrived_unit[1]=(short)(spry_trgt+1);
						you_can_order=0;
						you_ordered=1;
						}
					else
						{
						spry_no_cont=1;
						}
					}
				}
			}
			}
		ry+=30;



		if( you_are_host!=0 )
			{
#if !LNGG_VER
			len[0] = wsprintf(ach[0], "シナリオ選択へ");
#else
			len[0] = wsprintf(ach[0], "to Mission Menu");
#endif
			dstn_rect.left=sprt[BTN_BASE].x+120;
			dstn_rect.top=ry+20*9+5;
			dstn_rect.right=dstn_rect.left+(len[0]*12);
			dstn_rect.bottom=dstn_rect.top+18;
			if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 && rival_mode==GameMode.Battle )
				{
				SetTextColor(hdc, RGB(255, 0, 0));
				if( lf_btn==3 )
					{
					if( map_edit!=0)
						{


						mode=GameMode.ConfigSetting;

/*
						if( IDOK==MessageBox( hwndApp,"シナリオ作成を中断しますか？","NSPW on the Net",MB_OKCANCEL|MB_DEFBUTTON2) )
							{
//							bf_game_system_menu[1]=GO_GAME_SETTING;
							go_cnct_game_setting();
							SoundPlayEffect( NULL, CLICK2 ,(double)(MAP_RIGHT+1), 0);
							}
*/
						}
					else if( you_can_order==1 )
						{
	dlg_answer=MessageType.GoToGameSetting;
						g_hDlg = CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_OK_CANCEL), hwndApp, (DLGPROC)IDD_OK_CANCEL_Proc );
#if false
						my_dlg_wait();

						if( dlg_answer )
							{
							bf_game_system_menu[1]=MessageType.GoToGameSetting;
							you_can_order=0;
							you_ordered=1;
							SoundPlayEffect( NULL, CLICK2 ,(double)(MAP_RIGHT+1), 0);
							}
#endif
						}
					}
				}
			else
				{
				SetTextColor(hdc, RGB(255, 255, 255));
				}
			TextOut(hdc, sprt[BTN_BASE].x+120, ry+20*9+5, ach[0], len[0]);


			if( map_edit==0 )
				{		
#if !LNGG_VER
				len[0] = wsprintf(ach[0], "リジューム");
#else
				len[0] = wsprintf(ach[0], "Resume");
#endif
				dstn_rect.left=sprt[BTN_BASE].x+120+20;
				dstn_rect.top=ry+20*10+5;
				dstn_rect.right=dstn_rect.left+(len[0]*12);
				dstn_rect.bottom=dstn_rect.top+18;
				if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 && rival_mode==GameMode.Battle )
					{
					SetTextColor(hdc, RGB(255, 0, 0));
					if( lf_btn==3 )
						{
						if( you_can_order==1 )
							{
	dlg_answer=MessageType.ResumeAndGoToGameSetting;
							g_hDlg = CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_OK_CANCEL), hwndApp, (DLGPROC)IDD_OK_CANCEL_Proc );
#if false
							my_dlg_wait();

							if( dlg_answer )
								{
								bf_game_system_menu[1]=MessageType.ResumeAndGoToGameSetting;
								you_can_order=0;
								you_ordered=1;
								SoundPlayEffect( NULL, CLICK2 ,(double)(MAP_RIGHT+1), 0);
								}
#endif
							}
						}
					}
				else
					{
					SetTextColor(hdc, RGB(255, 255, 255));
					}
				TextOut(hdc, sprt[BTN_BASE].x+120+20, ry+20*10+5, ach[0], len[0]);
				}
			}

		IDirectDrawSurface_ReleaseDC(lpDDSBack, hdc);
		}


	ry-=30;

	// 補給
	if( ( spry_no_cont==0 || (FrameCount%2)!=0 ) && map_edit==0 )
		{
		if(your_side==Side.Japan)
			no1=UNIT_JPN;		//Off Screen Number		日本海軍の表示
		else
			no1=UNIT_USA;		//Off Screen Number		日本海軍の表示


		n=1;
		switch( spry_trgt )
			{
			case 0:	case 1:	 case 2: case 3:
				m=spry_trgt;
				break;
			case 15:			// 防空巡洋艦
				m=1;
				n=7;
				break;
			case 16:			// 対潜駆逐艦
				m=2;
				n=7;
				break;

			case 17:
				if( your_side==Side.Japan )
					m=0;		// 大和
				else
					m=6;		// エセックス
				n=7;
				break;


			case 4: case 5: case 6:
				m=spry_trgt;
				m++;
				break;
			case 7:		m=9;	break;
			case 8:		m=12;	break;
			case 9:		m=14;	break;
			case 10: case 11: case 12: case 13: case 14:
				m=13;
				break;
			}

		// src_rect は ソースサーフェスのレクタングルです。
		src_rect.left = sprt[no1].base_x+(sprt[no1].wd * ( n )) +1;		// 方向



		src_rect.top = sprt[no1].base_y+(sprt[no1].ht* (m)) +1;		// 機種

		src_rect.right = (src_rect.left + sprt[no1].wd)-2;
		src_rect.bottom = (src_rect.top + sprt[no1].ht)-2;

		// dstn_rect は ディスティネーションレクタングルです。
		dstn_rect.left=1024-120+10;
		dstn_rect.top=ry+20*2+5;

		if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,DDBLTFAST_SRCCOLORKEY) )
			{
			restoreAll();
			}
		}
#endif
	}









//============================================================================
// ミニマップ
//----------------------------------------------------------------------------
public void	draw_map()
	{
	RECT	src_rect,field_rect,dstn_rect;
	int		m,n,base_x,base_y;
	byte	my_cl, en_cl;


	// マップの下地を描画
	sprt[MAP_BASE].x=CMBT_WIDTH;
	sprt[MAP_BASE].y=CMBT_HEIGHT-sprt[MAP_BASE].ht;

	src_rect.left = 	sprt[MAP_BASE].base_x;
	src_rect.top = sprt[MAP_BASE].base_y;
	src_rect.right = sprt[MAP_BASE].base_x+sprt[MAP_BASE].wd;
	src_rect.bottom = sprt[MAP_BASE].base_y+sprt[MAP_BASE].ht;

	// dstn_rect は ディスティネーションレクタングルです。
	dstn_rect.left=sprt[MAP_BASE].x;
	dstn_rect.top=sprt[MAP_BASE].y;
	//dstn_rect.right=sprt[TTL_BACK].x+sprt[TTL_BACK].wd/2;
	//dstn_rect.bottom=sprt[TTL_BACK].y+sprt[TTL_BACK].ht/2;

	
	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,0) )
		{
		restoreAll();
		}


	base_x=sprt[MAP_BASE].x+8;
	base_y=sprt[MAP_BASE].y+8;










	// コンバット画面位置の描画
	my_cl=0;
	if( cmbt_x >= 0 )
		dstn_rect.left=base_x+((abs(MAP_LEFT)+(int)(cmbt_x))/80)*map_close;
	else
		dstn_rect.left=base_x+((abs(MAP_LEFT)+(int)(cmbt_x))/80)*map_close;
	if( cmbt_y >= 0 )
		dstn_rect.top=base_y+((abs(MAP_TOP)-(int)(cmbt_y))/80)*map_close;
	else
		dstn_rect.top=base_y+((abs(MAP_TOP)-(int)(cmbt_y))/80)*map_close;

	dstn_rect.right=dstn_rect.left+10;
	dstn_rect.bottom=dstn_rect.top+10;

	draw_line5(dstn_rect.left,dstn_rect.top,dstn_rect.right,dstn_rect.top,SCRN_WIDTH-1,SCRN_HEIGHT-1,my_cl);
	draw_line5(dstn_rect.right,dstn_rect.top,dstn_rect.right,dstn_rect.bottom,SCRN_WIDTH-1,SCRN_HEIGHT-1,my_cl);
	draw_line5(dstn_rect.right,dstn_rect.bottom,dstn_rect.left,dstn_rect.bottom,SCRN_WIDTH-1,SCRN_HEIGHT-1,my_cl);
	draw_line5(dstn_rect.left,dstn_rect.bottom,dstn_rect.left,dstn_rect.top,SCRN_WIDTH-1,SCRN_HEIGHT-1,my_cl);




	// マップ上のカーソルクリックでの位置指定
	my_cl=255;
	if ( crsr_pt.x>=base_x && crsr_pt.x<=base_x+240 && crsr_pt.y>=base_y && crsr_pt.y<=base_y+180 )
		{
		dstn_rect.left=crsr_pt.x-5;
		dstn_rect.top=crsr_pt.y-5;
		dstn_rect.right=dstn_rect.left+10;
		dstn_rect.bottom=dstn_rect.top+10;

		draw_line5(dstn_rect.left,dstn_rect.top,dstn_rect.right,dstn_rect.top,SCRN_WIDTH-1,SCRN_HEIGHT-1,my_cl);
		draw_line5(dstn_rect.right,dstn_rect.top,dstn_rect.right,dstn_rect.bottom,SCRN_WIDTH-1,SCRN_HEIGHT-1,my_cl);
		draw_line5(dstn_rect.right,dstn_rect.bottom,dstn_rect.left,dstn_rect.bottom,SCRN_WIDTH-1,SCRN_HEIGHT-1,my_cl);
		draw_line5(dstn_rect.left,dstn_rect.bottom,dstn_rect.left,dstn_rect.top,SCRN_WIDTH-1,SCRN_HEIGHT-1,my_cl);

		if(lf_btn==2)
			{
			cmbt_x=MAP_LEFT+(((crsr_pt.x-5-base_x)/map_close)*80);
			cmbt_y=MAP_TOP-(((crsr_pt.y-5-base_y)/map_close)*80);
			if(cmbt_y>MAP_TOP)
				cmbt_y=MAP_TOP;
			if(cmbt_x>(MAP_RIGHT-CMBT_WIDTH) )		
				cmbt_x=MAP_RIGHT-CMBT_WIDTH;
			if(cmbt_y<(MAP_BOTTOM+CMBT_HEIGHT) )
				cmbt_y=MAP_BOTTOM+CMBT_HEIGHT;
			if(cmbt_x<MAP_LEFT)		
				cmbt_x=MAP_LEFT;
			}
		}






	// ユニットの描画

	if( your_side==Side.Japan )
		{
//		my_cl=5457;	en_cl=5416;

		my_cl=(byte)(sprt[MAP_BASE].base_y+57);	en_cl=(byte)(sprt[MAP_BASE].base_y+16);


		}
	else
		{
//		en_cl=5457;	my_cl=5416;
		en_cl=(byte)(sprt[MAP_BASE].base_y+57);	my_cl=(byte)(sprt[MAP_BASE].base_y+16);
		}

	for(m=1; m<=max_unit; m++)
		{

		if( unit[m].used==your_side  &&   !( unit[m].y>MAP_TOP || unit[m].y<MAP_BOTTOM || unit[m].x<MAP_LEFT || unit[m].x>MAP_RIGHT )    && !(unit[m].info[0]==PARKING) && !(unit[m].ctgry==UnitCategory.Plane && (FrameCount%4)==0))
			{
			// マイユニット
			if( unit[m].x >= 0 )
				dstn_rect.left=base_x+((abs(MAP_LEFT)+(int)(unit[m].x-40))/80)*map_close;
			else
				dstn_rect.left=base_x+((abs(MAP_LEFT)+(int)(unit[m].x-40))/80)*map_close;
			if( unit[m].y >= 0 )
				dstn_rect.top=base_y+((abs(MAP_TOP)-(int)(unit[m].y+40))/80)*map_close;
			else
				dstn_rect.top=base_y+((abs(MAP_TOP)-(int)(unit[m].y+40))/80)*map_close;

			dstn_rect.right=dstn_rect.left+2;
			dstn_rect.bottom=dstn_rect.top+2;


			src_rect.left = 267;	
			if( your_side==Side.Japan )
				src_rect.top = sprt[MAP_BASE].base_y+16;
			else
				src_rect.top = sprt[MAP_BASE].base_y+57;
			src_rect.right = src_rect.left+3;
			src_rect.bottom = src_rect.top+3;

			
	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,0) )
		{
		restoreAll();
		}

			}


		if( unit[m].used!=0 && unit[m].used!=your_side && !( unit[m].y>MAP_TOP || unit[m].y<MAP_BOTTOM || unit[m].x<MAP_LEFT || unit[m].x>MAP_RIGHT ) && !(unit[m].info[0]==PARKING) && unit[m].found!=0 && !(unit[m].ctgry==UnitCategory.Plane && (FrameCount%4)==0) )
			{
			// エネユニット
			if( unit[m].kind==UnitKind.Submarine && unit[m].info[6]!=0 )
				{
				// 潜航中のおおよそ潜水艦
				if( unit[m].info[7] >= 0 )
					dstn_rect.left=base_x+((abs(MAP_LEFT)+(int)(unit[m].info[7]-40))/80)*map_close;
				else
					dstn_rect.left=base_x+((abs(MAP_LEFT)+(int)(unit[m].info[7]-40))/80)*map_close;
				if( unit[m].info[8] >= 0 )
					dstn_rect.top=base_y+((abs(MAP_TOP)-(int)(unit[m].info[8]+40))/80)*map_close;
				else
					dstn_rect.top=base_y+((abs(MAP_TOP)-(int)(unit[m].info[8]+40))/80)*map_close;
				}
			else
				{
				// 艦船基地航空機
				if( unit[m].x >= 0 )
					dstn_rect.left=base_x+((abs(MAP_LEFT)+(int)(unit[m].x-40))/80)*map_close;
				else
					dstn_rect.left=base_x+((abs(MAP_LEFT)+(int)(unit[m].x-40))/80)*map_close;
				if( unit[m].y >= 0 )
					dstn_rect.top=base_y+((abs(MAP_TOP)-(int)(unit[m].y+40))/80)*map_close;
				else
					dstn_rect.top=base_y+((abs(MAP_TOP)-(int)(unit[m].y+40))/80)*map_close;
				}
			dstn_rect.right=dstn_rect.left+2;
			dstn_rect.bottom=dstn_rect.top+2;

			src_rect.left = 267;
			if( your_side==Side.Japan )
				src_rect.top = sprt[MAP_BASE].base_y+57;
			else
				src_rect.top = sprt[MAP_BASE].base_y+16;
			src_rect.right = src_rect.left+3;
			src_rect.bottom = src_rect.top+3;

			
			if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,0) )
				{
				restoreAll();
				}
			}
		}
	}





//============================================================================
// ミニマップ
// サーフェスリストアの時に使うようだ
//----------------------------------------------------------------------------
public void	make_map()
	{
	RECT	src_rect,field_rect,dstn_rect;
	int m,n;


	// マップデータから陸地をマップに描画します
	dstn_rect.left=sprt[MAP_BASE].base_x;
	dstn_rect.top=sprt[MAP_BASE].base_y;

	src_rect.left = sprt[MAP_BASE].base_x+306;
	src_rect.top = sprt[MAP_BASE].base_y;
	src_rect.right = src_rect.left+255;
	src_rect.bottom = src_rect.top+199;

	
	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDS_OS, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,0) )
		{
		restoreAll();
		}


	for(m=0; m<=179; m++) // 縦の個数	マップの枠 縦１８０ドット
		for( n=0; n<=239; n++) // 横の個数		マップの枠 横２４０ドット
			{
			if( cmbt_map[m][n]!=0 )
				{
				// 陸地有り
				dstn_rect.left=sprt[MAP_BASE].base_x+8+n-0;
				dstn_rect.top=sprt[MAP_BASE].base_y+8+m-0;

//				src_rect.left = 271;
//				src_rect.top = 5506;

				src_rect.left = sprt[MAP_BASE].base_x+270;
				src_rect.top = sprt[MAP_BASE].base_y+110;

				src_rect.right = src_rect.left+2;
				src_rect.bottom = src_rect.top+2;
				
				if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDS_OS, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,0) )
					{
					restoreAll();
					}

				}
			}
	}







//============================================================================
//ユニットインフォーメイション
//----------------------------------------------------------------------------
public void	cnct_unit_info_cont_now()
	{
	short	menu2,e,tmp_slct_unit,g,i;


	for(e=0;e<=1;e++)
		{
		menu2=bf_new_menu[e].menu;
		tmp_slct_unit=bf_new_menu[e].the_slct_unit;



		// 指揮、随伴ユニットに下命
		if(menu2!=0)
			{
			cmbt_menu_slctd=menu2;
			if( menu2==SPRY )
				{
				unit[tmp_slct_unit].spry=1;
				unit[tmp_slct_unit].arm[2]=0;

				// ちょっと一応
				if( unit[tmp_slct_unit].kind==UnitKind.Carrier || unit[tmp_slct_unit].kind==UnitKind.LightCarrier )
					{
					unit[tmp_slct_unit].info[7]=0;	// 着艦、0許可、1不許可
					unit[tmp_slct_unit].info[8]=0;	// その空母の次機発進許可	0許可、1不許可
					}
				if( unit[tmp_slct_unit].kind==UnitKind.Submarine )
					unit[tmp_slct_unit].info[6]=0;		// 強制浮上
				}
			else
				{
				for(g=1;g<=max_unit;g++)
					{
					if( unit[g].used!=0 && ( unit[g].ltl_ldr==tmp_slct_unit || g==tmp_slct_unit || slct_unit[e][g]!=0) )
						{
						// 決定後の書く個別の処理
						switch( unit[g].kind )
							{
							case UnitKind.Fighter: case UnitKind.Attacker: case UnitKind.Bomber:	
								if(  menu2==RDY_TPD || menu2==RDY_BOM || menu2==NOTHING  )
									{	
									if(unit[g].kind==UnitKind.Attacker || (unit[g].kind==UnitKind.Bomber && ( (menu2==RDY_TPD && unit[g].used==Side.Japan ) || menu2==RDY_BOM || menu2==NOTHING) ))
										{		// 収容中の攻撃機だったばあい
										unit[g].arm[0]=menu2;


										if(menu2==RDY_TPD)
											unit[g].arm[1]=1;		// 魚雷の場合は常に１、弾数。
										else
											unit[g].arm[1]=unit[g].arm[4];

										if(menu2==NOTHING)
											unit[g].arm[3]=1;
										else
											unit[g].arm[3]=RDY_SPAN;
										}
									}
								else
									{
//									unit[g].info[5]=menu2;
									if( (unit[g].kind==UnitKind.Fighter ) && menu2==RETURN  && unit[g].info[0]==FLYING )
										{
										unit[g].info[5]=menu2;
										}
									if( (unit[g].kind==UnitKind.Attacker || unit[g].kind==UnitKind.Bomber ) && menu2==RETURN )
										{
										unit[g].arm[1]=0;		// 帰投選択時に攻撃機なら武装投棄
										unit[g].arm[2]=0;		// 帰投選択時に攻撃機攻撃目標放棄
										unit[g].info[5]=menu2;
										}
									if( unit[g].ctgry==UnitCategory.Plane && menu2==MOVE && unit[g].info[0]==FLYING  )
										{
										unit[g].info[3]=0;		// 着艦準備をクリア
										unit[g].info[5]=menu2;
										}
									}
								break;
							case UnitKind.Battleship: case UnitKind.Cruiser: case UnitKind.Destroyer: case UnitKind.Submarine: case UnitKind.Carrier: case UnitKind.LightCarrier:
								break;
							}
						}
					}
				}
			}
		}
	}
}
