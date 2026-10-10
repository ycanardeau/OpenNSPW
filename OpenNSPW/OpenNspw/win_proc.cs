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

// Port of win_proc.cpp.

namespace OpenNspw;

public unsafe partial class Nspw
{

//============================================================================
//失ったオブジェクトを再読み込みする
//----------------------------------------------------------------------------
[Original("restoreAll")]
public void	RestoreSurfaces()
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

	// クリッパー
	RELEASE(ref lpDDclip);
	if(IsFullscreen!=0)
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
	MakeTerrainSurface();

	}

[Original("load_user_map")]
public void	LoadUserMap()
	{
	Array2<byte> bf = default;
	HANDLE	hFile;
	short	m,n,f,i;
	Array256<Array256<ushort>> szBuf = default;					// マップ

	hFile=CreateFile( UserScenarioFileName /*"Map\\user_map.dat"*/, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
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
				MapTiles[m][n]=szBuf[m][n];

		ReadFile( hFile, ref Units, (uint)(sizeof(Array256<Unit>)), &dwActBytes, null );
		ReadFile( hFile, ref Reinforcements, (uint)(sizeof(Array3<byte>)), &dwActBytes, null );

		ReadFile( hFile, ref IsDecisionEnabled,  sizeof(byte) , &dwActBytes, null );
		ReadFile( hFile, ref ArrivalControl, sizeof(byte), &dwActBytes, null );

		ReadFile( hFile, ref SupplyRates, (uint)(sizeof(Array2<short>)), &dwActBytes, null );
		ReadFile( hFile, ref InitialSupplyPoints, (uint)(sizeof(Array2<short>)), &dwActBytes, null );

		ReadFile( hFile, ref SwapTime, sizeof(short), &dwActBytes, null );
		ReadFile( hFile, ref SwapRule, sizeof(short), &dwActBytes, null );

		CloseHandle(hFile);
		}

	}

[Original("load_it2")]
public void	LoadScenarioFile2(string str)
	{
	Array20<byte> bf = default;
	HANDLE	hFile;
	short	m,n,f,i;
	Array256<Array256<ushort>> szBuf = default;					// マップ

	if( strcmp ( str,"Map\\South_pacific.dat")==0 )
		CurrentMap=0;
	else if( strcmp ( str,"Map\\Middle_pacific.dat")==0 )
		CurrentMap=1;
	else
		CurrentMap=2;

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
				MapTiles[m][n]=szBuf[m][n];

		CloseHandle(hFile);
		}

	}

[Original("load_it3")]
public void	LoadScenarioFile3()
	{
	Array2<byte> bf = default;
	HANDLE	hFile;
	short	m,n,f,i;
	Array256<Array256<ushort>> szBuf = default;					// マップ

	hFile=CreateFile( UserScenarioFileName /*"Map\\user_map.dat"*/, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
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
				MapTiles[m][n]=szBuf[m][n];

		ReadFile( hFile, ref Units, (uint)(sizeof(Array256<Unit>)), &dwActBytes, null );
		ReadFile( hFile, ref Reinforcements, (uint)(sizeof(Array3<byte>)), &dwActBytes, null );

		CloseHandle(hFile);
		}

	}

