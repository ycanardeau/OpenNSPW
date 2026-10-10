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

// Port of demo.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{

[Original("exist_auto_save")] public int	HasAutoSave;

//============================================================================
// デモ画面
//----------------------------------------------------------------------------
[Original("demo_func")]
public void	UpdateTitle()
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
		dp_flag.dwType = MessageType.RivalMode;
		dp_flag.rival_mode=(short)Mode;
		bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
		bufferDesc.pBufferData  = (byte*) &dp_flag;
		g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1, 0, null, ref hAsync, EASY_SEND );

		// 自分のバージョンを相手に伝える。
		dp_data_20.dwType = MessageType.RivalVersion;
		wsprintf( dp_data_20.friend_chat, "%s",VER );
		bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_20));
		bufferDesc.pBufferData  = (byte*) &dp_data_20;
		g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, EASY_SEND );
		}

	if( TitleTime==0 )
		{
		RivalMode=0;
		wsprintf(RivalVersion, "- - -" );
		}

	ClearFlag=1;

	// ユニットインフォーメィション
	Sprites[TTL_BACK].x=212-50;
	Sprites[TTL_BACK].y=130;

	src_rect.left = 	Sprites[TTL_BACK].base_x;
	src_rect.top = Sprites[TTL_BACK].base_y;
	src_rect.right = Sprites[TTL_BACK].base_x+Sprites[TTL_BACK].wd;
	src_rect.bottom = Sprites[TTL_BACK].base_y+Sprites[TTL_BACK].ht;

	// dstn_rect は ディスティネーションレクタングルです。
	dstn_rect.left=Sprites[TTL_BACK].x;
	dstn_rect.top=Sprites[TTL_BACK].y;

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,0) )
		{
		RestoreSurfaces();
		}

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

			len[0] = wsprintf(ach[0], "RIVAL VERSION = %s",RivalVersion );
			SetTextColor(hdc, RGB(255, 255, 255));
			TextOut(hdc, 385, 99, ach[0], len[0]);

			IDirectDrawSurface_ReleaseDC(lpDDSBack, hdc);
			}

	if( LeftButton==3 )
		{
		if( IsEditingMap!=0 )
			{
			GoToGameSetting();
			}
		else if( ActivePlayerCount==2 && RivalMode==GameMode.Title && IsHost!=0 )
			{
			GoToGameSetting();

			if( 1!=0 )
				{
				dp_flag.dwType = MessageType.GoToGameSetting;
				dp_flag.rival_mode=(short)Mode;
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
					WasHost=0;
					}
				else
					{
					// ファイルはあった。
					LoadResume(1);
					WasHost=IsHost;		// 前回ホストだったら１が代入
					CloseHandle(hFile);
					}
				IsHost=1;						// ここに来るのはホストなのでこれでいい。
				ScenarioNumber=0;

				}
			}

//	MessageBox(hwndApp,"bitmap_surface","残念！",MB_OK | MB_ICONSTOP);

/*
		if( DialogBox(hInstance, MAKEINTRESOURCE(IDD_SPDIALOG), hWnd, (DLGPROC)SvDlgProc) )
			{
			// セッション選択のダイアログ・ボックスを表示する
			if (DialogBox(hInstance, MAKEINTRESOURCE(IDD_SESSIONDLG), hwnd, (DLGPROC)SessionDlgProc))
*/
//		DialogBoxParam(hInstance, MAKEINTRESOURCE(IDD_DP), hwnd, cnctn_dialog_proc,
		}

	if(   RightButton==3 )
		{

		RightButton=0;

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

	TitleTime++;

	}

//============================================================================
//
// コネクトゲームスタートの値
//----------------------------------------------------------------------------
[Original("go_cnct_game_setting")]
public void GoToGameSetting()
	{

	ScenarioNumber=0;

	Mode=GameMode.GameSetting;
	HostSide=0;
	IsDecisionEnabled=1;
	ArrivalControl=0;

	SupplyRates[0]=0;		// Host
	SupplyRates[1]=0;		// Guest

	InitialSupplyPoints[0]=0;		// Host
	InitialSupplyPoints[1]=0;		// Guest

	if( hwndChatDlg!=null )
		{
		DestroyWindow(hwndChatDlg);
		hwndChatDlg=null;
		}

#if SND_SW
	lpDSB_[SEA1][0].Stop();		//
#endif

HANDLE	hFile;

	hFile=CreateFile("Saved\\auto_save.dat", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
													null, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, null);

	if( INVALID_HANDLE_VALUE==hFile )
		{
		// ファイルありませんでした。
		HasAutoSave=0;
		}
	else
		{
		// ファイルはあった。
		HasAutoSave=1;
		CloseHandle(hFile);
		}

	}
