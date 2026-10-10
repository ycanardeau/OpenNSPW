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

private void SetUnitedStatesArrivalPoint(ref double rx, ref double ry, ref double rx2, ref double ry2, int new_unit_kind, Side arrived_side)
	{
	switch(CurrentMap)
		{
		case 0:		// 南太平洋
			rx=MAP_RIGHT-Random(1200)-100;
			ry=MAP_BOTTOM-10;
			rx2=rx-200;
			ry2=ry+500;
ry-=new_unit_kind*80;
			break;

		case 1:		// 中部太平洋
			rx=MAP_RIGHT+10;
			ry=Random(1200);
			rx2=rx-500;
			ry2=ry;
rx+=new_unit_kind*80;
			break;

		case 2:		// 日本近海
			rx=MAP_RIGHT-Random(1200)-100;
			ry=MAP_BOTTOM-10;
			rx2=rx-200;
			ry2=ry+500;
ry-=new_unit_kind*80;
			break;

		case 3:		// ユーザーマップ
			switch(Reinforcements[(int)arrived_side])
				{
				case 0:
					rx=MAP_LEFT-500;
					ry=MAP_TOP+500;
					rx2=rx+1200;
					ry2=ry-1200-Random(500);
					ry-=new_unit_kind*80;
					break;
				case 1:
					rx=MAP_RIGHT+500;
					ry=MAP_TOP+500;
					rx2=rx-1200;
					ry2=ry-1200-Random(500);
					ry-=new_unit_kind*80;
					break;
				case 2:
					rx=MAP_RIGHT+500;
					ry=MAP_BOTTOM-500;
					rx2=rx-1200;
					ry2=ry+1200+Random(500);
					ry+=new_unit_kind*80;
					break;
				case 3:
					rx=MAP_LEFT-500;
					ry=MAP_BOTTOM-500;
					rx2=rx+1200;
					ry2=ry+1200+Random(500);
					ry+=new_unit_kind*80;
					break;
				}
			break;
		}
	}

private void SetJapanArrivalPoint(ref double rx, ref double ry, ref double rx2, ref double ry2, int new_unit_kind, Side arrived_side)
	{
	switch(CurrentMap)
		{
		case 0:		// 南太平洋
			rx=MAP_LEFT-10;
			ry=MAP_TOP-Random(1200);
			rx2=rx+500;
			ry2=ry-200;
rx-=new_unit_kind*80 ;
			break;

		case 1:		// 中部太平洋
			rx=MAP_LEFT-10;
			ry=MAP_BOTTOM+Random(1200);
			rx2=rx+500;
			ry2=ry+200;
rx-=new_unit_kind*80;
			break;

		case 2:		// 日本近海
			rx=MAP_LEFT+Random(1200);
			ry=MAP_TOP+10;
			rx2=rx+200;
			ry2=ry-500;
ry+=new_unit_kind*80;
			break;

		case 3:		// ユーザーマップ
			switch(Reinforcements[(int)arrived_side])
				{
				case 0:
					rx=MAP_LEFT-500;
					ry=MAP_TOP+500;
					rx2=rx+1200;
					ry2=ry-1200-Random(500);
					ry-=new_unit_kind*80;
					break;
				case 1:
					rx=MAP_RIGHT+500;
					ry=MAP_TOP+500;
					rx2=rx-1200;
					ry2=ry-1200-Random(500);
					ry-=new_unit_kind*80;
					break;
				case 2:
					rx=MAP_RIGHT+500;
					ry=MAP_BOTTOM-500;
					rx2=rx-1200;
					ry2=ry+1200+Random(500);
					ry+=new_unit_kind*80;
					break;
				case 3:
					rx=MAP_LEFT-500;
					ry=MAP_BOTTOM-500;
					rx2=rx+1200;
					ry2=ry+1200+Random(500);
					ry+=new_unit_kind*80;
					break;
				}
			break;

		}
	}

