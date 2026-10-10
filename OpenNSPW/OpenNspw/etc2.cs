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

// Port of etc2.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{






//============================================================================
// 新ユニット登場
//----------------------------------------------------------------------------
public void new_unit_arrived(int side,int new_unit_kind)
	{
	double		rx=default /* C4701 */,ry=default /* C4701 */;
	double		rx2=default /* C4701 */,ry2=default /* C4701 */;
	UnitKind	kind=default /* C4701 */; int kind2,m,i;
	Side		arrived_side;





	if( side==1 )
		{
		arrived_side=your_side;
		}
	else
		{
		if(your_side==Side.Japan)
			arrived_side=Side.UnitedStates;
		else
			arrived_side=Side.Japan;
		}



	kind2=0;

	new_unit_kind--;

	switch(new_unit_kind)
		{
		case 0:		kind=UnitKind.Battleship;		break;
		case 1:		kind=UnitKind.Cruiser;		break;
		case 2:		kind=UnitKind.Destroyer;		break;
		case 3:		kind=UnitKind.Submarine;		break;
		case 4:		kind=UnitKind.LightCarrier;		break;
		case 5:		kind=UnitKind.Carrier;		break;
		case 6:		kind=UnitKind.Fighter;		break;
		case 7:		kind=UnitKind.Attacker;		break;
		case 8:		kind=UnitKind.Bomber;		break;
		case 9:		kind=UnitKind.Fighter;	kind2=1;			break;
		case 10:	kind=UnitKind.Transport;	kind2=TR_GF1;		break;
		case 11:	kind=UnitKind.Transport;	kind2=TR_GF2;		break;
		case 12:	kind=UnitKind.Transport;	kind2=TR_GF3;		break;
		case 13:	kind=UnitKind.Transport;	kind2=TR_AP;		break;
		case 14:	kind=UnitKind.Transport;	kind2=TR_SP;			break;

		case 15:		kind=UnitKind.Cruiser;	kind2=1;		break;
		case 16:		kind=UnitKind.Destroyer;	kind2=1;		break;
		case 17:		
			if( arrived_side==Side.Japan ) 
				kind=UnitKind.Battleship;	
			else
				kind=UnitKind.Carrier;	
			kind2=1;		
			break;

		}



	if(arrived_side==Side.Japan)
		{
		switch(map_now)
			{
			case 0:		// 南太平洋
				rx=MAP_LEFT-10;
				ry=MAP_TOP-rnd(1200);
				rx2=rx+500;
				ry2=ry-200;
rx-=new_unit_kind*80 ;
				break;

			case 1:		// 中部太平洋
				rx=MAP_LEFT-10;
				ry=MAP_BOTTOM+rnd(1200);
				rx2=rx+500;
				ry2=ry+200;
rx-=new_unit_kind*80;
				break;

			case 2:		// 日本近海
				rx=MAP_LEFT+rnd(1200);
				ry=MAP_TOP+10;
				rx2=rx+200;
				ry2=ry-500;
ry+=new_unit_kind*80;
				break;

			case 3:		// ユーザーマップ
				switch(rein[(int)arrived_side])
					{
					case 0:
						rx=MAP_LEFT-500;
						ry=MAP_TOP+500;
						rx2=rx+1200;
						ry2=ry-1200-rnd(500);
						ry-=new_unit_kind*80;
						break;
					case 1:
						rx=MAP_RIGHT+500;
						ry=MAP_TOP+500;
						rx2=rx-1200;
						ry2=ry-1200-rnd(500);
						ry-=new_unit_kind*80;
						break;
					case 2:
						rx=MAP_RIGHT+500;
						ry=MAP_BOTTOM-500;
						rx2=rx-1200;
						ry2=ry+1200+rnd(500);
						ry+=new_unit_kind*80;
						break;
					case 3:
						rx=MAP_LEFT-500;
						ry=MAP_BOTTOM-500;
						rx2=rx+1200;
						ry2=ry+1200+rnd(500);
						ry+=new_unit_kind*80;
						break;
					}
				break;

			}
		}
	else
		{
		switch(map_now)
			{
			case 0:		// 南太平洋
				rx=MAP_RIGHT-rnd(1200)-100;
				ry=MAP_BOTTOM-10;
//rx=0;
//ry=-1200;
				rx2=rx-200;
				ry2=ry+500;
ry-=new_unit_kind*80;
				break;

			case 1:		// 中部太平洋
				rx=MAP_RIGHT+10;
				ry=rnd(1200);
				rx2=rx-500;
				ry2=ry;
rx+=new_unit_kind*80;
				break;

			case 2:		// 日本近海
				rx=MAP_RIGHT-rnd(1200)-100;
				ry=MAP_BOTTOM-10;
				rx2=rx-200;
				ry2=ry+500;
ry-=new_unit_kind*80;
				break;

			case 3:		// ユーザーマップ
				switch(rein[(int)arrived_side])
					{
					case 0:
						rx=MAP_LEFT-500;
						ry=MAP_TOP+500;
						rx2=rx+1200;
						ry2=ry-1200-rnd(500);
						ry-=new_unit_kind*80;
						break;
					case 1:
						rx=MAP_RIGHT+500;
						ry=MAP_TOP+500;
						rx2=rx-1200;
						ry2=ry-1200-rnd(500);
						ry-=new_unit_kind*80;
						break;
					case 2:
						rx=MAP_RIGHT+500;
						ry=MAP_BOTTOM-500;
						rx2=rx-1200;
						ry2=ry+1200+rnd(500);
						ry+=new_unit_kind*80;
						break;
					case 3:
						rx=MAP_LEFT-500;
						ry=MAP_BOTTOM-500;
						rx2=rx+1200;
						ry2=ry+1200+rnd(500);
						ry+=new_unit_kind*80;
						break;
					}
				break;
			}
		}


			
	if( new_unit_kind<=5 || new_unit_kind>=10 )
		{
		// 艦船の登場
//		if(arrived_side==JPN )
//			m=set_new_unit(JPN,kind,rx,ry,(double)90);
//		else
//			m=set_new_unit(USA,kind,rx,ry,(double)90);

		if( arrived_side==Side.Japan )
			m=set_new_unit_2(Side.Japan,kind,kind2,rx,ry,(double)90);
		else
			m=set_new_unit_2(Side.UnitedStates,kind,kind2,rx,ry,(double)90);


		if(kind==UnitKind.Transport)
			{
			// 輸送船の場合、積荷
			unit[m].Weapon=kind2;		// 武装品種
			unit[m].Ammo=1;			// 数
			unit[m].MaxAmmo=1;			// 数 全容量
			}
		else
			{
			unit[m].Ammo=unit[m].MaxAmmo/4;			// 弾薬搭載量
			unit[m].Fuel=50;
			}
		}
	else
		{
		// 航空機の登場

		for(i=0;i<((kind==UnitKind.Attacker ? 1 : 0)*3)+((kind==UnitKind.Fighter ? 1 : 0)*2)+((kind==UnitKind.Bomber ? 1 : 0)*1)  ;i++)
			{
			if( arrived_side==Side.Japan )
				m=set_new_unit_2(Side.Japan,kind,kind2,rx,ry,(double)90);
			else
				m=set_new_unit_2(Side.UnitedStates,kind,kind2,rx,ry,(double)90);

			unit[m].pp_x[0]=rx2;
			unit[m].pp_y[0]=ry2;
			unit[m].pp_x[1]=MAP_RIGHT+1;			
			
			unit[m].PlaneState=UnitState.Flying;
			unit[m].spd=1.5;

			unit[m].info[5]=MOVE;				// モード（コンバットメニュー）

			unit[m].Fuel=50;
			if(kind==UnitKind.Fighter)
				unit[m].Ammo=unit[m].Ammo/3;
			else
				unit[m].Ammo=0;
			//unit[m].arm[1]=unit[m].arm[4]/4;			// 弾薬搭載量

			
			rx+=-160+rnd(320);
			ry+=-160+rnd(320);
			rx2+=-100+rnd(200);
			ry2+=-100+rnd(200);
			}
		}

	}




