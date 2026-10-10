using System.Runtime.CompilerServices;

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

// Port of draw_cmbt_area.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cont_upper_effect")]
public void	UpdateUpperEffects(RECT* pfield_rect,RECT* pinfo_rect)
	{
	RECT src_rect,dstn_rect;
	int	m,n,flg,size=default /* C4701 */;
	double	wrk_x,wrk_y,drctn,dstc;

	//=========		 Ｕｐｐｅｒのエフェクト描画		=========//
	for( m=1; m<EFFECT_MAX; m++ )
		{
		ref var effect = ref Effects[m];
		if( effect.Layer==EffectLayer.Upper )
			{
			flg=0;
			if(Result!=GameResult.None)
				flg=1;
			for(n=1;n<=MaxUnitId && flg==0 ;n++)
				{
				if( Units[n].IsUsed && Units[n].Side==LocalSide && Units[n].PlaneState!=UnitState.Parked )
					{
					// マイユニットからこのエフェクトが見えるか
					// 現地点からユニット地点への距離
					wrk_x=Units[n].Position.X;
					wrk_y=Units[n].Position.Y;
					if( Units[n].Kind==UnitKind.Fighter )
						{	// 航空機の場合はちょっと前へ
						wrk_x+=CosDegrees(Units[n].Direction)*FT_EYE;
						wrk_y+=SinDegrees(Units[n].Direction)*FT_EYE;
						}

					wrk_x=wrk_x-effect.Position.X;
					wrk_y=wrk_y-effect.Position.Y;

					dstc=Distance(wrk_x, wrk_y);

					switch( Units[n].Kind )
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

					if( Units[n].Kind>=UnitKind.AirBase && Units[n].Kind<=UnitKind.Fortress && Units[n].BuildTime!=0 )
						{
						// 工事中は視界を制限
						size=FT1_SIGHT/2;
						}

					if( dstc<=size )
						{
						flg=1;
						}
					}
				}

			if( flg!=0 )
				{

				// マイユニットから見えるので表示
				switch( effect.Animation )
					{
					case 0:		//
						Sprites[SpriteId.SubUnits].Frame=effect.SpriteNumber;
						Sprites[SpriteId.SubUnits].X=(int)(effect.Position.X-CameraPosition.X);
						Sprites[SpriteId.SubUnits].Y=(int)(CameraPosition.Y-effect.Position.Y);
						n=1;
						break;
					case 1:		// 対空機関砲弾がヒット
						Sprites[SpriteId.SubUnits].Frame=effect.SpriteNumber+(Sprites[SpriteId.SubUnits].FramesPerRow*(effect.TimeLeft%2));
						Sprites[SpriteId.SubUnits].X=(int)(effect.Position.X-CameraPosition.X);
						Sprites[SpriteId.SubUnits].Y=(int)(CameraPosition.Y-effect.Position.Y);
						n=1;
						break;
					case 2:		//	飛行機からの煙
						Sprites[SpriteId.SubUnits].Frame=effect.SpriteNumber+(Sprites[SpriteId.SubUnits].FramesPerRow*(SharedRandom(2)));
						Sprites[SpriteId.SubUnits].X=(int)(effect.Position.X-CameraPosition.X)+SharedRandom(10)-5;
						Sprites[SpriteId.SubUnits].Y=(int)(CameraPosition.Y-effect.Position.Y)+SharedRandom(10)-5;
						n=1;
						break;
					case 3:		//	駐機場の飛行機用
						Sprites[SpriteId.SubUnits].Frame=effect.SpriteNumber;
						Sprites[SpriteId.SubUnits].X=(int)(effect.Position.X);
						Sprites[SpriteId.SubUnits].Y=(int)(effect.Position.Y);
						n=0;
						break;
					case 4:		// 雷跡
						Sprites[SpriteId.SubUnits].Frame=effect.SpriteNumber+(Sprites[SpriteId.SubUnits].FramesPerRow*(SharedRandom(2)));
						Sprites[SpriteId.SubUnits].X=(int)(effect.Position.X-CameraPosition.X);
						Sprites[SpriteId.SubUnits].Y=(int)(CameraPosition.Y-effect.Position.Y);
						n=1;
						break;
					case 10:	// 対空機関砲
						DrawLine5((int)(effect.Position.X-CameraPosition.X),(int)(CameraPosition.Y-effect.Position.Y),(int)(effect.EndPosition.X-CameraPosition.X),(int)(CameraPosition.Y-effect.EndPosition.Y),CMBT_WIDTH-1,CMBT_HEIGHT-1,255);
						DrawLine5((int)(effect.Position.X-CameraPosition.X)+1,(int)(CameraPosition.Y-effect.Position.Y),(int)(effect.EndPosition.X-CameraPosition.X)+1,(int)(CameraPosition.Y-effect.EndPosition.Y),CMBT_WIDTH-1,CMBT_HEIGHT-1,255);
						DrawLine5((int)(effect.Position.X-CameraPosition.X),(int)(CameraPosition.Y-effect.Position.Y)-1,(int)(effect.EndPosition.X-CameraPosition.X),(int)(CameraPosition.Y-effect.EndPosition.Y)-1,CMBT_WIDTH-1,CMBT_HEIGHT-1,255);
						continue;
					case 11:	// 弾丸
						DrawLine5((int)(effect.Position.X-CameraPosition.X),(int)(CameraPosition.Y-effect.Position.Y),(int)(effect.EndPosition.X-CameraPosition.X),(int)(CameraPosition.Y-effect.EndPosition.Y),CMBT_WIDTH-1,CMBT_HEIGHT-1,255);
						continue;
					}
				// src_rect は ソースサーフェスのレクタングルです。
				src_rect.left = Sprites[SpriteId.SubUnits].SheetX+(Sprites[SpriteId.SubUnits].Width * (Sprites[SpriteId.SubUnits].Frame % Sprites[SpriteId.SubUnits].FramesPerRow)) +1;
				src_rect.top = Sprites[SpriteId.SubUnits].SheetY+(Sprites[SpriteId.SubUnits].Height* (Sprites[SpriteId.SubUnits].Frame / Sprites[SpriteId.SubUnits].FramesPerRow)) +1;
				src_rect.right = (src_rect.left + Sprites[SpriteId.SubUnits].Width)-1;
				src_rect.bottom = (src_rect.top + Sprites[SpriteId.SubUnits].Height)-1;
				// dstn_rect は ディスティネーションレクタングルです。
				dstn_rect.left=Sprites[SpriteId.SubUnits].X-Sprites[SpriteId.SubUnits].CenterX;
				dstn_rect.top=Sprites[SpriteId.SubUnits].Y-Sprites[SpriteId.SubUnits].CenterY;
				dstn_rect.right=dstn_rect.left+Sprites[SpriteId.SubUnits].Width-1;
				dstn_rect.bottom=dstn_rect.top+Sprites[SpriteId.SubUnits].Height-1;

				if( n!=0 )
					{
					if( ClipRects(ref dstn_rect,ref src_rect,ref *pfield_rect)!=0 )
						{

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,DDBLTFAST_SRCCOLORKEY) )
		{
		RestoreSurfaces();
		}

						}
					}
				else
					{
					if( ClipRects(ref dstn_rect,ref src_rect,ref *pinfo_rect)!=0 )
						{

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,DDBLTFAST_SRCCOLORKEY) )
		{
		RestoreSurfaces();
		}

						}
					}
				}
			}
		}
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cont_lower_effect")]
public void UpdateLowerEffects(RECT* pfield_rect,RECT* pinfo_rect)
	{
	RECT src_rect,dstn_rect;
	int	m,n=default /* C4701 */,size=default /* C4701 */,flg;
	double	wrk_x,wrk_y,drctn,dstc;

	//=========		 Ｌｏｗｅｒのエフェクト描画		=========//
	for(m=1;m<EFFECT_MAX;m++)
		{
		ref var effect = ref Effects[m];
		if( effect.Layer==EffectLayer.Lower )
			{
			if( effect.Animation!=3 || Result!=GameResult.None )
				{
				// ユニットインフォ画面以外のエフェクト表示は見える見えないのテストをします。
				flg=0;
				if(Result!=GameResult.None)
					flg=1;
				for(n=1;n<=MaxUnitId;n++)
					{
					ref var unit = ref Units[n];
					if( unit.IsUsed && unit.Side==LocalSide && unit.PlaneState!=UnitState.Parked )
						{
						// マイユニットからこのエフェクトが見えるか
						// 現地点からユニット地点への距離
						wrk_x=unit.Position.X;
						wrk_y=unit.Position.Y;
						if( unit.Kind==UnitKind.Fighter )
							{	// 航空機の場合はちょっと前へ
							wrk_x+=CosDegrees(unit.Direction)*FT_EYE;
							wrk_y+=SinDegrees(unit.Direction)*FT_EYE;
							}

						wrk_x=wrk_x-effect.Position.X;
						wrk_y=wrk_y-effect.Position.Y;

						dstc=Distance(wrk_x, wrk_y);

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

						if( dstc<=size)
							{
							flg=1;
							}
						}
					}
				}
			else
				{
				// ユニットインフォ内のエフェクトは必ず表示します。
				flg=1;
				}

			if( flg!=0 )
				{
				// マイユニットからの見える
				switch( effect.Animation )
					{
					case 0:		//
						Sprites[SpriteId.SubUnits].Frame=effect.SpriteNumber;
						Sprites[SpriteId.SubUnits].X=(int)(effect.Position.X-CameraPosition.X);
						Sprites[SpriteId.SubUnits].Y=(int)(CameraPosition.Y-effect.Position.Y);
						n=1;
						break;
					case 1:		// 対空機関砲弾がヒット
						Sprites[SpriteId.SubUnits].Frame=effect.SpriteNumber+(Sprites[SpriteId.SubUnits].FramesPerRow*(effect.TimeLeft%2));
						Sprites[SpriteId.SubUnits].X=(int)(effect.Position.X-CameraPosition.X);
						Sprites[SpriteId.SubUnits].Y=(int)(CameraPosition.Y-effect.Position.Y);
						n=1;
						break;
					case 2:		//	飛行機からの煙
						Sprites[SpriteId.SubUnits].Frame=effect.SpriteNumber+(Sprites[SpriteId.SubUnits].FramesPerRow*(SharedRandom(2)));
						Sprites[SpriteId.SubUnits].X=(int)(effect.Position.X-CameraPosition.X)+SharedRandom(10)-5;
						Sprites[SpriteId.SubUnits].Y=(int)(CameraPosition.Y-effect.Position.Y)+SharedRandom(10)-5;
						n=1;
						break;
					case 3:		//	駐機場の飛行機用
						Sprites[SpriteId.SubUnits].Frame=effect.SpriteNumber;
						Sprites[SpriteId.SubUnits].X=(int)(effect.Position.X);
						Sprites[SpriteId.SubUnits].Y=(int)(effect.Position.Y);
						n=0;
						break;
					case 4:		// 雷跡、航跡
						Sprites[SpriteId.SubUnits].Frame=effect.SpriteNumber+(Sprites[SpriteId.SubUnits].FramesPerRow*(SharedRandom(2)));
						Sprites[SpriteId.SubUnits].X=(int)(effect.Position.X-CameraPosition.X);
						Sprites[SpriteId.SubUnits].Y=(int)(CameraPosition.Y-effect.Position.Y);
						n=1;
						break;

					case 12:	// 矩形
						DrawLine5((int)(effect.Position.X-effect.EndPosition.X-CameraPosition.X),(int)(CameraPosition.Y-effect.Position.Y-effect.EndPosition.Y),(int)(effect.Position.X+effect.EndPosition.X-CameraPosition.X),(int)(CameraPosition.Y-effect.Position.Y-effect.EndPosition.Y),CMBT_WIDTH-1,CMBT_HEIGHT-1,255);
						DrawLine5((int)(effect.Position.X+effect.EndPosition.X-CameraPosition.X),(int)(CameraPosition.Y-effect.Position.Y-effect.EndPosition.Y),(int)(effect.Position.X+effect.EndPosition.X-CameraPosition.X),(int)(CameraPosition.Y-effect.Position.Y+effect.EndPosition.Y),CMBT_WIDTH-1,CMBT_HEIGHT-1,255);
						DrawLine5((int)(effect.Position.X-effect.EndPosition.X-CameraPosition.X),(int)(CameraPosition.Y-effect.Position.Y+effect.EndPosition.Y),(int)(effect.Position.X+effect.EndPosition.X-CameraPosition.X),(int)(CameraPosition.Y-effect.Position.Y+effect.EndPosition.Y),CMBT_WIDTH-1,CMBT_HEIGHT-1,255);
						DrawLine5((int)(effect.Position.X-effect.EndPosition.X-CameraPosition.X),(int)(CameraPosition.Y-effect.Position.Y+effect.EndPosition.Y),(int)(effect.Position.X-effect.EndPosition.X-CameraPosition.X),(int)(CameraPosition.Y-effect.Position.Y-effect.EndPosition.Y),CMBT_WIDTH-1,CMBT_HEIGHT-1,255);
						continue;
					}
				// src_rect は ソースサーフェスのレクタングルです。
				src_rect.left = Sprites[SpriteId.SubUnits].SheetX+(Sprites[SpriteId.SubUnits].Width * (Sprites[SpriteId.SubUnits].Frame % Sprites[SpriteId.SubUnits].FramesPerRow)) +1;
				src_rect.top = Sprites[SpriteId.SubUnits].SheetY+(Sprites[SpriteId.SubUnits].Height* (Sprites[SpriteId.SubUnits].Frame / Sprites[SpriteId.SubUnits].FramesPerRow)) +1;
				src_rect.right = (src_rect.left + Sprites[SpriteId.SubUnits].Width)-1;
				src_rect.bottom = (src_rect.top + Sprites[SpriteId.SubUnits].Height)-1;
				// dstn_rect は ディスティネーションレクタングルです。
				dstn_rect.left=Sprites[SpriteId.SubUnits].X-Sprites[SpriteId.SubUnits].CenterX;
				dstn_rect.top=Sprites[SpriteId.SubUnits].Y-Sprites[SpriteId.SubUnits].CenterY;
				dstn_rect.right=dstn_rect.left+Sprites[SpriteId.SubUnits].Width-1;
				dstn_rect.bottom=dstn_rect.top+Sprites[SpriteId.SubUnits].Height-1;

				if( n!=0 )
					{
					if( ClipRects(ref dstn_rect,ref src_rect,ref *pfield_rect)!=0 )
						{
						if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,DDBLTFAST_SRCCOLORKEY) )
							{
							RestoreSurfaces();
							}

						}
					}
				else
					{
					if( ClipRects(ref dstn_rect,ref src_rect,ref *pinfo_rect)!=0 )
						{
						if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,DDBLTFAST_SRCCOLORKEY) )
							{
							RestoreSurfaces();
							}
						}
					}
				}
			}
		}
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("draw_cloud")]
public void	DrawClouds(RECT* pfield_rect)
	{
	int	n;
	RECT src_rect,dstn_rect;

	for(n=0;n<KUMO_MAX;n++)
		{
		ref var cloud = ref Clouds[n];
		if( cloud.Used!=0 )
			{
			Sprites[SpriteId.MapTiles].X=(int)(cloud.Position.X-CameraPosition.X);
			Sprites[SpriteId.MapTiles].Y=(int)(CameraPosition.Y-cloud.Position.Y);
			Sprites[SpriteId.MapTiles].Frame=2;
			// src_rect は ソースサーフェスのレクタングルです。
			src_rect.left = Sprites[SpriteId.MapTiles].SheetX+(Sprites[SpriteId.MapTiles].Width * (Sprites[SpriteId.MapTiles].Frame % Sprites[SpriteId.MapTiles].FramesPerRow));
			src_rect.top = Sprites[SpriteId.MapTiles].SheetY+(Sprites[SpriteId.MapTiles].Height* (Sprites[SpriteId.MapTiles].Frame / Sprites[SpriteId.MapTiles].FramesPerRow)) ;
			src_rect.right = (src_rect.left + Sprites[SpriteId.MapTiles].Width);
			src_rect.bottom = (src_rect.top + Sprites[SpriteId.MapTiles].Height);

			// dstn_rect は ディスティネーションレクタングルです。
			dstn_rect.left=Sprites[SpriteId.MapTiles].X-(Sprites[SpriteId.MapTiles].Width/2);
			dstn_rect.top=Sprites[SpriteId.MapTiles].Y-(Sprites[SpriteId.MapTiles].Height/2);
			dstn_rect.right=dstn_rect.left+Sprites[SpriteId.MapTiles].Width;
			dstn_rect.bottom=dstn_rect.top+Sprites[SpriteId.MapTiles].Height;

			if( ClipRects(ref dstn_rect,ref src_rect,ref *pfield_rect)!=0 )
				{

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,DDBLTFAST_SRCCOLORKEY ) )
		{
		RestoreSurfaces();
		}

				}
			}
		}
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("be_dstryd")]
public void	DrawDestruction(int m)
	{
	int	f,i;

	if( !(Units[m].Kind>=UnitKind.AirBase && Units[m].Kind<=UnitKind.Fortress ) )
		{
		// 沈没の水門
		f=FindFreeEffect();
		Effects[f].Layer=EffectLayer.Lower;
		Effects[f].TimeLeft=220;
		Effects[f].Animation=0;
		Effects[f].Position=Units[m].Position;
		Effects[f].SpriteNumber=7;			// ソースファイル上の番号

		}

	if( Units[m].Kind==UnitKind.Battleship || Units[m].Kind==UnitKind.Carrier || Units[m].Kind==UnitKind.LightCarrier || Units[m].Kind==UnitKind.NavalBase || Units[m].Kind==UnitKind.AirBase || Units[m].Kind==UnitKind.Fortress )
		{
		// 大型艦船
		// 沈没の小水紋
		if( !(Units[m].Kind>=UnitKind.AirBase && Units[m].Kind<=UnitKind.Fortress ) )
			{
			for(i=0;i<=7;i++)
				{
				f=FindFreeEffect();
				Effects[f].Layer=EffectLayer.Lower;
				Effects[f].TimeLeft=200+Random(20);
				Effects[f].Animation=4;
				Effects[f].Position = new WorldPosition(Units[m].Position.X+Random(100)-50, Units[m].Position.Y+Random(100)-50);
				Effects[f].SpriteNumber=8;			// ソースファイル上の番号
				}
			}
		// 沈没の煙
		for(i=0;i<=2;i++)
			{
			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Upper;
			Effects[f].TimeLeft=150+Random(20);
			Effects[f].Animation=4;
			Effects[f].Position = new WorldPosition(Units[m].Position.X+Random(30)-15, Units[m].Position.Y+Random(30)-15);
			Effects[f].SpriteNumber=5+Random(2);			// ソースファイル上の番号
			}
		// 沈没の爆炎
		for(i=0;i<=4;i++)
			{
			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Upper;
			Effects[f].TimeLeft=30+Random(20);
			Effects[f].Animation=4;
			Effects[f].Position = new WorldPosition(Units[m].Position.X+Random(40)-20, Units[m].Position.Y+Random(40)-20);
			Effects[f].SpriteNumber=9;			// ソースファイル上の番号
			}
		// 沈没の小爆炎
		for(i=0;i<=4;i++)
			{
			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Upper;
			Effects[f].TimeLeft=40+Random(20);
			Effects[f].Animation=4;
			Effects[f].Position = new WorldPosition(Units[m].Position.X+Random(60)-30, Units[m].Position.Y+Random(60)-30);
			Effects[f].SpriteNumber=10;			// ソースファイル上の番号
			}
		}
	else
		{
		// 中小型艦船
		// 沈没の小水紋
		if( !(Units[m].Kind>=UnitKind.AirBase && Units[m].Kind<=UnitKind.Fortress ) )
			{
			for(i=0;i<=3;i++)
				{
				f=FindFreeEffect();
				Effects[f].Layer=EffectLayer.Lower;
				Effects[f].TimeLeft=200+Random(20);
				Effects[f].Animation=4;
				Effects[f].Position = new WorldPosition(Units[m].Position.X+Random(100)-50, Units[m].Position.Y+Random(100)-50);
				Effects[f].SpriteNumber=8;			// ソースファイル上の番号
				}
			}
		// 沈没の煙
		for(i=0;i<=1;i++)
			{
			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Upper;
			Effects[f].TimeLeft=150+Random(20);
			Effects[f].Animation=4;
			Effects[f].Position = new WorldPosition(Units[m].Position.X+Random(30)-15, Units[m].Position.Y+Random(30)-15);
			Effects[f].SpriteNumber=5+Random(2);			// ソースファイル上の番号
			}
		// 沈没の爆炎
		for(i=0;i<=0;i++)
			{
			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Upper;
			Effects[f].TimeLeft=30+Random(20);
			Effects[f].Animation=4;
			Effects[f].Position = new WorldPosition(Units[m].Position.X+Random(40)-20, Units[m].Position.Y+Random(40)-20);
			Effects[f].SpriteNumber=9;			// ソースファイル上の番号
			}
		// 沈没の小爆炎
		for(i=0;i<=1;i++)
			{
			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Upper;
			Effects[f].TimeLeft=20+Random(20);
			Effects[f].Animation=4;
			Effects[f].Position = new WorldPosition(Units[m].Position.X+Random(20)-10, Units[m].Position.Y+Random(20)-10);
			Effects[f].SpriteNumber=10;			// ソースファイル上の番号
			}

		}
	}

