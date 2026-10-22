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

// Port of etc3.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{

//============================================================================
//	マイ乱数を作ります
//----------------------------------------------------------------------------
[Original("make_my_rnd")]
public void	MakeSharedRandomTable()
	{

	for(short i=0;i<4096;i++)
		{
		SharedRandomTable[i]=Random(65536);
		}

	SharedRandomIndex=0;
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("my_rnd")]
public int		SharedRandom(int range)
	{
	SharedRandomIndex++;
	SharedRandomIndex%=4096;

	return(SharedRandomTable[SharedRandomIndex]%range);
	}

//============================================================================
//
//----------------------------------------------------------------------------

[Original("rnd")]
public int		Random(int range)
	{

	RandomCount++;

	return(rand()%(range));
	}

// A random number for an effect. NSPW NET draws them from SharedRandom, so that network games stay in step whatever
// their effects; 1.22 draws them from rand, as all the others.
public int EffectRandom(int range) => Rules.EffectsDrawFromRand ? Random(range) : SharedRandom(range);

// Whether this tick makes the effects that 1.22 makes on the tick that is drawn only.
public bool IsEffectTick => IsDrawnTick || !Rules.EffectsOnDrawnTickOnly;

//============================================================================
// スプライト基礎データ
//----------------------------------------------------------------------------
[Original("set_sprt_data")]
public void		InitializeSprites()
	{

	// タイトル

	// 1.22's title in its sheet is 599 by 387 (set_sprt_data, 0x432E43); NSPW NET's, 699 by 384.
	Sprites[SpriteId.TitleBackground].Size = new PixelSize(IsSingleplayer ? 599 : 699, IsSingleplayer ? 387 : 384);
	Sprites[SpriteId.TitleBackground].SheetPosition = new PixelPoint(0, 3940);
	// ユニット
	Sprites[SpriteId.JapanUnits].Size = new PixelSize(80, 80);
	Sprites[SpriteId.JapanUnits].SheetPosition = new PixelPoint(0, 1759);
	Sprites[SpriteId.JapanUnits].FramesPerRow=8;
	Sprites[SpriteId.JapanUnits].Center = new PixelPoint(40, 40);

	Sprites[SpriteId.UnitedStatesUnits].Size = new PixelSize(80, 80);
	Sprites[SpriteId.UnitedStatesUnits].SheetPosition = new PixelPoint(0, 4550);
	Sprites[SpriteId.UnitedStatesUnits].FramesPerRow=8;
	Sprites[SpriteId.UnitedStatesUnits].Center = new PixelPoint(40, 40);

	// サブユニット
	Sprites[SpriteId.SubUnits].Size = new PixelSize(40, 40);
	Sprites[SpriteId.SubUnits].SheetPosition = new PixelPoint(0, 3520);
	Sprites[SpriteId.SubUnits].FramesPerRow=12;
	Sprites[SpriteId.SubUnits].Center = new PixelPoint(20, 20);

	// マップチップ 雲
	Sprites[SpriteId.MapTiles].Size = new PixelSize(80, 80);
	Sprites[SpriteId.MapTiles].SheetPosition = new PixelPoint(0, 3000);
	Sprites[SpriteId.MapTiles].FramesPerRow=6;
	Sprites[SpriteId.MapTiles].Center = new PixelPoint(40, 40);

	// ユニットインフォ
	Sprites[SpriteId.JapanUnitInfo].Position = new PixelPoint(CMBT_WIDTH, 0);
	Sprites[SpriteId.JapanUnitInfo].Size = new PixelSize(120, 438);
	Sprites[SpriteId.JapanUnitInfo].SheetPosition = new PixelPoint(0, 0);
	Sprites[SpriteId.JapanUnitInfo].FramesPerRow=6;
	Sprites[SpriteId.JapanUnitInfo].Center = new PixelPoint(0, 0);

	Sprites[SpriteId.UnitedStatesUnitInfo].Size = new PixelSize(120, 438);
	Sprites[SpriteId.UnitedStatesUnitInfo].SheetPosition = new PixelPoint(0, 878);
	Sprites[SpriteId.UnitedStatesUnitInfo].FramesPerRow=6;
	Sprites[SpriteId.UnitedStatesUnitInfo].Center = new PixelPoint(0, 0);

	// 操作ボタンベース
	Sprites[SpriteId.ButtonBase].Size = new PixelSize(198, 120);
	Sprites[SpriteId.ButtonBase].SheetPosition = new PixelPoint(361, 3250);
	Sprites[SpriteId.ButtonBase].FramesPerRow=1;
	Sprites[SpriteId.ButtonBase].Center = new PixelPoint(0, 0);

	// 操作ボタンベース
	Sprites[SpriteId.Buttons1].Size = new PixelSize(140, 20);
	Sprites[SpriteId.Buttons1].SheetPosition = new PixelPoint(0, 3250);
	Sprites[SpriteId.Buttons1].FramesPerRow=1;
	Sprites[SpriteId.Buttons1].Center = new PixelPoint(0, 0);

	// マップベース
	Sprites[SpriteId.Minimap].Size = new PixelSize(256-1, 200-1);
	Sprites[SpriteId.Minimap].SheetPosition = new PixelPoint(0, 4340);
	Sprites[SpriteId.Minimap].FramesPerRow=1;
	Sprites[SpriteId.Minimap].Center = new PixelPoint(0, 0);

	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cloud_in_start")]
public void	InitializeClouds()
	{
	 Array16<int> freeClouds = default;

	while(true)
		{
		// 空いてるくもスプライトを探します。
		var n=0;
		for(var m=0; m<KUMO_MAX; m++)
			{
			if( Clouds[m].Used==0 )
				{
				freeClouds[n]=m;
				n++;
				}
			if(n>=16)
				break;
			}

		if( n<=14 )
			return;

		var baseX=(double)(Random(abs(MAP_RIGHT)+abs(MAP_LEFT))-abs(MAP_LEFT));
		var baseY=(double)(Random(abs(MAP_TOP)+abs(MAP_BOTTOM))-abs(MAP_BOTTOM));

		var offsetX=(double)( Random(Sprites[SpriteId.MapTiles].Size.Width*4)-Sprites[SpriteId.MapTiles].Size.Width*2 );

		n=0;
		for( var m=0;m<3;m++)
			{
			Clouds[freeClouds[n]].Used=1;
			Clouds[freeClouds[n]].Position = new WorldPosition(baseX+m*80+offsetX, baseY);
			Clouds[freeClouds[n]].Kind=1;
			n++;
			}
		offsetX=(double)( Random(Sprites[SpriteId.MapTiles].Size.Width*2)-Sprites[SpriteId.MapTiles].Size.Width*2 );
		for( var m=0;m<5;m++)
			{
			Clouds[freeClouds[n]].Used=1;
			Clouds[freeClouds[n]].Position = new WorldPosition(baseX+m*80-80+offsetX, baseY+80);
			Clouds[freeClouds[n]].Kind=1;
			n++;
			}
		offsetX=(double)( Random(Sprites[SpriteId.MapTiles].Size.Width*2)-Sprites[SpriteId.MapTiles].Size.Width*2 );
		for( var m=0;m<3;m++)
			{
			Clouds[freeClouds[n]].Used=1;
			Clouds[freeClouds[n]].Position = new WorldPosition(baseX+m*80+offsetX, baseY+80*2);
			Clouds[freeClouds[n]].Kind=1;
			n++;
			}

		}

	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cloud_cont")]
public void	UpdateClouds()
	{
	int n; Array16<int> freeClouds = default;

	for(n=0;n<KUMO_MAX;n++)
		{
		ref var cloud = ref Clouds[n];
		if( cloud.Used!=0 )
			{
			if( Result==GameResult.None )
				{
				cloud.Position -= new WorldVector(2.0, 0);
				if( cloud.Position.X < MAP_LEFT )
					cloud.Used=0;
				}
			}
		}

	// 空いてるくもスプライトを探します。
	n=0;
	for(var m=0; m<KUMO_MAX; m++)
		{
		if( Clouds[m].Used==0 )
			{
			freeClouds[n]=m;
			n++;
			}
		if(n>=16)
			break;
		}

	if( n<=14 )
		return;

	var baseY=(double)(Random(abs(MAP_TOP)+abs(MAP_BOTTOM))-abs(MAP_BOTTOM));
	var offsetX=(double)( Random(Sprites[SpriteId.MapTiles].Size.Width*4)-Sprites[SpriteId.MapTiles].Size.Width*2 );
	n=0;
	for( var m=0;m<3;m++)
		{
		Clouds[freeClouds[n]].Used=1;
		Clouds[freeClouds[n]].Position = new WorldPosition(MAP_RIGHT+m*80+offsetX, baseY);
		Clouds[freeClouds[n]].Kind=1;
		n++;
		}
	offsetX=(double)( Random(Sprites[SpriteId.MapTiles].Size.Width*2)-Sprites[SpriteId.MapTiles].Size.Width*2 );
	for( var m=0;m<5;m++)
		{
		Clouds[freeClouds[n]].Used=1;
		Clouds[freeClouds[n]].Position = new WorldPosition(MAP_RIGHT+m*80-80+offsetX, baseY+80);
		Clouds[freeClouds[n]].Kind=1;
		n++;
		}
	offsetX=(double)( Random(Sprites[SpriteId.MapTiles].Size.Width*2)-Sprites[SpriteId.MapTiles].Size.Width*2 );
	for( var m=0;m<3;m++)
		{
		Clouds[freeClouds[n]].Used=1;
		Clouds[freeClouds[n]].Position = new WorldPosition(MAP_RIGHT+m*80+offsetX, baseY+80*2);
		Clouds[freeClouds[n]].Kind=1;
		n++;
		}

	}

private void UpdateCargo(ref Fire fire, ref int n)
	{
	WorldRect landingArea;
	int f;
	if( fire.Ticks<=fire.FlightTime )
		{
		var previousPosition=fire.Position;
		fire.Position += WorldVector.FromDirection(fire.Direction) * fire.Speed;
		fire.Speed+=fire.Acceleration;

		var lands=0;

		// ptin dbg
		landingArea=WorldRect.Around(fire.TargetPoint, 20, 20);
		if( landingArea.Contains(fire.Position) )
			lands=1;

		if( lands!=0 )
			{
			lands=1;
			// 上陸地点到達
			for(n=1;n<=MaxUnitId && lands!=0 ;n++)
				{
				if( Units[n].IsUsed && Units[n].Kind>=UnitKind.AirBase && Units[n].Kind<=UnitKind.Fortress )
					{
					// ptin dbg
					landingArea=WorldRect.Around(Units[n].Position, 20, 20);
					if( landingArea.Contains(fire.Position) )
						{
						// とにかくほかの地上施設の上
						lands=0;
						}
					}
				}

			if(lands!=0)
				{
				// NSPW NET builds a base over 8000 to 20000 ticks, from a quarter of its HP; 1.22 completes it at once, with
				// its full HP (0x418E23).
				switch(fire.Kind)
					{
					case FireKind.CargoInfantryBase:
						f=AddUnit((Side)fire.ShooterSide, UnitKind.InfantryBase,(double)fire.TargetX,(double)fire.TargetY,0);
						Units[f].BuildTime=0;												// 建設期間
						break;
					case FireKind.CargoPillboxes:
						f=AddUnit((Side)fire.ShooterSide, UnitKind.Pillboxes,(double)fire.TargetX,(double)fire.TargetY,0);
						if( !Rules.LandedBasesAreBuilt )
							break;
						Units[f].BuildTime=(int)(8000*( fire.ShooterSide==(int)Side.UnitedStates ? 1.0 : 0.8 ));		// 建設期間
						Units[f].Hp/=4;
						Units[f].MaxHp/=4;
						break;
					case FireKind.CargoFortress:
						f=AddUnit((Side)fire.ShooterSide, UnitKind.Fortress,(double)fire.TargetX,(double)fire.TargetY,0);
						if( !Rules.LandedBasesAreBuilt )
							break;
						Units[f].BuildTime=(int)(15000*( fire.ShooterSide==(int)Side.UnitedStates ? 0.8 : 1.0 ));		// 建設期間
						Units[f].Hp/=4;
						Units[f].MaxHp/=4;
						break;
					case FireKind.CargoAirBase:
						f=AddUnit((Side)fire.ShooterSide, UnitKind.AirBase,(double)fire.TargetX,(double)fire.TargetY,0);
						if( !Rules.LandedBasesAreBuilt )
							break;
						Units[f].BuildTime=(int)(10000*( fire.ShooterSide==(int)Side.UnitedStates ? 0.8 : 1.0 ));		// 建設期間
						Units[f].Hp/=4;
						Units[f].MaxHp/=4;
						break;
					case FireKind.CargoNavalBase:
						f=AddUnit((Side)fire.ShooterSide, UnitKind.NavalBase,(double)fire.TargetX,(double)fire.TargetY,0);
						if( !Rules.LandedBasesAreBuilt )
							break;
						Units[f].BuildTime=(int)(20000*( fire.ShooterSide==(int)Side.UnitedStates ? 0.8 : 1.0 ));		// 建設期間
						Units[f].Hp/=4;
						Units[f].MaxHp/=4;
						break;
					}
				}
			fire.Target=0;

			}
		else
			{
			// 当たってないので
			fire.Ticks++;
			if( fire.Ticks==1 )
				{
				f=FindFreeEffect();
				Effects[f].Layer=EffectLayer.Lower;
				Effects[f].TimeLeft=30;
				Effects[f].Animation=0;

				Effects[f].Position=fire.Position;

				Effects[f].SpriteNumber=8;			// ソースファイル上の番号
				}

			if( fire.Ticks>=fire.ArmingTime && IsEffectTick )
				{
				if(!( fire.Position.Y>MAP_TOP || fire.Position.Y<MAP_BOTTOM || fire.Position.X<MAP_LEFT || fire.Position.X>MAP_RIGHT ))
					{
					var column=(int)((fire.Position.X+(Sprites[SpriteId.JapanUnits].Size.Width/2)-MAP_LEFT)/Sprites[SpriteId.MapTiles].Size.Width);
					var row=(int)((MAP_TOP-fire.Position.Y+(Sprites[SpriteId.JapanUnits].Size.Height/2))/Sprites[SpriteId.MapTiles].Size.Height);

					if( MapTiles[row][column]==0)
						{
						// 海の上
						if( (Tick%5)==0)
							{
							f=FindFreeEffect();
							Effects[f].Layer=EffectLayer.Lower;
							Effects[f].TimeLeft=30+EffectRandom(25);
							Effects[f].Animation=4;

							Effects[f].Position = previousPosition;

							Effects[f].SpriteNumber=8;			// ソースファイル上の番号
							}
						}
					}

				f=FindFreeEffect();
				Effects[f].Layer=EffectLayer.Lower;
				Effects[f].TimeLeft=1;
				Effects[f].Animation=0;

				Effects[f].Position=fire.Position;

				Effects[f].SpriteNumber=36+ToEightDirections((int)(fire.Direction));			// ソースファイル上の番号

				}
			}
		//effect[f].no=72;			// ソースファイル上の番号
		}
	else
		{
		fire.Target=0;
		}
	}

private void UpdateBullet(ref Fire fire, ref int n, int m)
	{
	WorldRect hitArea;
	if( fire.Speed>=fire.FinalSpeed )
		{
		var previousPosition=fire.Position;
		fire.Position += WorldVector.FromDirection(fire.Direction) * fire.Speed;
		fire.Speed+=fire.Acceleration;

		n=fire.Target;						// ターゲットナンバー

		// ptin dbg
		hitArea=WorldRect.Around(Units[n].Position, 10, 10);

		if( hitArea.Contains(fire.Position) )
			{
			// 命中
			fire.Target=0;
			if(ShowsAntiAir==0)
				{
				// 1.22 damages a transport with every bullet (0x418F92).
				if( Units[n].Kind!=UnitKind.Transport || !Rules.BulletsSpareTransports || Random(4)==0 )
					Units[n].Hp-=GetDamagePoints(m);
				}

			var f=FindFreeEffect();
			ref var effect = ref Effects[f];
			effect.Layer=EffectLayer.Upper;

			effect.TimeLeft=20;
			effect.Animation=1;	// アニメーションパターン

			effect.Position=fire.Position;
			effect.SpriteNumber=1;			// 弾丸着弾	のソースファイル上の番号
			}
		else
			{
			if( IsEffectTick )
				{
				// 弾丸描画
				var f=FindFreeEffect();
				ref var effect = ref Effects[f];
				effect.Layer=EffectLayer.Upper;
				effect.TimeLeft=1;	effect.Animation=11;
				effect.Position=fire.Position;
				effect.EndPosition = previousPosition;
				}
			}

		}
	else
		{
		fire.Target=0;
		}
	}

private void UpdateRapidAntiAircraftShell(ref Fire fire, ref int n, int m)
	{
	WorldRect burstArea;
	fire.Ticks--;
	if( fire.Ticks!=0 )
		{	//
		var previousPosition=fire.Position;
		fire.Position += WorldVector.FromDirection(fire.Direction) * fire.Speed;

		fire.Speed+=fire.Acceleration;		// 弾が減速

		if( IsEffectTick )
			{
			var f=FindFreeEffect();
			ref var effect = ref Effects[f];
			effect.Layer=EffectLayer.Upper;
			effect.TimeLeft=1;	effect.Animation=10;
			effect.Position=fire.Position;
			effect.EndPosition = previousPosition;

			effect.SpriteNumber=0;			// ソースファイル上の番号
			}

		}
	else
		{	// 炸裂！
		fire.Position += WorldVector.FromDirection(fire.Direction) * fire.Speed;

		// 砲弾炸裂
		var f=FindFreeEffect();
		ref var effect = ref Effects[f];
		effect.Layer=EffectLayer.Lower;

		effect.TimeLeft=10;
		effect.Animation=4;	// アニメーションパターン

		effect.Position=fire.Position;
		effect.SpriteNumber=0;			// 弾丸着弾	のソースファイル上の番号

		// ptin dbg
		burstArea=WorldRect.Around(fire.Position, 40, 40);

		for(n=1;n<=MaxUnitId;n++)
			{
			ref var unit = ref Units[n];
			if( unit.IsUsed && unit.Side==Units[fire.Target].Side && unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Flying )
				{
				if( burstArea.Contains(unit.Position)  )
					{
					// 命中
					// BUG(1.22, NSPW NET): clearing the fire's target compares the next planes with unit 0's side, none, so the
					// burst hits the first plane only (0x41957B). Kept as is for compatibility with 1.22 and NSPW NET.
					fire.Target=0;
					if(ShowsAntiAir==0  )
						{
						unit.Hp-=GetDamagePoints(m);
						if(unit.Speed<=unit.MaxSpeed )
							unit.Direction=Random(360);
						}

					f=FindFreeEffect();
					effect = ref Effects[f];
					effect.Layer=EffectLayer.Upper;

					effect.TimeLeft=20;
					effect.Animation=2;	// アニメーションパターン

					effect.Position=unit.Position;
					effect.SpriteNumber=1;			// 弾丸着弾	のソースファイル上の番号
					}
				}
			}
		fire.Target=0;
		}
	}

private void UpdateAntiSubmarineBomb(ref Fire fire, ref int n, int m)
	{
	fire.Ticks++;
	if( fire.Ticks==5 )
		{
		var f=FindFreeEffect();
		ref var effect = ref Effects[f];
		effect.Layer=EffectLayer.Lower;
		effect.TimeLeft=30;
		effect.Animation=4;

		effect.Position=fire.Position;

		effect.SpriteNumber=7;			// ソースファイル上の番号
		}
	if( fire.Ticks==fire.FlightTime )
		{	// バクハツ！
		if( Rules.ExplodedDepthChargesKeepTheirFire )
			fire.Target=0;

		var f=FindFreeEffect();
		ref var effect = ref Effects[f];

		PlaySoundEffect( 0, SoundId.TorpedoHit1 ,fire.Position.X, fire.Position.Y);

		effect.Layer=EffectLayer.Lower;

		effect.TimeLeft=40;
		effect.Animation=4;	// アニメーションパターン

		effect.Position=fire.Position;
		effect.SpriteNumber=11;			//ソースファイル上の番号

		var hit=0;
		for(n=1;n<=MaxUnitId;n++)
			{
			ref var unit = ref Units[n];
			if( unit.IsUsed && unit.Kind==UnitKind.Submarine && unit.IsSubmerged )
				{
				fire.Target=n;
				hit=CheckHit(m);
				if( hit!=0 )
					break;
				}
			}

		if( hit!=0 )
			{
			// 命中
			Units[n].Hp-=GetDamagePoints(m);
			}
		if( !Rules.ExplodedDepthChargesKeepTheirFire )
			fire.Target=0;
		}
	}

private void UpdateAntiAircraftShell(ref Fire fire, ref int n, int m)
	{
	WorldRect burstArea;
	fire.Ticks--;
	if( fire.Ticks!=0 )
		{	//
		var previousPosition=fire.Position;
		fire.Position += WorldVector.FromDirection(fire.Direction) * fire.Speed;

		fire.Speed+=fire.Acceleration;		// 弾が減速
		if( IsEffectTick )
			{
			// 弾自体の絵
			var f=FindFreeEffect();
			ref var effect = ref Effects[f];
			effect.Layer=EffectLayer.Upper;
			effect.TimeLeft=1;	effect.Animation=0;
			effect.Position=fire.Position;
			effect.SpriteNumber=96+ToEightDirections((int)(fire.Direction));			// ソースファイル上の番号
			// 弾の煙
			f=FindFreeEffect();
			effect = ref Effects[f];
			effect.Layer=EffectLayer.Upper;
			effect.TimeLeft=3+EffectRandom(3);
			effect.Animation=4;

			effect.Position = new WorldPosition(previousPosition.X+EffectRandom(10)-5, previousPosition.Y+EffectRandom(10)-5);

			effect.SpriteNumber=2;			// ソースファイル上の番号
			}
		}
	else
		{	// 炸裂！
		fire.Position += WorldVector.FromDirection(fire.Direction) * fire.Speed;

		// 砲弾炸裂
		// 煙
		var f=FindFreeEffect();
		ref var effect = ref Effects[f];
		effect.Layer=EffectLayer.Lower;
		effect.TimeLeft=80+Random(80);
		effect.Animation=2;
		effect.Position=fire.Position;
		effect.SpriteNumber=6;			// ソースファイル上の番号

		// 漠炎
		f=FindFreeEffect();
		effect = ref Effects[f];
		effect.Layer=EffectLayer.Lower;
		effect.TimeLeft=10;
		effect.Animation=4;	// アニメーションパターン
		effect.Position=fire.Position;
		effect.SpriteNumber=0;			// 弾丸着弾	のソースファイル上の番号

		f=40;
		// ptin dbg
		burstArea=WorldRect.Around(fire.Position, f, f);

		for(n=1;n<=MaxUnitId;n++)
			{
			ref var unit = ref Units[n];
			if( unit.IsUsed && unit.Side==Units[fire.Target].Side && unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Flying )
				{
				if( burstArea.Contains(unit.Position) && !( unit.Kind==UnitKind.Bomber && unit.Side==Side.UnitedStates && Random(3)!=0 ) )
					{
					// 命中
					if(ShowsAntiAir==0  )
						{
						unit.Hp-=GetDamagePoints(m);

						if(unit.Speed<=unit.MaxSpeed )
							{
							unit.Direction=Random(360);
							}

						}

					f=FindFreeEffect();
					effect = ref Effects[f];
					effect.Layer=EffectLayer.Upper;

					effect.TimeLeft=20;
					effect.Animation=2;	// アニメーションパターン

					effect.Position=unit.Position;
					effect.SpriteNumber=1;			// 弾丸着弾	のソースファイル上の番号
					}
				}
			}
		fire.Target=0;
		}
	}

private void UpdateGunShell(ref Fire fire, ref int n, int m)
	{
	fire.Ticks--;
	if( fire.Ticks!=0 )
		{	//
		var previousPosition=fire.Position;
		fire.Position += WorldVector.FromDirection(fire.Direction) * fire.Speed;
		if( fire.Ticks > fire.FlightTime )
			fire.Speed-=fire.Acceleration;		// 弾が上昇中
		else
			fire.Speed+=(fire.Acceleration*2.83);		// 弾が降下中
		if( IsEffectTick )
			{
			// 弾自体の絵
			var f=FindFreeEffect();
			ref var effect = ref Effects[f];
			effect.Layer=EffectLayer.Upper;
			effect.TimeLeft=1;	effect.Animation=0;
			effect.Position=fire.Position;
			effect.SpriteNumber=108+ToEightDirections((int)(fire.Direction));			// ソースファイル上の番号
			// 弾の煙
			f=FindFreeEffect();
			effect = ref Effects[f];
			effect.Layer=EffectLayer.Upper;
			effect.TimeLeft=3+EffectRandom(3);
			effect.Animation=4;

			effect.Position = new WorldPosition(previousPosition.X+EffectRandom(10)-5, previousPosition.Y+EffectRandom(10)-5);

			effect.SpriteNumber=2;			// ソースファイル上の番号
			}
		}
	else
		{	// 着弾！
		fire.Position += WorldVector.FromDirection(fire.Direction) * fire.Speed;
		n=fire.Target;
		var hit=CheckHit(m);
		fire.Target=0;
		if( Units[n].IsUsed && hit!=0)
			{
			// 命中
			Units[n].Hp-=GetDamagePoints(m);

			PlaySoundEffect( 0, SoundId.TorpedoHit1 ,fire.Position.X, fire.Position.Y);

			var f=FindFreeEffect();
			ref var effect = ref Effects[f];
			effect.Layer=EffectLayer.Upper;

			effect.TimeLeft=40;
			effect.Animation=4;	// アニメーションパターン

			effect.Position=fire.Position;

			effect.SpriteNumber=1;			// 弾丸着弾	のソースファイル上の番号

			// 当った的に収納機があれば破壊される場合もある
			if( Units[n].Kind==UnitKind.AirBase || Units[n].Kind==UnitKind.Carrier || Units[n].Kind==UnitKind.LightCarrier )
				{
				for(var i=0;i<=MaxUnitId;i++)
					{
					ref var unit = ref Units[i];
					if( unit.IsUsed && unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked && unit.Carrier==n && Random(10)==0 )
						{
						unit.Side=0;
						Units[unit.Carrier].PlaneCount--;	// 現在格納数

						if( unit.DeckPhase>=1 && Units[unit.Carrier].PlanesToLaunch>=1 && unit.Mode<=UnitMode.Slow )
							Units[unit.Carrier].PlanesToLaunch--;		// 発艦予定の機数を
						if( unit.DeckPhase>=3 && Units[unit.Carrier].LandingLock>=1  && unit.Mode<=UnitMode.Slow )
							Units[unit.Carrier].LandingLock--;		//

						if( unit.Mode==UnitMode.Return )
							{
							if(Units[unit.Carrier].LandingLock!=0)
								Units[unit.Carrier].LandingLock=0;	// 着艦、0許可、1不許可
							if(Units[unit.Carrier].LaunchLock!=0)
								Units[unit.Carrier].LaunchLock=0;	// その空母の次機発進許可	0許可、1不許可
							}

						if( SelectedUnit==i )
							{ SelectedUnit=0; CombatMenuKind=0; CombatMenuSelection=CombatMenuItem.None; ClearSelection2(1); }

						break;
						}
					}

				}

			}
		else
			{
			// ハズレ
			PlaySoundEffect( 0, SoundId.Splash ,fire.Position.X, fire.Position.Y);

			var f=FindFreeEffect();
			ref var effect = ref Effects[f];
			effect.Layer=EffectLayer.Lower;

			effect.TimeLeft=40;
			effect.Animation=4;	// アニメーションパターン

			effect.Position=fire.Position;

			effect.SpriteNumber=8;			// 弾丸着弾	のソースファイル上の番号
			if( IsLandAt(fire.Position) )
				effect.SpriteNumber=10;			// 弾丸着弾	のソースファイル上の番号

			}
		}
	}

private void UpdateTorpedo(ref Fire fire, ref int n, int m)
	{
	if( fire.Ticks<=fire.FlightTime )
		{
		if( IsLandAt(fire.Position+WorldVector.FromDirection(fire.Direction) * -40) )
			{
			fire.Ticks=fire.FlightTime;
			}

		var previousPosition=fire.Position;
		fire.Position += WorldVector.FromDirection(fire.Direction) * fire.Speed;
		fire.Speed+=fire.Acceleration;

		var hit=0;
		if( fire.Ticks>=fire.ArmingTime )
			{
			for(n=1;n<=MaxUnitId;n++)
				{
				ref var unit = ref Units[n];
				if( unit.IsUsed && unit.Category==UnitCategory.Ship && unit.Kind>=UnitKind.Battleship && unit.Kind<=UnitKind.Transport && !(unit.Kind==UnitKind.Submarine && unit.IsSubmerged))
					{
					fire.Target=n;
					hit=CheckHit(m);
					if( hit!=0 )
						break;
					}
				}
			}

		if( hit!=0 )
			{
			// 命中
			fire.Target=0;
			Units[n].Hp-=GetDamagePoints(m);

			PlaySoundEffect( 0, SoundId.TorpedoHit1+Random(2) ,fire.Position.X, fire.Position.Y);

			var f=FindFreeEffect();
			ref var effect = ref Effects[f];
			effect.Layer=EffectLayer.Lower;

			effect.TimeLeft=40;
			effect.Animation=4;	// アニメーションパターン

			effect.Position=fire.Position;
			effect.SpriteNumber=11;			// 弾丸着弾	のソースファイル上の番号
			}
		else
			{
			// 当たってないので
			fire.Ticks++;
			if( fire.Ticks==1 )
				{
				var f=FindFreeEffect();
				ref var effect = ref Effects[f];
				effect.Layer=EffectLayer.Lower;
				effect.TimeLeft=30;
				effect.Animation=0;

				effect.Position=fire.Position;

				effect.SpriteNumber=8;			// ソースファイル上の番号
				}

			if( fire.Ticks>=fire.ArmingTime && IsEffectTick )
				{
				var f=FindFreeEffect();
				ref var effect = ref Effects[f];
				effect.Layer=EffectLayer.Lower;
				effect.TimeLeft=1;
				effect.Animation=0;

				effect.Position=fire.Position;

				effect.SpriteNumber=72+ToEightDirections((int)(fire.Direction));			// ソースファイル上の番号
				if( (Tick%3)==0)
					{
					f=FindFreeEffect();
					effect = ref Effects[f];
					effect.Layer=EffectLayer.Lower;
					effect.TimeLeft=30+EffectRandom(25);
					effect.Animation=4;

					effect.Position = new WorldPosition(previousPosition.X+EffectRandom(10)-5, previousPosition.Y+EffectRandom(10)-5);

					effect.SpriteNumber=8;			// ソースファイル上の番号
					}
				}
			}
		//effect[f].no=72;			// ソースファイル上の番号
		}
	else
		{
		fire.Target=0;
		}
	}

private void UpdateBomb(ref Fire fire, ref int n, int m)
	{
	int column;
	int row;
	fire.Ticks++;

	if( fire.Target==(int)UnitKind.Attacker && fire.Ticks==12 )
		PlaySoundEffect( 0, SoundId.BombRelease ,fire.Position.X, fire.Position.Y);

	if( fire.Ticks<=fire.FlightTime  )
		{	// 爆弾降下中
		if( fire.Ticks>=50)
			{
			if( fire.Ticks==50)
			PlaySoundEffect( 0, SoundId.BombFalling ,fire.Position.X, fire.Position.Y);

			fire.Position += WorldVector.FromDirection(fire.Direction) * fire.Speed;
			fire.Speed+=fire.Acceleration;
			if( IsEffectTick )
				{
				var f=FindFreeEffect();
				ref var effect = ref Effects[f];
				effect.Layer=EffectLayer.Upper;
				effect.TimeLeft=1;	effect.Animation=0;
				effect.Position=fire.Position;
				effect.SpriteNumber=84+ToEightDirections((int)(fire.Direction));			// ソースファイル上の番号
				}
			}
		}
	else
		{	// 着弾！
		fire.Position += WorldVector.FromDirection(fire.Direction) * fire.Speed;
		var hit=0;
		for(n=1;n<=MaxUnitId;n++)
			{
			ref var unit = ref Units[n];
			if( unit.IsUsed && unit.Category==UnitCategory.Ship && !(unit.Kind==UnitKind.Submarine && unit.IsSubmerged))
				{
				fire.Target=n;
				hit=CheckHit(m);
				if( hit!=0 )
					break;
				}
			}
		fire.Target=0;
		if( hit!=0 )
			{
			// 命中
			Units[n].Hp-=GetDamagePoints(m);

			PlaySoundEffect( 0, SoundId.BombHit1+Random(2) ,fire.Position.X, fire.Position.Y);

			var f=FindFreeEffect();
			ref var effect = ref Effects[f];
			effect.Layer=EffectLayer.Upper;

			effect.TimeLeft=40;
			effect.Animation=4;	// アニメーションパターン

			effect.Position=fire.Position;
			effect.SpriteNumber=1;			// 弾丸着弾	のソースファイル上の番号

			// 当った的に収納機があれば破壊される場合もある
			if( Units[n].Kind==UnitKind.AirBase || Units[n].Kind==UnitKind.Carrier || Units[n].Kind==UnitKind.LightCarrier )
				{
				for(var i=0;i<=MaxUnitId;i++)
					{
					ref var unit = ref Units[i];
					if( unit.IsUsed && unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked && unit.Carrier==n && Random(10)==0 )
						{
						unit.Side=0;
						Units[unit.Carrier].PlaneCount--;	// 現在格納数

						if( unit.DeckPhase>=1 && Units[unit.Carrier].PlanesToLaunch>=1 && unit.Mode<=UnitMode.Slow )
							Units[unit.Carrier].PlanesToLaunch--;		// 発艦予定の機数を
						if( unit.DeckPhase>=3 && Units[unit.Carrier].LandingLock>=1  && unit.Mode<=UnitMode.Slow )
							Units[unit.Carrier].LandingLock--;		//

						if(  unit.Mode==UnitMode.Return )
							{
							if(Units[unit.Carrier].LandingLock!=0)
								Units[unit.Carrier].LandingLock=0;	// 着艦、0許可、1不許可
							if(Units[unit.Carrier].LaunchLock!=0)
								Units[unit.Carrier].LaunchLock=0;	// その空母の次機発進許可	0許可、1不許可
							}

						if( SelectedUnit==i )
							{ SelectedUnit=0; CombatMenuKind=0; CombatMenuSelection=CombatMenuItem.None; ClearSelection2(1); }
						break;
						}
					}

				}
			}
		else
			{
			// ハズレ

			var f=FindFreeEffect();
			ref var effect = ref Effects[f];
			effect.Layer=EffectLayer.Lower;

			effect.TimeLeft=40;
			effect.Animation=4;	// アニメーションパターン

			effect.Position=fire.Position;
			if(!( fire.Position.Y>MAP_TOP || fire.Position.Y<MAP_BOTTOM || fire.Position.X<MAP_LEFT || fire.Position.X>MAP_RIGHT ))
				{
				column=(int)((fire.Position.X+(Sprites[SpriteId.JapanUnits].Size.Width/2)-MAP_LEFT)/Sprites[SpriteId.MapTiles].Size.Width);
				row=(int)((MAP_TOP-fire.Position.Y+(Sprites[SpriteId.JapanUnits].Size.Height/2))/Sprites[SpriteId.MapTiles].Size.Height);
				}
			else
				{
				column=0;
				row=0;
				}

			if( MapTiles[row][column]>=1)
				{
				effect.SpriteNumber=10;			// 着弾	のソースファイル上の番号
				PlaySoundEffect( 0, SoundId.BombHit1 ,fire.Position.X, fire.Position.Y);
				}
			else
				{
				effect.SpriteNumber=8;			// 着弾	のソースファイル上の番号
				PlaySoundEffect( 0, SoundId.Splash ,fire.Position.X, fire.Position.Y);
				}
			}
		}
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cont_fire")]
public void	UpdateFires()
	{

	int n=default /* C4701 */ /* C4701 */;

	//=========		 ファイアの制御		=========//
	// 弾丸、爆弾等の機動、炸裂を制御します。
	for(var m=1;m<FIRE_MAX;m++)
		{
		ref var fire = ref Fires[m];
		if( fire.Target!=0 )
			{
			//	揚陸艇
			if( fire.Kind==FireKind.CargoAirBase || fire.Kind==FireKind.CargoNavalBase || fire.Kind==FireKind.CargoInfantryBase || fire.Kind==FireKind.CargoPillboxes || fire.Kind==FireKind.CargoFortress )
				{
				UpdateCargo(ref fire, ref n);
				}

			// 弾丸
			if( fire.Kind==FireKind.Bullet )
				{
				UpdateBullet(ref fire, ref n, m);
				}

			// 対空機関砲
			if( fire.Kind==FireKind.RapidAntiAircraftShell )
				{
				UpdateRapidAntiAircraftShell(ref fire, ref n, m);
				}

			// 対潜水艦爆弾
			if( fire.Kind==FireKind.AntiSubmarineBomb )
				{
				UpdateAntiSubmarineBomb(ref fire, ref n, m);
				}

			// 対空砲
			if( fire.Kind==FireKind.AntiAircraftShell )
				{
				UpdateAntiAircraftShell(ref fire, ref n, m);
				}

			// 艦砲
			if( fire.Kind==FireKind.Gun )
				{
				UpdateGunShell(ref fire, ref n, m);
				}

			//	魚雷
			if( fire.Kind==FireKind.Torpedo )
				{
				UpdateTorpedo(ref fire, ref n, m);
				}

			// 爆撃
			if( fire.Kind==FireKind.Bomb )
				{
				UpdateBomb(ref fire, ref n, m);
				}
			}
		}
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("set_new_ltl_ldr")]
public void	AssignGroupLeader(int m)
	{

	// 部下がまだ生きてるか
	var newLeader=MaxUnitId+1;
	var followers=0;
	for( var n=1; n<=MaxUnitId; n++)
		{
		ref var unit = ref Units[n];
		if( unit.IsUsed && unit.GroupLeader==m )
			{
			if( Units[newLeader].FormationNumber>unit.FormationNumber || newLeader==MaxUnitId+1 )
				newLeader=n;

			followers++;
			}
		}

	if( followers>=2 )
		{

		// 部下が生存
		Units[newLeader].FormationNumber=0;
		Units[newLeader].IsGroupLeader=(short)followers;
		Units[newLeader].GroupLeader=0;

		if( Units[newLeader].Kind==UnitKind.Fighter && (Units[m].Kind==UnitKind.Attacker || Units[m].Kind==UnitKind.Bomber) && Units[newLeader].PlaneState==UnitState.Flying )
			Units[newLeader].Mode=UnitMode.Return;		// それまでの隊長がボスだったらきかんしよっと

		for( var n=1; n<=MaxUnitId; n++)
			{
			ref var unit = ref Units[n];
			if( unit.IsUsed && unit.GroupLeader==m )
				{
				unit.GroupLeader=(short)newLeader;			// ｍｉｎ＿ｎｏが新しい隊長機

				if( unit.Kind==UnitKind.Fighter && (Units[m].Kind==UnitKind.Attacker || Units[m].Kind==UnitKind.Bomber) && unit.PlaneState==UnitState.Flying )
					unit.Mode=UnitMode.Return;		// それまでの隊長がボスだったらきかんしよっと

				}
			}
		// ｐｐ＿ｘ、ｙをコピーします。
		for(var i=0;i<=63;i++)
			{
			Units[newLeader].SetPathPoint(i, Units[m].GetPathPoint(i));
			}
		}

	else if( followers==1 )
		{
		// 部下がひとつ
		Units[newLeader].FormationNumber=0;
		Units[newLeader].IsGroupLeader=0;
		Units[newLeader].GroupLeader=0;
		}
	}

private void UpdateLightlyDamagedPlane(ref Unit unit)
	{
	if( Rules.DamageEffectsTestBeforeDrawing ? (Tick%10)==0 && Random(8)==0 : Random(8)==0 && (Tick%10)==0 )
		{
		var f=FindFreeEffect();
		ref var effect = ref Effects[f];
		effect.Layer=EffectLayer.Upper;
		effect.TimeLeft=40+EffectRandom(15);
		effect.Animation=2;

		effect.Position=unit.Position;
		effect.SpriteNumber=6;			// ソースファイル上の番号

		}
	}

private void UpdateDamagedPlane(ref Unit unit)
	{
	if( Random(500)==0 )
		{
		if(!IsEditingMap)
			unit.Hp--;
		}
	if( Rules.DamageEffectsTestBeforeDrawing ? (Tick%10)==0 && Random(2)==1 : Random(2)==1 && (Tick%10)==0 )
		{
		var f=FindFreeEffect();
		ref var effect = ref Effects[f];
		effect.Layer=EffectLayer.Upper;
		effect.TimeLeft=40+EffectRandom(15);
		effect.Animation=2;

		effect.Position=unit.Position;
		effect.SpriteNumber=6;			// ソースファイル上の番号

		if( Random(7)==0 )
			{
			// 小爆炎
			f=FindFreeEffect();
			effect = ref Effects[f];
			effect.Layer=EffectLayer.Upper;
			effect.TimeLeft=8+EffectRandom(6);
			effect.Animation=4;
			effect.Position = new WorldPosition(unit.Position.X+EffectRandom(6)-3, unit.Position.Y+EffectRandom(6)-3);
			effect.SpriteNumber=10;			// ソースファイル上の番号
			}
		}
	}

private void UpdateHeavilyDamagedPlane(ref Unit unit)
	{
	if( Random(500)==0 )
		{
		if(!IsEditingMap)
			unit.Hp--;
		}

	if( Rules.DamageEffectsTestBeforeDrawing ? (Tick%10)==0 && Random(3)!=0 : Random(3)!=0 && (Tick%(10) )==0 )
		{
		var f=FindFreeEffect();
		ref var effect = ref Effects[f];
		effect.Layer=EffectLayer.Upper;
		effect.TimeLeft=40+EffectRandom(15);
		effect.Animation=2;
		effect.Position=unit.Position;
		effect.SpriteNumber=6;			// ソースファイル上の番号

		if( Random(5)==0 )
			{
			// 小爆炎
			f=FindFreeEffect();
			effect = ref Effects[f];
			effect.Layer=EffectLayer.Upper;
			effect.TimeLeft=8+EffectRandom(6);
			effect.Animation=4;
			effect.Position = new WorldPosition(unit.Position.X+EffectRandom(6)-3, unit.Position.Y+EffectRandom(6)-3);
			effect.SpriteNumber=10;			// ソースファイル上の番号
			}
		}
	}

private void UpdateBurningPlane(ref Unit unit)
	{
	int f;
	// ＨＰはあるが、事実上の墜落、
	if( unit.Hp==unit.MaxHp*0.2 )
		{
		// 飛行機が火を吹く
		if(unit.Kind==UnitKind.Bomber)
			{
			for(var i=0;i<3;i++)
				{
				f=FindFreeEffect();
				ref var effect = ref Effects[f];
				if( f!=0 )
					{
					effect.Layer=EffectLayer.Upper;
					effect.TimeLeft=20+EffectRandom(20);
					effect.Animation=4;
					effect.Position = new WorldPosition(unit.Position.X+20-EffectRandom(40), unit.Position.Y+20-EffectRandom(40));
					effect.SpriteNumber=9;				// ソースファイル上の番号
					}
				}
			}
		else
			{
			f=FindFreeEffect();
			ref var effect = ref Effects[f];
			effect.Layer=EffectLayer.Upper;
			effect.TimeLeft=20+EffectRandom(20);
			effect.Animation=4;
			effect.Position=unit.Position;
			effect.SpriteNumber=9;				// ソースファイル上の番号
			}
		if(!IsEditingMap)
			unit.Hp--;
		}
	else
		{
		if( Random(80)==0 )
			{
			if(!IsEditingMap)
				unit.Hp--;
			}

		if( Rules.DamageEffectsTestBeforeDrawing ? (Tick%10)==0 && Random(5)!=0 : Random(5)!=0 && (Tick%(10))==0 )
			{
			f=FindFreeEffect();
			ref var effect = ref Effects[f];
			effect.Layer=EffectLayer.Upper;
			effect.TimeLeft=40+EffectRandom(15);
			effect.Animation=2;
			effect.Position=unit.Position;
			effect.SpriteNumber=6;			// ソースファイル上の番号
			}

		if( Random(3)==0 && unit.Hp<=unit.MaxHp*0.1 )
			{
			// 小爆炎
			f=FindFreeEffect();
			ref var effect = ref Effects[f];
			effect.Layer=EffectLayer.Upper;
			effect.TimeLeft=8+EffectRandom(6);
			effect.Animation=4;
			effect.Position = new WorldPosition(unit.Position.X+EffectRandom(6)-3, unit.Position.Y+EffectRandom(6)-3);
			effect.SpriteNumber=10;			// ソースファイル上の番号
			}
		}
	}

private void CrashPlane(ref Unit unit, int m)
	{
	int column;
	int row;
	unit.Side=0;

	if( UnitInfoPanel[3]==m )
		UnitInfoPanel[0]=0;				// ユニットインフォをクリア

	var f=FindFreeEffect();
	ref var effect = ref Effects[f];
	effect.Layer=EffectLayer.Lower;
	effect.TimeLeft=80;

	effect.Position=unit.Position;

	if(!( unit.Position.Y>MAP_TOP || unit.Position.Y<MAP_BOTTOM || unit.Position.X<MAP_LEFT || unit.Position.X>MAP_RIGHT ))
		{
		column=(int)((unit.Position.X+(Sprites[SpriteId.JapanUnits].Size.Width/2)-MAP_LEFT)/Sprites[SpriteId.MapTiles].Size.Width);
		row=(int)((MAP_TOP-unit.Position.Y+(Sprites[SpriteId.JapanUnits].Size.Height/2))/Sprites[SpriteId.MapTiles].Size.Height);
		}
	else
		{
		column=0;
		row=0;
		}

	if( MapTiles[row][column]>=1)
		{
		effect.SpriteNumber=9;			// ソースファイル上の番号
		effect.Animation=4;
		}
	else
		{
		effect.SpriteNumber=7;			// ソースファイル上の番号
		effect.Animation=0;
		}

	if( SelectedUnit==m )
		{
		SelectedUnit=0;
		CombatMenuKind=0; CombatMenuSelection=CombatMenuItem.None; ClearSelection2(1);
		}

	if( unit.IsGroupLeader!=0 )
		AssignGroupLeader(m);					// 爆砕されたのがＬＤＲなら、新しいのを決めます。
	}

private void UpdatePlaneEffects(ref Unit unit, int m)
	{
	if(  IsEffectTick && unit.Ammo!=0 && !(unit.ReloadTime!=0 && (FrameCount%3)==0)&& (unit.Weapon==FireKind.Bomb || unit.Weapon==FireKind.Torpedo || unit.Weapon==FireKind.Maintenance || unit.Weapon==FireKind.Unarmed)  && unit.Side==LocalSide && !(unit.PlaneState==UnitState.Parked && UnitInfoPanel[1]==0) && !( unit.PlaneState==UnitState.Parked && unit.DeckPhase>=3 ) && !( unit.PlaneState==UnitState.Parked && unit.Carrier!=UnitInfoPanel[3]))
		{
		var f=FindFreeEffect();
		ref var effect = ref Effects[f];
		effect.Layer=EffectLayer.Lower;
		effect.TimeLeft=1;
		if( unit.PlaneState==UnitState.Flying )
			{
			effect.Animation=0;
			effect.Position = unit.Position - new WorldVector(0, 25);
			}
		else
			{
			effect.Animation=3;
			effect.Position = unit.Position + new WorldVector(0, 25);
			}
		switch( unit.Weapon )
			{
			case FireKind.Torpedo:
				effect.SpriteNumber=3;			// ソースファイル上の番号
				break;
			case FireKind.Bomb:
				effect.SpriteNumber=4;			// ソースファイル上の番号
				break;
			case FireKind.Maintenance:	case FireKind.Unarmed:
				effect.SpriteNumber=15;			// ソースファイル上の番号
				break;
			}
		}

	if( unit.PlaneState==UnitState.Parked )
		{	// 収容後のエフェクト

		}
	else
		{	// 飛行中のエフェクト
		if( unit.Hp<=0 )
			{	// 墜落
			CrashPlane(ref unit, m);

			}
		else if( unit.Hp<=unit.MaxHp*0.2 )
			{
			UpdateBurningPlane(ref unit);
			}
		else if( unit.Hp<=unit.MaxHp*0.3  )
			{
			UpdateHeavilyDamagedPlane(ref unit);

			}
		else if( unit.Hp<=unit.MaxHp*0.5  )
			{
			UpdateDamagedPlane(ref unit);

			}
		else if( unit.Hp<=unit.MaxHp*0.7  )
			{

			UpdateLightlyDamagedPlane(ref unit);
			}
		}
	}

private void LeaveWake(ref Unit unit)
	{
	double distance;
	if( !(unit.Kind==UnitKind.Submarine||unit.Kind==UnitKind.NavalBase||unit.Kind==UnitKind.AirBase||unit.Kind==UnitKind.City||unit.Kind==UnitKind.Mine||unit.Kind==UnitKind.InfantryBase||unit.Kind==UnitKind.Pillboxes||unit.Kind==UnitKind.Fortress) && (Tick%15)==0 && unit.Speed>=unit.MaxSpeed/3 )
		{
		// 航跡のエフェクトを残す
		var f=FindFreeEffect();
		ref var effect = ref Effects[f];
		effect.Layer=EffectLayer.Lower;
		effect.TimeLeft=60+EffectRandom(60);
		effect.Animation=4;
		effect.Position=unit.Position;

		var heading=unit.Direction;
		heading+=180;
		heading=ReduceHeading(heading);

		switch(unit.Kind)
			{
			case UnitKind.Battleship:	case UnitKind.Carrier:
				distance=26;
				break;

			case UnitKind.Cruiser:	case UnitKind.LightCarrier:
				distance=22;
				break;

			default:
				distance=16;
				break;
			}

		effect.Position += WorldVector.FromDirection(heading) * distance;

		effect.SpriteNumber=8;			// ソースファイル上の番号
		}
	}

private void UpdateSurfaceShipEffects(ref Unit unit, int m)
	{
	if( unit.Hp<=0 )
		{	// 沈没

		PlaySoundEffect( 0, SoundId.ShipSinking1 ,unit.Position.X, unit.Position.Y);

		unit.Side=0;

		if( UnitInfoPanel[3]==m )
			UnitInfoPanel[0]=0;

		// 空母なら艦載機とユニットインフォを
		if( unit.Kind==UnitKind.Carrier || unit.Kind==UnitKind.LightCarrier || unit.Kind==UnitKind.AirBase )
			{
			for( var i=1;i<=MaxUnitId;i++)
				{
				ref var other = ref Units[i];
				if( other.IsUsed && other.Category==UnitCategory.Plane && other.PlaneState==UnitState.Parked && other.Carrier==m)
					{
					other.Side=0;
					if( SelectedUnit==i)
						{
						SelectedUnit=0;
						CombatMenuKind=0; CombatMenuSelection=CombatMenuItem.None; ClearSelection2(1);
						}
					}
				}
			}

		DrawDestruction(m);

		if( SelectedUnit==m )
			{
			SelectedUnit=0;
			CombatMenuKind=0; CombatMenuSelection=CombatMenuItem.None; ClearSelection2(1);
			}

		}
	else
		{
		if( unit.Hp<=unit.MaxHp*0.2  )
			{
			if( Random(4000)==0 && !unit.IsSupplying )
				{
				if(!IsEditingMap)
					unit.Hp--;
				}
			if( IsEffectTick && Random(2)!=0  )
				{
				var f=FindFreeEffect();
				ref var effect = ref Effects[f];
				effect.Layer=EffectLayer.Upper;
				effect.TimeLeft=1;
				effect.Animation=4;

				effect.Position=unit.Position;
				effect.SpriteNumber=9;			// ソースファイル上の番号
				}
			}
		else
			{
			if( unit.Hp<=unit.MaxHp*0.5  )
				{
				var n=Random(4);
				if(  IsEffectTick && n==0 )
					{
					var f=FindFreeEffect();
					ref var effect = ref Effects[f];
					effect.Layer=EffectLayer.Upper;
					effect.TimeLeft=1;	effect.Animation=4;
					effect.Position=unit.Position;
					effect.SpriteNumber=9;			// ソースファイル上の番号
					}
				if(  IsEffectTick && n==1 )
					{
					var f=FindFreeEffect();
					ref var effect = ref Effects[f];
					effect.Layer=EffectLayer.Upper;
					effect.TimeLeft=1;	effect.Animation=4;
					effect.Position=unit.Position;
					effect.SpriteNumber=10;			// ソースファイル上の番号
					}
				}
			else
				{
				if( Rules.DamageEffectsTestBeforeDrawing ? IsEffectTick && unit.Hp<=unit.MaxHp*0.7 && Random(10)==1 : Random(10)==1  && unit.Hp<=unit.MaxHp*0.7 )
					{
					var f=FindFreeEffect();
					ref var effect = ref Effects[f];
					effect.Layer=EffectLayer.Upper;
					effect.TimeLeft=1;	effect.Animation=4;
					effect.Position=unit.Position;
					effect.SpriteNumber=10;			// ソースファイル上の番号
					}
				}
			}
		}
	}

private void UpdateSubmergedSubmarineEffects(ref Unit unit, int m)
	{
	if( unit.Hp<=0 )
		{	// 沈没
		unit.Side=0;
		unit.IsFound=false;

		if( UnitInfoPanel[3]==m )
			UnitInfoPanel[0]=0;				// ユニットインフォをクリア

		if( SelectedUnit==m )
			{
			SelectedUnit=0;
			CombatMenuKind=0; CombatMenuSelection=CombatMenuItem.None; ClearSelection2(1);
			}
		}
	else
		{
		if( unit.Hp<=unit.MaxHp*0.2 )
			{	// 空気漏れ
			if( Random(100)==0 )
				{
				if(!IsEditingMap)
					unit.Hp--;
				}
			if( IsEffectTick && Random(300)==1  )
				{
				for(var n=0;n<=3;n++)
					{
					var f=FindFreeEffect();
					ref var effect = ref Effects[f];
					if( f!=0 )
						{
						effect.Layer=EffectLayer.Lower;
						effect.TimeLeft=100+EffectRandom(20);
						effect.Animation=4;
						effect.Position = new WorldPosition(unit.Position.X+EffectRandom(40)-20, unit.Position.Y+EffectRandom(40)-20);
						effect.SpriteNumber=8;			// ソースファイル上の番号
						}
					}
				// ついでに発見される
				unit.ContactX=(int)unit.Position.X;
				unit.ContactY=(int)unit.Position.Y;

				unit.ContactRadius=100;
				unit.ContactTime=400;

				unit.IsFound=true;
				}
			}
		else
			{
			if( unit.Hp<=unit.MaxHp*0.5 )
				{	// 空気漏れ
				if( Random(1000)==0 )
					{
					if(!IsEditingMap)
						unit.Hp--;
					}
				if( IsEffectTick && Random(600)==1  )
					{
					for(var n=0;n<=2;n++)
						{
						var f=FindFreeEffect();
						ref var effect = ref Effects[f];
						if( f!=0)
							{
							effect.Layer=EffectLayer.Lower;
							effect.TimeLeft=100+EffectRandom(20);
							effect.Animation=4;
							effect.Position = new WorldPosition(unit.Position.X+EffectRandom(40)-20, unit.Position.Y+EffectRandom(40)-20);
							effect.SpriteNumber=8;			// ソースファイル上の番号
							}
						}

					// ついでに発見される
					unit.ContactX=(int)unit.Position.X;
					unit.ContactY=(int)unit.Position.Y;

					unit.ContactRadius=100;
					unit.ContactTime=400;

					unit.IsFound=true;
					}
				}
			}
		}
	}

private void UpdateShipEffects(ref Unit unit, int m)
	{
	if(  IsEffectTick && unit.IsSupplying && (FrameCount%2)!=0 && unit.Side==LocalSide )
		{
		var f=FindFreeEffect();
		ref var effect = ref Effects[f];
		effect.Layer=EffectLayer.Lower;
		effect.TimeLeft=1;

		effect.Animation=0;
		effect.Position = unit.Position - new WorldVector(0, 25.0);

		effect.SpriteNumber=15;			// ソースファイル上の番号
		}

	// 武装の表示
// 弾薬の消費サイズ

	if( IsEffectTick && Rules.MarksSecondTypes && ( ( unit.Side==Side.Japan && unit.Kind==UnitKind.Battleship ) || ( unit.Side==Side.UnitedStates && unit.Kind==UnitKind.Carrier ) ) && unit.Variant==1 && unit.Side==LocalSide )
		{
		// 大和級とエセックス
		var f=FindFreeEffect();
		ref var effect = ref Effects[f];
		effect.Layer=EffectLayer.Lower;
		effect.TimeLeft=1;

		effect.Animation=0;
		effect.Position = unit.Position+new WorldVector(18, 25.0);

		effect.SpriteNumber=27;			// ソースファイル上の番号
		}
	else if( IsEffectTick && Rules.MarksSecondTypes && unit.Kind==UnitKind.Destroyer && unit.Variant==1  && unit.Side==LocalSide )
		{
		// 対潜駆逐艦
		var f=FindFreeEffect();
		ref var effect = ref Effects[f];
		effect.Layer=EffectLayer.Lower;
		effect.TimeLeft=1;

		effect.Animation=0;
		effect.Position = unit.Position+new WorldVector(18, 25.0);

		effect.SpriteNumber=26;			// ソースファイル上の番号
		}
	else if( IsEffectTick && Rules.MarksSecondTypes && unit.Kind==UnitKind.Cruiser && unit.Variant==1  && unit.Side==LocalSide )
		{
		// 防空巡洋艦
		var f=FindFreeEffect();
		ref var effect = ref Effects[f];
		effect.Layer=EffectLayer.Lower;
		effect.TimeLeft=1;

		effect.Animation=0;
		effect.Position = unit.Position+new WorldVector(18, 25.0);

		effect.SpriteNumber=16;			// ソースファイル上の番号
		}
	else if( IsEffectTick && ( unit.Side==Side.Japan && ( unit.Kind==UnitKind.Submarine || unit.Kind==UnitKind.Destroyer || unit.Kind==UnitKind.Cruiser ) || unit.Side==Side.UnitedStates && ( unit.Kind==UnitKind.Submarine || unit.Kind==UnitKind.Destroyer ) ) && !unit.IsSupplying && unit.Ammo!=0 && !(unit.ReloadTime!=0 && (FrameCount%2)!=0) && unit.Ammo>=1 && unit.Side==LocalSide )
		{
		var f=FindFreeEffect();
		ref var effect = ref Effects[f];
		effect.Layer=EffectLayer.Lower;
		effect.TimeLeft=1;

		effect.Animation=0;
		effect.Position = unit.Position - new WorldVector(0, 25.0);

		effect.SpriteNumber=3;			// ソースファイル上の番号
		}

	// トランスポーターの荷物の表示
	if(  IsEffectTick && unit.Kind==UnitKind.Transport && !unit.IsSupplying && unit.Ammo!=0 && !(unit.ReloadTime!=0 && (FrameCount%2)!=0) && unit.Side==LocalSide )
		{
		var f=FindFreeEffect();
		ref var effect = ref Effects[f];
		effect.Layer=EffectLayer.Lower;
		effect.TimeLeft=1;

		effect.Animation=0;
		effect.Position = unit.Position - new WorldVector(0, 25.0);

				effect.SpriteNumber=44;			// ソースファイル上の番号
		}

//TR_GF1

	// およその敵潜航潜水艦
	if(  IsEffectTick && unit.Side!=LocalSide && unit.Kind==UnitKind.Submarine && unit.IsSubmerged && unit.ContactTime!=0 )
		{
		var f=FindFreeEffect();
		ref var effect = ref Effects[f];
		effect.Layer=EffectLayer.Upper;
		effect.TimeLeft=1;
		effect.Animation=0;
		effect.Position = unit.ContactPosition;

		effect.SpriteNumber=60;			// ソースファイル上の番号

		f=FindFreeEffect();
		effect = ref Effects[f];
		effect.Layer=EffectLayer.Lower;
		effect.TimeLeft=1;
		effect.Animation=12;
		effect.Position = unit.ContactPosition;
		effect.EndPosition = new WorldPosition(unit.ContactRadius, unit.ContactRadius);
		}

	if( unit.Kind==UnitKind.Submarine && unit.IsSubmerged )
		{	// 潜航中潜水艦
		UpdateSubmergedSubmarineEffects(ref unit, m);
		}
	else
		{	// 水上艦船
		UpdateSurfaceShipEffects(ref unit, m);
		}

	// 航跡のエフェクト
	LeaveWake(ref unit);
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cont_unit_effect")]
public void	UpdateUnitEffects(int m)
	{
	ref var unit = ref Units[m];

	if( IsEffectTick && unit.IsGroupLeader!=0 && unit.Side==LocalSide && unit.PlaneState!=UnitState.Parked  )
		{
		var f=FindFreeEffect();
		ref var effect = ref Effects[f];
		effect.Layer=EffectLayer.Upper;
		effect.TimeLeft=1;
		effect.Animation=0;
		if( unit.Kind==UnitKind.Attacker || unit.Kind==UnitKind.Fighter || unit.Kind==UnitKind.Destroyer || unit.Kind==UnitKind.Submarine)
			effect.Position = new WorldPosition(unit.Position.X, unit.Position.Y+30.0);
		else
			effect.Position = new WorldPosition(unit.Position.X, unit.Position.Y+35.0);

		// 編隊長の旗
		effect.SpriteNumber=24;			// ソースファイル上の番号
		}

	// 航空機のユニットエフェクト
	if ( unit.Category==UnitCategory.Plane )
		{
		// 武装の表示
		UpdatePlaneEffects(ref unit, m);
		}

	// 艦船のエフェクト
	if( unit.Category==UnitCategory.Ship  )
		{
		// 修理と補給中の表示
		UpdateShipEffects(ref unit, m);
		}

	// 艦船のエフェクト
	if( unit.Kind==UnitKind.InfantryBase || unit.Kind==UnitKind.Pillboxes || unit.Kind==UnitKind.Fortress || unit.Kind==UnitKind.AirBase || unit.Kind==UnitKind.InfantryBase || unit.Kind==UnitKind.NavalBase )
		{
		// 建設工事中
		if(  unit.BuildTime!=0 && (FrameCount%2)!=0 && unit.Side==LocalSide )
			{
			var f=FindFreeEffect();
			ref var effect = ref Effects[f];
			effect.Layer=EffectLayer.Upper;
			effect.TimeLeft=1;

			effect.Animation=0;
			effect.Position = unit.Position - new WorldVector(0, 25.0);

			effect.SpriteNumber=15;			// ソースファイル上の番号
			}
		}

	}

private void SelectEditorUnit(ref int length, ref Array128<byte> text)
	{
	switch(EditorTarget)
		{
		case 1:
			length= wsprintf(text, "戦艦",10);
			EditorKind=(byte)UnitKind.Battleship;
			EditorVariant=0;
			break;
		case 2:
			length= wsprintf(text, "巡洋艦",10);
			EditorKind=(byte)UnitKind.Cruiser;
			EditorVariant=0;
			break;
		case 3:
			length= wsprintf(text, "駆逐艦",10);
			EditorKind=(byte)UnitKind.Destroyer;
			EditorVariant=0;
			break;
		case 4:
			length= wsprintf(text, "潜水艦",10);
			EditorKind=(byte)UnitKind.Submarine;
			EditorVariant=0;
			break;
		case 5:
			length= wsprintf(text, "正規空母",10);
			EditorKind=(byte)UnitKind.Carrier;
			EditorVariant=0;
			break;
		case 6:
			length= wsprintf(text, "軽空母",10);
			EditorKind=(byte)UnitKind.LightCarrier;
			EditorVariant=0;
			break;
		case 7:
			length = wsprintf(text, "輸送船(歩兵基地)",10);
			EditorKind=(byte)UnitKind.Transport;
			EditorVariant=(byte)FireKind.CargoInfantryBase;
			break;
		case 8:
			length = wsprintf(text, "輸送船(トーチカ群)",10);
			EditorKind=(byte)UnitKind.Transport;
			EditorVariant=(byte)FireKind.CargoPillboxes;
			break;
		case 9:
			length = wsprintf(text, "輸送船(要塞)",10);
			EditorKind=(byte)UnitKind.Transport;
			EditorVariant=(byte)FireKind.CargoFortress;
			break;
		case 10:
			length = wsprintf(text, "輸送船(航空基地)",10);
			EditorKind=(byte)UnitKind.Transport;
			EditorVariant=(byte)FireKind.CargoAirBase;
			break;
		case 11:
			length = wsprintf(text, "輸送船(軍港)",10);
			EditorKind=(byte)UnitKind.Transport;
			EditorVariant=(byte)FireKind.CargoNavalBase;
			break;

		case 12:
			length= wsprintf(text, "軍港",10);
			EditorKind=(byte)UnitKind.NavalBase;
			EditorVariant=0;
			break;
		case 13:
			length= wsprintf(text, "航空基地",10);
			EditorKind=(byte)UnitKind.AirBase;
			EditorVariant=0;
			break;
		case 14:
			length= wsprintf(text, "都市",10);
			EditorKind=(byte)UnitKind.City;
			EditorVariant=0;
			break;
		case 15:
			length= wsprintf(text, "歩兵基地",10);
			EditorKind=(byte)UnitKind.InfantryBase;
			EditorVariant=0;
			break;
		case 16:
			length= wsprintf(text, "トーチカ群",10);
			EditorKind=(byte)UnitKind.Pillboxes;
			EditorVariant=0;
			break;
		case 17:
			length= wsprintf(text, "要塞",10);
			EditorKind=(byte)UnitKind.Fortress;
			EditorVariant=0;
			break;

		case 18:
			length = wsprintf(text, "戦闘機",10);
			EditorKind=(byte)UnitKind.Fighter;
			EditorVariant=0;
			break;
		case 19:
			length = wsprintf(text, "陸上戦闘機",10);
			EditorKind=(byte)UnitKind.Fighter;
			EditorVariant=1;
			break;
		case 20:
			length= wsprintf(text, "攻撃機",10);
			EditorKind=(byte)UnitKind.Attacker;
			EditorVariant=0;
			break;
		case 21:
			length= wsprintf(text, "戦略爆撃機",10);
			EditorKind=(byte)UnitKind.Bomber;
			EditorVariant=0;
			break;

		case 22:
			length= wsprintf(text, "防空巡洋艦",10);
			EditorKind=(byte)UnitKind.Cruiser;
			EditorVariant=1;
			break;
		case 23:
			length= wsprintf(text, "対潜駆逐艦",10);
			EditorKind=(byte)UnitKind.Destroyer;
			EditorVariant=1;
			break;
		case 24:
			if( LocalSide==Side.Japan )
				{
				length= wsprintf(text, "大和級戦艦",10);
				EditorKind=(byte)UnitKind.Battleship;
				}
			else
				{
				length= wsprintf(text, "エセックス型空母",10);
				EditorKind=(byte)UnitKind.Carrier;
				}
			EditorVariant=1;
			break;

		default:
			length= wsprintf(text, "-----",10);
			break;
		}
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("edit_now")]
public void	UpdateMapEditor()
	{
    Array128<byte> text = default;
	HDC					hdc;

	if ( IDirectDrawSurface_GetDC(lpDDSBack, &hdc) == DD_OK )
		{

		// draw stats, like frame number and frame rate
		SetBkMode(hdc, TRANSPARENT);
		SelectObject(hdc, GameFont1);
		SetTextColor(hdc, RGB(255, 255, 0));

		short japanPlanes=0;
		short japanShips=0;
		short unitedStatesPlanes=0;
		short unitedStatesShips=0;

		for(short i=1;i<=MaxUnitId;i++)
			{
			ref var unit = ref Units[i];
			if( unit.IsUsed )
				{
				if( unit.Side==Side.Japan)
					{
					if( unit.Category==UnitCategory.Plane )
						japanPlanes++;
					else
						japanShips++;
					}
				else
					{
					if( unit.Category==UnitCategory.Plane )
						unitedStatesPlanes++;
					else
						unitedStatesShips++;
					}
				}
			}

#if !LNGG_VER
		var length = wsprintf(text, "日本海軍　：　航空機　%d／%d　艦船又は地上施設　%d／%d　",japanPlanes,JPN_PLANE_END-JPN_PLANE_START+1,japanShips,JPN_SHIP_END-JPN_SHIP_START+1 );
		TextOut(hdc, 0, 530, text, length);
		length = wsprintf(text, "合衆国海軍：　航空機　%d／%d　艦船又は地上施設　%d／%d　",unitedStatesPlanes,USA_PLANE_END-USA_PLANE_START+1,unitedStatesShips,USA_SHIP_END-USA_SHIP_START+1 );
		TextOut(hdc, 0, 550, text, length);

		// 使用説明
		length = wsprintf(text, "F1:SAVE　F2=LOAD　F3:マップ再描画 F5:ユニット選択-> F6:ユニット選択<-");
		TextOut(hdc, 0, 580, text, length);
		length = wsprintf(text, "F7:ユニット消去 F8:ユニット配置 F9:サイドチェンジ F11:増援ポイント変更");
		TextOut(hdc, 0, 600, text, length);
		length = wsprintf(text, "1:ユニットの回転 2:航空機の武装／艦船の停泊 3:損傷回復 4:損傷 5:増燃料 6:減燃料 7:増残弾 8:減残弾");
		TextOut(hdc, 0, 620, text, length);
#else

		len = wsprintf(ach, "Japan Navy : Airplane %d/%d  Ships or Ground Facility %d/%d ",jp_plane,JPN_PLANE_END-JPN_PLANE_START+1,jp_ship,JPN_SHIP_END-JPN_SHIP_START+1 );
		TextOut(hdc, 0, 530, ach, len);
		len = wsprintf(ach, "U.S. Navy  : Airplane %d/%d  Ships or Ground Facility %d/%d ",us_plane,USA_PLANE_END-USA_PLANE_START+1,us_ship,USA_SHIP_END-USA_SHIP_START+1 );
		TextOut(hdc, 0, 550, ach, len);

		// 使用説明
		len = wsprintf(ach, "F1:SAVE　F2=LOAD　F3:MAP REDRAW F5:KIND<- F6:KIND-> F7:UNIT DELETE");
		TextOut(hdc, 0, 580, ach, len);
		len = wsprintf(ach, "F8:PUT UNIT F9:SIDE CHANGE F11:CHANGE REINFORCE POINT");
		TextOut(hdc, 0, 600, ach, len);
		len = wsprintf(ach, "1:ROTATE 2:AIR PLANE'S ARM/ANCHORED 3:1 PT RECOVERY 4:1 PT DAMAGE 5:GAS UP 6:GAS DOWN 7:AMMO UP 8:AMMO DOWN");
		TextOut(hdc, 0, 620, ach, len);

#endif

#if !LNGG_VER

		// 配置ユニット
		SelectEditorUnit(ref length, ref text);
		TextOut(hdc, 120, 640, text, length);

		length = wsprintf(text, "配置ユニット:");
		TextOut(hdc, 0, 640, text, length);

		// 増援場所
		length = wsprintf(text, "増援場所:");
		TextOut(hdc, 0, 660, text, length);

		switch( Reinforcements[(int)LocalSide] )
			{
			case 0:
				length = wsprintf(text, "左上");
				break;
			case 1:
				length = wsprintf(text, "右上");
				break;
			case 2:
				length = wsprintf(text, "右下");
				break;
			case 3:
				length = wsprintf(text, "左下");
				break;
			}

		TextOut(hdc, 80, 660, text, length);

#else

		// 配置ユニット
		switch(put_trgt)
			{
			case 1:
				len= wsprintf(ach, "Battleship",10);
				put_kind=(byte)UnitKind.Battleship;
				put_kind_sub=0;
				break;
			case 2:
				len= wsprintf(ach, "Cruiser",10);
				put_kind=(byte)UnitKind.Cruiser;
				put_kind_sub=0;
				break;
			case 3:
				len= wsprintf(ach, "Destroyer",10);
				put_kind=(byte)UnitKind.Destroyer;
				put_kind_sub=0;
				break;
			case 4:
				len= wsprintf(ach, "Submarine",10);
				put_kind=(byte)UnitKind.Submarine;
				put_kind_sub=0;
				break;
			case 5:
				len= wsprintf(ach, "Carrier",10);
				put_kind=(byte)UnitKind.Carrier;
				put_kind_sub=0;
				break;
			case 6:
				len= wsprintf(ach, "Lt.Carrier",10);
				put_kind=(byte)UnitKind.LightCarrier;
				put_kind_sub=0;
				break;
			case 7:
				len = wsprintf(ach, "Transport(Trenchies)",10);
				put_kind=(byte)UnitKind.Transport;
				put_kind_sub=(byte)FireKind.CargoInfantryBase;
				break;
			case 8:
				len = wsprintf(ach, "Transport(Pillboxes)",10);
				put_kind=(byte)UnitKind.Transport;
				put_kind_sub=(byte)FireKind.CargoPillboxes;
				break;
			case 9:
				len = wsprintf(ach, "Transport(Fortress)",10);
				put_kind=(byte)UnitKind.Transport;
				put_kind_sub=(byte)FireKind.CargoFortress;
				break;
			case 10:
				len = wsprintf(ach, "Transport(Airfield)",10);
				put_kind=(byte)UnitKind.Transport;
				put_kind_sub=(byte)FireKind.CargoAirBase;
				break;
			case 11:
				len = wsprintf(ach, "Transport(port)",10);
				put_kind=(byte)UnitKind.Transport;
				put_kind_sub=(byte)FireKind.CargoNavalBase;
				break;

			case 12:
				len= wsprintf(ach, "Military Port",10);
				put_kind=(byte)UnitKind.NavalBase;
				put_kind_sub=0;
				break;
			case 13:
				len= wsprintf(ach, "Airfield",10);
				put_kind=(byte)UnitKind.AirBase;
				put_kind_sub=0;
				break;
			case 14:
				len= wsprintf(ach, "City",10);
				put_kind=(byte)UnitKind.City;
				put_kind_sub=0;
				break;
			case 15:
				len= wsprintf(ach, "Trenchies",10);
				put_kind=(byte)UnitKind.InfantryBase;
				put_kind_sub=0;
				break;
			case 16:
				len= wsprintf(ach, "Pillboxes",10);
				put_kind=(byte)UnitKind.Pillboxes;
				put_kind_sub=0;
				break;
			case 17:
				len= wsprintf(ach, "Fortress",10);
				put_kind=(byte)UnitKind.Fortress;
				put_kind_sub=0;
				break;

			case 18:
				len = wsprintf(ach, "Car.Fighter",10);
				put_kind=(byte)UnitKind.Fighter;
				put_kind_sub=0;
				break;
			case 19:
				len = wsprintf(ach, "Grn.Fighter",10);
				put_kind=(byte)UnitKind.Fighter;
				put_kind_sub=1;
				break;
			case 20:
				len= wsprintf(ach, "Car.Bomber",10);
				put_kind=(byte)UnitKind.Attacker;
				put_kind_sub=0;
				break;
			case 21:
				len= wsprintf(ach, "Bomber",10);
				put_kind=(byte)UnitKind.Bomber;
				put_kind_sub=0;
				break;

			case 22:
				len= wsprintf(ach, "AntiAir Cruiser",10);
				put_kind=(byte)UnitKind.Cruiser;
				put_kind_sub=1;
				break;
			case 23:
				len= wsprintf(ach, "AntiSub Destroyer",10);
				put_kind=(byte)UnitKind.Destroyer;
				put_kind_sub=1;
				break;
			case 24:
				if( your_side==Side.Japan )
					{
					len= wsprintf(ach, "Class Yamato",10);
					put_kind=(byte)UnitKind.Battleship;
					}
				else
					{
					len= wsprintf(ach, "Type Essex",10);
					put_kind=(byte)UnitKind.Carrier;
					}
				put_kind_sub=1;
				break;

			default:
				len= wsprintf(ach, "-----",10);
				break;
			}
		TextOut(hdc, 120, 640, ach, len);

		len = wsprintf(ach, "PUT UNIT:");
		TextOut(hdc, 0, 640, ach, len);

		// 増援場所
		len = wsprintf(ach, "Reinforce:");
		TextOut(hdc, 0, 660, ach, len);

		switch( rein[(int)your_side] )
			{
			case 0:
				len = wsprintf(ach, "Left-Top");
				break;
			case 1:
				len = wsprintf(ach, "Right-Top");
				break;
			case 2:
				len = wsprintf(ach, "Right-bottom");
				break;
			case 3:
				len = wsprintf(ach, "Left-Bottom");
				break;
			}

		TextOut(hdc, 80, 660, ach, len);

#endif

		IDirectDrawSurface_ReleaseDC(lpDDSBack, hdc);
		}
	}
}
