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

// Port of win_proc.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{







//============================================================================
//失ったオブジェクトを再読み込みする
//----------------------------------------------------------------------------
public void	restoreAll()
	{

	IDirectDrawSurface_Restore(lpDDSPrimary);
	IDirectDrawSurface_Restore(lpDDSBack);
	IDirectDrawSurface_Restore(lpDDS_OS);


	RELEASE(ref lpDDS_OS);
	lpDDS_OS=bitmap_surface("t3.bmp");

	// ピクセルフォーマットを取得
	DDPIXELFORMAT ddpf;
	ddpf.dwSize=(uint)(sizeof(DDPIXELFORMAT));
	lpDDS_OS.GetPixelFormat(&ddpf);

	uint KeyColor;
	KeyColor = ddpf.dwRBitMask | ddpf.dwBBitMask;


	// カラーキーを設定
	DDCOLORKEY key;
	key.dwColorSpaceLowValue = KeyColor;
	key.dwColorSpaceHighValue = KeyColor;
	lpDDS_OS.SetColorKey(DDCKEY_SRCBLT, &key);

/*	
	if( IDirectDrawSurface_Restore(lpDDSPrimary) == DD_OK )
		{
		if( IDirectDrawSurface_Restore(lpDDS_OS) == DD_OK)
#if LNGG_VER==0
			DDReLoadBitmap(lpDDS_OS,"tst_cg1.bmp");
#else
			DDReLoadBitmap(lpDDS_OS,"tst_cg1_eng.bmp");
#endif
		}
*/



	// クリッパー
	RELEASE(ref lpDDclip);
	if(fullscreen!=0)
		{
		lpDD.CreateClipper(0,out lpDDclip,null);
		lpDDclip.SetHWnd(0,hwndApp);
		lpDD.FlipToGDISurface();
		}
	else
		{
		lpDD.CreateClipper(0,out lpDDclip,null);
		lpDDclip.SetHWnd(0,hwndApp);
		lpDDSPrimary.SetClipper(lpDDclip);
		}



	// マップを作りなおす
//	make_map();		
	make_map_cg();

	}








/*-------------------------------------------

--------------------------------------------*/
public void	load_user_map()
	{
	Array2<byte> bf = default;
	HANDLE	hFile;
	short	m,n,f,i;
	Array256<Array256<ushort>> szBuf = default;					// マップ



	hFile=CreateFile( user_sinario_fn /*"Map\\user_map.dat"*/, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
								null, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, null);


	if( hFile != INVALID_HANDLE_VALUE )
		{
		uint	dwActBytes;

		// 読み込み
		SetFilePointer( hFile,0,0,FILE_BEGIN);		// 先頭へ

		ReadFile( hFile, ref szBuf, (uint)(sizeof(Array256<Array256<ushort>>)), &dwActBytes, null );

		// バッファよりデータへ
		// データ を バッファへ
		for(m=0;m<=255;m++)
			for(n=0;n<=255;n++)
				cmbt_map[m][n]=szBuf[m][n];

		ReadFile( hFile, ref unit, (uint)(sizeof(Array256<UNIT>)), &dwActBytes, null );
		ReadFile( hFile, ref rein, (uint)(sizeof(Array3<byte>)), &dwActBytes, null );


		ReadFile( hFile, ref decision_sw,  sizeof(byte) , &dwActBytes, null );
		ReadFile( hFile, ref arrival_cont, sizeof(byte), &dwActBytes, null );

		ReadFile( hFile, ref spry_rate, (uint)(sizeof(Array2<short>)), &dwActBytes, null );
		ReadFile( hFile, ref first_spry_pt, (uint)(sizeof(Array2<short>)), &dwActBytes, null );

		ReadFile( hFile, ref rvrs_time, sizeof(short), &dwActBytes, null );
		ReadFile( hFile, ref rvrs_rule, sizeof(short), &dwActBytes, null );



		CloseHandle(hFile);
		}

	}



/*-------------------------------------------

--------------------------------------------*/
public void	load_it2(string str)
	{
	Array20<byte> bf = default;
	HANDLE	hFile;
	short	m,n,f,i;
	Array256<Array256<ushort>> szBuf = default;					// マップ



	if( strcmp ( str,"Map\\South_pacific.dat")==0 )
		map_now=0;
	else if( strcmp ( str,"Map\\Middle_pacific.dat")==0 )
		map_now=1;
	else /*if( strcmp ( str,"Map\\Japan_off.dat")==0 )*/
		map_now=2;
/*
	else 
		map_now=3;
*/



	hFile=CreateFile(str, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
								null, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, null);


	if( hFile != INVALID_HANDLE_VALUE )
		{
		uint	dwActBytes;

		// 読み込み
		SetFilePointer( hFile,0,0,FILE_BEGIN);		// 先頭へ

		ReadFile( hFile, ref szBuf, (uint)(sizeof(Array256<Array256<ushort>>)), &dwActBytes, null );

		// バッファよりデータへ
		// データ を バッファへ
		for(m=0;m<=255;m++)
			for(n=0;n<=255;n++)
				cmbt_map[m][n]=szBuf[m][n];

		CloseHandle(hFile);
		}

	}







