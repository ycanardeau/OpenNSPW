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

// Port of etc2.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{

private void SetUnitedStatesArrivalPoint(ref double x, ref double y, ref double destinationX, ref double destinationY, int reinforcement, Side arrivingSide)
	{
	switch(CurrentMap)
		{
		case 0:		// 南太平洋
			x=MAP_RIGHT-Random(1200)-100;
			y=MAP_BOTTOM-10;
			destinationX=x-200;
			destinationY=y+500;
y-=reinforcement*80;
			break;

		case 1:		// 中部太平洋
			x=MAP_RIGHT+10;
			y=Random(1200);
			destinationX=x-500;
			destinationY=y;
x+=reinforcement*80;
			break;

		case 2:		// 日本近海
			x=MAP_RIGHT-Random(1200)-100;
			y=MAP_BOTTOM-10;
			destinationX=x-200;
			destinationY=y+500;
y-=reinforcement*80;
			break;

		case 3:		// ユーザーマップ
			switch(Reinforcements[(int)arrivingSide])
				{
				case 0:
					x=MAP_LEFT-500;
					y=MAP_TOP+500;
					destinationX=x+1200;
					destinationY=y-1200-Random(500);
					y-=reinforcement*80;
					break;
				case 1:
					x=MAP_RIGHT+500;
					y=MAP_TOP+500;
					destinationX=x-1200;
					destinationY=y-1200-Random(500);
					y-=reinforcement*80;
					break;
				case 2:
					x=MAP_RIGHT+500;
					y=MAP_BOTTOM-500;
					destinationX=x-1200;
					destinationY=y+1200+Random(500);
					y+=reinforcement*80;
					break;
				case 3:
					x=MAP_LEFT-500;
					y=MAP_BOTTOM-500;
					destinationX=x+1200;
					destinationY=y+1200+Random(500);
					y+=reinforcement*80;
					break;
				}
			break;
		}
	}

private void SetJapanArrivalPoint(ref double x, ref double y, ref double destinationX, ref double destinationY, int reinforcement, Side arrivingSide)
	{
	switch(CurrentMap)
		{
		case 0:		// 南太平洋
			x=MAP_LEFT-10;
			y=MAP_TOP-Random(1200);
			destinationX=x+500;
			destinationY=y-200;
x-=reinforcement*80 ;
			break;

		case 1:		// 中部太平洋
			x=MAP_LEFT-10;
			y=MAP_BOTTOM+Random(1200);
			destinationX=x+500;
			destinationY=y+200;
x-=reinforcement*80;
			break;

		case 2:		// 日本近海
			x=MAP_LEFT+Random(1200);
			y=MAP_TOP+10;
			destinationX=x+200;
			destinationY=y-500;
y+=reinforcement*80;
			break;

		case 3:		// ユーザーマップ
			switch(Reinforcements[(int)arrivingSide])
				{
				case 0:
					x=MAP_LEFT-500;
					y=MAP_TOP+500;
					destinationX=x+1200;
					destinationY=y-1200-Random(500);
					y-=reinforcement*80;
					break;
				case 1:
					x=MAP_RIGHT+500;
					y=MAP_TOP+500;
					destinationX=x-1200;
					destinationY=y-1200-Random(500);
					y-=reinforcement*80;
					break;
				case 2:
					x=MAP_RIGHT+500;
					y=MAP_BOTTOM-500;
					destinationX=x-1200;
					destinationY=y+1200+Random(500);
					y+=reinforcement*80;
					break;
				case 3:
					x=MAP_LEFT-500;
					y=MAP_BOTTOM-500;
					destinationX=x+1200;
					destinationY=y+1200+Random(500);
					y+=reinforcement*80;
					break;
				}
			break;

		}
	}

