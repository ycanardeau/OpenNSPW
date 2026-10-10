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
[Original("chara_cont")]
public void	UpdateBattle()
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







	if( Mode!=GameMode.Battle )
		return;	


#if true
	if( IsEditingMap==0 )
		{
		/* 入力フェーズ */
		// 通信対戦時
		if((Tick%(TurnLength))==SyncTick1 )
			{
			if(  CONN_DBG==0 && CanAdvance1==0 )
				{
				return;
				}

			if( Result==GameResult.None)
				{
				BattleTime++;

				if( SwapTime!=0 )
					{
					if( ( SwapRule==0 && BattleTime==(SwapTime*100) ) || ( SwapRule==1 && (BattleTime%(SwapTime*100))==(SwapTime*100)-1 ) )
						{
						// １ゲーム中１回だけ交代
DebugValues[0]++;
						s=SupplyRates[1];
						SupplyRates[1]=SupplyRates[0];
						SupplyRates[0]=s;
						}
					}
				}



			if(UnitChecksums[1]==UnitChecksums[0])
				{
				AreUnitsOutOfSync=0;
				}
			else
				{
				AreUnitsOutOfSync=1;
				if( HasSavedDesync==0 )
					{
					HasSavedDesync=1;
#if !CONN_DBG
					SaveResume(2);
#endif
					}
				}



			if(TickChecksums[1]==TickChecksums[0])
				{
				IsTickOutOfSync=0;
				}
			else
				{
				IsTickOutOfSync=1;
				if( HasSavedDesync==0 )
					{
					HasSavedDesync=1;
#if !CONN_DBG
					SaveResume(2);
#endif
					}
				}



			if(RandomChecksums[1]==RandomChecksums[0])
				{
				IsRandomOutOfSync=0;
				}
			else
				{
				IsRandomOutOfSync=1;
				if( HasSavedDesync==0 )
					{
					HasSavedDesync=1;
#if !CONN_DBG
					SaveResume(2);
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




			TickWaits[0]=0;
			TickWaits[1]=0;

			CanAdvance1=0;


			if( BufferedMoveOrders[1].Unit==0 && BufferedSelectOrders[1].IsSet==0 && BufferedMenuOrders[1].Menu==CombatMenuItem.None && BufferedSystemOrders[1]==0 && BufferedArrivedUnits[1]==0 )
				{
				// 命令が無い場合。
				dp_flag.dwType = MessageType.NoOrder;
//t				hr=lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_flag, sizeof(_DP_FLAG) );

				bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
				bufferDesc.pBufferData  = (byte*) &dp_flag;
				g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );


				}
			else
				{
				// まず、このフェーズで溜めた、命令をセンドする。
				if( BufferedSystemOrders[1]!=0 )
					{
					dp_flag.dwType = (MessageType)BufferedSystemOrders[1];
//t					hr=lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_flag, sizeof(_DP_FLAG) );

					bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
					bufferDesc.pBufferData  = (byte*) &dp_flag;
					g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );


					SystemOrders[1]=BufferedSystemOrders[1];
					}
				else if( BufferedArrivedUnits[1]!=0 )
					{
					dp_data_1.dwType = MessageType.UnitArrived;
					dp_data_1.data[0] = BufferedArrivedUnits[1];
//t					lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer,DPSEND_GUARANTEED , &dp_data_1, sizeof(DP_DATA_1) );
					bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
					bufferDesc.pBufferData  = (byte*) &dp_data_1;
					g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
					}
				else if(BufferedMoveOrders[1].Unit!=0)
					{
					// 移動
					ship=0;
					plane=0;
					for( s=0; s<=JPN_SHIP_END-1; s++)
						{
						ship+=BufferedSelections[1][s];
						}
					for( s=JPN_SHIP_END; s<=(USA_PLANE_END/2)-1; s++)
						{
						plane+=BufferedSelections[1][s];
						}

					if(ship!=0 && plane!=0)
						{
						// 航空機も艦船もある
						dp_new_pp.dwType = MessageType.MoveOrder;
						dp_new_pp.used=(byte)BufferedMoveOrders[1].Unit;
						dp_new_pp.x=(short)BufferedMoveOrders[1].Destination.X;
						dp_new_pp.y=(short)BufferedMoveOrders[1].Destination.Y;
						dp_new_pp.cls=BufferedMoveOrders[1].ClearsPath;
						for( s=0; s<=(USA_PLANE_END/2)-1; s++)
							{
							dp_new_pp.slct_unit[s]=(byte)BufferedSelections[1][s];
							}
//t						hr = lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_new_pp, sizeof(_DP_NEW_PP) );
						bufferDesc.dwBufferSize = (uint)(sizeof(_DP_NEW_PP));
						bufferDesc.pBufferData  = (byte*) &dp_new_pp;
						g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

						}
					else if( ship!=0 && plane==0 )
						{
						// 艦船のみ
						dp_new_pp_ship.dwType = MessageType.MoveShipsOrder;
						dp_new_pp_ship.used=(byte)BufferedMoveOrders[1].Unit;
						dp_new_pp_ship.x=(short)BufferedMoveOrders[1].Destination.X;
						dp_new_pp_ship.y=(short)BufferedMoveOrders[1].Destination.Y;
						dp_new_pp_ship.cls=BufferedMoveOrders[1].ClearsPath;
						for( s=0; s<=JPN_SHIP_END-1; s++)
							{
							dp_new_pp_ship.slct_unit[s]=(byte)BufferedSelections[1][s];
							}
//t						hr = lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_new_pp_ship, sizeof(_DP_NEW_PP_SHIP) );
						bufferDesc.dwBufferSize = (uint)(sizeof(_DP_NEW_PP_SHIP));
						bufferDesc.pBufferData  = (byte*) &dp_new_pp_ship;
						g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
						}
					else if( ship==0 && plane!=0 )
						{
						// 航空機のみ
						dp_new_pp_plane.dwType = MessageType.MovePlanesOrder;
						dp_new_pp_plane.used=(byte)BufferedMoveOrders[1].Unit;
						dp_new_pp_plane.x=(short)BufferedMoveOrders[1].Destination.X;
						dp_new_pp_plane.y=(short)BufferedMoveOrders[1].Destination.Y;
						dp_new_pp_plane.cls=BufferedMoveOrders[1].ClearsPath;
						for( s=0; s<=(JPN_PLANE_END-JPN_PLANE_START); s++)
							{
							dp_new_pp_plane.slct_unit[s]=(byte)BufferedSelections[1][s+JPN_SHIP_END];
							}
//t						hr = lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_new_pp_plane, sizeof(_DP_NEW_PP_PLANE) );
						bufferDesc.dwBufferSize = (uint)(sizeof(_DP_NEW_PP_PLANE));
						bufferDesc.pBufferData  = (byte*) &dp_new_pp_plane;
						g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
						}
					}
				else if(BufferedSelectOrders[1].IsSet!=0)
					{
					// 目標指定
					ship=0;
					plane=0;
					for( s=0; s<=JPN_SHIP_END-1; s++)
						{
						ship+=BufferedSelections[1][s];
						}
					for( s=JPN_SHIP_END; s<=(USA_PLANE_END/2)-1; s++)
						{
						plane+=BufferedSelections[1][s];
						}


					if(ship!=0 && plane!=0)
						{
						// 航空機も艦船もある
						dp_new_slct.dwType = MessageType.SelectOrder;
						dp_new_slct.sw=BufferedSelectOrders[1].IsSet;
						dp_new_slct.the_slct_unit=(byte)BufferedSelectOrders[1].SelectedUnit;
						dp_new_slct.m=(byte)BufferedSelectOrders[1].Unit;
						dp_new_slct.gr_x=(short)BufferedSelectOrders[1].GroundPosition.X;
						dp_new_slct.gr_y=(short)BufferedSelectOrders[1].GroundPosition.Y;
						for( s=0; s<=(USA_PLANE_END/2)-1; s++)
							{
							dp_new_slct.slct_unit[s]=(byte)BufferedSelections[1][s];
							}
//t						hr = lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_new_slct, sizeof(_DP_NEW_SLCT) );
						bufferDesc.dwBufferSize = (uint)(sizeof(_DP_NEW_SLCT));
						bufferDesc.pBufferData  = (byte*) &dp_new_slct;
						g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
						}
					else if( ship!=0 && plane==0 )
						{
						// 艦船のみ
						dp_new_slct_ship.dwType = MessageType.SelectShipsOrder;
						dp_new_slct_ship.sw=BufferedSelectOrders[1].IsSet;
						dp_new_slct_ship.the_slct_unit=(byte)BufferedSelectOrders[1].SelectedUnit;
						dp_new_slct_ship.m=(byte)BufferedSelectOrders[1].Unit;
						dp_new_slct_ship.gr_x=(short)BufferedSelectOrders[1].GroundPosition.X;
						dp_new_slct_ship.gr_y=(short)BufferedSelectOrders[1].GroundPosition.Y;
						for( s=0; s<=JPN_SHIP_END-1; s++)
							{
							dp_new_slct_ship.slct_unit[s]=(byte)BufferedSelections[1][s];
							}
//t						hr = lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_new_slct_ship, sizeof(_DP_NEW_SLCT_SHIP) );
						bufferDesc.dwBufferSize = (uint)(sizeof(_DP_NEW_SLCT_SHIP));
						bufferDesc.pBufferData  = (byte*) &dp_new_slct_ship;
						g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1, 0, null, ref hAsync, MUST_SEND );
						}
					else if( ship==0 && plane!=0 )
						{
						// 航空機のみ
						dp_new_slct_plane.dwType = MessageType.SelectPlanesOrder;
						dp_new_slct_plane.sw=BufferedSelectOrders[1].IsSet;
						dp_new_slct_plane.the_slct_unit=(byte)BufferedSelectOrders[1].SelectedUnit;
						dp_new_slct_plane.m=(byte)BufferedSelectOrders[1].Unit;
						dp_new_slct_plane.gr_x=(short)BufferedSelectOrders[1].GroundPosition.X;
						dp_new_slct_plane.gr_y=(short)BufferedSelectOrders[1].GroundPosition.Y;
						for( s=0; s<=JPN_PLANE_END-JPN_PLANE_START; s++)
							{
							dp_new_slct_plane.slct_unit[s]=(byte)BufferedSelections[1][s+JPN_SHIP_END];
							}
//t						hr = lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_new_slct_plane, sizeof(_DP_NEW_SLCT_PLANE) );
						bufferDesc.dwBufferSize = (uint)(sizeof(_DP_NEW_SLCT_PLANE));
						bufferDesc.pBufferData  = (byte*) &dp_new_slct_plane;
						g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
						}
					else if( ship==0 && plane==0 )
						{
						// ユニットに対する指定無し。おそらく輸送船の揚陸先
						dp_new_slct_land.dwType = MessageType.SelectLandOrder;
						dp_new_slct_land.sw=BufferedSelectOrders[1].IsSet;
						dp_new_slct_land.the_slct_unit=(byte)BufferedSelectOrders[1].SelectedUnit;
						dp_new_slct_land.m=(byte)BufferedSelectOrders[1].Unit;
						dp_new_slct_land.gr_x=(short)BufferedSelectOrders[1].GroundPosition.X;
						dp_new_slct_land.gr_y=(short)BufferedSelectOrders[1].GroundPosition.Y;
//t						hr = lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_new_slct_land, sizeof(_DP_NEW_SLCT_LAND) );
						bufferDesc.dwBufferSize = (uint)(sizeof(_DP_NEW_SLCT_LAND));
						bufferDesc.pBufferData  = (byte*) &dp_new_slct_land;
						g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
						}
					}
				else if(BufferedMenuOrders[1].Menu!=CombatMenuItem.None)
					{
					// メニュー
					dp_new_menu.dwType = MessageType.MenuOrder;

					dp_new_menu.menu=(byte)BufferedMenuOrders[1].Menu;
					dp_new_menu.the_slct_unit=(byte)BufferedMenuOrders[1].SelectedUnit;

					for( s=0; s<=(USA_PLANE_END/2)-1; s++)
						{
						dp_new_menu.slct_unit[s]=(byte)BufferedSelections[1][s];
						}
//t					hr = lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_new_menu, sizeof(_DP_NEW_MENU) );
					bufferDesc.dwBufferSize = (uint)(sizeof(_DP_NEW_MENU));
					bufferDesc.pBufferData  = (byte*) &dp_new_menu;
					g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
					}	
				}
			CanOrder=0;
			}



		/* 受信と命令発フェイズ */