/*-------------------------------------------

--------------------------------------------*/
public void	load_it3()
	{
	Array2<byte> bf = default;
	HANDLE	hFile;
	short	m,n,f,i;
	Array256<Array256<ushort>> szBuf = default;					// マップ



	hFile=CreateFile( user_sinario_fn /*"Map\\user_map.dat"*/, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
								null, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, null);


	if( hFile != INVALID_HANDLE_VALUE )
		{
		uint	dwActBytes;

		// 読み込み
		SetFilePointer( hFile,0,0,FILE_BEGIN);		// 先頭へ

		ReadFile( hFile, ref szBuf, (uint)(sizeof(Array256<Array256<ushort>>)), &dwActBytes, null );

		// バッファよりデータへ
		// データ を バッファへ
		for(m=0;m<=255;m++)
			for(n=0;n<=255;n++)
				cmbt_map[m][n]=szBuf[m][n];

		ReadFile( hFile, ref unit, (uint)(sizeof(Array256<UNIT>)), &dwActBytes, null );
		ReadFile( hFile, ref rein, (uint)(sizeof(Array3<byte>)), &dwActBytes, null );

		CloseHandle(hFile);
		}

	}




/*-------------------------------------------

--------------------------------------------*/
public void	save_user_map()
	{
	Array2<byte> bf = default;
	HANDLE	hFile;
	short	m,n,f,i;
	uint	dwActBytes;
	Array256<Array256<ushort>> szBuf = default;					// マップ
	short		data;	



	hFile=CreateFile( user_sinario_fn/*"Map\\user_map.dat"*/, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
								null, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, null);

	if( hFile != INVALID_HANDLE_VALUE )
		{
		//unsigned short		cmbt_map[256][256];					// マップ

		SetFilePointer( hFile,0,0,FILE_BEGIN);		// 先頭へ


		// マップを記録
		// データ を バッファへ
		for(m=0;m<=255;m++)
			for(n=0;n<=255;n++)
				szBuf[m][n]=cmbt_map[m][n];
		
		WriteFile(hFile, ref szBuf,(uint)(sizeof(Array256<Array256<ushort>>)),&dwActBytes,null);	// 書き込み


		//　ユニット、その他を記録
		// 書き込み
		WriteFile(hFile, ref unit,(uint)(sizeof(Array256<UNIT>)),&dwActBytes,null);
		WriteFile(hFile, ref rein,(uint)(sizeof(Array3<byte>)),&dwActBytes,null);


		WriteFile(hFile, ref decision_sw,sizeof(byte),&dwActBytes,null);
		WriteFile(hFile, ref arrival_cont,sizeof(byte),&dwActBytes,null);

		WriteFile(hFile, ref spry_rate,(uint)(sizeof(Array2<short>)),&dwActBytes,null);
		WriteFile(hFile, ref first_spry_pt,(uint)(sizeof(Array2<short>)),&dwActBytes,null);

		WriteFile(hFile, ref rvrs_time,sizeof(short),&dwActBytes,null);
		WriteFile(hFile, ref rvrs_rule,sizeof(short),&dwActBytes,null);

		CloseHandle(hFile);
		}
	}






/*-------------------------------------------
	終了の処理
--------------------------------------------*/
public int EndApp()
	{
	int	m,n,i;

	// 
	if (gameFont_1)
		DeleteObject(gameFont_1);
	if (gameFont_2)
		DeleteObject(gameFont_2);


	// ダイレクトミュージック
//	lpDMP->CloseDown();
//	for(i=0; i<4; i++)
//		RELEASE(lpSeg[i]);
	RELEASE(ref lpDMP);
	RELEASE(ref lpDML);


	// ダイレクトサウンド
	for(m=0; m<NUM_SOUND_EFFECTS; m++)
		{
		for(n=0; n<SND_DUP; n++)
			{
			RELEASE(ref lpDSB_[m][n]);
			}
		}
	RELEASE(ref lpDSP);
	RELEASE(ref lpDS);


	// ダイレクトドロー
	RELEASE(ref lpDDS_OS);
	RELEASE(ref lpDDclip);
	RELEASE(ref lpDDSBack);
	RELEASE(ref lpDDSPrimary);
	RELEASE(ref lpDD);


	// ダイレクトプレイ
	if( g_pDP!=null )
		{
		g_pDP.Close(0);
		g_pDP.Release();
		g_pDP = null;
		}

	if( g_pThreadPool!=null )
		{
		g_pThreadPool.Close(0);
		g_pThreadPool.Release();
		g_pThreadPool = null;
		}


	// Write information to the registry
	DXUtil_WriteStringRegKey( hDPlaySampleRegKey, TEXT("Player Name"), g_strLocalPlayerName );
//	DXUtil_WriteStringRegKey( hDPlaySampleRegKey, TEXT("Session Name"), g_strSessionName );
	DXUtil_WriteStringRegKey( hDPlaySampleRegKey, TEXT("Preferred Provider"), g_strPreferredProvider );
	DXUtil_WriteStringRegKey( hDPlaySampleRegKey, TEXT("Remote Hostname"), g_strRemoteHostname );

	RegCloseKey( hDPlaySampleRegKey );



	// DirectInputのデバイスを解放
	if (pDIDevice!=null)
		pDIDevice.Unacquire(); 
	RELEASE(ref pDIDevice);

	if (pDIDeviceMouse!=null)
		pDIDeviceMouse.Unacquire(); 
	RELEASE(ref pDIDeviceMouse);


	RELEASE(ref pDInput);


	// COM 終了
	CoUninitialize();


	return TRUE;
	}







