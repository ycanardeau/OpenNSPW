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

// Port of cnct_game_cont.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{





//============================================================================
// リジューム、セーブします。
//----------------------------------------------------------------------------
[Original("save_on_resume")]
public void	SaveResume(int type)
	{
//	char	bf[20];
	HANDLE	hFile=default;
//	short	m,n,f,i;
	uint	dwActBytes;
//	unsigned short		szBuf[256][256];					// マップ




	switch( type )
		{
		case 1:
			hFile=CreateFile("Saved\\resume_1.dat", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
								null, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, null);
			break;
		case 2:
			hFile=CreateFile("Saved\\cnct_error.dat", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
								null, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, null);
			break;
		case 3:
			hFile=CreateFile("Saved\\auto_save.dat", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
								null, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, null);
			break;
		}

	if( hFile != INVALID_HANDLE_VALUE )
		{
		//unsigned short		cmbt_map[256][256];					// マップ

		SetFilePointer( hFile,0,0,FILE_BEGIN);		// 先頭へ
/***
		// データ を バッファへ
		for(m=0;m<=255;m++)
			for(n=0;n<=255;n++)
				szBuf[m][n]=cmbt_map[m][n];

		// 書き込み
		WriteFile(hFile, szBuf,sizeof(szBuf),&dwActBytes,NULL);
***/

		// 書き込み
		WriteFile(hFile, ref Units,(uint)(sizeof(Array256<Unit>)),&dwActBytes,null);
		WriteFile(hFile, ref Fires,(uint)(sizeof(Array512<Fire>)),&dwActBytes,null);
		WriteFile(hFile, ref Effects,(uint)(sizeof(Array1024<Effect>)),&dwActBytes,null);

		WriteFile(hFile, ref LocalSide,sizeof(short),&dwActBytes,null);
		WriteFile(hFile, ref BattleTime,sizeof(int),&dwActBytes,null);

		WriteFile(hFile, ref SupplyCount,sizeof(short),&dwActBytes,null);
		WriteFile(hFile, ref SupplyTarget,sizeof(short),&dwActBytes,null);

		WriteFile(hFile, ref ScenarioNumber,sizeof(short),&dwActBytes,null);
		WriteFile(hFile, ref SupplyPoints,sizeof(short),&dwActBytes,null);

		WriteFile(hFile, ref SupplyRates,(uint)(sizeof(Array2<short>)),&dwActBytes,null);
		WriteFile(hFile, ref IsDecisionEnabled,sizeof(byte),&dwActBytes,null);
		WriteFile(hFile, ref ArrivalControl,sizeof(byte),&dwActBytes,null);

		WriteFile(hFile, ref IsHost,sizeof(byte),&dwActBytes,null);
		WriteFile(hFile, ref UserScenarioFileName,(uint)(sizeof(Array260<byte>)),&dwActBytes,null);


		WriteFile(hFile, ref SwapTime,sizeof(short),&dwActBytes,null);
		WriteFile(hFile, ref SwapRule,sizeof(short),&dwActBytes,null);
//		WriteFile(hFile, &rvrs_time,sizeof(rvrs_time),&dwActBytes,NULL);
//		WriteFile(hFile, &rvrs_rule,sizeof(rvrs_rule),&dwActBytes,NULL);



		CloseHandle(hFile);
		}
	}






//============================================================================
// リジューム、ロードします
//----------------------------------------------------------------------------
[Original("load_on_resume")]
public void	LoadResume(int type)
	{
//	char	bf[20];
	HANDLE	hFile=default;
//	short	m,n,f,i;
//	unsigned short		szBuf[256][256];					// マップ




/*
	hFile=CreateFile("Saved\\resume_1.dat", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
								NULL, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
*/
	switch( type )
		{
		case 1:
			hFile=CreateFile("Saved\\resume_1.dat", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
								null, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, null);
			break;
		case 2:
			hFile=CreateFile("Saved\\cnct_error.dat", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
								null, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, null);
			break;
		case 3:
			hFile=CreateFile("Saved\\auto_save.dat", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
								null, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, null);
			break;
		}




	if( hFile != INVALID_HANDLE_VALUE )
		{
		uint	dwActBytes;

		// 読み込み
		SetFilePointer( hFile,0,0,FILE_BEGIN);		// 先頭へ

		ReadFile( hFile, ref Units, (uint)(sizeof(Array256<Unit>)), &dwActBytes, null );
		ReadFile( hFile, ref Fires, (uint)(sizeof(Array512<Fire>)), &dwActBytes, null );
		ReadFile( hFile, ref Effects, (uint)(sizeof(Array1024<Effect>)), &dwActBytes, null );


		ReadFile( hFile, ref LocalSide, sizeof(short), &dwActBytes, null );
		ReadFile( hFile, ref BattleTime, sizeof(int), &dwActBytes, null );

		ReadFile( hFile, ref SupplyCount, sizeof(short), &dwActBytes, null );
		ReadFile( hFile, ref SupplyTarget, sizeof(short), &dwActBytes, null );

		ReadFile( hFile, ref ScenarioNumber, sizeof(short), &dwActBytes, null );
		ReadFile( hFile, ref SupplyPoints, sizeof(short), &dwActBytes, null );


		ReadFile( hFile, ref SupplyRates, (uint)(sizeof(Array2<short>)), &dwActBytes, null );
		ReadFile( hFile, ref IsDecisionEnabled, sizeof(byte), &dwActBytes, null );
//		ReadFile( hFile, first_spry_pt, sizeof(first_spry_pt), &dwActBytes, NULL );
		ReadFile( hFile, ref ArrivalControl, sizeof(byte), &dwActBytes, null );


		ReadFile( hFile, ref IsHost, sizeof(byte), &dwActBytes, null );
		ReadFile(hFile, ref UserScenarioFileName,(uint)(sizeof(Array260<byte>)),&dwActBytes,null);




		ReadFile( hFile, ref SwapTime, sizeof(short), &dwActBytes, null );
		ReadFile(hFile, ref SwapRule,sizeof(short),&dwActBytes,null);
//		WriteFile(hFile, &rvrs_time,sizeof(rvrs_time),&dwActBytes,NULL);
//		WriteFile(hFile, rvrs_rule,sizeof(rvrs_rule),&dwActBytes,NULL);


		CloseHandle(hFile);
		}


	}








