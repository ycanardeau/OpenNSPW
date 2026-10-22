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

// Port of input.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{

// The player's selection, kept with an order for the turn that gives it: the ships, then the planes, of the player's
// side, packed as the network sends them, and the selection of every unit, which a single-player game gives the order
// with (IssueSingleplayerOrders), as 1.22 does.
private void BufferSelections()
	{
	BufferedSelectionsOfAllUnits=Selections[1];
	if(LocalSide==Side.Japan)
		{
		// 日本海軍サイド
		for(var s=1;s<=JPN_SHIP_END;s++)
			{
			// 水上ユニット
			BufferedSelections[1][s-1]=Selections[1][s];
			}
		for(var s=JPN_PLANE_START;s<=JPN_PLANE_END;s++)
			{
			// 航空ユニット
			BufferedSelections[1][s-JPN_SHIP_END-1]=Selections[1][s];
			}
		}
	else
		{
		// 合衆国海軍サイド
		for(var s=USA_SHIP_START;s<=USA_SHIP_END;s++)
			{
			// 水上ユニット
			BufferedSelections[1][s-JPN_SHIP_END-1]=Selections[1][s];
			}
		for(var s=USA_PLANE_START;s<=USA_PLANE_END;s++)
			{
			// 航空ユニット
			BufferedSelections[1][s-(USA_PLANE_END/2)-1]=Selections[1][s];
			}
		}
	}

private void BufferMoveOrder(int e)
	{
	// あるマイユニットに新ＰＰ＿ＸＹが設定された場合
	// バッファに保存。これを命令をだせるタイミングにnew_ppに代入する。
	BufferedMoveOrders[1].Unit=MoveOrders[1].Unit;
	BufferedMoveOrders[1].Destination=MoveOrders[1].Destination;
	BufferedMoveOrders[1].ClearsPath=MoveOrders[1].ClearsPath;

	BufferSelections();

	int m=MoveOrders[e].Unit;
	if( Units[m].Category==UnitCategory.Plane && Units[m].PlaneState==UnitState.Parked )
		{
		Units[Units[m].Carrier].PlanesToLaunch=0;	// 空母なら これがオンで発艦中
		Units[Units[m].Carrier].LandingLock=0;	// 空母ならこの数値で甲板上の右左 (the slot of the landing lock now)
		SelectedUnit=0;
		CombatMenuKind=0;
		CombatMenuSelection=CombatMenuItem.None;
		Selections[1][m]=0;
		ClearSelection2(1);
		}
	CanOrder=false;
	HasOrdered=true;
	PlayInterfaceSound(SoundId.Click2);
	}

private void SelectFriendlyTarget(ref int m, int e)
	{
	if ( Units[SelectedUnit].Category==UnitCategory.Plane && (Units[m].Kind==UnitKind.Carrier || Units[m].Kind==UnitKind.LightCarrier || Units[m].Kind==UnitKind.AirBase))
		{
		// 航空機の格納先を指定
		if(CanOrder)
			{
			SelectedUnit=SelectOrders[e].SelectedUnit;
			m=SelectOrders[e].Unit;

			BufferedSelectOrders[1].IsSet=SelectOrders[e].IsSet;
			BufferedSelectOrders[1].SelectedUnit=SelectOrders[e].SelectedUnit;
			BufferedSelectOrders[1].Unit=SelectOrders[e].Unit;
			BufferedSelectOrders[1].GroundPosition=SelectOrders[e].GroundPosition;

			BufferSelections();

			CanOrder=false;
			HasOrdered=true;
			PlayInterfaceSound(SoundId.Click2);
			}
		}
	else
		{
		if( Selections[e][m]==0 )
			{	// ｍ番号ユニットを新規にセレクトに設定
			if(!(Units[m].Kind>=UnitKind.AirBase&&Units[m].Kind<=UnitKind.Fortress) && !(Units[SelectedUnit].Kind>=UnitKind.AirBase && Units[SelectedUnit].Kind<=UnitKind.Fortress))
				{
				SelectionCount++;
				Selections[e][m]=SelectionCount;
				}
			}
		else
			{	// ｍ番号ユニットをセレクトから外す
			SelectionCount--;
			// セレクトの設定番号を連番にする。
			for(var n=1;n<=MaxUnitId;n++)
				{
				if( Selections[e][n]>=Selections[e][m]+1 )
					Selections[e][n]--;
				}
			Selections[e][m]=0;
			}
		}
	}