/*--------------------------------------------
	アプリ変数の起動時初期化
---------------------------------------------*/
public void	init_apl_reg()
	{

	cc_count=0;



	/*** フォント設定 ***/
	if (gameFont_1)
		DeleteObject(gameFont_1);
	gameFont_1=CreateFont(16,0,0,0,0,FALSE,FALSE,FALSE,SHIFTJIS_CHARSET,OUT_DEFAULT_PRECIS,CLIP_DEFAULT_PRECIS,DEFAULT_QUALITY,DEFAULT_PITCH,null); // フォントオブジェクト


	if (gameFont_2)
		DeleteObject(gameFont_2);
	gameFont_2=CreateFont(20,0,0,0,0,FALSE,FALSE,FALSE,SHIFTJIS_CHARSET,OUT_DEFAULT_PRECIS,CLIP_DEFAULT_PRECIS,DEFAULT_QUALITY,DEFAULT_PITCH,null); // フォントオブジェクト



	lf_btn=0;	ri_btn=0;


	user_sinario_fn[0] = unchecked((byte)'\0');

	}





/*--------------------------------------------
	
---------------------------------------------*/
public IDirectDrawSurface7? bitmap_surface(string file_name)
	{
	HDC hdc;
	HBITMAP bit;
	IDirectDrawSurface7? surf;





	// lインターフェイスビットマップをロード

	bit=(HBITMAP) LoadImage(null,file_name,IMAGE_BITMAP,0,0,
								LR_DEFAULTSIZE|LR_LOADFROMFILE);
	if (!bit) 
		// ロード失敗、呼び出し側に失敗を返す
		return null;

	// ビットマップのディメンジョンを取得

	BITMAP bitmap;
    GetObject( bit, sizeof(BITMAP), &bitmap );
	int surf_width=bitmap.bmWidth;
	int surf_height=bitmap.bmHeight;

	// サーフェスを作成

	int ddrval;
	DDSURFACEDESC2 ddsd;
	ZeroMemory(&ddsd,(nuint)(sizeof(DDSURFACEDESC2)));
	ddsd.dwSize = (uint)(sizeof(DDSURFACEDESC2));
	ddsd.dwFlags = DDSD_CAPS | DDSD_WIDTH | DDSD_HEIGHT ;
	ddsd.ddsCaps.dwCaps = DDSCAPS_OFFSCREENPLAIN | DDSCAPS_SYSTEMMEMORY;
	ddsd.dwWidth = (uint)surf_width;
	ddsd.dwHeight = (uint)surf_height; 

	// サーフェスを実際に作成

	ddrval=lpDD.CreateSurface(&ddsd,out surf,null);

	// 作成できたか確認

	if (ddrval!=DD_OK) {

		// できなかったのでビットマップを解放、呼び出し側に失敗を返す

		DeleteObject(bit);
		return null;

	} else {

		// できたのでサーフェスのDCを取得

		surf.GetDC(&hdc);

		// 互換DCを生成

		HDC bit_dc=CreateCompatibleDC(hdc);

		// インターフェイスをサーフェスにブロック転送

		SelectObject(bit_dc,bit);
		BitBlt(hdc,0,0,surf_width,surf_height,bit_dc,0,0,SRCCOPY);

		// DCを解放

		surf.ReleaseDC(hdc);
		DeleteDC(bit_dc);

		// save the dimensions if rectangle pointer provided
/*
		if (dims) 
			{
			dims->left=0;
			dims->top=0;
			dims->right=surf_width;
			dims->bottom=surf_height;
			}
*/
	}

	// ビットマップをクリア 

	DeleteObject(bit);

	// 呼び出し側にポインタを返す

	return surf;
	}


/*-------------------------------------------
	　ファイル　ＳＡＶＥ
--------------------------------------------*/
public nint	IDD_FILE_SAVE_Proc(HWND hWnd,uint msg,nint wParam,nint lParam)
	{
	WIN32_FIND_DATA FindFileData;
	HANDLE hFind;
	Array260<byte> temp_buf = default;


	switch(msg)
		{
		case WM_INITDIALOG:

			SetWindowText(hWnd,"Save File");

			EnableWindow( GetDlgItem( hWnd, IDC_EDIT ), TRUE);

			hFind = FindFirstFile( "Scenario\\*.dat", &FindFileData );

			if( hFind!=INVALID_HANDLE_VALUE )
				{
				// フォルダに最初のなんかのファイルがあった。
				SendDlgItemMessage( hWnd, IDC_LIST, LB_ADDSTRING, 0, FindFileData.cFileName );
				while( TRUE==FindNextFile( hFind, &FindFileData ) )
					{
					// フォルダに次のなんかのファイルがあった。
					SendDlgItemMessage( hWnd, IDC_LIST, LB_ADDSTRING, 0, FindFileData.cFileName );
					}
				FindClose(hFind);
				}
			user_sinario_fn[0] = unchecked((byte)'\0');
			break;



		case WM_COMMAND:
			switch( LOWORD(wParam) )
				{
				case IDC_LIST:
					if( HIWORD( wParam )==LBN_SELCHANGE )
						{
						DlgDirSelectEx( hWnd, user_sinario_fn, sizeof( Array260<byte> ), IDC_LIST );
						SetDlgItemText( hWnd, IDC_EDIT, user_sinario_fn );
						}
					break;


				case IDOK:
//					PostQuitMessage( 0 );		// これでWM_QUITがでるので接続ダイアログループからぬける
					GetDlgItemText( hWnd, IDC_EDIT, user_sinario_fn, MAX_PATH );


					if( map_edit!=0 && mode==GameMode.Battle && user_sinario_fn[0]!='\0' )
						{
						// なんかユーザーファイルが選ばれた。


int	len;
						len=wsprintf( temp_buf, "%s", user_sinario_fn );

						if( !(user_sinario_fn[len-4]=='.' && user_sinario_fn[len-3]=='d' && user_sinario_fn[len-2]=='a' && user_sinario_fn[len-1]=='t') )
							wsprintf( temp_buf, "%s.dat", user_sinario_fn );
	


						wsprintf( user_sinario_fn, "Scenario\\%s", temp_buf );

						save_user_map();
						}


					DestroyWindow(hWnd);
					break;
	

				case IDCANCEL:
//					PostQuitMessage( 0 );		// これでWM_QUITがでるので接続ダイアログループからぬける
					user_sinario_fn[0] = unchecked((byte)'\0');
					DestroyWindow(hWnd);
					break;


				default:
					return FALSE;
				}
			break;

		default:
			return(FALSE);
		}

	return( TRUE );
	}







