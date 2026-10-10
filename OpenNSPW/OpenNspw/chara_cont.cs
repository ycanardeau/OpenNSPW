using System.Runtime.CompilerServices;

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

// Port of chara_cont.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{
// The selections of player e (0 the rival, 1 this player) from their buffer, which holds the ships of the player's
// side, then its planes, packed together; they go back to the unit numbers of that side, Japan's or the United States'.
private void UnpackSelections(int e, bool isJapan)
	{
	short s;

	if( isJapan )
		{
		for(s=0;s<=JPN_SHIP_END-1;s++)
			{
			// 水上ユニット
			Selections[e][s+1]=BufferedSelections[e][s];
			}
		for(s=JPN_SHIP_END;s<=(USA_PLANE_END/2)-1;s++)
			{
			// 航空ユニット
			Selections[e][s+JPN_SHIP_END+1]=BufferedSelections[e][s];
			}
		}
	else
		{
		for(s=0;s<=JPN_SHIP_END-1;s++)
			{
			// 水上ユニット
			Selections[e][s+JPN_SHIP_END+1]=BufferedSelections[e][s];
			}
		for(s=JPN_SHIP_END;s<=(USA_PLANE_END/2)-1;s++)
			{
			// 航空ユニット
			Selections[e][s+(USA_PLANE_END/2)+1]=BufferedSelections[e][s];
			}
		}
	}

private bool ReceiveAndIssueOrders(ref short s, ref Array200<short> bf_2_slct_unit, ref _DP_FLAG dp_flag, ref int f, ref int m)
	{
	short bf_the_slct_unit;
	short e;
	if( CONN_DBG==0 && !CanAdvance2)
		{
		if(TickWaits[1]<99)
			TickWaits[1]++;
		return false;
		}

	CanAdvance2=false;

	if( !IsEditingMap && Result==GameResult.None && ActivePlayerCount==2 )
		{
		AutoSaveTime++;
#if CONN_DBG
		if(auto_save_time>=90 )
#else
		if(AutoSaveTime>=90 && !HasSavedDesync )
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

				if( IsHost && !WasHost )
					WasHost=true;

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

	UnpackSelections(0, LocalSide!=Side.Japan);
	UnpackSelections(1, LocalSide==Side.Japan);

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

	BufferedSelectOrders[0].IsSet=false;						// クリア
	BufferedSelectOrders[1].IsSet=false;						// クリア

	BufferedMenuOrders[0].Menu=CombatMenuItem.None;
	BufferedMenuOrders[1].Menu=CombatMenuItem.None;

	BufferedSystemOrders[0]=0;
	BufferedSystemOrders[1]=0;

	SystemOrders[0]=0;
	SystemOrders[1]=0;

	BufferedArrivedUnits[0]=0;						// クリア
	BufferedArrivedUnits[1]=0;						// クリア

	for(s=0;s<=(USA_PLANE_END/2)-1;s++)
		{
		BufferedSelections[0][s]=0;
		BufferedSelections[1][s]=0;
		}

	CanOrder=true;
	HasOrdered=false;

#if false && CONN_DBG
dbg[5]=rnd(100);
#endif
	dp_flag.dwType = MessageType.SyncFlag;

	// プログラム同期エラーチェックの為
	dp_flag.cc_chk=(byte)Tick;
	TickChecksums[1]=(byte)Tick;

	// 座標のずれチェックの為
	f=0;
	for(m=1; m<=MaxUnitId; m++)
		{
		ref var unit = ref Units[m];
		if( unit.IsUsed && !(unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked) )
			{
			f+=(int)((unit.Position.X+unit.Position.Y+unit.Direction)*10000);
			}
		}

	dp_flag.unit_chk=(byte)f;
	UnitChecksums[1]=(byte)f;

	// ランダム同期エラーチェックのため
	dp_flag.rnd_chk=(byte)RandomCount;
	RandomChecksums[1]=(byte)RandomCount;

	dp_flag.ccc_wait_chk=TickWaits[1];

	dp_flag.rival_mode=(short)Mode;

	bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
	bufferDesc.pBufferData  = (byte*) Unsafe.AsPointer(ref dp_flag);
	g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
	return true;
	}

private void SendMenuOrder(ref short s)
	{
	_DP_NEW_MENU dp_new_menu;
	// メニュー
	dp_new_menu.dwType = MessageType.MenuOrder;

	dp_new_menu.menu=(byte)BufferedMenuOrders[1].Menu;
	dp_new_menu.the_slct_unit=(byte)BufferedMenuOrders[1].SelectedUnit;

	for( s=0; s<=(USA_PLANE_END/2)-1; s++)
		{
		dp_new_menu.slct_unit[s]=(byte)BufferedSelections[1][s];
		}
	bufferDesc.dwBufferSize = (uint)(sizeof(_DP_NEW_MENU));
	bufferDesc.pBufferData  = (byte*) &dp_new_menu;
	g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
	}

private void SendSelectOrder(ref short s)
	{
	short ship;
	short plane;
	_DP_NEW_SLCT dp_new_slct;
	_DP_NEW_SLCT_SHIP dp_new_slct_ship;
	_DP_NEW_SLCT_PLANE dp_new_slct_plane;
	_DP_NEW_SLCT_LAND dp_new_slct_land;
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
		dp_new_slct.sw=BufferedSelectOrders[1].IsSet.Value;
		dp_new_slct.the_slct_unit=(byte)BufferedSelectOrders[1].SelectedUnit;
		dp_new_slct.m=(byte)BufferedSelectOrders[1].Unit;
		dp_new_slct.gr_x=(short)BufferedSelectOrders[1].GroundPosition.X;
		dp_new_slct.gr_y=(short)BufferedSelectOrders[1].GroundPosition.Y;
		for( s=0; s<=(USA_PLANE_END/2)-1; s++)
			{
			dp_new_slct.slct_unit[s]=(byte)BufferedSelections[1][s];
			}
		bufferDesc.dwBufferSize = (uint)(sizeof(_DP_NEW_SLCT));
		bufferDesc.pBufferData  = (byte*) &dp_new_slct;
		g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
		}
	else if( ship!=0 && plane==0 )
		{
		// 艦船のみ
		dp_new_slct_ship.dwType = MessageType.SelectShipsOrder;
		dp_new_slct_ship.sw=BufferedSelectOrders[1].IsSet.Value;
		dp_new_slct_ship.the_slct_unit=(byte)BufferedSelectOrders[1].SelectedUnit;
		dp_new_slct_ship.m=(byte)BufferedSelectOrders[1].Unit;
		dp_new_slct_ship.gr_x=(short)BufferedSelectOrders[1].GroundPosition.X;
		dp_new_slct_ship.gr_y=(short)BufferedSelectOrders[1].GroundPosition.Y;
		for( s=0; s<=JPN_SHIP_END-1; s++)
			{
			dp_new_slct_ship.slct_unit[s]=(byte)BufferedSelections[1][s];
			}
		bufferDesc.dwBufferSize = (uint)(sizeof(_DP_NEW_SLCT_SHIP));
		bufferDesc.pBufferData  = (byte*) &dp_new_slct_ship;
		g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1, 0, null, ref hAsync, MUST_SEND );
		}
	else if( ship==0 && plane!=0 )
		{
		// 航空機のみ
		dp_new_slct_plane.dwType = MessageType.SelectPlanesOrder;
		dp_new_slct_plane.sw=BufferedSelectOrders[1].IsSet.Value;
		dp_new_slct_plane.the_slct_unit=(byte)BufferedSelectOrders[1].SelectedUnit;
		dp_new_slct_plane.m=(byte)BufferedSelectOrders[1].Unit;
		dp_new_slct_plane.gr_x=(short)BufferedSelectOrders[1].GroundPosition.X;
		dp_new_slct_plane.gr_y=(short)BufferedSelectOrders[1].GroundPosition.Y;
		for( s=0; s<=JPN_PLANE_END-JPN_PLANE_START; s++)
			{
			dp_new_slct_plane.slct_unit[s]=(byte)BufferedSelections[1][s+JPN_SHIP_END];
			}
		bufferDesc.dwBufferSize = (uint)(sizeof(_DP_NEW_SLCT_PLANE));
		bufferDesc.pBufferData  = (byte*) &dp_new_slct_plane;
		g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
		}
	else if( ship==0 && plane==0 )
		{
		// ユニットに対する指定無し。おそらく輸送船の揚陸先
		dp_new_slct_land.dwType = MessageType.SelectLandOrder;
		dp_new_slct_land.sw=BufferedSelectOrders[1].IsSet.Value;
		dp_new_slct_land.the_slct_unit=(byte)BufferedSelectOrders[1].SelectedUnit;
		dp_new_slct_land.m=(byte)BufferedSelectOrders[1].Unit;
		dp_new_slct_land.gr_x=(short)BufferedSelectOrders[1].GroundPosition.X;
		dp_new_slct_land.gr_y=(short)BufferedSelectOrders[1].GroundPosition.Y;
		bufferDesc.dwBufferSize = (uint)(sizeof(_DP_NEW_SLCT_LAND));
		bufferDesc.pBufferData  = (byte*) &dp_new_slct_land;
		g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
		}
	}

