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

using static OpenNspw.Nspw;

namespace OpenNspw;

public unsafe partial class Nspw
{
	[Original("load_user_map")]
	public void LoadUserMap()
	{
		short n;
		Array256<Array256<ushort>> path = default; // マップ

		var hFile = CreateFile(
			UserScenarioFileName /*"Map\\user_map.dat"*/
			,
			GENERIC_READ | GENERIC_WRITE,
			FILE_SHARE_READ,
			OPEN_ALWAYS
		);

		if (hFile != INVALID_HANDLE_VALUE)
		{
			uint bytesTransferred;

			// 読み込み
			SetFilePointer(hFile, 0, FILE_BEGIN); // 先頭へ

			ReadFile(
				hFile,
				ref path,
				(uint)(sizeof(Array256<Array256<ushort>>)),
				&bytesTransferred,
				null
			);

			// バッファよりデータへ
			// データ を バッファへ
			for (short m = 0; m <= 255; m++)
			for (n = 0; n <= 255; n++)
				MapTiles[m][n] = path[m][n];

			ReadFile(hFile, ref Units, (uint)(sizeof(Array256<Unit>)), &bytesTransferred, null);
			ReadFile(
				hFile,
				ref Reinforcements,
				(uint)(sizeof(Array3<byte>)),
				&bytesTransferred,
				null
			);

			ReadFile(hFile, ref IsDecisionEnabled, sizeof(byte), &bytesTransferred, null);
			ReadFile(hFile, ref ArrivalControl, sizeof(byte), &bytesTransferred, null);

			ReadFile(
				hFile,
				ref SupplyRates,
				(uint)(sizeof(Array2<short>)),
				&bytesTransferred,
				null
			);
			ReadFile(
				hFile,
				ref InitialSupplyPoints,
				(uint)(sizeof(Array2<short>)),
				&bytesTransferred,
				null
			);

			ReadFile(hFile, ref SwapTime, sizeof(short), &bytesTransferred, null);
			ReadFile(hFile, ref SwapRule, sizeof(short), &bytesTransferred, null);

			CloseHandle(hFile);
		}
	}

	[Original("load_it2")]
	public void LoadScenarioFile2(string str)
	{
		short n;
		Array256<Array256<ushort>> path = default; // マップ

		if (strcmp(str, "Map\\South_pacific.dat") == 0)
			CurrentMap = 0;
		else if (strcmp(str, "Map\\Middle_pacific.dat") == 0)
			CurrentMap = 1;
		else
			CurrentMap = 2;

		var hFile = CreateFile(str, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ, OPEN_ALWAYS);

		if (hFile != INVALID_HANDLE_VALUE)
		{
			uint bytesTransferred;

			// 読み込み
			SetFilePointer(hFile, 0, FILE_BEGIN); // 先頭へ

			ReadFile(
				hFile,
				ref path,
				(uint)(sizeof(Array256<Array256<ushort>>)),
				&bytesTransferred,
				null
			);

			// バッファよりデータへ
			// データ を バッファへ
			for (short m = 0; m <= 255; m++)
			for (n = 0; n <= 255; n++)
				MapTiles[m][n] = path[m][n];

			CloseHandle(hFile);
		}
	}

	[Original("load_it3")]
	public void LoadScenarioFile3()
	{
		short n;
		Array256<Array256<ushort>> path = default; // マップ

		var hFile = CreateFile(
			UserScenarioFileName /*"Map\\user_map.dat"*/
			,
			GENERIC_READ | GENERIC_WRITE,
			FILE_SHARE_READ,
			OPEN_ALWAYS
		);

		if (hFile != INVALID_HANDLE_VALUE)
		{
			uint bytesTransferred;

			// 読み込み
			SetFilePointer(hFile, 0, FILE_BEGIN); // 先頭へ

			ReadFile(
				hFile,
				ref path,
				(uint)(sizeof(Array256<Array256<ushort>>)),
				&bytesTransferred,
				null
			);

			// バッファよりデータへ
			// データ を バッファへ
			for (short m = 0; m <= 255; m++)
			for (n = 0; n <= 255; n++)
				MapTiles[m][n] = path[m][n];

			ReadFile(hFile, ref Units, (uint)(sizeof(Array256<Unit>)), &bytesTransferred, null);
			ReadFile(
				hFile,
				ref Reinforcements,
				(uint)(sizeof(Array3<byte>)),
				&bytesTransferred,
				null
			);

			CloseHandle(hFile);
		}
	}