/*-------------------------------------------
	　ファイル　ＬＯＡＤ
--------------------------------------------*/
public nint	IDD_FILE_LOAD_Proc(HWND hWnd,uint msg,nint wParam,nint lParam)
	{
	WIN32_FIND_DATA FindFileData;
	HANDLE hFind;
	_DP_DATA_20		dp_data_20=default;
	_DP_DATA_1		dp_data_1;
	Array260<byte> temp_buf = default;



	switch(msg)
		{
		case WM_INITDIALOG:
			SetWindowText(hWnd,"Load File");

			hFind = FindFirstFile( "Scenario\\*.dat", &FindFileData );

			if( hFind!=INVALID_HANDLE_VALUE )
				{
				// フォルダに最初のなんかのファイルがあった。
				SendDlgItemMessage( hWnd, IDC_LIST, LB_ADDSTRING, 0, FindFileData.cFileName );
				while( TRUE==FindNextFile( hFind, &FindFileData ) )
					{
					// フォルダに次のなんかのファイルがあった。
					SendDlgItemMessage( hWnd, IDC_LIST, LB_ADDSTRING, 0, FindFileData.cFileName );
					}
				FindClose(hFind);
				}
			user_sinario_fn[0] = unchecked((byte)'\0');
			break;


		case WM_COMMAND:
			switch(wParam)
				{
				case IDOK:
					DlgDirSelectEx( hWnd, user_sinario_fn, sizeof( Array260<byte> ), IDC_LIST );

					if( map_edit!=0 && mode==GameMode.Battle && user_sinario_fn[0]!='\0' )
						{
						wsprintf( temp_buf, "%s", user_sinario_fn );
						wsprintf( user_sinario_fn, "Scenario\\%s", temp_buf );

						load_user_map();
						make_map_cg();
						}
					else if( mode==GameMode.GameSetting && user_sinario_fn[0]!='\0' )
						{
						wsprintf( temp_buf, "%s", user_sinario_fn );
						wsprintf( user_sinario_fn, "Scenario\\%s", temp_buf );

						if( map_edit==0 )
							{
							// なんかユーザーファイルが選ばれた。
							dp_data_20.dwType = MessageType.UserScenarioFileName;
							wsprintf( dp_data_20.friend_chat, "%s",user_sinario_fn );
							bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_20));
							bufferDesc.pBufferData  = (byte*)&dp_data_20;
							g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
							}


						get_sinario_data();

						// ホストの選択状態をゲストにセンドします。
						dp_data_1.dwType = MessageType.LeaveGameSetting;
						dp_data_1.data[0] = (short)host_side;
						dp_data_1.data[1] = sinario;

						dp_data_1.data[2] = spry_rate[0];
						dp_data_1.data[3] = spry_rate[1];

						dp_data_1.data[4] = decision_sw;

						dp_data_1.data[5] = first_spry_pt[0];
						dp_data_1.data[6] = first_spry_pt[1];

						dp_data_1.data[7] = arrival_cont;

						dp_data_1.data[8] = rvrs_time;
						dp_data_1.data[9] = rvrs_rule;



						bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
						bufferDesc.pBufferData  = (byte*) &dp_data_1;
						g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );



						mode=GameMode.ConfigSetting;
						}



					DestroyWindow(hWnd);
//					PostQuitMessage( 0 );		// これでWM_QUITがでるので接続ダイアログループからぬける
					break;
	

				case IDCANCEL:
					user_sinario_fn[0] = unchecked((byte)'\0');
					DestroyWindow(hWnd);
//					PostQuitMessage( 0 );		// これでWM_QUITがでるので接続ダイアログループからぬける
					break;

				case IDC_LIST:
					break;


				default:
					return FALSE;
				}
			break;

		default:
			return(FALSE);
		}

	return( TRUE );
	}





/*-------------------------------------------
	　OK　キャンセル
--------------------------------------------*/
public nint	IDD_OK_CANCEL_Proc(HWND hWnd,uint msg,nint wParam,nint lParam)
	{
	switch(msg)
		{
		case WM_INITDIALOG:
//			dlg_answer=0;

			if( dlg_answer==MessageType.GoToGameSetting )
				SetWindowText(hWnd,"Exit Without Saving?");
			else
				SetWindowText(hWnd,"Resume-save and Exit?");

			break;

		case WM_COMMAND:
			switch(wParam)
				{
				case IDOK:
//					dlg_answer=1;

					bf_game_system_menu[1]=(byte)dlg_answer;
					you_can_order=0;
					you_ordered=1;
					SoundPlayEffect( 0, CLICK2 ,(double)(MAP_RIGHT+1), 0);

					DestroyWindow(hWnd);
					g_hDlg=null;
//					PostQuitMessage( 0 );		// これでWM_QUITがでるので接続ダイアログループからぬける
					break;
	

				case IDCANCEL:
//					dlg_answer=0;
					DestroyWindow(hWnd);
					g_hDlg=null;
//					PostQuitMessage( 0 );		// これでWM_QUITがでるので接続ダイアログループからぬける
					break;


				default:
					return FALSE;
				}
			break;

		default:
			return(FALSE);
		}

	return( TRUE );
	}



