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



/*
struct APP_PLAYER_INFO
	{
	LONG  lRefCount;									// Ref count so we can cleanup when all routines 
															// are done w/ this object
	DPNID dpnidPlayer;								// DPNID of player
	TCHAR strPlayerName[MAX_PLAYER_NAME];		// Player name, this is a duplicate of DirectPlay's copy
	};
*/




typedef	struct	_UNIT		// 全ての艦船、航空機、地図上の位置
	{
	short				used;					// 使用してるかしてないか オンならその国籍
	double				x,y;					// 地図上の位置
	int					ctgry;					// カテゴリー（船とか飛行機とかの）
	int					kind;					// 戦艦だとか空母だとか
	short				type;					// 形式
	int					info[16];				// 追加の情報、航空機なら飛んでるとか、格納庫の中とか
	int					os_indx_y;					// 各種パターンの頭の位置（ソースサーフェス）
	int					os_indx_x;				// ユニットの向き
	double				drctn,drctn_add,a_drctn_add;		// 進行角度 変進角度
	double				spd,spd_add,a_spd_add,min_spd,max_spd;			// スピード 速度変更
	BOOL				stop;					// オンで方向変えず、減速のみ
	int					spry;					// 補給と修理
	BOOL				mark;					// 選択されているか
	double				pp_x[64],pp_y[64];		// 移動目的地の地図上の位置
	int					em_flg[2];					// 緊急時の移動の処理フラグ
	double				em_x,em_y;				// 緊急時の移動目的地の地図上の位置
	double				to_ldr_drctn,to_ldr_dstc;		// 主に艦隊時、リーダとの相対位置。
	short				is_ltl_ldr,ltl_ldr,no,for_ltl_ldr;				// 一時的指揮機番号、何番機
	double				for_form_spd;			// 編隊を組み場合の遅れているユニットの速度
	int					hp[8];					// いわゆるヒットポイント
	int					arm[8];					// 武装
	int					arm2[2];				// サブ武装
	double				gas[8];					// 燃料
	BOOL				found;					// 相手サイドからの可視不可視
	int					tech;					// そのユニットの技量

	short				rnd_250[2];				// 0-99までの乱数
	short				rnd_225[2];				// 0-99までの乱数
	short				rnd_200[2];				// 0-99までの乱数
	short				rnd_175[2];				// 0-99までの乱数
	short				rnd_150[2];				// 0-99までの乱数
	short				rnd_125[2];				// 0-99までの乱数

	short				rnd_100[2];				// 0-99までの乱数
	short				rnd_80[2];				// 
	short				rnd_65[2];				// 
	short				rnd_50[2];				// 
	short				rnd_40[2];				// 
	short				rnd_30[2];				// 
	short				rnd_20[2];				// 
	short				rnd_10[2];				// 

	} UNIT;

typedef	struct	_EFFECT							// 雷跡とか爆炎とか
	{
	short				used;					// 自サイド
	short				layer;					// 使用してるかしてないか、アッパーかローワーか
	int					kind;
	int					no;						// Sprite nuber of its Sprite Source
	int					info[8];				// 追加の情報、
	double				x,y;					// 地図上の位置
	double				x2,y2;					// ＢＬＴ や ＲＡＳ
	BOOL				found;					// 自サイド	からの可視、不可視
	//BOOL				side;					// 
	} EFFECT;

typedef	struct	_FIRE
	{
	int		used;							// オン、オフ。オンなら、ターゲットのユニット番号（ＢＬＴに必要）
	int		kind;							// 弾丸(BLT)、爆弾(BOM)、魚雷(TPD)、炸裂弾(SHL)、ＶＴ信管(VTH)だとか、、
	int		info[9];
	int		no;								// Sprite nuber of its Sprite Source
	double	x,y;
	double	drctn,spd,spd_add,last_spd;
	double	last_x,last_y;					// 必要なら、最終目的地
	} FIRE;
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
typedef	struct	_NEW_PP
	{
	short		used;						// 
	double	x,y;							//
	BOOL	cls;
	} NEW_PP;

typedef	struct	_NEW_SLCT
	{
	BOOL	sw;								// 
	short		the_slct_unit,m;			//
	double	gr_x,gr_y;						// グランドX，Ｙ
	} NEW_SLCT;

typedef	struct	_NEW_MENU
	{
	short	menu;							// これがｓｗの代わり
	short	the_slct_unit;
	} NEW_MENU;

typedef struct _KUMO
	{
	short				used;					// 使用してるかしてないか
	double				x,y;					// 地図上の位置
	int					kind;					// 戦艦だとか空母だとか
	} KUMO;

