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

// Port of demo.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{




//BOOL CALLBACK SvDlgProc(HWND hDlg, UINT Msg, WPARAM wParam, LPARAM lParam);
//BOOL CALLBACK SessionDlgProc(HWND hDlg, UINT Msg, WPARAM wParam, LPARAM lParam);


public int	exist_auto_save;



//============================================================================
// デモ画面
//----------------------------------------------------------------------------
public void	demo_func()
	{
	RECT	src_rect,field_rect,dstn_rect;
	int	m,n,g,no1,i,wrk,wrk2,wrk3; Array6<int> menu = default; Array6<int> menu2 = default;
    Array5<Array128<byte>> ach = default;
    Array5<int> len = default;
	HDC					hdc;
	byte	bf; Array256<byte> copy = default;
	_DP_FLAG	dp_flag;
	_DP_DATA_20	dp_data_20=default;


#if CONN_DBG
	rival_mode=mode;
#endif



	if((FrameCount%40)==0)
		{
		// 自分のモードを相手に伝える。
		dp_flag.dwType = RIVAL_MODE;
		dp_flag.rival_mode=mode;
		bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
		bufferDesc.pBufferData  = (byte*) &dp_flag;
		g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1, 0, null, ref hAsync, EASY_SEND );



		// 自分のバージョンを相手に伝える。
		dp_data_20.dwType = RIVAL_VER;
		wsprintf( dp_data_20.friend_chat, "%s",VER );
		bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_20));
		bufferDesc.pBufferData  = (byte*) &dp_data_20;
		g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, EASY_SEND );
		}




	if( demo_time==0 )
		{
		rival_mode=0;
		wsprintf(rival_ver, "- - -" );
		}


	cls_flg=1;


//	key_cont();


	// ユニットインフォーメィション
	sprt[TTL_BACK].x=212-50;
	sprt[TTL_BACK].y=130;

	
	src_rect.left = 	sprt[TTL_BACK].base_x;
	src_rect.top = sprt[TTL_BACK].base_y;
	src_rect.right = sprt[TTL_BACK].base_x+sprt[TTL_BACK].wd;
	src_rect.bottom = sprt[TTL_BACK].base_y+sprt[TTL_BACK].ht;

	// dstn_rect は ディスティネーションレクタングルです。
	dstn_rect.left=sprt[TTL_BACK].x;
	dstn_rect.top=sprt[TTL_BACK].y;
	//dstn_rect.right=sprt[TTL_BACK].x+sprt[TTL_BACK].wd/2;
	//dstn_rect.bottom=sprt[TTL_BACK].y+sprt[TTL_BACK].ht/2;

	
	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,0) )
		{
		restoreAll();
		}





	//draw_line4(10, 10, 200,200,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));


/*****
	// src_rect は ソースサーフェスのレクタングルです。
	src_rect.left = (src_os[os[no1].src_no].wd * (os[no1].no % src_os[os[no1].src_no].os_of_x)) +1-1;
	src_rect.top = (src_os[os[no1].src_no].ht* (os[no1].no / src_os[os[no1].src_no].os_of_x)) +1-1;
	src_rect.right = (src_rect.left + src_os[os[no1].src_no].wd)-1+1;
	src_rect.bottom = (src_rect.top + src_os[os[no1].src_no].ht)-1+1;

	// dstn_rect は ディスティネーションレクタングルです。
	dstn_rect.left=os[no1].x-os[no1].cx;
	dstn_rect.top=os[no1].y-os[no1].cy;
	dstn_rect.right=dstn_rect.left+src_os[os[no1].src_no].wd-1+1;
	dstn_rect.bottom=dstn_rect.top+src_os[os[no1].src_no].ht-1+1;

*****/






		if ( IDirectDrawSurface_GetDC(lpDDSBack, &hdc) == DD_OK ) 
			{
			SetBkMode(hdc, TRANSPARENT);
			SelectObject(hdc, gameFont_2);

/*
			if( g_dwNumberOfActivePlayers==2 )
				len[0] = wsprintf(ach[0], "CONNECTED NOW");
			else
				len[0] = wsprintf(ach[0], "NO CONNECTED");

			SetTextColor(hdc, RGB(255, 0, 0));
			TextOut(hdc, 360, 60, ach[0], len[0]);
*/



#if !LNGG_VER
			len[0] = wsprintf(ach[0], "Naval South Pacific War on the Net %s",VER );
			len[1] = wsprintf(ach[1], "      Copyright 2002 Ken-ichi Tokumitsu");
			len[2] = wsprintf(ach[2], "       E-mail:kenken@ta2.so-net.ne.jp");
			len[3] = wsprintf(ach[3], "               奈良鹿製作所");
#else
			len[0] = wsprintf(ach[0], "Naval South Pacific War on the Net version 1.08 (English version)");
			len[1] = wsprintf(ach[1], "      Copyright 2002 Ken-ichi Tokumitsu");
			len[2] = wsprintf(ach[2], "  http://www02.u-page.so-net.ne.jp/ta2/kenken/");
			len[3] = wsprintf(ach[3], "           Nara-Shika Seisakusho");
#endif
			for( n=0; n<=3; n++)
				{
				SetTextColor(hdc, RGB(255, 255, 255));
				TextOut(hdc, 330, 580+(n*23), ach[n], len[n]);
				}



			SelectObject(hdc, gameFont_1);

			len[0] = wsprintf(ach[0], "YOUR VERSION = %s",VER );
			SetTextColor(hdc, RGB(255, 255, 255));
			TextOut(hdc, 385, 84, ach[0], len[0]);

			len[0] = wsprintf(ach[0], "RIVAL VERSION = %s",rival_ver );
			SetTextColor(hdc, RGB(255, 255, 255));
			TextOut(hdc, 385, 99, ach[0], len[0]);






//		len[0] = wsprintf(copy, "[0]=%d  [1]=%d  [2]=%d  [3]=%d" ,ach[3][0],ach[3][1],ach[3][2],ach[3][3] );

#if false
		len[0] = wsprintf(copy, "my_string_crsr=%d my_string_rpd=%d [0]=%d  [1]=%d  [2]=%d  [3]=%d" ,my_string_crsr,my_string_rpd,my_string[0],my_string[1],my_string[2],my_string[3] );
		TextOut(hdc, 100, 50, copy, len[0]);

		len[0] = wsprintf(copy, my_string );
		TextOut(hdc, 100, 100, copy, len[0]);
#endif

			IDirectDrawSurface_ReleaseDC(lpDDSBack, hdc);
			}





	if( lf_btn==3 )
		{
//		lf_btn=0;
		if( map_edit!=0 )
			{
			go_cnct_game_setting();
			}
		else if( g_dwNumberOfActivePlayers==2 && rival_mode==DEMO && you_are_host!=0 )
			{
			go_cnct_game_setting();

			if( 1!=0 )
				{
				dp_flag.dwType = GO_GAME_SETTING;
				dp_flag.rival_mode=mode;
				bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
				bufferDesc.pBufferData  = (byte*) &dp_flag;
				g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1, 0, null, ref hAsync, MUST_SEND );

				//ここにくるのはホストだけ
				// リジュームデータがホストをやってたかしらべる。


				HANDLE	hFile;

				hFile=CreateFile("Saved\\resume_1.dat", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
																null, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, null);

				if( INVALID_HANDLE_VALUE==hFile )
					{
					// ファイルありませんでした。
					you_were_host=0;
					}
				else
					{
					// ファイルはあった。
					load_on_resume(1);
					you_were_host=you_are_host;		// 前回ホストだったら１が代入
					CloseHandle(hFile);
					}
				you_are_host=1;						// ここに来るのはホストなのでこれでいい。
				sinario=0;

				}
			}


//if( dbg[0]==1 )
//	{
//	MessageBox(hwndApp,"bitmap_surface","残念！",MB_OK | MB_ICONSTOP);

//	DialogBox(hInstApp, MAKEINTRESOURCE(IDD_ADDRESS_OVERRIDE), hwndApp, OverrideDlgProc);


//	ShowWindow(hwndApp,SW_MINIMIZE);



