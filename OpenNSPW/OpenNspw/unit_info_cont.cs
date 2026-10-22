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

public const int		MinimapCellSize	= 1;

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

private void WriteUnitKindName(ref Array8<int> length, ref Array8<Array128<byte>> text)
	{
	switch((UnitKind)UnitInfoPanel[0])
		{
		case UnitKind.Battleship:
			if( Units[UnitInfoPanel[3]].Side==Side.Japan && Units[UnitInfoPanel[3]].Variant==1 )
				length[0] = wsprintf(text[0], "大和級戦艦");
			else
				length[0] = wsprintf(text[0], "戦艦",10);
			break;
		case UnitKind.Cruiser:
			if( Units[UnitInfoPanel[3]].Variant==0 )
				length[0] = wsprintf(text[0], "巡洋艦",10);
			else
			length[0] = wsprintf(text[0], "防空巡洋艦",10);
			break;

		case UnitKind.Destroyer:
			if( Units[UnitInfoPanel[3]].Variant==0 )
				length[0] = wsprintf(text[0], "駆逐艦",10);
			else
				length[0] = wsprintf(text[0], "対潜駆逐艦",10);
			break;
		case UnitKind.Submarine:
			length[0] = wsprintf(text[0], "潜水艦",10);
			break;
		case UnitKind.Carrier:
			if( Units[UnitInfoPanel[3]].Side==Side.UnitedStates && Units[UnitInfoPanel[3]].Variant==1 )
				length[0] = wsprintf(text[0], "エセックス型空母",10);
			else
				length[0] = wsprintf(text[0], "正規空母",10);
			break;
		case UnitKind.LightCarrier:
			length[0] = wsprintf(text[0], "軽空母",10);
			break;
		case UnitKind.Transport:
			if(Units[UnitInfoPanel[3]].Ammo!=0)
				{
				if(Units[UnitInfoPanel[3]].Weapon==FireKind.CargoInfantryBase)
					length[0] = wsprintf(text[0], "輸送船(歩兵基地)",10);
				else if(Units[UnitInfoPanel[3]].Weapon==FireKind.CargoPillboxes)
					length[0] = wsprintf(text[0], "輸送船(トーチカ群)",10);
				else if(Units[UnitInfoPanel[3]].Weapon==FireKind.CargoFortress)
					length[0] = wsprintf(text[0], "輸送船(要塞)",10);
				else if(Units[UnitInfoPanel[3]].Weapon==FireKind.CargoAirBase)
					length[0] = wsprintf(text[0], "輸送船(航空基地)",10);
				else if(Units[UnitInfoPanel[3]].Weapon==FireKind.CargoNavalBase)
					length[0] = wsprintf(text[0], "輸送船(軍港)",10);
				}
			else
				{
				length[0] = wsprintf(text[0], "輸送船",10);
				}
			break;
		case UnitKind.Fighter:
			if( Units[UnitInfoPanel[3]].Variant==0 )
				length[0] = wsprintf(text[0], "戦闘機",10);
			else
				length[0] = wsprintf(text[0], "陸上戦闘機",10);
			break;
		case UnitKind.Attacker:
			length[0] = wsprintf(text[0], "攻撃機",10);
			break;
		case UnitKind.Bomber:
			length[0] = wsprintf(text[0], "戦略爆撃機",10);
			break;
		case UnitKind.NavalBase:
			length[0] = wsprintf(text[0], "軍港",10);
			break;
		case UnitKind.AirBase:
			length[0] = wsprintf(text[0], "航空基地",10);
			break;
		case UnitKind.City:
			length[0] = wsprintf(text[0], "都市",10);
			break;
		case UnitKind.Mine:
			length[0] = wsprintf(text[0], "鉱山",10);
			break;
		case UnitKind.InfantryBase:
			length[0] = wsprintf(text[0], "歩兵基地",10);
			break;
		case UnitKind.Pillboxes:
			length[0] = wsprintf(text[0], "トーチカ群",10);
			break;
		case UnitKind.Fortress:
			length[0] = wsprintf(text[0], "要塞",10);
			break;
		default:
			length[0] = wsprintf(text[0], "だっちゅーの",10);
			break;
		}
	}

// The texts of the unit whose information the sidebar shows: [0] its kind, [1] its damage, or its building time,
// [2] its ammunition and [3] its fuel.
private void WriteUnitDetails(ref Array8<int> length, ref Array8<Array128<byte>> text)
	{
	// 艦種
	WriteUnitKindName(ref length, ref text);

	// 損傷
	if( Units[UnitInfoPanel[3]].IsUsed && ( Units[UnitInfoPanel[3]].Kind==UnitKind.AirBase||Units[UnitInfoPanel[3]].Kind==UnitKind.NavalBase||Units[UnitInfoPanel[3]].Kind==UnitKind.InfantryBase||Units[UnitInfoPanel[3]].Kind==UnitKind.Pillboxes||Units[UnitInfoPanel[3]].Kind==UnitKind.Fortress ) && Units[UnitInfoPanel[3]].info[0]!=0 && Units[UnitInfoPanel[3]].Hp==Units[UnitInfoPanel[3]].MaxHp
		)
		{
		length[1] = wsprintf(text[1], "工事:%d", Units[UnitInfoPanel[3]].BuildTime );
		}
	else
		{
		if( Units[UnitInfoPanel[3]].Hp <= Units[UnitInfoPanel[3]].MaxHp*0.2 )
			{
			length[1] = wsprintf(text[1], "大破");
			}
		else if( Units[UnitInfoPanel[3]].Hp <= Units[UnitInfoPanel[3]].MaxHp*0.5 )
			{
			length[1] = wsprintf(text[1], "中破");
			}
		else if( Units[UnitInfoPanel[3]].Hp <= Units[UnitInfoPanel[3]].MaxHp*0.7 )
			{
			length[1] = wsprintf(text[1], "小破");
			}
		else if( Units[UnitInfoPanel[3]].Hp <= Units[UnitInfoPanel[3]].MaxHp-1 )
			{
			length[1] = wsprintf(text[1], "軽損傷");
			}
		else
			{
			length[1] = wsprintf(text[1], "損傷無");
			}
		}

	// 残弾薬
	length[2] = wsprintf(text[2], "弾薬：%d",Units[UnitInfoPanel[3]].Ammo);

	// 残燃料
	if(Units[UnitInfoPanel[3]].Fuel==-1)
		length[3] = wsprintf(text[3], "停泊艦船");
	else
		length[3] = wsprintf(text[3], "燃料：%d", (int)(Units[UnitInfoPanel[3]].Fuel) );
	}

