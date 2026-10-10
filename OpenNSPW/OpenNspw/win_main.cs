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

// Port of win_main.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{

// ウィンドウアプリケーション変数
public object? hInstApp;
public HWND hwndApp;
public object? my_cursor;
//D3DPRESENT_PARAMETERS d3dpp;		// Direct3D is not used.
[Original("appActive")] public Bool32 IsAppActive;
[Original("fullscreen")] public Bool32 IsFullscreen;

//	DirectXオブジェクト

public IDirectDraw7? lpDD=null;
public IDirectDrawSurface7? lpDDSPrimary=null;
public IDirectDrawSurface7? lpDDSBack=null;

public IDirectDrawClipper? lpDDclip=null;

public IDirectDrawSurface7? lpDDS_OS=null;

// DirectInputの変数
public IDirectInput8? pDInput = null; // DirectInput
public IDirectInputDevice8? pDIDevice = null; // DirectInputデバイス
public IDirectInputDevice8? pDIDeviceMouse = null; // DirectInputデバイス
//DIDEVCAPS				diDevCaps;				// ジョイスティックの能力 (joysticks are not used)

// DirectSoundの変数
public IDirectSound8? lpDS = null;
public IDirectSoundBuffer? lpDSP = null;

public Array35<Array6<IDirectSoundBuffer?>> lpDSB_;
[Original("snd_")] public Array35<short> NextSoundBuffers;

// DirectMusicの変数
public IDirectMusicLoader8? lpDML = null;
public IDirectMusicPerformance8? lpDMP = null;

// フォント
public HFONT gameFont_1;
public HFONT gameFont_2;

// ゲーム用
[Original("cc_count")] public int Tick;
[Original("key_cndtn")] public InputButtons Buttons; // パッドの状態

//POINT	ptCursor;		// 純粋なマウスカーソルの位置

// デバグ
[Original("dbg_menu")] public int DebugMenu;
[Original("dbg")] public Array16<int> DebugValues;

// 通信対戦用
public Guid g_guidApp = new( 0x11bc0eb, 0xbdb3, 0x11d6, 0xba, 0x95, 0x9c, 0xce, 0x36, 0x89, 0x70, 0x55 );

public IDirectPlay8ThreadPool? g_pThreadPool = null; // DirectPlay threadpool object
public IDirectPlay8Peer? g_pDP = null; // DirectPlay peer object

public HKEY hDPlaySampleRegKey; // レジストリ

public HWND g_hDlg = null; // HWND of main dialog
[Original("dlg_answer")] public MessageType DialogAnswer;

public uint g_dpnidLocalPlayer = 0; // DPNID of local player
public uint g_dpnidRivalPlayer = 0; // DPNID of local player
//DPNID                      g_dpnidHostPlayer             = 0;       // DPNID of host player
[Original("g_dwNumberOfActivePlayers")] public uint ActivePlayerCount = 0; // Number of players currently in game
[Original("g_strAppName")] public Array256<byte> AppName = TEXT<Array256<byte>>("NSPW NET");

[Original("g_strLocalPlayerName")] public Array260<byte> LocalPlayerName; // Local player name
[Original("g_strRivalPlayerName")] public Array260<byte> RivalPlayerName =TEXT<Array260<byte>>(" - - - ");          // Rival player name
//TCHAR                      g_strSessionName[MAX_PATH];              // Session name
[Original("g_strPreferredProvider")] public Array260<byte> PreferredProvider; // Provider string
[Original("g_strRemoteHostname")] public Array260<byte> RemoteHostName; // TCP/IP remote host

[Original("g_bHostPlayer")] public int IsHostPlayer = FALSE; // TRUE if local player is host
public Guid? g_pCurSPGuid = null; // Currently selected guid
public uint g_hConnectAsyncOp = 0; // Async handle for connecting to host

public DPN_BUFFER_DESC bufferDesc;
public uint hAsync;

// チャット
public HWND hwndChatDlg = null; // HWND of chat dialog

