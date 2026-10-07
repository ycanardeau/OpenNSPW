using System.Runtime.InteropServices;

namespace OpenNspw;

// Stand-ins for the windows, dialogs and messages of user32 (winuser.h) that the game uses. They keep the state of
// each window and dialog control, and a message queue per game, so that the ported window and dialog procedures and
// message loops run unchanged. Nothing is drawn here: the desktop app draws the windows.
//
// The game runs on its own thread. The functions with Win32 names are called on it. The methods at the end are what a
// user does: tests call ClickDlgItem and the others directly on the game's thread, and the desktop app posts them with
// PostToGame, so that they run on the game's thread when its message loop dispatches them, as in Win32. Window state is
// read and changed under WindowsLock, which the desktop app also takes to draw the windows.

public delegate nint DLGPROC(HWND hDlg, uint msg, nint wParam, nint lParam);

public delegate nint WNDPROC(HWND hWnd, uint msg, nint wParam, nint lParam);

// An item of a combo box or a list box, with the data that CB_SETITEMDATA stores.
public sealed class LISTITEM(string text)
{
	public string Text { get; } = text;

	public object? Data { get; set; }
}

// A window: a top-level window, a dialog, or a control of a dialog.
public sealed class HWND(HWND? parent, int id, string kind, string text, bool enabled, bool sort)
{
	public HWND? Parent { get; } = parent;

	// The control ID, or the dialog's resource ID.
	public int Id { get; } = id;

	// "WINDOW", "DIALOG", or the control's kind in the resource script (NSPW_NET_RC.cs).
	public string Kind { get; } = kind;

	public string Text { get; internal set; } = text;

	public bool Enabled { get; internal set; } = enabled;

	public bool Visible { get; internal set; } = true;

	public bool Sort { get; } = sort;

	public bool Destroyed { get; internal set; }

	public uint Checked { get; internal set; }

	public List<LISTITEM> Items { get; } = [];

	public int CurSel { get; internal set; } = winuser.CB_ERR;

	public DLGPROC? DialogProc { get; internal set; }

	public WNDPROC? WindowProc { get; internal set; }

	public DLGTEMPLATE? Template { get; internal set; }

	public List<HWND> Children { get; } = [];

	// The result passed to EndDialog, for DialogBox.
	public nint? DialogResult { get; internal set; }
}

[StructLayout(LayoutKind.Sequential)]
public struct MSG
{
	public HWND? hwnd;
	public uint message;
	public nint wParam;
	public nint lParam;

	// What a user did, posted by PostToGame; DispatchMessage runs it.
	internal Action? userAction;
}

[StructLayout(LayoutKind.Sequential)]
public struct WNDCLASS
{
	public uint style;
	public WNDPROC? lpfnWndProc;
	public int cbClsExtra;
	public int cbWndExtra;
	public object? hInstance;
	public object? hIcon;
	public object? hCursor;
	public HGDIOBJ hbrBackground;
	public string? lpszMenuName;
	public string? lpszClassName;
}

public static class winuser
{
	public const uint WM_CREATE = 0x0001;
	public const uint WM_DESTROY = 0x0002;
	public const uint WM_SIZE = 0x0005;
	public const uint WM_ACTIVATE = 0x0006;
	public const uint WM_SETTEXT = 0x000C;
	public const uint WM_CLOSE = 0x0010;
	public const uint WM_QUIT = 0x0012;
	public const uint WM_ACTIVATEAPP = 0x001C;
	public const uint WM_SETICON = 0x0080;
	public const uint WM_KEYDOWN = 0x0100;
	public const uint WM_KEYUP = 0x0101;
	public const uint WM_CHAR = 0x0102;
	public const uint WM_INITDIALOG = 0x0110;
	public const uint WM_COMMAND = 0x0111;
	public const uint WM_MOUSEMOVE = 0x0200;
	public const uint WM_LBUTTONDOWN = 0x0201;
	public const uint WM_LBUTTONUP = 0x0202;
	public const uint WM_APP = 0x8000;

	public const int WA_INACTIVE = 0;

	public const int ICON_SMALL = 0;
	public const int ICON_BIG = 1;

	public const uint PM_NOREMOVE = 0x0000;
	public const uint PM_REMOVE = 0x0001;