private void SelectTargetOrLanding(ref int m, int e)
	{
	if( m==0 )
		{
		// 輸送船陸地を選択
		// 揚陸場所あり

		if(CanOrder)
			{
			SelectedUnit=SelectOrders[e].SelectedUnit;
			m=SelectOrders[e].Unit;

			BufferedSelectOrders[1].IsSet=SelectOrders[e].IsSet;
			BufferedSelectOrders[1].SelectedUnit=SelectOrders[e].SelectedUnit;
			BufferedSelectOrders[1].Unit=SelectOrders[e].Unit;
			BufferedSelectOrders[1].GroundPosition=SelectOrders[e].GroundPosition;

			CanOrder=false;
			HasOrdered=true;
			PlayInterfaceSound(SoundId.Click2);
			}
		}
	else
		{
		// 敵性ユニットを左クリック
		if(CanOrder)
			{
			SelectedUnit=SelectOrders[e].SelectedUnit;
			m=SelectOrders[e].Unit;

			BufferedSelectOrders[1].IsSet=SelectOrders[e].IsSet;
			BufferedSelectOrders[1].SelectedUnit=SelectOrders[e].SelectedUnit;
			BufferedSelectOrders[1].Unit=SelectOrders[e].Unit;
			BufferedSelectOrders[1].GroundPosition=SelectOrders[e].GroundPosition;

			BufferSelections();

			CanOrder=false;
			HasOrdered=true;
			PlayInterfaceSound(SoundId.Click2);

			}
		}
	}

//============================================================================
// 通信対戦用、入力データの保存、その他
//----------------------------------------------------------------------------
[Original("cnct_game_input_cont")]
public void	HandleInput()
	{
	 

	// A trace of 1.22 gives the player's orders here, or records them (docs/Verification.md).
	Trace?.ReachPlayerOrders(this);

	var e=1;
	if( SelectOrders[e].IsSet && !Units[SelectOrders[e].SelectedUnit].IsSupplying && !(Units[SelectOrders[e].SelectedUnit].Kind>=UnitKind.AirBase && Units[SelectOrders[e].SelectedUnit].Kind<=UnitKind.Fortress ))
		{
		// ユニット自体をクリックした。
		SelectedUnit=SelectOrders[e].SelectedUnit;
		int m=SelectOrders[e].Unit;

		if( m==0 || Units[SelectedUnit].Side!=Units[m].Side )
			{
			SelectTargetOrLanding(ref m, e);
			}
		else
			{
			SelectFriendlyTarget(ref m, e);
			}
		}
	else
		{
		if( CanOrder && MoveOrders[1].Unit!=0 && !Units[MoveOrders[1].Unit].IsSupplying && !(  Units[MoveOrders[1].Unit].Kind>=UnitKind.AirBase && Units[MoveOrders[1].Unit].Kind<=UnitKind.Fortress  )  && !(Units[MoveOrders[1].Unit].Category==UnitCategory.Plane && Units[MoveOrders[1].Unit].PlaneState==UnitState.Parked && Units[Units[MoveOrders[1].Unit].Carrier].Hp<=Units[Units[MoveOrders[1].Unit].Carrier].MaxHp*0.2) )
			{
			BufferMoveOrder(e);
			}
		}

	if( IsSingleplayer )
		IssueSingleplayerOrders();
	}

// Whether a position is in the world, its edges included.
private static bool IsInWorld(WorldPosition position)
	{
	return !( position.Y>MAP_TOP || position.Y<MAP_BOTTOM || position.X<MAP_LEFT || position.X>MAP_RIGHT );
	}

// The tile of the terrain under a position in the world, by its column and row, as the original finds it: offset by
// half a unit's sprite.
private (int Column, int Row) TileAt(WorldPosition position)
	{
	return ((int)((position.X+(Sprites[SpriteId.JapanUnits].Size.Width/2)-MAP_LEFT)/Sprites[SpriteId.MapTiles].Size.Width),
		(int)((MAP_TOP-position.Y+(Sprites[SpriteId.JapanUnits].Size.Height/2))/Sprites[SpriteId.MapTiles].Size.Height));
	}

// Ships: whether the world at a position is land, by the tile of the terrain there. A position outside the world is
// not.
private bool IsLandAt(WorldPosition position)
	{
	if( !IsInWorld(position) )
		return false;

	var (column, row)=TileAt(position);
	return MapTiles[row][column]>=1;
	}

// Ships: whether a point is on a ship other than unit m, one that a ship can run into: not a submerged submarine,
// nor a base.
private bool IsShipAt(int m, WorldPosition point)
	{

	for( var f=1; f<=MaxUnitId; f++)
		{
		ref var other = ref Units[f];
		if( other.IsUsed && m!=f && other.Category==UnitCategory.Ship && !(other.Kind==UnitKind.Submarine && other.IsSubmerged) && !(other.Kind>=UnitKind.AirBase && other.Kind<=UnitKind.Fortress) )
			{
			var shipArea=WorldRect.Around(other.Position, (Sprites[SpriteId.JapanUnits].Size.Width/2), (Sprites[SpriteId.JapanUnits].Size.Height/2));
			if( shipArea.Contains(point))
				return true;
			}
		}

	return false;
	}

