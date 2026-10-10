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

// Port of dplay.cpp. The message buffers are unsafe pointers, as in the original (see dplay8.cs).

namespace OpenNspw;

public unsafe partial class Nspw
{

/*
typedef struct _GENERICMSG
	{
    BYTE        byType;
	} GENERICMSG, *LPGENERICMSG;
*/

//-----------------------------------------------------------------------------
// Name: WaveToAllPlayers()
// Desc: Send a app-defined "wave" DirectPlay message to all connected players
//-----------------------------------------------------------------------------
public void WaveToAllPlayers()
	{

	if( g_pThreadPool==null || g_pDP==null || ActivePlayerCount!=0 )
		return;

	GENERICMSG msgWave;
	msgWave.dwType = MSG_TST;

	bufferDesc.dwBufferSize = (uint)sizeof(GENERICMSG);
	bufferDesc.pBufferData  = (byte*) &msgWave;

	// Group sends with valid parameters will always succeed unless no player
	// received the message, so ignore any errors and just let the message
	// handler deal with things like players going away.
//	g_pDP->SendTo( /*DPNID_ALL_PLAYERS_GROUP*/g_dpnidRivalPlayer, &bufferDesc, 1,

/**
    // This is called by the dialog UI thread.  This will send a message to all
    // the players or inform the player that there is no one around.
    if( g_dwNumberOfActivePlayers <= 1 )
    {
//                  TEXT("AddressOverride"), MB_OK );
    }
    else
    {
        // Send a message to all of the players
        GENERICMSG msgWave;
        msgWave.dwType = GAME_MSGID_WAVE;

        DPN_BUFFER_DESC bufferDesc;
        bufferDesc.dwBufferSize = (uint)sizeof(GENERICMSG);
        bufferDesc.pBufferData  = (byte*) &msgWave;

        // Group sends with valid parameters will always succeed unless no player
        // received the message, so ignore any errors and just let the message
        // handler deal with things like players going away.
        DPNHANDLE hAsync;
        g_pDP->SendTo( DPNID_ALL_PLAYERS_GROUP, &bufferDesc, 1,
                       0, NULL, &hAsync, DPNSEND_NOLOOPBACK | DPNSEND_GUARANTEED );
    }
**/
	return;
	}

//-----------------------------------------------------------------------------
// Name: GreetingDlgProc()
// Desc: Handles dialog messages
// 接続確認ダイアログのプロシージャ
//-----------------------------------------------------------------------------
public nint GreetingDlgProc( HWND hDlg, uint msg, nint wParam, nint lParam )
	{

    switch( msg )
		{
		case WM_INITDIALOG:
			{
         g_hDlg = hDlg;

         // Load and set the icon
         object? hIcon = LoadIcon( hInstApp, MAKEINTRESOURCE( IDI_ICON1 ) );
         SendMessage( hDlg, WM_SETICON, ICON_BIG,   hIcon );  // Set big icon
         SendMessage( hDlg, WM_SETICON, ICON_SMALL, hIcon );  // Set small icon

         if( IsHostPlayer!=0 )
             SetWindowText( hDlg, TEXT("Host Player") );
         else
             SetWindowText( hDlg, TEXT("Guest Player") );

         // Display local player's name
         SetDlgItemText( hDlg, IDC_PLAYER_NAME, LocalPlayerName );

			// ライバルプレイヤーネーム
			SetDlgItemText( hDlg, IDC_PLAYER_NAME2, RivalPlayerName );

			// 一応アップデートしときます。
         PostMessage( hDlg, WM_APP_UPDATE_STATS, 0, 0 );

			// 一応無効に
			EnableWindow( GetDlgItem( hDlg, IDC_START_GAME ), FALSE);

         break;
		  }

		case WM_APP_UPDATE_STATS:
			{
         // Update the number of players in the game

			// 現在のプレイヤー数をダイアログに書きます。

			// ライバルプレイヤーネーム
			SetDlgItemText( hDlg, IDC_PLAYER_NAME2, RivalPlayerName );

			// 参加人数によってゲームスタートボタンを有効か無効に。ホストのみ影響。
			if( IsHostPlayer!=0 )
				{
				if( ActivePlayerCount<2 )
					EnableWindow( GetDlgItem( hDlg, IDC_START_GAME ), FALSE);
				else
					EnableWindow( GetDlgItem( hDlg, IDC_START_GAME ), TRUE);
				}

			break;
		  }

		case WM_COMMAND:
			{
			switch( LOWORD(wParam) )
				{
				case IDC_START_GAME:

					if( IsHostPlayer!=0 )
						{
						// Send a message to all of the players
						GENERICMSG msgWave;
						msgWave.dwType = (MessageType)MSG_EXIT_WAITING;

						bufferDesc.dwBufferSize = (uint)sizeof(GENERICMSG);
						bufferDesc.pBufferData  = (byte*) &msgWave;

						// Group sends with valid parameters will always succeed unless no player
						// received the message, so ignore any errors and just let the message
						// handler deal with things like players going away.

//						g_pDP->SendTo( DPNID_ALL_PLAYERS_GROUP, &bufferDesc, 1,

						g_pDP.SendTo( g_dpnidRivalPlayer, ref bufferDesc, 1, 0, null, ref hAsync
													, MUST_SEND );
						}

					DestroyWindow(hDlg);
					g_hDlg=null;					// ダイアログ表示中かのフラグにもなる
					PostQuitMessage( 0 );
					return TRUE;

				case IDCANCEL:
					ActivePlayerCount=0;
					DestroyWindow(hDlg);
					g_hDlg=null;					// ダイアログ表示中かのフラグにもなる
					PostQuitMessage( 0 );
					return TRUE;
				}
			break;
			}
		}

	return FALSE; // Didn't handle message
	}

//-----------------------------------------------------------------------------
// Name: EnumAdapters()
// Desc: Fills the combobox with adapters for a specified SP
//-----------------------------------------------------------------------------
public int EnumAdapters( HWND hDlg, Guid? pSPGuid )
{
    DPN_SERVICE_PROVIDER_INFO[]? pdnSPInfo = null;
    Array260<byte>   strName = default;
    int hr;
    uint   dwItems = 0;
    uint   dwSize  = 0;
    int     nIndex;
    int     nAllAdaptersIndex = 0;

    SendDlgItemMessage( hDlg, IDC_ADAPTER_COMBO, CB_RESETCONTENT, 0, 0 );

    // Enumerate all adapters for the given service provider, and store them
    // in the listbox
    hr = g_pDP.EnumServiceProviders( pSPGuid, null, pdnSPInfo, ref dwSize,
                                      ref dwItems, 0 );
    if( hr != DPNERR_BUFFERTOOSMALL )
    {
        DXTRACE_ERR_MSGBOX( TEXT("EnumAdapters"), hr );
        goto LCleanReturn;
    }

    // Allocate space for adapter info
    pdnSPInfo = new DPN_SERVICE_PROVIDER_INFO[dwSize];
    if( null == pdnSPInfo )
    {
        hr = E_OUTOFMEMORY;
        DXTRACE_ERR_MSGBOX( TEXT("EnumAdapters"), hr );
        goto LCleanReturn;
    }

    // Enumerate adapters
    if( FAILED( hr = g_pDP.EnumServiceProviders( pSPGuid, null, pdnSPInfo,
                                                  ref dwSize, ref dwItems, 0 ) ) )
    {
        DXTRACE_ERR_MSGBOX( TEXT("EnumAdapters"), hr );
        goto LCleanReturn;
    }

    // Copy the pointer for enumeration
    int pdnSPInfoEnum;		// An index into pdnSPInfo, for the pointer
    pdnSPInfoEnum = 0;

    // For each detected adapter, add an item to the listbox
    uint i;
    for ( i = 0; i < dwItems; i++ )
    {
        DXUtil_ConvertWideStringToGenericCch( strName, pdnSPInfo[pdnSPInfoEnum].pwszName, MAX_PATH );

        // Found an adapter, so put it in the listbox
        nIndex = (int)SendDlgItemMessage( hDlg, IDC_ADAPTER_COMBO, CB_ADDSTRING,
                                          0, strName );

        // Store pointer to GUID in listbox
        Guid? pGuid = new Guid();
        if( null == pGuid )
        {
            hr = E_OUTOFMEMORY;
            DXTRACE_ERR_MSGBOX( TEXT("EnumAdapters"), hr );
            goto LCleanReturn;
        }

        // Fill in the GUID structure
        pGuid = pdnSPInfo[pdnSPInfoEnum].guid;

        SendDlgItemMessage( hDlg, IDC_ADAPTER_COMBO, CB_SETITEMDATA,
                            nIndex, pGuid );

        // Advance to the next adapter
        pdnSPInfoEnum++;
    }

    // Determine if this SP supports all-adapters, that is, not specifying
    // a device GUID and using all devices simultaneously.
    DPN_SP_CAPS dpnspCaps = new();
	dpnspCaps.dwSize = DPN_SP_CAPS.SIZE;
    hr = g_pDP.GetSPCaps(pSPGuid, dpnspCaps, 0);
    if ( FAILED( hr ) )
    {
        DXTRACE_ERR_MSGBOX( TEXT("GetSPCaps"), hr );
        goto LCleanReturn;
    }
    if ( (dpnspCaps.dwFlags & DPNSPCAPS_SUPPORTSALLADAPTERS)!=0 )
    {
        // Add an "All Adapters" special item to the listbox
        nIndex = (int)SendDlgItemMessage( hDlg, IDC_ADAPTER_COMBO, CB_ADDSTRING,
                                          0, TEXT("* All Adapters *") );

        nAllAdaptersIndex = nIndex;

        // Store pointer to a placeholder GUID in listbox
        Guid? pGuid = new Guid();
        if( null == pGuid )
        {
            hr = E_OUTOFMEMORY;
            DXTRACE_ERR_MSGBOX( TEXT("EnumAdapters"), hr );
            goto LCleanReturn;
        }

        // Fill the GUID structure with all 0s (i.e. GUID_NULL)
        pGuid = GUID_NULL;

        SendDlgItemMessage( hDlg, IDC_ADAPTER_COMBO, CB_SETITEMDATA,
                            nIndex, pGuid );
    }

    // Select the first item, or "All Adapters" if the SP supports it.
    SendDlgItemMessage( hDlg, IDC_ADAPTER_COMBO, CB_SETCURSEL, nAllAdaptersIndex, 0 );
    hr = S_OK;

LCleanReturn:
    SAFE_DELETE_ARRAY( ref pdnSPInfo );

    return hr;
}

//-----------------------------------------------------------------------------
// Name: EnumServiceProviders()
// Desc: Fills the combobox with service providers
//-----------------------------------------------------------------------------
public int EnumServiceProviders( HWND hDlg )
{
    DPN_SERVICE_PROVIDER_INFO[]? pdnSPInfo = null;
    int hr;
    uint   dwItems = 0;
    uint   dwSize  = 0;
    int     nIndex;

    // Enumerate all DirectPlay service providers, and store them in the listbox
    hr = g_pDP.EnumServiceProviders( null, null, pdnSPInfo, ref dwSize,
                                      ref dwItems, 0 );
    if( hr != DPNERR_BUFFERTOOSMALL )
    {
        DXTRACE_ERR_MSGBOX( TEXT("EnumServiceProviders"), hr );
        goto LCleanReturn;
    }

    // Allocate space for service provider info
    pdnSPInfo = new DPN_SERVICE_PROVIDER_INFO[dwSize];
    if( null == pdnSPInfo )
    {
        hr = E_OUTOFMEMORY;
        DXTRACE_ERR_MSGBOX( TEXT("EnumServiceProviders"), hr );
        goto LCleanReturn;
    }

    // Enumerate service providers
    if( FAILED( hr = g_pDP.EnumServiceProviders( null, null, pdnSPInfo,
                                                  ref dwSize, ref dwItems, 0 ) ) )
    {
        DXTRACE_ERR_MSGBOX( TEXT("EnumServiceProviders"), hr );
        goto LCleanReturn;
    }

    // Copy pointer for enumeration
    int pdnSPInfoEnum;		// An index into pdnSPInfo, for the pointer
    pdnSPInfoEnum = 0;

    // For each detected service provider, add an item to the list
    uint i;
    for ( i = 0; i < dwItems; i++ )
    {
        Array260<byte> strName = default;
        DXUtil_ConvertWideStringToGenericCch( strName, pdnSPInfo[pdnSPInfoEnum].pwszName, MAX_PATH );

        // Found a service provider, so put it in the listbox
        nIndex = (int)SendDlgItemMessage( hDlg, IDC_SP_COMBO, CB_ADDSTRING,
                                              0, strName );

        // Store pointer to GUID in listbox
        Guid? pGuid = new Guid();
        if( null == pGuid )
        {
            hr = E_OUTOFMEMORY;
            DXTRACE_ERR_MSGBOX( TEXT("EnumServiceProviders"), hr );
            goto LCleanReturn;
        }

        // Fill in the GUID structure
        pGuid = pdnSPInfo[pdnSPInfoEnum].guid;
        SendDlgItemMessage( hDlg, IDC_SP_COMBO, CB_SETITEMDATA,
                            nIndex, pGuid );

        // Advance to the next service provider
        pdnSPInfoEnum++;
    }

    // Try to select the default preferred provider
    nIndex = (int)SendDlgItemMessage( hDlg, IDC_SP_COMBO, CB_FINDSTRINGEXACT, -1,
                                      PreferredProvider );
    if( nIndex != LB_ERR )
        SendDlgItemMessage( hDlg, IDC_SP_COMBO, CB_SETCURSEL, nIndex, 0 );
    else
        SendDlgItemMessage( hDlg, IDC_SP_COMBO, CB_SETCURSEL, 0, 0 );

    hr = S_OK;

LCleanReturn:
    SAFE_DELETE_ARRAY( ref pdnSPInfo );

    return hr;
}

//-----------------------------------------------------------------------------
// Name: SetupAddressFields
// Desc: Based on the SP selected, update the address UI
// ホストか否か、ＳＰ変えたりするとホストアドレスとか、アダプタとかデフォ設定する。
//-----------------------------------------------------------------------------
public void SetupAddressFields( HWND hDlg )
{
    int nSPIndex = (int) SendDlgItemMessage( hDlg, IDC_SP_COMBO, CB_GETCURSEL, 0, 0 );
    if( nSPIndex == LB_ERR )
        return;
    Guid? pGuid = (Guid?) SendDlgItemMessage( hDlg, IDC_SP_COMBO, CB_GETITEMDATA, nSPIndex, 0 );
    if( pGuid == null )
        return;

    int bHosting = (int)IsDlgButtonChecked( hDlg, IDC_HOST_SESSION );

    if( pGuid == CLSID_DP8SP_TCPIP ||
        pGuid == CLSID_DP8SP_IPX )
    {
        Array40<byte> strPort = default;
        _itot( ADDRESSOVERRIDE_PORT, strPort, 10 );

        EnableWindow( GetDlgItem(hDlg, IDC_ADDRESS_LINE2), TRUE );
        SetDlgItemText( hDlg, IDC_ADDRESS_LINE2, strPort );
        EnableWindow( GetDlgItem(hDlg, IDC_ADDRESS_LINE2_TEXT), TRUE );
        SetDlgItemText( hDlg, IDC_ADDRESS_LINE2_TEXT, TEXT("Port") );

        if( bHosting!=0 )
        {
            EnableWindow( GetDlgItem(hDlg, IDC_ADDRESS_LINE1), FALSE );
            SetDlgItemText( hDlg, IDC_ADDRESS_LINE1, TEXT("") );
            EnableWindow( GetDlgItem(hDlg, IDC_ADDRESS_LINE1_TEXT), FALSE );
            SetDlgItemText( hDlg, IDC_ADDRESS_LINE1_TEXT, TEXT("") );
        }
        else
        {
            EnableWindow( GetDlgItem(hDlg, IDC_ADDRESS_LINE1), TRUE );
            SetDlgItemText( hDlg, IDC_ADDRESS_LINE1, TEXT("") );
            EnableWindow( GetDlgItem(hDlg, IDC_ADDRESS_LINE1_TEXT), TRUE );
            SetDlgItemText( hDlg, IDC_ADDRESS_LINE1_TEXT, TEXT("Address") );

            // TCP/IP only: As a convenience, we store the most recently used
            // remote IP in the registry, and should populate that field
            // with the stored value. Default is "localhost".
            if( pGuid == CLSID_DP8SP_TCPIP )
                SetDlgItemText( hDlg, IDC_ADDRESS_LINE1, RemoteHostName );

        }
    }
    else if( pGuid == CLSID_DP8SP_MODEM )
    {
        EnableWindow( GetDlgItem(hDlg, IDC_ADDRESS_LINE1), TRUE );
        SetDlgItemText( hDlg, IDC_ADDRESS_LINE1, TEXT("") );
        EnableWindow( GetDlgItem(hDlg, IDC_ADDRESS_LINE1_TEXT), TRUE );
        SetDlgItemText( hDlg, IDC_ADDRESS_LINE1_TEXT, TEXT("Phone Number:") );

        EnableWindow( GetDlgItem(hDlg, IDC_ADDRESS_LINE2), FALSE );
        SetDlgItemText( hDlg, IDC_ADDRESS_LINE2, TEXT("") );
        EnableWindow( GetDlgItem(hDlg, IDC_ADDRESS_LINE2_TEXT), FALSE );
        SetDlgItemText( hDlg, IDC_ADDRESS_LINE2_TEXT, TEXT("") );
    }
    else
    {
        // CLSID_DP8SP_SERIAL or unknown so disable all the address lines.
        // This sample does not support any other type of service provider.
        EnableWindow( GetDlgItem(hDlg, IDC_ADDRESS_LINE1), FALSE );
        SetDlgItemText( hDlg, IDC_ADDRESS_LINE1, TEXT("") );
        EnableWindow( GetDlgItem(hDlg, IDC_ADDRESS_LINE1_TEXT), FALSE );
        SetDlgItemText( hDlg, IDC_ADDRESS_LINE1_TEXT, TEXT("") );
        EnableWindow( GetDlgItem(hDlg, IDC_ADDRESS_LINE2), FALSE );
        SetDlgItemText( hDlg, IDC_ADDRESS_LINE2, TEXT("") );
        EnableWindow( GetDlgItem(hDlg, IDC_ADDRESS_LINE2_TEXT), FALSE );
        SetDlgItemText( hDlg, IDC_ADDRESS_LINE2_TEXT, TEXT("") );
    }
}

//-----------------------------------------------------------------------------
// Name: OnInitOverrideDialog
// Desc: Handler for dialog initialization
// 接続ダイアログの初期設定
//-----------------------------------------------------------------------------
public int OnInitOverrideDialog( HWND hDlg )
	{
	int hr;

	// Load and set the icon
	object? hIcon = LoadIcon( hInstApp, MAKEINTRESOURCE( IDI_ICON1 ) );
	SendMessage( hDlg, WM_SETICON, ICON_BIG,   hIcon );  // Set big icon
	SendMessage( hDlg, WM_SETICON, ICON_SMALL, hIcon );  // Set small icon

	CheckDlgButton( hDlg, IDC_HOST_SESSION, BST_CHECKED );

	// ローカルプレイヤーネーム
	SetDlgItemText( hDlg, IDC_PLAYER_NAME, LocalPlayerName );

	if( FAILED( hr = EnumServiceProviders( hDlg ) ) )
	return DXTRACE_ERR_MSGBOX( TEXT("EnumServiceProviders"), hr );

	SetupAddressFields( hDlg );

	int nSPIndex = (int) SendDlgItemMessage( hDlg, IDC_SP_COMBO, CB_GETCURSEL, 0, 0 );
	Guid? pSPGuid = (Guid?) SendDlgItemMessage( hDlg, IDC_SP_COMBO, CB_GETITEMDATA, nSPIndex, 0 );
	if( pSPGuid != null )
		{
		g_pCurSPGuid = pSPGuid;
		EnumAdapters( hDlg, pSPGuid );
		}

	ActivePlayerCount=0;
	sprintf(RivalPlayerName, " - - - " );

	return S_OK;
	}

//-----------------------------------------------------------------------------
// Name: OverrideDlgProc()
// Desc: Handles dialog messages
//
// 接続ダイアログのプロシージャ
//
// 接続のための設定とかの処理
//-----------------------------------------------------------------------------

//BOOL CALLBACK OverrideDlgProc(HWND hDlg, UINT msg, WPARAM wParam, LPARAM lParam)
public nint OverrideDlgProc( HWND hDlg, uint msg, nint wParam, nint lParam )
	{
	int hr;
	int nSPIndex;
	Guid? pSPGuid;

	int nIndex;

	switch( msg )
		{
		case WM_INITDIALOG:
			if( FAILED( hr = OnInitOverrideDialog( hDlg ) ) )
				{
             MessageBox( null, TEXT("Failed initializing dialog box. ") +
                         TEXT("The sample will now quit."),
                         AppName, MB_OK | MB_ICONERROR );
				DestroyWindow(hDlg);
            PostQuitMessage( 0 );
				}
         break;

		case WM_COMMAND:
			switch( LOWORD(wParam) )
				{
				case IDC_HOST_SESSION:
					SetupAddressFields( hDlg );
					break;

				case IDC_SP_COMBO:
					// If the pSPGuid changed then re-enum the adapters, and
					// update the address fields.
					nSPIndex = (int) SendDlgItemMessage( hDlg, IDC_SP_COMBO, CB_GETCURSEL, 0, 0 );
					pSPGuid = (Guid?) SendDlgItemMessage( hDlg, IDC_SP_COMBO, CB_GETITEMDATA, nSPIndex, 0 );

					if( pSPGuid != null && g_pCurSPGuid != pSPGuid )		// Compares the GUIDs, not the pointers to them, which are equal for the same item
						{
						g_pCurSPGuid = pSPGuid;
						SetupAddressFields( hDlg );
						EnumAdapters( hDlg, pSPGuid );
						}
					break;

				case IDOK:

//	MessageBox(hwndApp,"bitmap_surface","残念！",MB_OK | MB_ICONSTOP);

					// 接続ダイアログのデータをレジ記録する元データ
					GetDlgItemText( hDlg, IDC_PLAYER_NAME, LocalPlayerName, MAX_PATH );

					nIndex = (int) SendDlgItemMessage( hDlg, IDC_SP_COMBO, CB_GETCURSEL, 0, 0 );
					SendDlgItemMessage( hDlg, IDC_SP_COMBO, CB_GETLBTEXT, nIndex, PreferredProvider );

					// Disable the OK button while we work.
					EnableWindow( GetDlgItem( hDlg, IDOK ), FALSE);

              if( FAILED( hr = LaunchMultiplayerGame( hDlg ) ) )
						{
						// 設定された接続は失敗した。
						DXTRACE_ERR_MSGBOX( TEXT("LaunchMultiplayerGame"), hr );
                  MessageBox( null, TEXT("Failed to launch game. "),
                              AppName, MB_OK | MB_ICONERROR );

                  // Renable the OK button.
                  EnableWindow( GetDlgItem( hDlg, IDOK ), TRUE);
						}

					break;

					case IDCANCEL:
						DestroyWindow(hDlg);
						PostQuitMessage( 0 );		// これでWM_QUITがでるので接続ダイアログループからぬける
						break;

					case IDSNROEDIT:
						IsEditingMap=true;
						IsNetworkGame=1;
						IsHostPlayer=1;
						ActivePlayerCount=2;

						DestroyWindow(hDlg);
						PostQuitMessage( 0 );		// これでWM_QUITがでるので接続ダイアログループからぬける
						break;

					}
            break;

		case WM_DESTROY:
			break;
		}

	return FALSE; // Didn't handle message

	}

//-----------------------------------------------------------------------------
// Name: LaunchMultiplayerGame
// Desc: Use the settings in the configuration dialog to launch the session
// 設定にしたがって、接続を作るか、接続を探しに行く
//-----------------------------------------------------------------------------
public int LaunchMultiplayerGame( HWND hDlg )
	{
	int hr          = S_OK;
	int    bOkToQuery  = FALSE;

	IDirectPlay8Address? pHostAddress     = null;
	IDirectPlay8Address? pDeviceAddress   = null;		// 要はサービスプロバイダ？

	// Grab settings from the provided dialog
	// ＳＰがなんなのかインデックスから調べます。
	int   nSPIndex  = (int)   SendDlgItemMessage( hDlg, IDC_SP_COMBO, CB_GETCURSEL,   0,        0 );
	Guid? pSPGuid   = (Guid?) SendDlgItemMessage( hDlg, IDC_SP_COMBO, CB_GETITEMDATA, nSPIndex, 0 );

	IsHostPlayer = (int)IsDlgButtonChecked( hDlg, IDC_HOST_SESSION );

	// If not the host
	if( IsHostPlayer==0 )
		{
		// ゲストだった場合、ホストのアドレスを作ります。
		// Create a host address if connecting to a host,
		// otherwise keep it as NULL
		if( FAILED( hr = CoCreateInstance( CLSID_DirectPlay8Address, null, CLSCTX_INPROC_SERVER,
								 IID_IDirectPlay8Address, out pHostAddress ) ) )
			{
			DXTRACE_ERR_MSGBOX( TEXT("CoCreateInstance"), hr );
			goto LCleanReturn;
			}

		// Set the SP to pHostAddress
		if( FAILED( hr = pHostAddress.SetSP( pSPGuid ) ) )
			{
			DXTRACE_ERR_MSGBOX( TEXT("SetSP"), hr );
			goto LCleanReturn;
			}
		}

	// Create a device address to specify which device we are using
	if( FAILED( hr = CoCreateInstance( CLSID_DirectPlay8Address, null, CLSCTX_INPROC_SERVER,
									IID_IDirectPlay8Address, out pDeviceAddress ) ) )
		{
		DXTRACE_ERR_MSGBOX( TEXT("CoCreateInstance"), hr );
		goto LCleanReturn;
		}

	// Set the SP to pDeviceAddress
	if( FAILED( hr = pDeviceAddress.SetSP( pSPGuid ) ) )
		{
		DXTRACE_ERR_MSGBOX( TEXT("SetSP"), hr );
		goto LCleanReturn;
		}

	// Add the adapter to pDeviceAddress
	int nAdapterIndex;
	nAdapterIndex = (int) SendDlgItemMessage( hDlg, IDC_ADAPTER_COMBO, CB_GETCURSEL, 0, 0 );

	if( nAdapterIndex != CB_ERR )
		{
		// Get the GUID associated with the selected list item
		Guid? pAdapterGuid = (Guid?) SendDlgItemMessage( hDlg, IDC_ADAPTER_COMBO, CB_GETITEMDATA, nAdapterIndex, 0 );
		// Add the device GUID, unless its the special "All Adapters" placeholder
		if ( pAdapterGuid != GUID_NULL )
			{
			if( FAILED( hr = pDeviceAddress.SetDevice( pAdapterGuid ) ) )
				{
				DXTRACE_ERR_MSGBOX( TEXT("SetDevice"), hr );
				goto LCleanReturn;
				}
			}
		}

	// --------------------------------
	// Service Provider: TCP/IP, IPX
	// --------------------------------
	if( pSPGuid == CLSID_DP8SP_TCPIP || pSPGuid == CLSID_DP8SP_IPX )
		{
		// ネット接続する。
		Array260<byte> strHostname = default;
		Array260<byte> strPort = default;

		GetDlgItemText( hDlg, IDC_ADDRESS_LINE1, strHostname, MAX_PATH );
		GetDlgItemText( hDlg, IDC_ADDRESS_LINE2, strPort, MAX_PATH );

		// TCP/IP Only: As a convenience for the user, we store the
		// most recently used remote hostname in the registry when
		// the program exits.
		if( pSPGuid == CLSID_DP8SP_TCPIP )
			{
			_tcsncpy( RemoteHostName, strHostname, MAX_PATH );		// ホストネームとなっていてもホストのＩＰアドレスのこと
			RemoteHostName[MAX_PATH-1] = 0;
			}

		if( IsHostPlayer!=0 )
			{
			// ホスト選択の場合、ＳＰにポートアドレスを設定
			if( _tcslen( strPort ) > 0 )
				{
				 // Add the port to pDeviceAddress
				 uint dwPort = (uint)_ttoi( strPort );
				 if( FAILED( hr = pDeviceAddress.AddComponent( DPNA_KEY_PORT,
																				dwPort, sizeof(uint),
																				DPNA_DATATYPE_DWORD ) ) )
					 {
					  DXTRACE_ERR_MSGBOX( TEXT("AddComponent"), hr );
					  goto LCleanReturn;
					 }
				}
			}
	  else
			{
			// ゲストの場合、ホストのＩＰアドレスを作る
			// Add the hostname to pHostAddress
			if( _tcslen( strHostname ) > 0 )
				{
				 string wstrHostname;
				 DXUtil_ConvertGenericStringToWideCch( out wstrHostname, strHostname, MAX_PATH );

				 if( FAILED( hr = pHostAddress.AddComponent( DPNA_KEY_HOSTNAME,
									 wstrHostname, (uint) ((wcslen(wstrHostname)+1)*sizeof(char)),
									 DPNA_DATATYPE_STRING ) ) )
					 {
					  DXTRACE_ERR_MSGBOX( TEXT("AddComponent"), hr );
					  goto LCleanReturn;
					 }
				}

			if( _tcslen( strPort ) > 0 )
				{
				// Add the port to pHostAddress
				uint dwPort = (uint)_ttoi( strPort );
				if( FAILED( hr = pHostAddress.AddComponent( DPNA_KEY_PORT,
																			 dwPort, sizeof(uint),
																			 DPNA_DATATYPE_DWORD ) ) )
					{
					  DXTRACE_ERR_MSGBOX( TEXT("AddComponent"), hr );
					  goto LCleanReturn;
					}
				}
			}
		}
    // --------------------------------
    // Service Provider: Modem
    // --------------------------------
	else if( pSPGuid == CLSID_DP8SP_MODEM )
		{
		Array260<byte> strPhone = default;
		GetDlgItemText( hDlg, IDC_ADDRESS_LINE1, strPhone, MAX_PATH );

		if( IsHostPlayer==0 )
			{
			// Add the phonenumber to pHostAddress
			if( _tcslen( strPhone ) > 0 )
				{
				string wstrPhone;
				DXUtil_ConvertGenericStringToWideCch( out wstrPhone, strPhone, MAX_PATH );

				if( FAILED( hr = pHostAddress.AddComponent( DPNA_KEY_PHONENUMBER,
															 wstrPhone, (uint) ((wcslen(wstrPhone)+1)*sizeof(char)),
															 DPNA_DATATYPE_STRING ) ) )
					{
					DXTRACE_ERR_MSGBOX( TEXT("AddComponent"), hr );
					goto LCleanReturn;
					}
				}
			}
		}
	// --------------------------------
	// Service Provider: Serial Port
	// --------------------------------
	else if( pSPGuid == CLSID_DP8SP_SERIAL )
		{
		// This simple client doesn't have UI to query for the various
		// fields needed for the serial.  So we just let DPlay popup a dialog
		// to ask the user which settings are needed.
		bOkToQuery = TRUE;
		}
	// --------------------------------
	// Service Provider: Unknown
	// --------------------------------
	else
		{
		// Unknown SP, so leave as is
		bOkToQuery = TRUE;
		}

	// 自分をＤＰに登録する
	// Prepare local player name
	string wszPeerName;
	DXUtil_ConvertGenericStringToWideCch( out wszPeerName, LocalPlayerName, MAX_PATH );

	// Fill in player info structure
	DPN_PLAYER_INFO dpPlayerInfo = new();
	dpPlayerInfo.dwSize       = DPN_PLAYER_INFO.SIZE;
	dpPlayerInfo.dwInfoFlags  = DPNINFO_NAME;
	dpPlayerInfo.pwszName     = wszPeerName;

	// Set the peer info, and use the DPNOP_SYNC since by default this
	// is an async call.  If it is not DPNOP_SYNC, then the peer info may not
	// be set by the time we call Connect() below.
	if( FAILED( hr = g_pDP.SetPeerInfo( dpPlayerInfo, null, null, DPNOP_SYNC ) ) )
		{
		DXTRACE_ERR_MSGBOX( TEXT("SetPeerInfo"), hr );
		goto LCleanReturn;
		}

	// Fill in application description structure
	DPN_APPLICATION_DESC dpnAppDesc = new();
	dpnAppDesc.dwSize = DPN_APPLICATION_DESC.SIZE;
	dpnAppDesc.dwFlags = DPNSESSION_NODPNSVR;
	dpnAppDesc.guidApplication = g_guidApp;
	dpnAppDesc.guidInstance    = GUID_NULL;
	dpnAppDesc.pwszSessionName = null;
	dpnAppDesc.dwMaxPlayers = 2;

	//---------------------------------
	// If we are hosting...
	//---------------------------------
	if( IsHostPlayer!=0 )
		{
		// Set the dpnAppDesc.pwszSessionName

//	MessageBox(hwndApp,"bitmap_surface","残念！",MB_OK | MB_ICONSTOP);

		uint dwHostFlags = 0;
		if (bOkToQuery!=0)
			{
			dwHostFlags |= DPNHOST_OKTOQUERYFORADDRESSING;
			}

		// Host a game as described by pSettings
		hr = g_pDP.Host( dpnAppDesc,          // the application desc
								  [pDeviceAddress],      // array of addresses of the local devices used to connect to the host
								  1,                    // number in array
								  null, null,           // DPN_SECURITY_DESC, DPN_SECURITY_CREDENTIALS
								  null,                 // player context
								  dwHostFlags );        // flags

		if( FAILED(hr) )
			{
			DXTRACE_ERR_MSGBOX( TEXT("Host"), hr );
			goto LCleanReturn;
			}

		// Close the existing dialog and create the "in-game" dialog.
		DestroyWindow(g_hDlg);
		g_hDlg = CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_MAIN_GAME), null, GreetingDlgProc);
		}
    //---------------------------------
    // If we are connecting...
    //---------------------------------
	else
		{
		// 子機として接続を探しにいく、変なアドレスを指定すると標準のＤＰダイアログで
		// 質問してくるらしい。
		// We could enumerate the host first, but since we are overriding the defaults
		// with a specific address, we are probably expecting the host to be at that
		// address.  Connect directly.

//	MessageBox(hwndApp,"ここまで北","残念！",MB_OK | MB_ICONSTOP);

		uint dwConnectFlags = 0;
		if (bOkToQuery!=0)
			{
			dwConnectFlags |= DPNCONNECT_OKTOQUERYFORADDRESSING;
			}

		// Enumerate all the active DirectPlay games on the selected connection
		hr = g_pDP.Connect( dpnAppDesc,            // application description
							  pHostAddress,           // host address
							  pDeviceAddress,         // device address
							  null,                   // security desc
							  null,                   // credentials
							  null,                   // user connect data
							  0,                      // user connect data size
							  null,                   // player context
							  null,                   // async op user context
							  ref g_hConnectAsyncOp,     // place to store async op handle
							  dwConnectFlags );       // flags
		if( FAILED(hr) )
			{
			DXTRACE_ERR_MSGBOX( TEXT("Connect"), hr );
			goto LCleanReturn;
			}

		}

	hr = S_OK;

