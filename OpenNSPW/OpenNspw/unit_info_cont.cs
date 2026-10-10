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

// Port of unit_info_cont.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{

public const int		map_close	= 1;

//============================================================================
// 補給ポイントとユニットの値段
//----------------------------------------------------------------------------
[Original("spry_pt_per_unit")]
public int	GetSupplyPointsPerUnit()
	{

	if( LocalSide==Side.UnitedStates )
		{
		switch( SupplyTarget )
			{
			case 0:		return(1000);	//戦艦
			case 1:		return(700);	//巡洋艦
			case 2:		return(450);	//駆逐艦
			case 3:		return(250);	//潜水艦

			case 4:		return(600);	//軽空母
			case 5:		return(850);	//正規空母

			case 6:		return(100);	//戦闘機
			case 7:		return(200);	//攻撃機
			case 8:		return(350);	//戦略爆撃機
			case 9:		return(220);	//陸上戦闘機

			case 10:	return(120);	//輸送船(歩兵基地)
			case 11:	return(550);	//輸送船(トーチカ群)
			case 12:	return(1200);	//輸送船(要塞)
			case 13:	return(1500);	//輸送船(航空基地)
			case 14:	return(2500);	//輸送船(軍港)

			case 15:		return(700);	//防空巡洋艦
			case 16:		return(450+30);	//対潜潜水艦

			case 17:		return(1200);	//エセックス型空母
			}
		}
	else
		{
		switch( SupplyTarget )
			{
			case 0:		return(1000);	//戦艦
			case 1:		return(700);	//巡洋艦
			case 2:		return(450);	//駆逐艦
			case 3:		return(250);	//潜水艦

			case 4:		return(600);	//軽空母
			case 5:		return(800);	//正規空母

			case 6:		return(100);	//戦闘機
			case 7:		return(200);	//攻撃機
			case 8:		return(250);	//戦略爆撃機
			case 9:		return(175);	//陸上戦闘機

			case 10:	return(100);	//輸送船(歩兵基地)
			case 11:	return(500);	//輸送船(トーチカ群)
			case 12:	return(1100);	//輸送船(要塞)
			case 13:	return(1500);	//輸送船(航空基地)
			case 14:	return(2500);	//輸送船(軍港)

			case 15:		return(700+20);	//防空巡洋艦
			case 16:		return(450-30);	//対潜潜水艦

			case 17:		return(1300);		//大和級戦艦
			}
		}

	return 0;
	}