// Ships: inserts a path point that leads around an obstacle, in `direction` from the start of the leg, before the
// leg's end. If the turn it makes from the previous leg is in the range minTurn to maxTurn, the saved point goes back,
// and the search starts again farther away (extraDistance), counting an error.
private void TakeDetour(ref Unit unit, ref int index, WorldPosition start, double checkedDistance, double divisor, Angle direction, Angle minTurn, Angle maxTurn, ref double extraDistance, WorldPosition savedPoint, double extraStep, ref int error)
	{
	unit.SetPathPoint(index+1, unit.GetPathPoint(index));
	unit.PathX[index+2]=MAP_RIGHT+1;

	var step=checkedDistance/divisor;
	if(step<40)
		step=40;
	unit.SetPathPoint(index, start+WorldVector.FromDirection(direction) * step);

	if( index>=1 )
		{
		// ＰＰ０からＰＰ１への角度
		var turn=(unit.GetPathPoint(index)-unit.GetPathPoint(index-1)).NonZero().Direction;
		turn=turn.WrapFullTurn();
		// 現位置からＰＰ０への角度
		WorldVector previousLeg;
		if( index==1)
			{
			previousLeg=unit.GetPathPoint(index-1)-unit.Position;
			}
		else
			{
			previousLeg=unit.GetPathPoint(index-1)-unit.GetPathPoint(index-2);
			}
		var previousDirection=previousLeg.NonZero().Direction;

		// 方位角 drctn1
		turn=turn-previousDirection;
		turn=turn.WrapNegative().WrapFullTurn();

		if( turn>=(minTurn) && turn<=(maxTurn) && (extraDistance<=2000) )
			{
			unit.SetPathPoint(index, savedPoint);
			unit.PathX[index+1]=MAP_RIGHT+1;

			extraDistance+=extraStep;

			index--;

			error++;

			}
		else
			{
			extraDistance=0;
			unit.SetPathPoint(index+1, savedPoint);
			unit.PathX[index+2]=MAP_RIGHT+1;
			}
		}

	}

//============================================================================
// 通信対戦用、入力データの発動
//----------------------------------------------------------------------------
[Original("set_cpu_root2")]
public void	SetCpuRoute2(int m)
	{
	ref var unit = ref Units[m];
	double		step;
	WorldPosition start;

    Array128<byte> text = default;

	var error=0;
	var divisor=2.0;
	var extraStep=160.0;

	var minTurn=90.0;
	var maxTurn=270.0;

	//	ＰＰ０が侵入不可地なら移動無しにしてリターン
	if( unit.Category==UnitCategory.Ship  )
		{
		// 他の艦船があるか
		var destination=unit.GetPathPoint(0);
		for( var n=1; n<=MaxUnitId; n++)
			{
			ref var other = ref Units[n];
			if( other.IsUsed && m!=n && other.Category==UnitCategory.Ship && !(other.Kind==UnitKind.Submarine && other.IsSubmerged)   && !(other.Kind>=UnitKind.AirBase && other.Kind<=UnitKind.Fortress )  )
				{
				// ptin dbg
				var shipArea=WorldRect.Around(other.Position, (Sprites[SpriteId.JapanUnits].Size.Width/2), (Sprites[SpriteId.JapanUnits].Size.Width/2));

				if( shipArea.Contains(destination))
					{
					// 前方に艦船！
					return;
					}
				}
			}

		// ＰＰ方向に陸地があるか
		if( IsLandAt(unit.GetPathPoint(0)) )
			return;

		}

	//
	var extraDistance=0.0;
	var savedPoint=unit.GetPathPoint(0);
	var index=0;

	while(index==0)
		{

		// 最終定点への角度と距離
		if(index==0)
			{
			start=unit.Position;
			}
		else
			{
			start=unit.GetPathPoint(index-1);
			}

		if(extraDistance!=0)
			{
			unit.SetPathPoint(index, savedPoint);
			unit.PathX[index+1]=MAP_RIGHT+1;

			var toPoint=(unit.GetPathPoint(index)-unit.Position).NonZero();

			var direction=atan2(toPoint.Y,toPoint.X)*RadiansToDegrees;

			unit.SetPathPoint(index, unit.GetPathPoint(index)+WorldVector.FromDirection(direction) * extraDistance);
			}

		var leg=(unit.GetPathPoint(index)-start).NonZero();

		var bearing=leg.Direction;
		bearing=bearing.WrapFullTurn();

		// Distance folds the direction from 0 to 360, where the original folds bearing, 360 brought to 0: both fold
		// either to 0.
		var legLength=leg.Length;

		if( unit.Category==UnitCategory.Ship )
			{
			// 艦船のルート再計算

			var hit=0;
			var checkedDistance=40.0;
			while(hit==0 && checkedDistance<=legLength)
				{
				if(index==0 && checkedDistance<=80 )
					step=1.0;
				else
					step=40.0;

				checkedDistance+=step;

				var point=start+WorldVector.FromDirection(bearing) * checkedDistance;

				// 島に接触するか？
				if( IsLandAt(point) )
					hit=1;		// 島に接触

				// 船に接触するか
				if( hit==0 && IsShipAt(m, point) )
					hit=1;		// 船に接触

				}

			if( hit==1)
				{
				// 不幸にも進路を変更しないと目標点までいけない場合。
				var leftBearing=bearing;
				var rightBearing=bearing;
				for(var n=0; n<=7; n++)
					{
					leftBearing+=22.5;
					leftBearing=leftBearing.WrapFullTurn();
					rightBearing-=22.5;
					rightBearing=rightBearing.WrapNegative();
					checkedDistance=40; hit=0;	var left=0;	var right=0;
					while( checkedDistance<=(legLength) && (left==0||right==0))
						{

						if(index==0 && checkedDistance<=80)
							step=1.0;
						else
							step=40.0;

						checkedDistance+=step;

						if(left==0)
							{
							// 左回り
							var point=start+WorldVector.FromDirection(leftBearing) * checkedDistance;
							// 島に接触するか？
							if( IsLandAt(point) )
								left=1;		// 島に接触

							// 船に接触するか
							if( IsShipAt(m, point) )
								left=1;		// 船に接触

							}
						if(right==0)
							{
							// 右回り
							var point=start+WorldVector.FromDirection(rightBearing) * checkedDistance;
							// 島に接触するか？
							if( IsLandAt(point) )
								right=1;		// 島に接触

							// 船に接触するか
							if( IsShipAt(m, point) )
								right=1;		// 船に接触
							}
						}

					if( right==0 && checkedDistance>=legLength)
						{
						TakeDetour(ref unit, ref index, start, checkedDistance, divisor, rightBearing, minTurn, maxTurn, ref extraDistance, savedPoint, extraStep, ref error);
						break;
						}

					if( left==0 && checkedDistance>=legLength)
						{
						TakeDetour(ref unit, ref index, start, checkedDistance, divisor, leftBearing, minTurn, maxTurn, ref extraDistance, savedPoint, extraStep, ref error);
						break;
						}

					}
				index++;
				}
			else
				{
				if( extraDistance==0 )
					{
					index=-1;
					}
				else
					{
					extraDistance=0;
					unit.SetPathPoint(index+1, savedPoint);
					unit.PathX[index+2]=MAP_RIGHT+1;
					index++;
					}
				}
			}
		else
			{
			// 航空機のルート再計算
			index=-1;
			}

		var length = wsprintf(text, "in Set_cpu_root2 error=%d" ,error);

		}

	return;
	}

