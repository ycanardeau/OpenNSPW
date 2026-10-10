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
public void	save_on_resume(int type)
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
		WriteFile(hFile, ref unit,(uint)(sizeof(Array256<UNIT>)),&dwActBytes,null);
		WriteFile(hFile, ref fire,(uint)(sizeof(Array512<FIRE>)),&dwActBytes,null);
		WriteFile(hFile, ref effect,(uint)(sizeof(Array1024<EFFECT>)),&dwActBytes,null);

		WriteFile(hFile, ref your_side,sizeof(short),&dwActBytes,null);
		WriteFile(hFile, ref rest_time,sizeof(int),&dwActBytes,null);

		WriteFile(hFile, ref spry_no_cont,sizeof(short),&dwActBytes,null);
		WriteFile(hFile, ref spry_trgt,sizeof(short),&dwActBytes,null);

		WriteFile(hFile, ref sinario,sizeof(short),&dwActBytes,null);
		WriteFile(hFile, ref spry_pt,sizeof(short),&dwActBytes,null);

		WriteFile(hFile, ref spry_rate,(uint)(sizeof(Array2<short>)),&dwActBytes,null);
		WriteFile(hFile, ref decision_sw,sizeof(byte),&dwActBytes,null);
		WriteFile(hFile, ref arrival_cont,sizeof(byte),&dwActBytes,null);

		WriteFile(hFile, ref you_are_host,sizeof(byte),&dwActBytes,null);
		WriteFile(hFile, ref user_sinario_fn,(uint)(sizeof(Array260<byte>)),&dwActBytes,null);


		WriteFile(hFile, ref rvrs_time,sizeof(short),&dwActBytes,null);
		WriteFile(hFile, ref rvrs_rule,sizeof(short),&dwActBytes,null);
//		WriteFile(hFile, &rvrs_time,sizeof(rvrs_time),&dwActBytes,NULL);
//		WriteFile(hFile, &rvrs_rule,sizeof(rvrs_rule),&dwActBytes,NULL);



		CloseHandle(hFile);
		}
	}






//============================================================================
// リジューム、ロードします
//----------------------------------------------------------------------------
public void	load_on_resume(int type)
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

		ReadFile( hFile, ref unit, (uint)(sizeof(Array256<UNIT>)), &dwActBytes, null );
		ReadFile( hFile, ref fire, (uint)(sizeof(Array512<FIRE>)), &dwActBytes, null );
		ReadFile( hFile, ref effect, (uint)(sizeof(Array1024<EFFECT>)), &dwActBytes, null );


		ReadFile( hFile, ref your_side, sizeof(short), &dwActBytes, null );
		ReadFile( hFile, ref rest_time, sizeof(int), &dwActBytes, null );

		ReadFile( hFile, ref spry_no_cont, sizeof(short), &dwActBytes, null );
		ReadFile( hFile, ref spry_trgt, sizeof(short), &dwActBytes, null );

		ReadFile( hFile, ref sinario, sizeof(short), &dwActBytes, null );
		ReadFile( hFile, ref spry_pt, sizeof(short), &dwActBytes, null );


		ReadFile( hFile, ref spry_rate, (uint)(sizeof(Array2<short>)), &dwActBytes, null );
		ReadFile( hFile, ref decision_sw, sizeof(byte), &dwActBytes, null );
//		ReadFile( hFile, first_spry_pt, sizeof(first_spry_pt), &dwActBytes, NULL );
		ReadFile( hFile, ref arrival_cont, sizeof(byte), &dwActBytes, null );


		ReadFile( hFile, ref you_are_host, sizeof(byte), &dwActBytes, null );
		ReadFile(hFile, ref user_sinario_fn,(uint)(sizeof(Array260<byte>)),&dwActBytes,null);




		ReadFile( hFile, ref rvrs_time, sizeof(short), &dwActBytes, null );
		ReadFile(hFile, ref rvrs_rule,sizeof(short),&dwActBytes,null);
//		WriteFile(hFile, &rvrs_time,sizeof(rvrs_time),&dwActBytes,NULL);
//		WriteFile(hFile, rvrs_rule,sizeof(rvrs_rule),&dwActBytes,NULL);


		CloseHandle(hFile);
		}


	}