	public const uint BST_UNCHECKED = 0x0000;
	public const uint BST_CHECKED = 0x0001;

	public const int BN_CLICKED = 0;
	public const int CBN_SELCHANGE = 1;
	public const int LBN_SELCHANGE = 1;
	public const int LBN_DBLCLK = 2;

	public const int CB_OKAY = 0;
	public const int CB_ERR = -1;
	public const int LB_OKAY = 0;
	public const int LB_ERR = -1;

	public const uint CB_GETLBTEXT = 0x0148;
	public const uint CB_ADDSTRING = 0x0143;
	public const uint CB_GETCURSEL = 0x0147;
	public const uint CB_RESETCONTENT = 0x014B;
	public const uint CB_SETCURSEL = 0x014E;
	public const uint CB_GETITEMDATA = 0x0150;
	public const uint CB_SETITEMDATA = 0x0151;
	public const uint CB_FINDSTRINGEXACT = 0x0158;

	public const uint LB_ADDSTRING = 0x0180;
	public const uint LB_RESETCONTENT = 0x0184;
	public const uint LB_SETCURSEL = 0x0186;
	public const uint LB_GETCURSEL = 0x0188;
	public const uint LB_GETTEXT = 0x0189;
	public const uint LB_GETCOUNT = 0x018B;

	public const uint MB_OK = 0x00000000;
	public const uint MB_OKCANCEL = 0x00000001;
	public const uint MB_YESNO = 0x00000004;
	public const uint MB_ICONSTOP = 0x00000010;
	public const uint MB_ICONERROR = 0x00000010;
	public const uint MB_ICONQUESTION = 0x00000020;
	public const uint MB_ICONEXCLAMATION = 0x00000030;
	public const uint MB_ICONINFORMATION = 0x00000040;

	public const int IDYES = 6;
	public const int IDNO = 7;

	public const uint WS_OVERLAPPED = 0x00000000;
	public const uint WS_POPUP = 0x80000000;
	public const uint WS_VISIBLE = 0x10000000;
	public const uint WS_CAPTION = 0x00C00000;
	public const uint WS_SYSMENU = 0x00080000;
	public const uint WS_MINIMIZEBOX = 0x00020000;
	public const int CW_USEDEFAULT = unchecked((int)0x80000000);

	public const int SW_SHOWNORMAL = 1;
	public const int SW_SHOW = 5;
	public const int SW_MINIMIZE = 6;

	public const int SM_CYCAPTION = 4;
	public const int SM_CXDLGFRAME = 7;
	public const int SM_CYDLGFRAME = 8;

	public const int IDC_ARROW = 32512;

	public const int VK_RETURN = 0x0D;
	public const int VK_ESCAPE = 0x1B;
	public const int VK_LEFT = 0x25;
	public const int VK_UP = 0x26;
	public const int VK_RIGHT = 0x27;
	public const int VK_DOWN = 0x28;
	public const int VK_NUMPAD0 = 0x60;
	public const int VK_NUMPAD1 = 0x61;
	public const int VK_NUMPAD2 = 0x62;
	public const int VK_NUMPAD3 = 0x63;
	public const int VK_NUMPAD4 = 0x64;
	public const int VK_NUMPAD5 = 0x65;
	public const int VK_NUMPAD6 = 0x66;
	public const int VK_NUMPAD7 = 0x67;
	public const int VK_NUMPAD8 = 0x68;
	public const int VK_NUMPAD9 = 0x69;
	public const int VK_F1 = 0x70;
	public const int VK_F2 = 0x71;
	public const int VK_F3 = 0x72;
	public const int VK_F4 = 0x73;
	public const int VK_F5 = 0x74;
	public const int VK_F6 = 0x75;
	public const int VK_F7 = 0x76;
	public const int VK_F8 = 0x77;
	public const int VK_F9 = 0x78;
	public const int VK_F10 = 0x79;
	public const int VK_F11 = 0x7A;
	public const int VK_F12 = 0x7B;

	public static int LOWORD(nint l)
	{
		return (int)(l & 0xFFFF);
	}

	public static int HIWORD(nint l)
	{
		return (int)((l >> 16) & 0xFFFF);
	}