//============================================================================
// 通信対戦用、入力データの発動
//----------------------------------------------------------------------------
[Original("cnct_game_input_now")]
public void	ApplyOrders()
	{
	int formationNumber, n; Array256<int> usedFormationNumbers = default;

	for(var e=0; e<=1; e++)
		{
		if( SelectOrders[e].IsSet && !Units[SelectOrders[e].SelectedUnit].IsSupplying && !(Units[SelectOrders[e].SelectedUnit].Kind>=UnitKind.AirBase && Units[SelectOrders[e].SelectedUnit].Kind<=UnitKind.Fortress ))
			{
			// ユニット自体をクリックした。
			SelectedUnit=SelectOrders[e].SelectedUnit;
			int m=SelectOrders[e].Unit;

			if( m==0 || Units[SelectedUnit].Side!=Units[m].Side )
				{
				if( m==0 )
					{
					// 輸送船陸地を選択
					// 揚陸場所あり
					if( Units[SelectedUnit].Target!=0 && Units[SelectedUnit].LandingX==(int)SelectOrders[e].GroundPosition.X && Units[SelectedUnit].LandingY==(int)SelectOrders[e].GroundPosition.Y)
						{
						Units[SelectedUnit].Target=0;
						}
					else
						{
						Units[SelectedUnit].Target=MaxUnitId+1;
						Units[SelectedUnit].LandingX=(int)SelectOrders[e].GroundPosition.X;		// 揚陸座標
						Units[SelectedUnit].LandingY=(int)SelectOrders[e].GroundPosition.Y;
						}
					}
				else
					{
					// 敵性ユニットを左クリック
					// NSPW NET lets fighters attack transports; 1.22, no ship (0x430618, 0x430679). NSPW NET sends no torpedo
					// against a ground unit; 1.22, against an air base or a naval base only (0x4305F9).
					if( m!=Units[SelectedUnit].Target )
						{
						if(!(Units[SelectedUnit].Weapon==FireKind.Torpedo && ( Units[m].Kind>=UnitKind.AirBase && Units[m].Kind<=Rules.LastKindRefusedToTorpedoes )) &&
							!((Units[SelectedUnit].Kind==UnitKind.Fighter && ( Units[m].Category==UnitCategory.Ship && ( Units[m].Kind!=UnitKind.Transport || !Rules.FightersAttackTransports ) ))||(Units[SelectedUnit].Kind>=UnitKind.InfantryBase&&Units[SelectedUnit].Kind<=UnitKind.Fortress)||(Units[SelectedUnit].Kind==UnitKind.Transport) )
							)
							{
							Units[SelectedUnit].Target=m;			// 攻撃対象のナンバー
							}
						}
					else
						Units[SelectedUnit].Target=0;			// 攻撃目標ユニットをなくす
					// 攻撃目標を随伴機にも指定する。
					for(n=1;n<=MaxUnitId;n++)
						{
						ref var unit = ref Units[n];
						if( unit.IsUsed && Selections[e][n]!=0 && !((unit.Kind==UnitKind.Fighter && ( Units[Units[SelectedUnit].Target].Category==UnitCategory.Ship && ( Units[Units[SelectedUnit].Target].Kind!=UnitKind.Transport || !Rules.FightersAttackTransports ) ) )||(unit.Kind>=UnitKind.InfantryBase&&unit.Kind<=UnitKind.Fortress)||(unit.Kind==UnitKind.Transport))
							)
							unit.Target=Units[SelectedUnit].Target;				//
						}
					}
				}
			else
				{
				if ( Units[SelectedUnit].Category==UnitCategory.Plane  && (Units[m].Kind==UnitKind.Carrier || Units[m].Kind==UnitKind.LightCarrier || Units[m].Kind==UnitKind.AirBase))
					{
					for(n=1;n<=MaxUnitId;n++)
						if( Units[n].IsUsed && Selections[e][n]!=0 && Units[n].Category==UnitCategory.Plane && Units[n].PlaneState==UnitState.Flying && !(Units[n].Kind==UnitKind.Bomber&&(Units[m].Kind==UnitKind.Carrier||Units[m].Kind==UnitKind.LightCarrier)) )
							Units[n].Carrier=m;					// 所属の空母、及び、基地の番号
					}
				else
					{
					}
				}
			}
		else if( MoveOrders[e].Unit!=0 && !Units[MoveOrders[e].Unit].IsSupplying && !( Units[MoveOrders[e].Unit].Kind>=UnitKind.AirBase && Units[MoveOrders[e].Unit].Kind<=UnitKind.Fortress  )  && !(Units[MoveOrders[e].Unit].Category==UnitCategory.Plane && Units[MoveOrders[e].Unit].PlaneState==UnitState.Parked && Units[Units[MoveOrders[e].Unit].Carrier].Hp<=Units[Units[MoveOrders[e].Unit].Carrier].MaxHp*0.2) )
			{
			// あるユニットに新ＰＰ＿ＸＹが設定された場合

			int m=MoveOrders[e].Unit;
			for(n=0; Units[m].PathX[n]!=MAP_RIGHT+1; n++)
				{}
			// The single-player game: the condition that NSPW NET has as a comment, || unit[m].used==cpu_side, and the
			// computer's ships' route, as 1.22 has them (docs/Singleplayer.md).
			if(MoveOrders[e].ClearsPath!=0 || (CpuSide!=Side.None && Units[m].Side==CpuSide) )
				{

				BufferedMoveOrders[e].ClearsPath=0;
				MoveOrders[e].ClearsPath=0;

				Units[m].SetPathPoint(0, MoveOrders[e].Destination);
				Units[m].PathX[1]=MAP_RIGHT+1;
				n=1;

				if( CpuSide!=Side.None && Units[m].Side==CpuSide && Units[m].Category!=UnitCategory.Plane )
					SetCpuRoute(m, 4.0);
				}
			else
				{
				if(n<64)
					{
					Units[m].SetPathPoint(n, MoveOrders[e].Destination);
					Units[m].PathX[n+1]=MAP_RIGHT+1;
					}
				}

			if( n==1 )
				{
				Units[m].GroupLeader=0;
				Units[m].IsGroupLeader=0;
				Units[m].FormationNumber=0;
				Units[m].IsStopping=false;

				// 一時的寮機の重複番号阻止
				for(var s=0;s<=MaxUnitId;s++)
					usedFormationNumbers[s]=0;

				var followers=0;
				for( var s=0; s<=MaxUnitId; s++)
					{
					ref var unit = ref Units[s];
					if( Selections[e][s]==0 && unit.GroupLeader==m )
						{
						// 今回はセレクトされなかった、元同じリーダーのユニット
						unit.GroupLeader=0;
						unit.FormationNumber=0;
						unit.DirectionToLeader=0;
						unit.DistanceToLeader=0;
						}

					if( Selections[e][s]!=0 && s!=m && !(unit.Category==UnitCategory.Plane
					   && (unit.Weapon==FireKind.Torpedo||unit.Weapon==FireKind.Bomb||unit.Weapon==FireKind.Unarmed) && unit.ReloadTime!=0 )
						 )
						{
						followers++;
						if(usedFormationNumbers[Selections[e][s]]==0)
							{	// 既に使われた番機無し
							usedFormationNumbers[Selections[e][s]]++;
							unit.FormationNumber=Selections[e][s];
							unit.IsGroupLeader=0;

							if( unit.Category==UnitCategory.Ship )
								{
								if( unit.GroupLeader!=m )
									{
									unit.GroupLeader=(short)m;
									SetShipFormation(s);
									}
								}
							else
								unit.GroupLeader=(short)m;

							SetDynamicDestination(s);
							}
						else
							{	// 既に使われた番機をつかおうとした
							for(formationNumber=1;usedFormationNumbers[formationNumber]!=0&&formationNumber<=255;formationNumber++)
								{}
							usedFormationNumbers[formationNumber]++;
							unit.FormationNumber=(short)formationNumber;
							unit.IsGroupLeader=0;

							if( unit.Category==UnitCategory.Ship )
								{
								if( unit.GroupLeader!=m )
									{
									unit.GroupLeader=(short)m;
									SetShipFormation(s);
									}
								}
							else
								unit.GroupLeader=(short)m;

							SetDynamicDestination(s);
							}
						}
					}

				if( followers>=1 )
					{
					Units[m].IsGroupLeader=(short)(followers+1);		// 小隊機数(指揮機含む)
					}

				if( Units[m].Category==UnitCategory.Plane && Units[m].PlaneState==UnitState.Parked )
					{
					Units[Units[m].Carrier].PlanesToLaunch=0;	// 空母なら これがオンで発艦中
					Units[Units[m].Carrier].LandingLock=0;	// 空母ならこの数値で甲板上の右左 (the slot of the landing lock now)
					}

				for( var s=0; s<=MaxUnitId; s++)
					{
					ref var unit = ref Units[s];
					if( unit.GroupLeader!=0 && Units[unit.GroupLeader].IsGroupLeader==0 )
						unit.GroupLeader=0;
					}
				}
			}
		}
	}

