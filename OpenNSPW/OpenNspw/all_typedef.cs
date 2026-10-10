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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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

[Original("UNIT")]
[StructLayout(LayoutKind.Sequential)]
public struct	Unit		// 全ての艦船、航空機、地図上の位置
	{
	[Original("used")] public Side Side; // 使用してるかしてないか オンならその国籍
	[Original("x", "y")] public WorldPosition Position; // 地図上の位置
	[Original("ctgry")] public UnitCategory Category; // カテゴリー（船とか飛行機とかの）
	[Original("kind")] public UnitKind Kind; // 戦艦だとか空母だとか
	[Original("type")] public short Variant; // 形式
	public Array16<int> info; // 追加の情報、航空機なら飛んでるとか、格納庫の中とか
	[Original("os_indx_y")] public int SpriteRow; // 各種パターンの頭の位置（ソースサーフェス）
	[Original("os_indx_x")] public int SpriteColumn; // ユニットの向き
	[Original("drctn")] public double Direction; // 進行角度 変進角度
	[Original("drctn_add")] public double TurnRate;
	[Original("a_drctn_add")] public double TurnRateChange;
	[Original("spd")] public double Speed; // スピード 速度変更
	[Original("spd_add")] public double Acceleration;
	[Original("a_spd_add")] public double AccelerationChange;
	[Original("min_spd")] public double MinSpeed;
	[Original("max_spd")] public double MaxSpeed;
	[Original("stop")] public Bool32 IsStopping; // オンで方向変えず、減速のみ
	// 0 if the unit is not being supplied; otherwise the ticks counted up from 1, and back to 1 after each repair or refill.
	[Original("spry")] public int SupplyTime; // 補給と修理
	[Original("mark")] public Bool32 IsMarked; // 選択されているか
	[Original("pp_x")] public Array64<double> PathX; // 移動目的地の地図上の位置
	[Original("pp_y")] public Array64<double> PathY;
	[Original("em_flg")] public Array2<int> EmergencyFlags; // 緊急時の移動の処理フラグ
	[Original("em_x", "em_y")] public WorldPosition EmergencyDestination; // 緊急時の移動目的地の地図上の位置
	[Original("to_ldr_drctn")] public double DirectionToLeader; // 主に艦隊時、リーダとの相対位置。
	[Original("to_ldr_dstc")] public double DistanceToLeader;
	[Original("is_ltl_ldr")] public short IsGroupLeader; // 一時的指揮機番号、何番機
	[Original("ltl_ldr")] public short GroupLeader;
	[Original("no")] public short FormationNumber;
	[Original("for_ltl_ldr")] public short ForGroupLeader;
	[Original("for_form_spd")] public double FormationSpeed; // 編隊を組み場合の遅れているユニットの速度
	public Array8<int> hp; // いわゆるヒットポイント
	public Array8<int> arm; // 武装
	[Original("arm2")] public Array2<int> SubWeapons; // サブ武装
	public Array8<double> gas; // 燃料
	[Original("found")] public Bool32 IsFound; // 相手サイドからの可視不可視
	[Original("tech")] public int Skill; // そのユニットの技量

	[Original("rnd_250")] public Array2<short> Random250; // 0-99までの乱数
	[Original("rnd_225")] public Array2<short> Random225; // 0-99までの乱数
	[Original("rnd_200")] public Array2<short> Random200; // 0-99までの乱数
	[Original("rnd_175")] public Array2<short> Random175; // 0-99までの乱数
	[Original("rnd_150")] public Array2<short> Random150; // 0-99までの乱数
	[Original("rnd_125")] public Array2<short> Random125; // 0-99までの乱数

	[Original("rnd_100")] public Array2<short> Random100; // 0-99までの乱数
	[Original("rnd_80")] public Array2<short> Random80; //
	[Original("rnd_65")] public Array2<short> Random65; //
	[Original("rnd_50")] public Array2<short> Random50; //
	[Original("rnd_40")] public Array2<short> Random40; //
	[Original("rnd_30")] public Array2<short> Random30; //
	[Original("rnd_20")] public Array2<short> Random20; //
	[Original("rnd_10")] public Array2<short> Random10; //

	// Whether the unit is in the battle: an unused unit has no side.
	public readonly bool IsUsed => Side != Side.None;

	// Whether the unit is being supplied and repaired.
	public readonly bool IsSupplying => SupplyTime != 0;

	// The slots of hp, arm and gas by meaning (docs/Refactoring.md, Union slots). They are references to the slots, so
	// that they can be changed and passed by ref like the slots.

	// The slots of info whose meaning depends on the unit's category or kind.

	// Planes: flying or parked.
	[UnscopedRef] public ref UnitState PlaneState => ref Unsafe.As<int, UnitState>(ref info[0]);

	// Bases: 建設期間, the time until the base is built, or 0 once it is.
	[UnscopedRef] public ref int BuildTime => ref info[0];

	// Planes: 所属の空母、及び、基地の番号, the number of the carrier or air base the plane belongs to.
	[UnscopedRef] public ref int Carrier => ref info[1];

	// Carriers and air bases: 現在収容数, the planes aboard, with those on the flight deck.
	[UnscopedRef] public ref int PlaneCount => ref info[1];

	// Planes: 格納庫の位置, the plane's place in its carrier or base.
	[UnscopedRef] public ref int ParkingNumber => ref info[2];

	// Planes: 格納庫、基地での移動情態, the plane's step on its carrier or base: 1 to 4 and on while it taxies to take
	// off, 1 (着艦準備, ready to land) to 3 while it lands, and 0 otherwise.
	[UnscopedRef] public ref int DeckPhase => ref info[3];

	// Carriers and air bases: 最大収容数.
	[UnscopedRef] public ref int Capacity => ref info[2];

	// Carriers and air bases: 発艦予定の機数, the planes about to take off; while it is not 0, no plane can land.
	[UnscopedRef] public ref int PlanesToLaunch => ref info[4];

	// Planes: how far the plane has run along the flight deck to take off, up to 40.
	[UnscopedRef] public ref int TakeOffRun => ref info[4];

	// How the unit moves.
	[UnscopedRef] public ref UnitMode Mode => ref Unsafe.As<int, UnitMode>(ref info[5]);

	// Submarines: true while submerged (潜航中).
	[UnscopedRef] public ref Bool32 IsSubmerged => ref Unsafe.As<int, Bool32>(ref info[6]);

	// Transports: 揚陸座標, where the transport lands its cargo.
	[UnscopedRef] public ref int LandingX => ref info[6];

	[UnscopedRef] public ref int LandingY => ref info[7];

	// Where the other side last made contact with the unit, how far around it the contact is drawn, and how long it
	// lasts: Detect counts ContactTime down, and the unit is no longer found when it reaches 0.
	[UnscopedRef] public ref int ContactX => ref info[7];

	[UnscopedRef] public ref int ContactY => ref info[8];

	[UnscopedRef] public ref int ContactRadius => ref info[9];

	[UnscopedRef] public ref int ContactTime => ref info[10];

	// Fighters: 制空出撃フラグ, sent out for air superiority. Only cleared, when the fighter has landed; it is the slot
	// of ContactRadius.
	[UnscopedRef] public ref int AirSuperioritySortie => ref info[9];

	// Carriers and air bases: 着艦, 0 if the next plane may land, 1 if not.
	[UnscopedRef] public ref int LandingLock => ref info[7];

	// Carriers and air bases: その空母の次機発進許可, 0 if the next plane may take off.
	[UnscopedRef] public ref int LaunchLock => ref info[8];

	// Carriers: the planes launched, whose parity picks the side of the deck the next one takes off from.
	[UnscopedRef] public ref int LaunchCount => ref info[11];

	// いわゆるヒットポイント
	[UnscopedRef] public ref int Hp => ref hp[0];

	[UnscopedRef] public ref int MaxHp => ref hp[1];

	// 武装: the kind of weapon, viewed as a FireKind.
	[UnscopedRef] public ref FireKind Weapon => ref Unsafe.As<int, FireKind>(ref arm[0]);

	[UnscopedRef] public ref int Ammo => ref arm[1];

	// The number of the unit this one attacks, or 0.
	[UnscopedRef] public ref int Target => ref arm[2];

	// 再装填時間: the time until the weapon is ready again, also a plane's maintenance time.
	[UnscopedRef] public ref int ReloadTime => ref arm[3];

	// 全容量
	[UnscopedRef] public ref int MaxAmmo => ref arm[4];

	// 燃料
	[UnscopedRef] public ref double Fuel => ref gas[0];

	// 燃料を消費するタイミング: how often fuel is used.
	[UnscopedRef] public ref double FuelInterval => ref gas[1];
	}

