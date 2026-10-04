

	// Headder File
//winmm.lib dxguid.lib dsound.lib d3d9.lib d3dx9.lib dxerr9.lib dinput8.lib kernel32.lib user32.lib 
//gdi32.lib winspool.lib comdlg32.lib advapi32.lib shell32.lib ole32.lib oleaut32.lib uuid.lib odbc32.lib 
//odbccp32.lib 

#define	STRICT						// 型チェックを厳密に
#define	WIN32_LEAN_AND_MEAN		// ヘッダーから使われていないのを省く
#define	_WIN32_DCOM 


#include	<windows.h>
//#include <stdio.h>
//#include <stdlib.h>
#include <math.h>
#include <imm.h>
#include <ddraw.h>
#include <d3d9.h>
#include <d3dx9.h>

#ifndef DIRECTINPUT_VERSION
#define DIRECTINPUT_VERSION 0x800
#endif

#include <dinput.h>
#include <dxerr9.h>
#include <commctrl.h>
#include <dplay8.h>
#include <dplobby8.h>
#include <dpaddr.h>
#include <objbase.h>
//#include <conio.h>
#include <dmusicc.h>
#include <dmusici.h>
#include <dsound.h>
#include <time.h>				// 乱数に使用
#include <winuser.h>
#include <d3dx9math.h>

#include	<commdlg.h>




#include <tchar.h>

#include <stdio.h>


#include "DXUtil.h"
#include "resource.h"



#define RELEASE(x) 	if(x){x->Release();x=NULL;}

#define SCRN_WIDTH		1024
#define SCRN_HEIGHT		768

#define	WIDTH		1024
#define	HEIGHT	768



#define	CAPTION		"NSPW on the Net"
#define	CLASS_NAME	"NSPW_NET"


#define DIDEVICE_BUFFERSIZE	100				// ダイレクトインプット　デバイスに設定するバッファ・サイズ

#define FRONT_BTN	(0x01<<0)
#define BACK_BTN	(0x01<<1)
#define RIGHT_BTN	(0x01<<2)
#define LEFT_BTN	(0x01<<3)
#define UP_BTN		(0x01<<4)
#define DOWN_BTN	(0x01<<5)
#define R_TURN_BTN	(0x01<<6)
#define L_TURN_BTN	(0x01<<7)

#define MS_R_BTN	(0x01<<8)
#define MS_L_BTN	(0x01<<9)
#define MS_C_BTN	(0x01<<10)

#define MS_R_BTN2	(0x01<<11)
#define MS_L_BTN2	(0x01<<12)
#define MS_C_BTN2	(0x01<<13)

#define FRONT_BTN2	(0x01<<14)
#define BACK_BTN2	(0x01<<15)
#define SPACE		(0x01<<16)
#define V_KEY		(0x01<<17)

#define TOP_VIEW_BTN	(0x01<<18)
#define TOP_VIEW_BTN2	(0x01<<19)


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

#define	CMBT_WIDTH		768
#define	CMBT_HEIGHT		768
#define	CMBT_REST		40


#define MAX_SPRT		25


// スプライトナンバー
#define	TTL_BACK			0
#define	UNIT_JPN			1
#define	UNIT_USA			2
#define	UNIT_INFO_JPN		3
#define	UNIT_INFO_USA		4
#define	MAP_TIP_NRML		5
#define	SUB_UNIT			6
#define	BTN_1				7
#define	BTN_2				8
#define	BTN_BASE			9
#define	MAP_BASE			10


// 効果音
#define		SND_DUP		6		// 同時に鳴らせる場合の最大音数

#define	NUM_SOUND_EFFECTS		35


enum SND_NO
	{
	AA_BLT1,
	AA_BLT2,
	AA_BLT3,
	AA_BLT4,

	AA_SHL1,
	AA_SHL2,
	AA_SHL3,
	AA_SHL4,
	AA_SHL5,


	TPD_HIT1,
	TPD_HIT2,

	BOM_HIT1,
	BOM_HIT2,

	SHIP_SINK1,
	SHIP_SINK2,

	SPL1,
	SEA1,
	GUN1,
	GUN2,
	GUN3,

	FALL1,

	BOMB_OFF,
	BB_BOMB,

	TPD_LOS,

	PLANE_FLYING,
	PLANE1,
	PLANE2,

	TAKE_OFF,
	SNR,

	CLICK1,
	CLICK2,				//30
	END_OF_SND_NO
	};



