// Port of the dialogs in NSPW_NET.RC, as data for CreateDialog and DialogBox (winuser.cs). Positions and sizes are in
// dialog units, as in the resource script.

using System.Collections.Immutable;

namespace OpenNspw;

// A control of a dialog template: LTEXT, RTEXT, EDITTEXT, COMBOBOX, PUSHBUTTON, DEFPUSHBUTTON, GROUPBOX, LISTBOX or
// an auto check box (CONTROL ... "Button", BS_AUTOCHECKBOX). Disabled is WS_DISABLED, Sort is CBS_SORT or LBS_SORT.
public sealed record DLGITEMTEMPLATE(string Kind, string Text, int Id, int X, int Y, int Cx, int Cy, bool Disabled = false, bool Sort = false);

public sealed record DLGTEMPLATE(int Id, string Caption, int Cx, int Cy, string FontName, int FontSize, ImmutableArray<DLGITEMTEMPLATE> Items);

public static class NSPW_NET_RC
{
	private const int IDC_STATIC = -1;

	public static readonly DLGTEMPLATE IDD_FILE_CONT_TEMPLATE = new(resource.IDD_FILE_CONT, "NSPW on the net", 147, 252, "MS UI Gothic", 10, [
		new("DEFPUSHBUTTON", "OK", windef.IDOK, 9, 209, 63, 33),
		new("PUSHBUTTON", "ｷｬﾝｾﾙ", windef.IDCANCEL, 80, 217, 49, 20),
		new("LISTBOX", "", resource.IDC_LIST, 17, 12, 106, 159, Sort: true),
		new("EDITTEXT", "", resource.IDC_EDIT, 17, 177, 106, 17, Disabled: true),
	]);

	public static readonly DLGTEMPLATE IDD_ADDRESS_OVERRIDE_TEMPLATE = new(resource.IDD_ADDRESS_OVERRIDE, "NSPW NET CONTECTION SETTING", 245, 145, "MS UI Gothic", 11, [
		new("RTEXT", "Your Name:", IDC_STATIC, 26, 21, 55, 8),
		new("EDITTEXT", "", resource.IDC_PLAYER_NAME, 87, 19, 108, 12),
		new("RTEXT", "Service Provider:", IDC_STATIC, 3, 67, 31, 8),
		new("COMBOBOX", "", resource.IDC_SP_COMBO, 44, 65, 190, 65, Sort: true),
		new("RTEXT", "Address", resource.IDC_ADDRESS_LINE1_TEXT, 4, 84, 31, 8),
		new("EDITTEXT", "", resource.IDC_ADDRESS_LINE1, 44, 82, 119, 12),
		new("RTEXT", "Port", resource.IDC_ADDRESS_LINE2_TEXT, 163, 84, 24, 8),
		new("EDITTEXT", "", resource.IDC_ADDRESS_LINE2, 190, 82, 43, 12),
		new("RTEXT", "Adapter", IDC_STATIC, 2, 101, 33, 8),
		new("COMBOBOX", "", resource.IDC_ADAPTER_COMBO, 44, 99, 190, 89, Sort: true),
		new("AUTOCHECKBOX", "Host Player", resource.IDC_HOST_SESSION, 98, 41, 71, 10),
		new("PUSHBUTTON", "&OK", windef.IDOK, 102, 121, 65, 14),
		new("PUSHBUTTON", "&Cancel", windef.IDCANCEL, 172, 121, 50, 14),
		new("GROUPBOX", "", IDC_STATIC, 19, 9, 205, 46),
		new("PUSHBUTTON", "Sinario Edit", resource.IDSNROEDIT, 9, 120, 60, 17),
	]);

	public static readonly DLGTEMPLATE IDD_MAIN_GAME_TEMPLATE = new(resource.IDD_MAIN_GAME, "Waiting for Rival Player", 174, 126, "MS UI Gothic", 11, [
		new("DEFPUSHBUTTON", "START GAME", resource.IDC_START_GAME, 21, 99, 80, 18),
		new("GROUPBOX", "", IDC_STATIC, 7, 8, 160, 79),
		new("LTEXT", "Your Name:", IDC_STATIC, 13, 24, 38, 8),
		new("LTEXT", "Static", resource.IDC_PLAYER_NAME, 39, 36, 113, 8),
		new("PUSHBUTTON", "Exit", windef.IDCANCEL, 108, 100, 46, 15),
		new("LTEXT", "Rival Name:", IDC_STATIC, 13, 54, 62, 8),
		new("LTEXT", "Static", resource.IDC_PLAYER_NAME2, 39, 66, 113, 8),
	]);

	public static readonly DLGTEMPLATE IDD_CHAT_DIALOG_TEMPLATE = new(resource.IDD_CHAT_DIALOG, "", 271, 46, "ＭＳ ゴシック", 10, [
		new("EDITTEXT", "", resource.IDC_EDIT1, 9, 8, 250, 13),
		new("PUSHBUTTON", "OK", windef.IDOK, 78, 25, 96, 19),
		new("PUSHBUTTON", "Cancel", windef.IDCANCEL, 186, 28, 66, 14),
	]);

	public static readonly DLGTEMPLATE IDD_OK_CANCEL_TEMPLATE = new(resource.IDD_OK_CANCEL, "NSPW on the net", 159, 47, "MS UI Gothic", 10, [
		new("DEFPUSHBUTTON", "OK", windef.IDOK, 7, 11, 75, 25),
		new("PUSHBUTTON", "Cancel", windef.IDCANCEL, 91, 15, 58, 20),
	]);

	public static readonly ImmutableDictionary<int, DLGTEMPLATE> Dialogs = new[]
	{
		IDD_FILE_CONT_TEMPLATE,
		IDD_ADDRESS_OVERRIDE_TEMPLATE,
		IDD_MAIN_GAME_TEMPLATE,
		IDD_CHAT_DIALOG_TEMPLATE,
		IDD_OK_CANCEL_TEMPLATE,
	}.ToImmutableDictionary(d => d.Id);
}
