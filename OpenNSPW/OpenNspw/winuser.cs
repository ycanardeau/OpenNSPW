using System.Runtime.InteropServices;

namespace OpenNspw;

// Stand-ins for the windows, dialogs and messages of user32 (winuser.h) that the game uses. They keep the state of
// each window and dialog control, and a message queue per game, so that the ported dialog procedures and message loops
// run unchanged. Nothing is drawn here: the desktop app draws the windows, and tests read and drive them through the
// methods at the end (ClickDlgItem and others), as a user would.

public delegate nint DLGPROC(HWND hDlg, uint msg, nint wParam, nint lParam);

// An item of a combo box or a list box, with the data that CB_SETITEMDATA stores.
public sealed class LISTITEM(string text)
{
	public string Text { get; } = text;

	public object? Data { get; set; }
}

// A window: a dialog, or a control of a dialog.
public sealed class HWND(HWND? parent, int id, string kind, string text, bool enabled, bool sort)
{
	public HWND? Parent { get; } = parent;

	// The control ID, or the dialog's resource ID.
	public int Id { get; } = id;

	// "DIALOG", or the control's kind in the resource script (NSPW_NET_RC.cs).
	public string Kind { get; } = kind;

	public string Text { get; internal set; } = text;

	public bool Enabled { get; internal set; } = enabled;

	public bool Sort { get; } = sort;

	public bool Destroyed { get; internal set; }

	public uint Checked { get; internal set; }

	public List<LISTITEM> Items { get; } = [];

	public int CurSel { get; internal set; } = winuser.CB_ERR;

	public DLGPROC? DialogProc { get; internal set; }

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
}

public static class winuser
{
	public const uint WM_DESTROY = 0x0002;
	public const uint WM_SETTEXT = 0x000C;
	public const uint WM_QUIT = 0x0012;
	public const uint WM_SETICON = 0x0080;
	public const uint WM_INITDIALOG = 0x0110;
	public const uint WM_COMMAND = 0x0111;
	public const uint WM_APP = 0x8000;

	public const int ICON_SMALL = 0;
	public const int ICON_BIG = 1;

	public const uint PM_NOREMOVE = 0x0000;
	public const uint PM_REMOVE = 0x0001;

	public const uint BST_UNCHECKED = 0x0000;
	public const uint BST_CHECKED = 0x0001;

	public const int BN_CLICKED = 0;
	public const int CBN_SELCHANGE = 1;

	public const int CB_OKAY = 0;
	public const int CB_ERR = -1;
	public const int LB_ERR = -1;

	public const uint CB_GETLBTEXT = 0x0148;
	public const uint CB_ADDSTRING = 0x0143;
	public const uint CB_GETCURSEL = 0x0147;
	public const uint CB_RESETCONTENT = 0x014B;
	public const uint CB_SETCURSEL = 0x014E;
	public const uint CB_GETITEMDATA = 0x0150;
	public const uint CB_SETITEMDATA = 0x0151;
	public const uint CB_FINDSTRINGEXACT = 0x0158;

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
	private readonly Queue<MSG> _messages = new();

	// The windows that exist, oldest first.
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

	private static nint SendDialogMessage(HWND hDlg, uint msg, nint wParam, nint lParam)
	{
		return hDlg.DialogProc is { } proc && !hDlg.Destroyed ? proc(hDlg, msg, wParam, lParam) : 0;
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

		_windows.Add(hDlg);
		SendDialogMessage(hDlg, WM_INITDIALOG, 0, 0);
		return hDlg;
	}

	public int DestroyWindow(HWND? hWnd)
	{
		if (hWnd is null || hWnd.Destroyed)
		{
			return FALSE;
		}

		SendDialogMessage(hWnd, WM_DESTROY, 0, 0);
		hWnd.Destroyed = true;
		_windows.Remove(hWnd);
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
		_messages.Enqueue(new MSG { hwnd = hWnd, message = Msg, wParam = wParam, lParam = lParam });
		return TRUE;
	}

	public void PostQuitMessage(int nExitCode)
	{
		PostMessage(null, WM_QUIT, nExitCode, 0);
	}

	public nint SendMessage(HWND? hWnd, uint Msg, nint wParam, object? lParam)
	{
		if (hWnd is null)
		{
			return 0;
		}

		return Msg switch
		{
			WM_SETICON => 0,
			_ => SendDialogMessage(hWnd, Msg, wParam, lParam is nint l ? l : 0),
		};
	}

	public int PeekMessage(ref MSG lpMsg, HWND? hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg)
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

