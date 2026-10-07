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
//#include	"all_typedef.h"
//#include	"all_forward.h"

// Port of win_main.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{


// ウィンドウアプリケーション変数
public object?	hInstApp;
public HWND	hwndApp;
public object?		my_cursor;
//D3DPRESENT_PARAMETERS d3dpp;		// Direct3D is not used.
public int	appActive;
public int	fullscreen;


//	DirectXオブジェクト

public IDirectDraw7?			lpDD=null;
public IDirectDrawSurface7? lpDDSPrimary=null;
public IDirectDrawSurface7? lpDDSBack=null;

public IDirectDrawClipper?	lpDDclip=null;

public IDirectDrawSurface7? lpDDS_OS=null;



// DirectInputの変数
public IDirectInput8?			pDInput = null;					// DirectInput
public IDirectInputDevice8?	pDIDevice = null;			// DirectInputデバイス
public IDirectInputDevice8?	pDIDeviceMouse = null;			// DirectInputデバイス
//DIDEVCAPS				diDevCaps;				// ジョイスティックの能力 (joysticks are not used)


// DirectSoundの変数
public IDirectSound8?			lpDS = null;
public IDirectSoundBuffer?	lpDSP = null;

public Array35<Array6<IDirectSoundBuffer?>> lpDSB_;
public Array35<short> snd_;




// DirectMusicの変数
public IDirectMusicLoader8? lpDML = null;
public IDirectMusicPerformance8? lpDMP = null;




// フォント
public HFONT		gameFont_1,gameFont_2;




// ゲーム用
public int	cc_count;
public int	key_cndtn;	// パッドの状態


//POINT	ptCursor;		// 純粋なマウスカーソルの位置


// デバグ
public int	dbg_menu;
public Array16<int> dbg;


// 通信対戦用
/*
GUID g_guidApp = { 0x2ae835d, 0x9179, 0x485f, { 0x83, 0x43, 0x90, 0x1d, 0x32, 0x7c, 0xe7, 0x94 } };
*/
public Guid g_guidApp = new( 0x11bc0eb, 0xbdb3, 0x11d6, 0xba, 0x95, 0x9c, 0xce, 0x36, 0x89, 0x70, 0x55 );



public IDirectPlay8ThreadPool? g_pThreadPool = null;		// DirectPlay threadpool object
public IDirectPlay8Peer? g_pDP = null;					// DirectPlay peer object

public HKEY								hDPlaySampleRegKey;		// レジストリ

public HWND                       g_hDlg                        = null;    // HWND of main dialog
public int								dlg_answer;


public uint                      g_dpnidLocalPlayer            = 0;       // DPNID of local player
public uint                      g_dpnidRivalPlayer            = 0;       // DPNID of local player
//DPNID                      g_dpnidHostPlayer             = 0;       // DPNID of host player
public uint                      g_dwNumberOfActivePlayers     = 0;       // Number of players currently in game
public Array256<byte> g_strAppName = TEXT<Array256<byte>>("NSPW NET");




public Array260<byte> g_strLocalPlayerName;          // Local player name
public Array260<byte> g_strRivalPlayerName =TEXT<Array260<byte>>(" - - - ");          // Rival player name
//TCHAR                      g_strSessionName[MAX_PATH];              // Session name
public Array260<byte> g_strPreferredProvider;        // Provider string
public Array260<byte> g_strRemoteHostname;           // TCP/IP remote host


public int                       g_bHostPlayer                 = FALSE;   // TRUE if local player is host
public Guid? g_pCurSPGuid = null;    // Currently selected guid
public uint                  g_hConnectAsyncOp             = 0;    // Async handle for connecting to host

public DPN_BUFFER_DESC bufferDesc;
public uint hAsync;

// チャット
public HWND                       hwndChatDlg                        = null;    // HWND of chat dialog


/////////////////////

// iNSPWからもってきたやつ

public Array25<SPRT> sprt;

//UINT			timerid;
//BOOL			post_pending;
public uint			missed_pending,a_paint_speed;

//int FrameRate;
public int	FrameCount;
//int FrameCount0;
//DWORD FrameTime;
//DWORD FrameTime0;

//unsigned char		*dst_vram;							//書き込むＶＲＡＭのアドレス
//DDSURFACEDESC2		dst_ddsd;

//unsigned char		*src_vram;							//読み込むＶＲＡＭのアドレス
//DDSURFACEDESC		src_ddsd;

public int	scrn_mode;

public int	video_memory;