//============================================================================
// デシジョン
//----------------------------------------------------------------------------
[Original("cnct_decision")]
public void	CheckResult()
	{
	int		i,f,m;
	RECT	wrk_r;


	Array5<Array128<byte>> ach = default;
	int	n; Array5<int> len = default;
	HDC					hdc;



	// 結果途中判定
	if( Result==GameResult.None && IsDecisionEnabled!=0 )
		{
		switch( ScenarioNumber )
			{
			case 1:
			case 2:
			case 3:
				// 空母起動部隊の戦い
				f=0;
				for( i=1; i<=MaxUnitId; i++ )
					{
					ref var unit = ref Units[i];
					if( unit.Side==Side.Japan && (unit.Kind==UnitKind.Carrier || unit.Kind==UnitKind.LightCarrier) )
						f++;
					}
				m=0;
				for( i=1; i<=MaxUnitId; i++ )
					{
					ref var unit = ref Units[i];
					if( unit.Side==Side.Japan && (unit.Kind==UnitKind.InfantryBase || unit.Kind==UnitKind.Pillboxes || unit.Kind==UnitKind.Fortress ) )
						m++;
					}
				if( f<=0 || m<=0 )
					{	
					Result=GameResult.UnitedStatesWon;
					break;
					}





				f=0;
				for( i=1; i<=MaxUnitId; i++)
					{
					ref var unit = ref Units[i];
					if( unit.Side==Side.UnitedStates && (unit.Kind==UnitKind.Carrier || unit.Kind==UnitKind.LightCarrier) )
						f++;
					}
				m=0;
				for( i=1; i<=MaxUnitId; i++)
					{
					ref var unit = ref Units[i];
					if( unit.Side==Side.UnitedStates && (unit.Kind==UnitKind.InfantryBase || unit.Kind==UnitKind.Pillboxes || unit.Kind==UnitKind.Fortress ) )
						m++;
					}
				if( f<=0 || m<=0 )
					{	
					Result=GameResult.JapanWon;
					break;
					}
				break;


			case 4:
			case 5:
				// 艦隊決戦
				f=0;
				for( i=1; i<=MaxUnitId; i++)
					{
					ref var unit = ref Units[i];
					if( unit.Side==Side.Japan && (unit.Kind==UnitKind.Battleship || unit.Kind==UnitKind.Cruiser) )
						f++;
					}
				m=0;
				for( i=1; i<=MaxUnitId; i++ )
					{
					ref var unit = ref Units[i];
					if( unit.Side==Side.Japan && (unit.Kind==UnitKind.InfantryBase || unit.Kind==UnitKind.Pillboxes || unit.Kind==UnitKind.Fortress ) )
						m++;
					}
				if( f<=0 || m<=0 )
					{	
					Result=GameResult.UnitedStatesWon;
					break;
					}



				f=0;
				for( i=1; i<=MaxUnitId; i++)
					{
					ref var unit = ref Units[i];
					if( unit.Side==Side.UnitedStates && (unit.Kind==UnitKind.Battleship || unit.Kind==UnitKind.Cruiser) )
						f++;
					}
				m=0;
				for( i=1; i<=MaxUnitId; i++ )
					{
					ref var unit = ref Units[i];
					if( unit.Side==Side.UnitedStates && (unit.Kind==UnitKind.InfantryBase || unit.Kind==UnitKind.Pillboxes || unit.Kind==UnitKind.Fortress ) )
						m++;
					}
				if( f<=0 || m<=0 )
					{	
					Result=GameResult.JapanWon;
					break;
					}
				break;


			case 6:
				// ミッドウェイ島攻略
				m=0;
				for( i=1; i<=MaxUnitId; i++)
					{
					ref var unit = ref Units[i];
					if( unit.Side==Side.UnitedStates && (unit.Kind>=UnitKind.AirBase&&unit.Kind<=UnitKind.Fortress) )
						{
						// ミッドウェイ島
						// ptin debg
						wrk_r.top=(int)(3440)+(80*2);//(int)(3440)-(80*4);
						wrk_r.right=(int)(80)+(80*4);
						wrk_r.bottom=(int)(3440)-(80*4);//(int)(3440)+(80*2);
						wrk_r.left=(int)(80)-(80*2);
						if( PointInRect3(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)!=0)
							{
							m++;	// 生きてる基地で指定範囲内に要る
							}
						}
					}
				if( m==0 )
					{
					Result=GameResult.JapanWon;
					break;
					}

				break;



			case 7:
				// ミッドウェイ島攻略
				f=0;
				m=0;
				for( i=1; i<=MaxUnitId; i++)
					{
					ref var unit = ref Units[i];
					if( unit.Side==Side.UnitedStates && (unit.Kind>=UnitKind.AirBase&&unit.Kind<=UnitKind.Fortress) )
						{
						// ミッドウェイ島
						// ptin_dbg
						wrk_r.top=(int)(3440)+(80*2);//(int)(3440)-(80*4);
						wrk_r.right=(int)(80)+(80*4);
						wrk_r.bottom=(int)(3440)-(80*4);//(int)(3440)+(80*2);
						wrk_r.left=(int)(80)-(80*2);
						if( PointInRect3(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)!=0)
							{
							m++;	// 生きてる基地で指定範囲内に要る
							}
						}
					}
				for( i=1; i<=MaxUnitId; i++)
					{
					ref var unit = ref Units[i];
					if( unit.Side==Side.Japan && (unit.Kind==UnitKind.Fortress) && unit.info[0]==0 )
						{
						// ミッドウェイ島
						// ptin dbg
						wrk_r.top=(int)(3440)+(80*2);//(int)(3440)-(80*4);
						wrk_r.right=(int)(80)+(80*4);
						wrk_r.bottom=(int)(3440)-(80*4);//(int)(3440)+(80*2);
						wrk_r.left=(int)(80)-(80*2);
						if( PointInRect3(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)!=0)
							{
							f++;	// 生きてる基地で指定範囲内に要る
							}
						}
					}
				if( m==0 && f!=0)
					{
					Result=GameResult.JapanWon;
					break;
					}

				break;



			case 8:
				// 中部太平洋の戦い
				f=0;
				m=0;
				for( i=1; i<=MaxUnitId; i++)
					{
					ref var unit = ref Units[i];
					if( unit.Side==Side.UnitedStates && (unit.Kind>=UnitKind.AirBase&&unit.Kind<=UnitKind.Fortress) )
						{
						// ミッドウェイ島
						// ptin dbg
						wrk_r.top=(int)(3440)+(80*2);//(int)(3440)-(80*4);
						wrk_r.right=(int)(80)+(80*4);
						wrk_r.bottom=(int)(3440)-(80*4);//(int)(3440)+(80*2);
						wrk_r.left=(int)(80)-(80*2);
						if( PointInRect3(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)!=0)
							{
							m++;	// 生きてる基地で指定範囲内に要る
							}
						}
					}
				for( i=1; i<=MaxUnitId; i++)
					{
					ref var unit = ref Units[i];
					if( unit.Side==Side.Japan && (unit.Kind==UnitKind.Fortress) && unit.info[0]==0 )
						{
						// ミッドウェイ島
						// ptin_dbg
						wrk_r.top=(int)(3440)+(80*2);//(int)(3440)-(80*4);
						wrk_r.right=(int)(80)+(80*4);
						wrk_r.bottom=(int)(3440)-(80*4);//(int)(3440)+(80*2);
						wrk_r.left=(int)(80)-(80*2);
						if( PointInRect3(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)!=0)
							{
							f++;	// 生きてる基地で指定範囲内に要る
							}
						}
					}
				if( m==0 && f!=0)
					{
					Result=GameResult.JapanWon;
					break;
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
				for( i=1; i<=MaxUnitId; i++)
					{
					ref var unit = ref Units[i];
					if( unit.Side==Side.Japan && (unit.Kind>=UnitKind.AirBase&&unit.Kind<=UnitKind.Fortress) )
						{
						// ウェーク
						wrk_r.top=(int)(-720+80);
						wrk_r.right=(int)(-4080+80);
						wrk_r.bottom=(int)(-720-160);
						wrk_r.left=(int)(-4080-80);


						if( PointInRect2(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)!=0)
							{
							m++;	// 生きてる基地で指定範囲内に要る
							}
						}
					}
				for( i=1; i<=MaxUnitId; i++)
					{
					ref var unit = ref Units[i];
					if( unit.Side==Side.UnitedStates && (unit.Kind==UnitKind.Fortress) && unit.info[0]==0 )
						{
						// ウェーク
						wrk_r.top=(int)(-720+80);
						wrk_r.right=(int)(-4080+80);
						wrk_r.bottom=(int)(-720-160);
						wrk_r.left=(int)(-4080-80);

						if( PointInRect2(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)!=0)
							{
							f++;	// 生きてる基地で指定範囲内に要る
							}
						}
					}
				if( m==0 && f!=0)
					{
					Result=GameResult.UnitedStatesWon;
					break;
					}
				break;


			case 9:
				// ユーザーマップ
				break;

			case 101:
				f=0;
				// ガダルカナル島
				wrk_r.top=(int)(-160);
				wrk_r.right=(int)(-1040+(80*3));
				wrk_r.bottom=(int)(-160-80);
				wrk_r.left=(int)(-1040);
				for( i=1; i<=MaxUnitId; i++)
					{
					ref var unit = ref Units[i];
					if( unit.Side==Side.Japan && ( unit.Kind>=UnitKind.AirBase && unit.Kind<=UnitKind.Fortress ) && PointInRect2(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)!=0 )
						{
						f++;
						}
					}

				if( f==0 )
					{
					Result=GameResult.UnitedStatesWon;
					break;
					}

				break;




			case 102:
				f=0;
				m=0;
				// ガダルカナル島
				wrk_r.top=(int)(-160);
				wrk_r.right=(int)(-1040+(80*3));
				wrk_r.bottom=(int)(-160-80);
				wrk_r.left=(int)(-1040);
				for( i=1; i<=MaxUnitId; i++)
					{
					ref var unit = ref Units[i];
					if( unit.Side==Side.Japan && ( unit.Kind>=UnitKind.AirBase && unit.Kind<=UnitKind.Fortress ) && PointInRect2(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)!=0 )
						{
						f++;
						}
					}
				for( i=1; i<=MaxUnitId; i++)
					{
					ref var unit = ref Units[i];
					if( unit.Side==Side.UnitedStates && ( unit.Kind==UnitKind.AirBase )  && unit.info[0]==0 && PointInRect2(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)!=0 )
						{
						m++;
						}
					}

				if( f==0 && m>=2)
					{
					Result=GameResult.UnitedStatesWon;
					break;
					}

				break;


			case 103:
				f=0;
				for( i=1; i<=MaxUnitId; i++)
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
					break;
					}
				break;



			case 104:
				f=0;
				// ブーゲンビル島
				wrk_r.top=(int)(1200+80);
				wrk_r.right=(int)(-4480+80*5);
				wrk_r.bottom=(int)(1200-80*3);
				wrk_r.left=(int)(-4480-80);
				for( i=1; i<=MaxUnitId; i++)
					{
					ref var unit = ref Units[i];
					if( unit.Side==Side.Japan && ( unit.Kind>=UnitKind.AirBase && unit.Kind<=UnitKind.Fortress ) && PointInRect2(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)!=0 )
						{
						f++;
						}
					}


				if( f==0 )
					{
					Result=GameResult.UnitedStatesWon;
					break;
					}

				break;


			case 105:
				f=0;
				// ブーゲンビル島
				wrk_r.top=(int)(1200+80);
				wrk_r.right=(int)(-4480+80*5);
				wrk_r.bottom=(int)(1200-80*3);
				wrk_r.left=(int)(-4480-80);
				for( i=1; i<=MaxUnitId; i++)
					{
					ref var unit = ref Units[i];
					if( unit.Side==Side.Japan && ( unit.Kind>=UnitKind.AirBase && unit.Kind<=UnitKind.Fortress ) && PointInRect2(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)!=0 )
						{
						f++;
						}
					}
				if( f==0 )
					{
					Result=GameResult.UnitedStatesWon;
					break;
					}

				f=0;
				// ガダルカナル島
				wrk_r.top=(int)(-160);
				wrk_r.right=(int)(-1040+(80*3));
				wrk_r.bottom=(int)(-160-80);
				wrk_r.left=(int)(-1040);
				for( i=1; i<=MaxUnitId; i++)
					{
					ref var unit = ref Units[i];
					if( unit.Side==Side.UnitedStates && ( unit.Kind>=UnitKind.AirBase && unit.Kind<=UnitKind.Fortress ) && PointInRect2(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)!=0 )
						{
						f++;
						}
					}

				if( f==0 )
					{
					Result=GameResult.JapanWon;
					break;
					}
				break;



			case 106:
				// ガ島争奪戦。
				// ガダルカナル島
				wrk_r.top=(int)(-160);
				wrk_r.right=(int)(-1040+(80*3));
				wrk_r.bottom=(int)(-160-80);
				wrk_r.left=(int)(-1040);

				f=0;
				m=0;

				for( i=1; i<=MaxUnitId; i++)
					{
					ref var unit = ref Units[i];
					if( unit.Side==Side.Japan && ( unit.Kind>=UnitKind.AirBase && unit.Kind<=UnitKind.Fortress ) && PointInRect2(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)!=0 )
						{
						f++;	//　日本の施設
						}
					if( unit.Side==Side.UnitedStates && ( unit.Kind>=UnitKind.AirBase && unit.Kind<=UnitKind.Fortress ) && PointInRect2(ref wrk_r,(int)unit.Position.X,(int)unit.Position.Y)!=0 )
						{
						m++;	//　米の施設
						}
					}

				if( f>=4 && m==0 )
					{
					Result=GameResult.JapanWon;
					break;
					}
				if( f==0 && m>=4 )
					{
					Result=GameResult.UnitedStatesWon;
					break;
					}
				break;



			case 995:
				// ミッドウェイを巡る戦い１
				if( !Units[DecisionPoints[0]].IsUsed )
					{
					Result=GameResult.JapanWon;
					break;
					}
/***
				if( rest_time==0)
					{
					game_end=USA_WIN;
					break;
					}
***/
				break;
			}
		}



	// 結果はっぴょー
	if( Result!=GameResult.None )
		{
#if true
		if ( IDirectDrawSurface_GetDC(lpDDSBack, &hdc) == DD_OK ) 
			{
			SetBkMode(hdc, TRANSPARENT);
			SelectObject(hdc, gameFont_1);


#if !LNGG_VER
			switch( Result )
				{
				case GameResult.JapanWon:
					len[0] = wsprintf(ach[0], "ゲーム終了");
					len[1] = wsprintf(ach[1], "日本海軍は勝利条件を達成した。");
					len[2] = wsprintf(ach[2], "日本海軍の勝利");
					break;
				case GameResult.UnitedStatesWon:
					len[0] = wsprintf(ach[0], "ゲーム終了");
					len[1] = wsprintf(ach[1], "合衆国海軍は勝利条件を達成した。");
					len[2] = wsprintf(ach[2], "合衆国海軍の勝利");
					break;
				case GameResult.JapanLost:
					len[0] = wsprintf(ach[0], "ゲーム終了");
					len[1] = wsprintf(ach[1], "日本海軍は勝利条件を達成できなかった。");
					len[2] = wsprintf(ach[2], "");
					break;
				case GameResult.UnitedStatesLost:
					len[0] = wsprintf(ach[0], "ゲーム終了");
					len[1] = wsprintf(ach[1], "合衆国海軍は勝利条件を達成できなかった。");
					len[2] = wsprintf(ach[2], "");
					break;
				case GameResult.Draw:
					len[0] = wsprintf(ach[0], "ゲーム終了");
					len[1] = wsprintf(ach[1], "日米両海軍は勝利目標を達成できなかった。");
					len[2] = wsprintf(ach[2], "引き分け。");
					break;
				}

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

			for( n=0; n<=2; n++)
				{
				SetTextColor(hdc, RGB(255, 255, 255));
				TextOut(hdc, 300, 300+(n*20), ach[n], len[n]);
				}

			IDirectDrawSurface_ReleaseDC(lpDDSBack, hdc);
			}
#endif
		}


//	if( game_end==0)
//		rest_time++;

	}