private void DrawUnitDetails(SpriteId sprite, ref RECT src_rect, ref HDC hdc, ref Array8<int> len, ref Array8<Array128<byte>> ach)
	{
	RECT dstn_rect;
	int n;
	Sprites[sprite].no=UnitInfoPanel[2];
	if( UnitInfoPanel[1]!=0 )
		{
		if((UnitKind)UnitInfoPanel[0]==UnitKind.AirBase)
			{
			Sprites[sprite].no=9;
			}
		if((UnitKind)UnitInfoPanel[0]==UnitKind.Carrier)
			{
			Sprites[sprite].no=10;
			}
		if((UnitKind)UnitInfoPanel[0]==UnitKind.LightCarrier)
			{
			Sprites[sprite].no=11;
			}
		}

	// src_rect は ソースサーフェスのレクタングルです。
	src_rect.left = Sprites[sprite].base_x+(Sprites[sprite].wd * (Sprites[sprite].no % Sprites[sprite].os_of_x))+1;
	src_rect.top = Sprites[sprite].base_y+(Sprites[sprite].ht* (Sprites[sprite].no / Sprites[sprite].os_of_x))+1;
	src_rect.right = (src_rect.left + Sprites[sprite].wd)-3;
	src_rect.bottom = (src_rect.top + Sprites[sprite].ht)-2;

	// dstn_rect は ディスティネーションレクタングルです。
	dstn_rect.left=CMBT_WIDTH;
	dstn_rect.top=0;

	dstn_rect.right=dstn_rect.left+Sprites[sprite].wd-1+1;
	dstn_rect.bottom=dstn_rect.top+Sprites[sprite].ht-1+1;

	IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,(RECT*)Unsafe.AsPointer(ref src_rect),DDBLTFAST_WAIT );

	// そのユニットのテキストを表示します。
	if (IDirectDrawSurface_GetDC(lpDDSBack, (HDC*)Unsafe.AsPointer(ref hdc)) == DD_OK )
		{
		// draw stats, like frame number and frame rate
		SetBkMode(hdc, TRANSPARENT);
		SelectObject(hdc, gameFont_1);

#if !LNGG_VER
		// 艦種
		switch((UnitKind)UnitInfoPanel[0])
			{
			case UnitKind.Battleship:
				if( Units[UnitInfoPanel[3]].Side==Side.Japan && Units[UnitInfoPanel[3]].Variant==1 )
					len[0] = wsprintf(ach[0], "大和級戦艦");
				else
					len[0] = wsprintf(ach[0], "戦艦",10);
				break;
			case UnitKind.Cruiser:
				if( Units[UnitInfoPanel[3]].Variant==0 )
					len[0] = wsprintf(ach[0], "巡洋艦",10);
				else
				len[0] = wsprintf(ach[0], "防空巡洋艦",10);
				break;

			case UnitKind.Destroyer:
				if( Units[UnitInfoPanel[3]].Variant==0 )
					len[0] = wsprintf(ach[0], "駆逐艦",10);
				else
					len[0] = wsprintf(ach[0], "対潜駆逐艦",10);
				break;
			case UnitKind.Submarine:
				len[0] = wsprintf(ach[0], "潜水艦",10);
				break;
			case UnitKind.Carrier:
				if( Units[UnitInfoPanel[3]].Side==Side.UnitedStates && Units[UnitInfoPanel[3]].Variant==1 )
					len[0] = wsprintf(ach[0], "エセックス型空母",10);
				else
					len[0] = wsprintf(ach[0], "正規空母",10);
				break;
			case UnitKind.LightCarrier:
				len[0] = wsprintf(ach[0], "軽空母",10);
				break;
			case UnitKind.Transport:
				if(Units[UnitInfoPanel[3]].Ammo!=0)
					{
					if(Units[UnitInfoPanel[3]].Weapon==FireKind.CargoInfantryBase)
						len[0] = wsprintf(ach[0], "輸送船(歩兵基地)",10);
					else if(Units[UnitInfoPanel[3]].Weapon==FireKind.CargoPillboxes)
						len[0] = wsprintf(ach[0], "輸送船(トーチカ群)",10);
					else if(Units[UnitInfoPanel[3]].Weapon==FireKind.CargoFortress)
						len[0] = wsprintf(ach[0], "輸送船(要塞)",10);
					else if(Units[UnitInfoPanel[3]].Weapon==FireKind.CargoAirBase)
						len[0] = wsprintf(ach[0], "輸送船(航空基地)",10);
					else if(Units[UnitInfoPanel[3]].Weapon==FireKind.CargoNavalBase)
						len[0] = wsprintf(ach[0], "輸送船(軍港)",10);
					}
				else
					{
					len[0] = wsprintf(ach[0], "輸送船",10);
					}
				break;
			case UnitKind.Fighter:
				if( Units[UnitInfoPanel[3]].Variant==0 )
					len[0] = wsprintf(ach[0], "戦闘機",10);
				else
					len[0] = wsprintf(ach[0], "陸上戦闘機",10);
				break;
			case UnitKind.Attacker:
				len[0] = wsprintf(ach[0], "攻撃機",10);
				break;
			case UnitKind.Bomber:
				len[0] = wsprintf(ach[0], "戦略爆撃機",10);
				break;
			case UnitKind.NavalBase:
				len[0] = wsprintf(ach[0], "軍港",10);
				break;
			case UnitKind.AirBase:
				len[0] = wsprintf(ach[0], "航空基地",10);
				break;
			case UnitKind.City:
				len[0] = wsprintf(ach[0], "都市",10);
				break;
			case UnitKind.Mine:
				len[0] = wsprintf(ach[0], "鉱山",10);
				break;
			case UnitKind.InfantryBase:
				len[0] = wsprintf(ach[0], "歩兵基地",10);
				break;
			case UnitKind.Pillboxes:
				len[0] = wsprintf(ach[0], "トーチカ群",10);
				break;
			case UnitKind.Fortress:
				len[0] = wsprintf(ach[0], "要塞",10);
				break;
			default:
				len[0] = wsprintf(ach[0], "だっちゅーの",10);
				break;
			}

		// 損傷
		if( Units[UnitInfoPanel[3]].IsUsed && ( Units[UnitInfoPanel[3]].Kind==UnitKind.AirBase||Units[UnitInfoPanel[3]].Kind==UnitKind.NavalBase||Units[UnitInfoPanel[3]].Kind==UnitKind.InfantryBase||Units[UnitInfoPanel[3]].Kind==UnitKind.Pillboxes||Units[UnitInfoPanel[3]].Kind==UnitKind.Fortress ) && Units[UnitInfoPanel[3]].info[0]!=0 && Units[UnitInfoPanel[3]].Hp==Units[UnitInfoPanel[3]].MaxHp
			)
			{
			len[1] = wsprintf(ach[1], "工事:%d", Units[UnitInfoPanel[3]].BuildTime );
			}
		else
			{
			if( Units[UnitInfoPanel[3]].Hp <= Units[UnitInfoPanel[3]].MaxHp*0.2 )
				{
				len[1] = wsprintf(ach[1], "大破");
				}
			else if( Units[UnitInfoPanel[3]].Hp <= Units[UnitInfoPanel[3]].MaxHp*0.5 )
				{
				len[1] = wsprintf(ach[1], "中破");
				}
			else if( Units[UnitInfoPanel[3]].Hp <= Units[UnitInfoPanel[3]].MaxHp*0.7 )
				{
				len[1] = wsprintf(ach[1], "小破");
				}
			else if( Units[UnitInfoPanel[3]].Hp <= Units[UnitInfoPanel[3]].MaxHp-1 )
				{
				len[1] = wsprintf(ach[1], "軽損傷");
				}
			else
				{
				len[1] = wsprintf(ach[1], "損傷無");
				}
			}

		// 残弾薬
		len[2] = wsprintf(ach[2], "弾薬：%d",Units[UnitInfoPanel[3]].Ammo);

		// 残燃料
		if(Units[UnitInfoPanel[3]].Fuel==-1)
			len[3] = wsprintf(ach[3], "停泊艦船");
		else
			len[3] = wsprintf(ach[3], "燃料：%d", (int)(Units[UnitInfoPanel[3]].Fuel) );

#else
		// 艦種
		switch((UnitKind)unit_info[0])
			{
			case UnitKind.Battleship:
				if( unit[unit_info[3]].used==Side.Japan && unit[unit_info[3]].type==1 )
					len[0] = wsprintf(ach[0], "Type Yamato",10);
				else
					len[0] = wsprintf(ach[0], "Battleship",10);
				break;
			case UnitKind.Cruiser:
if( unit[unit_info[3]].type==0 )
				len[0] = wsprintf(ach[0], "Cruiser",10);
else
				len[0] = wsprintf(ach[0], "AntiAir Cruiser",10);

				break;
			case UnitKind.Destroyer:
				len[0] = wsprintf(ach[0], "Destroyer",10);
				break;
			case UnitKind.Submarine:
				len[0] = wsprintf(ach[0], "Submarine",10);
				break;
			case UnitKind.Carrier:
				if( unit[unit_info[3]].used==Side.UnitedStates && unit[unit_info[3]].type==1 )
					len[0] = wsprintf(ach[0], "Type Essex",10);
				else
					len[0] = wsprintf(ach[0], "Carrier",10);
				break;
			case UnitKind.LightCarrier:
				len[0] = wsprintf(ach[0], "Lt. Carrier",10);
				break;

			case UnitKind.Transport:
				if(unit[unit_info[3]].Ammo)
					{
					if(unit[unit_info[3]].Weapon==FireKind.CargoInfantryBase)
						len[0] = wsprintf(ach[0], "Transport(Trenchies)",10);
					else if(unit[unit_info[3]].Weapon==FireKind.CargoPillboxes)
						len[0] = wsprintf(ach[0], "Transport(Pillboxies)",10);
					else if(unit[unit_info[3]].Weapon==FireKind.CargoFortress)
						len[0] = wsprintf(ach[0], "Transport(Fortress)",10);
					else if(unit[unit_info[3]].Weapon==FireKind.CargoAirBase)
						len[0] = wsprintf(ach[0], "Transport(Airfield)",10);
					else if(unit[unit_info[3]].Weapon==FireKind.CargoNavalBase)
						len[0] = wsprintf(ach[0], "Transport(Port)",10);
					}
				else
					{
					len[0] = wsprintf(ach[0], "Transport",10);
					}

				break;
			case UnitKind.Fighter:
				if( unit[unit_info[3]].type==0 )
					len[0] = wsprintf(ach[0], "Car. Fighter",10);
				else
					len[0] = wsprintf(ach[0], "Grd. Fighter",10);
				break;

			case UnitKind.Attacker:
				len[0] = wsprintf(ach[0], "Car. Bomber",10);
				break;
			case UnitKind.Bomber:
				len[0] = wsprintf(ach[0], "Bomber",10);
				break;
			case UnitKind.NavalBase:
				len[0] = wsprintf(ach[0], "Military port",10);
				break;
			case UnitKind.AirBase:
				len[0] = wsprintf(ach[0], "Airfield",10);
				break;
			case UnitKind.City:
				len[0] = wsprintf(ach[0], "City",10);
				break;
			case UnitKind.Mine:
				len[0] = wsprintf(ach[0], "Mine",10);
				break;
			case UnitKind.InfantryBase:
				len[0] = wsprintf(ach[0], "Trenchies",10);
				break;
			case UnitKind.Pillboxes:
				len[0] = wsprintf(ach[0], "Pillboxies",10);
				break;
			case UnitKind.Fortress:
				len[0] = wsprintf(ach[0], "Fortress",10);
				break;
			default:
				len[0] = wsprintf(ach[0], "だっちゅーの",10);
				break;
			}

		// 損傷
		if( unit[unit_info[3]].Hp <= unit[unit_info[3]].MaxHp*0.2 )
			{
			len[1] = wsprintf(ach[1], "Heavy damaged"/*"大破"*/);
			}
		else if( unit[unit_info[3]].Hp <= unit[unit_info[3]].MaxHp*0.5 )
			{
			len[1] = wsprintf(ach[1], "Moderate damaged"/*"中破"*/);
			}
		else if( unit[unit_info[3]].Hp <= unit[unit_info[3]].MaxHp*0.7 )
			{
			len[1] = wsprintf(ach[1], "light damaged"/*"小破"*/);
			}
		else if( unit[unit_info[3]].Hp <= unit[unit_info[3]].MaxHp-1 )
			{
			len[1] = wsprintf(ach[1], "A bit of damaged");
			}
		else
			{
			len[1] = wsprintf(ach[1], "No damaged");
			}

		// 残弾薬
		len[2] = wsprintf(ach[2], "Ammo:%d",unit[unit_info[3]].Ammo);

		// 残燃料
		if(unit[unit_info[3]].Fuel==-1)
			len[3] = wsprintf(ach[3], "Anchored");
		else
			len[3] = wsprintf(ach[3], "Fuel:%d",(int)(unit[unit_info[3]].Fuel));

#endif

		for( n=0; n<=3; n++)
			{
			SetTextColor(hdc, RGB(255, 255, 0));
			TextOut(hdc, dstn_rect.left+20+110, 10+(n*20), ach[n], len[n]);
			}

		if( IsEditingMap!=0 )
			{
			len[0] = wsprintf(ach[0], "(%d/%d)", Units[UnitInfoPanel[3]].Hp, Units[UnitInfoPanel[3]].MaxHp);
			SetTextColor(hdc, RGB(255, 255, 0));
			TextOut(hdc, dstn_rect.left+20+110+55, 10+(1*20), ach[0], len[0]);
			}

		IDirectDrawSurface_ReleaseDC(lpDDSBack, hdc);
		}
	}

