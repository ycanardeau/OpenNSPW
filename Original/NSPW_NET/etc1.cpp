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
// 当たりチェック
//----------------------------------------------------------------------------
int		hit_chk(int m)
	{
	int		n,h,j,j2,f,i;
	RECT	wrk_rect;

	h=0;

	n=fire[m].used;						// ターゲットナンバー
	switch( unit[n].kind )
		{
		case BB1:	j=16;j2=j/2;	break;
		case CA1:	j=12;j2=j/2;	break;
		case DD1:	j=10;j2=j/2;		break;
		case SS1:	
			if( unit[n].info[6] )// 潜航中、あたりがでかくなる
				{
				// ptin dbg
				wrk_rect.top=(int)unit[n].y+50;//(int)unit[n].y-50;
				wrk_rect.right=(int)unit[n].x+50;
				wrk_rect.bottom=(int)unit[n].y-50;//(int)unit[n].y+50;
				wrk_rect.left=(int)unit[n].x-50;

				if( pt_in_rect3(&wrk_rect,(int)fire[m].x,(int)fire[m].y) )
					h=1;
				return	(h);			
				}	
			else
				{	j=8/*4*/;j2=j/2;		}

				break;
		case CV1:	j=14;j2=j/2;	break;
		case CVL1:	j=12;j2=j/2;	break;
		case AP:
		case SP:
		case GF3:	case GF2:	case	GF1:
		case CT1:	case MN1:
				// ptin dbg
				wrk_rect.top=(int)unit[n].y+30;//(int)unit[n].y-30;
				wrk_rect.right=(int)unit[n].x+30;
				wrk_rect.bottom=(int)unit[n].y-30;//(int)unit[n].y+30;
				wrk_rect.left=(int)unit[n].x-30;

				if( pt_in_rect3(&wrk_rect,(int)fire[m].x,(int)fire[m].y) )
					h=1;

				return	(h);			
				break;
		case TR1:	j=12;j2=j/2;	break;
		}
	h=0;
	f=drctn_for_8((int)(unit[n].drctn));
	switch( f )
		{
		case 3: case 7:
			for( i=0; i<=4 && !h ; i++)
				{
				// ptin dbg
				wrk_rect.top=(int)unit[n].y+(-j+(i*j2))+j2;//(int)unit[n].y+(-j+(i*j2))-j2;
				wrk_rect.right=(int)unit[n].x+(+j-(i*j2))+j2;
				wrk_rect.bottom=(int)unit[n].y+(-j+(i*j2))-j2;//(int)unit[n].y+(-j+(i*j2))+j2;
				wrk_rect.left=(int)unit[n].x+(+j-(i*j2))-j2;


				if( pt_in_rect3(&wrk_rect,(int)fire[m].x,(int)fire[m].y) )
					h=1;
				}
			break;
		case 1: case 5:
			for( i=0; i<=4 && !h ; i++)
				{
				// pt in dbg
				wrk_rect.top=(int)unit[n].y+(-j+(i*j2))+j2;//(int)unit[n].y+(-j+(i*j2))-j2;
				wrk_rect.right=(int)unit[n].x+(-j+(i*j2))+j2;
				wrk_rect.bottom=(int)unit[n].y+(-j+(i*j2))-j2;//(int)unit[n].y+(-j+(i*j2))+j2;
				wrk_rect.left=(int)unit[n].x+(-j+(i*j2))-j2;


				if( pt_in_rect3(&wrk_rect,(int)fire[m].x,(int)fire[m].y) )
					h=1;
				}

			break;
		case 0: case 4:
			for( i=0; i<=2  && !h ; i++)
				{
				// ptin dbg
				wrk_rect.top=(int)unit[n].y+(-j+(i*j))+j2;//(int)unit[n].y+(-j+(i*j))-j2;
				wrk_rect.right=(int)unit[n].x+j2;
				wrk_rect.bottom=(int)unit[n].y+(-j+(i*j))-j2;//(int)unit[n].y+(-j+(i*j))+j2;
				wrk_rect.left=(int)unit[n].x-j2;

				if( pt_in_rect3(&wrk_rect,(int)fire[m].x,(int)fire[m].y) )
					h=1;
				}
			break;
		case 2: case 6:
			for( i=0; i<=2  && !h ; i++)
				{
				// ptin dbg
				wrk_rect.top=(int)unit[n].y+j2;//(int)unit[n].y-j2;
				wrk_rect.right=(int)unit[n].x+(-j+(i*j))+j2;
				wrk_rect.bottom=(int)unit[n].y-j2;//(int)unit[n].y+j2;
				wrk_rect.left=(int)unit[n].x+(-j+(i*j))-j2;

				if( pt_in_rect3(&wrk_rect,(int)fire[m].x,(int)fire[m].y) )
					h=1;
				}
			break;
		}


	//h=0;
	return	(h);
	}





