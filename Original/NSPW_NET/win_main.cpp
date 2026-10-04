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


#include "all_head.h"
#include	"all_typedef.h"
#include	"all_forward.h"


// ウィンドウアプリケーション変数
HINSTANCE	hInstApp;
HWND	hwndApp;
HCURSOR		my_cursor;
D3DPRESENT_PARAMETERS d3dpp;
int	appActive;
int	fullscreen;


//	DirectXオブジェクト

LPDIRECTDRAW7			lpDD=NULL;
LPDIRECTDRAWSURFACE7 lpDDSPrimary=NULL;
LPDIRECTDRAWSURFACE7 lpDDSBack=NULL;

LPDIRECTDRAWCLIPPER	lpDDclip=NULL;

LPDIRECTDRAWSURFACE7 lpDDS_OS=NULL;



// DirectInputの変数
LPDIRECTINPUT8			pDInput = NULL;					// DirectInput
LPDIRECTINPUTDEVICE8	pDIDevice = NULL;			// DirectInputデバイス
LPDIRECTINPUTDEVICE8	pDIDeviceMouse = NULL;			// DirectInputデバイス
DIDEVCAPS				diDevCaps;				// ジョイスティックの能力


// DirectSoundの変数
LPDIRECTSOUND8			lpDS = NULL;
LPDIRECTSOUNDBUFFER	lpDSP = NULL;

LPDIRECTSOUNDBUFFER lpDSB_[NUM_SOUND_EFFECTS][SND_DUP];
short		snd_[NUM_SOUND_EFFECTS];




// DirectMusicの変数
IDirectMusicLoader8 *lpDML = NULL;
IDirectMusicPerformance8 *lpDMP = NULL;




// フォント
HFONT		gameFont_1,gameFont_2;




// ゲーム用
int	cc_count;
int	key_cndtn;	// パッドの状態


//POINT	ptCursor;		// 純粋なマウスカーソルの位置


// デバグ
int	dbg_menu;
int	dbg[16];


// 通信対戦用
/*
GUID g_guidApp = { 0x2ae835d, 0x9179, 0x485f, { 0x83, 0x43, 0x90, 0x1d, 0x32, 0x7c, 0xe7, 0x94 } };
*/
GUID g_guidApp = { 0x11bc0eb, 0xbdb3, 0x11d6, { 0xba, 0x95, 0x9c, 0xce, 0x36, 0x89, 0x70, 0x55 } };



IDirectPlay8ThreadPool*		g_pThreadPool = NULL;		// DirectPlay threadpool object
IDirectPlay8Peer*				g_pDP = NULL;					// DirectPlay peer object

HKEY								hDPlaySampleRegKey;		// レジストリ

HWND                       g_hDlg                        = NULL;    // HWND of main dialog
int								dlg_answer;


DPNID                      g_dpnidLocalPlayer            = 0;       // DPNID of local player
DPNID                      g_dpnidRivalPlayer            = 0;       // DPNID of local player
//DPNID                      g_dpnidHostPlayer             = 0;       // DPNID of host player
DWORD                      g_dwNumberOfActivePlayers     = 0;       // Number of players currently in game
TCHAR                      g_strAppName[256]             = TEXT("NSPW NET");




TCHAR                      g_strLocalPlayerName[MAX_PATH];          // Local player name
TCHAR                      g_strRivalPlayerName[MAX_PATH]=TEXT(" - - - ");          // Rival player name
//TCHAR                      g_strSessionName[MAX_PATH];              // Session name
TCHAR                      g_strPreferredProvider[MAX_PATH];        // Provider string
TCHAR                      g_strRemoteHostname[MAX_PATH];           // TCP/IP remote host


BOOL                       g_bHostPlayer                 = FALSE;   // TRUE if local player is host
GUID*                      g_pCurSPGuid                  = NULL;    // Currently selected guid
DPNHANDLE                  g_hConnectAsyncOp             = NULL;    // Async handle for connecting to host

DPN_BUFFER_DESC bufferDesc;
DPNHANDLE hAsync;

// チャット
HWND                       hwndChatDlg                        = NULL;    // HWND of chat dialog


/////////////////////

// iNSPWからもってきたやつ

SPRT					sprt[MAX_SPRT];

//UINT			timerid;
//BOOL			post_pending;
UINT			missed_pending,a_paint_speed;