//cnct_loop_pt2=2;




		if((Tick%TurnLength)==(SyncTick2) )
			{
			if( CONN_DBG==0/*1*/ && CanAdvance2==0)
				{
				if(TickWaits[1]<99)
					TickWaits[1]++;
				return;
				}

			CanAdvance2=0;


			if( IsEditingMap==0 && Result==GameResult.None && ActivePlayerCount==2 )
				{
				AutoSaveTime++;
#if CONN_DBG
				if(auto_save_time>=90 )
#else
				if(AutoSaveTime>=90 && HasSavedDesync==0 )
#endif
					{
					AutoSaveTime=0;
#if !CONN_DBG
					SaveResume(3);
#endif
					}
				}


			if( SystemOrders[0]!=0 || SystemOrders[1]!=0 )
				{
				switch( (MessageType)SystemOrders[1] )
					{
					case MessageType.GoToGameSetting:
						GoToGameSetting();
						break;
					case MessageType.ResumeAndGoToGameSetting:
						SaveResume(1);
						GoToGameSetting();

						if( IsHost!=0 && WasHost==0 )
							WasHost=1;

						break;
					}
				}



			if( LocalSide==Side.Japan )
				{
				if(BufferedArrivedUnits[0]!=0)
					{
					OnUnitArrived( 0, BufferedArrivedUnits[0] );		// 敵サイドが１ユニット増える
					}
				if(BufferedArrivedUnits[1]!=0)
					{
					OnUnitArrived( 1, BufferedArrivedUnits[1] );		// 自サイドが１ユニット増える
					}
				}
			else
				{
				if(BufferedArrivedUnits[1]!=0)
					{
					OnUnitArrived( 1, BufferedArrivedUnits[1] );		// 自サイドが１ユニット増える
					}
				if(BufferedArrivedUnits[0]!=0)
					{
					OnUnitArrived( 0, BufferedArrivedUnits[0] );		// 敵サイドが１ユニット増える
					}
				}




			bf_the_slct_unit=SelectedUnit;
			for( s=0; s<=MaxUnitId; s++)
				{
				bf_2_slct_unit[s]=Selections[1][s];
				Selections[0][s]=0;
				Selections[1][s]=0;
				}


			// 敵味方両方の命令データをここで入力する。
			for(e=0; e<=1; e++)
				{
				MoveOrders[e].Unit=BufferedMoveOrders[e].Unit;
				MoveOrders[e].Destination = new WorldPosition((double)BufferedMoveOrders[e].Destination.X, MoveOrders[e].Destination.Y);
				MoveOrders[e].Destination = new WorldPosition(MoveOrders[e].Destination.X, (double)BufferedMoveOrders[e].Destination.Y);
				MoveOrders[e].ClearsPath=BufferedMoveOrders[e].ClearsPath;

				SelectOrders[e].IsSet=BufferedSelectOrders[e].IsSet;
				SelectOrders[e].SelectedUnit=BufferedSelectOrders[e].SelectedUnit;
				SelectOrders[e].Unit=BufferedSelectOrders[e].Unit;
				SelectOrders[e].GroundPosition = new WorldPosition((short)BufferedSelectOrders[e].GroundPosition.X, SelectOrders[e].GroundPosition.Y);
				SelectOrders[e].GroundPosition = new WorldPosition(SelectOrders[e].GroundPosition.X, (short)BufferedSelectOrders[e].GroundPosition.Y);
				}



			e=0;
			if(LocalSide!=Side.Japan)
				{
				// 日本海軍サイド
//				for(s=0;s<=19;s++)
				for(s=0;s<=JPN_SHIP_END-1;s++)
					{
					// 水上ユニット
					Selections[e][s+1]=BufferedSelections[e][s];
					}
//				for(s=20;s<=49;s++)
				for(s=JPN_SHIP_END;s<=(USA_PLANE_END/2)-1;s++)
					{
					// 航空ユニット
					Selections[e][s+JPN_SHIP_END/*20*/+1]=BufferedSelections[e][s];
					}
				}
			else
				{
				// 合衆国海軍サイド
//				for(s=0;s<=19;s++)
				for(s=0;s<=JPN_SHIP_END-1;s++)
					{
					// 水上ユニット
					Selections[e][s+JPN_SHIP_END/*20*/+1]=BufferedSelections[e][s];
					}
//				for(s=20;s<=49;s++)
				for(s=JPN_SHIP_END;s<=(USA_PLANE_END/2)-1;s++)
					{
					// 航空ユニット
					Selections[e][s+(USA_PLANE_END/2)/*50*/+1]=BufferedSelections[e][s];
					}
				}

			e=1;
			if(LocalSide==Side.Japan)
				{
				// 日本海軍サイド
//				for(s=0;s<=19;s++)
				for(s=0;s<=JPN_SHIP_END-1;s++)
					{
					// 水上ユニット
					Selections[e][s+1]=BufferedSelections[e][s];
					}
//				for(s=20;s<=49;s++)
				for(s=JPN_SHIP_END;s<=(USA_PLANE_END/2)-1;s++)
					{
					// 航空ユニット
					Selections[e][s+JPN_SHIP_END/*20*/+1]=BufferedSelections[e][s];
					}
				}
			else
				{
				// 合衆国海軍サイド
//				for(s=0;s<=19;s++)
				for(s=0;s<=JPN_SHIP_END-1;s++)
					{
					// 水上ユニット
					Selections[e][s+JPN_SHIP_END/*20*/+1]=BufferedSelections[e][s];
					}
//				for(s=20;s<=49;s++)
				for(s=JPN_SHIP_END;s<=(USA_PLANE_END/2)-1;s++)
					{
					// 航空ユニット
					Selections[e][s+(USA_PLANE_END/2)/*50*/+1]=BufferedSelections[e][s];
					}
				}



			//
			ApplyOrders();
			ApplyUnitInfoInput();

			//
			for( s=0; s<=MaxUnitId; s++)
				{
				Selections[1][s]=bf_2_slct_unit[s];
				}
			SelectedUnit=bf_the_slct_unit;


			BufferedMoveOrders[0].Unit=0;						// クリア
			BufferedMoveOrders[1].Unit=0;						// クリア

			BufferedSelectOrders[0].IsSet=0;						// クリア
			BufferedSelectOrders[1].IsSet=0;						// クリア

			BufferedMenuOrders[0].Menu=CombatMenuItem.None;
			BufferedMenuOrders[1].Menu=CombatMenuItem.None;
	
			BufferedSystemOrders[0]=0;
			BufferedSystemOrders[1]=0;

			SystemOrders[0]=0;
			SystemOrders[1]=0;

			BufferedArrivedUnits[0]=0;						// クリア
			BufferedArrivedUnits[1]=0;						// クリア



//			for(s=0;s<=49;s++)
			for(s=0;s<=(USA_PLANE_END/2)-1;s++)
				{
				BufferedSelections[0][s]=0;
				BufferedSelections[1][s]=0;
				}

			CanOrder=1;
			HasOrdered=0;



#if false && CONN_DBG
dbg[5]=rnd(100);
#endif
			dp_flag.dwType = MessageType.SyncFlag;

			// プログラム同期エラーチェックの為
//			dp_flag.cc_chk=(BYTE)1024;
//			bf_cc_count[1]=(BYTE)1024;
			dp_flag.cc_chk=(byte)Tick;
			TickChecksums[1]=(byte)Tick;


			// 座標のずれチェックの為
			f=0;
			for(m=1; m<=MaxUnitId; m++)
				{
				if( Units[m].Side!=0 && !(Units[m].Category==UnitCategory.Plane && Units[m].PlaneState==UnitState.Parked) )
//				if( unit[m].used && /*unit[m].ctgry==PLANE &&*/ unit[m].hp[0]  )
					{
					f+=(int)((Units[m].Position.X+Units[m].Position.Y+Units[m].Direction)*10000);
					}
				}


			dp_flag.unit_chk=(byte)f;
			UnitChecksums[1]=(byte)f;


			// ランダム同期エラーチェックのため
			dp_flag.rnd_chk=(byte)RandomCount;
			RandomChecksums[1]=(byte)RandomCount;

			dp_flag.ccc_wait_chk=TickWaits[1];

			dp_flag.rival_mode=(short)Mode;

//t			hr=lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_flag, sizeof(_DP_FLAG) );
			bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
			bufferDesc.pBufferData  = (byte*) &dp_flag;
			g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
			}
		}