private void DrawUnitDetails(SpriteId sprite, ref RECT source, ref HDC hdc, ref Array8<int> length, ref Array8<Array128<byte>> text)
	{
	RECT destination;
	Sprites[sprite].Frame=UnitInfoPanel[2];
	if( UnitInfoPanel[1]!=0 )
		{
		if((UnitKind)UnitInfoPanel[0]==UnitKind.AirBase)
			{
			Sprites[sprite].Frame=9;
			}
		if((UnitKind)UnitInfoPanel[0]==UnitKind.Carrier)
			{
			Sprites[sprite].Frame=10;
			}
		if((UnitKind)UnitInfoPanel[0]==UnitKind.LightCarrier)
			{
			Sprites[sprite].Frame=11;
			}
		}

	// src_rect は ソースサーフェスのレクタングルです。
	source.left = Sprites[sprite].SheetPosition.X+(Sprites[sprite].Size.Width * (Sprites[sprite].Frame % Sprites[sprite].FramesPerRow))+1;
	source.top = Sprites[sprite].SheetPosition.Y+(Sprites[sprite].Size.Height* (Sprites[sprite].Frame / Sprites[sprite].FramesPerRow))+1;
	source.right = (source.left + Sprites[sprite].Size.Width)-3;
	source.bottom = (source.top + Sprites[sprite].Size.Height)-2;

	// dstn_rect は ディスティネーションレクタングルです。
	destination.left=CMBT_WIDTH;
	destination.top=0;

	destination.right=destination.left+Sprites[sprite].Size.Width-1+1;
	destination.bottom=destination.top+Sprites[sprite].Size.Height-1+1;

	IDirectDrawSurface_BltFast( lpDDSBack, destination.left, destination.top,OffscreenSurface,(RECT*)Unsafe.AsPointer(ref source),DDBLTFAST_WAIT );

	// そのユニットのテキストを表示します。
	if (IDirectDrawSurface_GetDC(lpDDSBack, (HDC*)Unsafe.AsPointer(ref hdc)) == DD_OK )
		{
		// draw stats, like frame number and frame rate
		SetBkMode(hdc, TRANSPARENT);
		SelectObject(hdc, GameFont1);

#if !LNGG_VER
		WriteUnitDetails(ref length, ref text);

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

		for( var n=0; n<=3; n++)
			{
			SetTextColor(hdc, RGB(255, 255, 0));
			TextOut(hdc, destination.left+20+110, 10+(n*20), text[n], length[n]);
			}

		if( IsEditingMap )
			{
			length[0] = wsprintf(text[0], "(%d/%d)", Units[UnitInfoPanel[3]].Hp, Units[UnitInfoPanel[3]].MaxHp);
			SetTextColor(hdc, RGB(255, 255, 0));
			TextOut(hdc, destination.left+20+110+55, 10+(1*20), text[0], length[0]);
			}

		IDirectDrawSurface_ReleaseDC(lpDDSBack, hdc);
		}
	}

// A click on the switch of a carrier's or an air base's view (切り替え): its hangar instead of its flight deck, which
// deselects, or its flight deck again.
private void ToggleDeckView()
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

private void UpdateDeckView(ref RECT source, SpriteId sprite, ref RECT destination)
	{

	// The host's UI's commands for the switch (UICommands.cs).
	RunUICommands(UICommandSite.DeckView, true);

	var i=0;
	source.left = Sprites[sprite].SheetPosition.X+(Sprites[sprite].Size.Width*0)+1;
	source.top = Sprites[sprite].SheetPosition.Y+(Sprites[sprite].Size.Height*(i+1))+1;
	source.right = source.left+(Sprites[sprite].Size.Width)-1;
	source.bottom = source.top+(Sprites[sprite].Size.Height)-1;

	destination.left=CMBT_WIDTH;
	destination.top=Sprites[SpriteId.JapanUnitInfo].Position.Y+Sprites[SpriteId.JapanUnitInfo].Size.Height;
	destination.right=destination.left+(Sprites[sprite].Size.Width)-1;
	destination.bottom=destination.top+(Sprites[sprite].Size.Height)-1;

	if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 )
		{

		if( LeftButton==3 )
			{
			ToggleDeckView();
			}
		}
	if( LeftButton==2 && PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 )
		{
		source.right+=180;	source.left+=180;
		}

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, destination.left, destination.top,OffscreenSurface,(RECT*)Unsafe.AsPointer(ref source),0) )
	{
	RestoreSurfaces();
	}
	}

