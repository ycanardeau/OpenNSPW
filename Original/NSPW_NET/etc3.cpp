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
//	マイ乱数を作ります
//----------------------------------------------------------------------------
void	make_my_rnd(void)
	{
	short	i;

	for(i=0;i<4096;i++)
		{
		my_rnd_sheet[i]=rnd(65536);
		}

	my_rnd_pt=0;
	}


//============================================================================
//	
//----------------------------------------------------------------------------
int		my_rnd(int	r)
	{
	my_rnd_pt++;
	my_rnd_pt%=4096;

	return(my_rnd_sheet[my_rnd_pt]%r);
	}



//============================================================================
//	
//----------------------------------------------------------------------------

int		rnd( int x)
	{


	rnd_count++;

	return(rand()%(x));
	}



//============================================================================
// スプライト基礎データ
//----------------------------------------------------------------------------
void		set_sprt_data( void )
	{
	
	// タイトル


	sprt[TTL_BACK].wd=699;//599;
	sprt[TTL_BACK].ht=384;//387;
	sprt[TTL_BACK].base_x=0;
	sprt[TTL_BACK].base_y=3940;
/*
#define	TTL_BACK			0
#define	UNIT_JPN			1
#define	UNIT_USA			2
#define UNIT_INFO_JPN		3
#define UNIT_INFO_USA		4
#define MAP_TIP_NRML		5
#define SUB_UNIT			6
#define BTN_1				7
#define BTN_2				8
#define BTN_BASE			9

*/
	// ユニット
	sprt[UNIT_JPN].wd=80;
	sprt[UNIT_JPN].ht=80;
	sprt[UNIT_JPN].base_x=0;
	sprt[UNIT_JPN].base_y=1759;
	sprt[UNIT_JPN].os_of_x=8;
	sprt[UNIT_JPN].cx=40;
	sprt[UNIT_JPN].cy=40;

	sprt[UNIT_USA].wd=80;
	sprt[UNIT_USA].ht=80;
	sprt[UNIT_USA].base_x=0;
	sprt[UNIT_USA].base_y=4550;
	sprt[UNIT_USA].os_of_x=8;
	sprt[UNIT_USA].cx=40;
	sprt[UNIT_USA].cy=40;

	// サブユニット
	sprt[SUB_UNIT].wd=40;
	sprt[SUB_UNIT].ht=40;
	sprt[SUB_UNIT].base_x=0;
	sprt[SUB_UNIT].base_y=3520;
	sprt[SUB_UNIT].os_of_x=12;
	sprt[SUB_UNIT].cx=20;
	sprt[SUB_UNIT].cy=20;

	// マップチップ 雲
	sprt[MAP_TIP_NRML].wd=80;
	sprt[MAP_TIP_NRML].ht=80;
	sprt[MAP_TIP_NRML].base_x=0;
	sprt[MAP_TIP_NRML].base_y=3000;
	sprt[MAP_TIP_NRML].os_of_x=6;
	sprt[MAP_TIP_NRML].cx=40;
	sprt[MAP_TIP_NRML].cy=40;

	// ユニットインフォ
	sprt[UNIT_INFO_JPN].x=CMBT_WIDTH;
	sprt[UNIT_INFO_JPN].y=0;
	sprt[UNIT_INFO_JPN].wd=120;
	sprt[UNIT_INFO_JPN].ht=438;
	sprt[UNIT_INFO_JPN].base_x=0;
	sprt[UNIT_INFO_JPN].base_y=0;
	sprt[UNIT_INFO_JPN].os_of_x=6;
	sprt[UNIT_INFO_JPN].cx=0;
	sprt[UNIT_INFO_JPN].cy=0;

	sprt[UNIT_INFO_USA].wd=120;
	sprt[UNIT_INFO_USA].ht=438;
	sprt[UNIT_INFO_USA].base_x=0;
	sprt[UNIT_INFO_USA].base_y=878;
	sprt[UNIT_INFO_USA].os_of_x=6;
	sprt[UNIT_INFO_USA].cx=0;
	sprt[UNIT_INFO_USA].cy=0;

	// 操作ボタンベース
	sprt[BTN_BASE].wd=198;
	sprt[BTN_BASE].ht=120;
	sprt[BTN_BASE].base_x=361;
	sprt[BTN_BASE].base_y=3250;
	sprt[BTN_BASE].os_of_x=1;
	sprt[BTN_BASE].cx=0;
	sprt[BTN_BASE].cy=0;

	// 操作ボタンベース
	sprt[BTN_1].wd=140;
	sprt[BTN_1].ht=20;
	sprt[BTN_1].base_x=0;
	sprt[BTN_1].base_y=3250;
	sprt[BTN_1].os_of_x=1;
	sprt[BTN_1].cx=0;
	sprt[BTN_1].cy=0;

	// マップベース
	sprt[MAP_BASE].wd=256-1;
	sprt[MAP_BASE].ht=200-1;
	sprt[MAP_BASE].base_x=0;
	sprt[MAP_BASE].base_y=4340;
	sprt[MAP_BASE].os_of_x=1;
	sprt[MAP_BASE].cx=0;
	sprt[MAP_BASE].cy=0;


	}



//============================================================================
// 
//----------------------------------------------------------------------------
void	cloud_in_start( void )
	{
	int		m,n,ok[16];	
	double	base_x,base_y,sub_x;



	while(1)
		{
		// 空いてるくもスプライトを探します。
		n=0;
		for(m=0; m<KUMO_MAX; m++)
			{
			if( !kumo[m].used )
				{
				ok[n]=m;
				n++;
				}
			if(n>=16)
				break;
			}

		if( n<=14 )
			return;



		base_x=(double)(rnd(abs(MAP_RIGHT)+abs(MAP_LEFT))-abs(MAP_LEFT));
		base_y=(double)(rnd(abs(MAP_TOP)+abs(MAP_BOTTOM))-abs(MAP_BOTTOM));


		//base_y=(double)(MAP_BOTTOM+300);
		sub_x=(double)( rnd(sprt[MAP_TIP_NRML].wd*4)-sprt[MAP_TIP_NRML].wd*2 );

		n=0;	
		for( m=0;m<3;m++)
			{
			kumo[ok[n]].used=1;
			kumo[ok[n]].x=/*MAP_RIGHT*/base_x+m*80+sub_x;
			kumo[ok[n]].y=base_y;
			kumo[ok[n]].kind=1;
			n++;
			}
		sub_x=(double)( rnd(sprt[MAP_TIP_NRML].wd*2)-sprt[MAP_TIP_NRML].wd*2 );
		for( m=0;m<5;m++)
			{
			kumo[ok[n]].used=1;
			kumo[ok[n]].x=/*MAP_RIGHT*/base_x+m*80-80+sub_x;
			kumo[ok[n]].y=base_y+80;
			kumo[ok[n]].kind=1;
			n++;
			}
		sub_x=(double)( rnd(sprt[MAP_TIP_NRML].wd*2)-sprt[MAP_TIP_NRML].wd*2 );
		for( m=0;m<3;m++)
			{
			kumo[ok[n]].used=1;
			kumo[ok[n]].x=/*MAP_RIGHT*/base_x+m*80+sub_x;
			kumo[ok[n]].y=base_y+80*2;
			kumo[ok[n]].kind=1;
			n++;
			}

		}


	}




//============================================================================
// 
//----------------------------------------------------------------------------
void	cloud_cont( void )
	{
	int		m,n,ok[16];	
	double	base_y,sub_x;



	
	for(n=0;n<KUMO_MAX;n++)
		{
		if( kumo[n].used )
			{
			if( !game_end )
				{
				kumo[n].x-=2.0/*2.0*/;
				if( kumo[n].x < MAP_LEFT )
					kumo[n].used=0;
				}
			}
		}





/*
	if( !((cc_count%200)==99) )
		return;
*/




	// 空いてるくもスプライトを探します。
	n=0;
	for(m=0; m<KUMO_MAX; m++)
		{
		if( !kumo[m].used )
			{
			ok[n]=m;
			n++;
			}
		if(n>=16)
			break;
		}

	if( n<=14 )
		return;




	base_y=(double)(rnd(abs(MAP_TOP)+abs(MAP_BOTTOM))-abs(MAP_BOTTOM));
	//base_y=(double)(MAP_BOTTOM+300);
	sub_x=(double)( rnd(sprt[MAP_TIP_NRML].wd*4)-sprt[MAP_TIP_NRML].wd*2 );
	n=0;	
	for( m=0;m<3;m++)
		{
		kumo[ok[n]].used=1;
		kumo[ok[n]].x=MAP_RIGHT+m*80+sub_x;
		kumo[ok[n]].y=base_y;
		kumo[ok[n]].kind=1;
		n++;
		}
	sub_x=(double)( rnd(sprt[MAP_TIP_NRML].wd*2)-sprt[MAP_TIP_NRML].wd*2 );
	for( m=0;m<5;m++)
		{
		kumo[ok[n]].used=1;
		kumo[ok[n]].x=MAP_RIGHT+m*80-80+sub_x;
		kumo[ok[n]].y=base_y+80;
		kumo[ok[n]].kind=1;
		n++;
		}
	sub_x=(double)( rnd(sprt[MAP_TIP_NRML].wd*2)-sprt[MAP_TIP_NRML].wd*2 );
	for( m=0;m<3;m++)
		{
		kumo[ok[n]].used=1;
		kumo[ok[n]].x=MAP_RIGHT+m*80+sub_x;
		kumo[ok[n]].y=base_y+80*2;
		kumo[ok[n]].kind=1;
		n++;
		}


	}