#define	BB1		1
#define	CA1		2
#define	DD1		3
#define SS1		4
#define CV1		5
#define CVL1	6

#define	FT1		7
#define	AT1		8
#define	BM1		9

#define TR1		10



#define AP		11
#define SP		12

#define CT1		13
#define MN1		14
#define GF1		15
#define GF2		16
#define GF3		17




#define	MAP_TOP		7200
#define	MAP_BOTTOM	-7200
#define	MAP_RIGHT	9600
#define	MAP_LEFT	-9600


#define	PI            3.14159265358979323846
#define	a_PI          0.01745329251994
#define	RAD_to		  57.2957795131

 
#define		SCRN_MAX_SPD		1600
#define		scrn_moving_add	15

#define		OS_MAX			10



#define		KEY_UP		8
#define		KEY_RIUP	9	
#define		KEY_RI		6
#define		KEY_RIDW	3	
#define		KEY_DW		2
#define		KEY_LFDW	1	
#define		KEY_LF		4
#define		KEY_LFUP	7	


#define		MAXPLAYERS			2			// max no. players in the session
#define		TYPE_UNIT_MSG       0x11    // message containing field layout, sent by host


#define		SHIP		1
#define		PLANE		2
#define		BASE		3

// 状態
#define		FLYING		1
#define		PARKING		2		

// モード／メニュー
#define		MOVE		1
#define		SLOW		2
#define		RETURN		4
#define		SPRY		5

#define		RDY_TPD		15
#define		RDY_BOM		16
#define		NOTHING		17

// ファイアの種類 武装の種類 
#define		BLT			11	// 弾丸
#define		GUN			12	// 対地砲
#define		SHL			13	// 対空炸裂弾
#define		VTH			14	// 対空炸裂ＶＴ信管弾
#define		TPD			15	// 魚雷
#define		BOM			16	// 爆弾
#define		NTG			17	// 
#define		TUN			18	// 発進最低整備
#define		ASB			19	// 対潜爆弾	
#define		RAS			20	// 対空機関砲弾　Rapid anti Air Shell

#define		SP_GUN			21	// 対地砲

#define		TR_SP		30
#define		TR_AP		31
#define		TR_GF1		32
#define		TR_GF2		33
#define		TR_GF3		34






// 弾薬の消費サイズ
#define		GUN_SZ			5+2	// 対地砲
#define		SHL_SZ			4+1	// 対空炸裂弾
#define		TPD_SZ			5+2	// 魚雷
#define		ASB_SZ			2	// 対潜爆弾	
#define		RAS_SZ			3+1	// 対空機関砲弾　Rapid anti Air Shell


// 各弾種の破壊力の値
#define		BLT_DMG		1
#define		GUN_DMG		2
#define		SHL_DMG		2
#define		TPD_DMG		5
#define		BOM_DMG		3
#define		ASB_DMG		3
#define		RAS_DMG		1


//ユニット別ＨＰ
#define	BB1_HP		40
#define	CA1_HP		32
#define	DD1_HP		18
#define SS1_HP			7
#define CV1_HP			35-3
#define CVL1_HP		30-3

#define	FT1_HP		14
#define	AT1_HP		16
#define	BM1_HP		50

#define TR1_HP		9

#define AP_HP		260
#define SP_HP		320

#define CT1_HP		150
#define MN1_HP		150
#define GF1_HP		180
#define GF2_HP		320
#define GF3_HP		400


//
#define	AIR_TPD_SPD				1.8
#define	AIR_TPD_LOS_DSTC		220
#define	TPD_SPD					2.0


#define		TUNE_SPAN	900
#define		RDY_SPAN		700

// エフェクト
#define		UPPER		1
#define		LOWER		2

// 国籍
#define		JPN					1	// 日本海軍
#define		USA					2	// 合衆国海軍

// 現モード
#define		DEMO						1
#define		CNCT_GAME_SETUP		3
#define		CNCT_GAME_SETTING		4
#define		CNCT_CNFG_SETTING		5
#define		CMBT						10