/*-------------------------------------------
	戦術マップ移動
--------------------------------------------*/
[Original("tac_map_scrl")]
public void	ScrollBattleArea(int direction)
	{
	switch(direction)
		{
		case 1:		// UP
			ScrollSpeed+=ScrollAcceleration;
			if(ScrollSpeed>=SCRN_MAX_SPD)
				ScrollSpeed=SCRN_MAX_SPD-1;
			CameraPosition += new WorldVector(0, ScrollSpeed);
			if(CameraPosition.Y>MAP_TOP)
				CameraPosition = new WorldPosition(CameraPosition.X, MAP_TOP);
			break;
		case 2:		// UP RI
			break;
		case 3:		// RI
			ScrollSpeed+=ScrollAcceleration;
			if(ScrollSpeed>=SCRN_MAX_SPD)
				ScrollSpeed=SCRN_MAX_SPD-1;
			CameraPosition += new WorldVector(ScrollSpeed, 0);
			if(CameraPosition.X>(MAP_RIGHT-CMBT_WIDTH) )
				CameraPosition = new WorldPosition(MAP_RIGHT-CMBT_WIDTH, CameraPosition.Y);
			break;
		case 4:		// RI DW
			break;
		case 5:		// DW
			ScrollSpeed+=ScrollAcceleration;
			if(ScrollSpeed>=SCRN_MAX_SPD)
				ScrollSpeed=SCRN_MAX_SPD-1;
			CameraPosition -= new WorldVector(0, ScrollSpeed);
			if(CameraPosition.Y<(MAP_BOTTOM+CMBT_HEIGHT) )
				CameraPosition = new WorldPosition(CameraPosition.X, MAP_BOTTOM+CMBT_HEIGHT);
			break;
		case 6:		// DW LF
			break;
		case 7:		// LF
			ScrollSpeed+=ScrollAcceleration;
			if(ScrollSpeed>=SCRN_MAX_SPD)
				ScrollSpeed=SCRN_MAX_SPD-1;
			CameraPosition -= new WorldVector(ScrollSpeed, 0);
			if(CameraPosition.X<MAP_LEFT)
				CameraPosition = new WorldPosition(MAP_LEFT, CameraPosition.Y);
			break;
		case 8:		// LF UP
			break;
		}
	}