private void SendMoveOrder(ref short s)
	{
	short ship;
	short plane;
	_DP_NEW_PP dp_new_pp;
	_DP_NEW_PP_SHIP dp_new_pp_ship;
	_DP_NEW_PP_PLANE dp_new_pp_plane;
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
		bufferDesc.dwBufferSize = (uint)(sizeof(_DP_NEW_PP_PLANE));
		bufferDesc.pBufferData  = (byte*) &dp_new_pp_plane;
		g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
		}
	}

private bool SendOrders(ref short s, ref _DP_FLAG dp_flag)
	{
	_DP_DATA_1 dp_data_1;
	if(  CONN_DBG==0 && !CanAdvance1 )
		{
		return false;
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
		AreUnitsOutOfSync=false;
		}
	else
		{
		AreUnitsOutOfSync=true;
		if( !HasSavedDesync )
			{
			HasSavedDesync=true;
#if !CONN_DBG
			SaveResume(2);
#endif
			}
		}

	if(TickChecksums[1]==TickChecksums[0])
		{
		IsTickOutOfSync=false;
		}
	else
		{
		IsTickOutOfSync=true;
		if( !HasSavedDesync )
			{
			HasSavedDesync=true;
#if !CONN_DBG
			SaveResume(2);
#endif
			}
		}

	if(RandomChecksums[1]==RandomChecksums[0])
		{
		IsRandomOutOfSync=false;
		}
	else
		{
		IsRandomOutOfSync=true;
		if( !HasSavedDesync )
			{
			HasSavedDesync=true;
#if !CONN_DBG
			SaveResume(2);
#endif
			}
		}

#if !CONN_DBG
#endif

	TickWaits[0]=0;
	TickWaits[1]=0;

	CanAdvance1=false;

	if( BufferedMoveOrders[1].Unit==0 && !BufferedSelectOrders[1].IsSet && BufferedMenuOrders[1].Menu==CombatMenuItem.None && BufferedSystemOrders[1]==0 && BufferedArrivedUnits[1]==0 )
		{
		// 命令が無い場合。
		dp_flag.dwType = MessageType.NoOrder;

		bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
		bufferDesc.pBufferData  = (byte*) Unsafe.AsPointer(ref dp_flag);
		g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

		}
	else
		{
		// まず、このフェーズで溜めた、命令をセンドする。
		if( BufferedSystemOrders[1]!=0 )
			{
			dp_flag.dwType = (MessageType)BufferedSystemOrders[1];

			bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
			bufferDesc.pBufferData  = (byte*) Unsafe.AsPointer(ref dp_flag);
			g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

			SystemOrders[1]=BufferedSystemOrders[1];
			}
		else if( BufferedArrivedUnits[1]!=0 )
			{
			dp_data_1.dwType = MessageType.UnitArrived;
			dp_data_1.data[0] = BufferedArrivedUnits[1];
			bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
			bufferDesc.pBufferData  = (byte*) &dp_data_1;
			g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
			}
		else if(BufferedMoveOrders[1].Unit!=0)
			{
			SendMoveOrder(ref s);
			}
		else if(BufferedSelectOrders[1].IsSet)
			{
			SendSelectOrder(ref s);
			}
		else if(BufferedMenuOrders[1].Menu!=CombatMenuItem.None)
			{
			SendMenuOrder(ref s);
			}
		}
	CanOrder=false;
	return true;
	}

private void BuildBase(ref Unit unit)
	{
	if( unit.IsUsed && ( unit.Kind==UnitKind.AirBase||unit.Kind==UnitKind.NavalBase||unit.Kind==UnitKind.InfantryBase||unit.Kind==UnitKind.Pillboxes||unit.Kind==UnitKind.Fortress ) && unit.BuildTime!=0
		)
		{
		if( unit.Hp<unit.MaxHp )
			{
			// 損傷してればその修理が先。
			if( (Tick%220)==0 && unit.Fuel>=0 )
				unit.Hp++;
			}
		else
			{
			// 損傷がなければ工事
			unit.BuildTime--;

			if( unit.BuildTime==0 )
				{
				// 完成
				unit.MaxHp*=4;
				unit.Hp=unit.MaxHp;
				}
			}
		}
	}

private void UpdateSupply(ref Unit unit)
	{
	RECT wrk_rect;
	int flg;
	int i;
	if( unit.Category==UnitCategory.Ship && unit.IsSupplying && unit.Fuel>=0 )
		{
		// 補給中の艦船
		// ptin dbg
		wrk_rect.top=(int)unit.Position.Y+40+240;
		wrk_rect.right=(int)unit.Position.X+40+240;
		wrk_rect.bottom=(int)unit.Position.Y-40-240;
		wrk_rect.left=(int)unit.Position.X-40-240;

		flg=0;
		for( i=1; i<=MaxUnitId; i++)
			{
			ref var other = ref Units[i];
			if( other.Side==unit.Side && other.Kind==UnitKind.NavalBase && other.BuildTime==0 )
				{
				if( PointInRect3( ref wrk_rect, (int)other.Position.X, (int)other.Position.Y )!=0 )
					{
					flg=1;
					break;
					}
				}
			}

		if( flg==0 )
			unit.SupplyTime=0;
		}

	// ユニットの補給
	if( unit.IsSupplying && unit.Fuel>=0 )
		{
		unit.SupplyTime++;

		if( unit.Hp<unit.MaxHp && (unit.SupplyTime%200)==199 && unit.Fuel>=0 )
			{ unit.Hp++; unit.SupplyTime=1;}

		if( unit.Hp==unit.MaxHp && unit.Ammo<unit.MaxAmmo && (unit.SupplyTime%10)==9 )
			{ unit.Ammo++; unit.SupplyTime=1; }

		if( unit.Hp==unit.MaxHp && unit.Ammo==unit.MaxAmmo && unit.Fuel<100 && (unit.SupplyTime%20)==19 )
			{
			unit.Fuel++;
			if(unit.Fuel>100)
				unit.Fuel=100;
			unit.SupplyTime=1;
			}

		if( unit.Hp==unit.MaxHp && unit.Ammo==unit.MaxAmmo && unit.Fuel==100 && (unit.SupplyTime%100)==99 )
			{
			unit.SupplyTime=0;
			}
		}
	}