	[Original("save_user_map")]
	public void SaveUserMap()
	{
		short n;
		uint bytesTransferred;
		Array256<Array256<ushort>> path = default; // マップ

		var hFile = CreateFile(
			UserScenarioFileName /*"Map\\user_map.dat"*/
			,
			GENERIC_READ | GENERIC_WRITE,
			FILE_SHARE_READ,
			OPEN_ALWAYS
		);

		if (hFile != INVALID_HANDLE_VALUE)
		{
			//unsigned short		cmbt_map[256][256];					// マップ

			SetFilePointer(hFile, 0, FILE_BEGIN); // 先頭へ

			// マップを記録
			// データ を バッファへ
			for (short m = 0; m <= 255; m++)
			for (n = 0; n <= 255; n++)
				path[m][n] = MapTiles[m][n];

			WriteFile(
				hFile,
				ref path,
				(uint)(sizeof(Array256<Array256<ushort>>)),
				&bytesTransferred,
				null
			); // 書き込み

			//　ユニット、その他を記録
			// 書き込み
			WriteFile(hFile, ref Units, (uint)(sizeof(Array256<Unit>)), &bytesTransferred, null);
			WriteFile(
				hFile,
				ref Reinforcements,
				(uint)(sizeof(Array3<byte>)),
				&bytesTransferred,
				null
			);

			WriteFile(hFile, ref IsDecisionEnabled, sizeof(byte), &bytesTransferred, null);
			WriteFile(hFile, ref ArrivalControl, sizeof(byte), &bytesTransferred, null);

			WriteFile(
				hFile,
				ref SupplyRates,
				(uint)(sizeof(Array2<short>)),
				&bytesTransferred,
				null
			);
			WriteFile(
				hFile,
				ref InitialSupplyPoints,
				(uint)(sizeof(Array2<short>)),
				&bytesTransferred,
				null
			);

			WriteFile(hFile, ref SwapTime, sizeof(short), &bytesTransferred, null);
			WriteFile(hFile, ref SwapRule, sizeof(short), &bytesTransferred, null);

			CloseHandle(hFile);
		}
	}

	/*-------------------------------------------
		終了の処理
	--------------------------------------------*/
	public void EndApp()
	{
		//
		if (Renderer.GameFont1)
			DeleteObject(Renderer.GameFont1);
		if (Renderer.GameFont2)
			DeleteObject(Renderer.GameFont2);

		// ダイレクトミュージック
		RELEASE(ref Sound.lpDMP);
		RELEASE(ref Sound.lpDML);

		// ダイレクトサウンド
		for (var m = 0; m < NUM_SOUND_EFFECTS; m++)
		{
			for (var n = 0; n < SND_DUP; n++)
			{
				RELEASE(ref Sound.SoundBuffers[m][n]);
			}
		}
		RELEASE(ref Sound.lpDSP);
		RELEASE(ref Sound.lpDS);

		// ダイレクトドロー
		RELEASE(ref Renderer.OffscreenSurface);
		RELEASE(ref Renderer.lpDDclip);
		RELEASE(ref Renderer.lpDDSBack);
		RELEASE(ref Renderer.lpDDSPrimary);
		RELEASE(ref Renderer.lpDD);

		// ダイレクトプレイ
		if (DirectPlayPeer != null)
		{
			DirectPlayPeer.Close(0);
			DirectPlayPeer.Release();
			DirectPlayPeer = null;
		}

		if (DirectPlayThreadPool != null)
		{
			DirectPlayThreadPool.Close(0);
			DirectPlayThreadPool.Release();
			DirectPlayThreadPool = null;
		}

		// Write information to the registry
		DXUtil_WriteStringRegKey(hDPlaySampleRegKey, TEXT("Player Name"), LocalPlayerName);
		DXUtil_WriteStringRegKey(hDPlaySampleRegKey, TEXT("Preferred Provider"), PreferredProvider);
		DXUtil_WriteStringRegKey(hDPlaySampleRegKey, TEXT("Remote Hostname"), RemoteHostName);

		RegCloseKey(hDPlaySampleRegKey);

		// DirectInputのデバイスを解放
		pDIDevice?.Unacquire();
		RELEASE(ref pDIDevice);

		pDIDeviceMouse?.Unacquire();
		RELEASE(ref pDIDeviceMouse);

		RELEASE(ref pDInput);

		// COM 終了
		CoUninitialize();
	}

	/*--------------------------------------------
		アプリ変数の起動時初期化
	---------------------------------------------*/
	[Original("init_apl_reg")]
	public void InitializeRegistry()
	{
		Tick = 0;

		/*** フォント設定 ***/
		if (Renderer.GameFont1)
			DeleteObject(Renderer.GameFont1);
		Renderer.GameFont1 = CreateFont(16, null); // フォントオブジェクト

		if (Renderer.GameFont2)
			DeleteObject(Renderer.GameFont2);
		Renderer.GameFont2 = CreateFont(20, null); // フォントオブジェクト

		LeftButton = 0;
		RightButton = 0;

		UserScenarioFileName[0] = unchecked((byte)'\0');
	}