	public int TranslateMessage(ref MSG lpMsg)
	{
		return FALSE;
	}

	public nint DispatchMessage(ref MSG lpMsg)
	{
		return lpMsg.hwnd is { } hWnd ? SendDialogMessage(hWnd, lpMsg.message, lpMsg.wParam, lpMsg.lParam) : 0;
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

		hWnd.Text = lpString;
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
		return (uint)CopyString(GetDlgItem(hDlg, nIDDlgItem)?.Text ?? "", lpString, cchMax);
	}

	public int EnableWindow(HWND? hWnd, int bEnable)
	{
		if (hWnd is null)
		{
			return FALSE;
		}

		var wasDisabled = !hWnd.Enabled;
		hWnd.Enabled = bEnable != 0;
		return wasDisabled ? TRUE : FALSE;
	}

	public int CheckDlgButton(HWND? hDlg, int nIDButton, uint uCheck)
	{
		if (GetDlgItem(hDlg, nIDButton) is not { } button)
		{
			return FALSE;
		}

		button.Checked = uCheck;
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

	private static object? SendListMessage(HWND list, uint Msg, nint wParam, object? lParam, Span<byte> buffer)
	{
		var index = (int)wParam;
		var valid = index >= 0 && index < list.Items.Count;
		switch (Msg)
		{
			case CB_RESETCONTENT:
				list.Items.Clear();
				list.CurSel = CB_ERR;
				return CB_OKAY;
			case CB_ADDSTRING:
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
			case CB_SETCURSEL:
				list.CurSel = valid ? index : CB_ERR;
				return list.CurSel;
			case CB_GETCURSEL:
				return list.CurSel;
			case CB_FINDSTRINGEXACT:
				var text = lParam as string ?? CString(buffer);
				return list.Items.FindIndex(index + 1, i => string.Equals(i.Text, text, StringComparison.OrdinalIgnoreCase));
			case CB_GETLBTEXT:
				return valid ? CopyString(list.Items[index].Text, buffer, buffer.Length) : CB_ERR;
			default:
				return 0;
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

	// LPARAM is a char buffer, which CB_ADDSTRING and CB_FINDSTRINGEXACT read and CB_GETLBTEXT writes.
	public object? SendDlgItemMessage(HWND? hDlg, int nIDDlgItem, uint Msg, nint wParam, Span<byte> lParam)
	{
		return GetDlgItem(hDlg, nIDDlgItem) is { } list ? SendListMessage(list, Msg, wParam, null, lParam) : 0;
	}

	public object? LoadIcon(object? hInstance, int lpIconName)
	{
		return null;
	}

	public int MessageBox(HWND? hWnd, string lpText, string lpCaption, uint uType)
	{
		return _platform.MessageBox(hWnd, lpText, lpCaption, uType);
	}

	public int MessageBox(HWND? hWnd, string lpText, ReadOnlySpan<byte> lpCaption, uint uType)
	{
		return MessageBox(hWnd, lpText, CString(lpCaption), uType);
	}

	// What a user does to a dialog. Used by tests and by the desktop app.

	public void ClickDlgItem(HWND hDlg, int nIDDlgItem)
	{
		var control = GetDlgItem(hDlg, nIDDlgItem) ?? throw new ArgumentException($"No control {nIDDlgItem}.", nameof(nIDDlgItem));
		if (!control.Enabled || hDlg.Destroyed)
		{
			return;
		}

		if (control.Kind == "AUTOCHECKBOX")
		{
			control.Checked = control.Checked == BST_CHECKED ? BST_UNCHECKED : BST_CHECKED;
		}

		SendDialogMessage(hDlg, WM_COMMAND, MAKEWPARAM(nIDDlgItem, BN_CLICKED), 0);
	}

	public void TypeDlgItemText(HWND hDlg, int nIDDlgItem, string text)
	{
		var control = GetDlgItem(hDlg, nIDDlgItem) ?? throw new ArgumentException($"No control {nIDDlgItem}.", nameof(nIDDlgItem));
		if (control.Enabled)
		{
			control.Text = text;
		}
	}

	public void SelectDlgItem(HWND hDlg, int nIDDlgItem, int index)
	{
		var control = GetDlgItem(hDlg, nIDDlgItem) ?? throw new ArgumentException($"No control {nIDDlgItem}.", nameof(nIDDlgItem));
		if (!control.Enabled)
		{
			return;
		}

		control.CurSel = index;
		SendDialogMessage(hDlg, WM_COMMAND, MAKEWPARAM(nIDDlgItem, CBN_SELCHANGE), 0);
	}
}