//============================================================================
// デシジョン
//----------------------------------------------------------------------------
public void	cnct_decision()
	{
	int		i,f,m;
	RECT	wrk_r;


	Array5<Array128<byte>> ach = default;
	int	n; Array5<int> len = default;
	HDC					hdc;



	// 結果途中判定
	if( game_end==GameResult.None && decision_sw!=0 )
		{
		switch( sinario )
			{
			case 1:
			case 2:
			case 3:
				// 空母起動部隊の戦い
				f=0;
				for( i=1; i<=max_unit; i++ )
					{
					if( unit[i].used==Side.Japan && (unit[i].kind==UnitKind.Carrier || unit[i].kind==UnitKind.LightCarrier) )
						f++;
					}
				m=0;
				for( i=1; i<=max_unit; i++ )
					{
					if( unit[i].used==Side.Japan && (unit[i].kind==UnitKind.InfantryBase || unit[i].kind==UnitKind.Pillboxes || unit[i].kind==UnitKind.Fortress ) )
						m++;
					}
				if( f<=0 || m<=0 )
					{	
					game_end=GameResult.UnitedStatesWon;
					break;
					}





				f=0;
				for( i=1; i<=max_unit; i++)
					{
					if( unit[i].used==Side.UnitedStates && (unit[i].kind==UnitKind.Carrier || unit[i].kind==UnitKind.LightCarrier) )
						f++;
					}
				m=0;
				for( i=1; i<=max_unit; i++)
					{
					if( unit[i].used==Side.UnitedStates && (unit[i].kind==UnitKind.InfantryBase || unit[i].kind==UnitKind.Pillboxes || unit[i].kind==UnitKind.Fortress ) )
						m++;
					}
				if( f<=0 || m<=0 )
					{	
					game_end=GameResult.JapanWon;
					break;
					}
				break;


			case 4:
			case 5:
				// 艦隊決戦
				f=0;
				for( i=1; i<=max_unit; i++)
					{
					if( unit[i].used==Side.Japan && (unit[i].kind==UnitKind.Battleship || unit[i].kind==UnitKind.Cruiser) )
						f++;
					}
				m=0;
				for( i=1; i<=max_unit; i++ )
					{
					if( unit[i].used==Side.Japan && (unit[i].kind==UnitKind.InfantryBase || unit[i].kind==UnitKind.Pillboxes || unit[i].kind==UnitKind.Fortress ) )
						m++;
					}
				if( f<=0 || m<=0 )
					{	
					game_end=GameResult.UnitedStatesWon;
					break;
					}



				f=0;
				for( i=1; i<=max_unit; i++)
					{
					if( unit[i].used==Side.UnitedStates && (unit[i].kind==UnitKind.Battleship || unit[i].kind==UnitKind.Cruiser) )
						f++;
					}
				m=0;
				for( i=1; i<=max_unit; i++ )
					{
					if( unit[i].used==Side.UnitedStates && (unit[i].kind==UnitKind.InfantryBase || unit[i].kind==UnitKind.Pillboxes || unit[i].kind==UnitKind.Fortress ) )
						m++;
					}
				if( f<=0 || m<=0 )
					{	
					game_end=GameResult.JapanWon;
					break;
					}
				break;


			case 6:
				// ミッドウェイ島攻略
				m=0;
				for( i=1; i<=max_unit; i++)
					{
					if( unit[i].used==Side.UnitedStates && (unit[i].kind>=UnitKind.AirBase&&unit[i].kind<=UnitKind.Fortress) )
						{
						// ミッドウェイ島
						// ptin debg
						wrk_r.top=(int)(3440)+(80*2);//(int)(3440)-(80*4);
						wrk_r.right=(int)(80)+(80*4);
						wrk_r.bottom=(int)(3440)-(80*4);//(int)(3440)+(80*2);
						wrk_r.left=(int)(80)-(80*2);
						if( pt_in_rect3(ref wrk_r,(int)unit[i].Position.X,(int)unit[i].Position.Y)!=0)
							{
							m++;	// 生きてる基地で指定範囲内に要る
							}
						}
					}
				if( m==0 )
					{
					game_end=GameResult.JapanWon;
					break;
					}

				break;



			case 7:
				// ミッドウェイ島攻略
				f=0;
				m=0;
				for( i=1; i<=max_unit; i++)
					{
					if( unit[i].used==Side.UnitedStates && (unit[i].kind>=UnitKind.AirBase&&unit[i].kind<=UnitKind.Fortress) )
						{
						// ミッドウェイ島
						// ptin_dbg
						wrk_r.top=(int)(3440)+(80*2);//(int)(3440)-(80*4);
						wrk_r.right=(int)(80)+(80*4);
						wrk_r.bottom=(int)(3440)-(80*4);//(int)(3440)+(80*2);
						wrk_r.left=(int)(80)-(80*2);
						if( pt_in_rect3(ref wrk_r,(int)unit[i].Position.X,(int)unit[i].Position.Y)!=0)
							{
							m++;	// 生きてる基地で指定範囲内に要る
							}
						}
					}
				for( i=1; i<=max_unit; i++)
					{
					if( unit[i].used==Side.Japan && (unit[i].kind==UnitKind.Fortress) && unit[i].info[0]==0 )
						{
						// ミッドウェイ島
						// ptin dbg
						wrk_r.top=(int)(3440)+(80*2);//(int)(3440)-(80*4);
						wrk_r.right=(int)(80)+(80*4);
						wrk_r.bottom=(int)(3440)-(80*4);//(int)(3440)+(80*2);
						wrk_r.left=(int)(80)-(80*2);
						if( pt_in_rect3(ref wrk_r,(int)unit[i].Position.X,(int)unit[i].Position.Y)!=0)
							{
							f++;	// 生きてる基地で指定範囲内に要る
							}
						}
					}
				if( m==0 && f!=0)
					{
					game_end=GameResult.JapanWon;
					break;
					}

				break;



			case 8:
				// 中部太平洋の戦い
				f=0;
				m=0;
				for( i=1; i<=max_unit; i++)
					{
					if( unit[i].used==Side.UnitedStates && (unit[i].kind>=UnitKind.AirBase&&unit[i].kind<=UnitKind.Fortress) )
						{
						// ミッドウェイ島
						// ptin dbg
						wrk_r.top=(int)(3440)+(80*2);//(int)(3440)-(80*4);
						wrk_r.right=(int)(80)+(80*4);
						wrk_r.bottom=(int)(3440)-(80*4);//(int)(3440)+(80*2);
						wrk_r.left=(int)(80)-(80*2);
						if( pt_in_rect3(ref wrk_r,(int)unit[i].Position.X,(int)unit[i].Position.Y)!=0)
							{
							m++;	// 生きてる基地で指定範囲内に要る
							}
						}
					}
				for( i=1; i<=max_unit; i++)
					{
					if( unit[i].used==Side.Japan && (unit[i].kind==UnitKind.Fortress) && unit[i].info[0]==0 )
						{
						// ミッドウェイ島
						// ptin_dbg
						wrk_r.top=(int)(3440)+(80*2);//(int)(3440)-(80*4);
						wrk_r.right=(int)(80)+(80*4);
						wrk_r.bottom=(int)(3440)-(80*4);//(int)(3440)+(80*2);
						wrk_r.left=(int)(80)-(80*2);
						if( pt_in_rect3(ref wrk_r,(int)unit[i].Position.X,(int)unit[i].Position.Y)!=0)
							{
							f++;	// 生きてる基地で指定範囲内に要る
							}
						}
					}
				if( m==0 && f!=0)
					{
					game_end=GameResult.JapanWon;
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
				for( i=1; i<=max_unit; i++)
					{
					if( unit[i].used==Side.Japan && (unit[i].kind>=UnitKind.AirBase&&unit[i].kind<=UnitKind.Fortress) )
						{
						// ウェーク
						wrk_r.top=(int)(-720+80);
						wrk_r.right=(int)(-4080+80);
						wrk_r.bottom=(int)(-720-160);
						wrk_r.left=(int)(-4080-80);


						if( pt_in_rect2(ref wrk_r,(int)unit[i].Position.X,(int)unit[i].Position.Y)!=0)
							{
							m++;	// 生きてる基地で指定範囲内に要る
							}
						}
					}
				for( i=1; i<=max_unit; i++)
					{
					if( unit[i].used==Side.UnitedStates && (unit[i].kind==UnitKind.Fortress) && unit[i].info[0]==0 )
						{
						// ウェーク
						wrk_r.top=(int)(-720+80);
						wrk_r.right=(int)(-4080+80);
						wrk_r.bottom=(int)(-720-160);
						wrk_r.left=(int)(-4080-80);

						if( pt_in_rect2(ref wrk_r,(int)unit[i].Position.X,(int)unit[i].Position.Y)!=0)
							{
							f++;	// 生きてる基地で指定範囲内に要る
							}
						}
					}
				if( m==0 && f!=0)
					{
					game_end=GameResult.UnitedStatesWon;
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
				for( i=1; i<=max_unit; i++)
					{
					if( unit[i].used==Side.Japan && ( unit[i].kind>=UnitKind.AirBase && unit[i].kind<=UnitKind.Fortress ) && pt_in_rect2(ref wrk_r,(int)unit[i].Position.X,(int)unit[i].Position.Y)!=0 )
						{
						f++;
						}
					}

				if( f==0 )
					{
					game_end=GameResult.UnitedStatesWon;
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
				for( i=1; i<=max_unit; i++)
					{
					if( unit[i].used==Side.Japan && ( unit[i].kind>=UnitKind.AirBase && unit[i].kind<=UnitKind.Fortress ) && pt_in_rect2(ref wrk_r,(int)unit[i].Position.X,(int)unit[i].Position.Y)!=0 )
						{
						f++;
						}
					}
				for( i=1; i<=max_unit; i++)
					{
					if( unit[i].used==Side.UnitedStates && ( unit[i].kind==UnitKind.AirBase )  && unit[i].info[0]==0 && pt_in_rect2(ref wrk_r,(int)unit[i].Position.X,(int)unit[i].Position.Y)!=0 )
						{
						m++;
						}
					}

				if( f==0 && m>=2)
					{
					game_end=GameResult.UnitedStatesWon;
					break;
					}

				break;


			case 103:
				f=0;
				for( i=1; i<=max_unit; i++)
					{
					if( unit[i].used==Side.Japan && unit[i].kind==UnitKind.City  )
						{
						f++;
						}
					}

				if( f==0 )
					{
					game_end=GameResult.UnitedStatesWon;
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
				for( i=1; i<=max_unit; i++)
					{
					if( unit[i].used==Side.Japan && ( unit[i].kind>=UnitKind.AirBase && unit[i].kind<=UnitKind.Fortress ) && pt_in_rect2(ref wrk_r,(int)unit[i].Position.X,(int)unit[i].Position.Y)!=0 )
						{
						f++;
						}
					}


				if( f==0 )
					{
					game_end=GameResult.UnitedStatesWon;
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
				for( i=1; i<=max_unit; i++)
					{
					if( unit[i].used==Side.Japan && ( unit[i].kind>=UnitKind.AirBase && unit[i].kind<=UnitKind.Fortress ) && pt_in_rect2(ref wrk_r,(int)unit[i].Position.X,(int)unit[i].Position.Y)!=0 )
						{
						f++;
						}
					}
				if( f==0 )
					{
					game_end=GameResult.UnitedStatesWon;
					break;
					}

				f=0;
				// ガダルカナル島
				wrk_r.top=(int)(-160);
				wrk_r.right=(int)(-1040+(80*3));
				wrk_r.bottom=(int)(-160-80);
				wrk_r.left=(int)(-1040);
				for( i=1; i<=max_unit; i++)
					{
					if( unit[i].used==Side.UnitedStates && ( unit[i].kind>=UnitKind.AirBase && unit[i].kind<=UnitKind.Fortress ) && pt_in_rect2(ref wrk_r,(int)unit[i].Position.X,(int)unit[i].Position.Y)!=0 )
						{
						f++;
						}
					}

				if( f==0 )
					{
					game_end=GameResult.JapanWon;
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

				for( i=1; i<=max_unit; i++)
					{
					if( unit[i].used==Side.Japan && ( unit[i].kind>=UnitKind.AirBase && unit[i].kind<=UnitKind.Fortress ) && pt_in_rect2(ref wrk_r,(int)unit[i].Position.X,(int)unit[i].Position.Y)!=0 )
						{
						f++;	//　日本の施設
						}
					if( unit[i].used==Side.UnitedStates && ( unit[i].kind>=UnitKind.AirBase && unit[i].kind<=UnitKind.Fortress ) && pt_in_rect2(ref wrk_r,(int)unit[i].Position.X,(int)unit[i].Position.Y)!=0 )
						{
						m++;	//　米の施設
						}
					}

				if( f>=4 && m==0 )
					{
					game_end=GameResult.JapanWon;
					break;
					}
				if( f==0 && m>=4 )
					{
					game_end=GameResult.UnitedStatesWon;
					break;
					}
				break;



			case 995:
				// ミッドウェイを巡る戦い１
				if( unit[decision_point[0]].used==0 )
					{
					game_end=GameResult.JapanWon;
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
	if( game_end!=GameResult.None )
		{
#if true
		if ( IDirectDrawSurface_GetDC(lpDDSBack, &hdc) == DD_OK ) 
			{
			SetBkMode(hdc, TRANSPARENT);
			SelectObject(hdc, gameFont_1);


#if !LNGG_VER
			switch( game_end )
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
public void	set_unit_data(int m)
	{
	int		i;


	switch( unit[m].kind )
		{
		case UnitKind.Battleship:
			unit[m].a_drctn_add=0.3;
			unit[m].a_spd_add=0.01;
			unit[m].min_spd=0.0;
			unit[m].max_spd=0.7;

			unit[m].os_indx_y=0;

			unit[m].Weapon=GUN;		// 武装品種
			unit[m].Ammo=1000;		// 数
			unit[m].MaxAmmo=1000;		// 数 全容量


			unit[m].Fuel=100;		// 残燃料
			unit[m].FuelInterval=1000;		// 燃料を消費するタイミング

			unit[m].tech=5;			

			unit[m].Hp=unit[m].MaxHp=BB1_HP;		// Ｈｐ

			if( unit[m].used==Side.Japan && unit[m].type==1 )
				{
				unit[m].a_drctn_add*=0.9;

				unit[m].a_spd_add*=0.9;
				unit[m].max_spd*=0.9;

				unit[m].Weapon=SP_GUN;		// 武装品種
				unit[m].Ammo=(int)(unit[m].Ammo * 1.35);		// 数
				unit[m].MaxAmmo=(int)(unit[m].MaxAmmo * 1.35);		// 数 全容量

				unit[m].FuelInterval*=1.2;		// 残燃料

				unit[m].Hp=unit[m].MaxHp=unchecked((int)(BB1_HP*1.4));		// Ｈｐ
				}
			break;

		case UnitKind.Cruiser:
			unit[m].a_drctn_add=0.5;
			unit[m].a_spd_add=0.01;
			unit[m].min_spd=0.0;
			unit[m].max_spd=0.8;

			unit[m].os_indx_y=1;

			unit[m].Weapon=GUN;		// 武装品種

			if( unit[m].type!=0  )
				{
				if( unit[m].used==Side.Japan )
					{
					unit[m].Ammo=450;		// 数
					unit[m].MaxAmmo=450;		// 数 全容量
					}
				else
					{
					unit[m].Ammo=500;		// 数
					unit[m].MaxAmmo=500;		// 数 全容量
					}
				}
			else
				{
				unit[m].Ammo=600;		// 数
				unit[m].MaxAmmo=600;		// 数 全容量
				}



			unit[m].Fuel=100;		// 残燃料
			unit[m].FuelInterval=650;		// 燃料を消費するタイミング

			unit[m].tech=5;			

			unit[m].Hp=unit[m].MaxHp=CA1_HP;		// Ｈｐ
			if( unit[m].type!=0 )
				{
				if( unit[m].used==Side.UnitedStates )
					unit[m].MaxHp=(int)(unit[m].MaxHp * 0.9);
				else
					unit[m].MaxHp=(int)(unit[m].MaxHp * 0.8);
				unit[m].Hp=unit[m].MaxHp;
				}
			break;

		case UnitKind.Destroyer:
			unit[m].a_drctn_add=1.3;
			unit[m].a_spd_add=0.05;
			unit[m].min_spd=0.0;
			unit[m].max_spd=1.00;

			unit[m].os_indx_y=2;

			unit[m].Weapon=GUN;		// 武装品種
			unit[m].Ammo=120;		// 数
			unit[m].MaxAmmo=120;		// 数 全容量

			unit[m].Fuel=100;		// 残燃料
			unit[m].FuelInterval=550;		// 燃料を消費するタイミング

			unit[m].tech=5;			

			unit[m].Hp=unit[m].MaxHp=DD1_HP;		// Ｈｐ
			if( unit[m].type!=0 )
				{
				if( unit[m].used==Side.UnitedStates )
					unit[m].MaxHp=(int)(unit[m].MaxHp * 0.9);
				else
					unit[m].MaxHp=(int)(unit[m].MaxHp * 0.7);
				unit[m].Hp=unit[m].MaxHp;
				}
			break;

		case UnitKind.Submarine:
			unit[m].a_drctn_add=0.5;
			unit[m].a_spd_add=0.02;
			unit[m].min_spd=0.0;
			unit[m].max_spd=0.5;

			unit[m].os_indx_y=3;

			unit[m].Weapon=GUN;		// 武装品種
			unit[m].Ammo=25;		// 数
			unit[m].MaxAmmo=25;		// 数 全容量

			unit[m].Fuel=100;		// 残燃料
			unit[m].FuelInterval=1000;		// 燃料を消費するタイミング

			unit[m].tech=5;			

			unit[m].Hp=unit[m].MaxHp=SS1_HP;		// Ｈｐ
			break;

		case UnitKind.Carrier:
			unit[m].a_drctn_add=0.3;
			unit[m].a_spd_add=0.01;
			unit[m].min_spd=0.0;
			unit[m].max_spd=0.7;

			unit[m].os_indx_y=6;

			unit[m].info[0]=0;					// 
			unit[m].info[1]=plane_in_cv(m);		// 現在収容数(飛行甲板上数も含む)
			unit[m].info[2]=12;					// 最大収容数
			unit[m].info[3]=0;					// 
			unit[m].info[4]=0;					// 発進予定機数 ０なら着艦可
			unit[m].info[5]=MOVE;				// モード（コンバットメニュー）

			unit[m].Weapon=GUN;	//0;		// 武装品種
			unit[m].Ammo=100;		// 数
			unit[m].MaxAmmo=100;		// 数 全容量

			unit[m].Fuel=100;		// 残燃料
			unit[m].FuelInterval=750;		// 燃料を消費するタイミング

			unit[m].tech=5;			

			unit[m].Hp=unit[m].MaxHp=CV1_HP+((unit[m].used==Side.UnitedStates ? 1 : 0)*5);		// Ｈｐ

			if( unit[m].used==Side.UnitedStates && unit[m].type==1 )
				{
				unit[m].a_drctn_add*=0.9;

				unit[m].a_spd_add*=0.9;
				unit[m].max_spd*=0.9;

				unit[m].info[2]=14;					// 最大収容数

				unit[m].Weapon=GUN;		//0;		// 武装品種
				unit[m].Ammo=(int)(unit[m].Ammo * 1.1);		// 数
				unit[m].MaxAmmo=(int)(unit[m].MaxAmmo * 1.1);		// 数 全容量

				unit[m].FuelInterval*=1.6;		// 残燃料

				unit[m].Hp=(int)(unit[m].Hp * 1.15);
				unit[m].MaxHp=(int)(unit[m].MaxHp * 1.15);		// Ｈｐ
				}
			break;


		case UnitKind.LightCarrier:
			unit[m].a_drctn_add=0.5;
			unit[m].a_spd_add=0.01;
			unit[m].min_spd=0.0;
			unit[m].max_spd=0.9;

			unit[m].os_indx_y=5;

			unit[m].info[0]=0;					// 
			unit[m].info[1]=plane_in_cv(m);		// 現在収容数(飛行甲板上数も含む)
			unit[m].info[2]=8;					// 最大収容数
			unit[m].info[3]=0;					// 
			unit[m].info[4]=0;					// 発進予定機数 ０なら着艦可
			unit[m].info[5]=MOVE;				// モード（コンバットメニュー）

			unit[m].Weapon=GUN;	//0;		// 武装品種
			unit[m].Ammo=80;		// 数
			unit[m].MaxAmmo=80;		// 数 全容量

			unit[m].Fuel=100;		// 残燃料
			unit[m].FuelInterval=700;		// 燃料を消費するタイミング

			unit[m].tech=5;			

			unit[m].Hp=unit[m].MaxHp=CVL1_HP+((unit[m].used==Side.UnitedStates ? 1 : 0)*5);		// Ｈｐ
			break;


		case UnitKind.Fighter:
			switch( unit[m].type )
				{
				case 0:		// 艦上戦闘機

					if(unit[m].used==Side.Japan)
						{
						unit[m].a_drctn_add=4.0;
						unit[m].a_spd_add=0.01;
						unit[m].min_spd=0.5;
						unit[m].max_spd=2.35;

						if( unit[m].stop!=0 )
							unit[m].os_indx_y=8;
						else
							unit[m].os_indx_y=7;

						unit[m].Weapon=BLT;		// 武装品種
						unit[m].Ammo=35;		// 数
						unit[m].MaxAmmo=35;		// 数 全容量

						unit[m].Fuel=100;		// 残燃料
						unit[m].FuelInterval=80;		// 燃料を消費するタイミング

						unit[m].tech=7;			

						unit[m].Hp=unit[m].MaxHp=FT1_HP;		// Ｈｐ
						}
					else
						{
						unit[m].a_drctn_add=2.2;
						unit[m].a_spd_add=0.01;
						unit[m].min_spd=0.5;
						unit[m].max_spd=2.5;

						if( unit[m].stop!=0 )
							unit[m].os_indx_y=8;
						else
							unit[m].os_indx_y=7;

						unit[m].Weapon=BLT;		// 武装品種
						unit[m].Ammo=40;		// 数
						unit[m].MaxAmmo=40;		// 数 全容量

						unit[m].Fuel=100;		// 残燃料
						unit[m].FuelInterval=60;		// 燃料を消費するタイミング

						unit[m].tech=5;			

						unit[m].Hp=unit[m].MaxHp=FT1_HP+4;		// Ｈｐ
						}

					break;


				case 1:		// 陸上戦闘機
#if true

					if(unit[m].used==Side.Japan)
						{
						unit[m].a_drctn_add=1.8;
						unit[m].a_spd_add=0.008;
						unit[m].min_spd=0.5;
						unit[m].max_spd=2.35;

						unit[m].os_indx_y=14;

						unit[m].Weapon=BLT;		// 武装品種
						unit[m].Ammo=50;		// 数
						unit[m].MaxAmmo=50;		// 数 全容量

						unit[m].Fuel=100;		// 残燃料
						unit[m].FuelInterval=100;		// 燃料を消費するタイミング

						unit[m].tech=5;			

						unit[m].Hp=unit[m].MaxHp=unchecked((int)(FT1_HP*0.8));		// Ｈｐ
						}
					else
						{
						unit[m].a_drctn_add=2.0;
						unit[m].a_spd_add=0.02;
						unit[m].min_spd=0.5;
						unit[m].max_spd=2.7;

						unit[m].os_indx_y=14;

						unit[m].Weapon=BLT;		// 武装品種
						unit[m].Ammo=60;		// 数
						unit[m].MaxAmmo=60;		// 数 全容量

						unit[m].Fuel=100;		// 残燃料
						unit[m].FuelInterval=95;		// 燃料を消費するタイミング

						unit[m].tech=5;			

						unit[m].Hp=unit[m].MaxHp=unchecked((int)(FT1_HP*2.0));		// Ｈｐ
						}


#else
					if(unit[m].used==Side.Japan)
						{
						unit[m].a_drctn_add=3.0;
						unit[m].a_spd_add=0.03;
						unit[m].min_spd=0.5;
						unit[m].max_spd=3.0;

						unit[m].os_indx_y=14;

						unit[m].Weapon=BLT;		// 武装品種
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

						unit[m].Weapon=BLT;		// 武装品種
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
			if(unit[m].used==Side.Japan)
				{	
				unit[m].a_drctn_add=3.0;
				unit[m].a_spd_add=0.01;
				unit[m].min_spd=0.5;
				unit[m].max_spd=2.2;

				if( unit[m].stop!=0 )
					unit[m].os_indx_y=10;
				else
					unit[m].os_indx_y=9;

				unit[m].Weapon=NTG;		// 武装品種
				unit[m].Ammo=0;		// 数
				unit[m].MaxAmmo=1;		// 数 全容量

				unit[m].Fuel=100;		// 残燃料
				unit[m].FuelInterval=85;		// 燃料を消費するタイミング

				unit[m].tech=5;			

				unit[m].Hp=unit[m].MaxHp=AT1_HP;		// Ｈｐ
				}
			else
				{
				unit[m].a_drctn_add=3.0;
				unit[m].a_spd_add=0.01;
				unit[m].min_spd=0.5;
				unit[m].max_spd=2.2;

				if( unit[m].stop!=0 )
					unit[m].os_indx_y=10;
				else
					unit[m].os_indx_y=9;

				unit[m].Weapon=NTG;		// 武装品種
				unit[m].Ammo=0;		// 数
				unit[m].MaxAmmo=1;		// 数 全容量

				unit[m].Fuel=100;		// 残燃料
				unit[m].FuelInterval=70;		// 燃料を消費するタイミング

				unit[m].tech=5;			

				unit[m].Hp=unit[m].MaxHp=AT1_HP+4;		// Ｈｐ
				}

			break;



		case UnitKind.Bomber:
			if(unit[m].used==Side.Japan)
				{
				unit[m].a_drctn_add=2.4;
				unit[m].a_spd_add=0.005;
				unit[m].min_spd=0.5;
				unit[m].max_spd=1.8;

				unit[m].os_indx_y=12;

				unit[m].Weapon=NTG;		// 武装品種
				unit[m].Ammo=0;		// 数
				unit[m].MaxAmmo=9;		// 数 全容量

				unit[m].Fuel=100;		// 残燃料
				unit[m].FuelInterval=120;		// 燃料を消費するタイミング

				unit[m].tech=5;			

				unit[m].Hp=unit[m].MaxHp=unchecked((int)(BM1_HP*0.65));		// Ｈｐ
				}
			else
				{	
				unit[m].a_drctn_add=2.0;
				unit[m].a_spd_add=0.005;
				unit[m].min_spd=0.5;
				unit[m].max_spd=1.9;

				unit[m].os_indx_y=12;

				unit[m].Weapon=NTG;		// 武装品種
				unit[m].Ammo=0;		// 数
				unit[m].MaxAmmo=20;		// 数 全容量

				unit[m].Fuel=100;		// 残燃料
				unit[m].FuelInterval=220;		// 燃料を消費するタイミング

				unit[m].tech=5;			

				unit[m].Hp=unit[m].MaxHp=BM1_HP;		// Ｈｐ
				}
			break;	



		case UnitKind.Transport:
			unit[m].a_drctn_add=0.3;
			unit[m].a_spd_add=0.01;
			unit[m].min_spd=0.0;
			unit[m].max_spd=0.65;

			unit[m].os_indx_y=13;

			unit[m].Weapon=NTG;		// 武装品種
			unit[m].Ammo=0;		// 数
			unit[m].MaxAmmo=0;		// 数 全容量

			unit[m].Fuel=100;		// 残燃料
			unit[m].FuelInterval=1000;		// 燃料を消費するタイミング

			unit[m].tech=5;			

			unit[m].Hp=unit[m].MaxHp=TR1_HP;		// Ｈｐ
			break;


		case UnitKind.NavalBase:
			unit[m].os_indx_y=11;

			unit[m].tech=5;			

			unit[m].Hp=unit[m].MaxHp=SP_HP;		// Ｈｐ

			unit[m].drctn=90.0;					// ９０がos_indx_x=0;
			break;
		case UnitKind.AirBase:
			unit[m].os_indx_y=11;
			unit[m].info[0]=0;					// 
			unit[m].info[1]=plane_in_cv(m);		// 現在収容数(飛行甲板上数も含む)
			unit[m].info[2]=16;					// 最大収容数
			unit[m].info[3]=0;					// 
			unit[m].info[4]=0;					// 発進予定機数 ０なら着艦可
			unit[m].info[5]=MOVE;				// モード（コンバットメニュー）

			unit[m].tech=5;			

			unit[m].Hp=unit[m].MaxHp=AP_HP;		// Ｈｐ


			unit[m].drctn=90.0-45.0;					// ９０がos_indx_x=0;

			break;



		case UnitKind.City:
			unit[m].os_indx_y=11;

			unit[m].Weapon=0;		// 武装品種
			unit[m].Ammo=0;		// 数
			unit[m].MaxAmmo=0;		// 数 全容量

			unit[m].drctn=0.0;					// ９０がos_indx_x=0;

			unit[m].tech=5;			
			unit[m].Hp=unit[m].MaxHp=CT1_HP;		// Ｈｐ
			break;



		case UnitKind.InfantryBase:
			unit[m].os_indx_y=11;

			unit[m].Weapon=GUN;		// 武装品種
			unit[m].Ammo=700;		// 数
			unit[m].MaxAmmo=700;		// 数 全容量

			unit[m].drctn=225.0;					// ９０がos_indx_x=0;

			unit[m].tech=5;			
			unit[m].Hp=unit[m].MaxHp=GF1_HP;		// Ｈｐ
			break;



		case UnitKind.Pillboxes:
			unit[m].os_indx_y=11;

			unit[m].Weapon=GUN;		// 武装品種
			unit[m].Ammo=1500;		// 数
			unit[m].MaxAmmo=1500;		// 数 全容量

			unit[m].drctn=180.0;					// ９０がos_indx_x=0;

			unit[m].tech=5;			
			unit[m].Hp=unit[m].MaxHp=GF2_HP;		// Ｈｐ
			break;
		case UnitKind.Fortress:
			unit[m].os_indx_y=11;

			unit[m].Weapon=GUN;		// 武装品種
			unit[m].Ammo=2000;		// 数
			unit[m].MaxAmmo=2000;		// 数 全容量

			unit[m].drctn=135.0;					// ９０がos_indx_x=0;

			unit[m].tech=5;			
			unit[m].Hp=unit[m].MaxHp=GF3_HP;		// Ｈｐ
			break;
		}



	//unit[m].hp[0]=10;		// 現在のＨｐ
	//unit[m].hp[1]=10;		// 最高Ｈｐ

	// 各ユニットの乱数データをセットします。
	for( i=0; i<=1; i++)
		{
		unit[m].rnd_250[i]=(short)rnd(250);
		unit[m].rnd_225[i]=(short)rnd(225);

		unit[m].rnd_200[i]=(short)rnd(200);
		unit[m].rnd_175[i]=(short)rnd(175);
		unit[m].rnd_150[i]=(short)rnd(150);
		unit[m].rnd_125[i]=(short)rnd(125);

		unit[m].rnd_100[i]=(short)rnd(100);
		unit[m].rnd_80[i]=(short)rnd(80);
		unit[m].rnd_65[i]=(short)rnd(65);
		unit[m].rnd_50[i]=(short)rnd(50);
		unit[m].rnd_40[i]=(short)rnd(40);
		unit[m].rnd_30[i]=(short)rnd(30);
		unit[m].rnd_20[i]=(short)rnd(20);
		unit[m].rnd_10[i]=(short)rnd(10);
		}


	unit[m].pp_x[0]=unit[m].Position.X;
	unit[m].pp_y[0]=unit[m].Position.Y;
	unit[m].pp_x[1]=MAP_RIGHT+1;
	//unit[m].max_spd*=1.0;
	}







//============================================================================
//		
//----------------------------------------------------------------------------
public int		set_new_unit(Side side,UnitKind kind,double rx,double ry,double drctn)
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
		if( unit[m].used==0 )
			{
			// まずクリア
			unit[m].used=0;
			unit[m].Position = new WorldPosition(0, 0);
			unit[m].ctgry=UnitCategory.None;
			unit[m].kind=0;
			unit[m].type=0;
			for(n=0;n<=15;n++)
				unit[m].info[n]=0;

			unit[m].os_indx_y=0;
			unit[m].os_indx_x=0;
			unit[m].drctn=0;
			unit[m].drctn_add=0;
			unit[m].spd=0;
			unit[m].spd_add=0;
			unit[m].max_spd=0;
			unit[m].min_spd=0;
			unit[m].a_spd_add=0;
			unit[m].spd=0;
			unit[m].stop=0;
			unit[m].spry=0;
			unit[m].em_flg[0]=unit[m].em_flg[1]=0;
			unit[m].EmergencyDestination=new WorldPosition(0, 0);
			//unit[m].pp_now=0;

			unit[m].is_ltl_ldr=0;
			unit[m].ltl_ldr=0;
			unit[m].no=0;
			unit[m].for_ltl_ldr=0;

			unit[m].for_form_spd=0;

			for(n=0;n<=7;n++)
				{
				unit[m].hp[n]=0;
				unit[m].arm[n]=0;
//				unit[m].arm2[n]=0;
				unit[m].gas[n]=0;
				}
			slct_unit[0][m]=0;
			slct_unit[1][m]=0;
			unit[m].tech=0;


			// あきスペース発見
			unit[m].used=side;
			unit[m].Position = new WorldPosition(rx, ry);
			unit[m].ctgry=ctgry;
			unit[m].kind=kind;
			unit[m].drctn=drctn;
			unit[m].spd=0;
			unit[m].em_flg[0]=0;
			set_unit_data(m);
			return(m);
			}
		}
	return(0);

	}





//============================================================================
//		
//----------------------------------------------------------------------------
public int		set_new_unit_2(Side side,UnitKind kind,int type,double rx,double ry,double drctn)
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
		if( unit[m].used==0 )
			{
			// まずクリア
			unit[m].used=0;
			unit[m].Position = new WorldPosition(0, 0);
			unit[m].ctgry=UnitCategory.None;
			unit[m].kind=0;
			unit[m].type=(short)type;
			for(n=0;n<=15;n++)
				unit[m].info[n]=0;

			unit[m].os_indx_y=0;
			unit[m].os_indx_x=0;
			unit[m].drctn=0;
			unit[m].drctn_add=0;
			unit[m].spd=0;
			unit[m].spd_add=0;
			unit[m].max_spd=0;
			unit[m].min_spd=0;
			unit[m].a_spd_add=0;
			unit[m].spd=0;
			unit[m].stop=0;
			unit[m].spry=0;
			unit[m].em_flg[0]=unit[m].em_flg[1]=0;
			unit[m].EmergencyDestination=new WorldPosition(0, 0);
			//unit[m].pp_now=0;

			unit[m].is_ltl_ldr=0;
			unit[m].ltl_ldr=0;
			unit[m].no=0;
			unit[m].for_ltl_ldr=0;

			unit[m].for_form_spd=0;

			for(n=0;n<=7;n++)
				{
				unit[m].hp[n]=0;
				unit[m].arm[n]=0;
//				unit[m].arm2[n]=0;
				unit[m].gas[n]=0;
				}
			slct_unit[0][m]=0;
			slct_unit[1][m]=0;
			unit[m].tech=0;


			// あきスペース発見
			unit[m].used=side;
			unit[m].Position = new WorldPosition(rx, ry);
			unit[m].ctgry=ctgry;
			if(ctgry==UnitCategory.Plane)
				unit[m].PlaneState=UnitState.Flying;
			unit[m].kind=kind;
			unit[m].drctn=drctn;
			unit[m].spd=0;
			unit[m].em_flg[0]=0;
			set_unit_data(m);
			return(m);
			}
		}
	return(0);

	}





//============================================================================
//		
//----------------------------------------------------------------------------
public int		set_new_unit_plane(Side side,UnitKind kind,int type,int no,int planes,int arm)
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
		if( unit[m].used==0 )
			{
			// あきスペース発見


			for( n=0; n<24; n++)
				{
				space[n]=0;
				}
			park=0;
			for(n=1;n<=max_unit;n++)
				{
				if( unit[n].used!=0 && unit[n].ctgry==UnitCategory.Plane && unit[n].info[1]==no && unit[n].PlaneState==UnitState.Parked )
					{
					space[unit[n].info[2]]=1;
					park++;
					}
				}	


			if( park >= unit[no].info[2] )
				return (-planes);



			for( n=0; n<24; n++)
				{
				if(space[n]==0)
					{
					f=n;				// 格納庫の位置、及び、その基地の番機番号
					break;
					}
				}


			unit[m].used=side;
			unit[m].Position = new WorldPosition(730, 150);
			unit[m].ctgry=UnitCategory.Plane;
			unit[m].kind=kind;
			unit[m].type=(short)type;
			unit[m].PlaneState=UnitState.Parked;
			unit[m].info[1]=no;					// 所属の空母、及び、基地の番号
			unit[m].info[2]=f;				// 格納庫の位置、及び、その基地の番機番号
			unit[m].info[3]=0;					// 8
			unit[m].info[4]=0;					// 発艦予定の機数
			unit[m].info[5]=MOVE;				// モード（コンバットメニュー）
			set_pos_of_parking(m);
			unit[m].stop=1;

			unit[unit[m].info[1]].info[1]++;					// 所属の空母、及び、基地の格納数を増やす｡


			set_unit_data(m);


			if( kind!=UnitKind.Fighter  )
				{
				if( arm==NTG )
					{
					unit[m].Weapon=arm;		// 武装品種
					unit[m].Ammo=0;		// 数
					//unit[m].arm[4]=1;		// 数
					}
				else
					{
					unit[m].Weapon=arm;		// 武装品種
					unit[m].Ammo=unit[m].MaxAmmo/*1*/;		// 数
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
public void	cnct_sinario_1()
	{
	int		m,no;
	double	rx,ry;
//	int		tf_no,unit_no;
	



	if(mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ

		spry_rate[0]=0;		// Host
		spry_rate[1]=0;		// Guest

		first_spry_pt[0]=0;		// Host
		first_spry_pt[1]=0;		// Guest

		rvrs_rule=0;
		rvrs_time=0;

		arrival_cont=1;
		decision_sw=1;

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
	set_new_unit(Side.Japan,UnitKind.NavalBase,rx,ry,(double)0);


	/*クエゼリン*/
	rx=-7120-80;
	ry=6720;
	set_new_unit(Side.Japan,UnitKind.InfantryBase,rx,ry,(double)0);



	rx=-2500;
	ry=2500;
//rx=0;
	m=set_new_unit(Side.Japan,UnitKind.Carrier,rx,ry,(double)0);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,9,TPD);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,3,NTG);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);




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
	m=set_new_unit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,(double)0);

	/*ヌーメア*/
	rx=5440-80;
	ry=-5760;
	m=set_new_unit(Side.UnitedStates,UnitKind.InfantryBase,rx,ry,(double)0);



	rx=2500;
	ry=-2500;

//rx=0;
//ry=2500;

	m=set_new_unit(Side.UnitedStates,UnitKind.Carrier,rx,ry,(double)180);
	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,9,BOM);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,3,NTG);

	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);

	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);

	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);


	// マップの作成
	load_it2("Map\\South_pacific.dat");

	}






//============================================================================
//		
//----------------------------------------------------------------------------
public void	cnct_sinario_2()
	{
	int		m,no;
	double	rx,ry;
//	int		tf_no,unit_no;


	if(mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ

		spry_rate[0]=0;		// Host
		spry_rate[1]=0;		// Guest

		first_spry_pt[0]=0;		// Host
		first_spry_pt[1]=0;		// Guest

		rvrs_rule=0;
		rvrs_time=0;

		arrival_cont=1;
		decision_sw=1;


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
	m=set_new_unit(Side.Japan,UnitKind.NavalBase,rx,ry,(double)0);

	// トラック島
	rx=-7600-80;
	ry=-4720;
	m=set_new_unit(Side.Japan,UnitKind.InfantryBase,rx,ry,(double)0);


	rx=-5000;
	ry=-2500;

	m=set_new_unit(Side.Japan,UnitKind.Carrier,rx,ry,0);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,9,TPD);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,3,NTG);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.LightCarrier,rx,ry,(double)0);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,6,TPD);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,2,NTG);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);




	//===============		 合衆国海軍		================
	// ハワイ港
	rx=7200;
	ry=-720;
	m=set_new_unit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,(double)90.0);


	// ハワイ
	rx=7200-80;
	ry=-720+80;
	m=set_new_unit(Side.UnitedStates,UnitKind.InfantryBase,rx,ry,(double)90.0);


	rx=5000;
	ry=2500;

	m=set_new_unit(Side.UnitedStates,UnitKind.Carrier,rx,ry,(double)180);
	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,9,BOM);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,3,NTG);

	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.LightCarrier,rx,ry,(double)180);
	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,6,BOM);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,2,NTG);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);




	// マップの作成
	load_it2("Map\\Middle_pacific.dat");

	}