/////////////////////

// iNSPWからもってきたやつ

[Original("sprt")] public SpriteArray Sprites;

[Original("missed_pending")] public uint MissedPending;
[Original("a_paint_speed")] public uint PaintSpeed;

public int FrameCount;

//unsigned char		*dst_vram;							//書き込むＶＲＡＭのアドレス

//unsigned char		*src_vram;							//読み込むＶＲＡＭのアドレス

[Original("scrn_mode")] public int ScreenMode;

[Original("video_memory")] public int VideoMemory;

[Original("anti_air")] public int ShowsAntiAir;
[Original("reveal")] public int RevealsAll;

[Original("cmbt_x", "cmbt_y")] public WorldPosition CameraPosition;
[Original("scrn_moving_spd")] public double ScrollSpeed;
[Original("unit")] public Array256<Unit> Units;
[Original("max_unit")] public short MaxUnitId;
[Original("the_slct_unit")] public short SelectedUnit;
[Original("old_the_slct_unit")] public short PreviousSelectedUnit;
[Original("slct_unit_no")] public short SelectionCount;
[Original("slct_unit")] public Array2<Array256<short>> Selections;
[Original("crsr_pt")] public POINT CursorPosition;

[Original("unit_info")] public Array10<int> UnitInfoPanel;
[Original("fire")] public Array512<Fire> Fires;
[Original("max_fire")] public int MaxFireId;
[Original("effect")] public Array1024<Effect> Effects;

[Original("cls_flg")] public short ClearFlag;

[Original("cmbt_map")] public Array256<Array256<ushort>> MapTiles; // マップ
[Original("kumo")] public Array4096<Cloud> Clouds; // 雲

[Original("lf_btn")] public short LeftButton;
[Original("ri_btn")] public short RightButton;
[Original("cmbt_menu_kind")] public short CombatMenuKind;
[Original("cmbt_menu_slctd")] public CombatMenuItem CombatMenuSelection;

[Original("wrk_pp_x")] public Array64<double> WorkPathX;
[Original("wrk_pp_y")] public Array64<double> WorkPathY; //（ワーク）移動目的地の地図上の位置

[Original("new_pp")] public Array3<MoveOrder> MoveOrders;
[Original("new_slct")] public Array3<SelectOrder> SelectOrders;
[Original("new_menu")] public Array3<MenuOrder> MenuOrders;

[Original("rest_time")] public int BattleTime;
[Original("game_end")] public GameResult Result;
[Original("decision_point")] public Array4<int> DecisionPoints;
[Original("your_side")] public Side LocalSide;
[Original("game_speed")] public short GameSpeed;

[Original("mode")] public GameMode Mode;
[Original("demo_time")] public short TitleTime;
[Original("sinario")] public short ScenarioNumber;

[Original("map_edit")] public Bool8 IsEditingMap;
[Original("put_trgt")] public byte EditorTarget;
[Original("put_kind")] public byte EditorKind;
[Original("put_kind_sub")] public byte EditorVariant;
[Original("rein")] public Array3<byte> Reinforcements;

[Original("user_sinario_fn")] public Array260<byte> UserScenarioFileName; // ユーザーシナリオのファイルネーム

// 通信対戦用
[Original("cnct_game")] public int			IsNetworkGame;
[Original("you_are_host")] public Bool8 IsHost;
[Original("you_were_host")] public Bool8 WasHost;

[Original("you_can_order")] public Bool32 CanOrder;
[Original("you_ordered")] public Bool32 HasOrdered;

[Original("bf_new_pp")] public Array3<MoveOrder> BufferedMoveOrders;
[Original("bf_new_slct")] public Array3<SelectOrder> BufferedSelectOrders;
[Original("bf_new_menu")] public Array3<MenuOrder> BufferedMenuOrders;

[Original("bf_game_system_menu")] public Array3<byte> BufferedSystemOrders;
[Original("game_system_menu")] public Array3<byte> SystemOrders;

