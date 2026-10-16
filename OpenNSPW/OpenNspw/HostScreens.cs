namespace OpenNspw;

// The game's screens that the host's own UI draws in the game's place (docs/Ui.md). Not part of the original.
public partial class Nspw
{
	// Set by a host whose UI draws the battle and the setting screens, before the game starts: the game runs their
	// frames as it does, clicks and all, but does not draw them (DirectDrawSurface7.IsDrawn).
	public bool HostDrawsScreens { get; set; }

	// Whether the screen is one that the host's UI draws: a battle's, but the map editor's, or a setting screen.
	public bool IsHostScreen()
	{
		return (Mode==GameMode.Battle && !IsEditingMap) || Mode==GameMode.GameSetting || Mode==GameMode.ConfigSetting;
	}
}