// The combat menu's items for the selected unit: in `menu`, their buttons' pictures (Buttons1), and in `menuItems`, the
// items. Returns their number. The choice shown (CombatMenuSelection) follows a supplying ship or a parked plane's arms.
private int ListCombatMenu(ref Array6<int> menu, ref Array6<CombatMenuItem> menuItems)
	{

	var n=0;
	if( Units[SelectedUnit].Category==UnitCategory.Ship && !(Units[SelectedUnit].Kind==UnitKind.InfantryBase||Units[SelectedUnit].Kind==UnitKind.Pillboxes||Units[SelectedUnit].Kind==UnitKind.Fortress||Units[SelectedUnit].Kind==UnitKind.AirBase) )
		{	// 艦船のメニュー
		if( Units[SelectedUnit].IsStopping && Units[SelectedUnit].Speed==0 )
			{
			for( var i=1; i<=MaxUnitId; i++)
				{
				ref var unit = ref Units[i];
				if( unit.Side==LocalSide && unit.Kind==UnitKind.NavalBase && unit.BuildTime==0 )
					{
					// ptin dbg
					var area=WorldRect.Around(unit.Position, 40+240, 40+240);

					if( area.Contains(Units[SelectedUnit].Position) )
						{
						n=1;
						menu[0]=10;			// ボタンＣＧのインデックス
						menuItems[0]=CombatMenuItem.Supply;

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
			for(var m=1;m<=MaxUnitId;m++)
				{
				ref var unit = ref Units[m];
				if( unit.IsUsed && ( unit.GroupLeader==SelectedUnit || m==SelectedUnit || Selections[1][m]!=0) )
					if( unit.PlaneState!=UnitState.Flying )
						{
						n=1; break;
						}
				}
			if( n==0 )
				{
				n=2;						// メニュー項目の数
				menu[0]=2;	menuItems[0]=CombatMenuItem.Move;
				menu[1]=6;	menuItems[1]=CombatMenuItem.Return;
				}
			else
				{
				n=1;						// メニュー項目の数
				menu[0]=2;	menuItems[0]=CombatMenuItem.Move;
				}
			}
		else
			{	// 収容中のメニュー
			if( (Units[SelectedUnit].Kind==UnitKind.Attacker || Units[SelectedUnit].Kind==UnitKind.Bomber ) && Units[SelectedUnit].Weapon!=FireKind.Maintenance )
				{
				if( Units[SelectedUnit].Kind==UnitKind.Attacker || Units[SelectedUnit].Side==Side.Japan )
					{
					n=3;			// メニューの数
					menu[0]=7;	menuItems[0]=CombatMenuItem.ArmWithTorpedo;
					menu[1]=8;	menuItems[1]=CombatMenuItem.ArmWithBomb;
					menu[2]=9;	menuItems[2]=CombatMenuItem.Disarm;
					}
				else
					{
					n=2;			// メニューの数
					menu[0]=8;	menuItems[0]=CombatMenuItem.ArmWithBomb;
					menu[1]=9;	menuItems[1]=CombatMenuItem.Disarm;
					}
				CombatMenuSelection=(CombatMenuItem)Units[SelectedUnit].Weapon;

				if ( Units[SelectedUnit].Ammo==0 )
					{
					CombatMenuSelection=(CombatMenuItem)FireKind.Unarmed;
					}
				}
			}
		}

	return n;
	}

// A click on an item of the combat menu, while the player can order (CanOrder): the item is the order of the turn.
private void ChooseCombatMenuItem(CombatMenuItem item)
	{
	BufferedMenuOrders[1].Menu=item;
	BufferedMenuOrders[1].SelectedUnit=SelectedUnit;

	BufferSelections();
	CanOrder=false;
	HasOrdered=true;
	PlayInterfaceSound(SoundId.Click2);
	}

private void UpdateCombatMenu(ref Array6<int> menu, ref Array6<CombatMenuItem> menuItems, ref int m, ref RECT source, ref RECT destination)
	{
	var sprite=SpriteId.Buttons1;

	var n=ListCombatMenu(ref menu, ref menuItems);

	// The host's UI's commands for the combat menu (UICommands.cs), on the items it shows, which wait for a frame in which
	// they count.
	ShownCombatMenu=menuItems;
	ShownCombatMenuCount=n;
	RunUICommands(UICommandSite.CombatMenu, true);
	RunWaitingCombatMenu();

	for( var i=0;i<n;i++ )
		{
		m=menu[i];
		source.left = Sprites[sprite].SheetPosition.X+(Sprites[sprite].Size.Width*0)+1;
		source.top = Sprites[sprite].SheetPosition.Y+(Sprites[sprite].Size.Height*(m))+1;
		source.right = source.left+Sprites[sprite].Size.Width-1;
		source.bottom = source.top+Sprites[sprite].Size.Height-1;

		destination.left=Sprites[SpriteId.ButtonBase].Position.X;
		destination.top=Sprites[SpriteId.ButtonBase].Position.Y+(Sprites[sprite].Size.Height*(i));
		destination.right=destination.left+Sprites[sprite].Size.Width-1;
		destination.bottom=destination.top+Sprites[sprite].Size.Height-1;

		if( LeftButton==3 && menuItems[i]!=CombatMenuSelection && PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 )
			{
			if( CanOrder )
				{
				ChooseCombatMenuItem(menuItems[i]);
				}
			}
		if( LeftButton==2 && PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 && FrameCount%2<1)
			{

			source.left=Sprites[sprite].SheetPosition.X+(Sprites[sprite].Size.Width*0)+1+180;
			source.right=source.left+Sprites[sprite].Size.Width-1;
			}
		if( menuItems[i]!=CombatMenuSelection && LeftButton==0 && PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 && FrameCount%6<4 )
			{
			source.left=Sprites[sprite].SheetPosition.X+(Sprites[sprite].Size.Width*0)+1+180;
			source.right=source.left+Sprites[sprite].Size.Width-1;
			}
		if( CombatMenuSelection==menuItems[i] )
			{
			source.left=Sprites[sprite].SheetPosition.X+(Sprites[sprite].Size.Width*0)+1+180;
			source.right=source.left+Sprites[sprite].Size.Width-1;
			}

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, destination.left, destination.top,OffscreenSurface,(RECT*)Unsafe.AsPointer(ref source),0) )
	{
	RestoreSurfaces();
	}

		}
	}

// The frame of the reinforcement chosen in the supply panel (SupplyTarget) on the sheet of the player's units: row m, its
// kind, and column n, its direction.
private void ChooseSupplyTargetFrame(ref int m, out int n)
	{
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
	}

private void DrawSupplyTarget(ref int m, ref RECT source, int y)
	{
	SpriteId sprite;
	int n;
	RECT destination = default;
	if(LocalSide==Side.Japan)
		sprite=SpriteId.JapanUnits;		//Off Screen Number		日本海軍の表示
	else
		sprite=SpriteId.UnitedStatesUnits;		//Off Screen Number		日本海軍の表示

	ChooseSupplyTargetFrame(ref m, out n);

	// src_rect は ソースサーフェスのレクタングルです。
	source.left = Sprites[sprite].SheetPosition.X+(Sprites[sprite].Size.Width * ( n )) +1;		// 方向

	source.top = Sprites[sprite].SheetPosition.Y+(Sprites[sprite].Size.Height* (m)) +1;		// 機種

	source.right = (source.left + Sprites[sprite].Size.Width)-2;
	source.bottom = (source.top + Sprites[sprite].Size.Height)-2;

	// dstn_rect は ディスティネーションレクタングルです。
	destination.left=1024-120+10;
	destination.top=y+20*2+5;

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, destination.left, destination.top,OffscreenSurface,(RECT*)Unsafe.AsPointer(ref source),DDBLTFAST_SRCCOLORKEY) )
		{
		RestoreSurfaces();
		}
	}

