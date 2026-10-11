namespace OpenNspw;

// The category of a unit (UNIT.ctgry). The underlying type is the field's.
public enum UnitCategory
{
	// An unused unit.
	None = 0,

	[Original("SHIP")]
	Ship = 1,

	[Original("PLANE")]
	Plane = 2,

	[Original("BASE")]
	Base = 3,
}
