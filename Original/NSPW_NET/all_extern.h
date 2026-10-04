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



#include	"all_typedef.h"



// ウィンドウアプリケーション変数
extern	HINSTANCE	hInstApp;
extern	HWND	hwndApp;
extern	HCURSOR		my_cursor;
extern	D3DPRESENT_PARAMETERS d3dpp;
extern	int	appActive;
extern	int	fullscreen;


//	DirectXオブジェクト
extern	LPDIRECTDRAW7			lpDD;
extern	LPDIRECTDRAWSURFACE7 lpDDSPrimary;
extern	LPDIRECTDRAWSURFACE7 lpDDSBack;

extern	LPDIRECTDRAWCLIPPER	lpDDclip;

extern	LPDIRECTDRAWSURFACE7 lpDDS_OS;



// DirectInputの変数
extern	LPDIRECTINPUT8			pDInput;					// DirectInput
extern	LPDIRECTINPUTDEVICE8	pDIDevice;			// DirectInputデバイス
extern	LPDIRECTINPUTDEVICE8	pDIDeviceMouse;			// DirectInputデバイス
extern	DIDEVCAPS				diDevCaps;				// ジョイスティックの能力



// DirectSoundの変数
extern	LPDIRECTSOUND8			lpDS;
extern	LPDIRECTSOUNDBUFFER	lpDSP;

extern	LPDIRECTSOUNDBUFFER lpDSB_[NUM_SOUND_EFFECTS][SND_DUP];
extern	short		snd_[NUM_SOUND_EFFECTS];


// DirectMusicの変数
extern	IDirectMusicLoader8 *lpDML;
extern	IDirectMusicPerformance8 *lpDMP;




// フォント
extern	HFONT		gameFont_1,gameFont_2;



// ゲーム用
extern	int	cc_count;
extern	int	key_cndtn;			// パッドの状態


// デバグ
extern	int	dbg_menu;
extern	int	dbg[16];


// 通信対戦用
extern	GUID	g_guidApp;

extern	HKEY	hDPlaySampleRegKey;		// レジストリ

extern	HWND	g_hDlg;    // HWND of main dialog
extern	int	dlg_answer;

extern	DPNID	g_dpnidLocalPlayer;       // DPNID of local player
extern	DPNID	g_dpnidRivalPlayer;       // DPNID of local player
//extern	DPNID	g_dpnidHostPlayer;       // DPNID of host player
extern	DWORD	g_dwNumberOfActivePlayers;       // Number of players currently in game
extern	TCHAR	g_strAppName[256];



extern	IDirectPlay8ThreadPool*		g_pThreadPool;		// DirectPlay threadpool object
extern	IDirectPlay8Peer*				g_pDP;					// DirectPlay peer object

extern	TCHAR                      g_strLocalPlayerName[MAX_PATH];          // Local player name
extern	TCHAR                      g_strRivalPlayerName[MAX_PATH];          // Local player name
//extern	TCHAR                      g_strSessionName[MAX_PATH];              // Session name
extern	TCHAR                      g_strPreferredProvider[MAX_PATH];        // Provider string
extern	TCHAR                      g_strRemoteHostname[MAX_PATH];           // TCP/IP remote host


extern	BOOL                       g_bHostPlayer;   // TRUE if local player is host
extern	GUID*                      g_pCurSPGuid;    // Currently selected guid
extern	DPNHANDLE                  g_hConnectAsyncOp;    // Async handle for connecting to host

extern	DPN_BUFFER_DESC bufferDesc;
extern	DPNHANDLE hAsync;

extern	HWND                       hwndChatDlg;    // HWND of chat dialog

/////////////////////

// iNSPWからもってきたやつ

extern	SPRT					sprt[MAX_SPRT];

//UINT			timerid;
//BOOL			post_pending;
extern	UINT			missed_pending,a_paint_speed;

//int FrameRate;
extern	int	FrameCount;
//int FrameCount0;
//DWORD FrameTime;
//DWORD FrameTime0;