private void WriteSupplyTargetName(ref Array8<int> length, ref Array8<Array128<byte>> text)
	{
	switch( SupplyTarget )
		{
#if !LNGG_VER
		case 0:		length[0] = wsprintf(text[0], "戦艦");		break;
		case 1:		length[0] = wsprintf(text[0], "巡洋艦");		break;
		case 2:		length[0] = wsprintf(text[0], "駆逐艦");		break;
		case 3:		length[0] = wsprintf(text[0], "潜水艦");		break;
		case 4:		length[0] = wsprintf(text[0], "軽空母");		break;
		case 5:		length[0] = wsprintf(text[0], "正規空母");		break;
		case 6:		length[0] = wsprintf(text[0], "戦闘機");		break;
		case 7:		length[0] = wsprintf(text[0], "攻撃機");		break;
		case 8:		length[0] = wsprintf(text[0], "戦略爆撃機");		break;
		case 9:		length[0] = wsprintf(text[0], "陸上戦闘機");		break;
		case 10:	length[0] = wsprintf(text[0], "輸送船(歩兵基地)");		break;
		case 11:	length[0] = wsprintf(text[0], "輸送船(トーチカ群)");		break;
		case 12:	length[0] = wsprintf(text[0], "輸送船(要塞)");		break;
		case 13:	length[0] = wsprintf(text[0], "輸送船(航空基地)");		break;
		case 14:	length[0] = wsprintf(text[0], "輸送船(軍港)");		break;

		case 15:		length[0] = wsprintf(text[0], "防空巡洋艦");		break;
		case 16:		length[0] = wsprintf(text[0], "対潜駆逐艦");		break;

		case 17:
			if( LocalSide==Side.Japan )
				length[0] = wsprintf(text[0], "大和級戦艦");
			else
				length[0] = wsprintf(text[0], "エセックス型空母");

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
	}

// Whether the player can request the reinforcement chosen in the supply panel (SupplyTarget), or why not.
private SupplyRequestState GetSupplyRequestState()
	{
	int start;
	int end;
	int group;
	if(LocalSide==Side.Japan)
		{
		// 日本サイドのユニット
		if( SupplyTarget<=5 || SupplyTarget>=10  )
			{
			start=JPN_SHIP_START;
			end=JPN_SHIP_END;
			group=1;						// 艦船
			}
		else
			{
			start=JPN_PLANE_START;
			end=JPN_PLANE_END;
			group=2;						// 航空機
			}
		}
	else
		{
		// 合衆国サイドのユニット
		if( SupplyTarget<=5 || SupplyTarget>=10  )
			{
			start=USA_SHIP_START;
			end=USA_SHIP_END;
			group=1;						// 艦船
			}
		else
			{
			start=USA_PLANE_START;
			end=USA_PLANE_END;
			group=2;						// 航空機
			}
		}

	var n=0;
	for( var m=start; m<=end; m++)
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

		if( (group==1 && n!=0 ) || (group==2 && n>=3) )
			{
			if( SupplyPoints>=GetSupplyPointsPerUnit() )
				return SupplyRequestState.Available;
			else
				return SupplyRequestState.ShortOfPoints;
			}
		else
			return SupplyRequestState.UnitLimit;
		}
	else
		return SupplyRequestState.Restricted;
	}

// A click that requests the reinforcement chosen in the supply panel, when it can be requested (GetSupplyRequestState)
// and the battle has no result: it costs its supply points, and comes after a while (SupplyCount).
private void RequestSupplyUnit()
	{
	SupplyPoints=(short)(SupplyPoints - GetSupplyPointsPerUnit());
	if(SupplyTarget<=5 || SupplyTarget>=10)
		SupplyCount=150;				// 艦船
	else
		SupplyCount=30;				// 航空機

if(CONN_DBG!=0)
SupplyCount=10;
	PlayInterfaceSound(SoundId.Click2);
	}

private void RequestSupply(ref Array8<int> length, ref Array8<Array128<byte>> text, ref RECT destination, int y, ref HDC hdc)
	{
	switch( GetSupplyRequestState() )
		{
		case SupplyRequestState.Available:
			{
#if !LNGG_VER
			length[0] = wsprintf(text[0], "　要求する");
#else
			len[0] = wsprintf(ach[0], "　Request");
#endif
			destination.left=Sprites[SpriteId.ButtonBase].Position.X+120;
			destination.top=y+20*7+5;
			destination.right=destination.left+(length[0]*12);
			destination.bottom=destination.top+18;
			if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 )
				{
				SetTextColor(hdc, RGB(255, 0, 0));
				if( LeftButton==3 && Result==GameResult.None )
					{
					RequestSupplyUnit();
					}
				}
			else
				{
				SetTextColor(hdc, RGB(255, 255, 255));
				}
			TextOut(hdc, Sprites[SpriteId.ButtonBase].Position.X+120, y+20*7+5, text[0], length[0]);
			}
			break;

		case SupplyRequestState.ShortOfPoints:
			{
			SetTextColor(hdc, RGB(255, 255, 255));
#if !LNGG_VER
			length[0] = wsprintf(text[0], "割当点数不足");
#else
			len[0] = wsprintf(ach[0], "Shortage of pts");
#endif
			TextOut(hdc, Sprites[SpriteId.ButtonBase].Position.X+120, y+20*7+5, text[0], length[0]);
			}
			break;

		case SupplyRequestState.UnitLimit:
			{
			SetTextColor(hdc, RGB(255, 255, 255));
#if !LNGG_VER
			length[0] = wsprintf(text[0], "ユニットリミット");
#else
			len[0] = wsprintf(ach[0], "Units Max");
#endif
			TextOut(hdc, Sprites[SpriteId.ButtonBase].Position.X+120, y+20*7+5, text[0], length[0]);
			}
			break;

		case SupplyRequestState.Restricted:
			{
			SetTextColor(hdc, RGB(255, 255, 255));
#if !LNGG_VER
			length[0] = wsprintf(text[0], "　要求制限");
#else
			len[0] = wsprintf(ach[0], "Restricted Unit");
#endif
			TextOut(hdc, Sprites[SpriteId.ButtonBase].Position.X+120, y+20*7+5, text[0], length[0]);
			}
			break;
		}
	}
// A click on the previous kind of reinforcement (前の機種), from the first to the last.
private void SelectPreviousSupplyTarget()
	{
	if(SupplyTarget==0)
		SupplyTarget=17;
	else
		SupplyTarget--;
	}

// A click on the next kind of reinforcement (次の機種), from the last to the first.
private void SelectNextSupplyTarget()
	{
	if(SupplyTarget==17)
		SupplyTarget=0;
	else
		SupplyTarget++;
	}

private void UpdateSupplyRequest(ref Array8<int> length, ref Array8<Array128<byte>> text, ref HDC hdc, int y, ref RECT destination)
	{
	// The host's UI's commands for the supply panel (UICommands.cs).
	RunUICommands(UICommandSite.SupplyPanel, true);

	length[0] = wsprintf(text[0], "%d:" ,GetSupplyPointsPerUnit());
	TextOut(hdc, Sprites[SpriteId.ButtonBase].Position.X+120, y+20*3+5+10, text[0], length[0]);

	// The previous kind.
#if !LNGG_VER
	length[0] = wsprintf(text[0], "<<<-前の機種");
#else
	len[0] = wsprintf(ach[0], "<<<- Previous");
#endif

	destination.left=Sprites[SpriteId.ButtonBase].Position.X+120;
	destination.top=y+20*5+5;
	destination.right=destination.left+(length[0]*12);
	destination.bottom=destination.top+18;
	if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 && SupplyCount==0 )
		{
		SetTextColor(hdc, RGB(255, 0, 0));
		if( LeftButton==3 )
			{
			SelectPreviousSupplyTarget();
			}
		}
	else
		{
		SetTextColor(hdc, RGB(255, 255, 255));
		}

	TextOut(hdc, Sprites[SpriteId.ButtonBase].Position.X+120, y+20*5+5, text[0], length[0]);

	// The next kind.