//============================================================================
// 可視不可視のサイズ設定
//----------------------------------------------------------------------------
public int		find_out_size( int m , int n)
	{
	int		size=0 /* C4701 */;



	// 見る側の追加
	switch( unit[m].kind )
		{
		case UnitKind.Battleship:		size=BB1_SIGHT;		break;
		case UnitKind.Cruiser:		size=CA1_SIGHT;		break;
		case UnitKind.Destroyer:		size=DD1_SIGHT;		break;
		case UnitKind.Submarine:		size=SS1_SIGHT;		break;
		case UnitKind.Carrier:		size=CV1_SIGHT;		break;
		case UnitKind.LightCarrier:		size=CVL1_SIGHT;	break;
		case UnitKind.Transport:		size=TR1_SIGHT;		break;
		case UnitKind.Fighter:		size=FT1_SIGHT;		break;
		case UnitKind.Attacker:		size=AT1_SIGHT;		break;
		case UnitKind.Bomber:		size=BM1_SIGHT;		break;
		case UnitKind.AirBase: case UnitKind.NavalBase:		size=AP_SIGHT;		break;		
		case UnitKind.City:		size=CT1_SIGHT;		break;
		case UnitKind.Mine:		size=MN1_SIGHT;		break;
		case UnitKind.InfantryBase:		size=GF1_SIGHT;		break;
		case UnitKind.Pillboxes:		size=GF2_SIGHT;		break;
		case UnitKind.Fortress:		size=GF3_SIGHT;		break;
		}


	if( unit[m].kind>=UnitKind.AirBase && unit[m].kind<=UnitKind.Fortress && unit[m].info[0]!=0 )
		{
		// 工事中は視界を制限
		size=FT1_SIGHT/2;
		}




	// 見られる側の追加
	switch( unit[n].kind )
		{
		case UnitKind.Battleship:							break;
		case UnitKind.Cruiser:		/*size=size*0.9;*/		break;
		case UnitKind.Destroyer:		/*size=size*0.8;*/		break;
		case UnitKind.Submarine:
			if( unit[n].info[6]!=0 )
				{	// 潜航中
				if( unit[m].kind!=UnitKind.Destroyer )
					{
					if(unit[m].kind==UnitKind.NavalBase && unit[m].info[0]==0 )
						size=(int)(SP_SIGHT*0.8);
					else
						size=0;
					}
				else
					{	// 駆逐艦
					if( unit[m].spd<=unit[m].max_spd/3 )
						{
						size=( unit[m].type==1 ? 380 : 280);
						}
					else
						{
						size=( unit[m].type==1 ? 270 : 140 );
						}
					if( unit[n].spd==0 )
						size+=50;		// 潜水艦の速度によって
					}
				}
			else if( unit[n].spry==0 )
				{	// 浮上航行
				size=(int)(size*0.5);
				if( size>300 )
					size=300;		// 潜水艦の見える範囲より小さくする
				}
				break;
		case UnitKind.Carrier:				break;
		case UnitKind.LightCarrier:				break;
		case UnitKind.Fighter: case UnitKind.Attacker:	case UnitKind.Bomber:
			// 航空機は固まっていると見つかりやすい
			break;
		case UnitKind.AirBase: case UnitKind.NavalBase:
			break;		
		case UnitKind.Transport:
			break;
		}
	

	return (size);
	}