private void UpdateDecisionSetting(int rx, ref HDC hdc, ref Array12<Array128<byte>> ach, ref Array12<int> len)
	{
	RECT dstn_rect;
	if(IsHost!=0)
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
		if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( LeftButton==3 )
				{
				IsDecisionEnabled=(byte)(IsDecisionEnabled==0 ? 1 : 0);
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}

		TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);

		}

	if(IsDecisionEnabled==0)
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
	TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
	}
private void UpdateHostSupplyRateSetting(ref Array12<int> len, ref Array12<Array128<byte>> ach, ref HDC hdc, int rx)
	{
	RECT dstn_rect;
	int flg;
#if !LNGG_VER
	len[0] = wsprintf(ach[0], "補給割当増加率（ホスト側）");
#else
	len[0] = wsprintf(ach[0], "increase rate of supply pts(Host)");
#endif
	SetTextColor(hdc, RGB(255, 255, 255));
	dstn_rect.left=rx;
	dstn_rect.top=200;
	TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);

	if( IsHost!=0 )
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
		if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( LeftButton==3 )
				{
				SupplyRates[0]++;
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
		if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( LeftButton==3 && SupplyRates[0]!=0)
				{
				SupplyRates[0]--;
				flg=1;
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}

		TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
		}

	len[0] = wsprintf(ach[0], "%d pts",SupplyRates[0]);
	SetTextColor(hdc, RGB(255, 255, 255));
	dstn_rect.left=rx+150;
	dstn_rect.top=237;
	TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
	}
private void UpdateSwapTimeSetting(ref Array12<int> len, ref Array12<Array128<byte>> ach, ref HDC hdc, int rx)
	{
	RECT dstn_rect;
	int flg;
#if !LNGG_VER
	len[0] = wsprintf(ach[0], "補給値反転地点");
#else
	len[0] = wsprintf(ach[0], "Reverse of suplly pts");
#endif
	SetTextColor(hdc, RGB(255, 255, 255));
	dstn_rect.left=rx+250;
	dstn_rect.top=200;
	TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);

	if( IsHost!=0 )
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
		if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( (LeftButton==1||LeftButton==2) )
				{
				SwapTime++;
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
		if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( (LeftButton==1||LeftButton==2) && SwapTime!=0)
				{
				SwapTime--;
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
	if( SwapTime!=0 )
		len[0] = wsprintf(ach[0], "経過時間 %d",SwapTime*100);
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
	}
private void UpdateGuestSupplyRateSetting(ref Array12<int> len, ref Array12<Array128<byte>> ach, ref HDC hdc, int rx)
	{
	RECT dstn_rect;
	int flg;
#if !LNGG_VER
	len[0] = wsprintf(ach[0], "補給割当増加率（ゲスト側）");
#else
	len[0] = wsprintf(ach[0], "Increse rate of supply pts(guest)");
#endif
	SetTextColor(hdc, RGB(255, 255, 255));
	dstn_rect.left=rx;
	dstn_rect.top=300;
	TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);

	if( IsHost!=0 )
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
		if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( LeftButton==3 )
				{
				SupplyRates[1]++;
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
		if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( LeftButton==3 && SupplyRates[1]!=0)
				{
				SupplyRates[1]--;
				flg=1;
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}
		TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
		}

	len[0] = wsprintf(ach[0], "%d pts",SupplyRates[1]);
	SetTextColor(hdc, RGB(255, 255, 255));
	dstn_rect.left=rx+150;
	dstn_rect.top=337;
	TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
	}