//	hwndDlg = CreateDialog(hInst,"SAMPLE_DIALOG",hwndApp,SampleDlgProc);

//	}

/*
		if( DialogBox(hInstance, MAKEINTRESOURCE(IDD_SPDIALOG), hWnd, (DLGPROC)SvDlgProc) )
			{
			// セッション選択のダイアログ・ボックスを表示する
			if (DialogBox(hInstance, MAKEINTRESOURCE(IDD_SESSIONDLG), hwnd, (DLGPROC)SessionDlgProc))
*/
//		CoInitialize( NULL );
//		SetupConnection(hInstance, &DPInfo);
//		CoUninitialize();
//		dp_init();
//		DialogBoxParam(hInstance, MAKEINTRESOURCE(IDD_DP), hwnd, cnctn_dialog_proc,
//							(LPARAM) hInstance);
		//mode=COMBAT;
		//game_init();
		}



	if( /*demo_time==10 ||*/  ri_btn==3 )
		{

		ri_btn=0;

/*
		// 通信対戦用の初期化
		mode=CNCT_GAME_SETUP;

		wsprintf(g_strLocalPlayerName, "Player" );	
		wsprintf(g_strRivalPlayerName, "Player" );	

		cnct_game=0;

		join_game_start=0;

		input_chat_now=0;
		for(n=0;n<128;n++)
			{
			my_chat[n]=0;
			friend_chat[n]=0;
			my_string[n]=0;
			my_string_crsr=0;
			my_string_rpd=0;
			}

		my_chat_dsp_time=0;
		friend_chat_dsp_time=0;

		you_can_order=1;
		go_next_1=0;
		go_next_2=0;

		bf_cc_count[0]=bf_cc_count[1]=0;
		bf_rnd_count[0]=bf_rnd_count[1]=0;
*/

		}



/*
mode=SETUP;
*/

	demo_time++;



	}










//============================================================================
// 
// コネクトゲームスタートの値
//----------------------------------------------------------------------------
public void go_cnct_game_setting()
	{


	sinario=0;


	mode=CNCT_GAME_SETTING;
	host_side=0;
	decision_sw=1;
	arrival_cont=0;

	spry_rate[0]=0;		// Host
	spry_rate[1]=0;		// Guest

	first_spry_pt[0]=0;		// Host
	first_spry_pt[1]=0;		// Guest


	if( hwndChatDlg!=null )
		{
		DestroyWindow(hwndChatDlg);
		hwndChatDlg=null;
		}


#if SND_SW
	lpDSB_[SEA1][0/*snd_[0]*/].Stop();		// 
#endif




HANDLE	hFile;

	hFile=CreateFile("Saved\\auto_save.dat", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
													null, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, null);

	if( INVALID_HANDLE_VALUE==hFile )
		{
		// ファイルありませんでした。
		exist_auto_save=0;
		}
	else
		{
		// ファイルはあった。
		exist_auto_save=1;
		CloseHandle(hFile);
		}

	}