private void UpdateParkedPlane(ref Unit unit, int m)
	{
	int cv_1;
	int f = default;
	int i;
	if( unit.IsStopping )
		{
		unit.PathX[0]=Units[unit.Carrier].Position.X;
		unit.PathY[0]=Units[unit.Carrier].Position.Y;
		}
	else
		{
		if( unit.Mode<=UnitMode.Slow  )
			{
			// 発進
			if( unit.DeckPhase==0)
				{
				unit.DeckPhase=1;
				Units[unit.Carrier].PlanesToLaunch++;		// 発艦予定の機数を
				}
			cv_1=Sprites[SpriteId.JapanUnitInfo].X+Sprites[SpriteId.JapanUnitInfo].Width/2;
			if((int)unit.Position.X==cv_1 && unit.DeckPhase==1)
				{
				unit.DeckPhase=2;
				unit.Direction=270.0;
				}
			cv_1=Sprites[SpriteId.JapanUnitInfo].Y+370;
			if((int)unit.Position.Y>=cv_1 && unit.DeckPhase==2)
				{
				unit.DeckPhase=3;
				unit.Direction=90.0;

				Units[unit.Carrier].LaunchCount++;
				Units[unit.Carrier].LaunchCount&=0xffff;

				if( (Units[unit.Carrier].LaunchCount%2)!=0 )
					{
					unit.Position = new WorldPosition(unit.Position.X + 15, unit.Position.Y);
					}
				else
					{
					unit.Position = new WorldPosition(unit.Position.X - 15, unit.Position.Y);
					}
				}
			cv_1=Sprites[SpriteId.JapanUnitInfo].Y+370-80;
			if((int)unit.Position.Y==cv_1 && unit.DeckPhase==3 )
				{
				unit.DeckPhase=4;
				if( unit.Kind!=UnitKind.Bomber && !(unit.Kind==UnitKind.Fighter&&unit.Variant==1) )
					unit.SpriteRow--;
				unit.TakeOffRun=0;
				}

			cv_1=Sprites[SpriteId.JapanUnitInfo].Y+370-120;
			if((int)unit.Position.Y<=cv_1 && unit.DeckPhase>=4)
				{	// 加速します
				unit.TakeOffRun+=1;

				if( unit.TakeOffRun==40 && unit.Carrier==UnitInfoPanel[3] &&  ((UnitKind)UnitInfoPanel[0]==UnitKind.Carrier || (UnitKind)UnitInfoPanel[0]==UnitKind.LightCarrier || (UnitKind)UnitInfoPanel[0]==UnitKind.AirBase ) )
					{
					PlaySoundEffect( 0, SoundId.TakeOff,(double)(MAP_RIGHT+1), 0);
					}

				unit.Position = new WorldPosition(unit.Position.X + (CosDegrees(unit.Direction)*(unit.TakeOffRun/20)), unit.Position.Y);
				unit.Position = new WorldPosition(unit.Position.X, unit.Position.Y - (SinDegrees(unit.Direction)*(unit.TakeOffRun/20)));
				}

			cv_1=Sprites[SpriteId.JapanUnitInfo].Y-30/*+60*/;
			if((int)unit.Position.Y<=cv_1 && unit.DeckPhase>=4 )
				{		// ここで発進はお終い。
				//unit[m].info[3]=100;			// 発進後の最低直線飛行
				unit.DeckPhase=0;			// 発進後の最低直線飛行
				unit.PlaneState=UnitState.Flying;
				//unit[m].info[2]=-1;	// 格納庫の位置、及び、その基地の番機番号
				unit.Position = Units[unit.Carrier].Position;
				unit.Speed=1.0;
				if(Units[unit.Carrier].PlanesToLaunch!=0)
					Units[unit.Carrier].PlanesToLaunch--;		// 発艦予定の機数を減らす。
				Units[unit.Carrier].PlaneCount--;		// 現在格納数
				Units[unit.Carrier].LandingLock=0;		// その空母の次機着艦許可
					// 発進した場合、最初のポイントは空母の方向から決める。
				switch((int)(Units[unit.Carrier].Direction/22.5))
					{
					case 0: case 15:
						unit.Direction=0.0;
						break;
					case 1: case 2:
						unit.Direction=45.0;
						break;
					case 3: case 4:
						unit.Direction=90.0;
						break;
					case 5: case 6:
						unit.Direction=90.0+45.0;
						break;
					case 7: case 8:
						unit.Direction=180.0;
						break;
					case 9: case 10:
						unit.Direction=180.0+45.0;
						break;
					case 11: case 12:
						unit.Direction=270.0;
						break;
					case 13: case 14:
						unit.Direction=270.0+45.0;
						break;
					}
				}
			if(unit.PlaneState==UnitState.Parked)
				{
				unit.Position = new WorldPosition(unit.Position.X + (CosDegrees(unit.Direction)*0.8), unit.Position.Y);
				unit.Position = new WorldPosition(unit.Position.X, unit.Position.Y - (SinDegrees(unit.Direction)*0.8));
				}
			}
		else
			{
			// 着陸
			cv_1=Sprites[SpriteId.JapanUnitInfo].Y+Sprites[SpriteId.JapanUnitInfo].Height-150;
			if((int)unit.Position.Y<=cv_1 && unit.DeckPhase==1)
				{
				unit.DeckPhase=2;
				unit.Speed=1.5;
				Units[unit.Carrier].LandingLock=0;		// その空母の次機着艦許可
				}
			cv_1=Sprites[SpriteId.JapanUnitInfo].Y+Sprites[SpriteId.JapanUnitInfo].Height-200;
			if((int)unit.Position.Y<=cv_1 && unit.DeckPhase==2)
				{
				unit.DeckPhase=3;
				unit.Speed=unit.Speed/2;
				if( unit.Kind!=UnitKind.Bomber && !(unit.Kind==UnitKind.Fighter&&unit.Variant==1) )
					unit.SpriteRow++;
				}
			cv_1=Sprites[SpriteId.JapanUnitInfo].Y+Sprites[SpriteId.JapanUnitInfo].Height-270;
			if( (int)unit.Position.Y<=cv_1 && unit.DeckPhase==3 )
				{
				// 着艦終了
				unit.DeckPhase=0;

				unit.AirSuperioritySortie=0;					// 戦闘機の場合は制空出撃フラグ

				unit.Mode=UnitMode.Move;				// モード（コンバットメニュー）
				SetParkingPosition(m);
				unit.IsStopping=true;

				unit.Weapon=FireKind.Maintenance;					// 武装品種
				unit.Ammo=1;						// 数
				unit.ReloadTime=TUNE_SPAN+(unit.MaxHp-unit.Hp)*(TUNE_SPAN/10)+((unit.Kind==UnitKind.Bomber ? 1 : 0)*(TUNE_SPAN/3));		// 数
				unit.Hp=unit.MaxHp;

				unit.PathX[0]=Units[unit.Carrier].Position.X;
				unit.PathY[0]=Units[unit.Carrier].Position.Y;
				unit.PathX[1]=MAP_RIGHT+1;

				f=0;
				for( i=1; i<=MaxUnitId; i++)
					{
					ref var other = ref Units[i];
					if( i!=m && other.IsUsed && other.Category==UnitCategory.Plane && other.Side==LocalSide && other.DeckPhase!=0
						&& other.PlaneState==UnitState.Parked && unit.Carrier==other.Carrier && !other.IsStopping )
						f++;
					}

				if( f==0 )
					{	// 滑走路に他の着陸機がなければ発進許可。
					Units[unit.Carrier].LaunchLock=0;		// その空母の次機発進許可
					}
else
	Units[unit.Carrier].LaunchLock=2+f;		// 空母の発進不可の調査のため

				}
			if(unit.PlaneState==UnitState.Parked)
				{
				unit.Position = new WorldPosition(unit.Position.X + (CosDegrees(unit.Direction)*unit.Speed), unit.Position.Y);
				unit.Position = new WorldPosition(unit.Position.X, unit.Position.Y - (SinDegrees(unit.Direction)*unit.Speed));
				}
			}
		}
	}

