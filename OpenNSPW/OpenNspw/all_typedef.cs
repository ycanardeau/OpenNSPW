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

// Port of all_typedef.h. Each struct has the same field order, packing and size as in the 32-bit MSVC build, which
// LayoutTests checks against the reference.

using System.Runtime.InteropServices;

namespace OpenNspw;

/*
struct APP_PLAYER_INFO
	{
	LONG  lRefCount;									// Ref count so we can cleanup when all routines
															// are done w/ this object
	DPNID dpnidPlayer;								// DPNID of player
	TCHAR strPlayerName[MAX_PLAYER_NAME];		// Player name, this is a duplicate of DirectPlay's copy
	};
*/




[StructLayout(LayoutKind.Sequential)]
public struct	UNIT		// 全ての艦船、航空機、地図上の位置
	{
	public short				used;					// 使用してるかしてないか オンならその国籍
	public double				x,y;					// 地図上の位置
	public int					ctgry;					// カテゴリー（船とか飛行機とかの）
	public int					kind;					// 戦艦だとか空母だとか
	public short				type;					// 形式
	public Array16<int>			info;				// 追加の情報、航空機なら飛んでるとか、格納庫の中とか
	public int					os_indx_y;					// 各種パターンの頭の位置（ソースサーフェス）
	public int					os_indx_x;				// ユニットの向き
	public double				drctn,drctn_add,a_drctn_add;		// 進行角度 変進角度
	public double				spd,spd_add,a_spd_add,min_spd,max_spd;			// スピード 速度変更
	public int					stop;					// オンで方向変えず、減速のみ
	public int					spry;					// 補給と修理
	public int					mark;					// 選択されているか
	public Array64<double>		pp_x,pp_y;		// 移動目的地の地図上の位置
	public Array2<int>			em_flg;					// 緊急時の移動の処理フラグ
	public double				em_x,em_y;				// 緊急時の移動目的地の地図上の位置
	public double				to_ldr_drctn,to_ldr_dstc;		// 主に艦隊時、リーダとの相対位置。
	public short				is_ltl_ldr,ltl_ldr,no,for_ltl_ldr;				// 一時的指揮機番号、何番機
	public double				for_form_spd;			// 編隊を組み場合の遅れているユニットの速度
	public Array8<int>			hp;					// いわゆるヒットポイント
	public Array8<int>			arm;					// 武装
	public Array2<int>			arm2;				// サブ武装
	public Array8<double>		gas;					// 燃料
	public int					found;					// 相手サイドからの可視不可視
	public int					tech;					// そのユニットの技量

	public Array2<short>		rnd_250;				// 0-99までの乱数
	public Array2<short>		rnd_225;				// 0-99までの乱数
	public Array2<short>		rnd_200;				// 0-99までの乱数
	public Array2<short>		rnd_175;				// 0-99までの乱数
	public Array2<short>		rnd_150;				// 0-99までの乱数
	public Array2<short>		rnd_125;				// 0-99までの乱数

	public Array2<short>		rnd_100;				// 0-99までの乱数
	public Array2<short>		rnd_80;				//
	public Array2<short>		rnd_65;				//
	public Array2<short>		rnd_50;				//
	public Array2<short>		rnd_40;				//
	public Array2<short>		rnd_30;				//
	public Array2<short>		rnd_20;				//
	public Array2<short>		rnd_10;				//

	}

[StructLayout(LayoutKind.Sequential)]
public struct	EFFECT							// 雷跡とか爆炎とか
	{
	public short				used;					// 自サイド
	public short				layer;					// 使用してるかしてないか、アッパーかローワーか
	public int					kind;
	public int					no;						// Sprite nuber of its Sprite Source
	public Array8<int>			info;					// 追加の情報、
	public double				x,y;					// 地図上の位置
	public double				x2,y2;					// ＢＬＴ や ＲＡＳ
	public int					found;					// 自サイド	からの可視、不可視
	//BOOL				side;					//
	}

[StructLayout(LayoutKind.Sequential)]
public struct	FIRE
	{
	public int		used;							// オン、オフ。オンなら、ターゲットのユニット番号（ＢＬＴに必要）
	public int		kind;							// 弾丸(BLT)、爆弾(BOM)、魚雷(TPD)、炸裂弾(SHL)、ＶＴ信管(VTH)だとか、、
	public Array9<int>	info;
	public int		no;								// Sprite nuber of its Sprite Source
	public double	x,y;
	public double	drctn,spd,spd_add,last_spd;
	public double	last_x,last_y;					// 必要なら、最終目的地
	}
/*
// structure used to store DirectPlay information
typedef struct
	{
	LPDIRECTPLAY3A		lpDirectPlay3A;		// IDirectPlay3A interface pointer
	HANDLE				hPlayerEvent;		// player event to use
	DPID				dpidPlayer;			// ID of player created
	BOOL				bIsHost;			// TRUE if we are hosting the session
	} DPLAYINFO, *LPDPLAYINFO;
*/
[StructLayout(LayoutKind.Sequential)]
public struct	NEW_PP
	{
	public short		used;						//
	public double	x,y;							//
	public int	cls;
	}