//============================================================================
// 
//----------------------------------------------------------------------------
[Original("set_unit_data")]
public void	SetUnitData(int m)
	{
	ref var unit = ref Units[m];
	int		i;


	switch( unit.Kind )
		{
		case UnitKind.Battleship:
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
			break;

		case UnitKind.Cruiser:
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
			break;

		case UnitKind.Destroyer:
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
			break;

		case UnitKind.Submarine:
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
			break;

		case UnitKind.Carrier:
			unit.TurnRateChange=0.3;
			unit.AccelerationChange=0.01;
			unit.MinSpeed=0.0;
			unit.MaxSpeed=0.7;

			unit.SpriteRow=6;

			unit.info[0]=0;					// 
			unit.PlaneCount=CountPlanesIn(m);		// 現在収容数(飛行甲板上数も含む)
			unit.Capacity=12;					// 最大収容数
			unit.info[3]=0;					// 
			unit.info[4]=0;					// 発進予定機数 ０なら着艦可
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
			break;


		case UnitKind.LightCarrier:
			unit.TurnRateChange=0.5;
			unit.AccelerationChange=0.01;
			unit.MinSpeed=0.0;
			unit.MaxSpeed=0.9;

			unit.SpriteRow=5;

			unit.info[0]=0;					// 
			unit.PlaneCount=CountPlanesIn(m);		// 現在収容数(飛行甲板上数も含む)
			unit.Capacity=8;					// 最大収容数
			unit.info[3]=0;					// 
			unit.info[4]=0;					// 発進予定機数 ０なら着艦可
			unit.Mode=UnitMode.Move;				// モード（コンバットメニュー）

			unit.Weapon=FireKind.Gun;	//0;		// 武装品種
			unit.Ammo=80;		// 数
			unit.MaxAmmo=80;		// 数 全容量

			unit.Fuel=100;		// 残燃料
			unit.FuelInterval=700;		// 燃料を消費するタイミング

			unit.Skill=5;			

			unit.Hp=unit.MaxHp=CVL1_HP+((unit.Side==Side.UnitedStates ? 1 : 0)*5);		// Ｈｐ
			break;


		case UnitKind.Fighter:
			switch( unit.Variant )
				{
				case 0:		// 艦上戦闘機

					if(unit.Side==Side.Japan)
						{
						unit.TurnRateChange=4.0;
						unit.AccelerationChange=0.01;
						unit.MinSpeed=0.5;
						unit.MaxSpeed=2.35;

						if( unit.Stop!=0 )
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

						if( unit.Stop!=0 )
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
#if true

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


#else
					if(unit[m].used==Side.Japan)
						{
						unit[m].a_drctn_add=3.0;
						unit[m].a_spd_add=0.03;
						unit[m].min_spd=0.5;
						unit[m].max_spd=3.0;

						unit[m].os_indx_y=14;

						unit[m].Weapon=FireKind.Bullet;		// 武装品種
						unit[m].Ammo=60;		// 数
						unit[m].MaxAmmo=60;		// 数 全容量

						unit[m].Fuel=100;		// 残燃料
						unit[m].FuelInterval=55;		// 燃料を消費するタイミング

						unit[m].tech=5;			

						unit[m].Hp=unit[m].MaxHp=FT1_HP*2.0;		// Ｈｐ
						}
					else
						{
						unit[m].a_drctn_add=2.0;
						unit[m].a_spd_add=0.02;
						unit[m].min_spd=0.5;
						unit[m].max_spd=2.8;

						unit[m].os_indx_y=14;

						unit[m].Weapon=FireKind.Bullet;		// 武装品種
						unit[m].Ammo=70;		// 数
						unit[m].MaxAmmo=70;		// 数 全容量

						unit[m].Fuel=100;		// 残燃料
						unit[m].FuelInterval=100;		// 燃料を消費するタイミング

						unit[m].tech=5;			

						unit[m].Hp=unit[m].MaxHp=FT1_HP*3.0;		// Ｈｐ
						}
#endif
					break;
				}
			break;

		case UnitKind.Attacker:
			if(unit.Side==Side.Japan)
				{	
				unit.TurnRateChange=3.0;
				unit.AccelerationChange=0.01;
				unit.MinSpeed=0.5;
				unit.MaxSpeed=2.2;

				if( unit.Stop!=0 )
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

				if( unit.Stop!=0 )
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

			break;



		case UnitKind.Bomber:
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
			break;	



		case UnitKind.Transport:
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
			unit.info[4]=0;					// 発進予定機数 ０なら着艦可
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
	for( i=0; i<=1; i++)
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


	unit.PathX[0]=unit.Position.X;
	unit.PathY[0]=unit.Position.Y;
	unit.PathX[1]=MAP_RIGHT+1;
	//unit[m].max_spd*=1.0;
	}







//============================================================================
//		
//----------------------------------------------------------------------------
[Original("set_new_unit")]
public int		AddUnit(Side side,UnitKind kind,double rx,double ry,double drctn)
	{
	UnitCategory	ctgry; int start,end,m,n;
	int	type;





	if(kind==UnitKind.Attacker||kind==UnitKind.Fighter||kind==UnitKind.Bomber)
		ctgry=UnitCategory.Plane;
	else
		ctgry=UnitCategory.Ship;

//	if(kind==BB1||kind==CA1||kind==DD1||kind==SS1||kind==CV1||kind==CVL1||kind==TR1||kind==AP||kind==SP)
//		ctgry=SHIP;
//	else
//		ctgry=PLANE;



	if(side==Side.Japan)
		{
		// 日本サイドのユニット
		if( ctgry==UnitCategory.Ship )
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
		if( ctgry==UnitCategory.Ship )
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


	for(m=start;m<=end;m++)
		{
		if( !Units[m].IsUsed )
			{
			// まずクリア
			Units[m].Side=0;
			Units[m].Position = new WorldPosition(0, 0);
			Units[m].Category=UnitCategory.None;
			Units[m].Kind=0;
			Units[m].Variant=0;
			for(n=0;n<=15;n++)
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
			Units[m].Stop=0;
			Units[m].Supply=0;
			Units[m].EmergencyFlags[0]=Units[m].EmergencyFlags[1]=0;
			Units[m].EmergencyDestination=new WorldPosition(0, 0);
			//unit[m].pp_now=0;

			Units[m].IsGroupLeader=0;
			Units[m].GroupLeader=0;
			Units[m].FormationNumber=0;
			Units[m].ForGroupLeader=0;

			Units[m].FormationSpeed=0;

			for(n=0;n<=7;n++)
				{
				Units[m].hp[n]=0;
				Units[m].arm[n]=0;
//				unit[m].arm2[n]=0;
				Units[m].gas[n]=0;
				}
			Selections[0][m]=0;
			Selections[1][m]=0;
			Units[m].Skill=0;


			// あきスペース発見
			Units[m].Side=side;
			Units[m].Position = new WorldPosition(rx, ry);
			Units[m].Category=ctgry;
			Units[m].Kind=kind;
			Units[m].Direction=drctn;
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
public int		AddUnit2(Side side,UnitKind kind,int type,double rx,double ry,double drctn)
	{
	UnitCategory	ctgry; int start,end,m,n;


	if(kind==UnitKind.Attacker||kind==UnitKind.Fighter||kind==UnitKind.Bomber)
		ctgry=UnitCategory.Plane;
	else
		ctgry=UnitCategory.Ship;

//	if(kind==BB1||kind==CA1||kind==DD1||kind==SS1||kind==CV1||kind==CVL1||kind==TR1||kind==AP||kind==SP)
//		ctgry=SHIP;
//	else
//		ctgry=PLANE;



	if(side==Side.Japan)
		{
		// 日本サイドのユニット
		if( ctgry==UnitCategory.Ship )
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
		if( ctgry==UnitCategory.Ship )
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


	for(m=start;m<=end;m++)
		{
		if( !Units[m].IsUsed )
			{
			// まずクリア
			Units[m].Side=0;
			Units[m].Position = new WorldPosition(0, 0);
			Units[m].Category=UnitCategory.None;
			Units[m].Kind=0;
			Units[m].Variant=(short)type;
			for(n=0;n<=15;n++)
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
			Units[m].Stop=0;
			Units[m].Supply=0;
			Units[m].EmergencyFlags[0]=Units[m].EmergencyFlags[1]=0;
			Units[m].EmergencyDestination=new WorldPosition(0, 0);
			//unit[m].pp_now=0;

			Units[m].IsGroupLeader=0;
			Units[m].GroupLeader=0;
			Units[m].FormationNumber=0;
			Units[m].ForGroupLeader=0;

			Units[m].FormationSpeed=0;

			for(n=0;n<=7;n++)
				{
				Units[m].hp[n]=0;
				Units[m].arm[n]=0;
//				unit[m].arm2[n]=0;
				Units[m].gas[n]=0;
				}
			Selections[0][m]=0;
			Selections[1][m]=0;
			Units[m].Skill=0;


			// あきスペース発見
			Units[m].Side=side;
			Units[m].Position = new WorldPosition(rx, ry);
			Units[m].Category=ctgry;
			if(ctgry==UnitCategory.Plane)
				Units[m].PlaneState=UnitState.Flying;
			Units[m].Kind=kind;
			Units[m].Direction=drctn;
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
public int		AddPlane(Side side,UnitKind kind,int type,int no,int planes,FireKind arm)
	{
	UnitCategory	ctgry; int start,end,m,n,f=default /* C4701 */,park;
	Array24<int> space = default;

	ctgry=UnitCategory.Plane;



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


	for(m=start;m<=end && planes!=0 ;m++)
		{
		if( !Units[m].IsUsed )
			{
			// あきスペース発見


			for( n=0; n<24; n++)
				{
				space[n]=0;
				}
			park=0;
			for(n=1;n<=MaxUnitId;n++)
				{
				ref var unit = ref Units[n];
				if( unit.IsUsed && unit.Category==UnitCategory.Plane && unit.Carrier==no && unit.PlaneState==UnitState.Parked )
					{
					space[unit.ParkingNumber]=1;
					park++;
					}
				}	


			if( park >= Units[no].Capacity )
				return (-planes);



			for( n=0; n<24; n++)
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
			Units[m].Carrier=no;					// 所属の空母、及び、基地の番号
			Units[m].ParkingNumber=f;				// 格納庫の位置、及び、その基地の番機番号
			Units[m].info[3]=0;					// 8
			Units[m].info[4]=0;					// 発艦予定の機数
			Units[m].Mode=UnitMode.Move;				// モード（コンバットメニュー）
			SetParkingPosition(m);
			Units[m].Stop=1;

			Units[Units[m].Carrier].info[1]++;					// 所属の空母、及び、基地の格納数を増やす｡


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
					Units[m].Ammo=Units[m].MaxAmmo/*1*/;		// 数
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
	int		m,no;
	double	rx,ry;
//	int		tf_no,unit_no;
	



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
		IsDecisionEnabled=1;

		return;
		}



	//  ゲーム設定
//	cmbt_x=0;
//	cmbt_y=0;

//	rest_time=50000;


	//===============		 日本海軍		================


	/*クエゼリン環礁*/
	rx=-7120;
	ry=6720;
	AddUnit(Side.Japan,UnitKind.NavalBase,rx,ry,(double)0);


	/*クエゼリン*/
	rx=-7120-80;
	ry=6720;
	AddUnit(Side.Japan,UnitKind.InfantryBase,rx,ry,(double)0);



	rx=-2500;
	ry=2500;
//rx=0;
	m=AddUnit(Side.Japan,UnitKind.Carrier,rx,ry,(double)0);
	no=m;
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,9,FireKind.Torpedo);
	m=AddPlane(Side.Japan,UnitKind.Fighter,0,no,3,FireKind.Unarmed);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);




/*
ry-=500;
m=set_new_unit(USA,CA1,rx,ry,(double)0);
*/


/**
for(n=0;n<45;n++)
{
rx-=100;
//m=set_new_unit(JPN,BB1+(n%6),rx,ry,(double)0);
m=set_new_unit(JPN,CV1,rx,ry,(double)0);
no=m;
m=set_new_unit_plane(JPN,AT1,0,no,12,TPD);
}
***/

	//===============		 合衆国海軍		================

	/*ヌーメア軍港*/
	rx=5440;
	ry=-5760;
	m=AddUnit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,(double)0);

	/*ヌーメア*/
	rx=5440-80;
	ry=-5760;
	m=AddUnit(Side.UnitedStates,UnitKind.InfantryBase,rx,ry,(double)0);



	rx=2500;
	ry=-2500;

//rx=0;
//ry=2500;

	m=AddUnit(Side.UnitedStates,UnitKind.Carrier,rx,ry,(double)180);
	no=m;
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,9,FireKind.Bomb);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,3,FireKind.Unarmed);

	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);

	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);

	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);


	// マップの作成
	LoadScenarioFile2("Map\\South_pacific.dat");

	}






//============================================================================
//		
//----------------------------------------------------------------------------
[Original("cnct_sinario_2")]
public void	SetUpScenario2()
	{
	int		m,no;
	double	rx,ry;
//	int		tf_no,unit_no;


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
		IsDecisionEnabled=1;


		return;
		}

	

	//  ゲーム設定
//	cmbt_x=0;
//	cmbt_y=0;

//	rest_time=75000;



	//===============		 日本海軍		================
	// トラック島 港
	rx=-7600;
	ry=-4720;
	m=AddUnit(Side.Japan,UnitKind.NavalBase,rx,ry,(double)0);

	// トラック島
	rx=-7600-80;
	ry=-4720;
	m=AddUnit(Side.Japan,UnitKind.InfantryBase,rx,ry,(double)0);


	rx=-5000;
	ry=-2500;

	m=AddUnit(Side.Japan,UnitKind.Carrier,rx,ry,0);
	no=m;
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,9,FireKind.Torpedo);
	m=AddPlane(Side.Japan,UnitKind.Fighter,0,no,3,FireKind.Unarmed);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.LightCarrier,rx,ry,(double)0);
	no=m;
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,6,FireKind.Torpedo);
	m=AddPlane(Side.Japan,UnitKind.Fighter,0,no,2,FireKind.Unarmed);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);




	//===============		 合衆国海軍		================
	// ハワイ港
	rx=7200;
	ry=-720;
	m=AddUnit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,(double)90.0);


	// ハワイ
	rx=7200-80;
	ry=-720+80;
	m=AddUnit(Side.UnitedStates,UnitKind.InfantryBase,rx,ry,(double)90.0);


	rx=5000;
	ry=2500;

	m=AddUnit(Side.UnitedStates,UnitKind.Carrier,rx,ry,(double)180);
	no=m;
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,9,FireKind.Bomb);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,3,FireKind.Unarmed);

	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.LightCarrier,rx,ry,(double)180);
	no=m;
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,6,FireKind.Bomb);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,2,FireKind.Unarmed);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);




	// マップの作成
	LoadScenarioFile2("Map\\Middle_pacific.dat");

	}









