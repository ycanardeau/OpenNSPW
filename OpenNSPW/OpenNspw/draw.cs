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

// Port of draw.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{

[Original("g_bDeviceLost")] public bool IsDeviceLost = false;						// デバイスの消失フラグ

/*-------------------------------------------
	アプリがアクティブの時のアイドリング
--------------------------------------------*/
[Original("draw")]
public void	Draw()
	{
	RECT	source,destination;

	source.left = 0;
	source.top = 1759;
	source.right = source.left+80;
	source.bottom = source.top+80;

	destination.left=100;
	destination.top=100;

	// オブジェクトを画面に描画

	lpDDSBack.BltFast(0+Tick,300,OffscreenSurface,&source, DDBLTFAST_SRCCOLORKEY| DDBLTFAST_WAIT);

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
