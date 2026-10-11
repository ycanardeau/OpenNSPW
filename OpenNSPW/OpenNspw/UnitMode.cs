namespace OpenNspw;

// How a unit moves (UNIT.info[5], UNIT.Mode), as chosen in the combat menu. The underlying type is the slot's. The code
// writes only Move and Return, and compares the mode with `<= Slow`.
public enum UnitMode
{
	None = 0,

	[Original("MOVE")]
	Move = 1,

	[Original("SLOW")]
	Slow = 2,

	[Original("RETURN")]
	Return = 4,
}
