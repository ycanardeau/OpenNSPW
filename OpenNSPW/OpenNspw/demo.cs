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
	RECT source, destination;
	  
    Array5<Array128<byte>> text = default;
    Array5<int> length = default;
	HDC					hdc;
	 
	_DP_FLAG	flagMessage;
	_DP_DATA_20	chatMessage=default;

#if CONN_DBG
	rival_mode=mode;
#endif

	if((FrameCount%40)==0)
		{
		// 自分のモードを相手に伝える。
		flagMessage.dwType = MessageType.RivalMode;
		flagMessage.rival_mode=(short)Mode;
		bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
		bufferDesc.pBufferData  = (byte*) &flagMessage;
		g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1, 0, null, ref hAsync, EASY_SEND );

		// 自分のバージョンを相手に伝える。
		chatMessage.dwType = MessageType.RivalVersion;
		wsprintf( chatMessage.friend_chat, "%s",VER );
		bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_20));
		bufferDesc.pBufferData  = (byte*) &chatMessage;
		g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, EASY_SEND );
		}

	if( TitleTime==0 )
		{
		RivalMode=0;
		wsprintf(RivalVersion, "- - -" );
		}

	ClearFlag=1;

	// ユニットインフォーメィション
	Sprites[SpriteId.TitleBackground].Position = new PixelPoint(212-50, 130);

	source.left = 	Sprites[SpriteId.TitleBackground].SheetPosition.X;
	source.top = Sprites[SpriteId.TitleBackground].SheetPosition.Y;
	source.right = Sprites[SpriteId.TitleBackground].SheetPosition.X+Sprites[SpriteId.TitleBackground].Size.Width;
	source.bottom = Sprites[SpriteId.TitleBackground].SheetPosition.Y+Sprites[SpriteId.TitleBackground].Size.Height;

	// dstn_rect は ディスティネーションレクタングルです。
	destination.left=Sprites[SpriteId.TitleBackground].Position.X;
	destination.top=Sprites[SpriteId.TitleBackground].Position.Y;

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, destination.left, destination.top,lpDDS_OS,&source,0) )
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
			length[0] = wsprintf(text[0], "Naval South Pacific War on the Net %s",VER );
			length[1] = wsprintf(text[1], "      Copyright 2002 Ken-ichi Tokumitsu");
			length[2] = wsprintf(text[2], "       E-mail:kenken@ta2.so-net.ne.jp");
			length[3] = wsprintf(text[3], "               奈良鹿製作所");
#else
			len[0] = wsprintf(ach[0], "Naval South Pacific War on the Net version 1.08 (English version)");
			len[1] = wsprintf(ach[1], "      Copyright 2002 Ken-ichi Tokumitsu");
			len[2] = wsprintf(ach[2], "  http://www02.u-page.so-net.ne.jp/ta2/kenken/");
			len[3] = wsprintf(ach[3], "           Nara-Shika Seisakusho");