//============================================================================
// 通信対戦セットアップ
// とりあえず、シナリオとサイドを選んでゲームへ
//----------------------------------------------------------------------------
public void	cnct_game_setting()
	{
	RECT	src_rect,field_rect,dstn_rect;
	int	m=default /* C4701 */,n,g,no1,i,wrk,wrk2,wrk3,flg,rx,ry; Array7<int> menu = default; Array7<int> menu2 = default;
    Array12<Array128<byte>> ach = default;
    Array12<int> len = default;
	HDC					hdc;
	Array256<byte> cBuf = default;


	_DP_DATA_1		dp_data_1;
	_DP_DATA_1* lp_dp_data_1;
	_DP_FLAG			dp_flag;
	_DP_DATA_20		dp_data_20=default;


#if CONN_DBG
	rival_mode=mode;
#endif

	cls_flg=1;



	// マウス情報
/*t
	GetCursorPos(&crsr_pt);
	if(!scrn_mode)
		ScreenToClient(hwnd, &crsr_pt);
	GetKeyboardState(cBuf);
*/


//you_are_host=1;
//rival_mode=CNCT_GAME_SETTING;


	if( map_edit!=0 )
		{
		rival_mode=mode;
		}



	if ( IDirectDrawSurface_GetDC(lpDDSBack, &hdc) == DD_OK ) 
		{
		SetBkMode(hdc, TRANSPARENT);
		SelectObject(hdc, gameFont_1);

if(mode==CNCT_GAME_SETTING)
		SetTextColor(hdc, RGB(255, 255, 255));
else
		SetTextColor(hdc, RGB(126, 126, 126));

#if !LNGG_VER
		len[0] = wsprintf(ach[0], "通信対戦セッティング");
#else
		len[0] = wsprintf(ach[0], "Connection setting");
#endif
		TextOut(hdc, 300, 80-26, ach[0], len[0]);

		IDirectDrawSurface_ReleaseDC(lpDDSBack, hdc);
		}



//return;


		if ( IDirectDrawSurface_GetDC(lpDDSBack, &hdc) == DD_OK ) 
			{
			SetBkMode(hdc, TRANSPARENT);
			SelectObject(hdc, gameFont_1);


#if !LNGG_VER
			len[0] = wsprintf(ach[0], "シナリオ選択画面");
#else
			len[0] = wsprintf(ach[0], "Mission Menu");
#endif

			if(mode==CNCT_GAME_SETTING)
				SetTextColor(hdc, RGB(255, 255, 255));
			else
				SetTextColor(hdc, RGB(126, 126, 126));

			TextOut(hdc, 200, 80, ach[0], len[0]);

#if !LNGG_VER
			if( sinario<=99 )
				{
				m=0;
				len[m] = wsprintf(ach[m], "空母機動部隊の戦い１");				
				m++;
				len[m] = wsprintf(ach[m], "空母機動部隊の戦い２");
				m++;
				len[m] = wsprintf(ach[m], "空母機動部隊の戦い３");

				m++;
				len[m] = wsprintf(ach[m], "艦隊決戦１");
				m++;
				len[m] = wsprintf(ach[m], "艦隊決戦２");

				m++;
				len[m] = wsprintf(ach[m], "ミッドウェイ攻略１");				
				m++;
				len[m] = wsprintf(ach[m], "ミッドウェイ攻略２");
				m++;
				len[m] = wsprintf(ach[m], "中部太平洋の戦い１");
				m++;
				len[m] = wsprintf(ach[m], "ユーザーシナリオ");
				}
			else if(sinario<=199)
				{
				m=0;
				len[m] = wsprintf(ach[m], "ガダルカナルを巡る戦い１");
				m++;
				len[m] = wsprintf(ach[m], "ガダルカナルを巡る戦い２");

				m++;
				len[m] = wsprintf(ach[m], "日本近海の戦い１");

				m++;
				len[m] = wsprintf(ach[m], "南太平洋の戦い１");
				m++;
				len[m] = wsprintf(ach[m], "南太平洋の戦い２");
				m++;
				len[m] = wsprintf(ach[m], "ガ島争奪戦");

				}
			else if(sinario<=299)
				{
				m=0;
				len[m] = wsprintf(ach[m], "硫黄島攻略１");
				m++;
				len[m] = wsprintf(ach[m], "硫黄島攻略２");
				m++;
				len[m] = wsprintf(ach[m], "日本近海の戦い１");
				m++;
				len[m] = wsprintf(ach[m], "日本近海の戦い２");
				m++;
				len[m] = wsprintf(ach[m], "日本近海の戦い３");
				}
#else

			if( sinario<=99 )
				{
				m=0;
				len[m] = wsprintf(ach[m], "Battle of Carriers 1");				
				m++;
				len[m] = wsprintf(ach[m], "Battle of Carriers 2");
				m++;
				len[m] = wsprintf(ach[m], "Battle of Carriers 3");

				m++;
				len[m] = wsprintf(ach[m], "Fleet Battle 1");
				m++;
				len[m] = wsprintf(ach[m], "Fleet Battle 2");

				m++;
				len[m] = wsprintf(ach[m], "Invasion of Midway 1");				
				m++;
				len[m] = wsprintf(ach[m], "Invasion of Midway 2");
				m++;
				len[m] = wsprintf(ach[m], "Battle of Middle Pacific 1");

				m++;
				len[m] = wsprintf(ach[m], "Battles on user mission");
				}
			else if(sinario<=199)
				{
				m=0;
				len[m] = wsprintf(ach[m], "Battles around Guadalcanal 1");
				m++;
				len[m] = wsprintf(ach[m], "Battles around Guadalcanal 2");

				m++;
				len[m] = wsprintf(ach[m], "Battles off Japan 1");

				m++;
				len[m] = wsprintf(ach[m], "Battles of South Pacific 1");
				m++;
				len[m] = wsprintf(ach[m], "Battles of South Pacific 2");

				m++;
				len[m] = wsprintf(ach[m], "Scramble for the Guadalcanal");
				}
			else if(sinario<=299)
				{
				}

#endif


			if( you_are_host!=0 && mode==CNCT_GAME_SETTING )
				{
				for( n=0; n<=m; n++)
					{
					// ptin dbg
					dstn_rect.left=120;
					dstn_rect.top=150+(n*25);
					dstn_rect.right=dstn_rect.left+(len[n]*12);
					dstn_rect.bottom=dstn_rect.top+24;
					if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 && rival_mode==CNCT_GAME_SETTING )
						{
						SetTextColor(hdc, RGB(255, 0, 0));

						if(sinario<=99)
							sinario=(short)(n+1);
						else if(sinario<=199)
							sinario=(short)(n+1+100);//99;
						else if(sinario<=299)
							sinario=(short)(n+1+200);//199;

						if( lf_btn==3 )
							{
							if( sinario==9 )
								{
								/*g_hDlg =*/ CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_FILE_CONT), hwndApp, (DLGPROC)IDD_FILE_LOAD_Proc );
//	return;

#if false
								my_dlg_wait();

								if( user_sinario_fn[0]!='\0' )
									{
TCHAR		temp_buf[MAX_PATH];

									wsprintf( temp_buf, "%s", user_sinario_fn );
									wsprintf( user_sinario_fn, "Scenario\\%s", temp_buf );

									if( map_edit==0 )
										{
										// なんかユーザーファイルが選ばれた。
										dp_data_20.dwType = USER_SINARIO_FN;
										wsprintf( dp_data_20.friend_chat, "%s",user_sinario_fn );
										bufferDesc.dwBufferSize = sizeof(_DP_DATA_20);
										bufferDesc.pBufferData  = (BYTE*)&dp_data_20;
										g_pDP->SendTo( g_dpnidRivalPlayer, &bufferDesc, 1,	0, NULL, &hAsync, MUST_SEND );
										}
									}
#endif
								}


							if( sinario!=9 || user_sinario_fn[0]!='\0' )
								{
								get_sinario_data();


								// ホストの選択状態をゲストにセンドします。
								dp_data_1.dwType = OUT_GAME_SETTING;
								dp_data_1.data[0] = (short)host_side;
								dp_data_1.data[1] = sinario;

								dp_data_1.data[2] = spry_rate[0];
								dp_data_1.data[3] = spry_rate[1];

								dp_data_1.data[4] = decision_sw;

								dp_data_1.data[5] = first_spry_pt[0];
								dp_data_1.data[6] = first_spry_pt[1];

								dp_data_1.data[7] = arrival_cont;

								dp_data_1.data[8] = rvrs_time;
								dp_data_1.data[9] = rvrs_rule;

								bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
								bufferDesc.pBufferData  = (byte*) &dp_data_1;
								g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );



								mode=CNCT_CNFG_SETTING;
								}
							}
						}
					else
						SetTextColor(hdc, RGB(255, 255, 255));
					TextOut(hdc, 120, 150+(n*25), ach[n], len[n]);
					}

				if( rival_mode==mode && mode!=CNCT_CNFG_SETTING && (FrameCount%10)==0 )
					{
					// ホストの選択状態をゲストにセンドします。
					dp_data_1.dwType = SIDE_AND_SINARIO;
					dp_data_1.data[0] = (short)host_side;
					dp_data_1.data[1] = sinario;

					dp_data_1.data[2] = spry_rate[0];
					dp_data_1.data[3] = spry_rate[1];

					dp_data_1.data[4] = decision_sw;

					dp_data_1.data[5] = first_spry_pt[0];
					dp_data_1.data[6] = first_spry_pt[1];

					dp_data_1.data[7] = arrival_cont;

					dp_data_1.data[8] = rvrs_time;
					dp_data_1.data[9] = rvrs_rule;

					bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
					bufferDesc.pBufferData  = (byte*) &dp_data_1;
					g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, EASY_SEND );
					}
				}
			else
				{
				for( n=0; n<=m; n++)
					{
					if(mode==CNCT_GAME_SETTING)
						{
						if( n+1==sinario%100 )
							SetTextColor(hdc, RGB(255, 0, 0));
						else
							SetTextColor(hdc, RGB(255, 255, 255));
						}
					else
						{
						if( n+1==sinario%100 )
							SetTextColor(hdc, RGB(126, 0, 0));
						else
							SetTextColor(hdc, RGB(126, 126, 126));
						}

					TextOut(hdc, 120, 150+(n*25), ach[n], len[n]);
					}

				if(join_game_start==OUT_GAME_SETTING && you_are_host==0 && mode==CNCT_GAME_SETTING )
					{
					// ジョインが受け取る
					mode=CNCT_CNFG_SETTING;
					join_game_start=0;
					}
				}


			if( you_are_host!=0 && mode==CNCT_GAME_SETTING )
				{
#if !LNGG_VER
				len[0] = wsprintf(ach[0], "シナリオ切り替え ->>>");
#else
				len[0] = wsprintf(ach[0], "Page Change ->>>");
#endif

//				dstn_rect.left=330;
				dstn_rect.left=250;
				dstn_rect.top=110;
				dstn_rect.right=dstn_rect.left+(len[0]*12);
				dstn_rect.bottom=dstn_rect.top+24;
				if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 && rival_mode==CNCT_GAME_SETTING )
					{
					SetTextColor(hdc, RGB(255, 0, 0));


#if true
					if( lf_btn==3 )
						{
						if(sinario<=99)
							sinario=101;
						else if(sinario<=199)
							sinario=1;
						}
#endif
					}
				else
					SetTextColor(hdc, RGB(255, 255, 255));

				TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
				}



			// リジュームスタート
			rx=80;
			if( you_are_host!=0 )
				{
#if !LNGG_VER
				if( you_were_host!=0 )
					len[0] = wsprintf(ach[0], "リジュームスタート ->>>");
				else
					len[0] = wsprintf(ach[0], "リジュームデータがホストではない。");
#else
				if( you_were_host )
					len[0] = wsprintf(ach[0], "Resume Start ->>>");
				else
					len[0] = wsprintf(ach[0], "You have no resumed data as your host.");
#endif

				dstn_rect.left=80;
				dstn_rect.top=400;

				if( mode==CNCT_GAME_SETTING )
					{
					dstn_rect.right=dstn_rect.left+(len[0]*12);
					dstn_rect.bottom=dstn_rect.top+24;
					if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 && rival_mode==CNCT_GAME_SETTING && you_were_host!=0 )
						{
						SetTextColor(hdc, RGB(255, 0, 0));

						if( lf_btn==3 )
							{
							// ホストの選択状態をゲストにセンドします。
							dp_data_1.dwType = START_IN_RESUME;
//t							lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer,DPSEND_GUARANTEED , &dp_data_1, sizeof(DP_DATA_1) );
							bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
							bufferDesc.pBufferData  = (byte*) &dp_data_1;
							g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

							mode=CMBT;
							sinario=-1;		// －１でリジュームを示す
							}
						}
					else
						SetTextColor(hdc, RGB(255, 255, 255));
					}
				else
					{
					SetTextColor(hdc, RGB(126, 126, 126));
					}

				TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
				}
			else
				{
				if( join_game_start==START_IN_RESUME )
					{
					mode=CMBT;
					join_game_start=0;
					sinario=-1;		// －１でリジュームを示す
					}
				}




			// リジュームスタート
			rx=80;
			if( you_are_host!=0  )
				{
#if !LNGG_VER
				if( exist_auto_save!=0 )
					len[0] = wsprintf(ach[0], "オートセーブスタート ->>>");
				else
					len[0] = wsprintf(ach[0], "オートセーブファイルは存在しない");
#else
				if( exist_auto_save )
					len[0] = wsprintf(ach[0], "Autosave Start ->>>");
				else
					len[0] = wsprintf(ach[0], "No exist of Autosave file");
#endif

				dstn_rect.left=80;
				dstn_rect.top=430;


				if( mode==CNCT_GAME_SETTING )
					{
					dstn_rect.right=dstn_rect.left+(len[0]*12);
					dstn_rect.bottom=dstn_rect.top+24;
					if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 && rival_mode==CNCT_GAME_SETTING && exist_auto_save!=0 )
						{
						SetTextColor(hdc, RGB(255, 0, 0));

						if( lf_btn==3 )
							{
							// ホストの選択状態をゲストにセンドします。
							dp_data_1.dwType = START_IN_AUTOSAVE;
//t							lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer,DPSEND_GUARANTEED , &dp_data_1, sizeof(DP_DATA_1) );
							bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
							bufferDesc.pBufferData  = (byte*) &dp_data_1;
							g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

							mode=CMBT;
							sinario=-2;		// －２でオートセーブからのスタートを示す
							}
						}
					else
						{
						SetTextColor(hdc, RGB(255, 255, 255));
						}
					}
				else
					{
					SetTextColor(hdc, RGB(126, 126, 126));
					}
				TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
				}
			else
				{
				if( join_game_start==START_IN_AUTOSAVE )
					{
					mode=CMBT;
					join_game_start=0;
					sinario=-2;		// －２でオートセーブからのスタートを示す
					}
				}





			// 操作対象の切り替え
			rx=80;
			if( map_edit==0 && you_are_host!=0 && mode==CNCT_GAME_SETTING )
				{
#if !LNGG_VER
				len[0] = wsprintf(ach[0], "操作対象の切り替え ->>>");
#else
				len[0] = wsprintf(ach[0], "Side Change ->>>");
#endif

				dstn_rect.left=rx;
				dstn_rect.top=475;
				dstn_rect.right=dstn_rect.left+(len[0]*12);
				dstn_rect.bottom=dstn_rect.top+24;
				if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 && rival_mode==CNCT_GAME_SETTING )
					{
					SetTextColor(hdc, RGB(255, 0, 0));
					if( lf_btn==3 )
						{
						host_side=(host_side==0 ? 1 : 0);
						}
					}
				else
					SetTextColor(hdc, RGB(255, 255, 255));

				TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
				}

			if(host_side==0)
				{
#if !LNGG_VER
				len[0] = wsprintf(ach[0], "操作対象：日本海軍（ホスト）、合衆国海軍（ゲスト）");
#else
				len[0] = wsprintf(ach[0], "Side: Japan Navy(Host Player)  U.S.Navy(Guest Player)");
#endif
				}
			else
				{
#if !LNGG_VER
				len[0] = wsprintf(ach[0], "操作対象：合衆国海軍（ホスト）、日本海軍（ゲスト）");
#else
				len[0] = wsprintf(ach[0], "Side: U.S.Navy(Host Player)  Japan Navy(Guest Player)");
#endif
				}