[Original("save_user_map")]
public void	SaveUserMap()
	{
	Array2<byte> bf = default;
	HANDLE	hFile;
	short	m,n,f,i;
	uint	dwActBytes;
	Array256<Array256<ushort>> szBuf = default;					// マップ
	short		data;

	hFile=CreateFile( UserScenarioFileName/*"Map\\user_map.dat"*/, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ,
								null, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, null);

	if( hFile != INVALID_HANDLE_VALUE )
		{
		//unsigned short		cmbt_map[256][256];					// マップ

		SetFilePointer( hFile,0,0,FILE_BEGIN);		// 先頭へ

		// マップを記録
		// データ を バッファへ
		for(m=0;m<=255;m++)
			for(n=0;n<=255;n++)
				szBuf[m][n]=MapTiles[m][n];

		WriteFile(hFile, ref szBuf,(uint)(sizeof(Array256<Array256<ushort>>)),&dwActBytes,null);	// 書き込み

		//　ユニット、その他を記録
		// 書き込み
		WriteFile(hFile, ref Units,(uint)(sizeof(Array256<Unit>)),&dwActBytes,null);
		WriteFile(hFile, ref Reinforcements,(uint)(sizeof(Array3<byte>)),&dwActBytes,null);

		WriteFile(hFile, ref IsDecisionEnabled,sizeof(byte),&dwActBytes,null);
		WriteFile(hFile, ref ArrivalControl,sizeof(byte),&dwActBytes,null);

		WriteFile(hFile, ref SupplyRates,(uint)(sizeof(Array2<short>)),&dwActBytes,null);
		WriteFile(hFile, ref InitialSupplyPoints,(uint)(sizeof(Array2<short>)),&dwActBytes,null);

		WriteFile(hFile, ref SwapTime,sizeof(short),&dwActBytes,null);
		WriteFile(hFile, ref SwapRule,sizeof(short),&dwActBytes,null);

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
	DXUtil_WriteStringRegKey( hDPlaySampleRegKey, TEXT("Player Name"), LocalPlayerName );
	DXUtil_WriteStringRegKey( hDPlaySampleRegKey, TEXT("Preferred Provider"), PreferredProvider );
	DXUtil_WriteStringRegKey( hDPlaySampleRegKey, TEXT("Remote Hostname"), RemoteHostName );

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
[Original("init_apl_reg")]
public void	InitializeRegistry()
	{

	Tick=0;

	/*** フォント設定 ***/
	if (gameFont_1)
		DeleteObject(gameFont_1);
	gameFont_1=CreateFont(16,0,0,0,0,FALSE,FALSE,FALSE,SHIFTJIS_CHARSET,OUT_DEFAULT_PRECIS,CLIP_DEFAULT_PRECIS,DEFAULT_QUALITY,DEFAULT_PITCH,null); // フォントオブジェクト

	if (gameFont_2)
		DeleteObject(gameFont_2);
	gameFont_2=CreateFont(20,0,0,0,0,FALSE,FALSE,FALSE,SHIFTJIS_CHARSET,OUT_DEFAULT_PRECIS,CLIP_DEFAULT_PRECIS,DEFAULT_QUALITY,DEFAULT_PITCH,null); // フォントオブジェクト

	LeftButton=0;	RightButton=0;

	UserScenarioFileName[0] = unchecked((byte)'\0');

	}

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
			UserScenarioFileName[0] = unchecked((byte)'\0');
			break;

		case WM_COMMAND:
			switch( LOWORD(wParam) )
				{
				case IDC_LIST:
					if( HIWORD( wParam )==LBN_SELCHANGE )
						{
						DlgDirSelectEx( hWnd, UserScenarioFileName, sizeof( Array260<byte> ), IDC_LIST );
						SetDlgItemText( hWnd, IDC_EDIT, UserScenarioFileName );
						}
					break;

				case IDOK:
//					PostQuitMessage( 0 );		// これでWM_QUITがでるので接続ダイアログループからぬける
					GetDlgItemText( hWnd, IDC_EDIT, UserScenarioFileName, MAX_PATH );

					if( IsEditingMap!=0 && Mode==GameMode.Battle && UserScenarioFileName[0]!='\0' )
						{
						// なんかユーザーファイルが選ばれた。

int	len;
						len=wsprintf( temp_buf, "%s", UserScenarioFileName );

						if( !(UserScenarioFileName[len-4]=='.' && UserScenarioFileName[len-3]=='d' && UserScenarioFileName[len-2]=='a' && UserScenarioFileName[len-1]=='t') )
							wsprintf( temp_buf, "%s.dat", UserScenarioFileName );

						wsprintf( UserScenarioFileName, "Scenario\\%s", temp_buf );

						SaveUserMap();
						}

					DestroyWindow(hWnd);
					break;

				case IDCANCEL:
//					PostQuitMessage( 0 );		// これでWM_QUITがでるので接続ダイアログループからぬける
					UserScenarioFileName[0] = unchecked((byte)'\0');
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
			UserScenarioFileName[0] = unchecked((byte)'\0');
			break;

		case WM_COMMAND:
			switch(wParam)
				{
				case IDOK:
					DlgDirSelectEx( hWnd, UserScenarioFileName, sizeof( Array260<byte> ), IDC_LIST );

					if( IsEditingMap!=0 && Mode==GameMode.Battle && UserScenarioFileName[0]!='\0' )
						{
						wsprintf( temp_buf, "%s", UserScenarioFileName );
						wsprintf( UserScenarioFileName, "Scenario\\%s", temp_buf );

						LoadUserMap();
						MakeTerrainSurface();
						}
					else if( Mode==GameMode.GameSetting && UserScenarioFileName[0]!='\0' )
						{
						wsprintf( temp_buf, "%s", UserScenarioFileName );
						wsprintf( UserScenarioFileName, "Scenario\\%s", temp_buf );

						if( IsEditingMap==0 )
							{
							// なんかユーザーファイルが選ばれた。
							dp_data_20.dwType = MessageType.UserScenarioFileName;
							wsprintf( dp_data_20.friend_chat, "%s",UserScenarioFileName );
							bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_20));
							bufferDesc.pBufferData  = (byte*)&dp_data_20;
							g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );
							}

						LoadScenarioData();

						// ホストの選択状態をゲストにセンドします。
						dp_data_1.dwType = MessageType.LeaveGameSetting;
						dp_data_1.data[0] = (short)HostSide;
						dp_data_1.data[1] = ScenarioNumber;

						dp_data_1.data[2] = SupplyRates[0];
						dp_data_1.data[3] = SupplyRates[1];

						dp_data_1.data[4] = IsDecisionEnabled;

						dp_data_1.data[5] = InitialSupplyPoints[0];
						dp_data_1.data[6] = InitialSupplyPoints[1];

						dp_data_1.data[7] = ArrivalControl;

						dp_data_1.data[8] = SwapTime;
						dp_data_1.data[9] = SwapRule;

						bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_1));
						bufferDesc.pBufferData  = (byte*) &dp_data_1;
						g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1,	0, null, ref hAsync, MUST_SEND );

						Mode=GameMode.ConfigSetting;
						}

					DestroyWindow(hWnd);
