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

// Port of demo.cpp. So far, its globals and go_cnct_game_setting are ported.

namespace OpenNspw;

public partial class Nspw
{
public int	exist_auto_save;



//============================================================================
// 
// コネクトゲームスタートの値
//----------------------------------------------------------------------------
public void go_cnct_game_setting()
	{


	sinario=0;


	mode=CNCT_GAME_SETTING;
	host_side=0;
	decision_sw=1;
	arrival_cont=0;

	spry_rate[0]=0;		// Host
	spry_rate[1]=0;		// Guest

	first_spry_pt[0]=0;		// Host
	first_spry_pt[1]=0;		// Guest


	if( hwndChatDlg!=null )
		{
		DestroyWindow(hwndChatDlg);
		hwndChatDlg=null;
		}


#if SND_SW
	lpDSB_[SEA1][0/*snd_[0]*/].Stop();		// 
#endif




HANDLE	hFile;

	hFile=CreateFile("Saved\\auto_save.dat", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
													null, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, null);

	if( INVALID_HANDLE_VALUE==hFile )
		{
		// ファイルありませんでした。
		exist_auto_save=0;
		}
	else
		{
		// ファイルはあった。
		exist_auto_save=1;
		CloseHandle(hFile);
		}

	}
}
