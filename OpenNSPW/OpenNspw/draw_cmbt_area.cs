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

// Where a position in the world is on the screen, in whole pixels: its offset from CameraPosition, with Y growing
// downward, as `(int)(x-cmbt_x)` and `(int)(cmbt_y-y)` compute it.
public PixelPoint WorldToScreen(WorldPosition position)
	{
	return new PixelPoint((int)(position.X-CameraPosition.X), (int)(CameraPosition.Y-position.Y));
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cont_upper_effect")]
public void	UpdateUpperEffects(RECT* field,RECT* infoArea)
	{
	RECT source,destination;
	int n, size=default /* C4701 */ /* C4701 */;

	//=========		 Ｕｐｐｅｒのエフェクト描画		=========//
	for( var m=1; m<EFFECT_MAX; m++ )
		{
		ref var effect = ref Effects[m];
		if( effect.Layer==EffectLayer.Upper )
			{
			var isSeen=0;
			if(Result!=GameResult.None)
				isSeen=1;
			for(n=1;n<=MaxUnitId && isSeen==0 ;n++)
				{
				if( Units[n].IsUsed && Units[n].Side==LocalSide && Units[n].PlaneState!=UnitState.Parked )
					{
					// マイユニットからこのエフェクトが見えるか
					// 現地点からユニット地点への距離
					var eye=EyeOf(ref Units[n]);	// 航空機の場合はちょっと前へ

					var distance=(eye-effect.Position).Length;

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

					// 1.22's ranges, none limited while a base is built (0x40C919).
					if( Rules.HasSightRangesOf122 )
						size=GetSightRangeAs122(Units[n].Kind);

					if( distance<=size )
						{
						isSeen=1;
						}
					}
				}

			if( isSeen!=0 )
				{

				// マイユニットから見えるので表示
				switch( effect.Animation )
					{
					case 0:		//
						Sprites[SpriteId.SubUnits].Frame=effect.SpriteNumber;
						Sprites[SpriteId.SubUnits].Position = WorldToScreen(effect.Position);
						n=1;
						break;
					case 1:		// 対空機関砲弾がヒット
						Sprites[SpriteId.SubUnits].Frame=effect.SpriteNumber+(Sprites[SpriteId.SubUnits].FramesPerRow*(effect.TimeLeft%2));
						Sprites[SpriteId.SubUnits].Position = WorldToScreen(effect.Position);
						n=1;
						break;
					case 2:		//	飛行機からの煙
						Sprites[SpriteId.SubUnits].Frame=effect.SpriteNumber+(Sprites[SpriteId.SubUnits].FramesPerRow*(EffectRandom(2)));
						Sprites[SpriteId.SubUnits].Position = WorldToScreen(effect.Position)+new PixelPoint(EffectRandom(10)-5, EffectRandom(10)-5);
						n=1;
						break;
					case 3:		//	駐機場の飛行機用
						Sprites[SpriteId.SubUnits].Frame=effect.SpriteNumber;
						Sprites[SpriteId.SubUnits].Position = effect.Position.ToPixelPoint();
						n=0;
						break;
					case 4:		// 雷跡
						Sprites[SpriteId.SubUnits].Frame=effect.SpriteNumber+(Sprites[SpriteId.SubUnits].FramesPerRow*(EffectRandom(2)));
						Sprites[SpriteId.SubUnits].Position = WorldToScreen(effect.Position);
						n=1;
						break;
					case 10:	// 対空機関砲
						DrawLine5(WorldToScreen(effect.Position), WorldToScreen(effect.EndPosition), CMBT_WIDTH-1, CMBT_HEIGHT-1, 255);
						DrawLine5(WorldToScreen(effect.Position)+new PixelPoint(1, 0), WorldToScreen(effect.EndPosition)+new PixelPoint(1, 0), CMBT_WIDTH-1, CMBT_HEIGHT-1, 255);
						DrawLine5(WorldToScreen(effect.Position)+new PixelPoint(0, -1), WorldToScreen(effect.EndPosition)+new PixelPoint(0, -1), CMBT_WIDTH-1, CMBT_HEIGHT-1, 255);
						continue;
					case 11:	// 弾丸
						DrawLine5(WorldToScreen(effect.Position), WorldToScreen(effect.EndPosition), CMBT_WIDTH-1, CMBT_HEIGHT-1, 255);
						continue;
					}
				// src_rect は ソースサーフェスのレクタングルです。
				source.left = Sprites[SpriteId.SubUnits].SheetPosition.X+(Sprites[SpriteId.SubUnits].Size.Width * (Sprites[SpriteId.SubUnits].Frame % Sprites[SpriteId.SubUnits].FramesPerRow)) +1;
				source.top = Sprites[SpriteId.SubUnits].SheetPosition.Y+(Sprites[SpriteId.SubUnits].Size.Height* (Sprites[SpriteId.SubUnits].Frame / Sprites[SpriteId.SubUnits].FramesPerRow)) +1;
				source.right = (source.left + Sprites[SpriteId.SubUnits].Size.Width)-1;
				source.bottom = (source.top + Sprites[SpriteId.SubUnits].Size.Height)-1;
				// dstn_rect は ディスティネーションレクタングルです。
				destination.left=Sprites[SpriteId.SubUnits].Position.X-Sprites[SpriteId.SubUnits].Center.X;
				destination.top=Sprites[SpriteId.SubUnits].Position.Y-Sprites[SpriteId.SubUnits].Center.Y;
				destination.right=destination.left+Sprites[SpriteId.SubUnits].Size.Width-1;
				destination.bottom=destination.top+Sprites[SpriteId.SubUnits].Size.Height-1;

				if( n!=0 )
					{
					if( ClipRects(ref destination,ref source,ref *field)!=0 )
						{

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, destination.left, destination.top,lpDDS_OS,&source,DDBLTFAST_SRCCOLORKEY) )
		{
		RestoreSurfaces();
		}

						}
					}
				else
					{
					if( ClipRects(ref destination,ref source,ref *infoArea)!=0 )
						{

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, destination.left, destination.top,lpDDS_OS,&source,DDBLTFAST_SRCCOLORKEY) )
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
public void UpdateLowerEffects(RECT* field,RECT* infoArea)
	{
	RECT source,destination;
	int n=default /* C4701 */, size=default /* C4701 */, isSeen;

	//=========		 Ｌｏｗｅｒのエフェクト描画		=========//
	for(var m=1;m<EFFECT_MAX;m++)
		{
		ref var effect = ref Effects[m];
		if( effect.Layer==EffectLayer.Lower )
			{
			if( effect.Animation!=3 || Result!=GameResult.None )
				{
				// ユニットインフォ画面以外のエフェクト表示は見える見えないのテストをします。
				// 1.22 still tests them once the battle has a result (0x40CDEC).
				isSeen=0;
				if(Result!=GameResult.None && Rules.ResultShowsEveryLowerEffect)
					isSeen=1;
				for(n=1;n<=MaxUnitId;n++)
					{
					ref var unit = ref Units[n];
					if( unit.IsUsed && unit.Side==LocalSide && unit.PlaneState!=UnitState.Parked )
						{
						// マイユニットからこのエフェクトが見えるか
						// 現地点からユニット地点への距離
						var eye=EyeOf(ref unit);	// 航空機の場合はちょっと前へ

						var distance=(eye-effect.Position).Length;

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

						// 1.22's ranges, none limited while a base is built (0x40CF46).
						if( Rules.HasSightRangesOf122 )
							size=GetSightRangeAs122(unit.Kind);

						if( distance<=size)
							{
							isSeen=1;
							}
						}
					}
				}
			else
				{
				// ユニットインフォ内のエフェクトは必ず表示します。
				isSeen=1;
				}

			if( isSeen!=0 )
				{
				// マイユニットからの見える
				switch( effect.Animation )
					{
					case 0:		//
						Sprites[SpriteId.SubUnits].Frame=effect.SpriteNumber;
						Sprites[SpriteId.SubUnits].Position = WorldToScreen(effect.Position);
						n=1;
						break;
					case 1:		// 対空機関砲弾がヒット
						Sprites[SpriteId.SubUnits].Frame=effect.SpriteNumber+(Sprites[SpriteId.SubUnits].FramesPerRow*(effect.TimeLeft%2));
						Sprites[SpriteId.SubUnits].Position = WorldToScreen(effect.Position);
						n=1;
						break;
					case 2:		//	飛行機からの煙
						Sprites[SpriteId.SubUnits].Frame=effect.SpriteNumber+(Sprites[SpriteId.SubUnits].FramesPerRow*(EffectRandom(2)));
						Sprites[SpriteId.SubUnits].Position = WorldToScreen(effect.Position)+new PixelPoint(EffectRandom(10)-5, EffectRandom(10)-5);
						n=1;
						break;
					case 3:		//	駐機場の飛行機用
						Sprites[SpriteId.SubUnits].Frame=effect.SpriteNumber;
						Sprites[SpriteId.SubUnits].Position = effect.Position.ToPixelPoint();
						n=0;
						break;
					case 4:		// 雷跡、航跡
						Sprites[SpriteId.SubUnits].Frame=effect.SpriteNumber+(Sprites[SpriteId.SubUnits].FramesPerRow*(EffectRandom(2)));
						Sprites[SpriteId.SubUnits].Position = WorldToScreen(effect.Position);
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
				source.left = Sprites[SpriteId.SubUnits].SheetPosition.X+(Sprites[SpriteId.SubUnits].Size.Width * (Sprites[SpriteId.SubUnits].Frame % Sprites[SpriteId.SubUnits].FramesPerRow)) +1;
				source.top = Sprites[SpriteId.SubUnits].SheetPosition.Y+(Sprites[SpriteId.SubUnits].Size.Height* (Sprites[SpriteId.SubUnits].Frame / Sprites[SpriteId.SubUnits].FramesPerRow)) +1;
				source.right = (source.left + Sprites[SpriteId.SubUnits].Size.Width)-1;
				source.bottom = (source.top + Sprites[SpriteId.SubUnits].Size.Height)-1;
				// dstn_rect は ディスティネーションレクタングルです。
				destination.left=Sprites[SpriteId.SubUnits].Position.X-Sprites[SpriteId.SubUnits].Center.X;
				destination.top=Sprites[SpriteId.SubUnits].Position.Y-Sprites[SpriteId.SubUnits].Center.Y;
				destination.right=destination.left+Sprites[SpriteId.SubUnits].Size.Width-1;
				destination.bottom=destination.top+Sprites[SpriteId.SubUnits].Size.Height-1;

				if( n!=0 )
					{
					if( ClipRects(ref destination,ref source,ref *field)!=0 )
						{
						if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, destination.left, destination.top,lpDDS_OS,&source,DDBLTFAST_SRCCOLORKEY) )
							{
							RestoreSurfaces();
							}

						}
					}
				else
					{
					if( ClipRects(ref destination,ref source,ref *infoArea)!=0 )
						{
						if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, destination.left, destination.top,lpDDS_OS,&source,DDBLTFAST_SRCCOLORKEY) )
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
public void	DrawClouds(RECT* field)
	{
	RECT source,destination;

	for(var n=0;n<KUMO_MAX;n++)
		{
		ref var cloud = ref Clouds[n];
		if( cloud.Used!=0 )
			{
			Sprites[SpriteId.MapTiles].Position = WorldToScreen(cloud.Position);
			Sprites[SpriteId.MapTiles].Frame=2;
			// src_rect は ソースサーフェスのレクタングルです。
			source.left = Sprites[SpriteId.MapTiles].SheetPosition.X+(Sprites[SpriteId.MapTiles].Size.Width * (Sprites[SpriteId.MapTiles].Frame % Sprites[SpriteId.MapTiles].FramesPerRow));
			source.top = Sprites[SpriteId.MapTiles].SheetPosition.Y+(Sprites[SpriteId.MapTiles].Size.Height* (Sprites[SpriteId.MapTiles].Frame / Sprites[SpriteId.MapTiles].FramesPerRow)) ;
			source.right = (source.left + Sprites[SpriteId.MapTiles].Size.Width);
			source.bottom = (source.top + Sprites[SpriteId.MapTiles].Size.Height);

			// dstn_rect は ディスティネーションレクタングルです。
			destination.left=Sprites[SpriteId.MapTiles].Position.X-(Sprites[SpriteId.MapTiles].Size.Width/2);
			destination.top=Sprites[SpriteId.MapTiles].Position.Y-(Sprites[SpriteId.MapTiles].Size.Height/2);
			destination.right=destination.left+Sprites[SpriteId.MapTiles].Size.Width;
			destination.bottom=destination.top+Sprites[SpriteId.MapTiles].Size.Height;

			if( ClipRects(ref destination,ref source,ref *field)!=0 )
				{

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, destination.left, destination.top,lpDDS_OS,&source,DDBLTFAST_SRCCOLORKEY ) )
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
	int f;

	if( !(Units[m].Kind>=UnitKind.AirBase && Units[m].Kind<=UnitKind.Fortress ) )
		{
		// 沈没の水門
		f=FindFreeEffect();
		ref var effect = ref Effects[f];
		effect.Layer=EffectLayer.Lower;
		effect.TimeLeft=220;
		effect.Animation=0;
		effect.Position=Units[m].Position;
		effect.SpriteNumber=7;			// ソースファイル上の番号

		}

	if( Units[m].Kind==UnitKind.Battleship || Units[m].Kind==UnitKind.Carrier || Units[m].Kind==UnitKind.LightCarrier || Units[m].Kind==UnitKind.NavalBase || Units[m].Kind==UnitKind.AirBase || Units[m].Kind==UnitKind.Fortress )
		{
		// 大型艦船
		// 沈没の小水紋
		if( !(Units[m].Kind>=UnitKind.AirBase && Units[m].Kind<=UnitKind.Fortress ) )
			{
			for(var i=0;i<=7;i++)
				{
				f=FindFreeEffect();
				ref var effect = ref Effects[f];
				effect.Layer=EffectLayer.Lower;
				effect.TimeLeft=200+Random(20);
				effect.Animation=4;
				effect.Position = new WorldPosition(Units[m].Position.X+Random(100)-50, Units[m].Position.Y+Random(100)-50);
				effect.SpriteNumber=8;			// ソースファイル上の番号
				}
			}
		// 沈没の煙
		for(var i=0;i<=2;i++)
			{
			f=FindFreeEffect();
			ref var effect = ref Effects[f];
			effect.Layer=EffectLayer.Upper;
			effect.TimeLeft=150+Random(20);
			effect.Animation=4;
			effect.Position = new WorldPosition(Units[m].Position.X+Random(30)-15, Units[m].Position.Y+Random(30)-15);
			effect.SpriteNumber=5+Random(2);			// ソースファイル上の番号
			}
		// 沈没の爆炎
		for(var i=0;i<=4;i++)
			{
			f=FindFreeEffect();
			ref var effect = ref Effects[f];
			effect.Layer=EffectLayer.Upper;
			effect.TimeLeft=30+Random(20);
			effect.Animation=4;
			effect.Position = new WorldPosition(Units[m].Position.X+Random(40)-20, Units[m].Position.Y+Random(40)-20);
			effect.SpriteNumber=9;			// ソースファイル上の番号
			}
		// 沈没の小爆炎
		for(var i=0;i<=4;i++)
			{
			f=FindFreeEffect();
			ref var effect = ref Effects[f];
			effect.Layer=EffectLayer.Upper;
			effect.TimeLeft=40+Random(20);
			effect.Animation=4;
			effect.Position = new WorldPosition(Units[m].Position.X+Random(60)-30, Units[m].Position.Y+Random(60)-30);
			effect.SpriteNumber=10;			// ソースファイル上の番号
			}
		}
	else
		{
		// 中小型艦船
		// 沈没の小水紋
		if( !(Units[m].Kind>=UnitKind.AirBase && Units[m].Kind<=UnitKind.Fortress ) )
			{
			for(var i=0;i<=3;i++)
				{
				f=FindFreeEffect();
				ref var effect = ref Effects[f];
				effect.Layer=EffectLayer.Lower;
				effect.TimeLeft=200+Random(20);
				effect.Animation=4;
				effect.Position = new WorldPosition(Units[m].Position.X+Random(100)-50, Units[m].Position.Y+Random(100)-50);
				effect.SpriteNumber=8;			// ソースファイル上の番号
				}
			}
		// 沈没の煙
		for(var i=0;i<=1;i++)
			{
			f=FindFreeEffect();
			ref var effect = ref Effects[f];
			effect.Layer=EffectLayer.Upper;
			effect.TimeLeft=150+Random(20);
			effect.Animation=4;
			effect.Position = new WorldPosition(Units[m].Position.X+Random(30)-15, Units[m].Position.Y+Random(30)-15);
			effect.SpriteNumber=5+Random(2);			// ソースファイル上の番号
			}
		// 沈没の爆炎
		for(var i=0;i<=0;i++)
			{
			f=FindFreeEffect();
			ref var effect = ref Effects[f];
			effect.Layer=EffectLayer.Upper;
			effect.TimeLeft=30+Random(20);
			effect.Animation=4;
			effect.Position = new WorldPosition(Units[m].Position.X+Random(40)-20, Units[m].Position.Y+Random(40)-20);
			effect.SpriteNumber=9;			// ソースファイル上の番号
			}
		// 沈没の小爆炎
		for(var i=0;i<=1;i++)
			{
			f=FindFreeEffect();
			ref var effect = ref Effects[f];
			effect.Layer=EffectLayer.Upper;
			effect.TimeLeft=20+Random(20);
			effect.Animation=4;
			effect.Position = new WorldPosition(Units[m].Position.X+Random(20)-10, Units[m].Position.Y+Random(20)-10);
			effect.SpriteNumber=10;			// ソースファイル上の番号
			}

		}
	}

private void DrawMapTiles(int firstRow, int firstColumn, int editedRow, int editedColumn, ref Array256<byte> keys, ref RECT source, ref RECT field)
	{
	int isSeen;
	RECT destination;
	for(var m=0;m<=10;m++)
		{
		for(var n=0;n<=10;n++)
			{
			if(  !IsEditingMap &&  Result==GameResult.None  && MapTiles[firstRow+n][firstColumn+m]==0 )
				{
				isSeen=0;
				for( var f=1; f<=MaxUnitId; f++)
					{
					if( Units[f].IsUsed && Units[f].Side==LocalSide && Units[f].PlaneState!=UnitState.Parked  )
						{
						// 現地点からユニット地点への距離
						var tile=new WorldPosition(((firstColumn+m)*Sprites[SpriteId.MapTiles].Size.Width)-MAP_RIGHT, MAP_TOP-((firstRow+n)*Sprites[SpriteId.MapTiles].Size.Height));

						var eye=EyeOf(ref Units[f]);	// 戦闘機場合、視点を

						//((cm_scrn_x+m)*sprt[MAP_TIP_NRML].wd)-MAP_RIGHT

						var distance=(eye-tile).Length;
						if( distance<=(double)GetDetectionSize(f,0) )
							{
							isSeen=1;
							f=MaxUnitId;
							break;
							}
						}
					}
				}
			else
				{
				isSeen=1;
				}

			Sprites[SpriteId.MapTiles].Position = new PixelPoint(
				m*Sprites[SpriteId.MapTiles].Size.Width-(int)(CameraPosition.X-MAP_LEFT)%Sprites[SpriteId.MapTiles].Size.Width,
				n*Sprites[SpriteId.MapTiles].Size.Height-(int)(MAP_TOP-CameraPosition.Y)%Sprites[SpriteId.MapTiles].Size.Height);

			if( MapTiles[firstRow+n][firstColumn+m]==0)
				{
				if(isSeen!=0)
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

			if( MapTiles[firstRow+n][firstColumn+m]>=1)
				{
				Sprites[SpriteId.MapTiles].Frame=6+(MapTiles[firstRow+n][firstColumn+m]-1);	// 陸地
				}

			// マップエディット時のプログ
			if( IsEditingMap && (firstRow+n)==(editedRow) && (firstColumn+m)==(editedColumn) )
				{
				if ( (FrameCount%2)!=0 )
					Sprites[SpriteId.MapTiles].Frame=3;
				GetKeyboardState(keys);

				if( (keys[VK_NUMPAD1]&0x80)!=0 )
					{
					MapTiles[firstRow+n][firstColumn+m]=1;
					}
				if( (keys[VK_NUMPAD2]&0x80)!=0 )
					{
					MapTiles[firstRow+n][firstColumn+m]=2;
					}
				if( (keys[VK_NUMPAD3]&0x80)!=0 )
					{
					MapTiles[firstRow+n][firstColumn+m]=3;
					}
				if( (keys[VK_NUMPAD4]&0x80)!=0 )
					{
					MapTiles[firstRow+n][firstColumn+m]=4;
					}
				if( (keys[VK_NUMPAD5]&0x80)!=0 )
					{
					MapTiles[firstRow+n][firstColumn+m]=5;
					}
				if( (keys[VK_NUMPAD6]&0x80)!=0 )
					{
					MapTiles[firstRow+n][firstColumn+m]=6;
					}
				if( (keys[VK_NUMPAD7]&0x80)!=0 )
					{
					MapTiles[firstRow+n][firstColumn+m]=7;
					}
				if( (keys[VK_NUMPAD8]&0x80)!=0 )
					{
					MapTiles[firstRow+n][firstColumn+m]=8;
					}
				if( (keys[VK_NUMPAD9]&0x80)!=0 )
					{
					MapTiles[firstRow+n][firstColumn+m]=9;
					}
				if( (keys[VK_NUMPAD0]&0x80)!=0 )
					{
					MapTiles[firstRow+n][firstColumn+m]=0;
					}
				}

			// src_rect は ソースサーフェスのレクタングルです。
			source.left = Sprites[SpriteId.MapTiles].SheetPosition.X+(Sprites[SpriteId.MapTiles].Size.Width * (Sprites[SpriteId.MapTiles].Frame % Sprites[SpriteId.MapTiles].FramesPerRow)) ;
			source.top = Sprites[SpriteId.MapTiles].SheetPosition.Y+(Sprites[SpriteId.MapTiles].Size.Height* (Sprites[SpriteId.MapTiles].Frame / Sprites[SpriteId.MapTiles].FramesPerRow)) ;
			source.right = (source.left + Sprites[SpriteId.MapTiles].Size.Width);
			source.bottom = (source.top + Sprites[SpriteId.MapTiles].Size.Height);

			// dstn_rect は ディスティネーションレクタングルです。
			destination.left=Sprites[SpriteId.MapTiles].Position.X-Sprites[SpriteId.MapTiles].Center.X;
			destination.top=Sprites[SpriteId.MapTiles].Position.Y-Sprites[SpriteId.MapTiles].Center.Y;
			destination.right=destination.left+Sprites[SpriteId.MapTiles].Size.Width;
			destination.bottom=destination.top+Sprites[SpriteId.MapTiles].Size.Height;

			if( ClipRects(ref destination,ref source,ref field)!=0 )
				{
				if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, destination.left, destination.top,lpDDS_OS,(RECT*)Unsafe.AsPointer(ref source),0) )
					{
					RestoreSurfaces();
					}
				}
			}
		}
	}

// A left click on unit m, that counts (CanClickUnit): it selects the unit, or the selected unit's target, or, for m 0,
// the land at `ground` where the selected transport lands.
private void ClickUnit(int m, WorldPosition ground)
	{
	MoveOrders[1].Unit=0;

	if( SelectedUnit==m )
		{
		SelectedUnit=0; CombatMenuKind=0; CombatMenuSelection=CombatMenuItem.None; ClearSelection2(1);
		}
	else
		{
		if( SelectedUnit==0 )
			{
			if( !(Units[m].Category==UnitCategory.Plane && Units[m].PlaneState==UnitState.Parked) )
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
				SelectOrders[1].GroundPosition = ground;
				}
			}
		}
	}

private void HandleUnitClick(ref Unit unit, ref RECT destination, ref int leftButton, int m, SpriteId sprite)
	{
	int right;
	int bottom;
	if( (FrameCount%2)!=0 )
		{
		if( unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked )
			{
			right=SCRN_WIDTH-1; bottom=SCRN_HEIGHT-1;
			}
		else
			{right=CMBT_WIDTH-1; bottom=CMBT_HEIGHT-1;}
		var n=15;
		DrawLine4(destination.left+n,destination.top+n,destination.right-n,destination.top+n,right,bottom,RGB(255,255,255));
		DrawLine4(destination.right-n,destination.top+n,destination.right-n,destination.bottom-n,right,bottom,RGB(255,255,255));
		DrawLine4(destination.right-n,destination.bottom-n,destination.left+n,destination.bottom-n,right,bottom,RGB(255,255,255));
		DrawLine4(destination.left+n,destination.bottom-n,destination.left+n,destination.top+n,right,bottom,RGB(255,255,255));
		}

	if( leftButton==1 )
		{
		leftButton=0;
		ClickUnit(m, new WorldPosition(CameraPosition.X+Sprites[sprite].Position.X, CameraPosition.Y-Sprites[sprite].Position.Y));
		}
	}

private void DrawSelectedUnit(ref RECT destination, int right, int bottom)
	{
	var n=10;
	DrawLine4(destination.left+n,destination.top+n,destination.right-n,destination.top+n,right,bottom,RGB(255,255,255));
	DrawLine4(destination.right-n,destination.top+n,destination.right-n,destination.bottom-n,right,bottom,RGB(255,255,255));
	DrawLine4(destination.right-n,destination.bottom-n,destination.left+n,destination.bottom-n,right,bottom,RGB(255,255,255));
	DrawLine4(destination.left+n,destination.bottom-n,destination.left+n,destination.top+n,right,bottom,RGB(255,255,255));
	}

private void DrawLandingPoint(SpriteId sprite)
	{
	RECT mark;
	var n=10+(FrameCount%8)*2;

	mark.left=(int)(Units[SelectedUnit].LandingX-CameraPosition.X-40);
	mark.top=(int)(CameraPosition.Y-Units[SelectedUnit].LandingY-40);
	mark.right=mark.left+Sprites[sprite].Size.Width-2;
	mark.bottom=mark.top+Sprites[sprite].Size.Height-2;

	DrawLine4(mark.left+n,mark.top+n,mark.right-n,mark.top+n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
	DrawLine4(mark.right-n,mark.top+n,mark.right-n,mark.bottom-n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
	DrawLine4(mark.right-n,mark.bottom-n,mark.left+n,mark.bottom-n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
	DrawLine4(mark.left+n,mark.bottom-n,mark.left+n,mark.top+n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
	}

private void DrawTargetLine(ref Unit unit)
	{
	WorldPosition target;
	if(  0!=0 && Units[unit.Target].Kind==UnitKind.Submarine && Units[unit.Target].IsSubmerged)
		{	// 対潜水艦
		target=Units[unit.Target].ContactPosition;
		}
	else
		{	// 対潜航潜水艦以外
		if( unit.Kind!=UnitKind.Transport )
			{
			target=Units[unit.Target].Position;
			}
		else
			{
			// 揚陸方向
			target=unit.LandingPoint;
			}
		}

	if( (Units[unit.Target].IsFound || (unit.Target==MaxUnitId+1&&unit.Kind==UnitKind.Transport) )  && !(Units[unit.Target].Kind==UnitKind.Submarine && Units[unit.Target].IsSubmerged)   )
		{	// 視認
		var bearing=(target-unit.Position).NonZero().Direction;

		var tip=unit.Position+WorldVector.FromDirection(bearing) * 40;

		var barb=bearing;
		barb-=10;
		if(barb<0)
			barb=360+barb;
		DrawLine4(WorldToScreen(tip), WorldToScreen(unit.Position+WorldVector.FromDirection(barb) * 20), CMBT_WIDTH-1, CMBT_HEIGHT-1, RGB(255,255,255));

		barb=bearing;
		barb-=350;
		if(barb<0)
			barb=360+barb;
		DrawLine4(WorldToScreen(tip), WorldToScreen(unit.Position+WorldVector.FromDirection(barb) * 20), CMBT_WIDTH-1, CMBT_HEIGHT-1, RGB(255,255,255));
		}
	else
		{	// 視認不可
		var from=new WorldPosition(unit.Position.X+10+20, unit.Position.Y+40);
		var to=new WorldPosition(unit.Position.X-10+20, unit.Position.Y+20);
		DrawLine4(WorldToScreen(from), WorldToScreen(to), CMBT_WIDTH-1, CMBT_HEIGHT-1, RGB(255,255,255));
		from=new WorldPosition(unit.Position.X-10+20, unit.Position.Y+40);
		to=new WorldPosition(unit.Position.X+10+20, unit.Position.Y+20);
		DrawLine4(WorldToScreen(from), WorldToScreen(to), CMBT_WIDTH-1, CMBT_HEIGHT-1, RGB(255,255,255));
		}
	}

// Whether the selected unit can be given a destination: it is the player's, it is not a parked plane that cannot take
// off yet, and the map is not being edited.
private bool CanSetDestination(ref Unit unit)
	{
	return unit.Side==LocalSide &&
		// 発進チェック
		!(unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked
		&& ( Units[unit.Carrier].PlanesToLaunch!=0 || Units[unit.Carrier].LaunchLock!=0 || unit.ReloadTime>0 || Units[unit.Carrier].IsSupplying ))
		&& !IsEditingMap;
	}

// A left click on the battlefield, that gives the selected unit (CanSetDestination) a destination.
private void SetDestination(WorldPosition destination)
	{
	MoveOrders[1].Unit=SelectedUnit;
	MoveOrders[1].Destination = destination;
	}

// A right click while unit m is selected: nothing is selected any more.
private void CancelSelection(int m)
	{
	SelectedUnit=0; Selections[1][m]=0;	CombatMenuKind=0; CombatMenuSelection=CombatMenuItem.None;
	ClearSelection2(1);

	BufferedMoveOrders[1].ClearsPath=0;
	}

private void DrawPathAndSetDestination(ref Unit unit, SpriteId sprite, int leftButton, ref int rightButton, int m)
	{
	int n;
	int right;
	int bottom;
	if( unit.EmergencyFlags[0]!=0 )
		{
		DrawLine5(WorldToScreen(unit.Position), WorldToScreen(unit.EmergencyDestination), CMBT_WIDTH-1, CMBT_HEIGHT-1, PALT_RED);
		}

	for(n=0; unit.PathX[n]!=MAP_RIGHT+1; n++)
		{
		//cl=(31<<7)|(0); // Ｇ 各値最大３１
		DrawLine4(WorldToScreen(unit.GetPathPoint(n))+new PixelPoint(-10, -10), WorldToScreen(unit.GetPathPoint(n))+new PixelPoint(10, -10), CMBT_WIDTH-1, CMBT_HEIGHT-1, RGB(255,255,255));
		DrawLine4(WorldToScreen(unit.GetPathPoint(n))+new PixelPoint(10, -10), WorldToScreen(unit.GetPathPoint(n))+new PixelPoint(10, 10), CMBT_WIDTH-1, CMBT_HEIGHT-1, RGB(255,255,255));
		DrawLine4(WorldToScreen(unit.GetPathPoint(n))+new PixelPoint(10, 10), WorldToScreen(unit.GetPathPoint(n))+new PixelPoint(-10, 10), CMBT_WIDTH-1, CMBT_HEIGHT-1, RGB(255,255,255));
		DrawLine4(WorldToScreen(unit.GetPathPoint(n))+new PixelPoint(-10, 10), WorldToScreen(unit.GetPathPoint(n))+new PixelPoint(-10, -10), CMBT_WIDTH-1, CMBT_HEIGHT-1, RGB(255,255,255));

		if(n==0)
			{
			right=CMBT_WIDTH-1; bottom=CMBT_HEIGHT-1;
			if( unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked )
				{DrawLine4(WorldToScreen(Units[unit.Carrier].Position), WorldToScreen(unit.GetPathPoint(n)), CMBT_WIDTH-1, CMBT_HEIGHT-1, RGB(255,255,255));}
			else
				{DrawLine4(Sprites[sprite].Position, WorldToScreen(unit.GetPathPoint(n)), CMBT_WIDTH-1, CMBT_HEIGHT-1, RGB(255,255,255));}
			}
		else
			{
			DrawLine4(WorldToScreen(unit.GetPathPoint(n-1)), WorldToScreen(unit.GetPathPoint(n)), CMBT_WIDTH-1, CMBT_HEIGHT-1, RGB(255,255,255));
			}
		}

	// 次の定点なるか までの線引き
	right=CMBT_WIDTH-1; bottom=CMBT_HEIGHT-1;
	if( CursorPosition.x<=CMBT_WIDTH )
		{
		if( MoveOrders[1].ClearsPath!=0 )
			{
			if( unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked )
				DrawLine4(WorldToScreen(Units[unit.Carrier].Position), new PixelPoint(CursorPosition.x, CursorPosition.y), CMBT_WIDTH-1, CMBT_HEIGHT-1, RGB(255,255,255));
			else
				DrawLine4(WorldToScreen(unit.Position), new PixelPoint(CursorPosition.x, CursorPosition.y), CMBT_WIDTH-1, CMBT_HEIGHT-1, RGB(255,255,255));
			}
		else
			DrawLine4(WorldToScreen(unit.GetPathPoint(n-1)), new PixelPoint(CursorPosition.x, CursorPosition.y), CMBT_WIDTH-1, CMBT_HEIGHT-1, RGB(255,255,255));
		}

	// 定点設定
	if( leftButton==1 && CursorPosition.x < CMBT_WIDTH && CanSetDestination(ref unit) )
		{
		SetDestination(new WorldPosition(CursorPosition.x+CameraPosition.X, CameraPosition.Y-CursorPosition.y));
		}

	if( 1!=0 )
		{
		if( rightButton==1 )
			{
			rightButton=0;
			CancelSelection(m);
			}
		}
	}

// Whether a parked plane is shown, on the flight deck or in the hangar of the carrier or the air base whose information
// the sidebar shows, as the view is switched (UnitInfoPanel[1]).
private bool IsOnShownDeck(ref Unit unit)
	{
	return unit.Carrier==UnitInfoPanel[3] &&  ((UnitKind)UnitInfoPanel[0]==UnitKind.Carrier || (UnitKind)UnitInfoPanel[0]==UnitKind.LightCarrier || (UnitKind)UnitInfoPanel[0]==UnitKind.AirBase )
		&& ( ( (unit.Mode<=UnitMode.Slow)  && (UnitInfoPanel[1]==1&&unit.DeckPhase<=2)||(UnitInfoPanel[1]==0&&unit.DeckPhase>=3))
		 || (unit.Mode==UnitMode.Return && (UnitInfoPanel[1]==0) ) );
	}

// Whether a left click on the unit counts, as it and the selected unit are: the conditions of DrawUnitMarks's click
// that are not about where the cursor is. The unit is Units[0] for the land, where the selected transport lands.
private bool CanClickUnit(ref Unit unit)
	{
	return !( Units[SelectedUnit].Category==UnitCategory.Plane && unit.Category==UnitCategory.Ship && !(unit.Kind==UnitKind.Carrier || unit.Kind==UnitKind.LightCarrier || unit.Kind==UnitKind.AirBase || Units[SelectedUnit].Side!=unit.Side) ) && !(Units[SelectedUnit].Category==UnitCategory.Plane && Units[SelectedUnit].PlaneState==UnitState.Parked && unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Flying )  && !( unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked && !unit.IsStopping ) && !(Units[SelectedUnit].Category==UnitCategory.Plane && Units[SelectedUnit].PlaneState==UnitState.Parked && !Units[SelectedUnit].IsStopping ) && !( Units[SelectedUnit].Category==UnitCategory.Plane && Units[SelectedUnit].PlaneState==UnitState.Parked && unit.Category==UnitCategory.Ship )
		&& !(Units[SelectedUnit].Category==UnitCategory.Ship && unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked)  && !(SelectedUnit!=0 && Units[SelectedUnit].Side!=LocalSide) && !(SelectedUnit!=0 && Units[SelectedUnit].Category==UnitCategory.Plane&&unit.Weapon==FireKind.Maintenance)
		 && !(unit.Side!=LocalSide && unit.Kind==UnitKind.Submarine && unit.IsSubmerged);
	}

private bool DrawUnitMarks(bool isPositioned, ref Unit unit, SpriteId sprite, ref int leftButton, int m, ref int rightButton)
	{
	RECT destination;
	RECT clickArea;
	int right;
	int bottom;
	if( !isPositioned )
	{

//				sprite=UNIT_JPN;		//Off Screen Number		日本海軍の表示
//				sprite=UNIT_USA;		//Off Screen Number		合衆国海軍の表示

	//The Slct された機体への移動予定の線引き、
	if( unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked )
		{

		if( IsOnShownDeck(ref unit) )
			{
			// 空母で飛行甲板か格納庫かで航空機を表示するかしない。
			Sprites[sprite].Position = unit.Position.ToPixelPoint();
			}
		else
			return false;

		}
	else
		{
		if( unit.Side!=LocalSide && unit.Kind==UnitKind.Submarine && unit.IsSubmerged )
			{	// およその敵潜航潜水艦
			Sprites[sprite].Position = WorldToScreen(unit.ContactPosition);
			}
		else
			{	// マップ上のユニット

			Sprites[sprite].Position = WorldToScreen(unit.Position);

			}
		}
	}

// dca1:
	destination.left=Sprites[sprite].Position.X-Sprites[sprite].Center.X;
	destination.top=Sprites[sprite].Position.Y-Sprites[sprite].Center.Y;
	destination.right=destination.left+Sprites[sprite].Size.Width-2;
	destination.bottom=destination.top+Sprites[sprite].Size.Height-2;

	clickArea.left=destination.left+20;
	clickArea.top=destination.top+20;
	clickArea.right=destination.right-20;
	clickArea.bottom=destination.bottom-20;

	// クリック選択・非選択
	if( PointInRect(ref clickArea,CursorPosition.x,CursorPosition.y)!=0 && CanClickUnit(ref unit)
		 && !(unit.PlaneState!=UnitState.Parked && CursorPosition.x>=CMBT_WIDTH-1)
		)
		{
		HandleUnitClick(ref unit, ref destination, ref leftButton, m, sprite);
		}

	if( m==0 )
		return false;

	// 選択されてればマークの絵というか枠
	if( unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked )
		{right=SCRN_WIDTH-1; bottom=SCRN_HEIGHT-1;}
	else
		{right=CMBT_WIDTH-1; bottom=CMBT_HEIGHT-1;}
	if( SelectedUnit==m && m!=0)
		{
		DrawSelectedUnit(ref destination, right, bottom);
		}
	else
		{
		if( Selections[1][m]!=0 )
			{
			var n=20;
			DrawLine4(destination.left+n,destination.top+n,destination.right-n,destination.top+n,right,bottom,RGB(255,255,255));
			DrawLine4(destination.right-n,destination.top+n,destination.right-n,destination.bottom-n,right,bottom,RGB(255,255,255));
			DrawLine4(destination.right-n,destination.bottom-n,destination.left+n,destination.bottom-n,right,bottom,RGB(255,255,255));
			DrawLine4(destination.left+n,destination.bottom-n,destination.left+n,destination.top+n,right,bottom,RGB(255,255,255));
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
		var n=10+(FrameCount%8)*2;
		DrawLine4(destination.left+n,destination.top+n,destination.right-n,destination.top+n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
		DrawLine4(destination.right-n,destination.top+n,destination.right-n,destination.bottom-n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
		DrawLine4(destination.right-n,destination.bottom-n,destination.left+n,destination.bottom-n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
		DrawLine4(destination.left+n,destination.bottom-n,destination.left+n,destination.top+n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
		}
	else  if( SelectedUnit!=0 && Units[SelectedUnit].Carrier==m && Units[SelectedUnit].Category==UnitCategory.Plane && Units[SelectedUnit].PlaneState==UnitState.Flying)
		{
		// 攻撃先 としてのマーク
		var n=15;
		DrawLine4(destination.right-n-4,destination.bottom-n,destination.left+n+4,destination.bottom-n,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
		DrawLine4(destination.right-n-4,destination.bottom-n+2,destination.left+n+4,destination.bottom-n+2,CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
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
		DrawPathAndSetDestination(ref unit, sprite, leftButton, ref rightButton, m);
		}
	return true;
	}

private void DrawUnit(ref Unit unit, SpriteId sprite, ref RECT source, ref RECT infoArea, ref RECT field, ref int playsPlaneSound)
	{
	RECT destination;
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

				Sprites[sprite].Position = unit.Position.ToPixelPoint();

				// src_rect は ソースサーフェスのレクタングルです。
				source.left = Sprites[sprite].SheetPosition.X+(Sprites[sprite].Size.Width * (Sprites[sprite].Frame % Sprites[sprite].FramesPerRow)) +1;
				source.top = Sprites[sprite].SheetPosition.Y+(Sprites[sprite].Size.Height* (Sprites[sprite].Frame / Sprites[sprite].FramesPerRow)) +1;
				source.right = (source.left + Sprites[sprite].Size.Width)-2;
				source.bottom = (source.top + Sprites[sprite].Size.Height)-2;

				// dstn_rect は ディスティネーションレクタングルです。

				destination.left=Sprites[sprite].Position.X-Sprites[sprite].Center.X;
				destination.top=Sprites[sprite].Position.Y-Sprites[sprite].Center.Y;
				destination.right=destination.left+Sprites[sprite].Size.Width-2;
				destination.bottom=destination.top+Sprites[sprite].Size.Height-2;

				if( ClipRects(ref destination,ref source,ref infoArea)!=0 )
					{
					if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, destination.left, destination.top,lpDDS_OS,(RECT*)Unsafe.AsPointer(ref source),DDBLTFAST_SRCCOLORKEY) )
						{
						RestoreSurfaces();
						}
					}
				}
			}
		}
	else
		{
		Sprites[sprite].Position = WorldToScreen(unit.Position);

		if( unit.Kind==UnitKind.Submarine && unit.IsSubmerged && Result==GameResult.None )
			{	// 潜航潜水艦
			Sprites[sprite].Frame=((unit.SpriteRow+1)*8)+unit.SpriteColumn;
			if( unit.Side!=LocalSide )
				Sprites[sprite].Position = new PixelPoint(-999, Sprites[sprite].Position.Y);				// それが敵潜水艦なら表示を外す
			}
		else
			{
			Sprites[sprite].Frame=(unit.SpriteRow*8)+unit.SpriteColumn;
			}

		// src_rect は ソースサーフェスのレクタングルです。
		source.left = Sprites[sprite].SheetPosition.X+(Sprites[sprite].Size.Width * (Sprites[sprite].Frame % Sprites[sprite].FramesPerRow)) +1;
		source.top = Sprites[sprite].SheetPosition.Y+(Sprites[sprite].Size.Height* (Sprites[sprite].Frame / Sprites[sprite].FramesPerRow)) +1;
		source.right = (source.left + Sprites[sprite].Size.Width)-2;
		source.bottom = (source.top + Sprites[sprite].Size.Height)-2;

		// dstn_rect は ディスティネーションレクタングルです。
		destination.left=Sprites[sprite].Position.X-Sprites[sprite].Center.X;
		destination.top=Sprites[sprite].Position.Y-Sprites[sprite].Center.Y;
		destination.right=destination.left+Sprites[sprite].Size.Width-2;
		destination.bottom=destination.top+Sprites[sprite].Size.Height-2;
		if( ClipRects(ref destination,ref source,ref field)!=0 )
			{
			if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, destination.left, destination.top,lpDDS_OS,(RECT*)Unsafe.AsPointer(ref source),DDBLTFAST_SRCCOLORKEY) )
				{
				RestoreSurfaces();
				}

			if( !IsEditingMap && playsPlaneSound==0 && Result==GameResult.None && unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Flying && (FrameCount%10)==0 )
				{
				PlaySoundEffect( 0, SoundId.PlaneFlying ,unit.Position.X, unit.Position.Y);
				playsPlaneSound=1;
				}

			}
		}
	}

// Whether the selected transport can land on the land at point (x, y) of the battlefield; if so, the landing mark (the
// sprite's position) is put on the land's tile.
private bool AimLanding(SpriteId sprite, int x, int y)
	{
	// カーソルの示す、マップチップの場所
	// BUG(NSPW NET): a cursor off the map gives a tile outside it, which NSPW NET reads out of bounds; the port throws
	// instead (docs/KnownIssues.md).
	var column=(int)(((x+40+(int)CameraPosition.X)-MAP_LEFT)/Sprites[SpriteId.MapTiles].Size.Width);
	var row=(int)((MAP_TOP-((int)CameraPosition.Y-y-40 ))/Sprites[SpriteId.MapTiles].Size.Height);
	if(
		MapTiles[row][column]==9 ||
		MapTiles[row][column]==8 ||
		MapTiles[row][column]==7 ||
		MapTiles[row][column]==5 ||
		MapTiles[row][column]==2
		)
		{
		Sprites[sprite].Position = new PixelPoint((((x+40+(int)(CameraPosition.X-MAP_LEFT)%Sprites[SpriteId.MapTiles].Size.Width)/80)*80)-(int)(CameraPosition.X-MAP_LEFT)%Sprites[SpriteId.MapTiles].Size.Width, (((y+40+(int)(MAP_TOP-CameraPosition.Y)%Sprites[SpriteId.MapTiles].Size.Height)/80)*80)-(int)(MAP_TOP-CameraPosition.Y)%Sprites[SpriteId.MapTiles].Size.Height);
		return true;
		}

	return false;
	}

// A right click that no selected unit took: the unit selected before is selected again, if nothing is selected and it
// is still there.
private void RestoreSelection()
	{
	if( SelectedUnit==0 && PreviousSelectedUnit!=0 && Units[PreviousSelectedUnit].IsUsed)
		{
		set_the_slct_unit( PreviousSelectedUnit );
		}
	}

// The sheet a unit is drawn from: its side's. 1.22 also draws the Americans' fighters of type 1 in scenario 303, the
// ones taken from Japan's air bases, as Japan's (0x40E070).
public SpriteId GetUnitSprites(ref Unit unit)
	{
	if( unit.Side==Side.Japan || (Rules.DrawsCapturedFightersAsJapans && ScenarioNumber==303 && unit.Side==Side.UnitedStates && unit.Kind==UnitKind.Fighter && unit.Variant==1) )
		return SpriteId.JapanUnits;		//Off Screen Number		日本海軍の表示
	return SpriteId.UnitedStatesUnits;		//Off Screen Number		合衆国海軍の表示
	}

//============================================================================
//コンバットエリア描画 同時にユーザー(通信対戦時はホスト)入力を受け付けます
//----------------------------------------------------------------------------
[Original("draw_cmbt_area")]
public void	DrawBattleArea()
	{
	RECT	source = default,field,infoArea;
	 
	 

	Array256<byte> keys = default;

	var playsPlaneSound=0;

	// field_rect は 戦域画面のレクタングルです。
	field.left=0;
	field.top=0;
	field.right=CMBT_WIDTH;
	field.bottom=CMBT_HEIGHT;

	// info_rect は インフォのレクタングルです

	infoArea.left=Sprites[SpriteId.JapanUnitInfo].Position.X;
	infoArea.top=Sprites[SpriteId.JapanUnitInfo].Position.Y;
	infoArea.right=infoArea.left+Sprites[SpriteId.JapanUnitInfo].Size.Width;
	infoArea.bottom=infoArea.top+Sprites[SpriteId.JapanUnitInfo].Size.Height;

	// カーソルの示す、マップチップの場所
	var editedColumn=(int)(((CursorPosition.x+40+(int)CameraPosition.X)-MAP_LEFT)/Sprites[SpriteId.MapTiles].Size.Width);
	var editedRow=(int)((MAP_TOP-((int)CameraPosition.Y-CursorPosition.y-40 ))/Sprites[SpriteId.MapTiles].Size.Height);

	// 標準キャラよう背景の表示
	var firstColumn=(int)((CameraPosition.X-MAP_LEFT)/Sprites[SpriteId.MapTiles].Size.Width);
	var firstRow=(int)((MAP_TOP-CameraPosition.Y)/Sprites[SpriteId.MapTiles].Size.Height);

	DrawMapTiles(firstRow, firstColumn, editedRow, editedColumn, ref keys, ref source, ref field);

	// ロウエフェクト
	UpdateLowerEffects( &field,&infoArea );

	// 自サイドユニットからの距離により、可視不可視

	MoveOrders[0].Unit=0;
	SelectOrders[0].IsSet=false;

	MoveOrders[1].Unit=0;
	SelectOrders[1].IsSet=false;

	// 標準キャラの表示
	int rightButton=RightButton;
	int leftButton=LeftButton;

	if(  HasOrdered && Result==GameResult.None)
		{
		rightButton=0;
		leftButton=0;
		}

	if( rightButton==1 || leftButton==1 )
		PlaySoundEffect( 0, SoundId.Click1 ,(double)(MAP_RIGHT+1), 0);

	// The host's UI's commands for clicks on the battlefield (UICommands.cs): taken here, as the clicks are, and dropped
	// as they are while an order waits for its turn; the units brushed are clicked, one a frame.
	RunUICommands(UICommandSite.Battlefield, !(HasOrdered && Result==GameResult.None));
	RunBrushedUnits(!(HasOrdered && Result==GameResult.None));

	for( var m=0; m<=MaxUnitId; m++)
		{
		ref var unit = ref Units[m];
		if( m==JPN_PLANE_START )
			{
			// 雲を描画します。
			DrawClouds( &field );
			}

		var sprite=GetUnitSprites(ref unit);

		// ユニットを描画します
		if( unit.IsUsed && (unit.Side==LocalSide || unit.IsFound) &&
	( ( ( CameraPosition.X-CMBT_REST<=unit.Position.X && CameraPosition.X+CMBT_WIDTH+CMBT_REST>=unit.Position.X) && (CameraPosition.Y+CMBT_REST>=unit.Position.Y && CameraPosition.Y-CMBT_HEIGHT-CMBT_REST<=unit.Position.Y) )
	|| (unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked )
	)
			)
			{
			DrawUnit(ref unit, sprite, ref source, ref infoArea, ref field, ref playsPlaneSound);
			}

		var isPositioned=false;
		if (  m==0 && SelectedUnit!=0 && Units[SelectedUnit].Kind==UnitKind.Transport && m!=SelectedUnit)
			{
			// カーソルのある場所が
			if( AimLanding(sprite, CursorPosition.x, CursorPosition.y) )
				{
				isPositioned=true;
				}
			}

		if( isPositioned || unit.IsUsed && (unit.Side==LocalSide || unit.IsFound)  )
			{
			if( !DrawUnitMarks(isPositioned, ref unit, sprite, ref leftButton, m, ref rightButton) )
				continue;
			}
		}

	if( rightButton==1 )
		{
		RestoreSelection();
		}

	// アッパーエフェクト
	UpdateUpperEffects(&field,&infoArea);

	}
}
