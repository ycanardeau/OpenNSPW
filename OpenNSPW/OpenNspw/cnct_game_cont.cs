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

// Port of cnct_game_cont.cpp.

using static OpenNspw.Nspw;

namespace OpenNspw;

public unsafe partial class Nspw
{

//============================================================================
// リジューム、セーブします。
//----------------------------------------------------------------------------
[Original("save_on_resume")]
public void	SaveResume(int type)
	{
	HANDLE	hFile=default;
	uint	bytesTransferred;
//	unsigned short		szBuf[256][256];					// マップ

	switch( type )
		{
		case 1:
			hFile=CreateFile("Saved\\resume_1.dat", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
								OPEN_ALWAYS);
			break;
		case 2:
			hFile=CreateFile("Saved\\cnct_error.dat", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
								OPEN_ALWAYS);
			break;
		case 3:
			hFile=CreateFile("Saved\\auto_save.dat", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
								OPEN_ALWAYS);
			break;
		}

	if( hFile != INVALID_HANDLE_VALUE )
		{
		//unsigned short		cmbt_map[256][256];					// マップ

		SetFilePointer( hFile,0,FILE_BEGIN);		// 先頭へ
/***
		// データ を バッファへ
		for(m=0;m<=255;m++)
			for(n=0;n<=255;n++)
				szBuf[m][n]=cmbt_map[m][n];

		// 書き込み
		WriteFile(hFile, szBuf,sizeof(szBuf),&dwActBytes,NULL);
***/

		// 書き込み
		WriteFile(hFile, ref Units,(uint)(sizeof(Array256<Unit>)),&bytesTransferred,null);
		WriteFile(hFile, ref Fires,(uint)(sizeof(Array512<Fire>)),&bytesTransferred,null);
		WriteFile(hFile, ref Effects,(uint)(sizeof(Array1024<Effect>)),&bytesTransferred,null);

		WriteFile(hFile, ref LocalSide,sizeof(short),&bytesTransferred,null);
		WriteFile(hFile, ref BattleTime,sizeof(int),&bytesTransferred,null);

		WriteFile(hFile, ref SupplyCount,sizeof(short),&bytesTransferred,null);
		WriteFile(hFile, ref SupplyTarget,sizeof(short),&bytesTransferred,null);

		WriteFile(hFile, ref ScenarioNumber,sizeof(short),&bytesTransferred,null);
		WriteFile(hFile, ref SupplyPoints,sizeof(short),&bytesTransferred,null);

		WriteFile(hFile, ref SupplyRates,(uint)(sizeof(Array2<short>)),&bytesTransferred,null);
		WriteFile(hFile, ref IsDecisionEnabled,sizeof(byte),&bytesTransferred,null);
		WriteFile(hFile, ref ArrivalControl,sizeof(byte),&bytesTransferred,null);

		WriteFile(hFile, ref IsHost,sizeof(byte),&bytesTransferred,null);
		WriteFile(hFile, ref UserScenarioFileName,(uint)(sizeof(Array260<byte>)),&bytesTransferred,null);

		WriteFile(hFile, ref SwapTime,sizeof(short),&bytesTransferred,null);
		WriteFile(hFile, ref SwapRule,sizeof(short),&bytesTransferred,null);

		CloseHandle(hFile);
		}
	}

//============================================================================
// リジューム、ロードします
//----------------------------------------------------------------------------
[Original("load_on_resume")]
public void	LoadResume(int type)
	{
	HANDLE	hFile=default;
//	unsigned short		szBuf[256][256];					// マップ

	switch( type )
		{
		case 1:
			hFile=CreateFile("Saved\\resume_1.dat", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
								OPEN_ALWAYS);
			break;
		case 2:
			hFile=CreateFile("Saved\\cnct_error.dat", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
								OPEN_ALWAYS);
			break;
		case 3:
			hFile=CreateFile("Saved\\auto_save.dat", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
								OPEN_ALWAYS);
			break;
		}

	if( hFile != INVALID_HANDLE_VALUE )
		{
		uint	bytesTransferred;

		// 読み込み
		SetFilePointer( hFile,0,FILE_BEGIN);		// 先頭へ

		ReadFile( hFile, ref Units, (uint)(sizeof(Array256<Unit>)), &bytesTransferred, null );
		ReadFile( hFile, ref Fires, (uint)(sizeof(Array512<Fire>)), &bytesTransferred, null );
		ReadFile( hFile, ref Effects, (uint)(sizeof(Array1024<Effect>)), &bytesTransferred, null );

		ReadFile( hFile, ref LocalSide, sizeof(short), &bytesTransferred, null );
		ReadFile( hFile, ref BattleTime, sizeof(int), &bytesTransferred, null );

		ReadFile( hFile, ref SupplyCount, sizeof(short), &bytesTransferred, null );
		ReadFile( hFile, ref SupplyTarget, sizeof(short), &bytesTransferred, null );

		ReadFile( hFile, ref ScenarioNumber, sizeof(short), &bytesTransferred, null );
		ReadFile( hFile, ref SupplyPoints, sizeof(short), &bytesTransferred, null );

		ReadFile( hFile, ref SupplyRates, (uint)(sizeof(Array2<short>)), &bytesTransferred, null );
		ReadFile( hFile, ref IsDecisionEnabled, sizeof(byte), &bytesTransferred, null );
		ReadFile( hFile, ref ArrivalControl, sizeof(byte), &bytesTransferred, null );

		ReadFile( hFile, ref IsHost, sizeof(byte), &bytesTransferred, null );
		ReadFile(hFile, ref UserScenarioFileName,(uint)(sizeof(Array260<byte>)),&bytesTransferred,null);

		ReadFile( hFile, ref SwapTime, sizeof(short), &bytesTransferred, null );
		ReadFile(hFile, ref SwapRule,sizeof(short),&bytesTransferred,null);

		CloseHandle(hFile);
		}

	}

private bool CheckResultOfScenario1()
	{
	var f=0;
	for( var i=1; i<=MaxUnitId; i++ )
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.Japan && (unit.Kind==UnitKind.Carrier || unit.Kind==UnitKind.LightCarrier) )
			f++;
		}
	var m=0;
	for( var i=1; i<=MaxUnitId; i++ )
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.Japan && (unit.Kind==UnitKind.InfantryBase || unit.Kind==UnitKind.Pillboxes || unit.Kind==UnitKind.Fortress ) )
			m++;
		}
	if( f<=0 || m<=0 )
		{
		Result=GameResult.UnitedStatesWon;
		return false;
		}

	f=0;
	for( var i=1; i<=MaxUnitId; i++)
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.UnitedStates && (unit.Kind==UnitKind.Carrier || unit.Kind==UnitKind.LightCarrier) )
			f++;
		}
	m=0;
	for( var i=1; i<=MaxUnitId; i++)
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.UnitedStates && (unit.Kind==UnitKind.InfantryBase || unit.Kind==UnitKind.Pillboxes || unit.Kind==UnitKind.Fortress ) )
			m++;
		}
	if( f<=0 || m<=0 )
		{
		Result=GameResult.JapanWon;
		return false;
		}
	return true;
	}

private bool CheckResultOfScenario4()
	{
	var f=0;
	for( var i=1; i<=MaxUnitId; i++)
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.Japan && (unit.Kind==UnitKind.Battleship || unit.Kind==UnitKind.Cruiser) )
			f++;
		}
	var m=0;
	for( var i=1; i<=MaxUnitId; i++ )
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.Japan && (unit.Kind==UnitKind.InfantryBase || unit.Kind==UnitKind.Pillboxes || unit.Kind==UnitKind.Fortress ) )
			m++;
		}
	if( f<=0 || m<=0 )
		{
		Result=GameResult.UnitedStatesWon;
		return false;
		}

	f=0;
	for( var i=1; i<=MaxUnitId; i++)
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.UnitedStates && (unit.Kind==UnitKind.Battleship || unit.Kind==UnitKind.Cruiser) )
			f++;
		}
	m=0;
	for( var i=1; i<=MaxUnitId; i++ )
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.UnitedStates && (unit.Kind==UnitKind.InfantryBase || unit.Kind==UnitKind.Pillboxes || unit.Kind==UnitKind.Fortress ) )
			m++;
		}
	if( f<=0 || m<=0 )
		{
		Result=GameResult.JapanWon;
		return false;
		}
	return true;
	}

private bool CheckResultOfScenario6()
	{
	var m=0;
	for( var i=1; i<=MaxUnitId; i++)
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.UnitedStates && (unit.Kind>=UnitKind.AirBase&&unit.Kind<=UnitKind.Fortress) )
			{
			// ミッドウェイ島
			// ptin debg
			var area=new WorldRect(80-(80*2), 3440+(80*2), 80+(80*4), 3440-(80*4));
			if( area.Contains(unit.Position))
				{
				m++;	// 生きてる基地で指定範囲内に要る
				}
			}
		}
	if( m==0 )
		{
		Result=GameResult.JapanWon;
		return false;
		}
	return true;
	}

private bool CheckResultOfScenario7()
	{
	var f=0;
	var m=0;
	for( var i=1; i<=MaxUnitId; i++)
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.UnitedStates && (unit.Kind>=UnitKind.AirBase&&unit.Kind<=UnitKind.Fortress) )
			{
			// ミッドウェイ島
			// ptin_dbg
			var area=new WorldRect(80-(80*2), 3440+(80*2), 80+(80*4), 3440-(80*4));
			if( area.Contains(unit.Position))
				{
				m++;	// 生きてる基地で指定範囲内に要る
				}
			}
		}
	for( var i=1; i<=MaxUnitId; i++)
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.Japan && (unit.Kind==UnitKind.Fortress) && unit.BuildTime==0 )
			{
			// ミッドウェイ島
			// ptin dbg
			var area=new WorldRect(80-(80*2), 3440+(80*2), 80+(80*4), 3440-(80*4));
			if( area.Contains(unit.Position))
				{
				f++;	// 生きてる基地で指定範囲内に要る
				}
			}
		}
	if( m==0 && f!=0)
		{
		Result=GameResult.JapanWon;
		return false;
		}
	return true;
	}

private bool CheckResultOfScenario8()
	{
	var f=0;
	var m=0;
	for( var i=1; i<=MaxUnitId; i++)
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.UnitedStates && (unit.Kind>=UnitKind.AirBase&&unit.Kind<=UnitKind.Fortress) )
			{
			// ミッドウェイ島
			// ptin dbg
			var area=new WorldRect(80-(80*2), 3440+(80*2), 80+(80*4), 3440-(80*4));
			if( area.Contains(unit.Position))
				{
				m++;	// 生きてる基地で指定範囲内に要る
				}
			}
		}
	for( var i=1; i<=MaxUnitId; i++)
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.Japan && (unit.Kind==UnitKind.Fortress) && unit.BuildTime==0 )
			{
			// ミッドウェイ島
			// ptin_dbg
			var area=new WorldRect(80-(80*2), 3440+(80*2), 80+(80*4), 3440-(80*4));
			if( area.Contains(unit.Position))
				{
				f++;	// 生きてる基地で指定範囲内に要る
				}
			}
		}
	if( m==0 && f!=0)
		{
		Result=GameResult.JapanWon;
		return false;
		}

	// ウェーク
/*
	rx=-4080;
	ry=-720;
	rx=-4080+80;
	ry=-720-160;
	m=set_new_unit(JPN,GF1,rx,ry,180);

	// ウェーク
	rx=-4080;
	ry=-720;
	rx=-4080+80;
	ry=-720-160;
*/
	f=0;
	m=0;
	for( var i=1; i<=MaxUnitId; i++)
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.Japan && (unit.Kind>=UnitKind.AirBase&&unit.Kind<=UnitKind.Fortress) )
			{
			// ウェーク
			var area=new WorldRect(-4080-80, -720+80, -4080+80, -720-160);

			if( area.Contains(unit.Position))
				{
				m++;	// 生きてる基地で指定範囲内に要る
				}
			}
		}
	for( var i=1; i<=MaxUnitId; i++)
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.UnitedStates && (unit.Kind==UnitKind.Fortress) && unit.BuildTime==0 )
			{
			// ウェーク
			var area=new WorldRect(-4080-80, -720+80, -4080+80, -720-160);

			if( area.Contains(unit.Position))
				{
				f++;	// 生きてる基地で指定範囲内に要る
				}
			}
		}
	if( m==0 && f!=0)
		{
		Result=GameResult.UnitedStatesWon;
		return false;
		}
	return true;
	}

private bool CheckResultOfScenario101()
	{
	var f=0;
	// ガダルカナル島
	var area=new WorldRect(-1040, -160, -1040+(80*3), -160-80);
	for( var i=1; i<=MaxUnitId; i++)
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.Japan && ( unit.Kind>=UnitKind.AirBase && unit.Kind<=UnitKind.Fortress ) && area.Contains(unit.Position) )
			{
			f++;
			}
		}

	if( f==0 )
		{
		Result=GameResult.UnitedStatesWon;
		return false;
		}
	return true;
	}

private bool CheckResultOfScenario102()
	{
	var f=0;
	var m=0;
	// ガダルカナル島
	var area=new WorldRect(-1040, -160, -1040+(80*3), -160-80);
	for( var i=1; i<=MaxUnitId; i++)
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.Japan && ( unit.Kind>=UnitKind.AirBase && unit.Kind<=UnitKind.Fortress ) && area.Contains(unit.Position) )
			{
			f++;
			}
		}
	for( var i=1; i<=MaxUnitId; i++)
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.UnitedStates && ( unit.Kind==UnitKind.AirBase )  && unit.BuildTime==0 && area.Contains(unit.Position) )
			{
			m++;
			}
		}

	if( f==0 && m>=2)
		{
		Result=GameResult.UnitedStatesWon;
		return false;
		}
	return true;
	}

private bool CheckResultOfScenario103()
	{
	var f=0;
	for( var i=1; i<=MaxUnitId; i++)
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.Japan && unit.Kind==UnitKind.City  )
			{
			f++;
			}
		}

	if( f==0 )
		{
		Result=GameResult.UnitedStatesWon;
		return false;
		}
	return true;
	}

private bool CheckResultOfScenario104()
	{
	var f=0;
	// ブーゲンビル島
	var area=new WorldRect(-4480-80, 1200+80, -4480+80*5, 1200-80*3);
	for( var i=1; i<=MaxUnitId; i++)
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.Japan && ( unit.Kind>=UnitKind.AirBase && unit.Kind<=UnitKind.Fortress ) && area.Contains(unit.Position) )
			{
			f++;
			}
		}

	if( f==0 )
		{
		Result=GameResult.UnitedStatesWon;
		return false;
		}
	return true;
	}