//============================================================================
//		
//----------------------------------------------------------------------------
public void	cnct_sinario_3()
	{
	int		m,n,no;
	double	rx,ry;
//	int		tf_no,unit_no;
	

	if(mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ

		// 増援設定などの設定のみ
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
		spry_rate[m]=3;
		first_spry_pt[m]=50;

		//合衆国海軍側
		spry_rate[n]=3;
		first_spry_pt[n]=50;

		rvrs_rule=0;
		rvrs_time=0;

		arrival_cont=5;

		decision_sw=1;

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
	m=set_new_unit(Side.Japan,UnitKind.NavalBase,rx,ry,(double)0);

	// 横須賀
	rx=-7440-80;
	ry=4880+80;
	m=set_new_unit(Side.Japan,UnitKind.InfantryBase,rx,ry,(double)0);



	rx=-6500;
	ry=5000;

	m=set_new_unit(Side.Japan,UnitKind.Carrier,rx,ry,(double)0);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,9,TPD);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,3,NTG);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);


	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Battleship,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);



	//===============		 合衆国海軍		================
	/*ヌーメア軍港*/
//	rx=5440;
//	ry=-5800;
//	m=set_new_unit(USA,SP,rx,ry,(double)0);

	// パラオ
	rx=6720-80;
	ry=-3760;
	m=set_new_unit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,(double)0);

	// パラオ
	rx=6720-80;
	ry=-3760+80;
	m=set_new_unit(Side.UnitedStates,UnitKind.InfantryBase,rx,ry,(double)0);


	rx=6500;
	ry=-5000;

	m=set_new_unit(Side.UnitedStates,UnitKind.Carrier,rx,ry,(double)180);
	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,9,BOM);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,3,NTG);

	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);

	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);

	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);



	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Battleship,rx,ry,(double)180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);



	// マップの作成