[Original("bf_slct_unit")] public Array3<Array90<short>> BufferedSelections;
[Original("go_next_1")] public Bool32 CanAdvance1;
[Original("go_next_2")] public Bool32 CanAdvance2;
[Original("join_game_start")] public MessageType JoinGameStart;
[Original("rival_mode")] public GameMode RivalMode;
[Original("my_rnd_sheet")] public Array4096<int> SharedRandomTable;
[Original("my_rnd_pt")] public short SharedRandomIndex;
[Original("cnct_game_rnd_sheed")] public short SharedRandomSeed;

[Original("host_side")] public int HostSide;
[Original("decision_sw")] public Bool8 IsDecisionEnabled;
[Original("arrival_cont")] public byte ArrivalControl;

[Original("rnd_count")] public int RandomCount;
[Original("bf_cc_count")] public Array2<byte> TickChecksums;
[Original("bf_rnd_count")] public Array2<byte> RandomChecksums;
[Original("bf_unit_chk")] public Array2<byte> UnitChecksums;
[Original("ccc_out")] public Bool32 IsTickOutOfSync;
[Original("rnd_out")] public Bool32 IsRandomOutOfSync;
[Original("unit_out")] public Bool32 AreUnitsOutOfSync;
[Original("ccc_wait")] public Array2<byte> TickWaits;
[Original("cnct_loop_ct")] public short TurnCounter;
[Original("cnct_loop")] public short TurnLength;
[Original("cnct_loop_pt1")] public short SyncTick1;
[Original("cnct_loop_pt2")] public short SyncTick2;

[Original("spry_pt")] public short SupplyPoints;
[Original("spry_no_cont")] public short SupplyCount;
[Original("spry_trgt")] public short SupplyTarget;
[Original("spry_rate")] public Array2<short> SupplyRates;
[Original("first_spry_pt")] public Array2<short> InitialSupplyPoints;
[Original("rvrs_time")] public short SwapTime;
[Original("rvrs_rule")] public short SwapRule;
[Original("map_now")] public short CurrentMap;
[Original("bf_arrived_unit")] public Array2<short> BufferedArrivedUnits;

[Original("auto_save_time")] public short AutoSaveTime;

[Original("last_tick")] public uint LastTime;
[Original("last_tick2")] public uint LastTime2;
[Original("tick_now")] public uint Now;
[Original("tick_diff")] public uint Elapsed;

// 通信対戦デバグ用
[Original("first_r_error")] public Bool8 HasSavedDesync;

// チャット用
[Original("input_chat_now")] public int IsTypingChat=0;

[Original("my_chat")] public Array260<byte> MyChat;
[Original("friend_chat")] public Array260<byte> RivalChat;

[Original("my_chat_dsp_time")] public byte MyChatDisplayTime;
[Original("friend_chat_dsp_time")] public byte RivalChatDisplayTime;

//
[Original("rival_ver")] public Array16<byte> RivalVersion;

private void DrawRivalChat(ref Array128<byte> ach, ref HDC hdc)
	{
	int len;
	int m;
	if(RivalChatDisplayTime!=0)
		{
		len = wsprintf(ach, RivalChat );
		TextOut(hdc, 10, 240+170, ach, len);
		RivalChatDisplayTime--;
		if(RivalChatDisplayTime==0)
			{
			for(m=0;m<128;m++)
				{
				RivalChat[m]=0;
				}
			}
		}
	}

private void DrawMyChat(ref Array128<byte> ach, ref HDC hdc)
	{
	int len;
	int m;
	if(MyChatDisplayTime!=0)
		{
		len = wsprintf(ach, MyChat );
		TextOut(hdc, 10, 200+170, ach, len);
		MyChatDisplayTime--;
		if(MyChatDisplayTime==0)
			{
			for(m=0;m<128;m++)
				{
				MyChat[m]=0;
				}
			}
		}
	}