//					PostQuitMessage( 0 );		// これでWM_QUITがでるので接続ダイアログループからぬける
					break;

				case IDCANCEL:
					UserScenarioFileName[0] = unchecked((byte)'\0');
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

			if( DialogAnswer==MessageType.GoToGameSetting )
				SetWindowText(hWnd,"Exit Without Saving?");
			else
				SetWindowText(hWnd,"Resume-save and Exit?");

			break;

		case WM_COMMAND:
			switch(wParam)
				{
				case IDOK:

					BufferedSystemOrders[1]=(byte)DialogAnswer;
					CanOrder=0;
					HasOrdered=1;
					PlaySoundEffect( 0, CLICK2 ,(double)(MAP_RIGHT+1), 0);

					DestroyWindow(hWnd);
					g_hDlg=null;
//					PostQuitMessage( 0 );		// これでWM_QUITがでるので接続ダイアログループからぬける
					break;

				case IDCANCEL:
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
[Original("my_dlg_wait")]
public void	WaitForDialog()
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

public nint ChatDlgProc(HWND hWnd,uint msg,nint wParam,nint lParam)
	{
	_DP_DATA_20	dp_data_20=default;
	int			m;

	switch(msg)
		{
		case WM_INITDIALOG:
			IsTypingChat=1;
			break;
		case WM_COMMAND:
			switch(wParam)
				{
				case IDOK:

					GetDlgItemText( hWnd, IDC_EDIT1, MyChat, MAX_PATH );
					MyChatDisplayTime=unchecked((byte)CHAT_DSP_TIME);

				// なんか入力があったならセンドする
					dp_data_20.dwType = MessageType.Chat;
					for(m=0;m<128;m++)
						{
						dp_data_20.friend_chat[m]=MyChat[m];
						}

					bufferDesc.dwBufferSize = (uint)(sizeof(_DP_DATA_20));
					bufferDesc.pBufferData  = (byte*) &dp_data_20;
					g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1, 0, null, ref hAsync, MUST_SEND );

					DestroyWindow(hwndChatDlg);
					hwndChatDlg=null;
					IsTypingChat=0;
					break;

				case IDCANCEL:
					DestroyWindow(hwndChatDlg);
					hwndChatDlg=null;
					IsTypingChat=0;
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
					DestroyWindow(hwndChatDlg);
					hwndChatDlg=null;
					IsTypingChat=0;
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
				IsAppActive=0;
				}
			else
				{
				pDIDevice.Acquire();
				pDIDeviceMouse.Acquire();
				IsAppActive=1;
				}

			break;

		case WM_ACTIVATEAPP:	//ウインドウが選択された時
			if(wParam == WA_INACTIVE)
				IsAppActive=0;
			else
				IsAppActive=1;
			break;

		case WM_SIZE:		// ウインドウ起動時にもここにくるようだ。

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
					if( IsEditingMap!=0 && Mode==GameMode.Battle )
						{
						/*g_hDlg =*/ CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_FILE_CONT), hwndApp, (DLGPROC)IDD_FILE_SAVE_Proc );

						PlaySoundEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
						}

					break;

				case VK_F2:
					if( IsEditingMap!=0 && Mode==GameMode.Battle )
						{
						Units[SelectedUnit].Side=0;
						SelectedUnit=0;
						CombatMenuKind=0;
						CombatMenuSelection=CombatMenuItem.None;
						ClearSelection2(1);

						// シナリオ選択がユーザーシナリオならファイル選択します。
						/*g_hDlg =*/ CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_FILE_CONT), hwndApp, (DLGPROC)IDD_FILE_LOAD_Proc );

						PlaySoundEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
						}
					break;

				case VK_F3:
					if( IsEditingMap!=0 && Mode==GameMode.Battle )
						{
						PlaySoundEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
						MakeTerrainSurface();
						}
					else
						DebugMenu--;

					break;

				case VK_F4:
					DebugMenu++;
					break;

				case VK_F5:
					if( IsEditingMap!=0 && EditorTarget>1 && Mode==GameMode.Battle )
						{
						EditorTarget--;
						PlaySoundEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
						}