//============================================================================
// 新ユニット登場
//----------------------------------------------------------------------------
[Original("new_unit_arrived")]
public void OnUnitArrived(int side,int reinforcement)
	{
	double		x=default /* C4701 */,y=default /* C4701 */;
	double		destinationX=default /* C4701 */,destinationY=default /* C4701 */;
	UnitKind	kind=default /* C4701 */; int m;
	Side		arrivingSide;

	if( side==1 )
		{
		arrivingSide=LocalSide;
		}
	else
		{
		if(LocalSide==Side.Japan)
			arrivingSide=Side.UnitedStates;
		else
			arrivingSide=Side.Japan;
		}

	var variant=0;

	reinforcement--;

	switch(reinforcement)
		{
		case 0:		kind=UnitKind.Battleship;		break;
		case 1:		kind=UnitKind.Cruiser;		break;
		case 2:		kind=UnitKind.Destroyer;		break;
		case 3:		kind=UnitKind.Submarine;		break;
		case 4:		kind=UnitKind.LightCarrier;		break;
		case 5:		kind=UnitKind.Carrier;		break;
		case 6:		kind=UnitKind.Fighter;		break;
		case 7:		kind=UnitKind.Attacker;		break;
		case 8:		kind=UnitKind.Bomber;		break;
		case 9:		kind=UnitKind.Fighter;	variant=1;			break;
		case 10:	kind=UnitKind.Transport;	variant=(int)FireKind.CargoInfantryBase;		break;
		case 11:	kind=UnitKind.Transport;	variant=(int)FireKind.CargoPillboxes;		break;
		case 12:	kind=UnitKind.Transport;	variant=(int)FireKind.CargoFortress;		break;
		case 13:	kind=UnitKind.Transport;	variant=(int)FireKind.CargoAirBase;		break;
		case 14:	kind=UnitKind.Transport;	variant=(int)FireKind.CargoNavalBase;			break;

		case 15:		kind=UnitKind.Cruiser;	variant=1;		break;
		case 16:		kind=UnitKind.Destroyer;	variant=1;		break;
		case 17:
			if( arrivingSide==Side.Japan )
				kind=UnitKind.Battleship;
			else
				kind=UnitKind.Carrier;
			variant=1;
			break;

		}

	if(arrivingSide==Side.Japan)
		{
		SetJapanArrivalPoint(ref x, ref y, ref destinationX, ref destinationY, reinforcement, arrivingSide);
		}
	else
		{
		SetUnitedStatesArrivalPoint(ref x, ref y, ref destinationX, ref destinationY, reinforcement, arrivingSide);
		}

	if( reinforcement<=5 || reinforcement>=10 )
		{
		// 艦船の登場

		if( arrivingSide==Side.Japan )
			m=AddUnit2(Side.Japan,kind,variant,x,y,(double)90);
		else
			m=AddUnit2(Side.UnitedStates,kind,variant,x,y,(double)90);

		if(kind==UnitKind.Transport)
			{
			// 輸送船の場合、積荷
			Units[m].Weapon=(FireKind)variant;		// 武装品種
			Units[m].Ammo=1;			// 数
			Units[m].MaxAmmo=1;			// 数 全容量
			}
		else
			{
			Units[m].Ammo=Units[m].MaxAmmo/4;			// 弾薬搭載量
			Units[m].Fuel=50;
			}
		}
	else
		{
		// 航空機の登場

		for(var i=0;i<((kind==UnitKind.Attacker ? 1 : 0)*3)+((kind==UnitKind.Fighter ? 1 : 0)*2)+((kind==UnitKind.Bomber ? 1 : 0)*1)  ;i++)
			{
			if( arrivingSide==Side.Japan )
				m=AddUnit2(Side.Japan,kind,variant,x,y,(double)90);
			else
				m=AddUnit2(Side.UnitedStates,kind,variant,x,y,(double)90);

			Units[m].SetPathPoint(0, new WorldPosition(destinationX, destinationY));
			Units[m].PathX[1]=MAP_RIGHT+1;

			Units[m].PlaneState=UnitState.Flying;
			Units[m].Speed=1.5;

			Units[m].Mode=UnitMode.Move;				// モード（コンバットメニュー）

			Units[m].Fuel=50;
			if(kind==UnitKind.Fighter)
				Units[m].Ammo=Units[m].Ammo/3;
			else
				Units[m].Ammo=0;
			//unit[m].arm[1]=unit[m].arm[4]/4;			// 弾薬搭載量

			x+=-160+Random(320);
			y+=-160+Random(320);
			destinationX+=-100+Random(200);
			destinationY+=-100+Random(200);
			}
		}

	}

// How far a unit of a kind sees in 1.22, as find_out_size (0x418080) and the drawing of the effects (0x40C919) have it.
public static int GetSightRangeAs122(UnitKind kind) => kind switch
	{
	UnitKind.Battleship => 900,
	UnitKind.Cruiser or UnitKind.City or UnitKind.Mine => 800,
	UnitKind.Destroyer or UnitKind.LightCarrier => 700,
	UnitKind.Submarine or UnitKind.Carrier => 600,
	UnitKind.Fighter => 500,
	UnitKind.Attacker or UnitKind.InfantryBase => 1000,
	UnitKind.Bomber => 1200,
	UnitKind.Transport => 300,
	UnitKind.AirBase or UnitKind.NavalBase => 1700,
	UnitKind.Pillboxes => 1500,
	UnitKind.Fortress => 2000,
	_ => 0,
	};

// 1.22's find_out_size (0x418080): other ranges, none limited while a base is built, and, for a submerged submarine,
// 80 for any unit but a destroyer (none for a supplying unit, a plane, a transport, a carrier, a light carrier or a
// battleship), 280 for a destroyer, 140 above a third of its top speed; a surfaced one within half the range, at most
// 300, also while it supplies.
private int GetDetectionSizeAs122(int m, int n)
	{
	ref var unit = ref Units[m];
	ref var other = ref Units[n];
	int size = GetSightRangeAs122(unit.Kind);

	if( other.Kind!=UnitKind.Submarine )
		return size;

	if( !other.IsSubmerged )
		return Math.Min((int)(size*0.5), 300);

	if( unit.Kind==UnitKind.Destroyer )
		{
		size=( unit.MaxSpeed*(1.0/3) < unit.Speed ? 140 : 280 );
		if( other.Speed==0 )
			size+=50;
		return size;
		}

	// 1.22 also compares the category with 11 to 17, the kinds of the bases, which it never is.
	if( unit.IsSupplying || unit.Category==UnitCategory.Plane || unit.Kind is UnitKind.Transport or UnitKind.Carrier or UnitKind.LightCarrier or UnitKind.Battleship )
		return 0;

	return 80;
	}