//int FrameRate;
int	FrameCount;
//int FrameCount0;
//DWORD FrameTime;
//DWORD FrameTime0;

//unsigned char		*dst_vram;							//書き込むＶＲＡＭのアドレス
//DDSURFACEDESC2		dst_ddsd;

//unsigned char		*src_vram;							//読み込むＶＲＡＭのアドレス
//DDSURFACEDESC		src_ddsd;

BOOL	scrn_mode;

BOOL	video_memory;


int		anti_air,reveal;


double			cmbt_x,cmbt_y;
double			scrn_moving_spd;
UNIT			unit[256];
short			max_unit;
short			the_slct_unit, old_the_slct_unit, slct_unit[2][256],slct_unit_no;
POINT			crsr_pt;

int				unit_info[10];
FIRE			fire[FIRE_MAX];
int				max_fire;
//BOOL			paint_effect_on;
EFFECT			effect[EFFECT_MAX];



short			cls_flg;


unsigned short		cmbt_map[256][256];					// マップ
KUMO				kumo[KUMO_MAX];								// 雲


short			lf_btn,ri_btn;
short			cmbt_menu_kind,cmbt_menu_slctd;

double			wrk_pp_x[64],wrk_pp_y[64];		//（ワーク）移動目的地の地図上の位置

NEW_PP			new_pp[3];
NEW_SLCT		new_slct[3];
NEW_MENU		new_menu[3];

int				rest_time,game_end,decision_point[4];
short			your_side;
short			game_speed;

short			mode,demo_time,sinario;


BYTE			map_edit,put_trgt,put_kind,put_kind_sub;
BYTE			rein[3];


TCHAR			user_sinario_fn[MAX_PATH];			// ユーザーシナリオのファイルネーム

// 通信対戦用
BOOL			cnct_game;		
//BOOL			cnct_now;
BYTE			you_are_host;
BYTE			you_were_host;

BOOL			you_can_order;
BOOL			you_ordered;

NEW_PP			bf_new_pp[3];
NEW_SLCT		bf_new_slct[3];
NEW_MENU		bf_new_menu[3];

BYTE			bf_game_system_menu[3];
BYTE			game_system_menu[3];

short			bf_slct_unit[3][USA_PLANE_END/2];
BOOL			go_next_1,go_next_2;
BOOL			join_game_start;
short			rival_mode;
int				my_rnd_sheet[4096];
short			my_rnd_pt;
short			cnct_game_rnd_sheed;

BOOL			host_side;
BYTE			decision_sw;
BYTE			arrival_cont;

int				rnd_count;
BYTE			bf_cc_count[2],bf_rnd_count[2],bf_unit_chk[2];
BOOL			ccc_out,rnd_out,unit_out;
BYTE			ccc_wait[2];
short			cnct_loop_ct,cnct_loop;
short			cnct_loop_pt1, cnct_loop_pt2;

short			spry_pt,spry_no_cont,spry_trgt,spry_rate[2],first_spry_pt[2];
short			rvrs_time,rvrs_rule;
short			map_now;
short			bf_arrived_unit[2];

short			auto_save_time;

DWORD	last_tick,last_tick2;
DWORD	tick_now,tick_diff;


// 通信対戦デバグ用
BYTE			first_r_error;

// チャット用
BOOL			input_chat_now=0;

//char			my_chat[128];
//char			friend_chat[128];
TCHAR			my_chat[MAX_PATH];
TCHAR			friend_chat[MAX_PATH];

BYTE			my_chat_dsp_time;
BYTE			friend_chat_dsp_time;

//char			my_string[128];
//BYTE			my_string_crsr;
//BYTE			my_string_rpd;

//
TCHAR			rival_ver[16];

/////////////////////