#endif
			for( var n=0; n<=3; n++)
				{
				SetTextColor(hdc, RGB(255, 255, 255));
				TextOut(hdc, 330, 580+(n*23), text[n], length[n]);
				}

			SelectObject(hdc, gameFont_1);

			length[0] = wsprintf(text[0], "YOUR VERSION = %s",VER );
			SetTextColor(hdc, RGB(255, 255, 255));
			TextOut(hdc, 385, 84, text[0], length[0]);

			length[0] = wsprintf(text[0], "RIVAL VERSION = %s",RivalVersion );
			SetTextColor(hdc, RGB(255, 255, 255));
			TextOut(hdc, 385, 99, text[0], length[0]);

			IDirectDrawSurface_ReleaseDC(lpDDSBack, hdc);
			}

	if( LeftButton==3 )
		{
		if( IsEditingMap )
			{
			GoToGameSetting();
			}
		else if( ActivePlayerCount==2 && RivalMode==GameMode.Title && IsHost )
			{
			GoToGameSetting();

			if( 1!=0 )
				{
				flagMessage.dwType = MessageType.GoToGameSetting;
				flagMessage.rival_mode=(short)Mode;
				bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
				bufferDesc.pBufferData  = (byte*) &flagMessage;
				g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1, 0, null, ref hAsync, MUST_SEND );

				//ここにくるのはホストだけ
				// リジュームデータがホストをやってたかしらべる。


				var hFile=CreateFile("Saved\\resume_1.dat", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
																null, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, null);

				if( INVALID_HANDLE_VALUE==hFile )
					{
					// ファイルありませんでした。
					WasHost=false;
					}
				else
					{
					// ファイルはあった。
					LoadResume(1);
					WasHost=IsHost;		// 前回ホストだったら１が代入
					CloseHandle(hFile);
					}
				IsHost=true;						// ここに来るのはホストなのでこれでいい。
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
	IsDecisionEnabled=true;
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
	lpDSB_[(int)SoundId.Sea][0].Stop();		//
#endif


	var hFile=CreateFile("Saved\\auto_save.dat", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
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
// A click on the switch of the victory conditions (勝敗判定の切り替え): on, or off.
private void ToggleDecision()
	{
	IsDecisionEnabled=!IsDecisionEnabled;
	}

private void UpdateDecisionSetting(int x, ref HDC hdc, ref Array12<Array128<byte>> text, ref Array12<int> length)
	{
	RECT destination;
	if(IsHost)
		{
		destination.left=x+50;
		destination.top=50;
		TextOut(hdc, destination.left, destination.top+(1*25), text[0], length[0]);

#if !LNGG_VER
		length[0] = wsprintf(text[0], "勝敗判定の切り替え ->>>");
#else
		len[0] = wsprintf(ach[0], "Decision ->>>");
#endif
		destination.left=x;
		destination.top=125;
		destination.right=destination.left+(length[0]*12);
		destination.bottom=destination.top+24;
		if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( LeftButton==3 )
				{
				ToggleDecision();
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}

		TextOut(hdc, destination.left, destination.top, text[0], length[0]);

		}

	if(!IsDecisionEnabled)
		{
#if !LNGG_VER
		length[0] = wsprintf(text[0], "勝敗条件は無効");
#else
		len[0] = wsprintf(ach[0], "Invalid");
#endif
		}
	else
		{
#if !LNGG_VER
		length[0] = wsprintf(text[0], "勝敗条件は有効");
#else
		len[0] = wsprintf(ach[0], "Valid");
#endif
		}
	SetTextColor(hdc, RGB(255, 255, 255));
	destination.left=x;
	destination.top=150;
	TextOut(hdc, destination.left, destination.top, text[0], length[0]);
	}
// A click on 増やす or 減らす of a player's increase of supply points (補給割当増加率): 0 for the host's, 1 for the
// guest's. A rate is decreased only while it is not 0.
private void IncreaseSupplyRate(int player)
	{
	SupplyRates[player]++;
	}

private void DecreaseSupplyRate(int player)
	{
	SupplyRates[player]--;
	}

private void UpdateHostSupplyRateSetting(ref Array12<int> length, ref Array12<Array128<byte>> text, ref HDC hdc, int x)
	{
	RECT destination;
#if !LNGG_VER
	length[0] = wsprintf(text[0], "補給割当増加率（ホスト側）");
#else
	len[0] = wsprintf(ach[0], "increase rate of supply pts(Host)");
#endif
	SetTextColor(hdc, RGB(255, 255, 255));
	destination.left=x;
	destination.top=200;
	TextOut(hdc, destination.left, destination.top, text[0], length[0]);

	if( IsHost )
		{
#if !LNGG_VER
		length[0] = wsprintf(text[0], "増やす ->>>");
#else
		len[0] = wsprintf(ach[0], "Increment ->>>");
#endif
		SetTextColor(hdc, RGB(255, 255, 255));
		destination.left=x;
		destination.top=225;
		destination.right=destination.left+(length[0]*10);
		destination.bottom=destination.top+24;
		if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( LeftButton==3 )
				{
				IncreaseSupplyRate(0);
				var changed=1;
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}
		TextOut(hdc, destination.left, destination.top, text[0], length[0]);

#if !LNGG_VER
		length[0] = wsprintf(text[0], "減らす ->>>");
#else
		len[0] = wsprintf(ach[0], "Decrement ->>>");
#endif
		SetTextColor(hdc, RGB(255, 255, 255));
		destination.left=x;
		destination.top=250;
		destination.right=destination.left+(length[0]*10);
		destination.bottom=destination.top+24;
		if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( LeftButton==3 && SupplyRates[0]!=0)
				{
				DecreaseSupplyRate(0);
				var changed=1;
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}

		TextOut(hdc, destination.left, destination.top, text[0], length[0]);
		}

	length[0] = wsprintf(text[0], "%d pts",SupplyRates[0]);
	SetTextColor(hdc, RGB(255, 255, 255));
	destination.left=x+150;
	destination.top=237;
	TextOut(hdc, destination.left, destination.top, text[0], length[0]);
	}
// A frame of a press on 増やす or 減らす of the time at which the supply points' rates swap (補給値反転地点), in hundreds,
// 0 for never. The time is decreased only while it is not 0.
private void IncreaseSwapTime()
	{
	SwapTime++;
	}

private void DecreaseSwapTime()
	{
	SwapTime--;
	}

private void UpdateSwapTimeSetting(ref Array12<int> length, ref Array12<Array128<byte>> text, ref HDC hdc, int x)
	{
	RECT destination;
#if !LNGG_VER
	length[0] = wsprintf(text[0], "補給値反転地点");
#else
	len[0] = wsprintf(ach[0], "Reverse of suplly pts");
#endif
	SetTextColor(hdc, RGB(255, 255, 255));
	destination.left=x+250;
	destination.top=200;
	TextOut(hdc, destination.left, destination.top, text[0], length[0]);

	if( IsHost )
		{
#if !LNGG_VER
		length[0] = wsprintf(text[0], "増やす ->>>");
#else
		len[0] = wsprintf(ach[0], "Increment ->>>");
#endif
		SetTextColor(hdc, RGB(255, 255, 255));
		destination.left=x+250;
		destination.top=225;
		destination.right=destination.left+(length[0]*10);
		destination.bottom=destination.top+24;
		if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( (LeftButton==1||LeftButton==2) )
				{
				IncreaseSwapTime();
				var changed=1;
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}
		TextOut(hdc, destination.left, destination.top, text[0], length[0]);

#if !LNGG_VER
		length[0] = wsprintf(text[0], "減らす ->>>");
#else
		len[0] = wsprintf(ach[0], "Decrement ->>>");
#endif
		SetTextColor(hdc, RGB(255, 255, 255));
		destination.left=x+250;
		destination.top=250;
		destination.right=destination.left+(length[0]*10);
		destination.bottom=destination.top+24;
		if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( (LeftButton==1||LeftButton==2) && SwapTime!=0)
				{
				DecreaseSwapTime();
				var changed=1;
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}

		TextOut(hdc, destination.left, destination.top, text[0], length[0]);
		}

#if !LNGG_VER
	if( SwapTime!=0 )
		length[0] = wsprintf(text[0], "経過時間 %d",SwapTime*100);
	else
		length[0] = wsprintf(text[0], "反転無し");
#else
	if( rvrs_time )
		len[0] = wsprintf(ach[0], "TIME: %d",rvrs_time*100);
	else
		len[0] = wsprintf(ach[0], "No reverse");
#endif
	SetTextColor(hdc, RGB(255, 255, 255));
	destination.left=x+150+250-30;
	destination.top=237;
	TextOut(hdc, destination.left, destination.top, text[0], length[0]);
	}
private void UpdateGuestSupplyRateSetting(ref Array12<int> length, ref Array12<Array128<byte>> text, ref HDC hdc, int x)
	{
	RECT destination;
#if !LNGG_VER
	length[0] = wsprintf(text[0], "補給割当増加率（ゲスト側）");
#else
	len[0] = wsprintf(ach[0], "Increse rate of supply pts(guest)");
#endif
	SetTextColor(hdc, RGB(255, 255, 255));
	destination.left=x;
	destination.top=300;
	TextOut(hdc, destination.left, destination.top, text[0], length[0]);

	if( IsHost )
		{

#if !LNGG_VER
		length[0] = wsprintf(text[0], "増やす ->>>");
#else
		len[0] = wsprintf(ach[0], "Increment ->>>");
#endif
		SetTextColor(hdc, RGB(255, 255, 255));
		destination.left=x;
		destination.top=325;
		destination.right=destination.left+(length[0]*10);
		destination.bottom=destination.top+24;
		if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( LeftButton==3 )
				{
				IncreaseSupplyRate(1);
				var changed=1;
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}
		TextOut(hdc, destination.left, destination.top, text[0], length[0]);

#if !LNGG_VER
		length[0] = wsprintf(text[0], "減らす ->>>");
#else
		len[0] = wsprintf(ach[0], "Decrement ->>>");
#endif
		SetTextColor(hdc, RGB(255, 255, 255));
		destination.left=x;
		destination.top=350;
		destination.right=destination.left+(length[0]*10);
		destination.bottom=destination.top+24;
		if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( LeftButton==3 && SupplyRates[1]!=0)
				{
				DecreaseSupplyRate(1);
				var changed=1;
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}
		TextOut(hdc, destination.left, destination.top, text[0], length[0]);
		}

	length[0] = wsprintf(text[0], "%d pts",SupplyRates[1]);
	SetTextColor(hdc, RGB(255, 255, 255));
	destination.left=x+150;
	destination.top=337;
	TextOut(hdc, destination.left, destination.top, text[0], length[0]);
	}
// A click on the rule of the swap (補給値反転ルール): once (一度だけ), or every time the time passes (指定時間毎).
private void ToggleSwapRule()
	{
	SwapRule=(short)(SwapRule==0 ? 1 : 0);
	}

private void UpdateSwapRuleSetting(ref Array12<int> length, ref Array12<Array128<byte>> text, ref HDC hdc, int x)
	{
	RECT destination;
	if( SwapTime!=0 )
		{
#if !LNGG_VER
		length[0] = wsprintf(text[0], ( IsHost ? "補給値反転ルール->>>" : "補給値反転ルール" ));
#else
		len[0] = wsprintf(ach[0], "Reverse rule");
#endif
		SetTextColor(hdc, RGB(255, 255, 255));
		destination.left=x+250;
		destination.top=300;
		destination.right=destination.left+(length[0]*10);
		destination.bottom=destination.top+24;

		if( IsHost )
			{
			if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 )
				{
				SetTextColor(hdc, RGB(255, 0, 0));
				if( LeftButton==3 )
					{
					ToggleSwapRule();
					var changed=1;
					}
				}
			else
				{
				SetTextColor(hdc, RGB(255, 255, 255));
				}
			}
		TextOut(hdc, destination.left, destination.top, text[0], length[0]);

		if( SwapRule==0 )
			{
#if !LNGG_VER
			length[0] = wsprintf(text[0], "一度だけ");
#else
			len[0] = wsprintf(ach[0], "Once a game");
#endif
			SetTextColor(hdc, RGB(255, 255, 255));
			destination.left=x+250;
			destination.top=325;
			TextOut(hdc, destination.left, destination.top, text[0], length[0]);
			}
		else
			{
#if !LNGG_VER
			length[0] = wsprintf(text[0], "指定時間毎");
#else
			len[0] = wsprintf(ach[0], "Every specified TIME");
#endif
			SetTextColor(hdc, RGB(255, 255, 255));
			destination.left=x+250;
			destination.top=325;
			TextOut(hdc, destination.left, destination.top, text[0], length[0]);
			}
		}
	}
// A frame of a press on 増やす or 減らす of a player's supply points at the start (初期補給割当): 0 for the host's, 1 for
// the guest's, by 50. The points are decreased only while they are not 0.
private void IncreaseInitialSupplyPoints(int player)
	{
	InitialSupplyPoints[player]+=50;
	}

private void DecreaseInitialSupplyPoints(int player)
	{
	InitialSupplyPoints[player]-=50;
	if( InitialSupplyPoints[player]<0)
		InitialSupplyPoints[player]=0;
	}

private void UpdateHostInitialSupplyPointsSetting(ref Array12<int> length, ref Array12<Array128<byte>> text, ref HDC hdc, int x)
	{
	RECT destination;
#if !LNGG_VER
	length[0] = wsprintf(text[0], "初期補給割当（ホスト側）");
#else
	len[0] = wsprintf(ach[0], "Supply pts on start(Host)");
#endif
	SetTextColor(hdc, RGB(255, 255, 255));
	destination.left=x;
	destination.top=400;
	TextOut(hdc, destination.left, destination.top, text[0], length[0]);

	if( IsHost )
		{
//				len[0] = wsprintf(ach[0], "増やす ->>>");
#if !LNGG_VER
		length[0] = wsprintf(text[0], "増やす ->>>");
#else
		len[0] = wsprintf(ach[0], "Increment ->>>");
#endif

		SetTextColor(hdc, RGB(255, 255, 255));
		destination.left=x;
		destination.top=425;
		destination.right=destination.left+(length[0]*10);
		destination.bottom=destination.top+24;
		if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( (LeftButton==1||LeftButton==2) )
				{
				IncreaseInitialSupplyPoints(0);
				var changed=1;
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}
		TextOut(hdc, destination.left, destination.top, text[0], length[0]);

//				len[0] = wsprintf(ach[0], "減らす ->>>");
#if !LNGG_VER
		length[0] = wsprintf(text[0], "減らす ->>>");
#else
		len[0] = wsprintf(ach[0], "Decrement ->>>");
#endif

		SetTextColor(hdc, RGB(255, 255, 255));
		destination.left=x;
		destination.top=450;
		destination.right=destination.left+(length[0]*10);
		destination.bottom=destination.top+24;
		if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( (LeftButton==1||LeftButton==2) && InitialSupplyPoints[0]!=0)
				{
				DecreaseInitialSupplyPoints(0);
				var changed=1;
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}
		TextOut(hdc, destination.left, destination.top, text[0], length[0]);
		}

	length[0] = wsprintf(text[0], "%d pts",InitialSupplyPoints[0]);
	SetTextColor(hdc, RGB(255, 255, 255));
	destination.left=x+150;
	destination.top=437;
	TextOut(hdc, destination.left, destination.top, text[0], length[0]);
	}
private void UpdateGuestInitialSupplyPointsSetting(ref Array12<int> length, ref Array12<Array128<byte>> text, ref HDC hdc, int x)
	{
	RECT destination;
#if !LNGG_VER
	length[0] = wsprintf(text[0], "初期補給割当（ゲスト側）");
#else
	len[0] = wsprintf(ach[0], "Supply pts on start(Guest)");
#endif
	SetTextColor(hdc, RGB(255, 255, 255));
	destination.left=x;
	destination.top=500;
	TextOut(hdc, destination.left, destination.top, text[0], length[0]);

	if( IsHost )
		{
//				len[0] = wsprintf(ach[0], "増やす ->>>");
#if !LNGG_VER
		length[0] = wsprintf(text[0], "増やす ->>>");
#else
		len[0] = wsprintf(ach[0], "Increment ->>>");
#endif

		SetTextColor(hdc, RGB(255, 255, 255));
		destination.left=x;
		destination.top=525;
		destination.right=destination.left+(length[0]*10);
		destination.bottom=destination.top+24;
		if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( (LeftButton==1||LeftButton==2) )
				{
				IncreaseInitialSupplyPoints(1);
				var changed=1;
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}
		TextOut(hdc, destination.left, destination.top, text[0], length[0]);

//				len[0] = wsprintf(ach[0], "減らす ->>>");
#if !LNGG_VER
		length[0] = wsprintf(text[0], "減らす ->>>");
#else
		len[0] = wsprintf(ach[0], "Decrement ->>>");
#endif

		SetTextColor(hdc, RGB(255, 255, 255));
		destination.left=x;
		destination.top=550;
		destination.right=destination.left+(length[0]*10);
		destination.bottom=destination.top+24;
		if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( (LeftButton==1||LeftButton==2) && InitialSupplyPoints[1]!=0)
				{
				DecreaseInitialSupplyPoints(1);
				var changed=1;
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}
		TextOut(hdc, destination.left, destination.top, text[0], length[0]);
		}

	length[0] = wsprintf(text[0], "%d pts",InitialSupplyPoints[1]);
	SetTextColor(hdc, RGB(255, 255, 255));
	destination.left=x+150;
	destination.top=537;
	TextOut(hdc, destination.left, destination.top, text[0], length[0]);
	}
// The rule of the reinforcements (増援ユニット制御), in ach[0].
private void WriteArrivalControl(ref Array12<int> length, ref Array12<Array128<byte>> text)
	{
	switch( ArrivalControl )
		{
		case 0:
			length[0] = wsprintf(text[0], "全種増援可(%d)",ArrivalControl);
			break;
		case 1:
			length[0] = wsprintf(text[0], "全種増援不可(%d)",ArrivalControl);
			break;
		case 2:
			length[0] = wsprintf(text[0], "輸送船以外可(%d)",ArrivalControl);
			break;
		case 3:
			length[0] = wsprintf(text[0], "輸送船のみ可(%d)",ArrivalControl);
			break;
		case 4:
			length[0] = wsprintf(text[0], "戦闘艦船のみ可(%d)",ArrivalControl);
			break;
		case 5:
			length[0] = wsprintf(text[0], "航空機のみ可(%d)",ArrivalControl);
			break;
		case 6:
			length[0] = wsprintf(text[0], "輸送船(軍港)以外可(%d)",ArrivalControl);
			break;
		}
	}

// A click on the control of the reinforcements (増援ユニット制御): the next of its 7 rules.
private void CycleArrivalControl()
	{
	ArrivalControl++;
	if(ArrivalControl>=7)
		ArrivalControl=0;
	}

private void UpdateArrivalControlSetting(ref Array12<int> length, ref Array12<Array128<byte>> text, int x, ref HDC hdc)
	{
	RECT destination;
	if(IsHost)
		{
#if !LNGG_VER
		length[0] = wsprintf(text[0], "増援ユニット制御 ->>>");
#else
		len[0] = wsprintf(ach[0], "Supply unit control ->>>");
#endif
		destination.left=x;
		destination.top=600;
		destination.right=destination.left+(length[0]*12);
		destination.bottom=destination.top+24;
		if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( LeftButton==3 )
				{
				CycleArrivalControl();
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}

		TextOut(hdc, destination.left, destination.top, text[0], length[0]);

		}

#if !LNGG_VER
	WriteArrivalControl(ref length, ref text);
#else
	switch( ArrivalControl )
		{
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

		}
#endif

	SetTextColor(hdc, RGB(255, 255, 255));
	destination.left=x;
	destination.top=625;
	TextOut(hdc, destination.left, destination.top, text[0], length[0]);
	}
// A click on シナリオセッティングへ戻る, while the guest is on the rules too: both go back to the scenarios, with the
// scenario kept, and the rules as they are at first (GoToGameSetting).
private void BackToScenarioSetting(ref _DP_FLAG flagMessage)
	{
	int m=ScenarioNumber;
	GoToGameSetting();
	ScenarioNumber=(short)m;

	flagMessage.dwType = MessageType.GoToGameSetting;

	bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
	bufferDesc.pBufferData  = (byte*) (_DP_FLAG*)Unsafe.AsPointer(ref flagMessage);
	g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
	}

private void UpdateBackToScenarioSettingButton(ref Array12<int> length, ref Array12<Array128<byte>> text, int x, ref HDC hdc, ref _DP_FLAG flagMessage)
	{
	RECT destination;
	if(IsHost)
		{
#if !LNGG_VER
		length[0] = wsprintf(text[0], "シナリオセッティングへ戻る->>>");
#else
		len[0] = wsprintf(ach[0], "Back to mission menu ->>>");
#endif
		destination.left=x;
		destination.top=675;
		destination.right=destination.left+(length[0]*12);
		destination.bottom=destination.top+24;
		if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 && RivalMode==GameMode.ConfigSetting )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( LeftButton==3 )
				{
				BackToScenarioSetting(ref flagMessage);
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}
		TextOut(hdc, destination.left, destination.top, text[0], length[0]);
		}
	}
// A click on ゲームスタート, while the guest is on the rules too: the host sends the shared random numbers' seed and
// the rules, and both go to the battle.
private void StartBattle(ref _DP_DATA_1 message)
	{
	// ホストの選択状態をゲストにセンドします。
	srand( (uint)time( null ) );
	SharedRandomSeed=(short)Random(65536);

	message.dwType = MessageType.LeaveSetup;
	message.data[0] = SharedRandomSeed;

	bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
	bufferDesc.pBufferData  = (byte*) (_DP_DATA_1*)Unsafe.AsPointer(ref message);
	g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

	message.dwType = MessageType.LeaveConfigSetting;
	message.data[0] = (short)HostSide;
	message.data[1] = ScenarioNumber;

	message.data[2] = SupplyRates[0];
	message.data[3] = SupplyRates[1];

	message.data[4] = IsDecisionEnabled.Value;

	message.data[5] = InitialSupplyPoints[0];
	message.data[6] = InitialSupplyPoints[1];

	message.data[7] = ArrivalControl;

	message.data[8] = SwapTime;
	message.data[9] = SwapRule;

	bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
	bufferDesc.pBufferData  = (byte*) (_DP_DATA_1*)Unsafe.AsPointer(ref message);
	g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

	Mode=GameMode.Battle;
	}

private void UpdateStartBattleButton(ref Array12<int> length, ref Array12<Array128<byte>> text, int x, ref HDC hdc, ref _DP_DATA_1 message)
	{
	RECT destination;
	if(IsHost)
		{
#if !LNGG_VER
		length[0] = wsprintf(text[0], "ゲームスタート ->>>");
#else
		len[0] = wsprintf(ach[0], "Game Start ->>>");
#endif
		destination.left=x;
		destination.top=700;
		destination.right=destination.left+(length[0]*12);
		destination.bottom=destination.top+24;
		if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 && RivalMode==GameMode.ConfigSetting )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( LeftButton==3 )
				{
				StartBattle(ref message);
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}
		TextOut(hdc, destination.left, destination.top, text[0], length[0]);
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
private void SendHostSettings(ref _DP_DATA_1 message)
	{
	message.dwType = MessageType.SideAndScenario;
	message.data[0] = (short)HostSide;
	message.data[1] = ScenarioNumber;

	message.data[2] = SupplyRates[0];
	message.data[3] = SupplyRates[1];

	message.data[4] = IsDecisionEnabled.Value;

	message.data[5] = InitialSupplyPoints[0];
	message.data[6] = InitialSupplyPoints[1];

	message.data[7] = ArrivalControl;

	message.data[8] = SwapTime;
	message.data[9] = SwapRule;

	bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
	bufferDesc.pBufferData  = (byte*) (_DP_DATA_1*)Unsafe.AsPointer(ref message);
	g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, EASY_SEND );
	}

private void UpdateConfigSetting(ref Array12<int> length, ref Array12<Array128<byte>> text, ref HDC hdc, ref _DP_FLAG flagMessage, ref _DP_DATA_1 message)
	{
	var x=630-120;

#if !LNGG_VER
	length[0] = wsprintf(text[0], "コンフィギュレーション");
#else
	len[0] = wsprintf(ach[0], "Configuration");
#endif
	SetTextColor(hdc, RGB(255, 255, 255));

	// 勝敗判定
	UpdateDecisionSetting(x, ref hdc, ref text, ref length);

	// 補給割当増加率（ホスト側）
	UpdateHostSupplyRateSetting(ref length, ref text, ref hdc, x);

	// 補給値反転地点
	UpdateSwapTimeSetting(ref length, ref text, ref hdc, x);

	// 補給割当増加率（ゲスト側）
	UpdateGuestSupplyRateSetting(ref length, ref text, ref hdc, x);

	// 補給値反転ルール
	UpdateSwapRuleSetting(ref length, ref text, ref hdc, x);

	// 初期補給割当（ホスト側）
	UpdateHostInitialSupplyPointsSetting(ref length, ref text, ref hdc, x);

	// 初期補給割当（ゲスト側）
	UpdateGuestInitialSupplyPointsSetting(ref length, ref text, ref hdc, x);

	UpdateArrivalControlSetting(ref length, ref text, x, ref hdc);

	// シナリオセッティングへ戻る
	UpdateBackToScenarioSettingButton(ref length, ref text, x, ref hdc, ref flagMessage);

	// 戦闘開始
	UpdateStartBattleButton(ref length, ref text, x, ref hdc, ref message);

	// The host's UI's commands for the rules (UICommands.cs).
	RunUICommands(UICommandSite.Rules, IsHost);

	if( IsHost && RivalMode==Mode && (FrameCount%10)==0 )
		{
		// ホストの選択状態をゲストにセンドします。
		SendHostSettings(ref message);
		}
	}

private void DrawScenarioDescription(ref Array12<int> length, ref Array12<Array128<byte>> text)
	{
	switch( ScenarioNumber )
		{
		case 1:
			length[1] = wsprintf(text[1], "空母機動部隊同士の戦いです。小規模です。");
			length[2] = wsprintf(text[2], "");
			length[3] = wsprintf(text[3], "");
			length[4] = wsprintf(text[4], "");

			length[5] = wsprintf(text[5], "勝敗条件：敵空母撃沈か敵歩兵基地破壊で勝利");
			length[6] = wsprintf(text[6], "");
			break;

		case 2:
			length[1] = wsprintf(text[1], "空母機動部隊同士の戦いです。中規模です。");
			length[2] = wsprintf(text[2], "");
			length[3] = wsprintf(text[3], "");
			length[4] = wsprintf(text[4], "");

			length[5] = wsprintf(text[5], "勝敗条件：敵空母撃沈か敵歩兵基地破壊で勝利");
			length[6] = wsprintf(text[6], "");
			break;

		case 3:
			length[1] = wsprintf(text[1], "空母機動部隊同士の戦いです。大規模です。");
			length[2] = wsprintf(text[2], "");
			length[3] = wsprintf(text[3], "");
			length[4] = wsprintf(text[4], "");

			length[5] = wsprintf(text[5], "勝敗条件：敵空母撃沈か敵歩兵基地破壊で勝利");
			length[6] = wsprintf(text[6], "");
			break;

		case 4:
			length[1] = wsprintf(text[1], "戦闘艦船のみの戦いです。");
			length[2] = wsprintf(text[2], "");
			length[3] = wsprintf(text[3], "");
			length[4] = wsprintf(text[4], "");

			length[5] = wsprintf(text[5], "勝敗条件：敵軍全ての戦艦、巡洋艦の撃沈、");
			length[6] = wsprintf(text[6], "または歩兵基地の破壊で勝利。");
			break;

		case 5:
			length[1] = wsprintf(text[1], "戦闘艦船のみの戦いです。");
			length[2] = wsprintf(text[2], "");
			length[3] = wsprintf(text[3], "");
			length[4] = wsprintf(text[4], "");

			length[5] = wsprintf(text[5], "勝敗条件：敵軍全ての戦艦、巡洋艦の撃沈、");
			length[6] = wsprintf(text[6], "または歩兵基地の破壊で勝利。");
			break;

		case 6:
			length[1] = wsprintf(text[1], "日本海軍によるミッドウェイ島の攻略作戦です。");
			length[2] = wsprintf(text[2], "");
			length[3] = wsprintf(text[3], "");
			length[4] = wsprintf(text[4], "");

			length[5] = wsprintf(text[5], "勝敗条件：ミッドウェイ島の米軍施設の壊滅で");
			length[6] = wsprintf(text[6], "日本海軍の勝利となります。");
			break;

		case 7:
			length[1] = wsprintf(text[1], "日本海軍によるミッドウェイ島の攻略作戦です。");
			length[2] = wsprintf(text[2], "");
			length[3] = wsprintf(text[3], "");
			length[4] = wsprintf(text[4], "");

			length[5] = wsprintf(text[5], "勝敗条件：ミッドウェイ島の米軍施設の壊滅と、同島に");
			length[6] = wsprintf(text[6], "日本軍の要塞を一つ完成で勝利となります。");
			break;

		case 8:
			length[1] = wsprintf(text[1], "中部太平洋での戦闘です。");
			length[2] = wsprintf(text[2], "");
			length[3] = wsprintf(text[3], "");
			length[4] = wsprintf(text[4], "");

			length[5] = wsprintf(text[5], "勝敗条件：ミッドウェイの米軍施設壊滅と自軍の要塞で");
			length[6] = wsprintf(text[6], "日本軍の勝利、ウェークに同じ条件で米軍の勝利。");
			break;

		case 9:
			length[1] = wsprintf(text[1], "ユーザーシナリオでの戦いです。");
			length[2] = wsprintf(text[2], "");
			length[3] = wsprintf(text[3], "");
			length[4] = wsprintf(text[4], "");

			length[5] = wsprintf(text[5], "勝敗条件：任意に決めてください。");
			length[6] = wsprintf(text[6], "");
			break;

		case 101:
			length[1] = wsprintf(text[1], "ガダルカナル島を巡る戦いです。");
			length[2] = wsprintf(text[2], "");
			length[3] = wsprintf(text[3], "");
			length[4] = wsprintf(text[4], "");

			length[5] = wsprintf(text[5], "勝敗条件：ガダルカナル島の日本軍の陸上施設の");
			length[6] = wsprintf(text[6], "壊滅で米海軍の勝利です。");
			break;

		case 102:
			length[1] = wsprintf(text[1], "ガダルカナル島を巡る戦いです。");
			length[2] = wsprintf(text[2], "");
			length[3] = wsprintf(text[3], "");
			length[4] = wsprintf(text[4], "");

			length[5] = wsprintf(text[5], "勝敗条件：ガダルカナル島の日本軍の陸上施設の壊滅と");
			length[6] = wsprintf(text[6], "同島に航空基地の完成で米海軍の勝利です。");
			break;

		case 103:
			length[1] = wsprintf(text[1], "日本近海の戦いです。");
			length[2] = wsprintf(text[2], "");
			length[3] = wsprintf(text[3], "");
			length[4] = wsprintf(text[4], "");

			length[5] = wsprintf(text[5], "勝敗条件：日本本土の都市を全て壊滅すれば");
			length[6] = wsprintf(text[6], "米海軍の勝利です。");
			break;

		case 104:
			length[1] = wsprintf(text[1], "南太平洋の戦いです。");
			length[2] = wsprintf(text[2], "");
			length[3] = wsprintf(text[3], "");
			length[4] = wsprintf(text[4], "");

			length[5] = wsprintf(text[5], "勝敗条件：ブーゲンビル島の日本軍地上施設");
			length[6] = wsprintf(text[6], "壊滅で米海軍の勝利です。");
			break;

		case 105:
			length[1] = wsprintf(text[1], "南太平洋の戦いです。");
			length[2] = wsprintf(text[2], "");
			length[3] = wsprintf(text[3], "");
			length[4] = wsprintf(text[4], "");

			length[5] = wsprintf(text[5], "勝敗条件：ブーゲンビル島の日本軍施設壊滅で米海軍の勝利、");
			length[6] = wsprintf(text[6], "ガダルカナルの米軍施設壊滅で日本海軍の勝利です。");
			break;

		case 106:
			length[1] = wsprintf(text[1], "ガダルカナル島の争奪戦です。");
			length[2] = wsprintf(text[2], "");
			length[3] = wsprintf(text[3], "");
			length[4] = wsprintf(text[4], "");

			length[5] = wsprintf(text[5], "勝敗条件：ガダルカナル島の敵陸上施設全滅と自軍施設");
			length[6] = wsprintf(text[6], "が四個以上あれば勝利です。");
			break;

		default:
			length[1] = wsprintf(text[1], "");
			length[2] = wsprintf(text[2], "");
			length[3] = wsprintf(text[3], "");
			length[4] = wsprintf(text[4], "");

			length[5] = wsprintf(text[5], "");
			length[6] = wsprintf(text[6], "");
			break;
		}
	}

// The cursor on the n-th scenario of the list: it is the scenario chosen, of the list shown (ListScenarioNames).
private void ChooseScenario(int n)
	{
	if(ScenarioNumber<=99)
		ScenarioNumber=(short)(n+1);
	else if(ScenarioNumber<=199)
		ScenarioNumber=(short)(n+1+100);
	else if(ScenarioNumber<=299)
		ScenarioNumber=(short)(n+1+200);
	}

// A click on the scenario chosen, while the guest is on the scenarios too: the host loads it and sends it with the
// rules, and both go to the rules; or, for the user's scenario (9), the host picks its file first (IDD_FILE_CONT).
private void LeaveGameSetting(ref _DP_DATA_1 message)
	{
	if( ScenarioNumber==9 )
		{
		/*g_hDlg =*/ CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_FILE_CONT), hwndApp, (DLGPROC)IDD_FILE_LOAD_Proc );

		}

	if( ScenarioNumber!=9 || UserScenarioFileName[0]!='\0' )
		{
		LoadScenarioData();

		// ホストの選択状態をゲストにセンドします。
		message.dwType = MessageType.LeaveGameSetting;
		message.data[0] = (short)HostSide;
		message.data[1] = ScenarioNumber;

		message.data[2] = SupplyRates[0];
		message.data[3] = SupplyRates[1];

		message.data[4] = IsDecisionEnabled.Value;

		message.data[5] = InitialSupplyPoints[0];
		message.data[6] = InitialSupplyPoints[1];

		message.data[7] = ArrivalControl;

		message.data[8] = SwapTime;
		message.data[9] = SwapRule;

		bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
		bufferDesc.pBufferData  = (byte*) (_DP_DATA_1*)Unsafe.AsPointer(ref message);
		g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

		Mode=GameMode.ConfigSetting;
		}
	}