#if !LNGG_VER
			switch( sinario )
				{
				case 1:
					len[1] = wsprintf(ach[1], "空母機動部隊同士の戦いです。小規模です。");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "勝敗条件：敵空母撃沈か敵歩兵基地破壊で勝利");
					len[6] = wsprintf(ach[6], "");
					break;

				case 2:
					len[1] = wsprintf(ach[1], "空母機動部隊同士の戦いです。中規模です。");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "勝敗条件：敵空母撃沈か敵歩兵基地破壊で勝利");
					len[6] = wsprintf(ach[6], "");
					break;

				case 3:
					len[1] = wsprintf(ach[1], "空母機動部隊同士の戦いです。大規模です。");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "勝敗条件：敵空母撃沈か敵歩兵基地破壊で勝利");
					len[6] = wsprintf(ach[6], "");
					break;

				case 4:
					len[1] = wsprintf(ach[1], "戦闘艦船のみの戦いです。");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "勝敗条件：敵軍全ての戦艦、巡洋艦の撃沈、");
					len[6] = wsprintf(ach[6], "または歩兵基地の破壊で勝利。");
					break;

				case 5:
					len[1] = wsprintf(ach[1], "戦闘艦船のみの戦いです。");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "勝敗条件：敵軍全ての戦艦、巡洋艦の撃沈、");
					len[6] = wsprintf(ach[6], "または歩兵基地の破壊で勝利。");
					break;

				case 6:
					len[1] = wsprintf(ach[1], "日本海軍によるミッドウェイ島の攻略作戦です。");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "勝敗条件：ミッドウェイ島の米軍施設の壊滅で");
					len[6] = wsprintf(ach[6], "日本海軍の勝利となります。");
					break;

				case 7:
					len[1] = wsprintf(ach[1], "日本海軍によるミッドウェイ島の攻略作戦です。");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "勝敗条件：ミッドウェイ島の米軍施設の壊滅と、同島に");
					len[6] = wsprintf(ach[6], "日本軍の要塞を一つ完成で勝利となります。");
					break;

				case 8:
					len[1] = wsprintf(ach[1], "中部太平洋での戦闘です。");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "勝敗条件：ミッドウェイの米軍施設壊滅と自軍の要塞で");
					len[6] = wsprintf(ach[6], "日本軍の勝利、ウェークに同じ条件で米軍の勝利。");
					break;


				case 9:
					len[1] = wsprintf(ach[1], "ユーザーシナリオでの戦いです。");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "勝敗条件：任意に決めてください。");
					len[6] = wsprintf(ach[6], "");
					break;



				case 101:
					len[1] = wsprintf(ach[1], "ガダルカナル島を巡る戦いです。");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "勝敗条件：ガダルカナル島の日本軍の陸上施設の");
					len[6] = wsprintf(ach[6], "壊滅で米海軍の勝利です。");
					break;

				case 102:
					len[1] = wsprintf(ach[1], "ガダルカナル島を巡る戦いです。");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "勝敗条件：ガダルカナル島の日本軍の陸上施設の壊滅と");
					len[6] = wsprintf(ach[6], "同島に航空基地の完成で米海軍の勝利です。");
					break;

				case 103:
					len[1] = wsprintf(ach[1], "日本近海の戦いです。");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "勝敗条件：日本本土の都市を全て壊滅すれば");
					len[6] = wsprintf(ach[6], "米海軍の勝利です。");
					break;

				case 104:
					len[1] = wsprintf(ach[1], "南太平洋の戦いです。");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "勝敗条件：ブーゲンビル島の日本軍地上施設");
					len[6] = wsprintf(ach[6], "壊滅で米海軍の勝利です。");
					break;

				case 105:
					len[1] = wsprintf(ach[1], "南太平洋の戦いです。");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "勝敗条件：ブーゲンビル島の日本軍施設壊滅で米海軍の勝利、");
					len[6] = wsprintf(ach[6], "ガダルカナルの米軍施設壊滅で日本海軍の勝利です。");
					break;

				case 106:
					len[1] = wsprintf(ach[1], "ガダルカナル島の争奪戦です。");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "勝敗条件：ガダルカナル島の敵陸上施設全滅と自軍施設");
					len[6] = wsprintf(ach[6], "が四個以上あれば勝利です。");
					break;




				default:
					len[1] = wsprintf(ach[1], "");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "");
					len[6] = wsprintf(ach[6], "");
					break;
				}
