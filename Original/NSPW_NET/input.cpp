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


#include "all_head.h"
#include "all_extern.h"
#include	"all_forward.h"








//============================================================================
// 通信対戦用、入力データの保存、その他
//----------------------------------------------------------------------------
void	cnct_game_input_cont( void )
	{
	int		h,m,f,s,n,chk[256],e;



	e=1;
	if( new_slct[e].sw && !unit[new_slct[e].the_slct_unit].spry && !(unit[new_slct[e].the_slct_unit].kind>=AP && unit[new_slct[e].the_slct_unit].kind<=GF3/*MN1*/ ))
		{
		// ユニット自体をクリックした。
		the_slct_unit=new_slct[e].the_slct_unit;
		m=new_slct[e].m;


		if( m==0 || unit[the_slct_unit].used!=unit[m].used )
			{
			if( m==0 )
				{
				// 輸送船陸地を選択
				// 揚陸場所あり

				if(you_can_order)
					{
					the_slct_unit=new_slct[e].the_slct_unit;
					m=new_slct[e].m;

					bf_new_slct[1].sw=new_slct[e].sw;
					bf_new_slct[1].the_slct_unit=new_slct[e].the_slct_unit;
					bf_new_slct[1].m=new_slct[e].m;
					bf_new_slct[1].gr_x=new_slct[e].gr_x;
					bf_new_slct[1].gr_y=new_slct[e].gr_y;

					you_can_order=0;
					you_ordered=1;
					SoundPlayEffect( NULL, CLICK2 ,(double)(MAP_RIGHT+1), 0);
					}
				}
			else
				{
				// 敵性ユニットを左クリック
				if(you_can_order)
					{
					the_slct_unit=new_slct[e].the_slct_unit;
					m=new_slct[e].m;

					bf_new_slct[1].sw=new_slct[e].sw;
					bf_new_slct[1].the_slct_unit=new_slct[e].the_slct_unit;
					bf_new_slct[1].m=new_slct[e].m;
					bf_new_slct[1].gr_x=new_slct[e].gr_x;
					bf_new_slct[1].gr_y=new_slct[e].gr_y;


					if(your_side==JPN)
						{
						// 日本海軍サイド
//						for(s=1;s<=20;s++)
						for(s=1;s<=JPN_SHIP_END;s++)
							{
							// 水上ユニット
							bf_slct_unit[1][s-1]=slct_unit[1][s];
							}
//						for(s=41;s<=70;s++)
						for(s=JPN_PLANE_START;s<=JPN_PLANE_END;s++)
							{
							// 航空ユニット
							bf_slct_unit[1][s-JPN_SHIP_END/*20*/-1]=slct_unit[1][s];
							}
						}
					else
						{
						// 合衆国海軍サイド
//						for(s=21;s<=40;s++)
						for(s=USA_SHIP_START;s<=USA_SHIP_END;s++)
							{
							// 水上ユニット
							bf_slct_unit[1][s-JPN_SHIP_END/*20*/-1]=slct_unit[1][s];
							}
//						for(s=71;s<=100;s++)
						for(s=USA_PLANE_START;s<=USA_PLANE_END;s++)
							{
							// 航空ユニット
							bf_slct_unit[1][s-(USA_PLANE_END/2)/*50*/-1]=slct_unit[1][s];
							}
						}


					you_can_order=0;
					you_ordered=1;
					SoundPlayEffect( NULL, CLICK2 ,(double)(MAP_RIGHT+1), 0);

					}
				}
			}
		else
			{
			if ( unit[the_slct_unit].ctgry==PLANE && (unit[m].kind==CV1 || unit[m].kind==CVL1 || unit[m].kind==AP))
				{
				// 航空機の格納先を指定
				if(you_can_order)
					{
					the_slct_unit=new_slct[e].the_slct_unit;
					m=new_slct[e].m;

					bf_new_slct[1].sw=new_slct[e].sw;
					bf_new_slct[1].the_slct_unit=new_slct[e].the_slct_unit;
					bf_new_slct[1].m=new_slct[e].m;
					bf_new_slct[1].gr_x=new_slct[e].gr_x;
					bf_new_slct[1].gr_y=new_slct[e].gr_y;

					if(your_side==JPN)
						{
						// 日本海軍サイド
//						for(s=1;s<=20;s++)
						for(s=1;s<=JPN_SHIP_END;s++)
							{
							// 水上ユニット
							bf_slct_unit[1][s-1]=slct_unit[1][s];
							}
//						for(s=41;s<=70;s++)
						for(s=JPN_PLANE_START;s<=JPN_PLANE_END;s++)
							{
							// 航空ユニット
							bf_slct_unit[1][s-JPN_SHIP_END/*20*/-1]=slct_unit[1][s];
							}
						}
					else
						{
						// 合衆国海軍サイド
//						for(s=21;s<=40;s++)
						for(s=USA_SHIP_START;s<=USA_SHIP_END;s++)
							{
							// 水上ユニット
							bf_slct_unit[1][s-JPN_SHIP_END/*20*/-1]=slct_unit[1][s];
							}
//						for(s=71;s<=100;s++)
						for(s=USA_PLANE_START;s<=USA_PLANE_END;s++)
							{
							// 航空ユニット
							bf_slct_unit[1][s-(USA_PLANE_END/2)/*50*/-1]=slct_unit[1][s];
							}
						}

					you_can_order=0;
					you_ordered=1;
					SoundPlayEffect( NULL, CLICK2 ,(double)(MAP_RIGHT+1), 0);
					}
				}
			else
				{
				if( slct_unit[e][m]==0 )
					{	// ｍ番号ユニットを新規にセレクトに設定
					if(!(unit[m].kind>=AP&&unit[m].kind<=GF3) && !(unit[the_slct_unit].kind>=AP && unit[the_slct_unit].kind<=GF3))
						{
						slct_unit_no++; 
						slct_unit[e][m]=slct_unit_no; 
						}
					}
				else
					{	// ｍ番号ユニットをセレクトから外す
					slct_unit_no--; 
					// セレクトの設定番号を連番にする。
					for(n=1;n<=max_unit;n++)				
						{
						if( slct_unit[e][n]>=slct_unit[e][m]+1 )
							slct_unit[e][n]--;
						}
					slct_unit[e][m]=0; 
					}
				}
			}
		}
	else
		{
		if( you_can_order && new_pp[1].used && !unit[new_pp[1].used].spry && !(  unit[new_pp[1].used].kind>=AP && unit[new_pp[1].used].kind<=GF3 /*unit[new_pp[1].used].kind==AP || unit[new_pp[1].used].kind==SP || unit[new_pp[1].used].kind==GF1 ||unit[new_pp[1].used].kind==GF2 || unit[new_pp[1].used].kind==GF3*/ )  && !(unit[new_pp[1].used].ctgry==PLANE && unit[new_pp[1].used].info[0]==PARKING && unit[unit[new_pp[1].used].info[1]].hp[0]<=unit[unit[new_pp[1].used].info[1]].hp[1]*0.2) )
			{
			// あるマイユニットに新ＰＰ＿ＸＹが設定された場合
			// バッファに保存。これを命令をだせるタイミングにnew_ppに代入する。
			bf_new_pp[1].used=new_pp[1].used;
			bf_new_pp[1].x=new_pp[1].x;
			bf_new_pp[1].y=new_pp[1].y;
			bf_new_pp[1].cls=new_pp[1].cls;


			if(your_side==JPN)
				{
				// 日本海軍サイド
//				for(s=1;s<=20;s++)
				for(s=1;s<=JPN_SHIP_END;s++)
					{
					// 水上ユニット
					bf_slct_unit[1][s-1]=slct_unit[1][s];
					}
//				for(s=41;s<=70;s++)
				for(s=JPN_PLANE_START;s<=JPN_PLANE_END;s++)
					{
					// 航空ユニット
					bf_slct_unit[1][s-JPN_SHIP_END/*20*/-1]=slct_unit[1][s];
					}
				}
			else
				{
				// 合衆国海軍サイド
//				for(s=21;s<=40;s++)
				for(s=USA_SHIP_START;s<=USA_SHIP_END;s++)
					{
					// 水上ユニット
					bf_slct_unit[1][s-JPN_SHIP_END/*20*/-1]=slct_unit[1][s];
					}
//				for(s=71;s<=100;s++)
				for(s=USA_PLANE_START;s<=USA_PLANE_END;s++)
					{
					// 航空ユニット
					bf_slct_unit[1][s-(USA_PLANE_END/2)/*50*/-1]=slct_unit[1][s];
					}
				}



			m=new_pp[e].used;
			if( unit[m].ctgry==PLANE && unit[m].info[0]==PARKING )
				{
				unit[unit[m].info[1]].info[4]=0;	// 空母なら これがオンで発艦中
				unit[unit[m].info[1]].info[7]=0;	// 空母ならこの数値で甲板上の右左
				the_slct_unit=0; 
				cmbt_menu_kind=0;
				cmbt_menu_slctd=0;
				slct_unit[1][m]=0; 
				cls_all_slct_unit_p2(1);	
				}
			you_can_order=0;
			you_ordered=1;
			SoundPlayEffect( NULL, CLICK2 ,(double)(MAP_RIGHT+1), 0);
			}
		}
	}