//============================================================================
// 新ユニット登場
//----------------------------------------------------------------------------
[Original("new_unit_arrived")]
public void OnUnitArrived(int side,int new_unit_kind)
	{
	double		rx=default /* C4701 */,ry=default /* C4701 */;
	double		rx2=default /* C4701 */,ry2=default /* C4701 */;
	UnitKind	kind=default /* C4701 */; int kind2,m,i;
	Side		arrived_side;

	if( side==1 )
		{
		arrived_side=LocalSide;
		}
	else
		{
		if(LocalSide==Side.Japan)
			arrived_side=Side.UnitedStates;
		else
			arrived_side=Side.Japan;
		}

	kind2=0;

	new_unit_kind--;

	switch(new_unit_kind)
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
		case 9:		kind=UnitKind.Fighter;	kind2=1;			break;
		case 10:	kind=UnitKind.Transport;	kind2=(int)FireKind.CargoInfantryBase;		break;
		case 11:	kind=UnitKind.Transport;	kind2=(int)FireKind.CargoPillboxes;		break;
		case 12:	kind=UnitKind.Transport;	kind2=(int)FireKind.CargoFortress;		break;
		case 13:	kind=UnitKind.Transport;	kind2=(int)FireKind.CargoAirBase;		break;
		case 14:	kind=UnitKind.Transport;	kind2=(int)FireKind.CargoNavalBase;			break;

		case 15:		kind=UnitKind.Cruiser;	kind2=1;		break;
		case 16:		kind=UnitKind.Destroyer;	kind2=1;		break;
		case 17:
			if( arrived_side==Side.Japan )
				kind=UnitKind.Battleship;
			else
				kind=UnitKind.Carrier;
			kind2=1;
			break;

		}

	if(arrived_side==Side.Japan)
		{
		SetJapanArrivalPoint(ref rx, ref ry, ref rx2, ref ry2, new_unit_kind, arrived_side);
		}
	else
		{
		SetUnitedStatesArrivalPoint(ref rx, ref ry, ref rx2, ref ry2, new_unit_kind, arrived_side);
		}

	if( new_unit_kind<=5 || new_unit_kind>=10 )
		{
		// 艦船の登場

		if( arrived_side==Side.Japan )
			m=AddUnit2(Side.Japan,kind,kind2,rx,ry,(double)90);
		else
			m=AddUnit2(Side.UnitedStates,kind,kind2,rx,ry,(double)90);

		if(kind==UnitKind.Transport)
			{
			// 輸送船の場合、積荷
			Units[m].Weapon=(FireKind)kind2;		// 武装品種
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

		for(i=0;i<((kind==UnitKind.Attacker ? 1 : 0)*3)+((kind==UnitKind.Fighter ? 1 : 0)*2)+((kind==UnitKind.Bomber ? 1 : 0)*1)  ;i++)
			{
			if( arrived_side==Side.Japan )
				m=AddUnit2(Side.Japan,kind,kind2,rx,ry,(double)90);
			else
				m=AddUnit2(Side.UnitedStates,kind,kind2,rx,ry,(double)90);

			Units[m].PathX[0]=rx2;
			Units[m].PathY[0]=ry2;
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

			rx+=-160+Random(320);
			ry+=-160+Random(320);
			rx2+=-100+Random(200);
			ry2+=-100+Random(200);
			}
		}

	}

