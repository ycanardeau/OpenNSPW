namespace OpenNspw;

// The single-player mode (docs/Singleplayer.md): the game against the computer of NSPW 1.22, whose own functions run in
// an emulator on this state.
public unsafe partial class Nspw
{
	// Whether this is a single-player game, which numbers its units as 1.22 does.
	public bool IsSingleplayer;

	// The unit ranges of all_head.h. 1.22 has the same, with 20 ships and 30 planes a side instead of 40 and 50.
	public int SHIP_LIMIT => IsSingleplayer ? 20 : 40;
	public int PLANE_LIMIT => IsSingleplayer ? 30 : 50;

	public int JPN_SHIP_START => 1;
	public int JPN_SHIP_END => JPN_SHIP_START - 1 + SHIP_LIMIT;

	public int USA_SHIP_START => JPN_SHIP_END + 1;
	public int USA_SHIP_END => USA_SHIP_START - 1 + SHIP_LIMIT;

	public int JPN_PLANE_START => USA_SHIP_END + 1;
	public int JPN_PLANE_END => JPN_PLANE_START - 1 + PLANE_LIMIT;

	public int USA_PLANE_START => JPN_PLANE_END + 1;
	public int USA_PLANE_END => USA_PLANE_START - 1 + PLANE_LIMIT;
}
