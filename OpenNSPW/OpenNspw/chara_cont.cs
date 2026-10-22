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

	if( isJapan )
		{
		for(short s=0;s<=JPN_SHIP_END-1;s++)
			{
			// 水上ユニット
			Selections[e][s+1]=BufferedSelections[e][s];
			}
		for(var s=(short)JPN_SHIP_END;s<=(USA_PLANE_END/2)-1;s++)
			{
			// 航空ユニット
			Selections[e][s+JPN_SHIP_END+1]=BufferedSelections[e][s];
			}
		}
	else
		{
		for(short s=0;s<=JPN_SHIP_END-1;s++)
			{
			// 水上ユニット
			Selections[e][s+JPN_SHIP_END+1]=BufferedSelections[e][s];
			}
		for(var s=(short)JPN_SHIP_END;s<=(USA_PLANE_END/2)-1;s++)
			{
			// 航空ユニット
			Selections[e][s+(USA_PLANE_END/2)+1]=BufferedSelections[e][s];
			}
		}
	}

private bool ReceiveAndIssueOrders(ref short s, ref Array200<short> savedSelections, ref SyncFlagMessage syncFlag, ref int checksum, ref int m)
	{
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

	var savedSelectedUnit=SelectedUnit;
	for( s=0; s<=MaxUnitId; s++)
		{
		savedSelections[s]=Selections[1][s];
		Selections[0][s]=0;
		Selections[1][s]=0;
		}

	// 敵味方両方の命令データをここで入力する。
	for(short e=0; e<=1; e++)
		{
		MoveOrders[e].Unit=BufferedMoveOrders[e].Unit;
		MoveOrders[e].Destination = BufferedMoveOrders[e].Destination;
		MoveOrders[e].ClearsPath=BufferedMoveOrders[e].ClearsPath;

		SelectOrders[e].IsSet=BufferedSelectOrders[e].IsSet;
		SelectOrders[e].SelectedUnit=BufferedSelectOrders[e].SelectedUnit;
		SelectOrders[e].Unit=BufferedSelectOrders[e].Unit;
		SelectOrders[e].GroundPosition = new WorldPosition((short)BufferedSelectOrders[e].GroundPosition.X, (short)BufferedSelectOrders[e].GroundPosition.Y);
		}

	UnpackSelections(0, LocalSide!=Side.Japan);
	UnpackSelections(1, LocalSide==Side.Japan);

	//
	ApplyOrders();
	ApplyUnitInfoInput();

	//
	for( s=0; s<=MaxUnitId; s++)
		{
		Selections[1][s]=savedSelections[s];
		}
	SelectedUnit=savedSelectedUnit;

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
	syncFlag.Type = MessageType.SyncFlag;

	// プログラム同期エラーチェックの為
	syncFlag.TickChecksum=(byte)Tick;
	TickChecksums[1]=(byte)Tick;

	// 座標のずれチェックの為
	checksum=0;
	for(m=1; m<=MaxUnitId; m++)
		{
		ref var unit = ref Units[m];
		if( unit.IsUsed && !(unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked) )
			{
			checksum+=(int)((unit.Position.X+unit.Position.Y+unit.Direction.Degrees)*10000);
			}
		}

	syncFlag.UnitChecksum=(byte)checksum;
	UnitChecksums[1]=(byte)checksum;

	// ランダム同期エラーチェックのため
	syncFlag.RandomChecksum=(byte)RandomCount;
	RandomChecksums[1]=(byte)RandomCount;

	syncFlag.TickWait=TickWaits[1];

	syncFlag.RivalMode=(short)Mode;

	bufferDesc.dwBufferSize = (uint)(sizeof(SyncFlagMessage));
	bufferDesc.pBufferData  = (byte*) Unsafe.AsPointer(ref syncFlag);
	DirectPlayPeer.SendTo( RivalPlayerId, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
	return true;
	}

private void SendMenuOrder(ref short s)
	{
	MenuOrderMessage message;
	// メニュー
	message.Type = MessageType.MenuOrder;

	message.Menu=(byte)BufferedMenuOrders[1].Menu;
	message.SelectedUnit=(byte)BufferedMenuOrders[1].SelectedUnit;

	for( s=0; s<=(USA_PLANE_END/2)-1; s++)
		{
		message.Selections[s]=(byte)BufferedSelections[1][s];
		}
	bufferDesc.dwBufferSize = (uint)(sizeof(MenuOrderMessage));
	bufferDesc.pBufferData  = (byte*) &message;
	DirectPlayPeer.SendTo( RivalPlayerId, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
	}

private void SendSelectOrder(ref short s)
	{
	SelectOrderMessage message;
	SelectShipsOrderMessage shipsMessage;
	SelectPlanesOrderMessage planesMessage;
	SelectBaseOrderMessage landMessage;
	// 目標指定
	short ships=0;
	short planes=0;
	for( s=0; s<=JPN_SHIP_END-1; s++)
		{
		ships+=BufferedSelections[1][s];
		}
	for( s=(short)JPN_SHIP_END; s<=(USA_PLANE_END/2)-1; s++)
		{
		planes+=BufferedSelections[1][s];
		}

	if(ships!=0 && planes!=0)
		{
		// 航空機も艦船もある
		message.Type = MessageType.SelectOrder;
		message.IsSet=BufferedSelectOrders[1].IsSet.Value;
		message.SelectedUnit=(byte)BufferedSelectOrders[1].SelectedUnit;
		message.Unit=(byte)BufferedSelectOrders[1].Unit;
		message.GroundX=(short)BufferedSelectOrders[1].GroundPosition.X;
		message.GroundY=(short)BufferedSelectOrders[1].GroundPosition.Y;
		for( s=0; s<=(USA_PLANE_END/2)-1; s++)
			{
			message.Selections[s]=(byte)BufferedSelections[1][s];
			}
		bufferDesc.dwBufferSize = (uint)(sizeof(SelectOrderMessage));
		bufferDesc.pBufferData  = (byte*) &message;
		DirectPlayPeer.SendTo( RivalPlayerId, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
		}
	else if( ships!=0 && planes==0 )
		{
		// 艦船のみ
		shipsMessage.Type = MessageType.SelectShipsOrder;
		shipsMessage.IsSet=BufferedSelectOrders[1].IsSet.Value;
		shipsMessage.SelectedUnit=(byte)BufferedSelectOrders[1].SelectedUnit;
		shipsMessage.Unit=(byte)BufferedSelectOrders[1].Unit;
		shipsMessage.GroundX=(short)BufferedSelectOrders[1].GroundPosition.X;
		shipsMessage.GroundY=(short)BufferedSelectOrders[1].GroundPosition.Y;
		for( s=0; s<=JPN_SHIP_END-1; s++)
			{
			shipsMessage.Selections[s]=(byte)BufferedSelections[1][s];
			}
		bufferDesc.dwBufferSize = (uint)(sizeof(SelectShipsOrderMessage));
		bufferDesc.pBufferData  = (byte*) &shipsMessage;
		DirectPlayPeer.SendTo( RivalPlayerId, ref bufferDesc, 1, 0, null, ref hAsync, MUST_SEND );
		}
	else if( ships==0 && planes!=0 )
		{
		// 航空機のみ
		planesMessage.Type = MessageType.SelectPlanesOrder;
		planesMessage.IsSet=BufferedSelectOrders[1].IsSet.Value;
		planesMessage.SelectedUnit=(byte)BufferedSelectOrders[1].SelectedUnit;
		planesMessage.Unit=(byte)BufferedSelectOrders[1].Unit;
		planesMessage.GroundX=(short)BufferedSelectOrders[1].GroundPosition.X;
		planesMessage.GroundY=(short)BufferedSelectOrders[1].GroundPosition.Y;
		for( s=0; s<=JPN_PLANE_END-JPN_PLANE_START; s++)
			{
			planesMessage.Selections[s]=(byte)BufferedSelections[1][s+JPN_SHIP_END];
			}
		bufferDesc.dwBufferSize = (uint)(sizeof(SelectPlanesOrderMessage));
		bufferDesc.pBufferData  = (byte*) &planesMessage;
		DirectPlayPeer.SendTo( RivalPlayerId, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
		}
	else if( ships==0 && planes==0 )
		{
		// ユニットに対する指定無し。おそらく輸送船の揚陸先
		landMessage.Type = MessageType.SelectLandOrder;
		landMessage.IsSet=BufferedSelectOrders[1].IsSet.Value;
		landMessage.SelectedUnit=(byte)BufferedSelectOrders[1].SelectedUnit;
		landMessage.Unit=(byte)BufferedSelectOrders[1].Unit;
		landMessage.GroundX=(short)BufferedSelectOrders[1].GroundPosition.X;
		landMessage.GroundY=(short)BufferedSelectOrders[1].GroundPosition.Y;
		bufferDesc.dwBufferSize = (uint)(sizeof(SelectBaseOrderMessage));
		bufferDesc.pBufferData  = (byte*) &landMessage;
		DirectPlayPeer.SendTo( RivalPlayerId, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
		}
	}

private void SendMoveOrder(ref short s)
	{
	MoveOrderMessage message;
	MoveShipsOrderMessage shipsMessage;
	MovePlanesOrderMessage planesMessage;
	// 移動
	short ships=0;
	short planes=0;
	for( s=0; s<=JPN_SHIP_END-1; s++)
		{
		ships+=BufferedSelections[1][s];
		}
	for( s=(short)JPN_SHIP_END; s<=(USA_PLANE_END/2)-1; s++)
		{
		planes+=BufferedSelections[1][s];
		}

	if(ships!=0 && planes!=0)
		{
		// 航空機も艦船もある
		message.Type = MessageType.MoveOrder;
		message.Unit=(byte)BufferedMoveOrders[1].Unit;
		message.X=(short)BufferedMoveOrders[1].Destination.X;
		message.Y=(short)BufferedMoveOrders[1].Destination.Y;
		message.ClearsPath=BufferedMoveOrders[1].ClearsPath;
		for( s=0; s<=(USA_PLANE_END/2)-1; s++)
			{
			message.Selections[s]=(byte)BufferedSelections[1][s];
			}
		bufferDesc.dwBufferSize = (uint)(sizeof(MoveOrderMessage));
		bufferDesc.pBufferData  = (byte*) &message;
		DirectPlayPeer.SendTo( RivalPlayerId, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

		}
	else if( ships!=0 && planes==0 )
		{
		// 艦船のみ
		shipsMessage.Type = MessageType.MoveShipsOrder;
		shipsMessage.Unit=(byte)BufferedMoveOrders[1].Unit;
		shipsMessage.X=(short)BufferedMoveOrders[1].Destination.X;
		shipsMessage.Y=(short)BufferedMoveOrders[1].Destination.Y;
		shipsMessage.ClearsPath=BufferedMoveOrders[1].ClearsPath;
		for( s=0; s<=JPN_SHIP_END-1; s++)
			{
			shipsMessage.Selections[s]=(byte)BufferedSelections[1][s];
			}
		bufferDesc.dwBufferSize = (uint)(sizeof(MoveShipsOrderMessage));
		bufferDesc.pBufferData  = (byte*) &shipsMessage;
		DirectPlayPeer.SendTo( RivalPlayerId, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
		}
	else if( ships==0 && planes!=0 )
		{
		// 航空機のみ
		planesMessage.Type = MessageType.MovePlanesOrder;
		planesMessage.Unit=(byte)BufferedMoveOrders[1].Unit;
		planesMessage.X=(short)BufferedMoveOrders[1].Destination.X;
		planesMessage.Y=(short)BufferedMoveOrders[1].Destination.Y;
		planesMessage.ClearsPath=BufferedMoveOrders[1].ClearsPath;
		for( s=0; s<=(JPN_PLANE_END-JPN_PLANE_START); s++)
			{
			planesMessage.Selections[s]=(byte)BufferedSelections[1][s+JPN_SHIP_END];
			}
		bufferDesc.dwBufferSize = (uint)(sizeof(MovePlanesOrderMessage));
		bufferDesc.pBufferData  = (byte*) &planesMessage;
		DirectPlayPeer.SendTo( RivalPlayerId, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
		}
	}

private bool SendOrders(ref short s, ref SyncFlagMessage syncFlag)
	{
	NameMessage arrivalMessage;
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
		syncFlag.Type = MessageType.NoOrder;

		bufferDesc.dwBufferSize = (uint)(sizeof(SyncFlagMessage));
		bufferDesc.pBufferData  = (byte*) Unsafe.AsPointer(ref syncFlag);
		DirectPlayPeer.SendTo( RivalPlayerId, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

		}
	else
		{
		// まず、このフェーズで溜めた、命令をセンドする。
		if( BufferedSystemOrders[1]!=0 )
			{
			syncFlag.Type = (MessageType)BufferedSystemOrders[1];

			bufferDesc.dwBufferSize = (uint)(sizeof(SyncFlagMessage));
			bufferDesc.pBufferData  = (byte*) Unsafe.AsPointer(ref syncFlag);
			DirectPlayPeer.SendTo( RivalPlayerId, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

			SystemOrders[1]=BufferedSystemOrders[1];
			}
		else if( BufferedArrivedUnits[1]!=0 )
			{
			arrivalMessage.Type = MessageType.UnitArrived;
			arrivalMessage.Data[0] = BufferedArrivedUnits[1];
			bufferDesc.dwBufferSize = (uint)(sizeof(NameMessage));
			bufferDesc.pBufferData  = (byte*) &arrivalMessage;
			DirectPlayPeer.SendTo( RivalPlayerId, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
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
	WorldRect supplyArea;
	// 1.22 does not check for the naval base (0x4010BB): its computer starts supplying within 580 of it, further than
	// this allows.
	if( unit.Category==UnitCategory.Ship && unit.IsSupplying && unit.Fuel>=0 && Rules.SupplyNeedsNavalBase )
		{
		// 補給中の艦船
		// ptin dbg
		supplyArea=WorldRect.Around(unit.Position, 40+240, 40+240);

		var nearNavalBase=0;
		for( var i=1; i<=MaxUnitId; i++)
			{
			ref var other = ref Units[i];
			if( other.Side==unit.Side && other.Kind==UnitKind.NavalBase && other.BuildTime==0 )
				{
				if( supplyArea.Contains(other.Position) )
					{
					nearNavalBase=1;
					break;
					}
				}
			}

		if( nearNavalBase==0 )
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
	if( unit.IsStopping )
		{
		unit.SetPathPoint(0, Units[unit.Carrier].Position);
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
			var deckMark=Sprites[SpriteId.JapanUnitInfo].Position.X+Sprites[SpriteId.JapanUnitInfo].Size.Width/2;
			if((int)unit.Position.X==deckMark && unit.DeckPhase==1)
				{
				unit.DeckPhase=2;
				unit.Direction=270.0;
				}
			deckMark=Sprites[SpriteId.JapanUnitInfo].Position.Y+370;
			if((int)unit.Position.Y>=deckMark && unit.DeckPhase==2)
				{
				unit.DeckPhase=3;
				unit.Direction=90.0;

				Units[unit.Carrier].LaunchCount++;
				Units[unit.Carrier].LaunchCount&=0xffff;

				if( (Units[unit.Carrier].LaunchCount%2)!=0 )
					{
					unit.Position += new WorldVector(15, 0);
					}
				else
					{
					unit.Position -= new WorldVector(15, 0);
					}
				}
			deckMark=Sprites[SpriteId.JapanUnitInfo].Position.Y+370-80;
			if((int)unit.Position.Y==deckMark && unit.DeckPhase==3 )
				{
				unit.DeckPhase=4;
				if( unit.Kind!=UnitKind.Bomber && !(unit.Kind==UnitKind.Fighter&&unit.Variant==1) )
					unit.SpriteRow--;
				unit.TakeOffRun=0;
				}

			deckMark=Sprites[SpriteId.JapanUnitInfo].Position.Y+370-120;
			if((int)unit.Position.Y<=deckMark && unit.DeckPhase>=4)
				{	// 加速します
				unit.TakeOffRun+=1;

				if( unit.TakeOffRun==40 && unit.Carrier==UnitInfoPanel[3] &&  ((UnitKind)UnitInfoPanel[0]==UnitKind.Carrier || (UnitKind)UnitInfoPanel[0]==UnitKind.LightCarrier || (UnitKind)UnitInfoPanel[0]==UnitKind.AirBase ) )
					{
					PlayInterfaceSound(SoundId.TakeOff);
					}

				unit.Position += new WorldVector(unit.Direction.Cos()*(unit.TakeOffRun/20), -unit.Direction.Sin()*(unit.TakeOffRun/20));
				}

			deckMark=Sprites[SpriteId.JapanUnitInfo].Position.Y-30/*+60*/;
			if((int)unit.Position.Y<=deckMark && unit.DeckPhase>=4 )
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
				unit.Direction=Units[unit.Carrier].Direction.SnappedToEightDirections(unit.Direction);
				}
			if(unit.PlaneState==UnitState.Parked)
				{
				unit.Position += new WorldVector(unit.Direction.Cos()*0.8, -unit.Direction.Sin()*0.8);
				}
			}
		else
			{
			// 着陸
			var deckMark=Sprites[SpriteId.JapanUnitInfo].Position.Y+Sprites[SpriteId.JapanUnitInfo].Size.Height-150;
			if((int)unit.Position.Y<=deckMark && unit.DeckPhase==1)
				{
				unit.DeckPhase=2;
				unit.Speed=1.5;
				Units[unit.Carrier].LandingLock=0;		// その空母の次機着艦許可
				}
			deckMark=Sprites[SpriteId.JapanUnitInfo].Position.Y+Sprites[SpriteId.JapanUnitInfo].Size.Height-200;
			if((int)unit.Position.Y<=deckMark && unit.DeckPhase==2)
				{
				unit.DeckPhase=3;
				unit.Speed=unit.Speed/2;
				if( unit.Kind!=UnitKind.Bomber && !(unit.Kind==UnitKind.Fighter&&unit.Variant==1) )
					unit.SpriteRow++;
				}
			deckMark=Sprites[SpriteId.JapanUnitInfo].Position.Y+Sprites[SpriteId.JapanUnitInfo].Size.Height-270;
			if( (int)unit.Position.Y<=deckMark && unit.DeckPhase==3 )
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

				unit.SetPathPoint(0, Units[unit.Carrier].Position);
				unit.PathX[1]=MAP_RIGHT+1;

				// BUG(NSPW NET): it counts the planes of LocalSide only, where it means the carrier's, so the two games of a
				// network game count those of the rival's carriers differently (docs/KnownIssues.md). Kept as is for
				// compatibility with NSPW NET.
				var landingPlanes=0;
				for( var i=1; i<=MaxUnitId; i++)
					{
					ref var other = ref Units[i];
					if( i!=m && other.IsUsed && other.Category==UnitCategory.Plane && other.Side==LocalSide && other.DeckPhase!=0
						&& other.PlaneState==UnitState.Parked && unit.Carrier==other.Carrier && !other.IsStopping )
						landingPlanes++;
					}

				// 1.22 lets the next plane take off whatever other planes are landing (0x4018F1).
				if( landingPlanes==0 || !Rules.LaunchLockCountsLandingPlanes )
					{	// 滑走路に他の着陸機がなければ発進許可。
					Units[unit.Carrier].LaunchLock=0;		// その空母の次機発進許可
					}
else
	Units[unit.Carrier].LaunchLock=2+landingPlanes;		// 空母の発進不可の調査のため

				}
			if(unit.PlaneState==UnitState.Parked)
				{
				unit.Position += new WorldVector(unit.Direction.Cos()*unit.Speed, -unit.Direction.Sin()*unit.Speed);
				}
			}
		}
	}

private void SteerUnit(ref Unit unit, int m)
	{
	WorldRect pathPointArea;
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
					// 1.22 does not bring submarines back into the map (0x401966, kind 4).
					if( unit.EmergencyFlags[0]==0 && Rules.ComputerShipsReturnIntoMap )
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
				// 1.22 adjusts the approach on the drawn tick only (0x401AB4).
				if(  unit.Category==UnitCategory.Plane && unit.Mode==UnitMode.Return && unit.DeckPhase==1 && (IsDrawnTick || !Rules.ApproachesOnDrawnTick) )
					{
					UpdateLanding(m);
					}

				// ptin dbg
				pathPointArea=WorldRect.Around(unit.GetPathPoint(0), ON_PP, ON_PP);

				if( pathPointArea.Contains(unit.Position) )
					{
					if( unit.PathX[1]!=MAP_RIGHT+1 )
						{
						// ＰＰ＿ＸＹを一つずつずらす
						for(var n=0; unit.PathX[n]!=MAP_RIGHT+1; n++)
							{
							unit.SetPathPoint(n, unit.GetPathPoint(n+1));
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
								for(var n=1;n<=MaxUnitId;n++)
									{
									ref var other = ref Units[n];
									if( other.IsUsed && other.GroupLeader==m )
										{
										other.GroupLeader=0;
										other.SetPathPoint(0, other.GetPathPoint(0)+new WorldVector(Random(600)-300, Random(600)-300));
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
				unit.SetPathPoint(0, unit.Position);
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
				unit.SetPathPoint(0, unit.Position);
				}

			// ptin dbg
			pathPointArea=WorldRect.Around(unit.GetPathPoint(0), 30, 30);

			if( !pathPointArea.Contains(unit.Position) && !unit.IsStopping)
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
	WorldRect deckArea;
	if( 0!=0 && Units[unit.Carrier].PlanesToLaunch!=0)
		{
		// ほんまにＩｎｆｏ［４］があるんやなチェック
		}

	if( ToEightDirections((int)unit.Direction)==ToEightDirections((int)Units[unit.Carrier].Direction)
	&& Units[unit.Carrier].Capacity>Units[unit.Carrier].PlaneCount
	&& Units[unit.Carrier].PlanesToLaunch<=0 )
		{
		// ptin dbg
		deckArea=WorldRect.Around(Units[unit.Carrier].Position, ON_PP/2, ON_PP/2);

		if( deckArea.Contains(unit.Position) )
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
				for(var n=1;n<=MaxUnitId;n++)
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
			unit.Position = new WorldPosition(Sprites[SpriteId.JapanUnitInfo].Position.X+Sprites[SpriteId.JapanUnitInfo].Size.Width/2+(EffectRandom(16)-7), Sprites[SpriteId.JapanUnitInfo].Position.Y+Sprites[SpriteId.JapanUnitInfo].Size.Height+40);
			unit.Speed=5.0; unit.Direction=90.0;

			unit.Target=0;					//

			unit.EmergencyFlags[0]=0;

			}
		}
	}

private void SetAcceleration(ref Unit unit, Angle turn, int land)
	{
	if( unit.Category!=UnitCategory.Plane && (turn>=80.0 || land!=0 ) )
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
		if(turn>=45.0  )
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

			if(turn>=22.5 )
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

						var keepsSpeed=0;

						if( unit.Target!=0 && Units[unit.Target].IsFound ) //&& unit[m].kind==AT1 )
							{
							keepsSpeed=1;
							// The distance as Distance computes it, but with no component of 0 counted as 1.
							var dx=Units[unit.Target].Position.X-unit.Position.X;
							var dy=Units[unit.Target].Position.Y-unit.Position.Y;

							var angle=Direction(dx, dy);
							if(dx<0)		dx=0-dx;
							if(dy<0)		dy=0-dy;
							if(angle>=180)	angle=angle-180;
							if(angle>=90)	angle=90-(angle-90);
							var distance=(dx)/(CosDegrees(angle));

							if( distance<=BB1_SIGHT )
								{
								keepsSpeed=1;		// 速度落とす要無し
								}
							}
						if( keepsSpeed==0 && unit.Speed>=(unit.FormationSpeed*(0.60-(unit.Kind==UnitKind.Carrier ? 1 : 0)*0.15 ))+unit.AccelerationChange )
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

// 1.22 replans the route of the computer's unit m that is blocked, with no leader, to the end of its path
// (set_cpu_route(m, 1.0)): from the last point of the path, or, with a single point, from where the ship is if that
// point is within ahead, the ship in its way (0x402147, 0x402348).
private void ReplanBlockedRoute(ref Unit unit, int m, WorldRect? ahead)
	{
	int i;
	for( i=1; i<=63 && unit.PathX[i]!=MAP_RIGHT+1; i++ )
		{
		}

	if( i>=2 )
		{
		unit.SetPathPoint(0, unit.GetPathPoint(i-1));
		unit.PathX[1]=MAP_RIGHT+1;
		}
	else if( ahead is { } area && area.Contains(unit.GetPathPoint(0)) )
		{
		unit.SetPathPoint(0, unit.Position);
		unit.PathX[1]=MAP_RIGHT+1;
		}

	SetCpuRoute(m, 1.0);
	}

// 1.22's ship looks 40 ahead, and only air bases and naval bases are not in its way (0x401F95 to 0x4023EB). A
// computer's ship with no leader is replanned when a ship in its way has slowed it to a fifth of its top speed, every
// 100 ticks, or when the point 80 toward its path is land, unless it is on an emergency move.
private void CheckCourseAheadAs122(ref Unit unit, int m, ref int land, Angle bearing)
	{
	WorldRect shipArea;
	var replans = unit.Side==CpuSide && unit.GroupLeader==0;
	var ahead=unit.Position+WorldVector.FromDirection(unit.Direction) * 40;
	if(  !( unit.Kind==UnitKind.Submarine && unit.IsSubmerged ) )
		{
		for( var n=1; n<=MaxUnitId; n++)
			{
			ref var other = ref Units[n];
			if(other.IsUsed && m!=n && other.Category==UnitCategory.Ship && !(other.Kind==UnitKind.Submarine && other.IsSubmerged) && other.Kind!=UnitKind.NavalBase && other.Kind!=UnitKind.AirBase )
				{
				shipArea=WorldRect.Around(other.Position, (Sprites[SpriteId.JapanUnits].Size.Width/2), (Sprites[SpriteId.JapanUnits].Size.Height/2));

				if( shipArea.Contains(ahead))
					{
					// 前方に艦船！
					unit.EmergencyFlags[0]=0;
					land=1;
					if( replans && (Tick+m)%100==0 && unit.Speed<=unit.MaxSpeed*0.2 )
						ReplanBlockedRoute(ref unit, m, shipArea);
					return;
					}
				}
			}
		}

	// ＰＰ方向に陸地があるか
	ahead=unit.Position+WorldVector.FromDirection(bearing) * 80;
	if( IsInWorld(ahead) )
		{
		var (column, row)=TileAt(ahead);
		if( MapTiles[row][column]>=1 && MapTiles[row][column]<=9 )
			{
			land=1;
			if( replans && unit.EmergencyFlags[0]==0 )
				ReplanBlockedRoute(ref unit, m, null);
			}
		}

	if( unit.EmergencyFlags[0]!=0 && land!=0)
		{
		// 緊急移動の取り消し
		unit.EmergencyFlags[0]=0;
		}
	}

private void CheckCourseAhead(ref Unit unit, int m, ref int land, Angle bearing)
	{
	if( Rules.ComputerReplansBlockedRoutes )
		{
		CheckCourseAheadAs122(ref unit, m, ref land, bearing);
		return;
		}

	WorldRect shipArea;
	var ahead=unit.Position+WorldVector.FromDirection(unit.Direction) * (40+unit.MaxSpeed*10);
	if(  !( unit.Kind==UnitKind.Submarine && unit.IsSubmerged ) )
		{
		for( var n=1; n<=MaxUnitId; n++)
			{
			ref var other = ref Units[n];
			if(other.IsUsed && m!=n && other.Category==UnitCategory.Ship && !(other.Kind==UnitKind.Submarine && other.IsSubmerged)  && !(other.Kind>=UnitKind.AirBase&&other.Kind<=UnitKind.Fortress) )
				{
				// ptin dbg
				shipArea=WorldRect.Around(other.Position, (Sprites[SpriteId.JapanUnits].Size.Width/2), (Sprites[SpriteId.JapanUnits].Size.Height/2));

				if( shipArea.Contains(ahead))
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
		ahead=unit.Position+WorldVector.FromDirection(bearing) * 80;

		if( IsInWorld(ahead) )
			{
			var (column, row)=TileAt(ahead);
			if( MapTiles[row][column]>=1 && MapTiles[row][column]<=9 )
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

private void MoveUnit(ref Unit unit, int m, ref double cargoSpeedFactor)
	{
	WorldRect destinationArea;
	WorldVector toDestination;
	if( (!unit.IsStopping || unit.EmergencyFlags[0]!=0)  && !(unit.Category==UnitCategory.Ship && unit.IsSupplying) )
		{
		if( unit.EmergencyFlags[0]!=0	)
			{
			// 緊急移動先についているか
			// ptin dbg
			destinationArea=WorldRect.Around(unit.EmergencyDestination, ON_PP, ON_PP);

			if( destinationArea.Contains(unit.Position) )
				{
				unit.EmergencyFlags[0]=0;
				// 通常移動		直前定点へ！
				toDestination=unit.GetPathPoint(0)-unit.Position;
				}
			else
				{
				// 緊急移動先がある場合
				toDestination=unit.EmergencyDestination-unit.Position;
				}
			}
		else
			{
			// 通常移動		直前定点へ！
			toDestination=unit.GetPathPoint(0)-unit.Position;
			}

		var bearing=toDestination.Direction;

		var land=0;
		if( unit.Category==UnitCategory.Ship )
			{
			// 艦首方向に他の艦船があるか
			CheckCourseAhead(ref unit, m, ref land, bearing);
			}

		var turn=bearing-unit.Direction;
		turn=turn.WrapNegative();

		unit.TurnRate=0;
		if(turn>=1.0&&turn<=180.0)
			{
			//	左へ
			unit.TurnRate=unit.TurnRateChange;
			if(turn <= 3.0)
				unit.TurnRate = +0.5;		// 要は微調整	//unit[m].a_drctn_add;
			}
		if(turn<=359.0 && turn>180.0)
			{
			//	右へ
			unit.TurnRate=-unit.TurnRateChange;
			if(turn >= 357.0)
				unit.TurnRate = -0.5;		// 要は微調整	//-unit[m].a_drctn_add;
			}

		unit.Direction+=unit.TurnRate;

		if(unit.Direction>360.0 || (Rules.HeadingWrapsAt360 && unit.Direction==360.0))
			unit.Direction-=360.0;
		unit.Direction=unit.Direction.WrapNegative();

		if(turn>180.0)
			turn=360.0-turn;

		// ユニットのスピード
		SetAcceleration(ref unit, turn, land);
		}
	else
		{
		if(unit.Speed>0)
			{
			unit.Acceleration=-(unit.AccelerationChange*2);

			}
		}

	if( unit.Fuel<=0 && (unit.Category==UnitCategory.Plane || (unit.Category==UnitCategory.Ship && unit.Speed>= (Rules.OutOfFuelShipsBrakeFromTopSpeedTimesATenth ? unit.MaxSpeed*0.1 : unit.MaxSpeed/10) ) ) )
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

	// NSPW NET slows a transport with cargo; 1.22, not (0x402955).
	if( unit.Kind==UnitKind.Transport && unit.Ammo!=0 && Rules.CargoSlowsTransports )
		{
		// 輸送船でなんかつんでると最高速度がおちる
		switch( unit.Weapon )
			{
			case FireKind.CargoInfantryBase:	cargoSpeedFactor=1.0;		break;
			case FireKind.CargoPillboxes:	cargoSpeedFactor=0.9;		break;
			case FireKind.CargoFortress:	cargoSpeedFactor=0.9;		break;
			case FireKind.CargoAirBase:		cargoSpeedFactor=0.8;		break;
			case FireKind.CargoNavalBase:		cargoSpeedFactor=0.8;		break;
			}

		if( unit.MaxSpeed*cargoSpeedFactor < unit.Speed )
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

	// 1.22 replans the computer's unit with no leader whose next step is land, every 500 ticks, planes included
	// (0x402D2B).
	if( Rules.ComputerReplansBlockedRoutes && unit.Side==CpuSide && unit.GroupLeader==0 && (Tick%500)==0 && IsLandAt(unit.Position+WorldVector.FromDirection(unit.Direction) * unit.Speed) )
		ReplanBlockedRoute(ref unit, m, null);

	// 新座標を設定
	unit.Position += WorldVector.FromDirection(unit.Direction) * unit.Speed;

	// 燃料消費
	var interval=(int)unit.FuelInterval;
	if( unit.Kind==UnitKind.Attacker && unit.Ammo!=0 && (unit.Weapon==FireKind.Torpedo || unit.Weapon==FireKind.Bomb))
		interval=interval-(interval/10);
	if( unit.Kind==UnitKind.Fighter && unit.Target!=0 && unit.MaxSpeed < unit.Speed )
		interval=interval-(interval/10);

	// 1.22's fuel is an integer, which a moving unit burns 1 at a time (0x402F72); NSPW NET burns its speed's share of
	// its top speed.
	if( unit.FuelInterval>=1 && (Tick%interval)==0 && unit.Fuel>0 && unit.Speed>0)
		{
		unit.Fuel-=( Rules.FuelBurnsBySpeed ? unit.Speed/unit.MaxSpeed : 1 );
		if(unit.Fuel<0)
			unit.Fuel=0;
		}

	if( unit.Fuel==0 && unit.Category==UnitCategory.Plane && unit.Speed<=0 )
		unit.Hp=0;							// 飛行機でガス０なら落ちます
	}

private void ControlInfantryBaseFiring(ref Unit unit, int damageSlowdown, int m)
	{
	unit.Ammo=unit.MaxAmmo;

	// 艦砲 自動
	// NSPW NET fires 1 in 250 a tick; 1.22, 1 in 200 (0x403140).
	if( unit.Ammo>=1 && Random(Rules.InfantryBaseGunInterval*damageSlowdown)==0 )
		{
		FireWeapons(m,0,FireKind.Gun);
		}
	// 艦砲　選択
	// 対空砲弾 自動砲撃
	if( unit.Ammo>=1 && Random(200*damageSlowdown)==0 )
		{
		FireWeapons(m,0,FireKind.AntiAircraftShell);
		}
	// 対空機関砲 X 2
	if( Random(180*damageSlowdown)==0 && unit.Side==Side.UnitedStates )
		FireWeapons(m,0,FireKind.RapidAntiAircraftShell);
	// 対空機銃
	if( Random(10*damageSlowdown)==0 )
		FireWeapons(m,0,FireKind.Bullet);
	}

private void ControlPillboxesFiring(ref Unit unit, int damageSlowdown, int m)
	{
	unit.Ammo=unit.MaxAmmo;
	// 艦砲 自動
	// NSPW NET fires 1 in 200 a tick; 1.22, 1 in 150 (0x4031DE).
	if( unit.Ammo>=1 && Random( Rules.PillboxesGunInterval*damageSlowdown )==0 )
		{
		FireWeapons(m,0,FireKind.Gun);
		}
	// 艦砲　選択
	// 対空砲弾 自動砲撃
	if( unit.Ammo>=1 && Random(160*damageSlowdown)==0 )
		{
		FireWeapons(m,0,FireKind.AntiAircraftShell);
		}
	// 対空機関砲
	if( Random(140*damageSlowdown)==0 && unit.Side==Side.UnitedStates )
		{
		FireWeapons(m,0,FireKind.RapidAntiAircraftShell);
		}
	// 対空機銃
	if( Random(10*damageSlowdown)==0 )
		{
		FireWeapons(m,0,FireKind.Bullet);
		}
	}

private void ControlFortressFiring(ref Unit unit, int damageSlowdown, int m)
	{
	unit.Ammo=unit.MaxAmmo;

	// 艦砲 自動
	// NSPW NET fires 1 in 150 a tick; 1.22, 1 in 100 (0x403275).
	if( unit.Ammo>=1 && Random(Rules.FortressGunInterval*damageSlowdown)==0 )
		{
		FireWeapons(m,0,FireKind.Gun);
		}
	// 艦砲　選択
	// 対空砲弾 自動砲撃
	if( unit.Ammo>=1 && Random(120*damageSlowdown)==0 )
		{
		FireWeapons(m,0,FireKind.AntiAircraftShell);
		}
	// 対空機関砲
	if( Random(120*damageSlowdown)==0 && unit.Side==Side.UnitedStates )
		FireWeapons(m,0,FireKind.RapidAntiAircraftShell);
	// 対空機関砲 X 2
	if( Random(160*damageSlowdown)==0 && unit.Side==Side.UnitedStates )
		FireWeapons(m,0,FireKind.RapidAntiAircraftShell);
	// 対空機銃
	if( Random(8*damageSlowdown)==0 )
		FireWeapons(m,0,FireKind.Bullet);
	}

private void ControlBattleshipFiring(ref Unit unit, int damageSlowdown, int m)
	{
	// 1.22 fires every battleship alike, as NSPW NET does all but the Yamato class (0x40332C).
	if( unit.Side==Side.Japan && unit.Variant==1 && Rules.HasYamatoClass )
		{
		// 大和級
		// 艦砲 自動
		if( unit.Ammo>=1 && Random(350*damageSlowdown)==0 && (unit.Target==0 || Units[unit.Target].Category==UnitCategory.Ship) )
			{
			FireWeapons(m,0,FireKind.Gun);
			}
		// 艦砲　選択
		if(unit.Ammo>=1 && Random(300*damageSlowdown)==0 && unit.Target!=0 )
			{
			FireWeapons(m,unit.Target,FireKind.NavalBaseGun);
			}

		if( unit.Ammo>=1 && Random(180*damageSlowdown)==0 && unit.Target!=0 )
			{
			FireWeapons(m,unit.Target,FireKind.AntiAircraftShell);		// 対空砲弾 選択
			}

		if( unit.Ammo>=1 && Random(95*damageSlowdown)==0 )
			{
			FireWeapons(m,0,FireKind.AntiAircraftShell);							// 対空砲弾 自動砲撃
			}

		// 対空機銃
		if( Random(4*damageSlowdown)==0 )
			FireWeapons(m,0,FireKind.Bullet);
		}
	else
		{
		// 艦砲 自動
		if( unit.Ammo>=1 && Random(350*damageSlowdown)==0 && (unit.Target==0 || Units[unit.Target].Category==UnitCategory.Ship) )
			{
			FireWeapons(m,0,FireKind.Gun);
			}
		// 艦砲　選択
		if(unit.Ammo>=1 && Random(300*damageSlowdown)==0 && unit.Target!=0 )
			{
			FireWeapons(m,unit.Target,FireKind.Gun);
			}

		if( unit.Ammo>=1 && Random(190*damageSlowdown)==0 && unit.Target!=0 )
			{
			FireWeapons(m,unit.Target,FireKind.AntiAircraftShell);		// 対空砲弾 選択
			}

		if( unit.Ammo>=1 && Random(110*damageSlowdown)==0 )
			{
			FireWeapons(m,0,FireKind.AntiAircraftShell);							// 対空砲弾 自動砲撃
			}

		// 対空機関砲
		if( Random(60*damageSlowdown)==0 && unit.Side==Side.UnitedStates )
			FireWeapons(m,0,FireKind.RapidAntiAircraftShell);
		// 対空機関砲 X 2
		if( Random(120*damageSlowdown)==0 && unit.Side==Side.UnitedStates )
			FireWeapons(m,0,FireKind.RapidAntiAircraftShell);

		// 対空機銃
		if( Random(5*damageSlowdown)==0 )
			FireWeapons(m,0,FireKind.Bullet);
		}
	}

private void ControlCruiserFiring(ref Unit unit, int damageSlowdown, int m)
	{
	// 1.22 has no anti-aircraft cruisers: it fires every cruiser as NSPW NET does its first type (0x40348C).
	if( unit.Variant==0 || !Rules.HasSecondCruiserAndDestroyerTypes )
		{
		// 巡洋艦
		// 艦砲 自動
		if( unit.Ammo>=1  && Random(350*damageSlowdown)==0  && (unit.Target==0 || Units[unit.Target].Category==UnitCategory.Ship) )
			{
			FireWeapons(m,0,FireKind.Gun);
			}
		// 艦砲 選択
		if( unit.Ammo>=1  && Random(300*damageSlowdown)==0 && unit.Target!=0 )
			{
			FireWeapons(m,unit.Target,FireKind.Gun);
			}

		// 対空砲弾
		if( unit.Ammo>=1 && Random(120*damageSlowdown)==0  )
			{
			if(unit.Target!=0)								// 対空砲弾 選択
				FireWeapons(m,unit.Target,FireKind.AntiAircraftShell);
			else
				FireWeapons(m,0,FireKind.AntiAircraftShell);							// 対空砲弾 自動砲撃
			}

		// 対空機関砲
		if( unit.Ammo>=1 && Random(60*damageSlowdown)==0 && unit.Side==Side.UnitedStates )
			FireWeapons(m,0,FireKind.RapidAntiAircraftShell);

		// 対空機銃 自動
		if( Random(10*damageSlowdown)==0  )
			FireWeapons(m,0,FireKind.Bullet);
		// 魚雷
		if( unit.Random40[0]==Tick%(40*damageSlowdown) && unit.Ammo>=1 && unit.ReloadTime<=0 && unit.Side==Side.Japan )
			{
			FireWeapons(m,0,FireKind.Torpedo);
			}
		}
	else
		{
		// 防空巡洋艦
		// 艦砲 自動
		if( unit.Ammo>=1  && Random(800*damageSlowdown)==0  && (unit.Target==0 || Units[unit.Target].Category==UnitCategory.Ship) )
			{
			FireWeapons(m,0,FireKind.Gun);
			}

		// 対空砲弾
		if( unit.Ammo>=1 && Random(100*damageSlowdown)==0 )
			{
			// 自動
			FireWeapons(m,0,FireKind.AntiAircraftShell);							// 対空砲弾 自動砲撃
			}

		if( unit.Ammo>=1 && Random(180*damageSlowdown)==0 && unit.Target!=0 )
			{
			// 選択
			FireWeapons(m,unit.Target,FireKind.AntiAircraftShell);
			}

		// 対空機関砲
		if( unit.Ammo>=1 && Random(55*damageSlowdown)==0 && unit.Side==Side.UnitedStates )
			FireWeapons(m,0,FireKind.RapidAntiAircraftShell);

		// 対空機銃 自動
		if( Random(10*damageSlowdown)==0  )
			FireWeapons(m,0,FireKind.Bullet);

		}
	}

private void ControlDestroyerFiring(ref Unit unit, int damageSlowdown, int m)
	{
	// 1.22 has no second type of destroyer: it fires every destroyer as NSPW NET does its first type (0x403647).
	if( unit.Variant==0 || !Rules.HasSecondCruiserAndDestroyerTypes )
		{
		// 艦砲 自動
		if( unit.Ammo>=1 && Random(200*damageSlowdown)==0 )
			{
			FireWeapons(m,0,FireKind.Gun);
			}

		// 魚雷
		if( unit.Random40[0]==Tick%(40*damageSlowdown) && unit.Ammo>=1 && unit.ReloadTime<=0 )
			{
			FireWeapons(m,0,FireKind.Torpedo);
			}

		// 爆雷
		if( unit.Ammo>=1 && (Tick%(35*damageSlowdown))==0 )
			{
			FireWeapons(m,0,FireKind.AntiSubmarineBomb);
			}
		// 対空機銃
		if( Random(15*damageSlowdown)==0 )
			FireWeapons(m,0,FireKind.Bullet);
		}
	else
		{
		// 艦砲 自動
		if( unit.Ammo>=1 && Random(500*damageSlowdown)==0 )
			{
			FireWeapons(m,0,FireKind.Gun);
			}

		// 対空機銃
		if( Random(20*damageSlowdown)==0 )
			FireWeapons(m,0,FireKind.Bullet);

		// 爆雷
		if( unit.Ammo>=1 && (Tick%(25*damageSlowdown))==0 )
			{
			FireWeapons(m,0,FireKind.AntiSubmarineBomb);
			}

		}
	}

private void ControlTransportFiring(ref Unit unit, int m)
	{
	// 1.22's transports land infantry bases, pillboxes and fortresses only (0x403745).
	if( !Rules.TransportsLandNavalAndAirBases )
		{
		if( unit.Weapon==FireKind.CargoInfantryBase && unit.Ammo>=1  && unit.Target==MaxUnitId+1)
			FireWeapons(m,0,FireKind.CargoInfantryBase);
		if( unit.Weapon==FireKind.CargoPillboxes && unit.Ammo>=1  && unit.Target==MaxUnitId+1)
			FireWeapons(m,0,FireKind.CargoPillboxes);
		if( unit.Weapon==FireKind.CargoFortress && unit.Ammo>=1  && unit.Target==MaxUnitId+1)
			FireWeapons(m,0,FireKind.CargoFortress);
		return;
		}

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

private void ControlFiring(ref Unit unit, int damageSlowdown, int m)
	{
	// 1.22 fires nothing for Japan in scenario 1 (0x4030FF).
	if( Rules.JapanHoldsFireInScenario1 && ScenarioNumber==1 && unit.Side==Side.Japan )
		return;
	if( !IsEditingMap && !unit.IsSupplying && (( unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Flying )||( unit.Category==UnitCategory.Ship)))
		{
		switch( unit.Kind )
			{
			case UnitKind.InfantryBase:
				// 地上基地は弾が減りません
				ControlInfantryBaseFiring(ref unit, damageSlowdown, m);
				break;

			case UnitKind.Pillboxes:
				// 地上基地は弾が減りません
				ControlPillboxesFiring(ref unit, damageSlowdown, m);
				break;

			case UnitKind.Fortress:
				// 地上基地は弾が減りません
				ControlFortressFiring(ref unit, damageSlowdown, m);
				break;

			case UnitKind.Battleship:
				ControlBattleshipFiring(ref unit, damageSlowdown, m);

				break;

			case UnitKind.Cruiser:
				ControlCruiserFiring(ref unit, damageSlowdown, m);
				break;

			case UnitKind.Carrier:
				// 対空機関砲
				if( unit.Ammo>=1 && Random(80*damageSlowdown)==0 && unit.Side==Side.UnitedStates )
					FireWeapons(m,0,FireKind.RapidAntiAircraftShell);
				// 対空機銃
				if( Random(15*damageSlowdown)==0)
					FireWeapons(m,0,FireKind.Bullet);
				break;

			case UnitKind.Destroyer:
				ControlDestroyerFiring(ref unit, damageSlowdown, m);
				break;

			case UnitKind.LightCarrier:
				// 対空機関砲
				if( unit.Ammo>=1 && Random(100*damageSlowdown)==0 && unit.Side==Side.UnitedStates )
					FireWeapons(m,0,FireKind.RapidAntiAircraftShell);
				// 対空機銃
				if( Random(20*damageSlowdown)==0 )
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
				if(Random(35*damageSlowdown)==0)
					FireWeapons(m,0,FireKind.Bullet);
				if( unit.Ammo>=1  && unit.Weapon==FireKind.Torpedo && unit.Target!=0 && unit.TurnRate==0 && unit.Speed>=unit.MaxSpeed )
					FireWeapons(m,unit.Target,FireKind.Torpedo);
				if( unit.Ammo>=1  && unit.Weapon==FireKind.Bomb && unit.Target!=0  )
					FireWeapons(m,unit.Target,FireKind.Bomb);
				break;

			case UnitKind.Bomber:
				if(Random(20*damageSlowdown)==0)
					FireWeapons(m,0,FireKind.Bullet);
				// 1.22's bombers drop no torpedoes (0x40392C).
				if( unit.Ammo>=1  && unit.Weapon==FireKind.Torpedo && unit.Target!=0 && unit.TurnRate==0 && unit.Speed>=unit.MaxSpeed && Rules.BombersDropTorpedoes )
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
	int m = default, checksum = default;

	double cargoSpeedFactor=default /* C4701 */ /* C4701 */;


	short	s = default;
	Array200<short> savedSelections = default;

	SyncFlagMessage			syncFlag = default;

	if( Mode!=GameMode.Battle )
		return;

	// A single-player game has no turns: it gives its orders every frame (IssueSingleplayerOrders).
	if( !IsEditingMap && !IsSingleplayer )
		{
		/* 入力フェーズ */
		// 通信対戦時
		if((Tick%(TurnLength))==SyncTick1 )
			{
			if( !SendOrders(ref s, ref syncFlag) )
				return;
			}

		/* 受信と命令発フェイズ */

		if((Tick%TurnLength)==(SyncTick2) )
			{
			if( !ReceiveAndIssueOrders(ref s, ref savedSelections, ref syncFlag, ref checksum, ref m) )
				return;
			}
		}

	if( Rules.TickOrderOf122 )
		{
		// 1.22 counts the tick first, then moves the clouds and finds the units, and then checks for the end (0x401009
		// to 0x401039).
		Tick++;
		UpdateClouds( );
		Detect();
		if( Result!=GameResult.None || Mode!=GameMode.Battle )
			return;
		}
	else
		{
		Detect();

		Tick++;

		if( Result!=GameResult.None || Mode!=GameMode.Battle )
			return;

		// 雲を制御します
		UpdateClouds( );
		}

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

		// 1.22 skips only air bases and naval bases (0x4010A3).
		if( !IsEditingMap && unit.IsUsed && !(unit.Kind==UnitKind.AirBase||unit.Kind==UnitKind.NavalBase||(unit.Kind==UnitKind.City && !Rules.TickOrderOf122)))
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
				MoveUnit(ref unit, m, ref cargoSpeedFactor);
				}

			//=========		 ユニットの攻撃制御		=========//
			// ターゲットがアウトならターゲットをクリア
			// 1.22 keeps a submerged submarine as a target, and a parked plane's target (0x402FD2).
			if( unit.Target!=0 && ( ( Units[unit.Target].Category==UnitCategory.Plane && ( Units[unit.Target].Hp<=0|| Units[unit.Target].PlaneState==UnitState.Parked )   ) || ( Units[unit.Target].Category==UnitCategory.Ship && Units[unit.Target].Hp<=0 ) || (unit.Kind==UnitKind.Fighter && unit.Ammo<=0) || (Rules.DropsHiddenTargets && ( (Units[unit.Target].Kind==UnitKind.Submarine && Units[unit.Target].IsSubmerged ) || ( unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked ) )) ))
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

			var damageSlowdown=1;
			if( unit.Hp<=unit.MaxHp*0.3 )
				{
				damageSlowdown=2;
				}

			// そのユニットの打つ、発射を制御します。
			ControlFiring(ref unit, damageSlowdown, m);
			}

		if( unit.IsUsed )
			{
			// そのユニットの発するエフェクト
			UpdateUnitEffects( m );
			}

		// 潜水艦から聞こえれる探知音
		if( unit.Side==LocalSide && unit.Kind==UnitKind.Submarine && unit.IsSubmerged && Result==GameResult.None && !unit.IsSupplying )
			{
			var nearestDestroyer=500.0;
			for( var i=1; i<=MaxUnitId; i++)
				{
				ref var other = ref Units[i];
				if( other.IsUsed && other.Side!=LocalSide && other.Kind==UnitKind.Destroyer && !unit.IsSupplying )
					{
					var distance=(unit.Position-other.Position).Length;
					if( nearestDestroyer>distance )
						{
						nearestDestroyer=distance;
						}
					}
				}

			// 1.22 has no sonar.
			if( !IsEditingMap && nearestDestroyer<=400 && Rules.PlaysSonar )
				{
				if( (SharedRandom(3+(int)(nearestDestroyer/5)))==0  )
				PlaySoundEffect(SoundId.Sonar, unit.Position);
				}
			}
		}

	// ｆｉｒｅの制御
	UpdateFires( );

	// 1.22 counts the time limit down, every tick.
	if( IsSingleplayer && BattleTime>=1 && Result==GameResult.None )
		BattleTime--;

	}
}