//	load_it2("Map\\South_pacific.dat");
	load_it2("Map\\Japan_off.dat");

	}







//============================================================================
//		
//----------------------------------------------------------------------------
public void	cnct_sinario_4()
	{
	int		m,n,no;
	double	rx,ry;
//	int		tf_no,unit_no;
	

	// 艦隊決戦１

	// 南太平洋
	

	if(mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ

		spry_rate[0]=0;		// Host
		spry_rate[1]=0;		// Guest

		first_spry_pt[0]=0;		// Host
		first_spry_pt[1]=0;		// Guest

		rvrs_rule=0;
		rvrs_time=0;

		arrival_cont=1;
		decision_sw=1;

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
	set_new_unit(Side.Japan,UnitKind.NavalBase,rx,ry,(double)0);


	// クエゼリン
	rx=-7120-80;
	ry=6720;
	set_new_unit(Side.Japan,UnitKind.InfantryBase,rx,ry,(double)0);


	rx=-2500;
	ry=2500;

	m=set_new_unit(Side.Japan,UnitKind.Battleship,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);



	//===============		 合衆国海軍		================

	/*ヌーメア軍港*/
	rx=5440;
	ry=-5760;
	m=set_new_unit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,(double)0);


	// ヌーメア
	rx=5440-80;
	ry=-5760-80;
	m=set_new_unit(Side.UnitedStates,UnitKind.InfantryBase,rx,ry,(double)0);



	rx=2500;
	ry=-2500;

	m=set_new_unit(Side.UnitedStates,UnitKind.Battleship,rx,ry,(double)180);
	rx+=120;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);
	rx+=120;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);
	rx+=120;
	m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);
	rx+=120;
	m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);
	rx+=120;
	m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);



	// マップの作成
	load_it2("Map\\South_pacific.dat");

	}