LCleanReturn:
	// Cleanup the addresses
	SAFE_RELEASE( ref pHostAddress );
	SAFE_RELEASE( ref pDeviceAddress );

	return hr;
	}

private void ReceiveSideAndScenario(DPNMSG_RECEIVE pReceiveMsg)
	{
	_DP_DATA_1* pMsg = (_DP_DATA_1*) pReceiveMsg.pReceiveData;

	// ジョインが受け取る
	HostSide=pMsg->data[0];
	ScenarioNumber=pMsg->data[1];

	SupplyRates[0]=pMsg->data[2];		// Host
	SupplyRates[1]=pMsg->data[3];		// Guest

	IsDecisionEnabled=new Bool8((byte)pMsg->data[4]);

	InitialSupplyPoints[0]=pMsg->data[5];		// Host
	InitialSupplyPoints[1]=pMsg->data[6];		// Guest

	ArrivalControl=(byte)pMsg->data[7];

	SwapTime=pMsg->data[8];
	SwapRule=pMsg->data[9];
	}

private void ReceiveLeaveGameSetting(DPNMSG_RECEIVE pReceiveMsg)
	{
	_DP_DATA_1* pMsg = (_DP_DATA_1*) pReceiveMsg.pReceiveData;

	// ジョインが受け取る
	HostSide=pMsg->data[0];
	ScenarioNumber=pMsg->data[1];

	JoinGameStart=MessageType.LeaveGameSetting;

	SupplyRates[0]=pMsg->data[2];		// Host
	SupplyRates[1]=pMsg->data[3];		// Guest

	IsDecisionEnabled=new Bool8((byte)pMsg->data[4]);

	InitialSupplyPoints[0]=pMsg->data[5];		// Host
	InitialSupplyPoints[1]=pMsg->data[6];		// Guest

	ArrivalControl=(byte)pMsg->data[7];

	SwapTime=pMsg->data[8];
	SwapRule=pMsg->data[9];
	}