/*-------------------------------------------
  マイダイアログのループウェイト
	事実上のモーダルダイアログ、制御を戻さない
	にする。
--------------------------------------------*/
public void	my_dlg_wait()
	{
	MSG msg=default;
	int	wait_for_connect=TRUE;




	while(wait_for_connect!=0)
		{
		while( PeekMessage(ref msg, null, 0, 0, PM_REMOVE)!=0 )
			{
			if (msg.message == WM_QUIT )
				{
				// Quit the application.
				wait_for_connect = FALSE;
				break;
				}

			if ( IsDialogMessage( g_hDlg, ref msg)==0 )
				{
				TranslateMessage(ref msg);
				DispatchMessage(ref msg);
				}
			}
		}



	}





/*-------------------------------------------

--------------------------------------------*/
public nint ChatDlgProc(HWND hWnd,uint msg,nint wParam,nint lParam)
	{
//	TCHAR			my_ctring[MAX_PATH];
	_DP_DATA_20	dp_data_20=default;
	int			m;




	switch(msg)
		{
		case WM_INITDIALOG:
			input_chat_now=1;
			break;
		case WM_COMMAND:
			switch(wParam)
				{
				case IDOK:

//					GetDlgItemText( hWnd, IDC_EDIT1, my_ctring, MAX_PATH );
					GetDlgItemText( hWnd, IDC_EDIT1, my_chat, MAX_PATH );
					my_chat_dsp_time=unchecked((byte)CHAT_DSP_TIME);

				// なんか入力があったならセンドする
					dp_data_20.dwType = MessageType.Chat;
					for(m=0;m<128;m++)
						{
						dp_data_20.friend_chat[m]=my_chat[m];
						}

					bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_20));
					bufferDesc.pBufferData  = (byte*) &dp_data_20;
					g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1, 0, null, ref hAsync, MUST_SEND );


//					EndDialog( hWnd, 0 );
					DestroyWindow(hwndChatDlg);
					hwndChatDlg=null;
					input_chat_now=0;
					break;
	

				case IDCANCEL:
//					EndDialog( hWnd, 0 );
					DestroyWindow(hwndChatDlg);
					hwndChatDlg=null;
					input_chat_now=0;
					break;

				default:
					return FALSE;
				}
			break;


		case WM_KEYDOWN:
			switch(wParam)
				{
				case VK_F1:
					break;
				case VK_ESCAPE:
				case VK_F12:
//					EndDialog( hWnd, 0 );
					DestroyWindow(hwndChatDlg);
					hwndChatDlg=null;
					input_chat_now=0;
					break;
				default:
					return FALSE;
				}
			break;


		default:
			return FALSE;
		}

	return TRUE;
	}



/*-------------------------------------------
	ウィンドウ処理
--------------------------------------------*/
public nint MainWndProc(HWND hWnd,uint msg,nint wParam,nint lParam)
	{
	int		m=default /* C4701 */,n,i;
	double	wrk_x,wrk_y,wrk_x2,wrk_y2;
	//OPENFILENAME ofn;		// Unused.
	Array260<byte> cd_buf = default;			// ユーザーシナリオのファイルネーム
	uint		nBufferLength;





	switch(msg)
		{
		case WM_ACTIVATE:		// ウインドウのアクティブ状態が変化
			if( pDIDevice == null || pDIDeviceMouse == null )
				break;

			if(wParam == WA_INACTIVE)
				{
				pDIDevice.Unacquire();
				pDIDeviceMouse.Unacquire();
				appActive=0;
				}
			else
				{
				pDIDevice.Acquire();
				pDIDeviceMouse.Acquire();
				appActive=1;
				}

			break;


		case WM_ACTIVATEAPP:	//ウインドウが選択された時
			if(wParam == WA_INACTIVE)
				appActive=0;
			else
				appActive=1;
			break;

		case WM_SIZE:		// ウインドウ起動時にもここにくるようだ。
#if false
			if(wParam == SIZE_RESTORED || wParam == SIZE_MAXIMIZED)
				{
				d3dpp.BackBufferWidth = LOWORD(lParam);
				d3dpp.BackBufferHeight = HIWORD(lParam);
				if(pD3DDevice /*&& !sizeMoving*/ )
					{
					// ここで強制復元
					pD3DDevice->Reset(&d3dpp);
					RELEASE(pD3DXSprite);

//					InitRender();

					D3DXCreateSprite(pD3DDevice,&pD3DXSprite);
					InvalidateRect(hWnd,NULL,TRUE);

					}
				}
#endif

			break;




		case WM_CREATE:			//ウインドウ作成時
			break;



		case WM_LBUTTONDOWN:
			break;
		case WM_LBUTTONUP:
			break;


		case WM_MOUSEMOVE:
			// マウスカーソルのウィンドウ上の座標を得る。
			break;



		case WM_KEYDOWN:
			switch(wParam)
				{

				case VK_F1:
					if( map_edit!=0 && mode==GameMode.Battle )
						{
						/*g_hDlg =*/ CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_FILE_CONT), hwndApp, (DLGPROC)IDD_FILE_SAVE_Proc );
#if false
						my_dlg_wait();

						if( user_sinario_fn[0]!='\0' )
							{
							// なんかユーザーファイルが選ばれた。
TCHAR		temp_buf[MAX_PATH];

							wsprintf( temp_buf, "%s", user_sinario_fn );
							wsprintf( user_sinario_fn, "Scenario\\%s", temp_buf );

							save_user_map();
							}
#endif


						SoundPlayEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
						}

//					play_snd( lpDSB_AA_BLT[3][snd_AA_BLT[3]], &snd_AA_BLT[3], 0 );
//		SoundPlayEffect( NULL, CLICK1 ,(double)(MAP_RIGHT+1), 0);
//					if( dbg_menu )
//						dbg_menu--;

					break;


				case VK_F2:
					if( map_edit!=0 && mode==GameMode.Battle )
						{
						unit[the_slct_unit].used=0;
						the_slct_unit=0; 
						cmbt_menu_kind=0; 
						cmbt_menu_slctd=0; 
						cls_all_slct_unit_p2(1);


						// シナリオ選択がユーザーシナリオならファイル選択します。
						/*g_hDlg =*/ CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_FILE_CONT), hwndApp, (DLGPROC)IDD_FILE_LOAD_Proc );