private bool CheckResultOfScenario105()
	{
	var f=0;
	// ブーゲンビル島
	var area=new WorldRect(-4480-80, 1200+80, -4480+80*5, 1200-80*3);
	for( var i=1; i<=MaxUnitId; i++)
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.Japan && ( unit.Kind>=UnitKind.AirBase && unit.Kind<=UnitKind.Fortress ) && area.Contains(unit.Position) )
			{
			f++;
			}
		}
	if( f==0 )
		{
		Result=GameResult.UnitedStatesWon;
		return false;
		}

	f=0;
	// ガダルカナル島
	area=new WorldRect(-1040, -160, -1040+(80*3), -160-80);
	for( var i=1; i<=MaxUnitId; i++)
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.UnitedStates && ( unit.Kind>=UnitKind.AirBase && unit.Kind<=UnitKind.Fortress ) && area.Contains(unit.Position) )
			{
			f++;
			}
		}

	if( f==0 )
		{
		Result=GameResult.JapanWon;
		return false;
		}
	return true;
	}

private bool CheckResultOfScenario106()
	{
	var area=new WorldRect(-1040, -160, -1040+(80*3), -160-80);

	var f=0;
	var m=0;

	for( var i=1; i<=MaxUnitId; i++)
		{
		ref var unit = ref Units[i];
		if( unit.Side==Side.Japan && ( unit.Kind>=UnitKind.AirBase && unit.Kind<=UnitKind.Fortress ) && area.Contains(unit.Position) )
			{
			f++;	//　日本の施設
			}
		if( unit.Side==Side.UnitedStates && ( unit.Kind>=UnitKind.AirBase && unit.Kind<=UnitKind.Fortress ) && area.Contains(unit.Position) )
			{
			m++;	//　米の施設
			}
		}

	if( f>=4 && m==0 )
		{
		Result=GameResult.JapanWon;
		return false;
		}
	if( f==0 && m>=4 )
		{
		Result=GameResult.UnitedStatesWon;
		return false;
		}
	return true;
	}

// The result of the battle, in ach[0] to ach[2]: that it is over, which side achieved its victory conditions, and the
// winner.
internal void WriteResult(ref Array5<int> length, ref Array5<Array128<byte>> text)
	{
	switch( Result )
		{
		case GameResult.JapanWon:
			length[0] = wsprintf(text[0], "ゲーム終了");
			length[1] = wsprintf(text[1], "日本海軍は勝利条件を達成した。");
			length[2] = wsprintf(text[2], "日本海軍の勝利");
			break;
		case GameResult.UnitedStatesWon:
			length[0] = wsprintf(text[0], "ゲーム終了");
			length[1] = wsprintf(text[1], "合衆国海軍は勝利条件を達成した。");
			length[2] = wsprintf(text[2], "合衆国海軍の勝利");
			break;
		case GameResult.JapanLost:
			length[0] = wsprintf(text[0], "ゲーム終了");
			length[1] = wsprintf(text[1], "日本海軍は勝利条件を達成できなかった。");
			length[2] = wsprintf(text[2], "");
			break;
		case GameResult.UnitedStatesLost:
			length[0] = wsprintf(text[0], "ゲーム終了");
			length[1] = wsprintf(text[1], "合衆国海軍は勝利条件を達成できなかった。");
			length[2] = wsprintf(text[2], "");
			break;
		case GameResult.Draw:
			length[0] = wsprintf(text[0], "ゲーム終了");
			length[1] = wsprintf(text[1], "日米両海軍は勝利目標を達成できなかった。");
			length[2] = wsprintf(text[2], "引き分け。");
			break;
		}
	}

//============================================================================
// デシジョン
//----------------------------------------------------------------------------
[Original("cnct_decision")]
public void	CheckResult()
	{

	Array5<Array128<byte>> text = default;
	 Array5<int> length = default;

	if( IsSingleplayer )
		{
		// 1.22's decision, which also shows the result.
		Trace?.Reach(this, TracePoint.Decision);
		RunDecision();
		return;
		}

	// 結果途中判定
	if( Result==GameResult.None && IsDecisionEnabled )
		{
		switch( ScenarioNumber )
			{
			case 1:
			case 2:
			case 3:
				// 空母起動部隊の戦い
				if( !CheckResultOfScenario1() )
					break;
				break;

			case 4:
			case 5:
				// 艦隊決戦
				if( !CheckResultOfScenario4() )
					break;
				break;

			case 6:
				// ミッドウェイ島攻略
				if( !CheckResultOfScenario6() )
					break;

				break;

			case 7:
				// ミッドウェイ島攻略
				if( !CheckResultOfScenario7() )
					break;

				break;

			case 8:
				// 中部太平洋の戦い
				if( !CheckResultOfScenario8() )
					break;
				break;

			case 9:
				// ユーザーマップ
				break;

			case 101:
				if( !CheckResultOfScenario101() )
					break;

				break;

			case 102:
				if( !CheckResultOfScenario102() )
					break;

				break;

			case 103:
				if( !CheckResultOfScenario103() )
					break;
				break;

			case 104:
				if( !CheckResultOfScenario104() )
					break;

				break;

			case 105:
				if( !CheckResultOfScenario105() )
					break;
				break;

			case 106:
				// ガ島争奪戦。
				// ガダルカナル島
				if( !CheckResultOfScenario106() )
					break;
				break;

			case 995:
				// ミッドウェイを巡る戦い１
				if( !Units[DecisionPoints[0]].IsUsed )
					{
					Result=GameResult.JapanWon;
					break;
					}
				break;
			}
		}

	// 結果はっぴょー
	if( Result!=GameResult.None )
		{
		Renderer.DrawResult(ref length, ref text);
		}

	}

private void SetBattleshipData(ref Unit unit)
	{
	unit.TurnRateChange=0.3;
	unit.AccelerationChange=0.01;
	unit.MinSpeed=0.0;
	unit.MaxSpeed=0.7;

	unit.SpriteRow=0;

	unit.Weapon=FireKind.Gun;		// 武装品種
	unit.Ammo=1000;		// 数
	unit.MaxAmmo=1000;		// 数 全容量

	unit.Fuel=100;		// 残燃料
	unit.FuelInterval=1000;		// 燃料を消費するタイミング

	unit.Skill=5;

	unit.Hp=unit.MaxHp=BB1_HP;		// Ｈｐ

	if( unit.Side==Side.Japan && unit.Variant==1 )
		{
		unit.TurnRateChange*=0.9;

		unit.AccelerationChange*=0.9;
		unit.MaxSpeed*=0.9;

		unit.Weapon=FireKind.NavalBaseGun;		// 武装品種
		unit.Ammo=(int)(unit.Ammo * 1.35);		// 数
		unit.MaxAmmo=(int)(unit.MaxAmmo * 1.35);		// 数 全容量

		unit.FuelInterval*=1.2;		// 残燃料

		unit.Hp=unit.MaxHp=unchecked((int)(BB1_HP*1.4));		// Ｈｐ
		}
	}

private void SetCruiserData(ref Unit unit)
	{
	unit.TurnRateChange=0.5;
	unit.AccelerationChange=0.01;
	unit.MinSpeed=0.0;
	unit.MaxSpeed=0.8;

	unit.SpriteRow=1;

	unit.Weapon=FireKind.Gun;		// 武装品種

	if( unit.Variant!=0  )
		{
		if( unit.Side==Side.Japan )
			{
			unit.Ammo=450;		// 数
			unit.MaxAmmo=450;		// 数 全容量
			}
		else
			{
			unit.Ammo=500;		// 数
			unit.MaxAmmo=500;		// 数 全容量
			}
		}
	else
		{
		unit.Ammo=600;		// 数
		unit.MaxAmmo=600;		// 数 全容量
		}

	unit.Fuel=100;		// 残燃料
	unit.FuelInterval=650;		// 燃料を消費するタイミング

	unit.Skill=5;

	unit.Hp=unit.MaxHp=CA1_HP;		// Ｈｐ
	if( unit.Variant!=0 )
		{
		if( unit.Side==Side.UnitedStates )
			unit.MaxHp=(int)(unit.MaxHp * 0.9);
		else
			unit.MaxHp=(int)(unit.MaxHp * 0.8);
		unit.Hp=unit.MaxHp;
		}
	}

private void SetDestroyerData(ref Unit unit)
	{
	unit.TurnRateChange=1.3;
	unit.AccelerationChange=0.05;
	unit.MinSpeed=0.0;
	unit.MaxSpeed=1.00;

	unit.SpriteRow=2;

	unit.Weapon=FireKind.Gun;		// 武装品種
	unit.Ammo=120;		// 数
	unit.MaxAmmo=120;		// 数 全容量

	unit.Fuel=100;		// 残燃料
	unit.FuelInterval=550;		// 燃料を消費するタイミング

	unit.Skill=5;

	unit.Hp=unit.MaxHp=DD1_HP;		// Ｈｐ
	if( unit.Variant!=0 )
		{
		if( unit.Side==Side.UnitedStates )
			unit.MaxHp=(int)(unit.MaxHp * 0.9);
		else
			unit.MaxHp=(int)(unit.MaxHp * 0.7);
		unit.Hp=unit.MaxHp;
		}
	}

private void SetSubmarineData(ref Unit unit)
	{
	unit.TurnRateChange=0.5;
	unit.AccelerationChange=0.02;
	unit.MinSpeed=0.0;
	unit.MaxSpeed=0.5;

	unit.SpriteRow=3;

	unit.Weapon=FireKind.Gun;		// 武装品種
	unit.Ammo=25;		// 数
	unit.MaxAmmo=25;		// 数 全容量

	unit.Fuel=100;		// 残燃料
	unit.FuelInterval=1000;		// 燃料を消費するタイミング

	unit.Skill=5;

	unit.Hp=unit.MaxHp=SS1_HP;		// Ｈｐ
	}

private void SetCarrierData(ref Unit unit, int m)
	{
	unit.TurnRateChange=0.3;
	unit.AccelerationChange=0.01;
	unit.MinSpeed=0.0;
	unit.MaxSpeed=0.7;

	unit.SpriteRow=6;

	unit.info[0]=0;					//
	unit.PlaneCount=CountPlanesIn(m);		// 現在収容数(飛行甲板上数も含む)
	unit.Capacity=12;					// 最大収容数
	unit.info[3]=0;					//
	unit.PlanesToLaunch=0;					// 発進予定機数 ０なら着艦可
	unit.Mode=UnitMode.Move;				// モード（コンバットメニュー）

	unit.Weapon=FireKind.Gun;	//0;		// 武装品種
	unit.Ammo=100;		// 数
	unit.MaxAmmo=100;		// 数 全容量

	unit.Fuel=100;		// 残燃料
	unit.FuelInterval=750;		// 燃料を消費するタイミング

	unit.Skill=5;

	unit.Hp=unit.MaxHp=CV1_HP+((unit.Side==Side.UnitedStates ? 1 : 0)*5);		// Ｈｐ

	if( unit.Side==Side.UnitedStates && unit.Variant==1 )
		{
		unit.TurnRateChange*=0.9;

		unit.AccelerationChange*=0.9;
		unit.MaxSpeed*=0.9;

		unit.Capacity=14;					// 最大収容数

		unit.Weapon=FireKind.Gun;		//0;		// 武装品種
		unit.Ammo=(int)(unit.Ammo * 1.1);		// 数
		unit.MaxAmmo=(int)(unit.MaxAmmo * 1.1);		// 数 全容量

		unit.FuelInterval*=1.6;		// 残燃料

		unit.Hp=(int)(unit.Hp * 1.15);
		unit.MaxHp=(int)(unit.MaxHp * 1.15);		// Ｈｐ
		}
	}

private void SetLightCarrierData(ref Unit unit, int m)
	{
	unit.TurnRateChange=0.5;
	unit.AccelerationChange=0.01;
	unit.MinSpeed=0.0;
	unit.MaxSpeed=0.9;

	unit.SpriteRow=5;

	unit.info[0]=0;					//
	unit.PlaneCount=CountPlanesIn(m);		// 現在収容数(飛行甲板上数も含む)
	unit.Capacity=8;					// 最大収容数
	unit.info[3]=0;					//
	unit.PlanesToLaunch=0;					// 発進予定機数 ０なら着艦可
	unit.Mode=UnitMode.Move;				// モード（コンバットメニュー）

	unit.Weapon=FireKind.Gun;	//0;		// 武装品種
	unit.Ammo=80;		// 数
	unit.MaxAmmo=80;		// 数 全容量

	unit.Fuel=100;		// 残燃料
	unit.FuelInterval=700;		// 燃料を消費するタイミング

	unit.Skill=5;

	unit.Hp=unit.MaxHp=CVL1_HP+((unit.Side==Side.UnitedStates ? 1 : 0)*5);		// Ｈｐ
	}

private void SetFighterData(ref Unit unit)
	{
	switch( unit.Variant )
		{
		case 0:		// 艦上戦闘機

			if(unit.Side==Side.Japan)
				{
				unit.TurnRateChange=4.0;
				unit.AccelerationChange=0.01;
				unit.MinSpeed=0.5;
				unit.MaxSpeed=2.35;

				if( unit.IsStopping )
					unit.SpriteRow=8;
				else
					unit.SpriteRow=7;

				unit.Weapon=FireKind.Bullet;		// 武装品種
				unit.Ammo=35;		// 数
				unit.MaxAmmo=35;		// 数 全容量

				unit.Fuel=100;		// 残燃料
				unit.FuelInterval=80;		// 燃料を消費するタイミング

				unit.Skill=7;

				unit.Hp=unit.MaxHp=FT1_HP;		// Ｈｐ
				}
			else
				{
				unit.TurnRateChange=2.2;
				unit.AccelerationChange=0.01;
				unit.MinSpeed=0.5;
				unit.MaxSpeed=2.5;

				if( unit.IsStopping )
					unit.SpriteRow=8;
				else
					unit.SpriteRow=7;

				unit.Weapon=FireKind.Bullet;		// 武装品種
				unit.Ammo=40;		// 数
				unit.MaxAmmo=40;		// 数 全容量

				unit.Fuel=100;		// 残燃料
				unit.FuelInterval=60;		// 燃料を消費するタイミング

				unit.Skill=5;

				unit.Hp=unit.MaxHp=FT1_HP+4;		// Ｈｐ
				}

			break;

		case 1:		// 陸上戦闘機

			if(unit.Side==Side.Japan)
				{
				unit.TurnRateChange=1.8;
				unit.AccelerationChange=0.008;
				unit.MinSpeed=0.5;
				unit.MaxSpeed=2.35;

				unit.SpriteRow=14;

				unit.Weapon=FireKind.Bullet;		// 武装品種
				unit.Ammo=50;		// 数
				unit.MaxAmmo=50;		// 数 全容量

				unit.Fuel=100;		// 残燃料
				unit.FuelInterval=100;		// 燃料を消費するタイミング

				unit.Skill=5;

				unit.Hp=unit.MaxHp=unchecked((int)(FT1_HP*0.8));		// Ｈｐ
				}
			else
				{
				unit.TurnRateChange=2.0;
				unit.AccelerationChange=0.02;
				unit.MinSpeed=0.5;
				unit.MaxSpeed=2.7;

				unit.SpriteRow=14;

				unit.Weapon=FireKind.Bullet;		// 武装品種
				unit.Ammo=60;		// 数
				unit.MaxAmmo=60;		// 数 全容量

				unit.Fuel=100;		// 残燃料
				unit.FuelInterval=95;		// 燃料を消費するタイミング

				unit.Skill=5;

				unit.Hp=unit.MaxHp=unchecked((int)(FT1_HP*2.0));		// Ｈｐ
				}

			break;
		}
	}

