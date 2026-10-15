using System.Drawing;

namespace OpenNspw.Desktop.Controls;

internal static class Palette
{
	// The fill of the game's setting screens (UpdateFrame, win_main.cpp): 0x0300 in RGB565.
	public static readonly Color SettingScreen = Color.FromArgb(0, 97, 0);
}