//============================================================================
//絶対スクリーン座標に線を描画する。色付き
//----------------------------------------------------------------------------
public void	draw_line4(int x1,int y1,int x2,int y2,int right,int bottom,uint rgb)
	{

	draw_line5( x1, y1, x2, y2, right, bottom , 0xFFFF);

/***
	int		dstX, dstY,addX,addY;
	int		ctr,x,y,i;
	int		cl1, cl2;
    DDCOLORKEY          ddck;



	ddck.dwColorSpaceLowValue  = 0xff;


	//書き込むＶＲＡＭのアドレスを得る（ロックして書き込めるようにする）
	memset(&dst_ddsd, 0, sizeof(DDSURFACEDESC));
	dst_ddsd.dwSize = sizeof(DDSURFACEDESC);
	IDirectDrawSurface_Lock( lpDDSBack, NULL, (LPDDSURFACEDESC)&dst_ddsd, DDLOCK_WAIT, NULL );
	dst_vram=dst_ddsd.lpSurface;




	dstX=x2-x1;	dstY=y2-y1;

	if(dstX<0)	{addX=-1;	dstX*=-1;}	else	addX=1;
	if(dstY<0)	{addY=-1;	dstY*=-1;}	else	addY=1;

	ctr=0;
	x=x1;	y=y1;
	if(dstX>=dstY)
		{
		for(i=0;i<dstX;i++)
			{
			if( x>=0 && y>=0 && x<=right && y<=bottom )
				{
				dst_vram[y*dst_ddsd.lPitch+(x)]=ddck.dwColorSpaceLowValue;
				}

			x+=addX;
			ctr+=dstY;
			if(ctr>=dstX)
				{
				y+=addY;
				ctr-=dstX;
				}
			}
		}
	else
		{
		for(i=0;i<dstY;i++)
			{
			if( x>=0 && y>=0 && x<=right && y<=bottom )
				{
				dst_vram[y*dst_ddsd.lPitch+(x)]=ddck.dwColorSpaceLowValue;
				}

			y+=addY;
			ctr+=dstX;
			if(ctr>=dstY)
				{
				x+=addX;
				ctr-=dstY;
				}			
			}
		}
	IDirectDrawSurface_Unlock( lpDDSBack, NULL );
***/
	}






//============================================================================
//絶対スクリーン座標に線を描画する。色付き
//----------------------------------------------------------------------------
public void	draw_line5(int x1,int y1,int x2,int y2,int right,int bottom,ushort cl)
	{
	int		dstX, dstY,addX,addY;
	int		ctr,x,y,i;
	int		cl1, cl2;
	DDCOLORKEY          ddck;
	ushort* dst_vram;			//書き込むＶＲＡＭのアドレス

	DDSURFACEDESC2		dst_ddsd;

cl=0xFFFF;


	//書き込むＶＲＡＭのアドレスを得る（ロックして書き込めるようにする）
	memset(&dst_ddsd, 0, (nuint)(sizeof(DDSURFACEDESC2)));
	dst_ddsd.dwSize = (uint)(sizeof(DDSURFACEDESC2));
	IDirectDrawSurface_Lock( lpDDSBack, null, &dst_ddsd, DDLOCK_WAIT, null );

	dst_vram=(ushort*)dst_ddsd.lpSurface;



	dstX=x2-x1;	dstY=y2-y1;

	if(dstX<0)	{addX=-1;	dstX*=-1;}	else	addX=1;
	if(dstY<0)	{addY=-1;	dstY*=-1;}	else	addY=1;

	ctr=0;
	x=x1;	y=y1;
	if(dstX>=dstY)
		{
		for(i=0;i<dstX;i++)
			{
			if( x>=0 && y>=0 && x<=right && y<=bottom )
				{
		//		dst_vram[y*dst_ddsd.lPitch+(x)]=ddck.dwColorSpaceLowValue;
				dst_vram[y*(dst_ddsd.lPitch/2)+(x)]=cl;
				}

			x+=addX;
			ctr+=dstY;
			if(ctr>=dstX)
				{
				y+=addY;
				ctr-=dstX;
				}
			}
		}
	else
		{
		for(i=0;i<dstY;i++)
			{
			if( x>=0 && y>=0 && x<=right && y<=bottom )
				{
//				dst_vram[y*dst_ddsd.lPitch+(x)]=ddck.dwColorSpaceLowValue;
				dst_vram[y*(dst_ddsd.lPitch/2)+(x)]=cl;
				}

			y+=addY;
			ctr+=dstX;
			if(ctr>=dstY)
				{
				x+=addX;
				ctr-=dstY;
				}			
			}
		}

	IDirectDrawSurface_Unlock( lpDDSBack, null );

	}
//============================================================================
// 
//----------------------------------------------------------------------------
public int	pt_in_rect(ref RECT dstn_rect,int crsr_x,int crsr_y)
	{
	if( dstn_rect.top <= crsr_y && dstn_rect.bottom >= crsr_y &&
		dstn_rect.right >= crsr_x && dstn_rect.left <= crsr_x)
		return 1;
	return 0;
	}


//============================================================================
// 
//----------------------------------------------------------------------------
public int	pt_in_rect2(ref RECT dstn_rect,int crsr_x,int crsr_y)
	{
	if( dstn_rect.top >= crsr_y && dstn_rect.bottom <= crsr_y &&
		dstn_rect.right >= crsr_x && dstn_rect.left <= crsr_x)
		return (1);
	return (0);
	}



//============================================================================
// 
//----------------------------------------------------------------------------
public int	pt_in_rect3(ref RECT dstn_rect,int crsr_x,int crsr_y)
	{
	if( dstn_rect.top >= crsr_y && dstn_rect.bottom <= crsr_y &&
		dstn_rect.right >= crsr_x && dstn_rect.left <= crsr_x)
		{
		return (1);
		}
	return (0);
	}