#if !LNGG_VER
	length[0] = wsprintf(text[0], "次の機種->>>");
#else
	len[0] = wsprintf(ach[0], "Next ->>>");
#endif

	destination.left=Sprites[SpriteId.ButtonBase].Position.X+120;
	destination.top=y+20*6+5;
	destination.right=destination.left+(length[0]*12);
	destination.bottom=destination.top+18;
	if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 )
		{
		SetTextColor(hdc, RGB(255, 0, 0));
		if( LeftButton==3 )
			{
			SelectNextSupplyTarget();
			}
		}
	else
		{
		SetTextColor(hdc, RGB(255, 255, 255));
		}

	TextOut(hdc, Sprites[SpriteId.ButtonBase].Position.X+120, y+20*6+5, text[0], length[0]);

	// 要求する
	RequestSupply(ref length, ref text, ref destination, y, ref hdc);
	}

private void UpdateSupplyPanel(ref Array8<int> length, ref Array8<Array128<byte>> text, ref HDC hdc, ref RECT destination, int y)
	{
#if !LNGG_VER
	length[0] = wsprintf(text[0], "補給割当:%d",SupplyPoints);
#else
	len[0] = wsprintf(ach[0], "Supry pts:%d",spry_pt);
#endif
	SetTextColor(hdc, RGB(255, 255, 255));
	TextOut(hdc, destination.left+120, y, text[0], length[0]);

#if !LNGG_VER
	length[0] = wsprintf(text[0], "要求対象:");
#else
	len[0] = wsprintf(ach[0], "Object:");
#endif
	SetTextColor(hdc, RGB(255, 255, 255));
	TextOut(hdc, destination.left+120, y+20*1, text[0], length[0]);

	WriteSupplyTargetName(ref length, ref text);
	SetTextColor(hdc, RGB(255, 255, 255));
	TextOut(hdc, destination.left+120, y+20*2, text[0], length[0]);

	if( SupplyCount==0 )
		{
		// 値段の表示
		UpdateSupplyRequest(ref length, ref text, ref hdc, y, ref destination);
		}
	else
		{
		// The host's UI's commands for choosing and requesting, while a reinforcement is coming, are dropped
		// (UICommands.cs).
		RunUICommands(UICommandSite.SupplyPanel, false);

		if((FrameCount%2)!=0)
			{
#if !LNGG_VER
			length[0] = wsprintf(text[0], "要求中");
			TextOut(hdc, Sprites[SpriteId.ButtonBase].Position.X+120+30, y+20*6+5, text[0], length[0]);
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
				if(CanOrder.Value==1)
					{
					BufferedArrivedUnits[1]=(short)(SupplyTarget+1);
					CanOrder=false;
					HasOrdered=true;
					}
				else
					{
					SupplyCount=1;
					}
				}
			}
		}
	}

// A click on an item of the host's system menu (シナリオ選択へ, リジューム), while the player can order: the game asks
// whether to do it (IDD_OK_CANCEL), and then sends the answer.
private void AskSystemMenu(MessageType answer)
	{
	DialogAnswer=answer;
	MainDialog = CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_OK_CANCEL), hwndApp, (DLGPROC)IDD_OK_CANCEL_Proc );
	}

private void UpdateSystemMenu(ref Array8<int> length, ref Array8<Array128<byte>> text, int y, ref HDC hdc)
	{
	RECT destination = default;

	// The host's UI's commands for the system menu (UICommands.cs), which wait for a frame in which they count.
	RunUICommands(UICommandSite.SystemMenu, true);
	RunWaitingSystemMenu();
#if !LNGG_VER
	length[0] = wsprintf(text[0], "シナリオ選択へ");
#else
	len[0] = wsprintf(ach[0], "to Mission Menu");
#endif
	destination.left=Sprites[SpriteId.ButtonBase].Position.X+120;
	destination.top=y+20*9+5;
	destination.right=destination.left+(length[0]*12);
	destination.bottom=destination.top+18;
	if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 && RivalMode==GameMode.Battle )
		{
		SetTextColor(hdc, RGB(255, 0, 0));
		if( LeftButton==3 )
			{
			if( IsEditingMap)
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
			else if( CanOrder.Value==1 )
				{
				AskSystemMenu(MessageType.GoToGameSetting);
				}
			}
		}
	else
		{
		SetTextColor(hdc, RGB(255, 255, 255));
		}
	TextOut(hdc, Sprites[SpriteId.ButtonBase].Position.X+120, y+20*9+5, text[0], length[0]);

	if( !IsEditingMap )
		{
#if !LNGG_VER
		length[0] = wsprintf(text[0], "リジューム");
#else
		len[0] = wsprintf(ach[0], "Resume");
#endif
		destination.left=Sprites[SpriteId.ButtonBase].Position.X+120+20;
		destination.top=y+20*10+5;
		destination.right=destination.left+(length[0]*12);
		destination.bottom=destination.top+18;
		if( PointInRect(ref destination,CursorPosition.x,CursorPosition.y)!=0 && RivalMode==GameMode.Battle )
			{
			SetTextColor(hdc, RGB(255, 0, 0));
			if( LeftButton==3 )
				{
				if( CanOrder.Value==1 )
					{
					AskSystemMenu(MessageType.ResumeAndGoToGameSetting);
					}
				}
			}
		else
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			}
		TextOut(hdc, Sprites[SpriteId.ButtonBase].Position.X+120+20, y+20*10+5, text[0], length[0]);
		}
	}