public int		anti_air,reveal;


public double			cmbt_x,cmbt_y;
public double			scrn_moving_spd;
public Array256<UNIT> unit;
public short			max_unit;
public short	the_slct_unit,old_the_slct_unit,slct_unit_no; public Array2<Array256<short>> slct_unit;
public POINT			crsr_pt;

public Array10<int> unit_info;
public Array512<FIRE> fire;
public int				max_fire;
//BOOL			paint_effect_on;
public Array1024<EFFECT> effect;



public short			cls_flg;


public Array256<Array256<ushort>> cmbt_map;					// マップ
public Array4096<KUMO> kumo;								// 雲


public short			lf_btn,ri_btn;
public short			cmbt_menu_kind,cmbt_menu_slctd;

public Array64<double> wrk_pp_x; public Array64<double> wrk_pp_y;		//（ワーク）移動目的地の地図上の位置

public Array3<NEW_PP> new_pp;
public Array3<NEW_SLCT> new_slct;
public Array3<NEW_MENU> new_menu;

public int	rest_time,game_end; public Array4<int> decision_point;
public short			your_side;
public short			game_speed;

public short			mode,demo_time,sinario;


public byte			map_edit,put_trgt,put_kind,put_kind_sub;
public Array3<byte> rein;


public Array260<byte> user_sinario_fn;			// ユーザーシナリオのファイルネーム

// 通信対戦用
public int			cnct_game;		
//BOOL			cnct_now;
public byte			you_are_host;
public byte			you_were_host;

public int			you_can_order;
public int			you_ordered;

public Array3<NEW_PP> bf_new_pp;
public Array3<NEW_SLCT> bf_new_slct;
public Array3<NEW_MENU> bf_new_menu;

public Array3<byte> bf_game_system_menu;
public Array3<byte> game_system_menu;

public Array3<Array90<short>> bf_slct_unit;
public int			go_next_1,go_next_2;
public int			join_game_start;
public short			rival_mode;
public Array4096<int> my_rnd_sheet;
public short			my_rnd_pt;
public short			cnct_game_rnd_sheed;

public int			host_side;
public byte			decision_sw;
public byte			arrival_cont;

public int				rnd_count;
public Array2<byte> bf_cc_count; public Array2<byte> bf_rnd_count; public Array2<byte> bf_unit_chk;
public int			ccc_out,rnd_out,unit_out;
public Array2<byte> ccc_wait;
public short			cnct_loop_ct,cnct_loop;
public short			cnct_loop_pt1, cnct_loop_pt2;

public short	spry_pt,spry_no_cont,spry_trgt; public Array2<short> spry_rate; public Array2<short> first_spry_pt;
public short			rvrs_time,rvrs_rule;
public short			map_now;
public Array2<short> bf_arrived_unit;

public short			auto_save_time;

public uint	last_tick,last_tick2;
public uint	tick_now,tick_diff;


// 通信対戦デバグ用
public byte			first_r_error;

// チャット用
public int			input_chat_now=0;

//char			my_chat[128];
//char			friend_chat[128];
public Array260<byte> my_chat;
public Array260<byte> friend_chat;

public byte			my_chat_dsp_time;
public byte			friend_chat_dsp_time;

//char			my_string[128];
//BYTE			my_string_crsr;
//BYTE			my_string_rpd;

//
public Array16<byte> rival_ver;

/////////////////////