//============================================================================
// 当たりチェック
//----------------------------------------------------------------------------
void		draw_hit_area(int m)
	{
	int		n,h,j,j2,f,i;
	RECT	wrk_rect;

//	h=0;

	n=m;						// ターゲットナンバー
	switch( unit[n].kind )
		{
		case BB1:	j=16;j2=j/2;	break;
		case CA1:	j=12;j2=j/2;	break;
		case DD1:	j=6;j2=j/2;	break;
		case SS1:	
			if( unit[n].info[6] )// 潜航中、あたりがでかくなる
				{
				wrk_rect.top=(int)unit[n].y-50;
				wrk_rect.right=(int)unit[n].x+50;
				wrk_rect.bottom=(int)unit[n].y+50;
				wrk_rect.left=(int)unit[n].x-50;

				draw_line4((int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.top),(int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4((int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.top),(int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4((int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),(int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4((int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),(int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));

				return;
				}	
			else
				{	j=4;j2=j/2;		}


				break;
		case CV1:	j=14;j2=j/2;	break;
		case CVL1:	j=12;j2=j/2;	break;
		}
//	h=0;
	f=drctn_for_8((int)(unit[n].drctn));
	switch( f )
		{
		case 3: case 7:
			for( i=0; i<=4 /*&& !h*/ ; i++)
				{
				wrk_rect.top=(int)unit[n].y+(-j+(i*j2))-j2;
				wrk_rect.right=(int)unit[n].x+(+j-(i*j2))+j2;
				wrk_rect.bottom=(int)unit[n].y+(-j+(i*j2))+j2;
				wrk_rect.left=(int)unit[n].x+(+j-(i*j2))-j2;

				draw_line4((int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.top),(int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4((int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.top),(int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4((int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),(int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4((int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),(int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				}
			break;
		case 1: case 5:
			for( i=0; i<=4 /*&& !h*/ ; i++)
				{
				wrk_rect.top=(int)unit[n].y+(-j+(i*j2))-j2;
				wrk_rect.right=(int)unit[n].x+(-j+(i*j2))+j2;
				wrk_rect.bottom=(int)unit[n].y+(-j+(i*j2))+j2;
				wrk_rect.left=(int)unit[n].x+(-j+(i*j2))-j2;

				draw_line4((int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.top),(int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4((int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.top),(int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4((int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),(int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4((int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),(int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				}

			break;
		case 0: case 4:
			for( i=0; i<=2  /*&& !h*/ ; i++)
				{
				wrk_rect.top=(int)unit[n].y+(-j+(i*j))-j2;
				wrk_rect.right=(int)unit[n].x+j2;
				wrk_rect.bottom=(int)unit[n].y+(-j+(i*j))+j2;
				wrk_rect.left=(int)unit[n].x-j2;

				draw_line4((int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.top),(int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4((int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.top),(int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4((int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),(int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4((int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),(int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));


				}
			break;
		case 2: case 6:
			for( i=0; i<=2  /*&& !h*/ ; i++)
				{
				wrk_rect.top=(int)unit[n].y-j2;
				wrk_rect.right=(int)unit[n].x+(-j+(i*j))+j2;
				wrk_rect.bottom=(int)unit[n].y+j2;
				wrk_rect.left=(int)unit[n].x+(-j+(i*j))-j2;

				draw_line4((int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.top),(int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4((int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.top),(int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4((int)(wrk_rect.right-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),(int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));
				draw_line4((int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.bottom),(int)(wrk_rect.left-cmbt_x),(int)(cmbt_y-wrk_rect.top),CMBT_WIDTH-1,CMBT_HEIGHT-1,RGB(255,255,255));

				}
			break;
		}


	return	/*(h)*/;
	}









//============================================================================
// 射撃します。
//----------------------------------------------------------------------------
void	fire_now( int m, int trgt, int kind )
	{
	int			n,f,fc[3],i,trgt2,rng,s;
	double		turn,wrk_x,wrk_y,drctn,drctn2,drctn3,drctn4,drctn5,dstc,dstc2,dstc3,wrk_x2,wrk_y2,trgt_x,trgt_y;
	RECT		wrk_r;
	int			cm_scrn_x,cm_scrn_y;




	if( (unit[m].kind==SP || unit[m].kind==AP || unit[m].kind==GF1 || unit[m].kind==GF2 || unit[m].kind==GF3) && 
		unit[m].info[0]	)
		{
		//工事中
		return;
		}


	if( unit[m].ctgry==SHIP )
		{
		//=========		 艦船の射撃制御		=========//
		if( kind==TR_SP || kind==TR_AP || kind==TR_GF1 || kind==TR_GF2 || kind==TR_GF3 )
			{
			// トランスポート
			// 攻撃地点から攻撃目標地点への方位角
			wrk_x=(double)unit[m].info[6]-unit[m].x;
			wrk_y=(double)unit[m].info[7]-unit[m].y;
			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;	
			drctn2=drctn;
			drctn=drctn-unit[m].drctn;
			if(drctn<0)
				drctn=360+drctn;	

			if( (int)drctn<=45||(int)drctn>=315)
				{
				// 距離を求めます
				wrk_x=unit[m].x-(double)unit[m].info[6];
				wrk_y=unit[m].y-(double)unit[m].info[7];
				if(wrk_x==0)	wrk_x=1;
				if(wrk_y==0)	wrk_y=1;

				drctn=atan2(wrk_y,wrk_x)*RAD_to;
				if(drctn<0)
					drctn=360+drctn;
				if(wrk_x<0)
					wrk_x=0-wrk_x;
				if(wrk_y<0)
					wrk_y=0-wrk_y;
				if(drctn>=180)
					drctn=drctn-180;
				if(drctn>=90)
					drctn=90-(drctn-90);
				dstc=(wrk_x)/(cos(drctn*a_PI));

#if NSPW_THE_NET
				if( dstc>=0 && dstc<=160 )
#else
				if( dstc>=0 && dstc<=360 )
#endif
					{
					// 発射！
					unit[m].arm[1]=0;		// 残弾が０
					unit[m].arm[2]=0;		// ターゲットをクリア


					//unit[m].arm[0]=NTG;		//  輸送船はこれやっと来ます、武装品種
					unit[m].arm[4]=0;		//  輸送船はこれやっと来ます、武装品種



					//unit[m].used=0;


					//unit[m].info[5]=RETURN;		// 航空機はメイン兵器ゼロで帰投

					// 部下、多分戦闘機に帰投命令					
/*
					if(unit[m].is_ltl_ldr)
						{
						for(f=1;f<=max_unit;f++)
							{
							if( unit[f].used && unit[f].ltl_ldr==m )
								{
								unit[f].ltl_ldr=0;
								unit[f].info[5]=RETURN;
								}
							}

						unit[m].is_ltl_ldr=0;
						}
*/
/*
					if( !unit[m].is_ltl_ldr )
						{
						unit[m].pp_x[0]=unit[m].x;
						unit[m].pp_y[0]=unit[m].y;
						unit[m].pp_x[1]=MAP_RIGHT+1;
						}
*/

					n=seek_fire_no();
					if( n )
						{
						SoundPlayEffect( NULL, SPL1 ,unit[m].x, unit[m].y);
						fire[n].used=max_unit+1;
						fire[n].kind=kind;
						fire[n].x=unit[m].x;
						fire[n].y=unit[m].y;
						fire[n].drctn=drctn2;
						fire[n].spd=1.0;
						fire[n].spd_add=+0.0;
						fire[n].last_spd=0.0;
						fire[n].info[0]=0;
						fire[n].info[1]=360;
						fire[n].info[6]=unit[m].info[6];
						fire[n].info[7]=unit[m].info[7];
						fire[n].info[8]=unit[m].used;

//if( cnct_game )
//{
						unit[m].used=0;
						unit[m].hp[0]=0;

						if( unit_info[0] && unit_info[3]==m )
							unit_info[0]=0;


						if( the_slct_unit==m )
							{
							the_slct_unit=0; slct_unit[1][m]=0;	cmbt_menu_kind=0; cmbt_menu_slctd=0; 
							cls_all_slct_unit_p2(1);
							bf_new_pp[1].cls=0;

							if( unit_info[3]==m )
								unit_info[0]=0;

//							unit_info[0]=0;//unit[m].kind;
//							unit_info[1]=0;				// 空母なら１で格納庫 ０ で飛行甲板
//							unit_info[3]=0;//m;				// そのユニットの番号
//							unit_info[4]=0;//unit[m].used;	// そのユニットの国籍

							}
//}
						}
					}
				}
			return;
			}






		if( kind==RAS )
			{
			// 自動の対空機関砲 Rapid Anti Air Shell
			trgt2=0;
			for(n=1;n<=max_unit;n++)
				{	//敵を探す。
				if( unit[n].used && unit[n].ctgry==PLANE && (((unit[n].kind==AT1||unit[n].kind==FT1) && unit[n].arm[1] )|| rnd(10)==0 )  && unit[n].info[0]==FLYING && unit[n].used!=unit[m].used /*&& unit[n].hp[0]>=unit[n].hp[2]+1*/ && unit[n].found )
				//if( unit[n].used && unit[n].ctgry==PLANE && unit[n].info[0]==FLYING && unit[n].used!=unit[m].used /*&& unit[n].hp[0]>=unit[n].hp[2]+1*/ && unit[n].found )
					{
					// 攻撃地点から攻撃目標地点への距離
					wrk_x=unit[n].x-unit[m].x;
					wrk_y=unit[n].y-unit[m].y;
					if(wrk_x==0)	wrk_x=1;
					if(wrk_y==0)	wrk_y=1;
					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					if(wrk_x<0)
						wrk_x=0-wrk_x;
					if(wrk_y<0)
						wrk_y=0-wrk_y;
					if(drctn>=180)
						drctn=drctn-180;
					if(drctn>=90)
						drctn=90-(drctn-90);
					turn=(wrk_x)/(cos(drctn*a_PI));
					turn=turn/10.0;


					// 攻撃地点から攻撃目標地点への絶対方位、方位角
					wrk_x=unit[n].x;
					wrk_y=unit[n].y;
					drctn=unit[n].drctn_add;
					drctn2=unit[n].drctn;
					for(f=0;f<=(int)turn;f++)
						{
						drctn2+=drctn;
						if(drctn2<0)		drctn2=360+drctn2;
						if(drctn2>=360)		drctn2=drctn2-360;
						wrk_x+=cos(drctn2*a_PI)*(unit[n].spd); // とりあえずターン後
						wrk_y+=sin(drctn2*a_PI)*(unit[n].spd);
						}


					wrk_x2=wrk_x;
					wrk_y2=wrk_y;
					wrk_x=wrk_x2-unit[m].x;
					wrk_y=wrk_y2-unit[m].y;





					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					drctn2=drctn;							


					drctn=drctn-unit[m].drctn;
					if(drctn<0)
						drctn=360+drctn;				
					drctn3=drctn;							// 方位角


					
					// 攻撃地点から攻撃目標地点への距離
					wrk_x=wrk_x2-unit[m].x;
					wrk_y=wrk_y2-unit[m].y;
					if(wrk_x==0)	wrk_x=1;
					if(wrk_y==0)	wrk_y=1;
					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					if(wrk_x<0)
						wrk_x=0-wrk_x;
					if(wrk_y<0)
						wrk_y=0-wrk_y;
					if(drctn>=180)
						drctn=drctn-180;
					if(drctn>=90)
						drctn=90-(drctn-90);
					dstc=(wrk_x)/(cos(drctn*a_PI));
	
					/****				
					switch( unit[m].kind )
						{
						case BB1:	rng=900; fc[0]=1;fc[1]=2;fc[2]=1;	break;
						case CA1:	rng=600; fc[0]=1;fc[1]=1;fc[2]=1;	break;
						case DD1:	rng=300; fc[0]=1;fc[1]=1;fc[2]=0;	break;
						}
					****/
					if( dstc>=40 && dstc<=300/*480*/ )
						{
						trgt2=n;	
						if( 1 /*|| rnd(2)==0*/ )
							break;
						else
							trgt2=0;
						}
					}
				}
			if( trgt2 )
				{
			
				/*
				f=fc[2];	// 後面
				if( drctn3>=315.0 || drctn3<=45.0 )
					f=fc[0];	// 正面
				if( (drctn3>=45.0 && drctn3<=135.0) || (drctn3>=225.0 && drctn3<=315.0) )
					f=fc[1];	// 側面
				*/


				//for( i=1; i<=f ;i++ )
					//{

				n=seek_fire_no();
				if( n )
					{
					if(unit[m].arm[1])
						unit[m].arm[1]-=RAS_SZ;			// 弾薬消費

					if(my_rnd(2) )
						SoundPlayEffect( NULL, AA_SHL3 ,unit[m].x, unit[m].y);
					else
						SoundPlayEffect( NULL, AA_SHL5 ,unit[m].x, unit[m].y);

					fire[n].used=trgt2;
					fire[n].kind=kind;
					fire[n].x=unit[m].x;
					fire[n].y=unit[m].y;

					drctn3=drctn2+(rnd(18)-9);			// 絶対方位
					if(drctn3>=360)	drctn3=drctn3-360;				
					if(drctn3<0)	drctn3=360+drctn3;				
					fire[n].drctn=drctn3;

					dstc2=dstc+(rnd( ((int)(dstc/5)) )-((int)(dstc/10))   );

					fire[n].spd=10.0;
					fire[n].spd_add=-0.00;
					fire[n].last_spd=0;
					fire[n].info[0]=(int)(dstc2/fire[n].spd);
					fire[n].info[1]=0;
					}
					//}
				}
			return;
			}




		if( kind==ASB && unit[m].spd>=unit[m].max_spd )
			{
			for( n=1; n<=max_unit; n++)
				{
				trgt=n;
				if( unit[trgt].used!=unit[m].used && unit[trgt].kind==SS1 && unit[trgt].info[6] && unit[trgt].found )
					{	// 爆雷

					// ptin dbg
					wrk_r.top=(int)unit[trgt].info[8]+unit[trgt].info[9];//(int)unit[trgt].info[8]-unit[trgt].info[9];
					wrk_r.right=(int)unit[trgt].info[7]+unit[trgt].info[9];
					wrk_r.bottom=(int)unit[trgt].info[8]-unit[trgt].info[9];//(int)unit[trgt].info[8]+unit[trgt].info[9];
					wrk_r.left=(int)unit[trgt].info[7]-unit[trgt].info[9];
					if( pt_in_rect3(&wrk_r,(int)unit[m].x,(int)unit[m].y) )
						{
						// 投雷
						n=seek_fire_no();
							if(n)
							{
							if(unit[m].arm[1])
								unit[m].arm[1]-=ASB_SZ;			// 弾薬消費
							if(unit[m].arm[1]<0)
								unit[m].arm[1]=0;

							SoundPlayEffect( NULL, SPL1 ,unit[m].x, unit[m].y);
							fire[n].used=trgt;
							fire[n].kind=kind;
							fire[n].x=unit[m].x;
							fire[n].y=unit[m].y;

							drctn=unit[m].drctn;
							if( unit[m].type==0 )
								{
								// ただの駆逐艦
								drctn+=180;
								drctn=(int)drctn%360;
								fire[n].x+=cos(drctn*a_PI)*20;
								fire[n].y+=sin(drctn*a_PI)*20;
								}
							else
								{
								// 対潜駆逐艦
								drctn+=120+rnd(3)*60;
								drctn=(int)drctn%360;
								fire[n].x+=cos(drctn*a_PI)*35;
								fire[n].y+=sin(drctn*a_PI)*35;
								}

							fire[n].drctn=0;
							fire[n].spd=0;
							fire[n].spd_add=0;
							fire[n].last_spd=0;
							fire[n].info[0]=0;
							fire[n].info[1]=100;
							}
						return;
						}
					}
				}
			return;
			}




		if( ( kind==GUN || kind==SP_GUN ) && trgt==0)	// ターゲットが選択されていない砲撃、
			{
			// 艦砲、自動射撃
			trgt2=0;
			dstc2=2000;
			for(n=1;n<=max_unit;n++)
				{	//敵を探す。
				if( unit[n].used && unit[n].ctgry==SHIP && !(unit[n].kind==SS1 && unit[n].info[6] ) && unit[n].used!=unit[m].used && unit[n].found  && unit[n].kind!=CT1 )
					{
					// 攻撃地点から攻撃目標地点への距離
					wrk_x=unit[n].x-unit[m].x;
					wrk_y=unit[n].y-unit[m].y;
					if(wrk_x==0)	wrk_x=1;
					if(wrk_y==0)	wrk_y=1;
					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					if(wrk_x<0)
						wrk_x=0-wrk_x;
					if(wrk_y<0)
						wrk_y=0-wrk_y;
					if(drctn>=180)
						drctn=drctn-180;
					if(drctn>=90)
						drctn=90-(drctn-90);
					turn=(wrk_x)/(cos(drctn*a_PI));
					turn=turn/10.0;



					// 敵の未来位置を求めます。
					wrk_x=unit[n].x;
					wrk_y=unit[n].y;
					wrk_x+=cos(unit[n].drctn*a_PI)*(unit[n].spd*turn); // とりあえずターン後
					wrk_y+=sin(unit[n].drctn*a_PI)*(unit[n].spd*turn);
					wrk_x2=wrk_x;										// ターゲットの未来位置
					wrk_y2=wrk_y;


					// 攻撃地点から攻撃目標地点への絶対方位、方位角
					wrk_x=wrk_x-unit[m].x;
					wrk_y=wrk_y-unit[m].y;
					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					drctn4=drctn;							// 絶対方位

					drctn=drctn-unit[m].drctn;
					if(drctn<0)
						drctn=360+drctn;				
					drctn5=drctn;							// 方位角


					
					// 攻撃地点から攻撃目標地点への距離
					wrk_x=wrk_x2-unit[m].x;
					wrk_y=wrk_y2-unit[m].y;
					if(wrk_x==0)	wrk_x=1;
					if(wrk_y==0)	wrk_y=1;


					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					if(wrk_x<0)
						wrk_x=0-wrk_x;
					if(wrk_y<0)
						wrk_y=0-wrk_y;
					if(drctn>=180)
						drctn=drctn-180;
					if(drctn>=90)
						drctn=90-(drctn-90);
					dstc=(wrk_x)/(cos(drctn*a_PI));
					
					switch( unit[m].kind )
						{
						case BB1:	
							if( kind==SP_GUN )
								{
								rng=1120; fc[0]=3;fc[1]=4;fc[2]=2;	
								}
							else
								{
								rng=600; fc[0]=3;fc[1]=4;fc[2]=2;	
								}
							break;
						case CA1:	rng=500; fc[0]=1;fc[1]=2;fc[2]=1;	break;
						case DD1:	rng=400; fc[0]=1;fc[1]=1;fc[2]=0;	break;
						case SS1:	rng=200; fc[0]=1;fc[1]=1;fc[2]=0;	break;

						case GF1:	rng=600; fc[0]=1;fc[1]=1;fc[2]=1;	break;
						case GF2:	rng=800; fc[0]=2;fc[1]=2;fc[2]=2;	break;
						case GF3:	rng=1000; fc[0]=3;fc[1]=3;fc[2]=3;	break;
						}

					if( (dstc>=(rng*0.3) || (unit[m].kind>=GF1&&unit[m].kind<=GF3) ) && dstc<=rng && dstc2>=dstc )
						{	
						trgt2=n;	
						dstc2=dstc;
						drctn2=drctn4;
						drctn3=drctn5;							
						//break;	
						}
					}
				}



			if( trgt2 )
				{
				f=fc[2];	// 後面
				if( drctn3>=315.0 || drctn3<=45.0 )
					f=fc[0];	// 正面
				if( (drctn3>=45.0 && drctn3<=135.0) || (drctn3>=225.0 && drctn3<=315.0) )
					f=fc[1];	// 側面
			

				switch( unit[m].kind )
					{
					case BB1:
					case GF3:
						if( kind==SP_GUN )
							{
							SoundPlayEffect( NULL, GUN3 ,unit[m].x, unit[m].y);	
							break;
							}
						SoundPlayEffect( NULL, GUN2+rnd(2) ,unit[m].x, unit[m].y);	
						break;
					case CA1:	
					case GF2:	
						SoundPlayEffect( NULL, GUN1+rnd(2) ,unit[m].x, unit[m].y);
						break;

					case DD1:	
					case SS1:	
					case GF1:	
						SoundPlayEffect( NULL, GUN1 ,unit[m].x, unit[m].y);
						break;
					}


				for( i=1; i<=f ;i++ )
					{
					n=seek_fire_no();
					if( n )
						{
						if(unit[m].arm[1])
							unit[m].arm[1]-=GUN_SZ;			// 弾薬消費

						fire[n].used=trgt2;
						fire[n].kind=GUN/*kind*/;
						fire[n].x=unit[m].x;
						fire[n].y=unit[m].y;

						if( kind==SP_GUN )
							{
							if( rnd(2)==0 )
								drctn3=drctn2+(double)((double)(rnd(50)-25)/10)    /*+(rnd(100)/100)*/;			// 絶対方位
							else
								drctn3=drctn2+(double)((double)(rnd(80)-40)/10)    /*+(rnd(100)/100)*/;			// 絶対方位
							}
						else
							{
							if( rnd(5+(unit[m].kind>=GF1&&unit[m].kind<=GF3)*4  )==0 || ( unit[m].kind==BB1 && rnd( 4 )==0 ) )
								drctn3=drctn2+(rnd(7)-3)+(rnd(100)/100);			// 絶対方位
							else
								drctn3=drctn2+(rnd(11)-5)+(rnd(100)/100);				// 絶対方位
							}

						if(drctn3>=360)	drctn3=drctn3-360;				
						if(drctn3<0)	drctn3=360+drctn3;				
						fire[n].drctn=drctn3;

						if( kind==SP_GUN )
							dstc2=dstc2+(rnd( ((int)(dstc2/12)) )-((int)(dstc2/24)));
						else
							dstc2=dstc2+(rnd( ((int)(dstc2/8)) )-((int)(dstc2/16)));


						fire[n].spd=10.0;
						fire[n].spd_add=((fire[n].spd)/(dstc2/fire[n].spd));
						fire[n].last_spd=0;
						fire[n].info[0]=(int)(dstc2/fire[n].spd)+1;
						fire[n].info[1]=fire[n].info[0]/2;



						}
					}
				}
			return;
			}





		// 選択でない自動の魚雷、主に駆逐艦
		if( kind==TPD && trgt==0)
			{
			// 
			trgt2=0;
			for(n=1;n<=max_unit;n++)
				{	//敵を探す。
				if( unit[n].used && unit[n].ctgry==SHIP && !(unit[n].kind==AP||unit[n].kind==SP||unit[n].kind==GF1||unit[n].kind==GF2||unit[n].kind==GF3) && unit[n].used!=unit[m].used  && unit[n].found && !(unit[n].kind==SS1||unit[n].kind==DD1) )
					{
					// 攻撃地点から攻撃目標地点への距離
					wrk_x=unit[n].x-unit[m].x;
					wrk_y=unit[n].y-unit[m].y;
					if(wrk_x==0)	wrk_x=1;
					if(wrk_y==0)	wrk_y=1;
					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					if(wrk_x<0)
						wrk_x=0-wrk_x;
					if(wrk_y<0)
						wrk_y=0-wrk_y;
					if(drctn>=180)
						drctn=drctn-180;
					if(drctn>=90)
						drctn=90-(drctn-90);
					turn=(wrk_x)/(cos(drctn*a_PI));
					turn=turn/TPD_SPD;							// 撃つ弾の速度で割る


					// 攻撃地点から攻撃目標地点への絶対方位、方位角
					wrk_x=unit[n].x;
					wrk_y=unit[n].y;
					wrk_x+=cos(unit[n].drctn*a_PI)*(unit[n].spd*turn); 
					wrk_y+=sin(unit[n].drctn*a_PI)*(unit[n].spd*turn);
/****
					drctn=unit[n].drctn_add;
					drctn2=unit[n].drctn;
					for(f=0;f<=(int)turn;f++)
						{
						drctn2+=drctn;
						if(drctn2<0)		drctn2=360+drctn2;
						if(drctn2>=360)		drctn2=drctn2-360;
						wrk_x+=cos(drctn2*a_PI)*(unit[n].spd); // とりあえずターン後
						wrk_y+=sin(drctn2*a_PI)*(unit[n].spd);
						}
***/
					wrk_x2=wrk_x;					// 標的の未来位置
					wrk_y2=wrk_y;					// 
					wrk_x=wrk_x2-unit[m].x;
					wrk_y=wrk_y2-unit[m].y;

					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					drctn2=drctn;					// 未来位置への絶対角


					drctn=drctn-unit[m].drctn;
					if(drctn<0)
						drctn=360+drctn;				
					drctn3=drctn;							// 方位角


					
					// 攻撃地点から攻撃目標地点への距離
					wrk_x=wrk_x2-unit[m].x;
					wrk_y=wrk_y2-unit[m].y;
					if(wrk_x==0)	wrk_x=1;
					if(wrk_y==0)	wrk_y=1;
					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					if(wrk_x<0)
						wrk_x=0-wrk_x;
					if(wrk_y<0)
						wrk_y=0-wrk_y;
					if(drctn>=180)
						drctn=drctn-180;
					if(drctn>=90)
						drctn=90-(drctn-90);
					dstc=(wrk_x)/(cos(drctn*a_PI));

					

					if( (dstc>=100 && dstc<=(500+(unit[m].used==JPN)*100)) && ((drctn3>=45&&drctn3<=135)||(drctn3>=225&&drctn3<=315)) )
						{
						// ばってん陸地があるけんしらべる
						trgt2=n;

						i=dstc/TPD_SPD;
						for(f=1;f<=i;f++)
							{
							wrk_x=unit[m].x;
							wrk_y=unit[m].y;
							wrk_x+=cos(drctn2*a_PI)*(TPD_SPD*f); // とりあえずターン後
							wrk_y+=sin(drctn2*a_PI)*(TPD_SPD*f);

							if(!( wrk_y>MAP_TOP || wrk_y<MAP_BOTTOM || wrk_x<MAP_LEFT || wrk_x>MAP_RIGHT ))
								{
								cm_scrn_x=(int)((wrk_x+(sprt[UNIT_JPN].wd/2)-MAP_LEFT)/sprt[MAP_TIP_NRML].wd);
								cm_scrn_y=(int)((MAP_TOP-wrk_y+(sprt[UNIT_JPN].ht/2))/sprt[MAP_TIP_NRML].ht);
								if( cmbt_map[cm_scrn_y][cm_scrn_x]>=1 )
									{
									trgt2=0;
									break;
									}
								}
							}


						if(trgt2)
							{
							if( rnd(3)==0 )
								break;
							else
								trgt2=0;
							}
						}
					}
				}
			if( trgt2 )
				{
			
				// 発射！
				if(unit[m].arm[1])
					unit[m].arm[1]-=TPD_SZ;
				switch( unit[m].kind )
					{
					case CA1:
					case DD1:		unit[m].arm[3]=RELOAD_TPD_DD;		break;	// 再装填時間
					case SS1:		unit[m].arm[3]=RELOAD_TPD_SS;		
									//撃った瞬間に発見される。
									unit[m].info[7]=unit[m].x+rnd((50)*2)-50;
									unit[m].info[8]=unit[m].y+rnd((50)*2)-50;

									unit[m].info[9]=100;
									unit[m].info[10]=300;

									unit[m].found=1;





									break;	// 再装填時間
					}


				if( unit[m].used==JPN  )
					{
					if(unit[m].kind==DD1)
						i=2;						// 日本海軍駆逐艦魚雷３発
					else
						i=1;						// 日本海軍巡洋艦魚雷２はつ
					}
				else
					{
					i=0;						// 合衆国海軍魚雷１発
					}

				if( unit[m].kind!=SS1 )
					SoundPlayEffect( NULL, TPD_LOS ,unit[m].x, unit[m].y);

				for( f=0; f<=i; f++)
					{
					n=seek_fire_no();
					if( n )
						{
//						if( unit[m].kind!=SS1 )
//							SoundPlayEffect( NULL, SPL1 ,unit[m].x, unit[m].y);
						fire[n].used=trgt2;
						fire[n].kind=kind;
						fire[n].x=unit[m].x;
						fire[n].y=unit[m].y;


						switch( f )
							{
							case 0:	fire[n].drctn=drctn2;	break;
							case 1:	fire[n].drctn=drctn2+5;	break;
							case 2:	fire[n].drctn=drctn2-5;	break;
							}
						fire[n].drctn=(int)(fire[n].drctn)%360;


/*****

						if( unit[m].kind==SS1 )
							{
							fire[n].drctn=unit[m].drctn+355.0+(f*5);
							while(fire[n].drctn>=360)
								{fire[n].drctn=fire[n].drctn-360;}
							}
						else
							{
							if( ((int)drctn3>=45&&(int)drctn3<=135) )
								fire[n].drctn=unit[m].drctn+85.0+(f*5);
							else
								fire[n].drctn=unit[m].drctn+265.0+(f*5);
							if(fire[n].drctn>=360)
								fire[n].drctn=fire[n].drctn-360;
							}
***/
						fire[n].spd=TPD_SPD;
						fire[n].spd_add=+0.0;
						fire[n].last_spd=0.0;
						if( unit[m].kind==SS1 )
							fire[n].info[0]=1;
						else
							fire[n].info[0]=0;
						fire[n].info[1]=275+(unit[m].used==JPN)*110;
						fire[n].info[2]=30;
						}
					}
				}
			return;
			}





		// 選択された敵への魚雷、主に、潜水艦
		if( kind==TPD && trgt)
			{
			// 

			trgt2=0;
			n=trgt;
			if( unit[n].used && unit[n].ctgry==SHIP && !(unit[n].kind==AP||unit[n].kind==SP||unit[n].kind==GF1||unit[n].kind==GF2||unit[n].kind==GF3) && unit[n].used!=unit[m].used  && unit[n].found )
				{

	
				// 攻撃地点から攻撃目標地点への距離
				wrk_x=unit[n].x-unit[m].x;
				wrk_y=unit[n].y-unit[m].y;
				if(wrk_x==0)	wrk_x=1;
				if(wrk_y==0)	wrk_y=1;
				drctn=atan2(wrk_y,wrk_x)*RAD_to;
				if(drctn<0)
					drctn=360+drctn;
				if(wrk_x<0)
					wrk_x=0-wrk_x;
				if(wrk_y<0)
					wrk_y=0-wrk_y;
				if(drctn>=180)
					drctn=drctn-180;
				if(drctn>=90)
					drctn=90-(drctn-90);
				turn=(wrk_x)/(cos(drctn*a_PI));
				turn=turn/TPD_SPD;							// 撃つ弾の速度で割る


				// 攻撃地点から攻撃目標地点への絶対方位、方位角
				wrk_x=unit[n].x;
				wrk_y=unit[n].y;
				wrk_x+=cos(unit[n].drctn*a_PI)*(unit[n].spd*turn); 
				wrk_y+=sin(unit[n].drctn*a_PI)*(unit[n].spd*turn);
				wrk_x2=wrk_x;					// 標的の未来位置
				wrk_y2=wrk_y;					// 
				wrk_x=wrk_x2-unit[m].x;
				wrk_y=wrk_y2-unit[m].y;

				drctn=atan2(wrk_y,wrk_x)*RAD_to;
				if(drctn<0)
					drctn=360+drctn;
				drctn2=drctn;					// 未来位置への絶対角


				drctn=drctn-unit[m].drctn;
				if(drctn<0)
					drctn=360+drctn;				
				drctn3=drctn;							// 方位角


				
				// 攻撃地点から攻撃目標地点への距離
				wrk_x=wrk_x2-unit[m].x;
				wrk_y=wrk_y2-unit[m].y;
				if(wrk_x==0)	wrk_x=1;
				if(wrk_y==0)	wrk_y=1;
				drctn=atan2(wrk_y,wrk_x)*RAD_to;
				if(drctn<0)
					drctn=360+drctn;
				if(wrk_x<0)
					wrk_x=0-wrk_x;
				if(wrk_y<0)
					wrk_y=0-wrk_y;
				if(drctn>=180)
					drctn=drctn-180;
				if(drctn>=90)
					drctn=90-(drctn-90);
				dstc=(wrk_x)/(cos(drctn*a_PI));

				
				if( (dstc>=100 && dstc<=(550+(unit[m].used==JPN)*100)) && (drctn3<=5 || drctn3>=355) )
					{
					trgt2=n;	
					i=dstc/TPD_SPD;
					for(f=1;f<=i;f++)
						{
						wrk_x=unit[m].x;
						wrk_y=unit[m].y;
						wrk_x+=cos(drctn2*a_PI)*(TPD_SPD*f); // とりあえずターン後
						wrk_y+=sin(drctn2*a_PI)*(TPD_SPD*f);

						if(!( wrk_y>MAP_TOP || wrk_y<MAP_BOTTOM || wrk_x<MAP_LEFT || wrk_x>MAP_RIGHT ))
							{
							cm_scrn_x=(int)((wrk_x+(sprt[UNIT_JPN].wd/2)-MAP_LEFT)/sprt[MAP_TIP_NRML].wd);
							cm_scrn_y=(int)((MAP_TOP-wrk_y+(sprt[UNIT_JPN].ht/2))/sprt[MAP_TIP_NRML].ht);
							if( cmbt_map[cm_scrn_y][cm_scrn_x]>=1 )
								{
								trgt2=0;
								break;
								}
							}
						}
					}
				}



			if( trgt2 )
				{
				// 発射！
				if(unit[m].arm[1])
					unit[m].arm[1]-=TPD_SZ;
				switch( unit[m].kind )
					{
					case DD1:		unit[m].arm[3]=RELOAD_TPD_DD;		break;	// 再装填時間
					case SS1:		unit[m].arm[3]=RELOAD_TPD_SS;		
									//撃った瞬間に発見される。
									unit[m].info[7]=unit[m].x+rnd((50)*2)-50;
									unit[m].info[8]=unit[m].y+rnd((50)*2)-50;

									unit[m].info[9]=100;
									unit[m].info[10]=300;

									unit[m].found=1;
									break;	// 再装填時間
					}


				if( unit[m].kind!=SS1 )
					{
					SoundPlayEffect( NULL, TPD_LOS ,unit[m].x, unit[m].y);
					}
				else
					{
					unit[m].arm[2]=0;
					}
					
/*					SoundPlayEffect( NULL, SPL1 ,unit[m].x, unit[m].y);
*/
				for( f=0; f<=2; f++)
					{
					n=seek_fire_no();
					if( n )
						{
//						if( unit[m].kind!=SS1 )
//							SoundPlayEffect( NULL, SPL1 ,unit[m].x, unit[m].y);
						fire[n].used=trgt2;
						fire[n].kind=kind;
						fire[n].x=unit[m].x;
						fire[n].y=unit[m].y;

						switch( f )
							{
							case 0:	fire[n].drctn=drctn2+5;	break;
							case 1:	fire[n].drctn=drctn2;	break;
							case 2:	fire[n].drctn=drctn2-5;	break;
							}
						fire[n].drctn=(int)(fire[n].drctn)%360;

						fire[n].spd=TPD_SPD;
						fire[n].spd_add=+0.0;
						fire[n].last_spd=0.0;
						if( unit[m].kind==SS1 )
							fire[n].info[0]=1;
						else
							fire[n].info[0]=0;
						fire[n].info[1]=290+(unit[m].used==JPN)*110;
						fire[n].info[2]=30;
						}
					}



				}
			return;
			}







		if( kind==SHL && trgt && unit[trgt].ctgry==PLANE )	// ターゲットが選択された対空砲
			{
			//	指定射撃
			if( !unit[trgt].found )
				return;

			trgt2=0;
			n=trgt;
			// 攻撃地点から攻撃目標地点への絶対方位、方位角

			wrk_x=unit[n].x-unit[m].x;
			wrk_y=unit[n].y-unit[m].y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;
			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			turn=(wrk_x)/(cos(drctn*a_PI));
			turn=turn/10.0;

			// ターゲットの未来位置を求めます。
			wrk_x=unit[n].x;
			wrk_y=unit[n].y;
			wrk_x+=cos(unit[n].drctn*a_PI)*(unit[n].spd*turn); // ターン後
			wrk_y+=sin(unit[n].drctn*a_PI)*(unit[n].spd*turn);
			wrk_x2=wrk_x;										// 未来位置
			wrk_y2=wrk_y;

			
			// ターゲットの方位関係を
			wrk_x=wrk_x-unit[m].x;
			wrk_y=wrk_y-unit[m].y;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			drctn2=drctn;							


			drctn=drctn-unit[m].drctn;
			if(drctn<0)
				drctn=360+drctn;				
			drctn3=drctn;							// 方位角


					
			// 攻撃地点から攻撃目標地点への距離
			wrk_x=wrk_x2-unit[m].x;
			wrk_y=wrk_y2-unit[m].y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			dstc=(wrk_x)/(cos(drctn*a_PI));




					
			switch( unit[m].kind )
				{
				case BB1:	rng=700; fc[0]=2;fc[1]=3;fc[2]=1;	break;
				case CA1:	rng=600; fc[0]=1;fc[1]=2;fc[2]=1;	break;
				case DD1:	rng=300; fc[0]=1;fc[1]=1;fc[2]=0;	break;

				case GF1:	rng=500; fc[0]=1;fc[1]=1;fc[2]=1;	break;
				case GF2:	rng=600; fc[0]=fc[1]=fc[2]=rnd(2)+1;	break;
				case GF3:	rng=700; fc[0]=fc[1]=fc[2]=rnd(2)+2;	break;
				}





			if( dstc>=(rng*0.25) && dstc<=rng )
				{	
				trgt2=n;	
				}

			if( trgt2 )
				{
				f=fc[2];	// 後面
				if( drctn3>=315.0 || drctn3<=45.0 )
					f=fc[0];	// 正面
				if( (drctn3>=45.0 && drctn3<=135.0) || (drctn3>=225.0 && drctn3<=315.0) )
					f=fc[1];	// 側面
			


				SoundPlayEffect( NULL, AA_SHL2 ,unit[m].x, unit[m].y);
				for( i=1; i<=f ;i++ )
					{

					n=seek_fire_no();
					if( n )
						{
						if(unit[m].arm[1])
							unit[m].arm[1]-=SHL_SZ;			// 弾薬消費
						//SoundPlayEffect( NULL, GUN1+rnd(3) ,unit[m].x, unit[m].y);
						fire[n].used=trgt2;


						fire[n].kind=SHL;

						fire[n].x=unit[m].x;
						fire[n].y=unit[m].y;


						drctn3=drctn2+(rnd(18)-9);			// 絶対方位
						if(drctn3>=360)	drctn3=drctn3-360;				
						if(drctn3<0)	drctn3=360+drctn3;				
						fire[n].drctn=drctn3;

						dstc2=dstc+(rnd( ((int)(dstc/10)) )-((int)(dstc/20))   );

						fire[n].spd=10.0;
						fire[n].spd_add=-0.00;
						fire[n].last_spd=0;
						fire[n].info[0]=(int)(dstc2/fire[n].spd);
						fire[n].info[1]=0;


						}
					}
				}
			return;
			}





		if( kind==SHL && trgt==0 )
			{
			// 自動の対空砲 Anti Air Shell
			trgt2=0;
			for(n=1;n<=max_unit;n++)
				{	//敵を探す。
				if( unit[n].used && unit[n].ctgry==PLANE && (((unit[n].kind==AT1||unit[n].kind==BM1) && unit[n].arm[1] )|| rnd(10)==0 )  && unit[n].info[0]==FLYING && unit[n].used!=unit[m].used && unit[n].found )
					{
					// 攻撃地点から攻撃目標地点への距離
					wrk_x=unit[n].x-unit[m].x;
					wrk_y=unit[n].y-unit[m].y;
					if(wrk_x==0)	wrk_x=1;
					if(wrk_y==0)	wrk_y=1;
					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					if(wrk_x<0)
						wrk_x=0-wrk_x;
					if(wrk_y<0)
						wrk_y=0-wrk_y;
					if(drctn>=180)
						drctn=drctn-180;
					if(drctn>=90)
						drctn=90-(drctn-90);
					turn=(wrk_x)/(cos(drctn*a_PI));
					turn=turn/10.0;


					// 攻撃地点から攻撃目標地点への絶対方位、方位角
					wrk_x=unit[n].x;
					wrk_y=unit[n].y;
					drctn=unit[n].drctn_add;
					drctn2=unit[n].drctn;
					for(f=0;f<=(int)turn;f++)
						{
						drctn2+=drctn;
						if(drctn2<0)		drctn2=360+drctn2;
						if(drctn2>=360)		drctn2=drctn2-360;
						wrk_x+=cos(drctn2*a_PI)*(unit[n].spd); // とりあえずターン後
						wrk_y+=sin(drctn2*a_PI)*(unit[n].spd);
						}


					wrk_x2=wrk_x;
					wrk_y2=wrk_y;
					wrk_x=wrk_x2-unit[m].x;
					wrk_y=wrk_y2-unit[m].y;





					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					drctn2=drctn;							


					drctn=drctn-unit[m].drctn;
					if(drctn<0)
						drctn=360+drctn;				
					drctn3=drctn;							// 方位角


					
					// 攻撃地点から攻撃目標地点への距離
					wrk_x=wrk_x2-unit[m].x;
					wrk_y=wrk_y2-unit[m].y;
					if(wrk_x==0)	wrk_x=1;
					if(wrk_y==0)	wrk_y=1;
					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					if(wrk_x<0)
						wrk_x=0-wrk_x;
					if(wrk_y<0)
						wrk_y=0-wrk_y;
					if(drctn>=180)
						drctn=drctn-180;
					if(drctn>=90)
						drctn=90-(drctn-90);
					dstc=(wrk_x)/(cos(drctn*a_PI));


					
					switch( unit[m].kind )
						{
						case BB1:	rng=700; fc[0]=2;fc[1]=3;fc[2]=1;	break;
						case CA1:	rng=600; fc[0]=1;fc[1]=2;fc[2]=1;	break;
						case DD1:	rng=300; fc[0]=1;fc[1]=1;fc[2]=0;	break;

						case GF1:	rng=500; fc[0]=1;fc[1]=1;fc[2]=1;	break;
						case GF2:	rng=600; fc[0]=fc[1]=fc[2]=rnd(2)+1;	break;
						case GF3:	rng=700; fc[0]=fc[1]=fc[2]=rnd(2)+2;	break;
						}

						if( dstc>=(rng*0.25) && dstc<=rng )
							{
							trgt2=n;
							break;
/**
							if( 1 || rnd(2)==0 )
								break;
							else
								trgt2=0;
**/
							}
					}
				}
			if( trgt2 )
				{
			

				f=fc[2];	// 後面
				if( drctn3>=315.0 || drctn3<=45.0 )
					f=fc[0];	// 正面
				if( (drctn3>=45.0 && drctn3<=135.0) || (drctn3>=225.0 && drctn3<=315.0) )
					f=fc[1];	// 側面
			


				//SoundPlayEffect( NULL, AA_SHL2 ,unit[m].x, unit[m].y);
				for( i=1; i<=f ;i++ )
					{
					n=seek_fire_no();
					if( n )
						{
						if(unit[m].arm[1])
							unit[m].arm[1]-=SHL_SZ;			// 弾薬消費
						SoundPlayEffect( NULL, AA_SHL2 ,unit[m].x, unit[m].y);
						fire[n].used=trgt2;
						fire[n].kind=kind;
						fire[n].x=unit[m].x;
						fire[n].y=unit[m].y;

						drctn3=drctn2+(rnd(18)-9);			// 絶対方位
						if(drctn3>=360)	drctn3=drctn3-360;				
						if(drctn3<0)	drctn3=360+drctn3;				
						fire[n].drctn=drctn3;

						dstc2=dstc+(rnd( ((int)(dstc/10)) )-((int)(dstc/20))   );

						fire[n].spd=10.0;
						fire[n].spd_add=-0.00;
						fire[n].last_spd=0;
						fire[n].info[0]=(int)(dstc2/fire[n].spd);
						fire[n].info[1]=0;
						}
					}
				}
			return;
			}






		if( ( kind==GUN || kind==SP_GUN ) && trgt && unit[trgt].ctgry==SHIP )	// ターゲットが選択された砲撃
			{
			// 艦砲		指定射撃
			if( !unit[trgt].found )
				return;

			trgt2=0;
			n=trgt;
			// 攻撃地点から攻撃目標地点への絶対方位、方位角

			// 攻撃地点から攻撃目標地点への距離
			wrk_x=unit[n].x-unit[m].x;
			wrk_y=unit[n].y-unit[m].y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;
			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			turn=(wrk_x)/(cos(drctn*a_PI));
			turn=turn/10.0;

			// ターゲットの未来位置を求めます。
			wrk_x=unit[n].x;
			wrk_y=unit[n].y;
			wrk_x+=cos(unit[n].drctn*a_PI)*(unit[n].spd*turn); // ターン後
			wrk_y+=sin(unit[n].drctn*a_PI)*(unit[n].spd*turn);
			wrk_x2=wrk_x;										// 未来位置
			wrk_y2=wrk_y;

	
			// ターゲットの方位関係を
			wrk_x=wrk_x-unit[m].x;
			wrk_y=wrk_y-unit[m].y;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			drctn2=drctn;							


			drctn=drctn-unit[m].drctn;
			if(drctn<0)
				drctn=360+drctn;				
			drctn3=drctn;							// 方位角


			
			// 攻撃地点から攻撃目標地点への距離
			wrk_x=wrk_x2-unit[m].x;
			wrk_y=wrk_y2-unit[m].y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			dstc=(wrk_x)/(cos(drctn*a_PI));



			// 水上艦への射程距離
			switch( unit[m].kind )
				{
				case BB1:	
					if( kind==SP_GUN )
						{
//						rng=750; fc[0]=3;fc[1]=4;fc[2]=2;	
						rng=1120; fc[0]=3;fc[1]=5;fc[2]=2;	
						}
					else
						{
						rng=800; fc[0]=3;fc[1]=5;fc[2]=2;	
						}
//					rng=800; fc[0]=3;fc[1]=5;fc[2]=2;	
					break;
				case CA1:	rng=700; fc[0]=2;fc[1]=3;fc[2]=1;	break;
				case DD1:	rng=400; fc[0]=1;fc[1]=1;fc[2]=0;	break;
				case SS1:	rng=300; fc[0]=1;fc[1]=1;fc[2]=0;	break;

				case GF1:	rng=600; fc[0]=1;fc[1]=1;fc[2]=1;	break;
				case GF2:	rng=800; fc[0]=2;fc[1]=2;fc[2]=2;	break;
				case GF3:	rng=1000; fc[0]=3;fc[1]=3;fc[2]=3;	break;
				}


			if( (dstc>=150  || (unit[m].kind>=GF1&&unit[m].kind<=GF3) ) && dstc<=rng )
				{	
				trgt2=n;	
				}

			if( trgt2 )
				{
				f=fc[2];	// 後面
				if( drctn3>=315.0 || drctn3<=45.0 )
					f=fc[0];	// 正面
				if( (drctn3>=45.0 && drctn3<=135.0) || (drctn3>=225.0 && drctn3<=315.0) )
					f=fc[1];	// 側面
			


				switch( unit[m].kind )
					{
					case BB1:	
					case GF3:
						SoundPlayEffect( NULL, GUN3 ,unit[m].x, unit[m].y);	
						break;
					case CA1:	
					case GF2:	
						SoundPlayEffect( NULL, GUN2 ,unit[m].x, unit[m].y);
						break;

					case DD1:	
					case SS1:	
					case GF1:	
						SoundPlayEffect( NULL, GUN1+rnd(2) ,unit[m].x, unit[m].y);
						break;
					}

				for( i=1; i<=f ;i++ )
					{

					n=seek_fire_no();
					if( n )
						{
						unit[m].arm[1]-=GUN_SZ;			// 弾薬消費
						fire[n].used=trgt2;


						if( unit[trgt2].ctgry==SHIP )
							{
							fire[n].kind=GUN/*kind*/;
	
							fire[n].x=unit[m].x;
							fire[n].y=unit[m].y;

							if( kind==SP_GUN && 0 )
								{
								if( rnd(3)!=0 )
									drctn3=drctn2+(double)((double)(rnd(20)-10)/10)    /*+(rnd(100)/100)*/;			// 絶対方位
								else
									drctn3=drctn2+(double)((double)(rnd(60)-30)/10)    /*+(rnd(100)/100)*/;			// 絶対方位
								}
							else
								{
								if( rnd(5)==0 || ( unit[m].kind==BB1 && rnd(4)==0 ) )
									drctn3=drctn2+(rnd(3)-1)+(rnd(100)/100);			// 絶対方位
								else
									drctn3=drctn2+(rnd(9)-4)+(rnd(100)/100);				// 絶対方位
								}

							if(drctn3>=360)	drctn3=drctn3-360;
							if(drctn3<0)	drctn3=360+drctn3;
							fire[n].drctn=drctn3;

							if( kind==SP_GUN )
								dstc2=dstc+(rnd( ((int)(dstc/12)) )-((int)(dstc/24)));
							else
								dstc2=dstc+(rnd( ((int)(dstc/8)) )-((int)(dstc/16))   );

							fire[n].spd=10.0;
							fire[n].spd_add=((fire[n].spd)/(dstc2/fire[n].spd));
							fire[n].last_spd=0;
							fire[n].info[0]=(int)(dstc2/fire[n].spd);
							fire[n].info[1]=fire[n].info[0]/2;
							}
						else
							{
							fire[n].kind=SHL;

							fire[n].x=unit[m].x;
							fire[n].y=unit[m].y;

							drctn3=drctn2+(rnd(20)-10);			// 絶対方位
							if(drctn3>=360)	drctn3=drctn3-360;				
							if(drctn3<0)	drctn3=360+drctn3;				
							fire[n].drctn=drctn3;

							dstc2=dstc+(rnd( ((int)(dstc/10)) )-((int)(dstc/20))   );

							fire[n].spd=10.0;
							fire[n].spd_add=-0.00;
							fire[n].last_spd=0;
							fire[n].info[0]=(int)(dstc2/fire[n].spd);
							fire[n].info[1]=0;
							}


						}
					}
				}
			return;
			}



		if( kind==BLT )
			{
			// 艦船の対空機銃 
			trgt=0;
			for(n=1;n<=max_unit;n++)
				{	//敵を探す。
				if( unit[n].used && ( (unit[n].ctgry==PLANE && unit[n].info[0]==FLYING )  || (unit[n].kind>=GF1 && unit[n].kind<=GF3 ) ) && unit[n].kind!=BM1
				&& unit[n].used!=unit[m].used  && unit[n].found )
					{
					// 全方位射撃可能
					wrk_x=unit[n].x-unit[m].x;
					wrk_y=unit[n].y-unit[m].y;

					drctn=atan2(wrk_y,wrk_x)*RAD_to;
				
					if(drctn<0)
						drctn=360+drctn;
					drctn2=drctn+(25-rnd(50));
//drctn2=drctn+(25);
					drctn2=abs((int)drctn2)%360;


					// 攻撃地点から攻撃目標地点への距離
					wrk_x=unit[n].x-unit[m].x;
					wrk_y=unit[n].y-unit[m].y;
					if(wrk_x==0)	wrk_x=1;
					if(wrk_y==0)	wrk_y=1;
					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					if(wrk_x<0)
						wrk_x=0-wrk_x;
					if(wrk_y<0)
						wrk_y=0-wrk_y;
					if(drctn>=180)
						drctn=drctn-180;
					if(drctn>=90)
						drctn=90-(drctn-90);
					dstc=(wrk_x)/(cos(drctn*a_PI));
					if( dstc<=100 )
						{	
						trgt=n;
						if(rnd(2)==1 )
//if(1 )
							break;
						else
							trgt=0;
						}
					else
						{
						if( dstc<=300 )
							{	
							trgt=n;
							if(rnd(8)==1 )
//if( 1 )
								break;	
							else
								trgt=0;
							}
						}
					}
				}


			if( trgt )
				{
				n=seek_fire_no();
				if( n )
					{
					//unit[m].arm[1]--;			// 弾薬消費
					SoundPlayEffect( NULL, AA_BLT3 ,unit[m].x, unit[m].y);
					fire[n].used=trgt;
					fire[n].kind=kind;
					fire[n].x=unit[m].x;
					fire[n].y=unit[m].y;
					fire[n].drctn=drctn2;
					fire[n].spd=17.0;
					fire[n].spd_add=-0.1;
					fire[n].last_spd=15.0;
					}
				}
			return;
			}
		}




	if( unit[m].ctgry==PLANE )
		{
				//=========		 航空機の射撃制御		=========//
		if( kind==BLT && unit[m].kind==BM1 )
			{
			// 航空機の全方向対空機銃 
			trgt=0;
			for(n=1;n<=max_unit;n++)
				{	//敵を探す。
				if( unit[n].used && unit[n].ctgry==PLANE && unit[n].info[0]==FLYING && unit[n].used!=unit[m].used /*&& unit[n].hp[0]>=unit[n].hp[2]+1*/ && unit[n].found )
					{
					// 全方位射撃可能
					wrk_x=unit[n].x-unit[m].x;
					wrk_y=unit[n].y-unit[m].y;

					drctn=atan2(wrk_y,wrk_x)*RAD_to;
				
					if(drctn<0)
						drctn=360+drctn;
					drctn2=drctn+(rnd(20)-10);
					drctn2=abs((int)drctn2)%360;


					// 攻撃地点から攻撃目標地点への距離
					wrk_x=unit[n].x-unit[m].x;
					wrk_y=unit[n].y-unit[m].y;
					if(wrk_x==0)	wrk_x=1;
					if(wrk_y==0)	wrk_y=1;
					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					if(wrk_x<0)
						wrk_x=0-wrk_x;
					if(wrk_y<0)
						wrk_y=0-wrk_y;
					if(drctn>=180)
						drctn=drctn-180;
					if(drctn>=90)
						drctn=90-(drctn-90);
					dstc=(wrk_x)/(cos(drctn*a_PI));
					if( dstc<=100 )
						{	
						trgt=n;
						if(rnd(2)==1 )
							break;	
						else
							trgt=0;
						}		
					else
						{
						if( dstc<=300 )
							{	
							trgt=n;
							if(rnd(8)==1 )
								break;	
							else
								trgt=0;
							}
						}
					}
				}
			if( trgt )
				{
				n=seek_fire_no();
				if( n )
					{
					//unit[m].arm[1]--;			// 弾薬消費
					SoundPlayEffect( NULL, AA_BLT4 ,unit[m].x, unit[m].y);
					fire[n].used=trgt;
					fire[n].kind=kind;
					fire[n].x=unit[m].x;
					fire[n].y=unit[m].y;
					fire[n].drctn=drctn2;
					fire[n].spd=17.0;
					fire[n].spd_add=-0.1;
					fire[n].last_spd=15.0;
					}
				}
			return;
			}





		if( kind==BLT && unit[m].kind==FT1 )
			{
			// 戦闘機
			// 前方固定銃
			trgt=0;		dstc2=5000;
			for(n=1;n<=max_unit;n++)
				{	//前方の敵を探す。
				if( unit[n].used && (( unit[n].ctgry==PLANE && unit[n].info[0]==FLYING ) || unit[n].kind==TR1 ) && unit[n].used!=unit[m].used )
					{
					// 距離を調べます
					wrk_x=unit[n].x-unit[m].x;
					wrk_y=unit[n].y-unit[m].y;
					if(wrk_x==0)	wrk_x=1;
					if(wrk_y==0)	wrk_y=1;

					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					if(wrk_x<0)
						wrk_x=0-wrk_x;
					if(wrk_y<0)
						wrk_y=0-wrk_y;
					if(drctn>=180)
						drctn=drctn-180;
					if(drctn>=90)
						drctn=90-(drctn-90);
					dstc=(wrk_x)/(cos(drctn*a_PI));
					if( dstc<=300 && dstc<=dstc2)
						{
						// 攻撃地点から攻撃目標地点への方位角
						wrk_x=unit[n].x-unit[m].x;
						wrk_y=unit[n].y-unit[m].y;
						drctn=atan2(wrk_y,wrk_x)*RAD_to;
						if(drctn<0)
							drctn=360+drctn;
						drctn=drctn-unit[m].drctn;
						if(drctn<0)
							drctn=360+drctn;
						if( ((int)drctn<=10||(int)drctn>=350) && unit[m].arm[2] && unit[m].arm[2]==n )
							{
							dstc2=dstc;
							trgt=n;
							}
						}


					if( !unit[m].arm[2] )
						{
						if(unit[m].info[5]==RETURN)
							{
#if 0
							if( /*unit[m].used==cpu_side &&*/ unit[m].gas[0]>=(60-(unit[m].used==JPN)*10) && unit[m].arm[1] )
								{
								if( dstc<=300 && rnd(10)==0 )
									{
									if( unit[n].found )
										{
										unit[m].arm[2]=n;
										}
									}

								}
#endif
							}
						else
							{
							if( dstc<=400+(unit[n].kind==AT1||unit[n].kind==BM1)*250 && rnd(10)==0 )
								{
								if( unit[n].found )
									{
									unit[m].arm[2]=n;
									}
								}
							}
						}
					}
				}




			if( trgt )
				{
				n=seek_fire_no();
				if( n )
					{
					unit[m].arm[1]--;			// 弾薬消費
					if(unit[m].type==0)
						{
						// 艦上戦闘機
						if(unit[m].used==JPN)
							SoundPlayEffect( NULL, AA_BLT1 ,unit[m].x, unit[m].y);
						else
							SoundPlayEffect( NULL, AA_BLT2 ,unit[m].x, unit[m].y);
						}
					else
						{
						// 陸上戦闘機
						SoundPlayEffect( NULL, AA_SHL4 ,unit[m].x, unit[m].y);
						}

					fire[n].used=trgt;
					fire[n].kind=kind;
					fire[n].x=unit[m].x;
					fire[n].y=unit[m].y;
					fire[n].drctn=unit[m].drctn;
					fire[n].spd=16.0;
					fire[n].spd_add=-0.1;
					fire[n].last_spd=14.0;
					}
				}
			return;
			}





		if( kind==BLT )
			{
			trgt=0;
			for(n=1;n<=max_unit;n++)
				{	//後方の敵を探す。
				if( unit[n].used && unit[n].ctgry==PLANE && unit[n].info[0]==FLYING && unit[n].used!=unit[m].used /*&& unit[n].hp[0]>=unit[n].hp[2]+1*/ && unit[n].found )
					{
					// 攻撃地点から攻撃目標地点への方位角
					wrk_x=unit[n].x-unit[m].x;
					wrk_y=unit[n].y-unit[m].y;
					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;	

					drctn2=drctn+(rnd(10)-5);
					if(drctn2>=360)	drctn2=drctn2-360;				
					if(drctn2<0)	drctn2=360+drctn2;				

					drctn=drctn-unit[m].drctn;
					if(drctn<0)
						drctn=360+drctn;
					if( (int)drctn>=150&&(int)drctn<=210 )
						{
						wrk_x=unit[n].x-unit[m].x;
						wrk_y=unit[n].y-unit[m].y;
						if(wrk_x==0)	wrk_x=1;
						if(wrk_y==0)	wrk_y=1;

						drctn=atan2(wrk_y,wrk_x)*RAD_to;
						if(drctn<0)
							drctn=360+drctn;
						if(wrk_x<0)
							wrk_x=0-wrk_x;
						if(wrk_y<0)
							wrk_y=0-wrk_y;
						if(drctn>=180)
							drctn=drctn-180;
						if(drctn>=90)
							drctn=90-(drctn-90);
						dstc=(wrk_x)/(cos(drctn*a_PI));
						if( dstc<=300 )
							{	trgt=n;	break;	}
						}
					}
				}
			if( trgt )
				{
				n=seek_fire_no();
				if( n )
					{
					//unit[m].arm[1]--;			// 弾薬消費
					SoundPlayEffect( NULL, AA_BLT3 ,unit[m].x, unit[m].y);
					fire[n].used=trgt;
					fire[n].kind=kind;
					fire[n].x=unit[m].x;
					fire[n].y=unit[m].y;
					fire[n].drctn=drctn2;
					fire[n].spd=16.0;
					fire[n].spd_add=-0.1;
					fire[n].last_spd=14.0;
					}
				}
			return;
			}




			if( kind==TPD && !(unit[trgt].kind>=AP && unit[trgt].kind<=GF3) )
				{
				// 攻撃機
				// トゥピード

				//地上の上なら投雷しない。

				if(!( unit[m].y>MAP_TOP || unit[m].y<MAP_BOTTOM || unit[m].x<MAP_LEFT || unit[m].x>MAP_RIGHT ))
					{
					cm_scrn_x=(int)((unit[m].x+(sprt[UNIT_JPN].wd/2)-MAP_LEFT)/sprt[MAP_TIP_NRML].wd);
					cm_scrn_y=(int)((MAP_TOP-unit[m].y+(sprt[UNIT_JPN].ht/2))/sprt[MAP_TIP_NRML].ht);

					if( cmbt_map[cm_scrn_y][cm_scrn_x]>=1 
						|| cmbt_map[cm_scrn_y-1][cm_scrn_x-1]>=1 
						|| cmbt_map[cm_scrn_y-1][cm_scrn_x]>=1 
						|| cmbt_map[cm_scrn_y-1][cm_scrn_x+1]>=1 

						|| cmbt_map[cm_scrn_y][cm_scrn_x-1]>=1 
						|| cmbt_map[cm_scrn_y][cm_scrn_x+1]>=1 

						|| cmbt_map[cm_scrn_y+1][cm_scrn_x-1]>=1 
						|| cmbt_map[cm_scrn_y+1][cm_scrn_x]>=1 
						|| cmbt_map[cm_scrn_y+1][cm_scrn_x+1]>=1 
						)
						{
						return;
						}
					}



			// 攻撃地点から攻撃目標地点への方位角
			if( !unit[trgt].found )
				return;

			trgt_x=unit[trgt].x;
			trgt_y=unit[trgt].y;

			trgt_x+=cos(unit[trgt].drctn*a_PI)*((AIR_TPD_LOS_DSTC/AIR_TPD_SPD)*unit[trgt].spd); // とりあえずターン後
			trgt_y+=sin(unit[trgt].drctn*a_PI)*((AIR_TPD_LOS_DSTC/AIR_TPD_SPD)*unit[trgt].spd);


			wrk_x=trgt_x-unit[m].x;
			wrk_y=trgt_y-unit[m].y;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;	

			drctn=drctn-unit[m].drctn;
			if(drctn<0)
				drctn=360+drctn;	


			if( (int)drctn<=45||(int)drctn>=315)
				{
				// 距離を求めます
				wrk_x=unit[m].x-trgt_x;
				wrk_y=unit[m].y-trgt_y;
				if(wrk_x==0)	wrk_x=1;
				if(wrk_y==0)	wrk_y=1;

				drctn=atan2(wrk_y,wrk_x)*RAD_to;
				if(drctn<0)
					drctn=360+drctn;
	drctn2=drctn;					// 未来位置への絶対角
				if(wrk_x<0)
					wrk_x=0-wrk_x;
				if(wrk_y<0)
					wrk_y=0-wrk_y;
				if(drctn>=180)
					drctn=drctn-180;
				if(drctn>=90)
					drctn=90-(drctn-90);
				dstc=(wrk_x)/(cos(drctn*a_PI));





				if( dstc>=100 && dstc<=AIR_TPD_LOS_DSTC )
					{
					i=dstc/2;
					for(f=1;f<=i;f++)
						{
						wrk_x=unit[m].x;
						wrk_y=unit[m].y;
						wrk_x+=cos(drctn2*a_PI)*(2*f); // とりあえずターン後
						wrk_y+=sin(drctn2*a_PI)*(2*f);

						if(!( wrk_y>MAP_TOP || wrk_y<MAP_BOTTOM || wrk_x<MAP_LEFT || wrk_x>MAP_RIGHT ))
							{
							cm_scrn_x=(int)((wrk_x+(sprt[UNIT_JPN].wd/2)-MAP_LEFT)/sprt[MAP_TIP_NRML].wd);
							cm_scrn_y=(int)((MAP_TOP-wrk_y+(sprt[UNIT_JPN].ht/2))/sprt[MAP_TIP_NRML].ht);
							if( cmbt_map[cm_scrn_y][cm_scrn_x]>=1 )
								{
								return;
								}
							}
						}


					// 発射！
					unit[m].arm[1]=0;		// 魚雷が０
					unit[m].arm[2]=0;		// ターゲットをクリア

					unit[m].info[5]=RETURN;		// 航空機はメイン兵器ゼロで帰投


					// 雷撃時に適当に移動さす
					wrk_x=unit[m].x;
					wrk_y=unit[m].y;
					drctn=unit[m].drctn;

					if(rnd(2)==0)
						drctn+=(70-rnd(40));
					else
						drctn-=(70-rnd(40));

					drctn=(int)(drctn)%360;


					wrk_x+=cos(drctn*a_PI)*300; 
					wrk_y+=sin(drctn*a_PI)*300;

					unit[m].em_x=wrk_x;
					unit[m].em_y=wrk_y;
					unit[m].em_flg[0]=20+rnd(300);


					




					// 部下、多分戦闘機に帰投命令					
					if(unit[m].is_ltl_ldr)
						{
						for(f=1;f<=max_unit;f++)
							{
							if( unit[f].used && unit[f].ltl_ldr==m && unit[f].info[0]==FLYING )
								{
								unit[f].ltl_ldr=0;
								unit[f].info[5]=RETURN;

unit[f].pp_x[0]=unit[m].x;
unit[f].pp_y[0]=unit[m].y;
unit[f].pp_x[1]=MAP_RIGHT+1;

								}
							}

						unit[m].is_ltl_ldr=0;
						}


					if( !unit[m].is_ltl_ldr )
						{
						unit[m].pp_x[0]=unit[m].x;
						unit[m].pp_y[0]=unit[m].y;
						unit[m].pp_x[1]=MAP_RIGHT+1;
						}

					n=seek_fire_no();
					if( n )
						{
						SoundPlayEffect( NULL, SPL1 ,unit[m].x, unit[m].y);
						fire[n].used=trgt;
						fire[n].kind=kind;
						fire[n].x=unit[m].x;
						fire[n].y=unit[m].y;
						//fire[n].drctn=(int)(unit[m].drctn+(2-rnd(4)))%360;
						fire[n].drctn=(int)unit[m].drctn;
						fire[n].spd=AIR_TPD_SPD;
						fire[n].spd_add=+0.0;
						fire[n].last_spd=0.0;
						fire[n].info[0]=0;
						fire[n].info[1]=240;
						fire[n].info[2]=15;
						}
					}
				}
			return;
			}




		if( kind==BOM && unit[m].kind==AT1 )
			{
			// 攻撃機
			// 爆撃
			// 攻撃地点から攻撃目標地点への方位角
			if( !unit[trgt].found )
				return;
			wrk_x=unit[trgt].x;
			wrk_y=unit[trgt].y;
			wrk_x+=cos(unit[trgt].drctn*a_PI)*(unit[trgt].spd*70.0);
			wrk_y+=sin(unit[trgt].drctn*a_PI)*(unit[trgt].spd*70.0);

			wrk_x2=wrk_x;	wrk_y2=wrk_y;



			wrk_x=wrk_x-unit[m].x;
			wrk_y=wrk_y-unit[m].y;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;	
			drctn2=drctn;
			drctn=drctn-unit[m].drctn;
			if(drctn<0)
				drctn=360+drctn;	

			if( (int)drctn<=30/*45*/||(int)drctn>=330/*315*/ )
				{
				// 距離を求めます
				wrk_x=unit[m].x-wrk_x2;
				wrk_y=unit[m].y-wrk_y2;
				if(wrk_x==0)	wrk_x=1;
				if(wrk_y==0)	wrk_y=1;

				drctn=atan2(wrk_y,wrk_x)*RAD_to;
				if(drctn<0)
					drctn=360+drctn;
				if(wrk_x<0)
					wrk_x=0-wrk_x;
				if(wrk_y<0)
					wrk_y=0-wrk_y;
				if(drctn>=180)
					drctn=drctn-180;
				if(drctn>=90)
					drctn=90-(drctn-90);
				dstc=(wrk_x)/(cos(drctn*a_PI));




//				if( dstc>=170/*40*/ && dstc<=180/*50*/  )
				if( ( dstc>=170 && dstc<=180 && unit[m].used==USA ) || ( dstc>=35 && dstc<=65 && unit[m].used==JPN ))
					{
					// 発射！
//					SoundPlayEffect( NULL, BOMB_OFF ,unit[m].x, unit[m].y);

					if( unit[m].used==USA )
						unit[m].spd+=unit[m].a_spd_add*700;
					else
						{
						wrk_x=unit[m].x;
						wrk_y=unit[m].y;
						drctn=unit[m].drctn;
						wrk_x+=cos(drctn*a_PI)*300; 
						wrk_y+=sin(drctn*a_PI)*300;
						unit[m].em_x=wrk_x;
						unit[m].em_y=wrk_y;
						unit[m].em_flg[0]=150+rnd(50);
						}

					unit[m].arm[1]=0;		// 消費
					unit[m].arm[2]=0;		

					unit[m].info[5]=RETURN;		// 航空機はメイン兵器ゼロで帰投


					// 部下、多分戦闘機に帰投命令					
					if(unit[m].is_ltl_ldr)
						{
						for(f=1;f<=max_unit;f++)
							{
							if( unit[f].used && unit[f].ltl_ldr==m && unit[f].info[0]==FLYING)
								{
								unit[f].ltl_ldr=0;
								unit[f].info[5]=RETURN;

unit[f].pp_x[0]=unit[m].x;
unit[f].pp_y[0]=unit[m].y;
unit[f].pp_x[1]=MAP_RIGHT+1;

								}
							}
						unit[m].is_ltl_ldr=0;
						}



					n=seek_fire_no();
					if( n )
						{
//						fire[n].used=trgt;
						fire[n].used=AT1;
						fire[n].kind=kind;
						fire[n].x=unit[m].x+(3-rnd(6));
						fire[n].y=unit[m].y+(3-rnd(6));
						fire[n].drctn=drctn2;
	
						if(unit[m].used==JPN)
							{
							fire[n].x+=cos(fire[n].drctn*a_PI)*(13);
							fire[n].y+=sin(fire[n].drctn*a_PI)*(13);
							}
						else
							{
							fire[n].x+=cos(fire[n].drctn*a_PI)*(130);
							fire[n].y+=sin(fire[n].drctn*a_PI)*(130);
							}

						fire[n].spd=0.3;
						fire[n].spd_add=+0.2;
						fire[n].last_spd=0.0;
						if(unit[m].used==JPN)
							{
							fire[n].info[0]=10;
							fire[n].info[1]=68+rnd(5);
							}
						else
							{
							fire[n].info[0]=0;
							fire[n].info[1]=70;
							}
							
						}

//					if( unit[m].used==USA )
//						{
						// もう一発
						n=seek_fire_no();
						if( n )
							{
							fire[n].used=trgt;
							fire[n].kind=kind;
							fire[n].x=unit[m].x+(20-rnd(40));
							fire[n].y=unit[m].y+(20-rnd(40));
							fire[n].drctn=drctn2;
	
							if(unit[m].used==JPN)
								{
								fire[n].x+=cos(fire[n].drctn*a_PI)*(13);
								fire[n].y+=sin(fire[n].drctn*a_PI)*(13);
								}
							else
								{
								fire[n].x+=cos(fire[n].drctn*a_PI)*(130);
								fire[n].y+=sin(fire[n].drctn*a_PI)*(130);
								}

							fire[n].spd=0.3;
							fire[n].spd_add=+0.2;
							fire[n].last_spd=0.0;

							if(unit[m].used==JPN)
								{
								fire[n].info[0]=10;
								fire[n].info[1]=75+(5-rnd(10));
								}
							else
								{
								fire[n].info[0]=0;
								fire[n].info[1]=70+(5-rnd(10));
								}
							}
//						}
					}
				}
			return;
			}

		if( kind==BOM && unit[m].kind==BM1 )
			{	
			// 爆撃機
			// 爆撃
			trgt2=0;
			if(trgt)
				trgt2=trgt;
			for(n=1;n<=max_unit;n++)
				{	//前方の敵を探す。
				if(trgt2)
					{ n=trgt2; trgt=0; }
				if( unit[n].used && unit[n].ctgry==SHIP && unit[n].used!=unit[m].used && unit[n].found )
					{
					// 攻撃地点から攻撃目標地点への方位角
					wrk_x=unit[n].x-unit[m].x;
					wrk_y=unit[n].y-unit[m].y;
					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;	
					drctn2=drctn;

					drctn2=drctn+(rnd(10)-5);
					if(drctn2>=360)	drctn2=drctn2-360;				
					if(drctn2<0)	drctn2=360+drctn2;				

					drctn=drctn-unit[m].drctn;
					if(drctn<0)
						drctn=360+drctn;
					if( (int)drctn<=30 || (int)drctn>=330 )
						{
						wrk_x=unit[n].x-unit[m].x;
						wrk_y=unit[n].y-unit[m].y;
						if(wrk_x==0)	wrk_x=1;
						if(wrk_y==0)	wrk_y=1;

						drctn=atan2(wrk_y,wrk_x)*RAD_to;
						if(drctn<0)
							drctn=360+drctn;
						if(wrk_x<0)
							wrk_x=0-wrk_x;
						if(wrk_y<0)
							wrk_y=0-wrk_y;
						if(drctn>=180)
							drctn=drctn-180;
						if(drctn>=90)
							drctn=90-(drctn-90);
						dstc=(wrk_x)/(cos(drctn*a_PI));
						if( dstc>=30 && dstc<=60 )
							{	trgt=n;	break;	}
						}
					}
				if( trgt2 )
					break;
				}
			if( trgt )
				{
				n=seek_fire_no();
				if( n )
					{
					SoundPlayEffect( NULL, BB_BOMB ,unit[m].x, unit[m].y);
					if(unit[m].arm[1])
						unit[m].arm[1]--;		// 消費

					if( unit[m].arm[1]<=0)
						{
						unit[m].arm[1]=0;		// 消費
						unit[m].arm[2]=0;		
						unit[m].info[5]=RETURN;		// 航空機はメイン兵器ゼロで帰投
						}

					unit[m].arm[3]=5;		// 再装填時間

					if(unit[m].arm[1]<=0)
						unit[m].info[5]=RETURN;		// 航空機はメイン兵器ゼロで帰投


					fire[n].used=BM1;
					fire[n].kind=kind;
					fire[n].x=unit[m].x+((double)(-6+rnd(13)));
					fire[n].y=unit[m].y+((double)(-6+rnd(13)));
					fire[n].drctn=drctn2;
					fire[n].spd=0.3;
					fire[n].spd_add=+0.2;
					fire[n].last_spd=0.0;
					fire[n].info[0]=0;
					fire[n].info[1]=70;
					}
				}




			return;
			}

		}
	}









//============================================================================
// 編隊のポジションをＰｐ＿ｘｙ「０」にセットします。
//----------------------------------------------------------------------------
void	set_pos_of_dynmc( int	n )
	{
	double			angl,dstc;
	int				pt,pos_of_no,a,b,c;
	int				nums;




	if( unit[n].ctgry==PLANE )
		{
	
		// 航空機編隊の制御

		pt=unit[n].ltl_ldr;
		
		if(unit[pt].info[0]==PARKING && unit[pt].info[5]==RETURN && unit[n].info[0]==FLYING )
			{
			unit[n].ltl_ldr=0;
			return;
			}


		pos_of_no=unit[n].no;

		if(unit[pt].ctgry==SHIP)	// こっちは飛行機だが指揮が艦船の場合
			{
			angl=(double)rnd(359);
			dstc=(double)rnd(500+150);
			unit[n].info[5]=MOVE;

			// 目的地を決定
			unit[n].pp_x[0]=unit[pt].x+cos(angl*a_PI)*dstc;
			unit[n].pp_y[0]=unit[pt].y+sin(angl*a_PI)*dstc;
			unit[n].pp_x[1]=MAP_RIGHT+1;
			}
		else
			{	
			// 指揮が通常移動
			nums=5/*6*/;			// １小隊何機か

			// 何番編隊か
			a=unit[n].no/nums;
			// 何番機か
			b=unit[n].no%nums;

			//if(a==1&&b==1)
			//	dstc=250+320;
				

			switch( b )
				{
				case 0:		// １番機
					angl=0;	dstc=0;
					break;
				case 1:		// 2番機
					angl=unit[pt].drctn-90.0-45.0;	dstc=60*0.80;
					break;
				case 2:		// 
					angl=unit[pt].drctn-90.0-45.0-90.0;	dstc=60*0.80;
					break;
				case 3:		// 
					angl=unit[pt].drctn-90.0-45.0;	dstc=120*0.80;
					break;
				case 4:		// 
					angl=unit[pt].drctn-90.0-45.0-90.0;	dstc=120*0.80;
					break;
				case 5:		// 
					angl=unit[pt].drctn-90.0-45.0-45.0;	dstc=100*0.80;
					break;
				}



			// 目的地を決定
			unit[n].pp_x[0]=unit[pt].x+cos(angl*a_PI)*dstc;
			unit[n].pp_y[0]=unit[pt].y+sin(angl*a_PI)*dstc;


			if( a>=0 )
				{
				// 各編隊随伴指揮機位置
				switch( a )
					{
					case 0:		// 1番編隊
						angl=unit[pt].drctn;	dstc=35;
						break;
					case 1:		// 2番編隊
						angl=unit[pt].drctn-90.0-45.0;	dstc=180*0.80;
						break;
					case 2:		// 
						angl=unit[pt].drctn-90.0-45.0-90.0;	dstc=180*0.80;
						break;
					case 3:		// 
						angl=unit[pt].drctn-90.0-45.0;	dstc=360*0.80;
						break;
					case 4:		// 
						angl=unit[pt].drctn-90.0-45.0-45.0;	dstc=300*0.80;
						break;
					case 5:		// 
						angl=unit[pt].drctn-90.0-45.0-90.0;	dstc=360*0.80;
						break;


					case 6:		// 
						angl=unit[pt].drctn-90.0-45.0;	dstc=540*0.80;
						break;
					case 7:		// 
						angl=unit[pt].drctn-90.0-45.0-22.5;	dstc=480*0.80;
						break;
					case 8:		// 
						angl=unit[pt].drctn-90.0-45.0-45.0-22.5;	dstc=480*0.80;
						break;
					case 9:		// 
						angl=unit[pt].drctn-90.0-45.0-90.0;		dstc=540*0.80;
						break;
					case 10:		// 
						angl=unit[pt].drctn-90.0-45.0-11.2;		dstc=640*0.80;
						break;
					case 11:		// 
						angl=unit[pt].drctn-90.0-45.0-90.0+11.2;		dstc=640*0.80;
						break;
					}


				// 目的地を決定
				unit[n].pp_x[0]=unit[n].pp_x[0]+cos(angl*a_PI)*dstc;
				unit[n].pp_y[0]=unit[n].pp_y[0]+sin(angl*a_PI)*dstc;
				unit[n].pp_x[1]=MAP_RIGHT+1;
				}
			}
		}
	else
		{
		// 艦隊制御
		pt=unit[n].ltl_ldr;
		angl=unit[n].to_ldr_drctn+unit[pt].drctn;
		if( angl>=360 )
			angl = angl-360;
		dstc=unit[n].to_ldr_dstc;


		unit[n].pp_x[0]=unit[pt].x+cos(angl*a_PI)*dstc;
		unit[n].pp_y[0]=unit[pt].y+sin(angl*a_PI)*dstc;
		unit[n].pp_x[1]=MAP_RIGHT+1;



		if( 1 /*|| unit[n].used==cpu_side*/ )
			{
			//コンピュータの進路計算
			set_cpu_root2( n );
			}
		}





//	unit[n].pp_now=0;
	unit[n].stop=0;

	}



//============================================================================
// 戦闘機の緊急起動をセット
//----------------------------------------------------------------------------
void	chk_another_unit( double *rx,  double *ry)
	{
	int		n;
	double	wrk_x,wrk_y;
	RECT	wrk_r;	



	for( n=1; n<=max_unit; n++)
		{
		if( unit[n].used && unit[n].ctgry==SHIP )
			{
			// ptin dbg
			wrk_r.top=(int)unit[n].y+(sprt[UNIT_JPN].ht/2);//(int)unit[n].y-(sprt[UNIT_JPN].ht/2);
			wrk_r.right=(int)unit[n].x+(sprt[UNIT_JPN].wd/2);
			wrk_r.bottom=(int)unit[n].y-(sprt[UNIT_JPN].ht/2);//(int)unit[n].y+(sprt[UNIT_JPN].ht/2);
			wrk_r.left=(int)unit[n].x-(sprt[UNIT_JPN].wd/2);

			if( pt_in_rect3(&wrk_r,(int)*rx,(int)*ry))
				{
				n=0;
				*ry-=80;
				}
			}
		}
	}


//============================================================================
// 戦闘機の緊急起動をセット
//----------------------------------------------------------------------------
void	set_pos_of_emrgncy_FT( int	m )
	{

#if 0
	// 損傷がひどくなったら逃げよう
	if( unit[m].kind==FT1 && unit[m].info[5]!=RETURN && unit[m].hp[0]<=unit[m].hp[1]/2 )
		{
		unit[m].arm[2]=0;		// ターゲットをクリア
		unit[m].info[5]=RETURN;		// 航空機はメイン兵器ゼロで帰投

		unit[m].ltl_ldr=0;

		if( !unit[m].is_ltl_ldr )
			{
			unit[m].pp_x[0]=unit[m].x;
			unit[m].pp_y[0]=unit[m].y;
			unit[m].pp_x[1]=MAP_RIGHT+1;
			}
		}
#endif


	// 目標も、爆弾も無く、損傷がひどいかガソリンが切れそうな場合はきとうしよう
	if( unit[m].kind==FT1 && unit[m].info[5]!=RETURN && (unit[m].gas[0]<=30 || unit[m].hp[0]<=unit[m].hp[1]*0.70 || unit[m].arm[1]<=0 ) )
		{
		// 発射！
		unit[m].arm[2]=0;		// ターゲットをクリア
		unit[m].info[5]=RETURN;		// 航空機はメイン兵器ゼロで帰投
		unit[m].ltl_ldr=0;

		if( !unit[m].is_ltl_ldr )
			{
			unit[m].pp_x[0]=unit[m].x;
			unit[m].pp_y[0]=unit[m].y;
			unit[m].pp_x[1]=MAP_RIGHT+1;
			}
		}
	}








//============================================================================
// 戦闘機の攻撃機動をセット
//----------------------------------------------------------------------------
void	set_pos_of_attack_FT( int	m )
	{
	double			angl,dstc,wrk_x,wrk_y,drctn,drctn2,em_drctn;
	int				trgt,pos_of_no,a,b,c;
	int				lvl_my,lvl_en,my_tec,en_tec,n;
	RECT			wrk_r;


	// ptin dbg
	wrk_r.top=(int)unit[m].em_y+35;//(int)unit[m].em_y-35;
	wrk_r.right=(int)unit[m].em_x+35;
	wrk_r.bottom=(int)unit[m].em_y-35;//(int)unit[m].em_y+35;
	wrk_r.left=(int)unit[m].em_x-35;

	if( pt_in_rect3(&wrk_r,(int)unit[m].x,(int)unit[m].y) )
		{
		unit[m].em_x=unit[m].x+cos(unit[m].drctn*a_PI)*(100+rnd(50));
		unit[m].em_y=unit[m].y+sin(unit[m].drctn*a_PI)*(100+rnd(50));
		unit[m].em_flg[0]=100;
		unit[m].stop=0;
		return;
		}





	trgt=unit[m].arm[2];
	lvl_my=unit[m].tech;
	lvl_en=unit[trgt].tech;

	if( lvl_my == lvl_en )
		{
		if( ((cc_count)%600)<300 )
			{lvl_my++;}
		else
			{lvl_en++;}
		}
	if( lvl_my > lvl_en )
		{	my_tec=10;	en_tec=40+(lvl_my-lvl_en);	}
	if( lvl_my < lvl_en )
		{	my_tec=40+(lvl_en-lvl_my);	en_tec=10;	}


	if( rnd(my_tec)!=0 )
		return;



	// 正面打ち合いをさけるようにします。
	n=trgt;
	// 自機ｍと敵機ｎの絶対角を調べます。
	wrk_x=unit[m].x-unit[n].x;
	wrk_y=unit[m].y-unit[n].y;
	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;					// drctnが絶対角

	drctn=drctn-unit[n].drctn;				// 敵機ｎからの方位角をしらべます。
	if(drctn<0)
		drctn=360+drctn;
	if( (int)drctn<=5 || (int)drctn>=355 )
		{
		// 敵機が正面に自機を捕らえています。
		wrk_x=unit[n].x-unit[m].x;
		wrk_y=unit[n].y-unit[m].y;
		drctn=atan2(wrk_y,wrk_x)*RAD_to;
		if(drctn<0)
			drctn=360+drctn;					// drctnが絶対角

		drctn=drctn-unit[m].drctn;				// 自機ｍからの方位角をしらべます。
		if(drctn<0)
			drctn=360+drctn;

		if( (int)drctn<=5 || (int)drctn>=355 )
			{
			// 自機も敵機を正面に捕らえています
			wrk_x=unit[n].x-unit[m].x;
			wrk_y=unit[n].y-unit[m].y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			dstc=(wrk_x)/(cos(drctn*a_PI));
			if( dstc>=160 && dstc<=320 )
				{	
				// さらに、距離が近い よけよう
				wrk_x=unit[m].x;
				wrk_y=unit[m].y;
				em_drctn=unit[m].drctn;
				switch( rnd(2) )
					{
					case 0:
						em_drctn+=45+rnd(45);
						break;
					case 1:
						em_drctn-=45+rnd(45);
						break;
					}
				em_drctn=(int)em_drctn%360;


				wrk_x+=cos(em_drctn*a_PI)*300; 
				wrk_y+=sin(em_drctn*a_PI)*300;

				unit[m].em_x=wrk_x;
				unit[m].em_y=wrk_y;
				unit[m].em_flg[0]=70+rnd(40);

				return;
				}
			}
		}







	// 敵機の直前にＥｍ＿Ｘｙを設定します。
	wrk_x=unit[trgt].x;
	wrk_y=unit[trgt].y;
	wrk_x+=cos(unit[trgt].drctn*a_PI)*(unit[trgt].spd*20.0); 
	wrk_y+=sin(unit[trgt].drctn*a_PI)*(unit[trgt].spd*20.0);

	unit[m].em_x=wrk_x;
	unit[m].em_y=wrk_y;
	unit[m].em_flg[0]=70+rnd(40);
	unit[m].stop=0;
	if(rnd(10)==0)
		SoundPlayEffect( NULL, PLANE1+rnd(2) ,unit[m].x, unit[m].y);

	}




//============================================================================
// 攻撃機の緊急起動をセット
//----------------------------------------------------------------------------
void	set_pos_of_emrgncy_AT( int	m )
	{
	int		n,g,new_ldr,f;
	double	em_drctn,wrk_x,wrk_y,drctn,dstc,drctn2;

	

	// 戦闘機から逃げよう
	for(n=1;n<=max_unit;n++)
		{	//後方の敵を探す。
		if( unit[n].used && unit[n].ctgry==PLANE && unit[n].info[0]==FLYING 
		&& unit[n].used!=unit[m].used && unit[n].found )
			{

			// 攻撃地点から攻撃目標地点への方位角
			wrk_x=unit[n].x-unit[m].x;
			wrk_y=unit[n].y-unit[m].y;
			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;	

			drctn=drctn-unit[m].drctn;
			if(drctn<0)
				drctn=360+drctn;


			// 攻撃目標地点から攻撃地点への方位角
			wrk_x=unit[m].x-unit[n].x;
			wrk_y=unit[m].y-unit[n].y;
			drctn2=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn2<0)
				drctn2=360+drctn2;	

			drctn2=drctn2-unit[n].drctn;
			if(drctn2<0)
				drctn2=360+drctn2;




			if( ((int)drctn>=150 && (int)drctn<=210 && unit[n].kind==FT1) 
				|| 
				( ((int)drctn<=45 || (int)drctn>=315) && ( (int)drctn2>=135 && (int)drctn2<=225)  && unit[n].kind==AT1 && unit[m].arm[2]==0 )
				/*||
				(  ( ((int)drctn>=150&&(int)drctn<=210) || ((int)drctn<=45||(int)drctn>=315) )  && unit[n].kind==FT1 && unit[m].kind==BM1) 
				*/
			  )
				{


				wrk_x=unit[n].x-unit[m].x;
				wrk_y=unit[n].y-unit[m].y;
				if(wrk_x==0)	wrk_x=1;
				if(wrk_y==0)	wrk_y=1;

				drctn=atan2(wrk_y,wrk_x)*RAD_to;
				if(drctn<0)
					drctn=360+drctn;
				if(wrk_x<0)
					wrk_x=0-wrk_x;
				if(wrk_y<0)
					wrk_y=0-wrk_y;
				if(drctn>=180)
					drctn=drctn-180;
				if(drctn>=90)
					drctn=90-(drctn-90);
				dstc=(wrk_x)/(cos(drctn*a_PI));
				if( dstc<=250+((unit[n].kind==AT1)*70) )
					{	

					wrk_x=unit[m].x;
					wrk_y=unit[m].y;
					em_drctn=unit[m].drctn;
					switch(rnd(2))
						{
						case 0:
							em_drctn+=45+rnd(90);
							break;
						case 1:
							em_drctn-=45+rnd(90);
							break;
						}
					/*(int)*/em_drctn=(int)em_drctn%360;


					wrk_x+=cos(em_drctn*a_PI)*300; 
					wrk_y+=sin(em_drctn*a_PI)*300;

					unit[m].em_x=wrk_x;
					unit[m].em_y=wrk_y;
					unit[m].em_flg[0]=100;


					return;
					}
				}
			}
		}








	// 損傷がひどいかガソリンが切れそうな場合はきとうしよう
	if( unit[m].info[5]!=RETURN && (unit[m].gas[0]<=20 || unit[m].hp[0]<=unit[m].hp[1]*0.70 ) 
		/*&& unit[unit[m].info[1]].used*/
		)
		{
		unit[m].arm[1]=0;		// 魚雷が０
		unit[m].arm[2]=0;		// ターゲットをクリア


		if(unit[unit[m].info[1]].used)		
			{
		unit[m].info[5]=RETURN;		// 航空機はメイン兵器ゼロで帰投

		unit[m].ltl_ldr=0;

		if( !unit[m].is_ltl_ldr )
			{
			unit[m].pp_x[0]=unit[m].x;
			unit[m].pp_y[0]=unit[m].y;
			unit[m].pp_x[1]=MAP_RIGHT+1;
			}
		else
			{

			n=max_unit+1;
			g=1;
			new_ldr=0;

			for(f=1;f<=max_unit;f++)
				{
				if( unit[f].used && unit[f].ltl_ldr==m )
					{
					g++;
					if( unit[f].no < n )
						{
						n=unit[f].no;
						new_ldr=f;					// これが新しい隊長番号
						}
					}
				}

			if(new_ldr && g>=2 )
				{
				unit[new_ldr].is_ltl_ldr=g;
				unit[new_ldr].ltl_ldr=0;
				unit[new_ldr].no=0;


				// 昔の小隊長が攻爆撃機だったら、帰投にしておく
				if( unit[new_ldr].kind==FT1 && (unit[m].kind==AT1 || unit[m].kind==BM1) /*&& unit[m].info[0]==FLYING*/ && unit[new_ldr].info[0]==FLYING )
					{
					unit[new_ldr].info[5]=RETURN;		// それまでの隊長がボスだったらきかんしよっと
					}


				for(f=1;f<=max_unit;f++)
					{
					if( unit[f].used && unit[f].ltl_ldr==m )
						{
						unit[f].ltl_ldr=new_ldr;

						// 昔の小隊長が攻爆撃機だったら、帰投にしておく
						if( unit[new_ldr].kind==FT1 && unit[f].kind==FT1 && (unit[m].kind==AT1 || unit[m].kind==BM1) /*&& unit[m].info[0]==FLYING*/&& unit[f].info[0]==FLYING )
							unit[f].info[5]=RETURN;		// それまでの隊長がボスだったらきかんしよっと
						}


					}

				for(f=0;f<64;f++)
					{
					unit[new_ldr].pp_x[f]=unit[m].pp_x[f];
					unit[new_ldr].pp_y[f]=unit[m].pp_y[f];
					}

				// 昔の小隊長
				unit[m].pp_x[0]=unit[m].x;
				unit[m].pp_y[0]=unit[m].y;
				unit[m].pp_x[1]=MAP_RIGHT+1;


				}
			}
			}
		}

	}






//============================================================================
// 攻撃機の攻撃機動をＰｐ＿ｘｙにセット
//----------------------------------------------------------------------------
void	set_pos_of_attack_AT( int	m )
	{
	double			angl,dstc,wrk_x,wrk_y,drctn,drctn2,drctn3,turn;
	int				trgt,pos_of_no,a,b,c,i;
	int				nums,lvl_jp,lvl_us,jp_tec,us_tec,n,f;
	RECT			wrk_r;



	n=unit[m].arm[2];				// 攻撃目標


	// 現位置から攻撃目標地点への距離
	wrk_x=unit[n].x-unit[m].x;
	wrk_y=unit[n].y-unit[m].y;
	if(wrk_x==0)	wrk_x=1;
	if(wrk_y==0)	wrk_y=1;

	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;
	if(wrk_x<0)
		wrk_x=0-wrk_x;
	if(wrk_y<0)
		wrk_y=0-wrk_y;
	if(drctn>=180)
		drctn=drctn-180;
	if(drctn>=90)
		drctn=90-(drctn-90);
	dstc=(wrk_x)/(cos(drctn*a_PI));



	// 目標ユニットへの方位角を求めます
	wrk_x=unit[n].x;
	wrk_y=unit[n].y;
	wrk_x=wrk_x-unit[m].x;
	wrk_y=wrk_y-unit[m].y;
	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;	
	drctn=drctn-unit[m].drctn;
	if(drctn<0)
		drctn=360+drctn;	


	if( dstc<=510 && dstc >= 500  && (drctn<=22.5||drctn>=337.5) /*&& unit[m].no /*&& unit[m].is_ltl_ldr==0*/ /*&& (int)(unit[m].ltl_ldr)*/ )
		{

		// リーダー機か単独機のみここに来ます。

		f=0;
		for(i=1;i<=max_unit;i++)
			{
			if( i!=m && unit[i].used && unit[i].ltl_ldr==m && unit[i].kind==AT1 )
				{
				f++;

				if( unit[i].kind!=FT1 )
					unit[i].ltl_ldr=0; 

				wrk_x=unit[i].x;
				wrk_y=unit[i].y;
				drctn=unit[i].drctn;

				switch( unit[i].no%5 )
					{
					case 1:
						drctn+=315+22.5;
						break;
					case 2:
						drctn+=22.5;
						break;
					case 3:
						drctn+=315;
						break;
					case 4:
						drctn+=45;
						break;
					}
				drctn=(int)drctn%360;

				wrk_x+=cos(drctn*a_PI)*600; 
				wrk_y+=sin(drctn*a_PI)*600;

				unit[i].em_x=wrk_x;
				unit[i].em_y=wrk_y;
				unit[i].em_flg[0]=10+((1+(unit[i].no%5))*20);

				unit[i].pp_x[0]=unit[n].x;
				unit[i].pp_y[0]=unit[n].y;
				unit[i].pp_x[1]=MAP_RIGHT+1;

				}
			}


		}
	else if( dstc >= 400 )
		{
		if( unit[m].pp_x[1]==MAP_RIGHT+1 )
			{
			// ptin dbg
			wrk_r.top=(int)unit[m].pp_y[0]+35;//(int)unit[m].pp_y[0]-35;
			wrk_r.right=(int)unit[m].pp_x[0]+35;
			wrk_r.bottom=(int)unit[m].pp_y[0]-35;//(int)unit[m].pp_y[0]+35;
			wrk_r.left=(int)unit[m].pp_x[0]-35;
			if( !pt_in_rect3(&wrk_r,(int)unit[m].x,(int)unit[m].y) || !unit[n].found)
				{
				return;
				}
			wrk_x=unit[n].x;
			wrk_y=unit[n].y;
			wrk_x+=cos(unit[n].drctn*a_PI); // とりあえずターン後
			wrk_y+=sin(unit[n].drctn*a_PI);
			unit[m].pp_x[0]=wrk_x;
			unit[m].pp_y[0]=wrk_y;			
			unit[m].pp_x[1]=MAP_RIGHT+1;
			unit[m].stop=0;
			}
		}
	else if ( dstc >= 40 && (drctn<=45||drctn>=315))
		{
		if( unit[m].pp_x[1]==MAP_RIGHT+1 )
			{
			wrk_r.top=(int)unit[m].pp_y[0]-35;
			wrk_r.right=(int)unit[m].pp_x[0]+35;
			wrk_r.bottom=(int)unit[m].pp_y[0]+35;
			wrk_r.left=(int)unit[m].pp_x[0]-35;
			if(unit[m].arm[0]==TPD)
				turn=(/*dstc*/ (AIR_TPD_LOS_DSTC) /AIR_TPD_SPD);		// 投雷距離　÷ 魚雷速度　でターンを求めます
			else
				turn=70.0;
			wrk_x=unit[n].x;
			wrk_y=unit[n].y;
			wrk_x+=cos(unit[n].drctn*a_PI)*(unit[n].spd*turn); // とりあえずターン後
			wrk_y+=sin(unit[n].drctn*a_PI)*(unit[n].spd*turn);
			unit[m].pp_x[0]=wrk_x;
			unit[m].pp_y[0]=wrk_y;
			unit[m].pp_x[1]=MAP_RIGHT+1;
			unit[m].stop=0;
			if( unit[m].is_ltl_ldr /*&& unit[m].arm[0]==BOM*/ )
				{
				a=0;
				for(f=1;f<=max_unit;f++)
					{
 					if( unit[f].used && unit[f].kind==FT1 && unit[f].ltl_ldr==m)
						{	a++;	}
					else
						{
						if( unit[f].used && unit[f].ltl_ldr==m )
							unit[f].ltl_ldr=0;
						}
					}
				unit[m].is_ltl_ldr=a;
				}
			}
		}
	else 
		{	// 近すぎる場合は離脱
		// ptin dbg
		wrk_r.top=(int)unit[m].pp_y[0]+35;//(int)unit[m].pp_y[0]-35;
		wrk_r.right=(int)unit[m].pp_x[0]+35;
		wrk_r.bottom=(int)unit[m].pp_y[0]-35;//(int)unit[m].pp_y[0]+35;
		wrk_r.left=(int)unit[m].pp_x[0]-35;
		if( pt_in_rect3(&wrk_r,(int)unit[m].x,(int)unit[m].y) )
			{
			unit[m].pp_x[0]=unit[m].x+cos(unit[m].drctn*a_PI)*(300);
			unit[m].pp_y[0]=unit[m].y+sin(unit[m].drctn*a_PI)*(300);
			unit[m].pp_x[1]=MAP_RIGHT+1;
			unit[m].stop=0;
			}
		}
	}







//============================================================================
// 輸送船の上陸機動をＥｍ＿ｘｙにセット
//----------------------------------------------------------------------------
void	set_pos_of_attack_TR1( int m )
	{
	double			angl,dstc,wrk_x,wrk_y,drctn,drctn2,drctn3,turn;
	int				trgt,pos_of_no,a,b,c;
	int				nums,lvl_jp,lvl_us,jp_tec,us_tec,n,f;
	RECT			wrk_r;



	if( rnd(200)!=0 )
		return;



	// 現位置から攻撃目標地点への距離
	wrk_x=unit[m].info[6]-unit[m].x;
	wrk_y=unit[m].info[7]-unit[m].y;
	if(wrk_x==0)	wrk_x=1;
	if(wrk_y==0)	wrk_y=1;

	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;
	if(wrk_x<0)
		wrk_x=0-wrk_x;
	if(wrk_y<0)
		wrk_y=0-wrk_y;
	if(drctn>=180)
		drctn=drctn-180;
	if(drctn>=90)
		drctn=90-(drctn-90);
	dstc=(wrk_x)/(cos(drctn*a_PI));

	if(dstc<=400)
		{
		unit[m].em_x=unit[m].info[6];
		unit[m].em_y=unit[m].info[7];
		unit[m].em_flg[0]=200;
		}
	}







//============================================================================
// 
//----------------------------------------------------------------------------
void em_of_out_of_map(int m)
	{
	// 展開海域より外れたなら戻る
	if( 1 /*unit[m].used!=cpu_side*/ )
		{
		if( unit[m].x>MAP_RIGHT )
			{
			unit[m].em_x=MAP_RIGHT-40;
			unit[m].em_y=unit[m].y-100+rnd(200);
			unit[m].em_flg[0]=50+rnd(200);
			return;
			}

		if( unit[m].x<MAP_LEFT )
			{
			unit[m].em_x=MAP_LEFT+40;
			unit[m].em_y=unit[m].y-100+rnd(200);
			unit[m].em_flg[0]=50+rnd(200);
			return;
			}

		if( unit[m].y>MAP_TOP )
			{
			unit[m].em_x=unit[m].x-100+rnd(200);
			unit[m].em_y=MAP_TOP-40;
			unit[m].em_flg[0]=50+rnd(200);
			return;
			}

		if( unit[m].y<MAP_BOTTOM )
			{
			unit[m].em_x=unit[m].x-100+rnd(200);
			unit[m].em_y=MAP_BOTTOM+40;
			unit[m].em_flg[0]=50+rnd(200);
			return;
			}
		}
	}



//============================================================================
// 艦船の緊急機動をＥｍ＿ｘｙにセット
//----------------------------------------------------------------------------
void	set_pos_of_emrgncy_SHIP( int m )
	{
	int		n;
	double	em_drctn,wrk_x,wrk_y,drctn,dstc;
	int		size;



//return;


	for(n=1;n<FIRE_MAX;n++)
		{
		// 艦船によってくる魚雷から逃げる
		if( fire[n].used && fire[n].kind==TPD && fire[n].info[0]>=fire[n].info[2] /*&& unit[n].found*/ )
			{
			// 自点と対象点の距離
			wrk_x=fire[n].x-unit[m].x;
			wrk_y=fire[n].y-unit[m].y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			dstc=(wrk_x)/(cos(drctn*a_PI));		// 距離

			if( dstc<=600 && dstc>=40 )
				{
				// 対象ユニットからの自点への方位角
				wrk_x=unit[m].x;
				wrk_y=unit[m].y;
				wrk_x=wrk_x-fire[n].x;
				wrk_y=wrk_y-fire[n].y;
				if(wrk_x==0)	wrk_x=1;
				if(wrk_y==0)	wrk_y=1;
				drctn=atan2(wrk_y,wrk_x)*RAD_to;
				if(drctn<0)
					drctn=360+drctn;	
				drctn=drctn-fire[n].drctn;
				if(drctn<0)
					drctn=360+drctn;
				if( drctn<=20 || drctn>=340 )
					{

					wrk_x=unit[m].x;
					wrk_y=unit[m].y;
					em_drctn=unit[m].drctn;

					switch(rnd(2))
						{
						case 0:
							em_drctn+=60+rnd(30);
							break;
						case 1:
							em_drctn-=60+rnd(30);
							break;
						}

					em_drctn=(int)em_drctn%360;


					wrk_x+=cos(em_drctn*a_PI)*300;
					wrk_y+=sin(em_drctn*a_PI)*300;

					unit[m].em_x=wrk_x;
					unit[m].em_y=wrk_y;
					unit[m].em_flg[0]=100+rnd(150);
					//unit[m].stop=1;

					return;
					}
				}
			}
		}



	for(n=1;n<=max_unit;n++)
		{
		// 艦船によってくる艦船からにげる
		if( (unit[m].stop||(unit[m].kind==CV1||unit[m].kind==CVL1)) /*&& unit[m].ltl_ldr==0*/ &&
		unit[n].used && (unit[n].ctgry==SHIP /*&& unit[n].kind!=AP && unit[n].kind!=SP*/ && !(unit[n].kind>=AP && unit[n].kind<=GF3) ) && 
		unit[n].kind!=SS1 && unit[n].used!=unit[m].used && unit[n].found && unit[n].spry<=0 && unit[m].arm[2]==0)
			{
			// 自点と対象点の距離
			wrk_x=unit[n].x-unit[m].x;
			wrk_y=unit[n].y-unit[m].y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			dstc=(wrk_x)/(cos(drctn*a_PI));		// 距離

			// 見る側
			
			if( dstc<=(double)( BB1_SIGHT ) )
				{
				wrk_x=unit[n].x-unit[m].x;
				wrk_y=unit[n].y-unit[m].y;
				if(wrk_x==0)	wrk_x=1;
				if(wrk_y==0)	wrk_y=1;
				drctn=atan2(wrk_y,wrk_x)*RAD_to;
				if(drctn<0)
					drctn=360+drctn;	

				if( (unit[m].kind==CV1||unit[m].kind==CVL1) && unit[m].ltl_ldr==0 && unit[n].stop && rnd(3)!=0 )
					{
					if(unit[m].rnd_10[0]<=4)
						{
						drctn+=(90+rnd(40));
						}
					else
						{
						drctn-=(90+rnd(40));
						}
					}
				else
					{
					drctn+=160+rnd(40);
					}

				em_drctn=(int)drctn%360;

				wrk_x=unit[m].x;
				wrk_y=unit[m].y;
				wrk_x+=cos(em_drctn*a_PI)*300;
				wrk_y+=sin(em_drctn*a_PI)*300;

				unit[m].em_x=wrk_x;
				unit[m].em_y=wrk_y;
				unit[m].em_flg[0]=100;
//unit[m].em_flg[0]=0;
				}
			}


		// 艦船によってくる攻撃機から逃げる
		if( unit[n].used && (unit[n].kind==AT1 || unit[n].kind==BM1 || ( unit[n].kind==FT1 && unit[m].kind==TR1 ) ) && unit[n].info[0]==FLYING && unit[n].used!=unit[m].used && unit[n].found 
			)
			{
			// 自点と対象点の距離
			wrk_x=unit[n].x-unit[m].x;
			wrk_y=unit[n].y-unit[m].y;
			if(wrk_x==0)	wrk_x=1;
			if(wrk_y==0)	wrk_y=1;

			drctn=atan2(wrk_y,wrk_x)*RAD_to;
			if(drctn<0)
				drctn=360+drctn;
			if(wrk_x<0)
				wrk_x=0-wrk_x;
			if(wrk_y<0)
				wrk_y=0-wrk_y;
			if(drctn>=180)
				drctn=drctn-180;
			if(drctn>=90)
				drctn=90-(drctn-90);
			dstc=(wrk_x)/(cos(drctn*a_PI));		// 距離

			if( dstc<=600-((unit[n].kind==BM1)*300) && dstc>=40 )
				{
				// 対象ユニットからの自点への方位角
				wrk_x=unit[m].x;
				wrk_y=unit[m].y;
				wrk_x=wrk_x-unit[n].x;
				wrk_y=wrk_y-unit[n].y;
				if(wrk_x==0)	wrk_x=1;
				if(wrk_y==0)	wrk_y=1;
				drctn=atan2(wrk_y,wrk_x)*RAD_to;
				if(drctn<0)
					drctn=360+drctn;	
				drctn=drctn-unit[n].drctn;
				if(drctn<0)
					drctn=360+drctn;
				if( drctn<=45 || drctn>=315 )
					{

					wrk_x=unit[m].x;
					wrk_y=unit[m].y;
					em_drctn=unit[m].drctn;
					switch(rnd(2))
						{
						case 0:
							em_drctn+=45+rnd(45+20);
							break;
						case 1:
							em_drctn-=45+rnd(45+20);
							break;
						}
					em_drctn=(int)em_drctn%360;


					wrk_x+=cos(em_drctn*a_PI)*300;
					wrk_y+=sin(em_drctn*a_PI)*300;

					unit[m].em_x=wrk_x;
					unit[m].em_y=wrk_y;
					unit[m].em_flg[0]=200+rnd(250);
					//unit[m].stop=1;

					return;
					}
				}
			}
		}



	if( unit[m].kind==DD1 /*&&  unit[m].spd<=unit[m].max_spd*0.9*/ && unit[m].stop==1  /*&& unit[m].used==cpu_side*/ )
		{
		// 駆逐艦の対潜水艦行動、発見された後！
		if(unit[m].em_flg[0]==0)
			{
			for(n=1; n<=max_unit; n++)
				{
				if( unit[n].used && unit[n].kind==SS1 && unit[n].found && unit[n].used!=unit[m].used )
					{
					// 自点と対象点の距離
					if( unit[n].info[6] )
						{
						// 潜水中
						wrk_x=unit[n].info[7]-unit[m].x;
						wrk_y=unit[n].info[8]-unit[m].y;
						}
					else
						{
						// 浮上してます
						wrk_x=unit[n].x-unit[m].x;
						wrk_y=unit[n].y-unit[m].y;
						}

					if(wrk_x==0)	wrk_x=1;
					if(wrk_y==0)	wrk_y=1;

					drctn=atan2(wrk_y,wrk_x)*RAD_to;
					if(drctn<0)
						drctn=360+drctn;
					if(wrk_x<0)
						wrk_x=0-wrk_x;
					if(wrk_y<0)
						wrk_y=0-wrk_y;
					if(drctn>=180)
						drctn=drctn-180;
					if(drctn>=90)
						drctn=90-(drctn-90);
					dstc=(wrk_x)/(cos(drctn*a_PI));		// 距離
					
					if( dstc<=( unit[m].type==1 ? 500 : 400 ) )
						{
						// 近くに潜水艦推定位置

						wrk_x=unit[n].info[7]-unit[m].x;
						wrk_y=unit[n].info[8]-unit[m].y;
						if(wrk_x==0)	wrk_x=1;
						if(wrk_y==0)	wrk_y=1;
						drctn=atan2(wrk_y,wrk_x)*RAD_to;

						drctn+=20-rnd(40);
/*
						if(drctn<0)
							drctn=360+drctn;	
*/

						em_drctn=(int)drctn%360;

						wrk_x=unit[m].x;
						wrk_y=unit[m].y;
						wrk_x+=cos(em_drctn*a_PI)*((dstc)+200);
						wrk_y+=sin(em_drctn*a_PI)*((dstc)+200);


						unit[m].em_x=wrk_x;
						unit[m].em_y=wrk_y;
						unit[m].em_flg[0]=100+rnd(250);
	
//						unit[m].em_flg[1]=n;
						return;
						}
					}
				}
			}

#if 0
		else
			{
			if( unit[unit[m].em_flg[1]].found==0  )
				{
				if( rnd(10)==0 )
					{
					unit[m].em_flg[1]=0;
					}
				else
					{
					unit[m].em_x=unit[unit[m].em_flg[1]].info[7]+(400-rnd(800));
					unit[m].em_y=unit[unit[m].em_flg[1]].info[8]+(400-rnd(800));
					unit[m].em_flg[0]=200+rnd(100);
					}
				return;
				}
			else
				{
				unit[m].em_flg[1]=0;	
				}
			}
#endif


		}


	em_of_out_of_map(m);

	}


