//============================================================================
//		
//----------------------------------------------------------------------------
public void	cnct_sinario_5()
	{
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;
	

	// 艦隊決戦２

	// 南太平洋
	

	if(mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ
		// 増援設定などの設定のみ
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
		spry_rate[m]=8;
		first_spry_pt[m]=0;

		//合衆国海軍側
		spry_rate[n]=8;
		first_spry_pt[n]=0;

		rvrs_rule=0;
		rvrs_time=0;

		arrival_cont=3;		// ３：輸送船のみ可

		decision_sw=1;

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
	set_new_unit(Side.Japan,UnitKind.NavalBase,rx,ry,(double)0);

	// クエゼリン環礁
	rx=-7120-80;
	ry=6720;
	set_new_unit(Side.Japan,UnitKind.InfantryBase,rx,ry,(double)0);


	rx=-2500;
	ry=2500;

	m=set_new_unit(Side.Japan,UnitKind.Battleship,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Battleship,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,(double)0);

	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Submarine,rx,ry,(double)0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Submarine,rx,ry,(double)0);


	//===============		 合衆国海軍		================

	/*ヌーメア軍港*/
	rx=5440;
	ry=-5760;
	m=set_new_unit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,(double)0);

	// ヌーメア軍港
	rx=5440-80;
	ry=-5760-80;
	m=set_new_unit(Side.UnitedStates,UnitKind.InfantryBase,rx,ry,(double)0);

	rx=2500;
	ry=-2500;
//rx=0;
//ry=2500;
	m=set_new_unit(Side.UnitedStates,UnitKind.Battleship,rx,ry,(double)180);
	rx+=120;
	m=set_new_unit(Side.UnitedStates,UnitKind.Battleship,rx,ry,(double)180);
	rx+=120;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);
	rx+=120;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);
	rx+=120;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,(double)180);
	rx+=120;
	m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);
	rx+=120;
	m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);
	rx+=120;
	m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);
	rx+=120;
	m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,(double)180);

	rx+=120;
	m=set_new_unit(Side.UnitedStates,UnitKind.Submarine,rx,ry,(double)180);
	rx+=120;
	m=set_new_unit(Side.UnitedStates,UnitKind.Submarine,rx,ry,(double)180);


	// マップの作成
	load_it2("Map\\South_pacific.dat");

	}







//============================================================================
//		
//----------------------------------------------------------------------------
public void	cnct_sinario_6()
	{
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;
	

	// ミッドウェイ島攻略１
	// 中部太平洋
	if(mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ
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
		spry_rate[m]=10;
		first_spry_pt[m]=1500;

		//合衆国海軍側
		spry_rate[n]=5;
		first_spry_pt[n]=500;

		rvrs_rule=0;
		rvrs_time=0;

		arrival_cont=0;

		decision_sw=1;

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
	m=set_new_unit(Side.Japan,UnitKind.NavalBase,rx,ry,135);

	// 空港
	rx=-7600-80;
	ry=-4720;
	no=m=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,135);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,5,BOM);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,3,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,1,no,1,BOM);


	// 戦闘艦船
	rx=-7450;
	ry=-4550;
	m=set_new_unit(Side.Japan,UnitKind.Battleship,rx-50,ry,135);
	unit[m].Fuel*=0.1;
	unit[m].Ammo=(int)(unit[m].MaxAmmo*0.2);			// 数
	unit[m].spry=1;
	ry-=150;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,135);
	unit[m].Fuel*=0.2;
	unit[m].Ammo=(int)(unit[m].MaxAmmo*0.1);			// 数
	unit[m].spry=1;
	ry-=150;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,135);
	unit[m].Fuel*=0.1;
	unit[m].Ammo=(int)(unit[m].MaxAmmo*0.2);			// 数
	unit[m].spry=1;
	ry-=150;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx-80,ry,135);
	unit[m].Fuel*=0.1;
	unit[m].Ammo=(int)(unit[m].MaxAmmo*0.3);			// 数
	unit[m].spry=1;


	// 輸送船団
	rx=-7340;
	ry=-4690;
//rx=-800;
//ry=5500;
	no=m=set_new_unit(Side.Japan,UnitKind.Transport,rx,ry,270);
	unit[m].Weapon=TR_GF1;		// 武装品種
	unit[m].Ammo=1;			// 数
	unit[m].MaxAmmo=1;			// 数 全容量
	unit[m].Fuel*=0.2;
	unit[m].spry=1;
	ry-=150;
	no=m=set_new_unit(Side.Japan,UnitKind.Transport,rx-20,ry,135);
	unit[m].Weapon=TR_GF2;		// 武装品種
	unit[m].Ammo=1;			// 数
	unit[m].MaxAmmo=1;			// 数 全容量
	unit[m].Fuel*=0;
	unit[m].spry=1;
	ry-=150;
	no=m=set_new_unit(Side.Japan,UnitKind.Transport,rx,ry,90);
	unit[m].Weapon=TR_GF2;		// 武装品種
	unit[m].Ammo=1;			// 数
	unit[m].MaxAmmo=1;			// 数 全容量
	unit[m].Fuel*=0;
	unit[m].spry=1;
	ry-=150;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx+90,ry-40,90);

	// 潜水艦
	rx=-7800;
	ry=-2000;
	m=set_new_unit(Side.Japan,UnitKind.Submarine,rx,ry,0);
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
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,6,BOM);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,8,NTG);


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
m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,12,BOM);
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
	m=set_new_unit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,90.0);

	// ハワイ島 基地
	rx=7200-80;
	ry=-640+80*2;
	m=set_new_unit(Side.UnitedStates,UnitKind.Fortress,rx,ry,0);

	// ハワイ空港
	rx=7200;
	ry=-560;
	m=set_new_unit(Side.UnitedStates,UnitKind.AirBase,rx,ry,45.0);
	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,1,NTG);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,3,NTG);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Bomber,0,no,4,NTG);



	// ミッドウェイ島 基地
	rx=80+160;
	ry=3440-80;
	m=set_new_unit(Side.UnitedStates,UnitKind.Pillboxes,rx,ry,0);


	// ミッドウェイ島航空基地 1
	rx=80;
	ry=3440;
	no=set_new_unit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,2,NTG);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,2,NTG);

	// 機動部隊
	rx=-3500;
//rx=-6500;
	ry=-3000;
	no=m=set_new_unit(Side.UnitedStates,UnitKind.Carrier,rx,ry,180);
	unit[m].Fuel/=2;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,10,BOM);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,2,NTG);
	rx+=150;
	no=m=set_new_unit(Side.UnitedStates,UnitKind.LightCarrier,rx,ry,180);
	unit[m].Fuel/=2;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,6,BOM);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,2,NTG);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	unit[m].Fuel/=2;
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,180);
	unit[m].Fuel/=2;
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	unit[m].Fuel/=2;





	// マップの作成
	load_it2("Map\\Middle_pacific.dat");

	}





//============================================================================
//		
//----------------------------------------------------------------------------
public void	cnct_sinario_7()
	{
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;


	// ミッドウェイ島攻略２
	// 中部太平洋
	if(mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ
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
		spry_rate[m]=12;
		first_spry_pt[m]=1500;

		//合衆国海軍側
		spry_rate[n]=5;
		first_spry_pt[n]=500;

		rvrs_rule=0;
		rvrs_time=0;

		arrival_cont=0;

		decision_sw=1;

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
	m=set_new_unit(Side.Japan,UnitKind.NavalBase,rx,ry,135);


	// 空港
	rx=-7600-80;
	ry=-4720;
	no=m=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,135);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,5,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,3,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,1,no,1,NTG);


	// 戦闘艦船
	rx=-7450;
	ry=-4550;
	m=set_new_unit(Side.Japan,UnitKind.Battleship,rx-50,ry,135);
	unit[m].Fuel*=0.1;
	unit[m].Ammo=(int)(unit[m].MaxAmmo*0.2);			// 数
	unit[m].spry=1;
	ry-=150;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,135);
	unit[m].Fuel*=0.2;
	unit[m].Ammo=(int)(unit[m].MaxAmmo*0.1);			// 数
	unit[m].spry=1;
	ry-=150;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,135);
	unit[m].Fuel*=0.1;
	unit[m].Ammo=(int)(unit[m].MaxAmmo*0.2);			// 数
	unit[m].spry=1;
	ry-=150;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx-80,ry,135);
	unit[m].Fuel*=0.1;
	unit[m].Ammo=(int)(unit[m].MaxAmmo*0.3);			// 数
	unit[m].spry=1;


	// 輸送船団
	rx=-7340;
	ry=-4690;
//rx=-800;
//ry=5500;
	no=m=set_new_unit(Side.Japan,UnitKind.Transport,rx,ry,270);
	unit[m].Weapon=TR_GF1;		// 武装品種
	unit[m].Ammo=1;			// 数
	unit[m].MaxAmmo=1;			// 数 全容量
	unit[m].Fuel*=0.2;
	unit[m].spry=1;
	ry-=150;
	no=m=set_new_unit(Side.Japan,UnitKind.Transport,rx-20,ry,135);
	unit[m].Weapon=TR_GF2;		// 武装品種
	unit[m].Ammo=1;			// 数
	unit[m].MaxAmmo=1;			// 数 全容量
	unit[m].Fuel*=0;
	unit[m].spry=1;
	ry-=150;
	no=m=set_new_unit(Side.Japan,UnitKind.Transport,rx,ry,90);
	unit[m].Weapon=TR_GF2;		// 武装品種
	unit[m].Ammo=1;			// 数
	unit[m].MaxAmmo=1;			// 数 全容量
	unit[m].Fuel*=0;
	unit[m].spry=1;
	ry-=150;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx+90,ry-40,90);

	// 潜水艦
	rx=-7800;
	ry=-2000;
	m=set_new_unit(Side.Japan,UnitKind.Submarine,rx,ry,0);

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
	m=set_new_unit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,90.0);

	// ハワイ島 基地
	rx=7200-80;
	ry=-640+80*2;
	m=set_new_unit(Side.UnitedStates,UnitKind.Fortress,rx,ry,0);

	// ハワイ空港
	rx=7200;
	ry=-560;
	m=set_new_unit(Side.UnitedStates,UnitKind.AirBase,rx,ry,45.0);
	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,1,no,3,NTG);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Bomber,0,no,2,NTG);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,4,NTG);



	// ミッドウェイ島 基地
	rx=80+160;
	ry=3440-80;
	m=set_new_unit(Side.UnitedStates,UnitKind.Pillboxes,rx,ry,0);


	// ミッドウェイ島航空基地 1
	rx=80;
	ry=3440;
	no=set_new_unit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,2,NTG);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,2,NTG);

	// 機動部隊
	rx=-3500;
	ry=-3000;
	no=m=set_new_unit(Side.UnitedStates,UnitKind.Carrier,rx,ry,180);
	unit[m].Fuel/=2;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,10,BOM);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,2,NTG);
	rx+=150;
	no=m=set_new_unit(Side.UnitedStates,UnitKind.LightCarrier,rx,ry,180);
	unit[m].Fuel/=2;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,6,BOM);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,2,NTG);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	unit[m].Fuel/=2;
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,180);
	unit[m].Fuel/=2;
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	unit[m].Fuel/=2;





	// マップの作成
	load_it2("Map\\Middle_pacific.dat");

	}






