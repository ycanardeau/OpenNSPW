namespace OpenNspw;

// The screen the game is on (mode, rival_mode). The underlying type is the globals'. The network message _DP_FLAG keeps
// its short.
public enum GameMode : short
{
	None = 0,

	// The title screen (demo.cpp).
	[Original("DEMO")]
	Title = 1,

	[Original("CNCT_GAME_SETUP")]
	Setup = 3,

	// Choosing the scenario.
	[Original("CNCT_GAME_SETTING")]
	GameSetting = 4,

	// Choosing the side and the rules.
	[Original("CNCT_CNFG_SETTING")]
	ConfigSetting = 5,

	[Original("CMBT")]
	Battle = 10,
}