private void ReceiveLeaveConfigSetting(DPNMSG_RECEIVE pReceiveMsg)
	{
	_DP_DATA_1* pMsg = (_DP_DATA_1*) pReceiveMsg.pReceiveData;

	// ジョインが受け取る
	HostSide=pMsg->data[0];
	ScenarioNumber=pMsg->data[1];

	JoinGameStart=MessageType.LeaveConfigSetting;

	SupplyRates[0]=pMsg->data[2];		// Host
	SupplyRates[1]=pMsg->data[3];		// Guest

	IsDecisionEnabled=new Bool8((byte)pMsg->data[4]);

	InitialSupplyPoints[0]=pMsg->data[5];		// Host
	InitialSupplyPoints[1]=pMsg->data[6];		// Guest

	ArrivalControl=(byte)pMsg->data[7];

	SwapTime=pMsg->data[8];
	SwapRule=pMsg->data[9];
	}

private void ReceiveMoveOrder(DPNMSG_RECEIVE pReceiveMsg)
	{
	int s;
	_DP_NEW_PP* pMsg = (_DP_NEW_PP*) pReceiveMsg.pReceiveData;

	// ホスト、ジョインともここで相手のデータを受け取る。
	BufferedMoveOrders[0].Unit=pMsg->used;
	BufferedMoveOrders[0].Destination = new WorldPosition(pMsg->x, pMsg->y);
	BufferedMoveOrders[0].ClearsPath=pMsg->cls;
	for( s=0; s<=(USA_PLANE_END/2)-1; s++)
		{
		BufferedSelections[0][s]=pMsg->slct_unit[s];
		}
	CanAdvance2=true;
	}