private void UpdateSwapRuleSetting(ref Array12<int> len, ref Array12<Array128<byte>> ach, ref HDC hdc, int rx)
	{
	RECT dstn_rect;
	int flg;
	if( SwapTime!=0 )
		{
#if !LNGG_VER
		len[0] = wsprintf(ach[0], ( IsHost!=0 ? "補給値反転ルール->>>" : "補給値反転ルール" ));
#else
		len[0] = wsprintf(ach[0], "Reverse rule");
#endif
		SetTextColor(hdc, RGB(255, 255, 255));
		dstn_rect.left=rx+250;
		dstn_rect.top=300;
		dstn_rect.right=dstn_rect.left+(len[0]*10);
		dstn_rect.bottom=dstn_rect.top+24;

		if( IsHost!=0 )
			{
			if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 )
				{
				SetTextColor(hdc, RGB(255, 0, 0));
				if( LeftButton==3 )
					{
					SwapRule=(short)(SwapRule==0 ? 1 : 0);
					flg=1;
					}
				}
			else
				{
				SetTextColor(hdc, RGB(255, 255, 255));
				}
			}
		TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);

		if( SwapRule==0 )
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
	}
private void UpdateHostInitialSupplyPointsSetting(ref Array12<int> len, ref Array12<Array128<byte>> ach, ref HDC hdc, int rx)
	{
	RECT dstn_rect;
	int flg;
#if !LNGG_VER
	len[0] = wsprintf(ach[0], "初期補給割当（ホスト側）");
#else
	len[0] = wsprintf(ach[0], "Supply pts on start(Host)");
#endif
	SetTextColor(hdc, RGB(255, 255, 255));
	dstn_rect.left=rx;
	dstn_rect.top=400;
	TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);

	if( IsHost!=0 )
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
		if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( (LeftButton==1||LeftButton==2) )
				{
				InitialSupplyPoints[0]+=50;
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
		if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( (LeftButton==1||LeftButton==2) && InitialSupplyPoints[0]!=0)
				{
				InitialSupplyPoints[0]-=50;
				if( InitialSupplyPoints[0]<0)
					InitialSupplyPoints[0]=0;
				flg=1;
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}
		TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
		}

	len[0] = wsprintf(ach[0], "%d pts",InitialSupplyPoints[0]);
	SetTextColor(hdc, RGB(255, 255, 255));
	dstn_rect.left=rx+150;
	dstn_rect.top=437;
	TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
	}
private void UpdateGuestInitialSupplyPointsSetting(ref Array12<int> len, ref Array12<Array128<byte>> ach, ref HDC hdc, int rx)
	{
	RECT dstn_rect;
	int flg;
#if !LNGG_VER
	len[0] = wsprintf(ach[0], "初期補給割当（ゲスト側）");
#else
	len[0] = wsprintf(ach[0], "Supply pts on start(Guest)");
#endif
	SetTextColor(hdc, RGB(255, 255, 255));
	dstn_rect.left=rx;
	dstn_rect.top=500;
	TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);

	if( IsHost!=0 )
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
		if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( (LeftButton==1||LeftButton==2) )
				{
				InitialSupplyPoints[1]+=50;
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
		if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( (LeftButton==1||LeftButton==2) && InitialSupplyPoints[1]!=0)
				{
				InitialSupplyPoints[1]-=50;
				if( InitialSupplyPoints[1]<0)
					InitialSupplyPoints[1]=0;
				flg=1;
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}
		TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
		}

	len[0] = wsprintf(ach[0], "%d pts",InitialSupplyPoints[1]);
	SetTextColor(hdc, RGB(255, 255, 255));
	dstn_rect.left=rx+150;
	dstn_rect.top=537;
	TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
	}
private void UpdateArrivalControlSetting(ref Array12<int> len, ref Array12<Array128<byte>> ach, int rx, ref HDC hdc)
	{
	RECT dstn_rect;
	if(IsHost!=0)
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
		if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( LeftButton==3 )
				{
				ArrivalControl++;
				if(ArrivalControl>=7)
					ArrivalControl=0;
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}

		TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);

		}

	switch( ArrivalControl )
		{
#if !LNGG_VER
		case 0:
			len[0] = wsprintf(ach[0], "全種増援可(%d)",ArrivalControl);
			break;
		case 1:
			len[0] = wsprintf(ach[0], "全種増援不可(%d)",ArrivalControl);
			break;
		case 2:
			len[0] = wsprintf(ach[0], "輸送船以外可(%d)",ArrivalControl);
			break;
		case 3:
			len[0] = wsprintf(ach[0], "輸送船のみ可(%d)",ArrivalControl);
			break;
		case 4:
			len[0] = wsprintf(ach[0], "戦闘艦船のみ可(%d)",ArrivalControl);
			break;
		case 5:
			len[0] = wsprintf(ach[0], "航空機のみ可(%d)",ArrivalControl);
			break;
		case 6:
			len[0] = wsprintf(ach[0], "輸送船(軍港)以外可(%d)",ArrivalControl);
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
	TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
	}