#if false
						my_dlg_wait();


						if( user_sinario_fn[0]!='\0' )
							{
							// なんかユーザーファイルが選ばれた。

TCHAR		temp_buf[MAX_PATH];
							wsprintf( temp_buf, "%s", user_sinario_fn );
							wsprintf( user_sinario_fn, "Scenario\\%s", temp_buf );


							load_user_map();
							make_map_cg();
							}
#endif


						SoundPlayEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
						}
					break;



				case VK_F3:
					if( map_edit!=0 && mode==GameMode.Battle )
						{
						SoundPlayEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
						make_map_cg();
						}
					else
						dbg_menu--;

/*
int	s;

	dbg[2]=0;
	for(s=1;s<FIRE_MAX;s++)
		{
		if( !fire[s].used )
			{
			dbg[2]++;
			}
		}



*/

/*
					if( dbg[1]==1 )
						{
						}
*/
					break;

				case VK_F4:
					dbg_menu++;
					break;

				case VK_F5:
					if( map_edit!=0 && put_trgt>1 && mode==GameMode.Battle )
						{
						put_trgt--;
						SoundPlayEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
						}


#if false
dbg[1]=0;
					for( i=1; i<=max_unit; i++)
						{
						if( unit[i].used && unit[i].ctgry==UnitCategory.Plane && unit[i].used==Side.UnitedStates && unit[i].info[1]==46/* && unit[i].info[3]
							&& unit[i].info[0]==PARKING && unit[m].info[1]==unit[i].info[1] && unit[i].stop==0*/ )
							dbg[1]++;
						}
#endif


#if false

int	f;
n=the_slct_unit;

					if( n && unit[n].used )
						{
						// 命中
//						unit[n].hp[0]-=3;
//						SoundPlayEffect( NULL, TPD_HIT1 ,fire[m].x, fire[m].y);

						f=seek_effect_no();
						effect[f].layer=EffectLayer.Upper;	

						effect[f].info[0]=40;
						effect[f].info[1]=4;	// アニメーションパターン

						effect[f].x=unit[n].x;
						effect[f].y=unit[n].y;

						effect[f].no=1;			// 弾丸着弾	のソースファイル上の番号



						// 当った的に収納機があれば破壊される場合もある
						if( unit[n].kind==UnitKind.AirBase || unit[n].kind==UnitKind.Carrier || unit[n].kind==UnitKind.LightCarrier )
							{
							for(i=0;i<=max_unit;i++)
								{
								if( unit[i].used && unit[i].ctgry==UnitCategory.Plane && unit[i].info[0]==PARKING && unit[i].info[1]==n /*&& rnd(10)==0*/ )
									{
									unit[i].used=0;
									unit[unit[i].info[1]].info[1]--;			// 現在格納数
							
									if( unit[i].info[3]>=1 && unit[unit[i].info[1]].info[4]>=1 && unit[i].info[5]<=SLOW )
										unit[unit[i].info[1]].info[4]--;		// 発艦予定の機数を	
									if( unit[i].info[3]>=3 && unit[unit[i].info[1]].info[7]>=1 && unit[i].info[5]<=SLOW )
										unit[unit[i].info[1]].info[7]--;		// 


									if( unit[i].info[5]==RETURN )
										{
										if(unit[unit[i].info[1]].info[7])
											unit[unit[i].info[1]].info[7]=0;	// 着艦、0許可、1不許可
										if(unit[unit[i].info[1]].info[8])
											unit[unit[i].info[1]].info[8]=0;	// その空母の次機発進許可	0許可、1不許可
										}

//									if( the_slct_unit==i )
//										{ the_slct_unit=0; cmbt_menu_kind=0; cmbt_menu_slctd=0; cls_all_slct_unit_p2(1); }

									break;
									}
								}

							}
						}
#endif


#if DBG_MODE
if( map_edit==0 )
	{
	if(your_side==Side.Japan)
		your_side=Side.UnitedStates;
	else
		your_side=Side.Japan;
	}
#endif


//					if( dbg[1]==1 )
//						reveal=!reveal;
					break;


				case VK_F6:
					if( map_edit!=0 && mode==GameMode.Battle )
						{
						if( put_trgt<24 )
							{
							put_trgt++;
							SoundPlayEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
							}
						}

#if DBG_MODE
if( map_edit==0 )
	{
	mode=GameMode.GameSetting;
	get_sinario_data();

	mode=GameMode.Battle;
	cnct_game_init();
	}