//============================================================================
// 可視不可視のサイズ設定
//----------------------------------------------------------------------------
[Original("find_out_size")]
public int		GetDetectionSize( int m , int n)
	{
	if( Rules.HasSightRangesOf122 )
		return GetDetectionSizeAs122(m, n);

	ref var unit = ref Units[m];
	int		size=0 /* C4701 */;

	// 見る側の追加
	switch( unit.Kind )
		{
		case UnitKind.Battleship:		size=BB1_SIGHT;		break;
		case UnitKind.Cruiser:		size=CA1_SIGHT;		break;
		case UnitKind.Destroyer:		size=DD1_SIGHT;		break;
		case UnitKind.Submarine:		size=SS1_SIGHT;		break;
		case UnitKind.Carrier:		size=CV1_SIGHT;		break;
		case UnitKind.LightCarrier:		size=CVL1_SIGHT;	break;
		case UnitKind.Transport:		size=TR1_SIGHT;		break;
		case UnitKind.Fighter:		size=FT1_SIGHT;		break;
		case UnitKind.Attacker:		size=AT1_SIGHT;		break;
		case UnitKind.Bomber:		size=BM1_SIGHT;		break;
		case UnitKind.AirBase: case UnitKind.NavalBase:		size=AP_SIGHT;		break;
		case UnitKind.City:		size=CT1_SIGHT;		break;
		case UnitKind.Mine:		size=MN1_SIGHT;		break;
		case UnitKind.InfantryBase:		size=GF1_SIGHT;		break;
		case UnitKind.Pillboxes:		size=GF2_SIGHT;		break;
		case UnitKind.Fortress:		size=GF3_SIGHT;		break;
		}

	if( unit.Kind>=UnitKind.AirBase && unit.Kind<=UnitKind.Fortress && unit.BuildTime!=0 )
		{
		// 工事中は視界を制限
		size=FT1_SIGHT/2;
		}

	// 見られる側の追加
	switch( Units[n].Kind )
		{
		case UnitKind.Battleship:							break;
		case UnitKind.Cruiser:				break;
		case UnitKind.Destroyer:				break;
		case UnitKind.Submarine:
			if( Units[n].IsSubmerged )
				{	// 潜航中
				if( unit.Kind!=UnitKind.Destroyer )
					{
					if(unit.Kind==UnitKind.NavalBase && unit.BuildTime==0 )
						size=(int)(SP_SIGHT*0.8);
					else
						size=0;
					}
				else
					{	// 駆逐艦
					if( unit.Speed<=unit.MaxSpeed/3 )
						{
						size=( unit.Variant==1 ? 380 : 280);
						}
					else
						{
						size=( unit.Variant==1 ? 270 : 140 );
						}
					if( Units[n].Speed==0 )
						size+=50;		// 潜水艦の速度によって
					}
				}
			else if( !Units[n].IsSupplying )
				{	// 浮上航行
				size=(int)(size*0.5);
				if( size>300 )
					size=300;		// 潜水艦の見える範囲より小さくする
				}
				break;
		case UnitKind.Carrier:				break;
		case UnitKind.LightCarrier:				break;
		case UnitKind.Fighter: case UnitKind.Attacker:	case UnitKind.Bomber:
			// 航空機は固まっていると見つかりやすい
			break;
		case UnitKind.AirBase: case UnitKind.NavalBase:
			break;
		case UnitKind.Transport:
			break;
		}

	return (size);
	}

//============================================================================
//絶対スクリーン座標に線を描画する。色付き
//----------------------------------------------------------------------------
[Original("draw_line4")]
public void	DrawLine4(int x1,int y1,int x2,int y2,int right,int bottom,uint color)
	{

	DrawLine5( x1, y1, x2, y2, right, bottom , 0xFFFF);

/***
	int		dstX, dstY,addX,addY;
	int		ctr,x,y,i;
	int		cl1, cl2;
    DDCOLORKEY          ddck;

	ddck.dwColorSpaceLowValue  = 0xff;

	//書き込むＶＲＡＭのアドレスを得る（ロックして書き込めるようにする）
	memset(&dst_ddsd, 0, sizeof(DDSURFACEDESC));
	dst_ddsd.dwSize = sizeof(DDSURFACEDESC);
	IDirectDrawSurface_Lock( lpDDSBack, NULL, (LPDDSURFACEDESC)&dst_ddsd, DDLOCK_WAIT, NULL );
	dst_vram=dst_ddsd.lpSurface;

	dstX=x2-x1;	dstY=y2-y1;

	if(dstX<0)	{addX=-1;	dstX*=-1;}	else	addX=1;
	if(dstY<0)	{addY=-1;	dstY*=-1;}	else	addY=1;

	ctr=0;
	x=x1;	y=y1;
	if(dstX>=dstY)
		{
		for(i=0;i<dstX;i++)
			{
			if( x>=0 && y>=0 && x<=right && y<=bottom )
				{
				dst_vram[y*dst_ddsd.lPitch+(x)]=ddck.dwColorSpaceLowValue;
				}

			x+=addX;
			ctr+=dstY;
			if(ctr>=dstX)
				{
				y+=addY;
				ctr-=dstX;
				}
			}
		}
	else
		{
		for(i=0;i<dstY;i++)
			{
			if( x>=0 && y>=0 && x<=right && y<=bottom )
				{
				dst_vram[y*dst_ddsd.lPitch+(x)]=ddck.dwColorSpaceLowValue;
				}

			y+=addY;
			ctr+=dstX;
			if(ctr>=dstY)
				{
				x+=addX;
				ctr-=dstY;
				}
			}
		}
	IDirectDrawSurface_Unlock( lpDDSBack, NULL );
***/
	}