private void SetAttackerData(ref Unit unit)
	{
	if(unit.Side==Side.Japan)
		{
		unit.TurnRateChange=3.0;
		unit.AccelerationChange=0.01;
		unit.MinSpeed=0.5;
		unit.MaxSpeed=2.2;

		if( unit.IsStopping )
			unit.SpriteRow=10;
		else
			unit.SpriteRow=9;

		unit.Weapon=FireKind.Unarmed;		// 武装品種
		unit.Ammo=0;		// 数
		unit.MaxAmmo=1;		// 数 全容量

		unit.Fuel=100;		// 残燃料
		unit.FuelInterval=85;		// 燃料を消費するタイミング

		unit.Skill=5;

		unit.Hp=unit.MaxHp=AT1_HP;		// Ｈｐ
		}
	else
		{
		unit.TurnRateChange=3.0;
		unit.AccelerationChange=0.01;
		unit.MinSpeed=0.5;
		unit.MaxSpeed=2.2;

		if( unit.IsStopping )
			unit.SpriteRow=10;
		else
			unit.SpriteRow=9;

		unit.Weapon=FireKind.Unarmed;		// 武装品種
		unit.Ammo=0;		// 数
		unit.MaxAmmo=1;		// 数 全容量

		unit.Fuel=100;		// 残燃料
		unit.FuelInterval=70;		// 燃料を消費するタイミング

		unit.Skill=5;

		unit.Hp=unit.MaxHp=AT1_HP+4;		// Ｈｐ
		}
	}

private void SetBomberData(ref Unit unit)
	{
	if(unit.Side==Side.Japan)
		{
		unit.TurnRateChange=2.4;
		unit.AccelerationChange=0.005;
		unit.MinSpeed=0.5;
		unit.MaxSpeed=1.8;

		unit.SpriteRow=12;

		unit.Weapon=FireKind.Unarmed;		// 武装品種
		unit.Ammo=0;		// 数
		unit.MaxAmmo=9;		// 数 全容量

		unit.Fuel=100;		// 残燃料
		unit.FuelInterval=120;		// 燃料を消費するタイミング

		unit.Skill=5;

		unit.Hp=unit.MaxHp=unchecked((int)(BM1_HP*0.65));		// Ｈｐ
		}
	else
		{
		unit.TurnRateChange=2.0;
		unit.AccelerationChange=0.005;
		unit.MinSpeed=0.5;
		unit.MaxSpeed=1.9;

		unit.SpriteRow=12;

		unit.Weapon=FireKind.Unarmed;		// 武装品種
		unit.Ammo=0;		// 数
		unit.MaxAmmo=20;		// 数 全容量

		unit.Fuel=100;		// 残燃料
		unit.FuelInterval=220;		// 燃料を消費するタイミング

		unit.Skill=5;

		unit.Hp=unit.MaxHp=BM1_HP;		// Ｈｐ
		}
	}

private void SetTransportData(ref Unit unit)
	{
	unit.TurnRateChange=0.3;
	unit.AccelerationChange=0.01;
	unit.MinSpeed=0.0;
	unit.MaxSpeed=0.65;

	unit.SpriteRow=13;

	unit.Weapon=FireKind.Unarmed;		// 武装品種
	unit.Ammo=0;		// 数
	unit.MaxAmmo=0;		// 数 全容量

	unit.Fuel=100;		// 残燃料
	unit.FuelInterval=1000;		// 燃料を消費するタイミング

	unit.Skill=5;

	unit.Hp=unit.MaxHp=TR1_HP;		// Ｈｐ
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("set_unit_data")]
public void	SetUnitData(int m)
	{
	// A single-player game has NSPW 1.22's units, whose data differ from NSPW NET's: those created in the battle, such as
	// the bases that transports land, get them from 1.22's set_unit_data, as the scenario's do.
	if( IsSingleplayer )
		{
		RunSetUnitData(m);
		return;
		}

	ref var unit = ref Units[m];

	switch( unit.Kind )
		{
		case UnitKind.Battleship:
			SetBattleshipData(ref unit);
			break;

		case UnitKind.Cruiser:
			SetCruiserData(ref unit);
			break;

		case UnitKind.Destroyer:
			SetDestroyerData(ref unit);
			break;

		case UnitKind.Submarine:
			SetSubmarineData(ref unit);
			break;

		case UnitKind.Carrier:
			SetCarrierData(ref unit, m);
			break;

		case UnitKind.LightCarrier:
			SetLightCarrierData(ref unit, m);
			break;

		case UnitKind.Fighter:
			SetFighterData(ref unit);
			break;

		case UnitKind.Attacker:
			SetAttackerData(ref unit);
			break;

		case UnitKind.Bomber:
			SetBomberData(ref unit);
			break;

		case UnitKind.Transport:
			SetTransportData(ref unit);
			break;

		case UnitKind.NavalBase:
			unit.SpriteRow=11;

			unit.Skill=5;

			unit.Hp=unit.MaxHp=SP_HP;		// Ｈｐ

			unit.Direction=90.0;					// ９０がos_indx_x=0;
			break;
		case UnitKind.AirBase:
			unit.SpriteRow=11;
			unit.info[0]=0;					//
			unit.PlaneCount=CountPlanesIn(m);		// 現在収容数(飛行甲板上数も含む)
			unit.Capacity=16;					// 最大収容数
			unit.info[3]=0;					//
			unit.PlanesToLaunch=0;					// 発進予定機数 ０なら着艦可
			unit.Mode=UnitMode.Move;				// モード（コンバットメニュー）

			unit.Skill=5;

			unit.Hp=unit.MaxHp=AP_HP;		// Ｈｐ

			unit.Direction=90.0-45.0;					// ９０がos_indx_x=0;

			break;

		case UnitKind.City:
			unit.SpriteRow=11;

			unit.Weapon=0;		// 武装品種
			unit.Ammo=0;		// 数
			unit.MaxAmmo=0;		// 数 全容量

			unit.Direction=0.0;					// ９０がos_indx_x=0;

			unit.Skill=5;
			unit.Hp=unit.MaxHp=CT1_HP;		// Ｈｐ
			break;

		case UnitKind.InfantryBase:
			unit.SpriteRow=11;

			unit.Weapon=FireKind.Gun;		// 武装品種
			unit.Ammo=700;		// 数
			unit.MaxAmmo=700;		// 数 全容量

			unit.Direction=225.0;					// ９０がos_indx_x=0;

			unit.Skill=5;
			unit.Hp=unit.MaxHp=GF1_HP;		// Ｈｐ
			break;

		case UnitKind.Pillboxes:
			unit.SpriteRow=11;

			unit.Weapon=FireKind.Gun;		// 武装品種
			unit.Ammo=1500;		// 数
			unit.MaxAmmo=1500;		// 数 全容量

			unit.Direction=180.0;					// ９０がos_indx_x=0;

			unit.Skill=5;
			unit.Hp=unit.MaxHp=GF2_HP;		// Ｈｐ
			break;
		case UnitKind.Fortress:
			unit.SpriteRow=11;

			unit.Weapon=FireKind.Gun;		// 武装品種
			unit.Ammo=2000;		// 数
			unit.MaxAmmo=2000;		// 数 全容量

			unit.Direction=135.0;					// ９０がos_indx_x=0;

			unit.Skill=5;
			unit.Hp=unit.MaxHp=GF3_HP;		// Ｈｐ
			break;
		}

	//unit[m].hp[0]=10;		// 現在のＨｐ
	//unit[m].hp[1]=10;		// 最高Ｈｐ

	// 各ユニットの乱数データをセットします。
	for( var i=0; i<=1; i++)
		{
		unit.Random250[i]=(short)Random(250);
		unit.Random225[i]=(short)Random(225);

		unit.Random200[i]=(short)Random(200);
		unit.Random175[i]=(short)Random(175);
		unit.Random150[i]=(short)Random(150);
		unit.Random125[i]=(short)Random(125);

		unit.Random100[i]=(short)Random(100);
		unit.Random80[i]=(short)Random(80);
		unit.Random65[i]=(short)Random(65);
		unit.Random50[i]=(short)Random(50);
		unit.Random40[i]=(short)Random(40);
		unit.Random30[i]=(short)Random(30);
		unit.Random20[i]=(short)Random(20);
		unit.Random10[i]=(short)Random(10);
		}

	unit.SetPathPoint(0, unit.Position);
	unit.EndPath(1);
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("set_new_unit")]
public int		AddUnit(Side side,UnitKind kind,double x,double y,Angle direction)
	{
	UnitCategory	category; int start, end;

	if(kind==UnitKind.Attacker||kind==UnitKind.Fighter||kind==UnitKind.Bomber)
		category=UnitCategory.Plane;
	else
		category=UnitCategory.Ship;

	if(side==Side.Japan)
		{
		// 日本サイドのユニット
		if( category==UnitCategory.Ship )
			{
			start=JPN_SHIP_START;
			end=JPN_SHIP_END;
			}
		else
			{
			start=JPN_PLANE_START;
			end=JPN_PLANE_END;
			}
		}
	else
		{
		// 合衆国サイドのユニット
		if( category==UnitCategory.Ship )
			{
			start=USA_SHIP_START;
			end=USA_SHIP_END;
			}
		else
			{
			start=USA_PLANE_START;
			end=USA_PLANE_END;
			}
		}

	for(var m=start;m<=end;m++)
		{
		if( !Units[m].IsUsed )
			{
			// まずクリア
			Units[m].Side=0;
			Units[m].Position = new WorldPosition(0, 0);
			Units[m].Category=UnitCategory.None;
			Units[m].Kind=0;
			Units[m].Variant=0;
			for(var n=0;n<=15;n++)
				Units[m].info[n]=0;

			Units[m].SpriteRow=0;
			Units[m].SpriteColumn=0;
			Units[m].Direction=0;
			Units[m].TurnRate=0;
			Units[m].Speed=0;
			Units[m].Acceleration=0;
			Units[m].MaxSpeed=0;
			Units[m].MinSpeed=0;
			Units[m].AccelerationChange=0;
			Units[m].Speed=0;
			Units[m].IsStopping=false;
			Units[m].SupplyTime=0;
			Units[m].EmergencyFlags[0]=Units[m].EmergencyFlags[1]=0;
			Units[m].EmergencyDestination=new WorldPosition(0, 0);

			Units[m].IsGroupLeader=0;
			Units[m].GroupLeader=0;
			Units[m].FormationNumber=0;
			Units[m].ForGroupLeader=0;

			Units[m].FormationSpeed=0;

			for(var n=0;n<=7;n++)
				{
				Units[m].hp[n]=0;
				Units[m].arm[n]=0;
				Units[m].gas[n]=0;
				}
			Selections[0][m]=0;
			Selections[1][m]=0;
			Units[m].Skill=0;

			// あきスペース発見
			Units[m].Side=side;
			Units[m].Position = new WorldPosition(x, y);
			Units[m].Category=category;
			Units[m].Kind=kind;
			Units[m].Direction=direction;
			Units[m].Speed=0;
			Units[m].EmergencyFlags[0]=0;
			SetUnitData(m);
			return(m);
			}
		}
	return(0);

	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("set_new_unit_2")]
public int		AddUnit2(Side side,UnitKind kind,int type,double x,double y,Angle direction)
	{
	UnitCategory	category; int start, end;

	if(kind==UnitKind.Attacker||kind==UnitKind.Fighter||kind==UnitKind.Bomber)
		category=UnitCategory.Plane;
	else
		category=UnitCategory.Ship;

	if(side==Side.Japan)
		{
		// 日本サイドのユニット
		if( category==UnitCategory.Ship )
			{
			start=JPN_SHIP_START;
			end=JPN_SHIP_END;
			}
		else
			{
			start=JPN_PLANE_START;
			end=JPN_PLANE_END;
			}
		}
	else
		{
		// 合衆国サイドのユニット
		if( category==UnitCategory.Ship )
			{
			start=USA_SHIP_START;
			end=USA_SHIP_END;
			}
		else
			{
			start=USA_PLANE_START;
			end=USA_PLANE_END;
			}
		}

	for(var m=start;m<=end;m++)
		{
		if( !Units[m].IsUsed )
			{
			// まずクリア
			Units[m].Side=0;
			Units[m].Position = new WorldPosition(0, 0);
			Units[m].Category=UnitCategory.None;
			Units[m].Kind=0;
			Units[m].Variant=(short)type;
			for(var n=0;n<=15;n++)
				Units[m].info[n]=0;

			Units[m].SpriteRow=0;
			Units[m].SpriteColumn=0;
			Units[m].Direction=0;
			Units[m].TurnRate=0;
			Units[m].Speed=0;
			Units[m].Acceleration=0;
			Units[m].MaxSpeed=0;
			Units[m].MinSpeed=0;
			Units[m].AccelerationChange=0;
			Units[m].Speed=0;
			Units[m].IsStopping=false;
			Units[m].SupplyTime=0;
			Units[m].EmergencyFlags[0]=Units[m].EmergencyFlags[1]=0;
			Units[m].EmergencyDestination=new WorldPosition(0, 0);

			Units[m].IsGroupLeader=0;
			Units[m].GroupLeader=0;
			Units[m].FormationNumber=0;
			Units[m].ForGroupLeader=0;

			Units[m].FormationSpeed=0;

			for(var n=0;n<=7;n++)
				{
				Units[m].hp[n]=0;
				Units[m].arm[n]=0;
				Units[m].gas[n]=0;
				}
			Selections[0][m]=0;
			Selections[1][m]=0;
			Units[m].Skill=0;

			// あきスペース発見
			Units[m].Side=side;
			Units[m].Position = new WorldPosition(x, y);
			Units[m].Category=category;
			if(category==UnitCategory.Plane)
				Units[m].PlaneState=UnitState.Flying;
			Units[m].Kind=kind;
			Units[m].Direction=direction;
			Units[m].Speed=0;
			Units[m].EmergencyFlags[0]=0;
			SetUnitData(m);
			return(m);
			}
		}
	return(0);

	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("set_new_unit_plane")]