#endif
					break;


				case VK_F7:

					if( map_edit!=0 && mode==GameMode.Battle )
						{
						if( unit[the_slct_unit].used!=0 )
							{
							// 空母か空港なら搭載ユニットも消す
							if( unit[the_slct_unit].kind==UnitKind.Carrier || unit[the_slct_unit].kind==UnitKind.LightCarrier || unit[the_slct_unit].kind==UnitKind.AirBase )
								{
								for( i=1;i<=max_unit;i++)
									{
									if( unit[i].used!=0 && unit[i].ctgry==UnitCategory.Plane && unit[i].info[0]==PARKING && unit[i].info[1]==the_slct_unit)
										{
										unit[i].used=0;
										}
									}
								}

							// パーキング中の航空機なら駐機数を減らします。
							if( unit[the_slct_unit].ctgry==UnitCategory.Plane && unit[the_slct_unit].info[0]==PARKING )
								{
								unit[unit[the_slct_unit].info[1]].info[1]--;	// 現在格納数
								}


//							unit[old_the_slct_unit].hp[0]=0;
							unit[the_slct_unit].used=0;

							the_slct_unit=0; 
							cmbt_menu_kind=0; 
							cmbt_menu_slctd=0; 
							cls_all_slct_unit_p2(1);

							SoundPlayEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
							}


						}
					break;


				case VK_F8:					
					if( map_edit!=0 && mode==GameMode.Battle )
						{
						if( unit_info[1]!=0 && ( unit[old_the_slct_unit].kind==UnitKind.Carrier || unit[old_the_slct_unit].kind==UnitKind.LightCarrier || unit[old_the_slct_unit].kind==UnitKind.AirBase )  && ((UnitKind)put_kind==UnitKind.Fighter || (UnitKind)put_kind==UnitKind.Attacker || (UnitKind)put_kind==UnitKind.Bomber ) )
							{
							// 駐機場への航空機の配置
							if( /*!( (unit[old_the_slct_unit].kind==CV1 || unit[old_the_slct_unit].kind==CVL1)  && ( put_kind==BM1 || (put_kind==FT1&&put_kind_sub==1) ) )   &&*/
								unit[old_the_slct_unit].used==your_side
								)
								m=set_new_unit_plane(your_side,(UnitKind)put_kind,put_kind_sub,old_the_slct_unit,1,NTG);

							if(m!=0)
								SoundPlayEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
							}
						else if( unit_info[1]==0 && crsr_pt.x<=CMBT_WIDTH && crsr_pt.y<=CMBT_HEIGHT )
							{
							// 艦船および、陸上施設

							wrk_x2=(cmbt_x+crsr_pt.x)/80;
							if(wrk_x2<0)
								wrk_x2=0-wrk_x2;
							wrk_x=(int)wrk_x2;
							if(  (wrk_x2-wrk_x)>=0.5  )
								wrk_x+=1;
							if(((cmbt_x+crsr_pt.x)/80)<0)
								wrk_x=0-wrk_x;
							wrk_x*=80;


							wrk_y2=(cmbt_y-crsr_pt.y)/80;
							if(wrk_y2<0)
								wrk_y2=0-wrk_y2;
							wrk_y=(int)wrk_y2;
							if(  (wrk_y2-wrk_y)>=0.5  )
								wrk_y+=1;
							if(((cmbt_y-crsr_pt.y)/80)<0)
								wrk_y=0-wrk_y;
							wrk_y*=80;


							m=0;
							for(i=1;i<=max_unit;i++)
								{
								if( unit[i].used!=0 && unit[i].x==wrk_x && unit[i].y==wrk_y )
									{
									m++;
									break;
									}
								}

							if( m==0 )
								{
								m=set_new_unit_2( your_side, (UnitKind)put_kind, put_kind_sub,  wrk_x,  wrk_y, (double)(0+(your_side==Side.UnitedStates ? 1 : 0)*180 ) );

								if(m!=0)
									{
									SoundPlayEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
									}
								if((UnitKind)put_kind==UnitKind.Transport)
									{
									unit[m].arm[0]=put_kind_sub;		// 武装品種
									unit[m].arm[1]=1;			// 数
									unit[m].arm[4]=1;			// 数 全容量
									}
								}
							}
						}
					break;


				case VK_F9:

#if DBG_MODE
if( map_edit==0 )
	{
	unit[the_slct_unit].hp[0]=0;
	}