	public static nint MAKEWPARAM(int l, int h)
	{
		return (nint)(uint)((l & 0xFFFF) | ((h & 0xFFFF) << 16));
	}

	public static int MAKEINTRESOURCE(int i)
	{
		return i;
	}
}

public partial class Nspw
{
	private readonly List<HWND> _windows = [];
	private readonly Dictionary<string, WNDCLASS> _classes = [];
	private readonly Queue<MSG> _messages = new();
	private readonly byte[] _keyState = new byte[256];
	private POINT _cursor;

	// Taken to read or change windows, by the game's thread and the desktop app.
	public object WindowsLock { get; } = new();

	// The windows that exist, oldest first. Read under WindowsLock.
	public IReadOnlyList<HWND> windows => _windows;

	private static string CString(ReadOnlySpan<byte> text)
	{
		var length = text.IndexOf((byte)0);
		return ShiftJis.GetString(length < 0 ? text : text[..length]);
	}

	// Copies text to a char buffer of cchMax chars, truncated and null-terminated as Win32 does. Returns its length.
	private static int CopyString(string text, Span<byte> buffer, int cchMax)
	{
		if (cchMax <= 0)
		{
			return 0;
		}

		var bytes = ShiftJis.GetBytes(text);
		var length = Math.Min(bytes.Length, Math.Min(cchMax, buffer.Length) - 1);
		bytes.AsSpan(0, length).CopyTo(buffer);
		buffer[length] = 0;
		return length;
	}

	// Calls the window's procedure, outside WindowsLock.
	private static nint CallProc(HWND hWnd, uint msg, nint wParam, nint lParam)
	{
		if (hWnd.Destroyed)
		{
			return 0;
		}

		return hWnd.WindowProc is { } wndProc ? wndProc(hWnd, msg, wParam, lParam)
			: hWnd.DialogProc is { } dlgProc ? dlgProc(hWnd, msg, wParam, lParam)
			: 0;
	}

	public void InitCommonControls()
	{
	}

	public int CoInitializeEx(object? pvReserved, uint dwCoInit)
	{
		return S_OK;
	}

	public void CoUninitialize()
	{
	}

	public object? ImmAssociateContext(HWND? hWnd, object? hIMC)
	{
		return null;
	}

	public void Sleep(uint dwMilliseconds)
	{
		Thread.Sleep((int)dwMilliseconds);
	}

	public object? LoadIcon(object? hInstance, int lpIconName)
	{
		return null;
	}

	public object? LoadCursor(object? hInstance, int lpCursorName)
	{
		return null;
	}

	public int GetSystemMetrics(int nIndex)
	{
		return nIndex switch
		{
			SM_CYCAPTION => 23,
			SM_CXDLGFRAME or SM_CYDLGFRAME => 3,
			_ => 0,
		};
	}

	public ushort RegisterClass(ref WNDCLASS lpWndClass)
	{
		lock (WindowsLock)
		{
			_classes[lpWndClass.lpszClassName ?? ""] = lpWndClass;
		}

		return 1;
	}

	public HWND? CreateWindow(string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, HWND? hWndParent, object? hMenu, object? hInstance, object? lpParam)
	{
		HWND hWnd;
		lock (WindowsLock)
		{
			if (!_classes.TryGetValue(lpClassName, out var wndClass))
			{
				return null;
			}

			hWnd = new HWND(hWndParent, 0, "WINDOW", lpWindowName, true, false)
			{
				WindowProc = wndClass.lpfnWndProc,
				Visible = false,
			};
			_windows.Add(hWnd);
		}

		CallProc(hWnd, WM_CREATE, 0, 0);
		return hWnd;
	}

	public HWND CreateDialog(object? hInstance, int lpTemplate, HWND? hWndParent, DLGPROC lpDialogFunc)
	{
		var template = NSPW_NET_RC.Dialogs[lpTemplate];
		var hDlg = new HWND(hWndParent, template.Id, "DIALOG", template.Caption, true, false)
		{
			DialogProc = lpDialogFunc,
			Template = template,
		};
		foreach (var item in template.Items)
		{
			hDlg.Children.Add(new HWND(hDlg, item.Id, item.Kind, item.Text, !item.Disabled, item.Sort));
		}

		lock (WindowsLock)
		{
			_windows.Add(hDlg);
		}

		CallProc(hDlg, WM_INITDIALOG, 0, 0);
		return hDlg;
	}