//============================================================================
//		
//----------------------------------------------------------------------------
[Original("cnct_sinario_3")]
public void	SetUpScenario3()
	{
	int		m,n,no;
	double	rx,ry;
//	int		tf_no,unit_no;
	

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

		IsDecisionEnabled=1;

		return;
		}


	//  ゲーム設定
//	cmbt_x=0;
//	cmbt_y=0;

//	rest_time=100000;



	//===============		 日本海軍		================
	/*クエゼリン環礁*/
//	rx=-7120;
//	ry=6640;
//	m=set_new_unit(JPN,SP,rx,ry,(double)0);

	// 横須賀
	// 港
	rx=-7440;
	ry=4880;
	m=AddUnit(Side.Japan,UnitKind.NavalBase,rx,ry,(double)0);

	// 横須賀
	rx=-7440-80;
	ry=4880+80;
	m=AddUnit(Side.Japan,UnitKind.InfantryBase,rx,ry,(double)0);



	rx=-6500;
	ry=5000;

	m=AddUnit(Side.Japan,UnitKind.Carrier,rx,ry,(double)0);
	no=m;
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,9,FireKind.Torpedo);
	m=AddPlane(Side.Japan,UnitKind.Fighter,0,no,3,FireKind.Unarmed);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);


	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Battleship,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);



	//===============		 合衆国海軍		================
	/*ヌーメア軍港*/
//	rx=5440;
//	ry=-5800;
//	m=set_new_unit(USA,SP,rx,ry,(double)0);

	// パラオ
	rx=6720-80;
	ry=-3760;
	m=AddUnit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,(double)0);

	// パラオ
	rx=6720-80;
	ry=-3760+80;
	m=AddUnit(Side.UnitedStates,UnitKind.InfantryBase,rx,ry,(double)0);


	rx=6500;
	ry=-5000;

	m=AddUnit(Side.UnitedStates,UnitKind.Carrier,rx,ry,(double)180);
	no=m;
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,9,FireKind.Bomb);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,3,FireKind.Unarmed);

	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);

	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);

	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);



	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Battleship,rx,ry,(double)180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);



	// マップの作成
//	load_it2("Map\\South_pacific.dat");
	LoadScenarioFile2("Map\\Japan_off.dat");

	}







//============================================================================
//		
//----------------------------------------------------------------------------
[Original("cnct_sinario_4")]
public void	SetUpScenario4()
	{
	int		m,n,no;
	double	rx,ry;
//	int		tf_no,unit_no;
	

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
		IsDecisionEnabled=1;

		return;
		}



	//  ゲーム設定
//	cmbt_x=0;
//	cmbt_y=0;

//	rest_time=50000;


	//===============		 日本海軍		================

	/*クエゼリン環礁*/
	rx=-7120;
	ry=6720;
	AddUnit(Side.Japan,UnitKind.NavalBase,rx,ry,(double)0);


	// クエゼリン
	rx=-7120-80;
	ry=6720;
	AddUnit(Side.Japan,UnitKind.InfantryBase,rx,ry,(double)0);


	rx=-2500;
	ry=2500;

	m=AddUnit(Side.Japan,UnitKind.Battleship,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);



	//===============		 合衆国海軍		================

	/*ヌーメア軍港*/
	rx=5440;
	ry=-5760;
	m=AddUnit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,(double)0);


	// ヌーメア
	rx=5440-80;
	ry=-5760-80;
	m=AddUnit(Side.UnitedStates,UnitKind.InfantryBase,rx,ry,(double)0);



	rx=2500;
	ry=-2500;

	m=AddUnit(Side.UnitedStates,UnitKind.Battleship,rx,ry,(double)180);
	rx+=120;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);
	rx+=120;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);
	rx+=120;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);
	rx+=120;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);
	rx+=120;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);



	// マップの作成
	LoadScenarioFile2("Map\\South_pacific.dat");

	}







//============================================================================
//		
//----------------------------------------------------------------------------
[Original("cnct_sinario_5")]
public void	SetUpScenario5()
	{
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;
	

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

		IsDecisionEnabled=1;

		return;
		}



	//  ゲーム設定
//	cmbt_x=0;
//	cmbt_y=0;

//	rest_time=50000;


	//===============		 日本海軍		================

	/*クエゼリン環礁*/
	rx=-7120;
	ry=6720;
	AddUnit(Side.Japan,UnitKind.NavalBase,rx,ry,(double)0);

	// クエゼリン環礁
	rx=-7120-80;
	ry=6720;
	AddUnit(Side.Japan,UnitKind.InfantryBase,rx,ry,(double)0);


	rx=-2500;
	ry=2500;

	m=AddUnit(Side.Japan,UnitKind.Battleship,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Battleship,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);

	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Submarine,rx,ry,(double)0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Submarine,rx,ry,(double)0);


	//===============		 合衆国海軍		================

	/*ヌーメア軍港*/
	rx=5440;
	ry=-5760;
	m=AddUnit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,(double)0);

	// ヌーメア軍港
	rx=5440-80;
	ry=-5760-80;
	m=AddUnit(Side.UnitedStates,UnitKind.InfantryBase,rx,ry,(double)0);

	rx=2500;
	ry=-2500;
//rx=0;
//ry=2500;
	m=AddUnit(Side.UnitedStates,UnitKind.Battleship,rx,ry,(double)180);
	rx+=120;
	m=AddUnit(Side.UnitedStates,UnitKind.Battleship,rx,ry,(double)180);
	rx+=120;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);
	rx+=120;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);
	rx+=120;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);
	rx+=120;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);
	rx+=120;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);
	rx+=120;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);
	rx+=120;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);

	rx+=120;
	m=AddUnit(Side.UnitedStates,UnitKind.Submarine,rx,ry,(double)180);
	rx+=120;
	m=AddUnit(Side.UnitedStates,UnitKind.Submarine,rx,ry,(double)180);


	// マップの作成
	LoadScenarioFile2("Map\\South_pacific.dat");

	}







//============================================================================
//		
//----------------------------------------------------------------------------
[Original("cnct_sinario_6")]
public void	SetUpScenario6()
	{
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;
	

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

		IsDecisionEnabled=1;

		return;
		}


	//  ゲーム設定
//	cmbt_x=0;
//	cmbt_y=0;
//	rest_time=50000;



	//===============		 日本海軍		================
#if true
	// トラック
	rx=-7600;
	ry=-4720;
	m=AddUnit(Side.Japan,UnitKind.NavalBase,rx,ry,135);

	// 空港
	rx=-7600-80;
	ry=-4720;
	no=m=AddUnit(Side.Japan,UnitKind.AirBase,rx,ry,135);
	no=m;
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,5,FireKind.Bomb);
	m=AddPlane(Side.Japan,UnitKind.Fighter,0,no,3,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Attacker,1,no,1,FireKind.Bomb);


	// 戦闘艦船
	rx=-7450;
	ry=-4550;
	m=AddUnit(Side.Japan,UnitKind.Battleship,rx-50,ry,135);
	Units[m].Fuel*=0.1;
	Units[m].Ammo=(int)(Units[m].MaxAmmo*0.2);			// 数
	Units[m].Supply=1;
	ry-=150;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,135);
	Units[m].Fuel*=0.2;
	Units[m].Ammo=(int)(Units[m].MaxAmmo*0.1);			// 数
	Units[m].Supply=1;
	ry-=150;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,135);
	Units[m].Fuel*=0.1;
	Units[m].Ammo=(int)(Units[m].MaxAmmo*0.2);			// 数
	Units[m].Supply=1;
	ry-=150;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx-80,ry,135);
	Units[m].Fuel*=0.1;
	Units[m].Ammo=(int)(Units[m].MaxAmmo*0.3);			// 数
	Units[m].Supply=1;


	// 輸送船団
	rx=-7340;
	ry=-4690;
//rx=-800;
//ry=5500;
	no=m=AddUnit(Side.Japan,UnitKind.Transport,rx,ry,270);
	Units[m].Weapon=FireKind.CargoInfantryBase;		// 武装品種
	Units[m].Ammo=1;			// 数
	Units[m].MaxAmmo=1;			// 数 全容量
	Units[m].Fuel*=0.2;
	Units[m].Supply=1;
	ry-=150;
	no=m=AddUnit(Side.Japan,UnitKind.Transport,rx-20,ry,135);
	Units[m].Weapon=FireKind.CargoPillboxes;		// 武装品種
	Units[m].Ammo=1;			// 数
	Units[m].MaxAmmo=1;			// 数 全容量
	Units[m].Fuel*=0;
	Units[m].Supply=1;
	ry-=150;
	no=m=AddUnit(Side.Japan,UnitKind.Transport,rx,ry,90);
	Units[m].Weapon=FireKind.CargoPillboxes;		// 武装品種
	Units[m].Ammo=1;			// 数
	Units[m].MaxAmmo=1;			// 数 全容量
	Units[m].Fuel*=0;
	Units[m].Supply=1;
	ry-=150;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx+90,ry-40,90);

	// 潜水艦
	rx=-7800;
	ry=-2000;
	m=AddUnit(Side.Japan,UnitKind.Submarine,rx,ry,0);
#endif



#if false
// 戦闘艦船
rx=-7800-200;
ry=-2000;
m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,135);

// 戦闘艦船
rx=-7600-100;
ry=-4720;
m=set_new_unit(Side.Japan,UnitKind.Submarine,rx,ry,135);
unit[m].Fuel*=0.1;
unit[m].Ammo=unit[m].MaxAmmo*0.1;			// 数

// 戦闘艦船
rx=-7600;
ry=-4720-800;
m=set_new_unit(Side.UnitedStates,UnitKind.Submarine,rx,ry,135);
#endif



#if false
	// 空港
	rx=-7600-80;
	ry=-4720;
	no=m=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,135);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,6,FireKind.Bomb);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,8,FireKind.Unarmed);


// 戦闘艦船
rx=0;
ry=0;
/*
m=set_new_unit(USA,BB1,rx,ry,135);
ry-=80;
m=set_new_unit(USA,BB1,rx,ry,135);
ry-=80;
//m=set_new_unit(USA,BB1,rx,ry,135);
*/
no=m=set_new_unit(Side.UnitedStates,UnitKind.Carrier,rx,ry,180);
m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,12,FireKind.Bomb);
//m=set_new_unit_plane(USA,FT1,0,no,2,NTG);