private void UpdateDeckView(ref RECT src_rect, SpriteId sprite, ref RECT dstn_rect)
	{
	int i;
	i=0;
	src_rect.left = Sprites[sprite].base_x+(Sprites[sprite].wd*0)+1;
	src_rect.top = Sprites[sprite].base_y+(Sprites[sprite].ht*(i+1))+1;
	src_rect.right = src_rect.left+(Sprites[sprite].wd)-1;
	src_rect.bottom = src_rect.top+(Sprites[sprite].ht)-1;

	dstn_rect.left=CMBT_WIDTH;
	dstn_rect.top=Sprites[SpriteId.JapanUnitInfo].y+Sprites[SpriteId.JapanUnitInfo].ht;
	dstn_rect.right=dstn_rect.left+(Sprites[sprite].wd)-1;
	dstn_rect.bottom=dstn_rect.top+(Sprites[sprite].ht)-1;

	if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 )
		{

		if( LeftButton==3 )
			{

			if( UnitInfoPanel[1]!=0 )
				{
				UnitInfoPanel[1]=0;				// 空母なら１で格納庫 ０ で飛行甲板
				}
			else
				{
				UnitInfoPanel[1]=1;				// 空母なら１で格納庫 ０ で飛行甲板

				Selections[1][SelectedUnit]=0;	CombatMenuKind=0; CombatMenuSelection=CombatMenuItem.None;
				SelectedUnit=0;
				ClearSelection();
				}
			}
		}
	if( LeftButton==2 && PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 )
		{
		src_rect.right+=180;	src_rect.left+=180;
		}

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,(RECT*)Unsafe.AsPointer(ref src_rect),0) )
	{
	RestoreSurfaces();
	}
	}

private void UpdateCombatMenu(ref Array6<int> menu, ref Array6<CombatMenuItem> menu2, ref int m, ref RECT src_rect, ref RECT dstn_rect)
	{
	SpriteId sprite;
	int n;
	int i;
	RECT wrk_rect;
	int g;
	int s;
	sprite=SpriteId.Buttons1;

	n=0;
	if( Units[SelectedUnit].Category==UnitCategory.Ship && !(Units[SelectedUnit].Kind==UnitKind.InfantryBase||Units[SelectedUnit].Kind==UnitKind.Pillboxes||Units[SelectedUnit].Kind==UnitKind.Fortress||Units[SelectedUnit].Kind==UnitKind.AirBase) )
		{	// 艦船のメニュー
		if( Units[SelectedUnit].IsStopping && Units[SelectedUnit].Speed==0 )
			{
			for( i=1; i<=MaxUnitId; i++)
				{
				ref var unit = ref Units[i];
				if( unit.Side==LocalSide && unit.Kind==UnitKind.NavalBase && unit.BuildTime==0 )
					{
					// ptin dbg
					wrk_rect.top=(int)unit.Position.Y+40+240;
					wrk_rect.right=(int)unit.Position.X+40+240;
					wrk_rect.bottom=(int)unit.Position.Y-40-240;
					wrk_rect.left=(int)unit.Position.X-40-240;

					if( PointInRect3( ref wrk_rect, (int)Units[SelectedUnit].Position.X, (int)Units[SelectedUnit].Position.Y )!=0 )
						{
						n=1;
						menu[0]=10;			// ボタンＣＧのインデックス
						menu2[0]=CombatMenuItem.Supply;

						if( Units[SelectedUnit].IsSupplying )
							CombatMenuSelection=CombatMenuItem.Supply;

						break;
						}
					}
				}
			}
		}
	else
		{
		if( Units[SelectedUnit].PlaneState==UnitState.Flying )
			{	// 飛行中のメニュー
			n=0;
			for(g=1;g<=MaxUnitId;g++)
				{
				ref var unit = ref Units[g];
				if( unit.IsUsed && ( unit.GroupLeader==SelectedUnit || g==SelectedUnit || Selections[1][g]!=0) )
					if( unit.PlaneState!=UnitState.Flying )
						{
						n=1; break;
						}
				}
			if( n==0 )
				{
				n=2;						// メニュー項目の数
				menu[0]=2;	menu2[0]=CombatMenuItem.Move;
				menu[1]=6;	menu2[1]=CombatMenuItem.Return;
				}
			else
				{
				n=1;						// メニュー項目の数
				menu[0]=2;	menu2[0]=CombatMenuItem.Move;
				}
			}
		else
			{	// 収容中のメニュー
			if( (Units[SelectedUnit].Kind==UnitKind.Attacker || Units[SelectedUnit].Kind==UnitKind.Bomber ) && Units[SelectedUnit].Weapon!=FireKind.Maintenance )
				{
				if( Units[SelectedUnit].Kind==UnitKind.Attacker || Units[SelectedUnit].Side==Side.Japan )
					{
					n=3;			// メニューの数
					menu[0]=7;	menu2[0]=CombatMenuItem.ArmWithTorpedo;
					menu[1]=8;	menu2[1]=CombatMenuItem.ArmWithBomb;
					menu[2]=9;	menu2[2]=CombatMenuItem.Disarm;
					}
				else
					{
					n=2;			// メニューの数
					menu[0]=8;	menu2[0]=CombatMenuItem.ArmWithBomb;
					menu[1]=9;	menu2[1]=CombatMenuItem.Disarm;
					}
				CombatMenuSelection=(CombatMenuItem)Units[SelectedUnit].Weapon;

				if ( Units[SelectedUnit].Ammo==0 )
					{
					CombatMenuSelection=(CombatMenuItem)FireKind.Unarmed;
					}
				}
			}
		}

	for( i=0;i<n;i++ )
		{
		m=menu[i];
		src_rect.left = Sprites[sprite].base_x+(Sprites[sprite].wd*0)+1;
		src_rect.top = Sprites[sprite].base_y+(Sprites[sprite].ht*(m))+1;
		src_rect.right = src_rect.left+Sprites[sprite].wd-1;
		src_rect.bottom = src_rect.top+Sprites[sprite].ht-1;

		dstn_rect.left=Sprites[SpriteId.ButtonBase].x;
		dstn_rect.top=Sprites[SpriteId.ButtonBase].y+(Sprites[sprite].ht*(i));
		dstn_rect.right=dstn_rect.left+Sprites[sprite].wd-1;
		dstn_rect.bottom=dstn_rect.top+Sprites[sprite].ht-1;

		if( LeftButton==3 && menu2[i]!=CombatMenuSelection && PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 )
			{
			if( CanOrder!=0 )
				{
				BufferedMenuOrders[1].Menu=menu2[i];
				BufferedMenuOrders[1].SelectedUnit=SelectedUnit;

				if(LocalSide==Side.Japan)
					{
					// 日本海軍サイド
					for(s=1;s<=JPN_SHIP_END;s++)
						{
						// 水上ユニット
						BufferedSelections[1][s-1]=Selections[1][s];
						}
					for(s=JPN_PLANE_START;s<=JPN_PLANE_END;s++)
						{
						// 航空ユニット
						BufferedSelections[1][s-JPN_SHIP_END-1]=Selections[1][s];
						}
					}
				else
					{
					// 合衆国海軍サイド
					for(s=USA_SHIP_START;s<=USA_SHIP_END;s++)
						{
						// 水上ユニット
						BufferedSelections[1][s-JPN_SHIP_END-1]=Selections[1][s];
						}
					for(s=USA_PLANE_START;s<=USA_PLANE_END;s++)
						{
						// 航空ユニット
						BufferedSelections[1][s-(USA_PLANE_END/2)-1]=Selections[1][s];
						}
					}
				CanOrder=0;
				HasOrdered=1;
				PlaySoundEffect( 0, SoundId.Click2 ,(double)(MAP_RIGHT+1), 0);
				}
			}
		if( LeftButton==2 && PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 && FrameCount%2<1)
			{

			src_rect.left=Sprites[sprite].base_x+(Sprites[sprite].wd*0)+1+180;
			src_rect.right=src_rect.left+Sprites[sprite].wd-1;
			}
		if( menu2[i]!=CombatMenuSelection && LeftButton==0 && PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 && FrameCount%6<4 )
			{
			src_rect.left=Sprites[sprite].base_x+(Sprites[sprite].wd*0)+1+180;
			src_rect.right=src_rect.left+Sprites[sprite].wd-1;
			}
		if( CombatMenuSelection==menu2[i] )
			{
			src_rect.left=Sprites[sprite].base_x+(Sprites[sprite].wd*0)+1+180;
			src_rect.right=src_rect.left+Sprites[sprite].wd-1;
			}

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,(RECT*)Unsafe.AsPointer(ref src_rect),0) )
	{
	RestoreSurfaces();
	}

		}
	}