//
#define		CMBT_SPD	1.5
#define		FT_EYE		160

//
#define		PALT_RED	249
#define		PALT_BUL	252

//
#define		ON_PP		40


// 視界
#define		BB1_SIGHT	800
#define		CA1_SIGHT	700
#define		DD1_SIGHT	550
#define		SS1_SIGHT	600
#define		CV1_SIGHT	500
#define		CVL1_SIGHT	600
#define		TR1_SIGHT	300
#define		FT1_SIGHT	500
#define		AT1_SIGHT	1000
#define		BM1_SIGHT	1200
#define		AP_SIGHT		1700
#define		SP_SIGHT		1700

#define		CT1_SIGHT	800
#define		MN1_SIGHT	800
#define		GF1_SIGHT	1000
#define		GF2_SIGHT	1500
#define		GF3_SIGHT	2000





// 再装填時間
#define		RELOAD_TPD_DD		800
#define		RELOAD_TPD_SS		1500

//タスクフォース
#define		MAX_TF				8

// 判定
#define		JPN_WIN		1	
#define		USA_WIN		2	
#define		JPN_LOST	3	
#define		USA_LOST	4	

#define		DRAW		10	


//くも
#define		KUMO_MAX	4096
#define		FIRE_MAX	512
#define		EFFECT_MAX	1024

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
#define		JPN_SHIP_START		1
#define		JPN_SHIP_END		40

#define		USA_SHIP_START		41
#define		USA_SHIP_END		80

#define		JPN_PLANE_START		81
#define		JPN_PLANE_END		130

#define		USA_PLANE_START		131
#define		USA_PLANE_END		180



// ダイレクトプレイ用

//#define PLAYER_ADDREF( pPlayerInfo )    if( pPlayerInfo ) pPlayerInfo->lRefCount++;
//#define PLAYER_RELEASE( pPlayerInfo )   if( pPlayerInfo ) { pPlayerInfo->lRefCount--; if( pPlayerInfo->lRefCount <= 0 ) SAFE_DELETE( pPlayerInfo ); } 	pPlayerInfo = NULL;

#define MAX_PLAYER_NAME                 14
#define WM_APP_UPDATE_STATS             (WM_APP + 0)
//#define WM_APP_DISPLAY_WAVE           (WM_APP + 1)
#define DOWORK_TIMESLICE                8 // let DirectPlay work for 8 ms at a time
#define ADDRESSOVERRIDE_PORT            2310





enum EFCT_NO
	{
	MSG_TST,
	MSG_EXIT_WAITING,
	MSG_END
	};




#define		NSPW_THE_NET		1

#define		MY_NAME_IS			1
#define		AND_MY_NAME_IS		2
#define		OUT_SETUP			3
#define		OUT_GAME_SETTING	4
#define		SIDE_AND_SINARIO	5
#define		GO_GAME_SETTING		6
#define		OUT_CNFG_SETTING	7

#define		RESUME_AND_GO_GAME_SETTING		8

#define		START_IN_RESUME		9

#define		START_IN_AUTOSAVE		10

#define		USER_SINARIO_FN		11
//#define		SNRO_SEND			12

#define		DP_NO_ORDER			15

#define		DP_NEW_PP			20
#define		DP_NEW_PP_SHIP		21
#define		DP_NEW_PP_PLANE		22

#define		DP_NEW_SLCT			30
#define		DP_NEW_SLCT_SHIP	31
#define		DP_NEW_SLCT_PLANE	32
#define		DP_NEW_SLCT_LAND	33

#define		DP_NEW_MENU			40
#define		DP_FLAG_1			50

#define		DP_ARRIVED_UNIT		60

#define		DP_CHAT_1			100
#define		CHAT_DSP_TIME		400

#define		RIVAL_MODE			500
#define		RIVAL_VER			501


#define		EASY_SEND		DPNSEND_NOCOMPLETE | DPNSEND_NOLOOPBACK
#define		MUST_SEND		DPNSEND_NOLOOPBACK | DPNSEND_GUARANTEED



#define		VER				"1.10"

#define		LNGG_VER			0		/* 0=Japanese 1=English */

#define		DBG_MODE			1

#define		CONN_DBG			0
#define		SND_SW			1