#else
			switch( sinario )
				{
				case 1:
					len[1] = wsprintf(ach[1], "Battle of small size of carrier fleets.");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "Victory Line : Sink of enemy's carrier or destruction of trenchies.");
					len[6] = wsprintf(ach[6], "");
					break;

				case 2:
					len[1] = wsprintf(ach[1], "Battle of middle size of carrier fleets.");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "Victory Line : Sink of enemy's carrier or destruction of trenchies.");
					len[6] = wsprintf(ach[6], "");
					break;

				case 3:
					len[1] = wsprintf(ach[1], "Battle of the biggest size of carrier fleets.");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "Victory Line : Sink of enemy's carrier or destruction of trenchies.");
					len[6] = wsprintf(ach[6], "");
					break;

				case 4:
					len[1] = wsprintf(ach[1], "Battle of the only combat ships.");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "Victory Line : Sink of all enemy's battle-ships and cruisers");
					len[6] = wsprintf(ach[6], "or destruction of trenchies.");
					break;

				case 5:
					len[1] = wsprintf(ach[1], "Battle of the only combat ships.");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "Victory Line : Sink of all enemy's battle-ships and cruisers");
					len[6] = wsprintf(ach[6], "or destruction of trenchies.");
					break;

				case 6:
					len[1] = wsprintf(ach[1], "Invasion of Japan Navy for Midway.");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "Victory Line : Total annihilation of U.S. bases on Midway.");
					len[6] = wsprintf(ach[6], "");
					break;

				case 7:
					len[1] = wsprintf(ach[1], "Invasion of Japan Navy for Midway.");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "Victory Line : Total annihilation of U.S. bases on Midway");
					len[6] = wsprintf(ach[6], "and complete of Japanese one of fortress on the island.");
					break;

				case 8:
					len[1] = wsprintf(ach[1], "Battles of middle pacific.");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");

					len[4] = wsprintf(ach[4], "Victory Line : Total annihilation of U.S. bases on Midway");
					len[5] = wsprintf(ach[5], "and complete of Japanese one of fortress on the island.");
					len[6] = wsprintf(ach[6], "Or opposite conditions for Wake island is U.S. Navy's victory.");
					break;

				case 9:
					len[1] = wsprintf(ach[1], "Battles on user mission.");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");

					len[4] = wsprintf(ach[4], "");
					len[5] = wsprintf(ach[5], "Victory Line : It depends on your rule.");
					len[6] = wsprintf(ach[6], "");
					break;



				case 101:
					len[1] = wsprintf(ach[1], "Battles around Guadalcanal.");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "Victory Line : Total annihilation of Japanese bases on Guadalcanal");
					len[6] = wsprintf(ach[6], "is U.S. Navy's victory.");
					break;

				case 102:
					len[1] = wsprintf(ach[1], "Battles around Guadalcanal.");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "Victory Line : Total annihilation of Japanese bases on Guadalcanal");
					len[6] = wsprintf(ach[6], "and complete of U.S.Navy's airfield is their victory.");
					break;

				case 103:
					len[1] = wsprintf(ach[1], "Battles off and over Japan.");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "Victory Line : Total annihilation of all of Japanese cities on their land");
					len[6] = wsprintf(ach[6], "is U.S.Navy's victory.");
					break;



				case 104:
					len[1] = wsprintf(ach[1], "Battle of south pacific.");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "Victory Line : Total annihilation of all of Japanese bases");
					len[6] = wsprintf(ach[6], "on Bougainville island.");
					break;

				case 105:
					len[1] = wsprintf(ach[1], "Battle of south pacific.");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");

					len[4] = wsprintf(ach[4], "Victory Line : Total annihilation of all of Japanese bases");
					len[5] = wsprintf(ach[5], "on Bougainville island. Or opposite conditions for");
					len[6] = wsprintf(ach[6], "Guadalcanal which U.S.Navy is defending is Japanese victory.");
					break;


				case 106:
					len[1] = wsprintf(ach[1], "Battles at Guadalcanal.");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");

					len[4] = wsprintf(ach[4], "");
					len[5] = wsprintf(ach[5], "Victory Line : Total annihilation of all of enemy's bases");
					len[6] = wsprintf(ach[6], "and remaining over 4 of bases on Guadalcanal island ");
					break;



				default:
					len[1] = wsprintf(ach[1], "");
					len[2] = wsprintf(ach[2], "");
					len[3] = wsprintf(ach[3], "");
					len[4] = wsprintf(ach[4], "");

					len[5] = wsprintf(ach[5], "");
					len[6] = wsprintf(ach[6], "");
					break;
				}


#endif


if( mode==CNCT_GAME_SETTING )
	{
			for( n=0; n<=6; n++)
				{
				SetTextColor(hdc, RGB(255, 255, 255));
				TextOut(hdc, rx, 500+(n*25), ach[n], len[n]);
				}
	}
else
	{
	for( n=0; n<=6; n++)
		{
		SetTextColor(hdc, RGB(126, 126, 126));
		TextOut(hdc, rx, 500+(n*25), ach[n], len[n]);
		}
	}




			// コンフィギュレーション

if(	mode==CNCT_CNFG_SETTING )
	{
			rx=630-120;

#if !LNGG_VER
			len[0] = wsprintf(ach[0], "コンフィギュレーション");
#else
			len[0] = wsprintf(ach[0], "Configuration");
#endif
			SetTextColor(hdc, RGB(255, 255, 255));



			// 勝敗判定
			if(you_are_host!=0)
				{
				dstn_rect.left=rx+50;
				dstn_rect.top=50;
				TextOut(hdc, dstn_rect.left, dstn_rect.top+(1*25), ach[0], len[0]);


#if !LNGG_VER
				len[0] = wsprintf(ach[0], "勝敗判定の切り替え ->>>");
#else
				len[0] = wsprintf(ach[0], "Decision ->>>");
#endif
				dstn_rect.left=rx;
				dstn_rect.top=125;
				dstn_rect.right=dstn_rect.left+(len[0]*12);
				dstn_rect.bottom=dstn_rect.top+24;
				if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 )
					{
					SetTextColor(hdc, RGB(255, 0, 0));
					if( lf_btn==3 )
						{
						decision_sw=(byte)(decision_sw==0 ? 1 : 0);
						}
					}
				else
					{
					SetTextColor(hdc, RGB(255, 255, 255));
					}

				TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);

				}

			if(decision_sw==0)
				{
#if !LNGG_VER
				len[0] = wsprintf(ach[0], "勝敗条件は無効");
#else
				len[0] = wsprintf(ach[0], "Invalid");
#endif
				}
			else
				{
#if !LNGG_VER
				len[0] = wsprintf(ach[0], "勝敗条件は有効");
#else
				len[0] = wsprintf(ach[0], "Valid");
#endif
				}
			SetTextColor(hdc, RGB(255, 255, 255));
			dstn_rect.left=rx;
			dstn_rect.top=150;
			TextOut(hdc, dstn_rect.left, dstn_rect.top/*+(1*25)*/, ach[0], len[0]);






			// 補給割当増加率（ホスト側）
#if !LNGG_VER
			len[0] = wsprintf(ach[0], "補給割当増加率（ホスト側）");
#else
			len[0] = wsprintf(ach[0], "increase rate of supply pts(Host)");
#endif
			SetTextColor(hdc, RGB(255, 255, 255));
			dstn_rect.left=rx;
			dstn_rect.top=200;
			TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);

			if( you_are_host!=0 )
				{
#if !LNGG_VER
				len[0] = wsprintf(ach[0], "増やす ->>>");
#else
				len[0] = wsprintf(ach[0], "Increment ->>>");
#endif
				SetTextColor(hdc, RGB(255, 255, 255));
				dstn_rect.left=rx;
				dstn_rect.top=225;
				dstn_rect.right=dstn_rect.left+(len[0]*10);
				dstn_rect.bottom=dstn_rect.top+24;
				if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 )
					{
					SetTextColor(hdc, RGB(255, 0, 0));
					if( lf_btn==3 )
						{
						spry_rate[0]++;
						flg=1;
						}
					}
				else
					{
					SetTextColor(hdc, RGB(255, 255, 255));
					}
				TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);


#if !LNGG_VER
				len[0] = wsprintf(ach[0], "減らす ->>>");
#else
				len[0] = wsprintf(ach[0], "Decrement ->>>");