// 戦闘艦船
rx=800;
ry=0;
m=set_new_unit(Side.Japan,UnitKind.Fortress,rx,ry,135);

#endif




	//===============		 合衆国海軍		================
	// ハワイ港	
	rx=7200;
	ry=-640;
	m=AddUnit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,90.0);

	// ハワイ島 基地
	rx=7200-80;
	ry=-640+80*2;
	m=AddUnit(Side.UnitedStates,UnitKind.Fortress,rx,ry,0);

	// ハワイ空港
	rx=7200;
	ry=-560;
	m=AddUnit(Side.UnitedStates,UnitKind.AirBase,rx,ry,45.0);
	no=m;
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,1,FireKind.Unarmed);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,3,FireKind.Unarmed);
	m=AddPlane(Side.UnitedStates,UnitKind.Bomber,0,no,4,FireKind.Unarmed);



	// ミッドウェイ島 基地
	rx=80+160;
	ry=3440-80;
	m=AddUnit(Side.UnitedStates,UnitKind.Pillboxes,rx,ry,0);


	// ミッドウェイ島航空基地 1
	rx=80;
	ry=3440;
	no=AddUnit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,2,FireKind.Unarmed);
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,2,FireKind.Unarmed);

	// 機動部隊
	rx=-3500;
//rx=-6500;
	ry=-3000;
	no=m=AddUnit(Side.UnitedStates,UnitKind.Carrier,rx,ry,180);
	Units[m].Fuel/=2;
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,10,FireKind.Bomb);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,2,FireKind.Unarmed);
	rx+=150;
	no=m=AddUnit(Side.UnitedStates,UnitKind.LightCarrier,rx,ry,180);
	Units[m].Fuel/=2;
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,6,FireKind.Bomb);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,2,FireKind.Unarmed);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	Units[m].Fuel/=2;
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,180);
	Units[m].Fuel/=2;
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
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
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;


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

		IsDecisionEnabled=1;

		return;
		}


	//  ゲーム設定
//	cmbt_x=0;
//	cmbt_y=0;
//	rest_time=50000;



	//===============		 日本海軍		================

	// トラック
	rx=-7600;
	ry=-4720;
	m=AddUnit(Side.Japan,UnitKind.NavalBase,rx,ry,135);


	// 空港
	rx=-7600-80;
	ry=-4720;
	no=m=AddUnit(Side.Japan,UnitKind.AirBase,rx,ry,135);
	no=m;
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,5,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Fighter,0,no,3,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Attacker,1,no,1,FireKind.Unarmed);


	// 戦闘艦船
	rx=-7450;
	ry=-4550;
	m=AddUnit(Side.Japan,UnitKind.Battleship,rx-50,ry,135);
	Units[m].Fuel*=0.1;
	Units[m].Ammo=(int)(Units[m].MaxAmmo*0.2);			// 数
	Units[m].Supply=1;
	ry-=150;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,135);
	Units[m].Fuel*=0.2;
	Units[m].Ammo=(int)(Units[m].MaxAmmo*0.1);			// 数
	Units[m].Supply=1;
	ry-=150;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,135);
	Units[m].Fuel*=0.1;
	Units[m].Ammo=(int)(Units[m].MaxAmmo*0.2);			// 数
	Units[m].Supply=1;
	ry-=150;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx-80,ry,135);
	Units[m].Fuel*=0.1;
	Units[m].Ammo=(int)(Units[m].MaxAmmo*0.3);			// 数
	Units[m].Supply=1;


	// 輸送船団
	rx=-7340;
	ry=-4690;
//rx=-800;
//ry=5500;
	no=m=AddUnit(Side.Japan,UnitKind.Transport,rx,ry,270);
	Units[m].Weapon=FireKind.CargoInfantryBase;		// 武装品種
	Units[m].Ammo=1;			// 数
	Units[m].MaxAmmo=1;			// 数 全容量
	Units[m].Fuel*=0.2;
	Units[m].Supply=1;
	ry-=150;
	no=m=AddUnit(Side.Japan,UnitKind.Transport,rx-20,ry,135);
	Units[m].Weapon=FireKind.CargoPillboxes;		// 武装品種
	Units[m].Ammo=1;			// 数
	Units[m].MaxAmmo=1;			// 数 全容量
	Units[m].Fuel*=0;
	Units[m].Supply=1;
	ry-=150;
	no=m=AddUnit(Side.Japan,UnitKind.Transport,rx,ry,90);
	Units[m].Weapon=FireKind.CargoPillboxes;		// 武装品種
	Units[m].Ammo=1;			// 数
	Units[m].MaxAmmo=1;			// 数 全容量
	Units[m].Fuel*=0;
	Units[m].Supply=1;
	ry-=150;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx+90,ry-40,90);

	// 潜水艦
	rx=-7800;
	ry=-2000;
	m=AddUnit(Side.Japan,UnitKind.Submarine,rx,ry,0);

/*
rx=-7800+800;
ry=-2000;
no=m=set_new_unit(USA,DD1,rx,ry,180);
*/
/***
rx=-7800+600;
ry=-2000+600;
no=m=set_new_unit(USA,SP,rx,ry,180);
***/

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
	rx=7200;
	ry=-640;
	m=AddUnit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,90.0);

	// ハワイ島 基地
	rx=7200-80;
	ry=-640+80*2;
	m=AddUnit(Side.UnitedStates,UnitKind.Fortress,rx,ry,0);

	// ハワイ空港
	rx=7200;
	ry=-560;
	m=AddUnit(Side.UnitedStates,UnitKind.AirBase,rx,ry,45.0);
	no=m;
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,1,no,3,FireKind.Unarmed);
	m=AddPlane(Side.UnitedStates,UnitKind.Bomber,0,no,2,FireKind.Unarmed);
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,4,FireKind.Unarmed);



	// ミッドウェイ島 基地
	rx=80+160;
	ry=3440-80;
	m=AddUnit(Side.UnitedStates,UnitKind.Pillboxes,rx,ry,0);


	// ミッドウェイ島航空基地 1
	rx=80;
	ry=3440;
	no=AddUnit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,2,FireKind.Unarmed);
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,2,FireKind.Unarmed);

	// 機動部隊
	rx=-3500;
	ry=-3000;
	no=m=AddUnit(Side.UnitedStates,UnitKind.Carrier,rx,ry,180);
	Units[m].Fuel/=2;
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,10,FireKind.Bomb);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,2,FireKind.Unarmed);
	rx+=150;
	no=m=AddUnit(Side.UnitedStates,UnitKind.LightCarrier,rx,ry,180);
	Units[m].Fuel/=2;
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,6,FireKind.Bomb);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,2,FireKind.Unarmed);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	Units[m].Fuel/=2;
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,180);
	Units[m].Fuel/=2;
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
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
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;
	

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

		IsDecisionEnabled=1;

		return;
		}


	//  ゲーム設定
//	cmbt_x=0;
//	cmbt_y=0;
//	rest_time=50000;



	//===============		 日本海軍		================

	// トラック
	rx=-7600;
	ry=-4720;
	m=AddUnit(Side.Japan,UnitKind.NavalBase,rx,ry,135);


	// 空港
	rx=-7600-80;
	ry=-4720;
	no=AddUnit(Side.Japan,UnitKind.AirBase,rx,ry,135);
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,3,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Fighter,0,no,3,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Attacker,1,no,2,FireKind.Unarmed);


	// ウェーク
	rx=-4080;
	ry=-720;
	m=AddUnit(Side.Japan,UnitKind.AirBase,rx,ry,180);
	no=m;
	m=AddPlane(Side.Japan,UnitKind.Fighter,0,no,6,FireKind.Bomb);
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,3,FireKind.Unarmed);

	rx=-4080+80;
	ry=-720-160;
	m=AddUnit(Side.Japan,UnitKind.InfantryBase,rx,ry,180);



	// 機動部隊
	rx=-7400;
	ry=-4200;
	no=AddUnit(Side.Japan,UnitKind.Carrier,rx,ry,90);
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,10,FireKind.Bomb);
	m=AddPlane(Side.Japan,UnitKind.Fighter,0,no,2,FireKind.Unarmed);
	ry-=90;
	no=AddUnit(Side.Japan,UnitKind.LightCarrier,rx,ry,90);
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,0,FireKind.Bomb);
	m=AddPlane(Side.Japan,UnitKind.Fighter,3,no,0,FireKind.Unarmed);
	ry-=90;
	m=AddUnit(Side.Japan,UnitKind.Battleship,rx,ry,90);
	ry-=90;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,90);
	ry-=90;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,90);
	ry-=90;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,90);


	// 戦闘艦船
	rx=-7500;
	ry=-4550;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,45);
	ry-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx+40,ry,135);
	ry-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx+20,ry,270);


	// 潜水艦
	rx=-7800;
	ry=-5000;
	m=AddUnit(Side.Japan,UnitKind.Submarine,rx,ry,45);





	//===============		 合衆国海軍		================
	// ハワイ港	
	rx=7200;
	ry=-640;
	m=AddUnit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,90.0);
	// ハワイ島 基地
	rx=7200;
	ry=-640+80;
	m=AddUnit(Side.UnitedStates,UnitKind.Fortress,rx,ry,0);
	// ハワイ島 空港
	rx=7200+80;
	ry=-640+80+80;
	no=AddUnit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,5,FireKind.Unarmed);
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,3,FireKind.Unarmed);
	m=AddPlane(Side.UnitedStates,UnitKind.Bomber,0,no,2,FireKind.Unarmed);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,1,no,2,FireKind.Unarmed);



	// ミッドウェイ島 基地
	rx=80+160;
	ry=3440-80;
	m=AddUnit(Side.UnitedStates,UnitKind.Pillboxes,rx,ry,0);


	// ミッドウェイ島航空基地 1
	rx=80;
	ry=3440;
	no=AddUnit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,2,FireKind.Unarmed);
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,2,FireKind.Unarmed);



	// 機動部隊
	rx=3500;
	ry=3000;
	no=AddUnit(Side.UnitedStates,UnitKind.Carrier,rx,ry,180);
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,10,FireKind.Bomb);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,2,FireKind.Unarmed);
	rx+=150;
	no=AddUnit(Side.UnitedStates,UnitKind.LightCarrier,rx,ry,180);
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,6,FireKind.Bomb);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,2,FireKind.Unarmed);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);



	// 艦隊
	rx=7200+140;
	ry=-640+80;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,45);
	rx+=120;
	m=AddUnit(Side.UnitedStates,UnitKind.Battleship,rx,ry,45);
	rx+=120;
	m=AddUnit(Side.UnitedStates,UnitKind.Submarine,rx,ry,45);



	// マップの作成
	LoadScenarioFile2("Map\\Middle_pacific.dat");

	}






//============================================================================
//		
//----------------------------------------------------------------------------
[Original("cnct_sinario_9")]
public void	SetUpScenario9()
	{
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;
	short		data;	

	HANDLE	hFile;
	Array256<Array256<ushort>> szBuf = default;					// マップ





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
			data=InitialSupplyPoints[0];
			InitialSupplyPoints[0]=InitialSupplyPoints[1];
			InitialSupplyPoints[1]=data;

			data=SupplyRates[0];
			SupplyRates[0]=SupplyRates[1];
			SupplyRates[1]=data;
			}
		return;
		}
	else
		{
		LoadScenarioFile3();		

//		load_it2( user_sinario_fn);
//		load_user_map();
		}


//	if( map_edit )
//		{
//		put_trgt=1;
//		put_kind=BB1;
//		}

	CurrentMap=3;

	// マップの作成
//	load_it2("Map\\user_map.dat");

	}









//============================================================================
//		
//----------------------------------------------------------------------------
[Original("cnct_sinario_101")]
public void	SetUpScenario101()
	{
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;
	

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

		IsDecisionEnabled=1;
		return;
		}

	//  ゲーム設定
