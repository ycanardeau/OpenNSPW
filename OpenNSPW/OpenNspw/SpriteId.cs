namespace OpenNspw;

// A sprite (the original's スプライトナンバー), an index into Sprites.
public enum SpriteId
{
	// The title screen.
	[Original("TTL_BACK")]
	TitleBackground = 0,

	// The units of Japan, by kind and direction.
	[Original("UNIT_JPN")]
	JapanUnits = 1,

	// The units of the United States.
	[Original("UNIT_USA")]
	UnitedStatesUnits = 2,

	// The pictures of the units of Japan in the unit info panel.
	[Original("UNIT_INFO_JPN")]
	JapanUnitInfo = 3,

	[Original("UNIT_INFO_USA")]
	UnitedStatesUnitInfo = 4,

	// The tiles of the terrain, and the clouds.
	[Original("MAP_TIP_NRML")]
	MapTiles = 5,

	// The effects and the fires.
	[Original("SUB_UNIT")]
	SubUnits = 6,

	// The buttons of the combat menu and the deck view.
	[Original("BTN_1")]
	Buttons1 = 7,

	[Original("BTN_2")]
	Buttons2 = 8,

	// The panel of the buttons, beside the battle area.
	[Original("BTN_BASE")]
	ButtonBase = 9,

	// The minimap.
	[Original("MAP_BASE")]
	Minimap = 10,
}