#endif
				SetTextColor(hdc, RGB(255, 255, 255));
				dstn_rect.left=rx;
				dstn_rect.top=250;
				dstn_rect.right=dstn_rect.left+(len[0]*10);
				dstn_rect.bottom=dstn_rect.top+24;
				if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 )
					{
					SetTextColor(hdc, RGB(255, 0, 0));
					if( lf_btn==3 && spry_rate[0]!=0)
						{
						spry_rate[0]--;
						flg=1;
						}
					}
				else
					{
					SetTextColor(hdc, RGB(255, 255, 255));
					}


				TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
				}


			len[0] = wsprintf(ach[0], "%d pts",spry_rate[0]);
			SetTextColor(hdc, RGB(255, 255, 255));
			dstn_rect.left=rx+150;
			dstn_rect.top=237;
			TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);







			// 補給値反転地点
#if !LNGG_VER
			len[0] = wsprintf(ach[0], "補給値反転地点");
#else
			len[0] = wsprintf(ach[0], "Reverse of suplly pts");
#endif
			SetTextColor(hdc, RGB(255, 255, 255));
			dstn_rect.left=rx+250;
			dstn_rect.top=200;
			TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);

			if( you_are_host!=0 )
				{
#if !LNGG_VER
				len[0] = wsprintf(ach[0], "増やす ->>>");
#else
				len[0] = wsprintf(ach[0], "Increment ->>>");
#endif
				SetTextColor(hdc, RGB(255, 255, 255));
				dstn_rect.left=rx+250;
				dstn_rect.top=225;
				dstn_rect.right=dstn_rect.left+(len[0]*10);
				dstn_rect.bottom=dstn_rect.top+24;
				if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 )
					{
					SetTextColor(hdc, RGB(255, 0, 0));
					if( (lf_btn==1||lf_btn==2) )
						{
						rvrs_time++;
						flg=1;
						}
					}
				else
					{
					SetTextColor(hdc, RGB(255, 255, 255));
					}
				TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);


#if !LNGG_VER
				len[0] = wsprintf(ach[0], "減らす ->>>");
#else
				len[0] = wsprintf(ach[0], "Decrement ->>>");
#endif
				SetTextColor(hdc, RGB(255, 255, 255));
				dstn_rect.left=rx+250;
				dstn_rect.top=250;
				dstn_rect.right=dstn_rect.left+(len[0]*10);
				dstn_rect.bottom=dstn_rect.top+24;
				if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 )
					{
					SetTextColor(hdc, RGB(255, 0, 0));
					if( (lf_btn==1||lf_btn==2) && rvrs_time!=0)
						{
						rvrs_time--;
						flg=1;
						}
					}
				else
					{
					SetTextColor(hdc, RGB(255, 255, 255));
					}


				TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
				}


#if !LNGG_VER
			if( rvrs_time!=0 )
				len[0] = wsprintf(ach[0], "経過時間 %d",rvrs_time*100);
			else
				len[0] = wsprintf(ach[0], "反転無し");
#else
			if( rvrs_time )
				len[0] = wsprintf(ach[0], "TIME: %d",rvrs_time*100);
			else
				len[0] = wsprintf(ach[0], "No reverse");
#endif
			SetTextColor(hdc, RGB(255, 255, 255));
			dstn_rect.left=rx+150+250-30;
			dstn_rect.top=237;
			TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);







			// 補給割当増加率（ゲスト側）
#if !LNGG_VER
			len[0] = wsprintf(ach[0], "補給割当増加率（ゲスト側）");
#else
			len[0] = wsprintf(ach[0], "Increse rate of supply pts(guest)");
#endif
			SetTextColor(hdc, RGB(255, 255, 255));
			dstn_rect.left=rx;
			dstn_rect.top=300;
			TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);

			if( you_are_host!=0 )
				{
				
#if !LNGG_VER
				len[0] = wsprintf(ach[0], "増やす ->>>");
#else
				len[0] = wsprintf(ach[0], "Increment ->>>");
#endif
				SetTextColor(hdc, RGB(255, 255, 255));
				dstn_rect.left=rx;
				dstn_rect.top=325;
				dstn_rect.right=dstn_rect.left+(len[0]*10);
				dstn_rect.bottom=dstn_rect.top+24;
				if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 )
					{
					SetTextColor(hdc, RGB(255, 0, 0));
					if( lf_btn==3 )
						{
						spry_rate[1]++;
						flg=1;
						}
					}
				else
					{
					SetTextColor(hdc, RGB(255, 255, 255));
					}
				TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);

#if !LNGG_VER
				len[0] = wsprintf(ach[0], "減らす ->>>");
#else
				len[0] = wsprintf(ach[0], "Decrement ->>>");
#endif
				SetTextColor(hdc, RGB(255, 255, 255));
				dstn_rect.left=rx;
				dstn_rect.top=350;
				dstn_rect.right=dstn_rect.left+(len[0]*10);
				dstn_rect.bottom=dstn_rect.top+24;
				if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 )
					{
					SetTextColor(hdc, RGB(255, 0, 0));
					if( lf_btn==3 && spry_rate[1]!=0)
						{
						spry_rate[1]--;
						flg=1;
						}
					}
				else
					{
					SetTextColor(hdc, RGB(255, 255, 255));
					}
				TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
				}

			len[0] = wsprintf(ach[0], "%d pts",spry_rate[1]);
			SetTextColor(hdc, RGB(255, 255, 255));
			dstn_rect.left=rx+150;
			dstn_rect.top=337;
			TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);








			// 補給値反転ルール
			if( rvrs_time!=0 )
				{
#if !LNGG_VER
				len[0] = wsprintf(ach[0], ( you_are_host!=0 ? "補給値反転ルール->>>" : "補給値反転ルール" ));
#else
				len[0] = wsprintf(ach[0], "Reverse rule");
#endif
				SetTextColor(hdc, RGB(255, 255, 255));
				dstn_rect.left=rx+250;
				dstn_rect.top=300;
				dstn_rect.right=dstn_rect.left+(len[0]*10);
				dstn_rect.bottom=dstn_rect.top+24;

				if( you_are_host!=0 )
					{
					if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 )
						{
						SetTextColor(hdc, RGB(255, 0, 0));
						if( lf_btn==3 )
							{
							rvrs_rule=(short)(rvrs_rule==0 ? 1 : 0);
							flg=1;
							}
						}
					else
						{
						SetTextColor(hdc, RGB(255, 255, 255));
						}
					}
				TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);



				if( rvrs_rule==0 )
					{
#if !LNGG_VER
					len[0] = wsprintf(ach[0], "一度だけ");
#else
					len[0] = wsprintf(ach[0], "Once a game");
#endif
					SetTextColor(hdc, RGB(255, 255, 255));
					dstn_rect.left=rx+250;
					dstn_rect.top=325;
					TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
					}
				else
					{
#if !LNGG_VER
					len[0] = wsprintf(ach[0], "指定時間毎");
#else
					len[0] = wsprintf(ach[0], "Every specified TIME");
#endif
					SetTextColor(hdc, RGB(255, 255, 255));
					dstn_rect.left=rx+250;
					dstn_rect.top=325;
					TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
					}
				}






			// 初期補給割当（ホスト側）
#if !LNGG_VER
			len[0] = wsprintf(ach[0], "初期補給割当（ホスト側）");
#else
			len[0] = wsprintf(ach[0], "Supply pts on start(Host)");
#endif
			SetTextColor(hdc, RGB(255, 255, 255));
			dstn_rect.left=rx;
			dstn_rect.top=400;
			TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);

			if( you_are_host!=0 )
				{
//				len[0] = wsprintf(ach[0], "増やす ->>>");
#if !LNGG_VER
				len[0] = wsprintf(ach[0], "増やす ->>>");
#else
				len[0] = wsprintf(ach[0], "Increment ->>>");
#endif

				SetTextColor(hdc, RGB(255, 255, 255));
				dstn_rect.left=rx;
				dstn_rect.top=425;
				dstn_rect.right=dstn_rect.left+(len[0]*10);
				dstn_rect.bottom=dstn_rect.top+24;
				if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 )
					{
					SetTextColor(hdc, RGB(255, 0, 0));
					if( (lf_btn==1||lf_btn==2) )
						{
						first_spry_pt[0]+=50;
						flg=1;
						}
					}
				else
					{
					SetTextColor(hdc, RGB(255, 255, 255));
					}
				TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);