private void DrawDebugInfo(ref int len, ref Array128<byte> ach, int no)
	{
	switch( DebugMenu )
		{
		case 0:
			len = wsprintf(ach, "%d  :%d  :%d  :%d  :%d", Units[SelectedUnit].info[1], Units[SelectedUnit].info[4], Units[SelectedUnit].info[7], Units[SelectedUnit].info[8], Units[81].info[0] );
			break;
		case 1:
			len = wsprintf(ach, "put_trgt %d put_kind %d cmbt_x %d cmbt_y %d crsr_pt.x %d crsr_pt.y %d ",EditorTarget, EditorKind, (int)CameraPosition.X, (int)CameraPosition.Y, (int)CursorPosition.x, (int)CursorPosition.y  );
			break;
		case 2:
			len = wsprintf(ach, "cnct_game=%d you_are_host=%d you_can_order=%d new_pp[1].used=%d",IsNetworkGame,IsHost,CanOrder,MoveOrders[1].Unit);
			break;
		case 3:
			len = wsprintf(ach, "you_ordered=%d you_can_order=%d new_pp[1].used=%d",HasOrdered,CanOrder,MoveOrders[1].Unit);
			break;
		case 4:
			len = wsprintf(ach, "game_speed=%d",GameSpeed);
			break;
		case 5:
			len = wsprintf(ach, "CR_X=%02d CR_Y=%02d Ri_btn=%d Lf_btn=%d ", CursorPosition.x,CursorPosition.y, RightButton, LeftButton );
			break;

		case 6:
			len = wsprintf(ach, "go_next_1=%d go_next_2=%d", CanAdvance1, CanAdvance2 );
			break;

		case 7:
			len = wsprintf(ach, "hp[0]=%d arm[0]=%d", Units[no].Hp, Units[no].Weapon );
			break;

		case 8:
			len = wsprintf(ach, "spry_rate[0]=%d [1]=%d  first_sply_pt[0]=%d [1]=%d  rvrs_time=%d _rule=%d", SupplyRates[0], SupplyRates[1], InitialSupplyPoints[0], InitialSupplyPoints[1], SwapTime, SwapRule );
			break;

		default:
			DebugMenu=0;
			break;
		}
	}

/////////////////////

