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

// Port of chara_cont.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{






//============================================================================
//
//----------------------------------------------------------------------------
public void	chara_cont()
	{
	int	m,n,i,f;
	RECT	wrk_r,wrk_rect;	
//	double	add_x,add_y;
	double	pp_drctn,wrk_x,wrk_y,drctn,dstc,wrk_x2,wrk_y2,wrk_3=default /* C4701 */;
	int		cv_1;
	int		cm_scrn_x,cm_scrn_y,dmg_act;
	int	land,flg;
	short	s,e,bf_the_slct_unit;
	Array200<short> bf_2_slct_unit = default;
//	HRESULT hr;
	short	plane,ship;
	


	_DP_NEW_PP			dp_new_pp;
	_DP_NEW_PP_SHIP		dp_new_pp_ship;
	_DP_NEW_PP_PLANE	dp_new_pp_plane;

	_DP_NEW_SLCT		dp_new_slct;
	_DP_NEW_SLCT_SHIP	dp_new_slct_ship;
	_DP_NEW_SLCT_PLANE	dp_new_slct_plane;
	_DP_NEW_SLCT_LAND	dp_new_slct_land;

	_DP_NEW_MENU		dp_new_menu;
	_DP_FLAG			dp_flag;

	_DP_DATA_1	dp_data_1;







	if( mode!=CMBT )
		return;	


#if true
	if( map_edit==0 )
		{
		/* 入力フェーズ */
		// 通信対戦時
		if((cc_count%(cnct_loop))==cnct_loop_pt1 )
			{
			if(  CONN_DBG==0 && go_next_1==0 )
				{
				return;
				}

			if( game_end==0)
				{
				rest_time++;

				if( rvrs_time!=0 )
					{
					if( ( rvrs_rule==0 && rest_time==(rvrs_time*100) ) || ( rvrs_rule==1 && (rest_time%(rvrs_time*100))==(rvrs_time*100)-1 ) )
						{
						// １ゲーム中１回だけ交代
dbg[0]++;
						s=spry_rate[1];
						spry_rate[1]=spry_rate[0];
						spry_rate[0]=s;
						}
					}
				}



			if(bf_unit_chk[1]==bf_unit_chk[0])
				{
				unit_out=0;
				}
			else
				{
				unit_out=1;
				if( first_r_error==0 )
					{
					first_r_error=1;
#if !CONN_DBG
					save_on_resume(2);
#endif
					}
				}



			if(bf_cc_count[1]==bf_cc_count[0])
				{
				ccc_out=0;
				}
			else
				{
				ccc_out=1;
				if( first_r_error==0 )
					{
					first_r_error=1;
#if !CONN_DBG
					save_on_resume(2);
#endif
					}
				}



			if(bf_rnd_count[1]==bf_rnd_count[0])
				{
				rnd_out=0;
				}
			else
				{
				rnd_out=1;
				if( first_r_error==0 )
					{
					first_r_error=1;
#if !CONN_DBG
					save_on_resume(2);
#endif
					}
				}



#if !CONN_DBG
/*
			if(!game_end)
				{
				if(ccc_wait[0] && ccc_wait[1]==0 )
					{
					if(game_speed==1)					
						game_speed=2;
					else
						game_speed=4;
					}
				else
					game_speed=1;
				}
*/
#endif




			ccc_wait[0]=0;
			ccc_wait[1]=0;

			go_next_1=0;


			if( bf_new_pp[1].used==0 && bf_new_slct[1].sw==0 && bf_new_menu[1].menu==0 && bf_game_system_menu[1]==0 && bf_arrived_unit[1]==0 )
				{
				// 命令が無い場合。
				dp_flag.dwType = DP_NO_ORDER;
//t				hr=lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_flag, sizeof(_DP_FLAG) );

				bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
				bufferDesc.pBufferData  = (byte*) &dp_flag;
				g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );


				}
			else
				{
				// まず、このフェーズで溜めた、命令をセンドする。
				if( bf_game_system_menu[1]!=0 )
					{
					dp_flag.dwType = bf_game_system_menu[1];
//t					hr=lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_flag, sizeof(_DP_FLAG) );

					bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
					bufferDesc.pBufferData  = (byte*) &dp_flag;
					g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );


					game_system_menu[1]=bf_game_system_menu[1];
					}
				else if( bf_arrived_unit[1]!=0 )
					{
					dp_data_1.dwType = DP_ARRIVED_UNIT;
					dp_data_1.data[0] = bf_arrived_unit[1];
//t					lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer,DPSEND_GUARANTEED , &dp_data_1, sizeof(DP_DATA_1) );
					bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
					bufferDesc.pBufferData  = (byte*) &dp_data_1;
					g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
					}
				else if(bf_new_pp[1].used!=0)
					{
					// 移動
					ship=0;
					plane=0;
					for( s=0; s<=JPN_SHIP_END-1; s++)
						{
						ship+=bf_slct_unit[1][s];
						}
					for( s=JPN_SHIP_END; s<=(USA_PLANE_END/2)-1; s++)
						{
						plane+=bf_slct_unit[1][s];
						}

					if(ship!=0 && plane!=0)
						{
						// 航空機も艦船もある
						dp_new_pp.dwType = DP_NEW_PP;
						dp_new_pp.used=(byte)bf_new_pp[1].used;
						dp_new_pp.x=(short)bf_new_pp[1].x;
						dp_new_pp.y=(short)bf_new_pp[1].y;
						dp_new_pp.cls=bf_new_pp[1].cls;
						for( s=0; s<=(USA_PLANE_END/2)-1; s++)
							{
							dp_new_pp.slct_unit[s]=(byte)bf_slct_unit[1][s];
							}
//t						hr = lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_new_pp, sizeof(_DP_NEW_PP) );
						bufferDesc.dwBufferSize = (uint)(sizeof(_DP_NEW_PP));
						bufferDesc.pBufferData  = (byte*) &dp_new_pp;
						g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

						}
					else if( ship!=0 && plane==0 )
						{
						// 艦船のみ
						dp_new_pp_ship.dwType = DP_NEW_PP_SHIP;
						dp_new_pp_ship.used=(byte)bf_new_pp[1].used;
						dp_new_pp_ship.x=(short)bf_new_pp[1].x;
						dp_new_pp_ship.y=(short)bf_new_pp[1].y;
						dp_new_pp_ship.cls=bf_new_pp[1].cls;
						for( s=0; s<=JPN_SHIP_END-1; s++)
							{
							dp_new_pp_ship.slct_unit[s]=(byte)bf_slct_unit[1][s];
							}
//t						hr = lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_new_pp_ship, sizeof(_DP_NEW_PP_SHIP) );
						bufferDesc.dwBufferSize = (uint)(sizeof(_DP_NEW_PP_SHIP));
						bufferDesc.pBufferData  = (byte*) &dp_new_pp_ship;
						g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
						}
					else if( ship==0 && plane!=0 )
						{
						// 航空機のみ
						dp_new_pp_plane.dwType = DP_NEW_PP_PLANE;
						dp_new_pp_plane.used=(byte)bf_new_pp[1].used;
						dp_new_pp_plane.x=(short)bf_new_pp[1].x;
						dp_new_pp_plane.y=(short)bf_new_pp[1].y;
						dp_new_pp_plane.cls=bf_new_pp[1].cls;
						for( s=0; s<=(JPN_PLANE_END-JPN_PLANE_START); s++)
							{
							dp_new_pp_plane.slct_unit[s]=(byte)bf_slct_unit[1][s+JPN_SHIP_END];
							}
//t						hr = lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_new_pp_plane, sizeof(_DP_NEW_PP_PLANE) );
						bufferDesc.dwBufferSize = (uint)(sizeof(_DP_NEW_PP_PLANE));
						bufferDesc.pBufferData  = (byte*) &dp_new_pp_plane;
						g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
						}
					}
				else if(bf_new_slct[1].sw!=0)
					{
					// 目標指定
					ship=0;
					plane=0;
					for( s=0; s<=JPN_SHIP_END-1; s++)
						{
						ship+=bf_slct_unit[1][s];
						}
					for( s=JPN_SHIP_END; s<=(USA_PLANE_END/2)-1; s++)
						{
						plane+=bf_slct_unit[1][s];
						}


					if(ship!=0 && plane!=0)
						{
						// 航空機も艦船もある
						dp_new_slct.dwType = DP_NEW_SLCT;
						dp_new_slct.sw=bf_new_slct[1].sw;
						dp_new_slct.the_slct_unit=(byte)bf_new_slct[1].the_slct_unit;
						dp_new_slct.m=(byte)bf_new_slct[1].m;
						dp_new_slct.gr_x=(short)bf_new_slct[1].gr_x;
						dp_new_slct.gr_y=(short)bf_new_slct[1].gr_y;
						for( s=0; s<=(USA_PLANE_END/2)-1; s++)
							{
							dp_new_slct.slct_unit[s]=(byte)bf_slct_unit[1][s];
							}
//t						hr = lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_new_slct, sizeof(_DP_NEW_SLCT) );
						bufferDesc.dwBufferSize = (uint)(sizeof(_DP_NEW_SLCT));
						bufferDesc.pBufferData  = (byte*) &dp_new_slct;
						g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
						}
					else if( ship!=0 && plane==0 )
						{
						// 艦船のみ
						dp_new_slct_ship.dwType = DP_NEW_SLCT_SHIP;
						dp_new_slct_ship.sw=bf_new_slct[1].sw;
						dp_new_slct_ship.the_slct_unit=(byte)bf_new_slct[1].the_slct_unit;
						dp_new_slct_ship.m=(byte)bf_new_slct[1].m;
						dp_new_slct_ship.gr_x=(short)bf_new_slct[1].gr_x;
						dp_new_slct_ship.gr_y=(short)bf_new_slct[1].gr_y;
						for( s=0; s<=JPN_SHIP_END-1; s++)
							{
							dp_new_slct_ship.slct_unit[s]=(byte)bf_slct_unit[1][s];
							}
//t						hr = lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_new_slct_ship, sizeof(_DP_NEW_SLCT_SHIP) );
						bufferDesc.dwBufferSize = (uint)(sizeof(_DP_NEW_SLCT_SHIP));
						bufferDesc.pBufferData  = (byte*) &dp_new_slct_ship;
						g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1, 0, null, ref hAsync, MUST_SEND );
						}
					else if( ship==0 && plane!=0 )
						{
						// 航空機のみ
						dp_new_slct_plane.dwType = DP_NEW_SLCT_PLANE;
						dp_new_slct_plane.sw=bf_new_slct[1].sw;
						dp_new_slct_plane.the_slct_unit=(byte)bf_new_slct[1].the_slct_unit;
						dp_new_slct_plane.m=(byte)bf_new_slct[1].m;
						dp_new_slct_plane.gr_x=(short)bf_new_slct[1].gr_x;
						dp_new_slct_plane.gr_y=(short)bf_new_slct[1].gr_y;
						for( s=0; s<=JPN_PLANE_END-JPN_PLANE_START; s++)
							{
							dp_new_slct_plane.slct_unit[s]=(byte)bf_slct_unit[1][s+JPN_SHIP_END];
							}
//t						hr = lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_new_slct_plane, sizeof(_DP_NEW_SLCT_PLANE) );
						bufferDesc.dwBufferSize = (uint)(sizeof(_DP_NEW_SLCT_PLANE));
						bufferDesc.pBufferData  = (byte*) &dp_new_slct_plane;
						g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
						}
					else if( ship==0 && plane==0 )
						{
						// ユニットに対する指定無し。おそらく輸送船の揚陸先
						dp_new_slct_land.dwType = DP_NEW_SLCT_LAND;
						dp_new_slct_land.sw=bf_new_slct[1].sw;
						dp_new_slct_land.the_slct_unit=(byte)bf_new_slct[1].the_slct_unit;
						dp_new_slct_land.m=(byte)bf_new_slct[1].m;
						dp_new_slct_land.gr_x=(short)bf_new_slct[1].gr_x;
						dp_new_slct_land.gr_y=(short)bf_new_slct[1].gr_y;
//t						hr = lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_new_slct_land, sizeof(_DP_NEW_SLCT_LAND) );
						bufferDesc.dwBufferSize = (uint)(sizeof(_DP_NEW_SLCT_LAND));
						bufferDesc.pBufferData  = (byte*) &dp_new_slct_land;
						g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
						}
					}
				else if(bf_new_menu[1].menu!=0)
					{
					// メニュー
					dp_new_menu.dwType = DP_NEW_MENU;

					dp_new_menu.menu=(byte)bf_new_menu[1].menu;
					dp_new_menu.the_slct_unit=(byte)bf_new_menu[1].the_slct_unit;

					for( s=0; s<=(USA_PLANE_END/2)-1; s++)
						{
						dp_new_menu.slct_unit[s]=(byte)bf_slct_unit[1][s];
						}
//t					hr = lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_new_menu, sizeof(_DP_NEW_MENU) );
					bufferDesc.dwBufferSize = (uint)(sizeof(_DP_NEW_MENU));
					bufferDesc.pBufferData  = (byte*) &dp_new_menu;
					g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
					}	
				}
			you_can_order=0;
			}



		/* 受信と命令発フェイズ */