private void UpdateScenarioList(int m, ref Array12<int> length, ref HDC hdc, ref _DP_DATA_1 message, ref Array12<Array128<byte>> text)
	{
	RECT destination;
	for( var n=0; n<=m; n++)
		{
		// ptin dbg
		destination.left=120;
		destination.top=150+(n*25);
		destination.right=destination.left+(length[n]*12);
		destination.bottom=destination.top+24;
		if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 && RivalMode==GameMode.GameSetting )
			{
			SetTextColor(hdc, RGB(255, 0, 0));

			ChooseScenario(n);

			if( LeftButton==3 )
				{
				LeaveGameSetting(ref message);
				}
			}
		else
			SetTextColor(hdc, RGB(255, 255, 255));
		TextOut(hdc, 120, 150+(n*25), text[n], length[n]);
		}

	if( RivalMode==Mode && Mode!=GameMode.ConfigSetting && (FrameCount%10)==0 )
		{
		// ホストの選択状態をゲストにセンドします。
		message.dwType = MessageType.SideAndScenario;
		message.data[0] = (short)HostSide;
		message.data[1] = ScenarioNumber;

		message.data[2] = SupplyRates[0];
		message.data[3] = SupplyRates[1];

		message.data[4] = IsDecisionEnabled.Value;

		message.data[5] = InitialSupplyPoints[0];
		message.data[6] = InitialSupplyPoints[1];

		message.data[7] = ArrivalControl;

		message.data[8] = SwapTime;
		message.data[9] = SwapRule;

		bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
		bufferDesc.pBufferData  = (byte*) (_DP_DATA_1*)Unsafe.AsPointer(ref message);
		g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, EASY_SEND );
		}
	}

