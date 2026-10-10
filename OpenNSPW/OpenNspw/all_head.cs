	// Headder File
//winmm.lib dxguid.lib dsound.lib d3d9.lib d3dx9.lib dxerr9.lib dinput8.lib kernel32.lib user32.lib
//gdi32.lib winspool.lib comdlg32.lib advapi32.lib shell32.lib ole32.lib oleaut32.lib uuid.lib odbc32.lib
//odbccp32.lib

// Port of all_head.h. The #defines become constants. A #define whose value is an expression (GUN_SZ, CV1_HP, ...)
// is substituted as text in C++, so it only behaves like a constant where it is not part of a larger expression with
// higher precedence; every use in the source has been checked to be one of those.

namespace OpenNspw;

public static class all_head
{
//#define	STRICT						// 型チェックを厳密に
//#define	WIN32_LEAN_AND_MEAN		// ヘッダーから使われていないのを省く
//#define	_WIN32_DCOM


//#include	<windows.h>
////#include <stdio.h>
////#include <stdlib.h>
//#include <math.h>
//#include <imm.h>
//#include <ddraw.h>
//#include <d3d9.h>
//#include <d3dx9.h>

//#ifndef DIRECTINPUT_VERSION
//#define DIRECTINPUT_VERSION 0x800
//#endif

//#include <dinput.h>
//#include <dxerr9.h>
//#include <commctrl.h>
//#include <dplay8.h>
//#include <dplobby8.h>
//#include <dpaddr.h>
//#include <objbase.h>
////#include <conio.h>
//#include <dmusicc.h>
//#include <dmusici.h>
//#include <dsound.h>
//#include <time.h>				// 乱数に使用
//#include <winuser.h>
//#include <d3dx9math.h>

//#include	<commdlg.h>




//#include <tchar.h>

//#include <stdio.h>


//#include "DXUtil.h"
//#include "resource.h"



//#define RELEASE(x) 	if(x){x->Release();x=NULL;}

public const int SCRN_WIDTH		= 1024;
public const int SCRN_HEIGHT		= 768;

public const int	WIDTH		= 1024;
public const int	HEIGHT	= 768;



public const string	CAPTION		= "NSPW on the Net";
public const string	CLASS_NAME	= "NSPW_NET";


public const int DIDEVICE_BUFFERSIZE	= 100;				// ダイレクトインプット　デバイスに設定するバッファ・サイズ

public const int FRONT_BTN	= (0x01<<0);
public const int BACK_BTN	= (0x01<<1);
public const int RIGHT_BTN	= (0x01<<2);
public const int LEFT_BTN	= (0x01<<3);
public const int UP_BTN		= (0x01<<4);
public const int DOWN_BTN	= (0x01<<5);
public const int R_TURN_BTN	= (0x01<<6);
public const int L_TURN_BTN	= (0x01<<7);

public const int MS_R_BTN	= (0x01<<8);
public const int MS_L_BTN	= (0x01<<9);
public const int MS_C_BTN	= (0x01<<10);

public const int MS_R_BTN2	= (0x01<<11);
public const int MS_L_BTN2	= (0x01<<12);
public const int MS_C_BTN2	= (0x01<<13);

public const int FRONT_BTN2	= (0x01<<14);
public const int BACK_BTN2	= (0x01<<15);
public const int SPACE		= (0x01<<16);
public const int V_KEY		= (0x01<<17);

public const int TOP_VIEW_BTN	= (0x01<<18);
public const int TOP_VIEW_BTN2	= (0x01<<19);


/*


winmm.lib dxguid.lib dsound.lib d3d9.lib d3dx9.lib dxerr9.lib dinput8.lib kernel32.lib user32.lib gdi32.lib winspool.lib comdlg32.lib advapi32.lib shell32.lib ole32.lib oleaut32.lib uuid.lib odbc32.lib odbccp32.lib


#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include <d3d9.h>
#include <d3dx9.h>
#include <dinput.h>
#include <dxerr9.h>
#include <dsound.h>
#include <conio.h>
#include <dmusicc.h>
#include <dmusici.h>
#include <time.h>				// 乱数に使用
#include <winuser.h>
#include <d3dx9math.h>




*/

//#define SCRN_WIDTH		1024
//#define SCRN_HEIGHT		768
//#define	SCRN_DEPTH		8

public const int	CMBT_WIDTH		= 768;
public const int	CMBT_HEIGHT		= 768;
public const int	CMBT_REST		= 40;


public const int MAX_SPRT		= 25;


// スプライトナンバー
public const int	TTL_BACK			= 0;
public const int	UNIT_JPN			= 1;
public const int	UNIT_USA			= 2;
public const int	UNIT_INFO_JPN		= 3;
public const int	UNIT_INFO_USA		= 4;
public const int	MAP_TIP_NRML		= 5;
public const int	SUB_UNIT			= 6;
public const int	BTN_1				= 7;
public const int	BTN_2				= 8;
public const int	BTN_BASE			= 9;
public const int	MAP_BASE			= 10;


// 効果音
public const int		SND_DUP		= 6;		// 同時に鳴らせる場合の最大音数

public const int	NUM_SOUND_EFFECTS		= 35;


// enum SND_NO
// The enumerators are used as ints, so they are ported as constants.
	//{
public const int	AA_BLT1 = 0;
public const int	AA_BLT2 = 1;
public const int	AA_BLT3 = 2;
public const int	AA_BLT4 = 3;

public const int	AA_SHL1 = 4;
public const int	AA_SHL2 = 5;
public const int	AA_SHL3 = 6;
public const int	AA_SHL4 = 7;
public const int	AA_SHL5 = 8;


public const int	TPD_HIT1 = 9;
public const int	TPD_HIT2 = 10;

public const int	BOM_HIT1 = 11;
public const int	BOM_HIT2 = 12;

public const int	SHIP_SINK1 = 13;
public const int	SHIP_SINK2 = 14;

public const int	SPL1 = 15;
public const int	SEA1 = 16;
public const int	GUN1 = 17;
public const int	GUN2 = 18;
public const int	GUN3 = 19;

public const int	FALL1 = 20;

public const int	BOMB_OFF = 21;
public const int	BB_BOMB = 22;

public const int	TPD_LOS = 23;

public const int	PLANE_FLYING = 24;
public const int	PLANE1 = 25;
public const int	PLANE2 = 26;

public const int	TAKE_OFF = 27;
public const int	SNR = 28;

public const int	CLICK1 = 29;
public const int	CLICK2 = 30;				//30
public const int	END_OF_SND_NO = 31;
	//};



// BB1, CA1, DD1, SS1, CV1, CVL1, FT1, AT1, BM1, TR1, AP, SP, CT1, MN1, GF1, GF2 and GF3: UnitKind.




public const int	MAP_TOP		= 7200;
public const int	MAP_BOTTOM	= -7200;
public const int	MAP_RIGHT	= 9600;
public const int	MAP_LEFT	= -9600;


public const double	PI            = 3.14159265358979323846;
public const double	a_PI          = 0.01745329251994;
public const double	RAD_to		  = 57.2957795131;


public const int		SCRN_MAX_SPD		= 1600;
public const int		scrn_moving_add	= 15;

public const int		OS_MAX			= 10;



public const int		KEY_UP		= 8;
public const int		KEY_RIUP	= 9;
public const int		KEY_RI		= 6;
public const int		KEY_RIDW	= 3;
public const int		KEY_DW		= 2;
public const int		KEY_LFDW	= 1;
public const int		KEY_LF		= 4;
public const int		KEY_LFUP	= 7;


public const int		MAXPLAYERS			= 2;			// max no. players in the session
public const int		TYPE_UNIT_MSG       = 0x11;    // message containing field layout, sent by host


// SHIP, PLANE and BASE: UnitCategory.

// 状態
public const int		FLYING		= 1;
public const int		PARKING		= 2;

// モード／メニュー
public const int		MOVE		= 1;
public const int		SLOW		= 2;
public const int		RETURN		= 4;
public const int		SPRY		= 5;

public const int		RDY_TPD		= 15;
public const int		RDY_BOM		= 16;
public const int		NOTHING		= 17;

// ファイアの種類 武装の種類
public const int		BLT			= 11;	// 弾丸
public const int		GUN			= 12;	// 対地砲
public const int		SHL			= 13;	// 対空炸裂弾
public const int		VTH			= 14;	// 対空炸裂ＶＴ信管弾
public const int		TPD			= 15;	// 魚雷
public const int		BOM			= 16;	// 爆弾
public const int		NTG			= 17;	//
public const int		TUN			= 18;	// 発進最低整備
public const int		ASB			= 19;	// 対潜爆弾
public const int		RAS			= 20;	// 対空機関砲弾　Rapid anti Air Shell

public const int		SP_GUN			= 21;	// 対地砲

public const int		TR_SP		= 30;
public const int		TR_AP		= 31;
public const int		TR_GF1		= 32;
public const int		TR_GF2		= 33;
public const int		TR_GF3		= 34;






// 弾薬の消費サイズ
public const int		GUN_SZ			= 5+2;	// 対地砲
public const int		SHL_SZ			= 4+1;	// 対空炸裂弾
public const int		TPD_SZ			= 5+2;	// 魚雷
public const int		ASB_SZ			= 2;	// 対潜爆弾
public const int		RAS_SZ			= 3+1;	// 対空機関砲弾　Rapid anti Air Shell


// 各弾種の破壊力の値
public const int		BLT_DMG		= 1;
public const int		GUN_DMG		= 2;
public const int		SHL_DMG		= 2;
public const int		TPD_DMG		= 5;
public const int		BOM_DMG		= 3;
public const int		ASB_DMG		= 3;
public const int		RAS_DMG		= 1;


//ユニット別ＨＰ
public const int	BB1_HP		= 40;
public const int	CA1_HP		= 32;
public const int	DD1_HP		= 18;
public const int SS1_HP			= 7;
public const int CV1_HP			= 35-3;
public const int CVL1_HP		= 30-3;

public const int	FT1_HP		= 14;
public const int	AT1_HP		= 16;
public const int	BM1_HP		= 50;

public const int TR1_HP		= 9;

public const int AP_HP		= 260;
public const int SP_HP		= 320;

public const int CT1_HP		= 150;
public const int MN1_HP		= 150;
public const int GF1_HP		= 180;
public const int GF2_HP		= 320;
public const int GF3_HP		= 400;


//
public const double	AIR_TPD_SPD				= 1.8;
public const int	AIR_TPD_LOS_DSTC		= 220;
public const double	TPD_SPD					= 2.0;


public const int		TUNE_SPAN	= 900;
public const int		RDY_SPAN		= 700;

// エフェクト
// UPPER and LOWER: EffectLayer.

// 国籍
// JPN and USA: Side.

// 現モード
// DEMO, CNCT_GAME_SETUP, CNCT_GAME_SETTING, CNCT_CNFG_SETTING and CMBT: GameMode.

//
public const double		CMBT_SPD	= 1.5;
public const int		FT_EYE		= 160;

//
public const int		PALT_RED	= 249;
public const int		PALT_BUL	= 252;

//
public const int		ON_PP		= 40;


// 視界
public const int		BB1_SIGHT	= 800;
public const int		CA1_SIGHT	= 700;
public const int		DD1_SIGHT	= 550;
public const int		SS1_SIGHT	= 600;
public const int		CV1_SIGHT	= 500;
public const int		CVL1_SIGHT	= 600;
public const int		TR1_SIGHT	= 300;
public const int		FT1_SIGHT	= 500;
public const int		AT1_SIGHT	= 1000;
public const int		BM1_SIGHT	= 1200;
public const int		AP_SIGHT		= 1700;
public const int		SP_SIGHT		= 1700;

public const int		CT1_SIGHT	= 800;
public const int		MN1_SIGHT	= 800;
public const int		GF1_SIGHT	= 1000;
public const int		GF2_SIGHT	= 1500;
public const int		GF3_SIGHT	= 2000;





// 再装填時間
public const int		RELOAD_TPD_DD		= 800;
public const int		RELOAD_TPD_SS		= 1500;

//タスクフォース
public const int		MAX_TF				= 8;

// 判定
// JPN_WIN, USA_WIN, JPN_LOST, USA_LOST and DRAW: GameResult.


//くも
public const int		KUMO_MAX	= 4096;
public const int		FIRE_MAX	= 512;
public const int		EFFECT_MAX	= 1024;

/*
#define		JPN_SHIP_START		1
#define		JPN_SHIP_END		30

#define		USA_SHIP_START		31
#define		USA_SHIP_END		60

#define		JPN_PLANE_START		61
#define		JPN_PLANE_END		100

#define		USA_PLANE_START		101
#define		USA_PLANE_END		140
*/
public const int		JPN_SHIP_START		= 1;
public const int		JPN_SHIP_END		= 40;

public const int		USA_SHIP_START		= 41;
public const int		USA_SHIP_END		= 80;

public const int		JPN_PLANE_START		= 81;
public const int		JPN_PLANE_END		= 130;

public const int		USA_PLANE_START		= 131;
public const int		USA_PLANE_END		= 180;



// ダイレクトプレイ用

//#define PLAYER_ADDREF( pPlayerInfo )    if( pPlayerInfo ) pPlayerInfo->lRefCount++;
//#define PLAYER_RELEASE( pPlayerInfo )   if( pPlayerInfo ) { pPlayerInfo->lRefCount--; if( pPlayerInfo->lRefCount <= 0 ) SAFE_DELETE( pPlayerInfo ); } 	pPlayerInfo = NULL;

public const int MAX_PLAYER_NAME                 = 14;
public const uint WM_APP_UPDATE_STATS             = (winuser.WM_APP + 0);
////#define WM_APP_DISPLAY_WAVE           (WM_APP + 1)
public const int DOWORK_TIMESLICE                = 8; // let DirectPlay work for 8 ms at a time
public const int ADDRESSOVERRIDE_PORT            = 2310;





// enum EFCT_NO
	//{
public const int	MSG_TST = 0;
public const int	MSG_EXIT_WAITING = 1;
public const int	MSG_END = 2;
	//};




public const int		NSPW_THE_NET		= 1;

// MY_NAME_IS to RIVAL_VER: MessageType.
public const int		CHAT_DSP_TIME		= 400;




//#define		EASY_SEND		DPNSEND_NOCOMPLETE | DPNSEND_NOLOOPBACK
//#define		MUST_SEND		DPNSEND_NOLOOPBACK | DPNSEND_GUARANTEED
// In dplay8.cs, with the DPNSEND_* values.



public const string		VER				= "1.10";

public const int		LNGG_VER			= 0;		/* 0=Japanese 1=English */

public const int		DBG_MODE			= 1;

public const int		CONN_DBG			= 0;
public const int		SND_SW			= 1;
}