private void ReceiveMoveShipsOrder(DPNMSG_RECEIVE pReceiveMsg)
	{
	int s;
	_DP_NEW_PP_SHIP* pMsg = (_DP_NEW_PP_SHIP*) pReceiveMsg.pReceiveData;

	BufferedMoveOrders[0].Unit=pMsg->used;
	BufferedMoveOrders[0].Destination = new WorldPosition(pMsg->x, pMsg->y);
	BufferedMoveOrders[0].ClearsPath=pMsg->cls;
	for( s=0; s<=JPN_SHIP_END-1; s++)
		{
		BufferedSelections[0][s]=pMsg->slct_unit[s];
		}
	CanAdvance2=true;
	}

private void ReceiveMovePlanesOrder(DPNMSG_RECEIVE pReceiveMsg)
	{
	int s;
	_DP_NEW_PP_PLANE* pMsg = (_DP_NEW_PP_PLANE*) pReceiveMsg.pReceiveData;

	// ホスト、ジョインともここで相手のデータを受け取る。
	BufferedMoveOrders[0].Unit=pMsg->used;
	BufferedMoveOrders[0].Destination = new WorldPosition(pMsg->x, pMsg->y);
	BufferedMoveOrders[0].ClearsPath=pMsg->cls;
	for( s=0; s<=JPN_PLANE_END-JPN_PLANE_START; s++)
		{
		BufferedSelections[0][s+JPN_SHIP_END]=pMsg->slct_unit[s];
		}
	CanAdvance2=true;
	}