//============================================================================
//		
//----------------------------------------------------------------------------
public void	cnct_sinario_8()
	{
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;
	

	// 中部太平洋の戦い
	// 中部太平洋
	if(mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ
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
		spry_rate[m]=8;
		first_spry_pt[m]=2000;

		//合衆国海軍側
		spry_rate[n]=14;
		first_spry_pt[n]=800;

		rvrs_rule=0;
		rvrs_time=0;

		arrival_cont=0;

		decision_sw=1;

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
	m=set_new_unit(Side.Japan,UnitKind.NavalBase,rx,ry,135);


	// 空港
	rx=-7600-80;
	ry=-4720;
	no=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,135);
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,3,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,3,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,1,no,2,NTG);


	// ウェーク
	rx=-4080;
	ry=-720;
	m=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,180);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,6,BOM);
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,3,NTG);

	rx=-4080+80;
	ry=-720-160;
	m=set_new_unit(Side.Japan,UnitKind.InfantryBase,rx,ry,180);



	// 機動部隊
	rx=-7400;
	ry=-4200;
	no=set_new_unit(Side.Japan,UnitKind.Carrier,rx,ry,90);
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,10,BOM);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,2,NTG);
	ry-=90;
	no=set_new_unit(Side.Japan,UnitKind.LightCarrier,rx,ry,90);
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,0,BOM);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,3,no,0,NTG);
	ry-=90;
	m=set_new_unit(Side.Japan,UnitKind.Battleship,rx,ry,90);
	ry-=90;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,90);
	ry-=90;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,90);
	ry-=90;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,90);


	// 戦闘艦船
	rx=-7500;
	ry=-4550;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,45);
	ry-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx+40,ry,135);
	ry-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx+20,ry,270);


	// 潜水艦
	rx=-7800;
	ry=-5000;
	m=set_new_unit(Side.Japan,UnitKind.Submarine,rx,ry,45);





	//===============		 合衆国海軍		================
	// ハワイ港	
	rx=7200;
	ry=-640;
	m=set_new_unit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,90.0);
	// ハワイ島 基地
	rx=7200;
	ry=-640+80;
	m=set_new_unit(Side.UnitedStates,UnitKind.Fortress,rx,ry,0);
	// ハワイ島 空港
	rx=7200+80;
	ry=-640+80+80;
	no=set_new_unit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,5,NTG);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,3,NTG);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Bomber,0,no,2,NTG);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,1,no,2,NTG);



	// ミッドウェイ島 基地
	rx=80+160;
	ry=3440-80;
	m=set_new_unit(Side.UnitedStates,UnitKind.Pillboxes,rx,ry,0);


	// ミッドウェイ島航空基地 1
	rx=80;
	ry=3440;
	no=set_new_unit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,2,NTG);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,2,NTG);



	// 機動部隊
	rx=3500;
	ry=3000;
	no=set_new_unit(Side.UnitedStates,UnitKind.Carrier,rx,ry,180);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,10,BOM);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,2,NTG);
	rx+=150;
	no=set_new_unit(Side.UnitedStates,UnitKind.LightCarrier,rx,ry,180);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,6,BOM);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,2,NTG);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);



	// 艦隊
	rx=7200+140;
	ry=-640+80;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,45);
	rx+=120;
	m=set_new_unit(Side.UnitedStates,UnitKind.Battleship,rx,ry,45);
	rx+=120;
	m=set_new_unit(Side.UnitedStates,UnitKind.Submarine,rx,ry,45);



	// マップの作成
	load_it2("Map\\Middle_pacific.dat");

	}






//============================================================================
//		
//----------------------------------------------------------------------------
public void	cnct_sinario_9()
	{
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;
	short		data;	

	HANDLE	hFile;
	Array256<Array256<ushort>> szBuf = default;					// マップ





	if(mode==GameMode.GameSetting)
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
		load_user_map();


		if(host_side==0)
			{
			}
		else
			{
			data=first_spry_pt[0];
			first_spry_pt[0]=first_spry_pt[1];
			first_spry_pt[1]=data;

			data=spry_rate[0];
			spry_rate[0]=spry_rate[1];
			spry_rate[1]=data;
			}
		return;
		}
	else
		{
		load_it3();		

//		load_it2( user_sinario_fn);
//		load_user_map();
		}


//	if( map_edit )
//		{
//		put_trgt=1;
//		put_kind=BB1;
//		}

	map_now=3;

	// マップの作成
//	load_it2("Map\\user_map.dat");

	}









//============================================================================
//		
//----------------------------------------------------------------------------
public void	cnct_sinario_101()
	{
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;
	

	// ガダルカナル島を巡る戦い
	if(mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ
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
		spry_rate[m]=5;
		first_spry_pt[m]=2500;

		//合衆国海軍側
		spry_rate[n]=12;
		first_spry_pt[n]=850;

		rvrs_rule=0;
		rvrs_time=0;

		arrival_cont=0;

		decision_sw=1;
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
	m=set_new_unit(Side.Japan,UnitKind.NavalBase,rx,ry,0);

	// クエゼリン航空基地
	rx=-7120-80;
	ry=6640+80;
	no=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,1,no,2,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,5,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Bomber,0,no,3,NTG);

	// ラバウル 空港
	rx=-7520;
	ry=1840;
	no=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,3,BOM);
	m=set_new_unit_plane(Side.Japan,UnitKind.Bomber,0,no,1,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,1,no,2,NTG);

	// ガダルカナル
	rx=-880;
	ry=-240;
	m=set_new_unit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);
	rx=-880+80;
	ry=-240;
	m=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,0);

	// 機動部隊
	rx=-6500;
	ry=4500;
	m=set_new_unit(Side.Japan,UnitKind.Carrier,rx,ry,0);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,3,TPD);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,3,NTG);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Battleship,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Battleship,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,0);




	//===============		 合衆国海軍		================
	/*ヌーメア軍港*/
	rx=5440;
	ry=-5800+40;
	m=set_new_unit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,0);


	/*ヌーメア航空基地*/
	rx=5280;
	ry=-5760;
	m=set_new_unit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);
	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,4,NTG);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,8,NTG);

	/*ヌーメア要塞*/
	rx=5360;
	ry=-5760;
	m=set_new_unit(Side.UnitedStates,UnitKind.Pillboxes,rx,ry,0);

	// 機動部隊
	rx=6500;
	ry=-5000;
	m=set_new_unit(Side.UnitedStates,UnitKind.Carrier,rx,ry,180);
	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,9,BOM);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,3,NTG);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Battleship,rx,ry,180);






	// マップの作成
	load_it2("Map\\South_pacific.dat");

	}








//============================================================================
//		
//----------------------------------------------------------------------------
public void	cnct_sinario_102()
	{
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;
	

	// ガダルカナル島を巡る戦い
	if(mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ
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
		spry_rate[m]=5;
		first_spry_pt[m]=2500;

		//合衆国海軍側
		spry_rate[n]=12;
		first_spry_pt[n]=850;

		rvrs_rule=0;
		rvrs_time=0;

		arrival_cont=0;

		decision_sw=1;

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
	m=set_new_unit(Side.Japan,UnitKind.NavalBase,rx,ry,0);

	// クエゼリン航空基地
	rx=-7120-80;
	ry=6640+80;
	no=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,1,no,2,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,5,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Bomber,0,no,3,NTG);

	// ラバウル 空港
	rx=-7520;
	ry=1840;
	no=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,2,BOM);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,5,NTG);

	// ガダルカナル
	rx=-880;
	ry=-240;
	m=set_new_unit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);
	rx=-880+80;
	ry=-240;
	m=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	rx=-880-80;
	ry=-240+80;
	m=set_new_unit(Side.Japan,UnitKind.Pillboxes,rx,ry,0);

	// 機動部隊
	rx=-6500;
	ry=5000;
	m=set_new_unit(Side.Japan,UnitKind.Carrier,rx,ry,0);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,3,TPD);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,3,NTG);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Battleship,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Submarine,rx,ry,0);


	//===============		 合衆国海軍		================
	/*ヌーメア軍港*/
	rx=5440;
	ry=-5800+40;
	m=set_new_unit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,0);


	/*ヌーメア航空基地*/
	rx=5280;
	ry=-5760;
	m=set_new_unit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);
	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,4,NTG);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,8,NTG);

	/*ヌーメア要塞*/
	rx=5360;
	ry=-5760;
	m=set_new_unit(Side.UnitedStates,UnitKind.Pillboxes,rx,ry,0);

	// 機動部隊
	rx=6500;
	ry=-5000;
	m=set_new_unit(Side.UnitedStates,UnitKind.Carrier,rx,ry,180);
	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,9,BOM);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,3,NTG);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Battleship,rx,ry,180);








	// マップの作成
	load_it2("Map\\South_pacific.dat");

	}



//============================================================================
//		
//----------------------------------------------------------------------------
public void	cnct_sinario_103()
	{
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;
	

	// 日本近海の戦い
	if(mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ
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
		spry_rate[m]=5;
		first_spry_pt[m]=3000;

		//合衆国海軍側
		spry_rate[n]=20;
		first_spry_pt[n]=850;

		rvrs_rule=0;
		rvrs_time=0;

		arrival_cont=0;

		decision_sw=1;

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
	m=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,2,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,2,NTG);
	// 都市
	rx=-7440-80*4;
	ry=5440-80*2;
	m=set_new_unit(Side.Japan,UnitKind.City,rx,ry,0);
/****	
	// 都市
	rx=-7440-80*3;
	ry=5440-80*4;
	m=set_new_unit(JPN,CT1,rx,ry,0);
****/

	// 都市
	rx=-7440-80*2;
	ry=5440-80*5;
	m=set_new_unit(Side.Japan,UnitKind.City,rx,ry,0);
/***
	// 都市
	rx=-7440-80*3;
	ry=5440-80*3;
	m=set_new_unit(JPN,CT1,rx,ry,0);
***/
	// 都市
	rx=-7440-80*3;
	ry=5440-80*1;
	m=set_new_unit(Side.Japan,UnitKind.City,rx,ry,0);

	// 基地
	rx=-7440-80*4;
	ry=5440-80*3;
	m=set_new_unit(Side.Japan,UnitKind.Pillboxes,rx,ry,0);


	// 横須賀
	// 港
	rx=-7440;
	ry=4880;
	m=set_new_unit(Side.Japan,UnitKind.NavalBase,rx,ry,0);



	// 大阪
	// 空港
	rx=-8000;
	ry=2800;
	m=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,1,no,4,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,2,NTG);

	// 都市
	rx=-8000-80*2;
	ry=2800-80*2;
	m=set_new_unit(Side.Japan,UnitKind.City,rx,ry,0);

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
	m=set_new_unit(Side.Japan,UnitKind.NavalBase,rx,ry,0);


	// 九州の空港
	rx=-7600;
	ry=960;
	m=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,4,NTG);

	// 都市
	rx=-7600-80*9;
	ry=960-80*4;
	m=set_new_unit(Side.Japan,UnitKind.City,rx,ry,0);
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
	m=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,5,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,2,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,1,no,2,NTG);


	//沖縄の日本軍基地
	rx=-5040;
	ry=-2800;
	m=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,0);
//	m=set_new_unit(JPN,GF1,rx-80,ry+80,0);


	//　台湾
	rx=-5040+80*2;
	ry=-7200+80*4;
	m=set_new_unit(Side.Japan,UnitKind.Pillboxes,rx,ry,0);


//#define MAP_BOTTOM	-7200


	rx=-3500;
	ry=1500;
	m=set_new_unit(Side.Japan,UnitKind.Carrier,rx,ry,0);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,6,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,2,NTG);
	rx-=150;
	m=set_new_unit(Side.Japan,UnitKind.Carrier,rx,ry,0);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,4,BOM);
//	m=set_new_unit_plane(JPN,FT1,0,no,4,NTG);
	rx-=150;
	m=set_new_unit(Side.Japan,UnitKind.Battleship,rx,ry,0);
	rx-=150;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=150;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=150;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=150;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,0);
	rx-=150;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,0);






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
	m=set_new_unit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,0);
	m=set_new_unit(Side.UnitedStates,UnitKind.Pillboxes,rx-80,ry+80,0);





	// 機動部隊
	rx=8000;
	ry=-6500;
	m=set_new_unit(Side.UnitedStates,UnitKind.Carrier,rx,ry,180);
	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,8,BOM);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,4,NTG);

	rx-=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.LightCarrier,rx,ry,135);
	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,6,BOM);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,2,NTG);


	rx-=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Battleship,rx,ry,180);
	rx-=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Battleship,rx,ry,180);
	rx-=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx-=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx-=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx-=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,180);
	rx-=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,180);




	// マップの作成
	load_it2("Map\\Japan_off.dat");

	}