private void UpdateBackToScenarioSettingButton(ref Array12<int> len, ref Array12<Array128<byte>> ach, int rx, ref HDC hdc, ref _DP_FLAG dp_flag)
	{
	RECT dstn_rect;
	int m =default /* C4701 */;
	if(IsHost!=0)
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
		if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 && RivalMode==GameMode.ConfigSetting )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( LeftButton==3 )
				{
				m=ScenarioNumber;
				GoToGameSetting();
				ScenarioNumber=(short)m;

				dp_flag.dwType = MessageType.GoToGameSetting;

				bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
				bufferDesc.pBufferData  = (byte*) (_DP_FLAG*)Unsafe.AsPointer(ref dp_flag);
				g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}
		TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
		}
	}
private void UpdateStartBattleButton(ref Array12<int> len, ref Array12<Array128<byte>> ach, int rx, ref HDC hdc, ref _DP_DATA_1 dp_data_1)
	{
	RECT dstn_rect;
	if(IsHost!=0)
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
		if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 && RivalMode==GameMode.ConfigSetting )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( LeftButton==3 )
				{
				// ホストの選択状態をゲストにセンドします。
				srand( (uint)time( null ) );
				SharedRandomSeed=(short)Random(65536);

				dp_data_1.dwType = MessageType.LeaveSetup;
				dp_data_1.data[0] = SharedRandomSeed;

				bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
				bufferDesc.pBufferData  = (byte*) (_DP_DATA_1*)Unsafe.AsPointer(ref dp_data_1);
				g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

				dp_data_1.dwType = MessageType.LeaveConfigSetting;
				dp_data_1.data[0] = (short)HostSide;
				dp_data_1.data[1] = ScenarioNumber;

				dp_data_1.data[2] = SupplyRates[0];
				dp_data_1.data[3] = SupplyRates[1];

				dp_data_1.data[4] = IsDecisionEnabled;

				dp_data_1.data[5] = InitialSupplyPoints[0];
				dp_data_1.data[6] = InitialSupplyPoints[1];

				dp_data_1.data[7] = ArrivalControl;

				dp_data_1.data[8] = SwapTime;
				dp_data_1.data[9] = SwapRule;

				bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
				bufferDesc.pBufferData  = (byte*) (_DP_DATA_1*)Unsafe.AsPointer(ref dp_data_1);
				g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

				Mode=GameMode.Battle;
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
		if( JoinGameStart==MessageType.LeaveConfigSetting )
			{
			Mode=GameMode.Battle;
			JoinGameStart=0;
			}
		}
	}
private void SendHostSettings(ref _DP_DATA_1 dp_data_1)
	{
	dp_data_1.dwType = MessageType.SideAndScenario;
	dp_data_1.data[0] = (short)HostSide;
	dp_data_1.data[1] = ScenarioNumber;

	dp_data_1.data[2] = SupplyRates[0];
	dp_data_1.data[3] = SupplyRates[1];

	dp_data_1.data[4] = IsDecisionEnabled;

	dp_data_1.data[5] = InitialSupplyPoints[0];
	dp_data_1.data[6] = InitialSupplyPoints[1];

	dp_data_1.data[7] = ArrivalControl;

	dp_data_1.data[8] = SwapTime;
	dp_data_1.data[9] = SwapRule;

	bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
	bufferDesc.pBufferData  = (byte*) (_DP_DATA_1*)Unsafe.AsPointer(ref dp_data_1);
	g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, EASY_SEND );
	}