//============================================================================
//絶対スクリーン座標に線を描画する。色付き
//----------------------------------------------------------------------------
[Original("draw_line5")]
public void	DrawLine5(int x1,int y1,int x2,int y2,int right,int bottom,ushort color)
	{
	int stepX, stepY;
	ushort* vram;			//書き込むＶＲＡＭのアドレス

	DDSURFACEDESC2		surface;

color=0xFFFF;

	//書き込むＶＲＡＭのアドレスを得る（ロックして書き込めるようにする）
	memset(&surface, 0, (nuint)(sizeof(DDSURFACEDESC2)));
	surface.dwSize = (uint)(sizeof(DDSURFACEDESC2));
	IDirectDrawSurface_Lock( lpDDSBack, null, &surface, DDLOCK_WAIT, null );

	vram=(ushort*)surface.lpSurface;

	var dx=x2-x1;	var dy=y2-y1;

	if(dx<0)	{stepX=-1;	dx*=-1;}	else	stepX=1;
	if(dy<0)	{stepY=-1;	dy*=-1;}	else	stepY=1;

	var error=0;
	var x=x1;	var y=y1;
	if(dx>=dy)
		{
		for(var i=0;i<dx;i++)
			{
			if( x>=0 && y>=0 && x<=right && y<=bottom )
				{
				vram[y*(surface.lPitch/2)+(x)]=color;
				}

			x+=stepX;
			error+=dy;
			if(error>=dx)
				{
				y+=stepY;
				error-=dx;
				}
			}
		}
	else
		{
		for(var i=0;i<dy;i++)
			{
			if( x>=0 && y>=0 && x<=right && y<=bottom )
				{
				vram[y*(surface.lPitch/2)+(x)]=color;
				}

			y+=stepY;
			error+=dx;
			if(error>=dy)
				{
				x+=stepX;
				error-=dy;
				}
			}
		}

	IDirectDrawSurface_Unlock( lpDDSBack, null );

	}

public void DrawLine4(PixelPoint from, PixelPoint to, int right, int bottom, uint color)
	{
	DrawLine4(from.X, from.Y, to.X, to.Y, right, bottom, color);
	}

public void DrawLine5(PixelPoint from, PixelPoint to, int right, int bottom, ushort color)
	{
	DrawLine5(from.X, from.Y, to.X, to.Y, right, bottom, color);
	}
//============================================================================
//
//----------------------------------------------------------------------------
[Original("pt_in_rect")]
public int	PointInRect(ref RECT rect,int x,int y)
	{
	if( rect.top <= y && rect.bottom >= y &&
		rect.right >= x && rect.left <= x)
		return 1;
	return 0;
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("pt_in_rect2")]
public int	PointInRect2(ref RECT rect,int x,int y)
	{
	if( rect.top >= y && rect.bottom <= y &&
		rect.right >= x && rect.left <= x)
		return (1);
	return (0);
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("pt_in_rect3")]
public int	PointInRect3(ref RECT rect,int x,int y)
	{
	if( rect.top >= y && rect.bottom <= y &&
		rect.right >= x && rect.left <= x)
		{
		return (1);
		}
	return (0);
	}

//					セイムレクト
//
//==========================================================================================
//
[Original("same_rect")]
public int	ClipRects(ref RECT destination, ref RECT source, ref RECT field)
	{
	// field_rectと一致するdstn_rectを見つけ出し。その差分をsrc_rectに反映させます。

	if( destination.top <= field.bottom )
		{
		if(destination.top < field.top )
			{
			source.top+=(field.top-destination.top);
			destination.top=field.top;
			}
		}
	else
		{
		return (0);
		}

	if( destination.bottom >= field.top )
		{
		if(destination.bottom > field.bottom )
			{
			source.bottom-=(destination.bottom-field.bottom);
			destination.bottom=field.bottom;
			}
		}
	else
		{
		return (0);
		}

	if( source.top==source.bottom )
		return (0);

	if( destination.left <= field.right )
		{
		if(destination.left < field.left )
			{
			source.left+=(field.left-destination.left);
			destination.left=field.left;
			}
		}
	else
		{
		return (0);
		}

	if( destination.right >= field.left )
		{
		if(destination.right > field.right )
			{
			source.right-=(destination.right-field.right);
			destination.right=field.right;
			}
		}
	else
		{
		return (0);
		}

	if( source.left==source.right )
		return (0);

	return (1);
	}

//============================================================================
// その空母の現在の開きスペースを代えします。
//----------------------------------------------------------------------------
[Original("seek_parking_no")]
public int		FindParkingNumber( int m )
	{
	int number=0 /* C4701 */ /* C4701 */; Array32<int> planes=default;

	for(var i=0;i<=31;i++)
		planes[i]=0;

	for( var i=1; i<=MaxUnitId; i++)
		{
		ref var unit = ref Units[i];
		if( unit.IsUsed && unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked
			&& Units[m].Carrier==unit.Carrier )
			planes[unit.ParkingNumber]++;
		}

	for(var i=0;i<=31;i++)
		if(planes[i]==0)
			{
			number=i;
			break;
			}

	return	(number);
	}

//============================================================================
// 現在収容数(着艦予定、飛行甲板上数も含む)
//----------------------------------------------------------------------------
[Original("plane_in_cv")]
public int		CountPlanesIn( int m )
	{

	var count=0;
	for(var i=1;i<=MaxUnitId;i++)
		{
		ref var unit = ref Units[i];
		if( unit.IsUsed && unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked && m==unit.Carrier )
			count++;
		}
	return (count);
	}