//					セイムレクト
//
//==========================================================================================
//
public int	same_rect(ref RECT dstn_rect, ref RECT src_rect, ref RECT field_rect)
	{
	// field_rectと一致するdstn_rectを見つけ出し。その差分をsrc_rectに反映させます。




	if( dstn_rect.top <= field_rect.bottom )
		{
		if(dstn_rect.top < field_rect.top )
			{
			src_rect.top+=(field_rect.top-dstn_rect.top);
			dstn_rect.top=field_rect.top;
			}
		}
	else
		{
		return (0);
		}


	if( dstn_rect.bottom >= field_rect.top )
		{
		if(dstn_rect.bottom > field_rect.bottom )
			{
			src_rect.bottom-=(dstn_rect.bottom-field_rect.bottom);
			dstn_rect.bottom=field_rect.bottom;
			}
		}
	else
		{
		return (0);
		}


	if( src_rect.top==src_rect.bottom )
		return (0);



	if( dstn_rect.left <= field_rect.right )
		{
		if(dstn_rect.left < field_rect.left )
			{
			src_rect.left+=(field_rect.left-dstn_rect.left);
			dstn_rect.left=field_rect.left;
			}
		}
	else
		{
		return (0);
		}


	if( dstn_rect.right >= field_rect.left )
		{
		if(dstn_rect.right > field_rect.right )
			{
			src_rect.right-=(dstn_rect.right-field_rect.right);
			dstn_rect.right=field_rect.right;
			}
		}
	else
		{
		return (0);
		}

	if( src_rect.left==src_rect.right )
		return (0);


	return (1);
	}







//============================================================================
// その空母の現在の開きスペースを代えします。
//----------------------------------------------------------------------------
public int		seek_parking_no( int m )
	{
	int		i,n,rtn=0 /* C4701 */; Array32<int> wrk=default;



	
	for(i=0;i<=31;i++)
		wrk[i]=0;	

	for( i=1; i<=max_unit; i++)
		{
		if( unit[i].used!=0 && unit[i].ctgry==UnitCategory.Plane && unit[i].PlaneState==UnitState.Parked 
			&& unit[m].info[1]==unit[i].info[1] )
			wrk[unit[i].info[2]]++;
		}

	for(i=0;i<=31;i++)
		if(wrk[i]==0)
			{
			rtn=i;
			break;
			}


	return	(rtn);
	}



//============================================================================
// 現在収容数(着艦予定、飛行甲板上数も含む)
//----------------------------------------------------------------------------
public int		plane_in_cv( int m )
	{
	int		i,rtn;

	rtn=0;
	for(i=1;i<=max_unit;i++)
		{
		if( unit[i].used!=0 && unit[i].ctgry==UnitCategory.Plane && unit[i].PlaneState==UnitState.Parked && m==unit[i].info[1] )
			rtn++;
		}
	return (rtn);
	}



//============================================================================
// 駐機場、格納庫の停止位置を求めます。
//----------------------------------------------------------------------------
public void	set_pos_of_parking( int	m )
	{
	int		max,i,w;
	int		parking_x,parking_y;



	// ２列格納
	if( unit[unit[m].info[1]].kind==UnitKind.AirBase )
		{	// 陸上基地
		parking_y=sprt[UNIT_INFO_JPN].y+sprt[UNIT_INFO_JPN].ht-((unit[m].info[2]/2)*40)-100;
		if( false && unit[m].kind==UnitKind.Bomber )
			w=55;
		else
			w=35;
		if( unit[m].info[2]%2!=0 )
			{ // 右側
			parking_x=sprt[UNIT_INFO_JPN].x+sprt[UNIT_INFO_JPN].wd/2-w;
			unit[m].drctn=270+45;
			parking_y-=18;
			}
		else
			{ // 左側
			parking_x=sprt[UNIT_INFO_JPN].x+sprt[UNIT_INFO_JPN].wd/2+w;
			unit[m].drctn=180+45;
			}
		}
	else
		{	// 航空母艦
		parking_y=sprt[UNIT_INFO_JPN].y+sprt[UNIT_INFO_JPN].ht-((unit[m].info[2]/2)*40)-100;
		if( unit[m].info[2]%2!=0 )
			{ // 右側
			parking_x=sprt[UNIT_INFO_JPN].x+sprt[UNIT_INFO_JPN].wd/2-25;
			unit[m].drctn=270+45;
			parking_y-=18;
			}
		else
			{ // 左側
			parking_x=sprt[UNIT_INFO_JPN].x+sprt[UNIT_INFO_JPN].wd/2+25;
			unit[m].drctn=180+45;
			}
		}



	unit[m].Position = new WorldPosition((double)parking_x, (double)parking_y);
	}









//============================================================================
// 
//----------------------------------------------------------------------------
public void	set_the_slct_unit (int m)
	{
	int	n;


	if( unit[m].used!=your_side )
		return;


//	new_pp[1].cls=1;

	bf_new_pp[1].cls=1;

	new_pp[1].cls=1;




	the_slct_unit=(short)m; slct_unit_no=0; slct_unit[1][m]=1; cmbt_menu_kind=1; wrk_pp_x[0]=MAP_RIGHT+1; 
	//cmbt_menu_slctd=1; 
	cmbt_menu_slctd = (short)unit[m].info[5];


	//your_side=unit[m].used;


	// そのユニットの随伴機を枠付けします。
	for( n=0; n<=max_unit; n++)
		{
		if( unit[n].used!=0 && unit[n].ltl_ldr==m )
			{
			slct_unit_no++; slct_unit[1][n]=unit[n].no; 
			}
		}
	// そのユニットをユニットインフォにセットします。
	if( !(unit[m].ctgry==UnitCategory.Plane && unit[m].PlaneState==UnitState.Parked) )
		{
		unit_info[0]=(int)unit[m].kind;
		unit_info[1]=0;				// 空母なら１で格納庫 ０ で飛行甲板
		unit_info[3]=m;				// そのユニットの番号
		unit_info[4]=(int)unit[m].used;	// そのユニットの国籍
		//unit_info[2]=0;				// 駐機機の発進までのフラグ
		switch((UnitKind)unit_info[0])
			{
			case UnitKind.Battleship:
				unit_info[2]=0;
				break;
			case UnitKind.Cruiser:
				unit_info[2]=1;
				break;
			case UnitKind.Destroyer:
				unit_info[2]=2;
				break;
			case UnitKind.Submarine:
				unit_info[2]=3;
				break;
			case UnitKind.Carrier:
				unit_info[2]=4;
				break;
			case UnitKind.LightCarrier:
				unit_info[2]=5;
				break;
			case UnitKind.Transport:
				unit_info[2]=6;
				break;
			case UnitKind.AirBase:
				unit_info[2]=8;
				break;
			default:
				unit_info[2]=7;
				break;
			}
		}
	}