private void UpdateConfigSetting(ref Array12<int> len, ref Array12<Array128<byte>> ach, ref HDC hdc, ref _DP_FLAG dp_flag, ref _DP_DATA_1 dp_data_1)
	{
	int rx;
	rx=630-120;

#if !LNGG_VER
	len[0] = wsprintf(ach[0], "コンフィギュレーション");
#else
	len[0] = wsprintf(ach[0], "Configuration");
#endif
	SetTextColor(hdc, RGB(255, 255, 255));

	// 勝敗判定
	UpdateDecisionSetting(rx, ref hdc, ref ach, ref len);

	// 補給割当増加率（ホスト側）
	UpdateHostSupplyRateSetting(ref len, ref ach, ref hdc, rx);

	// 補給値反転地点
	UpdateSwapTimeSetting(ref len, ref ach, ref hdc, rx);

	// 補給割当増加率（ゲスト側）
	UpdateGuestSupplyRateSetting(ref len, ref ach, ref hdc, rx);

	// 補給値反転ルール
	UpdateSwapRuleSetting(ref len, ref ach, ref hdc, rx);

	// 初期補給割当（ホスト側）
	UpdateHostInitialSupplyPointsSetting(ref len, ref ach, ref hdc, rx);

	// 初期補給割当（ゲスト側）
	UpdateGuestInitialSupplyPointsSetting(ref len, ref ach, ref hdc, rx);

	UpdateArrivalControlSetting(ref len, ref ach, rx, ref hdc);

	// シナリオセッティングへ戻る
	UpdateBackToScenarioSettingButton(ref len, ref ach, rx, ref hdc, ref dp_flag);

	// 戦闘開始
	UpdateStartBattleButton(ref len, ref ach, rx, ref hdc, ref dp_data_1);

	if( IsHost!=0 && RivalMode==Mode && (FrameCount%10)==0 )
		{
		// ホストの選択状態をゲストにセンドします。
		SendHostSettings(ref dp_data_1);
		}
	}

private void DrawScenarioDescription(ref Array12<int> len, ref Array12<Array128<byte>> ach)
	{
	switch( ScenarioNumber )
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
	}

private void UpdateScenarioList(int m, ref Array12<int> len, ref HDC hdc, ref _DP_DATA_1 dp_data_1, ref Array12<Array128<byte>> ach)
	{
	int n;
	RECT dstn_rect;
	for( n=0; n<=m; n++)
		{
		// ptin dbg
		dstn_rect.left=120;
		dstn_rect.top=150+(n*25);
		dstn_rect.right=dstn_rect.left+(len[n]*12);
		dstn_rect.bottom=dstn_rect.top+24;
		if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 && RivalMode==GameMode.GameSetting )
			{
			SetTextColor(hdc, RGB(255, 0, 0));

			if(ScenarioNumber<=99)
				ScenarioNumber=(short)(n+1);
			else if(ScenarioNumber<=199)
				ScenarioNumber=(short)(n+1+100);
			else if(ScenarioNumber<=299)
				ScenarioNumber=(short)(n+1+200);

			if( LeftButton==3 )
				{
				if( ScenarioNumber==9 )
					{
					/*g_hDlg =*/ CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_FILE_CONT), hwndApp, (DLGPROC)IDD_FILE_LOAD_Proc );

					}

				if( ScenarioNumber!=9 || UserScenarioFileName[0]!='\0' )
					{
					LoadScenarioData();

					// ホストの選択状態をゲストにセンドします。
					dp_data_1.dwType = MessageType.LeaveGameSetting;
					dp_data_1.data[0] = (short)HostSide;
					dp_data_1.data[1] = ScenarioNumber;

					dp_data_1.data[2] = SupplyRates[0];
					dp_data_1.data[3] = SupplyRates[1];

					dp_data_1.data[4] = IsDecisionEnabled;

					dp_data_1.data[5] = InitialSupplyPoints[0];
					dp_data_1.data[6] = InitialSupplyPoints[1];

					dp_data_1.data[7] = ArrivalControl;

					dp_data_1.data[8] = SwapTime;
					dp_data_1.data[9] = SwapRule;

					bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
					bufferDesc.pBufferData  = (byte*) (_DP_DATA_1*)Unsafe.AsPointer(ref dp_data_1);
					g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

					Mode=GameMode.ConfigSetting;
					}
				}
			}
		else
			SetTextColor(hdc, RGB(255, 255, 255));
		TextOut(hdc, 120, 150+(n*25), ach[n], len[n]);
		}

	if( RivalMode==Mode && Mode!=GameMode.ConfigSetting && (FrameCount%10)==0 )
		{
		// ホストの選択状態をゲストにセンドします。
		dp_data_1.dwType = MessageType.SideAndScenario;
		dp_data_1.data[0] = (short)HostSide;
		dp_data_1.data[1] = ScenarioNumber;

		dp_data_1.data[2] = SupplyRates[0];
		dp_data_1.data[3] = SupplyRates[1];

		dp_data_1.data[4] = IsDecisionEnabled;

		dp_data_1.data[5] = InitialSupplyPoints[0];
		dp_data_1.data[6] = InitialSupplyPoints[1];

		dp_data_1.data[7] = ArrivalControl;

		dp_data_1.data[8] = SwapTime;
		dp_data_1.data[9] = SwapRule;

		bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
		bufferDesc.pBufferData  = (byte*) (_DP_DATA_1*)Unsafe.AsPointer(ref dp_data_1);
		g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, EASY_SEND );
		}
	}