private void ReceiveSelectOrder(DPNMSG_RECEIVE pReceiveMsg)
	{
	int s;
	_DP_NEW_SLCT* pMsg = (_DP_NEW_SLCT*) pReceiveMsg.pReceiveData;

	// ホスト、ジョインともここで相手のデータを受け取る。
	BufferedSelectOrders[0].IsSet=new Bool32(pMsg->sw);
	BufferedSelectOrders[0].SelectedUnit=pMsg->the_slct_unit;
	BufferedSelectOrders[0].Unit=pMsg->m;
	BufferedSelectOrders[0].GroundPosition = new WorldPosition(pMsg->gr_x, pMsg->gr_y);
	for( s=0; s<=(USA_PLANE_END/2)-1; s++)
		{
		BufferedSelections[0][s]=pMsg->slct_unit[s];
		}
	CanAdvance2=true;
	}

private void ReceiveSelectShipsOrder(DPNMSG_RECEIVE pReceiveMsg)
	{
	int s;
	_DP_NEW_SLCT_SHIP* pMsg = (_DP_NEW_SLCT_SHIP*) pReceiveMsg.pReceiveData;

	// ホスト、ジョインともここで相手のデータを受け取る。
	BufferedSelectOrders[0].IsSet=new Bool32(pMsg->sw);
	BufferedSelectOrders[0].SelectedUnit=pMsg->the_slct_unit;
	BufferedSelectOrders[0].Unit=pMsg->m;
	BufferedSelectOrders[0].GroundPosition = new WorldPosition(pMsg->gr_x, pMsg->gr_y);
	for( s=0; s<=JPN_SHIP_END-1; s++)
		{
		BufferedSelections[0][s]=pMsg->slct_unit[s];
		}
	CanAdvance2=true;
	}