private void DrawMapTiles(int cm_scrn_y, int cm_scrn_x, int map_bld_y, int map_bld_x, ref Array256<byte> cBuf, ref RECT src_rect, ref RECT field_rect)
	{
	int m;
	int n;
	int flg;
	int f;
	double wrk_x;
	double wrk_y;
	double drctn;
	double dstc;
	RECT dstn_rect;
	for(m=0;m<=10;m++)
		{
		for(n=0;n<=10;n++)
			{
			if(  !IsEditingMap &&  Result==GameResult.None  && MapTiles[cm_scrn_y+n][cm_scrn_x+m]==0 )
				{
				flg=0;
				for( f=1; f<=MaxUnitId; f++)
					{
					if( Units[f].IsUsed && Units[f].Side==LocalSide && Units[f].PlaneState!=UnitState.Parked  )
						{
						// 現地点からユニット地点への距離

						if(Units[f].Kind==UnitKind.Fighter )
							{
							// 戦闘機場合、視点を
							wrk_x=Units[f].Position.X;
							wrk_y=Units[f].Position.Y;
							wrk_x+=CosDegrees(Units[f].Direction)*FT_EYE;
							wrk_y+=SinDegrees(Units[f].Direction)*FT_EYE;
							wrk_x=wrk_x-(((cm_scrn_x+m)*Sprites[SpriteId.MapTiles].Width)-MAP_RIGHT);
							wrk_y=wrk_y-(MAP_TOP-((cm_scrn_y+n)*Sprites[SpriteId.MapTiles].Height));
							}
						else
							{
							wrk_x=Units[f].Position.X-(((cm_scrn_x+m)*Sprites[SpriteId.MapTiles].Width)-MAP_RIGHT);
							wrk_y=Units[f].Position.Y-(MAP_TOP-((cm_scrn_y+n)*Sprites[SpriteId.MapTiles].Height));
							}

						//((cm_scrn_x+m)*sprt[MAP_TIP_NRML].wd)-MAP_RIGHT

						dstc=Distance(wrk_x, wrk_y);
						if( dstc<=(double)GetDetectionSize(f,0) )
							{
							flg=1;
							f=MaxUnitId;
							break;
							}
						}
					}
				}
			else
				{
				flg=1;
				}

			Sprites[SpriteId.MapTiles].X=(m*Sprites[SpriteId.MapTiles].Width);
			Sprites[SpriteId.MapTiles].Y=(n*Sprites[SpriteId.MapTiles].Height);

			Sprites[SpriteId.MapTiles].X-=(int)(CameraPosition.X-MAP_LEFT)%Sprites[SpriteId.MapTiles].Width;
			Sprites[SpriteId.MapTiles].Y-=(int)(MAP_TOP-CameraPosition.Y)%Sprites[SpriteId.MapTiles].Height;

			if( MapTiles[cm_scrn_y+n][cm_scrn_x+m]==0)
				{
				if(flg!=0)
					{
					// 見える範囲内の海
					Sprites[SpriteId.MapTiles].Frame=(FrameCount/30)%2;							// ただの海
					}
				else
					{
					//見えない範囲内の海
					Sprites[SpriteId.MapTiles].Frame=3;							// ただの海
					continue;
					}
				}

			if( MapTiles[cm_scrn_y+n][cm_scrn_x+m]>=1)
				{
				Sprites[SpriteId.MapTiles].Frame=6+(MapTiles[cm_scrn_y+n][cm_scrn_x+m]-1);	// 陸地
				}

			// マップエディット時のプログ
			if( IsEditingMap && (cm_scrn_y+n)==(map_bld_y) && (cm_scrn_x+m)==(map_bld_x) )
				{
				if ( (FrameCount%2)!=0 )
					Sprites[SpriteId.MapTiles].Frame=3;
				GetKeyboardState(cBuf);

				if( (cBuf[VK_NUMPAD1]&0x80)!=0 )
					{
					MapTiles[cm_scrn_y+n][cm_scrn_x+m]=1;
					}
				if( (cBuf[VK_NUMPAD2]&0x80)!=0 )
					{
					MapTiles[cm_scrn_y+n][cm_scrn_x+m]=2;
					}
				if( (cBuf[VK_NUMPAD3]&0x80)!=0 )
					{
					MapTiles[cm_scrn_y+n][cm_scrn_x+m]=3;
					}
				if( (cBuf[VK_NUMPAD4]&0x80)!=0 )
					{
					MapTiles[cm_scrn_y+n][cm_scrn_x+m]=4;
					}
				if( (cBuf[VK_NUMPAD5]&0x80)!=0 )
					{
					MapTiles[cm_scrn_y+n][cm_scrn_x+m]=5;
					}
				if( (cBuf[VK_NUMPAD6]&0x80)!=0 )
					{
					MapTiles[cm_scrn_y+n][cm_scrn_x+m]=6;
					}
				if( (cBuf[VK_NUMPAD7]&0x80)!=0 )
					{
					MapTiles[cm_scrn_y+n][cm_scrn_x+m]=7;
					}
				if( (cBuf[VK_NUMPAD8]&0x80)!=0 )
					{
					MapTiles[cm_scrn_y+n][cm_scrn_x+m]=8;
					}
				if( (cBuf[VK_NUMPAD9]&0x80)!=0 )
					{
					MapTiles[cm_scrn_y+n][cm_scrn_x+m]=9;
					}
				if( (cBuf[VK_NUMPAD0]&0x80)!=0 )
					{
					MapTiles[cm_scrn_y+n][cm_scrn_x+m]=0;
					}
				}

			// src_rect は ソースサーフェスのレクタングルです。
			src_rect.left = Sprites[SpriteId.MapTiles].SheetX+(Sprites[SpriteId.MapTiles].Width * (Sprites[SpriteId.MapTiles].Frame % Sprites[SpriteId.MapTiles].FramesPerRow)) ;
			src_rect.top = Sprites[SpriteId.MapTiles].SheetY+(Sprites[SpriteId.MapTiles].Height* (Sprites[SpriteId.MapTiles].Frame / Sprites[SpriteId.MapTiles].FramesPerRow)) ;
			src_rect.right = (src_rect.left + Sprites[SpriteId.MapTiles].Width);
			src_rect.bottom = (src_rect.top + Sprites[SpriteId.MapTiles].Height);

			// dstn_rect は ディスティネーションレクタングルです。
			dstn_rect.left=Sprites[SpriteId.MapTiles].X-Sprites[SpriteId.MapTiles].CenterX;
			dstn_rect.top=Sprites[SpriteId.MapTiles].Y-Sprites[SpriteId.MapTiles].CenterY;
			dstn_rect.right=dstn_rect.left+Sprites[SpriteId.MapTiles].Width;
			dstn_rect.bottom=dstn_rect.top+Sprites[SpriteId.MapTiles].Height;

			if( ClipRects(ref dstn_rect,ref src_rect,ref field_rect)!=0 )
				{
				if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,(RECT*)Unsafe.AsPointer(ref src_rect),0) )
					{
					RestoreSurfaces();
					}
				}
			}
		}
	}