public int		AddPlane(Side side,UnitKind kind,int type,int carrier,int planes,FireKind arm)
	{
	 int start, end, f=default /* C4701 */;
	Array24<int> space = default;

	if(side==Side.Japan)
		{
		// 日本サイドのユニット
		start=JPN_PLANE_START;
		end=JPN_PLANE_END;
		}
	else
		{
		// 合衆国サイドのユニット
		start=USA_PLANE_START;
		end=USA_PLANE_END;
		}

	for(var m=start;m<=end && planes!=0 ;m++)
		{
		if( !Units[m].IsUsed )
			{
			// あきスペース発見

			for( var n=0; n<24; n++)
				{
				space[n]=0;
				}
			var park=0;
			for(var n=1;n<=MaxUnitId;n++)
				{
				ref var unit = ref Units[n];
				if( unit.IsUsed && unit.Category==UnitCategory.Plane && unit.Carrier==carrier && unit.PlaneState==UnitState.Parked )
					{
					space[unit.ParkingNumber]=1;
					park++;
					}
				}

			// 1.22 adds the planes to a full carrier too (0x41F060).
			if( park >= Units[carrier].Capacity && Rules.FullCarriersRefusePlanes )
				return (-planes);

			for( var n=0; n<24; n++)
				{
				if(space[n]==0)
					{
					f=n;				// 格納庫の位置、及び、その基地の番機番号
					break;
					}
				}

			Units[m].Side=side;
			Units[m].Position = new WorldPosition(730, 150);
			Units[m].Category=UnitCategory.Plane;
			Units[m].Kind=kind;
			Units[m].Variant=(short)type;
			Units[m].PlaneState=UnitState.Parked;
			Units[m].Carrier=carrier;					// 所属の空母、及び、基地の番号
			// BUG(1.22, NSPW NET): with no parking place left, f is the previous plane's, or, for the first, read before it
			// is set: whatever the original's stack holds, 0 in the port (0x41F111). Kept as is for compatibility with 1.22
			// and NSPW NET.
			Units[m].ParkingNumber=f;				// 格納庫の位置、及び、その基地の番機番号
			Units[m].DeckPhase=0;					// 8
			Units[m].PlanesToLaunch=0;					// 発艦予定の機数
			Units[m].Mode=UnitMode.Move;				// モード（コンバットメニュー）
			SetParkingPosition(m);
			Units[m].IsStopping=true;

			Units[Units[m].Carrier].PlaneCount++;					// 所属の空母、及び、基地の格納数を増やす｡

			SetUnitData(m);

			if( kind!=UnitKind.Fighter  )
				{
				if( arm==FireKind.Unarmed )
					{
					Units[m].Weapon=arm;		// 武装品種
					Units[m].Ammo=0;		// 数
					//unit[m].arm[4]=1;		// 数
					}
				else
					{
					Units[m].Weapon=arm;		// 武装品種
					Units[m].Ammo=Units[m].MaxAmmo;		// 数
					//unit[m].arm[4]=1;		// 数
					}
				}
			planes--;
			}
		}

	if( planes==0 )
		return(1);
	else
		return(-planes);
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cnct_sinario_1")]
public void	SetUpScenario1()
	{

	if(Mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ

		SupplyRates[0]=0;		// Host
		SupplyRates[1]=0;		// Guest

		InitialSupplyPoints[0]=0;		// Host
		InitialSupplyPoints[1]=0;		// Guest

		SwapRule=0;
		SwapTime=0;

		ArrivalControl=1;
		IsDecisionEnabled=true;

		return;
		}

	//  ゲーム設定

	//===============		 日本海軍		================

	/*クエゼリン環礁*/
	var x=-7120.0;
	var y=6720.0;
	AddUnit(Side.Japan,UnitKind.NavalBase,x,y,(double)0);

	/*クエゼリン*/
	x=-7120-80;
	y=6720;
	AddUnit(Side.Japan,UnitKind.InfantryBase,x,y,(double)0);

	x=-2500;
	y=2500;
	var m=AddUnit(Side.Japan,UnitKind.Carrier,x,y,(double)0);
	var carrier=m;
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,9,FireKind.Torpedo);
	AddPlane(Side.Japan,UnitKind.Fighter,0,carrier,3,FireKind.Unarmed);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,(double)0);

	//===============		 合衆国海軍		================

	/*ヌーメア軍港*/
	x=5440;
	y=-5760;
	AddUnit(Side.UnitedStates,UnitKind.NavalBase,x,y,(double)0);

	/*ヌーメア*/
	x=5440-80;
	y=-5760;
	AddUnit(Side.UnitedStates,UnitKind.InfantryBase,x,y,(double)0);

	x=2500;
	y=-2500;

	m=AddUnit(Side.UnitedStates,UnitKind.Carrier,x,y,(double)180);
	carrier=m;
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,9,FireKind.Bomb);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,3,FireKind.Unarmed);

	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Destroyer,x,y,(double)180);

	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,(double)180);

	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,(double)180);

	// マップの作成
	LoadScenarioFile2("Map\\South_pacific.dat");

	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cnct_sinario_2")]
public void	SetUpScenario2()
	{

	if(Mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ

		SupplyRates[0]=0;		// Host
		SupplyRates[1]=0;		// Guest

		InitialSupplyPoints[0]=0;		// Host
		InitialSupplyPoints[1]=0;		// Guest

		SwapRule=0;
		SwapTime=0;

		ArrivalControl=1;
		IsDecisionEnabled=true;

		return;
		}

	//  ゲーム設定

	//===============		 日本海軍		================
	// トラック島 港
	var x=-7600.0;
	var y=-4720.0;
	AddUnit(Side.Japan,UnitKind.NavalBase,x,y,(double)0);

	// トラック島
	x=-7600-80;
	y=-4720;
	AddUnit(Side.Japan,UnitKind.InfantryBase,x,y,(double)0);

	x=-5000;
	y=-2500;

	var m=AddUnit(Side.Japan,UnitKind.Carrier,x,y,0);
	var carrier=m;
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,9,FireKind.Torpedo);
	AddPlane(Side.Japan,UnitKind.Fighter,0,carrier,3,FireKind.Unarmed);
	x-=120;
	m=AddUnit(Side.Japan,UnitKind.LightCarrier,x,y,(double)0);
	carrier=m;
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,6,FireKind.Torpedo);
	AddPlane(Side.Japan,UnitKind.Fighter,0,carrier,2,FireKind.Unarmed);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,(double)0);

	//===============		 合衆国海軍		================
	// ハワイ港
	x=7200;
	y=-720;
	AddUnit(Side.UnitedStates,UnitKind.NavalBase,x,y,(double)90.0);

	// ハワイ
	x=7200-80;
	y=-720+80;
	AddUnit(Side.UnitedStates,UnitKind.InfantryBase,x,y,(double)90.0);

	x=5000;
	y=2500;

	m=AddUnit(Side.UnitedStates,UnitKind.Carrier,x,y,(double)180);
	carrier=m;
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,9,FireKind.Bomb);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,3,FireKind.Unarmed);

	x+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.LightCarrier,x,y,(double)180);
	carrier=m;
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,6,FireKind.Bomb);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,2,FireKind.Unarmed);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Destroyer,x,y,(double)180);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,(double)180);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,(double)180);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,(double)180);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Destroyer,x,y,(double)180);

	// マップの作成
	LoadScenarioFile2("Map\\Middle_pacific.dat");

	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cnct_sinario_3")]
public void	SetUpScenario3()
	{
	int m, n;

	if(Mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ

		// 増援設定などの設定のみ
		if(HostSide==0)
			{
			m=0;
			n=1;
			}
		else
			{
			m=1;
			n=0;
			}

		// 日本海軍側
		SupplyRates[m]=3;
		InitialSupplyPoints[m]=50;

		//合衆国海軍側
		SupplyRates[n]=3;
		InitialSupplyPoints[n]=50;

		SwapRule=0;
		SwapTime=0;

		ArrivalControl=5;

		IsDecisionEnabled=true;

		return;
		}

	//  ゲーム設定

	//===============		 日本海軍		================
	/*クエゼリン環礁*/

	// 横須賀
	// 港
	var x=-7440.0;
	var y=4880.0;
	AddUnit(Side.Japan,UnitKind.NavalBase,x,y,(double)0);

	// 横須賀
	x=-7440-80;
	y=4880+80;
	AddUnit(Side.Japan,UnitKind.InfantryBase,x,y,(double)0);

	x=-6500;
	y=5000;

	m=AddUnit(Side.Japan,UnitKind.Carrier,x,y,(double)0);
	var carrier=m;
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,9,FireKind.Torpedo);
	AddPlane(Side.Japan,UnitKind.Fighter,0,carrier,3,FireKind.Unarmed);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,(double)0);

	x-=120;
	AddUnit(Side.Japan,UnitKind.Battleship,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,(double)0);

	//===============		 合衆国海軍		================
	/*ヌーメア軍港*/

	// パラオ
	x=6720-80;
	y=-3760;
	AddUnit(Side.UnitedStates,UnitKind.NavalBase,x,y,(double)0);

	// パラオ
	x=6720-80;
	y=-3760+80;
	AddUnit(Side.UnitedStates,UnitKind.InfantryBase,x,y,(double)0);

	x=6500;
	y=-5000;

	m=AddUnit(Side.UnitedStates,UnitKind.Carrier,x,y,(double)180);
	carrier=m;
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,9,FireKind.Bomb);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,3,FireKind.Unarmed);

	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Destroyer,x,y,(double)180);

	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,(double)180);

	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,(double)180);

	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Battleship,x,y,(double)180);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,(double)180);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Destroyer,x,y,(double)180);

	// マップの作成
	LoadScenarioFile2("Map\\Japan_off.dat");

	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cnct_sinario_4")]
public void	SetUpScenario4()
	{

	// 艦隊決戦１

	// 南太平洋

	if(Mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ

		SupplyRates[0]=0;		// Host
		SupplyRates[1]=0;		// Guest

		InitialSupplyPoints[0]=0;		// Host
		InitialSupplyPoints[1]=0;		// Guest

		SwapRule=0;
		SwapTime=0;

		ArrivalControl=1;
		IsDecisionEnabled=true;

		return;
		}

	//  ゲーム設定

	//===============		 日本海軍		================

	/*クエゼリン環礁*/
	var x=-7120.0;
	var y=6720.0;
	AddUnit(Side.Japan,UnitKind.NavalBase,x,y,(double)0);

	// クエゼリン
	x=-7120-80;
	y=6720;
	AddUnit(Side.Japan,UnitKind.InfantryBase,x,y,(double)0);

	x=-2500;
	y=2500;

	AddUnit(Side.Japan,UnitKind.Battleship,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,(double)0);

	//===============		 合衆国海軍		================

	/*ヌーメア軍港*/
	x=5440;
	y=-5760;
	AddUnit(Side.UnitedStates,UnitKind.NavalBase,x,y,(double)0);

	// ヌーメア
	x=5440-80;
	y=-5760-80;
	AddUnit(Side.UnitedStates,UnitKind.InfantryBase,x,y,(double)0);

	x=2500;
	y=-2500;

	AddUnit(Side.UnitedStates,UnitKind.Battleship,x,y,(double)180);
	x+=120;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,(double)180);
	x+=120;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,(double)180);
	x+=120;
	AddUnit(Side.UnitedStates,UnitKind.Destroyer,x,y,(double)180);
	x+=120;
	AddUnit(Side.UnitedStates,UnitKind.Destroyer,x,y,(double)180);
	x+=120;
	AddUnit(Side.UnitedStates,UnitKind.Destroyer,x,y,(double)180);

	// マップの作成
	LoadScenarioFile2("Map\\South_pacific.dat");

	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cnct_sinario_5")]
public void	SetUpScenario5()
	{
	int m, n;

	// 艦隊決戦２

	// 南太平洋

	if(Mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ
		// 増援設定などの設定のみ
		if(HostSide==0)
			{
			m=0;
			n=1;
			}
		else
			{
			m=1;
			n=0;
			}

		// 日本海軍側
		SupplyRates[m]=8;
		InitialSupplyPoints[m]=0;

		//合衆国海軍側
		SupplyRates[n]=8;
		InitialSupplyPoints[n]=0;

		SwapRule=0;
		SwapTime=0;

		ArrivalControl=3;		// ３：輸送船のみ可

		IsDecisionEnabled=true;

		return;
		}

	//  ゲーム設定

	//===============		 日本海軍		================

	/*クエゼリン環礁*/
	var x=-7120.0;
	var y=6720.0;
	AddUnit(Side.Japan,UnitKind.NavalBase,x,y,(double)0);

	// クエゼリン環礁
	x=-7120-80;
	y=6720;
	AddUnit(Side.Japan,UnitKind.InfantryBase,x,y,(double)0);

	x=-2500;
	y=2500;

	AddUnit(Side.Japan,UnitKind.Battleship,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Battleship,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,(double)0);

	x-=120;
	AddUnit(Side.Japan,UnitKind.Submarine,x,y,(double)0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Submarine,x,y,(double)0);

	//===============		 合衆国海軍		================

	/*ヌーメア軍港*/
	x=5440;
	y=-5760;
	AddUnit(Side.UnitedStates,UnitKind.NavalBase,x,y,(double)0);

	// ヌーメア軍港
	x=5440-80;
	y=-5760-80;
	AddUnit(Side.UnitedStates,UnitKind.InfantryBase,x,y,(double)0);

	x=2500;
	y=-2500;
	AddUnit(Side.UnitedStates,UnitKind.Battleship,x,y,(double)180);
	x+=120;
	AddUnit(Side.UnitedStates,UnitKind.Battleship,x,y,(double)180);
	x+=120;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,(double)180);
	x+=120;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,(double)180);
	x+=120;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,(double)180);
	x+=120;
	AddUnit(Side.UnitedStates,UnitKind.Destroyer,x,y,(double)180);
	x+=120;
	AddUnit(Side.UnitedStates,UnitKind.Destroyer,x,y,(double)180);
	x+=120;
	AddUnit(Side.UnitedStates,UnitKind.Destroyer,x,y,(double)180);
	x+=120;
	AddUnit(Side.UnitedStates,UnitKind.Destroyer,x,y,(double)180);

	x+=120;
	AddUnit(Side.UnitedStates,UnitKind.Submarine,x,y,(double)180);
	x+=120;
	AddUnit(Side.UnitedStates,UnitKind.Submarine,x,y,(double)180);

	// マップの作成
	LoadScenarioFile2("Map\\South_pacific.dat");

	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cnct_sinario_6")]