typedef	struct	_SPRT
	{
	int		no;
	int		x,y,cx,cy;					
	int		wd,ht,base_x,base_y;	
	int		os_of_x;				
	} SPRT;






// Change compiler pack alignment to be BYTE aligned, and pop the current value
#pragma pack( push, 1 )

//struct GAMEMSG_GENERIC
//struct _GENERICMSG
//{
    //DWORD dwType;
//};


typedef struct _GENERICMSG
	{
//	BYTE        byType;
	DWORD			dwType;
	} GENERICMSG, *LPGENERICMSG;


typedef struct _UNIT_MSG
	{
    BYTE		byType;
	int			used;							// 
	double		x,y;							//
	BOOL		cls;
	} UNIT_MSG;



typedef	struct	//_DP_DATA_1
	{
	DWORD	dwType;
	char	my_name[16];
	short	data[10];
	} _DP_DATA_1;



typedef	struct //_DP_NEW_PP
	{
	DWORD	dwType;

	// NEW PP
	BYTE	used;							// 
	short	x;							//
	short	y;							//
	BOOL	cls;
	BYTE	slct_unit[USA_PLANE_END/2];
	}_DP_NEW_PP;


typedef	struct	//__DP_NEW_PP_SHIP
	{
	DWORD	dwType;

	// NEW PP
	BYTE	used;							// 
	short	x;							//
	short	y;							//
	BOOL	cls;
	BYTE	slct_unit[JPN_SHIP_END/*20*/];
	} _DP_NEW_PP_SHIP;


typedef	struct	//__DP_NEW_PP_PLANE
	{
	DWORD	dwType;

	// NEW PP
	BYTE	used;							// 
	short	x;							//
	short	y;							//
	BOOL	cls;
	BYTE	slct_unit[JPN_PLANE_END-USA_SHIP_END/*30*/];
	} _DP_NEW_PP_PLANE;



typedef	struct	//__DP_NEW_SLCT
	{
	DWORD	dwType;

	// NEW SLCT
	BOOL	sw;								// 
	BYTE	the_slct_unit,m;				//
	short	gr_x,gr_y;						// グランドX，Ｙ
	BYTE	slct_unit[USA_PLANE_END/2/*50*/];
	} _DP_NEW_SLCT;

typedef	struct	//__DP_NEW_SLCT_SHIP
	{
	DWORD	dwType;

	// NEW SLCT
	BOOL	sw;								// 
	BYTE	the_slct_unit,m;				//
	short	gr_x,gr_y;						// グランドX，Ｙ
	BYTE	slct_unit[JPN_SHIP_END/*20*/];
	} _DP_NEW_SLCT_SHIP;



typedef	struct	//__DP_NEW_SLCT_PLANE
	{
	DWORD	dwType;

	// NEW SLCT
	BOOL	sw;								// 
	BYTE	the_slct_unit,m;				//
	short	gr_x,gr_y;						// グランドX，Ｙ
	BYTE	slct_unit[JPN_PLANE_END-USA_SHIP_END/*30*/];
	} _DP_NEW_SLCT_PLANE;


typedef	struct	//__DP_NEW_SLCT_LAND
	{
	DWORD	dwType;

	// NEW SLCT
	BOOL	sw;								// 
	BYTE	the_slct_unit,m;				//
	short	gr_x,gr_y;						// グランドX，Ｙ
	} _DP_NEW_SLCT_LAND;





typedef	struct	//__DP_NEW_MENU
	{
	DWORD	dwType;

	// NEW MENU
	BYTE		menu;								// これがｓｗの代わり
	BYTE		the_slct_unit;	

	BYTE	slct_unit[USA_PLANE_END/2/*50*/];
	} _DP_NEW_MENU;




typedef	struct	//__DP_FLAG
	{
	DWORD	dwType;


	BYTE	cc_chk;
	BYTE	unit_chk;
	BYTE	rnd_chk;

	BYTE	ccc_wait_chk;

	short	rival_mode;					// お互いのモードを飛ばす

	} _DP_FLAG;





typedef	struct	//_DP_DATA_20
	{
	DWORD	dwType;

	char	friend_chat[MAX_PATH/*128*/];

	} _DP_DATA_20;


#if 0
typedef	struct	//_DP_SNRO_SEND
	{
	DWORD	dwType;

	BYTE		cmbt_map[180][240];					// マップ
	UNIT		unit[USA_PLANE_END+1];
	BYTE		rein[3];

	} _DP_SNRO_SEND;
#endif


// Pop the old pack alignment
#pragma pack( pop )