//	cmbt_x=0;
//	cmbt_y=0;
//	rest_time=50000;




	//===============		 日本海軍		================
	// クエゼリン環礁
	rx=-7120;
	ry=6640;
	m=AddUnit(Side.Japan,UnitKind.NavalBase,rx,ry,0);

	// クエゼリン航空基地
	rx=-7120-80;
	ry=6640+80;
	no=AddUnit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	m=AddPlane(Side.Japan,UnitKind.Fighter,1,no,2,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,5,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Bomber,0,no,3,FireKind.Unarmed);

	// ラバウル 空港
	rx=-7520;
	ry=1840;
	no=AddUnit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,3,FireKind.Bomb);
	m=AddPlane(Side.Japan,UnitKind.Bomber,0,no,1,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Fighter,1,no,2,FireKind.Unarmed);

	// ガダルカナル
	rx=-880;
	ry=-240;
	m=AddUnit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);
	rx=-880+80;
	ry=-240;
	m=AddUnit(Side.Japan,UnitKind.AirBase,rx,ry,0);

	// 機動部隊
	rx=-6500;
	ry=4500;
	m=AddUnit(Side.Japan,UnitKind.Carrier,rx,ry,0);
	no=m;
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,3,FireKind.Torpedo);
	m=AddPlane(Side.Japan,UnitKind.Fighter,0,no,3,FireKind.Unarmed);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Battleship,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Battleship,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,0);




	//===============		 合衆国海軍		================
	/*ヌーメア軍港*/
	rx=5440;
	ry=-5800+40;
	m=AddUnit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,0);


	/*ヌーメア航空基地*/
	rx=5280;
	ry=-5760;
	m=AddUnit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);
	no=m;
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,4,FireKind.Unarmed);
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,8,FireKind.Unarmed);

	/*ヌーメア要塞*/
	rx=5360;
	ry=-5760;
	m=AddUnit(Side.UnitedStates,UnitKind.Pillboxes,rx,ry,0);

	// 機動部隊
	rx=6500;
	ry=-5000;
	m=AddUnit(Side.UnitedStates,UnitKind.Carrier,rx,ry,180);
	no=m;
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,9,FireKind.Bomb);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,3,FireKind.Unarmed);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Battleship,rx,ry,180);






	// マップの作成
	LoadScenarioFile2("Map\\South_pacific.dat");

	}








//============================================================================
//		
//----------------------------------------------------------------------------
[Original("cnct_sinario_102")]
public void	SetUpScenario102()
	{
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;
	

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

		IsDecisionEnabled=1;

		return;
		}


	//  ゲーム設定
//	cmbt_x=0;
//	cmbt_y=0;
//	rest_time=50000;



	//===============		 日本海軍		================
	/*クエゼリン環礁*/
	rx=-7120;
	ry=6640;
	m=AddUnit(Side.Japan,UnitKind.NavalBase,rx,ry,0);

	// クエゼリン航空基地
	rx=-7120-80;
	ry=6640+80;
	no=AddUnit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	m=AddPlane(Side.Japan,UnitKind.Fighter,1,no,2,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,5,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Bomber,0,no,3,FireKind.Unarmed);

	// ラバウル 空港
	rx=-7520;
	ry=1840;
	no=AddUnit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,2,FireKind.Bomb);
	m=AddPlane(Side.Japan,UnitKind.Fighter,0,no,5,FireKind.Unarmed);

	// ガダルカナル
	rx=-880;
	ry=-240;
	m=AddUnit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);
	rx=-880+80;
	ry=-240;
	m=AddUnit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	rx=-880-80;
	ry=-240+80;
	m=AddUnit(Side.Japan,UnitKind.Pillboxes,rx,ry,0);

	// 機動部隊
	rx=-6500;
	ry=5000;
	m=AddUnit(Side.Japan,UnitKind.Carrier,rx,ry,0);
	no=m;
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,3,FireKind.Torpedo);
	m=AddPlane(Side.Japan,UnitKind.Fighter,0,no,3,FireKind.Unarmed);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Battleship,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Submarine,rx,ry,0);


	//===============		 合衆国海軍		================
	/*ヌーメア軍港*/
	rx=5440;
	ry=-5800+40;
	m=AddUnit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,0);


	/*ヌーメア航空基地*/
	rx=5280;
	ry=-5760;
	m=AddUnit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);
	no=m;
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,4,FireKind.Unarmed);
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,8,FireKind.Unarmed);

	/*ヌーメア要塞*/
	rx=5360;
	ry=-5760;
	m=AddUnit(Side.UnitedStates,UnitKind.Pillboxes,rx,ry,0);

	// 機動部隊
	rx=6500;
	ry=-5000;
	m=AddUnit(Side.UnitedStates,UnitKind.Carrier,rx,ry,180);
	no=m;
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,9,FireKind.Bomb);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,3,FireKind.Unarmed);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Battleship,rx,ry,180);








	// マップの作成
	LoadScenarioFile2("Map\\South_pacific.dat");

	}



//============================================================================
//		
//----------------------------------------------------------------------------
[Original("cnct_sinario_103")]
public void	SetUpScenario103()
	{
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;
	

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

		IsDecisionEnabled=1;

		return;
		}


	//  ゲーム設定
//	cmbt_x=0;
//	cmbt_y=0;
//	rest_time=50000;



	//===============		 日本海軍		================
	// 東京
	// 空港
	rx=-7440;
	ry=5440;
	m=AddUnit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	no=m;
	m=AddPlane(Side.Japan,UnitKind.Fighter,0,no,2,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,2,FireKind.Unarmed);
	// 都市
	rx=-7440-80*4;
	ry=5440-80*2;
	m=AddUnit(Side.Japan,UnitKind.City,rx,ry,0);
/****	
	// 都市
	rx=-7440-80*3;
	ry=5440-80*4;
	m=set_new_unit(JPN,CT1,rx,ry,0);
****/

	// 都市
	rx=-7440-80*2;
	ry=5440-80*5;
	m=AddUnit(Side.Japan,UnitKind.City,rx,ry,0);
/***
	// 都市
	rx=-7440-80*3;
	ry=5440-80*3;
	m=set_new_unit(JPN,CT1,rx,ry,0);
***/
	// 都市
	rx=-7440-80*3;
	ry=5440-80*1;
	m=AddUnit(Side.Japan,UnitKind.City,rx,ry,0);

	// 基地
	rx=-7440-80*4;
	ry=5440-80*3;
	m=AddUnit(Side.Japan,UnitKind.Pillboxes,rx,ry,0);


	// 横須賀
	// 港
	rx=-7440;
	ry=4880;
	m=AddUnit(Side.Japan,UnitKind.NavalBase,rx,ry,0);



	// 大阪
	// 空港
	rx=-8000;
	ry=2800;
	m=AddUnit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	no=m;
	m=AddPlane(Side.Japan,UnitKind.Fighter,1,no,4,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Fighter,0,no,2,FireKind.Unarmed);

	// 都市
	rx=-8000-80*2;
	ry=2800-80*2;
	m=AddUnit(Side.Japan,UnitKind.City,rx,ry,0);

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
	rx=-8160;
	ry=1760-240;
	m=AddUnit(Side.Japan,UnitKind.NavalBase,rx,ry,0);


	// 九州の空港
	rx=-7600;
	ry=960;
	m=AddUnit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	no=m;
	m=AddPlane(Side.Japan,UnitKind.Fighter,0,no,4,FireKind.Unarmed);

	// 都市
	rx=-7600-80*9;
	ry=960-80*4;
	m=AddUnit(Side.Japan,UnitKind.City,rx,ry,0);
/***
	// 都市
	rx=-7600-80*9;
	ry=960-80*6;
	m=set_new_unit(JPN,CT1,rx,ry,0);
***/


	// 硫黄島
	// 基地
	rx=-1440;
	ry=4480;
//	m=set_new_unit(JPN,GF1,rx,ry,0);
	// 空港
	rx=-1520;
	ry=4400;
	m=AddUnit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	no=m;
	m=AddPlane(Side.Japan,UnitKind.Fighter,0,no,5,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,2,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Fighter,1,no,2,FireKind.Unarmed);


	//沖縄の日本軍基地
	rx=-5040;
	ry=-2800;
	m=AddUnit(Side.Japan,UnitKind.AirBase,rx,ry,0);
//	m=set_new_unit(JPN,GF1,rx-80,ry+80,0);


	//　台湾
	rx=-5040+80*2;
	ry=-7200+80*4;
	m=AddUnit(Side.Japan,UnitKind.Pillboxes,rx,ry,0);


//#define MAP_BOTTOM	-7200


	rx=-3500;
	ry=1500;
	m=AddUnit(Side.Japan,UnitKind.Carrier,rx,ry,0);
	no=m;
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,6,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Fighter,0,no,2,FireKind.Unarmed);
	rx-=150;
	m=AddUnit(Side.Japan,UnitKind.Carrier,rx,ry,0);
	no=m;
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,4,FireKind.Bomb);
//	m=set_new_unit_plane(JPN,FT1,0,no,4,NTG);
	rx-=150;
	m=AddUnit(Side.Japan,UnitKind.Battleship,rx,ry,0);
	rx-=150;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=150;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=150;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=150;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,0);
	rx-=150;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,0);






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
	rx=6720-80;
	ry=-3760;
	m=AddUnit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,0);
	m=AddUnit(Side.UnitedStates,UnitKind.Pillboxes,rx-80,ry+80,0);





	// 機動部隊
	rx=8000;
	ry=-6500;
	m=AddUnit(Side.UnitedStates,UnitKind.Carrier,rx,ry,180);
	no=m;
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,8,FireKind.Bomb);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,4,FireKind.Unarmed);

	rx-=150;
	m=AddUnit(Side.UnitedStates,UnitKind.LightCarrier,rx,ry,135);
	no=m;
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,6,FireKind.Bomb);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,2,FireKind.Unarmed);


	rx-=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Battleship,rx,ry,180);
	rx-=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Battleship,rx,ry,180);
	rx-=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx-=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx-=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx-=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,180);
	rx-=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,180);




	// マップの作成
	LoadScenarioFile2("Map\\Japan_off.dat");

	}




//============================================================================
//		
//----------------------------------------------------------------------------
[Original("cnct_sinario_104")]
public void	SetUpScenario104()
	{
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;
	

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

		IsDecisionEnabled=1;

		return;
		}



	//  ゲーム設定
//	cmbt_x=0;
//	cmbt_y=0;

//	rest_time=150000;



	//===============		 日本海軍		================
	/*クエゼリン環礁*/
	rx=-7120;
	ry=6640;
	m=AddUnit(Side.Japan,UnitKind.NavalBase,rx,ry,0);

	// クエゼリン航空基地
	rx=-7120-80;
	ry=6640+80;
	no=AddUnit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	m=AddPlane(Side.Japan,UnitKind.Fighter,1,no,2,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,5,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Bomber,0,no,3,FireKind.Unarmed);

	// ブーゲンビル
	rx=-4480;
	ry=1200;
	m=AddUnit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);
	rx=-4480+80*2;
	ry=1200-80*2;
	m=AddUnit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);
	rx=-4480+80*5;
	ry=1200-80*3;
	m=AddUnit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);



	// ガダルカナル
	rx=-880;
	ry=-240;
	m=AddUnit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);
	rx=-880+80;
	ry=-240;
	m=AddUnit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	rx=-880-80;
	ry=-240+80;
	m=AddUnit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);


	// ラバウル 空港
	rx=-7520;
	ry=1840;
	no=AddUnit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,3,FireKind.Bomb);
	m=AddPlane(Side.Japan,UnitKind.Bomber,0,no,1,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Fighter,1,no,2,FireKind.Unarmed);


	rx=-6500;
	ry=5000;
	m=AddUnit(Side.Japan,UnitKind.Carrier,rx,ry,0);
	no=m;
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,9,FireKind.Torpedo);
	m=AddPlane(Side.Japan,UnitKind.Fighter,0,no,3,FireKind.Unarmed);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Battleship,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,0);






	//===============		 合衆国海軍		================
	/*ヌーメア軍港*/
	rx=5440;
	ry=-5800+40;
	m=AddUnit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,0);


	/*ヌーメア航空基地*/
	rx=5280;
	ry=-5760;
	m=AddUnit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);
	no=m;
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,5,FireKind.Unarmed);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,1,no,3,FireKind.Unarmed);
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,5,FireKind.Unarmed);

	/*ヌーメア要塞*/
	rx=5360;
	ry=-5760;
	m=AddUnit(Side.UnitedStates,UnitKind.InfantryBase,rx,ry,0);


	rx=6500;
	ry=-5000;

	m=AddUnit(Side.UnitedStates,UnitKind.Carrier,rx,ry,180);
	no=m;
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,9,FireKind.Bomb);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,3,FireKind.Unarmed);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Battleship,rx,ry,180);

