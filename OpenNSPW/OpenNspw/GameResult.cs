namespace OpenNspw;

// How a battle ended (game_end), or None while it goes on. The underlying type is the global's.
public enum GameResult
{
	None = 0,

	[Original("JPN_WIN")]
	JapanWon = 1,

	[Original("USA_WIN")]
	UnitedStatesWon = 2,

	[Original("JPN_LOST")]
	JapanLost = 3,

	[Original("USA_LOST")]
	UnitedStatesLost = 4,

	[Original("DRAW")]
	Draw = 10,
}