private void ReceiveSelectPlanesOrder(DPNMSG_RECEIVE pReceiveMsg)
	{
	int s;
	_DP_NEW_SLCT_PLANE* pMsg = (_DP_NEW_SLCT_PLANE*) pReceiveMsg.pReceiveData;

	// ホスト、ジョインともここで相手のデータを受け取る。
	BufferedSelectOrders[0].IsSet=new Bool32(pMsg->sw);
	BufferedSelectOrders[0].SelectedUnit=pMsg->the_slct_unit;
	BufferedSelectOrders[0].Unit=pMsg->m;
	BufferedSelectOrders[0].GroundPosition = new WorldPosition(pMsg->gr_x, pMsg->gr_y);
	for( s=0; s<=JPN_PLANE_END-JPN_PLANE_START; s++)
		{
		BufferedSelections[0][s+JPN_SHIP_END]=pMsg->slct_unit[s];
		}
	CanAdvance2=true;
	}

private void ReceiveSelectLandOrder(DPNMSG_RECEIVE pReceiveMsg)
	{
	_DP_NEW_SLCT_LAND* pMsg = (_DP_NEW_SLCT_LAND*) pReceiveMsg.pReceiveData;

	// ホスト、ジョインともここで相手のデータを受け取る。
	BufferedSelectOrders[0].IsSet=new Bool32(pMsg->sw);
	BufferedSelectOrders[0].SelectedUnit=pMsg->the_slct_unit;
	BufferedSelectOrders[0].Unit=pMsg->m;
	BufferedSelectOrders[0].GroundPosition = new WorldPosition(pMsg->gr_x, pMsg->gr_y);
	CanAdvance2=true;
	}