//============================================================================
// 可視不可視のサイズ設定
//----------------------------------------------------------------------------
[Original("find_out_size")]
public int		GetDetectionSize( int m , int n)
	{
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
public void	DrawLine4(int x1,int y1,int x2,int y2,int right,int bottom,uint rgb)
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
public void	DrawLine5(int x1,int y1,int x2,int y2,int right,int bottom,ushort cl)
	{
	int		dstX, dstY,addX,addY;
	int		ctr,x,y,i;
	int		cl1, cl2;
	DDCOLORKEY          ddck;
	ushort* dst_vram;			//書き込むＶＲＡＭのアドレス

	DDSURFACEDESC2		dst_ddsd;

cl=0xFFFF;

	//書き込むＶＲＡＭのアドレスを得る（ロックして書き込めるようにする）
	memset(&dst_ddsd, 0, (nuint)(sizeof(DDSURFACEDESC2)));
	dst_ddsd.dwSize = (uint)(sizeof(DDSURFACEDESC2));
	IDirectDrawSurface_Lock( lpDDSBack, null, &dst_ddsd, DDLOCK_WAIT, null );

	dst_vram=(ushort*)dst_ddsd.lpSurface;

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
				dst_vram[y*(dst_ddsd.lPitch/2)+(x)]=cl;
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
				dst_vram[y*(dst_ddsd.lPitch/2)+(x)]=cl;
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

	IDirectDrawSurface_Unlock( lpDDSBack, null );

	}
//============================================================================
//
//----------------------------------------------------------------------------
[Original("pt_in_rect")]
public int	PointInRect(ref RECT dstn_rect,int crsr_x,int crsr_y)
	{
	if( dstn_rect.top <= crsr_y && dstn_rect.bottom >= crsr_y &&
		dstn_rect.right >= crsr_x && dstn_rect.left <= crsr_x)
		return 1;
	return 0;
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("pt_in_rect2")]
public int	PointInRect2(ref RECT dstn_rect,int crsr_x,int crsr_y)
	{
	if( dstn_rect.top >= crsr_y && dstn_rect.bottom <= crsr_y &&
		dstn_rect.right >= crsr_x && dstn_rect.left <= crsr_x)
		return (1);
	return (0);
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("pt_in_rect3")]
public int	PointInRect3(ref RECT dstn_rect,int crsr_x,int crsr_y)
	{
	if( dstn_rect.top >= crsr_y && dstn_rect.bottom <= crsr_y &&
		dstn_rect.right >= crsr_x && dstn_rect.left <= crsr_x)
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
public int	ClipRects(ref RECT dstn_rect, ref RECT src_rect, ref RECT field_rect)
	{
	// field_rectと一致するdstn_rectを見つけ出し。その差分をsrc_rectに反映させます。

	if( dstn_rect.top <= field_rect.bottom )
		{
		if(dstn_rect.top < field_rect.top )
			{
			src_rect.top+=(field_rect.top-dstn_rect.top);
			dstn_rect.top=field_rect.top;
			}
		}
	else
		{
		return (0);
		}

	if( dstn_rect.bottom >= field_rect.top )
		{
		if(dstn_rect.bottom > field_rect.bottom )
			{
			src_rect.bottom-=(dstn_rect.bottom-field_rect.bottom);
			dstn_rect.bottom=field_rect.bottom;
			}
		}
	else
		{
		return (0);
		}

	if( src_rect.top==src_rect.bottom )
		return (0);

	if( dstn_rect.left <= field_rect.right )
		{
		if(dstn_rect.left < field_rect.left )
			{
			src_rect.left+=(field_rect.left-dstn_rect.left);
			dstn_rect.left=field_rect.left;
			}
		}
	else
		{
		return (0);
		}

	if( dstn_rect.right >= field_rect.left )
		{
		if(dstn_rect.right > field_rect.right )
			{
			src_rect.right-=(dstn_rect.right-field_rect.right);
			dstn_rect.right=field_rect.right;
			}
		}
	else
		{
		return (0);
		}

	if( src_rect.left==src_rect.right )
		return (0);

	return (1);
	}

//============================================================================
// その空母の現在の開きスペースを代えします。
//----------------------------------------------------------------------------
[Original("seek_parking_no")]
public int		FindParkingNumber( int m )
	{
	int		i,n,rtn=0 /* C4701 */; Array32<int> wrk=default;

	for(i=0;i<=31;i++)
		wrk[i]=0;

	for( i=1; i<=MaxUnitId; i++)
		{
		ref var unit = ref Units[i];
		if( unit.IsUsed && unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked
			&& Units[m].Carrier==unit.Carrier )
			wrk[unit.ParkingNumber]++;
		}

	for(i=0;i<=31;i++)
		if(wrk[i]==0)
			{
			rtn=i;
			break;
			}

	return	(rtn);
	}

//============================================================================
// 現在収容数(着艦予定、飛行甲板上数も含む)
//----------------------------------------------------------------------------
[Original("plane_in_cv")]
public int		CountPlanesIn( int m )
	{
	int		i,rtn;

	rtn=0;
	for(i=1;i<=MaxUnitId;i++)
		{
		ref var unit = ref Units[i];
		if( unit.IsUsed && unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked && m==unit.Carrier )
			rtn++;
		}
	return (rtn);
	}

//============================================================================
// 駐機場、格納庫の停止位置を求めます。
//----------------------------------------------------------------------------
[Original("set_pos_of_parking")]
public void	SetParkingPosition( int	m )
	{
	ref var unit = ref Units[m];
	int		max,i,w;
	int		parking_x,parking_y;

	// ２列格納
	if( Units[unit.Carrier].Kind==UnitKind.AirBase )
		{	// 陸上基地
		parking_y=Sprites[SpriteId.JapanUnitInfo].Y+Sprites[SpriteId.JapanUnitInfo].Height-((unit.ParkingNumber/2)*40)-100;
		if( false && unit.Kind==UnitKind.Bomber )
			w=55;
		else
			w=35;
		if( unit.ParkingNumber%2!=0 )
			{ // 右側
			parking_x=Sprites[SpriteId.JapanUnitInfo].X+Sprites[SpriteId.JapanUnitInfo].Width/2-w;
			unit.Direction=270+45;
			parking_y-=18;
			}
		else
			{ // 左側
			parking_x=Sprites[SpriteId.JapanUnitInfo].X+Sprites[SpriteId.JapanUnitInfo].Width/2+w;
			unit.Direction=180+45;
			}
		}
	else
		{	// 航空母艦
		parking_y=Sprites[SpriteId.JapanUnitInfo].Y+Sprites[SpriteId.JapanUnitInfo].Height-((unit.ParkingNumber/2)*40)-100;
		if( unit.ParkingNumber%2!=0 )
			{ // 右側
			parking_x=Sprites[SpriteId.JapanUnitInfo].X+Sprites[SpriteId.JapanUnitInfo].Width/2-25;
			unit.Direction=270+45;
			parking_y-=18;
			}
		else
			{ // 左側
			parking_x=Sprites[SpriteId.JapanUnitInfo].X+Sprites[SpriteId.JapanUnitInfo].Width/2+25;
			unit.Direction=180+45;
			}
		}

	unit.Position = new WorldPosition((double)parking_x, (double)parking_y);
	}

//============================================================================
//
//----------------------------------------------------------------------------
public void	set_the_slct_unit (int m)
	{
	int	n;

	if( Units[m].Side!=LocalSide )
		return;

	BufferedMoveOrders[1].ClearsPath=1;

	MoveOrders[1].ClearsPath=1;

	SelectedUnit=(short)m; SelectionCount=0; Selections[1][m]=1; CombatMenuKind=1; WorkPathX[0]=MAP_RIGHT+1;
	CombatMenuSelection = (CombatMenuItem)Units[m].Mode;

	// そのユニットの随伴機を枠付けします。
	for( n=0; n<=MaxUnitId; n++)
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
	int		m;
	for(m=0; m<=255; m++)
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
	int		m;

	if( side==1 )
		{
		// マイサイドの消去
		for(m=0; m<=255; m++)
			{
			Selections[1][m]=0;
			}
		}
	else
		{
		// 敵サイドの消去
		for(m=0; m<=255; m++)
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
	int		rtn,s;

	rtn=0;
	for(s=1;s<EFFECT_MAX;s++)
		{
		if( Effects[s].Layer==EffectLayer.None )
			{
			rtn=s;
			break;
			}
		}

	return (rtn);
	}

//============================================================================
// Ｆｉｒｅ構造体の空いてる最低番号を代えします。また、必要ならＭａｘ＿ｆｉｒｅも変更
//----------------------------------------------------------------------------
[Original("seek_fire_no")]
public int		FindFreeFire()
	{
	int		rtn,s;

	rtn=0;
	for(s=1;s<FIRE_MAX;s++)
		{
		if( Fires[s].Target==0 )
			{
			rtn=s;
			break;
			}
		}

	return (rtn);
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("rtn_damage_pt")]
public int	GetDamagePoints(int	m)
	{
	int		rtn=0 /* C4701 */;

	//	基礎的な破壊力をセット
	switch( Fires[m].Kind )
		{
		case FireKind.Bullet:		rtn=BLT_DMG+Random(BLT_DMG);		break;
		case FireKind.Gun:		rtn=GUN_DMG+Random(GUN_DMG);		break;
			break;
		case FireKind.AntiAircraftShell:		rtn=SHL_DMG+Random(SHL_DMG);		break;
		case FireKind.Torpedo:		rtn=TPD_DMG+Random(TPD_DMG);
			break;
		case FireKind.Bomb:		rtn=BOM_DMG+Random(BOM_DMG);		break;
		case FireKind.AntiSubmarineBomb:		rtn=1+Random(ASB_DMG+2+1);		break;
		case FireKind.RapidAntiAircraftShell:		rtn=RAS_DMG+Random(RAS_DMG);		break;
		}

	// 少し乱数させる

	// サイドによって各破壊力を操作

	return(rtn);
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("drctn_for_8")]
public int		ToEightDirections(int drctn)
	{

	drctn=drctn%360;

	switch( (int)(drctn/22.5) )
		{
		case 0: case 15:
			drctn=2;
			break;
		case 1: case 2:
			drctn=1;
			break;
		case 3: case 4:
			drctn=0;
			break;
		case 5: case 6:
			drctn=7;
			break;
		case 7: case 8:
			drctn=6;
			break;
		case 9: case 10:
			drctn=5;
			break;
		case 11: case 12:
			drctn=4;
			break;
		case 13: case 14:
			drctn=3;
			break;
		default:
			drctn=2;
			break;
		}

	return ( drctn );
	}

//============================================================================
// 艦隊のフォーメーションの初期値を設定します
//----------------------------------------------------------------------------
[Original("set_frmtn_of_ships")]
public void	SetShipFormation( int s )
	{
	double		wrk_x,wrk_y,drctn,dstc,wrk_drctn=0 /* C4701 */;

	// リーダー艦からの方位角を求めます。
	wrk_x=Units[s].Position.X-Units[Units[s].GroupLeader].Position.X;
	wrk_y=Units[s].Position.Y-Units[Units[s].GroupLeader].Position.Y;
	if(wrk_x==0)	wrk_x=1;
	if(wrk_y==0)	wrk_y=1;

	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;
	switch((int)(Units[Units[s].GroupLeader].Direction/22.5))
		{
		case 0: case 15:
			wrk_drctn=0.0;
			break;
		case 1: case 2:
			wrk_drctn=45.0;
			break;
		case 3: case 4:
			wrk_drctn=90.0;
			break;
		case 5: case 6:
			wrk_drctn=90.0+45.0;
			break;
		case 7: case 8:
			wrk_drctn=180.0;
			break;
		case 9: case 10:
			wrk_drctn=180.0+45.0;
			break;
		case 11: case 12:
			wrk_drctn=270.0;
			break;
		case 13: case 14:
			wrk_drctn=270.0+45.0;
			break;
		}
	drctn=drctn-wrk_drctn;
	if(drctn<0)
		drctn=360+drctn;

	Units[s].DirectionToLeader=drctn;

	// リーダー艦からの距離を求めます
	wrk_x=Units[s].Position.X-Units[Units[s].GroupLeader].Position.X;
	wrk_y=Units[s].Position.Y-Units[Units[s].GroupLeader].Position.Y;
	if(wrk_x==0)	wrk_x=1;
	if(wrk_y==0)	wrk_y=1;

	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;

	if(wrk_x<0)
		wrk_x=0-wrk_x;
	if(wrk_y<0)
		wrk_y=0-wrk_y;
	if(drctn>=180)
		drctn=drctn-180;
	if(drctn>=90)
		drctn=90-(drctn-90);
	Units[s].DistanceToLeader=(wrk_x)/(CosDegrees(drctn));

	}

//============================================================================
// 着艦パターンをＰｐ＿ｘｙにセットします。
//----------------------------------------------------------------------------
[Original("set_pos_of_take_down")]
public void	SetLandingDestination(int n)
	{
	double			angl=0 /* C4701 */,angl2,dstc;
	int				pt,pos_of_no,a,b,c;

	pt=Units[n].Carrier;

	if( !Units[pt].IsUsed )
		{
		Units[n].PathX[0]=Units[n].Position.X+(double)(Random(400)-200);
		Units[n].PathY[0]=Units[n].Position.Y+(double)(Random(400)-200);

		Units[n].PathX[1]=MAP_RIGHT+1;
		Units[n].IsStopping=false;

		return;
		}

	switch((int)(Units[pt].Direction/22.5))
		{
		case 0: case 15:
			angl=0.0;
			break;
		case 1: case 2:
			angl=45.0;
			break;
		case 3: case 4:
			angl=90.0;
			break;
		case 5: case 6:
			angl=90.0+45.0;
			break;
		case 7: case 8:
			angl=180.0;
			break;
		case 9: case 10:
			angl=180.0+45.0;
			break;
		case 11: case 12:
			angl=270.0;
			break;
		case 13: case 14:
			angl=270.0+45.0;
			break;
		}

	angl2=angl;
	angl2-=180.0;	dstc=200;

	if( angl<0 )
		angl=360+angl;

	if( Units[n].Kind==UnitKind.Bomber )
		{
		Units[n].PathX[0]=Units[pt].Position.X+CosDegrees(angl2)*560.0;
		Units[n].PathY[0]=Units[pt].Position.Y+SinDegrees(angl2)*560.0;
		}
	else
		{
		Units[n].PathX[0]=Units[pt].Position.X+CosDegrees(angl2)*280.0;
		Units[n].PathY[0]=Units[pt].Position.Y+SinDegrees(angl2)*280.0;
		}

	Units[n].PathX[1]=Units[pt].Position.X+CosDegrees(angl2)*130.0;
	Units[n].PathY[1]=Units[pt].Position.Y+SinDegrees(angl2)*130.0;

	Units[n].PathX[2]=Units[pt].Position.X+CosDegrees(angl2)*10.0;
	Units[n].PathY[2]=Units[pt].Position.Y+SinDegrees(angl2)*10.0;

	Units[n].PathX[3]=Units[pt].Position.X+CosDegrees(angl)*100.0;
	Units[n].PathY[3]=Units[pt].Position.Y+SinDegrees(angl)*100.0;

	Units[n].PathX[4]=MAP_RIGHT+1;
	Units[n].IsStopping=false;
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cont_pos_of_take_down")]
public void	UpdateLanding( int n )
	{
	int		i;
	double			angl=0 /* C4701 */,angl2,dstc;
	int				pt,pos_of_no,a,b,c;

	pt=Units[n].Carrier;

	if( !(Units[pt].Kind==UnitKind.Carrier || Units[pt].Kind==UnitKind.LightCarrier || Units[pt].Kind==UnitKind.AirBase) )
		{
		Units[n].Carrier=0;
		pt=0;
		}

	if(  !Units[pt].IsUsed || pt==0 )
		{
		// もどる場所がない場合、あった場所で適当に動く
		Units[n].PathX[0]=Units[n].Position.X+(double)(Random(400)-200);
		Units[n].PathY[0]=Units[n].Position.Y+(double)(Random(400)-200);

		Units[n].PathX[1]=MAP_RIGHT+1;
		Units[n].IsStopping=false;

		return;
		}

	switch((int)(Units[pt].Direction/22.5))
		{
		case 0: case 15:
			angl=0.0;
			break;
		case 1: case 2:
			angl=45.0;
			break;
		case 3: case 4:
			angl=90.0;
			break;
		case 5: case 6:
			angl=90.0+45.0;
			break;
		case 7: case 8:
			angl=180.0;
			break;
		case 9: case 10:
			angl=180.0+45.0;
			break;
		case 11: case 12:
			angl=270.0;
			break;
		case 13: case 14:
			angl=270.0+45.0;
			break;
		}

	angl2=angl;
	angl2-=180.0;	dstc=200;

	if( angl<0 )
		angl=360+angl;

	for( i=0; Units[n].PathX[i]!=MAP_RIGHT+1; i++){}

	switch( i )
		{
		case 4:
			// 全4点を調整
			if( Units[n].Kind==UnitKind.Bomber )
				{
				Units[n].PathX[0]=Units[pt].Position.X+CosDegrees(angl2)*560.0;
				Units[n].PathY[0]=Units[pt].Position.Y+SinDegrees(angl2)*560.0;
				}
			else
				{
				Units[n].PathX[0]=Units[pt].Position.X+CosDegrees(angl2)*280.0;
				Units[n].PathY[0]=Units[pt].Position.Y+SinDegrees(angl2)*280.0;
				}

			Units[n].PathX[1]=Units[pt].Position.X+CosDegrees(angl2)*130.0;
			Units[n].PathY[1]=Units[pt].Position.Y+SinDegrees(angl2)*130.0;

			Units[n].PathX[2]=Units[pt].Position.X+CosDegrees(angl2)*10.0;
			Units[n].PathY[2]=Units[pt].Position.Y+SinDegrees(angl2)*10.0;

			Units[n].PathX[3]=Units[pt].Position.X+CosDegrees(angl)*100.0;
			Units[n].PathY[3]=Units[pt].Position.Y+SinDegrees(angl)*100.0;
			break;

		case 3:
			// 全3点を調整
			Units[n].PathX[0]=Units[pt].Position.X+CosDegrees(angl2)*130.0;
			Units[n].PathY[0]=Units[pt].Position.Y+SinDegrees(angl2)*130.0;

			Units[n].PathX[1]=Units[pt].Position.X+CosDegrees(angl2)*10.0;
			Units[n].PathY[1]=Units[pt].Position.Y+SinDegrees(angl2)*10.0;

			Units[n].PathX[2]=Units[pt].Position.X+CosDegrees(angl)*100.0;
			Units[n].PathY[2]=Units[pt].Position.Y+SinDegrees(angl)*100.0;
			break;

		case 2:
			// 全2点を調整
			Units[n].PathX[0]=Units[pt].Position.X+CosDegrees(angl2)*10.0;
			Units[n].PathY[0]=Units[pt].Position.Y+SinDegrees(angl2)*10.0;

			Units[n].PathX[1]=Units[pt].Position.X+CosDegrees(angl)*100.0;
			Units[n].PathY[1]=Units[pt].Position.Y+SinDegrees(angl)*100.0;
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
	int		q_size,q_size2;
	double	wrk_x,wrk_y,dstc,drctn;

	// 現地点からユニット地点への距離
	wrk_x=Units[n].Position.X-Units[m].Position.X;
	wrk_y=Units[n].Position.Y-Units[m].Position.Y;
	if(wrk_x==0)	wrk_x=1;
	if(wrk_y==0)	wrk_y=1;

	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;
	if(wrk_x<0)
		wrk_x=0-wrk_x;
	if(wrk_y<0)
		wrk_y=0-wrk_y;
	if(drctn>=180)
		drctn=drctn-180;
	if(drctn>=90)
		drctn=90-(drctn-90);

	q_size=((int)((wrk_x)/(CosDegrees(drctn))));

	if( q_size<100 )
		q_size=100;

	if( Random( ( Units[m].Variant==1 ? q_size : q_size )*( 1+(Units[n].Speed==0 ? 1 : 0)*4) )==2 )
		{	// 敵潜水艦探知！
		if( q_size>150 )
			q_size=150;

		q_size2=(int)(q_size*( Units[m].Variant==0 ? 0.60 : 0.50 ));

		Units[n].ContactX=(int)(Units[n].Position.X+Random((q_size2)*2)-q_size2);
		Units[n].ContactY=(int)(Units[n].Position.Y+Random((q_size2)*2)-q_size2);

		Units[n].ContactRadius=q_size;
		Units[n].ContactTime=250+Random(150);
		return(1);
		}
	else
		{
		return(0);
		}
	}

//============================================================================
// 可視不可視
//----------------------------------------------------------------------------
[Original("find_out")]
public void		Detect()
	{
	int		m,n,size,flg1,flg2,s;
	double	wrk_x,wrk_y,dstc,drctn;

	if( RevealsAll!=0 || Result!=GameResult.None )
		{
		for(m=0;m<=MaxUnitId;m++)
			Units[m].IsFound=true;
		return;
		}

	for(m=0;m<=MaxUnitId;m++)
		{
		ref var unit = ref Units[m];
		if( unit.ContactTime!=0 )
			unit.ContactTime--;
		else
			unit.IsFound=false;
		}

	// ユニットの見え隠れ
	for(m=1;m<=MaxUnitId;m++)
		{
		ref var unit = ref Units[m];
		if( unit.IsUsed && unit.Side==LocalSide  && unit.PlaneState!=UnitState.Parked  && !unit.IsSupplying )
			{
			flg1=0;
			for(n=1;n<=MaxUnitId;n++)
				{
				ref var other = ref Units[n];
				if( other.IsUsed && other.Side!=LocalSide && other.PlaneState!=UnitState.Parked && (!other.IsFound || unit.Kind==UnitKind.Submarine ) )
					{
					// 現地点からユニット地点への距離
					wrk_x=unit.Position.X;
					wrk_y=unit.Position.Y;

					if( unit.Kind==UnitKind.Fighter )
						{	// 航空機の場合はちょっと前へ
						wrk_x+=CosDegrees(unit.Direction)*FT_EYE;
						wrk_y+=SinDegrees(unit.Direction)*FT_EYE;
						}
					wrk_x=other.Position.X-wrk_x;
					wrk_y=other.Position.Y-wrk_y;

					if(wrk_x==0)	wrk_x=1;
					if(wrk_y==0)	wrk_y=1;
					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					if(wrk_x<0)
						wrk_x=0-wrk_x;
					if(wrk_y<0)
						wrk_y=0-wrk_y;
					if(drctn>=180)
						drctn=drctn-180;
					if(drctn>=90)
						drctn=90-(drctn-90);

					size=GetDetectionSize(m,n);
					dstc=(wrk_x)/(CosDegrees(drctn));

					if( (int)dstc<=size && size!=0 )
						{
						flg1++;
						if( other.Kind==UnitKind.Submarine && other.IsSubmerged )
							{	// 潜航中潜水艦が発見可能範囲にいる
							if( DetectSubmarines(m,n)!=0 )
								{
								other.IsFound=true;

								}
							}
						else
							{
							other.IsFound=true;	// 普通のユニットが見つかった場合

							if( unit.Kind==UnitKind.Submarine && !unit.IsSupplying && other.Kind!=UnitKind.Submarine )		// 発見したのが潜水艦の場合。
								{
								unit.IsSubmerged=true;		// 潜ります。
								}
							}
						}
					}
				}

			if( flg1==0 && unit.Kind==UnitKind.Submarine )		// 潜水艦が発見できなかった
				{
				unit.IsSubmerged=false;					// 浮上します。
				unit.IsFound=false;					// クリアします。
				unit.ContactTime=0;
				}

			}

		if( unit.IsUsed && unit.Side!=LocalSide  && unit.PlaneState!=UnitState.Parked  && !unit.IsSupplying )
			{
			flg2=0;
			for(n=1;n<=MaxUnitId;n++)
				{
				ref var other = ref Units[n];
				if( other.IsUsed && other.Side==LocalSide  && other.PlaneState!=UnitState.Parked && (!other.IsFound || unit.Kind==UnitKind.Submarine) )
					{
					// 現地点からユニット地点への距離

					wrk_x=unit.Position.X;
					wrk_y=unit.Position.Y;

					if( unit.Kind==UnitKind.Fighter )
						{	// 航空機の場合はちょっと前へ
						wrk_x+=CosDegrees(unit.Direction)*FT_EYE;
						wrk_y+=SinDegrees(unit.Direction)*FT_EYE;
						}
					wrk_x=other.Position.X-wrk_x;
					wrk_y=other.Position.Y-wrk_y;

					if(wrk_x==0)	wrk_x=1;
					if(wrk_y==0)	wrk_y=1;

					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					if(wrk_x<0)
						wrk_x=0-wrk_x;
					if(wrk_y<0)
						wrk_y=0-wrk_y;
					if(drctn>=180)
						drctn=drctn-180;
					if(drctn>=90)
						drctn=90-(drctn-90);

					size=GetDetectionSize(m,n);
					dstc=(wrk_x)/(CosDegrees(drctn));

					if( (int)dstc<=size && size!=0 )
						{
						flg2++;
						if( other.Kind==UnitKind.Submarine && other.IsSubmerged )
							{	// 潜航中潜水艦が発見可能範囲にいる
							if( DetectSubmarines(m,n)!=0 )
								{
								other.IsFound=true;

								}
							}
						else
							{	// 普通のユニットが見つかった場合
							other.IsFound=true;

							if( unit.Kind==UnitKind.Submarine && !unit.IsSupplying && other.Kind!=UnitKind.Submarine )		// 発見したのが潜水艦の場合。
								{
								unit.IsSubmerged=true;
								}
							}
						}
					}
				}

			if( flg2==0 && unit.Kind==UnitKind.Submarine )		// 潜水艦が発見できなかった
				{
				unit.IsSubmerged=false;					// 浮上します
				unit.IsFound=false;					// クリアします。
				unit.ContactTime=0;
				}

			}
		}
	}

}