// A click on 操作対象の切り替え: the host and the guest swap their sides.
private void SwitchHostSide()
	{
	HostSide=(HostSide==0 ? 1 : 0);
	}

private void UpdateSideSetting(ref int x, ref Array12<int> length, ref Array12<Array128<byte>> text, ref HDC hdc)
	{
	RECT destination;
	x=80;
	if( !IsEditingMap && IsHost && Mode==GameMode.GameSetting )
		{
#if !LNGG_VER
		length[0] = wsprintf(text[0], "操作対象の切り替え ->>>");
#else
		len[0] = wsprintf(ach[0], "Side Change ->>>");
#endif

		destination.left=x;
		destination.top=475;
		destination.right=destination.left+(length[0]*12);
		destination.bottom=destination.top+24;
		if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 && RivalMode==GameMode.GameSetting )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( LeftButton==3 )
				{
				SwitchHostSide();
				}
			}
		else
			SetTextColor(hdc, RGB(255, 255, 255));

		TextOut(hdc, destination.left, destination.top, text[0], length[0]);
		}

	if(HostSide==0)
		{
#if !LNGG_VER
		length[0] = wsprintf(text[0], "操作対象：日本海軍（ホスト）、合衆国海軍（ゲスト）");
#else
		len[0] = wsprintf(ach[0], "Side: Japan Navy(Host Player)  U.S.Navy(Guest Player)");
#endif
		}
	else
		{
#if !LNGG_VER
		length[0] = wsprintf(text[0], "操作対象：合衆国海軍（ホスト）、日本海軍（ゲスト）");
#else
		len[0] = wsprintf(ach[0], "Side: U.S.Navy(Host Player)  Japan Navy(Guest Player)");
#endif
		}
	}