private void ReadMouse()
	{

	// バッファリング・データを取得する
	while(IsAppActive)
		{
		DIDEVICEOBJECTDATA od;
		uint dwItems = 1;
		var hr = pDIDeviceMouse.GetDeviceData((uint)(sizeof(DIDEVICEOBJECTDATA)),
							&od, &dwItems, 0);
		if (hr==DIERR_INPUTLOST)
			pDIDeviceMouse.Acquire();
		else if (FAILED(hr) || dwItems == 0)
	            break;	// データが読めないか、存在しない
		else
			{
			switch (od.dwOfs)
				{

				// 左ボタンが押された、または離された。
				case DIMOFS_BUTTON0:
					if( od.dwData!=0 )
						{
						if( LeftButton==0 )
							LeftButton=1;
						}
					else
						{
						if( LeftButton!=0 )
							LeftButton=3;
						}
					break;

				// 右ボタンが押された、または離された。
				case DIMOFS_BUTTON1:
					if( od.dwData!=0 )
						{
						if( RightButton==0 )
							RightButton=1;
						}
					else
						{
						if( RightButton!=0 )
							RightButton=3;
						}
					break;
/*
				// 中ボタンが押された、または離された。
				case DIMOFS_BUTTON2:
					if( od.dwData )
						key_cndtn|=MS_C_BTN;
					else
						{
						key_cndtn&=~MS_C_BTN;
						key_cndtn&=~MS_C_BTN2;
						}
					break;

				// ５ボタン（右側面）が押された、または離された。
				case DIMOFS_BUTTON4:
					if (od.dwData & (0x80) )
						key_cndtn|=UP_BTN;
					else
						key_cndtn&=~UP_BTN;
					break;

				// ４ボタン（左側面）が押された、または離された。
				case DIMOFS_BUTTON3:
					if (od.dwData & (0x80) )
						key_cndtn|=DOWN_BTN;
					else
						key_cndtn&=~DOWN_BTN;
					break;
*/
				}
			}
		}
	}