//============================================================================
//ユニットインフォーメイション
//----------------------------------------------------------------------------
[Original("unit_info_cont")]
public void	UpdateUnitInfo()
	{
	RECT source = default, destination = default;
	int m=default /* C4701 */; Array6<int> menu = default; Array6<CombatMenuItem> menuItems = default;
	SpriteId sprite;
    Array8<Array128<byte>> text = default;
    Array8<int> length = default;
	HDC					hdc = default;
	//D3DXVECTOR2 chrPos;		// Direct3D is not used.

#if CONN_DBG
you_can_order=1;
rival_mode=mode;
#endif

	if( IsEditingMap )
		RivalMode=Mode;

	// ユニットインフォーメィション
	if( UnitInfoPanel[4]==(int)Side.Japan )
		sprite=SpriteId.JapanUnitInfo;	//	ユニットインフォのｏｓナンバー
	else
		sprite=SpriteId.UnitedStatesUnitInfo;	//	ユニットインフォのｏｓナンバー

	if( UnitInfoPanel[0]!=0 )
		{
		// ユニットインフォの絵（戦艦とか空母とか）を表示します。
		DrawUnitDetails(sprite, ref source, ref hdc, ref length, ref text);
		}

	//	スプライトグループ（メニュー下地）
	source.left = Sprites[SpriteId.ButtonBase].SheetPosition.X;
	source.top = Sprites[SpriteId.ButtonBase].SheetPosition.Y+(Sprites[SpriteId.ButtonBase].Size.Height*(Side.UnitedStates==LocalSide ? 1 : 0));
	source.right = source.left+Sprites[SpriteId.ButtonBase].Size.Width;
	source.bottom = source.top+Sprites[SpriteId.ButtonBase].Size.Height;

	Sprites[SpriteId.ButtonBase].Position = new PixelPoint(CMBT_WIDTH, Sprites[SpriteId.JapanUnitInfo].Position.Y+Sprites[SpriteId.JapanUnitInfo].Size.Height+20);
	destination.left=Sprites[SpriteId.ButtonBase].Position.X;
	destination.top=Sprites[SpriteId.ButtonBase].Position.Y;

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, destination.left, destination.top,OffscreenSurface,&source,0) )
		{
		RestoreSurfaces();
		}

	sprite=SpriteId.Buttons1;
	if( (UnitKind)UnitInfoPanel[0]==UnitKind.Carrier || (UnitKind)UnitInfoPanel[0]==UnitKind.LightCarrier  || (UnitKind)UnitInfoPanel[0]==UnitKind.AirBase )
		{
		// 航空母艦の場合切り替えボタンを表示
		UpdateDeckView(ref source, sprite, ref destination);

		}
	else
		{
		// The host's UI's commands for the switch, which is not shown, are dropped (UICommands.cs).
		RunUICommands(UICommandSite.DeckView, false);
		}

	if(CombatMenuKind!=0 && !IsEditingMap )
		{

		//	スプライトグループ（メニュー）
		UpdateCombatMenu(ref menu, ref menuItems, ref m, ref source, ref destination);
		}
	else
		{
		// The host's UI's commands for the combat menu, which is not shown, are dropped (UICommands.cs).
		ShownCombatMenuCount=0;
		RunUICommands(UICommandSite.CombatMenu, false);
		RunWaitingCombatMenu();
		}

	// ゲームデータ
	if ( !IsEditingMap && IDirectDrawSurface_GetDC(lpDDSBack, &hdc) == DD_OK )
		{
		SetBkMode(hdc, TRANSPARENT);
		SelectObject(hdc, GameFont1);

#if !LNGG_VER
		length[0] = wsprintf(text[0], "スピード : %d",GameSpeed);
		length[1] = wsprintf(text[1], "経過時間");
		length[2] = wsprintf(text[2], "%d",BattleTime);

#else
		len[0] = wsprintf(ach[0], "SPEED : %d",game_speed);
		len[1] = wsprintf(ach[1], "TIME");
		len[2] = wsprintf(ach[2], "%d",rest_time);
#endif

		for( var n=0; n<=2; n++)
			{
			SetTextColor(hdc, RGB(255, 255, 255));
			TextOut(hdc, destination.left+20+110, 100+(n*20), text[n], length[n]);
			}
		IDirectDrawSurface_ReleaseDC(lpDDSBack, hdc);
		}

	// 補給
	var y=190;
	if( IDirectDrawSurface_GetDC(lpDDSBack, &hdc) == DD_OK )
		{
		SetBkMode(hdc, TRANSPARENT);
		SelectObject(hdc, GameFont1);

		if( !IsEditingMap )
			{
			UpdateSupplyPanel(ref length, ref text, ref hdc, ref destination, y);
			}
		else
			{
			// The host's UI's commands for the supply panel, which is not shown, are dropped (UICommands.cs).
			RunUICommands(UICommandSite.SupplyPanel, false);
			}
		y+=30;

		if( IsHost )
			{
			UpdateSystemMenu(ref length, ref text, y, ref hdc);
			}
		else
			{
			// The host's UI's commands for the system menu, which only the host has, are dropped (UICommands.cs).
			RunUICommands(UICommandSite.SystemMenu, false);
			}

		IDirectDrawSurface_ReleaseDC(lpDDSBack, hdc);
		}

	y-=30;

	// 補給
	IsSupplyTargetShown = ( SupplyCount==0 || (FrameCount%2)!=0 ) && !IsEditingMap;
	if( IsSupplyTargetShown )
		{
		DrawSupplyTarget(ref m, ref source, y);
		}
	}

// A click held on the minimap, at cell (column, row) of the map: the battlefield shows the map from there, kept within
// the map.
private void ShowMinimapCell(int column, int row)
	{
	CameraPosition = new WorldPosition(MAP_LEFT+((column/MinimapCellSize)*80), MAP_TOP-((row/MinimapCellSize)*80));
	if(CameraPosition.Y>MAP_TOP)
		CameraPosition = new WorldPosition(CameraPosition.X, MAP_TOP);
	if(CameraPosition.X>(MAP_RIGHT-CMBT_WIDTH) )
		CameraPosition = new WorldPosition(MAP_RIGHT-CMBT_WIDTH, CameraPosition.Y);
	if(CameraPosition.Y<(MAP_BOTTOM+CMBT_HEIGHT) )
		CameraPosition = new WorldPosition(CameraPosition.X, MAP_BOTTOM+CMBT_HEIGHT);
	if(CameraPosition.X<MAP_LEFT)
		CameraPosition = new WorldPosition(MAP_LEFT, CameraPosition.Y);
	}