//============================================================================
//
//----------------------------------------------------------------------------
public void	cls_all_slct_unit()
	{
	int		m;
	for(m=0; m<=255; m++)
		{
		slct_unit[0][m]=0;
		slct_unit[1][m]=0;
		}
	}



//============================================================================
//
//----------------------------------------------------------------------------
public void	cls_all_slct_unit_p2( int side )
	{
	int		m;


	if( side==1 )
		{	
		// マイサイドの消去
		for(m=0; m<=255; m++)
			{
			slct_unit[1][m]=0;
			}
		}
	else
		{
		// 敵サイドの消去
		for(m=0; m<=255; m++)
			{
			slct_unit[0][m]=0;
			}
		}

	}







//============================================================================
// Effect構造体の空いてる最低番号を代えします。
//----------------------------------------------------------------------------
public int		seek_effect_no()
	{
	int		rtn,s;




	rtn=0;
	for(s=1;s<EFFECT_MAX/*255*/;s++)
		{
		if( effect[s].layer==EffectLayer.None )
			{
			rtn=s;
			break;
			}
		}


	return (rtn);
	}







//============================================================================
// Ｆｉｒｅ構造体の空いてる最低番号を代えします。また、必要ならＭａｘ＿ｆｉｒｅも変更
//----------------------------------------------------------------------------
public int		seek_fire_no()
	{
	int		rtn,s;

	rtn=0;
	for(s=1;s<FIRE_MAX/*255*/;s++)
		{
		if( fire[s].used==0 )
			{
			rtn=s;
			break;
			}
		}



	return (rtn);
	}








#if true
//============================================================================
// 
//----------------------------------------------------------------------------
public int	rtn_damage_pt(int	m)
	{
	int		rtn=0 /* C4701 */;



	//	基礎的な破壊力をセット
	switch( fire[m].kind )
		{
		case BLT:		rtn=BLT_DMG+rnd(BLT_DMG);		break;
		case GUN:		rtn=GUN_DMG+rnd(GUN_DMG);		break;
			break;
		case SHL:		rtn=SHL_DMG+rnd(SHL_DMG);		break;
		case TPD:		rtn=TPD_DMG+rnd(TPD_DMG);		
			break;
		case BOM:		rtn=BOM_DMG+rnd(BOM_DMG);		break;
		case ASB:		rtn=1+rnd(ASB_DMG+2+1)/*ASB_DMG+rnd(ASB_DMG)*/;		break;
		case RAS:		rtn=RAS_DMG+rnd(RAS_DMG);		break;
		}






	// 少し乱数させる
//	rtn+=rnd(rtn);

	// サイドによって各破壊力を操作



	return(rtn);
	}






//============================================================================
// 
//----------------------------------------------------------------------------
public int		drctn_for_8(int drctn)
	{

	drctn=drctn%360;

	switch( (int)(drctn/22.5) )
		{
		case 0: case 15:
			drctn=2;
			break;
		case 1: case 2:
			drctn=1;
			break;
		case 3: case 4:
			drctn=0;
			break;
		case 5: case 6:
			drctn=7;
			break;
		case 7: case 8:
			drctn=6;
			break;
		case 9: case 10:
			drctn=5;
			break;
		case 11: case 12:
			drctn=4;
			break;
		case 13: case 14:
			drctn=3;
			break;
		default:
			drctn=2;
			break;
		}

/**
	if( drctn<=90-22.5 && drctn>=90+22.5 )
		{
		drctn=0;
		}
	else if ( drctn<=45-22.5 && drctn>=45+22.5 )
		{
		drctn=1;
		}
	else if ( drctn<=360-22.5 && drctn>=0+22.5 )
		{
		drctn=2;
		}
	else if ( drctn<=315-22.5 && drctn>=315+22.5 )
		{
		drctn=3;
		}
	else if ( drctn<=315-22.5 && drctn>=315+22.5 )
		{
		drctn=4;
		}
***/



	return ( drctn );
	}






//============================================================================
// 艦隊のフォーメーションの初期値を設定します
//----------------------------------------------------------------------------
public void	set_frmtn_of_ships( int s )
	{
	double		wrk_x,wrk_y,drctn,dstc,wrk_drctn=0 /* C4701 */;



	// リーダー艦からの方位角を求めます。
	wrk_x=unit[s].Position.X-unit[unit[s].ltl_ldr].Position.X;
	wrk_y=unit[s].Position.Y-unit[unit[s].ltl_ldr].Position.Y;
	if(wrk_x==0)	wrk_x=1;
	if(wrk_y==0)	wrk_y=1;

	drctn=atan2(wrk_y,wrk_x)*RAD_to;
	if(drctn<0)
		drctn=360+drctn;
	switch((int)(unit[unit[s].ltl_ldr].drctn/22.5))
		{
		case 0: case 15:
			wrk_drctn=0.0;
			break;
		case 1: case 2:
			wrk_drctn=45.0;
			break;
		case 3: case 4:
			wrk_drctn=90.0;
			break;
		case 5: case 6:
			wrk_drctn=90.0+45.0;
			break;
		case 7: case 8:
			wrk_drctn=180.0;
			break;
		case 9: case 10:
			wrk_drctn=180.0+45.0;
			break;
		case 11: case 12:
			wrk_drctn=270.0;
			break;
		case 13: case 14:
			wrk_drctn=270.0+45.0;
			break;
		}
	drctn=drctn-wrk_drctn;
	if(drctn<0)
		drctn=360+drctn;

	unit[s].to_ldr_drctn=drctn;

	// リーダー艦からの距離を求めます
	wrk_x=unit[s].Position.X-unit[unit[s].ltl_ldr].Position.X;
	wrk_y=unit[s].Position.Y-unit[unit[s].ltl_ldr].Position.Y;
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
	unit[s].to_ldr_dstc=(wrk_x)/(cos(drctn*a_PI));

	}





