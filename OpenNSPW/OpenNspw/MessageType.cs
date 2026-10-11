namespace OpenNspw;

// The type of a network message (dwType), and of the system orders that reuse its values (game_system_menu,
// join_game_start, dlg_answer). The underlying type is dwType's. MSG_TST, MSG_EXIT_WAITING and MSG_END are another set.
public enum MessageType : uint
{
	None = 0,

	// The guest's name, sent when it joins.
	[Original("MY_NAME_IS")]
	MyNameIs = 1,

	// The host's name, sent in reply.
	[Original("AND_MY_NAME_IS")]
	AndMyNameIs = 2,

	[Original("OUT_SETUP")]
	LeaveSetup = 3,

	[Original("OUT_GAME_SETTING")]
	LeaveGameSetting = 4,

	[Original("SIDE_AND_SINARIO")]
	SideAndScenario = 5,

	[Original("GO_GAME_SETTING")]
	GoToGameSetting = 6,

	[Original("OUT_CNFG_SETTING")]
	LeaveConfigSetting = 7,

	[Original("RESUME_AND_GO_GAME_SETTING")]
	ResumeAndGoToGameSetting = 8,

	[Original("START_IN_RESUME")]
	StartFromResume = 9,

	[Original("START_IN_AUTOSAVE")]
	StartFromAutoSave = 10,

	[Original("USER_SINARIO_FN")]
	UserScenarioFileName = 11,

	// A turn without an order.
	[Original("DP_NO_ORDER")]
	NoOrder = 15,

	[Original("DP_NEW_PP")]
	MoveOrder = 20,

	[Original("DP_NEW_PP_SHIP")]
	MoveShipsOrder = 21,

	[Original("DP_NEW_PP_PLANE")]
	MovePlanesOrder = 22,

	[Original("DP_NEW_SLCT")]
	SelectOrder = 30,

	[Original("DP_NEW_SLCT_SHIP")]
	SelectShipsOrder = 31,

	[Original("DP_NEW_SLCT_PLANE")]
	SelectPlanesOrder = 32,

	[Original("DP_NEW_SLCT_LAND")]
	SelectLandOrder = 33,

	[Original("DP_NEW_MENU")]
	MenuOrder = 40,

	// The checksums of a turn.
	[Original("DP_FLAG_1")]
	SyncFlag = 50,

	[Original("DP_ARRIVED_UNIT")]
	UnitArrived = 60,

	[Original("DP_CHAT_1")]
	Chat = 100,

	[Original("RIVAL_MODE")]
	RivalMode = 500,

	[Original("RIVAL_VER")]
	RivalVersion = 501,
}