	public int ShowWindow(HWND? hWnd, int nCmdShow)
	{
		if (hWnd is null)
		{
			return FALSE;
		}

		var wasVisible = hWnd.Visible;
		hWnd.Visible = nCmdShow != SW_MINIMIZE;
		return wasVisible ? TRUE : FALSE;
	}

	public int UpdateWindow(HWND? hWnd)
	{
		return hWnd is null ? FALSE : TRUE;
	}

	public nint DefWindowProc(HWND hWnd, uint Msg, nint wParam, nint lParam)
	{
		return 0;
	}

	public int DestroyWindow(HWND? hWnd)
	{
		if (hWnd is null || hWnd.Destroyed)
		{
			return FALSE;
		}

		CallProc(hWnd, WM_DESTROY, 0, 0);
		lock (WindowsLock)
		{
			hWnd.Destroyed = true;
			_windows.Remove(hWnd);
		}

		return TRUE;
	}

	public int EndDialog(HWND? hDlg, nint nResult)
	{
		if (hDlg is null)
		{
			return FALSE;
		}

		hDlg.DialogResult = nResult;
		return DestroyWindow(hDlg);
	}

	public int PostMessage(HWND? hWnd, uint Msg, nint wParam, nint lParam)
	{
		lock (_messages)
		{
			_messages.Enqueue(new MSG { hwnd = hWnd, message = Msg, wParam = wParam, lParam = lParam });
			Monitor.PulseAll(_messages);
		}

		return TRUE;
	}

	public void PostQuitMessage(int nExitCode)
	{
		PostMessage(null, WM_QUIT, nExitCode, 0);
	}

	public nint SendMessage(HWND? hWnd, uint Msg, nint wParam, object? lParam)
	{
		if (hWnd is null || Msg == WM_SETICON)
		{
			return 0;
		}

		return CallProc(hWnd, Msg, wParam, lParam is nint l ? l : 0);
	}

	public int PeekMessage(ref MSG lpMsg, HWND? hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg)
	{
		lock (_messages)
		{
			if (!_messages.TryPeek(out var msg))
			{
				return FALSE;
			}

			lpMsg = msg;
			if ((wRemoveMsg & PM_REMOVE) != 0)
			{
				_messages.Dequeue();
			}

			return TRUE;
		}
	}

	// Waits for a message. Returns FALSE for WM_QUIT.
	public int GetMessage(ref MSG lpMsg, HWND? hWnd, uint wMsgFilterMin, uint wMsgFilterMax)
	{
		lock (_messages)
		{
			while (_messages.Count == 0)
			{
				Monitor.Wait(_messages);
			}

			lpMsg = _messages.Dequeue();
			return lpMsg.message == WM_QUIT ? FALSE : TRUE;
		}
	}

	public int WaitMessage()
	{
		lock (_messages)
		{
			while (_messages.Count == 0)
			{
				Monitor.Wait(_messages);
			}
		}

		return TRUE;
	}

	public int TranslateMessage(ref MSG lpMsg)
	{
		return FALSE;
	}

	public nint DispatchMessage(ref MSG lpMsg)
	{
		if (lpMsg.userAction is { } action)
		{
			action();
			return 0;
		}

		return lpMsg.hwnd is { } hWnd ? CallProc(hWnd, lpMsg.message, lpMsg.wParam, lpMsg.lParam) : 0;
	}

	public int IsDialogMessage(HWND? hDlg, ref MSG lpMsg)
	{
		if (hDlg is null || hDlg.Destroyed || (lpMsg.hwnd != hDlg && lpMsg.hwnd?.Parent != hDlg))
		{
			return FALSE;
		}

		DispatchMessage(ref lpMsg);
		return TRUE;
	}

	public HWND? GetDlgItem(HWND? hDlg, int nIDDlgItem)
	{
		return hDlg?.Children.FirstOrDefault(c => c.Id == nIDDlgItem);
	}

	public int SetWindowText(HWND? hWnd, string lpString)
	{
		if (hWnd is null)
		{
			return FALSE;
		}

		lock (WindowsLock)
		{
			hWnd.Text = lpString;
		}

		return TRUE;
	}