/***
	rx+=150;
	m=set_new_unit(USA,CA1,rx,ry,180);
	rx+=150;
	m=set_new_unit(USA,DD1,rx,ry,180);
**/



	// マップの作成
	LoadScenarioFile2("Map\\South_pacific.dat");

	}




//============================================================================
//		
//----------------------------------------------------------------------------
[Original("cnct_sinario_105")]
public void	SetUpScenario105()
	{
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;
	

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

		IsDecisionEnabled=1;

		return;
		}



	//  ゲーム設定
//	cmbt_x=0;
//	cmbt_y=0;

//	rest_time=150000;



	//===============		 日本海軍		================
	/*クエゼリン環礁*/
	rx=-7120;
	ry=6640;
	m=AddUnit(Side.Japan,UnitKind.NavalBase,rx,ry,0);

	// クエゼリン航空基地
	rx=-7120-80;
	ry=6640+80;
	no=AddUnit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	m=AddPlane(Side.Japan,UnitKind.Fighter,1,no,2,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,5,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Bomber,0,no,3,FireKind.Unarmed);


	// ブーゲンビル
	rx=-4480;
	ry=1200;
	m=AddUnit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);
	rx=-4480+80*2;
	ry=1200-80*2;
	m=AddUnit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);
	rx=-4480+80*5;
	ry=1200-80*3;
	m=AddUnit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);





	// ラバウル 空港
	rx=-7520;
	ry=1840;
	no=AddUnit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,3,FireKind.Bomb);
	m=AddPlane(Side.Japan,UnitKind.Bomber,0,no,1,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Fighter,1,no,2,FireKind.Unarmed);


	rx=-6500;
	ry=5000;
	m=AddUnit(Side.Japan,UnitKind.Carrier,rx,ry,0);
	no=m;
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,9,FireKind.Torpedo);
	m=AddPlane(Side.Japan,UnitKind.Fighter,0,no,3,FireKind.Unarmed);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Battleship,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,0);






	//===============		 合衆国海軍		================
	/*ヌーメア軍港*/
	rx=5440;
	ry=-5800+40;
	m=AddUnit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,0);


	/*ヌーメア航空基地*/
	rx=5280;
	ry=-5760;
	m=AddUnit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);
	no=m;
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,5,FireKind.Unarmed);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,1,no,3,FireKind.Unarmed);
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,5,FireKind.Unarmed);

	/*ヌーメア要塞*/
	rx=5360;
	ry=-5760;
	m=AddUnit(Side.UnitedStates,UnitKind.InfantryBase,rx,ry,0);

	// ガダルカナル
	rx=-880;
	ry=-240;
	m=AddUnit(Side.UnitedStates,UnitKind.InfantryBase,rx,ry,0);
	rx=-880+80;
	ry=-240;
	m=AddUnit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);



	rx=6500;
	ry=-5000;

	m=AddUnit(Side.UnitedStates,UnitKind.Carrier,rx,ry,180);
	no=m;
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,9,FireKind.Bomb);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,3,FireKind.Unarmed);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Battleship,rx,ry,180);

/***
	rx+=150;
	m=set_new_unit(USA,CA1,rx,ry,180);
	rx+=150;
	m=set_new_unit(USA,DD1,rx,ry,180);
**/



	// マップの作成
	LoadScenarioFile2("Map\\South_pacific.dat");

	}







//============================================================================
//		
//----------------------------------------------------------------------------
[Original("cnct_sinario_106")]
public void	SetUpScenario106()
	{
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;
	

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

		IsDecisionEnabled=1;

		return;
		}



	//  ゲーム設定
//	cmbt_x=0;
//	cmbt_y=0;

//	rest_time=150000;



	//===============		 日本海軍		================
	/*クエゼリン環礁*/
	rx=-7120;
	ry=6640;
	m=AddUnit(Side.Japan,UnitKind.NavalBase,rx,ry,0);

	// クエゼリン航空基地
	rx=-7120-80;
	ry=6640+80;
	no=AddUnit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	m=AddPlane(Side.Japan,UnitKind.Fighter,0,no,2,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,3,FireKind.Unarmed);


	rx=-7000;
	ry=5000;
	m=AddUnit(Side.Japan,UnitKind.LightCarrier,rx,ry,0);
	no=m;
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,4,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Fighter,0,no,3,FireKind.Unarmed);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Destroyer,rx,ry,0);
	rx-=120;
	m=AddUnit(Side.Japan,UnitKind.Transport,rx,ry,0);
	Units[m].Weapon=FireKind.CargoInfantryBase;		// 武装品種
	Units[m].Ammo=1;			// 数
	Units[m].MaxAmmo=1;			// 数 全容量


	// ガダルカナル
	rx=-880-160;
	ry=-240+80;
	m=AddUnit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);
	rx=-880+80-160;
	ry=-240+80;
	m=AddUnit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);
	rx=-880-160+80;
	ry=-240;
	m=AddUnit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);


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
	rx=5440;
	ry=-5800+40;
	m=AddUnit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,0);


	/*ヌーメア航空基地*/
	rx=5280;
	ry=-5760;
	m=AddUnit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);
	no=m;
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,2,FireKind.Unarmed);
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,3,FireKind.Unarmed);



	rx=6500;
	ry=-5000;
	m=AddUnit(Side.UnitedStates,UnitKind.LightCarrier,rx,ry,180);
	no=m;
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,4,FireKind.Unarmed);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,3,FireKind.Unarmed);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,180);
	rx+=150;
	m=AddUnit(Side.UnitedStates,UnitKind.Transport,rx,ry,180);
	Units[m].Weapon=FireKind.CargoInfantryBase;		// 武装品種
	Units[m].Ammo=1;			// 数
	Units[m].MaxAmmo=1;			// 数 全容量



	// ガダルカナル
	rx=-880;
	ry=-240;
	m=AddUnit(Side.UnitedStates,UnitKind.InfantryBase,rx,ry,0);
	rx=-880+80;
	ry=-240;
	m=AddUnit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);
	rx=-880+80;
	ry=-240+80;
	m=AddUnit(Side.UnitedStates,UnitKind.InfantryBase,rx,ry,0);
	rx=-880;
	ry=-240+80;
	m=AddUnit(Side.UnitedStates,UnitKind.InfantryBase,rx,ry,0);





	// マップの作成
	LoadScenarioFile2("Map\\South_pacific.dat");

	}










//============================================================================
//		
//----------------------------------------------------------------------------
[Original("cnct_sinario_999")]
public void	SetUpScenario999()
	{
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;
	

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
	rx=-7600;
	ry=-4720;
	m=AddUnit(Side.Japan,UnitKind.NavalBase,rx,ry,0);



	// 空港
	rx=-7600-80;
	ry=-4720;
	no=m=AddUnit(Side.Japan,UnitKind.AirBase,rx,ry,135);
	no=m;
	m=AddPlane(Side.Japan,UnitKind.Attacker,0,no,4,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Fighter,0,no,4,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Fighter,1,no,1,FireKind.Unarmed);
	m=AddPlane(Side.Japan,UnitKind.Bomber,0,no,1,FireKind.Unarmed);



#if false
	rx=0;
	ry=400;
	m=set_new_unit_2(Side.UnitedStates,UnitKind.Attacker,0,rx,ry,0);
	unit[m].Weapon=FireKind.Torpedo;		// 武装品種
	unit[m].Ammo=unit[m].MaxAmmo/*1*/;		// 数
	m=set_new_unit_2(Side.UnitedStates,UnitKind.Attacker,0,rx,ry+80*1,0);
	unit[m].Weapon=FireKind.Torpedo;		// 武装品種
	unit[m].Ammo=unit[m].MaxAmmo/*1*/;		// 数
	m=set_new_unit_2(Side.UnitedStates,UnitKind.Attacker,0,rx,ry+80*2,0);
	unit[m].Weapon=FireKind.Torpedo;		// 武装品種
	unit[m].Ammo=unit[m].MaxAmmo/*1*/;		// 数



	rx+=800;
	m=set_new_unit_2(Side.UnitedStates,UnitKind.Attacker,0,rx,ry,0);
	unit[m].Weapon=FireKind.Torpedo;		// 武装品種
	unit[m].Ammo=unit[m].MaxAmmo/*1*/;		// 数
	m=set_new_unit_2(Side.UnitedStates,UnitKind.Attacker,0,rx,ry+80*1,0);
	unit[m].Weapon=FireKind.Torpedo;		// 武装品種
	unit[m].Ammo=unit[m].MaxAmmo/*1*/;		// 数
	m=set_new_unit_2(Side.UnitedStates,UnitKind.Attacker,0,rx,ry+80*2,0);
	unit[m].Weapon=FireKind.Torpedo;		// 武装品種
	unit[m].Ammo=unit[m].MaxAmmo/*1*/;		// 数
#endif



#if false
	rx=-7500;
	ry=-5800;


	m=set_new_unit(Side.Japan,UnitKind.Carrier,rx,ry,0);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,9,FireKind.Torpedo);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,3,FireKind.Unarmed);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.LightCarrier,rx,ry,0);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,6,FireKind.Torpedo);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,2,FireKind.Unarmed);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Submarine,rx,ry,0);


	// 輸送船団
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Transport,rx,ry,135);
	unit[m].Weapon=FireKind.CargoInfantryBase;		// 武装品種
	unit[m].Ammo=1;			// 数
	unit[m].MaxAmmo=1;			// 数 全容量
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Transport,rx,ry,135);
	unit[m].Weapon=FireKind.CargoInfantryBase;		// 武装品種
	unit[m].Ammo=1;			// 数
	unit[m].MaxAmmo=1;			// 数 全容量
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Transport,rx,ry,135);
	unit[m].Weapon=FireKind.CargoInfantryBase;		// 武装品種
	unit[m].Ammo=1;			// 数
	unit[m].MaxAmmo=1;			// 数 全容量
#endif

#if false
	rx=10;
	ry=200;
	m=set_new_unit_2(Side.Japan,UnitKind.Submarine,0,rx,ry,0);
	rx+=700;
	m=set_new_unit_2(Side.Japan,UnitKind.Submarine,0,rx,ry,0);
#endif


	rx=0;
	ry=0;
	m=AddUnit2(Side.Japan,UnitKind.Battleship,1,rx,ry,90);
	ry+=80;
	m=AddUnit2(Side.UnitedStates,UnitKind.Submarine,0,rx,ry,90);




	//===============		 合衆国海軍		================
	// ハワイ港
	rx=7200;
	ry=-720;
	m=AddUnit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,90.0);

	// ハワイ空港
	rx=7200;
	ry=-560;
	m=AddUnit(Side.UnitedStates,UnitKind.AirBase,rx,ry,45.0);
	no=m;
	m=AddPlane(Side.UnitedStates,UnitKind.Attacker,0,no,1,FireKind.Unarmed);
	m=AddPlane(Side.UnitedStates,UnitKind.Fighter,0,no,3,FireKind.Unarmed);
	m=AddPlane(Side.UnitedStates,UnitKind.Bomber,0,no,4,FireKind.Unarmed);