	/*-------------------------------------------
		ファイル　ＳＡＶＥ
	--------------------------------------------*/
	public nint IDD_FILE_SAVE_Proc(HWND hWnd, uint msg, nint wParam, nint lParam)
	{
		WIN32_FIND_DATA FindFileData;
		Array260<byte> buffer = default;

		switch (msg)
		{
			case WM_INITDIALOG:

				SetWindowText(hWnd, "Save File");

				EnableWindow(GetDlgItem(hWnd, IDC_EDIT), TRUE);

				var hFind = FindFirstFile("Scenario\\*.dat", &FindFileData);

				if (hFind != INVALID_HANDLE_VALUE)
				{
					// フォルダに最初のなんかのファイルがあった。
					SendDlgItemMessage(hWnd, IDC_LIST, LB_ADDSTRING, 0, FindFileData.cFileName);
					while (TRUE == FindNextFile(hFind, &FindFileData))
					{
						// フォルダに次のなんかのファイルがあった。
						SendDlgItemMessage(hWnd, IDC_LIST, LB_ADDSTRING, 0, FindFileData.cFileName);
					}
					FindClose();
				}
				UserScenarioFileName[0] = unchecked((byte)'\0');
				break;

			case WM_COMMAND:
				switch (LOWORD(wParam))
				{
					case IDC_LIST:
						if (HIWORD(wParam) == LBN_SELCHANGE)
						{
							DlgDirSelectEx(
								hWnd,
								UserScenarioFileName,
								sizeof(Array260<byte>),
								IDC_LIST
							);
							SetDlgItemText(hWnd, IDC_EDIT, UserScenarioFileName);
						}
						break;

					case IDOK:
						//					PostQuitMessage( 0 );		// これでWM_QUITがでるので接続ダイアログループからぬける
						GetDlgItemText(hWnd, IDC_EDIT, UserScenarioFileName, MAX_PATH);

						if (
							IsEditingMap
							&& Mode == GameMode.Battle
							&& UserScenarioFileName[0] != '\0'
						)
						{
							// なんかユーザーファイルが選ばれた。

							var length = wsprintf(buffer, "%s", UserScenarioFileName);

							if (
								!(
									UserScenarioFileName[length - 4] == '.'
									&& UserScenarioFileName[length - 3] == 'd'
									&& UserScenarioFileName[length - 2] == 'a'
									&& UserScenarioFileName[length - 1] == 't'
								)
							)
								wsprintf(buffer, "%s.dat", UserScenarioFileName);

							wsprintf(UserScenarioFileName, "Scenario\\%s", buffer);

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
				return (FALSE);
		}

		return (TRUE);
	}

	/*-------------------------------------------
		ファイル　ＬＯＡＤ
	--------------------------------------------*/
	public nint IDD_FILE_LOAD_Proc(HWND hWnd, uint msg, nint wParam, nint lParam)
	{
		WIN32_FIND_DATA FindFileData;
		ChatMessage chatMessage = default;
		NameMessage message;
		Array260<byte> buffer = default;

		switch (msg)
		{
			case WM_INITDIALOG:
				SetWindowText(hWnd, "Load File");

				var hFind = FindFirstFile("Scenario\\*.dat", &FindFileData);

				if (hFind != INVALID_HANDLE_VALUE)
				{
					// フォルダに最初のなんかのファイルがあった。
					SendDlgItemMessage(hWnd, IDC_LIST, LB_ADDSTRING, 0, FindFileData.cFileName);
					while (TRUE == FindNextFile(hFind, &FindFileData))
					{
						// フォルダに次のなんかのファイルがあった。
						SendDlgItemMessage(hWnd, IDC_LIST, LB_ADDSTRING, 0, FindFileData.cFileName);
					}
					FindClose();
				}
				UserScenarioFileName[0] = unchecked((byte)'\0');
				break;

			case WM_COMMAND:
				switch (wParam)
				{
					case IDOK:
						DlgDirSelectEx(
							hWnd,
							UserScenarioFileName,
							sizeof(Array260<byte>),
							IDC_LIST
						);

						if (
							IsEditingMap
							&& Mode == GameMode.Battle
							&& UserScenarioFileName[0] != '\0'
						)
						{
							wsprintf(buffer, "%s", UserScenarioFileName);
							wsprintf(UserScenarioFileName, "Scenario\\%s", buffer);

							LoadUserMap();
							Renderer.MakeTerrainSurface();
						}
						else if (Mode == GameMode.GameSetting && UserScenarioFileName[0] != '\0')
						{
							wsprintf(buffer, "%s", UserScenarioFileName);
							wsprintf(UserScenarioFileName, "Scenario\\%s", buffer);

							if (!IsEditingMap)
							{
								// なんかユーザーファイルが選ばれた。
								chatMessage.Type = MessageType.UserScenarioFileName;
								wsprintf(chatMessage.Chat, "%s", UserScenarioFileName);
								bufferDesc.dwBufferSize = (uint)(sizeof(ChatMessage));
								bufferDesc.pBufferData = (byte*)&chatMessage;
								DirectPlayPeer.SendTo(
									RivalPlayerId,
									ref bufferDesc,
									1,
									0,
									null,
									ref hAsync,
									MUST_SEND
								);
							}

							LoadScenarioData();

							// ホストの選択状態をゲストにセンドします。
							message.Type = MessageType.LeaveGameSetting;
							message.Data[0] = (short)HostSide;
							message.Data[1] = ScenarioNumber;

							message.Data[2] = SupplyRates[0];
							message.Data[3] = SupplyRates[1];

							message.Data[4] = IsDecisionEnabled.Value;

							message.Data[5] = InitialSupplyPoints[0];
							message.Data[6] = InitialSupplyPoints[1];

							message.Data[7] = ArrivalControl;

							message.Data[8] = SwapTime;
							message.Data[9] = SwapRule;

							bufferDesc.dwBufferSize = (uint)(sizeof(NameMessage));
							bufferDesc.pBufferData = (byte*)&message;
							DirectPlayPeer.SendTo(
								RivalPlayerId,
								ref bufferDesc,
								1,
								0,
								null,
								ref hAsync,
								MUST_SEND
							);

							Mode = GameMode.ConfigSetting;
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
				return (FALSE);
		}

		return (TRUE);
	}

	/*-------------------------------------------
		OK　キャンセル
	--------------------------------------------*/
	public nint IDD_OK_CANCEL_Proc(HWND hWnd, uint msg, nint wParam, nint lParam)
	{
		switch (msg)
		{
			case WM_INITDIALOG:

				if (DialogAnswer == MessageType.GoToGameSetting)
					SetWindowText(hWnd, "Exit Without Saving?");
				else
					SetWindowText(hWnd, "Resume-save and Exit?");

				break;

			case WM_COMMAND:
				switch (wParam)
				{
					case IDOK:

						BufferedSystemOrders[1] = (byte)DialogAnswer;
						CanOrder = false;
						HasOrdered = true;
						Sound.PlayInterfaceSound(SoundId.Click2);

						DestroyWindow(hWnd);
						MainDialog = null;
						//					PostQuitMessage( 0 );		// これでWM_QUITがでるので接続ダイアログループからぬける
						break;

					case IDCANCEL:
						DestroyWindow(hWnd);
						MainDialog = null;
						//					PostQuitMessage( 0 );		// これでWM_QUITがでるので接続ダイアログループからぬける
						break;

					default:
						return FALSE;
				}
				break;

			default:
				return (FALSE);
		}

		return (TRUE);
	}

	/*-------------------------------------------
	  マイダイアログのループウェイト
		事実上のモーダルダイアログ、制御を戻さない
		にする。
	--------------------------------------------*/
	[Original("my_dlg_wait")]
	public void WaitForDialog()
	{
		MSG msg = default;
		bool waitsForConnection = true;

		while (waitsForConnection)
		{
			while (PeekMessage(ref msg, PM_REMOVE) != 0)
			{
				if (msg.message == WM_QUIT)
				{
					// Quit the application.
					waitsForConnection = false;
					break;
				}

				if (IsDialogMessage(MainDialog, ref msg) == 0)
				{
					TranslateMessage();
					DispatchMessage(ref msg);
				}
			}
		}
	}

	public nint ChatDlgProc(HWND hWnd, uint msg, nint wParam, nint lParam)
	{
		ChatMessage chatMessage = default;

		switch (msg)
		{
			case WM_INITDIALOG:
				IsTypingChat = 1;
				break;
			case WM_COMMAND:
				switch (wParam)
				{
					case IDOK:

						GetDlgItemText(hWnd, IDC_EDIT1, MyChat, MAX_PATH);
						MyChatDisplayTime = unchecked((byte)CHAT_DSP_TIME);

						// なんか入力があったならセンドする
						chatMessage.Type = MessageType.Chat;
						for (var m = 0; m < 128; m++)
						{
							chatMessage.Chat[m] = MyChat[m];
						}

						bufferDesc.dwBufferSize = (uint)(sizeof(ChatMessage));
						bufferDesc.pBufferData = (byte*)&chatMessage;
						DirectPlayPeer.SendTo(
							RivalPlayerId,
							ref bufferDesc,
							1,
							0,
							null,
							ref hAsync,
							MUST_SEND
						);

						DestroyWindow(hwndChatDlg);
						hwndChatDlg = null;
						IsTypingChat = 0;
						break;

					case IDCANCEL:
						DestroyWindow(hwndChatDlg);
						hwndChatDlg = null;
						IsTypingChat = 0;
						break;

					default:
						return FALSE;
				}
				break;

			case WM_KEYDOWN:
				switch (wParam)
				{
					case VK_F1:
						break;
					case VK_ESCAPE:
					case VK_F12:
						DestroyWindow(hwndChatDlg);
						hwndChatDlg = null;
						IsTypingChat = 0;
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

	private void ChangeEditorUnitWeapon()
	{
		if (Units[SelectedUnit].Kind == UnitKind.Attacker)
		{
			// 攻撃機の場合。
			switch (Units[SelectedUnit].Weapon)
			{
				case FireKind.Unarmed:
					Units[SelectedUnit].Weapon = FireKind.Bomb; // 武装品種
					Units[SelectedUnit].Ammo = Units[SelectedUnit].MaxAmmo; // 数
					break;
				case FireKind.Bomb:
					Units[SelectedUnit].Weapon = FireKind.Torpedo; // 武装品種
					Units[SelectedUnit].Ammo = Units[SelectedUnit].MaxAmmo; // 数
					break;
				case FireKind.Torpedo:
					Units[SelectedUnit].Weapon = FireKind.Unarmed; // 武装品種
					Units[SelectedUnit].Ammo = 0; // 数
					break;
			}
			Sound.PlayInterfaceSound(SoundId.Click1);
		}
		else if (Units[SelectedUnit].Kind == UnitKind.Bomber)
		{
			// 爆撃機の場合。
			switch (Units[SelectedUnit].Weapon)
			{
				case FireKind.Unarmed:
					Units[SelectedUnit].Weapon = FireKind.Bomb; // 武装品種
					Units[SelectedUnit].Ammo = Units[SelectedUnit].MaxAmmo; // 数
					break;
				case FireKind.Bomb:
					Units[SelectedUnit].Weapon = FireKind.Torpedo; // 武装品種
					Units[SelectedUnit].Ammo = 1; // 数
					break;
				case FireKind.Torpedo:
					Units[SelectedUnit].Weapon = FireKind.Unarmed; // 武装品種
					Units[SelectedUnit].Ammo = 0; // 数
					break;
			}
			Sound.PlayInterfaceSound(SoundId.Click1);
		}
		else if (
			(
				Units[SelectedUnit].Kind >= UnitKind.Battleship
				&& Units[SelectedUnit].Kind <= UnitKind.LightCarrier
			)
			|| Units[SelectedUnit].Kind == UnitKind.Transport
		)
		{
			if (Units[SelectedUnit].Fuel == -1)
			{
				Units[SelectedUnit].Fuel = 100;
				Units[SelectedUnit].SupplyTime = 0;
			}
			else
			{
				Units[SelectedUnit].Fuel = -1;
				Units[SelectedUnit].SupplyTime = 1;
			}
		}
	}

	private void PlaceEditorUnit(ref int m)
	{
		if (
			UnitInfoPanel[1] != 0
			&& (
				Units[PreviousSelectedUnit].Kind == UnitKind.Carrier
				|| Units[PreviousSelectedUnit].Kind == UnitKind.LightCarrier
				|| Units[PreviousSelectedUnit].Kind == UnitKind.AirBase
			)
			&& (
				(UnitKind)EditorKind == UnitKind.Fighter
				|| (UnitKind)EditorKind == UnitKind.Attacker
				|| (UnitKind)EditorKind == UnitKind.Bomber
			)
		)
		{
			// 駐機場への航空機の配置
			if (Units[PreviousSelectedUnit].Side == LocalSide)
				m = AddPlane(
					LocalSide,
					(UnitKind)EditorKind,
					EditorVariant,
					PreviousSelectedUnit,
					1,
					FireKind.Unarmed
				);

			if (m != 0)
				Sound.PlayInterfaceSound(SoundId.Click1);
		}
		else if (
			UnitInfoPanel[1] == 0
			&& CursorPosition.x <= CMBT_WIDTH
			&& CursorPosition.y <= CMBT_HEIGHT
		)
		{
			// 艦船および、陸上施設

			var cellX = (CameraPosition.X + CursorPosition.x) / 80;
			if (cellX < 0)
				cellX = 0 - cellX;
			double x = (int)cellX;
			if ((cellX - x) >= 0.5)
				x += 1;
			if (((CameraPosition.X + CursorPosition.x) / 80) < 0)
				x = 0 - x;
			x *= 80;

			var cellY = (CameraPosition.Y - CursorPosition.y) / 80;
			if (cellY < 0)
				cellY = 0 - cellY;
			double y = (int)cellY;
			if ((cellY - y) >= 0.5)
				y += 1;
			if (((CameraPosition.Y - CursorPosition.y) / 80) < 0)
				y = 0 - y;
			y *= 80;

			m = 0;
			for (var i = 1; i <= MaxUnitId; i++)
			{
				ref var unit = ref Units[i];
				if (unit.IsUsed && unit.Position.X == x && unit.Position.Y == y)
				{
					m++;
					break;
				}
			}

			if (m == 0)
			{
				m = AddUnit2(
					LocalSide,
					(UnitKind)EditorKind,
					EditorVariant,
					x,
					y,
					(double)(0 + (LocalSide == Side.UnitedStates ? 1 : 0) * 180)
				);

				if (m != 0)
				{
					Sound.PlayInterfaceSound(SoundId.Click1);
				}
				if ((UnitKind)EditorKind == UnitKind.Transport)
				{
					Units[m].Weapon = (FireKind)EditorVariant; // 武装品種
					Units[m].Ammo = 1; // 数
					Units[m].MaxAmmo = 1; // 数 全容量
				}
			}
		}
	}

	private void DeleteEditorUnit()
	{
		if (Units[SelectedUnit].IsUsed)
		{
			// 空母か空港なら搭載ユニットも消す
			if (
				Units[SelectedUnit].Kind == UnitKind.Carrier
				|| Units[SelectedUnit].Kind == UnitKind.LightCarrier
				|| Units[SelectedUnit].Kind == UnitKind.AirBase
			)
			{
				for (var i = 1; i <= MaxUnitId; i++)
				{
					ref var unit = ref Units[i];
					if (
						unit.IsUsed
						&& unit.Category == UnitCategory.Plane
						&& unit.PlaneState == UnitState.Parked
						&& unit.Carrier == SelectedUnit
					)
					{
						unit.Side = 0;
					}
				}
			}

			// パーキング中の航空機なら駐機数を減らします。
			if (
				Units[SelectedUnit].Category == UnitCategory.Plane
				&& Units[SelectedUnit].PlaneState == UnitState.Parked
			)
			{
				Units[Units[SelectedUnit].Carrier].PlaneCount--; // 現在格納数
			}

			Units[SelectedUnit].Side = 0;

			SelectedUnit = 0;
			CombatMenuKind = 0;
			CombatMenuSelection = CombatMenuItem.None;
			ClearSelection2(1);

			Sound.PlayInterfaceSound(SoundId.Click1);
		}
	}

	/*-------------------------------------------
		ウィンドウ処理
	--------------------------------------------*/
	public nint MainWndProc(HWND hWnd, uint msg, nint wParam, nint lParam)
	{
		int m =
			default /* C4701 */
		;
		//OPENFILENAME ofn;		// Unused.
		Array260<byte> scenarioFileName = default; // ユーザーシナリオのファイルネーム

		switch (msg)
		{
			case WM_ACTIVATE: // ウインドウのアクティブ状態が変化
				if (pDIDevice == null || pDIDeviceMouse == null)
					break;

				if (wParam == WA_INACTIVE)
				{
					pDIDevice.Unacquire();
					pDIDeviceMouse.Unacquire();
					IsAppActive = false;
				}
				else
				{
					pDIDevice.Acquire();
					pDIDeviceMouse.Acquire();
					IsAppActive = true;
				}

				break;

			case WM_ACTIVATEAPP: //ウインドウが選択された時
				if (wParam == WA_INACTIVE)
					IsAppActive = false;
				else
					IsAppActive = true;
				break;

			case WM_SIZE: // ウインドウ起動時にもここにくるようだ。

				break;

			case WM_CREATE: //ウインドウ作成時
				break;

			case WM_LBUTTONDOWN:
				break;
			case WM_LBUTTONUP:
				break;

			case WM_MOUSEMOVE:
				// マウスカーソルのウィンドウ上の座標を得る。
				break;

			case WM_KEYDOWN:
				switch (wParam)
				{
					case VK_F1:
						if (IsEditingMap && Mode == GameMode.Battle)
						{
							/*g_hDlg =*/CreateDialog(
								MAKEINTRESOURCE(IDD_FILE_CONT),
								hwndApp,
								(DLGPROC)IDD_FILE_SAVE_Proc
							);

							Sound.PlayInterfaceSound(SoundId.Click1);
						}

						break;

					case VK_F2:
						if (IsEditingMap && Mode == GameMode.Battle)
						{
							Units[SelectedUnit].Side = 0;
							SelectedUnit = 0;
							CombatMenuKind = 0;
							CombatMenuSelection = CombatMenuItem.None;
							ClearSelection2(1);

							// シナリオ選択がユーザーシナリオならファイル選択します。
							/*g_hDlg =*/CreateDialog(
								MAKEINTRESOURCE(IDD_FILE_CONT),
								hwndApp,
								(DLGPROC)IDD_FILE_LOAD_Proc
							);

							Sound.PlayInterfaceSound(SoundId.Click1);
						}
						break;

					case VK_F3:
						if (IsEditingMap && Mode == GameMode.Battle)
						{
							Sound.PlayInterfaceSound(SoundId.Click1);
							Renderer.MakeTerrainSurface();
						}
						else
							DebugMenu--;

						break;

					case VK_F4:
						DebugMenu++;
						break;

					case VK_F5:
						if (IsEditingMap && EditorTarget > 1 && Mode == GameMode.Battle)
						{
							EditorTarget--;
							Sound.PlayInterfaceSound(SoundId.Click1);
						}

#if DBG_MODE
						if (!IsEditingMap)
						{
							if (LocalSide == Side.Japan)
								LocalSide = Side.UnitedStates;
							else
								LocalSide = Side.Japan;
						}
#endif

						break;

					case VK_F6:
						if (IsEditingMap && Mode == GameMode.Battle)
						{
							if (EditorTarget < 24)
							{
								EditorTarget++;
								Sound.PlayInterfaceSound(SoundId.Click1);
							}
						}

#if DBG_MODE
						if (!IsEditingMap)
						{
							Mode = GameMode.GameSetting;
							LoadScenarioData();

							Mode = GameMode.Battle;
							InitializeGame();
						}
#endif
						break;

					case VK_F7:

						if (IsEditingMap && Mode == GameMode.Battle)
						{
							DeleteEditorUnit();
						}
						break;

					case VK_F8:
						if (IsEditingMap && Mode == GameMode.Battle)
						{
							PlaceEditorUnit(ref m);
						}
						break;

					case VK_F9:

#if DBG_MODE
						if (!IsEditingMap)
						{
							Units[SelectedUnit].Hp = 0;
						}
#endif
						if (IsEditingMap && Mode == GameMode.Battle)
						{
							if (LocalSide == Side.Japan)
								LocalSide = Side.UnitedStates;
							else
								LocalSide = Side.Japan;
							Sound.PlayInterfaceSound(SoundId.Click1);
						}
						break;

					case VK_F10:
						break;

					case VK_F11:
						if (IsEditingMap && Mode == GameMode.Battle)
						{
							Reinforcements[(int)LocalSide] = Reinforcements[(int)LocalSide]++;
							Reinforcements[(int)LocalSide] = (byte)(
								Reinforcements[(int)LocalSide] % 4
							);
							Sound.PlayInterfaceSound(SoundId.Click1);
						}
						break;

					case 0x31: // 1
						// ユニットを回転させます。
						if (
							IsEditingMap
							&& Mode == GameMode.Battle
							&& Units[SelectedUnit].IsUsed
							&& Units[SelectedUnit].Kind >= UnitKind.Battleship
							&& Units[SelectedUnit].Kind <= UnitKind.Transport
							&& !(
								Units[SelectedUnit].Category == UnitCategory.Plane
								&& Units[SelectedUnit].PlaneState == UnitState.Parked
							)
						)
						{
							Units[SelectedUnit].Direction = (
								Units[SelectedUnit].Direction + 45.0
							).WholeDegrees();

							Sound.PlayInterfaceSound(SoundId.Click1);
						}

						break;

					case 0x32: // 2
						// 航空機の武装を変えます。
						if (IsEditingMap && Mode == GameMode.Battle && Units[SelectedUnit].IsUsed)
						{
							ChangeEditorUnitWeapon();
						}
						break;

					case 0x33: // 3
						// ＨＰを増やす。
						if (
							IsEditingMap
							&& Mode == GameMode.Battle
							&& Units[SelectedUnit].IsUsed
							&& !(
								Units[SelectedUnit].Category == UnitCategory.Plane
								&& Units[SelectedUnit].PlaneState == UnitState.Parked
							)
						)
						{
							if (Units[SelectedUnit].Hp < Units[SelectedUnit].MaxHp)
							{
								Units[SelectedUnit].Hp++;
								Sound.PlayInterfaceSound(SoundId.Click1);
							}
						}
						break;

					case 0x34: // 4
						// ＨＰを増やす。
						if (
							IsEditingMap
							&& Mode == GameMode.Battle
							&& Units[SelectedUnit].IsUsed
							&& !(
								Units[SelectedUnit].Category == UnitCategory.Plane
								&& Units[SelectedUnit].PlaneState == UnitState.Parked
							)
						)
						{
							Units[SelectedUnit].Hp--;
							Sound.PlayInterfaceSound(SoundId.Click1);
						}
						break;

					case 0x35: // 5
						// ガスをふやす
						if (
							IsEditingMap
							&& Mode == GameMode.Battle
							&& Units[SelectedUnit].IsUsed
							&& Units[SelectedUnit].Kind >= UnitKind.Battleship
							&& Units[SelectedUnit].Kind <= UnitKind.Transport
							&& !(
								Units[SelectedUnit].Category == UnitCategory.Plane
								&& Units[SelectedUnit].PlaneState == UnitState.Parked
							)
						)
						{
							if (Units[SelectedUnit].Fuel < 100)
							{
								Units[SelectedUnit].Fuel++;
								Sound.PlayInterfaceSound(SoundId.Click1);
							}
						}
						break;

					case 0x36: // 6
						// ガスをへらす
						if (
							IsEditingMap
							&& Mode == GameMode.Battle
							&& Units[SelectedUnit].IsUsed
							&& Units[SelectedUnit].Kind >= UnitKind.Battleship
							&& Units[SelectedUnit].Kind <= UnitKind.Transport
							&& !(
								Units[SelectedUnit].Category == UnitCategory.Plane
								&& Units[SelectedUnit].PlaneState == UnitState.Parked
							)
						)
						{
							if (Units[SelectedUnit].Fuel != 0)
							{
								Units[SelectedUnit].Fuel--;
								Sound.PlayInterfaceSound(SoundId.Click1);
							}
						}
						break;

					case 0x37: // 7
						// 弾数をふやす
						if (
							IsEditingMap
							&& Mode == GameMode.Battle
							&& Units[SelectedUnit].IsUsed
							&& Units[SelectedUnit].Kind >= UnitKind.Battleship
							&& Units[SelectedUnit].Kind <= UnitKind.Transport
							&& Units[SelectedUnit].Weapon != FireKind.Unarmed
							&& !(
								Units[SelectedUnit].Category == UnitCategory.Plane
								&& Units[SelectedUnit].PlaneState == UnitState.Parked
							)
						)
						{
							if (Units[SelectedUnit].Ammo < Units[SelectedUnit].MaxAmmo)
							{
								Units[SelectedUnit].Ammo++;
								Sound.PlayInterfaceSound(SoundId.Click1);
							}
						}
						break;

					case 0x38: // 8
						// 弾数をへらす
						if (
							IsEditingMap
							&& Mode == GameMode.Battle
							&& Units[SelectedUnit].IsUsed
							&& Units[SelectedUnit].Kind >= UnitKind.Battleship
							&& Units[SelectedUnit].Kind <= UnitKind.Transport
							&& Units[SelectedUnit].Weapon != FireKind.Unarmed
							&& !(
								Units[SelectedUnit].Category == UnitCategory.Plane
								&& Units[SelectedUnit].PlaneState == UnitState.Parked
							)
						)
						{
							if (Units[SelectedUnit].Ammo != 0)
							{
								Units[SelectedUnit].Ammo--;
								Sound.PlayInterfaceSound(SoundId.Click1);
							}
						}
						break;

					case VK_ESCAPE:
					case VK_F12:
						PostMessage(hWnd, WM_CLOSE, 0, 0);
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
				return DefWindowProc(hWnd, msg);
		}

		return unchecked((nint)0L);
	}
}

public unsafe partial class Renderer
{
	//============================================================================
	//失ったオブジェクトを再読み込みする
	//----------------------------------------------------------------------------
	[Original("restoreAll")]
	public void RestoreSurfaces()
	{
		IDirectDrawSurface_Restore(lpDDSPrimary);
		IDirectDrawSurface_Restore(lpDDSBack);
		IDirectDrawSurface_Restore(OffscreenSurface);

		RELEASE(ref OffscreenSurface);
		OffscreenSurface = LoadBitmapSurface(_game.SpriteSheetName);

		// ピクセルフォーマットを取得
		DDPIXELFORMAT ddpf;
		ddpf.dwSize = (uint)(sizeof(DDPIXELFORMAT));
		OffscreenSurface.GetPixelFormat(&ddpf);

		var KeyColor = ddpf.dwRBitMask | ddpf.dwBBitMask;

		// カラーキーを設定
		DDCOLORKEY key;
		key.dwColorSpaceLowValue = KeyColor;
		key.dwColorSpaceHighValue = KeyColor;
		OffscreenSurface.SetColorKey(DDCKEY_SRCBLT, &key);

		// クリッパー
		RELEASE(ref lpDDclip);
		if (IsFullscreen)
		{
			lpDD.CreateClipper(0, out lpDDclip, null);
			lpDDclip.SetHWnd(0, _game.hwndApp);
			lpDD.FlipToGDISurface();
		}
		else
		{
			lpDD.CreateClipper(0, out lpDDclip, null);
			lpDDclip.SetHWnd(0, _game.hwndApp);
			lpDDSPrimary.SetClipper(lpDDclip);
		}

		// マップを作りなおす
		MakeTerrainSurface();
	}

	[Original("bitmap_surface")]
	public IDirectDrawSurface7? LoadBitmapSurface(string fileName)
	{
		HDC hdc;

		// lインターフェイスビットマップをロード

		var bit = (HBITMAP)_game.LoadImage(fileName);
		if (!bit)
			// ロード失敗、呼び出し側に失敗を返す
			return null;

		// ビットマップのディメンジョンを取得

		BITMAP bitmap;
		_game.GetObject(bit, sizeof(BITMAP), &bitmap);
		int surfaceWidth = bitmap.bmWidth;
		int surfaceHeight = bitmap.bmHeight;

		// サーフェスを作成

		DDSURFACEDESC2 ddsd;
		ZeroMemory(&ddsd, (nuint)(sizeof(DDSURFACEDESC2)));
		ddsd.dwSize = (uint)(sizeof(DDSURFACEDESC2));
		ddsd.dwFlags = DDSD_CAPS | DDSD_WIDTH | DDSD_HEIGHT;
		ddsd.ddsCaps.dwCaps = DDSCAPS_OFFSCREENPLAIN | DDSCAPS_SYSTEMMEMORY;
		ddsd.dwWidth = (uint)surfaceWidth;
		ddsd.dwHeight = (uint)surfaceHeight;

		// サーフェスを実際に作成

		var ddrval = lpDD.CreateSurface(&ddsd, out var surf, null);

		// 作成できたか確認

		if (ddrval != DD_OK)
		{
			// できなかったのでビットマップを解放、呼び出し側に失敗を返す

			_game.DeleteObject(bit);
			return null;
		}
		else
		{
			// できたのでサーフェスのDCを取得

			surf.GetDC(&hdc);

			// 互換DCを生成

			HDC bitmapDc = _game.CreateCompatibleDC();

			// インターフェイスをサーフェスにブロック転送

			_game.SelectObject(bitmapDc, bit);
			_game.BitBlt(hdc, 0, 0, surfaceWidth, surfaceHeight, bitmapDc, 0, 0);

			// DCを解放

			surf.ReleaseDC(hdc);
			_game.DeleteDC(bitmapDc);

			// save the dimensions if rectangle pointer provided
		}

		// ビットマップをクリア

		_game.DeleteObject(bit);

		// 呼び出し側にポインタを返す

		return surf;
	}
}