private void HandleUnitClick(ref Unit unit, ref RECT dstn_rect, ref int lc_lf_btn, int m, SpriteId sprite)
	{
	int right;
	int bottom;
	int cl;
	int n;
	if( (FrameCount%2)!=0 )
		{
		if( unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked )
			{
			right=SCRN_WIDTH-1; bottom=SCRN_HEIGHT-1;
			}
		else
			{right=CMBT_WIDTH-1; bottom=CMBT_HEIGHT-1;}
		cl=0xffff;

		n=15;
		DrawLine4(dstn_rect.left+n,dstn_rect.top+n,dstn_rect.right-n,dstn_rect.top+n,right,bottom,RGB(255,255,255));
		DrawLine4(dstn_rect.right-n,dstn_rect.top+n,dstn_rect.right-n,dstn_rect.bottom-n,right,bottom,RGB(255,255,255));
		DrawLine4(dstn_rect.right-n,dstn_rect.bottom-n,dstn_rect.left+n,dstn_rect.bottom-n,right,bottom,RGB(255,255,255));
		DrawLine4(dstn_rect.left+n,dstn_rect.bottom-n,dstn_rect.left+n,dstn_rect.top+n,right,bottom,RGB(255,255,255));
		}

	if( lc_lf_btn==1 )
		{
		lc_lf_btn=0;
		MoveOrders[1].Unit=0;

		if( SelectedUnit==m )
			{
			SelectedUnit=0; CombatMenuKind=0; CombatMenuSelection=CombatMenuItem.None; ClearSelection2(1);
			}
		else
			{
			if( SelectedUnit==0 )
				{
				if( !(unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked) )
					PreviousSelectedUnit=(short)m;

				set_the_slct_unit( m );
				}
			else if(!IsEditingMap)
				{
				if( m!=0  )
					{
					// 陸地以外、普通の場合
					SelectOrders[1].IsSet=true;
					SelectOrders[1].SelectedUnit=SelectedUnit;
					SelectOrders[1].Unit=(short)m;
					}
				else
					{
					// 陸地指定
					SelectOrders[1].IsSet=true;
					SelectOrders[1].SelectedUnit=SelectedUnit;
					SelectOrders[1].Unit=0;
					SelectOrders[1].GroundPosition = new WorldPosition(CameraPosition.X+Sprites[sprite].X, CameraPosition.Y-Sprites[sprite].Y);
					}
				}
			}
		}
	}