[StructLayout(LayoutKind.Sequential)]
public struct	NEW_SLCT
	{
	public int	sw;								//
	public short		the_slct_unit,m;			//
	public double	gr_x,gr_y;						// グランドX，Ｙ
	}

[StructLayout(LayoutKind.Sequential)]
public struct	NEW_MENU
	{
	public short	menu;							// これがｓｗの代わり
	public short	the_slct_unit;
	}

[StructLayout(LayoutKind.Sequential)]
public struct KUMO
	{
	public short				used;					// 使用してるかしてないか
	public double				x,y;					// 地図上の位置
	public int					kind;					// 戦艦だとか空母だとか
	}

[StructLayout(LayoutKind.Sequential)]
public struct	SPRT
	{
	public int		no;
	public int		x,y,cx,cy;
	public int		wd,ht,base_x,base_y;
	public int		os_of_x;
	}






// Change compiler pack alignment to be BYTE aligned, and pop the current value
//#pragma pack( push, 1 )

//struct GAMEMSG_GENERIC
//struct _GENERICMSG
//{
    //DWORD dwType;
//};


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GENERICMSG
	{
//	BYTE        byType;
	public uint			dwType;
	}


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct UNIT_MSG
	{
    public byte		byType;
	public int			used;							//
	public double		x,y;							//
	public int		cls;
	}



[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct	_DP_DATA_1
	{
	public uint	dwType;
	public Array16<byte>	my_name;
	public Array10<short>	data;
	}



[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct _DP_NEW_PP
	{
	public uint	dwType;

	// NEW PP
	public byte	used;							//
	public short	x;							//
	public short	y;							//
	public int	cls;
	public Array90<byte>	slct_unit;	// [USA_PLANE_END/2]
	}


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct	_DP_NEW_PP_SHIP
	{
	public uint	dwType;

	// NEW PP
	public byte	used;							//
	public short	x;							//
	public short	y;							//
	public int	cls;
	public Array40<byte>	slct_unit;	// [JPN_SHIP_END/*20*/]
	}


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct	_DP_NEW_PP_PLANE
	{
	public uint	dwType;

	// NEW PP
	public byte	used;							//
	public short	x;							//
	public short	y;							//
	public int	cls;
	public Array50<byte>	slct_unit;	// [JPN_PLANE_END-USA_SHIP_END/*30*/]
	}



[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct	_DP_NEW_SLCT
	{
	public uint	dwType;

	// NEW SLCT
	public int	sw;								//
	public byte	the_slct_unit,m;				//
	public short	gr_x,gr_y;						// グランドX，Ｙ
	public Array90<byte>	slct_unit;	// [USA_PLANE_END/2/*50*/]
	}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct	_DP_NEW_SLCT_SHIP
	{
	public uint	dwType;

	// NEW SLCT
	public int	sw;								//
	public byte	the_slct_unit,m;				//
	public short	gr_x,gr_y;						// グランドX，Ｙ
	public Array40<byte>	slct_unit;	// [JPN_SHIP_END/*20*/]
	}



[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct	_DP_NEW_SLCT_PLANE
	{
	public uint	dwType;

	// NEW SLCT
	public int	sw;								//
	public byte	the_slct_unit,m;				//
	public short	gr_x,gr_y;						// グランドX，Ｙ
	public Array50<byte>	slct_unit;	// [JPN_PLANE_END-USA_SHIP_END/*30*/]
	}


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct	_DP_NEW_SLCT_LAND
	{
	public uint	dwType;

	// NEW SLCT
	public int	sw;								//
	public byte	the_slct_unit,m;				//
	public short	gr_x,gr_y;						// グランドX，Ｙ
	}





[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct	_DP_NEW_MENU
	{
	public uint	dwType;

	// NEW MENU
	public byte		menu;								// これがｓｗの代わり
	public byte		the_slct_unit;

	public Array90<byte>	slct_unit;	// [USA_PLANE_END/2/*50*/]
	}




[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct	_DP_FLAG
	{
	public uint	dwType;


	public byte	cc_chk;
	public byte	unit_chk;
	public byte	rnd_chk;

	public byte	ccc_wait_chk;

	public short	rival_mode;					// お互いのモードを飛ばす

	}





[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct	_DP_DATA_20
	{
	public uint	dwType;

	public Array260<byte>	friend_chat;	// [MAX_PATH/*128*/]

	}


#if false
typedef	struct	//_DP_SNRO_SEND
	{
	DWORD	dwType;

	BYTE		cmbt_map[180][240];					// マップ
	UNIT		unit[USA_PLANE_END+1];
	BYTE		rein[3];

	} _DP_SNRO_SEND;
#endif


// Pop the old pack alignment
//#pragma pack( pop )