private void SteerUnit(ref Unit unit, int m)
	{
	RECT wrk_r;
	int n;
	if( 1!=0 )
		{
		// 戦闘機動および、緊急移動
		if( !unit.IsSupplying )
			{
			switch( unit.Kind )
				{
				case UnitKind.Cruiser:
				case UnitKind.Destroyer:
				case UnitKind.Battleship:
				case UnitKind.Carrier:
				case UnitKind.LightCarrier:
					if( unit.EmergencyFlags[0]==0 )
						SetShipEmergencyDestination(m);
					break;

				case UnitKind.Transport:
					if( unit.EmergencyFlags[0]==0 && unit.Target!=0 )
						SetTransportLandingDestination(m);
					if( unit.EmergencyFlags[0]==0  )
						SetShipEmergencyDestination(m);
					break;

				case UnitKind.Submarine:
					if( unit.EmergencyFlags[0]==0  )
						ReturnIntoWorld(m);
					break;

				case UnitKind.Fighter:
					SetFighterEmergencyDestination(m);
					if( unit.Target!=0 )
						{
						SetFighterAttackDestination(m);	// 攻撃目標あり
						}
					else
						{
						if( unit.EmergencyFlags[0]==0 && unit.Mode==UnitMode.Return )
							SetAttackerEmergencyDestination(m);
						}
					break;

				case UnitKind.Attacker:
				case UnitKind.Bomber:
					if( unit.Target!=0 && unit.GroupLeader==0 )
						SetAttackerAttackDestination(m);
					if( unit.EmergencyFlags[0]==0 )
						SetAttackerEmergencyDestination(m);

					if ( unit.Kind==UnitKind.Bomber && (Units[unit.Carrier].Kind!=UnitKind.AirBase) )
						{
						unit.Carrier=0;
						}
					break;
				}
			}

		if( unit.GroupLeader!=0 && !Units[unit.GroupLeader].IsUsed )
			unit.GroupLeader=0;

		if( unit.GroupLeader==0 )
			{
			// 単独、もしくは、編隊長
			if(!unit.IsStopping )
				{
				if(  unit.Category==UnitCategory.Plane && unit.Mode==UnitMode.Return && unit.DeckPhase==1 )
					{
					UpdateLanding(m);
					}

				// ptin dbg
				wrk_r.top=(int)unit.PathY[0]+ON_PP;
				wrk_r.right=(int)unit.PathX[0]+ON_PP;
				wrk_r.bottom=(int)unit.PathY[0]-ON_PP;
				wrk_r.left=(int)unit.PathX[0]-ON_PP;

				if( PointInRect3(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)!=0 )
					{
					if( unit.PathX[1]!=MAP_RIGHT+1 )
						{
						// ＰＰ＿ＸＹを一つずつずらす
						for(n=0; unit.PathX[n]!=MAP_RIGHT+1; n++)
							{
							unit.PathX[n]=unit.PathX[n+1];
							unit.PathY[n]=unit.PathY[n+1];
							}
						}
					else
						{
						// ＰＰの再終点に到着
						if( unit.Category==UnitCategory.Ship )
							{
							unit.IsStopping=true;
							}
						else
							{
							if( unit.Category==UnitCategory.Plane && unit.Mode==UnitMode.Return )
								{
								unit.DeckPhase=1;
								SetLandingDestination(m);
								for(n=1;n<=MaxUnitId;n++)
									{
									ref var other = ref Units[n];
									if( other.IsUsed && other.GroupLeader==m )
										{
										other.GroupLeader=0;
										other.PathX[0]+=(double)(Random(600)-300);
										other.PathY[0]+=(double)(Random(600)-300);
										other.PathX[1]=MAP_RIGHT+1;
										}
									}
								if( unit.IsGroupLeader!=0 )
									{
									unit.IsGroupLeader=0;	unit.FormationNumber=0;
									}
								}
							}
						}
					}
				}
			else
				{
				unit.PathX[0]=unit.Position.X;
				unit.PathY[0]=unit.Position.Y;
				}
			}
		else
			{
			// 編隊追随機
			if(!unit.IsStopping)
				{
				if( (Tick%10)==0 )
					{
					SetDynamicDestination(m);
					}
				}
			else
				{
				unit.PathX[0]=unit.Position.X;
				unit.PathY[0]=unit.Position.Y;
				}

			// ptin dbg
			wrk_r.top=(int)unit.PathY[0]+30;
			wrk_r.right=(int)unit.PathX[0]+30;
			wrk_r.bottom=(int)unit.PathY[0]-30;
			wrk_r.left=(int)unit.PathX[0]-30;

			if( PointInRect3(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)==0 && !unit.IsStopping)
				{	// 編隊指定位置に無し
				unit.ForGroupLeader=0;
				if( Units[unit.GroupLeader].ForGroupLeader==0 )
					Units[unit.GroupLeader].ForGroupLeader=1;

/*
if( m==131 && unit[129].arm2[0]==52-1 )
{
dbg[2]=unit[unit[m].ltl_ldr].for_form_spd*100000;		// この時点で値が　０と１９００００
dbg[3]=unit[m].max_spd*100000;
}
*/

				if( Units[unit.GroupLeader].FormationSpeed > unit.MaxSpeed || Units[unit.GroupLeader].FormationSpeed==0 )
					{
					Units[unit.GroupLeader].FormationSpeed = unit.MaxSpeed;
					}

				}
			else
				{	// 編隊指定位置にあり
				unit.ForGroupLeader=2;
				if( unit.Category==UnitCategory.Ship && Units[unit.GroupLeader].IsStopping.Value==1 )
					{
					unit.IsStopping=true;
					}

				}

			}
		}
	}

