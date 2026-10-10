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
	short	i;

	for(i=0;i<4096;i++)
		{
		SharedRandomTable[i]=Random(65536);
		}

	SharedRandomIndex=0;
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("my_rnd")]
public int		SharedRandom(int r)
	{
	SharedRandomIndex++;
	SharedRandomIndex%=4096;

	return(SharedRandomTable[SharedRandomIndex]%r);
	}

//============================================================================
//
//----------------------------------------------------------------------------

[Original("rnd")]
public int		Random(int x)
	{

	RandomCount++;

	return(rand()%(x));
	}

//============================================================================
// スプライト基礎データ
//----------------------------------------------------------------------------
[Original("set_sprt_data")]
public void		InitializeSprites()
	{

	// タイトル

	Sprites[TTL_BACK].wd=699;
	Sprites[TTL_BACK].ht=384;
	Sprites[TTL_BACK].base_x=0;
	Sprites[TTL_BACK].base_y=3940;
	// ユニット
	Sprites[UNIT_JPN].wd=80;
	Sprites[UNIT_JPN].ht=80;
	Sprites[UNIT_JPN].base_x=0;
	Sprites[UNIT_JPN].base_y=1759;
	Sprites[UNIT_JPN].os_of_x=8;
	Sprites[UNIT_JPN].cx=40;
	Sprites[UNIT_JPN].cy=40;

	Sprites[UNIT_USA].wd=80;
	Sprites[UNIT_USA].ht=80;
	Sprites[UNIT_USA].base_x=0;
	Sprites[UNIT_USA].base_y=4550;
	Sprites[UNIT_USA].os_of_x=8;
	Sprites[UNIT_USA].cx=40;
	Sprites[UNIT_USA].cy=40;

	// サブユニット
	Sprites[SUB_UNIT].wd=40;
	Sprites[SUB_UNIT].ht=40;
	Sprites[SUB_UNIT].base_x=0;
	Sprites[SUB_UNIT].base_y=3520;
	Sprites[SUB_UNIT].os_of_x=12;
	Sprites[SUB_UNIT].cx=20;
	Sprites[SUB_UNIT].cy=20;

	// マップチップ 雲
	Sprites[MAP_TIP_NRML].wd=80;
	Sprites[MAP_TIP_NRML].ht=80;
	Sprites[MAP_TIP_NRML].base_x=0;
	Sprites[MAP_TIP_NRML].base_y=3000;
	Sprites[MAP_TIP_NRML].os_of_x=6;
	Sprites[MAP_TIP_NRML].cx=40;
	Sprites[MAP_TIP_NRML].cy=40;

	// ユニットインフォ
	Sprites[UNIT_INFO_JPN].x=CMBT_WIDTH;
	Sprites[UNIT_INFO_JPN].y=0;
	Sprites[UNIT_INFO_JPN].wd=120;
	Sprites[UNIT_INFO_JPN].ht=438;
	Sprites[UNIT_INFO_JPN].base_x=0;
	Sprites[UNIT_INFO_JPN].base_y=0;
	Sprites[UNIT_INFO_JPN].os_of_x=6;
	Sprites[UNIT_INFO_JPN].cx=0;
	Sprites[UNIT_INFO_JPN].cy=0;

	Sprites[UNIT_INFO_USA].wd=120;
	Sprites[UNIT_INFO_USA].ht=438;
	Sprites[UNIT_INFO_USA].base_x=0;
	Sprites[UNIT_INFO_USA].base_y=878;
	Sprites[UNIT_INFO_USA].os_of_x=6;
	Sprites[UNIT_INFO_USA].cx=0;
	Sprites[UNIT_INFO_USA].cy=0;

	// 操作ボタンベース
	Sprites[BTN_BASE].wd=198;
	Sprites[BTN_BASE].ht=120;
	Sprites[BTN_BASE].base_x=361;
	Sprites[BTN_BASE].base_y=3250;
	Sprites[BTN_BASE].os_of_x=1;
	Sprites[BTN_BASE].cx=0;
	Sprites[BTN_BASE].cy=0;

	// 操作ボタンベース
	Sprites[BTN_1].wd=140;
	Sprites[BTN_1].ht=20;
	Sprites[BTN_1].base_x=0;
	Sprites[BTN_1].base_y=3250;
	Sprites[BTN_1].os_of_x=1;
	Sprites[BTN_1].cx=0;
	Sprites[BTN_1].cy=0;

	// マップベース
	Sprites[MAP_BASE].wd=256-1;
	Sprites[MAP_BASE].ht=200-1;
	Sprites[MAP_BASE].base_x=0;
	Sprites[MAP_BASE].base_y=4340;
	Sprites[MAP_BASE].os_of_x=1;
	Sprites[MAP_BASE].cx=0;
	Sprites[MAP_BASE].cy=0;

	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cloud_in_start")]
public void	InitializeClouds()
	{
	int	m,n; Array16<int> ok = default;
	double	base_x,base_y,sub_x;

	while(true)
		{
		// 空いてるくもスプライトを探します。
		n=0;
		for(m=0; m<KUMO_MAX; m++)
			{
			if( Clouds[m].Used==0 )
				{
				ok[n]=m;
				n++;
				}
			if(n>=16)
				break;
			}

		if( n<=14 )
			return;

		base_x=(double)(Random(abs(MAP_RIGHT)+abs(MAP_LEFT))-abs(MAP_LEFT));
		base_y=(double)(Random(abs(MAP_TOP)+abs(MAP_BOTTOM))-abs(MAP_BOTTOM));

		sub_x=(double)( Random(Sprites[MAP_TIP_NRML].wd*4)-Sprites[MAP_TIP_NRML].wd*2 );

		n=0;
		for( m=0;m<3;m++)
			{
			Clouds[ok[n]].Used=1;
			Clouds[ok[n]].Position = new WorldPosition(base_x+m*80+sub_x, base_y);
			Clouds[ok[n]].Kind=1;
			n++;
			}
		sub_x=(double)( Random(Sprites[MAP_TIP_NRML].wd*2)-Sprites[MAP_TIP_NRML].wd*2 );
		for( m=0;m<5;m++)
			{
			Clouds[ok[n]].Used=1;
			Clouds[ok[n]].Position = new WorldPosition(base_x+m*80-80+sub_x, base_y+80);
			Clouds[ok[n]].Kind=1;
			n++;
			}
		sub_x=(double)( Random(Sprites[MAP_TIP_NRML].wd*2)-Sprites[MAP_TIP_NRML].wd*2 );
		for( m=0;m<3;m++)
			{
			Clouds[ok[n]].Used=1;
			Clouds[ok[n]].Position = new WorldPosition(base_x+m*80+sub_x, base_y+80*2);
			Clouds[ok[n]].Kind=1;
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
	int	m,n; Array16<int> ok = default;
	double	base_y,sub_x;

	for(n=0;n<KUMO_MAX;n++)
		{
		ref var cloud = ref Clouds[n];
		if( cloud.Used!=0 )
			{
			if( Result==GameResult.None )
				{
				cloud.Position = new WorldPosition(cloud.Position.X - 2.0, cloud.Position.Y);
				if( cloud.Position.X < MAP_LEFT )
					cloud.Used=0;
				}
			}
		}

	// 空いてるくもスプライトを探します。
	n=0;
	for(m=0; m<KUMO_MAX; m++)
		{
		if( Clouds[m].Used==0 )
			{
			ok[n]=m;
			n++;
			}
		if(n>=16)
			break;
		}

	if( n<=14 )
		return;

	base_y=(double)(Random(abs(MAP_TOP)+abs(MAP_BOTTOM))-abs(MAP_BOTTOM));
	sub_x=(double)( Random(Sprites[MAP_TIP_NRML].wd*4)-Sprites[MAP_TIP_NRML].wd*2 );
	n=0;
	for( m=0;m<3;m++)
		{
		Clouds[ok[n]].Used=1;
		Clouds[ok[n]].Position = new WorldPosition(MAP_RIGHT+m*80+sub_x, base_y);
		Clouds[ok[n]].Kind=1;
		n++;
		}
	sub_x=(double)( Random(Sprites[MAP_TIP_NRML].wd*2)-Sprites[MAP_TIP_NRML].wd*2 );
	for( m=0;m<5;m++)
		{
		Clouds[ok[n]].Used=1;
		Clouds[ok[n]].Position = new WorldPosition(MAP_RIGHT+m*80-80+sub_x, base_y+80);
		Clouds[ok[n]].Kind=1;
		n++;
		}
	sub_x=(double)( Random(Sprites[MAP_TIP_NRML].wd*2)-Sprites[MAP_TIP_NRML].wd*2 );
	for( m=0;m<3;m++)
		{
		Clouds[ok[n]].Used=1;
		Clouds[ok[n]].Position = new WorldPosition(MAP_RIGHT+m*80+sub_x, base_y+80*2);
		Clouds[ok[n]].Kind=1;
		n++;
		}

	}

private void UpdateCargo(ref Fire fire, ref int n)
	{
	double wrk_x;
	double wrk_y;
	int h;
	RECT wrk_r;
	int f;
	int cm_scrn_x;
	int cm_scrn_y;
	if( fire.Ticks<=fire.info[1] )
		{
		wrk_x=fire.Position.X;
		wrk_y=fire.Position.Y;
		fire.Position += new WorldVector(cos(fire.Direction*a_PI)*fire.Speed, sin(fire.Direction*a_PI)*fire.Speed);
		fire.Speed+=fire.Acceleration;

		h=0;

		// ptin dbg
		wrk_r.top=(int)fire.TargetY+20;
		wrk_r.right=(int)fire.TargetX+20;
		wrk_r.bottom=(int)fire.TargetY-20;
		wrk_r.left=(int)fire.TargetX-20;
		if( PointInRect3(ref wrk_r,(int)fire.Position.X,(int)fire.Position.Y)!=0 )
			h=1;

		if( h!=0 )
			{
			h=1;
			// 上陸地点到達
			for(n=1;n<=MaxUnitId && h!=0 ;n++)
				{
				if( Units[n].IsUsed && Units[n].Kind>=UnitKind.AirBase && Units[n].Kind<=UnitKind.Fortress )
					{
					// ptin dbg
					wrk_r.top=(int)Units[n].Position.Y+20;
					wrk_r.right=(int)Units[n].Position.X+20;
					wrk_r.bottom=(int)Units[n].Position.Y-20;
					wrk_r.left=(int)Units[n].Position.X-20;
					if( PointInRect3(ref wrk_r,(int)fire.Position.X,(int)fire.Position.Y)!=0 )
						{
						// とにかくほかの地上施設の上
						h=0;
						}
					}
				}

			if(h!=0)
				{
				switch(fire.Kind)
					{
					case FireKind.CargoInfantryBase:
						f=AddUnit((Side)fire.ShooterSide, UnitKind.InfantryBase,(double)fire.TargetX,(double)fire.TargetY,0);
						Units[f].BuildTime=0;												// 建設期間
						break;
					case FireKind.CargoPillboxes:
						f=AddUnit((Side)fire.ShooterSide, UnitKind.Pillboxes,(double)fire.TargetX,(double)fire.TargetY,0);
						Units[f].BuildTime=(int)(8000*( fire.ShooterSide==(int)Side.UnitedStates ? 1.0 : 0.8 ));		// 建設期間
						Units[f].Hp/=4;
						Units[f].MaxHp/=4;
						break;
					case FireKind.CargoFortress:
						f=AddUnit((Side)fire.ShooterSide, UnitKind.Fortress,(double)fire.TargetX,(double)fire.TargetY,0);
						Units[f].BuildTime=(int)(15000*( fire.ShooterSide==(int)Side.UnitedStates ? 0.8 : 1.0 ));		// 建設期間
						Units[f].Hp/=4;
						Units[f].MaxHp/=4;
						break;
					case FireKind.CargoAirBase:
						f=AddUnit((Side)fire.ShooterSide, UnitKind.AirBase,(double)fire.TargetX,(double)fire.TargetY,0);
						Units[f].BuildTime=(int)(10000*( fire.ShooterSide==(int)Side.UnitedStates ? 0.8 : 1.0 ));		// 建設期間
						Units[f].Hp/=4;
						Units[f].MaxHp/=4;
						break;
					case FireKind.CargoNavalBase:
						f=AddUnit((Side)fire.ShooterSide, UnitKind.NavalBase,(double)fire.TargetX,(double)fire.TargetY,0);
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

			if( fire.Ticks>=fire.ArmingTime  )
				{
				if(!( fire.Position.Y>MAP_TOP || fire.Position.Y<MAP_BOTTOM || fire.Position.X<MAP_LEFT || fire.Position.X>MAP_RIGHT ))
					{
					cm_scrn_x=(int)((fire.Position.X+(Sprites[UNIT_JPN].wd/2)-MAP_LEFT)/Sprites[MAP_TIP_NRML].wd);
					cm_scrn_y=(int)((MAP_TOP-fire.Position.Y+(Sprites[UNIT_JPN].ht/2))/Sprites[MAP_TIP_NRML].ht);

					if( MapTiles[cm_scrn_y][cm_scrn_x]==0)
						{
						// 海の上
						if( (Tick%5)==0)
							{
							f=FindFreeEffect();
							Effects[f].Layer=EffectLayer.Lower;
							Effects[f].TimeLeft=30+SharedRandom(25);
							Effects[f].Animation=4;

							Effects[f].Position = new WorldPosition(wrk_x, wrk_y);

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
	double wrk_x;
	double wrk_y;
	RECT wrk_rect;
	int f;
	if( fire.Speed>=fire.FinalSpeed )
		{
		wrk_x=fire.Position.X;
		wrk_y=fire.Position.Y;
		fire.Position += new WorldVector(cos(fire.Direction*a_PI)*fire.Speed, sin(fire.Direction*a_PI)*fire.Speed);
		fire.Speed+=fire.Acceleration;

		n=fire.Target;						// ターゲットナンバー

		// ptin dbg
		wrk_rect.top=(int)Units[n].Position.Y+10;
		wrk_rect.right=(int)Units[n].Position.X+10;
		wrk_rect.bottom=(int)Units[n].Position.Y-10;
		wrk_rect.left=(int)Units[n].Position.X-10;

		if( PointInRect3(ref wrk_rect,(int)fire.Position.X,(int)fire.Position.Y)!=0 )
			{
			// 命中
			fire.Target=0;
			if(ShowsAntiAir==0)
				{
				if( Units[n].Kind!=UnitKind.Transport || Random(4)==0 )
					Units[n].Hp-=GetDamagePoints(m);
				}

			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Upper;

			Effects[f].TimeLeft=20;
			Effects[f].Animation=1;	// アニメーションパターン

			Effects[f].Position=fire.Position;
			Effects[f].SpriteNumber=1;			// 弾丸着弾	のソースファイル上の番号
			}
		else
			{
			if( 1!=0 )
				{
				// 弾丸描画
				f=FindFreeEffect();
				Effects[f].Layer=EffectLayer.Upper;
				Effects[f].TimeLeft=1;	Effects[f].Animation=11;
				Effects[f].Position=fire.Position;
				Effects[f].EndPosition = new WorldPosition(wrk_x, wrk_y);
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
	double wrk_x;
	double wrk_y;
	int f;
	RECT wrk_rect;
	fire.Ticks--;
	if( fire.Ticks!=0 )
		{	//
		wrk_x=fire.Position.X;	wrk_y=fire.Position.Y;
		fire.Position += new WorldVector(cos(fire.Direction*a_PI)*fire.Speed, sin(fire.Direction*a_PI)*fire.Speed);

		fire.Speed+=fire.Acceleration;		// 弾が減速

		if( 1!=0 )
			{
			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Upper;
			Effects[f].TimeLeft=1;	Effects[f].Animation=10;
			Effects[f].Position=fire.Position;
			Effects[f].EndPosition = new WorldPosition(wrk_x, wrk_y);

			Effects[f].SpriteNumber=0;			// ソースファイル上の番号
			}

		}
	else
		{	// 炸裂！
		fire.Position += new WorldVector(cos(fire.Direction*a_PI)*fire.Speed, sin(fire.Direction*a_PI)*fire.Speed);

		// 砲弾炸裂
		f=FindFreeEffect();
		Effects[f].Layer=EffectLayer.Lower;

		Effects[f].TimeLeft=10;
		Effects[f].Animation=4;	// アニメーションパターン

		Effects[f].Position=fire.Position;
		Effects[f].SpriteNumber=0;			// 弾丸着弾	のソースファイル上の番号

		// ptin dbg
		wrk_rect.top=(int)fire.Position.Y+40;
		wrk_rect.right=(int)fire.Position.X+40;
		wrk_rect.bottom=(int)fire.Position.Y-40;
		wrk_rect.left=(int)fire.Position.X-40;

		for(n=1;n<=MaxUnitId;n++)
			{
			ref var unit = ref Units[n];
			if( unit.IsUsed && unit.Side==Units[fire.Target].Side && unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Flying )
				{
				if( PointInRect3(ref wrk_rect,(int)unit.Position.X,(int)unit.Position.Y)!=0  )
					{
					// 命中
					fire.Target=0;
					if(ShowsAntiAir==0  )
						{
						unit.Hp-=GetDamagePoints(m);
						if(unit.Speed<=unit.MaxSpeed )
							unit.Direction=Random(360);
						}

					f=FindFreeEffect();
					Effects[f].Layer=EffectLayer.Upper;

					Effects[f].TimeLeft=20;
					Effects[f].Animation=2;	// アニメーションパターン

					Effects[f].Position=unit.Position;
					Effects[f].SpriteNumber=1;			// 弾丸着弾	のソースファイル上の番号
					}
				}
			}
		fire.Target=0;
		}
	}

private void UpdateAntiSubmarineBomb(ref Fire fire, ref int n, int m)
	{
	int f;
	int h;
	fire.Ticks++;
	if( fire.Ticks==5 )
		{
		f=FindFreeEffect();
		Effects[f].Layer=EffectLayer.Lower;
		Effects[f].TimeLeft=30;
		Effects[f].Animation=4;

		Effects[f].Position=fire.Position;

		Effects[f].SpriteNumber=7;			// ソースファイル上の番号
		}
	if( fire.Ticks==fire.info[1] )
		{	// バクハツ！

		f=FindFreeEffect();

		PlaySoundEffect( 0, TPD_HIT1 ,fire.Position.X, fire.Position.Y);

		Effects[f].Layer=EffectLayer.Lower;

		Effects[f].TimeLeft=40;
		Effects[f].Animation=4;	// アニメーションパターン

		Effects[f].Position=fire.Position;
		Effects[f].SpriteNumber=11;			//ソースファイル上の番号

		h=0;
		for(n=1;n<=MaxUnitId;n++)
			{
			ref var unit = ref Units[n];
			if( unit.IsUsed && unit.Kind==UnitKind.Submarine && unit.Submerged!=0 )
				{
				fire.Target=n;
				h=CheckHit(m);
				if( h!=0 )
					break;
				}
			}

		if( h!=0 )
			{
			// 命中
			Units[n].Hp-=GetDamagePoints(m);
			}
		fire.Target=0;
		}
	}

private void UpdateAntiAircraftShell(ref Fire fire, ref int n, int m)
	{
	double wrk_x;
	double wrk_y;
	int f;
	RECT wrk_rect;
	fire.Ticks--;
	if( fire.Ticks!=0 )
		{	//
		wrk_x=fire.Position.X;	wrk_y=fire.Position.Y;
		fire.Position += new WorldVector(cos(fire.Direction*a_PI)*fire.Speed, sin(fire.Direction*a_PI)*fire.Speed);

		fire.Speed+=fire.Acceleration;		// 弾が減速
		if( 1!=0 )
			{
			// 弾自体の絵
			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Upper;
			Effects[f].TimeLeft=1;	Effects[f].Animation=0;
			Effects[f].Position=fire.Position;
			Effects[f].SpriteNumber=96+ToEightDirections((int)(fire.Direction));			// ソースファイル上の番号
			// 弾の煙
			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Upper;
			Effects[f].TimeLeft=3+SharedRandom(3);
			Effects[f].Animation=4;

			Effects[f].Position = new WorldPosition(wrk_x+SharedRandom(10)-5, wrk_y+SharedRandom(10)-5);

			Effects[f].SpriteNumber=2;			// ソースファイル上の番号
			}
		}
	else
		{	// 炸裂！
		fire.Position += new WorldVector(cos(fire.Direction*a_PI)*fire.Speed, sin(fire.Direction*a_PI)*fire.Speed);

		// 砲弾炸裂
		// 煙
		f=FindFreeEffect();
		Effects[f].Layer=EffectLayer.Lower;
		Effects[f].TimeLeft=80+Random(80);
		Effects[f].Animation=2;
		Effects[f].Position=fire.Position;
		Effects[f].SpriteNumber=6;			// ソースファイル上の番号

		// 漠炎
		f=FindFreeEffect();
		Effects[f].Layer=EffectLayer.Lower;
		Effects[f].TimeLeft=10;
		Effects[f].Animation=4;	// アニメーションパターン
		Effects[f].Position=fire.Position;
		Effects[f].SpriteNumber=0;			// 弾丸着弾	のソースファイル上の番号

		f=40;
		// ptin dbg
		wrk_rect.top=(int)fire.Position.Y+f;
		wrk_rect.right=(int)fire.Position.X+f;
		wrk_rect.bottom=(int)fire.Position.Y-f;
		wrk_rect.left=(int)fire.Position.X-f;

		for(n=1;n<=MaxUnitId;n++)
			{
			ref var unit = ref Units[n];
			if( unit.IsUsed && unit.Side==Units[fire.Target].Side && unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Flying )
				{
				if( PointInRect3(ref wrk_rect,(int)unit.Position.X,(int)unit.Position.Y)!=0 && !( unit.Kind==UnitKind.Bomber && unit.Side==Side.UnitedStates && Random(3)!=0 ) )
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
					Effects[f].Layer=EffectLayer.Upper;

					Effects[f].TimeLeft=20;
					Effects[f].Animation=2;	// アニメーションパターン

					Effects[f].Position=unit.Position;
					Effects[f].SpriteNumber=1;			// 弾丸着弾	のソースファイル上の番号
					}
				}
			}
		fire.Target=0;
		}
	}

private void UpdateGunShell(ref Fire fire, ref int n, int m)
	{
	double wrk_x;
	double wrk_y;
	int f;
	int h;
	int i;
	int cm_scrn_x;
	int cm_scrn_y;
	fire.Ticks--;
	if( fire.Ticks!=0 )
		{	//
		wrk_x=fire.Position.X;	wrk_y=fire.Position.Y;
		fire.Position += new WorldVector(cos(fire.Direction*a_PI)*fire.Speed, sin(fire.Direction*a_PI)*fire.Speed);
		if( fire.Ticks > fire.info[1] )
			fire.Speed-=fire.Acceleration;		// 弾が上昇中
		else
			fire.Speed+=(fire.Acceleration*2.83);		// 弾が降下中
		if( 1!=0 )
			{
			// 弾自体の絵
			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Upper;
			Effects[f].TimeLeft=1;	Effects[f].Animation=0;
			Effects[f].Position=fire.Position;
			Effects[f].SpriteNumber=108+ToEightDirections((int)(fire.Direction));			// ソースファイル上の番号
			// 弾の煙
			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Upper;
			Effects[f].TimeLeft=3+SharedRandom(3);
			Effects[f].Animation=4;

			Effects[f].Position = new WorldPosition(wrk_x+SharedRandom(10)-5, wrk_y+SharedRandom(10)-5);

			Effects[f].SpriteNumber=2;			// ソースファイル上の番号
			}
		}
	else
		{	// 着弾！
		fire.Position += new WorldVector(cos(fire.Direction*a_PI)*fire.Speed, sin(fire.Direction*a_PI)*fire.Speed);
		n=fire.Target;
		h=CheckHit(m);
		fire.Target=0;
		if( Units[n].IsUsed && h!=0)
			{
			// 命中
			Units[n].Hp-=GetDamagePoints(m);

			PlaySoundEffect( 0, TPD_HIT1 ,fire.Position.X, fire.Position.Y);

			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Upper;

			Effects[f].TimeLeft=40;
			Effects[f].Animation=4;	// アニメーションパターン

			Effects[f].Position=fire.Position;

			Effects[f].SpriteNumber=1;			// 弾丸着弾	のソースファイル上の番号

			// 当った的に収納機があれば破壊される場合もある
			if( Units[n].Kind==UnitKind.AirBase || Units[n].Kind==UnitKind.Carrier || Units[n].Kind==UnitKind.LightCarrier )
				{
				for(i=0;i<=MaxUnitId;i++)
					{
					ref var unit = ref Units[i];
					if( unit.IsUsed && unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked && unit.Carrier==n && Random(10)==0 )
						{
						unit.Side=0;
						Units[unit.Carrier].PlaneCount--;	// 現在格納数

						if( unit.info[3]>=1 && Units[unit.Carrier].PlanesToLaunch>=1 && unit.Mode<=UnitMode.Slow )
							Units[unit.Carrier].PlanesToLaunch--;		// 発艦予定の機数を
						if( unit.info[3]>=3 && Units[unit.Carrier].LandingLock>=1  && unit.Mode<=UnitMode.Slow )
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
			PlaySoundEffect( 0, SPL1 ,fire.Position.X, fire.Position.Y);

			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Lower;

			Effects[f].TimeLeft=40;
			Effects[f].Animation=4;	// アニメーションパターン

			Effects[f].Position=fire.Position;

			wrk_x=fire.Position.X;
			wrk_y=fire.Position.Y;

			Effects[f].SpriteNumber=8;			// 弾丸着弾	のソースファイル上の番号
			if(!( wrk_y>MAP_TOP || wrk_y<MAP_BOTTOM || wrk_x<MAP_LEFT || wrk_x>MAP_RIGHT ))
				{
				cm_scrn_x=(int)((wrk_x+(Sprites[UNIT_JPN].wd/2)-MAP_LEFT)/Sprites[MAP_TIP_NRML].wd);
				cm_scrn_y=(int)((MAP_TOP-wrk_y+(Sprites[UNIT_JPN].ht/2))/Sprites[MAP_TIP_NRML].ht);
				if( MapTiles[cm_scrn_y][cm_scrn_x]>=1)
					Effects[f].SpriteNumber=10;			// 弾丸着弾	のソースファイル上の番号
				else
					Effects[f].SpriteNumber=8;			// 弾丸着弾	のソースファイル上の番号
				}

			}
		}
	}

private void UpdateTorpedo(ref Fire fire, ref int n, int m)
	{
	double wrk_x;
	double wrk_y;
	int cm_scrn_x;
	int cm_scrn_y;
	int h;
	int f;
	if( fire.Ticks<=fire.info[1] )
		{
		wrk_x=fire.Position.X;
		wrk_y=fire.Position.Y;
		wrk_x+=cos(fire.Direction*a_PI)*-40;
		wrk_y+=sin(fire.Direction*a_PI)*-40;
		if(!( wrk_y>MAP_TOP || wrk_y<MAP_BOTTOM || wrk_x<MAP_LEFT || wrk_x>MAP_RIGHT ))
			{
			cm_scrn_x=(int)((wrk_x+(Sprites[UNIT_JPN].wd/2)-MAP_LEFT)/Sprites[MAP_TIP_NRML].wd);
			cm_scrn_y=(int)((MAP_TOP-wrk_y+(Sprites[UNIT_JPN].ht/2))/Sprites[MAP_TIP_NRML].ht);
			if( MapTiles[cm_scrn_y][cm_scrn_x]>=1)
				{
				fire.Ticks=fire.info[1];
				}
			}

		wrk_x=fire.Position.X;
		wrk_y=fire.Position.Y;
		fire.Position += new WorldVector(cos(fire.Direction*a_PI)*fire.Speed, sin(fire.Direction*a_PI)*fire.Speed);
		fire.Speed+=fire.Acceleration;

		h=0;
		if( fire.Ticks>=fire.ArmingTime )
			{
			for(n=1;n<=MaxUnitId;n++)
				{
				ref var unit = ref Units[n];
				if( unit.IsUsed && unit.Category==UnitCategory.Ship && unit.Kind>=UnitKind.Battleship && unit.Kind<=UnitKind.Transport && !(unit.Kind==UnitKind.Submarine && unit.Submerged!=0))
					{
					fire.Target=n;
					h=CheckHit(m);
					if( h!=0 )
						break;
					}
				}
			}

		if( h!=0 )
			{
			// 命中
			fire.Target=0;
			Units[n].Hp-=GetDamagePoints(m);

			PlaySoundEffect( 0, TPD_HIT1+Random(2) ,fire.Position.X, fire.Position.Y);

			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Lower;

			Effects[f].TimeLeft=40;
			Effects[f].Animation=4;	// アニメーションパターン

			Effects[f].Position=fire.Position;
			Effects[f].SpriteNumber=11;			// 弾丸着弾	のソースファイル上の番号
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

			if( fire.Ticks>=fire.ArmingTime  )
				{
				f=FindFreeEffect();
				Effects[f].Layer=EffectLayer.Lower;
				Effects[f].TimeLeft=1;
				Effects[f].Animation=0;

				Effects[f].Position=fire.Position;

				Effects[f].SpriteNumber=72+ToEightDirections((int)(fire.Direction));			// ソースファイル上の番号
				if( (Tick%3)==0)
					{
					f=FindFreeEffect();
					Effects[f].Layer=EffectLayer.Lower;
					Effects[f].TimeLeft=30+SharedRandom(25);
					Effects[f].Animation=4;

					Effects[f].Position = new WorldPosition(wrk_x+SharedRandom(10)-5, wrk_y+SharedRandom(10)-5);

					Effects[f].SpriteNumber=8;			// ソースファイル上の番号
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
	int f;
	int h;
	int i;
	double wrk_x;
	double wrk_y;
	int cm_scrn_x;
	int cm_scrn_y;
	fire.Ticks++;

	if( fire.Target==(int)UnitKind.Attacker && fire.Ticks==12 )
		PlaySoundEffect( 0, BOMB_OFF ,fire.Position.X, fire.Position.Y);

	if( fire.Ticks<=fire.info[1]  )
		{	// 爆弾降下中
		if( fire.Ticks>=50)
			{
			if( fire.Ticks==50)
			PlaySoundEffect( 0, FALL1 ,fire.Position.X, fire.Position.Y);

			fire.Position += new WorldVector(cos(fire.Direction*a_PI)*fire.Speed, sin(fire.Direction*a_PI)*fire.Speed);
			fire.Speed+=fire.Acceleration;
			if( 1!=0 )
				{
				f=FindFreeEffect();
				Effects[f].Layer=EffectLayer.Upper;
				Effects[f].TimeLeft=1;	Effects[f].Animation=0;
				Effects[f].Position=fire.Position;
				Effects[f].SpriteNumber=84+ToEightDirections((int)(fire.Direction));			// ソースファイル上の番号
				}
			}
		}
	else
		{	// 着弾！
		fire.Position += new WorldVector(cos(fire.Direction*a_PI)*fire.Speed, sin(fire.Direction*a_PI)*fire.Speed);
		h=0;
		for(n=1;n<=MaxUnitId;n++)
			{
			ref var unit = ref Units[n];
			if( unit.IsUsed && unit.Category==UnitCategory.Ship && !(unit.Kind==UnitKind.Submarine && unit.Submerged!=0))
				{
				fire.Target=n;
				h=CheckHit(m);
				if( h!=0 )
					break;
				}
			}
		fire.Target=0;
		if( h!=0 )
			{
			// 命中
			Units[n].Hp-=GetDamagePoints(m);

			PlaySoundEffect( 0, BOM_HIT1+Random(2) ,fire.Position.X, fire.Position.Y);

			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Upper;

			Effects[f].TimeLeft=40;
			Effects[f].Animation=4;	// アニメーションパターン

			Effects[f].Position=fire.Position;
			Effects[f].SpriteNumber=1;			// 弾丸着弾	のソースファイル上の番号

			// 当った的に収納機があれば破壊される場合もある
			if( Units[n].Kind==UnitKind.AirBase || Units[n].Kind==UnitKind.Carrier || Units[n].Kind==UnitKind.LightCarrier )
				{
				for(i=0;i<=MaxUnitId;i++)
					{
					ref var unit = ref Units[i];
					if( unit.IsUsed && unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked && unit.Carrier==n && Random(10)==0 )
						{
						unit.Side=0;
						Units[unit.Carrier].PlaneCount--;	// 現在格納数

						if( unit.info[3]>=1 && Units[unit.Carrier].PlanesToLaunch>=1 && unit.Mode<=UnitMode.Slow )
							Units[unit.Carrier].PlanesToLaunch--;		// 発艦予定の機数を
						if( unit.info[3]>=3 && Units[unit.Carrier].LandingLock>=1  && unit.Mode<=UnitMode.Slow )
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

			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Lower;

			Effects[f].TimeLeft=40;
			Effects[f].Animation=4;	// アニメーションパターン

			Effects[f].Position=fire.Position;
			wrk_x=fire.Position.X;
			wrk_y=fire.Position.Y;
			if(!( wrk_y>MAP_TOP || wrk_y<MAP_BOTTOM || wrk_x<MAP_LEFT || wrk_x>MAP_RIGHT ))
				{
				cm_scrn_x=(int)((wrk_x+(Sprites[UNIT_JPN].wd/2)-MAP_LEFT)/Sprites[MAP_TIP_NRML].wd);
				cm_scrn_y=(int)((MAP_TOP-wrk_y+(Sprites[UNIT_JPN].ht/2))/Sprites[MAP_TIP_NRML].ht);
				}
			else
				{
				cm_scrn_x=0;
				cm_scrn_y=0;
				}

			if( MapTiles[cm_scrn_y][cm_scrn_x]>=1)
				{
				Effects[f].SpriteNumber=10;			// 着弾	のソースファイル上の番号
				PlaySoundEffect( 0, BOM_HIT1 ,fire.Position.X, fire.Position.Y);
				}
			else
				{
				Effects[f].SpriteNumber=8;			// 着弾	のソースファイル上の番号
				PlaySoundEffect( 0, SPL1 ,fire.Position.X, fire.Position.Y);
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

	int		m,cl,n=default /* C4701 */;

	//=========		 ファイアの制御		=========//
	// 弾丸、爆弾等の機動、炸裂を制御します。
	for(m=1;m<FIRE_MAX;m++)
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
	int		n,min_no,f,i;

	// 部下がまだ生きてるか
	min_no=MaxUnitId+1;
	f=0;
	for( n=1; n<=MaxUnitId; n++)
		{
		ref var unit = ref Units[n];
		if( unit.IsUsed && unit.GroupLeader==m )
			{
			if( Units[min_no].FormationNumber>unit.FormationNumber || min_no==MaxUnitId+1 )
				min_no=n;

			f++;
			}
		}

	if( f>=2 )
		{

		// 部下が生存
		Units[min_no].FormationNumber=0;
		Units[min_no].IsGroupLeader=(short)f;
		Units[min_no].GroupLeader=0;

		if( Units[min_no].Kind==UnitKind.Fighter && (Units[m].Kind==UnitKind.Attacker || Units[m].Kind==UnitKind.Bomber) && Units[min_no].PlaneState==UnitState.Flying )
			Units[min_no].Mode=UnitMode.Return;		// それまでの隊長がボスだったらきかんしよっと

		for( n=1; n<=MaxUnitId; n++)
			{
			ref var unit = ref Units[n];
			if( unit.IsUsed && unit.GroupLeader==m )
				{
				unit.GroupLeader=(short)min_no;			// ｍｉｎ＿ｎｏが新しい隊長機

				if( unit.Kind==UnitKind.Fighter && (Units[m].Kind==UnitKind.Attacker || Units[m].Kind==UnitKind.Bomber) && unit.PlaneState==UnitState.Flying )
					unit.Mode=UnitMode.Return;		// それまでの隊長がボスだったらきかんしよっと

				}
			}
		// ｐｐ＿ｘ、ｙをコピーします。
		for(i=0;i<=63;i++)
			{
			Units[min_no].PathX[i]=Units[m].PathX[i];
			Units[min_no].PathY[i]=Units[m].PathY[i];
			}
		}

	else if( f==1 )
		{
		// 部下がひとつ
		Units[min_no].FormationNumber=0;
		Units[min_no].IsGroupLeader=0;
		Units[min_no].GroupLeader=0;
		}
	}

private void UpdatePlaneEffects(ref Unit unit, int m)
	{
	int f;
	double wrk_x2;
	double wrk_y2;
	int cm_scrn_x;
	int cm_scrn_y;
	int i;
	if(  unit.Ammo!=0 && !(unit.ReloadTime!=0 && (FrameCount%3)==0)&& (unit.Weapon==FireKind.Bomb || unit.Weapon==FireKind.Torpedo || unit.Weapon==FireKind.Maintenance || unit.Weapon==FireKind.Unarmed)  && unit.Side==LocalSide && !(unit.PlaneState==UnitState.Parked && UnitInfoPanel[1]==0) && !( unit.PlaneState==UnitState.Parked && unit.info[3]>=3 ) && !( unit.PlaneState==UnitState.Parked && unit.Carrier!=UnitInfoPanel[3]))
		{
		f=FindFreeEffect();
		Effects[f].Layer=EffectLayer.Lower;
		Effects[f].TimeLeft=1;
		if( unit.PlaneState==UnitState.Flying )
			{
			Effects[f].Animation=0;
			Effects[f].Position = new WorldPosition(unit.Position.X, unit.Position.Y-25);
			}
		else
			{
			Effects[f].Animation=3;
			Effects[f].Position = new WorldPosition(unit.Position.X, unit.Position.Y+25);
			}
		switch( unit.Weapon )
			{
			case FireKind.Torpedo:
				Effects[f].SpriteNumber=3;			// ソースファイル上の番号
				break;
			case FireKind.Bomb:
				Effects[f].SpriteNumber=4;			// ソースファイル上の番号
				break;
			case FireKind.Maintenance:	case FireKind.Unarmed:
				Effects[f].SpriteNumber=15;			// ソースファイル上の番号
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
			unit.Side=0;

			if( UnitInfoPanel[3]==m )
				UnitInfoPanel[0]=0;				// ユニットインフォをクリア

			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Lower;
			Effects[f].TimeLeft=80;

			Effects[f].Position=unit.Position;

			wrk_x2=unit.Position.X;
			wrk_y2=unit.Position.Y;
			if(!( wrk_y2>MAP_TOP || wrk_y2<MAP_BOTTOM || wrk_x2<MAP_LEFT || wrk_x2>MAP_RIGHT ))
				{
				cm_scrn_x=(int)((wrk_x2+(Sprites[UNIT_JPN].wd/2)-MAP_LEFT)/Sprites[MAP_TIP_NRML].wd);
				cm_scrn_y=(int)((MAP_TOP-wrk_y2+(Sprites[UNIT_JPN].ht/2))/Sprites[MAP_TIP_NRML].ht);
				}
			else
				{
				cm_scrn_x=0;
				cm_scrn_y=0;
				}

			if( MapTiles[cm_scrn_y][cm_scrn_x]>=1)
				{
				Effects[f].SpriteNumber=9;			// ソースファイル上の番号
				Effects[f].Animation=4;
				}
			else
				{
				Effects[f].SpriteNumber=7;			// ソースファイル上の番号
				Effects[f].Animation=0;
				}

			if( SelectedUnit==m )
				{
				SelectedUnit=0;
				CombatMenuKind=0; CombatMenuSelection=CombatMenuItem.None; ClearSelection2(1);
				}

			if( unit.IsGroupLeader!=0 )
				AssignGroupLeader(m);					// 爆砕されたのがＬＤＲなら、新しいのを決めます。

			}
		else if( unit.Hp<=unit.MaxHp*0.2 )
			{
			// ＨＰはあるが、事実上の墜落、
			if( unit.Hp==unit.MaxHp*0.2 )
				{
				// 飛行機が火を吹く
				if(unit.Kind==UnitKind.Bomber)
					{
					for(i=0;i<3;i++)
						{
						f=FindFreeEffect();
						if( f!=0 )
							{
							Effects[f].Layer=EffectLayer.Upper;
							Effects[f].TimeLeft=20+SharedRandom(20);
							Effects[f].Animation=4;
							Effects[f].Position = new WorldPosition(unit.Position.X+20-SharedRandom(40), unit.Position.Y+20-SharedRandom(40));
							Effects[f].SpriteNumber=9;				// ソースファイル上の番号
							}
						}
					}
				else
					{
					f=FindFreeEffect();
					Effects[f].Layer=EffectLayer.Upper;
					Effects[f].TimeLeft=20+SharedRandom(20);
					Effects[f].Animation=4;
					Effects[f].Position=unit.Position;
					Effects[f].SpriteNumber=9;				// ソースファイル上の番号
					}
				if(IsEditingMap==0)
					unit.Hp--;
				}
			else
				{
				if( Random(80)==0 )
					{
					if(IsEditingMap==0)
						unit.Hp--;
					}

				if( Random(5)!=0 && (Tick%(10))==0 )
					{
					f=FindFreeEffect();
					Effects[f].Layer=EffectLayer.Upper;
					Effects[f].TimeLeft=40+SharedRandom(15);
					Effects[f].Animation=2;
					Effects[f].Position=unit.Position;
					Effects[f].SpriteNumber=6;			// ソースファイル上の番号
					}

				if( Random(3)==0 && unit.Hp<=unit.MaxHp*0.1 )
					{
					// 小爆炎
					f=FindFreeEffect();
					Effects[f].Layer=EffectLayer.Upper;
					Effects[f].TimeLeft=8+SharedRandom(6);
					Effects[f].Animation=4;
					Effects[f].Position = new WorldPosition(unit.Position.X+SharedRandom(6)-3, unit.Position.Y+SharedRandom(6)-3);
					Effects[f].SpriteNumber=10;			// ソースファイル上の番号
					}
				}
			}
		else if( unit.Hp<=unit.MaxHp*0.3  )
			{
			if( Random(500)==0 )
				{
				if(IsEditingMap==0)
					unit.Hp--;
				}

			if( Random(3)!=0 && (Tick%(10) )==0 )
				{
				f=FindFreeEffect();
				Effects[f].Layer=EffectLayer.Upper;
				Effects[f].TimeLeft=40+SharedRandom(15);
				Effects[f].Animation=2;
				Effects[f].Position=unit.Position;
				Effects[f].SpriteNumber=6;			// ソースファイル上の番号

				if( Random(5)==0 )
					{
					// 小爆炎
					f=FindFreeEffect();
					Effects[f].Layer=EffectLayer.Upper;
					Effects[f].TimeLeft=8+SharedRandom(6);
					Effects[f].Animation=4;
					Effects[f].Position = new WorldPosition(unit.Position.X+SharedRandom(6)-3, unit.Position.Y+SharedRandom(6)-3);
					Effects[f].SpriteNumber=10;			// ソースファイル上の番号
					}
				}

			}
		else if( unit.Hp<=unit.MaxHp*0.5  )
			{
			if( Random(500)==0 )
				{
				if(IsEditingMap==0)
					unit.Hp--;
				}
			if( Random(2)==1 && (Tick%10)==0 )
				{
				f=FindFreeEffect();
				Effects[f].Layer=EffectLayer.Upper;
				Effects[f].TimeLeft=40+SharedRandom(15);
				Effects[f].Animation=2;

				Effects[f].Position=unit.Position;
				Effects[f].SpriteNumber=6;			// ソースファイル上の番号

				if( Random(7)==0 )
					{
					// 小爆炎
					f=FindFreeEffect();
					Effects[f].Layer=EffectLayer.Upper;
					Effects[f].TimeLeft=8+SharedRandom(6);
					Effects[f].Animation=4;
					Effects[f].Position = new WorldPosition(unit.Position.X+SharedRandom(6)-3, unit.Position.Y+SharedRandom(6)-3);
					Effects[f].SpriteNumber=10;			// ソースファイル上の番号
					}
				}

			}
		else if( unit.Hp<=unit.MaxHp*0.7  )
			{

			if( Random(8)==0 && (Tick%10)==0 )
				{
				f=FindFreeEffect();
				Effects[f].Layer=EffectLayer.Upper;
				Effects[f].TimeLeft=40+SharedRandom(15);
				Effects[f].Animation=2;

				Effects[f].Position=unit.Position;
				Effects[f].SpriteNumber=6;			// ソースファイル上の番号

				}
			}
		}
	}

private void UpdateShipEffects(ref Unit unit, int m)
	{
	int f;
	int n;
	int i;
	double drctn;
	double dstc;
	if(  unit.Supply!=0 && (FrameCount%2)!=0 && unit.Side==LocalSide )
		{
		f=FindFreeEffect();
		Effects[f].Layer=EffectLayer.Lower;
		Effects[f].TimeLeft=1;

		Effects[f].Animation=0;
		Effects[f].Position = new WorldPosition(unit.Position.X, unit.Position.Y-25.0);

		Effects[f].SpriteNumber=15;			// ソースファイル上の番号
		}

	// 武装の表示
// 弾薬の消費サイズ

	if( ( ( unit.Side==Side.Japan && unit.Kind==UnitKind.Battleship ) || ( unit.Side==Side.UnitedStates && unit.Kind==UnitKind.Carrier ) ) && unit.Variant==1 && unit.Side==LocalSide )
		{
		// 大和級とエセックス
		f=FindFreeEffect();
		Effects[f].Layer=EffectLayer.Lower;
		Effects[f].TimeLeft=1;

		Effects[f].Animation=0;
		Effects[f].Position = new WorldPosition(unit.Position.X+18, unit.Position.Y+25.0);

		Effects[f].SpriteNumber=27;			// ソースファイル上の番号
		}
	else if( unit.Kind==UnitKind.Destroyer && unit.Variant==1  && unit.Side==LocalSide )
		{
		// 対潜駆逐艦
		f=FindFreeEffect();
		Effects[f].Layer=EffectLayer.Lower;
		Effects[f].TimeLeft=1;

		Effects[f].Animation=0;
		Effects[f].Position = new WorldPosition(unit.Position.X+18, unit.Position.Y+25.0);

		Effects[f].SpriteNumber=26;			// ソースファイル上の番号
		}
	else if( unit.Kind==UnitKind.Cruiser && unit.Variant==1  && unit.Side==LocalSide )
		{
		// 防空巡洋艦
		f=FindFreeEffect();
		Effects[f].Layer=EffectLayer.Lower;
		Effects[f].TimeLeft=1;

		Effects[f].Animation=0;
		Effects[f].Position = new WorldPosition(unit.Position.X+18, unit.Position.Y+25.0);

		Effects[f].SpriteNumber=16;			// ソースファイル上の番号
		}
	else if( ( unit.Side==Side.Japan && ( unit.Kind==UnitKind.Submarine || unit.Kind==UnitKind.Destroyer || unit.Kind==UnitKind.Cruiser ) || unit.Side==Side.UnitedStates && ( unit.Kind==UnitKind.Submarine || unit.Kind==UnitKind.Destroyer ) ) && unit.Supply==0 && unit.Ammo!=0 && !(unit.ReloadTime!=0 && (FrameCount%2)!=0) && unit.Ammo>=1 && unit.Side==LocalSide )
		{
		f=FindFreeEffect();
		Effects[f].Layer=EffectLayer.Lower;
		Effects[f].TimeLeft=1;

		Effects[f].Animation=0;
		Effects[f].Position = new WorldPosition(unit.Position.X, unit.Position.Y-25.0);

		Effects[f].SpriteNumber=3;			// ソースファイル上の番号
		}

	// トランスポーターの荷物の表示
	if(  unit.Kind==UnitKind.Transport && unit.Supply==0 && unit.Ammo!=0 && !(unit.ReloadTime!=0 && (FrameCount%2)!=0) && unit.Side==LocalSide )
		{
		f=FindFreeEffect();
		Effects[f].Layer=EffectLayer.Lower;
		Effects[f].TimeLeft=1;

		Effects[f].Animation=0;
		Effects[f].Position = new WorldPosition(unit.Position.X, unit.Position.Y-25.0);

				Effects[f].SpriteNumber=44;			// ソースファイル上の番号
		}

//TR_GF1

	// およその敵潜航潜水艦
	if(  unit.Side!=LocalSide && unit.Kind==UnitKind.Submarine && unit.Submerged!=0 && unit.ContactTime!=0 )
		{
		f=FindFreeEffect();
		Effects[f].Layer=EffectLayer.Upper;
		Effects[f].TimeLeft=1;
		Effects[f].Animation=0;
		Effects[f].Position = new WorldPosition(unit.ContactX, unit.ContactY);

		Effects[f].SpriteNumber=60;			// ソースファイル上の番号

		f=FindFreeEffect();
		Effects[f].Layer=EffectLayer.Lower;
		Effects[f].TimeLeft=1;
		Effects[f].Animation=12;
		Effects[f].Position = new WorldPosition(unit.ContactX, unit.ContactY);
		Effects[f].EndPosition = new WorldPosition(unit.ContactRadius, unit.ContactRadius);
		}

	if( unit.Kind==UnitKind.Submarine && unit.Submerged!=0 )
		{	// 潜航中潜水艦
		if( unit.Hp<=0 )
			{	// 沈没
			unit.Side=0;
			unit.Found=0;

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
					if(IsEditingMap==0)
						unit.Hp--;
					}
				if( Random(300)==1  )
					{
					for(n=0;n<=3;n++)
						{
						f=FindFreeEffect();
						if( f!=0 )
							{
							Effects[f].Layer=EffectLayer.Lower;
							Effects[f].TimeLeft=100+SharedRandom(20);
							Effects[f].Animation=4;
							Effects[f].Position = new WorldPosition(unit.Position.X+SharedRandom(40)-20, unit.Position.Y+SharedRandom(40)-20);
							Effects[f].SpriteNumber=8;			// ソースファイル上の番号
							}
						}
					// ついでに発見される
					unit.ContactX=(int)unit.Position.X;
					unit.ContactY=(int)unit.Position.Y;

					unit.ContactRadius=100;
					unit.ContactTime=400;

					unit.Found=1;
					}
				}
			else
				{
				if( unit.Hp<=unit.MaxHp*0.5 )
					{	// 空気漏れ
					if( Random(1000)==0 )
						{
						if(IsEditingMap==0)
							unit.Hp--;
						}
					if( Random(600)==1  )
						{
						for(n=0;n<=2;n++)
							{
							f=FindFreeEffect();
							if( f!=0)
								{
								Effects[f].Layer=EffectLayer.Lower;
								Effects[f].TimeLeft=100+SharedRandom(20);
								Effects[f].Animation=4;
								Effects[f].Position = new WorldPosition(unit.Position.X+SharedRandom(40)-20, unit.Position.Y+SharedRandom(40)-20);
								Effects[f].SpriteNumber=8;			// ソースファイル上の番号
								}
							}

						// ついでに発見される
						unit.ContactX=(int)unit.Position.X;
						unit.ContactY=(int)unit.Position.Y;

						unit.ContactRadius=100;
						unit.ContactTime=400;

						unit.Found=1;
						}
					}
				}
			}
		}
	else
		{	// 水上艦船
		if( unit.Hp<=0 )
			{	// 沈没

			PlaySoundEffect( 0, SHIP_SINK1 ,unit.Position.X, unit.Position.Y);

			unit.Side=0;

			if( UnitInfoPanel[3]==m )
				UnitInfoPanel[0]=0;

			// 空母なら艦載機とユニットインフォを
			if( unit.Kind==UnitKind.Carrier || unit.Kind==UnitKind.LightCarrier || unit.Kind==UnitKind.AirBase )
				{
				for( i=1;i<=MaxUnitId;i++)
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
				if( Random(4000)==0 && unit.Supply==0 )
					{
					if(IsEditingMap==0)
						unit.Hp--;
					}
				if( Random(2)!=0  )
					{
					f=FindFreeEffect();
					Effects[f].Layer=EffectLayer.Upper;
					Effects[f].TimeLeft=1;
					Effects[f].Animation=4;

					Effects[f].Position=unit.Position;
					Effects[f].SpriteNumber=9;			// ソースファイル上の番号
					}
				}
			else
				{
				if( unit.Hp<=unit.MaxHp*0.5  )
					{
					n=Random(4);
					if(  n==0 )
						{
						f=FindFreeEffect();
						Effects[f].Layer=EffectLayer.Upper;
						Effects[f].TimeLeft=1;	Effects[f].Animation=4;
						Effects[f].Position=unit.Position;
						Effects[f].SpriteNumber=9;			// ソースファイル上の番号
						}
					if(  n==1 )
						{
						f=FindFreeEffect();
						Effects[f].Layer=EffectLayer.Upper;
						Effects[f].TimeLeft=1;	Effects[f].Animation=4;
						Effects[f].Position=unit.Position;
						Effects[f].SpriteNumber=10;			// ソースファイル上の番号
						}
					}
				else
					{
					if( Random(10)==1  && unit.Hp<=unit.MaxHp*0.7 )
						{
						f=FindFreeEffect();
						Effects[f].Layer=EffectLayer.Upper;
						Effects[f].TimeLeft=1;	Effects[f].Animation=4;
						Effects[f].Position=unit.Position;
						Effects[f].SpriteNumber=10;			// ソースファイル上の番号
						}
					}
				}
			}
		}

	// 航跡のエフェクト
	if( !(unit.Kind==UnitKind.Submarine||unit.Kind==UnitKind.NavalBase||unit.Kind==UnitKind.AirBase||unit.Kind==UnitKind.City||unit.Kind==UnitKind.Mine||unit.Kind==UnitKind.InfantryBase||unit.Kind==UnitKind.Pillboxes||unit.Kind==UnitKind.Fortress) && (Tick%15)==0 && unit.Speed>=unit.MaxSpeed/3 )
		{
		// 航跡のエフェクトを残す
		f=FindFreeEffect();
		Effects[f].Layer=EffectLayer.Lower;
		Effects[f].TimeLeft=60+SharedRandom(60);
		Effects[f].Animation=4;
		Effects[f].Position=unit.Position;

		drctn=unit.Direction;
		drctn+=180;
		drctn=(int)drctn%360;

		switch(unit.Kind)
			{
			case UnitKind.Battleship:	case UnitKind.Carrier:
				dstc=26;
				break;

			case UnitKind.Cruiser:	case UnitKind.LightCarrier:
				dstc=22;
				break;

			default:
				dstc=16;
				break;
			}

		Effects[f].Position += new WorldVector(cos(drctn*a_PI)*dstc, sin(drctn*a_PI)*dstc);

		Effects[f].SpriteNumber=8;			// ソースファイル上の番号
		}
	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("cont_unit_effect")]
public void	UpdateUnitEffects(int m)
	{
	ref var unit = ref Units[m];
	RECT	wrk_rect;
	int		f;

	if( unit.IsGroupLeader!=0 && unit.Side==LocalSide && unit.PlaneState!=UnitState.Parked  )
		{
		f=FindFreeEffect();
		Effects[f].Layer=EffectLayer.Upper;
		Effects[f].TimeLeft=1;
		Effects[f].Animation=0;
		Effects[f].Position = new WorldPosition(unit.Position.X, Effects[f].Position.Y);
		if( unit.Kind==UnitKind.Attacker || unit.Kind==UnitKind.Fighter || unit.Kind==UnitKind.Destroyer || unit.Kind==UnitKind.Submarine)
			Effects[f].Position = new WorldPosition(Effects[f].Position.X, unit.Position.Y+30.0);
		else
			Effects[f].Position = new WorldPosition(Effects[f].Position.X, unit.Position.Y+35.0);

		// 編隊長の旗
		Effects[f].SpriteNumber=24;			// ソースファイル上の番号
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
			f=FindFreeEffect();
			Effects[f].Layer=EffectLayer.Upper;
			Effects[f].TimeLeft=1;

			Effects[f].Animation=0;
			Effects[f].Position = new WorldPosition(unit.Position.X, unit.Position.Y-25.0);

			Effects[f].SpriteNumber=15;			// ソースファイル上の番号
			}
		}

	}

//============================================================================
//
//----------------------------------------------------------------------------
[Original("edit_now")]
public void	UpdateMapEditor()
	{
    Array128<byte> ach = default;
    int len;
	HDC					hdc;
	short	i,jp_plane,jp_ship,us_plane,us_ship;

	if ( IDirectDrawSurface_GetDC(lpDDSBack, &hdc) == DD_OK )
		{

		// draw stats, like frame number and frame rate
		SetBkMode(hdc, TRANSPARENT);
		SelectObject(hdc, gameFont_1);
		SetTextColor(hdc, RGB(255, 255, 0));

		jp_plane=0;
		jp_ship=0;
		us_plane=0;
		us_ship=0;

		for(i=1;i<=MaxUnitId;i++)
			{
			ref var unit = ref Units[i];
			if( unit.IsUsed )
				{
				if( unit.Side==Side.Japan)
					{
					if( unit.Category==UnitCategory.Plane )
						jp_plane++;
					else
						jp_ship++;
					}
				else
					{
					if( unit.Category==UnitCategory.Plane )
						us_plane++;
					else
						us_ship++;
					}
				}
			}

#if !LNGG_VER
		len = wsprintf(ach, "日本海軍　：　航空機　%d／%d　艦船又は地上施設　%d／%d　",jp_plane,JPN_PLANE_END-JPN_PLANE_START+1,jp_ship,JPN_SHIP_END-JPN_SHIP_START+1 );
		TextOut(hdc, 0, 530, ach, len);
		len = wsprintf(ach, "合衆国海軍：　航空機　%d／%d　艦船又は地上施設　%d／%d　",us_plane,USA_PLANE_END-USA_PLANE_START+1,us_ship,USA_SHIP_END-USA_SHIP_START+1 );
		TextOut(hdc, 0, 550, ach, len);

		// 使用説明
		len = wsprintf(ach, "F1:SAVE　F2=LOAD　F3:マップ再描画 F5:ユニット選択-> F6:ユニット選択<-");
		TextOut(hdc, 0, 580, ach, len);
		len = wsprintf(ach, "F7:ユニット消去 F8:ユニット配置 F9:サイドチェンジ F11:増援ポイント変更");
		TextOut(hdc, 0, 600, ach, len);
		len = wsprintf(ach, "1:ユニットの回転 2:航空機の武装／艦船の停泊 3:損傷回復 4:損傷 5:増燃料 6:減燃料 7:増残弾 8:減残弾");
		TextOut(hdc, 0, 620, ach, len);
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
		switch(EditorTarget)
			{
			case 1:
				len= wsprintf(ach, "戦艦",10);
				EditorKind=(byte)UnitKind.Battleship;
				EditorVariant=0;
				break;
			case 2:
				len= wsprintf(ach, "巡洋艦",10);
				EditorKind=(byte)UnitKind.Cruiser;
				EditorVariant=0;
				break;
			case 3:
				len= wsprintf(ach, "駆逐艦",10);
				EditorKind=(byte)UnitKind.Destroyer;
				EditorVariant=0;
				break;
			case 4:
				len= wsprintf(ach, "潜水艦",10);
				EditorKind=(byte)UnitKind.Submarine;
				EditorVariant=0;
				break;
			case 5:
				len= wsprintf(ach, "正規空母",10);
				EditorKind=(byte)UnitKind.Carrier;
				EditorVariant=0;
				break;
			case 6:
				len= wsprintf(ach, "軽空母",10);
				EditorKind=(byte)UnitKind.LightCarrier;
				EditorVariant=0;
				break;
			case 7:
				len = wsprintf(ach, "輸送船(歩兵基地)",10);
				EditorKind=(byte)UnitKind.Transport;
				EditorVariant=(byte)FireKind.CargoInfantryBase;
				break;
			case 8:
				len = wsprintf(ach, "輸送船(トーチカ群)",10);
				EditorKind=(byte)UnitKind.Transport;
				EditorVariant=(byte)FireKind.CargoPillboxes;
				break;
			case 9:
				len = wsprintf(ach, "輸送船(要塞)",10);
				EditorKind=(byte)UnitKind.Transport;
				EditorVariant=(byte)FireKind.CargoFortress;
				break;
			case 10:
				len = wsprintf(ach, "輸送船(航空基地)",10);
				EditorKind=(byte)UnitKind.Transport;
				EditorVariant=(byte)FireKind.CargoAirBase;
				break;
			case 11:
				len = wsprintf(ach, "輸送船(軍港)",10);
				EditorKind=(byte)UnitKind.Transport;
				EditorVariant=(byte)FireKind.CargoNavalBase;
				break;

			case 12:
				len= wsprintf(ach, "軍港",10);
				EditorKind=(byte)UnitKind.NavalBase;
				EditorVariant=0;
				break;
			case 13:
				len= wsprintf(ach, "航空基地",10);
				EditorKind=(byte)UnitKind.AirBase;
				EditorVariant=0;
				break;
			case 14:
				len= wsprintf(ach, "都市",10);
				EditorKind=(byte)UnitKind.City;
				EditorVariant=0;
				break;
			case 15:
				len= wsprintf(ach, "歩兵基地",10);
				EditorKind=(byte)UnitKind.InfantryBase;
				EditorVariant=0;
				break;
			case 16:
				len= wsprintf(ach, "トーチカ群",10);
				EditorKind=(byte)UnitKind.Pillboxes;
				EditorVariant=0;
				break;
			case 17:
				len= wsprintf(ach, "要塞",10);
				EditorKind=(byte)UnitKind.Fortress;
				EditorVariant=0;
				break;

			case 18:
				len = wsprintf(ach, "戦闘機",10);
				EditorKind=(byte)UnitKind.Fighter;
				EditorVariant=0;
				break;
			case 19:
				len = wsprintf(ach, "陸上戦闘機",10);
				EditorKind=(byte)UnitKind.Fighter;
				EditorVariant=1;
				break;
			case 20:
				len= wsprintf(ach, "攻撃機",10);
				EditorKind=(byte)UnitKind.Attacker;
				EditorVariant=0;
				break;
			case 21:
				len= wsprintf(ach, "戦略爆撃機",10);
				EditorKind=(byte)UnitKind.Bomber;
				EditorVariant=0;
				break;

			case 22:
				len= wsprintf(ach, "防空巡洋艦",10);
				EditorKind=(byte)UnitKind.Cruiser;
				EditorVariant=1;
				break;
			case 23:
				len= wsprintf(ach, "対潜駆逐艦",10);
				EditorKind=(byte)UnitKind.Destroyer;
				EditorVariant=1;
				break;
			case 24:
				if( LocalSide==Side.Japan )
					{
					len= wsprintf(ach, "大和級戦艦",10);
					EditorKind=(byte)UnitKind.Battleship;
					}
				else
					{
					len= wsprintf(ach, "エセックス型空母",10);
					EditorKind=(byte)UnitKind.Carrier;
					}
				EditorVariant=1;
				break;

			default:
				len= wsprintf(ach, "-----",10);
				break;
			}
		TextOut(hdc, 120, 640, ach, len);

		len = wsprintf(ach, "配置ユニット:");
		TextOut(hdc, 0, 640, ach, len);

		// 増援場所
		len = wsprintf(ach, "増援場所:");
		TextOut(hdc, 0, 660, ach, len);

		switch( Reinforcements[(int)LocalSide] )
			{
			case 0:
				len = wsprintf(ach, "左上");
				break;
			case 1:
				len = wsprintf(ach, "右上");
				break;
			case 2:
				len = wsprintf(ach, "右下");
				break;
			case 3:
				len = wsprintf(ach, "左下");
				break;
			}

		TextOut(hdc, 80, 660, ach, len);

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