/*-------------------------------------------
	アプリがアクティブの時のアイドリング
	ゲームアプリのメインループ
--------------------------------------------*/
[Original("updateFrame")]
public void	UpdateFrame()
	{
	DDBLTFX ddbltfx;
	short		chara_loop;
	int	i;
	int	no;

	Now = timeGetTime();

	Elapsed = Now - LastTime2;

	if( IsEditingMap )
		chara_loop=1;
	else if( Mode==GameMode.Battle )
		{
		if( Buttons.HasFlag(InputButtons.Space) )
			chara_loop=20;
		else
			chara_loop=GameSpeed;
		}
	else
		chara_loop=1;

	if( chara_loop==1 && Elapsed<=45+5 )
		{
		return;
		}

	LastTime2 = timeGetTime();

	ReadInput();		// マウス、キーボード状況取得

	// 塗りつぶし
	ZeroMemory(&ddbltfx,(nuint)(sizeof(DDBLTFX)));
	ddbltfx.dwSize = (uint)(sizeof( DDBLTFX ));

	switch( Mode )
		{
		case GameMode.Title:
			// 塗りつぶし
			ddbltfx.dwFillColor = 0x0000;
			if( DDERR_SURFACELOST == IDirectDrawSurface_Blt( lpDDSBack,null,null,null,DDBLT_COLORFILL | DDBLT_WAIT,&ddbltfx ))
				{
				RestoreSurfaces();
				return;
				}

			UpdateTitle();
			break;

		case GameMode.GameSetting:
		case GameMode.ConfigSetting:
			// 塗りつぶし
			ddbltfx.dwFillColor = 0x0300;
			if( DDERR_SURFACELOST == IDirectDrawSurface_Blt( lpDDSBack,null,null,null,DDBLT_COLORFILL | DDBLT_WAIT,&ddbltfx ))
				{
				RestoreSurfaces();
				return;
				}

			UpdateGameSetting();
			break;

		case GameMode.Battle:
			// 塗りつぶし
			ddbltfx.dwFillColor = 0x0016;
			if( DDERR_SURFACELOST == IDirectDrawSurface_Blt( lpDDSBack,null,null,null,DDBLT_COLORFILL | DDBLT_WAIT,&ddbltfx ))
				{
				RestoreSurfaces();
				return;
				}

			for( i=chara_loop; i>=1; i-- )
				{
				UpdateBattle();
				if( !IsEditingMap )
					CheckResult();
				}

			UpdateUnitInfo();
			DrawBattleArea();
			DrawMinimap();

			if( !IsEditingMap )
				{
				CheckResult();
				HandleInput();
				}

			if(IsEditingMap )
				{
				UpdateMapEditor();
				}

			break;
		}

	HDC hdc;
	Array128<byte> ach = default;
	int len;

	if (DD_OK==lpDDSBack.GetDC(&hdc))
		{
		SetTextColor(hdc,RGB(255,255,255));
		SetBkMode(hdc, TRANSPARENT);

#if DBG_MODE

		no=SelectedUnit;

		len = wsprintf(ach, "dbg[0]=%d [1]=%d [2]=%d [3]=%d [4]=%d  :%d",DebugValues[0],DebugValues[1],DebugValues[2],DebugValues[3],DebugValues[4] ,SelectedUnit   );
		TextOut(hdc, 0, 0, ach, len);

		len = wsprintf(ach, "unit_out=%d ccc_out=%d rnd_out=%d rnd_count=%d CCC=%d FrameC=%d Sinario=%d host_side=%d rival_mode=%d" ,AreUnitsOutOfSync ,IsTickOutOfSync,IsRandomOutOfSync,RandomCount ,Tick, FrameCount, ScenarioNumber, HostSide, RivalMode);
		TextOut(hdc, 0, 20, ach, len);

		len = wsprintf(ach, "Dbg[5]=%d  bf_cc_count[0]=%d [1]=%d   bf_rnd_count[0]=%d [1]=%d  ccc_wait[0]=%d  [1]=%d " ,DebugValues[5], TickChecksums[0], TickChecksums[1], RandomChecksums[0], RandomChecksums[1], TickWaits[0], TickWaits[1] );
		TextOut(hdc, 0, 40, ach, len);

		DrawDebugInfo(ref len, ref ach, no);

		TextOut(hdc, 0, 60, ach, len);
#endif

#if !LNGG_VER
			if(AreUnitsOutOfSync)
				{
				len = wsprintf(ach, "ユニットデータ同期異常、ゲームを中断されたし。");
				TextOut(hdc, 0, 100, ach, len);
				}
			if(IsTickOutOfSync)
				{
				len = wsprintf(ach, "プログラム同期異常、ゲームを中断されたし。");
				TextOut(hdc, 0, 120, ach, len);
				}
			if(IsRandomOutOfSync)
				{
				len = wsprintf(ach, "ランダム同期異常、ゲームを中断されたし。");
				TextOut(hdc, 0, 140, ach, len);
				}
			if( ActivePlayerCount!=2  )
				{
				len = wsprintf(ach, "接続相手がいなくなりました。");
				TextOut(hdc, 0, 160, ach, len);
				}
#else
			if(unit_out)
				{
				len = wsprintf(ach, "Unit data synchronisum error! Exit application.");
				TextOut(hdc, 0, 100, ach, len);
				}
			if(ccc_out)
				{
				len = wsprintf(ach, "Program synchronisum error! Exit application.");
				TextOut(hdc, 0, 120, ach, len);
				}
			if(rnd_out)
				{
				len = wsprintf(ach, "Random synchronisum error! Exit application.");
				TextOut(hdc, 0, 140, ach, len);
				}
			if( cnct_now==0  )
				{
				len = wsprintf(ach, "No connection object.");
				TextOut(hdc, 0, 160, ach, len);
				}
#endif

		SetBkMode(hdc, TRANSPARENT);
		SelectObject(hdc, gameFont_1);

		SetTextColor(hdc, RGB(255, 255, 0));

		DrawMyChat(ref ach, ref hdc);

		DrawRivalChat(ref ach, ref hdc);

		lpDDSBack.ReleaseDC(hdc);
		}

	// マウス入力の後処理
	if(RightButton==1)	RightButton=2;
	if(RightButton==3)	RightButton=0;

	if(LeftButton==1)	LeftButton=2;
	if(LeftButton==3)	LeftButton=0;

	FrameCount++;

	// プライマリサーフェスにフリップ

	IDirectDrawSurface_SetClipper(lpDDSPrimary,lpDDclip);
	if( DDERR_SURFACELOST == IDirectDrawSurface_Blt(lpDDSPrimary,null,lpDDSBack,null,DDBLT_WAIT,null))
		RestoreSurfaces();

	}