#if false
	//ミッドウェイ島
	rx=80;
	ry=3440;
	m=set_new_unit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);

	decision_point[0]=m;

	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,4,FireKind.Unarmed);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,1,no,1,FireKind.Unarmed);

	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,5,FireKind.Unarmed);
	rx=80+160;
	ry=3440-80;
	m=set_new_unit(Side.UnitedStates,UnitKind.Pillboxes,rx,ry,0);


	rx=5000;
	ry=2500;



	m=set_new_unit(Side.UnitedStates,UnitKind.Carrier,rx,ry,180);
	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,9,FireKind.Bomb);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,3,FireKind.Unarmed);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Submarine,rx,ry,180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Submarine,rx,ry,180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Battleship,rx,ry,180);
#endif


#if false
	rx=0;
	ry=0;
	m=set_new_unit_2(Side.Japan,UnitKind.Cruiser,1,rx,ry,0);
//	m=set_new_unit_2(USA,CA1,1,rx,ry+80,0);
	rx+=800;
	m=set_new_unit_2(Side.Japan,UnitKind.Cruiser,0,rx,ry,0);
//	m=set_new_unit_2(USA,CA1,0,rx,ry+80,0);
#endif

#if false
	rx=5;
	ry=5;
	m=set_new_unit_2(Side.UnitedStates,UnitKind.Destroyer,1,rx,ry,0);
	rx+=700;
	m=set_new_unit_2(Side.UnitedStates,UnitKind.Destroyer,0,rx,ry,0);
#endif

	rx=700;
	ry=0;
	m=AddUnit2(Side.UnitedStates,UnitKind.Battleship,0,rx,ry,90);
	ry+=80;
	m=AddUnit2(Side.Japan,UnitKind.Submarine,0,rx,ry,90);




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





//============================================================================
//		
//----------------------------------------------------------------------------
[Original("make_map_cg")]
public void MakeTerrainSurface()
	{
	int	m,n;
	RECT	dstn_rect,src_rect;



	// マップデータから陸地をマップに描画します
	dstn_rect.left=Sprites[MAP_BASE].base_x;
	dstn_rect.top=Sprites[MAP_BASE].base_y;

	src_rect.left = Sprites[MAP_BASE].base_x+306;
	src_rect.top = Sprites[MAP_BASE].base_y;
	src_rect.right = src_rect.left+255;
	src_rect.bottom = src_rect.top+199;

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDS_OS, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,0))
		{
		RestoreSurfaces();
		}


	for(m=0; m<=179; m++) // 縦の個数	マップの枠 縦１８０ドット
		for( n=0; n<=239; n++) // 横の個数		マップの枠 横２４０ドット
			{
			if( MapTiles[m][n]!=0 )
				{

				// 陸地有り
				dstn_rect.left=Sprites[MAP_BASE].base_x+8+n-0;
				dstn_rect.top=Sprites[MAP_BASE].base_y+8+m-0;

				src_rect.left = Sprites[MAP_BASE].base_x+270;
				src_rect.top = Sprites[MAP_BASE].base_y+110;

				src_rect.right = src_rect.left+2;
				src_rect.bottom = src_rect.top+2;

				
				if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDS_OS, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,0))
					{
					RestoreSurfaces();
					}

				}
			}
	}






//============================================================================
//		
//----------------------------------------------------------------------------
[Original("cnct_game_init")]
public void	InitializeGame()
	{
	int		m,n,f;
	double	rx,ry;
	RECT	dstn_rect,src_rect;

	_DP_FLAG	dp_flag;




	// 乱数の初期化
	srand( (uint)( SharedRandomSeed ) );


	MakeSharedRandomTable();


	ScrollSpeed=0;


	// 
	for(m=0;m<=9;m++)
		UnitInfoPanel[m]=0;


#if true
	if( ScenarioNumber < 0 )
		{
		f=ScenarioNumber;
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

		WorkPathX[0]=MAP_RIGHT+1;
		SelectedUnit=0;
		PreviousSelectedUnit=0;
		SelectionCount=0;
		MaxUnitId=USA_PLANE_END;			// とりあえず最大値を入れておく
		ClearFlag=2;

		// 
		CombatMenuKind=0;
		CombatMenuSelection=CombatMenuItem.None;

		// 雲のクリア
		for(n=0; n<KUMO_MAX/*255*/; n++)
			{
			ref var cloud = ref Clouds[n];
			cloud.Used=0;
			cloud.Position = new WorldPosition(0, 0);
			cloud.Kind=0;
			}

		//最初の雲
		InitializeClouds();

		MakeTerrainSurface();

		ShowsAntiAir=0;		// 0が正常
		RevealsAll=0;		// 0が正常

		GameSpeed=1;
		Tick=0;
		FrameCount=0;
		Result=GameResult.None;


		TickChecksums[0]=0;
		TickChecksums[1]=0;

		RandomChecksums[0]=0;
		RandomChecksums[1]=0;

		RandomCount=0;


		CameraPosition = new WorldPosition(-400, 400);

		dp_flag.dwType = MessageType.SyncFlag;
		dp_flag.unit_chk=0;
		UnitChecksums[1]=0;
		dp_flag.cc_chk=(byte)Tick;
		TickChecksums[1]=(byte)Tick;
		dp_flag.rnd_chk=(byte)RandomCount;
		RandomChecksums[1]=(byte)RandomCount;
//t		lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_flag, sizeof(_DP_FLAG) );
		bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
		bufferDesc.pBufferData  = (byte*) &dp_flag;
		g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

		BufferedMoveOrders[0].Unit=0;						// クリア
		BufferedMoveOrders[1].Unit=0;						// クリア

		BufferedSelectOrders[0].IsSet=0;						// クリア
		BufferedSelectOrders[1].IsSet=0;						// クリア

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

//		load_on_resume(1);		
		switch( f )
			{
			case -1:
				LoadResume(1);		// セーブデータをロードする
				break;
			case -2:
				LoadResume(3);		// セーブデータをロードする
				break;
			}



		for(m=0; m<=255; m++)
			{
			Selections[0][m]=0;
			Selections[1][m]=0;
			}

#if SND_SW
		if( IsEditingMap==0 )
			{
			lpDSB_[SEA1][0].SetVolume( 0 );
			lpDSB_[SEA1][0].Play(0,0,DSBPLAY_LOOPING);	// ループする
			}
#endif



		return;
		}	// リジュームスタート
#endif



	
	// 全サウンドのダブリカウントクリア
	for(m=0; m<NUM_SOUND_EFFECTS; m++)
		{
		NextSoundBuffers[m]=0;
		}


	// 全エフェクトのクリア
	for(m=0; m<EFFECT_MAX/*255*/; m++)
		{
		Effects[m].Layer=EffectLayer.None;
		}

	// 全ファイアデータのクリア
	for(m=0; m<FIRE_MAX/*255*/; m++)
		{
		ref var fire = ref Fires[m];
		fire.Target=0;
		for(n=0;n<=7;n++)
			fire.info[n]=0;
		}
	MaxFireId=0;


	// 全ユニットデータのクリア
//if( sinario!=9 )
	for(m=0; m<=255; m++)
		{
		ref var unit = ref Units[m];
		unit.Side=0;
		unit.Position = new WorldPosition(0, 0);
		unit.Category=UnitCategory.None;
		unit.Kind=0;
		for(n=0;n<=15;n++)
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
		unit.Stop=0;
		unit.Supply=0;
		unit.EmergencyFlags[0]=unit.EmergencyFlags[1]=0;
		unit.EmergencyDestination=new WorldPosition(0, 0);
		//unit[m].pp_now=0;

		unit.IsGroupLeader=0;
		unit.GroupLeader=0;
		unit.FormationNumber=0;
		unit.ForGroupLeader=0;

		unit.FormationSpeed=0;

		for(n=0;n<=7;n++)
			{
			unit.hp[n]=0;
			unit.arm[n]=0;
//			unit[m].arm2[n]=0;
			unit.gas[n]=0;
			}
		Selections[0][m]=0;
		Selections[1][m]=0;
		unit.Skill=0;
		}


	WorkPathX[0]=MAP_RIGHT+1;



	SelectedUnit=0;
	PreviousSelectedUnit=0;
	SelectionCount=0;


	MaxUnitId=USA_PLANE_END;			// とりあえず最大値を入れておく


	ClearFlag=2;


	// マップのクリア マップは横120チップ、縦90チップ
//if( sinario!=9 )
	for(m=0; m<256; m++)
		for(n=0; n<256; n++)
			{
			MapTiles[m][n]=0;
			}

	CombatMenuKind=0;
	CombatMenuSelection=CombatMenuItem.None;



	// 雲のクリア
	for(n=0; n<KUMO_MAX/*255*/; n++)
		{
		ref var cloud = ref Clouds[n];
		cloud.Used=0;
		cloud.Position = new WorldPosition(0, 0);
		cloud.Kind=0;
		}

	//最初の雲
	InitializeClouds();

/*
kumo[0].used=1;
kumo[0].x=MAP_RIGHT;
kumo[0].y=0;
*/
	//
	for(n=0;n<=3;n++)
		DecisionPoints[n]=0;



//sinario=1;
//host_side=0;
	LoadScenarioData();

	if( IsEditingMap!=0 )
		{
		EditorTarget=1;
		EditorKind=(byte)UnitKind.Battleship;
		}


	MakeTerrainSurface();



	ShowsAntiAir=0;		// 0が正常
	RevealsAll=0;		// 0が正常



#if SND_SW
	if( IsEditingMap==0 )
		{
		lpDSB_[SEA1][0].SetVolume( 0 );
		lpDSB_[SEA1][0].Play(0,0,DSBPLAY_LOOPING);	// ループする
		}
#endif


	GameSpeed=1;
	Tick=0;
	FrameCount=0;

	Result=GameResult.None;

	LastTime=timeGetTime();
	LastTime2=LastTime;




//	if( cnct_game )
//		{
		TickChecksums[0]=0;
		TickChecksums[1]=0;

		RandomChecksums[0]=0;
		RandomChecksums[1]=0;

		RandomCount=0;


		//  ゲーム設定
		if(HostSide==0)
			{
			if(IsHost!=0)
				LocalSide=Side.Japan;
			else
				LocalSide=Side.UnitedStates;
			}
		else
			{
			if(IsHost!=0)
				LocalSide=Side.UnitedStates;
			else
				LocalSide=Side.Japan;
			}

		CameraPosition = new WorldPosition(-400, 400);


		dp_flag.dwType = MessageType.SyncFlag;
		dp_flag.unit_chk=0;
		UnitChecksums[1]=0;
		dp_flag.cc_chk=(byte)Tick;
		TickChecksums[1]=(byte)Tick;
		dp_flag.rnd_chk=(byte)RandomCount;
		RandomChecksums[1]=(byte)RandomCount;
		bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
		bufferDesc.pBufferData  = (byte*) &dp_flag;
		g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );



		BufferedMoveOrders[0].Unit=0;						// クリア
		BufferedMoveOrders[1].Unit=0;						// クリア

		BufferedSelectOrders[0].IsSet=0;						// クリア
		BufferedSelectOrders[1].IsSet=0;						// クリア

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

#if false
		cnct_loop=cnct_loop_ct=30;
		cnct_loop_pt1=8;
		cnct_loop_pt2=19;
#endif

#if false
cnct_loop=cnct_loop_ct=3;
cnct_loop_pt1=1;
cnct_loop_pt2=2;
#endif

		TickWaits[0]=TickWaits[1]=0;

		AutoSaveTime=0;

		if(IsHost!=0)
			SupplyPoints=InitialSupplyPoints[0];
		else
			SupplyPoints=InitialSupplyPoints[1];

		SupplyCount=0;
		SupplyTarget=0;


		BattleTime=0;


		if( IsEditingMap==0)
			SaveResume(3);


//		}


	}
}