//				len[0] = wsprintf(ach[0], "減らす ->>>");
#if !LNGG_VER
				len[0] = wsprintf(ach[0], "減らす ->>>");
#else
				len[0] = wsprintf(ach[0], "Decrement ->>>");
#endif

				SetTextColor(hdc, RGB(255, 255, 255));
				dstn_rect.left=rx;
				dstn_rect.top=450;
				dstn_rect.right=dstn_rect.left+(len[0]*10);
				dstn_rect.bottom=dstn_rect.top+24;
				if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 )
					{
					SetTextColor(hdc, RGB(255, 0, 0));
					if( (lf_btn==1||lf_btn==2) && first_spry_pt[0]!=0)
						{
						first_spry_pt[0]-=50;
						if( first_spry_pt[0]<0)
							first_spry_pt[0]=0;
						flg=1;
						}
					}
				else
					{
					SetTextColor(hdc, RGB(255, 255, 255));
					}
				TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
				}

			len[0] = wsprintf(ach[0], "%d pts",first_spry_pt[0]);
			SetTextColor(hdc, RGB(255, 255, 255));
			dstn_rect.left=rx+150;
			dstn_rect.top=437;
			TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);




			// 初期補給割当（ゲスト側）
#if !LNGG_VER
			len[0] = wsprintf(ach[0], "初期補給割当（ゲスト側）");
#else
			len[0] = wsprintf(ach[0], "Supply pts on start(Guest)");
#endif
			SetTextColor(hdc, RGB(255, 255, 255));
			dstn_rect.left=rx;
			dstn_rect.top=500;
			TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);

			if( you_are_host!=0 )
				{
//				len[0] = wsprintf(ach[0], "増やす ->>>");
#if !LNGG_VER
				len[0] = wsprintf(ach[0], "増やす ->>>");
#else
				len[0] = wsprintf(ach[0], "Increment ->>>");
#endif

				SetTextColor(hdc, RGB(255, 255, 255));
				dstn_rect.left=rx;
				dstn_rect.top=525;
				dstn_rect.right=dstn_rect.left+(len[0]*10);
				dstn_rect.bottom=dstn_rect.top+24;
				if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 )
					{
					SetTextColor(hdc, RGB(255, 0, 0));
					if( (lf_btn==1||lf_btn==2) )
						{
						first_spry_pt[1]+=50;
						flg=1;
						}
					}
				else
					{
					SetTextColor(hdc, RGB(255, 255, 255));
					}
				TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);


//				len[0] = wsprintf(ach[0], "減らす ->>>");
#if !LNGG_VER
				len[0] = wsprintf(ach[0], "減らす ->>>");
#else
				len[0] = wsprintf(ach[0], "Decrement ->>>");
#endif

				SetTextColor(hdc, RGB(255, 255, 255));
				dstn_rect.left=rx;
				dstn_rect.top=550;
				dstn_rect.right=dstn_rect.left+(len[0]*10);
				dstn_rect.bottom=dstn_rect.top+24;
				if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 )
					{
					SetTextColor(hdc, RGB(255, 0, 0));
					if( (lf_btn==1||lf_btn==2) && first_spry_pt[1]!=0)
						{
						first_spry_pt[1]-=50;
						if( first_spry_pt[1]<0)
							first_spry_pt[1]=0;
						flg=1;
						}
					}
				else
					{
					SetTextColor(hdc, RGB(255, 255, 255));
					}
				TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
				}

			len[0] = wsprintf(ach[0], "%d pts",first_spry_pt[1]);
			SetTextColor(hdc, RGB(255, 255, 255));
			dstn_rect.left=rx+150;
			dstn_rect.top=537;
			TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);





			if(you_are_host!=0)
				{
#if !LNGG_VER
				len[0] = wsprintf(ach[0], "増援ユニット制御 ->>>");
#else
				len[0] = wsprintf(ach[0], "Supply unit control ->>>");
#endif
				dstn_rect.left=rx;
				dstn_rect.top=600;
				dstn_rect.right=dstn_rect.left+(len[0]*12);
				dstn_rect.bottom=dstn_rect.top+24;
				if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 )
					{
					SetTextColor(hdc, RGB(255, 0, 0));
					if( lf_btn==3 )
						{
						arrival_cont++;
						if(arrival_cont>=7)
							arrival_cont=0;
						}
					}
				else
					{
					SetTextColor(hdc, RGB(255, 255, 255));
					}

				TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);

				}



			switch( arrival_cont )
				{
#if !LNGG_VER
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
#else

				case 0:
					len[0] = wsprintf(ach[0], "All of possible(%d)",arrival_cont);
					break;
				case 1:
					len[0] = wsprintf(ach[0], "Nothing of possible(%d)",arrival_cont);
					break;
				case 2:
					len[0] = wsprintf(ach[0], "Possible except transpot(%d)",arrival_cont);
					break;
				case 3:
					len[0] = wsprintf(ach[0], "Possible only transport(%d)",arrival_cont);
					break;
				case 4:
					len[0] = wsprintf(ach[0], "Possible only combat fleet(%d)",arrival_cont);
					break;
				case 5:
					len[0] = wsprintf(ach[0], "Possible only Airplane(%d)",arrival_cont);
					break;
				case 6:
					len[0] = wsprintf(ach[0], "Possible except transport(port)(%d)",arrival_cont);
					break;

#endif

				}


			SetTextColor(hdc, RGB(255, 255, 255));
			dstn_rect.left=rx;
			dstn_rect.top=625;
			TextOut(hdc, dstn_rect.left, dstn_rect.top/*+(1*25)*/, ach[0], len[0]);







			// シナリオセッティングへ戻る
			if(you_are_host!=0)
				{
#if !LNGG_VER
				len[0] = wsprintf(ach[0], "シナリオセッティングへ戻る->>>");
#else
				len[0] = wsprintf(ach[0], "Back to mission menu ->>>");
#endif
				dstn_rect.left=rx;
				dstn_rect.top=675;
				dstn_rect.right=dstn_rect.left+(len[0]*12);
				dstn_rect.bottom=dstn_rect.top+24;
				if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 && rival_mode==CNCT_CNFG_SETTING )
					{
					SetTextColor(hdc, RGB(255, 0, 0));
					if( lf_btn==3 )
						{
						m=sinario;
						go_cnct_game_setting();
						sinario=(short)m;

						dp_flag.dwType = GO_GAME_SETTING;
//t						lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, DPSEND_GUARANTEED, &dp_flag, sizeof(_DP_FLAG) );

						bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
						bufferDesc.pBufferData  = (byte*) &dp_flag;
						g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
						}
					}
				else
					{
					SetTextColor(hdc, RGB(255, 255, 255));
					}
				TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
				}




			// 戦闘開始
			if(you_are_host!=0)
				{
#if !LNGG_VER
				len[0] = wsprintf(ach[0], "ゲームスタート ->>>");
#else
				len[0] = wsprintf(ach[0], "Game Start ->>>");
#endif
				dstn_rect.left=rx;
				dstn_rect.top=700;
				dstn_rect.right=dstn_rect.left+(len[0]*12);
				dstn_rect.bottom=dstn_rect.top+24;
				if( pt_in_rect(ref dstn_rect,crsr_pt.x,crsr_pt.y)!=0 && rival_mode==CNCT_CNFG_SETTING )
					{
					SetTextColor(hdc, RGB(255, 0, 0));
					if( lf_btn==3 )
						{
						// ホストの選択状態をゲストにセンドします。
						srand( (uint)time( null ) );
						cnct_game_rnd_sheed=(short)rnd(65536);

						dp_data_1.dwType = OUT_SETUP;
						dp_data_1.data[0] = cnct_game_rnd_sheed;

						bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
						bufferDesc.pBufferData  = (byte*) &dp_data_1;
						g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );


						dp_data_1.dwType = OUT_CNFG_SETTING;
						dp_data_1.data[0] = (short)host_side;
						dp_data_1.data[1] = sinario;

						dp_data_1.data[2] = spry_rate[0];
						dp_data_1.data[3] = spry_rate[1];

						dp_data_1.data[4] = decision_sw;

						dp_data_1.data[5] = first_spry_pt[0];
						dp_data_1.data[6] = first_spry_pt[1];

						dp_data_1.data[7] = arrival_cont;

						dp_data_1.data[8] = rvrs_time;
						dp_data_1.data[9] = rvrs_rule;


						bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
						bufferDesc.pBufferData  = (byte*) &dp_data_1;
						g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );


						mode=CMBT;
						}
					}
				else
					{
					SetTextColor(hdc, RGB(255, 255, 255));
					}
				TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
				}
			else
				{
				if( join_game_start==OUT_CNFG_SETTING )
					{
					mode=CMBT;
					join_game_start=0;
					}
				}



			if( you_are_host!=0 && rival_mode==mode/*==CNCT_CNFG_SETTING*/ && (FrameCount%10)==0 )
				{
				// ホストの選択状態をゲストにセンドします。
				dp_data_1.dwType = SIDE_AND_SINARIO;
				dp_data_1.data[0] = (short)host_side;
				dp_data_1.data[1] = sinario;

				dp_data_1.data[2] = spry_rate[0];
				dp_data_1.data[3] = spry_rate[1];

				dp_data_1.data[4] = decision_sw;

				dp_data_1.data[5] = first_spry_pt[0];
				dp_data_1.data[6] = first_spry_pt[1];

				dp_data_1.data[7] = arrival_cont;

				dp_data_1.data[8] = rvrs_time;
				dp_data_1.data[9] = rvrs_rule;

				bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
				bufferDesc.pBufferData  = (byte*) &dp_data_1;
				g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, EASY_SEND );

				}

	}


			IDirectDrawSurface_ReleaseDC(lpDDSBack, hdc);
			}