//============================================================================
// ミニマップ
//----------------------------------------------------------------------------
[Original("draw_map")]
public void	DrawMinimap()
	{
	RECT source, destination;

	// マップの下地を描画
	Sprites[SpriteId.Minimap].Position = new PixelPoint(CMBT_WIDTH, CMBT_HEIGHT-Sprites[SpriteId.Minimap].Size.Height);

	source.left = 	Sprites[SpriteId.Minimap].SheetPosition.X;
	source.top = Sprites[SpriteId.Minimap].SheetPosition.Y;
	source.right = Sprites[SpriteId.Minimap].SheetPosition.X+Sprites[SpriteId.Minimap].Size.Width;
	source.bottom = Sprites[SpriteId.Minimap].SheetPosition.Y+Sprites[SpriteId.Minimap].Size.Height;

	// dstn_rect は ディスティネーションレクタングルです。
	destination.left=Sprites[SpriteId.Minimap].Position.X;
	destination.top=Sprites[SpriteId.Minimap].Position.Y;

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, destination.left, destination.top,OffscreenSurface,&source,0) )
		{
		RestoreSurfaces();
		}

	var baseX=Sprites[SpriteId.Minimap].Position.X+8;
	var baseY=Sprites[SpriteId.Minimap].Position.Y+8;

	// コンバット画面位置の描画
	byte ownColor=0;
	if( CameraPosition.X >= 0 )
		destination.left=baseX+((abs(MAP_LEFT)+(int)(CameraPosition.X))/80)*MinimapCellSize;
	else
		destination.left=baseX+((abs(MAP_LEFT)+(int)(CameraPosition.X))/80)*MinimapCellSize;
	if( CameraPosition.Y >= 0 )
		destination.top=baseY+((abs(MAP_TOP)-(int)(CameraPosition.Y))/80)*MinimapCellSize;
	else
		destination.top=baseY+((abs(MAP_TOP)-(int)(CameraPosition.Y))/80)*MinimapCellSize;

	destination.right=destination.left+10;
	destination.bottom=destination.top+10;

	DrawLine5(destination.left,destination.top,destination.right,destination.top,SCRN_WIDTH-1,SCRN_HEIGHT-1,ownColor);
	DrawLine5(destination.right,destination.top,destination.right,destination.bottom,SCRN_WIDTH-1,SCRN_HEIGHT-1,ownColor);
	DrawLine5(destination.right,destination.bottom,destination.left,destination.bottom,SCRN_WIDTH-1,SCRN_HEIGHT-1,ownColor);
	DrawLine5(destination.left,destination.bottom,destination.left,destination.top,SCRN_WIDTH-1,SCRN_HEIGHT-1,ownColor);

	// マップ上のカーソルクリックでの位置指定
	ownColor=255;
	if ( CursorPosition.x>=baseX && CursorPosition.x<=baseX+240 && CursorPosition.y>=baseY && CursorPosition.y<=baseY+180 )
		{
		destination.left=CursorPosition.x-5;
		destination.top=CursorPosition.y-5;
		destination.right=destination.left+10;
		destination.bottom=destination.top+10;

		DrawLine5(destination.left,destination.top,destination.right,destination.top,SCRN_WIDTH-1,SCRN_HEIGHT-1,ownColor);
		DrawLine5(destination.right,destination.top,destination.right,destination.bottom,SCRN_WIDTH-1,SCRN_HEIGHT-1,ownColor);
		DrawLine5(destination.right,destination.bottom,destination.left,destination.bottom,SCRN_WIDTH-1,SCRN_HEIGHT-1,ownColor);
		DrawLine5(destination.left,destination.bottom,destination.left,destination.top,SCRN_WIDTH-1,SCRN_HEIGHT-1,ownColor);

		if(LeftButton==2)
			{
			ShowMinimapCell(CursorPosition.x-5-baseX, CursorPosition.y-5-baseY);
			}
		}

	// The host's UI's commands for clicks held on the minimap (UICommands.cs).
	RunUICommands(UICommandSite.Minimap, true);

	// ユニットの描画

	if( LocalSide==Side.Japan )
		{

		ownColor=(byte)(Sprites[SpriteId.Minimap].SheetPosition.Y+57);	var enemyColor=(byte)(Sprites[SpriteId.Minimap].SheetPosition.Y+16);

		}
	else
		{
		var enemyColor=(byte)(Sprites[SpriteId.Minimap].SheetPosition.Y+57);	ownColor=(byte)(Sprites[SpriteId.Minimap].SheetPosition.Y+16);
		}

	for(var m=1; m<=MaxUnitId; m++)
		{
		ref var unit = ref Units[m];

		if( unit.Side==LocalSide  &&   IsInWorld(unit.Position)    && !(unit.PlaneState==UnitState.Parked) && !(unit.Category==UnitCategory.Plane && (FrameCount%4)==0))
			{
			// マイユニット
			if( unit.Position.X >= 0 )
				destination.left=baseX+((abs(MAP_LEFT)+(int)(unit.Position.X-40))/80)*MinimapCellSize;
			else
				destination.left=baseX+((abs(MAP_LEFT)+(int)(unit.Position.X-40))/80)*MinimapCellSize;
			if( unit.Position.Y >= 0 )
				destination.top=baseY+((abs(MAP_TOP)-(int)(unit.Position.Y+40))/80)*MinimapCellSize;
			else
				destination.top=baseY+((abs(MAP_TOP)-(int)(unit.Position.Y+40))/80)*MinimapCellSize;

			destination.right=destination.left+2;
			destination.bottom=destination.top+2;

			source.left = 267;
			if( LocalSide==Side.Japan )
				source.top = Sprites[SpriteId.Minimap].SheetPosition.Y+16;
			else
				source.top = Sprites[SpriteId.Minimap].SheetPosition.Y+57;
			source.right = source.left+3;
			source.bottom = source.top+3;

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, destination.left, destination.top,OffscreenSurface,&source,0) )
		{
		RestoreSurfaces();
		}

			}

		if( unit.IsUsed && unit.Side!=LocalSide && IsInWorld(unit.Position) && !(unit.PlaneState==UnitState.Parked) && unit.IsFound && !(unit.Category==UnitCategory.Plane && (FrameCount%4)==0) )
			{
			// エネユニット
			if( unit.Kind==UnitKind.Submarine && unit.IsSubmerged )
				{
				// 潜航中のおおよそ潜水艦
				if( unit.ContactX >= 0 )
					destination.left=baseX+((abs(MAP_LEFT)+(int)(unit.ContactX-40))/80)*MinimapCellSize;
				else
					destination.left=baseX+((abs(MAP_LEFT)+(int)(unit.ContactX-40))/80)*MinimapCellSize;
				if( unit.ContactY >= 0 )
					destination.top=baseY+((abs(MAP_TOP)-(int)(unit.ContactY+40))/80)*MinimapCellSize;
				else
					destination.top=baseY+((abs(MAP_TOP)-(int)(unit.ContactY+40))/80)*MinimapCellSize;
				}
			else
				{
				// 艦船基地航空機
				if( unit.Position.X >= 0 )
					destination.left=baseX+((abs(MAP_LEFT)+(int)(unit.Position.X-40))/80)*MinimapCellSize;
				else
					destination.left=baseX+((abs(MAP_LEFT)+(int)(unit.Position.X-40))/80)*MinimapCellSize;
				if( unit.Position.Y >= 0 )
					destination.top=baseY+((abs(MAP_TOP)-(int)(unit.Position.Y+40))/80)*MinimapCellSize;
				else
					destination.top=baseY+((abs(MAP_TOP)-(int)(unit.Position.Y+40))/80)*MinimapCellSize;
				}
			destination.right=destination.left+2;
			destination.bottom=destination.top+2;

			source.left = 267;
			if( LocalSide==Side.Japan )
				source.top = Sprites[SpriteId.Minimap].SheetPosition.Y+57;
			else
				source.top = Sprites[SpriteId.Minimap].SheetPosition.Y+16;
			source.right = source.left+3;
			source.bottom = source.top+3;

			if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( lpDDSBack, destination.left, destination.top,OffscreenSurface,&source,0) )
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
	RECT source, destination;
	int n;

	// マップデータから陸地をマップに描画します
	destination.left=Sprites[SpriteId.Minimap].SheetPosition.X;
	destination.top=Sprites[SpriteId.Minimap].SheetPosition.Y;

	source.left = Sprites[SpriteId.Minimap].SheetPosition.X+306;
	source.top = Sprites[SpriteId.Minimap].SheetPosition.Y;
	source.right = source.left+255;
	source.bottom = source.top+199;

	if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( OffscreenSurface, destination.left, destination.top,OffscreenSurface,&source,0) )
		{
		RestoreSurfaces();
		}

	for(var m=0; m<=179; m++) // 縦の個数	マップの枠 縦１８０ドット
		for( n=0; n<=239; n++) // 横の個数		マップの枠 横２４０ドット
			{
			if( MapTiles[m][n]!=0 )
				{
				// 陸地有り
				destination.left=Sprites[SpriteId.Minimap].SheetPosition.X+8+n-0;
				destination.top=Sprites[SpriteId.Minimap].SheetPosition.Y+8+m-0;

				source.left = Sprites[SpriteId.Minimap].SheetPosition.X+270;
				source.top = Sprites[SpriteId.Minimap].SheetPosition.Y+110;

				source.right = source.left+2;
				source.bottom = source.top+2;

				if( DDERR_SURFACELOST == IDirectDrawSurface_BltFast( OffscreenSurface, destination.left, destination.top,OffscreenSurface,&source,0) )
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
	 

	for(short e=0;e<=1;e++)
		{
		var menuItems=BufferedMenuOrders[e].Menu;
		var savedSelectedUnit=BufferedMenuOrders[e].SelectedUnit;

		// 指揮、随伴ユニットに下命
		if(menuItems!=0)
			{
			CombatMenuSelection=menuItems;
			if( menuItems==CombatMenuItem.Supply )
				{
				Units[savedSelectedUnit].SupplyTime=1;
				Units[savedSelectedUnit].Target=0;

				// ちょっと一応
				if( Units[savedSelectedUnit].Kind==UnitKind.Carrier || Units[savedSelectedUnit].Kind==UnitKind.LightCarrier )
					{
					Units[savedSelectedUnit].LandingLock=0;	// 着艦、0許可、1不許可
					Units[savedSelectedUnit].LaunchLock=0;	// その空母の次機発進許可	0許可、1不許可
					}
				if( Units[savedSelectedUnit].Kind==UnitKind.Submarine )
					Units[savedSelectedUnit].IsSubmerged=false;		// 強制浮上
				}
			else
				{
				for(short n=1;n<=MaxUnitId;n++)
					{
					ref var unit = ref Units[n];
					if( unit.IsUsed && ( unit.GroupLeader==savedSelectedUnit || n==savedSelectedUnit || Selections[e][n]!=0) )
						{
						// 決定後の書く個別の処理
						switch( unit.Kind )
							{
							case UnitKind.Fighter: case UnitKind.Attacker: case UnitKind.Bomber:
								if(  menuItems==CombatMenuItem.ArmWithTorpedo || menuItems==CombatMenuItem.ArmWithBomb || menuItems==CombatMenuItem.Disarm  )
									{
									// 1.22 arms no bomber with a torpedo (0x431D56).
									if(unit.Kind==UnitKind.Attacker || (unit.Kind==UnitKind.Bomber && ( (menuItems==CombatMenuItem.ArmWithTorpedo && unit.Side==Side.Japan && Rules.BombersDropTorpedoes ) || menuItems==CombatMenuItem.ArmWithBomb || menuItems==CombatMenuItem.Disarm) ))
										{		// 収容中の攻撃機だったばあい
										unit.Weapon=(FireKind)menuItems;

										if(menuItems==CombatMenuItem.ArmWithTorpedo)
											unit.Ammo=1;		// 魚雷の場合は常に１、弾数。
										else
											unit.Ammo=unit.MaxAmmo;

										if(menuItems==CombatMenuItem.Disarm)
											unit.ReloadTime=1;
										else
											unit.ReloadTime=RDY_SPAN;
										}
									}
								else if( Rules.MenuModesApplyToEveryPlane )
									{
									// 1.22 gives the mode to every plane, flying or not (0x431D19 to 0x431D4E).
									unit.Mode=(UnitMode)menuItems;
									if( (unit.Kind==UnitKind.Attacker || unit.Kind==UnitKind.Bomber ) && menuItems==CombatMenuItem.Return )
										{
										unit.Ammo=0;
										unit.Target=0;
										}
									if( unit.Category==UnitCategory.Plane && menuItems==CombatMenuItem.Move )
										unit.DeckPhase=0;
									}
								else
									{
									if( (unit.Kind==UnitKind.Fighter ) && menuItems==CombatMenuItem.Return  && unit.PlaneState==UnitState.Flying )
										{
										unit.Mode=(UnitMode)menuItems;
										}
									if( (unit.Kind==UnitKind.Attacker || unit.Kind==UnitKind.Bomber ) && menuItems==CombatMenuItem.Return )
										{
										unit.Ammo=0;		// 帰投選択時に攻撃機なら武装投棄
										unit.Target=0;		// 帰投選択時に攻撃機攻撃目標放棄
										unit.Mode=(UnitMode)menuItems;
										}
									if( unit.Category==UnitCategory.Plane && menuItems==CombatMenuItem.Move && unit.PlaneState==UnitState.Flying  )
										{
										unit.DeckPhase=0;		// 着艦準備をクリア
										unit.Mode=(UnitMode)menuItems;
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
