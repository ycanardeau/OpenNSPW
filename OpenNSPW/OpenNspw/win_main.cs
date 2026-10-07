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

// Port of win_main.cpp. So far only its globals are ported; the platform objects (handles, DirectX interfaces) are
// left as comments until their stand-ins are ported.

namespace OpenNspw;

public partial class Nspw
{
// ウィンドウアプリケーション変数
//HINSTANCE	hInstApp;
//HWND	hwndApp;
//HCURSOR		my_cursor;
//D3DPRESENT_PARAMETERS d3dpp;
public int	appActive;
public int	fullscreen;


//	DirectXオブジェクト

//LPDIRECTDRAW7			lpDD=NULL;
//LPDIRECTDRAWSURFACE7 lpDDSPrimary=NULL;
//LPDIRECTDRAWSURFACE7 lpDDSBack=NULL;

//LPDIRECTDRAWCLIPPER	lpDDclip=NULL;

//LPDIRECTDRAWSURFACE7 lpDDS_OS=NULL;



// DirectInputの変数
//LPDIRECTINPUT8			pDInput = NULL;					// DirectInput
//LPDIRECTINPUTDEVICE8	pDIDevice = NULL;			// DirectInputデバイス
//LPDIRECTINPUTDEVICE8	pDIDeviceMouse = NULL;			// DirectInputデバイス
//DIDEVCAPS				diDevCaps;				// ジョイスティックの能力


// DirectSoundの変数
//LPDIRECTSOUND8			lpDS = NULL;
//LPDIRECTSOUNDBUFFER	lpDSP = NULL;

//LPDIRECTSOUNDBUFFER lpDSB_[NUM_SOUND_EFFECTS][SND_DUP];
public Array35<short>		snd_;	// [NUM_SOUND_EFFECTS]




// DirectMusicの変数
//IDirectMusicLoader8 *lpDML = NULL;
//IDirectMusicPerformance8 *lpDMP = NULL;




// フォント
//HFONT		gameFont_1,gameFont_2;




// ゲーム用
public int	cc_count;
public int	key_cndtn;	// パッドの状態


//POINT	ptCursor;		// 純粋なマウスカーソルの位置


// デバグ
public int	dbg_menu;
public Array16<int>	dbg;


// 通信対戦用
/*
GUID g_guidApp = { 0x2ae835d, 0x9179, 0x485f, { 0x83, 0x43, 0x90, 0x1d, 0x32, 0x7c, 0xe7, 0x94 } };
*/
//GUID g_guidApp = { 0x11bc0eb, 0xbdb3, 0x11d6, { 0xba, 0x95, 0x9c, 0xce, 0x36, 0x89, 0x70, 0x55 } };



//IDirectPlay8ThreadPool*		g_pThreadPool = NULL;		// DirectPlay threadpool object
//IDirectPlay8Peer*				g_pDP = NULL;					// DirectPlay peer object

//HKEY								hDPlaySampleRegKey;		// レジストリ

//HWND                       g_hDlg                        = NULL;    // HWND of main dialog
public int								dlg_answer;


//DPNID                      g_dpnidLocalPlayer            = 0;       // DPNID of local player
//DPNID                      g_dpnidRivalPlayer            = 0;       // DPNID of local player
////DPNID                      g_dpnidHostPlayer             = 0;       // DPNID of host player
public uint                      g_dwNumberOfActivePlayers     = 0;       // Number of players currently in game
public Array256<byte>                      g_strAppName             = TEXT<Array256<byte>>("NSPW NET");




public Array260<byte>                      g_strLocalPlayerName;          // Local player name
public Array260<byte>                      g_strRivalPlayerName=TEXT<Array260<byte>>(" - - - ");          // Rival player name
////TCHAR                      g_strSessionName[MAX_PATH];              // Session name
public Array260<byte>                      g_strPreferredProvider;        // Provider string
public Array260<byte>                      g_strRemoteHostname;           // TCP/IP remote host


public int                       g_bHostPlayer                 = FALSE;   // TRUE if local player is host
//GUID*                      g_pCurSPGuid                  = NULL;    // Currently selected guid
//DPNHANDLE                  g_hConnectAsyncOp             = NULL;    // Async handle for connecting to host

//DPN_BUFFER_DESC bufferDesc;
//DPNHANDLE hAsync;

// チャット
//HWND                       hwndChatDlg                        = NULL;    // HWND of chat dialog


/////////////////////

// iNSPWからもってきたやつ

public Array25<SPRT>					sprt;	// [MAX_SPRT]

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
public Array256<UNIT>			unit;
public short			max_unit;
public short			the_slct_unit, old_the_slct_unit;
public Array2<Array256<short>>	slct_unit;
public short			slct_unit_no;
public POINT			crsr_pt;

public Array10<int>				unit_info;
public Array512<FIRE>			fire;	// [FIRE_MAX]
public int				max_fire;
//BOOL			paint_effect_on;
public Array1024<EFFECT>			effect;	// [EFFECT_MAX]



public short			cls_flg;


public Array256<Array256<ushort>>		cmbt_map;					// マップ
public Array4096<KUMO>				kumo;	// [KUMO_MAX]								// 雲


public short			lf_btn,ri_btn;
public short			cmbt_menu_kind,cmbt_menu_slctd;

public Array64<double>			wrk_pp_x,wrk_pp_y;		//（ワーク）移動目的地の地図上の位置

public Array3<NEW_PP>			new_pp;
public Array3<NEW_SLCT>		new_slct;
public Array3<NEW_MENU>		new_menu;

public int				rest_time,game_end;
public Array4<int>		decision_point;
public short			your_side;
public short			game_speed;

public short			mode,demo_time,sinario;


public byte			map_edit,put_trgt,put_kind,put_kind_sub;
public Array3<byte>			rein;


public Array260<byte>			user_sinario_fn;			// ユーザーシナリオのファイルネーム

// 通信対戦用
public int			cnct_game;
//BOOL			cnct_now;
public byte			you_are_host;
public byte			you_were_host;

public int			you_can_order;
public int			you_ordered;

public Array3<NEW_PP>			bf_new_pp;
public Array3<NEW_SLCT>		bf_new_slct;
public Array3<NEW_MENU>		bf_new_menu;

public Array3<byte>			bf_game_system_menu;
public Array3<byte>			game_system_menu;

public Array3<Array90<short>>			bf_slct_unit;	// [3][USA_PLANE_END/2]
public int			go_next_1,go_next_2;
public int			join_game_start;
public short			rival_mode;
public Array4096<int>				my_rnd_sheet;
public short			my_rnd_pt;
public short			cnct_game_rnd_sheed;

public int			host_side;
public byte			decision_sw;
public byte			arrival_cont;

public int				rnd_count;
public Array2<byte>			bf_cc_count,bf_rnd_count,bf_unit_chk;
public int			ccc_out,rnd_out,unit_out;
public Array2<byte>			ccc_wait;
public short			cnct_loop_ct,cnct_loop;
public short			cnct_loop_pt1, cnct_loop_pt2;

public short			spry_pt,spry_no_cont,spry_trgt;
public Array2<short>	spry_rate,first_spry_pt;
public short			rvrs_time,rvrs_rule;
public short			map_now;
public Array2<short>			bf_arrived_unit;

public short			auto_save_time;

public uint	last_tick,last_tick2;
public uint	tick_now,tick_diff;


// 通信対戦デバグ用
public byte			first_r_error;

// チャット用
public int			input_chat_now=0;

//char			my_chat[128];
//char			friend_chat[128];
public Array260<byte>			my_chat;	// [MAX_PATH]
public Array260<byte>			friend_chat;	// [MAX_PATH]

public byte			my_chat_dsp_time;
public byte			friend_chat_dsp_time;

//char			my_string[128];
//BYTE			my_string_crsr;
//BYTE			my_string_rpd;

//
public Array16<byte>			rival_ver;

/////////////////////
}