private void ReceiveMenuOrder(DPNMSG_RECEIVE pReceiveMsg)
	{
	int s;
	_DP_NEW_MENU* pMsg = (_DP_NEW_MENU*) pReceiveMsg.pReceiveData;

	// ホスト、ジョインともここで相手のデータを受け取る。
	BufferedMenuOrders[0].Menu=(CombatMenuItem)pMsg->menu;
	BufferedMenuOrders[0].SelectedUnit=pMsg->the_slct_unit;

	for( s=0; s<=(USA_PLANE_END/2)-1; s++)
		{
		BufferedSelections[0][s]=pMsg->slct_unit[s];
		}
	CanAdvance2=true;
	}

private void ReceiveSyncFlag(DPNMSG_RECEIVE pReceiveMsg)
	{
	_DP_FLAG* pMsg = (_DP_FLAG*) pReceiveMsg.pReceiveData;

	// ホスト、ジョインともここで相手のデータを受け取る。
	CanAdvance1=true;

	TickChecksums[0]=pMsg->cc_chk;
	UnitChecksums[0]=pMsg->unit_chk;
	RandomChecksums[0]=pMsg->rnd_chk;
	TickWaits[0]=pMsg->ccc_wait_chk;

	RivalMode=(GameMode)pMsg->rival_mode;
	}

private void ReceiveGoToGameSetting()
	{
	if( Mode==GameMode.ConfigSetting || Mode==GameMode.Title )
		{
		// ジョインが受け取る
		GoToGameSetting();
		}
	else
		{
		// ホスト、ジョインともここで相手のデータを受け取る。
		CanAdvance2=true;
		SystemOrders[1]=(byte)MessageType.GoToGameSetting;
		}
	}

private void ReceiveUserScenarioFileName(DPNMSG_RECEIVE pReceiveMsg)
	{
	int s;
	_DP_DATA_20* pMsg = (_DP_DATA_20 *) pReceiveMsg.pReceiveData;

	for(s=0;s<MAX_PATH;s++)
		{
		UserScenarioFileName[s]=pMsg->friend_chat[s];
		}
	}