public void	SetUpScenario6()
	{
	int m, n;

	// ミッドウェイ島攻略１
	// 中部太平洋
	if(Mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ
		if(HostSide==0)
			{
			m=0;
			n=1;
			}
		else
			{
			m=1;
			n=0;
			}

		// 日本海軍側
		SupplyRates[m]=10;
		InitialSupplyPoints[m]=1500;

		//合衆国海軍側
		SupplyRates[n]=5;
		InitialSupplyPoints[n]=500;

		SwapRule=0;
		SwapTime=0;

		ArrivalControl=0;

		IsDecisionEnabled=true;

		return;
		}

	//  ゲーム設定

	//===============		 日本海軍		================
	// トラック
	var x=-7600.0;
	var y=-4720.0;
	AddUnit(Side.Japan,UnitKind.NavalBase,x,y,135);

	// 空港
	x=-7600-80;
	y=-4720;
	m=AddUnit(Side.Japan,UnitKind.AirBase,x,y,135);
	var carrier=m;
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,5,FireKind.Bomb);
	AddPlane(Side.Japan,UnitKind.Fighter,0,carrier,3,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Attacker,1,carrier,1,FireKind.Bomb);

	// 戦闘艦船
	x=-7450;
	y=-4550;
	m=AddUnit(Side.Japan,UnitKind.Battleship,x-50,y,135);
	Units[m].Fuel*=0.1;
	Units[m].Ammo=(int)(Units[m].MaxAmmo*0.2);			// 数
	Units[m].SupplyTime=1;
	y-=150;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,x,y,135);
	Units[m].Fuel*=0.2;
	Units[m].Ammo=(int)(Units[m].MaxAmmo*0.1);			// 数
	Units[m].SupplyTime=1;
	y-=150;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,x,y,135);
	Units[m].Fuel*=0.1;
	Units[m].Ammo=(int)(Units[m].MaxAmmo*0.2);			// 数
	Units[m].SupplyTime=1;
	y-=150;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,x-80,y,135);
	Units[m].Fuel*=0.1;
	Units[m].Ammo=(int)(Units[m].MaxAmmo*0.3);			// 数
	Units[m].SupplyTime=1;

	// 輸送船団
	x=-7340;
	y=-4690;
	m=AddUnit(Side.Japan,UnitKind.Transport,x,y,270);
	Units[m].Weapon=FireKind.CargoInfantryBase;		// 武装品種
	Units[m].Ammo=1;			// 数
	Units[m].MaxAmmo=1;			// 数 全容量
	Units[m].Fuel*=0.2;
	Units[m].SupplyTime=1;
	y-=150;
	m=AddUnit(Side.Japan,UnitKind.Transport,x-20,y,135);
	Units[m].Weapon=FireKind.CargoPillboxes;		// 武装品種
	Units[m].Ammo=1;			// 数
	Units[m].MaxAmmo=1;			// 数 全容量
	Units[m].Fuel*=0;
	Units[m].SupplyTime=1;
	y-=150;
	m=AddUnit(Side.Japan,UnitKind.Transport,x,y,90);
	Units[m].Weapon=FireKind.CargoPillboxes;		// 武装品種
	Units[m].Ammo=1;			// 数
	Units[m].MaxAmmo=1;			// 数 全容量
	Units[m].Fuel*=0;
	Units[m].SupplyTime=1;
	y-=150;
	AddUnit(Side.Japan,UnitKind.Cruiser,x+90,y-40,90);

	// 潜水艦
	x=-7800;
	y=-2000;
	AddUnit(Side.Japan,UnitKind.Submarine,x,y,0);

	//===============		 合衆国海軍		================
	// ハワイ港
	x=7200;
	y=-640;
	AddUnit(Side.UnitedStates,UnitKind.NavalBase,x,y,90.0);

	// ハワイ島 基地
	x=7200-80;
	y=-640+80*2;
	AddUnit(Side.UnitedStates,UnitKind.Fortress,x,y,0);

	// ハワイ空港
	x=7200;
	y=-560;
	m=AddUnit(Side.UnitedStates,UnitKind.AirBase,x,y,45.0);
	carrier=m;
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,1,FireKind.Unarmed);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,3,FireKind.Unarmed);
	AddPlane(Side.UnitedStates,UnitKind.Bomber,0,carrier,4,FireKind.Unarmed);

	// ミッドウェイ島 基地
	x=80+160;
	y=3440-80;
	AddUnit(Side.UnitedStates,UnitKind.Pillboxes,x,y,0);

	// ミッドウェイ島航空基地 1
	x=80;
	y=3440;
	carrier=AddUnit(Side.UnitedStates,UnitKind.AirBase,x,y,0);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,2,FireKind.Unarmed);
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,2,FireKind.Unarmed);

	// 機動部隊
	x=-3500;
	y=-3000;
	carrier=m=AddUnit(Side.UnitedStates,UnitKind.Carrier,x,y,180);
	Units[m].Fuel/=2;
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,10,FireKind.Bomb);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,2,FireKind.Unarmed);
	x+=150;
	carrier=m=AddUnit(Side.UnitedStates,UnitKind.LightCarrier,x,y,180);
	Units[m].Fuel/=2;
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,6,FireKind.Bomb);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,2,FireKind.Unarmed);
	x+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,180);
	Units[m].Fuel/=2;
	x+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,x,y,180);
	Units[m].Fuel/=2;
	x+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,180);
	Units[m].Fuel/=2;

	// マップの作成
	LoadScenarioFile2("Map\\Middle_pacific.dat");

	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cnct_sinario_7")]
public void	SetUpScenario7()
	{
	int m, n;

	// ミッドウェイ島攻略２
	// 中部太平洋
	if(Mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ
		if(HostSide==0)
			{
			m=0;
			n=1;
			}
		else
			{
			m=1;
			n=0;
			}

		// 日本海軍側
		SupplyRates[m]=12;
		InitialSupplyPoints[m]=1500;

		//合衆国海軍側
		SupplyRates[n]=5;
		InitialSupplyPoints[n]=500;

		SwapRule=0;
		SwapTime=0;

		ArrivalControl=0;

		IsDecisionEnabled=true;

		return;
		}

	//  ゲーム設定

	//===============		 日本海軍		================

	// トラック
	var x=-7600.0;
	var y=-4720.0;
	AddUnit(Side.Japan,UnitKind.NavalBase,x,y,135);

	// 空港
	x=-7600-80;
	y=-4720;
	m=AddUnit(Side.Japan,UnitKind.AirBase,x,y,135);
	var carrier=m;
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,5,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Fighter,0,carrier,3,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Attacker,1,carrier,1,FireKind.Unarmed);

	// 戦闘艦船
	x=-7450;
	y=-4550;
	m=AddUnit(Side.Japan,UnitKind.Battleship,x-50,y,135);
	Units[m].Fuel*=0.1;
	Units[m].Ammo=(int)(Units[m].MaxAmmo*0.2);			// 数
	Units[m].SupplyTime=1;
	y-=150;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,x,y,135);
	Units[m].Fuel*=0.2;
	Units[m].Ammo=(int)(Units[m].MaxAmmo*0.1);			// 数
	Units[m].SupplyTime=1;
	y-=150;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,x,y,135);
	Units[m].Fuel*=0.1;
	Units[m].Ammo=(int)(Units[m].MaxAmmo*0.2);			// 数
	Units[m].SupplyTime=1;
	y-=150;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,x-80,y,135);
	Units[m].Fuel*=0.1;
	Units[m].Ammo=(int)(Units[m].MaxAmmo*0.3);			// 数
	Units[m].SupplyTime=1;

	// 輸送船団
	x=-7340;
	y=-4690;
	m=AddUnit(Side.Japan,UnitKind.Transport,x,y,270);
	Units[m].Weapon=FireKind.CargoInfantryBase;		// 武装品種
	Units[m].Ammo=1;			// 数
	Units[m].MaxAmmo=1;			// 数 全容量
	Units[m].Fuel*=0.2;
	Units[m].SupplyTime=1;
	y-=150;
	m=AddUnit(Side.Japan,UnitKind.Transport,x-20,y,135);
	Units[m].Weapon=FireKind.CargoPillboxes;		// 武装品種
	Units[m].Ammo=1;			// 数
	Units[m].MaxAmmo=1;			// 数 全容量
	Units[m].Fuel*=0;
	Units[m].SupplyTime=1;
	y-=150;
	m=AddUnit(Side.Japan,UnitKind.Transport,x,y,90);
	Units[m].Weapon=FireKind.CargoPillboxes;		// 武装品種
	Units[m].Ammo=1;			// 数
	Units[m].MaxAmmo=1;			// 数 全容量
	Units[m].Fuel*=0;
	Units[m].SupplyTime=1;
	y-=150;
	AddUnit(Side.Japan,UnitKind.Cruiser,x+90,y-40,90);

	// 潜水艦
	x=-7800;
	y=-2000;
	AddUnit(Side.Japan,UnitKind.Submarine,x,y,0);

/**
rx=80-800;
ry=3440;
m=set_new_unit(JPN,TR1,rx,ry,90);
unit[m].arm[0]=TR_GF3;		// 武装品種
unit[m].arm[1]=1;			// 数
unit[m].arm[4]=1;			// 数 全容量
**/

	//===============		 合衆国海軍		================
	// ハワイ港
	x=7200;
	y=-640;
	AddUnit(Side.UnitedStates,UnitKind.NavalBase,x,y,90.0);

	// ハワイ島 基地
	x=7200-80;
	y=-640+80*2;
	AddUnit(Side.UnitedStates,UnitKind.Fortress,x,y,0);

	// ハワイ空港
	x=7200;
	y=-560;
	m=AddUnit(Side.UnitedStates,UnitKind.AirBase,x,y,45.0);
	carrier=m;
	AddPlane(Side.UnitedStates,UnitKind.Fighter,1,carrier,3,FireKind.Unarmed);
	AddPlane(Side.UnitedStates,UnitKind.Bomber,0,carrier,2,FireKind.Unarmed);
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,4,FireKind.Unarmed);

	// ミッドウェイ島 基地
	x=80+160;
	y=3440-80;
	AddUnit(Side.UnitedStates,UnitKind.Pillboxes,x,y,0);

	// ミッドウェイ島航空基地 1
	x=80;
	y=3440;
	carrier=AddUnit(Side.UnitedStates,UnitKind.AirBase,x,y,0);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,2,FireKind.Unarmed);
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,2,FireKind.Unarmed);

	// 機動部隊
	x=-3500;
	y=-3000;
	carrier=m=AddUnit(Side.UnitedStates,UnitKind.Carrier,x,y,180);
	Units[m].Fuel/=2;
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,10,FireKind.Bomb);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,2,FireKind.Unarmed);
	x+=150;
	carrier=m=AddUnit(Side.UnitedStates,UnitKind.LightCarrier,x,y,180);
	Units[m].Fuel/=2;
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,6,FireKind.Bomb);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,2,FireKind.Unarmed);
	x+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,180);
	Units[m].Fuel/=2;
	x+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,x,y,180);
	Units[m].Fuel/=2;
	x+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,180);
	Units[m].Fuel/=2;

	// マップの作成
	LoadScenarioFile2("Map\\Middle_pacific.dat");

	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cnct_sinario_8")]
public void	SetUpScenario8()
	{
	int m, n;

	// 中部太平洋の戦い
	// 中部太平洋
	if(Mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ
		if(HostSide==0)
			{
			m=0;
			n=1;
			}
		else
			{
			m=1;
			n=0;
			}

		// 日本海軍側
		SupplyRates[m]=8;
		InitialSupplyPoints[m]=2000;

		//合衆国海軍側
		SupplyRates[n]=14;
		InitialSupplyPoints[n]=800;

		SwapRule=0;
		SwapTime=0;

		ArrivalControl=0;

		IsDecisionEnabled=true;

		return;
		}

	//  ゲーム設定

	//===============		 日本海軍		================

	// トラック
	var x=-7600.0;
	var y=-4720.0;
	AddUnit(Side.Japan,UnitKind.NavalBase,x,y,135);

	// 空港
	x=-7600-80;
	y=-4720;
	m=AddUnit(Side.Japan,UnitKind.AirBase,x,y,135);
	var carrier=m;
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,3,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Fighter,0,carrier,3,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Attacker,1,carrier,2,FireKind.Unarmed);

	// ウェーク
	x=-4080;
	y=-720;
	m=AddUnit(Side.Japan,UnitKind.AirBase,x,y,180);
	carrier=m;
	AddPlane(Side.Japan,UnitKind.Fighter,0,carrier,6,FireKind.Bomb);
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,3,FireKind.Unarmed);

	x=-4080+80;
	y=-720-160;
	AddUnit(Side.Japan,UnitKind.InfantryBase,x,y,180);

	// 機動部隊
	x=-7400;
	y=-4200;
	carrier=AddUnit(Side.Japan,UnitKind.Carrier,x,y,90);
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,10,FireKind.Bomb);
	AddPlane(Side.Japan,UnitKind.Fighter,0,carrier,2,FireKind.Unarmed);
	y-=90;
	carrier=AddUnit(Side.Japan,UnitKind.LightCarrier,x,y,90);
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,0,FireKind.Bomb);
	AddPlane(Side.Japan,UnitKind.Fighter,3,carrier,0,FireKind.Unarmed);
	y-=90;
	AddUnit(Side.Japan,UnitKind.Battleship,x,y,90);
	y-=90;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,90);
	y-=90;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,90);
	y-=90;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,90);

	// 戦闘艦船
	x=-7500;
	y=-4550;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,45);
	y-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x+40,y,135);
	y-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x+20,y,270);

	// 潜水艦
	x=-7800;
	y=-5000;
	AddUnit(Side.Japan,UnitKind.Submarine,x,y,45);

	//===============		 合衆国海軍		================
	// ハワイ港
	x=7200;
	y=-640;
	AddUnit(Side.UnitedStates,UnitKind.NavalBase,x,y,90.0);
	// ハワイ島 基地
	x=7200;
	y=-640+80;
	AddUnit(Side.UnitedStates,UnitKind.Fortress,x,y,0);
	// ハワイ島 空港
	x=7200+80;
	y=-640+80+80;
	carrier=AddUnit(Side.UnitedStates,UnitKind.AirBase,x,y,0);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,5,FireKind.Unarmed);
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,3,FireKind.Unarmed);
	AddPlane(Side.UnitedStates,UnitKind.Bomber,0,carrier,2,FireKind.Unarmed);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,1,carrier,2,FireKind.Unarmed);

	// ミッドウェイ島 基地
	x=80+160;
	y=3440-80;
	AddUnit(Side.UnitedStates,UnitKind.Pillboxes,x,y,0);

	// ミッドウェイ島航空基地 1
	x=80;
	y=3440;
	carrier=AddUnit(Side.UnitedStates,UnitKind.AirBase,x,y,0);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,2,FireKind.Unarmed);
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,2,FireKind.Unarmed);

	// 機動部隊
	x=3500;
	y=3000;
	carrier=AddUnit(Side.UnitedStates,UnitKind.Carrier,x,y,180);
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,10,FireKind.Bomb);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,2,FireKind.Unarmed);
	x+=150;
	carrier=AddUnit(Side.UnitedStates,UnitKind.LightCarrier,x,y,180);
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,6,FireKind.Bomb);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,2,FireKind.Unarmed);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,180);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Destroyer,x,y,180);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,180);

	// 艦隊
	x=7200+140;
	y=-640+80;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,45);
	x+=120;
	AddUnit(Side.UnitedStates,UnitKind.Battleship,x,y,45);
	x+=120;
	AddUnit(Side.UnitedStates,UnitKind.Submarine,x,y,45);

	// マップの作成
	LoadScenarioFile2("Map\\Middle_pacific.dat");

	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cnct_sinario_9")]