//============================================================================
//		
//----------------------------------------------------------------------------
public void	cnct_sinario_104()
	{
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;
	

	//南太平洋の戦い１
	if(mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ
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
		spry_rate[m]=8;
		first_spry_pt[m]=2500;

		//合衆国海軍側
		spry_rate[n]=15;
		first_spry_pt[n]=500;

		rvrs_rule=0;
		rvrs_time=0;

		arrival_cont=0;

		decision_sw=1;

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
	m=set_new_unit(Side.Japan,UnitKind.NavalBase,rx,ry,0);

	// クエゼリン航空基地
	rx=-7120-80;
	ry=6640+80;
	no=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,1,no,2,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,5,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Bomber,0,no,3,NTG);

	// ブーゲンビル
	rx=-4480;
	ry=1200;
	m=set_new_unit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);
	rx=-4480+80*2;
	ry=1200-80*2;
	m=set_new_unit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);
	rx=-4480+80*5;
	ry=1200-80*3;
	m=set_new_unit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);



	// ガダルカナル
	rx=-880;
	ry=-240;
	m=set_new_unit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);
	rx=-880+80;
	ry=-240;
	m=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	rx=-880-80;
	ry=-240+80;
	m=set_new_unit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);


	// ラバウル 空港
	rx=-7520;
	ry=1840;
	no=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,3,BOM);
	m=set_new_unit_plane(Side.Japan,UnitKind.Bomber,0,no,1,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,1,no,2,NTG);


	rx=-6500;
	ry=5000;
	m=set_new_unit(Side.Japan,UnitKind.Carrier,rx,ry,0);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,9,TPD);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,3,NTG);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Battleship,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,0);






	//===============		 合衆国海軍		================
	/*ヌーメア軍港*/
	rx=5440;
	ry=-5800+40;
	m=set_new_unit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,0);


	/*ヌーメア航空基地*/
	rx=5280;
	ry=-5760;
	m=set_new_unit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);
	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,5,NTG);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,1,no,3,NTG);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,5,NTG);

	/*ヌーメア要塞*/
	rx=5360;
	ry=-5760;
	m=set_new_unit(Side.UnitedStates,UnitKind.InfantryBase,rx,ry,0);


	rx=6500;
	ry=-5000;

	m=set_new_unit(Side.UnitedStates,UnitKind.Carrier,rx,ry,180);
	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,9,BOM);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,3,NTG);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Battleship,rx,ry,180);

/***
	rx+=150;
	m=set_new_unit(USA,CA1,rx,ry,180);
	rx+=150;
	m=set_new_unit(USA,DD1,rx,ry,180);
**/



	// マップの作成
	load_it2("Map\\South_pacific.dat");

	}




//============================================================================
//		
//----------------------------------------------------------------------------
public void	cnct_sinario_105()
	{
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;
	

	//南太平洋の戦い２
	if(mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ
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
		spry_rate[m]=8;
		first_spry_pt[m]=2750;

		//合衆国海軍側
		spry_rate[n]=18;
		first_spry_pt[n]=400;

		rvrs_rule=0;
		rvrs_time=0;

		arrival_cont=0;

		decision_sw=1;

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
	m=set_new_unit(Side.Japan,UnitKind.NavalBase,rx,ry,0);

	// クエゼリン航空基地
	rx=-7120-80;
	ry=6640+80;
	no=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,1,no,2,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,5,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Bomber,0,no,3,NTG);


	// ブーゲンビル
	rx=-4480;
	ry=1200;
	m=set_new_unit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);
	rx=-4480+80*2;
	ry=1200-80*2;
	m=set_new_unit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);
	rx=-4480+80*5;
	ry=1200-80*3;
	m=set_new_unit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);





	// ラバウル 空港
	rx=-7520;
	ry=1840;
	no=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,3,BOM);
	m=set_new_unit_plane(Side.Japan,UnitKind.Bomber,0,no,1,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,1,no,2,NTG);


	rx=-6500;
	ry=5000;
	m=set_new_unit(Side.Japan,UnitKind.Carrier,rx,ry,0);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,9,TPD);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,3,NTG);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Battleship,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,0);






	//===============		 合衆国海軍		================
	/*ヌーメア軍港*/
	rx=5440;
	ry=-5800+40;
	m=set_new_unit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,0);


	/*ヌーメア航空基地*/
	rx=5280;
	ry=-5760;
	m=set_new_unit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);
	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,5,NTG);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,1,no,3,NTG);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,5,NTG);

	/*ヌーメア要塞*/
	rx=5360;
	ry=-5760;
	m=set_new_unit(Side.UnitedStates,UnitKind.InfantryBase,rx,ry,0);

	// ガダルカナル
	rx=-880;
	ry=-240;
	m=set_new_unit(Side.UnitedStates,UnitKind.InfantryBase,rx,ry,0);
	rx=-880+80;
	ry=-240;
	m=set_new_unit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);



	rx=6500;
	ry=-5000;

	m=set_new_unit(Side.UnitedStates,UnitKind.Carrier,rx,ry,180);
	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,9,BOM);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,3,NTG);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Battleship,rx,ry,180);

/***
	rx+=150;
	m=set_new_unit(USA,CA1,rx,ry,180);
	rx+=150;
	m=set_new_unit(USA,DD1,rx,ry,180);
**/



	// マップの作成
	load_it2("Map\\South_pacific.dat");

	}







//============================================================================
//		
//----------------------------------------------------------------------------
public void	cnct_sinario_106()
	{
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;
	

	// ガ島争奪戦
	if(mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ
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
		spry_rate[m]=8;
		first_spry_pt[m]=180;

		//合衆国海軍側
		spry_rate[n]=8;
		first_spry_pt[n]=180;

		rvrs_rule=0;
		rvrs_time=0;

		arrival_cont=0;

		decision_sw=1;

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
	m=set_new_unit(Side.Japan,UnitKind.NavalBase,rx,ry,0);

	// クエゼリン航空基地
	rx=-7120-80;
	ry=6640+80;
	no=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,0);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,2,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,3,NTG);


	rx=-7000;
	ry=5000;
	m=set_new_unit(Side.Japan,UnitKind.LightCarrier,rx,ry,0);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,4,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,3,NTG);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Cruiser,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Destroyer,rx,ry,0);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Transport,rx,ry,0);
	unit[m].Weapon=TR_GF1;		// 武装品種
	unit[m].Ammo=1;			// 数
	unit[m].MaxAmmo=1;			// 数 全容量


	// ガダルカナル
	rx=-880-160;
	ry=-240+80;
	m=set_new_unit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);
	rx=-880+80-160;
	ry=-240+80;
	m=set_new_unit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);
	rx=-880-160+80;
	ry=-240;
	m=set_new_unit(Side.Japan,UnitKind.InfantryBase,rx,ry,0);


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
	m=set_new_unit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,0);


	/*ヌーメア航空基地*/
	rx=5280;
	ry=-5760;
	m=set_new_unit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);
	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,2,NTG);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,3,NTG);



	rx=6500;
	ry=-5000;
	m=set_new_unit(Side.UnitedStates,UnitKind.LightCarrier,rx,ry,180);
	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,4,NTG);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,3,NTG);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Cruiser,rx,ry,180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Destroyer,rx,ry,180);
	rx+=150;
	m=set_new_unit(Side.UnitedStates,UnitKind.Transport,rx,ry,180);
	unit[m].Weapon=TR_GF1;		// 武装品種
	unit[m].Ammo=1;			// 数
	unit[m].MaxAmmo=1;			// 数 全容量



	// ガダルカナル
	rx=-880;
	ry=-240;
	m=set_new_unit(Side.UnitedStates,UnitKind.InfantryBase,rx,ry,0);
	rx=-880+80;
	ry=-240;
	m=set_new_unit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);
	rx=-880+80;
	ry=-240+80;
	m=set_new_unit(Side.UnitedStates,UnitKind.InfantryBase,rx,ry,0);
	rx=-880;
	ry=-240+80;
	m=set_new_unit(Side.UnitedStates,UnitKind.InfantryBase,rx,ry,0);





	// マップの作成
	load_it2("Map\\South_pacific.dat");

	}










//============================================================================
//		
//----------------------------------------------------------------------------
public void	cnct_sinario_999()
	{
	int		m,n,no;
	double	rx,ry;
	int		tf_no,unit_no;
	

	if(mode==GameMode.GameSetting)
		{
		// 増援設定などの設定のみ
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
		spry_rate[m]=5;
		first_spry_pt[m]=4000;

		//合衆国海軍側
		spry_rate[n]=10;
		first_spry_pt[n]=4000;

		rvrs_rule=0;
		rvrs_time=0;

		arrival_cont=0;

		return;
		}



	//  ゲーム設定
	//===============		 日本海軍		================
	// トラック島 港
	rx=-7600;
	ry=-4720;
	m=set_new_unit(Side.Japan,UnitKind.NavalBase,rx,ry,0);



	// 空港
	rx=-7600-80;
	ry=-4720;
	no=m=set_new_unit(Side.Japan,UnitKind.AirBase,rx,ry,135);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,4,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,4,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,1,no,1,NTG);
	m=set_new_unit_plane(Side.Japan,UnitKind.Bomber,0,no,1,NTG);



#if false
	rx=0;
	ry=400;
	m=set_new_unit_2(Side.UnitedStates,UnitKind.Attacker,0,rx,ry,0);
	unit[m].Weapon=TPD;		// 武装品種
	unit[m].Ammo=unit[m].MaxAmmo/*1*/;		// 数
	m=set_new_unit_2(Side.UnitedStates,UnitKind.Attacker,0,rx,ry+80*1,0);
	unit[m].Weapon=TPD;		// 武装品種
	unit[m].Ammo=unit[m].MaxAmmo/*1*/;		// 数
	m=set_new_unit_2(Side.UnitedStates,UnitKind.Attacker,0,rx,ry+80*2,0);
	unit[m].Weapon=TPD;		// 武装品種
	unit[m].Ammo=unit[m].MaxAmmo/*1*/;		// 数



	rx+=800;
	m=set_new_unit_2(Side.UnitedStates,UnitKind.Attacker,0,rx,ry,0);
	unit[m].Weapon=TPD;		// 武装品種
	unit[m].Ammo=unit[m].MaxAmmo/*1*/;		// 数
	m=set_new_unit_2(Side.UnitedStates,UnitKind.Attacker,0,rx,ry+80*1,0);
	unit[m].Weapon=TPD;		// 武装品種
	unit[m].Ammo=unit[m].MaxAmmo/*1*/;		// 数
	m=set_new_unit_2(Side.UnitedStates,UnitKind.Attacker,0,rx,ry+80*2,0);
	unit[m].Weapon=TPD;		// 武装品種
	unit[m].Ammo=unit[m].MaxAmmo/*1*/;		// 数
#endif



#if false
	rx=-7500;
	ry=-5800;


	m=set_new_unit(Side.Japan,UnitKind.Carrier,rx,ry,0);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,9,TPD);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,3,NTG);
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.LightCarrier,rx,ry,0);
	no=m;
	m=set_new_unit_plane(Side.Japan,UnitKind.Attacker,0,no,6,TPD);
	m=set_new_unit_plane(Side.Japan,UnitKind.Fighter,0,no,2,NTG);
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
	unit[m].Weapon=TR_GF1;		// 武装品種
	unit[m].Ammo=1;			// 数
	unit[m].MaxAmmo=1;			// 数 全容量
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Transport,rx,ry,135);
	unit[m].Weapon=TR_GF1;		// 武装品種
	unit[m].Ammo=1;			// 数
	unit[m].MaxAmmo=1;			// 数 全容量
	rx-=120;
	m=set_new_unit(Side.Japan,UnitKind.Transport,rx,ry,135);
	unit[m].Weapon=TR_GF1;		// 武装品種
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
	m=set_new_unit_2(Side.Japan,UnitKind.Battleship,1,rx,ry,90);
	ry+=80;
	m=set_new_unit_2(Side.UnitedStates,UnitKind.Submarine,0,rx,ry,90);




	//===============		 合衆国海軍		================
	// ハワイ港
	rx=7200;
	ry=-720;
	m=set_new_unit(Side.UnitedStates,UnitKind.NavalBase,rx,ry,90.0);

	// ハワイ空港
	rx=7200;
	ry=-560;
	m=set_new_unit(Side.UnitedStates,UnitKind.AirBase,rx,ry,45.0);
	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,1,NTG);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,3,NTG);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Bomber,0,no,4,NTG);