private void DrawSupplyTarget(ref int m, ref RECT src_rect, int ry)
	{
	SpriteId sprite;
	int n;
	RECT dstn_rect = default;
	if(LocalSide==Side.Japan)
		sprite=SpriteId.JapanUnits;		//Off Screen Number		日本海軍の表示
	else
		sprite=SpriteId.UnitedStatesUnits;		//Off Screen Number		日本海軍の表示

	n=1;
	switch( SupplyTarget )
		{
		case 0:	case 1:	 case 2: case 3:
			m=SupplyTarget;
			break;
		case 15:			// 防空巡洋艦
			m=1;
			n=7;
			break;
		case 16:			// 対潜駆逐艦
			m=2;
			n=7;
			break;

		case 17:
			if( LocalSide==Side.Japan )
				m=0;		// 大和
			else
				m=6;		// エセックス
			n=7;
			break;

		case 4: case 5: case 6:
			m=SupplyTarget;
			m++;
			break;
		case 7:		m=9;	break;
		case 8:		m=12;	break;
		case 9:		m=14;	break;
		case 10: case 11: case 12: case 13: case 14:
			m=13;
			break;
		}

	// src_rect は ソースサーフェスのレクタングルです。
	src_rect.left = Sprites[sprite].base_x+(Sprites[sprite].wd * ( n )) +1;		// 方向

	src_rect.top = Sprites[sprite].base_y+(Sprites[sprite].ht* (m)) +1;		// 機種

	src_rect.right = (src_rect.left + Sprites[sprite].wd)-2;
	src_rect.bottom = (src_rect.top + Sprites[sprite].ht)-2;

	// dstn_rect は ディスティネーションレクタングルです。
	dstn_rect.left=1024-120+10;
	dstn_rect.top=ry+20*2+5;

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,(RECT*)Unsafe.AsPointer(ref src_rect),DDBLTFAST_SRCCOLORKEY) )
		{
		RestoreSurfaces();
		}
	}