//-----------------------------------------------------------------------------
// Name: DirectPlayMessageHandler
// Desc: Handler for DirectPlay messages.  This function is called by
//       DirectPlay as needed.  Since we are using DirectPlay in "DoWork" mode
//       and we only use a single thread, we do not have to worry about thread
//       synchronization problems
//
//　ＤＰの受信処理。
//
//-----------------------------------------------------------------------------
public int DirectPlayMessageHandler( object? pvUserContext, uint dwMessageId, object pMsgBuffer )
	{
	int		s,m,n;

	switch( dwMessageId )
		{
		case DPN_MSGID_CREATE_PLAYER:
			{
			int hr;
			DPNMSG_CREATE_PLAYER pCreatePlayerMsg;						// 新規追加につかう構造体
			pCreatePlayerMsg = (DPNMSG_CREATE_PLAYER)pMsgBuffer;		// この時点で参加者のＤＰＮＩＤはｗかる

			// Get the peer info and extract its name
			uint dwSize = 0;						// サイズをわざと０と少なくしておいてDPNERR_BUFFERTOOSMALLを誘発させる
			DPN_PLAYER_INFO? pdpPlayerInfo = null;
			hr = DPNERR_CONNECTING;

			while( hr == DPNERR_CONNECTING )
				hr = g_pDP.GetPeerInfo( pCreatePlayerMsg.dpnidPlayer, pdpPlayerInfo, ref dwSize, 0 );

			if( hr == DPNERR_BUFFERTOOSMALL )
				{
				pdpPlayerInfo = new DPN_PLAYER_INFO();
				if( null == pdpPlayerInfo )
					{
					// Out of memory
					break;
					}

				pdpPlayerInfo.dwSize = DPN_PLAYER_INFO.SIZE;

				hr = g_pDP.GetPeerInfo( pCreatePlayerMsg.dpnidPlayer, pdpPlayerInfo, ref dwSize, 0 );
				if( SUCCEEDED(hr) )
					{
					Array14<byte> strThisPlayerName = default;		// temp player name buffer

					DXUtil_ConvertWideStringToGenericCch( strThisPlayerName,
													 pdpPlayerInfo.pwszName, MAX_PLAYER_NAME );

					if( (pdpPlayerInfo.dwPlayerFlags & DPNPLAYER_LOCAL)!=0 )
						{
						g_dpnidLocalPlayer = pCreatePlayerMsg.dpnidPlayer;
						}
					else
						{
						// ゲストプレイヤーだった
						// まず名前を取得
						sprintf(RivalPlayerName, "%s", strThisPlayerName );
						RivalPlayerName[ MAX_PATH-1 ] = 0;

						// 対戦相手のＤＰＮＩＤを取得
						g_dpnidRivalPlayer = pCreatePlayerMsg.dpnidPlayer;
						}

					}
				SAFE_DELETE_ARRAY( ref pdpPlayerInfo );
				}

			ActivePlayerCount++;
			if( g_hDlg != null )
				PostMessage( g_hDlg, WM_APP_UPDATE_STATS, 0, 0 );

			break;
			}

		case DPN_MSGID_DESTROY_PLAYER:
				{
				// 多分ホストで、ゲストの誰かが接続を切ったらここに来る。
            DPNMSG_DESTROY_PLAYER pDestroyPlayerMsg;
            pDestroyPlayerMsg = (DPNMSG_DESTROY_PLAYER)pMsgBuffer;
            // Update the number of active players, and
            // post a message to the dialog thread to update the
            // UI.
            ActivePlayerCount--;
				if( g_hDlg != null )
					{
					sprintf(RivalPlayerName, " - - - " );
					PostMessage( g_hDlg, WM_APP_UPDATE_STATS, 0, 0 );
					}

            break;
		     }

		case DPN_MSGID_TERMINATE_SESSION:
			{
			// ゲストで、ホストが接続を切った場合ここにくる。
			DPNMSG_TERMINATE_SESSION pTerminateSessionMsg;
			pTerminateSessionMsg = (DPNMSG_TERMINATE_SESSION)pMsgBuffer;

			// The session was terminated.  Generally we don't want to display dialog boxes
			// and block a DirectPlay message handler callback, but we are doing it in this
			// sample for simplicity.

			if( g_hDlg != null )
				{
				// フルスクリーンの前の接続ダイアログの場合。
				MessageBox( g_hDlg, TEXT("Session was terminated."), AppName, MB_OK | MB_ICONERROR );
				PostMessage( g_hDlg, WM_COMMAND, IDCANCEL, 0 );
				}

			break;
			}

		case DPN_MSGID_RECEIVE:
			{
			// なんかメッセージが相手から届いた。
			DPNMSG_RECEIVE pReceiveMsg;
			pReceiveMsg = (DPNMSG_RECEIVE)pMsgBuffer;

			// とりあえず、ジェネラルに入れる。
			GENERICMSG* pGenericMsg = (GENERICMSG*) pReceiveMsg.pReceiveData;		// pMsg in the original, which the pMsg below shadow

			if( (uint)pGenericMsg->dwType == MSG_TST )
				{
				}
			else if( (uint)pGenericMsg->dwType == MSG_EXIT_WAITING )
				{
				PostMessage( g_hDlg, WM_COMMAND, IDC_START_GAME, 0 );
				}
			else if( pGenericMsg->dwType == MessageType.RivalMode )
				{
				_DP_FLAG* pMsg = (_DP_FLAG*) pReceiveMsg.pReceiveData;
				RivalMode=(GameMode)pMsg->rival_mode;
				}
			else if( pGenericMsg->dwType == MessageType.LeaveSetup )
				{
				_DP_DATA_1* pMsg = (_DP_DATA_1*) pReceiveMsg.pReceiveData;

				// ジョインが受け取る
				SharedRandomSeed=pMsg->data[0];

				}

			else if (pGenericMsg->dwType == MessageType.SideAndScenario && ( Mode==GameMode.GameSetting || Mode==GameMode.ConfigSetting ) )
				{
				ReceiveSideAndScenario(pReceiveMsg);
				}
			else if( pGenericMsg->dwType == MessageType.LeaveGameSetting && ( Mode==GameMode.GameSetting || Mode==GameMode.ConfigSetting ) )
				{
				ReceiveLeaveGameSetting(pReceiveMsg);
				}
			else if( pGenericMsg->dwType == MessageType.LeaveConfigSetting )
				{
				ReceiveLeaveConfigSetting(pReceiveMsg);
				}
			else if( pGenericMsg->dwType == MessageType.StartFromResume )
				{
				JoinGameStart=MessageType.StartFromResume;

				}
			else if( pGenericMsg->dwType == MessageType.StartFromAutoSave )
				{
				JoinGameStart=MessageType.StartFromAutoSave;
				}
			else if( pGenericMsg->dwType == MessageType.UnitArrived )
				{
				_DP_DATA_1* pMsg = (_DP_DATA_1*) pReceiveMsg.pReceiveData;

				BufferedArrivedUnits[0]=pMsg->data[0];		// 敵が１ユニット増える。
				CanAdvance2=true;
				}
			else if( pGenericMsg->dwType == MessageType.MoveOrder )
				{
				ReceiveMoveOrder(pReceiveMsg);
				}
			else if( pGenericMsg->dwType == MessageType.MoveShipsOrder )
				{
				ReceiveMoveShipsOrder(pReceiveMsg);
				}
			else if( pGenericMsg->dwType == MessageType.MovePlanesOrder )
				{
				ReceiveMovePlanesOrder(pReceiveMsg);
				}
			else if( pGenericMsg->dwType == MessageType.SelectOrder )
				{
				ReceiveSelectOrder(pReceiveMsg);
				}
			else if( pGenericMsg->dwType == MessageType.SelectShipsOrder )
				{
				ReceiveSelectShipsOrder(pReceiveMsg);
				}
			else if( pGenericMsg->dwType == MessageType.SelectPlanesOrder )
				{
				ReceiveSelectPlanesOrder(pReceiveMsg);
				}
			else if( pGenericMsg->dwType == MessageType.SelectLandOrder )
				{
				ReceiveSelectLandOrder(pReceiveMsg);
				}
			else if( pGenericMsg->dwType == MessageType.MenuOrder )
				{
				ReceiveMenuOrder(pReceiveMsg);
				}
			else if( pGenericMsg->dwType == MessageType.NoOrder )
				{
				// ホスト、ジョインともここで相手のデータを受け取る。
				CanAdvance2=true;
				}
			else if( pGenericMsg->dwType == MessageType.SyncFlag )
				{
				ReceiveSyncFlag(pReceiveMsg);
				}
			else if( pGenericMsg->dwType == MessageType.RivalMode )
				{
				_DP_FLAG* pMsg = (_DP_FLAG*) pReceiveMsg.pReceiveData;

				// ホスト、ジョインともここで相手のデータを受け取る。
				RivalMode=(GameMode)pMsg->rival_mode;

				}
			else if( pGenericMsg->dwType == MessageType.GoToGameSetting )
				{
				ReceiveGoToGameSetting();
				}
			else if( pGenericMsg->dwType == MessageType.ResumeAndGoToGameSetting )
				{
				// ジョイン ここで相手のデータを受け取る。
				CanAdvance2=true;
				SystemOrders[1]=(byte)MessageType.ResumeAndGoToGameSetting;
				}
			else if( pGenericMsg->dwType == MessageType.Chat )
				{
				_DP_DATA_20* pMsg = (_DP_DATA_20 *) pReceiveMsg.pReceiveData;

				for(s=0;s<MAX_PATH;s++)
					{
					RivalChat[s]=pMsg->friend_chat[s];
					}
				RivalChatDisplayTime=unchecked((byte)CHAT_DSP_TIME);		// 400 does not fit in a BYTE: 144
				}
			else if( pGenericMsg->dwType == MessageType.RivalVersion )
				{
				_DP_DATA_20* pMsg = (_DP_DATA_20 *) pReceiveMsg.pReceiveData;

				for(s=0;s<15;s++)
					{
					RivalVersion[s]=pMsg->friend_chat[s];
					}
				RivalVersion[s]=0;
				}
			else if( pGenericMsg->dwType == MessageType.UserScenarioFileName )
				{
				ReceiveUserScenarioFileName(pReceiveMsg);
				}

/*
			else if( pGenericMsg->dwType ==  )
				{
				_DP_DATA_1* pMsg = (_DP_DATA_1*) pReceiveMsg.pReceiveData;

				}

typedef	struct	_DP_DATA_1
	{
	DWORD	dwType;
	char	my_name[16];
	short	data[8];
	} DP_DATA_1;

			else if ( pGenericMsg->dwType == RIVAL_MODE )
				{
				lp_dp_flag = (_DP_FLAG *)pReceiveMsg->pReceiveData;
				// ホスト、ジョインともここで相手のデータを受け取る。
				rival_mode=lp_dp_flag->rival_mode;
				}
*/

/*
				lp_dp_flag = (_DP_FLAG *)lpvMsgBuffer;
				if ( lp_dp_flag->dwType == RIVAL_MODE )
					{
					// ホスト、ジョインともここで相手のデータを受け取る。
					rival_mode=lp_dp_flag->rival_mode;
					}
*/

			break;
			}

		case DPN_MSGID_CONNECT_COMPLETE:
			{
			// ゲストで、ホストに接続出来たらここへくる。
			DPNMSG_CONNECT_COMPLETE pConnectCompleteMsg;
			pConnectCompleteMsg = (DPNMSG_CONNECT_COMPLETE)pMsgBuffer;

			g_hConnectAsyncOp = 0;
			if( FAILED( pConnectCompleteMsg.hResultCode ) )
				{
				// The connect failed.  Generally we don't want to display dialog boxes and
				// block a DirectPlay message handler callback, but we are doing it in this
				// sample for simplicity.
				MessageBox( g_hDlg, TEXT("Unable to join game."),
					 AppName, MB_OK | MB_ICONERROR );

				// Re-enable the OK button.
				EnableWindow( GetDlgItem( g_hDlg, IDOK ), TRUE);
				break;
				}

			// Otherwise, the connect succeeded.  Create the "in-game" dialog.

			DestroyWindow(g_hDlg);

			g_hDlg = CreateDialog(hInstApp, MAKEINTRESOURCE(IDD_MAIN_GAME), null, GreetingDlgProc);
			break;
			}
		case DPN_MSGID_SEND_COMPLETE:
			{
			break;
			}
		}

	return S_OK;
	}
}