private void TryLandOnCarrier(ref Unit unit, int m)
	{
	RECT wrk_r;
	int n;
	if( 0!=0 && Units[unit.Carrier].PlanesToLaunch!=0)
		{
		// ほんまにＩｎｆｏ［４］があるんやなチェック
		}

	if( ToEightDirections((int)unit.Direction)==ToEightDirections((int)Units[unit.Carrier].Direction)
	&& Units[unit.Carrier].Capacity>Units[unit.Carrier].PlaneCount
	&& Units[unit.Carrier].PlanesToLaunch<=0 )
		{
		// ptin dbg
		wrk_r.top=(int)Units[unit.Carrier].Position.Y+ON_PP/2;
		wrk_r.right=(int)Units[unit.Carrier].Position.X+ON_PP/2;
		wrk_r.bottom=(int)Units[unit.Carrier].Position.Y-ON_PP/2;
		wrk_r.left=(int)Units[unit.Carrier].Position.X-ON_PP/2;

		if( PointInRect3(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)!=0 )
			{
			// 着艦
			Units[unit.Carrier].PlaneCount++;	// 現在格納数
			if(m==SelectedUnit)
				{
				SelectedUnit=0; PreviousSelectedUnit=(short)unit.Carrier; CombatMenuKind=0; CombatMenuSelection=CombatMenuItem.None; ClearSelection();
				}
			if(m==PreviousSelectedUnit)
				{
				PreviousSelectedUnit=(short)unit.Carrier;
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

			//unit[m].info[1]=4;				// 所属の空母、及び、基地の番号
			unit.ParkingNumber=FindParkingNumber(m);	// 格納庫の位置、及び、その基地の番機番号
			unit.DeckPhase=1;					// 格納庫、基地での移動情態
			unit.TakeOffRun=0;					// 減速度をクリア
			//unit[m].info[5]=MOVE;				// モード（コンバットメニュー）
			Units[unit.Carrier].LandingLock=1;	// 着艦、0許可、1不許可
			Units[unit.Carrier].LaunchLock=1;	// その空母の次機発進許可	0許可、1不許可
			unit.PlaneState=UnitState.Parked;
			unit.Position = new WorldPosition(Sprites[SpriteId.JapanUnitInfo].X+Sprites[SpriteId.JapanUnitInfo].Width/2+(SharedRandom(16)-7), Sprites[SpriteId.JapanUnitInfo].Y+Sprites[SpriteId.JapanUnitInfo].Height+40);
			unit.Speed=5.0; unit.Direction=90.0;

			unit.Target=0;					//

			unit.EmergencyFlags[0]=0;

			}
		}
	}

private void SetAcceleration(ref Unit unit, double pp_drctn, int land)
	{
	int n;
	double wrk_x;
	double wrk_y;
	double drctn;
	double dstc;
	if( unit.Category!=UnitCategory.Plane && (pp_drctn>=80.0 || land!=0 ) )
		{
		if( ( unit.MinSpeed ) < unit.Speed || land!=0 )
			{
			unit.Acceleration=-unit.AccelerationChange;
			}
		else
			{
			unit.Acceleration=+unit.AccelerationChange;
			}
		}
	else
		{
		if(pp_drctn>=45.0  )
			{

			if((unit.MaxSpeed+(unit.Kind==UnitKind.Fighter && unit.Target!=0 ? 1 : 0)*CMBT_SPD)/2 < unit.Speed  )
				{
				unit.Acceleration=-unit.AccelerationChange;
				}
			else
				{
				unit.Acceleration=+unit.AccelerationChange;
				}
			}
		else
			{

			if(pp_drctn>=22.5 )
				{

				if( ((unit.MaxSpeed+(unit.Kind==UnitKind.Fighter && unit.Target!=0 ? 1 : 0)*CMBT_SPD)/3)*2 < unit.Speed )
					{
					unit.Acceleration=-unit.AccelerationChange;
					}
				else
					{
					unit.Acceleration=+unit.AccelerationChange;
					}
				}
			else
				{

				if( unit.EmergencyFlags[0]!=0 )
					{
					//　緊急移動の場合
					unit.Acceleration=+unit.AccelerationChange;
					unit.ForGroupLeader=0;
					}
				else
					{
					//　通常移動
					if( unit.ForGroupLeader==1 )
						{	// 随伴機より。速度落とせの連絡 この場合ｍ番は編隊長

// この時点でfor_form_spdがちがう

						n=0;

						if( unit.Target!=0 && Units[unit.Target].IsFound ) //&& unit[m].kind==AT1 )
							{
							n=1;
							wrk_x=Units[unit.Target].Position.X-unit.Position.X;
							wrk_y=Units[unit.Target].Position.Y-unit.Position.Y;

							drctn=Direction(wrk_x, wrk_y);
							if(wrk_x<0)		wrk_x=0-wrk_x;
							if(wrk_y<0)		wrk_y=0-wrk_y;
							if(drctn>=180)	drctn=drctn-180;
							if(drctn>=90)	drctn=90-(drctn-90);
							dstc=(wrk_x)/(CosDegrees(drctn));

							if( dstc<=BB1_SIGHT )
								{
								n=1;		// 速度落とす要無し
								}
							}
						if( n==0 && unit.Speed>=(unit.FormationSpeed*(0.60-(unit.Kind==UnitKind.Carrier ? 1 : 0)*0.15 ))+unit.AccelerationChange )
							{
							unit.Acceleration=-unit.AccelerationChange;
							}
						else
							{
							unit.Acceleration=+unit.AccelerationChange;

							}

						unit.ForGroupLeader=0;
						unit.FormationSpeed=0.0;

						}
					else
						{
						if( unit.ForGroupLeader==2 && unit.MaxSpeed >= Units[unit.GroupLeader].MaxSpeed )
							{ // 編隊指定位置にいる。編隊Ｌｄｒの速度に合わせよ
							unit.Speed=Units[unit.GroupLeader].Speed;
							unit.Acceleration=0;
							}
						else
							{	// 単独機か、連絡無しの指揮機
							unit.Acceleration=+unit.AccelerationChange;
							}
						unit.ForGroupLeader=0;
						}
					}
				}
			}
		}
	}

private void CheckCourseAhead(ref Unit unit, int m, ref int land, double pp_drctn)
	{
	double wrk_x2;
	double wrk_y2;
	int n;
	RECT wrk_r;
	int cm_scrn_x;
	int cm_scrn_y;
	wrk_x2=unit.Position.X;
	wrk_y2=unit.Position.Y;
	wrk_x2+=CosDegrees(unit.Direction)*(40+unit.MaxSpeed*10);
	wrk_y2+=SinDegrees(unit.Direction)*(40+unit.MaxSpeed*10);
	if(  !( unit.Kind==UnitKind.Submarine && unit.IsSubmerged ) )
		{
		for( n=1; n<=MaxUnitId; n++)
			{
			ref var other = ref Units[n];
			if(other.IsUsed && m!=n && other.Category==UnitCategory.Ship && !(other.Kind==UnitKind.Submarine && other.IsSubmerged)  && !(other.Kind>=UnitKind.AirBase&&other.Kind<=UnitKind.Fortress) )
				{
				// ptin dbg
				wrk_r.top=(int)other.Position.Y+(Sprites[SpriteId.JapanUnits].Height/2);
				wrk_r.right=(int)other.Position.X+(Sprites[SpriteId.JapanUnits].Width/2);
				wrk_r.bottom=(int)other.Position.Y-(Sprites[SpriteId.JapanUnits].Height/2);
				wrk_r.left=(int)other.Position.X-(Sprites[SpriteId.JapanUnits].Width/2);

				if( PointInRect3(ref wrk_r,(int)wrk_x2,(int)wrk_y2)!=0)
					{
					// 前方に艦船！
					land=1;

					unit.EmergencyFlags[0]=0;

					break;
					}
				}
			}
		}

	// ＰＰ方向に陸地があるか
	if( land==0  )
		{
		wrk_x2=unit.Position.X;
		wrk_y2=unit.Position.Y;
		wrk_x2+=CosDegrees(pp_drctn)*80;
		wrk_y2+=SinDegrees(pp_drctn)*80;

		if(!( wrk_y2>MAP_TOP || wrk_y2<MAP_BOTTOM || wrk_x2<MAP_LEFT || wrk_x2>MAP_RIGHT ))
			{
			cm_scrn_x=(int)((wrk_x2+(Sprites[SpriteId.JapanUnits].Width/2)-MAP_LEFT)/Sprites[SpriteId.MapTiles].Width);
			cm_scrn_y=(int)((MAP_TOP-wrk_y2+(Sprites[SpriteId.JapanUnits].Height/2))/Sprites[SpriteId.MapTiles].Height);
			if( MapTiles[cm_scrn_y][cm_scrn_x]>=1 && MapTiles[cm_scrn_y][cm_scrn_x]<=9 )
				{
				land=1;
				}
			}
		}

	if( unit.EmergencyFlags[0]!=0 && land!=0)
		{
		// 緊急移動の取り消し
		unit.EmergencyFlags[0]=0;
		}
	}

private void MoveUnit(ref Unit unit, int m, ref double wrk_3)
	{
	RECT wrk_r;
	double wrk_x;
	double wrk_y;
	double pp_drctn;
	int land;
	int n;
	if( (!unit.IsStopping || unit.EmergencyFlags[0]!=0)  && !(unit.Category==UnitCategory.Ship && unit.IsSupplying) )
		{
		if( unit.EmergencyFlags[0]!=0	)
			{
			// 緊急移動先についているか
			// ptin dbg
			wrk_r.top=(int)unit.EmergencyDestination.Y+ON_PP;
			wrk_r.right=(int)unit.EmergencyDestination.X+ON_PP;
			wrk_r.bottom=(int)unit.EmergencyDestination.Y-ON_PP;
			wrk_r.left=(int)unit.EmergencyDestination.X-ON_PP;

			if( PointInRect3(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)!=0 )
				{
				unit.EmergencyFlags[0]=0;
				// 通常移動		直前定点へ！
				wrk_x=unit.PathX[0]-unit.Position.X;
				wrk_y=unit.PathY[0]-unit.Position.Y;
				}
			else
				{
				// 緊急移動先がある場合
				wrk_x=unit.EmergencyDestination.X-unit.Position.X;
				wrk_y=unit.EmergencyDestination.Y-unit.Position.Y;
				}
			}
		else
			{
			// 通常移動		直前定点へ！
			wrk_x=unit.PathX[0]-unit.Position.X;
			wrk_y=unit.PathY[0]-unit.Position.Y;
			}

		pp_drctn=Direction(wrk_x, wrk_y);

		land=0;
		if( unit.Category==UnitCategory.Ship )
			{
			// 艦首方向に他の艦船があるか
			CheckCourseAhead(ref unit, m, ref land, pp_drctn);
			}

		pp_drctn=pp_drctn-unit.Direction;
		if(pp_drctn<0)
			pp_drctn=360+pp_drctn;

		unit.TurnRate=0;
		if(pp_drctn>=1.0&&pp_drctn<=180.0)
			{
			//	左へ
			unit.TurnRate=unit.TurnRateChange;
			if(pp_drctn <= 3.0)
				unit.TurnRate = +0.5;		// 要は微調整	//unit[m].a_drctn_add;
			}
		if(pp_drctn<=359.0 && pp_drctn>180.0)
			{
			//	右へ
			unit.TurnRate=-unit.TurnRateChange;
			if(pp_drctn >= 357.0)
				unit.TurnRate = -0.5;		// 要は微調整	//-unit[m].a_drctn_add;
			}

		unit.Direction+=unit.TurnRate;

		if(unit.Direction>=360.0)
			unit.Direction-=360.0;
		if(unit.Direction<0.0)
			unit.Direction=360.0+unit.Direction;

		if(pp_drctn>180.0)
			pp_drctn=360.0-pp_drctn;

		// ユニットのスピード
		SetAcceleration(ref unit, pp_drctn, land);
		}
	else
		{
		if(unit.Speed>0)
			{
			unit.Acceleration=-(unit.AccelerationChange*2);

			}
		}

	if( unit.Fuel<=0 && (unit.Category==UnitCategory.Plane || (unit.Category==UnitCategory.Ship && unit.Speed>= unit.MaxSpeed/10 ) ) )
		unit.Acceleration=-(unit.AccelerationChange*2);		// ガス０なら減速へ

	// 速度を決定
	unit.Speed+=unit.Acceleration;

	// 最高速度の制限
	if( (unit.MaxSpeed+(unit.Kind==UnitKind.Fighter && unit.Target!=0 ? 1 : 0)*CMBT_SPD) < unit.Speed )
		{
		unit.Speed-=unit.AccelerationChange*8;
		if( unit.Speed < (unit.MaxSpeed+(unit.Kind==UnitKind.Fighter && unit.Target!=0 ? 1 : 0)*CMBT_SPD) )
			unit.Speed = (unit.MaxSpeed+(unit.Kind==UnitKind.Fighter && unit.Target!=0 ? 1 : 0)*CMBT_SPD);
		}

	if( unit.Kind==UnitKind.Transport && unit.Ammo!=0 )
		{
		// 輸送船でなんかつんでると最高速度がおちる
		switch( unit.Weapon )
			{
			case FireKind.CargoInfantryBase:	wrk_3=1.0;		break;
			case FireKind.CargoPillboxes:	wrk_3=0.9;		break;
			case FireKind.CargoFortress:	wrk_3=0.9;		break;
			case FireKind.CargoAirBase:		wrk_3=0.8;		break;
			case FireKind.CargoNavalBase:		wrk_3=0.8;		break;
			}

		if( unit.MaxSpeed*wrk_3 < unit.Speed )
			{
			unit.Speed-=unit.AccelerationChange*8;
			}

		}

	if(  unit.Kind==UnitKind.Submarine && unit.IsSubmerged.Value==1 && unit.Speed>(unit.MaxSpeed*0.7) )
		{
		unit.Speed=(unit.MaxSpeed*0.7);
		}

	if( (!unit.IsStopping || unit.EmergencyFlags[0]!=0) &&!(unit.Fuel<=0))
		{
		if( unit.MinSpeed > unit.Speed )
			unit.Speed=unit.MinSpeed;
		}
	else
		{
		if( unit.Speed < 0 )
			unit.Speed=0;
		}

	// Em_flgがあるならデクリ
	if(unit.EmergencyFlags[0]!=0)
		unit.EmergencyFlags[0]--;

	// 着艦チェック
	if( unit.Category==UnitCategory.Plane && unit.Mode==UnitMode.Return && unit.DeckPhase==1 && Units[unit.Carrier].IsUsed
		&& Units[unit.Carrier].LandingLock==0 && !(unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Flying && Units[unit.Carrier].Hp<=Units[unit.Carrier].MaxHp*0.2)
		&& !( unit.Kind==UnitKind.Bomber && (Units[unit.Carrier].Kind!=UnitKind.AirBase) )
		&& !( unit.Kind==UnitKind.Fighter && unit.Variant==1 && (Units[unit.Carrier].Kind!=UnitKind.AirBase) )
		&& unit.Side==Units[unit.Carrier].Side && Units[unit.Carrier].BuildTime==0
		)
		{
		TryLandOnCarrier(ref unit, m);
		}

	// 新座標を設定
	unit.Position += new WorldVector(CosDegrees(unit.Direction)*unit.Speed, SinDegrees(unit.Direction)*unit.Speed);

	// 燃料消費
	n=(int)unit.FuelInterval;
	if( unit.Kind==UnitKind.Attacker && unit.Ammo!=0 && (unit.Weapon==FireKind.Torpedo || unit.Weapon==FireKind.Bomb))
		n=n-(n/10);
	if( unit.Kind==UnitKind.Fighter && unit.Target!=0 && unit.MaxSpeed < unit.Speed )
		n=n-(n/10);

	if( unit.FuelInterval>=1 && (Tick%n)==0 && unit.Fuel>0 && unit.Speed>0)
		{
		unit.Fuel-=(unit.Speed/unit.MaxSpeed);
		if(unit.Fuel<0)
			unit.Fuel=0;
		}

	if( unit.Fuel==0 && unit.Category==UnitCategory.Plane && unit.Speed<=0 )
		unit.Hp=0;							// 飛行機でガス０なら落ちます
	}

private void ControlInfantryBaseFiring(ref Unit unit, int dmg_act, int m)
	{
	unit.Ammo=unit.MaxAmmo;

	// 艦砲 自動
	if( unit.Ammo>=1 && Random(250*dmg_act)==0 )
		{
		FireWeapons(m,0,FireKind.Gun);
		}
	// 艦砲　選択
	// 対空砲弾 自動砲撃
	if( unit.Ammo>=1 && Random(200*dmg_act)==0 )
		{
		FireWeapons(m,0,FireKind.AntiAircraftShell);
		}
	// 対空機関砲 X 2
	if( Random(180*dmg_act)==0 && unit.Side==Side.UnitedStates )
		FireWeapons(m,0,FireKind.RapidAntiAircraftShell);
	// 対空機銃
	if( Random(10*dmg_act)==0 )
		FireWeapons(m,0,FireKind.Bullet);
	}

private void ControlPillboxesFiring(ref Unit unit, int dmg_act, int m)
	{
	unit.Ammo=unit.MaxAmmo;
	// 艦砲 自動
	if( unit.Ammo>=1 && Random( 200*dmg_act )==0 )
		{
		FireWeapons(m,0,FireKind.Gun);
		}
	// 艦砲　選択
	// 対空砲弾 自動砲撃
	if( unit.Ammo>=1 && Random(160*dmg_act)==0 )
		{
		FireWeapons(m,0,FireKind.AntiAircraftShell);
		}
	// 対空機関砲
	if( Random(140*dmg_act)==0 && unit.Side==Side.UnitedStates )
		{
		FireWeapons(m,0,FireKind.RapidAntiAircraftShell);
		}
	// 対空機銃
	if( Random(10*dmg_act)==0 )
		{
		FireWeapons(m,0,FireKind.Bullet);
		}
	}

private void ControlFortressFiring(ref Unit unit, int dmg_act, int m)
	{
	unit.Ammo=unit.MaxAmmo;

	// 艦砲 自動
	if( unit.Ammo>=1 && Random(150*dmg_act)==0 )
		{
		FireWeapons(m,0,FireKind.Gun);
		}
	// 艦砲　選択
	// 対空砲弾 自動砲撃
	if( unit.Ammo>=1 && Random(120*dmg_act)==0 )
		{
		FireWeapons(m,0,FireKind.AntiAircraftShell);
		}
	// 対空機関砲
	if( Random(120*dmg_act)==0 && unit.Side==Side.UnitedStates )
		FireWeapons(m,0,FireKind.RapidAntiAircraftShell);
	// 対空機関砲 X 2
	if( Random(160*dmg_act)==0 && unit.Side==Side.UnitedStates )
		FireWeapons(m,0,FireKind.RapidAntiAircraftShell);
	// 対空機銃
	if( Random(8*dmg_act)==0 )
		FireWeapons(m,0,FireKind.Bullet);
	}

private void ControlBattleshipFiring(ref Unit unit, int dmg_act, int m)
	{
	if( unit.Side==Side.Japan && unit.Variant==1 )
		{
		// 大和級
		// 艦砲 自動
		if( unit.Ammo>=1 && Random(350*dmg_act)==0 && (unit.Target==0 || Units[unit.Target].Category==UnitCategory.Ship) )
			{
			FireWeapons(m,0,FireKind.Gun);
			}
		// 艦砲　選択
		if(unit.Ammo>=1 && Random(300*dmg_act)==0 && unit.Target!=0 )
			{
			FireWeapons(m,unit.Target,FireKind.NavalBaseGun);
			}

		if( unit.Ammo>=1 && Random(180*dmg_act)==0 && unit.Target!=0 )
			{
			FireWeapons(m,unit.Target,FireKind.AntiAircraftShell);		// 対空砲弾 選択
			}

		if( unit.Ammo>=1 && Random(95*dmg_act)==0 )
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
		if( unit.Ammo>=1 && Random(350*dmg_act)==0 && (unit.Target==0 || Units[unit.Target].Category==UnitCategory.Ship) )
			{
			FireWeapons(m,0,FireKind.Gun);
			}
		// 艦砲　選択
		if(unit.Ammo>=1 && Random(300*dmg_act)==0 && unit.Target!=0 )
			{
			FireWeapons(m,unit.Target,FireKind.Gun);
			}

		if( unit.Ammo>=1 && Random(190*dmg_act)==0 && unit.Target!=0 )
			{
			FireWeapons(m,unit.Target,FireKind.AntiAircraftShell);		// 対空砲弾 選択
			}

		if( unit.Ammo>=1 && Random(110*dmg_act)==0 )
			{
			FireWeapons(m,0,FireKind.AntiAircraftShell);							// 対空砲弾 自動砲撃
			}

		// 対空機関砲
		if( Random(60*dmg_act)==0 && unit.Side==Side.UnitedStates )
			FireWeapons(m,0,FireKind.RapidAntiAircraftShell);
		// 対空機関砲 X 2
		if( Random(120*dmg_act)==0 && unit.Side==Side.UnitedStates )
			FireWeapons(m,0,FireKind.RapidAntiAircraftShell);

		// 対空機銃
		if( Random(5*dmg_act)==0 )
			FireWeapons(m,0,FireKind.Bullet);
		}
	}

private void ControlCruiserFiring(ref Unit unit, int dmg_act, int m)
	{
	if( unit.Variant==0 )
		{
		// 巡洋艦
		// 艦砲 自動
		if( unit.Ammo>=1  && Random(350*dmg_act)==0  && (unit.Target==0 || Units[unit.Target].Category==UnitCategory.Ship) )
			{
			FireWeapons(m,0,FireKind.Gun);
			}
		// 艦砲 選択
		if( unit.Ammo>=1  && Random(300*dmg_act)==0 && unit.Target!=0 )
			{
			FireWeapons(m,unit.Target,FireKind.Gun);
			}

		// 対空砲弾
		if( unit.Ammo>=1 && Random(120*dmg_act)==0  )
			{
			if(unit.Target!=0)								// 対空砲弾 選択
				FireWeapons(m,unit.Target,FireKind.AntiAircraftShell);
			else
				FireWeapons(m,0,FireKind.AntiAircraftShell);							// 対空砲弾 自動砲撃
			}

		// 対空機関砲
		if( unit.Ammo>=1 && Random(60*dmg_act)==0 && unit.Side==Side.UnitedStates )
			FireWeapons(m,0,FireKind.RapidAntiAircraftShell);

		// 対空機銃 自動
		if( Random(10*dmg_act)==0  )
			FireWeapons(m,0,FireKind.Bullet);
		// 魚雷
		if( unit.Random40[0]==Tick%(40*dmg_act) && unit.Ammo>=1 && unit.ReloadTime<=0 && unit.Side==Side.Japan )
			{
			FireWeapons(m,0,FireKind.Torpedo);
			}
		}
	else
		{
		// 防空巡洋艦
		// 艦砲 自動
		if( unit.Ammo>=1  && Random(800*dmg_act)==0  && (unit.Target==0 || Units[unit.Target].Category==UnitCategory.Ship) )
			{
			FireWeapons(m,0,FireKind.Gun);
			}

		// 対空砲弾
		if( unit.Ammo>=1 && Random(100*dmg_act)==0 )
			{
			// 自動
			FireWeapons(m,0,FireKind.AntiAircraftShell);							// 対空砲弾 自動砲撃
			}

		if( unit.Ammo>=1 && Random(180*dmg_act)==0 && unit.Target!=0 )
			{
			// 選択
			FireWeapons(m,unit.Target,FireKind.AntiAircraftShell);
			}

		// 対空機関砲
		if( unit.Ammo>=1 && Random(55*dmg_act)==0 && unit.Side==Side.UnitedStates )
			FireWeapons(m,0,FireKind.RapidAntiAircraftShell);

		// 対空機銃 自動
		if( Random(10*dmg_act)==0  )
			FireWeapons(m,0,FireKind.Bullet);

		}
	}

private void ControlDestroyerFiring(ref Unit unit, int dmg_act, int m)
	{
	if( unit.Variant==0 )
		{
		// 艦砲 自動
		if( unit.Ammo>=1 && Random(200*dmg_act)==0 )
			{
			FireWeapons(m,0,FireKind.Gun);
			}

		// 魚雷
		if( unit.Random40[0]==Tick%(40*dmg_act) && unit.Ammo>=1 && unit.ReloadTime<=0 )
			{
			FireWeapons(m,0,FireKind.Torpedo);
			}

		// 爆雷
		if( unit.Ammo>=1 && (Tick%(35*dmg_act))==0 )
			{
			FireWeapons(m,0,FireKind.AntiSubmarineBomb);
			}
		// 対空機銃
		if( Random(15*dmg_act)==0 )
			FireWeapons(m,0,FireKind.Bullet);
		}
	else
		{
		// 艦砲 自動
		if( unit.Ammo>=1 && Random(500*dmg_act)==0 )
			{
			FireWeapons(m,0,FireKind.Gun);
			}

		// 対空機銃
		if( Random(20*dmg_act)==0 )
			FireWeapons(m,0,FireKind.Bullet);

		// 爆雷
		if( unit.Ammo>=1 && (Tick%(25*dmg_act))==0 )
			{
			FireWeapons(m,0,FireKind.AntiSubmarineBomb);
			}

		}
	}

private void ControlTransportFiring(ref Unit unit, int m)
	{
	if( unit.Weapon==FireKind.CargoNavalBase && unit.Ammo>=1  && unit.Target==MaxUnitId+1)
		FireWeapons(m,0,FireKind.CargoNavalBase);
	if( unit.Weapon==FireKind.CargoAirBase && unit.Ammo>=1  && unit.Target==MaxUnitId+1)
		FireWeapons(m,0,FireKind.CargoAirBase);

	if( unit.Weapon==FireKind.CargoInfantryBase && unit.Ammo>=1  && unit.Target==MaxUnitId+1)
		FireWeapons(m,0,FireKind.CargoInfantryBase);

	if( unit.Weapon==FireKind.CargoPillboxes && unit.Ammo>=1  && unit.Target==MaxUnitId+1)
		FireWeapons(m,0,FireKind.CargoPillboxes);
	if( unit.Weapon==FireKind.CargoFortress && unit.Ammo>=1  && unit.Target==MaxUnitId+1)
		FireWeapons(m,0,FireKind.CargoFortress);
	}

private void ControlFiring(ref Unit unit, int dmg_act, int m)
	{
	if( !IsEditingMap && !unit.IsSupplying && (( unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Flying )||( unit.Category==UnitCategory.Ship)))
		{
		switch( unit.Kind )
			{
			case UnitKind.InfantryBase:
				// 地上基地は弾が減りません
				ControlInfantryBaseFiring(ref unit, dmg_act, m);
				break;

			case UnitKind.Pillboxes:
				// 地上基地は弾が減りません
				ControlPillboxesFiring(ref unit, dmg_act, m);
				break;

			case UnitKind.Fortress:
				// 地上基地は弾が減りません
				ControlFortressFiring(ref unit, dmg_act, m);
				break;

			case UnitKind.Battleship:
				ControlBattleshipFiring(ref unit, dmg_act, m);

				break;

			case UnitKind.Cruiser:
				ControlCruiserFiring(ref unit, dmg_act, m);
				break;

			case UnitKind.Carrier:
				// 対空機関砲
				if( unit.Ammo>=1 && Random(80*dmg_act)==0 && unit.Side==Side.UnitedStates )
					FireWeapons(m,0,FireKind.RapidAntiAircraftShell);
				// 対空機銃
				if( Random(15*dmg_act)==0)
					FireWeapons(m,0,FireKind.Bullet);
				break;

			case UnitKind.Destroyer:
				ControlDestroyerFiring(ref unit, dmg_act, m);
				break;

			case UnitKind.LightCarrier:
				// 対空機関砲
				if( unit.Ammo>=1 && Random(100*dmg_act)==0 && unit.Side==Side.UnitedStates )
					FireWeapons(m,0,FireKind.RapidAntiAircraftShell);
				// 対空機銃
				if( Random(20*dmg_act)==0 )
					FireWeapons(m,0,FireKind.Bullet);
				break;

			case UnitKind.Transport:
				// トランスボーと
				ControlTransportFiring(ref unit, m);

				break;

			case UnitKind.Submarine:
				// 艦砲 自動
				// 魚雷
				if( unit.Ammo>=1 && unit.ReloadTime<=0 && unit.Target!=0  )
					{
					FireWeapons(m,unit.Target,FireKind.Torpedo);
					}
				break;

			case UnitKind.Fighter:
				// 戦闘機の場合は、前方に敵航空機が飛んでればとりあえず撃つ
				if( unit.Ammo>=1  && ((Tick+unit.Random20[0])% (10-(unit.Variant==1 ? 1 : 0)*3 ) )==0 )
					FireWeapons(m,0,FireKind.Bullet);
				break;

			case UnitKind.Attacker:
				// 攻撃機の場合は、後方に敵航空機が飛んでればとりあえず撃つ
				if(Random(35*dmg_act)==0)
					FireWeapons(m,0,FireKind.Bullet);
				if( unit.Ammo>=1  && unit.Weapon==FireKind.Torpedo && unit.Target!=0 && unit.TurnRate==0 && unit.Speed>=unit.MaxSpeed )
					FireWeapons(m,unit.Target,FireKind.Torpedo);
				if( unit.Ammo>=1  && unit.Weapon==FireKind.Bomb && unit.Target!=0  )
					FireWeapons(m,unit.Target,FireKind.Bomb);
				break;

			case UnitKind.Bomber:
				if(Random(20*dmg_act)==0)
					FireWeapons(m,0,FireKind.Bullet);
				if( unit.Ammo>=1  && unit.Weapon==FireKind.Torpedo && unit.Target!=0 && unit.TurnRate==0 && unit.Speed>=unit.MaxSpeed )
					FireWeapons(m,unit.Target,FireKind.Torpedo);
				if( unit.Ammo>=1  && unit.Weapon==FireKind.Bomb && unit.ReloadTime==0 && unit.TurnRate==0  )
					FireWeapons(m,unit.Target,FireKind.Bomb);
				break;

			}
		}
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("chara_cont")]
public void	UpdateBattle()
	{
	int	m = default,i,f = default;

	double	wrk_x,wrk_y,drctn,dstc,wrk_x2,wrk_3=default /* C4701 */;

	int		dmg_act;

	short	s = default;
	Array200<short> bf_2_slct_unit = default;

	_DP_FLAG			dp_flag = default;

	if( Mode!=GameMode.Battle )
		return;

	if( !IsEditingMap )
		{
		/* 入力フェーズ */
		// 通信対戦時
		if((Tick%(TurnLength))==SyncTick1 )
			{
			if( !SendOrders(ref s, ref dp_flag) )
				return;
			}

		/* 受信と命令発フェイズ */

		if((Tick%TurnLength)==(SyncTick2) )
			{
			if( !ReceiveAndIssueOrders(ref s, ref bf_2_slct_unit, ref dp_flag, ref f, ref m) )
				return;
			}
		}

	Detect();

	Tick++;

	if( Result!=GameResult.None || Mode!=GameMode.Battle )
		return;

	// 雲を制御します
	UpdateClouds( );

	// エフェクトのデクリ
	for( m=1; m<EFFECT_MAX; m++)
		{
		ref var effect = ref Effects[m];
		if( effect.Layer==EffectLayer.Upper || effect.Layer==EffectLayer.Lower)
			{
			effect.TimeLeft--;
			if( effect.TimeLeft==0 )
				effect.Layer=EffectLayer.None;
			}
		}

	// 補給値のインクリ
	if( (Tick%300)==0 )
		{
		if( IsHost )
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
		ref var unit = ref Units[m];

		// 陸上施設の工事処理
		BuildBase(ref unit);

		if( !IsEditingMap && unit.IsUsed && !(unit.Kind==UnitKind.AirBase||unit.Kind==UnitKind.NavalBase||unit.Kind==UnitKind.City))
			{
			// 補給先がちゃんとあるか
			UpdateSupply(ref unit);

			//=========		 ユニットの機動制御		=========//

			if(unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked)
				{
				// パーキング中の航空機へ
				UpdateParkedPlane(ref unit, m);
				}
			else
				{
				// 移動中の各ユニットへ
				SteerUnit(ref unit, m);

				//=========		 ユニットの座標変更		=========//
				MoveUnit(ref unit, m, ref wrk_3);
				}

			//=========		 ユニットの攻撃制御		=========//
			// ターゲットがアウトならターゲットをクリア
			if( unit.Target!=0 && ( ( Units[unit.Target].Category==UnitCategory.Plane && ( Units[unit.Target].Hp<=0|| Units[unit.Target].PlaneState==UnitState.Parked )   ) || ( Units[unit.Target].Category==UnitCategory.Ship && Units[unit.Target].Hp<=0 ) || (unit.Kind==UnitKind.Fighter && unit.Ammo<=0) || (Units[unit.Target].Kind==UnitKind.Submarine && Units[unit.Target].IsSubmerged ) || ( unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked ) ))
				{
				unit.Target=0;
				}

			if( unit.ReloadTime>=1 )
				{
				if( !(unit.Kind==UnitKind.Submarine&&!unit.IsStopping))
					unit.ReloadTime--;
				if( unit.ReloadTime==0 && (unit.Weapon==FireKind.Maintenance||unit.Weapon==FireKind.Unarmed) )
					{
					if( unit.Kind==UnitKind.Fighter )
						{
						unit.Weapon=FireKind.Bullet;		// 武装品種
						unit.Ammo=50;		// 数
						}
					else
						{
						unit.Weapon=FireKind.Unarmed;
						unit.Ammo=0;
						}

					unit.Fuel=100;
					}
				}

			dmg_act=1;
			if( unit.Hp<=unit.MaxHp*0.3 )
				{
				dmg_act=2;
				}

			// そのユニットの打つ、発射を制御します。
			ControlFiring(ref unit, dmg_act, m);
			}

		if( unit.IsUsed )
			{
			// そのユニットの発するエフェクト
			UpdateUnitEffects( m );
			}

		// 潜水艦から聞こえれる探知音
		if( unit.Side==LocalSide && unit.Kind==UnitKind.Submarine && unit.IsSubmerged && Result==GameResult.None && !unit.IsSupplying )
			{
			dstc=500;
			for( i=1; i<=MaxUnitId; i++)
				{
				ref var other = ref Units[i];
				if( other.IsUsed && other.Side!=LocalSide && other.Kind==UnitKind.Destroyer && !unit.IsSupplying )
					{
					wrk_x=unit.Position.X-other.Position.X;
					wrk_y=unit.Position.Y-other.Position.Y;

					wrk_x2=Distance(wrk_x, wrk_y);
					if( dstc>wrk_x2 )
						{
						dstc=wrk_x2;
						}
					}
				}

			if( !IsEditingMap && dstc<=400 )
				{
				if( (SharedRandom(3+(int)(dstc/5)))==0  )
				PlaySoundEffect( 0, SoundId.Sonar ,unit.Position.X, unit.Position.Y);
				}
			}
		}

	// ｆｉｒｅの制御
	UpdateFires( );

	}
}