private void UpdateSupplyPanel(ref Array8<int> len, ref Array8<Array128<byte>> ach, ref HDC hdc, ref RECT dstn_rect, int ry, ref int m)
	{
	int start;
	int end;
	int g;
	int n;
#if !LNGG_VER
	len[0] = wsprintf(ach[0], "補給割当:%d",SupplyPoints);
#else
	len[0] = wsprintf(ach[0], "Supry pts:%d",spry_pt);
#endif
	SetTextColor(hdc, RGB(255, 255, 255));
	TextOut(hdc, dstn_rect.left+120, ry, ach[0], len[0]);

#if !LNGG_VER
	len[0] = wsprintf(ach[0], "要求対象:");
#else
	len[0] = wsprintf(ach[0], "Object:");
#endif
	SetTextColor(hdc, RGB(255, 255, 255));
	TextOut(hdc, dstn_rect.left+120, ry+20*1, ach[0], len[0]);

	switch( SupplyTarget )
		{
#if !LNGG_VER
		case 0:		len[0] = wsprintf(ach[0], "戦艦");		break;
		case 1:		len[0] = wsprintf(ach[0], "巡洋艦");		break;
		case 2:		len[0] = wsprintf(ach[0], "駆逐艦");		break;
		case 3:		len[0] = wsprintf(ach[0], "潜水艦");		break;
		case 4:		len[0] = wsprintf(ach[0], "軽空母");		break;
		case 5:		len[0] = wsprintf(ach[0], "正規空母");		break;
		case 6:		len[0] = wsprintf(ach[0], "戦闘機");		break;
		case 7:		len[0] = wsprintf(ach[0], "攻撃機");		break;
		case 8:		len[0] = wsprintf(ach[0], "戦略爆撃機");		break;
		case 9:		len[0] = wsprintf(ach[0], "陸上戦闘機");		break;
		case 10:	len[0] = wsprintf(ach[0], "輸送船(歩兵基地)");		break;
		case 11:	len[0] = wsprintf(ach[0], "輸送船(トーチカ群)");		break;
		case 12:	len[0] = wsprintf(ach[0], "輸送船(要塞)");		break;
		case 13:	len[0] = wsprintf(ach[0], "輸送船(航空基地)");		break;
		case 14:	len[0] = wsprintf(ach[0], "輸送船(軍港)");		break;

		case 15:		len[0] = wsprintf(ach[0], "防空巡洋艦");		break;
		case 16:		len[0] = wsprintf(ach[0], "対潜駆逐艦");		break;

		case 17:
			if( LocalSide==Side.Japan )
				len[0] = wsprintf(ach[0], "大和級戦艦");
			else
				len[0] = wsprintf(ach[0], "エセックス型空母");

			break;

#else
		case 0:		len[0] = wsprintf(ach[0], "Battleship");		break;
		case 1:		len[0] = wsprintf(ach[0], "Cruiser");		break;
		case 2:		len[0] = wsprintf(ach[0], "Destroyer");		break;
		case 3:		len[0] = wsprintf(ach[0], "Submarine");		break;
		case 4:		len[0] = wsprintf(ach[0], "Light Carrier");		break;
		case 5:		len[0] = wsprintf(ach[0], "Carrier");		break;
		case 6:		len[0] = wsprintf(ach[0], "Car. Fighter");		break;
		case 7:		len[0] = wsprintf(ach[0], "Car. Bomber");		break;
		case 8:		len[0] = wsprintf(ach[0], "Bomber");		break;
		case 9:		len[0] = wsprintf(ach[0], "Grn. Fighter");		break;
		case 10:	len[0] = wsprintf(ach[0], "Transport(Trenchies)");		break;
		case 11:	len[0] = wsprintf(ach[0], "Transport(Pillboxies)");		break;
		case 12:	len[0] = wsprintf(ach[0], "Transport(Fortress)");		break;
		case 13:	len[0] = wsprintf(ach[0], "Transport(Airfield)");		break;
		case 14:	len[0] = wsprintf(ach[0], "Transport(Port)");		break;

		case 15:	len[0] = wsprintf(ach[0], "AntiAir Cruiser");		break;
		case 16:	len[0] = wsprintf(ach[0], "AntiSub Cruiser");		break;

		case 17:
			if( your_side==Side.Japan )
				len[0] = wsprintf(ach[0], "Class Yamato");
			else
				len[0] = wsprintf(ach[0], "Class Essex");

			break;
#endif
		}
	SetTextColor(hdc, RGB(255, 255, 255));
	TextOut(hdc, dstn_rect.left+120, ry+20*2, ach[0], len[0]);

	if( SupplyCount==0 )
		{
		// 値段の表示
		len[0] = wsprintf(ach[0], "%d:" ,GetSupplyPointsPerUnit());
		TextOut(hdc, Sprites[SpriteId.ButtonBase].x+120, ry+20*3+5+10, ach[0], len[0]);

		//
#if !LNGG_VER
		len[0] = wsprintf(ach[0], "<<<-前の機種");
#else
		len[0] = wsprintf(ach[0], "<<<- Previous");
#endif

		dstn_rect.left=Sprites[SpriteId.ButtonBase].x+120;
		dstn_rect.top=ry+20*5+5;
		dstn_rect.right=dstn_rect.left+(len[0]*12);
		dstn_rect.bottom=dstn_rect.top+18;
		if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 && SupplyCount==0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( LeftButton==3 )
				{
				if(SupplyTarget==0)
					SupplyTarget=17;
				else
					SupplyTarget--;
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}

		TextOut(hdc, Sprites[SpriteId.ButtonBase].x+120, ry+20*5+5, ach[0], len[0]);

		//
#if !LNGG_VER
		len[0] = wsprintf(ach[0], "次の機種->>>");
#else
		len[0] = wsprintf(ach[0], "Next ->>>");
#endif

		dstn_rect.left=Sprites[SpriteId.ButtonBase].x+120;
		dstn_rect.top=ry+20*6+5;
		dstn_rect.right=dstn_rect.left+(len[0]*12);
		dstn_rect.bottom=dstn_rect.top+18;
		if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( LeftButton==3 )
				{
				if(SupplyTarget==17)
					SupplyTarget=0;
				else
					SupplyTarget++;
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}

		TextOut(hdc, Sprites[SpriteId.ButtonBase].x+120, ry+20*6+5, ach[0], len[0]);

		// 要求する

		if(LocalSide==Side.Japan)
			{
			// 日本サイドのユニット
			if( SupplyTarget<=5 || SupplyTarget>=10  )
				{
				start=JPN_SHIP_START;
				end=JPN_SHIP_END;
				g=1;						// 艦船
				}
			else
				{
				start=JPN_PLANE_START;
				end=JPN_PLANE_END;
				g=2;						// 航空機
				}
			}
		else
			{
			// 合衆国サイドのユニット
			if( SupplyTarget<=5 || SupplyTarget>=10  )
				{
				start=USA_SHIP_START;
				end=USA_SHIP_END;
				g=1;						// 艦船
				}
			else
				{
				start=USA_PLANE_START;
				end=USA_PLANE_END;
				g=2;						// 航空機
				}
			}

		n=0;
		for( m=start; m<=end; m++)
			{
			if(!Units[m].IsUsed)
				{
				n++;
				}
			}

/***
			case 0:
				len[0] = wsprintf(ach[0], "全種増援可(%d)",arrival_cont);
				break;
			case 1:
				len[0] = wsprintf(ach[0], "全種増援不可(%d)",arrival_cont);
				break;
			case 2:
				len[0] = wsprintf(ach[0], "輸送船以外可(%d)",arrival_cont);
				break;
			case 3:
				len[0] = wsprintf(ach[0], "輸送船のみ可(%d)",arrival_cont);
				break;
			case 4:
				len[0] = wsprintf(ach[0], "戦闘艦船のみ可(%d)",arrival_cont);
				break;
			case 5:
				len[0] = wsprintf(ach[0], "航空機のみ可(%d)",arrival_cont);
				break;
			case 6:
				len[0] = wsprintf(ach[0], "輸送船(軍港)以外可(%d)",arrival_cont);
				break;
***/

		if(	(ArrivalControl!=1) &&
				(
					(ArrivalControl==0) ||
					(ArrivalControl==2 && ( SupplyTarget<=9 || SupplyTarget>=15 ) ) ||
					(ArrivalControl==3 && SupplyTarget>=10 && SupplyTarget<=14 ) ||
					(ArrivalControl==4 && (SupplyTarget<=3 || SupplyTarget>=15 ) && !(SupplyTarget==17 && LocalSide==Side.UnitedStates)  ) ||
					(ArrivalControl==5 && SupplyTarget>=6 && SupplyTarget<=9 ) ||
					(ArrivalControl==6 && SupplyTarget!=14 )
				)
				)
			{

			if( (g==1 && n!=0 ) || (g==2 && n>=3) )
				{
				if( SupplyPoints>=GetSupplyPointsPerUnit() )
					{
#if !LNGG_VER
					len[0] = wsprintf(ach[0], "　要求する");
#else
					len[0] = wsprintf(ach[0], "　Request");
#endif
					dstn_rect.left=Sprites[SpriteId.ButtonBase].x+120;
					dstn_rect.top=ry+20*7+5;
					dstn_rect.right=dstn_rect.left+(len[0]*12);
					dstn_rect.bottom=dstn_rect.top+18;
					if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 )
						{
						SetTextColor(hdc, RGB(255, 0, 0));
						if( LeftButton==3 && Result==GameResult.None )
							{
							SupplyPoints=(short)(SupplyPoints - GetSupplyPointsPerUnit());
							if(SupplyTarget<=5 || SupplyTarget>=10)
								SupplyCount=150;				// 艦船
							else
								SupplyCount=30;				// 航空機

if(CONN_DBG!=0)
SupplyCount=10;
							PlaySoundEffect( 0, SoundId.Click2 ,(double)(MAP_RIGHT+1), 0);
							}
						}
					else
						{
						SetTextColor(hdc, RGB(255, 255, 255));
						}
					TextOut(hdc, Sprites[SpriteId.ButtonBase].x+120, ry+20*7+5, ach[0], len[0]);
					}
				else
					{
					SetTextColor(hdc, RGB(255, 255, 255));
#if !LNGG_VER
					len[0] = wsprintf(ach[0], "割当点数不足");
#else
					len[0] = wsprintf(ach[0], "Shortage of pts");
#endif
					TextOut(hdc, Sprites[SpriteId.ButtonBase].x+120, ry+20*7+5, ach[0], len[0]);
					}
				}
			else
				{
				SetTextColor(hdc, RGB(255, 255, 255));
#if !LNGG_VER
				len[0] = wsprintf(ach[0], "ユニットリミット");
#else
				len[0] = wsprintf(ach[0], "Units Max");
#endif
				TextOut(hdc, Sprites[SpriteId.ButtonBase].x+120, ry+20*7+5, ach[0], len[0]);
				}

			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
#if !LNGG_VER
			len[0] = wsprintf(ach[0], "　要求制限");
#else
			len[0] = wsprintf(ach[0], "Restricted Unit");
#endif
			TextOut(hdc, Sprites[SpriteId.ButtonBase].x+120, ry+20*7+5, ach[0], len[0]);
			}
		}
	else
		{
		if((FrameCount%2)!=0)
			{
#if !LNGG_VER
			len[0] = wsprintf(ach[0], "要求中");
			TextOut(hdc, Sprites[SpriteId.ButtonBase].x+120+30, ry+20*6+5, ach[0], len[0]);
#else
			len[0] = wsprintf(ach[0], "Wait for coming");
			TextOut(hdc, sprt[SpriteId.ButtonBase].x+120, ry+20*6+5, ach[0], len[0]);
#endif
			}

		if( Result==GameResult.None )
			{
			SupplyCount--;

			if( SupplyCount==0 )
				{
				if(CanOrder==1)
					{
					BufferedArrivedUnits[1]=(short)(SupplyTarget+1);
					CanOrder=0;
					HasOrdered=1;
					}
				else
					{
					SupplyCount=1;
					}
				}
			}
		}
	}