/*-------------------------------------------
	アプリがアクティブの時のアイドリング
	ゲームアプリのメインループ
--------------------------------------------*/
void	updateFrame( void )
	{
	DDBLTFX ddbltfx;
	short		chara_loop;
	int	i,m;
	int	no;


	tick_now = timeGetTime();

	tick_diff = tick_now - last_tick2;


	if( map_edit )
		chara_loop=1;
	else if( mode==CMBT )
		{
		if( key_cndtn&SPACE )
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
	ZeroMemory(&ddbltfx,sizeof(ddbltfx));
	ddbltfx.dwSize = sizeof( ddbltfx );

	switch( mode )
		{
		case DEMO:
			// 塗りつぶし
			ddbltfx.dwFillColor = 0x0000;
			if( DDERR_SURFACELOST == IDirectDrawSurface_Blt( lpDDSBack,NULL,NULL,NULL,DDBLT_COLORFILL | DDBLT_WAIT,&ddbltfx ))
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
			if( DDERR_SURFACELOST == IDirectDrawSurface_Blt( lpDDSBack,NULL,NULL,NULL,DDBLT_COLORFILL | DDBLT_WAIT,&ddbltfx ))
				{
				restoreAll();
				return;
				}


			cnct_game_setting();
			break;

		case CMBT:
			// 塗りつぶし
			ddbltfx.dwFillColor = 0x0016;
			if( DDERR_SURFACELOST == IDirectDrawSurface_Blt( lpDDSBack,NULL,NULL,NULL,DDBLT_COLORFILL | DDBLT_WAIT,&ddbltfx ))
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


			if(map_edit )
				{
				edit_now();
				}

			break;
		}



	HDC hdc;
	char ach[128];
	int len;

	if (DD_OK==lpDDSBack->GetDC(&hdc)) 
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



#if LNGG_VER==0
			if(unit_out)
				{
				len = wsprintf(ach, "ユニットデータ同期異常、ゲームを中断されたし。");
				TextOut(hdc, 0, 100, ach, len);
				}
			if(ccc_out)
				{
				len = wsprintf(ach, "プログラム同期異常、ゲームを中断されたし。");
				TextOut(hdc, 0, 120, ach, len);
				}
			if(rnd_out)
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



		if(my_chat_dsp_time)
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


		if(friend_chat_dsp_time)
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


		lpDDSBack->ReleaseDC(hdc);
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
	if( DDERR_SURFACELOST == IDirectDrawSurface_Blt(lpDDSPrimary,NULL,lpDDSBack,NULL,DDBLT_WAIT,NULL))
		restoreAll();



	}
























/*--------------------------------------------
	メイン
---------------------------------------------*/
int WINAPI WinMain(HINSTANCE hInst,HINSTANCE hPrevInst,LPSTR lpCmdLine,int nCmdShow)
	{
	MSG msg;
	WNDCLASS wc;




	int	i;

	for(i=0; i<16; i++)
		dbg[i]=0;


	fullscreen=1;
	hInstApp=hInst;

	InitCommonControls();

	// Read persistent state information from registry
	RegCreateKeyEx( HKEY_CURRENT_USER, "Software\\Microsoft\\DirectX DirectPlay Samples", 0, NULL,
                    REG_OPTION_NON_VOLATILE, KEY_READ | KEY_WRITE, NULL, 
                    &hDPlaySampleRegKey, NULL );

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
	CoInitializeEx( NULL, COINIT_MULTITHREADED );




	// Create IDirectPlay8ThreadPool
	// ＤＰをバックスレッドでなく通常のルーティン処理とするためにこれを設定する。
	if( FAILED( CoCreateInstance( CLSID_DirectPlay8ThreadPool, NULL, CLSCTX_INPROC_SERVER,IID_IDirectPlay8ThreadPool, (LPVOID*) &g_pThreadPool ) ))
        return false;

	// Init IDirectPlay8ThreadPool
	g_pThreadPool->Initialize( NULL, DirectPlayMessageHandler, DPNINITIALIZE_DISABLEPARAMVAL );

	// Put DirectPlay in "DoWork" mode
   g_pThreadPool->SetThreadCount( (DWORD) -1, 0, 0 );

	// Create IDirectPlay8Peer
	if( FAILED( CoCreateInstance( CLSID_DirectPlay8Peer, NULL, CLSCTX_INPROC_SERVER, IID_IDirectPlay8Peer, (LPVOID*) &g_pDP ) ))
		return false;

	// Init IDirectPlay8Peer
	g_pDP->Initialize( NULL, DirectPlayMessageHandler, DPNINITIALIZE_DISABLEPARAMVAL );



#if 0

	map_edit=1;
	cnct_game=1;
	g_bHostPlayer=1;
	you_are_host=g_bHostPlayer;
	g_dwNumberOfActivePlayers=2;

#elif 0


	g_dwNumberOfActivePlayers=2;
	g_bHostPlayer=1;
	you_are_host=g_bHostPlayer;


#else

//CreateDialog(hInstApp,MAKEINTRESOURCE(IDD_CHAT_DIALOG),NULL/*hwndApp/*hWnd*/,ChatDlgProc);
//g_hDlg = CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_MAIN_GAME), NULL/*hwndApp*/, GreetingDlgProc);
//CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_CHAT_DIALOG), NULL/*hwndApp*/, GreetingDlgProc);


	// Create the initial dialog.
	// 接続ダイアログ 　オーナーウィンドウが無いため、モーダレスになります。
	g_hDlg = CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_ADDRESS_OVERRIDE), NULL, OverrideDlgProc);


	BOOL    wait_for_connect = TRUE;

	// ホストも、ゲストもここで接続が完了するまでループ
	// 接続確認ダイアログ
	while (wait_for_connect)
		{
		// Retrieve any windows messages.
		while (PeekMessage(&msg, NULL, 0, 0, PM_REMOVE))
			{
			if (msg.message == WM_QUIT)
				{
				// Quit the application.
				wait_for_connect = FALSE;
				break;
				}

			if ( !IsDialogMessage(g_hDlg, &msg) )
				{
				TranslateMessage(&msg);
				DispatchMessage(&msg);
				}
	     }
		// Let DirectPlay process network events and call our message handler.
		g_pThreadPool->DoWork(DOWORK_TIMESLICE, 0);
		Sleep(1);
		}

	if( g_dwNumberOfActivePlayers<2 )
		{
		// 接続キャンセル
		EndDialog( g_hDlg, 0 );
		EndApp( );
		return msg.wParam;
		}

	you_are_host=g_bHostPlayer;