private void ReadKeyboard()
	{
	int y = 0;

	// バッファリング・データを取得する
	while(IsAppActive)
		{
		DIDEVICEOBJECTDATA od;
		uint dwItems = 1;
		var hr = pDIDevice.GetDeviceData((uint)(sizeof(DIDEVICEOBJECTDATA)),
							&od, &dwItems, 0);
		if (hr==DIERR_INPUTLOST)
			pDIDevice.Acquire();
		else if (FAILED(hr) || dwItems == 0)
	            break;	// データが読めないか、存在しない
		else
			{

			switch (od.dwOfs)
				{

				case DIK_W:
					if ((od.dwData & (0x80))!=0 )
						Buttons|=InputButtons.Front;
					else
						Buttons&=~InputButtons.Front;
					break;

				case DIK_S:
					if ((od.dwData & (0x80))!=0 )
						Buttons|=InputButtons.Back;
					else
						Buttons&=~InputButtons.Back;
					break;

				case DIK_D:
					if ((od.dwData & (0x80))!=0 )
						Buttons|=InputButtons.Right;
					else
						Buttons&=~InputButtons.Right;
					break;

				case DIK_A:
					if ((od.dwData & (0x80))!=0 )
						Buttons|=InputButtons.Left;
					else
						Buttons&=~InputButtons.Left;
					break;

				case DIK_SPACE:
					if ((od.dwData & (0x80))!=0 )
						Buttons|=InputButtons.Space;
					else
						Buttons&=~InputButtons.Space;
					break;

				case DIK_F:
					if( Mode==GameMode.Battle && (od.dwData & (0x80))!=0 )
						{
						if(GameSpeed<2)
							GameSpeed++;
						else if(GameSpeed==2)
							GameSpeed=4;
						else if(GameSpeed<300)
							GameSpeed+=2;
						}
					break;

				case DIK_G:
					if( Mode==GameMode.Battle && (od.dwData & (0x80))!=0 )
						{
						if(GameSpeed<=1)
							GameSpeed=0;
						else if(GameSpeed<=2)
							GameSpeed=1;
						else if(GameSpeed==4)
							GameSpeed=2;
						else if(GameSpeed!=0)
							GameSpeed-=2;
						}

					break;

				case DIK_R:
					if( Mode==GameMode.Battle && (od.dwData & (0x80))!=0 )
						{
						GameSpeed=1;
						}
					break;

				case DIK_RETURN:

					if( !IsEditingMap && hwndChatDlg==null && (od.dwData & (0x80))!=0 )
						{
						hwndChatDlg = CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_CHAT_DIALOG), hwndApp, ChatDlgProc);

						}
					break;

				}
			}
		}
	}

/*-------------------------------------------
	入力フェッチ
--------------------------------------------*/
[Original("get_input")]
public void ReadInput()
	{

	int	isScrolling=0;

	// キーボードの入力チェック
	if (pDIDevice!=null)
		{
		ReadKeyboard();
		}

	isScrolling=0;

	if( CursorPosition.y<=0 || Buttons.HasFlag(InputButtons.Front) )
		{
		ScrollBattleArea(1);
		isScrolling=1;
		}

	if( CursorPosition.y>=SCRN_HEIGHT-1 || Buttons.HasFlag(InputButtons.Back) )
		{
		ScrollBattleArea(5);
		isScrolling=1;
		}

	if( CursorPosition.x>=SCRN_WIDTH-1 || Buttons.HasFlag(InputButtons.Right) )
		{
		ScrollBattleArea(3);
		isScrolling=1;
		}

	if( CursorPosition.x<=0 || Buttons.HasFlag(InputButtons.Left))
		{
		ScrollBattleArea(7);
		isScrolling=1;
		}

	if(isScrolling==0)
		{
		ScrollSpeed=0;
		}

	// マウスボタンの状況記録

	// マウス
	if (pDIDeviceMouse!=null)
		{

		ReadMouse();
		}

	// 純粋なマウス位置を取る
	GetCursorPos(ref CursorPosition);
	if( !IsFullscreen )
		ScreenToClient(hwndApp, ref CursorPosition);

	}