//cnct_loop_pt2=2;




		if((cc_count%cnct_loop)==(cnct_loop_pt2) )
			{
			if( CONN_DBG==0/*1*/ && go_next_2==0)
				{
				if(ccc_wait[1]<99)
					ccc_wait[1]++;
				return;
				}

			go_next_2=0;


			if( map_edit==0 && game_end==0 && g_dwNumberOfActivePlayers==2 )
				{
				auto_save_time++;
#if CONN_DBG
				if(auto_save_time>=90 )
#else
				if(auto_save_time>=90 && first_r_error==0 )
#endif
					{
					auto_save_time=0;
#if !CONN_DBG
					save_on_resume(3);
#endif
					}
				}


			if( game_system_menu[0]!=0 || game_system_menu[1]!=0 )
				{
				switch( game_system_menu[1] )
					{
					case GO_GAME_SETTING:
						go_cnct_game_setting();
						break;
					case RESUME_AND_GO_GAME_SETTING:
						save_on_resume(1);
						go_cnct_game_setting();

						if( you_are_host!=0 && you_were_host==0 )
							you_were_host=1;

						break;
					}
				}



			if( your_side==JPN )
				{
				if(bf_arrived_unit[0]!=0)
					{
					new_unit_arrived( 0, bf_arrived_unit[0] );		// 敵サイドが１ユニット増える
					}
				if(bf_arrived_unit[1]!=0)
					{
					new_unit_arrived( 1, bf_arrived_unit[1] );		// 自サイドが１ユニット増える
					}
				}
			else
				{
				if(bf_arrived_unit[1]!=0)
					{
					new_unit_arrived( 1, bf_arrived_unit[1] );		// 自サイドが１ユニット増える
					}
				if(bf_arrived_unit[0]!=0)
					{
					new_unit_arrived( 0, bf_arrived_unit[0] );		// 敵サイドが１ユニット増える
					}
				}




			bf_the_slct_unit=the_slct_unit;
			for( s=0; s<=max_unit; s++)
				{
				bf_2_slct_unit[s]=slct_unit[1][s];
				slct_unit[0][s]=0;
				slct_unit[1][s]=0;
				}


			// 敵味方両方の命令データをここで入力する。
			for(e=0; e<=1; e++)
				{
				new_pp[e].used=bf_new_pp[e].used;
				new_pp[e].x=(double)bf_new_pp[e].x;
				new_pp[e].y=(double)bf_new_pp[e].y;
				new_pp[e].cls=bf_new_pp[e].cls;

				new_slct[e].sw=bf_new_slct[e].sw;
				new_slct[e].the_slct_unit=bf_new_slct[e].the_slct_unit;
				new_slct[e].m=bf_new_slct[e].m;
				new_slct[e].gr_x=(short)bf_new_slct[e].gr_x;
				new_slct[e].gr_y=(short)bf_new_slct[e].gr_y;
				}



			e=0;
			if(your_side!=JPN)
				{
				// 日本海軍サイド
//				for(s=0;s<=19;s++)
				for(s=0;s<=JPN_SHIP_END-1;s++)
					{
					// 水上ユニット
					slct_unit[e][s+1]=bf_slct_unit[e][s];
					}
//				for(s=20;s<=49;s++)
				for(s=JPN_SHIP_END;s<=(USA_PLANE_END/2)-1;s++)
					{
					// 航空ユニット
					slct_unit[e][s+JPN_SHIP_END/*20*/+1]=bf_slct_unit[e][s];
					}
				}
			else
				{
				// 合衆国海軍サイド
//				for(s=0;s<=19;s++)
				for(s=0;s<=JPN_SHIP_END-1;s++)
					{
					// 水上ユニット
					slct_unit[e][s+JPN_SHIP_END/*20*/+1]=bf_slct_unit[e][s];
					}
//				for(s=20;s<=49;s++)
				for(s=JPN_SHIP_END;s<=(USA_PLANE_END/2)-1;s++)
					{
					// 航空ユニット
					slct_unit[e][s+(USA_PLANE_END/2)/*50*/+1]=bf_slct_unit[e][s];
					}
				}

			e=1;
			if(your_side==JPN)
				{
				// 日本海軍サイド
//				for(s=0;s<=19;s++)
				for(s=0;s<=JPN_SHIP_END-1;s++)
					{
					// 水上ユニット
					slct_unit[e][s+1]=bf_slct_unit[e][s];
					}
//				for(s=20;s<=49;s++)
				for(s=JPN_SHIP_END;s<=(USA_PLANE_END/2)-1;s++)
					{
					// 航空ユニット
					slct_unit[e][s+JPN_SHIP_END/*20*/+1]=bf_slct_unit[e][s];
					}
				}
			else
				{
				// 合衆国海軍サイド
//				for(s=0;s<=19;s++)
				for(s=0;s<=JPN_SHIP_END-1;s++)
					{
					// 水上ユニット
					slct_unit[e][s+JPN_SHIP_END/*20*/+1]=bf_slct_unit[e][s];
					}
//				for(s=20;s<=49;s++)
				for(s=JPN_SHIP_END;s<=(USA_PLANE_END/2)-1;s++)
					{
					// 航空ユニット
					slct_unit[e][s+(USA_PLANE_END/2)/*50*/+1]=bf_slct_unit[e][s];
					}
				}



			//
			cnct_game_input_now();
			cnct_unit_info_cont_now();

			//
			for( s=0; s<=max_unit; s++)
				{
				slct_unit[1][s]=bf_2_slct_unit[s];
				}
			the_slct_unit=bf_the_slct_unit;


			bf_new_pp[0].used=0;						// クリア
			bf_new_pp[1].used=0;						// クリア

			bf_new_slct[0].sw=0;						// クリア
			bf_new_slct[1].sw=0;						// クリア

			bf_new_menu[0].menu=0;
			bf_new_menu[1].menu=0;
	
			bf_game_system_menu[0]=0;
			bf_game_system_menu[1]=0;

			game_system_menu[0]=0;
			game_system_menu[1]=0;

			bf_arrived_unit[0]=0;						// クリア
			bf_arrived_unit[1]=0;						// クリア



//			for(s=0;s<=49;s++)
			for(s=0;s<=(USA_PLANE_END/2)-1;s++)
				{
				bf_slct_unit[0][s]=0;
				bf_slct_unit[1][s]=0;
				}

			you_can_order=1;
			you_ordered=0;



#if false && CONN_DBG
dbg[5]=rnd(100);
#endif
			dp_flag.dwType = DP_FLAG_1;

			// プログラム同期エラーチェックの為
//			dp_flag.cc_chk=(BYTE)1024;
//			bf_cc_count[1]=(BYTE)1024;
			dp_flag.cc_chk=(byte)cc_count;
			bf_cc_count[1]=(byte)cc_count;


			// 座標のずれチェックの為
			f=0;
			for(m=1; m<=max_unit; m++)
				{
				if( unit[m].used!=0 && !(unit[m].ctgry==PLANE && unit[m].info[0]==PARKING) )
//				if( unit[m].used && /*unit[m].ctgry==PLANE &&*/ unit[m].hp[0]  )
					{
					f+=(int)((unit[m].x+unit[m].y+unit[m].drctn)*10000);
					}
				}


			dp_flag.unit_chk=(byte)f;
			bf_unit_chk[1]=(byte)f;


			// ランダム同期エラーチェックのため
			dp_flag.rnd_chk=(byte)rnd_count;
			bf_rnd_count[1]=(byte)rnd_count;

			dp_flag.ccc_wait_chk=ccc_wait[1];

			dp_flag.rival_mode=mode;

//t			hr=lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_flag, sizeof(_DP_FLAG) );
			bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
			bufferDesc.pBufferData  = (byte*) &dp_flag;
			g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
			}
		}