//============================================================================
// 着艦パターンをＰｐ＿ｘｙにセットします。
//----------------------------------------------------------------------------
public void	set_pos_of_take_down(int n)
	{
	double			angl=0 /* C4701 */,angl2,dstc;
	int				pt,pos_of_no,a,b,c;



	pt=unit[n].info[1];
	//pt=4;
	//pos_of_no=unit[n].no;

	if( unit[pt].used==0 )
		{
//		unit[n].pp_x[0]=(double)(unit[n].info[7]+(rnd(200)-100));
//		unit[n].pp_y[0]=(double)(unit[n].info[8]+(rnd(200)-100));
		unit[n].pp_x[0]=unit[n].Position.X+(double)(rnd(400)-200);
		unit[n].pp_y[0]=unit[n].Position.Y+(double)(rnd(400)-200);

		unit[n].pp_x[1]=MAP_RIGHT+1;
//		unit[n].pp_now=0;
		unit[n].stop=0;

		return;
		}


	switch((int)(unit[pt].drctn/22.5))
		{
		case 0: case 15:
			angl=0.0;
			break;
		case 1: case 2:
			angl=45.0;
			break;
		case 3: case 4:
			angl=90.0;
			break;
		case 5: case 6:
			angl=90.0+45.0;
			break;
		case 7: case 8:
			angl=180.0;
			break;
		case 9: case 10:
			angl=180.0+45.0;
			break;
		case 11: case 12:
			angl=270.0;
			break;
		case 13: case 14:
			angl=270.0+45.0;
			break;
		}

	angl2=angl;
	angl2-=180.0;	dstc=200;

	if( angl<0 )
		angl=360+angl;



	if( unit[n].kind==UnitKind.Bomber )
		{
		unit[n].pp_x[0]=unit[pt].Position.X+cos(angl2*a_PI)*560.0;
		unit[n].pp_y[0]=unit[pt].Position.Y+sin(angl2*a_PI)*560.0;
		}
	else
		{
		unit[n].pp_x[0]=unit[pt].Position.X+cos(angl2*a_PI)*280.0;
		unit[n].pp_y[0]=unit[pt].Position.Y+sin(angl2*a_PI)*280.0;
		}

	unit[n].pp_x[1]=unit[pt].Position.X+cos(angl2*a_PI)*130.0;
	unit[n].pp_y[1]=unit[pt].Position.Y+sin(angl2*a_PI)*130.0;

	unit[n].pp_x[2]=unit[pt].Position.X+cos(angl2*a_PI)*10.0;
	unit[n].pp_y[2]=unit[pt].Position.Y+sin(angl2*a_PI)*10.0;

	unit[n].pp_x[3]=unit[pt].Position.X+cos(angl*a_PI)*100.0;
	unit[n].pp_y[3]=unit[pt].Position.Y+sin(angl*a_PI)*100.0;


	unit[n].pp_x[4]=MAP_RIGHT+1;
//	unit[n].pp_now=0;
	unit[n].stop=0;
	}







//============================================================================
//
//----------------------------------------------------------------------------
public void	cont_pos_of_take_down( int n )
	{
	int		i;
	double			angl=0 /* C4701 */,angl2,dstc;
	int				pt,pos_of_no,a,b,c;



	pt=unit[n].info[1];
	//pt=4;
	//pos_of_no=unit[n].no;

	if( !(unit[pt].kind==UnitKind.Carrier || unit[pt].kind==UnitKind.LightCarrier || unit[pt].kind==UnitKind.AirBase) )
		{
		unit[n].info[1]=0;
		pt=0;
		}

	if( /*paint_effect_on &&*/ unit[pt].used==0 || pt==0 )
		{
		// もどる場所がない場合、あった場所で適当に動く
		unit[n].pp_x[0]=unit[n].Position.X+(double)(rnd(400)-200);
		unit[n].pp_y[0]=unit[n].Position.Y+(double)(rnd(400)-200);

		unit[n].pp_x[1]=MAP_RIGHT+1;
		unit[n].stop=0;

		return;
		}


	switch((int)(unit[pt].drctn/22.5))
		{
		case 0: case 15:
			angl=0.0;
			break;
		case 1: case 2:
			angl=45.0;
			break;
		case 3: case 4:
			angl=90.0;
			break;
		case 5: case 6:
			angl=90.0+45.0;
			break;
		case 7: case 8:
			angl=180.0;
			break;
		case 9: case 10:
			angl=180.0+45.0;
			break;
		case 11: case 12:
			angl=270.0;
			break;
		case 13: case 14:
			angl=270.0+45.0;
			break;
		}

	angl2=angl;
	angl2-=180.0;	dstc=200;

	if( angl<0 )
		angl=360+angl;




	for( i=0; unit[n].pp_x[i]!=MAP_RIGHT+1; i++){}

	switch( i )
		{
		case 4:
			// 全4点を調整			
			if( unit[n].kind==UnitKind.Bomber )
				{
				unit[n].pp_x[0]=unit[pt].Position.X+cos(angl2*a_PI)*560.0;
				unit[n].pp_y[0]=unit[pt].Position.Y+sin(angl2*a_PI)*560.0;
				}
			else
				{
				unit[n].pp_x[0]=unit[pt].Position.X+cos(angl2*a_PI)*280.0;
				unit[n].pp_y[0]=unit[pt].Position.Y+sin(angl2*a_PI)*280.0;
				}

			unit[n].pp_x[1]=unit[pt].Position.X+cos(angl2*a_PI)*130.0;
			unit[n].pp_y[1]=unit[pt].Position.Y+sin(angl2*a_PI)*130.0;

			unit[n].pp_x[2]=unit[pt].Position.X+cos(angl2*a_PI)*10.0;
			unit[n].pp_y[2]=unit[pt].Position.Y+sin(angl2*a_PI)*10.0;

			unit[n].pp_x[3]=unit[pt].Position.X+cos(angl*a_PI)*100.0;
			unit[n].pp_y[3]=unit[pt].Position.Y+sin(angl*a_PI)*100.0;
			break;
		
		case 3:
			// 全3点を調整			
			unit[n].pp_x[0]=unit[pt].Position.X+cos(angl2*a_PI)*130.0;
			unit[n].pp_y[0]=unit[pt].Position.Y+sin(angl2*a_PI)*130.0;

			unit[n].pp_x[1]=unit[pt].Position.X+cos(angl2*a_PI)*10.0;
			unit[n].pp_y[1]=unit[pt].Position.Y+sin(angl2*a_PI)*10.0;

			unit[n].pp_x[2]=unit[pt].Position.X+cos(angl*a_PI)*100.0;
			unit[n].pp_y[2]=unit[pt].Position.Y+sin(angl*a_PI)*100.0;
			break;

		case 2:
			// 全2点を調整			
			unit[n].pp_x[0]=unit[pt].Position.X+cos(angl2*a_PI)*10.0;
			unit[n].pp_y[0]=unit[pt].Position.Y+sin(angl2*a_PI)*10.0;

			unit[n].pp_x[1]=unit[pt].Position.X+cos(angl*a_PI)*100.0;
			unit[n].pp_y[1]=unit[pt].Position.Y+sin(angl*a_PI)*100.0;
			break;
		}
	}