/*-------------------------------------------
	アプリがアクティブの時のアイドリング
	ゲームアプリのメインループ
--------------------------------------------*/
public void	updateFrame()
	{
	DDBLTFX ddbltfx;
	short		chara_loop;
	int	i,m;
	int	no;


	tick_now = timeGetTime();

	tick_diff = tick_now - last_tick2;


	if( map_edit!=0 )
		chara_loop=1;
	else if( mode==CMBT )
		{
		if( (key_cndtn&SPACE)!=0 )
			chara_loop=20;
		else
			chara_loop=game_speed;
		}
	else
		chara_loop=1;



	if( chara_loop==1 && tick_diff<=45+5 )
		{
		return;
		}

	last_tick2 = timeGetTime();

	get_input();		// マウス、キーボード状況取得





	// 塗りつぶし
	ZeroMemory(&ddbltfx,(nuint)(sizeof(DDBLTFX)));
	ddbltfx.dwSize = (uint)(sizeof( DDBLTFX ));

	switch( mode )
		{
		case DEMO:
			// 塗りつぶし
			ddbltfx.dwFillColor = 0x0000;
			if( DDERR_SURFACELOST == IDirectDrawSurface_Blt( lpDDSBack,null,null,null,DDBLT_COLORFILL | DDBLT_WAIT,&ddbltfx ))
				{
				restoreAll();
				return;
				}

			demo_func();
			break;

		case CNCT_GAME_SETTING:
		case CNCT_CNFG_SETTING:
			// 塗りつぶし
			ddbltfx.dwFillColor = 0x0300;
			if( DDERR_SURFACELOST == IDirectDrawSurface_Blt( lpDDSBack,null,null,null,DDBLT_COLORFILL | DDBLT_WAIT,&ddbltfx ))
				{
				restoreAll();
				return;
				}


			cnct_game_setting();
			break;

		case CMBT:
			// 塗りつぶし
			ddbltfx.dwFillColor = 0x0016;
			if( DDERR_SURFACELOST == IDirectDrawSurface_Blt( lpDDSBack,null,null,null,DDBLT_COLORFILL | DDBLT_WAIT,&ddbltfx ))
				{
				restoreAll();
				return;
				}



			for( i=chara_loop; i>=1; i-- )
				{
				chara_cont();
				if( map_edit==0 )
					cnct_decision();
				}

			unit_info_cont();
			draw_cmbt_area();
			draw_map();

			if( map_edit==0 )
				{
				cnct_decision();
				cnct_game_input_cont();
				}


			if(map_edit!=0 )
				{
				edit_now();
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
//		SelectObject(hdc, AppFont);



#if DBG_MODE

		no=the_slct_unit;

		len = wsprintf(ach, "dbg[0]=%d [1]=%d [2]=%d [3]=%d [4]=%d  :%d",dbg[0],dbg[1],dbg[2],dbg[3],dbg[4] ,the_slct_unit /*g_dwNumberOfActivePlayers*/ /*cc_count*/ );
		TextOut(hdc, 0, 0, ach, len);

		len = wsprintf(ach, "unit_out=%d ccc_out=%d rnd_out=%d rnd_count=%d CCC=%d FrameC=%d Sinario=%d host_side=%d rival_mode=%d" ,unit_out ,ccc_out,rnd_out,rnd_count ,cc_count, FrameCount, sinario, host_side, rival_mode);
		TextOut(hdc, 0, 20, ach, len);

		len = wsprintf(ach, "Dbg[5]=%d  bf_cc_count[0]=%d [1]=%d   bf_rnd_count[0]=%d [1]=%d  ccc_wait[0]=%d  [1]=%d " ,dbg[5], bf_cc_count[0], bf_cc_count[1], bf_rnd_count[0], bf_rnd_count[1], ccc_wait[0], ccc_wait[1] );
		TextOut(hdc, 0, 40, ach, len);



		switch( dbg_menu )
			{
			case 0:
//				len = wsprintf(ach, "%s", user_sinario_fn );
//				len = wsprintf(ach, "go_next_1=%d mode=%d rival_mode=%d you_are_host=%d ", go_next_1, mode, rival_mode, you_are_host );
//		len = wsprintf(ach, "arrival_cont %d", arrival_cont );
//		len = wsprintf(ach, "num %d", host_side );
				len = wsprintf(ach, "%d  :%d  :%d  :%d  :%d", unit[the_slct_unit].info[1], unit[the_slct_unit].info[4], unit[the_slct_unit].info[7], unit[the_slct_unit].info[8], unit[81].info[0] );
				break;
			case 1:
				len = wsprintf(ach, "put_trgt %d put_kind %d cmbt_x %d cmbt_y %d crsr_pt.x %d crsr_pt.y %d ",put_trgt, put_kind, (int)cmbt_x, (int)cmbt_y, (int)crsr_pt.x, (int)crsr_pt.y  );
				break;
			case 2:
				len = wsprintf(ach, "cnct_game=%d you_are_host=%d you_can_order=%d new_pp[1].used=%d",cnct_game,you_are_host,you_can_order,new_pp[1].used);
				break;
			case 3:
				len = wsprintf(ach, "you_ordered=%d you_can_order=%d new_pp[1].used=%d",you_ordered,you_can_order,new_pp[1].used);
				break;
			case 4:
				len = wsprintf(ach, "game_speed=%d",game_speed);
				break;
			case 5:
				len = wsprintf(ach, "CR_X=%02d CR_Y=%02d Ri_btn=%d Lf_btn=%d ", crsr_pt.x,crsr_pt.y, ri_btn, lf_btn );
				break;

			case 6:
				len = wsprintf(ach, "go_next_1=%d go_next_2=%d", go_next_1, go_next_2 );
				break;

			case 7:
				len = wsprintf(ach, "hp[0]=%d arm[0]=%d", unit[no].hp[0], unit[no].arm[0] );
				break;

			case 8:
				len = wsprintf(ach, "spry_rate[0]=%d [1]=%d  first_sply_pt[0]=%d [1]=%d  rvrs_time=%d _rule=%d", spry_rate[0], spry_rate[1], first_spry_pt[0], first_spry_pt[1], rvrs_time, rvrs_rule );
				break;

			default:
				dbg_menu=0;
				break;
			}

		TextOut(hdc, 0, 60, ach, len);
#endif



#if !LNGG_VER
			if(unit_out!=0)
				{
				len = wsprintf(ach, "ユニットデータ同期異常、ゲームを中断されたし。");
				TextOut(hdc, 0, 100, ach, len);
				}
			if(ccc_out!=0)
				{
				len = wsprintf(ach, "プログラム同期異常、ゲームを中断されたし。");
				TextOut(hdc, 0, 120, ach, len);
				}
			if(rnd_out!=0)
				{
				len = wsprintf(ach, "ランダム同期異常、ゲームを中断されたし。");
				TextOut(hdc, 0, 140, ach, len);
				}
			if( g_dwNumberOfActivePlayers!=2 /*&& ( mode==CMBT || mode==CNCT_GAME_SETTING || mode==CNCT_CNFG_SETTING )*/ )
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
			if( cnct_now==0 /*&& ( mode==CMBT || mode==CNCT_GAME_SETTING || mode==CNCT_CNFG_SETTING )*/ )
				{
				len = wsprintf(ach, "No connection object.");
				TextOut(hdc, 0, 160, ach, len);
				}
#endif






		SetBkMode(hdc, TRANSPARENT);
		SelectObject(hdc, gameFont_1);

		SetTextColor(hdc, RGB(255, 255, 0));



		if(my_chat_dsp_time!=0)
			{
			len = wsprintf(ach, my_chat );
			TextOut(hdc, 10, 200+170, ach, len);
			my_chat_dsp_time--;
			if(my_chat_dsp_time==0)
				{
				for(m=0;m<128;m++)
					{
					my_chat[m]=0;
					}
				}
			}


		if(friend_chat_dsp_time!=0)
			{
			len = wsprintf(ach, friend_chat );
			TextOut(hdc, 10, 240+170, ach, len);
			friend_chat_dsp_time--;
			if(friend_chat_dsp_time==0)
				{
				for(m=0;m<128;m++)
					{
					friend_chat[m]=0;
					}
				}
			}


		lpDDSBack.ReleaseDC(hdc);
		}










	// マウス入力の後処理
	if(ri_btn==1)	ri_btn=2;
	if(ri_btn==3)	ri_btn=0;

	if(lf_btn==1)	lf_btn=2;
	if(lf_btn==3)	lf_btn=0;

	FrameCount++;


	// プライマリサーフェスにフリップ
//	if( DDERR_SURFACELOST == IDirectDrawSurface_Flip(lpDDSPrimary,NULL, DDFLIP_WAIT ))
//	if( DDERR_SURFACELOST == IDirectDrawSurface_Blt(lpDDSPrimary,NULL,lpDDSBack,NULL,DDBLT_WAIT,NULL ))
//		restoreAll();

//	lpDDSPrimary->SetClipper(lpDDclip);
//	lpDDSPrimary->Blt(NULL,lpDDSBack,NULL,DDBLT_WAIT,NULL);

	IDirectDrawSurface_SetClipper(lpDDSPrimary,lpDDclip);
	if( DDERR_SURFACELOST == IDirectDrawSurface_Blt(lpDDSPrimary,null,lpDDSBack,null,DDBLT_WAIT,null))
		restoreAll();



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
		dbg[i]=0;


	fullscreen=1;
	hInstApp=hInst;

	InitCommonControls();

	// Read persistent state information from registry
	RegCreateKeyEx( HKEY_CURRENT_USER, "Software\\Microsoft\\DirectX DirectPlay Samples", 0, null,
                    REG_OPTION_NON_VOLATILE, KEY_READ | KEY_WRITE, null, 
                    ref hDPlaySampleRegKey, null );

    DXUtil_ReadStringRegKeyCch( hDPlaySampleRegKey, TEXT("Player Name"), 
                             g_strLocalPlayerName, MAX_PATH, TEXT("TestPlayer") );
//    DXUtil_ReadStringRegKeyCch( hDPlaySampleRegKey, TEXT("Session Name"), g_strSessionName, MAX_PATH, TEXT("TestGame") );
    DXUtil_ReadStringRegKeyCch( hDPlaySampleRegKey, TEXT("Preferred Provider"), 
                             g_strPreferredProvider, MAX_PATH, 
                             TEXT("DirectPlay8 TCP/IP Service Provider") );
    DXUtil_ReadStringRegKeyCch( hDPlaySampleRegKey, TEXT("Remote Hostname"), 
                             g_strRemoteHostname, MAX_PATH, 
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



#if false

	map_edit=1;
	cnct_game=1;
	g_bHostPlayer=1;
	you_are_host=g_bHostPlayer;
	g_dwNumberOfActivePlayers=2;

#elif false


	g_dwNumberOfActivePlayers=2;
	g_bHostPlayer=1;
	you_are_host=g_bHostPlayer;


#else

//CreateDialog(hInstApp,MAKEINTRESOURCE(IDD_CHAT_DIALOG),NULL/*hwndApp/*hWnd*/,ChatDlgProc);
//g_hDlg = CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_MAIN_GAME), NULL/*hwndApp*/, GreetingDlgProc);
//CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_CHAT_DIALOG), NULL/*hwndApp*/, GreetingDlgProc);


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

	if( g_dwNumberOfActivePlayers<2 )
		{
		// 接続キャンセル
		EndDialog( g_hDlg, 0 );
		EndApp( );
		return (int)msg.wParam;
		}

	you_are_host=(byte)g_bHostPlayer;

#endif


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
	if (fullscreen!=0)
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

	ShowWindow(hwndApp,SW_SHOWNORMAL/*nCmdShow*/);
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
	if (fullscreen!=0)
	    ddrval = lpDD.SetCooperativeLevel(hwndApp, DDSCL_EXCLUSIVE | DDSCL_FULLSCREEN );
	else
	    ddrval = lpDD.SetCooperativeLevel(hwndApp, DDSCL_NORMAL );
	if (ddrval != DD_OK) 
		{
		MessageBox(null,"残念！","って言うじゃなーい。",MB_OK | MB_ICONSTOP);
		return FALSE;
		}



	// ディスプレイモードを設定
	if(fullscreen!=0)
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
	if(fullscreen!=0)
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



	if(!InitDInput())
		return FALSE;
#if SND_SW
	if(InitDSound()==0)
		return FALSE;
	if(InitDMusic()==0)
		return FALSE;
#endif





	// ゲーム変数初期化
	init_apl_reg();

	appActive=1;

	set_sprt_data();

	demo_time=0;




#if true

	mode=DEMO;


#elif false

	go_cnct_game_setting();

#else
	// 通信対戦用の初期化
	cnct_game=0;
	you_can_order=1;
	go_next_1=0;
	go_next_2=0;
	bf_cc_count[0]=bf_cc_count[1]=0;
	bf_rnd_count[0]=bf_rnd_count[1]=0;

if( CONN_DBG )
{
cnct_game=1;
you_are_host=1;
}

sinario=999;


	mode=CNCT_GAME_SETTING;
	get_sinario_data();


	mode=CMBT;
	cnct_game_init();
#endif





	//メッセージループ
	while(TRUE!=0)
		{
/*
		if( hwndChatDlg==NULL )
			crnt_wnd=hwndApp;
		else
			crnt_wnd=hwndChatDlg;
*/

		if( /*hwndChatDlg==NULL &&*/ PeekMessage(ref msg,null,0,0,PM_NOREMOVE)!=0 )
			{
			if(GetMessage(ref msg,null,0,0)==0)
				break;

			TranslateMessage(ref msg);
			DispatchMessage(ref msg);
			}
/*
		else if( hwndChatDlg!=NULL && PeekMessage(&msg,hwndChatDlg,0,0,PM_NOREMOVE) )
			{
			GetMessage(&msg,hwndChatDlg,0,0);
			TranslateMessage(&msg);
			DispatchMessage(&msg);
			}
*/
		else 
			{
			if( ( appActive!=0 || hwndChatDlg!=null ) && g_hDlg==null )
				{
				updateFrame();
/*
if( hwndChatDlg!=NULL )
	{
	if( PeekMessage(&msg,hwndChatDlg,0,0,PM_REMOVE) )
		{
		}
	}
*/
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