#endif


	g_hDlg=0;


	// メインウインドウを作成する
	ZeroMemory(&wc,sizeof(WNDCLASS));
	wc.hbrBackground = (HBRUSH)GetStockObject(WHITE_BRUSH);
	wc.hInstance = hInst;
	wc.lpfnWndProc = (WNDPROC)MainWndProc;
	wc.lpszClassName = CLASS_NAME;
	wc.hCursor = LoadCursor(NULL, IDC_ARROW);


	RegisterClass(&wc);

	int width,height;
	// ウインドウの大きさを計算
	if (fullscreen)
		{
		// フルスクリーン時はそのままで OK
		width = WIDTH;
		height = HEIGHT;
		hwndApp = CreateWindow(CLASS_NAME,CAPTION, WS_POPUP ,CW_USEDEFAULT,CW_USEDEFAULT,width,height,NULL,NULL,hInst,NULL);
		}
	else
		{
		// ウインドウ時はウインドウの外枠のサイズを考慮する
		width = WIDTH + GetSystemMetrics(SM_CXDLGFRAME) * 2;
		height = HEIGHT + GetSystemMetrics(SM_CYDLGFRAME) * 2 + GetSystemMetrics(SM_CYCAPTION);
		hwndApp = CreateWindow(CLASS_NAME,CAPTION, WS_OVERLAPPED | WS_SYSMENU | WS_MINIMIZEBOX /*| WS_VISIBLE*/ ,CW_USEDEFAULT,CW_USEDEFAULT,width,height,NULL,NULL,hInst,NULL);
		}

	ShowWindow(hwndApp,SW_SHOWNORMAL/*nCmdShow*/);
	UpdateWindow(hwndApp);



	ImmAssociateContext(hwndApp,NULL);			// 日本語入力機能を通常ゲーム時抑制する。



    HRESULT		ddrval;

	// ダイレクトドロウ
    ddrval = DirectDrawCreateEx(NULL,(LPVOID*)&lpDD,IID_IDirectDraw7,NULL);
    if (ddrval != DD_OK) 
		{
		MessageBox(NULL,"DirectDrawの作成に失敗","Base",MB_OK | MB_ICONSTOP);
		return FALSE;
		}



    // 協調レベルを設定
	if (fullscreen)
	    ddrval = lpDD->SetCooperativeLevel(hwndApp, DDSCL_EXCLUSIVE | DDSCL_FULLSCREEN );
	else
	    ddrval = lpDD->SetCooperativeLevel(hwndApp, DDSCL_NORMAL );
	if (ddrval != DD_OK) 
		{
		MessageBox(NULL,"残念！","って言うじゃなーい。",MB_OK | MB_ICONSTOP);
		return FALSE;
		}



	// ディスプレイモードを設定
	if(fullscreen)
		{
		ddrval = lpDD->SetDisplayMode( width, height, 16, 0, 0);
		if (ddrval !=DD_OK) 
			{
			MessageBox(NULL,"SetDisplayMode","残念！",MB_OK | MB_ICONSTOP);
			return FALSE;
			}
		}




	// バックバッファをひとつ持つプライマリサーフェスを作成
	DDSURFACEDESC2 ddsd;
	DDSCAPS2 ddscaps;

	ZeroMemory(&ddsd,sizeof(ddsd));
	ddsd.dwSize = sizeof( ddsd );
	ddsd.dwFlags = DDSD_CAPS | DDSD_BACKBUFFERCOUNT;
	ddsd.ddsCaps.dwCaps = DDSCAPS_PRIMARYSURFACE |
						  DDSCAPS_FLIP | 
						  DDSCAPS_COMPLEX;
	ddsd.dwBackBufferCount = 1;
	ddrval = lpDD->CreateSurface( &ddsd, &lpDDSPrimary, NULL );
	if (ddrval!=DD_OK) 
		{
		MessageBox(NULL,"CreateSurface","残念！",MB_OK | MB_ICONSTOP);
		return FALSE;
		}

	// バックバッファインターフェイスを取得
	ZeroMemory(&ddscaps, sizeof(ddscaps));
	ddscaps.dwCaps=DDSCAPS_BACKBUFFER;
	ddrval=lpDDSPrimary->GetAttachedSurface(&ddscaps,&lpDDSBack);
	if (ddrval!=DD_OK) 
		{
		MessageBox(NULL,"GetAttachedSurface","残念！",MB_OK | MB_ICONSTOP);
		return FALSE;
		}





	// 最初のイメージをロードし、表示
	lpDDS_OS=bitmap_surface("t3.bmp");
	if (!lpDDS_OS)
		{
		MessageBox(NULL,"bitmap_surface","残念！",MB_OK | MB_ICONSTOP);
		return FALSE;
		}



	// クリッパー
	if(fullscreen)
		{
		lpDD->CreateClipper(0,&lpDDclip,NULL);
		lpDDclip->SetHWnd(0,hwndApp);
		lpDD->FlipToGDISurface();
		}
	else
		{
		lpDD->CreateClipper(0,&lpDDclip,NULL);
		lpDDclip->SetHWnd(0,hwndApp);
		lpDDSPrimary->SetClipper(lpDDclip);
		}




	// ピクセルフォーマットを取得
	DDPIXELFORMAT ddpf;
	ddpf.dwSize=sizeof(ddpf);
	lpDDS_OS->GetPixelFormat(&ddpf);

	DWORD KeyColor;
	KeyColor = ddpf.dwRBitMask | ddpf.dwBBitMask;


	// カラーキーを設定
	DDCOLORKEY key;
	key.dwColorSpaceLowValue = KeyColor;
	key.dwColorSpaceHighValue = KeyColor;
	lpDDS_OS->SetColorKey(DDCKEY_SRCBLT, &key);



	if(!InitDInput())
		return FALSE;
#if SND_SW
	if(!InitDSound())
		return FALSE;
	if(!InitDMusic())
		return FALSE;
#endif





	// ゲーム変数初期化
	init_apl_reg();

	appActive=1;

	set_sprt_data();

	demo_time=0;




#if 1

	mode=DEMO;


#elif 0

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
	while(TRUE)
		{
/*
		if( hwndChatDlg==NULL )
			crnt_wnd=hwndApp;
		else
			crnt_wnd=hwndChatDlg;
*/

		if( /*hwndChatDlg==NULL &&*/ PeekMessage(&msg,NULL,0,0,PM_NOREMOVE) )
			{
			if(!GetMessage(&msg,NULL,0,0))
				break;

			TranslateMessage(&msg);
			DispatchMessage(&msg);
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
			if( ( appActive || hwndChatDlg!=NULL ) && g_hDlg==0 )
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

			if( g_pThreadPool && g_pDP )
				g_pThreadPool->DoWork(DOWORK_TIMESLICE, 0);
			}

		}

	return msg.wParam;

	}