/*******
//============================================================================
//絶対スクリーン座標に線を描画する
//----------------------------------------------------------------------------
void	draw_line(int	x1,int	y1,int	x2,int	y2)
	{
	int		dstX, dstY,addX,addY;
	int		ctr,x,y,i;




	//書き込むＶＲＡＭのアドレスを得る（ロックして書き込めるようにする）
	memset(&dst_ddsd, 0, sizeof(DDSURFACEDESC));
	dst_ddsd.dwSize = sizeof(DDSURFACEDESC);
	IDirectDrawSurface_Lock( lpDDSBack, NULL, (LPDDSURFACEDESC)&dst_ddsd, DDLOCK_WAIT, NULL );
	dst_vram=dst_ddsd.lpSurface;
//	memset(dst_vram, NULL,  307200 );	//内容をクリア


	dstX=x2-x1;	dstY=y2-y1;

	if(dstX<0)	{addX=-1;	dstX*=-1;}	else	addX=1;
	if(dstY<0)	{addY=-1;	dstY*=-1;}	else	addY=1;




	ctr=0;
	x=x1;	y=y1;
	if(dstX>=dstY)
		{
		for(i=0;i<dstX;i++)
			{
			if( x>=0 && y>=0 && x<=CMBT_WIDTH-1 && y<=CMBT_HEIGHT-1 )
				{
				dst_vram[y*dst_ddsd.lPitch+(x*2)]=0xff;
				dst_vram[y*dst_ddsd.lPitch+(x*2)+1]=0xff;
				}

			x+=addX;
			ctr+=dstY;
			if(ctr>=dstX)
				{
				y+=addY;
				ctr-=dstX;
				}
			}
		}
	else
		{
		for(i=0;i<dstY;i++)
			{
			if( x>=0 && y>=0 && x<=CMBT_WIDTH-1 && y<=CMBT_HEIGHT-1 )
				{
				dst_vram[y*dst_ddsd.lPitch+(x*2)]=0xff;
				dst_vram[y*dst_ddsd.lPitch+(x*2)+1]=0xff;
				}

			y+=addY;
			ctr+=dstX;
			if(ctr>=dstY)
				{
				x+=addX;
				ctr-=dstY;
				}			
			}
		}



	IDirectDrawSurface_Unlock( lpDDSBack, NULL );

	}
*******/









//============================================================================
// 可視不可視の発見範囲内の潜航潜水艦
//----------------------------------------------------------------------------
public int		find_out_ss( int m , int n)
	{
	int		q_size,q_size2;
	double	wrk_x,wrk_y,dstc,drctn;



	// 現地点からユニット地点への距離
	wrk_x=unit[n].Position.X-unit[m].Position.X;
	wrk_y=unit[n].Position.Y-unit[m].Position.Y;
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

	q_size=((int)((wrk_x)/(cos(drctn*a_PI))));

//dbg[0]=q_size;


//	if( unit[m].type )
//		q_size*=0.6;

	if( q_size<100 )
		q_size=100;

	if( rnd( ( unit[m].type==1 ? q_size : q_size )*( 1+(unit[n].spd==0 ? 1 : 0)*4) )==2 )
		{	// 敵潜水艦探知！
		if( q_size>150 )
			q_size=150;

		q_size2=(int)(q_size*( unit[m].type==0 ? 0.60 : 0.50 ));


		//if( q_size<100 )
		unit[n].info[7]=(int)(unit[n].Position.X+rnd((q_size2)*2)-q_size2);
		unit[n].info[8]=(int)(unit[n].Position.Y+rnd((q_size2)*2)-q_size2);

		unit[n].info[9]=q_size;
		unit[n].info[10]=250+rnd(150);
		return(1);
		}
	else
		{
		return(0);
		}
	}