//============================================================================
// 駐機場、格納庫の停止位置を求めます。
//----------------------------------------------------------------------------
[Original("set_pos_of_parking")]
public void	SetParkingPosition( int	m )
	{
	ref var unit = ref Units[m];
	int offset;
	int		x,y;

	// ２列格納
	if( Units[unit.Carrier].Kind==UnitKind.AirBase )
		{	// 陸上基地
		y=Sprites[SpriteId.JapanUnitInfo].Position.Y+Sprites[SpriteId.JapanUnitInfo].Size.Height-((unit.ParkingNumber/2)*40)-100;
		if( false && unit.Kind==UnitKind.Bomber )
			offset=55;
		else
			offset=35;
		if( unit.ParkingNumber%2!=0 )
			{ // 右側
			x=Sprites[SpriteId.JapanUnitInfo].Position.X+Sprites[SpriteId.JapanUnitInfo].Size.Width/2-offset;
			unit.Direction=270+45;
			y-=18;
			}
		else
			{ // 左側
			x=Sprites[SpriteId.JapanUnitInfo].Position.X+Sprites[SpriteId.JapanUnitInfo].Size.Width/2+offset;
			unit.Direction=180+45;
			}
		}
	else
		{	// 航空母艦
		y=Sprites[SpriteId.JapanUnitInfo].Position.Y+Sprites[SpriteId.JapanUnitInfo].Size.Height-((unit.ParkingNumber/2)*40)-100;
		if( unit.ParkingNumber%2!=0 )
			{ // 右側
			x=Sprites[SpriteId.JapanUnitInfo].Position.X+Sprites[SpriteId.JapanUnitInfo].Size.Width/2-25;
			unit.Direction=270+45;
			y-=18;
			}
		else
			{ // 左側
			x=Sprites[SpriteId.JapanUnitInfo].Position.X+Sprites[SpriteId.JapanUnitInfo].Size.Width/2+25;
			unit.Direction=180+45;
			}
		}

	unit.Position = new WorldPosition((double)x, (double)y);
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("set_the_slct_unit")]
public void	SelectUnit(int m)
	{

	if( Units[m].Side!=LocalSide )
		return;

	BufferedMoveOrders[1].ClearsPath=1;

	MoveOrders[1].ClearsPath=1;

	SelectedUnit=(short)m; SelectionCount=0; Selections[1][m]=1; CombatMenuKind=1; WorkPathX[0]=MAP_RIGHT+1;
	CombatMenuSelection = (CombatMenuItem)Units[m].Mode;

	// そのユニットの随伴機を枠付けします。
	for( var n=0; n<=MaxUnitId; n++)
		{
		ref var unit = ref Units[n];
		if( unit.IsUsed && unit.GroupLeader==m )
			{
			SelectionCount++; Selections[1][n]=unit.FormationNumber;
			}
		}
	// そのユニットをユニットインフォにセットします。
	if( !(Units[m].Category==UnitCategory.Plane && Units[m].PlaneState==UnitState.Parked) )
		{
		UnitInfoPanel[0]=(int)Units[m].Kind;
		UnitInfoPanel[1]=0;				// 空母なら１で格納庫 ０ で飛行甲板
		UnitInfoPanel[3]=m;				// そのユニットの番号
		UnitInfoPanel[4]=(int)Units[m].Side;	// そのユニットの国籍
		//unit_info[2]=0;				// 駐機機の発進までのフラグ
		switch((UnitKind)UnitInfoPanel[0])
			{
			case UnitKind.Battleship:
				UnitInfoPanel[2]=0;
				break;
			case UnitKind.Cruiser:
				UnitInfoPanel[2]=1;
				break;
			case UnitKind.Destroyer:
				UnitInfoPanel[2]=2;
				break;
			case UnitKind.Submarine:
				UnitInfoPanel[2]=3;
				break;
			case UnitKind.Carrier:
				UnitInfoPanel[2]=4;
				break;
			case UnitKind.LightCarrier:
				UnitInfoPanel[2]=5;
				break;
			case UnitKind.Transport:
				UnitInfoPanel[2]=6;
				break;
			case UnitKind.AirBase:
				UnitInfoPanel[2]=8;
				break;
			default:
				UnitInfoPanel[2]=7;
				break;
			}
		}
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cls_all_slct_unit")]
public void	ClearSelection()
	{
	for(var m=0; m<=255; m++)
		{
		Selections[0][m]=0;
		Selections[1][m]=0;
		}
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cls_all_slct_unit_p2")]
public void	ClearSelection2( int side )
	{

	if( side==1 )
		{
		// マイサイドの消去
		for(var m=0; m<=255; m++)
			{
			Selections[1][m]=0;
			}
		}
	else
		{
		// 敵サイドの消去
		for(var m=0; m<=255; m++)
			{
			Selections[0][m]=0;
			}
		}

	}

//============================================================================
// Effect構造体の空いてる最低番号を代えします。
//----------------------------------------------------------------------------
[Original("seek_effect_no")]
public int		FindFreeEffect()
	{

	var free=0;
	// 1.22 has 255 effects in use at most.
	for(var s=1;s<Rules.EffectLimit;s++)
		{
		if( Effects[s].Layer==EffectLayer.None )
			{
			free=s;
			break;
			}
		}

	return (free);
	}

//============================================================================
// Ｆｉｒｅ構造体の空いてる最低番号を代えします。また、必要ならＭａｘ＿ｆｉｒｅも変更
//----------------------------------------------------------------------------
[Original("seek_fire_no")]
public int		FindFreeFire()
	{

	var free=0;
	// 1.22 has 255 fires in use at most.
	for(var s=1;s<Rules.FireLimit;s++)
		{
		if( Fires[s].Target==0 )
			{
			free=s;
			break;
			}
		}

	return (free);
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("rtn_damage_pt")]
public int	GetDamagePoints(int	m)
	{
	int		damage=0 /* C4701 */;

	//	基礎的な破壊力をセット
	switch( Fires[m].Kind )
		{
		case FireKind.Bullet:		damage=BLT_DMG+Random(BLT_DMG);		break;
		case FireKind.Gun:		damage=GUN_DMG+Random(GUN_DMG);		break;
			break;
		case FireKind.AntiAircraftShell:		damage=SHL_DMG+Random(SHL_DMG);		break;
		case FireKind.Torpedo:		damage=TPD_DMG+Random(TPD_DMG);
			break;
		case FireKind.Bomb:		damage=BOM_DMG+Random(BOM_DMG);		break;
		case FireKind.AntiSubmarineBomb:		damage=Rules.DepthChargeDamage+Random(Rules.DepthChargeDamageSpread);		break;
		case FireKind.RapidAntiAircraftShell:		damage=RAS_DMG+Random(RAS_DMG);		break;
		}

	// 少し乱数させる

	// サイドによって各破壊力を操作

	return(damage);
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("drctn_for_8")]
public int		ToEightDirections(int direction)
	{

	direction=direction%360;

	switch( (int)(direction/22.5) )
		{
		case 0: case 15:
			direction=2;
			break;
		case 1: case 2:
			direction=1;
			break;
		case 3: case 4:
			direction=0;
			break;
		case 5: case 6:
			direction=7;
			break;
		case 7: case 8:
			direction=6;
			break;
		case 9: case 10:
			direction=5;
			break;
		case 11: case 12:
			direction=4;
			break;
		case 13: case 14:
			direction=3;
			break;
		default:
			direction=2;
			break;
		}

	return ( direction );
	}

//============================================================================
// 艦隊のフォーメーションの初期値を設定します
//----------------------------------------------------------------------------
[Original("set_frmtn_of_ships")]
public void	SetShipFormation( int s )
	{
	double leaderHeading=0 /* C4701 */ /* C4701 */;

	// リーダー艦からの方位角を求めます。
	var offset=(Units[s].Position-Units[Units[s].GroupLeader].Position).NonZero();
	var bearing=offset.Direction;
	switch((int)(Units[Units[s].GroupLeader].Direction/22.5))
		{
		case 0: case 15:
			leaderHeading=0.0;
			break;
		case 1: case 2:
			leaderHeading=45.0;
			break;
		case 3: case 4:
			leaderHeading=90.0;
			break;
		case 5: case 6:
			leaderHeading=90.0+45.0;
			break;
		case 7: case 8:
			leaderHeading=180.0;
			break;
		case 9: case 10:
			leaderHeading=180.0+45.0;
			break;
		case 11: case 12:
			leaderHeading=270.0;
			break;
		case 13: case 14:
			leaderHeading=270.0+45.0;
			break;
		default:
			// 1.22 reads the angle where its stack slot holds the offset's X (0x417179).
			if( Rules.HeadingsOf360TakeNoCase )
				leaderHeading=offset.X;
			break;
		}
	bearing=bearing-leaderHeading;
	if(bearing<0)
		bearing=360+bearing;

	Units[s].DirectionToLeader=bearing;

	// リーダー艦からの距離を求めます
	Units[s].DistanceToLeader=(Units[s].Position-Units[Units[s].GroupLeader].Position).Length;

	}

//============================================================================
// 着艦パターンをＰｐ＿ｘｙにセットします。
//----------------------------------------------------------------------------
[Original("set_pos_of_take_down")]
public void	SetLandingDestination(int n)
	{
	double heading=0 /* C4701 */;

	var carrier=Units[n].Carrier;

	if( !Units[carrier].IsUsed )
		{
		Units[n].SetPathPoint(0, Units[n].Position+new WorldVector(Random(400)-200, Random(400)-200));

		Units[n].PathX[1]=MAP_RIGHT+1;
		Units[n].IsStopping=false;

		return;
		}

	switch((int)(Units[carrier].Direction/22.5))
		{
		case 0: case 15:
			heading=0.0;
			break;
		case 1: case 2:
			heading=45.0;
			break;
		case 3: case 4:
			heading=90.0;
			break;
		case 5: case 6:
			heading=90.0+45.0;
			break;
		case 7: case 8:
			heading=180.0;
			break;
		case 9: case 10:
			heading=180.0+45.0;
			break;
		case 11: case 12:
			heading=270.0;
			break;
		case 13: case 14:
			heading=270.0+45.0;
			break;
		default:
			// 1.22 reads the angle it has not set (0x4173BF).
			if( Rules.HeadingsOf360TakeNoCase )
				heading=TakeStackLocal(0x4173BF, heading);
			break;
		}

	var astern=heading;
	astern-=180.0;

	if( heading<0 )
		heading=360+heading;

	if( Units[n].Kind==UnitKind.Bomber )
		{
		Units[n].SetPathPoint(0, Units[carrier].Position+WorldVector.FromDirection(astern) * 560.0);
		}
	else
		{
		Units[n].SetPathPoint(0, Units[carrier].Position+WorldVector.FromDirection(astern) * 280.0);
		}

	Units[n].SetPathPoint(1, Units[carrier].Position+WorldVector.FromDirection(astern) * 130.0);

	Units[n].SetPathPoint(2, Units[carrier].Position+WorldVector.FromDirection(astern) * 10.0);

	Units[n].SetPathPoint(3, Units[carrier].Position+WorldVector.FromDirection(heading) * 100.0);

	Units[n].PathX[4]=MAP_RIGHT+1;
	Units[n].IsStopping=false;
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cont_pos_of_take_down")]
public void	UpdateLanding( int n )
	{
	int		count;
	double heading=0 /* C4701 */;

	var carrier=Units[n].Carrier;

	// 1.22 approaches any base still in use, whatever its kind (0x417555).
	if( !(Units[carrier].Kind==UnitKind.Carrier || Units[carrier].Kind==UnitKind.LightCarrier || Units[carrier].Kind==UnitKind.AirBase) && !Rules.ApproachesOnDrawnTick )
		{
		Units[n].Carrier=0;
		carrier=0;
		}

	if(  !Units[carrier].IsUsed || carrier==0 )
		{
		// もどる場所がない場合、あった場所で適当に動く
		Units[n].SetPathPoint(0, Units[n].Position+new WorldVector(Random(400)-200, Random(400)-200));

		Units[n].PathX[1]=MAP_RIGHT+1;
		Units[n].IsStopping=false;

		return;
		}

	switch((int)(Units[carrier].Direction/22.5))
		{
		case 0: case 15:
			heading=0.0;
			break;
		case 1: case 2:
			heading=45.0;
			break;
		case 3: case 4:
			heading=90.0;
			break;
		case 5: case 6:
			heading=90.0+45.0;
			break;
		case 7: case 8:
			heading=180.0;
			break;
		case 9: case 10:
			heading=180.0+45.0;
			break;
		case 11: case 12:
			heading=270.0;
			break;
		case 13: case 14:
			heading=270.0+45.0;
			break;
		default:
			// 1.22 reads the angle it has not set (0x41764E).
			if( Rules.HeadingsOf360TakeNoCase )
				heading=TakeStackLocal(0x41764E, heading);
			break;
		}

	var astern=heading;
	astern-=180.0;

	if( heading<0 )
		heading=360+heading;

	for( count=0; Units[n].PathX[count]!=MAP_RIGHT+1; count++){}

	switch( count )
		{
		case 4:
			// 全4点を調整
			if( Units[n].Kind==UnitKind.Bomber )
				{
				Units[n].SetPathPoint(0, Units[carrier].Position+WorldVector.FromDirection(astern) * 560.0);
				}
			else
				{
				Units[n].SetPathPoint(0, Units[carrier].Position+WorldVector.FromDirection(astern) * 280.0);
				}

			Units[n].SetPathPoint(1, Units[carrier].Position+WorldVector.FromDirection(astern) * 130.0);

			Units[n].SetPathPoint(2, Units[carrier].Position+WorldVector.FromDirection(astern) * 10.0);

			Units[n].SetPathPoint(3, Units[carrier].Position+WorldVector.FromDirection(heading) * 100.0);
			break;

		case 3:
			// 全3点を調整
			Units[n].SetPathPoint(0, Units[carrier].Position+WorldVector.FromDirection(astern) * 130.0);

			Units[n].SetPathPoint(1, Units[carrier].Position+WorldVector.FromDirection(astern) * 10.0);

			Units[n].SetPathPoint(2, Units[carrier].Position+WorldVector.FromDirection(heading) * 100.0);
			break;

		case 2:
			// 全2点を調整
			Units[n].SetPathPoint(0, Units[carrier].Position+WorldVector.FromDirection(astern) * 10.0);

			Units[n].SetPathPoint(1, Units[carrier].Position+WorldVector.FromDirection(heading) * 100.0);
			break;
		}
	}

/*******
//============================================================================
//絶対スクリーン座標に線を描画する
//----------------------------------------------------------------------------
void	draw_line(int	x1,int	y1,int	x2,int	y2)
	{
	int		dstX, dstY,addX,addY;
	int		ctr,x,y,i;

	//書き込むＶＲＡＭのアドレスを得る（ロックして書き込めるようにする）
	memset(&dst_ddsd, 0, sizeof(DDSURFACEDESC));
	dst_ddsd.dwSize = sizeof(DDSURFACEDESC);
	IDirectDrawSurface_Lock( lpDDSBack, NULL, (LPDDSURFACEDESC)&dst_ddsd, DDLOCK_WAIT, NULL );
	dst_vram=dst_ddsd.lpSurface;
//	memset(dst_vram, NULL,  307200 );	//内容をクリア

	dstX=x2-x1;	dstY=y2-y1;

	if(dstX<0)	{addX=-1;	dstX*=-1;}	else	addX=1;
	if(dstY<0)	{addY=-1;	dstY*=-1;}	else	addY=1;

	ctr=0;
	x=x1;	y=y1;
	if(dstX>=dstY)
		{
		for(i=0;i<dstX;i++)
			{
			if( x>=0 && y>=0 && x<=CMBT_WIDTH-1 && y<=CMBT_HEIGHT-1 )
				{
				dst_vram[y*dst_ddsd.lPitch+(x*2)]=0xff;
				dst_vram[y*dst_ddsd.lPitch+(x*2)+1]=0xff;
				}

			x+=addX;
			ctr+=dstY;
			if(ctr>=dstX)
				{
				y+=addY;
				ctr-=dstX;
				}
			}
		}
	else
		{
		for(i=0;i<dstY;i++)
			{
			if( x>=0 && y>=0 && x<=CMBT_WIDTH-1 && y<=CMBT_HEIGHT-1 )
				{
				dst_vram[y*dst_ddsd.lPitch+(x*2)]=0xff;
				dst_vram[y*dst_ddsd.lPitch+(x*2)+1]=0xff;
				}

			y+=addY;
			ctr+=dstX;
			if(ctr>=dstY)
				{
				x+=addX;
				ctr-=dstY;
				}
			}
		}

	IDirectDrawSurface_Unlock( lpDDSBack, NULL );

	}
*******/

//============================================================================
// 可視不可視の発見範囲内の潜航潜水艦
//----------------------------------------------------------------------------
[Original("find_out_ss")]
public int		DetectSubmarines( int m , int n)
	{

	// 現地点からユニット地点への距離
	var range=(int)(Units[n].Position-Units[m].Position).Length;

	if( range<100 )
		range=100;

	// 1.22 draws 10 where NSPW NET draws 2, and places the contact within 0.6 of the range whatever the type (0x417FC9).
	if( Random( ( Units[m].Variant==1 ? range : range )*( 1+(Units[n].Speed==0 ? 1 : 0)*4) )==Rules.SubmarineContactDraw )
		{	// 敵潜水艦探知！
		if( range>150 )
			range=150;

		var spread=(int)(range*( Units[m].Variant==0 || !Rules.ContactSpreadByType ? 0.60 : 0.50 ));

		Units[n].ContactX=(int)(Units[n].Position.X+Random((spread)*2)-spread);
		Units[n].ContactY=(int)(Units[n].Position.Y+Random((spread)*2)-spread);

		Units[n].ContactRadius=range;
		Units[n].ContactTime=250+Random(150);
		return(1);
		}
	else
		{
		return(0);
		}
	}

// When a submerged submarine is found, 1.22 stops the computer's destroyers that chase it (em_flg[1]) unless they are
// on its side (0x418724).
private void StopDestroyersChasing(int n)
	{
	for(var m=1;m<=MaxUnitId;m++)
		{
		ref var unit = ref Units[m];
		if( unit.Kind==UnitKind.Destroyer && unit.EmergencyFlags[0]!=0 && unit.EmergencyFlags[1]==n && unit.Side!=Units[n].Side && unit.Side==CpuSide && unit.IsUsed )
			{
			unit.EmergencyFlags[0]=0;
			unit.EmergencyFlags[1]=0;
			}
		}
	}

//============================================================================
// 可視不可視
//----------------------------------------------------------------------------
[Original("find_out")]
public void		Detect()
	{
	if( RevealsAll!=0 || Result!=GameResult.None )
		{
		for(var m=0;m<=MaxUnitId;m++)
			Units[m].IsFound=true;
		return;
		}

	for(var m=0;m<=MaxUnitId;m++)
		{
		ref var unit = ref Units[m];
		if( unit.ContactTime!=0 )
			unit.ContactTime--;
		else
			unit.IsFound=false;
		}

	// ユニットの見え隠れ
	for(var m=1;m<=MaxUnitId;m++)
		{
		ref var unit = ref Units[m];
		// 1.22's player's units look out while they supply too (0x41829B).
		if( unit.IsUsed && unit.Side==LocalSide  && unit.PlaneState!=UnitState.Parked  && (!unit.IsSupplying || Rules.SupplyingUnitsLookOut) )
			{
			var sightings=0;
			for(var n=1;n<=MaxUnitId;n++)
				{
				ref var other = ref Units[n];
				if( other.IsUsed && other.Side!=LocalSide && other.PlaneState!=UnitState.Parked && (!other.IsFound || unit.Kind==UnitKind.Submarine ) )
					{
					// 現地点からユニット地点への距離
					var eye=EyeOf(ref unit);	// 航空機の場合はちょっと前へ

					var size=GetDetectionSize(m,n);
					var distance=(other.Position-eye).Length;

					if( (int)distance<=size && size!=0 )
						{
						sightings++;
						if( other.Kind==UnitKind.Submarine && other.IsSubmerged )
							{	// 潜航中潜水艦が発見可能範囲にいる
							if( DetectSubmarines(m,n)!=0 )
								{
								other.IsFound=true;

								if( Rules.FoundSubmarinesStopChasers )
									StopDestroyersChasing(n);
								}
							}
						else
							{
							other.IsFound=true;	// 普通のユニットが見つかった場合

							// 1.22's submarine dives when it finds any unit, even while it supplies (0x4184DF).
							if( unit.Kind==UnitKind.Submarine && ( (!unit.IsSupplying && other.Kind!=UnitKind.Submarine) || Rules.SubmarinesDiveAtAnyUnit ) )		// 発見したのが潜水艦の場合。
								{
								unit.IsSubmerged=true;		// 潜ります。
								}
							}
						}
					}
				}

			if( sightings==0 && unit.Kind==UnitKind.Submarine )		// 潜水艦が発見できなかった
				{
				unit.IsSubmerged=false;					// 浮上します。
				unit.IsFound=false;					// クリアします。
				unit.ContactTime=0;
				}

			}

		if( unit.IsUsed && unit.Side!=LocalSide  && unit.PlaneState!=UnitState.Parked  && !unit.IsSupplying )
			{
			var sightings=0;
			for(var n=1;n<=MaxUnitId;n++)
				{
				ref var other = ref Units[n];
				if( other.IsUsed && other.Side==LocalSide  && other.PlaneState!=UnitState.Parked && (!other.IsFound || unit.Kind==UnitKind.Submarine) )
					{
					// 現地点からユニット地点への距離

					var eye=EyeOf(ref unit);	// 航空機の場合はちょっと前へ

					var size=GetDetectionSize(m,n);
					var distance=(other.Position-eye).Length;

					if( (int)distance<=size && size!=0 )
						{
						sightings++;
						if( other.Kind==UnitKind.Submarine && other.IsSubmerged )
							{	// 潜航中潜水艦が発見可能範囲にいる
							if( DetectSubmarines(m,n)!=0 )
								{
								other.IsFound=true;

								if( Rules.FoundSubmarinesStopChasers )
									StopDestroyersChasing(n);
								}
							}
						else
							{	// 普通のユニットが見つかった場合
							other.IsFound=true;

							// 1.22's submarine dives when it finds any unit, even while it supplies (0x4184DF).
							if( unit.Kind==UnitKind.Submarine && ( (!unit.IsSupplying && other.Kind!=UnitKind.Submarine) || Rules.SubmarinesDiveAtAnyUnit ) )		// 発見したのが潜水艦の場合。
								{
								unit.IsSubmerged=true;
								}
							}
						}
					}
				}

			if( sightings==0 && unit.Kind==UnitKind.Submarine )		// 潜水艦が発見できなかった
				{
				unit.IsSubmerged=false;					// 浮上します
				unit.IsFound=false;					// クリアします。
				unit.ContactTime=0;
				}

			}
		}
	}

}