private void UpdateSystemMenu(ref Array8<int> len, ref Array8<Array128<byte>> ach, int ry, ref HDC hdc)
	{
	RECT dstn_rect = default;
#if !LNGG_VER
	len[0] = wsprintf(ach[0], "シナリオ選択へ");
#else
	len[0] = wsprintf(ach[0], "to Mission Menu");
#endif
	dstn_rect.left=Sprites[SpriteId.ButtonBase].x+120;
	dstn_rect.top=ry+20*9+5;
	dstn_rect.right=dstn_rect.left+(len[0]*12);
	dstn_rect.bottom=dstn_rect.top+18;
	if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 && RivalMode==GameMode.Battle )
		{
		SetTextColor(hdc, RGB(255, 0, 0));
		if( LeftButton==3 )
			{
			if( IsEditingMap!=0)
				{

				Mode=GameMode.ConfigSetting;

/*
				if( IDOK==MessageBox( hwndApp,"シナリオ作成を中断しますか？","NSPW on the Net",MB_OKCANCEL|MB_DEFBUTTON2) )
					{
//							bf_game_system_menu[1]=GO_GAME_SETTING;
					go_cnct_game_setting();
					SoundPlayEffect( NULL, SoundId.Click2 ,(double)(MAP_RIGHT+1), 0);
					}
*/
				}
			else if( CanOrder==1 )
				{
	DialogAnswer=MessageType.GoToGameSetting;
				g_hDlg = CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_OK_CANCEL), hwndApp, (DLGPROC)IDD_OK_CANCEL_Proc );
				}
			}
		}
	else
		{
		SetTextColor(hdc, RGB(255, 255, 255));
		}
	TextOut(hdc, Sprites[SpriteId.ButtonBase].x+120, ry+20*9+5, ach[0], len[0]);

	if( IsEditingMap==0 )
		{
#if !LNGG_VER
		len[0] = wsprintf(ach[0], "リジューム");
#else
		len[0] = wsprintf(ach[0], "Resume");
#endif
		dstn_rect.left=Sprites[SpriteId.ButtonBase].x+120+20;
		dstn_rect.top=ry+20*10+5;
		dstn_rect.right=dstn_rect.left+(len[0]*12);
		dstn_rect.bottom=dstn_rect.top+18;
		if( PointInRect(ref dstn_rect,CursorPosition.x,CursorPosition.y)!=0 && RivalMode==GameMode.Battle )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( LeftButton==3 )
				{
				if( CanOrder==1 )
					{
	DialogAnswer=MessageType.ResumeAndGoToGameSetting;
					g_hDlg = CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_OK_CANCEL), hwndApp, (DLGPROC)IDD_OK_CANCEL_Proc );
					}
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}
		TextOut(hdc, Sprites[SpriteId.ButtonBase].x+120+20, ry+20*10+5, ach[0], len[0]);
		}
	}

//============================================================================
//ユニットインフォーメイション
//----------------------------------------------------------------------------
[Original("unit_info_cont")]
public void	UpdateUnitInfo()
	{
	RECT	src_rect = default,field_rect,dstn_rect = default;
	int	m=default /* C4701 */,n,wrk,wrk2,wrk3,f,ry; Array6<int> menu = default; Array6<CombatMenuItem> menu2 = default;
	SpriteId sprite;
    Array8<Array128<byte>> ach = default;
    Array8<int> len = default;
	HDC					hdc = default;
	//D3DXVECTOR2 chrPos;		// Direct3D is not used.

#if CONN_DBG
you_can_order=1;
rival_mode=mode;
#endif

	if( IsEditingMap!=0 )
		RivalMode=Mode;

	// ユニットインフォーメィション
	if( UnitInfoPanel[4]==(int)Side.Japan )
		sprite=SpriteId.JapanUnitInfo;	//	ユニットインフォのｏｓナンバー
	else
		sprite=SpriteId.UnitedStatesUnitInfo;	//	ユニットインフォのｏｓナンバー

	if( UnitInfoPanel[0]!=0 )
		{
		// ユニットインフォの絵（戦艦とか空母とか）を表示します。
		DrawUnitDetails(sprite, ref src_rect, ref hdc, ref len, ref ach);
		}

	//	スプライトグループ（メニュー下地）
	src_rect.left = Sprites[SpriteId.ButtonBase].base_x;
	src_rect.top = Sprites[SpriteId.ButtonBase].base_y+(Sprites[SpriteId.ButtonBase].ht*(Side.UnitedStates==LocalSide ? 1 : 0));
	src_rect.right = src_rect.left+Sprites[SpriteId.ButtonBase].wd;
	src_rect.bottom = src_rect.top+Sprites[SpriteId.ButtonBase].ht;

	dstn_rect.left=Sprites[SpriteId.ButtonBase].x=CMBT_WIDTH;
	dstn_rect.top=Sprites[SpriteId.ButtonBase].y=Sprites[SpriteId.JapanUnitInfo].y+Sprites[SpriteId.JapanUnitInfo].ht+20;

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,0) )
		{
		RestoreSurfaces();
		}

	sprite=SpriteId.Buttons1;
	if( (UnitKind)UnitInfoPanel[0]==UnitKind.Carrier || (UnitKind)UnitInfoPanel[0]==UnitKind.LightCarrier  || (UnitKind)UnitInfoPanel[0]==UnitKind.AirBase )
		{
		// 航空母艦の場合切り替えボタンを表示
		UpdateDeckView(ref src_rect, sprite, ref dstn_rect);

		}

	if(CombatMenuKind!=0 && IsEditingMap==0 )
		{

		//	スプライトグループ（メニュー）
		UpdateCombatMenu(ref menu, ref menu2, ref m, ref src_rect, ref dstn_rect);
		}

	// ゲームデータ
	if ( IsEditingMap==0 && IDirectDrawSurface_GetDC(lpDDSBack, &hdc) == DD_OK )
		{
		SetBkMode(hdc, TRANSPARENT);
		SelectObject(hdc, gameFont_1);

#if !LNGG_VER
		len[0] = wsprintf(ach[0], "スピード : %d",GameSpeed);
		len[1] = wsprintf(ach[1], "経過時間");
		len[2] = wsprintf(ach[2], "%d",BattleTime);

#else
		len[0] = wsprintf(ach[0], "SPEED : %d",game_speed);
		len[1] = wsprintf(ach[1], "TIME");
		len[2] = wsprintf(ach[2], "%d",rest_time);
#endif

		for( n=0; n<=2; n++)
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			TextOut(hdc, dstn_rect.left+20+110, 100+(n*20), ach[n], len[n]);
			}
		IDirectDrawSurface_ReleaseDC(lpDDSBack, hdc);
		}

	// 補給
	ry=190;
	if( IDirectDrawSurface_GetDC(lpDDSBack, &hdc) == DD_OK )
		{
		SetBkMode(hdc, TRANSPARENT);
		SelectObject(hdc, gameFont_1);

		if( IsEditingMap==0 )
			{
			UpdateSupplyPanel(ref len, ref ach, ref hdc, ref dstn_rect, ry, ref m);
			}
		ry+=30;

		if( IsHost!=0 )
			{
			UpdateSystemMenu(ref len, ref ach, ry, ref hdc);
			}

		IDirectDrawSurface_ReleaseDC(lpDDSBack, hdc);
		}

	ry-=30;

	// 補給
	if( ( SupplyCount==0 || (FrameCount%2)!=0 ) && IsEditingMap==0 )
		{
		DrawSupplyTarget(ref m, ref src_rect, ry);
		}
	}