//============================================================================
// 可視不可視
//----------------------------------------------------------------------------
public void		find_out()
	{
	int		m,n,size,flg1,flg2,s;
	double	wrk_x,wrk_y,dstc,drctn;



	if( reveal!=0 || game_end!=GameResult.None )
		{
		for(m=0;m<=max_unit;m++)
			unit[m].found=1;
		return;
		}


	for(m=0;m<=max_unit;m++)
		{
		if( unit[m].info[10]!=0 )
			unit[m].info[10]--;
		else
			unit[m].found=0;
		}


	// ユニットの見え隠れ
	for(m=1;m<=max_unit;m++)
		{
		if( unit[m].used!=0 && unit[m].used==your_side  && unit[m].PlaneState!=UnitState.Parked  && unit[m].spry==0 )
			{
			flg1=0;
			for(n=1;n<=max_unit;n++)
				{
				if( unit[n].used!=0 && unit[n].used!=your_side && unit[n].PlaneState!=UnitState.Parked && (unit[n].found==0 || unit[m].kind==UnitKind.Submarine ) )
					{
					// 現地点からユニット地点への距離
					wrk_x=unit[m].Position.X;
					wrk_y=unit[m].Position.Y;

					if( unit[m].kind==UnitKind.Fighter )
						{	// 航空機の場合はちょっと前へ
						wrk_x+=cos(unit[m].drctn*a_PI)*FT_EYE;
						wrk_y+=sin(unit[m].drctn*a_PI)*FT_EYE;
						}
					wrk_x=unit[n].Position.X-wrk_x;
					wrk_y=unit[n].Position.Y-wrk_y;


					//wrk_x=unit[n].x-unit[m].x;
					//wrk_y=unit[n].y-unit[m].y;
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


					size=find_out_size(m,n);
					dstc=(wrk_x)/(cos(drctn*a_PI));

					if( (int)dstc<=size && size!=0 )
						{
						flg1++;
						if( unit[n].kind==UnitKind.Submarine && unit[n].info[6]!=0 )
							{	// 潜航中潜水艦が発見可能範囲にいる
							if( find_out_ss(m,n)!=0 )
								{
								unit[n].found=1;

#if false
								for(s=1; s<=max_unit; s++)
									{
									if( unit[s].kind==UnitKind.Destroyer && unit[s].em_flg[0] && unit[s].em_flg[1]==n && unit[s].used!=unit[n].used /*&& unit[s].used==cpu_side*/  && unit[s].used )
										{
										// 駆逐艦の対潜水艦起動の再設定
										unit[s].em_flg[0]=0;
										unit[s].em_flg[1]=0;
										}	
									}
#endif


								}
							}
						else
							{	
							unit[n].found=1;	// 普通のユニットが見つかった場合

							if( unit[m].kind==UnitKind.Submarine && unit[m].spry==0 && unit[n].kind!=UnitKind.Submarine )		// 発見したのが潜水艦の場合。
								{						
								unit[m].info[6]=1;		// 潜ります。
								}
							}
						}
					}
				}

			if( flg1==0 && unit[m].kind==UnitKind.Submarine )		// 潜水艦が発見できなかった
				{
				unit[m].info[6]=0;					// 浮上します。
				unit[m].found=0;					// クリアします。
				unit[m].info[10]=0;
				}

			}

		if( unit[m].used!=0 && unit[m].used!=your_side  && unit[m].PlaneState!=UnitState.Parked  && unit[m].spry==0 )
			{
			flg2=0;
			for(n=1;n<=max_unit;n++)
				{
				if( unit[n].used!=0 && unit[n].used==your_side  && unit[n].PlaneState!=UnitState.Parked && (unit[n].found==0 || unit[m].kind==UnitKind.Submarine) )
					{
					// 現地点からユニット地点への距離


					wrk_x=unit[m].Position.X;
					wrk_y=unit[m].Position.Y;

					if( unit[m].kind==UnitKind.Fighter )
						{	// 航空機の場合はちょっと前へ
						wrk_x+=cos(unit[m].drctn*a_PI)*FT_EYE;
						wrk_y+=sin(unit[m].drctn*a_PI)*FT_EYE;
						}
					wrk_x=unit[n].Position.X-wrk_x;
					wrk_y=unit[n].Position.Y-wrk_y;

					//wrk_x=unit[n].x-unit[m].x;
					//wrk_y=unit[n].y-unit[m].y;
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

					size=find_out_size(m,n);
					dstc=(wrk_x)/(cos(drctn*a_PI));
		
					if( (int)dstc<=size && size!=0 )
						{
						flg2++;
						if( unit[n].kind==UnitKind.Submarine && unit[n].info[6]!=0 )
							{	// 潜航中潜水艦が発見可能範囲にいる
							if( find_out_ss(m,n)!=0 )
								{
								unit[n].found=1;

#if false
								for(s=1; s<=max_unit; s++)
									{
									if( unit[s].kind==UnitKind.Destroyer && unit[s].em_flg[0] && unit[s].em_flg[1]==n && unit[s].used!=unit[n].used /*&& unit[s].used==cpu_side*/ && unit[s].used )
										{
										// 駆逐艦の対潜水艦起動の再設定
										unit[s].em_flg[0]=0;
										unit[s].em_flg[1]=0;
										}	
									}
#endif

								}
							}
						else
							{	// 普通のユニットが見つかった場合
							unit[n].found=1;

							if( unit[m].kind==UnitKind.Submarine && unit[m].spry==0 && unit[n].kind!=UnitKind.Submarine )		// 発見したのが潜水艦の場合。
								{						
								unit[m].info[6]=1;
								}
							}
						}
					}
				}

			if( flg2==0 && unit[m].kind==UnitKind.Submarine )		// 潜水艦が発見できなかった
				{
				unit[m].info[6]=0;					// 浮上します
				unit[m].found=0;					// クリアします。
				unit[m].info[10]=0;
				}

			}
		}
	}










#endif
}
