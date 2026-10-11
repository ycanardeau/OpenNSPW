namespace OpenNspw;

// Whether a plane flies or is parked on its carrier or base (UNIT.info[0] of planes, UNIT.PlaneState). The underlying
// type is the slot's.
public enum UnitState
{
	None = 0,

	[Original("FLYING")]
	Flying = 1,

	[Original("PARKING")]
	Parked = 2,
}