//============================================================================
// ミニマップ
//----------------------------------------------------------------------------
[Original("draw_map")]
public void	DrawMinimap()
	{
	RECT	src_rect,field_rect,dstn_rect;
	int		m,n,base_x,base_y;
	byte	my_cl, en_cl;

	// マップの下地を描画
	Sprites[SpriteId.Minimap].x=CMBT_WIDTH;
	Sprites[SpriteId.Minimap].y=CMBT_HEIGHT-Sprites[SpriteId.Minimap].ht;

	src_rect.left = 	Sprites[SpriteId.Minimap].base_x;
	src_rect.top = Sprites[SpriteId.Minimap].base_y;
	src_rect.right = Sprites[SpriteId.Minimap].base_x+Sprites[SpriteId.Minimap].wd;
	src_rect.bottom = Sprites[SpriteId.Minimap].base_y+Sprites[SpriteId.Minimap].ht;

	// dstn_rect は ディスティネーションレクタングルです。
	dstn_rect.left=Sprites[SpriteId.Minimap].x;
	dstn_rect.top=Sprites[SpriteId.Minimap].y;

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,0) )
		{
		RestoreSurfaces();
		}

	base_x=Sprites[SpriteId.Minimap].x+8;
	base_y=Sprites[SpriteId.Minimap].y+8;

	// コンバット画面位置の描画
	my_cl=0;
	if( CameraPosition.X >= 0 )
		dstn_rect.left=base_x+((abs(MAP_LEFT)+(int)(CameraPosition.X))/80)*map_close;
	else
		dstn_rect.left=base_x+((abs(MAP_LEFT)+(int)(CameraPosition.X))/80)*map_close;
	if( CameraPosition.Y >= 0 )
		dstn_rect.top=base_y+((abs(MAP_TOP)-(int)(CameraPosition.Y))/80)*map_close;
	else
		dstn_rect.top=base_y+((abs(MAP_TOP)-(int)(CameraPosition.Y))/80)*map_close;

	dstn_rect.right=dstn_rect.left+10;
	dstn_rect.bottom=dstn_rect.top+10;

	DrawLine5(dstn_rect.left,dstn_rect.top,dstn_rect.right,dstn_rect.top,SCRN_WIDTH-1,SCRN_HEIGHT-1,my_cl);
	DrawLine5(dstn_rect.right,dstn_rect.top,dstn_rect.right,dstn_rect.bottom,SCRN_WIDTH-1,SCRN_HEIGHT-1,my_cl);
	DrawLine5(dstn_rect.right,dstn_rect.bottom,dstn_rect.left,dstn_rect.bottom,SCRN_WIDTH-1,SCRN_HEIGHT-1,my_cl);
	DrawLine5(dstn_rect.left,dstn_rect.bottom,dstn_rect.left,dstn_rect.top,SCRN_WIDTH-1,SCRN_HEIGHT-1,my_cl);

	// マップ上のカーソルクリックでの位置指定
	my_cl=255;
	if ( CursorPosition.x>=base_x && CursorPosition.x<=base_x+240 && CursorPosition.y>=base_y && CursorPosition.y<=base_y+180 )
		{
		dstn_rect.left=CursorPosition.x-5;
		dstn_rect.top=CursorPosition.y-5;
		dstn_rect.right=dstn_rect.left+10;
		dstn_rect.bottom=dstn_rect.top+10;

		DrawLine5(dstn_rect.left,dstn_rect.top,dstn_rect.right,dstn_rect.top,SCRN_WIDTH-1,SCRN_HEIGHT-1,my_cl);
		DrawLine5(dstn_rect.right,dstn_rect.top,dstn_rect.right,dstn_rect.bottom,SCRN_WIDTH-1,SCRN_HEIGHT-1,my_cl);
		DrawLine5(dstn_rect.right,dstn_rect.bottom,dstn_rect.left,dstn_rect.bottom,SCRN_WIDTH-1,SCRN_HEIGHT-1,my_cl);
		DrawLine5(dstn_rect.left,dstn_rect.bottom,dstn_rect.left,dstn_rect.top,SCRN_WIDTH-1,SCRN_HEIGHT-1,my_cl);

		if(LeftButton==2)
			{
			CameraPosition = new WorldPosition(MAP_LEFT+(((CursorPosition.x-5-base_x)/map_close)*80), MAP_TOP-(((CursorPosition.y-5-base_y)/map_close)*80));
			if(CameraPosition.Y>MAP_TOP)
				CameraPosition = new WorldPosition(CameraPosition.X, MAP_TOP);
			if(CameraPosition.X>(MAP_RIGHT-CMBT_WIDTH) )
				CameraPosition = new WorldPosition(MAP_RIGHT-CMBT_WIDTH, CameraPosition.Y);
			if(CameraPosition.Y<(MAP_BOTTOM+CMBT_HEIGHT) )
				CameraPosition = new WorldPosition(CameraPosition.X, MAP_BOTTOM+CMBT_HEIGHT);
			if(CameraPosition.X<MAP_LEFT)
				CameraPosition = new WorldPosition(MAP_LEFT, CameraPosition.Y);
			}
		}

	// ユニットの描画

	if( LocalSide==Side.Japan )
		{

		my_cl=(byte)(Sprites[SpriteId.Minimap].base_y+57);	en_cl=(byte)(Sprites[SpriteId.Minimap].base_y+16);

		}
	else
		{
		en_cl=(byte)(Sprites[SpriteId.Minimap].base_y+57);	my_cl=(byte)(Sprites[SpriteId.Minimap].base_y+16);
		}

	for(m=1; m<=MaxUnitId; m++)
		{
		ref var unit = ref Units[m];

		if( unit.Side==LocalSide  &&   !( unit.Position.Y>MAP_TOP || unit.Position.Y<MAP_BOTTOM || unit.Position.X<MAP_LEFT || unit.Position.X>MAP_RIGHT )    && !(unit.PlaneState==UnitState.Parked) && !(unit.Category==UnitCategory.Plane && (FrameCount%4)==0))
			{
			// マイユニット
			if( unit.Position.X >= 0 )
				dstn_rect.left=base_x+((abs(MAP_LEFT)+(int)(unit.Position.X-40))/80)*map_close;
			else
				dstn_rect.left=base_x+((abs(MAP_LEFT)+(int)(unit.Position.X-40))/80)*map_close;
			if( unit.Position.Y >= 0 )
				dstn_rect.top=base_y+((abs(MAP_TOP)-(int)(unit.Position.Y+40))/80)*map_close;
			else
				dstn_rect.top=base_y+((abs(MAP_TOP)-(int)(unit.Position.Y+40))/80)*map_close;

			dstn_rect.right=dstn_rect.left+2;
			dstn_rect.bottom=dstn_rect.top+2;

			src_rect.left = 267;
			if( LocalSide==Side.Japan )
				src_rect.top = Sprites[SpriteId.Minimap].base_y+16;
			else
				src_rect.top = Sprites[SpriteId.Minimap].base_y+57;
			src_rect.right = src_rect.left+3;
			src_rect.bottom = src_rect.top+3;

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,0) )
		{
		RestoreSurfaces();
		}

			}

		if( unit.IsUsed && unit.Side!=LocalSide && !( unit.Position.Y>MAP_TOP || unit.Position.Y<MAP_BOTTOM || unit.Position.X<MAP_LEFT || unit.Position.X>MAP_RIGHT ) && !(unit.PlaneState==UnitState.Parked) && unit.IsFound && !(unit.Category==UnitCategory.Plane && (FrameCount%4)==0) )
			{
			// エネユニット
			if( unit.Kind==UnitKind.Submarine && unit.IsSubmerged )
				{
				// 潜航中のおおよそ潜水艦
				if( unit.ContactX >= 0 )
					dstn_rect.left=base_x+((abs(MAP_LEFT)+(int)(unit.ContactX-40))/80)*map_close;
				else
					dstn_rect.left=base_x+((abs(MAP_LEFT)+(int)(unit.ContactX-40))/80)*map_close;
				if( unit.ContactY >= 0 )
					dstn_rect.top=base_y+((abs(MAP_TOP)-(int)(unit.ContactY+40))/80)*map_close;
				else
					dstn_rect.top=base_y+((abs(MAP_TOP)-(int)(unit.ContactY+40))/80)*map_close;
				}
			else
				{
				// 艦船基地航空機
				if( unit.Position.X >= 0 )
					dstn_rect.left=base_x+((abs(MAP_LEFT)+(int)(unit.Position.X-40))/80)*map_close;
				else
					dstn_rect.left=base_x+((abs(MAP_LEFT)+(int)(unit.Position.X-40))/80)*map_close;
				if( unit.Position.Y >= 0 )
					dstn_rect.top=base_y+((abs(MAP_TOP)-(int)(unit.Position.Y+40))/80)*map_close;
				else
					dstn_rect.top=base_y+((abs(MAP_TOP)-(int)(unit.Position.Y+40))/80)*map_close;
				}
			dstn_rect.right=dstn_rect.left+2;
			dstn_rect.bottom=dstn_rect.top+2;

			src_rect.left = 267;
			if( LocalSide==Side.Japan )
				src_rect.top = Sprites[SpriteId.Minimap].base_y+57;
			else
				src_rect.top = Sprites[SpriteId.Minimap].base_y+16;
			src_rect.right = src_rect.left+3;
			src_rect.bottom = src_rect.top+3;

			if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,0) )
				{
				RestoreSurfaces();
				}
			}
		}
	}