// A click on the start from the auto-saved battle (オートセーブスタート), while the guest is on the scenarios too: both go to the battle.
private void StartFromAutoSave(ref _DP_DATA_1 message)
	{
	// ホストの選択状態をゲストにセンドします。
	message.dwType = MessageType.StartFromAutoSave;
	bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
	bufferDesc.pBufferData  = (byte*) (_DP_DATA_1*)Unsafe.AsPointer(ref message);
	g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

	Mode=GameMode.Battle;
	ScenarioNumber=-2;		// －２でオートセーブからのスタートを示す
	}

private void UpdateAutoSaveButton(ref int x, ref Array12<int> length, ref Array12<Array128<byte>> text, ref HDC hdc, ref _DP_DATA_1 message)
	{
	RECT destination;
	x=80;
	if( IsHost  )
		{
#if !LNGG_VER
		if( HasAutoSave!=0 )
			length[0] = wsprintf(text[0], "オートセーブスタート ->>>");
		else
			length[0] = wsprintf(text[0], "オートセーブファイルは存在しない");
#else
		if( exist_auto_save )
			len[0] = wsprintf(ach[0], "Autosave Start ->>>");
		else
			len[0] = wsprintf(ach[0], "No exist of Autosave file");
#endif

		destination.left=80;
		destination.top=430;

		if( Mode==GameMode.GameSetting )
			{
			destination.right=destination.left+(length[0]*12);
			destination.bottom=destination.top+24;
			if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 && RivalMode==GameMode.GameSetting && HasAutoSave!=0 )
				{
				SetTextColor(hdc, RGB(255, 0, 0));

				if( LeftButton==3 )
					{
					StartFromAutoSave(ref message);
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
		TextOut(hdc, destination.left, destination.top, text[0], length[0]);
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
	}

// A click on the start from the resume-saved battle (リジュームスタート), which the player saved as the host, while the guest is on the scenarios too: both go to the battle.
private void StartFromResume(ref _DP_DATA_1 message)
	{
	// ホストの選択状態をゲストにセンドします。
	message.dwType = MessageType.StartFromResume;
	bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
	bufferDesc.pBufferData  = (byte*) (_DP_DATA_1*)Unsafe.AsPointer(ref message);
	g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

	Mode=GameMode.Battle;
	ScenarioNumber=-1;		// －１でリジュームを示す
	}

private void UpdateResumeButton(ref int x, ref Array12<int> length, ref Array12<Array128<byte>> text, ref HDC hdc, ref _DP_DATA_1 message)
	{
	RECT destination;
	x=80;
	if( IsHost )
		{
#if !LNGG_VER
		if( WasHost )
			length[0] = wsprintf(text[0], "リジュームスタート ->>>");
		else
			length[0] = wsprintf(text[0], "リジュームデータがホストではない。");
#else
		if( you_were_host )
			len[0] = wsprintf(ach[0], "Resume Start ->>>");
		else
			len[0] = wsprintf(ach[0], "You have no resumed data as your host.");
#endif

		destination.left=80;
		destination.top=400;

		if( Mode==GameMode.GameSetting )
			{
			destination.right=destination.left+(length[0]*12);
			destination.bottom=destination.top+24;
			if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 && RivalMode==GameMode.GameSetting && WasHost )
				{
				SetTextColor(hdc, RGB(255, 0, 0));

				if( LeftButton==3 )
					{
					StartFromResume(ref message);
					}
				}
			else
				SetTextColor(hdc, RGB(255, 255, 255));
			}
		else
			{
			SetTextColor(hdc, RGB(126, 126, 126));
			}

		TextOut(hdc, destination.left, destination.top, text[0], length[0]);
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
	}

// A click on シナリオ切り替え: the other page of the list, with its first scenario chosen.
private void TurnScenarioPage()
	{
	if(ScenarioNumber<=99)
		ScenarioNumber=101;
	else if(ScenarioNumber<=199)
		ScenarioNumber=1;
	}

private void UpdateScenarioPageButton(ref Array12<int> length, ref Array12<Array128<byte>> text, ref HDC hdc)
	{
	RECT destination;
#if !LNGG_VER
	length[0] = wsprintf(text[0], "シナリオ切り替え ->>>");
#else
	len[0] = wsprintf(ach[0], "Page Change ->>>");
#endif

	destination.left=250;
	destination.top=110;
	destination.right=destination.left+(length[0]*12);
	destination.bottom=destination.top+24;
	if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 && RivalMode==GameMode.GameSetting )
		{
		SetTextColor(hdc, RGB(255, 0, 0));

		if( LeftButton==3 )
			{
			TurnScenarioPage();
			}
		}
	else
		SetTextColor(hdc, RGB(255, 255, 255));

	TextOut(hdc, destination.left, destination.top, text[0], length[0]);
	}

private void ShowScenarioList(int m, ref HDC hdc, ref Array12<Array128<byte>> text, ref Array12<int> length)
	{
	for( var n=0; n<=m; n++)
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

		TextOut(hdc, 120, 150+(n*25), text[n], length[n]);
		}

	if(JoinGameStart==MessageType.LeaveGameSetting && !IsHost && Mode==GameMode.GameSetting )
		{
		// ジョインが受け取る
		Mode=GameMode.ConfigSetting;
		JoinGameStart=0;
		}
	}

private void ListScenarioNames(ref int m, ref Array12<int> length, ref Array12<Array128<byte>> text)
	{
	if( ScenarioNumber<=99 )
		{
		m=0;
		length[m] = wsprintf(text[m], "空母機動部隊の戦い１");
		m++;
		length[m] = wsprintf(text[m], "空母機動部隊の戦い２");
		m++;
		length[m] = wsprintf(text[m], "空母機動部隊の戦い３");

		m++;
		length[m] = wsprintf(text[m], "艦隊決戦１");
		m++;
		length[m] = wsprintf(text[m], "艦隊決戦２");

		m++;
		length[m] = wsprintf(text[m], "ミッドウェイ攻略１");
		m++;
		length[m] = wsprintf(text[m], "ミッドウェイ攻略２");
		m++;
		length[m] = wsprintf(text[m], "中部太平洋の戦い１");
		m++;
		length[m] = wsprintf(text[m], "ユーザーシナリオ");
		}
	else if(ScenarioNumber<=199)
		{
		m=0;
		length[m] = wsprintf(text[m], "ガダルカナルを巡る戦い１");
		m++;
		length[m] = wsprintf(text[m], "ガダルカナルを巡る戦い２");

		m++;
		length[m] = wsprintf(text[m], "日本近海の戦い１");

		m++;
		length[m] = wsprintf(text[m], "南太平洋の戦い１");
		m++;
		length[m] = wsprintf(text[m], "南太平洋の戦い２");
		m++;
		length[m] = wsprintf(text[m], "ガ島争奪戦");

		}
	else if(ScenarioNumber<=299)
		{
		m=0;
		length[m] = wsprintf(text[m], "硫黄島攻略１");
		m++;
		length[m] = wsprintf(text[m], "硫黄島攻略２");
		m++;
		length[m] = wsprintf(text[m], "日本近海の戦い１");
		m++;
		length[m] = wsprintf(text[m], "日本近海の戦い２");
		m++;
		length[m] = wsprintf(text[m], "日本近海の戦い３");
		}
	}

//============================================================================
// 通信対戦セットアップ
// とりあえず、シナリオとサイドを選んでゲームへ
//----------------------------------------------------------------------------
[Original("cnct_game_setting")]
public void	UpdateGameSetting()
	{
	int m=default /* C4701 */, x = default;  
    Array12<Array128<byte>> text = default;
    Array12<int> length = default;
	HDC					hdc;

	_DP_DATA_1		message = default;
	_DP_FLAG			flagMessage = default;

#if CONN_DBG
	rival_mode=mode;
#endif

	ClearFlag=1;

	// マウス情報

	if( IsEditingMap )
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
		length[0] = wsprintf(text[0], "通信対戦セッティング");
#else
		len[0] = wsprintf(ach[0], "Connection setting");
#endif
		TextOut(hdc, 300, 80-26, text[0], length[0]);

		IDirectDrawSurface_ReleaseDC(lpDDSBack, hdc);
		}

		if ( IDirectDrawSurface_GetDC(lpDDSBack, &hdc) == DD_OK )
			{
			SetBkMode(hdc, TRANSPARENT);
			SelectObject(hdc, gameFont_1);

#if !LNGG_VER
			length[0] = wsprintf(text[0], "シナリオ選択画面");
#else
			len[0] = wsprintf(ach[0], "Mission Menu");
#endif

			if(Mode==GameMode.GameSetting)
				SetTextColor(hdc, RGB(255, 255, 255));
			else
				SetTextColor(hdc, RGB(126, 126, 126));

			TextOut(hdc, 200, 80, text[0], length[0]);

#if !LNGG_VER
			ListScenarioNames(ref m, ref length, ref text);
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

			// The host's UI's commands for the list (UICommands.cs).
			RunUICommands(UICommandSite.ScenarioList, IsHost && Mode==GameMode.GameSetting);

			if( IsHost && Mode==GameMode.GameSetting )
				{
				UpdateScenarioList(m, ref length, ref hdc, ref message, ref text);
				}
			else
				{
				ShowScenarioList(m, ref hdc, ref text, ref length);
				}

			if( IsHost && Mode==GameMode.GameSetting )
				{
				UpdateScenarioPageButton(ref length, ref text, ref hdc);
				}

			// The host's UI's commands for the page, the starts from saved battles and the sides (UICommands.cs).
			RunUICommands(UICommandSite.ScenarioMenu, IsHost && Mode==GameMode.GameSetting);

			// リジュームスタート
			UpdateResumeButton(ref x, ref length, ref text, ref hdc, ref message);

			// オートセーブスタート
			UpdateAutoSaveButton(ref x, ref length, ref text, ref hdc, ref message);

			// 操作対象の切り替え
			UpdateSideSetting(ref x, ref length, ref text, ref hdc);

#if !LNGG_VER
			DrawScenarioDescription(ref length, ref text);
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
			for( var n=0; n<=6; n++)
				{
				SetTextColor(hdc, RGB(255, 255, 255));
				TextOut(hdc, x, 500+(n*25), text[n], length[n]);
				}
	}
else
	{
	for( var n=0; n<=6; n++)
		{
		SetTextColor(hdc, RGB(126, 126, 126));
		TextOut(hdc, x, 500+(n*25), text[n], length[n]);
		}
	}

			// コンフィギュレーション

if(	Mode==GameMode.ConfigSetting )
	{
			UpdateConfigSetting(ref length, ref text, ref hdc, ref flagMessage, ref message);

	}
else
	{
	// The host's UI's commands for the rules, which are not shown, are dropped (UICommands.cs).
	RunUICommands(UICommandSite.Rules, false);
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
		flagMessage.dwType = MessageType.RivalMode;
		flagMessage.rival_mode=(short)Mode;

		bufferDesc.dwBufferSize = (uint)(sizeof(_DP_FLAG));
		bufferDesc.pBufferData  = (byte*) &flagMessage;
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