	public int SetDlgItemText(HWND? hDlg, int nIDDlgItem, string lpString)
	{
		return SetWindowText(GetDlgItem(hDlg, nIDDlgItem), lpString);
	}

	public int SetDlgItemText(HWND? hDlg, int nIDDlgItem, ReadOnlySpan<byte> lpString)
	{
		return SetDlgItemText(hDlg, nIDDlgItem, CString(lpString));
	}

	public uint GetDlgItemText(HWND? hDlg, int nIDDlgItem, Span<byte> lpString, int cchMax)
	{
		lock (WindowsLock)
		{
			return (uint)CopyString(GetDlgItem(hDlg, nIDDlgItem)?.Text ?? "", lpString, cchMax);
		}
	}

	public int EnableWindow(HWND? hWnd, int bEnable)
	{
		if (hWnd is null)
		{
			return FALSE;
		}

		lock (WindowsLock)
		{
			var wasDisabled = !hWnd.Enabled;
			hWnd.Enabled = bEnable != 0;
			return wasDisabled ? TRUE : FALSE;
		}
	}

	public int CheckDlgButton(HWND? hDlg, int nIDButton, uint uCheck)
	{
		if (GetDlgItem(hDlg, nIDButton) is not { } button)
		{
			return FALSE;
		}

		lock (WindowsLock)
		{
			button.Checked = uCheck;
		}

		return TRUE;
	}

	public uint IsDlgButtonChecked(HWND? hDlg, int nIDButton)
	{
		return GetDlgItem(hDlg, nIDButton)?.Checked ?? BST_UNCHECKED;
	}

	private static int AddString(HWND list, string text)
	{
		var index = list.Sort
			? list.Items.TakeWhile(i => string.Compare(i.Text, text, StringComparison.OrdinalIgnoreCase) <= 0).Count()
			: list.Items.Count;
		list.Items.Insert(index, new LISTITEM(text));
		return index;
	}

	private object? SendListMessage(HWND list, uint Msg, nint wParam, object? lParam, Span<byte> buffer)
	{
		lock (WindowsLock)
		{
			var index = (int)wParam;
			var valid = index >= 0 && index < list.Items.Count;
			switch (Msg)
			{
				case CB_RESETCONTENT or LB_RESETCONTENT:
					list.Items.Clear();
					list.CurSel = CB_ERR;
					return CB_OKAY;
				case CB_ADDSTRING or LB_ADDSTRING:
					return AddString(list, lParam as string ?? CString(buffer));
				case CB_SETITEMDATA:
					if (!valid)
					{
						return CB_ERR;
					}

					list.Items[index].Data = lParam;
					return CB_OKAY;
				case CB_GETITEMDATA:
					return valid ? list.Items[index].Data : CB_ERR;
				case CB_SETCURSEL or LB_SETCURSEL:
					list.CurSel = valid ? index : CB_ERR;
					return list.CurSel;
				case CB_GETCURSEL or LB_GETCURSEL:
					return list.CurSel;
				case LB_GETCOUNT:
					return list.Items.Count;
				case CB_FINDSTRINGEXACT:
					var text = lParam as string ?? CString(buffer);
					return list.Items.FindIndex(index + 1, i => string.Equals(i.Text, text, StringComparison.OrdinalIgnoreCase));
				case CB_GETLBTEXT or LB_GETTEXT:
					return valid ? CopyString(list.Items[index].Text, buffer, buffer.Length) : CB_ERR;
				default:
					return 0;
			}
		}
	}

	// The overloads take the kinds of LPARAM that the game passes: a number, a string literal, a GUID for
	// CB_SETITEMDATA, or a char buffer.

	public object? SendDlgItemMessage(HWND? hDlg, int nIDDlgItem, uint Msg, nint wParam, nint lParam)
	{
		return GetDlgItem(hDlg, nIDDlgItem) is { } list ? SendListMessage(list, Msg, wParam, lParam, []) : 0;
	}

	public object? SendDlgItemMessage(HWND? hDlg, int nIDDlgItem, uint Msg, nint wParam, string lParam)
	{
		return GetDlgItem(hDlg, nIDDlgItem) is { } list ? SendListMessage(list, Msg, wParam, lParam, []) : 0;
	}