//============================================================================
// 
//----------------------------------------------------------------------------
void	cont_fire( void )
	{
	RECT	wrk_rect,wrk_r;
	int		m,f,h,cl,n,cm_scrn_x,cm_scrn_y,i;
	double	wrk_x,wrk_y;


	//=========		 ファイアの制御		=========//
	// 弾丸、爆弾等の機動、炸裂を制御します。
	for(m=1;m<FIRE_MAX/*255*/;m++)
		{
		if( fire[m].used )
			{
			//	揚陸艇
			if( fire[m].kind==TR_AP || fire[m].kind==TR_SP || fire[m].kind==TR_GF1 || fire[m].kind==TR_GF2 || fire[m].kind==TR_GF3 )
				{
				if( fire[m].info[0]<=fire[m].info[1] )
					{
					wrk_x=fire[m].x;
					wrk_y=fire[m].y;
					fire[m].x+=cos(fire[m].drctn*a_PI)*fire[m].spd;
					fire[m].y+=sin(fire[m].drctn*a_PI)*fire[m].spd;
					fire[m].spd+=fire[m].spd_add;
			

					h=0;

					// ptin dbg
					wrk_r.top=(int)fire[m].info[7]+20;//(int)fire[m].info[7]-20;
					wrk_r.right=(int)fire[m].info[6]+20;
					wrk_r.bottom=(int)fire[m].info[7]-20;//(int)fire[m].info[7]+20;
					wrk_r.left=(int)fire[m].info[6]-20;
					if( pt_in_rect3(&wrk_r,(int)fire[m].x,(int)fire[m].y) )
						h=1;

					//n=fire[m].used;

					if( h )
						{
						h=1;
						// 上陸地点到達
						for(n=1;n<=max_unit && h ;n++)
							{
							if( unit[n].used && unit[n].kind>=AP && unit[n].kind<=GF3 )
								{
								// ptin dbg
								wrk_r.top=(int)unit[n].y+20;//(int)unit[n].y-20;
								wrk_r.right=(int)unit[n].x+20;
								wrk_r.bottom=(int)unit[n].y-20;//(int)unit[n].y+20;
								wrk_r.left=(int)unit[n].x-20;
								if( pt_in_rect3(&wrk_r,(int)fire[m].x,(int)fire[m].y) )
									{
									// とにかくほかの地上施設の上
									h=0;		
									}
								}
							}

						if(h)
							{
							//set_new_unit(fire[m].info[8], fire[m].kind-17 /*GF1*/,(double)fire[m].info[6],(double)fire[m].info[7],0);
							switch(fire[m].kind)
								{
								case TR_GF1:
									f=set_new_unit(fire[m].info[8], GF1,(double)fire[m].info[6],(double)fire[m].info[7],0);
									unit[f].info[0]=0;												// 建設期間
									break;
								case TR_GF2:
									f=set_new_unit(fire[m].info[8], GF2,(double)fire[m].info[6],(double)fire[m].info[7],0);
									unit[f].info[0]=8000*( fire[m].info[8]==USA ? 1.0 : 0.8 );		// 建設期間
									unit[f].hp[0]/=4;
									unit[f].hp[1]/=4;
									break;
								case TR_GF3:
									f=set_new_unit(fire[m].info[8], GF3,(double)fire[m].info[6],(double)fire[m].info[7],0);
									unit[f].info[0]=15000*( fire[m].info[8]==USA ? 0.8 : 1.0 );		// 建設期間
									unit[f].hp[0]/=4;
									unit[f].hp[1]/=4;
									break;
								case TR_AP:
									f=set_new_unit(fire[m].info[8], AP,(double)fire[m].info[6],(double)fire[m].info[7],0);
									unit[f].info[0]=10000*( fire[m].info[8]==USA ? 0.8 : 1.0 );		// 建設期間
									unit[f].hp[0]/=4;
									unit[f].hp[1]/=4;
									break;
								case TR_SP:
									f=set_new_unit(fire[m].info[8], SP,(double)fire[m].info[6],(double)fire[m].info[7],0);
									unit[f].info[0]=20000*( fire[m].info[8]==USA ? 0.8 : 1.0 );		// 建設期間
									unit[f].hp[0]/=4;
									unit[f].hp[1]/=4;
									break;
								}
							}
						fire[m].used=0;


						}
					else
						{
						// 当たってないので
						fire[m].info[0]++;
						if( fire[m].info[0]==1 )
							{
							f=seek_effect_no();
							effect[f].layer=LOWER;	
							effect[f].info[0]=30;
							effect[f].info[1]=0;

							effect[f].x=fire[m].x;
							effect[f].y=fire[m].y;

							effect[f].no=8;			// ソースファイル上の番号
							}


						if( fire[m].info[0]>=fire[m].info[2] /*&& paint_effect_on*/ )
							{
							if(!( fire[m].y>MAP_TOP || fire[m].y<MAP_BOTTOM || fire[m].x<MAP_LEFT || fire[m].x>MAP_RIGHT ))
								{
								cm_scrn_x=(int)((fire[m].x+(sprt[UNIT_JPN].wd/2)-MAP_LEFT)/sprt[MAP_TIP_NRML].wd);
								cm_scrn_y=(int)((MAP_TOP-fire[m].y+(sprt[UNIT_JPN].ht/2))/sprt[MAP_TIP_NRML].ht);

								if( cmbt_map[cm_scrn_y][cm_scrn_x]==0)
									{
									// 海の上
									if( !(cc_count%5))
										{
										f=seek_effect_no();
										effect[f].layer=LOWER;	
										effect[f].info[0]=30+my_rnd(25);
										effect[f].info[1]=4;

										effect[f].x=wrk_x;
										effect[f].y=wrk_y;

										effect[f].no=8;			// ソースファイル上の番号
										}
									}
								}

							f=seek_effect_no();
							effect[f].layer=LOWER;	
							effect[f].info[0]=1;
							effect[f].info[1]=0;

							effect[f].x=fire[m].x;
							effect[f].y=fire[m].y;

							effect[f].no=36+drctn_for_8((int)(fire[m].drctn));			// ソースファイル上の番号

							}
						}
					//effect[f].no=72;			// ソースファイル上の番号
					}
				else
					{
					fire[m].used=0;
					}
				}






			// 弾丸
			if( fire[m].kind==BLT )
				{
				if( fire[m].spd>=fire[m].last_spd )
					{
					wrk_x=fire[m].x;
					wrk_y=fire[m].y;
					fire[m].x+=cos(fire[m].drctn*a_PI)*fire[m].spd;
					fire[m].y+=sin(fire[m].drctn*a_PI)*fire[m].spd;
					fire[m].spd+=fire[m].spd_add;

					n=fire[m].used;						// ターゲットナンバー

					// ptin dbg
					wrk_rect.top=(int)unit[n].y+10;//(int)unit[n].y-10;
					wrk_rect.right=(int)unit[n].x+10;
					wrk_rect.bottom=(int)unit[n].y-10;//(int)unit[n].y+10;
					wrk_rect.left=(int)unit[n].x-10;

#if 1
					if( pt_in_rect3(&wrk_rect,(int)fire[m].x,(int)fire[m].y) )
						{
						// 命中
						fire[m].used=0;
						if(!anti_air)
							{
							if( unit[n].kind!=TR1 || rnd(4)==0 ) 
								unit[n].hp[0]-=rtn_damage_pt(m);
							}

						f=seek_effect_no();
						effect[f].layer=UPPER;	

						effect[f].info[0]=20;
						effect[f].info[1]=1;	// アニメーションパターン
						//effect[f].kind=THERE;

						effect[f].x=fire[m].x;
						effect[f].y=fire[m].y;
						effect[f].no=1;			// 弾丸着弾	のソースファイル上の番号
						}
					else
						{
						if( 1/*paint_effect_on*/ )
							{
							// 弾丸描画
							f=seek_effect_no();
							effect[f].layer=UPPER;	
							effect[f].info[0]=1;	effect[f].info[1]=11;
							effect[f].x=fire[m].x;	effect[f].y=fire[m].y;
							effect[f].x2=wrk_x;		effect[f].y2=wrk_y;
							}
						}
#endif

					}
				else
					{
					fire[m].used=0;
					}
				}




			// 対空機関砲
			if( fire[m].kind==RAS )
				{
				fire[m].info[0]--;
				if( fire[m].info[0]/*fire[m].info[1] <= fire[m].info[0]*/ )
					{	//
					wrk_x=fire[m].x;	wrk_y=fire[m].y;
					fire[m].x+=cos(fire[m].drctn*a_PI)*fire[m].spd;
					fire[m].y+=sin(fire[m].drctn*a_PI)*fire[m].spd;

					fire[m].spd+=fire[m].spd_add;		// 弾が減速

					if( 1/*paint_effect_on*/ )
						{
						f=seek_effect_no();
						effect[f].layer=UPPER;	
						effect[f].info[0]=1;	effect[f].info[1]=10;
						effect[f].x=fire[m].x;	effect[f].y=fire[m].y;
						effect[f].x2=wrk_x;		effect[f].y2=wrk_y;

						effect[f].no=0;			// ソースファイル上の番号
						}

					//draw_line5((int)(fire[m].x-cmbt_x),(int)(cmbt_y-fire[m].y),(int)(wrk_x-cmbt_x),(int)(cmbt_y-wrk_y),CMBT_WIDTH-1,CMBT_HEIGHT-1,255);
					//draw_line5((int)(fire[m].x-cmbt_x)+1,(int)(cmbt_y-fire[m].y),(int)(wrk_x-cmbt_x)+1,(int)(cmbt_y-wrk_y),CMBT_WIDTH-1,CMBT_HEIGHT-1,255);
					//draw_line5((int)(fire[m].x-cmbt_x),(int)(cmbt_y-fire[m].y)-1,(int)(wrk_x-cmbt_x),(int)(cmbt_y-wrk_y)-1,CMBT_WIDTH-1,CMBT_HEIGHT-1,255);

					}
				else
					{	// 炸裂！
					fire[m].x+=cos(fire[m].drctn*a_PI)*fire[m].spd;
					fire[m].y+=sin(fire[m].drctn*a_PI)*fire[m].spd;


					// 砲弾炸裂
					f=seek_effect_no();
					effect[f].layer=LOWER;	

					effect[f].info[0]=10;
					effect[f].info[1]=4;	// アニメーションパターン

					effect[f].x=fire[m].x;
					effect[f].y=fire[m].y;
					effect[f].no=0;			// 弾丸着弾	のソースファイル上の番号



					// ptin dbg
					wrk_rect.top=(int)fire[m].y+40;//(int)fire[m].y-40;
					wrk_rect.right=(int)fire[m].x+40;
					wrk_rect.bottom=(int)fire[m].y-40;//(int)fire[m].y+40;
					wrk_rect.left=(int)fire[m].x-40;


					for(n=1;n<=max_unit;n++)
						{
						if( unit[n].used && unit[n].used==unit[fire[m].used].used && unit[n].ctgry==PLANE && unit[n].info[0]==FLYING )
							{
							if( pt_in_rect3(&wrk_rect,(int)unit[n].x,(int)unit[n].y) /*&& unit[n].hp[0]>=unit[m].hp[1]*0.2+1*/ )
								{
								// 命中
								fire[m].used=0;
								if(!anti_air /*&& unit[n].kind==AT1*/ )
									{
									unit[n].hp[0]-=rtn_damage_pt(m);
									if(unit[n].spd<=unit[n].max_spd )
										unit[n].drctn=rnd(360);
									}

								f=seek_effect_no();
								effect[f].layer=UPPER;	

								effect[f].info[0]=20;
								effect[f].info[1]=2;	// アニメーションパターン
								//effect[f].kind=THERE;

								effect[f].x=unit[n].x;
								effect[f].y=unit[n].y;
								effect[f].no=1;			// 弾丸着弾	のソースファイル上の番号
								}
							}
						}
					fire[m].used=0;
					}
				}










			// 対潜水艦爆弾
			if( fire[m].kind==ASB )
				{
				fire[m].info[0]++;
				if( fire[m].info[0]==5 )
					{
					f=seek_effect_no();
					effect[f].layer=LOWER;	
					effect[f].info[0]=30;
					effect[f].info[1]=4;

					effect[f].x=fire[m].x;
					effect[f].y=fire[m].y;

					effect[f].no=7;			// ソースファイル上の番号
					}
				if( fire[m].info[0]==fire[m].info[1] )
					{	// バクハツ！
					//unit[n].hp[0]--;

					f=seek_effect_no();

					SoundPlayEffect( NULL, TPD_HIT1 ,fire[m].x, fire[m].y);

					effect[f].layer=LOWER;	

					effect[f].info[0]=40;
					effect[f].info[1]=4;	// アニメーションパターン

					effect[f].x=fire[m].x;
					effect[f].y=fire[m].y;
					effect[f].no=11;			//ソースファイル上の番号

					h=0;
					for(n=1;n<=max_unit;n++)
						{
						if( unit[n].used  && unit[n].kind==SS1 && unit[n].info[6] )
							{
							fire[m].used=n;
							h=hit_chk(m);
							if( h )
								break;
							}
						}

					if( h )
						{
						// 命中
						unit[n].hp[0]-=rtn_damage_pt(m);
						}
					fire[m].used=0;
					}
				}


			// 対空砲
			if( fire[m].kind==SHL )
				{
				fire[m].info[0]--;
				if( fire[m].info[0]/*fire[m].info[1] <= fire[m].info[0]*/ )
					{	//
					wrk_x=fire[m].x;	wrk_y=fire[m].y;
					fire[m].x+=cos(fire[m].drctn*a_PI)*fire[m].spd;
					fire[m].y+=sin(fire[m].drctn*a_PI)*fire[m].spd;

					fire[m].spd+=fire[m].spd_add;		// 弾が減速
					if( 1/*paint_effect_on*/ )
						{
						// 弾自体の絵
						f=seek_effect_no();
						effect[f].layer=UPPER;	
						effect[f].info[0]=1;	effect[f].info[1]=0;
						effect[f].x=fire[m].x;	effect[f].y=fire[m].y;
						effect[f].no=96+drctn_for_8((int)(fire[m].drctn));			// ソースファイル上の番号
						// 弾の煙
						f=seek_effect_no();
						effect[f].layer=UPPER;	
						effect[f].info[0]=3+my_rnd(3);
						effect[f].info[1]=4;

						effect[f].x=wrk_x+my_rnd(10)-5;
						effect[f].y=wrk_y+my_rnd(10)-5;

						effect[f].no=2;			// ソースファイル上の番号
						}
					}
				else
					{	// 炸裂！
					fire[m].x+=cos(fire[m].drctn*a_PI)*fire[m].spd;
					fire[m].y+=sin(fire[m].drctn*a_PI)*fire[m].spd;





					// 砲弾炸裂
					// 煙
					f=seek_effect_no();
					effect[f].layer=LOWER;	
					//effect[f].kind=THERE;
					effect[f].info[0]=80+rnd(80);
					effect[f].info[1]=2;
					effect[f].x=fire[m].x;
					effect[f].y=fire[m].y;
					effect[f].no=6;			// ソースファイル上の番号

					// 漠炎
					f=seek_effect_no();
					effect[f].layer=LOWER;	
					effect[f].info[0]=10;
					effect[f].info[1]=4;	// アニメーションパターン
					effect[f].x=fire[m].x;
					effect[f].y=fire[m].y;
					effect[f].no=0;			// 弾丸着弾	のソースファイル上の番号






					//fire[m].used=0;
					f=40;
					// ptin dbg
					wrk_rect.top=(int)fire[m].y+f;//(int)fire[m].y-f;
					wrk_rect.right=(int)fire[m].x+f;
					wrk_rect.bottom=(int)fire[m].y-f;//(int)fire[m].y+f;
					wrk_rect.left=(int)fire[m].x-f;


					for(n=1;n<=max_unit;n++)
						{
						if( unit[n].used   && unit[n].used==unit[fire[m].used].used && unit[n].ctgry==PLANE && unit[n].info[0]==FLYING )
							{
							if( pt_in_rect3(&wrk_rect,(int)unit[n].x,(int)unit[n].y) && !( unit[n].kind==BM1 && unit[n].used==USA && rnd(3)!=0 ) )
								{
								// 命中
								/*fire[m].used=0;*/
								if(!anti_air  )
									{
									unit[n].hp[0]-=rtn_damage_pt(m);
	
									if(unit[n].spd<=unit[n].max_spd )
										{
										unit[n].drctn=rnd(360);
										//unit[n].spd=unit[n].spd/3;
										}

									}

								f=seek_effect_no();
								effect[f].layer=UPPER;	

								effect[f].info[0]=20;
								effect[f].info[1]=2;	// アニメーションパターン
								//effect[f].kind=THERE;

								effect[f].x=unit[n].x;
								effect[f].y=unit[n].y;
								effect[f].no=1;			// 弾丸着弾	のソースファイル上の番号
								}
							}
						}
					fire[m].used=0;
					}
				}






			// 艦砲
			if( fire[m].kind==GUN )
				{
				fire[m].info[0]--;
				if( fire[m].info[0] )
					{	//
					wrk_x=fire[m].x;	wrk_y=fire[m].y;
					fire[m].x+=cos(fire[m].drctn*a_PI)*fire[m].spd;
					fire[m].y+=sin(fire[m].drctn*a_PI)*fire[m].spd;
					if( fire[m].info[0] > fire[m].info[1] ) 
						fire[m].spd-=fire[m].spd_add;		// 弾が上昇中
					else
						fire[m].spd+=(fire[m].spd_add*2.83);		// 弾が降下中
					if( 1/*paint_effect_on*/ )
						{
						// 弾自体の絵
						f=seek_effect_no();
						effect[f].layer=UPPER;	
						effect[f].info[0]=1;	effect[f].info[1]=0;
						effect[f].x=fire[m].x;	effect[f].y=fire[m].y;
						effect[f].no=108+drctn_for_8((int)(fire[m].drctn));			// ソースファイル上の番号
						// 弾の煙
						f=seek_effect_no();
						effect[f].layer=UPPER;	
						effect[f].info[0]=3+my_rnd(3);
						effect[f].info[1]=4;

						effect[f].x=wrk_x+my_rnd(10)-5;
						effect[f].y=wrk_y+my_rnd(10)-5;

						effect[f].no=2;			// ソースファイル上の番号
						}
					}
				else
					{	// 着弾！
					fire[m].x+=cos(fire[m].drctn*a_PI)*fire[m].spd;
					fire[m].y+=sin(fire[m].drctn*a_PI)*fire[m].spd;
					n=fire[m].used;
					h=hit_chk(m);
					fire[m].used=0;
					if( unit[n].used && h)
						{
						// 命中
						unit[n].hp[0]-=rtn_damage_pt(m);

						SoundPlayEffect( NULL, TPD_HIT1 ,fire[m].x, fire[m].y);

						f=seek_effect_no();
						effect[f].layer=UPPER;	

						effect[f].info[0]=40;
						effect[f].info[1]=4;	// アニメーションパターン

						effect[f].x=fire[m].x;
						effect[f].y=fire[m].y;

						//effect[f].x=unit[n].x;
						//effect[f].y=unit[n].y;
						effect[f].no=1;			// 弾丸着弾	のソースファイル上の番号


						// 当った的に収納機があれば破壊される場合もある
						if( unit[n].kind==AP || unit[n].kind==CV1 || unit[n].kind==CVL1 )
							{
							for(i=0;i<=max_unit;i++)
								{
								if( unit[i].used && unit[i].ctgry==PLANE && unit[i].info[0]==PARKING && unit[i].info[1]==n && rnd(10)==0 )
									{
									unit[i].used=0;
									unit[unit[i].info[1]].info[1]--;	// 現在格納数
							
									if( unit[i].info[3]>=1 && unit[unit[i].info[1]].info[4]>=1 && unit[i].info[5]<=SLOW )
										unit[unit[i].info[1]].info[4]--;		// 発艦予定の機数を	
									if( unit[i].info[3]>=3 && unit[unit[i].info[1]].info[7]>=1  && unit[i].info[5]<=SLOW )
										unit[unit[i].info[1]].info[7]--;		// 


									if( unit[i].info[5]==RETURN )
										{
										if(unit[unit[i].info[1]].info[7])
											unit[unit[i].info[1]].info[7]=0;	// 着艦、0許可、1不許可
										if(unit[unit[i].info[1]].info[8])
											unit[unit[i].info[1]].info[8]=0;	// その空母の次機発進許可	0許可、1不許可
										}

									if( the_slct_unit==i )
										{ the_slct_unit=0; cmbt_menu_kind=0; cmbt_menu_slctd=0; cls_all_slct_unit_p2(1); }

									break;
									}
								}

							}

						}
					else
						{	
						// ハズレ
						SoundPlayEffect( NULL, SPL1 ,fire[m].x, fire[m].y);

						f=seek_effect_no();
						effect[f].layer=LOWER;	

						effect[f].info[0]=40;
						effect[f].info[1]=4;	// アニメーションパターン

						effect[f].x=fire[m].x;
						effect[f].y=fire[m].y;

						wrk_x=fire[m].x;
						wrk_y=fire[m].y;

						effect[f].no=8;			// 弾丸着弾	のソースファイル上の番号
						if(!( wrk_y>MAP_TOP || wrk_y<MAP_BOTTOM || wrk_x<MAP_LEFT || wrk_x>MAP_RIGHT ))
							{
							cm_scrn_x=(int)((wrk_x+(sprt[UNIT_JPN].wd/2)-MAP_LEFT)/sprt[MAP_TIP_NRML].wd);
							cm_scrn_y=(int)((MAP_TOP-wrk_y+(sprt[UNIT_JPN].ht/2))/sprt[MAP_TIP_NRML].ht);
							if( cmbt_map[cm_scrn_y][cm_scrn_x]>=1)
								effect[f].no=10;			// 弾丸着弾	のソースファイル上の番号
							else
								effect[f].no=8;			// 弾丸着弾	のソースファイル上の番号
							}

						}
					}
				}


			//	魚雷
			if( fire[m].kind==TPD )
				{
				if( fire[m].info[0]<=fire[m].info[1] )
					{
					wrk_x=fire[m].x;
					wrk_y=fire[m].y;
					wrk_x+=cos(fire[m].drctn*a_PI)*-40;
					wrk_y+=sin(fire[m].drctn*a_PI)*-40;
					if(!( wrk_y>MAP_TOP || wrk_y<MAP_BOTTOM || wrk_x<MAP_LEFT || wrk_x>MAP_RIGHT ))
						{
						cm_scrn_x=(int)((wrk_x+(sprt[UNIT_JPN].wd/2)-MAP_LEFT)/sprt[MAP_TIP_NRML].wd);
						cm_scrn_y=(int)((MAP_TOP-wrk_y+(sprt[UNIT_JPN].ht/2))/sprt[MAP_TIP_NRML].ht);
						if( cmbt_map[cm_scrn_y][cm_scrn_x]>=1)
							{
							fire[m].info[0]=fire[m].info[1];
							}
						}



					wrk_x=fire[m].x;
					wrk_y=fire[m].y;
					fire[m].x+=cos(fire[m].drctn*a_PI)*fire[m].spd;
					fire[m].y+=sin(fire[m].drctn*a_PI)*fire[m].spd;
					fire[m].spd+=fire[m].spd_add;
			


					h=0;
					if( fire[m].info[0]>=fire[m].info[2] )
						{
						for(n=1;n<=max_unit;n++)
							{
							if( unit[n].used && unit[n].ctgry==SHIP && unit[n].kind>=BB1 && unit[n].kind<=TR1 && !(unit[n].kind==SS1&&unit[n].info[6]))
								{
								fire[m].used=n;
								h=hit_chk(m);
								if( h )
									break;
								}
							}
						}
			
					if( h )
						{
						// 命中
						fire[m].used=0;
						unit[n].hp[0]-=rtn_damage_pt(m);


						SoundPlayEffect( NULL, TPD_HIT1+rnd(2) ,fire[m].x, fire[m].y);

						f=seek_effect_no();
						effect[f].layer=LOWER;	

						effect[f].info[0]=40;
						effect[f].info[1]=4;	// アニメーションパターン

						effect[f].x=fire[m].x;
						effect[f].y=fire[m].y;
						effect[f].no=11;			// 弾丸着弾	のソースファイル上の番号
						}
					else
						{
						// 当たってないので
						fire[m].info[0]++;
						if( fire[m].info[0]==1 )
							{
							f=seek_effect_no();
							effect[f].layer=LOWER;	
							effect[f].info[0]=30;
							effect[f].info[1]=0;

							effect[f].x=fire[m].x;
							effect[f].y=fire[m].y;

							effect[f].no=8;			// ソースファイル上の番号
							}

						if( fire[m].info[0]>=fire[m].info[2] /*&& paint_effect_on*/ )
							{
							f=seek_effect_no();
							effect[f].layer=LOWER;	
							effect[f].info[0]=1;
							effect[f].info[1]=0;

							effect[f].x=fire[m].x;
							effect[f].y=fire[m].y;

							effect[f].no=72+drctn_for_8((int)(fire[m].drctn));			// ソースファイル上の番号
							if( !(cc_count%3))
								{
								f=seek_effect_no();
								effect[f].layer=LOWER;	
								effect[f].info[0]=30+my_rnd(25);
								effect[f].info[1]=4;

								effect[f].x=wrk_x+my_rnd(10)-5;
								effect[f].y=wrk_y+my_rnd(10)-5;

								effect[f].no=8;			// ソースファイル上の番号
								}
							}
						}
					//effect[f].no=72;			// ソースファイル上の番号
					}
				else
					{
					fire[m].used=0;
					}
				}



			// 爆撃			
			if( fire[m].kind==BOM )
				{
				fire[m].info[0]++;

				if( fire[m].used==AT1 && fire[m].info[0]==12 )
					SoundPlayEffect( NULL, BOMB_OFF ,fire[m].x, fire[m].y);


				if( fire[m].info[0]<=fire[m].info[1]  )
					{	// 爆弾降下中
					if( fire[m].info[0]>=50)
						{
						if( fire[m].info[0]==50)
						SoundPlayEffect( NULL, FALL1 ,fire[m].x, fire[m].y);

						fire[m].x+=cos(fire[m].drctn*a_PI)*fire[m].spd;
						fire[m].y+=sin(fire[m].drctn*a_PI)*fire[m].spd;
						fire[m].spd+=fire[m].spd_add;
						if( 1/*paint_effect_on*/ )
							{
							f=seek_effect_no();
							effect[f].layer=UPPER;	
							effect[f].info[0]=1;	effect[f].info[1]=0;
							effect[f].x=fire[m].x;	effect[f].y=fire[m].y;
							effect[f].no=84+drctn_for_8((int)(fire[m].drctn));			// ソースファイル上の番号
							}
						}
					}
				else
					{	// 着弾！
					fire[m].x+=cos(fire[m].drctn*a_PI)*fire[m].spd;
					fire[m].y+=sin(fire[m].drctn*a_PI)*fire[m].spd;
					h=0;
					//n=fire[m].used;
					for(n=1;n<=max_unit;n++)
						{
						if( unit[n].used && unit[n].ctgry==SHIP && !(unit[n].kind==SS1&&unit[n].info[6]))
							{
							fire[m].used=n;
							h=hit_chk(m);
							if( h )
								break;
							}
						}
					fire[m].used=0;
					if( h )
						{
						// 命中
						unit[n].hp[0]-=rtn_damage_pt(m);

						SoundPlayEffect( NULL, BOM_HIT1+rnd(2) ,fire[m].x, fire[m].y);

						f=seek_effect_no();
						effect[f].layer=UPPER;	

						effect[f].info[0]=40;
						effect[f].info[1]=4;	// アニメーションパターン

						effect[f].x=fire[m].x;
						effect[f].y=fire[m].y;
						//effect[f].x=unit[n].x;
						//effect[f].y=unit[n].y;
						effect[f].no=1;			// 弾丸着弾	のソースファイル上の番号


						// 当った的に収納機があれば破壊される場合もある
						if( unit[n].kind==AP || unit[n].kind==CV1 || unit[n].kind==CVL1 )
							{
							for(i=0;i<=max_unit;i++)
								{
								if( unit[i].used && unit[i].ctgry==PLANE && unit[i].info[0]==PARKING && unit[i].info[1]==n && rnd(10)==0 )
									{
									unit[i].used=0;
									unit[unit[i].info[1]].info[1]--;	// 現在格納数
							
									if( unit[i].info[3]>=1 && unit[unit[i].info[1]].info[4]>=1 && unit[i].info[5]<=SLOW )
										unit[unit[i].info[1]].info[4]--;		// 発艦予定の機数を	
									if( unit[i].info[3]>=3 && unit[unit[i].info[1]].info[7]>=1  && unit[i].info[5]<=SLOW )
										unit[unit[i].info[1]].info[7]--;		// 


									if( /*unit[i].info[3]==1 &&*/ unit[i].info[5]==RETURN )
										{
										if(unit[unit[i].info[1]].info[7])
											unit[unit[i].info[1]].info[7]=0;	// 着艦、0許可、1不許可
										if(unit[unit[i].info[1]].info[8])
											unit[unit[i].info[1]].info[8]=0;	// その空母の次機発進許可	0許可、1不許可
										}


									if( the_slct_unit==i )
										{ the_slct_unit=0; cmbt_menu_kind=0; cmbt_menu_slctd=0; cls_all_slct_unit_p2(1); }
									break;
									}
								}

							}
						}
					else
						{	
						// ハズレ

						f=seek_effect_no();
						effect[f].layer=LOWER;	

						effect[f].info[0]=40;
						effect[f].info[1]=4;	// アニメーションパターン

						effect[f].x=fire[m].x;
						effect[f].y=fire[m].y;
						wrk_x=fire[m].x;
						wrk_y=fire[m].y;
						if(!( wrk_y>MAP_TOP || wrk_y<MAP_BOTTOM || wrk_x<MAP_LEFT || wrk_x>MAP_RIGHT ))
							{
							cm_scrn_x=(int)((wrk_x+(sprt[UNIT_JPN].wd/2)-MAP_LEFT)/sprt[MAP_TIP_NRML].wd);
							cm_scrn_y=(int)((MAP_TOP-wrk_y+(sprt[UNIT_JPN].ht/2))/sprt[MAP_TIP_NRML].ht);
							}
						else
							{
							cm_scrn_x=0;
							cm_scrn_y=0;
							}
	
						if( cmbt_map[cm_scrn_y][cm_scrn_x]>=1)
							{
							effect[f].no=10;			// 着弾	のソースファイル上の番号
							SoundPlayEffect( NULL, BOM_HIT1 ,fire[m].x, fire[m].y);
							}
						else
							{
							effect[f].no=8;			// 着弾	のソースファイル上の番号
							SoundPlayEffect( NULL, SPL1 ,fire[m].x, fire[m].y);
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
void	set_new_ltl_ldr( int m )
	{
	int		n,min_no,f,i;




	// 部下がまだ生きてるか
	min_no=max_unit+1;
	f=0;
	for( n=1; n<=max_unit; n++)
		{
		if( unit[n].used && unit[n].ltl_ldr==m )
			{
			if( unit[min_no].no>unit[n].no || min_no==max_unit+1 )
				min_no=n;

//			unit[n].no--;

			f++;
			}
		}



	if( f>=2 )
		{


		// 部下が生存
		unit[min_no].no=0;
		unit[min_no].is_ltl_ldr=f;
		unit[min_no].ltl_ldr=0;

		if( unit[min_no].kind==FT1 && (unit[m].kind==AT1 || unit[m].kind==BM1) && unit[min_no].info[0]==FLYING )
			unit[min_no].info[5]=RETURN;		// それまでの隊長がボスだったらきかんしよっと



		for( n=1; n<=max_unit; n++)
			{
			if( unit[n].used && unit[n].ltl_ldr==m )
				{
				unit[n].ltl_ldr=min_no;			// ｍｉｎ＿ｎｏが新しい隊長機

				if( unit[n].kind==FT1 && (unit[m].kind==AT1 || unit[m].kind==BM1) && unit[n].info[0]==FLYING )
					unit[n].info[5]=RETURN;		// それまでの隊長がボスだったらきかんしよっと

				}
			}
		// ｐｐ＿ｘ、ｙをコピーします。
		for(i=0;i<=63;i++)
			{
			unit[min_no].pp_x[i]=unit[m].pp_x[i];
			unit[min_no].pp_y[i]=unit[m].pp_y[i];
			}
		}



	else if( f==1 )
		{
		// 部下がひとつ
		unit[min_no].no=0;
		unit[min_no].is_ltl_ldr=0;
		unit[min_no].ltl_ldr=0;
		}
	}








//============================================================================
// 
//----------------------------------------------------------------------------
void	cont_unit_effect( int m )
	{
	RECT	wrk_rect;
	int		f,i,n;
	double	wrk_x2,wrk_y2,drctn,dstc;
	int		cm_scrn_x,cm_scrn_y;





	if( unit[m].is_ltl_ldr && unit[m].used==your_side && unit[m].info[0]!=PARKING /*&& paint_effect_on*/ )
		{
		f=seek_effect_no();
		effect[f].layer=UPPER;	
		effect[f].info[0]=1;
		effect[f].info[1]=0;
		effect[f].x=unit[m].x;
		if( unit[m].kind==AT1 || unit[m].kind==FT1 || unit[m].kind==DD1 || unit[m].kind==SS1)
			effect[f].y=unit[m].y+30.0;
		else
			effect[f].y=unit[m].y+35.0;

		// 編隊長の旗
		effect[f].no=24;			// ソースファイル上の番号
		}


	// 航空機のユニットエフェクト
	if ( unit[m].ctgry==PLANE )
		{
		// 武装の表示
		if( /*paint_effect_on &&*/ unit[m].arm[1] && !(unit[m].arm[3] && !(FrameCount%3))&& (unit[m].arm[0]==BOM || unit[m].arm[0]==TPD || unit[m].arm[0]==TUN || unit[m].arm[0]==NTG)  && unit[m].used==your_side && !(unit[m].info[0]==PARKING && unit_info[1]==0) && !( unit[m].info[0]==PARKING && unit[m].info[3]>=3 ) && !( unit[m].info[0]==PARKING && unit[m].info[1]!=unit_info[3]))
			{
			f=seek_effect_no();
			effect[f].layer=LOWER;	
			effect[f].info[0]=1;
			if( unit[m].info[0]==FLYING )
				{
				effect[f].info[1]=0;
				effect[f].x=unit[m].x;
				effect[f].y=unit[m].y-25;
				}
			else
				{
				effect[f].info[1]=3;
				effect[f].x=unit[m].x;
				effect[f].y=unit[m].y+25;
				}
			switch( unit[m].arm[0] )
				{
				case TPD:
					effect[f].no=3;			// ソースファイル上の番号
					break;
				case BOM:
					effect[f].no=4;			// ソースファイル上の番号
					break;
				case TUN:	case NTG:
					effect[f].no=15;			// ソースファイル上の番号
					break;
				}
			}


		if( unit[m].info[0]==PARKING )
			{	// 収容後のエフェクト

			}
		else
			{	// 飛行中のエフェクト
			if( unit[m].hp[0]<=0 )
				{	// 墜落
				unit[m].used=0;


				if( unit_info[3]==m )
					unit_info[0]=0;				// ユニットインフォをクリア


				f=seek_effect_no();
				effect[f].layer=LOWER;	
				//effect[f].kind=THERE;
				effect[f].info[0]=80;

				effect[f].x=unit[m].x;
				effect[f].y=unit[m].y;

				wrk_x2=unit[m].x;
				wrk_y2=unit[m].y;
				if(!( wrk_y2>MAP_TOP || wrk_y2<MAP_BOTTOM || wrk_x2<MAP_LEFT || wrk_x2>MAP_RIGHT ))
					{
					cm_scrn_x=(int)((wrk_x2+(sprt[UNIT_JPN].wd/2)-MAP_LEFT)/sprt[MAP_TIP_NRML].wd);
					cm_scrn_y=(int)((MAP_TOP-wrk_y2+(sprt[UNIT_JPN].ht/2))/sprt[MAP_TIP_NRML].ht);
					}
				else
					{
					cm_scrn_x=0;
					cm_scrn_y=0;
					}
	
				if( cmbt_map[cm_scrn_y][cm_scrn_x]>=1)
					{
					effect[f].no=9;			// ソースファイル上の番号
					effect[f].info[1]=4;
					}
				else
					{
					effect[f].no=7;			// ソースファイル上の番号
					effect[f].info[1]=0;
					}


				if( the_slct_unit==m )
					{
					the_slct_unit=0;
					cmbt_menu_kind=0; cmbt_menu_slctd=0; cls_all_slct_unit_p2(1);
					}

				if( unit[m].is_ltl_ldr )
					set_new_ltl_ldr(m);					// 爆砕されたのがＬＤＲなら、新しいのを決めます。


				}
			else if( unit[m].hp[0]<=unit[m].hp[1]*0.2 )
				{
				// ＨＰはあるが、事実上の墜落、
				if( unit[m].hp[0]==unit[m].hp[1]*0.2 )
					{
					// 飛行機が火を吹く
					if(unit[m].kind==BM1)
						{
						for(i=0;i<3;i++)
							{
							f=seek_effect_no();
							if( f )
								{
								effect[f].layer=UPPER;	
								effect[f].info[0]=20+my_rnd(20);
								effect[f].info[1]=4;
								effect[f].x=unit[m].x+20-my_rnd(40);
								effect[f].y=unit[m].y+20-my_rnd(40);
								effect[f].no=9;				// ソースファイル上の番号	
								}
							}
						}
					else
						{
						f=seek_effect_no();			
						effect[f].layer=UPPER;	
						effect[f].info[0]=20+my_rnd(20);
						effect[f].info[1]=4;
						effect[f].x=unit[m].x;
						effect[f].y=unit[m].y;
						effect[f].no=9;				// ソースファイル上の番号	
						}
					if(map_edit==0)
						unit[m].hp[0]--;
					}
				else
					{
					if( rnd(80)==0 )
						{
						if(map_edit==0)
							unit[m].hp[0]--;
						}

					if( rnd(5)!=0 && !(cc_count%(10)) )
						{
						f=seek_effect_no();
						effect[f].layer=UPPER;	
						//effect[f].kind=THERE;
						effect[f].info[0]=40+my_rnd(15);
						effect[f].info[1]=2;
						effect[f].x=unit[m].x;
						effect[f].y=unit[m].y;
						effect[f].no=6;			// ソースファイル上の番号
						}


					if( rnd(3)==0 && unit[m].hp[0]<=unit[m].hp[1]*0.1 )
						{
						// 小爆炎
						f=seek_effect_no();			
						effect[f].layer=UPPER;	
						effect[f].info[0]=8+my_rnd(6);
						effect[f].info[1]=4;
						effect[f].x=unit[m].x+my_rnd(6)-3;
						effect[f].y=unit[m].y+my_rnd(6)-3;
						effect[f].no=10;			// ソースファイル上の番号	
						}
					}
				}
			else if( unit[m].hp[0]<=unit[m].hp[1]*0.3  )
				{
				if( rnd(500)==0 )
					{
					if(map_edit==0)
						unit[m].hp[0]--;
					}

				if( rnd(3)!=0 && !(cc_count%(10) ) )
					{
					f=seek_effect_no();
					effect[f].layer=UPPER;	
					//effect[f].kind=THERE;
					effect[f].info[0]=40+my_rnd(15);
					effect[f].info[1]=2;
					effect[f].x=unit[m].x;
					effect[f].y=unit[m].y;
					effect[f].no=6;			// ソースファイル上の番号


					if( rnd(5)==0 )
						{
						// 小爆炎
						f=seek_effect_no();			
						effect[f].layer=UPPER;	
						effect[f].info[0]=8+my_rnd(6);
						effect[f].info[1]=4;
						effect[f].x=unit[m].x+my_rnd(6)-3;
						effect[f].y=unit[m].y+my_rnd(6)-3;
						effect[f].no=10;			// ソースファイル上の番号	
						}
					}


				}
			else if( unit[m].hp[0]<=unit[m].hp[1]*0.5  )
				{
				if( rnd(500)==0 )
					{
					if(map_edit==0)
						unit[m].hp[0]--;
					}
				if( rnd(2)==1 && !(cc_count%10) )
					{
					f=seek_effect_no();
					effect[f].layer=UPPER;	
					//effect[f].kind=THERE;
					effect[f].info[0]=40+my_rnd(15);
					effect[f].info[1]=2;

					effect[f].x=unit[m].x;
					effect[f].y=unit[m].y;
					effect[f].no=6;			// ソースファイル上の番号

					if( rnd(7)==0 )
						{
						// 小爆炎
						f=seek_effect_no();			
						effect[f].layer=UPPER;	
						effect[f].info[0]=8+my_rnd(6);
						effect[f].info[1]=4;
						effect[f].x=unit[m].x+my_rnd(6)-3;
						effect[f].y=unit[m].y+my_rnd(6)-3;
						effect[f].no=10;			// ソースファイル上の番号	
						}
					}

				}
			else if( unit[m].hp[0]<=unit[m].hp[1]*0.7  )
				{

				if( rnd(8)==0 && !(cc_count%10) )
					{
					f=seek_effect_no();
					effect[f].layer=UPPER;	
					//effect[f].kind=THERE;
					effect[f].info[0]=40+my_rnd(15);
					effect[f].info[1]=2;

					effect[f].x=unit[m].x;
					effect[f].y=unit[m].y;
					effect[f].no=6;			// ソースファイル上の番号


					}
				}
			}
		}


	// 艦船のエフェクト
	if( unit[m].ctgry==SHIP  )
		{
		// 修理と補給中の表示
		if( /*paint_effect_on &&*/ unit[m].spry && (FrameCount%2) && unit[m].used==your_side )
			{
			f=seek_effect_no();
			effect[f].layer=LOWER;	
			effect[f].info[0]=1;

			effect[f].info[1]=0;
			effect[f].x=unit[m].x;
			effect[f].y=unit[m].y-25.0;

			effect[f].no=15;			// ソースファイル上の番号
			}

		// 武装の表示
// 弾薬の消費サイズ
//		if( ( unit[m].used==JPN && ( unit[m].kind==SS1 || unit[m].kind==DD1 || unit[m].kind==CA1 ) || unit[m].used==USA && ( unit[m].kind==SS1 || unit[m].kind==DD1 ) ) && unit[m].spry==0 && unit[m].arm[1] && !(unit[m].arm[3] && (FrameCount%2)) && unit[m].arm[1]>=1 && unit[m].used==your_side )




		if( ( ( unit[m].used==JPN && unit[m].kind==BB1 ) || ( unit[m].used==USA && unit[m].kind==CV1 ) ) && unit[m].type==1 && unit[m].used==your_side )
			{
			// 大和級とエセックス
			f=seek_effect_no();
			effect[f].layer=LOWER;	
			effect[f].info[0]=1;

			effect[f].info[1]=0;
			effect[f].x=unit[m].x+18;
			effect[f].y=unit[m].y+25.0;

			effect[f].no=27;			// ソースファイル上の番号
			}
		else if( unit[m].kind==DD1 && unit[m].type==1 /*&& unit[m].spry==0 /*&& unit[m].arm[1]>=1*/ && unit[m].used==your_side )
			{
			// 対潜駆逐艦
			f=seek_effect_no();
			effect[f].layer=LOWER;	
			effect[f].info[0]=1;

			effect[f].info[1]=0;
			effect[f].x=unit[m].x+18;
			effect[f].y=unit[m].y+25.0;

			effect[f].no=26;			// ソースファイル上の番号
			}
		else if( unit[m].kind==CA1 && unit[m].type==1 /*&& unit[m].spry==0 /*&& unit[m].arm[1]>=1*/ && unit[m].used==your_side )
			{
			// 防空巡洋艦
			f=seek_effect_no();
			effect[f].layer=LOWER;	
			effect[f].info[0]=1;

			effect[f].info[1]=0;
			effect[f].x=unit[m].x+18;
			effect[f].y=unit[m].y+25.0;

			effect[f].no=16;			// ソースファイル上の番号
			}
		else if( ( unit[m].used==JPN && ( unit[m].kind==SS1 || unit[m].kind==DD1 || unit[m].kind==CA1 ) || unit[m].used==USA && ( unit[m].kind==SS1 || unit[m].kind==DD1 ) ) && unit[m].spry==0 && unit[m].arm[1] && !(unit[m].arm[3] && (FrameCount%2)) && unit[m].arm[1]>=1 && unit[m].used==your_side )
			{
			f=seek_effect_no();
			effect[f].layer=LOWER;	
			effect[f].info[0]=1;

			effect[f].info[1]=0;
			effect[f].x=unit[m].x;
			effect[f].y=unit[m].y-25.0;

			effect[f].no=3;			// ソースファイル上の番号
			}


		// トランスポーターの荷物の表示 
		if( /*paint_effect_on &&*/ unit[m].kind==TR1 && unit[m].spry==0 && unit[m].arm[1] && !(unit[m].arm[3] && (FrameCount%2)) && unit[m].used==your_side )
			{
			f=seek_effect_no();
			effect[f].layer=LOWER;	
			effect[f].info[0]=1;

			effect[f].info[1]=0;
			effect[f].x=unit[m].x;
			effect[f].y=unit[m].y-25.0;

			//switch( unit[m].arm[0] )
			//	{
			//	case TPD:
					effect[f].no=44;			// ソースファイル上の番号
			//		break;
			//	}
			}


//TR_GF1

		// およその敵潜航潜水艦
		if( /*paint_effect_on &&*/ unit[m].used!=your_side && unit[m].kind==SS1 && unit[m].info[6] && unit[m].info[10] )
			{
			f=seek_effect_no();
			effect[f].layer=UPPER;
			effect[f].info[0]=1;
			effect[f].info[1]=0;
			effect[f].x=unit[m].info[7];
			effect[f].y=unit[m].info[8];

			effect[f].no=60;			// ソースファイル上の番号



			f=seek_effect_no();
			effect[f].layer=LOWER;
			effect[f].info[0]=1;
			effect[f].info[1]=12;
			effect[f].x=unit[m].info[7];
			effect[f].y=unit[m].info[8];
			effect[f].x2=unit[m].info[9];
			effect[f].y2=unit[m].info[9];
/*
			wrk_rect.top=(int)effect[f].y-unit[m].info[9];
			wrk_rect.right=(int)effect[f].x+unit[m].info[9];
			wrk_rect.bottom=(int)effect[f].y+unit[m].info[9];
			wrk_rect.left=(int)effect[f].x-unit[m].info[9];
			draw_line4((int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.top),(int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
			draw_line4((int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.top),(int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
			draw_line4((int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),(int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
			draw_line4((int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),(int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
*/
			}


		if( unit[m].kind==SS1 && unit[m].info[6] )
			{	// 潜航中潜水艦
			if( unit[m].hp[0]<=0 )
				{	// 沈没
				unit[m].used=0;
				unit[m].found=0;


				if( unit_info[3]==m )
					unit_info[0]=0;				// ユニットインフォをクリア


				if( the_slct_unit==m )
					{
					the_slct_unit=0;
					cmbt_menu_kind=0; cmbt_menu_slctd=0; cls_all_slct_unit_p2(1);
					}
				}
			else
				{
				if( unit[m].hp[0]<=/*unit[m].hp[2]*/unit[m].hp[1]*0.2 )
					{	// 空気漏れ
					if( rnd(100)==0 )
						{
						if(map_edit==0)
							unit[m].hp[0]--;
						}
					if( rnd(300)==1 /*&& paint_effect_on*/ )
						{
						for(n=0;n<=3;n++)
							{
							f=seek_effect_no();			
							if( f )
								{
								effect[f].layer=LOWER;	
								effect[f].info[0]=100+my_rnd(20);
								effect[f].info[1]=4;
								effect[f].x=unit[m].x+my_rnd(40)-20;
								effect[f].y=unit[m].y+my_rnd(40)-20;
								effect[f].no=8;			// ソースファイル上の番号	
								}
							}
						// ついでに発見される
						unit[m].info[7]=unit[m].x;
						unit[m].info[8]=unit[m].y;

						unit[m].info[9]=100;
						unit[m].info[10]=400;

						unit[m].found=1;
						}
					}
				else
					{
					if( unit[m].hp[0]<=unit[m].hp[1]*0.5 )
						{	// 空気漏れ
						if( rnd(1000)==0 )
							{
							if(map_edit==0)
								unit[m].hp[0]--;
							}
						if( rnd(600)==1 /*&& paint_effect_on*/ )
							{
							for(n=0;n<=2;n++)
								{
								f=seek_effect_no();			
								if( f)
									{
									effect[f].layer=LOWER;	
									effect[f].info[0]=100+my_rnd(20);
									effect[f].info[1]=4;
									effect[f].x=unit[m].x+my_rnd(40)-20;
									effect[f].y=unit[m].y+my_rnd(40)-20;
									effect[f].no=8;			// ソースファイル上の番号	
									}
								}

							// ついでに発見される
							unit[m].info[7]=unit[m].x;
							unit[m].info[8]=unit[m].y;

							unit[m].info[9]=100;
							unit[m].info[10]=400;

							unit[m].found=1;
							}
						}
					}
				}
			}
		else
			{	// 水上艦船
			if( unit[m].hp[0]<=0 )
				{	// 沈没

				SoundPlayEffect( NULL, SHIP_SINK1 ,unit[m].x, unit[m].y);

				unit[m].used=0;

		
				if( unit_info[3]==m )
					unit_info[0]=0;

				// 空母なら艦載機とユニットインフォを
				if( unit[m].kind==CV1 || unit[m].kind==CVL1 || unit[m].kind==AP )
					{
					for( i=1;i<=max_unit;i++)
						{
						if( unit[i].used && unit[i].ctgry==PLANE && unit[i].info[0]==PARKING && unit[i].info[1]==m)
							{
							unit[i].used=0;
							if( the_slct_unit==i)
								{
								the_slct_unit=0;
								cmbt_menu_kind=0; cmbt_menu_slctd=0; cls_all_slct_unit_p2(1);
								}
							}
						}
					}



				be_dstryd(m);

				if( the_slct_unit==m )
					{
					the_slct_unit=0;
					cmbt_menu_kind=0; cmbt_menu_slctd=0; cls_all_slct_unit_p2(1);
					}


				}
			else 
				{
				if( unit[m].hp[0]<=unit[m].hp[1]*0.2  )
					{
					if( rnd(4000)==0 && unit[m].spry==0 )
						{
						if(map_edit==0)
							unit[m].hp[0]--;
						}
					if( rnd(2) /*&& paint_effect_on*/ )
						{
						f=seek_effect_no();
						effect[f].layer=UPPER;	
						effect[f].info[0]=1;
						effect[f].info[1]=4;

						effect[f].x=unit[m].x;
						effect[f].y=unit[m].y;
						effect[f].no=9;			// ソースファイル上の番号
						}
					}
				else
					{
					if( unit[m].hp[0]<=unit[m].hp[1]*0.5  )
						{
						n=rnd(4);
						if( /*paint_effect_on &&*/ n==0 )
							{
							f=seek_effect_no();
							effect[f].layer=UPPER;	
							effect[f].info[0]=1;	effect[f].info[1]=4;
							effect[f].x=unit[m].x;	effect[f].y=unit[m].y;
							effect[f].no=9;			// ソースファイル上の番号
							}
						if( /*paint_effect_on &&*/ n==1 )
							{
							f=seek_effect_no();
							effect[f].layer=UPPER;	
							effect[f].info[0]=1;	effect[f].info[1]=4;
							effect[f].x=unit[m].x;	effect[f].y=unit[m].y;
							effect[f].no=10;			// ソースファイル上の番号
							}
						}
					else
						{
						if( rnd(10)==1 /*&& paint_effect_on*/ && unit[m].hp[0]<=unit[m].hp[1]*0.7 )
							{
							f=seek_effect_no();
							effect[f].layer=UPPER;	
							effect[f].info[0]=1;	effect[f].info[1]=4;
							effect[f].x=unit[m].x;	effect[f].y=unit[m].y;
							effect[f].no=10;			// ソースファイル上の番号
							}
						}
					}
				}
			}


		// 航跡のエフェクト
		if( !(unit[m].kind==SS1||unit[m].kind==SP||unit[m].kind==AP||unit[m].kind==CT1||unit[m].kind==MN1||unit[m].kind==GF1||unit[m].kind==GF2||unit[m].kind==GF3) && (cc_count%15)==0 && unit[m].spd>=unit[m].max_spd/3 )
			{
			// 航跡のエフェクトを残す
			f=seek_effect_no();			
			effect[f].layer=LOWER;	
			effect[f].info[0]=60+my_rnd(60);
			effect[f].info[1]=4;
			effect[f].x=unit[m].x;
			effect[f].y=unit[m].y;

			drctn=unit[m].drctn;
			drctn+=180;
			drctn=(int)drctn%360;

			switch(unit[m].kind)
				{
				case BB1:	case CV1:
					dstc=26;
					break;

				case CA1:	case CVL1:
					dstc=22;
					break;
		
				default:
					dstc=16;
					break;
				}


			effect[f].x+=cos(drctn*a_PI)*dstc;
			effect[f].y+=sin(drctn*a_PI)*dstc;

			effect[f].no=8;			// ソースファイル上の番号	
			}
		}


	// 艦船のエフェクト
	if( unit[m].kind==GF1 || unit[m].kind==GF2 || unit[m].kind==GF3 || unit[m].kind==AP || unit[m].kind==GF1 || unit[m].kind==SP )
		{
		// 建設工事中
		if( /*paint_effect_on &&*/ unit[m].info[0] && (FrameCount%2) && unit[m].used==your_side )
			{
			f=seek_effect_no();
//			effect[f].layer=LOWER;	
			effect[f].layer=UPPER;	
			effect[f].info[0]=1;

			effect[f].info[1]=0;
			effect[f].x=unit[m].x;
			effect[f].y=unit[m].y-25.0;

			effect[f].no=15;			// ソースファイル上の番号
			}
		}


	}






//============================================================================
//	
//----------------------------------------------------------------------------
void	edit_now( void )
	{
    char ach[128];
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

		for(i=1;i<=max_unit;i++)
			{
			if( unit[i].used )
				{
				if( unit[i].used==JPN)
					{
					if( unit[i].ctgry==PLANE )
						jp_plane++;
					else
						jp_ship++;
					}
				else
					{
					if( unit[i].ctgry==PLANE )
						us_plane++;
					else
						us_ship++;
					}
				}
			}



#if LNGG_VER==0
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






#if LNGG_VER==0


		// 配置ユニット
		switch(put_trgt)
			{
			case 1:
				len= wsprintf(ach, "戦艦",10);
				put_kind=BB1;
				put_kind_sub=0;
				break;
			case 2:
				len= wsprintf(ach, "巡洋艦",10);
				put_kind=CA1;
				put_kind_sub=0;
				break;
			case 3:
				len= wsprintf(ach, "駆逐艦",10);
				put_kind=DD1;
				put_kind_sub=0;
				break;
			case 4:
				len= wsprintf(ach, "潜水艦",10);
				put_kind=SS1;
				put_kind_sub=0;
				break;
			case 5:
				len= wsprintf(ach, "正規空母",10);
				put_kind=CV1;
				put_kind_sub=0;
				break;
			case 6:
				len= wsprintf(ach, "軽空母",10);
				put_kind=CVL1;
				put_kind_sub=0;
				break;
			case 7:
				len = wsprintf(ach, "輸送船(歩兵基地)",10);
				put_kind=TR1;
				put_kind_sub=TR_GF1;
				break;
			case 8:
				len = wsprintf(ach, "輸送船(トーチカ群)",10);
				put_kind=TR1;
				put_kind_sub=TR_GF2;
				break;
			case 9:
				len = wsprintf(ach, "輸送船(要塞)",10);
				put_kind=TR1;
				put_kind_sub=TR_GF3;
				break;
			case 10:
				len = wsprintf(ach, "輸送船(航空基地)",10);
				put_kind=TR1;
				put_kind_sub=TR_AP;
				break;
			case 11:
				len = wsprintf(ach, "輸送船(軍港)",10);
				put_kind=TR1;
				put_kind_sub=TR_SP;
				break;


			case 12:
				len= wsprintf(ach, "軍港",10);
				put_kind=SP;
				put_kind_sub=0;
				break;
			case 13:
				len= wsprintf(ach, "航空基地",10);
				put_kind=AP;
				put_kind_sub=0;
				break;
			case 14:
				len= wsprintf(ach, "都市",10);
				put_kind=CT1;
				put_kind_sub=0;
				break;
			case 15:
				len= wsprintf(ach, "歩兵基地",10);
				put_kind=GF1;
				put_kind_sub=0;
				break;
			case 16:
				len= wsprintf(ach, "トーチカ群",10);
				put_kind=GF2;
				put_kind_sub=0;
				break;
			case 17:
				len= wsprintf(ach, "要塞",10);
				put_kind=GF3;
				put_kind_sub=0;
				break;



			case 18:
				len = wsprintf(ach, "戦闘機",10);
				put_kind=FT1;
				put_kind_sub=0;
				break;
			case 19:
				len = wsprintf(ach, "陸上戦闘機",10);
				put_kind=FT1;
				put_kind_sub=1;
				break;
			case 20:
				len= wsprintf(ach, "攻撃機",10);
				put_kind=AT1;
				put_kind_sub=0;
				break;
			case 21:
				len= wsprintf(ach, "戦略爆撃機",10);
				put_kind=BM1;
				put_kind_sub=0;
				break;


			case 22:
				len= wsprintf(ach, "防空巡洋艦",10);
				put_kind=CA1;
				put_kind_sub=1;
				break;
			case 23:
				len= wsprintf(ach, "対潜駆逐艦",10);
				put_kind=DD1;
				put_kind_sub=1;
				break;
			case 24:
				if( your_side==JPN )
					{
					len= wsprintf(ach, "大和級戦艦",10);
					put_kind=BB1;
					}
				else
					{
					len= wsprintf(ach, "エセックス型空母",10);
					put_kind=CV1;
					}
				put_kind_sub=1;
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

		switch( rein[your_side] )
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
				put_kind=BB1;
				put_kind_sub=0;
				break;
			case 2:
				len= wsprintf(ach, "Cruiser",10);
				put_kind=CA1;
				put_kind_sub=0;
				break;
			case 3:
				len= wsprintf(ach, "Destroyer",10);
				put_kind=DD1;
				put_kind_sub=0;
				break;
			case 4:
				len= wsprintf(ach, "Submarine",10);
				put_kind=SS1;
				put_kind_sub=0;
				break;
			case 5:
				len= wsprintf(ach, "Carrier",10);
				put_kind=CV1;
				put_kind_sub=0;
				break;
			case 6:
				len= wsprintf(ach, "Lt.Carrier",10);
				put_kind=CVL1;
				put_kind_sub=0;
				break;
			case 7:
				len = wsprintf(ach, "Transport(Trenchies)",10);
				put_kind=TR1;
				put_kind_sub=TR_GF1;
				break;
			case 8:
				len = wsprintf(ach, "Transport(Pillboxes)",10);
				put_kind=TR1;
				put_kind_sub=TR_GF2;
				break;
			case 9:
				len = wsprintf(ach, "Transport(Fortress)",10);
				put_kind=TR1;
				put_kind_sub=TR_GF3;
				break;
			case 10:
				len = wsprintf(ach, "Transport(Airfield)",10);
				put_kind=TR1;
				put_kind_sub=TR_AP;
				break;
			case 11:
				len = wsprintf(ach, "Transport(port)",10);
				put_kind=TR1;
				put_kind_sub=TR_SP;
				break;


			case 12:
				len= wsprintf(ach, "Military Port",10);
				put_kind=SP;
				put_kind_sub=0;
				break;
			case 13:
				len= wsprintf(ach, "Airfield",10);
				put_kind=AP;
				put_kind_sub=0;
				break;
			case 14:
				len= wsprintf(ach, "City",10);
				put_kind=CT1;
				put_kind_sub=0;
				break;
			case 15:
				len= wsprintf(ach, "Trenchies",10);
				put_kind=GF1;
				put_kind_sub=0;
				break;
			case 16:
				len= wsprintf(ach, "Pillboxes",10);
				put_kind=GF2;
				put_kind_sub=0;
				break;
			case 17:
				len= wsprintf(ach, "Fortress",10);
				put_kind=GF3;
				put_kind_sub=0;
				break;



			case 18:
				len = wsprintf(ach, "Car.Fighter",10);
				put_kind=FT1;
				put_kind_sub=0;
				break;
			case 19:
				len = wsprintf(ach, "Grn.Fighter",10);
				put_kind=FT1;
				put_kind_sub=1;
				break;
			case 20:
				len= wsprintf(ach, "Car.Bomber",10);
				put_kind=AT1;
				put_kind_sub=0;
				break;
			case 21:
				len= wsprintf(ach, "Bomber",10);
				put_kind=BM1;
				put_kind_sub=0;
				break;


			case 22:
				len= wsprintf(ach, "AntiAir Cruiser",10);
				put_kind=CA1;
				put_kind_sub=1;
				break;
			case 23:
				len= wsprintf(ach, "AntiSub Destroyer",10);
				put_kind=DD1;
				put_kind_sub=1;
				break;
			case 24:
				if( your_side==JPN )
					{
					len= wsprintf(ach, "Class Yamato",10);
					put_kind=BB1;
					}
				else
					{
					len= wsprintf(ach, "Type Essex",10);
					put_kind=CV1;
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

		switch( rein[your_side] )
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