#if false
	//ミッドウェイ島
	rx=80;
	ry=3440;
	m=set_new_unit(Side.UnitedStates,UnitKind.AirBase,rx,ry,0);

	decision_point[0]=m;

	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,4,NTG);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,1,no,1,NTG);

	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,5,NTG);
	rx=80+160;
	ry=3440-80;
	m=set_new_unit(Side.UnitedStates,UnitKind.Pillboxes,rx,ry,0);


	rx=5000;
	ry=2500;



	m=set_new_unit(Side.UnitedStates,UnitKind.Carrier,rx,ry,180);
	no=m;
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Attacker,0,no,9,BOM);
	m=set_new_unit_plane(Side.UnitedStates,UnitKind.Fighter,0,no,3,NTG);
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
	m=set_new_unit_2(Side.UnitedStates,UnitKind.Battleship,0,rx,ry,90);
	ry+=80;
	m=set_new_unit_2(Side.Japan,UnitKind.Submarine,0,rx,ry,90);




	// マップの作成
	load_it2("Map\\Middle_pacific.dat");

	}





//============================================================================
//		
//----------------------------------------------------------------------------
public void get_sinario_data()
	{
	switch( sinario )
		{
		case 1:			cnct_sinario_1();			
						break;
		case 2:			cnct_sinario_2();			
						break;
		case 3:			cnct_sinario_3();			
						break;
		case 4:			cnct_sinario_4();			
						break;
		case 5:			cnct_sinario_5();			
						break;
		case 6:			cnct_sinario_6();			
						break;
		case 7:			cnct_sinario_7();
						break;
		case 8:			cnct_sinario_8();
						break;
		case 9:			cnct_sinario_9();
						break;



		case 101:			cnct_sinario_101();
						break;
		case 102:			cnct_sinario_102();
						break;
		case 103:			cnct_sinario_103();
						break;
		case 104:			cnct_sinario_104();
						break;
		case 105:			cnct_sinario_105();
						break;
		case 106:			cnct_sinario_106();
						break;


		case 999:			cnct_sinario_999();
						break;
		}
	}





//============================================================================
//		
//----------------------------------------------------------------------------
public void make_map_cg()
	{
	int	m,n;
	RECT	dstn_rect,src_rect;



	// マップデータから陸地をマップに描画します
	dstn_rect.left=sprt[MAP_BASE].base_x;
	dstn_rect.top=sprt[MAP_BASE].base_y;

	src_rect.left = sprt[MAP_BASE].base_x+306;
	src_rect.top = sprt[MAP_BASE].base_y;
	src_rect.right = src_rect.left+255;
	src_rect.bottom = src_rect.top+199;

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDS_OS, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,0))
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

				src_rect.left = sprt[MAP_BASE].base_x+270;
				src_rect.top = sprt[MAP_BASE].base_y+110;

				src_rect.right = src_rect.left+2;
				src_rect.bottom = src_rect.top+2;

				
				if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDS_OS, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,0))
					{
					restoreAll();
					}

				}
			}
	}






//============================================================================
//		
//----------------------------------------------------------------------------
public void	cnct_game_init()
	{
	int		m,n,f;
	double	rx,ry;
	RECT	dstn_rect,src_rect;

	_DP_FLAG	dp_flag;




	// 乱数の初期化
	srand( (uint)( cnct_game_rnd_sheed ) );


	make_my_rnd();


	scrn_moving_spd=0;


	// 
	for(m=0;m<=9;m++)
		unit_info[m]=0;


#if true
	if( sinario < 0 )
		{
		f=sinario;
		switch( f )
			{
			case -1:
				load_on_resume(1);		// とりあえずマップだけロードする為にシナリオ読みこむ
				break;
			case -2:
				load_on_resume(3);		// とりあえずマップだけロードする為にシナリオ読みこむ
				break;
			}
		get_sinario_data();		// シナリオナンバーからマップだけロードしてくれればいい。

		wrk_pp_x[0]=MAP_RIGHT+1;
		the_slct_unit=0;
		old_the_slct_unit=0;
		slct_unit_no=0;
		max_unit=USA_PLANE_END;			// とりあえず最大値を入れておく
		cls_flg=2;

		// 
		cmbt_menu_kind=0;
		cmbt_menu_slctd=0;

		// 雲のクリア
		for(n=0; n<KUMO_MAX/*255*/; n++)
			{
			kumo[n].used=0;
			kumo[n].Position = new WorldPosition(0, 0);
			kumo[n].kind=0;
			}

		//最初の雲
		cloud_in_start();

		make_map_cg();

		anti_air=0;		// 0が正常
		reveal=0;		// 0が正常

		game_speed=1;
		cc_count=0;
		FrameCount=0;
		game_end=GameResult.None;


		bf_cc_count[0]=0;
		bf_cc_count[1]=0;

		bf_rnd_count[0]=0;
		bf_rnd_count[1]=0;

		rnd_count=0;


		CameraPosition = new WorldPosition(-400, 400);

		dp_flag.dwType = MessageType.SyncFlag;
		dp_flag.unit_chk=0;
		bf_unit_chk[1]=0;
		dp_flag.cc_chk=(byte)cc_count;
		bf_cc_count[1]=(byte)cc_count;
		dp_flag.rnd_chk=(byte)rnd_count;
		bf_rnd_count[1]=(byte)rnd_count;
//t		lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_flag, sizeof(_DP_FLAG) );
		bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
		bufferDesc.pBufferData  = (byte*) &dp_flag;
		g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

		bf_new_pp[0].used=0;						// クリア
		bf_new_pp[1].used=0;						// クリア

		bf_new_slct[0].sw=0;						// クリア
		bf_new_slct[1].sw=0;						// クリア

		bf_new_menu[0].menu=0;
		bf_new_menu[1].menu=0;

		bf_arrived_unit[0]=0;
		bf_arrived_unit[1]=0;

		bf_game_system_menu[0]=0;
		bf_game_system_menu[1]=0;

		game_system_menu[0]=0;
		game_system_menu[1]=0;


		cnct_loop=cnct_loop_ct=30;
		cnct_loop_pt1=8;
		cnct_loop_pt2=19;

		ccc_wait[0]=ccc_wait[1]=0;

		auto_save_time=0;

//		load_on_resume(1);		
		switch( f )
			{
			case -1:
				load_on_resume(1);		// セーブデータをロードする
				break;
			case -2:
				load_on_resume(3);		// セーブデータをロードする
				break;
			}



		for(m=0; m<=255; m++)
			{
			slct_unit[0][m]=0;
			slct_unit[1][m]=0;
			}

#if SND_SW
		if( map_edit==0 )
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
		snd_[m]=0;
		}


	// 全エフェクトのクリア
	for(m=0; m<EFFECT_MAX/*255*/; m++)
		{
		effect[m].layer=EffectLayer.None;
		}

	// 全ファイアデータのクリア
	for(m=0; m<FIRE_MAX/*255*/; m++)
		{
		fire[m].used=0;
		for(n=0;n<=7;n++)
			fire[m].info[n]=0;
		}
	max_fire=0;


	// 全ユニットデータのクリア
//if( sinario!=9 )
	for(m=0; m<=255; m++)
		{
		unit[m].used=0;
		unit[m].Position = new WorldPosition(0, 0);
		unit[m].ctgry=UnitCategory.None;
		unit[m].kind=0;
		for(n=0;n<=15;n++)
			unit[m].info[n]=0;

		unit[m].os_indx_y=0;
		unit[m].os_indx_x=0;
		unit[m].drctn=0;
		unit[m].drctn_add=0;
		unit[m].spd=0;
		unit[m].spd_add=0;
		unit[m].max_spd=0;
		unit[m].min_spd=0;
		unit[m].a_spd_add=0;
		unit[m].spd=0;
		unit[m].stop=0;
		unit[m].spry=0;
		unit[m].em_flg[0]=unit[m].em_flg[1]=0;
		unit[m].EmergencyDestination=new WorldPosition(0, 0);
		//unit[m].pp_now=0;

		unit[m].is_ltl_ldr=0;
		unit[m].ltl_ldr=0;
		unit[m].no=0;
		unit[m].for_ltl_ldr=0;

		unit[m].for_form_spd=0;

		for(n=0;n<=7;n++)
			{
			unit[m].hp[n]=0;
			unit[m].arm[n]=0;
//			unit[m].arm2[n]=0;
			unit[m].gas[n]=0;
			}
		slct_unit[0][m]=0;
		slct_unit[1][m]=0;
		unit[m].tech=0;
		}


	wrk_pp_x[0]=MAP_RIGHT+1;



	the_slct_unit=0;
	old_the_slct_unit=0;
	slct_unit_no=0;


	max_unit=USA_PLANE_END;			// とりあえず最大値を入れておく


	cls_flg=2;


	// マップのクリア マップは横120チップ、縦90チップ
//if( sinario!=9 )
	for(m=0; m<256; m++)
		for(n=0; n<256; n++)
			{
			cmbt_map[m][n]=0;
			}

	cmbt_menu_kind=0;
	cmbt_menu_slctd=0;



	// 雲のクリア
	for(n=0; n<KUMO_MAX/*255*/; n++)
		{
		kumo[n].used=0;
		kumo[n].Position = new WorldPosition(0, 0);
		kumo[n].kind=0;
		}

	//最初の雲
	cloud_in_start();

/*
kumo[0].used=1;
kumo[0].x=MAP_RIGHT;
kumo[0].y=0;
*/
	//
	for(n=0;n<=3;n++)
		decision_point[n]=0;



//sinario=1;
//host_side=0;
	get_sinario_data();

	if( map_edit!=0 )
		{
		put_trgt=1;
		put_kind=(byte)UnitKind.Battleship;
		}


	make_map_cg();



	anti_air=0;		// 0が正常
	reveal=0;		// 0が正常



#if SND_SW
	if( map_edit==0 )
		{
		lpDSB_[SEA1][0].SetVolume( 0 );
		lpDSB_[SEA1][0].Play(0,0,DSBPLAY_LOOPING);	// ループする
		}
#endif


	game_speed=1;
	cc_count=0;
	FrameCount=0;

	game_end=GameResult.None;

	last_tick=timeGetTime();
	last_tick2=last_tick;




//	if( cnct_game )
//		{
		bf_cc_count[0]=0;
		bf_cc_count[1]=0;

		bf_rnd_count[0]=0;
		bf_rnd_count[1]=0;

		rnd_count=0;


		//  ゲーム設定
		if(host_side==0)
			{
			if(you_are_host!=0)
				your_side=Side.Japan;
			else
				your_side=Side.UnitedStates;
			}
		else
			{
			if(you_are_host!=0)
				your_side=Side.UnitedStates;
			else
				your_side=Side.Japan;
			}

		CameraPosition = new WorldPosition(-400, 400);


		dp_flag.dwType = MessageType.SyncFlag;
		dp_flag.unit_chk=0;
		bf_unit_chk[1]=0;
		dp_flag.cc_chk=(byte)cc_count;
		bf_cc_count[1]=(byte)cc_count;
		dp_flag.rnd_chk=(byte)rnd_count;
		bf_rnd_count[1]=(byte)rnd_count;
		bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
		bufferDesc.pBufferData  = (byte*) &dp_flag;
		g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );



		bf_new_pp[0].used=0;						// クリア
		bf_new_pp[1].used=0;						// クリア

		bf_new_slct[0].sw=0;						// クリア
		bf_new_slct[1].sw=0;						// クリア

		bf_new_menu[0].menu=0;
		bf_new_menu[1].menu=0;

		bf_arrived_unit[0]=0;
		bf_arrived_unit[1]=0;

		bf_game_system_menu[0]=0;
		bf_game_system_menu[1]=0;

		game_system_menu[0]=0;
		game_system_menu[1]=0;



		cnct_loop=cnct_loop_ct=20;
		cnct_loop_pt1=6;
		cnct_loop_pt2=13;

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

		ccc_wait[0]=ccc_wait[1]=0;

		auto_save_time=0;

		if(you_are_host!=0)
			spry_pt=first_spry_pt[0];
		else
			spry_pt=first_spry_pt[1];

		spry_no_cont=0;
		spry_trgt=0;


		rest_time=0;


		if( map_edit==0)
			save_on_resume(3);


//		}


	}
}