//============================================================================
// 通信対戦セットアップ
// とりあえず、シナリオとサイドを選んでゲームへ
//----------------------------------------------------------------------------
[Original("cnct_game_setting")]
public void	UpdateGameSetting()
	{
	RECT	src_rect,field_rect,dstn_rect;
	int	m=default /* C4701 */,n,g,no1,i,wrk,wrk2,wrk3,rx,ry; Array7<int> menu = default; Array7<int> menu2 = default;
    Array12<Array128<byte>> ach = default;
    Array12<int> len = default;
	HDC					hdc;
	Array256<byte> cBuf = default;

	_DP_DATA_1		dp_data_1 = default;
	_DP_DATA_1* lp_dp_data_1;
	_DP_FLAG			dp_flag = default;
	_DP_DATA_20		dp_data_20=default;

#if CONN_DBG
	rival_mode=mode;
#endif

	ClearFlag=1;

	// マウス情報

	if( IsEditingMap!=0 )
		{
		RivalMode=Mode;
		}

	if ( IDirectDrawSurface_GetDC(lpDDSBack, &hdc) == DD_OK )
		{
		SetBkMode(hdc, TRANSPARENT);
		SelectObject(hdc, gameFont_1);

if(Mode==GameMode.GameSetting)
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

		if ( IDirectDrawSurface_GetDC(lpDDSBack, &hdc) == DD_OK )
			{
			SetBkMode(hdc, TRANSPARENT);
			SelectObject(hdc, gameFont_1);

#if !LNGG_VER
			len[0] = wsprintf(ach[0], "シナリオ選択画面");
#else
			len[0] = wsprintf(ach[0], "Mission Menu");
#endif

			if(Mode==GameMode.GameSetting)
				SetTextColor(hdc, RGB(255, 255, 255));
			else
				SetTextColor(hdc, RGB(126, 126, 126));

			TextOut(hdc, 200, 80, ach[0], len[0]);

#if !LNGG_VER
			if( ScenarioNumber<=99 )
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
			else if(ScenarioNumber<=199)
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
			else if(ScenarioNumber<=299)
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

			if( IsHost!=0 && Mode==GameMode.GameSetting )
				{
				UpdateScenarioList(m, ref len, ref hdc, ref dp_data_1, ref ach);
				}
			else
				{
				for( n=0; n<=m; n++)
					{
					if(Mode==GameMode.GameSetting)
						{
						if( n+1==ScenarioNumber%100 )
							SetTextColor(hdc, RGB(255, 0, 0));
						else
							SetTextColor(hdc, RGB(255, 255, 255));
						}
					else
						{
						if( n+1==ScenarioNumber%100 )
							SetTextColor(hdc, RGB(126, 0, 0));
						else
							SetTextColor(hdc, RGB(126, 126, 126));
						}

					TextOut(hdc, 120, 150+(n*25), ach[n], len[n]);
					}

				if(JoinGameStart==MessageType.LeaveGameSetting && IsHost==0 && Mode==GameMode.GameSetting )
					{
					// ジョインが受け取る
					Mode=GameMode.ConfigSetting;
					JoinGameStart=0;
					}
				}

			if( IsHost!=0 && Mode==GameMode.GameSetting )
				{
#if !LNGG_VER
				len[0] = wsprintf(ach[0], "シナリオ切り替え ->>>");
#else
				len[0] = wsprintf(ach[0], "Page Change ->>>");
#endif

				dstn_rect.left=250;
				dstn_rect.top=110;
				dstn_rect.right=dstn_rect.left+(len[0]*12);
				dstn_rect.bottom=dstn_rect.top+24;
				if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 && RivalMode==GameMode.GameSetting )
					{
					SetTextColor(hdc, RGB(255, 0, 0));

					if( LeftButton==3 )
						{
						if(ScenarioNumber<=99)
							ScenarioNumber=101;
						else if(ScenarioNumber<=199)
							ScenarioNumber=1;
						}
					}
				else
					SetTextColor(hdc, RGB(255, 255, 255));

				TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
				}

			// リジュームスタート
			rx=80;
			if( IsHost!=0 )
				{
#if !LNGG_VER
				if( WasHost!=0 )
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

				if( Mode==GameMode.GameSetting )
					{
					dstn_rect.right=dstn_rect.left+(len[0]*12);
					dstn_rect.bottom=dstn_rect.top+24;
					if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 && RivalMode==GameMode.GameSetting && WasHost!=0 )
						{
						SetTextColor(hdc, RGB(255, 0, 0));

						if( LeftButton==3 )
							{
							// ホストの選択状態をゲストにセンドします。
							dp_data_1.dwType = MessageType.StartFromResume;
							bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
							bufferDesc.pBufferData  = (byte*) &dp_data_1;
							g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

							Mode=GameMode.Battle;
							ScenarioNumber=-1;		// －１でリジュームを示す
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
				if( JoinGameStart==MessageType.StartFromResume )
					{
					Mode=GameMode.Battle;
					JoinGameStart=0;
					ScenarioNumber=-1;		// －１でリジュームを示す
					}
				}

			// リジュームスタート
			rx=80;
			if( IsHost!=0  )
				{
#if !LNGG_VER
				if( HasAutoSave!=0 )
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

				if( Mode==GameMode.GameSetting )
					{
					dstn_rect.right=dstn_rect.left+(len[0]*12);
					dstn_rect.bottom=dstn_rect.top+24;
					if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 && RivalMode==GameMode.GameSetting && HasAutoSave!=0 )
						{
						SetTextColor(hdc, RGB(255, 0, 0));

						if( LeftButton==3 )
							{
							// ホストの選択状態をゲストにセンドします。
							dp_data_1.dwType = MessageType.StartFromAutoSave;
							bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
							bufferDesc.pBufferData  = (byte*) &dp_data_1;
							g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

							Mode=GameMode.Battle;
							ScenarioNumber=-2;		// －２でオートセーブからのスタートを示す
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
				if( JoinGameStart==MessageType.StartFromAutoSave )
					{
					Mode=GameMode.Battle;
					JoinGameStart=0;
					ScenarioNumber=-2;		// －２でオートセーブからのスタートを示す
					}
				}

			// 操作対象の切り替え
			rx=80;
			if( IsEditingMap==0 && IsHost!=0 && Mode==GameMode.GameSetting )
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
				if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 && RivalMode==GameMode.GameSetting )
					{
					SetTextColor(hdc, RGB(255, 0, 0));
					if( LeftButton==3 )
						{
						HostSide=(HostSide==0 ? 1 : 0);
						}
					}
				else
					SetTextColor(hdc, RGB(255, 255, 255));

				TextOut(hdc, dstn_rect.left, dstn_rect.top, ach[0], len[0]);
				}

			if(HostSide==0)
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
			DrawScenarioDescription(ref len, ref ach);
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

if( Mode==GameMode.GameSetting )
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

if(	Mode==GameMode.ConfigSetting )
	{
			UpdateConfigSetting(ref len, ref ach, ref hdc, ref dp_flag, ref dp_data_1);

	}

			IDirectDrawSurface_ReleaseDC(lpDDSBack, hdc);
			}

//t		key_cont();		// チャット用

	if( Mode==GameMode.Battle )
		{
		InitializeGame();
		}
	else if( RivalMode!=0 && (FrameCount%40)==0 )
		{

		// 現在のモードをライバルに送る。
		dp_flag.dwType = MessageType.RivalMode;
		dp_flag.rival_mode=(short)Mode;

		bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
		bufferDesc.pBufferData  = (byte*) &dp_flag;
		g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, EASY_SEND );
		}
	}

//============================================================================
// 通信対戦セットアップ
// とにかく互いに通信するまで。
//----------------------------------------------------------------------------
[Original("cnct_game_setup")]
public void	UpdateSetup()
	{
	}
}