#if DBG_MODE
if( IsEditingMap==0 )
	{
	if(LocalSide==Side.Japan)
		LocalSide=Side.UnitedStates;
	else
		LocalSide=Side.Japan;
	}
#endif

					break;

				case VK_F6:
					if( IsEditingMap!=0 && Mode==GameMode.Battle )
						{
						if( EditorTarget<24 )
							{
							EditorTarget++;
							PlaySoundEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
							}
						}

#if DBG_MODE
if( IsEditingMap==0 )
	{
	Mode=GameMode.GameSetting;
	LoadScenarioData();

	Mode=GameMode.Battle;
	InitializeGame();
	}
#endif
					break;

				case VK_F7:

					if( IsEditingMap!=0 && Mode==GameMode.Battle )
						{
						if( Units[SelectedUnit].IsUsed )
							{
							// 空母か空港なら搭載ユニットも消す
							if( Units[SelectedUnit].Kind==UnitKind.Carrier || Units[SelectedUnit].Kind==UnitKind.LightCarrier || Units[SelectedUnit].Kind==UnitKind.AirBase )
								{
								for( i=1;i<=MaxUnitId;i++)
									{
									ref var unit = ref Units[i];
									if( unit.IsUsed && unit.Category==UnitCategory.Plane && unit.PlaneState==UnitState.Parked && unit.Carrier==SelectedUnit)
										{
										unit.Side=0;
										}
									}
								}

							// パーキング中の航空機なら駐機数を減らします。
							if( Units[SelectedUnit].Category==UnitCategory.Plane && Units[SelectedUnit].PlaneState==UnitState.Parked )
								{
								Units[Units[SelectedUnit].Carrier].info[1]--;	// 現在格納数
								}

							Units[SelectedUnit].Side=0;

							SelectedUnit=0;
							CombatMenuKind=0;
							CombatMenuSelection=CombatMenuItem.None;
							ClearSelection2(1);

							PlaySoundEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
							}

						}
					break;

				case VK_F8:
					if( IsEditingMap!=0 && Mode==GameMode.Battle )
						{
						if( UnitInfoPanel[1]!=0 && ( Units[PreviousSelectedUnit].Kind==UnitKind.Carrier || Units[PreviousSelectedUnit].Kind==UnitKind.LightCarrier || Units[PreviousSelectedUnit].Kind==UnitKind.AirBase )  && ((UnitKind)EditorKind==UnitKind.Fighter || (UnitKind)EditorKind==UnitKind.Attacker || (UnitKind)EditorKind==UnitKind.Bomber ) )
							{
							// 駐機場への航空機の配置
							if(
								Units[PreviousSelectedUnit].Side==LocalSide
								)
								m=AddPlane(LocalSide,(UnitKind)EditorKind,EditorVariant,PreviousSelectedUnit,1,FireKind.Unarmed);

							if(m!=0)
								PlaySoundEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
							}
						else if( UnitInfoPanel[1]==0 && CursorPosition.x<=CMBT_WIDTH && CursorPosition.y<=CMBT_HEIGHT )
							{
							// 艦船および、陸上施設

							wrk_x2=(CameraPosition.X+CursorPosition.x)/80;
							if(wrk_x2<0)
								wrk_x2=0-wrk_x2;
							wrk_x=(int)wrk_x2;
							if(  (wrk_x2-wrk_x)>=0.5  )
								wrk_x+=1;
							if(((CameraPosition.X+CursorPosition.x)/80)<0)
								wrk_x=0-wrk_x;
							wrk_x*=80;

							wrk_y2=(CameraPosition.Y-CursorPosition.y)/80;
							if(wrk_y2<0)
								wrk_y2=0-wrk_y2;
							wrk_y=(int)wrk_y2;
							if(  (wrk_y2-wrk_y)>=0.5  )
								wrk_y+=1;
							if(((CameraPosition.Y-CursorPosition.y)/80)<0)
								wrk_y=0-wrk_y;
							wrk_y*=80;

							m=0;
							for(i=1;i<=MaxUnitId;i++)
								{
								ref var unit = ref Units[i];
								if( unit.IsUsed && unit.Position.X==wrk_x && unit.Position.Y==wrk_y )
									{
									m++;
									break;
									}
								}

							if( m==0 )
								{
								m=AddUnit2( LocalSide, (UnitKind)EditorKind, EditorVariant,  wrk_x,  wrk_y, (double)(0+(LocalSide==Side.UnitedStates ? 1 : 0)*180 ) );

								if(m!=0)
									{
									PlaySoundEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
									}
								if((UnitKind)EditorKind==UnitKind.Transport)
									{
									Units[m].Weapon=(FireKind)EditorVariant;		// 武装品種
									Units[m].Ammo=1;			// 数
									Units[m].MaxAmmo=1;			// 数 全容量
									}
								}
							}
						}
					break;

				case VK_F9:

