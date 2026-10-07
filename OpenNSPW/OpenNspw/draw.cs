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

// Port of draw.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{

public bool g_bDeviceLost = false;						// デバイスの消失フラグ



/*-------------------------------------------
	アプリがアクティブの時のアイドリング
--------------------------------------------*/
public void	draw()
	{
	RECT	src_rect,dstn_rect;






	src_rect.left = 0;
	src_rect.top = 1759;
	src_rect.right = src_rect.left+80;
	src_rect.bottom = src_rect.top+80;


	dstn_rect.left=100;
	dstn_rect.top=100;

	// オブジェクトを画面に描画

	lpDDSBack.BltFast(0+cc_count,300,lpDDS_OS,&src_rect, DDBLTFAST_SRCCOLORKEY| DDBLTFAST_WAIT);
//	lpDDSBack->BltFast(0+cc_count,300,lpDDS_OS,&src_rect, DDBLTFAST_WAIT);
//	lpDDSBack->BltFast(300,300,lpDDS_OS,&src_rect, DDBLTFAST_WAIT);

	HDC hdc;
	if (DD_OK==lpDDSBack.GetDC(&hdc)) 
		{
		SetTextColor(hdc,0x00ff7f00);
		SetBkColor(hdc,0x000000);
		TextOut(hdc,20,400,"<- 前のスライド",15);
		TextOut(hdc,500,400,"次のスライド ->",15);
		SetTextColor(hdc,0x0000ffff);
		TextOut(hdc,190,440,"方向キーを押すとスライドが変わります",36);
		lpDDSBack.ReleaseDC(hdc);
		}


	// プライマリサーフェスにフリップ
	lpDDSPrimary.Flip(null,DDFLIP_WAIT); 
	}
}