//============================================================================
// 通信対戦用、入力データの発動
//----------------------------------------------------------------------------
void	set_cpu_root2( int	m )
	{
	double		add;
	double		pp_drctn,wrk_x,wrk_y,add_drctn,pp_dstc,rslt_drctn[16][2],chk_dstc,wrk,drctn1,drctn2,wrk_x2,wrk_y2,start_x,start_y,min_dstc,s_pp_x,s_pp_y,first_drctn,div,drctn_ok1,drctn_ok2,re_add;
	int			n,hit,cm_scrn_x,cm_scrn_y,left,right,pp_indx,error,f,i;
	RECT		wrk_r;

    char ach[128];
    int len;



//	if( !fullscreen )
//		SetWindowText( hwndApp, "in Set_cpu_root2" );



	//dbg[2]=0;
	error=0;
	div=2.0;
	re_add=160;


	drctn_ok1=90.0;
	drctn_ok2=270.0;


	//	ＰＰ０が侵入不可地なら移動無しにしてリターン
	if( unit[m].ctgry==SHIP  )
		{
		// 他の艦船があるか
		wrk_x2=unit[m].pp_x[0];
		wrk_y2=unit[m].pp_y[0];
		for( n=1; n<=max_unit; n++)
			{
			if( unit[n].used && m!=n && unit[n].ctgry==SHIP && !(unit[n].kind==SS1 && unit[n].info[6])  /*&& unit[n].kind!=SP && unit[n].kind!=AP*/ && !(unit[n].kind>=AP && unit[n].kind<=GF3 )  )
				{
				// ptin dbg
				wrk_r.top=(int)unit[n].y+(sprt[UNIT_JPN].wd/2);//(int)unit[n].y-(sprt[UNIT_JPN].wd/2);
				wrk_r.right=(int)unit[n].x+(sprt[UNIT_JPN].wd/2);
				wrk_r.bottom=(int)unit[n].y-(sprt[UNIT_JPN].wd/2);//(int)unit[n].y+(sprt[UNIT_JPN].wd/2);
				wrk_r.left=(int)unit[n].x-(sprt[UNIT_JPN].wd/2);

				if( pt_in_rect3(&wrk_r,(int)wrk_x2,(int)wrk_y2))
					{
					// 前方に艦船！
					//unit[m].pp_x[0]=unit[m].x;
					//unit[m].pp_y[0]=unit[m].y;
					//unit[m].pp_x[1]=MAP_RIGHT+1;
					return;
					}
				}
			}


		// ＰＰ方向に陸地があるか
		wrk_x2=unit[m].pp_x[0];
		wrk_y2=unit[m].pp_y[0];
		if(!( wrk_y2>MAP_TOP || wrk_y2<MAP_BOTTOM || wrk_x2<MAP_LEFT || wrk_x2>MAP_RIGHT ))
			{
			cm_scrn_x=(int)((wrk_x2+(sprt[UNIT_JPN].wd/2)-MAP_LEFT)/sprt[MAP_TIP_NRML].wd);
			cm_scrn_y=(int)((MAP_TOP-wrk_y2+(sprt[UNIT_JPN].ht/2))/sprt[MAP_TIP_NRML].ht);
			if( cmbt_map[cm_scrn_y][cm_scrn_x]>=1)
				{
				//unit[m].pp_x[0]=unit[m].x;
				//unit[m].pp_y[0]=unit[m].y;
				//unit[m].pp_x[1]=MAP_RIGHT+1;
				return;
				}
			}
	
		}






	//
	min_dstc=0;
	s_pp_x=unit[m].pp_x[0];
	s_pp_y=unit[m].pp_y[0];
	pp_indx=0;



	while(pp_indx==0)
		{



		// 最終定点への角度と距離
		if(pp_indx==0)
			{
			start_x=unit[m].x;
			start_y=unit[m].y;
			}
		else
			{
			start_x=unit[m].pp_x[pp_indx-1];
			start_y=unit[m].pp_y[pp_indx-1];
			}


		if(min_dstc)
			{
			unit[m].pp_x[pp_indx]=s_pp_x;
			unit[m].pp_y[pp_indx]=s_pp_y;
			unit[m].pp_x[pp_indx+1]=MAP_RIGHT+1;			


			wrk_x=unit[m].pp_x[pp_indx]-unit[m].x;
			wrk_y=unit[m].pp_y[pp_indx]-unit[m].y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;

			pp_drctn=atan2(wrk_y,wrk_x)*RAD_to;

			unit[m].pp_x[pp_indx]+=cos(pp_drctn*a_PI)*(min_dstc);
			unit[m].pp_y[pp_indx]+=sin(pp_drctn*a_PI)*(min_dstc);
			}



		wrk_x=unit[m].pp_x[pp_indx]-start_x;
		wrk_y=unit[m].pp_y[pp_indx]-start_y;
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






		if( unit[m].ctgry==SHIP )
			{
			// 艦船のルート再計算

			hit=0;
			chk_dstc=40;
			while(!hit && chk_dstc<=pp_dstc)
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
					cm_scrn_x=(int)((wrk_x+(sprt[UNIT_JPN].wd/2)-MAP_LEFT)/sprt[MAP_TIP_NRML].wd);
					cm_scrn_y=(int)((MAP_TOP-wrk_y+(sprt[UNIT_JPN].ht/2))/sprt[MAP_TIP_NRML].ht);
					if( cmbt_map[cm_scrn_y][cm_scrn_x]>=1)
						{
						hit=1;		// 島に接触
						}
					}

				// 船に接触するか
				for( f=1; f<=max_unit && hit==0 ; f++)
					{
					if( unit[f].used && m!=f && unit[f].ctgry==SHIP && !(unit[f].kind==SS1 && unit[f].info[6]) && /*!(unit[f].kind==SP||unit[f].kind==AP)*/!(unit[f].kind>=AP&&unit[f].kind<=GF3) )
						{
						// ptin dbg
						wrk_r.top=(int)unit[f].y+(sprt[UNIT_JPN].ht/2);//(int)unit[f].y-(sprt[UNIT_JPN].ht/2);
						wrk_r.right=(int)unit[f].x+(sprt[UNIT_JPN].wd/2);
						wrk_r.bottom=(int)unit[f].y-(sprt[UNIT_JPN].ht/2);//(int)unit[f].y+(sprt[UNIT_JPN].ht/2);
						wrk_r.left=(int)unit[f].x-(sprt[UNIT_JPN].wd/2);
						if( pt_in_rect3(&wrk_r,(int)wrk_x,(int)wrk_y))
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
								cm_scrn_x=(int)((wrk_x+(sprt[UNIT_JPN].wd/2)-MAP_LEFT)/sprt[MAP_TIP_NRML].wd);
								cm_scrn_y=(int)((MAP_TOP-wrk_y+(sprt[UNIT_JPN].ht/2))/sprt[MAP_TIP_NRML].ht);
								if( cmbt_map[cm_scrn_y][cm_scrn_x]>=1)
									{
									left=1;		// 島に接触
									}
								}

							// 船に接触するか
							for( f=1; f<=max_unit; f++)
								{
								if( unit[f].used && m!=f && unit[f].ctgry==SHIP && !(unit[f].kind==SS1 && unit[f].info[6]) && /*!(unit[f].kind==SP||unit[f].kind==AP)*/!(unit[f].kind>=AP && unit[f].kind<=GF3)  )
									{
									// ptin dbg
									wrk_r.top=(int)unit[f].y+(sprt[UNIT_JPN].ht/2);//(int)unit[f].y-(sprt[UNIT_JPN].ht/2);
									wrk_r.right=(int)unit[f].x+(sprt[UNIT_JPN].wd/2);
									wrk_r.bottom=(int)unit[f].y-(sprt[UNIT_JPN].ht/2);//(int)unit[f].y+(sprt[UNIT_JPN].ht/2);
									wrk_r.left=(int)unit[f].x-(sprt[UNIT_JPN].wd/2);
									if( pt_in_rect3(&wrk_r,(int)wrk_x,(int)wrk_y))
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
								cm_scrn_x=(int)((wrk_x2+(sprt[UNIT_JPN].wd/2)-MAP_LEFT)/sprt[MAP_TIP_NRML].wd);
								cm_scrn_y=(int)((MAP_TOP-wrk_y2+(sprt[UNIT_JPN].ht/2))/sprt[MAP_TIP_NRML].ht);
								if( cmbt_map[cm_scrn_y][cm_scrn_x]>=1)
									{
									right=1;		// 島に接触
									}
								}

							// 船に接触するか
							for( f=1; f<=max_unit; f++)
								{
								if( unit[f].used && m!=f && unit[f].ctgry==SHIP && !(unit[f].kind==SS1 && unit[f].info[6]) && /*!(unit[f].kind==SP||unit[f].kind==AP)*/!(unit[f].kind>=AP && unit[f].kind<=GF3 ) )
									{
									// ptin dbg
									wrk_r.top=(int)unit[f].y+(sprt[UNIT_JPN].ht/2);//(int)unit[f].y-(sprt[UNIT_JPN].ht/2);
									wrk_r.right=(int)unit[f].x+(sprt[UNIT_JPN].wd/2);
									wrk_r.bottom=(int)unit[f].y-(sprt[UNIT_JPN].ht/2);//(int)unit[f].y+(sprt[UNIT_JPN].ht/2);
									wrk_r.left=(int)unit[f].x-(sprt[UNIT_JPN].wd/2);
									if( pt_in_rect3(&wrk_r,(int)wrk_x2,(int)wrk_y2))
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
						unit[m].pp_x[pp_indx+1]=unit[m].pp_x[pp_indx];
						unit[m].pp_y[pp_indx+1]=unit[m].pp_y[pp_indx];
						unit[m].pp_x[pp_indx+2]=MAP_RIGHT+1;
						wrk_x2=start_x;
						wrk_y2=start_y;
 

						wrk=chk_dstc/div;
						if(wrk<40)
							wrk=40;
//						if(wrk>2024)
//							wrk=2024;
						wrk_x2+=cos(drctn2*a_PI)*wrk;
						wrk_y2+=sin(drctn2*a_PI)*wrk;
						unit[m].pp_x[pp_indx]=wrk_x2;
						unit[m].pp_y[pp_indx]=wrk_y2;
						


						if( pp_indx>=1 )
							{
							// ＰＰ０からＰＰ１への角度
							wrk_x=unit[m].pp_x[pp_indx]-unit[m].pp_x[pp_indx-1];
							wrk_y=unit[m].pp_y[pp_indx]-unit[m].pp_y[pp_indx-1];
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
								wrk_x=unit[m].pp_x[pp_indx-1]-unit[m].x;
								wrk_y=unit[m].pp_y[pp_indx-1]-unit[m].y;
								}
							else
								{
								wrk_x=unit[m].pp_x[pp_indx-1]-unit[m].pp_x[pp_indx-2];
								wrk_y=unit[m].pp_y[pp_indx-1]-unit[m].pp_y[pp_indx-2];
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
								unit[m].pp_x[pp_indx]=s_pp_x;
								unit[m].pp_y[pp_indx]=s_pp_y;
								unit[m].pp_x[pp_indx+1]=MAP_RIGHT+1;

								min_dstc+=re_add;

								pp_indx--;

								error++;

								}
							else
								{
								min_dstc=0;
								unit[m].pp_x[pp_indx+1]=s_pp_x;
								unit[m].pp_y[pp_indx+1]=s_pp_y;
								unit[m].pp_x[pp_indx+2]=MAP_RIGHT+1;
								}
							}

						break;
						}



					if( left==0 && chk_dstc>=pp_dstc)
						{
						unit[m].pp_x[pp_indx+1]=unit[m].pp_x[pp_indx];
						unit[m].pp_y[pp_indx+1]=unit[m].pp_y[pp_indx];
						unit[m].pp_x[pp_indx+2]=MAP_RIGHT+1;
						wrk_x=start_x;
						wrk_y=start_y;

						wrk=chk_dstc/div;
						if(wrk<40)
							wrk=40;
//						if(wrk>2024)
//							wrk=2024;
						wrk_x+=cos(drctn1*a_PI)*wrk;
						wrk_y+=sin(drctn1*a_PI)*wrk;
						unit[m].pp_x[pp_indx]=wrk_x;
						unit[m].pp_y[pp_indx]=wrk_y;
						


						if( pp_indx>=1 )
							{
							// ＰＰ０からＰＰ１への角度
							wrk_x=unit[m].pp_x[pp_indx]-unit[m].pp_x[pp_indx-1];
							wrk_y=unit[m].pp_y[pp_indx]-unit[m].pp_y[pp_indx-1];
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
								wrk_x=unit[m].pp_x[pp_indx-1]-unit[m].x;
								wrk_y=unit[m].pp_y[pp_indx-1]-unit[m].y;
								}
							else
								{
								wrk_x=unit[m].pp_x[pp_indx-1]-unit[m].pp_x[pp_indx-2];
								wrk_y=unit[m].pp_y[pp_indx-1]-unit[m].pp_y[pp_indx-2];
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
								unit[m].pp_x[pp_indx]=s_pp_x;
								unit[m].pp_y[pp_indx]=s_pp_y;
								unit[m].pp_x[pp_indx+1]=MAP_RIGHT+1;

								min_dstc+=re_add;

								pp_indx--;

								error++;

								}
							else
								{
								min_dstc=0;
								unit[m].pp_x[pp_indx+1]=s_pp_x;
								unit[m].pp_y[pp_indx+1]=s_pp_y;
								unit[m].pp_x[pp_indx+2]=MAP_RIGHT+1;
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
					unit[m].pp_x[pp_indx+1]=s_pp_x;
					unit[m].pp_y[pp_indx+1]=s_pp_y;
					unit[m].pp_x[pp_indx+2]=MAP_RIGHT+1;
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
//		if( !fullscreen )
//			SetWindowText( hwndApp, len );

		}





//	if( !fullscreen )
//		SetWindowText( hwndApp, "out Set_cpu_root2" );


	return;
	}




//============================================================================
// 通信対戦用、入力データの発動
//----------------------------------------------------------------------------
void	cnct_game_input_now( void )
	{
	int		h,m,f,s,n,chk[256],e;


	for(e=0; e<=1; e++)
		{
		if( new_slct[e].sw && !unit[new_slct[e].the_slct_unit].spry && !(unit[new_slct[e].the_slct_unit].kind>=AP && unit[new_slct[e].the_slct_unit].kind<=GF3/*MN1*/ ))
			{
			// ユニット自体をクリックした。
			the_slct_unit=new_slct[e].the_slct_unit;
			m=new_slct[e].m;

			if( m==0 || unit[the_slct_unit].used!=unit[m].used )
				{
				if( m==0 )
					{
					// 輸送船陸地を選択
					// 揚陸場所あり
					if( unit[the_slct_unit].arm[2] && unit[the_slct_unit].info[6]==(int)new_slct[e].gr_x && unit[the_slct_unit].info[7]==(int)new_slct[e].gr_y)
						{
						unit[the_slct_unit].arm[2]=0;
						}
					else
						{
						unit[the_slct_unit].arm[2]=max_unit+1;
						unit[the_slct_unit].info[6]=(int)new_slct[e].gr_x;		// 揚陸座標
						unit[the_slct_unit].info[7]=(int)new_slct[e].gr_y;
						}
					}
				else
					{
					// 敵性ユニットを左クリック
					if( m!=unit[the_slct_unit].arm[2] )
						{
						if(!(unit[the_slct_unit].arm[0]==TPD && ( unit[m].kind>=AP && unit[m].kind<=GF3 )) &&
							!((unit[the_slct_unit].kind==FT1 && ( unit[m].ctgry==SHIP && unit[m].kind!=TR1 ))||(unit[the_slct_unit].kind>=GF1&&unit[the_slct_unit].kind<=GF3)||(unit[the_slct_unit].kind==TR1) ) 
							/*&& !( unit[the_slct_unit].ctgry==PLANE && unit[the_slct_unit].info[0]==PARKING )*/
							)
							{
							unit[the_slct_unit].arm[2]=m;			// 攻撃対象のナンバー
							}									
						}
					else
						unit[the_slct_unit].arm[2]=0;			// 攻撃目標ユニットをなくす
					// 攻撃目標を随伴機にも指定する。
					for(n=1;n<=max_unit;n++)
						{
						if( unit[n].used && slct_unit[e][n] && !((unit[n].kind==FT1 && ( unit[unit[the_slct_unit].arm[2]].ctgry==SHIP &&  unit[unit[the_slct_unit].arm[2]].kind!=TR1  ) )||(unit[n].kind>=GF1&&unit[n].kind<=GF3)||(unit[n].kind==TR1)) 
							/*&& !( unit[n].ctgry==PLANE && unit[n].info[0]==PARKING )*/
							)
							unit[n].arm[2]=unit[the_slct_unit].arm[2];				// 
						}
					}
				}
			else
				{
				if ( unit[the_slct_unit].ctgry==PLANE /*&& ( unit[n].ctgry==PLANE && unit[n].info[0]==FLYING )*/ && (unit[m].kind==CV1 || unit[m].kind==CVL1 || unit[m].kind==AP))
					{
					for(n=1;n<=max_unit;n++)
						if( unit[n].used && slct_unit[e][n] && unit[n].ctgry==PLANE && unit[n].info[0]==FLYING && !(unit[n].kind==BM1&&(unit[m].kind==CV1||unit[m].kind==CVL1)) )
							unit[n].info[1]=m;					// 所属の空母、及び、基地の番号
					}
				else
					{
					}
				}
			}
		else if( new_pp[e].used && !unit[new_pp[e].used].spry && !( unit[new_pp[e].used].kind>=AP && unit[new_pp[e].used].kind<=GF3 /*unit[new_pp[e].used].kind==AP || unit[new_pp[e].used].kind==SP || unit[new_pp[e].used].kind==GF1 ||unit[new_pp[e].used].kind==GF2 || unit[new_pp[e].used].kind==GF3*/ )  && !(unit[new_pp[e].used].ctgry==PLANE && unit[new_pp[e].used].info[0]==PARKING && unit[unit[new_pp[e].used].info[1]].hp[0]<=unit[unit[new_pp[e].used].info[1]].hp[1]*0.2) )
			{
			// あるユニットに新ＰＰ＿ＸＹが設定された場合



			m=new_pp[e].used;
			for(n=0; unit[m].pp_x[n]!=MAP_RIGHT+1; n++)
				{}
			if(new_pp[e].cls /*|| unit[m].used==cpu_side*/ )
				{

				bf_new_pp[e].cls=0; 
				new_pp[e].cls=0; 

				unit[m].pp_x[0]=new_pp[e].x;
				unit[m].pp_y[0]=new_pp[e].y;
				unit[m].pp_x[1]=MAP_RIGHT+1;
				n=1;

				}
			else
				{
				if(n<64)
					{
					unit[m].pp_x[n]=new_pp[e].x;
					unit[m].pp_y[n]=new_pp[e].y;
					unit[m].pp_x[n+1]=MAP_RIGHT+1;
					}
				}


			if( n==1 )
				{
				unit[m].ltl_ldr=0;
				unit[m].is_ltl_ldr=0;
				unit[m].no=0;
				unit[m].stop=0;

				// 一時的寮機の重複番号阻止
				for(s=0;s<=max_unit;s++)
					chk[s]=0;

				f=0;
				for( s=0; s<=max_unit; s++)
					{
					if( !slct_unit[e][s] && unit[s].ltl_ldr==m )
						{
						// 今回はセレクトされなかった、元同じリーダーのユニット
						unit[s].ltl_ldr=0;
						unit[s].no=0;
						unit[s].to_ldr_drctn=0;
						unit[s].to_ldr_dstc=0;
						}

					if( slct_unit[e][s] && s!=m && !(unit[s].ctgry==PLANE
					   && (unit[s].arm[0]==RDY_TPD||unit[s].arm[0]==RDY_BOM||unit[s].arm[0]==NOTHING) 
					   && unit[s].arm[3] )
						 )
						{
						f++;
						if(chk[slct_unit[e][s]]==0)	
							{	// 既に使われた番機無し
							chk[slct_unit[e][s]]++;
							unit[s].no=slct_unit[e][s];
							unit[s].is_ltl_ldr=0;
		
							if( unit[s].ctgry==SHIP )
								{
								if( unit[s].ltl_ldr!=m )
									{
									unit[s].ltl_ldr=m;
									set_frmtn_of_ships(s);
									}
								}
							else
								unit[s].ltl_ldr=m;


							set_pos_of_dynmc(s);
							}
						else
							{	// 既に使われた番機をつかおうとした
							for(h=1;chk[h]!=0&&h<=255;h++)
								{}
							chk[h]++;
							unit[s].no=h;
							unit[s].is_ltl_ldr=0;

							if( unit[s].ctgry==SHIP )
								{
								if( unit[s].ltl_ldr!=m )
									{
									unit[s].ltl_ldr=m;
									set_frmtn_of_ships(s);
									}
								}
							else
								unit[s].ltl_ldr=m;

							set_pos_of_dynmc(s);
							}	
						}
					}

				if( f>=1 )
					{
					unit[m].is_ltl_ldr=f+1;		// 小隊機数(指揮機含む)
					}

				if( unit[m].ctgry==PLANE && unit[m].info[0]==PARKING )
					{
					unit[unit[m].info[1]].info[4]=0;	// 空母なら これがオンで発艦中
					unit[unit[m].info[1]].info[7]=0;	// 空母ならこの数値で甲板上の右左
					}

				for( s=0; s<=max_unit; s++)
					{
					if( unit[s].ltl_ldr && unit[unit[s].ltl_ldr].is_ltl_ldr==0 )
						unit[s].ltl_ldr=0;
					}
				}
			}
		}
	}








/*-------------------------------------------
	戦術マップ移動
--------------------------------------------*/
void	tac_map_scrl ( int	drctn )
	{
	switch(drctn)
		{
		case 1:		// UP 
			scrn_moving_spd+=scrn_moving_add;
			if(scrn_moving_spd>=SCRN_MAX_SPD)
				scrn_moving_spd=SCRN_MAX_SPD-1;	
			cmbt_y+=scrn_moving_spd;	
			if(cmbt_y>MAP_TOP)
				cmbt_y=MAP_TOP;
			break;
		case 2:		// UP RI
			break;
		case 3:		// RI
			scrn_moving_spd+=scrn_moving_add;
			if(scrn_moving_spd>=SCRN_MAX_SPD)
				scrn_moving_spd=SCRN_MAX_SPD-1;	
			cmbt_x+=scrn_moving_spd;
			if(cmbt_x>(MAP_RIGHT-CMBT_WIDTH) )		
				cmbt_x=MAP_RIGHT-CMBT_WIDTH;
			break;
		case 4:		// RI DW
			break;
		case 5:		// DW
			scrn_moving_spd+=scrn_moving_add;
			if(scrn_moving_spd>=SCRN_MAX_SPD)
				scrn_moving_spd=SCRN_MAX_SPD-1;	
			cmbt_y-=scrn_moving_spd;
			if(cmbt_y<(MAP_BOTTOM+CMBT_HEIGHT) )
				cmbt_y=MAP_BOTTOM+CMBT_HEIGHT;
			break;
		case 6:		// DW LF
			break;
		case 7:		// LF
			scrn_moving_spd+=scrn_moving_add;
			if(scrn_moving_spd>=SCRN_MAX_SPD)
				scrn_moving_spd=SCRN_MAX_SPD-1;
			cmbt_x-=scrn_moving_spd;	
			if(cmbt_x<MAP_LEFT)		
				cmbt_x=MAP_LEFT;
			break;
		case 8:		// LF UP
			break;
		}
	}



/*-------------------------------------------
	入力フェッチ
--------------------------------------------*/
void get_input(void)
	{

	int	flg=0;









	// キーボードの入力チェック
#if 1
	if (pDIDevice!=NULL)
		{
		HRESULT hr;
		int y = 0;

		// バッファリング・データを取得する
		while(appActive)
			{
			DIDEVICEOBJECTDATA od;
			DWORD dwItems = 1;
			hr = pDIDevice->GetDeviceData(sizeof(DIDEVICEOBJECTDATA),
								&od, &dwItems, 0);
			if (hr==DIERR_INPUTLOST)
				pDIDevice->Acquire();
			else if (FAILED(hr) || dwItems == 0)
	            break;	// データが読めないか、存在しない
			else
				{

				switch (od.dwOfs) 
					{

					case DIK_W:
						if (od.dwData & (0x80) )
							key_cndtn|=FRONT_BTN;
						else
							key_cndtn&=~FRONT_BTN;
						break;

					case DIK_S:
						if (od.dwData & (0x80) )
							key_cndtn|=BACK_BTN;
						else
							key_cndtn&=~BACK_BTN;
						break;


					case DIK_D:
						if (od.dwData & (0x80) )
							key_cndtn|=RIGHT_BTN;
						else
							key_cndtn&=~RIGHT_BTN;
						break;

					case DIK_A:
						if (od.dwData & (0x80) )
							key_cndtn|=LEFT_BTN;
						else
							key_cndtn&=~LEFT_BTN;
						break;



					case DIK_SPACE:
						if (od.dwData & (0x80) )
							key_cndtn|=SPACE;
						else
							key_cndtn&=~SPACE;
						break;



					case DIK_F:
						if( mode==CMBT && od.dwData & (0x80) )
							{
							if(game_speed<2)
								game_speed++;
							else if(game_speed==2)
								game_speed=4;
							else if(game_speed<300)
								game_speed+=2;
							}
						break;

					case DIK_G:
						if( mode==CMBT && od.dwData & (0x80) )
							{
							if(game_speed<=1)
								game_speed=0;
							else if(game_speed<=2)
								game_speed=1;//--;
							else if(game_speed==4)
								game_speed=2;
							else if(game_speed!=0)
								game_speed-=2;
							}

						break;
		
					case DIK_R:
						if( mode==CMBT && od.dwData & (0x80) )
							{
							game_speed=1;
							}
						break;

					case DIK_RETURN:


						if( map_edit==0 && hwndChatDlg==NULL/*input_chat_now==0*/ /*&& ( mode==CMBT || mode==CNCT_GAME_SETTING || mode==CNCT_CNFG_SETTING )*/ && od.dwData & (0x80) )
							{
//							if( hwndChatDlg==NULL )
							hwndChatDlg = CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_CHAT_DIALOG), hwndApp, ChatDlgProc);

//	ShowWindow(hwndChatDlg,SW_SHOW);

//							DialogBox(hInstApp,MAKEINTRESOURCE(IDD_CHAT_DIALOG),hwndApp,ChatDlgProc);
							}
						break;

/***



					case DIK_E:
						if (od.dwData & (0x80) )
							key_cndtn|=UP_BTN;
						else
							key_cndtn&=~UP_BTN;
						break;

					case DIK_Q:
						if (od.dwData & (0x80) )
							key_cndtn|=DOWN_BTN;
						else
							key_cndtn&=~DOWN_BTN;
						break;


					case DIK_C:
						if (od.dwData & (0x80) )
							key_cndtn|=R_TURN_BTN;
						else
							key_cndtn&=~R_TURN_BTN;
						break;

					case DIK_Z:
						if (od.dwData & (0x80) )
							key_cndtn|=L_TURN_BTN;
						else
							key_cndtn&=~L_TURN_BTN;
						break;


					case DIK_V:
						if (od.dwData & (0x80) )
							key_cndtn|=V_KEY;
						else
							key_cndtn&=~V_KEY;
						break;

					case DIK_F:
						if( mode==BATTLE && od.dwData & (0x80) )
							{
							if(game_speed<2)
								game_speed++;
							else if(game_speed==2)
								game_speed=30;
							else if(game_speed<300)
								game_speed+=30;
							}
						break;

					case DIK_G:
						if( mode==BATTLE && od.dwData & (0x80) )
							{
							if(game_speed<=1)
								game_speed=0;
							else if(game_speed<=2)
								game_speed--;
							else if(game_speed==30)
								game_speed=2;
							else if(game_speed!=0)
								game_speed-=30;
							}

						break;
		
					case DIK_R:
						if( mode==BATTLE && od.dwData & (0x80) )
							{
							game_speed=1;
							}
						break;

					case DIK_X:
						if( mode==BATTLE )
							{
							if( od.dwData & (0x80) )
								{
								if( !(key_cndtn&TOP_VIEW_BTN2) )
									{
									key_cndtn^=TOP_VIEW_BTN;
									key_cndtn|=TOP_VIEW_BTN2;
									}
								}
							else
								{
								key_cndtn&=~TOP_VIEW_BTN2;
								}
							}
						break;
***/
					}
				}
			}
		}




	flg=0;


	if( crsr_pt.y<=0 ||key_cndtn&FRONT_BTN )
		{
		tac_map_scrl(1);
		flg=1;
		}

	if( crsr_pt.y>=SCRN_HEIGHT-1 || key_cndtn&BACK_BTN )
		{
		tac_map_scrl(5);
		flg=1;
		}

	if( crsr_pt.x>=SCRN_WIDTH-1  || key_cndtn&RIGHT_BTN )
		{
		tac_map_scrl(3);
		flg=1;
		}

	if( crsr_pt.x<=0 || key_cndtn&LEFT_BTN)
		{
		tac_map_scrl(7);
		flg=1;
		}


/*
	if( (crsr_pt.x<=0 || cBuf[VK_LEFT]&0x80 || ( cBuf[VK_NUMPAD4]&0x80 && map_edit==0 ) ) && cmbt_x>=MAP_LEFT+1 )
		{
		tac_map_scrl(7);
		flg=1;
		}
	if( (crsr_pt.x>=SCRN_WIDTH-1 || cBuf[VK_RIGHT]&0x80 || ( cBuf[VK_NUMPAD6]&0x80 && map_edit==0 )  ) && cmbt_x<=MAP_RIGHT-CMBT_WIDTH-1 )
		{
		tac_map_scrl(3);
		flg=1;
		}
	if((crsr_pt.y<=0 || cBuf[VK_UP]&0x80 || ( cBuf[VK_NUMPAD8]&0x80 && map_edit==0 )  ) && cmbt_y<=MAP_TOP-1 )
		{
		tac_map_scrl(1);
		flg=1;
		}
	if((crsr_pt.y>=SCRN_HEIGHT-1 || cBuf[VK_DOWN]&0x80 || ( cBuf[VK_NUMPAD2]&0x80 && map_edit==0 )  ) && cmbt_y>=MAP_BOTTOM+CMBT_HEIGHT+1 )
		{
		tac_map_scrl(5);
		flg=1;
		}
*/



	if(!flg)
		{
		scrn_moving_spd=0;
		}

#endif





	// マウスボタンの状況記録
/*
	if( key_cndtn&MS_L_BTN && !(key_cndtn&MS_L_BTN2) )
		{
		key_cndtn|=MS_L_BTN2;
		}
	if( key_cndtn&MS_R_BTN && !(key_cndtn&MS_R_BTN2) )
		{
		key_cndtn|=MS_R_BTN2;
		}
	if( key_cndtn&MS_C_BTN && !(key_cndtn&MS_C_BTN2) )
		{
		key_cndtn|=MS_C_BTN2;
		}
*/



	// マウス
	if (pDIDeviceMouse!=NULL)
		{

		HRESULT hr;
//		char CData[256];
#if 0
		// デバイスの直接データを取得する
		DIMOUSESTATE2 dims;
		hr = pDIDeviceMouse->GetDeviceState(sizeof(DIMOUSESTATE2), &dims);
		if (SUCCEEDED(hr))
			{
//			crsr_pos.x+=dims.lX;
//			crsr_pos.y+=dims.lY;

			if( dims.lZ>0 )
				{
				if( key_cndtn&BACK_BTN2 )
					{
					key_cndtn&=~BACK_BTN2;
					key_cndtn&=~FRONT_BTN2;
					}
				else
					{
					key_cndtn|=FRONT_BTN2;
					}
				}
			else if( dims.lZ<0 )
				{
				if( key_cndtn&FRONT_BTN2 )
					{
					key_cndtn&=~BACK_BTN2;
					key_cndtn&=~FRONT_BTN2;
					}
				else
					{
					key_cndtn|=BACK_BTN2;
					}
				}

			}
		else if (appActive && hr==DIERR_INPUTLOST)
			pDIDeviceMouse->Acquire();
#endif

		// バッファリング・データを取得する
		while(/*g_bActive*/appActive)
			{
			DIDEVICEOBJECTDATA od;
			DWORD dwItems = 1;
			hr = pDIDeviceMouse->GetDeviceData(sizeof(DIDEVICEOBJECTDATA),
								&od, &dwItems, 0);
			if (hr==DIERR_INPUTLOST)
				pDIDeviceMouse->Acquire();
			else if (FAILED(hr) || dwItems == 0)
	            break;	// データが読めないか、存在しない
			else
				{
				switch (od.dwOfs) 
					{

					// 左ボタンが押された、または離された。 
					case DIMOFS_BUTTON0:
						if( od.dwData )
							{
//							key_cndtn|=MS_L_BTN;
							if( !lf_btn )
								lf_btn=1;
							}
						else
							{
//							key_cndtn&=~MS_L_BTN;
//							key_cndtn&=~MS_L_BTN2;
							if( lf_btn )
								lf_btn=3;
							}
						break;

					// 右ボタンが押された、または離された。 
					case DIMOFS_BUTTON1:
						if( od.dwData )
							{
//							key_cndtn|=MS_R_BTN;
							if( !ri_btn )
								ri_btn=1;
							}
						else
							{
//							key_cndtn&=~MS_R_BTN;
//							key_cndtn&=~MS_R_BTN2;
							if( ri_btn )
								ri_btn=3;
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
	GetCursorPos(&crsr_pt);
	if( !fullscreen )
		ScreenToClient(hwndApp, &crsr_pt);




	}





#if 0

void	mouse_cont (void)
	{

	flg=0;

	if( (crsr_pt.x<=0 || cBuf[VK_LEFT]&0x80 || ( cBuf[VK_NUMPAD4]&0x80 && map_edit==0 ) ) && cmbt_x>=MAP_LEFT+1 )
		{
		tac_map_scrl(7);
		flg=1;
		}
	if( (crsr_pt.x>=SCRN_WIDTH-1 || cBuf[VK_RIGHT]&0x80 || ( cBuf[VK_NUMPAD6]&0x80 && map_edit==0 )  ) && cmbt_x<=MAP_RIGHT-CMBT_WIDTH-1 )
		{
		tac_map_scrl(3);
		flg=1;
		}



	if((crsr_pt.y<=0 || cBuf[VK_UP]&0x80 || ( cBuf[VK_NUMPAD8]&0x80 && map_edit==0 )  ) && cmbt_y<=MAP_TOP-1 )
		{
		tac_map_scrl(1);
		flg=1;
		}

	if((crsr_pt.y>=SCRN_HEIGHT-1 || cBuf[VK_DOWN]&0x80 || ( cBuf[VK_NUMPAD2]&0x80 && map_edit==0 )  ) && cmbt_y>=MAP_BOTTOM+CMBT_HEIGHT+1 )
		{
		tac_map_scrl(5);
		flg=1;
		}


	if(!flg)
		{
		scrn_moving_spd=0;
		}
	}


#endif





/*-------------------------------------------
	DirectInput 初期化
---------------------------------------------*/
BOOL CALLBACK EnumJoysticksCallback(const DIDEVICEINSTANCE* pdidInstance, VOID* pContext);
BOOL CALLBACK EnumAxesCallback(LPCDIDEVICEOBJECTINSTANCE lpddoi, LPVOID pvRef);

bool InitDInput(void)
{
	HRESULT hr;
	DIPROPDWORD diprop;

	// *****************************************
	// DirectInputの作成
	hr = DirectInput8Create( hInstApp , DIRECTINPUT_VERSION, 
							IID_IDirectInput8, (void**)&pDInput, NULL); 
	if (FAILED(hr)) 
	{
		DXTRACE_ERR("DirectInput8オブジェクトの作成に失敗", hr);
		return false;
	}


	//*** キーボード
	// デバイス・オブジェクトを作成
	hr = pDInput->CreateDevice(GUID_SysKeyboard, &pDIDevice, NULL); 
	if (FAILED(hr)) {
		DXTRACE_ERR("DirectInputDevice8オブジェクトの作成に失敗", hr);
	    return false;
	}

	// データ形式を設定
	hr = pDIDevice->SetDataFormat(&c_dfDIKeyboard);
	if (FAILED(hr))
	{
		DXTRACE_ERR("c_dfDIMouse2形式の設定に失敗", hr);
		return false;
	}



	//モードを設定（フォアグラウンド＆非排他モード）
	hr = pDIDevice->SetCooperativeLevel(hwndApp, DISCL_NONEXCLUSIVE | DISCL_FOREGROUND);
	if (FAILED(hr))
	{
		DXTRACE_ERR("フォアグラウンド＆非排他モードの設定に失敗", hr);
		return false;
	}

	// バッファリング・データを取得するため、バッファ・サイズを設定
	diprop.diph.dwSize	= sizeof(diprop); 
	diprop.diph.dwHeaderSize	= sizeof(diprop.diph); 
	diprop.diph.dwObj	= 0;
	diprop.diph.dwHow	= DIPH_DEVICE;
	diprop.dwData = DIDEVICE_BUFFERSIZE;
	hr = pDIDevice->SetProperty(DIPROP_BUFFERSIZE, &diprop.diph);
	if (FAILED(hr))
	{
		DXTRACE_ERR("バッファ・サイズの設定に失敗", hr);
		return false;
	}

	// 入力制御開始
	pDIDevice->Acquire();




#if 0
	//*** ジョイスティック
	// デバイスを列挙して作成
	hr = g_pDInput->EnumDevices(DI8DEVCLASS_GAMECTRL, EnumJoysticksCallback,
							NULL, DIEDFL_ATTACHEDONLY);
	if (FAILED(hr) /*|| g_pDIDeviceJoy==NULL*/ )
		{
		DXTRACE_ERR("DirectInputDevice8オブジェクトの作成に失敗", hr);
		return false;
		}



    // Make sure we got a joystick
    if( NULL == g_pDIDeviceJoy )
	    {
        return TRUE;		// ジョイスティックはなし。
		}


	// データ形式を設定
	hr = g_pDIDeviceJoy->SetDataFormat(&c_dfDIJoystick2);
	if (FAILED(hr))
		{
		DXTRACE_ERR("c_dfDIJoystick2形式の設定に失敗", hr);
		return false;
		}

	//モードを設定（フォアグラウンド＆非排他モード）
	hr = g_pDIDeviceJoy->SetCooperativeLevel(hwndApp, DISCL_NONEXCLUSIVE | DISCL_FOREGROUND);
	if (FAILED(hr))
		{
		DXTRACE_ERR("フォアグラウンド＆非排他モードの設定に失敗", hr);
		return false;
		}

	// コールバック関数を使って各軸のモードを設定
	hr = g_pDIDeviceJoy->EnumObjects(EnumAxesCallback, NULL, DIDFT_AXIS);
	if (FAILED(hr))
		{
		DXTRACE_ERR("軸モードの設定に失敗", hr);
		return false;
		}


	// 軸モードを設定（絶対値モードに設定。デフォルトなので必ずしも設定は必要ない）
	//DIPROPDWORD diprop;
	diprop.diph.dwSize	= sizeof(diprop); 
	diprop.diph.dwHeaderSize	= sizeof(diprop.diph); 
	diprop.diph.dwObj	= 0;
	diprop.diph.dwHow	= DIPH_DEVICE;
	diprop.dwData		= DIPROPAXISMODE_ABS;
	//	diprop.dwData		= DIPROPAXISMODE_REL;	// 相対値モードの場合
	hr = g_pDIDeviceJoy->SetProperty(DIPROP_AXISMODE, &diprop.diph);
	if (FAILED(hr))
		{
		DXTRACE_ERR("軸モードの設定に失敗", hr);
		return false;
		}

	// バッファリング・データを取得するため、バッファ・サイズを設定
	diprop.dwData = DIDEVICE_BUFFERSIZE;
	hr = g_pDIDeviceJoy->SetProperty(DIPROP_BUFFERSIZE, &diprop.diph);
	if (FAILED(hr))
		{
		DXTRACE_ERR("バッファ・サイズの設定に失敗", hr);
		return false;
		}



	// 入力制御開始
	g_pDIDeviceJoy->Acquire();
#endif



	//*** マウス
	// デバイス・オブジェクトを作成
	hr = pDInput->CreateDevice(GUID_SysMouse, &pDIDeviceMouse, NULL); 
	if (FAILED(hr)) 
		{
		DXTRACE_ERR("DirectInputDevice8オブジェクトの作成に失敗", hr);
	    return false;
		}

	// データ形式を設定
	hr = pDIDeviceMouse->SetDataFormat(&c_dfDIMouse2);
	if (FAILED(hr))
	{
		DXTRACE_ERR("c_dfDIMouse2形式の設定に失敗", hr);
		return false;
	}

	//モードを設定（フォアグラウンド＆非排他モード）
	hr = pDIDeviceMouse->SetCooperativeLevel(hwndApp, DISCL_NONEXCLUSIVE | DISCL_FOREGROUND);
	if (FAILED(hr))
	{
		DXTRACE_ERR("フォアグラウンド＆非排他モードの設定に失敗", hr);
		return false;
	}

	// 軸モードを設定（相対値モードに設定）
//	DIPROPDWORD diprop;
	diprop.diph.dwSize	= sizeof(diprop); 
	diprop.diph.dwHeaderSize	= sizeof(diprop.diph); 
	diprop.diph.dwObj	= 0;
	diprop.diph.dwHow	= DIPH_DEVICE;
	diprop.dwData		= DIPROPAXISMODE_REL;
//	diprop.dwData		= DIPROPAXISMODE_ABS;	// 絶対値モードの場合
	hr = pDIDeviceMouse->SetProperty(DIPROP_AXISMODE, &diprop.diph);
	if (FAILED(hr))
	{
		DXTRACE_ERR("軸モードの設定に失敗", hr);
		return false;
	}

	// バッファリング・データを取得するため、バッファ・サイズを設定
	diprop.dwData = DIDEVICE_BUFFERSIZE;
	hr = pDIDeviceMouse->SetProperty(DIPROP_BUFFERSIZE, &diprop.diph);
	if (FAILED(hr))
	{
		DXTRACE_ERR("バッファ・サイズの設定に失敗", hr);
		return false;
	}

	// 入力制御開始
	pDIDeviceMouse->Acquire();

	return true;
	}