#endif





	find_out();

	cc_count++;



	if( game_end!=0 || mode!=CMBT )
		return;	



	// 雲を制御します
	cloud_cont( );



	// エフェクトのデクリ
	for( m=1; m<EFFECT_MAX/*255*/; m++)
		{
		if( effect[m].layer==UPPER || effect[m].layer==LOWER)
			{
			effect[m].info[0]--;
			if( effect[m].info[0]==0 )
				effect[m].layer=0;
			}
		}



	// 補給値のインクリ
	if( (cc_count%300)==0 )
		{
		if( you_are_host!=0 )
			{
			spry_pt+=spry_rate[0];
			}
		else
			{
			spry_pt+=spry_rate[1];
			}
		if(spry_pt>9999)
			spry_pt=9999;
		}





	for(m=1;m<=max_unit;m++)
		{


		// 陸上施設の工事処理
		if( unit[m].used!=0 && ( unit[m].kind==AP||unit[m].kind==SP||unit[m].kind==GF1||unit[m].kind==GF2||unit[m].kind==GF3 ) && unit[m].info[0]!=0
			)
			{
			if( unit[m].hp[0]<unit[m].hp[1] )
				{
				// 損傷してればその修理が先。
				if( (cc_count%220/*80*/)==0 && unit[m].gas[0]>=0 )
					unit[m].hp[0]++;
				}
			else
				{
				// 損傷がなければ工事
				unit[m].info[0]--;

				if( unit[m].info[0]==0 )
					{
					// 完成
					unit[m].hp[1]*=4;
					unit[m].hp[0]=unit[m].hp[1];
					}
				}
			}



		if( map_edit==0 && unit[m].used!=0 && !(unit[m].kind==AP||unit[m].kind==SP||unit[m].kind==CT1))
			{
			// 補給先がちゃんとあるか
			if( unit[m].ctgry==SHIP && unit[m].spry!=0 && unit[m].gas[0]>=0 )
				{
				// 補給中の艦船
				// ptin dbg
				wrk_rect.top=(int)unit[m].y+40+240;  //(int)unit[m].y-40-240;    
				wrk_rect.right=(int)unit[m].x+40+240;
				wrk_rect.bottom=(int)unit[m].y-40-240;    //(int)unit[m].y+40+240; 
				wrk_rect.left=(int)unit[m].x-40-240;
				
				flg=0;
				for( i=1; i<=max_unit; i++)
					{
					if( unit[i].used==unit[m].used && unit[i].kind==SP && unit[i].info[0]==0 )
						{
						if( pt_in_rect3( ref wrk_rect, (int)unit[i].x, (int)unit[i].y )!=0 )
							{
							flg=1;
							break;
							}
						}
					}

				if( flg==0 )
					unit[m].spry=0;
				}


			// ユニットの補給			
			if( unit[m].spry!=0 && unit[m].gas[0]>=0 )
				{
				unit[m].spry++;

				if( unit[m].hp[0]<unit[m].hp[1] && (unit[m].spry%200)==199 && unit[m].gas[0]>=0 )
					{ unit[m].hp[0]++; unit[m].spry=1;}

				if( unit[m].hp[0]==unit[m].hp[1] && unit[m].arm[1]<unit[m].arm[4] && (unit[m].spry%10)==9 )
					{ unit[m].arm[1]++; unit[m].spry=1; }

				if( unit[m].hp[0]==unit[m].hp[1] && unit[m].arm[1]==unit[m].arm[4] && unit[m].gas[0]<100 && (unit[m].spry%20)==19 )
					{
					unit[m].gas[0]++;
					if(unit[m].gas[0]>100)
						unit[m].gas[0]=100;
					unit[m].spry=1; 
					}

				if( unit[m].hp[0]==unit[m].hp[1] && unit[m].arm[1]==unit[m].arm[4] && unit[m].gas[0]==100 && (unit[m].spry%100)==99 )
					{
					unit[m].spry=0;
					}
				}

			//=========		 ユニットの機動制御		=========//

			if(unit[m].ctgry==PLANE && unit[m].info[0]==PARKING)
				{
				// パーキング中の航空機へ
				if( unit[m].stop!=0 )
					{
					unit[m].pp_x[0]=unit[unit[m].info[1]].x;
					unit[m].pp_y[0]=unit[unit[m].info[1]].y;
					}
				else
					{
					if( unit[m].info[5]<=SLOW  )
						{
						// 発進
						if( unit[m].info[3]==0)
							{
							unit[m].info[3]=1;
							unit[unit[m].info[1]].info[4]++;		// 発艦予定の機数を
							}
						cv_1=sprt[UNIT_INFO_JPN].x+sprt[UNIT_INFO_JPN].wd/2;
						if((int)unit[m].x==cv_1 && unit[m].info[3]==1)
							{	
							unit[m].info[3]=2;
							unit[m].drctn=270.0;
							}
						cv_1=sprt[UNIT_INFO_JPN].y+370;
						if((int)unit[m].y>=cv_1 && unit[m].info[3]==2)
							{
							unit[m].info[3]=3;
							unit[m].drctn=90.0;

							unit[unit[m].info[1]].info[11]++;
							unit[unit[m].info[1]].info[11]&=0xffff;	

							if( (unit[unit[m].info[1]].info[11]%2)!=0 )
								{
								unit[m].x+=15;
								}
							else
								{
								unit[m].x-=15;
								}
							}
						cv_1=sprt[UNIT_INFO_JPN].y+370-80;
						if((int)unit[m].y==cv_1 && unit[m].info[3]==3 )
							{
							unit[m].info[3]=4;
							if( unit[m].kind!=BM1 && !(unit[m].kind==FT1&&unit[m].type==1) )
								unit[m].os_indx_y--;
							unit[m].info[4]=0;
							}



						cv_1=sprt[UNIT_INFO_JPN].y+370-120;
						if((int)unit[m].y<=cv_1 && unit[m].info[3]>=4)
							{	// 加速します
							unit[m].info[4]+=1;

							if( unit[m].info[4]==40 && unit[m].info[1]==unit_info[3] &&  (unit_info[0]==CV1 || unit_info[0]==CVL1 || unit_info[0]==AP ) )
								{
								SoundPlayEffect( 0, TAKE_OFF,(double)(MAP_RIGHT+1), 0);
								}

							unit[m].x+=cos(unit[m].drctn*a_PI)*(unit[m].info[4]/20);
							unit[m].y-=sin(unit[m].drctn*a_PI)*(unit[m].info[4]/20);
							}

						cv_1=sprt[UNIT_INFO_JPN].y-30/*+60*/;
						if((int)unit[m].y<=cv_1 && unit[m].info[3]>=4 )
							{		// ここで発進はお終い。
							//unit[m].info[3]=100;			// 発進後の最低直線飛行
							unit[m].info[3]=0;			// 発進後の最低直線飛行
							unit[m].info[0]=FLYING;
							//unit[m].info[2]=-1;	// 格納庫の位置、及び、その基地の番機番号
							unit[m].x=unit[unit[m].info[1]].x;
							unit[m].y=unit[unit[m].info[1]].y;
							//unit[m].drctn=unit[unit[m].info[1]].drctn;
							unit[m].spd=1.0;
							if(unit[unit[m].info[1]].info[4]!=0)
								unit[unit[m].info[1]].info[4]--;		// 発艦予定の機数を減らす。
							unit[unit[m].info[1]].info[1]--;		// 現在格納数
							unit[unit[m].info[1]].info[7]=0;		// その空母の次機着艦許可
								// 発進した場合、最初のポイントは空母の方向から決める。
							switch((int)(unit[unit[m].info[1]].drctn/22.5))
								{
								case 0: case 15:
									unit[m].drctn=0.0;
									break;
								case 1: case 2:
									unit[m].drctn=45.0;
									break;
								case 3: case 4:
									unit[m].drctn=90.0;
									break;
								case 5: case 6:
									unit[m].drctn=90.0+45.0;
									break;
								case 7: case 8:
									unit[m].drctn=180.0;
									break;
								case 9: case 10:
									unit[m].drctn=180.0+45.0;
									break;
								case 11: case 12:
									unit[m].drctn=270.0;
									break;
								case 13: case 14:
									unit[m].drctn=270.0+45.0;
									break;
								}
							}
						if(unit[m].info[0]==PARKING)
							{
							unit[m].x+=cos(unit[m].drctn*a_PI)*0.8;
							unit[m].y-=sin(unit[m].drctn*a_PI)*0.8;
							}
						}
					else
						{
						// 着陸
						cv_1=sprt[UNIT_INFO_JPN].y+sprt[UNIT_INFO_JPN].ht-150;
						if((int)unit[m].y<=cv_1 && unit[m].info[3]==1)
							{
							unit[m].info[3]=2;
							unit[m].spd=1.5;
							unit[unit[m].info[1]].info[7]=0;		// その空母の次機着艦許可
							}
						cv_1=sprt[UNIT_INFO_JPN].y+sprt[UNIT_INFO_JPN].ht-200;
						if((int)unit[m].y<=cv_1 && unit[m].info[3]==2)
							{
							unit[m].info[3]=3;
							unit[m].spd=unit[m].spd/2;
							if( unit[m].kind!=BM1 && !(unit[m].kind==FT1&&unit[m].type==1) )
								unit[m].os_indx_y++;
							}
						cv_1=sprt[UNIT_INFO_JPN].y+sprt[UNIT_INFO_JPN].ht-270;
						if( (int)unit[m].y<=cv_1 && unit[m].info[3]==3 )
							{
							// 着艦終了
							unit[m].info[3]=0;

							unit[m].info[9]=0;					// 戦闘機の場合は制空出撃フラグ

							unit[m].info[5]=MOVE;				// モード（コンバットメニュー）
							set_pos_of_parking(m);
							unit[m].stop=1;

							unit[m].arm[0]=TUN;					// 武装品種
							unit[m].arm[1]=1;						// 数
							unit[m].arm[3]=TUNE_SPAN+(unit[m].hp[1]-unit[m].hp[0])*(TUNE_SPAN/10)+((unit[m].kind==BM1 ? 1 : 0)*(TUNE_SPAN/3));		// 数
							unit[m].hp[0]=unit[m].hp[1];

							unit[m].pp_x[0]=unit[unit[m].info[1]].x;
							unit[m].pp_y[0]=unit[unit[m].info[1]].y;
							unit[m].pp_x[1]=MAP_RIGHT+1;

							f=0;
/***
							for( i=0; i<=max_unit; i++)
								{
								if( unit[i].used && unit[i].ctgry==PLANE && unit[i].used==your_side && unit[i].info[3] 
									&& unit[i].info[0]==PARKING && unit[m].info[1]==unit[i].info[1] && unit[i].stop==0 )
									f++;
								}
***/
							for( i=1; i<=max_unit; i++)
								{
								if( i!=m && unit[i].used!=0 && unit[i].ctgry==PLANE && unit[i].used==your_side && unit[i].info[3]!=0
									&& unit[i].info[0]==PARKING && unit[m].info[1]==unit[i].info[1] && unit[i].stop==0 )
									f++;
								}


							if( f==0 )
								{	// 滑走路に他の着陸機がなければ発進許可。
								unit[unit[m].info[1]].info[8]=0;		// その空母の次機発進許可
								}
else
	unit[unit[m].info[1]].info[8]=2+f;		// 空母の発進不可の調査のため

							}
						if(unit[m].info[0]==PARKING)
							{
							unit[m].x+=cos(unit[m].drctn*a_PI)*unit[m].spd;
							unit[m].y-=sin(unit[m].drctn*a_PI)*unit[m].spd;
							}
						}
					}
				}
			else
				{
				// 移動中の各ユニットへ
				if( 1!=0 )
					{
					// 戦闘機動および、緊急移動
					if( unit[m].spry==0 )
						{
						switch( unit[m].kind )
							{
							case CA1:
							case DD1:
							case BB1:			
							case CV1:
							case CVL1:
								if( unit[m].em_flg[0]==0 )
									set_pos_of_emrgncy_SHIP(m);
								break;



							case TR1:
								if( unit[m].em_flg[0]==0 && unit[m].arm[2]!=0 )
									set_pos_of_attack_TR1(m);
								if( unit[m].em_flg[0]==0  )
									set_pos_of_emrgncy_SHIP(m);
								break;


							case SS1:
								if( unit[m].em_flg[0]==0  )
									em_of_out_of_map(m);
								break;


							case FT1:
								set_pos_of_emrgncy_FT(m);
								if( unit[m].arm[2]!=0 )
									{
									set_pos_of_attack_FT(m);	// 攻撃目標あり
									}
								else
									{
									if( unit[m].em_flg[0]==0 && unit[m].info[5]==RETURN )
										set_pos_of_emrgncy_AT(m);
									}
//unit[m].arm2[0]++;
								break;



							case AT1:
							case BM1:
								if( unit[m].arm[2]!=0 && unit[m].ltl_ldr==0 )
									set_pos_of_attack_AT(m);
								if( unit[m].em_flg[0]==0 )
									set_pos_of_emrgncy_AT(m);

								if ( unit[m].kind==BM1 && (unit[unit[m].info[1]].kind!=AP) )
									{
									unit[m].info[1]=0;
									}
//unit[m].arm2[0]++;
								break;
							}
						}


					if( unit[m].ltl_ldr!=0 && unit[unit[m].ltl_ldr].used==0 )
						unit[m].ltl_ldr=0;



					if( unit[m].ltl_ldr==0 )
						{
						// 単独、もしくは、編隊長
						if(unit[m].stop==0 )
							{
							if( /*paint_effect_on &&*/ unit[m].ctgry==PLANE && unit[m].info[5]==RETURN && unit[m].info[3]==1 )
								{
								cont_pos_of_take_down(m);
								}

							// ptin dbg
							wrk_r.top=(int)unit[m].pp_y[0]+ON_PP;//(int)unit[m].pp_y[0]-ON_PP;
							wrk_r.right=(int)unit[m].pp_x[0]+ON_PP;
							wrk_r.bottom=(int)unit[m].pp_y[0]-ON_PP; //(int)unit[m].pp_y[0]+ON_PP;
							wrk_r.left=(int)unit[m].pp_x[0]-ON_PP;

							if( pt_in_rect3(ref wrk_r,(int)unit[m].x,(int)unit[m].y)!=0 )
								{
								if( unit[m].pp_x[1]!=MAP_RIGHT+1 )
									{
									// ＰＰ＿ＸＹを一つずつずらす
									for(n=0; unit[m].pp_x[n]!=MAP_RIGHT+1; n++)
										{
										unit[m].pp_x[n]=unit[m].pp_x[n+1];
										unit[m].pp_y[n]=unit[m].pp_y[n+1];
										}
									}
								else
									{
									// ＰＰの再終点に到着
									if( unit[m].ctgry==SHIP )
										{
										unit[m].stop=1;
										}
									else
										{
										if( unit[m].ctgry==PLANE && unit[m].info[5]==RETURN /*&& unit[m].info[3]==0*/)
											{
											unit[m].info[3]=1;
											set_pos_of_take_down(m);
											for(n=1;n<=max_unit;n++)
												{
												if( unit[n].used!=0 && unit[n].ltl_ldr==m )
													{
													unit[n].ltl_ldr=0;
													unit[n].pp_x[0]+=(double)(rnd(600)-300);
													unit[n].pp_y[0]+=(double)(rnd(600)-300);
//													unit[n].pp_x[0]+=(rnd(500));
//													unit[n].pp_y[0]+=(rnd(500));
//unit[n].pp_x[0]+=(unit[n].rnd_250[0]*2)-250;
//unit[n].pp_y[0]+=(unit[n].rnd_250[1]*2)-250;
//unit[n].pp_x[0]+=500;
//unit[n].pp_y[0]+=500;
													unit[n].pp_x[1]=MAP_RIGHT+1;
													}
												}
											if( unit[m].is_ltl_ldr!=0 )
												{
												unit[m].is_ltl_ldr=0;	unit[m].no=0;
												}
											}
										}
									}
								}
							}
						else
							{
							unit[m].pp_x[0]=unit[m].x;
							unit[m].pp_y[0]=unit[m].y;
							}
						}
					else
						{
						// 編隊追随機
						if(unit[m].stop==0)
							{
							if( (cc_count%10)==0 )
								{
								set_pos_of_dynmc(m);
								}
							}
						else
							{
							unit[m].pp_x[0]=unit[m].x;
							unit[m].pp_y[0]=unit[m].y;
							}
					
						// ptin dbg
						wrk_r.top=(int)unit[m].pp_y[0]+30;//(int)unit[m].pp_y[0]-30;
						wrk_r.right=(int)unit[m].pp_x[0]+30;
						wrk_r.bottom=(int)unit[m].pp_y[0]-30;//(int)unit[m].pp_y[0]+30;
						wrk_r.left=(int)unit[m].pp_x[0]-30;

//dbg[2]++;

						if( pt_in_rect3(ref wrk_r,(int)unit[m].x,(int)unit[m].y)==0 && unit[m].stop==0)
							{	// 編隊指定位置に無し
//dbg[3]++;
							unit[m].for_ltl_ldr=0;
							if( unit[unit[m].ltl_ldr].for_ltl_ldr==0 )
								unit[unit[m].ltl_ldr].for_ltl_ldr=1;

/*
if( m==131 && unit[129].arm2[0]==52-1 )
{
dbg[2]=unit[unit[m].ltl_ldr].for_form_spd*100000;		// この時点で値が　０と１９００００
dbg[3]=unit[m].max_spd*100000;
}
*/

							if( unit[unit[m].ltl_ldr].for_form_spd > unit[m].max_spd || unit[unit[m].ltl_ldr].for_form_spd==0 )
								{
								unit[unit[m].ltl_ldr].for_form_spd = unit[m].max_spd;
/*
if( m==131 && unit[129].arm2[0]==52-1 )
{
dbg[7]=1;
}
*/								}

							}
						else
							{	// 編隊指定位置にあり
//dbg[4]++;
							unit[m].for_ltl_ldr=2;
							if( unit[m].ctgry==SHIP && unit[unit[m].ltl_ldr].stop==1 )
								{
								unit[m].stop=1;
								}

							}

/*
if( m==131 && unit[129].arm2[0]==52-1 )
{
dbg[4]=unit[unit[m].ltl_ldr].for_form_spd*100000;
}
*/




						}
					}




			//  if ( !(unit[m].ctgry==SHIP && unit[m].spry) )									
			//=========		 ユニットの座標変更		=========//
				if( (unit[m].stop==0 || unit[m].em_flg[0]!=0)  && !(unit[m].ctgry==SHIP && unit[m].spry!=0) )	
					{
					if( unit[m].em_flg[0]!=0	)
						{
						// 緊急移動先についているか
						// ptin dbg
						wrk_r.top=(int)unit[m].em_y+ON_PP;//(int)unit[m].em_y-ON_PP;
						wrk_r.right=(int)unit[m].em_x+ON_PP;
						wrk_r.bottom=(int)unit[m].em_y-ON_PP;//(int)unit[m].em_y+ON_PP;
						wrk_r.left=(int)unit[m].em_x-ON_PP;

						if( pt_in_rect3(ref wrk_r,(int)unit[m].x,(int)unit[m].y)!=0 )
							{
							unit[m].em_flg[0]=0;
							// 通常移動		直前定点へ！
							wrk_x=unit[m].pp_x[0]-unit[m].x;
							wrk_y=unit[m].pp_y[0]-unit[m].y;
							}
						else
							{
							// 緊急移動先がある場合
							wrk_x=unit[m].em_x-unit[m].x;
							wrk_y=unit[m].em_y-unit[m].y;
							}
						}
					else
						{
						// 通常移動		直前定点へ！
						wrk_x=unit[m].pp_x[0]-unit[m].x;
						wrk_y=unit[m].pp_y[0]-unit[m].y;
						}

					pp_drctn=atan2(wrk_y,wrk_x)*RAD_to;


					if(pp_drctn<0)
						pp_drctn=360+pp_drctn;



					land=0;
					if( unit[m].ctgry==SHIP )
						{
						// 艦首方向に他の艦船があるか
						wrk_x2=unit[m].x;
						wrk_y2=unit[m].y;
						wrk_x2+=cos(unit[m].drctn*a_PI)*(40+unit[m].max_spd*10/*50*/);
						wrk_y2+=sin(unit[m].drctn*a_PI)*(40+unit[m].max_spd*10/*50*/);
						if( /*(cc_count%5 && unit[m].used==cpu_side ) &&*/ !( unit[m].kind==SS1 && unit[m].info[6]!=0 ) )
							{
							for( n=1; n<=max_unit; n++)
								{
								if(unit[n].used!=0 && m!=n && unit[n].ctgry==SHIP && !(unit[n].kind==SS1 && unit[n].info[6]!=0) /*&& unit[n].kind!=SP && unit[n].kind!=AP*/ && !(unit[n].kind>=AP&&unit[n].kind<=GF3) )
									{
									// ptin dbg
									wrk_r.top=(int)unit[n].y+(sprt[UNIT_JPN].ht/2);//(int)unit[n].y-(sprt[UNIT_JPN].ht/2);
									wrk_r.right=(int)unit[n].x+(sprt[UNIT_JPN].wd/2);
									wrk_r.bottom=(int)unit[n].y-(sprt[UNIT_JPN].ht/2);//(int)unit[n].y+(sprt[UNIT_JPN].ht/2);
									wrk_r.left=(int)unit[n].x-(sprt[UNIT_JPN].wd/2);

									if( pt_in_rect3(ref wrk_r,(int)wrk_x2,(int)wrk_y2)!=0)
										{
										// 前方に艦船！
										land=1;

										unit[m].em_flg[0]=0;

										break;
										}
									}
								}
							}


						// ＰＰ方向に陸地があるか
						if( land==0 /*&& unit[m].used!=cpu_side*/ )
							{
							wrk_x2=unit[m].x;
							wrk_y2=unit[m].y;
							wrk_x2+=cos(pp_drctn*a_PI)*80;
							wrk_y2+=sin(pp_drctn*a_PI)*80;

							if(!( wrk_y2>MAP_TOP || wrk_y2<MAP_BOTTOM || wrk_x2<MAP_LEFT || wrk_x2>MAP_RIGHT ))
								{
								cm_scrn_x=(int)((wrk_x2+(sprt[UNIT_JPN].wd/2)-MAP_LEFT)/sprt[MAP_TIP_NRML].wd);
								cm_scrn_y=(int)((MAP_TOP-wrk_y2+(sprt[UNIT_JPN].ht/2))/sprt[MAP_TIP_NRML].ht);
								if( cmbt_map[cm_scrn_y][cm_scrn_x]>=1 && cmbt_map[cm_scrn_y][cm_scrn_x]<=9 )
									{
									land=1;
									}
								}
							}

						if( unit[m].em_flg[0]!=0 && land!=0)
							{
							// 緊急移動の取り消し
							unit[m].em_flg[0]=0;
							}
						}


					pp_drctn=pp_drctn-unit[m].drctn;
					if(pp_drctn<0)
						pp_drctn=360+pp_drctn;


					unit[m].drctn_add=0;
					if(pp_drctn>=1.0&&pp_drctn<=180.0)
						{
						//	左へ
						unit[m].drctn_add=unit[m].a_drctn_add;
						if(pp_drctn <= 3.0)
							unit[m].drctn_add = +0.5;		// 要は微調整	//unit[m].a_drctn_add;
						}
					if(pp_drctn<=359.0 && pp_drctn>180.0)
						{
						//	右へ
						unit[m].drctn_add=-unit[m].a_drctn_add;
						if(pp_drctn >= 357.0)
							unit[m].drctn_add = -0.5;		// 要は微調整	//-unit[m].a_drctn_add;
						}


					unit[m].drctn+=unit[m].drctn_add;


					

					if(unit[m].drctn>=360.0)
						unit[m].drctn-=360.0;
					if(unit[m].drctn<0.0)
						unit[m].drctn=360.0+unit[m].drctn;

					if(pp_drctn>180.0)
						pp_drctn=360.0-pp_drctn;


					// ユニットのスピード
					if( unit[m].ctgry!=PLANE && (pp_drctn>=80.0 || land!=0 ) )
						{
						if( ( unit[m].min_spd ) < unit[m].spd || land!=0 )
							{
							unit[m].spd_add=-unit[m].a_spd_add;
							}
						else
							{
							unit[m].spd_add=+unit[m].a_spd_add;
							}
						}
					else
						{
						if(pp_drctn>=45.0  )
							{


							if((unit[m].max_spd+(unit[m].kind==FT1 && unit[m].arm[2]!=0 ? 1 : 0)*CMBT_SPD)/2 < unit[m].spd  )
								{
								unit[m].spd_add=-unit[m].a_spd_add;
								}
							else
								{
								unit[m].spd_add=+unit[m].a_spd_add;
								}
							}
						else
							{


							if(pp_drctn>=22.5 )
								{




								if( ((unit[m].max_spd+(unit[m].kind==FT1 && unit[m].arm[2]!=0 ? 1 : 0)*CMBT_SPD)/3)*2 < unit[m].spd )
									{
									unit[m].spd_add=-unit[m].a_spd_add;
									}
								else
									{
									unit[m].spd_add=+unit[m].a_spd_add;
									}
								}
							else
								{




								if( unit[m].em_flg[0]!=0 )
									{
									//　緊急移動の場合
									unit[m].spd_add=+unit[m].a_spd_add;
									unit[m].for_ltl_ldr=0;
									}
								else
									{
									//　通常移動
									if( unit[m].for_ltl_ldr==1 )
										{	// 随伴機より。速度落とせの連絡 この場合ｍ番は編隊長

// この時点でfor_form_spdがちがう

										n=0;

										if( unit[m].arm[2]!=0 && unit[unit[m].arm[2]].found!=0 ) //&& unit[m].kind==AT1 )
											{
											n=1;
											wrk_x=unit[unit[m].arm[2]].x-unit[m].x;
											wrk_y=unit[unit[m].arm[2]].y-unit[m].y;

											drctn=atan2(wrk_y,wrk_x)*RAD_to;
											if(drctn<0)		drctn=360+drctn;
											if(wrk_x<0)		wrk_x=0-wrk_x;
											if(wrk_y<0)		wrk_y=0-wrk_y;
											if(drctn>=180)	drctn=drctn-180;
											if(drctn>=90)	drctn=90-(drctn-90);
											dstc=(wrk_x)/(cos(drctn*a_PI));

											if( dstc<=BB1_SIGHT )
												{
												n=1;		// 速度落とす要無し
												}
											}
										if( n==0 && unit[m].spd>=(unit[m].for_form_spd*(0.60-(unit[m].kind==CV1 ? 1 : 0)*0.15 ))+unit[m].a_spd_add )
											{
											unit[m].spd_add=-unit[m].a_spd_add;
											}
										else
											{
											unit[m].spd_add=+unit[m].a_spd_add;

											}


										unit[m].for_ltl_ldr=0;
										unit[m].for_form_spd=0.0;




										}
									else
										{
										if( unit[m].for_ltl_ldr==2 && unit[m].max_spd >= unit[unit[m].ltl_ldr].max_spd )
											{ // 編隊指定位置にいる。編隊Ｌｄｒの速度に合わせよ
											unit[m].spd=unit[unit[m].ltl_ldr].spd;
											unit[m].spd_add=0;
											}
										else
											{	// 単独機か、連絡無しの指揮機
											unit[m].spd_add=+unit[m].a_spd_add;
											}
										unit[m].for_ltl_ldr=0;
										}
									}
								}
							}
						}
					}
				else
					{
					if(unit[m].spd>0)
						{
						unit[m].spd_add=-(unit[m].a_spd_add*2);


						}
					}


/*
if( m==129 && unit[129].arm2[0]==52 )
{
dbg[2]=unit[m].spd_add*100000;
dbg[4]=unit[m].spd*100000;
}
*/

				if( unit[m].gas[0]<=0 && (unit[m].ctgry==PLANE || (unit[m].ctgry==SHIP && unit[m].spd>= unit[m].max_spd/10 ) ) )
					unit[m].spd_add=-(unit[m].a_spd_add*2);		// ガス０なら減速へ
				

				// 速度を決定
				unit[m].spd+=unit[m].spd_add;


				// 最高速度の制限
				if( (unit[m].max_spd+(unit[m].kind==FT1 && unit[m].arm[2]!=0 ? 1 : 0)*CMBT_SPD) < unit[m].spd/*-unit[m].a_spd_add*/ )
					{
					//unit[m].spd-=unit[m].spd_add;
					unit[m].spd-=unit[m].a_spd_add*8;
					if( unit[m].spd < (unit[m].max_spd+(unit[m].kind==FT1 && unit[m].arm[2]!=0 ? 1 : 0)*CMBT_SPD) )
						unit[m].spd = (unit[m].max_spd+(unit[m].kind==FT1 && unit[m].arm[2]!=0 ? 1 : 0)*CMBT_SPD);
					}

				if( unit[m].kind==TR1 && unit[m].arm[1]!=0 )
					{
					// 輸送船でなんかつんでると最高速度がおちる
					switch( unit[m].arm[0] )
						{
						case TR_GF1:	wrk_3=1.0;		break;
						case TR_GF2:	wrk_3=0.9;		break;
						case TR_GF3:	wrk_3=0.9;		break;
						case TR_AP:		wrk_3=0.8;		break;
						case TR_SP:		wrk_3=0.8;		break;
						}

					if( unit[m].max_spd*wrk_3 < unit[m].spd )
						{
						unit[m].spd-=unit[m].a_spd_add*8;
						}

					}


				if(  unit[m].kind==SS1 && unit[m].info[6]==1 && unit[m].spd>(unit[m].max_spd*0.7) )
					{
					unit[m].spd=(unit[m].max_spd*0.7);
					}



				if( (unit[m].stop==0 || unit[m].em_flg[0]!=0) &&!(unit[m].gas[0]<=0))
					{
					if( unit[m].min_spd > unit[m].spd )
						unit[m].spd=unit[m].min_spd;
					}
				else
					{
					if( unit[m].spd < 0 )
						unit[m].spd=0;
					}

				// Em_flgがあるならデクリ
				if(unit[m].em_flg[0]!=0)
					unit[m].em_flg[0]--;




/*
if( m==129 && unit[129].arm2[0]==52 )
{
dbg[5]=unit[m].spd*100000;
}
*/


				// 着艦チェック
				if( unit[m].ctgry==PLANE && unit[m].info[5]==RETURN && unit[m].info[3]==1 && unit[unit[m].info[1]].used!=0 
					&& unit[unit[m].info[1]].info[7]==0 && !(unit[m].ctgry==PLANE && unit[m].info[0]==FLYING && unit[unit[m].info[1]].hp[0]<=unit[unit[m].info[1]].hp[1]*0.2)
					&& !( unit[m].kind==BM1 && (unit[unit[m].info[1]].kind!=AP) )
					&& !( unit[m].kind==FT1 && unit[m].type==1 && (unit[unit[m].info[1]].kind!=AP) )
					&& unit[m].used==unit[unit[m].info[1]].used && unit[unit[m].info[1]].info[0]==0
					)
					{
					if( 0!=0 && unit[unit[m].info[1]].info[4]!=0)
						{
						// ほんまにＩｎｆｏ［４］があるんやなチェック
						}

					if( drctn_for_8((int)unit[m].drctn)==drctn_for_8((int)unit[unit[m].info[1]].drctn) 
					&& unit[unit[m].info[1]].info[2]>unit[unit[m].info[1]].info[1] 
					&& unit[unit[m].info[1]].info[4]<=0 )
						{
						// ptin dbg
						wrk_r.top=(int)unit[unit[m].info[1]].y+ON_PP/2;//(int)unit[unit[m].info[1]].y-ON_PP/2;
						wrk_r.right=(int)unit[unit[m].info[1]].x+ON_PP/2;
						wrk_r.bottom=(int)unit[unit[m].info[1]].y-ON_PP/2;//(int)unit[unit[m].info[1]].y+ON_PP/2;
						wrk_r.left=(int)unit[unit[m].info[1]].x-ON_PP/2;

						if( pt_in_rect3(ref wrk_r,(int)unit[m].x,(int)unit[m].y)!=0 )
							{
							// 着艦
							unit[unit[m].info[1]].info[1]++;	// 現在格納数
							if(m==the_slct_unit)
								{
								the_slct_unit=0; old_the_slct_unit=(short)unit[m].info[1]; cmbt_menu_kind=0; cmbt_menu_slctd=0; cls_all_slct_unit();
								}
							if(m==old_the_slct_unit)
								{
								old_the_slct_unit=(short)unit[m].info[1];
								}

							if(slct_unit[1][m]!=0)
								{
								if(slct_unit_no!=0)
									slct_unit_no--; 
								// セレクトの設定番号を連番にする。
								for(n=1;n<=max_unit;n++)				
									{
									if( slct_unit[1][n]>=slct_unit[1][m]+1 )
										slct_unit[1][n]--;
									}
								slct_unit[1][m]=0; 
								}

//							unit[m].stop=1;
							//unit[m].info[1]=4;				// 所属の空母、及び、基地の番号
							unit[m].info[2]=seek_parking_no(m);	// 格納庫の位置、及び、その基地の番機番号
							unit[m].info[3]=1;					// 格納庫、基地での移動情態
							unit[m].info[4]=0;					// 減速度をクリア
							//unit[m].info[5]=MOVE;				// モード（コンバットメニュー）
							unit[unit[m].info[1]].info[7]=1;	// 着艦、0許可、1不許可
							unit[unit[m].info[1]].info[8]=1;	// その空母の次機発進許可	0許可、1不許可
							unit[m].info[0]=PARKING;
							//set_pos_of_parking(m);
							unit[m].x=sprt[UNIT_INFO_JPN].x+sprt[UNIT_INFO_JPN].wd/2+(my_rnd(16)-7);
							unit[m].y=sprt[UNIT_INFO_JPN].y+sprt[UNIT_INFO_JPN].ht+40;
							unit[m].spd=5.0; unit[m].drctn=90.0;
							
							unit[m].arm[2]=0;					// 

							unit[m].em_flg[0]=0;
//unit[m].arm2[0]=0;
//unit[m].gas[0]=0;

							}
						}
					}


				// 新座標を設定
/*
if( m==129 && unit[129].arm2[0]==52 )
{
//dbg[4]=(unit[m].x+unit[m].y+unit[m].drctn)*100000;

dbg[2]=unit[m].x*100000;
dbg[3]=unit[m].y*100000;
dbg[4]=unit[m].spd*100000;

}
*/
				unit[m].x+=cos(unit[m].drctn*a_PI)*unit[m].spd;
				unit[m].y+=sin(unit[m].drctn*a_PI)*unit[m].spd;


/*
if( m==129 && unit[129].arm2[0]==52 )
{

dbg[5]=unit[m].x*100000;
dbg[6]=unit[m].y*100000;
dbg[7]=unit[m].spd*100000;

}
*/


				// 燃料消費
				n=(int)unit[m].gas[1];
				if( unit[m].kind==AT1 && unit[m].arm[1]!=0 && (unit[m].arm[0]==TPD || unit[m].arm[0]==BOM))
					n=n-(n/10);
				if( unit[m].kind==FT1 && unit[m].arm[2]!=0 && unit[m].max_spd < unit[m].spd )
					n=n-(n/10);


				if( unit[m].gas[1]>=1 && (cc_count%n)==0 && unit[m].gas[0]>0 && unit[m].spd>0)
					{
//					unit[m].gas[0]--;
					unit[m].gas[0]-=(unit[m].spd/unit[m].max_spd);
					if(unit[m].gas[0]<0)
						unit[m].gas[0]=0;
					}

				if( unit[m].gas[0]==0 && unit[m].ctgry==PLANE && unit[m].spd<=0 )
					unit[m].hp[0]=0;							// 飛行機でガス０なら落ちます
				}

					//=========		 潜水艦の浮上		=========//
				//if( unit[m].kind==SS1 && !unit[m].found && unit[m].info[6] )
				//unit[m].info[6]=0;

				//=========		 ユニットの攻撃相手探索		=========//
				
				//seek_enemy(m);	


			
				//=========		 ユニットの攻撃制御		=========//
				// ターゲットがアウトならターゲットをクリア
			if( unit[m].arm[2]!=0 && ( ( unit[unit[m].arm[2]].ctgry==PLANE && ( unit[unit[m].arm[2]].hp[0]<=0|| unit[unit[m].arm[2]].info[0]==PARKING )   ) || ( unit[unit[m].arm[2]].ctgry==SHIP && unit[unit[m].arm[2]].hp[0]<=0 ) || (unit[m].kind==FT1 && unit[m].arm[1]<=0) || (unit[unit[m].arm[2]].kind==SS1 && unit[unit[m].arm[2]].info[6]!=0 ) || ( unit[m].ctgry==PLANE && unit[m].info[0]==PARKING ) ))
				{
				unit[m].arm[2]=0;
				}
			
			if( unit[m].arm[3]>=1 )
				{
				if( !(unit[m].kind==SS1&&unit[m].stop==0))
					unit[m].arm[3]--;
				if( unit[m].arm[3]==0 && (unit[m].arm[0]==TUN||unit[m].arm[0]==NTG) )
					{
					if( unit[m].kind==FT1 )
						{
						unit[m].arm[0]=BLT;		// 武装品種
						unit[m].arm[1]=50;		// 数
						}
					else
						{
						unit[m].arm[0]=NTG;
						unit[m].arm[1]=0;
						}
					
					unit[m].gas[0]=100;
					}
				}


			dmg_act=1;
			if( unit[m].hp[0]<=unit[m].hp[1]*0.3 )
				{
				dmg_act=2;
				}


			// そのユニットの打つ、発射を制御します。  
			if( map_edit==0 && unit[m].spry==0 && (( unit[m].ctgry==PLANE && unit[m].info[0]==FLYING )||( unit[m].ctgry==SHIP)))
				{
				switch( unit[m].kind )
					{
					case GF1:
						// 地上基地は弾が減りません
						unit[m].arm[1]=unit[m].arm[4];

						// 艦砲 自動
						if( unit[m].arm[1]>=1 && rnd(/*200*/250*dmg_act)==0/*unit[m].rnd_200[0]==cc_count%(200*dmg_act)*/ )
							{
							fire_now(m,0,GUN);
							}
						// 艦砲　選択
						//if(unit[m].arm[1]>=1  && rnd(150*dmg_act)==0/*unit[m].rnd_65[0]==cc_count%(65*dmg_act)*/ && unit[m].arm[2] )
						//	{
						//	fire_now(m,unit[m].arm[2],GUN);
						//	}
						// 対空砲弾 自動砲撃
						if( unit[m].arm[1]>=1 && rnd(200*dmg_act)==0/*unit[m].rnd_150[1]==cc_count%(150*dmg_act)*/ /*&& !(unit[m].used==JPN && rnd(3)!=1)*/)
							{
							fire_now(m,0,SHL);
							}
						// 対空機関砲 X 2
						if( rnd(180*dmg_act)==0/*unit[m].rnd_80[0]==cc_count%(80*dmg_act)*/ && unit[m].used==USA )
							fire_now(m,0,RAS);
						// 対空機銃
						if( rnd(10*dmg_act)==0/*unit[m].rnd_20[0]==cc_count%(20*dmg_act)*/ )
							fire_now(m,0,BLT);
						break;


					case GF2:
						// 地上基地は弾が減りません
						unit[m].arm[1]=unit[m].arm[4];
						// 艦砲 自動
						if( unit[m].arm[1]>=1 && rnd( 200*dmg_act )==0/*unit[m].rnd_150[0]==cc_count%(150*dmg_act)*/ )
							{
							fire_now(m,0,GUN);
							}
						// 艦砲　選択
						//if(unit[m].arm[1]>=1  && rnd(100*dmg_act)==0/*unit[m].rnd_65[0]==cc_count%(65*dmg_act)*/ && unit[m].arm[2] )
						//	{
						//	fire_now(m,unit[m].arm[2],GUN);
						//	}
						// 対空砲弾 自動砲撃
						if( unit[m].arm[1]>=1 && rnd(160*dmg_act)==0/*unit[m].rnd_80[1]==cc_count%(80*dmg_act)*/ /*&& !(unit[m].used==JPN && rnd(3)!=1)*/)
							{
							fire_now(m,0,SHL);
							}
						// 対空機関砲
						if( rnd(140*dmg_act)==0 && unit[m].used==USA )
							{
							fire_now(m,0,RAS);
							}
						// 対空機銃
						if( rnd(10*dmg_act)==0 )
							{
							fire_now(m,0,BLT);
							}
						break;


					case GF3:
						// 地上基地は弾が減りません
						unit[m].arm[1]=unit[m].arm[4];

						// 艦砲 自動
						if( unit[m].arm[1]>=1 && rnd(/*100*/150*dmg_act)==0/*unit[m].rnd_100[0]==cc_count%(100*dmg_act)*/ )
							{
							fire_now(m,0,GUN);
							}
						// 艦砲　選択
						//if(unit[m].arm[1]>=1  && rnd(65*dmg_act)==0/*unit[m].rnd_65[0]==cc_count%(65*dmg_act)*/ && unit[m].arm[2] )
						//	{
						//	fire_now(m,unit[m].arm[2],GUN);
						//	}
						// 対空砲弾 自動砲撃
						if( unit[m].arm[1]>=1 && rnd(120*dmg_act)==0/*unit[m].rnd_80[1]==cc_count%(80*dmg_act)*/ /*&& !(unit[m].used==JPN && rnd(3)!=1)*/)
							{
							fire_now(m,0,SHL);
							}
						// 対空機関砲
						if( rnd(120*dmg_act)==0/*unit[m].rnd_65[1]==cc_count%(65*dmg_act)*/ && unit[m].used==USA )
							fire_now(m,0,RAS);
						// 対空機関砲 X 2
						if( rnd(160*dmg_act)==0/*unit[m].rnd_80[0]==cc_count%(80*dmg_act)*/ && unit[m].used==USA )
							fire_now(m,0,RAS);
						// 対空機銃
						if( rnd(8*dmg_act)==0/*unit[m].rnd_20[0]==cc_count%(20*dmg_act)*/ )
							fire_now(m,0,BLT);
						break;




					case BB1:
						if( unit[m].used==JPN && unit[m].type==1 )
							{
							// 大和級
							// 艦砲 自動
							if( unit[m].arm[1]>=1 && rnd(350*dmg_act)==0 && (unit[m].arm[2]==0 || unit[unit[m].arm[2]].ctgry==SHIP) )
								{
								fire_now(m,0,GUN);
								}
							// 艦砲　選択
							if(unit[m].arm[1]>=1 && rnd(300*dmg_act)==0 && unit[m].arm[2]!=0 )
								{
								fire_now(m,unit[m].arm[2],SP_GUN);
								}

							if( unit[m].arm[1]>=1 && rnd(180*dmg_act)==0 && unit[m].arm[2]!=0 )
								{
								fire_now(m,unit[m].arm[2],SHL);		// 対空砲弾 選択
								}

							if( unit[m].arm[1]>=1 && rnd(95*dmg_act)==0 )
								{
								fire_now(m,0,SHL);							// 対空砲弾 自動砲撃
								}

							// 対空機銃
							if( rnd(4*dmg_act)==0 )
								fire_now(m,0,BLT);
							}
						else
							{
							// 艦砲 自動
							if( unit[m].arm[1]>=1 && rnd(350*dmg_act)==0 && (unit[m].arm[2]==0 || unit[unit[m].arm[2]].ctgry==SHIP) )
								{
								fire_now(m,0,GUN);
								}
							// 艦砲　選択
							if(unit[m].arm[1]>=1 && rnd(300*dmg_act)==0 && unit[m].arm[2]!=0 )
								{
								fire_now(m,unit[m].arm[2],GUN);
								}


							if( unit[m].arm[1]>=1 && rnd(190*dmg_act)==0 && unit[m].arm[2]!=0 )
								{
								fire_now(m,unit[m].arm[2],SHL);		// 対空砲弾 選択
								}

							if( unit[m].arm[1]>=1 && rnd(110*dmg_act)==0 )
								{
								fire_now(m,0,SHL);							// 対空砲弾 自動砲撃
								}



							// 対空機関砲
							if( rnd(60*dmg_act)==0 && unit[m].used==USA )
								fire_now(m,0,RAS);
							// 対空機関砲 X 2
							if( rnd(120*dmg_act)==0 && unit[m].used==USA )
								fire_now(m,0,RAS);

							// 対空機銃
							if( rnd(5*dmg_act)==0 )
								fire_now(m,0,BLT);
							}

						break;


					case CA1:
						if( unit[m].type==0 )
							{
							// 巡洋艦
							// 艦砲 自動
							if( unit[m].arm[1]>=1  && rnd(350*dmg_act)==0  && (unit[m].arm[2]==0 || unit[unit[m].arm[2]].ctgry==SHIP) )
								{
								fire_now(m,0,GUN);
								}
							// 艦砲 選択
							if( unit[m].arm[1]>=1  && rnd(300*dmg_act)==0 && unit[m].arm[2]!=0 )
								{
								fire_now(m,unit[m].arm[2],GUN);
								}



							// 対空砲弾
							if( unit[m].arm[1]>=1 && rnd(120*dmg_act)==0  )
								{
								if(unit[m].arm[2]!=0)								// 対空砲弾 選択
									fire_now(m,unit[m].arm[2],SHL);
								else
									fire_now(m,0,SHL);							// 対空砲弾 自動砲撃
								}


							// 対空機関砲
							if( unit[m].arm[1]>=1 && rnd(60*dmg_act)==0 && unit[m].used==USA )
								fire_now(m,0,RAS);

							// 対空機銃 自動
							if( rnd(10*dmg_act)==0  )
								fire_now(m,0,BLT);
							// 魚雷
							if( unit[m].rnd_40[0]==cc_count%(40*dmg_act) && unit[m].arm[1]>=1 && unit[m].arm[3]<=0 && unit[m].used==JPN )
								{
								fire_now(m,0,TPD);
								}
							}
						else
							{
							// 防空巡洋艦
							// 艦砲 自動
							if( unit[m].arm[1]>=1  && rnd(800*dmg_act)==0  && (unit[m].arm[2]==0 || unit[unit[m].arm[2]].ctgry==SHIP) )
								{
								fire_now(m,0,GUN);
								}


							// 対空砲弾
							if( unit[m].arm[1]>=1 && rnd(100*dmg_act)==0 )
								{
								// 自動
								fire_now(m,0,SHL);							// 対空砲弾 自動砲撃
								}

							if( unit[m].arm[1]>=1 && rnd(180*dmg_act)==0 && unit[m].arm[2]!=0 )
								{
								// 選択
								fire_now(m,unit[m].arm[2],SHL);
								}


							// 対空機関砲
							if( unit[m].arm[1]>=1 && rnd(55*dmg_act)==0 && unit[m].used==USA )
								fire_now(m,0,RAS);

							// 対空機銃 自動
							if( rnd(10*dmg_act)==0  )
								fire_now(m,0,BLT);

							}
						break;


					case CV1:
						// 対空機関砲 
						if( unit[m].arm[1]>=1 && rnd(80*dmg_act)==0/*unit[m].rnd_80[0]==cc_count%(80*dmg_act)*/ && unit[m].used==USA )
							fire_now(m,0,RAS);
						// 対空機銃
						if( rnd(15*dmg_act)==0/*unit[m].rnd_10[0]==cc_count%(10*dmg_act)*/)
							fire_now(m,0,BLT);
						break;

					case DD1:
						if( unit[m].type==0 )
							{
							// 艦砲 自動
							if( unit[m].arm[1]>=1 && rnd(200*dmg_act)==0/*unit[m].rnd_200[0]==cc_count%(200*dmg_act)*/ )
								{
								fire_now(m,0,GUN);
								}

							// 魚雷
							if( unit[m].rnd_40[0]==cc_count%(40*dmg_act) && unit[m].arm[1]>=1 && unit[m].arm[3]<=0 )
								{
								fire_now(m,0,TPD);
								}

							// 爆雷
							if( unit[m].arm[1]>=1 && (cc_count%(35*dmg_act))==0 )
								{
								fire_now(m,0,ASB);
								}
							// 対空機銃
							if( rnd(15*dmg_act)==0/*unit[m].rnd_20[0]==cc_count%(20*dmg_act)*/ )
								fire_now(m,0,BLT);
							}
						else
							{
							// 艦砲 自動
							if( unit[m].arm[1]>=1 && rnd(500*dmg_act)==0 )
								{
								fire_now(m,0,GUN);
								}

							// 対空機銃
							if( rnd(20*dmg_act)==0 )
								fire_now(m,0,BLT);


							// 爆雷
							if( unit[m].arm[1]>=1 && (cc_count%(25*dmg_act))==0 )
								{
								fire_now(m,0,ASB);
								}

							}
						break;

					case CVL1:
						// 対空機関砲
						if( unit[m].arm[1]>=1 && rnd(100*dmg_act)==0/*unit[m].rnd_100[0]==cc_count%(100*dmg_act)*/ && unit[m].used==USA )
							fire_now(m,0,RAS);
						// 対空機銃
						if( rnd(20*dmg_act)==0/*unit[m].rnd_30[0]==cc_count%(30*dmg_act)*/ )
							fire_now(m,0,BLT);
						break;



					case TR1:
						// トランスボーと
						if( unit[m].arm[0]==TR_SP && unit[m].arm[1]>=1  && unit[m].arm[2]==max_unit+1)
							fire_now(m,0,TR_SP);
						if( unit[m].arm[0]==TR_AP && unit[m].arm[1]>=1  && unit[m].arm[2]==max_unit+1)
							fire_now(m,0,TR_AP);

						if( unit[m].arm[0]==TR_GF1 && unit[m].arm[1]>=1  && unit[m].arm[2]==max_unit+1)
							fire_now(m,0,TR_GF1);
			
						if( unit[m].arm[0]==TR_GF2 && unit[m].arm[1]>=1  && unit[m].arm[2]==max_unit+1)
							fire_now(m,0,TR_GF2);
						if( unit[m].arm[0]==TR_GF3 && unit[m].arm[1]>=1  && unit[m].arm[2]==max_unit+1)
							fire_now(m,0,TR_GF3);

						break;



					case SS1:
						// 艦砲 自動
						// 魚雷
						if( unit[m].arm[1]>=1 && unit[m].arm[3]<=0 && unit[m].arm[2]!=0 /*&& unit[m].rnd_20[0]==cc_count%(20)*/ )
							{
							fire_now(m,unit[m].arm[2],TPD);
							}
						break;


					case FT1:
						// 戦闘機の場合は、前方に敵航空機が飛んでればとりあえず撃つ
						if( unit[m].arm[1]>=1  && ((cc_count+unit[m].rnd_20[0])% (10-(unit[m].type==1 ? 1 : 0)*3 ) )==0 )
							fire_now(m,0,BLT);
						break;


					case AT1:
						// 攻撃機の場合は、後方に敵航空機が飛んでればとりあえず撃つ
						if(rnd(35*dmg_act)==0)
							fire_now(m,0,BLT);
						if( unit[m].arm[1]>=1  && unit[m].arm[0]==TPD && unit[m].arm[2]!=0 && unit[m].drctn_add==0 && unit[m].spd>=unit[m].max_spd )
							fire_now(m,unit[m].arm[2],TPD);
						if( unit[m].arm[1]>=1  && unit[m].arm[0]==BOM && unit[m].arm[2]!=0  )
							fire_now(m,unit[m].arm[2],BOM);
						break;


					case BM1:
						if(rnd(20*dmg_act)==0)
							fire_now(m,0,BLT);
						if( unit[m].arm[1]>=1  && unit[m].arm[0]==TPD && unit[m].arm[2]!=0 && unit[m].drctn_add==0 && unit[m].spd>=unit[m].max_spd )
							fire_now(m,unit[m].arm[2],TPD);
						if( unit[m].arm[1]>=1  && unit[m].arm[0]==BOM && unit[m].arm[3]==0 && unit[m].drctn_add==0 /*&& !(cc_count%10)*/ )	
							fire_now(m,unit[m].arm[2],BOM);
						break;

					}
				}
			}


		if( unit[m].used!=0 )
			{
			// そのユニットの発するエフェクト
			cont_unit_effect( m );
			}




		// 潜水艦から聞こえれる探知音
		if( unit[m].used==your_side && unit[m].kind==SS1 && unit[m].info[6]!=0 && game_end==0 && unit[m].spry==0 )
			{
			dstc=500;
			for( i=1; i<=max_unit; i++)
				{
				if( unit[i].used!=0 && unit[i].used!=your_side && unit[i].kind==DD1 && unit[m].spry==0 )
					{
					wrk_x=unit[m].x-unit[i].x;
					wrk_y=unit[m].y-unit[i].y;

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

					wrk_x2=((wrk_x)/(cos(drctn*a_PI)));
					if( dstc>wrk_x2 )
						{
						dstc=wrk_x2;
						}
					}
				}


			if( map_edit==0 && dstc<=400 )
				{
				if( (my_rnd(3+(int)(dstc/5)))==0  )
				SoundPlayEffect( 0, SNR ,unit[m].x, unit[m].y);
				}
			}
		}


	// ｆｉｒｅの制御
	cont_fire( );


	}
}