private void DrawSelectedUnit(ref RECT dstn_rect, int right, int bottom)
	{
	int n;
	n=10;
	DrawLine4(dstn_rect.left+n,dstn_rect.top+n,dstn_rect.right-n,dstn_rect.top+n,right,bottom,RGB(255,255,255));
	DrawLine4(dstn_rect.right-n,dstn_rect.top+n,dstn_rect.right-n,dstn_rect.bottom-n,right,bottom,RGB(255,255,255));
	DrawLine4(dstn_rect.right-n,dstn_rect.bottom-n,dstn_rect.left+n,dstn_rect.bottom-n,right,bottom,RGB(255,255,255));
	DrawLine4(dstn_rect.left+n,dstn_rect.bottom-n,dstn_rect.left+n,dstn_rect.top+n,right,bottom,RGB(255,255,255));
	}

private void DrawLandingPoint(SpriteId sprite)
	{
	int cl;
	int n;
	RECT dstn_rect;
	cl=0x1f;
	n=10+(FrameCount%8)*2;

	dstn_rect.left=(int)(Units[SelectedUnit].LandingX-CameraPosition.X-40);
	dstn_rect.top=(int)(CameraPosition.Y-Units[SelectedUnit].LandingY-40);
	dstn_rect.right=dstn_rect.left+Sprites[sprite].Width-2;
	dstn_rect.bottom=dstn_rect.top+Sprites[sprite].Height-2;

	DrawLine4(dstn_rect.left+n,dstn_rect.top+n,dstn_rect.right-n,dstn_rect.top+n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
	DrawLine4(dstn_rect.right-n,dstn_rect.top+n,dstn_rect.right-n,dstn_rect.bottom-n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
	DrawLine4(dstn_rect.right-n,dstn_rect.bottom-n,dstn_rect.left+n,dstn_rect.bottom-n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
	DrawLine4(dstn_rect.left+n,dstn_rect.bottom-n,dstn_rect.left+n,dstn_rect.top+n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
	}

private void DrawTargetLine(ref Unit unit)
	{
	double wrk_x3;
	double wrk_y3;
	double wrk_x;
	double wrk_y;
	double drctn;
	double wrk_x2;
	double wrk_y2;
	double drctn2;
	int cl;
	if(  0!=0 && Units[unit.Target].Kind==UnitKind.Submarine && Units[unit.Target].IsSubmerged)
		{	// 対潜水艦
		wrk_x3=Units[unit.Target].ContactX;
		wrk_y3=Units[unit.Target].ContactY;
		}
	else
		{	// 対潜航潜水艦以外
		if( unit.Kind!=UnitKind.Transport )
			{
			wrk_x3=Units[unit.Target].Position.X;
			wrk_y3=Units[unit.Target].Position.Y;
			}
		else
			{
			// 揚陸方向
			wrk_x3=(double)unit.LandingX;
			wrk_y3=(double)unit.LandingY;
			}
		}

	if( (Units[unit.Target].IsFound || (unit.Target==MaxUnitId+1&&unit.Kind==UnitKind.Transport) )  && !(Units[unit.Target].Kind==UnitKind.Submarine && Units[unit.Target].IsSubmerged)   )
		{	// 視認
		wrk_x=wrk_x3-unit.Position.X;
		wrk_y=wrk_y3-unit.Position.Y;
		if( wrk_x==0 )	wrk_x=1;
		if( wrk_y==0 )	wrk_y=1;
		drctn=Direction(wrk_x, wrk_y);

		wrk_x=unit.Position.X;
		wrk_y=unit.Position.Y;
		wrk_x+=CosDegrees(drctn)*40;
		wrk_y+=SinDegrees(drctn)*40;

		wrk_x2=unit.Position.X;
		wrk_y2=unit.Position.Y;
		drctn2=drctn;
		drctn2-=10;
		if(drctn2<0)
			drctn2=360+drctn2;
		wrk_x2+=CosDegrees(drctn2)*20;
		wrk_y2+=SinDegrees(drctn2)*20;
		cl=0x1f;
		DrawLine4((int)(wrk_x-CameraPosition.X),(int)(CameraPosition.Y-wrk_y),(int)(wrk_x2-CameraPosition.X),(int)(CameraPosition.Y-wrk_y2),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));

		wrk_x2=unit.Position.X;
		wrk_y2=unit.Position.Y;
		drctn2=drctn;
		drctn2-=350;
		if(drctn2<0)
			drctn2=360+drctn2;
		wrk_x2+=CosDegrees(drctn2)*20;
		wrk_y2+=SinDegrees(drctn2)*20;
		cl=0x1f;
		DrawLine4((int)(wrk_x-CameraPosition.X),(int)(CameraPosition.Y-wrk_y),(int)(wrk_x2-CameraPosition.X),(int)(CameraPosition.Y-wrk_y2),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
		}
	else
		{	// 視認不可
		wrk_x=unit.Position.X+10+20;
		wrk_y=unit.Position.Y+40;
		wrk_x2=unit.Position.X-10+20;
		wrk_y2=unit.Position.Y+20;
		cl=0x1f;
		DrawLine4((int)(wrk_x-CameraPosition.X),(int)(CameraPosition.Y-wrk_y),(int)(wrk_x2-CameraPosition.X),(int)(CameraPosition.Y-wrk_y2),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
		wrk_x=unit.Position.X-10+20;
		wrk_y=unit.Position.Y+40;
		wrk_x2=unit.Position.X+10+20;
		wrk_y2=unit.Position.Y+20;
		cl=0x1f;
		DrawLine4((int)(wrk_x-CameraPosition.X),(int)(CameraPosition.Y-wrk_y),(int)(wrk_x2-CameraPosition.X),(int)(CameraPosition.Y-wrk_y2),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
		}
	}

private void DrawPathAndSetDestination(ref Unit unit, SpriteId sprite, int lc_lf_btn, ref int lc_ri_btn, int m)
	{
	int n;
	int cl;
	int right;
	int bottom;
	if( unit.EmergencyFlags[0]!=0 )
		{
		DrawLine5((int)(unit.Position.X-CameraPosition.X),(int)(CameraPosition.Y-unit.Position.Y),(int)(unit.EmergencyDestination.X-CameraPosition.X),(int)(CameraPosition.Y-unit.EmergencyDestination.Y),CMBT_WIDTH-1,CMBT_HEIGHT-1,PALT_RED);
		}

	for(n=0; unit.PathX[n]!=MAP_RIGHT+1; n++)
		{
		cl=0xffff;
		//cl=(31<<7)|(0); // Ｇ 各値最大３１
		DrawLine4((int)(unit.PathX[n]-CameraPosition.X)-10,(int)(CameraPosition.Y-unit.PathY[n])-10,(int)(unit.PathX[n]-CameraPosition.X)+10,(int)(CameraPosition.Y-unit.PathY[n])-10,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
		DrawLine4((int)(unit.PathX[n]-CameraPosition.X)+10,(int)(CameraPosition.Y-unit.PathY[n])-10,(int)(unit.PathX[n]-CameraPosition.X)+10,(int)(CameraPosition.Y-unit.PathY[n])+10,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
		DrawLine4((int)(unit.PathX[n]-CameraPosition.X)+10,(int)(CameraPosition.Y-unit.PathY[n])+10,(int)(unit.PathX[n]-CameraPosition.X)-10,(int)(CameraPosition.Y-unit.PathY[n])+10,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
		DrawLine4((int)(unit.PathX[n]-CameraPosition.X)-10,(int)(CameraPosition.Y-unit.PathY[n])+10,(int)(unit.PathX[n]-CameraPosition.X)-10,(int)(CameraPosition.Y-unit.PathY[n])-10,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));

		if(n==0)
			{
			right=CMBT_WIDTH-1; bottom=CMBT_HEIGHT-1;
			if( unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked )
				{DrawLine4((int)(Units[unit.Carrier].Position.X-CameraPosition.X),(int)(CameraPosition.Y-Units[unit.Carrier].Position.Y),(int)(unit.PathX[n]-CameraPosition.X),(int)(CameraPosition.Y-unit.PathY[n]),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));}
			else
				{DrawLine4(Sprites[sprite].X,Sprites[sprite].Y,(int)(unit.PathX[n]-CameraPosition.X),(int)(CameraPosition.Y-unit.PathY[n]),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));}
			}
		else
			{
			DrawLine4((int)(unit.PathX[n-1]-CameraPosition.X),(int)(CameraPosition.Y-unit.PathY[n-1]),(int)(unit.PathX[n]-CameraPosition.X),(int)(CameraPosition.Y-unit.PathY[n]),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
			}
		}

	// 次の定点なるか までの線引き
	right=CMBT_WIDTH-1; bottom=CMBT_HEIGHT-1;
	if( CursorPosition.x<=CMBT_WIDTH )
		{
		if( MoveOrders[1].ClearsPath!=0 )
			{
			if( unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked )
				DrawLine4((int)(Units[unit.Carrier].Position.X-CameraPosition.X),(int)(CameraPosition.Y-Units[unit.Carrier].Position.Y),(int)(CursorPosition.x),(int)(CursorPosition.y),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
			else
				DrawLine4((int)(unit.Position.X-CameraPosition.X),(int)(CameraPosition.Y-unit.Position.Y),(int)(CursorPosition.x),(int)(CursorPosition.y),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
			}
		else
			DrawLine4((int)(unit.PathX[n-1]-CameraPosition.X),(int)(CameraPosition.Y-unit.PathY[n-1]),(int)(CursorPosition.x),(int)(CursorPosition.y),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
		}

	// 定点設定
	if( lc_lf_btn==1 && unit.Side==LocalSide && CursorPosition.x < CMBT_WIDTH &&
		// 発進チェック
		!(unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked
		&& ( Units[unit.Carrier].PlanesToLaunch!=0 || Units[unit.Carrier].LaunchLock!=0 || unit.ReloadTime>0 || Units[unit.Carrier].IsSupplying ))
		&& !IsEditingMap )
		{
		MoveOrders[1].Unit=SelectedUnit;
		MoveOrders[1].Destination = new WorldPosition(CursorPosition.x+CameraPosition.X, CameraPosition.Y-CursorPosition.y);
		}

	if( 1!=0 )
		{
		if( lc_ri_btn==1 )
			{
			lc_ri_btn=0;
			SelectedUnit=0; Selections[1][m]=0;	CombatMenuKind=0; CombatMenuSelection=CombatMenuItem.None;
			ClearSelection2(1);

			BufferedMoveOrders[1].ClearsPath=0;
			}
		}
	}

private bool DrawUnitMarks(bool goto_dca1, ref Unit unit, SpriteId sprite, ref int lc_lf_btn, int m, ref int lc_ri_btn)
	{
	RECT dstn_rect;
	RECT wrk_rect;
	int right;
	int bottom;
	int cl;
	int n;
	if( !goto_dca1 )
	{

//				sprite=UNIT_JPN;		//Off Screen Number		日本海軍の表示
//				sprite=UNIT_USA;		//Off Screen Number		合衆国海軍の表示

	//The Slct された機体への移動予定の線引き、
	if( unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked )
		{

		if( unit.Carrier==UnitInfoPanel[3] &&  ((UnitKind)UnitInfoPanel[0]==UnitKind.Carrier || (UnitKind)UnitInfoPanel[0]==UnitKind.LightCarrier || (UnitKind)UnitInfoPanel[0]==UnitKind.AirBase ) )
			{
			if( ( (unit.Mode<=UnitMode.Slow)  && (UnitInfoPanel[1]==1&&unit.DeckPhase<=2)||(UnitInfoPanel[1]==0&&unit.DeckPhase>=3))
			 || (unit.Mode==UnitMode.Return && (UnitInfoPanel[1]==0) ) )
				{
				// 空母で飛行甲板か格納庫かで航空機を表示するかしない。
				Sprites[sprite].X=(int)unit.Position.X;
				Sprites[sprite].Y=(int)unit.Position.Y;
				}
			else
				return false;
			}
		else
			return false;

		}
	else
		{
		if( unit.Side!=LocalSide && unit.Kind==UnitKind.Submarine && unit.IsSubmerged )
			{	// およその敵潜航潜水艦
			Sprites[sprite].X=(int)(unit.ContactX-CameraPosition.X);
			Sprites[sprite].Y=(int)(CameraPosition.Y-unit.ContactY);
			}
		else
			{	// マップ上のユニット

			Sprites[sprite].X=(int)(unit.Position.X-CameraPosition.X);
			Sprites[sprite].Y=(int)(CameraPosition.Y-unit.Position.Y);

			}
		}
	}

// dca1:
	dstn_rect.left=Sprites[sprite].X-Sprites[sprite].CenterX;
	dstn_rect.top=Sprites[sprite].Y-Sprites[sprite].CenterY;
	dstn_rect.right=dstn_rect.left+Sprites[sprite].Width-2;
	dstn_rect.bottom=dstn_rect.top+Sprites[sprite].Height-2;

	wrk_rect.left=dstn_rect.left+20;
	wrk_rect.top=dstn_rect.top+20;
	wrk_rect.right=dstn_rect.right-20;
	wrk_rect.bottom=dstn_rect.bottom-20;

	// クリック選択・非選択
	if( PointInRect(ref wrk_rect,CursorPosition.x,CursorPosition.y)!=0 && !( Units[SelectedUnit].Category==UnitCategory.Plane && unit.Category==UnitCategory.Ship && !(unit.Kind==UnitKind.Carrier || unit.Kind==UnitKind.LightCarrier || unit.Kind==UnitKind.AirBase || Units[SelectedUnit].Side!=unit.Side) ) && !(Units[SelectedUnit].Category==UnitCategory.Plane && Units[SelectedUnit].PlaneState==UnitState.Parked && unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Flying )  && !( unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked && !unit.IsStopping ) && !(Units[SelectedUnit].Category==UnitCategory.Plane && Units[SelectedUnit].PlaneState==UnitState.Parked && !Units[SelectedUnit].IsStopping ) && !( Units[SelectedUnit].Category==UnitCategory.Plane && Units[SelectedUnit].PlaneState==UnitState.Parked && unit.Category==UnitCategory.Ship )
		&& !(Units[SelectedUnit].Category==UnitCategory.Ship && unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked)  && !(SelectedUnit!=0 && Units[SelectedUnit].Side!=LocalSide) && !(SelectedUnit!=0 && Units[SelectedUnit].Category==UnitCategory.Plane&&unit.Weapon==FireKind.Maintenance)
		 && !(unit.PlaneState!=UnitState.Parked && CursorPosition.x>=CMBT_WIDTH-1)
		 && !(unit.Side!=LocalSide && unit.Kind==UnitKind.Submarine && unit.IsSubmerged)
		)
		{
		HandleUnitClick(ref unit, ref dstn_rect, ref lc_lf_btn, m, sprite);
		}

	if( m==0 )
		return false;

	// 選択されてればマークの絵というか枠
	if( unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked )
		{right=SCRN_WIDTH-1; bottom=SCRN_HEIGHT-1;}
	else
		{right=CMBT_WIDTH-1; bottom=CMBT_HEIGHT-1;}
	cl=0xffff;
	if( SelectedUnit==m && m!=0)
		{
		DrawSelectedUnit(ref dstn_rect, right, bottom);
		}
	else
		{
		if( Selections[1][m]!=0 )
			{
			n=20;
			DrawLine4(dstn_rect.left+n,dstn_rect.top+n,dstn_rect.right-n,dstn_rect.top+n,right,bottom,RGB(255,255,255));
			DrawLine4(dstn_rect.right-n,dstn_rect.top+n,dstn_rect.right-n,dstn_rect.bottom-n,right,bottom,RGB(255,255,255));
			DrawLine4(dstn_rect.right-n,dstn_rect.bottom-n,dstn_rect.left+n,dstn_rect.bottom-n,right,bottom,RGB(255,255,255));
			DrawLine4(dstn_rect.left+n,dstn_rect.bottom-n,dstn_rect.left+n,dstn_rect.top+n,right,bottom,RGB(255,255,255));
			}
		}

	if( SelectedUnit!=0 && Units[SelectedUnit].Kind==UnitKind.Transport && Units[SelectedUnit].Target==MaxUnitId+1 && m==SelectedUnit )
		{
		// 輸送船の揚陸先のマーク
		DrawLandingPoint(sprite);

		}
	else  if( SelectedUnit!=0 && Units[SelectedUnit].Target==m )
		{
		// 攻撃先 としてのマーク
		cl=0x1f;
		n=10+(FrameCount%8)*2;
		DrawLine4(dstn_rect.left+n,dstn_rect.top+n,dstn_rect.right-n,dstn_rect.top+n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
		DrawLine4(dstn_rect.right-n,dstn_rect.top+n,dstn_rect.right-n,dstn_rect.bottom-n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
		DrawLine4(dstn_rect.right-n,dstn_rect.bottom-n,dstn_rect.left+n,dstn_rect.bottom-n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
		DrawLine4(dstn_rect.left+n,dstn_rect.bottom-n,dstn_rect.left+n,dstn_rect.top+n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
		}
	else  if( SelectedUnit!=0 && Units[SelectedUnit].Carrier==m && Units[SelectedUnit].Category==UnitCategory.Plane && Units[SelectedUnit].PlaneState==UnitState.Flying)
		{
		// 攻撃先 としてのマーク
		cl=0x1f;
		n=15;
		DrawLine4(dstn_rect.right-n-4,dstn_rect.bottom-n,dstn_rect.left+n+4,dstn_rect.bottom-n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
		DrawLine4(dstn_rect.right-n-4,dstn_rect.bottom-n+2,dstn_rect.left+n+4,dstn_rect.bottom-n+2,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
		}

		// 攻撃先 着艦先 の方向
	if( unit.Target!=0 && unit.Side==LocalSide )
		{
		DrawTargetLine(ref unit);
		}

	// 決定された進路線ひき
	if( SelectedUnit==m && !IsEditingMap )
		{

		// 緊急移動先までの線
		DrawPathAndSetDestination(ref unit, sprite, lc_lf_btn, ref lc_ri_btn, m);
		}
	return true;
	}

private void DrawUnit(ref Unit unit, SpriteId sprite, ref RECT src_rect, ref RECT info_rect, ref RECT field_rect, ref int plane_fling_sound)
	{
	RECT dstn_rect;
	switch((int)(unit.Direction/22.5))
		{
		case 0: case 15:
			unit.SpriteColumn=2;
			break;
		case 1: case 2:
			unit.SpriteColumn=1;
			break;
		case 3: case 4:
			unit.SpriteColumn=0;
			break;
		case 5: case 6:
			unit.SpriteColumn=7;
			break;
		case 7: case 8:
			unit.SpriteColumn=6;
			break;
		case 9: case 10:
			unit.SpriteColumn=5;
			break;
		case 11: case 12:
			unit.SpriteColumn=4;
			break;
		case 13: case 14:
			unit.SpriteColumn=3;
			break;
		default:
			unit.SpriteColumn=2;
			break;
		}

	if( unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked)
		{
		if( unit.Carrier==UnitInfoPanel[3] && ((UnitKind)UnitInfoPanel[0]==UnitKind.Carrier || (UnitKind)UnitInfoPanel[0]==UnitKind.LightCarrier || (UnitKind)UnitInfoPanel[0]==UnitKind.AirBase ) )
			{
			if( ( (unit.Mode<=UnitMode.Slow) && (UnitInfoPanel[1]==1&&unit.DeckPhase<=2)||(UnitInfoPanel[1]==0&&unit.DeckPhase>=3))
			 || ( unit.Mode==UnitMode.Return && (UnitInfoPanel[1]==0) ) )
				{
				// 駐機中のの飛行機
				Sprites[sprite].Frame=(unit.SpriteRow*8)+unit.SpriteColumn;

				Sprites[sprite].X=(int)unit.Position.X;
				Sprites[sprite].Y=(int)unit.Position.Y;

				// src_rect は ソースサーフェスのレクタングルです。
				src_rect.left = Sprites[sprite].SheetX+(Sprites[sprite].Width * (Sprites[sprite].Frame % Sprites[sprite].FramesPerRow)) +1;
				src_rect.top = Sprites[sprite].SheetY+(Sprites[sprite].Height* (Sprites[sprite].Frame / Sprites[sprite].FramesPerRow)) +1;
				src_rect.right = (src_rect.left + Sprites[sprite].Width)-2;
				src_rect.bottom = (src_rect.top + Sprites[sprite].Height)-2;

				// dstn_rect は ディスティネーションレクタングルです。

				dstn_rect.left=Sprites[sprite].X-Sprites[sprite].CenterX;
				dstn_rect.top=Sprites[sprite].Y-Sprites[sprite].CenterY;
				dstn_rect.right=dstn_rect.left+Sprites[sprite].Width-2;
				dstn_rect.bottom=dstn_rect.top+Sprites[sprite].Height-2;

				if( ClipRects(ref dstn_rect,ref src_rect,ref info_rect)!=0 )
					{
					if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,(RECT*)Unsafe.AsPointer(ref src_rect),DDBLTFAST_SRCCOLORKEY) )
						{
						RestoreSurfaces();
						}
					}
				}
			}
		}
	else
		{
		Sprites[sprite].X=(int)(unit.Position.X-CameraPosition.X);
		Sprites[sprite].Y=(int)(CameraPosition.Y-unit.Position.Y);

		if( unit.Kind==UnitKind.Submarine && unit.IsSubmerged && Result==GameResult.None )
			{	// 潜航潜水艦
			Sprites[sprite].Frame=((unit.SpriteRow+1)*8)+unit.SpriteColumn;
			if( unit.Side!=LocalSide )
				Sprites[sprite].X=-999;				// それが敵潜水艦なら表示を外す
			}
		else
			{
			Sprites[sprite].Frame=(unit.SpriteRow*8)+unit.SpriteColumn;
			}

		// src_rect は ソースサーフェスのレクタングルです。
		src_rect.left = Sprites[sprite].SheetX+(Sprites[sprite].Width * (Sprites[sprite].Frame % Sprites[sprite].FramesPerRow)) +1;
		src_rect.top = Sprites[sprite].SheetY+(Sprites[sprite].Height* (Sprites[sprite].Frame / Sprites[sprite].FramesPerRow)) +1;
		src_rect.right = (src_rect.left + Sprites[sprite].Width)-2;
		src_rect.bottom = (src_rect.top + Sprites[sprite].Height)-2;

		// dstn_rect は ディスティネーションレクタングルです。
		dstn_rect.left=Sprites[sprite].X-Sprites[sprite].CenterX;
		dstn_rect.top=Sprites[sprite].Y-Sprites[sprite].CenterY;
		dstn_rect.right=dstn_rect.left+Sprites[sprite].Width-2;
		dstn_rect.bottom=dstn_rect.top+Sprites[sprite].Height-2;
		if( ClipRects(ref dstn_rect,ref src_rect,ref field_rect)!=0 )
			{
			if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,(RECT*)Unsafe.AsPointer(ref src_rect),DDBLTFAST_SRCCOLORKEY) )
				{
				RestoreSurfaces();
				}

			if( !IsEditingMap && plane_fling_sound==0 && Result==GameResult.None && unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Flying && (FrameCount%10)==0 )
				{
				PlaySoundEffect( 0, SoundId.PlaneFlying ,unit.Position.X, unit.Position.Y);
				plane_fling_sound=1;
				}

			}
		}
	}

//============================================================================
//コンバットエリア描画 同時にユーザー(通信対戦時はホスト)入力を受け付けます
//----------------------------------------------------------------------------
[Original("draw_cmbt_area")]
public void	DrawBattleArea()
	{
	RECT	src_rect = default,field_rect,info_rect;
	int	i,m,h,s,sign,lc_ri_btn,lc_lf_btn,j,j2; Array2<int> pp_on = default;
	SpriteId sprite;
	int	cm_scrn_x,cm_scrn_y; Array256<int> chk = default;

	int		map_bld_x,map_bld_y;
	Array256<byte> cBuf = default;
    Array5<Array128<byte>> ach = default;
    Array5<int> len = default;
	HDC					hdc;
	int	plane_fling_sound;

	plane_fling_sound=0;

	// field_rect は 戦域画面のレクタングルです。
	field_rect.left=0;
	field_rect.top=0;
	field_rect.right=CMBT_WIDTH;
	field_rect.bottom=CMBT_HEIGHT;

	// info_rect は インフォのレクタングルです

	info_rect.left=Sprites[SpriteId.JapanUnitInfo].X;
	info_rect.top=Sprites[SpriteId.JapanUnitInfo].Y;
	info_rect.right=info_rect.left+Sprites[SpriteId.JapanUnitInfo].Width;
	info_rect.bottom=info_rect.top+Sprites[SpriteId.JapanUnitInfo].Height;

	// カーソルの示す、マップチップの場所
	map_bld_x=(int)(((CursorPosition.x+40+(int)CameraPosition.X)-MAP_LEFT)/Sprites[SpriteId.MapTiles].Width);
	map_bld_y=(int)((MAP_TOP-((int)CameraPosition.Y-CursorPosition.y-40 ))/Sprites[SpriteId.MapTiles].Height);

	// 標準キャラよう背景の表示
	cm_scrn_x=(int)((CameraPosition.X-MAP_LEFT)/Sprites[SpriteId.MapTiles].Width);
	cm_scrn_y=(int)((MAP_TOP-CameraPosition.Y)/Sprites[SpriteId.MapTiles].Height);

	DrawMapTiles(cm_scrn_y, cm_scrn_x, map_bld_y, map_bld_x, ref cBuf, ref src_rect, ref field_rect);

	// ロウエフェクト
	UpdateLowerEffects( &field_rect,&info_rect );

	// 自サイドユニットからの距離により、可視不可視

	MoveOrders[0].Unit=0;
	SelectOrders[0].IsSet=false;

	MoveOrders[1].Unit=0;
	SelectOrders[1].IsSet=false;

	// 標準キャラの表示
	lc_ri_btn=RightButton;
	lc_lf_btn=LeftButton;

	if(  HasOrdered && Result==GameResult.None)
		{
		lc_ri_btn=0;
		lc_lf_btn=0;
		}

	if( lc_ri_btn==1 || lc_lf_btn==1 )
		PlaySoundEffect( 0, SoundId.Click1 ,(double)(MAP_RIGHT+1), 0);

	for( m=0; m<=MaxUnitId; m++)
		{
		ref var unit = ref Units[m];
		if( m==JPN_PLANE_START )
			{
			// 雲を描画します。
			DrawClouds( &field_rect );
			}

		if( unit.Side==Side.Japan)
			sprite=SpriteId.JapanUnits;		//Off Screen Number		日本海軍の表示
		else
			sprite=SpriteId.UnitedStatesUnits;		//Off Screen Number		合衆国海軍の表示

		// ユニットを描画します
		if( unit.IsUsed && (unit.Side==LocalSide || unit.IsFound) &&
	( ( ( CameraPosition.X-CMBT_REST<=unit.Position.X && CameraPosition.X+CMBT_WIDTH+CMBT_REST>=unit.Position.X) && (CameraPosition.Y+CMBT_REST>=unit.Position.Y && CameraPosition.Y-CMBT_HEIGHT-CMBT_REST<=unit.Position.Y) )
	|| (unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked )
	)
			)
			{
			DrawUnit(ref unit, sprite, ref src_rect, ref info_rect, ref field_rect, ref plane_fling_sound);
			}

		var goto_dca1=false;
		if (  m==0 && SelectedUnit!=0 && Units[SelectedUnit].Kind==UnitKind.Transport && m!=SelectedUnit)
			{
			// カーソルのある場所が
			// カーソルの示す、マップチップの場所
			map_bld_x=(int)(((CursorPosition.x+40+(int)CameraPosition.X)-MAP_LEFT)/Sprites[SpriteId.MapTiles].Width);
			map_bld_y=(int)((MAP_TOP-((int)CameraPosition.Y-CursorPosition.y-40 ))/Sprites[SpriteId.MapTiles].Height);
			if(
				MapTiles[map_bld_y][map_bld_x]==9 ||
				MapTiles[map_bld_y][map_bld_x]==8 ||
				MapTiles[map_bld_y][map_bld_x]==7 ||
				MapTiles[map_bld_y][map_bld_x]==5 ||
				MapTiles[map_bld_y][map_bld_x]==2
				)
				{
				Sprites[sprite].X=(((CursorPosition.x+40+(int)(CameraPosition.X-MAP_LEFT)%Sprites[SpriteId.MapTiles].Width)/80)*80)-(int)(CameraPosition.X-MAP_LEFT)%Sprites[SpriteId.MapTiles].Width;
				Sprites[sprite].Y=(((CursorPosition.y+40+(int)(MAP_TOP-CameraPosition.Y)%Sprites[SpriteId.MapTiles].Height)/80)*80)-(int)(MAP_TOP-CameraPosition.Y)%Sprites[SpriteId.MapTiles].Height;

				goto_dca1=true;
				}
			}

		if( goto_dca1 || unit.IsUsed && (unit.Side==LocalSide || unit.IsFound)  )
			{
			if( !DrawUnitMarks(goto_dca1, ref unit, sprite, ref lc_lf_btn, m, ref lc_ri_btn) )
				continue;
			}
		}

	if( lc_ri_btn==1 )
		{
		if( SelectedUnit==0 && PreviousSelectedUnit!=0 && Units[PreviousSelectedUnit].IsUsed)
			{
			set_the_slct_unit( PreviousSelectedUnit );
			}
		}

	// アッパーエフェクト
	UpdateUpperEffects(&field_rect,&info_rect);

	}
}
