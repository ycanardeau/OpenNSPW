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

//============================================================================
// 通信対戦用、入力データの保存、その他
//----------------------------------------------------------------------------
[Original("cnct_game_input_cont")]
public void	HandleInput()
	{
	int	h,m,f,s,n,e; Array256<int> chk = default;

	e=1;
	if( SelectOrders[e].IsSet!=0 && Units[SelectOrders[e].SelectedUnit].Supply==0 && !(Units[SelectOrders[e].SelectedUnit].Kind>=UnitKind.AirBase && Units[SelectOrders[e].SelectedUnit].Kind<=UnitKind.Fortress ))
		{
		// ユニット自体をクリックした。
		SelectedUnit=SelectOrders[e].SelectedUnit;
		m=SelectOrders[e].Unit;

		if( m==0 || Units[SelectedUnit].Side!=Units[m].Side )
			{
			if( m==0 )
				{
				// 輸送船陸地を選択
				// 揚陸場所あり

				if(CanOrder!=0)
					{
					SelectedUnit=SelectOrders[e].SelectedUnit;
					m=SelectOrders[e].Unit;

					BufferedSelectOrders[1].IsSet=SelectOrders[e].IsSet;
					BufferedSelectOrders[1].SelectedUnit=SelectOrders[e].SelectedUnit;
					BufferedSelectOrders[1].Unit=SelectOrders[e].Unit;
					BufferedSelectOrders[1].GroundPosition=SelectOrders[e].GroundPosition;

					CanOrder=0;
					HasOrdered=1;
					PlaySoundEffect( 0, CLICK2 ,(double)(MAP_RIGHT+1), 0);
					}
				}
			else
				{
				// 敵性ユニットを左クリック
				if(CanOrder!=0)
					{
					SelectedUnit=SelectOrders[e].SelectedUnit;
					m=SelectOrders[e].Unit;

					BufferedSelectOrders[1].IsSet=SelectOrders[e].IsSet;
					BufferedSelectOrders[1].SelectedUnit=SelectOrders[e].SelectedUnit;
					BufferedSelectOrders[1].Unit=SelectOrders[e].Unit;
					BufferedSelectOrders[1].GroundPosition=SelectOrders[e].GroundPosition;

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
					PlaySoundEffect( 0, CLICK2 ,(double)(MAP_RIGHT+1), 0);

					}
				}
			}
		else
			{
			if ( Units[SelectedUnit].Category==UnitCategory.Plane && (Units[m].Kind==UnitKind.Carrier || Units[m].Kind==UnitKind.LightCarrier || Units[m].Kind==UnitKind.AirBase))
				{
				// 航空機の格納先を指定
				if(CanOrder!=0)
					{
					SelectedUnit=SelectOrders[e].SelectedUnit;
					m=SelectOrders[e].Unit;

					BufferedSelectOrders[1].IsSet=SelectOrders[e].IsSet;
					BufferedSelectOrders[1].SelectedUnit=SelectOrders[e].SelectedUnit;
					BufferedSelectOrders[1].Unit=SelectOrders[e].Unit;
					BufferedSelectOrders[1].GroundPosition=SelectOrders[e].GroundPosition;

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
					PlaySoundEffect( 0, CLICK2 ,(double)(MAP_RIGHT+1), 0);
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
					for(n=1;n<=MaxUnitId;n++)
						{
						if( Selections[e][n]>=Selections[e][m]+1 )
							Selections[e][n]--;
						}
					Selections[e][m]=0;
					}
				}
			}
		}
	else
		{
		if( CanOrder!=0 && MoveOrders[1].Unit!=0 && Units[MoveOrders[1].Unit].Supply==0 && !(  Units[MoveOrders[1].Unit].Kind>=UnitKind.AirBase && Units[MoveOrders[1].Unit].Kind<=UnitKind.Fortress  )  && !(Units[MoveOrders[1].Unit].Category==UnitCategory.Plane && Units[MoveOrders[1].Unit].PlaneState==UnitState.Parked && Units[Units[MoveOrders[1].Unit].Carrier].Hp<=Units[Units[MoveOrders[1].Unit].Carrier].MaxHp*0.2) )
			{
			// あるマイユニットに新ＰＰ＿ＸＹが設定された場合
			// バッファに保存。これを命令をだせるタイミングにnew_ppに代入する。
			BufferedMoveOrders[1].Unit=MoveOrders[1].Unit;
			BufferedMoveOrders[1].Destination=MoveOrders[1].Destination;
			BufferedMoveOrders[1].ClearsPath=MoveOrders[1].ClearsPath;

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

			m=MoveOrders[e].Unit;
			if( Units[m].Category==UnitCategory.Plane && Units[m].PlaneState==UnitState.Parked )
				{
				Units[Units[m].Carrier].info[4]=0;	// 空母なら これがオンで発艦中
				Units[Units[m].Carrier].info[7]=0;	// 空母ならこの数値で甲板上の右左
				SelectedUnit=0;
				CombatMenuKind=0;
				CombatMenuSelection=CombatMenuItem.None;
				Selections[1][m]=0;
				ClearSelection2(1);
				}
			CanOrder=0;
			HasOrdered=1;
			PlaySoundEffect( 0, CLICK2 ,(double)(MAP_RIGHT+1), 0);
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
	double		add;
	double	pp_drctn,wrk_x,wrk_y,add_drctn,pp_dstc,chk_dstc,wrk,drctn1,drctn2,wrk_x2,wrk_y2,start_x,start_y,min_dstc,s_pp_x,s_pp_y,first_drctn,div,drctn_ok1,drctn_ok2,re_add; Array16<Array2<double>> rslt_drctn = default;
	int			n,hit,cm_scrn_x,cm_scrn_y,left,right,pp_indx,error,f,i;
	RECT		wrk_r;

    Array128<byte> ach = default;
    int len;

	error=0;
	div=2.0;
	re_add=160;

	drctn_ok1=90.0;
	drctn_ok2=270.0;

	//	ＰＰ０が侵入不可地なら移動無しにしてリターン
	if( unit.Category==UnitCategory.Ship  )
		{
		// 他の艦船があるか
		wrk_x2=unit.PathX[0];
		wrk_y2=unit.PathY[0];
		for( n=1; n<=MaxUnitId; n++)
			{
			ref var other = ref Units[n];
			if( other.IsUsed && m!=n && other.Category==UnitCategory.Ship && !(other.Kind==UnitKind.Submarine && other.info[6]!=0)   && !(other.Kind>=UnitKind.AirBase && other.Kind<=UnitKind.Fortress )  )
				{
				// ptin dbg
				wrk_r.top=(int)other.Position.Y+(Sprites[UNIT_JPN].wd/2);
				wrk_r.right=(int)other.Position.X+(Sprites[UNIT_JPN].wd/2);
				wrk_r.bottom=(int)other.Position.Y-(Sprites[UNIT_JPN].wd/2);
				wrk_r.left=(int)other.Position.X-(Sprites[UNIT_JPN].wd/2);

				if( PointInRect3(ref wrk_r,(int)wrk_x2,(int)wrk_y2)!=0)
					{
					// 前方に艦船！
					return;
					}
				}
			}

		// ＰＰ方向に陸地があるか
		wrk_x2=unit.PathX[0];
		wrk_y2=unit.PathY[0];
		if(!( wrk_y2>MAP_TOP || wrk_y2<MAP_BOTTOM || wrk_x2<MAP_LEFT || wrk_x2>MAP_RIGHT ))
			{
			cm_scrn_x=(int)((wrk_x2+(Sprites[UNIT_JPN].wd/2)-MAP_LEFT)/Sprites[MAP_TIP_NRML].wd);
			cm_scrn_y=(int)((MAP_TOP-wrk_y2+(Sprites[UNIT_JPN].ht/2))/Sprites[MAP_TIP_NRML].ht);
			if( MapTiles[cm_scrn_y][cm_scrn_x]>=1)
				{
				return;
				}
			}

		}

	//
	min_dstc=0;
	s_pp_x=unit.PathX[0];
	s_pp_y=unit.PathY[0];
	pp_indx=0;

	while(pp_indx==0)
		{

		// 最終定点への角度と距離
		if(pp_indx==0)
			{
			start_x=unit.Position.X;
			start_y=unit.Position.Y;
			}
		else
			{
			start_x=unit.PathX[pp_indx-1];
			start_y=unit.PathY[pp_indx-1];
			}

		if(min_dstc!=0)
			{
			unit.PathX[pp_indx]=s_pp_x;
			unit.PathY[pp_indx]=s_pp_y;
			unit.PathX[pp_indx+1]=MAP_RIGHT+1;

			wrk_x=unit.PathX[pp_indx]-unit.Position.X;
			wrk_y=unit.PathY[pp_indx]-unit.Position.Y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;

			pp_drctn=atan2(wrk_y,wrk_x)*RAD_to;

			unit.PathX[pp_indx]+=cos(pp_drctn*a_PI)*(min_dstc);
			unit.PathY[pp_indx]+=sin(pp_drctn*a_PI)*(min_dstc);
			}

		wrk_x=unit.PathX[pp_indx]-start_x;
		wrk_y=unit.PathY[pp_indx]-start_y;
		if(wrk_x==0)	wrk_x=1;
		if(wrk_y==0)	wrk_y=1;

		pp_drctn=atan2(wrk_y,wrk_x)*RAD_to;
		if(pp_drctn<0)
			pp_drctn=360+pp_drctn;
		if(pp_drctn>=360)
			pp_drctn=pp_drctn-360;

		if(wrk_x<0)
			wrk_x=0-wrk_x;
		if(wrk_y<0)
			wrk_y=0-wrk_y;
		wrk=pp_drctn;
		if(wrk>=180)
			wrk=wrk-180;
		if(wrk>=90)
			wrk=90-(wrk-90);
		pp_dstc=(wrk_x)/(cos(wrk*a_PI));

		if( unit.Category==UnitCategory.Ship )
			{
			// 艦船のルート再計算

			hit=0;
			chk_dstc=40;
			while(hit==0 && chk_dstc<=pp_dstc)
				{
				if(pp_indx==0 && chk_dstc<=80 )
					add=1.0;
				else
					add=40.0;

				chk_dstc+=add;

				wrk_x=start_x;
				wrk_y=start_y;
				wrk_x+=cos(pp_drctn*a_PI)*(chk_dstc);
				wrk_y+=sin(pp_drctn*a_PI)*(chk_dstc);

				// 島に接触するか？
				if(!( wrk_y>MAP_TOP || wrk_y<MAP_BOTTOM || wrk_x<MAP_LEFT || wrk_x>MAP_RIGHT ))
					{
					cm_scrn_x=(int)((wrk_x+(Sprites[UNIT_JPN].wd/2)-MAP_LEFT)/Sprites[MAP_TIP_NRML].wd);
					cm_scrn_y=(int)((MAP_TOP-wrk_y+(Sprites[UNIT_JPN].ht/2))/Sprites[MAP_TIP_NRML].ht);
					if( MapTiles[cm_scrn_y][cm_scrn_x]>=1)
						{
						hit=1;		// 島に接触
						}
					}

				// 船に接触するか
				for( f=1; f<=MaxUnitId && hit==0 ; f++)
					{
					if( Units[f].IsUsed && m!=f && Units[f].Category==UnitCategory.Ship && !(Units[f].Kind==UnitKind.Submarine && Units[f].info[6]!=0) && !(Units[f].Kind>=UnitKind.AirBase&&Units[f].Kind<=UnitKind.Fortress) )
						{
						// ptin dbg
						wrk_r.top=(int)Units[f].Position.Y+(Sprites[UNIT_JPN].ht/2);
						wrk_r.right=(int)Units[f].Position.X+(Sprites[UNIT_JPN].wd/2);
						wrk_r.bottom=(int)Units[f].Position.Y-(Sprites[UNIT_JPN].ht/2);
						wrk_r.left=(int)Units[f].Position.X-(Sprites[UNIT_JPN].wd/2);
						if( PointInRect3(ref wrk_r,(int)wrk_x,(int)wrk_y)!=0)
							{
							hit=1;		// 船に接触
							}
						}
					}

				}

			if( hit==1)
				{
				// 不幸にも進路を変更しないと目標点までいけない場合。
				drctn1=pp_drctn;
				drctn2=pp_drctn;
				for(n=0; n<=7; n++)
					{
					drctn1+=22.5;
					if(drctn1>=360)		drctn1=drctn1-360;
					drctn2-=22.5;
					if(drctn2<0)		drctn2=360+drctn2;
					chk_dstc=40; hit=0;	left=0;	right=0;
					while( chk_dstc<=(pp_dstc) && (left==0||right==0))
						{

						if(pp_indx==0 && chk_dstc<=80)
							add=1.0;
						else
							add=40.0;

						chk_dstc+=add;

						if(left==0)
							{
							// 左回り
							wrk_x=start_x;
							wrk_y=start_y;
							wrk_x+=cos(drctn1*a_PI)*(chk_dstc);
							wrk_y+=sin(drctn1*a_PI)*(chk_dstc);
							// 島に接触するか？
							if(!( wrk_y>MAP_TOP || wrk_y<MAP_BOTTOM || wrk_x<MAP_LEFT || wrk_x>MAP_RIGHT ))
								{
								cm_scrn_x=(int)((wrk_x+(Sprites[UNIT_JPN].wd/2)-MAP_LEFT)/Sprites[MAP_TIP_NRML].wd);
								cm_scrn_y=(int)((MAP_TOP-wrk_y+(Sprites[UNIT_JPN].ht/2))/Sprites[MAP_TIP_NRML].ht);
								if( MapTiles[cm_scrn_y][cm_scrn_x]>=1)
									{
									left=1;		// 島に接触
									}
								}

							// 船に接触するか
							for( f=1; f<=MaxUnitId; f++)
								{
								ref var other = ref Units[f];
								if( other.IsUsed && m!=f && other.Category==UnitCategory.Ship && !(other.Kind==UnitKind.Submarine && other.info[6]!=0) && !(other.Kind>=UnitKind.AirBase && other.Kind<=UnitKind.Fortress)  )
									{
									// ptin dbg
									wrk_r.top=(int)other.Position.Y+(Sprites[UNIT_JPN].ht/2);
									wrk_r.right=(int)other.Position.X+(Sprites[UNIT_JPN].wd/2);
									wrk_r.bottom=(int)other.Position.Y-(Sprites[UNIT_JPN].ht/2);
									wrk_r.left=(int)other.Position.X-(Sprites[UNIT_JPN].wd/2);
									if( PointInRect3(ref wrk_r,(int)wrk_x,(int)wrk_y)!=0)
										{
										left=1;		// 船に接触
										break;
										}
									}
								}

							}
						if(right==0)
							{
							// 右回り
							wrk_x2=start_x;
							wrk_y2=start_y;
							wrk_x2+=cos(drctn2*a_PI)*(chk_dstc);
							wrk_y2+=sin(drctn2*a_PI)*(chk_dstc);
							// 島に接触するか？
							if(!( wrk_y2>MAP_TOP || wrk_y2<MAP_BOTTOM || wrk_x2<MAP_LEFT || wrk_x2>MAP_RIGHT ))
								{
								cm_scrn_x=(int)((wrk_x2+(Sprites[UNIT_JPN].wd/2)-MAP_LEFT)/Sprites[MAP_TIP_NRML].wd);
								cm_scrn_y=(int)((MAP_TOP-wrk_y2+(Sprites[UNIT_JPN].ht/2))/Sprites[MAP_TIP_NRML].ht);
								if( MapTiles[cm_scrn_y][cm_scrn_x]>=1)
									{
									right=1;		// 島に接触
									}
								}

							// 船に接触するか
							for( f=1; f<=MaxUnitId; f++)
								{
								ref var other = ref Units[f];
								if( other.IsUsed && m!=f && other.Category==UnitCategory.Ship && !(other.Kind==UnitKind.Submarine && other.info[6]!=0) && !(other.Kind>=UnitKind.AirBase && other.Kind<=UnitKind.Fortress ) )
									{
									// ptin dbg
									wrk_r.top=(int)other.Position.Y+(Sprites[UNIT_JPN].ht/2);
									wrk_r.right=(int)other.Position.X+(Sprites[UNIT_JPN].wd/2);
									wrk_r.bottom=(int)other.Position.Y-(Sprites[UNIT_JPN].ht/2);
									wrk_r.left=(int)other.Position.X-(Sprites[UNIT_JPN].wd/2);
									if( PointInRect3(ref wrk_r,(int)wrk_x2,(int)wrk_y2)!=0)
										{
										right=1;		// 船に接触
										break;
										}
									}
								}
							}
						}

					if( right==0 && chk_dstc>=pp_dstc)
						{
						unit.PathX[pp_indx+1]=unit.PathX[pp_indx];
						unit.PathY[pp_indx+1]=unit.PathY[pp_indx];
						unit.PathX[pp_indx+2]=MAP_RIGHT+1;
						wrk_x2=start_x;
						wrk_y2=start_y;

						wrk=chk_dstc/div;
						if(wrk<40)
							wrk=40;
						wrk_x2+=cos(drctn2*a_PI)*wrk;
						wrk_y2+=sin(drctn2*a_PI)*wrk;
						unit.PathX[pp_indx]=wrk_x2;
						unit.PathY[pp_indx]=wrk_y2;

						if( pp_indx>=1 )
							{
							// ＰＰ０からＰＰ１への角度
							wrk_x=unit.PathX[pp_indx]-unit.PathX[pp_indx-1];
							wrk_y=unit.PathY[pp_indx]-unit.PathY[pp_indx-1];
							if(wrk_x==0)	wrk_x=1;
							if(wrk_y==0)	wrk_y=1;
							drctn1=atan2(wrk_y,wrk_x)*RAD_to;
							if(drctn1<0)
								drctn1=360+drctn1;
							if(drctn1>=360)
								drctn1=drctn1-360;
							// 現位置からＰＰ０への角度
							if( pp_indx==1)
								{
								wrk_x=unit.PathX[pp_indx-1]-unit.Position.X;
								wrk_y=unit.PathY[pp_indx-1]-unit.Position.Y;
								}
							else
								{
								wrk_x=unit.PathX[pp_indx-1]-unit.PathX[pp_indx-2];
								wrk_y=unit.PathY[pp_indx-1]-unit.PathY[pp_indx-2];
								}
							if(wrk_x==0)	wrk_x=1;
							if(wrk_y==0)	wrk_y=1;
							drctn2=atan2(wrk_y,wrk_x)*RAD_to;
							if(drctn2<0)
								drctn2=360+drctn2;

							// 方位角 drctn1
							drctn1=drctn1-drctn2;
							if(drctn1<0)
								drctn1=360+drctn1;
							if(drctn1>=360)
								drctn1=drctn1-360;

							if( drctn1>=(drctn_ok1) && drctn1<=(drctn_ok2) && (min_dstc<=2000))
								{
								unit.PathX[pp_indx]=s_pp_x;
								unit.PathY[pp_indx]=s_pp_y;
								unit.PathX[pp_indx+1]=MAP_RIGHT+1;

								min_dstc+=re_add;

								pp_indx--;

								error++;

								}
							else
								{
								min_dstc=0;
								unit.PathX[pp_indx+1]=s_pp_x;
								unit.PathY[pp_indx+1]=s_pp_y;
								unit.PathX[pp_indx+2]=MAP_RIGHT+1;
								}
							}

						break;
						}

					if( left==0 && chk_dstc>=pp_dstc)
						{
						unit.PathX[pp_indx+1]=unit.PathX[pp_indx];
						unit.PathY[pp_indx+1]=unit.PathY[pp_indx];
						unit.PathX[pp_indx+2]=MAP_RIGHT+1;
						wrk_x=start_x;
						wrk_y=start_y;

						wrk=chk_dstc/div;
						if(wrk<40)
							wrk=40;
						wrk_x+=cos(drctn1*a_PI)*wrk;
						wrk_y+=sin(drctn1*a_PI)*wrk;
						unit.PathX[pp_indx]=wrk_x;
						unit.PathY[pp_indx]=wrk_y;

						if( pp_indx>=1 )
							{
							// ＰＰ０からＰＰ１への角度
							wrk_x=unit.PathX[pp_indx]-unit.PathX[pp_indx-1];
							wrk_y=unit.PathY[pp_indx]-unit.PathY[pp_indx-1];
							if(wrk_x==0)	wrk_x=1;
							if(wrk_y==0)	wrk_y=1;
							drctn1=atan2(wrk_y,wrk_x)*RAD_to;
							if(drctn1<0)
								drctn1=360+drctn1;
							if(drctn1>=360)
								drctn1=drctn1-360;
							// 現位置からＰＰ０への角度
							if( pp_indx==1)
								{
								wrk_x=unit.PathX[pp_indx-1]-unit.Position.X;
								wrk_y=unit.PathY[pp_indx-1]-unit.Position.Y;
								}
							else
								{
								wrk_x=unit.PathX[pp_indx-1]-unit.PathX[pp_indx-2];
								wrk_y=unit.PathY[pp_indx-1]-unit.PathY[pp_indx-2];
								}
							if(wrk_x==0)	wrk_x=1;
							if(wrk_y==0)	wrk_y=1;
							drctn2=atan2(wrk_y,wrk_x)*RAD_to;
							if(drctn2<0)
								drctn2=360+drctn2;

							// 方位角 drctn1
							drctn1=drctn1-drctn2;
							if(drctn1<0)
								drctn1=360+drctn1;
							if(drctn1>=360)
								drctn1=drctn1-360;

							if( drctn1>=(drctn_ok1) && drctn1<=(drctn_ok2) && (min_dstc<=2000) )
								{
								unit.PathX[pp_indx]=s_pp_x;
								unit.PathY[pp_indx]=s_pp_y;
								unit.PathX[pp_indx+1]=MAP_RIGHT+1;

								min_dstc+=re_add;

								pp_indx--;

								error++;

								}
							else
								{
								min_dstc=0;
								unit.PathX[pp_indx+1]=s_pp_x;
								unit.PathY[pp_indx+1]=s_pp_y;
								unit.PathX[pp_indx+2]=MAP_RIGHT+1;
								}
							}

						break;
						}

					}
				pp_indx++;
				}
			else
				{
				if( min_dstc==0 )
					{
					pp_indx=-1;
					}
				else
					{
					min_dstc=0;
					unit.PathX[pp_indx+1]=s_pp_x;
					unit.PathY[pp_indx+1]=s_pp_y;
					unit.PathX[pp_indx+2]=MAP_RIGHT+1;
					pp_indx++;
					}
				}
			}
		else
			{
			// 航空機のルート再計算
			pp_indx=-1;
			}

		len = wsprintf(ach, "in Set_cpu_root2 error=%d" ,error);

		}

	return;
	}

//============================================================================
// 通信対戦用、入力データの発動
//----------------------------------------------------------------------------
[Original("cnct_game_input_now")]
public void	ApplyOrders()
	{
	int	h,m,f,s,n,e; Array256<int> chk = default;

	for(e=0; e<=1; e++)
		{
		if( SelectOrders[e].IsSet!=0 && Units[SelectOrders[e].SelectedUnit].Supply==0 && !(Units[SelectOrders[e].SelectedUnit].Kind>=UnitKind.AirBase && Units[SelectOrders[e].SelectedUnit].Kind<=UnitKind.Fortress ))
			{
			// ユニット自体をクリックした。
			SelectedUnit=SelectOrders[e].SelectedUnit;
			m=SelectOrders[e].Unit;

			if( m==0 || Units[SelectedUnit].Side!=Units[m].Side )
				{
				if( m==0 )
					{
					// 輸送船陸地を選択
					// 揚陸場所あり
					if( Units[SelectedUnit].Target!=0 && Units[SelectedUnit].info[6]==(int)SelectOrders[e].GroundPosition.X && Units[SelectedUnit].info[7]==(int)SelectOrders[e].GroundPosition.Y)
						{
						Units[SelectedUnit].Target=0;
						}
					else
						{
						Units[SelectedUnit].Target=MaxUnitId+1;
						Units[SelectedUnit].info[6]=(int)SelectOrders[e].GroundPosition.X;		// 揚陸座標
						Units[SelectedUnit].info[7]=(int)SelectOrders[e].GroundPosition.Y;
						}
					}
				else
					{
					// 敵性ユニットを左クリック
					if( m!=Units[SelectedUnit].Target )
						{
						if(!(Units[SelectedUnit].Weapon==FireKind.Torpedo && ( Units[m].Kind>=UnitKind.AirBase && Units[m].Kind<=UnitKind.Fortress )) &&
							!((Units[SelectedUnit].Kind==UnitKind.Fighter && ( Units[m].Category==UnitCategory.Ship && Units[m].Kind!=UnitKind.Transport ))||(Units[SelectedUnit].Kind>=UnitKind.InfantryBase&&Units[SelectedUnit].Kind<=UnitKind.Fortress)||(Units[SelectedUnit].Kind==UnitKind.Transport) )
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
						if( unit.IsUsed && Selections[e][n]!=0 && !((unit.Kind==UnitKind.Fighter && ( Units[Units[SelectedUnit].Target].Category==UnitCategory.Ship &&  Units[Units[SelectedUnit].Target].Kind!=UnitKind.Transport  ) )||(unit.Kind>=UnitKind.InfantryBase&&unit.Kind<=UnitKind.Fortress)||(unit.Kind==UnitKind.Transport))
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
		else if( MoveOrders[e].Unit!=0 && Units[MoveOrders[e].Unit].Supply==0 && !( Units[MoveOrders[e].Unit].Kind>=UnitKind.AirBase && Units[MoveOrders[e].Unit].Kind<=UnitKind.Fortress  )  && !(Units[MoveOrders[e].Unit].Category==UnitCategory.Plane && Units[MoveOrders[e].Unit].PlaneState==UnitState.Parked && Units[Units[MoveOrders[e].Unit].Carrier].Hp<=Units[Units[MoveOrders[e].Unit].Carrier].MaxHp*0.2) )
			{
			// あるユニットに新ＰＰ＿ＸＹが設定された場合

			m=MoveOrders[e].Unit;
			for(n=0; Units[m].PathX[n]!=MAP_RIGHT+1; n++)
				{}
			if(MoveOrders[e].ClearsPath!=0  )
				{

				BufferedMoveOrders[e].ClearsPath=0;
				MoveOrders[e].ClearsPath=0;

				Units[m].PathX[0]=MoveOrders[e].Destination.X;
				Units[m].PathY[0]=MoveOrders[e].Destination.Y;
				Units[m].PathX[1]=MAP_RIGHT+1;
				n=1;

				}
			else
				{
				if(n<64)
					{
					Units[m].PathX[n]=MoveOrders[e].Destination.X;
					Units[m].PathY[n]=MoveOrders[e].Destination.Y;
					Units[m].PathX[n+1]=MAP_RIGHT+1;
					}
				}

			if( n==1 )
				{
				Units[m].GroupLeader=0;
				Units[m].IsGroupLeader=0;
				Units[m].FormationNumber=0;
				Units[m].Stop=0;

				// 一時的寮機の重複番号阻止
				for(s=0;s<=MaxUnitId;s++)
					chk[s]=0;

				f=0;
				for( s=0; s<=MaxUnitId; s++)
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
						f++;
						if(chk[Selections[e][s]]==0)
							{	// 既に使われた番機無し
							chk[Selections[e][s]]++;
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
							for(h=1;chk[h]!=0&&h<=255;h++)
								{}
							chk[h]++;
							unit.FormationNumber=(short)h;
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

				if( f>=1 )
					{
					Units[m].IsGroupLeader=(short)(f+1);		// 小隊機数(指揮機含む)
					}

				if( Units[m].Category==UnitCategory.Plane && Units[m].PlaneState==UnitState.Parked )
					{
					Units[Units[m].Carrier].info[4]=0;	// 空母なら これがオンで発艦中
					Units[Units[m].Carrier].info[7]=0;	// 空母ならこの数値で甲板上の右左
					}

				for( s=0; s<=MaxUnitId; s++)
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
public void	ScrollBattleArea(int drctn)
	{
	switch(drctn)
		{
		case 1:		// UP
			ScrollSpeed+=scrn_moving_add;
			if(ScrollSpeed>=SCRN_MAX_SPD)
				ScrollSpeed=SCRN_MAX_SPD-1;
			CameraPosition = new WorldPosition(CameraPosition.X, CameraPosition.Y + ScrollSpeed);
			if(CameraPosition.Y>MAP_TOP)
				CameraPosition = new WorldPosition(CameraPosition.X, MAP_TOP);
			break;
		case 2:		// UP RI
			break;
		case 3:		// RI
			ScrollSpeed+=scrn_moving_add;
			if(ScrollSpeed>=SCRN_MAX_SPD)
				ScrollSpeed=SCRN_MAX_SPD-1;
			CameraPosition = new WorldPosition(CameraPosition.X + ScrollSpeed, CameraPosition.Y);
			if(CameraPosition.X>(MAP_RIGHT-CMBT_WIDTH) )
				CameraPosition = new WorldPosition(MAP_RIGHT-CMBT_WIDTH, CameraPosition.Y);
			break;
		case 4:		// RI DW
			break;
		case 5:		// DW
			ScrollSpeed+=scrn_moving_add;
			if(ScrollSpeed>=SCRN_MAX_SPD)
				ScrollSpeed=SCRN_MAX_SPD-1;
			CameraPosition = new WorldPosition(CameraPosition.X, CameraPosition.Y - ScrollSpeed);
			if(CameraPosition.Y<(MAP_BOTTOM+CMBT_HEIGHT) )
				CameraPosition = new WorldPosition(CameraPosition.X, MAP_BOTTOM+CMBT_HEIGHT);
			break;
		case 6:		// DW LF
			break;
		case 7:		// LF
			ScrollSpeed+=scrn_moving_add;
			if(ScrollSpeed>=SCRN_MAX_SPD)
				ScrollSpeed=SCRN_MAX_SPD-1;
			CameraPosition = new WorldPosition(CameraPosition.X - ScrollSpeed, CameraPosition.Y);
			if(CameraPosition.X<MAP_LEFT)
				CameraPosition = new WorldPosition(MAP_LEFT, CameraPosition.Y);
			break;
		case 8:		// LF UP
			break;
		}
	}

/*-------------------------------------------
	入力フェッチ
--------------------------------------------*/
[Original("get_input")]
public void ReadInput()
	{

	int	flg=0;

	// キーボードの入力チェック
	if (pDIDevice!=null)
		{
		int hr;
		int y = 0;

		// バッファリング・データを取得する
		while(IsAppActive!=0)
			{
			DIDEVICEOBJECTDATA od;
			uint dwItems = 1;
			hr = pDIDevice.GetDeviceData((uint)(sizeof(DIDEVICEOBJECTDATA)),
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
							Buttons|=FRONT_BTN;
						else
							Buttons&=~FRONT_BTN;
						break;

					case DIK_S:
						if ((od.dwData & (0x80))!=0 )
							Buttons|=BACK_BTN;
						else
							Buttons&=~BACK_BTN;
						break;

					case DIK_D:
						if ((od.dwData & (0x80))!=0 )
							Buttons|=RIGHT_BTN;
						else
							Buttons&=~RIGHT_BTN;
						break;

					case DIK_A:
						if ((od.dwData & (0x80))!=0 )
							Buttons|=LEFT_BTN;
						else
							Buttons&=~LEFT_BTN;
						break;

					case DIK_SPACE:
						if ((od.dwData & (0x80))!=0 )
							Buttons|=SPACE;
						else
							Buttons&=~SPACE;
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

						if( IsEditingMap==0 && hwndChatDlg==null && (od.dwData & (0x80))!=0 )
							{
							hwndChatDlg = CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_CHAT_DIALOG), hwndApp, ChatDlgProc);

							}
						break;

					}
				}
			}
		}

	flg=0;

	if( CursorPosition.y<=0 || (Buttons&FRONT_BTN)!=0 )
		{
		ScrollBattleArea(1);
		flg=1;
		}

	if( CursorPosition.y>=SCRN_HEIGHT-1 || (Buttons&BACK_BTN)!=0 )
		{
		ScrollBattleArea(5);
		flg=1;
		}

	if( CursorPosition.x>=SCRN_WIDTH-1 || (Buttons&RIGHT_BTN)!=0 )
		{
		ScrollBattleArea(3);
		flg=1;
		}

	if( CursorPosition.x<=0 || (Buttons&LEFT_BTN)!=0)
		{
		ScrollBattleArea(7);
		flg=1;
		}

	if(flg==0)
		{
		ScrollSpeed=0;
		}

	// マウスボタンの状況記録

	// マウス
	if (pDIDeviceMouse!=null)
		{

		int hr;

		// バッファリング・データを取得する
		while(IsAppActive!=0)
			{
			DIDEVICEOBJECTDATA od;
			uint dwItems = 1;
			hr = pDIDeviceMouse.GetDeviceData((uint)(sizeof(DIDEVICEOBJECTDATA)),
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

	// 純粋なマウス位置を取る
	GetCursorPos(ref CursorPosition);
	if( IsFullscreen==0 )
		ScreenToClient(hwndApp, ref CursorPosition);

	}

/*-------------------------------------------
	DirectInput 初期化
---------------------------------------------*/

[Original("InitDInput")]
public bool InitializeDirectInput()
{
	int hr;
	DIPROPDWORD diprop;

	// *****************************************
	// DirectInputの作成
	hr = DirectInput8Create( hInstApp , DIRECTINPUT_VERSION,
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