public void	SetUpScenario9()
	{

	Array256<Array256<ushort>> path;					// マップ

	if(Mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ

/*
		if(host_side==0)
			{
			m=0;
			n=1;
			}
		else
			{
			m=1;
			n=0;
			}
		// 日本海軍側
		spry_rate[m]=0;
		first_spry_pt[m]=0;

		//合衆国海軍側
		spry_rate[n]=0;
		first_spry_pt[n]=0;

		arrival_cont=0;

		decision_sw=0;
*/
		LoadUserMap();

		if(HostSide==0)
			{
			}
		else
			{
			(InitialSupplyPoints[0], InitialSupplyPoints[1])=(InitialSupplyPoints[1], InitialSupplyPoints[0]);

			(SupplyRates[0], SupplyRates[1])=(SupplyRates[1], SupplyRates[0]);
			}
		return;
		}
	else
		{
		LoadScenarioFile3();

		}

	CurrentMap=3;

	// マップの作成

	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cnct_sinario_101")]
public void	SetUpScenario101()
	{
	int m, n;

	// ガダルカナル島を巡る戦い
	if(Mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ
		if(HostSide==0)
			{
			m=0;
			n=1;
			}
		else
			{
			m=1;
			n=0;
			}

		// 日本海軍側
		SupplyRates[m]=5;
		InitialSupplyPoints[m]=2500;

		//合衆国海軍側
		SupplyRates[n]=12;
		InitialSupplyPoints[n]=850;

		SwapRule=0;
		SwapTime=0;

		ArrivalControl=0;

		IsDecisionEnabled=true;
		return;
		}

	//  ゲーム設定

	//===============		 日本海軍		================
	// クエゼリン環礁
	var x=-7120.0;
	var y=6640.0;
	AddUnit(Side.Japan,UnitKind.NavalBase,x,y,0);

	// クエゼリン航空基地
	x=-7120-80;
	y=6640+80;
	var carrier=AddUnit(Side.Japan,UnitKind.AirBase,x,y,0);
	AddPlane(Side.Japan,UnitKind.Fighter,1,carrier,2,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,5,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Bomber,0,carrier,3,FireKind.Unarmed);

	// ラバウル 空港
	x=-7520;
	y=1840;
	carrier=AddUnit(Side.Japan,UnitKind.AirBase,x,y,0);
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,3,FireKind.Bomb);
	AddPlane(Side.Japan,UnitKind.Bomber,0,carrier,1,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Fighter,1,carrier,2,FireKind.Unarmed);

	// ガダルカナル
	x=-880;
	y=-240;
	AddUnit(Side.Japan,UnitKind.InfantryBase,x,y,0);
	x=-880+80;
	y=-240;
	AddUnit(Side.Japan,UnitKind.AirBase,x,y,0);

	// 機動部隊
	x=-6500;
	y=4500;
	m=AddUnit(Side.Japan,UnitKind.Carrier,x,y,0);
	carrier=m;
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,3,FireKind.Torpedo);
	AddPlane(Side.Japan,UnitKind.Fighter,0,carrier,3,FireKind.Unarmed);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Battleship,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Battleship,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,0);

	//===============		 合衆国海軍		================
	/*ヌーメア軍港*/
	x=5440;
	y=-5800+40;
	AddUnit(Side.UnitedStates,UnitKind.NavalBase,x,y,0);

	/*ヌーメア航空基地*/
	x=5280;
	y=-5760;
	m=AddUnit(Side.UnitedStates,UnitKind.AirBase,x,y,0);
	carrier=m;
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,4,FireKind.Unarmed);
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,8,FireKind.Unarmed);

	/*ヌーメア要塞*/
	x=5360;
	y=-5760;
	AddUnit(Side.UnitedStates,UnitKind.Pillboxes,x,y,0);

	// 機動部隊
	x=6500;
	y=-5000;
	m=AddUnit(Side.UnitedStates,UnitKind.Carrier,x,y,180);
	carrier=m;
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,9,FireKind.Bomb);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,3,FireKind.Unarmed);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,180);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,180);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,180);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Battleship,x,y,180);

	// マップの作成
	LoadScenarioFile2("Map\\South_pacific.dat");

	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cnct_sinario_102")]
public void	SetUpScenario102()
	{
	int m, n;

	// ガダルカナル島を巡る戦い
	if(Mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ
		if(HostSide==0)
			{
			m=0;
			n=1;
			}
		else
			{
			m=1;
			n=0;
			}

		// 日本海軍側
		SupplyRates[m]=5;
		InitialSupplyPoints[m]=2500;

		//合衆国海軍側
		SupplyRates[n]=12;
		InitialSupplyPoints[n]=850;

		SwapRule=0;
		SwapTime=0;

		ArrivalControl=0;

		IsDecisionEnabled=true;

		return;
		}

	//  ゲーム設定

	//===============		 日本海軍		================
	/*クエゼリン環礁*/
	var x=-7120.0;
	var y=6640.0;
	AddUnit(Side.Japan,UnitKind.NavalBase,x,y,0);

	// クエゼリン航空基地
	x=-7120-80;
	y=6640+80;
	var carrier=AddUnit(Side.Japan,UnitKind.AirBase,x,y,0);
	AddPlane(Side.Japan,UnitKind.Fighter,1,carrier,2,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,5,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Bomber,0,carrier,3,FireKind.Unarmed);

	// ラバウル 空港
	x=-7520;
	y=1840;
	carrier=AddUnit(Side.Japan,UnitKind.AirBase,x,y,0);
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,2,FireKind.Bomb);
	AddPlane(Side.Japan,UnitKind.Fighter,0,carrier,5,FireKind.Unarmed);

	// ガダルカナル
	x=-880;
	y=-240;
	AddUnit(Side.Japan,UnitKind.InfantryBase,x,y,0);
	x=-880+80;
	y=-240;
	AddUnit(Side.Japan,UnitKind.AirBase,x,y,0);
	x=-880-80;
	y=-240+80;
	AddUnit(Side.Japan,UnitKind.Pillboxes,x,y,0);

	// 機動部隊
	x=-6500;
	y=5000;
	m=AddUnit(Side.Japan,UnitKind.Carrier,x,y,0);
	carrier=m;
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,3,FireKind.Torpedo);
	AddPlane(Side.Japan,UnitKind.Fighter,0,carrier,3,FireKind.Unarmed);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Battleship,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Submarine,x,y,0);

	//===============		 合衆国海軍		================
	/*ヌーメア軍港*/
	x=5440;
	y=-5800+40;
	AddUnit(Side.UnitedStates,UnitKind.NavalBase,x,y,0);

	/*ヌーメア航空基地*/
	x=5280;
	y=-5760;
	m=AddUnit(Side.UnitedStates,UnitKind.AirBase,x,y,0);
	carrier=m;
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,4,FireKind.Unarmed);
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,8,FireKind.Unarmed);

	/*ヌーメア要塞*/
	x=5360;
	y=-5760;
	AddUnit(Side.UnitedStates,UnitKind.Pillboxes,x,y,0);

	// 機動部隊
	x=6500;
	y=-5000;
	m=AddUnit(Side.UnitedStates,UnitKind.Carrier,x,y,180);
	carrier=m;
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,9,FireKind.Bomb);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,3,FireKind.Unarmed);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,180);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,180);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,180);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Battleship,x,y,180);

	// マップの作成
	LoadScenarioFile2("Map\\South_pacific.dat");

	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cnct_sinario_103")]
public void	SetUpScenario103()
	{
	int m, n;

	// 日本近海の戦い
	if(Mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ
		if(HostSide==0)
			{
			m=0;
			n=1;
			}
		else
			{
			m=1;
			n=0;
			}

		// 日本海軍側
		SupplyRates[m]=5;
		InitialSupplyPoints[m]=3000;

		//合衆国海軍側
		SupplyRates[n]=20;
		InitialSupplyPoints[n]=850;

		SwapRule=0;
		SwapTime=0;

		ArrivalControl=0;

		IsDecisionEnabled=true;

		return;
		}

	//  ゲーム設定

	//===============		 日本海軍		================
	// 東京
	// 空港
	var x=-7440.0;
	var y=5440.0;
	m=AddUnit(Side.Japan,UnitKind.AirBase,x,y,0);
	var carrier=m;
	AddPlane(Side.Japan,UnitKind.Fighter,0,carrier,2,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,2,FireKind.Unarmed);
	// 都市
	x=-7440-80*4;
	y=5440-80*2;
	AddUnit(Side.Japan,UnitKind.City,x,y,0);
/****
	// 都市
	rx=-7440-80*3;
	ry=5440-80*4;
	m=set_new_unit(JPN,CT1,rx,ry,0);
****/

	// 都市
	x=-7440-80*2;
	y=5440-80*5;
	AddUnit(Side.Japan,UnitKind.City,x,y,0);
/***
	// 都市
	rx=-7440-80*3;
	ry=5440-80*3;
	m=set_new_unit(JPN,CT1,rx,ry,0);
***/
	// 都市
	x=-7440-80*3;
	y=5440-80*1;
	AddUnit(Side.Japan,UnitKind.City,x,y,0);

	// 基地
	x=-7440-80*4;
	y=5440-80*3;
	AddUnit(Side.Japan,UnitKind.Pillboxes,x,y,0);

	// 横須賀
	// 港
	x=-7440;
	y=4880;
	AddUnit(Side.Japan,UnitKind.NavalBase,x,y,0);

	// 大阪
	// 空港
	x=-8000;
	y=2800;
	m=AddUnit(Side.Japan,UnitKind.AirBase,x,y,0);
	carrier=m;
	AddPlane(Side.Japan,UnitKind.Fighter,1,carrier,4,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Fighter,0,carrier,2,FireKind.Unarmed);

	// 都市
	x=-8000-80*2;
	y=2800-80*2;
	AddUnit(Side.Japan,UnitKind.City,x,y,0);

/****
	// 都市
	rx=-8000-80*3;
	ry=2800;
	m=set_new_unit(JPN,CT1,rx,ry,0);
	// 都市
	rx=-8000-80*2;
	ry=2800-80*4;
	m=set_new_unit(JPN,CT1,rx,ry,0);

	// 基地
	rx=-8000-80*1;
	ry=2800-80*2;
	m=set_new_unit(JPN,GF2,rx,ry,0);
****/

	// 呉
	x=-8160;
	y=1760-240;
	AddUnit(Side.Japan,UnitKind.NavalBase,x,y,0);

	// 九州の空港
	x=-7600;
	y=960;
	m=AddUnit(Side.Japan,UnitKind.AirBase,x,y,0);
	carrier=m;
	AddPlane(Side.Japan,UnitKind.Fighter,0,carrier,4,FireKind.Unarmed);

	// 都市
	x=-7600-80*9;
	y=960-80*4;
	AddUnit(Side.Japan,UnitKind.City,x,y,0);
/***
	// 都市
	rx=-7600-80*9;
	ry=960-80*6;
	m=set_new_unit(JPN,CT1,rx,ry,0);
***/

	// 硫黄島
	// 基地 (-1440, 4480)
	// 空港
	x=-1520;
	y=4400;
	m=AddUnit(Side.Japan,UnitKind.AirBase,x,y,0);
	carrier=m;
	AddPlane(Side.Japan,UnitKind.Fighter,0,carrier,5,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,2,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Fighter,1,carrier,2,FireKind.Unarmed);

	//沖縄の日本軍基地
	x=-5040;
	y=-2800;
	AddUnit(Side.Japan,UnitKind.AirBase,x,y,0);

	//　台湾
	x=-5040+80*2;
	y=-7200+80*4;
	AddUnit(Side.Japan,UnitKind.Pillboxes,x,y,0);

	x=-3500;
	y=1500;
	m=AddUnit(Side.Japan,UnitKind.Carrier,x,y,0);
	carrier=m;
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,6,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Fighter,0,carrier,2,FireKind.Unarmed);
	x-=150;
	m=AddUnit(Side.Japan,UnitKind.Carrier,x,y,0);
	carrier=m;
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,4,FireKind.Bomb);
	x-=150;
	AddUnit(Side.Japan,UnitKind.Battleship,x,y,0);
	x-=150;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,0);
	x-=150;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,0);
	x-=150;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,0);
	x-=150;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,0);
	x-=150;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,0);

	//===============		 合衆国海軍		================
/**
	// サイパン港
	rx=5200+80;
	ry=2240-160;
	m=set_new_unit(USA,SP,rx,ry,0);

	// 空港
	rx=5200;
	ry=2240;
	no=set_new_unit(USA,AP,rx,ry,0);
	m=set_new_unit_plane(USA,FT1,0,no,10,BOM);
**/

	// パラオ
	x=6720-80;
	y=-3760;
	AddUnit(Side.UnitedStates,UnitKind.NavalBase,x,y,0);
	AddUnit(Side.UnitedStates,UnitKind.Pillboxes,x-80,y+80,0);

	// 機動部隊
	x=8000;
	y=-6500;
	m=AddUnit(Side.UnitedStates,UnitKind.Carrier,x,y,180);
	carrier=m;
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,8,FireKind.Bomb);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,4,FireKind.Unarmed);

	x-=150;
	m=AddUnit(Side.UnitedStates,UnitKind.LightCarrier,x,y,135);
	carrier=m;
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,6,FireKind.Bomb);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,2,FireKind.Unarmed);

	x-=150;
	AddUnit(Side.UnitedStates,UnitKind.Battleship,x,y,180);
	x-=150;
	AddUnit(Side.UnitedStates,UnitKind.Battleship,x,y,180);
	x-=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,180);
	x-=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,180);
	x-=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,180);
	x-=150;
	AddUnit(Side.UnitedStates,UnitKind.Destroyer,x,y,180);
	x-=150;
	AddUnit(Side.UnitedStates,UnitKind.Destroyer,x,y,180);

	// マップの作成
	LoadScenarioFile2("Map\\Japan_off.dat");

	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cnct_sinario_104")]