//extern	unsigned char		*dst_vram;							//書き込むＶＲＡＭのアドレス
//extern	DDSURFACEDESC2		dst_ddsd;
//unsigned char		*src_vram;							//読み込むＶＲＡＭのアドレス
//DDSURFACEDESC		src_ddsd;

extern	BOOL	scrn_mode;

extern	BOOL	video_memory;


extern	int		anti_air,reveal;


extern	double			cmbt_x,cmbt_y;
extern	double			scrn_moving_spd;
extern	UNIT			unit[256];
extern	short			max_unit;
extern	short			the_slct_unit, old_the_slct_unit, slct_unit[2][256],slct_unit_no;
extern	POINT			crsr_pt;

extern	int				unit_info[10];
extern	FIRE			fire[FIRE_MAX];
extern	int				max_fire;
//extern	BOOL			paint_effect_on;
extern	EFFECT			effect[EFFECT_MAX];



extern	short			cls_flg;


extern	unsigned short		cmbt_map[256][256];					// マップ
extern	KUMO				kumo[KUMO_MAX];								// 雲


extern	short			lf_btn,ri_btn;
extern	short			cmbt_menu_kind,cmbt_menu_slctd;

extern	double			wrk_pp_x[64],wrk_pp_y[64];		//（ワーク）移動目的地の地図上の位置

extern	NEW_PP			new_pp[3];
extern	NEW_SLCT		new_slct[3];
extern	NEW_MENU		new_menu[3];

extern	int				rest_time,game_end,decision_point[4];
extern	short			your_side;
extern	short			game_speed;

extern	short			mode,demo_time,sinario;


extern	BYTE			map_edit,put_trgt,put_kind,put_kind_sub;
extern	BYTE			rein[3];


extern	TCHAR			user_sinario_fn[MAX_PATH];			// ユーザーシナリオのファイルネーム


// 通信対戦用
extern	BOOL			cnct_game;		
//extern	BOOL			cnct_now;
extern	BYTE			you_are_host;
extern	BYTE			you_were_host;

extern	BOOL			you_can_order;
extern	BOOL			you_ordered;

extern	NEW_PP			bf_new_pp[3];
extern	NEW_SLCT		bf_new_slct[3];
extern	NEW_MENU		bf_new_menu[3];

extern	BYTE			bf_game_system_menu[3];
extern	BYTE			game_system_menu[3];

extern	short			bf_slct_unit[3][USA_PLANE_END/2/*50*/];
extern	BOOL			go_next_1,go_next_2;
extern	BOOL			join_game_start;
extern	short			rival_mode;
extern	int				my_rnd_sheet[4096];
extern	short			my_rnd_pt;
extern	short			cnct_game_rnd_sheed;

extern	BOOL			host_side;
extern	BYTE			decision_sw;
extern	BYTE			arrival_cont;

extern	int				rnd_count;
extern	BYTE			bf_cc_count[2],bf_rnd_count[2],bf_unit_chk[2];
extern	BOOL			ccc_out,rnd_out,unit_out;
extern	BYTE			ccc_wait[2];
extern	short			cnct_loop_ct,cnct_loop;
extern	short			cnct_loop_pt1, cnct_loop_pt2;

extern	short			spry_pt,spry_no_cont,spry_trgt,spry_rate[2],first_spry_pt[2];
extern	short			rvrs_time,rvrs_rule;
extern	short			map_now;
extern	short			bf_arrived_unit[2];

extern	short			auto_save_time;

extern	DWORD	last_tick,last_tick2;
extern	DWORD	tick_now,tick_diff;

// 通信対戦デバグ用
extern	BYTE			first_r_error;

// チャット用
extern	BOOL			input_chat_now;
//extern	char			my_chat[128];
//extern	char			friend_chat[128];

extern	TCHAR			my_chat[MAX_PATH];
extern	TCHAR			friend_chat[MAX_PATH];

extern	BYTE			my_chat_dsp_time;
extern	BYTE			friend_chat_dsp_time;

//extern	char			my_string[128];
//extern	BYTE			my_string_crsr;
//extern	BYTE			my_string_rpd;

extern	TCHAR			rival_ver[16];


/////////////////////