	public object? SendDlgItemMessage(HWND? hDlg, int nIDDlgItem, uint Msg, nint wParam, Guid? lParam)
	{
		return GetDlgItem(hDlg, nIDDlgItem) is { } list ? SendListMessage(list, Msg, wParam, lParam, []) : 0;
	}

	public object? SendDlgItemMessage(HWND? hDlg, int nIDDlgItem, uint Msg, nint wParam, Span<byte> lParam)
	{
		return GetDlgItem(hDlg, nIDDlgItem) is { } list ? SendListMessage(list, Msg, wParam, null, lParam) : 0;
	}

	public int MessageBox(HWND? hWnd, string lpText, string lpCaption, uint uType)
	{
		return _platform.MessageBox(hWnd, lpText, lpCaption, uType);
	}

	public int MessageBox(HWND? hWnd, string lpText, ReadOnlySpan<byte> lpCaption, uint uType)
	{
		return MessageBox(hWnd, lpText, CString(lpCaption), uType);
	}

	public int GetCursorPos(ref POINT lpPoint)
	{
		lpPoint = _cursor;
		return TRUE;
	}

	// The game's window has no frame in the desktop app, so screen and client coordinates are the same.
	public int ScreenToClient(HWND? hWnd, ref POINT lpPoint)
	{
		return TRUE;
	}

	public int GetKeyboardState(Span<byte> lpKeyState)
	{
		lock (_keyState)
		{
			_keyState.CopyTo(lpKeyState);
		}

		return TRUE;
	}

	// What a user does. Tests call these on the game's thread; the desktop app posts them with PostToGame.

	public void ClickDlgItem(HWND hDlg, int nIDDlgItem)
	{
		var control = GetDlgItem(hDlg, nIDDlgItem) ?? throw new ArgumentException($"No control {nIDDlgItem}.", nameof(nIDDlgItem));
		if (!control.Enabled || hDlg.Destroyed)
		{
			return;
		}

		if (control.Kind == "AUTOCHECKBOX")
		{
			lock (WindowsLock)
			{
				control.Checked = control.Checked == BST_CHECKED ? BST_UNCHECKED : BST_CHECKED;
			}
		}

		CallProc(hDlg, WM_COMMAND, MAKEWPARAM(nIDDlgItem, BN_CLICKED), 0);
	}

	public void TypeDlgItemText(HWND hDlg, int nIDDlgItem, string text)
	{
		var control = GetDlgItem(hDlg, nIDDlgItem) ?? throw new ArgumentException($"No control {nIDDlgItem}.", nameof(nIDDlgItem));
		if (control.Enabled)
		{
			SetWindowText(control, text);
		}
	}

	public void SelectDlgItem(HWND hDlg, int nIDDlgItem, int index)
	{
		var control = GetDlgItem(hDlg, nIDDlgItem) ?? throw new ArgumentException($"No control {nIDDlgItem}.", nameof(nIDDlgItem));
		if (!control.Enabled)
		{
			return;
		}

		lock (WindowsLock)
		{
			control.CurSel = index;
		}

		CallProc(hDlg, WM_COMMAND, MAKEWPARAM(nIDDlgItem, control.Kind == "LISTBOX" ? LBN_SELCHANGE : CBN_SELCHANGE), 0);
	}

	// Runs a user's action on the game's thread, when its message loop dispatches it.
	public void PostToGame(Action action)
	{
		lock (_messages)
		{
			_messages.Enqueue(new MSG { userAction = action });
			Monitor.PulseAll(_messages);
		}
	}

	public void SetCursorPos(int x, int y)
	{
		_cursor = new POINT { x = x, y = y };
	}

	// A virtual key went down or up, for GetKeyboardState. Posts WM_KEYDOWN to the window.
	public void SetKeyState(HWND? hWnd, int vk, bool down)
	{
		lock (_keyState)
		{
			_keyState[vk & 0xFF] = down ? (byte)0x80 : (byte)0;
		}

		if (down && hWnd is not null)
		{
			PostMessage(hWnd, WM_KEYDOWN, vk, 0);
		}
	}
}