[Original("EFFECT")]
[StructLayout(LayoutKind.Sequential)]
public struct	Effect							// 雷跡とか爆炎とか
	{
	[Original("used")] public Side Side; // 自サイド
	[Original("layer")] public EffectLayer Layer; // 使用してるかしてないか、アッパーかローワーか
	[Original("kind")] public int Kind;
	[Original("no")] public int SpriteNumber; // Sprite nuber of its Sprite Source
	public Array8<int> info; // 追加の情報、
	[Original("x", "y")] public WorldPosition Position; // 地図上の位置
	[Original("x2", "y2")] public WorldPosition EndPosition; // ＢＬＴ や ＲＡＳ
	[Original("found")] public Bool32 IsVisible; // 自サイド	からの可視、不可視
	//BOOL				side;					//

	// The ticks left until the effect ends.
	[UnscopedRef] public ref int TimeLeft => ref info[0];

	// アニメーションパターン: how the effect is drawn.
	[UnscopedRef] public ref int Animation => ref info[1];
	}

[Original("FIRE")]
[StructLayout(LayoutKind.Sequential)]
public struct	Fire
	{
	[Original("used")] public int Target; // オン、オフ。オンなら、ターゲットのユニット番号（ＢＬＴに必要）
	[Original("kind")] public FireKind Kind; // 弾丸(BLT)、爆弾(BOM)、魚雷(TPD)、炸裂弾(SHL)、ＶＴ信管(VTH)だとか、、
	public Array9<int> info;
	[Original("no")] public int SpriteNumber; // Sprite nuber of its Sprite Source
	[Original("x", "y")] public WorldPosition Position;
	[Original("drctn")] public double Direction;
	[Original("spd")] public double Speed;
	[Original("spd_add")] public double Acceleration;
	[Original("last_spd")] public double FinalSpeed;
	[Original("last_x", "last_y")] public WorldPosition Destination; // 必要なら、最終目的地

	// The ticks the fire has flown, or has left to fly, by its kind.
	[UnscopedRef] public ref int Ticks => ref info[0];

	// The ticks the fire flies before it lands, bursts or hits, which Ticks counts up to.
	[UnscopedRef] public ref int FlightTime => ref info[1];

	// Torpedoes: the ticks before the torpedo can hit.
	[UnscopedRef] public ref int ArmingTime => ref info[2];

	// Cargo: where the transport lands it.
	[UnscopedRef] public ref int TargetX => ref info[6];

	[UnscopedRef] public ref int TargetY => ref info[7];

	// The side of the unit that fired, as an int.
	[UnscopedRef] public ref int ShooterSide => ref info[8];
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
[Original("NEW_PP")]
[StructLayout(LayoutKind.Sequential)]
public struct	MoveOrder
	{
	[Original("used")] public short Unit; //
	[Original("x", "y")] public WorldPosition Destination; //
	[Original("cls")] public int ClearsPath;
	}

[Original("NEW_SLCT")]
[StructLayout(LayoutKind.Sequential)]
public struct	SelectOrder
	{
	[Original("sw")] public Bool32 IsSet; //
	[Original("the_slct_unit")] public short SelectedUnit; //
	[Original("m")] public short Unit;
	[Original("gr_x", "gr_y")] public WorldPosition GroundPosition; // グランドX，Ｙ
	}

[Original("NEW_MENU")]
[StructLayout(LayoutKind.Sequential)]
public struct	MenuOrder
	{
	[Original("menu")] public CombatMenuItem Menu; // これがｓｗの代わり
	[Original("the_slct_unit")] public short SelectedUnit;
	}

[Original("KUMO")]
[StructLayout(LayoutKind.Sequential)]
public struct Cloud
	{
	[Original("used")] public short Used; // 使用してるかしてないか
	[Original("x", "y")] public WorldPosition Position; // 地図上の位置
	[Original("kind")] public int Kind; // 戦艦だとか空母だとか
	}

[Original("SPRT")]
[StructLayout(LayoutKind.Sequential)]
public struct	Sprite
	{
	[Original("no")] public int Frame;
	[Original("x")] public int X;
	[Original("y")] public int Y;
	[Original("cx")] public int CenterX;
	[Original("cy")] public int CenterY;
	[Original("wd")] public int Width;
	[Original("ht")] public int Height;
	[Original("base_x")] public int SheetX;
	[Original("base_y")] public int SheetY;
	[Original("os_of_x")] public int FramesPerRow;
	}

// Change compiler pack alignment to be BYTE aligned, and pop the current value
//#pragma pack( push, 1 )

//struct GAMEMSG_GENERIC
//struct _GENERICMSG

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GENERICMSG
	{
	public MessageType dwType;
	}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct UNIT_MSG
	{
    public byte byType;
	public int used; //
	public double x; //
	public double y;
	public int cls;
	}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct	_DP_DATA_1
	{
	public MessageType dwType;
	public Array16<byte> my_name;
	public Array10<short> data;
	}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct _DP_NEW_PP
	{
	public MessageType dwType;

	// NEW PP
	public byte used; //
	public short x; //
	public short y; //
	public int cls;
	public Array90<byte> slct_unit; // [USA_PLANE_END/2]
	}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct	_DP_NEW_PP_SHIP
	{
	public MessageType dwType;

	// NEW PP
	public byte used; //
	public short x; //
	public short y; //
	public int cls;
	public Array40<byte> slct_unit; // [JPN_SHIP_END/*20*/]
	}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct	_DP_NEW_PP_PLANE
	{
	public MessageType dwType;

	// NEW PP
	public byte used; //
	public short x; //
	public short y; //
	public int cls;
	public Array50<byte> slct_unit; // [JPN_PLANE_END-USA_SHIP_END/*30*/]
	}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct	_DP_NEW_SLCT
	{
	public MessageType dwType;

	// NEW SLCT
	public int sw; //
	public byte the_slct_unit; //
	public byte m;
	public short gr_x; // グランドX，Ｙ
	public short gr_y;
	public Array90<byte> slct_unit; // [USA_PLANE_END/2/*50*/]
	}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct	_DP_NEW_SLCT_SHIP
	{
	public MessageType dwType;

	// NEW SLCT
	public int sw; //
	public byte the_slct_unit; //
	public byte m;
	public short gr_x; // グランドX，Ｙ
	public short gr_y;
	public Array40<byte> slct_unit; // [JPN_SHIP_END/*20*/]
	}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct	_DP_NEW_SLCT_PLANE
	{
	public MessageType dwType;

	// NEW SLCT
	public int sw; //
	public byte the_slct_unit; //
	public byte m;
	public short gr_x; // グランドX，Ｙ
	public short gr_y;
	public Array50<byte> slct_unit; // [JPN_PLANE_END-USA_SHIP_END/*30*/]
	}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct	_DP_NEW_SLCT_LAND
	{
	public MessageType dwType;

	// NEW SLCT
	public int sw; //
	public byte the_slct_unit; //
	public byte m;
	public short gr_x; // グランドX，Ｙ
	public short gr_y;
	}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct	_DP_NEW_MENU
	{
	public MessageType dwType;

	// NEW MENU
	public byte menu; // これがｓｗの代わり
	public byte the_slct_unit;

	public Array90<byte> slct_unit; // [USA_PLANE_END/2/*50*/]
	}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct	_DP_FLAG
	{
	public MessageType dwType;

	public byte cc_chk;
	public byte unit_chk;
	public byte rnd_chk;

	public byte ccc_wait_chk;

	public short rival_mode; // お互いのモードを飛ばす

	}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct	_DP_DATA_20
	{
	public MessageType dwType;

	public Array260<byte> friend_chat; // [MAX_PATH/*128*/]

	}

// Pop the old pack alignment
//#pragma pack( pop )