/*--------------------------------------------
	メイン
---------------------------------------------*/
public int WinMain(object? hInst,object? hPrevInst,string lpCmdLine,int nCmdShow)
	{
	MSG msg=default;
	WNDCLASS wc;

	int	i;

	for(i=0; i<16; i++)
		DebugValues[i]=0;

	IsFullscreen=true;
	hInstApp=hInst;

	InitCommonControls();

	// Read persistent state information from registry
	RegCreateKeyEx( HKEY_CURRENT_USER, "Software\\Microsoft\\DirectX DirectPlay Samples", 0, null,
                    REG_OPTION_NON_VOLATILE, KEY_READ | KEY_WRITE, null,
                    ref hDPlaySampleRegKey, null );

    DXUtil_ReadStringRegKeyCch( hDPlaySampleRegKey, TEXT("Player Name"),
                             LocalPlayerName, MAX_PATH, TEXT("TestPlayer") );
    DXUtil_ReadStringRegKeyCch( hDPlaySampleRegKey, TEXT("Preferred Provider"),
                             PreferredProvider, MAX_PATH,
                             TEXT("DirectPlay8 TCP/IP Service Provider") );
    DXUtil_ReadStringRegKeyCch( hDPlaySampleRegKey, TEXT("Remote Hostname"),
                             RemoteHostName, MAX_PATH,
                             TEXT("localhost") );

	// COM 初期化
	CoInitializeEx( null, COINIT_MULTITHREADED );

	// Create IDirectPlay8ThreadPool
	// ＤＰをバックスレッドでなく通常のルーティン処理とするためにこれを設定する。
	if( FAILED( CoCreateInstance( CLSID_DirectPlay8ThreadPool, null, CLSCTX_INPROC_SERVER,IID_IDirectPlay8ThreadPool, out g_pThreadPool ) ))
        return (false ? 1 : 0);

	// Init IDirectPlay8ThreadPool
	g_pThreadPool.Initialize( null, DirectPlayMessageHandler, DPNINITIALIZE_DISABLEPARAMVAL );

	// Put DirectPlay in "DoWork" mode
   g_pThreadPool.SetThreadCount( unchecked((uint)((uint) -1)), 0, 0 );

	// Create IDirectPlay8Peer
	if( FAILED( CoCreateInstance( CLSID_DirectPlay8Peer, null, CLSCTX_INPROC_SERVER, IID_IDirectPlay8Peer, out g_pDP ) ))
		return (false ? 1 : 0);

	// Init IDirectPlay8Peer
	g_pDP.Initialize( null, DirectPlayMessageHandler, DPNINITIALIZE_DISABLEPARAMVAL );

	// Create the initial dialog.
	// 接続ダイアログ 　オーナーウィンドウが無いため、モーダレスになります。
	g_hDlg = CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_ADDRESS_OVERRIDE), null, OverrideDlgProc);

	int    wait_for_connect = TRUE;

	// ホストも、ゲストもここで接続が完了するまでループ
	// 接続確認ダイアログ
	while (wait_for_connect!=0)
		{
		// Retrieve any windows messages.
		while (PeekMessage(ref msg, null, 0, 0, PM_REMOVE)!=0)
			{
			if (msg.message == WM_QUIT)
				{
				// Quit the application.
				wait_for_connect = FALSE;
				break;
				}

			if ( IsDialogMessage(g_hDlg, ref msg)==0 )
				{
				TranslateMessage(ref msg);
				DispatchMessage(ref msg);
				}
	     }
		// Let DirectPlay process network events and call our message handler.
		g_pThreadPool.DoWork(DOWORK_TIMESLICE, 0);
		Sleep(1);
		}

	if( ActivePlayerCount<2 )
		{
		// 接続キャンセル
		EndDialog( g_hDlg, 0 );
		EndApp( );
		return (int)msg.wParam;
		}

	IsHost=new Bool8((byte)IsHostPlayer);

	g_hDlg=null;

	// メインウインドウを作成する
	ZeroMemory(&wc,(nuint)(sizeof(WNDCLASS)));
	wc.hbrBackground = (HBRUSH)GetStockObject(WHITE_BRUSH);
	wc.hInstance = hInst;
	wc.lpfnWndProc = (WNDPROC)MainWndProc;
	wc.lpszClassName = CLASS_NAME;
	wc.hCursor = LoadCursor(null, IDC_ARROW);

	RegisterClass(ref wc);

	int width,height;
	// ウインドウの大きさを計算
	if (IsFullscreen)
		{
		// フルスクリーン時はそのままで OK
		width = WIDTH;
		height = HEIGHT;
		hwndApp = CreateWindow(CLASS_NAME,CAPTION, WS_POPUP ,CW_USEDEFAULT,CW_USEDEFAULT,width,height,null,null,hInst,null);
		}
	else
		{
		// ウインドウ時はウインドウの外枠のサイズを考慮する
		width = WIDTH + GetSystemMetrics(SM_CXDLGFRAME) * 2;
		height = HEIGHT + GetSystemMetrics(SM_CYDLGFRAME) * 2 + GetSystemMetrics(SM_CYCAPTION);
		hwndApp = CreateWindow(CLASS_NAME,CAPTION, WS_OVERLAPPED | WS_SYSMENU | WS_MINIMIZEBOX /*| WS_VISIBLE*/ ,CW_USEDEFAULT,CW_USEDEFAULT,width,height,null,null,hInst,null);
		}

	ShowWindow(hwndApp,SW_SHOWNORMAL);
	UpdateWindow(hwndApp);

	ImmAssociateContext(hwndApp,null);			// 日本語入力機能を通常ゲーム時抑制する。

    int		ddrval;

	// ダイレクトドロウ
    ddrval = DirectDrawCreateEx(null,out lpDD,IID_IDirectDraw7,null);
    if (ddrval != DD_OK)
		{
		MessageBox(null,"DirectDrawの作成に失敗","Base",MB_OK | MB_ICONSTOP);
		return FALSE;
		}

    // 協調レベルを設定
	if (IsFullscreen)
	    ddrval = lpDD.SetCooperativeLevel(hwndApp, DDSCL_EXCLUSIVE | DDSCL_FULLSCREEN );
	else
	    ddrval = lpDD.SetCooperativeLevel(hwndApp, DDSCL_NORMAL );
	if (ddrval != DD_OK)
		{
		MessageBox(null,"残念！","って言うじゃなーい。",MB_OK | MB_ICONSTOP);
		return FALSE;
		}

	// ディスプレイモードを設定
	if(IsFullscreen)
		{
		ddrval = lpDD.SetDisplayMode( width, height, 16, 0, 0);
		if (ddrval !=DD_OK)
			{
			MessageBox(null,"SetDisplayMode","残念！",MB_OK | MB_ICONSTOP);
			return FALSE;
			}
		}

	// バックバッファをひとつ持つプライマリサーフェスを作成
	DDSURFACEDESC2 ddsd;
	DDSCAPS2 ddscaps;

	ZeroMemory(&ddsd,(nuint)(sizeof(DDSURFACEDESC2)));
	ddsd.dwSize = (uint)(sizeof( DDSURFACEDESC2 ));
	ddsd.dwFlags = DDSD_CAPS | DDSD_BACKBUFFERCOUNT;
	ddsd.ddsCaps.dwCaps = DDSCAPS_PRIMARYSURFACE |
						  DDSCAPS_FLIP |
						  DDSCAPS_COMPLEX;
	ddsd.dwBackBufferCount = 1;
	ddrval = lpDD.CreateSurface( &ddsd, out lpDDSPrimary, null );
	if (ddrval!=DD_OK)
		{
		MessageBox(null,"CreateSurface","残念！",MB_OK | MB_ICONSTOP);
		return FALSE;
		}

	// バックバッファインターフェイスを取得
	ZeroMemory(&ddscaps, (nuint)(sizeof(DDSCAPS2)));
	ddscaps.dwCaps=DDSCAPS_BACKBUFFER;
	ddrval=lpDDSPrimary.GetAttachedSurface(&ddscaps,out lpDDSBack);
	if (ddrval!=DD_OK)
		{
		MessageBox(null,"GetAttachedSurface","残念！",MB_OK | MB_ICONSTOP);
		return FALSE;
		}

	// 最初のイメージをロードし、表示
	lpDDS_OS=bitmap_surface("t3.bmp");
	if (lpDDS_OS==null)
		{
		MessageBox(null,"bitmap_surface","残念！",MB_OK | MB_ICONSTOP);
		return FALSE;
		}

	// クリッパー
	if(IsFullscreen)
		{
		lpDD.CreateClipper(0,out lpDDclip,null);
		lpDDclip.SetHWnd(0,hwndApp);
		lpDD.FlipToGDISurface();
		}
	else
		{
		lpDD.CreateClipper(0,out lpDDclip,null);
		lpDDclip.SetHWnd(0,hwndApp);
		lpDDSPrimary.SetClipper(lpDDclip);
		}

	// ピクセルフォーマットを取得
	DDPIXELFORMAT ddpf;
	ddpf.dwSize=(uint)(sizeof(DDPIXELFORMAT));
	lpDDS_OS.GetPixelFormat(&ddpf);

	uint KeyColor;
	KeyColor = ddpf.dwRBitMask | ddpf.dwBBitMask;

	// カラーキーを設定
	DDCOLORKEY key;
	key.dwColorSpaceLowValue = KeyColor;
	key.dwColorSpaceHighValue = KeyColor;
	lpDDS_OS.SetColorKey(DDCKEY_SRCBLT, &key);

	if(!InitializeDirectInput())
		return FALSE;
#if SND_SW
	if(InitializeDirectSound()==0)
		return FALSE;
	if(InitializeDirectMusic()==0)
		return FALSE;
#endif

	// ゲーム変数初期化
	InitializeRegistry();

	IsAppActive=true;

	InitializeSprites();

	TitleTime=0;

	Mode=GameMode.Title;

	//メッセージループ
	while(TRUE!=0)
		{

		if(  PeekMessage(ref msg,null,0,0,PM_NOREMOVE)!=0 )
			{
			if(GetMessage(ref msg,null,0,0)==0)
				break;

			TranslateMessage(ref msg);
			DispatchMessage(ref msg);
			}
		else
			{
			if( ( IsAppActive || hwndChatDlg!=null ) && g_hDlg==null )
				{
				UpdateFrame();
				}
			else
				{
				WaitMessage();
				}

			if( g_pThreadPool!=null && g_pDP!=null )
				g_pThreadPool.DoWork(DOWORK_TIMESLICE, 0);
			}

		}

	return (int)msg.wParam;

	}
}
