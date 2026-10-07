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


//#include "all_head.h"
//#include "all_extern.h"
//#include	"all_forward.h"

// Port of etc3.cpp. So far, make_my_rnd through cloud_cont are ported.

namespace OpenNspw;

public partial class Nspw
{









//============================================================================
//	マイ乱数を作ります
//----------------------------------------------------------------------------
public void	make_my_rnd()
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
public int		my_rnd(int	r)
	{
	my_rnd_pt++;
	my_rnd_pt%=4096;

	return(my_rnd_sheet[my_rnd_pt]%r);
	}



//============================================================================
//	
//----------------------------------------------------------------------------

public int		rnd( int x)
	{


	rnd_count++;

	return(rand()%(x));
	}



//============================================================================
// スプライト基礎データ
//----------------------------------------------------------------------------
public void		set_sprt_data()
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
public void	cloud_in_start()
	{
	int		m,n; Array16<int> ok=default;	
	double	base_x,base_y,sub_x;



	while(true)
		{
		// 空いてるくもスプライトを探します。
		n=0;
		for(m=0; m<KUMO_MAX; m++)
			{
			if( kumo[m].used==0 )
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
public void	cloud_cont()
	{
	int		m,n; Array16<int> ok=default;	
	double	base_y,sub_x;



	
	for(n=0;n<KUMO_MAX;n++)
		{
		if( kumo[n].used!=0 )
			{
			if( game_end==0 )
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
		if( kumo[m].used==0 )
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
}