public void	SetUpScenario104()
	{
	int m, n;

	//南太平洋の戦い１
	if(Mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ
		if(HostSide==0)
			{
			m=0;
			n=1;
			}
		else
			{
			m=1;
			n=0;
			}

		// 日本海軍側
		SupplyRates[m]=8;
		InitialSupplyPoints[m]=2500;

		//合衆国海軍側
		SupplyRates[n]=15;
		InitialSupplyPoints[n]=500;

		SwapRule=0;
		SwapTime=0;

		ArrivalControl=0;

		IsDecisionEnabled=true;

		return;
		}

	//  ゲーム設定

	//===============		 日本海軍		================
	/*クエゼリン環礁*/
	var x=-7120.0;
	var y=6640.0;
	AddUnit(Side.Japan,UnitKind.NavalBase,x,y,0);

	// クエゼリン航空基地
	x=-7120-80;
	y=6640+80;
	var carrier=AddUnit(Side.Japan,UnitKind.AirBase,x,y,0);
	AddPlane(Side.Japan,UnitKind.Fighter,1,carrier,2,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,5,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Bomber,0,carrier,3,FireKind.Unarmed);

	// ブーゲンビル
	x=-4480;
	y=1200;
	AddUnit(Side.Japan,UnitKind.InfantryBase,x,y,0);
	x=-4480+80*2;
	y=1200-80*2;
	AddUnit(Side.Japan,UnitKind.InfantryBase,x,y,0);
	x=-4480+80*5;
	y=1200-80*3;
	AddUnit(Side.Japan,UnitKind.InfantryBase,x,y,0);

	// ガダルカナル
	x=-880;
	y=-240;
	AddUnit(Side.Japan,UnitKind.InfantryBase,x,y,0);
	x=-880+80;
	y=-240;
	AddUnit(Side.Japan,UnitKind.AirBase,x,y,0);
	x=-880-80;
	y=-240+80;
	AddUnit(Side.Japan,UnitKind.InfantryBase,x,y,0);

	// ラバウル 空港
	x=-7520;
	y=1840;
	carrier=AddUnit(Side.Japan,UnitKind.AirBase,x,y,0);
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,3,FireKind.Bomb);
	AddPlane(Side.Japan,UnitKind.Bomber,0,carrier,1,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Fighter,1,carrier,2,FireKind.Unarmed);

	x=-6500;
	y=5000;
	m=AddUnit(Side.Japan,UnitKind.Carrier,x,y,0);
	carrier=m;
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,9,FireKind.Torpedo);
	AddPlane(Side.Japan,UnitKind.Fighter,0,carrier,3,FireKind.Unarmed);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Battleship,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,0);

	//===============		 合衆国海軍		================
	/*ヌーメア軍港*/
	x=5440;
	y=-5800+40;
	AddUnit(Side.UnitedStates,UnitKind.NavalBase,x,y,0);

	/*ヌーメア航空基地*/
	x=5280;
	y=-5760;
	m=AddUnit(Side.UnitedStates,UnitKind.AirBase,x,y,0);
	carrier=m;
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,5,FireKind.Unarmed);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,1,carrier,3,FireKind.Unarmed);
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,5,FireKind.Unarmed);

	/*ヌーメア要塞*/
	x=5360;
	y=-5760;
	AddUnit(Side.UnitedStates,UnitKind.InfantryBase,x,y,0);

	x=6500;
	y=-5000;

	m=AddUnit(Side.UnitedStates,UnitKind.Carrier,x,y,180);
	carrier=m;
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,9,FireKind.Bomb);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,3,FireKind.Unarmed);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Destroyer,x,y,180);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,180);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,180);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Battleship,x,y,180);

	// マップの作成
	LoadScenarioFile2("Map\\South_pacific.dat");

	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cnct_sinario_105")]
public void	SetUpScenario105()
	{
	int m, n;

	//南太平洋の戦い２
	if(Mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ
		if(HostSide==0)
			{
			m=0;
			n=1;
			}
		else
			{
			m=1;
			n=0;
			}

		// 日本海軍側
		SupplyRates[m]=8;
		InitialSupplyPoints[m]=2750;

		//合衆国海軍側
		SupplyRates[n]=18;
		InitialSupplyPoints[n]=400;

		SwapRule=0;
		SwapTime=0;

		ArrivalControl=0;

		IsDecisionEnabled=true;

		return;
		}

	//  ゲーム設定

	//===============		 日本海軍		================
	/*クエゼリン環礁*/
	var x=-7120.0;
	var y=6640.0;
	AddUnit(Side.Japan,UnitKind.NavalBase,x,y,0);

	// クエゼリン航空基地
	x=-7120-80;
	y=6640+80;
	var carrier=AddUnit(Side.Japan,UnitKind.AirBase,x,y,0);
	AddPlane(Side.Japan,UnitKind.Fighter,1,carrier,2,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,5,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Bomber,0,carrier,3,FireKind.Unarmed);

	// ブーゲンビル
	x=-4480;
	y=1200;
	AddUnit(Side.Japan,UnitKind.InfantryBase,x,y,0);
	x=-4480+80*2;
	y=1200-80*2;
	AddUnit(Side.Japan,UnitKind.InfantryBase,x,y,0);
	x=-4480+80*5;
	y=1200-80*3;
	AddUnit(Side.Japan,UnitKind.InfantryBase,x,y,0);

	// ラバウル 空港
	x=-7520;
	y=1840;
	carrier=AddUnit(Side.Japan,UnitKind.AirBase,x,y,0);
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,3,FireKind.Bomb);
	AddPlane(Side.Japan,UnitKind.Bomber,0,carrier,1,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Fighter,1,carrier,2,FireKind.Unarmed);

	x=-6500;
	y=5000;
	m=AddUnit(Side.Japan,UnitKind.Carrier,x,y,0);
	carrier=m;
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,9,FireKind.Torpedo);
	AddPlane(Side.Japan,UnitKind.Fighter,0,carrier,3,FireKind.Unarmed);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Battleship,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,0);

	//===============		 合衆国海軍		================
	/*ヌーメア軍港*/
	x=5440;
	y=-5800+40;
	AddUnit(Side.UnitedStates,UnitKind.NavalBase,x,y,0);

	/*ヌーメア航空基地*/
	x=5280;
	y=-5760;
	m=AddUnit(Side.UnitedStates,UnitKind.AirBase,x,y,0);
	carrier=m;
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,5,FireKind.Unarmed);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,1,carrier,3,FireKind.Unarmed);
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,5,FireKind.Unarmed);

	/*ヌーメア要塞*/
	x=5360;
	y=-5760;
	AddUnit(Side.UnitedStates,UnitKind.InfantryBase,x,y,0);

	// ガダルカナル
	x=-880;
	y=-240;
	AddUnit(Side.UnitedStates,UnitKind.InfantryBase,x,y,0);
	x=-880+80;
	y=-240;
	AddUnit(Side.UnitedStates,UnitKind.AirBase,x,y,0);

	x=6500;
	y=-5000;

	m=AddUnit(Side.UnitedStates,UnitKind.Carrier,x,y,180);
	carrier=m;
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,9,FireKind.Bomb);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,3,FireKind.Unarmed);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Destroyer,x,y,180);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,180);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,180);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Battleship,x,y,180);

	// マップの作成
	LoadScenarioFile2("Map\\South_pacific.dat");

	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cnct_sinario_106")]
public void	SetUpScenario106()
	{
	int m, n;

	// ガ島争奪戦
	if(Mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ
		if(HostSide==0)
			{
			m=0;
			n=1;
			}
		else
			{
			m=1;
			n=0;
			}

		// 日本海軍側
		SupplyRates[m]=8;
		InitialSupplyPoints[m]=180;

		//合衆国海軍側
		SupplyRates[n]=8;
		InitialSupplyPoints[n]=180;

		SwapRule=0;
		SwapTime=0;

		ArrivalControl=0;

		IsDecisionEnabled=true;

		return;
		}

	//  ゲーム設定

	//===============		 日本海軍		================
	/*クエゼリン環礁*/
	var x=-7120.0;
	var y=6640.0;
	AddUnit(Side.Japan,UnitKind.NavalBase,x,y,0);

	// クエゼリン航空基地
	x=-7120-80;
	y=6640+80;
	var carrier=AddUnit(Side.Japan,UnitKind.AirBase,x,y,0);
	AddPlane(Side.Japan,UnitKind.Fighter,0,carrier,2,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,3,FireKind.Unarmed);

	x=-7000;
	y=5000;
	m=AddUnit(Side.Japan,UnitKind.LightCarrier,x,y,0);
	carrier=m;
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,4,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Fighter,0,carrier,3,FireKind.Unarmed);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Cruiser,x,y,0);
	x-=120;
	AddUnit(Side.Japan,UnitKind.Destroyer,x,y,0);
	x-=120;
	m=AddUnit(Side.Japan,UnitKind.Transport,x,y,0);
	Units[m].Weapon=FireKind.CargoInfantryBase;		// 武装品種
	Units[m].Ammo=1;			// 数
	Units[m].MaxAmmo=1;			// 数 全容量

	// ガダルカナル
	x=-880-160;
	y=-240+80;
	AddUnit(Side.Japan,UnitKind.InfantryBase,x,y,0);
	x=-880+80-160;
	y=-240+80;
	AddUnit(Side.Japan,UnitKind.InfantryBase,x,y,0);
	x=-880-160+80;
	y=-240;
	AddUnit(Side.Japan,UnitKind.InfantryBase,x,y,0);

/**
	// 輸送船団
	rx=-880-800;
	ry=-240;
	m=set_new_unit(JPN,TR1,rx,ry,135);
	unit[m].arm[0]=TR_GF2;		// 武装品種
	unit[m].arm[1]=1;			// 数
	unit[m].arm[4]=1;			// 数 全容量
**/

	//===============		 合衆国海軍		================
	/*ヌーメア軍港*/
	x=5440;
	y=-5800+40;
	AddUnit(Side.UnitedStates,UnitKind.NavalBase,x,y,0);

	/*ヌーメア航空基地*/
	x=5280;
	y=-5760;
	m=AddUnit(Side.UnitedStates,UnitKind.AirBase,x,y,0);
	carrier=m;
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,2,FireKind.Unarmed);
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,3,FireKind.Unarmed);

	x=6500;
	y=-5000;
	m=AddUnit(Side.UnitedStates,UnitKind.LightCarrier,x,y,180);
	carrier=m;
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,4,FireKind.Unarmed);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,3,FireKind.Unarmed);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Destroyer,x,y,180);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Cruiser,x,y,180);
	x+=150;
	AddUnit(Side.UnitedStates,UnitKind.Destroyer,x,y,180);
	x+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Transport,x,y,180);
	Units[m].Weapon=FireKind.CargoInfantryBase;		// 武装品種
	Units[m].Ammo=1;			// 数
	Units[m].MaxAmmo=1;			// 数 全容量

	// ガダルカナル
	x=-880;
	y=-240;
	AddUnit(Side.UnitedStates,UnitKind.InfantryBase,x,y,0);
	x=-880+80;
	y=-240;
	AddUnit(Side.UnitedStates,UnitKind.AirBase,x,y,0);
	x=-880+80;
	y=-240+80;
	AddUnit(Side.UnitedStates,UnitKind.InfantryBase,x,y,0);
	x=-880;
	y=-240+80;
	AddUnit(Side.UnitedStates,UnitKind.InfantryBase,x,y,0);

	// マップの作成
	LoadScenarioFile2("Map\\South_pacific.dat");

	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cnct_sinario_999")]
public void	SetUpScenario999()
	{
	int m, n;

	if(Mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ
		if(HostSide==0)
			{
			m=0;
			n=1;
			}
		else
			{
			m=1;
			n=0;
			}

		// 日本海軍側
		SupplyRates[m]=5;
		InitialSupplyPoints[m]=4000;

		//合衆国海軍側
		SupplyRates[n]=10;
		InitialSupplyPoints[n]=4000;

		SwapRule=0;
		SwapTime=0;

		ArrivalControl=0;

		return;
		}

	//  ゲーム設定
	//===============		 日本海軍		================
	// トラック島 港
	var x=-7600.0;
	var y=-4720.0;
	AddUnit(Side.Japan,UnitKind.NavalBase,x,y,0);

	// 空港
	x=-7600-80;
	y=-4720;
	m=AddUnit(Side.Japan,UnitKind.AirBase,x,y,135);
	var carrier=m;
	AddPlane(Side.Japan,UnitKind.Attacker,0,carrier,4,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Fighter,0,carrier,4,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Fighter,1,carrier,1,FireKind.Unarmed);
	AddPlane(Side.Japan,UnitKind.Bomber,0,carrier,1,FireKind.Unarmed);

	x=0;
	y=0;
	AddUnit2(Side.Japan,UnitKind.Battleship,1,x,y,90);
	y+=80;
	AddUnit2(Side.UnitedStates,UnitKind.Submarine,0,x,y,90);

	//===============		 合衆国海軍		================
	// ハワイ港
	x=7200;
	y=-720;
	AddUnit(Side.UnitedStates,UnitKind.NavalBase,x,y,90.0);

	// ハワイ空港
	x=7200;
	y=-560;
	m=AddUnit(Side.UnitedStates,UnitKind.AirBase,x,y,45.0);
	carrier=m;
	AddPlane(Side.UnitedStates,UnitKind.Attacker,0,carrier,1,FireKind.Unarmed);
	AddPlane(Side.UnitedStates,UnitKind.Fighter,0,carrier,3,FireKind.Unarmed);
	AddPlane(Side.UnitedStates,UnitKind.Bomber,0,carrier,4,FireKind.Unarmed);

	x=700;
	y=0;
	AddUnit2(Side.UnitedStates,UnitKind.Battleship,0,x,y,90);
	y+=80;
	AddUnit2(Side.Japan,UnitKind.Submarine,0,x,y,90);

	// マップの作成
	LoadScenarioFile2("Map\\Middle_pacific.dat");

	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("get_sinario_data")]
public void LoadScenarioData()
	{
	switch( ScenarioNumber )
		{
		case 1:			SetUpScenario1();
						break;
		case 2:			SetUpScenario2();
						break;
		case 3:			SetUpScenario3();
						break;
		case 4:			SetUpScenario4();
						break;
		case 5:			SetUpScenario5();
						break;
		case 6:			SetUpScenario6();
						break;
		case 7:			SetUpScenario7();
						break;
		case 8:			SetUpScenario8();
						break;
		case 9:			SetUpScenario9();
						break;

		case 101:			SetUpScenario101();
						break;
		case 102:			SetUpScenario102();
						break;
		case 103:			SetUpScenario103();
						break;
		case 104:			SetUpScenario104();
						break;
		case 105:			SetUpScenario105();
						break;
		case 106:			SetUpScenario106();
						break;

		case 999:			SetUpScenario999();
						break;
		}
	}