#endif
					if( map_edit!=0 && mode==GameMode.Battle )
						{
						if(your_side==Side.Japan)
							your_side=Side.UnitedStates;
						else
							your_side=Side.Japan;
						SoundPlayEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
						}
					break;

				case VK_F10:
					break;

				case VK_F11:
					if( map_edit!=0 && mode==GameMode.Battle )
						{
						rein[(int)your_side]=rein[(int)your_side]++;
						rein[(int)your_side]=(byte)(rein[(int)your_side]%4);
						SoundPlayEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
						}
					break;



				case 0x31:	// 1
					// ユニットを回転させます。
					if( map_edit!=0 && mode==GameMode.Battle && unit[the_slct_unit].used!=0  && unit[the_slct_unit].kind>=UnitKind.Battleship && unit[the_slct_unit].kind<=UnitKind.Transport && !(unit[the_slct_unit].ctgry==UnitCategory.Plane && unit[the_slct_unit].info[0]==PARKING ))
						{
						unit[the_slct_unit].drctn= (int)(unit[the_slct_unit].drctn+45.0)%360 ;

						SoundPlayEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
						}

					break;


				case 0x32:	// 2
					// 航空機の武装を変えます。
					if( map_edit!=0 && mode==GameMode.Battle && unit[the_slct_unit].used!=0  )
						{
						if( unit[the_slct_unit].kind==UnitKind.Attacker )
							{
							// 攻撃機の場合。
							switch( unit[the_slct_unit].arm[0] )
								{
								case NTG:
									unit[the_slct_unit].arm[0]=BOM;				// 武装品種
									unit[the_slct_unit].arm[1]=unit[the_slct_unit].arm[4];	// 数
									break;
								case BOM:
									unit[the_slct_unit].arm[0]=TPD;				// 武装品種
									unit[the_slct_unit].arm[1]=unit[the_slct_unit].arm[4];	// 数
									break;
								case TPD:
									unit[the_slct_unit].arm[0]=NTG;				// 武装品種
									unit[the_slct_unit].arm[1]=0;	// 数
									break;
								}
							SoundPlayEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
							}
						else if( unit[the_slct_unit].kind==UnitKind.Bomber )
							{
							// 爆撃機の場合。
							switch( unit[the_slct_unit].arm[0] )
								{
								case NTG:
									unit[the_slct_unit].arm[0]=BOM;				// 武装品種
									unit[the_slct_unit].arm[1]=unit[the_slct_unit].arm[4];	// 数
									break;
								case BOM:
									unit[the_slct_unit].arm[0]=TPD;				// 武装品種
									unit[the_slct_unit].arm[1]=1;	// 数
									break;
								case TPD:
									unit[the_slct_unit].arm[0]=NTG;				// 武装品種
									unit[the_slct_unit].arm[1]=0;	// 数
									break;
								}
							SoundPlayEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
							}
						else if( (unit[the_slct_unit].kind>=UnitKind.Battleship && unit[the_slct_unit].kind<=UnitKind.LightCarrier) || unit[the_slct_unit].kind==UnitKind.Transport )
							{

							if(unit[the_slct_unit].gas[0]==-1)
								{
								unit[the_slct_unit].gas[0]=100;
								unit[the_slct_unit].spry=0;
								}
							else
								{
								unit[the_slct_unit].gas[0]=-1;
								unit[the_slct_unit].spry=1;
								}

							}
						}
					break;




				case 0x33:	// 3
					// ＨＰを増やす。
					if( map_edit!=0 && mode==GameMode.Battle && unit[the_slct_unit].used!=0 && !(unit[the_slct_unit].ctgry==UnitCategory.Plane && unit[the_slct_unit].info[0]==PARKING ) )
						{
						if( unit[the_slct_unit].hp[0] < unit[the_slct_unit].hp[1] )
							{
							unit[the_slct_unit].hp[0]++;
							SoundPlayEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
							}
						}
					break;


				case 0x34:	// 4
					// ＨＰを増やす。
					if( map_edit!=0 && mode==GameMode.Battle && unit[the_slct_unit].used!=0 && !(unit[the_slct_unit].ctgry==UnitCategory.Plane && unit[the_slct_unit].info[0]==PARKING ))
						{
						unit[the_slct_unit].hp[0]--;
						SoundPlayEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
						}
					break;



				case 0x35:	// 5
					// ガスをふやす
					if( map_edit!=0 && mode==GameMode.Battle && unit[the_slct_unit].used!=0 && unit[the_slct_unit].kind>=UnitKind.Battleship && unit[the_slct_unit].kind<=UnitKind.Transport && !(unit[the_slct_unit].ctgry==UnitCategory.Plane && unit[the_slct_unit].info[0]==PARKING ))
						{
						if( unit[the_slct_unit].gas[0] < 100 )
							{
							unit[the_slct_unit].gas[0]++;
							SoundPlayEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
							}
						}
					break;

				case 0x36:	// 6
					// ガスをへらす
					if( map_edit!=0 && mode==GameMode.Battle && unit[the_slct_unit].used!=0 && unit[the_slct_unit].kind>=UnitKind.Battleship && unit[the_slct_unit].kind<=UnitKind.Transport && !(unit[the_slct_unit].ctgry==UnitCategory.Plane && unit[the_slct_unit].info[0]==PARKING ))
						{
						if( unit[the_slct_unit].gas[0]!=0  )
							{
							unit[the_slct_unit].gas[0]--;
							SoundPlayEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
							}
						}
					break;


				case 0x37:	// 7
					// 弾数をふやす
					if( map_edit!=0 && mode==GameMode.Battle && unit[the_slct_unit].used!=0 && unit[the_slct_unit].kind>=UnitKind.Battleship && unit[the_slct_unit].kind<=UnitKind.Transport && unit[the_slct_unit].arm[0]!=NTG  && !(unit[the_slct_unit].ctgry==UnitCategory.Plane && unit[the_slct_unit].info[0]==PARKING ))
						{
						if( unit[the_slct_unit].arm[1]<unit[the_slct_unit].arm[4]  )
							{
							unit[the_slct_unit].arm[1]++;
							SoundPlayEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
							}
						}
					break;

				case 0x38:	// 8
					// 弾数をへらす
					if( map_edit!=0 && mode==GameMode.Battle && unit[the_slct_unit].used!=0 && unit[the_slct_unit].kind>=UnitKind.Battleship && unit[the_slct_unit].kind<=UnitKind.Transport && unit[the_slct_unit].arm[0]!=NTG  && !(unit[the_slct_unit].ctgry==UnitCategory.Plane && unit[the_slct_unit].info[0]==PARKING ))
						{
						if( unit[the_slct_unit].arm[1]!=0  )
							{
							unit[the_slct_unit].arm[1]--;
							SoundPlayEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
							}
						}
					break;



				case VK_ESCAPE:
				case VK_F12:
					PostMessage(hWnd,WM_CLOSE,0,0);
					break;
				default:
					break;
				}
			break;


		case WM_COMMAND:
/*
			switch(LOWORD(wParam))
				{
				case FILE_MENU_OPEN:
					break;
				case FILE_MENU_EXIT:
					DestroyWindow(hWnd);
					break;
				default:
					break;
				}
*/
			break;

		case WM_DESTROY:
			EndApp();
			PostQuitMessage(0);
			break;

		default:
			return DefWindowProc(hWnd,msg,wParam,lParam);
		}

	return unchecked((nint)0L);
	}
}