#endif





	Detect();

	Tick++;



	if( Result!=GameResult.None || Mode!=GameMode.Battle )
		return;	



	// 雲を制御します
	UpdateClouds( );



	// エフェクトのデクリ
	for( m=1; m<EFFECT_MAX/*255*/; m++)
		{
		if( Effects[m].Layer==EffectLayer.Upper || Effects[m].Layer==EffectLayer.Lower)
			{
			Effects[m].info[0]--;
			if( Effects[m].info[0]==0 )
				Effects[m].Layer=EffectLayer.None;
			}
		}



	// 補給値のインクリ
	if( (Tick%300)==0 )
		{
		if( IsHost!=0 )
			{
			SupplyPoints+=SupplyRates[0];
			}
		else
			{
			SupplyPoints+=SupplyRates[1];
			}
		if(SupplyPoints>9999)
			SupplyPoints=9999;
		}





	for(m=1;m<=MaxUnitId;m++)
		{


		// 陸上施設の工事処理
		if( Units[m].Side!=0 && ( Units[m].Kind==UnitKind.AirBase||Units[m].Kind==UnitKind.NavalBase||Units[m].Kind==UnitKind.InfantryBase||Units[m].Kind==UnitKind.Pillboxes||Units[m].Kind==UnitKind.Fortress ) && Units[m].info[0]!=0
			)
			{
			if( Units[m].Hp<Units[m].MaxHp )
				{
				// 損傷してればその修理が先。
				if( (Tick%220/*80*/)==0 && Units[m].Fuel>=0 )
					Units[m].Hp++;
				}
			else
				{
				// 損傷がなければ工事
				Units[m].info[0]--;

				if( Units[m].info[0]==0 )
					{
					// 完成
					Units[m].MaxHp*=4;
					Units[m].Hp=Units[m].MaxHp;
					}
				}
			}



		if( IsEditingMap==0 && Units[m].Side!=0 && !(Units[m].Kind==UnitKind.AirBase||Units[m].Kind==UnitKind.NavalBase||Units[m].Kind==UnitKind.City))
			{
			// 補給先がちゃんとあるか
			if( Units[m].Category==UnitCategory.Ship && Units[m].Supply!=0 && Units[m].Fuel>=0 )
				{
				// 補給中の艦船
				// ptin dbg
				wrk_rect.top=(int)Units[m].Position.Y+40+240;  //(int)unit[m].y-40-240;    
				wrk_rect.right=(int)Units[m].Position.X+40+240;
				wrk_rect.bottom=(int)Units[m].Position.Y-40-240;    //(int)unit[m].y+40+240; 
				wrk_rect.left=(int)Units[m].Position.X-40-240;
				
				flg=0;
				for( i=1; i<=MaxUnitId; i++)
					{
					if( Units[i].Side==Units[m].Side && Units[i].Kind==UnitKind.NavalBase && Units[i].info[0]==0 )
						{
						if( PointInRect3( ref wrk_rect, (int)Units[i].Position.X, (int)Units[i].Position.Y )!=0 )
							{
							flg=1;
							break;
							}
						}
					}

				if( flg==0 )
					Units[m].Supply=0;
				}


			// ユニットの補給			
			if( Units[m].Supply!=0 && Units[m].Fuel>=0 )
				{
				Units[m].Supply++;

				if( Units[m].Hp<Units[m].MaxHp && (Units[m].Supply%200)==199 && Units[m].Fuel>=0 )
					{ Units[m].Hp++; Units[m].Supply=1;}

				if( Units[m].Hp==Units[m].MaxHp && Units[m].Ammo<Units[m].MaxAmmo && (Units[m].Supply%10)==9 )
					{ Units[m].Ammo++; Units[m].Supply=1; }

				if( Units[m].Hp==Units[m].MaxHp && Units[m].Ammo==Units[m].MaxAmmo && Units[m].Fuel<100 && (Units[m].Supply%20)==19 )
					{
					Units[m].Fuel++;
					if(Units[m].Fuel>100)
						Units[m].Fuel=100;
					Units[m].Supply=1; 
					}

				if( Units[m].Hp==Units[m].MaxHp && Units[m].Ammo==Units[m].MaxAmmo && Units[m].Fuel==100 && (Units[m].Supply%100)==99 )
					{
					Units[m].Supply=0;
					}
				}

			//=========		 ユニットの機動制御		=========//

			if(Units[m].Category==UnitCategory.Plane && Units[m].PlaneState==UnitState.Parked)
				{
				// パーキング中の航空機へ
				if( Units[m].Stop!=0 )
					{
					Units[m].PathX[0]=Units[Units[m].info[1]].Position.X;
					Units[m].PathY[0]=Units[Units[m].info[1]].Position.Y;
					}
				else
					{
					if( Units[m].Mode<=UnitMode.Slow  )
						{
						// 発進
						if( Units[m].info[3]==0)
							{
							Units[m].info[3]=1;
							Units[Units[m].info[1]].info[4]++;		// 発艦予定の機数を
							}
						cv_1=Sprites[UNIT_INFO_JPN].x+Sprites[UNIT_INFO_JPN].wd/2;
						if((int)Units[m].Position.X==cv_1 && Units[m].info[3]==1)
							{	
							Units[m].info[3]=2;
							Units[m].Direction=270.0;
							}
						cv_1=Sprites[UNIT_INFO_JPN].y+370;
						if((int)Units[m].Position.Y>=cv_1 && Units[m].info[3]==2)
							{
							Units[m].info[3]=3;
							Units[m].Direction=90.0;

							Units[Units[m].info[1]].info[11]++;
							Units[Units[m].info[1]].info[11]&=0xffff;	

							if( (Units[Units[m].info[1]].info[11]%2)!=0 )
								{
								Units[m].Position = new WorldPosition(Units[m].Position.X + 15, Units[m].Position.Y);
								}
							else
								{
								Units[m].Position = new WorldPosition(Units[m].Position.X - 15, Units[m].Position.Y);
								}
							}
						cv_1=Sprites[UNIT_INFO_JPN].y+370-80;
						if((int)Units[m].Position.Y==cv_1 && Units[m].info[3]==3 )
							{
							Units[m].info[3]=4;
							if( Units[m].Kind!=UnitKind.Bomber && !(Units[m].Kind==UnitKind.Fighter&&Units[m].Variant==1) )
								Units[m].SpriteRow--;
							Units[m].info[4]=0;
							}



						cv_1=Sprites[UNIT_INFO_JPN].y+370-120;
						if((int)Units[m].Position.Y<=cv_1 && Units[m].info[3]>=4)
							{	// 加速します
							Units[m].info[4]+=1;

							if( Units[m].info[4]==40 && Units[m].info[1]==UnitInfoPanel[3] &&  ((UnitKind)UnitInfoPanel[0]==UnitKind.Carrier || (UnitKind)UnitInfoPanel[0]==UnitKind.LightCarrier || (UnitKind)UnitInfoPanel[0]==UnitKind.AirBase ) )
								{
								PlaySoundEffect( 0, TAKE_OFF,(double)(MAP_RIGHT+1), 0);
								}

							Units[m].Position = new WorldPosition(Units[m].Position.X + (cos(Units[m].Direction*a_PI)*(Units[m].info[4]/20)), Units[m].Position.Y);
							Units[m].Position = new WorldPosition(Units[m].Position.X, Units[m].Position.Y - (sin(Units[m].Direction*a_PI)*(Units[m].info[4]/20)));
							}

						cv_1=Sprites[UNIT_INFO_JPN].y-30/*+60*/;
						if((int)Units[m].Position.Y<=cv_1 && Units[m].info[3]>=4 )
							{		// ここで発進はお終い。
							//unit[m].info[3]=100;			// 発進後の最低直線飛行
							Units[m].info[3]=0;			// 発進後の最低直線飛行
							Units[m].PlaneState=UnitState.Flying;
							//unit[m].info[2]=-1;	// 格納庫の位置、及び、その基地の番機番号
							Units[m].Position = new WorldPosition(Units[Units[m].info[1]].Position.X, Units[Units[m].info[1]].Position.Y);
							//unit[m].drctn=unit[unit[m].info[1]].drctn;
							Units[m].Speed=1.0;
							if(Units[Units[m].info[1]].info[4]!=0)
								Units[Units[m].info[1]].info[4]--;		// 発艦予定の機数を減らす。
							Units[Units[m].info[1]].info[1]--;		// 現在格納数
							Units[Units[m].info[1]].info[7]=0;		// その空母の次機着艦許可
								// 発進した場合、最初のポイントは空母の方向から決める。
							switch((int)(Units[Units[m].info[1]].Direction/22.5))
								{
								case 0: case 15:
									Units[m].Direction=0.0;
									break;
								case 1: case 2:
									Units[m].Direction=45.0;
									break;
								case 3: case 4:
									Units[m].Direction=90.0;
									break;
								case 5: case 6:
									Units[m].Direction=90.0+45.0;
									break;
								case 7: case 8:
									Units[m].Direction=180.0;
									break;
								case 9: case 10:
									Units[m].Direction=180.0+45.0;
									break;
								case 11: case 12:
									Units[m].Direction=270.0;
									break;
								case 13: case 14:
									Units[m].Direction=270.0+45.0;
									break;
								}
							}
						if(Units[m].PlaneState==UnitState.Parked)
							{
							Units[m].Position = new WorldPosition(Units[m].Position.X + (cos(Units[m].Direction*a_PI)*0.8), Units[m].Position.Y);
							Units[m].Position = new WorldPosition(Units[m].Position.X, Units[m].Position.Y - (sin(Units[m].Direction*a_PI)*0.8));
							}
						}
					else
						{
						// 着陸
						cv_1=Sprites[UNIT_INFO_JPN].y+Sprites[UNIT_INFO_JPN].ht-150;
						if((int)Units[m].Position.Y<=cv_1 && Units[m].info[3]==1)
							{
							Units[m].info[3]=2;
							Units[m].Speed=1.5;
							Units[Units[m].info[1]].info[7]=0;		// その空母の次機着艦許可
							}
						cv_1=Sprites[UNIT_INFO_JPN].y+Sprites[UNIT_INFO_JPN].ht-200;
						if((int)Units[m].Position.Y<=cv_1 && Units[m].info[3]==2)
							{
							Units[m].info[3]=3;
							Units[m].Speed=Units[m].Speed/2;
							if( Units[m].Kind!=UnitKind.Bomber && !(Units[m].Kind==UnitKind.Fighter&&Units[m].Variant==1) )
								Units[m].SpriteRow++;
							}
						cv_1=Sprites[UNIT_INFO_JPN].y+Sprites[UNIT_INFO_JPN].ht-270;
						if( (int)Units[m].Position.Y<=cv_1 && Units[m].info[3]==3 )
							{
							// 着艦終了
							Units[m].info[3]=0;

							Units[m].info[9]=0;					// 戦闘機の場合は制空出撃フラグ

							Units[m].Mode=UnitMode.Move;				// モード（コンバットメニュー）
							SetParkingPosition(m);
							Units[m].Stop=1;

							Units[m].Weapon=FireKind.Maintenance;					// 武装品種
							Units[m].Ammo=1;						// 数
							Units[m].ReloadTime=TUNE_SPAN+(Units[m].MaxHp-Units[m].Hp)*(TUNE_SPAN/10)+((Units[m].Kind==UnitKind.Bomber ? 1 : 0)*(TUNE_SPAN/3));		// 数
							Units[m].Hp=Units[m].MaxHp;

							Units[m].PathX[0]=Units[Units[m].info[1]].Position.X;
							Units[m].PathY[0]=Units[Units[m].info[1]].Position.Y;
							Units[m].PathX[1]=MAP_RIGHT+1;

							f=0;
/***
							for( i=0; i<=max_unit; i++)
								{
								if( unit[i].used && unit[i].ctgry==PLANE && unit[i].used==your_side && unit[i].info[3] 
									&& unit[i].info[0]==PARKING && unit[m].info[1]==unit[i].info[1] && unit[i].stop==0 )
									f++;
								}
***/
							for( i=1; i<=MaxUnitId; i++)
								{
								if( i!=m && Units[i].Side!=0 && Units[i].Category==UnitCategory.Plane && Units[i].Side==LocalSide && Units[i].info[3]!=0
									&& Units[i].PlaneState==UnitState.Parked && Units[m].info[1]==Units[i].info[1] && Units[i].Stop==0 )
									f++;
								}


							if( f==0 )
								{	// 滑走路に他の着陸機がなければ発進許可。
								Units[Units[m].info[1]].info[8]=0;		// その空母の次機発進許可
								}
else
	Units[Units[m].info[1]].info[8]=2+f;		// 空母の発進不可の調査のため

							}
						if(Units[m].PlaneState==UnitState.Parked)
							{
							Units[m].Position = new WorldPosition(Units[m].Position.X + (cos(Units[m].Direction*a_PI)*Units[m].Speed), Units[m].Position.Y);
							Units[m].Position = new WorldPosition(Units[m].Position.X, Units[m].Position.Y - (sin(Units[m].Direction*a_PI)*Units[m].Speed));
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
					if( Units[m].Supply==0 )
						{
						switch( Units[m].Kind )
							{
							case UnitKind.Cruiser:
							case UnitKind.Destroyer:
							case UnitKind.Battleship:			
							case UnitKind.Carrier:
							case UnitKind.LightCarrier:
								if( Units[m].EmergencyFlags[0]==0 )
									SetShipEmergencyDestination(m);
								break;



							case UnitKind.Transport:
								if( Units[m].EmergencyFlags[0]==0 && Units[m].Target!=0 )
									SetTransportLandingDestination(m);
								if( Units[m].EmergencyFlags[0]==0  )
									SetShipEmergencyDestination(m);
								break;


							case UnitKind.Submarine:
								if( Units[m].EmergencyFlags[0]==0  )
									ReturnIntoWorld(m);
								break;


							case UnitKind.Fighter:
								SetFighterEmergencyDestination(m);
								if( Units[m].Target!=0 )
									{
									SetFighterAttackDestination(m);	// 攻撃目標あり
									}
								else
									{
									if( Units[m].EmergencyFlags[0]==0 && Units[m].Mode==UnitMode.Return )
										SetAttackerEmergencyDestination(m);
									}
//unit[m].arm2[0]++;
								break;



							case UnitKind.Attacker:
							case UnitKind.Bomber:
								if( Units[m].Target!=0 && Units[m].GroupLeader==0 )
									SetAttackerAttackDestination(m);
								if( Units[m].EmergencyFlags[0]==0 )
									SetAttackerEmergencyDestination(m);

								if ( Units[m].Kind==UnitKind.Bomber && (Units[Units[m].info[1]].Kind!=UnitKind.AirBase) )
									{
									Units[m].info[1]=0;
									}
//unit[m].arm2[0]++;
								break;
							}
						}


					if( Units[m].GroupLeader!=0 && Units[Units[m].GroupLeader].Side==0 )
						Units[m].GroupLeader=0;



					if( Units[m].GroupLeader==0 )
						{
						// 単独、もしくは、編隊長
						if(Units[m].Stop==0 )
							{
							if( /*paint_effect_on &&*/ Units[m].Category==UnitCategory.Plane && Units[m].Mode==UnitMode.Return && Units[m].info[3]==1 )
								{
								UpdateLanding(m);
								}

							// ptin dbg
							wrk_r.top=(int)Units[m].PathY[0]+ON_PP;//(int)unit[m].pp_y[0]-ON_PP;
							wrk_r.right=(int)Units[m].PathX[0]+ON_PP;
							wrk_r.bottom=(int)Units[m].PathY[0]-ON_PP; //(int)unit[m].pp_y[0]+ON_PP;
							wrk_r.left=(int)Units[m].PathX[0]-ON_PP;

							if( PointInRect3(ref wrk_r,(int)Units[m].Position.X,(int)Units[m].Position.Y)!=0 )
								{
								if( Units[m].PathX[1]!=MAP_RIGHT+1 )
									{
									// ＰＰ＿ＸＹを一つずつずらす
									for(n=0; Units[m].PathX[n]!=MAP_RIGHT+1; n++)
										{
										Units[m].PathX[n]=Units[m].PathX[n+1];
										Units[m].PathY[n]=Units[m].PathY[n+1];
										}
									}
								else
									{
									// ＰＰの再終点に到着
									if( Units[m].Category==UnitCategory.Ship )
										{
										Units[m].Stop=1;
										}
									else
										{
										if( Units[m].Category==UnitCategory.Plane && Units[m].Mode==UnitMode.Return /*&& unit[m].info[3]==0*/)
											{
											Units[m].info[3]=1;
											SetLandingDestination(m);
											for(n=1;n<=MaxUnitId;n++)
												{
												if( Units[n].Side!=0 && Units[n].GroupLeader==m )
													{
													Units[n].GroupLeader=0;
													Units[n].PathX[0]+=(double)(Random(600)-300);
													Units[n].PathY[0]+=(double)(Random(600)-300);
//													unit[n].pp_x[0]+=(rnd(500));
//													unit[n].pp_y[0]+=(rnd(500));
//unit[n].pp_x[0]+=(unit[n].rnd_250[0]*2)-250;
//unit[n].pp_y[0]+=(unit[n].rnd_250[1]*2)-250;
//unit[n].pp_x[0]+=500;
//unit[n].pp_y[0]+=500;
													Units[n].PathX[1]=MAP_RIGHT+1;
													}
												}
											if( Units[m].IsGroupLeader!=0 )
												{
												Units[m].IsGroupLeader=0;	Units[m].FormationNumber=0;
												}
											}
										}
									}
								}
							}
						else
							{
							Units[m].PathX[0]=Units[m].Position.X;
							Units[m].PathY[0]=Units[m].Position.Y;
							}
						}
					else
						{
						// 編隊追随機
						if(Units[m].Stop==0)
							{
							if( (Tick%10)==0 )
								{
								SetDynamicDestination(m);
								}
							}
						else
							{
							Units[m].PathX[0]=Units[m].Position.X;
							Units[m].PathY[0]=Units[m].Position.Y;
							}
					
						// ptin dbg
						wrk_r.top=(int)Units[m].PathY[0]+30;//(int)unit[m].pp_y[0]-30;
						wrk_r.right=(int)Units[m].PathX[0]+30;
						wrk_r.bottom=(int)Units[m].PathY[0]-30;//(int)unit[m].pp_y[0]+30;
						wrk_r.left=(int)Units[m].PathX[0]-30;

//dbg[2]++;

						if( PointInRect3(ref wrk_r,(int)Units[m].Position.X,(int)Units[m].Position.Y)==0 && Units[m].Stop==0)
							{	// 編隊指定位置に無し
//dbg[3]++;
							Units[m].ForGroupLeader=0;
							if( Units[Units[m].GroupLeader].ForGroupLeader==0 )
								Units[Units[m].GroupLeader].ForGroupLeader=1;

/*
if( m==131 && unit[129].arm2[0]==52-1 )
{
dbg[2]=unit[unit[m].ltl_ldr].for_form_spd*100000;		// この時点で値が　０と１９００００
dbg[3]=unit[m].max_spd*100000;
}
*/

							if( Units[Units[m].GroupLeader].FormationSpeed > Units[m].MaxSpeed || Units[Units[m].GroupLeader].FormationSpeed==0 )
								{
								Units[Units[m].GroupLeader].FormationSpeed = Units[m].MaxSpeed;
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
							Units[m].ForGroupLeader=2;
							if( Units[m].Category==UnitCategory.Ship && Units[Units[m].GroupLeader].Stop==1 )
								{
								Units[m].Stop=1;
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
				if( (Units[m].Stop==0 || Units[m].EmergencyFlags[0]!=0)  && !(Units[m].Category==UnitCategory.Ship && Units[m].Supply!=0) )	
					{
					if( Units[m].EmergencyFlags[0]!=0	)
						{
						// 緊急移動先についているか
						// ptin dbg
						wrk_r.top=(int)Units[m].EmergencyDestination.Y+ON_PP;//(int)unit[m].em_y-ON_PP;
						wrk_r.right=(int)Units[m].EmergencyDestination.X+ON_PP;
						wrk_r.bottom=(int)Units[m].EmergencyDestination.Y-ON_PP;//(int)unit[m].em_y+ON_PP;
						wrk_r.left=(int)Units[m].EmergencyDestination.X-ON_PP;

						if( PointInRect3(ref wrk_r,(int)Units[m].Position.X,(int)Units[m].Position.Y)!=0 )
							{
							Units[m].EmergencyFlags[0]=0;
							// 通常移動		直前定点へ！
							wrk_x=Units[m].PathX[0]-Units[m].Position.X;
							wrk_y=Units[m].PathY[0]-Units[m].Position.Y;
							}
						else
							{
							// 緊急移動先がある場合
							wrk_x=Units[m].EmergencyDestination.X-Units[m].Position.X;
							wrk_y=Units[m].EmergencyDestination.Y-Units[m].Position.Y;
							}
						}
					else
						{
						// 通常移動		直前定点へ！
						wrk_x=Units[m].PathX[0]-Units[m].Position.X;
						wrk_y=Units[m].PathY[0]-Units[m].Position.Y;
						}

					pp_drctn=atan2(wrk_y,wrk_x)*RAD_to;


					if(pp_drctn<0)
						pp_drctn=360+pp_drctn;



					land=0;
					if( Units[m].Category==UnitCategory.Ship )
						{
						// 艦首方向に他の艦船があるか
						wrk_x2=Units[m].Position.X;
						wrk_y2=Units[m].Position.Y;
						wrk_x2+=cos(Units[m].Direction*a_PI)*(40+Units[m].MaxSpeed*10/*50*/);
						wrk_y2+=sin(Units[m].Direction*a_PI)*(40+Units[m].MaxSpeed*10/*50*/);
						if( /*(cc_count%5 && unit[m].used==cpu_side ) &&*/ !( Units[m].Kind==UnitKind.Submarine && Units[m].info[6]!=0 ) )
							{
							for( n=1; n<=MaxUnitId; n++)
								{
								if(Units[n].Side!=0 && m!=n && Units[n].Category==UnitCategory.Ship && !(Units[n].Kind==UnitKind.Submarine && Units[n].info[6]!=0) /*&& unit[n].kind!=SP && unit[n].kind!=AP*/ && !(Units[n].Kind>=UnitKind.AirBase&&Units[n].Kind<=UnitKind.Fortress) )
									{
									// ptin dbg
									wrk_r.top=(int)Units[n].Position.Y+(Sprites[UNIT_JPN].ht/2);//(int)unit[n].y-(sprt[UNIT_JPN].ht/2);
									wrk_r.right=(int)Units[n].Position.X+(Sprites[UNIT_JPN].wd/2);
									wrk_r.bottom=(int)Units[n].Position.Y-(Sprites[UNIT_JPN].ht/2);//(int)unit[n].y+(sprt[UNIT_JPN].ht/2);
									wrk_r.left=(int)Units[n].Position.X-(Sprites[UNIT_JPN].wd/2);

									if( PointInRect3(ref wrk_r,(int)wrk_x2,(int)wrk_y2)!=0)
										{
										// 前方に艦船！
										land=1;

										Units[m].EmergencyFlags[0]=0;

										break;
										}
									}
								}
							}


						// ＰＰ方向に陸地があるか
						if( land==0 /*&& unit[m].used!=cpu_side*/ )
							{
							wrk_x2=Units[m].Position.X;
							wrk_y2=Units[m].Position.Y;
							wrk_x2+=cos(pp_drctn*a_PI)*80;
							wrk_y2+=sin(pp_drctn*a_PI)*80;

							if(!( wrk_y2>MAP_TOP || wrk_y2<MAP_BOTTOM || wrk_x2<MAP_LEFT || wrk_x2>MAP_RIGHT ))
								{
								cm_scrn_x=(int)((wrk_x2+(Sprites[UNIT_JPN].wd/2)-MAP_LEFT)/Sprites[MAP_TIP_NRML].wd);
								cm_scrn_y=(int)((MAP_TOP-wrk_y2+(Sprites[UNIT_JPN].ht/2))/Sprites[MAP_TIP_NRML].ht);
								if( MapTiles[cm_scrn_y][cm_scrn_x]>=1 && MapTiles[cm_scrn_y][cm_scrn_x]<=9 )
									{
									land=1;
									}
								}
							}

						if( Units[m].EmergencyFlags[0]!=0 && land!=0)
							{
							// 緊急移動の取り消し
							Units[m].EmergencyFlags[0]=0;
							}
						}


					pp_drctn=pp_drctn-Units[m].Direction;
					if(pp_drctn<0)
						pp_drctn=360+pp_drctn;


					Units[m].TurnRate=0;
					if(pp_drctn>=1.0&&pp_drctn<=180.0)
						{
						//	左へ
						Units[m].TurnRate=Units[m].TurnRateChange;
						if(pp_drctn <= 3.0)
							Units[m].TurnRate = +0.5;		// 要は微調整	//unit[m].a_drctn_add;
						}
					if(pp_drctn<=359.0 && pp_drctn>180.0)
						{
						//	右へ
						Units[m].TurnRate=-Units[m].TurnRateChange;
						if(pp_drctn >= 357.0)
							Units[m].TurnRate = -0.5;		// 要は微調整	//-unit[m].a_drctn_add;
						}


					Units[m].Direction+=Units[m].TurnRate;


					

					if(Units[m].Direction>=360.0)
						Units[m].Direction-=360.0;
					if(Units[m].Direction<0.0)
						Units[m].Direction=360.0+Units[m].Direction;

					if(pp_drctn>180.0)
						pp_drctn=360.0-pp_drctn;


					// ユニットのスピード
					if( Units[m].Category!=UnitCategory.Plane && (pp_drctn>=80.0 || land!=0 ) )
						{
						if( ( Units[m].MinSpeed ) < Units[m].Speed || land!=0 )
							{
							Units[m].Acceleration=-Units[m].AccelerationChange;
							}
						else
							{
							Units[m].Acceleration=+Units[m].AccelerationChange;
							}
						}
					else
						{
						if(pp_drctn>=45.0  )
							{


							if((Units[m].MaxSpeed+(Units[m].Kind==UnitKind.Fighter && Units[m].Target!=0 ? 1 : 0)*CMBT_SPD)/2 < Units[m].Speed  )
								{
								Units[m].Acceleration=-Units[m].AccelerationChange;
								}
							else
								{
								Units[m].Acceleration=+Units[m].AccelerationChange;
								}
							}
						else
							{


							if(pp_drctn>=22.5 )
								{




								if( ((Units[m].MaxSpeed+(Units[m].Kind==UnitKind.Fighter && Units[m].Target!=0 ? 1 : 0)*CMBT_SPD)/3)*2 < Units[m].Speed )
									{
									Units[m].Acceleration=-Units[m].AccelerationChange;
									}
								else
									{
									Units[m].Acceleration=+Units[m].AccelerationChange;
									}
								}
							else
								{




								if( Units[m].EmergencyFlags[0]!=0 )
									{
									//　緊急移動の場合
									Units[m].Acceleration=+Units[m].AccelerationChange;
									Units[m].ForGroupLeader=0;
									}
								else
									{
									//　通常移動
									if( Units[m].ForGroupLeader==1 )
										{	// 随伴機より。速度落とせの連絡 この場合ｍ番は編隊長

// この時点でfor_form_spdがちがう

										n=0;

										if( Units[m].Target!=0 && Units[Units[m].Target].Found!=0 ) //&& unit[m].kind==AT1 )
											{
											n=1;
											wrk_x=Units[Units[m].Target].Position.X-Units[m].Position.X;
											wrk_y=Units[Units[m].Target].Position.Y-Units[m].Position.Y;

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
										if( n==0 && Units[m].Speed>=(Units[m].FormationSpeed*(0.60-(Units[m].Kind==UnitKind.Carrier ? 1 : 0)*0.15 ))+Units[m].AccelerationChange )
											{
											Units[m].Acceleration=-Units[m].AccelerationChange;
											}
										else
											{
											Units[m].Acceleration=+Units[m].AccelerationChange;

											}


										Units[m].ForGroupLeader=0;
										Units[m].FormationSpeed=0.0;




										}
									else
										{
										if( Units[m].ForGroupLeader==2 && Units[m].MaxSpeed >= Units[Units[m].GroupLeader].MaxSpeed )
											{ // 編隊指定位置にいる。編隊Ｌｄｒの速度に合わせよ
											Units[m].Speed=Units[Units[m].GroupLeader].Speed;
											Units[m].Acceleration=0;
											}
										else
											{	// 単独機か、連絡無しの指揮機
											Units[m].Acceleration=+Units[m].AccelerationChange;
											}
										Units[m].ForGroupLeader=0;
										}
									}
								}
							}
						}
					}
				else
					{
					if(Units[m].Speed>0)
						{
						Units[m].Acceleration=-(Units[m].AccelerationChange*2);


						}
					}


/*
if( m==129 && unit[129].arm2[0]==52 )
{
dbg[2]=unit[m].spd_add*100000;
dbg[4]=unit[m].spd*100000;
}
*/

				if( Units[m].Fuel<=0 && (Units[m].Category==UnitCategory.Plane || (Units[m].Category==UnitCategory.Ship && Units[m].Speed>= Units[m].MaxSpeed/10 ) ) )
					Units[m].Acceleration=-(Units[m].AccelerationChange*2);		// ガス０なら減速へ
				

				// 速度を決定
				Units[m].Speed+=Units[m].Acceleration;


				// 最高速度の制限
				if( (Units[m].MaxSpeed+(Units[m].Kind==UnitKind.Fighter && Units[m].Target!=0 ? 1 : 0)*CMBT_SPD) < Units[m].Speed/*-unit[m].a_spd_add*/ )
					{
					//unit[m].spd-=unit[m].spd_add;
					Units[m].Speed-=Units[m].AccelerationChange*8;
					if( Units[m].Speed < (Units[m].MaxSpeed+(Units[m].Kind==UnitKind.Fighter && Units[m].Target!=0 ? 1 : 0)*CMBT_SPD) )
						Units[m].Speed = (Units[m].MaxSpeed+(Units[m].Kind==UnitKind.Fighter && Units[m].Target!=0 ? 1 : 0)*CMBT_SPD);
					}

				if( Units[m].Kind==UnitKind.Transport && Units[m].Ammo!=0 )
					{
					// 輸送船でなんかつんでると最高速度がおちる
					switch( Units[m].Weapon )
						{
						case FireKind.CargoInfantryBase:	wrk_3=1.0;		break;
						case FireKind.CargoPillboxes:	wrk_3=0.9;		break;
						case FireKind.CargoFortress:	wrk_3=0.9;		break;
						case FireKind.CargoAirBase:		wrk_3=0.8;		break;
						case FireKind.CargoNavalBase:		wrk_3=0.8;		break;
						}

					if( Units[m].MaxSpeed*wrk_3 < Units[m].Speed )
						{
						Units[m].Speed-=Units[m].AccelerationChange*8;
						}

					}


				if(  Units[m].Kind==UnitKind.Submarine && Units[m].info[6]==1 && Units[m].Speed>(Units[m].MaxSpeed*0.7) )
					{
					Units[m].Speed=(Units[m].MaxSpeed*0.7);
					}



				if( (Units[m].Stop==0 || Units[m].EmergencyFlags[0]!=0) &&!(Units[m].Fuel<=0))
					{
					if( Units[m].MinSpeed > Units[m].Speed )
						Units[m].Speed=Units[m].MinSpeed;
					}
				else
					{
					if( Units[m].Speed < 0 )
						Units[m].Speed=0;
					}

				// Em_flgがあるならデクリ
				if(Units[m].EmergencyFlags[0]!=0)
					Units[m].EmergencyFlags[0]--;




/*
if( m==129 && unit[129].arm2[0]==52 )
{
dbg[5]=unit[m].spd*100000;
}
*/


				// 着艦チェック
				if( Units[m].Category==UnitCategory.Plane && Units[m].Mode==UnitMode.Return && Units[m].info[3]==1 && Units[Units[m].info[1]].Side!=0 
					&& Units[Units[m].info[1]].info[7]==0 && !(Units[m].Category==UnitCategory.Plane && Units[m].PlaneState==UnitState.Flying && Units[Units[m].info[1]].Hp<=Units[Units[m].info[1]].MaxHp*0.2)
					&& !( Units[m].Kind==UnitKind.Bomber && (Units[Units[m].info[1]].Kind!=UnitKind.AirBase) )
					&& !( Units[m].Kind==UnitKind.Fighter && Units[m].Variant==1 && (Units[Units[m].info[1]].Kind!=UnitKind.AirBase) )
					&& Units[m].Side==Units[Units[m].info[1]].Side && Units[Units[m].info[1]].info[0]==0
					)
					{
					if( 0!=0 && Units[Units[m].info[1]].info[4]!=0)
						{
						// ほんまにＩｎｆｏ［４］があるんやなチェック
						}

					if( ToEightDirections((int)Units[m].Direction)==ToEightDirections((int)Units[Units[m].info[1]].Direction) 
					&& Units[Units[m].info[1]].info[2]>Units[Units[m].info[1]].info[1] 
					&& Units[Units[m].info[1]].info[4]<=0 )
						{
						// ptin dbg
						wrk_r.top=(int)Units[Units[m].info[1]].Position.Y+ON_PP/2;//(int)unit[unit[m].info[1]].y-ON_PP/2;
						wrk_r.right=(int)Units[Units[m].info[1]].Position.X+ON_PP/2;
						wrk_r.bottom=(int)Units[Units[m].info[1]].Position.Y-ON_PP/2;//(int)unit[unit[m].info[1]].y+ON_PP/2;
						wrk_r.left=(int)Units[Units[m].info[1]].Position.X-ON_PP/2;

						if( PointInRect3(ref wrk_r,(int)Units[m].Position.X,(int)Units[m].Position.Y)!=0 )
							{
							// 着艦
							Units[Units[m].info[1]].info[1]++;	// 現在格納数
							if(m==SelectedUnit)
								{
								SelectedUnit=0; PreviousSelectedUnit=(short)Units[m].info[1]; CombatMenuKind=0; CombatMenuSelection=CombatMenuItem.None; ClearSelection();
								}
							if(m==PreviousSelectedUnit)
								{
								PreviousSelectedUnit=(short)Units[m].info[1];
								}

							if(Selections[1][m]!=0)
								{
								if(SelectionCount!=0)
									SelectionCount--; 
								// セレクトの設定番号を連番にする。
								for(n=1;n<=MaxUnitId;n++)				
									{
									if( Selections[1][n]>=Selections[1][m]+1 )
										Selections[1][n]--;
									}
								Selections[1][m]=0; 
								}

//							unit[m].stop=1;
							//unit[m].info[1]=4;				// 所属の空母、及び、基地の番号
							Units[m].info[2]=FindParkingNumber(m);	// 格納庫の位置、及び、その基地の番機番号
							Units[m].info[3]=1;					// 格納庫、基地での移動情態
							Units[m].info[4]=0;					// 減速度をクリア
							//unit[m].info[5]=MOVE;				// モード（コンバットメニュー）
							Units[Units[m].info[1]].info[7]=1;	// 着艦、0許可、1不許可
							Units[Units[m].info[1]].info[8]=1;	// その空母の次機発進許可	0許可、1不許可
							Units[m].PlaneState=UnitState.Parked;
							//set_pos_of_parking(m);
							Units[m].Position = new WorldPosition(Sprites[UNIT_INFO_JPN].x+Sprites[UNIT_INFO_JPN].wd/2+(SharedRandom(16)-7), Sprites[UNIT_INFO_JPN].y+Sprites[UNIT_INFO_JPN].ht+40);
							Units[m].Speed=5.0; Units[m].Direction=90.0;
							
							Units[m].Target=0;					// 

							Units[m].EmergencyFlags[0]=0;
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
				Units[m].Position += new WorldVector(cos(Units[m].Direction*a_PI)*Units[m].Speed, sin(Units[m].Direction*a_PI)*Units[m].Speed);


/*
if( m==129 && unit[129].arm2[0]==52 )
{

dbg[5]=unit[m].x*100000;
dbg[6]=unit[m].y*100000;
dbg[7]=unit[m].spd*100000;

}
*/


				// 燃料消費
				n=(int)Units[m].FuelInterval;
				if( Units[m].Kind==UnitKind.Attacker && Units[m].Ammo!=0 && (Units[m].Weapon==FireKind.Torpedo || Units[m].Weapon==FireKind.Bomb))
					n=n-(n/10);
				if( Units[m].Kind==UnitKind.Fighter && Units[m].Target!=0 && Units[m].MaxSpeed < Units[m].Speed )
					n=n-(n/10);


				if( Units[m].FuelInterval>=1 && (Tick%n)==0 && Units[m].Fuel>0 && Units[m].Speed>0)
					{
//					unit[m].gas[0]--;
					Units[m].Fuel-=(Units[m].Speed/Units[m].MaxSpeed);
					if(Units[m].Fuel<0)
						Units[m].Fuel=0;
					}

				if( Units[m].Fuel==0 && Units[m].Category==UnitCategory.Plane && Units[m].Speed<=0 )
					Units[m].Hp=0;							// 飛行機でガス０なら落ちます
				}

					//=========		 潜水艦の浮上		=========//
				//if( unit[m].kind==SS1 && !unit[m].found && unit[m].info[6] )
				//unit[m].info[6]=0;

				//=========		 ユニットの攻撃相手探索		=========//
				
				//seek_enemy(m);	


			
				//=========		 ユニットの攻撃制御		=========//
				// ターゲットがアウトならターゲットをクリア
			if( Units[m].Target!=0 && ( ( Units[Units[m].Target].Category==UnitCategory.Plane && ( Units[Units[m].Target].Hp<=0|| Units[Units[m].Target].PlaneState==UnitState.Parked )   ) || ( Units[Units[m].Target].Category==UnitCategory.Ship && Units[Units[m].Target].Hp<=0 ) || (Units[m].Kind==UnitKind.Fighter && Units[m].Ammo<=0) || (Units[Units[m].Target].Kind==UnitKind.Submarine && Units[Units[m].Target].info[6]!=0 ) || ( Units[m].Category==UnitCategory.Plane && Units[m].PlaneState==UnitState.Parked ) ))
				{
				Units[m].Target=0;
				}
			
			if( Units[m].ReloadTime>=1 )
				{
				if( !(Units[m].Kind==UnitKind.Submarine&&Units[m].Stop==0))
					Units[m].ReloadTime--;
				if( Units[m].ReloadTime==0 && (Units[m].Weapon==FireKind.Maintenance||Units[m].Weapon==FireKind.Unarmed) )
					{
					if( Units[m].Kind==UnitKind.Fighter )
						{
						Units[m].Weapon=FireKind.Bullet;		// 武装品種
						Units[m].Ammo=50;		// 数
						}
					else
						{
						Units[m].Weapon=FireKind.Unarmed;
						Units[m].Ammo=0;
						}
					
					Units[m].Fuel=100;
					}
				}


			dmg_act=1;
			if( Units[m].Hp<=Units[m].MaxHp*0.3 )
				{
				dmg_act=2;
				}


			// そのユニットの打つ、発射を制御します。  
			if( IsEditingMap==0 && Units[m].Supply==0 && (( Units[m].Category==UnitCategory.Plane && Units[m].PlaneState==UnitState.Flying )||( Units[m].Category==UnitCategory.Ship)))
				{
				switch( Units[m].Kind )
					{
					case UnitKind.InfantryBase:
						// 地上基地は弾が減りません
						Units[m].Ammo=Units[m].MaxAmmo;

						// 艦砲 自動
						if( Units[m].Ammo>=1 && Random(/*200*/250*dmg_act)==0/*unit[m].rnd_200[0]==cc_count%(200*dmg_act)*/ )
							{
							FireWeapons(m,0,FireKind.Gun);
							}
						// 艦砲　選択
						//if(unit[m].arm[1]>=1  && rnd(150*dmg_act)==0/*unit[m].rnd_65[0]==cc_count%(65*dmg_act)*/ && unit[m].arm[2] )
						//	{
						//	fire_now(m,unit[m].arm[2],GUN);
						//	}
						// 対空砲弾 自動砲撃
						if( Units[m].Ammo>=1 && Random(200*dmg_act)==0/*unit[m].rnd_150[1]==cc_count%(150*dmg_act)*/ /*&& !(unit[m].used==JPN && rnd(3)!=1)*/)
							{
							FireWeapons(m,0,FireKind.AntiAircraftShell);
							}
						// 対空機関砲 X 2
						if( Random(180*dmg_act)==0/*unit[m].rnd_80[0]==cc_count%(80*dmg_act)*/ && Units[m].Side==Side.UnitedStates )
							FireWeapons(m,0,FireKind.RapidAntiAircraftShell);
						// 対空機銃
						if( Random(10*dmg_act)==0/*unit[m].rnd_20[0]==cc_count%(20*dmg_act)*/ )
							FireWeapons(m,0,FireKind.Bullet);
						break;


					case UnitKind.Pillboxes:
						// 地上基地は弾が減りません
						Units[m].Ammo=Units[m].MaxAmmo;
						// 艦砲 自動
						if( Units[m].Ammo>=1 && Random( 200*dmg_act )==0/*unit[m].rnd_150[0]==cc_count%(150*dmg_act)*/ )
							{
							FireWeapons(m,0,FireKind.Gun);
							}
						// 艦砲　選択
						//if(unit[m].arm[1]>=1  && rnd(100*dmg_act)==0/*unit[m].rnd_65[0]==cc_count%(65*dmg_act)*/ && unit[m].arm[2] )
						//	{
						//	fire_now(m,unit[m].arm[2],GUN);
						//	}
						// 対空砲弾 自動砲撃
						if( Units[m].Ammo>=1 && Random(160*dmg_act)==0/*unit[m].rnd_80[1]==cc_count%(80*dmg_act)*/ /*&& !(unit[m].used==JPN && rnd(3)!=1)*/)
							{
							FireWeapons(m,0,FireKind.AntiAircraftShell);
							}
						// 対空機関砲
						if( Random(140*dmg_act)==0 && Units[m].Side==Side.UnitedStates )
							{
							FireWeapons(m,0,FireKind.RapidAntiAircraftShell);
							}
						// 対空機銃
						if( Random(10*dmg_act)==0 )
							{
							FireWeapons(m,0,FireKind.Bullet);
							}
						break;


					case UnitKind.Fortress:
						// 地上基地は弾が減りません
						Units[m].Ammo=Units[m].MaxAmmo;

						// 艦砲 自動
						if( Units[m].Ammo>=1 && Random(/*100*/150*dmg_act)==0/*unit[m].rnd_100[0]==cc_count%(100*dmg_act)*/ )
							{
							FireWeapons(m,0,FireKind.Gun);
							}
						// 艦砲　選択
						//if(unit[m].arm[1]>=1  && rnd(65*dmg_act)==0/*unit[m].rnd_65[0]==cc_count%(65*dmg_act)*/ && unit[m].arm[2] )
						//	{
						//	fire_now(m,unit[m].arm[2],GUN);
						//	}
						// 対空砲弾 自動砲撃
						if( Units[m].Ammo>=1 && Random(120*dmg_act)==0/*unit[m].rnd_80[1]==cc_count%(80*dmg_act)*/ /*&& !(unit[m].used==JPN && rnd(3)!=1)*/)
							{
							FireWeapons(m,0,FireKind.AntiAircraftShell);
							}
						// 対空機関砲
						if( Random(120*dmg_act)==0/*unit[m].rnd_65[1]==cc_count%(65*dmg_act)*/ && Units[m].Side==Side.UnitedStates )
							FireWeapons(m,0,FireKind.RapidAntiAircraftShell);
						// 対空機関砲 X 2
						if( Random(160*dmg_act)==0/*unit[m].rnd_80[0]==cc_count%(80*dmg_act)*/ && Units[m].Side==Side.UnitedStates )
							FireWeapons(m,0,FireKind.RapidAntiAircraftShell);
						// 対空機銃
						if( Random(8*dmg_act)==0/*unit[m].rnd_20[0]==cc_count%(20*dmg_act)*/ )
							FireWeapons(m,0,FireKind.Bullet);
						break;




					case UnitKind.Battleship:
						if( Units[m].Side==Side.Japan && Units[m].Variant==1 )
							{
							// 大和級
							// 艦砲 自動
							if( Units[m].Ammo>=1 && Random(350*dmg_act)==0 && (Units[m].Target==0 || Units[Units[m].Target].Category==UnitCategory.Ship) )
								{
								FireWeapons(m,0,FireKind.Gun);
								}
							// 艦砲　選択
							if(Units[m].Ammo>=1 && Random(300*dmg_act)==0 && Units[m].Target!=0 )
								{
								FireWeapons(m,Units[m].Target,FireKind.NavalBaseGun);
								}

							if( Units[m].Ammo>=1 && Random(180*dmg_act)==0 && Units[m].Target!=0 )
								{
								FireWeapons(m,Units[m].Target,FireKind.AntiAircraftShell);		// 対空砲弾 選択
								}

							if( Units[m].Ammo>=1 && Random(95*dmg_act)==0 )
								{
								FireWeapons(m,0,FireKind.AntiAircraftShell);							// 対空砲弾 自動砲撃
								}

							// 対空機銃
							if( Random(4*dmg_act)==0 )
								FireWeapons(m,0,FireKind.Bullet);
							}
						else
							{
							// 艦砲 自動
							if( Units[m].Ammo>=1 && Random(350*dmg_act)==0 && (Units[m].Target==0 || Units[Units[m].Target].Category==UnitCategory.Ship) )
								{
								FireWeapons(m,0,FireKind.Gun);
								}
							// 艦砲　選択
							if(Units[m].Ammo>=1 && Random(300*dmg_act)==0 && Units[m].Target!=0 )
								{
								FireWeapons(m,Units[m].Target,FireKind.Gun);
								}


							if( Units[m].Ammo>=1 && Random(190*dmg_act)==0 && Units[m].Target!=0 )
								{
								FireWeapons(m,Units[m].Target,FireKind.AntiAircraftShell);		// 対空砲弾 選択
								}

							if( Units[m].Ammo>=1 && Random(110*dmg_act)==0 )
								{
								FireWeapons(m,0,FireKind.AntiAircraftShell);							// 対空砲弾 自動砲撃
								}



							// 対空機関砲
							if( Random(60*dmg_act)==0 && Units[m].Side==Side.UnitedStates )
								FireWeapons(m,0,FireKind.RapidAntiAircraftShell);
							// 対空機関砲 X 2
							if( Random(120*dmg_act)==0 && Units[m].Side==Side.UnitedStates )
								FireWeapons(m,0,FireKind.RapidAntiAircraftShell);

							// 対空機銃
							if( Random(5*dmg_act)==0 )
								FireWeapons(m,0,FireKind.Bullet);
							}

						break;


					case UnitKind.Cruiser:
						if( Units[m].Variant==0 )
							{
							// 巡洋艦
							// 艦砲 自動
							if( Units[m].Ammo>=1  && Random(350*dmg_act)==0  && (Units[m].Target==0 || Units[Units[m].Target].Category==UnitCategory.Ship) )
								{
								FireWeapons(m,0,FireKind.Gun);
								}
							// 艦砲 選択
							if( Units[m].Ammo>=1  && Random(300*dmg_act)==0 && Units[m].Target!=0 )
								{
								FireWeapons(m,Units[m].Target,FireKind.Gun);
								}



							// 対空砲弾
							if( Units[m].Ammo>=1 && Random(120*dmg_act)==0  )
								{
								if(Units[m].Target!=0)								// 対空砲弾 選択
									FireWeapons(m,Units[m].Target,FireKind.AntiAircraftShell);
								else
									FireWeapons(m,0,FireKind.AntiAircraftShell);							// 対空砲弾 自動砲撃
								}


							// 対空機関砲
							if( Units[m].Ammo>=1 && Random(60*dmg_act)==0 && Units[m].Side==Side.UnitedStates )
								FireWeapons(m,0,FireKind.RapidAntiAircraftShell);

							// 対空機銃 自動
							if( Random(10*dmg_act)==0  )
								FireWeapons(m,0,FireKind.Bullet);
							// 魚雷
							if( Units[m].Random40[0]==Tick%(40*dmg_act) && Units[m].Ammo>=1 && Units[m].ReloadTime<=0 && Units[m].Side==Side.Japan )
								{
								FireWeapons(m,0,FireKind.Torpedo);
								}
							}
						else
							{
							// 防空巡洋艦
							// 艦砲 自動
							if( Units[m].Ammo>=1  && Random(800*dmg_act)==0  && (Units[m].Target==0 || Units[Units[m].Target].Category==UnitCategory.Ship) )
								{
								FireWeapons(m,0,FireKind.Gun);
								}


							// 対空砲弾
							if( Units[m].Ammo>=1 && Random(100*dmg_act)==0 )
								{
								// 自動
								FireWeapons(m,0,FireKind.AntiAircraftShell);							// 対空砲弾 自動砲撃
								}

							if( Units[m].Ammo>=1 && Random(180*dmg_act)==0 && Units[m].Target!=0 )
								{
								// 選択
								FireWeapons(m,Units[m].Target,FireKind.AntiAircraftShell);
								}


							// 対空機関砲
							if( Units[m].Ammo>=1 && Random(55*dmg_act)==0 && Units[m].Side==Side.UnitedStates )
								FireWeapons(m,0,FireKind.RapidAntiAircraftShell);

							// 対空機銃 自動
							if( Random(10*dmg_act)==0  )
								FireWeapons(m,0,FireKind.Bullet);

							}
						break;


					case UnitKind.Carrier:
						// 対空機関砲 
						if( Units[m].Ammo>=1 && Random(80*dmg_act)==0/*unit[m].rnd_80[0]==cc_count%(80*dmg_act)*/ && Units[m].Side==Side.UnitedStates )
							FireWeapons(m,0,FireKind.RapidAntiAircraftShell);
						// 対空機銃
						if( Random(15*dmg_act)==0/*unit[m].rnd_10[0]==cc_count%(10*dmg_act)*/)
							FireWeapons(m,0,FireKind.Bullet);
						break;

					case UnitKind.Destroyer:
						if( Units[m].Variant==0 )
							{
							// 艦砲 自動
							if( Units[m].Ammo>=1 && Random(200*dmg_act)==0/*unit[m].rnd_200[0]==cc_count%(200*dmg_act)*/ )
								{
								FireWeapons(m,0,FireKind.Gun);
								}

							// 魚雷
							if( Units[m].Random40[0]==Tick%(40*dmg_act) && Units[m].Ammo>=1 && Units[m].ReloadTime<=0 )
								{
								FireWeapons(m,0,FireKind.Torpedo);
								}

							// 爆雷
							if( Units[m].Ammo>=1 && (Tick%(35*dmg_act))==0 )
								{
								FireWeapons(m,0,FireKind.AntiSubmarineBomb);
								}
							// 対空機銃
							if( Random(15*dmg_act)==0/*unit[m].rnd_20[0]==cc_count%(20*dmg_act)*/ )
								FireWeapons(m,0,FireKind.Bullet);
							}
						else
							{
							// 艦砲 自動
							if( Units[m].Ammo>=1 && Random(500*dmg_act)==0 )
								{
								FireWeapons(m,0,FireKind.Gun);
								}

							// 対空機銃
							if( Random(20*dmg_act)==0 )
								FireWeapons(m,0,FireKind.Bullet);


							// 爆雷
							if( Units[m].Ammo>=1 && (Tick%(25*dmg_act))==0 )
								{
								FireWeapons(m,0,FireKind.AntiSubmarineBomb);
								}

							}
						break;

					case UnitKind.LightCarrier:
						// 対空機関砲
						if( Units[m].Ammo>=1 && Random(100*dmg_act)==0/*unit[m].rnd_100[0]==cc_count%(100*dmg_act)*/ && Units[m].Side==Side.UnitedStates )
							FireWeapons(m,0,FireKind.RapidAntiAircraftShell);
						// 対空機銃
						if( Random(20*dmg_act)==0/*unit[m].rnd_30[0]==cc_count%(30*dmg_act)*/ )
							FireWeapons(m,0,FireKind.Bullet);
						break;



					case UnitKind.Transport:
						// トランスボーと
						if( Units[m].Weapon==FireKind.CargoNavalBase && Units[m].Ammo>=1  && Units[m].Target==MaxUnitId+1)
							FireWeapons(m,0,FireKind.CargoNavalBase);
						if( Units[m].Weapon==FireKind.CargoAirBase && Units[m].Ammo>=1  && Units[m].Target==MaxUnitId+1)
							FireWeapons(m,0,FireKind.CargoAirBase);

						if( Units[m].Weapon==FireKind.CargoInfantryBase && Units[m].Ammo>=1  && Units[m].Target==MaxUnitId+1)
							FireWeapons(m,0,FireKind.CargoInfantryBase);
			
						if( Units[m].Weapon==FireKind.CargoPillboxes && Units[m].Ammo>=1  && Units[m].Target==MaxUnitId+1)
							FireWeapons(m,0,FireKind.CargoPillboxes);
						if( Units[m].Weapon==FireKind.CargoFortress && Units[m].Ammo>=1  && Units[m].Target==MaxUnitId+1)
							FireWeapons(m,0,FireKind.CargoFortress);

						break;



					case UnitKind.Submarine:
						// 艦砲 自動
						// 魚雷
						if( Units[m].Ammo>=1 && Units[m].ReloadTime<=0 && Units[m].Target!=0 /*&& unit[m].rnd_20[0]==cc_count%(20)*/ )
							{
							FireWeapons(m,Units[m].Target,FireKind.Torpedo);
							}
						break;


					case UnitKind.Fighter:
						// 戦闘機の場合は、前方に敵航空機が飛んでればとりあえず撃つ
						if( Units[m].Ammo>=1  && ((Tick+Units[m].Random20[0])% (10-(Units[m].Variant==1 ? 1 : 0)*3 ) )==0 )
							FireWeapons(m,0,FireKind.Bullet);
						break;


					case UnitKind.Attacker:
						// 攻撃機の場合は、後方に敵航空機が飛んでればとりあえず撃つ
						if(Random(35*dmg_act)==0)
							FireWeapons(m,0,FireKind.Bullet);
						if( Units[m].Ammo>=1  && Units[m].Weapon==FireKind.Torpedo && Units[m].Target!=0 && Units[m].TurnRate==0 && Units[m].Speed>=Units[m].MaxSpeed )
							FireWeapons(m,Units[m].Target,FireKind.Torpedo);
						if( Units[m].Ammo>=1  && Units[m].Weapon==FireKind.Bomb && Units[m].Target!=0  )
							FireWeapons(m,Units[m].Target,FireKind.Bomb);
						break;


					case UnitKind.Bomber:
						if(Random(20*dmg_act)==0)
							FireWeapons(m,0,FireKind.Bullet);
						if( Units[m].Ammo>=1  && Units[m].Weapon==FireKind.Torpedo && Units[m].Target!=0 && Units[m].TurnRate==0 && Units[m].Speed>=Units[m].MaxSpeed )
							FireWeapons(m,Units[m].Target,FireKind.Torpedo);
						if( Units[m].Ammo>=1  && Units[m].Weapon==FireKind.Bomb && Units[m].ReloadTime==0 && Units[m].TurnRate==0 /*&& !(cc_count%10)*/ )	
							FireWeapons(m,Units[m].Target,FireKind.Bomb);
						break;

					}
				}
			}


		if( Units[m].Side!=0 )
			{
			// そのユニットの発するエフェクト
			UpdateUnitEffects( m );
			}




		// 潜水艦から聞こえれる探知音
		if( Units[m].Side==LocalSide && Units[m].Kind==UnitKind.Submarine && Units[m].info[6]!=0 && Result==GameResult.None && Units[m].Supply==0 )
			{
			dstc=500;
			for( i=1; i<=MaxUnitId; i++)
				{
				if( Units[i].Side!=0 && Units[i].Side!=LocalSide && Units[i].Kind==UnitKind.Destroyer && Units[m].Supply==0 )
					{
					wrk_x=Units[m].Position.X-Units[i].Position.X;
					wrk_y=Units[m].Position.Y-Units[i].Position.Y;

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


			if( IsEditingMap==0 && dstc<=400 )
				{
				if( (SharedRandom(3+(int)(dstc/5)))==0  )
				PlaySoundEffect( 0, SNR ,Units[m].Position.X, Units[m].Position.Y);
				}
			}
		}


	// ｆｉｒｅの制御
	UpdateFires( );


	}
}