#if DBG_MODE
if( IsEditingMap==0 )
	{
	Units[SelectedUnit].Hp=0;
	}
#endif
					if( IsEditingMap!=0 && Mode==GameMode.Battle )
						{
						if(LocalSide==Side.Japan)
							LocalSide=Side.UnitedStates;
						else
							LocalSide=Side.Japan;
						PlaySoundEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
						}
					break;

				case VK_F10:
					break;

				case VK_F11:
					if( IsEditingMap!=0 && Mode==GameMode.Battle )
						{
						Reinforcements[(int)LocalSide]=Reinforcements[(int)LocalSide]++;
						Reinforcements[(int)LocalSide]=(byte)(Reinforcements[(int)LocalSide]%4);
						PlaySoundEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
						}
					break;

				case 0x31:	// 1
					// ユニットを回転させます。
					if( IsEditingMap!=0 && Mode==GameMode.Battle && Units[SelectedUnit].IsUsed  && Units[SelectedUnit].Kind>=UnitKind.Battleship && Units[SelectedUnit].Kind<=UnitKind.Transport && !(Units[SelectedUnit].Category==UnitCategory.Plane && Units[SelectedUnit].PlaneState==UnitState.Parked ))
						{
						Units[SelectedUnit].Direction= (int)(Units[SelectedUnit].Direction+45.0)%360 ;

						PlaySoundEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
						}

					break;

				case 0x32:	// 2
					// 航空機の武装を変えます。
					if( IsEditingMap!=0 && Mode==GameMode.Battle && Units[SelectedUnit].IsUsed  )
						{
						if( Units[SelectedUnit].Kind==UnitKind.Attacker )
							{
							// 攻撃機の場合。
							switch( Units[SelectedUnit].Weapon )
								{
								case FireKind.Unarmed:
									Units[SelectedUnit].Weapon=FireKind.Bomb;				// 武装品種
									Units[SelectedUnit].Ammo=Units[SelectedUnit].MaxAmmo;	// 数
									break;
								case FireKind.Bomb:
									Units[SelectedUnit].Weapon=FireKind.Torpedo;				// 武装品種
									Units[SelectedUnit].Ammo=Units[SelectedUnit].MaxAmmo;	// 数
									break;
								case FireKind.Torpedo:
									Units[SelectedUnit].Weapon=FireKind.Unarmed;				// 武装品種
									Units[SelectedUnit].Ammo=0;	// 数
									break;
								}
							PlaySoundEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
							}
						else if( Units[SelectedUnit].Kind==UnitKind.Bomber )
							{
							// 爆撃機の場合。
							switch( Units[SelectedUnit].Weapon )
								{
								case FireKind.Unarmed:
									Units[SelectedUnit].Weapon=FireKind.Bomb;				// 武装品種
									Units[SelectedUnit].Ammo=Units[SelectedUnit].MaxAmmo;	// 数
									break;
								case FireKind.Bomb:
									Units[SelectedUnit].Weapon=FireKind.Torpedo;				// 武装品種
									Units[SelectedUnit].Ammo=1;	// 数
									break;
								case FireKind.Torpedo:
									Units[SelectedUnit].Weapon=FireKind.Unarmed;				// 武装品種
									Units[SelectedUnit].Ammo=0;	// 数
									break;
								}
							PlaySoundEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
							}
						else if( (Units[SelectedUnit].Kind>=UnitKind.Battleship && Units[SelectedUnit].Kind<=UnitKind.LightCarrier) || Units[SelectedUnit].Kind==UnitKind.Transport )
							{

							if(Units[SelectedUnit].Fuel==-1)
								{
								Units[SelectedUnit].Fuel=100;
								Units[SelectedUnit].SupplyTime=0;
								}
							else
								{
								Units[SelectedUnit].Fuel=-1;
								Units[SelectedUnit].SupplyTime=1;
								}

							}
						}
					break;

				case 0x33:	// 3
					// ＨＰを増やす。
					if( IsEditingMap!=0 && Mode==GameMode.Battle && Units[SelectedUnit].IsUsed && !(Units[SelectedUnit].Category==UnitCategory.Plane && Units[SelectedUnit].PlaneState==UnitState.Parked ) )
						{
						if( Units[SelectedUnit].Hp < Units[SelectedUnit].MaxHp )
							{
							Units[SelectedUnit].Hp++;
							PlaySoundEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
							}
						}
					break;

				case 0x34:	// 4
					// ＨＰを増やす。
					if( IsEditingMap!=0 && Mode==GameMode.Battle && Units[SelectedUnit].IsUsed && !(Units[SelectedUnit].Category==UnitCategory.Plane && Units[SelectedUnit].PlaneState==UnitState.Parked ))
						{
						Units[SelectedUnit].Hp--;
						PlaySoundEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
						}
					break;

				case 0x35:	// 5
					// ガスをふやす
					if( IsEditingMap!=0 && Mode==GameMode.Battle && Units[SelectedUnit].IsUsed && Units[SelectedUnit].Kind>=UnitKind.Battleship && Units[SelectedUnit].Kind<=UnitKind.Transport && !(Units[SelectedUnit].Category==UnitCategory.Plane && Units[SelectedUnit].PlaneState==UnitState.Parked ))
						{
						if( Units[SelectedUnit].Fuel < 100 )
							{
							Units[SelectedUnit].Fuel++;
							PlaySoundEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
							}
						}
					break;

				case 0x36:	// 6
					// ガスをへらす
					if( IsEditingMap!=0 && Mode==GameMode.Battle && Units[SelectedUnit].IsUsed && Units[SelectedUnit].Kind>=UnitKind.Battleship && Units[SelectedUnit].Kind<=UnitKind.Transport && !(Units[SelectedUnit].Category==UnitCategory.Plane && Units[SelectedUnit].PlaneState==UnitState.Parked ))
						{
						if( Units[SelectedUnit].Fuel!=0  )
							{
							Units[SelectedUnit].Fuel--;
							PlaySoundEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
							}
						}
					break;

				case 0x37:	// 7
					// 弾数をふやす
					if( IsEditingMap!=0 && Mode==GameMode.Battle && Units[SelectedUnit].IsUsed && Units[SelectedUnit].Kind>=UnitKind.Battleship && Units[SelectedUnit].Kind<=UnitKind.Transport && Units[SelectedUnit].Weapon!=FireKind.Unarmed  && !(Units[SelectedUnit].Category==UnitCategory.Plane && Units[SelectedUnit].PlaneState==UnitState.Parked ))
						{
						if( Units[SelectedUnit].Ammo<Units[SelectedUnit].MaxAmmo  )
							{
							Units[SelectedUnit].Ammo++;
							PlaySoundEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
							}
						}
					break;

				case 0x38:	// 8
					// 弾数をへらす
					if( IsEditingMap!=0 && Mode==GameMode.Battle && Units[SelectedUnit].IsUsed && Units[SelectedUnit].Kind>=UnitKind.Battleship && Units[SelectedUnit].Kind<=UnitKind.Transport && Units[SelectedUnit].Weapon!=FireKind.Unarmed  && !(Units[SelectedUnit].Category==UnitCategory.Plane && Units[SelectedUnit].PlaneState==UnitState.Parked ))
						{
						if( Units[SelectedUnit].Ammo!=0  )
							{
							Units[SelectedUnit].Ammo--;
							PlaySoundEffect( 0, CLICK1 ,(double)(MAP_RIGHT+1), 0);
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