/*-------------------------------------------
	DirectInput 初期化
---------------------------------------------*/

[Original("InitDInput")]
public bool InitializeDirectInput()
{
	DIPROPDWORD diprop;

	// *****************************************
	// DirectInputの作成
	var hr = DirectInput8Create( hInstApp , DIRECTINPUT_VERSION,
							IID_IDirectInput8, out pDInput, null);
	if (FAILED(hr))
	{
		DXTRACE_ERR("DirectInput8オブジェクトの作成に失敗", hr);
		return false;
	}

	//*** キーボード
	// デバイス・オブジェクトを作成
	hr = pDInput.CreateDevice(GUID_SysKeyboard, out pDIDevice, null);
	if (FAILED(hr)) {
		DXTRACE_ERR("DirectInputDevice8オブジェクトの作成に失敗", hr);
	    return false;
	}

	// データ形式を設定
	hr = pDIDevice.SetDataFormat(c_dfDIKeyboard);
	if (FAILED(hr))
	{
		DXTRACE_ERR("c_dfDIMouse2形式の設定に失敗", hr);
		return false;
	}

	//モードを設定（フォアグラウンド＆非排他モード）
	hr = pDIDevice.SetCooperativeLevel(hwndApp, DISCL_NONEXCLUSIVE | DISCL_FOREGROUND);
	if (FAILED(hr))
	{
		DXTRACE_ERR("フォアグラウンド＆非排他モードの設定に失敗", hr);
		return false;
	}

	// バッファリング・データを取得するため、バッファ・サイズを設定
	diprop.diph.dwSize	= (uint)(sizeof(DIPROPDWORD));
	diprop.diph.dwHeaderSize	= (uint)sizeof(DIPROPHEADER);
	diprop.diph.dwObj	= 0;
	diprop.diph.dwHow	= DIPH_DEVICE;
	diprop.dwData = DIDEVICE_BUFFERSIZE;
	hr = pDIDevice.SetProperty(DIPROP_BUFFERSIZE, &diprop.diph);
	if (FAILED(hr))
	{
		DXTRACE_ERR("バッファ・サイズの設定に失敗", hr);
		return false;
	}

	// 入力制御開始
	pDIDevice.Acquire();

	//*** マウス
	// デバイス・オブジェクトを作成
	hr = pDInput.CreateDevice(GUID_SysMouse, out pDIDeviceMouse, null);
	if (FAILED(hr))
		{
		DXTRACE_ERR("DirectInputDevice8オブジェクトの作成に失敗", hr);
	    return false;
		}

	// データ形式を設定
	hr = pDIDeviceMouse.SetDataFormat(c_dfDIMouse2);
	if (FAILED(hr))
	{
		DXTRACE_ERR("c_dfDIMouse2形式の設定に失敗", hr);
		return false;
	}

	//モードを設定（フォアグラウンド＆非排他モード）
	hr = pDIDeviceMouse.SetCooperativeLevel(hwndApp, DISCL_NONEXCLUSIVE | DISCL_FOREGROUND);
	if (FAILED(hr))
	{
		DXTRACE_ERR("フォアグラウンド＆非排他モードの設定に失敗", hr);
		return false;
	}

	// 軸モードを設定（相対値モードに設定）
	diprop.diph.dwSize	= (uint)(sizeof(DIPROPDWORD));
	diprop.diph.dwHeaderSize	= (uint)sizeof(DIPROPHEADER);
	diprop.diph.dwObj	= 0;
	diprop.diph.dwHow	= DIPH_DEVICE;
	diprop.dwData		= DIPROPAXISMODE_REL;
//	diprop.dwData		= DIPROPAXISMODE_ABS;	// 絶対値モードの場合
	hr = pDIDeviceMouse.SetProperty(DIPROP_AXISMODE, &diprop.diph);
	if (FAILED(hr))
	{
		DXTRACE_ERR("軸モードの設定に失敗", hr);
		return false;
	}

	// バッファリング・データを取得するため、バッファ・サイズを設定
	diprop.dwData = DIDEVICE_BUFFERSIZE;
	hr = pDIDeviceMouse.SetProperty(DIPROP_BUFFERSIZE, &diprop.diph);
	if (FAILED(hr))
	{
		DXTRACE_ERR("バッファ・サイズの設定に失敗", hr);
		return false;
	}

	// 入力制御開始
	pDIDeviceMouse.Acquire();

	return true;
	}
}