//	if(0 || cnct_game)
//		{

//t		key_cont();		// チャット用
/*
		if ( IDirectDrawSurface_GetDC(lpDDSBack, &hdc) == DD_OK ) 
			{
			SetBkMode(hdc, TRANSPARENT);
			SelectObject(hdc, gameFont_1);

			SetTextColor(hdc, RGB(255, 255, 0));



			if(my_chat_dsp_time)
				{
				len[0] = wsprintf(ach[0], my_chat );
				TextOut(hdc, 10, 200+170, ach[0], len[0]);
				my_chat_dsp_time--;
				if(my_chat_dsp_time==0)
					{
					for(m=0;m<128;m++)
						{
						my_chat[m]=0;
						}
					}
				}


			if(friend_chat_dsp_time)
				{
				len[0] = wsprintf(ach[0], friend_chat );
				TextOut(hdc, 10, 240+170, ach[0], len[0]);
				friend_chat_dsp_time--;
				if(friend_chat_dsp_time==0)
					{
					for(m=0;m<128;m++)
						{
						friend_chat[m]=0;
						}
					}
				}
			}
		IDirectDrawSurface_ReleaseDC(lpDDSBack, hdc);
*/


//		}



	if( mode==CMBT )
		{
		cnct_game_init();
		}
	else if( rival_mode!=0 && (FrameCount%40)==0 )
		{

		// 現在のモードをライバルに送る。
		dp_flag.dwType = RIVAL_MODE;
		dp_flag.rival_mode=mode;
//t		lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer, 0 /*DPSEND_GUARANTEED*/, &dp_flag, sizeof(_DP_FLAG) );

		bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
		bufferDesc.pBufferData  = (byte*) &dp_flag;
		g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, EASY_SEND );
		}
	}






//============================================================================
// 通信対戦セットアップ
// とにかく互いに通信するまで。
//----------------------------------------------------------------------------
public void	cnct_game_setup()
	{
#if false
	RECT	src_rect,field_rect,dstn_rect;
	int		m,n,g,no1,i,wrk,wrk2,wrk3,menu[7],menu2[7];
    char ach[12][128];
    int len[12];
	HDC					hdc;
	BYTE	cBuf[256];
	DPNAME		dpName;
	HRESULT 	hr;



	DP_DATA_1	dp_data_1;
	DP_DATA_1	*lp_dp_data_1;




	// マウス情報
	GetCursorPos(&crsr_pt);
	if(!scrn_mode)
		ScreenToClient(hwnd, &crsr_pt);
	GetKeyboardState(cBuf);


	if ( IDirectDrawSurface_GetDC(lpDDSBack, &hdc) == DD_OK ) 
		{
		SetBkMode(hdc, TRANSPARENT);
		SelectObject(hdc, gameFont_1);

		SetTextColor(hdc, RGB(255, 255, 255));

#if !LNGG_VER
		len[0] = wsprintf(ach[0], "通信セットアップ");
		TextOut(hdc, 300+50, 80-26, ach[0], len[0]);
		len[0] = wsprintf(ach[0], "Ｆ４：接続解除　Ｆ６：接続ダイアログ");
		TextOut(hdc, 290, 80-26+26, ach[0], len[0]);
#else
		len[0] = wsprintf(ach[0], "Connection set up");
		TextOut(hdc, 300+50, 80-26, ach[0], len[0]);
		len[0] = wsprintf(ach[0], "F4: Kill Connection  F6: Connection Dialog");
		TextOut(hdc, 290, 80-26+26, ach[0], len[0]);
#endif









		// あなたの名前
#if !LNGG_VER
		len[0] = wsprintf(ach[0], "自分の名前");
#else
		len[0] = wsprintf(ach[0], "My name :");
#endif
		TextOut(hdc, 200, 120, ach[0], len[0]);
		len[0] = wsprintf(ach[0], g_strLocalPlayerName);
		TextOut(hdc, 500, 120, ach[0], len[0]);

		//　対戦相手の名前
#if !LNGG_VER
		len[0] = wsprintf(ach[0], "対戦相手の名前");
#else
		len[0] = wsprintf(ach[0], "Rival name :");
#endif
		TextOut(hdc, 200, 160, ach[0], len[0]);
		len[0] = wsprintf(ach[0], g_strLocalRivalPlayerName);
		TextOut(hdc, 500, 160, ach[0], len[0]);






//		if( 0 || cnct_game  )
//			{
			if( cnct_game && you_are_host)
				{
#if !LNGG_VER
				len[0] = wsprintf(ach[0], "通信対戦開始");
#else
				len[0] = wsprintf(ach[0], "- Click to start -");
#endif

				dstn_rect.left=500;
				dstn_rect.top=600;
				dstn_rect.right=dstn_rect.left+(len[0]*12);
				dstn_rect.bottom=dstn_rect.top+24;

				if( pt_in_rect(&dstn_rect,crsr_pt.x,crsr_pt.y) )
					{
					SetTextColor(hdc, RGB(255, 0, 0));
					if( lf_btn==3 )
						{
						srand( (unsigned)time( NULL ) );
						cnct_game_rnd_sheed=rnd(65536);

						rnd_count=0;

						dp_data_1.dwType = OUT_SETUP;
						wsprintf(dp_data_1.my_name, g_strLocalPlayerName);	
						dp_data_1.data[0] = cnct_game_rnd_sheed;

//t						lpDirectPlay4A->lpVtbl->Send(lpDirectPlay4A, dpidPlayer, dpidRivalPlayer/*idFrom*/, DPSEND_GUARANTEED, &dp_data_1, sizeof(DP_DATA_1) );

						//sinario=4;
						//mode=CMBT;

						go_cnct_game_setting();

						// リジュームデータがホストをやってたかしらべる。
						load_on_resume(1);
						you_were_host=you_are_host;
						you_are_host=1;					//ここにくるのはホストだけ
						sinario=0;
						}
					}
				else
					{
					SetTextColor(hdc, RGB(255, 255, 255));
					}

				TextOut(hdc, 500, 600, ach[0], len[0]);
				}
			else
				{
				if(join_game_start)
					{
					join_game_start=0;
					go_cnct_game_setting();
					}
				}
//			}



		IDirectDrawSurface_ReleaseDC(lpDDSBack, hdc);
		}


/**
	if( mode==CMBT )
		{
		game_init();
		}
**/
#endif
	}
}