//============================================================================
// ミニマップ
// サーフェスリストアの時に使うようだ
//----------------------------------------------------------------------------
[Original("make_map")]
public void	MakeMinimap()
	{
	RECT	src_rect,field_rect,dstn_rect;
	int m,n;

	// マップデータから陸地をマップに描画します
	dstn_rect.left=Sprites[SpriteId.Minimap].base_x;
	dstn_rect.top=Sprites[SpriteId.Minimap].base_y;

	src_rect.left = Sprites[SpriteId.Minimap].base_x+306;
	src_rect.top = Sprites[SpriteId.Minimap].base_y;
	src_rect.right = src_rect.left+255;
	src_rect.bottom = src_rect.top+199;

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDS_OS, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,0) )
		{
		RestoreSurfaces();
		}

	for(m=0; m<=179; m++) // 縦の個数	マップの枠 縦１８０ドット
		for( n=0; n<=239; n++) // 横の個数		マップの枠 横２４０ドット
			{
			if( MapTiles[m][n]!=0 )
				{
				// 陸地有り
				dstn_rect.left=Sprites[SpriteId.Minimap].base_x+8+n-0;
				dstn_rect.top=Sprites[SpriteId.Minimap].base_y+8+m-0;

				src_rect.left = Sprites[SpriteId.Minimap].base_x+270;
				src_rect.top = Sprites[SpriteId.Minimap].base_y+110;

				src_rect.right = src_rect.left+2;
				src_rect.bottom = src_rect.top+2;

				if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDS_OS, dstn_rect.left, dstn_rect.top,lpDDS_OS,&src_rect,0) )
					{
					RestoreSurfaces();
					}

				}
			}
	}

//============================================================================
//ユニットインフォーメイション
//----------------------------------------------------------------------------
[Original("cnct_unit_info_cont_now")]
public void	ApplyUnitInfoInput()
	{
	CombatMenuItem	menu2; short e,tmp_slct_unit,g,i;

	for(e=0;e<=1;e++)
		{
		menu2=BufferedMenuOrders[e].Menu;
		tmp_slct_unit=BufferedMenuOrders[e].SelectedUnit;

		// 指揮、随伴ユニットに下命
		if(menu2!=0)
			{
			CombatMenuSelection=menu2;
			if( menu2==CombatMenuItem.Supply )
				{
				Units[tmp_slct_unit].SupplyTime=1;
				Units[tmp_slct_unit].Target=0;

				// ちょっと一応
				if( Units[tmp_slct_unit].Kind==UnitKind.Carrier || Units[tmp_slct_unit].Kind==UnitKind.LightCarrier )
					{
					Units[tmp_slct_unit].LandingLock=0;	// 着艦、0許可、1不許可
					Units[tmp_slct_unit].LaunchLock=0;	// その空母の次機発進許可	0許可、1不許可
					}
				if( Units[tmp_slct_unit].Kind==UnitKind.Submarine )
					Units[tmp_slct_unit].IsSubmerged=false;		// 強制浮上
				}
			else
				{
				for(g=1;g<=MaxUnitId;g++)
					{
					ref var unit = ref Units[g];
					if( unit.IsUsed && ( unit.GroupLeader==tmp_slct_unit || g==tmp_slct_unit || Selections[e][g]!=0) )
						{
						// 決定後の書く個別の処理
						switch( unit.Kind )
							{
							case UnitKind.Fighter: case UnitKind.Attacker: case UnitKind.Bomber:
								if(  menu2==CombatMenuItem.ArmWithTorpedo || menu2==CombatMenuItem.ArmWithBomb || menu2==CombatMenuItem.Disarm  )
									{
									if(unit.Kind==UnitKind.Attacker || (unit.Kind==UnitKind.Bomber && ( (menu2==CombatMenuItem.ArmWithTorpedo && unit.Side==Side.Japan ) || menu2==CombatMenuItem.ArmWithBomb || menu2==CombatMenuItem.Disarm) ))
										{		// 収容中の攻撃機だったばあい
										unit.Weapon=(FireKind)menu2;

										if(menu2==CombatMenuItem.ArmWithTorpedo)
											unit.Ammo=1;		// 魚雷の場合は常に１、弾数。
										else
											unit.Ammo=unit.MaxAmmo;

										if(menu2==CombatMenuItem.Disarm)
											unit.ReloadTime=1;
										else
											unit.ReloadTime=RDY_SPAN;
										}
									}
								else
									{
									if( (unit.Kind==UnitKind.Fighter ) && menu2==CombatMenuItem.Return  && unit.PlaneState==UnitState.Flying )
										{
										unit.Mode=(UnitMode)menu2;
										}
									if( (unit.Kind==UnitKind.Attacker || unit.Kind==UnitKind.Bomber ) && menu2==CombatMenuItem.Return )
										{
										unit.Ammo=0;		// 帰投選択時に攻撃機なら武装投棄
										unit.Target=0;		// 帰投選択時に攻撃機攻撃目標放棄
										unit.Mode=(UnitMode)menu2;
										}
									if( unit.Category==UnitCategory.Plane && menu2==CombatMenuItem.Move && unit.PlaneState==UnitState.Flying  )
										{
										unit.DeckPhase=0;		// 着艦準備をクリア
										unit.Mode=(UnitMode)menu2;
										}
									}
								break;
							case UnitKind.Battleship: case UnitKind.Cruiser: case UnitKind.Destroyer: case UnitKind.Submarine: case UnitKind.Carrier: case UnitKind.LightCarrier:
								break;
							}
						}
					}
				}
			}
		}
	}
}