private void ClearClouds()
	{
	for(var n=0; n<KUMO_MAX; n++)
		{
		ref var cloud = ref Clouds[n];
		cloud.Used=0;
		cloud.Position = new WorldPosition(0, 0);
		cloud.Kind=0;
		}
	}

private void ClearMapTiles()
	{
	int n;
	for(var m=0; m<256; m++)
		for(n=0; n<256; n++)
			{
			MapTiles[m][n]=0;
			}

	CombatMenuKind=0;
	CombatMenuSelection=CombatMenuItem.None;
	}

private void ClearUnits()
	{
	for(var m=0; m<=255; m++)
		{
		ref var unit = ref Units[m];
		unit.Side=0;
		unit.Position = new WorldPosition(0, 0);
		unit.Category=UnitCategory.None;
		unit.Kind=0;
		for(var n=0;n<=15;n++)
			unit.info[n]=0;

		unit.SpriteRow=0;
		unit.SpriteColumn=0;
		unit.Direction=0;
		unit.TurnRate=0;
		unit.Speed=0;
		unit.Acceleration=0;
		unit.MaxSpeed=0;
		unit.MinSpeed=0;
		unit.AccelerationChange=0;
		unit.Speed=0;
		unit.IsStopping=false;
		unit.SupplyTime=0;
		unit.EmergencyFlags[0]=unit.EmergencyFlags[1]=0;
		unit.EmergencyDestination=new WorldPosition(0, 0);

		unit.IsGroupLeader=0;
		unit.GroupLeader=0;
		unit.FormationNumber=0;
		unit.ForGroupLeader=0;

		unit.FormationSpeed=0;

		for(var n=0;n<=7;n++)
			{
			unit.hp[n]=0;
			unit.arm[n]=0;
			unit.gas[n]=0;
			}
		Selections[0][m]=0;
		Selections[1][m]=0;
		unit.Skill=0;
		}

	WorkPathX[0]=World.Beyond;

	SelectedUnit=0;
	PreviousSelectedUnit=0;
	SelectionCount=0;

	MaxUnitId=(short)USA_PLANE_END;			// とりあえず最大値を入れておく

	ClearFlag=2;
	}

private void ClearFires()
	{
	for(var m=0; m<FIRE_MAX; m++)
		{
		ref var fire = ref Fires[m];
		fire.Target=0;
		for(var n=0;n<=7;n++)
			fire.info[n]=0;
		}
	MaxFireId=0;
	}

private void ClearEffects()
	{
	for(var m=0; m<EFFECT_MAX; m++)
		{
		Effects[m].Layer=EffectLayer.None;
		}
	}

private void StartFromSave(ref SyncFlagMessage flagMessage)
	{
	int f=ScenarioNumber;
	switch( f )
		{
		case -1:
			LoadResume(1);		// とりあえずマップだけロードする為にシナリオ読みこむ
			break;
		case -2:
			LoadResume(3);		// とりあえずマップだけロードする為にシナリオ読みこむ
			break;
		}
	LoadScenarioData();		// シナリオナンバーからマップだけロードしてくれればいい。

	WorkPathX[0]=World.Beyond;
	SelectedUnit=0;
	PreviousSelectedUnit=0;
	SelectionCount=0;
	MaxUnitId=(short)USA_PLANE_END;			// とりあえず最大値を入れておく
	ClearFlag=2;

	//
	CombatMenuKind=0;
	CombatMenuSelection=CombatMenuItem.None;

	// 雲のクリア
	ClearClouds();

	//最初の雲
	InitializeClouds();

	Renderer.MakeTerrainSurface();

	ShowsAntiAir=0;		// 0が正常
	RevealsAll=0;		// 0が正常

	GameSpeed=1;
	Tick=0;
	Renderer.FrameCount=0;
	Result=GameResult.None;

	TickChecksums[0]=0;
	TickChecksums[1]=0;

	RandomChecksums[0]=0;
	RandomChecksums[1]=0;

	RandomCount=0;

	CameraPosition = new WorldPosition(-400, 400);

	flagMessage.Type = MessageType.SyncFlag;
	flagMessage.UnitChecksum=0;
	UnitChecksums[1]=0;
	flagMessage.TickChecksum=(byte)Tick;
	TickChecksums[1]=(byte)Tick;
	flagMessage.RandomChecksum=(byte)RandomCount;
	RandomChecksums[1]=(byte)RandomCount;
	bufferDesc.dwBufferSize = (uint)(sizeof(SyncFlagMessage));
	bufferDesc.pBufferData  = (byte*) (SyncFlagMessage*)Unsafe.AsPointer(ref flagMessage);
	DirectPlayPeer.SendTo( RivalPlayerId, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

	BufferedMoveOrders[0].Unit=0;						// クリア
	BufferedMoveOrders[1].Unit=0;						// クリア

	BufferedSelectOrders[0].IsSet=false;						// クリア
	BufferedSelectOrders[1].IsSet=false;						// クリア

	BufferedMenuOrders[0].Menu=CombatMenuItem.None;
	BufferedMenuOrders[1].Menu=CombatMenuItem.None;

	BufferedArrivedUnits[0]=0;
	BufferedArrivedUnits[1]=0;

	BufferedSystemOrders[0]=0;
	BufferedSystemOrders[1]=0;

	SystemOrders[0]=0;
	SystemOrders[1]=0;

	TurnLength=TurnCounter=30;
	SyncTick1=8;
	SyncTick2=19;

	TickWaits[0]=TickWaits[1]=0;

	AutoSaveTime=0;

	switch( f )
		{
		case -1:
			LoadResume(1);		// セーブデータをロードする
			break;
		case -2:
			LoadResume(3);		// セーブデータをロードする
			break;
		}

	for(var m=0; m<=255; m++)
		{
		Selections[0][m]=0;
		Selections[1][m]=0;
		}

#if SND_SW
	if( !IsEditingMap )
		{
		Sound.SoundBuffers[(int)SoundId.Sea][0].SetVolume( 0 );
		Sound.SoundBuffers[(int)SoundId.Sea][0].Play(0,0,DSBPLAY_LOOPING);	// ループする
		}
#endif
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cnct_game_init")]
public void	InitializeGame()
	{

	SyncFlagMessage	flagMessage = default;

	Rules = IsSingleplayer ? BattleRules.Nspw122 : BattleRules.NspwNet;

	// 乱数の初期化
	if( IsSingleplayer )
		{
		// 1.22 has no shared random numbers: its rand goes on from where it was, seeded from the time as the game started
		// (WinMain), or as a trace gives it.
		if( Trace?.Seeds?.Start is { } start )
			srand( start );
		Trace?.Reach(this, TracePoint.Start);
		}
	else
		{
		srand( (uint)( SharedRandomSeed ) );

		MakeSharedRandomTable();
		}

	ScrollSpeed=0;

	//
	for(var m=0;m<=9;m++)
		UnitInfoPanel[m]=0;

	if( ScenarioNumber < 0 )
		{
		StartFromSave(ref flagMessage);
		return;
		}	// リジュームスタート

	// 全サウンドのダブリカウントクリア
	for(var m=0; m<NUM_SOUND_EFFECTS; m++)
		{
		Sound.NextSoundBuffers[m]=0;
		}

	// 全エフェクトのクリア
	ClearEffects();

	// 全ファイアデータのクリア
	ClearFires();

	// 全ユニットデータのクリア
	ClearUnits();

	// マップのクリア マップは横120チップ、縦90チップ
	ClearMapTiles();

	// 雲のクリア
	ClearClouds();

	//最初の雲
	InitializeClouds();

	//
	for(var n=0;n<=3;n++)
		DecisionPoints[n]=0;

	if( IsSingleplayer )
		SetUpSingleplayerScenario();
	else
		LoadScenarioData();

	if( IsEditingMap )
		{
		EditorTarget=1;
		EditorKind=(byte)UnitKind.Battleship;
		}

	Renderer.MakeTerrainSurface();

	ShowsAntiAir=0;		// 0が正常
	RevealsAll=0;		// 0が正常

#if SND_SW
	if( !IsEditingMap )
		{
		Sound.SoundBuffers[(int)SoundId.Sea][0].SetVolume( 0 );
		Sound.SoundBuffers[(int)SoundId.Sea][0].Play(0,0,DSBPLAY_LOOPING);	// ループする
		}
#endif

	GameSpeed=1;
	Tick=0;
	Renderer.FrameCount=0;

	Result=GameResult.None;

	LastTime=timeGetTime();
	LastTime2=LastTime;

	if( IsSingleplayer )
		{
		// The scenario has set the sides, the camera and the time limit, and there is no rival.
		InitializeSingleplayerOrders();

		// 1.22's game_init runs within the frame of the setting screen that starts the battle (0x40C61E), which then
		// counts itself (0x434E9F): the battle's first frame is 1, and cpu_strtgy starts with fleet 2.
		Renderer.FrameCount=1;

		// 1.22's game_init seeds rand last, from the time (0x42A163).
		srand( Trace?.Seeds?.Battle ?? (uint)time() );
		Trace?.Reach(this, TracePoint.Seeded);
		return;
		}

		TickChecksums[0]=0;
		TickChecksums[1]=0;

		RandomChecksums[0]=0;
		RandomChecksums[1]=0;

		RandomCount=0;

		//  ゲーム設定
		if(HostSide==0)
			{
			if(IsHost)
				LocalSide=Side.Japan;
			else
				LocalSide=Side.UnitedStates;
			}
		else
			{
			if(IsHost)
				LocalSide=Side.UnitedStates;
			else
				LocalSide=Side.Japan;
			}

		CameraPosition = new WorldPosition(-400, 400);

		flagMessage.Type = MessageType.SyncFlag;
		flagMessage.UnitChecksum=0;
		UnitChecksums[1]=0;
		flagMessage.TickChecksum=(byte)Tick;
		TickChecksums[1]=(byte)Tick;
		flagMessage.RandomChecksum=(byte)RandomCount;
		RandomChecksums[1]=(byte)RandomCount;
		bufferDesc.dwBufferSize = (uint)(sizeof(SyncFlagMessage));
		bufferDesc.pBufferData  = (byte*) &flagMessage;
		DirectPlayPeer.SendTo( RivalPlayerId, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

		BufferedMoveOrders[0].Unit=0;						// クリア
		BufferedMoveOrders[1].Unit=0;						// クリア

		BufferedSelectOrders[0].IsSet=false;						// クリア
		BufferedSelectOrders[1].IsSet=false;						// クリア

		BufferedMenuOrders[0].Menu=CombatMenuItem.None;
		BufferedMenuOrders[1].Menu=CombatMenuItem.None;

		BufferedArrivedUnits[0]=0;
		BufferedArrivedUnits[1]=0;

		BufferedSystemOrders[0]=0;
		BufferedSystemOrders[1]=0;

		SystemOrders[0]=0;
		SystemOrders[1]=0;

		TurnLength=TurnCounter=20;
		SyncTick1=6;
		SyncTick2=13;

		TickWaits[0]=TickWaits[1]=0;

		AutoSaveTime=0;

		if(IsHost)
			SupplyPoints=InitialSupplyPoints[0];
		else
			SupplyPoints=InitialSupplyPoints[1];

		SupplyCount=0;
		SupplyTarget=0;

		BattleTime=0;

		if( !IsEditingMap)
			SaveResume(3);

	}
}

public unsafe partial class Renderer
{

internal void DrawResult(ref Array5<int> length, ref Array5<Array128<byte>> text)
	{
	HDC hdc;
	if ( IDirectDrawSurface_GetDC(lpDDSBack, &hdc) == DD_OK )
		{
		_game.SetBkMode(hdc, TRANSPARENT);
		_game.SelectObject(hdc, GameFont1);

#if !LNGG_VER
		_game.WriteResult(ref length, ref text);

#else

		len[0] = wsprintf(ach[0], "Battle is Over.");
		switch( game_end )
			{
			case GameResult.JapanWon:
				len[1] = wsprintf(ach[1], "Japan Navy got a victory.");
				len[2] = wsprintf(ach[2], "Japan Navy won.");
				break;
			case GameResult.UnitedStatesWon:
				len[1] = wsprintf(ach[1], "U.S.Navy got a victory.");
				len[2] = wsprintf(ach[2], "U.S.Navy won.");
				break;
			case GameResult.JapanLost:
				len[1] = wsprintf(ach[1], "Japan Navy lost a victory.");
				len[2] = wsprintf(ach[2], "");
				break;
			case GameResult.UnitedStatesLost:
				len[1] = wsprintf(ach[1], "U.S.Navy lost a victory.");
				len[2] = wsprintf(ach[2], "");
				break;
			case GameResult.Draw:
				len[1] = wsprintf(ach[1], "Both of Navies could not get a victory.");
				len[2] = wsprintf(ach[2], "Draw.");
				break;
			}

#endif

//			len[3] = wsprintf(ach[3], "ＥＳＣ：プログラム終了　Ｆ６：シナリオセッティング画面");

		for( var n=0; n<=2; n++)
			{
			_game.SetTextColor(hdc, RGB(255, 255, 255));
			_game.TextOut(hdc, 300, 300+(n*20), text[n], length[n]);
			}

		IDirectDrawSurface_ReleaseDC(lpDDSBack, hdc);
		}
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("make_map_cg")]
public void MakeTerrainSurface()
	{
	int n;
	RECT	destination,source;

	// マップデータから陸地をマップに描画します
	destination.left=Sprites[SpriteId.Minimap].SheetPosition.X;
	destination.top=Sprites[SpriteId.Minimap].SheetPosition.Y;

	source.left = Sprites[SpriteId.Minimap].SheetPosition.X+306;
	source.top = Sprites[SpriteId.Minimap].SheetPosition.Y;
	source.right = source.left+255;
	source.bottom = source.top+199;

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( OffscreenSurface, destination.left, destination.top,OffscreenSurface,&source,0))
		{
		RestoreSurfaces();
		}

	for(var m=0; m<=179; m++) // 縦の個数	マップの枠 縦１８０ドット
		for( n=0; n<=239; n++) // 横の個数		マップの枠 横２４０ドット
			{
			if( _game.MapTiles[m][n]!=0 )
				{

				// 陸地有り
				destination.left=Sprites[SpriteId.Minimap].SheetPosition.X+8+n-0;
				destination.top=Sprites[SpriteId.Minimap].SheetPosition.Y+8+m-0;

				source.left = Sprites[SpriteId.Minimap].SheetPosition.X+270;
				source.top = Sprites[SpriteId.Minimap].SheetPosition.Y+110;

				source.right = source.left+2;
				source.bottom = source.top+2;

				if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( OffscreenSurface, destination.left, destination.top,OffscreenSurface,&source,0))
					{
					RestoreSurfaces();
					}

				}
			}
	}
}
